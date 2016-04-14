using System.Data;
using System.Net.NetworkInformation;
using System.Windows.Forms;
using Npgsql;
using NpgsqlTypes;
using webroles.Properties;

namespace webroles.TablesFirstLevel
{
    class UsersTable:TableFirstLevel, IEditable
    { 
       static DataGridViewColumn[] columnCollection;
        private const string nodeTreeViewText = "Пользователи";
        private readonly string selectCommand = "SELECT id, login, user_name, is_blocked FROM " + Tables.users + " ORDER BY id; ";

        public override string NodeTreeViewText
        {
            get
            {
                return nodeTreeViewText;
            }
        }

        public override string TableName
        {
            get
            {
                return Tables.users;
            }
        }
        public override string SelectCommand
        {
            get
            {
                return selectCommand;
            }
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
            columnCollection = new DataGridViewColumn[4];
            columnCollection[3] = CreateDataGridViewColumn.CreateCheckBoxColumn(Tables.users, "is_blocked", Resources.usersTable_is_blocked);
            columnCollection[2] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.users, "user_name", Resources.usersTable_user_name, true, true);
            columnCollection[1] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.users, "login", Resources.usersTable_login, true, true);
            columnCollection[0] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.users, "id", Resources.usersTable_id, true, true);
            columnCollection[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
        }

        public override bool Save(NpgsqlConnection connect, NpgsqlDataAdapter adapter, DataTable dt)
        {
            adapter.DeleteCommand = new NpgsqlCommand("Delete from " + Tables.users + " Where id=@id", connect);
            NpgsqlParameter paramter = adapter.DeleteCommand.Parameters.Add("@id", NpgsqlDbType.Integer);
            paramter.SourceColumn = "id";
            paramter.SourceVersion = DataRowVersion.Original;

            adapter.UpdateCommand = new NpgsqlCommand("Update " + Tables.users + " set user_name=@user_name, is_blocked=@is_blocked where id=@id", connect);
            adapter.UpdateCommand.Parameters.Add("@user_name", NpgsqlDbType.Char, 255, "user_name");
            paramter = adapter.UpdateCommand.Parameters.Add("@is_blocked", NpgsqlDbType.Integer);
            paramter.SourceColumn = "is_blocked";
            paramter = adapter.UpdateCommand.Parameters.Add("@id", NpgsqlDbType.Integer);
            paramter.SourceColumn = "id";
            adapter.InsertCommand = null;
            if (!TransferData.TransferDataDb.UpdateDbTable(adapter, dt)) return false;
            return true;
        }

        public override void SetDefaultValuesAfterRowAdded(DataGridView dgv, int index)
        {
        }


        public override void SetDefaultValuesAfterCellChange(DataGridView dgv, int row, int column)
        {

        }

        public bool AllowEdit(EditOperations operation)
        {
            if (operation != EditOperations.Add && operation != EditOperations.Delete) return true;
            MessageBox.Show("В данной таблице нельзя добавлять и удалять строки", "Только изменение",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }
    }
}
