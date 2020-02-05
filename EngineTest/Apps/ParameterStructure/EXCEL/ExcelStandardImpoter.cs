using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.OleDb;
using System.Globalization;

using ParameterStructure.Values;

namespace ParameterStructure.EXCEL
{
    public class ExcelStandardImporter
    {
        #region STATC

        public const int MAX_NR_TABLE_ENTRIES = 8763;
        public const int COL_OFFSET = 5;
        public const int MAX_NR_VALUE_COLUMNS = 10;
        public const int ROW_OFFSET = 3;
        public const string TABLE_NAME = "Tabelle1";

        private static readonly IFormatProvider FORMAT_NEUTRAL = new NumberFormatInfo();
        private static readonly IFormatProvider FORMAT_DE = CultureInfo.GetCultureInfo("de-DE");

        #endregion

        #region CLASS MEMBERS

        private string sSheetName;
        private string sConnection;
        private DataTable dtTablesList;
        private OleDbCommand oleExcelCommand;
        private OleDbDataReader oleExcelReader;
        private OleDbConnection oleExcelConnection;

        #endregion

        #region .CTOR
        public ExcelStandardImporter()
        {
            this.dtTablesList = default(DataTable);
            this.oleExcelCommand = default(OleDbCommand);
            this.oleExcelReader = default(OleDbDataReader);
            this.oleExcelConnection = default(OleDbConnection);
        }
        #endregion

        #region IMPORT: Big Table

        public void ImportBigTableFromFile(string _file_path, ref MultiValueFactory _factory, int _nr_data_rows = 0)
        {
            if (string.IsNullOrEmpty(_file_path) || _factory == null) return;
            int nr_rows_to_read = (_nr_data_rows == 0) ? ExcelStandardImporter.MAX_NR_TABLE_ENTRIES : _nr_data_rows + ExcelStandardImporter.ROW_OFFSET;


            List<List<string>> raw_record = this.ImportFromFile(_file_path, ExcelStandardImporter.TABLE_NAME,
                                                                            nr_rows_to_read);
            List<string> names, units;
            List<List<double>> values;
            ExcelStandardImporter.ParseData(raw_record, ExcelStandardImporter.MAX_NR_TABLE_ENTRIES, 
                out names, out units, out values);

            // get the table name
            string[] file_path_comps = _file_path.Split(new string[] { "\\", "/", "." }, StringSplitOptions.RemoveEmptyEntries);
            int nr_comps = file_path_comps.Length;
            string table_name = "table";
            if (nr_comps > 1)
                table_name = file_path_comps[nr_comps - 2];
            else if (nr_comps > 0)
                table_name = file_path_comps[0];

            _factory.CreateBigTable(table_name, names, units, values);
        }

        public void ImportBigTableWNamesFromFile(string _file_path, ref MultiValueFactory _factory, int _nr_data_rows = 0)
        {
            if (string.IsNullOrEmpty(_file_path) || _factory == null) return;
            int nr_rows_to_read = (_nr_data_rows == 0) ? ExcelStandardImporter.MAX_NR_TABLE_ENTRIES : _nr_data_rows + ExcelStandardImporter.ROW_OFFSET;

            List<List<string>> raw_record = this.ImportFromFile(_file_path, ExcelStandardImporter.TABLE_NAME,
                                                                            nr_rows_to_read);

            List<string> names, units;
            List<List<double>> values;
            List<string> row_names;
            ExcelStandardImporter.ParseDataNamedRows(raw_record, ExcelStandardImporter.MAX_NR_TABLE_ENTRIES,
                out names, out units, out values, out row_names);

            // get the table name
            string[] file_path_comps = _file_path.Split(new string[] { "\\", "/", "." }, StringSplitOptions.RemoveEmptyEntries);
            int nr_comps = file_path_comps.Length;
            string table_name = "table";
            if (nr_comps > 1)
                table_name = file_path_comps[nr_comps - 2];
            else if (nr_comps > 0)
                table_name = file_path_comps[0];

            _factory.CreateBigTable(table_name, names, units, values, row_names);
        }

        #endregion

        #region IMPORT: General

        internal List<List<string>> ImportFromFile(string _file_path, string _table_name, int _max_nr_rows)
        {
            if (_file_path == null || _table_name == null)
                return null;

            List<List<string>> fields = new List<List<string>>();

            this.sConnection = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" +
                               _file_path +
                               ";Extended Properties=\"Excel 12.0;HDR=No;IMEX=1\"";
            this.oleExcelConnection = new OleDbConnection(sConnection);
            this.oleExcelConnection.Open();

            this.dtTablesList = oleExcelConnection.GetSchema("Tables");
            int nrTRows = this.dtTablesList.Rows.Count;
            if (nrTRows > 0)
            {
                for (int r = 0; r < nrTRows; r++)
                {
                    this.sSheetName = this.dtTablesList.Rows[r]["TABLE_NAME"].ToString();
                    if (!string.IsNullOrEmpty(this.sSheetName) && string.Equals(this.sSheetName, _table_name + "$"))
                    {
                        this.oleExcelCommand = oleExcelConnection.CreateCommand();
                        this.oleExcelCommand.CommandText = "Select * From [" + sSheetName + "]";
                        this.oleExcelCommand.CommandType = CommandType.Text;
                        this.oleExcelReader = oleExcelCommand.ExecuteReader();
                        int nOutputRow = 0;

                        while (this.oleExcelReader.Read() && nOutputRow < _max_nr_rows)
                        {
                            int nrF = this.oleExcelReader.FieldCount;
                            List<string> row = new List<string>();
                            for (int i = 0; i < nrF; i++)
                            {
                                row.Add(this.oleExcelReader.GetValue(i).ToString());
                            }
                            fields.Add(row);
                            nOutputRow++;
                        }
                        this.oleExcelReader.Close();
                        break;
                    }
                }
            }
            this.dtTablesList.Clear();
            this.dtTablesList.Dispose();


            this.oleExcelConnection.Close();
            return fields;
        }

