using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.OleDb;


namespace EXCELImportExport
{
    public class ExcelExporter
    {
        private string sConnection;
        private OleDbConnection oleExcelConnection;
        private OleDbCommand oleExcelCommand;

        public ExcelExporter()
        {            
            this.oleExcelConnection = default(OleDbConnection);
            this.oleExcelCommand = default(OleDbCommand);
        }

        public void ExportToFile(string _file_path, string _table_name, List<string> _headers, List<List<string>> _data)
        {
            if (_file_path == null || _table_name == null || _headers == null || _data == null)
                return;

            int nrCol = _headers.Count;
            if (nrCol < 1)
                return;

            this.sConnection = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" +
                               _file_path +
                               ";Mode=ReadWrite;Extended Properties=\"Excel 12.0;HDR=No;IMEX=0\"";
            this.oleExcelConnection = new OleDbConnection(sConnection);
            this.oleExcelConnection.Open();

            this.oleExcelCommand = new OleDbCommand();
            this.oleExcelCommand.Connection = this.oleExcelConnection;

            // create table headers
            string create_table = "CREATE TABLE [" + _table_name + "] (";
            string insert_data = "INSERT INTO [" + _table_name + "] (";
            foreach(string col in _headers)
            {
                create_table += col + " VARCHAR,";
                insert_data += col + ",";
            }
            create_table = create_table.Substring(0, create_table.Length - 1);
            create_table += ");";
            insert_data = insert_data.Substring(0, insert_data.Length - 1);
            insert_data += ") VALUES(";
            this.oleExcelCommand.CommandText = create_table;
            this.oleExcelCommand.ExecuteNonQuery();

            // write data                
            foreach(List<string> data_row in _data)
            {
                if (data_row.Count != nrCol) continue;

                string insert_data_current = insert_data;
                foreach(string entry in data_row)
                {
                    insert_data_current += "'" + entry + "',";
                }
                insert_data_current = insert_data_current.Substring(0, insert_data_current.Length - 1);
                insert_data_current += ");";

                this.oleExcelCommand.CommandText = insert_data_current;
                this.oleExcelCommand.ExecuteNonQuery();
            }

            // EXAMPLE: http://www.codeproject.com/Tips/705470/Read-and-Write-Excel-Documents-Using-OLEDB            
            //this.oleExcelCommand.CommandText = "CREATE TABLE [table1] (id INT, name VARCHAR, datecol DATE );";
            //this.oleExcelCommand.ExecuteNonQuery();

            //this.oleExcelCommand.CommandText = "INSERT INTO [table1](id,name,datecol) VALUES(1,'AAAA','2014-01-01');";
            //this.oleExcelCommand.ExecuteNonQuery();

            //this.oleExcelCommand.CommandText = "INSERT INTO [table1](id,name,datecol) VALUES(2, 'BBBB','2014-01-03');";
            //this.oleExcelCommand.ExecuteNonQuery();

            //this.oleExcelCommand.CommandText = "INSERT INTO [table1](id,name,datecol) VALUES(3, 'CCCC','2014-01-03');";
            //this.oleExcelCommand.ExecuteNonQuery();

            //this.oleExcelCommand.CommandText = "UPDATE [table1] SET name = 'DDDD' WHERE id = 3;";
            //this.oleExcelCommand.ExecuteNonQuery();

            this.oleExcelConnection.Close();

        }


    }
}
