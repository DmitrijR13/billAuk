using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using Npgsql;
using NpgsqlTypes;
using webroles.GenerateScriptTable;
using webroles.Properties;
using System.Data;
using webroles.TablesFirstLevel;
using webroles.TransferData;
using webroles.Windows;

namespace webroles.TablesSecondLevel
{
    class RoleActionsTable:TableSecondLevel,IDataSourceIndividComboBox
    {
        public RoleActionsTable(string nameParentTreeViewNode)
        {
            this.nameParentTreeViewNode = nameParentTreeViewNode;
        }
        private readonly string nameParentTreeViewNode;
        DataGridViewColumn[] columnCollection;
        readonly List<IObserver> nzpPageList = new List<IObserver>();
        readonly List<IObserver> nzpActList = new List<IObserver>();
        List<DataSourceStorageForComboBoxCell> dataStore;
        public List<IObserver> ObserversPagesTable
        {
            get { return nzpPageList; }
        }
        public List<IObserver> ObserversSactionTable
        {
            get { return nzpActList; }
        }

        private const string nodeTreeViewText = "Действия ролей";
        string selectCommand = ""; //"WITH selectcreate AS (SELECT role_actions.id, nzp_role, nzp_page, nzp_act, mod_act, created_on, created_by, user_name as user_name_created " +
        //       "FROM role_actions LEFT OUTER JOIN users ON (created_by=role_actions.id)) " +
        //       "SELECT  selectcreate.id, selectcreate.nzp_role, selectcreate.nzp_page, selectcreate.nzp_act, selectcreate.mod_act, selectcreate.created_on, selectcreate.created_by, user_name_created, s_roles.nzp_role||' '|| s_roles.role as role_name " +
        //       "FROM selectcreate LEFT OUTER JOIN s_roles ON (selectcreate.nzp_role=s_roles.nzp_role);";
        public override string NameParentTable
        {
            get { return nameParentTreeViewNode; }
        }

        public override string NameParentColumn
        {
            get { return "nzp_role"; }
        }

        public override string NameOwnBaseColumn
        {
            get { return "nzp_role"; }
        }

        public override void GetCorrespondTable(DataSet dataSet, NpgsqlDataAdapter adapter, NpgsqlConnection connect, int positionParam)
        {

                selectCommand = "WITH selectcreate AS (SELECT ra.id, nzp_role, nzp_page, nzp_act, mod_act, created_on, created_by, user_name as user_name_created " +
               "FROM " + Tables.role_actions + " ra LEFT OUTER JOIN " + Tables.users + " u ON (created_by=u.id)) " +
               "SELECT  selectcreate.id, selectcreate.nzp_role, selectcreate.nzp_page, selectcreate.nzp_act, selectcreate.mod_act, selectcreate.created_on, " +
                                "selectcreate.created_by, user_name_created, sr.nzp_role||' '|| sr.role as role_name " +
               "FROM selectcreate LEFT OUTER JOIN " + Tables.s_roles + " sr ON (selectcreate.nzp_role=sr.nzp_role) Where selectcreate.nzp_role=" + positionParam + " ORDER BY selectcreate.id ";
                dataSet.Tables[Tables.role_actions].Clear();
                adapter.SelectCommand.CommandText = selectCommand;
                TransferDataDb.Fill(adapter, dataSet.Tables[TableName]);
        }

        public override string NodeTreeViewText
        {
            get { return nodeTreeViewText; }
        }

