using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Npgsql;

namespace webroles.GenerateScriptTable
{
    class PagesTableGenScript : GenerateScript
    {
        private DataTable sortKodWhereUpKodNotZeroDataTable;
        public override void Request()
        {

            switch (TypeScr)
            {
                case TypeScript.ChangeDeveloper: break;
                case TypeScript.ChangeCustomer: break;
                case TypeScript.FullDeveloper:
                    requestToFullScript(""); break;
                case TypeScript.FullCustomer:
                    string where = " and (is_dev<>1 OR is_dev is null) ";
                    requestToFullScript(where); break;
            }
        }

        private void requestToFullScript(string where)
        {
            string commandText =
                //"WITH pageun AS (SELECT * FROM pages_intermed UNION SELECT *  FROM pages_intermed_rp) " +
                //                       "SELECT DISTINCT pages.nzp_page, page_url, page_menu, page_name, hlp,up_kod, group_id, page_type, sort_kod FROM pages, pageun WHERE pages.nzp_page=pageun.nzp_page ORDER BY pages.nzp_page;";
    "WITH pageun AS (SELECT * FROM " + Tables.pages_intermed + " UNION SELECT *  FROM " + Tables.pages_intermed_rp + "), " +
    "sel as (select Distinct up_kod as nzp_page from " + Tables.pages + " p," + Tables.role_pages + " rp Where up_kod is not null "+where+" and p.nzp_page=rp.nzp_page UniON select * from pageun) " +
    "SELECT DISTINCT p.nzp_page, page_url, page_menu, page_name, hlp,up_kod, group_id, page_type, sort_kod FROM " + Tables.pages + " p, pageun, sel WHERE p.nzp_page=sel.nzp_page ORDER BY p.nzp_page; ";
            // "Select * from pages_show where (cur_page=10 or page_url=10)";
            DataTable = new DataTable();
            Adapter = new NpgsqlDataAdapter(commandText, Connect);
            Adapter.Fill(DataTable);
        }

        public override void GenerateScr()
        {
            switch (TypeScr)
            {
                case TypeScript.ChangeCustomer: 
                case TypeScript.ChangeDeveloper:
                    var pagesRow = ChangedRowCollection.Select(row => row).Where(row => row.TableName == Tables.pages );
                    var listPagesRows = pagesRow as IList<ChangedRow> ?? pagesRow.ToList();
                    if (listPagesRows.Count() != 0)
                    {
                       
                        generateToChangeScript(listPagesRows, Tables.pages);
                    }
                    break;
                case TypeScript.FullDeveloper:
                    generateToFullScript(); break;
                case TypeScript.FullCustomer:
                    generateToFullScript(); break;
            }
        }



        private void generateToFullScript()
        {
            var comList = (from DataRow dr in DataTable.Rows
                           select "INSERT INTO " + Tables.pages + " (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES (" +
                                  (dr.Field<int?>("nzp_page").HasValue ? dr.Field<int?>("nzp_page").ToString() : "null") + ", " +
                                  (!String.IsNullOrEmpty(dr.Field<string>("page_url")) ? "'" + dr.Field<string>("page_url") + "'" : "''") + ", " +
                                  (!String.IsNullOrEmpty(dr.Field<string>("page_menu")) ? "'" + dr.Field<string>("page_menu") + "'" : "''") + ", " +
                                  (!String.IsNullOrEmpty(dr.Field<string>("page_name")) ? "'" + dr.Field<string>("page_name") + "'" : "''") + ", " +
                                  (!String.IsNullOrEmpty(dr.Field<string>("hlp")) ? "'" + dr.Field<string>("hlp") + "'" : "''") + ", " +
                                  (dr.Field<int?>("up_kod").HasValue ? dr.Field<int?>("up_kod").ToString() : "null") + ", " +
                                  (dr.Field<int?>("group_id").HasValue ? dr.Field<int?>("group_id").ToString() : "null") + ");").ToList();
            comList.Insert(0, "--Заполнение таблицы " + Tables.pages);
            comList.Add(string.Empty);
            AddToLinesList(comList);
        }

       
    }
}
