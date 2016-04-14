using System;
using System.Data;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using System.Collections.Generic;
using STCLINE.KP50.Client;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using System.Text;

namespace STCLINE.KP50.DataBase
{
    /// <summary>
    /// Аутентификация пользователя
    /// </summary>
    class ClassAuthenticateUser : DataBaseHeadClient
    {
        /// <summary>
        /// Аутентификация пользователя
        /// </summary>
        /// <param name="web_user"></param>
        /// <param name="ip"></param>
        /// <param name="browser"></param>
        /// <param name="idses"></param>
        /// <param name="inbase"></param>
        /// <param name="UseWSO2Autentification"></param>
        /// <returns></returns>
        public bool AuthenticateUser(BaseUser web_user, string ip, string browser, string idses, out int inbase, bool UseWSO2Autentification = false) //проверка пользователя
        {
            Returns ret = new Returns(true);
            inbase = 0;

            try
            {
#if PG
                ExecSQL("set search_path to public");
#else
                ret = ExecSQL(" set encryption password '" + BasePwd + "'");
                if (!ret.result) throw new Exception(ret.text);
#endif
                web_user.idses = idses;
                web_user.ip_log = ip;
                web_user.browser = browser;

                if (!CheckUser(web_user, UseWSO2Autentification))
                {
                    using (DbLogClient dbLoagFailure = new DbLogClient())
                    {
                        bool res = dbLoagFailure.LogAcc(this.ClientConnection, web_user, Constants.acc_failure);
                    }
                    return false;
                }

                //test
                //Conections.Set_connection(2);
                if (true)
                {
                    SetConnectionCount(web_user, out inbase);
                }

                using (ClassFillUserRigths checkRigths = new ClassFillUserRigths())
                {
                    checkRigths.FillingKeyRoles(this.ClientConnection, web_user.nzp_user);
                }
                
                if (inbase == -1)
                {
                    return true;
                }
                else
                {
                    bool res = false;
                    using (DbLogClient db = new DbLogClient())
                    {
                        res = db.LogAcc(this.ClientConnection, web_user, Constants.acc_in); //зафиксируем сессию
                    }
                    return res;
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка функцкии AuthenticateUser. " + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                return false;
            }
        }

        /// <summary>
        /// Проверка пользователя
        /// </summary>
        /// <param name="UseWSO2Autentification"></param>
        /// <returns></returns>
        private bool CheckUser(BaseUser web_user, bool UseWSO2Autentification)
        {
            Returns ret = new Returns(true);
            MyDataReader reader = null;
            int nzp_user = 0;
            bool userIsFound = true;

            try
            {
                if (!UseWSO2Autentification)
                {
                    var objNzpUser = ExecScalar("SELECT nzp_user FROM users WHERE login = " + Utils.EStrNull(web_user.login), out ret);
                    if (!ret.result) throw new Exception(ret.text);
                    nzp_user = Convert.ToInt32(objNzpUser);
                }

                string sql = "";
#if PG
                sql = " Select nzp_user, login, uname, dat_log, date_begin" +
                    " From users a Where (CAST(is_blocked as int) <> 1 or is_blocked is null) and login = " + Utils.EStrNull(web_user.login) +
                        " and 0 < (Select count(*) From users b Where a.nzp_user = b.nzp_user " +
                        (UseWSO2Autentification ? string.Empty : string.Format(" and b.pwd = {0} ", Utils.EStrNull(Utils.CreateMD5StringHash(web_user.password + nzp_user + BasePwd)))) + ")";
#else
                sql = " Select nzp_user, login, decrypt_char(uname) as uname, dat_log, date_begin" +
                    " From users a Where (is_blocked <> 1 or is_blocked is null) and login = " + Utils.EStrNull(_webUser.login) + 
                        "  and 0 < ( Select count(*) From users b Where a.nzp_user = b.nzp_user " +
                        "  and decrypt_char(pwd) = a.nzp_user||'-'||'" + _webUser.password + "' " +
                        "  and decrypt_char(pwd) <> a.nzp_user||'-#' )";
#endif

                ret = ExecRead(out reader, sql);
                if (!ret.result) throw new Exception(ret.text);

                if (!reader.Read())
                {
                    userIsFound = false;
                }
                else
                {
                    userIsFound = true;

                    web_user.nzp_user = (int)reader["nzp_user"];
                    web_user.login = ((string)reader["login"]).Trim();
                    web_user.uname = ((string)reader["uname"]).Trim();
                    if (reader["date_begin"] != DBNull.Value) web_user.date_begin = String.Format("{0:dd.MM.yyyy}", reader["date_begin"]);
                    web_user.dat_log = Constants._UNDEF_;
                }
            }
            catch (Exception ex)
            {
                userIsFound = false;
                MonitorLog.WriteLog("Ошибка функцкии CheckUser. " + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
            }
            finally
            {
                if (reader != null) reader.Close();
            }

            return userIsFound;
        }

        /// <summary>
        /// установка счетчика соединений
        /// </summary>
        /// <param name="inbase"></param>
        private void SetConnectionCount(BaseUser web_user, out int inbase)
        {
            Returns ret = new Returns(true);

            if (Connections.Inc_connection())
            {
                inbase = 1;
            }
            else
            {
                //превышено кол-во разрешенных подключений, 
                //проверим кол-во текущих подключений
                var logSessionCount = ExecScalar(" Select count(*) as cnt From " + sDefaultSchema + "log_sessions ", out ret);
                if (!ret.result) throw new Exception(ret.text);
                inbase = Convert.ToInt32(logSessionCount);

                if (inbase >= 0) //кол-во текущих пользователей
                {
                    //обновить счетчик
                    Connections.Set_connection(inbase);
                }
                //после обновления заново увеличим счетчик, 
                if (!Connections.Inc_connection()) inbase = -1; //превышен лимит подключений
            }

            string sql = " Update users Set " +
                " dat_log = " + sCurDateTime + ", ip_log = " + Utils.EStrNull(web_user.ip_log) + ", browser = " + Utils.EStrNull(web_user.browser) +
                " Where nzp_user = " + web_user.nzp_user;
            ret = ExecSQL(sql);

            if (!ret.result)
            {
                if (inbase != -1)
                {
                    Connections.Dec_connection();
                }
                throw new Exception(ret.text);
            }
        }
    }

    /// <summary>
    /// Формирование таблиц с правами пользователя
    /// </summary>
    class ClassFillUserRigths : DataBaseHead
    {
        private IDbConnection _connDb = null;
        private int _nzpUser = 0;
        private string temp_user_role = "";

        /// <summary>
        /// Заполнить права пользователя
        /// </summary>
        /// <param name="nzp_user"></param>
        public void FillingKeyRoles(int nzp_user)
        {
            IDbConnection conn_db = null;
            
            try
            {
                conn_db = GetConnection(Constants.cons_Webdata);
                Returns ret = OpenDb(conn_db, true);
                if (!ret.result) throw new Exception(ret.text);
                this.FillingKeyRoles(conn_db, nzp_user);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка функцкии FillingKeyRoles. " + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
            }
            finally
            {
                if (conn_db != null)
                {
                    conn_db.Close();
                    conn_db.Dispose();
                }
            }
        }

        /// <summary>
        /// Заполнение ролей пользователя
        /// </summary>
        /// <param name="connectionID"></param>
        /// <param name="nzp_user"></param>
        public void FillingKeyRoles(IDbConnection connectionID, int nzp_user)
        {
            _connDb = connectionID;
            _nzpUser = nzp_user;
            temp_user_role = "temp_user_roles_" + nzp_user;

            string sql = "";
          
#if PG
            sql = "set search_path to 'public'";
#else
            sql = " set encryption password " + Utils.EStrNull(BasePwd);
#endif
            ExecSQLWE(connectionID, sql);

            GetTempTableUserRoles();
            GetUserRolesKey("t" + _nzpUser + "_roleskey");
            GetUserAction("t" + _nzpUser + "_role_actions");
            GetUserPages("t" + _nzpUser + "_role_pages");
            GetUserArms("t" + _nzpUser + "_roles_arms");
            ExecSQL(_connDb, "drop table " + temp_user_role, false);
        }

        /// <summary>
        /// Перегрузить права пользователей
        /// </summary>
        /// <param name="connectionID"></param>
        /// <param name="nzp_role"></param>
        public void ReloadUserRights(int nzp_role)
        {
            string sql = "";
            
            IDataReader reader = null;
            IDbConnection conn_db = null;
            try
            {
                conn_db = GetConnection(Constants.cons_Webdata);
                Returns ret = OpenDb(conn_db, true);
                if (!ret.result) throw new Exception(ret.text);

#if PG
                sql = "set search_path to 'public'";
#else
                sql = " set encryption password " + Utils.EStrNull(BasePwd);
#endif
                ExecSQLWE(conn_db, sql);

                sql = "select nzp_user from userp where nzp_role = " + nzp_role;
                ret = ExecRead(conn_db, out reader, sql, true);
                if (!ret.result) throw new Exception(ret.text);

                while (reader.Read())
                {
                    this.FillingKeyRoles(conn_db, Convert.ToInt32(reader["nzp_user"]));
                }               
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка функцкии ReloadUserRoles. " + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                    reader.Dispose();
                }
            }
        }

        /// <summary>
        /// Получить подсистемы и роли пользователя
        /// </summary>
        private void GetTempTableUserRoles()
        {
            // nzp_role имеет широкое понятие
            // это и код роли и код подсистемы
            ExecSQL(_connDb, " drop table " + temp_user_role, false);

            string sql = "create temp table " + temp_user_role + " (" +
                " subsystem_id integer default 0, " +
                " nzp_role     integer, " +
                " nzp_user     integer)";
            ExecSQLWE(_connDb, sql);

            // основные роли
            sql = "insert into " + temp_user_role + "(nzp_role, nzp_user) " +
                " Select a.nzp_role, a.nzp_user " +
                " From userp a, s_roles b " +
                " Where a.nzp_user = " + _nzpUser +
                "   and a.nzp_role = b.nzp_role " +
                "   and b.is_active = 1"; 
#if PG
            //sql += " and a.nzp_user||CAST(a.nzp_role as TEXT)||'-'||a.nzp_usp||'userp' = a.sign";
#else
            sql += " and a.nzp_user||a.nzp_role||'-'||a.nzp_usp||'userp' = decrypt_char(a.sign)";
#endif
            ExecSQLWE(_connDb, sql);

            // расширяющие роли
            sql = " insert into " + temp_user_role + " (nzp_role,nzp_user) " + 
                " select r.kod,u.nzp_user " + 
                " from userp u, roleskey r, s_roles b " + 
                " where u.nzp_user = " + _nzpUser + 
                "   and r.tip = " + Constants.role_sql_ext + 
                "   and u.nzp_role = r.nzp_role " + 
                "   and r.kod = b.nzp_role " + 
                "   and b.is_active = 1 ";
#if PG
            //sql += " and r.tip||CAST(r.kod as TEXT)||r.nzp_role||'-'||r.nzp_rlsv||'roles' = decrypt_char(r.sign)";
#else
            sql += " and r.tip||r.kod||r.nzp_role||'-'||r.nzp_rlsv||'roles' = decrypt_char(r.sign)";
#endif
            ExecSQLWE(_connDb, sql);

            // определить подсистемы 
            // для основных (непользовательских) ролей код подсистемы = коду роли
            sql = "update " + temp_user_role + " t set subsystem_id = nzp_role where nzp_role < 1000";
            ExecSQLWE(_connDb, sql);

            // определить дополнительные (подчиненные) роли и их подсистемы
            sql = " insert into " + temp_user_role + " (subsystem_id, nzp_role, nzp_user) " +
                " select rol.nzp_role, u.nzp_role, u.nzp_user " +
                " From roleskey rol, " + temp_user_role + " u " +
                " where u.nzp_role = rol.kod " +
                "   and rol.tip = " + Constants.role_sql_subrole +
                " and exists" +
                  " (select 1 from " + temp_user_role + " t" +
                  " where t.subsystem_id = rol.nzp_role and t.subsystem_id = t.nzp_role)";
            ExecSQLWE(_connDb, sql);
            
            ExecSQLWE(_connDb, " Create index ix_t_temp_" + _nzpUser + "_1 on " + temp_user_role + " (nzp_user)");
            ExecSQLWE(_connDb, " Create index ix_t_temp_" + _nzpUser + "_2 on " + temp_user_role + " (nzp_role)");
            ExecSQLWE(_connDb, " Create index ix_t_temp_" + _nzpUser + "_3 on " + temp_user_role + " (subsystem_id)");
            ExecSQLWE(_connDb, DBManager.sUpdStat + " " + temp_user_role);
        }

        /// <summary>
        /// Заполнить ограничения пользователя 
        /// </summary>
        private void GetUserRolesKey(string roleskey)
        {
            string sql = "";
            string temp_table = "temp_" + roleskey;
            
            if (!TempTableInWebCashe(_connDb, roleskey))
            {
                sql = " Create table " + roleskey + " ( " +
                    " id serial not null, " +
                    " nzp_role integer, " +
                    " tip integer, " +
                    " kod integer) ";
                ExecSQLWE(_connDb, sql);

                ExecSQLWE(_connDb, " Create index ix_t_roleskey_" + _nzpUser + "_1 on " + roleskey + " (id)");
                ExecSQLWE(_connDb, " Create index ix_t_roleskey_" + _nzpUser + "_2 on " + roleskey + " (nzp_role)");
                ExecSQLWE(_connDb, " Create index ix_t_roleskey_" + _nzpUser + "_3 on " + roleskey + " (tip)");
                ExecSQLWE(_connDb, " Create index ix_t_roleskey_" + _nzpUser + "_4 on " + roleskey + " (kod)");
            }
            
            ExecSQL(_connDb, "drop table " + temp_table, false);
            sql = "create temp table " + temp_table + "(" +
                " nzp_role integer, " +
                " kod integer, " +
                " tip integer " + ")";
            ExecSQLWE(_connDb, sql);

            sql = " Insert into " + temp_table + " (nzp_role, kod, tip)  " + 
                " Select r.nzp_role, r.kod, r.tip " + 
                " From roleskey r, " + temp_user_role + " u " + 
                " Where r.nzp_role = u.nzp_role " + 
                "   and u.nzp_user =" + _nzpUser + 
                "   and u.nzp_role <> " + Constants.roleRaschetNachisleniy + 
                " group by 1,2,3";
#if PG
            //sql += " and r.tip ||CAST(r.kod as TEXT)||r.nzp_role ||'-'||r.nzp_rlsv ||'roles' = r.sign ";
#else
            sql +=  " and r.tip ||r.kod ||r.nzp_role ||'-'||r.nzp_rlsv ||'roles' = decrypt_char(r.sign) ";
#endif
            ExecSQLWE(_connDb, sql);
            ExecSQLWE(_connDb, " Create index ix_" + temp_table + "_1 on " + temp_table + " (nzp_role, kod)");
            ExecSQLWE(_connDb, DBManager.sUpdStat + " " + temp_table);

            // добавить отсутствующие строки
            sql = "Insert into " + roleskey + " (nzp_role, kod, tip) " +
                " Select t.nzp_role, t.kod, t.tip " + 
                " From " + temp_table + " t " +
                " Where not exists (select 1 from " + roleskey + " r where r.nzp_role = t.nzp_role and r.kod = t.kod)";
            ExecSQLWE(_connDb, sql);

            // удалить лишние строки
            sql = "delete from " + roleskey + " r " +
                " where not exists (select 1 from " + temp_table + " t where r.nzp_role = t.nzp_role and r.kod = t.kod) ";
            ExecSQLWE(_connDb, sql);

            ExecSQL(_connDb, "drop table " + temp_table, false);

            ExecSQLWE(_connDb, DBManager.sUpdStat + " " + roleskey);
        }

        /// <summary>
        /// Заполнить действия
        /// </summary>
        private void GetUserAction(string role_actions)
        {
            string sql = "";
            string temp_table = "temp_" + role_actions;
            
            if (!TempTableInWebCashe(_connDb, role_actions))
            {
                sql = " Create table " + role_actions + " ( " + 
                    " id serial  not null, " + 
                    " nzp_role integer, " + 
                    " nzp_act integer, " + 
                    " mod_act integer, " + 
                    " nzp_page integer )";
                ExecSQLWE(_connDb, sql);

                ExecSQL(_connDb, " Create index ix_t_role_actions_" + _nzpUser + "_1 on " + role_actions + " (nzp_role); ", false);
                ExecSQL(_connDb, " Create index ix_t_role_actions_" + _nzpUser + "_2 on " + role_actions + " (nzp_act); ", false);
                ExecSQL(_connDb, " Create index ix_t_role_actions_" + _nzpUser + "_3 on " + role_actions + " (mod_act); ", false);
                ExecSQL(_connDb, " Create index ix_t_role_actions_" + _nzpUser + "_4 on " + role_actions + " (nzp_page); ", false);          
            }

            ExecSQL(_connDb, "drop table " + temp_table, false);

            sql = " Create table " + temp_table + " ( " +
                    " nzp_role integer, " +
                    " nzp_act integer, " +
                    " mod_act integer, " +
                    " nzp_page integer )";
            ExecSQLWE(_connDb, sql);

            sql = "Insert into " + temp_table + " (nzp_role, nzp_act, mod_act, nzp_page) " +
                " Select u.subsystem_id, r.nzp_act, r.mod_act, r.nzp_page " +
                " From role_actions r, " + temp_user_role + " u  " +
                " Where r.nzp_role = u.nzp_role ";
#if PG
            //sql += " and r.nzp_role||CAST(r.nzp_page as TEXT)||r.nzp_act||'-'||r.id||'role_actions' = r.sign";
#else
            sql += " and r.nzp_role||r.nzp_page||r.nzp_act||'-'||r.id||'role_actions' = decrypt_char(r.sign)";
#endif
            sql += " group by 1,2,3,4";
            ExecSQLWE(_connDb, sql);

            ExecSQLWE(_connDb, " Create index ix_" + temp_table + "_1 on " + temp_table + " (nzp_role, nzp_act, nzp_page)");
            ExecSQLWE(_connDb, DBManager.sUpdStat + " " + temp_table);

            // добавить отсутствующие строки
            sql = "Insert into " + role_actions + " (nzp_role, nzp_act, mod_act, nzp_page) " +
                " Select t.nzp_role, t.nzp_act, t.mod_act, t.nzp_page " + 
                " From " + temp_table + " t " +
                " Where not exists (select 1 from " + role_actions + " r " +
                "   where r.nzp_role = t.nzp_role and r.nzp_act = t.nzp_act and r.nzp_page = t.nzp_page)";
            ExecSQLWE(_connDb, sql);

            // удалить лишние строки
            sql = "delete from " + role_actions + " r " +
                " where not exists (select 1 from " + temp_table + " t " +
                "   where r.nzp_role = t.nzp_role and r.nzp_act = t.nzp_act and r.nzp_page = t.nzp_page) ";
            ExecSQLWE(_connDb, sql);

            ExecSQL(_connDb, "drop table " + temp_table, false);

            ExecSQLWE(_connDb, DBManager.sUpdStat + " " + role_actions);
        }

        /// <summary>
        /// Заполнить страницы
        /// </summary>
        private void GetUserPages(string role_pages)
        {
            string sql = "";
            string temp_table = "temp_" + role_pages;
            
            if (!TempTableInWebCashe(_connDb, role_pages))
            {
                sql = " Create table " + role_pages + " ( " + 
                    " id serial  not null, " + 
                    " nzp_role integer, " + 
                    " nzp_page integer ) ";
                ExecSQLWE(_connDb, sql);

                ExecSQLWE(_connDb, " Create index ix_t_role_pages_" + _nzpUser + "_1 on " + role_pages + " (nzp_page)");
                ExecSQLWE(_connDb, " Create index ix_t_role_pages_" + _nzpUser + "_2 on " + role_pages + " (nzp_role)");
            }

            ExecSQL(_connDb, "drop table " + temp_table, false);

            sql = " Create table " + temp_table + " ( " +
                    " nzp_role integer, " +
                    " nzp_page integer ) ";
            ExecSQLWE(_connDb, sql);

            sql = " Insert into " + temp_table + " (nzp_role, nzp_page) " +
                " Select u.subsystem_id, r.nzp_page " +
                " From role_pages r, " + temp_user_role + " u " +
                " Where r.nzp_role = u.nzp_role ";
#if PG
            //sql += " and r.nzp_role||CAST(r.nzp_page as TEXT)||'-'||r.id||'role_pages' = decrypt_char(r.sign)";
#else
            sql += " and r.nzp_role||r.nzp_page||'-'||r.id||'role_pages' = decrypt_char(r.sign)";
#endif
            sql += " group by 1,2";
            ExecSQLWE(_connDb, sql);

            ExecSQLWE(_connDb, " Create index ix_" + temp_table + "_1 on " + temp_table + " (nzp_role, nzp_page)");
            ExecSQLWE(_connDb, DBManager.sUpdStat + " " + temp_table);

            // добавить отсутствующие строки
            sql = "Insert into " + role_pages + " (nzp_role, nzp_page) " +
                " Select t.nzp_role, t.nzp_page " +
                " From " + temp_table + " t " +
                " Where not exists (select 1 from " + role_pages + " r " +
                "   where r.nzp_role = t.nzp_role and r.nzp_page = t.nzp_page)";
            ExecSQLWE(_connDb, sql);

            // удалить лишние строки
            sql = "delete from " + role_pages + " r " +
                " where not exists (select 1 from " + temp_table + " t " +
                "   where r.nzp_role = t.nzp_role and r.nzp_page = t.nzp_page) ";
            ExecSQLWE(_connDb, sql);

            ExecSQL(_connDb, "drop table " + temp_table, false);

            ExecSQLWE(_connDb, DBManager.sUpdStat + " " + role_pages);
        }

        /// <summary>
        /// Заполнить АРМ
        /// </summary>
        private void GetUserArms(string roles_arms)
        {
            string sql = "";
            string role = "";
            string temp_table = "temp_" + roles_arms;
         
            if (!TempTableInWebCashe(_connDb, roles_arms))
            {
                sql = " Create table " + roles_arms + " (" +
                    " id serial not null, " +
                    " nzp_role integer, " +
                    " page_url integer, " +
                    " role char(120), " +
                    " sort integer, " +
                    " img_url char(255) ) ";
                ExecSQLWE(_connDb, sql);

                ExecSQL(_connDb, " Create index ix_t_roles_arms_" + _nzpUser + "_1 on " + roles_arms + " (id); ", false);
                ExecSQL(_connDb, " Create index ix_t_roles_arms_" + _nzpUser + "_2 on " + roles_arms + " (nzp_role); ", false);
                ExecSQL(_connDb, " Create index ix_t_roles_arms_" + _nzpUser + "_3 on " + roles_arms + " (page_url); ", false);
                ExecSQL(_connDb, " Create index ix_t_roles_arms_" + _nzpUser + "_4 on " + roles_arms + " (sort); ", false);
            }

#if PG
            role = "s.role";
#else
            role = "decrypt_char(s.role) as role";
#endif
            ExecSQL(_connDb, "drop table " + temp_table, false);

            sql = " Create table " + temp_table + " ( " +
                    " nzp_role integer, " +
                    " page_url integer, " +
                    " role char(120), " +
                    " sort integer, " +
                    " img_url char(255) ) ";
            ExecSQLWE(_connDb, sql);

            sql = " Insert into " + temp_table + " (nzp_role, page_url, role, sort, img_url) " +
                " Select s.nzp_role, s.page_url, " + role + ", s.sort, i.img_url " +
                " From s_roles s " + 
                "   left outer join img_lnk i on i.tip = 3 and i.kod = s.nzp_role and i.cur_page = 0, " +
                " role_pages r, " + temp_user_role + " u " +
                " Where s.nzp_role = r.nzp_role " +
                "   and s.nzp_role = u.subsystem_id " +
                "   and s.page_url > 0 " + 
                "   and s.page_url = r.nzp_page " + 
                " and s.is_active = 1 " + 
                " group by 1,2,3,4,5";
            ExecSQLWE(_connDb, sql);

            // добавить отсутствующие строки
            sql = "Insert into " + roles_arms + " (nzp_role, page_url, role, sort, img_url) " +
                " Select t.nzp_role, t.page_url, t.role, t.sort, t.img_url " +
                " From " + temp_table + " t " +
                " Where not exists (select 1 from " + roles_arms + " r where r.nzp_role = t.nzp_role)";
            ExecSQLWE(_connDb, sql);

            // удалить лишние строки
            sql = "delete from " + roles_arms + " r " +
                " where not exists (select 1 from " + temp_table + " t where r.nzp_role = t.nzp_role) ";
            ExecSQLWE(_connDb, sql);

            ExecSQL(_connDb, "drop table " + temp_table, false);
        }
    }
      
