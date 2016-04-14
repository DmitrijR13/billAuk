using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Npgsql;
using webroles.Properties;
using webroles.TransferData;

namespace webroles.TablesSecondLevel
{
    class UseInRolePages:TableSecondLevel, IEditable
    {
        DataGridViewColumn[] columnCollection;
        private readonly string nameParentTreeViewNode;
        public UseInRolePages(string nameParentTreeViewNode)
        {
            this.nameParentTreeViewNode = nameParentTreeViewNode;
        }

        private string selectCommand;
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
            get { return "nzp_page"; }
        }

        private string getSelectCommand()
        {
            return "with rp as (select r.nzp_role, r.nzp_role ||' '|| role as role from role_pages r, s_roles s where nzp_page=" + Position + " and s.nzp_role=r.nzp_role ), " +
                " tula as (select nzp_role from profile_roles where profile_id=1), " +
                " obn as (select nzp_role from profile_roles where profile_id=10), " +
                "saha as (select nzp_role from profile_roles where profile_id=11), " +
                "marii as (select nzp_role from profile_roles where profile_id=15) " +
                "select rp.nzp_role, rp.role, " +
                " case when tula.nzp_role is null then 0 else 1 end as tul, " +
                " case when obn.nzp_role is null then 0 else 1 end as ob, " +
                " case when saha.nzp_role is null then 0 else 1 end  as sah, " +
                "case when marii.nzp_role is null then 0 else 1 end as mari" +
                " from  rp left outer join obn on (rp.nzp_role=obn.nzp_role) " +
                " left outer join tula on (rp.nzp_role=tula.nzp_role) " +
                " left outer join saha on (rp.nzp_role=saha.nzp_role)  " +
                " left outer join marii on (rp.nzp_role=marii.nzp_role) Order by rp.nzp_role ";
        }

