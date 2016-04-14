using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using Npgsql;
using NpgsqlTypes;
using webroles.GenerateScriptTable;
using webroles.Properties;
using webroles.TransferData;

namespace webroles.TablesSecondLevel
{
    class ActionsLnkTable : TableSecondLevel, IDataSourceIndividComboBox
    {
         
         public ActionsLnkTable(string nameParentTreeViewNode)
        {
            this.nameParentTreeViewNode = nameParentTreeViewNode;
        }
        DataGridViewColumn[] columnCollection;
        readonly List<IObserver> nzpActList = new List<IObserver>();
        readonly List<IObserver> pageUrlList = new List<IObserver>();
         List<DataSourceStorageForComboBoxCell> dataStore;
        public List<IObserver> ObserversPagesTable
         {
             get { return pageUrlList; }
         }

        public List<IObserver> ObserversSactionsTable
        {
            get { return nzpActList; }
        }

        private readonly string nameParentTreeViewNode;
        private const string nodeTreeViewText = "Связь действий";
        string selectCommand = "";//"WITH selectcreate as (SELECT nzp_al, cur_page, nzp_act, page_url, created_on, created_by, changed_on, changed_by, user_name as user_name_created " +
//"FROM actions_lnk LEFT OUTER JOIN users ON (created_by=id)),  " +
//"selectchange AS (SELECT nzp_al, cur_page, nzp_act, page_url,  created_on, created_by, changed_on, changed_by, user_name_created, user_name as user_name_changed " +
//"FROM selectcreate LEFT OUTER JOIN users ON (changed_by=id)) " +
//"SELECT nzp_al, cur_page, nzp_act, selectchange.page_url, selectchange.created_on, selectchange.created_by, selectchange.changed_on, selectchange.changed_by, user_name_created, user_name_changed," +
//"pages.nzp_page ||' '|| pages.page_name as pag_name " +
//"FROM selectchange LEFT OUTER JOIN pages ON(selectchange.cur_page=pages.nzp_page) ;";

        public override string NameParentTable
        {
            get { return nameParentTreeViewNode; }
        }

        public override string NameParentColumn
        {
            get { return "nzp_page"; }
        }

        public override string NameOwnBaseColumn
        {
            get { return "cur_page"; }
        }

        private string getSelectCommand()
        {
            return "WITH selectcreate as (SELECT nzp_al, cur_page, nzp_act, page_url, created_on, created_by, changed_on, changed_by, user_name as user_name_created " +
            "FROM " + Tables.actions_lnk + " LEFT OUTER JOIN " + Tables.users + " ON (created_by=id)),  " +
            "selectchange AS (SELECT nzp_al, cur_page, nzp_act, page_url,  created_on, created_by, changed_on, changed_by, user_name_created, user_name as user_name_changed " +
            "FROM selectcreate LEFT OUTER JOIN " + Tables.users + " ON (changed_by=id)) " +
            "SELECT nzp_al, cur_page, nzp_act, selectchange.page_url, selectchange.created_on, selectchange.created_by, selectchange.changed_on, selectchange.changed_by, " +
            "user_name_created, user_name_changed," +
            "p.nzp_page ||' '|| p.page_name as pag_name " +
            "FROM selectchange LEFT OUTER JOIN " + Tables.pages + " p ON(selectchange.cur_page=p.nzp_page)   WHERE cur_page =" + Position + " ORDER BY nzp_al ;";
        }

        public override void GetCorrespondTable(DataSet dataSet, NpgsqlDataAdapter adapter, NpgsqlConnection connect, int positionParam)
        {
             selectCommand = 
            "WITH selectcreate as (SELECT nzp_al, cur_page, nzp_act, page_url, created_on, created_by, changed_on, changed_by, user_name as user_name_created "+
            "FROM " + Tables.actions_lnk + " LEFT OUTER JOIN " + Tables.users + " ON (created_by=id)),  " +
            "selectchange AS (SELECT nzp_al, cur_page, nzp_act, page_url,  created_on, created_by, changed_on, changed_by, user_name_created, user_name as user_name_changed "+
            "FROM selectcreate LEFT OUTER JOIN " + Tables.users + " ON (changed_by=id)) " +
            "SELECT nzp_al, cur_page, nzp_act, selectchange.page_url, selectchange.created_on, selectchange.created_by, selectchange.changed_on, selectchange.changed_by, " +
            "user_name_created, user_name_changed,"+
            "p.nzp_page ||' '|| p.page_name as pag_name "+
            "FROM selectchange LEFT OUTER JOIN " + Tables.pages + " p ON(selectchange.cur_page=p.nzp_page)   WHERE cur_page =" + positionParam + " ORDER BY nzp_al ;";
                dataSet.Tables[TableName].Clear();
                adapter.SelectCommand.CommandText = selectCommand;
                TransferDataDb.Fill(adapter, dataSet.Tables[TableName]);
        }

