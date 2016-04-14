using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using webroles.TablesFirstLevel;
using webroles.TransferData;

namespace webroles.GenerateScriptTable.PagesTableScripts
{
   public class PagesInsertWholeScript
    {
        private int nzp_page;
        private int profile;
        private List<int> nzp_role_list= new List<int>(); 
        private List<int> nzp_act_list= new List<int>(); 

        public PagesInsertWholeScript(int nzp_page, ProfilesEnum profile)
        {
            this.nzp_page = nzp_page;
            this.profile = (int)profile;
        }

       public void Generate()
       {
           // Вытащить список ролей, где встречается эта страница
           string sql = "select  sr.* from " + DBManager.sDefaultSchema + Tables.profile_roles + " pr, " +
                        DBManager.sDefaultSchema + Tables.role_pages + " r , " +DBManager.sDefaultSchema + Tables.s_roles+" sr "+
                        " where r.nzp_role=pr.nzp_role  and " +
                        "r.nzp_page="+nzp_page+" and profile_id=" +profile + " and sr.nzp_role=r.nzp_role "+
                        " union select sr.* from " + DBManager.sDefaultSchema + Tables.profile_roles + " pr, " +
                        DBManager.sDefaultSchema + Tables.role_actions + " r , " + DBManager.sDefaultSchema + Tables.s_roles + " sr " +
                        "where r.nzp_role=pr.nzp_role  and sr.nzp_role=r.nzp_role and r.nzp_page=" + nzp_page + " and profile_id=" + profile;
           IDbConnection connection = ConnectionToPostgreSqlDb.GetConnection();
           IDataReader reader = null;
           Returns ret= new Returns(true,"");
           try
           {
               connection.Open();
               reader = TransferDataDb.ExecuteReader(sql, out ret, connection);
               while (reader.Read())
               {
                   bool is_subsystem = false;
                   if (reader["is_subsystem"] != DBNull.Value)
                   {
                       is_subsystem = (int) reader["is_subsystem"] == 1;
                   }
                   if (reader["nzp_role"] == DBNull.Value) continue;
                   Dictionary<int,string >roles=SrolesTable.CheckRoles((int)reader["nzp_role"], true);
                   nzp_role_list.AddRange(roles.Keys);
                   // если это подсистема, продолжаем
                   if (is_subsystem) continue;
                   if (reader["role"] == DBNull.Value) continue;
                   if (reader["page_url"] == DBNull.Value) continue;
                   if (reader["sort"] == DBNull.Value) continue;
                   // ключ-значение id
                   Dictionary<string, int> idColsVals = new Dictionary<string, int> 
                   { { "nzp_role", (int)reader["nzp_role"] } };
                   // ключ значение для insert
                   Dictionary<string, string> colsVals = new Dictionary<string, string>
                   {
                       {"nzp_role", reader["nzp_role"].ToString()},
                       {"role", getField(reader["role"].ToString())},
                       {"page_url", reader["page_url"].ToString()},
                       {"sort", reader["sort"].ToString()}
                   };
                   GenerateScript.AddValSpecialForRolePagesAndRoleActions(Tables.s_roles, new Dictionary<string, string>(), colsVals, idColsVals, ScriptGenerateOper.Insert, (int)reader["nzp_role"], true);
               }
           }
           catch (Exception ex)
           {
               MessageBox.Show(ex.Message);
           }
           finally
           {
               if (reader!=null) reader.Close();
               if (connection!=null) connection.Close();
           }
           foreach (int nzp_role in nzp_role_list)
           {
               // добавить строки в roleskey
               rowsToRoleskey(nzp_role);
           }
           rowsToPages();
           rowsToRolePages();
           rowsToRoleActions();
           rowsToActionsShow();
           rowsToActionsLnk();
           rowsToSactions();
           rowsToImgLnkSActions();
           rowsToPageLinks();
           rowsToReport();
       }

