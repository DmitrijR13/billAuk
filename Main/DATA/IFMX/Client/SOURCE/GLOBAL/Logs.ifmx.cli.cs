using System;
using System.Data;
using System.Configuration;
using System.Linq;
using System.Xml.Linq;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.DataBase
{
    public partial class DbLogClient : DataBaseHead
    {
        //----------------------------------------------------------------------
        public bool LogAcc(BaseUser bu, int acc_kod ) //log_access
        //----------------------------------------------------------------------
        {
            IDbConnection connectionID = GetConnection(Constants.cons_Webdata);
            if (!OpenDb(connectionID, true).result) return false;

            /*
            if (!ExecSQL(connectionID,
                  " set encryption password '" + BasePwd + "'"
                  , true).result)
            {
                connectionID.Close();
                return false;
            }
            */
               
            bool ret = ExecSQL(connectionID,
                      " Insert into log_access (nzp_lacc, nzp_user, acc_kod, dat_log, ip_log, browser, login, pwd, idses) " +
#if PG
                        " Values (default," + bu.nzp_user + "," + acc_kod + ",now()," +
#else
                      " Values (0," + bu.nzp_user + "," + acc_kod + ",current," +
#endif
                                      Utils.EStrNull(bu.ip_log)  + "," +
                                      Utils.EStrNull(bu.browser) + "," +
                                      Utils.EStrNull(bu.login)   + "," +
                                      Utils.EStrNull((acc_kod == Constants.acc_failure) ? bu.password : "xxx")      + "," +
                                      Utils.EStrNull(bu.idses)   + ")"
                        , true).result;

            if (ret)
            {
                /*
                if (!TableInWebCashe(connectionID, "log_sessions"))
                {
                    if (!CreateLogSessions(connectionID))
                    {
                        connectionID.Close();
                        return false;
                    }
                }
                */

                //аутентификация неудачна
                if (acc_kod == Constants.acc_failure) return false;                               

                if (acc_kod == Constants.acc_in)
                {
                    //зафиксировать сессию
                    ret = ExecSQL(connectionID,
                      " Insert into log_sessions (nzp_user, dat_log, ip_log, browser, idses) " +
#if PG
                      " Values (  " + bu.nzp_user + ", now(), " +
#else
                      " Values (  " + bu.nzp_user + ", current, " +
#endif
                                      Utils.EStrNull(bu.ip_log)   + "," +
                                      Utils.EStrNull(bu.browser)  + "," +
                                      Utils.EStrNull(bu.idses)    + ")"
                        , true).result;

                    if (!Connections.IsAllowedOneUserHasSeveralSessions)
                    {
                        // удалить другие активные сессии этого пользователя
                        // этим не допускается одновременная работа двух и более пользователей под одним логином
#if PG
                        ret = ExecSQL(connectionID,
                                                " Delete From log_sessions Where nzp_user = " + bu.nzp_user + " and idses <> " + Utils.EStrNull(bu.idses)
                                                  , true).result;
#else
  ret = ExecSQL(connectionID,
                          " Delete From log_sessions Where nzp_user = " + bu.nzp_user + " and idses <> " + Utils.EStrNull(bu.idses)
                            , true).result;
#endif
                    }
                }
                else
                { //удалить сессию
#if PG
                    ret = ExecSQL(connectionID,
                                         " Delete From log_sessions Where nzp_user = " + bu.nzp_user + " and idses = " + Utils.EStrNull(bu.idses)
                                           , true).result;
#else
 ret = ExecSQL(connectionID,
                      " Delete From log_sessions Where nzp_user = " + bu.nzp_user + " and idses = " + Utils.EStrNull(bu.idses)
                        , true).result;
#endif
                }
            }

            connectionID.Close();
            return ret;
        }//LogAcc
        /*
        //----------------------------------------------------------------------
        bool CreateLogSessions(IfxConnection connectionID) //
        //----------------------------------------------------------------------
        {
        }
        */
        //----------------------------------------------------------------------
        public bool LogAccLast(int nzp_user, out BaseUser bu) //последний вход
        //----------------------------------------------------------------------
        {
            bu = new BaseUser(); 
            bu.nzp_user = nzp_user;

            IDbConnection connectionID = GetConnection(Constants.cons_Webdata);
            if (!OpenDb(connectionID, true).result) return false;

#if PG
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
            if (!ExecRead(connectionID, out reader,
                " Select decrypt_char(uname) as uname,l.dat_log,l.ip_log " +
                " From users u left outer join log_access l on u.nzp_user = l.nzp_user " +
                " Where u.nzp_user = " + bu.nzp_user.ToString() +
                "   and acc_kod = 1 " +
                " Order by l.dat_log desc "
                , true).result
               )
#else
            if (!ExecRead(connectionID, out reader,
                " Select decrypt_char(uname) as uname,l.dat_log,l.ip_log " +
                " From users u, outer log_access l "+
                " Where u.nzp_user = l.nzp_user "+
                "   and u.nzp_user = " + bu.nzp_user.ToString()+
                "   and acc_kod = 1 "+
                " Order by l.dat_log desc "
                , true).result
               )
#endif
            {
                connectionID.Close();
                return false;
            }
            if (!reader.Read())
            {
                connectionID.Close();
                return false;
            }

            bu.uname   = ((string)reader["uname"]).Trim();

            if (reader.Read())
            {

                if (reader["dat_log"] != DBNull.Value)
                {
                    bu.ip_log = ((string)reader["ip_log"]).Trim();
                    bu.dat_log = ((DateTime)reader["dat_log"]).ToString();
                }
            }
            else
                bu.ip_log = "newlog";



            connectionID.Close();
            return true;
        }//LogAcc

        //----------------------------------------------------------------------
        public bool LogSQL(string connString, int nzp_user, int err_kod, string sql_txt, string sql_err) //log_sql
        //----------------------------------------------------------------------
        {
            //пока уберем 
            return true;
            /*
            IfxConnection connectionID = GetConnection(Constants.cons_Webdata);
            if (!OpenDb(connectionID, true).result) return false;

            bool ret = ExecSQL(connectionID,
                      " Insert into log_sql (nzp_lsql, nzp_user, dat_log, err_kod, sql_txt, sql_err) "+
                      " Values (0," + Convert.ToString(nzp_user)+ "," +
                                        " current," +
                                      Convert.ToString(err_kod) + "," +
                                      Utils.EStrNull(sql_txt)   + "," +
                                      Utils.EStrNull(sql_err)   + ")"
                        , true).result;
            connectionID.Close();
            return ret;
            */
        }//LogSQL

        //----------------------------------------------------------------------
        public bool LogHis(int nzp_user, LogHis lh) //log_history
        //----------------------------------------------------------------------
        {
            IDbConnection connectionID = GetConnection(Constants.cons_Webdata);
            if (!OpenDb(connectionID, true).result) return false;

#if PG
            bool ret = ExecSQL(connectionID,
                      " Insert into log_history (nzp_lhis, nzp_user, nzp_page, dat_log, idses, kod1, kod2, kod3) " +
                      " Values (0," + Convert.ToString(nzp_user) + "," +
                                      Convert.ToString(lh.nzp_page) + "," +
                                        " now()," +
                                      Utils.EStrNull(lh.idses) + "," +
                                      Convert.ToString(lh.kod1) + "," +
                                      Convert.ToString(lh.kod2) + "," +
                                      Convert.ToString(lh.kod3) + ")"
                        , true).result;
#else
            bool ret = ExecSQL(connectionID,
                      " Insert into log_history (nzp_lhis, nzp_user, nzp_page, dat_log, idses, kod1, kod2, kod3) " +
                      " Values (0," + Convert.ToString(nzp_user) + "," +
                                      Convert.ToString(lh.nzp_page) + "," +
                                        " current," +
                                      Utils.EStrNull(lh.idses) + "," +
                                      Convert.ToString(lh.kod1) + "," +
                                      Convert.ToString(lh.kod2) + "," +
                                      Convert.ToString(lh.kod3) + ")"
                        , true).result;
#endif
            connectionID.Close();
            return ret;
        }
    }
}
