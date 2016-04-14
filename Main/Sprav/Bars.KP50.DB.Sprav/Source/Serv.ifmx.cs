using System;
using System.Data;
using System.Globalization;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections.Generic;
using Castle.MicroKernel.ModelBuilder.Descriptors;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;


namespace STCLINE.KP50.DataBase
{
    //----------------------------------------------------------------------
    public class DbLsServices : DataBaseHead
    //----------------------------------------------------------------------
    {
#if PG
        private readonly string pgDefaultDb = "public";
#else
#endif

        /// <summary> Возвращает список услуг квартиры с указанием поставщика для услуг, действующих в расчетном месяце
        /// </summary>
        /// <param name="finder">Обязательные параметры: nzp_user, month_, year_, pref, nzp_kvar</param>
        public List<Service> FindLsServices(Service finder, out Returns ret)
        {
            if (Utils.GetParams(finder.prms, Constants.page_spisnddom)) return FindLsServicesDom(finder, out ret);

            #region Проверка входных параметров
            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Не определен пользователь",-1);
                return null;
            }
            if (finder.pref.Trim() == "")
            {
                ret = new Returns(false, "Не определен префикс",-1);
                return null;
            }
            if (!Utils.GetParams(finder.prms, Constants.page_groupspisserv) &&
                !Utils.GetParams(finder.prms, Constants.page_group_spis_serv_dom))
            {
                if (!Utils.GetParams(finder.prms, Constants.page_spisls) &&
                    !Utils.GetParams(finder.prms, Constants.page_spisdom) &&
                     !Utils.GetParams(finder.prms, Constants.page_spisservdom))
                {
                    if (finder.nzp_kvar < 1)
                    {
                        ret = new Returns(false, "Не задана квартира",-1);
                        return null;
                    }
                }

                if (finder.month_ < 1)
                {
                    ret = new Returns(false, "Не задан расчетный месяц",-1);
                    return null;
                }

                if (finder.year_ < 1)
                {
                    ret = new Returns(false, "Не задан расчетный год",-1);
                    return null;
                }
            }
            #endregion

            string connectionString = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            List<Service> list = new List<Service>();

            IDataReader reader, reader3;
            string sql = "", where = "";

            if (finder.RolesVal != null)
                foreach (_RolesVal role in finder.RolesVal)
                {
                    if (role.tip == Constants.role_sql & role.kod == Constants.role_sql_serv) where += " and t.nzp_serv in (" + role.val + ")";
                    if (role.tip == Constants.role_sql & role.kod == Constants.role_sql_supp) where += " and t.nzp_supp in (" + role.val + ")";
                }

            if (finder.nzp_serv > 0) where += " and s.nzp_serv = " + finder.nzp_serv;

            if (Utils.GetParams(finder.prms, Constants.page_spisls) || Utils.GetParams(finder.prms, Constants.page_spisdom))
            {
                #region соединение с бд Webdata
                IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
                ret = OpenDb(conn_web, true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return null;
                }
                #endregion

                if (Utils.GetParams(finder.prms, Constants.page_spisls))
                {
                    string tXX_spls = "t" + finder.nzp_user + "_spls";
                    if (!TableInWebCashe(conn_web, tXX_spls))
                    {
                        conn_db.Close();
                        conn_web.Close();
                        ret = new Returns(false, "Лицевые счета не выбраны", -1);
                        return null;
                    }
#if PG
                    tXX_spls = "public." + tXX_spls;
#else
                    tXX_spls = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXX_spls;
#endif
#if PG
                    sql = "Select t.nzp_supp, s.name_supp, count(*) as num, nzp_frm From " + finder.pref + "_data.tarif t, " + tXX_spls + " a, " + finder.pref + "_kernel.supplier s" +
                        " Where t.nzp_kvar = a.nzp_kvar and t.nzp_serv = " + finder.nzp_serv + " and is_actual <> 100" +
                            " and '01." + finder.month_.ToString("00") + "." + finder.year_.ToString("0000") + "' between t.dat_s and t.dat_po" +
                            " and t.nzp_supp = s.nzp_supp" +
                        " Group by t.nzp_supp, s.name_supp, nzp_frm" +
                        " Order by 3 desc";
#else
                    sql = "Select t.nzp_supp, s.name_supp, count(*) as num, nzp_frm From " + finder.pref + "_data:tarif t, " + tXX_spls + " a, " + finder.pref + "_kernel:supplier s" +
                        " Where t.nzp_kvar = a.nzp_kvar and t.nzp_serv = " + finder.nzp_serv + " and is_actual <> 100" +
                            " and '01." + finder.month_.ToString("00") + "." + finder.year_.ToString("0000") + "' between t.dat_s and t.dat_po" +
                            " and t.nzp_supp = s.nzp_supp" +
                        " Group by t.nzp_supp, s.name_supp, nzp_frm" +
                        " Order by 3 desc";
#endif
                }
                else if (Utils.GetParams(finder.prms, Constants.page_spisdom))
                {
                    string tXX_spdom = "t" + finder.nzp_user + "_spdom";
                    if (!TableInWebCashe(conn_web, tXX_spdom))
                    {
                        conn_db.Close();
                        conn_web.Close();
                        ret = new Returns(false, "Дома не выбраны", -1);
                        return null;
                    }
#if PG
                    tXX_spdom = "public." + tXX_spdom;
#else
                    tXX_spdom = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXX_spdom;
#endif
#if PG
                    sql = "Select t.nzp_supp, s.name_supp, count(*) as num, nzp_frm From " + finder.pref + "_data.tarif t, " + finder.pref + "_data.kvar k, " + tXX_spdom + " a, " + finder.pref + "_kernel.supplier s" +
                        " Where t.nzp_kvar = k.nzp_kvar and k.nzp_dom = a.nzp_dom and t.nzp_serv = " + finder.nzp_serv + " and is_actual <> 100" +
                            " and '01." + finder.month_.ToString("00") + "." + finder.year_.ToString("0000") + "' between t.dat_s and t.dat_po" +
                            " and t.nzp_supp = s.nzp_supp" +
                        " Group by t.nzp_supp, s.name_supp, nzp_frm" +
                        " Order by 3 desc";
#else
                    sql = "Select t.nzp_supp, s.name_supp, count(*) as num, nzp_frm From " + finder.pref + "_data:tarif t, " + finder.pref + "_data:kvar k, " + tXX_spdom + " a, " + finder.pref + "_kernel:supplier s" +
                        " Where t.nzp_kvar = k.nzp_kvar and k.nzp_dom = a.nzp_dom and t.nzp_serv = " + finder.nzp_serv + " and is_actual <> 100" +
                            " and '01." + finder.month_.ToString("00") + "." + finder.year_.ToString("0000") + "' between t.dat_s and t.dat_po" +
                            " and t.nzp_supp = s.nzp_supp" +
                        " Group by t.nzp_supp, s.name_supp, nzp_frm" +
                        " Order by 3 desc";
#endif
                }

                conn_web.Close();

                ret = ExecRead(conn_db, out reader, sql, true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return null;
                }

                if (reader.Read())
                {
                    Service zap = new Service();
                    if (reader["nzp_supp"] != DBNull.Value) zap.nzp_supp = Convert.ToInt32(reader["nzp_supp"]);
                    if (reader["name_supp"] != DBNull.Value) zap.name_supp = Convert.ToString(reader["name_supp"]).Trim();
                    if (reader["nzp_frm"] != DBNull.Value) zap.nzp_frm = Convert.ToInt32(reader["nzp_frm"]);
                    list.Add(zap);
                }
                ret.tag = list.Count;
            }
            else
            {
                if (finder.is_actual == 1)
                {
                    //выбрать полный список услуг, когда либо оказываемый данной квартире
#if PG
                    sql = "Select distinct t.nzp_serv, s.service,s.ordering from " + finder.pref.Trim() + "_data.tarif t, " + finder.pref.Trim() + "_kernel.services s " +
                        " Where nzp_kvar = " + finder.nzp_kvar +
                        " and is_actual <> 100 " +
                        " and t.nzp_serv = s.nzp_serv" + where +
                        " Order by s.service";
#else
                    sql = "Select unique t.nzp_serv, s.service,s.ordering from " + finder.pref.Trim() + "_data:tarif t, " + finder.pref.Trim() + "_kernel:services s " +
                        " Where nzp_kvar = " + finder.nzp_kvar +
                        " and is_actual <> 100 " +
                        " and t.nzp_serv = s.nzp_serv" + where +
                        " Order by s.ordering";
#endif
                }
                else
                {
                    //выбрать полный список услуг, доступный в данной БД

                    if (Utils.GetParams(finder.prms, Constants.page_spisservdom))
                    {
                        //
                    }
#if PG
                    sql = " Select distinct t.nzp_serv, s.service, s.ordering from " + finder.pref.Trim() + "_kernel.l_foss t, " + finder.pref.Trim() + "_kernel.services s " +
                        " Where t.nzp_serv = s.nzp_serv and t.nzp_serv > 1 " + where +
                        " Order by s.service";
#else
                    sql = " Select unique t.nzp_serv, s.service, s.ordering from " + finder.pref.Trim() + "_kernel:l_foss t, " + finder.pref.Trim() + "_kernel:services s " +
                        " Where t.nzp_serv = s.nzp_serv and t.nzp_serv > 1 " + where +
                        " Order by s.ordering";
#endif
                }

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
                    if (finder.skip > 0 && i <= finder.skip) continue;
                    if (i > finder.skip + finder.rows) continue;
                    Service zap = new Service();

                    if (reader["nzp_serv"] != DBNull.Value) zap.nzp_serv = (int)reader["nzp_serv"];
                    if (reader["service"] != DBNull.Value) zap.service = ((string)reader["service"]).Trim();

                    if (!Utils.GetParams(finder.prms, Constants.page_groupspisserv) &&
                        !Utils.GetParams(finder.prms, Constants.page_group_spis_serv_dom))
                    {
                        IDataReader reader2;
                        sql = "select t.nzp_tarif, t.nzp_supp, s.name_supp, s.nzp_payer_princip, t.nzp_frm from " +
#if PG
 finder.pref.Trim() + "_data.tarif t, " + finder.pref.Trim() + "_kernel.supplier s " +
                            " where nzp_kvar = " + finder.nzp_kvar + " and nzp_serv = " + zap.nzp_serv +
                            " and is_actual <> 100 " +
                            " and '01." + finder.month_.ToString("00") + "." + finder.year_.ToString("0000") + "' between dat_s and dat_po " +
                            " and t.nzp_supp = s.nzp_supp";
#else
                            finder.pref.Trim() + "_data:tarif t, " + finder.pref.Trim() + "_kernel:supplier s " +
                            " where nzp_kvar = " + finder.nzp_kvar + " and nzp_serv = " + zap.nzp_serv +
                            " and is_actual <> 100 " +
                            " and '01." + finder.month_.ToString("00") + "." + finder.year_.ToString("0000") + "' between dat_s and dat_po " +
                            " and t.nzp_supp = s.nzp_supp";
#endif
                        ret = ExecRead(conn_db, out reader2, sql, true);
                        if (!ret.result)
                        {
                            reader.Close();
                            conn_db.Close();
                            return null;
                        }
                        bool exist = false;
                        while (reader2.Read())
                        {
                            SupplierFinder supp = new SupplierFinder();
                            if (reader2["nzp_supp"] != DBNull.Value) supp.nzp_supp = (int)reader2["nzp_supp"];
                            if (reader2["nzp_payer_princip"] != DBNull.Value) supp.nzp_payer_princip = (int)reader2["nzp_payer_princip"];
                            if (reader2["name_supp"] != DBNull.Value) supp.name_supp = ((string)reader2["name_supp"]).Trim();
                            if (reader2["nzp_frm"] != DBNull.Value) supp.nzp_frm = (int)reader2["nzp_frm"];
                            zap.activePeriod = 1;
                            zap.nzp_supp = supp.nzp_supp;
                            zap.nzp_frm = supp.nzp_frm;
                            zap.name_supp = supp.name_supp;
                            zap.list_supp.Add(supp);

                            if (GlobalSettings.NewGeneratePkodMode)
                            {
                                List<KvarPkodes> list_kvar_pkodes = new List<KvarPkodes>();
                                sql = "select pkod, is_default from " + Points.Pref + "_data" + tableDelimiter + "kvar_pkodes where nzp_kvar = " + finder.nzp_kvar +
                                      " and is_princip = 1 and nzp_payer = " + supp.nzp_payer_princip;
                                ret = ExecRead(conn_db, out reader3, sql, true);
                                if (!ret.result)
                                {
                                    reader2.Close();
                                    reader.Close();
                                    conn_db.Close();
                                    return null;
                                }
                                string pkodes = "";
                                while (reader3.Read())
                                {
                                    KvarPkodes kp = new KvarPkodes();
                                    kp.pkod = (reader3["pkod"] != DBNull.Value) ? Convert.ToString(reader3["pkod"]) : "";
                                    kp.is_default = (reader3["is_default"] != DBNull.Value) ? Convert.ToInt32(reader3["is_default"]) : 0;

                                    if (kp.is_default == 1)
                                    {
                                        if (pkodes == "") pkodes += kp.pkod;
                                        else pkodes += ", " + kp.pkod;
                                    }
                                    else
                                    {
                                        if (pkodes == "") pkodes += " <span style='color: red'>" + kp.pkod + "</span>";
                                        else pkodes += ", " + " <span style='color: red'>" + kp.pkod + "</span>";
                                    }
                                    list_kvar_pkodes.Add(kp);
                                }
                                reader3.Close();
                                zap.pkodes = pkodes;
                                zap.list_kvar_pkodes = list_kvar_pkodes;
                            }
                        }
                        reader2.Close();
                        if (!exist) // проверить, есть ли хоть один действующий период оказания услуги
                        {
                            reader2.Close();
#if PG
                            sql = "select t.nzp_tarif from " + finder.pref.Trim() + "_data.tarif t " +
                                " where nzp_kvar = " + finder.nzp_kvar + " and nzp_serv = " + zap.nzp_serv + " and is_actual <> 100 limit 1";
#else
                            sql = "select first 1 t.nzp_tarif from " + finder.pref.Trim() + "_data:tarif t " +
                                " where nzp_kvar = " + finder.nzp_kvar + " and nzp_serv = " + zap.nzp_serv + " and is_actual <> 100 ";
#endif
                            ret = ExecRead(conn_db, out reader2, sql, true);
                            if (!ret.result)
                            {
                                reader.Close();
                                conn_db.Close();
                                return null;
                            }
                            if (reader2.Read()) zap.activePeriod = 0;
                            else zap.activePeriod = -1;
                            reader2.Close();
                        }
                    }

                    list.Add(zap);
                }
                ret.tag = i;
            }
            if (reader != null) reader.Close();
            conn_db.Close();
            return list;
        }

        public List<Service> NewFdFindLsServices(Service finder, out Returns ret)
        {
            if (Utils.GetParams(finder.prms, Constants.page_spisnddom)) return FindLsServicesDom(finder, out ret);

            #region Проверка входных параметров
            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Не определен пользователь",-1);
                return null;
            }
            if (finder.pref.Trim() == "")
            {
                ret = new Returns(false, "Не определен префикс",-1);
                return null;
            }
            if (!Utils.GetParams(finder.prms, Constants.page_newfdgroupspisserv) &&
                !Utils.GetParams(finder.prms, Constants.page_newfd_group_spis_serv_dom))
            {
                if (!Utils.GetParams(finder.prms, Constants.page_spisls) &&
                    !Utils.GetParams(finder.prms, Constants.page_spisdom) &&
                     !Utils.GetParams(finder.prms, Constants.page_spisservdom) &&
                     !Utils.GetParams(finder.prms, Constants.page_new_spisservdom))
                {
                    if (finder.nzp_kvar < 1)
                    {
                        ret = new Returns(false, "Не задана квартира",-1);
                        return null;
                    }
                }

                if (finder.month_ < 1)
                {
                    ret = new Returns(false, "Не задан расчетный месяц",-1);
                    return null;
                }

                if (finder.year_ < 1)
                {
                    ret = new Returns(false, "Не задан расчетный год",-1);
                    return null;
                }
            }
            #endregion

            string connectionString = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            List<Service> list = new List<Service>();

            IDataReader reader, reader3;
            string sql = "", where = "";

            if (finder.RolesVal != null)
                foreach (_RolesVal role in finder.RolesVal)
                {
                    if (role.tip == Constants.role_sql & role.kod == Constants.role_sql_serv) where += " and t.nzp_serv in (" + role.val + ")";
                    if (role.tip == Constants.role_sql & role.kod == Constants.role_sql_supp) where += " and t.nzp_supp in (" + role.val + ")";
                }

            if (finder.nzp_serv > 0) where += " and s.nzp_serv = " + finder.nzp_serv;

            if (Utils.GetParams(finder.prms, Constants.page_spisls) || Utils.GetParams(finder.prms, Constants.page_spisdom))
            {
                #region соединение с бд Webdata
                IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
                ret = OpenDb(conn_web, true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return null;
                }
                #endregion

                if (Utils.GetParams(finder.prms, Constants.page_spisls))
                {
                    string tXX_spls = "t" + finder.nzp_user + "_spls";
                    if (!TableInWebCashe(conn_web, tXX_spls))
                    {
                        conn_db.Close();
                        conn_web.Close();
                        ret = new Returns(false, "Лицевые счета не выбраны", -1);
                        return null;
                    }
#if PG
                    tXX_spls = "public." + tXX_spls;
#else
                    tXX_spls = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXX_spls;
#endif
#if PG
                    sql = "Select t.nzp_supp, s.name_supp, count(*) as num, nzp_frm From " + finder.pref + "_data.tarif t, " + tXX_spls + " a, " + finder.pref + "_kernel.supplier s" +
                        " Where t.nzp_kvar = a.nzp_kvar and t.nzp_serv = " + finder.nzp_serv + " and is_actual <> 100" +
                            " and '01." + finder.month_.ToString("00") + "." + finder.year_.ToString("0000") + "' between t.dat_s and t.dat_po" +
                            " and t.nzp_supp = s.nzp_supp" +
                        " Group by t.nzp_supp, s.name_supp, nzp_frm" +
                        " Order by 3 desc";
#else
                    sql = "Select t.nzp_supp, s.name_supp, count(*) as num, nzp_frm From " + finder.pref + "_data:tarif t, " + tXX_spls + " a, " + finder.pref + "_kernel:supplier s" +
                        " Where t.nzp_kvar = a.nzp_kvar and t.nzp_serv = " + finder.nzp_serv + " and is_actual <> 100" +
                            " and '01." + finder.month_.ToString("00") + "." + finder.year_.ToString("0000") + "' between t.dat_s and t.dat_po" +
                            " and t.nzp_supp = s.nzp_supp" +
                        " Group by t.nzp_supp, s.name_supp, nzp_frm" +
                        " Order by 3 desc";
#endif
                }
                else if (Utils.GetParams(finder.prms, Constants.page_spisdom))
                {
                    string tXX_spdom = "t" + finder.nzp_user + "_spdom";
                    if (!TableInWebCashe(conn_web, tXX_spdom))
                    {
                        conn_db.Close();
                        conn_web.Close();
                        ret = new Returns(false, "Дома не выбраны", -1);
                        return null;
                    }
#if PG
                    tXX_spdom = "public." + tXX_spdom;
#else
                    tXX_spdom = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXX_spdom;
#endif
#if PG
                    sql = "Select t.nzp_supp, s.name_supp, count(*) as num, nzp_frm From " + finder.pref + "_data.tarif t, " + finder.pref + "_data.kvar k, " + tXX_spdom + " a, " + finder.pref + "_kernel.supplier s" +
                        " Where t.nzp_kvar = k.nzp_kvar and k.nzp_dom = a.nzp_dom and t.nzp_serv = " + finder.nzp_serv + " and is_actual <> 100" +
                            " and '01." + finder.month_.ToString("00") + "." + finder.year_.ToString("0000") + "' between t.dat_s and t.dat_po" +
                            " and t.nzp_supp = s.nzp_supp" +
                        " Group by t.nzp_supp, s.name_supp, nzp_frm" +
                        " Order by 3 desc";
#else
                    sql = "Select t.nzp_supp, s.name_supp, count(*) as num, nzp_frm From " + finder.pref + "_data:tarif t, " + finder.pref + "_data:kvar k, " + tXX_spdom + " a, " + finder.pref + "_kernel:supplier s" +
                        " Where t.nzp_kvar = k.nzp_kvar and k.nzp_dom = a.nzp_dom and t.nzp_serv = " + finder.nzp_serv + " and is_actual <> 100" +
                            " and '01." + finder.month_.ToString("00") + "." + finder.year_.ToString("0000") + "' between t.dat_s and t.dat_po" +
                            " and t.nzp_supp = s.nzp_supp" +
                        " Group by t.nzp_supp, s.name_supp, nzp_frm" +
                        " Order by 3 desc";
#endif
                }

                conn_web.Close();

                ret = ExecRead(conn_db, out reader, sql, true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return null;
                }

                if (reader.Read())
                {
                    Service zap = new Service();
                    if (reader["nzp_supp"] != DBNull.Value) zap.nzp_supp = Convert.ToInt32(reader["nzp_supp"]);
                    if (reader["name_supp"] != DBNull.Value) zap.name_supp = Convert.ToString(reader["name_supp"]).Trim();
                    if (reader["nzp_frm"] != DBNull.Value) zap.nzp_frm = Convert.ToInt32(reader["nzp_frm"]);
                    list.Add(zap);
                }
                ret.tag = list.Count;
            }
            else
            {
                if (finder.is_actual == 1)
                {
                    //выбрать полный список услуг, когда либо оказываемый данной квартире
#if PG
                    sql = "Select distinct t.nzp_serv, s.service,s.ordering from " + finder.pref.Trim() + "_data.tarif t, " + finder.pref.Trim() + "_kernel.services s " +
                        " Where nzp_kvar = " + finder.nzp_kvar +
                        " and is_actual <> 100 " +
                        " and t.nzp_serv = s.nzp_serv" + where +
                        " Order by s.service";
#else
                    sql = "Select unique t.nzp_serv, s.service,s.ordering from " + finder.pref.Trim() + "_data:tarif t, " + finder.pref.Trim() + "_kernel:services s " +
                        " Where nzp_kvar = " + finder.nzp_kvar +
                        " and is_actual <> 100 " +
                        " and t.nzp_serv = s.nzp_serv" + where +
                        " Order by s.ordering";
#endif
                }
                else
                {
                    //выбрать полный список услуг, доступный в данной БД

                    if (Utils.GetParams(finder.prms, Constants.page_spisservdom))
                    {
                        //
                    }
#if PG
                    sql = " Select distinct t.nzp_serv, s.service, s.ordering from " + finder.pref.Trim() + "_kernel.l_foss t, " + finder.pref.Trim() + "_kernel.services s " +
                        " Where t.nzp_serv = s.nzp_serv and t.nzp_serv > 1 " + where +
                        " Order by s.service";
#else
                    sql = " Select unique t.nzp_serv, s.service, s.ordering from " + finder.pref.Trim() + "_kernel:l_foss t, " + finder.pref.Trim() + "_kernel:services s " +
                        " Where t.nzp_serv = s.nzp_serv and t.nzp_serv > 1 " + where +
                        " Order by s.ordering";
#endif
                }

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
                    if (finder.skip > 0 && i <= finder.skip) continue;
                    if (i > finder.skip + finder.rows) continue;
                    Service zap = new Service();

                    if (reader["nzp_serv"] != DBNull.Value) zap.nzp_serv = (int)reader["nzp_serv"];
                    if (reader["service"] != DBNull.Value) zap.service = ((string)reader["service"]).Trim();

                    if (!Utils.GetParams(finder.prms, Constants.page_newfdgroupspisserv) &&
                        !Utils.GetParams(finder.prms, Constants.page_newfd_group_spis_serv_dom))
                    {
                        IDataReader reader2;
//                        sql = "select t.nzp_tarif, t.nzp_supp, s.name_supp, s.nzp_payer_princip, t.nzp_frm from " +
//#if PG
// finder.pref.Trim() + "_data.tarif t, " + finder.pref.Trim() + "_kernel.supplier s " +
//                            " where nzp_kvar = " + finder.nzp_kvar + " and nzp_serv = " + zap.nzp_serv +
//                            " and is_actual <> 100 " +
//                            " and '01." + finder.month_.ToString("00") + "." + finder.year_.ToString("0000") + "' between dat_s and dat_po " +
//                            " and t.nzp_supp = s.nzp_supp";
//#else
//                            finder.pref.Trim() + "_data:tarif t, " + finder.pref.Trim() + "_kernel:supplier s " +
//                            " where nzp_kvar = " + finder.nzp_kvar + " and nzp_serv = " + zap.nzp_serv +
//                            " and is_actual <> 100 " +
//                            " and '01." + finder.month_.ToString("00") + "." + finder.year_.ToString("0000") + "' between dat_s and dat_po " +
//                            " and t.nzp_supp = s.nzp_supp";
//#endif
                        sql = " select t.nzp_tarif, t.nzp_supp, s.name_supp, s.nzp_payer_princip, s.nzp_payer_podr, sppol.payer poluch,sppd.payer podr, t.nzp_frm , b.rcount, sp.payer bank_name, " +
                              " d.num_dog, d.dat_dog, spa.payer agent, spp.payer princip " +
                                " from " + finder.pref.Trim() + "_data"+tableDelimiter+"tarif t, " + finder.pref.Trim() + "_kernel"+tableDelimiter+"supplier s " +
                                " left outer join "+Points.Pref+"_kernel"+tableDelimiter+"s_payer sppd on s.nzp_payer_podr = sppd.nzp_payer " +
                                " left outer join " + Points.Pref + "_data" + tableDelimiter + "fn_dogovor_bank_lnk fdbl on fdbl.id = s.fn_dogovor_bank_lnk_id " +
                                " left outer join " + Points.Pref + "_data" + tableDelimiter + "fn_bank b on fdbl.nzp_fb = b.nzp_fb " +
                                " left outer join "+Points.Pref+"_kernel"+tableDelimiter+"s_payer sp on b.nzp_payer_bank = sp.nzp_payer "+
                                " left outer join " + Points.Pref + "_kernel" + tableDelimiter + "s_payer sppol on b.nzp_payer = sppol.nzp_payer " +
                                " left outer join "+Points.Pref+"_data"+tableDelimiter+"fn_dogovor d "+
                                " left outer join "+Points.Pref+"_kernel"+tableDelimiter+"s_payer spa on d.nzp_payer_agent = spa.nzp_payer " +
                                " left outer join "+Points.Pref+"_kernel"+tableDelimiter+"s_payer spp on d.nzp_payer_princip = spp.nzp_payer " +
                                " on d.nzp_fd = fdbl.nzp_fd " +
                                " where nzp_kvar = " + finder.nzp_kvar + " and nzp_serv = " + zap.nzp_serv +
                            " and is_actual <> 100 " +
                            " and '01." + finder.month_.ToString("00") + "." + finder.year_.ToString("0000") + "' between t.dat_s and t.dat_po " +
                            " and t.nzp_supp = s.nzp_supp";
                        ret = ExecRead(conn_db, out reader2, sql, true);
                        if (!ret.result)
                        {
                            reader.Close();
                            conn_db.Close();
                            return null;
                        }
                        bool exist = false;
                        while (reader2.Read())
                        {
                            SupplierFinder supp = new SupplierFinder();
                            if (reader2["nzp_supp"] != DBNull.Value) supp.nzp_supp = (int)reader2["nzp_supp"];
                            if (reader2["nzp_payer_princip"] != DBNull.Value) supp.nzp_payer_princip = (int)reader2["nzp_payer_princip"];
                            if (reader2["name_supp"] != DBNull.Value) supp.name_supp = ((string)reader2["name_supp"]).Trim();
                            if (reader2["nzp_frm"] != DBNull.Value) supp.nzp_frm = (int)reader2["nzp_frm"];
                            if (reader2["num_dog"] != DBNull.Value) supp.erc_dogovor = "№ "+Convert.ToString(reader2["num_dog"]);
                            if (reader2["dat_dog"] != DBNull.Value) supp.erc_dogovor += " от " + Convert.ToDateTime(reader2["dat_dog"]).ToShortDateString();
                            if (reader2["agent"] != DBNull.Value) supp.erc_dogovor += " " + Convert.ToString(reader2["agent"]) + "(агент)";
                            if (reader2["princip"] != DBNull.Value) supp.erc_dogovor += " " + Convert.ToString(reader2["princip"]) + "(принципал)";
                            if (reader2["bank_name"] != DBNull.Value) supp.rcount = Convert.ToString(reader2["bank_name"]);
                            if (reader2["rcount"] != DBNull.Value) supp.rcount += " р/с "+Convert.ToString(reader2["rcount"]);
                            if (reader2["poluch"] != DBNull.Value) supp.rcount += " получатель: " + Convert.ToString(reader2["poluch"]);
                            if (reader2["podr"] != DBNull.Value) supp.podr = Convert.ToString(reader2["podr"]);
                            zap.activePeriod = 1;
                            zap.nzp_supp = supp.nzp_supp;
                            zap.nzp_frm = supp.nzp_frm;
                            zap.name_supp = supp.name_supp;
                            zap.list_supp.Add(supp);

                            if (GlobalSettings.NewGeneratePkodMode)
                            {
                                List<KvarPkodes> list_kvar_pkodes = new List<KvarPkodes>();
                                sql = "select pkod, is_default from " + Points.Pref + "_data" + tableDelimiter + "kvar_pkodes where nzp_kvar = " + finder.nzp_kvar +
                                      " and is_princip = 1 and nzp_payer = " + supp.nzp_payer_princip;
                                ret = ExecRead(conn_db, out reader3, sql, true);
                                if (!ret.result)
                                {
                                    reader2.Close();
                                    reader.Close();
                                    conn_db.Close();
                                    return null;
                                }
                                string pkodes = "";
                                while (reader3.Read())
                                {
                                    KvarPkodes kp = new KvarPkodes();
                                    kp.pkod = (reader3["pkod"] != DBNull.Value) ? Convert.ToString(reader3["pkod"]) : "";
                                    kp.is_default = (reader3["is_default"] != DBNull.Value) ? Convert.ToInt32(reader3["is_default"]) : 0;

                                    if (kp.is_default == 1)
                                    {
                                        if (pkodes == "") pkodes += kp.pkod;
                                        else pkodes += ", " + kp.pkod;
                                    }
                                    else
                                    {
                                        if (pkodes == "") pkodes += " <span style='color: red'>" + kp.pkod + "</span>";
                                        else pkodes += ", " + " <span style='color: red'>" + kp.pkod + "</span>";
                                    }
                                    list_kvar_pkodes.Add(kp);
                                }
                                reader3.Close();
                                zap.pkodes = pkodes;
                                zap.list_kvar_pkodes = list_kvar_pkodes;
                            }
                        }
                        reader2.Close();
                        if (!exist) // проверить, есть ли хоть один действующий период оказания услуги
                        {
                            reader2.Close();
#if PG
                            sql = "select t.nzp_tarif from " + finder.pref.Trim() + "_data.tarif t " +
                                " where nzp_kvar = " + finder.nzp_kvar + " and nzp_serv = " + zap.nzp_serv + " and is_actual <> 100 limit 1";
#else
                            sql = "select first 1 t.nzp_tarif from " + finder.pref.Trim() + "_data:tarif t " +
                                " where nzp_kvar = " + finder.nzp_kvar + " and nzp_serv = " + zap.nzp_serv + " and is_actual <> 100 ";
#endif
                            ret = ExecRead(conn_db, out reader2, sql, true);
                            if (!ret.result)
                            {
                                reader.Close();
                                conn_db.Close();
                                return null;
                            }
                            if (reader2.Read()) zap.activePeriod = 0;
                            else zap.activePeriod = -1;
                            reader2.Close();
                        }
                    }

                    list.Add(zap);
                }
                ret.tag = i;
            }
            if (reader != null) reader.Close();
            conn_db.Close();
            return list;
        }

        public List<Service> FindLsServicesDom(Service finder, out Returns ret)
        {
            #region Проверка входных параметров
            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Не определен пользователь");
                return null;
            }
            if (finder.pref.Trim() == "")
            {
                ret = new Returns(false, "Не определен префикс");
                return null;
            }
            if (finder.nzp_dom < 1)
            {
                ret = new Returns(false, "Не задан дом");
                return null;
            }
            if (finder.month_ < 1)
            {
                ret = new Returns(false, "Не задан расчетный месяц");
                return null;
            }

            if (finder.year_ < 1)
            {
                ret = new Returns(false, "Не задан расчетный год");
                return null;
            }
            #endregion

            string connectionString = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            List<Service> list = new List<Service>();

            IDataReader reader;
            string sql = "", where = "";

            if (finder.RolesVal != null)
                foreach (_RolesVal role in finder.RolesVal)
                {
                    if (role.tip == Constants.role_sql & role.kod == Constants.role_sql_serv) where += " and t.nzp_serv in (" + role.val + ")";
                    if (role.tip == Constants.role_sql & role.kod == Constants.role_sql_supp) where += " and t.nzp_supp in (" + role.val + ")";
                }

            if (finder.nzp_serv > 0) where += " and s.nzp_serv = " + finder.nzp_serv;

