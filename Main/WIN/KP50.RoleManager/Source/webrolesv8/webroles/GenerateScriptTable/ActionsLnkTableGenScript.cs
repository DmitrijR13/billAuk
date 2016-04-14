using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Npgsql;

namespace webroles.GenerateScriptTable
{
    class ActionsLnkTableGenScript: GenerateScript
    {
 

        public override void Request()
        {

            switch (TypeScr)
            {
                case TypeScript.ChangeDeveloper: break;
                case TypeScript.ChangeCustomer: 
                    
                    break;
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
                      var actionsLnkRow = ChangedRowCollection.Select(row => row).Where(row => row.TableName == Tables.actions_lnk );
                    var listPagesRows = actionsLnkRow as IList<ChangedRow> ?? actionsLnkRow.ToList();
                    if (listPagesRows.Count() != 0)
                    {

                        generateToChangeScript(listPagesRows, Tables.actions_lnk);
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
            string commandText = "WITH pageun1 AS (SELECT * FROM " + Tables.pages_intermed + " UNION SELECT *  FROM " + Tables.pages_intermed_rp + "), " +
                                     "pageun2 AS (SELECT * FROM " + Tables.pages_intermed + " UNION SELECT *  FROM " + Tables.pages_intermed_rp + ") " +
                                     "SELECT DISTINCT " + Tables.actions_lnk + ".cur_page, " + Tables.actions_lnk + ".nzp_act, page_url FROM " + Tables.actions_lnk + ", pageun1, pageun2, " + Tables.s_actions_intermed + " " +
                                     "WHERE " + Tables.actions_lnk + ".cur_page=pageun1.nzp_page AND " + Tables.actions_lnk + ".nzp_act=" + Tables.s_actions_intermed + ".nzp_act AND page_url=pageun2.nzp_page ORDER BY cur_page; ";
            DataTable = new DataTable();
            Adapter = new NpgsqlDataAdapter(commandText, Connect);
            Adapter.Fill(DataTable);
        }



        private void generateToFullScript()
        {
            var comList = (from DataRow dr in DataTable.Rows
                           select "INSERT INTO " + Tables.actions_lnk + " (cur_page, nzp_act, page_url) VALUES (" +
                                  (dr.Field<int?>("cur_page").HasValue ? dr.Field<int?>("cur_page").ToString() : "null") + ", " +
                                  (dr.Field<int?>("nzp_act").HasValue ? dr.Field<int?>("nzp_act").ToString() : "null") + ", " +
                                  (dr.Field<int?>("page_url").HasValue ? dr.Field<int?>("page_url").ToString() : "null") + ");").ToList();
            comList.Insert(0, "--Заполнение таблицы " + Tables.actions_lnk);
            comList.Add(string.Empty);
            AddToLinesList(comList);  
        }

    }
}
