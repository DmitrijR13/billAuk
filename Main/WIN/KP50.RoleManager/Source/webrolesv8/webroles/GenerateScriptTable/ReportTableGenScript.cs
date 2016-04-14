using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Npgsql;

namespace webroles.GenerateScriptTable
{
    class ReportTableGenScript : GenerateScript
    {



        public override void Request()
        {
            switch (TypeScr)
            {
                case TypeScript.ChangeDeveloper: break;
                case TypeScript.ChangeCustomer: break;
                case TypeScript.FullDeveloper:
                    requestToFullScript(); break;
                case TypeScript.FullCustomer:
                    requestToFullScript(); break;
            }
        }
        public override void GenerateScr()
        {
            switch (TypeScr)
            {
                case TypeScript.ChangeDeveloper: break;
                case TypeScript.ChangeCustomer: 
                    var chanRows = ChangedRowCollection.Select(row => row).Where(row => row.TableName == Tables.report);
                    var listchanRows = chanRows as IList<ChangedRow> ?? chanRows.ToList();
                    if (listchanRows.Count() != 0)
                    {
                        generateToChangeScript(listchanRows, Tables.report);
                    }
                    break;
                case TypeScript.FullDeveloper:
                    generateToFullScript(); break;
                case TypeScript.FullCustomer:
                    generateToFullScript(); break;
            }
        }

        private void requestToFullScript()
        {
            string commandText = "SELECT DISTINCT r.nzp_act, name, file_name FROM " + Tables.report + " r, " + Tables.s_actions_intermed + " sa WHERE r.nzp_act= sa.nzp_act ORDER BY r.nzp_act;";
            DataTable = new DataTable();
            Adapter = new NpgsqlDataAdapter(commandText, Connect);
            Adapter.Fill(DataTable);  
        }

        private void generateToFullScript()
        {
            var comList = (from DataRow dr in DataTable.Rows
                           select "INSERT INTO " + Tables.report + " (nzp_act, name, file_name) VALUES (" +
                                  (dr.Field<int?>("nzp_act").HasValue ? dr.Field<int?>("nzp_act").ToString() : "null") + ", " +
                                  (!String.IsNullOrEmpty(dr.Field<string>("name")) ? "'" + dr.Field<string>("name") + "'" : "''") + ", " +
                                  (!String.IsNullOrEmpty(dr.Field<string>("file_name")) ? "'" + dr.Field<string>("file_name") + "'" : "''") + ");").ToList();
            comList.Insert(0, "--Заполнение таблицы " + Tables.report);
            comList.Add(string.Empty);
            AddToLinesList(comList);
        }
    }
}
