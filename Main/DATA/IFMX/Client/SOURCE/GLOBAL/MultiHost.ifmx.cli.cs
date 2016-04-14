using System;
using System.Data;
using System.Linq;
using System.Xml.Linq;
using System.IO;
using System.Text;
using System.Collections.Generic;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.DataBase
{
    //----------------------------------------------------------------------
    public partial class DbMultiHostClient : DataBaseHead
    //----------------------------------------------------------------------
    {
        //----------------------------------------------------------------------
        public Returns GetServers() //загрузка данных при мультихостинге
        //----------------------------------------------------------------------
        {
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);

            Returns ret = OpenDb(conn_web, true);
            if (!ret.result)
            {
                ret.text = "Ошибка открытия БД";
                return ret;
            }

#if PG
            ret = (ExecSQL(conn_web,
                  " set encryption password '" + BasePwd + "'"
                  , true));
#else
            ret = (ExecSQL(conn_web,
                  " set encryption password '" + BasePwd + "'"
                  , true));
#endif
            if (!ret.result)
            {
                conn_web.Close();
                return ret; ;
            }

            Points.PointList.Clear();
            Points.Servers.Clear();

            MultiHost.RCentr.Clear();
            MultiHost.RServers.Clear();

            IDataReader reader;

            //Расчетные центры
#if PG
            ret = (ExecRead(conn_web, out reader,
                " Select nzp_rc, rcentr, pref, rc_adr, rc_email, rc_ruk, nzp_raj  " +
                " From s_rcentr " +
                " Order by rcentr ", true));
#else
            ret = (ExecRead(conn_web, out reader,
                " Select nzp_rc, rcentr, pref, rc_adr, rc_email, rc_ruk, nzp_raj  " +
                " From s_rcentr " +
                " Order by rcentr ", true));
#endif
            if (!ret.result)
            {
                conn_web.Close();
                return ret; ;
            }

            return ret;
        }

        //----------------------------------------------------------------------
        public Returns LoadMultiHost() //загрузка данных при мультихостинге
        //----------------------------------------------------------------------
        {
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);

            Returns ret = OpenDb(conn_web, true);
            if (!ret.result)
            {
                ret.text = "Ошибка открытия БД";
                return ret;
            }

            ret = (ExecSQL(conn_web,
                  " set encryption password '" + BasePwd + "'"
                  , true));
            if (!ret.result)
            {
                conn_web.Close();
                return ret; ;
            }

            Points.PointList.Clear();
            Points.Servers.Clear();

            MultiHost.RCentr.Clear();
            MultiHost.RServers.Clear();

            IDataReader reader;

            //Расчетные центры
#if PG
            ret = (ExecRead(conn_web, out reader,
                    " Select nzp_rc, rcentr, pref, rc_adr, rc_email, rc_ruk, nzp_raj  " +
                    " From s_rcentr " +
                    " Order by rcentr ", true));
#else
        ret = (ExecRead(conn_web, out reader,
                " Select nzp_rc, rcentr, pref, rc_adr, rc_email, rc_ruk, nzp_raj  " +
                " From s_rcentr " +
                " Order by rcentr ", true));
#endif
            if (!ret.result)
            {
                conn_web.Close();
                return ret; ;
            }

            try
            {
                while (reader.Read())
                {
                    _RCentr zap = new _RCentr();

                    zap.is_valid = true;
                    zap.rcentr = "";
                    zap.adres = "";
                    zap.email = "";
                    zap.ruk = "";

                    zap.nzp_raj = (int)reader["nzp_raj"];
                    zap.nzp_rc = (int)reader["nzp_rc"];
                    zap.pref = (int)reader["pref"];

                    if (reader["rcentr"] != DBNull.Value)
                        zap.rcentr = (string)reader["rcentr"];
                    if (reader["rc_adr"] != DBNull.Value)
                        zap.adres = (string)reader["rc_adr"];
                    if (reader["rc_email"] != DBNull.Value)
                        zap.email = (string)reader["rc_email"];
                    if (reader["rc_ruk"] != DBNull.Value)
                        zap.ruk = (string)reader["rc_ruk"];

                    zap.rcentr = zap.rcentr.Trim();
                    zap.adres = zap.adres.Trim();
                    zap.email = zap.email.Trim();
                    zap.ruk = zap.ruk.Trim();

                    MultiHost.RCentr.Add(zap);
                }

                reader.Close();
            }
            catch (Exception ex)
            {
                reader.Close();

                string err;
                if (Constants.Viewerror)
                    err = ex.Message;
                else
                    err = "";

                MonitorLog.WriteLog("Ошибка заполнения servers " + err, MonitorLog.typelog.Error, 30, 1, true);
            }

            //Список серверов РЦ 
#if PG
            ret = (ExecRead(conn_web, out reader,
                //                " Select a.nzp_server, a.nzp_rc, decrypt_char(a.hadr) as hadr, decrypt_char(a.hadr2) as hadr2, c.ordering, c.rajon, b.pref, b.rcentr  " +
                " Select (case when c.nzp_raj=1 then 0 when c.nzp_raj in (2,3,24,16,22,16,29) then 1 else 2 end), a.nzp_server, a.nzp_rc, a.hadr, a.hadr2, c.ordering, c.rajon, b.pref, trim(c.rajon)||' - '||trim(b.rcentr) rcentr  " +
                " From servers a, s_rcentr b, s_rajon c " +
                " Where a.nzp_rc = b.nzp_rc and b.nzp_raj = c.nzp_raj " +
                " Order by 1,8 ", true));
#else
            ret = (ExecRead(conn_web, out reader,
                //                " Select a.nzp_server, a.nzp_rc, decrypt_char(a.hadr) as hadr, decrypt_char(a.hadr2) as hadr2, c.ordering, c.rajon, b.pref, b.rcentr  " +
                " Select (case when c.nzp_raj=1 then 0 when c.nzp_raj in (2,3,24,16,22,16,29) then 1 else 2 end), a.nzp_server, a.nzp_rc, a.hadr, a.hadr2, c.ordering, c.rajon, b.pref, trim(c.rajon)||' - '||trim(b.rcentr) rcentr  " +
                " From servers a, s_rcentr b, s_rajon c " +
                " Where a.nzp_rc = b.nzp_rc and b.nzp_raj = c.nzp_raj " +
                " Order by 1,8 ", true));
#endif
            if (!ret.result)
            {
                conn_web.Close();
                return ret; ;
            }
            try
            {
                while (reader.Read())
                {
                    _RServer zap = new _RServer();
                    zap.is_valid = true;

                    zap.ip_adr = "";
                    zap.login = "";
                    zap.pwd = "";

                    zap.nzp_server = (int)reader["nzp_server"];
                    zap.nzp_rc = (int)reader["nzp_rc"];

                    if (reader["hadr"] != DBNull.Value)
                        zap.ip_adr = (string)reader["hadr"];

                    zap.ip_adr = zap.ip_adr.Trim();

                    string hadr2 = "";
                    string login = "";
                    string pwd = "";
                    if (reader["hadr2"] != DBNull.Value)
                        hadr2 = (string)reader["hadr2"];

                    Utils.UserLogin(hadr2, out login, out pwd);

                    zap.login = login.Trim();
                    zap.pwd = pwd.Trim();

                    string rcentr = "";
                    if (reader["rcentr"] != DBNull.Value)
                    {
                        rcentr = (string)reader["rcentr"];
                        rcentr = rcentr.Trim();
                    }
                    //if (reader["rajon"] != DBNull.Value)
                    //{
                    //    rcentr += " /" + (string)reader["rajon"];
                    //    rcentr = rcentr.Trim();
                    //}
                    if (reader["pref"] != DBNull.Value)
                    {
                        rcentr += " (" + (int)reader["pref"] + ")";
                        rcentr = rcentr.Trim();
                    }

                    zap.rcentr = rcentr;
                    MultiHost.RServers.Add(zap);

                }
                reader.Close();
            }
            catch (Exception ex)
            {
                reader.Close();

                string err;
                if (Constants.Viewerror)
                    err = ex.Message;
                else
                    err = "";

                MonitorLog.WriteLog("Ошибка заполнения servers " + err, MonitorLog.typelog.Error, 30, 1, true);
            }

            reader.Close();
            conn_web.Close();
            return ret;
        }
    }
}