       private void rowsToPages()
       {
           string sql = "select * from " + DBManager.sDefaultSchema + Tables.pages  +
              "  where nzp_page= " + nzp_page ;
           IDbConnection connection = ConnectionToPostgreSqlDb.GetConnection();
           IDataReader reader = null;
           Returns ret = new Returns(true, "");
           try
           {
               connection.Open();
               reader = TransferDataDb.ExecuteReader(sql, out ret, connection);
               while (reader.Read())
               {
                   if (reader["nzp_page"] == DBNull.Value) continue;
                   Dictionary<string, int> idColsVals = new Dictionary<string, int> { { "nzp_page", (int)reader["nzp_page"] } };
                   Dictionary<string, string> colsVals = new Dictionary<string, string>
                   {
                       {"nzp_page", getField(reader["nzp_page"])},
                       {"page_url", getField(reader["page_url"])},
                       {"page_menu", getField(reader["page_menu"])},
                       {"page_name", getField(reader["page_name"])},
                       {"hlp", getField(reader["hlp"])},
                       {"up_kod",getField(reader["up_kod"])},
                       {"group_id", getField(reader["group_id"])}
                   };;
                   var chRow = GenerateScript.AddVal(Tables.pages, colsVals, new Dictionary<string, string>(), idColsVals, ScriptGenerateOper.Insert);
                   if (nzp_page != (int)reader["sort_kod"])
                   {
                       chRow.WhereColValuesDictionary.Add("nzp_page",nzp_page.ToString());
                       chRow.IsSortKodChanged = true;
                   }
               }
           }
           catch (Exception ex)
           {
               MessageBox.Show(ex.Message);
           }
           finally
           {
               if (reader != null) reader.Close();
               if (connection != null) connection.Close();
           }
       }
       // добавление в roleskey только в случае если эта роль дополнительная
       private void rowsToRoleskey(int nzpRole)
       {
           string sql = "select nzp_rlsv, nzp_role, tip, kod from " + DBManager.sDefaultSchema + Tables.roleskey + " where tip=105 and kod=" + nzpRole;
           IDbConnection connection = ConnectionToPostgreSqlDb.GetConnection();
           IDataReader reader = null;
          
           Returns ret = new Returns(true, "");
           try
           {
               connection.Open();
               reader = TransferDataDb.ExecuteReader(sql, out ret, connection);
               while (reader.Read())
               {
                   if (reader["nzp_rlsv"] == DBNull.Value) continue;
                   Dictionary<string, int> idColsVals = new Dictionary<string, int> { { "nzp_rlsv", (int)reader["nzp_rlsv"] } };
                   if (reader["nzp_role"] == DBNull.Value) continue;
                   if (reader["tip"] == DBNull.Value) continue;
                   if (reader["kod"] == DBNull.Value) continue;
                   Dictionary<string, string> colsVals = new Dictionary<string, string>
                   {
                       {"nzp_role", getField(reader["nzp_role"])},
                       {"tip", getField(reader["tip"])},
                       {"kod", getField(reader["kod"])}
                   };
                   GenerateScript.AddVal(Tables.roleskey, colsVals, new Dictionary<string, string>(), idColsVals, ScriptGenerateOper.Insert, (int) reader["nzp_role"]);
               }
           }
           catch (Exception ex)
           {
               MessageBox.Show(ex.Message);
           }
           finally
           {
               if (reader != null) reader.Close();
               if (connection != null) connection.Close();
           }
       }

       private void rowsToRolePages()
       {
           string sql = "select r.id, r.nzp_role, nzp_page from " + DBManager.sDefaultSchema + Tables.profile_roles + " pr, "+
               Tables.role_pages +" r where r.nzp_role=pr.nzp_role  and r.nzp_page="+nzp_page+" and profile_id="+profile;
           IDbConnection connection = ConnectionToPostgreSqlDb.GetConnection();
           IDataReader reader = null;
           Returns ret = new Returns(true, "");
           try
           {
               connection.Open();
               reader = TransferDataDb.ExecuteReader(sql, out ret, connection);
               while (reader.Read())
               {
                   if (reader["id"] == DBNull.Value) continue;
                   Dictionary<string, int> idColsVals = new Dictionary<string, int> { { "id", (int)reader["id"] } };
                   if (reader["nzp_role"] == DBNull.Value) continue;
                   if (reader["nzp_page"] == DBNull.Value) continue;
                   Dictionary<string, string> colsVals = new Dictionary<string, string>
                   {
                       {"nzp_role", getField(reader["nzp_role"])},
                       {"nzp_page", getField(reader["nzp_page"])}
                   };
                   GenerateScript.AddValSpecialForRolePagesAndRoleActions(Tables.role_pages, new Dictionary<string, string>(), colsVals, idColsVals, ScriptGenerateOper.Insert, (int)reader["nzp_role"]);
               }
           }
           catch (Exception ex)
           {
               MessageBox.Show(ex.Message);
           }
           finally
           {
               if (reader != null) reader.Close();
               if (connection != null) connection.Close();
           }
       }