        #endregion

        #region INTERPRET DATA

        internal static void ParseData(List<List<string>> _excel_strings, int _nr_data_rows, 
                            out List<string> names, out List<string> units, out List<List<double>> values)
        {
            names = new List<string>();
            units = new List<string>();
            values = new List<List<double>>();

            if (_excel_strings == null || _excel_strings.Count < 3 || _nr_data_rows < 1) return;



            for (int i = 0; i < _excel_strings.Count && i < _nr_data_rows; i++ )
            {
                List<string> row = _excel_strings[i];
                if (row == null || row.Count != ExcelStandardImporter.MAX_NR_VALUE_COLUMNS + ExcelStandardImporter.COL_OFFSET) continue;
                if (i == 0)
                {
                    names = row.Skip(ExcelStandardImporter.COL_OFFSET).Where(x => !string.IsNullOrEmpty(x)).ToList();
                }
                else if (i == 1)
                {
                    units = row.Skip(ExcelStandardImporter.COL_OFFSET).Where(x => !string.IsNullOrEmpty(x)).ToList();
                }
                else if (i > 2)
                {
                    List<string> row_vals_str = row.Skip(ExcelStandardImporter.COL_OFFSET).Where(x => !string.IsNullOrEmpty(x)).ToList();
                    List<double> row_vals = new List<double>();
                    foreach(string val_candidate in row_vals_str)
                    {
                        double value = double.NaN;
                        bool success = false;
                        if (val_candidate.Contains('.'))
                            success = double.TryParse(val_candidate,
                                NumberStyles.Float, ExcelStandardImporter.FORMAT_NEUTRAL, out value);
                        else
                            success = double.TryParse(val_candidate, 
                                NumberStyles.Float, ExcelStandardImporter.FORMAT_DE, out value);

                        if (success)
                            row_vals.Add(value);
                    }
                    if (row_vals_str.Count > 0 && row_vals.Count == row_vals_str.Count)
                        values.Add(row_vals);
                }
            }
        }


        internal static void ParseDataNamedRows(List<List<string>> _excel_strings, int _nr_data_rows,
                            out List<string> names, out List<string> units, out List<List<double>> values, out List<string> row_names)
        {
            names = new List<string>();
            units = new List<string>();
            values = new List<List<double>>();
            row_names = new List<string>();

            if (_excel_strings == null || _excel_strings.Count < 3 || _nr_data_rows < 1) return;



            for (int i = 0; i < _excel_strings.Count && i < _nr_data_rows; i++)
            {
                List<string> row = _excel_strings[i];
                if (row == null || row.Count != ExcelStandardImporter.MAX_NR_VALUE_COLUMNS + ExcelStandardImporter.COL_OFFSET) continue;
                if (i == 0)
                {
                    names = row.Skip(ExcelStandardImporter.COL_OFFSET).Where(x => !string.IsNullOrEmpty(x)).ToList();
                }
                else if (i == 1)
                {
                    units = row.Skip(ExcelStandardImporter.COL_OFFSET).Where(x => !string.IsNullOrEmpty(x)).ToList();
                }
                else if (i > 2)
                {
                    List<string> row_str = row.Skip(ExcelStandardImporter.COL_OFFSET).Where(x => !string.IsNullOrEmpty(x)).ToList();
                    string row_name = (row_str.Count > 0) ? row_str[0] : "name";
                    List<string> row_vals_str = row_str.Skip(1).ToList();
                    List<double> row_vals = new List<double>();
                    foreach (string val_candidate in row_vals_str)
                    {
                        double value = double.NaN;
                        bool success = false;
                        if (val_candidate.Contains('.'))
                            success = double.TryParse(val_candidate,
                                NumberStyles.Float, ExcelStandardImporter.FORMAT_NEUTRAL, out value);
                        else
                            success = double.TryParse(val_candidate,
                                NumberStyles.Float, ExcelStandardImporter.FORMAT_DE, out value);

                        if (success)
                            row_vals.Add(value);
                    }
                    if (row_vals_str.Count > 0 && row_vals.Count == row_vals_str.Count)
                    {
                        values.Add(row_vals);
                        row_names.Add(row_name);
                    }
                }
            }
        }

        #endregion

    }
}
