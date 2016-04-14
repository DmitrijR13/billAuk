using System;
using System.Data;
using System.Configuration;
using System.Linq;
using System.Xml.Linq;
using System.Collections.Generic;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using System.Text;

namespace STCLINE.KP50.DataBase
{
    public partial class DbUserClient : DataBaseHead
    {
        //----------------------------------------------------------------------
        public bool Authenticate(BaseUser web_user, string ip, string browser, string idses, out int inbase) //проверка пользователя
        //----------------------------------------------------------------------
        {
            inbase = 0;
            IDbConnection connectionID = GetConnection(Constants.cons_Webdata);
            if (!OpenDb(connectionID, true).result) return false;
#if PG
            ExecSQL(connectionID, "set search_path to public", false);
#else
            if (!ExecSQL(connectionID,
                  " set encryption password '" + BasePwd + "'"
                  , true).result)
            {
                connectionID.Close();
                return false;
            }
#endif

            IDataReader reader;
#if PG
            int nzp = 0;
            Returns ret = Utils.InitReturns();
            try { nzp = Convert.ToInt32(ExecScalar(connectionID, "SELECT nzp_user FROM users WHERE login = " + Utils.EStrNull(web_user.login), out ret, true)); }
            catch
            {
                connectionID.Close();
                return false;
            }

            if (!ExecRead(connectionID, out reader,
                            " Select nzp_user, login, uname, dat_log, date_begin" +
                //", case when dat_log - (current - " + Constants.users_min + " units minute) > 0 units minute then 1 else 0 end as pole " +
                            " From users a Where (CAST(is_blocked as int) <> 1 or is_blocked is null) and login = '" + web_user.login + "' " +
                            "  and 0 < ( Select count(*) From users b Where a.nzp_user = b.nzp_user " +
                //"  and b.pwd = (CAST(a.nzp_user as text)||'-'||'" + web_user.password + "') " +
                            "  and b.pwd = " + Utils.EStrNull(Utils.CreateMD5StringHash(web_user.password + nzp + BasePwd))+ ")", true).result
                //"  and pwd = '" + web_user.password + "'", true).result
                           )
            {
                connectionID.Close();
                return false;
            }
#else
            if (!ExecRead(connectionID, out reader,
                " Select nzp_user, login, decrypt_char(uname) as uname, dat_log, date_begin" +
                //", case when dat_log - (current - " + Constants.users_min + " units minute) > 0 units minute then 1 else 0 end as pole " +
                " From users a Where (is_blocked <> 1 or is_blocked is null) and login = '" + web_user.login + "' " +
                "  and 0 < ( Select count(*) From users b Where a.nzp_user = b.nzp_user " +
                "  and decrypt_char(pwd) = a.nzp_user||'-'||'" + web_user.password + "' " +
                "  and decrypt_char(pwd) <> a.nzp_user||'-#' )", true).result
                //"  and pwd = '" + web_user.password + "'", true).result
               )
            {
                connectionID.Close();
                return false;
            }
#endif
            if (!reader.Read())
            {
                connectionID.Close();


                web_user.idses = idses;
                web_user.ip_log = ip;
                web_user.browser = browser;
                DbLogClient dbLoagFailure = new DbLogClient();
                bool res = dbLoagFailure.LogAcc(web_user, Constants.acc_failure);
                dbLoagFailure.Close();

                return false;
            }
            web_user.nzp_user = (int)reader["nzp_user"];
            web_user.login = ((string)reader["login"]).Trim();
            web_user.uname = ((string)reader["uname"]).Trim();
            web_user.idses = idses;
            web_user.ip_log = ip;
            web_user.browser = browser;
            if (reader["date_begin"] != DBNull.Value) web_user.date_begin = String.Format("{0:dd.MM.yyyy}", reader["date_begin"]);

            bool b;
            b = true;
            if (reader["dat_log"] == DBNull.Value)
            {
                web_user.dat_log = Constants._UNDEF_; //dat_log = null
            }
            else
            {
                /*
                if ((int)reader["pole"] == 1) //время регистрации еще не вышло
                {
                    b = false; //пользователь был зарегестирован
                    web_user.dat_log = Convert.ToString((DateTime)reader["dat_log"]);
                }
                else
                    web_user.dat_log = Constants._UNDEF_; //вышло время таймаута
                */
                web_user.dat_log = Constants._UNDEF_;
            }
            reader.Close();

            //test
            //Conections.Set_connection(2);
            if (b)
            {
                if (Connections.Inc_connection())
                {
                    inbase = 1;
                }
                else
                {
                    //превышено кол-во разрешенных подключений, 
                    //проверим кол-во текущих подключений
                    IDbCommand cmd = DBManager.newDbCommand(" Select count(*) as cnt From log_sessions ", connectionID);
                    try
                    {
                        string s = Convert.ToString(cmd.ExecuteScalar());
                        inbase = Convert.ToInt32(s);
                    }
                    catch
                    {
                        connectionID.Close();
                        return false;
                    }

                    if (inbase >= 0) //кол-во текущих пользователей
                    {
                        //обновить счетчик
                        Connections.Set_connection(inbase);
                    }
                    //после обновления заново увеличим счетчик, 
                    if (!Connections.Inc_connection()) inbase = -1; //превышен лимит подключений
                }

#if PG
                if (!ExecSQL(connectionID,
                      " Update users Set dat_log = now(), ip_log = '" + ip + "', browser = '" + browser + "'" +
                      " Where nzp_user = " + web_user.nzp_user
                       , true).result)
#else
                if (!ExecSQL(connectionID,
                      " Update users Set dat_log = current, ip_log = '" + ip + "', browser = '" + browser + "'" +
                      " Where nzp_user = " + web_user.nzp_user
                       , true).result)
#endif
                {
                    if (inbase != -1)
                    {
                        Connections.Dec_connection();
                    }
                    connectionID.Close();
                    return false;
                }
            }
            connectionID.Close();
            FillingKeyRoles(web_user.nzp_user);
            if (inbase == -1)
            {
                return true;
            }
            else
            {
                DbLogClient db = new DbLogClient();
                bool res = db.LogAcc(web_user, Constants.acc_in); //зафиксируем сессию
                db.Close();
                return res;
            }

        }//Authenticate
        //----------------------------------------------------------------------
        public bool CloseSeans(int nzp_user, string idses) //закрыть сеанс
        //----------------------------------------------------------------------
        {
            IDbConnection connectionID = GetConnection(Constants.cons_Webdata);
            if (!OpenDb(connectionID, true).result) return false;

            DropCasheTables(connectionID, nzp_user, "");







#if PG
            if (!ExecSQL(connectionID,
                        " Update users Set dat_log = now() " +
                        " Where nzp_user = " + nzp_user.ToString()
                        , true).result
               )
            {
                connectionID.Close();
                return false;
            }
            connectionID.Close();
#else
            if (!ExecSQL(connectionID,
                        " Update users Set dat_log = " +DBManager.sCurDateTime+
                        " Where nzp_user = " + nzp_user.ToString()
                        , true).result
               )
            {
                connectionID.Close();
                return false;
            }
            connectionID.Close();
#endif








            BaseUser bu = new BaseUser();
            bu.nzp_user = nzp_user;
            bu.idses = idses;
            DbLogClient db = new DbLogClient();
            bool res = db.LogAcc(bu, Constants.acc_exit);
            db.Close();
            return res;
        }//CloseSeans

