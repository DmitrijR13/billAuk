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
    class TroleMergingTable:TableSecondLevel
    {
        public TroleMergingTable(string nameParentTreeViewNode)
        {
            this.nameParentTreeViewNode = nameParentTreeViewNode;
        }
        private readonly string nameParentTreeViewNode;
        readonly List<IObserver> nzpRoleList = new List<IObserver>();
        public List<IObserver> ObserversSrolesTable
        {
            get { return nzpRoleList; }
        }
        DataGridViewColumn[] columnCollection;
        private const string nodeTreeViewText = "Слияние ролей";
        private const string tableName = "t_role_merging";
        private string selectCommand = "";// "SELECT t_role_merging.id, nzp_role_from, nzp_role_to, created_on, created_by, user_name as user_name_created " +
        //       "FROM t_role_merging LEFT OUTER JOIN users ON (created_by=users.id);";
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
                selectCommand = "SELECT t_role_merging.id, nzp_role_from, nzp_role_to, created_on, created_by, user_name as user_name_created " +
               "FROM t_role_merging LEFT OUTER JOIN users ON (created_by=users.id) WHERE nzp_role_from =" + positionParam + " ORDER BY t_role_merging.id ;";
                dataSet.Tables[tableName].Clear();
                adapter.SelectCommand.CommandText = selectCommand;
                TransferDataDb.Fill(adapter, dataSet.Tables[TableName]);
        }

        public override string NodeTreeViewText
        {
            get { return nodeTreeViewText; }
        }

        public override string TableName
        {
            get { return tableName; }
        }

        public override string SelectCommand
        {
            get { return getSelectCommand(); }
        }

        private string getSelectCommand()
        {
            return "SELECT t_role_merging.id, nzp_role_from, nzp_role_to, created_on, created_by, user_name as user_name_created " +
               "FROM t_role_merging LEFT OUTER JOIN users ON (created_by=users.id) WHERE nzp_role_from =" + Position + " ORDER BY t_role_merging.id ;";
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
            columnCollection = new DataGridViewColumn[6];
            columnCollection[5] = CreateDataGridViewColumn.CreateTextBoxColumn(tableName, "user_name_created", Resources.user_name_creat, true, true);
            columnCollection[4] = CreateDataGridViewColumn.CreateTextBoxColumn(tableName, "created_by", Resources.created_by, true, false);
            columnCollection[3] = CreateDataGridViewColumn.CreateTextBoxColumn(tableName, "created_on", Resources.created_on, true, true);
            columnCollection[2] = CreateDataGridViewColumn.CreateComboBoxColumn<string>("К роли", "nzp_role_to");
            nzpRoleList.Add(new ComboBoxBindingSource( columnCollection[2], true));
            columnCollection[1] = CreateDataGridViewColumn.CreateTextBoxColumn(tableName, "nzp_role_from", "От роли", true, false);
           // ColumnCollection[1] = CreateDataGridViewColumn.CreateComboBoxColumn<string>("От роли", "nzp_role_from");
          //  nzpRoleList.Add(new ComboBoxBindingSource(ColumnCollection[1], true));
            columnCollection[0] = CreateDataGridViewColumn.CreateTextBoxColumn(tableName, "id", "Код", false, true);
            columnCollection[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
            //ColumnCollection[0] = CreateDataGridViewColumn.CreateTextBoxColumn(tableName, "id", Resources.actions_showTable_id, true, true);
        }

        public override bool Save(NpgsqlConnection connect, NpgsqlDataAdapter adapter, DataTable dt)
        {
  
            // Удаление строк
            adapter.DeleteCommand = new NpgsqlCommand("Delete from t_role_merging Where id=@id", connect);
            NpgsqlParameter paramter = adapter.DeleteCommand.Parameters.Add("@id", NpgsqlDbType.Integer);
            paramter.SourceColumn = "id";
            paramter.SourceVersion = DataRowVersion.Original;

            // Обновление данных
            adapter.UpdateCommand = new NpgsqlCommand("UPDATE t_role_merging SET nzp_role_from=@nzp_role_from,  nzp_role_to=@nzp_role_to, " +
             "created_on=@created_on, created_by=@created_by " + "Where id=@id", connect);
            paramter = adapter.UpdateCommand.Parameters.Add("@id", NpgsqlDbType.Integer);
            paramter.SourceColumn = "id";
            paramter = adapter.UpdateCommand.Parameters.Add("@nzp_role_from", NpgsqlDbType.Bigint);
            paramter.SourceColumn = "nzp_role_from";
            paramter = adapter.UpdateCommand.Parameters.Add("@nzp_role_to", NpgsqlDbType.Integer);
            paramter.SourceColumn = "nzp_role_to";
            paramter = adapter.UpdateCommand.Parameters.Add("@created_on", NpgsqlDbType.Timestamp);
            paramter.SourceColumn = "created_on";
            paramter = adapter.UpdateCommand.Parameters.Add("@created_by", NpgsqlDbType.Integer);
            paramter.SourceColumn = "created_by";

            // Вставка строк 
            adapter.InsertCommand = new NpgsqlCommand("Insert into t_role_merging (nzp_role_from, nzp_role_to, created_on, created_by) " +
            "values (  @nzp_role_from, @nzp_role_to,  @created_on, @created_by); ", connect);
            paramter = adapter.InsertCommand.Parameters.Add("@nzp_role_from", NpgsqlDbType.Bigint);
            paramter.SourceColumn = "nzp_role_from";
            paramter = adapter.InsertCommand.Parameters.Add("@nzp_role_to", NpgsqlDbType.Integer);
            paramter.SourceColumn = "nzp_role_to";
            paramter = adapter.InsertCommand.Parameters.Add("@created_on", NpgsqlDbType.Timestamp);
            paramter.SourceColumn = "created_on";
            paramter = adapter.InsertCommand.Parameters.Add("@created_by", NpgsqlDbType.Integer);
            paramter.SourceColumn = "created_by";
            if (!TransferData.TransferDataDb.UpdateDbTable(adapter, dt)) return false;
            return updateMergeAndAddition();
        }

        // Обновить колонки  Дополнительная роль у
        private bool updateMergeAndAddition()
        {
            string sql = "select nzp_role from " + DBManager.sDefaultSchema + Tables.s_roles;
            Returns ret;
            IDbConnection connection = ConnectionToPostgreSqlDb.GetConnection();
            try
            {
                DataTable nzp_role_table = getRolesFromSRoles();
                if (nzp_role_table == null) return false;
                connection.Open();
                foreach (DataRow nzp_role_row in nzp_role_table.Rows)
                {
                    int nzp_role = nzp_role_row.Field<int>("nzp_role");

                    // получить список доп. ролей
                    string merge_to = getMergeRoles(nzp_role);
                    // обновить s_roles
                    sql = "update " + DBManager.sDefaultSchema + Tables.s_roles + " set merge_to='" + merge_to + "' where nzp_role=" + nzp_role;
                    ret = TransferDataDb.ExecSQL(sql, connection);
                    if (!ret.Result) return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }
            finally
            {
                if (connection != null) connection.Close();
            }
        }

        private string getMergeRoles(int nzp_role)
        {
            string sql =  "select nzp_role_to as nzp_role from " + DBManager.sDefaultSchema + Tables.t_role_merging + " where nzp_role_from=" + nzp_role;
            Returns ret;
            IDbConnection connection = ConnectionToPostgreSqlDb.GetConnection();
            IDataReader reader = null;
            List<string> listRole = new List<string>();
            try
            {
                connection.Open();
                reader = TransferDataDb.ExecuteReader(sql, out ret, connection);
                while (reader.Read())
                {
                    if (reader["nzp_role"] == DBNull.Value) continue;
                    listRole.Add(reader["nzp_role"].ToString());
                }
                return String.Join(",", listRole);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return "";
            }
            finally
            {
                if (reader != null) reader.Close();
                if (connection != null) connection.Close();
            }
        }

        private DataTable getRolesFromSRoles()
        {
            string sql = "select nzp_role from " + DBManager.sDefaultSchema + Tables.s_roles;
            Returns ret;
            IDbConnection connectionMain = ConnectionToPostgreSqlDb.GetConnection();
            IDataReader readerMain = null;
            DataTable nzp_role_table = null;
            try
            {
                connectionMain.Open();
                // для каждой роли таблицы s_roles
                readerMain = TransferDataDb.ExecuteReader(sql, out ret, connectionMain);
                nzp_role_table = new DataTable();
                nzp_role_table.Load(readerMain);
                connectionMain.Close();
                return nzp_role_table;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return nzp_role_table;
            }
            finally
            {
                if (readerMain != null) readerMain.Close();
                connectionMain.Close();
            }
        }


        public override void SetDefaultValuesAfterRowAdded(DataGridView dgv, int index)
        {
            if (dgv.Rows.Count == 0) return;
            dgv.Rows[index].Cells["created_on"].Value = DateTime.Now;
            dgv.Rows[index].Cells["created_by"].Value = MainWindow.UserId;
            dgv.Rows[index].Cells["user_name_created"].Value = MainWindow.UserName;
            dgv.Rows[index].Cells["nzp_role_from"].Value=position;
            dgv.Rows[index].Cells["nzp_role_to"].Value = DBNull.Value;
        }

        public override void SetDefaultValuesAfterCellChange(DataGridView dgv, int row, int column)
        {

        }
    }
}
