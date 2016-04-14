using System;
using System.Linq;
using System.Text;
using System.Data;
using System.Collections.Generic;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.DataBase
{
    public class DbCert : DataBaseHead
    {
        //создать сертификат
        public void CheckCN(out Returns ret, string cn)
        {
            ret = Utils.InitReturns();

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return;

            //проверить наличие в базе сгенерированного CN (иначе будет ошибка)
#if PG
            string sql =
                            " Select * From crt_list Where lcn = " + Utils.EStrNull(cn) + " and num_lcer <> '0' ";
#else
            string sql =
                " Select * From crt_list Where lcn = " + Utils.EStrNull(cn) + " and num_lcer <> '0' ";
#endif
            IDataReader reader;
            ret = ExecRead(conn_web, out reader, sql, true);
            if (!ret.result)
            {
                conn_web.Close();
                return;
            }
            if (reader.Read())
            {
                //значит такой CN уже был сгенирирован!
                ret.text = "В базе уже заведен такой CN " + cn;
                ret.result = false;
            }

            conn_web.Close();
        }

        public int CreateCert(out Returns ret, CrtList cert)
        {
            ret = Utils.InitReturns();
            int res = Constants._ZERO_;
 
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return res;

#if PG
            string sql =
                            " Insert into crt_list ( nzp_ucer, num_lcer, kod, lcn, dat_create, crt_days, cer_path, cer_pwd ) " +
                            " Values (" + cert.nzp_ucer + ",'0'," + cert.kod + "," + Utils.EStrNull(cert.lcn) + "," + Utils.EStrNull(cert.dat_create) + "," +
                                cert.crt_days + "," + Utils.EStrNull(cert.cer_path) + "," + Utils.EStrNull(cert.cer_pwd) + ")";
#else
            string sql =
                " Insert into crt_list ( nzp_ucer, num_lcer, kod, lcn, dat_create, crt_days, cer_path, cer_pwd ) " +
                " Values (" + cert.nzp_ucer + ",'0'," + cert.kod + "," + Utils.EStrNull(cert.lcn) + "," + Utils.EStrNull(cert.dat_create) + "," +
                    cert.crt_days + "," + Utils.EStrNull(cert.cer_path) + "," + Utils.EStrNull(cert.cer_pwd) + ")";
#endif

                    
            ret = ExecSQL(conn_web, sql, true); 

            if (!ret.result)
            {
                conn_web.Close();
                return res;
            }

            res = GetSerialValue(conn_web);
            conn_web.Close();

            if (res < 0)
            {
                ret.result = false;
                ret.text = "Ошибка получения первичного ключа";
            }
            return res;
        }

        //изменить сертификат
        public void UpdCert(out Returns ret, CrtList cert)
        {
            ret = Utils.InitReturns();

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return;

#if PG
            string sql =
                            " Update crt_list " +
                            " Set num_lcer = " + Utils.EStrNull(cert.num_lcer) + ", cer_path = " + Utils.EStrNull(cert.cer_path) +
                            " Where nzp_lcer = " + cert.nzp_lcer;
#else
            string sql =
                " Update crt_list "+
                " Set num_lcer = " + Utils.EStrNull(cert.num_lcer) + ", cer_path = " + Utils.EStrNull(cert.cer_path) +
                " Where nzp_lcer = " + cert.nzp_lcer;
#endif

            ret = ExecSQL(conn_web, sql, true);

            conn_web.Close();
        }

        //изменить данные о клиенте
        public int UpdClient(out Returns ret, CrtClient client)
        {
            ret = Utils.InitReturns();

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return Constants._ZERO_;

            //прежде проверить, что такого cn нет в базе
#if PG
            string sql = " Select * From crt_client Where uc_prcn = " + Utils.EStrNull(client.uc_prcn) + " and nzp_ucer <> " + client.nzp_ucer;
#else
            string sql = " Select * From crt_client Where uc_prcn = " + Utils.EStrNull(client.uc_prcn) + " and nzp_ucer <> " + client.nzp_ucer;
#endif
            IDataReader reader;
            ret = ExecRead(conn_web, out reader, sql, true);
            if (!ret.result)
            {
                conn_web.Close();
                return Constants._ZERO_;;
            }
            if (reader.Read())
            {
                //значит такой CN уже был сгенирирован!
                ret.text = "В базе уже заведен такой CN " + client.uc_prcn;
                ret.result = false;

                conn_web.Close();
                return Constants._ZERO_;
            }

#if PG
            if (client.nzp_ucer < 1)
            {
                sql =
                " Insert into crt_client (uc_c, uc_st, uc_l, uc_o, uc_ou, uc_em, uc_prcn, uc_clcn, uc_name) " +
                " Values ( " + Utils.EStrNull(client.uc_c) + "," + Utils.EStrNull(client.uc_st) + "," +
                               Utils.EStrNull(client.uc_l) + "," + Utils.EStrNull(client.uc_o) + "," +
                               Utils.EStrNull(client.uc_ou) + "," + Utils.EStrNull(client.uc_em) + "," +
                               Utils.EStrNull(client.uc_prcn) + "," + Utils.EStrNull(client.uc_clcn) + "," +
                               Utils.EStrNull(client.uc_name) + ")";
            }
#else
            if (client.nzp_ucer < 1)
            {
                sql =
                " Insert into crt_client (uc_c, uc_st, uc_l, uc_o, uc_ou, uc_em, uc_prcn, uc_clcn, uc_name) " +
                " Values ( " + Utils.EStrNull(client.uc_c) + "," + Utils.EStrNull(client.uc_st) + "," +
                               Utils.EStrNull(client.uc_l) + "," + Utils.EStrNull(client.uc_o) + "," +
                               Utils.EStrNull(client.uc_ou) + "," + Utils.EStrNull(client.uc_em) + "," +
                               Utils.EStrNull(client.uc_prcn) + "," + Utils.EStrNull(client.uc_clcn) + "," +
                               Utils.EStrNull(client.uc_name) + ")";
            }
#endif
            #if PG
 else
            {
                sql =
                " Update crt_client " +
                " Set (uc_c, uc_st, uc_l, uc_o, uc_ou, uc_em, uc_prcn, uc_clcn, uc_name) = ("+
                               Utils.EStrNull(client.uc_c) + "," + Utils.EStrNull(client.uc_st) + "," +
                               Utils.EStrNull(client.uc_l) + "," + Utils.EStrNull(client.uc_o) + "," +
                               Utils.EStrNull(client.uc_ou) + "," + Utils.EStrNull(client.uc_em) + "," +
                               Utils.EStrNull(client.uc_prcn) + "," + Utils.EStrNull(client.uc_clcn) + "," +
                               Utils.EStrNull(client.uc_name) + ")"+
                " Where nzp_ucer = " + client.nzp_ucer;
            }
#else
 else
            {
                sql =
                " Update crt_client " +
                " Set (uc_c, uc_st, uc_l, uc_o, uc_ou, uc_em, uc_prcn, uc_clcn, uc_name) = ("+
                               Utils.EStrNull(client.uc_c) + "," + Utils.EStrNull(client.uc_st) + "," +
                               Utils.EStrNull(client.uc_l) + "," + Utils.EStrNull(client.uc_o) + "," +
                               Utils.EStrNull(client.uc_ou) + "," + Utils.EStrNull(client.uc_em) + "," +
                               Utils.EStrNull(client.uc_prcn) + "," + Utils.EStrNull(client.uc_clcn) + "," +
                               Utils.EStrNull(client.uc_name) + ")"+
                " Where nzp_ucer = " + client.nzp_ucer;
            }
#endif

            ret = ExecSQL(conn_web, sql, true);
            if (ret.result && client.nzp_ucer < 1)
            {
                client.nzp_ucer = GetSerialValue(conn_web);
            }

            conn_web.Close();
            return client.nzp_ucer;
        }

        //получить список клиентов
        public List<CrtClient> GetClient(CrtList finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<CrtClient> Spis = new List<CrtClient>();

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;
#if PG
            string sql_where = " Where 1 = 1 ";
            if (finder.nzp_ucer > 0)
                sql_where += " and nzp_ucer = " + finder.nzp_ucer;
            IDbCommand cmd = DBManager.newDbCommand(" Select count(*) From crt_client" + sql_where, conn_web);
#else
            string sql_where = " Where 1 = 1 ";
            if (finder.nzp_ucer > 0)
                sql_where += " and nzp_ucer = " + finder.nzp_ucer;
            IDbCommand cmd = DBManager.newDbCommand(" Select count(*) From crt_client" + sql_where, conn_web);
#endif
            int total_record_count = 0;
            try
            {
                total_record_count = Convert.ToInt32(cmd.ExecuteScalar());
            }
            catch (Exception ex)
            {
                conn_web.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;
                MonitorLog.WriteLog("Ошибка заполнения списка клиентов " + err, MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }

#if PG
            string sql =
                           " Select nzp_ucer, uc_c, uc_st, uc_l, uc_o, uc_ou, uc_em, uc_prcn, uc_clcn, uc_name " +
                           " From crt_client " +
                           sql_where +
                           " Order by 1 ";
#else
 string sql =
                " Select nzp_ucer, uc_c, uc_st, uc_l, uc_o, uc_ou, uc_em, uc_prcn, uc_clcn, uc_name " +
                " From crt_client " +
                sql_where +
                " Order by 1 ";
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
                while (reader.Read())
                {
                    CrtClient zap = new CrtClient();

                    if (reader["nzp_ucer"]  != DBNull.Value) zap.nzp_ucer = Convert.ToInt32 (reader["nzp_ucer"]);
                    if (reader["uc_c"]      != DBNull.Value) zap.uc_c     = (Convert.ToString(reader["uc_c"])).Trim();
                    if (reader["uc_st"]     != DBNull.Value) zap.uc_st    = (Convert.ToString(reader["uc_st"])).Trim();
                    if (reader["uc_l"]      != DBNull.Value) zap.uc_l     = (Convert.ToString(reader["uc_l"])).Trim();
                    if (reader["uc_o"]      != DBNull.Value) zap.uc_o     = (Convert.ToString(reader["uc_o"])).Trim();
                    if (reader["uc_ou"]     != DBNull.Value) zap.uc_ou    = (Convert.ToString(reader["uc_ou"])).Trim();
                    if (reader["uc_em"]     != DBNull.Value) zap.uc_em    = (Convert.ToString(reader["uc_em"])).Trim();
                    if (reader["uc_prcn"]   != DBNull.Value) zap.uc_prcn  = (Convert.ToString(reader["uc_prcn"])).Trim();
                    if (reader["uc_clcn"]   != DBNull.Value) zap.uc_clcn  = (Convert.ToString(reader["uc_clcn"])).Trim();
                    if (reader["uc_name"]   != DBNull.Value) zap.uc_name  = (Convert.ToString(reader["uc_name"])).Trim();

                    Spis.Add(zap);

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

                MonitorLog.WriteLog("Ошибка заполнения списка клиентов " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }
        }//GetClient

        //получить список сертификатов
        public List<CrtList> GetCerts(CrtList finder, out Returns ret)
        {
            ret = Utils.InitReturns();

            List<CrtList> Spis = new List<CrtList>();

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;
#if PG
            string sql_where = " Where num_lcer <> '0' ";
            if (finder.nzp_ucer > 0)
                sql_where += " and nzp_ucer = " + finder.nzp_ucer;
            IDbCommand cmd = DBManager.newDbCommand(" Select count(*) From crt_list " + sql_where, conn_web);
#else
            string sql_where = " Where num_lcer <> '0' ";
            if (finder.nzp_ucer > 0)
                sql_where += " and nzp_ucer = " + finder.nzp_ucer;
            IDbCommand cmd = DBManager.newDbCommand(" Select count(*) From crt_list " + sql_where, conn_web);
#endif
            int total_record_count = 0;
            try
            {
                total_record_count = Convert.ToInt32(cmd.ExecuteScalar());
            }
            catch (Exception ex)
            {
                conn_web.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;
                MonitorLog.WriteLog("Ошибка заполнения списка сертификатов " + err, MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }

#if PG
            string sql =
                " Select nzp_lcer, nzp_ucer, num_lcer, kod, lcn, dat_create, crt_days, dat_revoke, cer_path, cer_pwd " +
                " From crt_list " +
                sql_where +
                " Order by lcn ";
#else
            string sql =
                " Select nzp_lcer, nzp_ucer, num_lcer, kod, lcn, dat_create, crt_days, dat_revoke, cer_path, cer_pwd " +
                " From crt_list " +
                sql_where +
                " Order by lcn ";
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
                while (reader.Read())
                {
                    CrtList zap = new CrtList();

                    if (reader["nzp_lcer"]  != DBNull.Value) zap.nzp_lcer = Convert.ToInt32 (reader["nzp_lcer"]);
                    if (reader["nzp_ucer"]  != DBNull.Value) zap.nzp_ucer = Convert.ToInt32 (reader["nzp_ucer"]);
                    if (reader["num_lcer"]  != DBNull.Value) zap.num_lcer = (Convert.ToString(reader["num_lcer"])).Trim();
                    if (reader["kod"]       != DBNull.Value) zap.kod      = Convert.ToInt32(reader["kod"]);
                    if (reader["lcn"]       != DBNull.Value) zap.lcn      = (Convert.ToString(reader["lcn"])).Trim();
                    if (reader["dat_create"]!= DBNull.Value)
                    {
                        DateTime d = Convert.ToDateTime(reader["dat_create"]);
                        zap.dat_create = d.ToShortDateString();
                    }
                    if (reader["crt_days"]   != DBNull.Value) zap.crt_days = Convert.ToInt32(reader["crt_days"]);
                    if (reader["dat_revoke"] != DBNull.Value)
                    {
                        DateTime d = Convert.ToDateTime(reader["dat_revoke"]);
                        zap.dat_revoke = d.ToShortDateString();
                    }

                    if (reader["cer_path"]  != DBNull.Value) zap.cer_path = (Convert.ToString(reader["cer_path"])).Trim();
                    if (reader["cer_pwd"]   != DBNull.Value) zap.cer_pwd  = (Convert.ToString(reader["cer_pwd"])).Trim();

                    Spis.Add(zap);

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

                MonitorLog.WriteLog("Ошибка заполнения списка клиентов " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }
        }//GetClient

    }
}