       private void rowsToRoleActions()
       {
           string sql = "select r.id, r.nzp_role, r.nzp_page, nzp_act, r.mod_act from " + DBManager.sDefaultSchema + Tables.profile_roles + " pr, " +
               Tables.role_actions + " r where r.nzp_role=pr.nzp_role  and r.nzp_page=" + nzp_page + " and profile_id=" + profile;
           IDbConnection connection = ConnectionToPostgreSqlDb.GetConnection();
           IDataReader reader = null;
           Returns ret = new Returns(true, "");
           try
           {
               connection.Open();
               reader = TransferDataDb.ExecuteReader(sql, out ret, connection);
               while (reader.Read())
               {
                   if (reader["id"] == DBNull.Value) continue;
                   Dictionary<string, int> idColsVals = new Dictionary<string, int> { { "id", (int)reader["id"] } };
                   if (reader["nzp_role"] == DBNull.Value) continue;
                   if (reader["nzp_page"] == DBNull.Value) continue;
                   if (reader["nzp_act"] == DBNull.Value) continue;
                   Dictionary<string, string> colsVals = new Dictionary<string, string>
                   {
                       {"nzp_role", getField(reader["nzp_role"])},
                       {"nzp_page", getField(reader["nzp_page"])},
                       {"nzp_act", getField(reader["nzp_act"])},
                        {"mod_act", getField(reader["mod_act"])}
                   };
                   nzp_act_list.Add((int)reader["nzp_act"]);
                   GenerateScript.AddValSpecialForRolePagesAndRoleActions(Tables.role_actions, new Dictionary<string, string>(), colsVals, idColsVals, ScriptGenerateOper.Insert, (int)reader["nzp_role"]);
               }
           }
           catch (Exception ex)
           {
               MessageBox.Show(ex.Message);
           }
           finally
           {
               if (reader != null) reader.Close();
               if (connection != null) connection.Close();
           }
       }

       private void rowsToActionsShow()
       {
           if (nzp_act_list== null || nzp_act_list.Count==0) return;
           string sql = "select nzp_ash, cur_page, nzp_act, sort_kod, act_tip, act_dd from " + DBManager.sDefaultSchema + Tables.actions_show + " ash " +
              "  where cur_page= "+nzp_page+" and nzp_act in ("+String.Join(",", nzp_act_list)+")";
           IDbConnection connection = ConnectionToPostgreSqlDb.GetConnection();
           IDataReader reader = null;
           Returns ret = new Returns(true, "");
           try
           {
               connection.Open();
               reader = TransferDataDb.ExecuteReader(sql, out ret, connection);
               while (reader.Read())
               {
                   if (reader["nzp_ash"] == DBNull.Value) continue;
                   Dictionary<string, int> idColsVals = new Dictionary<string, int> { { "nzp_ash", (int)reader["nzp_ash"] } };
                   if (reader["cur_page"] == DBNull.Value) continue;
                   if (reader["nzp_act"] == DBNull.Value) continue;
                   if (reader["sort_kod"] == DBNull.Value) continue;
                   if (reader["act_tip"] == DBNull.Value) continue;
                   if (reader["act_dd"] == DBNull.Value) continue;
                   Dictionary<string, string> colsVals = new Dictionary<string, string>
                   {
                       {"cur_page", getField(reader["cur_page"])},
                       {"nzp_act", getField(reader["nzp_act"])},
                       {"sort_kod", getField(reader["sort_kod"])},
                       {"act_tip", getField(reader["act_tip"])},
                       {"act_dd", getField(reader["act_dd"])}
                   };
                   GenerateScript.AddVal(Tables.actions_show, colsVals, new Dictionary<string, string>(), idColsVals, ScriptGenerateOper.Insert, nzp_page);
               }
           }
           catch (Exception ex)
           {
               MessageBox.Show(ex.Message);
           }
           finally
           {
               if (reader != null) reader.Close();
               if (connection != null) connection.Close();
           }
       }