#if PG
            string prm_3 = finder.pref + "_data." + "prm_3";
#else
            string prm_3 = finder.pref + "_data@" + DBManager.getServer(conn_db) + ":" + "prm_3";
#endif
            DateTime dat = new DateTime(finder.year_, finder.month_, 1);

#if PG
            sql = "Select t.nzp_supp, s.name_supp, count(*) as num From " + finder.pref + "_data.tarif t, " + finder.pref + "_data.kvar a, " + finder.pref + "_kernel.supplier s" +
                " Where t.nzp_kvar = a.nzp_kvar and t.nzp_serv = " + finder.nzp_serv + " and is_actual <> 100" +
                    " and '01." + finder.month_.ToString("00") + "." + finder.year_.ToString("0000") + "' between t.dat_s and t.dat_po" +
                    " and t.nzp_supp = s.nzp_supp" +
                    " and a.nzp_dom = " + finder.nzp_dom +
                    " and (select Max(val_prm) from " + prm_3 + " p3 where p3.nzp_prm=51 and p3.nzp=a.nzp_kvar " +
                          " and " + Utils.EStrNull(dat.ToShortDateString()) + " between p3.dat_s and p3.dat_po and p3.is_actual <> 100)=" + "'" + (int)Ls.States.Open + "'" +
                " Group by t.nzp_supp, s.name_supp" +
                " Order by 3 desc";
#else
            sql = "Select t.nzp_supp, s.name_supp, count(*) as num From " + finder.pref + "_data:tarif t, " + finder.pref + "_data:kvar a, " + finder.pref + "_kernel:supplier s" +
                " Where t.nzp_kvar = a.nzp_kvar and t.nzp_serv = " + finder.nzp_serv + " and is_actual <> 100" +
                    " and '01." + finder.month_.ToString("00") + "." + finder.year_.ToString("0000") + "' between t.dat_s and t.dat_po" +
                    " and t.nzp_supp = s.nzp_supp" +
                    " and a.nzp_dom = " + finder.nzp_dom +
                    " and (select Max(val_prm) from " + prm_3 + " p3 where p3.nzp_prm=51 and p3.nzp=a.nzp_kvar " +
                          " and " + Utils.EStrNull(dat.ToShortDateString()) + " between p3.dat_s and p3.dat_po and p3.is_actual <> 100)=" + (int)Ls.States.Open +
                " Group by t.nzp_supp, s.name_supp" +
                " Order by 3 desc";
#endif
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }

            try
            {
                if (reader.Read())
                {
                    Service zap = new Service();
                    if (reader["nzp_supp"] != DBNull.Value) zap.nzp_supp = Convert.ToInt32(reader["nzp_supp"]);
                    if (reader["name_supp"] != DBNull.Value) zap.name_supp = Convert.ToString(reader["name_supp"]).Trim();

                    list.Add(zap);
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                if (reader != null) reader.Close();
                conn_db.Close();
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка поиска списка услуг FindLsServicesDom " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }

            conn_db.Close();
            ret.tag = list.Count;
            return list;
        }

        /// <summary> Возвращает периоды действия услуги в квартире
        /// </summary>
        /// <param name="finder">Обязательные параметры: nzp_user, pref, nzp_kvar, nzp_serv</param>
        public List<Service> FindLsServicePeriods(Service finder, out Returns ret)
        {
            #region Проверка входных параметров
            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Не определен пользователь");
                return null;
            }
            if (finder.pref.Trim() == "")
            {
                ret = new Returns(false, "Не определен префикс");
                return null;
            }
            if (finder.nzp_kvar < 1)
            {
                ret = new Returns(false, "Не задана квартира");
                return null;
            }

            if (finder.nzp_serv < 1)
            {
                ret = new Returns(false, "Не задана услуга");
                return null;
            }
            #endregion

            ret = Utils.InitReturns();

            string connectionString = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            string filter = "";
            if (finder.is_actual > 0) filter += " and is_actual = " + finder.is_actual.ToString();
            else if (finder.is_actual < 0) filter += " and is_actual <> " + (-1 * finder.is_actual).ToString();

            if (finder.RolesVal != null)
                foreach (_RolesVal role in finder.RolesVal)
                {
                    if (role.tip == Constants.role_sql && role.kod == Constants.role_sql_serv) filter += " and t.nzp_serv in (" + role.val + ")";
                    if (role.tip == Constants.role_sql && role.kod == Constants.role_sql_supp) filter += " and t.nzp_supp in (" + role.val + ")";
                }

#if PG
            var sqlBuider = new StringBuilder();
            sqlBuider.Append(
                             "select t.nzp_tarif, t.nzp_serv, t.nzp_supp, s.nzp_payer_princip, s.name_supp, t.nzp_frm, f.name_frm, t.dat_s, t.dat_po, t.is_actual, t.nzp_user, t.dat_when, t.dat_del, t.user_del, ");
            sqlBuider.Append(" u.comment as user_name, u1.comment as user_name_del ");
            sqlBuider.AppendFormat(" from {0}_data.tarif t", finder.pref.Trim());
            sqlBuider.AppendFormat(" left outer join {0}_kernel.supplier s on t.nzp_supp = s.nzp_supp", finder.pref.Trim());
            sqlBuider.AppendFormat(" left outer join {0}_kernel.formuls f on t.nzp_frm = f.nzp_frm", finder.pref.Trim());
            sqlBuider.AppendFormat(" left outer join {0}_data.users u on t.nzp_user = u.nzp_user", Points.Pref.Trim());
            sqlBuider.AppendFormat(" left outer join {0}_data.users u1 on t.user_del = u1.nzp_user", Points.Pref.Trim());
            sqlBuider.AppendFormat(" where t.nzp_kvar = {0}", finder.nzp_kvar)
                .AppendFormat(" and t.nzp_serv = {0}", finder.nzp_serv)
                .AppendFormat(filter)
                .Append(" Order by t.dat_s desc");

            var sql = sqlBuider.ToString();
#else
            string sql = "select t.nzp_tarif, t.nzp_serv, t.nzp_supp, s.name_supp, t.nzp_frm, f.name_frm, t.dat_s, t.dat_po, t.is_actual, t.nzp_user, t.dat_when, t.dat_del, t.user_del, " +
                " u.comment as user_name, u1.comment as user_name_del " +
                " From " + finder.pref.Trim() + "_data:tarif t, " +
                    finder.pref.Trim() + "_kernel:supplier s, " +
                    finder.pref.Trim() + "_kernel:formuls f, " +
                    " outer " + Points.Pref.Trim() + "_data:users u, " +
                    " outer " + Points.Pref.Trim() + "_data:users u1 " +
                " Where t.nzp_kvar = " + finder.nzp_kvar +
                    " and t.nzp_serv = " + finder.nzp_serv +
                    " and t.nzp_supp = s.nzp_supp" +
                    " and t.nzp_frm = f.nzp_frm" +
                    " and t.nzp_user = u.nzp_user" +
                    " and t.user_del = u1.nzp_user" +
                    filter +
                " Order by t.dat_s desc";
#endif
            IDataReader reader, reader3;
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }

            List<Service> list = new List<Service>();
            while (reader.Read())
            {
                Service zap = new Service();

                if (reader["nzp_tarif"] != DBNull.Value) zap.nzp_tarif = (int)reader["nzp_tarif"];
                if (reader["nzp_serv"] != DBNull.Value) zap.nzp_serv = (int)reader["nzp_serv"];
                if (reader["nzp_supp"] != DBNull.Value) zap.nzp_supp = (int)reader["nzp_supp"];
                if (reader["nzp_payer_princip"] != DBNull.Value) zap.nzp_payer_princip = (int)reader["nzp_payer_princip"];
                if (reader["name_supp"] != DBNull.Value) zap.name_supp = ((string)reader["name_supp"]).Trim();
                if (reader["nzp_frm"] != DBNull.Value) zap.nzp_frm = (int)reader["nzp_frm"];
                if (reader["name_frm"] != DBNull.Value) zap.name_frm = ((string)reader["name_frm"]).Trim();
                if (reader["dat_s"] != DBNull.Value)
                {
                    zap.dat_s = String.Format("{0:dd.MM.yyyy}", reader["dat_s"]);
                    DateTime ds;
                    bool res = DateTime.TryParseExact(zap.dat_s, "dd.MM.yyyy", new CultureInfo("ru-RU"), DateTimeStyles.None, out ds);
                    if (res)
                        if (ds <= new DateTime(1900, 1, 1)) zap.dat_s = "";
                }
                if (reader["dat_po"] != DBNull.Value)
                {
                    zap.dat_po = String.Format("{0:dd.MM.yyyy}", reader["dat_po"]);
                    DateTime dpo;
                    bool res = DateTime.TryParseExact(zap.dat_po, "dd.MM.yyyy", new CultureInfo("ru-RU"), DateTimeStyles.None, out dpo);
                    if (res)
                        if (dpo > new DateTime(2900, 1, 1)) zap.dat_po = "";
                }
                if (reader["is_actual"] != DBNull.Value) zap.is_actual = (int)reader["is_actual"];

                if (reader["nzp_user"] != DBNull.Value) zap.nzp_user = (int)reader["nzp_user"];
                if (reader["dat_when"] != DBNull.Value)
                {
                    zap.dat_when = String.Format("{0:dd.MM.yyyy}", reader["dat_when"]);
                    if (reader["user_name"] != DBNull.Value) zap.dat_when += " (" + Convert.ToString(reader["user_name"]).Trim() + ")";
                }

                if (reader["user_del"] != DBNull.Value) zap.nzp_user_del = (int)reader["user_del"];
                if (reader["dat_del"] != DBNull.Value)
                {
                    zap.dat_del = String.Format("{0:dd.MM.yyyy}", reader["dat_del"]);
                    if (reader["user_name_del"] != DBNull.Value) zap.dat_del += " (" + ((string)reader["user_name_del"]).Trim() + ")";
                }

                if (GlobalSettings.NewGeneratePkodMode)
                {
                    sql = "select pkod, is_default from " + Points.Pref + "_data" + tableDelimiter + "kvar_pkodes where nzp_kvar = " + finder.nzp_kvar +
                          " and is_princip = 1 and nzp_payer = " + zap.nzp_payer_princip;
                    ret = ExecRead(conn_db, out reader3, sql, true);
                    if (!ret.result)
                    {
                        reader.Close();
                        conn_db.Close();
                        return null;
                    }
                    string pkodes = "";
                    while (reader3.Read())
                    {
                        KvarPkodes kp = new KvarPkodes();
                        kp.pkod = (reader3["pkod"] != DBNull.Value) ? Convert.ToString(reader3["pkod"]) : "";
                        kp.is_default = (reader3["is_default"] != DBNull.Value) ? Convert.ToInt32(reader3["is_default"]) : 0;

                        if (kp.is_default == 1)
                        {
                            if (pkodes == "") pkodes += kp.pkod;
                            else pkodes += ", " + kp.pkod;
                        }
                        else
                        {
                            if (pkodes == "") pkodes += " <span style='color: red'>" + kp.pkod + "</span>";
                            else pkodes += ", " + " <span style='color: red'>" + kp.pkod + "</span>";
                        }
                    }
                    reader3.Close();
                    zap.pkodes = pkodes;
                }

                list.Add(zap);
            }
            reader.Close();

            ret.tag = list.Count;

            conn_db.Close();
            return list;
        }

        public List<Service> NewFdFindLsServicePeriods(Service finder, out Returns ret)
        {
            #region Проверка входных параметров
            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Не определен пользователь");
                return null;
            }
            if (finder.pref.Trim() == "")
            {
                ret = new Returns(false, "Не определен префикс");
                return null;
            }
            if (finder.nzp_kvar < 1)
            {
                ret = new Returns(false, "Не задана квартира");
                return null;
            }

            if (finder.nzp_serv < 1)
            {
                ret = new Returns(false, "Не задана услуга");
                return null;
            }
            #endregion

            ret = Utils.InitReturns();

            string connectionString = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            string filter = "";
            if (finder.is_actual > 0) filter += " and is_actual = " + finder.is_actual.ToString();
            else if (finder.is_actual < 0) filter += " and is_actual <> " + (-1 * finder.is_actual).ToString();

            if (finder.RolesVal != null)
                foreach (_RolesVal role in finder.RolesVal)
                {
                    if (role.tip == Constants.role_sql && role.kod == Constants.role_sql_serv) filter += " and t.nzp_serv in (" + role.val + ")";
                    if (role.tip == Constants.role_sql && role.kod == Constants.role_sql_supp) filter += " and t.nzp_supp in (" + role.val + ")";
                }

#if PG
            var sqlBuider = new StringBuilder();
            sqlBuider.Append(
                             "select t.nzp_tarif, t.nzp_serv, t.nzp_supp, s.nzp_payer_princip, s.name_supp, t.nzp_frm, f.name_frm, t.dat_s, t.dat_po, t.is_actual, t.nzp_user, t.dat_when, t.dat_del, t.user_del, ");
            sqlBuider.Append(" u.comment as user_name, u1.comment as user_name_del" +
                             ", sppol.payer poluch,sppd.payer podr, t.nzp_frm , b.rcount, sp.payer bank_name,d.num_dog, d.dat_dog, spa.payer agent, spp.payer princip ");
            sqlBuider.AppendFormat(" from {0}_data.tarif t", finder.pref.Trim());
            sqlBuider.AppendFormat(" left outer join {0}_kernel.supplier s " , finder.pref.Trim());
            sqlBuider.AppendFormat(" left outer join {0}_kernel{1}s_payer sppd on s.nzp_payer_podr = sppd.nzp_payer ",Points.Pref, tableDelimiter);
            sqlBuider.AppendFormat(" left outer join {0}_data{1}fn_dogovor_bank_lnk fdbl on fdbl.id = s.fn_dogovor_bank_lnk_id ", Points.Pref, tableDelimiter);  
            sqlBuider.AppendFormat(" left outer join {0}_data{1}fn_bank b on b.nzp_fb = fdbl.nzp_fb ",  Points.Pref, tableDelimiter);  
            sqlBuider.AppendFormat(" left outer join {0}_kernel{1}s_payer sp on b.nzp_payer_bank = sp.nzp_payer ",  Points.Pref, tableDelimiter);
            sqlBuider.AppendFormat(" left outer join {0}_kernel{1}s_payer sppol on b.nzp_payer = sppol.nzp_payer ", Points.Pref, tableDelimiter);  
            sqlBuider.AppendFormat(" left outer join {0}_data{1}fn_dogovor d ",  Points.Pref, tableDelimiter);  
            sqlBuider.AppendFormat(" left outer join {0}_kernel{1}s_payer spa on d.nzp_payer_agent = spa.nzp_payer ",  Points.Pref, tableDelimiter);  
            sqlBuider.AppendFormat(" left outer join {0}_kernel{1}s_payer spp on d.nzp_payer_princip = spp.nzp_payer  ",  Points.Pref, tableDelimiter);
            sqlBuider.Append(" on d.nzp_fd = fdbl.nzp_fd ");
            sqlBuider.Append(" on t.nzp_supp = s.nzp_supp");
            sqlBuider.AppendFormat(" left outer join {0}_kernel.formuls f on t.nzp_frm = f.nzp_frm", finder.pref.Trim());
            sqlBuider.AppendFormat(" left outer join {0}_data.users u on t.nzp_user = u.nzp_user", Points.Pref.Trim());
            sqlBuider.AppendFormat(" left outer join {0}_data.users u1 on t.user_del = u1.nzp_user", Points.Pref.Trim());
            sqlBuider.AppendFormat(" where t.nzp_kvar = {0}", finder.nzp_kvar)
                .AppendFormat(" and t.nzp_serv = {0}", finder.nzp_serv)
                .AppendFormat(filter)
                .Append(" Order by t.dat_s desc");

            var sql = sqlBuider.ToString();
#else
            string sql = "select t.nzp_tarif, t.nzp_serv, t.nzp_supp, s.name_supp, t.nzp_frm, f.name_frm, t.dat_s, t.dat_po, t.is_actual, t.nzp_user, t.dat_when, t.dat_del, t.user_del, " +
                " u.comment as user_name, u1.comment as user_name_del " +
                " From " + finder.pref.Trim() + "_data:tarif t, " +
                    finder.pref.Trim() + "_kernel:supplier s, " +
                    finder.pref.Trim() + "_kernel:formuls f, " +
                    " outer " + Points.Pref.Trim() + "_data:users u, " +
                    " outer " + Points.Pref.Trim() + "_data:users u1 " +
                " Where t.nzp_kvar = " + finder.nzp_kvar +
                    " and t.nzp_serv = " + finder.nzp_serv +
                    " and t.nzp_supp = s.nzp_supp" +
                    " and t.nzp_frm = f.nzp_frm" +
                    " and t.nzp_user = u.nzp_user" +
                    " and t.user_del = u1.nzp_user" +
                    filter +
                " Order by t.dat_s desc";
#endif
            IDataReader reader, reader3;
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }

            List<Service> list = new List<Service>();
            while (reader.Read())
            {
                Service zap = new Service();

                if (reader["nzp_tarif"] != DBNull.Value) zap.nzp_tarif = (int)reader["nzp_tarif"];
                if (reader["nzp_serv"] != DBNull.Value) zap.nzp_serv = (int)reader["nzp_serv"];
                if (reader["nzp_supp"] != DBNull.Value) zap.nzp_supp = (int)reader["nzp_supp"];
                if (reader["nzp_payer_princip"] != DBNull.Value) zap.nzp_payer_princip = (int)reader["nzp_payer_princip"];
                if (reader["name_supp"] != DBNull.Value) zap.name_supp = ((string)reader["name_supp"]).Trim();
                if (reader["nzp_frm"] != DBNull.Value) zap.nzp_frm = (int)reader["nzp_frm"];
                if (reader["name_frm"] != DBNull.Value) zap.name_frm = ((string)reader["name_frm"]).Trim();
                if (reader["dat_s"] != DBNull.Value)
                {
                    zap.dat_s = String.Format("{0:dd.MM.yyyy}", reader["dat_s"]);
                    DateTime ds;
                    bool res = DateTime.TryParseExact(zap.dat_s, "dd.MM.yyyy", new CultureInfo("ru-RU"), DateTimeStyles.None, out ds);
                    if (res)
                        if (ds <= new DateTime(1900, 1, 1)) zap.dat_s = "";
                }
                if (reader["dat_po"] != DBNull.Value)
                {
                    zap.dat_po = String.Format("{0:dd.MM.yyyy}", reader["dat_po"]);
                    DateTime dpo;
                    bool res = DateTime.TryParseExact(zap.dat_po, "dd.MM.yyyy", new CultureInfo("ru-RU"), DateTimeStyles.None, out dpo);
                    if (res)
                        if (dpo > new DateTime(2900, 1, 1)) zap.dat_po = "";
                }
                if (reader["is_actual"] != DBNull.Value) zap.is_actual = (int)reader["is_actual"];

                if (reader["nzp_user"] != DBNull.Value) zap.nzp_user = (int)reader["nzp_user"];
                if (reader["dat_when"] != DBNull.Value)
                {
                    zap.dat_when = String.Format("{0:dd.MM.yyyy}", reader["dat_when"]);
                    if (reader["user_name"] != DBNull.Value) zap.dat_when += " (" + Convert.ToString(reader["user_name"]).Trim() + ")";
                }

                if (reader["user_del"] != DBNull.Value) zap.nzp_user_del = (int)reader["user_del"];
                if (reader["dat_del"] != DBNull.Value)
                {
                    zap.dat_del = String.Format("{0:dd.MM.yyyy}", reader["dat_del"]);
                    if (reader["user_name_del"] != DBNull.Value) zap.dat_del += " (" + ((string)reader["user_name_del"]).Trim() + ")";
                }
                if (reader["num_dog"] != DBNull.Value) zap.erc_dogovor = "№ " + Convert.ToString(reader["num_dog"]);
                if (reader["dat_dog"] != DBNull.Value) zap.erc_dogovor += " от " + Convert.ToDateTime(reader["dat_dog"]).ToShortDateString();
                if (reader["agent"] != DBNull.Value) zap.erc_dogovor += " " + Convert.ToString(reader["agent"]) + "(агент)";
                if (reader["bank_name"] != DBNull.Value) zap.rcount = Convert.ToString(reader["bank_name"]);
                if (reader["rcount"] != DBNull.Value) zap.rcount += " р/с " + Convert.ToString(reader["rcount"]);
                if (reader["poluch"] != DBNull.Value) zap.rcount += " получатель: " + Convert.ToString(reader["poluch"]);
                if (reader["podr"] != DBNull.Value) zap.podr = Convert.ToString(reader["podr"]);
                if (GlobalSettings.NewGeneratePkodMode)
                {
                    sql = "select pkod, is_default from " + Points.Pref + "_data" + tableDelimiter + "kvar_pkodes where nzp_kvar = " + finder.nzp_kvar +
                          " and is_princip = 1 and nzp_payer = " + zap.nzp_payer_princip;
                    ret = ExecRead(conn_db, out reader3, sql, true);
                    if (!ret.result)
                    {
                        reader.Close();
                        conn_db.Close();
                        return null;
                    }
                    string pkodes = "";
                    while (reader3.Read())
                    {
                        KvarPkodes kp = new KvarPkodes();
                        kp.pkod = (reader3["pkod"] != DBNull.Value) ? Convert.ToString(reader3["pkod"]) : "";
                        kp.is_default = (reader3["is_default"] != DBNull.Value) ? Convert.ToInt32(reader3["is_default"]) : 0;

                        if (kp.is_default == 1)
                        {
                            if (pkodes == "") pkodes += kp.pkod;
                            else pkodes += ", " + kp.pkod;
                        }
                        else
                        {
                            if (pkodes == "") pkodes += " <span style='color: red'>" + kp.pkod + "</span>";
                            else pkodes += ", " + " <span style='color: red'>" + kp.pkod + "</span>";
                        }
                    }
                    reader3.Close();
                    zap.pkodes = pkodes;
                }

                list.Add(zap);
            }
            reader.Close();

            ret.tag = list.Count;

            conn_db.Close();
            return list;
        }

        public List<_Supplier> FindServDogovorERC(Service finder, out Returns ret)
        {
            #region Проверка входных параметров
            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Не определен пользователь");
                return null;
            }
            if (finder.pref.Trim() == "")
            {
                ret = new Returns(false, "Не определен префикс");
                return null;
            }
            if (finder.nzp_serv < 1)
            {
                ret = new Returns(false, "Не задана услуга");
                return null;
            }
            #endregion

            ret = Utils.InitReturns();
            List<_Supplier> list = new List<_Supplier>();

            string connectionString = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            try
            {
                string filter = "";
                if (finder.RolesVal != null)
                    foreach (_RolesVal role in finder.RolesVal)
                    {
                        if (role.tip == Constants.role_sql && role.kod == Constants.role_sql_supp) filter += " and f.nzp_supp in (" + role.val + ")";
                    }


                string sql =
                    " SELECT distinct fd.nzp_fd, p1.payer payer_agent, p2.payer payer_princip " +
                    " FROM " + finder.pref.Trim() + sKernelAliasRest + "l_foss f, " +
                    Points.Pref + sDataAliasRest + "fn_dogovor fd, " +
                    Points.Pref + sDataAliasRest + "fn_dogovor_bank_lnk fdlnk, "+
                    Points.Pref + sKernelAliasRest + "s_payer p1, " +
                    Points.Pref + sKernelAliasRest + "s_payer p2, " +
                    finder.pref.Trim() + sKernelAliasRest + "supplier s " +
                    " WHERE f.nzp_serv = " + finder.nzp_serv + " AND fd.nzp_fd = fdlnk.nzp_fd and fdlnk.id = s.fn_dogovor_bank_lnk_id  " +
                    " and f.nzp_supp = s.nzp_supp and f.nzp_supp > 0 " +
                    " AND p1.nzp_payer = fd.nzp_payer_agent AND p2.nzp_payer = fd.nzp_payer_princip " +
                    " ORDER BY p1.payer, p2.payer";

                DataTable dt = DBManager.ExecSQLToTable(conn_db, sql);

                foreach (DataRow row in dt.Rows)
                {
                    _Supplier zap = new _Supplier();
                    zap.nzp_supp = Convert.ToInt32(row["nzp_fd"]);
                    zap.name_supp = Convert.ToString(row["payer_agent"]) + ", " + Convert.ToString(row["payer_princip"]);
                    list.Add(zap);
                }
                
                ret.tag = list.Count;
            }
            catch (Exception ex)
            {
                ret = new Returns(false, "Ошибка получения списка договоров ЕРЦ по заданной услуге", -1);
                MonitorLog.WriteLog("Ошибка FindServDogovorERC " + ex.Message + " " + ex.StackTrace, MonitorLog.typelog.Error, true);
            }
            finally
            {
                conn_db.Close();
            }
            
            return list;
        }
        
        /// <summary> Возвращает список поставщиков, оказывающих заданную услугу
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public List<_Supplier> FindServiceSuppliers(Service finder, out Returns ret)
        {
            #region Проверка входных параметров
            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Не определен пользователь");
                return null;
            }
            if (finder.pref.Trim() == "")
            {
                ret = new Returns(false, "Не определен префикс");
                return null;
            }
            if (finder.nzp_serv < 1)
            {
                ret = new Returns(false, "Не задана услуга");
                return null;
            }
            #endregion

            ret = Utils.InitReturns();

            string connectionString = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            string filter = "";
            if (finder.RolesVal != null)
                foreach (_RolesVal role in finder.RolesVal)
                {
                    if (role.tip == Constants.role_sql && role.kod == Constants.role_sql_supp) filter += " and f.nzp_supp in (" + role.val + ")";
                }

