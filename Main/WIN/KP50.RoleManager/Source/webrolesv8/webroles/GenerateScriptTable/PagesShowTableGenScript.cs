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
    internal class PagesShowTableGenScript : GenerateScript
    {
        private DataTable sortKodWhereUpKodNotZeroDataTable;

        public override void Request()
        {

            switch (TypeScr)
            {
                case TypeScript.ChangeDeveloper: break;
                case TypeScript.ChangeCustomer: break;

                case TypeScript.FullDeveloper:
                    RequestToFullScript(""); break;
                case TypeScript.FullCustomer:
                    RequestToFullScript(""); break;
            }
        }
        public override void GenerateScr()
        {
            switch (TypeScr)
            {
                case TypeScript.ChangeDeveloper:
                    break;
                case TypeScript.ChangeCustomer:
                    GenerateToFullScript();
                    RequestToFullScript("");
                    UpdateSortKod();
                    break;
                case TypeScript.FullDeveloper:
                    GenerateToFullScript();
                    RequestToFullScript("");
                    UpdateSortKod();
                    break;
                case TypeScript.FullCustomer:
                    GenerateToFullScript();
                    RequestToFullScript("");
                    UpdateSortKod();
                    break;
            }
        }

        public void RequestToFullScript(string where)
        {

            requestUpkodZero();

            string commandTextUpKodNotZero =
                "select nzp_page, sort_kod from " + Tables.pages + " where sort_kod <>nzp_page and up_kod<>0 " + where + " order by sort_kod;";
            sortKodWhereUpKodNotZeroDataTable = new DataTable();
            Adapter = new NpgsqlDataAdapter(commandTextUpKodNotZero, Connect);
            Adapter.Fill(sortKodWhereUpKodNotZeroDataTable);
        }

        private void requestUpkodZero()
        {
            string commandTextUpKodZero =
                "select nzp_page, sort_kod, up_kod from " + Tables.pages + " where sort_kod <>nzp_page and up_kod=0 order by sort_kod;";

            NpgsqlConnection connect = ConnectionToPostgreSqlDb.GetConnection();
            NpgsqlCommand command = new NpgsqlCommand(commandTextUpKodZero, connect);
            DataTable = new DataTable();
            Adapter = new NpgsqlDataAdapter(commandTextUpKodZero, Connect);
            Adapter.Fill(DataTable);
        }

        public void GenerateToFullScript()
        {
            var list = new List<string>
            {
                "INSERT INTO " + Tables.pages_show + " (cur_page,page_url,up_kod,sort_kod)",
                "SELECT DISTINCT a.nzp_page, b.nzp_page, COALESCE(b.up_kod,0), b.nzp_page",
                "FROM " + Tables.pages + " a, " + Tables.pages + " b, " + Tables.page_links + "  pl",
                "WHERE (pl.page_from = a.nzp_page or pl.group_from = a.group_id or (pl.page_from is null and pl.group_from is null))",
                "and (pl.page_to = b.nzp_page or pl.group_to = b.group_id)",
                "and not exists (select 1 from " + Tables.pages_show + " ps where ps.cur_page = a.nzp_page and ps.page_url = b.nzp_page);",
                string.Empty,
                "--добавить недостающие пункты меню, у которых есть подменю",
                "CREATE temp table ps (cur_page integer, up_kod integer);",
                "insert into ps select distinct cur_page, up_kod from " + Tables.pages_show + " a where up_kod > 0 and not exists (select 1 from " + Tables.pages_show + " where cur_page = a.cur_page and page_url = a.up_kod);",
                "insert into " + Tables.pages_show + " (cur_page, page_url, up_kod, sort_kod) select cur_page, ps.up_kod, 0, 0 from ps, pages Where ps.up_kod=pages.nzp_page;",
                "drop table ps;",
                string.Empty
            };
            AddToLinesList(list);
          //  UpdateSortKod();
        }

        public void UpdateSortKod()
        {
            var comListUpKodZero = (from DataRow dr in DataTable.Rows
                                    select
                                        "UPDATE " + Tables.pages_show + " SET sort_kod =" +
                                        (dr.Field<int?>("sort_kod").HasValue ? dr.Field<int?>("sort_kod").ToString() : "null") + ", " +
                                        "up_kod = " + (dr.Field<int?>("up_kod").HasValue ? dr.Field<int?>("up_kod").ToString() : "null") +
                                        " WHERE page_url = " +
                                        (dr.Field<int?>("nzp_page").HasValue ? dr.Field<int?>("nzp_page").ToString() : "null") + ";").ToList();
            if (comListUpKodZero.Count != 0)
            {
                comListUpKodZero.Insert(0, "--Обновление кодов сортировки для корректного отображения пунктов меню");
                comListUpKodZero.Add(string.Empty);
                AddToLinesList(comListUpKodZero);
            }

            if (sortKodWhereUpKodNotZeroDataTable == null) return;
            var comListUpKodNotZero = (from DataRow dr in sortKodWhereUpKodNotZeroDataTable.Rows
                                       select
                                           "UPDATE " + Tables.pages_show + " SET sort_kod =" +
                                           (dr.Field<int?>("sort_kod").HasValue ? dr.Field<int?>("sort_kod").ToString() : "null") +
                                           " WHERE page_url = " +
                                           (dr.Field<int?>("nzp_page").HasValue ? dr.Field<int?>("nzp_page").ToString() : "null") + ";").ToList();
            if (comListUpKodNotZero.Count != 0)
            {
                comListUpKodNotZero.Add(string.Empty);
                AddToLinesList(comListUpKodNotZero);
            } 
        }

        private void generateChanges()
        {
            var tablesPageLinksPagesCount =
                ChangedRowCollection.Count(
                    row =>
                        (row.TableName == Tables.pages 
                         && row.State== DataRowState.Added) ||
                        row.TableName == Tables.page_links &&
                        (row.State == DataRowState.Added || row.State == DataRowState.Modified));
            if (tablesPageLinksPagesCount != 0)
            {
                GenerateToFullScript();
            }

            var pagesRow = ChangedRowCollection.Select(row => row).Where(row => row.TableName == Tables.pages && row.IsSortKodChanged);

            var changedRows = pagesRow as ChangedRow[] ?? pagesRow.ToArray();
            if (changedRows.Any())
            {
                foreach (var row in changedRows)
                {
                    string where = " and nzp_page=" + row.WhereColValuesDictionary["nzp_page"];
                    RequestToFullScript(where);
                   
                } 
            }
            else
            {
                requestUpkodZero();
            }
            UpdateSortKod();


            //foreach (string @where in pagesRow.Select(row => " and nzp_page=" + row.WhereColValuesDictionary["nzp_page"]))
            //{
            //    RequestToFullScript(@where);
            //    UpdateSortKod();
            //}

        }


    
    }




}