        public override DataTable GetDataSource(NpgsqlConnection connect, NpgsqlDataAdapter adapter)
        {
            DataTable dt = new DataTable();
            adapter.SelectCommand.CommandText = SelectCommand;
            TransferDataDb.Fill(adapter, dt);

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                string msg = "";
                var role = dt.Rows[i].Field<string>("role");
                RoleTypes rt = CheckRoles(dt.Rows[i].Field<int>("nzp_role"), out msg, false, true);
                switch (rt)
                {
                    case RoleTypes.SubSystem:
                        dt.Rows[i].SetField("role", role + " /Подсистема");
                        break;
                    case RoleTypes.Additional:
                        dt.Rows[i].SetField("role", role + " /Доп.роль у роли(ей): " + msg);
                        break;
                    case RoleTypes.Merge:
                        dt.Rows[i].SetField("role", role + " /Вливается в роль(и): " + msg);
                        break;
                    default:
                        dt.Rows[i].SetField("role", role + " /" + msg);
                        break;
                }              
            }
            return dt;
        }

        public override void GetCorrespondTable(System.Data.DataSet dataSet, Npgsql.NpgsqlDataAdapter adapter, Npgsql.NpgsqlConnection connect, int positionParam)
        {
            selectCommand =
         "with rp as (select r.nzp_role, r.nzp_role ||' '|| role as role from role_pages r, s_roles s where nzp_page=" + positionParam + " and s.nzp_role=r.nzp_role ), " +
                " tula as (select nzp_role from profile_roles where profile_id=1), " +
                " obn as (select nzp_role from profile_roles where profile_id=10), " +
                "saha as (select nzp_role from profile_roles where profile_id=11), " +
                "marii as (select nzp_role from profile_roles where profile_id=15) " +
                "select rp.nzp_role, rp.role, " +
                " case when tula.nzp_role is null then 0 else 1 end as tul, " +
                " case when obn.nzp_role is null then 0 else 1 end as ob, " +
                " case when saha.nzp_role is null then 0 else 1 end  as sah, " +
                "case when marii.nzp_role is null then 0 else 1 end as mari" +
                " from  rp left outer join obn on (rp.nzp_role=obn.nzp_role) " +
                " left outer join tula on (rp.nzp_role=tula.nzp_role) " +
                " left outer join saha on (rp.nzp_role=saha.nzp_role)  "+
                " left outer join marii on (rp.nzp_role=marii.nzp_role) Order by rp.nzp_role ";
                  //"SELECT r.id, r.nzp_role, s.role FROM " + Tables.role_pages + " r LEFT OUTER JOIN " + Tables.s_roles + " s ON (r.nzp_role=s.nzp_role)  WHERE nzp_page=" +
                  //positionParam + " ORDER BY r.nzp_role;";
            adapter.SelectCommand.CommandText = selectCommand;
            dataSet.Tables[TableName].Clear();
            TransferDataDb.Fill(adapter, dataSet.Tables[TableName]);
            if (dataSet.Tables[TableName].Rows.Count == 0) return;

           
            for (int i = 0; i < dataSet.Tables[TableName].Rows.Count; i++)
            {
                string msg = "";
              //  Dictionary<int, string> roles = CheckRoles(dataSet.Tables[TableName].Rows[i].Field<int>("nzp_role"), out rt);
               var role = dataSet.Tables[TableName].Rows[i].Field<string>("role");
                RoleTypes  rt = CheckRoles(dataSet.Tables[TableName].Rows[i].Field<int>("nzp_role"), out msg, false, true);
                switch (rt)
                {
                      case RoleTypes.SubSystem:
                          dataSet.Tables[TableName].Rows[i].SetField("role", role+" /Подсистема");
                        break;
                      case RoleTypes.Additional:
                        string doprole= "";
                        int k=0;
                        //foreach (KeyValuePair<int, string> pair in roles)
                        //{
                        //   k++;
                        //    doprole += pair.Key + " " + pair.Value + (k != (roles.Count) ? " ," : "");
                        //}
                        dataSet.Tables[TableName].Rows[i].SetField("role", role + " /Доп.роль у роли(ей): " + msg);
                        break;
                      case RoleTypes.Merge:
                        string mergeroles="";
                           int l=0;
                        //foreach (KeyValuePair<int, string> pair in roles)
                        //{
                        //   l++;
                        //   mergeroles += pair.Key + " " + pair.Value + (l != (roles.Count) ? " ," : "");
                        //}
                          dataSet.Tables[TableName].Rows[i].SetField("role", role + " /Вливается в роль(и): "+msg);
                        break;
                      default: dataSet.Tables[TableName].Rows[i].SetField("role", role +" /"+msg);
                        break;
                }
            }
        }
        //public static Dictionary<int, string> CheckRoles(int nzp_role, out RoleTypes rt)
        //{
        //    Returns ret = new Returns();
        //    var connect = ConnectionToPostgreSqlDb.GetConnection();
        //    Dictionary<int,string> roleskeyList= new Dictionary<int, string>();
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
        //        roleskeyList.Add((int)reader["nzp_role_to"],"");
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
        //                roles.Add(kv.Key,kv.Value);
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

        public override string NodeTreeViewText
        {
            get { return "Наличие в страницах ролей (для информации)"; }
        }

        public override string TableName
        {
            get { return Tables.useInRolePages; }
        }

        public override string SelectCommand
        {
            get { return getSelectCommand(); }
        }

        public override System.Windows.Forms.DataGridViewColumn[] GetTableColumns
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
            columnCollection[0] = CreateDataGridViewColumn.CreateTextBoxColumn(TableName, "nzp_role", "", true, false);
            columnCollection[1] = CreateDataGridViewColumn.CreateTextBoxColumn(TableName, "role", "Наименование роли", true, true);
            columnCollection[1].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
            columnCollection[2] = CreateDataGridViewColumn.CreateCheckBoxColumn("TableName", "tul","Профиль Тула",true);
            //columnCollection[1].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
            columnCollection[3] = CreateDataGridViewColumn.CreateCheckBoxColumn("TableName", "ob", "Профиль Обнинск", true);
            //columnCollection[2].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
            columnCollection[4] = CreateDataGridViewColumn.CreateCheckBoxColumn("TableName", "sah", "Профиль Саха", true);
            columnCollection[5] = CreateDataGridViewColumn.CreateCheckBoxColumn("TableName", "mari", "Профиль Мрий Эл", true);
           // columnCollection[3].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
        }

        public override bool Save(Npgsql.NpgsqlConnection connect, Npgsql.NpgsqlDataAdapter adapter, DataTable dt)
        {
            return true;
        }

        public override void SetDefaultValuesAfterRowAdded(System.Windows.Forms.DataGridView dgv, int index)
        {
           
        }

        public override void SetDefaultValuesAfterCellChange(System.Windows.Forms.DataGridView dgv, int row, int column)
        {
            
        }

        public bool AllowEdit(EditOperations operation)
        {
            MessageBox.Show("Данная таблица только для просмотра", "Только просмотр", MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return false;
        }
    }
}