        public override string TableName
        {
            get { return Tables.role_actions; }
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

        private string getSelectCommand()
        {
            return "WITH selectcreate AS (SELECT ra.id, nzp_role, nzp_page, nzp_act, mod_act, created_on, created_by, user_name as user_name_created " +
               "FROM " + Tables.role_actions + " ra LEFT OUTER JOIN " + Tables.users + " u ON (created_by=u.id)) " +
               "SELECT  selectcreate.id, selectcreate.nzp_role, selectcreate.nzp_page, selectcreate.nzp_act, selectcreate.mod_act, selectcreate.created_on, " +
                                "selectcreate.created_by, user_name_created, sr.nzp_role||' '|| sr.role as role_name " +
               "FROM selectcreate LEFT OUTER JOIN " + Tables.s_roles + " sr ON (selectcreate.nzp_role=sr.nzp_role) Where selectcreate.nzp_role=" + Position + " ORDER BY selectcreate.id ";
        }
        public override void CreateColumns()
        {


            string nzpPagesItems = "WITH selectsour AS(SELECT a.cur_page as cur_page, p.page_name as page_name "+
                                         "FROM " + Tables.actions_show + " a LEFT OUTER JOIN " + Tables.pages + " p ON (a.cur_page=p.nzp_page)) " +
                                         "SELECT DISTINCT cur_page as id, cur_page||' '||page_name as text FROM selectsour ORDER BY id";
            string modeActCommandString = "SELECT nzp_act as id, nzp_act||' '|| act_name as text FROM " + Tables.s_actions + " WHERE nzp_act=610 OR nzp_act=611;";
            //string nzpActItems = "SELECT nzp_act as id, nzp_act||' '|| act_name as text FROM s_actions ORDER BY id;";
            // var actionTypes = ComboBoxBindingSource.GetBindingSourceForColumnPages(actionTypesIdItems, false);
            // var nzpAct = ComboBoxBindingSource.GetBindingSourceForColumnPages(nzpActItems, false);           
            columnCollection = new DataGridViewColumn[10];
            columnCollection[9] = CreateDataGridViewColumn.CreateComboBoxColumn("Для скрипта");
            columnCollection[0] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.role_actions, "id", Resources.actions_showTable_id, true, true);
            columnCollection[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
            columnCollection[1] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.role_actions, "role_name", "Наименование роли", true, false);
            columnCollection[2] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.role_actions, "nzp_role", "Текущая роль", true, false);
            columnCollection[3] = CreateDataGridViewColumn.CreateComboBoxColumn<string>("Текущая страница", "nzp_page");
            columnCollection[3].SortMode = DataGridViewColumnSortMode.Automatic;
            nzpPageList.Add(new ComboBoxBindingSource(columnCollection[3], true, nzpPagesItems,true));
            columnCollection[4] = CreateDataGridViewColumn.CreateComboBoxColumn<string>(Resources.actions_showTable_nzp_act, "nzp_act");
            columnCollection[4].SortMode = DataGridViewColumnSortMode.Automatic;
            nzpActList.Add(new ComboBoxBindingSource(columnCollection[4], true));
            columnCollection[5] = CreateDataGridViewColumn.CreateComboBoxColumn<string>("Режим действий", "mod_act");
            columnCollection[5].SortMode = DataGridViewColumnSortMode.Automatic;
            var modeact = new ComboBoxBindingSource(modeActCommandString, columnCollection[5], true);
            modeact.update();
            columnCollection[6] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.role_actions, "created_on", Resources.created_on, true, true);
            columnCollection[7] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.role_actions, "created_by", Resources.created_by, true, false);
            columnCollection[8] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.role_actions, "user_name_created", Resources.user_name_creat, true, true);
        }

        public void SetDataSourceToIndividualComboBoxCell()
        {

           if( columnCollection[4].DataGridView==null) return;
            dataStore = new List<DataSourceStorageForComboBoxCell>();

            foreach (DataGridViewRow row in columnCollection[4].DataGridView.Rows)
            {
                if (row.Cells["nzp_page"].Value == null || row.Cells["nzp_page"].Value == DBNull.Value)
                    continue;
                var cur_page = (int)row.Cells["nzp_page"].Value;
                string commandActString = "WITH selectsour AS  (SELECT a.nzp_act as nzp_act, s.act_name as act_name " +
"FROM " + Tables.actions_show + " a LEFT OUTER JOIN " + Tables.s_actions + " s ON (a.nzp_act=s.nzp_act) WHERE a.cur_page=" + cur_page + ") " +
"SELECT selectsour.nzp_act AS id , selectsour.nzp_act||' '||selectsour.act_name AS text " +
"FROM selectsour; ";

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

            var dataGridViewCol = dgv.Columns["forscript"];
            if (dataGridViewCol != null && dataGridViewCol.Index == column)
            {
                // ключ значения для вставки
                Dictionary<string, string> valColumns = new Dictionary<string, string>
                {
                    {"mod_act", GenerateScript.GetCellValue(dgv.Rows[row].Cells["mod_act"])},
                };
                // ключ значение базовой колонки
                Dictionary<string, int> idColVal = new Dictionary<string, int>
                {
                    {"id", (int) dgv.Rows[row].Cells["id"].Value}
                };
                // ключ значение для where
                Dictionary<string, string> whereValColumns = new Dictionary<string, string>
                {
                    {"nzp_role", GenerateScript.GetCellValue(dgv.Rows[row].Cells["nzp_role"])},
                     {"nzp_page", GenerateScript.GetCellValue(dgv.Rows[row].Cells["nzp_page"])},
                      {"nzp_act", GenerateScript.GetCellValue(dgv.Rows[row].Cells["nzp_act"])}
                };
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
                          valColumns.Add("nzp_role", GenerateScript.GetCellValue(dgv.Rows[row].Cells["nzp_role"])); 
                          valColumns.Add("nzp_page", GenerateScript.GetCellValue(dgv.Rows[row].Cells["nzp_page"])); 
                          valColumns.Add("nzp_act", GenerateScript.GetCellValue(dgv.Rows[row].Cells["nzp_act"])); 
                            break;
                        case ScriptGenerateOper.Delete:
                            break;
                        case ScriptGenerateOper.Update:
                            break;
                        default:
                            return;
                    }
               var res=  GenerateScript.AddValSpecialForRolePagesAndRoleActions(TableName, whereValColumns, valColumns, idColVal, typescr, (int)dgv.Rows[row].Cells["nzp_role"].Value);
                if (res == null)
                {
                    dgv.Rows[row].Cells["forscript"].Value = null;
                }
                return;
            }
            var dataGridViewColumn = dgv.Columns["nzp_page"];
            if (dataGridViewColumn != null && column != dataGridViewColumn.Index) return;
            SetDataSourceToIndividualComboBoxCell();
            return;
            var celVal = (int)dgv.Rows[row].Cells[column].Value;
            var dataTable = from n in dataStore
                            where n.num == celVal
                            select n;
            var comboCell = ((DataGridViewComboBoxCell)dgv.Rows[row].Cells["nzp_act"]);

           
            if ( dataTable.Count()==0)
            {
                SetDataSourceToIndividualComboBoxCell();
                //string commandActString = "WITH selectsour AS  (SELECT a.nzp_act as nzp_act, s.act_name as act_name " +
                //            "FROM " + Tables.actions_show + " a LEFT OUTER JOIN " + Tables.s_actions + " s ON (a.nzp_act=s.nzp_act) WHERE a.cur_page=" + celVal + ") " +
                //            "SELECT selectsour.nzp_act AS id , selectsour.nzp_act||' '||selectsour.act_name AS text " +
                //            "FROM selectsour; ";
                //var dt = ComboBoxBindingSource.Update(commandActString);
                //comboCell.DataSource = dt;
                //dataStore.Add(new DataSourceStorageForComboBoxCell(dt, celVal));
                //comboCell.Value = dt.Rows[0].Field<int>("id");
                //return;
            }
            else
            {
                var sourceDataTable = dataTable.First().dt;
                comboCell.DataSource = sourceDataTable;
                comboCell.Value = sourceDataTable.Rows[0].Field<int>("id");
            }

        }




        public override bool Save(NpgsqlConnection connect, NpgsqlDataAdapter adapter, DataTable dt)
        {
            // Удаление строк
            adapter.DeleteCommand = new NpgsqlCommand("Delete from " + Tables.role_actions + " Where id=@id", connect);
            NpgsqlParameter paramter = adapter.DeleteCommand.Parameters.Add("@id", NpgsqlDbType.Integer);
            paramter.SourceColumn = "id";
            paramter.SourceVersion = DataRowVersion.Original;

            // Обновление данных
            adapter.UpdateCommand = new NpgsqlCommand("UPDATE " + Tables.role_actions + " SET id=@id, nzp_role=@nzp_role,  nzp_page=@nzp_page, nzp_act=@nzp_act, mod_act=@mod_act, " +
             "created_on=@created_on, created_by=@created_by " + "Where id=@id", connect);
            paramter = adapter.UpdateCommand.Parameters.Add("@id", NpgsqlDbType.Integer);
            paramter.SourceColumn = "id";
            paramter = adapter.UpdateCommand.Parameters.Add("@nzp_role", NpgsqlDbType.Bigint);
            paramter.SourceColumn = "nzp_role";
            paramter = adapter.UpdateCommand.Parameters.Add("@nzp_page", NpgsqlDbType.Integer);
            paramter.SourceColumn = "nzp_page";
            paramter = adapter.UpdateCommand.Parameters.Add("@nzp_act", NpgsqlDbType.Integer);
            paramter.SourceColumn = "nzp_act";
            paramter = adapter.UpdateCommand.Parameters.Add("@mod_act", NpgsqlDbType.Integer);
            paramter.SourceColumn = "mod_act";
            paramter = adapter.UpdateCommand.Parameters.Add("@created_on", NpgsqlDbType.Timestamp);
            paramter.SourceColumn = "created_on";
            paramter = adapter.UpdateCommand.Parameters.Add("@created_by", NpgsqlDbType.Integer);
            paramter.SourceColumn = "created_by";

            // Вставка строк id, @id,
            adapter.InsertCommand = new NpgsqlCommand("Insert into " + Tables.role_actions + " ( nzp_role,  nzp_page, nzp_act, mod_act, created_on, created_by) " +
            "values ( @nzp_role,  @nzp_page, @nzp_act, @mod_act, @created_on, @created_by); ", connect);
            //paramter = adapter.InsertCommand.Parameters.Add("@id", NpgsqlDbType.Integer);
            //paramter.SourceColumn = "id";
            paramter = adapter.InsertCommand.Parameters.Add("@nzp_role", NpgsqlDbType.Bigint);
            paramter.SourceColumn = "nzp_role";
            paramter = adapter.InsertCommand.Parameters.Add("@nzp_page", NpgsqlDbType.Integer);
            paramter.SourceColumn = "nzp_page";
            paramter = adapter.InsertCommand.Parameters.Add("@nzp_act", NpgsqlDbType.Integer);
            paramter.SourceColumn = "nzp_act";
            paramter = adapter.InsertCommand.Parameters.Add("@mod_act", NpgsqlDbType.Integer);
            paramter.SourceColumn = "mod_act";
            paramter = adapter.InsertCommand.Parameters.Add("@created_on", NpgsqlDbType.Timestamp);
            paramter.SourceColumn = "created_on";
            paramter = adapter.InsertCommand.Parameters.Add("@created_by", NpgsqlDbType.Integer);
            paramter.SourceColumn = "created_by";

            if (!TransferData.TransferDataDb.UpdateDbTable(adapter, dt)) return false;
            return true;
        }

        public override void SetDefaultValuesAfterRowAdded(DataGridView dgv, int index)
        {
            //dgv.Rows[index].Cells["nzp_page"].Value = 1;
            dgv.Rows[index].Cells["nzp_role"].Value = position;
            dgv.Rows[index].Cells["nzp_page"].Value = DBNull.Value;
            ((DataGridViewComboBoxCell)dgv.Rows[index].Cells["nzp_act"]).DataSource = ComboBoxBindingSource.GetEmptyTable();
            dgv.Rows[index].Cells["nzp_act"].Value = DBNull.Value;
            dgv.Rows[index].Cells["mod_act"].Value = DBNull.Value;
            dgv.Rows[index].Cells["created_on"].Value = DateTime.Now;
            dgv.Rows[index].Cells["created_by"].Value = MainWindow.UserId;
            dgv.Rows[index].Cells["user_name_created"].Value = MainWindow.UserName;
          //  dgv.Rows[index].Cells["changed_by"].Value = DBNull.Value;
        }
  
    }
}
