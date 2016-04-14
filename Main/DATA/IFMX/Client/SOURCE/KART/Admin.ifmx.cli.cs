using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Utility;
using System.IO;
namespace STCLINE.KP50.DataBase
{
    public class DbWorkUserClient : DataBaseHead
    {
        public void SaveWebUser(List<User> users, out Returns ret)
        {
            ret = Utils.InitReturns();

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return;

#if PG
            ret = ExecSQL(conn_web,
                              " set search_path to 'public'"
                              , true);
#else
            ret = ExecSQL(conn_web,
                  " set encryption password '" + BasePwd + "'"
                  , true);
#endif
            if (!ret.result)
            {
                conn_web.Close();
                return;
            }

            foreach (User user in users)
            {
#if PG
                ret = ExecSQL(conn_web,
                    " Update users " +
                    " Set login = '" + user.login + "'" +
                       ", email = '" + user.email + "'" +
                       ", uname = '" + user.uname + "'" +
                       ", pwd   = " +  Utils.EStrNull(Utils.CreateMD5StringHash(user.pwd + user.nzpuser + BasePwd)) +
                    " WHERE nzp_user = " + user.nzp_user
                    , true);
#else
                ret = ExecSQL(conn_web,
                    " Update users " +
                    " Set login = '" + user.login + "'" +
                       ", email = encrypt_aes('" + user.email + "')" +
                       ", uname = encrypt_aes('" + user.uname + "')" +
                       ", pwd   = encrypt_aes(nzp_user||'-'||'" + user.pwd + "') " +
                    " Where nzp_user = " + user.nzp_user
                    , true);
#endif
                if (!ret.result)
                {
                    conn_web.Close();
                    return;
                }
            }

            conn_web.Close();
        }
    }

    public partial class DbAdminClient : DbUserClient
    {
        private string tableUsersRecovery
        {
            get { return "users_recovery"; }
        }

        /// <summary>
        /// получить список пользователей
        /// </summary>
        /// <param name="finder">объект с параметрами поиска</param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public ReturnsObjectType<List<User>> GetUsers(User finder)
        {
            ReturnsObjectType<List<User>> result;

            if (finder.nzp_user < 1 && finder.sessionId.Trim() == "")
                return new ReturnsObjectType<List<User>>(false, "Не определен пользователь");

            result = new ReturnsObjectType<List<User>>();

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            Returns ret = OpenDb(conn_web, true);
            if (!ret.result)
            {
                result.result = ret.result;
                result.text = ret.text;
                return result;
            }

#if PG
            ret = ExecSQL(conn_web,
                              " set search_path to 'public'"
                              , true);
#else
            ret = ExecSQL(conn_web,
                " set encryption password '" + BasePwd + "'"
                , true);
#endif
            if (!ret.result)
            {
                conn_web.Close();
                result.result = ret.result;
                result.text = ret.text;
                return result;
            }

            IDataReader reader;

            if (finder.sessionId.Trim() != "")
            {
                ret = ExecRead(conn_web, out reader,
                    "select nzp_user from log_sessions where session_id = " + Utils.EStrNull(finder.sessionId.Trim()),
                    true);
                if (!ret.result)
                {
                    conn_web.Close();
                    result.result = ret.result;
                    result.text = ret.text;
                    result.tag = ret.tag;
                    return result;
                }
                if (reader.Read())
                {
                    if (reader["nzp_user"] != null) finder.nzpuser = Convert.ToInt32(reader["nzp_user"]);
                }

                reader.Close();
                reader.Dispose();

                if (finder.nzpuser < 1)
                {
                    conn_web.Close();
                    result.result = true;
                    result.text = "Пользователь не найден";
                    result.tag = -1;
                    return result;
                }
            }

            string where = " Where 1=1";
            if (finder.nzpuser > 0)
                where += " and u.nzp_user = " + finder.nzpuser.ToString();

            if (finder.isOnline > 0)
                where += " and u.nzp_user in ( Select nzp_user From log_sessions)";
            else if (finder.isOnline < 0)
                where += " and u.nzp_user not in (select nzp_user from log_sessions)";

            if (finder.is_blocked == 1)
#if PG
                where += " and CAST(u.is_blocked as int) = 1";
#else
                where += " and u.is_blocked = 1";
#endif
            if (finder.is_remote == 1)
#if PG
                where += " and CAST(u.is_remote as int) = 1";
#else
                where += " and u.is_remote = 1";
#endif
            if (finder.uname != "") where += " and upper(decrypt_char(u.uname)) like '%" + finder.uname.ToUpper() + "%'";
            if (finder.login != "") where += " and upper(u.login) like '%" + finder.login.ToUpper() + "%'";

            int total_record_count = 0;
            object count = ExecScalar(conn_web, " Select count(*) From users u" + where, out ret, true);
            if (ret.result)
            {
                try
                {
                    total_record_count = Convert.ToInt32(count);
                }
                catch (Exception ex)
                {
                    conn_web.Close();
                    result.result = false;
                    result.text = ex.Message;
                    return result;
                }
            }

#if PG
            string skip = "";
            if (finder.skip > 0) skip = " offset " + finder.skip.ToString();
            string sql =
                " Select " +
                    " nzp_user, login, pwd, uname, appointment, dat_log, ip_log, " +
                    " browser, email, is_blocked, is_remote, date_begin, nzp_payer, nzp_bank " +
                    ", ( Select count(*) From log_sessions s Where s.nzp_user = u.nzp_user) as isOnline " +
                 " From users u " + where;
#else
            string skip = "";
            if (finder.skip > 0) skip = " skip " + finder.skip.ToString();
            string sql =
                " Select " + skip +
                " nzp_user, login, decrypt_char(pwd) as pwd, decrypt_char(uname) as uname, decrypt_char(appointment) as appointment, dat_log, ip_log, " +
                " browser, decrypt_char(email) as email, is_blocked, is_remote, date_begin, nzp_payer, nzp_bank " +
                ", ( Select count(*) From log_sessions s Where s.nzp_user = u.nzp_user) as isOnline " +
                " From users u " + where;
#endif

            string desc = "";
            if (finder.sortby < 0) desc = " desc ";
            if (Math.Abs(finder.sortby) == Constants.sortby_nzp_user)
                sql += " Order by nzp_user " + desc;
            else if (Math.Abs(finder.sortby) == Constants.sortby_login)
                sql += " Order by login " + desc + ", uname";
            else if (Math.Abs(finder.sortby) == Constants.sortby_username)
                sql += " Order by uname " + desc + ", login";
            else if (Math.Abs(finder.sortby) == Constants.sortby_email)
                sql += " Order by email " + desc + ", login";
            else
                sql += " Order by dat_log desc, login, uname";

#if PG
            sql += skip;
#endif

            ret = ExecRead(conn_web, out reader, sql, true);
            if (!ret.result)
            {
                conn_web.Close();
                result.result = ret.result;
                result.text = ret.text;
                return result;
            }

            bool isMainBankConnected = false;
            IDbConnection conn_db = null;
            string connectionString = Points.GetConnByPref(Points.Pref);
            if (connectionString != "" && connectionString != null)
            {
                conn_db = GetConnection(connectionString);
                Returns ret2 = OpenDb(conn_db, true);
                isMainBankConnected = ret2.result;
            }

            List<User> Spis = new List<User>();

            try
            {
                int i = 0;
                while (reader.Read())
                {
                    User zap = new User();

                    zap.num = ++i + finder.skip;

                    if (reader["nzp_user"] != DBNull.Value) zap.nzpuser = Convert.ToInt32(reader["nzp_user"]);
                    if (reader["login"] != DBNull.Value) zap.login = Convert.ToString(reader["login"]).Trim();
                    if (reader["pwd"] != DBNull.Value) zap.pwd = Convert.ToString(reader["pwd"]).Trim();
                    if (reader["uname"] != DBNull.Value) zap.uname = Convert.ToString(reader["uname"]).Trim();
                    if (reader["email"] != DBNull.Value) zap.email = Convert.ToString(reader["email"]).Trim();
                    if (reader["appointment"] != DBNull.Value)
                        zap.appointment = Convert.ToString(reader["appointment"]).Trim();
                    if (reader["dat_log"] != DBNull.Value)
                        zap.dat_log = String.Format("{0:dd.MM.yyyy HH:mm:ss}", reader["dat_log"]);
                    if (reader["ip_log"] != DBNull.Value) zap.ip_log = Convert.ToString(reader["ip_log"]);
                    if (reader["browser"] != DBNull.Value) zap.browser = Convert.ToString(reader["browser"]).Trim();

                    if (reader["is_blocked"] != DBNull.Value) zap.is_blocked = Convert.ToByte(reader["is_blocked"]);
                    if (reader["is_remote"] != DBNull.Value) zap.is_remote = Convert.ToByte(reader["is_remote"]);

                    if (reader["date_begin"] != DBNull.Value)
                        zap.date_begin = String.Format("{0:dd.MM.yyyy}", reader["date_begin"]);
                    if (reader["isOnline"] != DBNull.Value) zap.isOnline = Convert.ToInt32(reader["isOnline"]);
                    if (reader["nzp_payer"] != DBNull.Value) zap.nzp_payer = Convert.ToInt32(reader["nzp_payer"]);
                    if (reader["nzp_bank"] != DBNull.Value) zap.nzp_bank = Convert.ToInt32(reader["nzp_bank"]);
                    if (finder.roleFinder != null)
                    {
                        finder.roleFinder.nzpuser = zap.nzpuser;
                        finder.roleFinder.nzp_user = finder.nzp_user;
                        finder.roleFinder.rows = 1000;
                        zap.roles = GetRoles(finder.roleFinder, out ret, conn_web);
                        if (ret.result) zap.rolesNumber = ret.tag;
                        else break;
                    }

                    if (finder.accessFinder != null)
                    {
                        finder.accessFinder.nzpuser = zap.nzpuser;
                        finder.accessFinder.nzp_user = finder.nzp_user;
                        zap.access = GetUserAccess(finder.accessFinder, out ret, conn_web);
                        if (ret.result) zap.accessNumber = ret.tag;
                        else break;
                    }

                    if (zap.nzp_bank > 0 && isMainBankConnected)
                    {
#if PG
                        sql = "select bank from " + Points.Pref + "_kernel.s_bank where nzp_bank = " + zap.nzp_bank;
#else
                        sql = "select bank from " + Points.Pref + "_kernel:s_bank where nzp_bank = " + zap.nzp_bank;
#endif
                        IDataReader reader2;
                        ret = ExecRead(conn_db, out reader2, sql, true);

                        if (result.result)
                        {
                            if (reader2.Read())
                                if (reader2["bank"] != DBNull.Value) zap.bank = Convert.ToString(reader2["bank"]);
                            reader.Close();
                        }
                    }

                    Spis.Add(zap);

                    if (i >= finder.rows) break;
                }

                result.tag = total_record_count;
                result.returnsData = Spis;
            }
            catch (Exception ex)
            {
                result.result = false;
                result.text = ex.Message;

                MonitorLog.WriteLog("Ошибка заполнения списка пользователей:\n" + ex.Message, MonitorLog.typelog.Error,
                    20, 201, true);
            }
            reader.Close();
            conn_web.Close();
            if (isMainBankConnected) conn_db.Close();
            return result;
        } //GetUser

        public enum UserBy
        {
            Login = 0,
            NZP_User = 1
        }

        /// <summary>
        /// Выбирает информацию о пользователе по логину.
        /// </summary>
        /// <param name="user">Заполняет и возвращает объект типа User. В качетве входного параметра использует поле "User.login".</param>
        /// <returns></returns>
        public Returns GetUserBy(ref User user, UserBy userBy)
        {
            Returns ret = Utils.InitReturns();
            string strSQLWhere = "";

            switch (userBy)
            {
                case UserBy.Login:
                    if (string.IsNullOrEmpty(user.login.Trim()))
                    {
                        ret.result = false;
                        ret.text = "Логин не может быть пустым.";
                        ret.tag = -1;
                        return ret;
                    }
                    strSQLWhere = "Where login = '" + user.login + "'";
                    break;
                case UserBy.NZP_User:
                    if (user.nzp_user < 1)
                    {
                        ret.result = false;
                        ret.text = "ID пользователя не может быть меньше ноля.";
                        ret.tag = -1;
                        return ret;
                    }
                    strSQLWhere = "Where nzp_user = " + user.nzp_user;
                    break;
            }



#if PG
            string strSQLQuery = " Select " +
                    " nzp_user, login, decrypt_char(pwd) as pwd, decrypt_char(uname) as uname, decrypt_char(appointment) as appointment, dat_log, ip_log, " +
                    " browser, decrypt_char(email) as email, is_blocked, is_remote, date_begin, nzp_payer, nzp_bank " +
                    ", (select count(*) From log_sessions s Where s.nzp_user = u.nzp_user) as isOnline " +
                 " From users u " + strSQLWhere + ";";
#else
            string strSQLQuery =
                " Select " +
                " nzp_user, login, decrypt_char(pwd) as pwd, decrypt_char(uname) as uname, decrypt_char(appointment) as appointment, dat_log, ip_log, " +
                " browser, decrypt_char(email) as email, is_blocked, is_remote, date_begin, nzp_payer, nzp_bank " +
                ", ( Select count(*) From log_sessions s Where s.nzp_user = u.nzp_user) as isOnline " +
                " From users u " + strSQLWhere + ";";
#endif

            IDbConnection conn_db = null;
            string connectionString = Constants.cons_Webdata;
            if (!string.IsNullOrEmpty(connectionString))
            {
                conn_db = GetConnection(connectionString);
                ret = OpenDb(conn_db, true);
            }
            else
            {
                ret.text = "Не определена строка подключения.";
                ret.result = false;
            }
            if (!ret.result) return ret;

            try
            {
                IDataReader reader = null;
#if !PG
                ExecSQL(conn_db, "set encryption password '" + BasePwd + "'", true);
#endif
                ret = ExecRead(conn_db, out reader, strSQLQuery, true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog(String.Format("Ошибка в функции GetUserByLogin:\n{0}", ret.text),
                        MonitorLog.typelog.Error, true);
                    throw new Exception("Не удалось выполнить запрос.");
                }

                int nzp_user = 0;
                string login = "";
                string pwd = "";
                string uname = "";
                string appointment = "";
                string dat_log = "";
                string ip_log = "";
                string browser = "";
                string email = "";
                byte is_blocked = 0;
                byte is_remote = 0;
                DateTime date_begin = new DateTime();
                int nzp_payer = 0;
                int nzp_bank = 0;
                int isOnline = 0;
                ret = Utils.InitReturns();

                while (reader.Read())
                {
                    if (reader["nzp_user"] != DBNull.Value)
                        if (!Int32.TryParse(reader["nzp_user"].ToString(), out nzp_user)) ret.result = false;
                    if (reader["login"] != DBNull.Value) login = reader["login"].ToString();
                    if (reader["pwd"] != DBNull.Value) pwd = reader["pwd"].ToString();
                    if (reader["uname"] != DBNull.Value) uname = reader["uname"].ToString();
                    if (reader["appointment"] != DBNull.Value) appointment = reader["appointment"].ToString();
                    if (reader["dat_log"] != DBNull.Value) dat_log = reader["dat_log"].ToString();
                    if (reader["ip_log"] != DBNull.Value) ip_log = reader["ip_log"].ToString();
                    if (reader["browser"] != DBNull.Value) browser = reader["browser"].ToString();
                    if (reader["email"] != DBNull.Value) email = reader["email"].ToString();
                    if (reader["is_blocked"] != DBNull.Value)
                        if (!Byte.TryParse(reader["is_blocked"].ToString(), out is_blocked)) ret.result = false;
                    if (reader["is_remote"] != DBNull.Value)
                        if (!Byte.TryParse(reader["is_remote"].ToString(), out is_remote)) ret.result = false;
                    if (reader["date_begin"] != DBNull.Value)
                        if (!DateTime.TryParse(reader["date_begin"].ToString(), out date_begin)) ret.result = false;
                    if (reader["nzp_payer"] != DBNull.Value)
                        if (!Int32.TryParse(reader["nzp_payer"].ToString(), out nzp_payer)) ret.result = false;
                    if (reader["nzp_bank"] != DBNull.Value)
                        if (!Int32.TryParse(reader["nzp_bank"].ToString(), out nzp_bank)) ret.result = false;
                    if (reader["isOnline"] != DBNull.Value)
                        if (!Int32.TryParse(reader["isOnline"].ToString(), out isOnline)) ret.result = false;
                }

                user.nzp_user = nzp_user;
                user.login = login;
                user.pwd = pwd;
                user.uname = uname;
                user.appointment = appointment;
                user.dat_log = dat_log;
                user.ip_log = ip_log;
                user.browser = browser;
                user.email = email;
                user.is_blocked = is_blocked;
                user.is_remote = is_remote;
                user.date_begin = date_begin.ToShortDateString();
                user.nzp_payer = nzp_payer;
                user.nzp_bank = nzp_bank;
                user.isOnline = isOnline;

                if (!ret.result) throw new Exception("Не удалось распарсить одно из полей.");
                if (user.isOnline < 1) throw new Exception("Нет активных сессий пользователя.");
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                ret.tag = -1;
                MonitorLog.WriteLog(String.Format("Ошибка в функции GetUserByLogin:\n{0}", ex.Message),
                    MonitorLog.typelog.Error, true);
            }
            finally
            {
                conn_db.Close();
            }

            return ret;
        }

        public List<Role> GetUserRoles(int nzp_user)
        {
            Returns ret = Utils.InitReturns();
            Role finder = new Role();
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            finder.nzp_user = nzp_user;
            return GetRoles(finder, out ret, conn_web, false);

            /*
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            List<Role> lstRoles = new List<Role>();
            Returns ret = Utils.InitReturns();
            try
            {
#if PG
                ret = ExecSQL(conn_web,
                      " set search_path to 'public'"
                      , true);
#else
            ret = ExecSQL(conn_web,
                  " set encryption password '" + BasePwd + "'"
                  , true);
#endif
                if (!ret.result) return null;

#if PG
                string strSQLWhere;
                if (nzp_user > 0)
                    strSQLWhere = "u.nzp_role = r.nzp_role and u.nzp_user = " + nzp_user;
                else
                    throw new Exception("Неверные входные параметры: NZP_User не может быть меньше 1.");

                string strSQLQuery = " Select " +
       " case when (select count(*) from role_pages rr where rr.nzp_page = 0 and rr.nzp_role = r.nzp_role and rr.sign = CAST(nzp_role as text)||CAST(nzp_page as text)||'-'||CAST(id as text)||'role_pages') > 0 " +
       " then 1 else case when r.nzp_role between 900 and 999 then 2 else 3 end end as role_type, r.nzp_role, r.role as role, r.page_url, p.page_name as page_name, r.sort, r.is_active " +
       " From s_roles r left outer join pages p on r.page_url = p.nzp_page , userp u " + strSQLWhere + " Order by 1, 3;";

#else
               string strSQLWhere;
               if (nzp_user > 0)
                   strSQLWhere = "u.nzp_role = r.nzp_role and u.nzp_user = " + nzp_user;
               else
                   throw new Exception("Неверные входные параметры: NZP_User не может быть меньше 1.");

                string strSQLQuery = " Select " + 
                 " case when (select count(*) from role_pages rr where rr.nzp_page = 0 and rr.nzp_role = r.nzp_role and decrypt_char(rr.sign) = nzp_role||nzp_page||'-'||id||'role_pages') > 0 then 1 else case when r.nzp_role between 900 and 999 then 2 else 3 end end as role_type, r.nzp_role, decrypt_char(r.role) as role, r.page_url, decrypt_char(p.page_name) as page_name, r.sort, r.is_active " +
                 " From s_roles r, outer pages p , userp u " + strSQLWhere + " Order by 1, 3";
#endif
                IDataReader reader;
                ret = ExecRead(conn_web, out reader, strSQLQuery, true);

                while (reader.Read())
                {
                    Role role = new Role();

                    int 

                    if (reader["nzp_role"] != DBNull.Value) zap.nzp_role = Convert.ToInt32(reader["nzp_role"]);
                    if (reader["role"] != DBNull.Value) zap.role = Convert.ToString(reader["role"]).Trim();
                    if (reader["page_url"] != DBNull.Value) zap.page_url = Convert.ToInt32(reader["page_url"]);
                    if (reader["page_name"] != DBNull.Value) zap.page_name = Convert.ToString(reader["page_name"]).Trim();
                    if (reader["sort"] != DBNull.Value) zap.sort = Convert.ToInt32(reader["sort"]);
                    if (reader["is_active"] != DBNull.Value) zap.is_active = Convert.ToInt32(reader["is_active"]);
                }

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog(String.Format("Ошибка в функции GetUserRoles:\n{0}", ex.Message), MonitorLog.typelog.Error, true);
                lstRoles = null;
            }
            finally
            {
                conn_web.Close();
            }
            return lstRoles;
            */
        }

        /// <summary>
        /// сохранение информации о пользователе
        /// </summary>
        /// <param name="user"></param>
        /// <returns>result = true - успех, тогда в поле tag - nzp_user пользователя,
        ///                 = false - неудача, тогда если tag меньше 0, то в текст сообщение для вывода пользователю
        ///                                          если tag = 0, то ошибка пользователю не выводится       </returns>
        public Returns SaveUser(User user)
        {
            Returns ret = Utils.InitReturns();

            if (user.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return ret;
            }

            if (user.login == "")
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Не задан логин";
                return ret;
            }

