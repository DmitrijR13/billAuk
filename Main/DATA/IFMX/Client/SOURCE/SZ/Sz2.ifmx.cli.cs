using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Global;
using STCLINE.KP50.DataBase;
using System.Collections;
using System.Data;
using System.Globalization;

namespace STCLINE.KP50.DataBase
{
    public partial class DbSzClient : DataBaseHead
    {
        public List<SzMo> GetMo(SzMo finder, out Returns ret)
        {
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;

            string sql = "select nzp_mo, mo from sz_mo order by mo";
            IDataReader reader;
            ret = ExecRead(conn_web, out reader, sql, true);
            if (!ret.result)
            {
                conn_web.Close();
                return null;
            }

            List<SzMo> list = new List<SzMo>();
            try
            {
                while (reader.Read())
                {
                    SzMo zap = new SzMo();
                    if (reader["nzp_mo"] != DBNull.Value) zap.nzp_mo = Convert.ToInt32(reader["nzp_mo"]);
                    if (reader["mo"] != DBNull.Value) zap.mo = Convert.ToString(reader["mo"]).Trim();
                    list.Add(zap);
                }
            }
            catch (Exception ex)
            {
                if (reader != null) reader.Close();
                conn_web.Close();
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка в функции GetMo " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }

            conn_web.Close();
            ret.tag = list.Count;
            return list;
        }

        public List<SzRajon> GetRajon(SzRajon finder, out Returns ret)
        {
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;

#if PG
string sql = "select distinct a.nzp_raj, a.rajon, b.nzp_mo from sz_rajon a, sz_link2 b where a.nzp_raj = b.nzp_raj";
#else
            string sql = "select unique a.nzp_raj, a.rajon, b.nzp_mo from sz_rajon a, sz_link2 b where a.nzp_raj = b.nzp_raj";
#endif
            if (finder.list_mo.Trim() != "") sql += " and b.nzp_mo in (" + finder.list_mo + ")";
            sql += " order by a.rajon";
            IDataReader reader;
            ret = ExecRead(conn_web, out reader, sql, true);
            if (!ret.result)
            {
                conn_web.Close();
                return null;
            }

            List<SzRajon> list = new List<SzRajon>();
            try
            {
                while (reader.Read())
                {
                    SzRajon zap = new SzRajon();
                    if (reader["nzp_mo"] != DBNull.Value) zap.nzp_mo = Convert.ToInt32(reader["nzp_mo"]);
                    if (reader["nzp_raj"] != DBNull.Value) zap.nzp_raj = Convert.ToInt32(reader["nzp_raj"]);
                    if (reader["rajon"] != DBNull.Value) zap.rajon = Convert.ToString(reader["rajon"]).Trim();
                    list.Add(zap);
                }
            }
            catch (Exception ex)
            {
                if (reader != null) reader.Close();
                conn_web.Close();
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка в функции GetRajon " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }

            conn_web.Close();
            ret.tag = list.Count;
            return list;
        }

        public List<SzUK> GetUK(SzUK finder, out Returns ret)
        {
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;

#if PG
string sql = "select distinct a.nzp_mo, a.nzp_uk2, a.name_uk from sz_uk2 a, sz_link2 b" +
                " where a.nzp_uk2 = b.nzp_uk2";
#else
            string sql = "select unique a.nzp_mo, a.nzp_uk2, a.name_uk from sz_uk2 a, sz_link2 b" +
                            " where a.nzp_uk2 = b.nzp_uk2";
#endif
            if (finder.list_mo.Trim() != "") sql += " and b.nzp_mo in (" + finder.list_mo + ")";
            if (finder.list_raj.Trim() != "") sql += " and b.nzp_raj in (" + finder.list_raj + ")";
            sql += " order by a.name_uk";
            IDataReader reader;
            ret = ExecRead(conn_web, out reader, sql, true);
            if (!ret.result)
            {
                conn_web.Close();
                return null;
            }

            List<SzUK> list = new List<SzUK>();
            try
            {
                while (reader.Read())
                {
                    SzUK zap = new SzUK();
                    if (reader["nzp_mo"] != DBNull.Value) zap.nzp_mo = Convert.ToInt32(reader["nzp_mo"]);
                    if (reader["nzp_uk2"] != DBNull.Value) zap.nzp_uk = Convert.ToInt32(reader["nzp_uk2"]);
                    if (reader["name_uk"] != DBNull.Value) zap.name_uk = Convert.ToString(reader["name_uk"]).Trim();
                    list.Add(zap);
                }
            }
            catch (Exception ex)
            {
                if (reader != null) reader.Close();
                conn_web.Close();
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка в функции GetUK " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }

            conn_web.Close();
            ret.tag = list.Count;
            return list;
        }

        public List<SzUKPodr> GetUKPodr(SzUKPodr finder, out Returns ret)
        {
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;

            string sql = "select nzp_mo, nzp_uk_podr2, name_podr from sz_uk_podr2";
            if (finder.list_mo.Trim() != "") sql += " where nzp_mo in (" + finder.list_mo + ")";
            sql += " order by name_podr";
            IDataReader reader;
            ret = ExecRead(conn_web, out reader, sql, true);
            if (!ret.result)
            {
                conn_web.Close();
                return null;
            }

            List<SzUKPodr> list = new List<SzUKPodr>();
            try
            {
                while (reader.Read())
                {
                    SzUKPodr zap = new SzUKPodr();
                    if (reader["nzp_mo"] != DBNull.Value) zap.nzp_mo = Convert.ToInt32(reader["nzp_mo"]);
                    if (reader["nzp_uk_podr2"] != DBNull.Value) zap.nzp_uk_podr = Convert.ToInt32(reader["nzp_uk_podr2"]);
                    if (reader["name_podr"] != DBNull.Value) zap.name_podr = Convert.ToString(reader["name_podr"]).Trim();
                    list.Add(zap);
                }
            }
            catch (Exception ex)
            {
                if (reader != null) reader.Close();
                conn_web.Close();
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка в функции GetUKPodr " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }

            conn_web.Close();
            ret.tag = list.Count;
            return list;
        }

        public List<SzUlica> GetUlica(SzUlica finder, out Returns ret)
        {
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;
#if PG
            string sql = "select distinct a.nzp_ul2, a.nzp_mo, a.ulica from sz_ulica2 a, sz_link2 b where a.nzp_ul2 = b.nzp_ul2";
#else
            string sql = "select unique a.nzp_ul2, a.nzp_mo, a.ulica from sz_ulica2 a, sz_link2 b where a.nzp_ul2 = b.nzp_ul2";
#endif
            if (finder.list_mo.Trim() != "") sql += " and b.nzp_mo in (" + finder.list_mo + ")";
            if (finder.list_raj.Trim() != "") sql += " and b.nzp_raj in (" + finder.list_raj + ")";
            if (finder.ulica.Trim() != "") sql += " and upper(a.ulica) matches '" + finder.ulica.Trim().Replace("'", "''").ToUpper() + "*'";
            sql += " order by a.ulica";
            IDataReader reader;
            ret = ExecRead(conn_web, out reader, sql, true);
            if (!ret.result)
            {
                conn_web.Close();
                return null;
            }

            List<SzUlica> list = new List<SzUlica>();
            try
            {
                while (reader.Read())
                {
                    if (list.Count >= finder.rows) break;
                    SzUlica zap = new SzUlica();
                    if (reader["nzp_ul2"] != DBNull.Value) zap.nzp_ul = Convert.ToInt32(reader["nzp_ul2"]);
                    if (reader["nzp_mo"] != DBNull.Value) zap.nzp_mo = Convert.ToInt32(reader["nzp_mo"]);
                    if (reader["ulica"] != DBNull.Value) zap.ulica = Convert.ToString(reader["ulica"]).Trim();
                    list.Add(zap);
                }
            }
            catch (Exception ex)
            {
                if (reader != null) reader.Close();
                conn_web.Close();
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка в функции GetUlica " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }

            conn_web.Close();
            ret.tag = list.Count;
            return list;
        }
        
    }
}
