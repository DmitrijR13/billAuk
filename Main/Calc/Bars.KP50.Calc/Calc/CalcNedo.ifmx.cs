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
    public partial class DbCalcCharge : DataBaseHead
    {
        struct NedoXX
        {
            public CalcTypes.ParamCalc paramcalc;
            public string where_z;
            public string dat_s
            {
                get
                {
                    return paramcalc.dat_s;
                }
            }
            public string dat_po
            {
                get
                {
                    string s = "";
                    if (paramcalc.calc_mm == 12)
                        s = sDefaultSchema + " MDY(1,1," + (paramcalc.calc_yy + 1) + ") ";
                    else
                        s = sDefaultSchema + " MDY(" + (paramcalc.calc_mm + 1) + ",1," + (paramcalc.calc_yy) + ") ";
                    return s;
                }
            }

            public string nedo_xx;
            public string nedo_tab;
            public bool otop5;
            /// <summary> временная таблица - локальный выборка из nedo_xx </summary>
            public string TempTableNedops;
            public NedoXX(CalcTypes.ParamCalc _paramcalc)
            {
                paramcalc = _paramcalc;
                otop5 = false;
                paramcalc.b_dom_in = true;

                nedo_tab = "nedo" + paramcalc.alias + "_" + paramcalc.calc_mm.ToString("00");
                nedo_xx = paramcalc.pref + "_charge_" + (paramcalc.calc_yy - 2000).ToString("00") + tableDelimiter + nedo_tab;

                where_z = "nzp_kvar in ( Select nzp_kvar From t_opn ) ";

                //paramcalc.dat_s = " MDY(" + paramcalc.calc_mm + ",1," + paramcalc.calc_yy + ") ";
                //dat_po = " MDY(" + paramcalc.calc_mm + "," + DateTime.DaysInMonth(paramcalc.calc_yy, paramcalc.calc_mm) + "," + paramcalc.calc_yy + ") ";
                //первй день след. месяца
                TempTableNedops = "temp_nedops_" + DateTime.Now.Ticks;
            }
        }
        //-----------------------------------------------------------------------------
        //Расчет nedo_xx
        //-----------------------------------------------------------------------------
        void CreateNedoXX(IDbConnection conn_db2, NedoXX nedoXX, out Returns ret)
        //-----------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();


            if (TempTableInWebCashe(conn_db2, nedoXX.nedo_xx))
            {
                string p_where = " Where EXISTS ( Select 1 From t_selkvar t WHERE t.nzp_kvar=" + nedoXX.nedo_xx + ".nzp_kvar) " +
                                   nedoXX.paramcalc.per_dat_charge;

                if (nedoXX.paramcalc.b_reval) //перерасчет всех услуг, конкретные услуги отсечем на этапе вычисления дельты
                    p_where = " Where EXISTS ( Select 1 From t_mustcalc b " +
                                          " Where " + nedoXX.nedo_xx + ".nzp_kvar = b.nzp_kvar ) " +
                                            nedoXX.paramcalc.per_dat_charge;

                ret = ExecSQL(conn_db2, " Delete From " + nedoXX.nedo_xx + p_where, true);
                if (!ret.result)
                {
                    return;
                }

                UpdateStatistics(false, nedoXX.paramcalc, nedoXX.nedo_tab, out ret);
                return;
            }

            string conn_kernel = Points.GetConnByPref(nedoXX.paramcalc.pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            //IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                return;
            }
            ret = CreateTableNedoXX(nedoXX, nedoXX.nedo_tab, conn_db);
            if (!ret.result) return;

            conn_db.Close();
        }


        private Returns CreateTableNedoXX(NedoXX nedoXX, string tableNedop, IDbConnection conn_db, bool Temp = false)
        {
            Returns ret;

            var tableNedopFull = nedoXX.paramcalc.pref + "_charge_" + (nedoXX.paramcalc.calc_yy - 2000).ToString("00") +
                            tableDelimiter + tableNedop;


            ret = ExecSQL(conn_db,
                " Create " + (Temp ? " TEMP " : "") + " table " + (Temp ? tableNedop : tableNedopFull) +
                " ( nzp_ndx     serial        not null, " +
                "   nzp_dom     integer       not null, " +
                "   nzp_kvar    integer       default 0 not null, " +
                "   dat_charge  date, " +
                "   cur_zap     integer       default 0 not null, " +
                //0-текущее значение, >0 - ссылка на следующее значение (nzp_cntx)
                "   prev_zap    integer       default 0 not null, " +
                //0-текущее значение, >0 - ссылка на предыдыущее значение (nzp_cntx)
                "   nzp_serv    integer       not null, " +
                "   nzp_kind    integer, " +
                "   koef        " + sDecimalType + "(10,8) default 0.00, " + //скидка за час
                "   tn          real, " +
                "   tn_min      " + sDecimalType + "(5,2), " +
                "   tn_max      " + sDecimalType + "(5,2), " +
#if PG
 "   dat_s       timestamp, " +
                "   dat_po      timestamp, " +
                "   cnts        interval     hour, " +
                "   cnts_del    interval     hour, " +
#else
                "   dat_s  DATETIME YEAR to HOUR, " +
                "   dat_po DATETIME YEAR to HOUR, " +
                "   cnts     INTERVAL HOUR(3) to HOUR, " +
                "   cnts_del INTERVAL HOUR(3) to HOUR, " +
#endif
 "   perc        " + sDecimalType + "(5,2)  default 0.00, " +
                "   kod_info    integer       default 0 ) "
                , true);
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }
            ret = ExecSQL(conn_db,
                " create unique index " + tbluser + "ix1_" + tableNedop + " on " + tableNedop + " (nzp_ndx) ", true);
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }
            ret = ExecSQL(conn_db,
                " create index " + tbluser + "ix2_" + tableNedop + " on " + tableNedop + " (nzp_dom,dat_charge) ",
                true);
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }
            ret = ExecSQL(conn_db,
                " create index " + tbluser + "ix3_" + tableNedop + " on " + tableNedop +
                " (nzp_kvar,dat_charge,kod_info) ", true);
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }
            ret = ExecSQL(conn_db,
                " create index " + tbluser + "ix4_" + tableNedop + " on " + tableNedop + " (cur_zap) ", true);
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }
            ret = ExecSQL(conn_db,
                " create index " + tbluser + "ix5_" + tableNedop + " on " + tableNedop + " (prev_zap) ", true);
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }
            return ret;
        }

        //--------------------------------------------------------------------------------
        void DropTempTablesNedo(IDbConnection conn_db)
        //--------------------------------------------------------------------------------
        {
            ExecSQL(conn_db, " Drop table ttt_aid_kh ", false);
            ExecSQL(conn_db, " Drop table ttt_aid_kh1 ", false);
            ExecSQL(conn_db, " Drop table ttt_aid_kh2 ", false);
            ExecSQL(conn_db, " Drop table ttt_aid_tn ", false);
            ExecSQL(conn_db, " Drop table ttt_aid_int ", false);
            ExecSQL(conn_db, " Drop table ttt_aid_del ", false);
            ExecSQL(conn_db, " Drop table ttt_work_zap ", false);
            ExecSQL(conn_db, " Drop table ttt_aid_pro ", false);
        }

        //недопоставки, учитывающие допустимые перерывы
        //-----------------------------------------------------------------------------
        bool KolHour(IDbConnection conn_db, NedoXX nedoXX, out Returns ret)
        //-----------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            //подсчет общего кол-ва часов недопоставки
            ExecSQL(conn_db, " Drop table ttt_aid_kh ", false);

            string ps;
            if (!nedoXX.otop5)
                ps = " and nzp_kind not in (5,9, 6,71) ";
            else
                ps = " and nzp_kind = 5 "; //отопление

            //выбрать интервалы для обработки
            string ssql;
            ssql =
                " Select *, 0 as work_zap" +
#if PG
 " Into temp ttt_aid_kh " +
#else
                " " +
#endif
 " From " + nedoXX.TempTableNedops + " a " +
                " Where " + nedoXX.where_z + nedoXX.paramcalc.per_dat_charge +
                ps +
                "   and 0 < ( Select count(*) From " + nedoXX.paramcalc.data_alias + "upg_s_nedop_type Where " + sNvlWord + "(kolhour,0) > 0  ) " +
                "   and cur_zap <> -1 " +
                "   and kod_info  = 1 " +
#if PG
 " ";
#else
                " Into temp ttt_aid_kh With no log ";
#endif
            ret = ExecSQL(conn_db, ssql, true);
            if (!ret.result)
            {
                DropTempTablesNedo(conn_db);
                return false;
            }

            ExecSQL(conn_db, " Create unique index ix_aid_kh_0 on ttt_aid_kh (nzp_ndx) ", true);
            ExecSQL(conn_db, " Create        index ix_aid_kh_1 on ttt_aid_kh (nzp_kvar, nzp_serv, nzp_kind, work_zap) ", true);
            ExecSQL(conn_db, " Create        index ix_aid_kh_2 on ttt_aid_kh (work_zap) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_aid_kh ", true);

            ExecSQL(conn_db, " Drop table ttt_aid_kh1 ", false);
            //итоговое кол-во часов отключения
            ssql =
                " Create temp table ttt_aid_kh1 " +
                //" Create table ttt_aid_kh1 " +
                " (  nzp_kvar    integer       default 0 not null, " +
                "    nzp_serv    integer       not null, " +
                "    nzp_kind    integer, " +
                "    tn          real          default 0, " +
                //нормативы отключения
                "    kh_itog     integer       default 0, " + //суммарная продолжительность отключения
                "    kh_edin     integer       default 0  " + //единовременная продолжительность отключения

#if PG
                " ) ";
#else
                    //" ) "
                " ) With no log ";
