using System;
using System.Linq;
using System.Text;
using System.Data;
using System.Collections.Generic;
using System.Globalization;

using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.DataBase
{
    public partial class DbCalc : DbCalcClient
    {
        //---------------------------------------------------
        //расчет начислений kredit_xx
        //---------------------------------------------------
        struct KreditXX
        {
            public ParamCalc paramcalc;

            public string charge_xx;
            public string kredit_tab;
            public string kredit_xx;
            public string kredo_tab;
            public string kredo_data;
            public string prm_1;
            public string prm_5;
            public string tarif;
            public string prev_kredit_xx;

            public KreditXX(ParamCalc _paramcalc)
            {
#if PG
      paramcalc = _paramcalc;
                string cur_bd = paramcalc.pref + "_charge_" + (paramcalc.cur_yy - 2000).ToString("00");
                string calc_bd = paramcalc.pref + "_charge_" + (paramcalc.calc_yy - 2000).ToString("00");
                charge_xx = calc_bd + ".charge" + paramcalc.alias + "_" + paramcalc.calc_mm.ToString("00");
                kredo_tab = "kredit" + paramcalc.alias;
                kredo_data = paramcalc.pref + "_data." + kredo_tab;
                prm_1 = paramcalc.pref + "_data.prm_1 ";
                prm_5 = paramcalc.pref + "_data.prm_5 ";
                tarif = paramcalc.pref + "_data.tarif ";
                kredit_tab = kredo_tab + "_" + paramcalc.calc_mm.ToString("00");
                kredit_xx = calc_bd + "." + kredit_tab;
                prev_kredit_xx = paramcalc.pref + "_charge_" + (paramcalc.prev_calc_yy - 2000).ToString("00") + ".kredit_" + paramcalc.prev_calc_mm.ToString("00");
#else
                paramcalc = _paramcalc;
                string cur_bd = paramcalc.pref + "_charge_" + (paramcalc.cur_yy - 2000).ToString("00");
                string calc_bd = paramcalc.pref + "_charge_" + (paramcalc.calc_yy - 2000).ToString("00");
                charge_xx = calc_bd + ":charge" + paramcalc.alias + "_" + paramcalc.calc_mm.ToString("00");
                kredo_tab = "kredit" + paramcalc.alias;
                kredo_data = paramcalc.pref + "_data:" + kredo_tab;
                prm_1 = paramcalc.pref + "_data:prm_1 ";
                prm_5 = paramcalc.pref + "_data:prm_5 ";
                tarif = paramcalc.pref + "_data:tarif ";
                kredit_tab = kredo_tab + "_" + paramcalc.calc_mm.ToString("00");
                kredit_xx = calc_bd + ":" + kredit_tab;
                prev_kredit_xx = paramcalc.pref + "_charge_" + (paramcalc.prev_calc_yy - 2000).ToString("00") + ":kredit_" + paramcalc.prev_calc_mm.ToString("00");
#endif

                string ol_srv = "";
                if (Points.IsFabric)
                {
                    foreach (_Server server in Points.Servers)
                    {
                        if (Points.Point.nzp_server == server.nzp_server)
                        {
                            ol_srv = "@" + server.ol_server;
                            break;
                        }
                    }
                }
            }
        }



        //-----------------------------------------------------------------------------
        void CreateKreditXX(IDbConnection conn_db2, KreditXX kreditXX, out Returns ret)
        //-----------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            //*****************************************************************
            //kredit
            //*****************************************************************
            if (!TempTableInWebCashe(conn_db2, kreditXX.kredo_data))
            {
                string conn_kernel = Points.GetConnByPref(kreditXX.paramcalc.pref);
                IDbConnection conn_db = GetConnection(conn_kernel);
                //IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
                ret = OpenDb(conn_db, true);
                if (!ret.result)
                {
                    return;
                }
#if PG
                ret = ExecSQL(conn_db, " set search_path to '" + kreditXX.paramcalc.pref + "_data'", true);
#else
                ret = ExecSQL(conn_db, " Database " + kreditXX.paramcalc.pref + "_data", true);
#endif
#if PG
                ret = ExecSQL(conn_db,
                    " Create table " + kreditXX.kredo_tab +
                    " (  nzp_kredit     serial not null, " +
                    "    nzp_kvar       integer not null, " +
                    "    nzp_serv       integer not null, " +
                    "    dat_month      date not null, " +              //расчетный месяц, когда возникла рассрочка
                    "    dat_s          date not null, " +              //период действия = расчетный месяц + 1 год (+11 месяцев)
                    "    dat_po         date not null, " +
                    "    valid          integer not null, " +           //0-новый, 1-подтвержденный жителем, 2 - завершен, ....
                    "    sum_dolg       numeric(14,2) default 0.00, " + //зафиксированный кредит
                    "    perc           numeric(5,2) default 0.00 ) "   //зафиксированный процент
                          , true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return;
                }
                ret = ExecSQL(conn_db, " Create unique index are.ixkr1_" + kreditXX.paramcalc.alias + " on " + kreditXX.kredo_tab + " (nzp_kredit) ", true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return;
                }
                ret = ExecSQL(conn_db, " Create index are.ixkr2_" + kreditXX.paramcalc.alias + " on " + kreditXX.kredo_tab + " (nzp_kvar,dat_month,nzp_serv,dat_s,dat_po,valid) ", true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return;
                }
                ExecSQL(conn_db, " analyze " + kreditXX.kredo_tab, true);
#else
                ret = ExecSQL(conn_db,
                    " Create table are." + kreditXX.kredo_tab +
                    " (  nzp_kredit     serial not null, " +
                    "    nzp_kvar       integer not null, " +
                    "    nzp_serv       integer not null, " +
                    "    dat_month      date not null, " +              //расчетный месяц, когда возникла рассрочка
                    "    dat_s          date not null, " +              //период действия = расчетный месяц + 1 год (+11 месяцев)
                    "    dat_po         date not null, " +
                    "    valid          integer not null, " +           //0-новый, 1-подтвержденный жителем, 2 - завершен, ....
                    "    sum_dolg       decimal(14,2) default 0.00, " + //зафиксированный кредит
                    "    perc           decimal(5,2) default 0.00 ) "   //зафиксированный процент
                          , true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return;
                }
                ret = ExecSQL(conn_db, " Create unique index are.ixkr1_" + kreditXX.paramcalc.alias + " on " + kreditXX.kredo_tab + " (nzp_kredit) ", true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return;
                }
                ret = ExecSQL(conn_db, " Create index are.ixkr2_" + kreditXX.paramcalc.alias + " on " + kreditXX.kredo_tab + " (nzp_kvar,dat_month,nzp_serv,dat_s,dat_po,valid) ", true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return;
                }
                ExecSQL(conn_db, " Update statistics for table " + kreditXX.kredo_tab, true);
#endif

                conn_db.Close();
            }

            //*****************************************************************
            //kredit_xx
            //*****************************************************************
            if (!TempTableInWebCashe(conn_db2, kreditXX.kredit_xx))
            {
                string conn_kernel = Points.GetConnByPref(kreditXX.paramcalc.pref);
                IDbConnection conn_db = GetConnection(conn_kernel);
                ret = OpenDb(conn_db, true);
                if (!ret.result)
                {
                    return;
                }
#if PG
                ret = ExecSQL(conn_db, " set search_path to '" + kreditXX.paramcalc.pref + "_charge_" + (kreditXX.paramcalc.calc_yy - 2000).ToString("00")+"'", true);
#else
                ret = ExecSQL(conn_db, " database " + kreditXX.paramcalc.pref + "_charge_" + (kreditXX.paramcalc.calc_yy - 2000).ToString("00"), true);
#endif
#if PG
                ret = ExecSQL(conn_db,
                    " Create table are." + kreditXX.kredit_tab +
                    " (  nzp_kredx      serial not null, " +
                    "    nzp_kvar       integer not null, " +
                    "    nzp_serv       integer not null, " +
                    "    nzp_kredit     integer not null, " +
                    "    sum_indolg     numeric(14,2) default 0.00, " +  //остаток по кредиту их прошлого месяца - сколько оставалось погасить
                    "    sum_dolg       numeric(14,2) default 0.00, " +  //сумма основного долга в первом месяце, в остальных месяцах = 0
                    "    sum_odna12     numeric(14,2) default 0.00, " +  //ежемесячный платеж (1/12)
                    "    sum_perc       numeric(14,2) default 0.00, " +  //процент пользования - уходит в charge_xx в отдельную статью
                    "    sum_charge     numeric(14,2) default 0.00, " +  //к оплате жителем (= sum_odna12 + sum_perc)
                    "    sum_outdolg    numeric(14,2) default 0.00 ) "   //остаток по кредиту (= sum_indolg - sum_odna12, в последнем месяце надо выровнить общую сумму с kredit.sum_dolg)
                          , true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return;
                }
                ret = ExecSQL(conn_db, " Create unique index ixkrx1_" + kreditXX.paramcalc.alias + "_" + kreditXX.paramcalc.calc_mm.ToString("00") + 
                    " on " + kreditXX.kredit_xx + " (nzp_kredx) ", true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return;
                }
                ret = ExecSQL(conn_db, " Create index ixkrx2_" + kreditXX.paramcalc.alias + "_" + kreditXX.paramcalc.calc_mm.ToString("00") +
                    " on " + kreditXX.kredit_xx + " (nzp_kvar,nzp_serv) ", true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return;
                }
                ret = ExecSQL(conn_db, " Create index ixkrx3_" + kreditXX.paramcalc.alias + "_" + kreditXX.paramcalc.calc_mm.ToString("00") +
                    " on " + kreditXX.kredit_xx + " (nzp_kredit) ", true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return;
                }
                ExecSQL(conn_db, " analyze " + kreditXX.kredit_tab, true);
#else
                ret = ExecSQL(conn_db,
                    " Create table are." + kreditXX.kredit_tab +
                    " (  nzp_kredx      serial not null, " +
                    "    nzp_kvar       integer not null, " +
                    "    nzp_serv       integer not null, " +
                    "    nzp_kredit     integer not null, " +
                    "    sum_indolg     decimal(14,2) default 0.00, " +  //остаток по кредиту их прошлого месяца - сколько оставалось погасить
                    "    sum_dolg       decimal(14,2) default 0.00, " +  //сумма основного долга в первом месяце, в остальных месяцах = 0
                    "    sum_odna12     decimal(14,2) default 0.00, " +  //ежемесячный платеж (1/12)
                    "    sum_perc       decimal(14,2) default 0.00, " +  //процент пользования - уходит в charge_xx в отдельную статью
                    "    sum_charge     decimal(14,2) default 0.00, " +  //к оплате жителем (= sum_odna12 + sum_perc)
                    "    sum_outdolg    decimal(14,2) default 0.00 ) "   //остаток по кредиту (= sum_indolg - sum_odna12, в последнем месяце надо выровнить общую сумму с kredit.sum_dolg)
                          , true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return;
                }
                ret = ExecSQL(conn_db, " Create unique index are.ixkrx1_" + kreditXX.paramcalc.alias + "_" + kreditXX.paramcalc.calc_mm.ToString("00") +
                    " on " + kreditXX.kredit_xx + " (nzp_kredx) ", true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return;
                }
                ret = ExecSQL(conn_db, " Create index are.ixkrx2_" + kreditXX.paramcalc.alias + "_" + kreditXX.paramcalc.calc_mm.ToString("00") +
                    " on " + kreditXX.kredit_xx + " (nzp_kvar,nzp_serv) ", true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return;
                }
                ret = ExecSQL(conn_db, " Create index are.ixkrx3_" + kreditXX.paramcalc.alias + "_" + kreditXX.paramcalc.calc_mm.ToString("00") +
                    " on " + kreditXX.kredit_xx + " (nzp_kredit) ", true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return;
                }
                ExecSQL(conn_db, " Update statistics for table " + kreditXX.kredit_tab, true);
#endif

                conn_db.Close();
            }
        }

        //--------------------------------------------------------------------------------
        void DropTempTablesKredit(IDbConnection conn_db)
        //--------------------------------------------------------------------------------
        {
            ExecSQL(conn_db, " Drop table t_kredo ", false);
            ExecSQL(conn_db, " Drop table t_charge ", false);
            ExecSQL(conn_db, " Drop table t_perc ", false);
            ExecSQL(conn_db, " Drop table t_servp ", false);
        }

        //-----------------------------------------------------------------------------
        public bool CalcKreditXX(IDbConnection conn_db, ParamCalc paramcalc, out Returns ret)
        //-----------------------------------------------------------------------------
        {
            KreditXX kreditXX = new KreditXX(paramcalc);

            //---------------------------------------------------
            //выбрать множество лицевых счетов
            //---------------------------------------------------
            if (!TempTableInWebCashe(conn_db, "t_selkvar"))
            {
                ChoiseTempKvar(conn_db, ref paramcalc, out ret);
                if (!ret.result)
                {
                    conn_db.Close();
                    return false;
                }
            }
            CreateKreditXX(conn_db, kreditXX, out ret);
            if (!ret.result)
            {
                return false;
            }

            DropTempTablesKredit(conn_db);

            //почистить данные расчета
            ExecByStep(conn_db, kreditXX.kredit_xx, "nzp_kredx",
                " Delete From " + kreditXX.kredit_xx +
                " Where nzp_kvar in ( Select nzp_kvar From t_selkvar ) "
                , 50000, " ", out ret);
            if (!ret.result)
            {
                DropTempTablesKredit(conn_db);
                return false;
            }

            
            //почистить кредитные истории расчетного месяца
            ExecByStep(conn_db, kreditXX.kredo_data, "nzp_kredit",
                " Delete From " + kreditXX.kredo_data +
                " Where nzp_kvar in ( Select nzp_kvar From t_selkvar ) " +
                "   and dat_month = " + kreditXX.paramcalc.dat_s
                , 50000, " ", out ret);
            if (!ret.result)
            {
                DropTempTablesKredit(conn_db);
                return false;
            }
            


            //Порядок расчета:
            //1. Добавить отсутствующие в kredit данные из prm_1 (nzp_prm in () && и есть соответствующие рассчитанные услуги, valid->0)
            //2. Выбрать все валидные кредиты (kredit.valid in (0,1) )
            //3. По ним рассчитать и заполнить kreditXX
            //4. Перенести в charge_xx посчитанные проценты


            //действующие процентные услуги

#if PG
 ret = ExecSQL(conn_db,
                " Select distinct p.nzp_kvar, p.nzp_serv as nzp_serv_perc, 0 as perc, "+
                        " case when p.nzp_serv = 306 then 6  else " +
                        " case when p.nzp_serv = 307 then 9  else " +
                        " case when p.nzp_serv = 308 then 7  else " +
                        " case when p.nzp_serv = 309 then 8  else " +
                        " case when p.nzp_serv = 310 then 25 else " +
                        " case when p.nzp_serv = 311 then 10  " +
                        " end " +
                        " end " +
                        " end " +
                        " end " +
                        " end " +
                        " end as nzp_serv_cmnl, " +  //заодно сопоставим с коммунальными услугами
                        " max(p.nzp_supp) as nzp_supp " +
                " Into temp t_servp "+
                " From " + kreditXX.tarif + " p,  t_selkvar t " +
                " Where p.nzp_kvar = t.nzp_kvar " +
                "   and p.nzp_serv >= 306 and p.nzp_serv <= 311 " +
                "   and p.is_actual <> 100 " +
                "   and p.dat_po >= " + kreditXX.paramcalc.dat_s +
                "   and p.dat_s <= " + kreditXX.paramcalc.dat_po +
                " Group by 1,2,3,4 "
                , true);
#else
            ret = ExecSQL(conn_db,
                           " Select unique p.nzp_kvar, p.nzp_serv as nzp_serv_perc, 0 as perc, " +
                                   " case when p.nzp_serv = 306 then 6  else " +
                                   " case when p.nzp_serv = 307 then 9  else " +
                                   " case when p.nzp_serv = 308 then 7  else " +
                                   " case when p.nzp_serv = 309 then 8  else " +
                                   " case when p.nzp_serv = 310 then 25 else " +
                                   " case when p.nzp_serv = 311 then 10  " +
                                   " end " +
                                   " end " +
                                   " end " +
                                   " end " +
                                   " end " +
                                   " end as nzp_serv_cmnl, " +  //заодно сопоставим с коммунальными услугами
                                   " max(p.nzp_supp) as nzp_supp " +
                           " From " + kreditXX.tarif + " p,  t_selkvar t " +
                           " Where p.nzp_kvar = t.nzp_kvar " +
                           "   and p.nzp_serv >= 306 and p.nzp_serv <= 311 " +
                           "   and p.is_actual <> 100 " +
                           "   and p.dat_po >= " + kreditXX.paramcalc.dat_s +
                           "   and p.dat_s <= " + kreditXX.paramcalc.dat_po +
                           " Group by 1,2,3,4 " +
                           " Into temp t_servp With no log "
                           , true);
#endif
            if (!ret.result)
            {
                DropTempTablesKredit(conn_db);
                return false;
            }
#if PG
            ExecSQL(conn_db, " Create unique index ix1_t_servp on t_servp (nzp_kvar, nzp_serv_perc, nzp_supp) ", true);
            ExecSQL(conn_db, " Create        index ix2_t_servp on t_servp (nzp_kvar, nzp_serv_cmnl) ", true);
            ExecSQL(conn_db, " analyze t_servp ", true);
#else
            ExecSQL(conn_db, " Create unique index ix1_t_servp on t_servp (nzp_kvar, nzp_serv_perc, nzp_supp) ", true);
            ExecSQL(conn_db, " Create        index ix2_t_servp on t_servp (nzp_kvar, nzp_serv_cmnl) ", true);
            ExecSQL(conn_db, " Update statistics for table t_servp ", true);
#endif
            
            //проставить проценты по-умолчанию
#if PG
            ret = ExecSQL(conn_db, " Update t_servp " +
                " Set perc = ( Select max(val_prm::integer) From " + kreditXX.prm_5 + " p " +
                             " Where p.nzp_prm = 1122 " +
                             "   and p.is_actual <> 100 " +
                             "   and p.dat_po >= " + kreditXX.paramcalc.dat_s +
                             "   and p.dat_s <= " + kreditXX.paramcalc.dat_po +
                             " ) " +
               " Where 0  < (  Select max(1) From " + kreditXX.prm_5 + " p " +
                             " Where p.nzp_prm = 1122 " +
                             "   and p.is_actual <> 100 " +
                             "   and p.dat_po >= " + kreditXX.paramcalc.dat_s +
                             "   and p.dat_s <= " + kreditXX.paramcalc.dat_po +
                             " ) ", true);
#else
            ret = ExecSQL(conn_db,
                           " Update t_servp " +
                           " Set perc = ( Select max(val_prm) From " + kreditXX.prm_5 + " p " +
                                        " Where p.nzp_prm = 1122 " +
                                        "   and p.is_actual <> 100 " +
                                        "   and p.dat_po >= " + kreditXX.paramcalc.dat_s +
                                        "   and p.dat_s <= " + kreditXX.paramcalc.dat_po +
                                        " ) " +
                          " Where 0  < (  Select max(1) From " + kreditXX.prm_5 + " p " +
                                        " Where p.nzp_prm = 1122 " +
                                        "   and p.is_actual <> 100 " +
                                        "   and p.dat_po >= " + kreditXX.paramcalc.dat_s +
                                        "   and p.dat_s <= " + kreditXX.paramcalc.dat_po +
                                        " ) "
                       , true);
#endif
            if (!ret.result)
            {
                DropTempTablesKredit(conn_db);
                return false;
            }

            /*
            1116|Рассрочка(процент) по холодной воде   |||float||1|0|100|4|  6
            1117|Рассрочка(процент) по горячей воде    |||float||1|0|100|4|  9
            1118|Рассрочка(процент) по канализации     |||float||1|0|100|4|  7  
            1119|Рассрочка(процент) по отоплению       |||float||1|0|100|4|  8
            1120|Рассрочка(процент) по электроснабжению|||float||1|0|100|4|  25
            1121|Рассрочка(процент) по газу            |||float||1|0|100|4|  10 
            */

            //выбрать проценты кредита по услугам на лс
#if PG
 ret = ExecSQL(conn_db,
                " Select distinct p.nzp as nzp_kvar, " +
                    " case when p.nzp_prm = 1116 then 306 else " +
                    " case when p.nzp_prm = 1117 then 307 else " +
                    " case when p.nzp_prm = 1118 then 308 else " +
                    " case when p.nzp_prm = 1119 then 309 else " +
                    " case when p.nzp_prm = 1120 then 310 else " +
                    " case when p.nzp_prm = 1121 then 311  " +
                    " end " +
                    " end " +
                    " end " +
                    " end " +
                    " end " +
                    " end as nzp_serv_perc, " +
                    " max(replace( coalesce(p.val_prm,'0'), ',', '.')::integer) as perc " +                    
                " Into temp t_kredo  "+
                " From " + kreditXX.prm_1 + " p,  t_selkvar t " +
                " Where p.nzp = t.nzp_kvar " +
                "   and p.nzp_prm >= 1116 and p.nzp_prm <= 1121 " +
                "   and p.is_actual <> 100 " +
                "   and p.dat_po >= " + kreditXX.paramcalc.dat_s +
                "   and p.dat_s <= " + kreditXX.paramcalc.dat_po +
                " Group by 1,2 " 
                , true);
#else
            ret = ExecSQL(conn_db,
                           " Select unique p.nzp as nzp_kvar, " +
                               " case when p.nzp_prm = 1116 then 306 else " +
                               " case when p.nzp_prm = 1117 then 307 else " +
                               " case when p.nzp_prm = 1118 then 308 else " +
                               " case when p.nzp_prm = 1119 then 309 else " +
                               " case when p.nzp_prm = 1120 then 310 else " +
                               " case when p.nzp_prm = 1121 then 311  " +
                               " end " +
                               " end " +
                               " end " +
                               " end " +
                               " end " +
                               " end as nzp_serv_perc, " +
                               " max(replace( nvl(p.val_prm,'0'), ',', '.')+0) as perc " +
                           " From " + kreditXX.prm_1 + " p,  t_selkvar t " +
                           " Where p.nzp = t.nzp_kvar " +
                           "   and p.nzp_prm >= 1116 and p.nzp_prm <= 1121 " +
                           "   and p.is_actual <> 100 " +
                           "   and p.dat_po >= " + kreditXX.paramcalc.dat_s +
                           "   and p.dat_s <= " + kreditXX.paramcalc.dat_po +
                           " Group by 1,2 " +
                           " Into temp t_kredo With no log "
                           , true);
#endif
            if (!ret.result)
            {
                DropTempTablesKredit(conn_db);
                return false;
            }
#if PG
            ExecSQL(conn_db, " Create unique index ix1_t_kredo on t_kredo (nzp_kvar, nzp_serv_perc) ", true);
            ExecSQL(conn_db, " analyze t_kredo ", true);
            //проставить проценты кредита
            ret = ExecSQL(conn_db,
                " Update t_servp " +
                " Set perc = ( Select max(perc) From t_kredo p " +
                             " Where p.nzp_kvar = t_servp.nzp_kvar " +
                             "   and p.nzp_serv_perc = t_servp.nzp_serv_perc " + 
                             " ) " +
               " Where 0  < (  Select max(1) From t_kredo p " +
                             " Where p.nzp_kvar = t_servp.nzp_kvar " +
                             "   and p.nzp_serv_perc = t_servp.nzp_serv_perc " +
                             " ) "
            , true);
#else
            ExecSQL(conn_db, " Create unique index ix1_t_kredo on t_kredo (nzp_kvar, nzp_serv_perc) ", true);
            ExecSQL(conn_db, " Update statistics for table t_kredo ", true);
            //проставить проценты кредита
            ret = ExecSQL(conn_db,
                " Update t_servp " +
                " Set perc = ( Select max(perc) From t_kredo p " +
                             " Where p.nzp_kvar = t_servp.nzp_kvar " +
                             "   and p.nzp_serv_perc = t_servp.nzp_serv_perc " +
                             " ) " +
               " Where 0  < (  Select max(1) From t_kredo p " +
                             " Where p.nzp_kvar = t_servp.nzp_kvar " +
                             "   and p.nzp_serv_perc = t_servp.nzp_serv_perc " +
                             " ) "
            , true);
#endif
#if PG
  if (!ret.result)
            {
                DropTempTablesKredit(conn_db);
                return false;
            }
            //выбрать коммунальные начисления в тек. расчете
            ret = ExecSQL(conn_db,
                " Select a.nzp_kvar, a.nzp_serv_perc, a.nzp_supp, a.perc, sum(b.sum_real) as sum_real " + " Into temp t_charge " +
                " From t_servp a, " + kreditXX.charge_xx + " b " +
                " Where a.nzp_kvar = b.nzp_kvar " +
                "   and a.nzp_serv_cmnl = b.nzp_serv " +
                "   and b.dat_charge is null " +
                "   and b.sum_real > 0 " +
                " Group by 1,2,3,4 " 
            , true);
            if (!ret.result)
            {
                DropTempTablesKredit(conn_db);
                return false;
            }
            ExecSQL(conn_db, " Create index ix_t_charge on t_charge (nzp_kvar, nzp_serv_perc) ", true);
            ExecSQL(conn_db, " analyze t_charge ", true);
            /*
                                " (  nzp_kredit     serial not null, " +
                                "    nzp_kvar       integer not null, " +
                                "    nzp_serv       integer not null, " +
                                "    dat_month      date not null, " +
                                "    dat_s          date not null, " +
                                "    dat_po         date not null, " +
                                "    valid          integer not null, " +
                                "    sum_dolg       numeric(14,2) default 0.00, " +
                                "    perc           numeric(5,2) default 0.00 ) "
            */
            //вставить в кредитные истории
            ret = ExecSQL(conn_db,
                " Insert into " + kreditXX.kredo_data + " (nzp_kvar, nzp_serv, dat_month, dat_s, dat_po, valid, sum_dolg, perc) " +
                " Select nzp_kvar, nzp_serv_perc, " + kreditXX.paramcalc.dat_s + ", " +
                    kreditXX.paramcalc.dat_s + ", (" + kreditXX.paramcalc.dat_s + " + INTERVAL '11 months' MONTH), 0, sum_real, perc " +
                " From t_charge " +
                " Where perc > 0 "
            , true);
            if (!ret.result)
            {
                DropTempTablesKredit(conn_db);
                return false;
            }
            /*
                    " (  nzp_kredx      serial not null, " +
                    "    nzp_kvar       integer not null, " +
                    "    nzp_serv       integer not null, " +
                    "    nzp_kredit     integer not null, " +
                    "    sum_indolg     numeric(14,2) default 0.00, " +
                    "    sum_dolg       numeric(14,2) default 0.00, " +  //сумма основного долга в первом месяце, в остальных месяцах = 0
                    "    sum_odna12     numeric(14,2) default 0.00, " +
                    "    sum_perc       numeric(14,2) default 0.00, " +
                    "    sum_charge     numeric(14,2) default 0.00, " +
                    "    sum_outdolg    numeric(14,2) default 0.00 ) "
            */
            //начинаем считать рассрочку и проценты по текущим валидным кредитам (valid = 0,1)
            ExecSQL(conn_db, " Drop table t_kredo ", false);
            //t_kredo - сожержит все данные по рассрочке!!!
            ret = ExecSQL(conn_db,
                " Select a.nzp_kvar, b.nzp_serv_perc, a.nzp_kredit, b.nzp_supp,  " +
                        //сумма рассрочки(долга) в первом месяце, в остальных месяцах = 0
                      "  case when a.dat_month = " + kreditXX.paramcalc.dat_s + " then a.sum_dolg else 0 end as sum_dolg, " +
                        //ежемес. платеж                
                      "  (a.sum_dolg / 12) as sum_odna12, " +
                        //процент пользования
                      "   a.perc,  " +
                        //сумма процента
                      "  (a.sum_dolg / 12 * a.perc / 100) as sum_perc, " +
                      "  0 as kod " +
                      " Into temp t_kredo "+
                " From " + kreditXX.kredo_data + " a, t_servp b " +
                " Where a.nzp_kvar = b.nzp_kvar " +
                "   and a.nzp_serv = b.nzp_serv_perc " +
                "   and a.dat_po >= " + kreditXX.paramcalc.dat_s +
                "   and a.dat_s <= " + kreditXX.paramcalc.dat_po +
                "   and a.valid in (0,1) " 
                
            , true);
            if (!ret.result)
            {
                DropTempTablesKredit(conn_db);
                return false;
            }
            ExecSQL(conn_db, " Create unique index ix1_t_kredo on t_kredo (nzp_kvar, nzp_serv_perc) ", true);
            ExecSQL(conn_db, " Create unique index ix2_t_kredo on t_kredo (nzp_kredit) ", true);
            ExecSQL(conn_db, " Create        index ix4_t_kredo on t_kredo (kod) ", true);
            ExecSQL(conn_db, " analyze t_kredo ", true);
            //загоним данные в kreditXX
            ret = ExecSQL(conn_db,
                " Insert into " + kreditXX.kredit_xx + " ( nzp_kvar, nzp_serv, nzp_kredit, sum_odna12, sum_perc, sum_dolg ) " +
                " Select nzp_kvar, nzp_serv_perc, nzp_kredit, sum_odna12, sum_perc, sum_dolg " +
                " From t_kredo "
            , true);
            if (!ret.result)
            {
                DropTempTablesKredit(conn_db);
                return false;
            }
            //посчитать сальдо
            if (TempTableInWebCashe(conn_db, kreditXX.prev_kredit_xx))
            {
                ExecSQL(conn_db, " Drop table t_charge ", false);
                ret = ExecSQL(conn_db,
                    " Select a.nzp_kvar, nzp_serv, nzp_kredit, 0 as kod, sum(sum_outdolg) as sum_outdolg " +
                     " Into temp t_charge "+
                    " From " + kreditXX.prev_kredit_xx + " a, t_selkvar b " +
                    " Where a.nzp_kvar = b.nzp_kvar " +
                    "   and abs(a.sum_outdolg) > 0.001 " +
                    " Group by 1,2,3,4  "                    
                , true);
                if (!ret.result)
                {
                    DropTempTablesKredit(conn_db);
                    return false;
                }
                ExecSQL(conn_db, " Create index ix1_t_charge on t_charge (nzp_kredit) ", true);
                ExecSQL(conn_db, " Create index ix2_t_charge on t_charge (kod) ", true);
                ExecSQL(conn_db, " analyze t_charge ", true);
                ret = ExecSQL(conn_db,
                    " Update t_charge " +
                    " Set kod = 1 " +
                    " Where nzp_kredit in ( Select nzp_kredit From " + kreditXX.kredit_xx + " ) "
                , true);
                if (!ret.result)
                {
                    DropTempTablesKredit(conn_db);
                    return false;
                }
                ret = ExecSQL(conn_db,
                    " Insert into " + kreditXX.kredit_xx + " ( nzp_kvar, nzp_serv, nzp_kredit, sum_indolg ) " +
                    " Select nzp_kvar, nzp_serv, nzp_kredit, sum_outdolg " +
                    " From t_charge " +
                    " Where kod = 0 "
                , true);
                if (!ret.result)
                {
                    DropTempTablesKredit(conn_db);
                    return false;
                }
                ret = ExecSQL(conn_db,
                    " Update " + kreditXX.kredit_xx +
                    " Set sum_indolg = ( Select sum(sum_outdolg) From t_charge a " +
                                       " Where " + kreditXX.kredit_xx + ".nzp_kredit = a.nzp_kredit ) " +
                    " Where nzp_kredit in ( Select nzp_kredit From t_charge ) "
                , true);
                if (!ret.result)
                {
                    DropTempTablesKredit(conn_db);
                    return false;
                }
            }
            //расчет сальдо
            ret = ExecSQL(conn_db,
                " Update " + kreditXX.kredit_xx +
                " Set sum_outdolg = sum_indolg - sum_odna12 + sum_dolg " +
                " Where nzp_kvar in ( Select nzp_kvar From t_selkvar ) "
            , true);
            if (!ret.result)
            {
                DropTempTablesKredit(conn_db);
                return false;
            }
            //выровнить sum_odna12
            ret = ExecSQL(conn_db,
                " Update " + kreditXX.kredit_xx +
                " Set sum_outdolg = 0, "+
                "     sum_odna12 = sum_indolg + sum_dolg " +
                " Where nzp_kvar in ( Select nzp_kvar From t_selkvar ) " +
                "   and sum_outdolg < 0 "
            , true);
            if (!ret.result)
            {
                DropTempTablesKredit(conn_db);
                return false;
            }
            ret = ExecSQL(conn_db,
                " Update " + kreditXX.kredit_xx +
                " Set sum_charge = sum_odna12 + sum_perc " +
                " Where nzp_kvar in ( Select nzp_kvar From t_selkvar ) "
            , true);
            if (!ret.result)
            {
                DropTempTablesKredit(conn_db);
                return false;
            }

            //затем проценты загоним в charge_xx в отдельные услуги

            // НЕ СОХРАНЯТЬ РАССРОЧКУ В CHARGE
            
            /*ret = ExecSQL(conn_db, //выбрать текущие строки из начислений
                " Update t_kredo " +
                " Set kod = 1 " +
                " Where 0 < ( Select max(1) From " + kreditXX.charge_xx + " b " +
                            " Where t_kredo.nzp_kvar = b.nzp_kvar " +
                            "   and t_kredo.nzp_serv_perc = b.nzp_serv " +
                            "   and t_kredo.nzp_supp = b.nzp_supp " +
                            "   and b.dat_charge is null ) "
            , true);
            if (!ret.result)
            {
                DropTempTablesKredit(conn_db);
                return false;
            }
            //исправляем charge_xx
            ret = ExecSQL(conn_db,
                " Update " + kreditXX.charge_xx +
                " Set sum_real = ( Select sum(sum_odna12 + sum_perc) From t_kredo a " +
                                 " Where a.nzp_kvar = " + kreditXX.charge_xx + ".nzp_kvar " +
                                 "   and a.nzp_serv_perc = " + kreditXX.charge_xx + ".nzp_serv " +
                                 "   and a.nzp_supp = " + kreditXX.charge_xx + ".nzp_supp " +
                                 "   and a.kod = 1 ) " +
                " Where 0 < ( Select max(1) From t_kredo a " +
                            " Where a.nzp_kvar = " + kreditXX.charge_xx + ".nzp_kvar " +
                            "   and a.nzp_serv_perc = " + kreditXX.charge_xx + ".nzp_serv " +
                            "   and a.nzp_supp = " + kreditXX.charge_xx + ".nzp_supp " +
                            "   and a.kod = 1 ) "
            , true);
            if (!ret.result)
            {
                DropTempTablesKredit(conn_db);
                return false;
            }
            ret = ExecSQL(conn_db,
                " Update " + kreditXX.charge_xx +
                " Set sum_money = money_to + money_from + money_del " +
                   " ,sum_outsaldo = sum_insaldo +  real_charge + sum_real + reval + izm_saldo - (money_to + money_from + money_del) " +
                   " ,sum_pere  = sum_insaldo + reval + real_charge - (money_to + money_from + money_del) " +
                   " ,sum_charge = sum_real + real_charge + reval " +
                " Where nzp_kvar in ( Select nzp_kvar From t_selkvar ) " +
                "   and nzp_serv >= 306 and nzp_serv <= 311 " +
                "   and dat_charge is null "
            , true);*/
#else
            if (!ret.result)
            {
                DropTempTablesKredit(conn_db);
                return false;
            }
            //выбрать коммунальные начисления в тек. расчете
            ret = ExecSQL(conn_db,
                " Select a.nzp_kvar, a.nzp_serv_perc, a.nzp_supp, a.perc, sum(b.sum_real) as sum_real " +
                " From t_servp a, " + kreditXX.charge_xx + " b " +
                " Where a.nzp_kvar = b.nzp_kvar " +
                "   and a.nzp_serv_cmnl = b.nzp_serv " +
                "   and b.dat_charge is null " +
                "   and b.sum_real > 0 " +
                " Group by 1,2,3,4 " +
                " Into temp t_charge With no log "
            , true);
            if (!ret.result)
            {
                DropTempTablesKredit(conn_db);
                return false;
            }
            ExecSQL(conn_db, " Create index ix_t_charge on t_charge (nzp_kvar, nzp_serv_perc) ", true);
            ExecSQL(conn_db, " Update statistics for table t_charge ", true);
            /*
                                " (  nzp_kredit     serial not null, " +
                                "    nzp_kvar       integer not null, " +
                                "    nzp_serv       integer not null, " +
                                "    dat_month      date not null, " +
                                "    dat_s          date not null, " +
                                "    dat_po         date not null, " +
                                "    valid          integer not null, " +
                                "    sum_dolg       decimal(14,2) default 0.00, " +
                                "    perc           decimal(5,2) default 0.00 ) "
            */
            //вставить в кредитные истории
            ret = ExecSQL(conn_db,
                " Insert into " + kreditXX.kredo_data + " (nzp_kvar, nzp_serv, dat_month, dat_s, dat_po, valid, sum_dolg, perc) " +
                " Select nzp_kvar, nzp_serv_perc, " + kreditXX.paramcalc.dat_s + ", " +
                    kreditXX.paramcalc.dat_s + ", " + kreditXX.paramcalc.dat_s + " + 11 units month, 0, sum_real, perc " +
                " From t_charge " +
                " Where perc > 0 "
            , true);
            if (!ret.result)
            {
                DropTempTablesKredit(conn_db);
                return false;
            }
            /*
                    " (  nzp_kredx      serial not null, " +
                    "    nzp_kvar       integer not null, " +
                    "    nzp_serv       integer not null, " +
                    "    nzp_kredit     integer not null, " +
                    "    sum_indolg     decimal(14,2) default 0.00, " +
                    "    sum_dolg       decimal(14,2) default 0.00, " +  //сумма основного долга в первом месяце, в остальных месяцах = 0
                    "    sum_odna12     decimal(14,2) default 0.00, " +
                    "    sum_perc       decimal(14,2) default 0.00, " +
                    "    sum_charge     decimal(14,2) default 0.00, " +
                    "    sum_outdolg    decimal(14,2) default 0.00 ) "
            */
            //начинаем считать рассрочку и проценты по текущим валидным кредитам (valid = 0,1)
            ExecSQL(conn_db, " Drop table t_kredo ", false);
            //t_kredo - сожержит все данные по рассрочке!!!
            ret = ExecSQL(conn_db,
                " Select a.nzp_kvar, b.nzp_serv_perc, a.nzp_kredit, b.nzp_supp,  " +
                //сумма рассрочки(долга) в первом месяце, в остальных месяцах = 0
                      "  case when a.dat_month = " + kreditXX.paramcalc.dat_s + " then a.sum_dolg else 0 end as sum_dolg, " +
                //ежемес. платеж                
                      "  (a.sum_dolg / 12) as sum_odna12, " +
                //процент пользования
                      "   a.perc,  " +
                //сумма процента
                      "  (a.sum_dolg / 12 * a.perc / 100) as sum_perc, " +
                      "  0 as kod " +
                " From " + kreditXX.kredo_data + " a, t_servp b " +
                " Where a.nzp_kvar = b.nzp_kvar " +
                "   and a.nzp_serv = b.nzp_serv_perc " +
                "   and a.dat_po >= " + kreditXX.paramcalc.dat_s +
                "   and a.dat_s <= " + kreditXX.paramcalc.dat_po +
                "   and a.valid in (0,1) " +
                " Into temp t_kredo With no log "
            , true);
            if (!ret.result)
            {
                DropTempTablesKredit(conn_db);
                return false;
            }
            ExecSQL(conn_db, " Create unique index ix1_t_kredo on t_kredo (nzp_kvar, nzp_serv_perc) ", true);
            ExecSQL(conn_db, " Create unique index ix2_t_kredo on t_kredo (nzp_kredit) ", true);
            ExecSQL(conn_db, " Create        index ix4_t_kredo on t_kredo (kod) ", true);
            ExecSQL(conn_db, " Update statistics for table t_kredo ", true);
            //загоним данные в kreditXX
            ret = ExecSQL(conn_db,
                " Insert into " + kreditXX.kredit_xx + " ( nzp_kvar, nzp_serv, nzp_kredit, sum_odna12, sum_perc, sum_dolg ) " +
                " Select nzp_kvar, nzp_serv_perc, nzp_kredit, sum_odna12, sum_perc, sum_dolg " +
                " From t_kredo "
            , true);
            if (!ret.result)
            {
                DropTempTablesKredit(conn_db);
                return false;
            }
            //посчитать сальдо
            if (TempTableInWebCashe(conn_db, kreditXX.prev_kredit_xx))
            {
                ExecSQL(conn_db, " Drop table t_charge ", false);
                ret = ExecSQL(conn_db,
                    " Select a.nzp_kvar, nzp_serv, nzp_kredit, 0 as kod, sum(sum_outdolg) as sum_outdolg " +
                    " From " + kreditXX.prev_kredit_xx + " a, t_selkvar b " +
                    " Where a.nzp_kvar = b.nzp_kvar " +
                    "   and abs(a.sum_outdolg) > 0.001 " +
                    " Group by 1,2,3,4  " +
                    " Into temp t_charge With no log "
                , true);
                if (!ret.result)
                {
                    DropTempTablesKredit(conn_db);
                    return false;
                }
                ExecSQL(conn_db, " Create index ix1_t_charge on t_charge (nzp_kredit) ", true);
                ExecSQL(conn_db, " Create index ix2_t_charge on t_charge (kod) ", true);
                ExecSQL(conn_db, " Update statistics for table t_charge ", true);
                ret = ExecSQL(conn_db,
                    " Update t_charge " +
                    " Set kod = 1 " +
                    " Where nzp_kredit in ( Select nzp_kredit From " + kreditXX.kredit_xx + " ) "
                , true);
                if (!ret.result)
                {
                    DropTempTablesKredit(conn_db);
                    return false;
                }
                ret = ExecSQL(conn_db,
                    " Insert into " + kreditXX.kredit_xx + " ( nzp_kvar, nzp_serv, nzp_kredit, sum_indolg ) " +
                    " Select nzp_kvar, nzp_serv, nzp_kredit, sum_outdolg " +
                    " From t_charge " +
                    " Where kod = 0 "
                , true);
                if (!ret.result)
                {
                    DropTempTablesKredit(conn_db);
                    return false;
                }
                ret = ExecSQL(conn_db,
                    " Update " + kreditXX.kredit_xx +
                    " Set sum_indolg = ( Select sum(sum_outdolg) From t_charge a " +
                                       " Where " + kreditXX.kredit_xx + ".nzp_kredit = a.nzp_kredit ) " +
                    " Where nzp_kredit in ( Select nzp_kredit From t_charge ) "
                , true);
                if (!ret.result)
                {
                    DropTempTablesKredit(conn_db);
                    return false;
                }
            }
            //расчет сальдо
            ret = ExecSQL(conn_db,
                " Update " + kreditXX.kredit_xx +
                " Set sum_outdolg = sum_indolg - sum_odna12 + sum_dolg " +
                " Where nzp_kvar in ( Select nzp_kvar From t_selkvar ) "
            , true);
            if (!ret.result)
            {
                DropTempTablesKredit(conn_db);
                return false;
            }
            //выровнить sum_odna12
            ret = ExecSQL(conn_db,
                " Update " + kreditXX.kredit_xx +
                " Set sum_outdolg = 0, " +
                "     sum_odna12 = sum_indolg + sum_dolg " +
                " Where nzp_kvar in ( Select nzp_kvar From t_selkvar ) " +
                "   and sum_outdolg < 0 "
            , true);
            if (!ret.result)
            {
                DropTempTablesKredit(conn_db);
                return false;
            }
            ret = ExecSQL(conn_db,
                " Update " + kreditXX.kredit_xx +
                " Set sum_charge = sum_odna12 + sum_perc " +
                " Where nzp_kvar in ( Select nzp_kvar From t_selkvar ) "
            , true);
            if (!ret.result)
            {
                DropTempTablesKredit(conn_db);
                return false;
            }
            //затем проценты загоним в charge_xx в отдельные услуги
            
            // НЕ СОХРАНЯТЬ РАССРОЧКУ В CHARGE

            /*ret = ExecSQL(conn_db, //выбрать текущие строки из начислений
                " Update t_kredo " +
                " Set kod = 1 " +
                " Where 0 < ( Select max(1) From " + kreditXX.charge_xx + " b " +
                            " Where t_kredo.nzp_kvar = b.nzp_kvar " +
                            "   and t_kredo.nzp_serv_perc = b.nzp_serv " +
                            "   and t_kredo.nzp_supp = b.nzp_supp " +
                            "   and b.dat_charge is null ) "
            , true);
            if (!ret.result)
            {
                DropTempTablesKredit(conn_db);
                return false;
            }
            //исправляем charge_xx
            ret = ExecSQL(conn_db,
                " Update " + kreditXX.charge_xx +
                " Set sum_real = ( Select sum(sum_odna12 + sum_perc) From t_kredo a " +
                                 " Where a.nzp_kvar = " + kreditXX.charge_xx + ".nzp_kvar " +
                                 "   and a.nzp_serv_perc = " + kreditXX.charge_xx + ".nzp_serv " +
                                 "   and a.nzp_supp = " + kreditXX.charge_xx + ".nzp_supp " +
                                 "   and a.kod = 1 ) " +
                " Where 0 < ( Select max(1) From t_kredo a " +
                            " Where a.nzp_kvar = " + kreditXX.charge_xx + ".nzp_kvar " +
                            "   and a.nzp_serv_perc = " + kreditXX.charge_xx + ".nzp_serv " +
                            "   and a.nzp_supp = " + kreditXX.charge_xx + ".nzp_supp " +
                            "   and a.kod = 1 ) "
            , true);
            if (!ret.result)
            {
                DropTempTablesKredit(conn_db);
                return false;
            }
            ret = ExecSQL(conn_db,
                " Update " + kreditXX.charge_xx +
                " Set sum_money = money_to + money_from + money_del " +
                   " ,sum_outsaldo = sum_insaldo +  real_charge + sum_real + reval + izm_saldo - (money_to + money_from + money_del) " +
                   " ,sum_pere  = sum_insaldo + reval + real_charge - (money_to + money_from + money_del) " +
                   " ,sum_charge = sum_real + real_charge + reval " +
                " Where nzp_kvar in ( Select nzp_kvar From t_selkvar ) " +
                "   and nzp_serv >= 306 and nzp_serv <= 311 " +
                "   and dat_charge is null "
            , true);*/
#endif
            if (!ret.result)
            {
                DropTempTablesKredit(conn_db);
                return false;
            }

            /*
            6  306,'% за рассрочку по холодной воде   ','Видеонаблюдение' ,'Проценты за рассрочку по холодной воде   ','с кв.в мес.',1,0,309);
            9  307,'% за рассрочку по горячей воде    ','Видеонаблюдение' ,'Проценты за рассрочку по горячей воде    ','с кв.в мес.',1,0,310);
            7  308,'% за рассрочку по канализации     ','Видеонаблюдение' ,'Проценты за рассрочку по канализации     ','с кв.в мес.',1,0,311);
            8  309,'% за рассрочку по отоплению       ','Видеонаблюдение' ,'Проценты за рассрочку по отоплению       ','с кв.в мес.',1,0,312);
            25 310,'% за рассрочку по электроснабжению','Видеонаблюдение' ,'Проценты за рассрочку по электроснабжению','с кв.в мес.',1,0,313);
            10 311,'% за рассрочку по газу            ','Видеонаблюдение' ,'Проценты за рассрочку по газу            ','с кв.в мес.',1,0,314);
            */


            DropTempTablesKredit(conn_db);
            return true;
        }
    }
}