#if PG
            string sql = "Select distinct f.nzp_supp, s.name_supp " +
                " From " + finder.pref.Trim() + "_kernel.l_foss f, " + finder.pref.Trim() + "_kernel.supplier s " +
                " Where f.nzp_serv = " + finder.nzp_serv + " and f.nzp_supp = s.nzp_supp and f.nzp_supp > 0 " +
                " Order by s.name_supp";
#else
            string sql = "Select unique f.nzp_supp, s.name_supp " +
                " From " + finder.pref.Trim() + "_kernel:l_foss f, " + finder.pref.Trim() + "_kernel:supplier s " +
                " Where f.nzp_serv = " + finder.nzp_serv + " and f.nzp_supp = s.nzp_supp and f.nzp_supp > 0 " +
                " Order by s.name_supp";
#endif
            IDataReader reader;
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }

            List<_Supplier> list = new List<_Supplier>();
            while (reader.Read())
            {
                _Supplier zap = new _Supplier();

                if (reader["nzp_supp"] != DBNull.Value) zap.nzp_supp = (int)reader["nzp_supp"];
                else zap.nzp_supp = 0;
                if (reader["name_supp"] != DBNull.Value) zap.name_supp = ((string)reader["name_supp"]).Trim();
                else zap.name_supp = "";

                list.Add(zap);
            }
            reader.Close();

            ret.tag = list.Count;

            conn_db.Close();
            return list;
        }

        /// <summary> Возвращает список доступных формул расчета с периодами действия, доступных для расчета заданной услуги, оказываемой заданным поставщиком
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public List<_Formula> FindSupplierFormuls(Service finder, out Returns ret)
        {
            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Не определен пользователь");
                return null;
            }

            if (!Utils.GetParams(finder.prms, Constants.page_findserv) && !Utils.GetParams(finder.prms, Constants.page_available_services)
                && !Utils.GetParams(finder.prms, Constants.page_new_available_services))
            {
                if (finder.pref.Trim() == "")
                {
                    ret = new Returns(false, "Не определен префикс");
                    return null;
                }

                if (finder.nzp_serv < 1)
                {
                    ret = new Returns(false, "Не задана услуга");
                    return null;
                }

                if (finder.nzp_supp < 1)
                {
                    ret = new Returns(false, "Не задан поставщик услуги");
                    return null;
                }
            }

            if (finder.pref.Trim() == "") finder.pref = Points.Pref;

            string connectionString = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            string filter = "";
            if (finder.nzp_frm > 0) filter += " and f.nzp_frm = " + finder.nzp_frm;

            string sql;

            if (Utils.GetParams(finder.prms, Constants.page_available_services) || Utils.GetParams(finder.prms, Constants.page_new_available_services))
            {
#if PG
                sql = "Select distinct f.nzp_frm, f.name_frm, f.dat_s, f.dat_po " +
                    " From " + Points.Pref + "_kernel.formuls f ";
#else
                sql = "Select unique f.nzp_frm, f.name_frm, f.dat_s, f.dat_po " +
                    " From " + finder.pref + "_kernel:formuls f ";
#endif
                if (finder.nzp_serv > 0)
                {
#if PG
                    sql += ", " + Points.Pref + "_kernel.prm_tarifs t";
#else
                    sql += ", " + finder.pref + "_kernel:prm_tarifs t";
#endif
                    sql += " Where f.nzp_frm = t.nzp_frm" + filter +
                        " and t.nzp_serv = " + finder.nzp_serv;
                }
                sql += " Order by f.name_frm";
            }
            else if (Utils.GetParams(finder.prms, Constants.page_findserv))
            {
#if PG
                sql = "Select distinct f.nzp_frm, frm.name_frm, f.dat_s, f.dat_po " +
                    " From " + Points.Pref + "_kernel.l_foss f, " + Points.Pref + "_kernel.formuls frm " +
                    " Where f.nzp_frm = frm.nzp_frm" + filter;
#else
                sql = "Select unique f.nzp_frm, frm.name_frm, f.dat_s, f.dat_po " +
                    " From " + Points.Pref + "_kernel:l_foss f, " + Points.Pref + "_kernel:formuls frm " +
                    " Where f.nzp_frm = frm.nzp_frm" + filter;
#endif
                if (finder.nzp_serv != 0) sql += " and f.nzp_serv " + (finder.nzp_serv > 0 ? " = " + finder.nzp_serv : " <> " + -finder.nzp_serv);
                if (finder.nzp_supp != 0) sql += " and f.nzp_supp " + (finder.nzp_supp > 0 ? " = " + finder.nzp_supp : " <> " + -finder.nzp_supp);

                sql += " Order by f.dat_s, f.dat_po desc, frm.name_frm";
            }
            else
            {
#if PG
                sql = "Select distinct f.nzp_frm, frm.name_frm, f.dat_s, f.dat_po " +
                    " From " + finder.pref.Trim() + "_kernel.l_foss f, " + finder.pref.Trim() + "_kernel.formuls frm " +
                    " Where f.nzp_serv = " + finder.nzp_serv +
                        " and f.nzp_supp = " + finder.nzp_supp +
                        filter +
                        " and f.nzp_frm = frm.nzp_frm " +
                    " Order by f.dat_s, f.dat_po desc, frm.name_frm";
#else
                sql = "Select unique f.nzp_frm, frm.name_frm, f.dat_s, f.dat_po " +
                    " From " + finder.pref.Trim() + "_kernel:l_foss f, " + finder.pref.Trim() + "_kernel:formuls frm " +
                    " Where f.nzp_serv = " + finder.nzp_serv +
                        " and f.nzp_supp = " + finder.nzp_supp +
                        filter +
                        " and f.nzp_frm = frm.nzp_frm " +
                    " Order by f.dat_s, f.dat_po desc, frm.name_frm";
#endif
            }

            IDataReader reader;
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }

            List<_Formula> list = new List<_Formula>();
            while (reader.Read())
            {
                _Formula zap = new _Formula();

                if (reader["nzp_frm"] != DBNull.Value) zap.nzp_frm = (int)reader["nzp_frm"];
                else zap.nzp_frm = 0;
                if (reader["name_frm"] != DBNull.Value)
                {
                    zap.name_frm = ((string)reader["name_frm"]).Trim();

                    string s;
                    DateTime ds, dpo;
                    if (reader["dat_s"] != DBNull.Value)
                    {
                        s = String.Format("{0:dd.MM.yyyy}", reader["dat_s"]);
                        bool res = DateTime.TryParseExact(s, "dd.MM.yyyy", new CultureInfo("ru-RU"), System.Globalization.DateTimeStyles.None, out ds);
                        if (!res)
                        {
                            ret = new Returns(false, "Ошибка при распознавании даты с");
                            reader.Close();
                            conn_db.Close();
                            return null;
                        }
                    }
                    else ds = DateTime.MinValue;

                    if (reader["dat_po"] != DBNull.Value)
                    {
                        s = String.Format("{0:dd.MM.yyyy}", reader["dat_po"]);
                        bool res = DateTime.TryParseExact(s, "dd.MM.yyyy", new CultureInfo("ru-RU"), System.Globalization.DateTimeStyles.None, out dpo);
                        if (!res)
                        {
                            ret = new Returns(false, "Ошибка при распознавании даты по");
                            reader.Close();
                            conn_db.Close();
                            return null;
                        }
                    }
                    else dpo = DateTime.MaxValue;

                    if (ds >= new DateTime(1900, 1, 1) || dpo < new DateTime(2900, 1, 1))
                    {
                        zap.name_frm += " (";
                        if (ds >= new DateTime(1900, 1, 1)) zap.name_frm += " с " + ds.ToString("dd.MM.yyyy");
                        if (dpo < new DateTime(2900, 1, 1)) zap.name_frm += " по " + dpo.ToString("dd.MM.yyyy");
                        zap.name_frm += " )";
                    }
                    if (finder.nzp_frm > 0)
                    {
                        zap.dat_s = ds.ToString("dd.MM.yyyy");
                        zap.dat_po = dpo.ToString("dd.MM.yyyy");
                    }
                }
                else zap.name_frm = "";

                list.Add(zap);
            }
            reader.Close();

            ret.tag = list.Count;

            conn_db.Close();
            return list;
        }
        public List<_Formula> LoadFormulsAllPoints(Service finder, out Returns ret)
        {
            string connectionString = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            ExecSQL(conn_db, "drop table temp_formuls_all", false);

            StringBuilder sql = new StringBuilder();
            sql.Append("create temp table temp_formuls_all (nzp_frm integer, name_frm char(60), dat_s date, dat_po date) " + sUnlogTempTable);
            ret = ExecSQL(conn_db, sql.ToString(), true);
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }

            foreach (_Point p in Points.PointList)
            {
                InsIntoTemp_formuls_all(conn_db, finder, p.pref, out ret);
                if (!ret.result)
                {
                    conn_db.Close();
                    return null;
                }
            }

            sql.Remove(0, sql.Length);
            sql.Append("select " + sUniqueWord + " * from temp_formuls_all ");
            sql.Append(" Order by dat_s, dat_po desc, name_frm");

            MyDataReader reader;
            ret = ExecRead(conn_db, out reader, sql.ToString(), true);
            if (!ret.result) return null;

            int i = 0;
            List<_Formula> list = new List<_Formula>();
            while (reader.Read())
            {
                i++;
                if (finder.skip > 0 && i <= finder.skip) continue;
                if (i > finder.skip + finder.rows) continue;

                _Formula zap = new _Formula();

                if (reader["nzp_frm"] != DBNull.Value) zap.nzp_frm = (int)reader["nzp_frm"];
                else zap.nzp_frm = 0;
                if (reader["name_frm"] != DBNull.Value)
                {
                    zap.name_frm = ((string)reader["name_frm"]).Trim();

                    string s;
                    DateTime ds, dpo;
                    if (reader["dat_s"] != DBNull.Value)
                    {
                        s = String.Format("{0:dd.MM.yyyy}", reader["dat_s"]);
                        bool res = DateTime.TryParseExact(s, "dd.MM.yyyy", new CultureInfo("ru-RU"), System.Globalization.DateTimeStyles.None, out ds);
                        if (!res)
                        {
                            ret = new Returns(false, "Ошибка при распознавании даты с");
                            reader.Close();
                            conn_db.Close();
                            return null;
                        }
                    }
                    else ds = DateTime.MinValue;

                    if (reader["dat_po"] != DBNull.Value)
                    {
                        s = String.Format("{0:dd.MM.yyyy}", reader["dat_po"]);
                        bool res = DateTime.TryParseExact(s, "dd.MM.yyyy", new CultureInfo("ru-RU"), System.Globalization.DateTimeStyles.None, out dpo);
                        if (!res)
                        {
                            ret = new Returns(false, "Ошибка при распознавании даты по");
                            reader.Close();
                            conn_db.Close();
                            return null;
                        }
                    }
                    else dpo = DateTime.MaxValue;

                    if (ds >= new DateTime(1900, 1, 1) || dpo < new DateTime(2900, 1, 1))
                    {
                        zap.name_frm += " (";
                        if (ds >= new DateTime(1900, 1, 1)) zap.name_frm += " с " + ds.ToString("dd.MM.yyyy");
                        if (dpo < new DateTime(2900, 1, 1)) zap.name_frm += " по " + dpo.ToString("dd.MM.yyyy");
                        zap.name_frm += " )";
                    }
                    if (finder.nzp_frm > 0)
                    {
                        zap.dat_s = ds.ToString("dd.MM.yyyy");
                        zap.dat_po = dpo.ToString("dd.MM.yyyy");
                    }
                }
                else zap.name_frm = "";

                list.Add(zap);
            }
            reader.Close();
            ExecSQL(conn_db, "drop table temp_formuls_all", false);
            conn_db.Close();
            return list;
        }

        public void InsIntoTemp_formuls_all(IDbConnection conn_db, Service finder, string pref, out Returns ret)
        {
            StringBuilder sql = new StringBuilder();
            sql.Append("Insert into temp_formuls_all (nzp_frm , name_frm , dat_s , dat_po )");
            sql.Append("Select " + sUniqueWord + " f.nzp_frm, frm.name_frm, f.dat_s, f.dat_po");
            sql.AppendFormat(" From {0}_kernel{1}l_foss f, {0}_kernel{1}formuls frm ", pref, tableDelimiter);
            sql.Append(" Where f.nzp_frm = frm.nzp_frm");

            if (finder.nzp_serv != 0) sql.Append(" and f.nzp_serv " + (finder.nzp_serv > 0 ? " = " + finder.nzp_serv : " <> " + -finder.nzp_serv));
            if (finder.nzp_supp != 0) sql.Append(" and f.nzp_supp " + (finder.nzp_supp > 0 ? " = " + finder.nzp_supp : " <> " + -finder.nzp_supp));
            ret = ExecSQL(conn_db, sql.ToString(), true);
        }

        private enum SaveModes
        {
            None = 0x00,
            ServiceOfOneKvar = 0x01,
            ServiceOfGroupKvar = 0x02,
            ServiceOfGroupDom = 0x03,
            ServiceOfOneDom = 0x04,
            AvailableServices = 0x05,   // операции с l_foss
            ServiceFromDictionary = 0x06              // Сохранение услуги в справочнике услуг
        }


        /// <summary>
        /// Сохранение услуги
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="primfinder"></param>
        /// <returns></returns>
        public Returns SaveService(Service finder, Service primfinder)
        {
            //IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            //Returns ret = OpenDb(conn_db, true);
            //if (!ret.result) return ret;

            //using (IDbTransaction transaction = conn_db.BeginTransaction())
            //{
            //    ret = SaveService(finder, primfinder, conn_db, transaction);
            //    if (ret.result) transaction.Commit();
            //    else transaction.Rollback();
            //}
            Returns ret;
            using (var dbSaveServices = new DbSaveServices())
            {
                ret = dbSaveServices.SaveService(finder, primfinder);
            }
            return ret;


            //IDbTransaction transaction;
            //try { transaction = conn_db.BeginTransaction(); }
            //catch { transaction = null; }

            //ret = SaveService(finder, primfinder, conn_db, transaction);

            //if (transaction != null)
            //{
            //    if (ret.result) transaction.Commit();
            //    else transaction.Rollback();
            //}

            //conn_db.Close();
            //return ret;
        }

        /// <summary>
        /// Сохранение одной услуги в справочник
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="conn_db"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public Returns SaveServiceToDictionary(Service finder, IDbConnection conn_db, IDbTransaction transaction)
        {
            if (finder.nzp_user < 1) return new Returns(false, "Не определен пользователь");
            if (finder.service.Trim() == "") return new Returns(false, "Не задано наименование услуги", -1);
            if (finder.service_small.Trim() == "") return new Returns(false, "Не задано краткое наименование услуги", -1);
            if (finder.service_name.Trim() == "") return new Returns(false, "Не задано наименование услуги для счетов на оплату", -1);
            if (!Points.IsSmr) if (finder.nzp_serv > 0 && finder.nzp_serv < 100000 && finder.pref == "") return new Returns(false, "Редактировать можно только пользовательские услуги", -1);

            finder.service = finder.service.Trim();
            finder.service_small = finder.service_small.Trim();
            finder.service_name = finder.service_name.Trim();

            if (finder.pref == "") finder.pref = Points.Pref;
            string table = finder.pref + "_kernel" + tableDelimiter + "services";
            string sql = "select nzp_serv from " + table + " where ordering = " + finder.ordering + " and nzp_serv <> " + finder.nzp_serv;

            MyDataReader reader;
            Returns ret = ExecRead(conn_db, transaction, out reader, sql, true);
            if (!ret.result) return ret;

            if (reader.Read())
            {
                ret.result = false;
                ret.text = "Порядковый номер услуги должен быть уникальным";
                ret.tag = -1;
                reader.Close();
                return ret;
            }
            reader.Close();

            string field_ins = "";
            string value_ins = "";
            if (finder.pref == Points.Pref)
            {
                field_ins = ",ordering_std";
                value_ins = ", " + finder.ordering;
            }

            if (finder.nzp_serv > 0)
            {
                string field_upd = "";
                if (finder.pref == Points.Pref) field_upd = ", ordering_std = " + finder.ordering;
                else field_upd = "";
                sql = "select nzp_serv from " + table + " where nzp_serv = " + finder.nzp_serv;
                ret = ExecRead(conn_db, transaction, out reader, sql, true);
                if (!ret.result) return ret;

                if (reader.Read())
                {
                    sql = "update " + table + " set service = " + Utils.EStrNull(finder.service) +
                        ", service_small = " + Utils.EStrNull(finder.service_small) +
                        ", service_name = " + Utils.EStrNull(finder.service_name) +
                        ", ed_izmer = " + Utils.EStrNull(finder.ed_izmer) +
                        ", nzp_measure = " + finder.nzp_measure +
                        ", ordering = " + finder.ordering +
                        field_upd +
                        " where nzp_serv = " + finder.nzp_serv;
                }
                else
                {
                    sql = "insert into " + table + " (nzp_serv, service, service_small, service_name, ed_izmer, nzp_measure, type_lgot, ordering" +
                        //is_lgot, 
                    field_ins + ")" +
                        " values (" + finder.nzp_serv +
                        ", " + Utils.EStrNull(finder.service) +
                        ", " + Utils.EStrNull(finder.service_small) +
                        ", " + Utils.EStrNull(finder.service_name) +
                        ", " + Utils.EStrNull(finder.ed_izmer) +
                        ", " + finder.nzp_measure +
                        ", 2" +
                        ", " + finder.ordering +
                        // ", 0" +
                        value_ins + ")";
                }
                reader.Close();
            }
            else
            {
                sql = "select max(nzp_serv) from " + table;
                object obj = ExecScalar(conn_db, transaction, sql, out ret, true);
                if (!ret.result)
                {
                    return ret;
                }
                int nzp_serv;
                if (obj == null)
                {
                    nzp_serv = 100000;
                }
                else
                {
                    try { nzp_serv = Convert.ToInt32(obj); }
                    catch (Exception ex)
                    {
                        ret.result = false;
                        ret.text = "Ошибка при определении кода услуги";
                        MonitorLog.WriteLog("Ошибка в функции сохранения услуги:" + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                        return ret;
                    }
                    if (nzp_serv < 100000) nzp_serv = 100000;
                    else ++nzp_serv;
                }
                reader.Close();

                finder.nzp_serv = nzp_serv;
                sql = "insert into " + table + " (nzp_serv, service, service_small, service_name, ed_izmer,nzp_measure, type_lgot, ordering" +
                    //is_lgot, 
                    field_ins + ")" +
                        " values (" + nzp_serv +
                        ", " + Utils.EStrNull(finder.service) +
                        ", " + Utils.EStrNull(finder.service_small) +
                        ", " + Utils.EStrNull(finder.service_name) +
                        ", " + Utils.EStrNull(finder.ed_izmer) +
                        ", " + finder.nzp_measure +
                        ", 2" +
                        ", " + finder.ordering +
                    //   ", 0" +
                        value_ins + ")";
            }

            ret = ExecSQL(conn_db, transaction, sql, true);
            if (ret.result)
            {
                ret.tag = finder.nzp_serv;
            }

            return ret;
        }

        /// <summary>
        /// Сохранение доступной услуги в l_foss
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="primfinder"></param>
        /// <param name="conn_db"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public Returns SaveServiceToLFoss(Service finder, IDbConnection conn_db, IDbTransaction transaction)
        {
            Returns ret = Utils.InitReturns();

            if (finder.nzp_user < 1) return new Returns(false, "Не определен пользователь");
            if (!Utils.GetParams(finder.prms, Constants.act_add_serv) && !Utils.GetParams(finder.prms, Constants.act_del_serv)) return new Returns(false, "Не задана операция");
            if (Utils.GetParams(finder.prms, Constants.act_add_serv))
            {
                if (finder.nzp_serv < 1 || finder.nzp_supp < 1 || finder.nzp_frm < 1) return new Returns(false, "Неверные входные параметры");
            }
            else if (Utils.GetParams(finder.prms, Constants.act_del_serv))
            {
                if (finder.nzp_foss < 1) return new Returns(false, "Не задан период действия услуги", -1);
            }

            string sql;

            if (Utils.GetParams(finder.prms, Constants.act_add_serv))
            {
                sql = "select nzp_foss from " + finder.pref + "_kernel" + tableDelimiter + "l_foss" +
                    " where nzp_serv = " + finder.nzp_serv + " and nzp_supp = " + finder.nzp_supp + " and nzp_frm = " + finder.nzp_frm +
                    " and dat_s <= " + Utils.EStrNull(finder.dat_po) + " and dat_po >= " + Utils.EStrNull(finder.dat_s);
                MyDataReader reader;
                ret = ExecRead(conn_db, transaction, out reader, sql, true);
                if (!ret.result) return ret;

                if (reader.Read())
                {
                    reader.Close();
                    ret = new Returns(false, "Добавляемый период не должен пересекаться с имеющимися периодами действия услуги с тем же поставщиком и формулой расчета", -1);
                    return ret;
                }
                reader.Close();

                sql = "insert into " + finder.pref + "_kernel" + tableDelimiter + "l_foss (nzp_serv, nzp_supp, nzp_frm, dat_s, dat_po) values (" +
                    finder.nzp_serv + "," + finder.nzp_supp + "," + finder.nzp_frm + "," + Utils.EStrNull(finder.dat_s) + "," + Utils.EStrNull(finder.dat_po) + ")";
                ret = ExecSQL(conn_db, transaction, sql, true);
            }
            else if (Utils.GetParams(finder.prms, Constants.act_del_serv))
            {
                sql = "delete from " + finder.pref + "_kernel" + tableDelimiter + "l_foss where nzp_foss = " + finder.nzp_foss;
                ret = ExecSQL(conn_db, transaction, sql, true);
            }

            return ret;
        }

        /// <summary> 1. Добавление или удаление услуги лицевым счетам (одному ЛС или выбранным ЛС)
        /// 2. Добавление или удаление периода действия услуги с информацией о действующем в этом периоде поставщике и формуле расчета
        /// </summary>
        public Returns SaveService(Service finder, Service primfinder, IDbConnection conn_db, IDbTransaction transaction)
        {
            if (Utils.GetParams(finder.prms, Constants.page_services))
            {
                //сохранение услуги в справочник
                return SaveServiceToDictionary(finder, conn_db, transaction);
            }
            else if (Utils.GetParams(finder.prms, Constants.page_available_service))
            {
                //сохранение доступной услуги в l_foss
                return SaveServiceToLFoss(finder, conn_db, transaction);
            }

            #region проверка входных параметров
            if (finder.nzp_user < 1) return new Returns(false, "Не определен пользователь");

            if (!Utils.GetParams(finder.prms, Constants.act_add_serv) && !Utils.GetParams(finder.prms, Constants.act_del_serv)) return new Returns(false, "Не задана операция");

            if (finder.pref.Trim() == "") return new Returns(false, "Не задан префикс базы данных");
            if (finder.nzp_serv < 1) return new Returns(false, "Не задана услуга");
            if (Utils.GetParams(finder.prms, Constants.act_add_serv) && (finder.nzp_supp < 1 || finder.nzp_frm < 1)) return new Returns(false, "Не задан поставщик или формула расчета");
            #endregion

            Returns ret = Utils.InitReturns();//инициализация результата
            if (finder.pref.Trim() == "") finder.pref = Points.Pref;

            SaveModes mode = SaveModes.None;
            if (finder.nzp_kvar > 0) mode = SaveModes.ServiceOfOneKvar;
            else if (Utils.GetParams(finder.prms, Constants.page_group_supp_formuls)) mode = SaveModes.ServiceOfGroupKvar;
            else if (Utils.GetParams(finder.prms, Constants.page_group_supp_formuls_dom)) mode = SaveModes.ServiceOfGroupDom;
            else if (Utils.GetParams(finder.prms, Constants.page_spisservdom)) mode = SaveModes.ServiceOfOneDom;

            // Алгоритм
            // 1. Если это групповая операция
            //   1.1. Определить список префиксов БД
            //   1.2. Организовать цикл по префиксам
            //     1.2.1. Для каждого префикса выполнить сохранение
            // 2. Если это операция с одним домом или ЛС
            //   2.1. Выполнить сохранение

            string sql;

            string pref = finder.pref.Trim();

            string tXX_spls = "t" + Convert.ToString(finder.nzp_user) + "_selectedls" + finder.listNumber;
            string tXX_spls_full = "";
            string tXX_spdom = "t" + Convert.ToString(finder.nzp_user) + "_spdom";
            string tXX_spdom_full = "";
            int numLs = 0;

            if (mode == SaveModes.ServiceOfGroupKvar || mode == SaveModes.ServiceOfGroupDom) // групповая операция по выбранным спискам ЛС или домов
            {
#if PG
                if (mode == SaveModes.ServiceOfGroupKvar && (finder.listNumber < 0 || !TableInWebCashe(conn_db, tXX_spls)))
                {
                    return new Returns(false, "Не выбран список лицевых счетов", -1);
                }
                else if (mode == SaveModes.ServiceOfGroupDom && !TableInWebCashe(conn_db, tXX_spdom))
                {
                    return new Returns(false, "Не выбран список домов", -1);
                }

                tXX_spls_full = pgDefaultDb + "." + tXX_spls;
                tXX_spdom_full = pgDefaultDb + "." + tXX_spdom;
#else
                IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
                ret = OpenDb(conn_web, true);
                if (!ret.result) return ret;

                if (mode == SaveModes.ServiceOfGroupKvar && (finder.listNumber < 0 || !TableInWebCashe(conn_web, tXX_spls)))
                {
                    conn_web.Close();
                    return new Returns(false, "Не выбран список лицевых счетов", -1);
                }
                else if (mode == SaveModes.ServiceOfGroupDom && !TableInWebCashe(conn_web, tXX_spdom))
                {
                    conn_web.Close();
                    return new Returns(false, "Не выбран список домов", -1);
                }
                tXX_spls_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXX_spls;
                tXX_spdom_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXX_spdom;
#endif
            }
            else if (mode == SaveModes.ServiceOfOneKvar)
            {
                string s = "select max(num_ls) num_ls from " + pref + "_data" + tableDelimiter + "kvar where nzp_kvar = " + finder.nzp_kvar;
                object id = ExecScalar(conn_db, transaction, s, out ret, true);
                if (!ret.result) return ret;
                if (!Int32.TryParse(id.ToString(), out numLs))
                {
                    return new Returns(false, "Ошибка при определении номера лицевого счета");
                }
            }
            else if (mode == SaveModes.ServiceOfOneDom)
            {
                if (primfinder == null) return new Returns(false, "Ошибка при определении списка лицевых счетов");
            }

            //укзываем таблицу для редактирования
            var editData = new EditInterData();
            editData.pref = pref;
            editData.nzp_wp = Points.GetPoint(editData.pref).nzp_wp;

            editData.nzp_user = finder.nzp_user;
            editData.webLogin = finder.webLogin;
            editData.webUname = finder.webUname;
            editData.primary = "nzp_tarif";
            editData.table = "tarif";
            editData.todelete = Utils.GetParams(finder.prms, Constants.act_del_serv);

            //указываем вставляемый период
            editData.dat_s = finder.dat_s;
            if (finder.dat_po == DateTime.MaxValue.ToString("dd.MM.yyyy")) editData.dat_po = "01.01.3000";
            else editData.dat_po = finder.dat_po;
            editData.intvType = enIntvType.intv_Day;

            string kvar = pref + "_data" + tableDelimiter + "kvar";
            string kvar_filter = " 1=0 ";
            string dom_filter = " 1=0 ";

            //условие выборки данных из целевой таблицы
            editData.dopFind = new List<string>();
            sql = " and nzp_serv = " + finder.nzp_serv;
            if (editData.todelete && finder.nzp_supp > 0)
                sql += " and nzp_supp = " + finder.nzp_supp;
            if (mode == SaveModes.ServiceOfOneKvar) sql += " and nzp_kvar = " + finder.nzp_kvar;
            else if (mode == SaveModes.ServiceOfGroupKvar)
            {
                kvar_filter = "nzp_kvar in (select nzp_kvar from " + tXX_spls_full + " where mark = 1 and pref = " + Utils.EStrNull(pref) + ") ";
                sql += " and " + kvar_filter;
            }
            else if (mode == SaveModes.ServiceOfGroupDom)
            {
                kvar_filter = "nzp_kvar in (select b.nzp_kvar from " + tXX_spdom_full + " a, " + kvar + " b where a.pref = " + Utils.EStrNull(pref) + " and a.mark = 1 and a.nzp_dom = b.nzp_dom) ";
                dom_filter = "nzp_dom in (select nzp_dom from " + tXX_spdom_full + " where pref = " + Utils.EStrNull(pref) + " and mark = 1) ";
                sql += " and " + kvar_filter;
            }
            else if (mode == SaveModes.ServiceOfOneDom)
            {
                if (primfinder.year_ < 0) return new Returns(false, "Не задан год", -1);
                if (primfinder.month_ < 0) return new Returns(false, "Не задан месяц", -1);
                if (primfinder.nzp_dom < 0) return new Returns(false, "Не указан дом", -1);
                if (primfinder.nzp_serv < 0) return new Returns(false, "Не указана услуга", -1);
                if (primfinder.nzp_supp < 0) return new Returns(false, "Не указан поставщик", -1);
                if (primfinder.nzp_frm < 0) return new Returns(false, "Не указана формула расчета", -1);
                DateTime dat = new DateTime(primfinder.year_, primfinder.month_, 1);
                kvar_filter = "nzp_kvar in (Select k.nzp_kvar " +
                    " From " + pref + "_data" + tableDelimiter + "tarif t, " + pref + "_data" + tableDelimiter + "kvar k " +
                    " Where k.nzp_dom = " + primfinder.nzp_dom +
                                " and t.nzp_kvar=k.nzp_kvar " +
                                " and t.nzp_serv = " + primfinder.nzp_serv +
                                " and t.nzp_supp = " + primfinder.nzp_supp +
                                " and t.nzp_frm = " + primfinder.nzp_frm +
                                " and t.is_actual <> 100 " +
                                " and t.dat_s < '" + dat.AddMonths(1).ToShortDateString() + "' " +
                                " and t.dat_po>='" + dat.ToShortDateString() + "')";
                sql += " and " + kvar_filter;
            }

            editData.dopFind.Add(sql);

            //перечисляем ключевые поля и значения (со знаком сравнения!)
            Dictionary<string, string> keys = new Dictionary<string, string>();
            if (mode == SaveModes.ServiceOfOneKvar)
            {
                keys.Add("nzp_kvar", "1|nzp_kvar|kvar|nzp_kvar = " + finder.nzp_kvar); //ссылка на ключевую таблицу
            }
            else if (mode == SaveModes.ServiceOfGroupKvar)
            {
                keys.Add("nzp_kvar", "1|nzp_kvar|" + tXX_spls_full + "|" + kvar_filter); //ссылка на ключевую таблицу
                keys.Add("num_ls", "1|num_ls|" + tXX_spls_full + "|num_ls in (select num_ls from " + tXX_spls_full + " where  mark = 1 and  pref = " + Utils.EStrNull(pref) + ") "); //ссылка на ключевую таблицу
            }
            else if (mode == SaveModes.ServiceOfGroupDom)
            {
                keys.Add("nzp_kvar", "1|nzp_kvar|kvar|" + dom_filter); //ссылка на ключевую таблицу
                keys.Add("num_ls", "1|num_ls|kvar|" + dom_filter); //ссылка на ключевую таблицу
            }
            else if (mode == SaveModes.ServiceOfOneDom)
            {
                keys.Add("nzp_kvar", "1|nzp_kvar|kvar|" + kvar_filter); //ссылка на ключевую таблицу
                keys.Add("num_ls", "1|num_ls|kvar|" + kvar_filter); //ссылка на ключевую таблицу
            }
            keys.Add("nzp_serv", "2|" + finder.nzp_serv);
            if (editData.todelete && finder.nzp_supp > 0)
            {
                keys.Add("nzp_supp", "2|" + finder.nzp_supp);
            }
            editData.keys = keys;

            //перечисляем поля и значения этих полей, которые вставляются
            Dictionary<string, string> vals = new Dictionary<string, string>();
            if (!editData.todelete || finder.nzp_supp <= 0)
            {
                vals.Add("nzp_supp", finder.nzp_supp.ToString());
            }
            vals.Add("nzp_frm", finder.nzp_frm.ToString());
            vals.Add("tarif", "0");
            if (mode == SaveModes.ServiceOfOneKvar)
            {
                vals.Add("num_ls", numLs.ToString());
            }
            editData.vals = vals;

            try
            {
                if (editData.todelete)
                {
                    #region Добавление в sys_events события 'Закрытие услуги'
                    var serv = ExecScalar(conn_db, transaction, "select service from " + Points.Pref + "_kernel" + tableDelimiter + "services where nzp_serv = " + finder.nzp_serv, out ret, true);
                    DbAdmin.InsertSysEvent(new SysEvents()
                    {
                        pref = finder.pref,
                        nzp_user = finder.nzp_user,
                        nzp_dict = 6609,
                        nzp_obj = primfinder != null ? primfinder.nzp_dom : finder.nzp_dom,
                        note = "Услуга " + (serv != null ? serv.ToString().Trim() : "") + " была закрыта"
                    }, transaction, conn_db);
                    #endregion
                }
                else
                {
                    #region Добавление в sys_events события 'Открытие услуги'
                    var serv = ExecScalar(conn_db, transaction, "select service from " + Points.Pref + "_kernel" + tableDelimiter + "services where nzp_serv = " + finder.nzp_serv, out ret, true);
                    DbAdmin.InsertSysEvent(new SysEvents()
                    {
                        pref = finder.pref,
                        nzp_user = finder.nzp_user,
                        nzp_dict = 6608,
                        nzp_obj = finder.nzp_dom,
                        note = "Услуга " + (serv != null ? serv.ToString().Trim() : "") + " была открыта"
                    }, transaction, conn_db);
                    #endregion
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
            }

            //вызов сервиса
            DbEditInterData db2 = new DbEditInterData();

            try
            {
                db2.Saver(conn_db, transaction, editData, out ret);

                if (Points.RecalcMode == RecalcModes.AutomaticWithCancelAbility)
                {
                    if (mode == SaveModes.ServiceOfOneKvar ||
                        mode == SaveModes.ServiceOfOneDom ||
                        mode == SaveModes.ServiceOfGroupKvar ||
                        mode == SaveModes.ServiceOfGroupDom)
                    {
                        EditInterDataMustCalc eid = new EditInterDataMustCalc();

                        eid.nzp_wp = editData.nzp_wp;
                        eid.pref = editData.pref;
                        eid.nzp_user = editData.nzp_user;
                        //eid.dat_s = "'" + new DateTime(Points.CalcMonth.year_, Points.CalcMonth.month_, 1).ToShortDateString() + "'";
                        //eid.dat_po = "'" + new DateTime(Points.CalcMonth.year_, Points.CalcMonth.month_, 1).AddMonths(1).AddDays(-1).ToShortDateString() + "'";
                        eid.intvType = editData.intvType;
                        eid.table = editData.table;
                        eid.primary = editData.primary;
                        eid.mcalcType = enMustCalcType.mcalc_Serv;

                        eid.dopFind = new List<string>();
                        eid.keys = new Dictionary<string, string>();
                        eid.vals = new Dictionary<string, string>();

                        switch (mode)
                        {
                            case SaveModes.ServiceOfOneKvar:
                                eid.dopFind.Add(" and nzp_kvar = " + finder.nzp_kvar + " and nzp_serv = " + finder.nzp_serv);
                                break;

                            case SaveModes.ServiceOfOneDom:
                                //eid.dopFind.Add(" and nzp_kvar in (select nzp_kvar from " + Points.Pref + "_data@" + conn_db.Server + ":kvar where nzp_dom = " + finder.nzp_dom + ") and nzp_serv = " + finder.nzp_serv);
                                DateTime dat = new DateTime(primfinder.year_, primfinder.month_, 1);
                                eid.dopFind.Add(" and nzp_kvar in (Select k.nzp_kvar " +
                                    " From " + pref + "_data" + tableDelimiter + "tarif t, " + pref + "_data" + tableDelimiter + "kvar k " +
                                    " Where k.nzp_dom = " + primfinder.nzp_dom +
                                        " and t.nzp_kvar=k.nzp_kvar " +
                                        " and t.nzp_serv = " + primfinder.nzp_serv +
                                        " and t.nzp_supp = " + primfinder.nzp_supp +
                                        " and t.nzp_frm = " + primfinder.nzp_frm +
                                        " and t.is_actual <> 100 " +
                                        " and t.dat_s < '" + dat.AddMonths(1).ToShortDateString() + "' " +
                                        " and t.dat_po >= '" + dat.ToShortDateString() + "')");
                                break;

                            case SaveModes.ServiceOfGroupKvar:
                                eid.dopFind.Add(" and nzp_kvar in (select nzp_kvar from " + tXX_spls_full + " where mark = 1 and pref = " + Utils.EStrNull(pref) + ") ");
                                break;

                            case SaveModes.ServiceOfGroupDom:
                                eid.dopFind.Add(" and nzp_kvar in (select b.nzp_kvar from " + tXX_spdom_full + " a, " + kvar + " b where a.pref = " + Utils.EStrNull(pref) + " and a.mark = 1 and a.nzp_dom = b.nzp_dom) ");
                                break;
                        }
                        eid.comment_action = finder.comment_action;
                        db2.MustCalc(conn_db, transaction, eid, out ret);
                    }
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                ret.result = false;
                ret.text = ex.Message;
            }
            finally
            {
                db2.Close();
            }

            return ret;
        }

        public static string MakeWhereString(Service finder, out Returns ret)
        {
            DateTime ds = DateTime.MinValue, dpo = DateTime.MaxValue;
            if (finder.dat_s != "" && !DateTime.TryParse(finder.dat_s, out ds))
            {
                ret = new Returns(false, "Неправильно введена дата начала периода", -1);
                return "0";
            }
            if (finder.dat_po != "" && !DateTime.TryParse(finder.dat_po, out dpo))
            {
                ret = new Returns(false, "Неправильно введена дата окончания периода", -1);
                return "0";
            }

            ret = Utils.InitReturns();

            string sql = "";
            string where = " and t.is_actual <> 100";
            if (ds != DateTime.MinValue || dpo != DateTime.MaxValue)
            {
                if (ds == DateTime.MinValue) ds = dpo;
                else if (dpo == DateTime.MaxValue) dpo = ds;

                where += " and t.dat_s <= " + Utils.EStrNull(dpo.ToShortDateString());
                where += " and t.dat_po >= " + Utils.EStrNull(ds.ToShortDateString());
            }

            if (finder.nzp_serv > 0 || finder.nzp_supp > 0 || finder.nzp_frm > 0 || (finder.nzp_serv == 0 && finder.nzp_supp == 0 && finder.nzp_frm == 0))
            {
                if (finder.nzp_serv > 0) where += " and t.nzp_serv = " + finder.nzp_serv;
                else if (finder.nzp_serv < 0) where += " and t.nzp_serv <> " + -finder.nzp_serv;
                if (finder.nzp_supp > 0) where += " and t.nzp_supp = " + finder.nzp_supp;
                else if (finder.nzp_supp < 0) where += " and t.nzp_supp <> " + -finder.nzp_supp;
                if (finder.nzp_frm > 0) where += " and t.nzp_frm = " + finder.nzp_frm;
                else if (finder.nzp_frm < 0) where += " and t.nzp_frm <> " + -finder.nzp_frm;
            }
            else
            {
                if (finder.nzp_serv < 0) where += " and t.nzp_serv = " + -finder.nzp_serv;
                if (finder.nzp_supp < 0) where += " and t.nzp_supp = " + -finder.nzp_supp;
                if (finder.nzp_frm < 0) where += " and t.nzp_frm = " + -finder.nzp_frm;
            }

            if (Utils.GetParams(finder.prms, Constants.page_spisls))
            {
                if (finder.nzp_serv > 0 || finder.nzp_supp > 0 || finder.nzp_frm > 0 || (finder.nzp_serv == 0 && finder.nzp_supp == 0 && finder.nzp_frm == 0))
                {
                    sql = "Select count(*) From PREFX_data:tarif t Where t.nzp_kvar = k.nzp_kvar" + where;
                }
                else
                {
#if PG
                    sql = "Select count(*) From PREFX_data.kvar k1 Where k1.nzp_kvar = k.nzp_kvar and k1.nzp_kvar not in (Select t.nzp_kvar from PREFX_data.tarif t where t.nzp_kvar = k1.nzp_kvar" + where + ")";
#else
                    sql = "Select count(*) From PREFX_data:kvar k1 Where k1.nzp_kvar = k.nzp_kvar and k1.nzp_kvar not in (Select t.nzp_kvar from PREFX_data:tarif t where t.nzp_kvar = k1.nzp_kvar" + where + ")";
#endif
                }
            }
            else if (Utils.GetParams(finder.prms, Constants.page_spisdom))
            {
                if (finder.nzp_serv > 0 || finder.nzp_supp > 0 || finder.nzp_frm > 0 || (finder.nzp_serv == 0 && finder.nzp_supp == 0 && finder.nzp_frm == 0))
                {
#if PG
                    sql = "Select count(*) From PREFX_data.kvar k1, PREFX_data.tarif t Where k1.nzp_dom = d.nzp_dom and t.nzp_kvar = k1.nzp_kvar" + where;
#else
                    sql = "Select count(*) From PREFX_data:kvar k1, PREFX_data:tarif t Where k1.nzp_dom = d.nzp_dom and t.nzp_kvar = k1.nzp_kvar" + where;
#endif
                }
                else
                {
#if PG
                    sql = "Select count(*) From PREFX_data.kvar k1 Where k1.nzp_dom = d.nzp_dom and k1.nzp_kvar not in (Select t.nzp_kvar from PREFX_data.tarif t where t.nzp_kvar = k1.nzp_kvar" + where + ")";
#else
                    sql = "Select count(*) From PREFX_data:kvar k1 Where k1.nzp_dom = d.nzp_dom and k1.nzp_kvar not in (Select t.nzp_kvar from PREFX_data:tarif t where t.nzp_kvar = k1.nzp_kvar" + where + ")";
#endif
                }
            }

            return sql;
        }

        /// <summary>
        /// Возвращает список услуг дома
        /// </summary>
        public List<Service> FindDomService(Service finder, out Returns ret)
        {
            #region Проверка входных параметров
            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Не определен пользователь");
                return null;
            }
            if (finder.pref.Trim() == "")
            {
                ret = new Returns(false, "Не определен префикс");
                return null;
            }

            if (finder.nzp_dom < 1)
            {
                ret = new Returns(false, "Не задан дом");
                return null;
            }

            if (finder.month_ < 1)
            {
                ret = new Returns(false, "Не задан расчетный месяц");
                return null;
            }

            if (finder.year_ < 1)
            {
                ret = new Returns(false, "Не задан расчетный год");
                return null;
            }
            #endregion

            DateTime dat = new DateTime(finder.year_, finder.month_, 1);

            string connectionString = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            string sql = "", where = "";

            if (finder.RolesVal != null)
                foreach (_RolesVal role in finder.RolesVal)
                {
                    if (role.tip == Constants.role_sql & role.kod == Constants.role_sql_serv) where += " and t.nzp_serv in (" + role.val + ")";
                    if (role.tip == Constants.role_sql & role.kod == Constants.role_sql_supp) where += " and t.nzp_supp in (" + role.val + ")";
                }
            if (finder.nzp_serv > 0) where += " and s.nzp_serv = " + finder.nzp_serv;

            ExecSQL(conn_db, "drop table t_tarif_dom", false);
#if PG
            sql = "Create temp table t_tarif_dom " +
                            "( nzp_dom     integer, " +
                              "nzp_serv    integer, " +
                              "nzp_supp    integer, " +
                              "nzp_frm     integer, " +
                              "cnt_ls      integer, " +
                              "ordering    integer, " +
                              "service     CHAR(100), " +
                              "name_supp   CHAR(100), " +
                              "name_frm    CHAR(60), " +
                              "tarif       numeric(14,2) default 0.00 " +
                            ")";
#else
            sql = "Create temp table t_tarif_dom " +
                            "( nzp_dom     integer, " +
                              "nzp_serv    integer, " +
                              "nzp_supp    integer, " +
                              "nzp_frm     integer, " +
                              "cnt_ls      integer, " +
                              "ordering    integer, " +
                              "service     CHAR(100), " +
                              "name_supp   CHAR(100), " +
                              "name_frm    CHAR(60), " +
                              "tarif       decimal(14,2) default 0.00 " +
                            ") with no log";
#endif
            ExecSQL(conn_db, sql, false);

            DateTime datnow = new DateTime(finder.year_, finder.month_, 1);
#if PG
            string prm_3 = finder.pref + "_data." + "prm_3";
#else
            string prm_3 = finder.pref + "_data@" + DBManager.getServer(conn_db) + ":" + "prm_3";
#endif

#if PG
            sql = "insert into t_tarif_dom (cnt_ls, nzp_serv,service,ordering, nzp_supp,name_supp, nzp_frm,name_frm) " +
                        "Select count(distinct t.nzp_kvar) as cnt_ls, " +
                         "t.nzp_serv, s.service, min(s.ordering), " +
                         "t.nzp_supp, sp.name_supp, " +
                         "t.nzp_frm, f.name_frm  " +
                        "from " + finder.pref + "_data.tarif t, " + Points.Pref + "_data.kvar k, " + finder.pref + "_kernel.services s, " + finder.pref + "_kernel.supplier sp, " + finder.pref + "_kernel.formuls f " +
                         "Where k.nzp_dom = " + finder.nzp_dom +
                         " and t.nzp_kvar=k.nzp_kvar and s.nzp_serv = t.nzp_serv  and sp.nzp_supp = t.nzp_supp and f.nzp_frm = t.nzp_frm " +
                         " and t.is_actual <> 100 " + where +
                         " and t.dat_s < '" + dat.AddMonths(1).ToShortDateString() + "' " +
                         " and t.dat_po>='" + dat.ToShortDateString() + "' " +
                         " and is_open='1' " +
                         " group by t.nzp_serv, s.service, t.nzp_frm, t.nzp_supp, sp.name_supp, f.name_frm";
#else
            sql = "insert into t_tarif_dom (cnt_ls, nzp_serv,service,ordering, nzp_supp,name_supp, nzp_frm,name_frm) " +
                        "Select count(unique t.nzp_kvar) as cnt_ls, " +
                         "t.nzp_serv, s.service, min(s.ordering), " +
                         "t.nzp_supp, sp.name_supp, " +
                         "t.nzp_frm, f.name_frm  " +
                        "from " + finder.pref + "_data:tarif t, " + finder.pref + "_data:kvar k, " + finder.pref + "_kernel:services s, " + finder.pref + "_kernel:supplier sp, " + finder.pref + "_kernel:formuls f " +
                         "Where k.nzp_dom = " + finder.nzp_dom +
                         " and t.nzp_kvar=k.nzp_kvar and s.nzp_serv = t.nzp_serv  and sp.nzp_supp = t.nzp_supp and f.nzp_frm = t.nzp_frm " +
                         " and t.is_actual <> 100 " + where +
                         " and t.dat_s < '" + dat.AddMonths(1).ToShortDateString() + "' " +
                         " and t.dat_po>='" + dat.ToShortDateString() + "' " +
                         " and (select max(val_prm) from " + prm_3 + " p3 where p3.nzp_prm=51 and p3.nzp= t.nzp_kvar " +
                                " and " + Utils.EStrNull(datnow.ToShortDateString()) + " between p3.dat_s and p3.dat_po and p3.is_actual <> 100)=1 " +
                         " group by t.nzp_serv, s.service, t.nzp_frm, t.nzp_supp, sp.name_supp, f.name_frm";
#endif
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }

#if PG
            sql = "update t_tarif_dom set tarif = ( " +
                            "select max(p.val_prm) from " + finder.pref + "_data.prm_5 p," + finder.pref + "_kernel.formuls_opis f " +
                            "where f.nzp_frm=t_tarif_dom.nzp_frm and p.nzp_prm=f.nzp_prm_tarif_bd and p.is_actual <> 100  " +
                             "and p.dat_s < '" + dat.AddMonths(1).ToShortDateString() + "' " +
                             "and p.dat_po>='" + dat.ToShortDateString() + "' " +
                            ")::numeric " +
                        " where 0<( " +
                            " select count(*) from " + finder.pref + "_data.prm_5 p," + finder.pref + "_kernel.formuls_opis f " +
                            " where f.nzp_frm=t_tarif_dom.nzp_frm and p.nzp_prm=f.nzp_prm_tarif_bd and p.is_actual <> 100 " +
                             " and p.dat_s < '" + dat.AddMonths(1).ToShortDateString() + "' " +
                             " and p.dat_po>='" + dat.ToShortDateString() + "' " +
                            ") ";
#else
            sql = "update t_tarif_dom set tarif = ( " +
                            "select max(p.val_prm+0) from " + finder.pref + "_data:prm_5 p," + finder.pref + "_kernel:formuls_opis f " +
                            "where f.nzp_frm=t_tarif_dom.nzp_frm and p.nzp_prm=f.nzp_prm_tarif_bd and p.is_actual <> 100  " +
                             "and p.dat_s < '" + dat.AddMonths(1).ToShortDateString() + "' " +
                             "and p.dat_po>='" + dat.ToShortDateString() + "' " +
                            ") " +
                        "where 0<( " +
                            "select count(*) from " + finder.pref + "_data:prm_5 p," + finder.pref + "_kernel:formuls_opis f " +
                            "where f.nzp_frm=t_tarif_dom.nzp_frm and p.nzp_prm=f.nzp_prm_tarif_bd and p.is_actual <> 100 " +
                             "and p.dat_s < '" + dat.AddMonths(1).ToShortDateString() + "' " +
                             "and p.dat_po>='" + dat.ToShortDateString() + "' " +
                            ") ";
#endif
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }

            IDataReader reader;
            sql = "select nzp_serv, nzp_supp, nzp_frm, cnt_ls, service, name_supp, name_frm, tarif from t_tarif_dom order by ordering, name_supp, name_frm";
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }

            List<Service> list = new List<Service>();

            try
            {
                while (reader.Read())
                {
                    Service zap = new Service();

                    if (reader["nzp_serv"] != DBNull.Value) zap.nzp_serv = (int)reader["nzp_serv"];
                    if (reader["nzp_supp"] != DBNull.Value) zap.nzp_supp = Convert.ToInt64(reader["nzp_supp"]);
                    if (reader["nzp_frm"] != DBNull.Value) zap.nzp_frm = (int)reader["nzp_frm"];
                    if (reader["cnt_ls"] != DBNull.Value) zap.cnt_ls = (int)reader["cnt_ls"];
                    if (reader["service"] != DBNull.Value) zap.service = ((string)reader["service"]).Trim();
                    if (reader["name_supp"] != DBNull.Value) zap.name_supp = ((string)reader["name_supp"]).Trim();
                    if (reader["name_frm"] != DBNull.Value) zap.name_frm = ((string)reader["name_frm"]).Trim();
                    if (reader["tarif"] != DBNull.Value) zap.tarif = Convert.ToDecimal(reader["tarif"]);
                    if (zap.tarif == 0) zap.tarif_s = "-";
                    else zap.tarif_s = zap.tarif.ToString("0.00");
                    list.Add(zap);
                }
                if (reader != null) reader.Close();
                ExecSQL(conn_db, "drop table t_tarif_dom", false);
            }
            catch
            {
                if (reader != null) reader.Close();
                conn_db.Close();
                return null;
            }
            conn_db.Close();
            return list;
        }


        /// <summary>
        /// Возвращает список услуг дома в системе новых договоров
        /// </summary>
        public List<Service> FindDomServiceNewDog(Service finder, out Returns ret)
        {
            #region Проверка входных параметров
            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Не определен пользователь");
                return null;
            }
            if (finder.pref.Trim() == "")
            {
                ret = new Returns(false, "Не определен префикс");
                return null;
            }

            if (finder.nzp_dom < 1)
            {
                ret = new Returns(false, "Не задан дом");
                return null;
            }

            if (finder.month_ < 1)
            {
                ret = new Returns(false, "Не задан расчетный месяц");
                return null;
            }

            if (finder.year_ < 1)
            {
                ret = new Returns(false, "Не задан расчетный год");
                return null;
            }
            #endregion

            DateTime dat = new DateTime(finder.year_, finder.month_, 1);

            string connectionString = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            string sql = "", where = "";

            if (finder.RolesVal != null)
                foreach (_RolesVal role in finder.RolesVal)
                {
                    if (role.tip == Constants.role_sql & role.kod == Constants.role_sql_serv) where += " and t.nzp_serv in (" + role.val + ")";
                    if (role.tip == Constants.role_sql & role.kod == Constants.role_sql_supp) where += " and t.nzp_supp in (" + role.val + ")";
                }
            if (finder.nzp_serv > 0) where += " and s.nzp_serv = " + finder.nzp_serv;

            ExecSQL(conn_db, "drop table t_tarif_dom", false);

            sql = "Create temp table t_tarif_dom " +
                            "( nzp_dom     integer, " +
                              "nzp_serv    integer, " +
                              "nzp_supp    integer, " +
                              "nzp_fd      integer, " +
                              "nzp_frm     integer, " +
                              "cnt_ls      integer, " +
                              "ordering    integer, " +
                              "service     CHAR(100), " +
                              "name_supp   CHAR(100), " +
                              "name_frm    CHAR(60), " +
                              "tarif       " + sDecimalType + "(14,2) default 0.00 " +
                            ")" + DBManager.sUnlogTempTable;
            ExecSQL(conn_db, sql, false);

            sql = 
                " INSERT INTO t_tarif_dom (cnt_ls, nzp_serv, service, ordering, nzp_supp, nzp_fd, name_supp, nzp_frm, name_frm) " +
                " SELECT count(distinct t.nzp_kvar) as cnt_ls, " +
                " t.nzp_serv, s.service, min(s.ordering), " +
                " t.nzp_supp, fdbl.nzp_fd, sp.name_supp, " +
                " t.nzp_frm, f.name_frm  " +
                " FROM " + finder.pref + sDataAliasRest + "tarif t, " +
                Points.Pref + sDataAliasRest + "kvar k, " + 
                finder.pref + sKernelAliasRest + "services s, " +
                Points.Pref + sKernelAliasRest + "supplier sp, " +
                finder.pref + sKernelAliasRest + "formuls f, " +
                Points.Pref + sDataAliasRest + "fn_dogovor_bank_lnk fdbl " + 
                " WHERE k.nzp_dom = " + finder.nzp_dom +
                " and t.nzp_kvar=k.nzp_kvar and s.nzp_serv = t.nzp_serv and fdbl.id = sp.fn_dogovor_bank_lnk_id " +
                " and sp.nzp_supp = t.nzp_supp and f.nzp_frm = t.nzp_frm " +
                " and t.is_actual <> 100 " + where +
                " and t.dat_s < '" + dat.AddMonths(1).ToShortDateString() + "' " +
                " and t.dat_po>='" + dat.ToShortDateString() + "' " +
                " and is_open='1' " +
                " group by t.nzp_serv, s.service, t.nzp_frm, t.nzp_supp, fdbl.nzp_fd, sp.name_supp, f.name_frm";

            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }

            sql = "update t_tarif_dom set tarif = ( " +
                            "select max(p.val_prm) " +
                            "from " + finder.pref + sDataAliasRest + "prm_5 p," + 
                            finder.pref + sKernelAliasRest + "formuls_opis f " +
                            "where f.nzp_frm = t_tarif_dom.nzp_frm and p.nzp_prm = f.nzp_prm_tarif_bd and p.is_actual <> 100  " +
                             "and p.dat_s < '" + dat.AddMonths(1).ToShortDateString() + "' " +
                             "and p.dat_po>='" + dat.ToShortDateString() + "' " +
                            ") " + sConvToNum + 
                        " where 0<( " +
                            " select count(*) from " + finder.pref + sDataAliasRest + "prm_5 p," +
                            finder.pref + sKernelAliasRest + "formuls_opis f " +
                            " where f.nzp_frm=t_tarif_dom.nzp_frm and p.nzp_prm=f.nzp_prm_tarif_bd and p.is_actual <> 100 " +
                             " and p.dat_s < '" + dat.AddMonths(1).ToShortDateString() + "' " +
                             " and p.dat_po>='" + dat.ToShortDateString() + "' " +
                            ") ";

            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }

            IDataReader reader;
            sql = " SELECT nzp_serv, nzp_supp, nzp_fd, nzp_frm, cnt_ls, service, name_supp, name_frm, tarif " +
                  " FROM t_tarif_dom " +
                  " ORDER BY ordering, name_supp, name_frm";
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }

            List<Service> list = new List<Service>();

            try
            {
                while (reader.Read())
                {
                    Service zap = new Service();

                    if (reader["nzp_serv"] != DBNull.Value) zap.nzp_serv = (int)reader["nzp_serv"];
                    if (reader["nzp_supp"] != DBNull.Value) zap.nzp_supp = Convert.ToInt64(reader["nzp_supp"]);
                    if (reader["nzp_fd"] != DBNull.Value) zap.nzp_fd = Convert.ToInt32(reader["nzp_fd"]);
                    if (reader["nzp_frm"] != DBNull.Value) zap.nzp_frm = (int)reader["nzp_frm"];
                    if (reader["cnt_ls"] != DBNull.Value) zap.cnt_ls = (int)reader["cnt_ls"];
                    if (reader["service"] != DBNull.Value) zap.service = ((string)reader["service"]).Trim();
                    if (reader["name_supp"] != DBNull.Value) zap.name_supp = ((string)reader["name_supp"]).Trim();
                    if (reader["name_frm"] != DBNull.Value) zap.name_frm = ((string)reader["name_frm"]).Trim();
                    if (reader["tarif"] != DBNull.Value) zap.tarif = Convert.ToDecimal(reader["tarif"]);
                    if (zap.tarif == 0) zap.tarif_s = "-";
                    else zap.tarif_s = zap.tarif.ToString("0.00");
                    list.Add(zap);
                }
                if (reader != null) reader.Close();
                ExecSQL(conn_db, "drop table t_tarif_dom", false);
            }
            catch
            {
                if (reader != null) reader.Close();
                conn_db.Close();
                return null;
            }
            conn_db.Close();
            return list;
        }

        public Returns FindLSDomFromDomService(Service finder)
        {
            Returns ret = Utils.InitReturns();
            if (finder.nzp_user < 1) return new Returns(false, "Не определен пользователь");
            if (finder.pref == "") return new Returns(false, "Префикс не определен");
            if (finder.nzp_serv < 0) return new Returns(false, "Услуга не определена");
            if (finder.nzp_supp < 0) return new Returns(false, "Поставщик не определен");
            if (finder.nzp_frm < 0) return new Returns(false, "Формула не определена");
            if (finder.nzp_dom < 0) return new Returns(false, "Дом не определен");
            string conn_kernel = Points.GetConnByPref(finder.pref);
            if (conn_kernel == "") return new Returns(false, "Не определен connect к БД");
            if (finder.year_ < 0) return new Returns(false, "Не задан год", -1);
            if (finder.month_ < 0) return new Returns(false, "Не задан месяц", -1);

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return ret;

            string tXX_spls = "t" + Convert.ToString(finder.nzp_user) + "_splsdom";

            //создать кэш-таблицу
            using (var db = new DbAdresClient())
            {
                ret = db.CreateTableWebLs(conn_web, tXX_spls, true);
            }
            if (!ret.result)
            {
                conn_web.Close();
                return ret;
            }

            IDbConnection conn_db = GetConnection(conn_kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                conn_web.Close();
                return ret;
            }

            //заполнить webdata:tXX_spls
#if PG
            string tXX_spls_full = "public." + tXX_spls;
#else
            string tXX_spls_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXX_spls;
#endif
            DateTime dat = new DateTime(finder.year_, finder.month_, 1);

            StringBuilder sql = new StringBuilder();
            string dop = "";

#if PG
            sql.Append(" Insert into " + tXX_spls_full + " (nzp_kvar,num_ls,pkod10,pkod,typek,pref,nzp_dom,nzp_area,nzp_geu,fio, ikvar,idom, ndom,nkvar,nzp_ul,ulica, adr,sostls,stypek, mark) ");
            sql.Append(" Select distinct k.nzp_kvar,k.num_ls,k.pkod10,k.pkod,k.typek,'" + finder.pref + "',k.nzp_dom,k.nzp_area,k.nzp_geu,k.fio, ikvar,idom, ");
            sql.Append("   trim(coalesce(d.ndom,''))||' '||trim(coalesce(d.nkor,'')) as ndom, " +
                       "   trim(coalesce(k.nkvar,''))||' '||trim(coalesce(k.nkvar_n,'')) as nkvar, " +
                       "   d.nzp_ul, trim(u.ulica)||' / '||trim(coalesce(r.rajon,'')) as ulica, ");
            sql.Append("   trim(coalesce(ulicareg,'улица'))||' '||trim(coalesce(u.ulica,''))||' / '||trim(coalesce(r.rajon,''))||'   дом '||" +
                       "   trim(coalesce(ndom,''))||'  корп. '|| trim(coalesce(nkor,''))||'  кв. '||trim(coalesce(nkvar,''))||'  ком. '||trim(coalesce(nkvar_n,'')) as adr, ");
            sql.Append("   ry.name_y as sostls, t.name_y stypek, 1 ");
            sql.Append(" From " + finder.pref + "_data.tarif tf ");
            sql.AppendFormat(" left outer join {0}_data.kvar    k on tf.nzp_kvar=k.nzp_kvar", finder.pref);
            sql.AppendFormat(" left outer join {0}_kernel.res_y t on k.typek=t.nzp_y", Points.Pref);
            sql.AppendFormat(" left outer join {0}_data.dom     d on k.nzp_dom=d.nzp_dom", Points.Pref);
            sql.AppendFormat(" left outer join {0}_data.s_ulica u on d.nzp_ul=u.nzp_ul", Points.Pref);
            sql.AppendFormat(" left outer join {0}_data.s_rajon r on u.nzp_raj=r.nzp_raj", finder.pref);
            if (finder.stateID > 0)
            {
                sql.AppendFormat(" left outer join {0}_data.prm_3   p on k.nzp_kvar=p.nzp", finder.pref);
                sql.AppendFormat(" left outer join {0}_kernel.res_y ry on trim(p.val_prm)::integer=ry.nzp_y", finder.pref);
                dop = " and ry.nzp_y = " + finder.stateID + " ";
            }
            else
            {
                sql.AppendFormat(" left outer join {0}_data.prm_3   p on k.nzp_kvar=p.nzp", finder.pref);
                sql.AppendFormat(" left outer join {0}_kernel.res_y ry on trim(p.val_prm)::integer=ry.nzp_y", finder.pref);
            }
            sql.AppendFormat(" Where k.nzp_dom = {0}", finder.nzp_dom)
                .Append(dop)
                .Append(" and tf.nzp_serv = " + finder.nzp_serv)
                .Append(" and tf.nzp_supp = " + finder.nzp_supp)
                .Append(" and tf.nzp_frm = " + finder.nzp_frm)
                .Append(" and tf.is_actual <> 100")
                .AppendFormat(" and tf.dat_s < '{0}'", dat.AddMonths(1).ToShortDateString())
                .AppendFormat(" and tf.dat_po>='{0}'", dat.ToShortDateString());
            sql.Append(" and p.nzp_prm=51");
            sql.Append(" and ry.nzp_res=18");
            sql.Append(" and p.dat_s<=current_date ");
            sql.Append(" and p.dat_po>=current_date ");
            sql.Append(" and p.is_actual <> 100");
            sql.Append(" and t.nzp_res=9999 ");
#else
            string field_num_ls_litera = "", value_num_ls_litera = "";
            if (Points.IsSmr)
            {
                string tprm1 = finder.pref + "_data@" + DBManager.getServer(conn_db) + ":prm_1";

                field_num_ls_litera = " , num_ls_litera ";
                value_num_ls_litera = " , (select case when year(dat_s)-2000 = 0 then k.pkod10 || '' else k.pkod10 || ' ' || year(dat_s)-2000 end from " +
                    tprm1 + " where nzp_prm=2004 and nzp = k.nzp_kvar) ";
            }

            sql.Append(" Insert into " + tXX_spls_full + " (nzp_kvar,num_ls" + field_num_ls_litera + ",pkod10,pkod,typek,pref,nzp_dom,nzp_area,nzp_geu,fio, ikvar,idom, ndom,nkvar,nzp_ul,ulica, adr,sostls,stypek, mark) ");
            sql.Append(" Select unique k.nzp_kvar,k.num_ls" + value_num_ls_litera + ",k.pkod10,k.pkod,k.typek,'" + finder.pref + "',k.nzp_dom,k.nzp_area,k.nzp_geu,k.fio, ikvar,idom, ");
            sql.Append("   trim(nvl(d.ndom,''))||' '||trim(nvl(d.nkor,'')) as ndom, " +
                       "   trim(nvl(k.nkvar,''))||' '||trim(nvl(k.nkvar_n,'')) as nkvar, " +
                       "   d.nzp_ul, trim(u.ulica)||' / '||trim(nvl(r.rajon,'')) as ulica, ");
            sql.Append("   trim(nvl(ulicareg,'улица'))||' '||trim(nvl(u.ulica,''))||' / '||trim(nvl(r.rajon,''))||'   дом '||" +
                       "   trim(nvl(ndom,''))||'  корп. '|| trim(nvl(nkor,''))||'  кв. '||trim(nvl(nkvar,''))||'  ком. '||trim(nvl(nkvar_n,'')) as adr, ");
            sql.Append("   ry.name_y as sostls, t.name_y stypek, 1 ");

            sql.Append(" From " + finder.pref + "_data:tarif tf, " + finder.pref + "_data:kvar k, ");
            sql.Append(Points.Pref + "_kernel:res_y t, ");
            sql.Append(Points.Pref + "_data:dom d, ");
            sql.Append(Points.Pref + "_data:s_ulica u, ");
            sql.Append(" outer  " + finder.pref + "_data:s_rajon r, ");
            if (finder.stateID > 0)
            {
                sql.Append(finder.pref + "_data:prm_3 p, "
                                      + finder.pref + "_kernel:res_y ry ");
                dop = " and ry.nzp_y = " + finder.stateID + " ";
            }
            else
                sql.Append(" outer (" + finder.pref + "_data:prm_3 p, "
                                      + finder.pref + "_kernel:res_y ry) ");

            sql.Append(" Where " +
                            " k.nzp_dom = " + finder.nzp_dom +
                            " and tf.nzp_kvar=k.nzp_kvar " +
                            " and tf.nzp_serv = " + finder.nzp_serv +
                            " and tf.nzp_supp = " + finder.nzp_supp +
                            " and tf.nzp_frm = " + finder.nzp_frm +
                            " and tf.is_actual <> 100 " +
                            " and tf.dat_s < '" + dat.AddMonths(1).ToShortDateString() + "' " +
                            " and tf.dat_po>='" + dat.ToShortDateString() + "' and " +
                            " k.nzp_dom=d.nzp_dom and d.nzp_ul=u.nzp_ul and u.nzp_raj=r.nzp_raj  ");
            sql.Append("   and k.nzp_kvar=p.nzp and p.nzp_prm=51 and trim(p.val_prm)=ry.nzp_y " + dop +
                       "   and ry.nzp_res=18 and p.dat_s<=today and p.dat_po>=today and p.is_actual <> 100");
            sql.Append("   and k.typek=t.nzp_y and t.nzp_res=9999 ");
#endif

            ret = ExecSQL(conn_db, sql.ToString(), true);
            if (!ret.result)
            {
                conn_db.Close();
                conn_web.Close();
                return ret;
            }

            sql.Length = 0;
#if PG
            sql.Append("Update " + tXX_spls_full +
                " Set has_pu = 1" +
                " Where pref = '" + finder.pref + "'" +
                    " and nzp_kvar in (select nzp from " + finder.pref + "_data.counters_spis" +
                                     " where nzp_type = " + (int)CounterKinds.Kvar + " and is_actual <> 100)");
#else
            sql.Append("Update " + tXX_spls_full +
                " Set has_pu = 1" +
                " Where pref = '" + finder.pref + "'" +
                    " and nzp_kvar in (select nzp from " + finder.pref + "_data:counters_spis" +
                                     " where nzp_type = " + (int)CounterKinds.Kvar + " and is_actual <> 100)");
#endif
            ret = ExecSQL(conn_db, sql.ToString(), true);
            if (!ret.result)
            {
                conn_db.Close();
                conn_web.Close();
                return ret;
            }

            sql = new StringBuilder();
            sql.Append("Update " + tXX_spls_full +
                " Set has_pu = 1" +
                " Where pref = '" + finder.pref + "'" +
#if PG
 " and exists (select 1 from " + finder.pref + "_data.counters_spis cs, " + finder.pref + "_data.counters_link cl" +
#else
 " and exists (select 1 from " + finder.pref + "_data:counters_spis cs, " + finder.pref + "_data:counters_link cl" +
#endif
 " where cs.nzp_type in (" + (int)CounterKinds.Group + "," + (int)CounterKinds.Communal + ")" +
                                         " and cs.nzp_counter = cl.nzp_counter" +
                                         " and cs.is_actual <> 100" +
                                         " and " + tXX_spls_full + ".nzp_kvar = cl.nzp_kvar)");
            ret = ExecSQL(conn_db, sql.ToString(), true);
            if (!ret.result)
            {
                conn_db.Close();
                conn_web.Close();
                return ret;
            }

            conn_db.Close(); //закрыть соединение с основной базой


            //создаем индексы на tXX_spls
            using (var db = new DbAdresClient())
            {
                ret = db.CreateTableWebLs(conn_web, tXX_spls, false);
            }

            conn_web.Close();
            return ret;
        }


        public List<Service> GetGroupServ(Service finder, out Returns ret)
        {
            throw new NotImplementedException();
        }

        /// <summary> Возвращает список доступных услуг с указанием поставщика и формулы расчета, действующих в заданном расчетном месяце
        /// </summary>
        /// <param name="finder">Обязательные параметры: nzp_user, month_, year_</param>
        public List<Service> FindAvailableServices(Service finder, out Returns ret)
        {
            #region Проверка входных параметров
            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Не определен пользователь");
                return null;
            }
            if (Utils.GetParams(finder.prms, Constants.page_available_service))
            {
                if (finder.nzp_serv < 1)
                {
                    ret = new Returns(false, "Не задана услуга");
                    return null;
                }
            }
            else
            {
                if (finder.month_ < 1)
                {
                    ret = new Returns(false, "Не задан расчетный месяц");
                    return null;
                }
                if (finder.year_ < 1)
                {
                    ret = new Returns(false, "Не задан расчетный год");
                    return null;
                }
            }
            #endregion

            if (finder.pref.Trim() == "") finder.pref = Points.Pref;

            string connectionString = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            IDataReader reader;
            string sql = "", where = "";

            if (finder.RolesVal != null)
                foreach (_RolesVal role in finder.RolesVal)
                {
                    if (role.tip == Constants.role_sql & role.kod == Constants.role_sql_serv) where += " and lf.nzp_serv in (" + role.val + ")";
                    if (role.tip == Constants.role_sql & role.kod == Constants.role_sql_supp) where += " and lf.nzp_supp in (" + role.val + ")";
                }

            if (finder.nzp_supp > 0) where += " and lf.nzp_supp = " + finder.nzp_supp;
            if (finder.nzp_frm > 0) where += " and lf.nzp_frm = " + finder.nzp_frm;

            if (Utils.GetParams(finder.prms, Constants.page_available_service) || Utils.GetParams(finder.prms, Constants.page_new_available_service))
            {
#if PG
                var sqlBuilder = new StringBuilder();
                sqlBuilder.Append("Select lf.nzp_foss, s.nzp_serv, s.service, lf.nzp_supp, sp.name_supp, lf.nzp_frm, f.name_frm, lf.dat_s, lf.dat_po");
                sqlBuilder.AppendFormat(" from {0}_kernel.l_foss lf", finder.pref);
                sqlBuilder.AppendFormat(" left outer join {0}_kernel.services s on lf.nzp_serv = s.nzp_serv", finder.pref);
                sqlBuilder.AppendFormat(" left outer join {0}_kernel.supplier sp on sp.nzp_supp = lf.nzp_supp", finder.pref);
                sqlBuilder.AppendFormat(" left outer join {0}_kernel.formuls f on lf.nzp_frm = f.nzp_frm", finder.pref);
                sqlBuilder.AppendFormat(" where lf.nzp_serv = {0}", finder.nzp_serv)
                    .Append(where)
                    .Append(" Order by lf.dat_s, lf.dat_po, sp.name_supp, f.name_frm");

                sql = sqlBuilder.ToString();
#else
                sql = "Select lf.nzp_foss, s.nzp_serv, s.service, lf.nzp_supp, sp.name_supp, lf.nzp_frm, f.name_frm, lf.dat_s, lf.dat_po" +
                    " From " + finder.pref + "_kernel:l_foss lf, outer " + finder.pref + "_kernel:services s, outer " + finder.pref + "_kernel:supplier sp, outer " + finder.pref + "_kernel:formuls f" +
                    " Where lf.nzp_serv = " + finder.nzp_serv + where + " and lf.nzp_serv = s.nzp_serv and sp.nzp_supp = lf.nzp_supp and lf.nzp_frm = f.nzp_frm " +
                    " Order by lf.dat_s, lf.dat_po, sp.name_supp, f.name_frm";
#endif
            }
            else
            {
                if (finder.nzp_serv > 0) where += " and lf.nzp_serv = " + finder.nzp_serv;

                sql = "Select 0 as nzp_foss, s.nzp_serv, s.service, lf.nzp_supp, sp.name_supp, lf.nzp_frm, f.name_frm";
                if (finder.is_actual == 1)
                {
#if PG
                    var sqlBuilder = new StringBuilder();
                    sqlBuilder.AppendFormat(" from {0}_kernel.services s", finder.pref);
                    sqlBuilder.AppendFormat(" left outer join {0}_kernel.l_foss lf on lf.nzp_serv = s.nzp_serv", finder.pref);
                    sqlBuilder.AppendFormat(" left outer join {0}_kernel.supplier sp on sp.nzp_supp = lf.nzp_supp", finder.pref);
                    sqlBuilder.AppendFormat(" left outer join {0}_kernel.formuls f on lf.nzp_frm = f.nzp_frm", finder.pref);

                    sql += sqlBuilder.ToString();
#else
                    sql += " From " + finder.pref + "_kernel:services s, " + finder.pref + "_kernel:l_foss lf, outer " + finder.pref + "_kernel:supplier sp, outer " + finder.pref + "_kernel:formuls f";
#endif
                }
                else
                {
#if PG
                    var sqlBuilder = new StringBuilder();
                    sqlBuilder.AppendFormat(" from {0}_kernel.services s", finder.pref);
                    sqlBuilder.AppendFormat(" left outer join  {0}_kernel.l_foss lf on lf.nzp_serv = s.nzp_serv", finder.pref);
                    sqlBuilder.AppendFormat(" and mdy({0},1,{1}) between lf.dat_s and lf.dat_po", finder.month_, finder.year_);

                    sqlBuilder.AppendFormat(" left outer join {0}_kernel.supplier sp on sp.nzp_supp = lf.nzp_supp", finder.pref);
                    sqlBuilder.AppendFormat(" left outer join {0}_kernel.formuls f on lf.nzp_frm = f.nzp_frm ", finder.pref);

                    sql += sqlBuilder.ToString();
#else
                    sql += " From " + finder.pref + "_kernel:services s, outer (" + finder.pref + "_kernel:l_foss lf, outer " + finder.pref + "_kernel:supplier sp, outer " + finder.pref + "_kernel:formuls f)";
#endif
                }

#if PG
                var whereBuider = new StringBuilder();


                whereBuider.Append(" Where s.nzp_serv > 1");
#if PG
                if (finder.is_actual == 1)
                {
                    whereBuider.AppendFormat(" and mdy({0},1,{1}) between lf.dat_s and lf.dat_po", finder.month_,
                        finder.year_);
                }
#else
                whereBuider.AppendFormat(" and mdy({0},1,{1}) between lf.dat_s and lf.dat_po", finder.month_, finder.year_);
#endif
                whereBuider.Append(where);
                whereBuider.Append(" Group by s.nzp_serv, s.service, lf.nzp_supp, sp.name_supp, lf.nzp_frm, f.name_frm");
                whereBuider.Append(" Order by s.service, sp.name_supp, f.name_frm");

                sql += whereBuider.ToString();

#else
                sql += " Where s.nzp_serv > 1 " + where + " and lf.nzp_serv = s.nzp_serv and sp.nzp_supp = lf.nzp_supp and lf.nzp_frm = f.nzp_frm " +
                    " and mdy(" + finder.month_ + ",1," + finder.year_ + ") between lf.dat_s and lf.dat_po" +
                    " Group by s.nzp_serv, s.service, lf.nzp_supp, sp.name_supp, lf.nzp_frm, f.name_frm" +
                    " Order by s.service, sp.name_supp, f.name_frm";
#endif
            }

            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }

            List<Service> list = new List<Service>();

            int i = 0;

            while (reader.Read())
            {
                i++;
                if (finder.skip > 0 && i <= finder.skip) continue;
                if (i > finder.skip + finder.rows) continue;

                Service zap = new Service();

                if (reader["nzp_foss"] != DBNull.Value) zap.nzp_foss = Convert.ToInt32(reader["nzp_foss"]);
                if (reader["nzp_serv"] != DBNull.Value) zap.nzp_serv = Convert.ToInt32(reader["nzp_serv"]);
                if (reader["service"] != DBNull.Value) zap.service = Convert.ToString(reader["service"]).Trim();
                if (reader["nzp_supp"] != DBNull.Value) zap.nzp_supp = Convert.ToInt64(reader["nzp_supp"]);
                if (reader["name_supp"] != DBNull.Value) zap.name_supp = Convert.ToString(reader["name_supp"]).Trim();
                if (reader["nzp_frm"] != DBNull.Value) zap.nzp_frm = Convert.ToInt32(reader["nzp_frm"]);
                if (reader["name_frm"] != DBNull.Value) zap.name_frm = Convert.ToString(reader["name_frm"]).Trim();
                if (reader["nzp_supp"] == DBNull.Value) zap.activePeriod = -1;

                if (Utils.GetParams(finder.prms, Constants.page_available_service))
                {
                    if (reader["dat_s"] != DBNull.Value)
                    {
                        zap.dat_s = String.Format("{0:dd.MM.yyyy}", reader["dat_s"]);
                        DateTime ds;
                        bool res = DateTime.TryParseExact(zap.dat_s, "dd.MM.yyyy", new CultureInfo("ru-RU"), DateTimeStyles.None, out ds);
                        if (res)
                            if (ds <= new DateTime(1900, 1, 1)) zap.dat_s = "";
                    }
                    if (reader["dat_po"] != DBNull.Value)
                    {
                        zap.dat_po = String.Format("{0:dd.MM.yyyy}", reader["dat_po"]);
                        DateTime dpo;
                        bool res = DateTime.TryParseExact(zap.dat_po, "dd.MM.yyyy", new CultureInfo("ru-RU"), DateTimeStyles.None, out dpo);
                        if (res)
                            if (dpo > new DateTime(2900, 1, 1)) zap.dat_po = "";
                    }
                }

                list.Add(zap);
            }

            ret.tag = i;

            if (reader != null) reader.Close();
            conn_db.Close();
            return list;
        }



        /// <summary> Возвращает список доступных услуг с указанием поставщика и формулы расчета, действующих в заданном расчетном месяце
        /// </summary>
        /// <param name="finder">Обязательные параметры: nzp_user, month_, year_</param>
        public List<Service> FindAvailableServNewDog(Service finder, out Returns ret)
        {
            #region Проверка входных параметров
            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Не определен пользователь");
                return null;
            }
            if (Utils.GetParams(finder.prms, Constants.page_new_available_service))
            {
                if (finder.nzp_serv < 1)
                {
                    ret = new Returns(false, "Не задана услуга");
                    return null;
                }
            }
            else
            {
                if (finder.month_ < 1)
                {
                    ret = new Returns(false, "Не задан расчетный месяц");
                    return null;
                }
                if (finder.year_ < 1)
                {
                    ret = new Returns(false, "Не задан расчетный год");
                    return null;
                }
            }
            #endregion

            if (finder.pref.Trim() == "") finder.pref = Points.Pref;

            string connectionString = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            IDataReader reader;
            string sql = "", where = "";

            if (finder.RolesVal != null)
                foreach (_RolesVal role in finder.RolesVal)
                {
                    if (role.tip == Constants.role_sql & role.kod == Constants.role_sql_serv) where += " and lf.nzp_serv in (" + role.val + ")";
                    if (role.tip == Constants.role_sql & role.kod == Constants.role_sql_supp) where += " and lf.nzp_supp in (" + role.val + ")";
                }

            if (finder.nzp_supp > 0) where += " and lf.nzp_supp = " + finder.nzp_supp;
            if (finder.nzp_frm > 0) where += " and lf.nzp_frm = " + finder.nzp_frm;

            if ( Utils.GetParams(finder.prms, Constants.page_new_available_service))
            {
#if PG
                var sqlBuilder = new StringBuilder();
                sqlBuilder.Append("Select lf.nzp_foss, s.nzp_serv, s.service, lf.nzp_supp, sp.name_supp, lf.nzp_frm, f.name_frm, lf.dat_s, lf.dat_po");
                sqlBuilder.AppendFormat(" from {0}_kernel.l_foss lf", finder.pref);
                sqlBuilder.AppendFormat(" left outer join {0}_kernel.services s on lf.nzp_serv = s.nzp_serv", finder.pref);
                sqlBuilder.AppendFormat(" left outer join {0}_kernel.supplier sp on sp.nzp_supp = lf.nzp_supp", finder.pref);
                sqlBuilder.AppendFormat((finder.nzp_fd > 0 ? " inner join " + Points.Pref + sDataAliasRest + "fn_dogovor_bank_lnk fdbl" +
                                                             " on sp.fn_dogovor_bank_lnk_id = fdbl.id and fdbl.nzp_fd = " + finder.nzp_fd : ""));
                sqlBuilder.AppendFormat(" left outer join {0}_kernel.formuls f on lf.nzp_frm = f.nzp_frm", finder.pref);
                sqlBuilder.AppendFormat(" where lf.nzp_serv = {0}", finder.nzp_serv)
                    .Append(where)
                    .Append(" Order by lf.dat_s, lf.dat_po, sp.name_supp, f.name_frm");

                sql = sqlBuilder.ToString();
#else
                sql = "Select lf.nzp_foss, s.nzp_serv, s.service, lf.nzp_supp, sp.name_supp, lf.nzp_frm, f.name_frm, lf.dat_s, lf.dat_po" +
                    " From " + finder.pref + "_kernel:l_foss lf, outer " + finder.pref + "_kernel:services s, outer " + finder.pref + "_kernel:supplier sp, outer " + finder.pref + "_kernel:formuls f" +
                    " Where lf.nzp_serv = " + finder.nzp_serv + where + " and lf.nzp_serv = s.nzp_serv and sp.nzp_supp = lf.nzp_supp and lf.nzp_frm = f.nzp_frm " +
                    " Order by lf.dat_s, lf.dat_po, sp.name_supp, f.name_frm";
#endif
            }
            else
            {
                if (finder.nzp_serv > 0) where += " and lf.nzp_serv = " + finder.nzp_serv;

                sql = "Select 0 as nzp_foss, s.nzp_serv, s.service, lf.nzp_supp, sp.name_supp, lf.nzp_frm, f.name_frm";
                if (finder.is_actual == 1)
                {
#if PG
                    var sqlBuilder = new StringBuilder();
                    sqlBuilder.AppendFormat(" from {0}_kernel.services s", finder.pref);
                    sqlBuilder.AppendFormat(" left outer join {0}_kernel.l_foss lf on lf.nzp_serv = s.nzp_serv", finder.pref);
                    sqlBuilder.AppendFormat(" left outer join {0}_kernel.supplier sp on sp.nzp_supp = lf.nzp_supp", finder.pref);
                    sqlBuilder.AppendFormat((finder.nzp_fd > 0 ? " inner join " + Points.Pref + sDataAliasRest + "fn_dogovor_bank_lnk fdbl" +
                                                                 " on sp.fn_dogovor_bank_lnk_id = fdbl.id and fdbl.nzp_fd = " + finder.nzp_fd : ""));
                    sqlBuilder.AppendFormat(" left outer join {0}_kernel.formuls f on lf.nzp_frm = f.nzp_frm", finder.pref);

                    sql += sqlBuilder.ToString();
#else
                    sql += " From " + finder.pref + "_kernel:services s, " + finder.pref + "_kernel:l_foss lf, outer " + finder.pref + "_kernel:supplier sp, outer " + finder.pref + "_kernel:formuls f";
#endif
                }
                else
                {
#if PG
                    var sqlBuilder = new StringBuilder();
                    sqlBuilder.AppendFormat(" from {0}_kernel.services s", finder.pref);
                    sqlBuilder.AppendFormat(" left outer join  {0}_kernel.l_foss lf on lf.nzp_serv = s.nzp_serv", finder.pref);
                    sqlBuilder.AppendFormat(" and mdy({0},1,{1}) between lf.dat_s and lf.dat_po", finder.month_, finder.year_);

                    sqlBuilder.AppendFormat(" left outer join {0}_kernel.supplier sp on sp.nzp_supp = lf.nzp_supp", finder.pref);
                    sqlBuilder.AppendFormat((finder.nzp_fd > 0 ? " inner join " + Points.Pref + sDataAliasRest + "fn_dogovor_bank_lnk fdbl" +
                                                                 " on sp.fn_dogovor_bank_lnk_id = fdbl.id and fdbl.nzp_fd = " + finder.nzp_fd : ""));
                    sqlBuilder.AppendFormat(" left outer join {0}_kernel.formuls f on lf.nzp_frm = f.nzp_frm ", finder.pref);

                    sql += sqlBuilder.ToString();
#else
                    sql += " From " + finder.pref + "_kernel:services s, outer (" + finder.pref + "_kernel:l_foss lf, outer " + finder.pref + "_kernel:supplier sp, outer " + finder.pref + "_kernel:formuls f)";
#endif
                }

#if PG
                var whereBuider = new StringBuilder();


                whereBuider.Append(" Where s.nzp_serv > 1");
#if PG
                if (finder.is_actual == 1)
                {
                    whereBuider.AppendFormat(" and mdy({0},1,{1}) between lf.dat_s and lf.dat_po", finder.month_,
                        finder.year_);
                }
#else
                whereBuider.AppendFormat(" and mdy({0},1,{1}) between lf.dat_s and lf.dat_po", finder.month_, finder.year_);
#endif
                whereBuider.Append(where);
                whereBuider.Append(" Group by s.nzp_serv, s.service, lf.nzp_supp, sp.name_supp, lf.nzp_frm, f.name_frm");
                whereBuider.Append(" Order by s.service, sp.name_supp, f.name_frm");

                sql += whereBuider.ToString();

#else
                sql += " Where s.nzp_serv > 1 " + where + " and lf.nzp_serv = s.nzp_serv and sp.nzp_supp = lf.nzp_supp and lf.nzp_frm = f.nzp_frm " +
                    " and mdy(" + finder.month_ + ",1," + finder.year_ + ") between lf.dat_s and lf.dat_po" +
                    " Group by s.nzp_serv, s.service, lf.nzp_supp, sp.name_supp, lf.nzp_frm, f.name_frm" +
                    " Order by s.service, sp.name_supp, f.name_frm";
#endif
            }

            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }

            List<Service> list = new List<Service>();

            int i = 0;

            while (reader.Read())
            {
                i++;
                if (finder.skip > 0 && i <= finder.skip) continue;
                if (i > finder.skip + finder.rows) continue;

                Service zap = new Service();

                if (reader["nzp_foss"] != DBNull.Value) zap.nzp_foss = Convert.ToInt32(reader["nzp_foss"]);
                if (reader["nzp_serv"] != DBNull.Value) zap.nzp_serv = Convert.ToInt32(reader["nzp_serv"]);
                if (reader["service"] != DBNull.Value) zap.service = Convert.ToString(reader["service"]).Trim();
                if (reader["nzp_supp"] != DBNull.Value) zap.nzp_supp = Convert.ToInt64(reader["nzp_supp"]);
                if (reader["name_supp"] != DBNull.Value) zap.name_supp = Convert.ToString(reader["name_supp"]).Trim();
                if (reader["nzp_frm"] != DBNull.Value) zap.nzp_frm = Convert.ToInt32(reader["nzp_frm"]);
                if (reader["name_frm"] != DBNull.Value) zap.name_frm = Convert.ToString(reader["name_frm"]).Trim();
                if (reader["nzp_supp"] == DBNull.Value) zap.activePeriod = -1;

                if (Utils.GetParams(finder.prms, Constants.page_new_available_service))
                {
                    if (reader["dat_s"] != DBNull.Value)
                    {
                        zap.dat_s = String.Format("{0:dd.MM.yyyy}", reader["dat_s"]);
                        DateTime ds;
                        bool res = DateTime.TryParseExact(zap.dat_s, "dd.MM.yyyy", new CultureInfo("ru-RU"), DateTimeStyles.None, out ds);
                        if (res)
                            if (ds <= new DateTime(1900, 1, 1)) zap.dat_s = "";
                    }
                    if (reader["dat_po"] != DBNull.Value)
                    {
                        zap.dat_po = String.Format("{0:dd.MM.yyyy}", reader["dat_po"]);
                        DateTime dpo;
                        bool res = DateTime.TryParseExact(zap.dat_po, "dd.MM.yyyy", new CultureInfo("ru-RU"), DateTimeStyles.None, out dpo);
                        if (res)
                            if (dpo > new DateTime(2900, 1, 1)) zap.dat_po = "";
                    }
                }

                list.Add(zap);
            }

            ret.tag = i;

            if (reader != null) reader.Close();
            conn_db.Close();
            return list;
        }



        public Returns AddServiceIntoServpriority(ServPriority finder)
        {
            #region Проверка входных параметров
            if (finder.nzp_user < 1) return new Returns(false, "Пользователь не определен");
            if (finder.webLogin == "") return new Returns(false, "Пользователь не определен");
            if (finder.webUname == "") return new Returns(false, "Пользователь не определен");
            if (finder.nzp_serv < 1) return new Returns(false, "Не задана услуга");
            if (finder.dat_s == "") return new Returns(false, "Не задана дата");
            #endregion

            #region соединение
            string connectionString = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(connectionString);
            Returns ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;
            #endregion

            #region проверка на существование уже открытой услуги
            bool res = IsOpenServExist(finder, conn_db, out ret);
            if (ret.result)
            {
                if (res) return new Returns(false, "Такая услуга уже существует", -1);
            }
            else
            {
                conn_db.Close();
                return ret;
            }
            #endregion

            string sql_text = "";
            IDataReader reader = null;
            int order = 1;  //максимальный приоритет
            int nzpUser = 0; //локальный пользователь

            #region определение максимального order
#if PG
            sql_text = "select max(ordering) as max_order from " + Points.Pref + "_kernel.servpriority where dat_po >= '01.01.3000'";
#else
            sql_text = "select max(ordering) as max_order from " + Points.Pref + "_kernel:servpriority where dat_po >= '01.01.3000'";
#endif
            ret = ExecRead(conn_db, out reader, sql_text, true);
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }
            try
            {
                if (reader.Read()) if (reader["max_order"] != DBNull.Value) order = Convert.ToInt32(reader["max_order"]) + 1;
                reader.Close();
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка AddServiceIntoServpriority " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                conn_db.Close();
                return ret;
            }
            #endregion

            #region определение локального пользователя
            nzpUser = finder.nzp_user;
            
            /*DbWorkUser db = new DbWorkUser();
            nzpUser = db.GetLocalUser(conn_db, finder, out ret);
            db.Close();
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }*/
            #endregion

            #region добавление новой услуги в конец списка
            if (finder.dat_po == "") finder.dat_po = "01.01.3000";