            if (user.uname == "")
            {
                ret.result = false;
                ret.tag = -2;
                ret.text = "Не задано имя пользователя";
                return ret;
            }

            if (user.nzpuser <= 0 && user.pwd == "")
            {
                ret.result = false;
                ret.tag = -2;
                ret.text = "Не задан пароль";
                return ret;
            }

            DateTime dateBegin = DateTime.MinValue;
            if (user.date_begin != "" && !DateTime.TryParse(user.date_begin, out dateBegin))
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Неверно задана дата начала работы";
                return ret;
            }

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return ret;

#if PG
            ret = ExecSQL(conn_web,
                  " set search_path to 'public'"
                  , true);
#else
            ret = ExecSQL(conn_web,
                " set encryption password '" + BasePwd + "'"
                , true);
#endif
            if (!ret.result)
            {
                conn_web.Close();
                return ret;
            }

            string sql;

            // проверка уникальности логина
            IDataReader reader;
#if PG
            ret = ExecRead(conn_web, out reader, "select nzp_user from users where login = '" + user.login + "' and nzp_user <> " + user.nzpuser.ToString() + " offset 1", true);
#else
            ret = ExecRead(conn_web, out reader,
                "select first 1 nzp_user from users where login = '" + user.login + "' and nzp_user <> " +
                user.nzpuser.ToString(), true);
#endif
            if (ret.result)
            {
                if (reader.Read())
                {
                    ret.result = false;
                    ret.tag = -1;
                    ret.text = "Пользователь с таким логином уже существует";
                    reader.Close();
                    conn_web.Close();
                    return ret;
                }
                reader.Close();
            }
            else
            {
                conn_web.Close();
                return ret;
            }

            // проверка уникальности email
            if (user.email.Trim() != "")
            {
#if PG
                sql = "select nzp_user from users where trim(upper(email)) = '" + user.email.ToUpper() + "' and nzp_user <> " + user.nzpuser.ToString() + " offset 1 ";
#else
                sql = "select first 1 nzp_user from users where trim(upper(decrypt_char(email))) = '" +
                      user.email.ToUpper() + "' and nzp_user <> " + user.nzpuser.ToString();
#endif
                ret = ExecRead(conn_web, out reader, sql, true);
                if (ret.result)
                {
                    if (reader.Read())
                    {
                        ret.result = false;
                        ret.tag = -2;
                        ret.text = "Заданный email уже существует";
                        reader.Close();
                        conn_web.Close();
                        return ret;
                    }
                    reader.Close();
                }
                else
                {
                    conn_web.Close();
                    return ret;
                }
            }

            if (user.nzpuser > 0)
            {
#if PG
                sql = "update users set login = '" + user.login + "'" +
                    ", email = '" + user.email + "'" +
                    ", uname = '" + user.uname + "'" +
                    ", appointment = '" + user.appointment + "'" +
#else
                sql = "update users set login = '" + user.login + "'" +
                      ", email = encrypt_aes('" + user.email + "')" +
                      ", uname = encrypt_aes('" + user.uname + "')" +
                      ", appointment = encrypt_aes('" + user.appointment + "')" +
#endif

 ", is_blocked = " + (user.is_blocked == 1 ? "1" : "0") +
                      ", nzp_payer = " + (user.nzp_payer > 0 ? user.nzp_payer.ToString() : "null") +
                      ", is_remote = " + (user.is_remote == 1 ? "1" : "0") +
                      ", nzp_bank = " + (user.nzp_bank > 0 ? user.nzp_bank.ToString() : "null") +
                      ", date_begin = " +
                      (dateBegin != DateTime.MinValue ? Utils.EStrNull(dateBegin.ToShortDateString()) : "null");

#if PG
                if (user.pwd.Trim().Length > 0) sql += ", pwd = " + Utils.EStrNull(Utils.CreateMD5StringHash(user.pwd + user.nzpuser + BasePwd)); 
#else
                if (user.pwd.Trim().Length > 0) sql += ", pwd = encrypt_aes(nzp_user||'-'||'" + user.pwd + "')";
#endif

                sql += " Where nzp_user = " + user.nzpuser;
                ret = ExecSQL(conn_web, sql, true);
                if (ret.result) ret.tag = user.nzpuser;
            }
            else
            {
                IDbTransaction transaction;
                try
                {
                    transaction = conn_web.BeginTransaction();
                }
                catch
                {
                    transaction = null;
                }
                sql =
                    "insert into users (login, pwd, uname, email, is_blocked, is_remote, date_begin, nzp_payer, appointment, nzp_bank) " +
                    " values (" + Utils.EStrNull(user.login) +
#if PG
                    ", ' '" + 
#else
 ", " + Utils.EStrNull(user.pwd) +
#endif
 ", " + Utils.EStrNull(user.uname) +
                    ", " + Utils.EStrNull(user.email) +

                    ", " + (user.is_blocked == 1 ? "1" : "0") +
                    ", " + (user.is_remote == 1 ? "1" : "0") +

                    ", " + (dateBegin != DateTime.MinValue ? Utils.EStrNull(dateBegin.ToShortDateString()) : "null") +
                    ", " + (user.nzp_payer > 0 ? user.nzp_payer.ToString() : "null") +
                    ", " + Utils.EStrNull(user.appointment) +
                    ", " + (user.nzp_bank > 0 ? user.nzp_bank.ToString() : "null") +
#if PG
 ") returning nzp_user";
                int nzp = 0;
                try { nzp = Convert.ToInt32(ExecScalar(conn_web, transaction, sql, out ret, true)); }
                catch
                {
                    if (transaction != null) transaction.Rollback();
                    ret.result = false;
                    conn_web.Close();
                    return ret;
                }
#else
 ")";
                ret = ExecSQL(conn_web, transaction, sql, true);
#endif
                if (ret.result)
                {
#if PG
#else
                    int nzp = GetSerialValue(conn_web, transaction);
#endif
                    if (nzp > 0)
                    {
#if PG
                        sql = "update users set pwd = " + Utils.EStrNull(Utils.CreateMD5StringHash(user.pwd + nzp + BasePwd)) + ", uname = uname, is_new_pwd = 1, email = email, appointment = appointment where nzp_user = " + nzp;
#else
                        sql =
                            "update users set pwd = encrypt_aes(nzp_user||'-'||pwd), uname = encrypt_aes(uname), email = encrypt_aes(email), appointment = encrypt_aes(appointment) where nzp_user = " +
                            nzp;
#endif
                        ret = ExecSQL(conn_web, transaction, sql, true);
                        if (ret.result)
                        {
                            if (transaction != null) transaction.Commit();
                            ret.tag = nzp;
                        }
                        else
                        {
                            if (transaction != null) transaction.Rollback();
                            ret.result = false;
                        }
                    }
                    else
                    {
                        if (transaction != null) transaction.Rollback();
                        ret.result = false;
                    }
                }
                else if (transaction != null) transaction.Rollback();
            }
            conn_web.Close();
            return ret;
        }

        /// <summary>
        /// выкинуть пользователя из сессии
        /// </summary>
        /// <param name="user"></param>
        /// <returns>result = true - успех, тогда в поле tag - nzp_user пользователя,
        ///                 = false - неудача, тогда если tag меньше 0, то в текст сообщение для вывода пользователю
        ///                                          если tag = 0, то ошибка пользователю не выводится       </returns>
        public Returns LogOutUser(User finder)
        {
            Returns ret = Utils.InitReturns();
            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Не определен пользователь, работающий в системе";
                return ret;
            }

            if (finder.nzpuser <= 0)
            {
                ret.result = false;
                ret.tag = -2;
                ret.text = "Не определен пользователь";
                return ret;
            }

            string sql = "select nzp_ses, idses from log_sessions where nzp_user = " + finder.nzpuser.ToString();

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return ret;

            IDataReader reader;
            ret = ExecRead(conn_web, out reader, sql, true);
            if (!ret.result)
            {
                conn_web.Close();
                return ret;
            }
            try
            {
                string idses = "";
                string err = "";
                while (reader.Read())
                {
                    if (reader["idses"] != DBNull.Value)
                    {
                        idses = Convert.ToString(reader["idses"]).Trim();
                        DbUserClient db = new DbUserClient();
                        if (!db.CloseSeans(finder.nzpuser, idses)) err = "Ошибка завершения сеанса";
                        db.Close();
                    }
                }

                reader.Close();
                conn_web.Close();

                if (err != "")
                {
                    ret.result = false;
                    ret.tag = -1;
                    ret.text = err;
                }

                return ret;
            }
            catch (Exception ex)
            {
                reader.Close();
                conn_web.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка заполнения списка пользователей " + err, MonitorLog.typelog.Error, 20, 201,
                    true);

                return ret;
            }
        }

        /// <summary>
        /// сохранение информации о роли
        /// </summary>
        /// <returns>result = true - успех, тогда в поле tag - nzp_role,
        ///                 = false - неудача, тогда если tag меньше 0, то в текст сообщение для вывода пользователю
        ///                                          если tag = 0, то ошибка пользователю не выводится       </returns>
        public Returns SaveRole(Role role)
        {
            Returns ret = Utils.InitReturns();

            if (role.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return ret;
            }

            if (role.role == "")
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Не задано наименование роли";
                return ret;
            }

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return ret;

#if PG
            ret = ExecSQL(conn_web,
                  " set search_path to 'public'"
                  , true);
#else
            ret = ExecSQL(conn_web,
                " set encryption password '" + BasePwd + "'"
                , true);
#endif
            if (!ret.result)
            {
                conn_web.Close();
                return ret;
            }

            string sql;

            // проверка уникальности роли
            IDataReader reader;
#if PG
            ret = ExecRead(conn_web, out reader, "select nzp_role from s_roles where role = " + Utils.EStrNull(role.role) + " and nzp_role <> " + role.nzp_role + " offset 1 ", true);
#else
            ret = ExecRead(conn_web, out reader,
                "select first 1 nzp_role from s_roles where decrypt_char(role) = " + Utils.EStrNull(role.role) +
                " and nzp_role <> " + role.nzp_role, true);
#endif
            if (ret.result)
            {
                if (reader.Read())
                {
                    ret.result = false;
                    ret.tag = -1;
                    ret.text = "Роль с таким наименованием уже существует";
                    reader.Close();
                    conn_web.Close();
                    return ret;
                }
                reader.Close();
            }
            else
            {
                conn_web.Close();
                return ret;
            }

            if (role.nzp_role > 0)
            {
#if PG
                sql = "update s_roles set (role, is_active) = (" + Utils.EStrNull(role.role) + ", " + role.is_active + ")" +
                    " Where nzp_role = " + role.nzp_role;
#else
                sql = "update s_roles set (role, is_active) = (encrypt_aes(" + Utils.EStrNull(role.role) + "), " +
                      role.is_active + ")" +
                      " Where nzp_role = " + role.nzp_role;
#endif
                ret = ExecSQL(conn_web, sql, true);
                if (ret.result) ret.tag = role.nzp_role;
            }
            else
            {
                IDbTransaction transaction;
                try
                {
                    transaction = conn_web.BeginTransaction();
                }
                catch
                {
                    transaction = null;
                }

#if PG
                sql = "insert into s_roles (role, page_url) values (" + Utils.EStrNull(role.role) + ", 0) returning nzp_role";
                int nzp = Convert.ToInt32(ExecScalar(conn_web, transaction, sql, out ret, true));
#else
                sql = "insert into s_roles (role, page_url) values (encrypt_aes(" + Utils.EStrNull(role.role) + "), 0)";
                ret = ExecSQL(conn_web, transaction, sql, true);
#endif
                if (ret.result)
                {
#if PG
#else
                    int nzp = GetSerialValue(conn_web, transaction);
#endif
                    if (nzp > 0)
                    {
                        sql = "update s_roles set sort = nzp_role where nzp_role = " + nzp;
                        ret = ExecSQL(conn_web, transaction, sql, true);
                        if (ret.result)
                        {
                            if (transaction != null) transaction.Commit();
                            ret.tag = nzp;
                        }
                        else
                        {
                            if (transaction != null) transaction.Rollback();
                            ret.result = false;
                        }
                    }
                    else
                    {
                        if (transaction != null) transaction.Rollback();
                        ret.result = false;
                    }
                }
                else if (transaction != null) transaction.Rollback();
            }
            conn_web.Close();
            return ret;
        }

        /// <summary>
        /// сохранение состояния ролей
        /// </summary>

        public Returns SaveStatusRoles(List<Role> spis)
        {
            Returns ret = Utils.InitReturns();
            foreach (Role role in spis)
            {
                if (role.nzp_user < 1)
                {
                    ret.result = false;
                    ret.text = "Не определен пользователь";
                    return ret;
                }
            }

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return ret;

#if PG
            ret = ExecSQL(conn_web,
                  " set search_path to 'public'"
                  , true);
#else
            ret = ExecSQL(conn_web,
                " set encryption password '" + BasePwd + "'"
                , true);
#endif
            if (!ret.result)
            {
                conn_web.Close();
                return ret;
            }

            string sql = "";

            foreach (Role role in spis)
            {
                if (role.nzp_role != 12)
                {
                    sql += " update s_roles set (is_active) =(" + role.is_active + ") where nzp_role =" + role.nzp_role +
                           " and page_url = " + role.page_url + " and sort = " + role.sort + ";";
                }
            }
            ret = ExecSQL(conn_web, sql, true);
            conn_web.Close();
            return ret;
        }

        /// <summary>
        /// получить список ролей
        /// </summary>
        public List<Role> GetRoles(Role finder, out Returns ret)
        {
            return GetRoles(finder, out ret, false);
        }

        public List<Role> GetRoles(Role finder, out Returns ret, bool isLoadRolesVal)
        {
            ret = Utils.InitReturns();
            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return null;
            }

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;

            List<Role> spis = GetRoles(finder, out ret, conn_web, isLoadRolesVal);
            conn_web.Close();
            return spis;
        }

        private List<Role> GetRoles(Role finder, out Returns ret, IDbConnection conn_web)
        {
            return GetRoles(finder, out ret, conn_web, false);
        }

        public void SaveUserRoles(User user, out Returns ret)
        {
            ret = Utils.InitReturns();
            if (user.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return;
            }
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return;
            IDbTransaction transaction;
            try
            {
                transaction = conn_web.BeginTransaction();
            }
            catch
            {
                transaction = null;
            }

            try
            {
                #region Удаление пользовательских ролей
                ExecSQL(conn_web, transaction, "delete from userp where nzp_user=" + user.nzpuser, true);
                #endregion

                #region Добавление пользовательских ролей
                foreach (var role in user.roles)
                {
                    ExecSQL(conn_web, transaction, "insert into userp(nzp_role,nzp_user) values (" + role.nzp_role + "," + user.nzpuser + ")", true);
                }
                #endregion

                string sql;
#if PG
                sql = "update public.userp set sign = nzp_user||CAST(nzp_role as text)||'-'||nzp_usp||'userp' where nzp_user = " + user.nzpuser.ToString();
#else
                sql =
                    "update webdb.userp set sign = encrypt_aes(nzp_user||nzp_role||'-'||nzp_usp||'userp') where nzp_user = " +
                    user.nzpuser.ToString();
#endif
                ret = ExecSQL(conn_web, transaction, sql, true);

            }
            catch
            {
                transaction.Rollback();
            }
            finally
            {
                if (ret.result)
                {
                    if (transaction != null) transaction.Commit();
                }
                else if (transaction != null) transaction.Rollback();
                conn_web.Close();
            }
        }

        public RolesTree GetRolesToTree(RolesTree finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return null;
            }

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;

#if PG
            if (!ExecSQL(conn_web,
               " set search_path to 'public'"
               , true).result)
            {
                conn_web.Close();
                return null;
            }
#else
            if (!ExecSQL(conn_web,
               " set encryption password '" + BasePwd + "'"
               , true).result)
            {
                conn_web.Close();
                return null;
            }
#endif

            RolesTree tree = new RolesTree();
            tree.SubSystemList = new SortedDictionary<int, RolesTreeNode>();
            tree.UserRolesList = new List<RolesTreeNode>();
            string where = "";
            if (finder.user_name.Length != 0)
#if PG
                where = " and upper(s.role) like '%" + finder.user_name.Trim().ToUpper() + "%'";
#else
                where = " and upper(decrypt_char(s.role)) like '%" + finder.user_name.Trim().ToUpper() + "%'";
#endif


            string sql = " select distinct decrypt_char(s.role) role_name,s.nzp_role, " +
                        DBManager.sNvlWord + "(r.nzp_role, 0) as sub_role, " +
                        " (case when s.nzp_role<=100 then 1 else 0 end) as main , " +
                        " (case when u.nzp_role is not null then 1 else 0 end) as check_node " +
                        " from s_roles s left outer join userp u on u.nzp_role=s.nzp_role " +
#if PG
 " and u.nzp_user||CAST(u.nzp_role as text)||'-'||u.nzp_usp||'userp'= u.sign and u.nzp_user=" + finder.nzp_user +
#else
 " and u.nzp_user||u.nzp_role||'-'||u.nzp_usp||'userp' = decrypt_char(u.sign) and u.nzp_user=" + finder.nzp_user +
