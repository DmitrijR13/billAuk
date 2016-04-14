using System;
using System.Data;
using STCLINE.KP50.Global;

using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.DataBase
{
    public partial class ExcelRep
    {

        public DataTable GetSpravSoderg8(Prm prm, out Returns ret, string nzpUser)
        {

            #region Подключение к БД
            IDbConnection connWeb = GetConnection(Constants.cons_Webdata);//new IDbConnection(Constants.cons_Webdata);
            MyDataReader reader;

            ret = OpenDb(connWeb, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("ExcelReport : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                return null;
            }


            IDbConnection connDb = GetConnection(Constants.cons_Kernel);//new IDbConnection(Constants.cons_Webdata);

            ret = OpenDb(connDb, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("ExcelReport : Ошибка при открытии соединения с локальной БД ", MonitorLog.typelog.Error, true);
                connWeb.Close();
                return null;
            }

            #endregion

            string tXXSpls = DBManager.GetFullBaseName(connWeb) + DBManager.tableDelimiter + "t" + nzpUser + "_spls";
            connWeb.Close();

            #region Выборка по локальным банкам


            ExecSQL(connDb, "drop table t_svod", false);

            string sql = " create temp table t_svod( " +
                         " nzp_dom integer," +
                         " nzp_kvar integer," +
                         " nzp_geu integer," +
                         " name_frm char(100)," +
                         " nzp_measure integer, " +
                         " count_gil integer," +
                         " rsum_tarif " + DBManager.sDecimalType + "(14,2), " +
                         " tarif " + DBManager.sDecimalType + "(14,3), " +
                         " c_calc_kv " + DBManager.sDecimalType + "(14,5), " +
                         " c_calc_gkal " + DBManager.sDecimalType + "(14,5), " +
                         " sum_nedop " + DBManager.sDecimalType + "(14,2), " +
                         " reval_k " + DBManager.sDecimalType + "(14,2)," +
                         " reval_d " + DBManager.sDecimalType + "(14,2), " +
                         " sum_otopl " + DBManager.sDecimalType + "(14,2), " +//Корректировка по итогам года
                         " c_otopl " + DBManager.sDecimalType + "(14,2), " +//Корректировка по итогам года
                         " c_nedop_kv " + DBManager.sDecimalType + "(14,5)," +
                         " c_nedop_gkal " + DBManager.sDecimalType + "(14,5)," +
                         " c_reval_k_kv " + DBManager.sDecimalType + "(14,5)," +
                         " c_reval_k_gkal " + DBManager.sDecimalType + "(14,5)," +
                         " c_reval_d_kv " + DBManager.sDecimalType + "(14,5), " +
                         " c_reval_d_gkal " + DBManager.sDecimalType + "(14,5), " +
                         " pl_kvar " + DBManager.sDecimalType + "(14,2), " +
                         " sum_charge " + DBManager.sDecimalType + "(14,2))" + DBManager.sUnlogTempTable;
            ExecSQL(connDb, sql, true);


            ExecSQL(connDb, "drop table sel_kvar_10", false);


            sql = " create temp table sel_kvar_10 (nzp_dom integer, " +
                  " nzp_geu integer,  nzp_kvar integer, pref char(10))" +
                  DBManager.sUnlogTempTable;
            ExecSQL(connDb, sql, true);

            sql = "insert into sel_kvar_10 (nzp_dom, nzp_geu, nzp_kvar, pref )" +
                  " select nzp_dom, nzp_geu, nzp_kvar, pref " +
                  " from " + tXXSpls + " k ";
            ExecSQL(connDb, sql, true);




            sql = " select pref " +
                  " from  sel_kvar_10 group by 1";

            ExecRead(connDb, out reader, sql, true);

            while (reader.Read())
            {
                if (reader["pref"] != null)
                {
                    string pref = Convert.ToString(reader["pref"]).Trim();
                    string sChargeAlias = pref + "_charge_" + (prm.year_ - 2000).ToString("00");


                    ExecSQL(connDb, " drop table sel_kvar10", false);

                    #region Заполнение sel_kvar10

                    sql = " create temp table sel_kvar10 ( " +
                          " nzp_kvar integer," +
                          " nzp_dom integer," +
                          " nzp_geu integer," +
                          " isol integer," +
                          " otop_koef " + DBManager.sDecimalType + "(14,5)," +
                          " pl_kvar  " + DBManager.sDecimalType + "(14,2)," +
                          " count_gil integer) " + DBManager.sUnlogTempTable;
                    ExecSQL(connDb, sql, true);


                    sql = " insert into sel_kvar10(nzp_dom, nzp_geu, nzp_kvar, count_gil, isol)" +
                          " select nzp_dom, nzp_geu, nzp_kvar, 0, 1 as isol " +
                          " from sel_kvar_10 k " +
                          " where pref='" + pref + "'";

                    if (prm.has_pu == "2")
                        sql += " and 0 < (select count(*)  " +
                            " from " + sChargeAlias + DBManager.tableDelimiter + "counters_" + prm.month_.ToString("00") +
                            " where stek=3 and nzp_type=1 and cnt_stage>0 and nzp_serv=" + prm.nzp_serv + ") ";
                    if (prm.has_pu == "3")
                        sql += " and 0 = (select count(*)  " +
                            " from " + sChargeAlias + DBManager.tableDelimiter + "counters_" + prm.month_.ToString("00") +
                            " where stek=3 and nzp_type=1 and cnt_stage>0 and nzp_serv=" + prm.nzp_serv + ") ";

                    ExecSQL(connDb, sql, true);


                    sql = "create index ix_tmpk_02 on sel_kvar10(nzp_kvar)";
                    ExecSQL(connDb, sql, true);


                    sql = DBManager.sUpdStat + " sel_kvar10 ";
                    ExecSQL(connDb, sql, true);



                    //Временно потом из calc_gku


                    sql = " update sel_kvar10 set pl_kvar=(select max(squ) " +
                          " from " + pref + "_charge_" + (prm.year_ - 2000).ToString("00") + DBManager.tableDelimiter + "calc_gku_" +
                          prm.month_.ToString("00") + " a" +
                          " where a.nzp_kvar=sel_kvar10.nzp_kvar and nzp_serv=8 and a.tarif>0)";
                    ExecSQL(connDb, sql, true);


                    sql = " update sel_kvar10 set (pl_kvar)=((select max(squ) " +
                          " from " + pref + "_charge_" + (prm.year_ - 2000).ToString("00") + DBManager.tableDelimiter + "calc_gku_" +
                          prm.month_.ToString("00") + " a" +
                          " where a.nzp_kvar=sel_kvar10.nzp_kvar and nzp_serv=8 and a.tarif>0)), (count_gil)=((select max(round(gil)) " +
                          " from " + pref + "_charge_" + (prm.year_ - 2000).ToString("00") + DBManager.tableDelimiter + "calc_gku_" +
                          prm.month_.ToString("00") + " a" +
                          " where a.nzp_kvar=sel_kvar10.nzp_kvar and nzp_serv=8 and a.tarif>0))";
                    ExecSQL(connDb, sql, true);

                    sql = " update sel_kvar10 set otop_koef=(select max(rsh2) " +
                       " from " + pref + "_charge_" + (prm.year_ - 2000).ToString("00") + DBManager.tableDelimiter + "calc_gku_" +
                       prm.month_.ToString("00") + " a" +
                       " where a.nzp_kvar=sel_kvar10.nzp_kvar and nzp_serv=8 and a.tarif>0)";
                    ExecSQL(connDb, sql, true);


                    ExecSQL(connDb, sql, true);

                    //Коммунальные

                    sql = " update sel_kvar10 set otop_koef=(select max(val_prm" + DBManager.sConvToNum + ") " +
                          " from " + pref + DBManager.sDataAliasRest + "prm_2 where nzp_prm=2074 and is_actual=1 " +
                          " and dat_s<=current_date and dat_po>=current_date and sel_kvar10.nzp_dom=nzp)" +
                          " where isol=2 ";

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
                          " count_gil integer, " +
                          " pl_kvar " + DBManager.sDecimalType + "(14,2), " +
                          " otop_koef " + DBManager.sDecimalType + "(14,5), " +
                          " tarif " + DBManager.sDecimalType + "(14,2), " +
                          " tarif_gkal " + DBManager.sDecimalType + "(14,2), " +
                          " sum_charge " + DBManager.sDecimalType + "(14,2), " +
                          " rsum_tarif " + DBManager.sDecimalType + "(14,2), " +
                          " sum_nedop " + DBManager.sDecimalType + "(14,2), " +
                          " sum_otopl " + DBManager.sDecimalType + "(14,2) default 0, " +
                          " sum_nedop_cor " + DBManager.sDecimalType + "(14,2) default 0, " +
                          " reval_k " + DBManager.sDecimalType + "(14,2), " +
                          " reval_d " + DBManager.sDecimalType + "(14,2), " +
                          " c_calc " + DBManager.sDecimalType + "(14,5)) " + DBManager.sUnlogTempTable;
                    ExecSQL(connDb, sql, true);


                    sql = " insert into t1(nzp_geu, nzp_dom, nzp_kvar, nzp_frm, " +
                          " pl_kvar, nzp_measure, otop_koef, tarif, count_gil, tarif_gkal," +
                          " sum_charge, rsum_tarif, sum_nedop,  reval_k, reval_d, c_calc)" +
                          " select nzp_geu, nzp_dom, a.nzp_kvar, nzp_frm, " +
                          " max(pl_kvar) as pl_kvar, sum(0) as nzp_measure, max(otop_koef) as otop_koef, " +
                          " max(tarif) as tarif, max(count_gil) as count_gil,max(tarif) as tarif_gkal," +
                          " sum(sum_charge) as sum_charge, " +
                          " sum(rsum_tarif) as rsum_tarif, sum(sum_nedop) as sum_nedop," +
                          " sum(case when real_charge<0 then real_charge else 0 end) +" +
                          " sum(case when reval<0 then reval else 0 end) as reval_k," +
                          " sum(case when real_charge>0 then real_charge else 0 end) +" +
                          " sum(case when reval>0 then reval else 0 end) as reval_d," +
                          " sum(c_calc) as c_calc" +
                          " from  " + sChargeAlias + DBManager.tableDelimiter + "charge_" +
                          prm.month_.ToString("00") + " a, sel_kvar10 b" +
                          " where nzp_serv = 8 " +
                          " and a.nzp_kvar=b.nzp_kvar " +
                          " and dat_charge is null and abs(rsum_tarif)+" +
                          " abs(reval)+abs(real_charge)+abs(tarif)>0.001 ";
                    if (prm.nzp_key > -1) //Добавляем поставщика
                    {
                        sql = sql + " and a.nzp_supp = " + prm.nzp_key;
                    }
                    sql = sql + " group by 1,2,3,4";



                    ExecSQL(connDb, sql, true);
                    //  DataTable dt = DBManager.ExecSQLToTable(connDb, "select sum(reval_k) from t1");

                    string sql7 = " UPDATE t1 set reval_k = reval_k - coalesce((SELECT reval  from(SELECT nzp_dom, a.nzp_kvar, sum(sum_rcl) as reval from " + sChargeAlias + ".perekidka " +
                   " a INNER JOIN " + pref + "_data.kvar b on b.nzp_kvar = a.nzp_kvar INNER JOIN fbill_data.document_base d on d.nzp_doc_base = a.nzp_doc_base where month_ = " +
                   prm.month_.ToString() + "  AND d.comment = 'Выравнивание сальдо' and nzp_serv in (8) group by 1,2) t " +
                   " where t1.nzp_dom = t.nzp_dom and t1.nzp_kvar = t.nzp_kvar), 0)";

                    if (!ExecSQL(connDb, sql7.ToString(), true).result)
                        return null;

                    #region корректировка отопления
                    sql = "update t1 set sum_otopl = " + DBManager.sNvlWord + "((select  " +
                         " sum(sum_rcl) " +
                         " from " + sChargeAlias + DBManager.tableDelimiter + "perekidka a " +
                         " where a.nzp_kvar=t1.nzp_kvar and type_rcl = 110 and month_=" + prm.month_.ToString("00") +
                         " and abs(sum_rcl)>0.001 and nzp_serv=8 ";
                    if (prm.nzp_key > -1) //Добавляем поставщика
                    {
                        sql = sql + " and a.nzp_supp = " + prm.nzp_key;
                    }
                    sql = sql + "),0) ";
                    ExecSQL(connDb, sql, true);

                    sql = " UPDATE t1 SET reval_k =reval_k - sum_otopl " +
                          " where sum_otopl<0 ";
                    ExecSQL(connDb, sql, true);
                    sql = " UPDATE t1 SET reval_d =reval_d - sum_otopl " +
                          " where sum_otopl>0 ";
                    ExecSQL(connDb, sql, true);
                    #endregion


                    #region Жигулевск недопоставка как перекидка
                    sql = "update t1 set sum_nedop_cor = (select  " +
                          " -sum(sum_rcl) " +
                          " from " + sChargeAlias + DBManager.tableDelimiter + "perekidka a " +
                          " where a.nzp_kvar=t1.nzp_kvar and type_rcl = 101 and month_=" + prm.month_.ToString("00") +
                          " and abs(sum_rcl)>0.001 and nzp_serv=8 ";
                    if (prm.nzp_key > -1) //Добавляем поставщика
                    {
                        sql = sql + " and a.nzp_supp = " + prm.nzp_key;
                    }
                    sql = sql + ") where reval_k<0 ";
                    ExecSQL(connDb, sql, true);

                    sql = "update t1 set sum_nedop = sum_nedop + " + DBManager.sNvlWord + "(sum_nedop_cor,0)";
                    ExecSQL(connDb, sql, true);
                    sql = "update t1 set reval_k = reval_k + " + DBManager.sNvlWord + "(sum_nedop_cor,0)";
                    ExecSQL(connDb, sql, true);
                    #endregion

                    #region Выборка перерасчетов прошлого периода

                    ExecSQL(connDb, "drop table t_nedop", false);
                    sql = "Create temp table t_nedop (nzp_geu integer," +
                          " nzp_dom integer, " +
                          " nzp_kvar integer, " +
                          " month_s integer, " +
                          " month_po integer)" + DBManager.sUnlogTempTable;
                    ExecSQL(connDb, sql, true);


                    sql = " insert into t_nedop (nzp_geu,nzp_dom,nzp_kvar, month_s, month_po)" +
                          " select b.nzp_geu,b.nzp_dom,a.nzp_kvar, min(EXTRACT (year FROM dat_s)*12+ EXTRACT (month FROM dat_s)) as month_s,  " +
                          " max(EXTRACT (year FROM dat_po)*12+EXTRACT (month FROM dat_po)) as month_po " +
                          " from " + pref + DBManager.sDataAliasRest + "nedop_kvar a, sel_kvar10 b " +
                          " where a.nzp_kvar=b.nzp_kvar and month_calc='01." + prm.month_.ToString("00") + "." + prm.year_.ToString("0000") + "'  " +
                          " group by 1,2,3";
                    ExecSQL(connDb, sql, true);


                    sql = " select month_, year_ " +
                          " from " + pref + "_charge_" + (prm.year_ - 2000).ToString("00") +
                          DBManager.tableDelimiter +
                          "lnk_charge_" + prm.month_.ToString("00") + " b, t_nedop d " +
                          " where  b.nzp_kvar=d.nzp_kvar " +
                          " and year_*12+month_>=month_s and  year_*12+month_<=month_po " +
                          " group by 1,2";
                    MyDataReader reader2;
                    ExecRead(connDb, out reader2, sql, true);
                    while (reader2.Read())
                    {
                        string sTmpAlias = pref + "_charge_" + (Int32.Parse(reader2["year_"].ToString()) - 2000).ToString("00");


                        sql =
                            " insert into t1 (nzp_geu, nzp_dom, nzp_kvar, nzp_frm,  tarif, sum_nedop, reval_k, reval_d)   " +
                            " select nzp_geu, nzp_dom, b.nzp_kvar, nzp_frm, 0,  " +
                            " sum(sum_nedop-sum_nedop_p),  " +
                            " sum(case when (sum_nedop-sum_nedop_p)>0 " +
                            " then sum_nedop-sum_nedop_p else 0 end ) as reval_k," +
                            " sum(case when (sum_nedop-sum_nedop_p)>0 " +
                            " then 0 else sum_nedop-sum_nedop_p end ) as reval_d" +
                            " from " + sTmpAlias + ":charge_" + Int32.Parse(reader2["month_"].ToString()).ToString("00") +
                            " b, t_nedop d " +
                            " where  b.nzp_kvar=d.nzp_kvar and dat_charge = date('28." +
                            prm.month_.ToString("00") + "." + prm.year_ + "')" +
                            " and abs(sum_nedop)+abs(sum_nedop_p)>0.001" +
                            " and nzp_serv = 8";
                        if (prm.nzp_key > -1) //Добавляем поставщика
                        {
                            sql = sql + " and nzp_supp = " + prm.nzp_key;
                        }
                        sql = sql + " group by 1,2,3,4,5";
                        ExecSQL(connDb, sql, true);

                    }
                    reader2.Close();

                    ExecSQL(connDb, "drop table t_nedop", true);
                    #endregion
                    #endregion
                    //        dt = DBManager.ExecSQLToTable(connDb, "select sum(reval_k) from t1");

                    ExecSQL(connDb, "drop table t2", false);

                    sql = " create temp table t2(nzp_kvar integer, nzp_frm integer, " +
                          " tarif_gkal " + DBManager.sDecimalType + "(14,6))" + DBManager.sUnlogTempTable;
                    ExecSQL(connDb, sql, true);

                    sql = " insert into t2(nzp_kvar, nzp_frm,tarif_gkal) " +
                          " select nzp_kvar, max(nzp_frm) as nzp_frm, max(tarif_gkal) " +
                          " from t1  where tarif>0 " +
                          " group by 1 ";
                    ExecSQL(connDb, sql, true);

                    ExecSQL(connDb, "create index ix_t2_w_01 on t2 (nzp_kvar)", true);
                    ExecSQL(connDb, "create index ix_t2_w_02 on t2 (nzp_kvar, nzp_frm)", true);
                    ExecSQL(connDb, DBManager.sUpdStat + " t2", true);

                    sql = " create index ixtmm_04 on t1(nzp_kvar)";
                    ExecSQL(connDb, sql, true);


                    ExecSQL(connDb, DBManager.sUpdStat + " t1", true);


                    sql = " update t1 set nzp_frm  = " + DBManager.sNvlWord + "((select nzp_frm " +
                          " from t2 where t1.nzp_kvar=t2.nzp_kvar),0) ";
                    ExecSQL(connDb, sql, true);


                    sql = " update t1 set nzp_frm  = " + DBManager.sNvlWord + "((select max(nzp_frm) " +
                          " from " + pref + DBManager.sDataAliasRest + "tarif f " +
                          " where t1.nzp_kvar=f.nzp_kvar and is_actual=1 and f.nzp_serv=8 and dat_po=" +
                          " (select max(dat_po) " +
                          " from " + pref + DBManager.sDataAliasRest + "tarif t " +
                          " where t.nzp_kvar=f.nzp_kvar and t.nzp_serv=8 and t.is_actual=1 )),0) where nzp_frm=0 ";
                    ExecSQL(connDb, sql, true);


                    sql = " update t1 set nzp_frm  = " + DBManager.sNvlWord + "((select max(nzp_frm) " +
                    " from " + pref + DBManager.sDataAliasRest + "tarif f " +
                    " where t1.nzp_kvar=f.nzp_kvar and is_actual=100 and f.nzp_serv=8 and dat_po=" +
                    " (select max(dat_po) " +
                    " from " + pref + DBManager.sDataAliasRest + "tarif t " +
                    " where t.nzp_kvar=f.nzp_kvar and t.nzp_serv=8 and t.is_actual=100 )),0) where nzp_frm=0 ";
                    ExecSQL(connDb, sql, true);


                    sql = " update t1 set tarif_gkal  = " + DBManager.sNvlWord + "((select max(tarif_gkal) " +
                          " from t2 where t1.nzp_kvar=t2.nzp_kvar),0) where coalesce(tarif_gkal,0) = 0";
                    ExecSQL(connDb, sql, true);


                    //Заполенение в случае, если нет начислений по услуге
                    ExecSQL(connDb, "drop table t2", true);

                    #region Заполенение в случае, если нет начислений по услуге

                    sql = " create temp table t2(nzp_dom integer,  " +
                          " tarif_gkal " + DBManager.sDecimalType + "(14,6))" + DBManager.sUnlogTempTable;
                    ExecSQL(connDb, sql, false);

                    sql = " insert into t2(nzp_dom, tarif_gkal) " +
                          " select nzp_dom, max(tarif_gkal) " +
                          " from t1  where tarif>0 " +
                          " group by 1 ";
                    ExecSQL(connDb, sql, true);

                    sql = " update t1 set tarif_gkal  = " + DBManager.sNvlWord + "((select max(tarif_gkal) " +
                     " from t2 where t1.nzp_dom=t2.nzp_dom),0) where tarif_gkal = 0";
                    ExecSQL(connDb, sql, true);


                    sql = " update t1 set tarif_gkal  = " + DBManager.sNvlWord + "((select max(tarif_gkal) " +
                          " from t2),0) where tarif_gkal = 0";
                    ExecSQL(connDb, sql, true);
                    ExecSQL(connDb, "drop table t2", true);
                    #endregion


                    sql = " create index ixtmm_01 on t1(nzp_frm)";
                    ExecSQL(connDb, sql, true);


                    ExecSQL(connDb, DBManager.sUpdStat + " t1", true);




                    sql = " update t1 set nzp_measure= " + DBManager.sNvlWord + "((select max(nzp_measure) " +
                          " from " + pref + DBManager.sKernelAliasRest + "formuls f " +
                          " where  t1.nzp_frm=f.nzp_frm),0) ";
                    ExecSQL(connDb, sql, true);


                    sql = " update t1 set c_calc = " + DBManager.sNvlWord + "(c_calc,0)*" +
                        DBManager.sNvlWord + "(otop_koef,0) " +
                          " where nzp_measure<>4";
                    ExecSQL(connDb, sql, true);

                    sql = " update t1 set nzp_dom = (select nzp_dom_base " +
                          " from " + pref + DBManager.sDataAliasRest + "link_dom_lit a" +
                          " where a.nzp_dom=t1.nzp_dom) " +
                          " where nzp_dom in (select nzp_dom from " + pref + DBManager.sDataAliasRest + "link_dom_lit)";
                    ExecSQL(connDb, sql, true);


                    #region Заполнение коэффициентов по домам
                    sql = " create temp table t2(nzp_dom integer, nzp_kvar integer,  " +
                          " otop_koef " + DBManager.sDecimalType + "(14,3))" + DBManager.sUnlogTempTable;
                    ExecSQL(connDb, sql, true);

                    sql = " insert into t2(nzp_dom,nzp_kvar, otop_koef) " +
                          " select nzp_dom,nzp_kvar,max(otop_koef) " +
                          " from t1   " +
                          " group by 1,2 ";
                    ExecSQL(connDb, sql, true);

                    sql = " update t1 set otop_koef  = " + DBManager.sNvlWord + "((select max(otop_koef) " +
                          " from t2 where t1.nzp_kvar=t2.nzp_kvar),0) where coalesce(otop_koef,0) = 0";
                    ExecSQL(connDb, sql, true);


                    sql = " update t1 set otop_koef  = " + DBManager.sNvlWord + "((select max(otop_koef) " +
                          " from t2 where t1.nzp_dom=t2.nzp_dom),0) where coalesce(otop_koef,0) = 0";
                    ExecSQL(connDb, sql, true);

                    sql = " update t1 set otop_koef  = " + DBManager.sNvlWord + "((select max(val_prm" + DBManager.sConvToNum + ") " +
                         " from " + pref + DBManager.sDataAliasRest + "prm_2 " +
                         " where t1.nzp_dom=nzp and nzp_prm=723 and is_actual=1 and dat_po>" + DBManager.sCurDate + "),0) where coalesce(otop_koef,0) = 0";
                    ExecSQL(connDb, sql, true);
                    ExecSQL(connDb, "drop table t2", true);
                    #endregion

                    //dt = DBManager.ExecSQLToTable(connDb, "select sum(reval_k) from t1");
                    //По всем

                    sql = " insert into t_svod(nzp_geu, nzp_dom, nzp_kvar, name_frm, nzp_measure, " +
                          " count_gil, rsum_tarif, tarif, " +
                          " sum_nedop, c_nedop_kv, c_nedop_gkal, reval_k, reval_d, " +
                          " c_reval_k_kv, c_reval_k_gkal, c_reval_d_kv, c_reval_d_gkal, sum_charge, " +
                          " c_calc_kv, c_calc_gkal, sum_otopl, c_otopl, pl_kvar )" +
                          " select nzp_geu,  a.nzp_dom,  a.nzp_kvar, " +
                          " trim(" + DBManager.sNvlWord +
                          "(name_frm,'Не определена формула'))||' норм.'||coalesce(otop_koef,0)||' : '||a.tarif_gkal as name_frm, " +
                          " f.nzp_measure, max(count_gil), sum(rsum_tarif), " +
                          " max(a.tarif_gkal) as tarif, " +
                          " sum(sum_nedop) as sum_nedop,  " +
                        //" sum(case when a.tarif>0 and f.nzp_measure<>4 then sum_nedop/a.tarif else 0 end) as c_nedop_kv, " +
                         " sum(0) as c_nedop_kv, " +
                        //" sum(case when a.tarif>0 and f.nzp_measure=4 then sum_nedop/a.tarif_gkal else 0 end) as c_nedop_gkal, " +
                          " sum(case when a.tarif_gkal>0 then sum_nedop/a.tarif_gkal else 0 end) as c_nedop_gkal, " +
                          " sum(reval_k) as reval_k," +
                          " sum(reval_d) as reval_d," +
                        //" sum(case when a.tarif>0 and f.nzp_measure<>4 then -1*reval_k/a.tarif else 0 end) as c_reval_k_kv," +
                          " sum(0) as c_reval_k_kv," +
                          " sum(case when a.tarif_gkal>0  then -1*reval_k/a.tarif_gkal else 0 end) as c_reval_k_gkal," +
                        //" sum(case when a.tarif>0 and f.nzp_measure<>4 then reval_d/a.tarif else 0 end) as c_reval_d_kv," +
                          " sum(0) as c_reval_d_kv," +
                          " sum(case when a.tarif_gkal>0  then reval_d/a.tarif_gkal else 0 end) as c_reval_d_gkal," +
                          " sum(sum_charge) as sum_charge, " +
                          " max(pl_kvar) as c_calc, " +
                          " sum(c_calc) as c_calc_gkal, " +
                          " sum(sum_otopl) as sum_otopl, " +
                          " sum(case when a.tarif_gkal>0  then sum_otopl/a.tarif_gkal else 0 end) as c_otopl," +
                          " max(pl_kvar) " +
                          " from t1 a LEFT OUTER JOIN " + pref + DBManager.sKernelAliasRest + "formuls f " +
                          " on a.nzp_frm=f.nzp_frm  " +
                          " group by 1,2,3,4,5";
                    ExecSQL(connDb, sql, true);

                    ExecSQL(connDb, "drop table t1", true);
                }

            }
            reader.Close();

            ExecSQL(connDb, "drop table t_dom", false);

            sql = "create temp table t_dom (nzp_dom integer, nzp_kvar integer, nzp_geu integer)" +
                  DBManager.sUnlogTempTable;
            ExecSQL(connDb, sql, true);

            sql = " insert into t_dom(nzp_dom,nzp_kvar, nzp_geu )" +
                  " select nzp_dom, nzp_kvar, nzp_geu from t_svod group by 1,2,3  ";

            ExecSQL(connDb, sql, true);


            sql = "create temp table t_frm (name_frm Char(100))" + DBManager.sUnlogTempTable;
            ExecSQL(connDb, sql, true);

            sql = " insert into t_frm  select name_frm from t_svod group by 1";
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
                  " rsum_tarif " + DBManager.sDecimalType + "(14,2), " +
                  " tarif " + DBManager.sDecimalType + "(14,3), " +
                  " c_calc_kv " + DBManager.sDecimalType + "(14,5), " +
                  " c_calc_gkal " + DBManager.sDecimalType + "(14,5), " +
                  " sum_nedop " + DBManager.sDecimalType + "(14,2), " +
                  " sum_otopl " + DBManager.sDecimalType + "(14,2), " +
                  " c_otopl " + DBManager.sDecimalType + "(14,5), " +
                  " reval_k " + DBManager.sDecimalType + "(14,2)," +
                  " reval_d " + DBManager.sDecimalType + "(14,2), " +
                  " c_nedop_kv " + DBManager.sDecimalType + "(14,5)," +
                  " c_nedop_gkal " + DBManager.sDecimalType + "(14,5)," +
                  " c_reval_k_kv " + DBManager.sDecimalType + "(14,5)," +
                  " c_reval_k_gkal " + DBManager.sDecimalType + "(14,5)," +
                  " c_reval_d_kv " + DBManager.sDecimalType + "(14,5), " +
                  " c_reval_d_gkal " + DBManager.sDecimalType + "(14,5), " +
                  " pl_kvar " + DBManager.sDecimalType + "(14,2), " +
                  " sum_charge " + DBManager.sDecimalType + "(14,2))" +
                  DBManager.sUnlogTempTable;
            ExecSQL(connDb, sql, true);


            sql = "insert into t_svod_all ( nzp_geu, nzp_dom,  " +
                  " name_frm, count_gil, rsum_tarif, tarif, nzp_measure," +
                  " sum_nedop, reval_k, reval_d, c_nedop_kv,  c_nedop_gkal,  " +
                  " c_reval_k_kv,  c_reval_k_gkal, c_reval_d_kv," +
                  " c_reval_d_gkal, sum_charge,  c_calc_kv,  c_calc_gkal, sum_otopl, " +
                  " c_otopl, pl_kvar)" +
                  " select nzp_geu, nzp_dom,  " +
                  " name_frm,sum(" + DBManager.sNvlWord + "(count_gil,0)) as count_gil, " +
                  " sum(" + DBManager.sNvlWord + "(rsum_tarif,0)) as rsum_tarif,  " +
                  " max(" + DBManager.sNvlWord + "(a.tarif,0)) as tarif ," +
                  " max(" + DBManager.sNvlWord + "(a.nzp_measure,0)) as nzp_measure," +
                  " sum(" + DBManager.sNvlWord + "(sum_nedop,0)) as sum_nedop,  " +
                  " sum(-1*" + DBManager.sNvlWord + "(reval_k,0)) as reval_k, " +
                  " sum(" + DBManager.sNvlWord + "(reval_d,0)) as reval_d," +
                  " sum(" + DBManager.sNvlWord + "(c_nedop_kv,0)) as c_nedop_kv,  " +
                  " sum(" + DBManager.sNvlWord + "(c_nedop_gkal,0)) as c_nedop_gkal,  " +
                  " sum(" + DBManager.sNvlWord + "(c_reval_k_kv,0)) as c_reval_k_kv, " +
                  " sum(" + DBManager.sNvlWord + "(c_reval_k_gkal,0)) as c_reval_k_gkal, " +
                  " sum(" + DBManager.sNvlWord + "(c_reval_d_kv,0)) as c_reval_d_kv," +
                  " sum(" + DBManager.sNvlWord + "(c_reval_d_gkal,0)) as c_reval_d_gkal," +
                  " sum(" + DBManager.sNvlWord + "(sum_charge,0)) as sum_charge, " +
                  " sum(" + DBManager.sNvlWord + "(c_calc_kv,0)) as c_calc_kv, " +
                  " sum(" + DBManager.sNvlWord + "(c_calc_gkal,0)) as c_calc_gkal, " +
                  " sum(" + DBManager.sNvlWord + "(sum_otopl,0)) as sum_otopl, " +
                  " sum(" + DBManager.sNvlWord + "(c_otopl,0)) as c_otopl, " +
                  " sum(" + DBManager.sNvlWord + "(pl_kvar,0)) as pl_kvar " +
                  " from t_svod a " +
                  " group by  1,2,3";
            ExecSQL(connDb, sql, true);

            sql = "create index ix_tmp_svod_01 on t_svod_all(nzp_dom)";
            ExecSQL(connDb, sql, true);

            ExecSQL(connDb, DBManager.sUpdStat + " t_svod_all", true);

            sql = " update t_svod_all set sum_charge = rsum_tarif - sum_nedop - reval_k + reval_d";
            ExecSQL(connDb, sql, true);

            ExecSQL(connDb, "drop table t_svod", true);
            sql = " select geu, trim(case when rajon = '-' then town else rajon end)||', '||trim(ulica) as ulica, ndom, Replace(nkor,'-','') as nkor, idom,  " +
                  " name_frm, count_gil, " +
                  " rsum_tarif,  " +
                  " tarif ," +
                  " nzp_measure," +
                  " sum_nedop,  " +
                  " reval_k, " +
                  " reval_d," +
                  " c_nedop_kv,  " +
                  " c_nedop_gkal,  " +
                  " c_reval_k_kv, " +
                  " c_reval_k_gkal, " +
                  " c_reval_d_kv," +
                  " c_reval_d_gkal," +
                  " sum_charge, " +
                  " c_calc_kv, " +
                  " c_calc_gkal, " +
                  " sum_otopl, " +
                  " c_otopl, " +
                  " pl_kvar " +
                  " from t_svod_all a, " +
                  Points.Pref + DBManager.sDataAliasRest + "dom d, " +
                  Points.Pref + DBManager.sDataAliasRest + "s_ulica su, " +
                  Points.Pref + DBManager.sDataAliasRest + "s_rajon sr, " +
                  Points.Pref + DBManager.sDataAliasRest + "s_town st, " +
                  Points.Pref + DBManager.sDataAliasRest + "s_geu sg " +
                  " where a.nzp_dom=d.nzp_dom " +
                  "     and d.nzp_ul=su.nzp_ul " +
                  "     and a.nzp_geu=sg.nzp_geu " +
                  "     and su.nzp_raj=sr.nzp_raj " +
                  "     and sr.nzp_town=st.nzp_town " +
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


            #endregion


            return localTable;

        }
    }
}
