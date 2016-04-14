using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Npgsql;

namespace webroles.GenerateScriptTable
{
    class ActionsShowTableGenScript : GenerateScript
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
                    var actionsShowRow = ChangedRowCollection.Select(row => row).Where(row => row.TableName == Tables.actions_show );
                    var listPagesRows = actionsShowRow as IList<ChangedRow> ?? actionsShowRow.ToList();
                    if (listPagesRows.Count() != 0)
                    {
                       
                        generateToChangeScript(listPagesRows, Tables.actions_show);
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
            string commandText =
   "SELECT DISTINCT nzp_ash, cur_page, a.nzp_act, act_tip, act_dd, sort_kod, a.sign FROM " + Tables.actions_show + " a, " + Tables.role_actions_intermed + " r " +
   "WHERE a.cur_page=r.nzp_page AND a.nzp_act in (r.nzp_act, 610, 611) ORDER BY cur_page, nzp_act ; ";
            DataTable = new DataTable();
            Adapter = new NpgsqlDataAdapter(commandText, Connect);
            Adapter.Fill(DataTable);
        }


        private void generateToFullScript()
        {
            var comList = (from DataRow dr in DataTable.Rows
                           select "INSERT INTO " + Tables.actions_show + " (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (" +
                                  (dr.Field<int?>("cur_page").HasValue ? dr.Field<int?>("cur_page").ToString() : "null") + ", " +
                                  (dr.Field<int?>("nzp_act").HasValue ? dr.Field<int?>("nzp_act").ToString() : "null") + ", " +
                                  (dr.Field<int?>("act_tip").HasValue ? dr.Field<int?>("act_tip").ToString() : "null") + ", " +
                                  (dr.Field<int?>("act_dd").HasValue ? dr.Field<int?>("act_dd").ToString() : "null") + ", " +
                                  (dr.Field<int?>("sort_kod").HasValue ? dr.Field<int?>("sort_kod").ToString() : "null") + ");").ToList();
            comList.Insert(0, "--Заполнение таблицы " + Tables.actions_show);
            comList.Add(string.Empty);
            AddToLinesList(comList);
        }

        private void generateChanges()
        {
            
        }
    }
}
