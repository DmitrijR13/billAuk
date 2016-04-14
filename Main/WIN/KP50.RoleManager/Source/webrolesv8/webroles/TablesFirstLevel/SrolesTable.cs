using System;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using Npgsql;
using webroles.GenerateScriptTable;
using webroles.Properties;
using System.Collections.Generic;
using webroles.TablesSecondLevel;
using webroles.TransferData;
using webroles.Windows;

namespace webroles.TablesFirstLevel
{
    class SrolesTable:TableFirstLevel,ISubject,INamePosition,IDeletable
    {
        DataGridViewColumn[] columnCollection;
        readonly List<IObserver> observers = new List<IObserver>();
        private int position;
        private const string nodeTreeViewText = "Роли";
        readonly List<IObserver> nzpPageList = new List<IObserver>();
        public List<IObserver> ObserversPagesTable
        {
            get { return nzpPageList; }
        }

        private  string selectCommand = "WITH selectcreate as (SELECT nzp_role, role, page_url, sort, is_dev, is_subsystem, created_on, created_by, changed_on, changed_by," +
                                        " user_name as user_name_created, merge_to, addition_by, comment " +
            "FROM " + Tables.s_roles + " s LEFT OUTER JOIN " + Tables.users + " u ON(s.created_by= u.id)) " +
            "SELECT nzp_role, role, page_url, sort, is_dev, is_subsystem, created_on, created_by, changed_on, changed_by, user_name_created, user_name as user_name_changed,  merge_to, addition_by, comment " +
            "FROM selectcreate LEFT OUTER JOIN " + Tables.users + " u ON(selectcreate.changed_by= u.id) ORDER BY selectcreate.nzp_role; ";

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
                return Tables.s_roles;
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
            columnCollection = new DataGridViewColumn[16];
            columnCollection[15] = CreateDataGridViewColumn.CreateComboBoxColumn("Для скрипта");
            columnCollection[14] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.s_roles, "user_name_changed", Resources.user_name_chang, true, true);
            columnCollection[13] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.s_roles, "changed_by", Resources.changed_by, true, false);
            columnCollection[12] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.s_roles, "changed_on", Resources.changed_on, true, true);
            columnCollection[11] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.s_roles, "user_name_created", Resources.user_name_creat, true, true);
            columnCollection[10] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.s_roles, "created_by", Resources.created_by, true, false);
            columnCollection[9] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.s_roles, "created_on", Resources.created_on, true, true);
            columnCollection[8] = CreateDataGridViewColumn.CreateCheckBoxColumn(Tables.s_roles, "is_subsystem", Resources.s_rolesTable_is_subsystem);
            columnCollection[7] = CreateDataGridViewColumn.CreateCheckBoxColumn(Tables.s_roles, "is_dev", Resources.is_dev);
            columnCollection[6] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.s_roles, "sort", Resources.s_rolesTable_sort, false, true);
            columnCollection[5] = CreateDataGridViewColumn.CreateComboBoxColumn<string>("Текущая страница", "page_url");
            nzpPageList.Add(new ComboBoxBindingSource(columnCollection[5], true, "", false, true));
            columnCollection[4] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.s_roles, "comment", "Комментарий", false, true);
            columnCollection[3] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.s_roles, "addition_by", "Дополнительная у ролей", true, true);
            columnCollection[2] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.s_roles, "merge_to", "Вливается в роли", true, true);
            columnCollection[1] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.s_roles, "role", Resources.s_rolesTable_role, false, true);
            columnCollection[0] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.s_roles, "nzp_role", Resources.s_rolesTable_nzp_role, true, true);
            columnCollection[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
        }

        public override bool Save(NpgsqlConnection connect, NpgsqlDataAdapter adapter, DataTable dt)
        {
            adapter.DeleteCommand = new NpgsqlCommand("Delete from " + Tables.s_roles + " Where nzp_role=@nzp_role", connect);
            NpgsqlParameter paramter = adapter.DeleteCommand.Parameters.Add("@nzp_role", NpgsqlTypes.NpgsqlDbType.Integer);
            paramter.SourceColumn = "nzp_role";
            paramter.SourceVersion = DataRowVersion.Original;

            adapter.UpdateCommand = new NpgsqlCommand("Update " + Tables.s_roles + " set nzp_role=@nzp_role, role=@role, page_url=@page_url, sort=@sort, is_dev=@is_dev, is_subsystem=@is_subsystem, " +
            " created_on=@created_on, created_by=@created_by, changed_on=@changed_on, changed_by=@changed_by, comment=@comment where nzp_role=@nzp_role; ", connect);
            paramter = adapter.UpdateCommand.Parameters.Add("@nzp_role", NpgsqlTypes.NpgsqlDbType.Integer);
            paramter.SourceColumn = "nzp_role";
            paramter.SourceVersion = DataRowVersion.Original;
            adapter.UpdateCommand.Parameters.Add("@role", NpgsqlTypes.NpgsqlDbType.Char, 255, "role");
            paramter = adapter.UpdateCommand.Parameters.Add("@page_url", NpgsqlTypes.NpgsqlDbType.Integer);
            paramter.SourceColumn = "page_url";
            paramter = adapter.UpdateCommand.Parameters.Add("@sort", NpgsqlTypes.NpgsqlDbType.Integer);
            paramter.SourceColumn = "sort";
            paramter = adapter.UpdateCommand.Parameters.Add("@is_dev", NpgsqlTypes.NpgsqlDbType.Integer);
            paramter.SourceColumn = "is_dev";
            paramter = adapter.UpdateCommand.Parameters.Add("@is_subsystem", NpgsqlTypes.NpgsqlDbType.Integer);
            paramter.SourceColumn = "is_subsystem";
            paramter = adapter.UpdateCommand.Parameters.Add("@created_on", NpgsqlTypes.NpgsqlDbType.Timestamp);
            paramter.SourceColumn = "created_on";
            paramter = adapter.UpdateCommand.Parameters.Add("@created_by", NpgsqlTypes.NpgsqlDbType.Integer);
            paramter.SourceColumn = "created_by";
            paramter = adapter.UpdateCommand.Parameters.Add("@changed_on", NpgsqlTypes.NpgsqlDbType.Timestamp);
            paramter.SourceColumn = "changed_on";
            paramter = adapter.UpdateCommand.Parameters.Add("@changed_by", NpgsqlTypes.NpgsqlDbType.Integer);
            paramter.SourceColumn = "changed_by";
            adapter.UpdateCommand.Parameters.Add("@comment", NpgsqlTypes.NpgsqlDbType.Varchar, 255, "comment");
 
            adapter.InsertCommand = new NpgsqlCommand("Insert into " + Tables.s_roles + " ( role, page_url, sort, is_dev, is_subsystem, created_on, created_by, changed_on, changed_by, comment ) " +
           "Values (@role, @page_url, @sort, @is_dev, @is_subsystem, @created_on, @created_by, @changed_on, @changed_by, @comment); ", connect);
            adapter.InsertCommand.Parameters.Add("@role", NpgsqlTypes.NpgsqlDbType.Char, 255, "role");
            paramter = adapter.InsertCommand.Parameters.Add("@page_url", NpgsqlTypes.NpgsqlDbType.Integer);
            paramter.SourceColumn = "page_url";
            paramter = adapter.InsertCommand.Parameters.Add("@sort", NpgsqlTypes.NpgsqlDbType.Integer);
            paramter.SourceColumn = "sort";
            paramter = adapter.InsertCommand.Parameters.Add("@is_dev", NpgsqlTypes.NpgsqlDbType.Integer);
            paramter.SourceColumn = "is_dev";
            paramter = adapter.InsertCommand.Parameters.Add("@is_subsystem", NpgsqlTypes.NpgsqlDbType.Integer);
            paramter.SourceColumn = "is_subsystem";
            paramter = adapter.InsertCommand.Parameters.Add("@created_on", NpgsqlTypes.NpgsqlDbType.Timestamp);
            paramter.SourceColumn = "created_on";
            paramter = adapter.InsertCommand.Parameters.Add("@created_by", NpgsqlTypes.NpgsqlDbType.Integer);
            paramter.SourceColumn = "created_by";
            paramter = adapter.InsertCommand.Parameters.Add("@changed_on", NpgsqlTypes.NpgsqlDbType.Timestamp);
            paramter.SourceColumn = "changed_on";
            paramter = adapter.InsertCommand.Parameters.Add("@changed_by", NpgsqlTypes.NpgsqlDbType.Integer);
            paramter.SourceColumn = "changed_by";
            adapter.InsertCommand.Parameters.Add("@comment", NpgsqlTypes.NpgsqlDbType.Varchar, 255, "comment");
            if (!TransferData.TransferDataDb.UpdateDbTable(adapter, dt)) return false;
            NotifyObservers();
            return true;
        }


        public override void SetDefaultValuesAfterRowAdded(DataGridView dgv, int index)
        {

            dgv.Rows[index].Cells["is_dev"].Value = 1;
            dgv.Rows[index].Cells["created_on"].Value = DateTime.Now;
            dgv.Rows[index].Cells["created_by"].Value = MainWindow.UserId;
            dgv.Rows[index].Cells["user_name_created"].Value = MainWindow.UserName;
            dgv.Rows[index].Cells["changed_by"].Value = DBNull.Value;
            dgv.Rows[index].Cells["changed_by"].Value = DBNull.Value;

        }
        public string BaseColumn
        {
            get { return "nzp_role"; }
        }

        public void AddObservers(List<IObserver> obsrvs)
        {
            observers.AddRange(obsrvs);
        }

        public void NotifyObservers()
        {
            var dt = ComboBoxBindingSource.Update("SELECT nzp_role as id, nzp_role||' '|| role as text FROM " + Tables.s_roles + " ORDER BY id;");
            for (int i = 0; i < observers.Count; i++)
            {
                if (!observers[i].AdditionalContition)
                    observers[i].update(dt);
                else
                    observers[i].update(ComboBoxBindingSource.Update(observers[i].AdditionalContotionString));
            }
        }

        public string GetNamePosition(DataGridViewRow dataRow)
        {
            //var num = dataRow.Field<int>("nzp_role").ToString();
            //string text;
            //if (dataRow.Field<string>("role") == null)
            //    text = string.Empty;
            //else
            //    text = dataRow.Field<string>("role").ToString();
            //return "Роль" + ": " + num + " " + text;
            int num;
            string text;
            if (dataRow != null)
            {
                 num = (int) dataRow.Cells["nzp_role"].Value;
                if (dataRow.Cells["role"].Value != null)
                {
                    text = dataRow.Cells["role"].Value.ToString();
                }
                else
                 text = string.Empty;
            }
            else
            {
                position = 0;
                num = 10;
              text = "Картотека";
            }
            return "Роль" + ": " + num + " " + text;
        }
        public int PositionDefault
        {
            get { return 10; }
        }
        public int Position
        {
            get { return position; }
            set { position = value; }
        }
        public override void SetDefaultValuesAfterCellChange(DataGridView dgv, int row,int column)
        {
            var dataGridViewColumn = dgv.Columns["forscript"];
            if (dataGridViewColumn != null && dataGridViewColumn.Index == column)
            {
                // ключ значения для вставки
                Dictionary<string, string> valColumns = new Dictionary<string, string>
            {
                {"role", GenerateScript.GetCellValue(dgv.Rows[row].Cells["role"])},
                {"page_url", GenerateScript.GetCellValue(dgv.Rows[row].Cells["page_url"])},
                    {"sort", GenerateScript.GetCellValue(dgv.Rows[row].Cells["sort"])}
            };
                // ключ значение базовой колонки
                Dictionary<string, int> idColVal = new Dictionary<string, int>
            {
                {"nzp_role", (int) dgv.Rows[row].Cells["nzp_role"].Value}
            };
                // ключ значение для where
                Dictionary<string, string> whereDict = new Dictionary<string, string> 
                { { "nzp_role", GenerateScript.GetCellValue(dgv.Rows[row].Cells["nzp_role"]) } };
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
                            break;
                        case ScriptGenerateOper.Delete:
                            break;
                        case ScriptGenerateOper.Update:
                            break;
                        default:
                            return;
                    }
                var res = GenerateScript.AddValSpecialForRolePagesAndRoleActions(TableName, whereDict, valColumns, idColVal, typescr, (int)dgv.Rows[row].Cells["nzp_role"].Value, true);
                if (res == null)
                {
                    dgv.Rows[row].Cells["forscript"].Value = null;
                }
                return;
            }
            dgv.Rows[row].Cells["user_name_changed"].Value = MainWindow.UserName;
            dgv.Rows[row].Cells["changed_on"].Value = DateTime.Now;
            dgv.Rows[row].Cells["changed_by"].Value = MainWindow.UserId;
        }

        public static Dictionary<int,string> CheckRoles(int nzp_role, bool isSroles)
        {
            Dictionary<int, string> roleskeyList = new Dictionary<int, string>();
            try
            {
                string selectFromSRoles = "select nzp_role, role from s_roles where is_subsystem=1 and nzp_role=" + nzp_role;
                roleskeyList=doCheckMergeOrAddition(selectFromSRoles);
                if (roleskeyList.Count > 0)
                {
                    return roleskeyList;
                }
                // Проверить, является ли она дополнительной ролью
                string selectFromRolesKey = "select  max(kod) as nzp_role, sr.role from " + DBManager.sDefaultSchema + Tables.roleskey + " r ," +
                                            DBManager.sDefaultSchema + Tables.s_roles + " sr  where kod=sr.nzp_role and  kod=" + nzp_role + " group by sr.nzp_role";
                roleskeyList = doCheckMergeOrAddition(selectFromRolesKey);
                if (roleskeyList.Count != 0)
                {
                    return roleskeyList;
                }
                // Извлечем те роли, в которые вливается выделенная роль
                string selectFromTroleMerging =
                    "select nzp_role_to as nzp_role, '' as role from t_role_merging where nzp_role_from=" + nzp_role;
                roleskeyList = doCheckMergeOrAddition(selectFromTroleMerging);
                if (roleskeyList.Count > 0)
                {
                    if (isSroles)
                    {
                        Dictionary<int, string> roles = new Dictionary<int, string>();
                        foreach (KeyValuePair<int, string> role in roleskeyList)
                        {

                            var list = CheckRoles(role.Key, true);
                            if (list == null)
                            {
                                MessageBox.Show(
                                    "Для данной роли скрипт не будет сгенерирован, т.к. не все роли имеют конечную роль",
                                    "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                return null;
                            }
                            foreach (KeyValuePair<int, string> checkedRole in list)
                            {
                                roles.Add(checkedRole.Key,checkedRole.Value);
                            }
                        }
                        return roles;
                        //string s = "";
                        //for (int i = 0; i < roles.Count; i++)
                        //{
                        //    s += roles[i] + (i == roles.Count - 1 ? ". " : ", ");
                        //}
                        //if (s != "")
                        //{

                        // }
                    }
                    else
                    {
                        Dictionary<int, string> roles = new Dictionary<int, string>();
                        foreach (KeyValuePair<int, string> role in roleskeyList)
                        {
                            var list = CheckRoles(role.Key, false);
                            if (list == null)
                            {
                                MessageBox.Show("Для роли " + role.Key + " скрипт не будет сгенерирован", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                continue;
                            }
                            foreach (KeyValuePair<int, string> checkedRole in list)
                            {
                                roles.Add(checkedRole.Key, checkedRole.Value);
                            }
                        }
                        if (roles.Count == 0)
                            return null;
                        return roles;
                    }
                }
                else
                {
                    MessageBox.Show("Для роли " + nzp_role + " скрипт не будет сгенерирован, т.к. она никуда не вливается, не является ни подсистемой, ни доп. ролью ", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception)
            {


            }
            finally
            {
                
            }
           // MessageBox.Show(
               // "Для этой роли скрипт не будет сгенерирован, т.к. веделенная роль не является ни подсистемой, ни доп. ролью и никуда не вливается.", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return null;
        }

        private static Dictionary<int, string> doCheckMergeOrAddition(string sql)
        {
            var connect = ConnectionToPostgreSqlDb.GetConnection();

            Dictionary<int, string> roleskeyList = new Dictionary<int, string>();
            IDataReader reader = null;
            try
            {
                Returns ret = new Returns();
                // Проверить является ли выделенная роль подсистемой
                connect.Open();
                reader = TransferDataDb.ExecuteReader(sql, out ret, connect);
                if (!ret.Result)
                {
                    return roleskeyList;
                }
                while (reader.Read())
                {
                    if (reader["nzp_role"] == DBNull.Value)
                    {
                        return new Dictionary<int,string>();
                    }
                        roleskeyList.Add((int)reader["nzp_role"], reader["role"].ToString());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                if (reader != null) reader.Close();
                connect.Close();
                connect.Dispose();
            }
            return roleskeyList;
        }

        //public static Dictionary<int, string> CheckRoles(int nzp_role, out RoleTypes rt)
        //{
        //    Returns ret = new Returns();
        //    var connect = ConnectionToPostgreSqlDb.GetConnection();
        //    Dictionary<int, string> roleskeyList = new Dictionary<int, string>();
        //    // Проверить является ли выделенная роль подсистемой
        //    string selectFromSRoles = "select nzp_role, role  from s_roles where is_subsystem=1 and nzp_role=" + nzp_role;
        //    var reader = TransferDataDb.ExecuteReader(selectFromSRoles, out ret, connect);
        //    if (!ret.Result)
        //    {
        //        rt = RoleTypes.none;
        //        return roleskeyList;
        //    }
        //    while (reader.Read())
        //    {
        //        roleskeyList.Add((int)reader["nzp_role"], reader["role"].ToString());
        //    }
        //    connect.Close();
        //    if (!reader.IsClosed) reader.Close();
        //    if (roleskeyList.Count != 0)
        //    {
        //        rt = RoleTypes.SubSystem;
        //        return roleskeyList;
        //    }

        //    // Проверить, является ли она дополнительной ролью
        //    string selectFromRolesKey = "select rk.nzp_role, rk.kod, sr.role from roleskey rk join s_roles sr on (rk.nzp_role=sr.nzp_role) where rk.kod=" + nzp_role;

        //    reader = TransferDataDb.ExecuteReader(selectFromRolesKey, out ret, connect);
        //    if (!ret.Result)
        //    {
        //        rt = RoleTypes.none;
        //        return roleskeyList;
        //    }
        //    while (reader.Read())
        //    {
        //        roleskeyList.Add((int)reader["nzp_role"], reader["role"].ToString());

        //    }
        //    connect.Close();
        //    if (!reader.IsClosed) reader.Close();
        //    if (roleskeyList.Count != 0)
        //    {
        //        rt = RoleTypes.Additional;
        //        return roleskeyList;
        //    }

        //    // Извлечем те роли, в которые вливается выделенная роль
        //    string selectFromTroleMerging =
        //        "select nzp_role_to from t_role_merging where nzp_role_from=" + nzp_role;

        //    reader = TransferDataDb.ExecuteReader(selectFromTroleMerging, out ret, connect);
        //    if (!ret.Result) if (!ret.Result)
        //        {
        //            rt = RoleTypes.none;
        //            return roleskeyList;
        //        }
        //    while (reader.Read())
        //    {
        //        roleskeyList.Add((int)reader["nzp_role_to"], "");
        //    }
        //    connect.Close();
        //    if (!reader.IsClosed) reader.Close();
        //    if (roleskeyList.Count != 0)
        //    {
        //        rt = RoleTypes.Merge;
        //        RoleTypes typroles;
        //        Dictionary<int, string> roles = new Dictionary<int, string>();
        //        foreach (KeyValuePair<int, string> kvp in roleskeyList)
        //        {
        //            var list = CheckRoles(kvp.Key, out typroles);
        //            if (list == null) continue;
        //            foreach (KeyValuePair<int, string> kv in list)
        //            {
        //                roles.Add(kv.Key, kv.Value);
        //            }
        //        }
        //        if (roles.Count == 0)
        //        {
        //            rt = RoleTypes.none;
        //        }
        //        return roles;
        //    }
        //    rt = RoleTypes.none;
        //    return null;
        //}
        public bool CheckToAllowDelete(DataGridView dvg)
        {
                if (dvg.CurrentRow == null) return true;
                var posit = dvg.CurrentRow.Cells["nzp_role"].Value;
                if (posit == DBNull.Value) return true;
                string[] tablesArr =
                {
                    Tables.role_actions, Tables.role_pages, Tables.roleskey, Tables.t_role_merging, Tables.profile_roles
                };
                Returns ret = new Returns(true, "");
                string commandText = "SELECT EXISTS (Select id from " + Tables.role_actions + " where nzp_role=" + posit + ");";
                commandText += "SELECT EXISTS (select id from " + Tables.role_pages + " Where nzp_role=" + posit + ");";
                commandText += "SELECT EXISTS (select nzp_rlsv from " + Tables.roleskey + " Where nzp_role=" + posit + ");";
                commandText += "SELECT EXISTS (select id from " + Tables.t_role_merging + " Where nzp_role_from=" + posit + " OR nzp_role_to=" + posit + ");";
                commandText += "SELECT EXISTS (select id from " + Tables.profile_roles + " Where nzp_role=" + posit + ");";
                return DBManager.CheckExistsRefernces(commandText, tablesArr, (int)posit);
            
        }
    }
}
