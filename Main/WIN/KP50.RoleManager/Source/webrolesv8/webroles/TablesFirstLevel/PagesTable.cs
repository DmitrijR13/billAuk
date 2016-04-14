using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Windows.Forms;
using Npgsql;
using NpgsqlTypes;
using webroles.GenerateScriptTable;
using webroles.GenerateScriptTable.PagesTableScripts;
using webroles.Properties;
using webroles.TransferData;
using webroles.Windows;

namespace webroles.TablesFirstLevel
{
    internal class PagesTable : TableFirstLevel, ISubject, INamePosition,IDeletable, IChangeScript
    {
        private DataGridViewColumn[] columnCollection;
        private const string nodeTreeViewText = "Страницы";
        private int position;

        private readonly string selectCommand = "WITH selectcreate as(select nzp_page, page_url, page_menu, " +
                                                "page_name, hlp, up_kod, group_id, page_type, is_dev, sort_kod, comment,  created_on, created_by, changed_on, changed_by, " +
                                                "u.user_name as user_name_created " + "FROM " + Tables.pages +
                                                " p LEFT OUTER JOIN " + Tables.users + " u on (p.created_by = u.id)) " +
                                                "SELECT nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id, page_type, is_dev, sort_kod, comment, created_on, created_by, " +
                                                "changed_on, changed_by, user_name_created, u.user_name as user_name_changed " +
                                                "FROM selectcreate LEFT OUTER JOIN " + Tables.users +
                                                " u on (selectcreate.changed_by = u.id) ORDER BY selectcreate.nzp_page;";

        public List<IObserver> GroupIdList = new List<IObserver>();

        public List<IObserver> ObserversPageGroupsTable
        {
            get { return GroupIdList; }
        }

        private readonly List<IObserver> observers = new List<IObserver>();

        public override string NodeTreeViewText
        {
            get { return nodeTreeViewText; }
        }

        public override string TableName
        {
            get { return Tables.pages; }
        }

        public override string SelectCommand
        {
            get { return selectCommand; }
        }

        public override DataGridViewColumn[] GetTableColumns
        {
            get { return columnCollection; }
            set { columnCollection = value; }
        }


