using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Npgsql;

namespace webroles.GenerateScriptTable
{
    class PageLinksTableGenScript : GenerateScript
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
                      var chanRows = ChangedRowCollection.Select(row => row).Where(row => row.TableName == Tables.page_links);
                    var listchanRows = chanRows as IList<ChangedRow> ?? chanRows.ToList();
                    if (listchanRows.Count() != 0)
                    {
                        generateToChangeScript(listchanRows, Tables.page_links);
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
    "SELECT DISTINCT id, page_from, group_from, page_to, group_to FROM " + Tables.page_links + " ORDER BY id;";
            DataTable = new DataTable();
            Adapter = new NpgsqlDataAdapter(commandText, Connect);
            Adapter.Fill(DataTable);
        }



        private void generateToFullScript()
        {
            var comList = (from DataRow dr in DataTable.Rows
                           select "INSERT INTO " + Tables.page_links + " (page_from, group_from, page_to, group_to) VALUES (" +
                                  (dr.Field<int?>("page_from").HasValue ? dr.Field<int?>("page_from").ToString() : "null") + ", " +
                                  (dr.Field<int?>("group_from").HasValue ? dr.Field<int?>("group_from").ToString() : "null") + ", " +
                                  (dr.Field<int?>("page_to").HasValue ? dr.Field<int?>("page_to").ToString() : "null") + ", " +
                                  (dr.Field<int?>("group_to").HasValue ? dr.Field<int?>("group_to").ToString() : "null") + ");").ToList();
            comList.Insert(0, "--Заполнение таблицы " + Tables.page_links);
            comList.Add(string.Empty);
            AddToLinesList(comList);
        }
    }
}