        //todo PostgreeSql
        public void FillingKeyRoles(int nzp_user)
        {
            IDbConnection connectionID = GetConnection(Constants.cons_Webdata);
            if (!OpenDb(connectionID, true).result) return;

#if PG
            if (!ExecSQL(connectionID,
               " set search_path to 'public'"
               , true).result)
            {
                connectionID.Close();
                return;
            }
#else
            if (!ExecSQL(connectionID,
               " set encryption password '" + BasePwd + "'"
               , true).result)
            {
                connectionID.Close();
                return;
            }
#endif


            string table_XX = "t" + nzp_user;

            ExecSQL(connectionID, " drop table _temp", false);
            ExecSQL(connectionID, " drop table " + table_XX + "_role_actions", false);
            ExecSQL(connectionID, " drop table " + table_XX + "_role_pages", false);
            ExecSQL(connectionID, " drop table " + table_XX + "_roleskey", false);
            ExecSQL(connectionID, " drop table " + table_XX + "_roles_arms", false);

            string sql = " select a.nzp_role,a.nzp_user " +
#if PG
 " into temp _temp " +
#endif
 " from userp a, s_roles b where a.nzp_user =" + nzp_user + " and a.nzp_role = b.nzp_role and b.is_active = 1 " +
#if PG
 " and a.nzp_user||CAST(a.nzp_role as TEXT)||'-'||a.nzp_usp||'userp' = a.sign";
#else
                " and a.nzp_user||a.nzp_role||'-'||a.nzp_usp||'userp' = decrypt_char(a.sign) into temp _temp";
#endif

            if (!ExecSQL(connectionID, sql, true).result)
            {
                connectionID.Close();
                return;
            }

            StringBuilder sqlString = new StringBuilder();

#if PG
            sqlString.Append(" insert into _temp (nzp_role,nzp_user) ");
            sqlString.Append(" select r.kod,u.nzp_user ");
            sqlString.Append(" from userp u, roleskey r, s_roles b ");
            sqlString.Append(" where u.nzp_user = " + nzp_user + " and r.tip = " + Constants.role_sql_ext + " and u.nzp_role = r.nzp_role ");
            sqlString.Append(" and r.kod = b.nzp_role and b.is_active = 1 ");
#else
            sqlString.Append(" insert into _temp (nzp_role,nzp_user) ");
            sqlString.Append(" select r.kod,u.nzp_user ");
            sqlString.Append(" from userp u, roleskey r, s_roles b ");
            sqlString.Append(" where u.nzp_user = " + nzp_user + " and r.tip = " + Constants.role_sql_ext + " and u.nzp_role = r.nzp_role ");
            sqlString.Append(" and r.kod = b.nzp_role and b.is_active = 1 ");
#endif

#if PG
            sqlString.Append(" and r.tip||CAST(r.kod as TEXT)||r.nzp_role||'-'||r.nzp_rlsv||'roles' = decrypt_char(r.sign); ");
#else
            sqlString.Append(" and r.tip||r.kod||r.nzp_role||'-'||r.nzp_rlsv||'roles' = decrypt_char(r.sign); ");
#endif

            if (!ExecSQL(connectionID, sqlString.ToString(), true).result)
            {
                connectionID.Close();
                return;
            }

            sqlString.Remove(0, sqlString.Length);

            //Заполнение tXX_roleskey
#if PG
            sqlString.Append(" Create table " + table_XX + "_roleskey( ");
            sqlString.Append(" id serial not null, ");
            sqlString.Append(" nzp_role integer, ");
            sqlString.Append(" tip integer, ");
            sqlString.Append(" kod integer ) ");
#else
         sqlString.Append(" Create table " + table_XX + "_roleskey( ");
            sqlString.Append(" id serial not null, ");
            sqlString.Append(" nzp_role integer, ");
            sqlString.Append(" tip integer, ");
            sqlString.Append(" kod integer ) ");
#endif


            if (!ExecSQL(connectionID, sqlString.ToString(), true).result)
            {
                connectionID.Close();
                return;
            }
            sqlString.Remove(0, sqlString.Length);

            sqlString.Append(" Insert into " + table_XX + "_roleskey (nzp_role,kod,tip)  ");
            sqlString.Append(" Select distinct r.nzp_role, r.kod, r.tip ");
            sqlString.Append(" From roleskey r, _temp u ");
            sqlString.Append(" Where r.nzp_role = u.nzp_role ");
            sqlString.Append(" and u.nzp_user =" + nzp_user);
            sqlString.Append(" and u.nzp_role <> " + Constants.roleRaschetNachisleniy);

#if PG
            sqlString.Append(" and r.tip ||CAST(r.kod as TEXT)||r.nzp_role ||'-'||r.nzp_rlsv ||'roles' = r.sign ");
#else
            sqlString.Append(" and r.tip ||r.kod ||r.nzp_role ||'-'||r.nzp_rlsv ||'roles' = decrypt_char(r.sign) ");
#endif
            if (!ExecSQL(connectionID, sqlString.ToString(), true).result)
            {
                connectionID.Close();
                return;
            }
            sqlString.Remove(0, sqlString.Length);

            //Заполнение tXX_role_actions
            sqlString.Append(" Create table " + table_XX + "_role_actions( ");
            sqlString.Append(" id serial  not null, ");
            sqlString.Append(" nzp_role integer, ");
            sqlString.Append(" nzp_act integer, ");
            sqlString.Append(" mod_act integer, ");
            sqlString.Append(" nzp_page integer ) ");

            if (!ExecSQL(connectionID, sqlString.ToString(), true).result)
            {
                connectionID.Close();
                return;
            }
            sqlString.Remove(0, sqlString.Length);

            sqlString.Append(" Insert into " + table_XX + "_role_actions (nzp_role,nzp_act,mod_act,nzp_page) ");
            sqlString.Append(" Select distinct r.nzp_role, r.nzp_act,r.mod_act,r.nzp_page ");
            sqlString.Append(" From role_actions r, _temp u  ");
            sqlString.Append(" Where r.nzp_role = u.nzp_role ");
#if PG
            sqlString.Append(" and r.nzp_role||CAST(r.nzp_page as TEXT)||r.nzp_act||'-'||r.id||'role_actions' = r.sign");
#else
            sqlString.Append(" and r.nzp_role||r.nzp_page||r.nzp_act||'-'||r.id||'role_actions' = decrypt_char(r.sign)");
#endif

            if (!ExecSQL(connectionID, sqlString.ToString(), true).result)
            {
                connectionID.Close();
                return;
            }
            sqlString.Remove(0, sqlString.Length);

            sqlString.Append(" Insert into " + table_XX + "_role_actions (nzp_role,nzp_act,mod_act,nzp_page) ");
            sqlString.Append(" Select rol.nzp_role,t.nzp_act,t.mod_act,t.nzp_page from " + table_XX + "_role_actions t , roleskey rol where  t.nzp_role=kod and rol.tip=" + Constants.role_sql_subrole);

            if (!ExecSQL(connectionID, sqlString.ToString(), true).result)
            {
                connectionID.Close();
                return;
            }
            sqlString.Remove(0, sqlString.Length);

            /*#if PG
                        sqlString.Append(" Delete from " + table_XX + "_role_actions where nzp_role not in (select distinct nzp_role from roleskey where tip =" + Constants.role_sql_subrole + " )");
            #else
                        sqlString.Append(" Delete from " + table_XX + "_role_actions where nzp_role not in (select unique nzp_role from roleskey where tip =" + Constants.role_sql_subrole + " )");
            #endif

                        if (!ExecSQL(connectionID, sqlString.ToString(), true).result)
                        {
                            connectionID.Close();
                            return;
                        }
                        sqlString.Remove(0, sqlString.Length);*/

#if PG
            if (!ExecSQL(connectionID, " Create index ix_t_role_actions_" + nzp_user + "_1 on " + table_XX + "_role_actions (nzp_role); ", true).result)
            {
                connectionID.Close();
                return;
            }
            if (!ExecSQL(connectionID, " Create index ix_t_role_actions_" + nzp_user + "_2 on " + table_XX + "_role_actions (nzp_act); ", true).result)
            {
                connectionID.Close();
                return;
            }
            if (!ExecSQL(connectionID, " Create index ix_t_role_actions_" + nzp_user + "_3 on " + table_XX + "_role_actions (mod_act); ", true).result)
            {
                connectionID.Close();
                return;
            }
            if (!ExecSQL(connectionID, " Create index ix_t_role_actions_" + nzp_user + "_4 on " + table_XX + "_role_actions (nzp_page); ", true).result)
            {
                connectionID.Close();
                return;
            }
#else
            if (!ExecSQL(connectionID, " Create index ix_t_role_actions_" + nzp_user + "_1 on " + table_XX + "_role_actions (nzp_role); ", true).result)
            {
                connectionID.Close();
                return;
            }
            if (!ExecSQL(connectionID, " Create index ix_t_role_actions_" + nzp_user + "_2 on " + table_XX + "_role_actions (nzp_act); ", true).result)
            {
                connectionID.Close();
                return;
            }
            if (!ExecSQL(connectionID, " Create index ix_t_role_actions_" + nzp_user + "_3 on " + table_XX + "_role_actions (mod_act); ", true).result)
            {
                connectionID.Close();
                return;
            }
            if (!ExecSQL(connectionID, " Create index ix_t_role_actions_" + nzp_user + "_4 on " + table_XX + "_role_actions (nzp_page); ", true).result)
            {
                connectionID.Close();
                return;
            }
#endif



#if PG
            if (!ExecSQL(connectionID, " analyze " + table_XX + "_role_actions; ", true).result)
#else
            if (!ExecSQL(connectionID, " Update statistics for table " + table_XX + "_role_actions; ", true).result)
#endif
            {
                connectionID.Close();
                return;
            }

            //Заполнение tXX_role_pages
#if PG
            sqlString.Append(" Create table " + table_XX + "_role_pages( ");
            sqlString.Append(" id serial  not null, ");
            sqlString.Append(" nzp_role integer, ");
            sqlString.Append(" nzp_page integer ) ");
#else
            sqlString.Append(" Create table " + table_XX + "_role_pages( ");
            sqlString.Append(" id serial  not null, ");
            sqlString.Append(" nzp_role integer, ");
            sqlString.Append(" nzp_page integer ) ");
#endif
            if (!ExecSQL(connectionID, sqlString.ToString(), true).result)
            {
                connectionID.Close();
                return;
            }
            sqlString.Remove(0, sqlString.Length);

            sqlString.Append(" Insert into " + table_XX + "_role_pages (nzp_role,nzp_page) ");
#if PG
            sqlString.Append(" Select distinct r.nzp_role , r.nzp_page ");
#else
            sqlString.Append(" Select unique r.nzp_role , r.nzp_page ");
#endif
            sqlString.Append(" From role_pages r, _temp u  ");
            sqlString.Append(" Where r.nzp_role = u.nzp_role  ");
#if PG
            sqlString.Append(" and r.nzp_role||CAST(r.nzp_page as TEXT)||'-'||r.id||'role_pages' = decrypt_char(r.sign)");
#else
            sqlString.Append(" and r.nzp_role||r.nzp_page||'-'||r.id||'role_pages' = decrypt_char(r.sign)");
#endif

            if (!ExecSQL(connectionID, sqlString.ToString(), true).result)
            {
                connectionID.Close();
                return;
            }
            sqlString.Remove(0, sqlString.Length);

#if PG
            sqlString.Append(" Insert into " + table_XX + "_role_pages (nzp_role,nzp_page) ");
            sqlString.Append(" Select rol.nzp_role,t.nzp_page from " + table_XX + "_role_pages t , roleskey rol where  t.nzp_role=kod and rol.tip=" + Constants.role_sql_subrole);
#else
  sqlString.Append(" Insert into " + table_XX + "_role_pages (nzp_role,nzp_page) ");
            sqlString.Append(" Select rol.nzp_role,t.nzp_page from " + table_XX + "_role_pages t , roleskey rol where  t.nzp_role=kod and rol.tip=" + Constants.role_sql_subrole);
#endif
            if (!ExecSQL(connectionID, sqlString.ToString(), true).result)
            {
                connectionID.Close();
                return;
            }
            sqlString.Remove(0, sqlString.Length);

            /*#if PG
                        sqlString.Append(" Delete from " + table_XX + "_role_pages where nzp_role not in (select distinct nzp_role from roleskey where tip =" + Constants.role_sql_subrole + " )");
            #else
                        sqlString.Append(" Delete from " + table_XX + "_role_pages where nzp_role not in (select unique nzp_role from roleskey where tip =" + Constants.role_sql_subrole + " )");
            #endif

                        if (!ExecSQL(connectionID, sqlString.ToString(), true).result)
                        {
                            connectionID.Close();
                            return;
                        }
                        sqlString.Remove(0, sqlString.Length);*/

            if (!ExecSQL(connectionID, " Create index ix_t_role_pages_" + nzp_user + "_1 on " + table_XX + "_role_pages (nzp_page); ", true).result)
            {
                connectionID.Close();
                return;
            }
            if (!ExecSQL(connectionID, " Create index ix_t_role_pages_" + nzp_user + "_2 on " + table_XX + "_role_pages (nzp_role); ", true).result)
            {
                connectionID.Close();
                return;
            }
#if PG
            if (!ExecSQL(connectionID, " analyze " + table_XX + "_role_pages; ", true).result)
#else
            if (!ExecSQL(connectionID, " Update statistics for table " + table_XX + "_role_pages; ", true).result)
#endif
            {
                connectionID.Close();
                return;
            }



            //Заполнение tXX_roles_arms
#if PG
            sqlString.Append(" Create table " + table_XX + "_roles_arms( ");
            sqlString.Append(" id serial not null, ");
            sqlString.Append(" nzp_role integer, ");
            sqlString.Append(" page_url integer, ");
            sqlString.Append(" role char(120), ");
            sqlString.Append(" sort integer, ");
            sqlString.Append(" img_url char(255) ) ");
#else
   sqlString.Append(" Create table " + table_XX + "_roles_arms( ");
            sqlString.Append(" id serial not null, ");
            sqlString.Append(" nzp_role integer, ");
            sqlString.Append(" page_url integer, ");
            sqlString.Append(" role char(120), ");
            sqlString.Append(" sort integer, ");
            sqlString.Append(" img_url char(255) ) ");
#endif
            if (!ExecSQL(connectionID, sqlString.ToString(), true).result)
            {
                connectionID.Close();
                return;
            }
            sqlString.Remove(0, sqlString.Length);

            sqlString.Append(" Insert into " + table_XX + "_roles_arms (nzp_role,page_url,role,sort,img_url)  ");
#if PG
            sqlString.Append(" Select distinct s.nzp_role, s.page_url, decrypt_char(s.role) as role, s.sort, i.img_url ");
            sqlString.Append(" From s_roles s left outer join img_lnk i on i.tip = 3 and i.kod = s.nzp_role and i.cur_page = 0, role_pages r, _temp u ");
#else
            sqlString.Append(" Select unique s.nzp_role, s.page_url, decrypt_char(s.role) as role, s.sort, i.img_url ");
            sqlString.Append(" From s_roles s, " + table_XX + "_role_pages r, _temp u, outer img_lnk i ");
#endif
#if PG
            sqlString.Append(" Where s.nzp_role = r.nzp_role ");
            sqlString.Append(" and ( s.nzp_role = u.nzp_role ");
            sqlString.Append(" or u.nzp_role in (select kod from " + table_XX + "_roleskey rol where rol.nzp_role = r.nzp_role and  rol.tip =" + Constants.role_sql_subrole + "))");
            sqlString.Append(" and u.nzp_user =" + nzp_user);
            sqlString.Append(" and s.page_url > 0 ");
            sqlString.Append(" and s.page_url = r.nzp_page ");
            sqlString.Append(" and s.is_active = 1 ");
#else
  sqlString.Append(" Where s.nzp_role = r.nzp_role ");
            sqlString.Append(" and ( s.nzp_role = u.nzp_role ");
            sqlString.Append(" or u.nzp_role in (select kod from " + table_XX + "_roleskey rol where rol.nzp_role = r.nzp_role and  rol.tip =" + Constants.role_sql_subrole + "))");
            sqlString.Append(" and u.nzp_user =" + nzp_user);
            sqlString.Append(" and s.page_url > 0 ");
            sqlString.Append(" and s.page_url = r.nzp_page ");
            sqlString.Append(" and s.is_active = 1 ");
#endif
#if !PG
            sqlString.Append(" and i.tip = 3 and i.kod = s.nzp_role and i.cur_page = 0 ");
#endif
            if (!ExecSQL(connectionID, sqlString.ToString(), true).result)
            {
                connectionID.Close();
                return;
            }
            sqlString.Remove(0, sqlString.Length);
            ExecSQL(connectionID, " drop table _temp", false);
            connectionID.Close();
        }