#if PG
            sql_text = "insert into " + Points.Pref + "_kernel.servpriority (dat_s, dat_po, nzp_serv, ordering, nzp_user, dat_when) " +
                " values ('" + finder.dat_s + "','" + finder.dat_po + "'," + finder.nzp_serv + "," + order + "," + nzpUser + ",now())";
#else
            sql_text = "insert into " + Points.Pref + "_kernel:servpriority (dat_s, dat_po, nzp_serv, ordering, nzp_user, dat_when) " +
                " values ('" + finder.dat_s + "','" + finder.dat_po + "'," + finder.nzp_serv + "," + order + "," + nzpUser + ",current)";
#endif
            ret = ExecSQL(conn_db, sql_text, true);
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }
            #endregion

            conn_db.Close();
            return ret;
        }

        public Returns DeleteServiceFromServpriority(ServPriority finder)
        {
            #region Проверка входных параметров
            if (finder.nzp_user < 1) return new Returns(false, "Пользователь не определен");
            if (finder.webLogin == "") return new Returns(false, "Пользователь не определен");
            if (finder.webUname == "") return new Returns(false, "Пользователь не определен");
            if (finder.nzp_key < 1) return new Returns(false, "Не выбрана запись");
            if (finder.dat_s == "") return new Returns(false, "Не задана дата");
            #endregion

            #region соединение
            string connectionString = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(connectionString);
            Returns ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;
            #endregion

            string sql_text = "";
            IDataReader reader = null;
            int nzpUser = 0; //локальный пользователь
            int order = 0;//приоритет удаляемой услуги

            #region определение локального пользователя
            nzpUser = finder.nzp_user;
            
            /*DbWorkUser db = new DbWorkUser();
            nzpUser = db.GetLocalUser(conn_db, finder, out ret);
            db.Close();
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }*/
            #endregion

            #region определение приоритета удаляемой услуги
#if PG
            sql_text = "select ordering from " + Points.Pref + "_kernel.servpriority where nzp_key = " + finder.nzp_key;
#else
            sql_text = "select ordering from " + Points.Pref + "_kernel:servpriority where nzp_key = " + finder.nzp_key;
#endif
            ret = ExecRead(conn_db, out reader, sql_text, true);
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }
            try
            {
                if (reader.Read()) if (reader["ordering"] != DBNull.Value) order = Convert.ToInt32(reader["ordering"]);
                reader.Close();
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка AddServiceIntoServpriority " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                conn_db.Close();
                return ret;
            }
            #endregion

            #region Смена приоритета у следующих за удаляемой услугой
            string dat_po = "";
            DateTime dt;
            if (DateTime.TryParse(finder.dat_s, out dt)) dat_po = dt.AddDays(-1).ToShortDateString();
            if (dat_po == "")
            {
                ret.result = false;
                ret.text = "Не задан операционный день";
                conn_db.Close();
                return ret;
            }

