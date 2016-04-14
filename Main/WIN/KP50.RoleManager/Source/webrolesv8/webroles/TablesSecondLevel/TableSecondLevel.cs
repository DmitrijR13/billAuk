using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using Npgsql;
using webroles.TablesFirstLevel;
using webroles.TransferData;

namespace webroles.TablesSecondLevel
{
   public abstract class TableSecondLevel: TableFirstLevel
   {
       /// <summary>
       /// Значение ячейки таблицы первого уровня, для которой будет подгружаться таблица второго уровня 
       /// </summary>
       protected int position;
       /// <summary>
       /// Наименование базовой таблицы (таблицы первого уровня), к которой принадлежит эта таблица
       /// </summary>
       public abstract string NameParentTable { get; }
       /// <summary>
       /// Наименование колонки, по значениям которой подгружаются данные в таблицах второго уровня
       /// </summary>
       public abstract string NameParentColumn { get; }
       public abstract string NameOwnBaseColumn { get; }
       /// <summary>
       /// Получает данные, соответствующие значению position
       /// </summary>
       /// <param name="dataSet"></param>
       /// <param name="position">Значение ячейки таблицы первого уровня, для которой будет подгружаться таблица второго уровня </param>
       /// <param name="connect"></param>
       public abstract void GetCorrespondTable(DataSet dataSet, NpgsqlDataAdapter adapter, NpgsqlConnection connect, int position);
       public int Position
       {
           get { return position; }
           set
           {
               if (value > -1)
                   position = value;
           }
       }

       //public static List<int> CheckRoles(int nzp_role, bool isSroles)
       //{
       //    Returns ret = new Returns();
       //    var connect = ConnectionToPostgreSqlDb.GetConnection();
       //    List<int> roleskeyList = new List<int>();
       //    // Проверить является ли выделенная роль подсистемой
       //    string selectFromSRoles = "select nzp_role  from s_roles where is_subsystem=1 and nzp_role=" + nzp_role;
       //    var reader = TransferDataDb.ExecuteReader(selectFromSRoles, out ret, connect);
       //    if (!ret.Result) return roleskeyList;
       //    while (reader.Read())
       //    {
       //        roleskeyList.Add((int)reader["nzp_role"]);
       //    }
       //    connect.Close();
       //    if (!reader.IsClosed) reader.Close();
       //    if (roleskeyList.Count != 0)
       //    {
       //        // 
       //        return roleskeyList;
       //    }

       //    // Проверить, является ли она дополнительной ролью
       //    string selectFromRolesKey = "select nzp_role, kod from roleskey where kod=" + nzp_role;

       //    reader = TransferDataDb.ExecuteReader(selectFromRolesKey, out ret, connect);
       //    if (!ret.Result) return roleskeyList;
       //    while (reader.Read())
       //    {
       //        roleskeyList.Add(nzp_role);
       //        return roleskeyList;
       //    }
       //    connect.Close();
       //    if (!reader.IsClosed) reader.Close();
       //    // Извлечем те роли, в которые вливается выделенная роль
       //    string selectFromTroleMerging =
       //        "select nzp_role_to from t_role_merging where nzp_role_from=" + nzp_role;

