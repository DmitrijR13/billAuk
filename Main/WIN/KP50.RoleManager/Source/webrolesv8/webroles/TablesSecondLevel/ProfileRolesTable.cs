using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Npgsql;
using NpgsqlTypes;
using webroles.Properties;
using System.Data;
using webroles.TransferData;

namespace webroles.TablesSecondLevel
{
    class ProfileRolesTable:TableSecondLevel
    {
        public ProfileRolesTable(string nameParentTreeViewNode)
        {
            this.nameParentTreeViewNode = nameParentTreeViewNode;
        }
        private readonly string nameParentTreeViewNode;
        List<IObserver> srolesList = new List<IObserver>();
        public List<IObserver> ObserversSrolesTable
        {
            get { return srolesList; }
        }

        DataGridViewColumn[] columnCollection;
        private const string nodeTreeViewText = "Роли профилей";
        string selectCommand = "";// "With selectcreate AS (SELECT profile_roles.id, profile_id, nzp_role, created_on, created_by, user_name as user_name_created " +
//"FROM profile_roles LEFT OUTER JOIN users ON (created_by=users.id)) " +
//"Select selectcreate.id, profile_id, nzp_role, selectcreate.created_on, selectcreate.created_by, user_name_created, profiles.id ||' '|| profile_name as profile_name " +
//"FROM selectcreate LEFT OUTER JOIN profiles ON (selectcreate.profile_id=profiles.id);";
        public override string NameParentTable
        {
            get { return nameParentTreeViewNode; }
        }

        public override string NameParentColumn
        {
            get { return "id"; }
        }

        public override string NameOwnBaseColumn
        {
            get { return "profile_id"; }
        }

        public override void GetCorrespondTable(DataSet dataSet, NpgsqlDataAdapter adapter, NpgsqlConnection connect, int positionParam)
        {
           
                selectCommand = "With selectcreate AS (SELECT p.id, profile_id, nzp_role, created_on, created_by, user_name as user_name_created " +
"FROM " + Tables.profile_roles + " p LEFT OUTER JOIN " + Tables.users + " u ON (created_by=u.id)) " +
"Select selectcreate.id, profile_id, nzp_role, selectcreate.created_on, selectcreate.created_by, user_name_created, pr.id ||' '|| profile_name as profile_name " +
"FROM selectcreate LEFT OUTER JOIN " + Tables.profiles + " pr ON (selectcreate.profile_id=pr.id) WHERE selectcreate.profile_id =" + positionParam + " ORDER BY  selectcreate.id ;";
                dataSet.Tables[Tables.profile_roles].Clear();
                adapter.SelectCommand.CommandText = selectCommand;
                TransferDataDb.Fill(adapter, dataSet.Tables[TableName]);
        }

        private string getSelectCommand()
        {
            return "With selectcreate AS (SELECT p.id, profile_id, nzp_role, created_on, created_by, user_name as user_name_created " +
"FROM " + Tables.profile_roles + " p LEFT OUTER JOIN " + Tables.users + " u ON (created_by=u.id)) " +
"Select selectcreate.id, profile_id, nzp_role, selectcreate.created_on, selectcreate.created_by, user_name_created, pr.id ||' '|| profile_name as profile_name " +
"FROM selectcreate LEFT OUTER JOIN " + Tables.profiles + " pr ON (selectcreate.profile_id=pr.id) WHERE selectcreate.profile_id =" + Position + " ORDER BY  selectcreate.id ;";
        }

        public override string NodeTreeViewText
        {
            get { return nodeTreeViewText; }
        }

