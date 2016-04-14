using System;
using System.Data;
using STCLINE.KP50.Global;

using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.DataBase
{
    public partial class ExcelRep
    {
        //        public DataTable GetSpravSodergOdn9(Prm prm, out Returns ret, string nzpUser)
        //        {
        //            //MonitorLog.WriteLog("ExcelReport start ifmx", MonitorLog.typelog.Info, true);

        //            #region Подключение к БД
        //            IDbConnection connWeb = GetConnection(Constants.cons_Webdata);//new IDbConnection(Constants.cons_Webdata);


        //            IDataReader reader;

        //            ret = OpenDb(connWeb, true);
        //            if (!ret.result)
        //            {
        //                MonitorLog.WriteLog("ExcelReport : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
        //                return null;
        //            }
        //            string tXXSpls = DBManager.GetFullBaseName(connWeb) + DBManager.tableDelimiter + "t" + nzpUser + "_spls";
        //            connWeb.Close();


        //            IDbConnection connDb = GetConnection(Constants.cons_Kernel);//new IDbConnection(Constants.cons_Webdata);

        //            ret = OpenDb(connDb, true);
        //            if (!ret.result)
        //            {
        //                MonitorLog.WriteLog("ExcelReport : Ошибка при открытии соединения с локальной БД ", MonitorLog.typelog.Error, true);
        //                return null;
        //            }

        //            #endregion



        //            #region Выборка по локальным банкам

        //            ExecSQL(connDb, "drop table t_svod", false);


        //            string sql = " create temp table t_svod( " +
        //                         " nzp_dom integer," +
        //                         " nzp_kvar integer," +
        //                         " nzp_geu integer," +
        //                         " name_frm char(100)," +
        //                         " nzp_measure integer, " +
        //                         " count_gil integer," +
        //                         " rsum_tarif_all " + DBManager.sDecimalType + "(14,2), " +
        //                         " rsum_tarif " + DBManager.sDecimalType + "(14,2), " +
        //                         " sum_odn " + DBManager.sDecimalType + "(14,2), " +
        //                         " tarif " + DBManager.sDecimalType + "(14,3), " +
        //                         " c_calc_kub " + DBManager.sDecimalType + "(14,5), " +
        //                         " c_calc_gkal " + DBManager.sDecimalType + "(14,5), " +
        //                         " c_calc_kub_odn " + DBManager.sDecimalType + "(14,5), " +
        //                         " c_calc_gkal_odn " + DBManager.sDecimalType + "(18,5), " +
        //                         " sum_nedop " + DBManager.sDecimalType + "(14,2), " +
        //                         " reval_k " + DBManager.sDecimalType + "(14,2)," +
        //                         " reval_d " + DBManager.sDecimalType + "(14,2), " +
        //                         " c_nedop_kub " + DBManager.sDecimalType + "(18,5)," +
        //                         " c_nedop_gkal " + DBManager.sDecimalType + "(18,5)," +
        //                         " c_reval_k_kub " + DBManager.sDecimalType + "(18,5)," +
        //                         " c_reval_k_gkal " + DBManager.sDecimalType + "(18,5)," +
        //                         " c_reval_d_kub " + DBManager.sDecimalType + "(18,5), " +
        //                         " c_reval_d_gkal " + DBManager.sDecimalType + "(18,5), " +
        //                         " pl_kvar " + DBManager.sDecimalType + "(14,4), " +
        //                         " sum_charge " + DBManager.sDecimalType + "(14,2))" + DBManager.sUnlogTempTable;

        //            ExecSQL(connDb, sql, true);


        //            ExecSQL(connDb, "drop table sel_kvar_10", false);

        //            sql = " Create temp table sel_kvar_10(nzp_dom integer, " +
        //                  " nzp_geu integer, nzp_kvar integer, pref char(10))" + DBManager.sUnlogTempTable;
        //            ExecSQL(connDb, sql, true);

        //            sql = "insert into sel_kvar_10 (nzp_dom, nzp_geu, nzp_kvar, pref)" +
        //                  " select nzp_dom, nzp_geu, nzp_kvar, pref " +
        //                  " from " + tXXSpls + " k ";
        //            ExecSQL(connDb, sql, true);


        //            sql = " select pref " +
        //                  " from  sel_kvar_10 group by 1";
        //            ret = ExecRead(connDb, out reader, sql, true);
        //            if (!ret.result)
        //            {
        //                connDb.Close();
        //                return null;
        //            }

        //            while (reader.Read())
        //            {
        //                if (reader["pref"] != null)
        //                {
        //                    string pref = Convert.ToString(reader["pref"]).Trim();
        //                    string sChargeAlias = pref + "_charge_" + (prm.year_ - 2000).ToString("00");
        //                    string tableCalcGku = pref + "_charge_" +
        //                                          (prm.year_ - 2000).ToString("00") + DBManager.tableDelimiter + "calc_gku_"
        //                                          + prm.month_.ToString("00");

        //                    ExecSQL(connDb, " drop table sel_kvar10", false);

        //                    #region Заполнение sel_kvar10

        //                    sql = " create temp table sel_kvar10 ( " +
        //                          " nzp_kvar integer," +
        //                          " nzp_dom integer," +
        //                          " nzp_geu integer," +
        //                          " isol integer default 1," +
        //                          " pl_kvar "+DBManager.sDecimalType+"(14,2)) "+DBManager.sUnlogTempTable;
        //                    ExecSQL(connDb, sql, true);


        //                    sql = " insert into sel_kvar10(nzp_dom, nzp_geu, nzp_kvar)" +
        //                          " select nzp_dom, nzp_geu, nzp_kvar " +
        //                          " from sel_kvar_10 k " +
        //                          " where pref='" + pref + "'";

        //                    if (prm.has_pu == "2")
        //                        sql = sql + " and 0 < (select count(*)  " +
        //                              " from " + sChargeAlias + DBManager.tableDelimiter + "counters_" +
        //                              prm.month_.ToString("00") + " d " +
        //                              " where d.stek=3 and d.nzp_type=1 and d.cnt_stage>0 and d.nzp_serv=" + prm.nzp_serv +
        //                              " and k.nzp_dom=d.nzp_dom ) ";
        //                    if (prm.has_pu == "3")
        //                        sql = sql + " and 0 = (select count(*)  " +
        //                              " from " + sChargeAlias + DBManager.tableDelimiter + "counters_" +
        //                              prm.month_.ToString("00") + " d" +
        //                              " where d.stek=3 and d.nzp_type=1 and d.cnt_stage>0 and d.nzp_serv=" + prm.nzp_serv +
        //                              " and k.nzp_dom=d.nzp_dom ) ";
        //                    ExecSQL(connDb, sql, true);


        //                    sql = "create index ix_tmpk_02 on sel_kvar10(nzp_kvar)";
        //                    ExecSQL(connDb, sql, true);

        //                    ExecSQL(connDb, sUpdStat+" sel_kvar10 ", true);




        //                    sql = " update sel_kvar10 set pl_kvar=(select max(val_prm"+DBManager.sConvToNum+") " +
        //                          " from " + pref + DBManager.sDataAliasRest + "prm_1 where nzp_prm=4 and is_actual=1 " +
        //                          " and dat_s<=today and dat_po>=today and sel_kvar10.nzp_kvar=nzp)";
        //                    ExecSQL(connDb, sql, true);



        //                    sql = " update sel_kvar10 set isol=2 where nzp_kvar in (select nzp " +
        //                          " from " + pref +  DBManager.sDataAliasRest+ "prm_1 " +
        //                          " where nzp_prm=3 and is_actual=1 and val_prm='2' " +
        //                          " and dat_s<=" + DBManager.sCurDate + " and dat_po>=" + DBManager.sCurDate + " )";
        //                    ExecSQL(connDb, sql, true);

        //                    #endregion


        //                    ExecSQL(connDb, "drop table t1", false);
        //                    #region Добавляем основную услугу

        //                     sql = "create temp table t1 (" +
        //                          " nzp_geu integer, " +
        //                          " nzp_dom integer, " +
        //                          " nzp_kvar integer, " +
        //                          " nzp_frm integer, " +
        //                          " nzp_measure integer, " +
        //                          " nzp_serv integer, " +
        //                          " count_gil integer, " +
        //                          " pl_kvar " + DBManager.sDecimalType + "(14,2), " +
        //                          " otop_koef " + DBManager.sDecimalType + "(14,6), " +
        //                          " norma " + DBManager.sDecimalType + "(14,2), " +
        //                          " tarif " + DBManager.sDecimalType + "(14,2), " +
        //                          " tarif_gkal " + DBManager.sDecimalType + "(14,2), " +
        //                          " tarif_hvs " + DBManager.sDecimalType + "(14,2), " +
        //                          " sum_charge " + DBManager.sDecimalType + "(14,2), " +
        //                          " rsum_tarif " + DBManager.sDecimalType + "(14,2), " +
        //                          " sum_nedop " + DBManager.sDecimalType + "(14,2), " +
        //                          " sum_nedop_cor " + DBManager.sDecimalType + "(14,2) default 0, " +
        //                          " reval_k " + DBManager.sDecimalType + "(14,2), " +
        //                          " reval_d " + DBManager.sDecimalType + "(14,2), " +
        //                          " c_calc " + DBManager.sDecimalType + "(14,5)) " + DBManager.sUnlogTempTable;
        //                    ExecSQL(connDb, sql, true);


        //                    sql =" insert into t1 (nzp_geu, nzp_dom, nzp_kvar, nzp_serv, nzp_frm, " +
        //                          " pl_kvar, nzp_measure, tarif, tarif_hvs, count_gil," +
        //                          " sum_charge, rsum_tarif, sum_nedop, reval_k, reval_d, c_calc) " +
        //                          " select nzp_geu, nzp_dom, a.nzp_kvar, nzp_serv, nzp_frm, " +
        //                          " sum(0) as pl_kvar, " +
        //                          " sum(0) as nzp_measure," +
        //                          " max(tarif) as tarif," +
        //                          " sum(0) as tarif_hvs, " +
        //                          " max(0) as count_gil," +
        //                          " sum(sum_charge) as sum_charge, " +
        //                          " sum(rsum_tarif) as rsum_tarif, sum(sum_nedop) as sum_nedop," +
        //                          " sum(case when real_charge<0 then real_charge else 0 end) +" +
        //                          " sum(case when reval<0 then reval else 0 end) as reval_k," +
        //                          " sum(case when real_charge>0 then real_charge else 0 end) +" +
        //                          " sum(case when reval>0 then reval else 0 end) as reval_d," +
        //                          " sum(0) as c_calc" +
        //                          " from  " + sChargeAlias + DBManager.tableDelimiter + "charge_" +
        //                          prm.month_.ToString("00") + " a, sel_kvar10 b" +
        //                          " where nzp_serv in (9, 14, 513, 514) " +
        //                          " and a.nzp_kvar=b.nzp_kvar " +
        //                          " and abs(rsum_tarif)+abs(sum_nedop)+abs(real_charge)+ abs(reval) +" +
        //                          " abs(tarif) + abs(sum_charge)>0.001"+
        //                          " and dat_charge is null ";
        //                    if (prm.nzp_key > -1) //Добавляем поставщика
        //                    {
        //                        sql = sql + " and a.nzp_supp = " + prm.nzp_key;
        //                    }
        //                    sql = sql + " group by 1,2,3,4,5";

        //                    ExecSQL(connDb, sql, true);


        //                    sql = "create index ix_tmpk_03 on t1(nzp_kvar,nzp_serv)";
        //                    ExecSQL(connDb, sql, true);
        //                    sql = "create index ix_tmpk_04 on t1(nzp_kvar)";
        //                    ExecSQL(connDb, sql, true);

        //                    ExecSQL(connDb, sUpdStat + " t1 ", true);



        //                    #region Жигулевск недопоставка как перекидка
        //                    sql = "update t1 set sum_nedop_cor = (select  " +
        //                          " -sum(sum_rcl) " +
        //                          " from " + sChargeAlias + DBManager.tableDelimiter + "perekidka a " +
        //                          " where a.nzp_kvar=t1.nzp_kvar and type_rcl = 101 and month_=" + prm.month_.ToString("00") +
        //                          " and abs(sum_rcl)>0.001 and nzp_serv in (9,14) and a.nzp_serv=t1.nzp_serv ";
        //                    if (prm.nzp_key > -1) //Добавляем поставщика
        //                    {
        //                        sql = sql + " and a.nzp_supp = " + prm.nzp_key;
        //                    }
        //                    sql = sql + ") where reval_k<0 ";
        //                    ExecSQL(connDb, sql, true);

        //                    sql = "update t1 set sum_nedop = sum_nedop + " + DBManager.sNvlWord + "(sum_nedop_cor,0)";
        //                    ExecSQL(connDb, sql, true);
        //                    sql = "update t1 set reval_k = reval_k + " + DBManager.sNvlWord + "(sum_nedop_cor,0)";
        //                    ExecSQL(connDb, sql, true);
        //                    #endregion


        //                    #region Проставляем площадь и количество жильцов


        //                    sql = " update t1 set pl_kvar = (select max(squ) " +
        //                          " from  " + tableCalcGku + " d " +
        //                          " where d.nzp_kvar=t1.nzp_kvar  and d.tarif>0 " +
        //                          " and 9 = d.nzp_serv ";
        //                    if (prm.nzp_key > -1) //Добавляем поставщика
        //                    {
        //                        sql = sql + " and d.nzp_supp = " + prm.nzp_key;
        //                    }
        //                    sql =sql +" )";
        //                    ExecSQL(connDb, sql, true);

        //                    sql = " update t1 set  count_gil = (select max(gil) " +
        //                          " from  " + tableCalcGku + " d " +
        //                          " where d.nzp_kvar=t1.nzp_kvar  and d.tarif>0 " +
        //                          " and 9 = d.nzp_serv ";
        //                    if (prm.nzp_key > -1) //Добавляем поставщика
        //                    {
        //                        sql = sql + " and d.nzp_supp = " + prm.nzp_key;
        //                    }
        //                    sql =sql +" )";
        //                    ExecSQL(connDb, sql, true);

        //                    sql = " update t1 set  norma = (select max(gil) " +
        //                        " from  " + tableCalcGku + " d " +
        //                        " where d.nzp_kvar=t1.nzp_kvar  and d.tarif>0 " +
        //                        " and 9 = d.nzp_serv ";
        //                    if (prm.nzp_key > -1) //Добавляем поставщика
        //                    {
        //                        sql = sql + " and d.nzp_supp = " + prm.nzp_key;
        //                    }
        //                    sql = sql + " )";

        //                    ExecSQL(connDb, sql, true);


        //                    sql = " update t1 set  norma = (select max(rash_norm_one/rsh2) " +
        //                      " from  " + tableCalcGku + " d " +
        //                      " where d.nzp_kvar=t1.nzp_kvar  " +
        //                      " and 9 = d.nzp_serv and rsh2>0 ";
        //                    if (prm.nzp_key > -1) //Добавляем поставщика
        //                    {
        //                        sql = sql + " and d.nzp_supp = " + prm.nzp_key;
        //                    }
        //                    sql = sql + " )";

        //                    ExecSQL(connDb, sql, true);

        //                    #endregion

        //                    //Проставляем расход по основным услугам

        //                    sql = " update t1 set c_calc = (select sum(valm+dlt_reval) " +
        //                          " from  " + tableCalcGku + " d " +
        //                          " where d.nzp_kvar=t1.nzp_kvar  and d.tarif>0 " +
        //                          " and t1.nzp_serv = d.nzp_serv ";

        //                    if (prm.nzp_key > -1) //Добавляем поставщика
        //                    {
        //                        sql = sql + " and d.nzp_supp = " + prm.nzp_key;
        //                    }
        //                    sql = sql + " ) where nzp_serv in (9,14) and tarif>0";
        //                    ExecSQL(connDb, sql, true);


        //                    //Проставляем расход по услугам ОДН Подогрев

        //                    sql = " update t1 set c_calc = (select "+
        //                    " sum(case when valm+dlt_reval>=0 and dop87<0 and dop87< -valm-dlt_reval then -valm-dlt_reval  "+
        //                    "          else dop87 end) "+
        //                    " from  " + tableCalcGku + " d " +
        //                    " where d.nzp_kvar=t1.nzp_kvar  and d.tarif>0  " +
        //                    " and 9 = d.nzp_serv ";

        //                    if (prm.nzp_key > -1) //Добавляем поставщика
        //                    {
        //                        sql = sql + " and d.nzp_supp = " + prm.nzp_key;
        //                    }
        //                    sql = sql +" ) where nzp_serv = 513  and tarif>0";
        //                    ExecSQL(connDb, sql, true);


        //                    //Проставляем расход по услугам ОДН Хвс для ГВС

        //                    sql = " update t1 set c_calc = (select " +
        //                          " sum(case when valm+dlt_reval>=0 and  dop87<0 and dop87< -valm-dlt_reval then -valm-dlt_reval  " +
        //                          "          else dop87 end) " +
        //                          " from  " + tableCalcGku + " d " +
        //                          " where d.nzp_kvar=t1.nzp_kvar  and d.tarif>0 " +
        //                          " and 14 = d.nzp_serv ";

        //                    if (prm.nzp_key > -1) //Добавляем поставщика
        //                    {
        //                        sql = sql + " and d.nzp_supp = " + prm.nzp_key;
        //                    }
        //                    sql = sql + " ) where nzp_serv = 514  and tarif>0";
        //                    ExecSQL(connDb, sql, true);



        //                    sql = " update t1 set pl_kvar = (select max(pl_kvar) " +
        //                          " from  sel_kvar10 d" +
        //                          " where d.nzp_kvar=t1.nzp_kvar  and isol=2 )" +
        //                          " where nzp_kvar in (select nzp_kvar from sel_kvar10 where isol=2) ";
        //                    ExecSQL(connDb, sql, true);



        //                    #region Выборка перерасчетов прошлого периода
        //                    ExecSQL(connDb, "drop table t_nedop", false);
        //                    sql = "Create temp table t_nedop (nzp_geu integer," +
        //                          " nzp_dom integer, " +
        //                          " nzp_kvar integer, " +
        //                          " month_s integer, " +
        //                          " month_po integer)" + DBManager.sUnlogTempTable;
        //                    ExecSQL(connDb, sql, true);
        //#warning PG
        //                    sql = " insert into t_nedop (nzp_geu,nzp_dom,nzp_kvar, month_s, month_po)" +
        //                          " select b.nzp_geu,b.nzp_dom,a.nzp_kvar, min(year(dat_s)*12+month(dat_s)) as month_s,  " +
        //                          " max(extract(year from dat_s)*12+month(dat_po)) as month_po" +
        //                          " from " + pref + DBManager.sDataAliasRest + "nedop_kvar a, sel_kvar10 b " +
        //                          " where a.nzp_kvar=b.nzp_kvar and month_calc='01." + prm.month_.ToString("00") + "." +
        //                          prm.year_.ToString("0000") + "'  " +
        //                          " group by 1,2,3";

        //                    ExecSQL(connDb, sql, true);


        //                    sql = " select month_, year_ " +
        //                          " from " + pref + "_charge_" + (prm.year_ - 2000).ToString("00") +
        //                          DBManager.tableDelimiter + "lnk_charge_" + prm.month_.ToString("00") + " b, t_nedop d " +
        //                          " where  b.nzp_kvar=d.nzp_kvar " +
        //                          " and year_*12+month_>=month_s and  year_*12+month_<=month_po " +
        //                          " group by 1,2";
        //                    IDataReader reader2;
        //                    ExecRead(connDb, out reader2, sql, true);

        //                    while (reader2.Read())
        //                    {
        //                        string sTmpAlias = pref + "_charge_" + (Int32.Parse(reader2["year_"].ToString()) - 2000).ToString("00");


        //                        sql =
        //                            " insert into t1 (nzp_geu, nzp_dom, nzp_kvar, nzp_frm, nzp_serv, tarif, sum_nedop, reval_k, reval_d)   " +
        //                            " select nzp_geu, nzp_dom, b.nzp_kvar, nzp_frm, nzp_serv, 0,  " +
        //                            " sum(sum_nedop-sum_nedop_p),  " +
        //                            " sum(case when (sum_nedop-sum_nedop_p)>0 " +
        //                            " then sum_nedop-sum_nedop_p else 0 end ) as reval_k," +
        //                            " sum(case when (sum_nedop-sum_nedop_p)>0 " +
        //                            " then 0 else sum_nedop-sum_nedop_p end ) as reval_d" +
        //                            " from " + sTmpAlias + DBManager.tableDelimiter +
        //                            "charge_" + Int32.Parse(reader2["month_"].ToString()).ToString("00") +" b, " +
        //                            "t_nedop d " +
        //                            " where  b.nzp_kvar=d.nzp_kvar and dat_charge = date('28." +
        //                            prm.month_.ToString("00") + "." + prm.year_ + "')" +
        //                            " and abs(sum_nedop)+abs(sum_nedop_p)>0.001" +
        //                            " and nzp_serv in (9, 14, 513, 514)";
        //                        if (prm.nzp_key > -1) //Добавляем поставщика
        //                        {
        //                            sql = sql + " and nzp_supp = " + prm.nzp_key;
        //                        }
        //                        sql = sql + " group by 1,2,3,4,5,6";
        //                        ExecSQL(connDb, sql, true);
        //                    }
        //                    reader2.Close();

        //                    ExecSQL(connDb, "drop table t_nedop", true);
        //                    #endregion
        //                    #endregion


        //                    ExecSQL(connDb, "drop table t2", false);

        //                    ExecSQL(connDb, DBManager.sUpdStat +" t1", true);

        //                    sql = " create temp table t2(nzp_dom integer, nzp_kvar integer, nzp_frm_gvs integer, " +
        //                          " tarif_gvs " + DBManager.sDecimalType + "(14,6)," +
        //                          " tarif_hvsgvs " + DBManager.sDecimalType + "(14,6)" +
        //                          ")" + DBManager.sUnlogTempTable;
        //                    ExecSQL(connDb, sql, true);

        //                    sql = " insert into t2(nzp_dom,nzp_kvar,nzp_frm_gvs, tarif_gvs, tarif_hvsgvs )" +
        //                          " select nzp_dom, nzp_kvar, max(case when nzp_serv=9 then nzp_frm else 0 end) as nzp_frm_gvs, " +
        //                          " max(case when nzp_serv=9 then tarif else 0 end) as tarif_gvs, " +
        //                          " max(case when nzp_serv=14 then tarif else 0 end) as tarif_hvsgvs " +
        //                          " from t1  where nzp_frm>0 " +
        //                          " group by 1,2";
        //                    ExecSQL(connDb, sql, true);



        //                    sql = " update t1 set " +
        //                          " nzp_frm = (select max(nzp_frm_gvs) " +
        //                          " from t2 where t1.nzp_kvar=t2.nzp_kvar)" +
        //                          " where nzp_kvar in (select nzp_kvar from t2)";
        //                    ExecSQL(connDb, sql, true);


        //                    sql = "  update t1 set tarif = (select max(tarif_gvs)" +
        //                          " from t2 where t1.nzp_kvar=t2.nzp_kvar) " +
        //                          " where nzp_kvar in (select nzp_kvar from t2) ";
        //                    ExecSQL(connDb, sql, true);

        //                    DataTable d1 = DBManager.ExecSQLToTable(connDb, "select unique name_frm from t1 a, zhig362_kernel:formuls f where  a.nzp_frm=f.nzp_frm");

        //                    sql = " update t1 set nzp_frm  = " + DBManager.sNvlWord + "((select max(nzp_frm) " +
        //                          " from " + pref + DBManager.sDataAliasRest + "tarif f " +
        //                          " where t1.nzp_kvar=f.nzp_kvar and is_actual=1 and f.nzp_serv=9 and dat_po=" +
        //                          " (select max(dat_po) " +
        //                          " from " + pref + DBManager.sDataAliasRest + "tarif t " +
        //                          " where t.nzp_kvar=f.nzp_kvar and t.nzp_serv=9 and t.is_actual=1 )),0) where nzp_frm=0 ";
        //                    ExecSQL(connDb, sql, true);

        //                    d1 = DBManager.ExecSQLToTable(connDb, "select unique name_frm from t1 a, zhig362_kernel:formuls f where  a.nzp_frm=f.nzp_frm");
        //                    sql = " update t1 set  tarif_hvs  = (select max(tarif_hvsgvs) " +
        //                          " from t2 where t1.nzp_kvar=t2.nzp_kvar) " +
        //                          " where nzp_serv in (14, 514) and nzp_kvar in (select nzp_kvar from t2) ";
        //                    ExecSQL(connDb, sql, true);

        //                    //Принудительно проставляем формулу для ГВС услуге ХВС для ГВС

        //                    sql = " update t1 set nzp_frm  = (select max(nzp_frm_gvs)  " +
        //                          " from t2 where t1.nzp_kvar=t2.nzp_kvar) " +
        //                          " where nzp_serv in (14, 514) and nzp_kvar in (select nzp_kvar from t2) ";
        //                    ExecSQL(connDb, sql, true);


        //                    ExecSQL(connDb, "drop table t2", false);



        //                    ExecSQL(connDb, " create index ixtmm_06 on t1(nzp_frm)", true);
        //                    ExecSQL(connDb, " create index ixtmm_08 on t1(nzp_dom)", true);
        //                    ExecSQL(connDb, DBManager.sUpdStat + " t1", true);





        //                    sql = " update t1 set nzp_measure= " +
        //                          DBManager.sNvlWord + "((select max(nzp_measure)" +
        //                          " from " + pref + DBManager.sKernelAliasRest + "formuls f " +
        //                          " where  t1.nzp_frm=f.nzp_frm),0) ";
        //                    ExecSQL(connDb, sql, true);


        //                    #region Устанавливаем тариф для перерасчетных строк или там где тариф равен 0
        //                    ExecSQL(connDb, "drop table tmp_tar", false);

        //                    sql = "create temp table tmp_tar( nzp_kvar integer, " +
        //                          " nzp_serv integer," +
        //                          " nzp_frm integer," +
        //                          " tarif_hvs " + DBManager.sDecimalType + "(14,2)," +
        //                          " tarif " + DBManager.sDecimalType + "(14,2))" + DBManager.sUnlogTempTable;
        //                    ExecSQL(connDb, sql, true);

        //                    sql = " insert into tmp_tar (nzp_kvar, nzp_serv, nzp_frm, tarif_hvs, tarif)" +
        //                          " select nzp_kvar, nzp_serv, nzp_frm, tarif_hvs, tarif   " +
        //                          " from t1 where tarif = 0 and nzp_frm=0 ";
        //                    ExecSQL(connDb, sql, true);


        //                    ExecSQL(connDb, " create index ixtmm_07 on tmp_tar(nzp_kvar, nzp_serv)", true);
        //                    ExecSQL(connDb, DBManager.sUpdStat + " t1", true);


        //                    sql = " update t1 set tarif =" +
        //                          DBManager.sNvlWord + "((select max(tmp_tar.tarif) " +
        //                          " from tmp_tar where t1.nzp_kvar=tmp_tar.nzp_kvar  " +
        //                          " and t1.nzp_serv=tmp_tar.nzp_serv),0) " +
        //                          " where nzp_serv in (9,513) and tarif=0";
        //                    ExecSQL(connDb, sql, true);

        //                    sql = " update t1 set tarif_hvs = " +
        //                          DBManager.sNvlWord + "((select max(tmp_tar.tarif_hvs) " +
        //                          " from tmp_tar " +
        //                          " where t1.nzp_kvar=tmp_tar.nzp_kvar  " +
        //                          " and t1.nzp_serv=tmp_tar.nzp_serv),0) " +
        //                          " where nzp_serv in (14,514) and tarif_hvs=0";
        //                    ExecSQL(connDb, sql, true);

        //                    ExecSQL(connDb, "drop table t_maxtarif", false);

        //                    sql = "create temp table t_maxtarif(nzp_dom integer, " +
        //                          " tarif " + DBManager.sDecimalType + "(14,2)," +
        //                          "tarif_hvs " + DBManager.sDecimalType + "(14,2))" + DBManager.sUnlogTempTable;
        //                    ExecSQL(connDb, sql, true);

        //                    sql = " insert into t_maxtarif(nzp_dom,  tarif,  tarif_hvs) " +
        //                          " select nzp_dom, max(tarif) as tarif, max(tarif_hvs) as tarif_hvs  " +
        //                          " from t1 " +
        //                          " group by 1 ";
        //                    ExecSQL(connDb, sql, true);


        //                    sql = " update t1 set tarif =" +
        //                          DBManager.sNvlWord + "((select max(t_maxtarif.tarif) " +
        //                          " from t_maxtarif where t1.nzp_dom=t_maxtarif.nzp_dom  " +
        //                          " ),0) " +
        //                          " where nzp_serv in (9,513) and tarif=0";
        //                    ExecSQL(connDb, sql, true);

        //                    sql = " update t1 set tarif =" +
        //                        DBManager.sNvlWord + "((select max(t_maxtarif.tarif) " +
        //                        " from t_maxtarif   " +
        //                        " ),0) " +
        //                        " where nzp_serv in (9,513) and tarif=0";
        //                    ExecSQL(connDb, sql, true);

        //                    sql = " update t1 set tarif =" +
        //                        DBManager.sNvlWord + "((select max(t_maxtarif.tarif) " +
        //                        " from t_maxtarif where t1.nzp_dom=t_maxtarif.nzp_dom  " +
        //                        " ),0) " +
        //                        " where tarif=0";
        //                    ExecSQL(connDb, sql, true);

        //                    sql = " update t1 set tarif =" +
        //                        DBManager.sNvlWord + "((select max(t_maxtarif.tarif) " +
        //                        " from t_maxtarif   " +
        //                        " ),0) " +
        //                        " where tarif=0";
        //                    ExecSQL(connDb, sql, true);



        //                    sql = " update t1 set tarif_hvs = " +
        //                          DBManager.sNvlWord + "((select max(t_maxtarif.tarif_hvs) " +
        //                          " from t_maxtarif where t1.nzp_dom=t_maxtarif.nzp_dom  " +
        //                          " ),0) " +
        //                          " where nzp_serv in (14,514) and tarif_hvs=0";
        //                    ExecSQL(connDb, sql, true);

        //                    ExecSQL(connDb, "drop table tmp_tar", true);
        //                    ExecSQL(connDb, "drop table t_maxtarif", true);

        //                    #endregion

        //                    sql = " update t1 set nzp_dom = (select a.nzp_dom_base " +
        //                          " from " + pref + DBManager.sDataAliasRest + "link_dom_lit a" +
        //                          " where a.nzp_dom=t1.nzp_dom) " +
        //                          " where nzp_dom in (select nzp_dom from " + pref +
        //                          DBManager.sDataAliasRest +"link_dom_lit)";
        //                    ExecSQL(connDb, sql, true);


        //                    #region Простановка норматива по ГВС
        //                    ExecSQL(connDb, "drop table t_norm", false);

        //                    sql = "create temp table t_norm(nzp_dom integer, " +
        //                          " norma " + DBManager.sDecimalType + "(14,2))" + DBManager.sUnlogTempTable;
        //                    ExecSQL(connDb, sql, true);

        //                    sql = " insert into t_norm(nzp_dom,  norma) " +
        //                          " select nzp_dom, max(norma) as norma  " +
        //                          " from t1 " +
        //                          " group by 1 ";
        //                    ExecSQL(connDb, sql, true);


        //                    sql = " update t1 set norma =" +DBManager.sNvlWord + "((select norma " +
        //                          " from t_norm where t1.nzp_dom=t_norm.nzp_dom   ),0) ";
        //                    ExecSQL(connDb, sql, true);
        //                    ExecSQL(connDb, "drop table t_norm", true);
        //                    #endregion


        //                    //По всем

        //                    sql = " insert into t_svod(nzp_geu, nzp_dom, nzp_kvar, name_frm, nzp_measure, " +
        //                          " count_gil, rsum_tarif_all, rsum_tarif, sum_odn, tarif, " +
        //                          " sum_nedop, c_nedop_kub, c_nedop_gkal, reval_k, reval_d, " +
        //                          " c_reval_k_kub, c_reval_k_gkal, c_reval_d_kub, c_reval_d_gkal, sum_charge, " +
        //                          " c_calc_kub, c_calc_kub_odn, c_calc_gkal, c_calc_gkal_odn, pl_kvar )" +
        //                          " select nzp_geu,  a.nzp_dom,  a.nzp_kvar, " +
        //                          " trim("+DBManager.sNvlWord+"(name_frm,'Не определена формула'))" +
        //                          "||' норм.'||norma||' : '||a.tarif as name_frm, " +
        //                          " f.nzp_measure, max(count_gil), sum(rsum_tarif), " +
        //                          " sum(case when nzp_serv in (14,9) then rsum_tarif else 0 end) as rsum_tarif, " +
        //                          " sum(case when nzp_serv in (513,514) then rsum_tarif else 0 end) as sum_odn, " +
        //                          " max(a.tarif) as tarif, " +
        //                          " sum(sum_nedop) as sum_nedop,  " +
        //                          " sum(case when a.tarif_hvs>0 and nzp_serv in (14,514) then sum_nedop/a.tarif_hvs else 0 end) as c_nedop_kub, " +
        //                          " sum(case when a.tarif>0 and nzp_serv in (9, 513) then sum_nedop/a.tarif else 0 end) as c_nedop_gkal, " +
        //                          " sum(reval_k) as reval_k," +
        //                          " sum(reval_d) as reval_d," +
        //                          " sum(case when a.tarif_hvs>0 and nzp_serv in (14,514) then -1*reval_k/a.tarif_hvs else 0 end) as c_reval_k_kub," +
        //                          " sum(case when a.tarif>0 and nzp_serv in (9, 513) then -1*reval_k/a.tarif else 0 end) as c_reval_k_gkal," +
        //                          " sum(case when a.tarif_hvs>0 and nzp_serv in (14,514) then reval_d/a.tarif_hvs else 0 end) as c_reval_d_kub," +
        //                          " sum(case when a.tarif>0 and nzp_serv in (9, 513) then reval_d/a.tarif else 0 end) as c_reval_d_gkal," +
        //                          " sum(sum_charge) as sum_charge, " +
        //                          " sum(case when nzp_serv = 14 then c_calc else 0 end) as c_calc_kub, " +
        //                          " sum(case when nzp_serv = 514 then c_calc else 0 end) as c_calc_kub_odn, " +
        //                          " sum(case when nzp_serv = 9 then c_calc else 0 end) as c_calc_gkal, " +
        //                          " sum(case when nzp_serv = 513 then c_calc else 0 end) as c_calc_gkal_odn, " +
        //                          " max(pl_kvar) as pl_kvar " +
        //                          " from t1 a, outer " + pref + DBManager.sKernelAliasRest+"formuls f " +
        //                          " where a.nzp_frm=f.nzp_frm  and abs(" + DBManager.sNvlWord + "(rsum_tarif,0))+  " +
        //                          " abs(" + DBManager.sNvlWord + "(sum_nedop,0)) + abs(" + DBManager.sNvlWord + "(reval_k,0))+ " +
        //                          " abs(" + DBManager.sNvlWord + "(reval_d,0)) + " +
        //                          " abs(" + DBManager.sNvlWord + "(sum_charge,0))+" +
        //                          " abs(" + DBManager.sNvlWord + "(c_calc,0))+" +
        //                          " abs(" + DBManager.sNvlWord + "(count_gil,0))+" +
        //                          " abs(" + DBManager.sNvlWord + "(pl_kvar,0)) >0.001 " +
        //                          " group by 1,2,3,4,5";
        //                    ExecSQL(connDb, sql, true);


        //                    ExecSQL(connDb, "drop table t1", true);


        //                }

        //            }
        //            reader.Close();
        //#endregion
        //            ExecSQL(connDb, "drop table t_dom", false);

        //            sql = "create temp table t_dom (nzp_dom integer, nzp_kvar integer, nzp_geu integer)" +
        //                  DBManager.sUnlogTempTable;
        //            ExecSQL(connDb, sql, true);

        //            sql = " insert into t_dom(nzp_dom, nzp_kvar, nzp_geu)" +
        //                  " select nzp_dom, nzp_kvar, nzp_geu " +
        //                  " from t_svod group by 1,2,3 ";
        //            ExecSQL(connDb, sql, true);

        //            ExecSQL(connDb, "drop table t_frm", false);

        //            sql = "create temp table t_frm (name_frm char(100))" + DBManager.sUnlogTempTable;
        //            ExecSQL(connDb, sql, true);

        //            sql = " insert into t_frm (name_frm) " +
        //                  " select name_frm from t_svod group by 1  ";
        //            ExecSQL(connDb, sql, true);


        //            sql = " insert into t_svod(nzp_geu,  nzp_dom, nzp_kvar, name_frm) " +
        //                  " select nzp_geu,  nzp_dom, nzp_kvar, name_frm from t_dom, t_frm ";
        //            ExecSQL(connDb, sql, true);

        //            ExecSQL(connDb,  "drop table t_dom", true);
        //            ExecSQL(connDb,  "drop table t_frm", true);
        //            ExecSQL(connDb,  "drop table sel_kvar_10", true);

        //            ExecSQL(connDb, "drop table t_svod_all", false);
        //             sql = " create temp table t_svod_all( " +
        //                         " nzp_dom integer," +
        //                         " nzp_geu integer," +
        //                         " name_frm char(100)," +
        //                         " nzp_measure integer, " +
        //                         " count_gil integer," +
        //                         " rsum_tarif_all " + DBManager.sDecimalType + "(14,2), " +
        //                         " rsum_tarif " + DBManager.sDecimalType + "(14,2), " +
        //                         " sum_odn " + DBManager.sDecimalType + "(14,2), " +
        //                         " tarif " + DBManager.sDecimalType + "(14,3), " +
        //                         " c_calc_kub " + DBManager.sDecimalType + "(14,5), " +
        //                         " c_calc_gkal " + DBManager.sDecimalType + "(14,5), " +
        //                         " c_calc_kub_odn " + DBManager.sDecimalType + "(14,5), " +
        //                         " c_calc_gkal_odn " + DBManager.sDecimalType + "(18,7), " +
        //                         " sum_nedop " + DBManager.sDecimalType + "(14,2), " +
        //                         " reval_k " + DBManager.sDecimalType + "(14,2)," +
        //                         " reval_d " + DBManager.sDecimalType + "(14,2), " +
        //                         " c_nedop_kub " + DBManager.sDecimalType + "(18,5)," +
        //                         " c_nedop_gkal " + DBManager.sDecimalType + "(18,5)," +
        //                         " c_reval_k_kub " + DBManager.sDecimalType + "(18,5)," +
        //                         " c_reval_k_gkal " + DBManager.sDecimalType + "(18,5)," +
        //                         " c_reval_d_kub " + DBManager.sDecimalType + "(18,5), " +
        //                         " c_reval_d_gkal " + DBManager.sDecimalType + "(18,5), " +
        //                         " pl_kvar " + DBManager.sDecimalType + "(14,4), " +
        //                         " sum_charge " + DBManager.sDecimalType + "(14,2))" + DBManager.sUnlogTempTable;
        //            ExecSQL(connDb, sql, true);

        //            sql = "insert into t_svod_all(nzp_dom, nzp_geu,   " +
        //                  " name_frm, count_gil, rsum_tarif_all, rsum_tarif,  " +
        //                  " sum_odn,  tarif,  nzp_measure, sum_nedop, reval_k, " +
        //                  " reval_d, c_nedop_kub, c_nedop_gkal,  c_reval_k_kub, " +
        //                  " c_reval_k_gkal, c_reval_d_kub, c_reval_d_gkal," +
        //                  " sum_charge, c_calc_kub, c_calc_gkal, c_calc_kub_odn, c_calc_gkal_odn, " +
        //                  " pl_kvar)" +
        //                  " select nzp_dom, nzp_geu,   " +
        //                  " name_frm," +
        //                  " sum(" + DBManager.sNvlWord + "(count_gil,0)) as count_gil, " +
        //                  " sum(" + DBManager.sNvlWord + "(rsum_tarif_all,0)) as rsum_tarif_all,  " +
        //                  " sum(" + DBManager.sNvlWord + "(rsum_tarif,0)) as rsum_tarif,  " +
        //                  " sum(" + DBManager.sNvlWord + "(sum_odn,0)) as sum_odn,  " +
        //                  " max(" + DBManager.sNvlWord + "(a.tarif,0)) as tarif ," +
        //                  " max(" + DBManager.sNvlWord + "(a.nzp_measure,0)) as nzp_measure," +
        //                  " sum(" + DBManager.sNvlWord + "(sum_nedop,0)) as sum_nedop,  " +
        //                  " sum(-1*" + DBManager.sNvlWord + "(reval_k,0)) as reval_k, " +
        //                  " sum(" + DBManager.sNvlWord + "(reval_d,0)) as reval_d," +
        //                  " sum(" + DBManager.sNvlWord + "(c_nedop_kub,0)) as c_nedop_kub,  " +
        //                  " sum(" + DBManager.sNvlWord + "(c_nedop_gkal,0)) as c_nedop_gkal,  " +
        //                  " sum(" + DBManager.sNvlWord + "(c_reval_k_kub,0)) as c_reval_k_kub, " +
        //                  " sum(" + DBManager.sNvlWord + "(c_reval_k_gkal,0)) as c_reval_k_gkal, " +
        //                  " sum(" + DBManager.sNvlWord + "(c_reval_d_kub,0)) as c_reval_d_kub," +
        //                  " sum(" + DBManager.sNvlWord + "(c_reval_d_gkal,0)) as c_reval_d_gkal," +
        //                  " sum(" + DBManager.sNvlWord + "(sum_charge,0)) as sum_charge, " +
        //                  " sum(" + DBManager.sNvlWord + "(c_calc_kub,0)) as c_calc_kub, " +
        //                  " sum(" + DBManager.sNvlWord + "(c_calc_gkal,0)) as c_calc_gkal, " +
        //                  " sum(" + DBManager.sNvlWord + "(c_calc_kub_odn,0)) as c_calc_kub_odn, " +
        //                  " sum(" + DBManager.sNvlWord + "(c_calc_gkal_odn,0)) as c_calc_gkal_odn, " +
        //                  " sum(" + DBManager.sNvlWord + "(pl_kvar,0)) as pl_kvar " +
        //                  " from t_svod a group by 1,2,3 ";
        //            ExecSQL(connDb, sql, true);

        //            sql = "create index ix_tmp_svod_01 on t_svod_all(nzp_dom)";
        //            ExecSQL(connDb, sql, true);

        //            ExecSQL(connDb, DBManager.sUpdStat + " t_svod_all", true);

        //            ExecSQL(connDb, "drop table t_svod", true);

        //           // var dt = DBManager.ExecSQLToTable(connDb, "select * from t_svod_all");

        //            sql = " select geu, ulica, ndom, Replace(nkor,'-','') as nkor, idom,  " +
        //                  " name_frm," +
        //                  " count_gil, " +
        //                  " rsum_tarif_all,  " +
        //                  " rsum_tarif,  " +
        //                  " sum_odn,  " +
        //                  " tarif ," +
        //                  " nzp_measure," +
        //                  " sum_nedop,  " +
        //                  " reval_k, " +
        //                  " reval_d," +
        //                  " c_nedop_kub,  " +
        //                  " c_nedop_gkal,  " +
        //                  " c_reval_k_kub, " +
        //                  " c_reval_k_gkal, " +
        //                  " c_reval_d_kub," +
        //                  " c_reval_d_gkal," +
        //                  " sum_charge, " +
        //                  " c_calc_kub, " +
        //                  " c_calc_gkal, " +
        //                  " c_calc_kub_odn, " +
        //                  " c_calc_gkal_odn, " +
        //                  " pl_kvar " +
        //                  " from t_svod_all a, " + 
        //                  Points.Pref + DBManager.sDataAliasRest + "dom d, "+
        //                  Points.Pref + DBManager.sDataAliasRest + "s_ulica su, " +
        //                  Points.Pref + DBManager.sDataAliasRest + "s_geu sg " +
        //                  " where a.nzp_dom=d.nzp_dom " +
        //                  " and d.nzp_ul=su.nzp_ul " +
        //                  " and a.nzp_geu=sg.nzp_geu " +
        //                  " order by geu, ulica, idom, ndom, nkor, name_frm  ";
        //            DataTable localTable = null;
        //            try
        //            {

        //                localTable = DBManager.ExecSQLToTable(connDb, sql);
        //            }
        //            catch (Exception ex)
        //            {

        //                MonitorLog.WriteLog("ExcelReport : Ошибка при выборке начислений на экран " +
        //                ex.Message, MonitorLog.typelog.Error, true);
        //            }


        //            ExecSQL(connDb, "drop table t_svod_all", true);

        //            connDb.Close();




        //            return localTable;

        //        }


        public DataTable GetSpravSodergOdn9(Prm prm, out Returns ret, string nzpUser)
        {
            //MonitorLog.WriteLog("ExcelReport start ifmx", MonitorLog.typelog.Info, true);

            #region Подключение к БД
            IDbConnection connWeb = GetConnection(Constants.cons_Webdata);//new IDbConnection(Constants.cons_Webdata);


            MyDataReader reader;

            ret = OpenDb(connWeb, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("ExcelReport : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                return null;
            }
            string tXXSpls = DBManager.GetFullBaseName(connWeb) + DBManager.tableDelimiter + "t" + nzpUser + "_spls";
            connWeb.Close();


            IDbConnection connDb = GetConnection(Constants.cons_Kernel);//new IDbConnection(Constants.cons_Webdata);

            ret = OpenDb(connDb, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("ExcelReport : Ошибка при открытии соединения с локальной БД ", MonitorLog.typelog.Error, true);
                return null;
            }

            #endregion


            #region Выборка по локальным банкам

            ExecSQL(connDb, "drop table t_svod", false);


            string sql = " create temp table t_svod( " +
                         " nzp_dom integer," +
                         " nzp_kvar integer," +
                         " nzp_geu integer," +
                         " name_frm char(100)," +
                         " nzp_measure integer, " +
                         " count_gil integer," +
                         " rsum_tarif_all " + DBManager.sDecimalType + "(14,2), " +
                         " rsum_tarif " + DBManager.sDecimalType + "(14,2), " +
                         " sum_odn " + DBManager.sDecimalType + "(14,2), " +
                         " tarif " + DBManager.sDecimalType + "(14,3), " +
                         " c_calc_kub " + DBManager.sDecimalType + "(14,5), " +
                         " c_calc_gkal " + DBManager.sDecimalType + "(14,5), " +
                         " c_calc_kub_odn " + DBManager.sDecimalType + "(14,5), " +
                         " c_calc_gkal_odn " + DBManager.sDecimalType + "(18,5), " +
                         " sum_nedop " + DBManager.sDecimalType + "(14,2), " +
                         " reval_k " + DBManager.sDecimalType + "(14,2)," +
                         " reval_d " + DBManager.sDecimalType + "(14,2), " +
                         " c_nedop_kub " + DBManager.sDecimalType + "(18,5)," +
                         " c_nedop_gkal " + DBManager.sDecimalType + "(18,5)," +
                         " c_reval_k_kub " + DBManager.sDecimalType + "(18,5)," +
                         " c_reval_k_gkal " + DBManager.sDecimalType + "(18,5)," +
                         " c_reval_d_kub " + DBManager.sDecimalType + "(18,5), " +
                         " c_reval_d_gkal " + DBManager.sDecimalType + "(18,5), " +
                         " pl_kvar " + DBManager.sDecimalType + "(14,4), " +
                         " sum_charge " + DBManager.sDecimalType + "(14,2))" + DBManager.sUnlogTempTable;

            ExecSQL(connDb, sql, true);


            ExecSQL(connDb, "drop table sel_kvar_10", false);

            sql = " Create temp table sel_kvar_10(nzp_dom integer, " +
                  " nzp_geu integer, nzp_kvar integer, pref char(10))" + DBManager.sUnlogTempTable;
            ExecSQL(connDb, sql, true);

            sql = "insert into sel_kvar_10 (nzp_dom, nzp_geu, nzp_kvar, pref)" +
                  " select nzp_dom, nzp_geu, nzp_kvar, pref " +
                  " from " + tXXSpls + " k ";
            ExecSQL(connDb, sql, true);


            sql = " select pref " +
                  " from  sel_kvar_10 group by 1";
            ret = ExecRead(connDb, out reader, sql, true);
            if (!ret.result)
            {
                connDb.Close();
                return null;
            }

            while (reader.Read())
            {
                if (reader["pref"] != null)
                {
                    string pref = Convert.ToString(reader["pref"]).Trim();
                    string sChargeAlias = pref + "_charge_" + (prm.year_ - 2000).ToString("00");
                    string tableCalcGku = pref + "_charge_" +
                                          (prm.year_ - 2000).ToString("00") + DBManager.tableDelimiter + "calc_gku_"
                                          + prm.month_.ToString("00");

                    ExecSQL(connDb, " drop table sel_kvar10", false);

                    #region Заполнение sel_kvar10

                    sql = " create temp table sel_kvar10 ( " +
                          " nzp_kvar integer," +
                          " nzp_dom integer," +
                          " nzp_geu integer," +
                          " is_device integer default 0," +
                          " isol integer default 1," +
                          " pl_kvar " + DBManager.sDecimalType + "(14,2)) " + DBManager.sUnlogTempTable;
                    ExecSQL(connDb, sql, true);


                    sql = " insert into sel_kvar10(nzp_dom, nzp_geu, nzp_kvar)" +
                          " select nzp_dom, nzp_geu, nzp_kvar " +
                          " from sel_kvar_10 k " +
                          " where pref='" + pref + "'";

                    if (prm.has_pu == "2")
                        sql = sql + " and 0 < (select count(*)  " +
                              " from " + sChargeAlias + DBManager.tableDelimiter + "counters_" +
                              prm.month_.ToString("00") + " d " +
                              " where d.stek=3 and d.nzp_type=1 and d.cnt_stage>0 and d.nzp_serv=" + prm.nzp_serv +
                              " and k.nzp_dom=d.nzp_dom ) ";
                    if (prm.has_pu == "3")
                        sql = sql + " and 0 = (select count(*)  " +
                              " from " + sChargeAlias + DBManager.tableDelimiter + "counters_" +
                              prm.month_.ToString("00") + " d" +
                              " where d.stek=3 and d.nzp_type=1 and d.cnt_stage>0 and d.nzp_serv=" + prm.nzp_serv +
                              " and k.nzp_dom=d.nzp_dom ) ";
                    ExecSQL(connDb, sql, true);


                    sql = "create index ix_tmpk_02 on sel_kvar10(nzp_kvar)";
                    ExecSQL(connDb, sql, true);

                    ExecSQL(connDb, sUpdStat + " sel_kvar10 ", true);




                    sql = " update sel_kvar10 set pl_kvar=(select max(val_prm" + DBManager.sConvToNum + ") " +
                          " from " + pref + DBManager.sDataAliasRest + "prm_1 where nzp_prm=4 and is_actual=1 " +
                          " and dat_s<=current_date and dat_po>=current_date and sel_kvar10.nzp_kvar=nzp)";
                    ExecSQL(connDb, sql, true);

                    sql = " update sel_kvar10 set is_device =1 " +
                          " where 0<(select count(*) " +
                          " from " + pref + DBManager.sDataAliasRest + "prm_1 " +
                          " where nzp_prm=103 and is_actual=1 " +
                          " and dat_s<=current_date and dat_po>=current_date and sel_kvar10.nzp_kvar=nzp)";
                    ExecSQL(connDb, sql, true);

                    sql = " update sel_kvar10 set isol=2 where nzp_kvar in (select nzp " +
                          " from " + pref + DBManager.sDataAliasRest + "prm_1 " +
                          " where nzp_prm=3 and is_actual=1 and val_prm='2' " +
                          " and dat_s<=" + DBManager.sCurDate + " and dat_po>=" + DBManager.sCurDate + " )";
                    ExecSQL(connDb, sql, true);

                    #endregion


                    ExecSQL(connDb, "drop table t1", false);
                    #region Добавляем основную услугу

                    sql = "create temp table t1 (" +
                         " nzp_geu integer, " +
                         " nzp_dom integer, " +
                         " nzp_kvar integer, " +
                         " nzp_frm integer, " +
                         " nzp_measure integer, " +
                         " nzp_serv integer, " +
                         " is_device integer, " +
                         " count_gil integer, " +
                         " pl_kvar " + DBManager.sDecimalType + "(14,2), " +
                         " gkal_koef " + DBManager.sDecimalType + "(14,6), " +
                         " norma " + DBManager.sDecimalType + "(14,2), " +
                         " tarif_gkal " + DBManager.sDecimalType + "(14,2), " +
                         " tarif_hvs " + DBManager.sDecimalType + "(14,2), " +
                         " sum_charge " + DBManager.sDecimalType + "(14,2), " +
                         " rsum_tarif " + DBManager.sDecimalType + "(14,2), " +
                         " rsum_tarif_odn " + DBManager.sDecimalType + "(14,2), " +
                         " sum_nedop " + DBManager.sDecimalType + "(14,2), " +
                         " sum_nedop_cor " + DBManager.sDecimalType + "(14,2) default 0, " +
                         " reval_k " + DBManager.sDecimalType + "(14,2), " +
                         " reval_d " + DBManager.sDecimalType + "(14,2), " +

                         " sum_nedop_kub " + DBManager.sDecimalType + "(14,2), " +
                         " sum_nedop_cor_kub " + DBManager.sDecimalType + "(14,2) default 0, " +
                         " reval_k_kub " + DBManager.sDecimalType + "(14,2), " +
                         " reval_d_kub " + DBManager.sDecimalType + "(14,2), " +

                         " c_calc_gkal " + DBManager.sDecimalType + "(14,5)," +
                         " c_calc_kub " + DBManager.sDecimalType + "(14,5)," +
                         " c_calc_gkal_odn " + DBManager.sDecimalType + "(14,5)," +
                         " c_calc_kub_odn " + DBManager.sDecimalType + "(14,5)) " + DBManager.sUnlogTempTable;
                    ExecSQL(connDb, sql, true);


                    sql = " insert into t1 (nzp_geu, nzp_dom, nzp_kvar, is_device, nzp_frm, " +
                          " tarif_gkal, tarif_hvs, sum_charge, rsum_tarif, rsum_tarif_odn, sum_nedop, reval_k, reval_d," +
                          " sum_nedop_kub, reval_k_kub, reval_d_kub) " +
                          " select nzp_geu, nzp_dom, a.nzp_kvar, b.is_device, " +
                          " max(case when nzp_serv=9 and tarif>0 then nzp_frm else 0 end), " +
                          " max(case when nzp_serv=9 then tarif else 0 end ) as tarif_gkal," +
                          " max(case when nzp_serv=14 then tarif else 0 end ) as tarif_hvs," +
                          " sum(sum_charge) as sum_charge, " +
                          " sum(case when nzp_serv<500 then rsum_tarif else 0 end) as rsum_tarif, " +
                          " sum(case when nzp_serv>500 then rsum_tarif else 0 end) as rsum_tarif_odn, " +
                          " sum(case when nzp_serv=9 then sum_nedop else 0 end) as sum_nedop," +
                          " sum(case when real_charge<0 and nzp_serv in (9,513,1010052) then real_charge else 0 end) +" +
                          " sum(case when reval<0 and nzp_serv in (9,513,1010052) then reval else 0 end) as reval_k," +
                          " sum(case when real_charge>0 and nzp_serv in (9,513,1010052) then real_charge else 0 end) +" +
                          " sum(case when reval>0 and nzp_serv in (9,513,1010052) then reval else 0 end) as reval_d," +

                          " sum(case when nzp_serv=14 then sum_nedop else 0 end) as sum_nedop_kub," +
                          " sum(case when real_charge<0 and nzp_serv in (14,514,1010053) then real_charge else 0 end) +" +
                          " sum(case when reval<0 and nzp_serv in (14,514,1010053) then reval else 0 end) as reval_k_kub," +
                          " sum(case when real_charge>0 and nzp_serv in (14,514,1010053) then real_charge else 0 end) +" +
                          " sum(case when reval>0 and nzp_serv in (14,514,1010053)then reval else 0 end) as reval_d_kub" +

                          " from  " + sChargeAlias + DBManager.tableDelimiter + "charge_" +
                          prm.month_.ToString("00") + " a, sel_kvar10 b" +
                          " where nzp_serv  in (9,14,513,514,1010052,1010053) " +
                          " and a.nzp_kvar=b.nzp_kvar " +
                          " and abs(rsum_tarif)+abs(sum_nedop)+abs(real_charge)+ abs(reval) +" +
                          " abs(tarif) + abs(sum_charge)>0.001" +
                          " and dat_charge is null ";
                    if (prm.nzp_key > -1) //Добавляем поставщика
                    {
                        sql = sql + " and a.nzp_supp = " + prm.nzp_key;
                    }
                    sql = sql + " group by 1,2,3,4";

                    ExecSQL(connDb, sql, true);

                    sql = " UPDATE t1 set reval_k = reval_k - coalesce((SELECT reval  from(SELECT nzp_dom, a.nzp_kvar, sum(sum_rcl) as reval from " + sChargeAlias + ".perekidka " +
                    " a INNER JOIN " + pref + "_data.kvar b on b.nzp_kvar = a.nzp_kvar INNER JOIN fbill_data.document_base d on d.nzp_doc_base = a.nzp_doc_base where month_ = " + 
                    prm.month_.ToString() + "  AND d.comment = 'Выравнивание сальдо' and nzp_serv in (9,14,513,514,1010052,1010053) group by 1,2) t " +
                    " where t1.nzp_dom = t.nzp_dom and t1.nzp_kvar = t.nzp_kvar), 0)";

                    if (!ExecSQL(connDb, sql.ToString(), true).result)
                        return null;

                    sql = "create index ix_tmpk_04 on t1(nzp_kvar)";
                    ExecSQL(connDb, sql, true);

                    ExecSQL(connDb, sUpdStat + " t1 ", true);



                    #region Жигулевск недопоставка как перекидка


                    sql = "update t1 set sum_nedop_cor = (select  " +
                          " -sum(sum_rcl) " +
                          " from " + sChargeAlias + DBManager.tableDelimiter + "perekidka a " +
                          " where a.nzp_kvar=t1.nzp_kvar and type_rcl = 101 and month_=" + prm.month_.ToString("00") +
                          " and abs(sum_rcl)>0.001 and nzp_serv =9 ";
                    if (prm.nzp_key > -1) //Добавляем поставщика
                    {
                        sql = sql + " and a.nzp_supp = " + prm.nzp_key;
                    }
                    sql = sql + ") where reval_k<0 ";
                    ExecSQL(connDb, sql, true);


                    sql = "update t1 set sum_nedop_cor_kub = (select  " +
                    " -sum(sum_rcl) " +
                    " from " + sChargeAlias + DBManager.tableDelimiter + "perekidka a " +
                    " where a.nzp_kvar=t1.nzp_kvar and type_rcl = 101 and month_=" + prm.month_.ToString("00") +
                    " and abs(sum_rcl)>0.001 and nzp_serv =14 ";
                    if (prm.nzp_key > -1) //Добавляем поставщика
                    {
                        sql = sql + " and a.nzp_supp = " + prm.nzp_key;
                    }
                    sql = sql + ") where reval_k_kub<0 ";
                    ExecSQL(connDb, sql, true);

                    sql = "update t1 set sum_nedop = sum_nedop + " + DBManager.sNvlWord + "(sum_nedop_cor,0)";
                    ExecSQL(connDb, sql, true);

                    sql = "update t1 set reval_k = reval_k + " + DBManager.sNvlWord + "(sum_nedop_cor,0)";
                    ExecSQL(connDb, sql, true);


                    sql = "update t1 set sum_nedop_kub = sum_nedop_kub + " + DBManager.sNvlWord + "(sum_nedop_cor_kub,0)";
                    ExecSQL(connDb, sql, true);

                    sql = "update t1 set reval_k_kub = reval_k_kub + " + DBManager.sNvlWord + "(sum_nedop_cor_kub,0)";
                    ExecSQL(connDb, sql, true);

                    #endregion


                    #region Проставляем площадь и количество жильцов


                    sql = " update t1 set pl_kvar = " + DBManager.sNvlWord + "((select max(squ) " +
                          " from  " + tableCalcGku + " d " +
                          " where d.nzp_kvar=t1.nzp_kvar  and d.tarif>0 and d.nzp_serv = 9 ";
                    if (prm.nzp_key > -1) //Добавляем поставщика
                    {
                        sql = sql + " and d.nzp_supp = " + prm.nzp_key;
                    }
                    sql = sql + " ),0)";
                    ExecSQL(connDb, sql, true);

                    sql = " update t1 set  count_gil = " + DBManager.sNvlWord + "((select max(gil) " +
                          " from  " + tableCalcGku + " d " +
                          " where d.nzp_kvar=t1.nzp_kvar  and d.tarif>0 and d.nzp_serv = 9 ";
                    if (prm.nzp_key > -1) //Добавляем поставщика
                    {
                        sql = sql + " and d.nzp_supp = " + prm.nzp_key;
                    }
                    sql = sql + " ),0)";
                    ExecSQL(connDb, sql, true);



                    //sql = " update t1 set  norma = " + DBManager.sNvlWord + "((select max(rash_norm_one/rsh2) " +
                    //  " from  " + tableCalcGku + " d " +
                    //  " where d.nzp_kvar=t1.nzp_kvar  " +
                    //  " and 9 = d.nzp_serv and rsh2>0 ";
                    //if (prm.nzp_key > -1) //Добавляем поставщика
                    //{
                    //    sql = sql + " and d.nzp_supp = " + prm.nzp_key;
                    //}
                    //sql = sql + " ),0)";

                    //ExecSQL(connDb, sql, true);

                    sql = " update t1 set  norma = " + DBManager.sNvlWord + "((select max(rash_norm_one) " +
                      " from  " + tableCalcGku + " d " +
                      " where d.nzp_kvar=t1.nzp_kvar  " +
                      " and 14 = d.nzp_serv ";
                    if (prm.nzp_key > -1) //Добавляем поставщика
                    {
                        sql = sql + " and d.nzp_supp = " + prm.nzp_key;
                    }
                    sql = sql + " ),0)";

                    ExecSQL(connDb, sql, true);

                    sql = " update t1 set  norma = " + DBManager.sNvlWord + "((select max(Round(rash_norm_one/rsh2,1)) " +
                    " from  " + tableCalcGku + " d " +
                    " where d.nzp_kvar=t1.nzp_kvar  " +
                    " and 9 = d.nzp_serv and rsh2>0";
                    if (prm.nzp_key > -1) //Добавляем поставщика
                    {
                        sql = sql + " and d.nzp_supp = " + prm.nzp_key;
                    }
                    sql = sql + " ),0) where norma<0.01";

                    ExecSQL(connDb, sql, true);

                    #endregion

                    //Проставляем расход по основным услугам

                    sql = " update t1 set c_calc_gkal = (select sum(valm+dlt_reval) " +
                          " from  " + tableCalcGku + " d " +
                          " where d.nzp_kvar=t1.nzp_kvar  and d.tarif>0 and d.nzp_serv = 9 ";

                    if (prm.nzp_key > -1) //Добавляем поставщика
                    {
                        sql = sql + " and d.nzp_supp = " + prm.nzp_key;
                    }
                    sql = sql + " ) where tarif_gkal>0";
                    ExecSQL(connDb, sql, true);


                    sql = " update t1 set c_calc_kub = (select sum(valm+dlt_reval) " +
                     " from  " + tableCalcGku + " d " +
                     " where d.nzp_kvar=t1.nzp_kvar  and d.tarif>0 and d.nzp_serv = 14 ";

                    if (prm.nzp_key > -1) //Добавляем поставщика
                    {
                        sql = sql + " and d.nzp_supp = " + prm.nzp_key;
                    }
                    sql = sql + " ) where tarif_gkal>0";
                    ExecSQL(connDb, sql, true);

                    sql = " update t1 set gkal_koef = (select max(rsh2) " +
                        " from  " + tableCalcGku + " d " +
                        " where d.nzp_kvar=t1.nzp_kvar  and d.tarif>0 " +
                        " and 9 = d.nzp_serv ";

                    if (prm.nzp_key > -1) //Добавляем поставщика
                    {
                        sql = sql + " and d.nzp_supp = " + prm.nzp_key;
                    }
                    sql = sql + " ) where tarif_gkal>0";
                    ExecSQL(connDb, sql, true);

                    //Проставляем расход по услугам ОДН Подогрев

                    sql = " update t1 set c_calc_gkal_odn = (select " +
                    " sum(case when valm+dlt_reval>=0 and dop87<0 and dop87< -valm-dlt_reval then -valm-dlt_reval  " +
                    "          else dop87 end) " +
                    " from  " + tableCalcGku + " d " +
                    " where d.nzp_kvar=t1.nzp_kvar  and d.tarif>0  " +
                    " and 9 = d.nzp_serv ";
                    if (prm.nzp_key > -1) //Добавляем поставщика
                    {
                        sql = sql + " and d.nzp_supp = " + prm.nzp_key;
                    }
                    sql = sql + " ) where tarif_gkal>0";
                    ExecSQL(connDb, sql, true);


                    //Проставляем расход по услугам ОДН Хвс для ГВС
                    sql = " update t1 set c_calc_kub_odn = (select " +
                          " sum(case when valm+dlt_reval>=0 and  dop87<0 and dop87< -valm-dlt_reval then -valm-dlt_reval  " +
                          "          else dop87 end) " +
                          " from  " + tableCalcGku + " d " +
                          " where d.nzp_kvar=t1.nzp_kvar  and d.tarif>0 " +
                          " and 14 = d.nzp_serv ";

                    if (prm.nzp_key > -1) //Добавляем поставщика
                    {
                        sql = sql + " and d.nzp_supp = " + prm.nzp_key;
                    }
                    sql = sql + " ) where tarif_gkal>0";
                    ExecSQL(connDb, sql, true);



                    //sql = " update t1 set pl_kvar = (select max(pl_kvar) " +
                    //      " from  sel_kvar10 d" +
                    //      " where d.nzp_kvar=t1.nzp_kvar  and isol=2 )" +
                    //      " where nzp_kvar in (select nzp_kvar from sel_kvar10 where isol=2) ";
                    //ExecSQL(connDb, sql, true);



                    #region Выборка перерасчетов прошлого периода
                    ExecSQL(connDb, "drop table t_upd_nedop", false);
                    sql = "create temp table t_upd_nedop (" +
                        " nzp_kvar integer, " +
                        " sum_nedop " + DBManager.sDecimalType + "(14,2)," +
                        " sum_nedop_kub " + DBManager.sDecimalType + "(14,2)) " + DBManager.sUnlogTempTable;
                    ExecSQL(connDb, sql, true);


                    ExecSQL(connDb, "drop table t_nedop", false);
                    sql = "Create temp table t_nedop (nzp_geu integer," +
                          " nzp_dom integer, " +
                          " nzp_kvar integer, " +
                          " month_s integer, " +
                          " month_po integer)" + DBManager.sUnlogTempTable;
                    ExecSQL(connDb, sql, true);
#warning PG
                    sql = " insert into t_nedop (nzp_geu,nzp_dom,nzp_kvar, month_s, month_po)" +
                          " select b.nzp_geu,b.nzp_dom,a.nzp_kvar, min(EXTRACT (year FROM dat_s)*12+EXTRACT (month FROM dat_s)) as month_s,  " +
                          " max(EXTRACT (year FROM dat_po)*12+EXTRACT (month FROM dat_po)) as month_po" +
                          " from " + pref + DBManager.sDataAliasRest + "nedop_kvar a, sel_kvar10 b " +
                          " where a.nzp_kvar=b.nzp_kvar and month_calc='01." + prm.month_.ToString("00") + "." +
                          prm.year_.ToString("0000") + "'  " +
                          " group by 1,2,3";

                    ExecSQL(connDb, sql, true);


                    sql = " select month_, year_ " +
                          " from " + pref + "_charge_" + (prm.year_ - 2000).ToString("00") +
                          DBManager.tableDelimiter + "lnk_charge_" + prm.month_.ToString("00") + " b, t_nedop d " +
                          " where  b.nzp_kvar=d.nzp_kvar " +
                          " and year_*12+month_>=month_s and  year_*12+month_<=month_po " +
                          " group by 1,2";
                    IDataReader reader2;
                    ExecRead(connDb, out reader2, sql, true);

                    while (reader2.Read())
                    {
                        string sTmpAlias = pref + "_charge_" + (Int32.Parse(reader2["year_"].ToString()) - 2000).ToString("00");


                        sql =
                            " insert into t_upd_nedop (nzp_kvar,sum_nedop, sum_nedop_kub)   " +
                            " select  b.nzp_kvar,  " +
                            " sum(case when nzp_serv = 9 then sum_nedop-sum_nedop_p else 0 end), " +
                            " sum(case when nzp_serv = 14 then sum_nedop-sum_nedop_p else 0 end) " +
                            " from " + sTmpAlias + DBManager.tableDelimiter +
                            "charge_" + Int32.Parse(reader2["month_"].ToString()).ToString("00") + " b, " +
                            "t_nedop d " +
                            " where  b.nzp_kvar=d.nzp_kvar and dat_charge = date('28." +
                            prm.month_.ToString("00") + "." + prm.year_ + "')" +
                            " and abs(sum_nedop)+abs(sum_nedop_p)>0.001" +
                            " and nzp_serv in (9, 14, 513, 514, 1010052, 1010053)";
                        if (prm.nzp_key > -1) //Добавляем поставщика
                        {
                            sql = sql + " and nzp_supp = " + prm.nzp_key;
                        }
                        sql = sql + " group by 1";
                        ExecSQL(connDb, sql, true);
                    }
                    reader2.Close();

                    ExecSQL(connDb, "drop table t_nedop", true);


                    sql = "update t1 set sum_nedop = sum_nedop + " + DBManager.sNvlWord + "((" +
                          " select sum(sum_nedop) " +
                          " from t_upd_nedop a where t1.nzp_kvar=a.nzp_kvar),0)";
                    ExecSQL(connDb, sql, true);
                    sql = "update t1 set reval_k = reval_k + " + DBManager.sNvlWord + "((" +
                          " select sum(sum_nedop) " +
                          " from t_upd_nedop a where t1.nzp_kvar=a.nzp_kvar and sum_nedop>0),0)";
                    ExecSQL(connDb, sql, true);

                    sql = "update t1 set reval_d = reval_d + " + DBManager.sNvlWord + "((" +
                         " select sum(sum_nedop) " +
                         " from t_upd_nedop a where t1.nzp_kvar=a.nzp_kvar and sum_nedop<0 ),0)";
                    ExecSQL(connDb, sql, true);

                    sql = "update t1 set sum_nedop_kub = sum_nedop_kub + " + DBManager.sNvlWord + "((" +
                    " select sum(sum_nedop_kub) " +
                    " from t_upd_nedop a where t1.nzp_kvar=a.nzp_kvar),0)";
                    ExecSQL(connDb, sql, true);
                    sql = "update t1 set reval_k_kub = reval_k_kub + " + DBManager.sNvlWord + "((" +
                          " select sum(sum_nedop_kub) " +
                          " from t_upd_nedop a where t1.nzp_kvar=a.nzp_kvar and sum_nedop_kub>0),0)";
                    ExecSQL(connDb, sql, true);

                    sql = "update t1 set reval_d_kub = reval_d_kub + " + DBManager.sNvlWord + "((" +
                         " select sum(sum_nedop_kub) " +
                         " from t_upd_nedop a where t1.nzp_kvar=a.nzp_kvar and sum_nedop_kub<0),0)";
                    ExecSQL(connDb, sql, true);

                    ExecSQL(connDb, "drop table t_upd_nedop", true);

                    #endregion
                    #endregion




                    ExecSQL(connDb, DBManager.sUpdStat + " t1", true);





                    sql = " update t1 set nzp_frm  = " + DBManager.sNvlWord + "((select max(nzp_frm) " +
                          " from " + pref + DBManager.sDataAliasRest + "tarif f " +
                          " where t1.nzp_kvar=f.nzp_kvar and is_actual=1 and f.nzp_serv=9 and dat_po=" +
                          " (select max(dat_po) " +
                          " from " + pref + DBManager.sDataAliasRest + "tarif t " +
                          " where t.nzp_kvar=f.nzp_kvar and t.nzp_serv=9 and t.is_actual=1 )),0) where nzp_frm=0 ";
                    ExecSQL(connDb, sql, true);

                    sql = " update t1 set nzp_frm  = " + DBManager.sNvlWord + "((select max(nzp_frm) " +
                       " from " + pref + DBManager.sDataAliasRest + "tarif f " +
                       " where t1.nzp_kvar=f.nzp_kvar and f.nzp_serv=9 and dat_po=" +
                       " (select max(dat_po) " +
                       " from " + pref + DBManager.sDataAliasRest + "tarif t " +
                       " where t.nzp_kvar=f.nzp_kvar and t.nzp_serv=9  )),0) where nzp_frm=0 ";
                    ExecSQL(connDb, sql, true);







                    ExecSQL(connDb, " create index ixtmm_06 on t1(nzp_frm)", true);
                    ExecSQL(connDb, " create index ixtmm_08 on t1(nzp_dom)", true);
                    ExecSQL(connDb, DBManager.sUpdStat + " t1", true);





                    sql = " update t1 set nzp_measure= " +
                          DBManager.sNvlWord + "((select max(nzp_measure)" +
                          " from " + pref + DBManager.sKernelAliasRest + "formuls f " +
                          " where  t1.nzp_frm=f.nzp_frm),0) ";
                    ExecSQL(connDb, sql, true);




                    sql = " update t1 set nzp_dom = (select a.nzp_dom_base " +
                          " from " + pref + DBManager.sDataAliasRest + "link_dom_lit a" +
                          " where a.nzp_dom=t1.nzp_dom) " +
                          " where nzp_dom in (select nzp_dom from " + pref +
                          DBManager.sDataAliasRest + "link_dom_lit)";
                    ExecSQL(connDb, sql, true);


                    #region Простановка норматива по ГВС
                    ExecSQL(connDb, "drop table t_norm", false);

                    sql = "create temp table t_norm(nzp_dom integer, " +
                          " norma " + DBManager.sDecimalType + "(14,2))" + DBManager.sUnlogTempTable;
                    ExecSQL(connDb, sql, true);

                    sql = " insert into t_norm(nzp_dom,  norma) " +
                          " select nzp_dom, max(norma) as norma  " +
                          " from t1 " +
                          " group by 1 ";
                    ExecSQL(connDb, sql, true);


                    sql = " update t1 set norma =" + DBManager.sNvlWord + "((select norma " +
                          " from t_norm where t1.nzp_dom=t_norm.nzp_dom   ),0) ";
                    ExecSQL(connDb, sql, true);
                    ExecSQL(connDb, "drop table t_norm", true);
                    #endregion



                    #region Простановка тарифа по ГВС
                    ExecSQL(connDb, "drop table t_tar", false);

                    sql = "create temp table t_tar(nzp_dom integer, " +
                          " tarif " + DBManager.sDecimalType + "(14,2)," +
                          " tarif_hvs " + DBManager.sDecimalType + "(14,2)," +
                          " gkal_koef " + DBManager.sDecimalType + "(14,2))" + DBManager.sUnlogTempTable;
                    ExecSQL(connDb, sql, true);

                    sql = " insert into t_tar(nzp_dom,  tarif,tarif_hvs, gkal_koef) " +
                          " select nzp_dom, max(tarif_gkal) as tarif, max(tarif_hvs) as tarif_hvs, max(gkal_koef)  " +
                          " from t1 " +
                          " group by 1 ";
                    ExecSQL(connDb, sql, true);


                    sql = " update t1 set tarif_gkal =" + DBManager.sNvlWord + "((select max(tarif) " +
                          " from t_tar where t1.nzp_dom=t_tar.nzp_dom   ),0) where coalesce(tarif_gkal,0) = 0 ";
                    ExecSQL(connDb, sql, true);

                    sql = " update t1 set tarif_hvs =" + DBManager.sNvlWord + "((select max(tarif_hvs) " +
                      " from t_tar where t1.nzp_dom=t_tar.nzp_dom   ),0) where coalesce(tarif_hvs,0) = 0 ";
                    ExecSQL(connDb, sql, true);

                    sql = " update t1 set gkal_koef =" + DBManager.sNvlWord + "((select max(gkal_koef) " +
                         " from t_tar where t1.nzp_dom=t_tar.nzp_dom   ),0) where coalesce(gkal_koef,0) = 0 ";
                    ExecSQL(connDb, sql, true);

                    sql = " update t1 set tarif_gkal =" + DBManager.sNvlWord + "((select max(tarif) " +
                         " from t_tar ),0) where coalesce(tarif_gkal,0) = 0 ";
                    ExecSQL(connDb, sql, true);
                    sql = " update t1 set tarif_hvs =" + DBManager.sNvlWord + "((select max(tarif_hvs) " +
                    " from t_tar ),0) where coalesce(tarif_hvs,0) = 0 ";
                    ExecSQL(connDb, sql, true);

                    sql = " update t1 set gkal_koef =" + DBManager.sNvlWord + "((select max(gkal_koef) " +
                        " from t_tar),0) where coalesce(gkal_koef,0) = 0 ";
                    ExecSQL(connDb, sql, true);

                    ExecSQL(connDb, "drop table t_tar", true);
                    #endregion


                    //sql = "update t1 set c_calc_kub = round(c_calc_gkal/gkal_koef,3) where nvl(c_calc_kub,0) = 0";
                    //ExecSQL(connDb, sql, true);


                    //sql = "update t1 set c_calc_kub_odn = round(c_calc_gkal_odn/gkal_koef,3) where nvl(c_calc_kub_odn,0) = 0";
                    //ExecSQL(connDb, sql, true);


                    //По всем

                    sql = " insert into t_svod(nzp_geu, nzp_dom, nzp_kvar, name_frm, nzp_measure, " +
                          " count_gil, rsum_tarif_all, rsum_tarif, sum_odn, tarif, " +
                          " sum_nedop, c_nedop_kub, c_nedop_gkal, reval_k, reval_d, " +
                          " c_reval_k_kub, c_reval_k_gkal, c_reval_d_kub, c_reval_d_gkal, sum_charge, " +
                          " c_calc_kub, c_calc_kub_odn, c_calc_gkal, c_calc_gkal_odn, pl_kvar )" +
                          " select nzp_geu,  a.nzp_dom,  a.nzp_kvar, " +
                          " trim(" + DBManager.sNvlWord + "(name_frm,'Не определена формула'))" +
                          "||(case when a.is_device =1 then ' ИПУ ' else ' ' end) ||' норм.'||norma||' : '||a.tarif_gkal as name_frm, " +
                          " f.nzp_measure, max(count_gil), sum(rsum_tarif+rsum_tarif_odn), " +
                          " sum(rsum_tarif) as rsum_tarif, " +
                          " sum(rsum_tarif_odn) as sum_odn, " +
                          " max(a.tarif_gkal) as tarif, " +
                          " sum(sum_nedop+sum_nedop_kub) as sum_nedop,  " +
                          " sum(Round(case when " + DBManager.sNvlWord + "(tarif_hvs,0)>0 then sum_nedop_kub/tarif_hvs else 0 end,5)) as c_nedop_kub, " +
                          " sum(Round(case when " + DBManager.sNvlWord + "(a.tarif_gkal,0)>0 then sum_nedop/a.tarif_gkal else 0 end,5)) as c_nedop_gkal, " +
                          " sum(reval_k+reval_k_kub) as reval_k," +
                          " sum(reval_d+reval_d_kub) as reval_d," +
                          " sum(Round(case when " + DBManager.sNvlWord + "(tarif_hvs,0)>0 then -1*reval_k_kub/tarif_hvs  else 0 end,5)) as c_reval_k_kub," +
                          " sum(Round(case when " + DBManager.sNvlWord + "(a.tarif_gkal,0)>0 then -1*reval_k/a.tarif_gkal  else 0 end,5)) as c_reval_k_gkal," +
                          " sum(Round(case when " + DBManager.sNvlWord + "(tarif_hvs,0)>0 then reval_d_kub/tarif_hvs else 0 end,5)) as c_reval_d_kub," +
                          " sum(Round(case when " + DBManager.sNvlWord + "(a.tarif_gkal,0)>0 then reval_d/a.tarif_gkal else 0 end,5)) as c_reval_d_gkal," +
                          " sum(sum_charge) as sum_charge, " +
                          " sum(c_calc_kub) as c_calc_kub, " +
                          " sum(c_calc_kub_odn) as c_calc_kub_odn, " +
                          " sum(c_calc_gkal) as c_calc_gkal, " +
                          " sum(c_calc_gkal_odn) as c_calc_gkal_odn, " +
                          " max(pl_kvar) as pl_kvar " +
                          " from t1 a LEFT OUTER JOIN " + pref + DBManager.sKernelAliasRest + "formuls f " +
                          " on a.nzp_frm=f.nzp_frm  and abs(" + DBManager.sNvlWord + "(rsum_tarif,0))+  " +
                          " abs(" + DBManager.sNvlWord + "(sum_nedop,0)) + abs(" + DBManager.sNvlWord + "(reval_k,0))+ " +
                          " abs(" + DBManager.sNvlWord + "(reval_d,0)) + " +
                          " abs(" + DBManager.sNvlWord + "(sum_charge,0))+" +
                          " abs(" + DBManager.sNvlWord + "(c_calc_gkal,0))+" +
                          " abs(" + DBManager.sNvlWord + "(count_gil,0))+" +
                          " abs(" + DBManager.sNvlWord + "(pl_kvar,0)) >0.001 " +
                          " group by 1,2,3,4,5";
                    ExecSQL(connDb, sql, true);
                    DataTable d1 = DBManager.ExecSQLToTable(connDb, "select * from t1 where reval_k_kub<>0");

                    ExecSQL(connDb, "drop table t1", true);
                    d1 = DBManager.ExecSQLToTable(connDb, "select * from t_svod where reval_k<>0");


                }

            }
            reader.Close();
            #endregion
            ExecSQL(connDb, "drop table t_dom", false);

            sql = "create temp table t_dom (nzp_dom integer, nzp_kvar integer, nzp_geu integer)" +
                  DBManager.sUnlogTempTable;
            ExecSQL(connDb, sql, true);

            sql = " insert into t_dom(nzp_dom, nzp_kvar, nzp_geu)" +
                  " select nzp_dom, nzp_kvar, nzp_geu " +
                  " from t_svod group by 1,2,3 ";
            ExecSQL(connDb, sql, true);

            ExecSQL(connDb, "drop table t_frm", false);

            sql = "create temp table t_frm (name_frm char(100))" + DBManager.sUnlogTempTable;
            ExecSQL(connDb, sql, true);

            sql = " insert into t_frm (name_frm) " +
                  " select name_frm from t_svod group by 1  ";
            ExecSQL(connDb, sql, true);


            sql = " insert into t_svod(nzp_geu,  nzp_dom, nzp_kvar, name_frm) " +
                  " select nzp_geu,  nzp_dom, nzp_kvar, name_frm from t_dom, t_frm ";
            ExecSQL(connDb, sql, true);

            ExecSQL(connDb, "drop table t_dom", true);
            ExecSQL(connDb, "drop table t_frm", true);
            ExecSQL(connDb, "drop table sel_kvar_10", true);

            ExecSQL(connDb, "drop table t_svod_all", false);


            sql = " create temp table t_svod_all( " +
                        " nzp_dom integer," +
                        " nzp_geu integer," +
                        " name_frm char(100)," +
                        " nzp_measure integer, " +
                        " count_gil integer," +
                        " rsum_tarif_all " + DBManager.sDecimalType + "(14,2), " +
                        " rsum_tarif " + DBManager.sDecimalType + "(14,2), " +
                        " sum_odn " + DBManager.sDecimalType + "(14,2), " +
                        " tarif " + DBManager.sDecimalType + "(14,3), " +
                        " c_calc_kub " + DBManager.sDecimalType + "(14,5), " +
                        " c_calc_gkal " + DBManager.sDecimalType + "(14,5), " +
                        " c_calc_kub_odn " + DBManager.sDecimalType + "(14,5), " +
                        " c_calc_gkal_odn " + DBManager.sDecimalType + "(18,7), " +
                        " sum_nedop " + DBManager.sDecimalType + "(14,2), " +
                        " reval_k " + DBManager.sDecimalType + "(14,2)," +
                        " reval_d " + DBManager.sDecimalType + "(14,2), " +
                        " c_nedop_kub " + DBManager.sDecimalType + "(18,5)," +
                        " c_nedop_gkal " + DBManager.sDecimalType + "(18,5)," +
                        " c_reval_k_kub " + DBManager.sDecimalType + "(18,5)," +
                        " c_reval_k_gkal " + DBManager.sDecimalType + "(18,5)," +
                        " c_reval_d_kub " + DBManager.sDecimalType + "(18,5), " +
                        " c_reval_d_gkal " + DBManager.sDecimalType + "(18,5), " +
                        " pl_kvar " + DBManager.sDecimalType + "(14,4), " +
                        " sum_charge " + DBManager.sDecimalType + "(14,2))" + DBManager.sUnlogTempTable;
            ExecSQL(connDb, sql, true);

            sql = "insert into t_svod_all(nzp_dom, nzp_geu,   " +
                  " name_frm, count_gil, rsum_tarif_all, rsum_tarif,  " +
                  " sum_odn,  tarif,  nzp_measure, sum_nedop, reval_k, " +
                  " reval_d, c_nedop_kub, c_nedop_gkal,  c_reval_k_kub, " +
                  " c_reval_k_gkal, c_reval_d_kub, c_reval_d_gkal," +
                  " sum_charge, c_calc_kub, c_calc_gkal, c_calc_kub_odn, c_calc_gkal_odn, " +
                  " pl_kvar)" +
                  " select nzp_dom, nzp_geu,   " +
                  " name_frm," +
                  " sum(" + DBManager.sNvlWord + "(count_gil,0)) as count_gil, " +
                  " sum(" + DBManager.sNvlWord + "(rsum_tarif_all,0)) as rsum_tarif_all,  " +
                  " sum(" + DBManager.sNvlWord + "(rsum_tarif,0)) as rsum_tarif,  " +
                  " sum(" + DBManager.sNvlWord + "(sum_odn,0)) as sum_odn,  " +
                  " max(" + DBManager.sNvlWord + "(a.tarif,0)) as tarif ," +
                  " max(" + DBManager.sNvlWord + "(a.nzp_measure,0)) as nzp_measure," +
                  " sum(" + DBManager.sNvlWord + "(sum_nedop,0)) as sum_nedop,  " +
                  " sum(-1*" + DBManager.sNvlWord + "(reval_k,0)) as reval_k, " +
                  " sum(" + DBManager.sNvlWord + "(reval_d,0)) as reval_d," +
                  " sum(" + DBManager.sNvlWord + "(c_nedop_kub,0)) as c_nedop_kub,  " +
                  " sum(" + DBManager.sNvlWord + "(c_nedop_gkal,0)) as c_nedop_gkal,  " +
                  " sum(" + DBManager.sNvlWord + "(c_reval_k_kub,0)) as c_reval_k_kub, " +
                  " sum(" + DBManager.sNvlWord + "(c_reval_k_gkal,0)) as c_reval_k_gkal, " +
                  " sum(" + DBManager.sNvlWord + "(c_reval_d_kub,0)) as c_reval_d_kub," +
                  " sum(" + DBManager.sNvlWord + "(c_reval_d_gkal,0)) as c_reval_d_gkal," +
                  " sum(" + DBManager.sNvlWord + "(sum_charge,0)) as sum_charge, " +
                  " sum(" + DBManager.sNvlWord + "(c_calc_kub,0)) as c_calc_kub, " +
                  " sum(" + DBManager.sNvlWord + "(c_calc_gkal,0)) as c_calc_gkal, " +
                  " sum(" + DBManager.sNvlWord + "(c_calc_kub_odn,0)) as c_calc_kub_odn, " +
                  " sum(" + DBManager.sNvlWord + "(c_calc_gkal_odn,0)) as c_calc_gkal_odn, " +
                  " sum(" + DBManager.sNvlWord + "(pl_kvar,0)) as pl_kvar " +
                  " from t_svod a group by 1,2,3 ";
            ExecSQL(connDb, sql, true);

            sql = "create index ix_tmp_svod_01 on t_svod_all(nzp_dom)";
            ExecSQL(connDb, sql, true);

            ExecSQL(connDb, DBManager.sUpdStat + " t_svod_all", true);

            ExecSQL(connDb, "drop table t_svod", true);
            sql = " update t_svod_all set sum_charge = rsum_tarif_all - sum_nedop - reval_k + reval_d";
            ExecSQL(connDb, sql, true);
            // var dt = DBManager.ExecSQLToTable(connDb, "select * from t_svod_all");

            sql =
                " select geu, trim(case when rajon = '-' then town else rajon end)||', '||trim(ulica) as ulica, ndom, Replace(nkor,'-','') as nkor, idom,  " +
                " name_frm," +
                " count_gil, " +
                " rsum_tarif_all,  " +
                " rsum_tarif,  " +
                " sum_odn,  " +
                " tarif ," +
                " nzp_measure," +
                " sum_nedop,  " +
                " reval_k, " +
                " reval_d," +
                " c_nedop_kub,  " +
                " c_nedop_gkal,  " +
                " c_reval_k_kub, " +
                " c_reval_k_gkal, " +
                " c_reval_d_kub," +
                " c_reval_d_gkal," +
                " sum_charge, " +
                " c_calc_kub, " +
                " c_calc_gkal, " +
                " c_calc_kub_odn, " +
                " c_calc_gkal_odn, " +
                " pl_kvar " +
                " from t_svod_all a, " +
                Points.Pref + DBManager.sDataAliasRest + "dom d, " +
                Points.Pref + DBManager.sDataAliasRest + "s_ulica su, " +
                Points.Pref + DBManager.sDataAliasRest + "s_rajon sr, " +
                Points.Pref + DBManager.sDataAliasRest + "s_town st, " +
                Points.Pref + DBManager.sDataAliasRest + "s_geu sg " +
                " where a.nzp_dom=d.nzp_dom " +
                "       and d.nzp_ul=su.nzp_ul " +
                "       and a.nzp_geu=sg.nzp_geu " +
                "       and su.nzp_raj=sr.nzp_raj " +
                "       and sr.nzp_town=st.nzp_town " +
                " order by geu, 2, idom, ndom, nkor, name_frm  ";
            DataTable localTable = null;
            try
            {

                localTable = DBManager.ExecSQLToTable(connDb, sql);
            }
            catch (Exception ex)
            {

                MonitorLog.WriteLog("ExcelReport : Ошибка при выборке начислений на экран " +
                ex.Message, MonitorLog.typelog.Error, true);
            }


            ExecSQL(connDb, "drop table t_svod_all", true);

            connDb.Close();




            return localTable;

        }
    }
}