        public void GetRoles(int nzp_role, int nzp_user, int cur_page, ref int mod, ref List<_RoleActions> RoleActions, ref List<_RolePages> RolePages, string idses) //загрузить права доступа
        {
            IDbConnection connectionID = GetConnection(Constants.cons_Webdata);
            if (!OpenDb(connectionID, true).result) return;

            //надо отметить, что данная сессия еще жива
            if (idses.Trim() != "")
            {
#if PG
                if (!ExecSQL(connectionID,
                      " Update log_sessions Set dat_log = now() " +
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
                  " Delete From log_sessions Where (now() - INTERVAL '" + Constants.users_min + " minutes') > dat_log and session_id is null"
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
                ret = ExecRead(connectionID, out reader, "Select nzp_user From log_sessions Where nzp_user = " + nzp_user + " and idses = " + Utils.EStrNull(idses), true);
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


        //----------------------------------------------------------------------
        public void GetRolesVal(int nzp_role, int nzp_user, ref List<_RolesVal> RolesVal) //загрузить права доступа
        //----------------------------------------------------------------------
        {
            IDbConnection connectionID = GetConnection(Constants.cons_Webdata);
            if (!OpenDb(connectionID, true).result) return;

            string table_XX = "t" + nzp_user;
            //ROLESVAL
            IDataReader reader;
            if (nzp_role > 0)
            {

#if PG
                string sql = " Select distinct nzp_role, tip, kod " +
                                             " From " + table_XX + "_roleskey Where " +
                                             " nzp_role=" + nzp_role + " or nzp_role in (select kod from " + table_XX + "_roleskey r where r.nzp_role = "
                                             + nzp_role + " and  r.tip =" + Constants.role_sql_subrole + " ) or nzp_role >= 1000 ";

#else
                string sql = " Select unique nzp_role, tip, kod " +
                             " From " + table_XX + "_roleskey Where " +
                             " nzp_role=" + nzp_role + " or nzp_role in (select kod from " + table_XX + "_roleskey r where r.nzp_role = "
                             + nzp_role + " and  r.tip =" + Constants.role_sql_subrole + " ) or nzp_role >= 1000 ";

#endif
                if (!ExecRead(connectionID, out reader, sql, true).result)
                {
                    ExecSQL(connectionID, "drop table t_user_roles", false);
                    connectionID.Close();
                    return;
                }
                try
                {
                    string val = "";
                    int ptab = -99;
                    int tab = -99;
                    int nzp = -99;

                    while (reader.Read())
                    {
                        tab = (int)reader["tip"];
                        nzp = (int)reader["kod"];

                        if (ptab != tab)
                        {
                            if (ptab > 0)
                            {
                                _RolesVal zap = new _RolesVal();
                                zap.tip = Constants.role_sql;
                                zap.kod = ptab;
                                zap.val = val.Remove(0, 1);
                                zap.nzp_role = 0;

                                RolesVal.Add(zap);

                                val = "," + nzp.ToString();
                                ptab = tab;
                            }
                            else
                            {
                                val += "," + nzp.ToString();
                                ptab = tab;
                            }
                        }
                        else
                        {
                            val += "," + nzp.ToString();
                        }
                    }

                    if (val != "")
                    {
                        _RolesVal zap = new _RolesVal();
                        zap.tip = Constants.role_sql;
                        zap.kod = tab;
                        zap.val = val.Remove(0, 1);
                        zap.nzp_role = 0;

                        RolesVal.Add(zap);
                    }

                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка заполнения roleskey " + (Constants.Viewerror ? ex.Message : ""), MonitorLog.typelog.Error, 20, 101, true);
                }

                ExecSQL(connectionID, "drop table t_user_roles", false);

                reader.Close();
            }
            connectionID.Close();
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
