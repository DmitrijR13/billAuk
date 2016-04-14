using System;
using System.Linq;
using System.Text;
using System.Data;
using System.Collections.Generic;
using System.Globalization;

using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.IFMX.Kernel.source.CommonType;

namespace STCLINE.KP50.DataBase
{
    //здась находятся классы для подсчета расходов
    public partial class DbCalcCharge : DataBaseHead
    {
        #region Создать структуру GilXX
        public struct GilecXX
        {
            public CalcTypes.ParamCalc paramcalc;

            public string gil_xx;
            public string gilec_tab;

            public GilecXX(CalcTypes.ParamCalc _paramcalc)
            {
                paramcalc = _paramcalc;
                paramcalc.b_dom_in = true;

                gilec_tab = "gil" + paramcalc.alias + "_" + paramcalc.calc_mm.ToString("00");
                gil_xx = paramcalc.pref + "_charge_" + (paramcalc.calc_yy - 2000).ToString("00") + tableDelimiter + gilec_tab;
            }
        }
        #endregion Создать структуру GilXX
        //-----------------------------------------------------------------------------

        //-----------------------------------------------------------------------------
        #region Создать таблицу Gil_XX если ее нет - иначе чистка
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

            ret = ExecSQL(conn_db,
                " Create table " + tbluser + gilec.gilec_tab +
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
                "    val1        " + sDecimalType + "(11,7) default 0.00, " +    //итоговое кол-во жильцов в лс с учетом времен. выбывших
                "    val2        " + sDecimalType + "(11,7) default 0.00, " +    //nzp_prm = 5
                "    val3        " + sDecimalType + "(11,7) default 0.00, " +    //nzp_prm = 131
                "    val4        " + sDecimalType + "(11,7) default 0.00, " +    //дробное кол-во жильцов в лс по kart с учетом времен. выбывших
                "    val5        " + sDecimalType + "(11,7) default 0.00, " +    //дробное кол-во времен. выбывших
                "    val6        " + sDecimalType + "(11,7) default 0.00, " +    //Количество не зарегистрированных проживающих
                "    kod_info    integer       default 0 ) "
                , true);
            if (!ret.result) { conn_db.Close(); return; }

            ret = ExecSQL(conn_db, " create unique index " + tbluser + "ix1_" + gilec.gilec_tab + " on " + gilec.gilec_tab + " (nzp_gx) ", true);
            if (!ret.result) { conn_db.Close(); return; }

            ret = ExecSQL(conn_db, " create index " + tbluser + "ix2_" + gilec.gilec_tab + " on " + gilec.gilec_tab + " (nzp_dom,dat_charge) ", true);
            if (!ret.result) { conn_db.Close(); return; }

            ret = ExecSQL(conn_db, " create index " + tbluser + "ix3_" + gilec.gilec_tab + " on " + gilec.gilec_tab + " (nzp_kvar,dat_charge,stek, dat_s,dat_po) ", true);
            if (!ret.result) { conn_db.Close(); return; }

            ret = ExecSQL(conn_db, " create index " + tbluser + "ix4_" + gilec.gilec_tab + " on " + gilec.gilec_tab + " (nzp_kvar,nzp_gil,dat_charge) ", true);
            if (!ret.result) { conn_db.Close(); return; }

            ret = ExecSQL(conn_db, " create index " + tbluser + "ix5_" + gilec.gilec_tab + " on " + gilec.gilec_tab + " (cur_zap) ", true);
            if (!ret.result) { conn_db.Close(); return; }

            ret = ExecSQL(conn_db, " create index " + tbluser + "ix6_" + gilec.gilec_tab + " on " + gilec.gilec_tab + " (prev_zap) ", true);
            if (!ret.result) { conn_db.Close(); return; }

            ExecSQL(conn_db, sUpdStat + " " + gilec.gilec_tab, true);

            conn_db.Close();
        }
        #endregion Создать таблицу Gil_XX если ее нет - иначе чистка

        #region Удаление временных таблиц
        private void CalcGil_CloseTmp(IDbConnection conn_db)
        //-----------------------------------------------------------------------------
        {
            ExecSQL(conn_db, " Drop table ttt_aid_ub ", false);
            ExecSQL(conn_db, " Drop table ttt_aid_gx ", false);
            ExecSQL(conn_db, " Drop table ttt_aid_uni ", false);
            ExecSQL(conn_db, " Drop table ttt_aid_prm ", false);
            ExecSQL(conn_db, " Drop table ttt_aid_prm_pd ", false);
            ExecSQL(conn_db, " Drop table ttt_aid_cnt ", false);
            ExecSQL(conn_db, " Drop table ttt_itog ", false);
        }
        #endregion Удаление временных таблиц

        #region Выборка параметров из prm_1 для учета кол-ва жильцов цифрой
        private bool SelParamsForCalcGil(IDbConnection conn_db, GilecXX gilec, out Returns ret)
        //-----------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            ExecSQL(conn_db, " Drop table ttt_aid_prm ", false);
            ExecSQL(conn_db, " Drop table ttt_aid_prm_pd ", false);

