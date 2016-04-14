using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.SzExchange.Unload
{
    class PrepareTables : DataBaseHeadServer
    {
        /// <summary>
        /// Подготовка временныйх таблиц для выгрузки
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="conn_db"></param>
        /// <param name="dat_po"></param>
        /// <param name="dats"></param>
        /// <returns></returns>
        public Returns PrepareTempTable(FilesImported finder, IDbConnection conn_db, string dat_po, string dats)
        {
            Returns ret = Utils.InitReturns();
            string sAliasm = "";
            string month;
            string sql;
            string sChargeAlias = "_charge_" + dats.Substring(8, 2); // с учетом, что дата приходит в формате 01.01.2001

            Utilits u = new Utilits();
            int nzpBadGroup = u.ExcludeLs(finder, conn_db);
            int nzp_group = -1;
         
            bool stop_flag = false;                             //заглушка, нужно обрабатывать, откуда приходит - неизвестно
            bool is_nch = u.UnloadForChelny(finder, conn_db);   //признак выгрузки в Челны
            bool is_kzn = u.UnloadForKazan(finder,conn_db);     // признак выгрузки в  Казань



            //Загрузка справочника с территоряими
            u.LoadArea(finder, conn_db);

            #region Выбираем наименование улицы, района, города

            //try
            //{
                sql = "DROP TABLE t_raj";

                DBManager.ExecSQL(conn_db, sql, false);

                sql = " SELECT ulica, ulicareg, rajon, town, nzp_ul, r.nzp_raj, t.nzp_town " +
                      " INTO TEMP t_raj " +
                      " FROM " + finder.bank + DBManager.sDataAliasRest
                      + "s_ulica s, " + Points.Pref + DBManager.sDataAliasRest + "s_town t, " +
                      Points.Pref + DBManager.sDataAliasRest + "s_rajon r " +
                      " WHERE s.nzp_raj = r.nzp_raj " +
                      " AND r.nzp_town = t.nzp_town ";
                DBManager.ExecSQL(conn_db, sql, true);

                if (stop_flag)
                {
                    // Обработать: if stop_flag=true then exit;
                }


            #endregion Выбираем наименование улицы, района, города

                #region Выбираем домохозяйства

                sql = " SELECT fio, num_ls, nkvar, nkvar_n, ndom, nkor, ulica, ulicareg, rajon, town, " +
                      " nzp_kvar, d.nzp_dom, s.nzp_ul, s.nzp_raj, s.nzp_town, area, typehos, pkod " +
                      " INTO TEMP t_adres1 " +
                      " FROM " + Points.Pref + DBManager.sDataAliasRest + "kvar k, " + Points.Pref +
                      DBManager.sDataAliasRest +
                      "dom d, t_raj s, " +
                      " t_area sa " +
                      " WHERE k.nzp_dom = d.nzp_dom " +
                      " AND d.nzp_ul = s.nzp_ul " +
                      " AND num_ls > 0 " +
                      " AND k.nzp_area = sa.nzp_area " +
                      " AND k.typek = 1 "; // для postgres
                if (nzp_group > 0)
                {
                    sql = sql + " AND k.nzp_kvar IN ( " +
                          " SELECT nzp as nzp_kvar from " + finder.bank + DBManager.sDataAliasRest + "link_group " +
                          " WHERE nzp_group=" + nzp_group + ")";
                    DBManager.ExecSQL(conn_db, sql, true);
                }

                //if (is_dop_unl)  // чтоэто?!
                //{
                //    sql = sql + " AND k.num_ls in ( " +
                //                  " select num_ls from " + finder.bank +DBManager.sDataAliasRest + " sz_must_unl "+
                //                  " where dat_calc=' " + dats + "' and dat_charge=' " + dats+"') ";//BaseDatS
                //DBManager.ExecSQL(conn_db, sql, true);
                //}

                //sqlAdres = sqlAdres + " INTO TEMP t_adres1 ";// для informix
                //DBManager.ExecSQL(conn_db, sql, true);

                DBManager.ExecSQL(conn_db, sql, true);
                //if (!ret.result)
                //{
                //    return ret;
                //}
                MonitorLog.WriteLog("Окончание загрузки адресов", MonitorLog.typelog.Info, true);

                #endregion Выбираем домохозяйства

                //Загрузка параметров
                u.LoadParamsSz(finder, conn_db, nzp_group, dat_po);

                
                //Получаем строку с допустимыми льготами
                u.GetLgotaString(finder,conn_db);

                #region Загрузка начислений

                MonitorLog.WriteLog("Старт загрузки начислений", MonitorLog.typelog.Info, true);

                //Выбираем только открытые лицевые счета
                sql =
                    " CREATE TEMP TABLE t_pere( " +
                    " nzp_kvar INTEGER, " +
                    " num_ls INTEGER, " +
                    " nzp_serv INTEGER, " +
                    " nzp_measure INTEGER, " +
                    " nzp_supp INTEGER, " +
                    " tarif DECIMAL(14,3), " +
                    " tarif_f DECIMAL(14,3), " +
                    " isdel INTEGER, " +
                    " dat_uchet DATE, " +
                    " sum_subsidy DECIMAL(14,2), " +
                    " sum_subsidy_p DECIMAL(14,2), " +
                    " sum_tarif_sn_f DECIMAL(14,2), " +
                    " sum_tarif_sn_f_p DECIMAL(14,2), " +
                    " sum_tarif_eot DECIMAL(14,2), " +
                    " sum_lgota DECIMAL(14,2), " +
                    " sum_lgota_p DECIMAL(14,2), " +
                    " sum_smo DECIMAL(14,2), " +
                    " sum_smo_p DECIMAL(14,2), " +
                    " sum_tarif_eot_p DECIMAL(14,2), " +
                    " is_device INTEGER)";
                DBManager.ExecSQL(conn_db, sql, true);

                sql = " CREATE TEMP TABLE tmp_lgcharge2 (" +
                      " nzp_kvar  integer, " +
                      " nzp_serv  INTEGER," +
                      " nzp_supp  integer," +
                      " nzp_gilec integer," +
                      " is_family integer," +
                      " nzp_lgota integer," +
                      " kod_cz Decimal(13,0)," +
                      " nzp_law integer, " +
                      " nzp_bud integer, " +
                      " cz_law Decimal(13,0)," +
                      " cz_lgota Decimal(13,0)," +
                      " delta_lgota DECIMAL(14,2)," +
                      " dat_charge DATE)";
                DBManager.ExecSQL(conn_db, sql, true);

                sql =
                    " SELECT * INTO TEMP t_adres FROM t_adres1 " +
                    " WHERE nzp_kvar IN ( " +
                    "  SELECT nzp_kvar " +
                    "  FROM " + finder.bank + sChargeAlias + ".charge_" + dats.Substring(3, 2) +
                    "  WHERE nzp_serv > 1" +
                    "  AND dat_charge is null group by 1) " +
                    "  AND nzp_kvar NOT IN " +
                    "   (SELECT nzp " +
                    "   FROM " + finder.bank + DBManager.sDataAliasRest + "prm_3 " +
                    "   WHERE nzp_prm = 51 " +
                    "   AND val_prm = '3' " +
                    "   AND is_actual <> 100 " +
                    "   AND dat_s <='" + dat_po + "'  " +
                    "   AND dat_po>='" + dats + "')";

                if (is_nch)
                {
                    sql = sql + " AND nzp_kvar NOT IN (SELECT nzp + " + " INTO TEMP t_adres " +
                          " FROM " + finder.bank + DBManager.sDataAliasRest + " prm_3 " +
                          " WHERE nzp_prm=51 AND val_prm='2' AND is_actual <> 100 " +
                          " AND dat_s <= '01.01.2008' AND dat_po >= '" + dats + "')";
                    DBManager.ExecSQL(conn_db, sql, true);
                }

                sql = "DROP TABLE t_adres1";
                DBManager.ExecSQL(conn_db, sql, false);

                sql = " CREATE INDEX ix_tm_01 ON t_adres(num_ls) ";
                DBManager.ExecSQL(conn_db, sql, true);

                sql = " CREATE INDEX ix_tm_0212 ON t_adres(nzp_kvar) ";
                DBManager.ExecSQL(conn_db, sql, true);

                sql = " ANALYZE t_adres ";
                DBManager.ExecSQL(conn_db, sql, true);

                sql =
                    " SELECT nzp_kvar," + finder.bank + DBManager.sDataAliasRest + "get_kol_gil('" + dats + "', '" +
                    dat_po +
                    "', 15, nzp_kvar) as kol_gil " +
                    " INTO TEMP t_gil" +
                    " FROM t_adres ";
                DBManager.ExecSQL(conn_db, sql, true);

                sql = " CREATE INDEX ix_tm_06 ON t_gil(nzp_kvar) ";
                DBManager.ExecSQL(conn_db, sql, true);

                sql = " ANALYZE t_gil ";
                DBManager.ExecSQL(conn_db, sql, true);
            //}
            //catch (Exception ex)
            //{
            //    MonitorLog.WriteLog("Ошибка выборки адресного пространства: " + ex.Message, MonitorLog.typelog.Error, true);
            //    return new Returns(false, "Ошибка выборки адресного пространства", -1);
            //}

                #endregion Загрузка начислений

            // Для Нижнекамска заменяем услугу
            NizhnServ nz = new NizhnServ();
            nz.ReplaceServForNizhn(finder, conn_db, dats, stop_flag, sAliasm);


            #region Выборка перерасчетов

            sql =
                  " CREATE TEMP TABLE t_pere1 (" +
                  " nzp_kvar integer, " +
                  " num_ls integer, " +
                  " nzp_serv integer, " +
                  " nzp_supp integer," +
                  " tarif Decimal(14,3), " +
                  " tarif_f Decimal(14,3), " +
                  " nzp_frm integer, " +
                  " is_device integer, " +
                  " isdel integer, " +
                  " dat_uchet Date, " +
                  " sum_subsidy Decimal(14,2)," +
                  " sum_subsidy_p Decimal(14,2)," +
                  " sum_tarif_sn_f_p Decimal(14,2)," +
                  " sum_tarif_sn_f Decimal(14,2)," +
                  " sum_tarif_eot Decimal(14,2)," +
                  " sum_lgota Decimal(14,2)," +
                  " sum_lgota_p Decimal(14,2)," +
                  " sum_smo Decimal(14,2)," +
                  " sum_smo_p Decimal(14,2)," +
                  " sum_tarif_eot_p Decimal(14,2))";
            DBManager.ExecSQL(conn_db, sql, true);

            sql =
                " SELECT  month_, dbname, dbserver, b.year_ " +
                " FROM " + finder.bank + sChargeAlias + "lnk_charge_" + dats.Substring(3, 2) + " b, " +
                " s_baselist a, t_adres c, logtodb ld, s_logicdblist sl " +
                " WHERE a.yearr = b.year_ " +
                " AND b.nzp_kvar = c.nzp_kvar " +
                " AND yearr >= 2005 " +
                " AND ld.nzp_bl = a.nzp_bl " +
                " AND sl.nzp_ldb = ld.nzp_ldb " +
                // " AND sl.ldbname = " + LogicDbNAme +  // ЧТО ТАКОЕ LogicDbNAme??
                " AND idtype = 1";

            if (u.UnloadForChelny(finder, conn_db))
            {
                sql += sql +
                    " AND yearr >= 2010";
            }
            sql += sql +
                   " GROUP BY 1,2,3,4 ORDER BY year_, month_";

            DataTable dtb = ClassDBUtils.OpenSQL(sql, conn_db, ClassDBUtils.ExecMode.Exception).GetData();
            if (dtb.Rows.Count != 0)
            {
                foreach (DataRow rr in dtb.Rows)
                {
                    //if (rr["dbserver"].ToString() != "")                                          //   ?????????
                    //{
                    //    sAliasm = rr["dbname"].ToString() + "@" + rr["dbserver"].ToString();
                    //}
                    //else
                    //{
                    //    sAliasm = rr["dbname"].ToString();
                    //}

                    //sAliasm += ":";

                    //Выборка перерасчета за месяц\год ..
                    month = String.Format("{00}", rr["month_"]);
                    MonitorLog.WriteLog("Выборка перерасчета за " + month + "." + rr["year_"].ToString() + "г.", MonitorLog.typelog.Info, true);

                    if (stop_flag)
                    {
                        //ВЫХОД
                    }

                    if (is_kzn)  // ??!!! СИНТАКСИС ЗАПРОСА
                    {
                        sql =
                            " INSERT INTO t_pere1 " +
                            " SELECT b.nzp_kvar, b.num_ls, a.nzp_serv, nzp_supp, tarif, tarif as tarif_f, nzp_frm, a.is_device, isdel, '01." +
                            month + "." + rr["year_"].ToString() + "', " +
                            " sum(0) as sum_subsidy, sum(0) as sum_subsidy_p, sum(0) as sum_tarif_sn_f_p, " +
                            " sum(0) as sum_tarif_sn, sum(sum_tarif) as sum_tarif_eot, sum(sum_lgota) as sum_lgota, " +
                            " sum(sum_lgota_p) as sum_lgota_p, sum(0) as sum_smo, sum(0) as sum_smo_p, " +
                            " sum(sum_tarif_p) as sum_tarif_eot_p " +
                            " FROM " + finder.bank + "_public.charge_" + month + "a, t_adres b" +
                            " WHERE a.num_ls = b.num_ls " +
                            " AND a.nzp_serv > 1" +
                            " AND dat_charge = '28." + dats.Substring(3, 7) + "' " +
                            " GROUP BY 1,2,3,4,5,6,7,8,9";
                        DBManager.ExecSQL(conn_db, sql, true);
                    }
                    else
                    {
                        sql =
                            " INSERT INTO t_pere1 " +
                            " SELECT b.nzp_kvar, b.num_ls, a.nzp_serv, nzp_supp, tarif, tarif_f, nzp_frm, a.is_device, isdel, '01." +
                            month + "." + rr["year_"].ToString() + "', " +
                            " sum(sum_subsidy) as sum_subsidy, " +
                            " sum(sum_subsidy_p) as sum_subsidy_p, " +
                            " sum(sum_tarif_sn_f_p) as sum_tarif_sn_f_p, " +
                            " sum(sum_tarif_sn_f) as sum_tarif_sn, " +
                            " sum(sum_tarif_eot) as sum_tarif_eot, " +
                            " sum(sum_lgota) as sum_lgota, " +
                            " sum(sum_lgota_p) as sum_lgota_p, " +
                            " sum(sum_smo) as sum_smo, " +
                            " sum(sum_smo_p) as sum_smo_p, " +
                            " sum(sum_tarif_eot_p) as sum_tarif_eot_p " +
                            " FROM " + finder.bank + "_public.charge_" + month + "a, t_adres b" +
                            " WHERE a.num_ls = b.num_ls " +
                            " AND a.nzp_serv > 1" +
                            " AND dat_charge = '28." + dats.Substring(3, 7) + "' " +
                            " GROUP BY 1,2,3,4,5,6,7,8,9";
                        DBManager.ExecSQL(conn_db, sql, true);
                    }

                }

                MonitorLog.WriteLog("Группировка перерасчетов..", MonitorLog.typelog.Info, true);

            }
            #endregion Выборка перерасчетов

            return ret;
        }
    }
}
