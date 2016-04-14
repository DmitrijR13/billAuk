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
    //здась находятся классы для подсчета расходов
    public partial class DbCalc : DbCalcClient
    {

        struct GilecXX
        {
            public ParamCalc paramcalc;

            public string gil_xx;
            public string gilec_tab;

            public GilecXX(ParamCalc _paramcalc)
            {
                paramcalc = _paramcalc;
                paramcalc.b_dom_in = true;

                gilec_tab   = "gil" + paramcalc.alias + "_" + paramcalc.calc_mm.ToString("00");
                gil_xx      = paramcalc.pref + "_charge_" + (paramcalc.calc_yy - 2000).ToString("00") + tableDelimiter + gilec_tab;
            }
        }
        //-----------------------------------------------------------------------------
        //Расчет gil_xx
        //-----------------------------------------------------------------------------
        void CreateGilXX(IDbConnection conn_db2, GilecXX gilec, out Returns ret)
        //-----------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            if (TempTableInWebCashe(conn_db2, gilec.gil_xx))
            {
                ExecByStep(conn_db2, gilec.gil_xx, "nzp_gx",
                        " Delete From " + gilec.gil_xx +
                      //" Where 1 = 1 and " + gilec.paramcalc.where_z + gilec.paramcalc.per_dat_charge
                        " Where nzp_kvar in ( Select nzp_kvar From t_selkvar ) " + gilec.paramcalc.per_dat_charge
                        , 100000, " ", out ret);
                if (!ret.result) { conn_db2.Close(); return; }

                UpdateStatistics(false, gilec.paramcalc, gilec.gilec_tab, out ret);
                if (!ret.result) { conn_db2.Close(); return; }

                return;
            }

            string conn_kernel = Points.GetConnByPref(gilec.paramcalc.pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            //IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                return;
            }
            ret = ExecSQL(conn_db,
#if PG
                " set search_path to '" + gilec.paramcalc.pref + "_charge_" + (gilec.paramcalc.calc_yy - 2000).ToString("00") + "'"
#else
                " Database " + gilec.paramcalc.pref + "_charge_" + (gilec.paramcalc.calc_yy - 2000).ToString("00")
#endif
                , true);

#if PG
            ret = ExecSQL(conn_db,
                " Create table " + gilec.gilec_tab +
                " (  nzp_gx      serial        not null, " +
                "    nzp_dom     integer       not null, " +
                "    nzp_kvar    integer       default 0 not null, " +
                "    dat_charge  date, " +
                "    cur_zap     integer       default 0 not null, " +     //0-текущее значение, >0 - ссылка на следующее значение (nzp_cntx)
                "    prev_zap    integer       default 0 not null, " +     //0-текущее значение, >0 - ссылка на предыдыущее значение (nzp_cntx)
                "    nzp_gil     integer       default 0 not null, " + //
                "    dat_s       date, " +
                "    dat_po      date, " +
                "    stek        integer       default 0 not null, " + //
                "    cnt1        integer       default 0 not null, " +    //кол-во жильцов в лс с учетом времен. выбывших
                "    cnt2        integer       default 0 not null, " +    //кол-во жильцов в лс без учета времен. выбывших
                "    cnt3        integer       default 0 not null, " +    //кол-во дней cnt1 x 31 (кол-во дней проживания)
                "    val1        numeric(11,7) default 0.00, " +    //итоговое кол-во жильцов в лс с учетом времен. выбывших
                "    val2        numeric(11,7) default 0.00, " +    //nzp_prm = 5
                "    val3        numeric(11,7) default 0.00, " +    //nzp_prm = 131
                "    val4        numeric(11,7) default 0.00, " +    //дробное кол-во жильцов в лс по kart с учетом времен. выбывших
                "    val5        numeric(11,7) default 0.00, " +    //дробное кол-во времен. выбывших
                "    kod_info    integer       default 0 ) "
                , true);
#else
            ret = ExecSQL(conn_db,
                           " Create table are." + gilec.gilec_tab +
                           " (  nzp_gx      serial        not null, " +
                           "    nzp_dom     integer       not null, " +
                           "    nzp_kvar    integer       default 0 not null, " +
                           "    dat_charge  date, " +
                           "    cur_zap     integer       default 0 not null, " +     //0-текущее значение, >0 - ссылка на следующее значение (nzp_cntx)
                           "    prev_zap    integer       default 0 not null, " +     //0-текущее значение, >0 - ссылка на предыдыущее значение (nzp_cntx)
                           "    nzp_gil     integer       default 0 not null, " + //
                           "    dat_s       date, " +
                           "    dat_po      date, " +
                           "    stek        integer       default 0 not null, " + //
                           "    cnt1        integer       default 0 not null, " +    //кол-во жильцов в лс с учетом времен. выбывших
                           "    cnt2        integer       default 0 not null, " +    //кол-во жильцов в лс без учета времен. выбывших
                           "    cnt3        integer       default 0 not null, " +    //кол-во дней cnt1 x 31 (кол-во дней проживания)
                           "    val1        decimal(11,7) default 0.00, " +    //итоговое кол-во жильцов в лс с учетом времен. выбывших
                           "    val2        decimal(11,7) default 0.00, " +    //nzp_prm = 5
                           "    val3        decimal(11,7) default 0.00, " +    //nzp_prm = 131
                           "    val4        decimal(11,7) default 0.00, " +    //дробное кол-во жильцов в лс по kart с учетом времен. выбывших
                           "    val5        decimal(11,7) default 0.00, " +    //дробное кол-во времен. выбывших
                           "    kod_info    integer       default 0 ) "
                                 , true);
#endif
            if (!ret.result)
            {
                conn_db.Close();
                return;
            }
#if PG
            ret = ExecSQL(conn_db, " create unique index ix1_" + gilec.gilec_tab + " on " + gilec.gilec_tab + " (nzp_gx) ", true);
#else
            ret = ExecSQL(conn_db, " create unique index are.ix1_" + gilec.gilec_tab + " on " + gilec.gilec_tab + " (nzp_gx) ", true);
#endif
            if (!ret.result)
            {
                conn_db.Close();
                return;
            }
            ret = ExecSQL(conn_db, " create index "+
#if PG
 "ix2_"
#else
"are.ix2_" 
#endif
                + gilec.gilec_tab + " on " + gilec.gilec_tab + " (nzp_dom,dat_charge) ", true);
            if (!ret.result)
            {
                conn_db.Close();
                return;
            }
            ret = ExecSQL(conn_db, " create index "+
#if PG
 " ix3_"
#else
" are.ix3_" 
#endif
                + gilec.gilec_tab + " on " + gilec.gilec_tab + " (nzp_kvar,dat_charge,stek, dat_s,dat_po) ", true);
            if (!ret.result)
            {
                conn_db.Close();
                return;
            }
            ret = ExecSQL(conn_db, " create index "+
#if PG
 "ix4_"
#else
"are.ix4_" 
#endif
                + gilec.gilec_tab + " on " + gilec.gilec_tab + " (nzp_kvar,nzp_gil,dat_charge) ", true);
            if (!ret.result)
            {
                conn_db.Close();
                return;
            }
            ret = ExecSQL(conn_db, " create index "+
#if PG
 "ix5_"
#else
"are.ix5_"
#endif
                + gilec.gilec_tab + " on " + gilec.gilec_tab + " (cur_zap) ", true);
            if (!ret.result)
            {
                conn_db.Close();
                return;
            }
            ret = ExecSQL(conn_db, " create index "+
#if PG
 "ix6_"
#else
"are.ix6_"
#endif
                + gilec.gilec_tab + " on " + gilec.gilec_tab + " (prev_zap) ", true);
            if (!ret.result)
            {
                conn_db.Close();
                return;
            }
#if PG
            ExecSQL(conn_db, " analyze " + gilec.gilec_tab, true);
#else
ExecSQL(conn_db, " update statistics for table " + gilec.gilec_tab, true);
#endif

            conn_db.Close();
        }

        //-----------------------------------------------------------------------------
        public bool CalcGilXX(IDbConnection conn_db, ParamCalc paramcalc, out Returns ret)
        //-----------------------------------------------------------------------------
        {
            GilecXX gilec = new GilecXX(paramcalc);

            //---------------------------------------------------
            //выбрать множество лицевых счетов
            //---------------------------------------------------
            if (!TempTableInWebCashe(conn_db, "t_opn"))
            {
                ChoiseTempKvar(conn_db, ref paramcalc, out ret);
                if (!ret.result)
                {
                    conn_db.Close();
                    return false;
                }
            }

            CreateGilXX(conn_db, gilec, out ret);
            if (!ret.result)
            {
                return false;
            }



            //выбрать все карточки по дому
            ExecSQL(conn_db, " Drop table ttt_aid_gx ", false);

#if PG
            ret = ExecSQL(conn_db,
                " Select distinct k.nzp_kvar,k.nzp_dom, nzp_gil, nzp_tkrt, " +
                       " coalesce(dat_ofor, "+MDY(1,1,1901)+") as dat_ofor, dat_oprp " +
                        " Into temp ttt_aid_gx "+
                " From " + gilec.paramcalc.pref + "_data.kart g, t_opn k " +
                " Where k.nzp_kvar = g.nzp_kvar " +
                "   and nzp_tkrt is not null "                
                , true);
#else
            ret = ExecSQL(conn_db,
                " Select unique k.nzp_kvar,k.nzp_dom, nzp_gil, nzp_tkrt, " +
                       " nvl(dat_ofor,MDY(1,1,1901)) as dat_ofor, dat_oprp " +
                " From " + gilec.paramcalc.pref + "_data:kart g, t_opn k " +
                " Where k.nzp_kvar = g.nzp_kvar " +
                "   and nzp_tkrt is not null " +
                " Into temp ttt_aid_gx With no log "
                , true);
#endif
            if (!ret.result)
            {
                //ExecSQL(conn_db, " Drop table t_opn ", false);
                return false;
            }
            ret = ExecSQL(conn_db, " Create index ix_aid_gx1 on ttt_aid_gx (nzp_kvar, nzp_gil, nzp_tkrt, dat_ofor) ", true);
            if (!ret.result)
            {
                ExecSQL(conn_db, " Drop table ttt_aid_gx ", false);
                //ExecSQL(conn_db, " Drop table t_opn ", false);
                return false; ;
            }
            ret = ExecSQL(conn_db, " Create index ix_aid_gx2 on ttt_aid_gx (nzp_tkrt) ", true);
            if (!ret.result)
            {
                ExecSQL(conn_db, " Drop table ttt_aid_gx ", false);
                //ExecSQL(conn_db, " Drop table t_opn ", false);
                return false; ;
            }
            ret = ExecSQL(conn_db, " Create index ix_aid_gx3 on ttt_aid_gx (nzp_gil) ", true);
            if (!ret.result)
            {
                ExecSQL(conn_db, " Drop table ttt_aid_gx ", false);
                //ExecSQL(conn_db, " Drop table t_opn ", false);
                return false;
            }
            ExecSQL(conn_db, sUpdStat + " ttt_aid_gx ", true);

            string p_dat_charge = DateNullString;
            if (!gilec.paramcalc.b_cur)
                p_dat_charge = MDY(gilec.paramcalc.cur_mm, 28, gilec.paramcalc.cur_yy);

            //карточки прибытия
            ExecByStep(conn_db, "ttt_aid_gx", "nzp_gil",
                " Insert into " + gilec.gil_xx + " ( nzp_kvar,nzp_dom,dat_charge,nzp_gil, dat_s,dat_po, stek ) " +
                " Select " + sUniqueWord + " nzp_kvar,nzp_dom, " + p_dat_charge + ", nzp_gil, dat_ofor, dat_oprp, 1  " +
                " From ttt_aid_gx g " +
                " Where nzp_tkrt <> 2 " + //прибытие
                //все даты внутри периоды и ближайший предыдущий до периода
                "   and ( dat_ofor between " + gilec.paramcalc.dat_s + " and " + gilec.paramcalc.dat_po +
                        "   or dat_ofor = " +
                        " ( Select max(g1.dat_ofor) From ttt_aid_gx g1 " +
                         "  Where g.nzp_kvar = g1.nzp_kvar " +
                           "  and g.nzp_gil  = g1.nzp_gil " +
                           "  and g1.nzp_tkrt <> 2 " +
                           "  and g1.dat_ofor < " + gilec.paramcalc.dat_s +
                        " ) " +
                       ")"
            , 130000, " ", out ret);
            if (!ret.result)
            {
                ExecSQL(conn_db, " Drop table ttt_aid_gx ", false);
                //ExecSQL(conn_db, " Drop table t_opn ", false);
                return false;
            }

            UpdateStatistics(false, paramcalc, gilec.gilec_tab, out ret);
            if (!ret.result)
            {
                ExecSQL(conn_db, " Drop table ttt_aid_gx ", false);
                //ExecSQL(conn_db, " Drop table t_opn ", false);
                return false;
            }

            //карточки убытия - ищем ближайший dat_ofor после dat_s
            ExecByStep(conn_db, gilec.gil_xx, "nzp_gx",
                " Update " + gilec.gil_xx +
                " Set dat_po = ( Select min(dat_ofor) From ttt_aid_gx g " +
                               " Where " + gilec.gil_xx + ".nzp_kvar = g.nzp_kvar " +
                               "   and " + gilec.gil_xx + ".nzp_gil  = g.nzp_gil " +
                               "   and g.nzp_tkrt = 2 " +
                               "   and " + gilec.gil_xx + ".dat_s < g.dat_ofor " +
                               " ) " +
                " Where " + gilec.paramcalc.where_z + gilec.paramcalc.per_dat_charge +
                "   and 0 < ( Select count(*) From ttt_aid_gx g " +
                              " Where " + gilec.gil_xx + ".nzp_kvar = g.nzp_kvar " +
                              "   and " + gilec.gil_xx + ".nzp_gil  = g.nzp_gil " +
                              "   and g.nzp_tkrt = 2 " +
                              "   and " + gilec.gil_xx + ".dat_s < g.dat_ofor " +
                            " ) "
               , 100000, "", out ret);
            if (!ret.result)
            {
                ExecSQL(conn_db, " Drop table ttt_aid_gx ", false);
                //ExecSQL(conn_db, " Drop table t_opn ", false);
                return false;
            }

            UpdateStatistics(false, paramcalc, gilec.gilec_tab, out ret);
            if (!ret.result)
            {
                ExecSQL(conn_db, " Drop table ttt_aid_gx ", false);
                //ExecSQL(conn_db, " Drop table t_opn ", false);
                return false;
            }


            //вставляем одинокие карточки убытия
            ExecSQL(conn_db, " Drop table ttt_aid_ub ", false);

            ret = ExecSQL(conn_db,
                " Select nzp_kvar,nzp_dom, nzp_gil, nzp_tkrt, dat_ofor " +
#if PG
                " Into temp ttt_aid_ub "+
#else
                " "+
#endif
                " From ttt_aid_gx g " +
                " Where g.nzp_tkrt = 2 " +
                "   and g.dat_ofor<= " + gilec.paramcalc.dat_po +
                //не были выбраны
                "   and 1 > ( Select count(*) From " + gilec.gil_xx + " gx " +
                            " Where g.nzp_kvar = gx.nzp_kvar " +
                            "   and g.nzp_gil  = gx.nzp_gil  " + gilec.paramcalc.per_dat_charge +
                          " ) " +
                //самую минимальную дату убытия после начала периода
                "   and dat_ofor = " +
                    " ( Select min(g1.dat_ofor) From ttt_aid_gx g1 " +
                      " Where g1.nzp_kvar = g.nzp_kvar " +
                      "   and g1.nzp_gil  = g.nzp_gil " +
                      "   and g1.nzp_tkrt = 2 " +
                      "   and g1.dat_ofor >= " + gilec.paramcalc.dat_s +
                     ")" +
#if PG
 "  "
#else
 " Into temp ttt_aid_ub With no log "
#endif
                , true);
            if (!ret.result)
            {
                ExecSQL(conn_db, " Drop table ttt_aid_gx ", false);
                //ExecSQL(conn_db, " Drop table t_opn ", false);
                return false;
            }

            ret = ExecSQL(conn_db, " Create index ix_aid_ub1 on ttt_aid_ub (nzp_dom) ", true);
            if (!ret.result)
            {
                ExecSQL(conn_db, " Drop table ttt_aid_ub ", false);
                ExecSQL(conn_db, " Drop table ttt_aid_gx ", false);
                //ExecSQL(conn_db, " Drop table t_opn ", false);
                return false;
            }

            ExecByStep(conn_db, "ttt_aid_ub", "nzp_dom",
                " Insert into " + gilec.gil_xx + " ( nzp_kvar,nzp_dom,dat_charge,nzp_gil, dat_po, stek ) " +
                " Select nzp_kvar,nzp_dom, " + p_dat_charge + ", nzp_gil, dat_ofor, 1  " +
                " From ttt_aid_ub Where 1=1 "
                , 5000, "", out ret);
            if (!ret.result)
            {
                ExecSQL(conn_db, " Drop table ttt_aid_ub ", false);
                ExecSQL(conn_db, " Drop table ttt_aid_gx ", false);
                //ExecSQL(conn_db, " Drop table t_opn ", false);
                return false;
            }

            UpdateStatistics(false, paramcalc, gilec.gilec_tab, out ret);
            if (!ret.result)
            {
                ExecSQL(conn_db, " Drop table ttt_aid_ub ", false);
                ExecSQL(conn_db, " Drop table ttt_aid_gx ", false);
                //ExecSQL(conn_db, " Drop table t_opn ", false);
                return false;
            }

            ExecSQL(conn_db, " Drop table ttt_aid_ub ", false);


            ExecByStep(conn_db, gilec.gil_xx, "nzp_gx",
              " Update " + gilec.gil_xx +
              " Set dat_s = " + MDY(1,1,1901) +
              " Where " + gilec.paramcalc.where_z + gilec.paramcalc.per_dat_charge +
              "   and dat_s is null "
              , 100000, "", out ret);
            if (!ret.result)
            {
                ExecSQL(conn_db, " Drop table ttt_aid_gx ", false);
                //ExecSQL(conn_db, " Drop table t_opn ", false);
                return false;
            }

            ExecByStep(conn_db, gilec.gil_xx, "nzp_gx",
              " Update " + gilec.gil_xx +
              " Set dat_po = "+
#if PG
 "public.MDY" +
#else
"MDY"+
#endif
              "(1,1,3000) " +
              " Where " + gilec.paramcalc.where_z + gilec.paramcalc.per_dat_charge +
              "   and dat_po is null "
              , 100000, "", out ret);
            if (!ret.result)
            {
                ExecSQL(conn_db, " Drop table ttt_aid_gx ", false);
                //ExecSQL(conn_db, " Drop table t_opn ", false);
                return false;
            }

            UpdateStatistics(false, paramcalc, gilec.gilec_tab, out ret);
            if (!ret.result)
            {
                ExecSQL(conn_db, " Drop table ttt_aid_gx ", false);
                //ExecSQL(conn_db, " Drop table t_opn ", false);
                return false;
            }

            //кол-во дней бытия в месяце
#if PG
            ExecByStep(conn_db, gilec.gil_xx, "nzp_gx",
                          " Update " + gilec.gil_xx +
                          " Set cnt1 =  EXTRACT('days' from" +
                             " (case when dat_po > " + gilec.paramcalc.dat_po + " then " + gilec.paramcalc.dat_po + " + interval '1 day' " +
                             " else dat_po+ interval '1 day' end) - " +
                             " (case when dat_s < " + gilec.paramcalc.dat_s + " then " + gilec.paramcalc.dat_s + " else dat_s end) )  " +
                          " Where " + gilec.paramcalc.where_z + gilec.paramcalc.per_dat_charge +
                          "   and dat_po >= " + gilec.paramcalc.dat_s +
                          "   and dat_s  <= " + gilec.paramcalc.dat_po
                          , 100000, "", out ret);
#else
ExecByStep(conn_db, gilec.gil_xx, "nzp_gx",
              " Update " + gilec.gil_xx +
              " Set cnt1 = " + gilec.paramcalc.pref + "_data:sortnum( (case when dat_po > " + gilec.paramcalc.dat_po + " then " + gilec.paramcalc.dat_po + " +1 " +
                              " else dat_po+1 end) - (case when dat_s < " + gilec.paramcalc.dat_s + " then " + gilec.paramcalc.dat_s + " else dat_s end)  ) " +
              " Where " + gilec.paramcalc.where_z + gilec.paramcalc.per_dat_charge +
              "   and dat_po >= " + gilec.paramcalc.dat_s +
              "   and dat_s  <= " + gilec.paramcalc.dat_po
              , 100000, "", out ret);
#endif

            if (!ret.result)
            {
                ExecSQL(conn_db, " Drop table ttt_aid_gx ", false);
                //ExecSQL(conn_db, " Drop table t_opn ", false);
                return false;
            }

            //загнать gil_periods в stek = 2
#if PG
            ExecByStep(conn_db, gilec.paramcalc.pref + "_data.gil_periods", "nzp_glp",
                          " Insert into " + gilec.gil_xx + " ( nzp_kvar,nzp_dom,dat_charge,nzp_gil, dat_s,dat_po, stek, cnt1 ) " +
                          " Select distinct k.nzp_kvar,k.nzp_dom, " + p_dat_charge + ", nzp_gilec, g.dat_s, g.dat_po, 2,  " +
                                 " EXTRACT('days' from  (case when g.dat_po > " + gilec.paramcalc.dat_po +
                                                      " then " + gilec.paramcalc.dat_po + " +interval '1 day' " +
                                                      " else g.dat_po+interval '1 day' end) - (case when g.dat_s < " + gilec.paramcalc.dat_s +
                                                                                   " then " + gilec.paramcalc.dat_s + " else g.dat_s end)  ) " +
                          " From " + gilec.paramcalc.pref + "_data.gil_periods g, t_opn k " +
                          " Where g.nzp_kvar = k.nzp_kvar " +
                          "   and g.is_actual <> 100 " +
                          "   and g.dat_s  <= " + gilec.paramcalc.dat_po +
                          "   and g.dat_po >= " + gilec.paramcalc.dat_s +
                          "   and g.dat_po - g.dat_s > 5 " //где убыл не менее 6 дней
                          , 100000, "", out ret);
#else
ExecByStep(conn_db, gilec.paramcalc.pref + "_data:gil_periods", "nzp_glp",
              " Insert into " + gilec.gil_xx + " ( nzp_kvar,nzp_dom,dat_charge,nzp_gil, dat_s,dat_po, stek, cnt1 ) " +
              " Select unique k.nzp_kvar,k.nzp_dom, " + p_dat_charge + ", nzp_gilec, g.dat_s, g.dat_po, 2,  " +
                    gilec.paramcalc.pref + "_data:sortnum( (case when g.dat_po > " + gilec.paramcalc.dat_po +
                                          " then " + gilec.paramcalc.dat_po + " +1 " +
                                          " else g.dat_po+1 end) - (case when g.dat_s < " + gilec.paramcalc.dat_s +
                                                                       " then " + gilec.paramcalc.dat_s + " else g.dat_s end)  ) " +
              " From " + gilec.paramcalc.pref + "_data:gil_periods g, t_opn k " +
              " Where g.nzp_kvar = k.nzp_kvar " +
              "   and g.is_actual <> 100 " +
              "   and g.dat_s  <= " + gilec.paramcalc.dat_po +
              "   and g.dat_po >= " + gilec.paramcalc.dat_s +
              "   and g.dat_po - g.dat_s > 5 " //где убыл не менее 6 дней
              , 100000, "", out ret);
#endif
            if (!ret.result)
            {
                ExecSQL(conn_db, " Drop table ttt_aid_gx ", false);
                //ExecSQL(conn_db, " Drop table t_opn ", false);
                return false;
            }

            UpdateStatistics(false, paramcalc, gilec.gilec_tab, out ret);
            if (!ret.result)
            {
                ExecSQL(conn_db, " Drop table ttt_aid_gx ", false);
                //ExecSQL(conn_db, " Drop table t_opn ", false);
                return false;
            }


            //сначала объединим пересекающиеся интервалы gil_periods
            ExecSQL(conn_db, " Drop table ttt_aid_uni ", false);

            string st1;
            if (gilec.paramcalc.b_cur)
                st1 = " and a.dat_charge is null and b.dat_charge is null ";
            else
                st1 = " and a.dat_charge = b.dat_charge and a.dat_charge = " + MDY(gilec.paramcalc.cur_mm, 28, gilec.paramcalc.cur_yy);

#if PG
            ret = ExecSQL(conn_db,
                           " Select distinct a.nzp_kvar, a.nzp_dom, a.nzp_gil, a.stek, a.dat_charge, min(a.dat_s) as dat_s, max(a.dat_po) as dat_po " +
                           " Into temp ttt_aid_uni " +
                           " From " + gilec.gil_xx + " a, " + gilec.gil_xx + " b " +
                           " Where a.nzp_kvar = b.nzp_kvar " +
                           "   and a.nzp_gil  = b.nzp_gil " +
                           "   and a.stek     = b.stek " +
                           "   and a.nzp_gx <>  b.nzp_gx " +
                           "   and a.stek     = 2 " +
                           "   and a.dat_s  <= b.dat_po " +
                           "   and a.dat_po >= b.dat_s  " +
                           st1 +
                           " Group by 1,2,3,4,5 "                            
                           , true);
#else
 ret = ExecSQL(conn_db,
                " Select unique a.nzp_kvar, a.nzp_dom, a.nzp_gil, a.stek, a.dat_charge, min(a.dat_s) as dat_s, max(a.dat_po) as dat_po " +
                " From " + gilec.gil_xx + " a, " + gilec.gil_xx + " b " +
                " Where a.nzp_kvar = b.nzp_kvar " +
                "   and a.nzp_gil  = b.nzp_gil " +
                "   and a.stek     = b.stek " +
                "   and a.nzp_gx <>  b.nzp_gx " +
                "   and a.stek     = 2 " +
                "   and a.dat_s  <= b.dat_po " +
                "   and a.dat_po >= b.dat_s  " +
                st1 +
                " Group by 1,2,3,4,5 " +
                " Into temp ttt_aid_uni With no log "
                , true);
#endif

            if (!ret.result)
            {
                ExecSQL(conn_db, " Drop table ttt_aid_gx ", false);
                //ExecSQL(conn_db, " Drop table t_opn ", false);
                return false;
            }

            ret = ExecSQL(conn_db, " Create index ix_aid_uni1 on ttt_aid_uni (nzp_kvar, nzp_gil) ", true);
            if (!ret.result)
            {
                ExecSQL(conn_db, " Drop table ttt_aid_uni ", false);
                ExecSQL(conn_db, " Drop table ttt_aid_gx ", false);
                //ExecSQL(conn_db, " Drop table t_opn ", false);
                return false;
            }
            ret = ExecSQL(conn_db, " Create index ix_aid_uni2 on ttt_aid_uni (nzp_dom) ", true);
            if (!ret.result)
            {
                ExecSQL(conn_db, " Drop table ttt_aid_uni ", false);
                ExecSQL(conn_db, " Drop table ttt_aid_gx ", false);
                //ExecSQL(conn_db, " Drop table t_opn ", false);
                return false;
            }

            //удалим измененные строки (пока скинем в архив для отладки)
            ExecByStep(conn_db, gilec.gil_xx, "nzp_gx",
                " Update " + gilec.gil_xx +
                " Set cur_zap = -1 " +
                " Where " + gilec.paramcalc.where_z + gilec.paramcalc.per_dat_charge +
                "   and 0 < ( Select count(*) From ttt_aid_uni b " +
                            " Where " + gilec.gil_xx + ".nzp_kvar = b.nzp_kvar " +
                            "   and " + gilec.gil_xx + ".nzp_gil  = b.nzp_gil " +
                            "   and " + gilec.gil_xx + ".stek     = b.stek " +
                           " ) "
              , 100000, "", out ret);
            if (!ret.result)
            {
                ExecSQL(conn_db, " Drop table ttt_aid_uni ", false);
                ExecSQL(conn_db, " Drop table ttt_aid_gx ", false);
                //ExecSQL(conn_db, " Drop table t_opn ", false);
                return false;
            }

            //и введем измененную строку
            ret = ExecSQL(conn_db,
                " Insert into " + gilec.gil_xx + " ( nzp_kvar,nzp_dom,dat_charge,nzp_gil,stek, dat_s,dat_po, cur_zap,cnt1 ) " +
                " Select nzp_kvar,nzp_dom,dat_charge,nzp_gil,stek, dat_s,dat_po, 1 , " +
                       
#if PG
                "EXTRACT('days' from" +
#else
                gilec.paramcalc.pref +
                "_data:sortnum"+
                "( (case when dat_po > " + gilec.paramcalc.dat_po + " then " + gilec.paramcalc.dat_po +
                       " else dat_po+1 end) - (case when dat_s < " + gilec.paramcalc.dat_s + " then " + gilec.paramcalc.dat_s + " else dat_s end)  ) " +
#endif

#if PG
                "( (case when dat_po > " + gilec.paramcalc.dat_po + " then " + gilec.paramcalc.dat_po +
                       " else dat_po+interval '1 day' end) - (case when dat_s < " + gilec.paramcalc.dat_s + " then " + gilec.paramcalc.dat_s + " else dat_s end)  ) " +
                ")" +
#endif
                " From ttt_aid_uni "
                , true);
            if (!ret.result)
            {
                ExecSQL(conn_db, " Drop table ttt_aid_uni ", false);
                ExecSQL(conn_db, " Drop table ttt_aid_gx ", false);
                //ExecSQL(conn_db, " Drop table t_opn ", false);
                return false;
            }
            ExecSQL(conn_db, " Drop table ttt_aid_uni ", false);


            //вычислить кол-во дней перекрытия периода убытия и занести в cnt2
            ExecSQL(conn_db, " Drop table ttt_aid_cnt ", false);
#if PG
            ret = ExecSQL(conn_db,
                            " Select distinct a.nzp_kvar,a.nzp_gil, " +
                //     [---------------] a. gil_periods
                //  [-----------]        b. прибытие - урезаем
                                    " case when " + ZamenaS(gilec, "b.dat_s") + " <= " + ZamenaS(gilec, "a.dat_s") + " and " +
                                                    ZamenaPo(gilec, "b.dat_po") + " < " + ZamenaPo(gilec, "a.dat_po") +
                                    " then (" + ZamenaPo(gilec, "b.dat_po") + " +interval '1 day' - " + ZamenaS(gilec, "a.dat_s") + ") " +
                                    " else " +
                //  [---------------]    a.
                //        [-----------]  b.
                                    " case when " + ZamenaS(gilec, "b.dat_s") + " >= " + ZamenaS(gilec, "a.dat_s") + " and " +
                                                    ZamenaPo(gilec, "b.dat_po") + " > " + ZamenaPo(gilec, "a.dat_po") +
                                    " then (" + ZamenaPo(gilec, "a.dat_po") + " +interval '1 day' - " + ZamenaS(gilec, "b.dat_s") + ") " +
                                    " else " +
                //      [----------]     a.
                //   [---------------]   b.
                                    " case when " + ZamenaS(gilec, "b.dat_s") + " <= " + ZamenaS(gilec, "a.dat_s") + " and " +
                                                    ZamenaPo(gilec, "b.dat_po") + " >= " + ZamenaPo(gilec, "a.dat_po") +
                                    " then (" + ZamenaPo(gilec, "a.dat_po") + " +interval '1 day' - " + ZamenaS(gilec, "a.dat_s") + ") " +
                                    " else " +
                //  [---------------]    a.
                //    [----------]       b.
                                    " (" + ZamenaPo(gilec, "b.dat_po") + " +interval '1 day' - " + ZamenaS(gilec, "b.dat_s") + ")" +
                            " end end end as cnt_del2 " +
                               " Into temp ttt_aid_cnt  "+
                            " From " + gilec.gil_xx + " a, " + gilec.gil_xx + " b " +
                            " Where 1 = 1 " + st1 +
                            "   and a.nzp_kvar = b.nzp_kvar " +
                            "   and a.nzp_gil = b.nzp_gil " +
                            "   and a.stek = 2 " +
                            "   and b.stek = 1 " +
                            "   and a.cur_zap <> -1 " +
                            "   and b.cur_zap <> -1 " +
                            "   and a.dat_s <= b.dat_po " +
                            "   and a.dat_po>= a.dat_s " +
                            "   and a.dat_po >=" + gilec.paramcalc.dat_s +
                            "   and b.dat_po >=" + gilec.paramcalc.dat_s                         
                            , true);
#else
ret = ExecSQL(conn_db,
                " Select unique a.nzp_kvar,a.nzp_gil, " +
                //     [---------------] a. gil_periods
                //  [-----------]        b. прибытие - урезаем
                        " case when " + ZamenaS(gilec, "b.dat_s") + " <= " + ZamenaS(gilec, "a.dat_s") + " and " +
                                        ZamenaPo(gilec, "b.dat_po") + " < " + ZamenaPo(gilec, "a.dat_po") +
                        " then (" + ZamenaPo(gilec, "b.dat_po") + " +1 - " + ZamenaS(gilec, "a.dat_s") + ") " +
                        " else " +
                //  [---------------]    a.
                //        [-----------]  b.
                        " case when " + ZamenaS(gilec, "b.dat_s") + " >= " + ZamenaS(gilec, "a.dat_s") + " and " +
                                        ZamenaPo(gilec, "b.dat_po") + " > " + ZamenaPo(gilec, "a.dat_po") +
                        " then (" + ZamenaPo(gilec, "a.dat_po") + " +1 - " + ZamenaS(gilec, "b.dat_s") + ") " +
                        " else " +
                //      [----------]     a.
                //   [---------------]   b.
                        " case when " + ZamenaS(gilec, "b.dat_s") + " <= " + ZamenaS(gilec, "a.dat_s") + " and " +
                                        ZamenaPo(gilec, "b.dat_po") + " >= " + ZamenaPo(gilec, "a.dat_po") +
                        " then (" + ZamenaPo(gilec, "a.dat_po") + " +1 - " + ZamenaS(gilec, "a.dat_s") + ") " +
                        " else " +
                //  [---------------]    a.
                //    [----------]       b.
                        " (" + ZamenaPo(gilec, "b.dat_po") + " +1 - " + ZamenaS(gilec, "b.dat_s") + ")" +
                " end end end as cnt_del2 " +
                " From " + gilec.gil_xx + " a, " + gilec.gil_xx + " b " +
                " Where 1 = 1 " + st1 +
                "   and a.nzp_kvar = b.nzp_kvar " +
                "   and a.nzp_gil = b.nzp_gil " +
                "   and a.stek = 2 " +
                "   and b.stek = 1 " +
                "   and a.cur_zap <> -1 " +
                "   and b.cur_zap <> -1 " +
                "   and a.dat_s <= b.dat_po " +
                "   and a.dat_po>= a.dat_s " +
                "   and a.dat_po >=" + gilec.paramcalc.dat_s +
                "   and b.dat_po >=" + gilec.paramcalc.dat_s +
                " Into temp ttt_aid_cnt With no log "
                , true);
#endif

            if (!ret.result)
            {
                ExecSQL(conn_db, " Drop table ttt_aid_gx ", false);
                //ExecSQL(conn_db, " Drop table t_opn ", false);
                return false;
            }

            ret = ExecSQL(conn_db, " Create index ix_aid_cn1 on ttt_aid_cnt (nzp_kvar, nzp_gil) ", true);
            if (!ret.result)
            {
                ExecSQL(conn_db, " Drop table ttt_aid_cnt ", false);
                ExecSQL(conn_db, " Drop table ttt_aid_gx ", false);
                //ExecSQL(conn_db, " Drop table t_opn ", false);
                return false;
            }

            //кол-во дней временного выбытия
#if PG
            ExecByStep(conn_db, gilec.gil_xx, "nzp_gx",
                          " Update " + gilec.gil_xx +
                          " Set cnt2 = ( Select sum(" + "EXTRACT('days' from cnt_del2 )) From ttt_aid_cnt a " +
                                       " Where " + gilec.gil_xx + ".nzp_kvar = a.nzp_kvar " +
                                       "   and " + gilec.gil_xx + ".nzp_gil = a.nzp_gil " +
                                     " ) " +
                          " Where " + gilec.paramcalc.where_z + gilec.paramcalc.per_dat_charge +
                          "   and dat_po >= " + gilec.paramcalc.dat_s +
                          "   and dat_s  <= " + gilec.paramcalc.dat_po +
                          "   and stek = 1 " +
                          "   and 0 < ( Select count(*) From ttt_aid_cnt a " +
                                      " Where " + gilec.gil_xx + ".nzp_kvar = a.nzp_kvar " +
                                      "   and " + gilec.gil_xx + ".nzp_gil = a.nzp_gil " +
                                     " ) "
                        , 100000, "", out ret);
#else
  ExecByStep(conn_db, gilec.gil_xx, "nzp_gx",
                " Update " + gilec.gil_xx +
                " Set cnt2 = ( Select sum(" + gilec.paramcalc.pref + "_data:sortnum( cnt_del2 )) From ttt_aid_cnt a " +
                             " Where " + gilec.gil_xx + ".nzp_kvar = a.nzp_kvar " +
                             "   and " + gilec.gil_xx + ".nzp_gil = a.nzp_gil " +
                           " ) " +
                " Where " + gilec.paramcalc.where_z + gilec.paramcalc.per_dat_charge +
                "   and dat_po >= " + gilec.paramcalc.dat_s +
                "   and dat_s  <= " + gilec.paramcalc.dat_po +
                "   and stek = 1 " +
                "   and 0 < ( Select count(*) From ttt_aid_cnt a " +
                            " Where " + gilec.gil_xx + ".nzp_kvar = a.nzp_kvar " +
                            "   and " + gilec.gil_xx + ".nzp_gil = a.nzp_gil " +
                           " ) "
              , 100000, "", out ret);
#endif
            if (!ret.result)
            {
                ExecSQL(conn_db, " Drop table ttt_aid_cnt ", false);
                ExecSQL(conn_db, " Drop table ttt_aid_gx ", false);
                //ExecSQL(conn_db, " Drop table t_opn ", false);
                return false;
            }
            ExecSQL(conn_db, " Drop table ttt_aid_cnt ", false);


            //посчитать кол-во жильцов stek =3 & nzp_gil = 1
            ExecSQL(conn_db, " Drop table ttt_itog ", false);



            //string st2 = " (case when cnt1 >= 15 then cnt1 else 0 end) - (case when cnt2>=6 then cnt2 else 0 end )"; //сколько прожил дней с учетом врем. выбытия (м.б.<0)
            //string stv = " (case when cnt2>=6 then cnt2 else 0 end )"; //сколько дней врем. выбытия

            //поскольку уже при вставке в stek=2 проверяется, что временое выбытие > 5 дней, то второй раз проверять вредно!
            string st2 = " (case when cnt1 >= 15 then cnt1 else 0 end) - (case when cnt2>=0 then cnt2 else 0 end )"; //сколько прожил дней с учетом врем. выбытия (м.б.<0)
            string stv = " (case when cnt2>=0 then cnt2 else 0 end )"; //сколько дней врем. выбытия

            //Calendar myCal = CultureInfo.InvariantCulture.Calendar;
#if PG
            ret = ExecSQL(conn_db,
                " Select distinct nzp_kvar, nzp_dom, nzp_gil,dat_charge, " + gilec.paramcalc.dat_s + " as dat_s, " + gilec.paramcalc.dat_po + " as dat_po, " +
                    " case when " + st2 + " >= 15 then 1 else 0 end as cnt1, " +//нормативное кол-во жильцов с учетом врем. выбывших
                    " case when cnt1 >= 15 then 1 else 0 end as cnt2, " +       //кол-во жителей без врем. убытия
                    st2 + " as cnt3, " +                                        //сколько прожил дней с учетом врем. выбытия (м.б.<0)
                    " case when " + st2 + " > 0 then (" + st2 + ")/" + DateTime.DaysInMonth(paramcalc.calc_yy, paramcalc.calc_mm) + " else 0 end as val1 " + //доля бытия в месяце
                    ",case when " + stv + " > 0 then (" + stv + ")/" + DateTime.DaysInMonth(paramcalc.calc_yy, paramcalc.calc_mm) + " else 0 end as val5 " + //доля врем. выбытия в месяце
              " Into temp ttt_itog  " +
                    " From " + gilec.gil_xx +
               " Where " + gilec.paramcalc.where_z + gilec.paramcalc.per_dat_charge +
               "   and stek = 1 " +
               "   and cur_zap <> -1 "
               , true);
#else
            ret = ExecSQL(conn_db,
                " Select unique nzp_kvar, nzp_dom, nzp_gil,dat_charge, " + gilec.paramcalc.dat_s + " as dat_s, " + gilec.paramcalc.dat_po + " as dat_po, " +
                    " case when " + st2 + " >= 15 then 1 else 0 end as cnt1, " +//нормативное кол-во жильцов с учетом врем. выбывших
                    " case when cnt1 >= 15 then 1 else 0 end as cnt2, " +       //кол-во жителей без врем. убытия
                    st2 + " as cnt3, " +                                        //сколько прожил дней с учетом врем. выбытия (м.б.<0)
                    " case when " + st2 + " > 0 then (" + st2 + ")/" + DateTime.DaysInMonth(paramcalc.calc_yy, paramcalc.calc_mm) + " else 0 end as val1 " + //доля бытия в месяце
                    ",case when " + stv + " > 0 then (" + stv + ")/" + DateTime.DaysInMonth(paramcalc.calc_yy, paramcalc.calc_mm) + " else 0 end as val5 " + //доля врем. выбытия в месяце
               " From " + gilec.gil_xx +
               " Where " + gilec.paramcalc.where_z + gilec.paramcalc.per_dat_charge +
               "   and stek = 1 " +
               "   and cur_zap <> -1 " +
               " Into temp ttt_itog With no log "
               , true);
#endif
            if (!ret.result)
            {
                ExecSQL(conn_db, " Drop table ttt_aid_gx ", false);
                //ExecSQL(conn_db, " Drop table t_opn ", false);
                return false;
            }

            ExecByStep(conn_db, "ttt_itog", "nzp_kvar",
                " Insert into " + gilec.gil_xx + " ( nzp_kvar,nzp_dom,dat_charge,nzp_gil,stek, dat_s,dat_po, cnt1,cnt2,cnt3,val1,val5 ) " +
                " Select nzp_kvar,nzp_dom,dat_charge,1 nzp_gil,3 stek, dat_s,dat_po, sum(cnt1),sum(cnt2),sum(cnt3),sum(val1),sum(val5) " +
                " From ttt_itog where 1=1 "
                , 100000, " Group by 1,2,3,4,5,6,7 ", out ret);
            if (!ret.result)
            {
                ExecSQL(conn_db, " Drop table ttt_itog   ", false);
                ExecSQL(conn_db, " Drop table ttt_aid_gx ", false);
                //ExecSQL(conn_db, " Drop table t_opn ", false);
                return false;
            }
            UpdateStatistics(false, paramcalc, gilec.gilec_tab, out ret);

            ExecSQL(conn_db, " Drop table ttt_itog ", false);



            //загнать параметры жильцов в stek = 3 (nzp_prm: 5,131 val2,val3)
            ExecSQL(conn_db, " Drop table ttt_aid_prm ", false);

#if PG
            ret = ExecSQL(conn_db,
                          " Select p.nzp as nzp_kvar, p.nzp_prm, 0 as kod, max(coalesce(p.val_prm::int,0)+0) as val_prm " +
                            " Into temp ttt_aid_prm  "+
                          " From " + gilec.paramcalc.pref + "_data.prm_1 p,t_opn a " +
                          " Where a.nzp_kvar = p.nzp " +
                          "   and p.nzp_prm in (5,131) " +
                          "   and p.is_actual <> 100 " +
                          "   and p.dat_s  <= " + gilec.paramcalc.dat_po +
                          "   and p.dat_po >= " + gilec.paramcalc.dat_s +
                          " Group by 1,2,3 "                        
                         , true);
#else
  ret = ExecSQL(conn_db,
                " Select p.nzp as nzp_kvar, p.nzp_prm, 0 as kod, max(nvl(p.val_prm,0)+0) as val_prm " +
                " From " + gilec.paramcalc.pref + "_data:prm_1 p,t_opn a " +
                " Where a.nzp_kvar = p.nzp " +
                "   and p.nzp_prm in (5,131,10) " +
                "   and p.is_actual <> 100 " +
                "   and p.dat_s  <= " + gilec.paramcalc.dat_po +
                "   and p.dat_po >= " + gilec.paramcalc.dat_s +
                " Group by 1,2,3 " +
                " Into temp ttt_aid_prm With no log "
               , true);
#endif
            if (!ret.result)
            {
                ExecSQL(conn_db, " Drop table ttt_aid_gx ", false);
                //ExecSQL(conn_db, " Drop table t_opn ", false);
                return false;
            }

            ret = ExecSQL(conn_db, " Create index ix_aid_prm1 on ttt_aid_prm (nzp_kvar, nzp_prm) "
               , true);
            if (!ret.result)
            {
                ExecSQL(conn_db, " Drop table ttt_aid_prm ", false);
                ExecSQL(conn_db, " Drop table ttt_aid_gx ", false);
                //ExecSQL(conn_db, " Drop table t_opn ", false);
                return false;
            }
            ret = ExecSQL(conn_db, " Create index ix_aid_prm2 on ttt_aid_prm (nzp_kvar, kod) "
               , true);
            if (!ret.result)
            {
                ExecSQL(conn_db, " Drop table ttt_aid_prm ", false);
                ExecSQL(conn_db, " Drop table ttt_aid_gx ", false);
                //ExecSQL(conn_db, " Drop table t_opn ", false);
                return false;
            }


            ret = ExecSQL(conn_db,
                " Update ttt_aid_prm " +
                " Set kod = 1 " +
                " Where 0 < ( Select count(*) From " + gilec.gil_xx + " a " +
                            " Where ttt_aid_prm.nzp_kvar = a.nzp_kvar " +
                "   and a.stek = 3 " +
                " ) "
               , true);
            if (!ret.result)
            {
                ExecSQL(conn_db, " Drop table ttt_aid_prm ", false);
                ExecSQL(conn_db, " Drop table ttt_aid_gx ", false);
                //ExecSQL(conn_db, " Drop table t_opn ", false);
                return false;
            }


            ExecByStep(conn_db, gilec.gil_xx, "nzp_gx",
                " Update " + gilec.gil_xx +
#if PG
                " Set val2 =( Select max(case when nzp_prm = 5   then val_prm else 0.00 end) " +
                            " From ttt_aid_prm a " +
                            " Where " + gilec.gil_xx + ".nzp_kvar = a.nzp_kvar and a.kod = 1 )," +
                     "val3 = ( Select max(case when nzp_prm = 131 then val_prm else 0.00 end) " +
                            " From ttt_aid_prm a " +
                            " Where " + gilec.gil_xx + ".nzp_kvar = a.nzp_kvar and a.kod = 1 ) " +

#else
               " Set (val2,val3) = (( " +
                            " Select max(case when nzp_prm = 5   then val_prm else 0.00 end), " +
                                    " max(case when nzp_prm = 131 then val_prm else 0.00 end) " +
                                    " " +
                            " From ttt_aid_prm a " +
                            " Where " + gilec.gil_xx + ".nzp_kvar = a.nzp_kvar and a.kod = 1 " +
                                  " )) " +
#endif
                " Where " + gilec.paramcalc.where_z + gilec.paramcalc.per_dat_charge +
                "   and stek = 3 " +
                "   and 0 < ( Select count(*) From ttt_aid_prm a " +
                            " Where " + gilec.gil_xx + ".nzp_kvar = a.nzp_kvar " +
                            "   and a.kod = 1 and nzp_prm<>10 " +
                          " ) "
              , 100000, "", out ret);


            ExecByStep(conn_db, gilec.gil_xx, "nzp_gx",
                " Update " + gilec.gil_xx +
                " Set val5 = ( " +
                            " Select max(val_prm) " +
                            " From ttt_aid_prm a " +
                            " Where " + gilec.gil_xx + ".nzp_kvar = a.nzp_kvar and a.kod = 1 and nzp_prm=10 " +
                                  " ) " +
                " Where " + gilec.paramcalc.where_z + gilec.paramcalc.per_dat_charge +
                "   and stek = 3 " +
                "   and 0 < ( Select count(*) From ttt_aid_prm a " +
                            " Where " + gilec.gil_xx + ".nzp_kvar = a.nzp_kvar " +
                            "   and a.kod = 1 and nzp_prm=10 " +
                          " ) "+
                "   and 0 = ( Select count(*) From ttt_prm_1 a " +
                            " Where " + gilec.gil_xx + ".nzp_kvar = a.nzp " +
                            "   and nzp_prm=130 and val_prm='1'" +
                          " ) "
              , 100000, "", out ret);

            if (!ret.result)
            {
                ExecSQL(conn_db, " Drop table ttt_aid_prm ", false);
                ExecSQL(conn_db, " Drop table ttt_aid_gx ", false);
                //ExecSQL(conn_db, " Drop table t_opn ", false);
                return false;
            }

            //вставить строки с kod = 0 (нет данных паспортистки, но есть жильцовые параметры)
            ret = ExecSQL(conn_db,
                " Insert into " + gilec.gil_xx + " ( nzp_kvar,nzp_dom,dat_charge,nzp_gil,stek, dat_s,dat_po, cnt1,cnt2,cnt3,val1,val2,val3,val5 ) " +
                " Select a.nzp_kvar,a.nzp_dom," + p_dat_charge + ",0 nzp_gil,3 stek, " + gilec.paramcalc.dat_s + "," + gilec.paramcalc.dat_po + ", 0,0,0,0, " +
                       " max(case when nzp_prm = 5   then val_prm+0 else 0 end)," +
                       " max(case when nzp_prm = 131 then val_prm+0 else 0 end) " +
                       " , case when  max(case when nzp_prm = 5 then val_prm+0 else 0.00 end)> "+
                       "              max(case when nzp_prm = 10 then val_prm+0 else 0.00 end) " +
                       "    then max(case when nzp_prm = 10 then val_prm+0 else 0.00 end) "+
                       "  else max(case when nzp_prm = 5   then val_prm+0 else 0.00 end) end " +                     
                " From t_opn a, ttt_aid_prm b " +
                " Where a.nzp_kvar = b.nzp_kvar " +
                "   and b.kod = 0 " +
                "   and val_prm>0 " +
                " Group by 1,2,3,4,5,6,7,8,9,10,11 "
               , true);
            if (!ret.result)
            {
                ExecSQL(conn_db, " Drop table ttt_aid_prm ", false);
                ExecSQL(conn_db, " Drop table ttt_aid_gx ", false);
                //ExecSQL(conn_db, " Drop table t_opn ", false);
                return false;
            }

            UpdateStatistics(false, paramcalc, gilec.gilec_tab, out ret);

            ExecSQL(conn_db, " Drop table ttt_aid_prm ", false);
            ExecSQL(conn_db, " Drop table ttt_aid_gx ", false);
            //ExecSQL(conn_db, " Drop table t_opn ", false);
            //
            IDataReader reader;
            //
            //считать по аис паспортистка kod_info = 1
#if PG
            ret = ExecRead(conn_db, out reader,
                       " Select count(*) cnt From " + gilec.paramcalc.pref + "_data.prm_10 p Where p.nzp_prm = 89 " +
                       " and p.is_actual <> 100 and p.dat_s  <= " + gilec.paramcalc.dat_po + " and p.dat_po >= " + gilec.paramcalc.dat_s + " "
                       , true);
#else
 ret = ExecRead(conn_db, out reader,
            " Select count(*) cnt From " + gilec.paramcalc.pref + "_data:prm_10 p Where p.nzp_prm = 89 " +
            " and p.is_actual <> 100 and p.dat_s  <= " + gilec.paramcalc.dat_po + " and p.dat_po >= " + gilec.paramcalc.dat_s + " "
            , true);
#endif
            if (!ret.result)
            {
                ret.text = "Ошибка поиска признака -Разрешить подневной расчет - подключена АИС Паспортистка ЖЭУ- ";
                return false;
            }
            bool bIsGood = false;
            if (reader.Read())
            {
                bIsGood = (Convert.ToInt32(reader["cnt"]) > 0);
            }
            reader.Close();
            if (bIsGood)
            {
                // если установлен параметр в prm_10 - nzp_prm=89 'Разрешить подневной расчет - подключена АИС Паспортистка ЖЭУ'
                ExecByStep(conn_db, gilec.gil_xx, "nzp_gx",
                    " Update " + gilec.gil_xx + " Set kod_info = 1 " +
                    " Where " + gilec.paramcalc.where_z + gilec.paramcalc.per_dat_charge + " and stek = 3 "
                  , 100000, "", out ret);
            }
            else
            {
#if PG
                ret = ExecRead(conn_db, out reader,
                               " Select count(*) cnt From " + gilec.paramcalc.pref + "_data.prm_10 p Where p.nzp_prm = 129 " +
                               " and p.is_actual <> 100 and p.dat_s  <= " + gilec.paramcalc.dat_po + " and p.dat_po >= " + gilec.paramcalc.dat_s + " "
                               , true);
#else
 ret = ExecRead(conn_db, out reader,
                " Select count(*) cnt From " + gilec.paramcalc.pref + "_data:prm_10 p Where p.nzp_prm = 129 " +
                " and p.is_actual <> 100 and p.dat_s  <= " + gilec.paramcalc.dat_po + " and p.dat_po >= " + gilec.paramcalc.dat_s + " "
                , true);
#endif
                if (!ret.result)
                {
                    ret.text = "Ошибка поиска признака -Учет льгот без жильцов- ";
                    return false;
                }
                bIsGood = false;
                if (reader.Read())
                {
                    bIsGood = (Convert.ToInt32(reader["cnt"]) == 0);
                }
                reader.Close();
                if (bIsGood)
                {
                    // если НЕ установлен параметр в prm_10 - nzp_prm=129 'Учет льгот без жильцов' и есть льготы
#if PG
                    ExecByStep(conn_db, gilec.gil_xx, "nzp_gx",
                                           " Update " + gilec.gil_xx + " Set kod_info = 1 " +
                                           " Where " + gilec.paramcalc.where_z + gilec.paramcalc.per_dat_charge + " and stek = 3 " +
                                           "   and 0 < ( Select count(*) From " + gilec.paramcalc.pref + "_data.lgots l " +
                                           " Where " + gilec.gil_xx + ".nzp_kvar = l.nzp_kvar and l.is_actual <> 100" +
                                           " ) "
                                         , 100000, "", out ret);
#else
 ExecByStep(conn_db, gilec.gil_xx, "nzp_gx",
                        " Update " + gilec.gil_xx + " Set kod_info = 1 " +
                        " Where " + gilec.paramcalc.where_z + gilec.paramcalc.per_dat_charge + " and stek = 3 " +
                        "   and 0 < ( Select count(*) From " + gilec.paramcalc.pref + "_data:lgots l " +
                        " Where " + gilec.gil_xx + ".nzp_kvar = l.nzp_kvar and l.is_actual <> 100" +
                        " ) "
                      , 100000, "", out ret);
#endif
                }

                // если установлен параметр в prm_1 - nzp_prm= 90 'Разрешить подневной расчет для лицевого счета'
                // если установлен параметр в prm_1 - nzp_prm=130 'Считать количество жильцов по АИС Паспортистка ЖЭУ'
#if PG
                ExecByStep(conn_db, gilec.gil_xx, "nzp_gx",
                                   " Update " + gilec.gil_xx + " Set kod_info = 1 " +
                                   " Where " + gilec.paramcalc.where_z + gilec.paramcalc.per_dat_charge + " and stek = 3 " +
                                   "   and 0 < ( Select count(*) From " + gilec.paramcalc.pref + "_data.prm_1 p " +
                                   "   Where " + gilec.gil_xx + ".nzp_kvar = p.nzp and p.nzp_prm in (90,130)" +
                                   "   and p.is_actual <> 100 and p.dat_s  <= " + gilec.paramcalc.dat_po + " and p.dat_po >= " + gilec.paramcalc.dat_s +
                                   "   ) "
                                 , 100000, "", out ret);
#else
 ExecByStep(conn_db, gilec.gil_xx, "nzp_gx",
                    " Update " + gilec.gil_xx + " Set kod_info = 1 " +
                    " Where " + gilec.paramcalc.where_z + gilec.paramcalc.per_dat_charge + " and stek = 3 " +
                    "   and 0 < ( Select count(*) From " + gilec.paramcalc.pref + "_data:prm_1 p " +
                    "   Where " + gilec.gil_xx + ".nzp_kvar = p.nzp and p.nzp_prm in (90,130)" +
                    "   and p.is_actual <> 100 and p.dat_s  <= " + gilec.paramcalc.dat_po + " and p.dat_po >= " + gilec.paramcalc.dat_s +
                    "   ) "
                  , 100000, "", out ret);
#endif
                //
            }

            // val4 - кол-во жильцов по данным АИС Пасспортистка - до этого - val1 ! Сохраним !
            ExecByStep(conn_db, gilec.gil_xx, "nzp_gx",
                " Update " + gilec.gil_xx + " Set val4 = val1 " +
                " Where " + gilec.paramcalc.where_z + gilec.paramcalc.per_dat_charge + " and stek = 3 "
              , 100000, "", out ret);

            // val1 - итоговое кол-во жильцов по данным АИС Пасспортистка или параметру nzp_prm=5 (val2) с учетом врем. выбытия (val5)
            ExecByStep(conn_db, gilec.gil_xx, "nzp_gx",
                " Update " + gilec.gil_xx + " Set val1 = val2 - val5, cnt2 = round(val2) " +
                " Where " + gilec.paramcalc.where_z + gilec.paramcalc.per_dat_charge + " and stek = 3 and kod_info <> 1 "
              , 100000, "", out ret);

            // cnt1 - итоговое целое кол-во жильцов по данным АИС Пасспортистка или параметру nzp_prm=5
            ExecByStep(conn_db, gilec.gil_xx, "nzp_gx",
                " Update " + gilec.gil_xx + " Set cnt1 = round(val1) " +
                " Where " + gilec.paramcalc.where_z + gilec.paramcalc.per_dat_charge + " and stek = 3 "
              , 100000, "", out ret);
            /*
            if (false) //(!(gilec.paramcalc.nzp_kvar>0))
            {
                //проверить, что nzp_gil живет в нескольких nzp_kvar
#if PG
ret = ExecRead(conn_db, out reader,
                    " Select a.nzp_gil, a.nzp_kvar, k.num_ls, k.nkvar, max( coalesce(trim(fam),' ')||' '||' '||coalesce(trim(ima),' ')||' '||coalesce(trim(otch),' ') ) as fio " +
                    " From " + gilec.gil_xx + " a, " + gilec.gil_xx + " b, " + gilec.paramcalc.pref + "_data.kvar k, " + gilec.paramcalc.pref + "_data.kart g " +
                    " Where 1 = 1 " + st1 +
                    "   and a.nzp_gil = b.nzp_gil " +
                    "   and a.nzp_kvar <> b.nzp_kvar " +
                    "   and a.stek = 1 " +
                    "   and a.stek = b.stek " + 
                    "   and a.nzp_kvar = k.nzp_kvar " +
                    "   and a.nzp_gil  = g.nzp_gil  " +
                    "   and a.cnt1 - a.cnt2 > 0 " +
                    "   and b.cnt1 - b.cnt2 > 0 " +
                    " Group by 1,2,3,4 " +
                    " Order by 5,4 "
                    , true, 900);
#else
ret = ExecRead(conn_db, out reader,
                    " Select a.nzp_gil, a.nzp_kvar, k.num_ls, k.nkvar, max( nvl(trim(fam),' ')||' '||' '||nvl(trim(ima),' ')||' '||nvl(trim(otch),' ') ) as fio " +
                    " From " + gilec.gil_xx + " a, " + gilec.gil_xx + " b, " + gilec.paramcalc.pref + "_data:kvar k, " + gilec.paramcalc.pref + "_data:kart g " +
                    " Where 1 = 1 " + st1 +
                    "   and a.nzp_gil = b.nzp_gil " +
                    "   and a.nzp_kvar <> b.nzp_kvar " +
                    "   and a.stek = 1 " +
                    "   and a.stek = b.stek " + 
                    "   and a.nzp_kvar = k.nzp_kvar " +
                    "   and a.nzp_gil  = g.nzp_gil  " +
                    "   and a.cnt1 - a.cnt2 > 0 " +
                    "   and b.cnt1 - b.cnt2 > 0 " +
                    " Group by 1,2,3,4 " +
                    " Order by 5,4 "
                    , true, 900);
#endif
                if (!ret.result)
                {
                    conn_db.Close();
                    return false;
                }
                try
                {
                    string mes = "";
                    while (reader.Read())
                    {
                        mes += "\r\n";
                        if (reader["fio"] != DBNull.Value)
                            mes += " " + (string)reader["fio"];
                        if (reader["nkvar"] != DBNull.Value)
                            mes += " № кв. " + (string)reader["nkvar"];
                        if (reader["num_ls"] != DBNull.Value)
                            mes += " № лс " + (int)reader["num_ls"];
                    }
                    reader.Close();

                    if (mes.Trim() != "")
                    {
                        ret.result = false;
                        ret.text = "Обнаружены одновременное проживание людей в разных квартирах. Смотрите LOG-журнал";

                        MonitorLog.WriteLog("Обнаружены одновременное проживание людей в разных квартирах" + mes, MonitorLog.typelog.Error, 30, 301, true);

                    }
                }
                catch (Exception ex)
                {
                    reader.Close();
                    ret.result = false;
                    ret.text = ex.Message;
                    return false;
                }
            }
            */
            return true;
        }

        string ZamenaS(GilecXX gilec, string dat)
        {
            return " (case when " + dat + " < " + gilec.paramcalc.dat_s + " then " + gilec.paramcalc.dat_s + " else " + dat + " end )";
        }
        string ZamenaPo(GilecXX gilec, string dat)
        {
            return " (case when " + dat + " > " + gilec.paramcalc.dat_po + " then " + gilec.paramcalc.dat_po + " else " + 
                dat + " end )";
                //dat + "+1 end )";
        }


    }
}


