using System.Data;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using System;
using System.Collections.Generic;


namespace STCLINE.KP50.DataBase
{
    public class DbWorkUserClient : DataBaseHeadClient
    {
        public Returns GetWebUser(Finder finder)
        {
#if PG
            Returns ret = ExecSQL(" set search_path to 'public'");
#else
            Returns ret = ExecSQL(" set encryption password '" + BasePwd + "'");
#endif
            if (!ret.result)
            {
                return ret;
            }

            MyDataReader reader;
            string sql = "select login, decrypt_char(uname) as uname from users where nzp_user = " + finder.nzp_user;
            ret = ExecRead(out reader, sql);
            if (!ret.result)
            {
                return ret;
            }
            try
            {
                if (reader.Read())
                {
                    if (reader["login"] != DBNull.Value) finder.webLogin = Convert.ToString(reader["login"]).Trim();
                    if (reader["uname"] != DBNull.Value) finder.webUname = Convert.ToString(reader["uname"]).Trim();
                }
            }
            finally
            {
                reader.Close();
            }
            
            if (finder.webLogin == "")
            {
                ret.result = false;
                ret.text = "Не найден web-пользователь";
            }
            return ret;
        }

        public void SaveWebUser(List<User> users, out Returns ret)
        {
#if PG
            ret = ExecSQL(" set search_path to 'public'");
#else
            ret = ExecSQL(" set encryption password '" + BasePwd + "'");
#endif
            if (!ret.result) return;

            foreach (User user in users)
            {
#if PG
                ret = ExecSQL(" Update users " +
                    " Set login = '" + user.login + "'" +
                       ", email = '" + user.email + "'" +
                       ", uname = '" + user.uname + "'" +
                       ", pwd   = " + Utils.EStrNull(Utils.CreateMD5StringHash(user.pwd + user.nzpuser + BasePwd)) +
                    " WHERE nzp_user = " + user.nzp_user);
#else
                ret = ExecSQL(" Update users " +
                    " Set login = '" + user.login + "'" +
                       ", email = encrypt_aes('" + user.email + "')" +
                       ", uname = encrypt_aes('" + user.uname + "')" +
                       ", pwd   = encrypt_aes(nzp_user||'-'||'" + user.pwd + "') " +
                    " Where nzp_user = " + user.nzp_user);
#endif
                if (!ret.result)
                {
                    return;
                }
            }
        }
    }
}