        public override string NodeTreeViewText
        {
            get { return nodeTreeViewText; }
        }

        public override string TableName
        {
            get { return Tables.actions_lnk; }
        }

        public override string SelectCommand
        {
            get { return getSelectCommand(); }
        }

        public override DataGridViewColumn[] GetTableColumns
        {
            get
            {
                return columnCollection;
            }
            set
            {
                columnCollection = value;
            }
        }

        public override void CreateColumns()
        {
             //string nzpPagesItems = "WITH selectsour AS(SELECT a.cur_page as cur_page, p.page_name as page_name " +
             //                            "FROM " + Tables.actions_show + " a LEFT OUTER JOIN " + Tables.pages + " p ON (a.cur_page=p.nzp_page)) " +
             //                            "SELECT DISTINCT cur_page as id, cur_page||' '||page_name as text FROM selectsour ORDER BY id";
            string nzpPagesItems = "select nzp_page as id, nzp_page||' '||page_name as text from " + Tables.pages + " order by id";
            columnCollection = new DataGridViewColumn[12];
            columnCollection[11] = CreateDataGridViewColumn.CreateComboBoxColumn("Для скрипта");
            columnCollection[10] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.actions_lnk, "user_name_changed", Resources.user_name_chang, true, true);
            columnCollection[9] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.actions_lnk, "changed_by", Resources.changed_by, true, false);
            columnCollection[8] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.actions_lnk, "changed_on", Resources.changed_on, true, true);
            columnCollection[7] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.actions_lnk, "user_name_created", Resources.user_name_creat, true, true);
            columnCollection[6] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.actions_lnk, "created_by", Resources.created_by, true, false);
            columnCollection[5] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.actions_lnk, "created_on", Resources.created_on, true, true);
            columnCollection[4] = CreateDataGridViewColumn.CreateComboBoxColumn<string>(Resources.actions_showTable_nzp_act, "nzp_act");
            nzpActList.Add(new ComboBoxBindingSource(columnCollection[4], true));
            columnCollection[3] = CreateDataGridViewColumn.CreateComboBoxColumn<string>("Наименование страницы", "page_url");
            pageUrlList.Add(new ComboBoxBindingSource(columnCollection[3], true, nzpPagesItems, true));
            columnCollection[2] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.actions_lnk, "pag_name", "Наименование текущей страницы", true, false);
            columnCollection[1] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.actions_lnk, "cur_page", "Текущая страница", true, false);
            columnCollection[0] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.actions_lnk, "nzp_al", Resources.actions_showTable_id, true, true);
            columnCollection[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
        }

        public override bool Save(NpgsqlConnection connect, NpgsqlDataAdapter adapter, DataTable dt)
        {
            // Удаление строк
            adapter.DeleteCommand = new NpgsqlCommand("Delete from " + Tables.actions_lnk + " Where nzp_al=@nzp_al", connect);
            NpgsqlParameter paramter = adapter.DeleteCommand.Parameters.Add("@nzp_al", NpgsqlDbType.Integer);
            paramter.SourceColumn = "nzp_al";
            paramter.SourceVersion = DataRowVersion.Original;

            // Обновление данных
            adapter.UpdateCommand = new NpgsqlCommand("UPDATE " + Tables.actions_lnk + " SET nzp_al=@nzp_al, cur_page=@cur_page,  nzp_act=@nzp_act, page_url=@page_url, " +
             "created_on=@created_on, created_by=@created_by, changed_on=@changed_on, changed_by=@changed_by " + "Where nzp_al=@nzp_al", connect);
            paramter = adapter.UpdateCommand.Parameters.Add("@nzp_al", NpgsqlDbType.Integer);
            paramter.SourceColumn = "nzp_al";
            paramter = adapter.UpdateCommand.Parameters.Add("@cur_page", NpgsqlDbType.Bigint);
            paramter.SourceColumn = "cur_page";
            paramter = adapter.UpdateCommand.Parameters.Add("@nzp_act", NpgsqlDbType.Integer);
            paramter.SourceColumn = "nzp_act";
            paramter = adapter.UpdateCommand.Parameters.Add("@page_url", NpgsqlDbType.Integer);
            paramter.SourceColumn = "page_url";
            paramter = adapter.UpdateCommand.Parameters.Add("@created_on", NpgsqlDbType.Timestamp);
            paramter.SourceColumn = "created_on";
            paramter = adapter.UpdateCommand.Parameters.Add("@created_by", NpgsqlDbType.Integer);
            paramter.SourceColumn = "created_by";
            paramter = adapter.UpdateCommand.Parameters.Add("@changed_on", NpgsqlDbType.Timestamp);
            paramter.SourceColumn = "changed_on";
            paramter = adapter.UpdateCommand.Parameters.Add("@changed_by", NpgsqlDbType.Integer);
            paramter.SourceColumn = "changed_by";
            paramter.SourceVersion = DataRowVersion.Original;

            // Вставка строк
            adapter.InsertCommand = new NpgsqlCommand("Insert into " + Tables.actions_lnk + " (cur_page,  nzp_act, page_url, created_on, created_by, changed_on, changed_by) " +
            "values ( @cur_page,  @nzp_act, @page_url, @created_on, @created_by, @changed_on, @changed_by); ", connect);
            paramter = adapter.InsertCommand.Parameters.Add("@cur_page", NpgsqlDbType.Bigint);
            paramter.SourceColumn = "cur_page";
            paramter = adapter.InsertCommand.Parameters.Add("@nzp_act", NpgsqlDbType.Integer);
            paramter.SourceColumn = "nzp_act";
            paramter = adapter.InsertCommand.Parameters.Add("@page_url", NpgsqlDbType.Integer);
            paramter.SourceColumn = "page_url";
            paramter = adapter.InsertCommand.Parameters.Add("@created_on", NpgsqlDbType.Timestamp);
            paramter.SourceColumn = "created_on";
            paramter = adapter.InsertCommand.Parameters.Add("@created_by", NpgsqlDbType.Integer);
            paramter.SourceColumn = "created_by";
            paramter = adapter.InsertCommand.Parameters.Add("@changed_on", NpgsqlDbType.Timestamp);
            paramter.SourceColumn = "changed_on";
            paramter = adapter.InsertCommand.Parameters.Add("@changed_by", NpgsqlDbType.Integer);
            paramter.SourceColumn = "changed_by";
            if (!TransferData.TransferDataDb.UpdateDbTable(adapter, dt)) return false;
            return true;
        }

        public override void SetDefaultValuesAfterRowAdded(DataGridView dgv, int index)
        {
            dgv.Rows[index].Cells["cur_page"].Value = position;
            dgv.Rows[index].Cells["nzp_act"].Value = DBNull.Value;
            dgv.Rows[index].Cells["page_url"].Value = DBNull.Value;
            dgv.Rows[index].Cells["created_on"].Value = DateTime.Now;
            dgv.Rows[index].Cells["created_by"].Value = MainWindow.UserId;
            dgv.Rows[index].Cells["user_name_created"].Value = MainWindow.UserName;
            dgv.Rows[index].Cells["changed_by"].Value = DBNull.Value;
            SetDataSourceToIndividualComboBoxCell();
        }
        public void SetDataSourceToIndividualComboBoxCell()
        {
            dataStore = new List<DataSourceStorageForComboBoxCell>();

            foreach (DataGridViewRow row in columnCollection[3].DataGridView.Rows)
            {
                int cur_page = (int)row.Cells["cur_page"].Value;
                string commandActString = "WITH selectsour AS  (SELECT a.nzp_act as nzp_act, s.act_name as act_name " +
"FROM " + Tables.actions_show + " a LEFT OUTER JOIN " + Tables.s_actions + " s ON (a.nzp_act=s.nzp_act) WHERE a.cur_page=" + cur_page + ") " +
"SELECT selectsour.nzp_act AS id , selectsour.nzp_act||' '||selectsour.act_name AS text " +
"FROM selectsour ORDER BY id; ";

                var con = (from n in dataStore
                           where n.num == cur_page
                           select n).Count();

                var dt = ComboBoxBindingSource.Update(commandActString);
                if (con == 0) dataStore.Add(new DataSourceStorageForComboBoxCell(dt, cur_page));
                ((DataGridViewComboBoxCell)row.Cells["nzp_act"]).DataSource = dt;
            }
        }

        public void Sort()
        {
            columnCollection[0].DataGridView.Sort(columnCollection[0], ListSortDirection.Ascending);
        }

        public override void SetDefaultValuesAfterCellChange(DataGridView dgv, int row, int column)
        {
            var dataGridViewColumn = dgv.Columns["forscript"];
            if (dataGridViewColumn != null && dataGridViewColumn.Index == column)
            {
                // ключ значения для вставки
                Dictionary<string, string> valColumns = new Dictionary<string, string>
                {
                     {"page_url", GenerateScript.GetCellValue(dgv.Rows[row].Cells["page_url"])},
                };
                // ключ значение базовой колонки
                Dictionary<string, int> idColVal = new Dictionary<string, int>
                {
                    { "nzp_al", (int)dgv.Rows[row].Cells["nzp_al"].Value }
                };
                // ключ значение для where
                Dictionary<string, string> whereDict = new Dictionary<string, string>
                {{"cur_page", GenerateScript.GetCellValue(dgv.Rows[row].Cells["cur_page"])}, 
                {"nzp_act", GenerateScript.GetCellValue(dgv.Rows[row].Cells["nzp_act"])}};
                ScriptGenerateOper typescr;
                if (dgv.Rows[row].Cells["forscript"].Value == null)
                {
                    typescr = ScriptGenerateOper.none;
                }
                else
                {
                    typescr = (ScriptGenerateOper)dgv.Rows[row].Cells["forscript"].Value;
                }

                switch (typescr)
                    {
                        case ScriptGenerateOper.Insert:
                            valColumns.Add("cur_page", GenerateScript.GetCellValue(dgv.Rows[row].Cells["cur_page"])); 
                            valColumns.Add("nzp_act", GenerateScript.GetCellValue(dgv.Rows[row].Cells["nzp_act"])); 
                            break;
                        case ScriptGenerateOper.Update:
                            break;
                        case ScriptGenerateOper.Delete:
                            break;
                    }
                GenerateScript.AddVal(TableName, valColumns, whereDict, idColVal, typescr, (int)dgv.Rows[row].Cells["cur_page"].Value);
                return;
            }
            dgv.Rows[row].Cells["user_name_changed"].Value = MainWindow.UserName;
            dgv.Rows[row].Cells["changed_on"].Value = DateTime.Now;
            dgv.Rows[row].Cells["changed_by"].Value = MainWindow.UserId;

            var dataGridViewCol = dgv.Columns["cur_page"];
            if (dataGridViewCol != null && column != dataGridViewCol.Index) return;
            var celVal = (int)dgv.Rows[row].Cells[column].Value;
            var dataTable = from n in dataStore
                            where n.num == celVal
                            select n;
            var comboCell = ((DataGridViewComboBoxCell)dgv.Rows[row].Cells["nzp_act"]);

            if (!dataTable.Any())
            {
                string commandActString = "WITH selectsour AS  (SELECT a.nzp_act as nzp_act, s.act_name as act_name " +
                            "FROM " + Tables.actions_show + " a LEFT OUTER JOIN " + Tables.s_actions+ " s ON (a.nzp_act=s.nzp_act) WHERE a.cur_page=" + celVal + ") " +
                            "SELECT selectsour.nzp_act AS id , selectsour.nzp_act||' '||selectsour.act_name AS text " +
                            "FROM selectsour; ";
                var dt = ComboBoxBindingSource.Update(commandActString);
                comboCell.DataSource = dt;
                dataStore.Add(new DataSourceStorageForComboBoxCell(dt, celVal));
                comboCell.Value = dt.Rows[0].Field<int>("id");
                return;
            }
            var sourceDataTable = dataTable.First().dt;
            comboCell.DataSource = sourceDataTable;
            comboCell.Value = sourceDataTable.Rows[0].Field<int>("id");

        }


        

       }
    }