#endif
 " left join roleskey r on s.nzp_role=r.kod and " +
                        " ( r.kod>100 and r.kod<1000 ) and r.tip=105 " +
                        " where s.is_active=1 " + where + (finder.regim == 1 ? " and u.nzp_user=" + finder.nzp_user : "") + " order by s.nzp_role";

            DataTable dt = ClassDBUtils.OpenSQL(sql, conn_web).GetData();

            foreach (DataRow row in dt.Rows)
            {
                if (row["main"].ToString() == "1")
                {
                    tree.SubSystemList.Add(Convert.ToInt32(row["nzp_role"]), new RolesTreeNode()
                   {
                       roles = new List<RolesTreeNode>(),
                       num = 1,
                       nzp_role = Convert.ToInt32(row["nzp_role"]),
                       role_name = row["role_name"].ToString().Trim(),
                       check_node = Convert.ToBoolean(row["check_node"])
                   });
                }
                else if (row["main"].ToString() != "1" && row["sub_role"].ToString() == "0")
                {
                    tree.UserRolesList.Add(new RolesTreeNode()
                   {
                       roles = null,
                       num = 2,
                       nzp_role = Convert.ToInt32(row["nzp_role"]),
                       role_name = row["role_name"].ToString().Trim(),
                       check_node = Convert.ToBoolean(row["check_node"])
                   });
                }
                else if (row["main"].ToString() != "1" && (row["sub_role"].ToString() != "0") && tree.SubSystemList.Keys.Count(a => a == Convert.ToInt32(row["sub_role"])) == 0)
                {
#if PG
                    DataTable dt1 = ClassDBUtils.OpenSQL("select distinct s.role role_name from s_roles s where nzp_role=" + row["sub_role"].ToString(), conn_web).GetData();
#else
                    DataTable dt1 = ClassDBUtils.OpenSQL("select unique decrypt_char(role) role_name from s_roles where nzp_role=" + row["sub_role"].ToString(), conn_web).GetData();
#endif
                    tree.SubSystemList.Add(Convert.ToInt32(row["sub_role"]), new RolesTreeNode()
                    {
                        roles = new List<RolesTreeNode>(),
                        num = 1,
                        nzp_role = Convert.ToInt32(row["sub_role"]),
                        role_name = dt1.Rows[0][0].ToString(),
                        check_node = false
                    });
                    tree.SubSystemList[Convert.ToInt32(row["sub_role"])].roles.Add(new RolesTreeNode()
                    {
                        roles = null,
                        num = 1,
                        nzp_role = Convert.ToInt32(row["nzp_role"]),
                        role_name = row["role_name"].ToString().Trim(),
                        check_node = Convert.ToBoolean(row["check_node"])
                    });
                }
                else if (row["main"].ToString() != "1" && (row["sub_role"].ToString() != "0") && tree.SubSystemList.Keys.Count(a => a == Convert.ToInt32(row["sub_role"])) != 0)
                {
                    tree.SubSystemList[Convert.ToInt32(row["sub_role"])].roles.Add(new RolesTreeNode()
                    {
                        roles = null,
                        num = 1,
                        nzp_role = Convert.ToInt32(row["nzp_role"]),
                        role_name = row["role_name"].ToString().Trim(),
                        check_node = Convert.ToBoolean(row["check_node"])
                    });
                }
            }
            tree.UserRolesList = tree.UserRolesList.OrderBy(a => a.role_name).ToList<RolesTreeNode>();
            return tree;
        }

        private List<Role> GetRoles(Role finder, out Returns ret, IDbConnection conn_web, bool isLoadRolesVal)
        {
#if PG
            ret = ExecSQL(conn_web,
                  " set search_path to 'public'"
                  , true);
#else
            ret = ExecSQL(conn_web,
                " set encryption password '" + BasePwd + "'"
                , true);
#endif
            if (!ret.result) return null;

            List<Role> spis = new List<Role>();

            string where = " Where 1=1";
            string tableUserp = "";
            if (finder.nzp_role > 0) where += " and r.nzp_role = " + finder.nzp_role.ToString();
#if PG
            if (finder.role != "") where += " and upper(r.role) like '%" + finder.role.ToUpper().Replace("'", "''").Replace("*", "%") + "%'";
#else
            if (finder.role != "")
                where += " and upper(decrypt_char(r.role)) like '%" +
                         finder.role.ToUpper().Replace("'", "''").Replace("*", "%") + "%'";
#endif
            if (finder.nzpuser > 0)
            {
                where += " and u.nzp_role = r.nzp_role and u.nzp_user = " + finder.nzpuser.ToString();
                tableUserp = ", userp u";
            }
            else if (finder.nzpuser < 0)
            {
                where += " and r.nzp_role not in (select nzp_role from userp where nzp_user = " +
                         (-finder.nzpuser).ToString() + ")";
                tableUserp = "";
            }

            int total_record_count = 0;
            object count = ExecScalar(conn_web, " Select count(*) From s_roles r" + tableUserp + where, out ret, true);
            if (ret.result)
            {
                try
                {
                    total_record_count = Convert.ToInt32(count);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = ex.Message;
                    return null;
                }
            }

            string skip = "";
#if PG
            if (finder.skip > 0) skip = " offset " + finder.skip.ToString();
#else
            if (finder.skip > 0) skip = " skip " + finder.skip.ToString();
            where += " and r.page_url = p.nzp_page";
#endif

#if PG
            string sql = " Select " +
                   " case when (select count(*) from role_pages rr where rr.nzp_page = 0 and rr.nzp_role = r.nzp_role and rr.sign = CAST(nzp_role as text)||CAST(nzp_page as text)||'-'||CAST(id as text)||'role_pages') > 0 " +
                   " then 1 else case when r.nzp_role between 900 and 999 then 2 else 3 end end as role_type, r.nzp_role, r.role as role, r.page_url, p.page_name as page_name, r.sort, r.is_active " +
                 " From s_roles r left outer join pages p on r.page_url = p.nzp_page " + tableUserp + where + " Order by 1, 3 " + skip;
#else
            string sql = " Select " + skip +
                         " case when (select count(*) from role_pages rr where rr.nzp_page = 0 and rr.nzp_role = r.nzp_role and decrypt_char(rr.sign) = nzp_role||nzp_page||'-'||id||'role_pages') > 0 then 1 else case when r.nzp_role between 900 and 999 then 2 else 3 end end as role_type, r.nzp_role, decrypt_char(r.role) as role, r.page_url, decrypt_char(p.page_name) as page_name, r.sort, r.is_active " +
                         " From s_roles r, outer pages p " + tableUserp + where + " Order by 1, 3";
#endif

            IDataReader reader;

            ret = ExecRead(conn_web, out reader, sql, true);
            if (!ret.result) return null;

            try
            {
                int i = 0;
                while (reader.Read())
                {
                    Role zap = new Role();

                    zap.num = ++i + finder.skip;

                    if (reader["nzp_role"] != DBNull.Value) zap.nzp_role = Convert.ToInt32(reader["nzp_role"]);
                    if (reader["role"] != DBNull.Value) zap.role = Convert.ToString(reader["role"]).Trim();
                    if (finder.nzp_role <= 0 && reader["role_type"] != DBNull.Value &&
                        Convert.ToInt32(reader["role_type"]) == 1) zap.role += " (подсистема)";
                    if (reader["page_url"] != DBNull.Value) zap.page_url = Convert.ToInt32(reader["page_url"]);
                    if (reader["page_name"] != DBNull.Value)
                        zap.page_name = Convert.ToString(reader["page_name"]).Trim();
                    if (reader["sort"] != DBNull.Value) zap.sort = Convert.ToInt32(reader["sort"]);
                    if (reader["is_active"] != DBNull.Value) zap.is_active = Convert.ToInt32(reader["is_active"]);

                    if (isLoadRolesVal)
                    {
                        zap.RolesVal = GetRolesKey(zap.nzp_role, conn_web, out ret);
                        if (!ret.result)
                        {
                            reader.Close();
                            return null;
                        }
                    }

                    spis.Add(zap);

                    if (i >= finder.rows) break;
                }

                reader.Close();
                ret.tag = total_record_count;
                return spis;
            }
            catch (Exception ex)
            {
                reader.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка заполнения списка ролей " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }
        } //GetUser

        /// <summary> Проверяет, имеет ли пользователь указанную роль
        /// </summary>
        /// <param name="finder">Надо заполнить поля nzp_user - от чьего имени выполняется запрос, 
        /// nzp_role - код роли, 
        /// nzpuser - код пользователя, у которого надо проверить наличие роли (если он отличается от nzp_user)</param>
        public List<Role> GetSubRoles(Role finder, out Returns ret)
        {
            List<Role> spis = new List<Role>();
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            int nzp_role = finder.nzp_role;
            string sql = "select * from roleskey where nzp_role=" + finder.nzp_role + " and tip=107 ";

            IDataReader reader;

            ret = ExecRead(conn_web, out reader, sql, true);

            if (!ret.result) return null;

            try
            {
                int i = 0;
                while (reader.Read())
                {
                    Role zap = new Role();
                    zap.num = ++i + finder.skip;
                    if (reader["nzp_role"] != DBNull.Value) zap.nzp_role = Convert.ToInt32(reader["nzp_role"]);
                    if (reader["kod"] != DBNull.Value) zap.num = Convert.ToInt32(reader["kod"]);
                    spis.Add(zap);

                    if (i >= finder.rows) break;
                }
                reader.Close();
                return spis;
            }
            catch (Exception ex)
            {
                reader.Close();
                ret.result = false;
                ret.text = ex.Message;
                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;
                MonitorLog.WriteLog("Ошибка заполнения списка ролей " + err, MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
        }

        /// <summary> Получает список подчиненных ролей
        /// </summary>
        /// <param name="finder">Надо заполнить поля nzp_user - от чьего имени выполняется запрос, 
        /// nzp_role - код роли, 
        /// nzpuser - код пользователя, у которого надо проверить наличие роли (если он отличается от nzp_user)</param>
        public bool IsUserHasRole(Role finder, out Returns ret)
        {
            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Не определен пользователь");
                return false;
            }
            if (finder.nzp_role < 1)
            {
                ret = new Returns(false, "Не определена роль");
                return false;
            }

            if (finder.nzpuser < 1) finder.nzpuser = finder.nzp_user;

            List<Role> roles = GetRoles(finder, out ret);
            return ret.result && roles.Count > 0;
        }

        /// <summary>
        /// Проверка на наличие права выполнить расчет по префиксу банка данных
        /// </summary>
        /// <param name="conn_web"></param>
        /// <param name="pref"></param>
        /// <returns></returns>
        public bool IsAllowCalcByPref(string pref, out Returns ret)
        {
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return false;

            try
            {
                return IsAllowCalcByPref(conn_web, pref, out ret);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                MonitorLog.WriteLog("IsAllowCalcByPref\n" + ex.Message, MonitorLog.typelog.Error, true);
                return false;
            }
            finally
            {
                conn_web.Close();
            }
        }

        public bool IsAllowCalcByPref(IDbConnection conn_web, string pref, out Returns ret)
        {
            List<_RolesVal> roles = null;

            DbAdminClient dba = new DbAdminClient();
            try
            {
                roles = dba.GetRolesKey(Constants.roleRaschetNachisleniy, conn_web, out ret);
            }
            finally
            {
                dba.Close();
            }

            bool allow = false;
            if (ret.result)
            {
                if (roles != null && roles.Count > 0)
                {
                    allow = false;
                    foreach (_RolesVal rv in roles)
                    {
                        if (rv.tip == Constants.role_sql_wp)
                        {
                            if (pref == Points.GetPref(Convert.ToInt32(rv.kod)))
                            {
                                allow = true;
                                break;
                            }
                        }
                    }
                }
                else allow = true;
            }
            return allow;
        }

        public List<_RolesVal> GetRolesKey(int nzp_role, IDbConnection conn_web, out Returns ret)
        {
#if PG
            ret = ExecSQL(conn_web,
                  " set search_path to 'public'"
                  , true);
#else
            ret = ExecSQL(conn_web,
                " set encryption password '" + BasePwd + "'"
                , true);
#endif
            if (!ret.result) return null;

#if PG
            string select = "Select distinct r.tip, r.kod";
            string from = " From roleskey r ";
            string where = " Where r.tip||CAST(r.kod as text)||r.nzp_role||'-'||r.nzp_rlsv||'roles' = r.sign" +
                " and r.nzp_role = " + nzp_role;
            if (TableInWebCashe(conn_web, "s_area"))
            {
                select += ", a.area";
                from += " left outer join s_area a on case when r.tip = 102 then r.kod else -1 end = a.nzp_area ";
            }
            else select += ", '' as area";
            if (TableInWebCashe(conn_web, "s_geu"))
            {
                select += ", g.geu";
                from += " left outer join s_geu g on case when r.tip = 103 then r.kod else -1 end = g.nzp_geu ";
            }
            else select += ", '' as geu";
            if (TableInWebCashe(conn_web, "services"))
            {
                select += ", s.service";
                from += " left outer join services s on case when r.tip = 121 then r.kod else -1 end = s.nzp_serv ";
            }
            else select += ", '' as service";
            if (TableInWebCashe(conn_web, "supplier"))
            {
                select += ", sp.name_supp";
                from += " left outer join supplier sp on case when r.tip = 120 then r.kod else -1 end = sp.nzp_supp ";
            }
            else select += ", '' as name_supp";
            if (TableInWebCashe(conn_web, "s_point"))
            {
                select += ", p.point";
                from += " left outer join s_point p on case when r.tip = 101 then r.kod else -1 end = p.nzp_wp ";
            }
            else select += ", '' as point";
            if (TableInWebCashe(conn_web, "prm_name"))
            {
                select += ", prm.name_prm";
                from += " left outer join prm_name prm on case when r.tip = " + Constants.role_sql_prm + " then r.kod else -1 end = prm.nzp_prm ";
            }
            else select += ", '' as name_prm";
            if (TableInWebCashe(conn_web, "servers") && TableInWebCashe(conn_web, "s_rcentr"))
            {
                select += ", rc.rcentr";
                from += " left outer join (servers srvrleft left outer join s_rcentr rc on and srvr.nzp_rc = rc.nzp_rc) on case when r.tip = " + Constants.role_sql_server + " then r.kod else -1 end = srvr.nzp_serve r";
            }
            else select += ", '' as rcentr";
            string sql = select + from + where + " Order by tip, kod";
#else
            string select = "Select unique r.tip, r.kod";
            string from = " From roleskey r ";
            string where = " Where r.tip||r.kod||r.nzp_role||'-'||r.nzp_rlsv||'roles' = decrypt_char(r.sign)" +
                           " and r.nzp_role = " + nzp_role;
            if (TableInWebCashe(conn_web, "s_area"))
            {
                select += ", a.area";
                from += ", outer s_area a";
                where += " and case when r.tip = 102 then r.kod else -1 end = a.nzp_area";
            }
            else select += ", '' as area";
            if (TableInWebCashe(conn_web, "s_geu"))
            {
                select += ", g.geu";
                from += ", outer s_geu g";
                where += " and case when r.tip = 103 then r.kod else -1 end = g.nzp_geu";
            }
            else select += ", '' as geu";
            if (TableInWebCashe(conn_web, "services"))
            {
                select += ", s.service";
                from += ", outer services s";
                where += " and case when r.tip = 121 then r.kod else -1 end = s.nzp_serv";
            }
            else select += ", '' as service";
            if (TableInWebCashe(conn_web, "supplier"))
            {
                select += ", sp.name_supp";
                from += ", outer supplier sp";
                where += " and case when r.tip = 120 then r.kod else -1 end = sp.nzp_supp";
            }
            else select += ", '' as name_supp";
            if (TableInWebCashe(conn_web, "s_point"))
            {
                select += ", p.point";
                from += ", outer s_point p";
                where += " and case when r.tip = 101 then r.kod else -1 end = p.nzp_wp";
            }
            else select += ", '' as point";
            if (TableInWebCashe(conn_web, "prm_name"))
            {
                select += ", prm.name_prm";
                from += ", outer prm_name prm";
                where += " and case when r.tip = " + Constants.role_sql_prm + " then r.kod else -1 end = prm.nzp_prm";
            }
            else select += ", '' as name_prm";
            if (TableInWebCashe(conn_web, "servers") && TableInWebCashe(conn_web, "s_rcentr"))
            {
                select += ", rc.rcentr";
                from += ", outer (servers srvr, outer s_rcentr rc)";
                where += " and srvr.nzp_rc = rc.nzp_rc" +
                         " and case when r.tip = " + Constants.role_sql_server +
                         " then r.kod else -1 end = srvr.nzp_server";
            }
            else select += ", '' as rcentr";
            string sql = select + from + where + " Order by tip, kod";
#endif

            IDataReader reader;
            ret = ExecRead(conn_web, out reader, sql, true);
            if (!ret.result) return null;

            List<_RolesVal> list = new List<_RolesVal>();

            try
            {
                while (reader.Read())
                {
                    _RolesVal roleVal = new _RolesVal();
                    if (reader["tip"] != DBNull.Value) roleVal.tip = Convert.ToInt32(reader["tip"]);
                    if (reader["kod"] != DBNull.Value) roleVal.kod = Convert.ToInt32(reader["kod"]);
                    switch (roleVal.tip)
                    {
                        case Constants.role_sql_wp:
                            if (reader["point"] != DBNull.Value) roleVal.val = Convert.ToString(reader["point"]).Trim();
                            break;
                        case Constants.role_sql_area:
                            if (reader["area"] != DBNull.Value) roleVal.val = Convert.ToString(reader["area"]).Trim();
                            break;
                        case Constants.role_sql_geu:
                            if (reader["geu"] != DBNull.Value) roleVal.val = Convert.ToString(reader["geu"]).Trim();
                            break;
                        case Constants.role_sql_supp:
                            if (reader["name_supp"] != DBNull.Value)
                                roleVal.val = Convert.ToString(reader["name_supp"]).Trim();
                            break;
                        case Constants.role_sql_serv:
                            if (reader["service"] != DBNull.Value)
                                roleVal.val = Convert.ToString(reader["service"]).Trim();
                            break;
                        case Constants.role_sql_prm:
                            if (reader["name_prm"] != DBNull.Value)
                                roleVal.val = Convert.ToString(reader["name_prm"]).Trim();
                            break;
                        case Constants.role_sql_server:
                            if (reader["rcentr"] != DBNull.Value)
                                roleVal.val = Convert.ToString(reader["rcentr"]).Trim();
                            break;
                    }
                    list.Add(roleVal);
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                reader.Close();

                ret.result = false;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка в функции GetRolesKey " + (Constants.Viewerror ? "\n" + ex.Message : ""),
                    MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }

            return list;
        }

        /// <summary>
        /// Получить историю входов пользователей в систему
        /// </summary>
        public List<UserAccess> GetUserAccess(UserAccess finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return null;
            }

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;

            List<UserAccess> spis = GetUserAccess(finder, out ret, conn_web);
            conn_web.Close();
            return spis;
        }

        public List<UserAccess> GetUserAccess(UserAccess finder, out Returns ret, IDbConnection conn_web)
        {
#if PG
            ret = ExecSQL(conn_web,
                  " set search_path to 'public'"
                  , true);
#else
            ret = ExecSQL(conn_web,
                " set encryption password '" + BasePwd + "'"
                , true);
#endif
            if (!ret.result) return null;

            List<UserAccess> spis = new List<UserAccess>();

            string where = " Where acc_kod = 1";
            if (finder.nzpuser > 0) where += " and a.nzp_user = " + finder.nzpuser.ToString();

            DateTime datLog = DateTime.MinValue;
            DateTime datLogPo = DateTime.MaxValue;
            if (finder.dat_log != "" && !DateTime.TryParse(finder.dat_log, out datLog))
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка в параметрах запроса";
                conn_web.Close();
                return null;
            }
            if (finder.dat_log_po != "" && !DateTime.TryParse(finder.dat_log_po, out datLogPo))
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка в параметрах запроса";
                conn_web.Close();
                return null;
            }

            if (finder.dat_log_po != "")
            {
                where += " and a.dat_log < '" + datLogPo.AddDays(1).ToString("yyyy-MM-dd HH:mm") + "'";
                if (finder.dat_log != "")
                    where += " and a.dat_log >= '" + datLog.ToString("yyyy-MM-dd HH:mm") + "'";
            }
            else if (finder.dat_log != "")
                where += " and a.dat_log >= '" + datLog.ToString("yyyy-MM-dd HH:mm") + "' and a.dat_log < '" +
                         datLog.AddDays(1).ToString("yyyy-MM-dd HH:mm") + "'";

            int total_record_count = 0;
            object count = ExecScalar(conn_web, " Select count(*) From log_access a" + where, out ret, true);
            if (ret.result)
            {
                try
                {
                    total_record_count = Convert.ToInt32(count);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = ex.Message;
                    return null;
                }
            }

            string skip = "";
#if PG
            if (finder.skip > 0)
            {
                if(finder.skip < total_record_count)
                    skip = " offset " + finder.skip.ToString();
            }
#else
            if (finder.skip > 0)
            {
                if (finder.skip < total_record_count)
                    skip = " skip " + finder.skip.ToString();
            }
#endif

#if PG
            string sql = " Select " +
                   " nzp_lacc, nzp_user, acc_kod, dat_log, ip_log, browser, login, pwd, idses, (select max(dat_log) from log_access b where b.idses = a.idses and acc_kod = 2) as dat_exit " +
                 " From log_access a " + where;
            sql += " Order by dat_log desc" + skip;
#else
            string sql = " Select " + skip +
                         " nzp_lacc, nzp_user, acc_kod, dat_log, ip_log, browser, login, pwd, idses, (select max(dat_log) from log_access b where b.idses = a.idses and acc_kod = 2) as dat_exit " +
                         " From log_access a " + where;
            sql += " Order by dat_log desc";
#endif

            IDataReader reader;

            ret = ExecRead(conn_web, out reader, sql, true);
            if (!ret.result) return null;

            try
            {
                int i = 0;
                while (reader.Read())
                {
                    UserAccess zap = new UserAccess();

                    zap.num = ++i + finder.skip;

                    if (reader["nzp_lacc"] != DBNull.Value) zap.nzp_lacc = Convert.ToInt32(reader["nzp_lacc"]);
                    if (reader["nzp_user"] != DBNull.Value) zap.nzpuser = Convert.ToInt32(reader["nzp_user"]);
                    if (reader["acc_kod"] != DBNull.Value) zap.acc_kod = Convert.ToInt32(reader["acc_kod"]);
                    if (reader["dat_log"] != DBNull.Value)
                        zap.dat_log = String.Format("{0:dd.MM.yyyy HH:mm}", reader["dat_log"]);
                    if (reader["ip_log"] != DBNull.Value) zap.ip_log = Convert.ToString(reader["ip_log"]);
                    if (reader["browser"] != DBNull.Value) zap.browser = Convert.ToString(reader["browser"]);
                    if (reader["login"] != DBNull.Value) zap.login = Convert.ToString(reader["login"]);
                    if (reader["pwd"] != DBNull.Value) zap.pwd = Convert.ToString(reader["pwd"]);
                    if (reader["idses"] != DBNull.Value) zap.idses = Convert.ToString(reader["idses"]);
                    if (reader["dat_exit"] != DBNull.Value)
                        zap.dat_exit = String.Format("{0:dd.MM.yyyy HH:mm}", reader["dat_exit"]);

                    spis.Add(zap);

                    if (i >= finder.rows) break;
                }

                reader.Close();
                ret.tag = total_record_count;
                return spis;
            }
            catch (Exception ex)
            {
                reader.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка заполнения списка ролей " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }
        }

        /// <summary>
        /// Получить историю действий пользователей
        /// </summary>
        public List<UserActions> GetUsersActionsList(UserActions finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return null;
            }

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;

            List<UserActions> spis = GetUsersActionsList(finder, out ret, conn_web);
            conn_web.Close();
            return spis;
        }

        public List<UserActions> GetUsersActionsList(UserActions finder, out Returns ret, IDbConnection conn_web)
        {
#if PG
            ret = ExecSQL(conn_web,
                  " set search_path to 'public'"
                  , true);
#else
            ret = ExecSQL(conn_web,
                " set encryption password '" + BasePwd + "'"
                , true);
#endif
            if (!ret.result) return null;

            List<UserActions> spis = new List<UserActions>();


            string where = " Where 1 = 1 ";
            if (finder.nzp_user_act > 0)
                where += " and ua.nzp_user = " + finder.nzp_user_act.ToString();
            if (finder.from_date != null)
                where += " and cast(ua.changed_on as date) >= cast('" + Convert.ToDateTime(finder.from_date).ToShortDateString() + "' as date) ";
            if (finder.to_date != null)
                where += " and cast(ua.changed_on as date) <= cast('" + Convert.ToDateTime(finder.to_date).ToShortDateString() + "' as date) ";
            if (finder.pages_list != null)
            {
                where += " and ua.nzp_page in (";
                for (int i = 0; i < finder.pages_list.Count; i++)
                {
                    where += finder.pages_list[i] + ",";
                }
                where = where.Substring(0, where.Length - 1) + ") ";
            }

            int total_record_count = 0;
            object count = ExecScalar(conn_web, " Select count(*) From user_actions ua" + where, out ret, true);
            if (ret.result)
            {
                try
                {
                    total_record_count = Convert.ToInt32(count);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = ex.Message;
                    return null;
                }
            }

#if PG
            string skip = finder.skip >= 0 && finder.rows >= 0 ? " offset " + finder.skip + " limit " + finder.rows : String.Empty;
#else
            string skip = finder.skip >= 0 && finder.rows >= 0 ? " skip " + finder.skip + " first " + finder.rows : String.Empty;
#endif

#if PG
            string sql = " select ua.*, u.login, u.uname from user_actions ua " +
                            " join users u on ua.nzp_user = u.nzp_user   " + where;
            sql += " Order by changed_on desc" + skip;
#else
            string sql = " Select " + skip +
                         " ua.*, u.login, decrypt_char(u.uname) as uname from user_actions ua " +
                            " join users u on ua.nzp_user = u.nzp_user   " + where;
            sql += " Order by changed_on desc";
#endif

            IDataReader reader;

            ret = ExecRead(conn_web, out reader, sql, true);
            if (!ret.result) return null;

            try
            {
                int i = 0;
                while (reader.Read())
                {
                    UserActions resEl = new UserActions();

                    resEl.num = ++i + finder.skip;

                    if (reader["id"] != DBNull.Value) resEl.id = Convert.ToInt32(reader["id"]);
                    if (reader["nzp_user"] != DBNull.Value) resEl.nzp_user = Convert.ToInt32(reader["nzp_user"]);
                    if (reader["login"] != DBNull.Value) resEl.login = Convert.ToString(reader["login"]);
                    if (reader["uname"] != DBNull.Value) resEl.user = Convert.ToString(reader["uname"]);
                    if (reader["nzp_page"] != DBNull.Value) resEl.nzp_page = Convert.ToInt32(reader["nzp_page"]);
                    if (reader["page_name"] != DBNull.Value && reader["page_name"].ToString() != "")
                        resEl.page_name = Convert.ToString(reader["page_name"]);
                    else
                        resEl.page_name = Convert.ToString("Страница №" + reader["nzp_page"]);
                    if (reader["nzp_act"] != DBNull.Value) resEl.nzp_act = Convert.ToInt32(reader["nzp_act"]);
                    if (reader["act_name"] != DBNull.Value) resEl.act_name = Convert.ToString(reader["act_name"]);
                    if (reader["changed_on"] != DBNull.Value)
                        resEl.changed_on = Convert.ToDateTime(reader["changed_on"]);

                    spis.Add(resEl);
                }

                reader.Close();
                ret.tag = total_record_count;
                return spis;
            }
            catch (Exception ex)
            {
                reader.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка получения списка действий пользователей " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }
        }

        /// <summary>
        /// Получить список пользователей, у которых есть история действий
        /// </summary>
        public List<UserActions> GetUsersInActList(Finder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return null;
            }

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;

            List<UserActions> spis = GetUsersInActList(finder, out ret, conn_web);
            conn_web.Close();
            return spis;
        }

        public List<UserActions> GetUsersInActList(Finder finder, out Returns ret, IDbConnection conn_web)
        {
#if PG
            ret = ExecSQL(conn_web,
                  " set search_path to 'public'"
                  , true);
#else
            ret = ExecSQL(conn_web,
                " set encryption password '" + BasePwd + "'"
                , true);
#endif
            if (!ret.result) return null;

            List<UserActions> spis = new List<UserActions>();

            int total_record_count = 0;
            object count = ExecScalar(conn_web, " select count(*) from (select distinct u.* from users u join user_actions ua on u.nzp_user = ua.nzp_user) ", out ret, true);
            if (ret.result)
            {
                try
                {
                    total_record_count = Convert.ToInt32(count);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = ex.Message;
                    return null;
                }
            }
#if PG
            string sql = " select distinct u.nzp_user,trim(u.login)  || ' (' || trim(u.uname) || ')' as login, u.uname from users u " +
                                " join user_actions ua on u.nzp_user = ua.nzp_user " +
                            " order by login ";
#else
            string sql = " select distinct u.nzp_user, trim(u.login)  || ' (' || trim(decrypt_char(u.uname)) || ')' as login from users u " +
                                " join user_actions ua on u.nzp_user = ua.nzp_user " +
                            " order by login ";
#endif

            IDataReader reader;

            ret = ExecRead(conn_web, out reader, sql, true);
            if (!ret.result) return null;

            try
            {
                UserActions resEl = new UserActions() { login = "Все", nzp_user = -1 };
                spis.Add(resEl);
                while (reader.Read())
                {
                    resEl = new UserActions();

                    if (reader["nzp_user"] != DBNull.Value) resEl.nzp_user = Convert.ToInt32(reader["nzp_user"]);
                    if (reader["login"] != DBNull.Value) resEl.login = Convert.ToString(reader["login"]).Trim();

                    spis.Add(resEl);
                }

                reader.Close();
                ret.tag = total_record_count;
                return spis;
            }
            catch (Exception ex)
            {
                reader.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка получения списка пользователей " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }
        }

        /// <summary>
        /// Получить список страниц, которые есть в истории действий
        /// </summary>
        public List<UserActions> GetPagesInActList(Finder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return null;
            }

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;

            List<UserActions> spis = GetPagesInActList(finder, out ret, conn_web);
            conn_web.Close();
            return spis;
        }

        public List<UserActions> GetPagesInActList(Finder finder, out Returns ret, IDbConnection conn_web)
        {
#if PG
            ret = ExecSQL(conn_web,
                  " set search_path to 'public'"
                  , true);
#else
            ret = ExecSQL(conn_web,
                " set encryption password '" + BasePwd + "'"
                , true);
#endif
            if (!ret.result) return null;

            List<UserActions> spis = new List<UserActions>();

            int total_record_count = 0;
            object count = ExecScalar(conn_web, " select count(*) from (select distinct nzp_page, trim(page_name) from user_actions) ", out ret, true);
            if (ret.result)
            {
                try
                {
                    total_record_count = Convert.ToInt32(count);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = ex.Message;
                    return null;
                }
            }
#if PG
            string sql = " select distinct nzp_page, trim(page_name) as page_name from user_actions order by page_name ";
#else
            string sql = " select distinct nzp_page, trim(page_name) as page_name from user_actions order by page_name ";
#endif

            IDataReader reader;

            ret = ExecRead(conn_web, out reader, sql, true);
            if (!ret.result) return null;

            try
            {
                while (reader.Read())
                {
                    var resEl = new UserActions();

                    if (reader["nzp_page"] != DBNull.Value) resEl.nzp_page = Convert.ToInt32(reader["nzp_page"]);
                    if (reader["page_name"] != DBNull.Value && reader["page_name"] != "")
                        resEl.page_name = Convert.ToString(reader["page_name"]);
                    else
                        resEl.page_name = "Страница №" + resEl.nzp_page;

                    spis.Add(resEl);
                }

                reader.Close();
                ret.tag = total_record_count;
                return spis;
            }
            catch (Exception ex)
            {
                reader.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка получения списка страниц " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }
        }

        /// <summary>
        /// Добавление привязки пользователя к ролям
        /// </summary>
        /// <param name="user">пользователь, в списке roles - роли для добавления</param>
        /// <returns></returns>
        public Returns AddUserRoles(User user)
        {
            Returns ret = Utils.InitReturns();

            if (user.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return ret;
            }

            if (user.nzpuser < 1)
            {
                ret.result = false;
                ret.text = "Не выбран пользователь";
                return ret;
            }

            if (user.roles.Count == 0)
            {
                ret.result = false;
                ret.text = "Не выбрана роль";
                return ret;
            }

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return ret;

            IDbTransaction transaction;
            try
            {
                transaction = conn_web.BeginTransaction();
            }
            catch
            {
                transaction = null;
            }

#if PG
            ret = ExecSQL(conn_web,
                  " set search_path to 'public'"
                  , true);
#else
            ret = ExecSQL(conn_web, transaction,
                " set encryption password '" + BasePwd + "'"
                , true);
#endif
            if (!ret.result)
            {
                if (transaction != null) transaction.Rollback();
                conn_web.Close();
                return ret;
            }

            string sql;
            IDataReader reader;

            foreach (Role role in user.roles)
            {
#if PG
                sql = "select r.nzp_role, r.role as role from userp p, s_roles r where p.nzp_role = r.nzp_role and p.nzp_user = " + user.nzpuser.ToString() + " and p.nzp_role = " + role.nzp_role.ToString();
#else
                sql =
                    "select r.nzp_role, decrypt_char(r.role) as role from userp p, s_roles r where p.nzp_role = r.nzp_role and p.nzp_user = " +
                    user.nzpuser.ToString() + " and p.nzp_role = " + role.nzp_role.ToString();
#endif
                ret = ExecRead(conn_web, transaction, out reader, sql, true);
                if (!ret.result)
                {
                    if (transaction != null) transaction.Rollback();
                    conn_web.Close();
                    return ret;
                }
                if (!reader.Read())
                {
                    sql = "insert into userp (nzp_user, nzp_role) values (" + user.nzpuser.ToString() + ", " +
                          role.nzp_role.ToString() + ")";
                    ret = ExecSQL(conn_web, transaction, sql, true);
                    if (!ret.result)
                    {
                        if (transaction != null) transaction.Rollback();
                        reader.Close();
                        conn_web.Close();
                        return ret;
                    }
                }
                else
                {
                    ret.result = false;
                    ret.tag = -1;
                    if (reader["role"] != null)
                        ret.text = "Пользователь уже принадлежит роли \"" + Convert.ToString(reader["role"]).Trim() +
                                   "\"";
                    else ret.text = "Пользователь уже принадлежит выбранной роли";
                    if (transaction != null) transaction.Rollback();
                    reader.Close();
                    conn_web.Close();
                    return ret;
                }
                reader.Close();
            }
#if PG
            sql = "update public.userp set sign = nzp_user||CAST(nzp_role as text)||'-'||nzp_usp||'userp' where nzp_user = " + user.nzpuser.ToString();
#else
            sql =
                "update webdb.userp set sign = encrypt_aes(nzp_user||nzp_role||'-'||nzp_usp||'userp') where nzp_user = " +
                user.nzpuser.ToString();
#endif
            ret = ExecSQL(conn_web, transaction, sql, true);
            if (ret.result)
            {
                if (transaction != null) transaction.Commit();
            }
            else if (transaction != null) transaction.Rollback();
            conn_web.Close();
            return ret;
        }

        /// <summary>
        /// Удаление привязки пользователя к ролям
        /// </summary>
        /// <param name="user">пользователь, в списке roles - роли для удаления</param>
        /// <returns></returns>
        public Returns DeleteUserRoles(User user)
        {
            Returns ret = Utils.InitReturns();

            if (user.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return ret;
            }

            if (user.nzpuser < 1)
            {
                ret.result = false;
                ret.text = "Не выбран пользователь";
                return ret;
            }

            if (user.roles.Count == 0)
            {
                ret.result = false;
                ret.text = "Не определена роль";
                return ret;
            }

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return ret;

            IDbTransaction transaction;
            try
            {
                transaction = conn_web.BeginTransaction();
            }
            catch
            {
                transaction = null;
            }

            string sql;
            foreach (Role role in user.roles)
            {
                sql = "delete from userp where nzp_user = " + user.nzpuser.ToString() + " and nzp_role = " +
                      role.nzp_role.ToString();
                ret = ExecSQL(conn_web, transaction, sql, true);
                if (!ret.result)
                {
                    if (transaction != null) transaction.Rollback();
                    conn_web.Close();
                    return ret;
                }
            }
            if (transaction != null) transaction.Commit();
            conn_web.Close();
            return ret;
        }

        /// <summary>
        /// Блокировка пользователя
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public Returns BlockUser(User user)
        {
            Returns ret = Utils.InitReturns();

            if (user.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return ret;
            }

            if (user.nzpuser < 1)
            {
                ret.result = false;
                ret.text = "Не выбран пользователь";
                return ret;
            }

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return ret;

            ret = ExecSQL(conn_web, "update users set is_blocked = 1 where nzp_user = " + user.nzpuser.ToString(), true);
            conn_web.Close();
            return ret;
        }

        /// <summary>
        /// Разблокировка пользователя
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public Returns UnblockUser(User user)
        {
            Returns ret = Utils.InitReturns();

            if (user.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return ret;
            }

            if (user.nzpuser < 1)
            {
                ret.result = false;
                ret.text = "Не выбран пользователь";
                return ret;
            }

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return ret;

            ret = ExecSQL(conn_web, "update users set is_blocked = 0 where nzp_user = " + user.nzpuser.ToString(), true);
            conn_web.Close();
            return ret;
        }

        /// <summary>
        /// Блокирует / разблокирует пользователя в зависимости от его текущего состояния
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public Returns ChangeUserBlock(User user)
        {
            Returns ret = Utils.InitReturns();

            if (user.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return ret;
            }

            if (user.nzpuser < 1)
            {
                ret.result = false;
                ret.text = "Не выбран пользователь";
                return ret;
            }

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return ret;

#if PG
            ret = ExecSQL(conn_web, "update users set is_blocked = case when is_blocked is null or CAST(is_blocked as int) <> 1 then 1 else 0 end where nzp_user = " + user.nzpuser.ToString(), true);
#else
            ret = ExecSQL(conn_web,
                "update users set is_blocked = case when is_blocked is null or is_blocked <> 1 then 1 else 0 end where nzp_user = " +
                user.nzpuser.ToString(), true);
#endif
            conn_web.Close();
            return ret;
        }

        /// <summary> добавить условия фильтрации данных для роли
        /// </summary>
        public Returns AddRolesVal(Role role)
        {
            Returns ret = Utils.InitReturns();

            if (role.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return ret;
            }

            if (role.nzp_role < 1)
            {
                ret.result = false;
                ret.text = "Не задана роль";
                return ret;
            }

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return ret;

            IDbTransaction transaction;
            try
            {
                transaction = conn_web.BeginTransaction();
            }
            catch
            {
                transaction = null;
            }

#if PG
            ret = ExecSQL(conn_web,
                  " set search_path to 'public'"
                  , true);
#else
            ret = ExecSQL(conn_web, transaction,
                " set encryption password '" + BasePwd + "'"
                , true);
#endif
            if (!ret.result)
            {
                if (transaction != null) transaction.Rollback();
                conn_web.Close();
                return ret;
            }

            IDataReader reader;

            string sql;
            if (role.RolesVal != null)
            {
                foreach (_RolesVal rv in role.RolesVal)
                {
                    if (rv.tip < 1)
                    {
                        if (transaction != null) transaction.Rollback();
                        conn_web.Close();

                        ret.result = false;
                        ret.tag = -1;
                        ret.text = "Не задан тип ограничения";
                        return ret;
                    }
                    if (rv.kod < 1)
                    {
                        if (transaction != null) transaction.Rollback();
                        conn_web.Close();

                        ret.result = false;
                        ret.tag = -1;
                        switch (rv.tip)
                        {
                            case Constants.role_sql_area:
                                ret.text = "Не задана Управляющая организация";
                                break;
                            case Constants.role_sql_geu:
                                ret.text = "Не задан участок";
                                break;
                            case Constants.role_sql_wp:
                                ret.text = "Не задан банк банных";
                                break;
                            case Constants.role_sql_serv:
                                ret.text = "Не задана услуга";
                                break;
                            case Constants.role_sql_supp:
                                ret.text = "Не задан поставщик";
                                break;
                            case Constants.role_sql_prm:
                                ret.text = "Не задан параметр";
                                break;
                            default:
                                ret.text = "Не задано условие";
                                break;
                        }
                        return ret;
                    }
                    sql = "select nzp_rlsv from roleskey where nzp_role = " + role.nzp_role + " and tip = " + rv.tip +
                          " and kod = " + rv.kod;
                    ret = ExecRead(conn_web, transaction, out reader, sql, true);
                    if (!ret.result)
                    {
                        if (transaction != null) transaction.Rollback();
                        conn_web.Close();
                        return ret;
                    }
                    if (!reader.Read())
                    {
                        reader.Close();
                        sql = "insert into roleskey (nzp_role, tip, kod) values (" + role.nzp_role + "," + rv.tip + "," +
                              rv.kod + ")";
                        ret = ExecSQL(conn_web, transaction, sql, true);
                        if (!ret.result)
                        {
                            if (transaction != null) transaction.Rollback();
                            conn_web.Close();
                            return ret;
                        }

#if PG
                        sql = "update roleskey set sign = tip||CAST(kod as text)||nzp_role||'-'||nzp_rlsv||'roles' where nzp_role = " + role.nzp_role;
#else
                        sql =
                            "update roleskey set sign = encrypt_aes(tip||kod||nzp_role||'-'||nzp_rlsv||'roles') where nzp_role = " +
                            role.nzp_role;
#endif
                        ret = ExecSQL(conn_web, transaction, sql, true);
                        if (!ret.result)
                        {
                            if (transaction != null) transaction.Rollback();
                            conn_web.Close();
                            return ret;
                        }
                    }
                    else reader.Close();
                }
            }
            if (transaction != null) transaction.Commit();
            conn_web.Close();
            return ret;
        }

        /// <summary> удалить условия фильтрации данных для роли
        /// </summary>
        public Returns DeleteRolesVal(Role role)
        {
            Returns ret = Utils.InitReturns();

            if (role.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return ret;
            }

            if (role.nzp_role < 1)
            {
                ret.result = false;
                ret.text = "Не задана роль";
                return ret;
            }

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return ret;

            IDbTransaction transaction;
            try
            {
                transaction = conn_web.BeginTransaction();
            }
            catch
            {
                transaction = null;
            }

            string sql;
            if (role.RolesVal != null)
            {
                foreach (_RolesVal rv in role.RolesVal)
                {
                    if (rv.tip < 1)
                    {
                        if (transaction != null) transaction.Rollback();
                        conn_web.Close();

                        ret.result = false;
                        ret.tag = -1;
                        ret.text = "Не задан тип ограничения";
                        return ret;
                    }
                    if (rv.kod < 1)
                    {
                        if (transaction != null) transaction.Rollback();
                        conn_web.Close();

                        ret.result = false;
                        ret.tag = -1;
                        switch (rv.tip)
                        {
                            case Constants.role_sql_area:
                                ret.text = "Не задана Управляющая организация";
                                break;
                            case Constants.role_sql_geu:
                                ret.text = "Не задан участок";
                                break;
                            case Constants.role_sql_wp:
                                ret.text = "Не задан банк банных";
                                break;
                            case Constants.role_sql_serv:
                                ret.text = "Не задана услуга";
                                break;
                            case Constants.role_sql_supp:
                                ret.text = "Не задан поставщик";
                                break;
                            case Constants.role_sql_prm:
                                ret.text = "Не задан параметр";
                                break;
                            default:
                                ret.text = "Не задано условие";
                                break;
                        }
                        return ret;
                    }
                    sql = "delete from roleskey where nzp_role = " + role.nzp_role + " and tip = " + rv.tip +
                          " and kod = " + rv.kod;
                    ret = ExecSQL(conn_web, transaction, sql, true);
                    if (!ret.result)
                    {
                        if (transaction != null) transaction.Rollback();
                        conn_web.Close();
                        return ret;
                    }
                }
            }
            if (transaction != null) transaction.Commit();
            conn_web.Close();
            return ret;
        }

        /// <summary> сделать строку where 
        /// </summary>
        private string makeWhereForProcess(BackgroundProcess finder, string alias, ref Returns ret)
        {
            if (alias != "") alias = alias + ".";

            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Не определен пользователь";
                return "";
            }

            DateTime datIn = DateTime.MinValue;
            DateTime datInPo = DateTime.MaxValue;

            if (finder.dat_in != "" && !DateTime.TryParse(finder.dat_in, out datIn))
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка в параметрах запроса";
                return "";
            }
            if (finder.dat_in_po != "" && !DateTime.TryParse(finder.dat_in_po, out datInPo))
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка в параметрах запроса";
                return "";
            }

            string where = "";

            if (finder.nzp_key > 0) where += " and " + alias + "nzp_key = " + finder.nzp_key.ToString();

            if (finder.dat_in_po != "")
            {
                where += " and " + alias + "dat_in < '" + datInPo.AddDays(1).ToString("yyyy-MM-dd HH:mm") + "'";
                if (finder.dat_in != "")
                    where += " and " + alias + "dat_in >= '" + datIn.ToString("yyyy-MM-dd HH:mm") + "'";
            }
            else if (finder.dat_in != "")
                where += " and " + alias + "dat_in >= '" + datIn.ToString("yyyy-MM-dd HH:mm") + "' and "
                         + alias + "dat_in < '" + datIn.AddDays(1).ToString("yyyy-MM-dd HH:mm") + "'";

            string prms = "";
            if (Utils.GetParams(finder.prms, Constants.act_process_in_queue.ToString()))
                prms += "," + BackgroundProcess.getKodInfo(Constants.act_process_in_queue);
            if (Utils.GetParams(finder.prms, Constants.act_process_active.ToString()))
                prms += "," + BackgroundProcess.getKodInfo(Constants.act_process_active);
            if (Utils.GetParams(finder.prms, Constants.act_process_finished.ToString()))
                prms += "," + BackgroundProcess.getKodInfo(Constants.act_process_finished);
            if (Utils.GetParams(finder.prms, Constants.act_process_with_errors.ToString()))
                prms += "," + BackgroundProcess.getKodInfo(Constants.act_process_with_errors);
            if (prms != "") where += " and " + alias + "kod_info in (" + prms.Substring(1, prms.Length - 1) + ")";

            return where;
        }

        private string makeWhereForProcess(ProcessWithYearMonth finder, string alias, ref Returns ret)
        {
            string where = makeWhereForProcess((BackgroundProcess)finder, alias, ref ret);

            if (alias != "") alias = alias + ".";

            if (finder.year_ > 0) where += " and " + alias + "year_ = " + finder.year_;
            if (finder.month_ > 0) where += " and " + alias + "month_ = " + finder.month_;

            return where;
        }

        private string makeWhereForProcess(ProcessSaldo finder, string alias, ref Returns ret)
        {
            string where = makeWhereForProcess(finder as ProcessWithYearMonth, alias, ref ret);

            if (alias != "") alias = alias + ".";

            if (finder.nzp_area > 0) where += " and " + alias + "nzp_area = " + finder.nzp_area;

            return where;
        }

        private string makeWhereForProcess(ProcessCalc finder, string alias, ref Returns ret)
        {
            string where = makeWhereForProcess(finder as ProcessWithYearMonth, alias, ref ret);

            if (alias != "") alias = alias + ".";

            if (finder.nzp >= 0) where += " and " + alias + "nzp = " + finder.nzp;
            if (finder.nzpt > 0) where += " and " + alias + "nzpt = " + finder.nzpt;
            if (finder.task >= 0) where += " and " + alias + "task = " + finder.task;
            if (finder.prior > 0) where += " and " + alias + "prior = " + finder.prior;

            return where;
        }

        private string makeWhereForProcess(ProcessBill finder, string alias, ref Returns ret)
        {
            string where = makeWhereForProcess(finder as ProcessWithYearMonth, alias, ref ret);

            if (alias != "") alias = alias + ".";

            if (finder.nzp_key > 0) where += " and " + alias + "nzp_key = " + finder.nzp_key;
            if (finder.nzp_area > 0) where += " and " + alias + "nzp_area = " + finder.nzp_area;
            if (finder.nzp_geu > 0) where += " and " + alias + "nzp_geu = " + finder.nzp_geu;
            if (finder.nzp_wp > 0) where += " and " + alias + "nzp_wp = " + finder.nzp_wp;

            return where;
        }

        /// <summary> Получить список фоновых процессов расчета сальдо
        /// </summary>
        public List<ProcessSaldo> GetProcessSaldo(ProcessSaldo finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            string where = makeWhereForProcess(finder, "p", ref ret);
            if (!ret.result) return null;

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;

            int total_record_count = 0;
            object count = ExecScalar(conn_web, " Select count(*) From saldo_fon p Where 1=1 " + where, out ret, true);
            if (ret.result)
            {
                try
                {
                    total_record_count = Convert.ToInt32(count);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = ex.Message;
                    return null;
                }
            }

            string skip = "";
#if PG
            if (finder.skip > 0) skip = " offset " + finder.skip.ToString();

            string sql = " Select p.nzp_key, p.nzp_area, p.year_, p.month_, p.kod_info, p.dat_in, p.dat_work, p.dat_out, p.txt, a.area " +
                " From saldo_fon p, outer s_area a" +
                " Where a.nzp_area = p.nzp_area" + where +
                " Order by dat_in desc, a.area, year_, month_" + skip;
#else
            if (finder.skip > 0) skip = " skip " + finder.skip.ToString();

            string sql = " Select " + skip +
                         " p.nzp_key, p.nzp_area, p.year_, p.month_, p.kod_info, p.dat_in, p.dat_work, p.dat_out, p.txt, a.area " +
                         " From saldo_fon p, outer s_area a" +
                         " Where a.nzp_area = p.nzp_area" + where +
                         " Order by dat_in desc, a.area, year_, month_";
#endif

            IDataReader reader;

            ret = ExecRead(conn_web, out reader, sql, true);
            if (!ret.result)
            {
                conn_web.Close();
                return null;
            }
            try
            {
                List<ProcessSaldo> Spis = new List<ProcessSaldo>();

                int i = 0;
                while (reader.Read())
                {
                    ProcessSaldo zap = new ProcessSaldo();

                    zap.num = ++i + finder.skip;

                    if (reader["nzp_key"] != DBNull.Value) zap.nzp_key = Convert.ToInt32(reader["nzp_key"]);
                    if (reader["nzp_area"] != DBNull.Value) zap.nzp_area = Convert.ToInt32(reader["nzp_area"]);
                    if (reader["area"] != DBNull.Value) zap.area = Convert.ToString(reader["area"]).Trim();
                    if (reader["year_"] != DBNull.Value) zap.year_ = Convert.ToInt32(reader["year_"]);
                    if (reader["month_"] != DBNull.Value) zap.month_ = Convert.ToInt32(reader["month_"]);
                    if (reader["kod_info"] != DBNull.Value) zap.kod_info = Convert.ToInt32(reader["kod_info"]);
                    if (reader["dat_in"] != DBNull.Value)
                        zap.dat_in = String.Format("{0:dd.MM.yyyy HH:mm}", reader["dat_in"]);
                    if (reader["dat_work"] != DBNull.Value)
                        zap.dat_work = String.Format("{0:dd.MM.yyyy HH:mm}", reader["dat_work"]);
                    if (reader["dat_out"] != DBNull.Value)
                        zap.dat_out = String.Format("{0:dd.MM.yyyy HH:mm}", reader["dat_out"]);
                    if (reader["txt"] != DBNull.Value) zap.txt = Convert.ToString(reader["txt"]).Trim();

                    Spis.Add(zap);

                    if (i >= finder.rows) break;
                }

                reader.Close();
                conn_web.Close();
                ret.tag = total_record_count;
                return Spis;
            }
            catch (Exception ex)
            {
                reader.Close();
                conn_web.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка заполнения списка процессов " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }
        }

        public Returns SaveProcessSaldo(ProcessSaldo proc)
        {
            Returns ret = Utils.InitReturns();

            if (proc.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return ret;
            }

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return ret;

            string sql;

            if (proc.nzp_key > 0)
            {


                sql = "update saldo_fon set txt = " + Utils.EStrNull(proc.txt.Trim(), "") +
                      " Where nzp_key = " + proc.nzp_key;
                ret = ExecSQL(conn_web, sql, true);
            }
            else
            {
                IDbTransaction transaction;
                try
                {
                    transaction = conn_web.BeginTransaction();
                }
                catch
                {
                    transaction = null;
                }

                int numMonths;
                if (proc.year_ == proc.year_po)
                    numMonths = proc.month_po - proc.month_ + 1;
                else if (proc.year_po > proc.year_)
                    numMonths = proc.year_po * 12 + proc.month_po - proc.year_ * 12 - proc.month_ + 1;
                else
                    numMonths = 0;

                int y = proc.year_, m = proc.month_;
                for (int i = 0; i < numMonths; i++)
                {
                    sql = "insert into saldo_fon (nzp_area, year_, month_, kod_info, dat_in, txt)";
                    if (proc.nzp_area < 1)
                    {
                        sql += " select nzp_area, " + y + ", " + m + ", " +
                               BackgroundProcess.getKodInfo(Constants.act_process_in_queue) +
                               ", current, " + Utils.EStrNull(proc.txt.Trim(), "") + " from s_area a " +
                               " where (select count(*) from saldo_fon b where a.nzp_area = b.nzp_area " +
                               " and b.year_ = " + y + " and b.month_ = " + m + " and b.kod_info = " +
                               BackgroundProcess.getKodInfo(Constants.act_process_in_queue) + ") = 0";
                        ret = ExecSQL(conn_web, transaction, sql, true);
                    }
                    else
                    {
                        object num = ExecScalar(conn_web, transaction,
                            "select count(*) from saldo_fon b where b.nzp_area = " + proc.nzp_area +
                            " and b.year_ = " + y + " and b.month_ = " + m + " and b.kod_info = " +
                            BackgroundProcess.getKodInfo(Constants.act_process_in_queue) + "", out ret, true);
                        if (!ret.result)
                        {
                            if (transaction != null) transaction.Rollback();
                            conn_web.Close();
                            return ret;
                        }
                        if (Convert.ToInt32(num) == 0)
                        {
                            sql += " values (" + proc.nzp_area + ", " + y + ", " + m + ", " +
                                   BackgroundProcess.getKodInfo(Constants.act_process_in_queue) +
                                   ", current, " + Utils.EStrNull(proc.txt.Trim(), "") + ")";
                            ret = ExecSQL(conn_web, transaction, sql, true);
                        }
                    }

                    if (!ret.result)
                    {
                        if (transaction != null) transaction.Rollback();
                        conn_web.Close();
                        return ret;
                    }

                    if (m < 12) m++;
                    else
                    {
                        m = 1;
                        y++;
                    }
                }

                if (transaction != null) transaction.Commit();
            }
            conn_web.Close();

            return ret;
        }

        public Returns DeleteProcessSaldo(ProcessSaldo proc)
        {
            Returns ret = Utils.InitReturns();
            string where = makeWhereForProcess(proc, "", ref ret);
            if (!ret.result) return ret;

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return ret;

            string sql = "delete from saldo_fon Where 1=1 " + where;
            ret = ExecSQL(conn_web, sql, true);

            conn_web.Close();

            return ret;
        }

        /// <summary> Получить список фоновых процессов расчета начислений
        /// </summary>
        public List<ProcessCalc> GetProcessCalc(ProcessCalc finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            string where = makeWhereForProcess(finder, "p", ref ret);
            if (where != "") where = " Where 1=1 " + where;
            if (!ret.result) return null;

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;

            string skip = "";
#if PG
            if (finder.skip > 0) skip = " offset " + finder.skip.ToString();
#else
            if (finder.skip > 0) skip = " skip " + finder.skip.ToString();
#endif

            string table, sql, fields = "";

            if (finder.queue != Constants._ZERO_)
            {
                table = "calc_fon_" + finder.queue;

                if (!TableInWebCashe(conn_web, table))
                {
                    conn_web.Close();
                    return new List<ProcessCalc>();
                }
            }
            else
            {
                string tmpTable = "t" + finder.nzp_user + "_tmp_calc_fon";
                sql = "drop table " + tmpTable;
                ExecSQL(conn_web, sql, false);

#if PG
                sql = "CREATE temp TABLE " + tmpTable + " (" +
                    " nzp_key INTEGER NOT NULL" +
                    ", queue INTEGER NOT NULL" +
                    ", nzp INTEGER default 0 NOT NULL" +
                    ", nzpt INTEGER default 0 NOT NULL" +
                    ", year_ INTEGER default 0 NOT NULL" +
                    ", month_ INTEGER default 0 NOT NULL" +
                    ", task INTEGER default 0 NOT NULL" +
                    ", prior INTEGER default 0 NOT NULL" +
                    ", kod_info INTEGER default 0 NOT NULL" +
                    ", dat_in TIMESTAMP" +
                    ", dat_work TIMESTAMP" +
                    ", dat_out TIMESTAMP" +
                    ", txt CHAR(255))";
#else
                sql = "CREATE temp TABLE " + tmpTable + " (" +
                      " nzp_key INTEGER NOT NULL" +
                      ", queue INTEGER NOT NULL" +
                      ", nzp INTEGER default 0 NOT NULL" +
                      ", nzpt INTEGER default 0 NOT NULL" +
                      ", year_ INTEGER default 0 NOT NULL" +
                      ", month_ INTEGER default 0 NOT NULL" +
                      ", task INTEGER default 0 NOT NULL" +
                      ", prior INTEGER default 0 NOT NULL" +
                      ", kod_info INTEGER default 0 NOT NULL" +
                      ", dat_in DATETIME YEAR to MINUTE" +
                      ", dat_work DATETIME YEAR to MINUTE" +
                      ", dat_out DATETIME YEAR to MINUTE" +
                      ", txt CHAR(255))";
#endif
                ret = ExecSQL(conn_web, sql, true);

                if (!ret.result)
                {
                    conn_web.Close();
                    return null;
                }

                for (int i = 0; i < 5; i++)
                {
                    table = "calc_fon_" + i;
                    if (!TableInWebCashe(conn_web, table)) continue;

                    sql = "Insert into " + tmpTable +
                          " (nzp_key, queue, nzp, nzpt, year_, month_, task, prior, kod_info, dat_in, dat_work, dat_out, txt)" +
                          " Select p.nzp_key, " + i +
                          ", p.nzp, p.nzpt, p.year_, p.month_, p.task, p.prior, p.kod_info, p.dat_in, p.dat_work, p.dat_out, p.txt" +
                          " From " + table + " p" +
                          where;
                    ret = ExecSQL(conn_web, sql, true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return null;
                    }
                }
                table = tmpTable;
                where = "";
                fields = ", queue";
            }

            int total_record_count = 0;
            object count = ExecScalar(conn_web, " Select count(*) From " + table + " p" + where, out ret, true);
            if (ret.result)
            {
                try
                {
                    total_record_count = Convert.ToInt32(count);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = ex.Message;
                    return null;
                }
            }

#if PG
            sql = " Select p.nzp_key, p.nzp, p.nzpt, p.year_, p.month_, p.task, p.prior, p.kod_info, p.dat_in, p.dat_work, p.dat_out, p.txt" + fields +
                " From " + table + " p" +
                where +
                " Order by dat_in desc, year_, month_" + skip;
#else
            sql = " Select " + skip +
                  " p.nzp_key, p.nzp, p.nzpt, p.year_, p.month_, p.task, p.prior, p.kod_info, p.dat_in, p.dat_work, p.dat_out, p.txt" +
                  fields +
                  " From " + table + " p" +
                  where +
                  " Order by dat_in desc, year_, month_";
#endif

            IDataReader reader;

            ret = ExecRead(conn_web, out reader, sql, true);
            if (!ret.result)
            {
                conn_web.Close();
                return null;
            }
            try
            {
                List<ProcessCalc> Spis = new List<ProcessCalc>();

                int i = 0;
                while (reader.Read())
                {
                    ProcessCalc zap = new ProcessCalc();

                    zap.num = ++i + finder.skip;

                    if (reader["nzp_key"] != DBNull.Value) zap.nzp_key = Convert.ToInt32(reader["nzp_key"]);
                    if (reader["nzp"] != DBNull.Value) zap.nzp = Convert.ToInt32(reader["nzp"]);
                    if (reader["nzpt"] != DBNull.Value) zap.nzpt = Convert.ToInt32(reader["nzpt"]);
                    if (reader["year_"] != DBNull.Value) zap.year_ = Convert.ToInt32(reader["year_"]);
                    if (reader["month_"] != DBNull.Value) zap.month_ = Convert.ToInt32(reader["month_"]);
                    if (reader["task"] != DBNull.Value) zap.task = Convert.ToInt32(reader["task"]);
                    if (reader["prior"] != DBNull.Value) zap.prior = Convert.ToInt32(reader["prior"]);
                    if (reader["kod_info"] != DBNull.Value) zap.kod_info = Convert.ToInt32(reader["kod_info"]);
                    if (reader["dat_in"] != DBNull.Value)
                        zap.dat_in = String.Format("{0:dd.MM.yyyy HH:mm}", reader["dat_in"]);
                    if (reader["dat_work"] != DBNull.Value)
                        zap.dat_work = String.Format("{0:dd.MM.yyyy HH:mm}", reader["dat_work"]);
                    if (reader["dat_out"] != DBNull.Value)
                        zap.dat_out = String.Format("{0:dd.MM.yyyy HH:mm}", reader["dat_out"]);
                    if (reader["txt"] != DBNull.Value) zap.txt = Convert.ToString(reader["txt"]).Trim();

                    if (finder.queue != Constants._ZERO_) zap.queue = finder.queue;
                    else if (reader["queue"] != DBNull.Value) zap.queue = Convert.ToInt32(reader["queue"]);

                    Spis.Add(zap);

                    if (i >= finder.rows) break;
                }

                reader.Close();
                conn_web.Close();
                ret.tag = total_record_count;
                return Spis;
            }
            catch (Exception ex)
            {
                reader.Close();
                conn_web.Close();

                ret.result = false;
                ret.text = ex.Message;

                MonitorLog.WriteLog(
                    "Ошибка в функции GetProcessCalc заполнения списка процессов " +
                    (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }
        }

        public Returns SaveProcessCalc(ProcessCalc proc)
        {
            if (proc.nzp_user < 1) return new Returns(false, "Не определен пользователь");

            Returns ret;

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return ret;

            string table = "calc_fon_" + proc.queue;

            if (!TableInWebCashe(conn_web, table))
            {
                conn_web.Close();
                return new Returns(false, "Не найдена таблица для сохранения задания");
            }

            string sql;

            if (proc.nzp_key > 0)
            {
                sql = "Update " + table + " set txt = " + Utils.EStrNull(proc.txt.Trim(), "") +
                      " Where nzp_key = " + proc.nzp_key;
                ret = ExecSQL(conn_web, sql, true);
            }
            else
            {
                IDbTransaction transaction;
                try
                {
                    transaction = conn_web.BeginTransaction();
                }
                catch
                {
                    transaction = null;
                }

                int numMonths;
                if (proc.year_ == proc.year_po)
                    numMonths = proc.month_po - proc.month_ + 1;
                else if (proc.year_po > proc.year_)
                    numMonths = proc.year_po * 12 + proc.month_po - proc.year_ * 12 - proc.month_ + 1;
                else
                    numMonths = 0;

                int y = proc.year_, m = proc.month_;
                for (int i = 0; i < numMonths; i++)
                {
                    sql = "insert into " + table + " (nzp, nzpt, year_, month_, task, prior, kod_info, dat_in, txt)" +
                          " values (" + proc.nzp + ", " + proc.nzpt + ", " + y + ", " + m + ", " + proc.task + ", " +
                          proc.prior + ", " + BackgroundProcess.getKodInfo(Constants.act_process_in_queue) +
                          ", current, " + Utils.EStrNull(proc.txt.Trim(), "") + ")";
                    ret = ExecSQL(conn_web, transaction, sql, true);
                    if (!ret.result)
                    {
                        if (transaction != null) transaction.Rollback();
                        conn_web.Close();
                        return ret;
                    }

                    if (m < 12) m++;
                    else
                    {
                        m = 1;
                        y++;
                    }
                }

                if (transaction != null) transaction.Commit();
            }
            conn_web.Close();

            return ret;
        }

        public Returns DeleteProcessCalc(ProcessCalc proc)
        {
            Returns ret = Utils.InitReturns();
            string where = makeWhereForProcess(proc, "", ref ret);
            if (!ret.result) return ret;

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return ret;

            string table;
            for (int i = 0; i < 5; i++)
            {
                if (proc.queue != Constants._ZERO_ && proc.queue != i) continue;
                table = "calc_fon_" + i;
                if (!TableInWebCashe(conn_web, table)) continue;

                string sql = "delete from " + table + " Where 1=1 " + where;
                ret = ExecSQL(conn_web, sql, true);
                if (!ret.result)
                {
                    conn_web.Close();
                    return ret;
                }
            }
            conn_web.Close();
            return ret;
        }

        /// <summary>
        /// Получить список заданий на формирование платежных документов
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public List<ProcessBill> GetProcessBill(ProcessBill finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            string where = makeWhereForProcess(finder, "p", ref ret);
            if (!ret.result) return null;

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;

            string table = "bill_fon";

            int total_record_count = 0;
            object count = ExecScalar(conn_web, " Select count(*) From " + table + " p Where 1=1 " + where, out ret,
                true);
            if (ret.result)
            {
                try
                {
                    total_record_count = Convert.ToInt32(count);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = ex.Message;
                    return null;
                }
            }

            string skip = "";
#if PG
            if (finder.skip > 0) skip = " offset " + finder.skip.ToString();
            string sql = " Select p.nzp_key, p.nzp_area, a.area, p.nzp_geu, b.geu, p.nzp_wp, c.point, p.year_, p.month_, p.kod_info, p.dat_in, p.dat_work, p.dat_out, p.txt, p.file_name, p.progress " +
                " From " + table + " p left outer join s_area a on (a.nzp_area = p.nzp_area) left outer join s_geu b on (p.nzp_geu = b.nzp_geu) left outer join s_point c on (p.nzp_wp = c.nzp_wp) " +
                " Where 1=1 " + where +
                " Order by dat_in desc, year_, month_, c.point, a.area, b.geu" + skip;
#else
            if (finder.skip > 0) skip = " skip " + finder.skip.ToString();
            string sql = " Select " + skip +
                         " p.nzp_key, p.nzp_area, a.area, p.nzp_geu, b.geu, p.nzp_wp, c.point, p.year_, p.month_, p.kod_info, p.dat_in, p.dat_work, p.dat_out, p.txt, p.file_name, p.progress " +
                         " From " + table + " p, outer s_area a, outer s_geu b, outer s_point c" +
                         " Where a.nzp_area = p.nzp_area and p.nzp_geu = b.nzp_geu and p.nzp_wp = c.nzp_wp " + where +
                         " Order by dat_in desc, year_, month_, c.point, a.area, b.geu";
#endif

            IDataReader reader;

            ret = ExecRead(conn_web, out reader, sql, true);
            if (!ret.result)
            {
                conn_web.Close();
                return null;
            }
            try
            {
                List<ProcessBill> Spis = new List<ProcessBill>();

                int i = 0;
                while (reader.Read())
                {
                    ProcessBill zap = new ProcessBill();

                    zap.num = ++i + finder.skip;

                    if (reader["nzp_key"] != DBNull.Value) zap.nzp_key = Convert.ToInt32(reader["nzp_key"]);
                    if (reader["nzp_area"] != DBNull.Value) zap.nzp_area = Convert.ToInt32(reader["nzp_area"]);
                    if (reader["area"] != DBNull.Value) zap.area = Convert.ToString(reader["area"]).Trim();
                    if (reader["nzp_geu"] != DBNull.Value) zap.nzp_geu = Convert.ToInt32(reader["nzp_geu"]);
                    if (reader["geu"] != DBNull.Value) zap.geu = Convert.ToString(reader["geu"]).Trim();

                    if (reader["nzp_wp"] != DBNull.Value) zap.nzp_wp = Convert.ToInt32(reader["nzp_wp"]);
                    if (reader["point"] != DBNull.Value) zap.point = Convert.ToString(reader["point"]).Trim();

                    if (reader["year_"] != DBNull.Value) zap.year_ = Convert.ToInt32(reader["year_"]);
                    if (reader["month_"] != DBNull.Value) zap.month_ = Convert.ToInt32(reader["month_"]);
                    if (reader["kod_info"] != DBNull.Value) zap.kod_info = Convert.ToInt32(reader["kod_info"]);
                    if (reader["dat_in"] != DBNull.Value)
                        zap.dat_in = String.Format("{0:dd.MM.yyyy HH:mm}", reader["dat_in"]);
                    if (reader["dat_work"] != DBNull.Value)
                        zap.dat_work = String.Format("{0:dd.MM.yyyy HH:mm}", reader["dat_work"]);
                    if (reader["dat_out"] != DBNull.Value)
                        zap.dat_out = String.Format("{0:dd.MM.yyyy HH:mm}", reader["dat_out"]);
                    if (reader["txt"] != DBNull.Value) zap.txt = Convert.ToString(reader["txt"]).Trim();
                    if (reader["file_name"] != DBNull.Value)
                        zap.file_name = Convert.ToString(reader["file_name"]).Trim();
                    if (reader["progress"] != DBNull.Value) zap.progress = Convert.ToDecimal(reader["progress"]);

                    Spis.Add(zap);

                    if (i >= finder.rows) break;
                }

                reader.Close();
                conn_web.Close();
                ret.tag = total_record_count;
                return Spis;
            }
            catch (Exception ex)
            {
                reader.Close();
                conn_web.Close();

                ret.result = false;
                ret.text = ex.Message;

                MonitorLog.WriteLog(
                    "Ошибка заполнения списка заданий на формирование платежных документов\n" + ex.Message,
                    MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }
        }

        /// <summary>
        /// Добавить или изменить задания на формирование платежных документов
        /// </summary>
        /// <param name="tasks"></param>
        /// <returns></returns>
        public Returns SaveProcessBill(List<ProcessBill> tasks)
        {
            if (tasks == null) return new Returns(false, "Неверно заданы входные параметры");
            if (tasks.Count == 0) return new Returns(false, "Неверно заданы входные параметры");
            if (tasks[0].nzp_user < 1) return new Returns(false, "Не определен пользователь");

            Returns ret;

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return ret;

            string table = "bill_fon";

            if (!TableInWebCashe(conn_web, table))
            {
                conn_web.Close();
                return new Returns(false, "Не найдена таблица для сохранения задания");
            }

            string sql;

            IDbTransaction transaction;
            try
            {
                transaction = conn_web.BeginTransaction();
            }
            catch
            {
                transaction = null;
            }

            foreach (ProcessBill bill in tasks)
            {
                if (bill.nzp_key > 0)
                {
                    sql = "Update " + table + " set txt = " + Utils.EStrNull(bill.txt.Trim(), "") +
                          " Where nzp_key = " + bill.nzp_key;
                }
                else
                {
                    sql = "insert into " + table +
                          " (nzp_key, nzp_area, nzp_geu, nzp_wp, year_, month_, kod_info, dat_in, txt, " +
                          "nzp_user, count_list_in_pack, kod_sum_faktura, result_file_type, id_faktura, with_dolg)" +
#if PG
 " values (default, " + bill.nzp_area +
#else
 " values (0, " + bill.nzp_area +
#endif
 ", " + bill.nzp_geu +
                          ", " + bill.nzp_wp +
                          ", " + bill.year_ +
                          ", " + bill.month_ +
                          ", " + BackgroundProcess.getKodInfo(Constants.act_process_in_queue) +
                          ", " + DBManager.sCurDateTime +
                          ", " + Utils.EStrNull(bill.txt.Trim(), "") +
                          ", " + bill.nzp_user +
                          ", " + bill.count_list_in_pack +
                          ", " + bill.kod_sum_faktura +
                          ", " + Utils.EStrNull(bill.result_file_type.Trim()) +
                          ", " + bill.id_faktura +
                          ", " + (bill.with_dolg ? "1" : "0") + ")";
                }
                ret = ExecSQL(conn_web, transaction, sql, true);
                if (!ret.result)
                {
                    if (transaction != null) transaction.Rollback();
                    conn_web.Close();
                    return ret;
                }
            }

            if (transaction != null) transaction.Commit();
            conn_web.Close();

            ret.tag = tasks.Count;

            return ret;
        }

        /// <summary>
        /// Обновляет процент выполнения задания по формированию платежных документов
        /// </summary>
        /// <param name="finder">Необходимо заполнить поля nzp_key, progress (от 0 до 1)</param>
        /// <returns></returns>
        public Returns SetProcessBillProgress(ProcessBill finder)
        {
            IDbConnection conn_db = GetConnection(Constants.cons_Webdata);
            Returns ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;

            string sql = "update bill_fon set progress = " + finder.progress.ToString("N4") + " where nzp_key = " +
                         finder.nzp_key;
            ret = ExecSQL(conn_db, sql, true);

            conn_db.Close();
            return ret;
        }

        /// <summary>
        /// Удалить задания на формирование платежных документов
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        public Returns DeleteProcessBill(ProcessBill finder)
        {
            Returns ret = Utils.InitReturns();
            string where = makeWhereForProcess(finder, "", ref ret);
            if (!ret.result) return ret;

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return ret;

            string table = "bill_fon";
            if (!TableInWebCashe(conn_web, table))
            {
                ret.result = false;
                ret.text = "Таблицы со списком заданий на формирование платежных документов не существует";
                MonitorLog.WriteLog(ret.text, MonitorLog.typelog.Error, true);
                return ret;
            }

            string sql = "delete from " + table + " Where 1=1 " + where;
            ret = ExecSQL(conn_web, sql, true);

            conn_web.Close();
            return ret;
        }

        /// <summary>
        /// Сгенерировать уникальный идентификатор запроса на восстановление пароля
        /// </summary>
        /// <param name="finder">Логин или email пользователя, чей пароль надо восстановить</param>
        /// <param name="ret">Результат выполнения операции</param>
        /// <returns>Информация о пользователе (pwd - уникальная строка-идентификатор запроса на восстановление)</returns>
        public User AddPasswordRecoveryRequest(User finder, out Returns ret)
        {
            ret = Utils.InitReturns();

            if (finder.login == "" && finder.email == "")
            {
                ret.result = false;
                ret.text = "Значение не задано";
                return null;
            }

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;

#if PG
            ret = ExecSQL(conn_web,
                  " set search_path to 'public'"
                  , true);
#else
            ret = ExecSQL(conn_web,
                " set encryption password '" + BasePwd + "'"
                , true);
#endif
            if (!ret.result)
            {
                conn_web.Close();
                return null;
            }

#if PG
            string sql = "select nzp_user, email, uname from users";
#else
            string sql = "select nzp_user, decrypt_char(email) as email, decrypt_char(uname) as uname from users";
#endif
            if (finder.login != "")
                sql += " where login = " + Utils.EStrNull(finder.login);
            else if (finder.email != "")
#if PG
                sql += " where email = " + Utils.EStrNull(finder.email);
#else
                sql += " where decrypt_char(email) = " + Utils.EStrNull(finder.email);
#endif
            else sql += " where 1 = 0";

            IDataReader reader;
            ret = ExecRead(conn_web, out reader, sql, true);
            if (!ret.result)
            {
                conn_web.Close();
                return null;
            }

            if (!reader.Read())
            {
                ret.result = false;
                ret.text = "Логин или e-mail не найден";
                ret.tag = -1;
                reader.Close();
                return null;
            }

            User user = new User();
            if (reader["nzp_user"] != DBNull.Value) user.nzp_user = Convert.ToInt32(reader["nzp_user"]);
            if (reader["email"] != DBNull.Value) user.email = Convert.ToString(reader["email"]).Trim();
            if (reader["uname"] != DBNull.Value) user.uname = Convert.ToString(reader["uname"]).Trim();

            reader.Close();

            if (user.nzp_user <= 0)
            {
                ret.result = false;
                ret.text = "Пользователь не найден";
                return null;
            }

            if (!TableInWebCashe(conn_web, tableUsersRecovery))
            {
#if PG
                ret = ExecSQL(conn_web, "create table public." + tableUsersRecovery +
                    " ( nzp_rec serial not null, " +
                    " nzp_user integer not null, " +
                    " request_str char(100), " +
                    " pwd char(200), " +
                    " dat_request timestamp)", true);
#else
                ret = ExecSQL(conn_web, "create table webdb." + tableUsersRecovery +
                                        " ( nzp_rec serial not null, " +
                                        " nzp_user integer not null, " +
                                        " request_str char(100), " +
                                        " pwd char(200), " +
                                        " dat_request datetime year to second)", true);
#endif
                if (!ret.result)
                {
                    conn_web.Close();
                    return null;
                }
#if PG
                ExecSQL(conn_web, "create distinct index ix_" + tableUsersRecovery + "_1 on " + tableUsersRecovery + " (nzp_rec)", true);
                ExecSQL(conn_web, "create index ix_" + tableUsersRecovery + "_2 on " + tableUsersRecovery + " (request_str)", true);
#else
                ExecSQL(conn_web,
                    "create unique index webdb.ix_" + tableUsersRecovery + "_1 on webdb." + tableUsersRecovery +
                    " (nzp_rec)", true);
                ExecSQL(conn_web,
                    "create index webdb.ix_" + tableUsersRecovery + "_2 on webdb." + tableUsersRecovery +
                    " (request_str)", true);
#endif
            }

            user.requestId = RandomText.Generate(100); // генерация идентификатора запроса

            IDbTransaction transaction;
            try
            {
                transaction = conn_web.BeginTransaction();
            }
            catch
            {
                transaction = null;
            }

            // удаление просроченных запросов с пустыми паролями и невостребованные пользователем запросы
#if PG
            ret = ExecSQL(conn_web, transaction,
                            "delete from " + tableUsersRecovery + " where pwd is null and (dat_request < now() - interval '" +
                            Constants.recovery_link_lifetime + "  hour' or nzp_user = " + user.nzp_user + ")", true);
#else
            ret = ExecSQL(conn_web, transaction,
                            "delete from " + tableUsersRecovery + " where pwd is null and (dat_request < current - " +
                            Constants.recovery_link_lifetime + " units hour or nzp_user = " + user.nzp_user + ")", true);
#endif
            if (!ret.result)
            {
                if (transaction != null) transaction.Rollback();
                conn_web.Close();
                return null;
            }

#if PG
            ret = ExecSQL(conn_web, transaction, "insert into " + tableUsersRecovery + " values (default, " + user.nzp_user + ", " + Utils.EStrNull(user.requestId) + ", null, now())", true);
#else
            ret = ExecSQL(conn_web, transaction,
                "insert into " + tableUsersRecovery + " values (0, " + user.nzp_user + ", " +
                Utils.EStrNull(user.requestId) + ", null, current year to second)", true);
#endif
            if (!ret.result)
            {
                if (transaction != null) transaction.Rollback();
                conn_web.Close();
                return null;
            }

            if (transaction != null) transaction.Commit();
            conn_web.Close();
            return user;
        }

        /// <summary>
        /// Проверяет действительность запроса восстановления пароля
        /// </summary>
        /// <param name="user">идентификатор запроса восстановления</param>
        /// <param name="ret">Результат выполнения операции</param>
        /// <returns>Информация о пользователе в случае успеха операции</returns>
        public Returns isPasswordRecoveryRequestValid(User finder)
        {
            if (finder.requestId == "") return new Returns(false, "Ошибка в параметрах запроса");

            Returns ret = Utils.InitReturns();

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return ret;

            if (!TableInWebCashe(conn_web, tableUsersRecovery))
            {
                conn_web.Close();
                return new Returns(false,
                    "Ссылка на восстановление пароля недействительная. Отправьте новый запрос на восстановление.");
            }

            IDataReader reader;
#if PG
            ret = ExecRead(conn_web, out reader,
                            "select a.nzp_rec, u.login from " + tableUsersRecovery +
                            " a, users u where u.nzp_user = a.nzp_user and request_str = " + Utils.EStrNull(finder.requestId) +
                            " and dat_request between now() - interval '" + Constants.recovery_link_lifetime + " hour' and now()",
                            true);
#else
            ret = ExecRead(conn_web, out reader,
                "select a.nzp_rec, u.login from " + tableUsersRecovery +
                " a, users u where u.nzp_user = a.nzp_user and request_str = " + Utils.EStrNull(finder.requestId) +
                " and dat_request between current - " + Constants.recovery_link_lifetime + " units hour and current",
                true);
#endif
            if (!ret.result)
            {
                conn_web.Close();
                return ret;
            }

            if (!reader.Read())
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ссылка на восстановление пароля недействительная. Отправьте новый запрос на восстановление.";
            }
            else
            {
                if (reader["login"] != DBNull.Value) ret.text = Convert.ToString(reader["login"]).Trim();
            }

            reader.Close();
            conn_web.Close();

            return ret;
        }

        /// <summary>
        /// Сохраняет новый пароль пользователя
        /// </summary>
        /// <param name="user">Новый пароль и идентификатор запроса восстановления</param>
        /// <param name="ret">Результат выполнения операции</param>
        /// <returns>Информация о пользователе в случае успеха операции</returns>
        public User SetNewPassword(User finder, out Returns ret)
        {
            ret = Utils.InitReturns();

            if (finder.pwd == "")
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Не задан пароль";
                return null;
            }

            if (finder.pwd == "#")
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Недопустимый пароль";
                return null;
            }

            if (finder.requestId == "")
            {
                ret.result = false;
                ret.text = "Ошибка в параметрах запроса";
                return null;
            }

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;

#if PG
            ret = ExecSQL(conn_web,
                  " set search_path to 'public'"
                  , true);
#else
            ret = ExecSQL(conn_web,
                " set encryption password '" + BasePwd + "'"
                , true);
#endif
            if (!ret.result)
            {
                conn_web.Close();
                return null;
            }

            // проверка действительности запроса
            IDataReader reader;
#if PG
            ret = ExecRead(conn_web, out reader,
                           "select a.nzp_rec, a.nzp_user, u.login, decrypt_char(u.uname) as uname from " + tableUsersRecovery +
                           " a, users u where u.nzp_user = a.nzp_user and a.pwd is null and request_str = " +
                           Utils.EStrNull(finder.requestId) + " and dat_request between now() - interval '" +
                           Constants.recovery_link_lifetime + " hour' and now()", true);
#else
            ret = ExecRead(conn_web, out reader,
                           "select a.nzp_rec, a.nzp_user, u.login, decrypt_char(u.uname) as uname from " + tableUsersRecovery +
                           " a, users u where u.nzp_user = a.nzp_user and a.pwd is null and request_str = " +
                           Utils.EStrNull(finder.requestId) + " and dat_request between current - " +
                           Constants.recovery_link_lifetime + " units hour and current", true);
#endif
            if (!ret.result)
            {
                conn_web.Close();
                return null;
            }

            if (!reader.Read())
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ссылка на восстановление пароля недействительная. Отправьте новый запрос на восстановление.";
                reader.Close();
                return null;
            }

            User user = new User();
            if (reader["nzp_user"] != DBNull.Value) user.nzp_user = Convert.ToInt32(reader["nzp_user"]);
            if (reader["login"] != DBNull.Value) user.login = Convert.ToString(reader["login"]).Trim();
            if (reader["uname"] != DBNull.Value) user.uname = Convert.ToString(reader["uname"]).Trim();
            int nzpRec = 0;
            if (reader["nzp_rec"] != DBNull.Value) nzpRec = Convert.ToInt32(reader["nzp_rec"]);

            reader.Close();

            if (user.nzp_user <= 0)
            {
                conn_web.Close();
                ret.result = false;
                ret.text = "Пользователь не найден";
                return null;
            }

            //проверка пароля на уникальность среди последних введенных паролей
#if PG
            ret = ExecRead(conn_web, out reader, "select * from " + tableUsersRecovery + " where pwd is not null and nzp_user = " + user.nzp_user + " and nzp_user||'-'||" + Utils.EStrNull(finder.pwd) + " = pwd", true);
#else
            ret = ExecRead(conn_web, out reader,
                "select * from " + tableUsersRecovery + " where pwd is not null and nzp_user = " + user.nzp_user +
                " and nzp_user||'-'||" + Utils.EStrNull(finder.pwd) + " = decrypt_char(pwd)", true);
#endif
            if (!ret.result)
            {
                conn_web.Close();
                return null;
            }
            if (reader.Read())
            {
                reader.Close();
                conn_web.Close();
                ret.result = false;
                ret.tag = -1;
                ret.text = "Такой пароль уже использовался ранее. Задайте другой пароль.";
                return null;
            }
            else reader.Close();

            IDbTransaction transaction;
            try
            {
                transaction = conn_web.BeginTransaction();
            }
            catch
            {
                transaction = null;
            }

#if PG
            ret = ExecSQL(conn_web, transaction, "update users set pwd = nzp_user||'-'||" + Utils.EStrNull(finder.pwd) + " where nzp_user = " + user.nzp_user, true);
#else
            ret = ExecSQL(conn_web, transaction,
                "update users set pwd = encrypt_aes(nzp_user||'-'||" + Utils.EStrNull(finder.pwd) +
                ") where nzp_user = " + user.nzp_user, true);
#endif
            if (!ret.result)
            {
                if (transaction != null) transaction.Rollback();
                conn_web.Close();
                return null;
            }

            // сохранить пароль для заданного запроса на восстановление
#if PG
            ret = ExecSQL(conn_web, transaction, "update " + tableUsersRecovery + " set pwd = nzp_user||'-'||" + Utils.EStrNull(finder.pwd) + " where nzp_rec = " + nzpRec, true);
#else
            ret = ExecSQL(conn_web, transaction,
                "update " + tableUsersRecovery + " set pwd = encrypt_aes(nzp_user||'-'||" + Utils.EStrNull(finder.pwd) +
                ") where nzp_rec = " + nzpRec, true);
#endif
            if (!ret.result)
            {
                if (transaction != null) transaction.Rollback();
                conn_web.Close();
                return null;
            }
            // удалить просроченные запросы с пустыми паролями
#if PG
            ret = ExecSQL(conn_web, transaction, "delete from " + tableUsersRecovery +
                                                    " where dat_request < now() - interval '" + Constants.recovery_link_lifetime + " hour'", true);
#else
            ret = ExecSQL(conn_web, transaction, "delete from " + tableUsersRecovery +
                                                 " where dat_request < current - " + Constants.recovery_link_lifetime +
                                                 " units hour", true);
#endif
            if (!ret.result)
            {
                if (transaction != null) transaction.Rollback();
                conn_web.Close();
                return null;
            }
            // оставить только 5 последних запросов, для которых был введен новый пароль, остальные удалить
#if PG
            ret = ExecRead(conn_web, transaction, out reader, "select  nzp_rec from " + tableUsersRecovery +
                                                                " where nzp_user = " + user.nzp_user + " order by nzp_rec desc limit 5 ", true);
#else
            ret = ExecRead(conn_web, transaction, out reader, "select first 5 nzp_rec from " + tableUsersRecovery +
                                                              " where nzp_user = " + user.nzp_user +
                                                              " order by nzp_rec desc ", true);
#endif
            if (!ret.result)
            {
                if (transaction != null) transaction.Rollback();
                conn_web.Close();
                return null;
            }
            int counter = 0;
            string listRec = "";
            while (reader.Read() && counter < 5)
            {
                if (reader["nzp_rec"] != DBNull.Value) listRec += "," + Convert.ToString(reader["nzp_rec"]).Trim();
                counter++;
            }
            reader.Close();
            if (listRec != "")
            {
                ret = ExecSQL(conn_web, transaction,
                    "delete from " + tableUsersRecovery + " where nzp_user = " + user.nzp_user +
                    " and nzp_rec not in (0" + listRec + ")", true);
                if (!ret.result)
                {
                    if (transaction != null) transaction.Rollback();
                    conn_web.Close();
                    return null;
                }
            }

            if (transaction != null) transaction.Commit();
            return user;
        }

        public Setup GetSetup(Setup finder, out Returns ret)
        {
            ret = Utils.InitReturns();

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;
#if PG
            ExecSQL(conn_web, "set search_path to 'public'", true);
#else
            ret = ExecSQL(conn_web, " set encryption password '" + BasePwd + "'", true);
            if (!ret.result)
            {
                conn_web.Close();
                return null;
            }
#endif
            Setup setup = GetSetup(finder, out ret, conn_web);

            conn_web.Close();
            return setup;
        }

        public Setup GetSetup(Setup finder, out Returns ret, IDbConnection conn_web)
        {
            IDataReader reader;
#if PG
            ret = ExecRead(conn_web, out reader, "select nzp_setup, nzp_param, param_name, value_, nzp_user, dat_when from s_setups where nzp_param = " + finder.nzp_param, true);
#else
            ret = ExecRead(conn_web, out reader,
                "select nzp_setup, nzp_param, decrypt_char(param_name) as param_name, decrypt_char(value_) as value_, nzp_user, dat_when from s_setups where nzp_param = " +
                finder.nzp_param, true);
#endif
            if (!ret.result) return null;

            if (!reader.Read())
            {
                reader.Close();
                ret.result = false;
                ret.tag = -1;
                ret.text = "Значение параметра не найдено";
                return null;
            }

            Setup setup = new Setup();

            if (reader["nzp_setup"] != DBNull.Value) setup.nzp_setup = Convert.ToInt32(reader["nzp_setup"]);
            if (reader["nzp_param"] != DBNull.Value) setup.nzp_param = Convert.ToInt32(reader["nzp_param"]);
            if (reader["param_name"] != DBNull.Value) setup.param_name = Convert.ToString(reader["param_name"]).Trim();
            if (reader["value_"] != DBNull.Value) setup.value = Convert.ToString(reader["value_"]).Trim();
            if (reader["nzp_user"] != DBNull.Value) setup.nzpuser = Convert.ToInt32(reader["nzp_user"]);
            if (reader["dat_when"] != DBNull.Value)
                setup.dat_when = String.Format("{0:dd.MM.yyyy HH:mm:ss}", reader["dat_when"]);

            reader.Close();

            return setup;
        }

        public List<Setup> GetListSetup(Setup finder, out Returns ret)
        {
            ret = Utils.InitReturns();

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;

#if PG
            ret = ExecSQL(conn_web,
                  " set search_path to 'public'"
                  , true);
#else
            ret = ExecSQL(conn_web,
                " set encryption password '" + BasePwd + "'"
                , true);
#endif
            if (!ret.result)
            {
                conn_web.Close();
                return null;
            }

            List<Setup> setup = GetListSetup(finder, out ret, conn_web);

            conn_web.Close();
            return setup;
        }

        public List<Setup> GetListSetup(Setup finder, out Returns ret, IDbConnection conn_web)
        {
            IDataReader reader;
#if PG
            ret = ExecRead(conn_web, out reader, "select nzp_setup, nzp_param, param_name, value_, param_type, nzp_user, dat_when from s_setups ", true);
#else
            ret = ExecRead(conn_web, out reader,
                "select nzp_setup, nzp_param, decrypt_char(param_name) as param_name, decrypt_char(value_) as value_, decrypt_char(param_type) as param_type, nzp_user, dat_when from s_setups ",
                true);
#endif
            if (!ret.result) return null;

            /* if (!reader.Read())
             {
                 reader.Close();
                 ret.result = false;
                 ret.tag = -1;
                 ret.text = "Параметры не найдены";
                 return null;
             }*/

            List<Setup> listsetup = new List<Setup>();

            try
            {
                while (reader.Read())
                {
                    Setup setup = new Setup();
                    if (reader["nzp_setup"] != DBNull.Value) setup.nzp_setup = Convert.ToInt32(reader["nzp_setup"]);
                    if (reader["nzp_param"] != DBNull.Value) setup.nzp_param = Convert.ToInt32(reader["nzp_param"]);
                    if (reader["param_name"] != DBNull.Value)
                        setup.param_name = Convert.ToString(reader["param_name"]).Trim();
                    if (reader["value_"] != DBNull.Value) setup.value = Convert.ToString(reader["value_"]).Trim();
                    if (reader["param_type"] != DBNull.Value)
                        setup.param_type = Convert.ToString(reader["param_type"]).Trim();
                    if (reader["nzp_user"] != DBNull.Value) setup.nzpuser = Convert.ToInt32(reader["nzp_user"]);
                    if (reader["dat_when"] != DBNull.Value)
                        setup.dat_when = String.Format("{0:dd.MM.yyyy HH:mm:ss}", reader["dat_when"]);
                    listsetup.Add(setup);
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                listsetup = null;
                MonitorLog.WriteLog(
                    "Ошибка заполнения справочника настройки " + (Constants.Viewerror ? "\n" + ex.Message : ""),
                    MonitorLog.typelog.Error, 20, 201, true);
            }

            return listsetup;
        }

        public Returns SaveSetup(Setup finder)
        {
            Returns ret = Utils.InitReturns();

            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return ret;
            }

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return ret;

#if PG
            ret = ExecSQL(conn_web,
                  " set search_path to 'public'"
                  , true);
#else
            ret = ExecSQL(conn_web,
                " set encryption password '" + BasePwd + "'"
                , true);
#endif
            if (!ret.result)
            {
                conn_web.Close();
                return ret;
            }

            IDataReader reader;
            ret = ExecRead(conn_web, out reader, "select nzp_setup from s_setups where nzp_param = " + finder.nzp_param,
                true);
            if (!ret.result)
            {
                conn_web.Close();
                return ret;
            }

            if (reader.Read())
#if PG
                ret = ExecSQL(conn_web, "update s_setups set value_ = " + Utils.EStrNull(finder.value) + ", " +
                    " nzp_user = " + finder.nzp_user + ", dat_when = now() where nzp_param = " + finder.nzp_param, true);
#else
                ret = ExecSQL(conn_web,
                    "update s_setups set value_ = encrypt_aes(" + Utils.EStrNull(finder.value) + "), " +
                    " nzp_user = " + finder.nzp_user + ", dat_when = current where nzp_param = " + finder.nzp_param,
                    true);
#endif
            else
#if PG
                ret = ExecSQL(conn_web, "insert into s_setups values (default, " + finder.nzp_param +
                    ", " + Utils.EStrNull(finder.param_name) + "" +
                    ", " + Utils.EStrNull(finder.value) + ", " +
                    finder.nzp_user + ", now()", true);
#else
                ret = ExecSQL(conn_web, "insert into s_setups values (0, " + finder.nzp_param +
                                        ", encrypt_aes(" + Utils.EStrNull(finder.param_name) + ")" +
                                        ", encrypt_aes(" + Utils.EStrNull(finder.value) + "), " +
                                        finder.nzp_user + ", current", true);
#endif

            reader.Close();
            conn_web.Close();

            return ret;
        }

        public SMTPSetup GetSMTPSetup(out Returns ret)
        {
            ret = Utils.InitReturns();

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;

#if PG
            ret = ExecSQL(conn_web,
                  " set search_path to 'public'"
                  , true);
#else
            ret = ExecSQL(conn_web,
                " set encryption password '" + BasePwd + "'"
                , true);
#endif
            if (!ret.result)
            {
                conn_web.Close();
                return null;
            }

            SMTPSetup smtp = new SMTPSetup();

            Setup finder = new Setup();
            finder.nzp_param = WebSetups.smtp_host;
            Setup setup = GetSetup(finder, out ret, conn_web);
            if (ret.result) smtp.host = setup.value;

            finder.nzp_param = WebSetups.smtp_port;
            setup = GetSetup(finder, out ret, conn_web);
            if (ret.result)
            {
                try
                {
                    smtp.port = Convert.ToInt32(setup.value);
                }
                catch
                {
                    smtp.port = 0;
                }
            }

            finder.nzp_param = WebSetups.smtp_user_name;
            setup = GetSetup(finder, out ret, conn_web);
            if (ret.result) smtp.userName = setup.value;

            finder.nzp_param = WebSetups.smtp_user_pwd;
            setup = GetSetup(finder, out ret, conn_web);
            if (ret.result) smtp.userPwd = setup.value;

            finder.nzp_param = WebSetups.smtp_from_name;
            setup = GetSetup(finder, out ret, conn_web);
            if (ret.result) smtp.fromName = setup.value;

            finder.nzp_param = WebSetups.smtp_from_email;
            setup = GetSetup(finder, out ret, conn_web);
            if (ret.result) smtp.fromEmail = setup.value;

            conn_web.Close();

            ret = Utils.InitReturns();

            return smtp;
        }

        public Returns ResetUserPwd(User finder)
        {
            if (finder.nzp_user < 1)
            {
                return new Returns(false, "Не определен пользователь");
            }

            if (finder.nzpuser < 1)
            {
                return new Returns(false, "Не задан пользователь, чей пароль надо сбросить", -1);
            }

            Returns ret = Utils.InitReturns();

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return ret;

#if PG
            ret = ExecSQL(conn_web,
                  " set search_path to 'public'"
                  , true);
#else
            ret = ExecSQL(conn_web,
                " set encryption password '" + BasePwd + "'"
                , true);
#endif
            if (!ret.result)
            {
                conn_web.Close();
                return ret;
            }

            IDataReader reader;
#if PG
            ret = ExecRead(conn_web, out reader, "Select email From users u Where nzp_user = " + finder.nzpuser, true);
#else
            ret = ExecRead(conn_web, out reader,
                "Select decrypt_char(email) as email From users u Where nzp_user = " + finder.nzpuser, true);
#endif
            if (!ret.result)
            {
                conn_web.Close();
                return ret;
            }

            if (!reader.Read())
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Пользователь не найден";
                reader.Close();
                conn_web.Close();
                return ret;
            }

            if (reader["email"] == DBNull.Value || ((string)reader["email"]).Trim() == "")
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Для сброса пароля у пользователя должен быть задан адрес электронной почты.";
            }
            reader.Close();
            conn_web.Close();
            return ret;
        }

        public bool IsResetUserPwd(User finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            bool isReset = false;

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return isReset;

#if PG
#else
            ret = ExecSQL(conn_web, " set encryption password '" + BasePwd + "'", true);
#endif
            if (ret.result)
            {
                IDataReader reader;
#if PG
                string a = "select case when pwd = CAST(nzp_user as text)||'-#' then 1 else 0 end as is_reset from users where login = " + Utils.EStrNull(finder.login);
                ret = ExecRead(conn_web, out reader, a, true);
#else
                ret = ExecRead(conn_web, out reader,
                    "select case when decrypt_char(pwd) = nzp_user||'-#' then 1 else 0 end as is_reset from users where login = " +
                    Utils.EStrNull(finder.login), true);
#endif
                if (ret.result)
                {
                    if (!reader.Read())
                    {
                        ret.result = false;
                        ret.tag = -1;
                        ret.text = "Пользователь не найден";
                        reader.Close();
                        conn_web.Close();
                        return isReset;
                    }
                    else
                    {
                        try
                        {
                            isReset = ((int)reader["is_reset"] == 1);
                        }
                        catch
                        {
                            ret.result = false;
                        }

                        reader.Close();
                    }
                }
            }

            conn_web.Close();
            return isReset;
        }

        public void TestDostup(out Returns ret)
        {
            ret = Utils.InitReturns();

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result)
            {
                conn_web.Close();
                return;
            }

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                conn_db.Close();
                conn_web.Close();
                return;
            }