        public override void CreateColumns()
        {

            string selectCommandToUpKodColumnPagesTable =
                "select nzp_page as id, nzp_page ||' '|| page_name  as text from " + Tables.pages +
                " where page_type=2 ORDER BY id;";
            string selectCommandToPageTypeColumnPagesTable = "SELECT id, type_name as text FROM  " + Tables.page_types +
                                                             "; ";
            //const string selectCommandToPageTypeGroupsPagesTable = "SELECT group_id as id, group_id ||' '|| group_name  as text from page_groups ORDER BY id;";
            columnCollection = new DataGridViewColumn[18];
            columnCollection[17] = CreateDataGridViewColumn.CreateComboBoxColumnPagesTableForScript("Для скрипта");
            columnCollection[16] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.pages, "user_name_changed",
                Resources.user_name_chang, true, true);
            columnCollection[15] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.pages, "changed_on",
                Resources.changed_on, true, true);
            columnCollection[14] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.pages, "user_name_created",
                Resources.user_name_creat, true, true);
            columnCollection[13] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.pages, "created_on",
                Resources.created_on, true, true);
            columnCollection[12] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.pages, "comment",
    "Комметарии", false, true);
            columnCollection[11] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.pages, "sort_kod",
                Resources.sort_kod, false, true);
            columnCollection[10] = CreateDataGridViewColumn.CreateCheckBoxColumn(Tables.pages, "is_dev",
                Resources.is_dev);
            columnCollection[9] = CreateDataGridViewColumn.CreateComboBoxColumn<string>(Resources.pagesTable_group_id,
                "group_id");
            columnCollection[9].SortMode = DataGridViewColumnSortMode.Automatic;
            GroupIdList.Add(new ComboBoxBindingSource(columnCollection[9], true));
            columnCollection[8] = CreateDataGridViewColumn.CreateComboBoxColumn<string>(Resources.pagesTable_page_type,
                "page_type");
            columnCollection[8].SortMode = DataGridViewColumnSortMode.Automatic;
            var pagetype = new ComboBoxBindingSource(selectCommandToPageTypeColumnPagesTable, columnCollection[8], false);
            pagetype.update();
            columnCollection[7] = CreateDataGridViewColumn.CreateComboBoxColumn<string>(Resources.pagesTable_up_kod,
                "up_kod");
            observers.Add(new ComboBoxBindingSource(columnCollection[7], true, selectCommandToUpKodColumnPagesTable,
                true, true));
            columnCollection[7].SortMode = DataGridViewColumnSortMode.Automatic;
            columnCollection[6] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.pages, "hlp",
                Resources.pagesTable_hlp, false, true);

            columnCollection[5] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.pages, "page_name",
                Resources.pagesTable_page_name, false, true);
            columnCollection[4] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.pages, "page_menu",
                Resources.pagesTable_page_menu, false, true);
            columnCollection[3] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.pages, "page_url",
                Resources.page_url, false, true);
            columnCollection[2] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.pages, "nzp_page",
                Resources.pagesTable_nzp_page, true, true);
            columnCollection[2].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
            columnCollection[1] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.pages, "created_by",
                Resources.created_by, true, false);
            columnCollection[0] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.pages, "changed_by",
                Resources.changed_by, false, false);
        }

        public override bool Save(NpgsqlConnection connect, NpgsqlDataAdapter adapter, DataTable dt)
        {
           
            // Удаление строк

            adapter.DeleteCommand = new NpgsqlCommand("Delete from " + Tables.pages + " Where nzp_page=@nzp_page",
                connect);
            NpgsqlParameter paramter = adapter.DeleteCommand.Parameters.Add("@nzp_page",
                NpgsqlDbType.Integer);
            paramter.SourceColumn = "nzp_page";
            paramter.SourceVersion = DataRowVersion.Original;
      
            adapter.UpdateCommand =
                new NpgsqlCommand(
                    "UPDATE " + Tables.pages +
                    " SET page_url=@page_url, page_menu=@page_menu,  page_name=@page_name, hlp=@hlp, up_kod=@up_kod, group_id=@group_id, page_type=@page_type, is_dev=@is_dev, " +
                    "sort_kod=@sort_kod, comment=@comment, created_on=@created_on, created_by=@created_by, changed_on=@changed_on, changed_by=@changed_by " +
                    "Where nzp_page=@nzp_page", connect);
            adapter.UpdateCommand.Parameters.Add("@page_url", NpgsqlDbType.Char, 200, "page_url");
            adapter.UpdateCommand.Parameters.Add("@page_menu", NpgsqlDbType.Char, 200, "page_menu");
            adapter.UpdateCommand.Parameters.Add("@page_name", NpgsqlDbType.Char, 200, "page_name");
            adapter.UpdateCommand.Parameters.Add("@hlp", NpgsqlDbType.Char, 200, "hlp");
            adapter.UpdateCommand.Parameters.Add("@comment", NpgsqlDbType.Char, 255, "comment");
            paramter = adapter.UpdateCommand.Parameters.Add("@up_kod", NpgsqlDbType.Integer);
            paramter.SourceColumn = "up_kod";
            paramter = adapter.UpdateCommand.Parameters.Add("@group_id", NpgsqlDbType.Bigint);
            paramter.SourceColumn = "group_id";
            paramter = adapter.UpdateCommand.Parameters.Add("@page_type", NpgsqlDbType.Integer);
            paramter.SourceColumn = "page_type";
            paramter = adapter.UpdateCommand.Parameters.Add("@is_dev", NpgsqlDbType.Integer);
            paramter.SourceColumn = "is_dev";
            paramter = adapter.UpdateCommand.Parameters.Add("@sort_kod", NpgsqlDbType.Integer);
            paramter.SourceColumn = "sort_kod";
            paramter = adapter.UpdateCommand.Parameters.Add("@created_on", NpgsqlDbType.Timestamp);
            paramter.SourceColumn = "created_on";
            paramter = adapter.UpdateCommand.Parameters.Add("@created_by", NpgsqlDbType.Integer);
            paramter.SourceColumn = "created_by";
            paramter = adapter.UpdateCommand.Parameters.Add("@changed_on", NpgsqlDbType.Timestamp);
            paramter.SourceColumn = "changed_on";
            paramter = adapter.UpdateCommand.Parameters.Add("@changed_by", NpgsqlDbType.Integer);
            paramter.SourceColumn = "changed_by";
            paramter = adapter.UpdateCommand.Parameters.Add("@nzp_page", NpgsqlDbType.Integer);
            paramter.SourceColumn = "nzp_page";

            // Вставка строк
            adapter.InsertCommand =
                new NpgsqlCommand(
                    "Insert into " + Tables.pages +
                    " ( page_url, page_menu,  page_name, hlp, up_kod, group_id, page_type, is_dev, sort_kod, comment, created_on, created_by, changed_on, changed_by) " +
                    "values (@page_url, @page_menu,  @page_name, @hlp, @up_kod, @group_id, @page_type, @is_dev, @sort_kod, @comment, @created_on, @created_by, @changed_on, @changed_by); ",
                    connect);
            adapter.InsertCommand.Parameters.Add("@page_url", NpgsqlDbType.Char, 200, "page_url");
            adapter.InsertCommand.Parameters.Add("@page_menu", NpgsqlDbType.Char, 200, "page_menu");
            adapter.InsertCommand.Parameters.Add("@page_name", NpgsqlDbType.Char, 200, "page_name");
            adapter.InsertCommand.Parameters.Add("@hlp", NpgsqlDbType.Char, 200, "hlp");
            adapter.InsertCommand.Parameters.Add("@comment", NpgsqlDbType.Char, 255, "comment");
            paramter = adapter.InsertCommand.Parameters.Add("@up_kod", NpgsqlDbType.Integer);
            paramter.SourceColumn = "up_kod";
            paramter = adapter.InsertCommand.Parameters.Add("@group_id", NpgsqlDbType.Bigint);
            paramter.SourceColumn = "group_id";
            paramter = adapter.InsertCommand.Parameters.Add("@page_type", NpgsqlDbType.Integer);
            paramter.SourceColumn = "page_type";
            paramter = adapter.InsertCommand.Parameters.Add("@is_dev", NpgsqlDbType.Integer);
            paramter.SourceColumn = "is_dev";
            paramter = adapter.InsertCommand.Parameters.Add("@sort_kod", NpgsqlDbType.Integer);
            paramter.SourceColumn = "sort_kod";
            paramter = adapter.InsertCommand.Parameters.Add("@created_on", NpgsqlDbType.Timestamp);
            paramter.SourceColumn = "created_on";
            paramter = adapter.InsertCommand.Parameters.Add("@created_by", NpgsqlDbType.Integer);
            paramter.SourceColumn = "created_by";
            paramter = adapter.InsertCommand.Parameters.Add("@changed_on", NpgsqlDbType.Timestamp);
            paramter.SourceColumn = "changed_on";
            paramter = adapter.InsertCommand.Parameters.Add("@changed_by", NpgsqlDbType.Integer);
            paramter.SourceColumn = "changed_by";

            if (!TransferData.TransferDataDb.UpdateDbTable(adapter, dt)) return false;
            NotifyObservers();
            return true;
        }

        public override void SetDefaultValuesAfterRowAdded(DataGridView dgv, int index)
        {
            dgv.Rows[index].Cells["is_dev"].Value = 1;
            dgv.Rows[index].Cells["page_type"].Value = 1;
            dgv.Rows[index].Cells["group_id"].Value = DBNull.Value;
            dgv.Rows[index].Cells["up_kod"].Value = DBNull.Value;
            dgv.Rows[index].Cells["created_on"].Value = DateTime.Now;
            dgv.Rows[index].Cells["created_by"].Value = MainWindow.UserId;
            dgv.Rows[index].Cells["user_name_created"].Value = MainWindow.UserName;
            dgv.Rows[index].Cells["changed_by"].Value = DBNull.Value;
            dgv.Rows[index].Cells["page_url"].Value = "";
            dgv.Rows[index].Cells["page_menu"].Value = "";
            dgv.Rows[index].Cells["page_name"].Value = "";
            dgv.Rows[index].Cells["hlp"].Value = "";

        }

        public void AddObservers(List<IObserver> obsrvs)
        {
            observers.AddRange(obsrvs);
        }

        public void NotifyObservers()
        {

            foreach (IObserver obs in observers)
            {
                if (!obs.AdditionalContition)
                {
                    var dt =
                        ComboBoxBindingSource.Update(
                            "SELECT nzp_page AS id, nzp_page ||' '|| page_name  AS text FROM " + Tables.pages +
                            " ORDER BY id;");
                    obs.update(dt);
                }
                else
                    obs.update(ComboBoxBindingSource.Update(obs.AdditionalContotionString));
            }
        }

        public string GetNamePosition(DataGridViewRow dataRow)
        {
            int num;
            string text;
            if (dataRow != null)
            {
                num = (int) dataRow.Cells["nzp_page"].Value;
                if (dataRow.Cells["page_name"].Value != null)
                {
                    text = dataRow.Cells["page_name"].Value.ToString();
                }
                else
                {
                 text = String.Empty;
                }
                
            }
            else
            {
                position = 0;
                num = PositionDefault;
                text = "Главная страница";
            }
            return "Страница" + ": " + num + " " + text;
        }
        public int Position
        {
            get { return position; }
            set { position = value; }
        }

       public int PositionDefault {
           get { return 1; }
       }

        public void GenerateUpdateStateMent(DataTable dt)
        {
           
        }

        public override void SetDefaultValuesAfterCellChange(DataGridView dgv, int row, int column)
        {
            var dataGridViewColumn = dgv.Columns["forscript"];
            if (dataGridViewColumn != null && dataGridViewColumn.Index == column)
            {
                // ключ значение для вставки
                Dictionary<string, string> valColumns = new Dictionary<string, string>
                {
                    {"nzp_page", dgv.Rows[row].Cells["nzp_page"].Value.ToString()},
                    {"page_url", GenerateScript.GetCellValue(dgv.Rows[row].Cells["page_url"])},
                    {"page_menu", GenerateScript.GetCellValue(dgv.Rows[row].Cells["page_menu"])},
                    {"page_name", GenerateScript.GetCellValue(dgv.Rows[row].Cells["page_name"])},
                    {"hlp", GenerateScript.GetCellValue(dgv.Rows[row].Cells["hlp"])},
                    {"up_kod", GenerateScript.GetCellValue(dgv.Rows[row].Cells["up_kod"])},
                    {"group_id", GenerateScript.GetCellValue(dgv.Rows[row].Cells["group_id"])},
                };
                // ключ значение для where
                Dictionary<string, string> whereDict = new Dictionary<string, string>
                {{"nzp_page", dgv.Rows[row].Cells["nzp_page"].Value.ToString()}};
                // ключ значение основной колонки (ключа)
                Dictionary<string, int> idColVal = new Dictionary<string, int>
                {{"nzp_page", (int) dgv.Rows[row].Cells["nzp_page"].Value}};
                ScriptGenerateOper typescr;
                if (dgv.Rows[row].Cells["forscript"].Value == null)
                {
                    typescr = ScriptGenerateOper.none;
                }
                else
                {
                    typescr = (ScriptGenerateOper) dgv.Rows[row].Cells["forscript"].Value;
                }
                switch (typescr)
                {
                    case ScriptGenerateOper.Insert:
                        var chRow = GenerateScript.AddVal(TableName, valColumns, whereDict, idColVal, ScriptGenerateOper.Insert);
                        if ((int) dgv.Rows[row].Cells["nzp_page"].Value != (int) dgv.Rows[row].Cells["sort_kod"].Value)
                        {
                            chRow.WhereColValuesDictionary.Add("nzp_page", dgv.Rows[row].Cells["nzp_page"].Value.ToString());
                            chRow.IsSortKodChanged = true;
                        }
                        return;
                    case ScriptGenerateOper.Update:
                        valColumns.Add("sort_kod", dgv.Rows[row].Cells["sort_kod"].Value.ToString());
                        GenerateScript.AddVal(TableName, valColumns, whereDict, idColVal, typescr);
                        break;
                    case ScriptGenerateOper.Delete:
                        GenerateScript.AddVal(TableName, valColumns, whereDict, idColVal, typescr);
                        break;
                    case ScriptGenerateOper.InsertWhole:
                        SelectProfileWindow win = new SelectProfileWindow((int)dgv.Rows[row].Cells["nzp_page"].Value);
                        if (win.IsNoProfiles)
                        {
                            MessageBox.Show("Данной страницы нет ни в одном профиле");
                            dgv.Rows[row].Cells["forscript"].Value = null;
                        }
                        else
                        {
                            if (win.ShowDialog() == DialogResult.OK)
                            {
                                PagesInsertWholeScript profile = new PagesInsertWholeScript((int)dgv.Rows[row].Cells["nzp_page"].Value, win.ProfileEnum);
                                profile.Generate();
                            }
                            else
                            {
                                dgv.Rows[row].Cells["forscript"].Value = null;
                            }
                        }
                        win.Close();
                        break;
                    case ScriptGenerateOper.DeleteWhole:
                        PagesDeleteWholeScript pagesDelete = new PagesDeleteWholeScript((int)dgv.Rows[row].Cells["nzp_page"].Value);
                        pagesDelete.GenerateScript();
                        break;
                }
                return;
            }
            dgv.Rows[row].ErrorText = "";
            dgv.Rows[row].Cells["user_name_changed"].Value = MainWindow.UserName;
            dgv.Rows[row].Cells["changed_on"].Value = DateTime.Now;
            dgv.Rows[row].Cells["changed_by"].Value = MainWindow.UserId;
        }





        public bool CheckToAllowDelete(DataGridView dvg)
        {
            if (dvg.CurrentRow == null) return true;
            var posit = dvg.CurrentRow.Cells["nzp_page"].Value;
            if (posit == DBNull.Value) return true;
            string[] tablesArr =
            {
                Tables.page_links, Tables.actions_show, Tables.img_lnk, Tables.actions_lnk,
                Tables.role_pages, Tables.role_actions, Tables.s_roles
            };
            Returns ret = new Returns(true, "");
            string commandText = "SELECT EXISTS (Select id from " + Tables.page_links + " where page_from=" + posit +
                                 " OR page_to=" +
                                 posit + ");";
            commandText += "SELECT EXISTS (select nzp_ash from " + Tables.actions_show + " Where cur_page=" + posit +
                           ");";
            commandText += "SELECT EXISTS (select nzp_img from " + Tables.img_lnk + " Where cur_page=" + posit +
                           " OR (tip=1 and kod=" + posit + "));";
            commandText += "SELECT EXISTS (select nzp_al from " + Tables.actions_lnk + " Where cur_page=" + posit +
                           " OR page_url=" + posit + ");";
            commandText += "SELECT EXISTS (select id from " + Tables.role_pages + " Where nzp_page=" + posit + ");";
            commandText += "SELECT EXISTS (select id from " + Tables.role_actions + " Where nzp_page=" + posit + ");";
            commandText += "SELECT EXISTS (select nzp_role from " + Tables.s_roles + " Where page_url=" + posit + ");";
            return  DBManager.CheckExistsRefernces(commandText, tablesArr, (int) posit);
        }
    }
}