       private void rowsToActionsLnk()
       {
           if (nzp_act_list == null || nzp_act_list.Count == 0) return;
           string sql = "select nzp_al, cur_page, nzp_act, page_url from " + DBManager.sDefaultSchema + Tables.actions_lnk + " al " +
              "  where cur_page= " + nzp_page + " and nzp_act in (" + String.Join(",", nzp_act_list) + ")";
           IDbConnection connection = ConnectionToPostgreSqlDb.GetConnection();
           IDataReader reader = null;
           Returns ret = new Returns(true, "");
           try
           {
               connection.Open();
               reader = TransferDataDb.ExecuteReader(sql, out ret, connection);
               while (reader.Read())
               {
                   if (reader["nzp_al"] == DBNull.Value) continue;
                   Dictionary<string, int> idColsVals = new Dictionary<string, int> { { "nzp_al", (int)reader["nzp_al"] } };
                   if (reader["cur_page"] == DBNull.Value) continue;
                   if (reader["nzp_act"] == DBNull.Value) continue;
                   if (reader["page_url"] == DBNull.Value) continue;

                   if (nzp_page != (int) reader["page_url"] )
                   {
                       var count = GenerateScript.ChangedRowCollection.Count(ch => ch.TableName == Tables.pages && ch.IdNum == (int)reader["page_url"]);
                       if (count>0) continue;
                       PagesInsertWholeScript pagesInsert = new PagesInsertWholeScript((int)reader["page_url"],(ProfilesEnum)profile);
                       pagesInsert.Generate();
                   }
                   Dictionary<string, string> colsVals = new Dictionary<string, string>
                   {
                       {"cur_page", getField(reader["cur_page"])},
                       {"nzp_act", getField(reader["nzp_act"])},
                       {"page_url", getField(reader["page_url"])},
                   };
                   GenerateScript.AddVal(Tables.actions_lnk, colsVals, new Dictionary<string, string>(), idColsVals, ScriptGenerateOper.Insert, nzp_page);
               }
           }
           catch (Exception ex)
           {
               MessageBox.Show(ex.Message);
           }
           finally
           {
               if (reader != null) reader.Close();
               if (connection != null) connection.Close();
           }
       }

       private void rowsToSactions()
       {
           if (nzp_act_list == null || nzp_act_list.Count == 0) return;
           string sql = "select nzp_act, act_name, hlp from " + DBManager.sDefaultSchema + Tables.s_actions + " sa " +
               "  where nzp_act in (" + String.Join(",", nzp_act_list) + ")";
           IDbConnection connection = ConnectionToPostgreSqlDb.GetConnection();
           IDataReader reader = null;
           Returns ret = new Returns(true, "");
           try
           {
               connection.Open();
               reader = TransferDataDb.ExecuteReader(sql, out ret, connection);
               while (reader.Read())
               {
                   if (reader["nzp_act"] == DBNull.Value) continue;
                   Dictionary<string, int> idColsVals = new Dictionary<string, int> { { "nzp_act", (int)reader["nzp_act"] } };
                   if (reader["nzp_act"] == DBNull.Value) continue;
                   if (reader["act_name"] == DBNull.Value) continue;
                   if (reader["hlp"] == DBNull.Value) continue;
                   Dictionary<string, string> colsVals = new Dictionary<string, string>
                   {
                       {"nzp_act", getField(reader["nzp_act"])},
                       {"act_name", getField(reader["act_name"])},
                       {"hlp", getField(reader["hlp"])}
                   };
                   GenerateScript.AddVal(Tables.s_actions, colsVals, new Dictionary<string, string>(), idColsVals, ScriptGenerateOper.Insert, (int)reader["nzp_act"]);
               }
           }
           catch (Exception ex)
           {
               MessageBox.Show(ex.Message);
           }
           finally
           {
               if (reader != null) reader.Close();
               if (connection != null) connection.Close();
           }  
       }

       private void rowsToImgLnkSActions()
       {
           if (nzp_act_list == null || nzp_act_list.Count == 0) return;
           string sql = "select nzp_img, cur_page, tip, kod, img_url, nzp_act from " + DBManager.sDefaultSchema + Tables.img_lnk + " im " +
                "  where nzp_act in (" + String.Join(",", nzp_act_list) + ") and tip=2";
           IDbConnection connection = ConnectionToPostgreSqlDb.GetConnection();
           IDataReader reader = null;
           Returns ret = new Returns(true, "");
           try
           {
               connection.Open();
               reader = TransferDataDb.ExecuteReader(sql, out ret, connection);
               while (reader.Read())
               {
                   if (reader["nzp_img"] == DBNull.Value) continue;
                   Dictionary<string, int> idColsVals = new Dictionary<string, int> { { "nzp_img", (int)reader["nzp_img"] } };
                   if (reader["cur_page"] == DBNull.Value) continue;
                   if (reader["tip"] == DBNull.Value) continue;
                   if (reader["kod"] == DBNull.Value) continue;
                   if (reader["img_url"] == DBNull.Value) continue;
                   Dictionary<string, string> colsVals = new Dictionary<string, string>
                   {
                       {"cur_page", getField(reader["cur_page"])},
                       {"tip", getField(reader["tip"])},
                       {"kod", getField(reader["kod"])},
                       {"img_url", getField(reader["img_url"])},
                   };
                   GenerateScript.AddVal(Tables.img_lnk, colsVals, new Dictionary<string, string>(), idColsVals, ScriptGenerateOper.Insert, (int)reader["nzp_act"]);
               }
           }
           catch (Exception ex)
           {
               MessageBox.Show(ex.Message);
           }
           finally
           {
               if (reader != null) reader.Close();
               if (connection != null) connection.Close();
           }    
       }

