// Подсчет расходов

#region Подключаемые модули

using System;
using System.Data;

using STCLINE.KP50.Global;
using STCLINE.KP50.IFMX.Kernel.source.CommonType;

#endregion Подключаемые модули

#region здесь производится подсчет расходов

namespace STCLINE.KP50.DataBase
{
   
    //здась находятся классы для подсчета расходов
    public partial class DbCalcCharge : DataBaseHead
    {
        #region Заполнение таблиц deltaxx по текущему месяцу  - UseDeltaCntsForMonth (вызывается при расчете дома/БД!)
        //--------------------------------------------------------------------------------
        bool UseDeltaCntsForMonth(IDbConnection conn_db, Rashod rashod, string pStekDltCnts, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns(); //CalcTypes.ParamCalc paramcalc

            ExecSQL(conn_db, " Drop table ttt_ans_delta", false);

            // выполнять только для текущего расчетного месяца
            if ((rashod.paramcalc.cur_yy == rashod.paramcalc.calc_yy) && (rashod.paramcalc.cur_mm == rashod.paramcalc.calc_mm))
            {
                // записать в стек =4 перекидки по объемам в текщем расчетном месяце

                #region удалить изменения расходов, сделанных в текущем месяце

                string sSumVal = "val1"; // поле где дельта расхода!

                // удалить изменения расходов вручную, сделанных в текущем месяце
                ret = ExecSQL(conn_db,
                      " delete from " + rashod.delta_xx_cur +
                      " Where stek=4 and year_=" + rashod.paramcalc.cur_yy + " and month_=" + rashod.paramcalc.cur_mm +
                      " and nzp_kvar in (select nzp_kvar from t_selkvar) "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // удалить перерасчет расходов, сделанный в текущем месяце, но записанный в прошлые месяцы (для последующего возможного снятия при перерасчете ОДН)
                if (pStekDltCnts == "3")
                {
                    // реализация в CalcCharge -> CreateChargeXX
                }
                // удалить перерасчет расходов, записанный для учета в текущем месяце
                // реализация в CalcCharge -> CreateChargeXX

                string revalXX_tab = rashod.reval_xx_cur;
                    //rashod.paramcalc.pref + "_charge_" + (rashod.paramcalc.cur_yy - 2000).ToString("00") + tableDelimiter + "reval_" + (rashod.paramcalc.cur_mm).ToString("00");

                ExecSQL(conn_db, " Drop table t_recalc_cnt ", false);
                ExecSQL(conn_db, " Drop table t_recalc_all ", false);

                #endregion удалить изменения расходов, сделанных в текущем месяце

                #region записать сумму перекидок по объемам, сделанных вручную

                string perekidka_tab = rashod.paramcalc.pref + "_charge_" + (rashod.paramcalc.cur_yy - 2000).ToString("00") + tableDelimiter + "perekidka ";

                ret = ExecSQL(conn_db,
                      " Insert into " + rashod.delta_xx_cur +
                      " (nzp_kvar, nzp_dom, nzp_serv, year_, month_, stek, val1, val2, val3, cnt_stage, kod_info, is_used)" +
                      " Select p.nzp_kvar, k.nzp_dom, p.nzp_serv, " + rashod.paramcalc.cur_yy + " year_, p.month_, 4 stek," +
                      " sum(p.volum) val1,0 val2,0 val3,0 cnt_stage,0 kod_info,0 is_used" +
                      " From " + perekidka_tab + " p, t_selkvar k " +
                      " Where p.nzp_kvar=k.nzp_kvar and p.type_rcl>100 and abs(p.volum)>0 and p.month_= " + rashod.paramcalc.cur_mm +
                      " group by 1,2,3,4,5 "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                #endregion записать сумму перекидок по объемам, сделанных вручную

                //
                #region если включен самарский алгоритм учета перерасчета расходов в расходе тек.месяца
                // ... beg если включен самарский алгоритм учета перерасчета расходов в расходе тек.месяца
                //1988|Учет перерасчета расходов ИПУ для Самары в текущем месяце|||bool||5||||
                bool bUchetRecalcRash = CheckValBoolPrm(conn_db, rashod.paramcalc.data_alias, 1988, "5");

                if (bUchetRecalcRash && (pStekDltCnts == "104"))
                {
                    MyDataReader reader;
                    sSumVal = "val1+val2";

                    ret = ExecSQL(conn_db,
                        " select b.nzp_kvar,k.nzp_dom,b.nzp_serv,b.nzp_supp,b.tarif,b.year_,b.month_," +
                        " sum(b.sum_tarif-b.sum_tarif_p) reval_tarif,max(f.nzp_measure) nzp_mea," +
                        " sum(b.c_calc) c_calc,sum(b.c_calcm_p) c_calcm_p,sum(b.c_calc_p) c_calc_p," +
                        " max(b.type_rsh) type_rsh,max(" + sDefaultSchema + "mdy(b.month_p,1,b.year_p)) dat_charge_p " +
#if PG
                        " into temp t_recalc_cnt " +
#else
#endif
                        " from " + revalXX_tab + " b, " + rashod.paramcalc.kernel_alias + "formuls f" +
                        ", t_selkvar k" +
                        " where k.nzp_kvar=b.nzp_kvar and b.nzp_frm=f.nzp_frm" +
                        " and b.tarif>0 and b.nzp_serv in (6,9,14,25) and f.nzp_measure in (3,4,5) " +
                        " and abs(b.reval) > 0.0001 " +
                        " and 0<(select count(*) from temp_counters a where b.nzp_kvar=a.nzp_kvar" +
                          " and (case when b.nzp_serv=14 then 9 else b.nzp_serv end)=a.nzp_serv" +
                          " and a.month_calc='01." + rashod.paramcalc.cur_mm + "." + rashod.paramcalc.cur_yy + "')" +
                        " group by 1,2,3,4,5,6,7 " +

                        " union all " +

                        " select b.nzp_kvar,k.nzp_dom,b.nzp_serv,b.nzp_supp,b.tarif,b.year_,b.month_," +
                        " sum(b.sum_tarif-b.sum_tarif_p) reval_tarif,max(f.nzp_measure) nzp_mea," +
                        " sum(b.c_calc) c_calc,sum(b.c_calcm_p) c_calcm_p,sum(b.c_calc_p) c_calc_p," +
                        " max(b.type_rsh) type_rsh,max(" + sDefaultSchema + "mdy(b.month_p,1,b.year_p)) dat_charge_p " +
                        " from " + revalXX_tab + " b, " + rashod.paramcalc.kernel_alias + "formuls f" +
                        ", t_selkvar k" +
                        " where k.nzp_kvar=b.nzp_kvar and b.nzp_frm=f.nzp_frm" +
                        " and b.tarif>0 and b.nzp_serv=7 and f.nzp_measure=3 " +
                        " and abs(b.reval) > 0.0001 " +
                        " and 0<(select count(*) from temp_counters a where b.nzp_kvar=a.nzp_kvar" +
                          " and a.nzp_serv in (6,9)" +
                          " and a.month_calc='01." + rashod.paramcalc.cur_mm + "." + rashod.paramcalc.cur_yy + "')" +
                        " group by 1,2,3,4,5,6,7 "

#if PG
#else
                        + " into temp t_recalc_cnt with no log "
#endif
                        , true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                    ret = ExecSQL(conn_db, " create index ix2_recalc_cnt on t_recalc_cnt(nzp_kvar,nzp_serv,nzp_supp) ", true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
                    ret = ExecSQL(conn_db, sUpdStat + " t_recalc_cnt ", true);

                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
                    //
                    ExecSQL(conn_db, " Drop table t_recalc_all", false);

                    ret = ExecSQL(conn_db,
                        " Select 0 nzp_delta, p.nzp_kvar, p.nzp_dom, p.nzp_serv, p.year_, p.month_," +
                        " max(p.tarif) val1, max(p.tarif) val2," +
                        " sum(case when p.nzp_mea=4 and p.nzp_serv=9 then p.reval_tarif/(p.tarif*0.0611) else  p.reval_tarif/p.tarif end) val3," +
                        " sum(case when p.nzp_mea=4 and p.nzp_serv=9 then p.c_calc/0.0611 else p.c_calc end) c_calc," +
                        " sum(case when p.nzp_mea=4 and p.nzp_serv=9 and p.type_rsh<>0 then p.c_calcm_p/0.0611 else p.c_calcm_p end) c_calcm_p," +
                        " sum(p.c_calc_p) c_calc_p," +
                        " 0 cnt_stage,-1 kod_info,0 is_used," +
                        " max(p.tarif) valm,max(p.tarif) valm_p, max(p.tarif) dop87,max(p.tarif) dop87_p," +
                        " max(p.type_rsh) type_rsh,max(p.dat_charge_p) dat_charge_p,0 is_good_rsh,max(p.tarif) rsh_all " +
#if PG
                        " into temp t_recalc_all " +
#else
#endif
                        " From t_recalc_cnt p Where 1=1 " +
                        " group by 2,3,4,5,6 "
#if PG
#else
                        +" into temp t_recalc_all with no log "
#endif
                        , true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                    ret = ExecSQL(conn_db, " create index ix_recalc_all on t_recalc_all(nzp_kvar,nzp_serv) ", true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                    ret = ExecSQL(conn_db, sUpdStat + " t_recalc_all ", true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                    ret = ExecSQL(conn_db, " update t_recalc_all set val1=0,val2=0,valm=0,valm_p=0,dop87=0,dop87_p=0,rsh_all=0 where 1=1 ", true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
                    //

                    //ViewTbl(conn_db, " select * from t_recalc_all ");

                    string sDateBegin = MDY(07, 01, 2013);
                    string countersXX_tab = "";
                    // 1. проставим расходы за текущий месяц расчета (начисляемый)
                    ret = ExecRead(conn_db, out reader,
                        " Select year_, month_, nzp_serv  From t_recalc_all " +
                        //" where mdy(month_,01,year_) >= " + sDateBegin +
                        " Group by 1,2,3 Order by 1,2,3 "
                        , true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                    try
                    {
                        while (reader.Read())
                        {
                            //
                            int tek_yy = 0;
                            int tek_mm = 0;
                            int tek_serv = 0;
                            if (reader["month_"] != DBNull.Value)
                            {
                                tek_mm = System.Convert.ToInt32(reader["month_"]);

                            }
                            if (reader["year_"] != DBNull.Value)
                            {
                                tek_yy = System.Convert.ToInt32(reader["year_"]);

                            }
                            if (reader["nzp_serv"] != DBNull.Value)
                            {
                                tek_serv = System.Convert.ToInt32(reader["nzp_serv"]);

                            }
                            if ((tek_mm > 0) && (tek_yy > 0) && (tek_serv > 0))
                            {
                                countersXX_tab = rashod.paramcalc.pref + "_charge_" + (tek_yy - 2000).ToString("00") + tableDelimiter +
                                    "counters" + (rashod.paramcalc.cur_yy - 2000).ToString("00") + (rashod.paramcalc.cur_mm).ToString("00") + "_" +
                                    (tek_mm).ToString("00");

                                ret = ExecSQL(conn_db,
                                  " update t_recalc_all set valm=" +
                                    "(select sum(t.val1+t.val2) from " + countersXX_tab + " t" +
                                    " where t.nzp_kvar=t_recalc_all.nzp_kvar and t.nzp_serv=(case when t_recalc_all.nzp_serv=14 then 9 else t_recalc_all.nzp_serv end)" +
                                    " and t.nzp_type=3 and t.stek=3" +
                                    " ) " +
                                  " where year_=" + tek_yy + " and month_=" + tek_mm + " and nzp_serv=" + tek_serv +
                                    " and 0<" +
                                    "(select count(*) from " + countersXX_tab + " t" +
                                    " where t.nzp_kvar=t_recalc_all.nzp_kvar and t.nzp_serv=(case when t_recalc_all.nzp_serv=14 then 9 else t_recalc_all.nzp_serv end)" +
                                    " and t.nzp_type=3 and t.stek=3" +
                                    " ) "
                                    , true);
                                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                                countersXX_tab = rashod.paramcalc.pref + "_charge_" + (tek_yy - 2000).ToString("00") + tableDelimiter +
                                    "counters_" + (tek_mm).ToString("00");
                                // ОДН вегда берется из первого месяца ! - не пересчитывается
                                ret = ExecSQL(conn_db,
                                  " update t_recalc_all set dop87_p=" +
                                    "(select sum(t.dop87) from " + countersXX_tab + " t" +
                                    " where t.nzp_kvar=t_recalc_all.nzp_kvar and t.nzp_serv=(case when t_recalc_all.nzp_serv=14 then 9 else t_recalc_all.nzp_serv end)" +
                                    " and t.nzp_type=3 and t.stek=3" +
                                    " ) " +
                                  " where year_=" + tek_yy + " and month_=" + tek_mm + " and nzp_serv=" + tek_serv +
                                    " and 0<" +
                                    "(select count(*) from " + countersXX_tab + " t" +
                                    " where t.nzp_kvar=t_recalc_all.nzp_kvar and t.nzp_serv=(case when t_recalc_all.nzp_serv=14 then 9 else t_recalc_all.nzp_serv end)" +
                                    " and t.nzp_type=3 and t.stek=3" +
                                    " ) "
                                    , true);
                                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
                            }
                            //
                        }
                    }
                    catch
                    {
                        ret.result = false;
                    }

                    reader.Close();

                    //ViewTbl(conn_db, " select * from t_recalc_all ");

                    // 2. проставим расходы за прошлый месяц расчета (перерасчитываемый)
                    ret = ExecRead(conn_db, out reader,
#if PG
                        " Select EXTRACT(year from dat_charge_p) year_p,EXTRACT(month from dat_charge_p) month_p," +
#else
                        " Select year(dat_charge_p) year_p,month(dat_charge_p) month_p," +
#endif
                        " nzp_serv,year_,month_  From t_recalc_all " +
                        " where (dat_charge_p >= " + sDateBegin + ") or dat_charge_p = " + MDY(1, 1, 1901) +
                        " Group by 1,2,3,4,5 Order by 1,2,3,4,5 "
                        , true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                    try
                    {
                        while (reader.Read())
                        {
                            //
                            int tek_yy = 0;
                            int tek_mm = 0;
                            int tekp_yy = 0;
                            int tekp_mm = 0;
                            int tek_serv = 0;
                            if (reader["month_p"] != DBNull.Value)
                            {
                                tekp_mm = System.Convert.ToInt32(reader["month_p"]);

                            }
                            if (reader["year_p"] != DBNull.Value)
                            {
                                tekp_yy = System.Convert.ToInt32(reader["year_p"]);

                            }
                            if (reader["nzp_serv"] != DBNull.Value)
                            {
                                tek_serv = System.Convert.ToInt32(reader["nzp_serv"]);

                            }
                            if (reader["month_"] != DBNull.Value)
                            {
                                tek_mm = System.Convert.ToInt32(reader["month_"]);

                            }
                            if (reader["year_"] != DBNull.Value)
                            {
                                tek_yy = System.Convert.ToInt32(reader["year_"]);

                            }
                            if ((tek_mm > 0) && (tek_yy > 0) && (tek_serv > 0))
                            {
                                string sAliasPref = (tekp_yy - 2000).ToString("00") + (tekp_mm).ToString("00");
                                countersXX_tab = rashod.paramcalc.pref + "_charge_" + (tek_yy - 2000).ToString("00") + tableDelimiter +
                                    "counters" + sAliasPref + "_" + (tek_mm).ToString("00");
                                if ((tekp_mm == 1) && (tekp_yy == 1901))
                                {
                                    countersXX_tab = rashod.paramcalc.pref + "_charge_" + (tek_yy - 2000).ToString("00") + tableDelimiter +
                                            "counters_" + (tek_mm).ToString("00");
                                }

                                ret = ExecSQL(conn_db,
                                  " update t_recalc_all set is_good_rsh=1, valm_p=" +
                                    "(select sum(t.val1+t.val2) from " + countersXX_tab + " t" +
                                    " where t.nzp_kvar=t_recalc_all.nzp_kvar and t.nzp_serv=(case when t_recalc_all.nzp_serv=14 then 9 else t_recalc_all.nzp_serv end)" +
                                    " and t.nzp_type=3 and t.stek=3" +
                                    " ) " +
#if PG
                                    " where EXTRACT(year from dat_charge_p)=" + tekp_yy + " and EXTRACT(month from dat_charge_p)=" + tekp_mm +
#else
                                    " where year(dat_charge_p)=" + tekp_yy + " and month(dat_charge_p)=" + tekp_mm + 
#endif
                                    " and nzp_serv=" + tek_serv +
                                    " and year_=" + tek_yy + " and month_=" + tek_mm +
                                    " and 0<" +
                                    "(select count(*) from " + countersXX_tab + " t" +
                                    " where t.nzp_kvar=t_recalc_all.nzp_kvar and t.nzp_serv=(case when t_recalc_all.nzp_serv=14 then 9 else t_recalc_all.nzp_serv end)" +
                                    " and t.nzp_type=3 and t.stek=3" +
                                    " ) "
                                    , true);
                                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
                            }
                            //
                        }
                    }
                    catch
                    {
                        ret.result = false;
                    }

                    reader.Close();

                    //ViewTbl(conn_db, " select * from t_recalc_all ");

                    ret = ExecSQL(conn_db,
                          " update t_recalc_all set valm_p = (case when type_rsh<>0 then c_calcm_p else c_calc_p end) " +
                          " Where is_good_rsh = 0 "
                        , true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
                    //
                    ret = ExecSQL(conn_db,
                          " update t_recalc_all" +
                        // сменили расчет??? 30.04.2014 - по словам Якименко - учитывать отрицательный ОДН не нужно!
                          " set rsh_all = valm," +
                          " val1 = (valm - valm_p) " +
                        //" set rsh_all = valm + ((case when dop87<0 then dop87 else 0 end)-(case when dop87_p<0 then dop87_p else 0 end))," +
                        //" val1 = (valm - valm_p) + ((case when dop87<0 then dop87 else 0 end)-(case when dop87_p<0 then dop87_p else 0 end)) " +
                          " Where 1 = 1 "
                        , true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
                    //
                    ret = ExecSQL(conn_db,
                          " Insert into " + rashod.delta_xx_cur +
                          " (nzp_kvar, nzp_dom, nzp_serv, year_, month_, stek, val1, val2, val3, cnt_stage, kod_info, is_used, valm, valm_p, dop87, dop87_p)" +
                          " Select p.nzp_kvar, p.nzp_dom, p.nzp_serv, " +
                          " p.year_, p.month_, " + pStekDltCnts + " stek," +
                          " sum( val1 ), 0 val2,sum(val3) val3,0 cnt_stage,-1 kod_info,0 is_used,sum(valm),sum(valm_p),sum(dop87),sum(dop87_p)" +
                          " From t_recalc_all p Where 1=1" +
                          " group by 1,2,3,4,5 "
                        , true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                    //string serial_fld = ",nzp_delta";  string serial_val = ",0";
                    ret = ExecSQL(conn_db,
                          " Insert into " + rashod.delta_xx_cur +
                          " (nzp_kvar, nzp_dom, nzp_serv, year_, month_, stek, val1, val2, val3, cnt_stage, kod_info, is_used, valm, valm_p, dop87, dop87_p)" +
                          " Select p.nzp_kvar, p.nzp_dom, p.nzp_serv, " +
                          rashod.paramcalc.cur_yy + " year_, " + rashod.paramcalc.cur_mm + " month_, 4 stek," +
                          " sum( val1 ), 0 val2,sum(val3) val3,0 cnt_stage,-1 kod_info,0 is_used,sum(valm),sum(valm_p),sum(dop87),sum(dop87_p)" +
                          " From t_recalc_all p Where 1=1" +
                          " group by 1,2,3,4,5 "
                        , true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                    ret = ExecSQL(conn_db,
                        " update " + revalXX_tab + " set kod_info=1" +
                        " where 0<(select count(*) from t_recalc_cnt a where a.nzp_kvar=" + revalXX_tab + ".nzp_kvar and a.nzp_serv=" + revalXX_tab + ".nzp_serv" +
                        " and a.nzp_supp=" + revalXX_tab + ".nzp_supp) "
                        , true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                    ExecSQL(conn_db, " Drop table t_recalc_cnt ", false);
                    ExecSQL(conn_db, " Drop table t_recalc_all", false);
                }
                // ... end если включен самарский алгоритм учета перерасчета расходов в расходе тек.месяца
                #endregion если включен самарский алгоритм учета перерасчета расходов в расходе тек.месяца

                // если включен стандартный алгоритм учета перерасчета расходов в расходе тек.месяца, то все записано в CalcReval -> LoadDeltaRashod()
                //if (pStekDltCnts == "3")

                #region учесть сумму дельт по ЛС

                // в текущем расчетном месяце записать учтенные дельты в месяцы, за которые они образовались
                ret = ExecSQL(conn_db,
                    " Create temp table ttt_ans_delta" +
                    "  ( nzp_delta  integer not null, " +
                    "    nzp_kvar   integer not null, " +
                    "    nzp_dom    integer not null, " +
                    "    nzp_serv   integer not null, " +
                    "    year_      integer not null, " +
                    "    month_     integer not null, " +
                    "    stek       integer not null, " +
                    "    val1       " + sDecimalType + "(15,7) default 0.00, " +
                    "    val2       " + sDecimalType + "(15,7) default 0.00, " +
                    "    val3       " + sDecimalType + "(15,7) default 0.00, " +
                    "    valm       " + sDecimalType + "(15,7) default 0.00, " +
                    "    valm_p     " + sDecimalType + "(15,7) default 0.00, " +
                    "    cnt_stage  integer not null, " +
                    "    kod_info   integer not null, " +
                    "    is_used    integer default 0 " +
                    "  ) " + sUnlogTempTable
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                ret = ExecSQL(conn_db,
                    " Insert into ttt_ans_delta" +
                      " (nzp_delta,nzp_kvar,nzp_dom,nzp_serv,year_,month_,stek,val1,val2,val3,cnt_stage,is_used,kod_info,valm,valm_p)" +
                    " Select " +
                       " nzp_delta,nzp_kvar,nzp_dom,nzp_serv,year_,month_,stek,val1,val2,val3,cnt_stage,is_used,kod_info,valm,valm_p" +
                    " From " + rashod.delta_xx_cur +
                    " Where stek in (4," + pStekDltCnts + ") and " + rashod.where_dom + rashod.where_kvar
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                ExecSQL(conn_db, " Create index ix1_ttt_ans_delta on ttt_ans_delta (nzp_dom,nzp_serv) ", true);
                ExecSQL(conn_db, " Create index ix2_ttt_ans_delta on ttt_ans_delta (nzp_kvar,nzp_serv,year_,month_) ", true);
                ExecSQL(conn_db, " Create index ix3_ttt_ans_delta on ttt_ans_delta (is_used,year_,month_) ", true);
                ExecSQL(conn_db, sUpdStat + " ttt_ans_delta ", true);

                // учеть рсход в ГКал в кубах!
                ret = ExecSQL(conn_db,
                    " update ttt_ans_delta" +
                    " set val1= valm/val2 - valm_p/val3 " +
                    " Where nzp_serv = 9 and stek in (" + pStekDltCnts + ") and kod_info=4 and val2>0.0001 and val3>0.0001 "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                //записать сумму дельт для ЛС дома с ОДПУ
                string sql =
                        " Update " + rashod.counters_xx + " Set " +
                        " dlt_reval =" +
                        " (select sum(case when stek <>4 then " + sSumVal + " else 0 end)" +
                        " from ttt_ans_delta k" +
                        "  where k.nzp_kvar=" + rashod.counters_xx + ".nzp_kvar and k.nzp_serv=" + rashod.counters_xx + ".nzp_serv)" +
                        ",dlt_real_charge =" +
                        " (select sum(case when stek = 4 then " + sSumVal + " else 0 end)" +
                        " from ttt_ans_delta k" +
                        "  where k.nzp_kvar=" + rashod.counters_xx + ".nzp_kvar and k.nzp_serv=" + rashod.counters_xx + ".nzp_serv)" +
                        " Where nzp_type = 3 and stek = 3 " + //and cnt_stage in (1,9)
                        " and " + rashod.where_dom +
                        "  and exists (select 1 from ttt_ans_delta k" +
                                " where k.nzp_kvar=" + rashod.counters_xx + ".nzp_kvar and k.nzp_serv=" + rashod.counters_xx + ".nzp_serv) ";

                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                #endregion учесть сумму дельт по ЛС

                #region записать сумму дельт для домов с ОДПУ
                //записать сумму дельт для домов с ОДПУ
                //
                ExecSQL(conn_db, " Drop table ttt_dlt_dom", false);
                ret = ExecSQL(conn_db,
                    " Create temp table ttt_dlt_dom" +
                    "  ( nzp_dom   integer not null, " +
                    "    nzp_serv  integer not null, " +
                    "    dlt       " + sDecimalType + "(15,7) default 0.00, " +
                    "    dlt_rcl   " + sDecimalType + "(15,7) default 0.00  " +
                    "  ) " + sUnlogTempTable
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                ret = ExecSQL(conn_db,
                    " Insert into ttt_dlt_dom (nzp_dom,nzp_serv,dlt,dlt_rcl) " +
                    " Select nzp_dom,nzp_serv," +
                    " sum(case when stek <>4 then " + sSumVal + " else 0 end) dlt," +
                    " sum(case when stek = 4 then " + sSumVal + " else 0 end) dlt_rcl" +
                    " From ttt_ans_delta " +
                    " Group by 1,2 "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                ExecSQL(conn_db, " Create unique index ix1_ttt_dlt_dom on ttt_dlt_dom (nzp_dom,nzp_serv) ", true);
                ExecSQL(conn_db, sUpdStat + " ttt_dlt_dom ", true);

                sql =
                        " Update " + rashod.counters_xx + " Set " +
                        " dlt_reval =" +
                        " (select d.dlt from ttt_dlt_dom d" +
                        "  where d.nzp_dom=" + rashod.counters_xx + ".nzp_dom and d.nzp_serv=" + rashod.counters_xx + ".nzp_serv)" +
                        ",dlt_real_charge =" +
                        " (select d.dlt_rcl from ttt_dlt_dom d" +
                        "  where d.nzp_dom=" + rashod.counters_xx + ".nzp_dom and d.nzp_serv=" + rashod.counters_xx + ".nzp_serv)" +
                        " Where nzp_type = 1 and stek = 3 and cnt_stage = 1 " +
                        "  and exists (select 1 from ttt_dlt_dom d" +
                                " where d.nzp_dom=" + rashod.counters_xx + ".nzp_dom and d.nzp_serv=" + rashod.counters_xx + ".nzp_serv)";

                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                #endregion записать сумму дельт для домов с ОДПУ

                #region записать сумму дельт для ГрПУ

                ExecSQL(conn_db, " Drop table ttt_dlt_dom", false);
                ret = ExecSQL(conn_db,
                    " Create temp table ttt_dlt_dom" +
                    "  ( nzp_counter integer not null, " +
                    "    dlt       " + sDecimalType + "(15,7) default 0.00, " +
                    "    dlt_rcl   " + sDecimalType + "(15,7) default 0.00  " +
                    "  ) " + sUnlogTempTable
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                ret = ExecSQL(conn_db,
                    " Insert into ttt_dlt_dom (nzp_counter,dlt,dlt_rcl) " +
                    " Select nzp_counter," +
                    " sum(case when stek <>4 then " + sSumVal + " else 0 end) dlt," +
                    " sum(case when stek = 4 then " + sSumVal + " else 0 end) dlt_rcl" +
                    " From ttt_ans_delta a,temp_counters_link b " +
                    " where a.nzp_kvar=b.nzp_kvar " +
                    " Group by 1 "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                ExecSQL(conn_db, " Create unique index ix1_ttt_dlt_dom on ttt_dlt_dom (nzp_counter) ", true);
                ExecSQL(conn_db, sUpdStat + " ttt_dlt_dom ", true);

                sql =
                        " Update " + rashod.counters_xx + " Set " +
                        " dlt_reval =" +
                        " (select d.dlt from ttt_dlt_dom d where d.nzp_counter=" + rashod.counters_xx + ".nzp_counter)" +
                        ",dlt_real_charge =" +
                        " (select d.dlt_rcl from ttt_dlt_dom d where d.nzp_counter=" + rashod.counters_xx + ".nzp_counter)" +
                        " Where nzp_type = 2 and stek = 3 and cnt_stage = 1 " +
                        "  and exists (select 1 from ttt_dlt_dom d where d.nzp_counter=" + rashod.counters_xx + ".nzp_counter) ";

                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                ExecSQL(conn_db, " Drop table ttt_dlt_dom", false);
                ExecSQL(conn_db, " Drop table ttt_ans_delta", false);

                #endregion записать сумму дельт для ГрПУ
            }
            else
            {
                // при перерасчетах в перерасчетном месяце снять учтенные дельты для избежания двойного учета,
                // если дельты были когда либо учтены

                // снимаем 1 раз!
            }
            return true;
        }
        #endregion Заполнение таблиц deltaxx

        #region отметить ученные суммы дельт по ЛС
        //--------------------------------------------------------------------------------
        private bool SetIsUseDeltaCntsLSForMonth(IDbConnection conn_db, Rashod rashod, string pStekDltCnts, out Returns ret)
            //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            MyDataReader reader;

            ExecSQL(conn_db, " drop table ttt_upd_delta ", false);
            ret = ExecSQL(conn_db,
                " Create temp table ttt_upd_delta " +
                "  ( nzp_delta integer not null, " +
                "    nzp_dom    integer not null, " +
                "    nzp_kvar   integer not null, " +
                "    nzp_serv   integer not null, " +
                "    month_     integer default 0, " +
                "    year_      integer default 0, " +
                "    stek       integer default 0, " +
                "    val1      " + sDecimalType + "(15,7) default 0.00, " +
                "    val2      " + sDecimalType + "(15,7) default 0.00, " +
                "    val3      " + sDecimalType + "(15,7) default 0.00, " +
                "    cnt_stage      integer default 0, " +
                "    kod_info      integer default 0, " +
                "    valm      " + sDecimalType + "(15,7) default 0.00, " +
                "    valm_p    " + sDecimalType + "(15,7) default 0.00, " +
                "    dop87     " + sDecimalType + "(15,7) default 0.00, " +
                "    dop87_p   " + sDecimalType + "(15,7) default 0.00  " +
                " )  " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                " insert into ttt_upd_delta " +
                  "(nzp_delta,nzp_kvar,nzp_dom,nzp_serv,year_,month_,stek,val1,val2,val3,cnt_stage,kod_info,valm,valm_p,dop87,dop87_p)" +
                " select " +
                  " nzp_delta,nzp_kvar,nzp_dom,nzp_serv,year_,month_,stek,val1,val2,val3,cnt_stage,kod_info,valm,valm_p,dop87,dop87_p " +
                " from " + rashod.delta_xx_cur + " a " +
                " where stek in (4," + pStekDltCnts + ") and " + rashod.where_dom +
                  " and exists (select 1 from " + rashod.counters_xx + " b " +
                    " Where a.nzp_dom=b.nzp_dom and a.nzp_serv=b.nzp_serv " +
                    " and b.nzp_type=1 and b.stek = 3 and b.kod_info in (21,22,23,26,27,31,32,33,36) " +
                    " and abs(b.dlt_reval + b.dlt_real_charge)>0.000001) "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " Drop table ttt_upd_dpux ", false);
            ret = ExecSQL(conn_db,
                " Create temp table ttt_upd_dpux " +
                " ( nzp_delta integer " +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                " Insert into ttt_upd_dpux (nzp_delta) " +
                " Select nzp_delta" +
                " From ttt_upd_delta "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
            ExecSQL(conn_db, " Create index ix_ttt_upd_dpux on ttt_upd_dpux (nzp_delta) ", false);
            ExecSQL(conn_db, sUpdStat + " ttt_upd_dpux ", false);

            ret = ExecSQL(conn_db,
                " insert into ttt_upd_delta " +
                  "(nzp_delta,nzp_kvar,nzp_dom,nzp_serv,year_,month_,stek,val1,val2,val3,cnt_stage,kod_info,valm,valm_p,dop87,dop87_p)" +
                " select " +
                  " nzp_delta,nzp_kvar,nzp_dom,nzp_serv,year_,month_,stek,val1,val2,val3,cnt_stage,kod_info,valm,valm_p,dop87,dop87_p " +
                " from " + rashod.delta_xx_cur + " a " +
                " where stek in (4," + pStekDltCnts + ") and " + rashod.where_dom +
                  " and exists (select 1 from " + rashod.counters_xx + " b, temp_counters_link l " +
                    " Where a.nzp_kvar=l.nzp_kvar and l.nzp_counter=b.nzp_counter and a.nzp_serv=b.nzp_serv " +
                    " and b.nzp_type=2 and b.stek = 3 and b.kod_info in (21,22,23,26,27,31,32,33,36) " +
                    " and abs(b.dlt_reval + b.dlt_real_charge)>0.000001) " +
                  " and not exists (select 1 from ttt_upd_dpux d where d.nzp_delta=a.nzp_delta) "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " Create index ix1_ttt_upd_delta on ttt_upd_delta (nzp_kvar,nzp_serv,year_,month_) ", true);
            ExecSQL(conn_db, " Create index ix2_ttt_upd_delta on ttt_upd_delta (nzp_delta) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_upd_delta ", true);

            ExecSQL(conn_db, " Drop table ttt_upd_dpux ", false);

            // выборка такая же, как для формирования ttt_ans_delta!
            ret = ExecSQL(conn_db,
                " Update " + rashod.delta_xx_cur +
                " Set is_used = 1" +
                " Where exists (select 1 from ttt_upd_delta d where d.nzp_delta=" + rashod.delta_xx_cur +".nzp_delta) "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // для стандартного учета дельт расходов записать в прошлое для последущего снятия при возможном перерасчете ОДН
            if (pStekDltCnts == "3")
            {
                ret = ExecRead(conn_db, out reader,
                    " Select year_, month_ " +
                    " From ttt_upd_delta " +
                    " Where stek = 3 " +
                    " Group by 1,2 " +
                    " Order by 1,2 "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                try
                {
                    while (reader.Read())
                    {
                        //
                        int tek_yy = 0;
                        int tek_mm = 0;
                        if (reader["month_"] != DBNull.Value)
                        {
                            tek_mm = System.Convert.ToInt32(reader["month_"]);

                        }
                        if (reader["year_"] != DBNull.Value)
                        {
                            tek_yy = System.Convert.ToInt32(reader["year_"]);

                        }
                        if ((tek_mm > 0) && (tek_yy > 0))
                        {
                            string deltaXX_tab = rashod.paramcalc.pref + "_charge_" + (tek_yy - 2000).ToString("00") + tableDelimiter +
                                                 "delta_" + (tek_mm).ToString("00");

                            // проверить - есть таблица для удаления в прошлом?
                            if (TempTableInWebCashe(conn_db, deltaXX_tab))
                            {
                                // вставить в прошлое стек = 103 = 3 + 100! & kod_info = тек.год/месяц!
                                ret = ExecSQL(conn_db,
                                    " Insert into " + deltaXX_tab +
                                    " (nzp_kvar,nzp_dom,nzp_serv,year_,month_,stek,val1,val2,val3,cnt_stage,kod_info,is_used) " +
                                    " Select" +
                                    "  nzp_kvar,nzp_dom,nzp_serv,year_,month_, 103, val1,val2,val3,cnt_stage,kod_info," +
                                    rashod.paramcalc.cur_yy + " * 100 + " + rashod.paramcalc.cur_mm + " as is_used " +
                                    " From ttt_upd_delta " +
                                    " Where stek = 3 and year_=" + tek_yy + " and month_= " + tek_mm
                                    , true);
                                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
                            }
                        }
                        //
                    }
                }
                catch
                {
                    ret.result = false;
                }
                reader.Close();
            }
            ExecSQL(conn_db, " drop table ttt_upd_delta ", false);

            return true;
        }
        #endregion отметить ученные суммы дельт по ЛС

        #region Заполнение таблиц deltaxx по текущему месяцу в лиц счете - UseDeltaCntsLSForMonth

        //--------------------------------------------------------------------------------
        bool UseDeltaCntsLSForMonth(IDbConnection conn_db, Rashod rashod, string pStekDltCnts, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            ExecSQL(conn_db, " Drop table ttt_ans_delta", false);
            if ((rashod.paramcalc.cur_yy == rashod.paramcalc.calc_yy) && (rashod.paramcalc.cur_mm == rashod.paramcalc.calc_mm))
            {
                // в текущем расчетном месяце записать учтенные дельты в месяцы, за которые они образовались

                ret = ExecSQL(conn_db,
                    " Create temp table ttt_ans_delta" +
                    "  ( nzp_delta  integer not null, " +
                    "    nzp_kvar   integer not null, " +
                    "    nzp_dom    integer not null, " +
                    "    nzp_serv   integer not null, " +
                    "    year_      integer not null, " +
                    "    month_     integer not null, " +
                    "    stek       integer not null, " +
                    "    val1       " + sDecimalType + "(15,7) default 0.00, " +
                    "    val2       " + sDecimalType + "(15,7) default 0.00, " +
                    "    val3       " + sDecimalType + "(15,7) default 0.00, " +
                    "    cnt_stage  integer not null, " +
                    "    kod_info   integer not null, " +
                    "    is_used    integer default 0 " +
                    "  )  " + sUnlogTempTable
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                ret = ExecSQL(conn_db,
                      " Insert into ttt_ans_delta" +
                      " (nzp_delta,nzp_kvar,nzp_dom,nzp_serv,year_,month_,stek,val1,val2,val3,cnt_stage,kod_info,is_used)" +
                      " Select nzp_delta,nzp_kvar,nzp_dom,nzp_serv,year_,month_,stek,val1,val2,val3,cnt_stage,kod_info,is_used " +
                      " From " + rashod.delta_xx_cur +
                      " Where stek in (4," + pStekDltCnts + ") and " + rashod.where_dom + rashod.where_kvar
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                ExecSQL(conn_db, " Create index ix1_ttt_ans_delta on ttt_ans_delta (nzp_dom,nzp_serv) ", true);
                ExecSQL(conn_db, " Create index ix2_ttt_ans_delta on ttt_ans_delta (nzp_kvar,nzp_serv,year_,month_) ", true);
                ExecSQL(conn_db, " Create index ix3_ttt_ans_delta on ttt_ans_delta (is_used,year_,month_) ", true);
                ExecSQL(conn_db, sUpdStat + " ttt_ans_delta ", true);
            }
            return true;
        }

        #endregion Заполнение таблиц deltaxx по текущему месяцу в лиц счете - UseDeltaCntsLSForMonth

        #region запись дельт расходов при расчете ОДН за тек.месяц без перерасчета ОДН WriteDeltaCntsForMonth
        //--------------------------------------------------------------------------------
        //
        // ... запись дельт расходов при расчете ОДН за тек.месяц без перерасчета ОДН ...
        //
        // может эта функция вообще НЕ нужна! зачем вставлять в прошлое? 
        // ведь учитывается только дельта расходов, а она рассчитывается от предыдущего расхода в counters_XX!
        // если будет перерасчет ОДН, то пусть считает от реального расхода - т.е. нужно будет пересчитать ВЕСЬ период "дурацкого" расчета ОДН
        //
        bool WriteDeltaCntsForMonth(IDbConnection conn_db, Rashod rashod, CalcTypes.ParamCalc paramcalc, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            if ((rashod.paramcalc.cur_yy == rashod.paramcalc.calc_yy) && (rashod.paramcalc.cur_mm == rashod.paramcalc.calc_mm))
            {
                // для ОДН
                //
                ExecSQL(conn_db, " Drop table ttt_dlt_dom", false);
                ret = ExecSQL(conn_db,
                    " Create temp table ttt_dlt_dom" +
                    "  ( nzp_dom    integer not null, " +
                    "    nzp_serv   integer not null  " +
                    "  ) " + sUnlogTempTable
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                ret = ExecSQL(conn_db,
                  " insert into ttt_dlt_dom (nzp_dom, nzp_serv) " +
                  " Select nzp_dom, nzp_serv " +
                  " From " + rashod.counters_xx +
                  " Where nzp_type = 1 and stek = 3 and kod_info>0 and " + rashod.where_dom +
                  " group by 1,2 "
                , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                ExecSQL(conn_db, " Create unique index ix1_ttt_dlt_dom on ttt_dlt_dom (nzp_dom,nzp_serv) ", true);
                ExecSQL(conn_db, sUpdStat + " ttt_dlt_dom ", true);

                // вставить по месяцам в прошлом
                MyDataReader reader;
                ret = ExecRead(conn_db, out reader,
                      " Select a.year_, a.month_ From ttt_ans_delta a,ttt_dlt_dom d " +
                      " Where d.nzp_dom=a.nzp_dom and d.nzp_serv=a.nzp_serv and a.stek<>4 " +
                      " Group by 1,2 Order by 1,2 "
                      , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                try
                {
                    while (reader.Read())
                    {
                        int imn = (int)reader["month_"];
                        int iyr = (int)reader["year_"];

                        string delta_xx_rab = paramcalc.pref + "_charge_" + (iyr - 2000).ToString("00") +
                            tableDelimiter + "delta_" + imn.ToString("00");

                        ret = ExecSQL(conn_db,
                            " Insert into " + delta_xx_rab +
                            " (nzp_delta,nzp_kvar,nzp_dom,nzp_serv,year_,month_,stek,val1,val2,val3,cnt_stage,kod_info,is_used) " +
                            " Select" +
                            " 0,a.nzp_kvar,a.nzp_dom,a.nzp_serv," + rashod.paramcalc.cur_yy + "," + rashod.paramcalc.cur_mm +
                            ", 2, a.val1,a.val2,a.val3,a.cnt_stage,a.kod_info,a.is_used " +
                            " From ttt_ans_delta a,ttt_dlt_dom d " +
                            " Where d.nzp_dom=a.nzp_dom and d.nzp_serv=a.nzp_serv and a.year_=" + iyr + " and a.month_=" + imn
                            , true);
                        if (!ret.result)
                        {
                            reader.Close();
                            DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                            return false;
                        }
                    }
                    reader.Close();
                }
                catch (Exception ex)
                {
                    reader.Close();
                    ret.result = false;
                    ret.text = ex.Message;
                    return false;
                }
                //
            }
            else
            {
                // при перерасчетах в перерасчетном месяце снять учтенные дельты для избежания двойного учета,
                // если дельты были когда либо учтены

                // снимаем 1 раз!
            }
            return true;
        }
        #endregion запись дельт расходов при расчете ОДН за тек.месяц без перерасчета ОДН

    }
}

#endregion здесь производится подсчет расходов

