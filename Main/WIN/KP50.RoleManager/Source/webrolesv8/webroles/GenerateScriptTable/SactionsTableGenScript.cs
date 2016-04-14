using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Npgsql;

namespace webroles.GenerateScriptTable
{
    class SactionsTableGenScript : GenerateScript
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
                    var chanRows = ChangedRowCollection.Select(row => row).Where(row => row.TableName == Tables.s_actions);
                    var listchanRows = chanRows as IList<ChangedRow> ?? chanRows.ToList();
                    if (listchanRows.Count() != 0)
                    {
                        generateToChangeScript(listchanRows, Tables.s_actions);
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
            var commandText =
               "SELECT DISTINCT sa.nzp_act, act_name, hlp FROM " + Tables.s_actions + " sa, " + Tables.s_actions_intermed + " sai " +
               "WHERE sa.nzp_act=sai.nzp_act ORDER BY sa.nzp_act;";
            DataTable = new DataTable();
            Adapter = new NpgsqlDataAdapter(commandText, Connect);
            Adapter.Fill(DataTable);
        }


        private void generateToFullScript()
        {
            var comList = (from DataRow dr in DataTable.Rows
                           select "INSERT INTO " + Tables.s_actions + " (nzp_act, act_name, hlp) VALUES (" +
                                  (dr.Field<int?>("nzp_act").HasValue ? dr.Field<int?>("nzp_act").ToString() : "null") + ", " +
                                  (!String.IsNullOrEmpty(dr.Field<string>("act_name")) ? "'" + dr.Field<string>("act_name") + "'" : "''") + ", " +
                                  (!String.IsNullOrEmpty(dr.Field<string>("hlp")) ? "'" + dr.Field<string>("hlp") + "'" : "''") + ");").ToList();
            comList.Insert(0, "--Заполнение таблицы " + Tables.s_actions);
            comList.Add(string.Empty);
            AddToLinesList(comList);
        }
    }
}
