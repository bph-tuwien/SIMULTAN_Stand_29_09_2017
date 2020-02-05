using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows;
using System.Globalization;

using ParameterStructure.Component;
using ParameterStructure.Parameter;
using ParameterStructure.Values;

namespace ParameterStructure.DXF
{
    #region HELP CLASSES

    internal enum PartialRecordState
    {
        UNCHECKED = 0,
        UNCHANGED = 1,
        UPDATED = 2,
        STRUCTURE_CHANGED = 3,
        NOT_FOUND = 4
    }

    internal class PartialFileRecord
    {
        public string FileName { get; set; }
        public ComponentManagerType Manager { get; set; }
        public DateTime LastModified { get; set; }       
        public int NrLocks { get; set; }

        #region METHODS: ToString

        public void AddToExport(ref StringBuilder _sb)
        {
            if (_sb == null) return;

            _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString());    // 0
            string path_rel = DXFDistributedDecoder.ExtractRelativeNameFromAbsolute(this.FileName);
            _sb.AppendLine(path_rel);                                                    // file name

            _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_KEY).ToString());      // 904
            _sb.AppendLine(ComponentUtils.ComponentManagerTypeToLetter(this.Manager));   // role that can modify the file
            _sb.AppendLine(((int)ParamStructCommonSaveCode.TIME_STAMP).ToString());      // 902
            _sb.AppendLine(this.LastModified.ToString(ParamStructTypes.DT_FORMATTER));   // last modified
            _sb.AppendLine(((int)ParamStructCommonSaveCode.X_VALUE).ToString());         // 910
            _sb.AppendLine(this.NrLocks.ToString());                                     // nrumber of locks on this file

        }

        #endregion

        #region STATIC

        public static PartialRecordState CompareRecordStates(List<PartialFileRecord> _record1, List<PartialFileRecord> _record2)
        {
            if (_record1 == null && _record2 == null)
                return PartialRecordState.UNCHANGED;
            else if (_record1 == null || _record2 == null)
                return PartialRecordState.STRUCTURE_CHANGED;

            if (_record1.Count != _record2.Count) return PartialRecordState.STRUCTURE_CHANGED;

            List<PartialRecordState> checked_mark = Enumerable.Repeat(PartialRecordState.UNCHECKED, _record1.Count).ToList();
            for (int c1 = 0; c1 < _record1.Count; c1++ )
            {
                for (int c2 = 0; c2 < _record2.Count; c2++)
                {                   
                    if (_record1[c1].Manager == _record2[c2].Manager)
                    {
                        if (_record1[c1].LastModified != _record2[c2].LastModified)
                            checked_mark[c1] = PartialRecordState.UPDATED;
                        else
                            checked_mark[c1] = PartialRecordState.UNCHANGED;
                        break;
                    }
                }
                if (checked_mark[c1] == PartialRecordState.UNCHECKED)
                    checked_mark[c1] = PartialRecordState.NOT_FOUND;
            }

            if (checked_mark.Contains(PartialRecordState.NOT_FOUND))
                return PartialRecordState.STRUCTURE_CHANGED;
            else if (checked_mark.Contains(PartialRecordState.UPDATED))
                return PartialRecordState.UPDATED;
            else
                return PartialRecordState.UNCHANGED;
        }

        #endregion

    }

    public enum ProjectMergeResult
    {
        UNKNOWN = 0,
        OK = 1,
        PROJECT_FILE_NAME_MISMATCH = 2,
        SINGLE_FILE_NAME_MISMATCH = 3,
        IO_ERROR = 4,
        NULL_INPUT_ERROR = 5,
    }

    #endregion

    public class DXFDistributedDecoder
    {
        #region STATIC

        private static void FinalizeFileContent(ref StringBuilder _content)
        {
            if (_content == null) return;

            _content.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString()); // 0
            _content.AppendLine(ParamStructTypes.EOF);                                     // end of file
        }

        internal static string ExtractRelativeNameFromAbsolute(string _absolute_file_path)
        {
            if (string.IsNullOrEmpty(_absolute_file_path)) return string.Empty;

            int ind_slash = _absolute_file_path.IndexOf('/');
            int ind_backSlash = _absolute_file_path.IndexOf('\\');
            string[] path_parts;
            if (ind_slash > -1)
                path_parts = _absolute_file_path.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            else if (ind_backSlash > -1)
                path_parts = _absolute_file_path.Split(new char[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
            else
                path_parts = new string[] { _absolute_file_path };

            int nr_parts = path_parts.Length;
            return path_parts[nr_parts - 1];
        }

        internal static string ReconstructAbsolutePathFromRelative(string _summary_file_path, string _relative_single_file_path)
        {
            if (string.IsNullOrEmpty(_summary_file_path)) return _relative_single_file_path;
            if (string.IsNullOrEmpty(_relative_single_file_path)) return _summary_file_path;

            int last_ind_slash = _summary_file_path.LastIndexOf('/');
            int last_ind_backSlash = _summary_file_path.LastIndexOf('\\');

            string path = string.Empty;
            string abs_path = _relative_single_file_path;
            if (last_ind_slash > -1)
            {
                path = _summary_file_path.Substring(0, last_ind_slash);
                abs_path = path + '/' + _relative_single_file_path;
            }
            else if (last_ind_backSlash > -1)
            {
                path = _summary_file_path.Substring(0, last_ind_backSlash);
                abs_path = path + '\\' + _relative_single_file_path;
            }

            return abs_path;
        }

        #endregion

        #region CLASS MEMBERS

        // parsing
        public StreamReader FStream { get; private set; }
        public string FValue { get; private set; }
        public int FCode { get; private set; }

        // component management
        public MultiValueFactory MV_Factory { get; private set; }
        public ParameterFactory P_Factory { get; private set; }
        public ComponentFactory COMP_Factory { get; private set; }

        // file management
        private PartialFileRecord current_file_record;
        private List<PartialFileRecord> loaded_file_records;
        public string SummaryFileName { get; private set; }
        public List<string> SingleFileNames { get; private set; }
        private ComponentManagerType manager;
        public bool FileRecordsOpen { get; private set; }
        
        // update notification
        private FileSystemWatcher FWatcher;
        private List<int> nr_locks_record;
        private bool write_in_progress;

        #endregion

        #region .CTOR

        public DXFDistributedDecoder(MultiValueFactory _mv_factory, ParameterFactory _p_factory, ComponentFactory _comp_factory,
                                    string _file_name, ComponentManagerType _user)
        {
            this.MV_Factory = _mv_factory;
            this.P_Factory = _p_factory;
            this.COMP_Factory = _comp_factory;

            this.SetFileManagementContent(_file_name, _user);
            this.nr_locks_record = new List<int>(ComponentUtils.MANAGER_TYPE_OPENING_SIGNATURE_NONE);
        }

        private void SetFileManagementContent(string _file_name, ComponentManagerType _user)
        {
            this.current_file_record = null;
            this.loaded_file_records = new List<PartialFileRecord>();
            this.SummaryFileName = _file_name;
            this.SingleFileNames = new List<string>();
            this.manager = _user;
            this.FileRecordsOpen = false;
            this.write_in_progress = false;
        }

        #endregion

        #region METHODS: Writing / Modifying files

        // writing the summary file (smn: SIMULTAN)
        // ----------------------------------------------------------------------
        // code 0  : ENTITY_START
        // value   : file name (e.g. architecture: 'ComponentRecord_01ARC.dxf')
        // code 904: ENTITY_KEY
        // value   : role that can open the file with writing access (e.g. administrator: '@') 
        // code 902: TIME_STAMP
        // value   : last modified
        // code 910: X_VALUE
        // value   : 1 if locked, 0 if unlocked (this is set during file open, according to role)

        // only the files corsponding to the manager role can be replaced
        // NO !!! (added 01.09.2016)
        // all files can be replaced: user w/o writing access can still have supervizing or release access
        public void SaveFiles(bool _unlock_the_summary_file = false)
        {
            if (this.loaded_file_records.Count == 0) return;
            if (string.IsNullOrEmpty(this.SummaryFileName)) return;
            if (this.SummaryFileName.Length < 5) return;

            StringBuilder sb = new StringBuilder();
            string filename_wo_ext = this.SummaryFileName.Substring(0, this.SummaryFileName.Length - ParamStructFileExtensions.FILE_EXT_PROJECT.Length - 1);

            // create the export stringS
            Dictionary<ComponentManagerType, StringBuilder> exports = this.COMP_Factory.ExportRecordDistributed();
            // prepare for checking the state of the single files (init w ZERO)
            List<int> single_file_state = new List<int>(ComponentUtils.MANAGER_TYPE_OPENING_SIGNATURE_NONE);
            
            // save the file records
            foreach (var entry in exports)
            {
                // changed 01.09.2016 to allow for release and supervize actions to be recorded
                //if (DXFDistributedDecoder.HasEditingRights(this.manager, entry.Key))
                //{
                    // check if the locks are set correctly, if a new single file is to be created (added 07.02.2017)
                    if (DXFDistributedDecoder.HasEditingRights(this.manager, entry.Key) && this.nr_locks_record[(int)entry.Key] == 0)
                    {
                        this.nr_locks_record[(int)entry.Key] = 1;
                    }
                    // save file
                    PartialFileRecord record = this.WriteSingleFile(filename_wo_ext, this.nr_locks_record[(int)entry.Key], entry.Key, entry.Value);
                    single_file_state[(int)entry.Key] = 1;
                    // replace record, if it exists; otherwise just add
                    PartialFileRecord pfr = this.loaded_file_records.Find(x => x.Manager == entry.Key);
                    if (pfr != null)
                        this.loaded_file_records.Remove(pfr);                        
                    this.loaded_file_records.Add(record);
                //}
            }

            // clean up old file records (e.g. when the writing rights of components change a record may become obsolete)
            for (int c = 0; c < single_file_state.Count; c++)
            {
                if (single_file_state[c] > 0) continue;

                // get rid of the record and of the file
                PartialFileRecord pfr = this.loaded_file_records.Find(x => x.Manager == (ComponentManagerType)c);
                if (pfr != null)
                {
                    this.loaded_file_records.Remove(pfr);
                    this.DeleteSingleFile(filename_wo_ext, (ComponentManagerType)c);
                }
            }

            // ... modify the summary file accordingly
            this.SingleFileNames = new List<string>();
            foreach (PartialFileRecord fr in this.loaded_file_records)
            {
                if (_unlock_the_summary_file)
                    fr.NrLocks = 0;
                fr.AddToExport(ref sb);
                this.SingleFileNames.Add(fr.FileName);
            }
            this.WriteSummaryFile(this.SummaryFileName, ref sb);
        }


        // any role can do this, i.e. save the project under a new name
        public void SaveFilesAs(string _filename_w_ext)
        {
            if (string.IsNullOrEmpty(_filename_w_ext) || this.COMP_Factory == null) return;
            if (_filename_w_ext.Length < 5) return;

            StringBuilder sb = new StringBuilder();
            string filename_wo_ext = _filename_w_ext.Substring(0, _filename_w_ext.Length - ParamStructFileExtensions.FILE_EXT_PROJECT.Length - 1);

            // create the export stringS
            Dictionary<ComponentManagerType, StringBuilder> exports = this.COMP_Factory.ExportRecordDistributed();
            this.SingleFileNames = new List<string>();
            foreach (var entry in exports)
            {
                // export to a separate file
                int nr_locks = 0;
                PartialFileRecord record = this.WriteSingleFile(filename_wo_ext, nr_locks, entry.Key, entry.Value);
                this.SingleFileNames.Add(record.FileName);
                record.AddToExport(ref sb);
            }

            // write the summary file
            this.WriteSummaryFile(_filename_w_ext, ref sb);
        }

        #endregion

        #region METHODS: Writing single files

        private PartialFileRecord WriteSingleFile(string _filename_wo_ext, int _nr_locks, ComponentManagerType _user, StringBuilder _sb)
        {
            string content = _sb.ToString();
            string filename_part = _filename_wo_ext + "_" +
                                   ComponentUtils.ComponentManagerTypeToAbbrevEN(_user) + "." +
                                   ParamStructFileExtensions.FILE_EXT_COMPONENTS;
            try
            {
                using (FileStream fs = File.Create(filename_part))
                {
                    byte[] content_B = System.Text.Encoding.UTF8.GetBytes(content);
                    fs.Write(content_B, 0, content_B.Length);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error saving partial file: " + filename_part,
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }

            PartialFileRecord record = new PartialFileRecord()
            {
                FileName = filename_part,
                Manager = _user,
                LastModified = DateTime.Now,
                NrLocks = _nr_locks
            };

            return record;
        }

        // added 07.02.2017
        private bool DeleteSingleFile(string _filename_wo_ext, ComponentManagerType _user)
        {
            string filename_part = _filename_wo_ext + "_" +
                                   ComponentUtils.ComponentManagerTypeToAbbrevEN(_user) + "." +
                                   ParamStructFileExtensions.FILE_EXT_COMPONENTS;
            try
            {
                if (File.Exists(filename_part))
                {
                    File.Delete(filename_part);
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error deleting partial file: " + filename_part,
                                MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private void WriteSummaryFile(string _filename_w_ext, ref StringBuilder _sb)
        {
            if (_sb == null) return;
            if (string.IsNullOrEmpty(_filename_w_ext)) return;
            DXFDistributedDecoder.FinalizeFileContent(ref _sb);

            string content_summary = _sb.ToString();
            try
            {
                using (FileStream fs = File.Create(_filename_w_ext))
                {
                    byte[] content_B = System.Text.Encoding.UTF8.GetBytes(content_summary);
                    fs.Write(content_B, 0, content_B.Length);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error saving simultan file: " + _filename_w_ext,
                                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                this.write_in_progress = true;
            }
        }

        #endregion

        #region METHODS: Loading Summary File (and set a Watcher)

        public void LoadFiles(bool _use_a_watcher = true)
        {
            if (!File.Exists(this.SummaryFileName)) return;

            try
            {
                // read and parse file
                this.FStream = new StreamReader(this.SummaryFileName);
                bool reached_eof = false;
                while (this.HasNext())
                {
                    this.Next();
                    if (this.FValue == ParamStructTypes.EOF)
                    {
                        reached_eof = true;
                        if (this.current_file_record != null)
                            this.loaded_file_records.Add(this.current_file_record);
                        this.ReleaseRessources();
                        break;
                    }
                    this.ParseSummaryFile();
                }
                if (!reached_eof)
                    this.ReleaseRessources();

                // load components from the files according to user role and ...
                StringBuilder sb_new = new StringBuilder();
                DXFDecoder dxf_decoder = new DXFDecoder(this.MV_Factory, this.P_Factory, this.COMP_Factory);

                foreach (PartialFileRecord fr in this.loaded_file_records)
                {
                    if (File.Exists(fr.FileName))
                    {
                        // adjust the locks on the file (06.02.2017)
                        bool lock_edit = true;
                        this.nr_locks_record[(int)fr.Manager] = fr.NrLocks;
                        if (DXFDistributedDecoder.HasEditingRights(this.manager, fr.Manager))
                        {
                            lock_edit = (fr.NrLocks > 0);
                            this.nr_locks_record[(int)fr.Manager] = fr.NrLocks + 1;
                            fr.NrLocks++;
                        }
                        
                        dxf_decoder.LoadFromFile(fr.FileName, lock_edit);                                
                    }
                    fr.AddToExport(ref sb_new);
                }
                dxf_decoder.DoDeferredOperations(); // connects components and networks saved in different partial files
                this.COMP_Factory.RestoreReferencesWithinRecord();
                this.COMP_Factory.MakeParameterOutsideBoundsVisible();
                
                // ... modify the summary file accordingly
                this.WriteSummaryFile(this.SummaryFileName, ref sb_new);
                this.FileRecordsOpen = true;

                // ... set a watcher on the file
                if (_use_a_watcher)
                    this.SetWatcher();

            }
            catch(Exception ex)
            {
                this.ReleaseRessources();
                MessageBox.Show(ex.Message, "Error reading simultan file: " + this.SummaryFileName,
                                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public bool CanExecute_LoadFiles()
        {
            bool canDo = true;
            canDo &= (!string.IsNullOrEmpty(this.SummaryFileName));
            canDo &= this.SummaryFileName.Length >= 5;
            canDo &= !this.FileRecordsOpen;
            canDo &= this.loaded_file_records.Count == 0;

            return canDo;
        }

        private void ParseSummaryFile()
        {
            switch(this.FCode)
            {
                case (int)ParamStructCommonSaveCode.ENTITY_START:
                    if (this.current_file_record != null)
                        this.loaded_file_records.Add(this.current_file_record);
                    this.current_file_record = new PartialFileRecord();
                    this.current_file_record.FileName = DXFDistributedDecoder.ReconstructAbsolutePathFromRelative(this.SummaryFileName, this.FValue);
                    break;
                case (int)ParamStructCommonSaveCode.ENTITY_KEY:
                    if (this.current_file_record != null)
                        this.current_file_record.Manager = ComponentUtils.StringToComponentManagerType(this.FValue);
                    break;
                case (int)ParamStructCommonSaveCode.TIME_STAMP:
                    if (this.current_file_record != null)
                    {
                        DateTime dt_tmp;
                        bool success = DateTime.TryParse(this.FValue, ParamStructTypes.DT_FORMATTER, System.Globalization.DateTimeStyles.None, out dt_tmp);
                        if (success)
                            this.current_file_record.LastModified = dt_tmp;
                    }
                    break;
                case (int)ParamStructCommonSaveCode.X_VALUE:
                    if (this.current_file_record != null)
                    {
                        int nr_locks;
                        bool success = Int32.TryParse(this.FValue, out nr_locks);
                        this.current_file_record.NrLocks = nr_locks;
                    }
                    break;
            }
        }

        #endregion

        #region METHODS: Closing Summary File (i.e. Project)

        public void ReleaseFiles(bool _save_changes)
        {
            if (_save_changes)
            {
                // modifies this.loaded_file_records
                this.SaveFiles();
            }
            
            // release locks on files
            StringBuilder sb_new = new StringBuilder();
            foreach (PartialFileRecord fr in this.loaded_file_records)
            {
                if (DXFDistributedDecoder.HasEditingRights(this.manager, fr.Manager))
                    fr.NrLocks--;
                fr.AddToExport(ref sb_new);
            }

            // ... release the watcher
            this.ReleaseWatcher();

            // ... modify the summary file accordingly
            this.WriteSummaryFile(this.SummaryFileName, ref sb_new);

            // reset internal state
            this.SetFileManagementContent(null, ComponentManagerType.GUEST);
            this.COMP_Factory.ClearRecord();
        }

        public bool CanExecute_ReleaseFiles()
        {
            bool canDo = true;
            canDo &= (!string.IsNullOrEmpty(this.SummaryFileName));
            canDo &= this.SummaryFileName.Length >= 5;
            canDo &= this.loaded_file_records.Count > 0;
            
            return canDo;
        }

        #endregion

        #region METHODS: Reparing Summary File after Error (e.g. software crash)

        public void RepareSummaryFile()
        {
            if (this.manager != ComponentManagerType.ADMINISTRATOR) return;
            this.InternalLockReset();
        }

        private void InternalLockReset()
        {
            if (!File.Exists(this.SummaryFileName)) return;

            try
            {
                // read and parse file
                this.FStream = new StreamReader(this.SummaryFileName);
                bool reached_eof = false;
                while (this.HasNext())
                {
                    this.Next();
                    if (this.FValue == ParamStructTypes.EOF)
                    {
                        reached_eof = true;
                        if (this.current_file_record != null)
                            this.loaded_file_records.Add(this.current_file_record);
                        this.ReleaseRessources();
                        break;
                    }
                    this.ParseSummaryFile();
                }
                if (!reached_eof)
                    this.ReleaseRessources();

                // unlock all records
                StringBuilder sb_new = new StringBuilder();
                foreach (PartialFileRecord fr in this.loaded_file_records)
                {
                    fr.NrLocks = 0;
                    fr.AddToExport(ref sb_new);
                }
                // ... modify the summary file accordingly
                this.WriteSummaryFile(this.SummaryFileName, ref sb_new);
            }
            catch (Exception ex)
            {
                this.ReleaseRessources();
                MessageBox.Show(ex.Message, "Error reading simultan file: " + this.SummaryFileName,
                                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region METHODS: Merging summary files

        private bool LoadSummaryFileOnly()
        {
            if (!File.Exists(this.SummaryFileName)) return false;

            if (this.loaded_file_records.Count > 0) return true; // already loaded

            try
            {
                // read and parse file
                this.FStream = new StreamReader(this.SummaryFileName);
                bool reached_eof = false;
                while (this.HasNext())
                {
                    this.Next();
                    if (this.FValue == ParamStructTypes.EOF)
                    {
                        reached_eof = true;
                        if (this.current_file_record != null)
                            this.loaded_file_records.Add(this.current_file_record);
                        this.ReleaseRessources();
                        break;
                    }
                    this.ParseSummaryFile();
                }
                if (!reached_eof)
                    this.ReleaseRessources();

                return true;
            }
            catch (Exception ex)
            {
                this.ReleaseRessources();
                MessageBox.Show(ex.Message, "Error reading simultan file: " + this.SummaryFileName,
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public List<string> GetSingleFilePaths()
        {
            this.LoadSummaryFileOnly();

            if (this.loaded_file_records == null || this.loaded_file_records.Count == 0) return new List<string>();

            return this.loaded_file_records.Select(x => x.FileName).ToList();
        }

        /// <summary>
        /// Attempts to merge two summary files. It compares the file records they contain by NAME only.
        /// Assumes that if a file is in RECORD 1 (own) but not in the RECORD 2 (from other source) and its manager is the current user of RECORD 1, it should be kept.
        /// If a file is in RECORD 1 (own) but not in the RECORD 2 (from other source) and its manager is NOT the current user of RECORD 1, the file should be deleted. This means 
        /// that references or mappings to components in it made from components in other files will be lost.
        /// </summary>
        /// <param name="_dec_1_to_override"></param>
        /// <param name="_dec_2"></param>
        /// <returns></returns>
        public static ProjectMergeResult MergeSummaryFiles(DXFDistributedDecoder _dec_1_to_override, DXFDistributedDecoder _dec_2)
        {
            if (_dec_1_to_override == null || _dec_2 == null) return ProjectMergeResult.NULL_INPUT_ERROR;

            bool ok_1 = _dec_1_to_override.LoadSummaryFileOnly();
            bool ok_2 = _dec_2.LoadSummaryFileOnly();

            if (!ok_1 || !ok_2) return ProjectMergeResult.IO_ERROR;
            
            if (!(Utils.StringUtils.UnQualifiedFileNamesEqual(_dec_1_to_override.SummaryFileName, _dec_2.SummaryFileName)))
                return ProjectMergeResult.PROJECT_FILE_NAME_MISMATCH;

            // sort the file records
            SortedDictionary<ComponentManagerType, PartialFileRecord> sorted_records_1 = new SortedDictionary<ComponentManagerType, PartialFileRecord>();
            foreach (PartialFileRecord pfr in _dec_1_to_override.loaded_file_records)
            {
                sorted_records_1.Add(pfr.Manager, pfr);
            }

            SortedDictionary<ComponentManagerType, PartialFileRecord> sorted_records_2 = new SortedDictionary<ComponentManagerType, PartialFileRecord>();
            foreach (PartialFileRecord pfr in _dec_2.loaded_file_records)
            {
                sorted_records_2.Add(pfr.Manager, pfr);
            }

            // compare the file records
            List<PartialFileRecord> merged_records = new List<PartialFileRecord>();

            for (int i = 0; i < Enum.GetNames(typeof(ComponentManagerType)).Length; i++ )
            {
                ComponentManagerType m = (ComponentManagerType)i;
                if (sorted_records_1.ContainsKey(m) && !sorted_records_2.ContainsKey(m))
                {
                    if (m != _dec_1_to_override.manager)
                    {
                        // someone else deleted this record -> so skip it
                    }
                    else
                    {
                        // possibly I created this record -> so keep it
                        merged_records.Add(sorted_records_1[m]);
                    }                   
                }
                else if (!sorted_records_1.ContainsKey(m) && sorted_records_2.ContainsKey(m))
                {
                    if (m != _dec_1_to_override.manager)
                    {
                        // someone else created this record -> so KEEP it
                        merged_records.Add(sorted_records_2[m]);
                    }
                    else
                    {
                        // possibly I deleted this record -> so SKIP it
                    }   
                }
                else if (sorted_records_1.ContainsKey(m) && sorted_records_2.ContainsKey(m))
                {
                    if (!(Utils.StringUtils.UnQualifiedFileNamesEqual(sorted_records_1[m].FileName, sorted_records_2[m].FileName)))
                        return ProjectMergeResult.SINGLE_FILE_NAME_MISMATCH;
                    // take own record
                    merged_records.Add(sorted_records_1[m]);
                }
            }

            // re-write the summary file
            _dec_1_to_override.loaded_file_records = merged_records;
            StringBuilder sb_new = new StringBuilder();
            foreach (PartialFileRecord fr in _dec_1_to_override.loaded_file_records)
            {
                // fr.NrLocks = (DXFDistributedDecoder.HasEditingRights(_dec_1_to_override.manager, fr.Manager)) ? 1 : 0;
                fr.NrLocks = 0;
                fr.AddToExport(ref sb_new);
            }
            _dec_1_to_override.WriteSummaryFile(_dec_1_to_override.SummaryFileName, ref sb_new);

            return ProjectMergeResult.OK;
        }


        #endregion

        #region UTILS: Local info

        private static bool HasEditingRights(ComponentManagerType _user, ComponentManagerType _file_manager)
        {
            return (_file_manager == ComponentManagerType.GUEST ||
                    _user == ComponentManagerType.ADMINISTRATOR ||
                    _file_manager == _user);
        }


        #endregion

        #region UTILS: String

        private static string GetUnQualifiedFileName(string _file_name)
        {
            if (_file_name == null) return null;

            int ind_slash = _file_name.LastIndexOf("/");
            int ind_backslash = _file_name.LastIndexOf("\\");
            int ind = Math.Max(ind_slash, ind_backslash);

            if (ind < 0) return string.Empty;

            return _file_name.Substring(ind + 1, _file_name.Length - ind - 1);
        }


        #endregion

        #region UTILS: Reading summary files

        // processes 2 lines: 
        // 1. the line containing the DXF CODE
        // 2. the line containing the INFORMATION saved under said code
        public void Next()
        {
            int code;
            bool success = Int32.TryParse(this.FStream.ReadLine(), out code);
            if (success)
                this.FCode = code;
            else
                this.FCode = (int)ParamStructCommonSaveCode.INVALID_CODE;

            if (this.HasNext())
                this.FValue = this.FStream.ReadLine();
        }

        public bool HasNext()
        {
            if (this.FStream == null) return false;
            if (this.FStream.Peek() < 0) return false;
            return true;
        }

        public void ReleaseRessources()
        {
            if (this.FStream != null)
            {
                this.FStream.Close();
                try
                {
                    FStream.Dispose();
                }
                catch (Exception ex)
                {
                    string message = ex.Message;
                }
            }
        }
        #endregion

        #region UTILS: Watching Summary Files

        private void SetWatcher()
        {
            // extract the directory path
            string dir = this.SummaryFileName;
            int last_bslash_ind = dir.LastIndexOf("\\");
            int last_slash_ind = dir.LastIndexOf("/");
            int up_to_ind = Math.Max(last_bslash_ind, last_slash_ind);
            dir = dir.Substring(0, up_to_ind);

            this.FWatcher = new FileSystemWatcher();
            this.FWatcher.Path = dir;
            this.FWatcher.NotifyFilter = NotifyFilters.LastWrite;
            this.FWatcher.Filter = "*.smn";
            this.FWatcher.Changed += OnSummaryFileChanged;
            this.FWatcher.EnableRaisingEvents = true;
        }

        public void ReleaseWatcher()
        {
            if (this.FWatcher == null) return;

            this.FWatcher.Changed -= OnSummaryFileChanged;
            this.FWatcher.EnableRaisingEvents = false;
            this.FWatcher = null;
        }

        private void OnSummaryFileChanged(object sender, FileSystemEventArgs e)
        {
            // reload the summary file to update the number of locks
            if (this.SummaryFileName == null) return;
            if (!File.Exists(this.SummaryFileName)) return;
            string uqfn = DXFDistributedDecoder.GetUnQualifiedFileName(this.SummaryFileName);

            // prepare to calculate the difference btw the previous and current state of the summary file
            List<PartialFileRecord> prev_loaded_file_records = new List<PartialFileRecord>(this.loaded_file_records);

            // reset
            this.current_file_record = null;
            this.loaded_file_records = new List<PartialFileRecord>();

            // debug
            string prefix = "[*]";
#if DEBUG
            prefix = "[D]";
#else
            prefix = "[R]";
#endif

            // re-load
            try
            {
                // read and parse file
                using (FileStream fs = new FileStream(this.SummaryFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    this.FStream = new StreamReader(fs);
                    bool reached_eof = false;
                    while (this.HasNext())
                    {
                        this.Next();
                        if (this.FValue == ParamStructTypes.EOF)
                        {
                            reached_eof = true;
                            if (this.current_file_record != null)
                                this.loaded_file_records.Add(this.current_file_record);
                            this.ReleaseRessources();
                            break;
                        }
                        this.ParseSummaryFile();
                    }
                    if (!reached_eof)
                        this.ReleaseRessources();
                }

                foreach (PartialFileRecord fr in this.loaded_file_records)
                {
                    if (File.Exists(fr.FileName))
                    {                       
                        this.nr_locks_record[(int)fr.Manager] = fr.NrLocks;
                    }
                }

                // calculate difference
                if (this.write_in_progress)
                    this.write_in_progress = false;
                else
                {
                    PartialRecordState diff = PartialFileRecord.CompareRecordStates(prev_loaded_file_records, this.loaded_file_records);
                    if (diff == PartialRecordState.UPDATED)
                        MessageBox.Show("Another user updated the project!", prefix + "Project File State: " + uqfn,
                                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    else if (diff == PartialRecordState.STRUCTURE_CHANGED)
                        MessageBox.Show("Another user changed the structure of the project!", prefix + "Project File State: " + uqfn,
                                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch(System.IO.IOException ioex)
            {
                // happens when another process is using the file (e.g. GIT repo copying process)
                // do not user a watcher when saving with GIT - not necessary
                this.ReleaseWatcher();
                string io_tmp = ioex.Message;
            }
            catch (Exception ex)
            {
                this.ReleaseRessources();
                string tmp = ex.Message;
                //MessageBox.Show(ex.Message, prefix + "Error re-reading simultan file: " + uqfn,
                //                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
}