#if PG
            sql_text = "select nzp_key from " + Points.Pref + "_kernel.servpriority where dat_po >= '01.01.3000' and ordering > " + order + " order by ordering";
#else
            sql_text = "select nzp_key from " + Points.Pref + "_kernel:servpriority where dat_po >= '01.01.3000' and ordering > " + order + " order by ordering";
#endif
            ret = ExecRead(conn_db, out reader, sql_text, true);
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }
            try
            {
                int prioritet = order;
                while (reader.Read())
                {
                    int nzp_key = 0;
                    if (reader["nzp_key"] != DBNull.Value) nzp_key = Convert.ToInt32(reader["nzp_key"]);

#if PG
                    sql_text = "update " + Points.Pref + "_kernel.servpriority set dat_po = '" + dat_po + "' where nzp_key = " + nzp_key;
#else
                    sql_text = "update " + Points.Pref + "_kernel:servpriority set dat_po = '" + dat_po + "' where nzp_key = " + nzp_key;
#endif
                    ret = ExecSQL(conn_db, sql_text, true);
                    if (!ret.result)
                    {
                        conn_db.Close();
                        return ret;
                    }

#if PG
                    sql_text = "insert into " + Points.Pref + "_kernel.servpriority (dat_s, dat_po, nzp_serv, ordering, nzp_user, dat_when)" +
                        " select '" + finder.dat_s + "', '01.01.3000', nzp_serv, " + prioritet + ", " + nzpUser + ", now() from " + Points.Pref + "_kernel.servpriority " +
                        " where nzp_key = " + nzp_key;
#else
                    sql_text = "insert into " + Points.Pref + "_kernel:servpriority (dat_s, dat_po, nzp_serv, ordering, nzp_user, dat_when)" +
                        " select '" + finder.dat_s + "', '01.01.3000', nzp_serv, " + prioritet + ", " + nzpUser + ", current from " + Points.Pref + "_kernel:servpriority " +
                        " where nzp_key = " + nzp_key;
#endif
                    ret = ExecSQL(conn_db, sql_text, true);
                    if (!ret.result)
                    {
                        conn_db.Close();
                        return ret;
                    }

                    prioritet++;
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка AddServiceIntoServpriority " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                conn_db.Close();
                return ret;
            }
            #endregion

            #region закрыть услугу
#if PG
            sql_text = "update " + Points.Pref + "_kernel.servpriority set dat_po = '" + dat_po + "' where nzp_key = " + finder.nzp_key;
#else
            sql_text = "update " + Points.Pref + "_kernel:servpriority set dat_po = '" + dat_po + "' where nzp_key = " + finder.nzp_key;
#endif
            ret = ExecSQL(conn_db, sql_text, true);
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }
            #endregion

            conn_db.Close();
            return ret;
        }

        private bool IsOpenServExist(ServPriority finder, IDbConnection conn_db, out Returns ret)
        {
            IDataReader reader = null;
            int cnt = 0;
#if PG
            string sql_text = "select count(*) as cnt from " + Points.Pref + "_kernel.servpriority " +
                " where nzp_serv = " + finder.nzp_serv + " and dat_po >= '01.01.3000'";
#else
            string sql_text = "select count(*) as cnt from " + Points.Pref + "_kernel:servpriority " +
                " where nzp_serv = " + finder.nzp_serv + " and dat_po >= '01.01.3000'";
#endif
            ret = ExecRead(conn_db, out reader, sql_text, true);
            if (!ret.result)
            {
                return false;
            }
            try
            {
                if (reader.Read()) if (reader["cnt"] != DBNull.Value) cnt = Convert.ToInt32(reader["cnt"]);
                reader.Close();
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка IsOpenServExist " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                conn_db.Close();
                return false;
            }
            if (cnt > 0) return true;
            else return false;
        }

        public Returns ChangeOrderIntoServpriority(ServPriority finder)
        {
            #region Проверка входных параметров
            if (finder.nzp_user < 1) return new Returns(false, "Пользователь не определен");
            if (finder.webLogin == "") return new Returns(false, "Пользователь не определен");
            if (finder.webUname == "") return new Returns(false, "Пользователь не определен");
            if (finder.nzp_serv < 1) return new Returns(false, "Не задана услуга");
            if (finder.dat_s == "") return new Returns(false, "Не задана дата");
            if (finder.nzp_key < 1) return new Returns(false, "Не указан код записи");
            if (finder.move <= 0) return new Returns(false, "Не указан move");
            #endregion

            #region соединение
            string connectionString = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(connectionString);
            Returns ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;
            #endregion

            #region определение локального пользователя
            int nzpUser = finder.nzp_user;
            
            /*DbWorkUser db = new DbWorkUser();
            int nzpUser = db.GetLocalUser(conn_db, finder, out ret);
            db.Close();
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }*/
            #endregion

            IDataReader reader = null;
            string sql_text = "";
            int order = 0, neworder = 0;
            int nzp_key_neibour = 0;//nzp_key соседней записи
            int new_order_neibour = 0;//новый order соседней записи
            int order_neibour = 0;//order соседней записи

            #region Определить текущий приоритет
#if PG
            sql_text = "select ordering from " + Points.Pref + "_kernel.servpriority where nzp_key = " + finder.nzp_key;
#else
            sql_text = "select ordering from " + Points.Pref + "_kernel:servpriority where nzp_key = " + finder.nzp_key;
#endif
            ret = ExecRead(conn_db, out reader, sql_text, true);
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }
            try
            {
                if (reader.Read()) if (reader["ordering"] != DBNull.Value)
                    {
                        order = Convert.ToInt32(reader["ordering"]);
                        if (finder.move == Actions.MoveServiceDown) neworder = order + 1;
                        else neworder = order - 1;
                    }
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка AddServiceIntoServpriority " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                conn_db.Close();
                return ret;
            }
            #endregion

            if (order == 1 && finder.move == Actions.MoveServiceUp)
            {
                return new Returns(false, "Приоритет услуги не может быть меньше 1", -1);
            }

            #region определить максимальный приоритет
            if (finder.move == Actions.MoveServiceDown)
            {
                int maxorder = 0;
#if PG
                sql_text = "select max(ordering) as ordering from " + Points.Pref + "_kernel.servpriority where dat_po >= '01.01.3000'";
#else
                sql_text = "select max(ordering) as ordering from " + Points.Pref + "_kernel:servpriority where dat_po >= '01.01.3000'";
#endif
                ret = ExecRead(conn_db, out reader, sql_text, true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return ret;
                }
                try
                {
                    if (reader.Read()) if (reader["ordering"] != DBNull.Value)
                        {
                            maxorder = Convert.ToInt32(reader["ordering"]);
                        }
                }
                catch (Exception ex)
                {
                    ret = new Returns(false, ex.Message);
                    MonitorLog.WriteLog("Ошибка AddServiceIntoServpriority " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                    conn_db.Close();
                    return ret;
                }

                if (order == maxorder && finder.move == Actions.MoveServiceDown)
                {
                    return new Returns(false, "Приоритет услуги не может быть больше " + maxorder, -1);
                }
            }
            #endregion

            #region Определить order соседней записи
            if (finder.move == Actions.MoveServiceDown) new_order_neibour = neworder - 1;
            else new_order_neibour = neworder + 1;
            #endregion

            #region определить nzp_key соседней записи
            order_neibour = neworder;
#if PG
            sql_text = "select nzp_key from " + Points.Pref + "_kernel.servpriority where dat_po >= '01.01.3000' and ordering = " + order_neibour;
#else
            sql_text = "select nzp_key from " + Points.Pref + "_kernel:servpriority where dat_po >= '01.01.3000' and ordering = " + order_neibour;
#endif
            ret = ExecRead(conn_db, out reader, sql_text, true);
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }
            try
            {
                while (reader.Read())
                {
                    if (reader["nzp_key"] != DBNull.Value) nzp_key_neibour = Convert.ToInt32(reader["nzp_key"]);
                }
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка AddServiceIntoServpriority " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                conn_db.Close();
                return ret;
            }
            #endregion

            #region выставить dat_po записям, у которых сменился приоритет
            string dat_po = "";
            DateTime dt;
            if (DateTime.TryParse(finder.dat_s, out dt)) dat_po = dt.AddDays(-1).ToShortDateString();
            if (dat_po == "")
            {
                ret.result = false;
                ret.text = "Не задан операционный день";
                conn_db.Close();
                return ret;
            }