            ret = ExecSQL(conn_db,
                " CREATE TEMP TABLE ttt_aid_prm (" +
                "   nzp_kvar  integer, " +
                "   kod       integer default 0, " +
                "   val5    " + sDecimalType + "(11,7) not null default 0.00, " + // nzp_prm=5
                "   val131  " + sDecimalType + "(11,7) not null default 0.00, " + // nzp_prm=131
                "   val10   " + sDecimalType + "(11,7) not null default 0.00, " + // nzp_prm=10
                "   val1395 " + sDecimalType + "(11,7) not null default 0.00  " + // nzp_prm=1395
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { CalcGil_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " CREATE TEMP TABLE ttt_aid_prm_pd (" +
                "   nzp_kvar  integer, " +
                "   kod       integer default 0, " +
                "   val5    " + sDecimalType + "(11,7) not null default 0.00, " + // nzp_prm=5
                "   val131  " + sDecimalType + "(11,7) not null default 0.00, " + // nzp_prm=131
                "   val10   " + sDecimalType + "(11,7) not null default 0.00, " + // nzp_prm=10
                "   val1395 " + sDecimalType + "(11,7) not null default 0.00, " + // nzp_prm=1395
                "   nzp_period integer, " +
                "   dp         date," +
                "   dp_end     date," +
                "   cntd       integer, " +
                "   cntd_mn    integer  " +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { CalcGil_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " Insert Into ttt_aid_prm_pd (nzp_kvar,nzp_period,dp,dp_end,cntd,cntd_mn,val5,val131,val10,val1395) " +
                " Select p.nzp as nzp_kvar, a.nzp_period, a.dp, a.dp_end, a.cntd, a.cntd_mn," +
                " max(case when a.dp<=p.dat_po and a.dp_end>=p.dat_s and p.nzp_prm=5    then p.val_prm" + sConvToNum + " else 0 end) as val5," +
                " max(case when a.dp<=p.dat_po and a.dp_end>=p.dat_s and p.nzp_prm=131  then p.val_prm" + sConvToNum + " else 0 end) as val131," +
                " max(case when a.dp<=p.dat_po and a.dp_end>=p.dat_s and p.nzp_prm=10   then p.val_prm" + sConvToNum + " else 0 end) as val10," +
                " max(case when a.dp<=p.dat_po and a.dp_end>=p.dat_s and p.nzp_prm=1395 then p.val_prm" + sConvToNum + " else 0 end) as val1395 " +
                " From ttt_prm_1d p,t_gku_periods a " +
                " Where a.nzp_kvar = p.nzp " +
                "   and p.nzp_prm in (5,131,10,1395) " +
                " Group by 1,2,3,4,5,6 "
                , true);
            if (!ret.result) { CalcGil_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, " Create index ix_aid_prm1_pd on ttt_aid_prm_pd (nzp_kvar,kod) ", true);
            if (!ret.result) { CalcGil_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, " Create index ix_aid_prm2_pd on ttt_aid_prm_pd (nzp_kvar,dp,dp_end) ", true);
            if (!ret.result) { CalcGil_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, " Create index ix_aid_prm3_pd on ttt_aid_prm_pd (nzp_period) ", true);
            if (!ret.result) { CalcGil_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, sUpdStat + " ttt_aid_prm_pd ", true);
            if (!ret.result) { CalcGil_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " Insert Into ttt_aid_prm (nzp_kvar,val5,val131,val10,val1395) " +
                " Select p.nzp as nzp_kvar," +
                " max(case when p.nzp_prm=5    then p.val_prm" + sConvToNum + " else 0 end) as val5," +
                " max(case when p.nzp_prm=131  then p.val_prm" + sConvToNum + " else 0 end) as val131," +
                " max(case when p.nzp_prm=10   then p.val_prm" + sConvToNum + " else 0 end) as val10," +
                " max(case when p.nzp_prm=1395 then p.val_prm" + sConvToNum + " else 0 end) as val1395 " +
                " From ttt_prm_1d p,t_opn a " +
                " Where a.nzp_kvar = p.nzp " +
                "   and p.nzp_prm in (5,131,10,1395) " +
                " Group by 1 "
                , true);
            if (!ret.result) { CalcGil_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, " Create index ix_aid_prm1 on ttt_aid_prm (nzp_kvar, kod) ", true);
            if (!ret.result) { CalcGil_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, sUpdStat + " ttt_aid_prm ", true);
            if (!ret.result) { CalcGil_CloseTmp(conn_db); return false; }

            return true;
        }
        #endregion Выборка параметров из prm_1 для учета кол-ва жильцов цифрой

        #region вычислить кол-во дней перекрытия периода убытия и занести в cnt2
        public bool CalcGilToUseGilPeriods(IDbConnection conn_db, string st1, string sTabName, string pDatS, string pDatPo, string pWhere, string pDataAlias, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            //вычислить кол-во дней перекрытия периода убытия и занести в cnt2
            ExecSQL(conn_db, " Drop table ttt_aid_cnt ", false);

            ret = ExecSQL(conn_db,
                " CREATE TEMP TABLE ttt_aid_cnt (" +
                "   nzp_kvar  integer," +
                "   nzp_gil   integer," +
#if PG
 "   cnt_del2 interval hour " +
#else
                "   cnt_del2 INTERVAL HOUR(3) to HOUR " +
#endif
 " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { CalcGil_CloseTmp(conn_db); return false; }

            string sIntervalDay;
#if PG
            sIntervalDay = "interval '1 day'";
#else
            sIntervalDay = "1";
#endif

            ret = ExecSQL(conn_db,
                " Insert Into ttt_aid_cnt (nzp_kvar,nzp_gil,cnt_del2) " +
                " Select distinct a.nzp_kvar,a.nzp_gil, " +
                //     [---------------] a. gil_periods
                //  [-----------]        b. прибытие - урезаем
                        " case when " + Zamena_S("b.dat_s", pDatS) + " <= " + Zamena_S("a.dat_s", pDatS) + " and " +
                                        Zamena_Po("b.dat_po", pDatPo) + " < " + Zamena_Po("a.dat_po", pDatPo) +
                        " then (" + Zamena_Po("b.dat_po", pDatPo) + " + " + sIntervalDay + " - " + Zamena_S("a.dat_s", pDatS) + ") " +
                        " else " +
                //  [---------------]    a.
                //        [-----------]  b.
                        " case when " + Zamena_S("b.dat_s", pDatS) + " >= " + Zamena_S("a.dat_s", pDatS) + " and " +
                                        Zamena_Po("b.dat_po", pDatPo) + " > " + Zamena_Po("a.dat_po", pDatPo) +
                        " then (" + Zamena_Po("a.dat_po", pDatPo) + " + " + sIntervalDay + " - " + Zamena_S("b.dat_s", pDatS) + ") " +
                        " else " +
                //      [----------]     a.
                //   [---------------]   b.
                        " case when " + Zamena_S("b.dat_s", pDatS) + " <= " + Zamena_S("a.dat_s", pDatS) + " and " +
                                        Zamena_Po("b.dat_po", pDatPo) + " >= " + Zamena_Po("a.dat_po", pDatPo) +
                        " then (" + Zamena_Po("a.dat_po", pDatPo) + " + " + sIntervalDay + " - " + Zamena_S("a.dat_s", pDatS) + ") " +
                        " else " +
                //  [---------------]    a.
                //    [----------]       b.
                             " (" + Zamena_Po("b.dat_po", pDatPo) + " + " + sIntervalDay + " - " + Zamena_S("b.dat_s", pDatS) + ")" +
                " end end end as cnt_del2 " +
                " From " + sTabName + " a, " + sTabName + " b , t_selkvar t " +
                " Where 1 = 1 " + st1 +
                "   and a.nzp_kvar = b.nzp_kvar " +
                "   and a.nzp_kvar= t.nzp_kvar" +
                "   and a.nzp_gil = b.nzp_gil " +
                "   and a.stek = 2 " +
                "   and b.stek = 1 " +
                "   and a.cur_zap <> -1 " +
                "   and b.cur_zap <> -1 " +
                "   and a.dat_s <= b.dat_po " +
                "   and a.dat_po>= b.dat_s " +
                "   and a.dat_po >=" + pDatS +
                "   and b.dat_po >=" + pDatS
                , true);
            if (!ret.result) { CalcGil_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, " Create index ix_aid_cn1 on ttt_aid_cnt (nzp_kvar, nzp_gil) ", true);
            if (!ret.result) { CalcGil_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, sUpdStat + " ttt_aid_cnt ", true);
            if (!ret.result) { CalcGil_CloseTmp(conn_db); return false; }

            //кол-во дней временного выбытия
            ExecByStep(conn_db, sTabName, "nzp_gx",
                " Update " + sTabName +
                " Set cnt2 = ( Select sum(" +
#if PG
 "EXTRACT('days' from cnt_del2 ) " +
#else
                            pDataAlias + "sortnum( cnt_del2 ) " +
#endif
 ") From ttt_aid_cnt a " +
                             " Where " + sTabName + ".nzp_kvar = a.nzp_kvar " +
                             "   and " + sTabName + ".nzp_gil = a.nzp_gil " +
                           " ) " +
                " Where " + pWhere +
                "   and dat_po >= " + pDatS +
                "   and dat_s  <= " + pDatPo +
                "   and stek = 1 " +
                "   and 0 < ( Select count(*) From ttt_aid_cnt a " +
                            " Where " + sTabName + ".nzp_kvar = a.nzp_kvar " +
                            "   and " + sTabName + ".nzp_gil = a.nzp_gil " +
                           " ) "
              , 100000, "", out ret);
            if (!ret.result) { CalcGil_CloseTmp(conn_db); return false; }

            ExecSQL(conn_db, " Drop table ttt_aid_cnt ", false);

            return true;
        }

        public bool CalcGilToUseGilPeriods_Pd(IDbConnection conn_db, GilecXX gilec, string st1, string pWhere, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            //вычислить кол-во дней перекрытия периода убытия и занести в cnt2
            ExecSQL(conn_db, " Drop table ttt_aid_cnt ", false);

            ret = ExecSQL(conn_db,
                " CREATE TEMP TABLE ttt_aid_cnt (" +
                "   nzp_kvar   integer," +
                "   nzp_gil    integer," +
                "   nzp_period integer," +
#if PG
 "   cnt_del2 interval hour " +
#else
                "   cnt_del2 INTERVAL HOUR(3) to HOUR " +
#endif
 " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { CalcGil_CloseTmp(conn_db); return false; }

            string sIntervalDay;
#if PG
            sIntervalDay = "interval '1 day'";
#else
            sIntervalDay = "1";
#endif

            ret = ExecSQL(conn_db,
                " Insert Into ttt_aid_cnt (nzp_kvar,nzp_gil,nzp_period,cnt_del2) " +
                " Select distinct a.nzp_kvar,a.nzp_gil,b.nzp_period, " +
                //     [---------------] a. gil_periods
                //  [-----------]        b. прибытие - урезаем
                        " case when " + Zamena_S("b.dat_s", "b.dp") + " <= " + Zamena_S("a.dat_s", "b.dp") + " and " +
                                        Zamena_Po("b.dat_po", "b.dp_end") + " < " + Zamena_Po("a.dat_po", "b.dp_end") +
                        " then (" + Zamena_Po("b.dat_po", "b.dp_end") + " + " + sIntervalDay + " - " + Zamena_S("a.dat_s", "b.dp") + ") " +
                        " else " +
                //  [---------------]    a.
                //        [-----------]  b.
                        " case when " + Zamena_S("b.dat_s", "b.dp") + " >= " + Zamena_S("a.dat_s", "b.dp") + " and " +
                                        Zamena_Po("b.dat_po", "b.dp_end") + " > " + Zamena_Po("a.dat_po", "b.dp_end") +
                        " then (" + Zamena_Po("a.dat_po", "b.dp_end") + " + " + sIntervalDay + " - " + Zamena_S("b.dat_s", "b.dp") + ") " +
                        " else " +
                //      [----------]     a.
                //   [---------------]   b.
                        " case when " + Zamena_S("b.dat_s", "b.dp") + " <= " + Zamena_S("a.dat_s", "b.dp") + " and " +
                                        Zamena_Po("b.dat_po", "b.dp_end") + " >= " + Zamena_Po("a.dat_po", "b.dp_end") +
                        " then (" + Zamena_Po("a.dat_po", "b.dp_end") + " + " + sIntervalDay + " - " + Zamena_S("a.dat_s", "b.dp") + ") " +
                        " else " +
                //  [---------------]    a.
                //    [----------]       b.
                             " (" + Zamena_Po("b.dat_po", "b.dp_end") + " + " + sIntervalDay + " - " + Zamena_S("b.dat_s", "b.dp") + ")" +
                " end end end as cnt_del2 " +
                " From " + gilec.gil_xx + " a, ttt_gils_pd b " +
                " Where 1 = 1 " + st1 +
                "   and a.nzp_kvar = b.nzp_kvar " +
                "   and a.nzp_gil = b.nzp_gil " +
                "   and a.stek = 2 " +
                "   and b.stek = 1 " +
                "   and a.cur_zap <> -1 " +
                "   and b.cur_zap <> -1 " +
                "   and a.dat_s  <= b.dat_po " +
                "   and a.dat_po >= b.dat_s " +
                "   and a.dat_s  <= b.dp_end " +
                "   and a.dat_po >= b.dp "
                , true);
            if (!ret.result) { CalcGil_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, " Create index ix_aid_cn1 on ttt_aid_cnt (nzp_kvar, nzp_gil) ", true);
            if (!ret.result) { CalcGil_CloseTmp(conn_db); return false; }

            ExecSQL(conn_db, sUpdStat + " ttt_aid_cnt ", true);
            if (!ret.result) { CalcGil_CloseTmp(conn_db); return false; }

            ViewTbl(conn_db, "select * from ttt_aid_cnt ");

            //кол-во дней временного выбытия
            ExecByStep(conn_db, "ttt_gils_pd", "nzp_gx",
                " Update ttt_gils_pd " +
                " Set cnt2 = ( Select sum(" +
#if PG
 "EXTRACT('days' from cnt_del2 ) " +
#else
                             gilec.paramcalc.pref + "_data:sortnum( cnt_del2 ) " +
#endif
 ") From ttt_aid_cnt a " +
                             " Where ttt_gils_pd.nzp_kvar   = a.nzp_kvar " +
                             "   and ttt_gils_pd.nzp_gil    = a.nzp_gil " +
                             "   and ttt_gils_pd.nzp_period = a.nzp_period " +
                           " ) " +
                " Where " + pWhere +
                "   and stek = 1 " +
                "   and 0 < ( Select count(*) From ttt_aid_cnt a " +
                             " Where ttt_gils_pd.nzp_kvar   = a.nzp_kvar " +
                             "   and ttt_gils_pd.nzp_gil    = a.nzp_gil " +
                             "   and ttt_gils_pd.nzp_period = a.nzp_period " +
                           " ) "
              , 100000, "", out ret);
            if (!ret.result) { CalcGil_CloseTmp(conn_db); return false; }

            ExecSQL(conn_db, " Drop table ttt_aid_cnt ", false);

            return true;
        }
        #endregion вычислить кол-во дней перекрытия периода убытия и занести в cnt2

        #region вычислить кол-во жильцов по периоду и вставить в стек
        public bool CalcGilItogInStek(IDbConnection conn_db, GilecXX gilec, string st1, string sTabName, string pDatS, string pDatPo, string pWhere,
            string sKolDayInPeriod, string sCntDaysMax, string sStek, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            //посчитать кол-во жильцов stek =3 & nzp_gil = 1
            ExecSQL(conn_db, " Drop table ttt_itog ", false);

            ret = ExecSQL(conn_db,
                " CREATE TEMP TABLE ttt_itog (" +
                "   nzp_kvar  integer," +
                "   nzp_dom   integer," +
                "   nzp_gil   integer," +
                "   dat_charge date," +
                "   dat_s      date," +
                "   dat_po     date," +
                "   cnt1      integer," +
                "   cnt2      integer," +
                "   cnt3      integer," +
                "   val1      " + sDecimalType + "(11,7) default 0.00, " +
                "   val5      " + sDecimalType + "(11,7) default 0.00  " +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { CalcGil_CloseTmp(conn_db); return false; }

            //string st2 = " (case when cnt1 >= 15 then cnt1 else 0 end) - (case when cnt2>=6 then cnt2 else 0 end )"; //сколько прожил дней с учетом врем. выбытия (м.б.<0)
            //string stv = " (case when cnt2>=6 then cnt2 else 0 end )"; //сколько дней врем. выбытия

            //поскольку уже при вставке в stek=2 проверяется, что временое выбытие > 5 дней, то второй раз проверять вредно!
            string st2 = " (case when cnt1 >= " + sCntDaysMax + " then cnt1 else 0 end) - (case when cnt2>=0 then cnt2 else 0 end )"; //сколько прожил дней с учетом врем. выбытия (м.б.<0)
            string stv = " (case when cnt2>=0 then cnt2 else 0 end )"; //сколько дней врем. выбытия

            //Calendar myCal = CultureInfo.InvariantCulture.Calendar;
            ret = ExecSQL(conn_db,
                " Insert Into ttt_itog (nzp_kvar,nzp_dom,nzp_gil,dat_charge,dat_s,dat_po,cnt1,cnt2,cnt3,val1,val5) " +
                " Select " + sUniqueWord + " nzp_kvar, nzp_dom, nzp_gil, dat_charge, " + pDatS + " as dat_s, " + pDatPo + " as dat_po, " +
                    " case when " + st2 + " >= " + sCntDaysMax + " then 1 else 0 end as cnt1, " +//нормативное кол-во жильцов с учетом врем. выбывших
                    " case when cnt1 >= " + sCntDaysMax + " then 1 else 0 end as cnt2, " +       //кол-во жителей без врем. убытия
                    st2 + " as cnt3, " +                                        //сколько прожил дней с учетом врем. выбытия (м.б.<0)
                    " case when " + st2 + " > 0 then (" + st2 + ") * 1.00/" + sKolDayInPeriod + " else 0 end as val1 " + //доля бытия в месяце
                    ",case when " + stv + " > 0 then (" + stv + ") * 1.00/" + sKolDayInPeriod + " else 0 end as val5 " + //доля врем. выбытия в месяце
                " From " + sTabName +
                " Where " + pWhere +
                "   and stek = 1 " +
                "   and cur_zap <> -1 "
               , true);
            if (!ret.result) { CalcGil_CloseTmp(conn_db); return false; }

            ExecByStep(conn_db, "ttt_itog", "nzp_kvar",
                " Insert into " + gilec.gil_xx + " ( nzp_kvar,nzp_dom,dat_charge,nzp_gil,stek, dat_s,dat_po, cnt1,cnt2,cnt3,val1,val5 ) " +
                " Select nzp_kvar,nzp_dom,dat_charge,1 nzp_gil," + sStek + " stek, dat_s,dat_po, sum(cnt1),sum(cnt2),sum(cnt3),sum(val1),sum(val5) " +
                " From ttt_itog where 1=1 "
                , 100000, " Group by 1,2,3,4,5,6,7 ", out ret);
            if (!ret.result) { CalcGil_CloseTmp(conn_db); return false; }

            ExecSQL(conn_db, " Drop table ttt_itog ", false);

            return true;
        }
        #endregion вычислить кол-во жильцов по периоду и вставить в стек

        //-----------------------------------------------------------------------------
        public bool CalcGilXX(IDbConnection conn_db, CalcTypes.ParamCalc paramcalc, out Returns ret)
        //-----------------------------------------------------------------------------
        {
            GilecXX gilec = new GilecXX(paramcalc);

            //---------------------------------------------------
            //выбрать множество лицевых счетов
            //---------------------------------------------------
            if (!TempTableInWebCashe(conn_db, "t_opn"))
            {
                ChoiseTempKvar(conn_db, ref paramcalc, out ret);
                if (!ret.result) { conn_db.Close(); return false; }
            }

            // Создать таблицу Gil_XX если ее нет - иначе чистка
            CreateGilXX(conn_db, gilec, out ret);
            if (!ret.result) { conn_db.Close(); return false; }

            string st1;
            if (gilec.paramcalc.b_cur)
                st1 = " and a.dat_charge is null and b.dat_charge is null ";
            else
                st1 = " and a.dat_charge = b.dat_charge and a.dat_charge = " + MDY(gilec.paramcalc.cur_mm, 28, gilec.paramcalc.cur_yy);

            #region заполнить stek = 1 периодами проживания каждого жильца без учета врем.выбытия и значения параметра 5
            //выбрать все карточки по дому
            ExecSQL(conn_db, " Drop table ttt_aid_gx ", false);

            ret = ExecSQL(conn_db,
                " CREATE TEMP TABLE ttt_aid_gx (" +
                "   nzp_kvar  integer," +
                "   nzp_dom   integer," +
                "   nzp_gil   integer," +
                "   nzp_tkrt  integer," +
                "   dat_ofor  date," +
                "   dat_oprp  date " +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { conn_db.Close(); return false; }

            ret = ExecSQL(conn_db,
                " Insert Into ttt_aid_gx (nzp_kvar,nzp_dom,nzp_gil,nzp_tkrt,dat_ofor,dat_oprp) " +
                " Select " + sUniqueWord + " k.nzp_kvar,k.nzp_dom, g.nzp_gil, g.nzp_tkrt, " +
                       sNvlWord + "(g.dat_ofor, " + MDY(1, 1, 1901) + ") as dat_ofor, g.dat_oprp " +
                " From " + gilec.paramcalc.data_alias + "kart g, t_opn k " +
                " Where k.nzp_kvar = g.nzp_kvar " +
                "   and g.nzp_tkrt is not null and " + sNvlWord + "(g.neuch,'0')<>'1' "
                , true);
            if (!ret.result) { conn_db.Close(); return false; }

            ret = ExecSQL(conn_db, " Create index ix_aid_gx1 on ttt_aid_gx (nzp_kvar, nzp_gil, nzp_tkrt, dat_ofor) ", true);
            if (!ret.result) { CalcGil_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, " Create index ix_aid_gx2 on ttt_aid_gx (nzp_tkrt) ", true);
            if (!ret.result) { CalcGil_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, " Create index ix_aid_gx3 on ttt_aid_gx (nzp_gil) ", true);
            if (!ret.result) { CalcGil_CloseTmp(conn_db); return false; }

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
            if (!ret.result) { CalcGil_CloseTmp(conn_db); return false; }

            UpdateStatistics(false, paramcalc, gilec.gilec_tab, out ret);
            if (!ret.result) { CalcGil_CloseTmp(conn_db); return false; }

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
                "   and EXISTS ( Select 1 From ttt_aid_gx g " +
                              " Where " + gilec.gil_xx + ".nzp_kvar = g.nzp_kvar " +
                              "   and " + gilec.gil_xx + ".nzp_gil  = g.nzp_gil " +
                              "   and g.nzp_tkrt = 2 " +
                              "   and " + gilec.gil_xx + ".dat_s < g.dat_ofor " +
                            " ) "
               , 100000, "", out ret);
            if (!ret.result) { CalcGil_CloseTmp(conn_db); return false; }

            UpdateStatistics(false, paramcalc, gilec.gilec_tab, out ret);
            if (!ret.result) { CalcGil_CloseTmp(conn_db); return false; }

            //вставляем одинокие карточки убытия
            ExecSQL(conn_db, " Drop table ttt_aid_ub ", false);

            ret = ExecSQL(conn_db,
                " CREATE TEMP TABLE ttt_aid_ub (" +
                "   nzp_kvar  integer," +
                "   nzp_dom   integer," +
                "   nzp_gil   integer," +
                "   nzp_tkrt  integer," +
                "   dat_ofor  date " +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { CalcGil_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " Insert Into ttt_aid_ub (nzp_kvar,nzp_dom,nzp_gil,nzp_tkrt,dat_ofor) " +
                " Select nzp_kvar, nzp_dom, nzp_gil, nzp_tkrt, dat_ofor " +
                " From ttt_aid_gx g " +
                " Where g.nzp_tkrt = 2 " +
                "   and g.dat_ofor<= " + gilec.paramcalc.dat_po +
                //не были выбраны
                "   and NOT EXISTS ( Select 1 From " + gilec.gil_xx + " gx " +
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
                     ")"
                , true);
            if (!ret.result) { CalcGil_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, " Create index ix_aid_ub1 on ttt_aid_ub (nzp_dom) ", true);
            if (!ret.result) { CalcGil_CloseTmp(conn_db); return false; }

            ExecByStep(conn_db, "ttt_aid_ub", "nzp_dom",
                " Insert into " + gilec.gil_xx + " ( nzp_kvar,nzp_dom,dat_charge,nzp_gil, dat_po, stek ) " +
                " Select nzp_kvar,nzp_dom, " + p_dat_charge + ", nzp_gil, dat_ofor, 1  " +
                " From ttt_aid_ub Where 1=1 "
                , 5000, "", out ret);
            if (!ret.result) { CalcGil_CloseTmp(conn_db); return false; }

            UpdateStatistics(false, paramcalc, gilec.gilec_tab, out ret);
            if (!ret.result) { CalcGil_CloseTmp(conn_db); return false; }

            ExecSQL(conn_db, " Drop table ttt_aid_ub ", false);

            ExecByStep(conn_db, gilec.gil_xx, "nzp_gx",
              " Update " + gilec.gil_xx +
              " Set dat_s = " + MDY(1, 1, 1901) +
              " Where " + gilec.paramcalc.where_z + gilec.paramcalc.per_dat_charge +
              "   and dat_s is null "
              , 100000, "", out ret);
            if (!ret.result) { CalcGil_CloseTmp(conn_db); return false; }

            ExecByStep(conn_db, gilec.gil_xx, "nzp_gx",
              " Update " + gilec.gil_xx +
              " Set dat_po = " + sDefaultSchema + "MDY(1,1,3000) " +
              " Where " + gilec.paramcalc.where_z + gilec.paramcalc.per_dat_charge +
              "   and dat_po is null "
              , 100000, "", out ret);
            if (!ret.result) { CalcGil_CloseTmp(conn_db); return false; }

            UpdateStatistics(false, paramcalc, gilec.gilec_tab, out ret);
            if (!ret.result) { CalcGil_CloseTmp(conn_db); return false; }

            //кол-во дней бытия в месяце
            ExecByStep(conn_db, gilec.gil_xx, "nzp_gx",
              " Update " + gilec.gil_xx +
              " Set cnt1 = " +
#if PG
 " EXTRACT('days' from" +
                 " (case when dat_po > " + gilec.paramcalc.dat_po + " then " + gilec.paramcalc.dat_po + " + interval '1 day' " +
                 " else dat_po+ interval '1 day' end) - " +
                 " (case when dat_s < " + gilec.paramcalc.dat_s + " then " + gilec.paramcalc.dat_s + " else dat_s end) )  " +
#else
              gilec.paramcalc.pref + "_data:sortnum( (case when dat_po > " + gilec.paramcalc.dat_po + " then " + gilec.paramcalc.dat_po + " +1 " +
                 " else dat_po+1 end) - (case when dat_s < " + gilec.paramcalc.dat_s + " then " + gilec.paramcalc.dat_s + " else dat_s end)  ) " +
#endif
 " Where " + gilec.paramcalc.where_z + gilec.paramcalc.per_dat_charge +
              "   and dat_po >= " + gilec.paramcalc.dat_s +
              "   and dat_s  <= " + gilec.paramcalc.dat_po
              , 100000, "", out ret);
            if (!ret.result) { CalcGil_CloseTmp(conn_db); return false; }

            #endregion заполнить stek = 1 периодами проживания каждого жильца без учета врем.выбытия и значения параметра 5

            #region заполнить stek = 2 периодами врем.выбытия
            //загнать gil_periods в stek = 2
            ExecByStep(conn_db, gilec.paramcalc.data_alias + "gil_periods", "nzp_glp",
              " Insert into " + gilec.gil_xx + " ( nzp_kvar,nzp_dom,dat_charge,nzp_gil, dat_s,dat_po, stek, cnt1 ) " +
              " Select " + sUniqueWord + " k.nzp_kvar,k.nzp_dom, " + p_dat_charge + ", nzp_gilec, g.dat_s, g.dat_po, 2, " +
#if PG
 " EXTRACT('days' from  (case when g.dat_po > " + gilec.paramcalc.dat_po +
                                    " then " + gilec.paramcalc.dat_po + " +interval '1 day' " +
                                    " else g.dat_po+interval '1 day' end) - (case when g.dat_s < " + gilec.paramcalc.dat_s +
                                                                 " then " + gilec.paramcalc.dat_s + " else g.dat_s end)  ) " +
#else
              gilec.paramcalc.pref + "_data:sortnum( (case when g.dat_po > " + gilec.paramcalc.dat_po +
                                    " then " + gilec.paramcalc.dat_po + " +1 " +
                                    " else g.dat_po+1 end) - (case when g.dat_s < " + gilec.paramcalc.dat_s +
                                                                 " then " + gilec.paramcalc.dat_s + " else g.dat_s end)  ) " +
#endif
 " From " + gilec.paramcalc.data_alias + "gil_periods g, t_opn k " +
              " Where g.nzp_kvar = k.nzp_kvar " +
              "   and g.is_actual <> 100 " +
              "   and g.dat_s  <= " + gilec.paramcalc.dat_po +
              "   and g.dat_po >= " + gilec.paramcalc.dat_s +
              "   and g.dat_po + 1 - g.dat_s > 5 " //где убыл не менее 6 дней
              , 100000, "", out ret);
            if (!ret.result) { CalcGil_CloseTmp(conn_db); return false; }

            UpdateStatistics(false, paramcalc, gilec.gilec_tab, out ret);
            if (!ret.result) { CalcGil_CloseTmp(conn_db); return false; }

            //сначала объединим пересекающиеся интервалы gil_periods
            ExecSQL(conn_db, " Drop table ttt_aid_uni ", false);

            ret = ExecSQL(conn_db,
                " CREATE TEMP TABLE ttt_aid_uni (" +
                "   nzp_kvar  integer," +
                "   nzp_dom   integer," +
                "   nzp_gil   integer," +
                "   stek      integer," +
                "   dat_charge date," +
                "   dat_s      date," +
                "   dat_po     date " +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { CalcGil_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                 " Insert Into ttt_aid_uni (nzp_kvar,nzp_dom,nzp_gil,stek,dat_charge,dat_s,dat_po) " +
                 " Select a.nzp_kvar, a.nzp_dom, a.nzp_gil, a.stek, a.dat_charge, min(a.dat_s) as dat_s, max(a.dat_po) as dat_po " +
                 " From " + gilec.gil_xx + " a, " + gilec.gil_xx + " b, t_selkvar t " +
                 " Where a.nzp_kvar = b.nzp_kvar " +
                 "   and a.nzp_kvar= t.nzp_kvar" +
                 "   and a.nzp_gil  = b.nzp_gil " +
                 "   and a.stek     = b.stek " +
                 "   and a.nzp_gx <>  b.nzp_gx " +
                 "   and a.stek     = 2 " +
                 "   and a.dat_s  <= b.dat_po " +
                 "   and a.dat_po >= b.dat_s  " +
                 st1 +
                 " Group by 1,2,3,4,5 "
                 , true);
            if (!ret.result) { CalcGil_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, " Create index ix_aid_uni1 on ttt_aid_uni (nzp_kvar, nzp_gil) ", true);
            if (!ret.result) { CalcGil_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, " Create index ix_aid_uni2 on ttt_aid_uni (nzp_dom) ", true);
            if (!ret.result) { CalcGil_CloseTmp(conn_db); return false; }

            //удалим измененные строки (пока скинем в архив для отладки)
            ExecByStep(conn_db, gilec.gil_xx, "nzp_gx",
                " Update " + gilec.gil_xx +
                " Set cur_zap = -1 " +
                " Where " + gilec.paramcalc.where_z + gilec.paramcalc.per_dat_charge +
                "   and EXISTS ( Select 1 From ttt_aid_uni b " +
                            " Where " + gilec.gil_xx + ".nzp_kvar = b.nzp_kvar " +
                            "   and " + gilec.gil_xx + ".nzp_gil  = b.nzp_gil " +
                            "   and " + gilec.gil_xx + ".stek     = b.stek " +
                           " ) "
              , 100000, "", out ret);
            if (!ret.result) { CalcGil_CloseTmp(conn_db); return false; }

            //и введем измененную строку
            ret = ExecSQL(conn_db,
                " Insert into " + gilec.gil_xx + " ( nzp_kvar,nzp_dom,dat_charge,nzp_gil,stek, dat_s,dat_po, cur_zap,cnt1 ) " +
                " Select nzp_kvar,nzp_dom,dat_charge,nzp_gil,stek, dat_s,dat_po, 1 , " +
#if PG
 "EXTRACT('days' from " +
                   "(" +
                      "(case when dat_po > " + gilec.paramcalc.dat_po + " then " + gilec.paramcalc.dat_po + " else dat_po + interval '1 day' end)" +
                      " - " +
                      "(case when dat_s  < " + gilec.paramcalc.dat_s + " then " + gilec.paramcalc.dat_s + " else dat_s end) " +
                   ") " +
                " ) " +
#else
                gilec.paramcalc.pref + "_data:sortnum"+
                " ( "+
                      "(case when dat_po > " + gilec.paramcalc.dat_po + " then " + gilec.paramcalc.dat_po + " else dat_po + 1 end)"+
                      " - "+
                      "(case when dat_s  < " + gilec.paramcalc.dat_s  + " then " + gilec.paramcalc.dat_s  + " else dat_s end)" +
                " ) " +
#endif
 " From ttt_aid_uni "
                , true);
            if (!ret.result) { CalcGil_CloseTmp(conn_db); return false; }

            ExecSQL(conn_db, " Drop table ttt_aid_uni ", false);

            #endregion заполнить stek = 2 периодами врем.выбытия

            //вычислить кол-во дней перекрытия периода убытия и занести в cnt2
            CalcGilToUseGilPeriods(conn_db, st1, gilec.gil_xx, gilec.paramcalc.dat_s, gilec.paramcalc.dat_po,
                gilec.paramcalc.where_z + gilec.paramcalc.per_dat_charge, gilec.paramcalc.data_alias, out ret);
            if (!ret.result) { CalcGil_CloseTmp(conn_db); return false; }

            #region 1
            /*
            ExecSQL(conn_db, " Drop table ttt_aid_cnt ", false);

            ret = ExecSQL(conn_db,
                " CREATE TEMP TABLE ttt_aid_cnt (" +
                "   nzp_kvar  integer," +
                "   nzp_gil   integer," +
#if PG
                "   cnt_del2 interval hour " +
#else
                "   cnt_del2 INTERVAL HOUR(3) to HOUR " +
#endif
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { CalcGil_CloseTmp(conn_db); return false; }

            string sIntervalDay;
#if PG
            sIntervalDay = "interval '1 day'";
#else
            sIntervalDay = "1";
#endif

            ret = ExecSQL(conn_db,
                " Insert Into ttt_aid_cnt (nzp_kvar,nzp_gil,cnt_del2) " +
                " Select distinct a.nzp_kvar,a.nzp_gil, " +
                //     [---------------] a. gil_periods
                //  [-----------]        b. прибытие - урезаем
                        " case when " + ZamenaS(gilec, "b.dat_s") + " <= " + ZamenaS(gilec, "a.dat_s") + " and " +
                                        ZamenaPo(gilec, "b.dat_po") + " < " + ZamenaPo(gilec, "a.dat_po") +
                        " then (" + ZamenaPo(gilec, "b.dat_po") + " + " + sIntervalDay + " - " + ZamenaS(gilec, "a.dat_s") + ") " +
                        " else " +
                //  [---------------]    a.
                //        [-----------]  b.
                        " case when " + ZamenaS(gilec, "b.dat_s") + " >= " + ZamenaS(gilec, "a.dat_s") + " and " +
                                        ZamenaPo(gilec, "b.dat_po") + " > " + ZamenaPo(gilec, "a.dat_po") +
                        " then (" + ZamenaPo(gilec, "a.dat_po") + " + " + sIntervalDay + " - " + ZamenaS(gilec, "b.dat_s") + ") " +
                        " else " +
                //      [----------]     a.
                //   [---------------]   b.
                        " case when " + ZamenaS(gilec, "b.dat_s") + " <= " + ZamenaS(gilec, "a.dat_s") + " and " +
                                        ZamenaPo(gilec, "b.dat_po") + " >= " + ZamenaPo(gilec, "a.dat_po") +
                        " then (" + ZamenaPo(gilec, "a.dat_po") + " + " + sIntervalDay + " - " + ZamenaS(gilec, "a.dat_s") + ") " +
                        " else " +
                //  [---------------]    a.
                //    [----------]       b.
                             " (" + ZamenaPo(gilec, "b.dat_po") + " + " + sIntervalDay + " - " + ZamenaS(gilec, "b.dat_s") + ")" +
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
                "   and a.dat_po>= b.dat_s " +
                "   and a.dat_po >=" + gilec.paramcalc.dat_s +
                "   and b.dat_po >=" + gilec.paramcalc.dat_s                         
                , true);
            if (!ret.result) { CalcGil_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, " Create index ix_aid_cn1 on ttt_aid_cnt (nzp_kvar, nzp_gil) ", true);
            if (!ret.result) { CalcGil_CloseTmp(conn_db); return false; }

            //кол-во дней временного выбытия
            ExecByStep(conn_db, gilec.gil_xx, "nzp_gx",
                " Update " + gilec.gil_xx +
                " Set cnt2 = ( Select sum(" + 
#if PG
                             "EXTRACT('days' from cnt_del2 ) " +
#else
                             gilec.paramcalc.pref + "_data:sortnum( cnt_del2 ) " +
#endif
                             ") From ttt_aid_cnt a " +
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
            if (!ret.result) { CalcGil_CloseTmp(conn_db); return false; }

            ExecSQL(conn_db, " Drop table ttt_aid_cnt ", false);
            */

            #endregion 1

            //посчитать кол-во жильцов stek =3 & nzp_gil = 1
            CalcGilItogInStek(conn_db, gilec, st1, gilec.gil_xx, gilec.paramcalc.dat_s, gilec.paramcalc.dat_po,
                gilec.paramcalc.where_z + gilec.paramcalc.per_dat_charge, (DateTime.DaysInMonth(paramcalc.calc_yy, paramcalc.calc_mm)).ToString(), "15", "3", out ret);
            if (!ret.result) { CalcGil_CloseTmp(conn_db); return false; }

            #region 2
            /*
            ExecSQL(conn_db, " Drop table ttt_itog ", false);

            ret = ExecSQL(conn_db,
                " CREATE TEMP TABLE ttt_itog (" +
                "   nzp_kvar  integer," +
                "   nzp_dom   integer," +
                "   nzp_gil   integer," +
                "   dat_charge date," +
                "   dat_s      date," +
                "   dat_po     date," +
                "   cnt1      integer," +
                "   cnt2      integer," +
                "   cnt3      integer," +
                "   val1      " + sDecimalType + "(11,7) default 0.00, " +
                "   val5      " + sDecimalType + "(11,7) default 0.00  " +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { CalcGil_CloseTmp(conn_db); return false; }

            //string st2 = " (case when cnt1 >= 15 then cnt1 else 0 end) - (case when cnt2>=6 then cnt2 else 0 end )"; //сколько прожил дней с учетом врем. выбытия (м.б.<0)
            //string stv = " (case when cnt2>=6 then cnt2 else 0 end )"; //сколько дней врем. выбытия

            //поскольку уже при вставке в stek=2 проверяется, что временое выбытие > 5 дней, то второй раз проверять вредно!
            string st2 = " (case when cnt1 >= 15 then cnt1 else 0 end) - (case when cnt2>=0 then cnt2 else 0 end )"; //сколько прожил дней с учетом врем. выбытия (м.б.<0)
            string stv = " (case when cnt2>=0 then cnt2 else 0 end )"; //сколько дней врем. выбытия

            //Calendar myCal = CultureInfo.InvariantCulture.Calendar;
            ret = ExecSQL(conn_db,
                " Insert Into ttt_itog (nzp_kvar,nzp_dom,nzp_gil,dat_charge,dat_s,dat_po,cnt1,cnt2,cnt3,val1,val5) " +
                " Select " + sUniqueWord + " nzp_kvar, nzp_dom, nzp_gil, dat_charge, " + gilec.paramcalc.dat_s + " as dat_s, " + gilec.paramcalc.dat_po + " as dat_po, " +
                    " case when " + st2 + " >= 15 then 1 else 0 end as cnt1, " +//нормативное кол-во жильцов с учетом врем. выбывших
                    " case when cnt1 >= 15 then 1 else 0 end as cnt2, " +       //кол-во жителей без врем. убытия
                    st2 + " as cnt3, " +                                        //сколько прожил дней с учетом врем. выбытия (м.б.<0)
                    " case when " + st2 + " > 0 then (" + st2 + ")/" + DateTime.DaysInMonth(paramcalc.calc_yy, paramcalc.calc_mm) + ".00 else 0 end as val1 " + //доля бытия в месяце
                    ",case when " + stv + " > 0 then (" + stv + ")/" + DateTime.DaysInMonth(paramcalc.calc_yy, paramcalc.calc_mm) + ".00 else 0 end as val5 " + //доля врем. выбытия в месяце
                " From " + gilec.gil_xx +
                " Where " + gilec.paramcalc.where_z + gilec.paramcalc.per_dat_charge +
                "   and stek = 1 " +
                "   and cur_zap <> -1 "
               , true);
            if (!ret.result) { CalcGil_CloseTmp(conn_db); return false; }

            ExecByStep(conn_db, "ttt_itog", "nzp_kvar",
                " Insert into " + gilec.gil_xx + " ( nzp_kvar,nzp_dom,dat_charge,nzp_gil,stek, dat_s,dat_po, cnt1,cnt2,cnt3,val1,val5 ) " +
                " Select nzp_kvar,nzp_dom,dat_charge,1 nzp_gil,3 stek, dat_s,dat_po, sum(cnt1),sum(cnt2),sum(cnt3),sum(val1),sum(val5) " +
                " From ttt_itog where 1=1 "
                , 100000, " Group by 1,2,3,4,5,6,7 ", out ret);
            if (!ret.result) { CalcGil_CloseTmp(conn_db); return false; }

            ExecSQL(conn_db, " Drop table ttt_itog ", false);
            */
            #endregion 2

            UpdateStatistics(false, paramcalc, gilec.gilec_tab, out ret);

            // Выборка параметров из prm_1 для учета кол-ва жильцов цифрой
            SelParamsForCalcGil(conn_db, gilec, out ret);
            if (!ret.result) { CalcGil_CloseTmp(conn_db); return false; }

            #region 3
            /*
            ExecSQL(conn_db, " Drop table ttt_aid_prm ", false);

            ret = ExecSQL(conn_db,
                " CREATE TEMP TABLE ttt_aid_prm (" +
                "   nzp_kvar  integer, " +
                "   nzp_prm   integer, " +
                "   kod       integer, " +
                "   val_prm   " + sDecimalType + "(11,7) default 0.00  " +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { CalcGil_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " Insert Into ttt_aid_prm (nzp_kvar,nzp_prm,kod,val_prm) " +
                " Select p.nzp as nzp_kvar, p.nzp_prm, 0 as kod," +
                " max(" + sNvlWord + "(p.val_prm" + sConvToNum + ",0)) as val_prm " +
                " From " + gilec.paramcalc.data_alias + "prm_1 p,t_opn a " +
                " Where a.nzp_kvar = p.nzp " +
                "   and p.nzp_prm in (5,131,10) " +
                "   and p.is_actual <> 100 " +
                "   and p.dat_s  <= " + gilec.paramcalc.dat_po +
                "   and p.dat_po >= " + gilec.paramcalc.dat_s +
                " Group by 1,2,3 " 
                , true);
            if (!ret.result) { CalcGil_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, " Create index ix_aid_prm1 on ttt_aid_prm (nzp_kvar, nzp_prm) ", true);
            if (!ret.result) { CalcGil_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, " Create index ix_aid_prm2 on ttt_aid_prm (nzp_kvar, kod) ", true);
            if (!ret.result) { CalcGil_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, sUpdStat + " ttt_aid_prm ", true);
            if (!ret.result) { CalcGil_CloseTmp(conn_db); return false; }
            */
            #endregion 3

            #region загнать параметры жильцов в stek = 3 (nzp_prm: 5,131,1395 / val2,val3,val6)

            ret = ExecSQL(conn_db,
                " Update ttt_aid_prm " +
                " Set kod = 1 " +
                " Where exists ( Select 1 From " + gilec.gil_xx + " a " +
                            " Where ttt_aid_prm.nzp_kvar = a.nzp_kvar and a.stek = 3 " +
                " ) "
               , true);
            if (!ret.result) { CalcGil_CloseTmp(conn_db); return false; }

            ExecByStep(conn_db, gilec.gil_xx, "nzp_gx",
                " Update " + gilec.gil_xx +
                " Set" +
#if PG
 " val2 = a.val5," +
                " val3 = a.val131, " +
                " val6 = a.val1395 " +
                " From ttt_aid_prm a " +
                " Where " + gilec.gil_xx + "." + gilec.paramcalc.where_z +
                "   and " + gilec.gil_xx + ".stek = 3 " +
                "   and " + gilec.gil_xx + ".nzp_kvar = a.nzp_kvar and a.kod = 1 "
#else
                " (val2,val3,val6) = (( " +
                " Select a.val5,a.val131,a.val1395 From ttt_aid_prm a Where " + gilec.gil_xx + ".nzp_kvar = a.nzp_kvar and a.kod = 1 " +
                " )) " +
                " Where " + gilec.paramcalc.where_z + gilec.paramcalc.per_dat_charge +
                "   and stek = 3 " +
                "   and exists ( Select 1 From ttt_aid_prm a " +
                            " Where " + gilec.gil_xx + ".nzp_kvar = a.nzp_kvar and a.kod = 1 " +
                          " ) "
#endif
, 100000, "", out ret);
            if (!ret.result) { CalcGil_CloseTmp(conn_db); return false; }


            ExecByStep(conn_db, gilec.gil_xx, "nzp_gx",
                " Update " + gilec.gil_xx +
                " Set" +
#if PG
 " val5 = a.val10 " +
                            " From ttt_aid_prm a Where " + gilec.gil_xx + ".nzp_kvar = a.nzp_kvar and a.kod = 1 " +
#else
                " val5 = ( " +
                            " Select a.val10 " +
                            " From ttt_aid_prm a Where " + gilec.gil_xx + ".nzp_kvar = a.nzp_kvar and a.kod = 1 " +
                                  " ) " +
                " Where exists ( Select 1 " +
                            " From ttt_aid_prm a Where " + gilec.gil_xx + ".nzp_kvar = a.nzp_kvar and a.kod = 1 " +
                          " ) " +
#endif
 "   and " + gilec.gil_xx + "." + gilec.paramcalc.where_z + gilec.paramcalc.per_dat_charge +
                "   and " + gilec.gil_xx + ".stek = 3 " +
                "   and not exists ( Select 1 From ttt_prm_1 a " +
                            " Where " + gilec.gil_xx + ".nzp_kvar = a.nzp and a.nzp_prm=130 and a.val_prm='1'" +
                          " ) "
                , 100000, "", out ret);
            if (!ret.result) { CalcGil_CloseTmp(conn_db); return false; }

            //вставить строки с kod = 0 (нет данных паспортистки, но есть жильцовые параметры)
            ret = ExecSQL(conn_db,
                " Insert into " + gilec.gil_xx + " ( nzp_kvar,nzp_dom,dat_charge,nzp_gil,stek, dat_s,dat_po, cnt1,cnt2,cnt3,val1,val2,val3,val5,val6 ) " +
                " Select a.nzp_kvar,a.nzp_dom," + p_dat_charge + ",0 nzp_gil,3 stek, " + gilec.paramcalc.dat_s + "," + gilec.paramcalc.dat_po + ", 0,0,0,0, " +
                       " max(b.val5)," +
                       " max(b.val131)," +
                       " (case when max(b.val5) > max(b.val10) then max(b.val10) else max(b.val5) end) as val10, " +
                       " max(b.val1395) " +
                " From t_opn a, ttt_aid_prm b " +
                " Where a.nzp_kvar = b.nzp_kvar " +
                "   and b.kod = 0 and a.is_day_calc=0 " +
                " Group by 1,2 "
               , true);
            if (!ret.result) { CalcGil_CloseTmp(conn_db); return false; }

            UpdateStatistics(false, paramcalc, gilec.gilec_tab, out ret);

            #endregion загнать параметры жильцов в stek = 3 (nzp_prm: 5,131 val2,val3)

            #region посчитать кол-во жильцов для по-дневного расчета - stek = 4 & nzp_gil = 1
            //
            ExecSQL(conn_db, " Drop table ttt_gils_pd ", false);

            ret = ExecSQL(conn_db,
                " CREATE TEMP TABLE ttt_gils_pd (" +
                "   nzp_gx    serial, " +
                "   nzp_kvar  integer," +
                "   nzp_dom   integer," +
                "   nzp_gil   integer," +
                "   stek      integer," +
                "   cur_zap   integer default 0," +
                "   dat_charge date," +
                "   dat_s      date," +
                "   dat_po     date," +
                "   nzp_period integer," +
                "   dp         date," +
                "   dp_end     date," +
                "   cntd      integer," +
                "   cntd_mn   integer," +
                "   cnt1      integer," +
                "   cnt2      integer," +
                "   cnt3      integer," +
                "   val1      " + sDecimalType + "(11,7) default 0.00, " +
                "   val2      " + sDecimalType + "(11,7) default 0.00, " +
                "   val3      " + sDecimalType + "(11,7) default 0.00, " +
                "   val5      " + sDecimalType + "(11,7) default 0.00,  " +
                "   val6      " + sDecimalType + "(11,7) default 0.00  " +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { CalcGil_CloseTmp(conn_db); return false; }

            //поскольку уже при вставке в stek=2 проверяется, что временое выбытие > 5 дней, то второй раз проверять вредно!
            // st2 = " (case when cnt1 >= 15 then cnt1 else 0 end) - (case when cnt2>=0 then cnt2 else 0 end )"; //сколько прожил дней с учетом врем. выбытия (м.б.<0)
            // stv = " (case when cnt2>=0 then cnt2 else 0 end )"; //сколько дней врем. выбытия

            ret = ExecSQL(conn_db,
                " Insert Into ttt_gils_pd (nzp_kvar,nzp_dom,nzp_gil,dat_charge,dat_s,dat_po,nzp_period,dp,dp_end,cntd,cntd_mn,cnt1,cnt2,cnt3,val1,val2,val3,val5,val6,stek) " +
                " Select " + sUniqueWord + " g.nzp_kvar, g.nzp_dom, g.nzp_gil, g.dat_charge, g.dat_s, g.dat_po, p.nzp_period, p.dp, p.dp_end, p.cntd, p.cntd_mn, " +
                    " 0 as cnt1, " + // кол-во жильцов с учетом врем. выбывших
                    " 0 as cnt2, " + // кол-во жителей без врем. убытия
                    " 0 as cnt3, " + // сколько прожил дней с учетом врем. выбытия (м.б.<0)
                    " 0 as val1, " + // доля бытия в месяце
                    " 0 as val2, " + // кол-во жильцов по параметру 5
                    " 0 as val3, " + // кол-во верм.выбывших по параметру 10
                    " 0 as val5, " + // доля врем. выбытия в месяце
                    " 0 as val6, " +
                    " 1 stek " +
                " From " + gilec.gil_xx + " g, t_opn k,t_gku_periods p " +
                " Where g.nzp_kvar=k.nzp_kvar and k.nzp_kvar=p.nzp_kvar and k." + gilec.paramcalc.where_z + gilec.paramcalc.per_dat_charge +
                "   and g.stek = 1 and k.is_day_calc = 1 " +
                "   and g.cur_zap <> -1 "
               , true);
            if (!ret.result) { CalcGil_CloseTmp(conn_db); return false; }

            ViewTbl(conn_db, "select * from ttt_gils_pd order by nzp_kvar,stek,nzp_gil,dp,dat_s ");

            //кол-во дней бытия в месяце
            ret = ExecSQL(conn_db,
              " Update ttt_gils_pd" +
              " Set cnt1 = " +
#if PG
 " EXTRACT('days' from" +
                 " (case when dat_po > dp_end then dp_end + interval '1 day' " +
                 " else dat_po+ interval '1 day' end) - " +
                 " (case when dat_s < dp then dp else dat_s end) )  " +
#else
                 gilec.paramcalc.pref + "_data:sortnum( (case when dat_po > dp_end then dp_end +1 " +
                 " else dat_po+1 end) - (case when dat_s < dp then dp_end else dat_s end)  ) " +
#endif
 " Where " + gilec.paramcalc.where_z + gilec.paramcalc.per_dat_charge +
              "   and dat_po >= " + gilec.paramcalc.dat_s +
              "   and dat_s  <= " + gilec.paramcalc.dat_po
               , true);
            if (!ret.result) { CalcGil_CloseTmp(conn_db); return false; }

            ViewTbl(conn_db, "select * from ttt_gils_pd order by nzp_kvar,stek,nzp_gil,dp,dat_s ");

            CalcGilToUseGilPeriods_Pd(conn_db, gilec, st1, "1=1", out ret);
            if (!ret.result) { CalcGil_CloseTmp(conn_db); return false; }

            String sCntDays;
#if PG
            sCntDays =
                "EXTRACT('days' from(dp_end + interval '1 day' - dp))";
#else
            sCntDays =
                "dp_end + 1 - dp";
#endif

            ViewTbl(conn_db, "select * from ttt_gils_pd order by nzp_kvar,stek,nzp_gil,dp,dat_s ");

            //посчитать кол-во жильцов stek =4 & nzp_gil = 1
            CalcGilItogInStek(conn_db, gilec, st1, "ttt_gils_pd", "dp", "dp_end",
                gilec.paramcalc.where_z + gilec.paramcalc.per_dat_charge, sCntDays, "1", "4", out ret);

            if (!ret.result) { CalcGil_CloseTmp(conn_db); return false; }
            //
            #endregion посчитать кол-во жильцов для по-дневного расчета - stek = 4 & nzp_gil = 1

            #region загнать параметры жильцов в stek = 4 (nzp_prm: 5,131,1395 / val2,val3,val6)

            ViewTbl(conn_db, "select * from ttt_gils_pd ");

            ret = ExecSQL(conn_db,
                " Update ttt_aid_prm_pd " +
                " Set kod = 1 " +
                " Where exists ( Select 1 From " + gilec.gil_xx + " a " +
                            " Where ttt_aid_prm_pd.nzp_kvar = a.nzp_kvar and a.stek = 4 " +
                " ) "
               , true);
            if (!ret.result) { CalcGil_CloseTmp(conn_db); return false; }

            ViewTbl(conn_db, "select * from ttt_aid_prm_pd order by nzp_kvar,dp ");

            ExecByStep(conn_db, gilec.gil_xx, "nzp_gx",
                " Update " + gilec.gil_xx +
                " Set (val2,val3,val6) = (" +
                            "(Select max(a.val5) " +
                            " From ttt_aid_prm_pd a " +
                            " Where " + gilec.gil_xx + ".nzp_kvar = a.nzp_kvar and a.kod = 1" +
                            "   and " + gilec.gil_xx + ".dat_s <= a.dp_end and " + gilec.gil_xx + ".dat_po >= a.dp)," +
                            "(Select max(a.val131) " +
                            " From ttt_aid_prm_pd a " +
                            " Where " + gilec.gil_xx + ".nzp_kvar = a.nzp_kvar and a.kod = 1" +
                            "   and " + gilec.gil_xx + ".dat_s <= a.dp_end and " + gilec.gil_xx + ".dat_po >= a.dp), " +
                            "(Select max(a.val1395) " +
                            " From ttt_aid_prm_pd a " +
                            " Where " + gilec.gil_xx + ".nzp_kvar = a.nzp_kvar and a.kod = 1" +
                            "   and " + gilec.gil_xx + ".dat_s <= a.dp_end and " + gilec.gil_xx + ".dat_po >= a.dp) " +
                            " ) " +
                " Where exists ( Select 1 From ttt_aid_prm_pd a " +
                            " Where " + gilec.gil_xx + ".nzp_kvar = a.nzp_kvar and a.kod = 1" +
                            "   and " + gilec.gil_xx + ".dat_s <= a.dp_end and " + gilec.gil_xx + ".dat_po >= a.dp " +
                          " ) " +
                "   and " + gilec.gil_xx + "." + gilec.paramcalc.where_z + gilec.paramcalc.per_dat_charge +
                "   and " + gilec.gil_xx + ".stek = 4 "
                , 100000, "", out ret);
            if (!ret.result) { CalcGil_CloseTmp(conn_db); return false; }

            ExecByStep(conn_db, gilec.gil_xx, "nzp_gx",
                " Update " + gilec.gil_xx +
                " Set val5 = " +
                            "(Select (case when max(a.val5) > max(a.val10) then max(a.val10) else max(a.val5) end) as val10 " +
                            " From ttt_aid_prm_pd a " +
                            " Where " + gilec.gil_xx + ".nzp_kvar = a.nzp_kvar and a.kod = 1 " +
                            "   and " + gilec.gil_xx + ".dat_s <= a.dp_end and " + gilec.gil_xx + ".dat_po >= a.dp) " +
                " Where exists ( Select 1 From ttt_aid_prm_pd a " +
                            " Where " + gilec.gil_xx + ".nzp_kvar = a.nzp_kvar and a.kod = 1 " +
                            "   and " + gilec.gil_xx + ".dat_s <= a.dp_end and " + gilec.gil_xx + ".dat_po >= a.dp " +
                          " ) " +
                "   and " + gilec.gil_xx + "." + gilec.paramcalc.where_z + gilec.paramcalc.per_dat_charge +
                "   and " + gilec.gil_xx + ".stek = 4 " +
                "   and not exists ( Select 1 From ttt_prm_1 a " +
                            " Where " + gilec.gil_xx + ".nzp_kvar = a.nzp and a.nzp_prm=130 and a.val_prm='1'" +
                          " ) "
              , 100000, "", out ret);
            if (!ret.result) { CalcGil_CloseTmp(conn_db); return false; }

            //вставить строки с kod = 0 (нет данных паспортистки, но есть жильцовые параметры)

            //посчитать кол-во жильцов stek =3 & nzp_gil = 1, кот. не было в ПАССПОРТИСТКЕ
            ExecSQL(conn_db, " Drop table ttt_itog ", false);

            ret = ExecSQL(conn_db,
                " CREATE TEMP TABLE ttt_itog (" +
                "   nzp_kvar  integer," +
                "   nzp_dom   integer," +
                "   stek      integer," +
                "   dat_s     date," +
                "   dat_po    date," +
                "   val2      " + sDecimalType + "(11,7) default 0.00, " +
                "   val3      " + sDecimalType + "(11,7) default 0.00, " +
                "   val5      " + sDecimalType + "(11,7) default 0.00,  " +
                "   val6      " + sDecimalType + "(11,7) default 0.00,  " +
                "   val2d     " + sDecimalType + "(11,7) default 0.00, " +
                "   val3d     " + sDecimalType + "(11,7) default 0.00, " +
                "   val5d     " + sDecimalType + "(11,7) default 0.00,  " +
                "   val6d     " + sDecimalType + "(11,7) default 0.00,  " +
                "   cntd_mn   integer " +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { CalcGil_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " Insert Into ttt_itog (nzp_kvar,nzp_dom,stek,dat_s,dat_po,val2,val3,val5,val6,cntd_mn) " +
                " Select a.nzp_kvar,a.nzp_dom,4 stek, b.dp,b.dp_end, " +
                       " max(b.val5)," +
                       " max(b.val131)," +
                       " (case when max(b.val5) > max(b.val10) then max(b.val10) else max(b.val5) end) as val10," +
                       " max(b.val1395)," +
                       DateTime.DaysInMonth(paramcalc.calc_yy, paramcalc.calc_mm) +
                " From t_opn a, ttt_aid_prm_pd b " +
                " Where a.nzp_kvar = b.nzp_kvar and a.is_day_calc = 1 " +
                "   and exists (select 1 from ttt_aid_prm p where a.nzp_kvar = p.nzp_kvar and p.kod = 0)" +
                " Group by 1,2,3,4,5 "
                , true);
            if (!ret.result) { CalcGil_CloseTmp(conn_db); return false; }

            string sDlt =
#if PG
 "EXTRACT('days' from " +
                "(" +
                "(case when dat_po > " + gilec.paramcalc.dat_po + " then " + gilec.paramcalc.dat_po +
                " else dat_po + interval '1 day' end)" +
                " - " +
                "(case when dat_s  < " + gilec.paramcalc.dat_s + " then " + gilec.paramcalc.dat_s + " else dat_s end) " +
                ") " +
                " ) * 1.00 / cntd_mn ";
#else
                gilec.paramcalc.pref + "_data:sortnum"+
                " ( "+
                      "(case when dat_po > " + gilec.paramcalc.dat_po + " then " + gilec.paramcalc.dat_po + " else dat_po + 1 end)"+
                      " - "+
                      "(case when dat_s  < " + gilec.paramcalc.dat_s  + " then " + gilec.paramcalc.dat_s  + " else dat_s end)" +
                " ) * 1.00 / cntd_mn ";
#endif

            ret = ExecSQL(conn_db,
                " Update ttt_itog " +
                " Set val2d = val2 * " + sDlt + " , val3d = val3 * " + sDlt + ", val5d = val5 * " + sDlt + ", val6d = val6 * " + sDlt +
                " Where 1=1 "
               , true);
            if (!ret.result) { CalcGil_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " Insert into " + gilec.gil_xx + " ( nzp_kvar,nzp_dom,dat_charge,nzp_gil,stek, dat_s,dat_po, cnt1,cnt2,cnt3,val1,val2,val3,val5,val6 ) " +
                " Select nzp_kvar,nzp_dom," + p_dat_charge + ",1 nzp_gil,4 stek, dat_s,dat_po, 0,0,0,0, val2, val3, val5, val6 " +
                " From ttt_itog "
               , true);
            if (!ret.result) { CalcGil_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " Insert into " + gilec.gil_xx + " ( nzp_kvar,nzp_dom,dat_charge,nzp_gil,stek, dat_s,dat_po, cnt1,cnt2,cnt3,val1,val2,val3,val5,val6 ) " +
                " Select nzp_kvar,nzp_dom," + p_dat_charge + ",1 nzp_gil,3 stek, " + gilec.paramcalc.dat_s + "," + gilec.paramcalc.dat_po + ", 0,0,0,0," +
                " sum(val2d), sum(val3d), sum(val5d), sum(val6d) " +
                " From ttt_itog " +
                " Group by 1,2 "
                , true);
            if (!ret.result) { CalcGil_CloseTmp(conn_db); return false; }

            UpdateStatistics(false, paramcalc, gilec.gilec_tab, out ret);

            ExecSQL(conn_db, " Drop table ttt_itog ", false);

            #endregion загнать параметры жильцов в stek = 4 (nzp_prm: 5,131 val2,val3)


            CalcGil_CloseTmp(conn_db);
            //
            #region учесть кол-во жильцов - stek = 3 & 4

            IDataReader reader;
            //
            //считать по аис паспортистка kod_info = 1
            ret = ExecRead(conn_db, out reader,
                " Select count(*) cnt From " + gilec.paramcalc.data_alias + "prm_10 p Where p.nzp_prm = 89 " +
                " and p.is_actual <> 100 and p.dat_s  <= " + gilec.paramcalc.dat_po + " and p.dat_po >= " + gilec.paramcalc.dat_s + " "
                , true);
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
                    " Where " + gilec.paramcalc.where_z + gilec.paramcalc.per_dat_charge + " and stek in (3,4) "
                  , 100000, "", out ret);
                if (!ret.result)
                {
                    ret.text = "Ошибка поиска признака -Разрешить подневной расчет - подключена АИС Паспортистка ЖЭУ- ";
                    return false;
                }
            }
            else
            {
                ret = ExecRead(conn_db, out reader,
                " Select count(*) cnt From " + gilec.paramcalc.data_alias + "prm_10 p Where p.nzp_prm = 129 " +
                " and p.is_actual <> 100 and p.dat_s  <= " + gilec.paramcalc.dat_po + " and p.dat_po >= " + gilec.paramcalc.dat_s + " "
                , true);
                if (!ret.result)
                {
                    ret.text = "Ошибка поиска признака -Учет льгот без жильцов- ";
                    return false;
                }
                if (reader.Read())
                {
                    bIsGood = (Convert.ToInt32(reader["cnt"]) == 0);
                }
                reader.Close();
                if (bIsGood)
                {
                    // если НЕ установлен параметр в prm_10 - nzp_prm=129 'Учет льгот без жильцов' и есть льготы
                    ExecByStep(conn_db, gilec.gil_xx, "nzp_gx",
                        " Update " + gilec.gil_xx + " Set kod_info = 1 " +
                        " Where " + gilec.paramcalc.where_z + gilec.paramcalc.per_dat_charge + " and stek in (3,4) " +
                        "   and 0 < ( Select count(*) From " + gilec.paramcalc.data_alias + "lgots l " +
                        " Where " + gilec.gil_xx + ".nzp_kvar = l.nzp_kvar and l.is_actual <> 100" +
                        " ) "
                        , 100000, "", out ret);
                    if (!ret.result)
                    {
                        ret.text = "Ошибка расчета";
                        return false;
                    }
                }

                // если установлен параметр в prm_1 - nzp_prm= 90 'Разрешить подневной расчет для лицевого счета'
                // если установлен параметр в prm_1 - nzp_prm=130 'Считать количество жильцов по АИС Паспортистка ЖЭУ'
                ExecByStep(conn_db, gilec.gil_xx, "nzp_gx",
                    " Update " + gilec.gil_xx + " Set kod_info = 1 " +
                    " Where " + gilec.paramcalc.where_z + gilec.paramcalc.per_dat_charge + " and stek in (3,4) " +
                    "   and EXISTS ( Select 1 From " + gilec.paramcalc.data_alias + "prm_1 p " +
                    "   Where " + gilec.gil_xx + ".nzp_kvar = p.nzp and p.nzp_prm in (130)" + //(90,130) - for RT!
                    "   and p.is_actual <> 100 and p.dat_s  <= " + gilec.paramcalc.dat_po + " and p.dat_po >= " + gilec.paramcalc.dat_s +
                    "   ) "
                    , 100000, "", out ret);
                if (!ret.result)
                {
                    ret.text = "Ошибка расчета";
                    return false;
                }
                //
            }

            // val4 - кол-во жильцов по данным АИС Пасспортистка - до этого - val1 ! Сохраним !
            ExecByStep(conn_db, gilec.gil_xx, "nzp_gx",
                " Update " + gilec.gil_xx + " Set val4 = val1 " +
                " Where " + gilec.paramcalc.where_z + gilec.paramcalc.per_dat_charge + " and stek in (3,4) "
              , 100000, "", out ret);
            if (!ret.result)
            {
                ret.text = "Ошибка расчета";
                return false;
            }

            // val1 - итоговое кол-во жильцов по данным АИС Пасспортистка или параметру nzp_prm=5 (val2) с учетом врем. выбытия (val5)
            ExecByStep(conn_db, gilec.gil_xx, "nzp_gx",
                " Update " + gilec.gil_xx + " Set val1 = (case when val2<val5 then 0 else val2 - val5 end), cnt2 = round(val2) " +
                " Where " + gilec.paramcalc.where_z + gilec.paramcalc.per_dat_charge + " and stek in (3,4) and kod_info <> 1 "
              , 100000, "", out ret);
            if (!ret.result)
            {
                ret.text = "Ошибка расчета";
                return false;
            }

            // cnt1 - итоговое целое кол-во жильцов по данным АИС Пасспортистка или параметру nzp_prm=5
            ExecByStep(conn_db, gilec.gil_xx, "nzp_gx",
                " Update " + gilec.gil_xx + " Set cnt1 = round(val1) " +
                " Where " + gilec.paramcalc.where_z + gilec.paramcalc.per_dat_charge + " and stek in (3,4) "
              , 100000, "", out ret);
            if (!ret.result)
            {
                ret.text = "Ошибка расчета";
                return false;
            }

            #endregion учесть кол-во жильцов - stek = 3 & 4

            #region  закоментировано - одновременное проживание людей в разных квартирах
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
            #endregion  закоментировано - одновременное проживание людей в разных квартирах

            return true;
        }

        string ZamenaS(GilecXX gilec, string dat)
        {
            return " (case when " + dat + " < " + gilec.paramcalc.dat_s + " then " + gilec.paramcalc.dat_s + " else " + dat + " end )";
        }
        string ZamenaPo(GilecXX gilec, string dat)
        {
            return " (case when " + dat + " > " + gilec.paramcalc.dat_po + " then " + gilec.paramcalc.dat_po + " else " + dat + " end )";
            //dat + "+1 end )";
        }

        string Zamena_S(string dat, string pDatPeriodS)
        {
            return " (case when " + dat + " < " + pDatPeriodS + " then " + pDatPeriodS + " else " + dat + " end )";
        }
        string Zamena_Po(string dat, string pDatPeriodPo)
        {
            return " (case when " + dat + " > " + pDatPeriodPo + " then " + pDatPeriodPo + " else " + dat + " end )";
        }


    }
}


