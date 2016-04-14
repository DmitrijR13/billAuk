using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Npgsql;
using NpgsqlTypes;
using webroles.GenerateScriptTable;
using webroles.Properties;
using System.Data;
using webroles.TransferData;

namespace webroles.TablesSecondLevel
{
    class RolesKeyTable:TableSecondLevel
    {

        public RolesKeyTable(string nameParentTreeViewNode)
        {
            this.nameParentTreeViewNode = nameParentTreeViewNode;
        }
        private readonly string nameParentTreeViewNode;
        public List<IObserver> NzpPageList = new List<IObserver>();
        DataGridViewColumn[] columnCollection;
        private const string nodeTreeViewText = "Ключи ролей";

        string selectCommand = "";//"WITH selectcreate AS (SELECT nzp_rlsv, nzp_role, tip, kod, created_on, created_by, user_name as user_name_created " +
//"FROM roleskey LEFT OUTER JOIN users ON (created_by=users.id)) " +
//"SELECT selectcreate.nzp_rlsv, selectcreate.nzp_role, selectcreate.tip, selectcreate.kod, selectcreate.created_on, selectcreate.created_by, user_name_created, " +
//"s_roles.nzp_role||' '||s_roles.role as role_name " +
//"FROM selectcreate LEFT OUTER JOIN s_roles ON (selectcreate.nzp_role=s_roles.nzp_role);";
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
                selectCommand = "WITH selectcreate AS (SELECT nzp_rlsv, nzp_role, tip, kod, created_on, created_by, user_name as user_name_created " +
"FROM " + Tables.roleskey + " LEFT OUTER JOIN " + Tables.users + " ON (created_by=users.id)) " +
"SELECT selectcreate.nzp_rlsv, selectcreate.nzp_role, selectcreate.tip, selectcreate.kod, selectcreate.created_on, selectcreate.created_by, user_name_created, " +
"s.nzp_role||' '||s.role as role_name " +
"FROM selectcreate LEFT OUTER JOIN " + Tables.s_roles + " s ON (selectcreate.nzp_role=s.nzp_role) WHERE selectcreate.nzp_role =" + positionParam + " ORDER BY selectcreate.nzp_rlsv;";
                dataSet.Tables[Tables.roleskey].Clear();
                adapter.SelectCommand.CommandText = selectCommand;
                TransferDataDb.Fill(adapter, dataSet.Tables[TableName]);
        }

        public override string NodeTreeViewText
        {
            get { return nodeTreeViewText; }
        }

        public override string TableName
        {
            get { return Tables.roleskey; }
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
            return
                "WITH selectcreate AS (SELECT nzp_rlsv, nzp_role, tip, kod, created_on, created_by, user_name as user_name_created " +
                "FROM " + Tables.roleskey + " LEFT OUTER JOIN " + Tables.users + " ON (created_by=users.id)) " +
                "SELECT selectcreate.nzp_rlsv, selectcreate.nzp_role, selectcreate.tip, selectcreate.kod, selectcreate.created_on, selectcreate.created_by, user_name_created, " +
                "s.nzp_role||' '||s.role as role_name " +
                "FROM selectcreate LEFT OUTER JOIN " + Tables.s_roles +
                " s ON (selectcreate.nzp_role=s.nzp_role) WHERE selectcreate.nzp_role =" + Position +
                " ORDER BY selectcreate.nzp_rlsv;";
        }