#if PG
            sql_text = "update " + Points.Pref + "_kernel.servpriority set dat_po = '" + dat_po + "' where nzp_key in (" + finder.nzp_key + ", " + nzp_key_neibour + ")";
#else
            sql_text = "update " + Points.Pref + "_kernel:servpriority set dat_po = '" + dat_po + "' where nzp_key in (" + finder.nzp_key + ", " + nzp_key_neibour + ")";
#endif
            ret = ExecSQL(conn_db, sql_text, true);
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }
            #endregion

            #region добавить записи с новым приоритетом
#if PG
            sql_text = "insert into " + Points.Pref + "_kernel.servpriority (dat_s, dat_po, nzp_serv, ordering, nzp_user, dat_when)" +
                " select '" + finder.dat_s + "', '01.01.3000', nzp_serv, " + neworder + ", " + nzpUser + ", now() from " + Points.Pref + "_kernel.servpriority " +
                " where nzp_key = " + finder.nzp_key;
#else
            sql_text = "insert into " + Points.Pref + "_kernel:servpriority (dat_s, dat_po, nzp_serv, ordering, nzp_user, dat_when)" +
                " select '" + finder.dat_s + "', '01.01.3000', nzp_serv, " + neworder + ", " + nzpUser + ", current from " + Points.Pref + "_kernel:servpriority " +
                " where nzp_key = " + finder.nzp_key;
#endif
            ret = ExecSQL(conn_db, sql_text, true);
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }

#if PG
            sql_text = "insert into " + Points.Pref + "_kernel.servpriority (dat_s, dat_po, nzp_serv, ordering, nzp_user, dat_when)" +
                " select '" + finder.dat_s + "', '01.01.3000', nzp_serv, " + new_order_neibour + ", " + nzpUser + ", now() from " + Points.Pref + "_kernel.servpriority " +
                " where nzp_key = " + nzp_key_neibour;
#else
            sql_text = "insert into " + Points.Pref + "_kernel:servpriority (dat_s, dat_po, nzp_serv, ordering, nzp_user, dat_when)" +
                " select '" + finder.dat_s + "', '01.01.3000', nzp_serv, " + new_order_neibour + ", " + nzpUser + ", current from " + Points.Pref + "_kernel:servpriority " +
                " where nzp_key = " + nzp_key_neibour;
