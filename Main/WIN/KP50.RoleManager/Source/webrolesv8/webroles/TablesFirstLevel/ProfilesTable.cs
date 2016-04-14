using System;
using System.Data;
using System.Windows.Forms;
using Npgsql;
using webroles.Properties;
using webroles.TransferData;

namespace webroles.TablesFirstLevel
{
    class ProfilesTable:TableFirstLevel,INamePosition,IDeletable
    {
        DataGridViewColumn[] columnCollection;
        private const string nodeTreeViewText = "Профили";
        private int position;

        private readonly string selectCommand = "WITH selectcreate as (SELECT p.id, profile_name, db, created_on, created_by, changed_on, changed_by, user_name as user_name_created " +
            "FROM " + Tables.profiles + " p LEFT OUTER JOIN " + Tables.users + " u ON(p.created_by= u.id)) " + 
            "SELECT selectcreate.id, profile_name, db, created_on, created_by, changed_on, changed_by, user_name_created, user_name as user_name_changed " +
            "FROM selectcreate LEFT OUTER JOIN " + Tables.users + " u ON(selectcreate.changed_by= u.id) ORDER BY selectcreate.id; ";

        public override  string NodeTreeViewText
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
                return Tables.profiles;
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
            columnCollection = new DataGridViewColumn[9];
            columnCollection[8] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.profiles, "user_name_changed", Resources.user_name_chang, true, true);
            columnCollection[7] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.profiles, "changed_by", Resources.changed_by, true, false);
            columnCollection[6] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.profiles, "changed_on", Resources.changed_on, true, true);
            columnCollection[5] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.profiles, "user_name_created", Resources.user_name_creat, true, true);
            columnCollection[4] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.profiles, "created_by", Resources.created_by, true, false);
            columnCollection[3] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.profiles, "created_on", Resources.created_on, true, true);
            columnCollection[2] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.profiles, "db", Resources.profilesTable_db, false, true);
            columnCollection[1] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.profiles, "profile_name", Resources.profilesTable_profiles_name, false, true);
            columnCollection[0] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.profiles, "id", Resources.profilesTable_id, true, true);
            columnCollection[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
        }

        public override bool Save(NpgsqlConnection connect, NpgsqlDataAdapter adapter, DataTable dt)
        {
            adapter.DeleteCommand = new NpgsqlCommand("Delete from " + Tables.profiles + " Where id=@id", connect);
            NpgsqlParameter paramter = adapter.DeleteCommand.Parameters.Add("@id", NpgsqlTypes.NpgsqlDbType.Integer);
            paramter.SourceColumn = "id";
            paramter.SourceVersion = DataRowVersion.Original;

            adapter.UpdateCommand = new NpgsqlCommand("Update " + Tables.profiles + " set profile_name=@profile_name, db=@db, created_on=@created_on, created_by=@created_by, changed_on=@changed_on, changed_by=@changed_by where id=@id", connect);
            paramter = adapter.UpdateCommand.Parameters.Add("@id", NpgsqlTypes.NpgsqlDbType.Integer);
            paramter.SourceColumn = "id";
            adapter.UpdateCommand.Parameters.Add("@profile_name", NpgsqlTypes.NpgsqlDbType.Char, 255, "profile_name");
            paramter = adapter.UpdateCommand.Parameters.Add("@db", NpgsqlTypes.NpgsqlDbType.Integer);
            paramter.SourceColumn = "db";
            paramter = adapter.UpdateCommand.Parameters.Add("@created_on", NpgsqlTypes.NpgsqlDbType.Timestamp);
            paramter.SourceColumn = "created_on";
            paramter = adapter.UpdateCommand.Parameters.Add("@created_by", NpgsqlTypes.NpgsqlDbType.Integer);
            paramter.SourceColumn = "created_by";
            paramter = adapter.UpdateCommand.Parameters.Add("@changed_on", NpgsqlTypes.NpgsqlDbType.Timestamp);
            paramter.SourceColumn = "changed_on";
            paramter = adapter.UpdateCommand.Parameters.Add("@changed_by", NpgsqlTypes.NpgsqlDbType.Integer);
            paramter.SourceColumn = "changed_by";

            adapter.InsertCommand = new NpgsqlCommand("Insert into " + Tables.profiles + " ( profile_name,  db,  created_on, created_by, changed_on, changed_by) " +
            "values ( @profile_name,  @db,  @created_on, @created_by, @changed_on, @changed_by); ", connect);
            adapter.InsertCommand.Parameters.Add("@profile_name", NpgsqlTypes.NpgsqlDbType.Char, 200, "profile_name");
            paramter = adapter.InsertCommand.Parameters.Add("@db", NpgsqlTypes.NpgsqlDbType.Integer);
            paramter.SourceColumn = "db";
            paramter = adapter.InsertCommand.Parameters.Add("@created_on", NpgsqlTypes.NpgsqlDbType.Timestamp);
            paramter.SourceColumn = "created_on";
            paramter = adapter.InsertCommand.Parameters.Add("@created_by", NpgsqlTypes.NpgsqlDbType.Integer);
            paramter.SourceColumn = "created_by";
            paramter = adapter.InsertCommand.Parameters.Add("@changed_on", NpgsqlTypes.NpgsqlDbType.Timestamp);
            paramter.SourceColumn = "changed_on";
            paramter = adapter.InsertCommand.Parameters.Add("@changed_by", NpgsqlTypes.NpgsqlDbType.Integer);
            paramter.SourceColumn = "changed_by";

            if (!TransferData.TransferDataDb.UpdateDbTable(adapter,dt)) return false;
            return true;
        }

        public override void SetDefaultValuesAfterRowAdded(DataGridView dgv, int index)
        {
            dgv.Rows[index].Cells["created_on"].Value = DateTime.Now;
            dgv.Rows[index].Cells["created_by"].Value = MainWindow.UserId;
            dgv.Rows[index].Cells["user_name_created"].Value = MainWindow.UserName;
            dgv.Rows[index].Cells["changed_by"].Value = DBNull.Value;
        }

        public string BaseColumn
        {
            get { return "profile_id"; }
        }
        public string GetNamePosition(DataGridViewRow dataRow)
        {
            //var num = dataRow.Field<int>("id").ToString();
            //string text;
            //if (dataRow.Field<string>("profile_name") == null)
            //    text = string.Empty;
            //else
            //    text = dataRow.Field<string>("profile_name").ToString();
            //return "Профиль" + ": " + num + " " + text;
            int num;
            string text;
            if (dataRow != null)
            {
                 num = (int)dataRow.Cells["id"].Value;
                 if (dataRow.Cells["profile_name"].Value!=null)
                 text = dataRow.Cells["profile_name"].Value.ToString(); 
                 else
                     text = string.Empty; 
            }
            else
            {
                position = 0;
                num = PositionDefault;
                text = "тула";
            }
           
            return "Профиль" + ": " + num + " " + text;
        }
        public int PositionDefault
        {
            get { return 1; }
        }
        public int Position
        {
            get { return position; }
            set { position = value; }
        }
        public override void SetDefaultValuesAfterCellChange(DataGridView dgv, int row, int column)
        {
            dgv.Rows[row].Cells["user_name_changed"].Value = MainWindow.UserName;
            dgv.Rows[row].Cells["changed_on"].Value = DateTime.Now;
            dgv.Rows[row].Cells["changed_by"].Value = MainWindow.UserId;
        }

        public bool CheckToAllowDelete(DataGridView dvg)
        {
            if (dvg.CurrentRow == null) return true;
            var posit = dvg.CurrentRow.Cells["id"].Value;
            if (posit == DBNull.Value) return true;
            string[] tablesArr =
            {
                Tables.profile_roles
            };
            string commandText = "SELECT EXISTS (Select id from " + Tables.profile_roles + " where profile_id=" + posit + ");";
            return DBManager.CheckExistsRefernces(commandText, tablesArr, (int)posit);
        }
    }
}