    public partial class DbUserClient : DataBaseHeadClient
    {
        //----------------------------------------------------------------------
        public bool CloseSeans(int nzp_user, string idses) //закрыть сеанс
        //----------------------------------------------------------------------
        {
            Returns ret = new Returns(true);
            bool res = true;

            try
            {
                ret = ExecSQL(" Update users Set dat_log = now() Where nzp_user = " + nzp_user);
                if (!ret.result) throw new Exception(ret.text);

                BaseUser bu = new BaseUser();
                bu.nzp_user = nzp_user;
                bu.idses = idses;
                
                using (DbLogClient db = new DbLogClient())
                {
                    res = db.LogAcc(this.ClientConnection, bu, Constants.acc_exit);
                }

                return true;
            }
            catch
            { 
                return false;
            }
        }//CloseSeans
      
        public void GetRoles(int nzp_role, int nzp_user, int cur_page, ref int mod, ref List<_RoleActions> RoleActions, ref List<_RolePages> RolePages, string idses) //загрузить права доступа
        {
            IDbConnection connectionID = GetConnection(Constants.cons_Webdata);
            if (!OpenDb(connectionID, true).result) return;

            //надо отметить, что данная сессия еще жива
            if (idses.Trim() != "")
            {
#if PG
                if (!ExecSQL(connectionID,
                      " Update " + sDefaultSchema + "log_sessions Set dat_log = now() " +
                      " Where nzp_user = " + nzp_user + " and idses = " + Utils.EStrNull(idses)
                       , true).result)
#else
                if (!ExecSQL(connectionID,
                      " Update log_sessions Set dat_log = current " +
                      " Where nzp_user = " + nzp_user + " and idses = " + Utils.EStrNull(idses)
                       , true).result)
#endif
                {
                    connectionID.Close();
                    return;
                }
            }

            //удалить просроченные сессии
#if PG
            if (!ExecSQL(connectionID,
                  " Delete From " + sDefaultSchema + "log_sessions Where (now() - INTERVAL '" + Constants.users_min + " minutes') > dat_log and session_id is null"
                   , true).result)
#else
            if (!ExecSQL(connectionID,
                  " Delete From log_sessions Where (current - " + Constants.users_min + " units minute) > dat_log and session_id is null"
                   , true).result)
#endif
            {
                connectionID.Close();
                return;
            }

            //проверка наличия активной сессии пользователя
            Returns ret;
            IDataReader reader;
            if (idses.Trim() != "" && nzp_role != Constants.roleAdministrator)  //не учитывать сессии в Администраторе, чтобы дать возможность зайти и снять зависшие сессии
            {
#if PG
                ret = ExecRead(connectionID, out reader, "Select nzp_user From " + sDefaultSchema + "log_sessions Where nzp_user = " + nzp_user + " and idses = " + Utils.EStrNull(idses), true);
#else
    ret = ExecRead(connectionID, out reader, "Select nzp_user From log_sessions Where nzp_user = " + nzp_user + " and idses = " + Utils.EStrNull(idses), true);
#endif
                if (!ret.result)
                {
                    connectionID.Close();
                    return;
                }
                bool b = reader.Read();
                reader.Close();
                reader.Dispose();
                if (!b) //активная сессия пользователя не найдена
                {
                    connectionID.Close();
                    return;
                }
            }
            string table_XX = "t" + nzp_user;

            #region загрузка списка доступных действий
#if PG
            string sql = " Select distinct nzp_role, nzp_act" +
                " From " + table_XX + "_role_actions Where " +
                " nzp_page = " + cur_page +
                " {role_filter}" +
                " {filter}";
#else

            string sql = " Select unique nzp_role, nzp_act" +
                " From " + table_XX + "_role_actions Where " +
                " nzp_page = " + cur_page +
                " {role_filter}" +
                " {filter}";
#endif

            // Загрузка прав для заданного режима
            string filter = "";
            string role_filter = "";

            if (nzp_role > 0)
            {
                role_filter = " and nzp_role =" + nzp_role;
            }

            sql = sql.Replace("{role_filter}", role_filter);

            if (mod > 0)
            {
                if (!ExecRead(connectionID, out reader, sql.Replace("{filter}", " and  nzp_act = " + mod), true).result)
                {
                    connectionID.Close();
                    return;
                }
                if (reader.Read())
                {
                    reader.Close();
                    filter += " and (mod_act = " + mod + " or mod_act is null)";
                }
                else
                {
                    reader.Close();
                    mod = (mod == Constants.act_mode_edit ? Constants.act_mode_view : Constants.act_mode_edit);
                    if (!ExecRead(connectionID, out reader, sql.Replace("{filter}", " and nzp_act = " + mod), true).result)
                    {
                        connectionID.Close();
                        return;
                    }
                    if (reader.Read()) filter += " and (mod_act = " + mod + " or mod_act is null)";
                    else
                    {
                        mod = Constants.act_mode_view;
                        filter += " and mod_act is null";
                    }
                    reader.Close();
                }
            }

            sql = sql.Replace("{filter}", filter);



            if (!ExecRead(connectionID, out reader,
                sql, true).result)
            {
                connectionID.Close();
                return;
            }

            try
            {
                while (reader.Read())
                {
                    _RoleActions zap_a = new _RoleActions();
                    //zap_a.nzp_page = (int)reader["nzp_page"];
                    zap_a.nzp_act = (int)reader["nzp_act"];
                    zap_a.nzp_role = (int)reader["nzp_role"];
                    RoleActions.Add(zap_a);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка заполнения role_actions: " + ex.Message, MonitorLog.typelog.Error, 20, 101, true);
                ExecSQL(connectionID, "drop table _temp", false);
                connectionID.Close();
                return;
            }
            finally
            {
                reader.Close();
                reader.Dispose();
            }
            #endregion

            #region загрузка списка доступных страниц
#if PG
            sql = " Select distinct nzp_role , nzp_page" +
                " From " + table_XX + "_role_pages " +
                "  {role_filter}";
#else
            sql = " Select unique nzp_role , nzp_page" +
                " From " + table_XX + "_role_pages " +
                "  {role_filter}";
#endif

            if (nzp_role > 0)
            {
                role_filter = " Where nzp_role =" + nzp_role;
            }

            sql = sql.Replace("{role_filter}", role_filter);

            if (!ExecRead(connectionID, out reader,
              sql, true).result)
            {
                connectionID.Close();
                return;
            }

            try
            {
                while (reader.Read())
                {
                    _RolePages zap_p = new _RolePages();
                    zap_p.nzp_page = (int)reader["nzp_page"];
                    zap_p.nzp_role = (int)reader["nzp_role"];
                    RolePages.Add(zap_p);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка заполнения role_pages " + ex.Message, MonitorLog.typelog.Error, 20, 101, true);
                connectionID.Close();
                return;
            }
            finally
            {
                reader.Close();
                reader.Dispose();
            }
            #endregion

            connectionID.Close();
        }//GetRolesNew

        //----------------------------------------------------------------------
        public void GetArmsNew(int nzp_user, ref List<_Arms> Arms) //загрузить права доступа
        //----------------------------------------------------------------------
        {
            IDbConnection connectionID = GetConnection(Constants.cons_Webdata);
            if (!OpenDb(connectionID, true).result) return;

            string select = ", '' as url";
            string from = "";
            string where = "";

            if (TempTableInWebCashe(connectionID, "foreign_systems"))
            {
#if PG
                select = ", fs.url";
                from = "left outer join foreign_systems fs on fs.nzp_role = s.nzp_role";
                where = " ";
#else
                select = ", fs.url";
                from = ", outer foreign_systems fs";
                where = " and fs.nzp_role = s.nzp_role";
#endif
            }
            string table_XX = "t" + nzp_user;

            //S_ROLES
            IDataReader reader;
#if PG
            if (!ExecRead(connectionID, out reader,
                 " Select distinct nzp_role, page_url, role, sort, img_url " + select +
                 " From " + table_XX + "_roles_arms " + from +
                 " Where 1=1 " + where +
                 " Order by 4,3 ", true).result)
            {
                connectionID.Close();
                return;
            }
#else
            if (!ExecRead(connectionID, out reader,
                " Select unique nzp_role, page_url, role, sort, img_url " + select +
                " From " + table_XX + "_roles_arms " + from +
                " Where 1=1 " + where +
                " Order by 4,3 ", true).result)
            {
                connectionID.Close();
                return;
            }
#endif
            try
            {
                while (reader.Read())
                {
                    _Arms zap = new _Arms();
                    zap.role = (string)reader["role"];
                    zap.nzp_role = (int)reader["nzp_role"];
                    zap.page_url = (int)reader["page_url"];
                    zap.url = reader["url"] != DBNull.Value ? Convert.ToString(reader["url"]).Trim() : "";

                    if (reader["img_url"] != DBNull.Value) zap.img_url = (string)reader["img_url"];
                    else zap.img_url = "";

                    zap.role = zap.role.Trim();
                    zap.img_url = zap.img_url.Trim();
                    Arms.Add(zap);
                }
            }
            catch (Exception ex)
            {
                string err;
                if (Constants.Viewerror)
                    err = ex.Message;
                else
                    err = "";

                MonitorLog.WriteLog("Ошибка заполнения Arms " + err, MonitorLog.typelog.Error, 20, 101, true);
                reader.Close();
                connectionID.Close();
                return;
            }


            reader.Close();
            connectionID.Close();
        }//GetArms


        public void GetRolesVal(int nzp_role, int nzp_user, ref List<_RolesVal> RolesVal) //загрузить права доступа
        {
#if PG
            ExecSQL("set search_path to 'public'", false);
#endif

            string table_XX = "t" + nzp_user + "_roleskey";
            MyDataReader reader;
            if (nzp_role > 0)
            {
                string sql = " Select distinct nzp_role, tip, kod " +
                    " From " + table_XX + 
                    " Where nzp_role = " + nzp_role + " or nzp_role in (select kod from " + table_XX + " r where r.nzp_role = "
                    + nzp_role + " and  r.tip =" + Constants.role_sql_subrole + " ) or nzp_role >= 1000 order by tip, kod";

                if (!ExecRead(out reader, sql).result)
                {
                    ExecSQL("drop table t_user_roles", false);
                    return;
                }

                Dictionary<int, List<int>> dict = new Dictionary<int, List<int>>();
                try
                {
                    int tab = -99;
                    //int nzp = -99;

                    while (reader.Read())
                    {
                        tab = (int)reader["tip"];

                        if (!dict.Keys.Contains(tab))
                        {
                            dict[tab] = new List<int>();
                        }
                        dict[tab].Add((int)reader["kod"]);
                    }

                    foreach (KeyValuePair<int, List<int>> kv in dict)
                    {
                        RolesVal.Add(new _RolesVal()
                        {
                            tip = Constants.role_sql,
                            kod = kv.Key,
                            val = String.Join(",", kv.Value.ConvertAll<string>(Convert.ToString).ToArray())
                        });
                    }
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка заполнения roleskey " + (Constants.Viewerror ? ex.Message : ""), MonitorLog.typelog.Error, 20, 101, true);
                }
                finally
                {
                    reader.Close();
                }

                ExecSQL("drop table t_user_roles", false);
            }
        }//GetRolesVal

        //----------------------------------------------------------------------
        public void GetRolesKey(int nzp_role, int nzp_user, ref List<_RolesVal> RolesVal) //загрузить права доступа
        //----------------------------------------------------------------------
        {
            GetRolesKey(nzp_role, "", "", nzp_user, ref RolesVal);
        }
        //----------------------------------------------------------------------
        public void GetRolesKey(int nzp_role, string tip, string kod, int nzp_user, ref List<_RolesVal> RolesVal) //загрузить права доступа
        //----------------------------------------------------------------------
        {
            IDbConnection connectionID = GetConnection(Constants.cons_Webdata);
            if (!OpenDb(connectionID, true).result) return;

            string table_XX = "t" + nzp_user;
            string srole = "";

            if (nzp_role > 0 || tip.Trim() != "" || kod.Trim() != "")
                srole = " Where 1=1 ";

            if (nzp_role > 0)
                srole = " and r.nzp_role = " + nzp_role;

            if (tip.Trim() != "")
                srole += " and r.tip in (" + tip + ")";
            if (kod.Trim() != "")
                srole += " and r.kod in (" + kod + ")";

            //ROLESKEY
            IDataReader reader;
            if (!ExecRead(connectionID, out reader, "Select tip,kod,nzp_role From " + table_XX + "_roleskey " + srole, true).result)
            {
                connectionID.Close();
                return;
            }
            try
            {
                while (reader.Read())
                {
                    _RolesVal zap = new _RolesVal();
                    zap.tip = (int)reader["tip"];
                    zap.kod = (int)reader["kod"];
                    zap.val = "0";
                    zap.nzp_role = (int)reader["nzp_role"];

                    RolesVal.Add(zap);
                }
            }
            catch (Exception ex)
            {
                string err;
                if (Constants.Viewerror)
                    err = ex.Message;
                else
                    err = "";

                MonitorLog.WriteLog("Ошибка заполнения roleskey " + err, MonitorLog.typelog.Error, 20, 101, true);
                reader.Close();
                connectionID.Close();
                return;
            }


            reader.Close();
            connectionID.Close();
        }//GetRolesKey

        //----------------------------------------------------------------------
        public BaseUser GetUser(User finder)
        //----------------------------------------------------------------------
        {
            if (finder.nzpuser < 1 && finder.login == "")
                return null;

            IDbConnection connectionID = GetConnection(Constants.cons_Webdata);
            if (!OpenDb(connectionID, true).result) return null;

            IDataReader reader;

#if PG
            string sql = "select nzp_user, date_begin, nzp_payer from public.users where 1=1 ";
#else
            string sql = "select nzp_user, date_begin, nzp_payer from users where 1=1 ";
#endif
            if (finder.nzpuser > 0)
                sql += " and nzp_user = " + finder.nzpuser;
            else
                if (finder.login != "")
                    sql += " and login = " + Utils.EStrNull(finder.login, "");

            if (finder.is_remote == 1)
                sql += " and is_remote = 1 ";

            Returns ret;
            ret = ExecRead(connectionID, out reader, sql, true);
            if (!ret.result)
            {
                connectionID.Close();
                return null;
            }

            BaseUser user = new BaseUser();

            try
            {
                if (reader.Read())
                {
                    if (reader["date_begin"] != DBNull.Value) user.date_begin = String.Format("{0:dd.MM.yyyy}", reader["date_begin"]);
                    if (reader["nzp_user"] != DBNull.Value) user.nzp_user = Convert.ToInt32(reader["nzp_user"]);
                    if (reader["nzp_payer"] != DBNull.Value) user.nzp_payer = Convert.ToInt32(reader["nzp_payer"]);
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                if (Constants.Debug) MonitorLog.WriteLog("Ошибка в функции DbLoginAspx.GetUser: " + ex.Message, MonitorLog.typelog.Error, true); ;
                user = null;
            }
            if (reader != null) reader.Close();

            #region Загрузка логина пользователя для удаленного сервера
            if (user != null && finder.nzp_server > 0)
            {
                if (TempTableInWebCashe(connectionID, "users_links"))
                {
#if PG
                    sql = "select login from users_links where nzp_user = " + finder.nzpuser + " and nzp_server = " + finder.nzp_server +
                        " and CAST(nzp_user_link as text)||CAST(nzp_user as text)||CAST(nzp_role as text)||CAST(nzp_server as text)||'links' = sign";
#else
                    if (ExecSQL(connectionID, " set encryption password '" + BasePwd + "'", true).result)
                    {
                        sql = "select decrypt_char(login) as login from users_links where nzp_user = " + finder.nzpuser + " and nzp_server = " + finder.nzp_server +
                            " and nzp_user_link ||nzp_user ||nzp_role ||nzp_server ||'links' = decrypt_char(sign)";
#endif
                    ret = ExecRead(connectionID, out reader, sql, true);
                    if (ret.result)
                    {
                        try
                        {
                            if (reader.Read())
                            {
                                if (reader["login"] != DBNull.Value) user.remoteLogin = Convert.ToString(reader["login"]).Trim();
                            }
                        }
                        catch (Exception ex)
                        {
                            if (Constants.Debug) MonitorLog.WriteLog("Ошибка в функции DbLoginAspx.GetUser: " + ex.Message, MonitorLog.typelog.Error, true); ;
                        }
                        if (reader != null) reader.Close();
                    }
#if PG
#else
                    }
#endif
                }
            }
            #endregion

            connectionID.Close();

            user.nzp_disp = Payers.DispatchingOffice.GetHashCode();

            return user;
        }

        protected bool DropCasheTables(IDbConnection conn_web, int nzp_user, string tab) //
        {
            string stab = "";
            if (tab.Trim() != "") stab = " and tabname = '" + stab + "'";

            IDataReader reader;
#if PG
            if (!ExecRead(conn_web, out reader,
                " Select table_name as tabname From information_schema.tables " +
                " Where table_schema = 'public' and lower(table_name) like 't" + nzp_user.ToString().ToLower() + "\\_%' " + stab, true).result)
#else
            if (!ExecRead(conn_web, out reader,
                " Select tabname From systables " +
                " Where lower(tabname) matches 't" + nzp_user.ToString().ToLower() + "_*' " + stab, true).result)
#endif
            {
                return false;
            }

            try
            {
                while (reader.Read())
                {
                    string sql = "";
                    if (reader["tabname"] != DBNull.Value)
                    {
                        sql = " Drop table " + (string)reader["tabname"];
                        ExecSQL(conn_web, sql.Trim(), false);
                    }
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка удаления временных таблиц \n " + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                return false;
            }
            finally
            {
                reader.Close();
                reader.Dispose();
            }
            return true;
        }
    }
}