#endif
            ret = ExecSQL(conn_db, sql_text, true);
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }
            #endregion

            conn_db.Close();
            return ret;
        }

        public List<ServPriority> LoadServpriority(ServPriority finder, out Returns ret)
        {
            #region Проверка входных параметров
            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Пользователь не определен");
                return null;
            }
            #endregion

            #region соединение
            string connectionString = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;
            #endregion

            IDataReader reader = null;
            string sql_text = "";
            List<ServPriority> list = new List<ServPriority>();

            string where = "";
            if (finder.nzp_key > 0) where = " and nzp_key = " + finder.nzp_key;
            else where = " and dat_po >= '01.01.3000'";

            #region Получить данные
#if PG
            sql_text = "select nzp_key, dat_s, dat_po, nzp_serv, ordering, (select service from " + Points.Pref + "_kernel.services where nzp_serv=s.nzp_serv) as service " +
                " from " + Points.Pref + "_kernel.servpriority s where 1=1 " + where +
                " order by ordering ";
#else
            sql_text = "select nzp_key, dat_s, dat_po, nzp_serv, ordering, (select service from " + Points.Pref + "_kernel:services where nzp_serv=s.nzp_serv) as service " +
                " from " + Points.Pref + "_kernel:servpriority s where 1=1 " + where +
                " order by ordering ";
#endif
            ret = ExecRead(conn_db, out reader, sql_text, true);
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }
            try
            {
                while (reader.Read())
                {
                    ServPriority servp = new ServPriority();
                    if (reader["nzp_key"] != DBNull.Value) servp.nzp_key = Convert.ToInt32(reader["nzp_key"]);
                    if (reader["ordering"] != DBNull.Value) servp.ordering = Convert.ToInt32(reader["ordering"]);
                    if (reader["dat_s"] != DBNull.Value) servp.dat_s = Convert.ToString(reader["dat_s"]);
                    if (reader["dat_po"] != DBNull.Value) servp.dat_po = Convert.ToString(reader["dat_po"]);
                    if (reader["nzp_serv"] != DBNull.Value) servp.nzp_serv = Convert.ToInt32(reader["nzp_serv"]);
                    if (reader["service"] != DBNull.Value) servp.service = Convert.ToString(reader["service"]);
                    list.Add(servp);
                }
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка LoadServpriority " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                conn_db.Close();
                return null;
            }
            #endregion

            conn_db.Close();
            return list;
        }

        public List<ServPriority> GetServicesForAdd(ServPriority finder, out Returns ret)
        {
            ret = Utils.InitReturns();

            #region Проверка входных параметров
            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Пользователь не определен");
                return null;
            }
            #endregion

            List<ServPriority> spis = new List<ServPriority>();

            #region соединение
            string connectionString = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;
            #endregion

            //выбрать список
            string sql =
                " Select nzp_serv, service " +
#if PG
 " From " + Points.Pref + "_kernel.services " +
#else
 " From " + Points.Pref + "_kernel:services " +
