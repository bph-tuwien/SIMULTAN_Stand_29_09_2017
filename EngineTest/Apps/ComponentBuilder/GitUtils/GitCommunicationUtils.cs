using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows;

using ComponentBuilder.WinUtils;

namespace ComponentBuilder.GitUtils
{
    public static class GitCommunicationUtils
    {
        #region GIT CONFIG HANDLING
        public static void ReadGitConfigFile(string _git_path_to_config_file, int _git_nr_required_entries, 
                                             out string _repo, out string _account_user_name, out string _account_password,
                                             out bool _git_repo_config_OK, out string _git_config_msg_long, out string _git_config_msg_short)
        {
            _repo = string.Empty;
            _account_user_name = string.Empty;
            _account_password = string.Empty;
            _git_repo_config_OK = false;
            _git_config_msg_long = string.Empty;
            _git_config_msg_short = string.Empty;

            // get current application path
            string path = AppDomain.CurrentDomain.BaseDirectory;
            // construct path of config file
            string[] path_comps = path.Split(new string[] { "\\", "/" }, StringSplitOptions.RemoveEmptyEntries);
            int nr_comps = path_comps.Length;
            if (nr_comps <= 3) return;

            string path_CONFIG = string.Empty;
            for (int i = 0; i < nr_comps - 3; i++)
            {
                path_CONFIG += path_comps[i] + "\\";
            }
            path_CONFIG += _git_path_to_config_file;

            string err_mesg = string.Empty;
            try
            {
                int nr_found_entries = 0;
                if (File.Exists(path_CONFIG))
                {
                    using (StreamReader fstream = new StreamReader(path_CONFIG))
                    {
                        while (fstream.Peek() >= 0)
                        {
                            string line = fstream.ReadLine();

                            if (string.IsNullOrEmpty(line))
                                continue;
                            string[] line_comps = line.Split(new string[] { "=" }, StringSplitOptions.RemoveEmptyEntries);
                            if (line_comps == null || line_comps.Length < 2)
                                continue;

                            string key = line_comps[0].Trim();
                            if (key == "repo")
                            {
                                _repo = line_comps[1].Trim();
                                nr_found_entries++;
                            }
                            else if (key == "account_user_name")
                            {
                                _account_user_name = line_comps[1].Trim();
                                nr_found_entries++;
                            }
                            else if (key == "account_password")
                            {
                                _account_password = line_comps[1].Trim();
                                nr_found_entries++;
                            }
                        }
                    }
                }

                // check if the repo path is valid
                bool repo_is_valid = false;
                if (!string.IsNullOrEmpty(_repo))
                {
                    bool dir_exists = Directory.Exists(_repo);
                    if (dir_exists)
                    {
                        string path_to_remote_config = Path.Combine(_repo, @".git", "config");
                        if (File.Exists(path_to_remote_config))
                        {
                            using (FileStream fs = new FileStream(path_to_remote_config, FileMode.Open, FileAccess.Read, FileShare.Read))
                            {
                                using (StreamReader sr = new StreamReader(fs))
                                {
                                    while (sr.Peek() > 0)
                                    {
                                        string config_line = sr.ReadLine();
                                        if (config_line.Contains(_account_user_name))
                                        {
                                            repo_is_valid = true;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }


                if (nr_found_entries == _git_nr_required_entries && repo_is_valid)
                    _git_repo_config_OK = true;
                else
                    _git_repo_config_OK = false;
            }
            catch (Exception ex)
            {
                _git_repo_config_OK = false;
                err_mesg = ex.Message;
            }
            finally
            {
                if (_git_repo_config_OK)
                {
                    _git_config_msg_long = "Using local GIT repository at: '" + _repo + "'.\nSettings in file: '" + _git_path_to_config_file + "'.";
                    _git_config_msg_short = "Configuration of GIT repository";
                }
                else
                {
                    _git_config_msg_long = err_mesg + "\nSaving to server not possible.\nSettings in file: '" + _git_path_to_config_file + "'.";
                    _git_config_msg_short = "Error loading configuration of GIT repository";
                }
            }

        }

        #endregion

        #region FILE COPYING (with similarity detection)

        /// <summary>
        /// <para>Seraches the file structure at '_source_path' and performs a match of the file name and the directory name to those in the list '_file_paths'.</para>
        /// <para>If a match is found, it attepts to copy the matched files.</para>
        /// </summary>
        /// <param name="_source_path"></param>
        /// <param name="_file_paths"></param>
        /// <param name="_error_msg"></param>
        public static void CopyFilesFromSource(string _source_path, List<string> _file_paths, out string _error_msg)
        {
            _error_msg = string.Empty;

            if (string.IsNullOrEmpty(_source_path)) return;
            if (_file_paths == null) return;
            if (_file_paths.Count == 0) return;

            try
            {
                string[] files_in_repo = Directory.GetFiles(_source_path, "*", SearchOption.AllDirectories);                
                if (files_in_repo == null || files_in_repo.Length < 1)
                    return;

                // copy EXPECTED files
                foreach (string path_to_target in _file_paths)
                {
                    // compare files
                    string target_dir = "";
                    string target_dir_last_comp = "";
                    string target_filename = "";
                    StringHandling.GetUnQualifiedFileName(path_to_target, out target_dir, out target_filename, out target_dir_last_comp);

                    // look for matching files in the source directory
                    string source_best_match = "";
                    int source_match_depth = 0;
                    for (int i = 0; i < files_in_repo.Length; i++ )
                    {
                        string source_path = files_in_repo[i];
                    
                        string source_dir = "";
                        string source_dir_last_comp = "";
                        string source_filename = "";
                        StringHandling.GetUnQualifiedFileName(source_path, out source_dir, out source_filename, out source_dir_last_comp);

                        if (source_filename == target_filename && source_match_depth < 1)
                        {
                            source_best_match = source_path;
                            source_match_depth = 1;
                        }
                        if (source_filename == target_filename && source_dir_last_comp == target_dir_last_comp && source_match_depth < 2)
                        {
                            source_best_match = source_path;
                            source_match_depth = 2;
                            break;
                        }
                    }

                    // copy the file from the source to the target directory
                    if (source_best_match.Length > 0)
                    {
                        File.Copy(source_best_match, path_to_target, true);
                    }
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error copying file from Repository", MessageBoxButton.OK, MessageBoxImage.Error);
                _error_msg = ex.Message;
            }
        }

        /// <summary>
        /// Deletes all files with a paticular extension from the target dir, the one which holds '_one_target_file_path'.
        /// It copies all files in '_source_file_paths_to_copy' to the same dir. If '_file_ext_to_copy' is not null or empty it
        /// also copies any other file with a matching extension it finds in the source dir to the target dir.
        /// </summary>
        /// <param name="_source_path"></param>
        /// <param name="_source_file_paths_to_copy"></param>
        /// <param name="_one_target_file_path"></param>
        /// <param name="_file_ext_to_delete"></param>
        /// <param name="_err_msg"></param>
        public static void ReplaceFilesAtTargetFromSource(string _source_path, List<string> _source_file_paths_to_copy,
                                                          string _one_target_file_path, string _file_ext_to_delete, List<string> _file_ext_to_copy, out string _err_msg)
        {
            _err_msg = string.Empty;

            if (string.IsNullOrEmpty(_source_path)) return;
            if (_source_file_paths_to_copy == null) return;
            if (_source_file_paths_to_copy.Count == 0) return;
            if (string.IsNullOrEmpty(_one_target_file_path)) return;

            try
            {
                string target_dir = "";
                string target_dir_last_comp = "";
                string target_filename = "";
                StringHandling.GetUnQualifiedFileName(_one_target_file_path, out target_dir, out target_filename, out target_dir_last_comp);

                // clean the target location
                if (!string.IsNullOrEmpty(_file_ext_to_delete))
                {
                    string[] files_in_target_to_delete = Directory.GetFiles(Path.Combine(target_dir, target_dir_last_comp), _file_ext_to_delete, SearchOption.AllDirectories);
                    foreach(string to_del_path in files_in_target_to_delete)
                    {
                        File.Delete(to_del_path);
                    }
                }

                // copy the files from source to target
                foreach(string to_copy_path in _source_file_paths_to_copy)
                {
                    string source_dir = "";
                    string source_dir_last_comp = "";
                    string source_filename = "";
                    StringHandling.GetUnQualifiedFileName(to_copy_path, out source_dir, out source_filename, out source_dir_last_comp);

                    File.Copy(to_copy_path, Path.Combine(target_dir, target_dir_last_comp, source_filename), true);
                }

                if (_file_ext_to_copy != null && _file_ext_to_copy.Count > 0)
                {
                    foreach(string ext in _file_ext_to_copy)
                    {
                        string[] found_files_in_source = Directory.GetFiles(_source_path, ext, SearchOption.AllDirectories);
                        if (found_files_in_source == null || found_files_in_source.Length < 1)
                            continue;

                        foreach (string source_path in found_files_in_source)
                        {
                            if (_source_file_paths_to_copy.Contains(source_path)) continue;

                            string source_dir = "";
                            string source_dir_last_comp = "";
                            string source_filename = "";
                            StringHandling.GetUnQualifiedFileName(source_path, out source_dir, out source_filename, out source_dir_last_comp);

                            File.Copy(source_path, Path.Combine(target_dir, target_dir_last_comp, source_filename), true);
                        }

                    }
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Error replacing files.", MessageBoxButton.OK, MessageBoxImage.Error);
                _err_msg = ex.Message;
            }
        }

        #endregion

        #region FILE MATCHING

        public static string FindBestMatchForFileAtSource(string _source_path, string _file_path_to_match, out string _error_msg)
        {
            _error_msg = string.Empty;

            if (string.IsNullOrEmpty(_file_path_to_match) || string.IsNullOrEmpty(_source_path))
                return null;

            try
            {
                string[] files_in_repo = Directory.GetFiles(_source_path, "*", SearchOption.AllDirectories);
                if (files_in_repo == null || files_in_repo.Length < 1)
                    return null;

                string target_dir = "";
                string target_dir_last_comp = "";
                string target_filename = "";
                StringHandling.GetUnQualifiedFileName(_file_path_to_match, out target_dir, out target_filename, out target_dir_last_comp);

                // look for matching files in the source directory
                string source_best_match = "";
                int source_match_depth = 0;
                foreach(string source_path in files_in_repo)
                {
                    string source_dir = "";
                    string source_dir_last_comp = "";
                    string source_filename = "";
                    StringHandling.GetUnQualifiedFileName(source_path, out source_dir, out source_filename, out source_dir_last_comp);

                    if (source_filename == target_filename && source_match_depth < 1)
                    {
                        source_best_match = source_path;
                        source_match_depth = 1;
                    }
                    if (source_filename == target_filename && source_dir_last_comp == target_dir_last_comp && source_match_depth < 2)
                    {
                        source_best_match = source_path;
                        source_match_depth = 2;
                        return source_best_match;
                    }
                }

                return null;

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error searching for files in Repository", MessageBoxButton.OK, MessageBoxImage.Error);
                _error_msg = ex.Message;
                return null;
            }
        }

        #endregion

        #region FILE SAVING AND COPYING

        public static void SaveAndCopyFileToLocalLocation(string _target_path_local, string _file_path_current, string _content)
        {
            using (FileStream fs = File.Create(_file_path_current))
            {
                byte[] content_B = System.Text.Encoding.UTF8.GetBytes(_content);
                fs.Write(content_B, 0, content_B.Length);
            }

            string dir = "";
            string dir_last_comp = "";
            string filename = "";
            StringHandling.GetUnQualifiedFileName(_file_path_current, out dir, out filename, out dir_last_comp);

            // copy file to target location
            string dir_MV_to_target = string.IsNullOrEmpty(dir_last_comp) ? _target_path_local : Path.Combine(_target_path_local, dir_last_comp);
            Directory.CreateDirectory(dir_MV_to_target);
            File.Copy(_file_path_current, Path.Combine(dir_MV_to_target, filename), true);
        }


        public static void CopyFilesToLocalLocation(string _target_path_local, List<string> _source_paths)
        {
            if (string.IsNullOrEmpty(_target_path_local)) return;
            if (_source_paths == null || _source_paths.Count == 0) return;

            foreach(string path in _source_paths)
            {
                if (!(File.Exists(path))) continue;

                string dir = "";
                string dir_last_comp = "";
                string filename = "";
                StringHandling.GetUnQualifiedFileName(path, out dir, out filename, out dir_last_comp);

                // copy file to target location
                string dir_MV_to_target = string.IsNullOrEmpty(dir_last_comp) ? _target_path_local : Path.Combine(_target_path_local, dir_last_comp);
                Directory.CreateDirectory(dir_MV_to_target);
                File.Copy(path, Path.Combine(dir_MV_to_target, filename), true);
            }
        }

        #endregion
    }
}
