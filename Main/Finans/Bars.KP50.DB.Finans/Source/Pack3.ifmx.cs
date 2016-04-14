using System;
using System.Data;
using System.Globalization;
using System.Linq;
using System.ServiceModel.Channels;
using System.Xml.Linq;
using System.IO;
using System.Text;
using System.Collections.Generic;
using Microsoft.Office.Interop.Excel;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using Constants = STCLINE.KP50.Global.Constants;
using Points = STCLINE.KP50.Interfaces.Points;


namespace STCLINE.KP50.DataBase
{
    public partial class DbPack : DbPackClient
    {
        public List<Bank> LoadBankForKassa(Bank finder, out Returns ret)
        {
            ret = Utils.InitReturns();

            List<Bank> spis = new List<Bank>();

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            string sql = "", where = "";
            IDataReader reader;

            if (finder.nzp_user > 0 && finder.nzp_bank <= 0)
            {
                IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
                ret = OpenDb(conn_web, true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return null;
                }

#if PG
                sql = "select nzp_bank, nzp_payer from public.users where nzp_user = " + finder.nzp_user;
#else
sql = "select nzp_bank, nzp_payer from " + conn_web.Database + "@" + DBManager.getServer(conn_web) + ":users where nzp_user = " + finder.nzp_user;
#endif
                if (!ExecRead(conn_db, out reader, sql, true).result)
                {
                    conn_db.Close();
                    return null;
                }

                try
                {
                    while (reader.Read())
                    {
                        if (reader["nzp_bank"] != DBNull.Value) finder.nzp_bank = Convert.ToInt32(reader["nzp_bank"]);
                        if (reader["nzp_payer"] != DBNull.Value) finder.nzp_payer = Convert.ToInt32(reader["nzp_payer"]);
                    }
                }
                catch (Exception ex)
                {
                    reader.Close();
                    conn_db.Close();
                    conn_web.Close();

                    ret.result = false;
                    ret.text = ex.Message;

                    string err;
                    if (Constants.Viewerror) err = " \n " + ex.Message;
                    else err = "";
                    MonitorLog.WriteLog("Ошибка заполнения мест формирования " + err, MonitorLog.typelog.Error, 20, 201, true);
                    return null;
                }

                if (finder.nzp_bank <= 0 && finder.nzp_payer > 0)
                {
                    where += " and nzp_payer = " + finder.nzp_payer;
                }
            }

            //выбрать список
#if PG
            sql = " Select nzp_bank, bank,nzp_payer From " + Points.Pref + "_kernel.s_bank Where nzp_payer is not null" + where;
#else
sql = " Select nzp_bank, bank,nzp_payer From " + Points.Pref + "_kernel:s_bank Where nzp_payer is not null" + where;
#endif
#if PG
            if (finder.nzp_bank > 0) sql += " and nzp_bank = " + finder.nzp_bank;
            else sql += " and not (lower(trim(bank)) like '*суперпачка%')" +
                " Order by bank ";
#else
 if (finder.nzp_bank > 0) sql += " and nzp_bank = " + finder.nzp_bank;
            else sql += " and not (lower(trim(bank)) matches '*суперпачка*')" +
                " Order by bank ";
#endif

            if (!ExecRead(conn_db, out reader, sql, true).result)
            {
                conn_db.Close();
                return null;
            }
            try
            {
                while (reader.Read())
                {
                    Bank zap = new Bank();
                    if (reader["nzp_bank"] == DBNull.Value) zap.nzp_bank = 0;
                    else zap.nzp_bank = (int)reader["nzp_bank"];
                    if (reader["bank"] == DBNull.Value) zap.bank = "";
                    else zap.bank = (string)reader["bank"];
                    if (reader["nzp_payer"] == DBNull.Value) zap.nzp_payer = 0;
                    else zap.nzp_payer = (int)reader["nzp_payer"];
                    spis.Add(zap);
                }

                reader.Close();
                conn_db.Close();
                return spis;
            }
            catch (Exception ex)
            {
                reader.Close();
                conn_db.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err;
                if (Constants.Viewerror) err = " \n " + ex.Message;
                else err = "";
                MonitorLog.WriteLog("Ошибка заполнения мест формирования " + err, MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
        }

        public List<Bank> LoadListBanks(Bank finder, out Returns ret)
        {
            ret = Utils.InitReturns();

            List<Bank> spis = new List<Bank>();

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;
            
            string sql = "", where = "";

            if (finder.nzp_payer > 0) where = " and nzp_payer = " + finder.nzp_payer;

            IDataReader reader;           
            //выбрать список
#if PG
            sql = " Select nzp_bank, bank,nzp_payer From " + Points.Pref + "_kernel.s_bank Where nzp_payer is not null" + where;
#else
sql = " Select nzp_bank, bank,nzp_payer From " + Points.Pref + "_kernel:s_bank Where nzp_payer is not null" + where;
#endif
#if PG
            if (finder.nzp_bank > 0) sql += " and nzp_bank = " + finder.nzp_bank;
            else sql += " and not (lower(trim(bank)) like '*суперпачка%')" +
                " Order by bank ";
#else
 if (finder.nzp_bank > 0) sql += " and nzp_bank = " + finder.nzp_bank;
            else sql += " and not (lower(trim(bank)) matches '*суперпачка*')" +
                " Order by bank ";
#endif

            if (!ExecRead(conn_db, out reader, sql, true).result)
            {
                conn_db.Close();
                return null;
            }
            try
            {
                while (reader.Read())
                {
                    Bank zap = new Bank();
                    if (reader["nzp_bank"] == DBNull.Value) zap.nzp_bank = 0;
                    else zap.nzp_bank = (int)reader["nzp_bank"];
                    if (reader["bank"] == DBNull.Value) zap.bank = "";
                    else zap.bank = (string)reader["bank"];
                    if (reader["nzp_payer"] == DBNull.Value) zap.nzp_payer = 0;
                    else zap.nzp_payer = (int)reader["nzp_payer"];
                    spis.Add(zap);
                }

                reader.Close();
                conn_db.Close();
                return spis;
            }
            catch (Exception ex)
            {
                reader.Close();
                conn_db.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err;
                if (Constants.Viewerror) err = " \n " + ex.Message;
                else err = "";
                MonitorLog.WriteLog("Ошибка заполнения мест формирования " + err, MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
        }

        public List<Ulica> LoadUlica(Ulica finder, out Returns ret)
        {
            ret = Utils.InitReturns();

            List<Ulica> spis = new List<Ulica>();

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            //выбрать список
            string where = "";

            if (finder.RolesVal != null)
                foreach (_RolesVal role in finder.RolesVal)
                    if (role.tip == Constants.role_sql)
                    {
                        if (role.kod == Constants.role_sql_wp)
                        {
                            where += " and p.nzp_wp in (" + role.val + ")";
                        }
                        else if (role.kod == Constants.role_sql_area) where += " and k.nzp_area in (" + role.val + ") ";
                    }

            #region Фильтр по kvar nzp_wp

        
            bool haskvFilter = false;
            string sql1 = "Create temp table t_ul (nzp_ul integer)";
            ExecSQL(conn_db, sql1, true);

            if (!String.IsNullOrEmpty(where) || (finder.nzp_wp > 0) || (finder.nzp_area > 0) ||
                (finder.nzp_geu > 0))
            {


                sql1 = " insert into t_ul " +
                       " select nzp_ul " +
                       " from " + Points.Pref + sDataAliasRest + "dom d, " +
                       " " + Points.Pref + sDataAliasRest + "kvar k, " +
                       " " + Points.Pref + sKernelAliasRest + "s_point p " +
                       " where k.nzp_dom=d.nzp_dom " +
                       " and k.nzp_wp=p.nzp_wp " + where;
                if (finder.nzp_wp > 0) sql1 += " and p.nzp_wp = " + finder.nzp_wp;
                if (finder.nzp_area > 0) sql1 += " and k.nzp_area = " + finder.nzp_area;
                if (finder.nzp_geu > 0) sql1 += " and k.nzp_geu = " + finder.nzp_geu;
                sql1+=" group by 1 ";
                ExecSQL(conn_db, sql1, true);
                haskvFilter = true;
            }

            #endregion





            string sql = " Select distinct u.nzp_ul, u.ulicareg, u.ulica, r.rajon " +
                         " From " + Points.Pref + sDataAliasRest +"s_ulica u, " +
                         "  " + Points.Pref + sDataAliasRest +"s_rajon r " +
                         " where  r.nzp_raj = u.nzp_raj ";
            if (finder.nzp_ul > 0) sql += " and u.nzp_ul = " + finder.nzp_ul;
            if (finder.nzp_raj > 0) sql += " and u.nzp_raj = " + finder.nzp_raj;
            if (haskvFilter) sql += " and u.nzp_ul in (select nzp_ul from t_ul)";
            sql += " Order by u.ulica ";


            MyDataReader reader;
            if (!ExecRead(conn_db, out reader, sql, true).result)
            {
                ExecSQL(conn_db, "drop table t_ul", true);
                conn_db.Close();
                return null;
            }
            try
            {
                while (reader.Read())
                {
                    Ulica zap = new Ulica();
                    if (reader["nzp_ul"] == DBNull.Value) zap.nzp_ul = 0;
                    else zap.nzp_ul = Convert.ToInt32(reader["nzp_ul"]);
                    if (reader["ulica"] == DBNull.Value) zap.ulica = "";
                    else zap.ulica = (string) reader["ulica"];
                    if (reader["rajon"] != DBNull.Value) zap.rajon = Convert.ToString(reader["rajon"]);
                    if (reader["ulicareg"] == DBNull.Value) zap.ulicareg = "";
                    else zap.ulicareg = (string) reader["ulicareg"];
                    spis.Add(zap);
                }



                if (spis != null)
                {
                    ret.tag = spis.Count;
                    if (finder.skip > 0 && spis.Count > finder.skip) spis.RemoveRange(0, finder.skip);
                    if (finder.rows > 0 && spis.Count > finder.rows)
                        spis.RemoveRange(finder.rows, spis.Count - finder.rows);
                }


                return spis;
            }
            catch (Exception ex)
            {

                ret.result = false;
                ret.text = ex.Message;

                string err;
                if (Constants.Viewerror) err = " \n " + ex.Message;
                else err = "";
                MonitorLog.WriteLog("Ошибка заполнения справочника улиц " + err, MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
            finally
            {
                reader.Close();
                ExecSQL(conn_db, "drop table t_ul", true);
                conn_db.Close();
            }
           
           

        }

        public List<Dom> LoadDom(Dom finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            if (finder.nzp_ul <= 0)
            {
                ret.result = false;
                ret.text = "Не указана улица";
                ret.tag = -1;
                return null;
            }

            List<Dom> spis = new List<Dom>();

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            bool hasPointConstraint = finder.nzp_wp > 0;
            string where = "";

            if (finder.RolesVal != null)
                foreach (_RolesVal role in finder.RolesVal)
                    if (role.tip == Constants.role_sql)
                    {
                        if (role.kod == Constants.role_sql_wp)
                        {
                            where += " and p.nzp_wp in (" + role.val + ") ";
                            hasPointConstraint = true;
                        }
                        else if (role.kod == Constants.role_sql_area) where += " and d.nzp_area in (" + role.val + ") ";
                    }

            //выбрать список
            string sql = "";

#if PG
            sql = " Select distinct d.nzp_dom, d.ndom, d.idom, d.nkor, min(k.ikvar) as min_kvar, max(k.ikvar) as max_kvar " +
                " From " + Points.Pref + "_data.dom d LEFT OUTER JOIN " + Points.Pref + "_data.kvar k ON (d.nzp_dom = k.nzp_dom)";
            if (hasPointConstraint) sql += ", " + Points.Pref + "_kernel.s_point p ";
            sql += " where 1=1 " + where;
            if (finder.nzp_ul > 0) sql += " and d.nzp_ul = " + finder.nzp_ul;
            if (finder.nzp_area > 0) sql += " and (k.nzp_area = " + finder.nzp_area + " or d.nzp_area = " + finder.nzp_area + ")";
            if (finder.nzp_geu > 0) sql += " and (k.nzp_geu = " + finder.nzp_geu + " or d.nzp_geu = " + finder.nzp_geu + ")";
            if (finder.nzp_dom > 0) sql += " and d.nzp_dom = " + finder.nzp_dom;
            if (hasPointConstraint) sql += " and k.pref = p.bd_kernel";
            if (finder.nzp_wp > 0) sql += " and  p.nzp_wp = " + finder.nzp_wp;
            if (finder.is_blocked == -1) sql += " and k.is_open::int <>2 ";

            sql += " Group by 1, 2, 3, 4";
            sql += " Order by 3, 2, 4, 5, 6 ";
#else
            sql = " Select unique d.nzp_dom, d.ndom, d.idom, d.nkor, min(k.ikvar) as min_kvar, max(k.ikvar) as max_kvar " +
                " From " + Points.Pref + "_data:dom d, " + Points.Pref + "_data:kvar k ";
            if (hasPointConstraint) sql += ", " + Points.Pref + "_kernel:s_point p ";
            sql += " where d.nzp_dom = k.nzp_dom " + where;
            if (finder.nzp_ul > 0) sql += " and d.nzp_ul = " + finder.nzp_ul;
            if (finder.nzp_area > 0) sql += " and (k.nzp_area = " + finder.nzp_area + " or d.nzp_area = " + finder.nzp_area + ")";
            if (finder.nzp_geu > 0) sql += " and (k.nzp_geu = " + finder.nzp_geu + " or d.nzp_geu = " + finder.nzp_geu + ")";
            if (finder.nzp_dom > 0) sql += " and d.nzp_dom = " + finder.nzp_dom;
            if (hasPointConstraint) sql += " and k.pref = p.bd_kernel";
            if (finder.nzp_wp > 0) sql += " and p.nzp_wp = " + finder.nzp_wp;
            if (finder.is_blocked == -1) sql += " and k.is_open <>2 ";

            sql += " Group by 1, 2, 3, 4";
            sql += " Order by 3, 2, 4, 5, 6 ";
#endif
            IDataReader reader;
            if (!ExecRead(conn_db, out reader, sql, true).result)
            {
                conn_db.Close();
                return null;
            }
            try
            {
                int min_kvar;
                int max_kvar;

                while (reader.Read())
                {
                    Dom zap = new Dom();

                    min_kvar = 0;
                    max_kvar = 0;

                    if (reader["nzp_dom"] == DBNull.Value) zap.nzp_dom = 0;
                    else zap.nzp_dom = Convert.ToInt32(reader["nzp_dom"]);
                    if (reader["ndom"] == DBNull.Value) zap.ndom = "";
                    else zap.ndom = (string)reader["ndom"];
                    if (reader["nkor"] != DBNull.Value) zap.nkor = ((string)reader["nkor"]).Trim();

                    zap.adr = zap.ndom;
                    if (zap.nkor != "" && zap.nkor != "-") zap.adr += " корп. " + zap.nkor;

                    if (reader["min_kvar"] != DBNull.Value) min_kvar = Convert.ToInt32(reader["min_kvar"]);
                    if (reader["max_kvar"] != DBNull.Value) max_kvar = Convert.ToInt32(reader["max_kvar"]);

                    if (min_kvar != max_kvar)
                    {
                        zap.adr += " (c " + min_kvar + " по " + max_kvar + ")";
                    }

                    spis.Add(zap);
                }

                reader.Close();
                conn_db.Close();
                return spis;
            }
            catch (Exception ex)
            {
                reader.Close();
                conn_db.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err;
                if (Constants.Viewerror) err = " \n " + ex.Message;
                else err = "";
                MonitorLog.WriteLog("Ошибка заполнения справочника домов " + err, MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
        }


        //--------------------------------------------------------------
        public List<Ls> LoadKvar(Ls finder, out Returns ret)
        //--------------------------------------------------------------
        {
            if (finder.nzp_dom <= 0)
            {
                ret = new Returns(false, "Не указан дом", -1);
                //ret.result = false;
                //ret.text = "Не указан дом";
                //ret.tag = -1;
                return null;
            }

            return LoadKvarList(finder, out ret);
        }
        //--------------------------------------------------------------
        public List<Ls> LoadLsForKassa(Ls finder, out Returns ret)
        //--------------------------------------------------------------
        {
            if (finder.pkod == "" && finder.nzp_kvar < 1 && finder.num_ls < 1 && finder.pkod10 < 1)
            {
                ret = new Returns(false, "Не указан платежный код или лицевой счет", -1);
                return null;
            }

            return LoadKvarList(finder, out ret);

            /*List<Ls> spis = LoadKvarList(finder, out ret);

            if (spis == null)
            {
                return null;
            }
            else if (spis.Count == 0)
            {
                return new Ls();
            }
            else
                return spis[0];*/
        }

        //--------------------------------------------------------------
        public List<Ls> LoadKvarList(Ls finder, out Returns ret)
        //--------------------------------------------------------------
        {
            #region Проверки
            DateTime datOpen;
            if (finder.stateValidOn != "")
            {
                if (!DateTime.TryParse(finder.stateValidOn, out datOpen))
                {
                    ret = new Returns(false, "Неверно задана дата открытия ЛС");
                    return null;
                }
            }
            else
            {
                datOpen = DateTime.MinValue;
            }
            #endregion


            ret = Utils.InitReturns();
            List<Ls> spis = new List<Ls>();

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            DbTables tables = new DbTables(DBManager.getServer(conn_db));

            string sql = "";
            //string sql_part1 = "";
            //string sql_part2 = "";

            #region Собрать условия для запроса
            string _where = "";
            if (!GlobalSettings.NewGeneratePkodMode) if (finder.pkod != "") _where += " and pkod = " + finder.pkod;
            if (finder.nzp_kvar > 0) _where += " and nzp_kvar = " + finder.nzp_kvar;
            //else if (finder.kvar_s > 0 && finder.kvar_po > 0) _where += " and k.ikvar between " + finder.kvar_s + " and " + finder.kvar_po;
            if (finder.num_ls > 0) _where += " and num_ls = " + finder.num_ls;
            if (finder.pkod10 > 0) _where += " and pkod10 = " + finder.pkod10;
            if (finder.nzp_dom > 0) _where += " and k.nzp_dom = " + finder.nzp_dom;
            if (finder.nzp_area > 0) _where += " and k.nzp_area = " + finder.nzp_area + " ";
            if (finder.nzp_geu > 0) _where += " and k.nzp_geu = " + finder.nzp_geu + " ";
            if (finder.is_blocked == -1) _where += " and k.is_open <> '2' ";

            if (finder.RolesVal != null)
                foreach (_RolesVal role in finder.RolesVal)
                    if (role.tip == Constants.role_sql)
                    {
                        if (role.kod == Constants.role_sql_area) _where += " and k.nzp_area in (" + role.val + ") ";
                        else if (role.kod == Constants.role_sql_geu) _where += " and k.nzp_geu in (" + role.val + ") ";
                    }
            #endregion

            string kptable = "";
            if (finder.pkod != "" && GlobalSettings.NewGeneratePkodMode)
            {
                kptable = " and nzp_kvar in (select nzp_kvar from " + tables.kvar_pkodes + " kp where kp.pkod = "+finder.pkod+") ";
            }

            #region общее количество записей
            int countRows = 0;

            //int ikvarCount = 0;
            //if (finder.kvar_s > 0 || finder.kvar_po > 0)
            //{
            //    IDataReader reader_c;

            //    string sql_c = " Select max(ikvar) as count_r " +
            //                   " From " + tables.kvar + " k, " + tables.dom + " d, " + tables.ulica + " u " +
            //                   " Where k.nzp_dom = d.nzp_dom and d.nzp_ul = u.nzp_ul";


            //    if (finder.pkod != "") sql_c += " and pkod = " + finder.pkod;
            //    if (finder.nzp_kvar > 0) sql_c += " and nzp_kvar = " + finder.nzp_kvar;
            //    if (finder.num_ls > 0) sql_c += " and num_ls = " + finder.num_ls;
            //    if (finder.nzp_dom > 0) sql_c += " and k.nzp_dom = " + finder.nzp_dom;
            //    if (finder.is_blocked == -1) sql_c += " and k.is_open <>2 ";


            //    if (finder.RolesVal != null)
            //    {
            //        foreach (_RolesVal role in finder.RolesVal)
            //        {
            //            if (role.tip == Constants.role_sql)
            //            {
            //                if (role.kod == Constants.role_sql_area)
            //                {
            //                    sql_c += " and k.nzp_area in (" + role.val + ") ";
            //                }
            //            }
            //        }
            //    }

            //    if (!ExecRead(conn_db, out reader_c, sql_c, true).result)
            //    {
            //        conn_db.Close();
            //        return null;
            //    }

            //    while (reader_c.Read())
            //    {
            //        if (reader_c["count_r"] != DBNull.Value) ikvarCount = Convert.ToInt32(reader_c["count_r"]);
            //    }
            //    CloseReader(ref reader_c);
            //}
            //else
            //{
            // вычислить общее количество записей
#if PG
            sql = "Select count(*) From " + tables.kvar + " k left outer join " + tables.area +
                  " a on  k.nzp_area = a.nzp_area" +
                  " Where 1=1  " + kptable + _where;
#else
            sql = "Select count(*) From " + tables.kvar + " k, outer " + tables.area + " a " +
                " Where k.nzp_area = a.nzp_area " + _where;
#endif
            Object obj = ExecScalar(conn_db, sql, out ret, true);
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }

            try
            {
                countRows = Convert.ToInt32(obj);
            }
            catch
            {
                countRows = 0;
            }
            //}
            #endregion

            //string skip = (finder.skip > 0) ? " skip " + finder.skip.ToString() : "";
            //string first = (finder.rows > 0) ? " first " + finder.rows.ToString() : "";

            //sql_part1 = " Select " + skip + first; 
            
            sql = " Select  k.nzp_kvar, k.nkvar, k.nkvar_n, k.ikvar, k.fio, "+
                (GlobalSettings.NewGeneratePkodMode? "(select max(pkod) from "+tables.kvar_pkodes+" where nzp_kvar = k.nzp_kvar "+
                (finder.pkod!=""?" and pkod = " + finder.pkod : " and is_default = 1 and is_princip=0")
                +") pkod" :"round(k.pkod)||'' as pkod")+
                ", k.pkod10, k.pref, k.nzp_dom, k.num_ls, k.nzp_area, a.area, k.nzp_geu, g.geu " + 
                ", d.nzp_ul, d.ndom, d.nkor, u.ulica" +
#if PG           
                " From " + tables.kvar + " k "+
                " left outer join " + tables.dom + " d on  k.nzp_dom = d.nzp_dom " +
                " left outer join " + tables.ulica + " u on d.nzp_ul = u.nzp_ul" +
                " left outer join " + tables.area + " a on k.nzp_area = a.nzp_area"+
                " left outer join " + tables.geu + " g on k.nzp_geu = g.nzp_geu" +
                " Where 1=1  " + kptable + _where +
                " Order by ikvar, nkvar, fio ";
#else          
                " From " + tables.kvar + " k, " + tables.dom + " d, " + tables.ulica + " u " +
                ", outer " + tables.area + " a, outer " + tables.geu + " g" +
                " Where k.nzp_dom = d.nzp_dom and d.nzp_ul = u.nzp_ul and k.nzp_area = a.nzp_area and k.nzp_geu = g.nzp_geu " + _where + kptable +
                " Order by ikvar, nkvar, fio ";
#endif
            #region Доп. возможность - получить группы квартир по всему списку(дерево СУПГ)
            if (finder.is_get_group)
            {
                //определяем количество групп 
                int rem = 0;
                Math.DivRem(countRows, finder.rows, out rem);
                int grCount = rem != 0 ? countRows / finder.rows + 1 : countRows / finder.rows;

                //формируем список                
                int sk = 0;

                //цикл по групам квартир
                for (int i = 0; i < grCount; i++)
                {
                    //временный список
                    List<Ls> tempList = new List<Ls>();

                    //Запрос на группу
                    //string skip2 = (sk > 0) ? " skip " + sk.ToString() : ""; 
                    //string sql_gr = " Select " + skip2 + first + sql_part2;

                    IDataReader read_gr = null;
                    try
                    {
                        if (!ExecRead(conn_db, out read_gr, sql, true).result)
                        {
                            throw new Exception();
                        }

                        int j = 0;
                        while (read_gr.Read())
                        {
                            j++;
                            if (sk > 0 && j <= sk) continue;

                            Ls kvar_s_po = new Ls();
                            if (read_gr["nkvar"] != DBNull.Value) kvar_s_po.nkvar = Convert.ToString(read_gr["nkvar"]);
                            if (read_gr["ikvar"] != DBNull.Value) kvar_s_po.ikvar = Convert.ToInt32(read_gr["ikvar"]);
                            tempList.Add(kvar_s_po);

                            if (finder.rows > 0 && j >= finder.rows + sk) break;
                        }

                        //формируем группу
                        if (tempList.Count > 0)
                        {
                            Ls group = new Ls();
                            group.nkvar_n = tempList[0].nkvar;
                            group.kvar_s = tempList[0].ikvar;

                            group.nkvar_po = tempList[tempList.Count - 1].nkvar;
                            group.kvar_po = tempList[tempList.Count - 1].ikvar;

                            group.skip = sk;
                            group.rows = finder.rows;

                            spis.Add(group);
                        }

                        sk += finder.rows;
                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog("Ошибка получения групп квартир(СУПГ) :" + ex.Message, MonitorLog.typelog.Error, true);
                        return null;
                    }
                    finally
                    {
                        CloseReader(ref read_gr);
                    }
                }
                conn_db.Close();


                return spis;

            }
            #endregion

            IDataReader reader;
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }
            DbAdres dba = new DbAdres();
            try
            {
                _Point point;
                int i = 0;

                while (reader.Read())
                {
                    Ls zap = new Ls();
                    i++;

                    if (finder.skip > 0 && i <= finder.skip) continue;

                    if (reader["nzp_kvar"] == DBNull.Value) zap.nzp_kvar = 0; else zap.nzp_kvar = (int)reader["nzp_kvar"];
                    if (reader["nzp_dom"] == DBNull.Value) zap.nzp_dom = 0; else zap.nzp_dom = Convert.ToInt32(reader["nzp_dom"]);
                    if (reader["nzp_ul"] == DBNull.Value) zap.nzp_ul = 0; else zap.nzp_ul = Convert.ToInt32(reader["nzp_ul"]);
                    if (reader["pref"] == DBNull.Value) zap.pref = ""; else zap.pref = ((string)reader["pref"]).Trim();

                    if (reader["ulica"] == DBNull.Value) zap.ulica = ""; else zap.ulica = ((string)reader["ulica"]).Trim();
                    if (reader["ndom"] == DBNull.Value) zap.ndom = ""; else zap.ndom = ((string)reader["ndom"]).Trim();
                    if (reader["nkor"] == DBNull.Value) zap.nkor = ""; else zap.nkor = ((string)reader["nkor"]).Trim();
                    if (reader["nkvar"] == DBNull.Value) zap.nkvar = ""; else zap.nkvar = (string)reader["nkvar"];
                    if (reader["nkvar_n"] == DBNull.Value) zap.nkvar_n = ""; else zap.nkvar_n = (string)reader["nkvar_n"];

                    if (reader["nzp_area"] == DBNull.Value) zap.nzp_area = 0; else zap.nzp_area = Convert.ToInt32(reader["nzp_area"]);
                    if (reader["area"] == DBNull.Value) zap.area = ""; else zap.area = ((string)reader["area"]).Trim();

                    if (reader["nzp_geu"] == DBNull.Value) zap.nzp_geu = 0; else zap.nzp_geu = Convert.ToInt32(reader["nzp_geu"]);
                    if (reader["geu"] == DBNull.Value) zap.geu = ""; else zap.geu = ((string)reader["geu"]).Trim();

                    zap.adr = zap.getAddressFromUlica();

                    point = Points.GetPoint(zap.pref);
                    zap.nzp_wp = point.nzp_wp;
                    zap.point = point.point;

                    if (reader["fio"] != DBNull.Value)
                    {
                        zap.nkvar += " (" + (string)reader["fio"] + ")";
                        zap.fio = (string)reader["fio"];
                    }

                   // if (reader["spkod"] != DBNull.Value) zap.pkod = Convert.ToString(reader["spkod"]).Trim();

                    if (reader["pkod"] != DBNull.Value) zap.pkod = Convert.ToDecimal(reader["pkod"]).ToString("00");

                    if (reader["num_ls"] != DBNull.Value)
                    {
                        int num_ls = 0;
                        Int32.TryParse(reader["num_ls"].ToString(), out num_ls);
                        zap.num_ls = num_ls;
                    }
                    if (reader["pkod10"] != DBNull.Value) zap.pkod10 = Convert.ToInt32(reader["pkod10"]);

                    if ((zap.pref != "") && (finder.is_blocked != -1))
                    {
                        if (finder.checkstateIncalcMonth == 1)
                        {
                            RecordMonth r_m = Points.GetCalcMonth(new CalcMonthParams(zap.pref));
                            zap.stateValidOn = new DateTime(r_m.year_, r_m.month_, 1).ToShortDateString();
                        }
                        else zap.stateValidOn = finder.stateValidOn;
                        ret = dba.LoadLsState(zap, conn_db, null);
                    }
                    if (reader["ikvar"] != DBNull.Value) zap.ikvar = Convert.ToInt32(reader["ikvar"]);


                    spis.Add(zap);

                    if (finder.rows > 0 && i >= finder.rows + finder.skip) break;
                }

                CloseReader(ref reader);
                conn_db.Close();
                dba.Close();

                ret.tag = countRows;

                return spis;
            }
            catch (Exception ex)
            {
                dba.Close();
                reader.Close();
                conn_db.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err;
                if (Constants.Viewerror) err = " \n " + ex.Message;
                else err = "";
                MonitorLog.WriteLog("Ошибка заполнения LoadKvarList() " + err, MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
        }

        //--------------------------------------------------------------
        public List<Ls> LoadKvar2(Ls finder, out Returns ret)
        //--------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            if (finder.nzp_dom <= 0)
            {
                ret.result = false;
                ret.text = "Не указан дом";
                ret.tag = -1;
                return null;
            }

            List<Ls> spis = new List<Ls>();

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            //выбрать список
#if PG
            string sql = " Select nzp_kvar, num_ls, nkvar, ikvar, fio, pkod, pref From " + Points.Pref + "_data.kvar where 1=1 ";
#else
  string sql = " Select nzp_kvar, num_ls, nkvar, ikvar, fio, pkod, pref From " + Points.Pref + "_data:kvar where 1=1 ";
#endif
            if (finder.nzp_kvar > 0) sql += " and nzp_kvar = " + finder.nzp_kvar;
            if (finder.nzp_dom > 0) sql += " and nzp_dom = " + finder.nzp_dom;
            sql += " Order by ikvar,nkvar, fio ";

            IDataReader reader;
            if (!ExecRead(conn_db, out reader, sql, true).result)
            {
                conn_db.Close();
                return null;
            }
            try
            {
                while (reader.Read())
                {
                    Ls zap = new Ls();
                    if (reader["nzp_kvar"] == DBNull.Value) zap.nzp_kvar = 0;
                    else zap.nzp_kvar = (int)reader["nzp_kvar"];
                    if (reader["nkvar"] == DBNull.Value) zap.nkvar = "";
                    else zap.nkvar = (string)reader["nkvar"];
                    if (reader["pref"] == DBNull.Value) zap.pref = "";
                    else zap.pref = (string)reader["pref"];
                    if (reader["fio"] != DBNull.Value)
                    {
                        zap.nkvar += " (" + (string)reader["fio"] + ")";
                        zap.fio = (string)reader["fio"];
                    }
                    if (reader["pkod"] != DBNull.Value) zap.pkod = ((decimal)reader["pkod"]).ToString("00");
                    if (reader["num_ls"] != DBNull.Value)
                    {
                        int num_ls = 0;
                        Int32.TryParse(reader["num_ls"].ToString(), out num_ls);
                        zap.num_ls = num_ls;
                    }
                    spis.Add(zap);
                }

                reader.Close();
                conn_db.Close();
                return spis;
            }
            catch (Exception ex)
            {
                reader.Close();
                conn_db.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err;
                if (Constants.Viewerror) err = " \n " + ex.Message;
                else err = "";
                MonitorLog.WriteLog("Ошибка заполнения справочника квартир " + err, MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
        }
        //--------------------------------------------------------------
        public Ls LoadLsForKassa2(Ls finder, out Returns ret)
        //--------------------------------------------------------------
        {
            if (finder.pkod == "" && finder.nzp_kvar < 1 && finder.num_ls < 1)
            {
                ret = new Returns(false, "Не указан платежный код", -1);
                return null;
            }

            Ls zap = new Ls();

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            //найти лс
#if PG
            string sql = " Select k.nzp_kvar, k.nkvar, k.ikvar, k.fio, round(k.pkod)||'' as spkod, k.pref, k.nzp_dom, d.nzp_ul " +
                                    " From " + Points.Pref + "_data.kvar k, " + Points.Pref + "_data.dom d " +
                                    " Where k.nzp_dom = d.nzp_dom ";
#else
 string sql = " Select k.nzp_kvar, k.nkvar, k.ikvar, k.fio, round(k.pkod)||'' as spkod, k.pref, k.nzp_dom, d.nzp_ul " +
                         " From " + Points.Pref + "_data:kvar k, " + Points.Pref + "_data:dom d " +
                         " Where k.nzp_dom = d.nzp_dom ";
#endif

            if (finder.pkod != "") sql += " and pkod = " + finder.pkod;
            if (finder.nzp_kvar > 0) sql += " and nzp_kvar = " + finder.nzp_kvar;
            if (finder.num_ls > 0) sql += " and num_ls = " + finder.num_ls;

            IDataReader reader;
            if (!ExecRead(conn_db, out reader, sql, true).result)
            {
                conn_db.Close();
                return null;
            }
            try
            {
                if (!reader.Read())
                {
                    //нет данных, надо бы найти в локальных БД
                }
                else
                {
                    if (reader["nzp_kvar"] == DBNull.Value) zap.nzp_kvar = 0;
                    else zap.nzp_kvar = (int)reader["nzp_kvar"];
                    if (reader["nzp_dom"] == DBNull.Value) zap.nzp_dom = 0;
                    else zap.nzp_dom = Convert.ToInt32(reader["nzp_dom"]);
                    if (reader["nzp_ul"] == DBNull.Value) zap.nzp_ul = 0;
                    else zap.nzp_ul = Convert.ToInt32(reader["nzp_ul"]);
                    if (reader["nkvar"] == DBNull.Value) zap.nkvar = "";
                    else zap.nkvar = (string)reader["nkvar"];
                    if (reader["pref"] == DBNull.Value) zap.pref = "";
                    else zap.pref = (string)reader["pref"];
                    if (reader["fio"] != DBNull.Value)
                    {
                        zap.nkvar += " (" + (string)reader["fio"] + ")";
                        zap.fio = (string)reader["fio"];
                    }
                    if (reader["spkod"] != DBNull.Value) zap.pkod = Convert.ToString(reader["spkod"]).Trim();
                }

                reader.Close();
                conn_db.Close();
                return zap;
            }
            catch (Exception ex)
            {
                reader.Close();
                conn_db.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err;
                if (Constants.Viewerror) err = " \n " + ex.Message;
                else err = "";
                MonitorLog.WriteLog("Ошибка заполнения LoadLsForKassa " + err, MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
        }

        public List<Pack_errtype> LoadErrorTypes(Finder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            if (finder.nzp_user <= 0)
            {
                ret.result = false;
                ret.text = "Пользователь не определен";
                return null;
            }

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            List<Pack_errtype> spis = new List<Pack_errtype>();
#if PG
            string sql = "select nzp_err, name from " + Points.Pref + "_data.s_error_types where nzp_err in (1,2,3,4,5,6,7,8,666,700,701,703,800,1001) order by name";
#else
string sql = "select nzp_err, name from " + Points.Pref + "_data:s_error_types where nzp_err in (1,2,3,4,5,6,7,8,666,700,701,703,800,1001) order by name";
#endif

            IDataReader reader;
            if (!ExecRead(conn_db, out reader, sql, true).result)
            {
                conn_db.Close();
                return null;
            }
            try
            {
                while (reader.Read())
                {
                    Pack_errtype zap = new Pack_errtype();
                    if (reader["nzp_err"] == DBNull.Value) zap.nzp_err = 0;
                    else zap.nzp_err = (int)reader["nzp_err"];
                    if (reader["name"] == DBNull.Value) zap.name = "";
                    else zap.name = (string)reader["name"];
                    spis.Add(zap);
                }

                reader.Close();
                conn_db.Close();
                return spis;
            }
            catch (Exception ex)
            {
                reader.Close();
                conn_db.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err;
                if (Constants.Viewerror) err = " \n " + ex.Message;
                else err = "";
                MonitorLog.WriteLog("Ошибка заполнения справочника Тип ошибки " + err, MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }

        }

        public List<Pack_ls> GetBasketPack_ls(Pack_ls finder, out Returns ret)
        {
            var connDb = GetConnection(Points.GetConnByPref(Points.Pref));
            ret = OpenDb(connDb, true);
            if (!ret.result) return null;

            IDataReader reader = null;
            var list = new List<Pack_ls>();
            var totalNumber = 0;

            try
            {
                var whereYear = "";
                switch (finder.finYearsToShow)
                {
                    case Pack_ls.FinYearsToShow.CurrentYear :
                        whereYear = " AND yearr = " + Points.DateOper.Year;
                        break;
                    case Pack_ls.FinYearsToShow.PreviousYears:
                        whereYear = " AND yearr < " + Points.DateOper.Year;
                        break;
                }

            var where = "";
            #region Фильтр поиска
            if (finder.dat_vvod != "" || finder.dat_vvod_po != "")
            {
                DateTime dt = DateTime.MinValue, dt2 = DateTime.MinValue;
                if (finder.dat_vvod != "") DateTime.TryParse(finder.dat_vvod, out dt);
                if (finder.dat_vvod_po != "") DateTime.TryParse(finder.dat_vvod_po, out dt2);

                if (finder.dat_vvod != "" && finder.dat_vvod_po == "")
                     where += " and pls.dat_vvod = " +sDefaultSchema+ "mdy(" + dt.Month + "," + dt.Day + "," + dt.Year + ")";
                else where += " and pls.dat_vvod between " + sDefaultSchema + "mdy(" + dt.Month + "," + dt.Day + "," + dt.Year + ") " +
                              " and " + sDefaultSchema + "mdy(" + dt2.Month + "," + dt2.Day + "," + dt2.Year + ")";
            }

            if (finder.dat_pack != "" || finder.dat_pack_po != "")
            {
                DateTime dt = DateTime.MinValue, dt2 = DateTime.MinValue;
                if (finder.dat_pack != "") DateTime.TryParse(finder.dat_pack, out dt);
                if (finder.dat_pack_po != "") DateTime.TryParse(finder.dat_pack_po, out dt2);

                if (finder.dat_pack != "" && finder.dat_pack_po == "")
                     where += " and p.dat_pack = " + sDefaultSchema + "mdy(" + dt.Month + "," + dt.Day + "," + dt.Year + ")";
                else where += " and p.dat_pack between " + sDefaultSchema + "mdy(" + dt.Month + "," + dt.Day + "," + dt.Year + ") " +
                              " and " + sDefaultSchema + "mdy(" +dt2.Month + "," + dt2.Day + "," + dt2.Year + ")";
            }

            if (finder.g_sum_ls != 0) where += " and pls.g_sum_ls =" + finder.g_sum_ls;

            if (finder.num_ls > 0)
            {
                if (Points.IsSmr) where += " and k.pkod10 = " + finder.num_ls;
                else where += " and pls.num_ls = " + finder.num_ls;
            }

            if (finder.pkod != "") where += " and k.pkod = " + finder.pkod;

            if (finder.num_pack > 0) where += " and p.num_pack = '" + finder.num_pack + "'";
            if (finder.info_num > 0) where += " and pls.info_num = " + finder.info_num + "";
            if (finder.sum_pack > 0) where += " and p.sum_pack = " + finder.num_pack + "";

            var sw = "";
#if PG
            var tkvr = " left outer join " + Points.Pref + "_data.kvar k " +
                          " left outer join " + Points.Pref + "_data.dom d " +
                          " left outer join " + Points.Pref + "_data.s_ulica u " +
                          " left outer join " + Points.Pref + "_data.s_rajon r " +
                          " left outer join " + Points.Pref + "_data.s_town t " +  
                          " on r.nzp_town = t.nzp_town on r.nzp_raj = u.nzp_raj "+
                          " on d.nzp_ul = u.nzp_ul on  k.nzp_dom = d.nzp_dom on pls.num_ls = k.num_ls";
#else
            string tkvr = ", "+"outer (" + Points.Pref + "_data:kvar k, " + Points.Pref + "_data:dom d," +
                       Points.Pref + "_data:s_ulica u, " + Points.Pref + "_data:s_town t," + Points.Pref + "_data:s_rajon r) ";
#endif
            if (finder.pkod != "" && (finder.num_ls > 0 && Points.IsSmr))
            {
                tkvr = ", " + Points.Pref + "_data" + tableDelimiter + "kvar k, " + 
                    Points.Pref + "_data" + tableDelimiter + "dom d, " +
                    Points.Pref + "_data" + tableDelimiter + "s_ulica u, " +
                    Points.Pref + "_data" + tableDelimiter + "s_town t," +
                    Points.Pref + "_data" + tableDelimiter + "s_rajon r ";

                sw = " and pls.num_ls = k.num_ls and k.nzp_dom = d.nzp_dom and d.nzp_ul = u.nzp_ul " +
                     " and r.nzp_town = t.nzp_town and r.nzp_raj = u.nzp_raj";
            }
            #endregion

            #region Создать временную таблицу tableBasket с оплатами, которые в корзине
            var tableBasket = "tableBasket" + DateTime.Now.Ticks;
            ExecSQL(connDb, "drop table " + tableBasket, false);
            var sql = " select 0::int year_, pls.nzp_pack_ls nzp_pack_ls, pls.nzp_pack nzp_pack, pls.prefix_ls prefix_ls, k.nzp_kvar nzp_kvar," +
                  " k.pref pref, pls.g_sum_ls g_sum_ls, pls.sum_ls sum_ls, p.sum_pack sum_pack, p.num_pack num_pack," +
                  " p.dat_uchet pack_dat_uchet, p.dat_pack dat_pack, pls.sum_peni sum_peni, pls.dat_month dat_month, " +
                  " pls.dat_uchet packls_dat_uchet, pls.kod_sum kod_sum, pls.paysource paysource, pls.id_bill id_bill, pls.incase incase, " +
                  " pls.dat_vvod dat_vvod, pls.info_num info_num, pls.unl unl, pls.erc_code erc_code, pls.nzp_user nzp_user, pls.alg alg, pls.inbasket inbasket, " +
                  " round(k.pkod)||'' as spkod, k.num_ls num_ls, k.nkvar nkvar, k.nkvar_n nkvar_n, k.fio fio, d.nzp_dom nzp_dom," +
                  " d.ndom ndom, d.nkor nkor, u.nzp_ul nzp_ul, u.ulica ulica, trim(" + sNvlWord + "(u.ulicareg,'улица')) ulicareg, r.rajon rajon, t.town town " +
                  " into temp " + tableBasket +
                  " From " + Points.Pref + "_fin_" + (Points.DateOper.Year % 100).ToString("00") + tableDelimiter + "pack_ls pls " + tkvr +
                  ", " + Points.Pref + "_fin_" + (Points.DateOper.Year % 100).ToString("00") + tableDelimiter + "pack p " +
                  " where pls.num_ls = k.num_ls and k.nzp_dom = d.nzp_dom and d.nzp_ul = u.nzp_ul " +
                  " and r.nzp_town = t.nzp_town and r.nzp_raj = u.nzp_raj and pls.nzp_pack = p.nzp_pack and 1<>1";
            ret = ExecSQL(connDb, sql, true);
            if (!ret.result)
            {
                connDb.Close();
                return null;
            }
            #endregion

                var years = new List<int>();
                sql = "select yearr from " + Points.Pref + sKernelAliasRest + "s_baselist" +
                      " where idtype = 4 " + whereYear + " order by yearr";
                ret = ExecRead(connDb, out reader, sql, true);
                if (!ret.result) return null;
                while (reader.Read()) if (reader["yearr"] != DBNull.Value) years.Add(Convert.ToInt32(reader["yearr"]));
                CloseReader(ref reader);

                foreach (var year in years)
                {
                    var where2 = "";
                    if (finder.year_ > 0)
                        if (finder.year_ != year) continue;

                    if (finder.nzp_pack_ls > 0) where = " and nzp_pack_ls = " + finder.nzp_pack_ls;

                    var table = Points.Pref + "_fin_" + (year%100).ToString("00") + tableDelimiter + "pack_ls";
                    var table2 = Points.Pref + "_fin_" + (year%100).ToString("00") + tableDelimiter + "pack_ls_err";

                    if (!TempTableInWebCashe(connDb, table)) continue;

                    if (finder.nzp_err > 0)
                    {
                        where2 += " and pls.nzp_pack_ls in (select nzp_pack_ls from " + table2 + " e " +
                                  " where e.nzp_pack_ls = pls.nzp_pack_ls and e.nzp_err = " + finder.nzp_err + ") ";
                    }

                    var tablePack = ", " + Points.Pref + "_fin_" + (year % 100).ToString("00") + tableDelimiter + "pack ";

                    sql = "insert into " + tableBasket + "(year_, nzp_pack_ls, nzp_pack, prefix_ls, nzp_kvar," +
                          " pref, g_sum_ls, sum_ls, sum_pack, num_pack, pack_dat_uchet, dat_pack, sum_peni, dat_month, " +
                          " packls_dat_uchet, kod_sum, paysource, id_bill, incase, dat_vvod, info_num, unl, erc_code, nzp_user, alg, inbasket, " +
                          " spkod, num_ls, nkvar, nkvar_n, fio, nzp_dom, ndom, nkor, nzp_ul, ulica, ulicareg, rajon, town) " +
                          " select " + year +
                          ", pls.nzp_pack_ls, pls.nzp_pack, pls.prefix_ls, k.nzp_kvar,k.pref, pls.g_sum_ls, pls.sum_ls, p.sum_pack, p.num_pack," +
                          " p.dat_uchet, p.dat_pack, " +
                          " pls.sum_peni, pls.dat_month, pls.dat_uchet, pls.kod_sum, pls.paysource, pls.id_bill, pls.incase, " +
                          " pls.dat_vvod, pls.info_num, pls.unl, pls.erc_code, pls.nzp_user, pls.alg, pls.inbasket, " +
                          " round(k.pkod)||'' as spkod, k.num_ls, k.nkvar, k.nkvar_n, k.fio, d.nzp_dom, d.ndom, d.nkor, u.nzp_ul, u.ulica, " +
                          " trim(" + sNvlWord + "(u.ulicareg,'улица')) ulicareg, r.rajon, t.town " +
                          " From  " + table + " pls " + tkvr + tablePack + "p " + 
                          " Where 1 = 1 " + sw +
                          " and p.nzp_pack = pls.nzp_pack and pls.inbasket = 1 " + where + where2;
                    ret = ExecSQL(connDb, sql, true);
                    if (ret.result) continue;
                    return null;
                }

                sql = " select * from " + tableBasket;
                ret = ExecRead(connDb, out reader, sql, true);
                if (!ret.result) return null;
                var i = 0;
                while (reader.Read())
                {
                    i++;
                    if (i <= finder.skip) continue;
                    var packls = new Pack_ls
                    {
                        num = i.ToString(CultureInfo.InvariantCulture),
                        mark = 0,
                        nzp_user = finder.nzp_user
                    };
                    if (reader["year_"] != DBNull.Value) packls.year_ = Convert.ToInt32(reader["year_"]);
                    if (reader["nzp_kvar"] != DBNull.Value) packls.nzp_kvar = Convert.ToInt32(reader["nzp_kvar"]);
                    if (reader["num_ls"] != DBNull.Value) packls.num_ls = Convert.ToInt32(reader["num_ls"]);
                    if (reader["pref"] != DBNull.Value) packls.pref = Convert.ToString(reader["pref"]).Trim();
                    if (reader["nzp_pack"] != DBNull.Value) packls.nzp_pack = Convert.ToInt32(reader["nzp_pack"]);
                    if (reader["nzp_pack_ls"] != DBNull.Value)
                        packls.nzp_pack_ls = Convert.ToInt32(reader["nzp_pack_ls"]);
                    if (reader["prefix_ls"] != DBNull.Value) packls.prefix_ls = Convert.ToInt32(reader["prefix_ls"]);
                    if (reader["g_sum_ls"] != DBNull.Value) packls.g_sum_ls = Convert.ToDecimal(reader["g_sum_ls"]);
                    if (reader["info_num"] != DBNull.Value) packls.info_num = Convert.ToInt32(reader["info_num"]);
                    if (reader["nzp_dom"] != DBNull.Value) packls.nzp_dom = Convert.ToInt32(reader["nzp_dom"]);
                    if (reader["nzp_ul"] != DBNull.Value) packls.nzp_ul = Convert.ToInt32(reader["nzp_ul"]);
                    if (reader["dat_vvod"] != DBNull.Value)
                        packls.dat_vvod = Convert.ToDateTime(reader["dat_vvod"]).ToShortDateString();
                    if (reader["packls_dat_uchet"] != DBNull.Value)
                        packls.dat_uchet = Convert.ToDateTime(reader["packls_dat_uchet"]).ToShortDateString();
                    if (reader["dat_pack"] != DBNull.Value)
                        packls.dat_pack = Convert.ToDateTime(reader["dat_pack"]).ToShortDateString();
                    if (reader["sum_ls"] != DBNull.Value) packls.sum_ls = Convert.ToDecimal(reader["sum_ls"]);
                    if (reader["sum_pack"] != DBNull.Value) packls.sum_pack = Convert.ToDecimal(reader["sum_pack"]);
                    if (reader["kod_sum"] != DBNull.Value) packls.kod_sum = Convert.ToInt32(reader["kod_sum"]);
                    if (reader["num_pack"] != DBNull.Value) packls.snum_pack = Convert.ToString(reader["num_pack"]);

                    if (reader["nkvar"] != DBNull.Value) packls.nkvar = Convert.ToString(reader["nkvar"]).Trim();
                    if (reader["nkvar_n"] != DBNull.Value) packls.nkvar_n = Convert.ToString(reader["nkvar_n"]).Trim();
                    if (reader["fio"] != DBNull.Value) packls.fio = Convert.ToString(reader["fio"]).Trim();
                    if (reader["ndom"] != DBNull.Value) packls.ndom = Convert.ToString(reader["ndom"]).Trim();
                    if (reader["nkor"] != DBNull.Value) packls.nkor = Convert.ToString(reader["nkor"]).Trim();
                    if (reader["ulica"] != DBNull.Value)
                        packls.ulica = Convert.ToString(reader["ulicareg"]) + " " +
                                       Convert.ToString(reader["ulica"]).Trim();
                    if (reader["rajon"] != DBNull.Value) packls.rajon = Convert.ToString(reader["rajon"]).Trim();
                    if (reader["town"] != DBNull.Value) packls.town = Convert.ToString(reader["town"]).Trim();
                    if (reader["alg"] != DBNull.Value) packls.alg = Convert.ToString(reader["alg"]).Trim();
                    if (reader["inbasket"] != DBNull.Value) packls.inbasket = Convert.ToInt32(reader["inbasket"]);
                    packls.adr = packls.ulica;
                    if (packls.adr.Trim() != "") packls.adr += " / ";
                    packls.adr += packls.rajon;
                    if (packls.adr.Trim() != "") packls.adr += " / ";
                    packls.adr += packls.town;
                    if (packls.ndom != "" && packls.ndom != "-") packls.adr += ", д. " + packls.ndom;
                    if (packls.nkor != "" && packls.nkor != "-") packls.adr += ", корп. " + packls.nkor;
                    if (packls.nkvar != "" && packls.nkvar != "-") packls.adr += ", кв. " + packls.nkvar;
                    if (packls.nkvar_n != "" && packls.nkvar_n != "-") packls.adr += ", комн. " + packls.nkvar_n;

                    if (reader["spkod"] != DBNull.Value)
                    {
                        packls.pkod = Convert.ToString(reader["spkod"]).Trim();
                        if (packls.pkod != null && packls.pkod.Length > 2)
                        {
                            var prefix = packls.pkod.Substring(0, 3);
                            int iprefix;
                            if (Int32.TryParse(prefix, out iprefix)) packls.prefix_ls = iprefix;
                        }
                    }

                    if (reader["dat_month"] != DBNull.Value)
                        packls.dat_month = Convert.ToDateTime(reader["dat_month"]).ToString("MM.yyyy");

                    if (packls.inbasket == 1) packls.status = "В корзине";
                    else if (packls.incase == 1) packls.status = "В портфеле";
                    else if (packls.dat_uchet != "" && packls.alg != "" && packls.inbasket == 0)
                        packls.status = "Распределена";
                    else packls.status = "Не распределена";


                    Returns ret2;
                    var listerr = GetBasketErr(packls, connDb, out ret2);
                    packls.errors = "";
                    if (ret2.result && listerr != null && listerr.Count > 0)
                    {
                        for (var j = 0; j < listerr.Count; j++)
                        {
                            if (j == listerr.Count - 1) packls.errors += listerr[j].name;
                            else packls.errors += listerr[j].name + "; ";
                        }
                    }

                    list.Add(packls);
                    if (finder.rows > 0 && i >= finder.rows + finder.skip) break;
                }

                #region Определение новых ЛС (код ошибки = Оплата по закрытому ЛС)

                if (finder.nzp_err == (int) Pack_ls.err.lsIsClosed)
                {
                    foreach (var pls in list)
                    {
                        #region определение открытого лицевого счета по р/с, лицевому счету и литере квартиры

                        var sqlStr = " select * from " + Points.Pref + sDataAliasRest + "kvar" +
                                     " where substr(pkod" + sConvToVarChar + ", 1, 3) = substr('" + pls.pkod +
                                     "', 1, 3) and " +
                                     " substr(pkod" + sConvToVarChar + ", 6, 6) = substr('" + pls.pkod +
                                     "', 6, 6) and is_open = '1'";

                        var resDt = ClassDBUtils.OpenSQL(sqlStr, connDb).resultData.Rows;
                        if (resDt.Count != 1)
                            continue;

                        #endregion

                        pls.nzp_kvar_new = (resDt[0]["nzp_kvar"] != DBNull.Value)
                            ? Convert.ToString(resDt[0]["nzp_kvar"]).Trim()
                            : "Не определен";
                        pls.pref_new = (resDt[0]["pref"] != DBNull.Value)
                            ? Convert.ToString(resDt[0]["pref"]).Trim()
                            : "";
                        pls.num_ls_new = (resDt[0]["num_ls"] != DBNull.Value)
                            ? Convert.ToString(resDt[0]["num_ls"]).Trim()
                            : "Не определен";
                        pls.pkod_new = (resDt[0]["pkod"] != DBNull.Value)
                            ? Convert.ToDecimal(resDt[0]["pkod"]).ToString("#")
                            : "";
                        pls.nzp_wp = (resDt[0]["nzp_wp"] != DBNull.Value) ? Convert.ToInt32(resDt[0]["nzp_wp"]) : 0;
                    }
                }
                #endregion

                sql = "select sum(g_sum_ls) sum_g_sum_ls, sum(sum_ls) sum_sum_ls, count(*) count_pack_ls from " +
                      tableBasket;
                ret = ExecRead(connDb, out reader, sql, true);
                if (!ret.result) return null;
                var pkls = new Pack_ls();

                if (reader.Read())
                {
                    if (reader["sum_g_sum_ls"] != DBNull.Value)
                        pkls.g_sum_ls = Convert.ToDecimal(reader["sum_g_sum_ls"]);
                    if (reader["sum_sum_ls"] != DBNull.Value) pkls.sum_ls = Convert.ToDecimal(reader["sum_sum_ls"]);
                    if (reader["count_pack_ls"] != DBNull.Value) totalNumber = Convert.ToInt32(reader["count_pack_ls"]);
                }

                if (list != null) list.Add(pkls);

                ExecSQL(connDb, "drop table " + tableBasket, false);
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка GetBasketPack_ls " + (Constants.Viewerror ? "\n " + ex.Message : ""),
                    MonitorLog.typelog.Error, 20, 201, true);
                
                return null;
            }
            finally
            {
                CloseReader(ref reader);
                connDb.Close();
            }

            if (ret.result) ret.tag = totalNumber;
            return list;
        }

        /// <summary>
        /// Пересчитать список оплат
        /// </summary>
        /// <param name="packList">список оплат</param>
        /// <returns>результат</returns>
        public Returns ReallocatePacks(List<Pack_ls> packList)
        {
            Returns ret = Utils.InitReturns();

            using (IDbConnection conn_db = DBManager.newDbConnection(Constants.cons_Kernel))
            {
                try
                {
                    ret = OpenDb(conn_db, true);
                    if (!ret.result)
                    {
                        ret.result = false;
                        ret.text = "Отсутствует подключение к БД.";
                        ret.tag = -1;

                        return ret;
                    }

                    bool err = false;
                    int countErr = 0;
                    //список оплат для фоновых задач перераспределения
                    Dictionary<int, int> packsToFon = new Dictionary<int, int>();

                    //текущий пользователь
                    int nzp_user = 0;


                    foreach (Pack_ls pack in packList)
                    {

                        //пропустить оплаты без нового ЛС
                        if (pack.num_ls_new == "")
                        {
                            continue;
                        }

                        nzp_user = pack.nzp_user;

                        //обновить лицевой счет
                        string y = (pack.year_ - 2000).ToString("00");
#if PG
                        string sql = " update " + Points.Pref + "_fin_" + y + ".pack_ls ";
#else
  string sql = " update " + Points.Pref + "_fin_" + y + ":pack_ls ";
#endif
                        sql += " set num_ls = " + pack.num_ls_new + ", pkod = \'" + pack.pkod_new + "\' ";
                        sql += " where nzp_pack_ls = " + pack.nzp_pack_ls;

                        ret = ExecSQL(conn_db, sql, true);
                        if (!ret.result)
                        {
                            err = true;
                            countErr++;

                            continue;
                        }

                        //добавляем в список фоновых задач
                        packsToFon.Add(pack.nzp_pack_ls, pack.year_);

                        //оставляем след
                        DbCalcPack dc = new DbCalcPack();
                        //DbCalc.PackXX pXX = new DbCalc.PackXX();                        
                        //DbCalc.ParamCalc param = new DbCalc.ParamCalc();
                        //pXX.nzp_pack_ls = pack.nzp_pack_ls;
                        //pXX.paramcalc = param;
                        //param.nzp_dom = pack.nzp_pack; // вот так вот
                        //pXX.pack_log = Points.Pref + "_fin_" + y + ":pack_log ";
                        //param.nzp_wp = pack.nzp_wp;

                        PackLog pLog = new PackLog();
#if PG
                        pLog.tableName = Points.Pref + "_fin_" + y + ".pack_log ";
#else
 pLog.tableName = Points.Pref + "_fin_" + y + ":pack_log ";
#endif
                        pLog.nzp_pack = pack.nzp_pack;
                        pLog.nzp_pack_ls = pack.nzp_pack_ls;
                        pLog.dat_oper = Points.DateOper.ToShortDateString();
                        pLog.nzp_wp = pack.nzp_wp;
                        pLog.message = "Замена значений лицевого счета с " + pack.num_ls + " на " + pack.num_ls_new + ", платежного кода с " + pack.pkod + " на " + pack.pkod_new;
                        pLog.err = false;


                        ret = dc.MessageInPackLog(conn_db, null, pLog);
                        dc.Close();
                    }

                    //постановка задачи на перераспределение данной оплаты                    
                    ret = this.PutTaskDistribLs(packsToFon, nzp_user);

                    if (err)
                    {
                        ret.result = false;
                        ret.text = "Во время обновления ЛС в списке оплат возникли ошибки. Количество не обновленных оплат : " + countErr + ".";
                        ret.tag = -1;

                        return ret;
                    }
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка обновления ЛС в выбранном списке оплат. " + ex.Message, MonitorLog.typelog.Error, true);

                    ret.result = false;
                    ret.text = "Ошибка обновления ЛС в выбранном списке оплат.";
                    ret.tag = -1;
                }
            }

            return ret;
        }

        public List<Pack_errtype> GetBasketErr(Pack_ls finder, IDbConnection conn_db, out Returns ret)
        {
            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Пользователь не определен");
                return null;
            }
            if (finder.year_ < 1)
            {
                ret = new Returns(false, "Не задан год");
                return null;
            }
            if (finder.nzp_pack_ls < 1)
            {
                ret = new Returns(false, "Не выбрана оплата");
                return null;
            }

            IDataReader reader = null;
            string table;
            List<Pack_errtype> list = new List<Pack_errtype>();
            int totalNumber = 0;

#if PG
            table = Points.Pref + "_fin_" + finder.year_.ToString("0000").Substring(2, 2) + ".pack_ls_err";
#else
 table = Points.Pref + "_fin_" + finder.year_.ToString("0000").Substring(2, 2) + ":pack_ls_err";
#endif
            if (!TempTableInWebCashe(conn_db, table))
            {
                ret = new Returns(false, "Таблица " + table + " не найдена");
                return null;
            }

            string sql = "Select count(*) From " + table + " Where nzp_pack_ls = " + finder.nzp_pack_ls;

            object obj = ExecScalar(conn_db, sql, out ret, true);
            if (!ret.result) return null;
            try { totalNumber += Convert.ToInt32(obj); }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                return null;
            }

#if PG
            sql = " select nzp_err, note, (select name from " + Points.Pref + "_data.s_error_types where nzp_err = a.nzp_err) as name from " +
                          table + " a where nzp_pack_ls = " + finder.nzp_pack_ls + " order by name";
#else
  sql = " select nzp_err, note, (select name from " + Points.Pref + "_data:s_error_types where nzp_err = a.nzp_err) as name from " +
                table + " a where nzp_pack_ls = " + finder.nzp_pack_ls + " order by name";
#endif
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                return null;
            }
            try
            {
                int i = 0;
                while (reader.Read())
                {
                    i++;
                    Pack_errtype err = new Pack_errtype();
                    err.num = i.ToString();
                    err.nzp_pack_ls = finder.nzp_pack_ls;
                    err.year_ = finder.year_;
                    if (reader["nzp_err"] != DBNull.Value) err.nzp_err = Convert.ToInt32(reader["nzp_err"]);
                    if (reader["note"] != DBNull.Value) err.note = Convert.ToString(reader["note"]).Trim();
                    if (reader["name"] != DBNull.Value) err.name = Convert.ToString(reader["name"]).Trim();
                    string s = "";
                    if (err.name.Trim() != "") s += err.name;
                    if (err.note.Trim() != "" && err.name.Trim() != "") s += ": ";
                    if (err.note.Trim() != "") s += err.note;
                    err.name = s;

                    list.Add(err);
                }
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                list = null;
                MonitorLog.WriteLog("Ошибка GetBasketErr " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
            }

            reader.Close();
            if (ret.result) ret.tag = totalNumber;
            return list;
        }

        public List<Pack_errtype> GetBasketErr(Pack_ls finder, out Returns ret)
        {
            string connectionString = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            List<Pack_errtype> list = GetBasketErr(finder, conn_db, out ret);
            conn_db.Close();
            return list;
        }

        public Returns CheckPkod(Pack_ls finder)
        {
            #region Проверка входных параметров
            if (finder.nzp_user < 1)
            {
                return new Returns(false, "Пользователь не определен");
            }
            if (finder.pkod == "")
            {
                return new Returns(false, "Не задан платежный код");
            }
            #endregion

            string connectionString = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(connectionString);
            Returns ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;

            DbTables tables = new DbTables(conn_db);

            string sql = "";
            if (GlobalSettings.NewGeneratePkodMode) sql = "select count(*) from " + tables.kvar_pkodes + " where pkod = " + finder.pkod;
            else sql = "select count(*) as cnt from " + tables.kvar + " where pkod = " + finder.pkod; 

            int totalNumber = 0;
            object obj = ExecScalar(conn_db, sql, out ret, true);
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }
            try { totalNumber = Convert.ToInt32(obj); }
            catch (Exception ex)
            {
                conn_db.Close();
                ret.result = false;
                ret.text = ex.Message;
                return ret;
            }
            if (totalNumber == 0)
            {
                conn_db.Close();
                return new Returns(false, "Платежный код не существует", -1);
            }
            return new Returns(true);
        }

        public List<ChargeForDistribSum> GetSumsForDistrib(ChargeForDistribSum finder, out Returns ret)
        {
            #region проверки
            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Пользователь не определен");
                return null;
            }
            if (finder.year_ < 1)
            {
                ret = new Returns(false, "Не задан год");
                return null;
            }

            if (finder.month_ < 1)
            {
                ret = new Returns(false, "Не задан месяц");
                return null;
            }

            if (finder.nzp_pack_ls < 1)
            {
               // ret = new Returns(false, "Не задан код квитанции");
               // return null;
                return PreviouslyGetSumsForDistrib(finder, out ret);
            }

            if (finder.pack_year < 1)
            {
                ret = new Returns(false, "Не задан год");
                return null;
            }

            
            #endregion

            string pref = "", nzp_kvar = "", num_ls = "";

            string connectionString = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;
            IDataReader reader;

            List<ChargeForDistribSum> list = new List<ChargeForDistribSum>();

#if PG
            string sql = "select k.pref, k.nzp_kvar, k.num_ls from " + Points.Pref + "_fin_" + (finder.pack_year % 100).ToString("00") + ".pack_ls pls, " +
                                    Points.Pref + "_data.kvar k " + " where pls.nzp_pack_ls = " + finder.nzp_pack_ls + " and k.num_ls = pls.num_ls";
#else
 string sql = "select k.pref, k.nzp_kvar, k.num_ls from " + Points.Pref + "_fin_" + (finder.pack_year % 100).ToString("00") + ":pack_ls pls, " +
                         Points.Pref + "_data:kvar k " + " where pls.nzp_pack_ls = " + finder.nzp_pack_ls + " and k.num_ls = pls.num_ls";
#endif
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }
            if (!reader.Read())
            {
                reader.Close();
                conn_db.Close();
                ret = new Returns(false, "Не найден лицевой счет, связанный с оплатой", -1);
                return null;
            }

            if (reader["nzp_kvar"] != DBNull.Value) nzp_kvar = Convert.ToInt32(reader["nzp_kvar"]).ToString();
            if (reader["num_ls"] != DBNull.Value) num_ls = Convert.ToInt32(reader["num_ls"]).ToString();
            if (reader["pref"] != DBNull.Value) pref = Convert.ToString(reader["pref"]).Trim();


            if (nzp_kvar != "" && pref != "")
            {
#if PG
                sql = "Select ch.nzp_serv, s.service, s.ordering, sp.name_supp, sp.nzp_supp " +
                                       ", sum(sum_charge) as sum_charge " +
                                       ", sum(sum_tarif) as sum_tarif " +
                                       ", sum(sum_insaldo) as sum_insaldo " +
                                       ", sum(sum_outsaldo) as sum_outsaldo " +
                                       ", sum(gs.sum_oplat) as sum_oplat " +
                                   " From " + pref + "_charge_" + (finder.year_ % 100).ToString("00") + ".charge_" + finder.month_.ToString("00") + " ch" +
                                       " left outer join " + Points.Pref + "_fin_" + (finder.pack_year % 100).ToString("00") + ".gil_sums gs on gs.nzp_serv = ch.nzp_serv and gs.nzp_supp = ch.nzp_supp  and gs.nzp_pack_ls = " + finder.nzp_pack_ls + " and gs.num_ls = " + num_ls +
                                       ", " + pref + "_kernel.services s" +
                                       ", " + pref + "_kernel.supplier sp" +
                                   " Where ch.nzp_kvar = " + nzp_kvar +
                                   " and ch.dat_charge is null" +
                                   " and ch.nzp_serv > 1" +
                                   " and ch.nzp_serv = s.nzp_serv" +
                                   " and ch.nzp_supp = sp.nzp_supp" +                                  
                                  
                                   " group by 1, 2, 3, 4, 5";
#else
                sql = "Select ch.nzp_serv, s.service, s.ordering, sp.name_supp, sp.nzp_supp " +
                        ", sum(sum_charge) as sum_charge " +
                        ", sum(sum_tarif) as sum_tarif " +
                        ", sum(sum_insaldo) as sum_insaldo " +
                        ", sum(sum_outsaldo) as sum_outsaldo " +
                        ", sum(gs.sum_oplat) as sum_oplat " +
                    " From " + pref + "_charge_" + (finder.year_ % 100).ToString("00") + ":charge_" + finder.month_.ToString("00") + " ch" +
                        ", outer " + Points.Pref + "_fin_" + (finder.pack_year % 100).ToString("00") + ":gil_sums gs " +
                        ", " + pref + "_kernel:services s" +
                        ", " + pref + "_kernel:supplier sp" +
                    " Where ch.nzp_kvar = " + nzp_kvar +
                    " and ch.dat_charge is null" +
                    " and ch.nzp_serv > 1" +
                    " and ch.nzp_serv = s.nzp_serv" +
                    " and ch.nzp_supp = sp.nzp_supp" +
                    " and gs.nzp_pack_ls = " + finder.nzp_pack_ls + " and gs.num_ls = " + num_ls +
                    " and gs.nzp_serv = ch.nzp_serv and gs.nzp_supp = ch.nzp_supp " +
                    " group by 1, 2, 3, 4, 5";
#endif
 sql += " order by s.ordering";
                ret = ExecRead(conn_db, out reader, sql, true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return null;
                }
                int i = 0;
                while (reader.Read())
                {
                    i++;
                    ChargeForDistribSum zap = new ChargeForDistribSum();
                    zap.num = i;
                    if (reader["service"] != DBNull.Value) zap.service = Convert.ToString(reader["service"]).Trim();
                    if (reader["nzp_serv"] != DBNull.Value) zap.nzp_serv = Convert.ToInt32(reader["nzp_serv"]);
                    if (reader["ordering"] != DBNull.Value) zap.ordering = Convert.ToInt32(reader["ordering"]);
                    if (reader["name_supp"] != DBNull.Value) zap.name_supp = Convert.ToString(reader["name_supp"]).Trim();
                    if (reader["nzp_supp"] != DBNull.Value) zap.nzp_supp = Convert.ToInt32(reader["nzp_supp"]);
                    if (reader["sum_oplat"] != DBNull.Value) zap.distr_sum = Convert.ToDecimal(reader["sum_oplat"]);
                    switch (finder.etalon)
                    {
                        case 1: if (reader["sum_charge"] != DBNull.Value) zap.sum = Convert.ToDecimal(reader["sum_charge"]);
                            break;
                        case 2: if (reader["sum_tarif"] != DBNull.Value) zap.sum = Convert.ToDecimal(reader["sum_tarif"]);
                            break;
                        case 3: if (reader["sum_insaldo"] != DBNull.Value) zap.sum = Convert.ToDecimal(reader["sum_insaldo"]);
                            break;
                        case 4: if (reader["sum_outsaldo"] != DBNull.Value) zap.sum = Convert.ToDecimal(reader["sum_outsaldo"]);
                            break;
                    }

                    list.Add(zap);
                }
            }

            reader.Close();
            conn_db.Close();
            return list;
        }

        public List<ChargeForDistribSum> PreviouslyGetSumsForDistrib(ChargeForDistribSum finder, out Returns ret)
        {
            #region проверки            
            if (finder.nzp_kvar < 1)
            {
                ret = new Returns(false, "Не задан лицевой счет");
                return null;
            }

            if (finder.pref == "")
            {
                ret = new Returns(false, "Не задан префикс БД");
                return null;
            }
            #endregion

            string connectionString = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;
            IDataReader reader;

            List<ChargeForDistribSum> list = new List<ChargeForDistribSum>();

            string
            sql = "Select ch.nzp_serv, s.service, sp.name_supp, sp.nzp_supp , sum(sum_charge) as sum_charge , "+
                    " sum(sum_tarif) as sum_tarif , sum(sum_insaldo) as sum_insaldo , sum(sum_outsaldo) as sum_outsaldo "+
                    " From " + finder.pref + "_charge_" + (finder.year_ % 100).ToString("00") +tableDelimiter +"charge_" + finder.month_.ToString("00") + " ch" +
                    ", " + finder.pref + "_kernel" + tableDelimiter + "services s " +
                    ", " + finder.pref + "_kernel" + tableDelimiter + "supplier sp " +
                    " Where ch.nzp_kvar = " + finder.nzp_kvar +
                    " and ch.dat_charge is null " +
                    " and ch.nzp_serv > 1 " +
                    " and ch.nzp_serv = s.nzp_serv " +
                    " and ch.nzp_supp = sp.nzp_supp " +
                    " group by 1, 2, 3, 4";
                
                ret = ExecRead(conn_db, out reader, sql, true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return null;
                }

                while (reader.Read())
                {
                    ChargeForDistribSum zap = new ChargeForDistribSum();
                    if (reader["service"] != DBNull.Value) zap.service = Convert.ToString(reader["service"]).Trim();
                    if (reader["nzp_serv"] != DBNull.Value) zap.nzp_serv = Convert.ToInt32(reader["nzp_serv"]);
                    if (reader["name_supp"] != DBNull.Value) zap.name_supp = Convert.ToString(reader["name_supp"]).Trim();
                    if (reader["nzp_supp"] != DBNull.Value) zap.nzp_supp = Convert.ToInt32(reader["nzp_supp"]);
                
                    switch (finder.etalon)
                    {
                        case 1: if (reader["sum_charge"] != DBNull.Value) zap.sum = Convert.ToDecimal(reader["sum_charge"]);
                            break;
                        case 2: if (reader["sum_tarif"] != DBNull.Value) zap.sum = Convert.ToDecimal(reader["sum_tarif"]);
                            break;
                        case 3: if (reader["sum_insaldo"] != DBNull.Value) zap.sum = Convert.ToDecimal(reader["sum_insaldo"]);
                            break;
                        case 4: if (reader["sum_outsaldo"] != DBNull.Value) zap.sum = Convert.ToDecimal(reader["sum_outsaldo"]);
                            break;
                    }

                    list.Add(zap);
                }
           

            reader.Close();
            conn_db.Close();
            return list;
        }

        public Returns BlockPackLs(Pack_ls finder)
        {
            #region Проверка входных параметров
            if (finder.nzp_user < 1)
            {
                return new Returns(false, "Пользователь не определен");
            }
            if (finder.nzp_pack_ls < 1)
            {
                return new Returns(false, "Не задана квитанция об оплате");
            }       
            if (finder.year_ < 1)
            {
                return new Returns(false, "Год не задан");
            }   
            #endregion

            Returns ret;

            string connectionString = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;

            #region Определить пользователя
            finder.pref = Points.Pref;

            int nzpUser = finder.nzp_user;

            /*DbWorkUser db = new DbWorkUser();
            int nzpUser = db.GetLocalUser(conn_db, finder, out ret); //локальный пользователь      
            db.Close();
            if (!ret.result) return ret;*/
            #endregion

            int is_blocked =  IsBlockedRecord(nzpUser, finder.nzp_pack_ls, finder.year_, conn_db, out ret);

            if (!ret.result) return ret;
            if (is_blocked == 1)
            {
                ret = new Returns(false, "Редактирование квитанции невозможно, так как запись заблокирована другим пользователем", -1);
                return ret;
            }
            return ret;
        }

        private int IsBlockedRecord(int nzpUser, int nzp_pack_ls, int pack_year, IDbConnection conn_db, out Returns ret)
        {

            int is_blocked = -1;
            #region проверить не заблокирован, если нет то заблокировать
            IDataReader reader2;
#if PG
            string sqltxt = "select nzp_user, dat_when,  (now() - INTERVAL " + string.Format("'{0} minutes')", Constants.users_min) + " as cur_date from " +  Points.Pref +
                    "_data.pack_ls_block where nzp_pack_ls = " + nzp_pack_ls+ " and year_ = "+pack_year + " order by dat_when desc";
#else
            string sqltxt = "select nzp_user, dat_when,  (current year to second - " + Constants.users_min.ToString() + " units minute) as cur_date from " + Points.Pref +
                  "_data:pack_ls_block where nzp_pack_ls = " + nzp_pack_ls + " and year_ = " + pack_year + " order by dat_when desc";
#endif
            ret = ExecRead(conn_db, out reader2, sqltxt, true);
            if (!ret.result)
            {
                return is_blocked;
            }

            try
            {
                DateTime datwhen = DateTime.MinValue;
                DateTime curdate = DateTime.MinValue;
                int nzpuser = 0;

                if (reader2.Read())
                {
                    if (reader2["dat_when"] != DBNull.Value) datwhen = Convert.ToDateTime(reader2["dat_when"]);
                    if (reader2["cur_date"] != DBNull.Value) curdate = Convert.ToDateTime(reader2["cur_date"]);
                    if (reader2["nzp_user"] != DBNull.Value) nzpuser = Convert.ToInt32(reader2["nzp_user"]);

                    if (nzpuser > 0 && datwhen != DateTime.MinValue) //заблокирован 
                    {
                        if (nzpuser != nzpUser && curdate <= datwhen) //если заблокирована запись другим пользователем и 20 мин не прошло
                            is_blocked = 1;
                    }
                }
                reader2.Close();

                if (is_blocked != 1) //если не заблокирован или заблокирован тем же пользователем
                {
                    #region Удалить все записи
                    ret = ExecSQL(conn_db,
#if PG
                    "delete from " + Points.Pref + "_data.pack_ls_block where nzp_pack_ls = " + nzp_pack_ls + 
                    " and year_ = "+pack_year
#else
                    "delete from " + Points.Pref + "_data:pack_ls_block where nzp_pack_ls = " + nzp_pack_ls +
                    " and year_ = " + pack_year
#endif
, true);

                    if (!ret.result)
                    {
                        ret.result = false;
                        ret.text = "Ошибка удаления из таблицы pack_ls_block";
                        return is_blocked;
                    }
                    #endregion

                    #region Заблокировать л/с
                    ret = ExecSQL(conn_db,
#if PG
                    "insert into " + Points.Pref + "_data.pack_ls_block (nzp_pack_ls,year_,nzp_user, dat_when) values(" +
                    nzp_pack_ls + "," + pack_year + "," + nzpUser + ",now())"
#else
                    "insert into " + Points.Pref + "_data:pack_ls_block (nzp_pack_ls,year_,nzp_user, dat_when) values(" +
                    nzp_pack_ls + "," + pack_year + "," + nzpUser + ",current year to second)"
#endif
, true);

                    if (!ret.result)
                    {
                        ret.result = false;
                        ret.text = "Ошибка добавления записи о блокировке в таблицу pack_ls_block";
                        return is_blocked;
                    }
                    #endregion
                }
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                string err;
                if (Constants.Viewerror) err = " \n " + ex.Message;
                else err = "";
                MonitorLog.WriteLog("Ошибка получения информации о блокировки пользователя " + err, MonitorLog.typelog.Error, 20, 201, true);
                return is_blocked;
            }
            #endregion

            return is_blocked;
        }

        public Returns SaveManualDistrib(List<ChargeForDistribSum> listfinder)
        {
            Returns ret;

            if (listfinder.Count == 0) return new Returns(false, "Нет данных для сохранения", -1);

            #region проверки
            if (listfinder[0].nzp_user < 1) return new Returns(false, "Пользователь не определен");
            if (listfinder[0].year_ < 1) return new Returns(false, "Не задан год");
            if (listfinder[0].month_ < 1) return new Returns(false, "Не задан месяц");
            if (listfinder[0].pack_year < 1) return new Returns(false, "Не задан год");
            if (listfinder[0].nzp_pack_ls < 1) return new Returns(false, "Не задан код квитанции");
            #endregion

            string pref = "", num_ls = "";
            int pack_year = 0, nzp_pack_ls = 0;
                Pack_ls pls = new Pack_ls();
           

            pack_year = listfinder[0].pack_year;
            nzp_pack_ls = listfinder[0].nzp_pack_ls;

            string connectionString = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;

            #region Определить пользователя
            listfinder[0].pref = Points.Pref;

            int nzpUser = listfinder[0].nzp_user;

            /*DbWorkUser db = new DbWorkUser();
            int nzpUser = db.GetLocalUser(conn_db, listfinder[0], out ret); //локальный пользователь      
            db.Close();
            if (!ret.result) return ret;*/
            #endregion

            int is_blocked = IsBlockedRecord(nzpUser, nzp_pack_ls, pack_year, conn_db, out ret);
            if (!ret.result) return ret;
            if (is_blocked == 1)
            {
                ret = new Returns(false, "Редактирование сумм оплат по услугам невозможно, так как запись заблокирована другим пользователем", -1);
                return ret;
            }

            IDataReader reader;

            string sql = "select k.pref, k.num_ls, pls.kod_sum, p.pack_type, p.nzp_supp , p.nzp_payer, payer, name_supp from " +
                         Points.Pref + "_fin_" + (pack_year % 100).ToString("00") + ".pack_ls pls" +
                         " left outer join " + Points.Pref + "_fin_" + (pack_year % 100).ToString("00") + ".pack p on p.nzp_pack = pls.nzp_pack " +
                         " left outer join " + Points.Pref + "_kernel" + tableDelimiter + "s_payer sp on sp.nzp_payer = p.nzp_payer "+
                         " left outer join " + Points.Pref + "_kernel" + tableDelimiter + "supplier supp on supp.nzp_supp = p.nzp_supp " + 
                          ", " +
                         Points.Pref + "_data.kvar k " + 
                         " where pls.nzp_pack_ls = " + nzp_pack_ls + " and k.num_ls = pls.num_ls";


            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }
            if (!reader.Read())
            {
                reader.Close();
                conn_db.Close();
                ret = new Returns(false, "Не найден лицевой счет, связанный с оплатой", -1);
                return ret;
            }

            if (reader["num_ls"] != DBNull.Value) num_ls = Convert.ToInt32(reader["num_ls"]).ToString();
            if (reader["pref"] != DBNull.Value) pref = Convert.ToString(reader["pref"]).Trim();
            if (reader["kod_sum"] != DBNull.Value) pls.kod_sum = Convert.ToInt32(reader["kod_sum"]);
            if (reader["nzp_supp"] != DBNull.Value) pls.nzp_supp = Convert.ToInt32(reader["nzp_supp"]);
            if (reader["nzp_payer"] != DBNull.Value) pls.nzp_payer = Convert.ToInt32(reader["nzp_payer"]);
            if (reader["pack_type"] != DBNull.Value) pls.pack_type = Convert.ToInt32(reader["pack_type"]);
            if (reader["payer"] != DBNull.Value) pls.payer = Convert.ToString(reader["payer"]).Trim();
            if (reader["name_supp"] != DBNull.Value) pls.name_supp = Convert.ToString(reader["name_supp"]).Trim();
            
            reader.Close();
            
            //проверка
            if (pls.pack_type == 20)
            {
                if (pls.kod_sum == 49)
                {
                    List<int> listsupp = new List<int>(); 
                    sql = "select nzp_supp from " + Points.Pref + "_kernel" + tableDelimiter +
                          "supplier where nzp_payer_princip = " + pls.nzp_payer;
                    ret = ExecRead(conn_db, out reader, sql, true);
                    if (!ret.result)
                    {
                        conn_db.Close();
                        return ret;
                    }
                    while (reader.Read())
                    {
                        if (reader["nzp_supp"] != DBNull.Value) listsupp.Add(Convert.ToInt32(reader["nzp_supp"]));
                    }
                    reader.Close();
                    foreach (ChargeForDistribSum finder in listfinder)
                        if (finder.distr_sum != 0 && !listsupp.Contains(finder.nzp_supp))
                        {
                            reader.Close();
                            conn_db.Close();
                            return new Returns(false, "Есть уточнения оплаты по другим принципалам, отличающимся от "+pls.payer, -1);
                        }
                }
                else if (pls.kod_sum == 50)
                {
                    foreach (ChargeForDistribSum finder in listfinder)
                    if (finder.nzp_supp != pls.nzp_supp && finder.distr_sum != 0)
                    {
                        reader.Close();
                        conn_db.Close();
                        return new Returns(false, "Есть уточнения оплаты по другим договорам, отличающимся от " + pls.name_supp, -1);
                    }
                }
                
            }

            ret = DeleteManualDistrib(conn_db, pack_year, nzp_pack_ls,  (num_ls!="")?Convert.ToInt32(num_ls):0);
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }

            foreach (ChargeForDistribSum finder in listfinder)
            {
#if PG
                sql = " insert into " + Points.Pref + "_fin_" + (finder.pack_year % 100).ToString("00") + ".gil_sums " +
                                      " (nzp_pack_ls,num_ls,nzp_serv,nzp_supp,sum_oplat,dat_month)" +
                                      " values (" + finder.nzp_pack_ls + ", " + num_ls + "," + finder.nzp_serv + "," + finder.nzp_supp + "," + finder.distr_sum + ",'" + finder.dat_month + "')";
#else
sql = " insert into " + Points.Pref + "_fin_" + (finder.pack_year % 100).ToString("00") + ":gil_sums " +
                      " (nzp_pack_ls,num_ls,nzp_serv,nzp_supp,sum_oplat,dat_month)" +
                      " values (" + finder.nzp_pack_ls + ", " + num_ls + "," + finder.nzp_serv + "," + finder.nzp_supp + "," + finder.distr_sum + ",'" + finder.dat_month + "')";
#endif
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return ret;
                }
            }
            return ret;
        }

        public Returns DeleteManualDistrib(Pack_ls finder)
        {
            if (finder.year_ <= 0) return new Returns(false, "Не задан финансовый год оплаты", -1);
            if (finder.nzp_pack_ls <= 0 && finder.nzp_pack <= 0) return new Returns(false, "Оплата или пачка оплат не выбрана", -1);

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            Returns ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;

            if (finder.nzp_pack_ls > 0) ret =  DeleteManualDistrib(conn_db, finder.year_, finder.nzp_pack_ls, 0);
            else DeleteManualDistrib(conn_db, finder.year_, finder.nzp_pack);
            conn_db.Close();
            return ret;
        }

        public Returns DeleteManualDistrib(IDbConnection conn_db, int pack_year, int nzp_pack_ls, int num_ls)
        {           
#if PG
            string sql = " delete from " + Points.Pref + "_fin_" + (pack_year % 100).ToString("00") + ".gil_sums " +
                         " where nzp_pack_ls = " + nzp_pack_ls ;//+ " and num_ls = " + num_ls;
#else
            string sql = " delete from " + Points.Pref + "_fin_" + (pack_year % 100).ToString("00") + ":gil_sums " +
                         " where nzp_pack_ls = " + nzp_pack_ls;// +" and num_ls = " + num_ls;
#endif
            Returns ret = ExecSQL(conn_db, sql, true);          
            return ret;          
        }

        public Returns DeleteManualDistrib(IDbConnection conn_db, int pack_year, int nzp_pack)
        {
            string sql = " delete from " + Points.Pref + "_fin_" + (pack_year % 100).ToString("00") + tableDelimiter +"gil_sums " +
                         " where nzp_pack_ls in (select nzp_pack_ls from "+
                         Points.Pref + "_fin_" + (pack_year % 100).ToString("00") + tableDelimiter + "pack_ls where nzp_pack = "+
                         nzp_pack+")";
            Returns ret = ExecSQL(conn_db, sql, true);
            return ret;
        }

        public Returns GetPrincipForManualDistrib(List<ChargeForDistribSum> listfinder, out List<ChargeForDistribSum> res)
        {
            res = new List<ChargeForDistribSum>();
            var supps = "";
            foreach (var finder in listfinder)
            {
                if (supps == "") supps += finder.nzp_supp;
                else supps += "," + finder.nzp_supp;
            }
            if (supps == "") return new Returns(false, "Не указаны договора", -1);

            var connDb = GetConnection(Constants.cons_Kernel);
            var  ret = OpenDb(connDb, true);
            if (!ret.result) return ret;
            MyDataReader reader;
            var sb = new StringBuilder();
            sb.AppendFormat("select nzp_supp, nzp_payer_princip from {0}_kernel{1}supplier where nzp_supp in ({2})",
                Points.Pref, tableDelimiter, supps);
            ret = ExecRead(connDb, out reader, sb.ToString(), true);
            if (!ret.result)
            {
                connDb.Close();
                return ret;
            }

            while (reader.Read())
            {
                int nzpsupp = 0, nzppayerprincip = 0;
                if (reader["nzp_supp"] != DBNull.Value) nzpsupp = Convert.ToInt32(reader["nzp_supp"]);
                if (reader["nzp_payer_princip"] != DBNull.Value) nzppayerprincip = Convert.ToInt32(reader["nzp_payer_princip"]);
                foreach (var finder in listfinder.Where(finder => nzpsupp == finder.nzp_supp))
                {
                    finder.nzp_payer_princip = nzppayerprincip;
                    ChargeForDistribSum p = new ChargeForDistribSum();
                    p.nzp_supp = finder.nzp_supp;
                    p.nzp_payer_princip = finder.nzp_payer_princip;
                    res.Add(p);
                    break;
                }
            }
            reader.Close();
            connDb.Close();
            return ret;
        }

        /// <summary>
        /// получение списка лицевых счетов для закрытого лицевого счета
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        /// 
        public List<Ls> GetPackLsList(string finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            
            if(finder.Length < 9)
            {
                return null;
            }

            List<Ls> list = new List<Ls>();

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            try
            {
                foreach (var cur_bank in Points.PointList)
                {

#if PG
                    string sql = "select  ul.ulica, ul.nzp_ul, d. nzp_dom, d.nkor, d.ndom, k.nkvar, k.nzp_kvar, g.geu, k.fio,"+
                                 " k.pkod10, r.nzp_raj, r.rajon, t.nzp_town, t.town, "+
                                 " round(k.pkod)||'' as spkod from " + cur_bank.pref + "_data.kvar k, " + cur_bank.pref + "_data.dom d, " +
                                 cur_bank.pref + "_data.s_rajon r, "+ cur_bank.pref + "_data.s_town t, "+
                                 cur_bank.pref + "_data.s_ulica ul, " + Points.Pref + "_data.s_geu g " +
                                 " where d.nzp_dom = k.nzp_dom and ul.nzp_ul = d.nzp_ul and g.nzp_geu = k.nzp_geu and " +
                                 "  r.nzp_raj=ul.nzp_raj and t.nzp_town=r.nzp_town and substr(pkod::char, 6, 5) = " + finder.Substring(5, 5)+"::char";
#else
                    string sql = "select ul.ulica, ul.nzp_ul, d. nzp_dom, d.nkor, d.ndom, k.nkvar, k.nzp_kvar, g.geu, k.fio,"+
                                 " k.pkod10, r.nzp_raj, r.rajon, t.nzp_town, t.town, " +
                                 "  round(k.pkod)||'' as spkod from " + cur_bank.pref + 
                                 "_data:kvar k, " + cur_bank.pref + "_data:dom d, " + cur_bank.pref + "_data:s_rajon r, "+
                                  cur_bank.pref + "_data:s_town t, "+
                                 cur_bank.pref + "_data:s_ulica ul, " + Points.Pref + "_data:s_geu g " +
                                 " where d.nzp_dom = k.nzp_dom and ul.nzp_ul = d.nzp_ul and g.nzp_geu = k.nzp_geu " +
                                 " and r.nzp_raj=ul.nzp_raj and t.nzp_town=r.nzp_town and substr(pkod, 6, 5) = " + finder.Substring(5, 5);
#endif
                    var dt = ClassDBUtils.OpenSQL(sql, conn_db);
                    for (int i = 0; i < dt.resultData.Rows.Count; i++)
                    {
                        var pkod = (dt.resultData.Rows[i]["spkod"]).ToString().Trim();
                        var rs = pkod.Substring(0, 3);
                        var geu = dt.resultData.Rows[i]["geu"].ToString().Trim();
                        var fio = dt.resultData.Rows[i]["fio"].ToString().Trim();
                        var pkod10 = dt.resultData.Rows[i]["pkod10"].ToString().Trim();                    
                        var kor = "";
                        if (dt.resultData.Rows[i]["nkor"].ToString().Trim() != "-")
                            kor = dt.resultData.Rows[i]["nkor"].ToString().Trim() + ", ";
                        var adr = dt.resultData.Rows[i]["ulica"].ToString().Trim() + ", " +
                                  "д. " + dt.resultData.Rows[i]["ndom"].ToString().Trim() + ", " +
                                  kor +
                                  "кв. " + dt.resultData.Rows[i]["nkvar"].ToString().Trim() + "";
                        var liter = "";
                        if (Points.IsSmr)
                        {
                            if (pkod.Substring(10, 1) != "0")
                                liter = "литера " + pkod.Substring(10, 1) + ", ";
                        }
                        list.Add(new Ls()
                        {
                            nzp_kvar = Convert.ToInt32(dt.resultData.Rows[i]["nzp_kvar"]),
                            remark = "Р/с " + rs + ", " + geu + ", л/с " + pkod10 + ", " + liter + fio + " (" + adr + ")",
                            nzp_ul = Convert.ToInt32(dt.resultData.Rows[i]["nzp_ul"]),
                            nzp_dom = Convert.ToInt32(dt.resultData.Rows[i]["nzp_dom"]),
                            nzp_raj = Convert.ToInt32(dt.resultData.Rows[i]["nzp_raj"]),
                            nzp_town = Convert.ToInt32(dt.resultData.Rows[i]["nzp_town"]),
                            pkod = pkod,
                            fio = fio,
                            nkvar = (dt.resultData.Rows[i]["nkvar"]).ToString().Trim() + " (" + fio + " - " + geu + ")",
                            ulica = (dt.resultData.Rows[i]["ulica"]).ToString().Trim(),
                            ndom = (dt.resultData.Rows[i]["ndom"]).ToString().Trim(),
                            pref = cur_bank.pref
                        });
                    }
                }

                conn_db.Close();
                return list;
            }
            catch (Exception ex)
            {
                conn_db.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err;
                if (Constants.Viewerror) err = " \n " + ex.Message;
                else err = "";
                MonitorLog.WriteLog("Ошибка заполнения справочника улиц " + err, MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
        }
    } //end class

} //end namespace