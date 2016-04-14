#region Здесь производится заполнение таблиц Calc_gku_XX
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
    #region Полный цикл от выборки тарифов и расходов , до сохранения в БД
    public partial class DbCalcCharge : DataBaseHead
    {
        #region Пустышка запуска подсчета расходов
        //-----------------------------------------------------------------------------
        //Расчет gku.calc_gku_xx -> вызов Ctrl+9
        //-----------------------------------------------------------------------------
        public void CalcGkuXX_Run(out Returns ret)
        //-----------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
        }
        #endregion Пустышка запуска подсчета расходов

        #region Структура для работы с таблицами calc_gku
        public struct Gku
        {
            public CalcTypes.ParamCalc paramcalc;
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
                        s = MDY(1, 1, paramcalc.calc_yy + 1);
                    else
                        s = MDY(paramcalc.calc_mm + 1, 1, paramcalc.calc_yy);
                    return s;
                }
            }
            public string calc_charge_bd;
            public string cur_charge_bd;
            public string calc_gku_tab;
            public string calc_gku_xx;
            public string counters_xx;
            public string gil_xx;
            public string perekidka_xx;
            public string prevSaldoMon_charge;
            public string calc_tosupplXX;
            public string calc_lnkchargeXX;
            public string curSaldoMon_charge;
            public string reval_xx;

            #region Заполнялка структуры gku
            public Gku(CalcTypes.ParamCalc _paramcalc)
            {
                paramcalc = _paramcalc;
                paramcalc.b_dom_in = true;
                calc_charge_bd = paramcalc.pref + "_charge_" + (paramcalc.calc_yy - 2000).ToString("00");
                cur_charge_bd = paramcalc.pref + "_charge_" + (paramcalc.cur_yy - 2000).ToString("00");
                calc_gku_tab = "calc_gku" + paramcalc.alias + "_" + paramcalc.calc_mm.ToString("00");
                calc_gku_xx = calc_charge_bd + tableDelimiter + calc_gku_tab;
                counters_xx = calc_charge_bd + tableDelimiter + "counters" + paramcalc.alias + "_" + paramcalc.calc_mm.ToString("00");
                gil_xx = calc_charge_bd + tableDelimiter + "gil" + paramcalc.alias + "_" + paramcalc.calc_mm.ToString("00");
                perekidka_xx = cur_charge_bd + tableDelimiter + "perekidka";
                prevSaldoMon_charge = paramcalc.pref + "_charge_" + (paramcalc.prev_calc_yy - 2000).ToString("00") + tableDelimiter + "charge_" + paramcalc.prev_calc_mm.ToString("00");
                curSaldoMon_charge = paramcalc.pref + "_charge_" + (paramcalc.calc_yy - 2000).ToString("00") + tableDelimiter + "charge_" + paramcalc.calc_mm.ToString("00");
                calc_tosupplXX = paramcalc.pref + "_charge_" + (paramcalc.calc_yy - 2000).ToString("00") + tableDelimiter + "to_supplier" + paramcalc.calc_mm.ToString("00");
                calc_lnkchargeXX = paramcalc.pref + "_charge_" + (paramcalc.calc_yy - 2000).ToString("00") + tableDelimiter + "lnk_charge_" + paramcalc.calc_mm.ToString("00");
                reval_xx = paramcalc.pref + "_charge_" + (paramcalc.cur_yy - 2000).ToString("00") + tableDelimiter + "reval_" + paramcalc.cur_mm.ToString("00");
            }
            #endregion Заполнялка структуры gku
        }
        #endregion Структура для работы с таблицами calc_gku

        #region Создание таблицы расходов если нет , чистка с шагом 100000 если есть
        //--------------------------------------------------------------------------------
        void CreateGkuXX(IDbConnection conn_db2, Gku gku, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            if (TempTableInWebCashe(conn_db2, gku.calc_gku_xx))
            {
                ExecByStep(conn_db2, gku.calc_gku_xx, "nzp_clc",
                        " Delete From " + gku.calc_gku_xx +
                        " Where nzp_kvar in ( Select nzp_kvar From t_selkvar) " + gku.paramcalc.per_dat_charge
                        , 100000, " ", out ret);

                UpdateStatistics(false, gku.paramcalc, gku.calc_gku_tab, out ret);
                return;
            }



            string conn_kernel = Points.GetConnByPref(gku.paramcalc.pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            //IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                return;
            }
#if PG
            ret = ExecSQL(conn_db, "  set search_path to '" + gku.calc_charge_bd + "'", true);
#else
            ret = ExecSQL(conn_db, " Database " + gku.calc_charge_bd, true);
#endif

            ret = ExecSQL(conn_db,
                " Create table " + tbluser + gku.calc_gku_tab +
                " (  nzp_clc        serial        not null, " + // сериал
                "    nzp_dom        integer       not null, " + // код дома
                "    nzp_kvar       integer       not null, " + // код ЛС
                "    nzp_serv       integer       not null, " + // код услуги
                "    nzp_supp       integer       not null, " + // код договора/поставщика
                "    nzp_frm        integer       default 0 not null, " + // код формула расчета
                "    dat_charge     date, " + // месяц/год перерасчета или NULL для тек. расч.месяца
                "    nzp_prm_tarif  integer       default 0 not null, " + // код параметра тарифа
                "    nzp_prm_rashod integer       default 0 not null, " + // код параметра расхода
                "    tarif          " + sDecimalType + "(17,7) default 0.00 not null, " + // тариф в руб.
                "    rashod         " + sDecimalType + "(14,7) default 0.00 not null, " + // расход (ед.изм. по формуле расчета) м.б. с ОДН
                "    rashod_norm    " + sDecimalType + "(14,7) default 0.0000000 NOT NULL," +   // расход по нормативу
                "    rashod_source    " + sDecimalType + "(14,7) default 0.0000000 NOT NULL," +   // расход по нормативу без учета повыщающего коэф-та
                "    rashod_g       " + sDecimalType + "(14,7) default 0.0000000 NOT NULL," +   // расход лс без учета вр.выбывших
                "    gil            " + sDecimalType + "(14,7) default 0.00         , " +       // кол-во жильцов в лс
                "    gil_g          " + sDecimalType + "(14,7) default 0.00         , " +       // кол-во жильцов в лс без учета вр.выбывших
                "    squ            " + sDecimalType + "(14,7) default 0.00         , " +       // площадь лс
                "    trf1           " + sDecimalType + "(14,4) default 0.00 not null, " +       // тариф в ГКал
                "    trf2           " + sDecimalType + "(14,4) default 0.00 not null, " +       // норматив в ГКал на ед.изм. (на дом)
                "    trf3           " + sDecimalType + "(14,4) default 0.00 not null, " +       // повышающий коэф-т для норматива в ГКал ГВС
                "    trf4           " + sDecimalType + "(14,4) default 0.00 not null, " +       // процент тарифа для населения от ЭОТ тарифа
                // тип расхода = 11,1814 - перевод отопление из м2 в ГКал:
                // rsh1 - площадь ЛС
                // rsh2 - расход ГКал на 1 кв.м
                "    rsh1           " + sDecimalType + "(14,7) default 0.00 not null, " +       // для отопления - норма расхода в ГКал на 1 кв.м / для ГВС - расход в м3
                "    rsh2           " + sDecimalType + "(14,7) default 0.00 not null, " +       // для отопления - расход в ГКал                  / для ГВС - расход в ГКал на нагрев 1 м3
                "    rsh3           " + sDecimalType + "(14,7) default 0.00 not null, " +       // для отопления - неотапливаемая площадь
                "    nzp_prm_trf_dt integer       default 0    not null, " +                    // код параметра тарифа для населения при расчете дотаций
                "    tarif_f        " + sDecimalType + "(17,7) default 0.00 not null, " +       // тариф в руб. для населения при расчете дотаций
                "    rash_norm_one  " + sDecimalType + "(14,7) default 0.0000 NOT NULL, " +     // нормативный расход на 1 ед.изм.
                "    valm           " + sDecimalType + "(15,7) default 0.0000 NOT NULL, " +     // расход без ОДН
                "    dop87          " + sDecimalType + "(15,7) default 0.0000 NOT NULL, " +     // расход на ОДН
                "    dlt_reval      " + sDecimalType + "(14,7) default 0.0000 NOT NULL, " +     // перерасчитанный в прошлых периодах расход 
                "    is_device      integer default 0 NOT NULL, " +     // признак ИПУ: =1 - ИПУ, =9- среднее ИПУ, =0- норматив
                "    nzp_frm_typ    integer       default 0 not null, " +     // тип расчета тарифа
                "    nzp_frm_typrs  integer       default 0 not null, " +     // тип расчета расхода
                "    rashod_full    " + sDecimalType + "(15,7) default 0.00 not null, " +
                "    stek   integer default 3 not null,  " +
                "    dat_s  date, " +
                "    dat_po date, " +
                "    up_kf    " + sDecimalType + "(14,7) default 1 NOT NULL," +   // повышающий коэф-т
                "    cntd    integer default 0 not null,  " +
                "    cntd_mn integer default 0 not null ) "
                , true);
            if (!ret.result) { return; }

            ret = ExecSQL(conn_db, " create unique index " + tbluser + "ix1_" + gku.calc_gku_tab + " on " + gku.calc_gku_tab + " (nzp_clc)", true);
            if (!ret.result) { return; }
            ret = ExecSQL(conn_db, " create        index " + tbluser + "ix2_" + gku.calc_gku_tab + " on " + gku.calc_gku_tab + " (nzp_kvar,nzp_frm,dat_charge)", true);
            if (!ret.result) { return; }
            ret = ExecSQL(conn_db, " create        index " + tbluser + "ix3_" + gku.calc_gku_tab + " on " + gku.calc_gku_tab + " (nzp_dom)", true);
            if (!ret.result) { return; }
            ret = ExecSQL(conn_db, " create        index " + tbluser + "ix4_" + gku.calc_gku_tab + " on " + gku.calc_gku_tab + " (nzp_kvar,stek,dat_s,dat_po)", true);
            if (!ret.result) { return; }

            ExecSQL(conn_db, sUpdStat + " " + gku.calc_gku_tab, true);

        }
        #endregion Создание таблицы расходов если нет , чистка с шагом 100000 если есть

        /*
        public bool CalcTypeRashod_1056(IDbConnection conn_db, Gku gku, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();


            return true;
        }
        */

        #region Выбрать параметры лицевого счета  на дату t_par1 & t_par2 & t_opn_dom
        public bool SelParamsForCalc(IDbConnection conn_db, Gku gku, bool bIsCalcSubsidy, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            #region Выбрать параметры лицевого счета на дату t_par1
            // выбрать все параметры л/с на дату расчета
            ret = ExecSQL(conn_db,
                " create temp table t_par1 (" +
                " nzp      integer," +
                " nzp_prm  integer," +
                " val      CHAR(20)," +
                " dat_s    DATE," +
                " dat_po   DATE " +
                " )  " + sUnlogTempTable
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " insert into t_par1 (nzp,nzp_prm,val,dat_s,dat_po) " +
                " select p.nzp,p.nzp_prm, " + sNvlWord + "(p.val_prm,'0')," + gku.paramcalc.dat_s + "," + gku.paramcalc.dat_po +
                " from ttt_prm_1 p, t_opn k " +
                " where p.nzp = k.nzp_kvar and k.is_day_calc=0 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " insert into t_par1 (nzp,nzp_prm,val,dat_s,dat_po) " +
                " select p.nzp,p.nzp_prm, " + sNvlWord + "(p.val_prm,'0')," + gku.paramcalc.dat_s + "," + gku.paramcalc.dat_po +
                " from ttt_prm_1 p, t_opn k, " + gku.paramcalc.kernel_alias + "prm_name n" +
                " where p.nzp = k.nzp_kvar and p.nzp_prm=n.nzp_prm and k.is_day_calc=1 and n.is_day_uchet=0 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " insert into t_par1 (nzp,nzp_prm,val,dat_s,dat_po) " +
               " select p.nzp,p.nzp_prm," + sNvlWord + "(p.val_prm,'0'),p.dat_s,p.dat_po " +
                " from ttt_prm_1d p, t_opn k, " + gku.paramcalc.kernel_alias + "prm_name n" +
                " where p.nzp = k.nzp_kvar and p.nzp_prm=n.nzp_prm and k.is_day_calc=1 and n.is_day_uchet=1 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, " create index ix1t_par1 on t_par1(nzp,nzp_prm,dat_s,dat_po) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, " create index ix2t_par1 on t_par1(dat_s,dat_po) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, sUpdStat + " t_par1 ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            #endregion Выбрать параметры лицевого счета  на дату t_par1

            #region Выбрать параметры дома на дату t_par2 & t_opn_dom

            // выбрать все дома
            ret = ExecSQL(conn_db,
                " create temp table t_opn_dom (" +
                " nzp_dom     integer, " +
                " is_day_calc integer " +
                " )  " + sUnlogTempTable
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " insert into t_opn_dom (nzp_dom,is_day_calc) " +
                " select nzp_dom,max(is_day_calc) " +
                " from  t_opn " +
                " group by 1 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }
            ret = ExecSQL(conn_db, " create index ixt_opn_dom on t_opn_dom(nzp_dom,is_day_calc) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, sUpdStat + " t_opn_dom ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }


            // выбрать все параметры дома на дату расчета
            ret = ExecSQL(conn_db,
                " create temp table t_par2 (" +
                " nzp      integer," +
                " nzp_prm  integer," +
                " val_prm  CHAR(20)," +
                " dat_s    DATE," +
                " dat_po   DATE " +
                " )  " + sUnlogTempTable
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " insert into t_par2 (nzp,nzp_prm,val_prm,dat_s,dat_po) " +
                " select p.nzp,p.nzp_prm, p.val_prm," + gku.paramcalc.dat_s + "," + gku.paramcalc.dat_po +
                " from ttt_prm_2 p, t_opn_dom k " +
                " where p.nzp = k.nzp_dom and k.is_day_calc=0 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " insert into t_par2 (nzp,nzp_prm,val_prm,dat_s,dat_po) " +
                " select p.nzp,p.nzp_prm, p.val_prm," + gku.paramcalc.dat_s + "," + gku.paramcalc.dat_po +
                " from ttt_prm_2 p, t_opn_dom k, " + gku.paramcalc.kernel_alias + "prm_name n" +
                " where p.nzp = k.nzp_dom and p.nzp_prm=n.nzp_prm and k.is_day_calc=1 and n.is_day_uchet=0 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " insert into t_par2 (nzp,nzp_prm,val_prm,dat_s,dat_po) " +
                " select p.nzp,p.nzp_prm,p.val_prm,p.dat_s,p.dat_po " +
                " from ttt_prm_2d p, t_opn_dom k, " + gku.paramcalc.kernel_alias + "prm_name n" +
                " where p.nzp = k.nzp_dom and p.nzp_prm=n.nzp_prm and k.is_day_calc=1 and n.is_day_uchet=1 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, " create index ix1t_par2 on t_par2(nzp,nzp_prm,dat_s,dat_po) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, " create index ix2t_par2 on t_par2(dat_s,dat_po) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, sUpdStat + " t_par2 ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            #endregion Выбрать параметры лицевого счета  на дату t_par1 & t_opn_dom & t_par2 & t_opn_dom

            #region выбрать параметры л/с для nzp_prm_tarif_bd на дату расчета - t_par26f

            // выбрать параметры л/с для nzp_prm_tarif_bd на дату расчета
            ret = ExecSQL(conn_db,
                " create temp table t_par26f (" +
                " nzp     integer," +
                " nzp_prm integer" +
                " )  " + sUnlogTempTable
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " insert into t_par26f (nzp,nzp_prm)" +
                " select p.nzp,p.nzp_prm from " + gku.paramcalc.kernel_alias + "formuls_opis f,t_par1 p" +
                " where p.nzp_prm=f.nzp_prm_tarif_bd " +
                " group by 1,2 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, " create index ixt_par26f on t_par26f(nzp,nzp_prm) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, sUpdStat + " t_par26f ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            #endregion выбрать параметры л/с для nzp_prm_tarif_bd на дату расчета - t_par26f

            #region Выборка тарифов для домовых квартирных тарифов t_par1d

            // выбрать все параметры/тарифы л/с          //уточнение которые связаны с формулами 
            ret = ExecSQL(conn_db,
                " create temp table t_par1d (" +
                " nzp      integer," +
                " nzp_prm  integer," +
                " vald     " + sDecimalType + "(14,4) default 0," +
                " dat_s    DATE," +
                " dat_po   DATE " +
                " )  " + sUnlogTempTable
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " insert into t_par1d (nzp,nzp_prm,vald,dat_s,dat_po)" +
                " select p.nzp,p.nzp_prm,p.val" + sConvToNum + ",p.dat_s,p.dat_po" +
                " from " + gku.paramcalc.kernel_alias + "formuls_opis f,t_par1 p" +
                " where p.nzp_prm=f.nzp_prm_tarif_ls" +
                " group by 1,2,3,4,5 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " insert into t_par1d (nzp,nzp_prm,vald,dat_s,dat_po)" +
                " select p.nzp,p.nzp_prm,p.val" + sConvToNum + ",p.dat_s,p.dat_po" +
                " from " + gku.paramcalc.kernel_alias + "formuls_opis f,t_par1 p" +
                " where p.nzp_prm=f.nzp_prm_tarif_lsp" +
                " group by 1,2,3,4,5 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // ... для расчета дотаций ...
            if (bIsCalcSubsidy)
            {
                ret = ExecSQL(conn_db,
                    " insert into t_par1d (nzp,nzp_prm,vald,dat_s,dat_po)" +
                    " select p.nzp,p.nzp_prm, p.val" + sConvToNum + ",p.dat_s,p.dat_po from " + gku.paramcalc.kernel_alias + "formuls_ops_dt f,t_par1 p" +
                    " where p.nzp_prm=f.nzp_prm_trf_ls" +
                    " group by 1,2,3,4,5 "
                    , true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

                ret = ExecSQL(conn_db,
                    " insert into t_par1d (nzp,nzp_prm,vald,dat_s,dat_po)" +
                    " select p.nzp,p.nzp_prm, p.val" + sConvToNum + ",p.dat_s,p.dat_po from " + gku.paramcalc.kernel_alias + "formuls_ops_dt f,t_par1 p" +
                    " where p.nzp_prm=f.nzp_prm_trf_lsp" +
                    " group by 1,2,3,4,5 "
                    , true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }
            }
            ret = ExecSQL(conn_db, " create index ixt_par1d on t_par1d(nzp,nzp_prm,dat_s,dat_po) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, sUpdStat + " t_par1d ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            #endregion Выборка тарифов для домовых квартирных тарифов t_par1d

            #region Учесть параметры коммунальных квартир

            // выбрать параметры л/с для расчета коммуналок
            ret = ExecSQL(conn_db,
                " create temp table t_rsh_kmnl (" +
                " nzp_key  serial," +
                " nzp_kvar integer," +
                " nzp_prm  integer default 0," +
                " rashod   " + sDecimalType + "(10,8) default 0," +
                " val_prm  " + sDecimalType + "(8,2) default 0" +
                " )  " + sUnlogTempTable
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // комфортность =3 , кол лиц счетов в коммунальной кв =21
            ret = ExecSQL(conn_db,
                " insert into t_rsh_kmnl (nzp_key,nzp_kvar,nzp_prm,rashod,val_prm)" +
                " select 0,t.nzp_kvar,21,1/max(p.val" + sConvToNum + "),max(p.val" + sConvToNum + ") " +
                " from t_opn t,t_par1 p,t_par1 p1" +
                " where t.nzp_kvar=p.nzp  and p.nzp_prm =21 and p.val" + sConvToNum + " >0  " +
                "   and t.nzp_kvar=p1.nzp and p1.nzp_prm=3  and p1.val='2'" +
                " group by 2 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, " create index ixt_rsh_kmnl on t_rsh_kmnl(nzp_kvar) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, sUpdStat + " t_rsh_kmnl ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // выбрать параметры л/с для расчета коммуналок
            ret = ExecSQL(conn_db,
                " create temp table t_kmnl (" +
                " nzp_kvar integer" +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " insert into t_kmnl (nzp_kvar)" +
                " select t.nzp_kvar from t_opn t,t_par1 p" +
                " where t.nzp_kvar=p.nzp and p.nzp_prm =3 and p.val='2'  " +
                " group by 1 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, " create index ixt_kmnl on t_kmnl(nzp_kvar) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, sUpdStat + " t_kmnl ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            #endregion Учесть параметры коммунальных квартир

            return true;
        }
        #endregion Выбрать параметры лицевого счета  на дату t_par1

        #region Типы расчетов тарифа

        #region Типы расчетов тарифа 1,53 - простой тариф ЛС/дом/Пост/БД + НЕучет 335 - Ставки ЭОТ по лиц.счетам!
        public bool CalcTypeTarif_1(IDbConnection conn_db, Gku gku, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            // тариф на всю базу - в последнюю очередь //(здесь очередь priority )
            ret = ExecSQL(conn_db,
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,nzp_frm_typ,tarif,priority" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn)" +
                " select  t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm," +
                " case when f.nzp_frm_typ=53 then f.nzp_frm_typrs else p.nzp_prm end," +
                " f.nzp_frm_typ,max(p.val_prm" + sConvToNum + "),0" +
                " ,t.nzp_period,max(t.dat_s),max(t.dat_po),max(t.cntd),max(t.cntd_mn)" +
                " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f," + gku.paramcalc.data_alias + "prm_5 p" +
                " where t.nzp_frm=f.nzp_frm and p.nzp_prm=f.nzp_prm_tarif_bd and f.nzp_frm_typ in (1,53)" +
                " and p.is_actual<>100 and p.dat_s <= t.dat_po and p.dat_po >= t.dat_s " +
                " group by 1,2,3,4,5,6,9 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // тариф на поставщика - во третью очередь
            ret = ExecSQL(conn_db,
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,nzp_frm_typ,tarif,priority" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn)" +
                " select t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm," +
                " case when f.nzp_frm_typ=53 then f.nzp_frm_typrs else p.nzp_prm end," +
                " f.nzp_frm_typ,max(p.val_prm" + sConvToNum + "),1" +
                " ,t.nzp_period,max(t.dat_s),max(t.dat_po),max(t.cntd),max(t.cntd_mn)" +
                " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f," + gku.paramcalc.data_alias + "prm_11 p" +
                " where t.nzp_frm=f.nzp_frm and t.nzp_supp=p.nzp and p.nzp_prm=f.nzp_prm_tarif_su and f.nzp_frm_typ in (1,53)" +
                " and p.is_actual<>100 and p.dat_s <= t.dat_po and p.dat_po >= t.dat_s " +
                // наличие ЭОТ на поставщика - nzp_prm=336 - если есть, то во вторую очередь
                " and exists (select 1 from " + gku.paramcalc.data_alias + "prm_5 p1" +
                "     where p1.nzp_prm=336 and p1.is_actual<>100 and p1.dat_s<" + gku.dat_po + " and p1.dat_po>=" + gku.dat_s + ") " +
                " group by 1,2,3,4,5,6,9 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // тариф на дом - во вторую очередь
            ret = ExecSQL(conn_db,
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,nzp_frm_typ,tarif,priority" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn)" +
                " select t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm," +
                " case when f.nzp_frm_typ=53 then f.nzp_frm_typrs else p.nzp_prm end," +
                " f.nzp_frm_typ,max(p.val_prm" + sConvToNum + "),2" +
                " ,t.nzp_period,max(t.dat_s),max(t.dat_po),max(t.cntd),max(t.cntd_mn)" +
                " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f,t_par2 p" +
                " where t.nzp_frm=f.nzp_frm and t.nzp_dom=p.nzp and p.nzp_prm=f.nzp_prm_tarif_dm and f.nzp_frm_typ in (1,53)" +
                " and p.dat_s <= t.dat_po and p.dat_po >= t.dat_s " +
                " group by 1,2,3,4,5,6,9 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // тариф на л/с - во первую очередь
            string ssql =
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,nzp_prm1,nzp_frm_typ,tarif,priority" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn)" +
                " select t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm," +
                " case when f.nzp_frm_typ=53 then f.nzp_frm_typrs else p.nzp_prm end," +
                " f.nzp_prm_tarif_lsp,f.nzp_frm_typ," +
                " max(p.vald + " + sNvlWord + "(" +
                "( select p1.vald from t_par1d p1 where t.nzp_kvar=p1.nzp and p1.nzp_prm=f.nzp_prm_tarif_lsp )" +
                ",0)),3" +
                " ,t.nzp_period,max(t.dat_s),max(t.dat_po),max(t.cntd),max(t.cntd_mn)" +
                " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f, t_par1d p " +
                " where t.nzp_frm=f.nzp_frm and t.nzp_kvar=p.nzp and p.nzp_prm=f.nzp_prm_tarif_ls" +
                " and f.nzp_frm_typ in (1,53)" +
                " and p.dat_s <= t.dat_po and p.dat_po >= t.dat_s " +
                " group by 1,2,3,4,5,6,7,10 ";
            ret = ExecSQL(conn_db, ssql, true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            return true;
        }
        #endregion Типы расчетов тарифа 1,53 - простой тариф ЛС/дом/Пост/БД + НЕучет 335 - Ставки ЭОТ по лиц.счетам!

        #region Типы расчетов тарифа  1638 - простой тариф ЛС/дом/Пост/БД или есть ИПУ другой комплект тарифов!
        public bool CalcTypeTarif_1638(IDbConnection conn_db, Gku gku, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            // тариф на всю базу - в последнюю очередь //(здесь очередь priority ) (case when t.is_use_ctr = 1 then g.kg else g.kg_g end),0)
            ret = ExecSQL(conn_db,
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,nzp_frm_typ,tarif,priority" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn)" +
                " select t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm," +
                " p.nzp_prm,f.nzp_frm_typ,max(p.val_prm" + sConvToNum + "),0" +
                " ,t.nzp_period,max(t.dat_s),max(t.dat_po),max(t.cntd),max(t.cntd_mn)" +
                " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f," + gku.paramcalc.data_alias + "prm_5 p" +
                " where t.nzp_frm=f.nzp_frm and p.nzp_prm=f.nzp_prm_tarif_bd and f.nzp_frm_typ=1638" +
                " and p.is_actual<>100 and p.dat_s <= t.dat_po and p.dat_po >= t.dat_s " +
                " group by 1,2,3,4,5,6,9 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // тариф на поставщика - во третью очередь
            ret = ExecSQL(conn_db,
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,nzp_frm_typ,tarif,priority" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn)" +
                " select t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm," +
                " p.nzp_prm,f.nzp_frm_typ,max(p.val_prm" + sConvToNum + "),1" +
                " ,t.nzp_period,max(t.dat_s),max(t.dat_po),max(t.cntd),max(t.cntd_mn)" +
                " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f," + gku.paramcalc.data_alias + "prm_11 p" +
                " where t.nzp_frm=f.nzp_frm and t.nzp_supp=p.nzp" +
                " and p.nzp_prm=(case when t.is_ipu=1 then f.nzp_prm_rash2 else f.nzp_prm_tarif_su end) and f.nzp_frm_typ=1638" +
                " and p.is_actual<>100 and p.dat_s <= t.dat_po and p.dat_po >= t.dat_s " +
                // наличие ЭОТ на поставщика - nzp_prm=336 - если есть, то во вторую очередь
                " and exists (select 1 from " + gku.paramcalc.data_alias + "prm_5 p1" +
                "     where p1.nzp_prm=336 and p1.is_actual<>100 and p1.dat_s<" + gku.dat_po + " and p1.dat_po>=" + gku.dat_s + ") " +
                " group by 1,2,3,4,5,6,9 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // тариф на дом - во вторую очередь
            ret = ExecSQL(conn_db,
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,nzp_frm_typ,tarif,priority" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn)" +
                " select t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm," +
                " p.nzp_prm,f.nzp_frm_typ,max(p.val_prm" + sConvToNum + "),2" +
                " ,t.nzp_period,max(t.dat_s),max(t.dat_po),max(t.cntd),max(t.cntd_mn)" +
                " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f,t_par2 p" +
                " where t.nzp_frm=f.nzp_frm and t.nzp_dom=p.nzp" +
                " and p.nzp_prm=(case when t.is_ipu=1 then f.nzp_prm_rash1 else f.nzp_prm_tarif_dm end) and f.nzp_frm_typ=1638" +
                " and p.dat_s <= t.dat_po and p.dat_po >= t.dat_s " +
                " group by 1,2,3,4,5,6,9 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // тариф на л/с - во первую очередь
            string ssql =
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,nzp_prm1,nzp_frm_typ,tarif,priority" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn)" +
                " select t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm," +
                " p.nzp_prm,f.nzp_prm_tarif_lsp,f.nzp_frm_typ," +
               " max(p.val" + sConvToNum + " + " + sNvlWord + "(" +
                "( select p1.vald from t_par1d p1 where t.nzp_kvar=p1.nzp and p1.nzp_prm=f.nzp_prm_tarif_lsp )" +
                ",0)),3" +
                " ,t.nzp_period,max(t.dat_s),max(t.dat_po),max(t.cntd),max(t.cntd_mn)" +
                " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f, t_par1 p " +
                " where t.nzp_frm=f.nzp_frm and t.nzp_kvar=p.nzp" +
                " and p.nzp_prm=(case when t.is_ipu=1 then f.nzp_prm_rash else f.nzp_prm_tarif_ls end) and f.nzp_frm_typ=1638" +
                " and p.dat_s <= t.dat_po and p.dat_po >= t.dat_s " +
                " group by 1,2,3,4,5,6,7,10 ";
            ret = ExecSQL(conn_db, ssql, true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            return true;
        }
        #endregion Типы расчетов тарифа 1638 - простой тариф ЛС/дом/Пост/БД или есть ИПУ другой комплект тарифов!

        #region Тиы расчета 306 - для лифтов
        public bool CalcLift(IDbConnection conn_db, Gku gku, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            #region Учесть особенности лифта (этажность, начисление на нижних этажах)

            ret = ExecSQL(conn_db,
                " create temp table t_rsh_lift (" +
                " nzp_kvar integer," +
                " nzp_dom integer," +
                " etag     integer default 0," +
                " calc3    integer default 0" +
                " )  " + sUnlogTempTable
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " insert into t_rsh_lift (nzp_kvar,nzp_dom,etag)" +
                " select t.nzp_kvar,t.nzp_dom," + sNvlWord + "(p.val" + sConvToInt + ",0) from " +

#if PG
 " t_opn t left outer join t_par1 p on t.nzp_kvar=p.nzp and p.nzp_prm=2" +
                " where 1=1" +
#else
                " t_opn t,outer t_par1 p" +
                " where t.nzp_kvar=p.nzp and p.nzp_prm=2" +
#endif
 " group by 1,2,3 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, " create index ixt_rsh_lift1 on t_rsh_lift(nzp_dom) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }
            ret = ExecSQL(conn_db, sUpdStat + " t_rsh_lift ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " update t_rsh_lift set calc3=1 " +
                " where 0<(select count(*) " +
                " from ttt_prm_2 p " +
                " where p.nzp = t_rsh_lift.nzp_dom and p.nzp_prm=787) "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " delete from t_rsh_lift where calc3=0 and etag<3 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, " create index ixt_rsh_lift2 on t_rsh_lift(nzp_kvar) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, sUpdStat + " t_rsh_lift ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            #endregion Учесть особенности лифта (этажность, начисление на нижних этажах)

            // тип расчета = 306 - разрешать расчет лифта на нижних этажах!
            #region Тиы расчета 306
            // тариф на всю базу - в последнюю очередь
            ret = ExecSQL(conn_db,
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,tarif,priority,nzp_frm_typ" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn)" +
                " select  t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,max(p.val_prm" + sConvToNum + "),0,306" +
                " ,t.nzp_period,max(t.dat_s),max(t.dat_po),max(t.cntd),max(t.cntd_mn)" +
                " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f," + gku.paramcalc.data_alias + "prm_5 p" +
                " where t.nzp_frm=f.nzp_frm and p.nzp_prm=f.nzp_prm_tarif_bd and f.nzp_frm_typ=306" +
                " and p.is_actual<>100 and p.dat_s <= t.dat_po and p.dat_po >= t.dat_s " +
                " and 0<(select count(*) from t_rsh_lift n where t.nzp_kvar=n.nzp_kvar)" +
                " group by 1,2,3,4,5,9 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // тариф на поставщика - во третью очередь
            ret = ExecSQL(conn_db,
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,tarif,priority,nzp_frm_typ" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn)" +
                " select t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,max(p.val_prm" + sConvToNum + "),1,306" +
                " ,t.nzp_period,max(t.dat_s),max(t.dat_po),max(t.cntd),max(t.cntd_mn)" +
                " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f," + gku.paramcalc.data_alias + "prm_11 p" +
                " where t.nzp_frm=f.nzp_frm and t.nzp_supp=p.nzp and p.nzp_prm=f.nzp_prm_tarif_su and f.nzp_frm_typ=306" +
                " and p.is_actual<>100 and p.dat_s <= t.dat_po and p.dat_po >= t.dat_s " +
                " and 0<(select count(*) from t_rsh_lift n where t.nzp_kvar=n.nzp_kvar)" +
                // наличие ЭОТ на поставщика - nzp_prm=336 - если есть, то во вторую очередь
                " and 0<(select count(*) from " + gku.paramcalc.data_alias + "prm_5 p1" +
                "     where p1.nzp_prm=336 and p1.is_actual<>100 and p1.dat_s<" + gku.dat_po + " and p1.dat_po>=" + gku.dat_s + ") " +
                " group by 1,2,3,4,5,9 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // тариф на дом - во вторую очередь
            ret = ExecSQL(conn_db,
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,tarif,priority,nzp_frm_typ" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn)" +
                " select t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,max(p.val_prm" + sConvToNum + "),2,306" +
                " ,t.nzp_period,max(t.dat_s),max(t.dat_po),max(t.cntd),max(t.cntd_mn)" +
                " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f,t_par2 p" +
                " where t.nzp_frm=f.nzp_frm and t.nzp_dom=p.nzp and p.nzp_prm=f.nzp_prm_tarif_dm and f.nzp_frm_typ=306" +
                " and p.dat_s <= t.dat_po and p.dat_po >= t.dat_s " +
                " and 0<(select count(*) from t_rsh_lift n where t.nzp_kvar=n.nzp_kvar)" +
                " group by 1,2,3,4,5,9 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // тариф на л/с - во первую очередь
            ret = ExecSQL(conn_db,
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,nzp_prm1,tarif,priority,nzp_frm_typ" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn)" +
                " select t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,f.nzp_prm_tarif_lsp," +
                " max(p.vald + " + sNvlWord + "(" +
                "( select max(p1.vald) from t_par1d p1 where t.nzp_kvar=p1.nzp and p1.nzp_prm=f.nzp_prm_tarif_lsp )" +
                ",0)),3,306" +
                " ,t.nzp_period,max(t.dat_s),max(t.dat_po),max(t.cntd),max(t.cntd_mn)" +
                " from " + gku.paramcalc.kernel_alias + "formuls_opis f, t_trf t, t_par1d p" +
                " where t.nzp_frm=f.nzp_frm and t.nzp_kvar=p.nzp and p.nzp_prm=f.nzp_prm_tarif_ls" +
                " and f.nzp_frm_typ=306" +
                " and p.dat_s <= t.dat_po and p.dat_po >= t.dat_s " +
                " and 0<(select count(*) from t_rsh_lift n where t.nzp_kvar=n.nzp_kvar)" +
                " group by 1,2,3,4,5,6,10 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            #endregion Тиы расчета 306

            return true;
        }
        #endregion Тиы расчета 306 - для лифтов

        #region Типы расчетов 101 / 400 для найма
        public bool CalcNaem(IDbConnection conn_db, Gku gku, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            #region Учесть параметры квартир у которых есть наем

            ret = ExecSQL(conn_db,
                " create temp table t_rsh_naem (" +
                " nzp_kvar integer," +
                " nzp_prm  integer default 0" +
                " ) " + sUnlogTempTable
                , true);

            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }
            // 8 приватизировано 34 ветхий фонд
            ret = ExecSQL(conn_db,
                " insert into t_rsh_naem (nzp_kvar,nzp_prm)" +
                " select t.nzp_kvar,p.nzp_prm from t_opn t,t_par1 p" +
                " where t.nzp_kvar=p.nzp  and p.nzp_prm in (8,34)" +
                " group by 1,2 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, " create index ixt_rsh_naem on t_rsh_naem(nzp_kvar) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, sUpdStat + " t_rsh_naem ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            #endregion Учесть параметры квартир у которых есть наем

            #region Тип расчета 101
            // тип расчета = 101

            ret = ExecSQL(conn_db,
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,tarif,priority,nzp_frm_typ" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn)" +
                " select  t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,max(p.val_prm" + sConvToNum + "),0,101" +
                " ,t.nzp_period,max(t.dat_s),max(t.dat_po),max(t.cntd),max(t.cntd_mn)" +
                " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f," + gku.paramcalc.data_alias + "prm_5 p" +
                " where t.nzp_frm=f.nzp_frm and p.nzp_prm=f.nzp_prm_tarif_bd and f.nzp_frm_typ=101" +
                " and p.is_actual<>100 and p.dat_s <= t.dat_po and p.dat_po >= t.dat_s " +
                " and 0=(select count(*) from t_rsh_naem n where t.nzp_kvar=n.nzp_kvar)" +
                " group by 1,2,3,4,5,9 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }
            ret = ExecSQL(conn_db,
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,tarif,priority,nzp_frm_typ" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn)" +
                " select t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,max(p.val_prm" + sConvToNum + "),1,101" +
                " ,t.nzp_period,max(t.dat_s),max(t.dat_po),max(t.cntd),max(t.cntd_mn)" +
                " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f," + gku.paramcalc.data_alias + "prm_11 p" +
                " where t.nzp_frm=f.nzp_frm and t.nzp_supp=p.nzp and p.nzp_prm=f.nzp_prm_tarif_su and f.nzp_frm_typ=101" +
                " and p.is_actual<>100 and p.dat_s <= t.dat_po and p.dat_po >= t.dat_s " +
                " and 0=(select count(*) from t_rsh_naem n where t.nzp_kvar=n.nzp_kvar)" +
                // наличие ЭОТ на поставщика - nzp_prm=336 - если есть, то во вторую очередь
                " and 0<(select count(*) from " + gku.paramcalc.pref + "_data.prm_5 p1" +
                "     where p1.nzp_prm=336 and p1.is_actual<>100 and p1.dat_s<" + gku.dat_po + " and p1.dat_po>=" + gku.dat_s + ") " +
                " group by 1,2,3,4,5,9 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }
            ret = ExecSQL(conn_db,
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,tarif,priority,nzp_frm_typ" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn)" +
                " select t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,max(p.val_prm" + sConvToNum + "),2,101" +
                " ,t.nzp_period,max(t.dat_s),max(t.dat_po),max(t.cntd),max(t.cntd_mn)" +
                " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f,t_par2 p" +
                " where t.nzp_frm=f.nzp_frm and t.nzp_dom=p.nzp and p.nzp_prm=f.nzp_prm_tarif_dm and f.nzp_frm_typ=101" +
                " and p.dat_s <= t.dat_po and p.dat_po >= t.dat_s " +
                " and 0=(select count(*) from t_rsh_naem n where t.nzp_kvar=n.nzp_kvar)" +
                " group by 1,2,3,4,5,9 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }
            ret = ExecSQL(conn_db,
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,tarif,priority,nzp_frm_typ" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn)" +
                " select t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,max(p.vald),3,101" +
                " ,t.nzp_period,max(t.dat_s),max(t.dat_po),max(t.cntd),max(t.cntd_mn)" +
                " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f,t_par1d p" +
                " where t.nzp_frm=f.nzp_frm and t.nzp_kvar=p.nzp and p.nzp_prm=f.nzp_prm_tarif_ls and f.nzp_frm_typ=101" +
                " and p.dat_s <= t.dat_po and p.dat_po >= t.dat_s " +
                " and 0=(select count(*) from t_rsh_naem n where t.nzp_kvar=n.nzp_kvar) " +
                " group by 1,2,3,4,5,9 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            #endregion Тип расчета 101

            #region Тип расчета  400 (найм от базовой ставки)
            // тип расчета = 400 - найм от базовой ставки
            ExecSQL(conn_db, " Drop table t_prm400 ", false);


            ret = ExecSQL(conn_db,
                " create temp table t_prm400 (" +
                " nzp_dom  integer," +
                " nzp_kvar integer," +
                " nzp_supp integer," +
                " nzp_frm  integer," +
                " nzp_frm_kod integer," +
                " priority integer," +
                " nzp_prm  integer," +
                " tarif    " + sDecimalType + "(17,7) default 0," +
                " tarif_bs " + sDecimalType + "(17,7) default 0," +
                " kparam   integer default 1," +
                " k1       " + sDecimalType + "(14,8) default 0," +
                " k2       " + sDecimalType + "(14,8) default 0," +
                " k3       " + sDecimalType + "(14,8) default 0," +
                " k4       " + sDecimalType + "(14,8) default 0," +
                " k5       " + sDecimalType + "(14,8) default 0," +
                " k6       " + sDecimalType + "(14,8) default 0," +
                " p260     integer default 0," +
                " p150     date," +
                " p450     " + sDecimalType + "(14,8) default 0," +
                " p451     integer default 0," +
                " p37      integer default 0," +
                " p940     " + sDecimalType + "(14,8) default 0," +
                " p2       integer default 0," +
                " p1077    integer default 0," +
                " p14      integer default 0," +
                " p16      integer default 0," +
                " tsten    integer default 0," +
                " tdsrok   integer default 0," +
                " tplan    integer default 0," +
                " tetag    integer default 0," +
                " nzp_frm_typ integer default 0 not null," +
                " nzp_period integer," +
                " dat_s    DATE," +
                " dat_po   DATE," +
                " cntd     integer, " +
                " cntd_mn  integer  " +
                " )  " + sUnlogTempTable
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " insert into t_prm400 (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,tarif_bs,priority,nzp_frm_typ,nzp_frm_kod" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn)" +
                " select t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,max(p.vald),0,400,f.nzp_frm_kod" +
                " ,t.nzp_period,max(t.dat_s),max(t.dat_po),max(t.cntd),max(t.cntd_mn)" +
                " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f,t_par1d p" +
                " where t.nzp_frm=f.nzp_frm and t.nzp_kvar=p.nzp and p.nzp_prm=f.nzp_prm_tarif_ls and f.nzp_frm_typ=400" +
                " and p.dat_s <= t.dat_po and p.dat_po >= t.dat_s " +
                " and 0=(select count(*) from t_rsh_naem n where t.nzp_kvar=n.nzp_kvar) " +
                " group by 1,2,3,4,5,9,10 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " insert into t_prm400 (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,tarif_bs,priority,nzp_frm_typ,nzp_frm_kod" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn)" +
                " select t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,max(p.val_prm" + sConvToNum + "),1,400,f.nzp_frm_kod" +
                " ,t.nzp_period,max(t.dat_s),max(t.dat_po),max(t.cntd),max(t.cntd_mn)" +
                " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f,t_par2 p" +
                " where t.nzp_frm=f.nzp_frm and t.nzp_dom=p.nzp and p.nzp_prm=f.nzp_prm_tarif_dm and f.nzp_frm_typ=400" +
                " and p.dat_s <= t.dat_po and p.dat_po >= t.dat_s " +
                " and 0=(select count(*) from t_rsh_naem n where t.nzp_kvar=n.nzp_kvar) " +
                " group by 1,2,3,4,5,9,10 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, " create index ix1t_prm400 on t_prm400(nzp_dom) ", true);
            ret = ExecSQL(conn_db, " create index ix2t_prm400 on t_prm400(nzp_kvar) ", true);
            ret = ExecSQL(conn_db, " create index ix3t_prm400 on t_prm400(nzp_frm_kod) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, sUpdStat + " t_prm400 ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " update t_prm400 set" +
                " p260=(select max( case when p.nzp_prm=260 then p.val_prm::numeric else 0 end )" +
                "  from ttt_prm_2 p where p.nzp=t_prm400.nzp_dom and p.nzp_prm=260)," +
                " p450=(select max( case when p.nzp_prm=450 then p.val_prm::numeric else 0 end )" +
                "  from ttt_prm_2 p where p.nzp=t_prm400.nzp_dom and p.nzp_prm=450)," +
                " p451=(select max( case when p.nzp_prm=451 then p.val_prm::numeric else 0 end )" +
                "  from ttt_prm_2 p where p.nzp=t_prm400.nzp_dom and p.nzp_prm=451)," +
                " p37=(select max( case when p.nzp_prm=37  then p.val_prm::numeric else 0 end )" +
                "  from ttt_prm_2 p where p.nzp=t_prm400.nzp_dom and p.nzp_prm=37)," +
                " p940=(select max( case when p.nzp_prm=940 then p.val_prm::numeric else 0 end ) " +
                "  from ttt_prm_2 p where p.nzp=t_prm400.nzp_dom and p.nzp_prm=940)" +
                " where 0<( select count(*)" +
                "  from ttt_prm_2 p1 where p1.nzp=t_prm400.nzp_dom" +
                "  and p1.nzp_prm in (260,450,451,37,940) ) "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // для даты постройки дома отдельно - м.б. не дата для старых значений!
            ret = ExecSQL(conn_db,
                " update t_prm400 set p150=( select" +
                "  max( p.val_prm" + sConvToDate + " )" +
                "  from ttt_prm_2 p where p.nzp=t_prm400.nzp_dom" +
                "  and p.nzp_prm=150" +
                " )" +
                " where 0<( select count(*)" +
                "  from ttt_prm_2 p1 where p1.nzp=t_prm400.nzp_dom" +
                "  and p1.nzp_prm=150 ) "
                , false);

            ret = ExecSQL(conn_db, " update t_prm400 set p150=" + MDY(1, 1, 1900) + "where p150 is null ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " update t_prm400 set" +
                " p2   =(select max( case when p.nzp_prm=2    then p.val" + sConvToNum + " else 0 end )" +
                "  from t_par1 p where p.nzp=t_prm400.nzp_kvar and p.nzp_prm=2)," +
                " p1077=(select max( case when p.nzp_prm=1077 then p.val" + sConvToNum + " else 0 end )" +
                "  from t_par1 p where p.nzp=t_prm400.nzp_kvar and p.nzp_prm=1077)," +
                " p14  =(select max( case when p.nzp_prm=14   then p.val" + sConvToNum + " else 0 end ) " +
                "  from t_par1 p where p.nzp=t_prm400.nzp_kvar and p.nzp_prm=14)," +
                " p16  =(select max( case when p.nzp_prm=16   then p.val" + sConvToNum + " else 0 end ) " +
                "  from t_par1 p where p.nzp=t_prm400.nzp_kvar and p.nzp_prm=16)" +
                " where 0<( select count(*)" +
                "  from t_par1 p1 where p1.nzp=t_prm400.nzp_kvar and p1.nzp_prm in (2,1077,14,16) )"
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " update t_prm400 set kparam=4,k2=p450,tsten=26,tdsrok=-25,tplan=50,tetag=0" +
                " where nzp_frm_kod in (401,601) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " update t_prm400 set kparam=5,k2=p450,tsten=26,tdsrok=-24,tplan=-18,tetag=0,k5=p940" +
                " where nzp_frm_kod=402 ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " update t_prm400 set kparam=6,k2=p450,tsten=-22,tdsrok=-26,tplan=-20,tetag=-23," +
                "  k6 = (case when p14+p16>0 then 1 else 0.8 end) " +
                " where nzp_frm_kod=403 ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " update t_prm400 set kparam=5,k2=p450,tsten=-22,tdsrok=-26,tplan=-20,tetag=-23," +
                "  p2 = (case when p1077=1 then p37 else p2 end) " +
                " where nzp_frm_kod=404 ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // ограничить по размерностям таблиц
            ret = ExecSQL(conn_db,
                " update t_prm400 set " +
                "  p260 = (case when p260>4 then 4 else (case when p260<=0 then 1 else p260 end) end)," +
                "  p451 = (case when p451>3 then 3 else (case when p451<=0 then 1 else p451 end) end) " +
                " where 1=1 ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " update t_prm400 set " +
                "  k1 = ( Select value From " + gku.paramcalc.kernel_alias + "res_values " +
                "   Where nzp_res = tsten and nzp_y = p260 and nzp_x = 1 )" + sConvToNum +
                " ,k3 = ( Select value From " + gku.paramcalc.kernel_alias + "res_values " +
                "   Where nzp_res = tdsrok and nzp_y = (" +
                    " Select a.nzp_y " +
                    " From " + gku.paramcalc.kernel_alias + "res_values a," + gku.paramcalc.kernel_alias + "res_values b " +
                    " Where a.nzp_res = tdsrok and b.nzp_res = tdsrok and a.nzp_y= b.nzp_y" +
                    " and a.nzp_x = 1 and b.nzp_x = 2 " +
#if PG
 " and EXTRACT(year FROM t_prm400.p150)>=a.value" + sConvToNum +
                    " and EXTRACT(year FROM t_prm400.p150)<=b.value" + sConvToNum +
#else
                    " and year(t_prm400.p150)>=a.value and year(t_prm400.p150)<=b.value" +
#endif
 "      ) and nzp_x = 3 )" + sConvToNum +
                " ,k4 = ( Select value From " + gku.paramcalc.kernel_alias + "res_values " +
                "   Where nzp_res = tplan and nzp_y = p451 and nzp_x = 1 )" + sConvToNum +
                " where 1=1 ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " update t_prm400 set " +
                "  k5 = ( Select value From " + gku.paramcalc.kernel_alias + "res_values " +
                "   Where nzp_res = tetag and nzp_y = " +
                "     case when p2=p37 then 3 else (case when p2=1 then 1 else 2 end) end " +
                "   and nzp_x = 1 )" + sConvToNum +
                " where nzp_frm_kod in (403,404) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, " update t_prm400 set k1 = 0 where k1 is null ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
            ret = ExecSQL(conn_db, " update t_prm400 set k2 = 0 where k2 is null ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
            ret = ExecSQL(conn_db, " update t_prm400 set k3 = 0 where k3 is null ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
            ret = ExecSQL(conn_db, " update t_prm400 set k4 = 0 where k4 is null ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
            ret = ExecSQL(conn_db, " update t_prm400 set k5 = 0 where k5 is null ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
            ret = ExecSQL(conn_db, " update t_prm400 set k6 = 0 where k6 is null ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            ret = ExecSQL(conn_db,
                " update t_prm400 set " +
                "  tarif = tarif_bs * (k1 + k2 + k3 + k4 + k5 + k6) / kparam " +
                " where 1=1 ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " update t_prm400 set " +
                "  tarif = round(tarif * 100) / 100 " +
                " where 1=1 ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            //ViewTbl(conn_db, " select * from t_prm400 ");

            ret = ExecSQL(conn_db,
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,tarif,priority,nzp_frm_typ" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn)" +
                " select nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,tarif,priority,nzp_frm_typ" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn " +
                " from t_prm400 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ExecSQL(conn_db, " Drop table t_prm400 ", false);

            #endregion Тип расчета  400 (найм от базовой ставки)

            return true;
        }
        #endregion Типы расчетов 101 / 400 для найма

        #region Тип расчета 1056 - Целевой сбор-есть приватизация(кв.м) -- для Губкина
        public bool CalcTypeTarif_1056(IDbConnection conn_db, Gku gku, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            #region Учесть приватизированные квартиры

            ret = ExecSQL(conn_db,
                " create temp table t_rsh_privat (" +
                " nzp_kvar integer" +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }
            // 8 приватизировано
            ret = ExecSQL(conn_db,
                " insert into t_rsh_privat (nzp_kvar)" +
                " select t.nzp_kvar from t_opn t,t_par1 p" +
                " where t.nzp_kvar=p.nzp and p.nzp_prm=8" +
                " group by 1 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, " create index ixt_rsh_privat on t_rsh_privat(nzp_kvar) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, sUpdStat + " t_rsh_privat ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            #endregion Учесть приватизированные квартиры

            #region Тип расчета 1056
            // тип расчета = 1056
            ret = ExecSQL(conn_db,
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,tarif,priority,nzp_frm_typ" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn)" +
                " select  t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,max(p.val_prm" + sConvToNum + "),0,1056" +
                " ,t.nzp_period,max(t.dat_s),max(t.dat_po),max(t.cntd),max(t.cntd_mn)" +
                " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f," + gku.paramcalc.data_alias + "prm_5 p" +
                " where t.nzp_frm=f.nzp_frm and p.nzp_prm=f.nzp_prm_tarif_bd and f.nzp_frm_typ=1056" +
                " and p.is_actual<>100 and p.dat_s <= t.dat_po and p.dat_po >= t.dat_s " +
                " and 0<(select count(*) from t_rsh_privat n where t.nzp_kvar=n.nzp_kvar)" +
                " group by 1,2,3,4,5,9 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,tarif,priority,nzp_frm_typ" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn)" +
                " select t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,max(p.val_prm" + sConvToNum + "),1,1056" +
                " ,t.nzp_period,max(t.dat_s),max(t.dat_po),max(t.cntd),max(t.cntd_mn)" +
                " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f," + gku.paramcalc.data_alias + "prm_11 p" +
                " where t.nzp_frm=f.nzp_frm and t.nzp_supp=p.nzp and p.nzp_prm=f.nzp_prm_tarif_su and f.nzp_frm_typ=1056" +
                " and p.is_actual<>100 and p.dat_s <= t.dat_po and p.dat_po >= t.dat_s " +
                " and 0<(select count(*) from t_rsh_privat n where t.nzp_kvar=n.nzp_kvar)" +
                // наличие ЭОТ на поставщика - nzp_prm=336 - если есть, то во вторую очередь
                " and 0<(select count(*) from " + gku.paramcalc.data_alias + "prm_5 p1" +
                "     where p1.nzp_prm=336 and p1.is_actual<>100 and p1.dat_s<" + gku.dat_po + " and p1.dat_po>=" + gku.dat_s + ") " +
                " group by 1,2,3,4,5,9 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,tarif,priority,nzp_frm_typ" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn)" +
                " select t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,max(p.val_prm" + sConvToNum + "),2,1056" +
                " ,t.nzp_period,max(t.dat_s),max(t.dat_po),max(t.cntd),max(t.cntd_mn)" +
                " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f,t_par2 p" +
                " where t.nzp_frm=f.nzp_frm and t.nzp_dom=p.nzp and p.nzp_prm=f.nzp_prm_tarif_dm and f.nzp_frm_typ=1056" +
                " and p.dat_s <= t.dat_po and p.dat_po >= t.dat_s " +
                " and 0<(select count(*) from t_rsh_privat n where t.nzp_kvar=n.nzp_kvar)" +
                " group by 1,2,3,4,5,9 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,tarif,priority,nzp_frm_typ" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn)" +
                " select t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,max(p.vald),3,1056" +
                " ,t.nzp_period,max(t.dat_s),max(t.dat_po),max(t.cntd),max(t.cntd_mn)" +
                " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f,t_par1d p" +
                " where t.nzp_frm=f.nzp_frm and t.nzp_kvar=p.nzp and p.nzp_prm=f.nzp_prm_tarif_ls and f.nzp_frm_typ=1056" +
                " and p.dat_s <= t.dat_po and p.dat_po >= t.dat_s " +
                " and 0<(select count(*) from t_rsh_privat n where t.nzp_kvar=n.nzp_kvar) " +
                " group by 1,2,3,4,5,9 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            //ViewTbl(conn_db, " select * from t_par1d ");
            //ViewTbl(conn_db, " select * from t_prm ");

            #endregion Тип расчета 1056

            return true;
        }
        #endregion Тип расчета 1056 - Целевой сбор-есть приватизация(кв.м) -- для Губкина

        #region Тип расчета 1999 (рассрочка)
        public bool CalcRassr(IDbConnection conn_db, Gku gku, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            string sChargeLocal = gku.paramcalc.pref + "_charge_" + (gku.paramcalc.calc_yy - 2000).ToString("00") + tableDelimiter;
            string sDebtMain = Points.Pref + DBManager.sDebtAliasRest;

            //ViewTbl(conn_db, " select * from t_trf ");

            #region выбрать суммы рассрочки из sChargeLocal

            ExecSQL(conn_db, " drop table t_ls_kredit ", false);

            ret = ExecSQL(conn_db,
                " create temp table t_ls_kredit (" +
                " nzp_kvar integer," +
                " nzp_serv integer," +
                " sum_perc " + sDecimalType + "(17,7) default 0 " +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }
            // 8 приватизировано 34 ветхий фонд
            ret = ExecSQL(conn_db,
                " insert into t_ls_kredit (nzp_kvar,nzp_serv,sum_perc)" +
                " select  t.nzp_kvar,k.nzp_serv,sum(s.sum_perc)" +
                " from t_opn t," + sDebtMain + "kredit k," + sDebtMain + "kredit_pay s " +
                " where t.nzp_kvar=k.nzp_kvar and k.nzp_kredit=s.nzp_kredit" +
                "   AND s.calc_month = " + DBManager.MDY(gku.paramcalc.calc_mm, 1, gku.paramcalc.calc_yy) +
                " and k.valid=1 and k.dat_s<=" + gku.paramcalc.dat_po + " and k.dat_po>=" + gku.paramcalc.dat_s +
                " group by 1,2 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, " create index ixt_ls_kredit on t_ls_kredit(nzp_kvar,nzp_serv) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, sUpdStat + " t_ls_kredit ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            #endregion выбрать суммы рассрочки из sChargeLocal

            #region выбрать действующие услуги рассрочки

            ExecSQL(conn_db, " drop table t_srv_kredit ", false);

            ret = ExecSQL(conn_db,
                " create temp table t_srv_kredit (" +
                " nzp_dom     integer," +
                " nzp_kvar    integer," +
                " nzp_supp    integer," +
                " nzp_frm     integer," +
                " nzp_prm     integer," +
                " priority    integer," +
                " nzp_frm_typ integer," +
                " nzp_period  integer " +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }
            // 8 приватизировано 34 ветхий фонд
            ret = ExecSQL(conn_db,
                " insert into t_srv_kredit (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,priority,nzp_frm_typ,nzp_period)" +
                " select t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,0 nzp_prm,0,1999,max(t.nzp_period)" +
                " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f " +
                " where t.nzp_frm=f.nzp_frm and f.nzp_frm_typ=1999" +
                " group by 1,2,3,4 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, " create index ix1t_ls_kredit on t_srv_kredit(nzp_kvar) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, " create index ix2t_ls_kredit on t_srv_kredit(nzp_frm) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, " create index ix3t_ls_kredit on t_srv_kredit(nzp_period) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, sUpdStat + " t_srv_kredit ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            #endregion выбрать действующие услуги рассрочки

            #region выбрать рассрочку

            // тип расчета = 1999
            ret = ExecSQL(conn_db,
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,tarif,priority,nzp_frm_typ" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn)" +
                " select t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,t.nzp_prm,k.sum_perc,t.priority,t.nzp_frm_typ" +
                " ,t.nzp_period,d.dp,d.dp_end,d.cntd,d.cntd_mn" +
                " from t_srv_kredit t," + gku.paramcalc.kernel_alias + "serv_odn r, t_ls_kredit k,t_gku_periods d " +
                " where t.nzp_kvar=k.nzp_kvar and t.nzp_frm=r.nzp_frm_repay and r.nzp_serv_repay=k.nzp_serv " +
                " and t.nzp_period=d.nzp_period "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            //ViewTbl(conn_db, " select * from t_prm ");

            ret = ExecSQL(conn_db, " drop table t_ls_kredit ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, " drop table t_srv_kredit ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            #endregion выбрать рассрочку

            return true;
        }
        #endregion Тип расчета 1999 (рассрочка)

        #region Тип расчета 2 (отличается от типа 1: тариф на дом в первую очередь, а не тариф ЛС! + Учет 335 - Ставки ЭОТ по лиц.счетам!)
        public bool CalcTypeTarif_2(IDbConnection conn_db, Gku gku, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            // тип расчета = 2
            // ЭОТ на БД - если есть, то во последнюю очередь
            ret = ExecSQL(conn_db,
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,tarif,priority,nzp_frm_typ" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn)" +
                " select  t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,max(p.val_prm" + sConvToNum + "),0,2" +
                " ,t.nzp_period,max(t.dat_s),max(t.dat_po),max(t.cntd),max(t.cntd_mn)" +
                " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f," + gku.paramcalc.data_alias + "prm_5 p" +
                " where t.nzp_frm=f.nzp_frm and p.nzp_prm=f.nzp_prm_tarif_bd and f.nzp_frm_typ=2" +
                " and p.is_actual<>100 and p.dat_s <= t.dat_po and p.dat_po >= t.dat_s " +
                " group by 1,2,3,4,5,9 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // наличие ЭОТ на поставщика - во вторую очередь
            ret = ExecSQL(conn_db,
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,tarif,priority,nzp_frm_typ" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn)" +
                " select t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,max(p.val_prm" + sConvToNum + "),1,2" +
                " ,t.nzp_period,max(t.dat_s),max(t.dat_po),max(t.cntd),max(t.cntd_mn)" +
                " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f," + gku.paramcalc.data_alias + "prm_11 p" +
                " where t.nzp_frm=f.nzp_frm and t.nzp_supp=p.nzp and p.nzp_prm=f.nzp_prm_tarif_su and f.nzp_frm_typ=2" +
                " and p.is_actual<>100 and p.dat_s <= t.dat_po and p.dat_po >= t.dat_s " +
                // наличие ЭОТ на поставщика - nzp_prm=336 - если есть, то во вторую очередь
                " and 0<(select count(*) from " + gku.paramcalc.data_alias + "prm_5 p1" +
                "     where p1.nzp_prm=336 and p1.is_actual<>100 and p1.dat_s<" + gku.dat_po + " and p1.dat_po>=" + gku.dat_s + ") " +
                " group by 1,2,3,4,5,9 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // выбрать параметр 335 - Ставки ЭОТ по лиц.счетам
            ret = ExecSQL(conn_db,
                " create temp table t_par335 (" +
                " nzp      integer" +
                " )  " + sUnlogTempTable
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " insert into t_par335 (nzp)" +
                " select nzp from t_par1 where nzp_prm=335 group by 1 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, " create index ixt_par335 on t_par335(nzp) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, sUpdStat + " t_par335 ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // наличие ЭОТ на л/с - в первую очередь
            ret = ExecSQL(conn_db,
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,tarif,priority,nzp_frm_typ" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn)" +
                " select t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,max(p.vald),3,2" +
                " ,t.nzp_period,max(t.dat_s),max(t.dat_po),max(t.cntd),max(t.cntd_mn)" +
                " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f,t_par1d p" +
                " where t.nzp_frm=f.nzp_frm and t.nzp_kvar=p.nzp and p.nzp_prm=f.nzp_prm_tarif_ls and f.nzp_frm_typ=2" +
                // наличие ЭОТ на л/с - nzp_prm=335 - если есть, то в первую очередь
                " and p.dat_s <= t.dat_po and p.dat_po >= t.dat_s " +
                " and 0<(select count(*) from t_par335 p1 where p1.nzp=t.nzp_kvar) " +
                " group by 1,2,3,4,5,9 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // наличие ЭОТ на дом - f.nzp_prm_tarif_dm - если есть, то в самую первую очередь
            ret = ExecSQL(conn_db,
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,tarif,priority,nzp_frm_typ" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn)" +
                " select t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,max(p.val_prm" + sConvToNum + "),4,2" +
                " ,t.nzp_period,max(t.dat_s),max(t.dat_po),max(t.cntd),max(t.cntd_mn)" +
                " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f,t_par2 p" +
                " where t.nzp_frm=f.nzp_frm and t.nzp_dom=p.nzp and p.nzp_prm=f.nzp_prm_tarif_dm and f.nzp_frm_typ=2" +
                " and p.dat_s <= t.dat_po and p.dat_po >= t.dat_s " +
                " group by 1,2,3,4,5,9 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            return true;
        }
        #endregion Тип расчета 2 (отличается от типа 1: тариф на дом в первую очередь, а не тариф ЛС! + Учет 335 - Ставки ЭОТ по лиц.счетам!)

        #region Тип расчета 26 (домофон с трубкой без трубки , тарифы выбираются в разных местах )
        public bool CalcDomof(IDbConnection conn_db, Gku gku, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            // тип расчета = 26
            //ViewTbl(conn_db, " select * from t_par26f ");

            //ViewTbl(conn_db, " select * from t_trf ");

            //ViewTbl(conn_db, " select * from ttt_prm_2 ");

            // домовой тариф с трубкой на л/с - f.nzp_prm_tarif_dm
            ret = ExecSQL(conn_db,
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,tarif,priority,nzp_frm_typ" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn)" +
                " select t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,max(p.val_prm" + sConvToNum + "),1,26" +
                " ,t.nzp_period,max(t.dat_s),max(t.dat_po),max(t.cntd),max(t.cntd_mn)" +
                " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f,t_par2 p" +
                " where t.nzp_frm=f.nzp_frm and t.nzp_dom=p.nzp and p.nzp_prm=f.nzp_prm_tarif_dm and f.nzp_frm_typ=26" +
                " and p.dat_s <= t.dat_po and p.dat_po >= t.dat_s " +
                // наличие трубки на л/с - f.nzp_prm_tarif_bd
                "  and 0<(select count(*) from t_par26f p1 where p1.nzp=t.nzp_kvar and p1.nzp_prm=f.nzp_prm_tarif_bd)" +
                " group by 1,2,3,4,5,9 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // домовой тариф без трубки на л/с - f.nzp_prm_tarif_su
            ret = ExecSQL(conn_db,
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,tarif,priority,nzp_frm_typ" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn)" +
                " select t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,max(p.val_prm" + sConvToNum + "),2,26" +
                " ,t.nzp_period,max(t.dat_s),max(t.dat_po),max(t.cntd),max(t.cntd_mn)" +
                " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f,t_par2 p" +
                " where t.nzp_frm=f.nzp_frm and t.nzp_dom=p.nzp and p.nzp_prm=f.nzp_prm_tarif_su and f.nzp_frm_typ=26" +
                " and p.dat_s <= t.dat_po and p.dat_po >= t.dat_s " +
                // осутствие трубки на л/с - f.nzp_prm_tarif_bd
                "  and 0=(select count(*) from t_par26f p1 where p1.nzp=t.nzp_kvar and p1.nzp_prm=f.nzp_prm_tarif_bd)" +
                " group by 1,2,3,4,5,9 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // квартирный тариф на л/с - f.nzp_prm_tarif_ls
            ret = ExecSQL(conn_db,
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,tarif,priority,nzp_frm_typ" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn)" +
                " select t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,max(p.vald),3,26" +
                " ,t.nzp_period,max(t.dat_s),max(t.dat_po),max(t.cntd),max(t.cntd_mn)" +
                " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f,t_par1d p" +
                " where t.nzp_frm=f.nzp_frm and t.nzp_kvar=p.nzp and p.nzp_prm=f.nzp_prm_tarif_ls and f.nzp_frm_typ=26" +
                " and p.dat_s <= t.dat_po and p.dat_po >= t.dat_s " +
                " group by 1,2,3,4,5,9 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            return true;
        }
        #endregion Тип расчета 26 (домофон с трубкой без трубки , тарифы выбираются в разных местах )

        #region Типы расчета эл/энергии 12 & 312 & 412 & 912
        public bool CalcTypeTarif_ElEn(IDbConnection conn_db, Gku gku, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            #region Тип расчета 12 ( )
            // тип расчета = 12
            // тариф на базу с эл/плитой на л/с - f.nzp_prm_tarif_dm
            ret = ExecSQL(conn_db,
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,tarif,priority,nzp_frm_typ" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn)" +
                " select t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,max(p.val_prm" + sConvToNum + "),1,12" +
                " ,t.nzp_period,max(t.dat_s),max(t.dat_po),max(t.cntd),max(t.cntd_mn)" +
                " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f," + gku.paramcalc.data_alias + "prm_5 p" +
                " where t.nzp_frm=f.nzp_frm and p.nzp_prm=f.nzp_prm_tarif_dm and f.nzp_frm_typ=12" +
                " and p.is_actual<>100 and p.dat_s <= t.dat_po and p.dat_po >= t.dat_s " +
                // наличие эл/плиты на л/с - f.nzp_prm_tarif_bd
                "  and 0<(select count(*) from t_par26f p1 where p1.nzp=t.nzp_kvar and p1.nzp_prm=f.nzp_prm_tarif_bd)" +
                " group by 1,2,3,4,5,9 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
            // тариф на базу без эл/плиты на л/с - f.nzp_prm_tarif_su
            ret = ExecSQL(conn_db,
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,tarif,priority,nzp_frm_typ" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn)" +
                " select t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,max(p.val_prm" + sConvToNum + "),2,12" +
                " ,t.nzp_period,max(t.dat_s),max(t.dat_po),max(t.cntd),max(t.cntd_mn)" +
                " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f," + gku.paramcalc.data_alias + "prm_5 p" +
                " where t.nzp_frm=f.nzp_frm and p.nzp_prm=f.nzp_prm_tarif_su and f.nzp_frm_typ=12" +
                " and p.is_actual<>100 and p.dat_s <= t.dat_po and p.dat_po >= t.dat_s " +
                // осутствие эл/плиты на л/с - f.nzp_prm_tarif_bd
                "  and 0=(select count(*) from t_par26f p1 where p1.nzp=t.nzp_kvar and p1.nzp_prm=f.nzp_prm_tarif_bd)" +
                " group by 1,2,3,4,5,9 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
            #endregion Тип расчета 12 ( )

            //7.1
            #region Тип расчета 312 ( )
            // тип расчета = 312
            // тариф на ЛС с эл/плитой на л/с - f.nzp_prm_tarif_dm
            ret = ExecSQL(conn_db,
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,tarif,priority,nzp_frm_typ" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn)" +
                " select t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,max(p.val" + sConvToNum + "),1,312" +
                " ,t.nzp_period,max(t.dat_s),max(t.dat_po),max(t.cntd),max(t.cntd_mn)" +
                " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f,t_par1 p" +
                " where t.nzp_frm=f.nzp_frm and t.nzp_kvar=p.nzp and p.nzp_prm=f.nzp_prm_tarif_dm and f.nzp_frm_typ=312" +
                " and p.dat_s <= t.dat_po and p.dat_po >= t.dat_s " +
                // наличие эл/плиты на л/с - f.nzp_prm_tarif_bd
                "  and 0<(select count(*) from t_par26f p1 where p1.nzp=t.nzp_kvar and p1.nzp_prm=f.nzp_prm_tarif_bd)" +
                " group by 1,2,3,4,5,9 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
            // тариф на ЛС без эл/плиты на л/с - f.nzp_prm_tarif_su
            ret = ExecSQL(conn_db,
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,tarif,priority,nzp_frm_typ" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn)" +
                " select t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,max(p.val" + sConvToNum + "),2,312" +
                " ,t.nzp_period,max(t.dat_s),max(t.dat_po),max(t.cntd),max(t.cntd_mn)" +
                " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f,t_par1 p" +
                " where t.nzp_frm=f.nzp_frm and t.nzp_kvar=p.nzp and p.nzp_prm=f.nzp_prm_tarif_su and f.nzp_frm_typ=312" +
                " and p.dat_s <= t.dat_po and p.dat_po >= t.dat_s " +
                // осутствие эл/плиты на л/с - f.nzp_prm_tarif_bd
                "  and 0=(select count(*) from t_par26f p1 where p1.nzp=t.nzp_kvar and p1.nzp_prm=f.nzp_prm_tarif_bd)" +
                " group by 1,2,3,4,5,9 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
            #endregion Тип расчета 312 ( )

            //DataTable DtTbl = new DataTable();
            //DtTbl = ViewTbl(conn_db, " select * from t_prm where nzp_kvar=70784 order by nzp_frm ");
            //DtTbl = ViewTbl(conn_db, " select * from t_par26f where nzp=70784 order by nzp_prm ");

            //8
            #region Тип расчета 412 ()
            // тип расчета = 412
            // выбрать параметры л/с для nzp_prm_tarif_bd на дату расчета
            string ssql =
                " create temp table t_par412 (" +
                " nzp_kvar integer" +
                " )  " + sUnlogTempTable;
            ret = ExecSQL(conn_db, ssql, true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            ret = ExecSQL(conn_db,
                " insert into t_par412 (nzp_kvar) select " + sUniqueWord + " p.nzp from t_par1 p where p.nzp_prm=754 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            ret = ExecSQL(conn_db, " create index ixt_par412 on t_par412(nzp_kvar) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            ret = ExecSQL(conn_db, sUpdStat + " t_par412 ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            // тариф на базу с эл/плитой на л/с - f.nzp_prm_tarif_dm
            ret = ExecSQL(conn_db,
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,tarif,priority,nzp_frm_typ" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn)" +
                " select t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,max(p.val_prm" + sConvToNum + "),1,412" +
                " ,t.nzp_period,max(t.dat_s),max(t.dat_po),max(t.cntd),max(t.cntd_mn)" +
                " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f," + gku.paramcalc.data_alias + "prm_5 p" +
                " where t.nzp_frm=f.nzp_frm and p.nzp_prm=f.nzp_prm_tarif_ls and f.nzp_frm_typ=412" +
                " and p.is_actual<>100 and p.dat_s <= t.dat_po and p.dat_po >= t.dat_s " +
                // Ночное электроснабжение по своему тарифу
                "  and 0=(select count(*) from t_par412 p2 where p2.nzp_kvar=t.nzp_kvar)" +
                // наличие эл/плиты на л/с - f.nzp_prm_tarif_bd
                "  and 0<(select count(*) from t_par26f p1 where p1.nzp=t.nzp_kvar and p1.nzp_prm=f.nzp_prm_tarif_bd)" +
                " group by 1,2,3,4,5,9 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            // тариф на базу без эл/плиты на л/с - f.nzp_prm_tarif_su
            ret = ExecSQL(conn_db,
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,tarif,priority,nzp_frm_typ" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn)" +
                " select t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,max(p.val_prm" + sConvToNum + "),2,412" +
                " ,t.nzp_period,max(t.dat_s),max(t.dat_po),max(t.cntd),max(t.cntd_mn)" +
                " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f," + gku.paramcalc.data_alias + "prm_5 p" +
                " where t.nzp_frm=f.nzp_frm and p.nzp_prm=f.nzp_prm_tarif_lsp and f.nzp_frm_typ=412" +
                " and p.is_actual<>100 and p.dat_s <= t.dat_po and p.dat_po >= t.dat_s " +
                // Ночное электроснабжение по своему тарифу
                "  and 0=(select count(*) from t_par412 p2 where p2.nzp_kvar=t.nzp_kvar)" +
                // осутствие эл/плиты на л/с - f.nzp_prm_tarif_bd
                "  and 0=(select count(*) from t_par26f p1 where p1.nzp=t.nzp_kvar and p1.nzp_prm=f.nzp_prm_tarif_bd)" +
                " group by 1,2,3,4,5,9 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            // тариф на базу с эл/плитой на л/с - f.nzp_prm_tarif_dm
            ret = ExecSQL(conn_db,
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,tarif,priority,nzp_frm_typ" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn)" +
                " select t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,max(p.val_prm" + sConvToNum + "),1,412" +
                " ,t.nzp_period,max(t.dat_s),max(t.dat_po),max(t.cntd),max(t.cntd_mn)" +
                " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f," + gku.paramcalc.data_alias + "prm_5 p" +
                " where t.nzp_frm=f.nzp_frm and p.nzp_prm=f.nzp_prm_tarif_dm and f.nzp_frm_typ=412" +
                " and p.is_actual<>100 and p.dat_s <= t.dat_po and p.dat_po >= t.dat_s " +
                // Ночное электроснабжение по дневному тарифу
                "  and 0<(select count(*) from t_par412 p2 where p2.nzp_kvar=t.nzp_kvar)" +
                // наличие эл/плиты на л/с - f.nzp_prm_tarif_bd
                "  and 0<(select count(*) from t_par26f p1 where p1.nzp=t.nzp_kvar and p1.nzp_prm=f.nzp_prm_tarif_bd)" +
                " group by 1,2,3,4,5,9 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            // тариф на базу без эл/плиты на л/с - f.nzp_prm_tarif_su
            ret = ExecSQL(conn_db,
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,tarif,priority,nzp_frm_typ" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn)" +
                " select t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,max(p.val_prm" + sConvToNum + "),2,412" +
                " ,t.nzp_period,max(t.dat_s),max(t.dat_po),max(t.cntd),max(t.cntd_mn)" +
                " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f," + gku.paramcalc.data_alias + "prm_5 p" +
                " where t.nzp_frm=f.nzp_frm and p.nzp_prm=f.nzp_prm_tarif_su and f.nzp_frm_typ=412" +
                " and p.is_actual<>100 and p.dat_s <= t.dat_po and p.dat_po >= t.dat_s " +
                // Ночное электроснабжение по дневному тарифу
                "  and 0<(select count(*) from t_par412 p2 where p2.nzp_kvar=t.nzp_kvar)" +
                // осутствие эл/плиты на л/с - f.nzp_prm_tarif_bd
                "  and 0=(select count(*) from t_par26f p1 where p1.nzp=t.nzp_kvar and p1.nzp_prm=f.nzp_prm_tarif_bd)" +
                " group by 1,2,3,4,5,9 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            #endregion Тип расчета 412 (в)

            //8.1
            #region Тип расчета 912 ()
            // тип расчета = 912
            // тариф на базу с эл/плитой на л/с - f.nzp_prm_tarif_dm
            ret = ExecSQL(conn_db,
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,tarif,priority,nzp_frm_typ" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn)" +
                " select t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,max(p.val" + sConvToNum + "),1,912" +
                " ,t.nzp_period,max(t.dat_s),max(t.dat_po),max(t.cntd),max(t.cntd_mn)" +
                " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f,t_par1 p" +
                " where t.nzp_frm=f.nzp_frm and t.nzp_kvar=p.nzp and p.nzp_prm=f.nzp_prm_tarif_ls and f.nzp_frm_typ=912" +
                " and p.dat_s <= t.dat_po and p.dat_po >= t.dat_s " +
                // наличие эл/плиты на л/с - f.nzp_prm_tarif_bd
                "  and 0<(select count(*) from t_par26f p1 where p1.nzp=t.nzp_kvar and p1.nzp_prm=f.nzp_prm_tarif_bd)" +
                " group by 1,2,3,4,5,9 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            // тариф на базу без эл/плиты на л/с - f.nzp_prm_tarif_su
            ret = ExecSQL(conn_db,
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,tarif,priority,nzp_frm_typ" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn)" +
                " select t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,max(p.val" + sConvToNum + "),2,912" +
                " ,t.nzp_period,max(t.dat_s),max(t.dat_po),max(t.cntd),max(t.cntd_mn)" +
                " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f,t_par1 p" +
                " where t.nzp_frm=f.nzp_frm and t.nzp_kvar=p.nzp and p.nzp_prm=f.nzp_prm_tarif_lsp and f.nzp_frm_typ=912" +
                " and p.dat_s <= t.dat_po and p.dat_po >= t.dat_s " +
                // осутствие эл/плиты на л/с - f.nzp_prm_tarif_bd
                "  and 0=(select count(*) from t_par26f p1 where p1.nzp=t.nzp_kvar and p1.nzp_prm=f.nzp_prm_tarif_bd)" +
                " group by 1,2,3,4,5,9 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            #endregion Тип расчета 912 ()

            return true;
        }
        #endregion Типы расчета эл/энергии 12 & 312 & 412 & 912

        #region Тип расчета = 40 & 814 & 440 & 514 & 1140 & 1814 & 1983 - выборка тарифа от ГКал
        public bool CalcTypeTarif_GKal(IDbConnection conn_db, Gku gku, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();


            // ЭОТ на БД - если есть, то во последнюю очередь
            ret = ExecSQL(conn_db,
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,tarif_gkal,priority,nzp_frm_typ" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn)" +
                " select  t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,max(p.val_prm" + sConvToNum + "),0,f.nzp_frm_typ" +
                " ,t.nzp_period,max(t.dat_s),max(t.dat_po),max(t.cntd),max(t.cntd_mn)" +
                " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f," + gku.paramcalc.data_alias + "prm_5 p" +
                " where t.nzp_frm=f.nzp_frm and p.nzp_prm=f.nzp_prm_tarif_bd and f.nzp_frm_typ in (40,814,440,514,1140,1814,1983)" +
                " and p.is_actual<>100 and p.dat_s <= t.dat_po and p.dat_po >= t.dat_s " +
                " group by 1,2,3,4,5,8,9 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // наличие ЭОТ на поставщика - во вторую очередь
            // выборка тарифа и параметра по поставщикам (nzp_prm_tarif_su) из prm_11 c доп условием настройки на базу (признак брать тарифы по поставщикам 336 параметр )
            ret = ExecSQL(conn_db,
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,tarif_gkal,priority,nzp_frm_typ" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn)" +
                " select t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,max(p.val_prm" + sConvToNum + "),1,f.nzp_frm_typ" +
                " ,t.nzp_period,max(t.dat_s),max(t.dat_po),max(t.cntd),max(t.cntd_mn)" +
                " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f," + gku.paramcalc.data_alias + "prm_11 p" +
                " where t.nzp_frm=f.nzp_frm and t.nzp_supp=p.nzp and p.nzp_prm=f.nzp_prm_tarif_su and f.nzp_frm_typ in (40,814,440,514,1140,1814,1983)" +
                " and p.is_actual<>100 and p.dat_s <= t.dat_po and p.dat_po >= t.dat_s " +
                // наличие ЭОТ на поставщика - nzp_prm=336 - если есть, то во вторую очередь
                " and 0<(select count(*) from " + gku.paramcalc.data_alias + "prm_5 p1" +
                "     where p1.nzp_prm=336 and p1.is_actual<>100 and p1.dat_s<" + gku.dat_po + " and p1.dat_po>=" + gku.dat_s + ") " +
                " group by 1,2,3,4,5,8,9 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // тариф на дом для отопления - во первую после ЛС очередь
            ret = ExecSQL(conn_db,
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,tarif_gkal,priority,nzp_frm_typ" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn)" +
                " select t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,max(p.val_prm" + sConvToNum + "),2,f.nzp_frm_typ" +
                " ,t.nzp_period,max(t.dat_s),max(t.dat_po),max(t.cntd),max(t.cntd_mn)" +
                " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f,t_par2 p" +
                " where t.nzp_frm=f.nzp_frm and t.nzp_dom=p.nzp and p.nzp_prm=f.nzp_prm_tarif_dm and f.nzp_frm_typ in (40,814,440,514,1140,1814,1983) " +
                " and p.dat_s <= t.dat_po and p.dat_po >= t.dat_s " +
                " group by 1,2,3,4,5,8,9 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // наличие ЭОТ на л/с - в первую очередь
            ret = ExecSQL(conn_db,
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,tarif_gkal,priority,nzp_frm_typ" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn)" +
                " select t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,max(p.vald),3,f.nzp_frm_typ" +
                " ,t.nzp_period,max(t.dat_s),max(t.dat_po),max(t.cntd),max(t.cntd_mn)" +
                " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f,t_par1d p" +
                " where t.nzp_frm=f.nzp_frm and t.nzp_kvar=p.nzp and p.nzp_prm=f.nzp_prm_tarif_ls and f.nzp_frm_typ in (40,814,440,514,1140,1814,1983)" +
                " and p.dat_s <= t.dat_po and p.dat_po >= t.dat_s " +
                // наличие ЭОТ на л/с - nzp_prm=335 - если есть, то в первую очередь
                " and 0<(select count(*) from t_par335 p1 where p1.nzp=t.nzp_kvar) " +
                " group by 1,2,3,4,5,8,9 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            return true;
        }
        #endregion Тип расчета = 40 & 814 & 440 & 514 & 1140 & 1814 & 1983 - выборка тарифа от ГКал

        #region Расчет тарифа по дополнительным параметрам тип расчета = 40,440,1140,1814,1983
        public bool CalcTypeTarif_GKalDop(IDbConnection conn_db, Gku gku, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            // Учесть доп логику по гор.воде

            #region Выбрать параметры полотенцесушителя и неизолированный трубопровод (только для горячей воды тип 40)

            // выбрать параметры л/с для типа 40 на дату расчета
            ret = ExecSQL(conn_db,
                " create temp table t_par40f (" +
                " nzp     integer," +
                " nzp_prm integer" +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " insert into t_par40f (nzp,nzp_prm)" +
                " select " + sUniqueWord + " nzp,nzp_prm from t_par1 where nzp_prm in (59,327) "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, " create index ixt_par40f on t_par40f(nzp,nzp_prm) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, sUpdStat + " t_par40f ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            #endregion Выбрать параметры полотенцесушителя и неизолированный трубопровод

            #region Выбрать норму Гкал из параметра БД + установка: полотенцесушитель / неизолированный трубопровод
            // Выбрать параметр норма Гкал на базу данных для всех формул с Гкал: nzp_frm_typ in (40,440,1140)
            ret = ExecSQL(conn_db,
                " update t_prm set" +
                " norm_gkal=(" +
                " select max(p.val_prm" + sConvToNum + ") from " + gku.paramcalc.data_alias + "prm_5 p where p.nzp_prm=253" +
                " and p.is_actual<>100 and p.dat_s <= t_prm.dat_po and p.dat_po >= t_prm.dat_s " +
                " )" +
                " where nzp_frm_typ in (40,440,1140) "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, " update t_prm set norm_gkal = 0 where nzp_frm_typ in (40,440,1140) and norm_gkal  is null ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // Выставить полотенцесушитель (только горячая вода типы 40,440,1140)
            ret = ExecSQL(conn_db,
                " update t_prm set is_poloten=1" +
                " where nzp_frm_typ in (40,440,1140) and 0<(select count(*) from t_par40f p where t_prm.nzp_kvar=p.nzp and p.nzp_prm= 59) "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // Выставить неизолированный трубопровод (только горячая вода типы 40,440,1140)
            ret = ExecSQL(conn_db,
                " update t_prm set is_neiztrp=1" +
                " where nzp_frm_typ in (40,440,1140) and 0<(select count(*) from t_par40f p where t_prm.nzp_kvar=p.nzp and p.nzp_prm=327) "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            #endregion Выбрать норму Гкал из параметра БД + установка: полотенцесушитель / неизолированный трубопровод

            #region Применение повышающего коэффициента  (только горячая вода типы 40,440,1140)

            // запрещено применять повышающий коэффициент для ГВС ?
            bool bNotUseKoefGVS =
                CheckValBoolPrmWithVal(conn_db, gku.paramcalc.data_alias, 1173, "5", "1", gku.dat_s, gku.dat_po);

            // параметр 1173 на  базу - запрещено применять повышающий коэффициент 
            if (bNotUseKoefGVS)
            {
                ret = ExecSQL(conn_db, " update t_prm set koef_gkal=1 where nzp_frm_typ in (40,440,1140) ", true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }
            }
            else
            {
                ret = ExecSQL(conn_db, " update t_prm set koef_gkal=1.2 where nzp_frm_typ in (40,440,1140) ", true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

                ret = ExecSQL(conn_db, " update t_prm set koef_gkal=1.1 where nzp_frm_typ in (40,440,1140) and is_neiztrp=0 and is_neiztrp=0 ", true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

                ret = ExecSQL(conn_db, " update t_prm set koef_gkal=1.3 where nzp_frm_typ in (40,440,1140) and is_neiztrp=1 and is_neiztrp=1 ", true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }
            }
            #endregion Применение повышающего коэффициента  (только горячая вода типы 40,440,1140)

            #region Выбрать норму Гкалл из домового параметра ( prm_2  nzp_prm =436 по типам 40,440,1140,1983 / nzp_prm =723 по типам 1814 )
            ret = ExecSQL(conn_db,
                " update t_prm set" +
                " norm_gkal=(" +
                  " select max(p.val_prm" + sConvToNum + ") from t_par2 p where p.nzp=t_prm.nzp_dom and p.nzp_prm=436 " +
                  " and p.dat_s <= t_prm.dat_po and p.dat_po >= t_prm.dat_s " +
                " )," +
                " koef_gkal=1" +
                " where nzp_frm_typ in (40,440,1140,1983) " +
                // наличие нормы на дом - nzp_prm=436 - если есть, то во вторую очередь
                "  and exists (" +
                  " select 1 from t_par2 p where p.nzp=t_prm.nzp_dom and p.nzp_prm=436 " +
                  " and p.dat_s <= t_prm.dat_po and p.dat_po >= t_prm.dat_s " +
                " )"
                , true, 6000);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " update t_prm set" +
                " norm_gkal=(" +
                  " select max(p.val_prm" + sConvToNum + ") from t_par2 p where p.nzp=t_prm.nzp_dom and p.nzp_prm=723 " +
                  " and p.dat_s <= t_prm.dat_po and p.dat_po >= t_prm.dat_s " +
                " )," +
                " koef_gkal=1" +
                " where nzp_frm_typ in (1814) " +
                // наличие нормы на дом - nzp_prm=723 - если есть, то во вторую очередь
                "  and exists (" +
                  " select 1 from t_par2 p where p.nzp=t_prm.nzp_dom and p.nzp_prm=723 " +
                  " and p.dat_s <= t_prm.dat_po and p.dat_po >= t_prm.dat_s " +
                " )"
                , true, 6000);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            #endregion Выбрать норму Гкалл из домового параметра ( prm_2:  nzp_prm =436 по типам 40,440,1140,1983 / nzp_prm =723 по типам 1814 )

            #region Выбрать норму Гкал на 1 м2 для отопления на лицевой счет prm_1 nzp_prm =2463: nzp_frm_typ in (1814)

            // выбрать параметры л/с для типа 1814 на дату расчета
            ExecSQL(conn_db, " drop table t_par2463 ", false);

            ret = ExecSQL(conn_db,
                " create temp table t_par2463 (" +
                " nzp     integer," +
                " vald    " + sDecimalType + "(14,7)," +
                " dat_s   date," +
                " dat_po  date " +
                " )  " + sUnlogTempTable
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // Выбрать квартирный норматив гкал на м3
            ret = ExecSQL(conn_db,
                " insert into t_par2463 (nzp,vald,dat_s,dat_po)" +
                " select nzp,val" + sConvToNum + ",dat_s,dat_po" +
                " from t_par1 where nzp_prm=2463 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, " create index ixt_par2463 on t_par2463(nzp,dat_s,dat_po) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, sUpdStat + " t_par2463 ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " update t_prm set" +
                " norm_gkal=(" +
                  " select max(p2.vald) from t_par2463 p2 where p2.nzp=t_prm.nzp_kvar " +
                  " and p2.dat_s <= t_prm.dat_po and p2.dat_po >= t_prm.dat_s " +
                ")," +
                " koef_gkal=1" +
                " where nzp_frm_typ in (1814) " +
                // наличие нормы на л/с - nzp_prm=2463 - если есть, то в первую очередь
                "  and exists (" +
                  " select 1 from t_par2463 p1 where p1.nzp=t_prm.nzp_kvar" +
                  " and p1.dat_s <= t_prm.dat_po and p1.dat_po >= t_prm.dat_s " +
                ") "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            #endregion Выбрать норму Гкал на 1 м2 для отопления на лицевой счет prm_1 nzp_prm =2463: nzp_frm_typ in (1814)

            #region Выбрать норму Гкал на 1 м3 воды на лицевой счет prm_1 nzp_prm =894: nzp_frm_typ in (40,440,1140,1983)

            // выбрать параметры л/с для типа 40 на дату расчета
            ExecSQL(conn_db, " drop table t_par894 ", false);

            ret = ExecSQL(conn_db,
                " create temp table t_par894 (" +
                " nzp     integer," +
                " vald    " + sDecimalType + "(14,7)," +
                " dat_s   date," +
                " dat_po  date " +
                " )  " + sUnlogTempTable
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // Выбрать квартирный норматив гкал на м3
            ret = ExecSQL(conn_db,
                " insert into t_par894 (nzp,vald,dat_s,dat_po)" +
                " select nzp,val" + sConvToNum + ",dat_s,dat_po" +
                " from t_par1 where nzp_prm=894 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, " create index ixt_par894 on t_par894(nzp,dat_s,dat_po) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, sUpdStat + " t_par894 ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " update t_prm set" +
                " norm_gkal=(" +
                  " select max(p2.vald) from t_par894 p2 where p2.nzp=t_prm.nzp_kvar " +
                  " and p2.dat_s <= t_prm.dat_po and p2.dat_po >= t_prm.dat_s " +
                ")," +
                " koef_gkal=1" +
                " where nzp_frm_typ in (40,440,1140,1983) " +
                // наличие нормы на л/с - nzp_prm=894 - если есть, то в первую очередь
                "  and exists (" +
                  " select 1 from t_par894 p1 where p1.nzp=t_prm.nzp_kvar" +
                  " and p1.dat_s <= t_prm.dat_po and p1.dat_po >= t_prm.dat_s " +
                ") "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            #endregion Выбрать норму Гкал на 1 м3 воды на лицевой счет prm_1 nzp_prm =894: nzp_frm_typ in (40,440,1140,1983)

            #region Выбрать тариф на 1 м3 горячей воды на дом (горячая вода по счетчикам ), добавить сумму к тарифу  prm_2 nzp_prm=2003: nzp_frm_typ=40

            // наличие тарифа на 1 м3 ГВ на дом - nzp_prm=2003 - если есть, то прибавить к тарифу
            ret = ExecSQL(conn_db,
                " update t_prm set" +
                " tarif_m3=(" +
                  " select max(p.val_prm" + sConvToNum + ") from t_par2 p where p.nzp=t_prm.nzp_dom and p.nzp_prm=2003 " +
                  " and p.dat_s <= t_prm.dat_po and p.dat_po >= t_prm.dat_s " +
                " )" +
                " where nzp_frm_typ=40 " +
                "  and exists (" +
                  " select 1 from t_par2 p where p.nzp=t_prm.nzp_dom and p.nzp_prm=2003 " +
                  " and p.dat_s <= t_prm.dat_po and p.dat_po >= t_prm.dat_s " +
                ") "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            #endregion Выбрать тариф на 1 м3 горячей воды на дом (горячая вода по счетчикам ), добавить сумму к тарифу  prm_2 nzp_prm=2003: nzp_frm_typ=40

            #region Расчет для всех типов окончательного тарифа исходя из всех коэффициентов (по типу 40 = round(tarif_gkal*koef_gkal*norm_gkal*100)/100+tarif_m3)

            // расчитать окончательный тариф на 1м3 ГВС
            ret = ExecSQL(conn_db,
                " update t_prm set tarif=round(tarif_gkal*koef_gkal*norm_gkal*100)/100+tarif_m3 where nzp_frm_typ=40 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // расчитать окончательный тариф на 1ГКал ГВС - корректировка расходов потом!
            ret = ExecSQL(conn_db,
                " update t_prm set tarif=tarif_gkal where nzp_frm_typ in (440,1140,1814,1983,514) "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, sUpdStat + " t_prm ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            #endregion Расчет для всех типов окончательного тарифа исходя из всех коэффициентов (по типу 40 = round(tarif_gkal*koef_gkal*norm_gkal*100)/100+tarif_m3)

            return true;
        }
        #endregion Расчет тарифа по дополнительным параметрам тип расчета = 40,440,1140,1814,1983

        #endregion Типы расчетов тарифа

        #region Завершающие действия по выборке тарифов
        public bool CalcTarifsEndAction(IDbConnection conn_db, Gku gku, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            //
            //          Завершающие действия по выборке тарифов
            //
            #region тип расхода = 814 - Завершающие действия (+округление)
            // тип расхода = 814 - Завершающие действия
            ret = ExecSQL(conn_db,
                " update t_prm set tarif = tarif_gkal * " +
                "  (select " + sNvlWord + "(r.rsh1,0) from t_rsh814 r" +
                  " where t_prm.nzp_kvar=r.nzp_kvar and t_prm.nzp_frm=r.nzp_frm and t_prm.nzp_period=r.nzp_period and r.nzp_frm_typrs=814) " +
                " where nzp_frm_typ=814 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }


            //977|Не округлять тариф по отоплению|||bool||2||||
            bool bUchetNotRndOtopl =
                CheckValBoolPrm(conn_db, gku.paramcalc.data_alias, 977, "5");

            if (!bUchetNotRndOtopl)
            {
                int iRndTarifOtopl = 2;
                // округление до двух знаков
                ret = ExecSQL(conn_db,
                    " update t_prm set tarif = round(tarif," + iRndTarifOtopl + ") " +
                    " where nzp_frm_typ=814 "
                    , true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }
            }
            #endregion тип расхода = 814 - Завершающие действия (+округление)

            #region Завершающие действия по выборке тарифов(расчетная площадь, жилая , отапливаемая? количество жильцов)

            // расчетная площадь
            ret = ExecSQL(conn_db,
                " update t_prm set" +
                " squ=(" +
                  " select max(p.val" + sConvToNum + ")" +
                  " from t_par1 p" +
                  " where p.nzp=t_prm.nzp_kvar and p.nzp_prm=4" +
                  " and p.dat_s <= t_prm.dat_po and p.dat_po >= t_prm.dat_s " +
                  ")" +
                " where exists (" +
                  " select 1" +
                  " from t_par1 p" +
                  " where p.nzp=t_prm.nzp_kvar and p.nzp_prm=4" +
                  " and p.dat_s <= t_prm.dat_po and p.dat_po >= t_prm.dat_s " +
                  ")"
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            if (Points.IsSmr)
            {
                // в Самаре для коммуналок жилая площадь
                ret = ExecSQL(conn_db,
                    " update t_prm set" +
                    " squ=(" +
                    " select max(p.val" + sConvToNum + ")" +
                    " from t_par1 p" +
                    " where p.nzp=t_prm.nzp_kvar and p.nzp_prm=6" +
                    " and p.dat_s <= t_prm.dat_po and p.dat_po >= t_prm.dat_s " +
                    ")" +
                    " where exists (" +
                    " select 1" +
                    " from t_par1 p" +
                    " where p.nzp=t_prm.nzp_kvar and p.nzp_prm=6" +
                    " and p.dat_s <= t_prm.dat_po and p.dat_po >= t_prm.dat_s " +
                    ")" +
                    " and exists (select 1 from t_kmnl r where t_prm.nzp_kvar=r.nzp_kvar)"
                    , true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }
            }
            else
            {
                // в РТ для отопления отапливаемая площадь
                ret = ExecSQL(conn_db,
                    " update t_prm set" +
                    " squ=(" +
                    " select max(p.val" + sConvToNum + ")" +
                    " from t_par1 p" +
                    " where p.nzp=t_prm.nzp_kvar and p.nzp_prm=133" +
                    " and p.dat_s <= t_prm.dat_po and p.dat_po >= t_prm.dat_s " +
                    ")" +
                    " where exists (" +
                    " select 1" +
                    " from t_par1 p" +
                    " where p.nzp=t_prm.nzp_kvar and p.nzp_prm=133" +
                    " and p.dat_s <= t_prm.dat_po and p.dat_po >= t_prm.dat_s " +
                    ")" +
                    " and nzp_frm in (114, 814, 1814) "
                    , true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }
            }

            ret = ExecSQL(conn_db,
           " update t_prm set is_use_knp=1 " +
           " where exists (select 1" +
             " from " + gku.paramcalc.data_alias + "prm_11 p" +
             " where p.nzp=t_prm.nzp_supp" +
             "   and p.nzp_prm=1396 and p.val_prm='1'" +
             "   and p.dat_s  <= " + gku.paramcalc.dat_po +
             "   and p.dat_po >= " + gku.paramcalc.dat_s +
             " and p.is_actual<>100) "
           , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " update t_prm set is_use_ctr=1 " +
                " where not exists (select 1" +
                  " from " + gku.paramcalc.data_alias + "prm_10 p,temp_cnt_spis cs,t_tarifs t " +
                  " where p.nzp=0" +
                  "   and p.nzp_prm=1427 and p.val_prm='1'" +
                  "   and p.dat_s  <= " + gku.paramcalc.dat_po +
                  "   and p.dat_po >= " + gku.paramcalc.dat_s +
                  " and p.is_actual<>100 and t.nzp_kvar = t_prm.nzp_kvar and t.nzp_frm = t_prm.nzp_frm and t_prm.nzp_supp = t.nzp_supp " +
                " and cs.nzp_serv = t.nzp_serv and cs.nzp_type = 3 and cs.nzp = t_prm.nzp_kvar and coalesce(cs.dat_block,public.mdy(1,1,2001)) < " + gku.paramcalc.dat_s + " ) "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // расчетное кол-во жильцов
            ret = ExecSQL(conn_db,
                " update t_prm set" +
                " gil  =(select max(" + sNvlWord + "((case when t_prm.is_use_ctr = 1 then g.kg else g.kg_g end),0) + (case when t_prm.is_use_knp=1 then " + sNvlWord + "(g.kvp,0) else 0 end)) " +
                  " from t_rsh_kolgil g where g.nzp_kvar=t_prm.nzp_kvar and t_prm.dat_s<=g.dat_po and t_prm.dat_po>=g.dat_s)" +
                ",gil_g=(select max(" + sNvlWord + "(g.kg_g,0) + " + sNvlWord + "(g.kvp,0))" +
                  " from t_rsh_kolgil g where g.nzp_kvar=t_prm.nzp_kvar and t_prm.dat_s<=g.dat_po and t_prm.dat_po>=g.dat_s)" +
                " where exists (select 1 " +
                  " from t_rsh_kolgil g where g.nzp_kvar=t_prm.nzp_kvar and t_prm.dat_s<=g.dat_po and t_prm.dat_po>=g.dat_s)"
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            #endregion Завершающие действия по выборке тарифов(расчетная площадь, жилая , отапливаемая? количество жильцов)

            #region Выборка применяемых тарифов в соответствии с максимальным приоритетом (t_prm_max)

            // выборка применяемых тарифов
            ret = ExecSQL(conn_db,
                " create temp table t_prm_max (" +
                " nzp_kvar integer," +
                " nzp_supp integer," +
                " nzp_frm  integer," +
                " nzp_period integer," +
                " priority integer" +
                " )  " + sUnlogTempTable
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            ret = ExecSQL(conn_db,
                " insert into t_prm_max (nzp_kvar,nzp_supp,nzp_frm,nzp_period,priority)" +
                " select nzp_kvar,nzp_supp,nzp_frm,nzp_period,max(priority) from t_prm group by 1,2,3,4 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            ret = ExecSQL(conn_db, " create index ixt_prm_max on t_prm_max(nzp_kvar,nzp_supp,nzp_frm,nzp_period,priority) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            ret = ExecSQL(conn_db, sUpdStat + " t_prm_max ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            #endregion Выборка применяемых тарифов в соответствии с максимальным приоритетом

            return true;
        }
        #endregion Завершающие действия по выборке тарифов

        #region Типы расчетов расхода

        #region Тип расхода = 1 (просто расход квартирного параметра)
        public bool CalcTypeRashod_1(IDbConnection conn_db, Gku gku, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            ret = ExecSQL(conn_db,
                " insert into t_rsh (nzp_kvar,nzp_frm,nzp_prm, nzp_frm_typ,rashod,rashod_g,nzp_frm_typrs,valm" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn)" +
                " select t.nzp_kvar,t.nzp_frm,p.nzp_prm,f.nzp_frm_typ," +
                " max(p.val" + sConvToNum + "), max(p.val" + sConvToNum + "), 1, max(p.val" + sConvToNum + ")" +
                " ,t.nzp_period,max(t.dat_s),max(t.dat_po),max(t.cntd),max(t.cntd_mn)" +
                " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f,t_par1 p" +
                " where t.nzp_frm=f.nzp_frm and t.nzp_kvar=p.nzp and p.nzp_prm=f.nzp_prm_rash and f.nzp_frm_typrs=1" +
                " and p.dat_s <= t.dat_po and p.dat_po >= t.dat_s " +
                " group by 1,2,3,4,9 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            return true;
        }
        #endregion Тип расхода = 1 (просто расход квартирного параметра)

        #region Тип расхода =  2200 (взять расход из другой услуги)
        public bool CalcTypeRashod_2200(IDbConnection conn_db, Gku gku, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            // id услуги, расход которой будет копироваться лежит в nzp_prm_rash
            ret = ExecSQL(conn_db,
@" INSERT INTO t_rsh (nzp_kvar, nzp_frm, nzp_prm, rashod, rashod_g, is_device, nzp_frm_typrs, nzp_frm_typ, valm, rash_norm_one, rashod_norm, dop87, nzp_period, dat_s, dat_po, cntd, cntd_mn)
   SELECT t.nzp_kvar, t.nzp_frm, COALESCE(r2.nzp_prm, 0), COALESCE(r2.rashod, 0), COALESCE(r2.rashod_g, 0), 0 is_device, COALESCE(r2.nzp_frm_typrs, 2200), f.nzp_frm_typ, COALESCE(r2.valm, 0), COALESCE(r2.rash_norm_one, 0), COALESCE(r2.rashod_norm, 0), COALESCE(r2.dop87), t.nzp_period, t.dat_s, t.dat_po, t.cntd, t.cntd_mn
   FROM t_tarifs t
        INNER JOIN " + gku.paramcalc.kernel_alias + @"formuls_opis f ON t.nzp_frm = f.nzp_frm AND f.nzp_frm_typrs in (2200)
        LEFT JOIN t_tarifs t2 ON t.nzp_kvar = t2.nzp_kvar AND t.nzp_period = t2.nzp_period AND t2.nzp_serv = f.nzp_prm_rash
        LEFT JOIN t_rsh r2 ON r2.nzp_kvar = t2.nzp_kvar AND r2.nzp_frm = t2.nzp_frm AND t2.nzp_period = r2.nzp_period"
                , true);

            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            return true;
        }
        #endregion Тип расхода =  2200 (взять расход из другой услуги)

        #region Тип расхода = 2201 (0 расход)
        public bool CalcTypeRashod_2201(IDbConnection conn_db, Gku gku, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            ret = ExecSQL(conn_db, @"
INSERT INTO t_rsh (nzp_kvar, nzp_frm, nzp_prm, nzp_frm_typ, rashod, rashod_g, nzp_frm_typrs, valm, nzp_period, dat_s, dat_po, cntd, cntd_mn)
SELECT t.nzp_kvar, t.nzp_frm, 0 nzp_prm, f.nzp_frm_typ, 0 rashod, 0, f.nzp_frm_typrs, 0 valm, t.nzp_period, t.dat_s, t.dat_po, t.cntd, t.cntd_mn
FROM t_trf t
     INNER JOIN " + gku.paramcalc.kernel_alias + "formuls_opis f ON t.nzp_frm = f.nzp_frm AND f.nzp_frm_typrs = 2201",
                true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            return true;
        }
        #endregion Тип расхода = 2201 (0 расход)

        #region Тип расхода = 2202 (перемножение нескольких параметров (до 3-х, можно по ЛС и по Дому))
        public bool CalcTypeRashod_2202(IDbConnection conn_db, Gku gku, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            ret = ExecSQL(conn_db, string.Format(@"
CREATE TEMP TABLE t2202_pars (
  nzp_kvar INTEGER NOT NULL,
  nzp_prm INTEGER NOT NULL,
  val {0}(16,7) NOT NULL DEFAULT 0
)", sDecimalType), true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, string.Format(@"
INSERT INTO t2202_pars (nzp_kvar, nzp_prm, val)
SELECT t.nzp_kvar, p.nzp_prm, MAX(p.val{0})
FROM t_trf t
     INNER JOIN {1}formuls_opis f ON t.nzp_frm = f.nzp_frm AND f.nzp_frm_typrs = 2202
     INNER JOIN t_par1 p ON t.nzp_kvar = p.nzp
WHERE p.nzp_prm IN (f.nzp_prm_rash, f.nzp_prm_rash1, f.nzp_prm_rash2) AND p.dat_s <= t.dat_po and p.dat_po >= t.dat_s
GROUP BY nzp_kvar, nzp_prm", sConvToNum, gku.paramcalc.kernel_alias), true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }
            // id параметров из ЛС и дома не пересекаются, можно не проверять
            ret = ExecSQL(conn_db, string.Format(@"
INSERT INTO t2202_pars (nzp_kvar, nzp_prm, val)
SELECT t.nzp_kvar, p.nzp_prm, MAX(p.val_prm{0})
FROM t_trf t
     INNER JOIN {1}formuls_opis f ON t.nzp_frm = f.nzp_frm AND f.nzp_frm_typrs = 2202
     INNER JOIN t_par2 p ON t.nzp_dom = p.nzp
WHERE p.nzp_prm IN (f.nzp_prm_rash, f.nzp_prm_rash1, f.nzp_prm_rash2) AND p.dat_s <= t.dat_po and p.dat_po >= t.dat_s
GROUP BY nzp_kvar, nzp_prm", sConvToNum, gku.paramcalc.kernel_alias), true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            const string resultExpression = "COALESCE(p1.val, CASE WHEN f.nzp_prm_rash = 0 THEN 1 ELSE 0 END) * COALESCE(p2.val, CASE WHEN f.nzp_prm_rash1 = 0 THEN 1 ELSE 0 END) * COALESCE(p3.val, CASE WHEN f.nzp_prm_rash2 = 0 THEN 1 ELSE 0 END)";
            ret = ExecSQL(conn_db, string.Format(@"
INSERT INTO t_rsh (nzp_kvar, nzp_frm, nzp_prm, nzp_frm_typ, rashod, rashod_g, nzp_frm_typrs, valm, nzp_period, dat_s, dat_po, cntd, cntd_mn)
SELECT t.nzp_kvar, t.nzp_frm, 0 nzp_prm, f.nzp_frm_typ, {1}, {1}, f.nzp_frm_typrs, {1}, t.nzp_period, t.dat_s, t.dat_po, t.cntd, t.cntd_mn
FROM t_trf t
     INNER JOIN {0}formuls_opis f ON t.nzp_frm = f.nzp_frm AND f.nzp_frm_typrs = 2202
     LEFT JOIN t2202_pars p1 ON t.nzp_kvar = p1.nzp_kvar AND p1.nzp_prm = f.nzp_prm_rash
     LEFT JOIN t2202_pars p2 ON t.nzp_kvar = p2.nzp_kvar AND p2.nzp_prm = f.nzp_prm_rash1
     LEFT JOIN t2202_pars p3 ON t.nzp_kvar = p3.nzp_kvar AND p3.nzp_prm = f.nzp_prm_rash2
WHERE f.nzp_prm_rash != 0 OR f.nzp_prm_rash1 != 0 OR f.nzp_prm_rash2 != 0", gku.paramcalc.kernel_alias, resultExpression)
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ExecSQL(conn_db, @"DROP TABLE t2202_pars", true);

            return true;
        }
        #endregion Тип расхода = 2202 (перемножение нескольких параметров (до 3-х, можно по ЛС и по Дому))

        #region Тип расхода = 2203 (расход квартирного параметра при наличии показания ОДПУ в текущем месяце)
        public bool CalcTypeRashod_2203(IDbConnection conn_db, Gku gku, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            if (!gku.paramcalc.ExistsCounters) return true;
            // тип ОДПУ, показание которого нужно проверять лежит в nzp_prm_rash1
            var checkDate = new DateTime(gku.paramcalc.calc_yy, gku.paramcalc.calc_mm, 1).AddMonths(1);
            ret = ExecSQL(conn_db, string.Format(@"
WITH rsh2203 AS (
    SELECT t.nzp_kvar, t.nzp_frm, p.nzp_prm, f.nzp_frm_typ, f.nzp_frm_typrs, t.nzp_period, t.dat_s, t.dat_po, t.cntd, t.cntd_mn,
           CASE WHEN EXISTS (SELECT 1 
                             FROM temp_cnt_spis tcs
                                  LEFT JOIN temp_counters_dom tcd ON tcs.nzp_counter = tcd.nzp_counter
                                  LEFT JOIN temp_counters_dom_gkal tcdg ON tcdg.nzp_counter = tcdg.nzp_counter
                             WHERE t.nzp_dom = tcs.nzp AND tcs.nzp_cnt = f.nzp_prm_rash1 AND COALESCE(tcd.dat_uchet, tcdg.dat_uchet) = {2}) THEN p.val{0} ELSE 0 END volume
    FROM t_trf t 
         INNER JOIN {1}formuls_opis f ON t.nzp_frm = f.nzp_frm AND f.nzp_frm_typrs = 2203
         INNER JOIN t_par1 p ON t.nzp_kvar = p.nzp AND p.nzp_prm = f.nzp_prm_rash
    WHERE p.dat_s <= t.dat_po AND p.dat_po >= t.dat_s
)
INSERT INTO t_rsh (nzp_kvar, nzp_frm, nzp_prm, nzp_frm_typ, rashod, rashod_g, nzp_frm_typrs, valm, nzp_period, dat_s, dat_po, cntd, cntd_mn)
SELECT r.nzp_kvar, r.nzp_frm, r.nzp_prm, r.nzp_frm_typ, r.volume, r.volume, r.nzp_frm_typrs, r.volume, r.nzp_period, r.dat_s, r.dat_po, r.cntd, r.cntd_mn
FROM rsh2203 r", sConvToNum, gku.paramcalc.kernel_alias, MDY(checkDate.Month, 1, checkDate.Year))
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            return true;
        }
        #endregion Тип расхода = 2203 (расход квартирного параметра при наличии показания ОДПУ в текущем месяце)


        #region Тип расхода = 101 (расход квартирного параметра с вычитанием другого квартирного параметра)
        public bool CalcTypeRashod_101(IDbConnection conn_db, Gku gku, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            ret = ExecSQL(conn_db,
                " create temp table t_rsh101 (" +
                " nzp_kvar integer," +
                " nzp_frm  integer," +
                " nzp_prm  integer," +
                " nzp_prm_minus integer," +
                " nzp_frm_typ   integer default 0 not null," +
                " nzp_frm_typrs integer default 0 not null," +
                " valm       " + sDecimalType + "(16,7) default 0," +
                " valm_first " + sDecimalType + "(16,7) default 0," +
                " valm_minus " + sDecimalType + "(16,7) default 0," +
                " nzp_period integer," +
                " dat_s    DATE," +
                " dat_po   DATE," +
                " cntd     integer, " +
                " cntd_mn  integer  " +
                " )  " + sUnlogTempTable
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // выбрать неотапливаемую площадь для отопления - nzp_serv=8 and nzp_prm=2010
            ret = ExecSQL(conn_db,
                " insert into t_rsh101 (nzp_kvar,nzp_frm,nzp_prm,nzp_frm_typ,nzp_frm_typrs,valm_first,nzp_prm_minus" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn)" +
                " select t.nzp_kvar,t.nzp_frm,p.nzp_prm,f.nzp_frm_typ, 101, max(p.val" + sConvToNum + "),max(f.nzp_prm_rash1)" +
                " ,t.nzp_period,max(t.dat_s),max(t.dat_po),max(t.cntd),max(t.cntd_mn)" +
                " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f,t_par1 p" +
                " where t.nzp_frm=f.nzp_frm and t.nzp_kvar=p.nzp and p.nzp_prm=f.nzp_prm_rash and f.nzp_frm_typrs=101" +
                " and p.dat_s <= t.dat_po and p.dat_po >= t.dat_s " +
                " group by 1,2,3,4,8 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, " create index ixt_rsh101 on t_rsh101(nzp_kvar,nzp_prm_minus) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, sUpdStat + " t_rsh101 ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " update t_rsh101 set valm_minus =" +
                " (select max(p.val_prm" + sConvToNum + ") from ttt_prm_1d p" +
                 " where p.nzp=t_rsh101.nzp_kvar and p.nzp_prm=t_rsh101.nzp_prm_minus " +
                   " and p.dat_s <= t_rsh101.dat_po and p.dat_po >= t_rsh101.dat_s )" +
                " where exists " +
                " (select 1 from ttt_prm_1d p" +
                "  where p.nzp=t_rsh101.nzp_kvar and p.nzp_prm=t_rsh101.nzp_prm_minus " +
                "    and p.dat_s <= t_rsh101.dat_po and p.dat_po >= t_rsh101.dat_s )"
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " update t_rsh101 set valm = case when valm_first - valm_minus > 0 then valm_first - valm_minus else 0 end " +
                " where 1=1 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " insert into t_rsh (nzp_kvar,nzp_frm,nzp_prm, nzp_frm_typ,rashod,rashod_g,nzp_frm_typrs,valm,rsh1" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn) " +
                " select nzp_kvar,nzp_frm,nzp_prm, nzp_frm_typ,valm,valm,nzp_frm_typrs,valm,valm_first" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn " +
                " from t_rsh101 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ExecSQL(conn_db, " Drop table t_rsh101 ", false);

            return true;
        }
        #endregion Тип расхода = 101 (расход квартирного параметра с вычитанием другого квартирного параметра)

        #region Тип расхода = 2,4 (тариф на квартиру)
        public bool CalcTypeRashod_2_4(IDbConnection conn_db, Gku gku, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            #region Тип расхода = 2 (тариф на квартиру - с учетом доли расхода коммунальных квартир)
            // тип расхода = 2
            ret = ExecSQL(conn_db,
                " insert into t_rsh (nzp_kvar,nzp_frm,nzp_prm,nzp_frm_typ,rashod,rashod_g,nzp_frm_typrs,valm" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn)" +
                " select t.nzp_kvar,t.nzp_frm,0 nzp_prm,f.nzp_frm_typ,1 rashod,1,2,1 valm" +
                " ,t.nzp_period,t.dat_s,t.dat_po,t.cntd,t.cntd_mn" +
                " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f" +
                " where t.nzp_frm=f.nzp_frm and f.nzp_frm_typrs=2 " +
                " and not exists (select 1 from t_rsh_kmnl r where t.nzp_kvar=r.nzp_kvar) "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " insert into t_rsh (nzp_kvar,nzp_frm,nzp_prm,nzp_frm_typ,rashod,rashod_g,nzp_frm_typrs,rsh1" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn)" +
                " select t.nzp_kvar,t.nzp_frm,r.nzp_prm,f.nzp_frm_typ,r.rashod,r.rashod,2,r.val_prm" +
                " ,t.nzp_period,t.dat_s,t.dat_po,t.cntd,t.cntd_mn" +
                " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f,t_rsh_kmnl r" +
                " where t.nzp_kvar=r.nzp_kvar and t.nzp_frm=f.nzp_frm and f.nzp_frm_typrs=2 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            #endregion Тип расхода = 2 (тариф на квартиру - с учетом доли расхода коммунальных квартир)

            #region Тип расхода = 4 - расход = 1 - тариф на квартиру

            // тип расхода = 4
            ret = ExecSQL(conn_db,
                " insert into t_rsh (nzp_kvar,nzp_frm,nzp_prm,nzp_frm_typ,rashod,rashod_g,nzp_frm_typrs,valm" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn)" +
                " select t.nzp_kvar,t.nzp_frm,0 nzp_prm,f.nzp_frm_typ,1 rashod,1,4,1 valm" +
                " ,t.nzp_period,t.dat_s,t.dat_po,t.cntd,t.cntd_mn" +
                " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f" +
                " where t.nzp_frm=f.nzp_frm and f.nzp_frm_typrs=4 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            #endregion Тип расхода = 4 - расход = 1 - тариф на квартиру

            return true;
        }
        #endregion Тип расхода = 2,4 (тариф на квартиру)

        #region Тип расхода = 11,1814 (с учетом индивидуального расхода коммунальных квартир)
        public bool CalcTypeRashod_11_1814(IDbConnection conn_db, Gku gku, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            // тип расхода = 11,1814
            ret = ExecSQL(conn_db,
                " insert into t_rsh (nzp_kvar,nzp_frm,nzp_prm,nzp_frm_typ,nzp_frm_typrs,rashod,rashod_g,valm" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn)" +
                " select t.nzp_kvar,t.nzp_frm,p.nzp_prm,f.nzp_frm_typ,f.nzp_frm_typrs," +
                " max(p.val" + sConvToNum + "), max(p.val" + sConvToNum + "), max(p.val" + sConvToNum + ")" +
                " ,t.nzp_period,max(t.dat_s),max(t.dat_po),max(t.cntd),max(t.cntd_mn)" +
                " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f,t_par1 p" +
                " where t.nzp_frm=f.nzp_frm and t.nzp_kvar=p.nzp and p.nzp_prm=f.nzp_prm_rash and f.nzp_frm_typrs in (11,1814)" +
                " and p.dat_s <= t.dat_po and p.dat_po >= t.dat_s " +
                " and not exists (select 1 from t_kmnl r where t.nzp_kvar=r.nzp_kvar)" +
                " group by 1,2,3,4,5,9 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " insert into t_rsh (nzp_kvar,nzp_frm,nzp_prm,nzp_frm_typ,nzp_frm_typrs,rashod,rashod_g,valm" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn)" +
                " select t.nzp_kvar,t.nzp_frm,p.nzp_prm,f.nzp_frm_typ,f.nzp_frm_typrs," +
                " max(p.val" + sConvToNum + "), max(p.val" + sConvToNum + "), max(p.val" + sConvToNum + ")" +
                " ,t.nzp_period,max(t.dat_s),max(t.dat_po),max(t.cntd),max(t.cntd_mn)" +
                " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f,t_par1 p" +
                " where t.nzp_frm=f.nzp_frm and t.nzp_kvar=p.nzp and p.nzp_prm=f.nzp_prm_rash1 and f.nzp_frm_typrs in (11,1814)" +
                " and p.dat_s <= t.dat_po and p.dat_po >= t.dat_s " +
                " and exists (select 1 from t_kmnl r where t.nzp_kvar=r.nzp_kvar)" +
                " group by 1,2,3,4,5,9 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            return true;
        }
        #endregion Тип расхода = 11,1814 (с учетом индивидуального расхода коммунальных квартир)

        #region Тип расхода = 3,7,106,300,512 (количество проживающих)
        public bool CalcTypeRashod_3_7_106_300_512(IDbConnection conn_db, Gku gku, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            #region Тип расхода = 106 (количество жильцов + временно проживающие - временно выбывшие - не пользуются лифтом)

            ret = ExecSQL(conn_db,
                " insert into t_rsh (nzp_kvar,nzp_frm,nzp_prm,nzp_frm_typ,rashod,nzp_frm_typrs,rsh1,rashod_g,valm" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn)" +
                " select t.nzp_kvar,t.nzp_frm,5 nzp_prm,f.nzp_frm_typ," +
                " max(" + sNvlWord + "((case when t.is_use_ctr = 1 then g.kg else g.kg_g end),0))  + max(" + sNvlWord + "(g.kvp,0)) - max(" + sNvlWord + "(g.knpl,0)) + max(case when t.is_use_knp=1 then " + sNvlWord + "(g.knp,0) else 0 end)," +
                " 106," +
                " max(" + sNvlWord + "(g.knpl,0))," +
                " max(" + sNvlWord + "(g.kg_g,0))+ max(" + sNvlWord + "(g.kvp,0)) - max(" + sNvlWord + "(g.knpl,0)) + max(case when t.is_use_knp=1 then " + sNvlWord + "(g.knp,0) else 0 end)," +
                " max(" + sNvlWord + "((case when t.is_use_ctr = 1 then g.kg else g.kg_g end),0))  + max(" + sNvlWord + "(g.kvp,0)) - max(" + sNvlWord + "(g.knpl,0)) + max(case when t.is_use_knp=1 then " + sNvlWord + "(g.knp,0) else 0 end)" +
                " ,t.nzp_period, max(t.dat_s),max(t.dat_po),max(t.cntd),max(t.cntd_mn)" +
                " from " + gku.paramcalc.kernel_alias + "formuls_opis f, t_trf t" +
#if PG
 " left outer join t_rsh_kolgil g on t.nzp_kvar=g.nzp_kvar where " +
#else
                ",outer t_rsh_kolgil g where t.nzp_kvar=g.nzp_kvar and" +
#endif
 " t.nzp_frm=f.nzp_frm and f.nzp_frm_typrs=106 " +
                " and t.dat_s <= g.dat_po and t.dat_po >= g.dat_s" +
                " group by 1,2,3,4,10 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            #endregion Тип расхода = 106 (количество жильцов + временно проживающие - временно выбывшие - не пользуются лифтом)

            #region Тип расхода = 3 (количество жильцов + временно проживающие УЧИТЫВАЯ временно выбывших и др человечьи)

            ret = ExecSQL(conn_db,
                " insert into t_rsh (nzp_kvar,nzp_frm,nzp_prm,nzp_frm_typ,rashod,nzp_frm_typrs,rashod_g,valm" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn)" +
                " select t.nzp_kvar,t.nzp_frm,5 nzp_prm,f.nzp_frm_typ," +
                "max(" + sNvlWord + "((case when t.is_use_ctr = 1 then g.kg else g.kg_g end),0)) + max(" + sNvlWord + "(g.kvp,0)) + max(case when t.is_use_knp=1 then " + sNvlWord + "(g.knp,0) else 0 end)," +
                "3," +
                "max(" + sNvlWord + "(g.kg_g,0)) + max(" + sNvlWord + "(g.kvp,0)) + max(case when t.is_use_knp=1 then " + sNvlWord + "(g.knp,0) else 0 end)," +
                "max(" + sNvlWord + "((case when t.is_use_ctr = 1 then g.kg else g.kg_g end),0)) + max(" + sNvlWord + "(g.kvp,0)) + max(case when t.is_use_knp=1 then " + sNvlWord + "(g.knp,0) else 0 end)" +
                " ,t.nzp_period, max(t.dat_s),max(t.dat_po),max(t.cntd),max(t.cntd_mn)" +
                " from " + gku.paramcalc.kernel_alias + "formuls_opis f,t_trf t " +
#if PG
 " left outer join t_rsh_kolgil g on t.nzp_kvar=g.nzp_kvar where " +
#else
                ",outer t_rsh_kolgil g where t.nzp_kvar=g.nzp_kvar and " +
#endif
 " t.nzp_frm=f.nzp_frm and f.nzp_frm_typrs=3 " +
                " and t.dat_s <= g.dat_po and t.dat_po >= g.dat_s" +
                " group by 1,2,3,4,9 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            #endregion Тип расхода = 3 (количество жильцов + временно проживающие УЧИТЫВАЯ временно выбывших и др человечьи)

            #region Тип расхода = 300 (количество жильцов + временно проживающие НЕ УЧИТЫВАЯ временно выбывших и др человечьи)

            ret = ExecSQL(conn_db,
                " insert into t_rsh (nzp_kvar,nzp_frm,nzp_prm,nzp_frm_typ,rashod,nzp_frm_typrs,rashod_g,valm" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn)" +
                " select t.nzp_kvar,t.nzp_frm,5 nzp_prm,f.nzp_frm_typ," +
                "max(" + sNvlWord + "(g.kg_g,0)) + max(" + sNvlWord + "(g.kvp,0)) + max(case when t.is_use_knp=1 then " + sNvlWord + "(g.knp,0) else 0 end)," +
                "300," +
                "max(" + sNvlWord + "(g.kg_g,0)) + max(" + sNvlWord + "(g.kvp,0)) + max(case when t.is_use_knp=1 then " + sNvlWord + "(g.knp,0) else 0 end)," +
                "max(" + sNvlWord + "(g.kg_g,0)) + max(" + sNvlWord + "(g.kvp,0)) + max(case when t.is_use_knp=1 then " + sNvlWord + "(g.knp,0) else 0 end)" +
                " ,t.nzp_period, max(t.dat_s),max(t.dat_po),max(t.cntd),max(t.cntd_mn)" +
                " from " + gku.paramcalc.kernel_alias + "formuls_opis f,t_trf t " +
#if PG
 " left outer join t_rsh_kolgil g on t.nzp_kvar=g.nzp_kvar where " +
#else
                ",outer t_rsh_kolgil g where t.nzp_kvar=g.nzp_kvar and " +
#endif
 " t.nzp_frm=f.nzp_frm and f.nzp_frm_typrs=300 " +
                " and t.dat_s <= g.dat_po and t.dat_po >= g.dat_s" +
                " group by 1,2,3,4,9 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            #endregion Тип расхода = 300 (количество жильцов + временно проживающие НЕ УЧИТЫВАЯ временно выбывших и др человечьи)

            #region Тип расхода = 7 -- для Самары Эл/эн лифтов от кол-ва жильцов, если есть эл/снабжение

            // тип расхода = 7 -- для Самары Эл/эн лифтов от кол-ва жильцов, если есть эл/снабжение
            ret = ExecSQL(conn_db,
                " insert into t_rsh (nzp_kvar,nzp_frm,nzp_prm,nzp_frm_typ,rashod,nzp_frm_typrs,rashod_g,valm" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn)" +
                " select t.nzp_kvar,t.nzp_frm,p.nzp_prm,f.nzp_frm_typ," +
                " max( (p.val_prm" + sConvToNum + ") * (" + sNvlWord + "((case when t.is_use_ctr = 1 then g.kg else g.kg_g end),0) + " + sNvlWord + "(g.kvp,0) + (case when t.is_use_knp=1 then " + sNvlWord + "(g.knp,0) else 0 end)) )," +
                "7," +
                " max( (p.val_prm" + sConvToNum + ") * (" + sNvlWord + "(g.kg_g,0) + " + sNvlWord + "(g.kvp,0) + (case when t.is_use_knp=1 then " + sNvlWord + "(g.knp,0) else 0 end)) )," +
                " max( (p.val_prm" + sConvToNum + ") * (" + sNvlWord + "((case when t.is_use_ctr = 1 then g.kg else g.kg_g end),0) + " + sNvlWord + "(g.kvp,0) + (case when t.is_use_knp=1 then " + sNvlWord + "(g.knp,0) else 0 end)) )  " +
                " ,t.nzp_period,max(t.dat_s),max(t.dat_po),max(t.cntd),max(t.cntd_mn)" +
                " from " + gku.paramcalc.kernel_alias + "formuls_opis f," + gku.paramcalc.data_alias + "prm_5 p, t_trf t" +
#if PG
 " left outer join t_rsh_kolgil g on t.nzp_kvar=g.nzp_kvar  where" +
#else
                ",outer t_rsh_kolgil g where t.nzp_kvar=g.nzp_kvar and " +
#endif
 " t.nzp_frm=f.nzp_frm and p.nzp_prm=f.nzp_prm_rash and f.nzp_frm_typrs=7" +
                "   and p.is_actual<>100 and p.dat_s<" + gku.dat_po + " and p.dat_po>=" + gku.dat_s + " " +
                "   and exists (select 1 from " + gku.counters_xx + " s where t.nzp_kvar=s.nzp_kvar and s.nzp_type=3 and s.stek=3 " +
                "        and s.nzp_serv=25 and s.rashod>0) " +
                " group by 1,2,3,4,9 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            #endregion Тип расхода = 7 -- для Самары Эл/эн лифтов от кол-ва жильцов, если есть эл/снабжение

            #region Тип расхода = 512-- Эл/эн лифтов от кол-ва жильцов

            // тип расхода = 512-- Эл/эн лифтов от кол-ва жильцов
            // кол-во жильцов
            ret = ExecSQL(conn_db,
                " insert into t_rsh (nzp_kvar,nzp_frm,nzp_prm,nzp_frm_typ,rsh2,nzp_frm_typrs,rsh2_g" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn)" +
                " select t.nzp_kvar,t.nzp_frm,0 nzp_prm,f.nzp_frm_typ," +
                " max(" + sNvlWord + "((case when t.is_use_ctr = 1 then g.kg else g.kg_g end),0)) + max(" + sNvlWord + "(g.kvp,0)) - max(" + sNvlWord + "(g.knpl,0)) + max(case when t.is_use_knp=1 then " + sNvlWord + "(g.knp,0) else 0 end)," +
                "512," +
                " max(" + sNvlWord + "(g.kg_g,0))+ max(" + sNvlWord + "(g.kvp,0)) - max(" + sNvlWord + "(g.knpl,0)) + max(case when t.is_use_knp=1 then " + sNvlWord + "(g.knp,0) else 0 end)" +
                " ,t.nzp_period,max(t.dat_s),max(t.dat_po),max(t.cntd),max(t.cntd_mn)" +
                " from " + gku.paramcalc.kernel_alias + "formuls_opis f, t_trf t" +
#if PG
 " left outer join t_rsh_kolgil g on t.nzp_kvar=g.nzp_kvar where " +
#else
                ",outer t_rsh_kolgil g where t.nzp_kvar=g.nzp_kvar and " +
#endif
 " t.nzp_frm=f.nzp_frm and f.nzp_frm_typrs=512" +
                " group by 1,2,3,4,8 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // 1я норма на 1 человека - на базу
            ret = ExecSQL(conn_db,
                " update t_rsh set " +
                " nzp_prm=(select max(f.nzp_prm_rash)" +
                "   from " + gku.paramcalc.kernel_alias + "formuls_opis f where f.nzp_frm_typrs=512)," +
                " rsh1=  (select max(" + sNvlWord + "(p.val_prm,'0')" + sConvToNum + ")" +
                "   from " + gku.paramcalc.kernel_alias + "formuls_opis f," + gku.paramcalc.data_alias + "prm_5 p" +
                "   where p.nzp_prm=f.nzp_prm_rash and f.nzp_frm_typrs=512" +
                "   and p.is_actual<>100 and p.dat_s <= t_rsh.dat_po and p.dat_po >= t_rsh.dat_s) " +
                " where nzp_frm_typrs=512 and " +
                "  exists (select 1 " +
                "   from " + gku.paramcalc.kernel_alias + "formuls_opis f," + gku.paramcalc.data_alias + "prm_5 p" +
                "   where p.nzp_prm=f.nzp_prm_rash and f.nzp_frm_typrs=512" +
                "   and p.is_actual<>100 and p.dat_s <= t_rsh.dat_po and p.dat_po >= t_rsh.dat_s) "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // 2я норма на 1 человека - на л/с среднее
            ret = ExecSQL(conn_db,
                " update t_rsh set" +
                " nzp_prm=(select max(f.nzp_prm_rash1)" +
                "   from " + gku.paramcalc.kernel_alias + "formuls_opis f where f.nzp_frm_typrs=512)," +
                " rsh1=" +
                "  (select max(" + sNvlWord + "(p.val,'0')" + sConvToNum + ")" +
                "   from " + gku.paramcalc.kernel_alias + "formuls_opis f,t_par1 p" +
                "   where p.nzp_prm=f.nzp_prm_rash1 and f.nzp_frm_typrs=512" +
                "   and p.dat_s <= t_rsh.dat_po and p.dat_po >= t_rsh.dat_s) " +
                " where nzp_frm_typrs=512 and " +
                "  exists (select 1 " +
                "   from " + gku.paramcalc.kernel_alias + "formuls_opis f,t_par1 p" +
                "   where p.nzp_prm=f.nzp_prm_rash1 and f.nzp_frm_typrs=512" +
                "   and p.dat_s <= t_rsh.dat_po and p.dat_po >= t_rsh.dat_s) "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // 3я норма на 1 человека - на л/с изменение
            ret = ExecSQL(conn_db,
                " update t_rsh set" +
                " nzp_prm=(select max(f.nzp_prm_rash2)" +
                "   from " + gku.paramcalc.kernel_alias + "formuls_opis f where f.nzp_frm_typrs=512)," +
                " rsh1=" +
                "  (select max(" + sNvlWord + "(p.val,'0')" + sConvToNum + ")" +
                "   from " + gku.paramcalc.kernel_alias + "formuls_opis f,t_par1 p" +
                "   where p.nzp_prm=f.nzp_prm_rash2 and f.nzp_frm_typrs=512" +
                "   and p.dat_s <= t_rsh.dat_po and p.dat_po >= t_rsh.dat_s) " +
                " where nzp_frm_typrs=512 and " +
                "  exists (select 1 " +
                "   from " + gku.paramcalc.kernel_alias + "formuls_opis f,t_par1 p" +
                "   where p.nzp_prm=f.nzp_prm_rash2 and f.nzp_frm_typrs=512" +
                "   and p.dat_s <= t_rsh.dat_po and p.dat_po >= t_rsh.dat_s) "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // расход = норма на 1 человека * кол-во жильцов
            ret = ExecSQL(conn_db,
                " update t_rsh set rashod=rsh1*rsh2,rashod_g=rsh1*rsh2_g,valm=rsh1*rsh2 " +
                " where nzp_frm_typrs=512 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            #endregion Тип расхода = 512-- Эл/эн лифтов от кол-ва жильцов

            return true;
        }
        #endregion Тип расхода = 3,7,106,300,512 (количество проживающих)

        #region Тип расхода = 509 - вода для домашних животных по видам скота
        public bool CalcTypeRashod_509(IDbConnection conn_db, Gku gku, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            // выбрать перечень ЛС
            ExecSQL(conn_db, " drop table t_z509 ", false);
            ret = ExecSQL(conn_db,
                " create temp table t_z509 (" +
                " nzp_kvar integer," +
                " nzp_frm  integer," +
                " nzp_frm_typ integer," +
                " nzp_period integer," +
                " dat_s    DATE," +
                " dat_po   DATE," +
                " cntd     integer, " +
                " cntd_mn  integer  " +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " insert into t_z509 (nzp_kvar,nzp_frm,nzp_frm_typ" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn)" +
                " select t.nzp_kvar,t.nzp_frm,f.nzp_frm_typ " +
                " ,t.nzp_period,max(t.dat_s),max(t.dat_po),max(t.cntd),max(t.cntd_mn)" +
                " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f" +
                " where t.nzp_frm=f.nzp_frm and f.nzp_frm_typrs=509" +
                " group by 1,2,3,4 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ExecSQL(conn_db, sUpdStat + " t_z509 ", true);

            //ViewTbl(conn_db, " select * from t_z509 order by nzp_kvar ");

            // выбрать параметры нормы расхода на 1 ед.(ГОЛОВУ!) количества животних
            ExecSQL(conn_db, " drop table t_k509 ", false);
            ret = ExecSQL(conn_db,
                " create temp table t_k509 (" +
                " ipos integer," +
                " nzp_prmk integer," +
                " valk " + sDecimalType + "(14,7) default 0" +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " insert into t_k509 (ipos,nzp_prmk)" +
                " select trunc(p.order/4) ipos,p.nzp_prm " +
                " from " + gku.paramcalc.kernel_alias + "prm_frm p " +
                " where p.nzp_frm = 509 and p.frm_calc = 2 and mod(p.order-1,4)=0 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ExecSQL(conn_db, sUpdStat + " t_k509 ", true);

            //ViewTbl(conn_db, " select * from t_k509 ");

            // выбрать параметры количества животних
            ExecSQL(conn_db, " drop table t_r509 ", false);
            ret = ExecSQL(conn_db,
                " create temp table t_r509 (" +
                " ipos integer," +
                " nzp_prmr integer," +
                " valr " + sDecimalType + "(14,7) default 0" +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " insert into t_r509 (ipos,nzp_prmr)" +
                " select trunc(p.order/4) ipos,p.nzp_prm " +
                " from " + gku.paramcalc.kernel_alias + "prm_frm p " +
                " where p.nzp_frm = 509 and p.frm_calc = 2 and mod(p.order+1,4)=0 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ExecSQL(conn_db, sUpdStat + " t_r509 ", true);

            //ViewTbl(conn_db, " select * from t_r509 ");

            ExecSQL(conn_db, " drop table t_v509 ", false);

            ret = ExecSQL(conn_db,
                " create temp table t_v509 (" +
                " nzp_kvar integer," +
                " nzp_frm  integer," +
                " nzp_frm_typ integer," +
                " ipos     integer," +
                " nzp_prmr integer," +
                " nzp_prmk integer," +
                " valk " + sDecimalType + "(14,7) default 0," +
                " valr " + sDecimalType + "(14,7) default 0," +
                " valm " + sDecimalType + "(14,7) default 0," +
                " nzp_period integer," +
                " dat_s    DATE," +
                " dat_po   DATE," +
                " cntd     integer, " +
                " cntd_mn  integer  " +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }


            ret = ExecSQL(conn_db,
                " Insert into t_v509 " +
                " (nzp_kvar,nzp_frm,nzp_frm_typ,ipos,nzp_prmr,nzp_prmk" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn)" +
                " select z.nzp_kvar,z.nzp_frm,z.nzp_frm_typ,a.ipos,a.nzp_prmr,b.nzp_prmk " +
                " ,z.nzp_period,z.dat_s,z.dat_po,z.cntd,z.cntd_mn " +
                " from t_z509 z, t_r509 a, t_k509 b " +
                " where a.ipos=b.ipos "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ExecSQL(conn_db, " Create index ix_t_v509 on t_v509 (nzp_kvar,dat_s,dat_po) ", true);
            ExecSQL(conn_db, sUpdStat + " t_v509 ", true);

            ret = ExecSQL(conn_db,
                " update t_v509 set " +
                " valk=(select max(p.val_prm" + sConvToNum + ") from " + gku.paramcalc.data_alias + "prm_5 p where p.nzp_prm=t_v509.nzp_prmk" +
                       " and p.is_actual<>100 and p.dat_s <= t_v509.dat_po and p.dat_po >= t_v509.dat_s), " +
                " valr=(select max(p.val" + sConvToNum + ") from t_par1 p where p.nzp=t_v509.nzp_kvar and p.nzp_prm=t_v509.nzp_prmr" +
                       " and p.dat_s <= t_v509.dat_po and p.dat_po >= t_v509.dat_s) " +
                " where exists (select 1 from t_par1 p where p.nzp=t_v509.nzp_kvar and p.nzp_prm=t_v509.nzp_prmr" +
                       " and p.dat_s <= t_v509.dat_po and p.dat_po >= t_v509.dat_s) "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, " update t_v509 set valk = 0 where valk is null ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, " update t_v509 set valm = valk * valr where 1=1 ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            //ViewTbl(conn_db, " select * from t_par1 order by nzp ");

            //ViewTbl(conn_db, "select * from " + gku.paramcalc.data_alias + "prm_5 p where p.nzp_prm in (318,319,320,321,467,948,1108,1109,1110,1111)" +
            //           " and p.is_actual<>100 and p.dat_s<" + gku.dat_po + " and p.dat_po>=" + gku.dat_s + " ");

            ViewTbl(conn_db, " select * from t_v509 order by nzp_kvar ");

            ret = ExecSQL(conn_db,
                " insert into t_rsh (nzp_kvar,nzp_frm,nzp_prm,nzp_frm_typ,rashod,rashod_g,nzp_frm_typrs,valm" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn)" +
                " select t.nzp_kvar,t.nzp_frm,0 nzp_prm,t.nzp_frm_typ,sum(t.valm),sum(t.valm),509,sum(t.valm)" +
                " ,t.nzp_period,max(t.dat_s),max(t.dat_po),max(t.cntd),max(t.cntd_mn) " +
                " from t_v509 t " +
                " group by 1,2,3,4,9 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }
            ViewTbl(conn_db, " select * from t_rsh  ");
            ret = ExecSQL(conn_db, " drop table t_v509 ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }
            ret = ExecSQL(conn_db, " drop table t_z509 ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }
            ret = ExecSQL(conn_db, " drop table t_r509 ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }
            ret = ExecSQL(conn_db, " drop table t_k509 ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            return true;
        }
        #endregion Тип расхода = 509 - вода для домашних животных по видам скота

        #region Тип расхода = 5 (расход коммунальной услуги + ОДН)
        public bool CalcTypeRashod_5(IDbConnection conn_db, Gku gku, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            // тип расхода = 5

            string d_dat_charge = " and s.dat_charge is null ";
            if (!gku.paramcalc.b_cur)
            {
                d_dat_charge = " and s.dat_charge = " + MDY(gku.paramcalc.cur_mm, 28, gku.paramcalc.cur_yy);
            }
            #region нет расчета по Пост
            // 
            ret = ExecSQL(conn_db,
                " insert into t_rsh (nzp_kvar,nzp_frm,nzp_prm,rashod,rashod_g,is_device,nzp_frm_typrs,nzp_frm_typ,dlt_reval,valm,dop87,rash_norm_one" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn,rashod_source,up_kf,kod_info_gkal9,dop87_gkal9,kf307_gkal9)" +
                " select t.nzp_kvar,t.nzp_frm,0 nzp_prm,max(case when s.rashod>0 then s.rashod else 0 end)," +
                " max(case when s.cnt_stage in (1,9) then (case when s.rashod>0 then s.rashod else 0 end) else s.val1_g end)," +
                " max(s.cnt_stage),f.nzp_frm_typrs,f.nzp_frm_typ,max(s.dlt_reval),max(s.val2 + s.val1),min(s.dop87),max(rash_norm_one) " +
                " ,t.nzp_period,max(t.dat_s),max(t.dat_po),max(t.cntd),max(t.cntd_mn)," +

                // превышение есть, если нет ИПУ
                " max(case when s.cnt_stage not in (1,9) then (case when s.rashod>0 then s.val1_source+s.val2 else 0 end) else (case when s.rashod>0 then s.rashod else 0 end) end)," +
                " max(case when s.cnt_stage not in (1,9) then (case when s.rashod>0 then s.up_kf else 1 end) else 1 end)" +

                " ,max(s.cls2),max(s.pu7kw),max(s.gl7kw)" +
                " from t_tarifs t," + gku.paramcalc.kernel_alias + "formuls_opis f," + gku.counters_xx + " s" +
                " where t.nzp_kvar=s.nzp_kvar and t.is_day_calc=0" +
                " and (case when t.nzp_serv=14 then 9 else (case when t.nzp_serv=374 then 6 else t.nzp_serv end) end)=s.nzp_serv " + d_dat_charge +
                " and s.nzp_type=3 and s.stek=3 and not (s.kod_info in (21,22,23,26,27,31,32,33,36))" +
                " and t.nzp_frm=f.nzp_frm and f.nzp_frm_typrs in (5,440)" +
                " group by 1,2,7,8,13 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " insert into t_rsh (nzp_kvar,nzp_frm,nzp_prm,rashod,rashod_g,is_device,nzp_frm_typrs,nzp_frm_typ,dlt_reval,valm,dop87,rash_norm_one" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn,rashod_source,up_kf,kod_info_gkal9,dop87_gkal9,kf307_gkal9)" +
                " select t.nzp_kvar,t.nzp_frm,0 nzp_prm,max(case when s.rashod>0 then s.rashod else 0 end)," +
                " max(case when s.cnt_stage in (1,9) then (case when s.rashod>0 then s.rashod else 0 end) else s.val1_g end)," +
                " max(s.cnt_stage),f.nzp_frm_typrs,f.nzp_frm_typ,max(s.dlt_reval),max(s.val2 + s.val1),min(s.dop87),max(rash_norm_one) " +
                " ,t.nzp_period,max(t.dat_s),max(t.dat_po),max(t.cntd),max(t.cntd_mn)," +

                // превышение есть, если нет ИПУ
                " max(case when s.cnt_stage not in (1,9) then (case when s.rashod>0 then s.val1_source+s.val2 else 0 end) else (case when s.rashod>0 then s.rashod else 0 end) end)," +
                " max(case when s.cnt_stage not in (1,9) then (case when s.rashod>0 then s.up_kf else 1 end) else 1 end)" +

                " ,max(s.cls2),max(s.pu7kw),max(s.gl7kw)" +
                " from t_tarifs t," + gku.paramcalc.kernel_alias + "formuls_opis f," + gku.counters_xx + " s" +
                " where t.nzp_kvar=s.nzp_kvar and t.is_day_calc=1" +
                " and (case when t.nzp_serv=14 then 9 else (case when t.nzp_serv=374 then 6 else t.nzp_serv end) end)=s.nzp_serv " + d_dat_charge +
                " and s.nzp_type=3 and s.stek=20 and not (s.kod_info in (21,22,23,26,27,31,32,33,36))" +
                " and t.dat_s<=s.dat_po and t.dat_po>=s.dat_s" +
                " and t.nzp_frm=f.nzp_frm and f.nzp_frm_typrs in (5,440)" +
                " group by 1,2,7,8,13 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            #endregion нет расчета по Пост

            #region есть расчет по Пост 354 и ОДН<0

            ret = ExecSQL(conn_db,
                " insert into t_rsh (nzp_kvar,nzp_frm,nzp_prm,rashod,rashod_g,is_device,nzp_frm_typrs,nzp_frm_typ,valm,dop87,kod_info,dlt_reval,rash_norm_one" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn,rashod_source,up_kf,kod_info_gkal9,dop87_gkal9,kf307_gkal9)" +
                " select t.nzp_kvar,t.nzp_frm,0 nzp_prm,max(case when s.rashod>0 then s.rashod else 0 end)," +
                " max(case when s.cnt_stage in (1,9) then (case when s.rashod>0 then s.rashod else 0 end) else s.val1_g end)," +
                " max(s.cnt_stage),f.nzp_frm_typrs,f.nzp_frm_typ,max(s.val2 + s.val1),min(s.dop87),max(s.kod_info),min(s.dlt_reval),max(s.rash_norm_one) " +
                " ,t.nzp_period,max(t.dat_s),max(t.dat_po),max(t.cntd),max(t.cntd_mn)," +

                // превышение есть, если нет ИПУ и ОДН не гасит превышение
                " max(case when (s.cnt_stage not in (1,9)) and ((case when s.rashod>0 then s.rashod else 0 end) >= (s.val1_source+s.val2))" +
                    " then s.val1_source+s.val2 " +
                    " else (case when s.rashod>0 then s.rashod else 0 end) " +
                    " end)," +
                " max(case when s.cnt_stage not in (1,9) then s.up_kf else 1 end)" +

                " ,max(s.cls2),max(s.pu7kw),max(s.gl7kw)" +
                " from t_tarifs t," + gku.paramcalc.kernel_alias + "formuls_opis f," + gku.counters_xx + " s" +
                " where t.nzp_kvar=s.nzp_kvar and t.is_day_calc=0" +
                " and (case when t.nzp_serv=14 then 9 else (case when t.nzp_serv=374 then 6 else t.nzp_serv end) end)=s.nzp_serv " + d_dat_charge +
                " and s.nzp_type=3 and s.stek=3 and (s.kod_info in (31,32,33,36))" +
                " and t.nzp_frm=f.nzp_frm and f.nzp_frm_typrs in (5,440)" +
                " and not exists (select 1 from ttt_prm_2 p where p.nzp=t.nzp_dom and p.nzp_prm=2095)" +
                " group by 1,2,7,8,14 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " insert into t_rsh (nzp_kvar,nzp_frm,nzp_prm,rashod,rashod_g,is_device,nzp_frm_typrs,nzp_frm_typ,valm,dop87,kod_info,dlt_reval,rash_norm_one" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn,rashod_source,up_kf,kod_info_gkal9,dop87_gkal9,kf307_gkal9)" +
                " select t.nzp_kvar,t.nzp_frm,0 nzp_prm,max(case when s.rashod>0 then s.rashod else 0 end)," +
                " max(case when s.cnt_stage in (1,9) then (case when s.rashod>0 then s.rashod else 0 end) else s.val1_g end)," +
                " max(s.cnt_stage),f.nzp_frm_typrs,f.nzp_frm_typ,max(s.val2 + s.val1),min(s.dop87),max(s.kod_info),min(s.dlt_reval),max(s.rash_norm_one) " +
                " ,t.nzp_period,max(t.dat_s),max(t.dat_po),max(t.cntd),max(t.cntd_mn)," +

                // превышение есть, если нет ИПУ и ОДН не гасит превышение
                " max(case when (s.cnt_stage not in (1,9)) and ((case when s.rashod>0 then s.rashod else 0 end) >= (s.val1_source+s.val2))" +
                    " then s.val1_source+s.val2 " +
                    " else (case when s.rashod>0 then s.rashod else 0 end) " +
                    " end)," +
                " max(case when s.cnt_stage not in (1,9) then s.up_kf else 1 end)" +

                " ,max(s.cls2),max(s.pu7kw),max(s.gl7kw)" +
                " from t_tarifs t," + gku.paramcalc.kernel_alias + "formuls_opis f," + gku.counters_xx + " s" +
                " where t.nzp_kvar=s.nzp_kvar and t.is_day_calc=1" +
                " and (case when t.nzp_serv=14 then 9 else (case when t.nzp_serv=374 then 6 else t.nzp_serv end) end)=s.nzp_serv " + d_dat_charge +
                " and s.nzp_type=3 and s.stek=20 and (s.kod_info in (31,32,33,36))" +
                " and t.dat_s<=s.dat_po and t.dat_po>=s.dat_s" +
                " and t.nzp_frm=f.nzp_frm and f.nzp_frm_typrs in (5,440)" +
                " and not exists (select 1 from ttt_prm_2 p where p.nzp=t.nzp_dom and p.nzp_prm=2095)" +
                " group by 1,2,7,8,14 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // для домов с учетом отрицательного ОДН в услуге ОДН
            ret = ExecSQL(conn_db,
                " insert into t_rsh (nzp_kvar,nzp_frm,nzp_prm,rashod,rashod_g,is_device,nzp_frm_typrs,nzp_frm_typ,valm,dop87,kod_info,dlt_reval,rash_norm_one" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn,rashod_source,up_kf,kod_info_gkal9,dop87_gkal9,kf307_gkal9)" +
                " select t.nzp_kvar,t.nzp_frm,0 nzp_prm,max(s.val2 + s.val1)," +
                " max(s.val2 + (case when s.cnt_stage in (1,9) then s.val1 else s.val1_g end) )," +
                " max(s.cnt_stage),f.nzp_frm_typrs,f.nzp_frm_typ,max(s.val2 + s.val1)," +
                " min(case when s.val2 + s.val1 + s.dop87 >= 0 then s.dop87 else (-1) * (s.val2 + s.val1) end)," +
                " max(s.kod_info),min(s.dlt_reval),max(s.rash_norm_one) " +
                " ,t.nzp_period,max(t.dat_s),max(t.dat_po),max(t.cntd),max(t.cntd_mn)," +

                // превышение есть, если нет ИПУ и ОДН не гасит превышение
                " max(case when (s.cnt_stage not in (1,9)) and ((s.val2 + s.val1 + s.dop87) >= (s.val1_source+s.val2))" +
                    " then s.val1_source+s.val2 - s.dop87 " +
                    " else s.val2 + s.val1 " +
                    " end),max(s.up_kf)" +

                " ,max(s.cls2),max(s.pu7kw),max(s.gl7kw)" +
                " from t_tarifs t," + gku.paramcalc.kernel_alias + "formuls_opis f," + gku.counters_xx + " s" +
                " where t.nzp_kvar=s.nzp_kvar and t.is_day_calc=0" +
                " and (case when t.nzp_serv=14 then 9 else (case when t.nzp_serv=374 then 6 else t.nzp_serv end) end)=s.nzp_serv " + d_dat_charge +
                " and s.nzp_type=3 and s.stek=3 and (s.kod_info in (31,32,33,36))" +
                " and t.nzp_frm=f.nzp_frm and f.nzp_frm_typrs in (5,440)" +
                " and exists (select 1 from ttt_prm_2 p where p.nzp=t.nzp_dom and p.nzp_prm=2095)" +
                " group by 1,2,7,8,14 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " insert into t_rsh (nzp_kvar,nzp_frm,nzp_prm,rashod,rashod_g,is_device,nzp_frm_typrs,nzp_frm_typ,valm,dop87,kod_info,dlt_reval,rash_norm_one" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn,rashod_source,up_kf,kod_info_gkal9,dop87_gkal9,kf307_gkal9)" +
                " select t.nzp_kvar,t.nzp_frm,0 nzp_prm,max(s.val2 + s.val1)," +
                " max(s.val2 + (case when s.cnt_stage in (1,9) then s.val1 else s.val1_g end) )," +
                " max(s.cnt_stage),f.nzp_frm_typrs,f.nzp_frm_typ,max(s.val2 + s.val1)," +
                " min(case when s.val2 + s.val1 + s.dop87 >= 0 then s.dop87 else (-1) * (s.val2 + s.val1) end)," +
                " max(s.kod_info),min(s.dlt_reval),max(s.rash_norm_one) " +
                " ,t.nzp_period,max(t.dat_s),max(t.dat_po),max(t.cntd),max(t.cntd_mn)," +

                // превышение есть, если нет ИПУ и ОДН не гасит превышение
                " max(case when (s.cnt_stage not in (1,9)) and ((s.val2 + s.val1 + s.dop87) >= (s.val1_source+s.val2))" +
                    " then s.val1_source+s.val2 - s.dop87 " +
                    " else s.val2 + s.val1 " +
                    " end),max(s.up_kf)" +

                " ,max(s.cls2),max(s.pu7kw),max(s.gl7kw)" +
                " from t_tarifs t," + gku.paramcalc.kernel_alias + "formuls_opis f," + gku.counters_xx + " s" +
                " where t.nzp_kvar=s.nzp_kvar and t.is_day_calc=1" +
                " and (case when t.nzp_serv=14 then 9 else (case when t.nzp_serv=374 then 6 else t.nzp_serv end) end)=s.nzp_serv " + d_dat_charge +
                " and s.nzp_type=3 and s.stek=20 and (s.kod_info in (31,32,33,36))" +
                " and t.dat_s<=s.dat_po and t.dat_po>=s.dat_s" +
                " and t.nzp_frm=f.nzp_frm and f.nzp_frm_typrs in (5,440)" +
                " and exists (select 1 from ttt_prm_2 p where p.nzp=t.nzp_dom and p.nzp_prm=2095)" +
                " group by 1,2,7,8,14 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            #endregion есть расчет по Пост 354 и ОДН<0

            #region есть расчет по Пост 354 и ОДН>0 - есть

            #region основная услуга - есть расчет по Пост 354 и ОДН>0
            // основная услуга
            ret = ExecSQL(conn_db,
                " insert into t_rsh (nzp_kvar,nzp_frm,nzp_prm,rashod,rashod_g,is_device,nzp_frm_typrs,nzp_frm_typ,valm,dop87,kod_info,dlt_reval,rash_norm_one" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn,rashod_source,up_kf,kod_info_gkal9,dop87_gkal9,kf307_gkal9)" +
                " select t.nzp_kvar,t.nzp_frm,0 nzp_prm,max(s.val2 + s.val1)," +
                " max(s.val2 + (case when s.cnt_stage in (1,9) then s.val1 else s.val1_g end) )," +
                " max(s.cnt_stage),f.nzp_frm_typrs,f.nzp_frm_typ,max(s.val2 + s.val1),max(s.dop87),max(s.kod_info),max(s.dlt_reval),max(s.rash_norm_one)" +
                " ,t.nzp_period,max(t.dat_s),max(t.dat_po),max(t.cntd),max(t.cntd_mn)," +

                // превышение есть, если нет ИПУ
                " max(case when s.cnt_stage not in (1,9) then (case when s.val2 + s.val1>0 then s.val1_source+s.val2 else 0 end) else s.val2 + s.val1 end)," +
                " max(case when s.cnt_stage not in (1,9) then (case when s.val2 + s.val1>0 then s.up_kf else 1 end) else 1 end)" +

                " ,max(s.cls2),max(s.pu7kw),max(s.gl7kw)" +
                " from t_tarifs t," + gku.paramcalc.kernel_alias + "formuls_opis f," + gku.counters_xx + " s" +
                " where t.nzp_kvar=s.nzp_kvar and t.is_day_calc=0" +
                " and (case when t.nzp_serv=14 then 9 else (case when t.nzp_serv=374 then 6 else t.nzp_serv end) end)=s.nzp_serv " + d_dat_charge +
                " and s.nzp_type=3 and s.stek=3 and (s.kod_info in (21,22,23,26,27))" +
                " and t.nzp_frm=f.nzp_frm and f.nzp_frm_typrs in (5,440)" +
                " group by 1,2,7,8,14 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " insert into t_rsh (nzp_kvar,nzp_frm,nzp_prm,rashod,rashod_g,is_device,nzp_frm_typrs,nzp_frm_typ,valm,dop87,kod_info,dlt_reval,rash_norm_one" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn, rashod_source,up_kf,kod_info_gkal9,dop87_gkal9,kf307_gkal9)" +
                " select t.nzp_kvar,t.nzp_frm,0 nzp_prm,max(s.val2 + s.val1)," +
                " max(s.val2 + (case when s.cnt_stage in (1,9) then s.val1 else s.val1_g end) )," +
                " max(s.cnt_stage),f.nzp_frm_typrs,f.nzp_frm_typ,max(s.val2 + s.val1),max(s.dop87),max(s.kod_info),max(s.dlt_reval),max(s.rash_norm_one)" +
                " ,t.nzp_period,max(t.dat_s),max(t.dat_po),max(t.cntd),max(t.cntd_mn)," +

                // превышение есть, если нет ИПУ
                " max(case when s.cnt_stage not in (1,9) then (case when s.val2 + s.val1>0 then s.val1_source+s.val2 else 0 end) else s.val2 + s.val1 end)," +
                " max(case when s.cnt_stage not in (1,9) then (case when s.val2 + s.val1>0 then s.up_kf else 1 end) else 1 end)" +

                " ,max(s.cls2),max(s.pu7kw),max(s.gl7kw)" +
                " from t_tarifs t," + gku.paramcalc.kernel_alias + "formuls_opis f," + gku.counters_xx + " s" +
                " where t.nzp_kvar=s.nzp_kvar and t.is_day_calc=1" +
                " and (case when t.nzp_serv=14 then 9 else (case when t.nzp_serv=374 then 6 else t.nzp_serv end) end)=s.nzp_serv " + d_dat_charge +
                " and s.nzp_type=3 and s.stek=20 and (s.kod_info in (21,22,23,26,27))" +
                " and t.dat_s<=s.dat_po and t.dat_po>=s.dat_s" +
                " and t.nzp_frm=f.nzp_frm and f.nzp_frm_typrs in (5,440)" +
                " group by 1,2,7,8,14 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            #endregion основная услуга - есть расчет по Пост 354 и ОДН>0

            #region услуга на ОДН - есть расчет по Пост 354 и ОДН>0
            // услуга на ОДН
            ret = ExecSQL(conn_db,
                " insert into t_rsh (nzp_kvar,nzp_frm,nzp_prm,rashod,rashod_g,is_device,nzp_frm_typrs,nzp_frm_typ" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn,rashod_source,up_kf,kod_info_gkal9,dop87_gkal9,kf307_gkal9)" +
                " select t.nzp_kvar,t.nzp_frm,0 nzp_prm,max(case when s.dop87>0 then s.dop87 else 0 end)," +
                " max(case when s.dop87>0 then s.dop87 else 0 end),max(s.cnt_stage),f.nzp_frm_typrs,f.nzp_frm_typ " +
                " ,t.nzp_period,max(t.dat_s),max(t.dat_po),max(t.cntd),max(t.cntd_mn)," +

                // превышение ограничено расходом ОДН>0 и расходом ОДН по нормативу
                " max(case when (s.dop87>0) and (s.dop87>s.dop87_source) and (s.dop87_source>0.000001)" +
                    " then " +
                        " case when s.dop87 > (s.dop87_source + (case when (s.dop87>s.kf_dpu_plob*squ1) and (s.kf_dpu_plob>0.0000001) then s.dop87-s.kf_dpu_plob*squ1 else 0 end)) " +
                        " then s.dop87_source + (case when (s.dop87>s.kf_dpu_plob*squ1) and (s.kf_dpu_plob>0.0000001) then s.dop87-s.kf_dpu_plob*squ1 else 0 end)" +
                        " else s.dop87 end" +
                    " else s.dop87 " +
                    " end)," +
                // up_kf
                " max(case when (s.dop87>0) and (s.dop87>s.dop87_source) and (s.dop87_source>0.000001)" +
                    " then s.dop87 / s.dop87_source " +
                    " else 1 " +
                    " end)," +

                " max(s.cls2),max(s.pu7kw),max(s.gl7kw)" +
                " from t_tarifs t," + gku.paramcalc.kernel_alias + "formuls_opis f," + gku.counters_xx + " s," + gku.paramcalc.kernel_alias + "serv_odn n" +
                " where t.nzp_kvar=s.nzp_kvar and t.is_day_calc=0" +
                " and t.nzp_serv=n.nzp_serv and s.nzp_serv=(case when n.nzp_serv_link=14 then 9 else n.nzp_serv_link end) " + d_dat_charge +
                " and s.nzp_type=3 and s.stek=3 and (s.kod_info in (21,22,23,26,27))" +
                " and t.nzp_frm=f.nzp_frm and f.nzp_frm_typrs in (5,440)" +
                " group by 1,2,7,8,9 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " insert into t_rsh (nzp_kvar,nzp_frm,nzp_prm,rashod,rashod_g,is_device,nzp_frm_typrs,nzp_frm_typ" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn, rashod_source,up_kf,kod_info_gkal9,dop87_gkal9,kf307_gkal9)" +
                " select t.nzp_kvar,t.nzp_frm,0 nzp_prm,max(case when s.dop87>0 then s.dop87 else 0 end)," +
                " max(case when s.dop87>0 then s.dop87 else 0 end),max(s.cnt_stage),f.nzp_frm_typrs,f.nzp_frm_typ " +
                " ,t.nzp_period,max(t.dat_s),max(t.dat_po),max(t.cntd),max(t.cntd_mn)," +

                // превышение ограничено расходом ОДН>0 и расходом ОДН по нормативу
                " max(case when (s.dop87>0) and (s.dop87>s.dop87_source) and (s.dop87_source>0.000001)" +
                    " then s.dop87_source + (case when s.dop87>s.kf_dpu_plob*squ1 then s.dop87-s.kf_dpu_plob*squ1 else 0 end) " +
                    " else s.dop87 " +
                    " end)," +
                // up_kf
                " max(case when (s.dop87>0) and (s.dop87>s.dop87_source) and (s.dop87_source>0.000001)" +
                    " then s.dop87 / s.dop87_source " +
                    " else 1 " +
                    " end)," +

                " max(s.cls2),max(s.pu7kw),max(s.gl7kw)" +
                " from t_tarifs t," + gku.paramcalc.kernel_alias + "formuls_opis f," + gku.counters_xx + " s," + gku.paramcalc.kernel_alias + "serv_odn n" +
                " where t.nzp_kvar=s.nzp_kvar and t.is_day_calc=1" +
                " and t.nzp_serv=n.nzp_serv and s.nzp_serv=(case when n.nzp_serv_link=14 then 9 else n.nzp_serv_link end) " + d_dat_charge +
                " and s.nzp_type=3 and s.stek=20 and (s.kod_info in (21,22,23,26,27))" +
                " and t.dat_s<=s.dat_po and t.dat_po>=s.dat_s" +
                " and t.nzp_frm=f.nzp_frm and f.nzp_frm_typrs in (5,440)" +
                " group by 1,2,7,8,9 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // для домов с учетом отрицательного ОДН в услуге ОДН
            ret = ExecSQL(conn_db,
                " insert into t_rsh (nzp_kvar,nzp_frm,nzp_prm,rashod,rashod_g,is_device,nzp_frm_typrs,nzp_frm_typ" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn,rashod_source,up_kf,kod_info_gkal9,dop87_gkal9,kf307_gkal9,dop87,rsh3)" +
                " select t.nzp_kvar,t.nzp_frm,0 nzp_prm," +
                " min(case when s.val2 + s.val1 + s.dop87 >= 0 then s.dop87 else (-1) * (s.val2 + s.val1) end)," +
                " min(case when s.val2 + s.val1 + s.dop87 >= 0 then s.dop87 else (-1) * (s.val2 + s.val1) end)," +
                " max(s.cnt_stage),f.nzp_frm_typrs,f.nzp_frm_typ " +
                " ,t.nzp_period,max(t.dat_s),max(t.dat_po),max(t.cntd),max(t.cntd_mn)," +

                // превышения нет, если ОДН<0
                " min(case when s.val2 + s.val1 + s.dop87 >= 0 then s.dop87 else (-1) * (s.val2 + s.val1) end),max(s.up_kf)" +

                " ,max(s.cls2),max(s.pu7kw),max(s.gl7kw),min(s.dop87),max(s.val2 + s.val1)" +
                " from t_tarifs t," + gku.paramcalc.kernel_alias + "formuls_opis f," + gku.counters_xx + " s," + gku.paramcalc.kernel_alias + "serv_odn n" +
                " where t.nzp_kvar=s.nzp_kvar and t.is_day_calc=0" +
                " and t.nzp_serv=n.nzp_serv and s.nzp_serv=(case when n.nzp_serv_link=14 then 9 else n.nzp_serv_link end) " + d_dat_charge +
                " and s.nzp_type=3 and s.stek=3 and (s.kod_info in (31,32,33,36))" +
                " and t.nzp_frm=f.nzp_frm and f.nzp_frm_typrs in (5,440)" +
                " and exists (select 1 from ttt_prm_2 p where p.nzp=t.nzp_dom and p.nzp_prm=2095)" +
                " group by 1,2,7,8,9 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " insert into t_rsh (nzp_kvar,nzp_frm,nzp_prm,rashod,rashod_g,is_device,nzp_frm_typrs,nzp_frm_typ" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn, rashod_source,up_kf,kod_info_gkal9,dop87_gkal9,kf307_gkal9,dop87,rsh3)" +
                " select t.nzp_kvar,t.nzp_frm,0 nzp_prm," +
                " min(case when s.val2 + s.val1 + s.dop87 >= 0 then s.dop87 else (-1) * (s.val2 + s.val1) end)," +
                " min(case when s.val2 + s.val1 + s.dop87 >= 0 then s.dop87 else (-1) * (s.val2 + s.val1) end)," +
                " max(s.cnt_stage),f.nzp_frm_typrs,f.nzp_frm_typ " +
                " ,t.nzp_period,max(t.dat_s),max(t.dat_po),max(t.cntd),max(t.cntd_mn)," +

                // превышения нет, если ОДН<0
                " min(case when s.val2 + s.val1 + s.dop87 >= 0 then s.dop87 else (-1) * (s.val2 + s.val1) end),max(s.up_kf)" +

                " ,max(s.cls2),max(s.pu7kw),max(s.gl7kw),min(s.dop87),max(s.val2 + s.val1)" +
                " from t_tarifs t," + gku.paramcalc.kernel_alias + "formuls_opis f," + gku.counters_xx + " s," + gku.paramcalc.kernel_alias + "serv_odn n" +
                " where t.nzp_kvar=s.nzp_kvar and t.is_day_calc=1" +
                " and t.nzp_serv=n.nzp_serv and s.nzp_serv=(case when n.nzp_serv_link=14 then 9 else n.nzp_serv_link end) " + d_dat_charge +
                " and s.nzp_type=3 and s.stek=20 and (s.kod_info in (31,32,33,36))" +
                " and t.dat_s<=s.dat_po and t.dat_po>=s.dat_s" +
                " and t.nzp_frm=f.nzp_frm and f.nzp_frm_typrs in (5,440)" +
                " and exists (select 1 from ttt_prm_2 p where p.nzp=t.nzp_dom and p.nzp_prm=2095)" +
                " group by 1,2,7,8,9 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            #endregion услуга на ОДН - есть расчет по Пост 354 и ОДН>0

            #endregion есть расчет по Пост 354 и ОДН>0 - есть

            return true;
        }
        #endregion Тип расхода = 5 (расход коммунальной услуги + ОДН)

        #region Тип расхода = 39,391 (расход коммунальной услуги канализация + ОДН)
        public bool CalcTypeRashod_39_391(IDbConnection conn_db, Gku gku, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            string d_dat_charge = " and s.dat_charge is null ";
            if (!gku.paramcalc.b_cur)
            {
                d_dat_charge = " and s.dat_charge = " + MDY(gku.paramcalc.cur_mm, 28, gku.paramcalc.cur_yy);
            }
            // тип расхода = 39 - КАН
            ret = ExecSQL(conn_db,
                " insert into t_rsh (nzp_kvar,nzp_frm,nzp_prm,nzp_frm_typ,rashod,rashod_g,rsh_hv,is_device,nzp_frm_typrs,rash_norm_one" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn,kod_info,rashod_source,up_kf)" +
                " select t.nzp_kvar,t.nzp_frm,0 nzp_prm,f.nzp_frm_typ,max(case when s.rashod>0 then s.rashod else 0 end)," +
                " max(case when s.cnt_stage in (1,9) then (case when s.rashod>0 then s.rashod else 0 end) else s.val1_g end)," +
                " max(s.val4),max(s.cnt_stage),f.nzp_frm_typrs,max(s.rash_norm_one)" +
                " ,t.nzp_period,max(t.dat_s),max(t.dat_po),max(t.cntd),max(t.cntd_mn),max(s.kod_info),max(s.val1_source),max(s.up_kf)" +
                " from t_tarifs t," + gku.paramcalc.kernel_alias + "formuls_opis f," + gku.counters_xx + " s" +
                " where t.nzp_kvar=s.nzp_kvar and t.is_day_calc=0" +
                " and (case when t.nzp_serv=353 then 324 else t.nzp_serv end)=s.nzp_serv and t.nzp_serv in (7,324,353) " +
                d_dat_charge + " and s.nzp_type=3 and s.stek=3 and not (s.kod_info in (21,22,23,26,27))" +
                " and t.nzp_frm=f.nzp_frm and f.nzp_frm_typrs in (39,391)" +
                " AND t.dat_s>=s.dat_s AND s.dat_po<=t.dat_po " +
                " group by 1,2,3,4,9,11 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " insert into t_rsh (nzp_kvar,nzp_frm,nzp_prm,nzp_frm_typ,rashod,rashod_g,rsh_hv,is_device,nzp_frm_typrs,rash_norm_one" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn,kod_info,rashod_source,up_kf)" +
                " select t.nzp_kvar,t.nzp_frm,0 nzp_prm,f.nzp_frm_typ,max(case when s.rashod>0 then s.rashod else 0 end)," +
                " max(case when s.cnt_stage in (1,9) then (case when s.rashod>0 then s.rashod else 0 end) else s.val1_g end)," +
                " max(s.val4),max(s.cnt_stage),f.nzp_frm_typrs,max(s.rash_norm_one)" +
                " ,t.nzp_period,max(t.dat_s),max(t.dat_po),max(t.cntd),max(t.cntd_mn),max(s.kod_info),max(s.val1_source),max(s.up_kf)" +
                " from t_tarifs t," + gku.paramcalc.kernel_alias + "formuls_opis f," + gku.counters_xx + " s" +
                " where t.nzp_kvar=s.nzp_kvar and t.is_day_calc=1" +
                " and (case when t.nzp_serv=353 then 324 else t.nzp_serv end)=s.nzp_serv and t.nzp_serv in (7,324,353) " +
                d_dat_charge + " and s.nzp_type=3 and s.stek=20 and not (s.kod_info in (21,22,23,26,27))" +
                " and t.dat_s<=s.dat_po and t.dat_po>=s.dat_s" +
                " and t.nzp_frm=f.nzp_frm and f.nzp_frm_typrs in (39,391)" +
                " AND t.dat_s>=s.dat_s AND s.dat_po<=t.dat_po " +
                " group by 1,2,3,4,9,11 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // Пост 354
            // основная услуга
            ret = ExecSQL(conn_db,
                " insert into t_rsh (nzp_kvar,nzp_frm,nzp_prm,nzp_frm_typ,rashod,rashod_g,rsh_hv,is_device,nzp_frm_typrs,rash_norm_one" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn,kod_info,rashod_source,up_kf)" +
                " select t.nzp_kvar,t.nzp_frm,0 nzp_prm,f.nzp_frm_typ,max(s.val2 + s.val1)," +
                " max(s.val2 + (case when s.cnt_stage in (1,9) then s.val1 else s.val1_g end) )," +
                " max(s.val4),max(s.cnt_stage),f.nzp_frm_typrs,max(s.rash_norm_one)" +
                " ,t.nzp_period,max(t.dat_s),max(t.dat_po),max(t.cntd),max(t.cntd_mn),max(s.kod_info),max(s.val1_source),max(s.up_kf)" +
                " from t_tarifs t," + gku.paramcalc.kernel_alias + "formuls_opis f," + gku.counters_xx + " s" +
                " where t.nzp_kvar=s.nzp_kvar and t.is_day_calc=0" +
                " and (case when t.nzp_serv=353 then 324 else t.nzp_serv end)=s.nzp_serv and t.nzp_serv in (7,324,353) " +
                d_dat_charge + " and s.nzp_type=3 and s.stek=3 and (s.kod_info in (21,22,23,26,27))" +
                " and t.nzp_frm=f.nzp_frm and f.nzp_frm_typrs in (39,391)" +
                " AND t.dat_s>=s.dat_s AND s.dat_po<=t.dat_po " +
                " group by 1,2,3,4,9,11 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " insert into t_rsh (nzp_kvar,nzp_frm,nzp_prm,nzp_frm_typ,rashod,rashod_g,rsh_hv,is_device,nzp_frm_typrs,rash_norm_one" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn,kod_info,rashod_source,up_kf)" +
                " select t.nzp_kvar,t.nzp_frm,0 nzp_prm,f.nzp_frm_typ,max(s.val2 + s.val1)," +
                " max(s.val2 + (case when s.cnt_stage in (1,9) then s.val1 else s.val1_g end) )," +
                " max(s.val4),max(s.cnt_stage),f.nzp_frm_typrs,max(s.rash_norm_one)" +
                " ,t.nzp_period,max(t.dat_s),max(t.dat_po),max(t.cntd),max(t.cntd_mn),max(s.kod_info),max(s.val1_source),max(s.up_kf)" +
                " from t_tarifs t," + gku.paramcalc.kernel_alias + "formuls_opis f," + gku.counters_xx + " s" +
                " where t.nzp_kvar=s.nzp_kvar and t.is_day_calc=1" +
                " and (case when t.nzp_serv=353 then 324 else t.nzp_serv end)=s.nzp_serv and t.nzp_serv in (7,324,353) " +
                d_dat_charge + " and s.nzp_type=3 and s.stek=20 and (s.kod_info in (21,22,23,26,27))" +
                " and t.dat_s<=s.dat_po and t.dat_po>=s.dat_s" +
                " and t.nzp_frm=f.nzp_frm and f.nzp_frm_typrs in (39,391)" +
                " AND t.dat_s>=s.dat_s AND s.dat_po<=t.dat_po " +
                " group by 1,2,3,4,9,11 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // услуга на ОДН
            ret = ExecSQL(conn_db,
                " insert into t_rsh (nzp_kvar,nzp_frm,nzp_prm,nzp_frm_typ,rashod,rashod_g,rsh_hv,is_device,nzp_frm_typrs" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn,rashod_source)" +
                " select t.nzp_kvar,t.nzp_frm,0 nzp_prm,f.nzp_frm_typ,max(case when s.dop87>0 then s.dop87 else 0 end)," +
                " max(case when s.dop87>0 then s.dop87 else 0 end),0,max(s.cnt_stage),f.nzp_frm_typrs " +
                " ,t.nzp_period,max(t.dat_s),max(t.dat_po),max(t.cntd),max(t.cntd_mn),max(s.dop87_source)" +
                " from t_tarifs t," + gku.paramcalc.kernel_alias + "formuls_opis f," + gku.counters_xx + " s," + gku.paramcalc.kernel_alias + "serv_odn n" +
                " where t.nzp_kvar=s.nzp_kvar and t.is_day_calc=0" +
                " and (case when t.nzp_serv=353 then 324 else t.nzp_serv end)=s.nzp_serv and t.nzp_serv in (7,324,353) " +
                " and t.nzp_serv=n.nzp_serv and s.nzp_serv=7" +
                " and s.nzp_serv=n.nzp_serv_link " +
                d_dat_charge + " and s.nzp_type=3 and s.stek=3 and (s.kod_info in (21,22,23,26,27))" +
                " and t.nzp_frm=f.nzp_frm and f.nzp_frm_typrs in (39,391)" +
                " AND t.dat_s>=s.dat_s AND s.dat_po<=t.dat_po " +
                " group by 1,2,3,4,9,10 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " insert into t_rsh (nzp_kvar,nzp_frm,nzp_prm,nzp_frm_typ,rashod,rashod_g,rsh_hv,is_device,nzp_frm_typrs" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn,rashod_source)" +
                " select t.nzp_kvar,t.nzp_frm,0 nzp_prm,f.nzp_frm_typ,max(case when s.dop87>0 then s.dop87 else 0 end)," +
                " max(case when s.dop87>0 then s.dop87 else 0 end),0,max(s.cnt_stage),f.nzp_frm_typrs " +
                " ,t.nzp_period,max(t.dat_s),max(t.dat_po),max(t.cntd),max(t.cntd_mn),max(s.dop87_source)" +
                " from t_tarifs t," + gku.paramcalc.kernel_alias + "formuls_opis f," + gku.counters_xx + " s," + gku.paramcalc.kernel_alias + "serv_odn n" +
                " where t.nzp_kvar=s.nzp_kvar and t.is_day_calc=1" +
                " and (case when t.nzp_serv=353 then 324 else t.nzp_serv end)=s.nzp_serv and t.nzp_serv in (7,324,353) " +
                " and t.nzp_serv=n.nzp_serv and s.nzp_serv=7" +
                " and s.nzp_serv=n.nzp_serv_link " +
                d_dat_charge + " and s.nzp_type=3 and s.stek=20 and (s.kod_info in (21,22,23,26,27))" +
                " and t.dat_s<=s.dat_po and t.dat_po>=s.dat_s" +
                " and t.nzp_frm=f.nzp_frm and f.nzp_frm_typrs in (39,391)" +
                " AND t.dat_s>=s.dat_s AND s.dat_po<=t.dat_po " +
                " group by 1,2,3,4,9,10 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " update t_rsh set valm=rashod " +
                " where nzp_frm_typrs in (39,391) "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            return true;
        }
        #endregion Тип расхода = 39,391 (расход коммунальной услуги канализация + ОДН)

        #region Тип расхода 390 -КАН =ХВ+ГВ
        public bool CalcTypeRashod_390(IDbConnection conn_db, Gku gku, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            string d_dat_charge = " and s.dat_charge is null ";
            if (!gku.paramcalc.b_cur)
            {
                d_dat_charge = " and s.dat_charge = " + MDY(gku.paramcalc.cur_mm, 28, gku.paramcalc.cur_yy);
            }

            ret = ExecSQL(conn_db,
                " create temp table t_rsh390 (" +
                " nzp_kvar integer," +
                " nzp_frm  integer," +
                " nzp_prm  integer default 0," +
                " rashod   " + sDecimalType + "(16,6) default 0," +
                " rashod_g " + sDecimalType + "(16,6) default 0," +
                " rash_kv  " + sDecimalType + "(16,6) default 0," +
                " rash_odn " + sDecimalType + "(16,6) default 0," +
                " rsh_kn   " + sDecimalType + "(16,6) default 0," +
                " rsh_hv   " + sDecimalType + "(16,6) default 0," +
                " rsh_gv   " + sDecimalType + "(16,6) default 0," +
                " rsh_kn_g   " + sDecimalType + "(16,6) default 0," +
                " rsh_hv_g   " + sDecimalType + "(16,6) default 0," +
                " rsh_gv_g   " + sDecimalType + "(16,6) default 0," +
                " rsh_kv_kn    " + sDecimalType + "(16,6) default 0," +
                " rsh_kv_hv    " + sDecimalType + "(16,6) default 0," +
                " rsh_kv_gv    " + sDecimalType + "(16,6) default 0," +
                " rsh_odn_kn   " + sDecimalType + "(16,6) default 0," +
                " rsh_odn_hv   " + sDecimalType + "(16,6) default 0," +
                " rsh_odn_gv   " + sDecimalType + "(16,6) default 0," +
                " rash_norm_one " + sDecimalType + "(16,7) default 0," +
                " rash_norm_one_hv " + sDecimalType + "(16,7) default 0," +
                " rash_norm_one_gv " + sDecimalType + "(16,7) default 0," +
                " is_device    integer  default 0," +
                " is_device_kn integer  default 0," +
                " is_device_hv integer  default 0," +
                " is_device_gv integer  default 0, " +
                " kod_info     integer  default 0," +
                " kod_info_kn  integer  default 0," +
                " kod_info_hv  integer  default 0," +
                " nzp_frm_typ  integer  default 0 not null, " +
                " kod_info_gv  integer  default 0," +
                " rashod_source " + sDecimalType + "(16,7) default 0," +
                " up_kf " + sDecimalType + "(16,7) default 0," +
                " rashod_source_hv " + sDecimalType + "(16,7) default 0," +
                " up_kf_hv " + sDecimalType + "(16,7) default 0," +
                " rashod_source_gv " + sDecimalType + "(16,7) default 0," +
                " up_kf_gv " + sDecimalType + "(16,7) default 0," +
                " nzp_period integer," +
                " dat_s    DATE," +
                " dat_po   DATE," +
                " cntd     integer, " +
                " cntd_mn  integer,  " +
                " is_day_calc  integer" +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // вставить все расходы по КАН, где есть формулы 
            ret = ExecSQL(conn_db,
                " insert into t_rsh390 (nzp_kvar,nzp_frm,nzp_frm_typ,nzp_prm,rsh_kn,rsh_kn_g,is_device_kn,kod_info_kn,rsh_kv_kn,rsh_odn_kn" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn,rash_norm_one,rashod_source,up_kf,is_day_calc)" +
                " select t.nzp_kvar,t.nzp_frm,f.nzp_frm_typ,0 nzp_prm,max(" + sNvlWord + "(s.rashod,0))," +
                " max(case when s.cnt_stage in (1,9) then " + sNvlWord + "(s.rashod,0) else " + sNvlWord + "(s.val1_g,0) end)," +
                " max(" + sNvlWord + "(s.cnt_stage,0)),max(" + sNvlWord + "(s.kod_info,0))," +
                " max(" + sNvlWord + "(s.val2,0) + " + sNvlWord + "(s.val1,0)),max(" + sNvlWord + "(s.dop87,0))" +
                " ,t.nzp_period,max(t.dat_s),max(t.dat_po),max(t.cntd),max(t.cntd_mn),max(s.rash_norm_one)" +
                " ,max(" + sNvlWord + "(s.val1_source,0)),max(" + sNvlWord + "(s.up_kf,0)),0 is_day_calc " +
                " from t_tarifs t left outer join " + gku.counters_xx + " s on t.nzp_kvar=s.nzp_kvar" +
                " and (case when t.nzp_serv=353 then 324 else t.nzp_serv end)=s.nzp_serv and s.nzp_type=3 and s.stek=3,"
                + gku.paramcalc.pref + "_kernel.formuls_opis f " +
                " where  " +
                " t.nzp_serv in (7,324,353) " + //d_dat_charge +
                " and t.nzp_frm=f.nzp_frm and f.nzp_frm_typrs=390 and t.is_day_calc=0" +
                " group by 1,2,3,11 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            //для подневного расчета
            ret = ExecSQL(conn_db,
                " insert into t_rsh390 (nzp_kvar,nzp_frm,nzp_frm_typ,nzp_prm,rsh_kn,rsh_kn_g,is_device_kn,kod_info_kn,rsh_kv_kn,rsh_odn_kn" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn,rash_norm_one,rashod_source,up_kf,is_day_calc)" +
                " select t.nzp_kvar,t.nzp_frm,f.nzp_frm_typ,0 nzp_prm,max(" + sNvlWord + "(s.rashod,0))," +
                " max(case when s.cnt_stage in (1,9) then " + sNvlWord + "(s.rashod,0) else " + sNvlWord + "(s.val1_g,0) end)," +
                " max(" + sNvlWord + "(s.cnt_stage,0)),max(" + sNvlWord + "(s.kod_info,0))," +
                " max(" + sNvlWord + "(s.val2,0) + " + sNvlWord + "(s.val1,0)),max(" + sNvlWord + "(s.dop87,0))" +
                " ,t.nzp_period,max(t.dat_s),max(t.dat_po),max(t.cntd),max(t.cntd_mn),max(s.rash_norm_one)" +
                " ,max(" + sNvlWord + "(s.val1_source,0)),max(" + sNvlWord + "(s.up_kf,0))," +
                " 1 is_day_calc " +
                " from t_tarifs t left outer join " + gku.counters_xx + " s on t.nzp_kvar=s.nzp_kvar" +
                " and (case when t.nzp_serv=353 then 324 else t.nzp_serv end)=s.nzp_serv and s.nzp_type=3 and s.stek=20,"
                + gku.paramcalc.pref + "_kernel.formuls_opis f " +
                " where  " +
                " t.nzp_serv in (7,324,353) " + //d_dat_charge +
                " and t.nzp_frm=f.nzp_frm and f.nzp_frm_typrs=390 and t.is_day_calc=1" +
                " group by 1,2,3,11 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, " create index ixt_rsh1_390 on t_rsh390(nzp_kvar,nzp_period) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, " create index ixt_rsh2_390 on t_rsh390(kod_info) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, sUpdStat + " t_rsh390 ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ViewTbl(conn_db, " select * from t_tarifs order by nzp_kvar,nzp_frm ");
            ViewTbl(conn_db, " select * from t_rsh390 order by nzp_kvar,nzp_frm ");


            var sql = " CREATE TEMP TABLE t_rsh390_hv AS " +
                      " SELECT t.nzp_kvar, t.nzp_period,max(s.cnt_stage) is_device_hv, max(s.rashod) rsh_hv," +
                      " max(case when s.cnt_stage in (1,9) then s.rashod else s.val1_g end) rsh_hv_g," +
                      " max(s.kod_info) kod_info_hv, max(s.val2 + s.val1) rsh_kv_hv, " +
                      " max(s.dop87) rsh_odn_hv,max(s.rash_norm_one) rash_norm_one_hv," +
                      // превышение есть, если нет ИПУ
                      " MAX(CASE WHEN s.cnt_stage NOT IN (1,9) " +
                      "     THEN (CASE WHEN s.val2 + s.val1>0 " +
                      "                THEN " + sNvlWord + "(s.val1_source)+s.val2 " +
                      "                ELSE 0" +
                      "                END) " +
                      "     ELSE s.val2 + s.val1 " +
                      "     END) rashod_source_hv," +
                      " MAX(CASE WHEN s.cnt_stage NOT IN (1,9) " +
                      "     THEN (CASE WHEN s.val2 + s.val1>0 " +
                      "           THEN s.up_kf else 1 " +
                      "           END) else 1 " +
                      "      END) up_kf_hv" +
                      " FROM " + gku.counters_xx + " s, t_rsh390 t" +
                      " WHERE t.nzp_kvar=s.nzp_kvar and s.nzp_serv=6 " + d_dat_charge +
                      " and s.nzp_type=3 and s.stek=3 and t.is_day_calc=0 " +
                      " GROUP BY 1,2";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }


            sql = " INSERT INTO t_rsh390_hv (nzp_kvar,nzp_period,is_device_hv,rsh_hv,rsh_hv_g,kod_info_hv," +
                  " rsh_kv_hv,rsh_odn_hv,rash_norm_one_hv,rashod_source_hv,up_kf_hv)" +
                  " SELECT t.nzp_kvar, t.nzp_period,max(s.cnt_stage) is_device_hv, max(s.rashod) rsh_hv," +
                  " max(case when s.cnt_stage in (1,9) then s.rashod else s.val1_g end) rsh_hv_g," +
                  " max(s.kod_info) kod_info_hv, max(s.val2 + s.val1) rsh_kv_hv, " +
                  " max(s.dop87) rsh_odn_hv,max(s.rash_norm_one) rash_norm_one_hv," +
                  // превышение есть, если нет ИПУ
                  " MAX(CASE WHEN s.cnt_stage NOT IN (1,9) " +
                  "     THEN (CASE WHEN s.val2 + s.val1>0 " +
                  "                THEN " + sNvlWord + "(s.val1_source)+s.val2 " +
                  "                ELSE 0" +
                  "                END) " +
                  "     ELSE s.val2 + s.val1 " +
                  "     END) rashod_source_hv," +
                  " MAX(CASE WHEN s.cnt_stage NOT IN (1,9) " +
                  "     THEN (CASE WHEN s.val2 + s.val1>0 " +
                  "           THEN s.up_kf else 1 " +
                  "           END) else 1 " +
                  "      END) up_kf_hv" +
                  " FROM " + gku.counters_xx + " s, t_rsh390 t" +
                  " WHERE t.nzp_kvar=s.nzp_kvar and s.nzp_serv=6 " + d_dat_charge +
                  " and s.nzp_type=3 and s.stek=20 and t.is_day_calc=1 " +
                  " AND t.dat_s>=s.dat_s AND t.dat_po<=s.dat_po " +
                  " GROUP BY 1,2";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, " create index ix1_t_rsh390_hv on t_rsh390_hv(nzp_kvar) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            sql = " CREATE TEMP TABLE t_rsh390_gv AS " +
                  " SELECT t.nzp_kvar,  t.nzp_period, max(s.cnt_stage) is_device_gv, max(s.rashod) rsh_gv," +
                  " max(case when s.cnt_stage in (1,9) then s.rashod else s.val1_g end) rsh_gv_g," +
                  " max(s.kod_info) kod_info_gv, max(s.val2 + s.val1) rsh_kv_gv, " +
                  " max(s.dop87) rsh_odn_gv,max(s.rash_norm_one) rash_norm_one_gv," +
                // превышение есть, если нет ИПУ
                      " MAX(CASE WHEN s.cnt_stage NOT IN (1,9) " +
                      "     THEN (CASE WHEN s.val2 + s.val1>0 " +
                      "                THEN " + sNvlWord + "(s.val1_source)+s.val2 " +
                      "                ELSE 0" +
                      "                END) " +
                      "     ELSE s.val2 + s.val1 " +
                      "     END) rashod_source_gv," +
                      " MAX(CASE WHEN s.cnt_stage NOT IN (1,9) " +
                      "     THEN (CASE WHEN s.val2 + s.val1>0 " +
                      "           THEN s.up_kf else 1 " +
                      "           END) else 1 " +
                      "      END) up_kf_gv" +
                  " FROM " + gku.counters_xx + " s, t_rsh390 t " +
                  " WHERE t.nzp_kvar=s.nzp_kvar and s.nzp_serv=9 " + d_dat_charge +
                  " and s.nzp_type=3 and s.stek=3 and t.is_day_calc=0 " +
                  " AND t.dat_s>=s.dat_s AND t.dat_po<=s.dat_po" +
                  " GROUP BY 1,2";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            sql = " INSERT INTO t_rsh390_gv (nzp_kvar,nzp_period,is_device_gv,rsh_gv,rsh_gv_g,kod_info_gv,rsh_kv_gv," +
                  " rsh_odn_gv,rash_norm_one_gv,rashod_source_gv,up_kf_gv)" +
                  "  SELECT t.nzp_kvar,  t.nzp_period, max(s.cnt_stage) is_device_gv, max(s.rashod) rsh_gv," +
                  " max(case when s.cnt_stage in (1,9) then s.rashod else s.val1_g end) rsh_gv_g," +
                  " max(s.kod_info) kod_info_gv, max(s.val2 + s.val1) rsh_kv_gv, " +
                  " max(s.dop87) rsh_odn_gv,max(s.rash_norm_one) rash_norm_one_gv," +
                  // превышение есть, если нет ИПУ
                  " MAX(CASE WHEN s.cnt_stage NOT IN (1,9) " +
                  "     THEN (CASE WHEN s.val2 + s.val1>0 " +
                  "                THEN " + sNvlWord + "(s.val1_source)+s.val2 " +
                  "                ELSE 0" +
                  "                END) " +
                  "     ELSE s.val2 + s.val1 " +
                  "     END) rashod_source_gv," +
                  " MAX(CASE WHEN s.cnt_stage NOT IN (1,9) " +
                  "     THEN (CASE WHEN s.val2 + s.val1>0 " +
                  "           THEN s.up_kf else 1 " +
                  "           END) else 1 " +
                  "      END) up_kf_gv" +
                  " FROM " + gku.counters_xx + " s, t_rsh390 t" +
                  " WHERE t.nzp_kvar=s.nzp_kvar and s.nzp_serv=9 " + d_dat_charge +
                  " and s.nzp_type=3 and s.stek=20 and t.is_day_calc=1 " +
                  " AND t.dat_s>=s.dat_s AND t.dat_po<=s.dat_po" +
                  " GROUP BY 1,2";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, " create index ix1_t_rsh390_gv on t_rsh390_gv(nzp_kvar) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // добавить расход по ХВС
            ret = ExecSQL(conn_db,
                " update t_rsh390 t set nzp_prm=1," +
                " is_device_hv = h.is_device_hv," +
                " rsh_hv = h.rsh_hv," +
                " rsh_hv_g = h.rsh_hv_g," +
                " kod_info_hv = h.kod_info_hv," +
                " rsh_kv_hv = h.rsh_kv_hv," +
                " rsh_odn_hv = h.rsh_odn_hv," +
                " rash_norm_one_hv = h.rash_norm_one_hv," +
                " rashod_source_hv = h.rashod_source_hv," +
                " up_kf_hv = h.up_kf_hv " +
                " FROM t_rsh390_hv h " +
                " WHERE h.nzp_kvar = t.nzp_kvar" +
                " and h.nzp_period= t.nzp_period"
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }


            // добавить расход по ГВС
            ret = ExecSQL(conn_db,
                " update t_rsh390 t set nzp_prm=nzp_prm+3," +
                " is_device_gv = g.is_device_gv," +
                " rsh_gv = g.rsh_gv," +
                " rsh_gv_g = g.rsh_gv_g," +
                " kod_info_gv = g.kod_info_gv," +
                " rsh_kv_gv = g.rsh_kv_gv," +
                " rsh_odn_gv = g.rsh_odn_gv," +
                " rash_norm_one_gv = g.rash_norm_one_gv," +
                " rashod_source_gv = g.rashod_source_gv," +
                " up_kf_gv = g.up_kf_gv " +
                " FROM t_rsh390_gv g " +
                " WHERE g.nzp_kvar = t.nzp_kvar" +
                " and g.nzp_period= t.nzp_period"
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // расход КАН = ХВС + ГВС
            ret = ExecSQL(conn_db,
                " update t_rsh390 set" +
                    " rsh_hv    = case when (kod_info_hv in (31,32,33,36)) and rsh_hv<0 then 0 else rsh_hv end," +
                    " rsh_hv_g  = case when (kod_info_hv in (31,32,33,36)) and rsh_hv_g<0 then 0 else rsh_hv_g end," +
                    " rsh_odn_hv= case when kod_info_hv in (31,32,33,36) then 0 else rsh_odn_hv end," +
                    " rsh_gv    = case when (kod_info_gv in (31,32,33,36)) and rsh_gv<0 then 0 else rsh_gv end," +
                    " rsh_gv_g  = case when (kod_info_gv in (31,32,33,36)) and rsh_gv_g<0 then 0 else rsh_gv_g end," +
                    " rsh_odn_gv= case when kod_info_gv in (31,32,33,36) then 0 else rsh_odn_gv end," +
                    " kod_info= case when kod_info_hv in (21,22,23,26)" +
                              " then kod_info_hv" +
                              " else " +
                                " case when kod_info_gv in (21,22,23,26)" +
                                " then kod_info_gv" +
                                " else kod_info" +
                                " end " +
                              " end " +
                //" where kod_info<>69 "
                " where 1=1 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " update t_rsh390 set rash_kv=rsh_kv_hv+rsh_kv_gv," +
                " rash_odn=rsh_odn_hv+rsh_odn_gv,rashod_g=rsh_hv_g+rsh_gv_g," +
                " rash_norm_one=rash_norm_one_hv+rash_norm_one_gv," +
                " rashod_source=rashod_source_hv+rashod_source_gv," +
                " up_kf=(case when up_kf_hv>up_kf_gv then up_kf_hv else up_kf_gv end)" +
                " where 1=1 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " update t_rsh390 set rsh_hv=rsh_kv_hv, rsh_gv=rsh_kv_gv, kod_info=kod_info_hv" +
                //" where kod_info<>69 and kod_info_hv in (21,22,23,26) and kod_info_gv in (21,22,23,26) "
                " where kod_info_hv in (21,22,23,26) and kod_info_gv in (21,22,23,26) "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " update t_rsh390 set rsh_hv=rsh_kv_hv, kod_info=kod_info_hv" +
                //" where kod_info<>69 and kod_info_hv in (21,22,23,26) and kod_info_gv not in (21,22,23,26) "
                " where kod_info_hv in (21,22,23,26) and kod_info_gv not in (21,22,23,26) "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " update t_rsh390 set rsh_gv=rsh_kv_gv, kod_info=kod_info_gv" +
                //" where kod_info<>69 and kod_info_hv not in (21,22,23,26) and kod_info_gv in (21,22,23,26) "
                " where kod_info_hv not in (21,22,23,26) and kod_info_gv in (21,22,23,26) "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " update t_rsh390 set rashod=rsh_hv+rsh_gv,rash_kv=rsh_hv+rsh_gv,nzp_prm=nzp_prm+100" +
                //" where kod_info<>69 "
                " where 1=1 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // перекрыть расходом КПУ по КАН, там где они есть
            ret = ExecSQL(conn_db,
                " update t_rsh390 set nzp_prm=0, rashod=rsh_kn, is_device=1, rash_odn=rsh_odn_kn, rash_kv=rsh_kv_kn, kod_info=kod_info_kn" +
                " where is_device_kn=1 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // вставка итогового расхода по КАН
            ret = ExecSQL(conn_db,
                " insert into t_rsh (nzp_kvar,nzp_frm,nzp_prm,rashod,rashod_g,rsh_hv,is_device,nzp_frm_typrs,kod_info" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn,rash_norm_one,rashod_source,up_kf)" +
                " select t.nzp_kvar,t.nzp_frm,t.nzp_prm," +
                " t.rashod ,t.rashod_g ,t.rashod_source_hv," +
                " t.is_device,390,t.kod_info," +
                " t.nzp_period,t.dat_s,t.dat_po,t.cntd,t.cntd_mn,t.rash_norm_one," +
                " (case when t.is_device not in (1,9) then t.rashod_source else t.rashod end)," +
                " t.up_kf" +
                " from t_rsh390 t where not (kod_info in (21,22,23,26)) "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // Пост 354
            // основная услуга
            ret = ExecSQL(conn_db,
                " insert into t_rsh (nzp_kvar,nzp_frm,nzp_prm,rashod,rashod_g,rsh_hv,is_device,nzp_frm_typrs,kod_info,nzp_frm_typ" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn,rash_norm_one,rashod_source,up_kf)" +
                " select t.nzp_kvar,t.nzp_frm,t.nzp_prm," +
                " t.rash_kv ,t.rashod_g ,t.rashod_source_hv ," +
                " t.is_device,390,t.kod_info,t.nzp_frm_typ" +
                " ,t.nzp_period,t.dat_s,t.dat_po,t.cntd,t.cntd_mn,t.rash_norm_one" +
                " ,(case when t.is_device not in (1,9) then t.rashod_source else t.rashod end),t.up_kf" +
                " from t_rsh390 t where (kod_info in (21,22,23,26)) "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // услуга на ОДН
            ret = ExecSQL(conn_db,
                " insert into t_rsh (nzp_kvar,nzp_frm,nzp_prm,nzp_frm_typ,rashod,rashod_g,rsh_hv,is_device,nzp_frm_typrs,kod_info" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn,rashod_source,up_kf)" +
                " select t.nzp_kvar,t.nzp_frm,0 nzp_prm,f.nzp_frm_typ," +
                " max(s.rash_odn),max(s.rash_odn),max(s.rsh_odn_hv)," +
                " max(s.is_device),390,max(s.kod_info)" +
                " ,t.nzp_period,max(t.dat_s),max(t.dat_po),max(t.cntd),max(t.cntd_mn),max(s.rash_odn),1" +
                " from t_tarifs t," + gku.paramcalc.kernel_alias + "formuls_opis f,t_rsh390 s" +
                " where t.nzp_kvar=s.nzp_kvar and t.nzp_serv=511 and t.nzp_period=s.nzp_period " +
                " and (s.kod_info in (21,22,23,26))" +
                " and t.nzp_frm=f.nzp_frm and f.nzp_frm_typrs=390" +
                " group by 1,2,3,4,11 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " update t_rsh set valm=rashod " +
                " where nzp_frm_typrs=390 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            return true;
        }
        #endregion Тип расхода 390 -КАН =ХВ+ГВ

        #region Тип расхода 339 -> КАН = ХВ
        public bool CalcTypeRashod_339(IDbConnection conn_db, Gku gku, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            string d_dat_charge = " and s.dat_charge is null ";
            if (!gku.paramcalc.b_cur)
            {
                d_dat_charge = " and s.dat_charge = " + MDY(gku.paramcalc.cur_mm, 28, gku.paramcalc.cur_yy);
            }

            ret = ExecSQL(conn_db,
                " create temp table t_rsh339 (" +
                " nzp_kvar integer," +
                " nzp_frm  integer," +
                " nzp_prm  integer default 0," +
                " rashod   " + sDecimalType + "(16,6) default 0," +
                " rashod_g " + sDecimalType + "(16,6) default 0," +
                " rash_kv  " + sDecimalType + "(16,6) default 0," +
                " rash_odn " + sDecimalType + "(16,6) default 0," +
                " rsh_kn   " + sDecimalType + "(16,6) default 0," +
                " rsh_hv   " + sDecimalType + "(16,6) default 0," +
                " rsh_kn_g   " + sDecimalType + "(16,6) default 0," +
                " rsh_hv_g   " + sDecimalType + "(16,6) default 0," +
                " rsh_kv_kn    " + sDecimalType + "(16,6) default 0," +
                " rsh_kv_hv    " + sDecimalType + "(16,6) default 0," +
                " rsh_odn_kn   " + sDecimalType + "(16,6) default 0," +
                " rsh_odn_hv   " + sDecimalType + "(16,6) default 0," +
                " rash_norm_one    " + sDecimalType + "(16,6) default 0," +
                " rash_norm_one_hv " + sDecimalType + "(16,6) default 0," +
                " is_device    integer  default 0," +
                " is_device_kn integer  default 0," +
                " is_device_hv integer  default 0," +
                " kod_info     integer  default 0," +
                " kod_info_kn  integer  default 0," +
                " kod_info_hv  integer  default 0," +
                " nzp_frm_typ  integer  default 0 not null," +
                " rashod_source " + sDecimalType + "(16,7) default 0," +
                " up_kf " + sDecimalType + "(16,7) default 0," +
                " rashod_source_hv " + sDecimalType + "(16,7) default 0," +
                " up_kf_hv " + sDecimalType + "(16,7) default 0," +
                " nzp_period integer," +
                " dat_s    DATE," +
                " dat_po   DATE," +
                " cntd     integer, " +
                " cntd_mn  integer  " +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // вставить все расходы по КАН, где есть формулы
            ret = ExecSQL(conn_db,
                " insert into t_rsh339 (nzp_kvar,nzp_frm,nzp_frm_typ,nzp_prm,rsh_kn,rsh_kn_g,is_device_kn,kod_info_kn,rsh_kv_kn,rsh_odn_kn,rash_norm_one" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn,rashod_source,up_kf)" +
                " select t.nzp_kvar,t.nzp_frm,f.nzp_frm_typ,0 nzp_prm,max(" + sNvlWord + "(s.rashod,0))," +
                " max(case when s.cnt_stage in (1,9) then " + sNvlWord + "(s.rashod,0) else " + sNvlWord + "(s.val1_g,0) end)," +
                " max(" + sNvlWord + "(s.cnt_stage,0)),max(" + sNvlWord + "(s.kod_info,0))," +
                " max(" + sNvlWord + "(s.val2,0) + " + sNvlWord + "(s.val1,0)),max(" + sNvlWord + "(s.dop87,0))," +
                " max(" + sNvlWord + "(s.rash_norm_one,0)) " +
                " ,t.nzp_period,max(t.dat_s),max(t.dat_po),max(t.cntd),max(t.cntd_mn)" +
                " ,max(s.val1_source),max(s.up_kf) " +
                " from t_tarifs t left outer join " + gku.counters_xx + " s on t.nzp_kvar=s.nzp_kvar and t.nzp_serv=s.nzp_serv and s.nzp_type=3 and s.stek=3,"
                + gku.paramcalc.pref + "_kernel.formuls_opis f " +
                " where  " +
                " t.nzp_serv in (7,324,353) " + d_dat_charge +
                " and t.nzp_frm=f.nzp_frm and f.nzp_frm_typrs=339" +
                " group by 1,2,3,12 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, " create index ixt_rsh1_339 on t_rsh339(nzp_kvar) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, " create index ixt_rsh2_339 on t_rsh339(kod_info) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, sUpdStat + " t_rsh339 ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // добавить расход по ХВС
            ret = ExecSQL(conn_db,
                " update t_rsh339 set nzp_prm=0," +
                " is_device_hv = (select max(s.cnt_stage) from " + gku.counters_xx + " s" +
                  " where t_rsh339.nzp_kvar=s.nzp_kvar and s.nzp_serv=6 " + d_dat_charge + " and s.nzp_type=3 and s.stek=3)," +
                " rsh_hv = (select max(s.rashod) from " + gku.counters_xx + " s" +
                  " where t_rsh339.nzp_kvar=s.nzp_kvar and s.nzp_serv=6 " + d_dat_charge + " and s.nzp_type=3 and s.stek=3)," +
                " rsh_hv_g = (select max(case when s.cnt_stage in (1,9) then s.rashod else s.val1_g end) from " + gku.counters_xx + " s" +
                  " where t_rsh339.nzp_kvar=s.nzp_kvar and s.nzp_serv=6 " + d_dat_charge + " and s.nzp_type=3 and s.stek=3)," +
                " kod_info_hv = (select max(s.kod_info) from " + gku.counters_xx + " s" +
                  " where t_rsh339.nzp_kvar=s.nzp_kvar and s.nzp_serv=6 " + d_dat_charge + " and s.nzp_type=3 and s.stek=3)," +
                " rsh_kv_hv = (select max(s.val2 + s.val1) from " + gku.counters_xx + " s" +
                  " where t_rsh339.nzp_kvar=s.nzp_kvar and s.nzp_serv=6 " + d_dat_charge + " and s.nzp_type=3 and s.stek=3)," +
                " rsh_odn_hv = (select max(s.dop87) from " + gku.counters_xx + " s" +
                  " where t_rsh339.nzp_kvar=s.nzp_kvar and s.nzp_serv=6 " + d_dat_charge + " and s.nzp_type=3 and s.stek=3)," +
                " rash_norm_one_hv = (select max(s.rash_norm_one) from " + gku.counters_xx + " s" +
                  " where t_rsh339.nzp_kvar=s.nzp_kvar and s.nzp_serv=6 " + d_dat_charge + " and s.nzp_type=3 and s.stek=3)," +
                " rashod_source_hv = (select max(s.val1_source) from " + gku.counters_xx + " s" +
                  " where t_rsh339.nzp_kvar=s.nzp_kvar and s.nzp_serv=6 " + d_dat_charge + " and s.nzp_type=3 and s.stek=3)," +
                " up_kf_hv = (select max(s.up_kf) from " + gku.counters_xx + " s" +
                  " where t_rsh339.nzp_kvar=s.nzp_kvar and s.nzp_serv=6 " + d_dat_charge + " and s.nzp_type=3 and s.stek=3) " +
                " where exists ( select 1 from " + gku.counters_xx + " s" +
                " where t_rsh339.nzp_kvar=s.nzp_kvar and s.nzp_serv=6 " + d_dat_charge + " and s.nzp_type=3 and s.stek=3 )"
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            #region установка расхода КАН 339

            // обрезать расход ХВС с отрицательным значением из-за ОДН
            ret = ExecSQL(conn_db,
                " update t_rsh339 set" +
                    " rsh_hv    = case when rsh_hv<0   then 0 else rsh_hv end," +
                    " rsh_hv_g  = case when rsh_hv_g<0 then 0 else rsh_hv_g end," +
                    " rsh_odn_hv= 0 " +
                " where kod_info_hv in (31,32,33,36) "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // установить расход по ХВС, по-умолчанию всем
            // установить расход по КАН-нормативу для ХВ, там где норматив ХВС
            ret = ExecSQL(conn_db,
                " update t_rsh339" +
                " set" +
                " nzp_prm  = case when is_device_hv=0 and kod_info_hv=0 then 3      else 1            end," +
                " rashod   = case when is_device_hv=0 and kod_info_hv=0 then rsh_kn else rsh_hv       end," +
                " rashod_g = case when is_device_hv=0 and kod_info_hv=0 then rsh_kn else rsh_hv_g     end," +
                " is_device= case when is_device_hv=0 and kod_info_hv=0 then 0      else is_device_hv end," +
                " rash_kv  = case when is_device_hv=0 and kod_info_hv=0 then rsh_kn else rsh_kv_hv    end," +
                " rash_odn = case when is_device_hv=0 and kod_info_hv=0 then 0      else rsh_odn_hv   end," +
                " kod_info = case when is_device_hv=0 and kod_info_hv=0 then 0      else kod_info_hv  end," +
                " rash_norm_one = case when is_device_hv=0 and kod_info_hv=0 then rash_norm_one else rash_norm_one_hv end, " +
                " rashod_source = case when is_device_hv=0 and kod_info_hv=0 then rashod_source else rashod_source_hv end" +
                " where 1=1 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }


            ret = ExecSQL(conn_db,
                " update t_rsh339" +
                " set" +
                " rashod   =  rsh_kn *  up_kf_hv," +
                " rashod_g =  rsh_kn *  up_kf_hv," +
                " rash_kv  =  rsh_kn *  up_kf_hv," +
                " rashod_source =  rsh_kn," +
                " up_kf = up_kf_hv " +
                " where up_kf_hv>1 and is_device_hv=0 and (kod_info_hv=0 OR kod_info_hv in (21,22,23,26,27))"
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }



            // установить расход ИПУ по КАН, там где он есть
            ret = ExecSQL(conn_db,
                " update t_rsh339" +
                " set nzp_prm=2, rashod=rsh_kn, rashod_g=rsh_kn_g, is_device=is_device_kn, rash_kv=rsh_kv_kn," +
                    " rash_odn=rsh_odn_kn, kod_info=kod_info_kn, rashod_source=rsh_kn, up_kf=1" +
                " where is_device_kn in (1,9) "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            #endregion установка расхода КАН 339

            // вставка итогового расхода по КАН
            ret = ExecSQL(conn_db,
                " insert into t_rsh (nzp_kvar,nzp_frm,nzp_prm,rashod,rashod_g,rsh_hv,is_device,nzp_frm_typrs,kod_info,rash_norm_one" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn,rashod_source,up_kf)" +
                " select nzp_kvar,nzp_frm,nzp_prm,rashod,rashod_g,rsh_hv,is_device,339,kod_info,rash_norm_one" +
                " ,nzp_period,dat_s,dat_po,cntd,cntd_mn,rashod_source,up_kf" +
                " from t_rsh339 where not (kod_info in (21,22,23,26)) "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // Пост 354
            // основная услуга
            ret = ExecSQL(conn_db,
                " insert into t_rsh (nzp_kvar,nzp_frm,nzp_prm,rashod,rashod_g,rsh_hv,is_device,nzp_frm_typrs,kod_info,nzp_frm_typ,rash_norm_one" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn,rashod_source,up_kf)" +
                " select nzp_kvar,nzp_frm,nzp_prm,rash_kv,rashod_g,rsh_kv_hv,is_device,339,kod_info,nzp_frm_typ,rash_norm_one" +
                " ,nzp_period,dat_s,dat_po,cntd,cntd_mn,rashod_source,up_kf" +
                " from t_rsh339 where (kod_info in (21,22,23,26)) "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // услуга на ОДН
            ret = ExecSQL(conn_db,
                " insert into t_rsh (nzp_kvar,nzp_frm,nzp_prm,nzp_frm_typ,rashod,rashod_g,rsh_hv,is_device,nzp_frm_typrs,kod_info,rash_norm_one" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn,rashod_source,up_kf)" +
                " select t.nzp_kvar,t.nzp_frm,0 nzp_prm,f.nzp_frm_typ,max(s.rash_odn),max(s.rash_odn),max(s.rsh_odn_hv),max(s.is_device),339,max(s.kod_info),max(s.rash_norm_one)" +
                " ,t.nzp_period,max(t.dat_s),max(t.dat_po),max(t.cntd),max(t.cntd_mn),max(rash_odn),1" +
                " from t_tarifs t," + gku.paramcalc.kernel_alias + "formuls_opis f,t_rsh339 s" +
                " where t.nzp_kvar=s.nzp_kvar and t.nzp_serv=511 " +
                " and (s.kod_info in (21,22,23,26))" +
                " and t.nzp_frm=f.nzp_frm and f.nzp_frm_typrs=339" +
                " group by 1,2,3,4,12 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " update t_rsh set valm=rashod " +
                " where nzp_frm_typrs=339 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            return true;
        }
        #endregion Тип расхода 339 -> КАН = ХВ

        #region выборка расходов по ПУ - для типа расхода =  814
        public bool SelTypeRashod_814_514(IDbConnection conn_db, Gku gku, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            string d_dat_charge = " and s.dat_charge is null ";
            if (!gku.paramcalc.b_cur)
            {
                d_dat_charge = " and s.dat_charge = " + MDY(gku.paramcalc.cur_mm, 28, gku.paramcalc.cur_yy);
            }

            ret = ExecSQL(conn_db,
                " create temp table t_rsh814 (" +
                " nzp_kvar integer," +
                " nzp_frm  integer," +
                " nzp_frm_typ   integer default 0 not null," +
                " nzp_frm_typrs integer default 0 not null," +
                " rash_norm_one " + sDecimalType + "(16,7) default 0," +
                " rashod   " + sDecimalType + "(16,7) default 0," +
                " rashod_g " + sDecimalType + "(16,7) default 0," +
                " rsh1     " + sDecimalType + "(16,7) default 0," +
                " rsh2     " + sDecimalType + "(16,7) default 0," +
                " is_device     integer  default 0," +
                " nzp_period integer," +
                " dat_s    DATE," +
                " dat_po   DATE," +
                " cntd     integer, " +
                " cntd_mn  integer, " +
                " val1_source " + sDecimalType + "(16,7) default 0," +
                " up_kf     " + sDecimalType + "(16,7) default 1" +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " insert into t_rsh814 (nzp_kvar,nzp_frm,nzp_frm_typ,nzp_frm_typrs,rashod,rashod_g,rsh1,rsh2,is_device," +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn,rash_norm_one,up_kf,val1_source)" +
                " select t.nzp_kvar,t.nzp_frm,f.nzp_frm_typ,f.nzp_frm_typrs,max(s.squ2),max(s.squ2)," +
                " max(case when s.kod_info>4 then (case when s.cnt_stage=0 then s.kf307n else s.kf307 end) else (case when s.cnt_stage=0 then s.val3 else s.val4 end) end)," +
                " max(s.rashod),max(s.cnt_stage)" +
                " ,t.nzp_period,max(t.dat_s),max(t.dat_po),max(t.cntd),max(t.cntd_mn),max(rash_norm_one)" +
                " ,max(s.up_kf),max(case when s.cnt_stage not in (1,9) and (s.kod_info=0) then s.val1_source else s.rashod end)" +
                " from t_tarifs t," + gku.paramcalc.kernel_alias + "formuls_opis f," + gku.counters_xx + " s" +
                " where t.nzp_kvar=s.nzp_kvar and t.nzp_serv=s.nzp_serv and t.nzp_serv=8 " + d_dat_charge +
                " and s.nzp_type=3 and s.stek=3" +
                " and t.nzp_frm=f.nzp_frm and f.nzp_frm_typrs in (814,514)" +
                " group by 1,2,3,4,10 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, " create index ix1t_rsh814 on t_rsh814(nzp_kvar,nzp_frm,nzp_period,nzp_frm_typrs) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, " create index ix2t_rsh814 on t_rsh814(nzp_frm_typrs) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, sUpdStat + " t_rsh814 ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }


            return true;
        }
        #endregion выборка расходов по ПУ - для типа расхода =  814

        #region выборка расходов по ПУ - для типа расхода =  1814  [(отопление Гкал)]
        public bool CalcTypeRashod_1814(IDbConnection conn_db, Gku gku, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            string d_dat_charge = " and s.dat_charge is null ";
            if (!gku.paramcalc.b_cur)
            {
                d_dat_charge = " and s.dat_charge = " + MDY(gku.paramcalc.cur_mm, 28, gku.paramcalc.cur_yy);
            }

            ret = ExecSQL(conn_db,
                " create temp table t_rsh1814 (" +
                " nzp_kvar integer," +
                " nzp_frm  integer," +
                " rash_norm_one " + sDecimalType + "(16,7) default 0," +
                " rashod   " + sDecimalType + "(16,7) default 0," +
                " rashod_g " + sDecimalType + "(16,7) default 0," +
                " rsh1     " + sDecimalType + "(16,7) default 0," +
                " rsh2     " + sDecimalType + "(16,7) default 0," +
                " is_device     integer  default 0," +
                " kod_info      integer  default 0," +
                " nzp_frm_typ   integer default 0 not null," +
                " nzp_frm_typrs integer default 0 not null," +
                " nzp_period integer," +
                " dat_s    DATE," +
                " dat_po   DATE," +
                " cntd     integer, " +
                " cntd_mn  integer,  " +
                " val1_source " + sDecimalType + "(16,7) default 0," +
                " up_kf     " + sDecimalType + "(16,7) default 1" +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " insert into t_rsh1814 (nzp_kvar,nzp_frm,nzp_frm_typ,nzp_frm_typrs,rashod,rashod_g,rsh1,rsh2,is_device,kod_info" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn,rash_norm_one,up_kf,val1_source)" +
                " select t.nzp_kvar,t.nzp_frm,f.nzp_frm_typ,f.nzp_frm_typrs,max(s.squ2),max(s.squ2)," +
                " max(case when s.kod_info>4 then (case when s.cnt_stage=0 then s.kf307n else s.kf307 end) else " +
                " (case when s.cnt_stage=0 then s.val3 else s.val4 end) end)," +
                " max(s.rashod),max(s.cnt_stage),max(s.kod_info)" +
                " ,t.nzp_period,max(t.dat_s),max(t.dat_po),max(t.cntd),max(t.cntd_mn),max(rash_norm_one)" +
                " ,max(s.up_kf),max(case when s.cnt_stage not in (1,9) and (s.kod_info=0) then s.val1_source else s.rashod end)" +
                " from t_tarifs t," + gku.paramcalc.kernel_alias + "formuls_opis f," + gku.counters_xx + " s" +
                " where t.nzp_kvar=s.nzp_kvar and t.nzp_serv=s.nzp_serv and t.nzp_serv=8 " + d_dat_charge +
                " and s.nzp_type=3 and s.stek=3 and t.is_day_calc=0" +
                " and t.nzp_frm=f.nzp_frm and f.nzp_frm_typrs in (1814)" +
                " group by 1,2,3,4,11 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " insert into t_rsh1814 (nzp_kvar,nzp_frm,nzp_frm_typ,nzp_frm_typrs,rashod,rashod_g,rsh1,rsh2,is_device,kod_info" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn,rash_norm_one,up_kf,val1_source)" +
                " select t.nzp_kvar,t.nzp_frm,f.nzp_frm_typ,f.nzp_frm_typrs,max(s.squ2),max(s.squ2)," +
                " max(case when s.kod_info>4 then (case when s.cnt_stage=0 then s.kf307n else s.kf307 end) else " +
                " (case when s.cnt_stage=0 then s.val3 else s.val4 end) end)," +
                " max(s.rashod),max(s.cnt_stage),max(s.kod_info)" +
                " ,t.nzp_period,max(t.dat_s),max(t.dat_po),max(t.cntd),max(t.cntd_mn),max(rash_norm_one)" +
                " ,max(s.up_kf),max(case when s.cnt_stage not in (1,9) and (s.kod_info=0) then s.val1_source else s.rashod end)" +
                " from t_tarifs t," + gku.paramcalc.kernel_alias + "formuls_opis f," + gku.counters_xx + " s" +
                " where t.nzp_kvar=s.nzp_kvar and t.nzp_serv=s.nzp_serv and t.nzp_serv=8 " + d_dat_charge +
                " and s.nzp_type=3 and s.stek=20 and t.is_day_calc=1" +
                " and t.dat_s<=s.dat_po and t.dat_po>=s.dat_s" +
                " and t.nzp_frm=f.nzp_frm and f.nzp_frm_typrs in (1814)" +
                " group by 1,2,3,4,11 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, " create index ixt_rsh1814 on t_rsh1814(nzp_kvar,nzp_frm,nzp_period) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, sUpdStat + " t_rsh1814 ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            return true;
        }
        #endregion выборка расходов по ПУ - для типа расхода =  1814  [(отопление Гкал)]

        #region тип расхода = 814,514  [(Отопление (от ГКал-СН по отопительной площади)), (отопление Гкал)]
        public bool CalcTypeRashod_814_514(IDbConnection conn_db, Gku gku, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            ret = ExecSQL(conn_db,
                " insert into t_rsh" +
                " (nzp_kvar,nzp_frm,nzp_frm_typ,nzp_frm_typrs,nzp_prm,rashod,rashod_g,rsh1,rsh2,is_device,rash_norm_one" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn,rashod_source,up_kf)" +
                " select" +
                "  nzp_kvar,nzp_frm,nzp_frm_typ,nzp_frm_typrs,0 nzp_prm,rashod,rashod_g,rsh1,rsh2,is_device,rash_norm_one" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn,val1_source,up_kf " +
                " from t_rsh814 where nzp_frm_typrs=814 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " insert into t_rsh" +
                " (nzp_kvar,nzp_frm,nzp_frm_typ,nzp_frm_typrs,nzp_prm,rashod,rashod_g,rsh1,rsh2,rsh3,is_device,rash_norm_one" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn,rashod_source,up_kf)" +
                " select" +
                "  nzp_kvar,nzp_frm,nzp_frm_typ,nzp_frm_typrs,0 nzp_prm,rsh2,rsh2,rsh1,rsh2,rashod,is_device,rash_norm_one" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn,val1_source,up_kf " +
                " from t_rsh814 where nzp_frm_typrs=514  "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            return true;
        }
        #endregion тип расхода = 814,514  [(Отопление (от ГКал-СН по отопительной площади)), (отопление Гкал)]

        #endregion Типы расчетов расхода

        // Основные действия разбора формул расчета здесь
        #region Непосредственно подготовка расходов для расчета (Главная функция)
        //-----------------------------------------------------------------------------
        public bool CalcGkuXX(IDbConnection conn_db, CalcTypes.ParamCalc paramcalc, out Returns ret)
        //-----------------------------------------------------------------------------
        {
            #region Подготовка к расчету расходов
            Gku gku = new Gku(paramcalc);

            //---------------------------------------------------
            //выбрать множество лицевых счетов
            //---------------------------------------------------
            if (!TempTableInWebCashe(conn_db, "t_opn"))
            {
                ChoiseTempKvar(conn_db, ref paramcalc, out ret);
                if (!ret.result)
                {
                    ///*conn_db.Close();*/
                    return false;
                }
            }
            IDataReader reader;

            string s_dat_charge = "";
            string f_dat_charge = "";
            string d_dat_charge = " and dat_charge is null ";
            if (!gku.paramcalc.b_cur)
            {
                s_dat_charge = MDY(gku.paramcalc.cur_mm, 28, gku.paramcalc.cur_yy) + ",";
                f_dat_charge = "dat_charge,";
                d_dat_charge = " and dat_charge = " + MDY(gku.paramcalc.cur_mm, 28, gku.paramcalc.cur_yy);
            }

            CreateGkuXX(conn_db, gku, out ret);
            if (!ret.result)
            {
                CalcGkuXX_CloseTmp(conn_db);
                ///*conn_db.Close();*/ 
                return false;
            }
            //
            // удалить временные таблицы
            CalcGkuXX_CloseTmp(conn_db);

            // ... для расчета дотаций ...
            // ... !!! временно до появления переменной в Point.* !!! ... 
            bool bIsCalcSubsidy =
                CheckValBoolPrmWithVal(conn_db, gku.paramcalc.data_alias, 1992, "5", "1", gku.dat_s, gku.dat_po);

            // ... Saha
            bool bIsSaha =
                CheckValBoolPrm(conn_db, Points.Pref + "_data" + tableDelimiter, 9000, "5");

            #endregion Подготовка к расчету расходов

            #region Выбрать параметры лицевого счета и дома на дату t_par1 & t_par2 & t_opn_dom

            SelParamsForCalc(conn_db, gku, bIsCalcSubsidy, out ret);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            #endregion Выбрать параметры лицевого счета и дома на дату t_par1 & t_par2 & t_opn_dom

            #region Подсчитать количество жильцов

            ret = ExecSQL(conn_db,
                " create temp table t_rsh_kolgil (" +
                " nzp_kvar integer," +
                " kg   " + sDecimalType + "(11,7) not null default 0," +
                " kg_g " + sDecimalType + "(11,7) not null default 0," +
                " kvp  " + sDecimalType + "(11,7) not null default 0," +
                " knpl " + sDecimalType + "(11,7) not null default 0," +
                " knp  " + sDecimalType + "(11,7) not null default 0," +
                " dat_s  date not null," +
                " dat_po date not null " +
                " )  " + sUnlogTempTable
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*/conn_db.Close();/*/ return false; }

            // gil_xx - cnt1 - целое число жильцов без учета правила 15 дней, но с учетом временно выбывших
            // gil_xx - cnt2 - кол-во жильцов с учетом правила 15 дней без учета временно выбывших
            // gil_xx - val1 - дробное число жильцов без учета правила 15 дней, но с учетом временно выбывших
            // gil_xx - val2 - кол-во жильцов цифрой из БД (выбран nzp_prm=5 из prm_1)
            // gil_xx - val3 - кол-во временно прибывших (выбран nzp_prm=131 из prm_1)
            // gil_xx - val5 - временно выбывших (nzp_prm=10 - это всегда из gil_periods / м.б. nzp_prm=10 ! по признаку)
            // gil_xx - val6 - Количество незарегистврированных проживающих (выбран nzp_prm=1395 из prm_1)
            ret = ExecSQL(conn_db,
                " insert into t_rsh_kolgil (nzp_kvar,dat_s,dat_po,kg,kvp,kg_g,knp)" +
                " select t.nzp_kvar," + gku.paramcalc.dat_s + "," + gku.paramcalc.dat_po + "," +
                " max(case when (g.cnt2 - g.val5)>0 then g.cnt2 - g.val5 else 0 end)," +
                " max(g.val3),max(g.cnt2),max(g.val6)" +
                " from t_opn t," + gku.gil_xx + " g " +
                " where t.nzp_kvar=g.nzp_kvar and g.stek=3 and t.is_day_calc=0 " +
                " group by 1 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/  return false; }

            ret = ExecSQL(conn_db,
                " insert into t_rsh_kolgil (nzp_kvar,dat_s,dat_po,kg,kvp,kg_g,knp)" +
                " select t.nzp_kvar,g.dat_s,g.dat_po,max(case when (g.cnt2 - g.val5)>0 then g.cnt2 - g.val5 else 0 end)," +
                " max(g.val3),max(g.cnt2),max(g.val6)" +
                " from t_opn t," + gku.gil_xx + " g " +
                " where t.nzp_kvar=g.nzp_kvar and g.stek=4 and t.is_day_calc=1 " +
                " group by 1,2,3 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/  return false; }



            ret = ExecSQL(conn_db, " create index ixt_rsh_kolgil on t_rsh_kolgil (nzp_kvar,dat_s,dat_po) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/  return false; }

            ret = ExecSQL(conn_db, sUpdStat + " t_rsh_kolgil ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            ret = ExecSQL(conn_db,
                " update t_rsh_kolgil set knpl=" +
                " (select max(p.val" + sConvToNum + ") from t_par1 p where t_rsh_kolgil.nzp_kvar=p.nzp and p.nzp_prm=9)" +
                " where exists (select 1 from t_par1 p where t_rsh_kolgil.nzp_kvar=p.nzp and p.nzp_prm=9) "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            #endregion Подсчитать количество жильцов

            #region Предварительно считать все тарифы по выбранным лицевым счетам t_tarifs & t_trf

            #region выбрать только нужные тарифы t_tarifs

            ret = ExecSQL(conn_db,
                " create temp table t_tarifs_uni (" +
                " nzp_dom  integer not null," +
                " nzp_kvar integer not null," +
                " nzp_serv integer not null," +
                " nzp_supp integer not null," +
                " is_day_calc integer not null," +
                " is_ipu      integer not null," +
                " is_use_knp  integer not null," +
                " is_use_ctr  integer not null " +
                " )  " + sUnlogTempTable
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // выбрать уникальные связки ЛС/услуга/договор поставщика
            ret = ExecSQL(conn_db,
                " insert into t_tarifs_uni (nzp_dom,nzp_kvar,nzp_serv,nzp_supp,is_day_calc,is_ipu,is_use_knp,is_use_ctr)" +
                " select k.nzp_dom,t.nzp_kvar,t.nzp_serv,t.nzp_supp,max(k.is_day_calc),max(t.is_ipu),max(t.is_use_knp),max(t.is_use_ctr)" +
                " from temp_table_tarif t, t_opn k " +
                " where t.nzp_kvar=k.nzp_kvar" +
                " group by 1,2,3,4 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, " create index ixt_tarifs_uni on t_tarifs_uni(nzp_kvar,nzp_serv,nzp_supp) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, sUpdStat + " t_tarifs_uni ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " create temp table t_tarifs (" +
                " nzp_key  serial not null," +
                " nzp_dom  integer not null," +
                " nzp_kvar integer not null," +
                " nzp_serv integer not null," +
                " nzp_supp integer not null," +
                " nzp_frm  integer not null," +
                " is_day_calc integer not null," +
                " is_ipu      integer not null," +
                " is_use_knp  integer not null," +
                " is_use_ctr  integer not null," +
                " nzp_period integer not null," +
                " dat_s    DATE not null," +
                " dat_po   DATE not null," +
                " cntd     integer not null, " +
                " cntd_mn  integer not null  " +
                " )  " + sUnlogTempTable
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }
            var where_prehibited_recalc = " and not exists (select 1 from temp_prohibited_recalc pr " +
                                          " where t.nzp_serv = pr.nzp_serv and t.nzp_supp = pr.nzp_supp and t.nzp_dom = pr.nzp_dom" +
                                          " and pr.nzp_kvar = t.nzp_kvar)";

            // по связке ЛС/услуга/договор поставщика установить периоды расчета
            // имеет смысл для по-дневного расчета - для обычного расчета будет 1 строка/период!
            // t_gku_periods - содержит только открытые ЛС из t_opn!
            ret = ExecSQL(conn_db,
                " insert into t_tarifs (nzp_dom,nzp_kvar,nzp_serv,nzp_supp,nzp_frm,nzp_period,dat_s,dat_po,cntd,cntd_mn,is_day_calc,is_ipu,is_use_knp,is_use_ctr)" +
                " select t.nzp_dom,t.nzp_kvar,t.nzp_serv,t.nzp_supp,0 nzp_frm,k.nzp_period,k.dp,k.dp_end,k.cntd,k.cntd_mn,t.is_day_calc,t.is_ipu,t.is_use_knp,t.is_use_ctr" +
                " from t_tarifs_uni t,t_gku_periods k" +
                " where t.nzp_kvar=k.nzp_kvar " + where_prehibited_recalc
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, " create index ixt_tarifs0 on t_tarifs(nzp_kvar,nzp_serv,nzp_supp,dat_s,dat_po) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, sUpdStat + " t_tarifs ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // установить формулы расчета
            ret = ExecSQL(conn_db,
                " update t_tarifs set nzp_frm=" +
                " ( select max(t.nzp_frm) from temp_table_tarif t" +
                  " where t.nzp_kvar=t_tarifs.nzp_kvar and t.nzp_serv=t_tarifs.nzp_serv and t.nzp_supp=t_tarifs.nzp_supp" +
                  " and t.dat_s<=t_tarifs.dat_po and t.dat_po>=t_tarifs.dat_s ) " +
                " Where exists ( select 1 from temp_table_tarif t" +
                  " where t.nzp_kvar=t_tarifs.nzp_kvar and t.nzp_serv=t_tarifs.nzp_serv and t.nzp_supp=t_tarifs.nzp_supp" +
                  " and t.dat_s<=t_tarifs.dat_po and t.dat_po>=t_tarifs.dat_s ) " +
                  " and exists ( select 1 from ttt_prm_3 t " +
                         " where t_tarifs.nzp_kvar=t.nzp and t_tarifs.dat_s <= t.dat_po and t_tarifs.dat_po >= t.dat_s) "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, " create unique index ixt_tarifs1 on t_tarifs(nzp_key) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, " create index ixt_tarifs2 on t_tarifs(nzp_dom) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, " create index ixt_tarifs3 on t_tarifs(nzp_kvar,nzp_frm,nzp_period) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, " create index ixt_tarifs4 on t_tarifs(nzp_kvar,nzp_frm,nzp_supp,nzp_period) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, " create index ixt_tarifs5 on t_tarifs(is_day_calc) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, sUpdStat + " t_tarifs ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            #endregion выбрать только нужные тарифы t_tarifs

            #region выбрать уникальные тарифы в t_trf

            ret = ExecSQL(conn_db,
                " create temp table t_trf (" +
                " nzp_dom  integer," +
                " nzp_kvar integer," +
                " nzp_supp integer," +
                " nzp_frm  integer," +
                " is_ipu     integer not null," +
                " is_use_knp integer not null," +
                " is_use_ctr integer not null," +
                " nzp_period integer," +
                " dat_s    DATE," +
                " dat_po   DATE," +
                " cntd     integer, " +
                " cntd_mn  integer  " +
                " )  " + sUnlogTempTable
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " insert into t_trf (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_period,dat_s,dat_po,cntd,cntd_mn,is_ipu,is_use_knp,is_use_ctr) " +
                " select nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_period,max(dat_s),max(dat_po),max(cntd),max(cntd_mn),max(is_ipu),max(is_use_knp),max(is_use_ctr) " +
                " from t_tarifs " +
                " where nzp_frm>0 " +
                " group by 1,2,3,4,5 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, " create index ixt_trf1 on t_trf(nzp_kvar,nzp_supp,nzp_frm) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, " create index ixt_trf2 on t_trf(nzp_dom) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, " create index ixt_trf3 on t_trf(nzp_kvar,dat_s,dat_po) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, " create index ixt_trf4 on t_trf(nzp_dom,dat_s,dat_po) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, " create index ixt_trf5 on t_trf(is_use_knp) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, " create index ixt_trf6 on t_trf(is_use_ctr) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, sUpdStat + " t_trf ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            #endregion выбрать уникальные тарифы в t_trf

            #endregion Предварительно считать все тарифы по выбранным лицевым счетам t_tarifs & t_trf

            //
            //          Выборка тарифов
            //

            #region создать сводную таблицу t_prm (тарифы)

            #region создание таблицы тарифов t_prm

            ret = ExecSQL(conn_db,
                " create temp table t_prm (" +
                " nzp_dom  integer," +
                " nzp_kvar integer," +
                " nzp_supp integer," +
                " nzp_frm  integer," +
                " priority integer," +
                " nzp_prm  integer," +
                " nzp_prm1 integer default 0," +
                " gil      " + sDecimalType + "(14,7) default 0.00, " +  //кол-во жильцов в лс
                " gil_g    " + sDecimalType + "(14,7) default 0.00, " +  //кол-во жильцов в лс без учета вр.выбывших
                " squ      " + sDecimalType + "(14,7) default 0.00, " +  //площадь лс
                " tarif    " + sDecimalType + "(17,7) default 0," +
                " tarif_gkal " + sDecimalType + "(14,7) default 0," +
                " norm_gkal  " + sDecimalType + "(14,7) default 0," +
                " koef_gkal  " + sDecimalType + "(14,7) default 1," +
                " is_poloten integer default 0," +
                " is_neiztrp integer default 0," +
                " tarif_m3   " + sDecimalType + "(14,4) default 0," +
                " nzp_frm_dot integer default 0," +
                " tarif_f    " + sDecimalType + "(14,4) default 0," +
                " nzp_prm_pt  integer default 0," +
                " nzp_frm_typ integer default 0 not null," +
                " nzp_period integer," +
                " dat_s    DATE," +
                " dat_po   DATE," +
                " cntd     integer, " +
                " cntd_mn  integer, " +
                " is_use_knp integer default 0," +
                " is_use_ctr integer default 0" +//Количество временно выбывших
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            #endregion создание таблицы тарифов t_prm
            //1
            #region Тип расчета 1,53 (тариф * расход)

            CalcTypeTarif_1(conn_db, gku, out ret);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            #endregion Тип расчета 1,53 (тариф * расход)

            #region Тип расчета 1638 - простой тариф ЛС/дом/Пост/БД или есть ИПУ другой комплект тарифов!

            CalcTypeTarif_1638(conn_db, gku, out ret);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            #endregion Тип расчета 1638 - простой тариф ЛС/дом/Пост/БД или есть ИПУ другой комплект тарифов!

            //2
            #region Тип Расчета 306 (с учетом параметров лифта -начисление на нижних этажах , заранее подготовлена табл t_rsh_lift)

            CalcLift(conn_db, gku, out ret);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            #endregion Тип Расчета 306 (с учетом параметров лифта -начисление на нижних этажах , заранее подготовлена табл t_rsh_lift)

            //3
            #region Тип расчета 101 & 400 [(просто наем) & (наем от базовой ставки)]

            CalcNaem(conn_db, gku, out ret);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            #endregion Тип расчета 101 & 400 [(просто наем) & (наем от базовой ставки)]

            //4
            #region Тип расчета 1056 (тариф только для приватизированных квартир)

            CalcTypeTarif_1056(conn_db, gku, out ret);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            #endregion Тип расчета 1056 (тариф только для приватизированных квартир)

            #region Тип расчета 1999 (рассрочка)

            CalcRassr(conn_db, gku, out ret);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            #endregion Тип расчета 1999 (рассрочка)

            //5
            #region Тип расчета 2 (чем отличается от 1 надо узнать 335 параметра нет в базе выполняться не будет)

            CalcTypeTarif_2(conn_db, gku, out ret);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            #endregion Тип расчета 2 (чем отличается от 1 надо узнать 335 параметра нет в базе выполняться не будет)

            //6
            #region Тип расчета 26 (домофон с трубкой без трубки , тарифы выбираются в разных местах )

            CalcDomof(conn_db, gku, out ret);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            #endregion Тип расчета 26 (домофон с трубкой без трубки , тарифы выбираются в разных местах )

            //7
            #region Типы расчета 12 & 312 & 412 & 912 (эл/энергия)

            CalcTypeTarif_ElEn(conn_db, gku, out ret);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            #endregion Типы расчета 12 & 312 & 412 & 912 (эл/энергия)

            //8
            #region Тип расчета = 40 & 814 & 440 & 514 & 1140 & 1814 & 1983 - выборка тарифа от ГКал

            CalcTypeTarif_GKal(conn_db, gku, out ret);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            #endregion Тип расчета = 40 & 814 & 440 & 514 & 1140 & 1814 & 1983 - выборка тарифа от ГКал

            // индексы для установки тарифов - нужно для CalcTypeTarif_GKalDop - Расчет тарифа по дополнительным параметрам!
            #region создать индексы t_prm

            ret = ExecSQL(conn_db, " create index ixt_prm on t_prm(nzp_kvar,nzp_supp,nzp_frm,nzp_period,priority) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, sUpdStat + " t_prm ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            #endregion создать индексы t_prm
            //9
            #region  Расчет тарифа по дополнительным параметрам тип расчета = 40,440,1140,1814,1983 - выборка тарифа от ГКал

            CalcTypeTarif_GKalDop(conn_db, gku, out ret);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            #endregion  Расчет тарифа по дополнительным параметрам тип расчета = 40,440,1140,1814,1983 - выборка тарифа от ГКал

            #region Завершающие действия по выборке тарифов

            // предварительная выборка расхода для типа расчета 814 
            // используется при завершающих действиях по выборке тарифов - CalcTarifsEndAction! 
            SelTypeRashod_814_514(conn_db, gku, out ret);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            #region тип тарифа =97 - процент удержания
            CalcTypeTarif_97(conn_db, gku, out ret);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }
            #endregion тип тарифа =97 - процент удержания


            //
            CalcTarifsEndAction(conn_db, gku, out ret);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            #endregion Завершающие действия по выборке тарифов

            #endregion создать сводную таблицу t_prm (тарифы)

            #region Выборка тарифов по приоритетам - создание t_prm_calc

            ret = ExecSQL(conn_db,
                " create temp table t_prm_calc (" +
                " nzp_dom  integer," +
                " nzp_kvar integer," +
                " nzp_supp integer," +
                " nzp_frm  integer," +
                " nzp_prm  integer," +
                " gil      " + sDecimalType + "(14,7) default 0.00, " +  //кол-во жильцов в лс
                " gil_g    " + sDecimalType + "(14,7) default 0.00, " +  //кол-во жильцов в лс
                " squ      " + sDecimalType + "(14,7) default 0.00, " +  //площадь лс
                " tarif    " + sDecimalType + "(17,7) default 0," +
                " tarif_gkal " + sDecimalType + "(14,4) default 0," +
                " norm_gkal  " + sDecimalType + "(14,8) default 0," +
                " koef_gkal  " + sDecimalType + "(14,8) default 1," +
                " is_poloten integer default 0," +
                " is_neiztrp integer default 0," +
                " tarif_m3 " + sDecimalType + "(14,4) default 0," +
                " tarif_f  " + sDecimalType + "(17,7) default 0," +
                " perc     " + sDecimalType + "(14,8) default 1," +
                " is_p1176   integer default 0," +
                " is_p1178   integer default 0," +
                " is_p1195   integer default 0," +
                " is_notdev  integer default 1," + // ИПУ наоборот!
                " nzp_prm_pt integer default 0," +
                " nzp_frm_typ integer default 0 not null," +
                " nzp_period integer," +
                " dat_s    DATE," +
                " dat_po   DATE," +
                " cntd     integer, " +
                " cntd_mn  integer  " +
                " )  " + sUnlogTempTable
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // выборка тарифов по выбранным приоритетам
            ret = ExecSQL(conn_db,
                " insert into t_prm_calc" +
                " (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,gil,gil_g,squ,tarif,tarif_gkal,norm_gkal,koef_gkal," +
                " is_poloten,is_neiztrp,tarif_m3,nzp_frm_typ" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn)" +
                " select p.nzp_dom,p.nzp_kvar,p.nzp_supp,p.nzp_frm,p.nzp_prm,p.gil,p.gil_g,p.squ,p.tarif,p.tarif_gkal,p.norm_gkal,p.koef_gkal," +
                " p.is_poloten,p.is_neiztrp,tarif_m3,p.nzp_frm_typ" +
                " ,p.nzp_period,p.dat_s,p.dat_po,p.cntd,p.cntd_mn" +
                " from t_prm p,t_prm_max m" +
                " Where p.nzp_kvar=m.nzp_kvar and p.nzp_supp=m.nzp_supp and p.nzp_frm=m.nzp_frm and p.nzp_period=m.nzp_period and p.priority=m.priority "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, " create index ixt1_prm_calc on t_prm_calc(nzp_kvar,nzp_frm,nzp_period,nzp_supp) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, " create index ixt2_prm_calc on t_prm_calc(nzp_kvar,nzp_frm,nzp_frm_typ) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, sUpdStat + " t_prm_calc ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            #endregion Выборка тарифов по приоритетам - создание t_prm_calc

            //            
            //          Выборка расходов
            //
            #region Создать сводную таблицу расходов t_rsh (плоский вид)

            #region Создать врем таблицу расходов

            ret = ExecSQL(conn_db,
                " create temp table t_rsh (" +
                " nzp_kvar integer," +
                " nzp_frm  integer," +
                " nzp_prm  integer," +
                " rashod   " + sDecimalType + "(16,7) default 0," +
                " rashod_source   " + sDecimalType + "(16,7) default 0," +
                " up_kf   " + sDecimalType + "(16,7) default 1," +
                " rashod_norm   " + sDecimalType + "(16,7) default 0," +
                " rash_norm_one " + sDecimalType + "(15,7) default 0," +
                " rash_norm_onek " + sDecimalType + "(15,7) default 0," +
                " rashod_g " + sDecimalType + "(16,7) default 0," +
                " set_norm integer  default 0," +
                " rsh_hv   " + sDecimalType + "(16,7) default 0," +
                " rsh_gv   " + sDecimalType + "(16,7) default 0," +
                " rsh1     " + sDecimalType + "(16,7) default 0," +
                " rsh2     " + sDecimalType + "(16,7) default 0," +
                " rsh3     " + sDecimalType + "(16,7) default 0," +
                " rsh2_g   " + sDecimalType + "(16,7) default 0," +
                " rsh_gkal " + sDecimalType + "(16,7) default 0," +     // 253 параметр (норма гкалл на м3) /для отопленияГкалл 723- общпл,отапл 2074 жил площадь 
                " valm     " + sDecimalType + "(16,7) default 0," +
                " valmk    " + sDecimalType + "(16,7) default 0," +
                " dop87    " + sDecimalType + "(16,7) default 0," +
                " dop87k   " + sDecimalType + "(16,7) default 0," +
                " dlt_reval " + sDecimalType + "(16,7) default 0," +
                " dlt_revalk " + sDecimalType + "(16,7) default 0," +
                " gil_smr  " + sDecimalType + "(16,7) default 0," +
                " kf307hv  " + sDecimalType + "(14,7) default 1," +
                " kf307gv  " + sDecimalType + "(14,7) default 1," +
                " kod_info  integer  default 0," +
                " is_device integer  default 0," +
                " is_trunc  integer  default 7," +                    // по умолчанию округление до 7 знаков
                " recalc_ipu    integer  default 0," +
                " nzp_frm_typ   integer default 0 not null," +        // тип тарифа добавил потому что нужно делать связанные с ним апдейты и не вводить новые типы расхода 
                " nzp_frm_typrs integer default 0 not null," +        // тип расхода 
                " is_rsh_cnt    integer default 0 not null," +
                " is_not_sum    integer default 0 not null," +        // если =1, то не суммировать при формировании стека 3, расход по-дням переходит из counters_XX!
                " nzp_period integer," +
                " dat_s    DATE," +
                " dat_po   DATE," +
                " cntd     integer, " +
                " cntd_mn  integer, " +
                " kod_info_gkal9 integer  default 0," +
                " dop87_gkal9 " + sDecimalType + "(14,7) default 0.00," +
                " kf307_gkal9 " + sDecimalType + "(14,7) default 0.00 " +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            #endregion Создать врем таблицу расходов

            #region Тип расхода = 1 (просто расход квартирного параметра)

            CalcTypeRashod_1(conn_db, gku, out ret);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            #endregion Тип расхода = 1 (просто расход квартирного параметра)


            #region Тип расхода = 2201 (0 расход)

            CalcTypeRashod_2201(conn_db, gku, out ret);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            #endregion Тип расхода = 2201 (0 расход)

            #region Тип расхода = 2202 (перемножение нескольких параметров (до 3-х, можно по ЛС и по Дому))

            CalcTypeRashod_2202(conn_db, gku, out ret);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            #endregion Тип расхода = 2202 (перемножение нескольких параметров (до 3-х, можно по ЛС и по Дому))

            #region Тип расхода = 2203 (расход квартирного параметра при наличии показания ОДПУ в текущем месяце)
            CalcTypeRashod_2203(conn_db, gku, out ret);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }
            #endregion


            #region Тип расхода = 101 (расход квартирного параметра с вычитанием другого квартирного параметра)

            CalcTypeRashod_101(conn_db, gku, out ret);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            #endregion Тип расхода = 101 (расход квартирного параметра с вычитанием другого квартирного параметра)

            #region Тип расхода = 2,4 (тариф на квартиру)

            CalcTypeRashod_2_4(conn_db, gku, out ret);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            #endregion Тип расхода = 2,4 (тариф на квартиру)

            #region Тип расхода = 11,1814 (с учетом индивидуального расхода коммунальных квартир)

            CalcTypeRashod_11_1814(conn_db, gku, out ret);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            #endregion Тип расхода = 11,1814 (с учетом индивидуального расхода коммунальных квартир)

            #region Типы расхода = 3,7,106,300,512 (количество проживающих)

            CalcTypeRashod_3_7_106_300_512(conn_db, gku, out ret);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            #endregion Типы расхода = 3,7,106,300,512 (количество проживающих)

            #region Тип расхода = 509 - вода для домашних животных по видас скота

            CalcTypeRashod_509(conn_db, gku, out ret);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            #endregion Тип расхода = 509 - вода для домашних животных по видас скота

            #region Тип расхода = 610 - вода для транспорта по видам транспорта

            CalcTypeRashod_610(conn_db, gku, out ret);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            #endregion Тип расхода = 610 - вода для транспорта по видам транспорта

            #region Тип расхода = 5

            CalcTypeRashod_5(conn_db, gku, out ret);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            #endregion Тип расхода = 5

            #region Тип расхода = 39 -КАН

            CalcTypeRashod_39_391(conn_db, gku, out ret);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            #endregion Тип расхода = 39 -КАН

            #region Тип расхода 390 -КАН =ХВ+ГВ

            CalcTypeRashod_390(conn_db, gku, out ret);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            #endregion Тип расхода 390 -КАН =ХВ+ГВ

            #region Тип расхода 339 -> КАН = ХВ

            CalcTypeRashod_339(conn_db, gku, out ret);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            #endregion Тип расхода 339 -> КАН = ХВ

            #region тип расхода = 814,514  [(Отопление (от ГКал-СН по отопительной площади)), (отопление Гкал)]

            CalcTypeRashod_814_514(conn_db, gku, out ret);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            #endregion тип расхода = 814,514  [(Отопление (от ГКал-СН по отопительной площади)), (отопление Гкал)]


            #region тип расхода =97 - процент удержания
            CalcTypeRashod_97(conn_db, gku, out ret);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }
            #endregion тип расхода =97 - процент удержания

            #region индексы для установки расходов
            //
            // индексы для установки расходов
            ret = ExecSQL(conn_db, " create index ixt_rsh1 on t_rsh(nzp_kvar,nzp_frm,nzp_period) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, " create index ixt_rsh2 on t_rsh(nzp_frm_typrs) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, " create index ixt_rsh3 on t_rsh(nzp_frm_typ,is_device) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, sUpdStat + " t_rsh ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            #endregion индексы для установки расходов

            //
            //          Завершающие действия по выборке расходов
            //

            #region Установить расход по норме

            // установить расход по норме для коммунальных услуг
            ExecSQL(conn_db, " Drop table ttt_ans_norm0 ", false);
            ret = ExecSQL(conn_db,
                " create temp table ttt_ans_norm0 (" +
                " nzp_kvar integer," +
                " nzp_serv integer," +
                " val1     " + sDecimalType + "(14,7) default 0.00 " +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            // услуги на ОДН с тарифом основной услуги
            ret = ExecSQL(conn_db,
                " insert into ttt_ans_norm0 (nzp_kvar,nzp_serv,val1)" +
                " select s.nzp_kvar,s.nzp_serv,max(case when s.val1>0 then s.val1 else 0 end) val1" +
                " from " + gku.counters_xx + " s, t_selkvar t " +
                " Where s.nzp_type=3 and s.stek=30 and t.nzp_kvar=s.nzp_kvar " + d_dat_charge +
                " group by 1,2 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            ret = ExecSQL(conn_db, " create index ixttt_ans_norm0 on ttt_ans_norm0(nzp_kvar,nzp_serv) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
            ExecSQL(conn_db, sUpdStat + " ttt_ans_norm0 ", true);

            ExecSQL(conn_db, " Drop table ttt_ans_norm1 ", false);
            ret = ExecSQL(conn_db,
                " create temp table ttt_ans_norm1 (" +
                " nzp_kvar integer," +
                " nzp_frm  integer," +
                " val1     " + sDecimalType + "(14,7) default 0.00 " +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            // услуги на ОДН с тарифом основной услуги
            ret = ExecSQL(conn_db,
                 " insert into ttt_ans_norm1 (nzp_kvar,nzp_frm,val1)" +
                 " select t.nzp_kvar,t.nzp_frm,max(s.val1) val1" +
                 " from t_tarifs t,ttt_ans_norm0 s " +
                 " Where t.nzp_kvar=s.nzp_kvar and (case when t.nzp_serv=14 then 9 else t.nzp_serv end)=s.nzp_serv " +
                 " group by 1,2 "
                 , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            ret = ExecSQL(conn_db, " create index ixttt_ans_norm1 on ttt_ans_norm1(nzp_kvar,nzp_frm) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
            ExecSQL(conn_db, sUpdStat + " ttt_ans_norm1 ", true);

            ret = ExecSQL(conn_db,
                " update t_rsh set set_norm=1,rashod_norm=" +
                " (select max(t.val1) from ttt_ans_norm1 t " +
                 " where t_rsh.nzp_kvar=t.nzp_kvar and t_rsh.nzp_frm=t.nzp_frm)" +
                " where nzp_frm_typrs<>339" +
                " and exists (select 1 from ttt_ans_norm1 t " +
                 " where t_rsh.nzp_kvar=t.nzp_kvar and t_rsh.nzp_frm=t.nzp_frm)"
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            ret = ExecSQL(conn_db, " Drop table ttt_ans_norm0 ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            ret = ExecSQL(conn_db, " Drop table ttt_ans_norm1 ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            // установить расход по норме для услуг по жильцам
            ret = ExecSQL(conn_db, " update t_rsh set set_norm=1,rash_norm_one=1,rashod_norm=rashod where set_norm=0 and nzp_frm_typrs in (3,7,106,300,512) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            // установить расход по норме для услуг по жильцам
            ret = ExecSQL(conn_db, " update t_rsh set set_norm=1,rashod_norm=rashod where nzp_frm_typrs=339 ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            // установить расход по норме для услуг по квартире (все неустановленные формулы кроме площадных)
            ret = ExecSQL(conn_db,
                " update t_rsh set set_norm=1,rash_norm_one=1,rashod_norm=1 " +
                " where set_norm=0 and exists (select 1 from " + gku.paramcalc.kernel_alias + "formuls f where t_rsh.nzp_frm=f.nzp_frm and f.nzp_measure<>1)"
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            #endregion Установить расход по норме

            #region Если расход еще не определился взять -Нормативы по площади

            // установить расход по норме для услуг по площади
            #region Определить количество жильцов -округленно для выборки из таблицы нормативов
            ret = ExecSQL(conn_db,
                " create temp table t_kg_m2 (" +
                " nzp_kvar integer," +
                " kg   " + sDecimalType + "(11,7) default 0," +
                " itbl integer default 0," +
                " ikg  integer default 0," +
                " iikg integer default 0" +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            ret = ExecSQL(conn_db,
                " Insert into t_kg_m2 (nzp_kvar,kg)" +
                " Select nzp_kvar,max(kg+kvp) " +
                " From t_rsh_kolgil " +
                " Group by 1 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            ret = ExecSQL(conn_db, " create index ixt_kg_m2 on t_kg_m2 (nzp_kvar) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
            ret = ExecSQL(conn_db, sUpdStat + " t_kg_m2 ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " update t_kg_m2 set ikg=" +
                " (case when Round(kg)-Trunc(kg)>0.5" +
                 " then Round(kg)" +
                 " else (case when Round(kg)-Trunc(kg)<0.001 then Trunc(kg) else Trunc(kg)+1 end)" +
                 " end)" +
                " where 1=1 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
            ret = ExecSQL(conn_db,
                " update t_kg_m2 set iikg= (case when ikg>5 then 5 else ikg end) "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            #endregion Определить количество жильцов -округленно для выборки из таблицы нормативов

            // для 814 формулы расход по норме - площадь!
            ret = ExecSQL(conn_db,
                " update t_rsh set set_norm=0 where nzp_frm_typrs=814 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // выставить номер таблицы норматива по площади
            ret = ExecSQL(conn_db,
                " update t_kg_m2" +
                " set itbl=(select max(p.val_prm)" + sConvToInt + " from " + paramcalc.data_alias + "prm_13 p" +
                          " where p.nzp_prm=171 and p.is_actual<>100 and p.dat_s<" + gku.dat_po + " and p.dat_po>=" + gku.dat_s + ")" +
                " where 1=1 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, " create index ix0t_kg_m2 on t_kg_m2 (nzp_kvar) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }
            ret = ExecSQL(conn_db, " create index ix1t_kg_m2 on t_kg_m2 (iikg) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }
            ret = ExecSQL(conn_db, " create index ix2t_kg_m2 on t_kg_m2 (itbl) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }
            ret = ExecSQL(conn_db, sUpdStat + " t_kg_m2 ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " update t_rsh " +
                " set set_norm=1, " +
                 " rash_norm_one=" +
                 " (select max((r.value" + sConvToNum + ")) from " + paramcalc.kernel_alias + "res_values r,t_kg_m2 g" +
                 " where g.nzp_kvar=t_rsh.nzp_kvar and r.nzp_y=g.iikg and r.nzp_x=2 and r.nzp_res =  g.itbl )," +
                 " rashod_norm=" +
                 " (select max((r.value" + sConvToNum + ")*g.ikg) from " + paramcalc.kernel_alias + "res_values r,t_kg_m2 g" +
                 " where g.nzp_kvar=t_rsh.nzp_kvar and r.nzp_y=g.iikg and r.nzp_x=2 and r.nzp_res = g.itbl ) " +
                " where set_norm=0 and exists " +
                 " (select 1 from " + paramcalc.kernel_alias + "res_values r,t_kg_m2 g" +
                 " where g.nzp_kvar=t_rsh.nzp_kvar and r.nzp_y=g.iikg and r.nzp_x=2 and r.nzp_res = g.itbl ) "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            #endregion Если расход еще не определился взять -Нормативы по площади

            #endregion Создать сводную таблицу расходов t_rsh (плоский вид)

            //
            //          Запись результатов расчета в БД
            //

            #region Добавление услуг ОДН - Дополнение t_prm_calc с учетом расходов t_rsh

            // услуги на ОДН

            // выбрать параметры л/с для nzp_prm_tarif_bd на дату расчета
            ExecSQL(conn_db, " Drop table ttt_aid_virt0 ", false);
            ret = ExecSQL(conn_db,
                " create temp table ttt_aid_virt0 (" +
                // " create  table ttt_aid_virt0 (" +
                " nzp_dom  integer," +
                " nzp_kvar integer," +
                " nzp_supp integer," +
                " nzp_frm  integer," +
                " nzp_prm  integer," +
                " gil      " + sDecimalType + "(14,7) default 0.00, " +  //кол-во жильцов в лс
                " gil_g    " + sDecimalType + "(14,7) default 0.00, " +  //кол-во жильцов в лс
                " squ      " + sDecimalType + "(14,7) default 0.00, " +  //площадь лс
                " tarif    " + sDecimalType + "(17,7) default 0," +
                " tarif_gkal " + sDecimalType + "(14,4) default 0," +
                " norm_gkal  " + sDecimalType + "(14,8) default 0," +
                " koef_gkal  " + sDecimalType + "(14,8) default 1," +
                " is_poloten integer default 0," +
                " is_neiztrp integer default 0," +
                " tarif_m3   " + sDecimalType + "(14,4) default 0," +
                " nzp_frm_typ integer default 0 not null," +
                " nzp_period integer," +
                " dat_s    DATE," +
                " dat_po   DATE," +
                " cntd     integer, " +
                " cntd_mn  integer  " +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // услуги на ОДН с тарифом основной услуги с nzp_frm_typ=999! их точно нет - этот тип расчета тарифа НЕ обрабатывается!
            ret = ExecSQL(conn_db,
                 " insert into ttt_aid_virt0" +
                 " (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,gil,gil_g,squ,tarif,tarif_gkal,norm_gkal,koef_gkal," +
                 " is_poloten,is_neiztrp,tarif_m3,nzp_frm_typ" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn)" +
                 " select" +
                 " p.nzp_dom,p.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,p.gil,p.gil_g,p.squ,p.tarif,p.tarif_gkal,p.norm_gkal,p.koef_gkal," +
                 " p.is_poloten,p.is_neiztrp,p.tarif_m3,p.nzp_frm_typ" +
                " ,p.nzp_period,max(p.dat_s),max(p.dat_po),max(p.cntd),max(p.cntd_mn)" +
                 " from t_prm_calc p,t_tarifs r,t_tarifs t," + gku.paramcalc.kernel_alias + "serv_odn n" +
                 " Where r.nzp_kvar=p.nzp_kvar and r.nzp_frm=p.nzp_frm     and r.nzp_serv=n.nzp_serv_link" +
                 "   and t.nzp_kvar=p.nzp_kvar and t.nzp_frm=n.nzp_frm_eqv and t.nzp_serv=n.nzp_serv and p.nzp_frm_typ<>1140 " +
                 " group by 1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17 "
                 , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " insert into ttt_aid_virt0" +
                " (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,gil,gil_g,squ,tarif,tarif_gkal,norm_gkal,koef_gkal," +
                " is_poloten,is_neiztrp,tarif_m3,nzp_frm_typ" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn)" +
                " select" +
                " p.nzp_dom,p.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,p.gil,p.gil_g,p.squ,p.tarif,p.tarif_gkal,p.norm_gkal,p.koef_gkal," +
                " p.is_poloten,p.is_neiztrp,p.tarif_m3,p.nzp_frm_typ" +
                " ,p.nzp_period,max(p.dat_s),max(p.dat_po),max(p.cntd),max(p.cntd_mn)" +
                " from t_prm_calc p,t_tarifs r,t_tarifs t," + gku.paramcalc.kernel_alias + "serv_odn n" +
                " Where r.nzp_kvar=p.nzp_kvar and r.nzp_frm=p.nzp_frm   and r.nzp_serv=n.nzp_serv_link" +
                "   and t.nzp_kvar=p.nzp_kvar and t.nzp_frm=1983 and t.nzp_serv=n.nzp_serv and p.nzp_frm_typ=1140 " +
                " and not exists (select 1 from t_rsh v where v.nzp_kvar=t.nzp_kvar and v.nzp_frm=t.nzp_frm and v.nzp_frm_typ=999) " +
                " group by 1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ExecSQL(conn_db, sUpdStat + " ttt_aid_virt0 ", true);

            ret = ExecSQL(conn_db,
                 " insert into t_prm_calc" +
                 " (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,gil,gil_g,squ,tarif,tarif_gkal,norm_gkal,koef_gkal," +
                 " is_poloten,is_neiztrp,tarif_m3,nzp_frm_typ" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn)" +
                 " select" +
                 " p.nzp_dom,p.nzp_kvar,p.nzp_supp,p.nzp_frm,p.nzp_prm,p.gil,p.gil_g,p.squ,p.tarif,p.tarif_gkal,p.norm_gkal,p.koef_gkal," +
                 " p.is_poloten,p.is_neiztrp,p.tarif_m3,p.nzp_frm_typ" +
                " ,p.nzp_period,p.dat_s,p.dat_po,p.cntd,p.cntd_mn" +
                 " from ttt_aid_virt0 p"
                 , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, sUpdStat + " t_prm_calc ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ExecSQL(conn_db, " Drop table ttt_aid_virt0 ", false);

            #endregion Добавление услуг ОДН - Дополнение t_prm_calc с учетом расходов t_rsh

            #region Завершающие действия по формуле 440

            // тип расхода = 440 - Завершающие действия
            // записать коэф-т перевода в ГКал
            ret = ExecSQL(conn_db,
                " update t_rsh set rsh1 = rashod, rsh2 = " +
                        " (select max(koef_gkal*norm_gkal) from t_prm_calc r" +
                        " where t_rsh.nzp_kvar=r.nzp_kvar and t_rsh.nzp_frm=r.nzp_frm and t_rsh.nzp_period=r.nzp_period and r.nzp_frm_typ=440) " +
                " where nzp_frm_typrs=440 " +
                  " and 0<(select count(*) from t_prm_calc r" +
                        " where t_rsh.nzp_kvar=r.nzp_kvar and t_rsh.nzp_frm=r.nzp_frm and t_rsh.nzp_period=r.nzp_period and r.nzp_frm_typ=440) "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // расчитать окончательный расход в Гкал ГВС = расход в м3 * расход в ГКал на нагрев 1 м3
            ret = ExecSQL(conn_db,
                " update t_rsh set rashod = rsh1 * rsh2 where nzp_frm_typrs=440 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            #endregion Завершающие действия по формуле 440

            #region Взять количество знаков округления
            //  DataTable rta=  ViewTbl(conn_db, " select * from t_rsh ");

            // параметр округления на базу - 2я очередь (ставим первым!)
            ret = ExecSQL(conn_db,
                " update t_rsh set is_trunc =" +
                  " (select max(" + sNvlWord + "(p.val_prm,'0')" + sConvToNum + ") " +
                  " from " + paramcalc.data_alias + "prm_10 p" +
                  " where p.nzp_prm=1232 and p.is_actual<>100 and p.dat_s<" + gku.dat_po + " and p.dat_po>=" + gku.dat_s + " )" +
                " where t_rsh.nzp_frm_typ in (1140,1814,1983) " +
                " and 0<(select count(*) from " + paramcalc.data_alias + "prm_10 p1" +
                "     where p1.nzp_prm=1232 and p1.is_actual<>100 and p1.dat_s<" + gku.dat_po + " and p1.dat_po>=" + gku.dat_s + ") "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // параметр округления для УК - 1я очередь (ставим вторым!)
            ret = ExecSQL(conn_db,
                " update t_rsh set is_trunc =" +
                " (select max(" + sNvlWord + "(p.val_prm,'0')" + sConvToNum + ") " +
                " from " + paramcalc.data_alias + "prm_7 p, t_selkvar k" +
                " where t_rsh.nzp_kvar=k.nzp_kvar and p.nzp=k.nzp_area and p.nzp_prm=1233" +
                " and p.is_actual<>100 and p.dat_s<" + gku.dat_po + " and p.dat_po>=" + gku.dat_s + " )" +
                " where t_rsh.nzp_frm_typ in (1140,1814,1983) and 0<" +
                " (select count(*) " +
                " from " + paramcalc.data_alias + "prm_7 p, t_selkvar k" +
                " where t_rsh.nzp_kvar=k.nzp_kvar and p.nzp=k.nzp_area and p.nzp_prm=1233" +
                " and p.is_actual<>100 and p.dat_s<" + gku.dat_po + " and p.dat_po>=" + gku.dat_s +
                " ) "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " update t_rsh set is_trunc = 7 " +
                " where t_rsh.nzp_frm_typ in (1140,1814,1983) and is_trunc <= 0 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            string sss = " select val_prm" + sConvToInt + " cnt " +
                         " from " + gku.paramcalc.data_alias + "prm_10 p " +
                         " where p.nzp_prm=1279  " +
                         " and p.is_actual<>100 and p.dat_s<" + gku.dat_po + " and p.dat_po>=" + gku.dat_s + " ";
            ret = ExecRead(conn_db, out reader, sss, true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            int isGvsTrunc = 2;
            if (reader.Read())
            {
                isGvsTrunc = (Convert.ToInt32(reader["cnt"]));
            }
            reader.Close();

            #endregion Взять количество знаков округления

            #region Завершающие действия по формуле 1140

            #region параметр норма гКалл на 1 м3

            // параметр норма гКалл на 1 м3
            ret = ExecSQL(conn_db,
             " update t_rsh set rsh_gkal =" +
             "(select max(a.norm_gkal) from t_prm_calc a where a.nzp_kvar=t_rsh.nzp_kvar and t_rsh.nzp_period=a.nzp_period and a.nzp_frm_typ=1140)" +
             " where nzp_frm_typ=1140  "
             , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
             " update t_rsh set rsh_gkal =" +
             "(select max(a.norm_gkal) from t_prm_calc a where a.nzp_kvar=t_rsh.nzp_kvar and t_rsh.nzp_period=a.nzp_period and a.nzp_frm_typ=1140)" +
             " where nzp_frm_typ=1983  "
             , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // rta = ViewTbl(conn_db, " select * from t_rsh ");

            #endregion параметр норма гКалл на 1 м3

            #region записать коэф-т перевода m3 ГВС в ГКал

            // записать коэф-т перевода в ГКал
            ret = ExecSQL(conn_db,
                " update t_rsh set  rsh1 = rashod, rsh2 = " +
                        " (select max(koef_gkal*norm_gkal) from t_prm_calc r" +
                        " where t_rsh.nzp_kvar=r.nzp_kvar and t_rsh.nzp_frm=r.nzp_frm and t_rsh.nzp_period=r.nzp_period and r.nzp_frm_typ=1140) " +
                " where nzp_frm_typrs=5 and nzp_frm_typ=1140 " +
                  " and 0<(select count(*) from t_prm_calc r" +
                        " where t_rsh.nzp_kvar=r.nzp_kvar and t_rsh.nzp_frm=r.nzp_frm and t_rsh.nzp_period=r.nzp_period and r.nzp_frm_typ=1140) "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " update t_rsh set rsh1=rashod, rsh2=rsh_gkal where nzp_frm_typrs=5 and nzp_frm_typ=1983  "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // rta = ViewTbl(conn_db, " select * from t_rsh ");

            #endregion записать коэф-т перевода m3 ГВС в ГКал

            #region расчитать окончательный расход в Гкал ГВС = расход в м3 * расход в ГКал на нагрев 1 м3

            // сохранить расходы в куб.м для ГВС - nzp_frm_typ in (1140,1983)
            ret = ExecSQL(conn_db,
                    " update t_rsh set valmk=valm, dop87k=dop87, dlt_revalk=dlt_reval, rash_norm_onek=rash_norm_one" +
                    " where nzp_frm_typrs=5 and nzp_frm_typ in (1140,1983) "
                    , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // сначала для услуги ОДН - nzp_frm_typ=1983
            ret = ExecSQL(conn_db,
                    " update t_rsh set rashod = rsh1 * rsh2,rashod_norm=rashod_norm * rsh2" +
                    " , valm=valm * rsh2, dop87=dop87 * rsh2, dlt_reval=dlt_reval * rsh2" +
                    " , rashod_source=rashod_source * rsh2" +
                    " , rash_norm_one=rash_norm_one * rsh2" +
                    " where nzp_frm_typrs=5 and nzp_frm_typ=1983 "
                    , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " update t_rsh set rashod = round(rashod,is_trunc),rashod_norm=round(rashod_norm,is_trunc)" +
                " , valm=round(valm,is_trunc), dop87=round(dop87,is_trunc), dlt_reval=round(dlt_reval,is_trunc)" +
                " , rash_norm_one=round(rash_norm_one,is_trunc)" +
                " where nzp_frm_typrs=5 and nzp_frm_typ=1983 and is_trunc between 1 and 6 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }
            // rta = ViewTbl(conn_db, " select * from t_rsh ");
            // для основной услуги услуги ОДН - nzp_frm_typ=1140
            if (Points.IsSmr)
            {

                // для Самары округлить до 2х - если работаем по нормативу - решение от 28.08.2013 -> gil_smr>0! это кол-во жильцов!
                ret = ExecSQL(conn_db,
                    " update t_rsh set gil_smr = case when rash_norm_one>0 and is_device=0 then valm/rash_norm_one else 0 end " +
                    " where nzp_frm_typrs=5 and nzp_frm_typ=1140 and rsh2>0 "
                    , true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }
            }
            ret = ExecSQL(conn_db,
                    " update t_rsh set rashod = rsh1 * rsh2,rashod_norm=rashod_norm * rsh2" +
                    " , valm=valm * rsh2, dop87=dop87 * rsh2, dlt_reval=dlt_reval * rsh2" +
                    " , rashod_source=rashod_source * rsh2" +
                    " , rash_norm_one=rash_norm_one * rsh2" +
                    " where nzp_frm_typrs=5 and nzp_frm_typ=1140 and abs(gil_smr)<=0.000001 "
                    , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // rta = ViewTbl(conn_db, " select * from t_rsh ");
            ret = ExecSQL(conn_db,
                    " update t_rsh set " +
                    "   rashod      = round(rsh1 * rsh2," + isGvsTrunc + ") " +
                    " , rash_norm_one=round(rash_norm_one * rsh2," + isGvsTrunc + ")" +
                    " , rashod_norm = round(rash_norm_one * rsh2," + isGvsTrunc + ") " +
                    " , valm        = round(valm * rsh2," + isGvsTrunc + ") " +
                    " , dop87=dop87 * rsh2, dlt_reval=dlt_reval * rsh2 " +
                    " , rashod_source=rashod_source * rsh2 " +
                    " where nzp_frm_typrs=5 and nzp_frm_typ=1140 and abs(gil_smr)>0.000001 "
                    , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            //    rta = ViewTbl(conn_db, " select * from t_rsh ");
            ret = ExecSQL(conn_db,
                " update t_rsh set " +
                "   rashod_norm=round(rashod_norm,is_trunc)" +
                " , valm=round(valm,is_trunc), dop87=round(dop87,is_trunc), dlt_reval=round(dlt_reval,is_trunc)" +
                " , rash_norm_one=round(rash_norm_one,is_trunc)" +
                " where nzp_frm_typrs=5 and nzp_frm_typ=1140 and is_trunc between 1 and 6 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " update t_rsh set " +

                " rashod = " +
                " case when abs(gil_smr) > 0.000001" + // есть округление норматива ГКал по жильцам в Самаре
                " then case when dop87 >= 0" +
                "      then rashod_norm" +
                "      else (case when rashod_norm + dop87 > 0" +
                "            then rashod_norm + dop87" +
                "            else 0" +
                "            end) " +
                "      end " +
                " else case when dop87 >= 0" +
                "      then valm" +
                "      else (case when valm + dop87 > 0" +
                "            then valm + dop87" +
                "            else 0" +
                "            end) " +
                "      end " +
                " end " +

                " where nzp_frm_typrs=5 and nzp_frm_typ=1140 and is_trunc between 1 and 6 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // rta = ViewTbl(conn_db, " select * from t_rsh ");

            #region взять готовый окончательный расход в Гкал ГВС по Пост 354
            // ... учет расчета по ГКал по Пост 354 ... уже выбран в kod_info_gkal9,dop87_gkal9,kf307_gkal9

            // для услуги ОДН - nzp_frm_typ=1983 если ОДН в ГКал по Пост354 > 0 / kod_info_gkal9 = 21 - учесть расход в ГКал
            ret = ExecSQL(conn_db,
                    " update t_rsh set rashod = dop87_gkal9,rashod_norm=rashod_norm * rsh2" +
                    " , valm=dop87_gkal9, dop87=0, dlt_reval=dlt_reval * rsh2" +
                    " , rash_norm_one=rash_norm_one * rsh2, nzp_prm = kod_info_gkal9 " +
                    " where nzp_frm_typrs=5 and nzp_frm_typ=1983 and kod_info_gkal9 = 21 "
                    , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // для основной услуги - nzp_frm_typ=1140 если ОДН в ГКал по Пост354 > 0 / kod_info_gkal9 = 21 - учесть расход ОДН в ГКал
            ret = ExecSQL(conn_db,
                    " update t_rsh set dop87 = (case when dop87_gkal9 + rashod >= 0 then dop87_gkal9 else (-1) * rashod end) " +
                    " where nzp_frm_typrs=5 and nzp_frm_typ=1140 and kod_info_gkal9 in (21,31) "
                    , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ExecSQL(conn_db, " Drop table ttt_aid_virt0 ", false);
            ret = ExecSQL(conn_db,
                " create temp table ttt_aid_virt0 (" +
                " nzp_kvar integer," +
                " rsh_gkal  " + sDecimalType + "(14,8) default 0 " +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // услуги на ОДН с тарифом основной услуги с nzp_frm_typ=999! их точно нет - этот тип расчета тарифа НЕ обрабатывается!
            ret = ExecSQL(conn_db,
                " insert into ttt_aid_virt0 (nzp_kvar,rsh_gkal)" +
                " select p.nzp_kvar,max(p.rashod)" +
                " from t_rsh p" +
                " where nzp_frm_typrs=5 and nzp_frm_typ=1140 " +
                " group by 1 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ExecSQL(conn_db, " Create index ix_ttt_aid_virt0 on ttt_aid_virt0 (nzp_kvar) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_aid_virt0 ", true);

            // для услуги ОДН - nzp_frm_typ=1983 если ОДН в ГКал по Пост354 < 0 / kod_info_gkal9 = 31 - обрезать расход в ГКал
            ret = ExecSQL(conn_db,
                    " update t_rsh " +
                    " set rsh3 = b.rsh_gkal " +
                    " from ttt_aid_virt0 b " +
                    " where t_rsh.nzp_kvar=b.nzp_kvar " +
                    " and t_rsh.nzp_frm_typrs=5 and t_rsh.nzp_frm_typ=1983 and t_rsh.kod_info_gkal9 in (21,31) "
                    , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                    " update t_rsh set" +
                    " rashod = case when dop87_gkal9 + rsh3 >= 0 then dop87_gkal9 else (-1) * rsh3 end," +
                    " rashod_norm=rashod_norm * rsh2, " +
                    " valm = case when dop87_gkal9 + rsh3 >= 0 then dop87_gkal9 else (-1) * rsh3 end," +
                    " dop87=0, dlt_reval=dlt_reval * rsh2, rash_norm_one=rash_norm_one * rsh2, nzp_prm = kod_info_gkal9 " +
                    " where nzp_frm_typrs=5 and nzp_frm_typ=1983 and kod_info_gkal9 = 31 "
                    , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ExecSQL(conn_db, " Drop table ttt_aid_virt0 ", false);

            #endregion взять готовый окончательный расход в Гкал ГВС по Пост 354


            #endregion расчитать окончательный расход в Гкал ГВС = расход в м3 * расход в ГКал на нагрев 1 м3

            #endregion Завершающие действия по формуле 1140

            #region Учет неотапливаемой площади для отопления

            ret = ExecSQL(conn_db,
                " create temp table t_rsh8 (" +
                " nzp_kvar integer," +
                " nzp_frm  integer," +
                " nzp_prm  integer default 0," +
                " nzp_period integer default 0," +
                " rsh3     " + sDecimalType + "(16,7) default 0" +
                " )  " + sUnlogTempTable
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // выбрать неотапливаемую площадь для отопления - nzp_serv=8 and nzp_prm=2010
            ret = ExecSQL(conn_db,
                " insert into t_rsh8 (nzp_kvar,nzp_frm,rsh3) " +
                " select t.nzp_kvar,t.nzp_frm,max(p.val" + sConvToNum + ") " +
                " from t_tarifs t," + gku.paramcalc.kernel_alias + "formuls f,t_par1 p " +
                " where t.nzp_kvar=p.nzp and t.nzp_frm=f.nzp_frm and t.nzp_serv=8 and p.nzp_prm=2010 and f.nzp_measure in (1,4) " +
                " group by 1,2 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, " create index ixt_rsh8 on t_rsh8(nzp_kvar,nzp_frm) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, sUpdStat + " t_rsh8 ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " update t_rsh set rsh1 = rashod, rsh3 = (select r.rsh3 from t_rsh8 r where r.nzp_kvar=t_rsh.nzp_kvar and r.nzp_frm=t_rsh.nzp_frm)" +
                " where exists (select 1 from t_rsh8 r where r.nzp_kvar=t_rsh.nzp_kvar and r.nzp_frm=t_rsh.nzp_frm) "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " update t_rsh set rashod = case when rsh1-rsh3>0 then rsh1-rsh3 else 0 end " +
                " where exists (select 1 from t_rsh8 r where r.nzp_kvar=t_rsh.nzp_kvar and r.nzp_frm=t_rsh.nzp_frm) "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            #endregion Учет неотапливаемой площади для отопления

            #region Завершающие действия по формуле 312

            // тип расчета = 312 
            // -- для ЛС по нормативу переопределить тарифы в случае, если ЛС по основному ЭЭ рассчитан по норме, 
            // -- а начислений по ИПУ по связанным услугам (210,410) нет

            ret = ExecSQL(conn_db, " TRUNCATE t_rsh8 ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // тариф на ЛС с эл/плитой на л/с - f.nzp_prm_tarif_ls
            ret = ExecSQL(conn_db,
                " insert into t_rsh8 (nzp_kvar,nzp_frm,nzp_prm,nzp_period,rsh3)" +
                " select t.nzp_kvar,t.nzp_frm,p.nzp_prm,t.nzp_period,max(p.val" + sConvToNum + ")" +
                " from t_rsh t," + gku.paramcalc.kernel_alias + "formuls_opis f,t_par1 p" +
                " where t.nzp_frm=f.nzp_frm and t.nzp_kvar=p.nzp and p.nzp_prm=f.nzp_prm_tarif_ls and t.nzp_frm_typ=312 and t.is_device=0" +
                " and p.dat_s <= t.dat_po and p.dat_po >= t.dat_s " +
                // наличие эл/плиты на л/с - f.nzp_prm_tarif_bd
                "  and exists (select 1 from t_par26f p1 where p1.nzp=t.nzp_kvar and p1.nzp_prm=f.nzp_prm_tarif_bd)" +
                " group by 1,2,3,4 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }
            // тариф на ЛС без эл/плиты на л/с - f.nzp_prm_tarif_lsp
            ret = ExecSQL(conn_db,
                " insert into t_rsh8 (nzp_kvar,nzp_frm,nzp_prm,nzp_period,rsh3)" +
                " select t.nzp_kvar,t.nzp_frm,p.nzp_prm,t.nzp_period,max(p.val" + sConvToNum + ")" +
                " from t_rsh t," + gku.paramcalc.kernel_alias + "formuls_opis f,t_par1 p" +
                " where t.nzp_frm=f.nzp_frm and t.nzp_kvar=p.nzp and p.nzp_prm=f.nzp_prm_tarif_lsp and t.nzp_frm_typ=312 and t.is_device=0" +
                " and p.dat_s <= t.dat_po and p.dat_po >= t.dat_s " +
                // осутствие эл/плиты на л/с - f.nzp_prm_tarif_bd
                "  and not exists (select 1 from t_par26f p1 where p1.nzp=t.nzp_kvar and p1.nzp_prm=f.nzp_prm_tarif_bd)" +
                " group by 1,2,3,4 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, " create index ixt1_rsh8 on t_rsh8(nzp_kvar,nzp_frm,nzp_period) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, sUpdStat + " t_rsh8 ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ExecSQL(conn_db, " Drop table t_rsh_ee ", false);
            ret = ExecSQL(conn_db, 
                " create temp table t_rsh_ee ( nzp_kvar integer, nzp_period integer ) " + sUnlogTempTable
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // выбрать nzp_serv=(210,410) с ИПУ
            ret = ExecSQL(conn_db,
                " insert into t_rsh_ee (nzp_kvar, nzp_period) " +
                " select distinct a.nzp_kvar, a.nzp_period " +
                " from t_tarifs t, t_rsh a, t_rsh8 b " +
                " where t.nzp_kvar=a.nzp_kvar and t.nzp_frm=a.nzp_frm and a.nzp_kvar=b.nzp_kvar and t.nzp_serv in (210,410) and a.is_device in (1,9) "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, " create index ixt_rsh_ee on t_rsh_ee(nzp_kvar, nzp_period) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, sUpdStat + " t_rsh_ee ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " update t_prm_calc t " +
                " set tarif = r.rsh3, nzp_prm = r.nzp_prm " +
                " from t_rsh8 r " +
                " where r.nzp_kvar=t.nzp_kvar and r.nzp_frm=t.nzp_frm and r.nzp_period=t.nzp_period " +
                " and not exists (select 1 from t_rsh_ee e where e.nzp_kvar=r.nzp_kvar and e.nzp_period=r.nzp_period) "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ExecSQL(conn_db, " Drop table t_rsh8 ", false);
            ExecSQL(conn_db, " Drop table t_rsh_ee ", false);

            #endregion Завершающие действия по формуле 312

            #region Завершающие действия по формуле 1814
            // тип расхода = 1814 - Завершающие действия обнаружил что есть понятие неотапливаемой площади в квартире , это  что то новенькое , поэтому перенес 
            // завершающие действия ниже
            #region Признак округления для формулы 1814 выставлен ранее в 1140
            // сделано в завершающих действиях по типу 1440   (признаки округления одинаковы )         
            #endregion  Признак округления для формулы 1814 выставлен в 1140

            #region тип расхода = 1814 - параметр норма Отопления гКалл на 1 м2 для каждого дома

            // параметр норма гКалл на 1 м2 для 
            ret = ExecSQL(conn_db,
                " update t_rsh set rsh_gkal =" +
                " (select max(" + sNvlWord + "(p.val_prm,'0')" + sConvToNum + ") " +
                  " from t_par2 p" +
                  " where p.nzp_prm=723 " +
                  " and p.nzp in (select nzp_dom from t_prm pp where pp.nzp_kvar=t_rsh.nzp_kvar) " +
                  " and p.dat_s <= t_rsh.dat_po and p.dat_po >= t_rsh.dat_s )" +
                " where t_rsh.nzp_frm_typ=1814 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // наличие нормы на л/с - nzp_prm=2463 - если есть, то в первую очередь
            // в CalcTypeTarif_GKalDop определена таблица t_par2463
            ret = ExecSQL(conn_db,
                " update t_rsh set rsh_gkal =" +
                " (" +
                  " select max(p2.vald) from t_par2463 p2 where p2.nzp=t_rsh.nzp_kvar " +
                  " and p2.dat_s <= t_rsh.dat_po and p2.dat_po >= t_rsh.dat_s " +
                " )" +
                " where t_rsh.nzp_frm_typ=1814 " +
                "  and exists (" +
                  " select 1 from t_par2463 p2 where p2.nzp=t_rsh.nzp_kvar " +
                  " and p2.dat_s <= t_rsh.dat_po and p2.dat_po >= t_rsh.dat_s " +
                ") "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // в Самаре для коммуналок жилая площадь
            if (Points.IsSmr)
            {
                ret = ExecSQL(conn_db,
                    " update t_rsh set rsh_gkal =" +
                    " (select max(" + sNvlWord + "(p.val_prm,'0')" + sConvToNum + ") " +
                      " from t_par2 p" +
                      " where p.nzp_prm=2074 " +
                      " and p.nzp in (select nzp_dom from t_prm pp where pp.nzp_kvar=t_rsh.nzp_kvar) " +
                      " and 0<(select count(*) from t_kmnl r where t_rsh.nzp_kvar=r.nzp_kvar) " +
                      " and p.dat_s <= t_rsh.dat_po and p.dat_po >= t_rsh.dat_s )" +
                    " where t_rsh.nzp_frm_typ=1814 and t_rsh.nzp_prm in (6) "
                    // добавить условие на площадь жилую и общую
                    , true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }
            }

            #endregion тип расхода = 1814 - параметр норма Отопления гКалл на 1 м2 для каждого дома

            #region тип расхода = 11,1814 - учет расхода ПУ

            #region выборка расходов по ПУ - для тип расхода =  1814  [(отопление Гкал)]

            CalcTypeRashod_1814(conn_db, gku, out ret);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            #endregion выборка расходов по ПУ - для тип расхода =  1814  [(отопление Гкал)]

            // если был домовой расчет! - kod_info>0
            ret = ExecSQL(conn_db,
                " update t_rsh " +
                " set rash_norm_one=" +
                        "(select max(r.rash_norm_one) from t_rsh1814 r where r.nzp_kvar=t_rsh.nzp_kvar and r.nzp_frm=t_rsh.nzp_frm and t_rsh.nzp_period=r.nzp_period)" +
                  ", up_kf=" +
                        "(select max(r.up_kf) from t_rsh1814 r where r.nzp_kvar=t_rsh.nzp_kvar and r.nzp_frm=t_rsh.nzp_frm and t_rsh.nzp_period=r.nzp_period)" +
                  ", rashod_source=" +
                        "(select max(r.val1_source) from t_rsh1814 r where r.nzp_kvar=t_rsh.nzp_kvar and r.nzp_frm=t_rsh.nzp_frm and t_rsh.nzp_period=r.nzp_period)" +
                  ", is_device=" +
                        "(select max(r.is_device) from t_rsh1814 r where r.nzp_kvar=t_rsh.nzp_kvar and r.nzp_frm=t_rsh.nzp_frm and t_rsh.nzp_period=r.nzp_period)" +
                  ", kod_info=" +
                        "(select max(r.kod_info) from t_rsh1814 r where r.nzp_kvar=t_rsh.nzp_kvar and r.nzp_frm=t_rsh.nzp_frm and t_rsh.nzp_period=r.nzp_period)" +
                  ", valmk=" +
                        "(select max(r.rsh1) from t_rsh1814 r where r.nzp_kvar=t_rsh.nzp_kvar and r.nzp_frm=t_rsh.nzp_frm and t_rsh.nzp_period=r.nzp_period)" +
                " where nzp_frm_typrs in (11,1814) and nzp_frm_typ=1814 " +
                " and exists (select 1 from t_rsh1814 r where r.nzp_kvar=t_rsh.nzp_kvar and r.nzp_frm=t_rsh.nzp_frm and t_rsh.nzp_period=r.nzp_period) "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // если был ИПУ! - is_device>0
            // если был домовой расчет! - kod_info>0
            ret = ExecSQL(conn_db,
                " update t_rsh " +
                " set rsh3=kod_info, rsh_gkal= valmk " +
                " where nzp_frm_typrs in (11,1814) and nzp_frm_typ=1814 " +
                " and (is_device in (1,9) or kod_info>0) "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            #endregion тип расхода = 11,1814 - учет расхода ПУ

            #region тип расхода = 11,1814 - перевод отопление из м2 в ГКал
            // rsh1 - площадь ЛС
            // rsh2 - расход ГКал на 1 кв.м
            ret = ExecSQL(conn_db,
                " update t_rsh set  rsh1 = rashod, " +
                " rsh2 = CASE WHEN is_device=0 THEN rsh_gkal*" + sNvlWord + "(up_kf,1) ELSE rsh_gkal END " + //применяем повышающий коэф-т
                " where nzp_frm_typrs in (11,1814) and nzp_frm_typ=1814 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // расчитать окончательный расход в Гкал отопление = расход в м2 * расход в ГКал на отопл 1 м2
            ret = ExecSQL(conn_db,
                " update t_rsh set rashod = rsh1 * rsh2" +
                " where nzp_frm_typrs in (11,1814) and nzp_frm_typ=1814 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            #endregion тип расхода = 11,1814 - перевод отопление из м2 в ГКал

            #region тип расхода = 11,1814 - округлить если надо
            // Нужно округлить до если есть признак округления Вот и все что здесь нового 
            ret = ExecSQL(conn_db,
                " update t_rsh set valm = round(rashod,is_trunc) " +
                " where nzp_frm_typrs in (11,1814) and nzp_frm_typ=1814 and is_trunc between 1 and 6 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " update t_rsh set rashod = valm " +
                " where nzp_frm_typrs in (11,1814) and nzp_frm_typ=1814 and is_trunc between 1 and 6 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            #endregion тип расхода = 11,1814 - округлить если надо

            #endregion Завершающие действия по формуле 1814

            #region Специфика расчета дотаций
            // ... для расчета дотаций рассчитывается ПТ (ЭОТ - тариф по услуге в t_perc_pt) ...
            if (bIsCalcSubsidy)
            {
                // ... установка ПТ тарифов ...
                ret = ExecSQL(conn_db,
                    " create temp table t_perc_pt (" +
                    " nzp_dom  integer," +
                    " nzp_kvar integer," +
                    " nzp_serv integer," +
                    " nzp_supp integer," +
                    " nzp_vill " + sDecimalType + "(13,0) default 0," +
                    " nzp_frm  integer," +
                    " etag     integer," +
                    " perc     " + sDecimalType + "(14,8) default 1," +
                    " tarif    " + sDecimalType + "(17,7) default 0," + // eot
                    " norm_gkal  " + sDecimalType + "(14,8) default 0," +
                    " koef_gkal  " + sDecimalType + "(14,8) default 1," +
                    " tarif_m3   " + sDecimalType + "(17,7) default 0," +
                    " nzp_frm_typ integer default 0," +
                    " tarif_f    " + sDecimalType + "(17,7) default 0," +
                    " nzp_prm_pt  integer default 0" +
                    " ) " + sUnlogTempTable
                    , true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

                // выбрать ЭОТ тарифы по ЛС/услугам/поставщикам
                ret = ExecSQL(conn_db,
                    " insert into t_perc_pt (nzp_dom,nzp_kvar,nzp_serv,nzp_supp,nzp_frm,tarif,norm_gkal,koef_gkal,tarif_m3,nzp_frm_typ)" +
                    " select t.nzp_dom,t.nzp_kvar,t.nzp_serv,t.nzp_supp,t.nzp_frm,p.tarif,p.norm_gkal,p.koef_gkal,p.tarif_m3,p.nzp_frm_typ" +
                    " from t_tarifs t,t_prm_calc p," + paramcalc.kernel_alias + "services s " +
                    " where t.nzp_kvar=p.nzp_kvar and t.nzp_supp=p.nzp_supp and t.nzp_frm=p.nzp_frm and t.nzp_serv=s.nzp_serv and s.is_subs=1"
                    , true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

                ret = ExecSQL(conn_db, " create index ixt_perc_pt1 on t_perc_pt(nzp_kvar,nzp_serv,nzp_supp,nzp_frm) ", true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

                ret = ExecSQL(conn_db, " create index ixt_perc_pt2 on t_perc_pt(nzp_dom) ", true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

                ret = ExecSQL(conn_db, " create index ixt_perc_pt3 on t_perc_pt(nzp_prm_pt) ", true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

                ret = ExecSQL(conn_db, sUpdStat + " t_perc_pt ", true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

                // ... beg выборка установленных ПТ тарифов ...

                // ПТ на БД - если есть, то во последнюю очередь
                ret = ExecSQL(conn_db,
                    " update t_perc_pt set nzp_prm_pt=101,tarif_f = " +
                    " (select max(p.val_prm" + sConvToNum + ")" +
                    " from " + paramcalc.kernel_alias + "formuls_ops_dt f," + paramcalc.data_alias + "prm_5 p " +
                    " where t_perc_pt.nzp_frm=f.nzp_frm and p.nzp_prm=f.nzp_prm_trf_bd" +
                    " and p.is_actual<>100 and p.dat_s<" + gku.dat_po + " and p.dat_po>=" + gku.dat_s + ") " +
                    " where 0<(select count(*)" +
                    " from " + paramcalc.kernel_alias + "formuls_ops_dt f," + paramcalc.data_alias + "prm_5 p " +
                    " where t_perc_pt.nzp_frm=f.nzp_frm and p.nzp_prm=f.nzp_prm_trf_bd" +
                    " and p.is_actual<>100 and p.dat_s<" + gku.dat_po + " and p.dat_po>=" + gku.dat_s + ") "
                    , true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

                // наличие ПТ на поставщика - во вторую очередь
                ret = ExecSQL(conn_db,
                    " update t_perc_pt set nzp_prm_pt=102,tarif_f = " +
                    " (select max(p.val_prm" + sConvToNum + ")" +
                    " from " + paramcalc.kernel_alias + "formuls_ops_dt f," + paramcalc.data_alias + "prm_11 p " +
                    " where t_perc_pt.nzp_frm=f.nzp_frm and t_perc_pt.nzp_supp=p.nzp and p.nzp_prm=f.nzp_prm_trf_su" +
                    " and p.is_actual<>100 and p.dat_s<" + gku.dat_po + " and p.dat_po>=" + gku.dat_s +
                    " and 0<(select count(*) from " + paramcalc.data_alias + "prm_5 p1" +
                           " where p1.nzp_prm=457 and p1.is_actual<>100" +
                           " and p1.dat_s<" + gku.dat_po + " and p1.dat_po>=" + gku.dat_s + ") " +
                    " ) " +
                    " where 0<(select count(*)" +
                    " from " + paramcalc.kernel_alias + "formuls_ops_dt f," + paramcalc.data_alias + "prm_11 p " +
                    " where t_perc_pt.nzp_frm=f.nzp_frm and t_perc_pt.nzp_supp=p.nzp and p.nzp_prm=f.nzp_prm_trf_su" +
                    " and p.is_actual<>100 and p.dat_s<" + gku.dat_po + " and p.dat_po>=" + gku.dat_s +
                    " and 0<(select count(*) from " + paramcalc.data_alias + "prm_5 p1" +
                           " where p1.nzp_prm=457 and p1.is_actual<>100" +
                           " and p1.dat_s<" + gku.dat_po + " and p1.dat_po>=" + gku.dat_s + ") " +
                    " ) "
                    , true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

                // тариф на дом - во первую после ЛС очередь
                ret = ExecSQL(conn_db,
                    " update t_perc_pt set nzp_prm_pt=103,tarif_f = " +
                    " (select max(p.val_prm" + sConvToNum + ")" +
                    " from " + paramcalc.kernel_alias + "formuls_ops_dt f,ttt_prm_2 p" +
                    " where t_perc_pt.nzp_frm=f.nzp_frm and t_perc_pt.nzp_dom=p.nzp and p.nzp_prm=f.nzp_prm_trf_dm) " +
                    " where 0<(select count(*)" +
                    " from " + paramcalc.kernel_alias + "formuls_ops_dt f,ttt_prm_2 p" +
                    " where t_perc_pt.nzp_frm=f.nzp_frm and t_perc_pt.nzp_dom=p.nzp and p.nzp_prm=f.nzp_prm_trf_dm) "
                    , true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

                // наличие ЭОТ на л/с - в первую очередь
                ret = ExecSQL(conn_db,
                    " update t_perc_pt set nzp_prm_pt=104,tarif_f = " +
                    " (select max(p.vald)" +
                      " from " + paramcalc.kernel_alias + "formuls_ops_dt f,t_par1d p" +
                      " where t_perc_pt.nzp_frm=f.nzp_frm and t_perc_pt.nzp_kvar=p.nzp and p.nzp_prm=f.nzp_prm_trf_ls" +
                    // наличие ЭОТ на л/с - nzp_prm=335 - если есть, то в первую очередь
                      " and 0<(select count(*) from t_par335 p1 where p1.nzp=t_perc_pt.nzp_kvar) )" +
                    " where 0<(select count(*)" +
                      " from " + paramcalc.kernel_alias + "formuls_ops_dt f,t_par1d p" +
                      " where t_perc_pt.nzp_frm=f.nzp_frm and t_perc_pt.nzp_kvar=p.nzp and p.nzp_prm=f.nzp_prm_trf_ls" +
                    // наличие ЭОТ на л/с - nzp_prm=335 - если есть, то в первую очередь
                      " and 0<(select count(*) from t_par335 p1 where p1.nzp=t_perc_pt.nzp_kvar) )"
                    , true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

                // расчитать окончательный тариф на 1м3 ГВС
                ret = ExecSQL(conn_db,
                    " update t_perc_pt set tarif_f=round(tarif_f*koef_gkal*norm_gkal*100)/100+tarif_m3 where nzp_frm_typ=40 "
                    , true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

                // ... end выборка установленных ПТ тарифов ... ( 5 ... 8 )

                ret = ExecSQL(conn_db, sUpdStat + " t_perc_pt ", true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

                // ... beg определение ПТ  тарифов в %-х от ЭОТ тарифов для ЛС, у которых тарифы не определены явно (см.выше - t_prm_pt)  ...

                // проставить этажи
                ret = ExecSQL(conn_db,
                    " update t_perc_pt set" +
                    " etag=(select max(p.val_prm" + sConvToInt + ") from ttt_prm_2 p where p.nzp=t_perc_pt.nzp_dom and p.nzp_prm=37)" +
                    " where 0<(select count(*) from ttt_prm_2 p1 where p1.nzp=t_perc_pt.nzp_dom and p1.nzp_prm=37) "
                    , true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

                // проставить МО (муниципальное образование)
                ret = ExecSQL(conn_db,
                    " update t_perc_pt set" +
                    " nzp_vill=(select max(v.nzp_vill) " +
                       " from " + paramcalc.data_alias + "rajon_vill v," + Points.Pref + "_data" + tableDelimiter + "s_ulica u," + paramcalc.data_alias + "dom d " +
                       " where d.nzp_dom=t_perc_pt.nzp_dom and d.nzp_ul=u.nzp_ul and u.nzp_raj=v.nzp_raj)" +
                    " where 0<(select count(*) " +
                       " from " + paramcalc.data_alias + "rajon_vill v," + Points.Pref + "_data" + tableDelimiter + "s_ulica u," + paramcalc.data_alias + "dom d " +
                       " where d.nzp_dom=t_perc_pt.nzp_dom and d.nzp_ul=u.nzp_ul and u.nzp_raj=v.nzp_raj) "
                    , true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

                // (1) если есть % по услуге - без поставщиков / без МО / без этажей
                ret = ExecSQL(conn_db,
                    " update t_perc_pt set nzp_prm_pt=1,perc = " +
                    " ( select max(p.perc) from " + paramcalc.kernel_alias + "s_perc_pt p" +
                      " where p.nzp_serv=t_perc_pt.nzp_serv and p.nzp_supp=0 and p.nzp_vill=0 and p.etag=0 and p.dat_s<" + gku.dat_po + " and p.dat_po>=" + gku.dat_s + " ) " +
                    " where nzp_prm_pt<100 and 0<" +
                    " ( select count(*) from " + paramcalc.kernel_alias + "s_perc_pt p" +
                      " where p.nzp_serv=t_perc_pt.nzp_serv and p.nzp_supp=0 and p.nzp_vill=0 and p.etag=0 and p.dat_s<" + gku.dat_po + " and p.dat_po>=" + gku.dat_s + " ) "
                    , true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

                // (2) если есть % по услуге - без поставщиков / без МО / с этажами
                ret = ExecSQL(conn_db,
                    " update t_perc_pt set nzp_prm_pt=2,perc = " +
                    " ( select max(p.perc) from " + paramcalc.kernel_alias + "s_perc_pt p" +
                      " where p.nzp_serv=t_perc_pt.nzp_serv and p.nzp_supp=0 and p.nzp_vill=0 and p.etag=t_perc_pt.etag and p.dat_s<" + gku.dat_po + " and p.dat_po>=" + gku.dat_s + " ) " +
                    " where nzp_prm_pt<100 and 0<" +
                    " ( select count(*) from " + paramcalc.kernel_alias + "s_perc_pt p" +
                      " where p.nzp_serv=t_perc_pt.nzp_serv and p.nzp_supp=0 and p.nzp_vill=0 and p.etag=t_perc_pt.etag and p.dat_s<" + gku.dat_po + " and p.dat_po>=" + gku.dat_s + " ) "
                    , true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

                // (3) если есть % по услуге - без поставщиков / c МО / без этажей
                ret = ExecSQL(conn_db,
                    " update t_perc_pt set nzp_prm_pt=3,perc = " +
                    " ( select max(p.perc) from " + paramcalc.kernel_alias + "s_perc_pt p" +
                      " where p.nzp_serv=t_perc_pt.nzp_serv and p.nzp_supp=0 and p.nzp_vill=t_perc_pt.nzp_vill and p.etag=0 and p.dat_s<" + gku.dat_po + " and p.dat_po>=" + gku.dat_s + " ) " +
                    " where nzp_prm_pt<100 and 0<" +
                    " ( select count(*) from " + paramcalc.kernel_alias + "s_perc_pt p" +
                      " where p.nzp_serv=t_perc_pt.nzp_serv and p.nzp_supp=0 and p.nzp_vill=t_perc_pt.nzp_vill and p.etag=0 and p.dat_s<" + gku.dat_po + " and p.dat_po>=" + gku.dat_s + " ) "
                    , true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

                // (4) если есть % по услуге - с поставщиком / без МО / без этажей
                ret = ExecSQL(conn_db,
                    " update t_perc_pt set nzp_prm_pt=4,perc = " +
                    " ( select max(p.perc) from " + paramcalc.kernel_alias + "s_perc_pt p" +
                      " where p.nzp_serv=t_perc_pt.nzp_serv and p.nzp_supp=t_perc_pt.nzp_supp and p.nzp_vill=0 and p.etag=0 and p.dat_s<" + gku.dat_po + " and p.dat_po>=" + gku.dat_s + " ) " +
                    " where nzp_prm_pt<100 and 0<" +
                    " ( select count(*) from " + paramcalc.kernel_alias + "s_perc_pt p" +
                      " where p.nzp_serv=t_perc_pt.nzp_serv and p.nzp_supp=t_perc_pt.nzp_supp and p.nzp_vill=0 and p.etag=0 and p.dat_s<" + gku.dat_po + " and p.dat_po>=" + gku.dat_s + " ) "
                    , true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
                // (5) если есть % по услуге - без поставщиков / c МО / с этажами
                ret = ExecSQL(conn_db,
                    " update t_perc_pt set nzp_prm_pt=5,perc = " +
                    " ( select max(p.perc) from " + paramcalc.kernel_alias + "s_perc_pt p" +
                      " where p.nzp_serv=t_perc_pt.nzp_serv and p.nzp_supp=0 and p.nzp_vill=t_perc_pt.nzp_vill and p.etag=t_perc_pt.etag and p.dat_s<" + gku.dat_po + " and p.dat_po>=" + gku.dat_s + " ) " +
                    " where nzp_prm_pt<100 and 0<" +
                    " ( select count(*) from " + paramcalc.kernel_alias + "s_perc_pt p" +
                      " where p.nzp_serv=t_perc_pt.nzp_serv and p.nzp_supp=0 and p.nzp_vill=t_perc_pt.nzp_vill and p.etag=t_perc_pt.etag and p.dat_s<" + gku.dat_po + " and p.dat_po>=" + gku.dat_s + " ) "
                    , true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
                // (6) если есть % по услуге - с поставщиком / без МО / с этажами
                ret = ExecSQL(conn_db,
                    " update t_perc_pt set nzp_prm_pt=6,perc = " +
                    " ( select max(p.perc) from " + paramcalc.kernel_alias + "s_perc_pt p" +
                      " where p.nzp_serv=t_perc_pt.nzp_serv and p.nzp_supp=t_perc_pt.nzp_supp and p.nzp_vill=0 and p.etag=t_perc_pt.etag and p.dat_s<" + gku.dat_po + " and p.dat_po>=" + gku.dat_s + " ) " +
                    " where nzp_prm_pt<100 and 0<" +
                    " ( select count(*) from " + paramcalc.kernel_alias + "s_perc_pt p" +
                      " where p.nzp_serv=t_perc_pt.nzp_serv and p.nzp_supp=t_perc_pt.nzp_supp and p.nzp_vill=0 and p.etag=t_perc_pt.etag and p.dat_s<" + gku.dat_po + " and p.dat_po>=" + gku.dat_s + " ) "
                    , true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
                // (7) если есть % по услуге - с поставщиком / c МО / без этажей
                ret = ExecSQL(conn_db,
                    " update t_perc_pt set nzp_prm_pt=7,perc = " +
                    " ( select max(p.perc) from " + paramcalc.kernel_alias + "s_perc_pt p" +
                      " where p.nzp_serv=t_perc_pt.nzp_serv and p.nzp_supp=t_perc_pt.nzp_supp and p.nzp_vill=t_perc_pt.nzp_vill and p.etag=0 and p.dat_s<" + gku.dat_po + " and p.dat_po>=" + gku.dat_s + " ) " +
                    " where nzp_prm_pt<100 and 0<" +
                    " ( select count(*) from " + paramcalc.kernel_alias + "s_perc_pt p" +
                      " where p.nzp_serv=t_perc_pt.nzp_serv and p.nzp_supp=t_perc_pt.nzp_supp and p.nzp_vill=t_perc_pt.nzp_vill and p.etag=0 and p.dat_s<" + gku.dat_po + " and p.dat_po>=" + gku.dat_s + " ) "
                    , true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
                // (8) если есть % по услуге - с поставщиком / c МО / с этажами
                ret = ExecSQL(conn_db,
                    " update t_perc_pt set nzp_prm_pt=8,perc = " +
                    " ( select max(p.perc) from " + paramcalc.kernel_alias + "s_perc_pt p" +
                      " where p.nzp_serv=t_perc_pt.nzp_serv and p.nzp_supp=t_perc_pt.nzp_supp and p.nzp_vill=t_perc_pt.nzp_vill and p.etag=t_perc_pt.etag and p.dat_s<" + gku.dat_po + " and p.dat_po>=" + gku.dat_s + " ) " +
                    " where nzp_prm_pt<100 and 0<" +
                    " ( select count(*) from " + paramcalc.kernel_alias + "s_perc_pt p" +
                      " where p.nzp_serv=t_perc_pt.nzp_serv and p.nzp_supp=t_perc_pt.nzp_supp and p.nzp_vill=t_perc_pt.nzp_vill and p.etag=t_perc_pt.etag and p.dat_s<" + gku.dat_po + " and p.dat_po>=" + gku.dat_s + " ) "
                    , true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
                // наличие ЭОТ на л/с - в первую очередь
                ret = ExecSQL(conn_db,
                    " update t_perc_pt set tarif_f = tarif * perc / 100" +
                    " where nzp_prm_pt<100 and perc > 0.000001 "
                    , true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

                // ... end определение ПТ  тарифов в %-х от ЭОТ тарифов для ЛС, у которых тарифы не определены явно (см.выше - t_prm_pt)  ...

                // записать ПТ-тарифы
                ret = ExecSQL(conn_db,
                    " update t_prm_calc set " +
#if PG
 " nzp_prm_pt=(select max(p.nzp_prm_pt) from t_perc_pt p where t_prm_calc.nzp_kvar=p.nzp_kvar and t_prm_calc.nzp_supp=p.nzp_supp and t_prm_calc.nzp_frm=p.nzp_frm )," +
                      " tarif_f=(select max(p.tarif_f) from t_perc_pt p where t_prm_calc.nzp_kvar=p.nzp_kvar and t_prm_calc.nzp_supp=p.nzp_supp and t_prm_calc.nzp_frm=p.nzp_frm )," +
                      " perc = (select max(perc) from t_perc_pt p where t_prm_calc.nzp_kvar=p.nzp_kvar and t_prm_calc.nzp_supp=p.nzp_supp and t_prm_calc.nzp_frm=p.nzp_frm ) " +
#else
                    "(nzp_prm_pt,tarif_f,perc) = " +
                    " (( select max(p.nzp_prm_pt),max(p.tarif_f),max(perc) from t_perc_pt p" +
                      " where t_prm_calc.nzp_kvar=p.nzp_kvar and t_prm_calc.nzp_supp=p.nzp_supp and t_prm_calc.nzp_frm=p.nzp_frm )) " +
#endif
 " where 0<" +
                    " ( select count(*) from t_perc_pt p" +
                      " where t_prm_calc.nzp_kvar=p.nzp_kvar and t_prm_calc.nzp_supp=p.nzp_supp and t_prm_calc.nzp_frm=p.nzp_frm ) "
                      , true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

                // проставить повышающий коэффициент расхода в ГКал отопления для Сахи
                if (bIsSaha)
                {
                    ret = ExecSQL(conn_db,
                        " update t_prm_calc set is_p1176 = 1 " +
                        " where nzp_frm_typ=514 and 0<( select count(*) from ttt_prm_2 p where t_prm_calc.nzp_dom=p.nzp and p.nzp_prm=1176 and p.val_prm='2' ) "
                          , true);
                    if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

                    ret = ExecSQL(conn_db,
                        " update t_prm_calc set is_p1178 = 1 " +
                        " where nzp_frm_typ=514 and 0<( select count(*) from ttt_prm_2 p where t_prm_calc.nzp_dom=p.nzp and p.nzp_prm=1178 ) "
                          , true);
                    if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

                    ret = ExecSQL(conn_db,
                        " update t_prm_calc set is_p1195 = 1 " +
                        " where nzp_frm_typ=514 and 0<( select count(*) from ttt_prm_2 p where t_prm_calc.nzp_dom=p.nzp and p.nzp_prm=1195 ) "
                          , true);
                    if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

                    // если ПУ, то повышающие коэффициенты не применяются
                    ret = ExecSQL(conn_db,
                        " update t_prm_calc set is_notdev = 0 " +
                        " where nzp_frm_typ=514 and 0<( select count(*) from t_rsh r" +
                        " Where r.nzp_kvar=t_prm_calc.nzp_kvar and r.nzp_frm=t_prm_calc.nzp_frm and r.is_device>0 ) "
                        , true);
                    if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

                    decimal r1196 = 0; // повышаюший коэффициент на остекление - Саха
                    object count = ExecScalar(conn_db,
                        " Select max(val_prm)" + sConvToNum + " From " + paramcalc.data_alias + "prm_5 Where nzp_prm=1196 and is_actual<>100 ",
                        out ret, true);
                    if (ret.result)
                    {
                        try { r1196 = Convert.ToDecimal(count) / 100; }
                        catch { r1196 = 0; }
                    }
                    decimal r1197 = 0; // повышаюший коэффициент на просушку - Саха
                    count = ExecScalar(conn_db,
                        " Select max(val_prm)" + sConvToNum + " From " + paramcalc.data_alias + "prm_5 Where nzp_prm=1197 and is_actual<>100 ",
                        out ret, true);
                    if (ret.result)
                    {
                        try { r1197 = Convert.ToDecimal(count) / 100; }
                        catch { r1197 = 0; }
                    }
                    decimal r1198 = 0; // повышаюший коэффициент на вентиляцию - Саха
                    count = ExecScalar(conn_db,
                        " Select max(val_prm)" + sConvToNum + " From " + paramcalc.data_alias + "prm_5 Where nzp_prm=1198 and is_actual<>100 ",
                        out ret, true);
                    if (ret.result)
                    {
                        try { r1198 = Convert.ToDecimal(count) / 100; }
                        catch { r1198 = 0; }
                    }

                    // вычислить повышаюший коэффициент на норматив по отоплению - Саха
                    ret = ExecSQL(conn_db,
                        " update t_prm_calc set koef_gkal = koef_gkal * " +
                        " (1 + is_notdev * is_p1176 * " + r1196.ToString() + " + is_notdev * is_p1178 * " + r1197.ToString() + " + is_notdev * is_p1195 * " + r1198.ToString() + " ) " +
                        " where nzp_frm_typ=514 "
                          , true);
                    if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

                }
                //

            }

            #endregion Специфика расчета дотаций

            #region Включен самарский алгоритм учета перерасчета расходов в расходе тек.месяца

            //1988|Учет перерасчета расходов ИПУ для Самары в текущем месяце|||bool||5||||
            bool bUchetRecalcRash =
                CheckValBoolPrm(conn_db, paramcalc.data_alias, 1988, "5");

            if (bUchetRecalcRash &&
                (gku.paramcalc.cur_yy == gku.paramcalc.calc_yy) && (gku.paramcalc.cur_mm == gku.paramcalc.calc_mm))
            {

                //ViewTbl(conn_db, " select * from t_rsh where nzp_kvar=551956 ");

                // если ОДН меньше 0, то не применять его если отрицательный расход по перерасчету по ИПУ round(rashod,is_trunc)
                ret = ExecSQL(conn_db,
                    " update t_rsh set recalc_ipu=1," +
                    " rashod = valm+dlt_reval," +
                    " rsh1 = valmk+dlt_revalk" +
                    " where nzp_frm_typrs in (5,440) and abs(dlt_reval)>0.000001 and kod_info in (31,32,33,36) and (valm+dlt_reval)<0 "
                    , true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

                //ViewTbl(conn_db, " select * from t_rsh where nzp_kvar=551956 ");

                ret = ExecSQL(conn_db,
                    " update t_rsh set recalc_ipu=1," +
                    " rashod = case when valm+dlt_reval+dop87>0    then valm+dlt_reval+dop87    else 0 end, " +
                    " rsh1   = case when valmk+dlt_revalk+dop87k>0 then valmk+dlt_revalk+dop87k else 0 end" +
                    " where nzp_frm_typrs in (5,440) and abs(dlt_reval)>0.000001 and kod_info in (31,32,33,36) and (valm+dlt_reval)>=0 and rashod<=0.00000001 "
                    , true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

                //ViewTbl(conn_db, " select * from t_rsh where nzp_kvar=551956 ");

                ret = ExecSQL(conn_db,
                    " update t_rsh set recalc_ipu=1," +
                    " rashod = rashod + dlt_reval," +
                    " rsh1 = rsh1+dlt_revalk" +
                    " where nzp_frm_typrs in (5,440) and abs(dlt_reval)>0.000001 and kod_info in (31,32,33,36) and (valm+dlt_reval)>=0 and rashod>0.00000001 "
                    , true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

                //ViewTbl(conn_db, " select * from t_rsh where nzp_kvar=551956 ");

                ret = ExecSQL(conn_db,
                    " update t_rsh set recalc_ipu=1," +
                    " rashod = rashod + dlt_reval," +
                    " rsh1 = rsh1+dlt_revalk" +
                    " where nzp_frm_typrs in (5,440) and abs(dlt_reval)>0.000001 and kod_info not in (31,32,33,36) "
                    , true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

                //ViewTbl(conn_db, " select * from t_rsh ");

                // KAN = HV + GV
                ExecSQL(conn_db, " Drop table t_rsh_recalc6 ", false);
                ExecSQL(conn_db, " Drop table t_rsh_recalc9 ", false);

                ret = ExecSQL(conn_db,
                    " Select t.nzp_kvar,r.rashod,r.rashod_g" +
#if PG
 " into temp t_rsh_recalc6 " +
#else
                    " " +
#endif
 " From t_tarifs t, t_rsh r" +
                    " Where t.nzp_kvar=r.nzp_kvar and t.nzp_frm=r.nzp_frm and r.recalc_ipu=1 and t.nzp_serv=6 " +
#if PG
 " "
#else
                    " into temp t_rsh_recalc6 with no log "
#endif
, true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

                ret = ExecSQL(conn_db, " create index ixt_rsh_recalc6 on t_rsh_recalc6(nzp_kvar) ", true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

                ret = ExecSQL(conn_db, sUpdStat + " t_rsh_recalc6 ", true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

                //ViewTbl(conn_db, " select * from t_rsh_recalc6 ");

                ret = ExecSQL(conn_db,
                    " Select t.nzp_kvar,(case when r.nzp_frm_typ=1140 then r.rsh1 else r.rashod end) rashod,r.rashod_g" +
#if PG
 " into temp t_rsh_recalc9 " +
#else
                    " " +
#endif
 " From t_tarifs t, t_rsh r" +
                    " Where t.nzp_kvar=r.nzp_kvar and t.nzp_frm=r.nzp_frm and r.recalc_ipu=1 and t.nzp_serv=9 " +
#if PG
 " "
#else
                    " into temp t_rsh_recalc9 with no log "
#endif
, true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

                ret = ExecSQL(conn_db, " create index ixt_rsh_recalc9 on t_rsh_recalc9(nzp_kvar) ", true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

                ret = ExecSQL(conn_db, sUpdStat + " t_rsh_recalc9 ", true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

                //ViewTbl(conn_db, " select * from t_rsh_recalc9 ");

                ret = ExecSQL(conn_db,
                    " update t_rsh set recalc_ipu=1," +
                    " rsh1=(select sum(r.rashod) from t_rsh_recalc6 r where r.nzp_kvar=t_rsh.nzp_kvar)" +
                    " where nzp_frm_typrs in (390) and nzp_prm>=100 " +
                    " and 0<(select count(*) from t_rsh_recalc6 r where r.nzp_kvar=t_rsh.nzp_kvar) "
                    , true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

                ret = ExecSQL(conn_db,
                    " update t_rsh set recalc_ipu=recalc_ipu+2," +
                    " rsh2=(select sum(r.rashod) from t_rsh_recalc9 r where r.nzp_kvar=t_rsh.nzp_kvar)" +
                    " where nzp_frm_typrs in (390) and nzp_prm>=100 " +
                    " and 0<(select count(*) from t_rsh_recalc9 r where r.nzp_kvar=t_rsh.nzp_kvar) "
                    , true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

                //ViewTbl(conn_db, " select * from t_rsh where nzp_frm_typrs in (390) and nzp_prm>=100");

                ret = ExecSQL(conn_db,
                    // rashod,rsh_hv
                    " update t_rsh set rashod = rashod - rsh_hv + rsh1" +
                    " where nzp_frm_typrs in (390) and nzp_prm>=100 and recalc_ipu=1 "
                    , true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

                ret = ExecSQL(conn_db,
                    // rashod,rsh_hv
                    " update t_rsh set rsh_hv = rsh1" +
                    " where nzp_frm_typrs in (390) and nzp_prm>=100 and recalc_ipu=1 "
                    , true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

                ret = ExecSQL(conn_db,
                    // rashod,rsh_hv
                    " update t_rsh set rashod = rsh_hv + rsh2" +
                    " where nzp_frm_typrs in (390) and nzp_prm>=100 and recalc_ipu=2 "
                    , true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

                ret = ExecSQL(conn_db,
                    // rashod,rsh_hv
                    " update t_rsh set rashod = rsh1 + rsh2, rsh_hv = rsh1" +
                    " where nzp_frm_typrs in (390) and nzp_prm>=100 and recalc_ipu=3 "
                    , true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

                ExecSQL(conn_db, " Drop table t_rsh_recalc6 ", false);
                ExecSQL(conn_db, " Drop table t_rsh_recalc9 ", false);
            }

            #endregion Включен самарский алгоритм учета перерасчета расходов в расходе тек.месяца

            #region Непосредственно вставка в БД
            //
            // учет начисления только ОДН по коммунальной услуге
            // 1052|Начислять только ОДН по хол.воде        |bool|1|
            // 1053|Начислять только ОДН по гор.воде        |bool|1|
            // 1054|Начислять только ОДН по канализации     |bool|1|
            // 1055|Начислять только ОДН по электроэнергии  |bool|1|
            // 1056|Начислять только ОДН по газу            |bool|1|
            // 1057|Начислять только ОДН по отоплению       |bool|1|

            #region подготовка таблицы для вставки
            //DataTable drr1= ViewTbl(conn_db, "select * from t_rsh ");
            //DataTable drr2= ViewTbl(conn_db, "select * from t_tarifs ");
            //DataTable drr3= ViewTbl(conn_db, "select * from t_prm_calc ");

            ret = ExecSQL(conn_db,
                " Create temp table t_calc_gku_tab_xx" +
                " (  nzp_clc        serial        not null, " +
                "    nzp_dom        integer       not null, " +
                "    nzp_kvar       integer       not null, " +
                "    nzp_serv       integer       not null, " +
                "    nzp_supp       integer       not null, " +
                "    nzp_frm        integer       default 0 not null, " +
                "    dat_charge     date, " +
                "    nzp_prm_tarif  integer       default 0 not null, " +
                "    nzp_prm_rashod integer       default 0 not null, " +
                "    tarif          " + sDecimalType + "(17,7) default 0.00 not null, " +
                "    rashod         " + sDecimalType + "(14,7) default 0.00 not null, " +
                "    rashod_source  " + sDecimalType + "(14,7) default 0," +
                "    up_kf          " + sDecimalType + "(14,7) default 1," +
                "    up_kf_gv       " + sDecimalType + "(14,7) default 1," +
                "    rashod_norm    " + sDecimalType + "(14,7) default 0.0000000 NOT NULL," +
                "    rashod_g       " + sDecimalType + "(14,7) default 0.0000000 NOT NULL," +    //расход лс без учета вр.выбывших
                "    gil            " + sDecimalType + "(14,7) default 0.00         , " +        //кол-во жильцов в лс
                "    gil_g          " + sDecimalType + "(14,7) default 0.00         , " +        //кол-во жильцов в лс без учета вр.выбывших
                "    squ            " + sDecimalType + "(14,7) default 0.00         , " +        //площадь лс
                "    trf1           " + sDecimalType + "(14,4) default 0.00 not null, " +
                "    trf2           " + sDecimalType + "(14,4) default 0.00 not null, " +
                "    trf3           " + sDecimalType + "(14,4) default 0.00 not null, " +
                "    trf4           " + sDecimalType + "(14,4) default 0.00 not null, " +
                "    rsh1           " + sDecimalType + "(14,7) default 0.00 not null, " +
                "    rsh2           " + sDecimalType + "(14,7) default 0.00 not null, " +
                "    rsh3           " + sDecimalType + "(14,7) default 0.00 not null, " +
                "    nzp_prm_trf_dt integer       default 0    not null, " +
                "    tarif_f        " + sDecimalType + "(17,7) default 0.00 not null, " +
                "    rash_norm_one  " + sDecimalType + "(14,7) default 0.0000 NOT NULL, " +
                "    valm           " + sDecimalType + "(15,7) default 0.0000 NOT NULL, " +
                "    dop87          " + sDecimalType + "(15,7) default 0.0000 NOT NULL, " +
                "    dlt_reval      " + sDecimalType + "(14,7) default 0.0000 NOT NULL, " +
                "    is_device      integer default 0 NOT NULL, " +
                "    nzp_frm_typ    integer default 0 not null, " +
                "    nzp_frm_typrs  integer default 0 not null, " +
                "    rashod_full    " + sDecimalType + "(15,7) default 0.00 not null, " +
                "    stek   integer default 3 not null,  " +
                "    dat_s  date, " +
                "    dat_po date, " +
                "    cntd    integer default 0 not null,  " +
                "    cntd_mn integer default 0 not null,  " +
                "    nzp_period  integer default 0 not null, " +
                "    use_pk      integer default 0 not null, " +
                "    kod_info    integer default 0 NOT NULL, " +
                "    is_day_calc integer not null" +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // вставка расчета в БД
            string ssql =
                " Insert into t_calc_gku_tab_xx" +
                " ( nzp_dom,nzp_kvar,nzp_serv,nzp_supp,nzp_frm,nzp_period," + f_dat_charge +

                " nzp_frm_typ,gil,gil_g,squ,nzp_prm_tarif,tarif,trf1,trf2,trf3,trf4," +

                " nzp_frm_typrs,nzp_prm_rashod,rashod,rashod_g,rsh1,rsh2,rsh3,nzp_prm_trf_dt,tarif_f," +
                " rashod_norm,valm,dop87,is_device,rash_norm_one,dlt_reval" +

                ",stek,dat_s,dat_po,cntd,cntd_mn,is_day_calc,rashod_source,up_kf,kod_info ) " +

                " Select " +
                " t.nzp_dom,t.nzp_kvar,t.nzp_serv,t.nzp_supp,t.nzp_frm,t.nzp_period," + s_dat_charge +

                sNvlWord + "(p.nzp_frm_typ,0)," + sNvlWord + "(p.gil,0)," + sNvlWord + "(p.gil_g,0)," + sNvlWord + "(p.squ,0)," + sNvlWord + "(p.nzp_prm,0)," +
                sNvlWord + "(p.tarif,0)," + sNvlWord + "(p.tarif_gkal,0)," + sNvlWord + "(p.norm_gkal,0)," + sNvlWord + "(p.koef_gkal,0)," + sNvlWord + "(p.perc,0)," +

                sNvlWord + "(r.nzp_frm_typrs,0)," + sNvlWord + "(r.nzp_prm,0)," + sNvlWord + "(r.rashod,0)," + sNvlWord + "(r.rashod_g,0)," + sNvlWord + "(r.rsh1,0)," +
                sNvlWord + "(r.rsh2,0)," + sNvlWord + "(case when t.nzp_serv in (8,513) then r.rsh3 else r.rsh_hv end,0) " +
                " ," + sNvlWord + "(p.nzp_prm_pt,0)," + sNvlWord + "(p.tarif_f,0)," + sNvlWord + "(r.rashod_norm,0)," + sNvlWord + "(r.valm,0)," + sNvlWord + "(r.dop87,0)," +
                sNvlWord + "(r.is_device,0)," + sNvlWord + "(r.rash_norm_one,0)," + sNvlWord + "(r.dlt_reval,0) " +

                ",1 stek,t.dat_s,t.dat_po,t.cntd,t.cntd_mn,t.is_day_calc, " + sNvlWord + "(r.rashod_source,0)," + sNvlWord + "(r.up_kf,1)," + sNvlWord + "(r.kod_info,0) " +
                " From t_tarifs t" +
#if PG
 " left outer join t_prm_calc p on t.nzp_kvar=p.nzp_kvar and t.nzp_frm=p.nzp_frm and t.nzp_supp=p.nzp_supp" +
                  " and t.nzp_period=p.nzp_period " +
                " left outer join t_rsh r on t.nzp_kvar=r.nzp_kvar and t.nzp_frm=r.nzp_frm " +
                  " and t.nzp_period=r.nzp_period " +
                " Where 1=1 ";
#else
                " ,outer t_prm_calc p,outer t_rsh r" +
                " Where t.nzp_kvar=p.nzp_kvar and t.nzp_supp=p.nzp_supp and t.nzp_frm=p.nzp_frm" +
                "   and t.nzp_kvar=r.nzp_kvar and t.nzp_frm=r.nzp_frm " +
                  " and t.nzp_period=p.nzp_period " +
                  " and t.nzp_period=r.nzp_period ";
#endif
            ret = ExecSQL(conn_db, ssql, true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, " create unique index ix1_t_calc_gku_tab_xx on t_calc_gku_tab_xx (nzp_clc)", true);
            ret = ExecSQL(conn_db, " create index ix2_t_calc_gku_tab_xx on t_calc_gku_tab_xx (stek,nzp_frm_typrs,nzp_serv)", true);
            ret = ExecSQL(conn_db, " create index ix3_t_calc_gku_tab_xx on t_calc_gku_tab_xx (nzp_kvar,nzp_serv,nzp_supp)", true);
            ret = ExecSQL(conn_db, " create index ix4_t_calc_gku_tab_xx on t_calc_gku_tab_xx (stek,nzp_kvar,nzp_serv,nzp_supp, nzp_period)", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, sUpdStat + " t_calc_gku_tab_xx ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // сохранить полный результат до деления по дням
            ret = ExecSQL(conn_db,
                " Update t_calc_gku_tab_xx" +
                " Set rashod_full = rashod " +
                " Where 1=1 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // установить ЛС, для кот. по-дневного расчета нет!
            ret = ExecSQL(conn_db,
                " Update t_calc_gku_tab_xx" +
                " Set stek = 3 " +
                " Where is_day_calc = 0 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // поделить расход по дням
            ret = ExecSQL(conn_db,
                " Update t_calc_gku_tab_xx" +
                " Set rashod   = rashod_full * (cntd * 1.00 / cntd_mn), " +
                    " rashod_g = rashod_g    * (cntd * 1.00 / cntd_mn)  " +
                " Where stek = 1 and nzp_frm_typrs not in (5,440,39,391,390) "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            #region учет повышающих коэффициентов по нормативам

            // учет - Вода ДомЖив(201)/Вода Трансп(202)/Полив(200) - повышающий коэффициент по ХВС
            ret = ExecSQL(conn_db,
                " update t_calc_gku_tab_xx u set " +
                " up_kf = " +
                  " (Select max(" + sNvlWord + "(p.val_prm,'0')" + sConvToNum + ") from ttt_prm_2d p" +
                  "  where p.nzp_prm=1557 and p.nzp=u.nzp_dom" +
                  "    and u.dat_s<=p.dat_po and u.dat_po>=p.dat_s) " +
                ", use_pk=3 " +
                " Where nzp_serv in (200,201,202,203) and rashod>0.00001 " +
                  " and exists (Select 1 from ttt_prm_2d p" +
                  "  where p.nzp_prm=1557 and p.nzp=u.nzp_dom and " + sNvlWord + "(p.val_prm,'0')" + sConvToNum + ">1.00001 " +
                  "    and u.dat_s<=p.dat_po and u.dat_po>=p.dat_s) " +
                  " and exists (Select 1 from t_calc_gku_tab_xx t, " + Points.Pref + "_kernel.serv_norm_koef d" +
                  " Where t.nzp_kvar=u.nzp_kvar and t.nzp_serv=d.nzp_serv and d.nzp_serv_link=u.nzp_serv) "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // учет - Канал(7)/Очистка стоков(324)/Трансп Стоков(535) - повышающий коэффициент по ХВС
            ret = ExecSQL(conn_db,
                " update t_calc_gku_tab_xx u set " +
                " up_kf = " +
                  " (Select max(" + sNvlWord + "(p.val_prm,'0')" + sConvToNum + ") from ttt_prm_2d p" +
                  "  where p.nzp_prm=1557 and p.nzp=u.nzp_dom " +
                  "    and u.dat_s<=p.dat_po and u.dat_po>=p.dat_s) " +
                ", use_pk=1 " +
                " Where u.nzp_serv in (7,324,353) and u.nzp_frm_typrs in (39,391) and u.kod_info=0 and u.rashod>0.00001 " +
                  " and exists (Select 1 from ttt_prm_2d p" +
                  "  where p.nzp_prm=1557 and p.nzp=u.nzp_dom and " + sNvlWord + "(p.val_prm,'0')" + sConvToNum + ">1.00001 " +
                  "    and u.dat_s<=p.dat_po and u.dat_po>=p.dat_s) " +
                  " and exists (Select 1 from t_calc_gku_tab_xx t, " + Points.Pref + "_kernel.serv_norm_koef d" +
                  " Where t.nzp_kvar=u.nzp_kvar and t.nzp_serv=d.nzp_serv and d.nzp_serv_link=u.nzp_serv) " +
                  " and exists (Select 1 from t_calc_gku_tab_xx t, " + Points.Pref + "_kernel.serv_norm_koef d" +
                  " Where t.nzp_kvar=u.nzp_kvar and t.nzp_serv=d.nzp_serv and d.nzp_serv_link=6) "
                  , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " update t_calc_gku_tab_xx u set " +
                " up_kf_gv = " +
                  " (Select max(" + sNvlWord + "(p.val_prm,'0')" + sConvToNum + ") from ttt_prm_2d p" +
                  "  where p.nzp_prm=1558 and p.nzp=u.nzp_dom " +
                  "    and u.dat_s<=p.dat_po and u.dat_po>=p.dat_s) " +
                ", use_pk=1 " +
                " Where u.nzp_serv in (7,324,353) and u.nzp_frm_typrs in (39) and u.kod_info=0 and u.rashod>0.00001 " + // только для ХВ+ГВ
                  " and exists (Select 1 from ttt_prm_2d p" +
                  "  where p.nzp_prm=1558 and p.nzp=u.nzp_dom and " + sNvlWord + "(p.val_prm,'0')" + sConvToNum + ">1.00001 " +
                  "    and u.dat_s<=p.dat_po and u.dat_po>=p.dat_s) " +
                  " and exists (Select 1 from t_calc_gku_tab_xx t, " + Points.Pref + "_kernel.serv_norm_koef d" +
                  " Where t.nzp_kvar=u.nzp_kvar and t.nzp_serv=d.nzp_serv and d.nzp_serv_link=u.nzp_serv) " +
                  " and exists (Select 1 from t_calc_gku_tab_xx t, " + Points.Pref + "_kernel.serv_norm_koef d" +
                  " Where t.nzp_kvar=u.nzp_kvar and t.nzp_serv=d.nzp_serv and d.nzp_serv_link=9) "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " update t_calc_gku_tab_xx u set " +
                " up_kf = " +
                  " (Select max(" + sNvlWord + "(p.val_prm,'0')" + sConvToNum + ") from ttt_prm_2d p" +
                  "  where p.nzp_prm=1557 and p.nzp=u.nzp_dom " +
                  "    and u.dat_s<=p.dat_po and u.dat_po>=p.dat_s) " +
                ", use_pk=1 " +
                " Where u.nzp_serv in (7,324,353) and u.nzp_frm_typrs not in (390,339,39,391) and u.rashod>0.00001 " +
                  " and exists (Select 1 from ttt_prm_2d p" +
                  "  where p.nzp_prm=1557 and p.nzp=u.nzp_dom and " + sNvlWord + "(p.val_prm,'0')" + sConvToNum + ">1.00001 " +
                  "    and u.dat_s<=p.dat_po and u.dat_po>=p.dat_s) " +
                  " and exists (Select 1 from t_calc_gku_tab_xx t, " + Points.Pref + "_kernel.serv_norm_koef d" +
                  " Where t.nzp_kvar=u.nzp_kvar and t.nzp_serv=d.nzp_serv and d.nzp_serv_link=u.nzp_serv) "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " update t_calc_gku_tab_xx u set " +
                " up_kf_gv = " +
                  " (Select max(" + sNvlWord + "(p.val_prm,'0')" + sConvToNum + ") from ttt_prm_2d p" +
                  "  where p.nzp_prm=1558 and p.nzp=u.nzp_dom " +
                  "    and u.dat_s<=p.dat_po and u.dat_po>=p.dat_s) " +
                ", use_pk=1 " +
                " Where u.nzp_serv in (7,324,353) and u.nzp_frm_typrs not in (390,339,39,391) and u.rashod>0.00001 " +
                  " and exists (Select 1 from ttt_prm_2d p" +
                  "  where p.nzp_prm=1558 and p.nzp=u.nzp_dom and " + sNvlWord + "(p.val_prm,'0')" + sConvToNum + ">1.00001 " +
                  "    and u.dat_s<=p.dat_po and u.dat_po>=p.dat_s) " +
                  " and exists (Select 1 from t_calc_gku_tab_xx t, " + Points.Pref + "_kernel.serv_norm_koef d" +
                  " Where t.nzp_kvar=u.nzp_kvar and t.nzp_serv=d.nzp_serv and d.nzp_serv_link=u.nzp_serv) "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // учет - Отопление(8) - повышающий коэффициент по Отопл
            ret = ExecSQL(conn_db,
                " update t_calc_gku_tab_xx u set " +
                " up_kf = " +
                  " (Select max(" + sNvlWord + "(p.val_prm,'0')" + sConvToNum + ") from ttt_prm_2d p" +
                  "  where p.nzp_prm=1556 and p.nzp=u.nzp_dom " +
                  "    and u.dat_s<=p.dat_po and u.dat_po>=p.dat_s) " +
                ", use_pk=2 " +
                " Where nzp_serv=8 and nzp_frm_typrs=1 and rashod>0.00001 " +
                  " and exists (Select 1 from ttt_prm_2d p" +
                  "  where p.nzp_prm=1556 and p.nzp=u.nzp_dom and " + sNvlWord + "(p.val_prm,'0')" + sConvToNum + ">1.00001 " +
                  "    and u.dat_s<=p.dat_po and u.dat_po>=p.dat_s) " +
                  " and exists (Select 1 from t_calc_gku_tab_xx t, " + Points.Pref + "_kernel.serv_norm_koef d" +
                  " Where t.nzp_kvar=u.nzp_kvar and t.nzp_serv=d.nzp_serv and d.nzp_serv_link=u.nzp_serv) "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ExecSQL(conn_db, " Drop table t_calc_gku_pk ", false);
            ret = ExecSQL(conn_db,
                " Create temp table t_calc_gku_pk" +
                " (  nzp_kvar       integer not null, " +
                "    nzp_serv       integer not null, " +
                "    nzp_serv_main  integer not null, " +
                "    stek           integer not null, " +
                "    nzp_period     integer default 0 not null, " +
                "    nzp_frm        integer default 0 not null, " +
                "    tarif          " + sDecimalType + "(16,7) default 0.00 not null, " +
                "    rashod         " + sDecimalType + "(16,7) default 0.00 not null, " +
                "    rashod_main    " + sDecimalType + "(16,7) default 0.00 not null, " +
                "    rashod_old     " + sDecimalType + "(16,7) default 0.00 not null, " +
                "    rashod_hv      " + sDecimalType + "(16,7) default 0.00 not null, " +
                "    up_kf          " + sDecimalType + "(16,7) default 1, " +
                "    up_kf_gv       " + sDecimalType + "(16,7) default 1, " +
                "    nzp_frm_typrs  integer not null, " +
                "    use_pk         integer default 0 not null " +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // установить расходы - если есть действующая услуга на ПК
            ssql =
                " Insert into t_calc_gku_pk" +
                " ( nzp_kvar,nzp_serv,nzp_serv_main,stek,nzp_period,nzp_frm,tarif,rashod,up_kf,up_kf_gv,rashod_main,rashod_old,use_pk,rashod_hv,nzp_frm_typrs ) " +
                " Select t.nzp_kvar,d.nzp_serv,t.nzp_serv,t.stek,t.nzp_period,max(d.nzp_frm),max(t.tarif)," +
                        " max(case when t.nzp_frm_typrs=814 and t.rashod>0.0001" +
                            " then (t.up_kf - 1.00) * t.rashod" +
                            " else t.rashod-t.rashod_source end)," +
                        " max(t.up_kf), max(t.up_kf_gv)," +
                        " max(case when t.nzp_frm_typrs=814 and t.rashod>0.0001" +
                            " then t.rashod - (t.up_kf - 1.00) * t.rashod " +
                            " else t.rashod_source end)," +
                        " max(t.rashod),max(t.use_pk),max(t.rsh3),max(t.nzp_frm_typrs) " +
                " From t_calc_gku_tab_xx t, t_calc_gku_tab_xx a," + Points.Pref + "_kernel.serv_norm_koef d" +
                " Where t.nzp_kvar=a.nzp_kvar and t.nzp_serv=d.nzp_serv_link " +
                  " and a.nzp_serv=d.nzp_serv and (t.up_kf>1.0001 or t.up_kf_gv>1.0001) " +
                  " and t.nzp_frm_typrs not in (390,339) and t.nzp_serv not in (14,374)" +
                  " and not exists (select 1 from " + Points.Pref + "_kernel.serv_odn r where r.nzp_serv=t.nzp_serv)" +
                " group by 1,2,3,4,5 ";
            ret = ExecSQL(conn_db, ssql, true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // установить расходы - КАН in (390,339) - ВСЕГДА! если не действующей услуги на ПК, то основную услугу начислять без ПК
            ssql =
                " Insert into t_calc_gku_pk" +
                " ( nzp_kvar,nzp_serv,nzp_serv_main,stek,nzp_period,nzp_frm,tarif,rashod,up_kf,up_kf_gv,rashod_main,rashod_old,use_pk,rashod_hv,nzp_frm_typrs ) " +
                " Select t.nzp_kvar,d.nzp_serv,t.nzp_serv,t.stek,t.nzp_period,max(d.nzp_frm),max(t.tarif)," +
                        " max(case when t.nzp_frm_typrs=814 and t.rashod>0.0001" +
                            " then (t.up_kf - 1.00) * t.rashod" +
                            " else t.rashod-t.rashod_source end)," +
                        " max(t.up_kf), max(t.up_kf_gv)," +
                        " max(case when t.nzp_frm_typrs=814 and t.rashod>0.0001" +
                            " then t.rashod - (t.up_kf - 1.00) * t.rashod " +
                            " else t.rashod_source end)," +
                        " max(t.rashod),max(t.use_pk),max(t.rsh3),max(t.nzp_frm_typrs) " +
                " From t_calc_gku_tab_xx t," + Points.Pref + "_kernel.serv_norm_koef d" +
                " Where t.nzp_serv=d.nzp_serv_link and (t.up_kf>1.0001 or t.up_kf_gv>1.0001) " +
                  " and (t.nzp_frm_typrs in (390,339) or t.nzp_serv in (14,374)) " +
                " group by 1,2,3,4,5 ";
            ret = ExecSQL(conn_db, ssql, true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // установить расходы - КАН in (390,339) - ВСЕГДА! если не действующей услуги на ПК, то основную услугу начислять без ПК
            ssql =
                " Insert into t_calc_gku_pk" +
                " ( nzp_kvar,nzp_serv,nzp_serv_main,stek,nzp_period,nzp_frm,tarif,rashod,up_kf,up_kf_gv,rashod_main,rashod_old,use_pk,rashod_hv,nzp_frm_typrs ) " +
                " Select t.nzp_kvar,d.nzp_serv,t.nzp_serv,t.stek,t.nzp_period,max(d.nzp_frm),max(t.tarif)," +
                        " max(case when t.nzp_frm_typrs=814 and t.rashod>0.0001" +
                            " then (t.up_kf - 1.00) * t.rashod" +
                            " else t.rashod-t.rashod_source end)," +
                        " max(t.up_kf), max(t.up_kf_gv)," +
                        " max(case when t.nzp_frm_typrs=814 and t.rashod>0.0001" +
                            " then t.rashod - (t.up_kf - 1.00) * t.rashod " +
                            " else t.rashod_source end)," +
                        " max(t.rashod),max(t.use_pk),max(t.rsh3),max(t.nzp_frm_typrs) " +
                " From t_calc_gku_tab_xx t," + Points.Pref + "_kernel.serv_norm_koef d" +
                " Where t.nzp_serv=d.nzp_serv_link and (t.up_kf>1.0001 or t.up_kf_gv>1.0001) " +
                  " and exists (select 1 from " + Points.Pref + "_kernel.serv_odn r where r.nzp_serv=t.nzp_serv)" +
                " group by 1,2,3,4,5 ";
            ret = ExecSQL(conn_db, ssql, true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, " create unique index ix1_t_calc_gku_pk on t_calc_gku_pk (nzp_kvar,nzp_serv,stek,nzp_period)", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, " create unique index ix2_t_calc_gku_pk on t_calc_gku_pk (nzp_kvar,nzp_serv_main,stek,nzp_period)", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, sUpdStat + " t_calc_gku_pk ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // установить расходы - Вода ДомЖив(201)/Вода Трансп(202)/Полив(200) - повышающий коэффициент по ХВС
            ret = ExecSQL(conn_db,
                " update t_calc_gku_pk" +
                " set " +
                  " rashod = rashod_old * up_kf - rashod_old," +
                  " rashod_main = rashod_old " +
                " Where use_pk=3 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // установить расходы - КАН с ПК установленным принудительно: in (39,391) or not in (390,339)
            ret = ExecSQL(conn_db,
                " update t_calc_gku_pk" +
                " set " +
                  " rashod = (rashod_hv * up_kf + (rashod_old - rashod_hv) * up_kf_gv) - rashod_old," +
                  " rashod_main = rashod_old " +
                " Where use_pk=1 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // учет - Отопление(8) - повышающий коэффициент по Отопл
            ret = ExecSQL(conn_db,
                " update t_calc_gku_pk" +
                " set rashod = (up_kf - 1.00) * rashod_old, rashod_main = rashod_old - (up_kf - 1.00) * rashod_old " +
                " Where use_pk=2 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " update t_calc_gku_pk" +
                " set rashod = 0" +
                " Where rashod<0 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // услуга-спутник
            ssql =
                " Update t_calc_gku_tab_xx " +
                " Set nzp_frm=a.nzp_frm, tarif= a.tarif, up_kf=a.up_kf, rashod=a.rashod, valm=a.rashod, nzp_frm_typ=1901, nzp_frm_typrs=1901 " +
                " From t_calc_gku_pk a " +
                " Where t_calc_gku_tab_xx.nzp_kvar=a.nzp_kvar and t_calc_gku_tab_xx.nzp_serv=a.nzp_serv " +
                " and t_calc_gku_tab_xx.stek=a.stek and t_calc_gku_tab_xx.nzp_period=a.nzp_period" +
                " and t_calc_gku_tab_xx.nzp_frm<>0";
            ret = ExecSQL(conn_db, ssql, true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // основная услуга
            ssql =
                " Update t_calc_gku_tab_xx " +
                " Set rashod=a.rashod_main, valm=a.rashod_main," +
                  " rashod_source = case when a.use_pk=3 then a.rashod_old else rashod_source end, " +
                  " rashod_norm = case when a.use_pk=3 then rashod_norm * a.up_kf else rashod_norm end " +
                " From t_calc_gku_pk a " +
                " Where t_calc_gku_tab_xx.nzp_kvar=a.nzp_kvar and t_calc_gku_tab_xx.nzp_serv=a.nzp_serv_main " +
                " and t_calc_gku_tab_xx.stek=a.stek and t_calc_gku_tab_xx.nzp_period=a.nzp_period" +
                " and t_calc_gku_tab_xx.nzp_frm<>0";
            ret = ExecSQL(conn_db, ssql, true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ssql =
                " Update t_calc_gku_tab_xx " +
                " Set rashod_source=a.rashod_old," +
                " rashod_norm = case when (a.rashod_hv * a.up_kf + (rashod_norm - a.rashod_hv) * a.up_kf_gv)>0.0001" +
                                   " then (a.rashod_hv * a.up_kf + (rashod_norm - a.rashod_hv) * a.up_kf_gv)" +
                                   " else rashod_norm * a.up_kf" +
                                   " end " +
                " From t_calc_gku_pk a " +
                " Where t_calc_gku_tab_xx.nzp_kvar=a.nzp_kvar and t_calc_gku_tab_xx.nzp_serv=a.nzp_serv_main " +
                " and t_calc_gku_tab_xx.stek=a.stek and t_calc_gku_tab_xx.nzp_period=a.nzp_period " +
                " and t_calc_gku_tab_xx.use_pk=1 ";
            ret = ExecSQL(conn_db, ssql, true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ssql =
                " Update t_calc_gku_tab_xx " +
                " Set rashod_source=a.rashod_main  " +
                " From t_calc_gku_pk a " +
                " Where t_calc_gku_tab_xx.nzp_kvar=a.nzp_kvar and t_calc_gku_tab_xx.nzp_serv=a.nzp_serv_main " +
                " and t_calc_gku_tab_xx.stek=a.stek and t_calc_gku_tab_xx.nzp_period=a.nzp_period " +
                " and t_calc_gku_tab_xx.nzp_frm_typrs=814 and a.rashod_old>0.0001 ";
            ret = ExecSQL(conn_db, ssql, true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ExecSQL(conn_db, " Drop table t_calc_gku_pk ", false);

          
            // корректируем расход ОДН в основной услуге
            ret = CorrectOdnInMainServ(conn_db);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            #endregion учет повышающих коэффициентов по нормативам

            ExecSQL(conn_db, " Drop table ttt_aid_virt0 ", false);
            ret = ExecSQL(conn_db,
                " Create temp table ttt_aid_virt0" +
                " (  nzp_kvar integer not null, " +
                "    nzp_serv integer not null, " +
                "    nzp_supp integer not null  " +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " Insert into ttt_aid_virt0 (nzp_kvar,nzp_serv,nzp_supp)" +
                " Select nzp_kvar,nzp_serv,nzp_supp" +
                " From t_calc_gku_tab_xx " +
                " Where stek = 1 and nzp_frm_typrs in (5,390,39,391,339) " +
                " Group by 1,2,3 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }


            ret = ExecSQL(conn_db, " create unique index ix_ttt_aid_virt0 on ttt_aid_virt0 (nzp_kvar,nzp_serv,nzp_supp)", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db, sUpdStat + " ttt_aid_virt0 ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // запись в 3-й стек - сумма (максимум по формулам!) по 1-му стеку
            ret = ExecSQL(conn_db,
                " Insert into t_calc_gku_tab_xx" +
                " ( nzp_dom,nzp_kvar,nzp_serv,nzp_supp,nzp_frm,dat_charge," +
                " nzp_frm_typ,gil,gil_g,squ,nzp_prm_tarif,tarif,trf1,trf2,trf3,trf4," +
                " nzp_frm_typrs,nzp_prm_rashod,rashod,rashod_g,rsh1,rsh2,rsh3,nzp_prm_trf_dt,tarif_f," +
                " rashod_norm,valm,dop87,is_device,rash_norm_one,dlt_reval" +
                ",rashod_full,stek,dat_s,dat_po,cntd,cntd_mn,is_day_calc,rashod_source,up_kf ) " +
                " Select " +
                " nzp_dom,nzp_kvar,nzp_serv,nzp_supp,max(nzp_frm),max(dat_charge)," +
                " max(nzp_frm_typ)," +
                " sum(gil * (cntd * 1.00 / cntd_mn)), sum(gil_g * (cntd * 1.00 / cntd_mn)), max(squ)," +
                " max(nzp_prm_tarif),max(tarif),max(trf1),max(trf2),max(trf3),max(trf4)," +
                " max(nzp_frm_typrs),max(nzp_prm_rashod)," +
                //" sum(rashod),sum(rashod_g)," +
                // для учета по-дневных тарифов: тариф остается утвержденный, а корректируется расход!
                " case when max(tarif) > 0 then sum(tarif * rashod)   / max(tarif) else sum(rashod)   end as rashod," +
                " case when max(tarif) > 0 then sum(tarif * rashod_g) / max(tarif) else sum(rashod_g) end as rashod_g," +
                " max(rsh1),max(rsh2), case when max(nzp_frm_typrs) in (39,391) then sum(rsh3) else max(rsh3) end as rsh3, max(nzp_prm_trf_dt),max(tarif_f)," +
                " max(rashod_norm),max(valm),max(dop87),max(is_device),max(rash_norm_one),max(dlt_reval)" +
                ",max(rashod_full),3 stek,min(dat_s),max(dat_po)," +
                " sum(cntd),max(cntd_mn),max(is_day_calc), " +
                " case when max(tarif) > 0 then sum(tarif * rashod_source)   / max(tarif) else sum(rashod_source)   end as rashod_source," +
                " max(up_kf) " +
                " From t_calc_gku_tab_xx " +
                " Where stek = 1" +
                " and not exists (select 1 from ttt_aid_virt0 a " +
                   " where a.nzp_kvar=t_calc_gku_tab_xx.nzp_kvar " +
                   "   and a.nzp_serv=t_calc_gku_tab_xx.nzp_serv " +
                   "   and a.nzp_supp=t_calc_gku_tab_xx.nzp_supp) " +
                " Group by 1,2,3,4 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " Insert into t_calc_gku_tab_xx" +
                " ( nzp_dom,nzp_kvar,nzp_serv,nzp_supp,nzp_frm,dat_charge," +
                " nzp_frm_typ,gil,gil_g,squ,nzp_prm_tarif,tarif,trf1,trf2,trf3,trf4," +
                " nzp_frm_typrs,nzp_prm_rashod,rashod,rashod_g,rsh1,rsh2,rsh3,nzp_prm_trf_dt,tarif_f," +
                " rashod_norm,valm,dop87,is_device,rash_norm_one,dlt_reval" +
                ",rashod_full,stek,dat_s,dat_po,cntd,cntd_mn,is_day_calc,rashod_source,up_kf ) " +
                " Select " +
                " nzp_dom,nzp_kvar,nzp_serv,nzp_supp,max(nzp_frm),max(dat_charge)," +
                " max(nzp_frm_typ)," +
                " sum(gil * (cntd * 1.00 / cntd_mn)), sum(gil_g * (cntd * 1.00 / cntd_mn)), max(squ)," +
                " max(nzp_prm_tarif),max(tarif),max(trf1),max(trf2),max(trf3),max(trf4)," +
                " max(nzp_frm_typrs),max(nzp_prm_rashod)," +
                //" sum(rashod),sum(rashod_g)," +
                // для учета по-дневных тарифов: тариф остается утвержденный, а корректируется расход!
                " case when max(tarif) > 0 then sum(tarif * rashod)   / max(tarif) else sum(rashod)   end as rashod," +
                " case when max(tarif) > 0 then sum(tarif * rashod_g) / max(tarif) else sum(rashod_g) end as rashod_g," +
                " max(rsh1),max(rsh2),sum(rsh3),max(nzp_prm_trf_dt),max(tarif_f)," +
                " max(rashod_norm),sum(valm),sum(dop87),max(is_device),max(rash_norm_one),max(dlt_reval)" +
                ",sum(rashod_full),3 stek,min(dat_s),max(dat_po)," +
                " sum(cntd),max(cntd_mn),max(is_day_calc), " +
                " case when max(tarif) > 0 then sum(tarif * rashod_source)   / max(tarif) else sum(rashod_source)   end as rashod_source," +
                " max(up_kf) " +
                " From t_calc_gku_tab_xx " +
                " Where stek = 1" +
                " and exists (select 1 from ttt_aid_virt0 a " +
                   " where a.nzp_kvar=t_calc_gku_tab_xx.nzp_kvar " +
                   "   and a.nzp_serv=t_calc_gku_tab_xx.nzp_serv " +
                   "   and a.nzp_supp=t_calc_gku_tab_xx.nzp_supp) " +
                " Group by 1,2,3,4 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            ret = ExecSQL(conn_db,
                " update t_calc_gku_tab_xx set " +
                " rashod_full = " +
                  " (Select " + sNvlWord + "(s.rashod_full,0) from t_calc_gku_tab_xx s" +
                  "  where s.stek=3 and s.nzp_kvar=t_calc_gku_tab_xx.nzp_kvar and s.nzp_serv=t_calc_gku_tab_xx.nzp_serv and s.nzp_supp=t_calc_gku_tab_xx.nzp_supp) " +
                " Where stek = 1" +
                " and exists (select 1 from ttt_aid_virt0 a " +
                   " where a.nzp_kvar=t_calc_gku_tab_xx.nzp_kvar " +
                   "   and a.nzp_serv=t_calc_gku_tab_xx.nzp_serv " +
                   "   and a.nzp_supp=t_calc_gku_tab_xx.nzp_supp) "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            #endregion подготовка таблицы для вставки

            ssql =
                " Insert into " + gku.calc_gku_xx +
                " ( nzp_dom,nzp_kvar,nzp_serv,nzp_supp,nzp_frm,dat_charge," +
                " nzp_frm_typ,gil,gil_g,squ,nzp_prm_tarif,tarif,trf1,trf2,trf3,trf4," +
                " nzp_frm_typrs,nzp_prm_rashod,rashod,rashod_g,rsh1,rsh2,rsh3,nzp_prm_trf_dt,tarif_f," +
                " rashod_norm,valm,dop87,is_device,rash_norm_one,dlt_reval" +
                ",rashod_full,stek,dat_s,dat_po,cntd,cntd_mn,rashod_source,up_kf ) " +
                " Select " +
                " nzp_dom,nzp_kvar,nzp_serv,nzp_supp,nzp_frm,dat_charge," +
                " nzp_frm_typ,gil,gil_g,squ,nzp_prm_tarif,tarif,trf1,trf2,trf3,trf4," +
                " nzp_frm_typrs,nzp_prm_rashod,rashod,rashod_g,rsh1,rsh2,rsh3,nzp_prm_trf_dt,tarif_f," +
                " rashod_norm,valm,dop87,is_device,rash_norm_one,dlt_reval" +
                ",rashod_full,stek,dat_s,dat_po,cntd,cntd_mn,rashod_source,up_kf " +
                " From t_calc_gku_tab_xx " +
                " Where 1=1 ";
            ExecByStep(conn_db, "t_calc_gku_tab_xx", "nzp_clc", ssql, 20000, "", out ret);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            UpdateStatistics(false, gku.paramcalc, gku.calc_gku_tab, out ret);

            #endregion Непосредственно вставка в БД

            #region учет снятия ОДН в содержании жилья в ЕИРЦ г.Самаре - перекидками только для текущего расчетного месяца.
            //
            //----------------------------------------------------------------
            //
            // учет снятия ОДН в содержании жилья в ЕИРЦ г.Самаре - перекидками только для текущего расчетного месяца.

            //----------------------------------------------------------------
            // проводится расчет 1-го л/с
            //----------------------------------------------------------------
            bool b_calc_kvar = (gku.paramcalc.nzp_kvar > 0);

            if (Points.IsSmr && (!b_calc_kvar) && ((new DateTime(gku.paramcalc.calc_yy, gku.paramcalc.calc_mm, 01)) < (new DateTime(2013, 07, 01))))
            {
                int inzpuser = -100 * gku.paramcalc.calc_yy - gku.paramcalc.calc_mm;
                string stypercl = "1";
                string ssqlfindls = " where 0<(select count(*) from t_mustcalc m where t.nzp_kvar=m.nzp_kvar and m.nzp_serv in (1,17)) ";
                if ((gku.paramcalc.cur_mm == gku.paramcalc.calc_mm)
                    && (gku.paramcalc.cur_yy == gku.paramcalc.calc_yy))
                {
                    stypercl = "63";
                    ssqlfindls = "";
                }
                ret = ExecSQL(conn_db,
                    " Delete from " + gku.perekidka_xx +
                    " Where type_rcl=" + stypercl + " and month_=" + gku.paramcalc.cur_mm.ToString() +
                    " and nzp_serv=17 and nzp_user=" + inzpuser.ToString() +
                    " and 0<(select count(*) from t_opn t where t.nzp_kvar=" + gku.perekidka_xx + ".nzp_kvar) "
                    , true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

                ExecSQL(conn_db, " Drop table t_mustcalc_dop ", false);

                ret = ExecSQL(conn_db,
                    " Create temp table t_mustcalc_dop (nzp_kvar integer) " + sUnlogTempTable
                     , true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

                ret = ExecSQL(conn_db,
                    " Insert into t_mustcalc_dop (nzp_kvar) " +
                    " Select nzp_kvar From t_opn t " + ssqlfindls
                    , true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

                ExecSQL(conn_db, " Create index ix_mustcalc_dop on t_mustcalc_dop (nzp_kvar) ", true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

                ExecSQL(conn_db, sUpdStat + " t_mustcalc_dop ", true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

                ExecSQL(conn_db, " Drop table ttt_aid_virt0 ", false);
                ret = ExecSQL(conn_db,
                    " Create temp table ttt_aid_virt0 (" +
                    " nzp_kvar integer," +
                    " nzp_supp integer," +
                    " sum_rcl  " + sDecimalType + "(14,2) default 0.00 " +
                    " ) " + sUnlogTempTable
                    , true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

                ret = ExecSQL(conn_db,
                    " Insert into ttt_aid_virt0 (nzp_kvar,nzp_supp)" +
                    " Select nzp_kvar,nzp_supp" +
                    " From " + gku.calc_gku_xx + " t" +
                    " Where nzp_serv = 17 and tarif * rashod > 0.0001 " +
                    "   and 0<(select count(*) from t_mustcalc_dop m where t.nzp_kvar=m.nzp_kvar) " +
                    " Group by 1,2 "
                    , true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

                ret = ExecSQL(conn_db, " Create index ixttt_aid_virt0 on ttt_aid_virt0(nzp_kvar) ", true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

                ret = ExecSQL(conn_db, sUpdStat + " ttt_aid_virt0 ", true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

                ExecSQL(conn_db, " Drop table ttt_aid_virt1 ", false);
                ret = ExecSQL(conn_db,
                    " Create temp table ttt_aid_virt1 (" +
                    " nzp_dom  integer," +
                    " nzp_kvar integer," +
                    " tarif    " + sDecimalType + "(14,4) default 0.00," +
                    " squ      " + sDecimalType + "(14,7) default 0.00," +
                    " koef_norm_odn " + sDecimalType + "(14,7) default 0.00," +
                    " sum_odn  " + sDecimalType + "(14,2) default 0.00 " +
                    " ) " + sUnlogTempTable
                    , true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

                ret = ExecSQL(conn_db,
                    " Insert into ttt_aid_virt1 (nzp_dom,nzp_kvar,tarif,squ,sum_odn)" +
                    " Select nzp_dom,nzp_kvar,max(tarif),max(squ),sum(tarif * rashod)" +
                    " From " + gku.calc_gku_xx + " t" +
                    " Where nzp_serv = 515 and tarif * rashod > 0.0001 " +
                    "   and 0<(select count(*) from t_mustcalc_dop m where t.nzp_kvar=m.nzp_kvar) " +
                    " Group by 1,2 "
                    , true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

                // проставить норматив ОДН на 1 кв.метр
                ret = ExecSQL(conn_db,
                    " Update ttt_aid_virt1 set koef_norm_odn=" +
                    " (Select max(gl7kw)" +
                    " From " + gku.counters_xx + " s" +
                    " Where ttt_aid_virt1.nzp_dom=s.nzp_dom and s.stek = 354 and s.nzp_serv = 25 and s.gl7kw > 0.000001) " +
                    " Where 1=1 "
                    , true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

                ret = ExecSQL(conn_db,
                    " Update ttt_aid_virt1 set koef_norm_odn = 0" +
                    " Where koef_norm_odn is null "
                    , true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

                // обрезать перекидку нормативным расходом ОДН
                ret = ExecSQL(conn_db,
                    " Update ttt_aid_virt1 set sum_odn =" +
                    "  case when sum_odn<=squ*koef_norm_odn*tarif then sum_odn else squ*koef_norm_odn*tarif end" +
                    " Where koef_norm_odn > 0.000001 "
                    , true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

                ret = ExecSQL(conn_db, " create index ixttt_aid_virt1 on ttt_aid_virt1(nzp_kvar) ", true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

                ExecSQL(conn_db, sUpdStat + " ttt_aid_virt1 ", true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

                ret = ExecSQL(conn_db,
                    " Insert into " + gku.perekidka_xx +
                    " (nzp_kvar,num_ls,nzp_serv,nzp_supp,type_rcl,date_rcl,sum_rcl,month_,comment,nzp_user)" +
                    " Select " +
                    "  t.nzp_kvar, t.num_ls, 17, a.nzp_supp, " + stypercl + "," + sCurDate + ", (-1)*b.sum_odn," +
                    gku.paramcalc.cur_mm + ",'Снятие ОДН по электроснабжению'," + inzpuser +
                    " From t_opn t,ttt_aid_virt0 a,ttt_aid_virt1 b" +
                    " Where t.nzp_kvar=a.nzp_kvar and t.nzp_kvar=b.nzp_kvar "
                    , true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

                ExecSQL(conn_db, " Drop table t_mustcalc_dop ", false);
            }
            #endregion учет снятия ОДН в содержании жилья в ЕИРЦ г.Самаре - перекидками только для текущего расчетного месяца.

            #region Очистить временные таблицы , расчет окончен положительно

            CalcGkuXX_CloseTmp(conn_db);

            #endregion Очистить временные таблицы , расчет окончен положительно

            return true;
        }

        /// <summary>
        /// корректируем расход ОДН в основной услуге
        /// </summary>
        /// <param name="conn_db"></param>
        /// <returns></returns>
        private Returns CorrectOdnInMainServ(IDbConnection conn_db)
        {
            string ssql;
            var ret = Utils.InitReturns();
            var tableSumOdnAndOdnPk = "t_sum_odn_pk_" + DateTime.Now.Ticks;
            var tableOdnAndOdnPk = "t_odn_pk_" + DateTime.Now.Ticks;
            try
            {
                //достаем расходы по ОДН ПК
                ssql = string.Format(" CREATE TEMP TABLE {0} AS " +
                                     " SELECT t.stek,t.nzp_kvar, t.nzp_serv, t.nzp_supp,t.nzp_period,o.nzp_serv_link, t.rashod" +
                                     " FROM {1}serv_odn o, {1}serv_norm_koef pk,t_calc_gku_tab_xx t " +
                                     " WHERE o.nzp_serv=pk.nzp_serv_link AND pk.nzp_serv=t.nzp_serv",
                    tableOdnAndOdnPk, Points.Pref + sKernelAliasRest);
                ret = ExecSQL(conn_db, ssql, true);
                if (!ret.result) return ret;


                //достаем расходы по ОДН ПК
                ssql = string.Format(" INSERT INTO  {0} (stek,nzp_kvar, nzp_serv, nzp_supp,nzp_period,nzp_serv_link, rashod) " +
                                     " SELECT t.stek,t.nzp_kvar, t.nzp_serv, t.nzp_supp, t.nzp_period, o.nzp_serv_link, t.rashod" +
                                     " FROM {1}serv_odn o, {1}serv_norm_koef pk,t_calc_gku_tab_xx t " +
                                     " WHERE o.nzp_serv=pk.nzp_serv_link AND o.nzp_serv=t.nzp_serv",
                    tableOdnAndOdnPk, Points.Pref + sKernelAliasRest);
                ret = ExecSQL(conn_db, ssql, true);
                if (!ret.result) return ret;

                ret = ExecSQL(conn_db,
                    " CREATE index ix1_" + tableOdnAndOdnPk + " on " + tableOdnAndOdnPk +
                    " (stek,nzp_kvar,nzp_serv_link,nzp_supp, nzp_period)", true);
                if (!ret.result) return ret;

                ssql = " CREATE TEMP TABLE " + tableSumOdnAndOdnPk + " AS " +
                       " SELECT t.stek,t.nzp_kvar, t.nzp_serv_link as nzp_serv, t.nzp_supp, t.nzp_period, SUM(t.rashod) as dop87" +
                       " FROM " + tableOdnAndOdnPk + " t" +
                       " GROUP BY 1,2,3,4,5";
                ret = ExecSQL(conn_db, ssql, true);
                if (!ret.result) return ret;

                ret = ExecSQL(conn_db,
                    " CREATE UNIQUE index ix1_" + tableSumOdnAndOdnPk + " on " + tableSumOdnAndOdnPk +
                    " (stek,nzp_kvar,nzp_serv,nzp_supp, nzp_period)", true);
                if (!ret.result) return ret;

                //корректируем расход ОДН в базовой услуге
                ssql = " UPDATE t_calc_gku_tab_xx t" +
                       " SET dop87=s.dop87" +
                       " FROM " + tableSumOdnAndOdnPk + " s" +
                       " WHERE s.stek=t.stek " +
                       " AND s.nzp_kvar=t.nzp_kvar " +
                       " AND s.nzp_serv=t.nzp_serv " +
                       " AND s.nzp_supp=t.nzp_supp" +
                       " AND s.nzp_period=t.nzp_period ";
                ret = ExecSQL(conn_db, ssql, true);
                if (!ret.result) return ret;
            }
            finally
            {
                ExecSQL(conn_db, " Drop table  " + tableOdnAndOdnPk, false);
                ExecSQL(conn_db, " Drop table  " + tableSumOdnAndOdnPk, false);
            }
            return ret;
        }

        #endregion Непосредственно подготовка расходов для расчета (Главная функция)


        #region Удалить временные таблицы
        //-----------------------------------------------------------------------------
        void CalcGkuXX_CloseTmp(IDbConnection conn_db)
        //-----------------------------------------------------------------------------
        {
            //ExecSQL(conn_db, " Drop table t_opn ", false);
            ExecSQL(conn_db, " Drop table t_rsh_kmnl ", false);
            ExecSQL(conn_db, " Drop table t_rsh_kolgil ", false);
            ExecSQL(conn_db, " Drop table t_rsh_naem ", false);
            ExecSQL(conn_db, " Drop table t_tarifs_uni ", false);
            ExecSQL(conn_db, " Drop table t_tarifs ", false);
            ExecSQL(conn_db, " Drop table t_trf ", false);
            ExecSQL(conn_db, " Drop table t_prm ", false);
            ExecSQL(conn_db, " Drop table t_prm_max ", false);
            ExecSQL(conn_db, " Drop table t_rsh ", false);
            ExecSQL(conn_db, " Drop table t_rsh8 ", false);
            ExecSQL(conn_db, " Drop table t_par1 ", false);
            ExecSQL(conn_db, " Drop table t_par2 ", false);
            ExecSQL(conn_db, " Drop table t_par1d ", false);
            ExecSQL(conn_db, " Drop table t_par335 ", false);
            ExecSQL(conn_db, " Drop table t_par26f ", false);
            ExecSQL(conn_db, " Drop table t_par40f ", false);
            ExecSQL(conn_db, " Drop table t_par894 ", false);
            ExecSQL(conn_db, " Drop table t_kmnl ", false);
            ExecSQL(conn_db, " Drop table t_prm_calc ", false);
            ExecSQL(conn_db, " Drop table t_rsh390 ", false);
            ExecSQL(conn_db, " Drop table t_rsh390_hv ", false);
            ExecSQL(conn_db, " Drop table t_rsh390_gv ", false);
            ExecSQL(conn_db, " Drop table t_rsh_lift ", false);
            ExecSQL(conn_db, " Drop table t_par412 ", false);
            ExecSQL(conn_db, " Drop table t_prm400 ", false);
            ExecSQL(conn_db, " Drop table ttt_aid_virt0 ", false);
            ExecSQL(conn_db, " Drop table ttt_aid_virt1 ", false);
            ExecSQL(conn_db, " Drop table t_perc_pt ", false);
            ExecSQL(conn_db, " Drop table t_perc_pt_max ", false);
            ExecSQL(conn_db, " Drop table t_kg_m2 ", false);
            ExecSQL(conn_db, " Drop table t_rsh_privat ", false);
            ExecSQL(conn_db, " Drop table t_rsh339 ", false);
            ExecSQL(conn_db, " Drop table t_rsh1814 ", false);
            ExecSQL(conn_db, " Drop table t_rsh814 ", false);
            ExecSQL(conn_db, " Drop table t_opn_dom ", false);
            ExecSQL(conn_db, " Drop table t_calc_gku_tab_xx ", false);
        }
        #endregion Удалить временные таблицы

        #region Генерация средних значений счетчиков
        //-----------------------------------------------------------------------------
        public void Call_GenSrZnKPU(Ls finder, int p_year, int p_month, out Returns ret)
        //-----------------------------------------------------------------------------
        {
            int nzp_user = finder.nzp_user;
            #region Получить данные по соединению

            ret = Utils.InitReturns();

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) { return; }
            string sNameWebDb = DBManager.GetFullBaseName(conn_web);
            /*conn_db.Close();*/

            string conn_kernel = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) { return; }
            #endregion Получить данные по соединению

            #region Выполнить расчет средних значений ИПУ
            string sNmTblLs = sNameWebDb + tableDelimiter + "t" + nzp_user + "_spls";
            Call_GenSrZnKPUWithOutConnect(conn_db, nzp_user, p_year, p_month, sNmTblLs, out ret, finder.nzp_kvar);

            /*conn_db.Close();*/

            #endregion Выполнить расчет средних значений ИПУ
        }

        #region Сгенерировать средние значения счетчиков по группе
        //-----------------------------------------------------------------------------
        public void Call_GenSrZnKPUWithOutConnect(IDbConnection conn_db, int nzp_user, int p_year, int p_month, string pNmTblLs, out Returns ret, int nzp_kvar = 0)
        //-----------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            #region выбрать мно-во лицевых счетов

            ExecSQL(conn_db, " Drop table t_ls_gen ", false);
            ret = ExecSQL(conn_db,
                " Create temp table t_ls_gen " +
                " (  nzp_gen   serial  not null, " +
                "    nzp_dom   integer not null, " +
                "    nzp_kvar  integer not null, " +
                "    pref      char(20) " +
                " ) " +
#if PG
 " "
#else
                "with no log "
#endif
, true);
            if (!ret.result) { /*conn_db.Close();*/ return; }

#if PG
            string serial_fld = "";
            string serial_val = "";
#else
            string serial_fld = ",nzp_gen";
            string serial_val = ",0";
#endif
            string whereNzpKvar = nzp_kvar > 0 ? " AND nzp_kvar = " + nzp_kvar : "";
            ret = ExecSQL(conn_db,
                " Insert Into t_ls_gen (nzp_dom,nzp_kvar,pref" + serial_fld + ")" +
                " Select nzp_dom,nzp_kvar,pref" + serial_val +
                " From " + pNmTblLs +
                " Where mark=1 " + whereNzpKvar + " group by 1,2,3 "
                , true);
            if (!ret.result) { /*conn_db.Close();*/ return; }

            ret = ExecSQL(conn_db, " create unique index ix1_t_ls_gen on t_ls_gen (nzp_gen) ", true);
            if (!ret.result) { /*conn_db.Close();*/ return; }
            ExecSQL(conn_db, sUpdStat + " t_ls_gen ", true);

            #endregion выбрать мно-во лицевых счетов

            #region Выполнить расчет

            // 61,"0Генерация средних расходов КПУ - установлен расход"
            // 62,"0Генерация средних расходов КПУ - нет показаний для генерации"
            // 63,"0Генерация средних расходов КПУ - есть показание КПУ"
            // 64,"0Генерация средних расходов КПУ - нет КПУ"

            // ... есть ЛС для генерации средних?
            bool bIsLsToGen = true;
            IDbCommand cmdFindCntLs = DBManager.newDbCommand(" select count(*) cnt from t_ls_gen ", conn_db);

            try
            {
                string scntvals = Convert.ToString(cmdFindCntLs.ExecuteScalar());
                bIsLsToGen = (Convert.ToInt32(scntvals) > 0);
            }
            catch
            {
                bIsLsToGen = true;
            }
            // ...
            if (bIsLsToGen)
            {
                GenSrZnKPU(conn_db, p_year, p_month, out ret);
                if (!ret.result) { return; }
            }

            #endregion Выполнить расчет

            #region Удалить временные таблицы и закрыть соединение
            ExecSQL(conn_db, " Drop table t_ls_gen ", false);
            #endregion Удалить временные таблицы
        }
        #endregion Сгенерировать средние значения счетчиков по группе

        #region Генерация средних значений счетчиков КПУ(квартирные)
        //-----------------------------------------------------------------------------
        void GenSrZnKPU(IDbConnection conn_db, int p_year, int p_month, out Returns ret)
        //-----------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();


            #region Определить лицевые счета для генерации
            IDataReader reader;
            ret = ExecRead(conn_db, out reader, " Select pref,count(*) cnt From t_ls_gen Group by 1 Order by 1 ", true);
            if (!ret.result) { /*conn_db.Close();*/ return; }

            #endregion Определить лицевые счета для генерации

            #region Определить префиксы по которым нужно будет пройти положить в структуру sCurPref
            List<string> sPrefs = new List<string>();

            while (reader.Read())
            {
                if (reader["cnt"] != DBNull.Value)
                {
                    string sCurPref = Convert.ToString(reader["pref"]).Trim();
                    if (sCurPref.Trim() != "")
                    {
                        sPrefs.Add(sCurPref);
                    }
                }
            }

            reader.Close();
            #endregion Определить префиксы по которым нужно будет пройти
            //
            #region определить начальную дату и конечную  расчета средних по входящему месяцу и году
            DateTime dDtBeg = new DateTime(p_year, p_month, 01);
            DateTime dDtEnd = new DateTime(p_year, p_month, DateTime.DaysInMonth(p_year, p_month));
            DateTime dDtNext = dDtBeg.AddMonths(1);

            
            #endregion определить начальную дату и конечную  расчета средних по входящему месяцу и году

            #region Расчет средних для каждого префикса базы данных
            foreach (string sCurPref in sPrefs)
            {


                #region Взять настройки дату старта расчета средних
                DateTime dDtUseCnt = new DateTime(p_year, p_month, 01);

                string sDataAlias = sCurPref.Trim() + "_data" + tableDelimiter;
                string sKernelAlias = sCurPref.Trim() + "_kernel" + tableDelimiter;

                IDbCommand cmdFindCntLs = DBManager.newDbCommand(
                    " select max(val_prm) from " + sDataAlias + "prm_5" +
                    " where nzp_prm=1024 and is_actual<>100" +
                    " and dat_s <=" + MDY(p_month, 28, p_year) +
                    " and dat_po>=" + MDY(p_month, 1, p_year)
                    , conn_db);

                try
                {
                    dDtUseCnt = Convert.ToDateTime(cmdFindCntLs.ExecuteScalar());
                }
                catch
                {
                    dDtUseCnt = Convert.ToDateTime("01.01.2000");
                }

                DateTime dDtUseCntTmp = new DateTime(p_year, p_month, 01);
                int iCntMn = 0;
                cmdFindCntLs = DBManager.newDbCommand(
                    " select max(val_prm) from " + sDataAlias + "prm_5" +
                    " where nzp_prm=1175 and is_actual<>100" +
                    " and dat_s <=" + MDY(p_month, 28, p_year) +
                    " and dat_po>=" + MDY(p_month, 1, p_year)
                    , conn_db);

                try
                {
                    iCntMn = Convert.ToInt16(cmdFindCntLs.ExecuteScalar());
                }
                catch
                {
                    iCntMn = 0;
                }
                #endregion Взять настройки дату старта расчета средних

                #region с 344 постановления учитываем 6 месяцев
                //if ((p_month>=6)&&(p_year>=2013) ) 
                //{
                //    iCntMn =6-1;
                //} 
                #endregion с 344 постановления учитываем 6 месяцев


                #region Количество анализируемых месяцев
                if (iCntMn > 0)
                {
                    dDtUseCntTmp = dDtUseCntTmp.AddMonths((-1) * iCntMn);
                    if (dDtUseCnt <= dDtUseCntTmp)
                    {
                        dDtUseCnt = dDtUseCntTmp;
                    }

                }

                #endregion Количество анализируемых месяцев

                #region Создать таблицу расходов
                ExecSQL(conn_db, " Drop table t_doit ", false);
#if PG
                ret = ExecSQL(conn_db,
                   " Create temp table t_doit " +
                    " (  nzp_dom  integer not null, " +
                    "    nzp_kvar integer not null, " +
                    "    nzp_counter integer not null, " +
                    "    nzp_cnttype integer not null, " +
                    "    cnt_stage   integer default 10, " +
                    "    mmnog       numeric(14,7) default 1, " +
                    "    count_days  integer not null, " +
                    "    dn_uchet    date default '01.01.1900', " +
                    "    vn_cnt      numeric(18,7), " +
                    "    dk_uchet    date default '01.01.1900', " +
                    "    vk_cnt      numeric(18,7), " +
                    "    cnt_days_rs integer default 1, " +
                    "    rashod      numeric(18,7), " +
                    "    gen_rash_pu numeric(18,7), " +
                    "    kol_mes     integer, " +   // количество месяцев не важно за какой период , потому что сданное и учтенное показание после двух лет все равно может участвовать в среднем 
                    "    kol_mes_sr  integer, " +   // количество месяцев сколько уже было среднее (его могло не быть ), считать от даты послед показания независимо от prm_17
                    "    saldo_date  date, " +     // сальдовый месяц расчета  
                    // 62,"0Генерация средних расходов КПУ - нет показаний для генерации"
                    "    nzp_group   integer default 62 " +
                    "    ,nzp_serv   integer " +   // Для учета среднего если есть более 1 счетчика на услугу (и по одному счетчику среднее равно 0 )
                    "    ,kol_count  integer " +   // Количество счетчиков со средним  для услуги, Для учета среднего если есть более 1 счетчика на услугу (и по одному счетчику среднее равно 0 )
                    " ) "
                          , true);
#else
                ret = ExecSQL(conn_db,
                    //  " Create table are.t_doit " +
                       " Create temp table t_doit " +
                        " (  nzp_dom  integer not null, " +
                        "    nzp_kvar integer not null, " +
                        "    nzp_counter integer not null, " +
                        "    nzp_cnttype integer not null, " +
                        "    cnt_stage   integer default 10, " +
                        "    mmnog       decimal(14,7) default 1, " +
                        "    count_days  integer not null, " +
                        "    dn_uchet    date default '01.01.1900', " +
                        "    vn_cnt      decimal(18,7), " +
                        "    dk_uchet    date default '01.01.1900', " +
                        "    vk_cnt      decimal(18,7), " +
                        "    cnt_days_rs integer default 1, " +
                        "    rashod      decimal(18,7), " +
                        "    gen_rash_pu decimal(18,7), " +
                        "    kol_mes     integer, "+   // количество месяцев не важно за какой период , потому что сданное и учтенное показание после двух лет все равно может участвовать в среднем 
                        "    kol_mes_sr  integer, "+   // количество месяцев сколько уже было среднее (его могло не быть ), считать от даты послед показания независимо от prm_17
                        "    saldo_date  date, " +     // сальдовый месяц расчета  
                        // 62,"0Генерация средних расходов КПУ - нет показаний для генерации"
                        "    nzp_group   integer default 62 " +
                        "    ,nzp_serv   integer " +   // Для учета среднего если есть более 1 счетчика на услугу (и по одному счетчику среднее равно 0 )
                        "    ,kol_count  integer " +   // Количество счетчиков со средним  для услуги, Для учета среднего если есть более 1 счетчика на услугу (и по одному счетчику среднее равно 0 )
                     //  " ) "
                        " ) with no log "
                              , true);
#endif
                if (!ret.result) { /*conn_db.Close();*/ return; }
                #endregion Создать таблицу расходов

                #region Выбрать показания по группе для индивидуальных приборов учета

               
                DateTime curMonth = Points.GetCalcMonth(new CalcMonthParams(sCurPref.Trim())).RecordDateTime;

                //условие на генерацию среднего: по-умолчанию для закрытых ИПУ среднее не считаем
                var whereDateClose = string.Format(@" AND {0}(s.dat_close, {1}) >= {2}
                                        AND NOT EXISTS(SELECT 1 FROM {3}counters c 
                                        WHERE c.nzp_counter = s.nzp_counter AND c.is_actual = 1 AND {0}(c.dat_close, {1}) < {2})",
                    sNvlWord, MDY(1, 1, 3000), MDY(dDtBeg.Month, dDtBeg.Day, dDtBeg.Year), sDataAlias);

                //включаем генерацию среднего по закрытым ИПУ
                var enableGencAvgForClosedPu = DBManager.GetParamValueInCurrentMonth<bool>(conn_db, sCurPref,
                    1976, 10, out ret);
                if (!ret.result) return;
                if (enableGencAvgForClosedPu)
                {
                    //при отсутствии параметра проставляем по умолчанию 3 месяца
                    var countMonthsWithAvg = 3;
                    // nzp_prm=1448 -	Количество месяцев учета среднего после выхода из строя ПУ
                    var countMonthsWithAvgObj = ExecScalar(conn_db, string.Format("SELECT max(val_prm) FROM {0}{1}prm_10 WHERE nzp_prm=1448 AND is_actual<>100 AND " +
                        " '{2}'" + " BETWEEN dat_s and dat_po", sCurPref, sDataAliasRest,
                        curMonth), out ret, true);
                    if (countMonthsWithAvgObj != DBNull.Value && countMonthsWithAvgObj != null)
                    {
                        countMonthsWithAvg = Convert.ToInt32(countMonthsWithAvgObj);
                    }
                    //включаем генерацию среднего по закрытым ИПУ, ограничиваясь количеством месяцев учета среднего после выхода из строя ПУ
                    whereDateClose = string.Format(@" AND ({0}(s.dat_close, {1})::DATE + interval '{3} month') >={2} ",
                        sNvlWord, MDY(1, 1, 3000), MDY(dDtBeg.Month, dDtBeg.Day, dDtBeg.Year), countMonthsWithAvg);
                }

                //!!!! таблица ttt_prm_17 формируется только при полном расчете, а при отдельном вызове расчета ср расх ИПУ нет!
                ret = ExecSQL(conn_db, string.Format(@"
                INSERT INTO t_doit (nzp_dom, nzp_kvar, nzp_counter, nzp_cnttype, count_days, saldo_date, nzp_serv)
                SELECT t.nzp_dom, t.nzp_kvar, s.nzp_counter, s.nzp_cnttype, {0}, {1}, s.nzp_serv 
                FROM t_ls_gen t
                     INNER JOIN {2}counters_spis s ON t.nzp_kvar = s.nzp
                WHERE t.pref = '{3}' AND s.nzp_type = 3 AND s.is_actual != 100 {6}
                      AND NOT EXISTS(SELECT 1 FROM {2}prm_17 p 
                                    WHERE s.nzp_counter=p.nzp AND p.is_actual=1 AND p.nzp_prm=1463 AND p.val_prm='1'
                                    AND dat_s <= {5} AND dat_po >= {4})  ", //учитываем период запрета генерации среднего для ПУ
                    DateTime.DaysInMonth(p_year, p_month), MDY(p_month, 28, p_year), sDataAlias, sCurPref.Trim(),
                    MDY(curMonth.Month, 1, curMonth.Year), MDY(curMonth.Month, DateTime.DaysInMonth(curMonth.Year, curMonth.Month), curMonth.Year),whereDateClose)
                    , true);

                if (!ret.result) { /*conn_db.Close();*/ return; }

                ret = ExecSQL(conn_db, " create index ix1_t_doit on t_doit (nzp_counter) ", true);
                if (!ret.result) { /*conn_db.Close();*/ return; }
                ret = ExecSQL(conn_db, " create index ix2_t_doit on t_doit (nzp_cnttype) ", true);
                if (!ret.result) { /*conn_db.Close();*/ return; }
                ret = ExecSQL(conn_db, " create index ix3_t_doit on t_doit (nzp_serv, nzp_kvar) ", true);
                if (!ret.result) { /*conn_db.Close();*/ return; }
                ExecSQL(conn_db, sUpdStat + " t_doit ", true);
                //dt1 = ViewTbl(conn_db, "select * from t_doit ");
                #endregion Выбрать показания по группе для индивидуальных приборов учета

                #region Скинуть в архив предыдущие расчитанные средние для текущего месяца

                
                ret = ExecSQL(conn_db, string.Format(@"
                        UPDATE {0}prm_17 p
                        SET is_actual = 100
                        WHERE nzp_prm = 979 and is_actual=1 and dat_s >= {1}
                        AND (EXISTS (SELECT 1 FROM t_doit c WHERE c.nzp_counter = p.nzp)
                             OR EXISTS (SELECT 1 
                   FROM {0}counters_spis cs 
                        INNER JOIN t_ls_gen t ON t.pref = '{2}' AND t.nzp_kvar = cs.nzp
                             WHERE cs.nzp_type = 3 and cs.is_actual != 100 AND cs.nzp_counter = p.nzp AND cs.dat_close < {1}))
                        AND NOT EXISTS(SELECT 1 FROM {0}prm_17 pp 
                                    WHERE p.nzp=pp.nzp AND pp.is_actual=1 AND pp.nzp_prm=1463 AND pp.val_prm='1'
                                    AND pp.dat_s <= {4} AND pp.dat_po >= {3})  ", //учитываем период запрета генерации среднего для ПУ,
                    sDataAlias, MDY(dDtBeg.Month, dDtBeg.Day, dDtBeg.Year), sCurPref.Trim(),
                    MDY(curMonth.Month, 1, curMonth.Year), MDY(curMonth.Month, DateTime.DaysInMonth(curMonth.Year, curMonth.Month), curMonth.Year)), true);

                if (!ret.result) { /*conn_db.Close();*/ return; }

                #endregion Скинуть в архив предыдущие расчитанные средние


                #region Обрезать дату действия среднего началом расчитываемого периода dDtBeg.
                ret = ExecSQL(conn_db,
                    " Update " + sDataAlias + "prm_17 Set dat_po=date('" + dDtBeg.ToShortDateString() + "') " +
                    " Where nzp_prm=979 and is_actual<>100 and dat_s <'" + dDtBeg.ToShortDateString() + "' and dat_po>'" + dDtBeg.ToShortDateString() + "' " +
                    " and EXISTS(select 1 from t_doit c where c.nzp_counter=" + sDataAlias + "prm_17.nzp) "
                    , true);
                if (!ret.result) { /*conn_db.Close();*/ return; }

                #endregion Обрезать дату действия среднего началом расчитываемого периода dDtBeg.

                #region Выбрать максимальную дату учета в пределах действия параметра начала учета средних
                ret = ExecSQL(conn_db,
                    " Update t_doit Set dk_uchet =" +
                    " (Select max(c.dat_uchet) From " + sDataAlias + "counters c" +
                     " Where c.nzp_counter=t_doit.nzp_counter and c.is_actual<>100 and c.dat_uchet>='" + dDtUseCnt.ToShortDateString() + "' and " + sNvlWord + "(c.ist,0) <> 7 ) " +
                    " Where EXISTS(Select 1 From " + sDataAlias + "counters c" +
                     " Where c.nzp_counter=t_doit.nzp_counter and c.is_actual<>100 and c.dat_uchet>='" + dDtUseCnt.ToShortDateString() + "' and " + sNvlWord + "(c.ist,0) <> 7 ) "
                    , true);
                if (!ret.result) { /*conn_db.Close();*/ return; }

                #endregion Выбрать максимальную дату учета в пределах действия параметра начала учета средних




                #region Выбрать минимальную дату учета в пределах действия параметра начала учета средних
                //dt1 = ViewTbl(conn_db, "select * from t_doit ");
                #region ошибка информикса на 31 число вычитание месяцев дает лажу, поэтому вычтем 1 день , все равно потом сдвигать дату не на первое число
#if PG
                ret = ExecSQL(conn_db,
                    " Update t_doit Set dk_uchet =dk_uchet - interval '1 day' where EXTRACT(day FROM dk_uchet)=31"
                    , true);

#else
                    // исправление ошибки информикса
                    ret = ExecSQL(conn_db,
                        " Update t_doit Set dk_uchet =dk_uchet -1 units day where day(dk_uchet)=31"
                        , true);
#endif
                if (!ret.result) { /*conn_db.Close();*/ return; }
                //                    dt1 = ViewTbl(conn_db, "select * from t_doit ");
                #endregion ошибка информикса на 31 число вычитание месяцев дает лажу
#if PG
                string sDopSql = "interval '6 month'";
#else
                    string sDopSql = "6 units month";
#endif
                ret = ExecSQL(conn_db,
                    " Update t_doit Set dn_uchet =" +
                    " (Select min(c.dat_uchet) From " + sDataAlias + "counters c" +
                    " Where c.nzp_counter=t_doit.nzp_counter and c.is_actual<>100 and c.dat_uchet>='" + dDtUseCnt.ToShortDateString() + "' and " + sNvlWord + "(c.ist,0) <> 7  " +
                    " and c.dat_uchet>=t_doit.dk_uchet-" + sDopSql + " )" +
                    " Where EXISTS(Select 1 From " + sDataAlias + "counters c" +
                    " Where c.nzp_counter=t_doit.nzp_counter and c.is_actual<>100 and c.dat_uchet>='" + dDtUseCnt.ToShortDateString() + "' and " + sNvlWord + "(c.ist,0) <> 7  " +
                    " and c.dat_uchet>=t_doit.dk_uchet-" + sDopSql + " ) "
                    , true);
                if (!ret.result) { /*conn_db.Close();*/ return; }



                // исправить минимальную дату если не хватает до нормального периода
                //dt1 = ViewTbl(conn_db, "select * from t_doit ");
#if PG
                ret = ExecSQL(conn_db,
                    " Update t_doit Set dn_uchet =" +
                    " coalesce( (Select max(c.dat_uchet) From " + sCurPref.Trim() + "_data.counters c" +
                     " Where c.nzp_counter=t_doit.nzp_counter and c.is_actual<>100  " +
                     " and c.dat_uchet>=t_doit.dk_uchet- interval '35 month'  and t_doit.dn_uchet>c.dat_uchet and coalesce(c.ist,0) <> 7 ), dn_uchet) " +
                    " Where (EXTRACT(month FROM dk_uchet)+12*EXTRACT(year FROM dk_uchet)) -(EXTRACT(month FROM dn_uchet)+12*EXTRACT(year FROM dn_uchet))<6 " +
                    " and 0<(Select count(*) From " + sCurPref.Trim() + "_data.counters c" +
                    " Where c.nzp_counter=t_doit.nzp_counter and c.is_actual<>100 and c.dat_uchet>='" + dDtUseCnt.ToShortDateString() + "' and coalesce(c.ist,0) <> 7  " +
                    " and c.dat_uchet>=t_doit.dk_uchet- interval '6 month' )"
                    , true);


#else
                    // Вот главное отличие , мы стали выбирать данные раньше если есть показание в пределах 6 месяцев
                    // в постгри не надо исправлять , это из ошибки информикса 
                    //ret = ExecSQL(conn_db,
                    //    " Update t_doit Set dn_uchet =" +
                    //    " nvl((Select max(c.dat_uchet) From " + sCurPref.Trim() + "_data:counters c" +
                    //     " Where c.nzp_counter=t_doit.nzp_counter and c.is_actual<>100  " +
                    //     " and c.dat_uchet>=(case when day(t_doit.dk_uchet)>28 then cast((t_doit.dk_uchet-3 units day) as date) else cast(t_doit.dk_uchet as date) end -35 units month) "+
                         
                    //     " and t_doit.dn_uchet>c.dat_uchet  and nvl(c.ist,0) <> 7 ),dn_uchet) " +
                    //    " Where (month(dk_uchet)+12*year(dk_uchet)) -(month(dn_uchet)+12*year(dn_uchet))<3 "+
                    //    " and 0<(Select count(*) From " + sCurPref.Trim() + "_data:counters c" +
                    //    " Where c.nzp_counter=t_doit.nzp_counter and c.is_actual<>100 and c.dat_uchet>='" + dDtUseCnt.ToShortDateString() + "' and nvl(c.ist,0) <> 7  " +
                    //    " and c.dat_uchet>=(t_doit.dk_uchet-3 units day) -6 units month and day(c.dat_uchet)>28 )"
                    //    , true);
                    //if (!ret.result) { /*conn_db.Close();*/ return; }

                    //ret = ExecSQL(conn_db,
                    //    " Update t_doit Set dn_uchet =" +
                    //    " nvl((Select max(c.dat_uchet) From " + sCurPref.Trim() + "_data:counters c" +
                    //    " Where c.nzp_counter=t_doit.nzp_counter and c.is_actual<>100  " +
                    //    " and c.dat_uchet>=(case when day(t_doit.dk_uchet)>28 then cast((t_doit.dk_uchet-3 units day) as date) else cast(t_doit.dk_uchet as date) end -35 units month) " +
                    //    " and t_doit.dn_uchet>c.dat_uchet  and nvl(c.ist,0) <> 7 ),dn_uchet) " +
                    //    " Where (month(dk_uchet)+12*year(dk_uchet)) -(month(dn_uchet)+12*year(dn_uchet))<3 " +
                    //    " and 0<(Select count(*) From " + sCurPref.Trim() + "_data:counters c" +
                    //    " Where c.nzp_counter=t_doit.nzp_counter and c.is_actual<>100 and c.dat_uchet>='" + dDtUseCnt.ToShortDateString() + "' and nvl(c.ist,0) <> 7  " +
                    //    " and c.dat_uchet>=t_doit.dk_uchet -6 units month and day(c.dat_uchet)<=28)"
                    //    , true);

                    ret = ExecSQL(conn_db,
                      " Update t_doit Set dn_uchet =" +
                      " nvl((Select max(c.dat_uchet) From " + sCurPref.Trim() + "_data:counters c" +
                      " Where c.nzp_counter=t_doit.nzp_counter and c.is_actual<>100  " +
                      " and c.dat_uchet>=add_months(t_doit.dk_uchet,-35) " +
                      " and t_doit.dn_uchet>c.dat_uchet  and nvl(c.ist,0) <> 7 ),dn_uchet) " +
                      " Where (month(dk_uchet)+12*year(dk_uchet)) -(month(dn_uchet)+12*year(dn_uchet))<3 " +
                      " and 0<(Select count(*) From " + sCurPref.Trim() + "_data:counters c" +
                      " Where c.nzp_counter=t_doit.nzp_counter and c.is_actual<>100 and c.dat_uchet>='" + dDtUseCnt.ToShortDateString() + "' and nvl(c.ist,0) <> 7  " +
                      " and c.dat_uchet>=add_months(t_doit.dk_uchet,-6))"
                      , true);

#endif
                if (!ret.result) { /*conn_db.Close();*/ return; }

                //dt1 = ViewTbl(conn_db, "select * from t_doit ");
                #endregion Выбрать минимальную дату учета в пределах действия параметра начала учета средних



                #region выбрать начальное и конечное показание
                ret = ExecSQL(conn_db,
                    " Update t_doit Set vn_cnt =" +
                    " (Select max(c.val_cnt) From " + sDataAlias + "counters c" +
                     " Where c.nzp_counter=t_doit.nzp_counter and c.is_actual<>100 and c.dat_uchet=t_doit.dn_uchet and t_doit.dn_uchet>'01.01.1900' and " + sNvlWord + "(c.ist,0) <> 7 ) " +
                    " Where EXISTS(Select 1 From " + sDataAlias + "counters c" +
                     " Where c.nzp_counter=t_doit.nzp_counter and c.is_actual<>100 and c.dat_uchet=t_doit.dn_uchet and t_doit.dn_uchet>'01.01.1900' and " + sNvlWord + "(c.ist,0) <> 7 ) "
                    , true);
                if (!ret.result) { /*conn_db.Close();*/ return; }
                //dt1 = ViewTbl(conn_db, "select * from t_doit ");
                ret = ExecSQL(conn_db,
                    " Update t_doit Set vk_cnt =" +
                    " (Select max(c.val_cnt)   From " + sDataAlias + "counters c" +
                     " Where c.nzp_counter=t_doit.nzp_counter and c.is_actual<>100 and c.dat_uchet=t_doit.dk_uchet and t_doit.dk_uchet>'01.01.1900' and " + sNvlWord + "(c.ist,0) <> 7 ) " +
                    " Where EXISTS(Select 1 From " + sDataAlias + "counters c" +
                     " Where c.nzp_counter=t_doit.nzp_counter and c.is_actual<>100 and c.dat_uchet=t_doit.dk_uchet and t_doit.dk_uchet>'01.01.1900' and " + sNvlWord + "(c.ist,0) <> 7 ) "
                    , true);
                if (!ret.result) { /*conn_db.Close();*/ return; }
                #endregion выбрать начальное и конечное показание

                #region Выставить группу 63,"0Генерация средних расходов КПУ - есть показание КПУ"
                ret = ExecSQL(conn_db,
                    " Update t_doit Set nzp_group = 63 " +
                    " Where dn_uchet>'01.01.1900' and dn_uchet<= '" + dDtBeg.ToShortDateString() + "' and dk_uchet>= '" + dDtNext.ToShortDateString() + "' "
                    , true);
                if (!ret.result) { /*conn_db.Close();*/ return; }
                #endregion 63,"0Генерация средних расходов КПУ - есть показание КПУ"
                //dt1 = ViewTbl(conn_db, "select * from t_doit ");
                #region Выставить группу 61 или 65 (искл гр 63), подсчитать количество дней, "0Генерация средних расходов КПУ - установлен расход"

                ret = ExecSQL(conn_db,
                    " Update t_doit Set nzp_group = (case when abs(vk_cnt-vn_cnt)>0 then 61 else 65 end), cnt_days_rs = (dk_uchet - dn_uchet)" + sConvToInt +
                    " Where dn_uchet>'01.01.1900' and dk_uchet>'01.01.1900' and dk_uchet>dn_uchet and nzp_group <> 63 "
                    , true);
                if (!ret.result) { /*conn_db.Close();*/ return; }

                #endregion Выставить группу 61 (искл гр 63),"0Генерация средних расходов КПУ - установлен расход"
                //dt1 = ViewTbl(conn_db, "select * from t_doit ");
                #region Определить разрядность счетчика и коэф трансф
                ret = ExecSQL(conn_db,
                    " Update t_doit Set " +
                    " cnt_stage=(Select cnt_stage From " + sKernelAlias + "s_counttypes t Where t_doit.nzp_cnttype=t.nzp_cnttype)," +
                    " mmnog =(Select " + sNvlWord + "(formula,'1')" + sConvToNum + " From " + sKernelAlias + "s_counttypes t Where t_doit.nzp_cnttype=t.nzp_cnttype) " +
                    " Where EXISTS(Select 1 From " + sKernelAlias + "s_counttypes t Where t_doit.nzp_cnttype=t.nzp_cnttype) "
                    , true);
                if (!ret.result) { /*conn_db.Close();*/ return; }
                #endregion Определить разрядность счетчика и коэф трансф

                #region Расчитать расход с учетом коэф трансф и перехода через 0
                ret = ExecSQL(conn_db,
                    " Update t_doit Set rashod = " +
                      " case when" +
                      " (case when (vk_cnt-vn_cnt)>-0.0001 then (vk_cnt-vn_cnt)*mmnog else (pow(10,cnt_stage)+vk_cnt-vn_cnt)*mmnog end) > 1000000 " +
                      " then 1000000 " +
                      " else (case when (vk_cnt-vn_cnt)>-0.0001 then (vk_cnt-vn_cnt)*mmnog else (pow(10,cnt_stage)+vk_cnt-vn_cnt)*mmnog end) " +
                      " end " +
                      " Where nzp_group in (61,65) "
                    , true);
                if (!ret.result) { /*conn_db.Close();*/ return; }



                ExecSQL(conn_db, "DROP TABLE t_max_val", false);
                //ограничение расходов по ИПУ
                var sql = " CREATE TEMP TABLE t_max_val AS " +
                          " SELECT nzp_prm, MAX(val_prm)::numeric as ipu_limit," +
                          " CASE " +
                          "     WHEN nzp_prm=1457 THEN 0 " +
                          "     WHEN nzp_prm=2081 THEN 25" +
                          "     WHEN nzp_prm=2082 THEN 9" +
                          "     WHEN nzp_prm=2083 THEN 6" +
                          "     WHEN nzp_prm=2084 THEN 10" +
                          " END as nzp_serv" +
                          " FROM " + sDataAlias + "prm_10 p " +
                          " WHERE p.nzp_prm IN (1457,2081,2082,2083,2084) and p.is_actual <> 100 " +
                          " AND " + Utils.EStrNull(new DateTime(p_year, p_month, 01).ToShortDateString()) +
                          " BETWEEN p.dat_s AND p.dat_po" +
                          " GROUP BY 1";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) { return; }

                sql = " CREATE INDEX ix1_t_max_val ON t_max_val(nzp_serv)";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) { return; }


                #endregion Расчитать расход с учетом коэф трансф и перехода через 0
                //dt1 = ViewTbl(conn_db, "select * from t_doit ");
                #region Разделить на количество месяцев

                // if (Points.IsSmr)  -- убрал потому что в днях считает неправильно , а в месяцах нормально 
                {
                    #region Расход в месяцах

#if PG
                    #region Нужно отсечь показания не с начала месяца
                    ret = ExecSQL(conn_db,
                        " Update t_doit Set dn_uchet= " +
                        "( '01.' ||EXTRACT(month FROM (dn_uchet+interval '1 month')) ||'.'|| EXTRACT(year FROM (dn_uchet+interval '1 month')) )" + sConvToDate +
                        " Where nzp_group in (61,65) and EXTRACT(day FROM dn_uchet) > 1 and EXTRACT(day FROM dn_uchet)<31 "
                        , true);
                    //dt1 = ViewTbl(conn_db, "select * from t_doit ");
                    ret = ExecSQL(conn_db,
                        " Update t_doit Set dn_uchet= " +
                        "( '01.' ||EXTRACT(month FROM (dn_uchet+interval '1 day'))   ||'.'|| EXTRACT(year FROM (dn_uchet+interval '1 day'))   )" + sConvToDate +
                        " Where nzp_group in (61,65) and EXTRACT(day FROM dn_uchet)=31 "
                        , true);
                    //dt1 = ViewTbl(conn_db, "select * from t_doit ");
                    #endregion Нужно отсечь показания не с начала месяца
                    ret = ExecSQL(conn_db,
                        " Update t_doit Set " +
                        " kol_mes    = (( EXTRACT(month FROM dk_uchet)  +(EXTRACT(year FROM  dk_uchet)  *12))-( EXTRACT(month FROM dn_uchet)+(12*EXTRACT(year FROM dn_uchet)) ) ) " +
                        ",kol_mes_sr = (( EXTRACT(month FROM saldo_date)+(EXTRACT(year FROM  saldo_date)*12))-( EXTRACT(month FROM dk_uchet)+(12*EXTRACT(year FROM dk_uchet)) ) )" +
                        " Where nzp_group in (61,65) and (( EXTRACT(month FROM dk_uchet)+(EXTRACT(year FROM  dk_uchet)*12))-( EXTRACT(month FROM dn_uchet)+(12*EXTRACT(year FROM dn_uchet)) ) ) > 0 "
                        , true);
                    if (!ret.result) { /*conn_db.Close();*/ return; }

                    //переносим в группу с показаниями превышающими лимиты
                    sql = " UPDATE t_doit t SET nzp_group=66 " +
                          " FROM t_max_val m " +
                          " WHERE (m.nzp_serv=t.nzp_serv AND (m.ipu_limit*t.kol_mes)<t.rashod) " +
                          " OR (m.nzp_serv=0 AND (m.ipu_limit*t.kol_mes)<t.rashod) ";
                    ret = ExecSQL(conn_db, sql, true);
                    if (!ret.result) { return; }

                    ExecSQL(conn_db, "DROP TABLE t_max_val", false);

                    ret = ExecSQL(conn_db,
                        " Update t_doit Set gen_rash_pu = rashod/" +
                        " ( case when kol_mes_sr=0 then 0 else 0 end + " +
                        "   (EXTRACT(month FROM dk_uchet)+(EXTRACT(year FROM  dk_uchet)*12)) - (EXTRACT(month FROM dn_uchet)+(12*EXTRACT(year FROM dn_uchet)) " +
                        " )) " +
                        " Where nzp_group in (61,65) and " +
                        " ( (EXTRACT(month FROM dk_uchet)+(EXTRACT(year FROM  dk_uchet)*12)) - (EXTRACT(month FROM dn_uchet)+(12*EXTRACT(year FROM (dn_uchet)) ) )) > 0 "
                        , true);
                    if (!ret.result) { /*conn_db.Close();*/ return; }
                    ret = ExecSQL(conn_db,
                        " Update t_doit Set gen_rash_pu = rashod/kol_mes" +

                        " Where nzp_group in (61,65) and kol_mes>0 and gen_rash_pu=0  "
                        , true);
                    if (!ret.result) { /*conn_db.Close();*/ return; }
                    //dt1 = ViewTbl(conn_db, "select * from t_doit ");
                    ret = ExecSQL(conn_db,
                        " Update t_doit Set gen_rash_pu = round(gen_rash_pu,2) " +
                        " Where nzp_group in (61,65) "
                        , true);
                    //dt1 = ViewTbl(conn_db, "select * from t_doit ");
#else
                    #region Нужно отсечь показания не с начала месяца
                        ret = ExecSQL(conn_db,
                            " Update t_doit Set dn_uchet= "+
                            "'01.' ||lpad(month(dn_uchet+1 units month),2,'0') ||'.'|| year(dn_uchet+1 units month) " +
                            " Where nzp_group in (61,65) and day(dn_uchet) > 1 and day(dn_uchet)<31 "
                            , true);
                        ret = ExecSQL(conn_db,
                            " Update t_doit Set dn_uchet= '01.' ||lpad(month(dn_uchet+1 units day),2,'0') ||'.'|| year(dn_uchet+1 units day)  " +
                            " Where nzp_group in (61,65) and day(dn_uchet)=31 "
                            , true);
                    #endregion Нужно отсечь показания не с начала месяца
                        ret = ExecSQL(conn_db,
                            " Update t_doit Set "+
                            " kol_mes= (( month(dk_uchet)+(year( dk_uchet)*12))-( month(dn_uchet)+(12*year(dn_uchet)) ) ) " +
                            ", kol_mes_sr = (( month(saldo_date)+(year( saldo_date )*12))-( month(dk_uchet)+(12*year(dk_uchet)) ) )" +
                            " Where nzp_group in (61,65) and (( month(dk_uchet)+(year( dk_uchet)*12))-( month(dn_uchet)+(12*year(dn_uchet)) ) ) > 0 "
                            , true);
                        if (!ret.result) { /*conn_db.Close();*/ return; }
                        ret = ExecSQL(conn_db,
                            " Update t_doit Set gen_rash_pu = round(rashod/"+
                            " ( case when kol_mes_sr=0 then 0 else 0 end+ (month(dk_uchet)+(year( dk_uchet)*12)) - (month(dn_uchet)+(12*year(dn_uchet)) " +
                        " )),2) " +
                            " Where nzp_group in (61,65) and (( month(dk_uchet)+(year( dk_uchet)*12))-( month(dn_uchet)+(12*year(dn_uchet)) ) ) > 0 "
                            , true);
#endif
                    if (!ret.result) { /*conn_db.Close();*/ return; }
                    //dt1 = ViewTbl(conn_db, "select * from t_doit ");
                    #endregion Расход в месяцах
                }
                #region Расчет в днях убрал совсем , если нужно будет то нужно будет полностью переписать
                //else
                //{
                //    #region Расход в днях
                //    ret = ExecSQL(conn_db,
                //        " Update t_doit Set gen_rash_pu = rashod * count_days / cnt_days_rs " +
                //        " Where nzp_group = 61 and cnt_days_rs > 0 "
                //        , true);
                //    if (!ret.result) { /*conn_db.Close();*/ return; }
                //    #endregion Расход в днях
                //}
                #endregion Расчет в днях убрал совсем

                #endregion Разделить на количество дней

                #region Перенести из группы 65 в группу 61
                ret = ExecSQL(conn_db, " create temp table t_doitR(nzp_kvar integer , nzp_serv integer)  ", false);
                if (!ret.result) { ExecSQL(conn_db, " truncate table t_doitR  ", false); }

                ret = ExecSQL(conn_db, " insert into t_doitR select distinct nzp_kvar,nzp_serv from t_doit where nzp_group in (61) and gen_rash_pu>0 and kol_mes_sr<=6 and kol_mes>=3   ", true);

                //По просьбе БФТ
                //ret = ExecSQL(conn_db, " Update t_doit Set kol_count =1 " +
                //                       " where nzp_group in (65) and gen_rash_pu=0 and kol_mes_sr<=6 and kol_mes>=3   " +
                //                       " and (select count(*) from t_doitR a where a.nzp_serv =t_doit.nzp_serv and a.nzp_kvar=t_doit.nzp_kvar )>0 "
                //                           , true);
                ret = ExecSQL(conn_db, " Update t_doit Set kol_count =1 " +
                       " where nzp_group in (65) and gen_rash_pu=0 and kol_mes_sr<=6 and kol_mes>=3   " /*+
                                       " and EXISTS(select 1 from t_doitR a where a.nzp_serv =t_doit.nzp_serv and a.nzp_kvar=t_
doit.nzp_kvar ) "*/
                           , true);

                #endregion Перенести из группы 65 в группу 61


                #region Сохранить значения среднего для счетчика для группы 61
#if PG
                string serial_fld = "";
                string serial_val = "";
#else
                    string serial_fld = ",nzp_key";
                    string serial_val = ",0";
#endif
                ret = ExecSQL(conn_db,
                    " insert into " + sDataAlias + "prm_17 (nzp,nzp_prm,dat_s,dat_po,val_prm,is_actual,nzp_user,dat_when" + serial_fld + ") " +
                    " select nzp_counter,979,'" + dDtBeg.ToShortDateString() + "','" + dDtEnd.ToShortDateString() + "', " +
                      " gen_rash_pu" + sConvToChar10 + ",1,1," + sCurDate + serial_val +
                    " from t_doit where nzp_group = 61  and gen_rash_pu>=0 and kol_mes_sr<=6 and kol_mes>=3 "
                    , true);
                if (!ret.result) { /*conn_db.Close();*/ return; }
                //dt1 = ViewTbl(conn_db, "select * from t_doit ");
                // теперь добавим нулевые средние если есть не нулевые 
                ret = ExecSQL(conn_db,
                    " insert into " + sDataAlias + "prm_17 (nzp,nzp_prm,dat_s,dat_po,val_prm,is_actual,nzp_user,dat_when" + serial_fld + ") " +
                    " select nzp_counter,979,'" + dDtBeg.ToShortDateString() + "','" + dDtEnd.ToShortDateString() + "', " +
                      " gen_rash_pu" + sConvToChar10 + ",1,1," + sCurDate + serial_val +
                    " from t_doit where nzp_group in ( 65 )  and kol_count >0 and gen_rash_pu=0 and kol_mes_sr<=6 and kol_mes>=3  "
                    , true);
                if (!ret.result) { /*conn_db.Close();*/ return; }
                //dt1 = ViewTbl(conn_db, "select * from t_doit ");
                #endregion Сохранить значения среднего для счетчика для группы 61

                #region Сохранение средних в верхнем и нижних банках по префиксам
                RecordMonth rm = new RecordMonth();
                rm.year_ = p_year;
                rm.month_ = p_month;

                InsGenSrZnKPUGroups(conn_db, Points.Pref, rm.name_month, out ret);
                InsGenSrZnKPUGroups(conn_db, sCurPref, rm.name_month, out ret);
                CrtGenSrZnKPUGroups(conn_db, sCurPref, out ret);

                #endregion Сохранение средних в верхнем и нижних банках по префиксам

                #region Удалить временную таблицу
                ExecSQL(conn_db, " Drop table t_doit ", false);
                #endregion Удалить временную таблицу
            }
            #endregion Расчет средних для каждого префикса базы данных

        }
        #endregion Генерация средних значений счетчиков КПУ(квартирные)

        #region Создание наименований групп по средним значениям ПУ
        //-----------------------------------------------------------------------------
        void InsGenSrZnKPUGroups(IDbConnection conn_db, string sGrpPref, string sname_month, out Returns ret)
        //-----------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            string sDataAlias = sGrpPref.Trim() + "_data" + tableDelimiter;

            #region nzp_group=61
            if (!DBManager.ExecScalar<bool>(conn_db,
                "SELECT count(nzp_group)>0 FROM " + sDataAlias + "s_group WHERE nzp_group=61"))
            {

                ret = ExecSQL(conn_db,
                    " insert into " + sDataAlias +
                    "s_group (nzp_group,ngroup) values (61,'0Генерация средних расходов ИПУ - установлен расход -" +
                    sname_month + "') "
                    , true);
                if (!ret.result)
                    return;
            }
            else
            {
                ret = ExecSQL(conn_db,
                  " UPDATE " + sDataAlias +
                  "s_group SET ngroup='0Генерация средних расходов ИПУ - установлен расход -" +
                  sname_month + "' WHERE nzp_group=61 "
                  , true);
                if (!ret.result)
                    return;
            }
            #endregion nzp_group=61
            #region nzp_group=62
            if (!DBManager.ExecScalar<bool>(conn_db,
                "SELECT count(nzp_group)>0 FROM " + sDataAlias + "s_group WHERE nzp_group=62"))
            {
                ret = ExecSQL(conn_db,
                    " insert into " + sDataAlias +
                    "s_group (nzp_group,ngroup) values (62,'0Генерация средних расходов ИПУ - нет показаний для генерации -" +
                    sname_month + "') "
                    , true);
                if (!ret.result)
                    return;
            }
            else
            {
                ret = ExecSQL(conn_db,
                  " UPDATE " + sDataAlias +
                  "s_group SET ngroup='0Генерация средних расходов ИПУ - нет показаний для генерации -" +
                  sname_month + "' WHERE nzp_group=62 "
                  , true);
                if (!ret.result)
                    return;
            }
            #endregion nzp_group=62
            #region nzp_group=63
            if (!DBManager.ExecScalar<bool>(conn_db,
                "SELECT count(nzp_group)>0 FROM " + sDataAlias + "s_group WHERE nzp_group=63"))
            {
                ret = ExecSQL(conn_db,
                    " insert into " + sDataAlias +
                    "s_group (nzp_group,ngroup) values (63,'0Генерация средних расходов ИПУ - есть показание ИПУ -" +
                    sname_month + "') "
                    , true);
                if (!ret.result)
                    return;
            }
            else
            {
                ret = ExecSQL(conn_db,
                  " UPDATE " + sDataAlias +
                  "s_group SET ngroup='0Генерация средних расходов ИПУ - есть показание ИПУ -" +
                  sname_month + "' WHERE nzp_group=63 "
                  , true);
                if (!ret.result)
                    return;
            }
            #endregion nzp_group=63
            #region nzp_group=64
            if (!DBManager.ExecScalar<bool>(conn_db,
                "SELECT count(nzp_group)>0 FROM " + sDataAlias + "s_group WHERE nzp_group=64"))
            {
                ret = ExecSQL(conn_db,
                    " insert into " + sDataAlias +
                    "s_group (nzp_group,ngroup) values (64,'0Генерация средних расходов ИПУ - нет ИПУ -" + sname_month +
                    "') "
                    , true);
                if (!ret.result)
                    return;
            }
            else
            {
                ret = ExecSQL(conn_db,
                  " UPDATE " + sDataAlias +
               "s_group SET ngroup='0Генерация средних расходов ИПУ - нет ИПУ -" +
                  sname_month + "' WHERE nzp_group=64 "
                  , true);
                if (!ret.result)
                    return;
            }
            #endregion nzp_group=64
            #region nzp_group=65
            if (!DBManager.ExecScalar<bool>(conn_db,
                "SELECT count(nzp_group)>0 FROM " + sDataAlias + "s_group WHERE nzp_group=65"))
            {
                ret = ExecSQL(conn_db,
                    " insert into " + sDataAlias +
                    "s_group (nzp_group,ngroup) values (65,'0Генерация средних расходов ИПУ - расход ИПУ равен 0 -" +
                    sname_month + "') "
                    , true);
                if (!ret.result)
                    return;
            }
            else
            {
                ret = ExecSQL(conn_db,
                  " UPDATE " + sDataAlias +
                  "s_group SET ngroup='0Генерация средних расходов ИПУ - расход ИПУ равен 0 -" +
                  sname_month + "' WHERE nzp_group=65 "
                  , true);
                if (!ret.result)
                    return;
            }
            #endregion nzp_group=65

            #region nzp_group=66
            if (!DBManager.ExecScalar<bool>(conn_db,
                "SELECT count(nzp_group)>0 FROM " + sDataAlias + "s_group WHERE nzp_group=66"))
            {
                ret = ExecSQL(conn_db,
                    " insert into " + sDataAlias +
                    "s_group (nzp_group,ngroup) values (66,'0Генерация средних расходов ИПУ - превышение допустимого расхода -" +
                    sname_month + "') "
                    , true);
                if (!ret.result)
                    return;
            }
            else
            {
                ret = ExecSQL(conn_db,
                  " UPDATE " + sDataAlias +
                  "s_group SET ngroup='0Генерация средних расходов ИПУ - превышение допустимого расхода -" +
                  sname_month + "' WHERE nzp_group=66 "
                  , true);
                if (!ret.result)
                    return;
            }
            #endregion nzp_group=66

        }
        #endregion Создание наименований групп по средним значениям ПУ

        #region Наполнение группы по сгенерированным значениям счетчиков
        //-----------------------------------------------------------------------------
        void CrtGenSrZnKPUGroups(IDbConnection conn_db, string sGrpPref, out Returns ret)
        //-----------------------------------------------------------------------------
        {
            string sDataAlias = sGrpPref.Trim() + "_data" + tableDelimiter;

            ret = ExecSQL(conn_db,
                " delete from " + sDataAlias + "link_group where nzp_group in (61,62,63,64,65,66) "
                , false);
            if (!ret.result) { return; }
            ret = ExecSQL(conn_db,
                " insert into " + sDataAlias + "link_group (nzp_group,nzp) select nzp_group,nzp_kvar from t_doit group by 1,2 "
                , true);
            if (!ret.result) { /*conn_db.Close();*/ return; }
            ret = ExecSQL(conn_db,
                " insert into " + sDataAlias + "link_group (nzp_group,nzp)" +
                " select 64 nzp_group,g.nzp_kvar from t_ls_gen g " +
                " Where g.pref='" + sGrpPref.Trim() + "' and not exists(select 1 from t_doit k where k.nzp_kvar=g.nzp_kvar)" +
                " group by 1,2 "
                , true);
            if (!ret.result) { /*conn_db.Close();*/ return; }

        }

        #endregion Наполнение группы по сгенерированным значениям счетчиков

        #endregion Генерация средних значений счетчиков

        #region расчет услуг в процентах от суммы начислений
        public void CalcSumForAllServices(IDbConnection conn_db, int mm, int yy, string pLocalPref, string pChargeName, string pWhere, bool pDoExecStep, out Returns ret)
        {
            ret = Utils.InitReturns();

            ExecSQL(conn_db, " drop table t_sum_itog ", false);
            ret = ExecSQL(conn_db,
                " create temp table t_sum_itog (" +
                " nzp_kvar integer," +
                " nzp_serv integer," +
                " nzp_supp integer," +
                " nzp_clc  integer," +
                " nzp_frm_typrs  integer," +
                " perc     " + sDecimalType + "(8,4) default 0, " +
                " sum_itog " + sDecimalType + "(14,2) default 0, " +
                " sum_nach " + sDecimalType + "(14,2) default 0  " +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesCharge(conn_db); return; }

            // выбрать ЭОТ тарифы по ЛС/услугам/поставщикам
            ret = ExecSQL(conn_db,
                " insert into t_sum_itog (nzp_kvar,nzp_serv,nzp_supp,nzp_clc,perc,nzp_frm_typrs)" +
                " select t.nzp_kvar,t.nzp_serv,t.nzp_supp,max(t.nzp_clc) as nzp_clc,max(t.tarif) as perc,max(nzp_prm_tarif) as nzp_frm_typrs " +
                " from t_calc_gku t " +
                " where t.nzp_frm_typ=53 " +
                " group by 1,2,3 "
                , true);
            if (!ret.result) { DropTempTablesCharge(conn_db); return; }

            ret = ExecSQL(conn_db, " create index ix1t_sum_itog on t_sum_itog(nzp_kvar,nzp_serv,nzp_supp) ", true);
            if (!ret.result) { DropTempTablesCharge(conn_db); return; }
            ret = ExecSQL(conn_db, " create index ix2t_sum_itog on t_sum_itog(nzp_clc) ", true);
            if (!ret.result) { DropTempTablesCharge(conn_db); return; }

            ret = ExecSQL(conn_db, sUpdStat + " t_sum_itog ", true);
            if (!ret.result) { DropTempTablesCharge(conn_db); return; }

            ExecSQL(conn_db, " drop table t_ls_itog ", false);
            ret = ExecSQL(conn_db,
                " create temp table t_ls_itog (" +
                " nzp_kvar integer," +
                " nzp_frm_typrs integer " +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesCharge(conn_db); return; }

            // выбрать ЭОТ тарифы по ЛС/услугам/поставщикам
            ret = ExecSQL(conn_db,
                " insert into t_ls_itog (nzp_kvar,nzp_frm_typrs)" +
                " select nzp_kvar,max(nzp_frm_typrs) as nzp_frm_typrs " +
                " from t_sum_itog " +
                " group by 1 "
                , true);
            if (!ret.result) { DropTempTablesCharge(conn_db); return; }

            ret = ExecSQL(conn_db, " create index ixt_ls_itog on t_ls_itog(nzp_kvar,nzp_frm_typrs) ", true);
            if (!ret.result) { DropTempTablesCharge(conn_db); return; }

            ret = ExecSQL(conn_db, sUpdStat + " t_ls_itog ", true);
            if (!ret.result) { DropTempTablesCharge(conn_db); return; }

            ExecSQL(conn_db, " drop table t_serv_itog ", false);
            ret = ExecSQL(conn_db,
                " create temp table t_serv_itog (" +
                " nzp_serv integer" +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesCharge(conn_db); return; }

            // выбрать ЭОТ тарифы по ЛС/услугам/поставщикам
            ret = ExecSQL(conn_db,
                " insert into t_serv_itog (nzp_serv)" +
                " select nzp_serv from t_sum_itog group by 1 "
                , true);
            if (!ret.result) { DropTempTablesCharge(conn_db); return; }

            ret = ExecSQL(conn_db, sUpdStat + " t_serv_itog ", true);
            if (!ret.result) { DropTempTablesCharge(conn_db); return; }

            ExecSQL(conn_db, " drop table t_sum_ch ", false);
            ret = ExecSQL(conn_db,
                " create temp table t_sum_ch (" +
                " nzp_kvar integer," +
                " sum_itog " + sDecimalType + "(14,2) default 0 " +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesCharge(conn_db); return; }

            // ... взять поле для суммирования по начислениям для определения процента ... 
            //1986|Коэффициент удержания|||float||11|0|1000000|7| -- в формуле как тариф!=расход на договор/поставщика
            //179|Коэффициент удержания|||float||1|0|1000000|7|   -- в формуле как тариф!=расход на ЛС
            //
            //1254|Расчет % общей суммы без НДС|||bool||5||||
            //1255|Процент НДС|1||float||5|0|1000000|7|
            //
            // выбрать итоговую сумму по всем услугам для начисления процента
            ret = ExecSQL(conn_db,
                " insert into t_sum_ch (nzp_kvar,sum_itog)" +
                " select t.nzp_kvar,sum(" +
                " case" +
                " when t.nzp_frm_typrs=5301 then c.sum_outsaldo" +
                " when t.nzp_frm_typrs=5302 then c.sum_real" +
                " when t.nzp_frm_typrs=5303 then c.sum_money" +
                " when t.nzp_frm_typrs=5304 then c.sum_tarif" +
                " when t.nzp_frm_typrs=5305 then c.rsum_tarif" +
                " when t.nzp_frm_typrs=5306 then c.sum_insaldo" +
                " when t.nzp_frm_typrs=5307 then c.sum_real + c.reval + c.real_charge" +
                " else c.sum_charge " +
                " end " +
                ") as sum_itog " +
                " from t_ls_itog t," + pChargeName + " c " +
                " where t.nzp_kvar=c.nzp_kvar and c.nzp_serv>1 and c.dat_charge is null" +
                " and 0=(select count(*) from t_serv_itog s where s.nzp_serv=c.nzp_serv) " +
                " group by 1 "
                , true);
            if (!ret.result) { DropTempTablesCharge(conn_db); return; }

            ret = ExecSQL(conn_db, " create index ixt_sum_ch on t_sum_ch(nzp_kvar) ", true);
            if (!ret.result) { DropTempTablesCharge(conn_db); return; }

            ret = ExecSQL(conn_db, sUpdStat + " t_sum_ch ", true);
            if (!ret.result) { DropTempTablesCharge(conn_db); return; }

            ret = ExecSQL(conn_db,
                " update t_sum_itog " +
                " set sum_itog=" +
                  "(select sum(sum_itog) from t_sum_ch t where t.nzp_kvar=t_sum_itog.nzp_kvar) " +
                " where 0<" +
                  "(select count(*) from t_sum_ch t where t.nzp_kvar=t_sum_itog.nzp_kvar) "
                , true);
            if (!ret.result) { DropTempTablesCharge(conn_db); return; }

            ret = ExecSQL(conn_db,
                " update t_sum_itog " +
                " set sum_nach = sum_itog * perc " +
                " where 1=1 "
                , true);
            if (!ret.result) { DropTempTablesCharge(conn_db); return; }

            string sqql =
                " Update " + pChargeName + " Set" +

                " tarif = " +
                " (select sum(t.sum_itog) from t_sum_itog t" +
                " where t.nzp_kvar=" + pChargeName + ".nzp_kvar and t.nzp_serv=" + pChargeName + ".nzp_serv and t.nzp_supp=" + pChargeName + ".nzp_supp) " +

                ",sum_real = " +
                " (select sum(t.sum_nach) from t_sum_itog t" +
                " where t.nzp_kvar=" + pChargeName + ".nzp_kvar and t.nzp_serv=" + pChargeName + ".nzp_serv and t.nzp_supp=" + pChargeName + ".nzp_supp) " +

                ",c_calc = " +
                " (select sum(t.perc) from t_sum_itog t" +
                " where t.nzp_kvar=" + pChargeName + ".nzp_kvar and t.nzp_serv=" + pChargeName + ".nzp_serv and t.nzp_supp=" + pChargeName + ".nzp_supp) " +

                " Where " + pWhere +
                " and 0<(select count(*) from t_sum_itog t" +
                " where t.nzp_kvar=" + pChargeName + ".nzp_kvar and t.nzp_serv=" + pChargeName + ".nzp_serv and t.nzp_supp=" + pChargeName + ".nzp_supp) ";
            if (!pDoExecStep)
                ret = ExecSQL(conn_db, sqql, true);
            else
                ExecByStep(conn_db, pChargeName, "nzp_charge", sqql, 50000, " ", out ret);
            if (!ret.result) { DropTempTablesCharge(conn_db); return; }

            sqql =
                " Update " + pChargeName +
                " Set gsum_tarif = sum_real, rsum_tarif = sum_real, sum_tarif = sum_real, c_sn = c_calc" +
                " ,sum_outsaldo = sum_insaldo + real_charge + sum_real + reval + izm_saldo - (money_to + money_from + money_del) " +
                " Where " + pWhere +
                " and 0<(select count(*) from t_sum_itog t" +
                " where t.nzp_kvar=" + pChargeName + ".nzp_kvar and t.nzp_serv=" + pChargeName + ".nzp_serv and t.nzp_supp=" + pChargeName + ".nzp_supp) ";
            if (!pDoExecStep)
                ret = ExecSQL(conn_db, sqql, true);
            else
                ExecByStep(conn_db, pChargeName, "nzp_charge", sqql, 50000, " ", out ret);
            if (!ret.result) { DropTempTablesCharge(conn_db); return; }

            GetFldSumToPay(conn_db, mm, yy, "", out ret);

            sqql =
                " Update " + pChargeName +
                " Set sum_charge = " + ret.text.Trim() +
                " Where " + pWhere +
                " and 0<(select count(*) from t_sum_itog t" +
                " where t.nzp_kvar=" + pChargeName + ".nzp_kvar and t.nzp_serv=" + pChargeName + ".nzp_serv and t.nzp_supp=" + pChargeName + ".nzp_supp) ";
            if (!pDoExecStep)
                ret = ExecSQL(conn_db, sqql, true);
            else
                ExecByStep(conn_db, pChargeName, "nzp_charge", sqql, 50000, " ", out ret);
            if (!ret.result) { DropTempTablesCharge(conn_db); return; }

            ret = ExecSQL(conn_db, " drop table t_ls_itog ", true);
            if (!ret.result) { DropTempTablesCharge(conn_db); return; }
            ret = ExecSQL(conn_db, " drop table t_sum_ch ", true);
            if (!ret.result) { DropTempTablesCharge(conn_db); return; }
            ret = ExecSQL(conn_db, " drop table t_sum_itog ", true);
            if (!ret.result) { DropTempTablesCharge(conn_db); return; }
        }
        #endregion расчет услуг в процентах от суммы начислений


        #region Тип расхода = 610 - вода для транспорта по видам транспорта

        public bool CalcTypeRashod_610(IDbConnection conn_db, Gku gku, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            #region  выбрать перечень ЛС с услугами с типом расхода 610
            ExecSQL(conn_db, " drop table t_z610 ", false);
            ret = ExecSQL(conn_db,
                " create temp table t_z610 (" +
                " nzp_kvar integer," +
                " nzp_frm  integer," +
                " nzp_frm_typ integer," +
                " nzp_period integer," +
                " dat_s    DATE," +
                " dat_po   DATE," +
                " cntd     integer, " +
                " cntd_mn  integer  " +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result)
            {
                CalcGkuXX_CloseTmp(conn_db);
                return false;
            }

            ret = ExecSQL(conn_db,
                " insert into t_z610 (nzp_kvar,nzp_frm,nzp_frm_typ" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn)" +
                " select t.nzp_kvar,t.nzp_frm,f.nzp_frm_typ " +
                " ,t.nzp_period,max(t.dat_s),max(t.dat_po),max(t.cntd),max(t.cntd_mn)" +
                " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f" +
                " where t.nzp_frm=f.nzp_frm and f.nzp_frm_typrs=610" +
                " group by 1,2,3,4 "
                , true);
            if (!ret.result)
            {
                CalcGkuXX_CloseTmp(conn_db);
                return false;
            }

            ExecSQL(conn_db, sUpdStat + " t_z610 ", true);

            #endregion  выбрать перечень ЛС

            ViewTbl(conn_db, " select * from t_z610 order by nzp_kvar ");

            #region  выбрать параметры нормы расхода на 1 ед.(шт!) количества на тип транспорта
            ExecSQL(conn_db, " drop table t_k610 ", false);
            ret = ExecSQL(conn_db,
                " create temp table t_k610 (" +
                " ipos integer," +
                " nzp_prmk integer," +
                " valk " + sDecimalType + "(14,7) default 0" +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result)
            {
                CalcGkuXX_CloseTmp(conn_db);
                return false;
            }

            ret = ExecSQL(conn_db,
                " insert into t_k610 (ipos,nzp_prmk)" +
                " select p.order ipos,p.nzp_prm " +
                " from " + gku.paramcalc.kernel_alias + "prm_frm p " +
                " where p.nzp_frm = 610 and p.frm_calc = 2 and mod(p.order-1,4)=0 and p.nzp_prm>0 "
                , true);
            if (!ret.result)
            {
                CalcGkuXX_CloseTmp(conn_db);
                return false;
            }

            ExecSQL(conn_db, sUpdStat + " t_k610 ", true);

            #endregion  выбрать параметры нормы расхода на 1 ед.(шт!) количества на тип транспорта

            ViewTbl(conn_db, " select * from t_k610 ");

            #region выбрать параметры количества по типам транспорта

            ExecSQL(conn_db, " drop table t_r610 ", false);
            ret = ExecSQL(conn_db,
                " create temp table t_r610 (" +
                " ipos integer," +
                " nzp_prmr integer," +
                " valr " + sDecimalType + "(14,7) default 0" +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result)
            {
                CalcGkuXX_CloseTmp(conn_db);
                return false;
            }

            ret = ExecSQL(conn_db,
                " insert into t_r610 (ipos,nzp_prmr)" +
                " select p.order ipos,p.nzp_prm " +
                " from " + gku.paramcalc.kernel_alias + "prm_frm p " +
                " where p.nzp_frm = 610 and p.frm_calc = 2 and mod(p.order+1,4)=0 and p.nzp_prm>0 "
                , true);
            if (!ret.result)
            {
                CalcGkuXX_CloseTmp(conn_db);
                return false;
            }

            ExecSQL(conn_db, sUpdStat + " t_r610 ", true);

            #endregion выбрать параметры количества по типам транспорта

            ViewTbl(conn_db, " select * from t_r610 ");

            #region подсчитать общий объем воды для всех видов транспорта
            ExecSQL(conn_db, " drop table t_v610 ", false);

            ret = ExecSQL(conn_db,
                " create temp table t_v610 (" +
                " nzp_kvar integer," +
                " nzp_frm  integer," +
                " nzp_frm_typ integer," +
                " ipos     integer," +
                " nzp_prmr integer," +
                " nzp_prmk integer," +
                " valk " + sDecimalType + "(14,7) default 0," +
                " valr " + sDecimalType + "(14,7) default 0," +
                " valm " + sDecimalType + "(14,7) default 0," +
                " nzp_period integer," +
                " dat_s    DATE," +
                " dat_po   DATE," +
                " cntd     integer, " +
                " cntd_mn  integer  " +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result)
            {
                CalcGkuXX_CloseTmp(conn_db);
                return false;
            }


            ret = ExecSQL(conn_db,
                " Insert into t_v610 " +
                " (nzp_kvar,nzp_frm,nzp_frm_typ,ipos,nzp_prmr,nzp_prmk" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn)" +
                " select z.nzp_kvar,z.nzp_frm,z.nzp_frm_typ,a.ipos,a.nzp_prmr,b.nzp_prmk " +
                " ,z.nzp_period,z.dat_s,z.dat_po,z.cntd,z.cntd_mn " +
                " from t_z610 z, t_r610 a, t_k610 b " +
                " where a.ipos-b.ipos =2 "
                , true);
            if (!ret.result)
            {
                CalcGkuXX_CloseTmp(conn_db);
                return false;
            }

            ExecSQL(conn_db, " Create index ix_t_v610 on t_v610 (nzp_kvar,dat_s,dat_po) ", true);
            ExecSQL(conn_db, sUpdStat + " t_v610 ", true);

            ret = ExecSQL(conn_db,
                " update t_v610 set " +
                " valk=(select max(p.val_prm" + sConvToNum + ") from " + gku.paramcalc.data_alias +
                "prm_5 p where p.nzp_prm=t_v610.nzp_prmk" +
                " and p.is_actual<>100 and p.dat_s <= t_v610.dat_po and p.dat_po >= t_v610.dat_s), " +
                " valr=(select max(p.val" + sConvToNum +
                ") from t_par1 p where p.nzp=t_v610.nzp_kvar and p.nzp_prm=t_v610.nzp_prmr" +
                " and p.dat_s <= t_v610.dat_po and p.dat_po >= t_v610.dat_s) " +
                " where 0<(select count(*) from t_par1 p where p.nzp=t_v610.nzp_kvar and p.nzp_prm=t_v610.nzp_prmr" +
                " and p.dat_s <= t_v610.dat_po and p.dat_po >= t_v610.dat_s) "
                , true);
            if (!ret.result)
            {
                CalcGkuXX_CloseTmp(conn_db);
                return false;
            }

            ret = ExecSQL(conn_db, " update t_v610 set valk = 0 where valk is null ", true);
            if (!ret.result)
            {
                CalcGkuXX_CloseTmp(conn_db);
                return false;
            }

            ret = ExecSQL(conn_db, " update t_v610 set valm = valk * valr where 1=1 ", true);
            if (!ret.result)
            {
                CalcGkuXX_CloseTmp(conn_db);
                return false;
            }
            #endregion подсчитать общий объем воды для всех видов транспорта

            //проверка
            ViewTbl(conn_db, " select * from t_par1 order by nzp ");
            ViewTbl(conn_db, "select * from " + gku.paramcalc.data_alias + "prm_5 p where p.nzp_prm in (347,350,348,351,349,352)" +
                      " and p.is_actual<>100 and p.dat_s<" + gku.dat_po + " and p.dat_po>=" + gku.dat_s + " ");
            ViewTbl(conn_db, " select * from t_v610 order by nzp_kvar ");


            #region положить в таблицу расходов

            ret = ExecSQL(conn_db,
                " insert into t_rsh (nzp_kvar,nzp_frm,nzp_prm,nzp_frm_typ,rashod,rashod_g,nzp_frm_typrs,valm" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn)" +
                " select t.nzp_kvar,t.nzp_frm,0 nzp_prm,t.nzp_frm_typ,sum(t.valm),sum(t.valm),610,sum(t.valm)" +
                " ,t.nzp_period,max(t.dat_s),max(t.dat_po),max(t.cntd),max(t.cntd_mn) " +
                " from t_v610 t " +
                " group by 1,2,3,4,9 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            #endregion положить в таблицу расходов
            ViewTbl(conn_db, " select * from t_rsh  ");
            #region Удалить временные таблицы
            ret = ExecSQL(conn_db, " drop table t_v610 ", true);
            if (!ret.result)
            {
                CalcGkuXX_CloseTmp(conn_db);
                return false;
            }
            ret = ExecSQL(conn_db, " drop table t_z610 ", true);
            if (!ret.result)
            {
                CalcGkuXX_CloseTmp(conn_db);
                return false;
            }
            ret = ExecSQL(conn_db, " drop table t_r610 ", true);
            if (!ret.result)
            {
                CalcGkuXX_CloseTmp(conn_db);
                return false;
            }
            ret = ExecSQL(conn_db, " drop table t_k610 ", true);
            if (!ret.result)
            {
                CalcGkuXX_CloseTmp(conn_db);
                return false;
            }
            #endregion Удалить временные таблицы

            return true;
        }

        #endregion Тип расхода = 610 - вода для домашних животных по видам скота


        #region Тип тарифа =97 - процент удержания
        public bool CalcTypeTarif_97(IDbConnection conn_db, Gku gku, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();


            ExecSQL(conn_db, " drop table t_97 ", false);
            ret = ExecSQL(conn_db,
                " create temp table t_97 (" +
                " nzp_kvar integer," +
                " nzp_dom integer," +
                " nzp_frm  integer," +
                " nzp_prm  integer," +
                " nzp_frm_typ integer," +
                " nzp_frm_typrs integer," +
                " nzp_prm_tarif_ls integer," + //параметр для определения процента удержания по ЛС 
                " nzp_prm_tarif_dm integer," +//параметр для определения процента удержания по дому 
                " nzp_prm_tarif_bd integer," +//параметр для определения процента удержания по банку 
                " nzp_period integer," +
                " dat_s    DATE," +
                " dat_po   DATE," +
                " cntd     integer, " +
                " cntd_mn  integer  " +
                " ,procent    " + sDecimalType + //процент удержания 
                " ,tarif " + sDecimalType + "(14,2)" + //начислено по тарифу за прошлый месяц
                " ,rashod " + sDecimalType +  //процент от sum_tarif
                " ,priority integer " +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result)
            {
                CalcGkuXX_CloseTmp(conn_db);
                return false;
            }

            //получаем список лс типом расхода =97
            ret = ExecSQL(conn_db,
          " insert into t_97 (nzp_kvar,nzp_dom,nzp_frm,nzp_frm_typ,nzp_frm_typrs,nzp_prm_tarif_ls,nzp_prm_tarif_dm,nzp_prm_tarif_bd" +
          ",nzp_period,dat_s,dat_po,cntd,cntd_mn)" +
          " select t.nzp_kvar,t.nzp_dom,t.nzp_frm,f.nzp_frm_typ,f.nzp_frm_typrs,f.nzp_prm_tarif_ls, f.nzp_prm_tarif_dm, f.nzp_prm_tarif_bd " +
          " ,t.nzp_period,max(t.dat_s),max(t.dat_po),max(t.cntd),max(t.cntd_mn)" +
          " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f" +
          " where t.nzp_frm=f.nzp_frm and f.nzp_frm_typ=97" +
          " group by 1,2,3,4,5,6,7,8,9 "
          , true);
            if (!ret.result)
            {
                CalcGkuXX_CloseTmp(conn_db);
                return false;
            }


            #region получение процента удержания
            //дата текущего расчетного месяца
            DateTime curDate = new DateTime(gku.paramcalc.cur_yy, gku.paramcalc.cur_mm, 1);

            //получаем значение процента удержания для ЛС
            string sql = "UPDATE t_97 SET procent=" +
                         "(SELECT p1.val_prm" + sConvToNum + " as procent" +
                         " FROM ttt_prm_1 p1" +
                         " WHERE t_97.nzp_kvar=p1.nzp AND p1.nzp_prm=t_97.nzp_prm_tarif_ls), nzp_prm=nzp_prm_tarif_ls, priority=3";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
            {
                CalcGkuXX_CloseTmp(conn_db);
                return false;
            }


            //получаем значение процента удержания по домовому параметру(для тех лс у которых нет параметра по лс)
            sql = "UPDATE t_97 SET procent=" +
                        "(SELECT p2.val_prm " + sConvToNum + " as procent" +
                        " FROM ttt_prm_2 p2 WHERE t_97.nzp_dom=p2.nzp  AND p2.nzp_prm=t_97.nzp_prm_tarif_dm), nzp_prm=nzp_prm_tarif_dm,priority=2 " +
                  " WHERE procent is null ";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
            {
                CalcGkuXX_CloseTmp(conn_db);
                return false;
            }

            //получаем значение по банку для тех лс у которых еще не проставлен процента удержания 
            sql = "UPDATE t_97 SET procent=" +
                        "(SELECT max(p5.val_prm) " + sConvToNum + " as procent" +
                        " FROM " + gku.paramcalc.data_alias +
                        "prm_5 p5 WHERE p5.is_actual<>100" +
                        " and " + Utils.EStrNull(curDate.ToShortDateString()) + sConvToDate + " between p5.dat_s and p5.dat_po" +
                        " AND p5.nzp_prm=t_97.nzp_prm_tarif_bd), nzp_prm=nzp_prm_tarif_bd, priority=1" +//временное значение nzp_prm
                  " WHERE procent is null ";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
            {
                CalcGkuXX_CloseTmp(conn_db);
                return false;
            }

            sql = "UPDATE t_97 SET procent=procent/100 WHERE  procent is not null;"; //если и по банку нет процента 
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
            {
                CalcGkuXX_CloseTmp(conn_db);
                return false;
            }


            sql = "UPDATE t_97 SET procent=0, priority=0 WHERE procent is null;"; //если и по банку нет процента 
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
            {
                CalcGkuXX_CloseTmp(conn_db);
                return false;
            }

            #endregion


            string pref_charge_yy = gku.paramcalc.pref + "_charge_" + (gku.paramcalc.prev_calc_yy - 2000).ToString("00");
            if (!DBManager.SchemaExists(pref_charge_yy, conn_db)) //нет схемы с начислениями за прошлый месяц
                return true;


            //получаем начисления по тарифу за предыдущий месяц исключая услуги 1-итого, 97-процент удержания,500 - пени
            sql = " UPDATE t_97 SET tarif= " +
                  "(SELECT " + sNvlWord + "(sum(" +
                " case" +
                " when t_97.nzp_frm_typrs=5301 then ch.sum_outsaldo" +
                " when t_97.nzp_frm_typrs=5302 then ch.sum_real" +
                " when t_97.nzp_frm_typrs=5303 then ch.sum_money" +
                " when t_97.nzp_frm_typrs=5304 then ch.sum_tarif" +
                " when t_97.nzp_frm_typrs=5305 then ch.rsum_tarif" +
                " when t_97.nzp_frm_typrs=5306 then ch.sum_insaldo" +
                " when t_97.nzp_frm_typrs=5307 then ch.sum_real + ch.reval + ch.real_charge" +
                " else ch.sum_charge " +
                " end " +
                ") ,0) FROM " + gku.prevSaldoMon_charge + " ch" +
                  " WHERE ch.nzp_kvar=t_97.nzp_kvar and ch.dat_charge is null and ch.nzp_serv not in (1,97,500))";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
            {
                CalcGkuXX_CloseTmp(conn_db);
                return false;
            }


            #region положить в таблицу тарифов
            sql = " INSERT into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,nzp_frm_typ,tarif,priority" +
                  ",nzp_period,dat_s,dat_po,cntd,cntd_mn)" +
                  " SELECT t.nzp_dom, t.nzp_kvar,t.nzp_supp,t.nzp_frm,tp.nzp_prm,tp.nzp_frm_typ,tp.tarif," +
                  " tp.priority, t.nzp_period, t.dat_s, t.dat_po, t.cntd, t.cntd_mn " +
                  " FROM t_trf t, t_97 tp " +
                  " WHERE t.nzp_kvar=tp.nzp_kvar and t.nzp_frm=tp.nzp_frm ";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
            {
                CalcGkuXX_CloseTmp(conn_db);
                return false;
            }
            #endregion положить в таблицу тарифов

            return true;
        }


        public bool CalcTypeRashod_97(IDbConnection conn_db, Gku gku, out Returns ret)
        {
            #region положить в таблицу расходов
            ret = ExecSQL(conn_db,
                " insert into t_rsh (nzp_kvar,nzp_frm,nzp_prm,nzp_frm_typ,rashod,rashod_g,nzp_frm_typrs,valm" +
                ",nzp_period,dat_s,dat_po,cntd,cntd_mn)" +
                " select t.nzp_kvar,t.nzp_frm,0 nzp_prm,t.nzp_frm_typ,t.procent,t.procent,97,t.procent" +
                " ,t.nzp_period,t.dat_s,t.dat_po,t.cntd,t.cntd_mn " +
                " from t_97 t " +
                " where 1=1 "
                , true);

            if (!ret.result)
            {
                CalcGkuXX_CloseTmp(conn_db);
                return false;
            }
            #endregion положить в таблицу расходов
            ExecSQL(conn_db, " drop table t_97 ", false);

            return true;
        }
    }

        #endregion Тип расхода =97 - процент удержания
    #endregion Полный цикл от выборки тарифов и расходов , до сохранения в БД


}
#endregion Здесь производится заполнение таблиц Calc_gku_XX