       private void rowsToPageLinks()
       {
           string sql = "select distinct pl.* from " + DBManager.sDefaultSchema + Tables.page_links + " pl, " +Tables.pages+ " p "+
                "  where (p.nzp_page = pl.page_from or p.nzp_page=pl.page_to or p.group_id= pl.group_from or p.group_id=pl.group_to) and p.nzp_page="+nzp_page;
           IDbConnection connection = ConnectionToPostgreSqlDb.GetConnection();
           IDataReader reader = null;
           Returns ret = new Returns(true, "");
           try
           {
               connection.Open();
               reader = TransferDataDb.ExecuteReader(sql, out ret, connection);
               while (reader.Read())
               {
                   if (reader["id"] == DBNull.Value) continue;
                   Dictionary<string, int> idColsVals = new Dictionary<string, int> { { "id", (int)reader["id"] } };
                   Dictionary<string, string> colsVals = new Dictionary<string, string>();
                   colsVals.Add("page_from", getField(reader["page_from"]));
                   colsVals.Add("group_from", getField(reader["group_from"]));
                   colsVals.Add("page_to", getField(reader["page_to"]));
                   colsVals.Add("group_to", getField(reader["group_to"]));
                   GenerateScript.AddVal(Tables.page_links, colsVals, new Dictionary<string, string>(), idColsVals, ScriptGenerateOper.Insert);
               }
           }
           catch (Exception ex)
           {
               MessageBox.Show(ex.Message);
           }
           finally
           {
               if (reader != null) reader.Close();
               if (connection != null) connection.Close();
           }    
       }

       private void rowsToReport()
       {
           if (nzp_act_list == null || nzp_act_list.Count == 0) return;
           string sql = "select nzp_rep, nzp_act, name, file_name from " + DBManager.sDefaultSchema + Tables.report + " r " +
          "  where nzp_act in (" + String.Join(",", nzp_act_list) + ") ";
           IDbConnection connection = ConnectionToPostgreSqlDb.GetConnection();
           IDataReader reader = null;
           Returns ret = new Returns(true, "");
           try
           {
               connection.Open();
               reader = TransferDataDb.ExecuteReader(sql, out ret, connection);
               while (reader.Read())
               {
                   if (reader["nzp_rep"] == DBNull.Value) continue;
                   Dictionary<string, int> idColsVals = new Dictionary<string, int> { { "nzp_rep", (int)reader["nzp_rep"] } };
                   if (reader["nzp_act"] == DBNull.Value) continue;
                   if (reader["name"] == DBNull.Value) continue;
                   if (reader["file_name"] == DBNull.Value) continue;
                   Dictionary<string, string> colsVals = new Dictionary<string, string>
                   {
                       {"nzp_act", getField(reader["nzp_act"])},
                       {"name", getField(reader["name"])},
                       {"file_name", getField(reader["file_name"])},
                   };
                   GenerateScript.AddVal(Tables.report, colsVals, new Dictionary<string, string>(), idColsVals, ScriptGenerateOper.Insert);
               }
           }
           catch (Exception ex)
           {
               MessageBox.Show(ex.Message);
           }
           finally
           {
               if (reader != null) reader.Close();
               if (connection != null) connection.Close();
           }    
       }

       private string getField(object val)
       {
           if (val == DBNull.Value || val == null)
               return "null";
           if (val.GetType() == typeof (Int32))
           {
               return val.ToString();
           }
           if (val.GetType() == typeof(String))
           {
               return "'"+ val+"'";
           }
           return "null";
       }
    }
}