#endif
            ret = ExecSQL(conn_db, ssql, true);
            if (!ret.result)
            {
                DropTempTablesNedo(conn_db);
                return false;
            }

            ret = ExecSQL(conn_db,
                " Insert into ttt_aid_kh1 (nzp_kvar, nzp_serv, nzp_kind, tn) " +
                " Select " + sUniqueWord + " nzp_kvar, nzp_serv, nzp_kind,  tn " + // всего
                " From ttt_aid_kh "
                , true);
            if (!ret.result)
            {
                DropTempTablesNedo(conn_db);
                return false;
            }
            ExecSQL(conn_db, " Create unique index ix_aid_kh1_1 on ttt_aid_kh1 (nzp_kvar, nzp_serv, nzp_kind, tn) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_aid_kh1 ", true);

            //выставим нормативы отключения
            //kh_itog - сколько можня снять по закону

            if (!nedoXX.otop5)
            {
                ret = ExecSQL(conn_db,
                    " Update ttt_aid_kh1 Set " +
#if PG
 " (kh_itog,kh_edin) = (( Select max(kolhour) From " + nedoXX.paramcalc.data_alias + "upg_s_nedop_type a " +
                                               " Where ttt_aid_kh1.nzp_kind = a.num_nedop " +
                                               "   and coalesce(a.kolhour,0) > 0 " +
                                            " ),( Select  min(kolhour) From " + nedoXX.paramcalc.data_alias + "upg_s_nedop_type a " +
                                               " Where ttt_aid_kh1.nzp_kind = a.num_nedop " +
                                               "   and coalesce(a.kolhour,0) > 0 " +
                                            " )) " +
#else
                    " (kh_itog,kh_edin) = (( Select max(kolhour), min(kolhour) From " + nedoXX.paramcalc.data_alias + "upg_s_nedop_type a " +
                                               " Where ttt_aid_kh1.nzp_kind = a.num_nedop " +
                                               "   and nvl(a.kolhour,0) > 0 " +
                                            " )) " +
#endif
 " Where EXISTS ( Select 1 From " + nedoXX.paramcalc.data_alias + "upg_s_nedop_type a " +
                                " Where ttt_aid_kh1.nzp_kind = a.num_nedop " +
                                "   and " + sNvlWord + "(a.kolhour,0) > 0 ) " +
                    "   and nzp_kind not in (5,9, 6,71)  "
                    , true);
                if (!ret.result)
                {
                    DropTempTablesNedo(conn_db);
                    return false;
                }
            }
            else
            {
                ret = ExecSQL(conn_db,
                    " Update ttt_aid_kh1 " +
                    " Set kh_itog = 24 " +
                       ", kh_edin = ( case when tn>=12 then 16 else " +
                                    " case when tn>=10 then 8 else " +
                                    " case when tn>=8 then 4 else 0 end " +
                                    " end " +
                                    " end ) " +
                    " Where nzp_kind = 5 "
                    , true);
                if (!ret.result)
                {
                    DropTempTablesNedo(conn_db);
                    return false;
                }
            }

            ret = ExecSQL(conn_db,
                " Update ttt_aid_kh1 " +
                " Set kh_edin = 0 " + //поскольку взято одно и тоже значение, а они д.б. разными!
                " Where kh_edin = kh_itog "
                , true);
            if (!ret.result)
            {
                DropTempTablesNedo(conn_db);
                return false;
            }

            //начинаем в цикле снимать нормативы
            //суммарно не более kh_itog
            //непростая задача
            while (true)
            {
                ExecSQL(conn_db, " Drop table ttt_aid_kh2 ", false);
                ssql =
                    " Create temp table ttt_aid_kh2 " +
                    " (  nzp_kvar    integer default 0 not null, " +
                    "    nzp_serv    integer not null, " +
                    "    nzp_kind    integer default 0, " +
                    "    nzp_ndx     integer default 0  " +
#if PG
 " ) ";
#else
                    " ) With no log ";
#endif
                ret = ExecSQL(conn_db, ssql, true);
                if (!ret.result)
                {
                    DropTempTablesNedo(conn_db);
                    return false;
                }

                //для начала определим work_zap - уникальная запись для update
                //т.к. при каждой итерации надо обрабатывать только единственный интервал лс!
                ret = ExecSQL(conn_db,
                    " Insert into ttt_aid_kh2 (nzp_kvar,nzp_serv,nzp_kind,nzp_ndx) " +
                    " Select nzp_kvar,nzp_serv,nzp_kind, max(nzp_ndx) as nzp_ndx " +
                    " From ttt_aid_kh b " +
                    " Where (b.cnts - b.cnts_del) > " + s0hour + //есть куда положить
                    "   and b.cnts_del = ' 0' " + //кроме уже обработанных интервалов
                    " Group by 1,2,3 "
                    , true);
                if (!ret.result)
                {
                    DropTempTablesNedo(conn_db);
                    return false;
                }
                ExecSQL(conn_db, " Create unique index ix_aid_kh2_0 on ttt_aid_kh2 (nzp_ndx) ", true);
                ExecSQL(conn_db, sUpdStat + " ttt_aid_kh2 ", true);

                ret = ExecSQL(conn_db,
                    " Update ttt_aid_kh " +
                    " Set work_zap = 0 " +
                    " Where work_zap = 1 " //кроме уже обработанных интервалов
                    , true);
                if (!ret.result)
                {
                    DropTempTablesNedo(conn_db);
                    return false;
                }
                ret = ExecSQL(conn_db,
                    " Update ttt_aid_kh k " +
                    " Set work_zap = 1 " +
                    " From ttt_aid_kh2 k2" +
                    " Where k.nzp_ndx = k2.nzp_ndx "
                    , true);
                if (!ret.result)
                {
                    DropTempTablesNedo(conn_db);
                    return false;
                }

                ExecSQL(conn_db, " Drop table ttt_aid_kh2 ", false);

                //единовремменно можно снять не более kh_edin>0, но в пределах kh_itog
                //два случая:
                //1 - когда kh_edin>0
                //2 - когда kh_edin=0


                if (!nedoXX.otop5)
                    ps = " and abs( " + sNvlWord + "(a.tn,0) - " + sNvlWord + "(b.tn,0) )<0.001 ";  //я не знаю зачем это условие здесь - 13.11.2012 !!!
                else
                    ps = " ";


#if PG
                ret = ExecSQL(conn_db,
                " Select distinct b.nzp_ndx, b.work_zap, " +
                       " case when a.kh_edin > 0 then " +
                    //1 - когда kh_edin>0
                            " case when b.cnts - b.cnts_del - a.kh_edin * interval '1 hour' > interval '0 hour' then " +  //интервал больше, чем един. снятие
                                 " case when a.kh_edin >= a.kh_itog " +                                //един. снятие больше остатка
                                 " then a.kh_itog * interval '1 hour'  " +                                       //берем остаток
                                 " else a.kh_edin * interval '1 hour'  " +                                       //берем един. снятие
                                 " end " +
                            " else " +                                                                 //интервал меньше, чем един. снятие
                                 " case when b.cnts - b.cnts_del >= a.kh_itog * interval '1 hour'  " +           //интервал больше, чем остаток
                                 " then a.kh_itog * interval '1 hour'  " +                                       //берем остаток
                                 " else b.cnts - b.cnts_del  " +                                       //берем интервал
                                 " end " +
                            " end " +
                       " else " +
                    //2 - когда kh_edin=0
                            " case when b.cnts - b.cnts_del - a.kh_itog * interval '1 hour'  > interval '0 hour' " +  //интервал больше, чем остаток
                            " then a.kh_itog * interval '1 hour'  " +                                            //берем остаток
                            " else b.cnts - b.cnts_del  " +                                            //берем интервал
                            " end " +
                       " end as cnt_del2 " +                                                           //сколько можем снять
                " Into temp ttt_aid_kh2  " +
                " From ttt_aid_kh1 a, ttt_aid_kh b   " +
                " Where a.kh_itog > 0 " + //есть остаток, где можно еще снять (это значение будем постепенно уменьшать)
                "   and a.nzp_kvar = b.nzp_kvar " +
                "   and a.nzp_serv = b.nzp_serv " +
                "   and a.nzp_kind = b.nzp_kind " +
                "   and b.cnts - b.cnts_del > interval '0 hour' " + //есть куда положить
                "   and b.cnts_del = ' 0' " + //кроме уже обработанных интервалов
                    ps
                , true);
#else
                ret = ExecSQL(conn_db,
                " Select unique b.nzp_ndx, b.work_zap, " +
                       " case when a.kh_edin > 0 then " +
                //1 - когда kh_edin>0
                            " case when b.cnts - b.cnts_del - a.kh_edin units hour > 0 units hour then " +  //интервал больше, чем един. снятие
                                 " case when a.kh_edin >= a.kh_itog " +                                //един. снятие больше остатка
                                 " then a.kh_itog units hour " +                                       //берем остаток
                                 " else a.kh_edin units hour " +                                       //берем един. снятие
                                 " end " +
                            " else " +                                                                 //интервал меньше, чем един. снятие
                                 " case when b.cnts - b.cnts_del >= a.kh_itog units hour " +           //интервал больше, чем остаток
                                 " then a.kh_itog units hour " +                                       //берем остаток
                                 " else b.cnts - b.cnts_del  " +                                       //берем интервал
                                 " end " +
                            " end " +
                       " else " +
                //2 - когда kh_edin=0
                            " case when b.cnts - b.cnts_del - a.kh_itog units hour > 0 units hour " +  //интервал больше, чем остаток
                            " then a.kh_itog units hour " +                                            //берем остаток
                            " else b.cnts - b.cnts_del  " +                                            //берем интервал
                            " end " +
                       " end as cnt_del2 " +                                                           //сколько можем снять
                " From ttt_aid_kh1 a, ttt_aid_kh b   " +
                " Where a.kh_itog > 0 " + //есть остаток, где можно еще снять (это значение будем постепенно уменьшать)
                "   and a.nzp_kvar = b.nzp_kvar " +
                "   and a.nzp_serv = b.nzp_serv " +
                "   and a.nzp_kind = b.nzp_kind " +
                "   and b.cnts - b.cnts_del >0 units hour " + //есть куда положить
                "   and b.cnts_del = ' 0' " + //кроме уже обработанных интервалов
                    ps +
                " Into temp ttt_aid_kh2 With no log "
                , true);
#endif
                if (!ret.result)
                {
                    DropTempTablesNedo(conn_db);
                    return false;
                }

                //ViewTbl(conn_db, " select * from ttt_aid_kh2 ");

                //ViewTbl(conn_db, " select * from ttt_aid_kh1 ");

                //ViewTbl(conn_db, " select * from ttt_aid_kh ");


                var b = DBManager.ExecScalar<bool>(conn_db,
                    " Select count(1)>0 as cnt From ttt_aid_kh2 Where work_zap = 1 and cnt_del2 >" + s0hour, out ret,
                    true);
                if (!ret.result)
                {
                    DropTempTablesNedo(conn_db);
                    return false;
                }
                if (b)
                {
                    ExecSQL(conn_db, " Create index ix_aid_kh2_0 on ttt_aid_kh2 (nzp_ndx) ", true);
                    ExecSQL(conn_db, sUpdStat + " ttt_aid_kh2 ", true);
                    //цикл только по одной записи <nzp_kvar,nzp_serv,nzp_kind>
                    ret = ExecSQL(conn_db,
                        " Update ttt_aid_kh " +
                        " Set cnts_del = ( Select max(cnt_del2) From ttt_aid_kh2 Where ttt_aid_kh.nzp_ndx = ttt_aid_kh2.nzp_ndx and ttt_aid_kh2.work_zap = 1) " +
                        " Where EXISTS ( Select 1 From ttt_aid_kh2 Where ttt_aid_kh.nzp_ndx = ttt_aid_kh2.nzp_ndx and ttt_aid_kh2.work_zap = 1) "
                        // + "  and work_zap = 1 "
                        , true);
                    if (!ret.result)
                    {
                        DropTempTablesNedo(conn_db);
                        return false;
                    }
                    ret = ExecSQL(conn_db,
                        " Update " + nedoXX.TempTableNedops +
                        " Set cnts_del = ( Select max(cnt_del2) From ttt_aid_kh2 Where " + nedoXX.TempTableNedops + ".nzp_ndx = ttt_aid_kh2.nzp_ndx and ttt_aid_kh2.work_zap = 1) " +
                        " Where EXISTS ( Select 1 From ttt_aid_kh2 Where " + nedoXX.TempTableNedops + ".nzp_ndx = ttt_aid_kh2.nzp_ndx and ttt_aid_kh2.work_zap = 1) "
                        , true);
                    if (!ret.result)
                    {
                        DropTempTablesNedo(conn_db);
                        return false;
                    }
                    ret = ExecSQL(conn_db,
                        " Update ttt_aid_kh1 " +
                        " Set kh_itog = kh_itog - ( Select sum( " +
                        "(EXTRACT(day FROM cnts_del)*24 + EXTRACT(hour FROM cnts_del))::int)" +
                        " From ttt_aid_kh a " +
                        " Where a.nzp_kvar = ttt_aid_kh1.nzp_kvar " +
                        "   and a.nzp_serv = ttt_aid_kh1.nzp_serv " +
                        "   and a.nzp_kind = ttt_aid_kh1.nzp_kind " +
                        " ) " +
                        " Where EXISTS ( Select 1 From ttt_aid_kh a " +
                        " Where a.nzp_kvar = ttt_aid_kh1.nzp_kvar " +
                        "   and a.nzp_serv = ttt_aid_kh1.nzp_serv " +
                        "   and a.nzp_kind = ttt_aid_kh1.nzp_kind " +
                        " ) "
                        , true);
                    if (!ret.result)
                    {
                        DropTempTablesNedo(conn_db);
                        return false;
                    }
                }


                ExecSQL(conn_db, " Drop table ttt_aid_kh2 ", false);

                if (!b) break;
            }

            if (!nedoXX.otop5)
                ps = " and nzp_kind not in (5,9, 6,71) ";
            else
                ps = " and nzp_kind = 5 "; //отопление


            ExecByStep(conn_db, nedoXX.TempTableNedops, "nzp_ndx",
                " Update " + nedoXX.TempTableNedops +
                //" Set koef = "+nedoXX.paramcalc.pref+"_data:sortnum(cnts) * perc/100 "+//коэфициент почасовой
                " Set koef = perc/100 " +//коэфициент почасовой
                " Where " + nedoXX.where_z + nedoXX.paramcalc.per_dat_charge +
                "   and kod_info  = 1 " +
                "   and cur_zap <> -1 " +
                ps
               , 100000, "", out ret);
            if (!ret.result)
            {
                DropTempTablesNedo(conn_db);
                return false;
            }

            DropTempTablesNedo(conn_db);
            return true;
        }
        //-----------------------------------------------------------------------------
        public bool CalcNedoXX(IDbConnection conn_db, CalcTypes.ParamCalc paramcalc, out Returns ret)
        //-----------------------------------------------------------------------------
        {
            NedoXX nedoXX = new NedoXX(paramcalc);

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

            CreateNedoXX(conn_db, nedoXX, out ret);
            if (!ret.result)
            {
                ret.text = "Ошибка инициализации таблицы недопоставок";
                return false;
            }

            string p_dat_charge = DateNullString;
            if (!nedoXX.paramcalc.b_cur)
                p_dat_charge = sDefaultSchema + "MDY(" + nedoXX.paramcalc.cur_mm + ",28," + nedoXX.paramcalc.cur_yy + ")";

            //проверка tn на float
            ExecSQL(conn_db, " Drop table ttt_aid_tn ", false);

            ret = ExecSQL(conn_db,
                " Create temp table ttt_aid_tn (tn numeric(10,5)) " +
#if PG
 " "
#else
                " With no log "
#endif
, true);
            if (!ret.result)
            {
                DropTempTablesNedo(conn_db);
                return false;
            }


            //выбрать nedop_kvar во временную таблицу
            /*
            LoadTempTableNedop(conn_db, paramcalc, out ret);
            if (!ret.result)
            {
                DropTempTablesNedo(conn_db);
                return false;
            }
            */

            if (paramcalc.isPortal)
            {
                //учтем текущий charge_nedo во временной таблице (удалим и вставим строки)
                Portal_UchetNedop(conn_db, paramcalc, out ret);
                if (!ret.result)
                {
                    DropTempTablesNedo(conn_db);
                    return false;
                }
            }

            //создаем временную таблицу
            ret = CreateTableNedoXX(nedoXX, nedoXX.TempTableNedops, conn_db, true);
            if (!ret.result)
            {
                DropTempTablesNedo(conn_db);
                return false;
            }

            //выбираем только те записи, у которых tn приводим к numeric
            ret = ExecSQL(conn_db,
                string.Format(@" INSERT INTO {0} (nzp_kvar, nzp_dom,dat_charge,nzp_serv,tn,nzp_kind, dat_s, dat_po)
                                 SELECT {1} nzp_kvar,nzp_dom, {2} as dat_charge, nzp_serv,
                                 (case when trim(tn)<>'' then replace( tn , ',', '.') else '0' end){3} as tn, nzp_kind, dat_s, dat_po
                                 From temp_table_nedop 
                                 WHERE (case when trim(tn)<>'' then replace(tn,',', '.') else '0' end)~'^([0-9]+\.?[0-9]*|\.[0-9]+)$'",
                    nedoXX.TempTableNedops, sUniqueWord, p_dat_charge, sConvToNum),
                true);
            if (!ret.result)
            {
                DropTempTablesNedo(conn_db);
                return false;
            }

            ExecByStep(conn_db, nedoXX.TempTableNedops, "nzp_ndx",
                " Update " + nedoXX.TempTableNedops +
                " Set dat_s = " + nedoXX.dat_s +
                " Where " + nedoXX.where_z +
                "   and dat_s < " + nedoXX.dat_s
               , 100000, "", out ret);
            if (!ret.result)
            {
                DropTempTablesNedo(conn_db);
                return false;
            }
            ExecByStep(conn_db, nedoXX.TempTableNedops, "nzp_ndx",
                " Update " + nedoXX.TempTableNedops +
                " Set dat_po = " + nedoXX.dat_po +
                " Where " + nedoXX.where_z +
                "   and dat_po > " + nedoXX.dat_po
               , 100000, "", out ret);
            if (!ret.result)
            {
                DropTempTablesNedo(conn_db);
                return false;
            }

            ExecByStep(conn_db, nedoXX.TempTableNedops, "nzp_ndx",
                " Update " + nedoXX.TempTableNedops +
                " Set cnts = (dat_po - dat_s)" +
                //nzp_msr
                 ", kod_info = ( Select max(nzp_msr) From " + paramcalc.data_alias + "upg_s_nedop_type a " +
                               " Where " + nedoXX.TempTableNedops + ".nzp_kind = a.num_nedop ) " +
                 ", perc = ( Select max(percent) From " + paramcalc.data_alias + "upg_s_nedop_type a " +
                           " Where " + nedoXX.TempTableNedops + ".nzp_kind = a.num_nedop ) " +
                " Where " + nedoXX.where_z + nedoXX.paramcalc.per_dat_charge +
                    "   and EXISTS ( Select 1 From " + paramcalc.data_alias + "upg_s_nedop_type a " +
                                 " Where " + nedoXX.TempTableNedops + ".nzp_kind = a.num_nedop ) "
               , 100000, "", out ret);
            if (!ret.result)
            {
                DropTempTablesNedo(conn_db);
                return false;
            }

            //удалим неправльно установленные tn
            ExecByStep(conn_db, nedoXX.TempTableNedops, "nzp_ndx",
                " Update " + nedoXX.TempTableNedops +
                " Set tn = null " +
                " Where " + nedoXX.where_z + nedoXX.paramcalc.per_dat_charge +
                    "   and (nzp_kind not in (5,6,71,9) and nzp_kind < 2001) and tn is not null "
               , 100000, "", out ret);
            if (!ret.result)
            {
                DropTempTablesNedo(conn_db);
                return false;
            }
            //в остальных выставим tn где пусто
            ExecByStep(conn_db, nedoXX.TempTableNedops, "nzp_ndx",
                " Update " + nedoXX.TempTableNedops +
                " Set tn = 0 " +
                " Where " + nedoXX.where_z + nedoXX.paramcalc.per_dat_charge +
                    "   and (nzp_kind in (5,6,71,9) or nzp_kind > 2000) and tn is null "
               , 100000, "", out ret);
            if (!ret.result)
            {
                DropTempTablesNedo(conn_db);
                return false;
            }
            //ГВС - отработаем исключение из правил
            ExecByStep(conn_db, nedoXX.TempTableNedops, "nzp_ndx",
                " Update " + nedoXX.TempTableNedops +
                " Set kod_info = 1 " +
                   ", perc = ( Select min(percent) From " + paramcalc.data_alias + "upg_s_nedop_type a " +
                             " Where " + nedoXX.TempTableNedops + ".nzp_kind = a.num_nedop ) " +
                " Where " + nedoXX.where_z + nedoXX.paramcalc.per_dat_charge +
                "   and nzp_kind = 9 and tn >= 40 and kod_info = 2 "
               , 100000, "", out ret);
            if (!ret.result)
            {
                DropTempTablesNedo(conn_db);
                return false;
            }



            //----------------------------------------------------------------
            //нетревиальная обработка пересекающихся интервалов одинаковых nzp_kvar,nzp_serv,nzp_kind,tn
            //----------------------------------------------------------------
            ExecSQL(conn_db, " Drop table ttt_aid_tn ", false);

            string st1 = "";
            if (nedoXX.paramcalc.b_cur)
                st1 = " and a.dat_charge is null and b.dat_charge is null ";
            else
                st1 = " and a.dat_charge = " + MDY(nedoXX.paramcalc.cur_mm, 28, nedoXX.paramcalc.cur_yy) +
                      " and a.dat_charge = b.dat_charge ";
            ret = ExecSQL(conn_db,
                " Select a.nzp_kvar, a.nzp_dom, a.dat_charge, a.nzp_serv, a.nzp_kind, a.tn, a.kod_info, a.perc, " +
                          " min(a.dat_s) as dat_s, max(a.dat_po) as dat_po " +
#if PG
 " Into temp ttt_aid_tn  " +
#else
                " " +
#endif
 " From " + nedoXX.TempTableNedops + " a, " + nedoXX.TempTableNedops + " b " +
                " Where a.nzp_kvar = b.nzp_kvar " +
                "   and a.nzp_serv = b.nzp_serv " +
                "   and a.nzp_kind = b.nzp_kind " +
                "   and " + sNvlWord + "(a.tn,0) = " + sNvlWord + "(b.tn,0)" +
                "   and a.nzp_ndx <> b.nzp_ndx  " +
                "   and a.dat_s  <= b.dat_po " +
                "   and a.dat_po >= b.dat_s  " +
                "   and a." + nedoXX.where_z + st1 +
                " Group by 1,2,3,4,5,6,7,8 " +
#if PG
 " "
#else
                " Into temp ttt_aid_tn With no log "
#endif
, true);
            if (!ret.result)
            {
                DropTempTablesNedo(conn_db);
                return false;
            }
            ExecSQL(conn_db, " Create index ix_aid_tn1 on ttt_aid_tn (nzp_kvar, nzp_serv, nzp_kind) ", true);
            ExecSQL(conn_db, " Create index ix_aid_tn2 on ttt_aid_tn (nzp_dom) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_aid_tn ", true);
            //удалим измененные строки (пока скинем в архив для отладки)
            ExecByStep(conn_db, nedoXX.TempTableNedops, "nzp_ndx",
                " Update " + nedoXX.TempTableNedops + " n "+
                " Set cur_zap = -1 " +
                " From ttt_aid_tn b" +
                " Where "+
                "   n.nzp_kvar = b.nzp_kvar " +
                "   and n.nzp_serv = b.nzp_serv " +
                "   and n.nzp_kind = b.nzp_kind " +
                "   and " + sNvlWord + "(n.tn,0) = " + sNvlWord + "(b.tn,0)"
                , 100000, "", out ret);
            if (!ret.result)
            {
                DropTempTablesNedo(conn_db);
                return false;
            }
            //и введем измененную строку
            ret = ExecSQL(conn_db,
                " Insert into " + nedoXX.TempTableNedops +
                " ( nzp_kvar,nzp_dom,dat_charge,nzp_serv,tn,nzp_kind, dat_s,dat_po, kod_info,perc, cnts, cur_zap ) " +
                " Select nzp_kvar,nzp_dom,dat_charge,nzp_serv,tn,nzp_kind, dat_s,dat_po, kod_info,perc, " +
#if PG
 " case when dat_po - dat_s > interval '999 hour' then interval '999 hour' " +
#else
                " case when dat_po - dat_s > 999 units hour then 999 units hour " +
#endif
 " else dat_po - dat_s end, 1 " +
                " From ttt_aid_tn "
                , true);
            if (!ret.result)
            {
                DropTempTablesNedo(conn_db);
                return false;
            }

            ExecSQL(conn_db, " Drop table ttt_aid_tn ", false);


            //----------------------------------------------------------------
            //подсчет коэфициентов скидок
            //----------------------------------------------------------------
            string month_hours = (DateTime.DaysInMonth(nedoXX.paramcalc.calc_yy, nedoXX.paramcalc.calc_mm) * 24).ToString();

            //сначала 100% скидки (nzp_msr = 2)
            //простая почасовая пропорция
            ExecByStep(conn_db, nedoXX.TempTableNedops, "nzp_ndx",
                " Update " + nedoXX.TempTableNedops +
                " Set cur_zap = 1, " +
                    " koef = 1.00 / " + month_hours +
                " Where " + nedoXX.where_z + nedoXX.paramcalc.per_dat_charge +
                    "   and kod_info  = 2 " +
                    "   and cur_zap <> -1 "
               , 100000, "", out ret);
            if (!ret.result)
            {
                DropTempTablesNedo(conn_db);
                return false;
            }

            //% скидки tn
            // выставить %(tn) по недопоставке услуги содержания жилья по части услуги (калькуляции)
            //

            ExecSQL(conn_db, " Drop table ttt_ndp_clcsp ", false);
            ret = ExecSQL(conn_db,
                " Create temp table ttt_ndp_clcsp " +
                //" Create table are.ttt_ndp_clcsp " +
                " ( nzp_ndx     integer not null, " +
                "   nzp_dom     integer not null, " +
                "   nzp_kvar    integer default 0 not null, " +
                "   nzp_area    integer default 0 not null, " +
                "   nzp_serv    integer not null, " +
                "   tn          float, " +
#if PG
 "   dat_s       timestamp, " +
                "   dat_po      timestamp, " +
#else
                "   dat_s       datetime year to hour, " +
                "   dat_po      datetime year to hour, " +
#endif
 "   prop        float default 0, " +
                "   nzp_frm     integer default 0, " +
                "   tarif       " + sDecimalType + "(14,4) default 0," +
                "   nzp_serv_link integer default 0 " +
#if PG
 " )  "
#else
                //" ) "
                " ) with no log "
#endif
, true);
            if (!ret.result)
            {
                DropTempTablesNedo(conn_db);
                return false;
            }
            ret = ExecSQL(conn_db,
                " Insert into ttt_ndp_clcsp (nzp_ndx,nzp_kvar,nzp_dom,nzp_serv,tn,dat_s,dat_po,nzp_serv_link)" +
                " Select b.nzp_ndx,b.nzp_kvar,b.nzp_dom,b.nzp_serv,b.tn,b.dat_s,b.dat_po,a.step" +
                " From " + nedoXX.TempTableNedops + " b," + paramcalc.data_alias + "upg_s_nedop_type a " +
                " Where " + nedoXX.where_z + nedoXX.paramcalc.per_dat_charge +
                "   and b.nzp_kind = a.num_nedop and b.nzp_serv=a.nzp_serv " +
                "   and b.kod_info = 2 and b.tn is not null and b.cur_zap <> -1 " +
                "   and a.nzp_msr=2 and a.num_nedop>2000 and a.step>0 "
                , true);
            if (!ret.result)
            {
                DropTempTablesNedo(conn_db);
                return false;
            }

            ExecSQL(conn_db, " Create index ix_ndp_clcsp1 on ttt_ndp_clcsp (nzp_ndx) ", true);
            ExecSQL(conn_db, " Create index ix_ndp_clcsp2 on ttt_ndp_clcsp (nzp_kvar,nzp_serv) ", true);
            ExecSQL(conn_db, " Create index ix_ndp_clcsp3 on ttt_ndp_clcsp (nzp_serv_link) ", true);
            ExecSQL(conn_db, sUpdStat + "  ttt_ndp_clcsp ", true);

            ret = ExecSQL(conn_db,
                " update ttt_ndp_clcsp set nzp_area=" +
                " (Select max(k.nzp_area) From t_selkvar k" +
                " Where k.nzp_kvar=ttt_ndp_clcsp.nzp_kvar " +
                " ) " +
                " where EXISTS (SELECT 1 From t_selkvar k" +
                " Where k.nzp_kvar=ttt_ndp_clcsp.nzp_kvar " +
                " ) "
                , true);
            if (!ret.result)
            {
                DropTempTablesNedo(conn_db);
                return false;
            }
            ret = ExecSQL(conn_db,
                " update ttt_ndp_clcsp set nzp_frm=" +
                " (Select max(a.nzp_frm) From temp_table_tarif a" +
                " Where a.nzp_kvar=ttt_ndp_clcsp.nzp_kvar and a.nzp_serv=ttt_ndp_clcsp.nzp_serv " +
                " ) " +
                " where EXISTS (SELECT 1 From temp_table_tarif a" +
                " Where a.nzp_kvar=ttt_ndp_clcsp.nzp_kvar and a.nzp_serv=ttt_ndp_clcsp.nzp_serv " +
                " ) "
                , true);
            if (!ret.result)
            {
                DropTempTablesNedo(conn_db);
                return false;
            }

            // ttc_tarif ttt_prm_1f ttt_prm_2f
            //установить тарифы и пропорции
            ret = SetTarifsAndProportion(conn_db, paramcalc, nedoXX);
            if (!ret.result)
            {
                DropTempTablesNedo(conn_db);
                return false;
            }

            //
            ExecByStep(conn_db, nedoXX.TempTableNedops, "nzp_ndx",
                " UPDATE " + nedoXX.TempTableNedops +
                " SET tn= a.tn * a.prop " +
                " FROM ttt_ndp_clcsp a" +
                " WHERE a.nzp_ndx=" + nedoXX.TempTableNedops + ".nzp_ndx"
               , 100000, "", out ret);
            if (!ret.result)
            {
                DropTempTablesNedo(conn_db);
                return false;
            }
            //

            //% скидки tn
            //простая почасовая пропорция
            ExecByStep(conn_db, nedoXX.TempTableNedops, "nzp_ndx",
                " Update " + nedoXX.TempTableNedops +
                " Set cur_zap = 1, " +
                    " perc = tn, " +
                    " koef = tn / 100.00 / " + month_hours +
                " Where " + nedoXX.where_z + nedoXX.paramcalc.per_dat_charge +
                    "   and kod_info = 2 " +
                    "   and nzp_kind >= 2001 and tn is not null " +
                    "   and cur_zap <> -1 "
               , 100000, "", out ret);
            if (!ret.result)
            {
                DropTempTablesNedo(conn_db);
                return false;
            }

            //----------------------------------------------------------------
            //Выберем типы, где нет kolhour и температурных недпоставок (num_nedop not in (5,9))
            //----------------------------------------------------------------
            ExecByStep(conn_db, nedoXX.TempTableNedops, "nzp_ndx",
                " Update " + nedoXX.TempTableNedops +
                //" Set koef = " + paramcalc.data_alias + "sortnum(cnts) * perc/100 / "+month_hours+
                " Set koef = perc/100.00 " +
                " Where " + nedoXX.where_z + nedoXX.paramcalc.per_dat_charge +
                "   and kod_info  = 1 " +
                "   and cur_zap <> -1 " +
                "   and NOT EXISTS (SELECT 1 From " + paramcalc.data_alias + "upg_s_nedop_type" +
                            " Where " + sNvlWord + "(kolhour,0) > 0  ) " +
                "   and nzp_kind not in (5,9, 6,71) "
               , 100000, "", out ret);
            if (!ret.result)
            {
                DropTempTablesNedo(conn_db);
                return false;
            }
            ExecByStep(conn_db, nedoXX.TempTableNedops, "nzp_ndx",
                " Update " + nedoXX.TempTableNedops +
                " Set cnts_del = ' 0' " +
                " Where " + nedoXX.where_z + nedoXX.paramcalc.per_dat_charge +
                "   and cur_zap <> -1 " +
                "   and cnts_del is null "
               , 100000, "", out ret);
            if (!ret.result)
            {
                DropTempTablesNedo(conn_db);
                return false;
            }


            //----------------------------------------------------------------
            //Выберем типы, где есть kolhour - допустимые прерывания и нет температурных недпоставок (num_nedop not in (5,9, 6,71)
            //----------------------------------------------------------------
            nedoXX.otop5 = false;
            var b = KolHour(conn_db, nedoXX, out ret);
            if (!b || !ret.result)
            {
                DropTempTablesNedo(conn_db);
                return false;
            }

            //----------------------------------------------------------------
            //Выберем типы, где есть температурные недпоставки (num_nedop in (5,9, 6,71))
            //----------------------------------------------------------------
            //отопление: num_nedop =5
            nedoXX.otop5 = true;
            b = KolHour(conn_db, nedoXX, out ret);
            if (!b || !ret.result)
            {
                DropTempTablesNedo(conn_db);
                return false;
            }

            //отопление: температурные (num_nedop in (6,71))
            //угловые комнаты (prm_1 310) 20-24 (при нормативе 18С: num_nedop=6) и 22-26 (при нормативе 20С: num_nedop=71)
            //иначе 18-22 (при нормативе 18С) и 20-24 (при нормативе 20С)
            //0.15 за каждый градус
            // 4 градуса допустимо для Пост354!!!
            ret = CalcTemperatureNedoForHeating(conn_db, nedoXX);
            if (!ret.result)
            {
                DropTempTablesNedo(conn_db);
                return false;
            }

            //----------------------------------------------------------------
            //и наконец ГВС: num_nedop =9
            //----------------------------------------------------------------
            //обработка step, надо увеличить perc в пропорции step за пределами [tn_min,tn_max]

            ret = CalcTemperatureNedoForHotWater(conn_db, nedoXX, month_hours);
            if (!ret.result)
            {
                DropTempTablesNedo(conn_db);
                return false;
            }
            //----------------------------------------------------------------
            //напоследок надо выбрать максимальный коэфициент для пересекающихся недопоставок, остальные интервалы урезать 
            //----------------------------------------------------------------
            ret = CalcMaxNedoInCrossingPeriods(conn_db, ret, nedoXX, st1);
            if (!ret.result)
            {
                DropTempTablesNedo(conn_db);
                return false;
            }

            //завершающие действия с недопоставками и запись их в nedo_xx
            ret = FinalActionsWithNedo(conn_db, nedoXX);
            if (!ret.result)
            {
                DropTempTablesNedo(conn_db);
                return false;
            }

            UpdateStatistics(false, paramcalc, nedoXX.nedo_tab, out ret);
            DropTempTablesNedo(conn_db);
            return true;
        }

        /// <summary>
        /// установить тарифы и пропорции
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="paramcalc"></param>
        /// <param name="nedoXX"></param>
        /// <returns></returns>
        private Returns SetTarifsAndProportion(IDbConnection conn_db, CalcTypes.ParamCalc paramcalc, NedoXX nedoXX)
        {
            Returns ret;
            ExecSQL(conn_db, " Drop table ttt_ndp_spr ", false);
            ret = ExecSQL(conn_db,
                " Create temp table ttt_ndp_spr " +
                " (nzp_tarif integer," +
                "  nzp_serv integer," +
                "  nzp_frm integer," +
                "  nzp_prm_ls integer default 0," +
                "  nzp_prm_dom integer default 0," +
                "  nzp_prm_bd integer default 0," +
                "  sumt numeric(14,4) " +
#if PG
 " ) "
#else
                " ) with no log "
#endif
, true);
            if (!ret.result) return ret;

            ret = ExecSQL(conn_db,
                " insert into ttt_ndp_spr (nzp_tarif,nzp_serv,nzp_frm,nzp_prm_ls,nzp_prm_dom,nzp_prm_bd,sumt) " +
                " select a.nzp_tarif,a.nzp_serv,a.nzp_frm,a.nzp_prm_ls,a.nzp_prm_dom,a.nzp_prm_bd,a.sumt " +
                " from " + paramcalc.data_alias + "s_calc_trf a, ttt_ndp_clcsp t " +
                " where t.nzp_serv=a.nzp_serv and t.nzp_frm=a.nzp_frm and t.nzp_area=a.nzp_area" +
                " and a.dat_s <=" + sDefaultSchema + "mdy(" + nedoXX.paramcalc.calc_mm.ToString() + ",28," +
                nedoXX.paramcalc.calc_yy.ToString("0000") + ") " +
                " and a.dat_po>=" + sDefaultSchema + "mdy(" + nedoXX.paramcalc.calc_mm.ToString() + ",01," +
                nedoXX.paramcalc.calc_yy.ToString("0000") + ") "
                , true);
            if (!ret.result) return ret;

            ExecSQL(conn_db, " Create index ix_ndp_spr1 on ttt_ndp_spr (nzp_serv,nzp_frm) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_ndp_spr ", true);

            // проставить тарифы на всю БД
            ret = ExecSQL(conn_db,
                " update ttt_ndp_clcsp set tarif=" +
                " (Select max( replace(a.val_prm,',','.')" + sConvToNum + " ) From " + paramcalc.data_alias +
                "prm_5 a,ttt_ndp_spr b" +
                " Where b.nzp_serv=ttt_ndp_clcsp.nzp_serv and b.nzp_frm=ttt_ndp_clcsp.nzp_frm" +
                "   and a.nzp_prm=b.nzp_prm_bd and b.nzp_prm_bd>0 and a.is_actual <> 100 " +
                "   and a.dat_s <=" + sDefaultSchema + "mdy(" + nedoXX.paramcalc.calc_mm + ",28," +
                nedoXX.paramcalc.calc_yy.ToString("0000") + ") " +
                "   and a.dat_po>=" + sDefaultSchema + "mdy(" + nedoXX.paramcalc.calc_mm + ",01," +
                nedoXX.paramcalc.calc_yy.ToString("0000") + ") " +
                " ) " +
                " where EXISTS (SELECT 1 From " + paramcalc.data_alias + "prm_5 a,ttt_ndp_spr b" +
                " Where b.nzp_serv=ttt_ndp_clcsp.nzp_serv and b.nzp_frm=ttt_ndp_clcsp.nzp_frm" +
                "   and a.nzp_prm=b.nzp_prm_bd and b.nzp_prm_bd>0 and a.is_actual <> 100 " +
                "   and a.dat_s <=" + sDefaultSchema + "mdy(" + nedoXX.paramcalc.calc_mm + ",28," +
                nedoXX.paramcalc.calc_yy.ToString("0000") + ") " +
                "   and a.dat_po>=" + sDefaultSchema + "mdy(" + nedoXX.paramcalc.calc_mm + ",01," +
                nedoXX.paramcalc.calc_yy.ToString("0000") + ") " +
                " ) "
                , true);
            if (!ret.result) return ret;

            // проставить домовые тарифы
            ret = ExecSQL(conn_db,
                " update ttt_ndp_clcsp set tarif=" +
                " (Select max(a.val_prm" + sConvToNum + ") From ttt_prm_2 a,ttt_ndp_spr b" +
                " Where a.nzp=ttt_ndp_clcsp.nzp_dom and a.nzp_prm=b.nzp_prm_dom and b.nzp_prm_dom>0 " +
                "   and b.nzp_serv=ttt_ndp_clcsp.nzp_serv and b.nzp_frm=ttt_ndp_clcsp.nzp_frm" +
                " ) " +
                " where EXISTS (SELECT 1 From ttt_prm_2 a,ttt_ndp_spr b" +
                " Where a.nzp=ttt_ndp_clcsp.nzp_dom and a.nzp_prm=b.nzp_prm_dom and b.nzp_prm_dom>0 " +
                "   and b.nzp_serv=ttt_ndp_clcsp.nzp_serv and b.nzp_frm=ttt_ndp_clcsp.nzp_frm" +
                " ) "
                , true);
            if (!ret.result) return ret;

            // проставить квартирные тарифы
            ret = ExecSQL(conn_db,
                " update ttt_ndp_clcsp set tarif=" +
                " (Select max(a.val_prm" + sConvToNum + ") From ttt_prm_1 a,ttt_ndp_spr b" +
                " Where a.nzp=ttt_ndp_clcsp.nzp_kvar and a.nzp_prm=b.nzp_prm_ls and b.nzp_prm_ls>0 " +
                "   and b.nzp_serv=ttt_ndp_clcsp.nzp_serv and b.nzp_frm=ttt_ndp_clcsp.nzp_frm" +
                " ) " +
                " where EXISTS (SELECT 1 From ttt_prm_1 a,ttt_ndp_spr b" +
                " Where a.nzp=ttt_ndp_clcsp.nzp_kvar and a.nzp_prm=b.nzp_prm_ls and b.nzp_prm_ls>0 " +
                "   and b.nzp_serv=ttt_ndp_clcsp.nzp_serv and b.nzp_frm=ttt_ndp_clcsp.nzp_frm" +
                " ) "
                , true);
            if (!ret.result) return ret;

            ExecSQL(conn_db, " Create index ix_ndp_clcsp4 on ttt_ndp_clcsp (tarif) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_ndp_clcsp ", true);

            // проставить пропорцию по тарифу
            ret = ExecSQL(conn_db,
                " update ttt_ndp_clcsp set prop=" +
                " (Select max(a.sump/b.sumt) From " + paramcalc.data_alias + "s_calc_trf_lnk a,ttt_ndp_spr b" +
                " Where a.nzp_tarif=b.nzp_tarif and abs(b.sumt-ttt_ndp_clcsp.tarif)<0.0001 and a.nzp_serv=ttt_ndp_clcsp.nzp_serv_link " +
                " ) " +
                " where tarif>0" +
                "   and EXISTS (SELECT 1 From " + paramcalc.data_alias + "s_calc_trf_lnk a,ttt_ndp_spr b" +
                "   Where a.nzp_tarif=b.nzp_tarif and abs(b.sumt-ttt_ndp_clcsp.tarif)<0.0001 and a.nzp_serv=ttt_ndp_clcsp.nzp_serv_link " +
                " ) "
                , true);
            if (!ret.result) return ret;

            return ret;
        }

        /// <summary>
        /// Финальная обработка недопоставок и сохранение в постоянную таблицу
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="nedoXX"></param>
        /// <returns></returns>
        private Returns FinalActionsWithNedo(IDbConnection conn_db, NedoXX nedoXX)
        {
            Returns ret;
            ExecSQL(conn_db, " Drop table ttt_work_zap ", false);

            ret = ExecSQL(conn_db,
                " Create temp table ttt_work_zap " +
                " (  nzp_ndx    integer       not null, " +
#if PG
 "    cnts       interval      hour, " +
                "    cnts_del   interval      hour  " +
                " )  "
#else
                "    cnts       interval      hour(3) to hour, " +
                "    cnts_del   interval      hour(6) to hour  " +
                " ) With no log "
#endif
, true);
            if (!ret.result) return ret;

            ret = ExecSQL(conn_db,
                " Insert into ttt_work_zap (nzp_ndx, cnts, cnts_del ) " +
                " Select nzp_ndx, max(cnts), sum(cnts_del) From ttt_aid_int " +
                " Group by 1 "
                , true);
            if (!ret.result) return ret;

            ExecSQL(conn_db, " Create index ix_wrkzp_0 on ttt_work_zap (nzp_ndx) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_work_zap ", true);

            //ограничить
            ret = ExecSQL(conn_db,
                " Update ttt_work_zap Set cnts_del = cnts Where cnts_del > cnts "
                , true);
            if (!ret.result) return ret;

            ret = ExecSQL(conn_db,
                " Update " + nedoXX.TempTableNedops +
                " Set cnts_del = ( Select max(cnts_del) From ttt_work_zap Where " + nedoXX.TempTableNedops +
                ".nzp_ndx = ttt_work_zap.nzp_ndx ) " +
                " Where EXISTS (SELECT 1 From ttt_work_zap Where " + nedoXX.TempTableNedops +
                ".nzp_ndx = ttt_work_zap.nzp_ndx ) "
                , true);
            if (!ret.result) return ret;

            ExecSQL(conn_db, " Drop table ttt_work_zap ", false);


            ret = ExecSQL(conn_db,
                " Update " + nedoXX.TempTableNedops +
                " Set cnts_del = ' 0' " +
                " Where cnts_del is null "
                , true);
            if (!ret.result) return ret;

            //переносим данные в постоянную таблицу
            var sql = " INSERT INTO " + nedoXX.nedo_xx + "" +
                      " (nzp_dom, nzp_kvar, dat_charge, cur_zap, nzp_serv, nzp_kind, koef, " +
                      " tn, tn_min, tn_max, dat_s, dat_po, cnts, cnts_del, perc, kod_info)" +
                      " SELECT nzp_dom, nzp_kvar, dat_charge, cur_zap, nzp_serv, nzp_kind, koef, " +
                      " tn, tn_min, tn_max, dat_s, dat_po, cnts, cnts_del, perc, kod_info " +
                      " FROM " + nedoXX.TempTableNedops;

            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result) return ret;

            return ret;
        }

        /// <summary>
        /// Выбрать максимальные недопоставки в пересекающихся периодах
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="ret"></param>
        /// <param name="nedoXX"></param>
        /// <param name="st1"></param>
        /// <returns></returns>
        private Returns CalcMaxNedoInCrossingPeriods(IDbConnection conn_db, Returns ret, NedoXX nedoXX, string st1)
        {
            //заполним cnts_del
            ExecSQL(conn_db, " Drop table ttt_aid_int ", false);

            ret = ExecSQL(conn_db,
                " Create temp table ttt_aid_int " +
                //" Create table ttt_aid_int "+
                " (  nzp_pere    serial        not null, " +
                "    nzp_ndx     integer       not null, " +
                "    nzp_kvar    integer       default 0 not null, " +
                "    nzp_serv    integer       not null, " +
#if PG
 "    dat_s       timestamp, " +
                "    dat_po      timestamp, " +
                "    cnts        interval  hour, " +
                "    cnts_del    interval  hour, " +
                "    koef        numeric(10,8) default 0.00  " + //скидка за час
                " ) "
#else
                "    dat_s       datetime      year to hour, " +
                "    dat_po      datetime      year to hour, " +
                "    cnts        interval      hour(3) to hour, " +
                "    cnts_del    interval      hour(6) to hour, " +
                "    koef        decimal(10,8) default 0.00  " +  //скидка за час
                " ) With no log "
                //" ) "
#endif
, true);
            if (!ret.result)
            {
                DropTempTablesNedo(conn_db);
                return ret;
            }

            ret = ExecSQL(conn_db,
                " Insert into ttt_aid_int (nzp_ndx,nzp_kvar,nzp_serv, dat_s,dat_po, cnts,cnts_del, koef) " +
                " Select " + sUniqueWord + " a.nzp_ndx,a.nzp_kvar,a.nzp_serv, a.dat_s,a.dat_po, a.cnts,a.cnts_del, a.koef " +
                " From " + nedoXX.TempTableNedops + " a, " + nedoXX.TempTableNedops + " b " +
                " Where a.nzp_kvar = b.nzp_kvar " +
                "   and a.nzp_serv = b.nzp_serv " +
                "   and a.nzp_ndx <> b.nzp_ndx  " +
                "   and a.dat_s  <= b.dat_po " +
                "   and a.dat_po >= b.dat_s  " +
                "   and a." + nedoXX.where_z + st1 +
                "   and a.cur_zap <> -1 " +
                "   and b.cur_zap <> -1 "
                , true);
            if (!ret.result)
            {
                DropTempTablesNedo(conn_db);
                return ret;
            }

            ExecSQL(conn_db, " Create unique index ix_aid_int_0 on ttt_aid_int (nzp_pere) ", true);
            ExecSQL(conn_db, " Create        index ix_aid_int_1 on ttt_aid_int (nzp_kvar, nzp_serv, dat_s, dat_po) ", true);
            ExecSQL(conn_db, " Create        index ix_aid_int_2 on ttt_aid_int (nzp_ndx) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_aid_int ", true);

            while (true)
            {
                //схема следуюущая:
                //1. Обработка сверху вниз (от максимального коэфициента к минимальному) 
                //2. Каждый раз обрабатывется только одна пара пересекающихся интервалов
                //3. Обработынный интервал корректируется
                //4. Цикл продолжается пока есть пересечения

                ExecSQL(conn_db, " Drop table ttt_aid_del ", false);

                ret = ExecSQL(conn_db,
                    " Create temp table ttt_aid_del " +
                    //" Create table ttt_aid_del " +
                    " (  nzp_pere    integer       not null, " +
                    "    nzp_ndx     integer       not null, " +
                    "    a_nzp_pere  integer       not null, " +
                    "    nzp_kvar    integer       default 0 not null, " +
                    "    nzp_serv    integer       not null, " +
                    "    work_zap    integer       default 0, " + //рабочая запись
#if PG
                        "    a_dat_s     timestamp, " +
                    "    a_dat_po    timestamp, " +
                    "    b_dat_s     timestamp, " +
                    "    b_dat_po    timestamp, " +
                    "    cnts        interval      hour, " +
                    "    cnts_del    interval      hour, " +
                    "    cnt_del2    interval      hour, " +
                    "    a_koef      numeric(10,8) default 0.00, " +
                    "    koef        numeric(10,8) default 0.00  " +
                    " )  "
#else
                    "    a_dat_s     datetime      year to hour, " +
                    "    a_dat_po    datetime      year to hour, " +
                    "    b_dat_s     datetime      year to hour, " +
                    "    b_dat_po    datetime      year to hour, " +
                    "    cnts        interval      hour(3) to hour, " +
                    "    cnts_del    interval      hour(6) to hour, " +
                    "    cnt_del2    interval      hour(6) to hour, " +
                    "    a_koef      decimal(10,8) default 0.00, " +
                    "    koef        decimal(10,8) default 0.00  " +
                    " ) With no log "
                    //") "
#endif
, true);
                if (!ret.result) return ret;

                ret = ExecSQL(conn_db,
                    " Insert into ttt_aid_del (nzp_ndx,nzp_pere,a_nzp_pere,a_dat_s,a_dat_po,b_dat_s,b_dat_po,nzp_kvar,nzp_serv,cnts,cnts_del,koef,a_koef,cnt_del2) " +
                    " Select " + sUniqueWord + " b.nzp_ndx, b.nzp_pere, a.nzp_pere, " +
                    " a.dat_s as a_dat_s, a.dat_po as a_dat_po, b.dat_s as b_dat_s, b.dat_po as b_dat_po," +
                    " b.nzp_kvar,b.nzp_serv, b.cnts,b.cnts_del, b.koef, a.koef," +
                    //интервал b. урезаем
                    //     [---------------] a.
                    //  [-----------]        b.
                    " case when b.dat_s <= a.dat_s and b.dat_po < a.dat_po " +
                    " then (b.dat_po - a.dat_s) " +
                    " else " +
                    //  [---------------]    a.
                    //        [-----------]  b.
                    " case when b.dat_s >= a.dat_s and b.dat_po > a.dat_po " +
                    " then (a.dat_po - b.dat_s) " +
                    " else " +
                    //      [----------]     a.
                    //   [---------------]   b.
                    " case when b.dat_s <= a.dat_s and b.dat_po >= a.dat_po " +
                    " then (a.dat_po - a.dat_s) " +
                    " else " +
                    //  [---------------]    a.
                    //    [----------]       b.
                    " (b.dat_po - b.dat_s) " + //весь интервал
                    " end end end as cnt_del2 " +
                    " From ttt_aid_int a, ttt_aid_int b  " +
                    " Where a.nzp_kvar = b.nzp_kvar " +
                    "   and a.nzp_serv = b.nzp_serv " +
                    "   and a.nzp_ndx <> b.nzp_ndx  " +
                    "   and a.dat_s   <= b.dat_po " + //пересекающиеся интервалы
                    "   and a.dat_po  >= b.dat_s  " +
                    "   and a.koef > 0  " +
                    "   and b.koef > 0  " +
                    "   and a.koef >= b.koef      " + //интервал b. урезаем (коэфициент почасовой)
                    "   and b.cnts > b.cnts_del  " + //если есть, что урезать
                    "   and a.cnts > a.cnts_del  " + //если этот интервал тоже еще жив
                    //выбрать ближайший интервал с минимальным b.koef < a.koef
                    "   and b.nzp_ndx = ( Select max(c.nzp_ndx) From ttt_aid_int c " +
                    " Where b.nzp_kvar = c.nzp_kvar " +
                    "   and b.nzp_serv = c.nzp_serv " +
                    //"   and b.nzp_ndx <> c.nzp_ndx  "+
                    "   and b.dat_s   <= c.dat_po " + //пересекающиеся интервалы
                    "   and b.dat_po  >= c.dat_s  " +
                    "   and a.dat_s   <= c.dat_po " + //пересекающиеся интервалы
                    "   and a.dat_po  >= c.dat_s  " +
                    "   and c.koef    > 0 " +
                    "   and c.koef = ( Select min(f.koef) From ttt_aid_int f " +
                    " Where b.nzp_kvar = f.nzp_kvar " +
                    "   and b.nzp_serv = f.nzp_serv " +
                    //"   and b.nzp_ndx <> f.nzp_ndx  "+
                    "   and b.dat_s   <= f.dat_po " + //пересекающиеся интервалы
                    "   and b.dat_po  >= f.dat_s  " +
                    "   and a.dat_s   <= f.dat_po " + //пересекающиеся интервалы
                    "   and a.dat_po  >= f.dat_s  " +
                    "   and f.koef    <= a.koef " +
                    "   and f.koef    > 0 " +
                    " ) " +
                    " ) "
                    , true);
                if (!ret.result) return ret;

                var b = DBManager.ExecScalar<bool>(conn_db,
                    " Select count(1)>0 From ttt_aid_del Where cnt_del2 > " + s0hour, // "0 units hour "
                    out ret, true);
                if (!ret.result) return ret;

                if (b)
                {
                    ExecSQL(conn_db, " Create index ix_aid_del_0 on ttt_aid_del (nzp_ndx) ", true);
                    ExecSQL(conn_db, " Create index ix_aid_del_1 on ttt_aid_del (nzp_pere) ", true);
                    ExecSQL(conn_db, " Create index ix_aid_del_2 on ttt_aid_del (a_nzp_pere) ", true);
                    ExecSQL(conn_db, sUpdStat + " ttt_aid_del ", true);

                    ExecSQL(conn_db, " Drop table ttt_work_zap ", false);

                    //надо определить work_zap
                    //выбираем max(a_koef) - коэфициент сильного интервала
                    //логика: сначало отработаем пары с сильным интервалом, которая покрывает остальных 
                    string ssql =
                        " Select " + sUniqueWord + " nzp_pere, a_nzp_pere, a_koef, cnt_del2 " +
#if PG
 " Into temp ttt_work_zap From ttt_aid_del ";
#else
                        " From ttt_aid_del Into temp ttt_work_zap With no log ";
#endif
                    ret = ExecSQL(conn_db, ssql, true);
                    if (!ret.result) return ret;

                    ExecSQL(conn_db, " Create index ix_wzap_111 on ttt_work_zap (nzp_pere) ", true);
                    ExecSQL(conn_db, sUpdStat + " ttt_work_zap ", true);

                    ret = ExecSQL(conn_db,
                        " Update ttt_aid_del " +
                        " Set work_zap = 1 " +
                        " Where a_nzp_pere = ( Select max(a_nzp_pere) From ttt_work_zap w " +
                        " Where ttt_aid_del.nzp_pere = w.nzp_pere and w.cnt_del2 >" + s0hour + //" 0 units hour "
                        "   and a_koef = ( Select max(a_koef) From ttt_work_zap w2 " +
                        " Where ttt_aid_del.nzp_pere = w2.nzp_pere " +
                        "   and w2.cnt_del2 >" + s0hour + //" 0 units hour "
                        ")" +
                        ")"
                        , true);
                    if (!ret.result) return ret;


                    ExecSQL(conn_db, " Drop table ttt_work_zap ", false);

                    ret = ExecSQL(conn_db,
                        " Create temp table ttt_work_zap " +
                        " (  nzp_pere    integer       not null, " +
#if PG
 "    cnts        interval      hour, " +
                        "    cnts_del    interval      hour  " +
                        " ) "
#else
                        "    cnts        interval      hour(3) to hour, " +
                        "    cnts_del    interval      hour(6) to hour  " +
                        " ) With no log "
#endif
, true);
                    if (!ret.result) return ret;

                    ret = ExecSQL(conn_db,
                        " Insert into ttt_work_zap (nzp_pere, cnts, cnts_del ) " +
                        " Select nzp_pere, max(cnts), sum(cnt_del2) From ttt_aid_del " +
                        " Where work_zap = 1 " +
                        " Group by 1 "
                        , true);
                    if (!ret.result) return ret;

                    ExecSQL(conn_db, " Create index ix_wrkzp_0 on ttt_work_zap (nzp_pere) ", true);
                    ExecSQL(conn_db, sUpdStat + " ttt_work_zap ", true);

                    //ограничить
                    ret = ExecSQL(conn_db,
                        " Update ttt_work_zap Set cnts_del = cnts Where cnts_del > cnts "
                        , true);
                    if (!ret.result) return ret;

                    ret = ExecSQL(conn_db,
                        " Update ttt_aid_int " +
                        " Set cnts_del = cnts_del + ( Select max(cnts_del) From ttt_work_zap Where ttt_aid_int.nzp_pere = ttt_work_zap.nzp_pere  ) " +
                        " Where EXISTS (SELECT 1 From ttt_work_zap Where ttt_aid_int.nzp_pere = ttt_work_zap.nzp_pere ) "
                        , true);
                    if (!ret.result) return ret;

                    ExecSQL(conn_db, " Drop table ttt_work_zap ", false);


                    //корректируем урезанный интервал b.
                    //1. урезать даты
                    //       [-----------]   a.
                    //  [-----------]        b.1
                    //         [-----------] b.2
                    //         [-------]     b.3
                    ret = ExecSQL(conn_db,
                        " Update ttt_aid_int " + //1
                        " Set (dat_s,dat_po) = " +
#if PG
 " (( Select max(b_dat_s)" +
                        " From ttt_aid_del Where ttt_aid_int.nzp_pere = ttt_aid_del.nzp_pere and work_zap=1 " +
                        " ), " +
                        "( Select max(a_dat_s -" +
                        " interval '1 hour') " +
                        " From ttt_aid_del Where ttt_aid_int.nzp_pere = ttt_aid_del.nzp_pere and work_zap=1 " +
                        " )) " +
#else
                                             " (( Select max(b_dat_s), max(a_dat_s -" +
                                               " 1 units hour) " +
                                               " From ttt_aid_del Where ttt_aid_int.nzp_pere = ttt_aid_del.nzp_pere and work_zap=1 " +
                                             " )) " +
#endif
 " Where EXISTS (SELECT 1 From ttt_aid_del " +
                        " Where ttt_aid_int.nzp_pere = ttt_aid_del.nzp_pere " +
                        "   and b_dat_s <= a_dat_s and b_dat_po < a_dat_po and work_zap=1 ) "
                        , true);
                    if (!ret.result) return ret;

                    ret = ExecSQL(conn_db,
                        " Update ttt_aid_int " + //2
                        " Set (dat_s,dat_po) = " +
#if PG
 " (( Select max(a_dat_po + interval '1 hour') " +
                        "  From ttt_aid_del Where ttt_aid_int.nzp_pere = ttt_aid_del.nzp_pere and work_zap=1 " +
                        " ), " +
                        "( Select max(b_dat_po) " +
                        "  From ttt_aid_del Where ttt_aid_int.nzp_pere = ttt_aid_del.nzp_pere and work_zap=1 " +
                        " )) " +
#else
                                             " (( Select max(a_dat_po + 1 units hour),max(b_dat_po) " +
                                               "  From ttt_aid_del Where ttt_aid_int.nzp_pere = ttt_aid_del.nzp_pere and work_zap=1 " +
                                             " )) " +
#endif
 " Where EXISTS (SELECT 1 From ttt_aid_del " +
                        " Where ttt_aid_int.nzp_pere = ttt_aid_del.nzp_pere " +
                        "   and b_dat_s >= a_dat_s and b_dat_po > a_dat_po and work_zap=1 ) "
                        , true);
                    if (!ret.result) return ret;

                    ret = ExecSQL(conn_db,
                        " Update ttt_aid_int " + //3
                        " Set cnts_del = cnts " +
                        " Where EXISTS (SELECT 1 From ttt_aid_del " +
                        " Where ttt_aid_int.nzp_pere = ttt_aid_del.nzp_pere " +
                        "   and b_dat_s >= a_dat_s and b_dat_po <= a_dat_po and work_zap=1 ) "
                        , true);
                    if (!ret.result) return ret;

                    //2. породить новые крайние интервалы
                    //       [---------]     a.
                    //  [-----------------]  b.
                    ExecSQL(conn_db, " Drop table ttt_aid_pro ", false);

                    ret = ExecSQL(conn_db,
                        " Update ttt_aid_int " +
                        " Set (dat_s, dat_po) =" +
#if PG
 " (( Select max(b_dat_s) " +
                        " From ttt_aid_del " +
                        " Where ttt_aid_int.nzp_pere = ttt_aid_del.nzp_pere " +
                        "   and b_dat_s < a_dat_s and b_dat_po > a_dat_po and work_zap=1 " +
                        " ), " +
                        "( Select max(a_dat_s - interval '1 hour') " +
                        " From ttt_aid_del " +
                        " Where ttt_aid_int.nzp_pere = ttt_aid_del.nzp_pere " +
                        "   and b_dat_s < a_dat_s and b_dat_po > a_dat_po and work_zap=1 " +
                        " )) " +
#else
                                              " (( Select max(b_dat_s), max(a_dat_s - 1 units hour) " +
                                                 " From ttt_aid_del " +
                                                 " Where ttt_aid_int.nzp_pere = ttt_aid_del.nzp_pere " +
                                                 "   and b_dat_s < a_dat_s and b_dat_po > a_dat_po and work_zap=1 " +
                                              " )) " +
#endif
 " Where EXISTS (SELECT 1 From ttt_aid_del " +
                        " Where ttt_aid_int.nzp_pere = ttt_aid_del.nzp_pere " +
                        "   and b_dat_s < a_dat_s and b_dat_po > a_dat_po and work_zap=1 ) "
                        , true);
                    if (!ret.result) return ret;

                    ret = ExecSQL(conn_db,
#if PG
 " Select distinct nzp_ndx,nzp_kvar,nzp_serv, a_dat_po + interval '1 hour' as dat_s, b_dat_po as dat_po, " +
                        " b_dat_po - (a_dat_s + interval '1 hour') as cnts, b_dat_po - b_dat_po as cnts_del, koef " +
                        " Into temp ttt_aid_pro  " +
                        " From ttt_aid_del " +
                        " Where b_dat_s < a_dat_s and b_dat_po > a_dat_po and work_zap=1 "
#else
                        " Select unique nzp_ndx,nzp_kvar,nzp_serv, a_dat_po + 1 units hour as dat_s, b_dat_po as dat_po, " +
                               " b_dat_po - (a_dat_s + 1 units hour) as cnts, b_dat_po - b_dat_po as cnts_del, koef " +
                        " From ttt_aid_del " +
                        " Where b_dat_s < a_dat_s and b_dat_po > a_dat_po and work_zap=1 " +
                        " Into temp ttt_aid_pro With no log "
#endif
, true);
                    if (!ret.result) return ret;

                    ret = ExecSQL(conn_db,
                        " Insert into ttt_aid_int (nzp_ndx,nzp_kvar,nzp_serv, dat_s,dat_po, cnts,cnts_del, koef) " +
                        " Select nzp_ndx,nzp_kvar,nzp_serv, dat_s,dat_po, cnts,cnts_del, koef " +
                        " From ttt_aid_pro "
                        , true);
                    if (!ret.result) return ret;

                    ExecSQL(conn_db, " Drop table ttt_aid_pro ", false);
                }

                ExecSQL(conn_db, " Drop table ttt_aid_del ", false);
                if (!b) break;
            }
            return ret;
        }

        /// <summary>
        /// Расчет температурной недопоставки по отоплению
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="nedoXX"></param>
        /// <param name="calcNedoXx"></param>
        /// <returns></returns>
        private Returns CalcTemperatureNedoForHeating(IDbConnection conn_db, NedoXX nedoXX)
        {
            var ret = Utils.InitReturns();
            var tableAnalyzePeriods = "temp_anl_perd_" + DateTime.Now.Ticks;
            var countNightHours = "temp_n_" + DateTime.Now.Ticks;
            var tableCountHours = "temp_n_d_hours_" + DateTime.Now.Ticks;
            var tableKoefNedo = "temp_koef_nedo_" + DateTime.Now.Ticks;

            #region анализируем кол-во ночных и дневных часов в периодах
            try
            {
                var sql = " CREATE TEMP TABLE " + tableAnalyzePeriods + " AS " +
                          " SELECT nzp_ndx, EXTRACT(EPOCH FROM dat_po - dat_s)/3600::integer AS sum_hours," +
                    //кол-во часов в периоде
                          " EXTRACT('hour' FROM dat_s) as minus_hour," + //кол-во вычитаемых часов 
                    //кол-во прибавочных часов (только если это не один день)
                          " CASE WHEN dat_s::DATE<>dat_po::DATE " +
                          "      THEN ( CASE " +
                          "             WHEN EXTRACT('hour' FROM dat_po)=0 " +
                          "             THEN 24 ELSE EXTRACT('hour' FROM dat_po) " +
                          "             END) " +
                          "      ELSE 0 " +
                          " END as plus_hour," +
                          " 5 * GREATEST((EXTRACT('day' FROM dat_po-dat_s)::integer),1) as night_hours, " +
                    //кол-во ночных часов полученных из округленного кол-ва дней 
                          " 19 * GREATEST((EXTRACT('day' FROM dat_po-dat_s)::integer),1) as day_hours" +
                    //кол-во дневных часов  полученных из округленного кол-ва дней
                          " FROM " + nedoXX.TempTableNedops +
                          " WHERE " +
                          " kod_info  = 1 " +
                          " AND cur_zap <> -1 " +
                          " AND nzp_kind in (6,71) ";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return ret;


                sql = " CREATE TEMP TABLE " + countNightHours + " AS " +
                      " SELECT nzp_ndx," +
                      " sum_hours,night_hours - LEAST(minus_hour,5) + LEAST(plus_hour,5) as real_night_hours" +//cчитаем реальное кол-во ночных часов в периоде
                      " FROM  " + tableAnalyzePeriods;
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return ret;

                ret = ExecSQL(conn_db,
                   " CREATE UNIQUE INDEX ix1" + countNightHours + " ON " + countNightHours + " (nzp_ndx)", true);
                if (!ret.result) return ret;

                #region угловые квартиры
                //температурный норматив в угловых квартирах 
                var tempNorm = "(CASE WHEN n.nzp_kind = 6 THEN 20 ELSE 22 END)";

                //ночные часы - ниже нормы не более 3 градусов, выше - не более 4
                sql = " CREATE TEMP TABLE " + tableCountHours + " AS " +
                      " SELECT n.nzp_ndx, " +
                      " " + tempNorm + "+4 as tn_max," + tempNorm + "-3 as tn_min, n.tn," +
                      " n.perc, cnt.real_night_hours as hour, true as is_night , sum_hours, n.cnts_del   " +
                      " FROM  " + countNightHours + " cnt, " + nedoXX.TempTableNedops + " n " +
                      " WHERE cnt.nzp_ndx=n.nzp_ndx " +
                      " AND EXISTS ( SELECT 1 FROM ttt_prm_1 p " + //угловая квартира
                      "              WHERE n.nzp_kvar = p.nzp " +
                      "              AND p.nzp_prm = 310 " +
                      "              AND p.val_prm = '1' ) ";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return ret;

                //дневные часы - ниже нормы - не допускается, выше - не более 4
                sql = "INSERT INTO " + tableCountHours + " (nzp_ndx,tn_max,tn_min,tn,perc,hour,is_night,sum_hours,cnts_del)" +
                      " SELECT n.nzp_ndx, " +
                      " " + tempNorm + "+4 as tn_max," + tempNorm + " as tn_min, n.tn," +
                      " n.perc, (sum_hours - cnt.real_night_hours) as hour, false as is_night, sum_hours, n.cnts_del   " +
                      " FROM  " + countNightHours + " cnt, " + nedoXX.TempTableNedops + " n " +
                      " WHERE cnt.nzp_ndx=n.nzp_ndx " +
                      " AND EXISTS ( SELECT 1 FROM ttt_prm_1 p " + //угловая квартира
                      "              WHERE n.nzp_kvar = p.nzp " +
                      "              AND p.nzp_prm = 310 " +
                      "              AND p.val_prm = '1' ) ";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return ret;
                #endregion угловые квартиры

                ret = ExecSQL(conn_db, " CREATE UNIQUE INDEX ix1" + tableCountHours + " ON " + tableCountHours + " (nzp_ndx,is_night)", true);
                if (!ret.result) return ret;

                #region остальные квартиры
                //температурный норматив в угловых квартирах 
                tempNorm = "(CASE WHEN n.nzp_kind = 6 THEN 18 ELSE 20 END)";

                //ночные часы - ниже нормы не более 3 градусов, выше - не более 4
                sql = " INSERT INTO " + tableCountHours + " (nzp_ndx,tn_max,tn_min,tn,perc,hour,is_night,sum_hours,cnts_del)" +
                      " SELECT n.nzp_ndx, " +
                      " " + tempNorm + "+4 as tn_max," + tempNorm + "-3 as tn_min, n.tn," +
                      " n.perc, cnt.real_night_hours as hour, true as is_night , sum_hours, n.cnts_del   " +
                      " FROM  " + countNightHours + " cnt, " + nedoXX.TempTableNedops + " n " +
                      " WHERE cnt.nzp_ndx=n.nzp_ndx " +
                      " AND NOT EXISTS ( SELECT 1 FROM " + tableCountHours + " ex " + //НЕ угловая квартира
                      "                  WHERE n.nzp_ndx = ex.nzp_ndx AND ex.is_night=TRUE) ";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return ret;

                //дневные часы - ниже нормы - не допускается, выше - не более 4
                sql = " INSERT INTO " + tableCountHours + " (nzp_ndx,tn_max,tn_min,tn,perc,hour,is_night,sum_hours,cnts_del)" +
                      " SELECT n.nzp_ndx, " +
                      " " + tempNorm + "+4 as tn_max," + tempNorm + " as tn_min, n.tn," +
                      " n.perc, (sum_hours - cnt.real_night_hours) as hour, false as is_night, sum_hours, n.cnts_del   " +
                      " FROM  " + countNightHours + " cnt, " + nedoXX.TempTableNedops + " n " +
                      " WHERE cnt.nzp_ndx=n.nzp_ndx " +
                      " AND NOT EXISTS ( SELECT 1 FROM  " + tableCountHours + " ex " + //НЕ угловая квартира
                      "                  WHERE n.nzp_ndx = ex.nzp_ndx AND ex.is_night=FALSE ) ";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return ret;
                #endregion остальные квартиры

            #endregion анализируем кол-во ночных и дневных часов в периодах


                //уберем perc в интервале отклонения (не считаем их часами недопоставки)
                ExecByStep(conn_db, tableCountHours, "nzp_ndx",
                    " UPDATE " + tableCountHours +
                    " SET perc = 0 " +
                    " WHERE tn >= tn_min and tn<=tn_max  "
                    , 100000, "", out ret);
                if (!ret.result) return ret;

                //увеличиваем процент за каждый градус отклонения в меньшую сторону
                ret = ExecSQL(conn_db,
                    " UPDATE " + tableCountHours +
                    " SET perc = perc * (tn_min - tn) " + //step=1 градус
                    " WHERE tn < tn_min   ", true);
                if (!ret.result) return ret;

                //увеличиваем процент за каждый градус отклонения в большую сторону
                ret = ExecSQL(conn_db,
                    " UPDATE " + tableCountHours +
                    " SET perc = perc * (tn - tn_max) " + //step=1 градус
                    " WHERE tn > tn_max   ", true);
                if (!ret.result) return ret;

                //вычитаем часы, которые не считаются часами недопоставки 
                sql = " UPDATE " + tableCountHours +
                      " SET cnts_del = hour * interval '1 hour' " +
                      " WHERE perc=0";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return ret;

                //получаем сборный коэф. недопоставки
                //потому что в ночное и дневное время могут быть разные проценты недопоставок
                sql = " CREATE TEMP TABLE " + tableKoefNedo + " AS " +
                      " SELECT nzp_ndx, SUM((hour * perc) / sum_hours) as koef" +
                      " FROM " + tableCountHours +
                      " GROUP BY 1";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return ret;

                //проставляе сборный коэф.
                sql = " UPDATE " + nedoXX.TempTableNedops + " n" +
                      " SET koef= k.koef/100, perc = k.koef " +
                      " FROM " + tableKoefNedo + " k " +
                      " WHERE k.nzp_ndx=n.nzp_ndx";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return ret;
                
                return ret;
            }
            finally
            {
                ExecSQL(conn_db, "DROP TABLE " + tableAnalyzePeriods, false);
                ExecSQL(conn_db, "DROP TABLE " + countNightHours, false);
                ExecSQL(conn_db, "DROP TABLE " + tableCountHours, false);
                ExecSQL(conn_db, "DROP TABLE " + tableKoefNedo, false);
            }
        }

        /// <summary>
        /// Расчет температурной недопоставки для Горячей воды
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="paramcalc"></param>
        /// <param name="nedoXX"></param>
        /// <param name="month_hours"></param>
        /// <returns></returns>
        private Returns CalcTemperatureNedoForHotWater(IDbConnection conn_db, NedoXX nedoXX, string month_hours)
        {
            var ret = Utils.InitReturns();
            var tableAnalyzePeriods = "temp_anl_perd_" + DateTime.Now.Ticks;
            var countNightHours = "temp_n_" + DateTime.Now.Ticks;
            var tableCountHours = "temp_n_d_hours_" + DateTime.Now.Ticks;
            var tableMaxPercents = "temp_max_perc_" + DateTime.Now.Ticks;
            try
            {
                //если tn < 40, то 100%
                ExecByStep(conn_db, nedoXX.TempTableNedops, "nzp_ndx",
                    " Update " + nedoXX.TempTableNedops +
                    " Set perc = 100, " +
                    " koef = 1.00 / " + month_hours +
                    " Where " + nedoXX.where_z + nedoXX.paramcalc.per_dat_charge +
                    "   and kod_info  = 1 " +
                    "   and cur_zap <> -1 " +
                    "   and nzp_kind = 9  " +
                    "   and tn < 40 "
                    , 100000, "", out ret);
                if (!ret.result)
                {
                    return ret;
                }
                //определим нормативы [tn_min,tn_max]
                ExecByStep(conn_db, nedoXX.TempTableNedops, "nzp_ndx",
                    " Update " + nedoXX.TempTableNedops +
                    " Set tn_max = 75, tn_min = 60 " +
                    " Where " + nedoXX.where_z + nedoXX.paramcalc.per_dat_charge +
                    "   and kod_info  = 1 " +
                    "   and cur_zap <> -1 " +
                    "   and nzp_kind = 9  " +
                    "   and tn > 39 "
                    , 100000, "", out ret);
                if (!ret.result)
                {
                    return ret;
                }

                #region анализируем кол-во ночных и дневных часов в периодах


                var sql = " CREATE TEMP TABLE " + tableAnalyzePeriods + " AS " +
                          " SELECT nzp_ndx, EXTRACT(EPOCH FROM dat_po - dat_s)/3600::integer AS sum_hours," + //кол-во часов в периоде
                          " EXTRACT('hour' FROM dat_s) as minus_hour," + //кол-во вычитаемых часов 
                    //кол-во прибавочных часов (только если это не один день)
                          " CASE WHEN dat_s::DATE<>dat_po::DATE " +
                          "      THEN ( CASE " +
                          "             WHEN EXTRACT('hour' FROM dat_po)=0 " +
                          "             THEN 24 ELSE EXTRACT('hour' FROM dat_po) " +
                          "             END) " +
                          "      ELSE 0 " +
                          " END as plus_hour," +
                          " 5 * GREATEST((EXTRACT('day' FROM dat_po-dat_s)::integer),1) as night_hours, " +
                    //кол-во ночных часов полученных из округленного кол-ва дней 
                          " 19 * GREATEST((EXTRACT('day' FROM dat_po-dat_s)::integer),1) as day_hours" +
                    //кол-во дневных часов  полученных из округленного кол-ва дней
                          " FROM " + nedoXX.TempTableNedops +
                          " WHERE " +
                          " kod_info  = 1 " +
                          " AND cur_zap <> -1 " +
                          " AND nzp_kind = 9  " +
                          " AND tn > 39 ";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return ret;


                sql = " CREATE TEMP TABLE " + countNightHours + " AS " +
                      " SELECT nzp_ndx," +
                      " sum_hours,night_hours - LEAST(minus_hour,5) + LEAST(plus_hour,5) as real_night_hours" +//cчитаем реальное кол-во ночных часов в периоде
                      " FROM  " + tableAnalyzePeriods;
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return ret;

                ret = ExecSQL(conn_db,
                   " CREATE UNIQUE INDEX ix1" + countNightHours + " ON " + countNightHours + " (nzp_ndx)", true);
                if (!ret.result) return ret;

                //кол-во ночных часов
                sql = " CREATE TEMP TABLE " + tableCountHours + " AS " +
                      " SELECT n.nzp_ndx, n.tn_max+ 5 as tn_max_dop,n.tn_min- 5 as tn_min_dop," +//расширим [tn_min_dop,tn_max_dop] на допустимые отклонения
                      " n.tn_max,n.tn_min, n.tn, n.perc, cnt.real_night_hours as hour, true as is_night , sum_hours   " +
                      " FROM  " + countNightHours + " cnt, " + nedoXX.TempTableNedops + " n " +
                      " WHERE cnt.nzp_ndx=n.nzp_ndx";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return ret;

                //кол-во дневных часов
                sql = "INSERT INTO " + tableCountHours + " (nzp_ndx,tn_max_dop,tn_min_dop,tn,tn_max,tn_min,perc,hour,is_night,sum_hours)" +
                      " SELECT n.nzp_ndx, n.tn_max+ 3 as tn_max_dop,n.tn_min- 3 as tn_min_dop, n.tn," +//расширим [tn_min_dop,tn_max_dop] на допустимые отклонения
                      " n.tn_max,n.tn_min, n.perc, (sum_hours - cnt.real_night_hours) as hour, false as is_night, sum_hours   " +
                      " FROM  " + countNightHours + " cnt, " + nedoXX.TempTableNedops + " n " +
                      " WHERE cnt.nzp_ndx=n.nzp_ndx";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return ret;

                ret = ExecSQL(conn_db,
                " CREATE UNIQUE INDEX ix1" + tableCountHours + " ON " + tableCountHours + " (nzp_ndx,is_night)", true);
                if (!ret.result) return ret;

                #endregion анализируем кол-во ночных и дневных часов в периодах


                //уберем perc в интервале отклонения (не считаем их часами недопоставки)
                ExecByStep(conn_db, tableCountHours, "nzp_ndx",
                    " Update " + tableCountHours +
                    " Set perc = 0 " +
                    " Where tn >= tn_min_dop and tn<=tn_max_dop  "
                    , 100000, "", out ret);
                if (!ret.result)
                {
                    return ret;
                }

                //температура ниже нормы
                ExecByStep(conn_db, tableCountHours, "nzp_ndx",
                    " Update " + tableCountHours +
                    " Set perc = perc * round ((tn_min::NUMERIC - tn::NUMERIC)/3)  " + //за каждые 3 градуса отклонения от нормы снимаем 1/10 процента
                    " Where  tn < tn_min_dop   "
                    , 100000, "", out ret);
                if (!ret.result)
                {
                    return ret;
                }

                //температура выше нормы
                ExecByStep(conn_db, tableCountHours, "nzp_ndx",
                    " Update " + tableCountHours +
                    " Set perc = perc * round ((tn::NUMERIC - tn_max::NUMERIC)/ 3)  " +//за каждые 3 градуса отклонения от нормы снимаем 1/10 процента
                    " Where  tn > tn_max_dop   "
                    , 100000, "", out ret);
                if (!ret.result)
                {
                    return ret;
                }

                sql = " CREATE TEMP TABLE " + tableMaxPercents + " AS " +
                      " SELECT nzp_ndx, MAX(perc) perc " + //берем максимальный процент недопоставки за час
                      " FROM " + tableCountHours + " " +
                      " GROUP BY 1";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return ret;


                ret = ExecSQL(conn_db,
                " CREATE UNIQUE INDEX ix1" + tableMaxPercents + " ON " + tableMaxPercents + " (nzp_ndx)", true);
                if (!ret.result) return ret;


                ExecByStep(conn_db, nedoXX.TempTableNedops, "nzp_ndx",
                    " UPDATE " + nedoXX.TempTableNedops + " n " +
                    " SET perc = s.perc, koef = s.perc/100.00 " +
                    " FROM  " + tableMaxPercents + " s " +
                    " WHERE s.nzp_ndx=n.nzp_ndx"
                    , 100000, "", out ret);
                if (!ret.result) return ret;

                //вычитаем часы, которые не считаются часами недопоставки 
                sql = " UPDATE " + nedoXX.TempTableNedops + " n " +
                      " SET cnts_del = hour * interval '1 hour' " +
                      " FROM " + tableCountHours + " h " +
                      " WHERE n.nzp_ndx=h.nzp_ndx AND h.perc=0";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return ret;

                return ret;
            }
            finally
            {
                ExecSQL(conn_db, "DROP TABLE " + tableAnalyzePeriods, false);
                ExecSQL(conn_db, "DROP TABLE " + countNightHours, false);
                ExecSQL(conn_db, "DROP TABLE " + tableCountHours, false);
                ExecSQL(conn_db, "DROP TABLE " + tableMaxPercents, false);
            }
        }
    }
}