        public override void CreateColumns()
        {
             const string tipItems = "SELECT  id, id ||' '|| setting_name as text FROM role_setting_types ORDER BY id;";
            // var actionTypes = ComboBoxBindingSource.GetBindingSourceForColumnPages(actionTypesIdItems, false);
            // var nzpAct = ComboBoxBindingSource.GetBindingSourceForColumnPages(nzpActItems, false);
            columnCollection = new DataGridViewColumn[9];
            columnCollection[8] = CreateDataGridViewColumn.CreateComboBoxColumn("Для скрипта");
            columnCollection[7] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.roleskey, "user_name_created", Resources.user_name_creat, true, true);
            columnCollection[6] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.roleskey, "created_by", Resources.created_by, true, false);
            columnCollection[5] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.roleskey, "created_on", Resources.created_on, true, true);
            columnCollection[4] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.roleskey, "kod", "Порядковый номер", false, true);
            columnCollection[3] = CreateDataGridViewColumn.CreateComboBoxColumn<string>( "Тип", "tip");
            var cbc = new ComboBoxBindingSource(tipItems, columnCollection[3], true);
            cbc.update();
            columnCollection[2] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.roleskey, "role_name", "Наименование роли", true, false);
            columnCollection[1] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.roleskey, "nzp_role", "Текущая роль", true, false);
            columnCollection[0] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.roleskey, "nzp_rlsv", Resources.actions_showTable_id, true, true);
            columnCollection[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
        }

        public override bool Save(NpgsqlConnection connect, NpgsqlDataAdapter adapter, DataTable dt)
        {
            // Удаление строк
            adapter.DeleteCommand = new NpgsqlCommand("Delete from " + Tables.roleskey + "  Where nzp_rlsv=@nzp_rlsv", connect);
            NpgsqlParameter paramter = adapter.DeleteCommand.Parameters.Add("@nzp_rlsv", NpgsqlDbType.Integer);
            paramter.SourceColumn = "nzp_rlsv";
            paramter.SourceVersion = DataRowVersion.Original;

            // Обновление данных
            adapter.UpdateCommand = new NpgsqlCommand("UPDATE " + Tables.roleskey + "  SET nzp_rlsv=@nzp_rlsv, nzp_role=@nzp_role,  tip=@tip, kod=@kod, " +
             "created_on=@created_on, created_by=@created_by " + "Where nzp_rlsv=@nzp_rlsv", connect);
            paramter = adapter.UpdateCommand.Parameters.Add("@nzp_rlsv", NpgsqlDbType.Integer);
            paramter.SourceColumn = "nzp_rlsv";
            paramter = adapter.UpdateCommand.Parameters.Add("@nzp_role", NpgsqlDbType.Bigint);
            paramter.SourceColumn = "nzp_role";
            paramter = adapter.UpdateCommand.Parameters.Add("@tip", NpgsqlDbType.Integer);
            paramter.SourceColumn = "tip";
            paramter = adapter.UpdateCommand.Parameters.Add("@kod", NpgsqlDbType.Integer);
            paramter.SourceColumn = "kod";
            paramter = adapter.UpdateCommand.Parameters.Add("@created_on", NpgsqlDbType.Timestamp);
            paramter.SourceColumn = "created_on";
            paramter = adapter.UpdateCommand.Parameters.Add("@created_by", NpgsqlDbType.Integer);
            paramter.SourceColumn = "created_by";

            // Вставка строк nzp_rlsv, @nzp_rlsv,
            adapter.InsertCommand = new NpgsqlCommand("Insert into " + Tables.roleskey + "  ( nzp_role,  tip, kod, created_on, created_by) " +
            "values ( @nzp_role,  @tip, @kod, @created_on, @created_by); ", connect);
            //paramter = adapter.InsertCommand.Parameters.Add("@nzp_rlsv", NpgsqlDbType.Integer);
            //paramter.SourceColumn = "nzp_rlsv";
            paramter = adapter.InsertCommand.Parameters.Add("@nzp_role", NpgsqlDbType.Bigint);
            paramter.SourceColumn = "nzp_role";
            paramter = adapter.InsertCommand.Parameters.Add("@tip", NpgsqlDbType.Integer);
            paramter.SourceColumn = "tip";
            paramter = adapter.InsertCommand.Parameters.Add("@kod", NpgsqlDbType.Integer);
            paramter.SourceColumn = "kod";
            paramter = adapter.InsertCommand.Parameters.Add("@created_on", NpgsqlDbType.Timestamp);
            paramter.SourceColumn = "created_on";
            paramter = adapter.InsertCommand.Parameters.Add("@created_by", NpgsqlDbType.Integer);
            paramter.SourceColumn = "created_by";
            if (!TransferData.TransferDataDb.UpdateDbTable(adapter,  dt)) return false;
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
                    string addition_by = getAdditRoles(nzp_role);
                    // обновить s_roles
                    sql = "update " + DBManager.sDefaultSchema + Tables.s_roles + " set addition_by='" + addition_by + "' where nzp_role=" + nzp_role;
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
              if (connection!=null) connection.Close();
            }
        }

        private string getAdditRoles(int nzp_role)
        {
            string sql = "select nzp_role from " + DBManager.sDefaultSchema + Tables.roleskey + " where tip=105 and kod=" + nzp_role;
            Returns ret;
            IDbConnection connection = ConnectionToPostgreSqlDb.GetConnection();
            IDataReader reader = null;
            List<string> listRole= new List<string>();
            try
            {
                connection.Open();
                reader = TransferDataDb.ExecuteReader(sql, out ret, connection);
                while (reader.Read())
                {
                    if (reader["nzp_role"]==DBNull.Value) continue;
                    listRole.Add(reader["nzp_role"].ToString());
                }
                return String.Join(",",listRole);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return "";
            }
            finally
            {
                if (reader != null) reader.Close();
                if (connection!=null) connection.Close();
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

            dgv.Rows[index].Cells["nzp_role"].Value = position;
            dgv.Rows[index].Cells["tip"].Value = DBNull.Value;
            dgv.Rows[index].Cells["kod"].Value = DBNull.Value;
            dgv.Rows[index].Cells["created_on"].Value = DateTime.Now;
            dgv.Rows[index].Cells["created_by"].Value = MainWindow.UserId;
            dgv.Rows[index].Cells["user_name_created"].Value = MainWindow.UserName;
        }

        public override void SetDefaultValuesAfterCellChange(DataGridView dgv, int row, int column)
        {
            var dataGridViewColumn = dgv.Columns["forscript"];
            if (dataGridViewColumn != null && dataGridViewColumn.Index == column)
            {
                // ключ значения для вставки
                Dictionary<string, string> valColumns = new Dictionary<string, string>
                {
                    {"nzp_role", GenerateScript.GetCellValue(dgv.Rows[row].Cells["nzp_role"])},
                     {"tip", GenerateScript.GetCellValue(dgv.Rows[row].Cells["tip"])},
                      {"kod", GenerateScript.GetCellValue(dgv.Rows[row].Cells["kod"])}
                };
                // ключ значение базовой колонки
                Dictionary<string, int> idColVal = new Dictionary<string, int>
                {
                    {"nzp_rlsv", (int) dgv.Rows[row].Cells["nzp_rlsv"].Value}
                };
                // ключ значение для where
                Dictionary<string, string> whereDict = new Dictionary<string, string> 
                {{"nzp_role", GenerateScript.GetCellValue(dgv.Rows[row].Cells["nzp_role"])}, 
                {"tip",GenerateScript.GetCellValue(dgv.Rows[row].Cells["tip"])},
                {"kod",  GenerateScript.GetCellValue(dgv.Rows[row].Cells["kod"])}};
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
                            break;
                        case ScriptGenerateOper.Update:
                            MessageBox.Show("Строки этой таблицы обновлять нельзя!");
                            dgv.Rows[row].Cells["forscript"].Value = null;
                            typescr = ScriptGenerateOper.none;
                            break;
                        case ScriptGenerateOper.Delete:
                            break;
                    }
                
                GenerateScript.AddVal(TableName, valColumns, whereDict, idColVal, typescr, (int)dgv.Rows[row].Cells["nzp_role"].Value);
            }
        }
    }
}