       //    reader = TransferDataDb.ExecuteReader(selectFromTroleMerging, out ret, connect);
       //    if (!ret.Result) return roleskeyList;
       //    while (reader.Read())
       //    {
       //        roleskeyList.Add((int)reader["nzp_role_to"]);
       //    }
       //    connect.Close();
       //    if (!reader.IsClosed) reader.Close();
       //    if (roleskeyList.Count != 0)
       //    {
       //        if (isSroles)
       //        {
       //            List<int> roles = new List<int>();
       //            for (int i = 0; i < roleskeyList.Count; i++)
       //            {
       //                var list = CheckRoles(roleskeyList[i], true);
       //                if (list == null) continue;
       //                roles.AddRange(list);
       //            }
       //            string s = "";
       //            for (int i = 0; i < roles.Count; i++)
       //            {
       //                s += roles[i] + (i == roles.Count - 1 ? ". " : ", ");
       //            }
       //            if (s != "")
       //            {
       //                MessageBox.Show(
       //                    "Для этой роли скрипт не будет сгенерирован, т.к. эта роль вливается в роли : " + s,
       //                    "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Information);
       //                return null;
       //            }
       //        }
       //        else
       //        {
       //            List<int> roles = new List<int>();
       //            for (int i = 0; i < roleskeyList.Count; i++)
       //            {
       //                var list = CheckRoles(roleskeyList[i], false);
       //                if (list == null)
       //                {
       //                    MessageBox.Show("Для роли " + roleskeyList[i] + " скрипт не будет сгенерирован", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Information);
       //                    continue;
       //                }
       //                roles.AddRange(list);
       //            }
       //            if (roles.Count == 0)
       //                return null;
       //            return roles;
       //        }
       //    }
       //    MessageBox.Show(
       //        "Для этой роли скрипт не будет сгенерирован, т.к. веделенная роль не является ни подсистемой, ни доп. ролью и никуда не вливается.", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Information);
       //    return null;
       //}
       private static bool isFirstStart = true;
       public static RoleTypes CheckRoles(int nzp_role, out string message, bool isSroles, bool isForRead, string roleName="", bool isFirst=true)
       {
           // Словарь: ключ-тип роли, значение- строка запроса
           Dictionary<RoleTypes, string> requestDictionary = new Dictionary<RoleTypes, string>
           {
               {
                   RoleTypes.SubSystem, "select nzp_role, role  from s_roles where is_subsystem=1 and nzp_role=" + nzp_role
               },
               {
                   RoleTypes.Additional,
                   "select rk.nzp_role, rk.kod, sr.role from roleskey rk join s_roles sr on (rk.nzp_role=sr.nzp_role) where rk.kod=" +
                   nzp_role
               },
               {RoleTypes.Merge, "select nzp_role_to as nzp_role,  sr.role  from t_role_merging t join s_roles sr on (t.nzp_role_to=sr.nzp_role) where nzp_role_from=" + nzp_role}

           };
           message = "";
           IDbConnection connect = ConnectionToPostgreSqlDb.GetConnection();
           IDataReader reader = null;
           try
           {
               connect.Open();
               foreach (KeyValuePair<RoleTypes, string> request in requestDictionary)
               {
                   Returns ret = new Returns();
                   Dictionary<int, string> roleskeyList = new Dictionary<int, string>();
                   // Проверить является ли выделенная роль подсистемой
                   reader = TransferDataDb.ExecuteReader(request.Value, out ret, connect);
                   if (!ret.Result)
                   {
                       message = "Ошибка запроса ролей из базы " + ret.SqlError;
                       return RoleTypes.none;
                   }
                   while (reader.Read())
                   {
                       roleskeyList.Add((int) reader["nzp_role"], reader["role"].ToString());
                   }

                   if (!isFirst)
                   {
                       if (RoleTypes.Merge == request.Key)
                       {
                           message += nzp_role + " " + roleName + "->";
                       }
                   }
                   if (roleskeyList.Count != 0)
                   {
                       // Если  переданная роль вливается
                       if (request.Key == RoleTypes.Merge)
                       {
                           int k = 0;
                           string fullRolesName = "";
                           foreach (KeyValuePair<int, string> pair in roleskeyList)
                           {
                               string delimeter = ", ";
                               if (CheckRoles(pair.Key, out fullRolesName, false, true, pair.Value, false) == RoleTypes.Merge)
                               {
                                   delimeter = " & ";
                               }
                               message += fullRolesName + (k != (roleskeyList.Count - 1) ? delimeter : "");
                               k++;
                           }
                           return RoleTypes.Merge;
                       }

                       if (request.Key == RoleTypes.Additional)
                       {
                           if (!isFirst)
                           {
                               message += nzp_role + " " + roleName + "+>";
                           }
                       }
                       string doprole = "";
                       int j = 0;
                       foreach (KeyValuePair<int, string> pair in roleskeyList)
                       {
                           doprole += pair.Key + " " + pair.Value + (j != (roleskeyList.Count - 1) ? " ," : "");
                           j++;
                       }
                       message += doprole;
                       return request.Key;
                   }
               }
               message += "none";
             
           }
           catch (Exception ex)
           {

               MessageBox.Show(ex.Message);
           }
           finally
           {
               if (reader!=null) reader.Close();
               if (connect != null)
               {
                   connect.Close();
                   connect.Dispose();
               }

           }
           return RoleTypes.none;  
       }


       //// Проверить, является ли она дополнительной ролью
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
       //        "select nzp_role_to as nzp_role, '' as role from t_role_merging where nzp_role_from=" + nzp_role;

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
       }
    }

