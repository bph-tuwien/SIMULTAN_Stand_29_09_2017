using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.OleDb;

namespace EXCELImportExport
{
    public class ExcelImporter
    {
        private string sSheetName;
        private string sConnection;
        private DataTable dtTablesList;
        private OleDbCommand oleExcelCommand;
        private OleDbDataReader oleExcelReader;
        private OleDbConnection oleExcelConnection;

        public ExcelImporter()
        {
            this.dtTablesList = default(DataTable);
            this.oleExcelCommand = default(OleDbCommand);
            this.oleExcelReader = default(OleDbDataReader);
            this.oleExcelConnection = default(OleDbConnection);
        }

        public List<List<string>> ImportFromFile(string _file_path, string _table_name, int _max_nr_rows)
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
    }
}