#endif
 " Where nzp_serv not in (select sp.nzp_serv from " + Points.Pref + "_kernel" + tableDelimiter + "servpriority sp where dat_po >= '01.01.3000') ";

            string where = " and nzp_serv > 1 ";

            if (finder.RolesVal != null)
                foreach (_RolesVal role in finder.RolesVal)
                    if (role.tip == Constants.role_sql & role.kod == Constants.role_sql_serv) where += " and nzp_serv in (" + role.val + ")";

            sql += where + " Order by service";

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
                    ServPriority zap = new ServPriority();
                    if (reader["nzp_serv"] != DBNull.Value) zap.nzp_serv = (int)reader["nzp_serv"];
                    if (reader["service"] != DBNull.Value) zap.service = ((string)reader["service"]).Trim();
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
                MonitorLog.WriteLog("Ошибка заполнения списка услуг " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }

        }

        public Returns SaveServFormula(ServFormula finder)
        {
            Returns ret = Utils.InitReturns();
            if (finder.pref == "") finder.pref = Points.Pref;
            #region соединение
            string connectionString = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;
            #endregion

            IDbTransaction transaction;
            try { transaction = conn_db.BeginTransaction(); }
            catch { transaction = null; }

            finder.nzp_frm_typrs = 0;
            finder.nzp_prm_rash = 0;

            switch (finder.nzp_calc_method)
            {
                case 1: finder.nzp_frm_typrs = 4; //по л/с
                    finder.nzp_prm_rash = 0;
                    break;
                case 2: finder.nzp_frm_typrs = 1; //по общей площади
                    finder.nzp_prm_rash = 4;
                    break;
                case 3: finder.nzp_frm_typrs = 1; //по отапливаемой площади
                    finder.nzp_prm_rash = 133;
                    break;
                case 4: finder.nzp_frm_typrs = 3; //по кол жильцов
                    finder.nzp_prm_rash = 0;
                    break;
                case 5: finder.nzp_frm_typrs = 1; //новый расход
                    int nzp_prm_rash;
                    ret = SaveRashod(finder, conn_db, transaction, out nzp_prm_rash);
                    if (!ret.result)
                    {
                        transaction.Rollback();
                        conn_db.Close();
                        return ret;
                    }
                    finder.nzp_prm_rash = nzp_prm_rash;
                    break;
                case 6: finder.nzp_frm_typrs = 2; //по квартире, с учетом колич л/с в комм квартире
                    finder.nzp_prm_rash = 0;
                    break;
                case 7: finder.nzp_frm_typrs = 1; //по жилой площади
                    finder.nzp_prm_rash = 6;
                    break;
            }

            int nzp_prm_ls = 0, nzp_prm_dom = 0;
            ret = SaveTarif(finder, conn_db, transaction, out nzp_prm_ls, out nzp_prm_dom);
            if (!ret.result)
            {
                transaction.Rollback();
                conn_db.Close();
                return ret;
            }
            finder.nzp_prm_dom = nzp_prm_dom;
            finder.nzp_prm_ls = nzp_prm_ls;

            ret = SaveFormuls(finder, conn_db, transaction);
            if (!ret.result)
            {
                transaction.Rollback();
                conn_db.Close();
                return ret;
            }

            if (transaction != null)
            {
                if (ret.result) transaction.Commit();
                else transaction.Rollback();
            }
            conn_db.Close();
            return ret;
        }

        private Returns SaveRashod(ServFormula finder, IDbConnection conn_db, IDbTransaction transaction, out int nzp_prm_rsh)
        {
            Returns ret = Utils.InitReturns();
            nzp_prm_rsh = 0;
            DbTables tables = new DbTables(conn_db);
            string prm_name = tables.prm_name;
            if (finder.pref == "") finder.pref = Points.Pref;
            if (finder.pref != Points.Pref)
            {
#if PG
                prm_name = finder.pref + "_kernel.prm_name";
#else
                prm_name = finder.pref + "_kernel@" + DBManager.getServer(conn_db) + ":prm_name";
#endif
            }

            string sql = "";
            int cnt = 0;
            int nzp_prm;
            if (finder.nzp_prm_rash > 0)
            {
                nzp_prm = finder.nzp_prm_rash;
                sql = "select count(*) from " + prm_name + " where nzp_prm = " + nzp_prm;
                object obj = ExecScalar(conn_db, transaction, sql, out ret, true);
                if (!ret.result) return ret;

                if (obj == null) cnt = 0;
                else
                {
                    try { cnt = Convert.ToInt32(obj); }
                    catch (Exception ex)
                    {
                        ret.result = false;
                        ret.text = "Ошибка при определении параметра";
                        MonitorLog.WriteLog("Ошибка в функции сохранения расхода:" + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                        return ret;
                    }
                }
            }
            else
            {
                sql = "select max(nzp_prm) from " + prm_name;
                object obj = ExecScalar(conn_db, transaction, sql, out ret, true);
                if (!ret.result) return ret;

                if (obj == null) nzp_prm = 100000;
                else
                {
                    try { nzp_prm = Convert.ToInt32(obj); }
                    catch (Exception ex)
                    {
                        ret.result = false;
                        ret.text = "Ошибка при определении кода параметра";
                        MonitorLog.WriteLog("Ошибка в функции сохранения расхода:" + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                        return ret;
                    }
                    if (nzp_prm < 100000) nzp_prm = 100000;
                    else ++nzp_prm;
                }
            }

            if (cnt > 0)
            {
                sql = " update " + prm_name + " set name_prm = " + Utils.EStrNull(finder.rashod_name) +
                      " where nzp_prm = " + nzp_prm;
            }
            else
            {
                sql = " insert into " + prm_name + " (nzp_prm, name_prm, type_prm, prm_num, low_, high_, digits_) " +
                      " values (" + nzp_prm + "," + Utils.EStrNull(finder.rashod_name) + ", 'int', 1, 0, 1000, 4)";
            }
            ret = ExecSQL(conn_db, transaction, sql, true);
            if (!ret.result) return ret;
            nzp_prm_rsh = nzp_prm;

            return ret;
        }

        private Returns SaveTarif(ServFormula finder, IDbConnection conn_db, IDbTransaction transaction, out int nzp_prm_ls, out int nzp_prm_dom)
        {
            Returns ret = Utils.InitReturns();
            nzp_prm_ls = 0;
            nzp_prm_dom = 0;
            if (finder.pref == "") finder.pref = Points.Pref;
            DbTables tables = new DbTables(conn_db);
            string prm_name = tables.prm_name;
            if (finder.pref != Points.Pref)
            {
#if PG
                prm_name = finder.pref + "_kernel.prm_name";
#else
                prm_name = finder.pref + "_kernel@" + DBManager.getServer(conn_db) + ":prm_name";
#endif
            }

            string sql = "select max(nzp_prm) from " + prm_name;
            object obj = ExecScalar(conn_db, transaction, sql, out ret, true);
            if (!ret.result) return ret;
            int nzp_prm;
            if (obj == null) nzp_prm = 100000;
            else
            {
                try { nzp_prm = Convert.ToInt32(obj); }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка при определении кода параметра";
                    MonitorLog.WriteLog("Ошибка в функции сохранения расхода:" + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                    return ret;
                }
                if (nzp_prm < 100000) nzp_prm = 100000;
                else ++nzp_prm;
            }
            if (finder.tarif_name.Length > 61)
                finder.tarif_name = finder.tarif_name.Substring(0, 60) + "...";
#if PG
            sql = "begin; insert into " + prm_name + " (nzp_prm, name_prm, type_prm, prm_num, low_, high_, digits_) " +
                  " values (" + nzp_prm + "," + Utils.EStrNull(finder.tarif_name + " для л/с") + ", 'float', 1, 0, 1000000, 7); commit;";
#else
            sql = "insert into " + prm_name + " (nzp_prm, name_prm, type_prm, prm_num, low_, high_, digits_) " +
                  " values (" + nzp_prm + "," + Utils.EStrNull(finder.tarif_name + " для л/с") + ", 'float', 1, 0, 1000000, 7)";
#endif
            ret = ExecSQL(conn_db, transaction, sql, true);
            if (!ret.result) return ret;
#if PG
            nzp_prm = (int)ExecScalar(conn_db, transaction, "select max(nzp_prm) from " + prm_name, out ret, true);
#else
            nzp_prm = GetSerialValue(conn_db, transaction);
#endif
            nzp_prm_ls = nzp_prm;
            if (finder.nzp_calc_method != 5)
            {
                nzp_prm++;
                sql = " insert into " + prm_name + " (nzp_prm, name_prm, type_prm, prm_num, low_, high_, digits_) " +
                      " values (" + nzp_prm + "," + Utils.EStrNull(finder.tarif_name + " для  дома") + ", 'float', 2, 0, 1000000, 7)";
                ret = ExecSQL(conn_db, transaction, sql, true);
                if (!ret.result) return ret;
                nzp_prm_dom = nzp_prm;
            }
            return ret;
        }

        private Returns SaveFormuls(ServFormula finder, IDbConnection conn_db, IDbTransaction transaction)
        {
            Returns ret = Utils.InitReturns();
            if (finder.pref == "") finder.pref = Points.Pref;
            DbTables tables = new DbTables(conn_db);
            string formuls = tables.formuls;
            string formuls_opis = tables.formuls_opis;
            string prm_tarifs = tables.prm_tarifs;
            string prm_frm = tables.prm_frm;
            if (finder.pref != Points.Pref)
            {
#if PG
                formuls = finder.pref + "_kernel.formuls";
                formuls_opis = finder.pref + "_kernel.formuls_opis";
                prm_tarifs = finder.pref + "_kernel.prm_tarifs";
                prm_frm = finder.pref + "_kernel.prm_frm";
#else
                formuls = finder.pref + "_kernel@" + DBManager.getServer(conn_db) + ":formuls";
                formuls_opis = finder.pref + "_kernel@" + DBManager.getServer(conn_db) + ":formuls_opis";
                prm_tarifs = finder.pref + "_kernel@" + DBManager.getServer(conn_db) + ":prm_tarifs";
                prm_frm = finder.pref + "_kernel@" + DBManager.getServer(conn_db) + ":prm_frm";
#endif
            }
            string fields = "", values = "";
            if (finder.dat_s != "")
            {
                fields += ", dat_s ";
                values += ", " + Utils.EStrNull(finder.dat_s);
            }

            if (finder.dat_po != "")
            {
                fields += ", dat_po ";
                values += ", " + Utils.EStrNull(finder.dat_po);
            }

            string sql = "select max(nzp_frm) from " + formuls;
            object obj = ExecScalar(conn_db, transaction, sql, out ret, true);
            if (!ret.result) return ret;
            int nzp_frm;
            if (obj == null) nzp_frm = 100000;
            else
            {
                try { nzp_frm = Convert.ToInt32(obj); }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка при определении кода параметра";
                    MonitorLog.WriteLog("Ошибка в функции сохранения расхода:" + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                    return ret;
                }
                if (nzp_frm < 100000) nzp_frm = 100000;
                else ++nzp_frm;
            }

            sql = " insert into " + formuls + " (nzp_frm, name_frm, nzp_measure, is_device " + fields + ") " +
                         " values (" + nzp_frm + "," + Utils.EStrNull(finder.formula_name) + ", " + finder.nzp_measure + ", 0" + values + ")";
            ret = ExecSQL(conn_db, transaction, sql, true);
            if (!ret.result) return ret;

            //   int nzp_frm = GetSerialValue(conn_db, transaction);

            sql = " insert into " + formuls_opis + "(nzp_frm, nzp_frm_kod, nzp_frm_typ, nzp_prm_tarif_ls, nzp_prm_tarif_dm, nzp_frm_typrs, nzp_prm_rash)" +
                  " values (" + nzp_frm + "," + nzp_frm + ",1," + finder.nzp_prm_ls + "," + finder.nzp_prm_dom + ", " + finder.nzp_frm_typrs + "," + finder.nzp_prm_rash + ")";
            ret = ExecSQL(conn_db, transaction, sql, true);
            if (!ret.result) return ret;

            #region определение локального пользователя
            int nzpUser = finder.nzp_user;
            
            /*DbWorkUser db = new DbWorkUser();
            int nzpUser = db.GetLocalUser(conn_db, transaction, finder, out ret);
            db.Close();
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }*/
            #endregion

#if PG
            sql = " insert into " + prm_tarifs + "(nzp_serv, nzp_frm, nzp_prm, is_edit, nzp_user, dat_when )" +
                  " values(" + finder.nzp_serv + "," + nzp_frm + ",0,1," + nzpUser + ",now())";
#else
            sql = " insert into " + prm_tarifs + "(nzp_serv, nzp_frm, nzp_prm, is_edit, nzp_user, dat_when )" +
                  " values(" + finder.nzp_serv + "," + nzp_frm + ",0,1," + nzpUser + ",current)";
#endif
            ret = ExecSQL(conn_db, transaction, sql, true);
            if (!ret.result) return ret;

            if (finder.nzp_prm_rash > 0)
            {
                sql = " insert into " + prm_frm + "(nzp_frm, frm_calc,is_prm, nzp_prm)" +
                      " values (" + nzp_frm + ",0,1," + finder.nzp_prm_rash + ")";
                ret = ExecSQL(conn_db, transaction, sql, true);
                if (!ret.result) return ret;
            }

            if (finder.nzp_prm_ls > 0)
            {
                sql = " insert into " + prm_frm + "(nzp_frm, frm_calc,is_prm, nzp_prm)" +
                      " values (" + nzp_frm + ",0,1," + finder.nzp_prm_ls + ")";
                ret = ExecSQL(conn_db, transaction, sql, true);
                if (!ret.result) return ret;
            }

            if (finder.nzp_prm_dom > 0)
            {
                sql = " insert into " + prm_frm + "(nzp_frm, frm_calc,is_prm, nzp_prm)" +
                      " values (" + nzp_frm + ",0,1," + finder.nzp_prm_dom + ")";
                ret = ExecSQL(conn_db, transaction, sql, true);
                if (!ret.result) return ret;
            }

            return ret;
        }

        public Returns CopyFormulsToLocalBD(Service finder)
        {
            #region соединение
            string connectionString = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(connectionString);
            Returns ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;
            #endregion

            DbTables tables = new DbTables(conn_db);
            string sql = " select * from " + tables.prm_tarifs + " where nzp_serv = " + finder.nzp_serv;
            IDataReader reader, reader2;

            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }
            ServFormula sf = new ServFormula();
            List<ServFormula> listf = new List<ServFormula>();
            while (reader.Read())
            {
                sf = new ServFormula();
                if (reader["nzp_frm"] != DBNull.Value) sf.nzp_frm = Convert.ToInt32(reader["nzp_frm"]);
                if (reader["nzp_prm"] != DBNull.Value) sf.nzp_prm_tarif_ls = Convert.ToInt32(reader["nzp_prm"]);
                listf.Add(sf);
            }
            reader.Close();

            if (listf.Count == 0)
            {
                ret = new Returns(true, "У выбранной услуги нет формул", -1);
                return ret;
            }

            string prm_name = "";
            string prm_tarifs = "";
            string prm_frm = "";
            string formuls = "";
            string formuls_opis = "";
#if PG
            prm_name = finder.pref + "_kernel.prm_name";
            prm_tarifs = finder.pref + "_kernel.prm_tarifs";
            prm_frm = finder.pref + "_kernel.prm_frm";
            formuls = finder.pref + "_kernel.formuls";
            formuls_opis = finder.pref + "_kernel.formuls_opis";
#else
            prm_name = finder.pref + "_kernel@" + DBManager.getServer(conn_db) + ":prm_name";
            prm_tarifs = finder.pref + "_kernel@" + DBManager.getServer(conn_db) + ":prm_tarifs";
            prm_frm = finder.pref + "_kernel@" + DBManager.getServer(conn_db) + ":prm_frm";
            formuls = finder.pref + "_kernel@" + DBManager.getServer(conn_db) + ":formuls";
            formuls_opis = finder.pref + "_kernel@" + DBManager.getServer(conn_db) + ":formuls_opis";
#endif
            #region определение локального пользователя
            int nzpUser = finder.nzp_user;
            
            /*DbWorkUser db = new DbWorkUser();
            int nzpUser = db.GetLocalUser(conn_db, finder, out ret);
            db.Close();
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }*/
            #endregion

            foreach (ServFormula f in listf)
            {
                #region formuls
                sql = "select count(*) from " + formuls + " where nzp_frm = " + f.nzp_frm;
                object obj = ExecScalar(conn_db, sql, out ret, true);
                if (!ret.result) return ret;
                int count;
                if (obj == null) count = 0;
                else
                {
                    try { count = Convert.ToInt32(obj); }
                    catch (Exception ex)
                    {
                        ret.result = false;
                        MonitorLog.WriteLog("Ошибка при копировании формул из центрального банка в локальный банк:" + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                        return ret;
                    }
                }
                if (count == 0)
                {
                    sql = "insert into " + formuls + " (nzp_frm,name_frm,dat_s,dat_po,tarif,nzp_measure,is_device) " +
                        "  select nzp_frm,name_frm,dat_s,dat_po,tarif,nzp_measure,is_device from " + tables.formuls + " " +
                        "  where nzp_frm = " + f.nzp_frm;
                }
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return ret;
                #endregion

                #region formuls_opis
                sql = "select count(*) from " + formuls_opis + " where nzp_frm = " + f.nzp_frm;
                obj = ExecScalar(conn_db, sql, out ret, true);
                if (!ret.result) return ret;
                if (obj == null) count = 0;
                else
                {
                    try { count = Convert.ToInt32(obj); }
                    catch (Exception ex)
                    {
                        ret.result = false;
                        MonitorLog.WriteLog("Ошибка при копировании формул из центрального банка в локальный банк:" + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                        return ret;
                    }
                }
                if (count == 0)
                {
                    sql = "insert into " + formuls_opis + " (nzp_frm,nzp_frm_kod,nzp_frm_typ,nzp_prm_tarif_ls,nzp_prm_tarif_lsp,nzp_prm_tarif_dm," +
                          "nzp_prm_tarif_su,nzp_prm_tarif_bd,nzp_frm_typrs,nzp_prm_rash,nzp_prm_rash1,nzp_prm_rash2,dat_s,dat_po) " +
                          " select nzp_frm,nzp_frm_kod,nzp_frm_typ,nzp_prm_tarif_ls,nzp_prm_tarif_lsp,nzp_prm_tarif_dm," +
                          " nzp_prm_tarif_su,nzp_prm_tarif_bd,nzp_frm_typrs,nzp_prm_rash,nzp_prm_rash1,nzp_prm_rash2,dat_s,dat_po from " +
                          tables.formuls_opis + "  where nzp_frm = " + f.nzp_frm;
                }
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return ret;
                #endregion

                #region prm_name
                sql = "select count(*) from " + prm_name + " where nzp_prm = " + f.nzp_prm_tarif_ls;
                obj = ExecScalar(conn_db, sql, out ret, true);
                if (!ret.result) return ret;
                if (obj == null) count = 0;
                else
                {
                    try { count = Convert.ToInt32(obj); }
                    catch (Exception ex)
                    {
                        ret.result = false;
                        MonitorLog.WriteLog("Ошибка при копировании формул из центрального банка в локальный банк:" + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                        return ret;
                    }
                }
                if (count == 0 && f.nzp_prm_tarif_ls > 0)
                {
                    sql = "insert into " + prm_name + " (nzp_prm,name_prm,numer,old_field,type_prm,nzp_res,prm_num,low_,high_,digits_) " +
                          " select nzp_prm,name_prm,numer,old_field,type_prm,nzp_res,prm_num,low_,high_,digits_ from " +
                          tables.prm_name + "  where nzp_prm = " + f.nzp_prm_tarif_ls;
                }
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return ret;


                #endregion

                #region prm_frm
                sql = "select count(*) from " + prm_frm + " where nzp_frm = " + f.nzp_frm;
                obj = ExecScalar(conn_db, sql, out ret, true);
                if (!ret.result) return ret;
                if (obj == null) count = 0;
                else
                {
                    try { count = Convert.ToInt32(obj); }
                    catch (Exception ex)
                    {
                        ret.result = false;
                        MonitorLog.WriteLog("Ошибка при копировании формул из центрального банка в локальный банк:" + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                        return ret;
                    }
                }
                if (count == 0)
                {
                    string order = "";
#if PG
                    order = "\"order\"";
#else
                    order = "order";
#endif
                    sql = "insert into " + prm_frm + " (nzp_frm,frm_calc," + order + ",is_prm,operation,nzp_prm,frm_p1,frm_p2,frm_p3,result) " +
                        " select nzp_frm,frm_calc," + order + ",is_prm,operation,nzp_prm,frm_p1,frm_p2,frm_p3,result from " + tables.prm_frm + " " +
                        "  where nzp_frm = " + f.nzp_frm;
                }
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return ret;
                #endregion

                #region prm_trifs
                sql = "select count(*) from " + prm_tarifs + " where nzp_serv = " + finder.nzp_serv +
                    " and nzp_frm = " + f.nzp_frm + " and nzp_prm = " + f.nzp_prm_tarif_ls;
                obj = ExecScalar(conn_db, sql, out ret, true);
                if (!ret.result) return ret;
                if (obj == null) count = 0;
                else
                {
                    try { count = Convert.ToInt32(obj); }
                    catch (Exception ex)
                    {
                        ret.result = false;
                        MonitorLog.WriteLog("Ошибка при копировании формул из центрального банка в локальный банк:" + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                        return ret;
                    }
                }
                if (count == 0)
                {
                    string dat_when = "";
#if PG
                    dat_when = "now()";
#else
                    dat_when = "current";
#endif
                    sql = " insert into " + prm_tarifs + " (nzp_serv,nzp_frm,nzp_prm,is_edit,nzp_user,dat_when) " +
                          " select nzp_serv,nzp_frm,nzp_prm,is_edit," + nzpUser + "," + dat_when + "  from " + tables.prm_tarifs +
                          " where nzp_serv = " + finder.nzp_serv +
                          " and nzp_frm = " + f.nzp_frm + " and nzp_prm = " + f.nzp_prm_tarif_ls;
                    ret = ExecSQL(conn_db, sql, true);
                    if (!ret.result) return ret;
                }
                #endregion

                sql = "select nzp_prm from " + prm_frm + " where nzp_frm = " + f.nzp_frm;
                ret = ExecRead(conn_db, out reader2, sql, true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return ret;
                }
                while (reader2.Read())
                {
                    int nzp_prm = 0;
                    if (reader2["nzp_prm"] != DBNull.Value) nzp_prm = Convert.ToInt32(reader2["nzp_prm"]);

                    if (nzp_prm > 0)
                    {
                        sql = "select count(*) from " + prm_name + " where nzp_prm = " + nzp_prm;
                        obj = ExecScalar(conn_db, sql, out ret, true);
                        if (!ret.result) return ret;
                        if (obj == null) count = 0;
                        else
                        {
                            try { count = Convert.ToInt32(obj); }
                            catch (Exception ex)
                            {
                                ret.result = false;
                                MonitorLog.WriteLog("Ошибка при копировании формул из центрального банка в локальный банк:" + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                                return ret;
                            }
                        }
                        if (count == 0)
                        {
                            sql = "insert into " + prm_name + " (nzp_prm,name_prm,numer,old_field,type_prm,nzp_res,prm_num,low_,high_,digits_) " +
                                  " select nzp_prm,name_prm,numer,old_field,type_prm,nzp_res,prm_num,low_,high_,digits_ from " +
                                  tables.prm_name + "  where nzp_prm = " + nzp_prm;
                        }
                        ret = ExecSQL(conn_db, sql, true);
                        if (!ret.result) return ret;
                    }
                }
                reader2.Close();
            }
            conn_db.Close();
            return ret;
        }

        public List<ServFormula> LoadSevFormuls(ServFormula finder, out Returns ret)
        {
            #region соединение
            string connectionString = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;
            #endregion

            DbTables tables = new DbTables(conn_db);

            string sql = "select pt.nzp_frm, pt.nzp_serv, s.service, u.comment, pt.dat_when, " +
                         " f.name_frm, f.nzp_measure, m.measure, f.dat_s, f.dat_po " +
                         " , fo.nzp_frm_typrs,fo.nzp_prm_rash, fo.nzp_prm_tarif_ls, fo.nzp_prm_tarif_dm " +
                         " from " + tables.prm_tarifs + " pt " +
                         " left outer join " + tables.user + " u on  u.nzp_user = pt.nzp_user ,"
                         + tables.services + " s, " + tables.formuls +
                         " f, " + tables.measure + " m, " + tables.formuls_opis + " fo " +
                         " where pt.nzp_serv = " + finder.nzp_serv + " and pt.nzp_serv = s.nzp_serv " +
                         " and f.nzp_frm = pt.nzp_frm  and f.nzp_measure = m.nzp_measure " +
                         " and fo.nzp_frm = pt.nzp_frm "; ;
            List<ServFormula> list = new List<ServFormula>();

            IDataReader reader, reader2, reader3;

            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }

            while (reader.Read())
            {
                ServFormula sf = new ServFormula();
                if (reader["nzp_frm"] != DBNull.Value) sf.nzp_frm = Convert.ToInt32(reader["nzp_frm"]);
                if (reader["nzp_serv"] != DBNull.Value) sf.nzp_serv = Convert.ToInt32(reader["nzp_serv"]);
                if (reader["service"] != DBNull.Value) sf.service = Convert.ToString(reader["service"]);
                if (reader["dat_when"] != DBNull.Value) sf.created = String.Format("{0:dd.MM.yyyy}", reader["dat_when"]);
                if (reader["comment"] != DBNull.Value) sf.created += " (" + Convert.ToString(reader["comment"]) + ")";
                if (reader["name_frm"] != DBNull.Value) sf.formula_name = Convert.ToString(reader["name_frm"]);
                if (reader["measure"] != DBNull.Value) sf.measure = Convert.ToString(reader["measure"]);
                if (reader["dat_s"] != DBNull.Value) sf.dat_s = String.Format("{0:dd.MM.yyyy}", reader["dat_s"]);
                if (reader["dat_po"] != DBNull.Value) sf.dat_po = String.Format("{0:dd.MM.yyyy}", reader["dat_po"]);
                if (reader["nzp_frm_typrs"] != DBNull.Value) sf.nzp_frm_typrs = Convert.ToInt32(reader["nzp_frm_typrs"]);
                if (reader["nzp_prm_rash"] != DBNull.Value) sf.nzp_prm_rash = Convert.ToInt32(reader["nzp_prm_rash"]);
                if (reader["nzp_prm_tarif_dm"] != DBNull.Value) sf.nzp_prm_tarif_dm = Convert.ToInt32(reader["nzp_prm_tarif_dm"]);
                if (reader["nzp_prm_tarif_ls"] != DBNull.Value) sf.nzp_prm_tarif_ls = Convert.ToInt32(reader["nzp_prm_tarif_ls"]);

                if (sf.nzp_frm_typrs == 4 && sf.nzp_prm_rash == 0) sf.nzp_calc_method = 1;
                else if (sf.nzp_frm_typrs == 1 && sf.nzp_prm_rash == 4) sf.nzp_calc_method = 2;
                else if (sf.nzp_frm_typrs == 1 && sf.nzp_prm_rash == 133) sf.nzp_calc_method = 3;
                else if (sf.nzp_frm_typrs == 3 && sf.nzp_prm_rash == 0) sf.nzp_calc_method = 4;
                else if (sf.nzp_frm_typrs == 2 && sf.nzp_prm_rash == 0) sf.nzp_calc_method = 6;
                else sf.nzp_calc_method = 5;

                sql = "select method_name from " + tables.calc_method + " where nzp_calc_method = " + sf.nzp_calc_method;
                ret = ExecRead(conn_db, out reader2, sql, true);
                if (!ret.result)
                {
                    reader.Close();
                    conn_db.Close();
                    return null;
                }
                while (reader2.Read())
                {
                    if (reader2["method_name"] != DBNull.Value) sf.method_name = Convert.ToString(reader2["method_name"]);
                }

                sql = "select is_prm, nzp_prm from " + tables.prm_frm + " where nzp_frm = " + sf.nzp_frm;
                ret = ExecRead(conn_db, out reader2, sql, true);
                if (!ret.result)
                {
                    reader.Close();
                    conn_db.Close();
                    return null;
                }
                int is_prm = 0;
                int nzp_prm = 0;
                string name_prm = "";
                while (reader2.Read())
                {
                    is_prm = 0;
                    nzp_prm = 0;
                    if (reader2["is_prm"] != DBNull.Value) is_prm = Convert.ToInt32(reader2["is_prm"]);
                    if (reader2["nzp_prm"] != DBNull.Value) nzp_prm = Convert.ToInt32(reader2["nzp_prm"]);

                    if (is_prm == 1)
                    {
                        sql = "select name_prm from " + tables.prm_name + " where nzp_prm = " + nzp_prm;
                        ret = ExecRead(conn_db, out reader3, sql, true);
                        if (!ret.result)
                        {
                            reader2.Close();
                            conn_db.Close();
                            return null;
                        }
                        while (reader3.Read())
                        {
                            if (name_prm == "")
                            {
                                if (reader3["name_prm"] != DBNull.Value) name_prm += Convert.ToString(reader3["name_prm"]).Trim();
                            }
                            else
                            {
                                if (reader3["name_prm"] != DBNull.Value) name_prm += ", " + Convert.ToString(reader3["name_prm"]).Trim();
                            }
                        }
                    }
                }

                sf.tarif_name = name_prm;

                list.Add(sf);
            }
            reader.Close();

            return list;
        }

        public int Suppliers(List<string> pref, string Pref, StreamWriter writer, IDbConnection conn, bool flag)
        {
            string sqlString = "";

            if (!ExecSQL(conn,
           " drop table tmp_supp;", false).result) { }

            IDataReader reader = null;
            sqlString = "  create temp table tmp_supp(nzp_supp integer, name_supp char(100), ur_adres     char(100),  fact_adres char(100), " +
            "inn   char(20),  kpp   char(20), raschet_schet  char(20), bank       char(100),  bik_bank   char(20),  korresp_schet char(20)  )   with no log; ";

            ClassDBUtils.OpenSQL(sqlString, conn);


            sqlString = " insert into tmp_supp(nzp_supp,name_supp,ur_adres, fact_adres, inn,kpp,raschet_schet,bank,bik_bank,korresp_schet ) " +
#if PG
 " select s.nzp_supp, s.name_supp, '', '', '', '', '', '', '', '' from " + Pref + "_kernel.supplier s ;";
#else
 " select s.nzp_supp, s.name_supp, '', '', '', '', '', '', '', '' from " + Pref + "_kernel:supplier s ;";
#endif
            ClassDBUtils.OpenSQL(sqlString, conn);


#if PG
            sqlString = "update tmp_supp  set ur_adres= (select p.val_prm from    " + Pref + "_data.prm_11 p  where p.nzp_prm=117 and p.nzp=nzp_supp );";
#else
            sqlString = "update tmp_supp  set ur_adres= (select p.val_prm from    " + Pref + "_data:prm_11 p  where p.nzp_prm=117 and p.nzp=nzp_supp );";
#endif
            ClassDBUtils.OpenSQL(sqlString, conn);
            sqlString = "update tmp_supp set inn = (select p.val_prm from " + Pref + "_data:prm_11 p where p.nzp_prm=445 and p.nzp=nzp_supp  ); ";
            ClassDBUtils.OpenSQL(sqlString, conn);
            sqlString = " update tmp_supp set kpp = (select p.val_prm from " + Pref + "_data:prm_11 p where p.nzp_prm=445 and p.nzp=nzp_supp  ); ";
            ClassDBUtils.OpenSQL(sqlString, conn);

            sqlString = "select * from tmp_supp order by nzp_supp;";

            int i = 0;
            if (!ExecRead(conn, out reader, sqlString, true).result)
            {
                conn.Close();
                return 0;
            }
            try
            {
                if (reader != null)
                {
                    if (!flag)
                    {

                        while (reader.Read())
                        {

                            string str = "5|" +
                            (reader["nzp_supp"] != DBNull.Value ? ((int)reader["nzp_supp"]) + "|" : "|") +
                            (reader["name_supp"] != DBNull.Value ? ((string)reader["name_supp"]).ToString().Trim() + "|" : "|") +
                            (reader["ur_adres"] != DBNull.Value ? ((string)reader["ur_adres"]).ToString().Trim() + "|" : "|") +
                            (reader["fact_adres"] != DBNull.Value ? ((string)reader["fact_adres"]).ToString().Trim() + "|" : "|") +
                            (reader["inn"] != DBNull.Value ? ((string)reader["inn"]).ToString().Trim() + "|" : "|") +
                            (reader["kpp"] != DBNull.Value ? ((string)reader["kpp"]).ToString().Trim() + "|" : "|") +
                            (reader["raschet_schet"] != DBNull.Value ? ((string)reader["raschet_schet"]).ToString().Trim() + "|" : "|") +
                            (reader["bank"] != DBNull.Value ? ((string)reader["bank"]).ToString().Trim() + "|" : "|") +
                            (reader["bik_bank"] != DBNull.Value ? ((string)reader["bik_bank"]).ToString().Trim() + "|" : "|") +
                            (reader["korresp_schet"] != DBNull.Value ? ((string)reader["korresp_schet"]).ToString().Trim() + "|" : "|");

                            writer.WriteLine(str);
                            i++;

                        }
                    }
                    else
                    {
                        while (reader.Read())
                        {

                            string str = "5|" +
                            (reader["nzp_supp"] != DBNull.Value ? ((int)reader["nzp_supp"]) + "|" : "|") +
                            (reader["nzp_supp"] != DBNull.Value ? "Поставщик" + ((string)reader["nzp_supp"]).ToString().Trim() + "|" : "|") +
                            (reader["ur_adres"] != DBNull.Value ? ((string)reader["ur_adres"]).ToString().Trim() + "|" : "|") +
                            (reader["fact_adres"] != DBNull.Value ? ((string)reader["fact_adres"]).ToString().Trim() + "|" : "|") +
                            (reader["inn"] != DBNull.Value ? ((string)reader["inn"]).ToString().Trim() + "|" : "|") +
                            (reader["kpp"] != DBNull.Value ? ((string)reader["kpp"]).ToString().Trim() + "|" : "|") +
                            (reader["raschet_schet"] != DBNull.Value ? ((string)reader["raschet_schet"]).ToString().Trim() + "|" : "|") +
                            (reader["bank"] != DBNull.Value ? ((string)reader["bank"]).ToString().Trim() + "|" : "|") +
                            (reader["bik_bank"] != DBNull.Value ? ((string)reader["bik_bank"]).ToString().Trim() + "|" : "|") +
                            (reader["korresp_schet"] != DBNull.Value ? ((string)reader["korresp_schet"]).ToString().Trim() + "|" : "|");

                            writer.WriteLine(str);
                            i++;

                        }
                    }

                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка при записи данных  " + ex.Message, MonitorLog.typelog.Error, true);
                reader.Close();
                return 0;
            }

            writer.Flush();
            return i;
        }

        public Returns GetDependencie(out List<Dependencie> Dependencie)
        {
            Returns ret = Utils.InitReturns();
            Dependencie = new List<Dependencie>();

#if PG
            string strSqlQuery = String.Format("SELECT dep.nzp_dep_servs, dep.nzp_dep, dep.nzp_serv, dep.nzp_serv_slave, dep.nzp_area, types.name_dep, area.area, " +
                                               " master_ser.service AS master_service, slave_ser.service AS slave_service, dep.dat_s, dep.dat_po " +
                                               " FROM {0}_data.dep_servs dep LEFT OUTER JOIN {0}_data.s_area area ON (dep.nzp_area = area.nzp_area) " +
                                               " INNER JOIN {0}_kernel.services master_ser ON (dep.nzp_serv = master_ser.nzp_serv) " +
                                               " INNER JOIN {0}_kernel.services slave_ser ON (dep.nzp_serv_slave = slave_ser.nzp_serv) " +
                                               " INNER JOIN {0}_kernel.s_dep_types types ON (dep.nzp_dep = types.nzp_dep) " +
                                               " WHERE dep.is_actual = 1 ORDER BY area.area,types.name_dep, master_ser.service,dep.nzp_dep_servs DESC;", Points.Pref);
#else
            string strSqlQuery = String.Format("SELECT dep.nzp_dep_servs, dep.nzp_dep, dep.nzp_serv, dep.nzp_serv_slave, dep.nzp_area, types.name_dep, area.area, master_ser.service AS master_service, slave_ser.service AS slave_service, dep.dat_s, dep.dat_po FROM {0}_data:dep_servs dep LEFT OUTER JOIN {0}_data:s_area area ON (dep.nzp_area = area.nzp_area) INNER JOIN {0}_kernel:services master_ser ON (dep.nzp_serv = master_ser.nzp_serv) INNER JOIN {0}_kernel:services slave_ser ON (dep.nzp_serv_slave = slave_ser.nzp_serv) INNER JOIN {0}_kernel:s_dep_types types ON (dep.nzp_dep = types.nzp_dep) WHERE dep.is_actual = 1 ORDER BY dep.nzp_dep_servs DESC;", Points.Pref);
#endif
            IDbConnection conn = GetConnection(Constants.cons_Kernel);
            IDataReader reader;
            try
            {
                conn.Open();
                ret = ExecRead(conn, out reader, strSqlQuery, true);
                if (ret.result)
                {
                    while (reader.Read())
                    {
                        int nzp_dep_servs = 0;
                        string name_dep = "";
                        int nzp_dep = 0;
                        int nzp_serv = 0;
                        int nzp_serv_slave = 0;
                        int nzp_area = 0;
                        string area = "";
                        string master_service = "";
                        string slave_service = "";
                        DateTime dat_s = new DateTime();
                        DateTime dat_po = new DateTime();

                        if (reader["nzp_dep_servs"] != DBNull.Value) Int32.TryParse(reader["nzp_dep_servs"].ToString(), out nzp_dep_servs);
                        if (reader["name_dep"] != DBNull.Value) name_dep = reader["name_dep"].ToString();
                        if (reader["nzp_dep"] != DBNull.Value) Int32.TryParse(reader["nzp_dep"].ToString(), out nzp_dep);
                        if (reader["nzp_serv"] != DBNull.Value) Int32.TryParse(reader["nzp_serv"].ToString(), out nzp_serv);
                        if (reader["nzp_serv_slave"] != DBNull.Value) Int32.TryParse(reader["nzp_serv_slave"].ToString(), out nzp_serv_slave);
                        if (reader["nzp_area"] != DBNull.Value) Int32.TryParse(reader["nzp_area"].ToString(), out nzp_area);
                        if (reader["area"] != DBNull.Value) area = reader["area"].ToString();
                        if (reader["master_service"] != DBNull.Value) master_service = reader["master_service"].ToString();
                        if (reader["slave_service"] != DBNull.Value) slave_service = reader["slave_service"].ToString();
                        if (reader["dat_s"] != DBNull.Value) DateTime.TryParse(reader["dat_s"].ToString(), out dat_s);
                        if (reader["dat_po"] != DBNull.Value) DateTime.TryParse(reader["dat_po"].ToString(), out dat_po);

                        Dependencie.Add(
                            new Dependencie()
                            {
                                nzp_dep_servs = nzp_dep_servs,
                                name_dep = name_dep,
                                nzp_dep = nzp_dep,
                                nzp_serv = nzp_serv,
                                nzp_serv_slave = nzp_serv_slave,
                                nzp_area = nzp_area,
                                area = area,
                                master_service = master_service,
                                slave_service = slave_service,
                                dat_s = dat_s,
                                dat_po = dat_po,
                                is_actual = 1
                            }
                            );
                    }
                }
            }
            catch (Exception ex)
            {
                ret.tag = -1;
                ret.text = ex.Message;
                ret.result = false;
                MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
            }
            finally { conn.Close(); }
            return ret;
        }

        public Returns SetDependencie(Dependencie dep)
        {
            Returns ret = Utils.InitReturns();
            string strSqlQuery;

            if (dep.nzp_dep_servs > 0)
            {
#if PG
                strSqlQuery = String.Format("UPDATE {0}_data.dep_servs SET (nzp_dep, nzp_serv, nzp_serv_slave, nzp_area, dat_s, dat_po, is_actual) = ({1}, {2}, {3}, {4}, {5}, {6}, {7}) WHERE nzp_dep_servs = {8};", Points.Pref, dep.nzp_dep, dep.nzp_serv, dep.nzp_serv_slave, dep.nzp_area, Utils.EStrNull(dep.dat_s.Date.ToShortDateString()), Utils.EStrNull(dep.dat_po.ToShortDateString()), dep.is_actual, dep.nzp_dep_servs);
#else
                strSqlQuery = String.Format("UPDATE {0}_data:dep_servs SET (nzp_dep, nzp_serv, nzp_serv_slave, nzp_area, dat_s, dat_po, is_actual) = ({1}, {2}, {3}, {4}, DATE({5}), DATE({6}), {7}) WHERE nzp_dep_servs = {8};", Points.Pref, dep.nzp_dep, dep.nzp_serv, dep.nzp_serv_slave, dep.nzp_area, Utils.EStrNull(dep.dat_s.Date.ToShortDateString()), Utils.EStrNull(dep.dat_po.ToShortDateString()), dep.is_actual, dep.nzp_dep_servs);
#endif
            }
            else
            {
#if PG
                strSqlQuery = String.Format("INSERT INTO {0}_data.dep_servs (nzp_dep, nzp_serv, nzp_serv_slave, nzp_area, dat_s, dat_po) VALUES ({1}, {2}, {3}, {4}, {5}, {6}) RETURNING nzp_dep_servs;", Points.Pref, dep.nzp_dep, dep.nzp_serv, dep.nzp_serv_slave, dep.nzp_area, Utils.EStrNull(dep.dat_s.Date.ToShortDateString()), Utils.EStrNull(dep.dat_po.ToShortDateString()));
#else
                strSqlQuery = String.Format("INSERT INTO {0}_data:dep_servs (nzp_dep, nzp_serv, nzp_serv_slave, nzp_area, dat_s, dat_po) VALUES ({1}, {2}, {3}, {4}, DATE({5}), DATE({6})) ", Points.Pref, dep.nzp_dep, dep.nzp_serv, dep.nzp_serv_slave, dep.nzp_area, Utils.EStrNull(dep.dat_s.Date.ToShortDateString()), Utils.EStrNull(dep.dat_po.ToShortDateString()));
#endif
            }

            IDbConnection conn = GetConnection(Constants.cons_Kernel);
            //IDataReader reader;
            try
            {
                conn.Open();
                ret = ExecSQL(conn, strSqlQuery, true);
                /*
                if (dep.nzp_dep_servs > 0) ret = ExecSQL(conn, strSqlQuery, true);
                else
                {
                    ret = ExecRead(conn, out reader, strSqlQuery, true);
                    if (ret.result)
                    {
                        while (reader.Read())
                        {
                            int nzp_dep_servs = 0;
                            if (reader["nzp_dep_servs"] != DBNull.Value) Int32.TryParse(reader["nzp_dep_servs"].ToString(), out nzp_dep_servs);
                            ret.tag = nzp_dep_servs;
                        }
                    }
                }
                 * */
            }
            catch (Exception ex)
            {
                ret.tag = -1;
                ret.text = ex.Message;
                ret.result = false;
                MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
            }
            finally { conn.Close(); }
            return ret;
        }

        public Returns FillDependencieList(out List<Returns> lst)
        {
            Returns ret = Utils.InitReturns();
            lst = new List<Returns>();

#if PG
            string strSqlQuery = String.Format("SELECT nzp_serv, service_small FROM {0}_kernel.services ORDER BY 2;", Points.Pref);
#else
            string strSqlQuery = String.Format("SELECT nzp_serv, service_small FROM {0}_kernel:services;", Points.Pref);
#endif
            IDbConnection conn = GetConnection(Constants.cons_Kernel);

            try
            {
                conn.Open();
                IDataReader reader;
                ret = ExecRead(conn, out reader, strSqlQuery, true);

                if (ret.result)
                    while (reader.Read())
                    {
                        int nzp_serv = 0;
                        string service_small = "";
                        if (reader["nzp_serv"] != DBNull.Value) Int32.TryParse(reader["nzp_serv"].ToString(), out nzp_serv);
                        if (reader["service_small"] != DBNull.Value) service_small = reader["service_small"].ToString();
                        if (nzp_serv != 0 && !String.IsNullOrEmpty(service_small)) lst.Add(new Returns() { tag = nzp_serv, text = service_small, sql_error = "services" });
                    }
#if PG
                strSqlQuery = String.Format("SELECT nzp_area, area FROM {0}_data.s_area ORDER BY 2;", Points.Pref);
#else
                strSqlQuery = String.Format("SELECT nzp_area, area FROM {0}_data:s_area;", Points.Pref);
#endif
                reader = null;
                ret = ExecRead(conn, out reader, strSqlQuery, true);

                if (ret.result)
                    while (reader.Read())
                    {
                        int nzp_area = 0;
                        string area = "";
                        if (reader["nzp_area"] != DBNull.Value) Int32.TryParse(reader["nzp_area"].ToString(), out nzp_area);
                        if (reader["area"] != DBNull.Value) area = reader["area"].ToString();
                        if (nzp_area != 0 && !String.IsNullOrEmpty(area)) lst.Add(new Returns() { tag = nzp_area, text = area, sql_error = "area" });
                    }

#if PG
                strSqlQuery = String.Format("SELECT nzp_dep, name_dep FROM {0}_kernel.s_dep_types ORDER BY 2;", Points.Pref);
#else
                strSqlQuery = String.Format("SELECT nzp_dep, name_dep FROM {0}_kernel:s_dep_types;", Points.Pref);
#endif
                reader = null;
                ret = ExecRead(conn, out reader, strSqlQuery, true);

                if (ret.result)
                    while (reader.Read())
                    {
                        int nzp_dep = 0;
                        string name_dep = "";
                        if (reader["nzp_dep"] != DBNull.Value) Int32.TryParse(reader["nzp_dep"].ToString(), out nzp_dep);
                        if (reader["name_dep"] != DBNull.Value) name_dep = reader["name_dep"].ToString();
                        if (nzp_dep != 0 && !String.IsNullOrEmpty(name_dep)) lst.Add(new Returns() { tag = nzp_dep, text = name_dep, sql_error = "dep_types" });
                    }
            }
            catch (Exception ex)
            {
                ret.tag = -1;
                ret.text = ex.Message;
                ret.result = false;
                lst = new List<Returns>();
                MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
            }
            finally { conn.Close(); }

            return ret;
        }

        public List<Service> LoadServicesBySupplier(Service finder, out Returns ret)
        {
            List<Service> listServices = new List<Service>();
            #region Проверка входных параметров
            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Не определен пользователь");
                return listServices;
            }
            #endregion
            
              ret = Utils.InitReturns();
            #region соединение
            string connectionString = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;
            #endregion
            foreach (_Point p in Points.PointList)
            {
                var sqlBuilder = new StringBuilder();               
                sqlBuilder.Append("WITH serv_num as (select nzp_serv from " + p.pref + "_kernel.l_foss where nzp_supp=" + finder.nzp_supp + ") ");
                sqlBuilder.Append("select distinct s.nzp_serv, s.service_name from " + Points.Pref + "_kernel.services s, serv_num sn where s.nzp_serv=sn.nzp_serv");
                string sql = sqlBuilder.ToString();
                try
                {
                    IDataReader reader;
                    ret = ExecRead(conn_db, out reader, sql, true);
                    if (!ret.result)
                    {
                        return listServices;
                    }
                    while (reader.Read())
                    {
                        int parsed_nzp_serv;
                        string srv_name="";

                        if (reader["nzp_serv"] == DBNull.Value) continue;
                        // Преобразование nzp_serv в тип int
                            if (!Int32.TryParse(reader["nzp_serv"].ToString(), out parsed_nzp_serv))
                            {
                                ret.result = false;
                                ret.text = "Ошибка преобразования nzp_serv в тип int в методе LoadServicesBySupplier()";
                                return listServices;
                            }
                       
                        if (reader["service_name"] != DBNull.Value) srv_name = reader["service_name"].ToString();
                        // Игнорируем повторяющиеся услуги
                        if (listServices.Count(srv => srv.nzp_serv == parsed_nzp_serv) != 0) continue;
                        // Добавляем только те услуги, которых нет в списке
                        listServices.Add(new Service{nzp_serv=parsed_nzp_serv, service_name = srv_name});
                    }
                }
                catch (Exception ex)
                {
                    ret.tag = -1;
                    ret.text = ex.Message;
                    ret.result = false;
                    MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                    break;
                }
                finally
                {
                    conn_db.Close();
                }
            }
            return listServices;
        }

        public List<Service> LoadServicesAndSuppliersForMustCalcLS(Service finder, out Returns ret)
        {
            List<Service> listServices = new List<Service>();
            #region Проверка входных параметров
            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Не определен пользователь");
                return listServices;
            }
            #endregion

            ret = Utils.InitReturns();
            #region соединение
            string connectionString = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;
            #endregion

            string where_is_actual = "";
            if (finder.is_actual == 100) where_is_actual = " ";
            else where_is_actual = " and t.is_actual <> 100 ";

            string sql = "";
            if (finder.one_actual_supp)
            {
                sql = "select distinct  t.nzp_supp, su.name_supp from " + finder.pref + "_data.tarif t, " + finder.pref + "_kernel.supplier su " +
"WHERE t.nzp_kvar=" + finder.nzp_kvar + " and t.nzp_supp=su.nzp_supp and t.nzp_serv= " + finder.nzp_serv + where_is_actual +  "  order by nzp_supp";
            }
            else
            {
                sql = "select distinct t.nzp_serv, s.service_name  from " + finder.pref + "_data.tarif t, " + Points.Pref + "_kernel.services s " +
"WHERE t.nzp_kvar=" + finder.nzp_kvar + " and t.nzp_serv=s.nzp_serv " + where_is_actual + " order by nzp_serv";
            }

            try
            {
                IDataReader reader;
                ret = ExecRead(conn_db, out reader, sql, true);
                if (!ret.result)
                {
                    return listServices;
                }
                while (reader.Read())
                {
                    Service service = new Service();
                    if (finder.one_actual_supp)
                    {
                        if (reader["nzp_supp"] != DBNull.Value) service.nzp_supp = (int) reader["nzp_supp"];
                        if (reader["name_supp"] != DBNull.Value) service.name_supp = reader["name_supp"].ToString();
                    }
                    else
                    {
                        if (reader["nzp_serv"] != DBNull.Value) service.nzp_serv = (int)reader["nzp_serv"];
                        if (reader["service_name"] != DBNull.Value) service.service_name = reader["service_name"].ToString();  
                    }
                    listServices.Add(service);
                }
            }
            catch (Exception ex)
            {
                ret.tag = -1;
                ret.text = ex.Message;
                ret.result = false;
                MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);           
            }
            finally
            {
                conn_db.Close();
            }
            
            return listServices;
        }

        public double[] CountGilsForCalc(Service finder, out Returns ret)
        {
            double[] countGil={0,0,0};
            #region Проверка входных параметров
            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Не определен пользователь");
                return countGil;
            }
            #endregion

            ret = Utils.InitReturns();
            #region соединение
            string connectionString = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;
            #endregion

            string sql = "select val1, val3, val5 from " + finder.pref + "_charge_"+(finder.year_-2000).ToString("00")+".gil_" + finder.month_.ToString("00") +
                         " t " +
                         "WHERE stek=3 and dat_charge IS NULL and t.nzp_kvar=" + finder.nzp_kvar;   
            try
            {
                IDataReader reader;
                ret = ExecRead(conn_db, out reader, sql, true);
                if (!ret.result)
                {
                    return countGil;
                }
                while (reader.Read())
                {
                    if (reader["val1"] != DBNull.Value) countGil[0] = Convert.ToDouble(reader["val1"]);
                    if (reader["val3"] != DBNull.Value) countGil[1] = Convert.ToDouble(reader["val3"]);
                    if (reader["val5"] != DBNull.Value) countGil[2] = Convert.ToDouble(reader["val5"]);
                }
            }
            catch (Exception ex)
            {
                ret.tag = -1;
                ret.text = ex.Message;
                ret.result = false;
                MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
            }
            finally
            {
                conn_db.Close();
            }

            return countGil;
        }
    }
}