#if PG
            IDataReader reader;
            ret = ExecRead(conn_db, out reader,
                 " Select * From " + Points.Pref + "_kernel.s_baselist Order by 1"
                 , true);
#else
            IDataReader reader;
            ret = ExecRead(conn_db, out reader,
                " Select * From s_baselist Order by 1"
                , true);
#endif
            if (!ret.result)
            {
                conn_db.Close();
                conn_web.Close();
                return;
            }

            string ret_text = "";
            try
            {
                if (reader.Read())
                {
#if PG
                    ret_text = " KERNEL. " + (string)reader["dbname"];
#else
                    ret_text = " KERNEL: " + (string)reader["dbname"];
#endif
                }
                else
                {
                    ret_text = " Данные не считываются (kernel)";
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                ret.text = ex.Message;
                ret.result = false;
                reader.Close();
                conn_db.Close();
                conn_web.Close();
                return;
            }

            ret = ExecRead(conn_db, out reader,
#if PG
 " Select * From public.s_help Order by 1"
#else
 " Select * From " + conn_web.Database + "@" + DBManager.getServer(conn_web) + ":s_help Order by 1"
#endif
, true);
            if (!ret.result)
            {
                conn_db.Close();
                conn_web.Close();
                return;
            }

            try
            {
                if (reader.Read())
                {
                    ret_text += " WEB: " + (string)reader["hlp"];
                }
                else
                {
                    ret_text += " Данные не считываются (web)";
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                reader.Close();
                ret.text = ex.Message;
                ret.result = false;
                conn_db.Close();
                conn_web.Close();
                return;
            }

            ret.text = ret_text;
            conn_db.Close();
            conn_web.Close();
        }

        public void FirstRunCreateUsers(Finder finder, out Returns ret)
        {
            ret = Utils.InitReturns();

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);

            //----------------------
#if PG
            ret = ExecSQL(conn_web,
                  " set search_path to 'public'"
                  , true);
#else
            ret = ExecSQL(conn_web,
                " set encryption password '" + BasePwd + "'"
                , true);
#endif
            if (!ret.result)
            {
                conn_web.Close();
                return;
            }

            ret = ExecSQL(conn_web, " drop table t_users_123", false);
            ret = ExecSQL(conn_web, " Select nzp_user from users into temp t_users_123 with no log ", true);


            ret = ExecSQL(conn_web,
                " delete from roleskey where nzp_role > 999 and nzp_role not in (select nzp_user from t_users_123)",
                true);
            if (!ret.result)
            {
                conn_web.Close();
                return;
            }
            ret = ExecSQL(conn_web,
                " delete from role_pages where nzp_role > 999 and nzp_role not in (select nzp_user from t_users_123)",
                true);
            if (!ret.result)
            {
                conn_web.Close();
                return;
            }
            ret = ExecSQL(conn_web,
                " delete from role_actions where nzp_role > 999 and nzp_role not in (select nzp_user from t_users_123)",
                true);
            if (!ret.result)
            {
                conn_web.Close();
                return;
            }
            ret = ExecSQL(conn_web,
                " delete from s_roles  where nzp_role > 999 and nzp_role not in (select nzp_user from t_users_123)",
                true);
            if (!ret.result)
            {
                conn_web.Close();
                return;
            }
            ret = ExecSQL(conn_web,
                " delete from userp   where nzp_user > 999 and nzp_user not in (select nzp_user from t_users_123)", true);
            if (!ret.result)
            {
                conn_web.Close();
                return;
            }
            ret = ExecSQL(conn_web,
                " delete from users   where nzp_user > 999 and nzp_user not in (select nzp_user from t_users_123)", true);
            if (!ret.result)
            {
                conn_web.Close();
                return;
            }

            //full system
#if PG
            ret = ExecSQL(conn_web, " insert into public.s_roles (nzp_role,role,page_url,sort) values (1000, 'Картотека', 0,1000)", false);
#else
            ret = ExecSQL(conn_web,
                " insert into webdb.s_roles (nzp_role,role,page_url,sort) values (1000, 'Картотека', 0,1000)", false);
#endif
            //if (!ret.result)
            //{
            //    conn_web.Close();
            //    return;
            //}
#if PG
            ret = ExecSQL(conn_web,
                " insert into public.s_roles (nzp_role,role,page_url,sort) " +
                " select distinct CAST((nzp_area + 1000) as text), 'УК'||' '||CAST((nzp_area + 1000) as text), 0, nzp_area + 1000 " +
#else
            ret = ExecSQL(conn_web,
                " insert into webdb.s_roles (nzp_role,role,page_url,sort) " +
                " select unique nzp_area + 1000, 'УК'||' '||nzp_area + 1000, 0, nzp_area + 1000 " +
#endif
 " from anl" + Points.CalcMonth.year_ +
                " where nzp_area > 0 and nzp_area+1000 not in (select nzp_user from t_users_123)", true);
            if (!ret.result)
            {
                conn_web.Close();
                return;
            }
#if PG
            ret = ExecSQL(conn_web,
                " insert into public.users (nzp_user,login,pwd,uname) " +
                " select distinct  CAST((nzp_area + 1000) as text),'uk'|| CAST((nzp_area + 1000) as text),'ukw'|| CAST((nzp_area + 1000) as text),'uk'|| CAST((nzp_area + 1000) as text) " +
                " from anl" + Points.CalcMonth.year_ + " where nzp_area > 0 and nzp_area+1000 not in (select nzp_user from t_users_123)", true);
#else
            ret = ExecSQL(conn_web,
                " insert into webdb.users (nzp_user,login,pwd,uname) " +
                " select unique nzp_area + 1000,'uk'||nzp_area + 1000,'ukw'||nzp_area + 1000,'uk'||nzp_area + 1000 " +
                " from anl" + Points.CalcMonth.year_ +
                " where nzp_area > 0 and nzp_area+1000 not in (select nzp_user from t_users_123)", true);
#endif
            if (!ret.result)
            {
                conn_web.Close();
                return;
            }
            ret = ExecSQL(conn_web,
#if PG
 " insert into public.s_roles (nzp_role,role,page_url,sort)" +
                " select distinct nzp_supp + 10000, 'Поставщик'||' '||nzp_supp, 0, nzp_supp + 10000 " +
#else
 " insert into webdb.s_roles (nzp_role,role,page_url,sort)" +
                " select unique nzp_supp + 10000, 'Поставщик'||' '||nzp_supp, 0, nzp_supp + 10000 " +
#endif
 " from anl" + Points.CalcMonth.year_ +
                " where nzp_supp > 0 and nzp_supp+10000 not in (select nzp_user from t_users_123)", true);
            if (!ret.result)
            {
                conn_web.Close();
                return;
            }
#if PG
            ret = ExecSQL(conn_web,
                " insert into public.users (nzp_user,login,pwd,uname) " +
                " select distinct nzp_supp + 10000,'pu'||nzp_supp + 10000,'puw'||nzp_supp + 10000,'pu'||nzp_supp + 10000 " +
                " from anl" + Points.CalcMonth.year_ + " where nzp_supp > 0 and nzp_supp+10000 not in (select nzp_user from t_users_123)", true);
#else
            ret = ExecSQL(conn_web,
                " insert into webdb.users (nzp_user,login,pwd,uname) " +
                " select unique nzp_supp + 10000,'pu'||nzp_supp + 10000,'puw'||nzp_supp + 10000,'pu'||nzp_supp + 10000 " +
                " from anl" + Points.CalcMonth.year_ +
                " where nzp_supp > 0 and nzp_supp+10000 not in (select nzp_user from t_users_123)", true);
#endif
            if (!ret.result)
            {
                conn_web.Close();
                return;
            }
            ret = ExecSQL(conn_web,
                " update users " +
                " set uname = ( select max(area) from anl" + Points.CalcMonth.year_ +
                "_supp where nzp_user = nzp_area + 1000 and area is not null) " +
                " where nzp_user > 999 and nzp_user in ( select nzp_area + 1000 from anl" + Points.CalcMonth.year_ +
                "_supp where area is not null) " +
                " and nzp_user not in (select nzp_user from t_users_123) "
                , true);
            if (!ret.result)
            {
                conn_web.Close();
                return;
            }
            ret = ExecSQL(conn_web,
                " update s_roles " +
                " set role = ( select max(area) from anl" + Points.CalcMonth.year_ +
                "_supp where nzp_role = nzp_area + 1000 and area is not null )" +
                " where nzp_role > 999 and nzp_role in ( select nzp_area + 1000 from anl" + Points.CalcMonth.year_ +
                "_supp where area is not null) " +
                " and nzp_role not in (select nzp_user from t_users_123) "
                , true);
            if (!ret.result)
            {
                conn_web.Close();
                return;
            }

            ret = ExecSQL(conn_web,
                " update users " +
                " set uname = ( select max(name_supp) from anl" + Points.CalcMonth.year_ +
                "_supp where nzp_user = nzp_supp + 10000 and name_supp is not null) " +
                " where nzp_user > 999 and nzp_user in ( select nzp_supp + 10000 from anl" + Points.CalcMonth.year_ +
                "_supp where name_supp is not null) " +
                " and nzp_user not in (select nzp_user from t_users_123) "
                , true);
            if (!ret.result)
            {
                conn_web.Close();
                return;
            }
            ret = ExecSQL(conn_web,
                " update s_roles " +
                " set role = ( select max(name_supp) from anl" + Points.CalcMonth.year_ +
                "_supp where nzp_role = nzp_supp + 10000 and name_supp is not null ) " +
                " where nzp_role > 999 and nzp_role in ( select nzp_supp + 10000 from anl" + Points.CalcMonth.year_ +
                "_supp where name_supp is not null) " +
                " and nzp_role not in (select nzp_user from t_users_123) "
                , true);
            if (!ret.result)
            {
                conn_web.Close();
                return;
            }
            ret = ExecSQL(conn_web,
#if PG
 " update s_roles set role   = role where nzp_role > 999  and nzp_role not in (select nzp_user from t_users_123) "
#else
 " update s_roles set role   = encrypt_aes(role) where nzp_role > 999  and nzp_role not in (select nzp_user from t_users_123) "
#endif
, true);
            if (!ret.result)
            {
                conn_web.Close();
                return;
            }
            ret = ExecSQL(conn_web,
#if PG
 " update users set pwd   = nzp_user||'-'||pwd, uname = uname where nzp_user > 999  and nzp_user not in (select nzp_user from t_users_123) "
#else
 " update users set pwd   = encrypt_aes(nzp_user||'-'||pwd), uname = encrypt_aes(uname) where nzp_user > 999  and nzp_user not in (select nzp_user from t_users_123) "
#endif
, true);
            if (!ret.result)
            {
                conn_web.Close();
                return;
            }
            ret = ExecSQL(conn_web,
#if PG
 " update public.role_pages set sign = CAST(nzp_role as text)||nzp_page||'-'||id||'role_pages' where 1 = 1 " +
#else
 " update webdb.role_pages set sign = encrypt_aes(nzp_role||nzp_page||'-'||id||'role_pages') where 1 = 1 " +
#endif
 " and nzp_role not in (select nzp_user from t_users_123) "
                , true);
            if (!ret.result)
            {
                conn_web.Close();
                return;
            }

            ret = ExecSQL(conn_web,
#if PG
 " update public.role_actions set sign = nzp_role||CAST(nzp_page as text)||nzp_act||'-'||id||'role_actions' where 1 = 1 " +
#else
 " update webdb.role_actions set sign = encrypt_aes(nzp_role||nzp_page||nzp_act||'-'||id||'role_actions') where 1 = 1 " +
#endif
 " and nzp_role not in (select nzp_user from t_users_123) "
                , true);
            if (!ret.result)
            {
                conn_web.Close();
                return;
            }

            //доступы по пользователям
            ret = ExecSQL(conn_web,
#if PG
 " insert into public.userp (nzp_role, nzp_user, sign) select nzp_role,nzp_user,'' " +
#else
 " insert into webdb.userp (nzp_role, nzp_user, sign) select nzp_role,nzp_user,'' " +
#endif
 " from users a, s_roles b where nzp_role > 999 and nzp_role = nzp_user  " +
                " and nzp_role not in (select nzp_user from t_users_123) "
                , true);
            if (!ret.result)
            {
                conn_web.Close();
                return;
            }
            ret = ExecSQL(conn_web,
#if PG
 " insert into public.userp (nzp_role, nzp_user, sign) select nzp_role,nzp_user,'' " +
#else
 " insert into webdb.userp (nzp_role, nzp_user, sign) select nzp_role,nzp_user,'' " +
#endif
 " from users a, s_roles b where nzp_user > 999 and nzp_role = 11 and nzp_user not in (select nzp_user from t_users_123)"
                , true);
            if (!ret.result)
            {
                conn_web.Close();
                return;
            }
            ret = ExecSQL(conn_web,
#if PG
 " update public.userp set sign = CAST(nzp_user as text)||nzp_role||'-'||nzp_usp||'userp' where nzp_user > 999 " +
#else
 " update webdb.userp set sign = encrypt_aes(nzp_user||nzp_role||'-'||nzp_usp||'userp') where nzp_user > 999 " +
#endif
 " and nzp_user not in (select nzp_user from t_users_123) "
                , true);
            if (!ret.result)
            {
                conn_web.Close();
                return;
            }
            ret = ExecSQL(conn_web,
                " delete from roleskey where nzp_role > 999" +
                " and nzp_role not in (select nzp_user from t_users_123) "
                , true);
            if (!ret.result)
            {
                conn_web.Close();
                return;
            }
            //УК nzp_wp (101)
            ret = ExecSQL(conn_web,
#if PG
 " insert into roleskey (nzp_role,tip,kod) select distinct nzp_area + 1000, 101, nzp_wp from anl" + Points.CalcMonth.year_ +
#else
 " insert into roleskey (nzp_role,tip,kod) select unique nzp_area + 1000, 101, nzp_wp from anl" +
                Points.CalcMonth.year_ +
#endif
 " where nzp_area + 1000 not in (select nzp_user from t_users_123) "
                , true);
            if (!ret.result)
            {
                conn_web.Close();
                return;
            }
            //УК nzp_area (102)
            ret = ExecSQL(conn_web,
#if PG
 " insert into roleskey (nzp_role,tip,kod) select distinct nzp_area + 1000, 102, nzp_area from anl" + Points.CalcMonth.year_ +
#else
 " insert into roleskey (nzp_role,tip,kod) select unique nzp_area + 1000, 102, nzp_area from anl" +
                Points.CalcMonth.year_ +
#endif
 " where nzp_area + 1000 not in (select nzp_user from t_users_123) "
                , true);
            if (!ret.result)
            {
                conn_web.Close();
                return;
            }
            //УК nzp_geu (103)
            ret = ExecSQL(conn_web,
#if PG
 " insert into roleskey (nzp_role,tip,kod) select distinct nzp_area + 1000, 103, nzp_geu from anl" + Points.CalcMonth.year_ +
#else
 " insert into roleskey (nzp_role,tip,kod) select unique nzp_area + 1000, 103, nzp_geu from anl" +
                Points.CalcMonth.year_ +
#endif
 " where nzp_area + 1000 not in (select nzp_user from t_users_123) "
                , true);
            if (!ret.result)
            {
                conn_web.Close();
                return;
            }

            //Поставщик nzp_wp (101)
            ret = ExecSQL(conn_web,
#if PG
 " insert into roleskey (nzp_role,tip,kod) select distinct nzp_supp + 10000, 101, nzp_wp from anl" + Points.CalcMonth.year_ +
#else
 " insert into roleskey (nzp_role,tip,kod) select unique nzp_supp + 10000, 101, nzp_wp from anl" +
                Points.CalcMonth.year_ +
#endif
 " where nzp_supp + 10000 not in (select nzp_user from t_users_123) "
                , true);
            if (!ret.result)
            {
                conn_web.Close();
                return;
            }
            //Поставщик nzp_supp (120)
            ret = ExecSQL(conn_web,
#if PG
 " insert into roleskey (nzp_role,tip,kod) select distinct nzp_supp + 10000, 120, nzp_supp from anl" + Points.CalcMonth.year_ +
#else
 " insert into roleskey (nzp_role,tip,kod) select unique nzp_supp + 10000, 120, nzp_supp from anl" +
                Points.CalcMonth.year_ +
#endif
 " where nzp_supp + 10000 not in (select nzp_user from t_users_123) "

                , true);
            if (!ret.result)
            {
                conn_web.Close();
                return;
            }
            //Поставщик nzp_serv (121)
            ret = ExecSQL(conn_web,
#if PG
 " insert into roleskey (nzp_role,tip,kod) select distinct nzp_supp + 10000, 121, nzp_serv from anl" + Points.CalcMonth.year_ +
#else
 " insert into roleskey (nzp_role,tip,kod) select unique nzp_supp + 10000, 121, nzp_serv from anl" +
                Points.CalcMonth.year_ +
#endif
 " where nzp_supp + 10000 not in (select nzp_user from t_users_123) "
                , true);
            if (!ret.result)
            {
                conn_web.Close();
                return;
            }

            ////пока добавим full system всем
            //ExecSQL(conn_web, " drop table ttt ", false);

            //ret = ExecSQL(conn_web,
            //    " select unique tip,kod from roleskey into temp ttt "
            //    , true);
            //if (!ret.result)
            //{
            //    conn_web.Close();
            //    return;
            //}
            //ret = ExecSQL(conn_web,
            //    " insert into roleskey (nzp_role,tip,kod) select 1000, tip,kod from ttt "
            //    , true);
            //if (!ret.result)
            //{
            //    conn_web.Close();
            //    return;
            //}

            //ExecSQL(conn_web, " drop table ttt ", false);

            ret = ExecSQL(conn_web,
#if PG
 " update public.roleskey set sign = tip||CAST(kod as text)||nzp_role||'-'||nzp_rlsv||'roles') " +
#else
 " update webdb.roleskey set sign = encrypt_aes(tip||kod||nzp_role||'-'||nzp_rlsv||'roles') " +
#endif
 " where nzp_role not in (select nzp_user from t_users_123) "
                , true);
            if (!ret.result)
            {
                conn_web.Close();
                return;
            }

            /*
            ret = ExecSQL(conn_web,
                " delete from roles where 1 = 0 " +
                " or ( nzp_role = 11 and tip=2 and kod in (65) ) " +
                " or ( cur_page in (12,13,14,15,17, 32,34,36, 43,44,45,46,47, 60, 125, 949,951,952, 53,62,63, 128) ) " +
                " or ( tip=1 and kod in (12,13,14,15,17, 32,34,36, 43,44,45,46,47, 52,56,60, 125, 949,951,952, 53,62,63, 128) ) " +
                " or ( tip=2 and kod in (4,6,51,61,62,62,63,64, 32,33,34,36, 502,504,506, 7) and cur_page < 151 ) " +
                " or ( cur_page=31 and tip=1 and kod in (51,54,56) ) " +
                " or ( cur_page=31 and tip=2 and kod in (502,504,506) ) " +
                " or ( cur_page=35 and tip=1 and kod in (51,54) ) "
                , true);
            if (!ret.result)
            {
                conn_web.Close();
                return;
            }
            */

            //----------------------
            conn_web.Close();
        }

        /// <summary>
        /// Получить список серверов, доступных для добавления к роли
        /// </summary>
        public List<_RServer> ServersAvailableForRole(Role finder, out Returns ret)
        {
            ret = Utils.InitReturns();

            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return null;
            }

            if (finder.nzp_role < 1)
            {
                ret.result = false;
                ret.text = "Не определена роль";
                return null;
            }

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;

            if (!TableInWebCashe(conn_web, "servers"))
            {
                ret = new Returns(false, "Справочник серверов не загружен", -1);
                return null;
            }

            if (!TableInWebCashe(conn_web, "s_rcentr"))
            {
                ret = new Returns(false, "Справочник расчетных центров не загружен", -1);
                return null;
            }

            string sql =
                "Select nzp_server, rc.rcentr from servers srvr, outer s_rcentr rc where srvr.nzp_rc = rc.nzp_rc" +
                " and srvr.nzp_server not in (select kod from roleskey r where r.nzp_role = " + finder.nzp_role +
                " and r.tip = " + Constants.role_sql_server +
#if PG
 " and r.tip||CAST(r.kod as text)||r.nzp_role||'-'||r.nzp_rlsv||'roles' = r.sign) ";
#else
 " and r.tip||r.kod||r.nzp_role||'-'||r.nzp_rlsv||'roles' = decrypt_char(r.sign)) ";
#endif

            //выбрать список
            IDataReader reader;
            ret = ExecRead(conn_web, out reader, sql, true);
            if (!ret.result)
            {
                conn_web.Close();
                return null;
            }

            List<_RServer> list = new List<_RServer>();
            try
            {
                while (reader.Read())
                {
                    _RServer zap = new _RServer();

                    if (reader["nzp_server"] != DBNull.Value) zap.nzp_server = Convert.ToInt32(reader["nzp_server"]);
                    if (reader["rcentr"] != DBNull.Value) zap.rcentr = Convert.ToString(reader["rcentr"]).Trim();

                    list.Add(zap);
                }
                ret.tag = list.Count;
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                list = null;
                MonitorLog.WriteLog(
                    "Ошибка заполнения справочника банков данных " + (Constants.Viewerror ? "\n" + ex.Message : ""),
                    MonitorLog.typelog.Error, 20, 201, true);
            }
            if (reader != null) reader.Close();
            conn_web.Close();
            return list;
        }

        public Returns DropCacheTables(User finder)
        {
            IDbConnection connectionID = GetConnection(Constants.cons_Webdata);
            Returns ret = OpenDb(connectionID, true);
            if (!ret.result) return ret;

#if PG
            string sql = "select table_name as tabname from information_schema.tables where table_schema = 'public' and table_name like 't\\_%'";
#else
            string sql = "Select tabname From systables Where lower(tabname) matches 't*_*'";
#endif
            IDataReader reader;
            ret = ExecRead(connectionID, out reader, sql, true);
            if (!ret.result)
            {
                connectionID.Close();
                return ret;
            }
            while (reader.Read())
            {
                if (reader["tabname"] != DBNull.Value)
                {
                    sql = "drop table " + Convert.ToString(reader["tabname"]).Trim();
                    ExecSQL(connectionID, sql, true);
                }
            }
            return ret;
        }

        /// <summary>
        /// Возвращает список страниц из заданного перечня, доступных в заданной роли
        /// </summary>
        /// <param name="finder">Обязательны: nzp_user, nzp_role</param>
        /// <param name="pages">Обязательный параметр. список кодов страниц через запятую (например: 92,185)</param>
        /// <param name="ret">Результат выполнения функции</param>
        /// <returns>Список доступных кодов страниц</returns>
        public List<int> CheckPagePermission(Finder finder, string pages, out Returns ret)
        {
            if (pages == "")
            {
                ret = new Returns(false, "Не заданы страницы");
                return null;
            }

            ret = Utils.InitReturns();

            string sql = "Select distinct nzp_page from t" + finder.nzp_user + "_role_pages where nzp_role = " +
                         finder.nzp_role + " and nzp_page in (" + pages + ")";

            IDbConnection connectionID = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(connectionID, true);
            if (!ret.result) return null;

            MyDataReader reader = null;
            List<int> list = null;
            try
            {
                ret = ExecRead(connectionID, out reader, sql, true);
                if (ret.result)
                {
                    list = new List<int>();
                    while (reader.Read())
                    {
                        if (reader["nzp_page"] != DBNull.Value)
                        {
                            list.Add(Convert.ToInt32(reader["nzp_page"]));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                list = null;
                MonitorLog.WriteLog("Ошибка в функции CheckPagePermission\n" + ex.ToString(), MonitorLog.typelog.Error,
                    20, 201, true);
            }
            finally
            {
                if (reader != null) reader.Close();
                connectionID.Close();
            }

            return list;
        }

        public Returns SaveUserSessionId(BaseUser finder)
        {
            if (finder.nzp_user < 1)
            {
                return new Returns(false, "Не определен пользователь");
            }

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            Returns ret = OpenDb(conn_web, true);
            if (!ret.result) return ret;

            ret = ExecSQL(conn_web, "update log_sessions" +
#if PG
 " Set (session_id, left_on) = " + (finder.sessionId.Trim() == "" ? "(null, null)" : "(" + Utils.EStrNull(finder.sessionId.Trim()) + ", now())") +
#else
 " Set (session_id, left_on) = " +
                                    (finder.sessionId.Trim() == ""
                                        ? "(null, null)"
                                        : "(" + Utils.EStrNull(finder.sessionId.Trim()) + ", current)") +
#endif
 " Where nzp_user = " + finder.nzp_user, true);

            conn_web.Close();
            return ret;
        }

        public Returns UploadExchangeSZ(Finder finder, DataTable dt)
        {
            if (dt == null) return new Returns(false, "Таблица не задана");
            if (finder.nzp_user < 1) return new Returns(false, "Пользователь не задан");

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            Returns ret = OpenDb(conn_web, true);
            if (!ret.result) return ret;

            DBUtils db = new DBUtils();
            ret = db.SaveDataTable(conn_web, finder, "exchange_sz", dt);
            db.Close();

            conn_web.Close();

            return ret;
        }

        /// <summary>
        /// Возвращает список задач
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public List<Job> GetJobs(Finder finder, out Returns ret)
        {
            ret = Utils.InitReturns();

            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return null;
            }

            var conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;

            var sql = new StringBuilder();
            sql.Append(" SELECT COUNT(*) FROM ");
#if PG
            sql.Append("public.jobs");
#else
            sql.Append(" jobs ");
#endif
            try
            {
                ret.tag = Convert.ToInt32(ExecScalar(conn_web, sql.ToString(), out ret, true));
                if (!ret.result) return null;

#if PG
                sql = new StringBuilder(" SELECT * FROM ");
                sql.Append("public.jobs");
#else
                sql = new StringBuilder("SELECT ");
                if (finder.rows > 0) sql = sql.Append(string.Format(" LIMIT {0} OFFSET {1}", finder.rows, finder.skip));
                sql.Append(" FROM jobs ");
#endif
                sql.Append(" ORDER BY create_date DESC ");
#if PG
                if (finder.rows > 0) sql.Append(string.Format(" LIMIT {0} OFFSET {1}", finder.rows, finder.skip));
#endif
                var res = ClassDBUtils.OpenSQL(sql.ToString(), conn_web);
                if (res.resultCode != 0)
                {
                    ret.text = res.resultMessage;
                    ret.result = false;
                    return null;
                }
                var list = new List<Job>();
                foreach (DataRow x in res.GetData().Rows)
                {
                    var job = new Job()
                    {
                        id = Convert.ToInt32(x["id"]),
                        job_state = Convert.ToInt32(x["job_state"]),
                        job_type = Convert.ToInt32(x["job_type"]),
                        job_code = Convert.ToString(x["job_code"]),
                        job_name = Convert.ToString(x["job_name"]),
                        data = x["data"] != DBNull.Value ? Convert.ToString(x["data"]) : "",
                        create_date = Convert.ToDateTime(x["create_date"]),
                        success = DataConvert.FieldValue(x, "success", false),
                        message = DataConvert.FieldValue(x, "message", ""),
                        heart_beat = Convert.ToDateTime(x["heart_beat"])
                    };
                    if (x["start_date"] != DBNull.Value) job.start_date = Convert.ToDateTime(x["start_date"]);
                    if (x["end_date"] != DBNull.Value) job.end_date = Convert.ToDateTime(x["end_date"]);
                    list.Add(job);
                }
                return list;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка получения списка заданий.";
                MonitorLog.WriteException("Ошибка получения списка заданий.", ex);
                return null;
            }
        }

        public bool RegisterUserAction(int nzpUser, int nzpPage, string pageName, int nzpAct, string actName)
        {
            var connection = GetConnection(Constants.cons_Webdata);
            Returns ret = OpenDb(connection, true);
            if (!ret.result) return false;

            ret = ExecSQL(connection,
                "insert into user_actions (nzp_user, nzp_page, page_name, nzp_act, act_name, changed_on)" +
                " values (" + nzpUser + ", " + nzpPage + "," + Utils.EStrNull(pageName) + "," + nzpAct + "," +
                Utils.EStrNull(actName) + "," + sCurDateTime + ")"
                , true);

            connection.Close();

            return ret.result;
        }

        /// <summary>
        /// Проверка завершенности задачи
        /// </summary>
        /// <param name="jobId"></param>
        /// <returns></returns>
        public Returns CheckJobEnd(int jobId)
        {
            Returns ret;
            using (var conn = GetConnection(Constants.cons_Webdata))
            {
                ret = OpenDb(conn, true);
                if (!ret.result) return ret;

#if PG
                var endDate = ExecScalar(conn, string.Format("SELECT end_date FROM public.jobs WHERE id = {0}", jobId), out ret, true);
#else
                var endDate = ExecScalar(conn, string.Format("SELECT end_date FROM jobs WHERE id = {0}", jobId), out ret, true);
#endif
                if (endDate == DBNull.Value) return ret;
                ret.result = false;
                ret.text = "Задача уже завершена!";
            }
            return ret;
        }

        /// <summary>
        /// Получение списка логов на вебе
        /// </summary>
        /// <returns></returns>
        public LogsTree GetWebLogsList(LogsTree finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                var res = new LogsTree();

                #region Читаем список логов хоста
                if (Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + @"\Logs"))
                {
                    var dirs = Directory.GetDirectories(AppDomain.CurrentDomain.BaseDirectory + @"\Logs");
                    foreach (var dir in dirs)
                    {
                        DateTime curDate;
                        var parseRes = DateTime.TryParse(dir.Split('\\').Last(), out curDate);
                        if (parseRes == false)
                            continue;

                        var year = curDate.Year;
                        var month = curDate.Month;
                        var day = curDate.Day;

                        //Если в словаре еще нет указанного года, добавляем
                        if (!res.childs.Keys.Contains(year))
                            res.childs.Add(year, new LogsTree() { log_name = year.ToString() });

                        //Если в словаре еще нет указанного месяца, добавляем
                        if (!res.childs[year].childs.Keys.Contains(month))
                            res.childs[year].childs.Add(month, new LogsTree() { log_name = curDate.ToString("MMMM") });

                        //Добавляем день в словарь
                        res.childs[year].childs[month].childs.Add(day, new LogsTree() { log_name = day + " (Web)" });
                    }
                }
                #endregion Читаем список логов хоста

                return res;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка функции GetHostLogsList" + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                ret = new Returns(false, ex.Message);
                return new LogsTree();
            }
        }

        /// <summary>
        /// Получение файла логов c веба
        /// </summary>
        /// <returns></returns>
        public LogsTree GetWebLogsFile(LogsTree finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            var res = new LogsTree();
            var logsDir = AppDomain.CurrentDomain.BaseDirectory + @"Logs\";
            try
            {
                if (Directory.Exists(logsDir))
                {
                    //получаем список папок с логами
                    var dirs = Directory.GetDirectories(logsDir);

                    foreach (var dir in dirs)
                    {
                        DateTime curDate;
                        var parseRes = DateTime.TryParse(dir.Split('\\').Last(), out curDate);
                        //если не является логом
                        if (parseRes == false)
                            continue;

                        var year = curDate.Year;
                        var month = curDate.Month;
                        var day = curDate.Day;

                        //если дата совпадает с той, что пришла в файндере
                        if (year == finder.year && month == finder.month && day == finder.day)
                        {
                            // архивируем логи веба
                            var webLogName = "web_" + DateTime.Now.Ticks + ".7z";
                            var arch = new Utility.Archive();
                            var result = arch.CompressDirectory(dir, Path.Combine(Constants.Directories.ImportAbsoluteDir.Replace("/", "\\"), webLogName), false);

                            //отсылаем их на фтп
                            if (InputOutput.useFtp)
                                InputOutput.SaveOutputFile(Path.Combine(Constants.Directories.ImportAbsoluteDir.Replace("/", "\\"), webLogName));

                            res.has_web_logs = true;
                            res.web_log_name = webLogName;

                            break;
                        }
                    }
                }

                return res;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка функции DbGetWebLogsFile" + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                ret = new Returns(false, ex.Message);
                return new LogsTree();
            }
        }
    }
}