        public override string TableName
        {
            get { return Tables.profile_roles; }
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
           // string actionTypesIdItems = "SELECT nzp_page as id, nzp_page||' '|| page_name as text FROM pages ORDER BY id;";
           // var srolesItems = "SELECT nzp_role as id, nzp_role||' '|| role as text FROM s_roles ORDER BY id;";
            // var nzpAct = ComboBoxBindingSource.GetBindingSourceForColumnPages(nzpActItems, false);
            columnCollection = new DataGridViewColumn[7];
            columnCollection[6] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.profile_roles, "user_name_created", Resources.user_name_creat, true, true);
            columnCollection[5] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.profile_roles, "created_by", Resources.created_by, true, false);
            columnCollection[4] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.profile_roles, "created_on", Resources.created_on, true, true);
            columnCollection[3] = CreateDataGridViewColumn.CreateComboBoxColumn<string>("Роль", "nzp_role");
            srolesList.Add(new ComboBoxBindingSource(columnCollection[3], true));
            columnCollection[3].SortMode = DataGridViewColumnSortMode.Automatic;
            columnCollection[2] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.profile_roles, "profile_id", "Код профиля", true, false);
            columnCollection[1] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.profile_roles, "profile_name", "Нименование профиля", true, false);
            columnCollection[0] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.profile_roles, "id", "Код", false, true);
            columnCollection[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
        }

        public override bool Save(NpgsqlConnection connect, NpgsqlDataAdapter adapter, DataTable dt)
        {

            // Удаление строк
            adapter.DeleteCommand = new NpgsqlCommand("Delete from " + Tables.profile_roles + " Where id=@id", connect);
            NpgsqlParameter paramter = adapter.DeleteCommand.Parameters.Add("@id", NpgsqlDbType.Integer);
            paramter.SourceColumn = "id";
            paramter.SourceVersion = DataRowVersion.Original;

            // Обновление данных
            adapter.UpdateCommand = new NpgsqlCommand("UPDATE " + Tables.profile_roles + " SET id=@id, profile_id=@profile_id,  nzp_role=@nzp_role, " +
             "created_on=@created_on, created_by=@created_by " + "Where id=@id", connect);
            paramter = adapter.UpdateCommand.Parameters.Add("@id", NpgsqlDbType.Integer);
            paramter.SourceColumn = "id";
            paramter = adapter.UpdateCommand.Parameters.Add("@profile_id", NpgsqlDbType.Bigint);
            paramter.SourceColumn = "profile_id";
            paramter = adapter.UpdateCommand.Parameters.Add("@nzp_role", NpgsqlDbType.Integer);
            paramter.SourceColumn = "nzp_role";
            paramter = adapter.UpdateCommand.Parameters.Add("@created_on", NpgsqlDbType.Timestamp);
            paramter.SourceColumn = "created_on";
            paramter = adapter.UpdateCommand.Parameters.Add("@created_by", NpgsqlDbType.Integer);
            paramter.SourceColumn = "created_by";
      
            // Вставка строк id, nextval('profile_roles_seq'::regclass),
            adapter.InsertCommand = new NpgsqlCommand("Insert into " + Tables.profile_roles + " ( profile_id, nzp_role, created_on, created_by) " +
            "values ( @profile_id, @nzp_role,  @created_on, @created_by); ", connect);
            paramter = adapter.InsertCommand.Parameters.Add("@profile_id", NpgsqlDbType.Bigint);
            paramter.SourceColumn = "profile_id";
            paramter = adapter.InsertCommand.Parameters.Add("@nzp_role", NpgsqlDbType.Integer);
            paramter.SourceColumn = "nzp_role";
            paramter = adapter.InsertCommand.Parameters.Add("@created_on", NpgsqlDbType.Timestamp);
            paramter.SourceColumn = "created_on";
            paramter = adapter.InsertCommand.Parameters.Add("@created_by", NpgsqlDbType.Integer);
            paramter.SourceColumn = "created_by";
            if (!TransferData.TransferDataDb.UpdateDbTable(adapter, dt)) return false;
            return true;
        }

        public override void SetDefaultValuesAfterRowAdded(DataGridView dgv, int index)
        {
            dgv.Rows[index].Cells["profile_id"].Value = position;
            //dgv.Rows[index].Cells["nzp_page"].Value = DBNull.Value;
            dgv.Rows[index].Cells["nzp_role"].Value = DBNull.Value;
            dgv.Rows[index].Cells["created_on"].Value = DateTime.Now;
            dgv.Rows[index].Cells["created_by"].Value = MainWindow.UserId;
            dgv.Rows[index].Cells["user_name_created"].Value = MainWindow.UserName;
            //dgv.Rows[index].Cells["changed_by"].Value = DBNull.Value;
        }

        public override void SetDefaultValuesAfterCellChange(DataGridView dgv, int row, int column)
        {

        }
    }
}
