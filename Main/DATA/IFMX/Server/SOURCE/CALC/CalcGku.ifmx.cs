#region Здесь производится заполнение таблиц Calc_gku_XX
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
    #region Полный цикл от выборки тарифов и расходов , до сохранения в БД
    public partial class DbCalc : DbCalcClient
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
        struct Gku
        {
            public ParamCalc paramcalc;
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

            #region Заполнялка структуры gku 
            public Gku (ParamCalc _paramcalc)
            {
                paramcalc = _paramcalc;
                paramcalc.b_dom_in = true;

#if PG
calc_charge_bd  = paramcalc.pref + "_charge_" + (paramcalc.calc_yy - 2000).ToString("00");
                cur_charge_bd   = paramcalc.pref + "_charge_" + (paramcalc.cur_yy  - 2000).ToString("00");
                calc_gku_tab    = "calc_gku" + paramcalc.alias + "_" + paramcalc.calc_mm.ToString("00");
                calc_gku_xx     = calc_charge_bd + "." + calc_gku_tab;
                counters_xx     = calc_charge_bd + ".counters" + paramcalc.alias + "_" + paramcalc.calc_mm.ToString("00");
                gil_xx          = calc_charge_bd + ".gil" + paramcalc.alias + "_" + paramcalc.calc_mm.ToString("00");
                perekidka_xx    = cur_charge_bd + ".perekidka";
                prevSaldoMon_charge= paramcalc.pref + "_charge_" + (paramcalc.prev_calc_yy - 2000).ToString("00") + ".charge_"     + paramcalc.prev_calc_mm.ToString("00");
                curSaldoMon_charge = paramcalc.pref + "_charge_" + (paramcalc.calc_yy - 2000).ToString("00") + ".charge_"          + paramcalc.calc_mm.ToString("00");
                calc_tosupplXX     = paramcalc.pref + "_charge_" + (paramcalc.calc_yy      - 2000).ToString("00") + ".to_supplier" + paramcalc.calc_mm.ToString("00");
                calc_lnkchargeXX   = paramcalc.pref + "_charge_" + (paramcalc.calc_yy      - 2000).ToString("00") + ".lnk_charge_" + paramcalc.calc_mm.ToString("00");
#else
                calc_charge_bd = paramcalc.pref + "_charge_" + (paramcalc.calc_yy - 2000).ToString("00");
                cur_charge_bd = paramcalc.pref + "_charge_" + (paramcalc.cur_yy - 2000).ToString("00");
                calc_gku_tab = "calc_gku" + paramcalc.alias + "_" + paramcalc.calc_mm.ToString("00");
                calc_gku_xx = calc_charge_bd + ":" + calc_gku_tab;
                counters_xx = calc_charge_bd + ":counters" + paramcalc.alias + "_" + paramcalc.calc_mm.ToString("00");
                gil_xx = calc_charge_bd + ":gil" + paramcalc.alias + "_" + paramcalc.calc_mm.ToString("00");
                perekidka_xx = cur_charge_bd + ":perekidka";
                prevSaldoMon_charge = paramcalc.pref + "_charge_" + (paramcalc.prev_calc_yy - 2000).ToString("00") + ":charge_" + paramcalc.prev_calc_mm.ToString("00");
                curSaldoMon_charge = paramcalc.pref + "_charge_" + (paramcalc.calc_yy - 2000).ToString("00") + ":charge_" + paramcalc.calc_mm.ToString("00");
                calc_tosupplXX = paramcalc.pref + "_charge_" + (paramcalc.calc_yy - 2000).ToString("00") + ":to_supplier" + paramcalc.calc_mm.ToString("00");
                calc_lnkchargeXX = paramcalc.pref + "_charge_" + (paramcalc.calc_yy - 2000).ToString("00") + ":lnk_charge_" + paramcalc.calc_mm.ToString("00");
#endif
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
            ret = ExecSQL(conn_db, "  set search_path to '" + gku.calc_charge_bd+"'", true);
#else
            ret = ExecSQL(conn_db, " Database " + gku.calc_charge_bd, true);
#endif

            ret = ExecSQL(conn_db,
                " Create table " + tbluser + gku.calc_gku_tab +
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
                "    nzp_frm_typ    integer       default 0 not null, " +
                "    nzp_frm_typrs  integer       default 0 not null ) "
                , true);
            if (!ret.result)
            {
                ///*conn_db.Close();*/
                return;
            }

            ret = ExecSQL(conn_db, " create unique index " + tbluser + "ix1_" + gku.calc_gku_tab + " on " + gku.calc_gku_tab + " (nzp_clc)", true);
            if (!ret.result) { return; }
            ret = ExecSQL(conn_db, " create        index " + tbluser + "ix2_" + gku.calc_gku_tab + " on " + gku.calc_gku_tab + " (nzp_kvar,nzp_frm,dat_charge)", true);
            if (!ret.result) { return; }
            ret = ExecSQL(conn_db, " create        index " + tbluser + "ix3_" + gku.calc_gku_tab + " on " + gku.calc_gku_tab + " (nzp_dom)", true);
            if (!ret.result) { return; }

            ExecSQL(conn_db, sUpdStat + " " + gku.calc_gku_tab, true);

        }
        #endregion Создание таблицы расходов если нет , чистка с шагом 100000 если есть

        // Основные действия разбора формул расчета здесь
        #region Непосредственно подготовка расходов для расчета (Главная функция)
        //-----------------------------------------------------------------------------
        public bool CalcGkuXX(IDbConnection conn_db, ParamCalc paramcalc, out Returns ret)
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
                s_dat_charge = MDY(gku.paramcalc.cur_mm, 28, gku.paramcalc.cur_yy)+",";
                f_dat_charge = "dat_charge,";
                d_dat_charge = " and dat_charge = "+ MDY(gku.paramcalc.cur_mm, 28, gku.paramcalc.cur_yy);
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
            //

            // ... для расчета дотаций ...
            // ... !!! временно до появления переменной в Point.* !!! ... 
            ret = ExecRead(conn_db, out reader,
                " select count(*) cnt " +
                " from " + gku.paramcalc.data_alias + "prm_5 p " +
                " where p.nzp_prm=1992 and p.val_prm='1' " +
                " and p.is_actual<>100 and p.dat_s<" + gku.dat_po + " and p.dat_po>=" + gku.dat_s + " "
            , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*/conn_db.Close();// */     return false; }

            bool bIsCalcSubsidy = false;
            if (reader.Read())
            {
                bIsCalcSubsidy = (Convert.ToInt32(reader["cnt"]) > 0);
            }
            reader.Close();
            // ... Saha
            bool bIsSaha = false;
            object count = ExecScalar(conn_db,
                " Select count(*) From " + Points.Pref + "_data" + tableDelimiter + "prm_5 Where nzp_prm=9000 and is_actual<>100 ",
                out ret, true);
            if (ret.result)
            {
                try
                {
                    bIsSaha = (Convert.ToInt32(count) > 0);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = ex.Message;
                    MonitorLog.WriteLog(ret.text, MonitorLog.typelog.Error, 1, 222, true);
                    DropTempTablesRahod(conn_db, gku.paramcalc.pref);
                    return false;
                }
            }
            #endregion Подготовка к расчету расходов

            #region Выбрать параметры лицевого счета на дату t_par1
            // выбрать все параметры л/с на дату расчета
            ret = ExecSQL(conn_db,
                " create temp table t_par1 (" +
                " nzp      integer," +
                " nzp_prm  integer," +
                " val      CHAR(20)" +
#if PG
                " )  "
#else
                " ) with no log "
#endif
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*/conn_db.Close();//  */   return false; }

            ret = ExecSQL(conn_db,
                " insert into t_par1 (nzp,nzp_prm,val) " +
                " select p.nzp,p.nzp_prm, p.val_prm " +
                " from ttt_prm_1 p, t_opn k " +
                " where p.nzp = k.nzp_kvar " 
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*/conn_db.Close();// */ return false; }

            ret = ExecSQL(conn_db, " create index ixt_par1 on t_par1(nzp,nzp_prm) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*/conn_db.Close();/*/ return false; }

            ret = ExecSQL(conn_db, sUpdStat + " t_par1 ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*/conn_db.Close();// */ return false; }

            #endregion Выбрать параметры лицевого счета  на дату t_par1

            #region Учесть параметры коммунальных квартир
            // выбрать параметры л/с для расчета коммуналок
            ret = ExecSQL(conn_db,
                " create temp table t_rsh_kmnl (" +
                " nzp_key  serial," +
                " nzp_kvar integer," +
                " nzp_prm  integer default 0," +
                " rashod   " + sDecimalType + "(10,8) default 0," +
                " val_prm  " + sDecimalType + "(8,2) default 0" +
#if PG
                " )  "
#else
                " ) with no log "
#endif
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*/conn_db.Close();/*/ return false; }
            // комфортность =3 , кол лиц счетов в коммунальной кв =21
            ret = ExecSQL(conn_db,
                " insert into t_rsh_kmnl (nzp_key,nzp_kvar,nzp_prm,rashod,val_prm)" +
                " select 0,t.nzp_kvar,21,1/max(p.val" + sConvToNum + "),max(p.val" + sConvToNum + ") " +
                " from t_opn t,t_par1 p,t_par1 p1" +
                " where t.nzp_kvar=p.nzp  and p.nzp_prm =21 and p.val" + sConvToNum + " >0  " +
                "   and t.nzp_kvar=p1.nzp and p1.nzp_prm=3  and p1.val='2'" +
                " group by 2 "
                , true);
            
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db);/* //conn_db.Close();/*/ return false; }

            ret = ExecSQL(conn_db, " create index ixt_rsh_kmnl on t_rsh_kmnl(nzp_kvar) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db);/* //conn_db.Close();/*/ return false; }

            ret = ExecSQL(conn_db, sUpdStat + " t_rsh_kmnl ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*/conn_db.Close();/*/ return false; }

            // выбрать параметры л/с для расчета коммуналок
            ret = ExecSQL(conn_db,
                " create temp table t_kmnl (" +
                " nzp_kvar integer" +
#if PG
                " ) "
#else
                " ) with no log "
#endif
, true);

            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*/conn_db.Close();/*/ return false; }

            ret = ExecSQL(conn_db,
                " insert into t_kmnl (nzp_kvar)" +
                " select t.nzp_kvar from t_opn t,t_par1 p" +
                " where t.nzp_kvar=p.nzp and p.nzp_prm =3 and p.val='2'  " +
                " group by 1 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /* //conn_db.Close();/*/ return false; }

            ret = ExecSQL(conn_db, " create index ixt_kmnl on t_kmnl(nzp_kvar) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*/conn_db.Close();// */ return false; }

            ret = ExecSQL(conn_db, sUpdStat + " t_kmnl ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*/conn_db.Close();/*/ return false; }

            #endregion Учесть параметры коммунальных квартир

            #region Учесть приватизированные квартиры

            ret = ExecSQL(conn_db,
                " create temp table t_rsh_privat (" +
                " nzp_kvar integer" +
#if PG
                " ) "
#else
                " ) with no log "
#endif
                , true);

            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*/conn_db.Close();/*/ return false; }
            // 8 приватизировано
            ret = ExecSQL(conn_db,
                " insert into t_rsh_privat (nzp_kvar)" +
                " select t.nzp_kvar from t_opn t,t_par1 p" +
                " where t.nzp_kvar=p.nzp and p.nzp_prm=8" +
                " group by 1 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*/conn_db.Close();/*/ return false; }

            ret = ExecSQL(conn_db, " create index ixt_rsh_privat on t_rsh_privat(nzp_kvar) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*/conn_db.Close();/*/ return false; }

            ret = ExecSQL(conn_db, sUpdStat + " t_rsh_privat ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*/conn_db.Close();/*/ return false; }

            #endregion Учесть приватизированные квартиры

            #region Учесть параметры квартир у которых есть наем

            ret = ExecSQL(conn_db,
                " create temp table t_rsh_naem (" +
                " nzp_kvar integer," +
                " nzp_prm  integer default 0" +
#if PG
                " ) "
#else
                " ) with no log "
#endif
                , true);

            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*/conn_db.Close();/*/ return false; }
            // 8 приватизировано 34 ветхий фонд
            ret = ExecSQL(conn_db,
                " insert into t_rsh_naem (nzp_kvar,nzp_prm)" +
                " select t.nzp_kvar,p.nzp_prm from t_opn t,t_par1 p" +
                " where t.nzp_kvar=p.nzp  and p.nzp_prm in (8,34)" +
                " group by 1,2 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*/conn_db.Close();/*/ return false; }

            ret = ExecSQL(conn_db, " create index ixt_rsh_naem on t_rsh_naem(nzp_kvar) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*/conn_db.Close();/*/ return false; }

            ret = ExecSQL(conn_db, sUpdStat + " t_rsh_naem ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*/conn_db.Close();/*/ return false; }

            #endregion Учесть параметры квартир у которых есть наем

            #region Подсчитать количество жильцов 
            ret = ExecSQL(conn_db,
                " create temp table t_rsh_kolgil (" +
                " nzp_kvar integer," +
                " kg   " + sDecimalType + "(11,7) default 0," +
                " kg_g " + sDecimalType + "(11,7) default 0," +
                " kvp  " + sDecimalType + "(11,7) default 0," +
                " knpl " + sDecimalType + "(11,7) default 0" +
#if PG
                " )  "
#else
                " ) with no log "
#endif
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*/conn_db.Close();/*/ return false; }

            // gil_xx - cnt1 - целое число жильцов без учета правила 15 дней, но с учетом временно выбывших
            // gil_xx - cnt2 - кол-во жильцов с учетом правила 15 дней без учета временно выбывших
            // gil_xx - val1 - дробное число жильцов без учета правила 15 дней, но с учетом временно выбывших
            // gil_xx - val2 - кол-во жильцов цифрой из БД (выбран nzp_prm=5 из prm_1)
            // gil_xx - val3 - кол-во временно прибывших (выбран nzp_prm=131 из prm_1)
            // gil_xx - val5 - временно выбывших (nzp_prm=10 - это всегда из gil_periods / м.б. nzp_prm=10 ! по признаку)
            ret = ExecSQL(conn_db,
                " insert into t_rsh_kolgil (nzp_kvar,kg,kvp,kg_g)" +
                " select t.nzp_kvar,max(g.cnt2 - g.val5),max(g.val3),max(g.cnt2)" +
                " from t_opn t," + gku.gil_xx + " g " +
                " where t.nzp_kvar=g.nzp_kvar and g.stek=3" +
                " group by 1 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/  return false; }

            ret = ExecSQL(conn_db, " create index ixt_rsh_kolgil on t_rsh_kolgil (nzp_kvar) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/  return false; }

            ret = ExecSQL(conn_db, sUpdStat + " t_rsh_kolgil ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            ret = ExecSQL(conn_db,
                " update t_rsh_kolgil set knpl=" +
                " (select max(p.val" + sConvToNum + ") from t_par1 p where t_rsh_kolgil.nzp_kvar=p.nzp and p.nzp_prm=9) "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            ret = ExecSQL(conn_db, " update t_rsh_kolgil set knpl=0 where knpl is null ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/  return false; }

            #endregion Подсчитать количество жильцов

            #region Учесть особенности лифта (этажность, начисление на нижних этажах, нет учета количества не пользующихся странно)
            ret = ExecSQL(conn_db,
                " create temp table t_rsh_lift (" +
                " nzp_kvar integer," +
                " nzp_dom integer," +
                " etag     integer default 0," +
                " calc3    integer default 0" +
#if PG
                " )  "
#else
                " ) with no log "
#endif
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

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
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            ret = ExecSQL(conn_db, " create index ixt_rsh_lift1 on t_rsh_lift(nzp_dom) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
            ret = ExecSQL(conn_db, sUpdStat + " t_rsh_lift ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            ret = ExecSQL(conn_db,
                " update t_rsh_lift set calc3=1 " +
                " where 0<(select count(*) " +
                " from ttt_prm_2 p " +
                " where p.nzp = t_rsh_lift.nzp_dom and p.nzp_prm=787) "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            ret = ExecSQL(conn_db,
                " delete from t_rsh_lift where calc3=0 and etag<3 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*/conn_db.Close();/*/ return false; }

            ret = ExecSQL(conn_db, " create index ixt_rsh_lift2 on t_rsh_lift(nzp_kvar) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*/conn_db.Close();/*/ return false; }

            ret = ExecSQL(conn_db, sUpdStat + " t_rsh_lift ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*/conn_db.Close();/*/ return false; }

            #endregion Учесть особенности лифта (этажность, начисление на нижних этажах, нет учета количества не пользующихся странно)

            #region Предварительно считать все тарифы по выбранным лицевым счетам 

            #region выбрать только нужные тарифы t_tarifs
#if PG
            ret = ExecSQL(conn_db,
                            " create temp table t_tarifs (" +
                //   " create table t_tarifs (" +
                            " nzp_key  serial not null," +
                            " nzp_dom  integer not null," +
                            " nzp_kvar integer not null," +
                            " nzp_serv integer not null," +
                            " nzp_supp integer not null," +
                            " nzp_frm  integer not null," +
                            " dat_s    DATE not null," +
                            " dat_po   DATE not null" +
                            " )  "
                //" )  "
                            , true);
#else
ret = ExecSQL(conn_db,
                " create temp table t_tarifs (" +
               //   " create table t_tarifs (" +
                " nzp_key  serial not null," +
                " nzp_dom  integer not null," +
                " nzp_kvar integer not null," +
                " nzp_serv integer not null," +
                " nzp_supp integer not null," +
                " nzp_frm  integer not null," +
                " dat_s    DATE not null," +
                " dat_po   DATE not null" +
                " ) with no log "
                //" )  "
                , true);
#endif
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*/conn_db.Close();/*/ return false; }

#if PG
            ret = ExecSQL(conn_db,
                          " insert into t_tarifs (nzp_key,nzp_dom,nzp_kvar,nzp_serv,nzp_supp,nzp_frm,dat_s,dat_po)" +
                          " select 0,k.nzp_dom,t.nzp_kvar,t.nzp_serv,t.nzp_supp,max(t.nzp_frm),min(t.dat_s),max(t.dat_po)" +
                          " from temp_table_tarif t,t_opn k" +
                          " where t.nzp_kvar=k.nzp_kvar" +
                          " group by 2,3,4,5 "
                          , true);
#else
  ret = ExecSQL(conn_db,
                " insert into t_tarifs (nzp_key,nzp_dom,nzp_kvar,nzp_serv,nzp_supp,nzp_frm,dat_s,dat_po)" +
                " select 0,k.nzp_dom,t.nzp_kvar,t.nzp_serv,t.nzp_supp,max(t.nzp_frm),min(t.dat_s),max(t.dat_po)"+
                " from temp_table_tarif t,t_opn k" +
                " where t.nzp_kvar=k.nzp_kvar" +
                " group by 2,3,4,5 "
                , true);
#endif
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*/conn_db.Close();/*/ return false; }
#if PG
            ret = ExecSQL(conn_db, " create  index ixt_tarifs1 on t_tarifs(nzp_key) ", true);
#else
            ret = ExecSQL(conn_db, " create unique index ixt_tarifs1 on t_tarifs(nzp_key) ", true);
#endif
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*/conn_db.Close();/*/ return false; }

            ret = ExecSQL(conn_db, " create index ixt_tarifs2 on t_tarifs(nzp_kvar,nzp_frm) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*/conn_db.Close();/*/ return false; }

            ret = ExecSQL(conn_db, " create index ixt_tarifs3 on t_tarifs(nzp_kvar,nzp_serv,nzp_supp,nzp_frm) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*/conn_db.Close();/*/ return false; }

            ret = ExecSQL(conn_db, " create index ixt_tarifs4 on t_tarifs(nzp_dom) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*/conn_db.Close();/*/ return false; }
#if PG
            ret = ExecSQL(conn_db, " analyze t_tarifs ", true);
#else
            ret = ExecSQL(conn_db, " update statistics for table t_tarifs ", true);
#endif
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*/conn_db.Close();/*/ return false; }

            #endregion выбрать только нужные тарифы t_tarifs

            #region выбрать уникальные тарифы в t_trf
            ret = ExecSQL(conn_db,
                " create temp table t_trf (" +
                " nzp_dom  integer," +
                " nzp_kvar integer," +
                " nzp_supp integer," +
                " nzp_frm  integer " +
#if PG
                " )  "
#else
                " ) with no log "
#endif
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*/conn_db.Close();/*/ return false; }

            ret = ExecSQL(conn_db,
                " insert into t_trf (nzp_dom,nzp_kvar,nzp_supp,nzp_frm)" +
                " select nzp_dom,nzp_kvar,nzp_supp,nzp_frm from t_tarifs" +
                " group by 1,2,3,4 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*/conn_db.Close();/*/ return false; }

            ret = ExecSQL(conn_db, " create index ixt_trf1 on t_trf(nzp_kvar,nzp_supp,nzp_frm) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*/conn_db.Close();/*/ return false; }

            ret = ExecSQL(conn_db, " create index ixt_trf2 on t_trf(nzp_dom) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*/conn_db.Close();/*/ return false; }

            ret = ExecSQL(conn_db, sUpdStat + " t_trf ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*/conn_db.Close();/*/ return false; }
            #endregion выбрать уникальные тарифы в t_trf

            #endregion Предварительно считать все тарифы по выбранным лицевым счетам

//
//          Выборка тарифов
//
            
            #region создать сводную таблицу t_prm (тарифы)
            
            #region создание таблицы t_prm и t_par1d
            ret = ExecSQL(conn_db,
                //" create  table t_prm (" +
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
                " nzp_frm_typ integer default 0 not null" +
#if PG
                " ) "
#else
                //" )  "
                " ) with no log "
#endif
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*/conn_db.Close();/*/ return false; }

            // выбрать все параметры/тарифы л/с          //уточнение которые связаны с формулами 
            ret = ExecSQL(conn_db,
                " create temp table t_par1d (" +
                " nzp      integer," +
                " nzp_prm  integer," +
                " vald     " + sDecimalType + "(14,4) default 0" +
#if PG
                " )  "
#else
                " ) with no log "
#endif
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*/conn_db.Close();/*/ return false; }
            #endregion создание таблицы t_prm и t_par1d
            
            #region Выборка тарифов для домовых квартирных тарифов t_par1d

            ret = ExecSQL(conn_db,
                " insert into t_par1d (nzp,nzp_prm,vald)" +
                " select p.nzp,p.nzp_prm, max(p.val" + sConvToNum + ") from " + gku.paramcalc.kernel_alias + "formuls_opis f,t_par1 p" +
                " where p.nzp_prm=f.nzp_prm_tarif_ls" +
                " group by 1,2 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*/conn_db.Close();/*/ return false; }

            ret = ExecSQL(conn_db,
                " insert into t_par1d (nzp,nzp_prm,vald)" +
                " select p.nzp,p.nzp_prm, max(p.val" + sConvToNum + ") from " + gku.paramcalc.kernel_alias + "formuls_opis f,t_par1 p" +
                " where p.nzp_prm=f.nzp_prm_tarif_lsp" +
                " group by 1,2 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*/conn_db.Close();/*/ return false; }

            #endregion Выборка тарифов для домовых квартирных тарифов t_par1d
            
            #region Доп выборка в t_par1d
            // ... для расчета дотаций ...
            if (bIsCalcSubsidy)
            {
#if PG
                ret = ExecSQL(conn_db,
                                   " insert into t_par1d (nzp,nzp_prm,vald)" +
                                   " select p.nzp,p.nzp_prm, max(p.val::numeric) from " + gku.paramcalc.pref + "_kernel.formuls_ops_dt f,t_par1 p" +
                                   " where p.nzp_prm=f.nzp_prm_trf_ls" +
                                   " group by 1,2 "
                                   , true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*/conn_db.Close();/*/ return false; }
                ret = ExecSQL(conn_db,
                    " insert into t_par1d (nzp,nzp_prm,vald)" +
                    " select p.nzp,p.nzp_prm, max(p.val::numeric) from " + gku.paramcalc.pref + "_kernel.formuls_ops_dt f,t_par1 p" +
                    " where p.nzp_prm=f.nzp_prm_trf_lsp" +
                    " group by 1,2 "
                    , true);
#else
                ret = ExecSQL(conn_db,
                    " insert into t_par1d (nzp,nzp_prm,vald)" +
                    " select p.nzp,p.nzp_prm, max(p.val+0) from " + gku.paramcalc.pref + "_kernel:formuls_ops_dt f,t_par1 p" +
                    " where p.nzp_prm=f.nzp_prm_trf_ls" +
                    " group by 1,2 "
                    , true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*/conn_db.Close();/*/ return false; }
                ret = ExecSQL(conn_db,
                    " insert into t_par1d (nzp,nzp_prm,vald)" +
                    " select p.nzp,p.nzp_prm, max(p.val+0) from " + gku.paramcalc.pref + "_kernel:formuls_ops_dt f,t_par1 p" +
                    " where p.nzp_prm=f.nzp_prm_trf_lsp" +
                    " group by 1,2 "
                    , true);
#endif
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*/conn_db.Close();/*/ return false; }
            }
            ret = ExecSQL(conn_db, " create index ixt_par1d on t_par1d(nzp,nzp_prm) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*/conn_db.Close();/*/ return false; }
#if PG
            ret = ExecSQL(conn_db, " analyze t_par1d ", true);
#else
            ret = ExecSQL(conn_db, " update statistics for table t_par1d ", true);
#endif
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*/conn_db.Close();/*/ return false; }
            #endregion Доп выборка в t_par1d
            //1
            #region Тип расчета 1 (тариф * расход)
            // тип расчета = 1
            // тариф на всю базу - в последнюю очередь //(здесь очередь priority )
            ret = ExecSQL(conn_db,
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,tarif,priority,nzp_frm_typ)" +
                " select  t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,max(p.val_prm" + sConvToNum + "),0,1" +
                " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f," + gku.paramcalc.data_alias + "prm_5 p" +
                " where t.nzp_frm=f.nzp_frm and p.nzp_prm=f.nzp_prm_tarif_bd and f.nzp_frm_typ=1" +
                " and p.is_actual<>100 and p.dat_s<" + gku.dat_po + " and p.dat_po>=" + gku.dat_s +
                " group by 1,2,3,4,5 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*/conn_db.Close();/*/ return false; }

            // тариф на поставщика - во третью очередь
            ret = ExecSQL(conn_db,
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,tarif,priority,nzp_frm_typ)" +
                " select t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,max(p.val_prm" + sConvToNum + "),1,1" +
                " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f," + gku.paramcalc.data_alias + "prm_11 p" +
                " where t.nzp_frm=f.nzp_frm and t.nzp_supp=p.nzp and p.nzp_prm=f.nzp_prm_tarif_su and f.nzp_frm_typ=1" +
                "  and p.is_actual<>100 and p.dat_s<" + gku.dat_po + " and p.dat_po>=" + gku.dat_s +
                // наличие ЭОТ на поставщика - nzp_prm=336 - если есть, то во вторую очередь
                " and 0<(select count(*) from " + gku.paramcalc.data_alias + "prm_5 p1" +
                "     where p1.nzp_prm=336 and p1.is_actual<>100 and p1.dat_s<" + gku.dat_po + " and p1.dat_po>=" + gku.dat_s + ") " +
                " group by 1,2,3,4,5 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*/conn_db.Close();/*/ return false; }

            // тариф на дом - во вторую очередь
            ret = ExecSQL(conn_db,
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,tarif,priority,nzp_frm_typ)" +
                " select t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,max(p.val_prm" + sConvToNum + "),2,1" +
                " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f,ttt_prm_2 p" +
                " where t.nzp_frm=f.nzp_frm and t.nzp_dom=p.nzp and p.nzp_prm=f.nzp_prm_tarif_dm and f.nzp_frm_typ=1" +
                " group by 1,2,3,4,5 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*/conn_db.Close();/*/ return false; }

            // тариф на л/с - во первую очередь
            string ssql =
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,nzp_prm1,tarif,priority,nzp_frm_typ)" +
                " select t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,f.nzp_prm_tarif_lsp," +
                " p.vald + " + sNvlWord + "("+
                "( select p1.vald from t_par1d p1 where t.nzp_kvar=p1.nzp and p1.nzp_prm=f.nzp_prm_tarif_lsp )" +
                ",0),3,1" +
                " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f, t_par1d p " +
                " where t.nzp_frm=f.nzp_frm and t.nzp_kvar=p.nzp and p.nzp_prm=f.nzp_prm_tarif_ls" +
                " and f.nzp_frm_typ=1";
            ret = ExecSQL(conn_db, ssql, true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*/conn_db.Close();/*/ return false; }

            #endregion Тип расчета 1 (тариф * расход)
            //2
            #region Тип Расчета 306 (с учетом параметров лифта -начисление на нижних этажах , заранее подготовлена табл t_rsh_lift)
            // тип расчета = 306 - разрешать расчет лифта на нижних этажах!
            // тариф на всю базу - в последнюю очередь
            ret = ExecSQL(conn_db,
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,tarif,priority,nzp_frm_typ)" +
                " select  t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,max(p.val_prm" + sConvToNum + "),0,306" +
                " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f," + gku.paramcalc.data_alias + "prm_5 p" +
                " where t.nzp_frm=f.nzp_frm and p.nzp_prm=f.nzp_prm_tarif_bd and f.nzp_frm_typ=306" +
                " and p.is_actual<>100 and p.dat_s<" + gku.dat_po + " and p.dat_po>=" + gku.dat_s +
                " and 0<(select count(*) from t_rsh_lift n where t.nzp_kvar=n.nzp_kvar)" +
                " group by 1,2,3,4,5 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*/conn_db.Close();/*/ return false; }

            // тариф на поставщика - во третью очередь
            ret = ExecSQL(conn_db,
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,tarif,priority,nzp_frm_typ)" +
                " select t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,max(p.val_prm" + sConvToNum + "),1,306" +
                " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f," + gku.paramcalc.data_alias + "prm_11 p" +
                " where t.nzp_frm=f.nzp_frm and t.nzp_supp=p.nzp and p.nzp_prm=f.nzp_prm_tarif_su and f.nzp_frm_typ=306" +
                " and p.is_actual<>100 and p.dat_s<" + gku.dat_po + " and p.dat_po>=" + gku.dat_s +
                " and 0<(select count(*) from t_rsh_lift n where t.nzp_kvar=n.nzp_kvar)" +
                // наличие ЭОТ на поставщика - nzp_prm=336 - если есть, то во вторую очередь
                " and 0<(select count(*) from " + gku.paramcalc.data_alias + "prm_5 p1" +
                "     where p1.nzp_prm=336 and p1.is_actual<>100 and p1.dat_s<" + gku.dat_po + " and p1.dat_po>=" + gku.dat_s + ") " +
                " group by 1,2,3,4,5 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*/conn_db.Close();/*/ return false; }

            // тариф на дом - во вторую очередь
            ret = ExecSQL(conn_db,
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,tarif,priority,nzp_frm_typ)" +
                " select t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,max(p.val_prm" + sConvToNum + "),2,306" +
                " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f,ttt_prm_2 p" +
                " where t.nzp_frm=f.nzp_frm and t.nzp_dom=p.nzp and p.nzp_prm=f.nzp_prm_tarif_dm and f.nzp_frm_typ=306" +
                " and 0<(select count(*) from t_rsh_lift n where t.nzp_kvar=n.nzp_kvar)" +
                " group by 1,2,3,4,5 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*/conn_db.Close();/*/ return false; }

#if PG
            // тариф на л/с - во первую очередь
            ret = ExecSQL(conn_db,
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,nzp_prm1,tarif,priority,nzp_frm_typ)" +
                " select t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,coalesce(p1.nzp_prm,0),p.vald+coalesce(p1.vald,0),3,306" +
                " from " + gku.paramcalc.pref + "_kernel.formuls_opis f," +
                " t_trf t left outer join t_par1d p1 on t.nzp_kvar=p1.nzp, " +
                "t_par1d p " +
                " where t.nzp_frm=f.nzp_frm and t.nzp_kvar=p.nzp and p.nzp_prm=f.nzp_prm_tarif_ls and p1.nzp_prm=f.nzp_prm_tarif_lsp " +
                " and f.nzp_frm_typ=306" +
                " and 0<(select count(*) from t_rsh_lift n where t.nzp_kvar=n.nzp_kvar)"
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*/conn_db.Close();/*/ return false; }
#else
            // тариф на л/с - во первую очередь
            ret = ExecSQL(conn_db,
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,nzp_prm1,tarif,priority,nzp_frm_typ)" +
                " select t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,nvl(p1.nzp_prm,0),p.vald+nvl(p1.vald,0),3,306" +
                " from t_trf t," + gku.paramcalc.pref + "_kernel:formuls_opis f,t_par1d p,outer t_par1d p1" +
                " where t.nzp_frm=f.nzp_frm and t.nzp_kvar=p.nzp and p.nzp_prm=f.nzp_prm_tarif_ls and t.nzp_kvar=p1.nzp and p1.nzp_prm=f.nzp_prm_tarif_lsp and f.nzp_frm_typ=306" +
                " and 0<(select count(*) from t_rsh_lift n where t.nzp_kvar=n.nzp_kvar)"
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*/conn_db.Close();/*/ return false; }
#endif

            #endregion Тип Расчета 306 (с учетом параметров лифта -начисление на нижних этажах , заранее подготовлена табл t_rsh_lift)
            //3
            #region Тип расчета 101 (просто наем)
            // тип расчета = 101

#if PG
            ret = ExecSQL(conn_db,
                           " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,tarif,priority,nzp_frm_typ)" +
                           " select  t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,max(p.val_prm::numeric),0,101" +
                           " from t_trf t," + gku.paramcalc.pref + "_kernel.formuls_opis f," + gku.paramcalc.pref + "_data.prm_5 p" +
                           " where t.nzp_frm=f.nzp_frm and p.nzp_prm=f.nzp_prm_tarif_bd and f.nzp_frm_typ=101" +
                           " and p.is_actual<>100 and p.dat_s<" + gku.dat_po + " and p.dat_po>=" + gku.dat_s +
                           " and 0=(select count(*) from t_rsh_naem n where t.nzp_kvar=n.nzp_kvar)" +
                           " group by 1,2,3,4,5 "
                           , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*/conn_db.Close();/*/ return false; }
            ret = ExecSQL(conn_db,
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,tarif,priority,nzp_frm_typ)" +
                " select t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,max(p.val_prm::numeric),1,101" +
                " from t_trf t," + gku.paramcalc.pref + "_kernel.formuls_opis f," + gku.paramcalc.pref + "_data.prm_11 p" +
                " where t.nzp_frm=f.nzp_frm and t.nzp_supp=p.nzp and p.nzp_prm=f.nzp_prm_tarif_su and f.nzp_frm_typ=101" +
                "  and p.is_actual<>100 and p.dat_s<" + gku.dat_po + " and p.dat_po>=" + gku.dat_s +
                " and 0=(select count(*) from t_rsh_naem n where t.nzp_kvar=n.nzp_kvar)" +
                // наличие ЭОТ на поставщика - nzp_prm=336 - если есть, то во вторую очередь
                " and 0<(select count(*) from " + gku.paramcalc.pref + "_data.prm_5 p1" +
                "     where p1.nzp_prm=336 and p1.is_actual<>100 and p1.dat_s<" + gku.dat_po + " and p1.dat_po>=" + gku.dat_s + ") " +
                " group by 1,2,3,4,5 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*/conn_db.Close();/*/ return false; }
            ret = ExecSQL(conn_db,
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,tarif,priority,nzp_frm_typ)" +
                " select t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,max(p.val_prm::numeric),2,101" +
                " from t_trf t," + gku.paramcalc.pref + "_kernel.formuls_opis f,ttt_prm_2 p" +
                " where t.nzp_frm=f.nzp_frm and t.nzp_dom=p.nzp and p.nzp_prm=f.nzp_prm_tarif_dm and f.nzp_frm_typ=101" +
                " and 0=(select count(*) from t_rsh_naem n where t.nzp_kvar=n.nzp_kvar)" +
                " group by 1,2,3,4,5 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*/conn_db.Close();/*/ return false; }
            ret = ExecSQL(conn_db,
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,tarif,priority,nzp_frm_typ)" +
                " select t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,p.vald,3,101" +
                " from t_trf t," + gku.paramcalc.pref + "_kernel.formuls_opis f,t_par1d p" +
                " where t.nzp_frm=f.nzp_frm and t.nzp_kvar=p.nzp and p.nzp_prm=f.nzp_prm_tarif_ls and f.nzp_frm_typ=101" +
                " and 0=(select count(*) from t_rsh_naem n where t.nzp_kvar=n.nzp_kvar) "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*/conn_db.Close();/*/ return false; }
#else
 ret = ExecSQL(conn_db,
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,tarif,priority,nzp_frm_typ)" +
                " select  t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,max(p.val_prm),0,101" +
                " from t_trf t," + gku.paramcalc.pref + "_kernel:formuls_opis f," + gku.paramcalc.pref + "_data:prm_5 p" +
                " where t.nzp_frm=f.nzp_frm and p.nzp_prm=f.nzp_prm_tarif_bd and f.nzp_frm_typ=101" +
                " and p.is_actual<>100 and p.dat_s<" + gku.dat_po + " and p.dat_po>=" + gku.dat_s +
                " and 0=(select count(*) from t_rsh_naem n where t.nzp_kvar=n.nzp_kvar)" +
                " group by 1,2,3,4,5 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*/conn_db.Close();/*/ return false; }
            ret = ExecSQL(conn_db,
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,tarif,priority,nzp_frm_typ)" +
                " select t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,max(p.val_prm),1,101" +
                " from t_trf t," + gku.paramcalc.pref + "_kernel:formuls_opis f," + gku.paramcalc.pref + "_data:prm_11 p" +
                " where t.nzp_frm=f.nzp_frm and t.nzp_supp=p.nzp and p.nzp_prm=f.nzp_prm_tarif_su and f.nzp_frm_typ=101" +
                "  and p.is_actual<>100 and p.dat_s<" + gku.dat_po + " and p.dat_po>=" + gku.dat_s +
                " and 0=(select count(*) from t_rsh_naem n where t.nzp_kvar=n.nzp_kvar)" +
                // наличие ЭОТ на поставщика - nzp_prm=336 - если есть, то во вторую очередь
                " and 0<(select count(*) from " + gku.paramcalc.pref + "_data:prm_5 p1" +
                "     where p1.nzp_prm=336 and p1.is_actual<>100 and p1.dat_s<" + gku.dat_po + " and p1.dat_po>=" + gku.dat_s + ") " +
                " group by 1,2,3,4,5 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*/conn_db.Close();/*/ return false; }
            ret = ExecSQL(conn_db,
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,tarif,priority,nzp_frm_typ)" +
                " select t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,max(p.val_prm),2,101" +
                " from t_trf t," + gku.paramcalc.pref + "_kernel:formuls_opis f,ttt_prm_2 p" +
                " where t.nzp_frm=f.nzp_frm and t.nzp_dom=p.nzp and p.nzp_prm=f.nzp_prm_tarif_dm and f.nzp_frm_typ=101" +
                " and 0=(select count(*) from t_rsh_naem n where t.nzp_kvar=n.nzp_kvar)" +
                " group by 1,2,3,4,5 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*/conn_db.Close();/*/ return false; }
            ret = ExecSQL(conn_db,
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,tarif,priority,nzp_frm_typ)" +
                " select t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,p.vald,3,101" +
                " from t_trf t," + gku.paramcalc.pref + "_kernel:formuls_opis f,t_par1d p" +
                " where t.nzp_frm=f.nzp_frm and t.nzp_kvar=p.nzp and p.nzp_prm=f.nzp_prm_tarif_ls and f.nzp_frm_typ=101" +
                " and 0=(select count(*) from t_rsh_naem n where t.nzp_kvar=n.nzp_kvar) "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*/conn_db.Close();/*/ return false; }
#endif
            #endregion Тип расчета 101 (просто наем)

            #region Тип расчета 1056 (тариф только для приватизированных квартир)
            // тип расчета = 1056
            ret = ExecSQL(conn_db,
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,tarif,priority,nzp_frm_typ)" +
                " select  t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,max(p.val_prm" + sConvToNum + "),0,1056" +
                " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f," + gku.paramcalc.data_alias + "prm_5 p" +
                " where t.nzp_frm=f.nzp_frm and p.nzp_prm=f.nzp_prm_tarif_bd and f.nzp_frm_typ=1056" +
                " and p.is_actual<>100 and p.dat_s<" + gku.dat_po + " and p.dat_po>=" + gku.dat_s +
                " and 0<(select count(*) from t_rsh_privat n where t.nzp_kvar=n.nzp_kvar)" +
                " group by 1,2,3,4,5 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*/conn_db.Close();/*/ return false; }

            ret = ExecSQL(conn_db,
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,tarif,priority,nzp_frm_typ)" +
                " select t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,max(p.val_prm" + sConvToNum + "),1,1056" +
                " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f," + gku.paramcalc.data_alias + "prm_11 p" +
                " where t.nzp_frm=f.nzp_frm and t.nzp_supp=p.nzp and p.nzp_prm=f.nzp_prm_tarif_su and f.nzp_frm_typ=1056" +
                "  and p.is_actual<>100 and p.dat_s<" + gku.dat_po + " and p.dat_po>=" + gku.dat_s +
                " and 0<(select count(*) from t_rsh_privat n where t.nzp_kvar=n.nzp_kvar)" +
                // наличие ЭОТ на поставщика - nzp_prm=336 - если есть, то во вторую очередь
                " and 0<(select count(*) from " + gku.paramcalc.pref + "_data:prm_5 p1" +
                "     where p1.nzp_prm=336 and p1.is_actual<>100 and p1.dat_s<" + gku.dat_po + " and p1.dat_po>=" + gku.dat_s + ") " +
                " group by 1,2,3,4,5 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*/conn_db.Close();/*/ return false; }

            ret = ExecSQL(conn_db,
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,tarif,priority,nzp_frm_typ)" +
                " select t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,max(p.val_prm" + sConvToNum + "),2,1056" +
                " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f,ttt_prm_2 p" +
                " where t.nzp_frm=f.nzp_frm and t.nzp_dom=p.nzp and p.nzp_prm=f.nzp_prm_tarif_dm and f.nzp_frm_typ=1056" +
                " and 0<(select count(*) from t_rsh_privat n where t.nzp_kvar=n.nzp_kvar)" +
                " group by 1,2,3,4,5 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*/conn_db.Close();/*/ return false; }

            ret = ExecSQL(conn_db,
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,tarif,priority,nzp_frm_typ)" +
                " select t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,p.vald,3,1056" +
                " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f,t_par1d p" +
                " where t.nzp_frm=f.nzp_frm and t.nzp_kvar=p.nzp and p.nzp_prm=f.nzp_prm_tarif_ls and f.nzp_frm_typ=1056" +
                " and 0<(select count(*) from t_rsh_privat n where t.nzp_kvar=n.nzp_kvar) "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*/conn_db.Close();/*/ return false; }

            //ViewTbl(conn_db, " select * from t_par1d ");
            //ViewTbl(conn_db, " select * from t_prm ");

            #endregion Тип расчета 1056 (тариф только для приватизированных квартир)
            
            //4
            #region Тип расчета  400 (найм от базовой ставки)
            // тип расчета = 400 - найм от базовой ставки
            ExecSQL(conn_db, " Drop table t_prm400 ", false);


            ret = ExecSQL(conn_db,
                // " create table are.t_prm400 (" +
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
                " nzp_frm_typ integer default 0 not null" +
#if PG
                " )  "
                , true);
#else
                " ) with no log "
                // " ) "
                , true);
#endif
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*/conn_db.Close();/*/ return false; }

            ret = ExecSQL(conn_db,
                " insert into t_prm400 (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,tarif_bs,priority,nzp_frm_typ,nzp_frm_kod)" +
                " select t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,p.vald,0,400,f.nzp_frm_kod" +
                " from t_trf t," + paramcalc.kernel_alias + "formuls_opis f,t_par1d p" +
                " where t.nzp_frm=f.nzp_frm and t.nzp_kvar=p.nzp and p.nzp_prm=f.nzp_prm_tarif_ls and f.nzp_frm_typ=400" +
                " and 0=(select count(*) from t_rsh_naem n where t.nzp_kvar=n.nzp_kvar) "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*/conn_db.Close();/*/ return false; }

            ret = ExecSQL(conn_db,
                " insert into t_prm400 (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,tarif_bs,priority,nzp_frm_typ,nzp_frm_kod)" +
                " select t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,max(p.val_prm" + sConvToNum + "),1,400,f.nzp_frm_kod" +
                " from t_trf t," + paramcalc.kernel_alias + "formuls_opis f,ttt_prm_2 p" +
                " where t.nzp_frm=f.nzp_frm and t.nzp_dom=p.nzp and p.nzp_prm=f.nzp_prm_tarif_dm and f.nzp_frm_typ=400" +
                " and 0=(select count(*) from t_rsh_naem n where t.nzp_kvar=n.nzp_kvar) " +
                " group by 1,2,3,4,5,9 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*/conn_db.Close();/*/ return false; }

            ret = ExecSQL(conn_db, " create index ix1t_prm400 on t_prm400(nzp_dom) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
            ret = ExecSQL(conn_db, " create index ix2t_prm400 on t_prm400(nzp_kvar) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
            ret = ExecSQL(conn_db, " create index ix3t_prm400 on t_prm400(nzp_frm_kod) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
#if PG
            ret = ExecSQL(conn_db, " analyze t_prm400 ", true);
#else
            ret = ExecSQL(conn_db, " update statistics for table t_prm400 ", true);
#endif
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            ret = ExecSQL(conn_db,
                " update t_prm400 set" +
#if PG
                " p260=(select max( case when p.nzp_prm=260 then p.val_prm::numeric else 0 end )" +
                "  from ttt_prm_2 p where p.nzp=t_prm400.nzp_dom and p.nzp_prm=260)," +
                " p450=(select max( case when p.nzp_prm=450 then p.val_prm::numeric else 0 end )" +
                "  from ttt_prm_2 p where p.nzp=t_prm400.nzp_dom and p.nzp_prm=450),"+
                " p451=(select max( case when p.nzp_prm=451 then p.val_prm::numeric else 0 end )" +
                "  from ttt_prm_2 p where p.nzp=t_prm400.nzp_dom and p.nzp_prm=451),"+
                " p37=(select max( case when p.nzp_prm=37  then p.val_prm::numeric else 0 end )" +
                "  from ttt_prm_2 p where p.nzp=t_prm400.nzp_dom and p.nzp_prm=37),"+
                " p940=(select max( case when p.nzp_prm=940 then p.val_prm::numeric else 0 end ) " +
                "  from ttt_prm_2 p where p.nzp=t_prm400.nzp_dom and p.nzp_prm=940)" +
#else
                " (p260,p450,p451,p37,p940)=(( select" +
                "  max( case when p.nzp_prm=260 then p.val_prm+0 else 0 end )," +
                "  max( case when p.nzp_prm=450 then p.val_prm+0 else 0 end )," +
                "  max( case when p.nzp_prm=451 then p.val_prm+0 else 0 end )," +
                "  max( case when p.nzp_prm=37  then p.val_prm+0 else 0 end )," +
                "  max( case when p.nzp_prm=940 then p.val_prm+0 else 0 end ) " +
                "  from ttt_prm_2 p where p.nzp=t_prm400.nzp_dom" +
                "  and p.nzp_prm in (260,450,451,37,940)" +
                " ))" +
#endif
                " where 0<( select count(*)" +
                "  from ttt_prm_2 p1 where p1.nzp=t_prm400.nzp_dom" +
                "  and p1.nzp_prm in (260,450,451,37,940) ) "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            // для даты постройки дома отдельно - м.б. не дата для старых значений!
            ret = ExecSQL(conn_db,
                " update t_prm400 set p150=( select" +
                "  max( p.val_prm )" +
                "  from ttt_prm_2 p where p.nzp=t_prm400.nzp_dom" +
                "  and p.nzp_prm=150" +
                " )" +
                " where 0<( select count(*)" +
                "  from ttt_prm_2 p1 where p1.nzp=t_prm400.nzp_dom" +
                "  and p1.nzp_prm=150 ) "
                , false);

            ret = ExecSQL(conn_db, " update t_prm400 set p150=" + MDY(1,1,1900) + "where p150 is null ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            ret = ExecSQL(conn_db,
                " update t_prm400 set" +
#if PG
                " p2=(select max( case when p.nzp_prm=2    then p.val::numeric else 0 end )" +
                "  from t_par1 p where p.nzp=t_prm400.nzp_kvar and p.nzp_prm=2),"+
                " p1077=(select max( case when p.nzp_prm=1077 then p.val::numeric else 0 end )" +
                "  from t_par1 p where p.nzp=t_prm400.nzp_kvar and p.nzp_prm=1077),"+
                " p14=(select max( case when p.nzp_prm=14   then p.val::numeric else 0 end ) " +
                "  from t_par1 p where p.nzp=t_prm400.nzp_kvar and p.nzp_prm=14),"+
                " p16=(select max( case when p.nzp_prm=16   then p.val::numeric else 0 end ) " +
                "  from t_par1 p where p.nzp=t_prm400.nzp_kvar and p.nzp_prm=16)"+
#else
                " (p2,p1077,p14,p16)=(( select" +
                "  max( case when p.nzp_prm=2    then p.val+0 else 0 end )," +
                "  max( case when p.nzp_prm=1077 then p.val+0 else 0 end )," +
                "  max( case when p.nzp_prm=14   then p.val+0 else 0 end )," +
                "  max( case when p.nzp_prm=16   then p.val+0 else 0 end ) " +
                "  from t_par1 p where p.nzp=t_prm400.nzp_kvar" +
                "  and p.nzp_prm in (2,1077,14,16)" +
                " ))" +
#endif
                " where 0<( select count(*)" +
                "  from t_par1 p1 where p1.nzp=t_prm400.nzp_kvar and p1.nzp_prm in (2,1077,14,18) )"
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            ret = ExecSQL(conn_db,
                " update t_prm400 set kparam=4,k2=p450,tsten=26,tdsrok=-25,tplan=50,tetag=0" +
                " where nzp_frm_kod in (401,601) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            ret = ExecSQL(conn_db,
                " update t_prm400 set kparam=5,k2=p450,tsten=26,tdsrok=-24,tplan=-18,tetag=0,k5=p940" +
                " where nzp_frm_kod=402 ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            ret = ExecSQL(conn_db,
                " update t_prm400 set kparam=6,k2=p450,tsten=-22,tdsrok=-26,tplan=-20,tetag=-23," +
                "  k6 = (case when p14+p16>0 then 1 else 0.8 end) " +
                " where nzp_frm_kod=403 ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            ret = ExecSQL(conn_db,
                " update t_prm400 set kparam=5,k2=p450,tsten=-22,tdsrok=-26,tplan=-20,tetag=-23," + 
                "  p2 = (case when p1077=1 then p37 else p2 end) " +
                " where nzp_frm_kod=404 ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            // ограничить по размерностям таблиц
            ret = ExecSQL(conn_db,
                " update t_prm400 set "+ 
                "  p260 = (case when p260>4 then 4 else (case when p260<=0 then 1 else p260 end) end)," +
                "  p451 = (case when p451>3 then 3 else (case when p451<=0 then 1 else p451 end) end) " +
                " where 1=1 ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            ret = ExecSQL(conn_db,
                " update t_prm400 set " +
                "  k1 = ( Select value From " + paramcalc.kernel_alias + "res_values " +
                "   Where nzp_res = tsten and nzp_y = p260 and nzp_x = 1 )" + sConvToNum.Trim() +
                " ,k3 = ( Select value From " + paramcalc.kernel_alias + "res_values " +
                "   Where nzp_res = tdsrok and nzp_y = (" +
                    " Select a.nzp_y " +
                    " From " + paramcalc.kernel_alias + "res_values a," + paramcalc.kernel_alias + "res_values b " +
                    " Where a.nzp_res = tdsrok and b.nzp_res = tdsrok and a.nzp_y= b.nzp_y" +
                    " and a.nzp_x = 1 and b.nzp_x = 2 " +
#if PG
                    " and EXTRACT(year FROM t_prm400.p150)>=a.value" + sConvToNum.Trim() + 
                    " and EXTRACT(year FROM t_prm400.p150)<=b.value" + sConvToNum.Trim() +
#else
                    " and year(t_prm400.p150)>=a.value and year(t_prm400.p150)<=b.value" +
#endif
                "      ) and nzp_x = 3 )" + sConvToNum.Trim() +
                " ,k4 = ( Select value From " + paramcalc.kernel_alias + "res_values " +
                "   Where nzp_res = tplan and nzp_y = p451 and nzp_x = 1 )" + sConvToNum.Trim() + 
                " where 1=1 ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            ret = ExecSQL(conn_db,
                " update t_prm400 set " +
                "  k5 = ( Select value From " + paramcalc.kernel_alias + "res_values " +
                "   Where nzp_res = tetag and nzp_y = " +
                "     case when p2=p37 then 3 else (case when p2=1 then 1 else 2 end) end " +
                "   and nzp_x = 1 )" + sConvToNum.Trim() + 
                " where nzp_frm_kod in (403,404) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

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
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            ret = ExecSQL(conn_db,
                " update t_prm400 set " +
                "  tarif = round(tarif * 100) / 100 " +
                " where 1=1 ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            //ViewTbl(conn_db, " select * from t_prm400 ");
           
            ret = ExecSQL(conn_db,
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,tarif,priority,nzp_frm_typ)" +
                " select nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,tarif,priority,nzp_frm_typ" +
                " from t_prm400 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            ExecSQL(conn_db, " Drop table t_prm400 ", false);

            #endregion Тип расчета  400 (найм от базовой ставки)
            //5
            #region Тип расчета 2 (чем отличается от 1 надо узнать 335 параметра нет в базе выполняться не будет)
            // тип расчета = 2
            // ЭОТ на БД - если есть, то во последнюю очередь
            ret = ExecSQL(conn_db,
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,tarif,priority,nzp_frm_typ)" +
                " select  t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,max(p.val_prm" + sConvToNum + "),0,2" +
                " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f," + gku.paramcalc.data_alias + "prm_5 p" +
                " where t.nzp_frm=f.nzp_frm and p.nzp_prm=f.nzp_prm_tarif_bd and f.nzp_frm_typ=2" +
                " and p.is_actual<>100 and p.dat_s<" + gku.dat_po + " and p.dat_po>=" + gku.dat_s +
                " group by 1,2,3,4,5 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            // наличие ЭОТ на поставщика - во вторую очередь
            ret = ExecSQL(conn_db,
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,tarif,priority,nzp_frm_typ)" +
                " select t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,max(p.val_prm" + sConvToNum + "),1,2" +
                " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f," + gku.paramcalc.data_alias + "prm_11 p" +
                " where t.nzp_frm=f.nzp_frm and t.nzp_supp=p.nzp and p.nzp_prm=f.nzp_prm_tarif_su and f.nzp_frm_typ=2" +
                "  and p.is_actual<>100 and p.dat_s<" + gku.dat_po + " and p.dat_po>=" + gku.dat_s +
                // наличие ЭОТ на поставщика - nzp_prm=336 - если есть, то во вторую очередь
                " and 0<(select count(*) from " + gku.paramcalc.data_alias + "prm_5 p1" +
                "     where p1.nzp_prm=336 and p1.is_actual<>100 and p1.dat_s<" + gku.dat_po + " and p1.dat_po>=" + gku.dat_s + ") " +
                " group by 1,2,3,4,5 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            // выбрать параметр 335 на дату расчета
            ret = ExecSQL(conn_db,
                " create temp table t_par335 (" +
                " nzp      integer" +
#if PG
                " )  "
#else
                " ) with no log "
#endif
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            ret = ExecSQL(conn_db,
                " insert into t_par335 (nzp)" +
                " select nzp from t_par1 where nzp_prm=335 group by 1 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            ret = ExecSQL(conn_db, " create index ixt_par335 on t_par335(nzp) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            ret = ExecSQL(conn_db, sUpdStat + " t_par335 ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            // наличие ЭОТ на л/с - в первую очередь
            ret = ExecSQL(conn_db,
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,tarif,priority,nzp_frm_typ)" +
                " select t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,p.vald,3,2" +
                " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f,t_par1d p" +
                " where t.nzp_frm=f.nzp_frm and t.nzp_kvar=p.nzp and p.nzp_prm=f.nzp_prm_tarif_ls and f.nzp_frm_typ=2" +
                // наличие ЭОТ на л/с - nzp_prm=335 - если есть, то в первую очередь
                " and 0<(select count(*) from t_par335 p1 where p1.nzp=t.nzp_kvar) "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
            // наличие ЭОТ на дом - f.nzp_prm_tarif_dm - если есть, то в самую первую очередь
            ret = ExecSQL(conn_db,
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,tarif,priority,nzp_frm_typ)" +
                " select t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,max(p.val_prm" + sConvToNum + "),4,2" +
                " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f,ttt_prm_2 p" +
                " where t.nzp_frm=f.nzp_frm and t.nzp_dom=p.nzp and p.nzp_prm=f.nzp_prm_tarif_dm and f.nzp_frm_typ=2" +
                " group by 1,2,3,4,5 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
            #endregion Тип расчета 2 (чем отличается от 1 надо узнать 335 параметра нет в базе выполняться не будет)
            //6
            #region Тип расчета 26 (домофон с трубкой без трубки , тарифы выбираются в разных местах )
            // тип расчета = 26
            // выбрать параметры л/с для nzp_prm_tarif_bd на дату расчета
            ret = ExecSQL(conn_db,
                " create temp table t_par26f (" +
                " nzp     integer," +
                " nzp_prm integer" +
#if PG
                " )  "
#else
                " ) with no log "
#endif
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            ret = ExecSQL(conn_db,
                " insert into t_par26f (nzp,nzp_prm)" +
                " select unique p.nzp,p.nzp_prm from " + gku.paramcalc.kernel_alias + "formuls_opis f,t_par1 p" +
                " where p.nzp_prm=f.nzp_prm_tarif_bd "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            ret = ExecSQL(conn_db, " create index ixt_par26f on t_par26f(nzp,nzp_prm) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
            
            ret = ExecSQL(conn_db, sUpdStat + " t_par26f ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            // домовой тариф с трубкой на л/с - f.nzp_prm_tarif_dm

            ret = ExecSQL(conn_db,
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,tarif,priority,nzp_frm_typ)" +
                " select t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,max(p.val_prm" + sConvToNum + "),1,26" +
                " from t_trf t," + paramcalc.kernel_alias + "formuls_opis f,ttt_prm_2 p" +
                " where t.nzp_frm=f.nzp_frm and t.nzp_dom=p.nzp and p.nzp_prm=f.nzp_prm_tarif_dm and f.nzp_frm_typ=26" +
                // наличие трубки на л/с - f.nzp_prm_tarif_bd
                "  and 0<(select count(*) from t_par26f p1 where p1.nzp=t.nzp_kvar and p1.nzp_prm=f.nzp_prm_tarif_bd)" +
                " group by 1,2,3,4,5 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            // домовой тариф без трубки на л/с - f.nzp_prm_tarif_su
            ret = ExecSQL(conn_db,
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,tarif,priority,nzp_frm_typ)" +
                " select t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,max(p.val_prm" + sConvToNum + "),2,26" +
                " from t_trf t," + paramcalc.kernel_alias + "formuls_opis f,ttt_prm_2 p" +
                " where t.nzp_frm=f.nzp_frm and t.nzp_dom=p.nzp and p.nzp_prm=f.nzp_prm_tarif_su and f.nzp_frm_typ=26" +
                // осутствие трубки на л/с - f.nzp_prm_tarif_bd
                "  and 0=(select count(*) from t_par26f p1 where p1.nzp=t.nzp_kvar and p1.nzp_prm=f.nzp_prm_tarif_bd)" +
                " group by 1,2,3,4,5 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            // квартирный тариф на л/с - f.nzp_prm_tarif_ls
            ret = ExecSQL(conn_db,
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,tarif,priority,nzp_frm_typ)" +
                " select t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,p.vald,3,26" +
                " from t_trf t," + paramcalc.kernel_alias + "formuls_opis f,t_par1d p" +
                " where t.nzp_frm=f.nzp_frm and t.nzp_kvar=p.nzp and p.nzp_prm=f.nzp_prm_tarif_ls and f.nzp_frm_typ=26"
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
            #endregion Тип расчета 26 (домофон с трубкой без трубки , тарифы выбираются в разных местах )
            //7
            #region Тип расчета 12 ( )
            // тип расчета = 12
            // тариф на базу с эл/плитой на л/с - f.nzp_prm_tarif_dm
            ret = ExecSQL(conn_db,
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,tarif,priority,nzp_frm_typ)" +
                " select t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,max(p.val_prm" + sConvToNum + "),1,12" +
                " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f," + gku.paramcalc.data_alias + "prm_5 p" +
                " where t.nzp_frm=f.nzp_frm and p.nzp_prm=f.nzp_prm_tarif_dm and f.nzp_frm_typ=12" +
                " and p.is_actual<>100 and p.dat_s<" + gku.dat_po + " and p.dat_po>=" + gku.dat_s +
                // наличие эл/плиты на л/с - f.nzp_prm_tarif_bd
                "  and 0<(select count(*) from t_par26f p1 where p1.nzp=t.nzp_kvar and p1.nzp_prm=f.nzp_prm_tarif_bd)" +
                " group by 1,2,3,4,5 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
            // тариф на базу без эл/плиты на л/с - f.nzp_prm_tarif_su
            ret = ExecSQL(conn_db,
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,tarif,priority,nzp_frm_typ)" +
                " select t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,max(p.val_prm" + sConvToNum + "),2,12" +
                " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f," + gku.paramcalc.data_alias + "prm_5 p" +
                " where t.nzp_frm=f.nzp_frm and p.nzp_prm=f.nzp_prm_tarif_su and f.nzp_frm_typ=12" +
                " and p.is_actual<>100 and p.dat_s<" + gku.dat_po + " and p.dat_po>=" + gku.dat_s +
                // осутствие эл/плиты на л/с - f.nzp_prm_tarif_bd
                "  and 0=(select count(*) from t_par26f p1 where p1.nzp=t.nzp_kvar and p1.nzp_prm=f.nzp_prm_tarif_bd)" +
                " group by 1,2,3,4,5 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
            #endregion Тип расчета 12 ( )

            //7.1
            #region Тип расчета 312 ( )
            // тип расчета = 312
            // тариф на базу с эл/плитой на л/с - f.nzp_prm_tarif_dm
            ret = ExecSQL(conn_db,
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,tarif,priority,nzp_frm_typ)" +
                " select t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,max(p.val" + sConvToNum + "),1,312" +
                " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f,t_par1 p" +
                " where t.nzp_frm=f.nzp_frm and t.nzp_kvar=p.nzp and p.nzp_prm=f.nzp_prm_tarif_dm and f.nzp_frm_typ=312" +
                // наличие эл/плиты на л/с - f.nzp_prm_tarif_bd
                "  and 0<(select count(*) from t_par26f p1 where p1.nzp=t.nzp_kvar and p1.nzp_prm=f.nzp_prm_tarif_bd)" +
                " group by 1,2,3,4,5 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
            // тариф на базу без эл/плиты на л/с - f.nzp_prm_tarif_su
            ret = ExecSQL(conn_db,
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,tarif,priority,nzp_frm_typ)" +
                " select t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,max(p.val" + sConvToNum + "),2,312" +
                " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f,t_par1 p" +
                " where t.nzp_frm=f.nzp_frm and t.nzp_kvar=p.nzp and p.nzp_prm=f.nzp_prm_tarif_su and f.nzp_frm_typ=312" +
                // осутствие эл/плиты на л/с - f.nzp_prm_tarif_bd
                "  and 0=(select count(*) from t_par26f p1 where p1.nzp=t.nzp_kvar and p1.nzp_prm=f.nzp_prm_tarif_bd)" +
                " group by 1,2,3,4,5 "
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
            ssql =
                " create temp table t_par412 (" +
                " nzp_kvar integer" +
                " )  ";
#if PG
#else
            ssql = ssql.Trim() + " with no log ";
#endif
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
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,tarif,priority,nzp_frm_typ)" +
                " select t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,max(p.val_prm" + sConvToNum + "),1,412" +
                " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f," + gku.paramcalc.data_alias + "prm_5 p" +
                " where t.nzp_frm=f.nzp_frm and p.nzp_prm=f.nzp_prm_tarif_ls and f.nzp_frm_typ=412" +
                " and p.is_actual<>100 and p.dat_s<" + gku.dat_po + " and p.dat_po>=" + gku.dat_s +
                // Ночное электроснабжение по своему тарифу
                "  and 0=(select count(*) from t_par412 p2 where p2.nzp_kvar=t.nzp_kvar)" +
                // наличие эл/плиты на л/с - f.nzp_prm_tarif_bd
                "  and 0<(select count(*) from t_par26f p1 where p1.nzp=t.nzp_kvar and p1.nzp_prm=f.nzp_prm_tarif_bd)" +
                " group by 1,2,3,4,5 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            // тариф на базу без эл/плиты на л/с - f.nzp_prm_tarif_su
            ret = ExecSQL(conn_db,
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,tarif,priority,nzp_frm_typ)" +
                " select t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,max(p.val_prm" + sConvToNum + "),2,412" +
                " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f," + gku.paramcalc.data_alias + "prm_5 p" +
                " where t.nzp_frm=f.nzp_frm and p.nzp_prm=f.nzp_prm_tarif_lsp and f.nzp_frm_typ=412" +
                " and p.is_actual<>100 and p.dat_s<" + gku.dat_po + " and p.dat_po>=" + gku.dat_s +
                // Ночное электроснабжение по своему тарифу
                "  and 0=(select count(*) from t_par412 p2 where p2.nzp_kvar=t.nzp_kvar)" +
                // осутствие эл/плиты на л/с - f.nzp_prm_tarif_bd
                "  and 0=(select count(*) from t_par26f p1 where p1.nzp=t.nzp_kvar and p1.nzp_prm=f.nzp_prm_tarif_bd)" +
                " group by 1,2,3,4,5 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            // тариф на базу с эл/плитой на л/с - f.nzp_prm_tarif_dm
            ret = ExecSQL(conn_db,
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,tarif,priority,nzp_frm_typ)" +
                " select t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,max(p.val_prm" + sConvToNum + "),1,412" +
                " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f," + gku.paramcalc.data_alias + "prm_5 p" +
                " where t.nzp_frm=f.nzp_frm and p.nzp_prm=f.nzp_prm_tarif_dm and f.nzp_frm_typ=412" +
                " and p.is_actual<>100 and p.dat_s<" + gku.dat_po + " and p.dat_po>=" + gku.dat_s +
                // Ночное электроснабжение по дневному тарифу
                "  and 0<(select count(*) from t_par412 p2 where p2.nzp_kvar=t.nzp_kvar)" +
                // наличие эл/плиты на л/с - f.nzp_prm_tarif_bd
                "  and 0<(select count(*) from t_par26f p1 where p1.nzp=t.nzp_kvar and p1.nzp_prm=f.nzp_prm_tarif_bd)" +
                " group by 1,2,3,4,5 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            // тариф на базу без эл/плиты на л/с - f.nzp_prm_tarif_su
            ret = ExecSQL(conn_db,
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,tarif,priority,nzp_frm_typ)" +
                " select t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,max(p.val_prm" + sConvToNum + "),2,412" +
                " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f," + gku.paramcalc.data_alias + "prm_5 p" +
                " where t.nzp_frm=f.nzp_frm and p.nzp_prm=f.nzp_prm_tarif_su and f.nzp_frm_typ=412" +
                " and p.is_actual<>100 and p.dat_s<" + gku.dat_po + " and p.dat_po>=" + gku.dat_s +
                // Ночное электроснабжение по дневному тарифу
                "  and 0<(select count(*) from t_par412 p2 where p2.nzp_kvar=t.nzp_kvar)" +
                // осутствие эл/плиты на л/с - f.nzp_prm_tarif_bd
                "  and 0=(select count(*) from t_par26f p1 where p1.nzp=t.nzp_kvar and p1.nzp_prm=f.nzp_prm_tarif_bd)" +
                " group by 1,2,3,4,5 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            #endregion Тип расчета 412 (в)

            //8.1
            #region Тип расчета 912 ()
            // тип расчета = 912
            // тариф на базу с эл/плитой на л/с - f.nzp_prm_tarif_dm
            ret = ExecSQL(conn_db,
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,tarif,priority,nzp_frm_typ)" +
                " select t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,max(p.val" + sConvToNum + "),1,912" +
                " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f,t_par1 p" +
                " where t.nzp_frm=f.nzp_frm and p.nzp_prm=f.nzp_prm_tarif_ls and f.nzp_frm_typ=912" +
                // наличие эл/плиты на л/с - f.nzp_prm_tarif_bd
                "  and 0<(select count(*) from t_par26f p1 where p1.nzp=t.nzp_kvar and p1.nzp_prm=f.nzp_prm_tarif_bd)" +
                " group by 1,2,3,4,5 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            // тариф на базу без эл/плиты на л/с - f.nzp_prm_tarif_su
            ret = ExecSQL(conn_db,
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,tarif,priority,nzp_frm_typ)" +
                " select t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,max(p.val" + sConvToNum + "),2,912" +
                " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f,t_par1 p" +
                " where t.nzp_frm=f.nzp_frm and p.nzp_prm=f.nzp_prm_tarif_lsp and f.nzp_frm_typ=912" +
                // осутствие эл/плиты на л/с - f.nzp_prm_tarif_bd
                "  and 0=(select count(*) from t_par26f p1 where p1.nzp=t.nzp_kvar and p1.nzp_prm=f.nzp_prm_tarif_bd)" +
                " group by 1,2,3,4,5 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            #endregion Тип расчета 912 ()

            //9
            #region Тип расчета = 40 & 814 & 440 & 514 & 1140 &1814 1983
            
            // ЭОТ на БД - если есть, то во последнюю очередь
            #region выборка тарифа и параметра на базу для формул (nzp_prm_tarif_bd) данного типа (ЭОТ на бд)
#if PG
            ret = ExecSQL(conn_db,
                           " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,tarif_gkal,priority,nzp_frm_typ)" +
                           " select  t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,max(p.val_prm::numeric),0,f.nzp_frm_typ" +
                           " from t_trf t," + gku.paramcalc.pref + "_kernel.formuls_opis f," + gku.paramcalc.pref + "_data.prm_5 p" +
                           " where t.nzp_frm=f.nzp_frm and p.nzp_prm=f.nzp_prm_tarif_bd and f.nzp_frm_typ in (40,814,440,514,1140,1814,1983)" +
                           " and p.is_actual<>100 and p.dat_s<" + gku.dat_po + " and p.dat_po>=" + gku.dat_s +
                           " group by 1,2,3,4,5,8 "
                           , true);
#else
 ret = ExecSQL(conn_db,
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,tarif_gkal,priority,nzp_frm_typ)" +
                " select  t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,max(p.val_prm),0,f.nzp_frm_typ" +
                " from t_trf t," + gku.paramcalc.pref + "_kernel:formuls_opis f," + gku.paramcalc.pref + "_data:prm_5 p" +
                " where t.nzp_frm=f.nzp_frm and p.nzp_prm=f.nzp_prm_tarif_bd and f.nzp_frm_typ in (40,814,440,514,1140,1814,1983)" +
                " and p.is_actual<>100 and p.dat_s<" + gku.dat_po + " and p.dat_po>=" + gku.dat_s +
                " group by 1,2,3,4,5,8 "
                , true);
#endif
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
            #endregion выборка тарифа и параметра на базу для формул данного типа

            // наличие ЭОТ на поставщика - во вторую очередь
            #region выборка тарифа и параметра по поставщикам (nzp_prm_tarif_su) из prm_11 c доп условием настройки на базу (признак брать тарифы по поставщикам 336 параметр )
#if PG
            ret = ExecSQL(conn_db,
                           " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,tarif_gkal,priority,nzp_frm_typ)" +
                           " select t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,max(p.val_prm::numeric),1,f.nzp_frm_typ" +
                           " from t_trf t," + gku.paramcalc.pref + "_kernel.formuls_opis f," + gku.paramcalc.pref + "_data.prm_11 p" +
                           " where t.nzp_frm=f.nzp_frm and t.nzp_supp=p.nzp and p.nzp_prm=f.nzp_prm_tarif_su and f.nzp_frm_typ in (40,814,440,514,1140,1814,1983)" +
                           "  and p.is_actual<>100 and p.dat_s<" + gku.dat_po + " and p.dat_po>=" + gku.dat_s +
                // наличие ЭОТ на поставщика - nzp_prm=336 - если есть, то во вторую очередь
                           " and 0<(select count(*) from " + gku.paramcalc.pref + "_data.prm_5 p1" +
                           "     where p1.nzp_prm=336 and p1.is_actual<>100 and p1.dat_s<" + gku.dat_po + " and p1.dat_po>=" + gku.dat_s + ") " +
                           " group by 1,2,3,4,5,8 "
                           , true);
#else
 ret = ExecSQL(conn_db,
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,tarif_gkal,priority,nzp_frm_typ)" +
                " select t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,max(p.val_prm),1,f.nzp_frm_typ" +
                " from t_trf t," + gku.paramcalc.pref + "_kernel:formuls_opis f," + gku.paramcalc.pref + "_data:prm_11 p" +
                " where t.nzp_frm=f.nzp_frm and t.nzp_supp=p.nzp and p.nzp_prm=f.nzp_prm_tarif_su and f.nzp_frm_typ in (40,814,440,514,1140,1814,1983)" +
                "  and p.is_actual<>100 and p.dat_s<" + gku.dat_po + " and p.dat_po>=" + gku.dat_s +
                // наличие ЭОТ на поставщика - nzp_prm=336 - если есть, то во вторую очередь
                " and 0<(select count(*) from " + gku.paramcalc.pref + "_data:prm_5 p1" +
                "     where p1.nzp_prm=336 and p1.is_actual<>100 and p1.dat_s<" + gku.dat_po + " and p1.dat_po>=" + gku.dat_s + ") " +
                " group by 1,2,3,4,5,8 "
                , true);
#endif
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
            #endregion выборка тарифа и параметра по поставщикам из prm_11 c доп условием настройки на базу (признак брать тарифы по поставщикам 336 параметр )

            // тариф на дом для отопления - во первую после ЛС очередь
            #region выборка тарифа из домовых параметров по отопительным формулам (nzp_prm_tarif_dm)
#if PG
            ret = ExecSQL(conn_db,
                            " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,tarif_gkal,priority,nzp_frm_typ)" +
                            " select t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,max(p.val_prm::numeric),2,f.nzp_frm_typ" +
                            " from t_trf t," + gku.paramcalc.pref + "_kernel.formuls_opis f,ttt_prm_2 p" +
                            " where t.nzp_frm=f.nzp_frm and t.nzp_dom=p.nzp and p.nzp_prm=f.nzp_prm_tarif_dm and f.nzp_frm_typ in (814,514,1814,1983) " +
                            " group by 1,2,3,4,5,8 "
                            , true);
#else
ret = ExecSQL(conn_db,
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,tarif_gkal,priority,nzp_frm_typ)" +
                " select t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,max(p.val_prm),2,f.nzp_frm_typ" +
                " from t_trf t," + gku.paramcalc.pref + "_kernel:formuls_opis f,ttt_prm_2 p" +
                " where t.nzp_frm=f.nzp_frm and t.nzp_dom=p.nzp and p.nzp_prm=f.nzp_prm_tarif_dm and f.nzp_frm_typ in (814,514,1814,1983) " +
                " group by 1,2,3,4,5,8 "
                , true);
#endif
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
            #endregion выборка тарифа из домовых параметров по отопительным формулам

            // наличие ЭОТ на л/с - в первую очередь
            #region Выборка тарифа из тарифов уровня лицевого счета (nzp_prm_tarif_ls)
#if PG
            ret = ExecSQL(conn_db,
                            " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,tarif_gkal,priority,nzp_frm_typ)" +
                            " select t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,p.vald,3,f.nzp_frm_typ" +
                            " from t_trf t," + gku.paramcalc.pref + "_kernel.formuls_opis f,t_par1d p" +
                            " where t.nzp_frm=f.nzp_frm and t.nzp_kvar=p.nzp and p.nzp_prm=f.nzp_prm_tarif_ls and f.nzp_frm_typ in (40,814,440,514,1140,1814,1983)" +
                // наличие ЭОТ на л/с - nzp_prm=335 - если есть, то в первую очередь
                            " and 0<(select count(*) from t_par335 p1 where p1.nzp=t.nzp_kvar) "
                            , true);
#else
ret = ExecSQL(conn_db,
                " insert into t_prm (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,tarif_gkal,priority,nzp_frm_typ)" +
                " select t.nzp_dom,t.nzp_kvar,t.nzp_supp,t.nzp_frm,p.nzp_prm,p.vald,3,f.nzp_frm_typ" +
                " from t_trf t," + gku.paramcalc.pref + "_kernel:formuls_opis f,t_par1d p" +
                " where t.nzp_frm=f.nzp_frm and t.nzp_kvar=p.nzp and p.nzp_prm=f.nzp_prm_tarif_ls and f.nzp_frm_typ in (40,814,440,514,1140,1814,1983)" +
                // наличие ЭОТ на л/с - nzp_prm=335 - если есть, то в первую очередь
                " and 0<(select count(*) from t_par335 p1 where p1.nzp=t.nzp_kvar) "
                , true);
#endif
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
            //          индексы для установки тарифов
            #region создать и учесть индексы
            ret = ExecSQL(conn_db, " create index ixt_prm on t_prm(nzp_kvar,nzp_supp,nzp_frm,priority) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
#if PG
            ret = ExecSQL(conn_db, " analyze t_prm ", true);
#else
            ret = ExecSQL(conn_db, " update statistics for table t_prm ", true);
#endif
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
            #endregion создать индексы

            #endregion Выборка тарифа из тарифов уровня лицевого счета (nzp_prm_tarif_ls)

            // Учесть доп логику по гор.воде
            #region  Расчет тарифа по дополнительным параметрам тип расчета = 40,440,1140,1814 , 1983

            #region Выбрать параметры полотенцесушителя и неизолированный трубопровод (только для горячей воды тип 40)
            // выбрать параметры л/с для типа 40 на дату расчета
#if PG
            ret = ExecSQL(conn_db,
                           " create temp table t_par40f (" +
                           " nzp     integer," +
                           " nzp_prm integer" +
                           " ) "
                           , true);
#else
 ret = ExecSQL(conn_db,
                " create temp table t_par40f (" +
                " nzp     integer," +
                " nzp_prm integer" +
                " ) with no log "
                , true);
#endif
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

#if PG
            ret = ExecSQL(conn_db,
                            " insert into t_par40f (nzp,nzp_prm)" +
                            " select distinct nzp,nzp_prm from t_par1 where nzp_prm in (59,327) "
                            , true);
#else
ret = ExecSQL(conn_db,
                " insert into t_par40f (nzp,nzp_prm)" +
                " select unique nzp,nzp_prm from t_par1 where nzp_prm in (59,327) "
                , true);
#endif
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

           

            ret = ExecSQL(conn_db, " create index ixt_par40f on t_par40f(nzp,nzp_prm) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
#if PG
            ret = ExecSQL(conn_db, " analyze t_par40f ", true);
#else
            ret = ExecSQL(conn_db, " update statistics for table t_par40f ", true);
#endif
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            #endregion Выбрать параметры полотенцесушителя и неизолированный трубопровод

            #region Выбрать параметр норма Гкал на базу данных для всех формул с Гкалл
#if PG
            ret = ExecSQL(conn_db,
                            " update t_prm set" +
                            " norm_gkal=(" +
                            " select max(p.val_prm::numeric) from " + gku.paramcalc.pref + "_data.prm_5 p where p.nzp_prm=253" +
                            " and p.is_actual<>100 and p.dat_s<" + gku.dat_po + " and p.dat_po>=" + gku.dat_s +
                            " )" +
                            " where nzp_frm_typ in (40,440,1140,1814) "
                            , true);
#else
ret = ExecSQL(conn_db,
                " update t_prm set" +
                " norm_gkal=(" +
                " select max(p.val_prm) from " + gku.paramcalc.pref + "_data:prm_5 p where p.nzp_prm=253" +
                " and p.is_actual<>100 and p.dat_s<" + gku.dat_po + " and p.dat_po>=" + gku.dat_s +
                " )" +
                " where nzp_frm_typ in (40,440,1140,1814) "
                , true);
#endif
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
            #endregion Выбрать параметр норма Гкал на базу данных для всех формул с Гкалл

            #region Выставить полотенцесушитель  (только горячая вода типы 40,440,1140)
            ret = ExecSQL(conn_db,
                " update t_prm set is_poloten=1" +
                " where nzp_frm_typ in (40,440,1140) and 0<(select count(*) from t_par40f p where t_prm.nzp_kvar=p.nzp and p.nzp_prm= 59) "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
            #endregion Выставить полотенцесушитель

            #region Выставить неизолированный полотенцесушитель  (только горячая вода типы 40,440,1140)
            ret = ExecSQL(conn_db,
                " update t_prm set is_neiztrp=1" +
                " where nzp_frm_typ in (40,440,1140) and 0<(select count(*) from t_par40f p where t_prm.nzp_kvar=p.nzp and p.nzp_prm=327) "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            ret = ExecSQL(conn_db, " update t_prm set norm_gkal =0 where nzp_frm_typ in (40,440,1140) and norm_gkal  is null ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            ret = ExecSQL(conn_db, " update t_prm set is_poloten=0 where nzp_frm_typ in (40,440,1140) and is_poloten is null ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            ret = ExecSQL(conn_db, " update t_prm set is_neiztrp=0 where nzp_frm_typ in (40,440,1140) and is_neiztrp is null ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            #endregion Выставить неизолированный полотенцесушитель

            #region Применение повышающего коэффициента  (только горячая вода типы 40,440,1140)
            // запрещено применять повышающий коэффициент для ГВС ?
#if PG
            ret = ExecRead(conn_db, out reader,
                            " select count(*) cnt " +
                            " from " + gku.paramcalc.pref + "_data.prm_5 p " +
                            " where p.nzp_prm=1173 and p.val_prm='1' " +
                            " and p.is_actual<>100 and p.dat_s<" + gku.dat_po + " and p.dat_po>=" + gku.dat_s + " "
                        , true);
#else
            ret = ExecRead(conn_db, out reader,
                " select count(*) cnt " +
                " from " + gku.paramcalc.pref + "_data:prm_5 p " +
                " where p.nzp_prm=1173 and p.val_prm='1' " +
                " and p.is_actual<>100 and p.dat_s<" + gku.dat_po + " and p.dat_po>=" + gku.dat_s + " "
            , true);
#endif
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            bool bNotUseKoefGVS = false;
            if (reader.Read())
            {
                bNotUseKoefGVS = (Convert.ToInt32(reader["cnt"]) > 0);
            }
            reader.Close();

            // параметр 1173 на  базу - запрещено применять повышающий коэффициент 
            if (bNotUseKoefGVS)
            {
                ret = ExecSQL(conn_db, " update t_prm set koef_gkal=1 where nzp_frm_typ in (40,440,1140) ", true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
            }
            else 
            {
                ret = ExecSQL(conn_db, " update t_prm set koef_gkal=1.2 where nzp_frm_typ in (40,440,1140) ", true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

                ret = ExecSQL(conn_db, " update t_prm set koef_gkal=1.1 where nzp_frm_typ in (40,440,1140) and is_neiztrp=0 and is_neiztrp=0 ", true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

                ret = ExecSQL(conn_db, " update t_prm set koef_gkal=1.3 where nzp_frm_typ in (40,440,1140) and is_neiztrp=1 and is_neiztrp=1 ", true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
            }
            #endregion Применение повышающего коэффициента  (только горячая вода типы 40,440,1140)

            #region Выбрать норму Гкалл из домового параметра ( prm_2  nzp_prm =436 по типам 40,440,1140,1814,1983 )
            ret = ExecSQL(conn_db,
                " update t_prm set" +
                " norm_gkal=(" +
                "  select max(p.val_prm::numeric) from ttt_prm_2 p where p.nzp=t_prm.nzp_dom and p.nzp_prm=436" +
                " )," +
                " koef_gkal=1" +
                " where nzp_frm_typ in (40,440,1140,1814,1983) " +
                // наличие нормы на дом - nzp_prm=436 - если есть, то во вторую очередь
                "  and 0<(select count(*) from ttt_prm_2 p1 where p1.nzp=t_prm.nzp_dom and p1.nzp_prm=436 ) "
                , true, 6000);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
            #endregion Выбрать норму Гкалл из домового параметра ( prm_2  nzp_prm =436)

            #region Выбрать норму Гкалл на 1 м3 воды на лицевой счет prm_1 nzp_prm =894 
            // выбрать параметры л/с для типа 40 на дату расчета
#if PG
            ret = ExecSQL(conn_db,
                            " create temp table t_par894 (" +
                            " nzp     integer," +
                            " vald     numeric(14,7)" +
                            " )  "
                            , true);
#else
ret = ExecSQL(conn_db,
                " create temp table t_par894 (" +
                " nzp     integer," +
                " vald     decimal(14,7)" +
                " ) with no log "
                , true);
#endif
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            // Выбрать квартирный норматив гкал на м3
            ret = ExecSQL(conn_db,
                " insert into t_par894 (nzp,vald)" +
                " select nzp,max(val"+
#if PG
 "::numeric)" +
#else
"+0)"+
#endif
                " from t_par1 where nzp_prm=894 group by 1 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            ret = ExecSQL(conn_db, " create index ixt_par894 on t_par894(nzp) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
#if PG
            ret = ExecSQL(conn_db, " analyze t_par894 ", true);
#else
            ret = ExecSQL(conn_db, " update statistics for table t_par894 ", true);
#endif
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            ret = ExecSQL(conn_db,
                " update t_prm set" +
                " norm_gkal=(select p2.vald from t_par894 p2 where p2.nzp=t_prm.nzp_kvar)," +
                " koef_gkal=1" +
                " where nzp_frm_typ in (40,440,1140) " +
                // наличие нормы на л/с - nzp_prm=894 - если есть, то в первую очередь
                "  and 0<(select count(*) from t_par894 p1 where p1.nzp=t_prm.nzp_kvar) "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
            #endregion Выбрать норму Гкалл на 1 м3 воды на лицевой счет prm_1 nzp_prm =894

            #region Выбрать тариф на 1 м3 горячей воды на дом (горячая вода по счетчикам ), добавить сумму к тарифу  prm_2 nzp_prm=2003
            // наличие тарифа на 1 м3 ГВ на дом - nzp_prm=2003 - если есть, то прибавить к тарифу
            ret = ExecSQL(conn_db,
                " update t_prm set" +
                " tarif_m3=(" +
                "  select max(p.val_prm::numeric) from ttt_prm_2 p where p.nzp=t_prm.nzp_dom and p.nzp_prm=2003" +
                " )" +
                " where nzp_frm_typ=40 " +
                // наличие нормы на дом - nzp_prm=436 - если есть, то во вторую очередь
                "  and 0<(select count(*) from ttt_prm_2 p1 where p1.nzp=t_prm.nzp_dom and p1.nzp_prm=2003) "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            ret = ExecSQL(conn_db, " update t_prm set tarif_m3=0 where tarif_m3 is Null ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
            #endregion Выбрать тариф на 1 м3 горячей воды на дом (горячая вода по счетчикам ), добавить сумму к тарифу  prm_2 nzp_prm=2003

            #region Расчет окончательного тарифа исходя из всех коэффициентов по типу 40 = round(tarif_gkal*koef_gkal*norm_gkal*100)/100+tarif_m3
            // расчитать окончательный тариф на 1м3 ГВС
            ret = ExecSQL(conn_db,
                " update t_prm set tarif=round(tarif_gkal*koef_gkal*norm_gkal*100)/100+tarif_m3 where nzp_frm_typ=40 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
            #endregion Расчет окончательного тарифа исходя из всех коэффициентов по типу 40 = round(tarif_gkal*koef_gkal*norm_gkal*100)/100+tarif_m3

            #region Выставить для всех остальных типов окончательный тариф на 1ГКал ГВС - корректировка расходов потом!
            // расчитать окончательный тариф на 1ГКал ГВС - корректировка расходов потом!
            ret = ExecSQL(conn_db,
                " update t_prm set tarif=tarif_gkal where nzp_frm_typ in (440,1140,1814,1983) "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            #endregion Выставить для всех остальных типов

            #region Обновить статистику
#if PG
            ret = ExecSQL(conn_db, " analyze t_prm ", true);
#else
            ret = ExecSQL(conn_db, " update statistics for table t_prm ", true);
#endif
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
            #endregion Обновить статистику

            #endregion  Расчет тарифа по дополнительным параметрам тип расчета = 40,440,1140,1814

            #endregion Тип расчета = 40 & 814 & 440 & 514 & 1140
           

            #region Завершающие действия по выборке тарифов(расчетная площадь, жилая , отапливаемая? количество жильцов)
            //
//          Завершающие действия по выборке тарифов
//

            // расчетная площадь
            ret = ExecSQL(conn_db,
                " update t_prm set" +
                " squ=(select max(p.val"+
#if PG
 "::numeric)"
#else
"+0)"
#endif
                +" from t_par1 p where p.nzp=t_prm.nzp_kvar and p.nzp_prm=4)" +
                " where 0<(select count(*) from t_par1 p where p.nzp=t_prm.nzp_kvar and p.nzp_prm=4)"
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            if (Points.IsSmr)
            {
                // в Самаре для коммуналок жилая площадь
                ret = ExecSQL(conn_db,
                    " update t_prm set" +
                    " squ=(select max(p.val"+
#if PG
 "::numeric)"
#else
"+0)"
#endif
                    +" from t_par1 p where p.nzp=t_prm.nzp_kvar and p.nzp_prm=6)" +
                    " where 0<(select count(*) from t_par1 p where p.nzp=t_prm.nzp_kvar and p.nzp_prm=6)" +
                    " and 0<(select count(*) from t_kmnl r where t_prm.nzp_kvar=r.nzp_kvar)"
                    , true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
            }
            else
            {
                // в РТ для отопления отапливаемая площадь
                ret = ExecSQL(conn_db,
                    " update t_prm set" +
                    " squ=(select max(p.val"+
#if PG
 "::numeric)"
#else
"+0)"
#endif
                    +" from t_par1 p where p.nzp=t_prm.nzp_kvar and p.nzp_prm=133)" +
                    " where 0<(select count(*) from t_par1 p where p.nzp=t_prm.nzp_kvar and p.nzp_prm=133)" +
                    " and nzp_frm in (114,814, 1814) "
                    , true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
            }
            

            // расчетное кол-во жильцов
            ret = ExecSQL(conn_db,
                " update t_prm set" +
                " gil=(select "+
#if PG
 "coalesce"
#else
"nvl"
#endif
                +"(g.kg,0)+"+
#if PG
 "coalesce"
#else
"nvl"
#endif
                +"(g.kvp,0) from t_rsh_kolgil g where g.nzp_kvar=t_prm.nzp_kvar)" +
                ",gil_g=(select "+
#if PG
 "coalesce"
#else
"nvl"
#endif
                +"(g.kg_g,0)+"+
#if PG
 "coalesce"
#else
"nvl"
#endif
                +"(g.kvp,0) from t_rsh_kolgil g where g.nzp_kvar=t_prm.nzp_kvar)" +
                " where 0<(select count(*) from t_rsh_kolgil g where g.nzp_kvar=t_prm.nzp_kvar)" 
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
            #endregion Завершающие действия по выборке тарифов(расчетная площадь, жилая , отапливаемая? количество жильцов)

            #region Выборка применяемых тарифов в соответствии с максимальным приоритетом (t_prm_max)
            // выборка применяемых тарифов
            ret = ExecSQL(conn_db,
                " create temp table t_prm_max (" +
                " nzp_kvar integer," +
                " nzp_supp integer," +
                " nzp_frm  integer," +
                " priority integer" +
#if PG
 " )  "
#else
" ) with no log "
#endif
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            ret = ExecSQL(conn_db,
                " insert into t_prm_max (nzp_kvar,nzp_supp,nzp_frm,priority)" +
                " select nzp_kvar,nzp_supp,nzp_frm,max(priority) from t_prm group by 1,2,3 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            ret = ExecSQL(conn_db, " create index ixt_prm_max on t_prm_max(nzp_kvar,nzp_supp,nzp_frm,priority) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
#if PG
            ret = ExecSQL(conn_db, " analyze t_prm_max ", true);
#else
            ret = ExecSQL(conn_db, " update statistics for table t_prm_max ", true);
#endif
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
            #endregion Выборка применяемых тарифов в соответствии с максимальным приоритетом

            #endregion создать сводную таблицу t_prm (тарифы)

            //            
//          Выборка расходов
//
            #region Создать сводную таблицу расходов t_rsh (плоский вид)

            #region Создать врем таблицу расходов
            ret = ExecSQL(conn_db,
                " create temp table t_rsh (" +
                //" create table are.t_rsh (" +
                " nzp_kvar integer," +
                " nzp_frm  integer," +
                " nzp_prm  integer," +
                " rashod   "       + sDecimalType + "(16,7) default 0," +
                " rashod_norm   "  + sDecimalType + "(16,7) default 0," +
                " rash_norm_one "  + sDecimalType + "(15,7) default 0," +
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
                " dlt_reval "  + sDecimalType + "(16,7) default 0," +
                " dlt_revalk " + sDecimalType + "(16,7) default 0," +
                " gil_smr  " + sDecimalType + "(16,7) default 0," +
                " kf307hv  " + sDecimalType + "(14,7) default 1," +
                " kf307gv  " + sDecimalType + "(14,7) default 1," +
                " kod_info  integer  default 0," +
                " is_device integer  default 0," +
                " is_trunc  integer  default 7," +                    // по умолчанию округление до 7 знаков
                " recalc_ipu    integer  default 0," +
                " nzp_frm_typ   integer default 0 not null," +        // тип тарифа добавил потому что нужно делать связанные с ним апдейты и не вводить новые типы расхода 
                " nzp_frm_typrs integer default 0 not null " +        // тип расхода 
#if PG
                " ) "
#else
                //" ) "
                " ) with no log "
#endif
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
            #endregion Создать врем таблицу расходов

            #region Тип расхода = 1 (просто расход квартирного параметра)
            // тип расхода = 1

            ret = ExecSQL(conn_db,
                " insert into t_rsh (nzp_kvar,nzp_frm,nzp_prm, nzp_frm_typ,rashod,rashod_g,nzp_frm_typrs,valm)" +
                " select t.nzp_kvar,t.nzp_frm,p.nzp_prm, f.nzp_frm_typ," +
                " max(p.val" + sConvToNum + "), max(p.val" + sConvToNum + "), 1, max(p.val" + sConvToNum + ")" +
                " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f,t_par1 p" +
                " where t.nzp_frm=f.nzp_frm and t.nzp_kvar=p.nzp and p.nzp_prm=f.nzp_prm_rash and f.nzp_frm_typrs=1" +
                " group by 1,2,3,4 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            #endregion Тип расхода = 1 (просто расход квартирного параметра)

            #region Тип расхода = 2 (с учетом индивидуального расхода коммунальных квартир)
            // тип расхода = 2
            ret = ExecSQL(conn_db,
                " insert into t_rsh (nzp_kvar,nzp_frm,nzp_prm, nzp_frm_typ,rashod,rashod_g,nzp_frm_typrs,valm)" +
                " select t.nzp_kvar,t.nzp_frm,0 nzp_prm, f.nzp_frm_typ,1 rashod,1,2,1 valm" +
                " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f" +
                " where t.nzp_frm=f.nzp_frm and f.nzp_frm_typrs=2" +
                " and 0=(select count(*) from t_rsh_kmnl r where t.nzp_kvar=r.nzp_kvar)" +
                " group by 1,2,3,4 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }          

            ret = ExecSQL(conn_db,
                " insert into t_rsh (nzp_kvar,nzp_frm,nzp_prm,rashod,rashod_g,nzp_frm_typrs,rsh1,nzp_frm_typ)" +
                " select t.nzp_kvar,t.nzp_frm,r.nzp_prm,r.rashod,r.rashod,2,r.val_prm,f.nzp_frm_typ" +
                " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f,t_rsh_kmnl r" +
                " where t.nzp_kvar=r.nzp_kvar and t.nzp_frm=f.nzp_frm and f.nzp_frm_typrs=2 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            #endregion Тип расхода = 2 (с учетом индивидуального расхода коммунальных квартир)

            #region Тип расхода = 11 (с учетом индивидуального расхода коммунальных квартир)
            // тип расхода = 11
            ret = ExecSQL(conn_db,
                " insert into t_rsh (nzp_kvar,nzp_frm,nzp_prm,nzp_frm_typ,rashod,rashod_g,nzp_frm_typrs,valm)" +
                " select t.nzp_kvar,t.nzp_frm,p.nzp_prm,f.nzp_frm_typ, max(p.val" + sConvToNum + "),max(p.val" + sConvToNum + "),11,max(p.val" + sConvToNum + ")" +
                " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f,t_par1 p" +
                " where t.nzp_frm=f.nzp_frm and t.nzp_kvar=p.nzp and p.nzp_prm=f.nzp_prm_rash and f.nzp_frm_typrs=11" +
                " and 0=(select count(*) from t_kmnl r where t.nzp_kvar=r.nzp_kvar)" +
                " group by 1,2,3,4 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
            ret = ExecSQL(conn_db,
                " insert into t_rsh (nzp_kvar,nzp_frm,nzp_prm,nzp_frm_typ,rashod,rashod_g,nzp_frm_typrs,valm)" +
                " select t.nzp_kvar,t.nzp_frm,p.nzp_prm,f.nzp_frm_typ,max(p.val" + sConvToNum + "),max(p.val" + sConvToNum + "),11,max(p.val" + sConvToNum + ")" +
                " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f,t_par1 p" +
                " where t.nzp_frm=f.nzp_frm and t.nzp_kvar=p.nzp and p.nzp_prm=f.nzp_prm_rash1 and f.nzp_frm_typrs=11" +
                " and 0<(select count(*) from t_kmnl r where t.nzp_kvar=r.nzp_kvar)" +
                " group by 1,2,3,4 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            #endregion Тип расхода = 11 (с учетом индивидуального расхода коммунальных квартир)

            #region Тип расхода = 106 (количество жильцов +временно проживающие - временно выбывшие и др человечьи)
            // тип расхода = 106
            ret = ExecSQL(conn_db,
                " insert into t_rsh (nzp_kvar,nzp_frm,nzp_prm,nzp_frm_typ,rashod,nzp_frm_typrs,rsh1,rashod_g,valm)" +
                " select t.nzp_kvar,t.nzp_frm,5 nzp_prm,f.nzp_frm_typ," + 
                sNvlWord + "(g.kg,0) + " + sNvlWord + "(g.kvp,0) - " + sNvlWord + "(g.knpl,0),106," + sNvlWord + "(g.knpl,0)," + 
                sNvlWord + "(g.kg_g,0)+" + sNvlWord + "(g.kvp,0) - " + sNvlWord + "(g.knpl,0)," + 
                sNvlWord + "(g.kg,0) + " + sNvlWord + "(g.kvp,0) - " + sNvlWord + "(g.knpl,0)" +
                " from " + gku.paramcalc.kernel_alias + "formuls_opis f, t_trf t" +
#if PG
                " left outer join t_rsh_kolgil g on t.nzp_kvar=g.nzp_kvar where " +
#else
                ",outer t_rsh_kolgil g where t.nzp_kvar=g.nzp_kvar and" +
#endif
                " t.nzp_frm=f.nzp_frm and f.nzp_frm_typrs=106 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            #endregion Тип расхода = 106 (количество жильцов +временно проживающие - временно выбывшие и др человечьи)

            #region Тип расхода = 3 (количество жильцов + временно проживающие УЧИТЫВАЯ временно выбывших и др человечьи)
            // тип расхода = 3
            ret = ExecSQL(conn_db,
                " insert into t_rsh (nzp_kvar,nzp_frm,nzp_prm,rashod,nzp_frm_typrs,rashod_g,nzp_frm_typ,valm)" +
                " select t.nzp_kvar,t.nzp_frm,5 nzp_prm," + sNvlWord + "(g.kg,0)+" + sNvlWord + "(g.kvp,0),3," +
                sNvlWord + "(g.kg_g,0)+" + sNvlWord + "(g.kvp,0),f.nzp_frm_typ," + sNvlWord + "(g.kg,0)+" + sNvlWord + "(g.kvp,0)" +
                " from " + gku.paramcalc.kernel_alias + "formuls_opis f,t_trf t " +
#if PG
                " left outer join t_rsh_kolgil g on t.nzp_kvar=g.nzp_kvar where " +
#else
                ",outer t_rsh_kolgil g where t.nzp_kvar=g.nzp_kvar and " +
#endif
                " t.nzp_frm=f.nzp_frm and f.nzp_frm_typrs=3 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
            #endregion Тип расхода = 3 (количество жильцов + временно проживающие УЧИТЫВАЯ временно выбывших и др человечьи)

            #region Тип расхода = 300 (количество жильцов + временно проживающие НЕ УЧИТЫВАЯ временно выбывших и др человечьи)
            // тип расхода = 300
            ret = ExecSQL(conn_db,
                " insert into t_rsh (nzp_kvar,nzp_frm,nzp_prm,rashod,nzp_frm_typrs,rashod_g,nzp_frm_typ,valm)" +
                " select t.nzp_kvar,t.nzp_frm,5 nzp_prm," + sNvlWord + "(g.kg_g,0)+" + sNvlWord + "(g.kvp,0),300," +
                sNvlWord + "(g.kg_g,0)+" + sNvlWord + "(g.kvp,0),f.nzp_frm_typ," + sNvlWord + "(g.kg_g,0)+" + sNvlWord + "(g.kvp,0)" +
                " from " + gku.paramcalc.kernel_alias + "formuls_opis f,t_trf t " +
#if PG
                " left outer join t_rsh_kolgil g on t.nzp_kvar=g.nzp_kvar where " +
#else
                ",outer t_rsh_kolgil g where t.nzp_kvar=g.nzp_kvar and " +
#endif
                " t.nzp_frm=f.nzp_frm and f.nzp_frm_typrs=300 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
            #endregion Тип расхода = 300 (количество жильцов + временно проживающие НЕ УЧИТЫВАЯ временно выбывших и др человечьи)

            #region Тип расхода = 4

            // тип расхода = 4
            ret = ExecSQL(conn_db,
                " insert into t_rsh (nzp_kvar,nzp_frm,nzp_prm,nzp_frm_typ,rashod,rashod_g,nzp_frm_typrs,valm)" +
                " select t.nzp_kvar,t.nzp_frm,0 nzp_prm,f.nzp_frm_typ,1 rashod,1,4,1 valm" +
                " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f" +
                " where t.nzp_frm=f.nzp_frm and f.nzp_frm_typrs=4" +
                " group by 1,2,3,4 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            #endregion Тип расхода = 4

            #region Тип расхода = 509

            // тип расхода = 509

            // выбрать перечень ЛС
            ret = ExecSQL(conn_db, " drop table t_z509 ", false);
            ret = ExecSQL(conn_db,
                " create temp table t_z509 (" +
                " nzp_kvar integer," +
                " nzp_frm  integer," +
                " nzp_frm_typ integer," +
                " valk " + sDecimalType + "(14,7) default 0," +
                " valr " + sDecimalType + "(14,7) default 0," +
                " valm " + sDecimalType + "(14,7) default 0" +
#if PG
                " ) "
#else
                " ) with no log "
#endif
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            ret = ExecSQL(conn_db,
                " insert into t_z509 (nzp_kvar,nzp_frm,nzp_frm_typ)" +
                " select t.nzp_kvar,t.nzp_frm,f.nzp_frm_typ " +
                " from t_trf t," + gku.paramcalc.kernel_alias + "formuls_opis f" +
                " where t.nzp_frm=f.nzp_frm and f.nzp_frm_typrs=509" +
                " group by 1,2,3 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            ExecSQL(conn_db, sUpdStat + " t_z509 ", true);

            //ViewTbl(conn_db, " select * from t_z509 order by nzp_kvar ");

            // выбрать параметры нормы расхода на 1 ед.(ГОЛОВУ!) количества животних
            ret = ExecSQL(conn_db, " drop table t_k509 ", false);
            ret = ExecSQL(conn_db,
                " create temp table t_k509 (" +
                " ipos integer," +
                " nzp_prmk integer," +
                " valk " + sDecimalType + "(14,7) default 0" +
#if PG
                " ) "
#else
                " ) with no log "
#endif
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            ret = ExecSQL(conn_db,
                " insert into t_k509 (ipos,nzp_prmk)" +
                " select trunc(order/4) ipos,nzp_prm " +
                " from " + gku.paramcalc.kernel_alias + "prm_frm " +
                " where nzp_frm = 509 and frm_calc = 2 and mod(order-1,4)=0 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            //ViewTbl(conn_db, " select * from t_k509 ");

            // выбрать параметры количества животних
            ret = ExecSQL(conn_db, " drop table t_r509 ", false);
            ret = ExecSQL(conn_db,
                " create temp table t_r509 (" +
                " ipos integer," +
                " nzp_prmr integer," +
                " valr " + sDecimalType + "(14,7) default 0" +
#if PG
                " ) "
#else
                " ) with no log "
#endif
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            ret = ExecSQL(conn_db,
                " insert into t_r509 (ipos,nzp_prmr)" +
                " select trunc(order/4) ipos,nzp_prm " +
                " from " + gku.paramcalc.kernel_alias + "prm_frm " +
                " where nzp_frm = 509 and frm_calc = 2 and mod(order+1,4)=0 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            //ViewTbl(conn_db, " select * from t_r509 ");

            ret = ExecSQL(conn_db, " drop table t_v509 ", false);
            ret = ExecSQL(conn_db,
                " select z.nzp_kvar,z.nzp_frm,z.nzp_frm_typ,a.ipos,a.nzp_prmr,b.nzp_prmk,z.valk,z.valr,z.valm " +
#if PG
                " into temp t_v509 " + 
#else
                " " +
#endif
                " from t_z509 z, t_r509 a, t_k509 b " +
                " where a.ipos=b.ipos " +
#if PG
                " "
#else
                " into temp t_v509 with no log " 
#endif
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            ExecSQL(conn_db, " Create index ix_t_v509 on t_v509 (nzp_kvar) ", true);
            ExecSQL(conn_db, sUpdStat + " t_v509 ", true);

            ret = ExecSQL(conn_db,
                " update t_v509 set " +
                " valk=(select max(p.val_prm" + sConvToNum + ") from " + gku.paramcalc.data_alias + "prm_5 p where p.nzp_prm=t_v509.nzp_prmk" +
                       " and p.is_actual<>100 and p.dat_s<" + gku.dat_po + " and p.dat_po>=" + gku.dat_s + "), " +
                " valr=(select max(p.val" + sConvToNum + ") from t_par1 p where p.nzp=t_v509.nzp_kvar and p.nzp_prm=t_v509.nzp_prmr) " +
                " where 0<(select count(*) from t_par1 p where p.nzp=t_v509.nzp_kvar and p.nzp_prm=t_v509.nzp_prmr) "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            ret = ExecSQL(conn_db,
                " update t_v509 set valm = valk * valr " +
                " where 1=1 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            //ViewTbl(conn_db, " select * from t_par1 order by nzp ");

            //ViewTbl(conn_db, "select * from " + gku.paramcalc.data_alias + "prm_5 p where p.nzp_prm in (318,319,320,321,467,948,1108,1109,1110,1111)" +
            //           " and p.is_actual<>100 and p.dat_s<" + gku.dat_po + " and p.dat_po>=" + gku.dat_s + " ");

            //ViewTbl(conn_db, " select * from t_v509 order by nzp_kvar ");
            
            ret = ExecSQL(conn_db,
                " insert into t_rsh (nzp_kvar,nzp_frm,nzp_prm,nzp_frm_typ,rashod,rashod_g,nzp_frm_typrs,valm)" +
                " select t.nzp_kvar,t.nzp_frm,0 nzp_prm,t.nzp_frm_typ,sum(t.valm),sum(t.valm),509,sum(t.valm)" +
                " from t_v509 t " +
                " where 1=1 Group by 1,2,3,4 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            ret = ExecSQL(conn_db, " drop table t_v509 ", true);
            ret = ExecSQL(conn_db, " drop table t_z509 ", true);
            ret = ExecSQL(conn_db, " drop table t_r509 ", true);
            ret = ExecSQL(conn_db, " drop table t_k509 ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            #endregion Тип расхода = 509

            #region Тип расхода = 7 -- для Самары Эл/эн лифтов от кол-ва жильцов, если есть эл/снабжение
            // тип расхода = 7 -- для Самары Эл/эн лифтов от кол-ва жильцов, если есть эл/снабжение
            ret = ExecSQL(conn_db,
                " insert into t_rsh (nzp_kvar,nzp_frm,nzp_prm,nzp_frm_typ,rashod,nzp_frm_typrs,rashod_g,valm)" +
                " select t.nzp_kvar,t.nzp_frm,p.nzp_prm,f.nzp_frm_typ," +
                " max( (p.val_prm" + sConvToNum + ") * (" + sNvlWord + "(g.kg,0) + " + sNvlWord + "(g.kvp,0)) ),7," +
                " max( (p.val_prm" + sConvToNum + ") * (" + sNvlWord + "(g.kg_g,0)+" + sNvlWord + "(g.kvp,0)) )," +
                " max( (p.val_prm" + sConvToNum + ") * (" + sNvlWord + "(g.kg,0) + " + sNvlWord + "(g.kvp,0)) )  " +
                " from " + gku.paramcalc.kernel_alias + "formuls_opis f," + gku.paramcalc.data_alias + "prm_5 p, t_trf t" +
#if PG
                " left outer join t_rsh_kolgil g on t.nzp_kvar=g.nzp_kvar  where" +
#else
                ",outer t_rsh_kolgil g where t.nzp_kvar=g.nzp_kvar and " +
#endif
                " t.nzp_frm=f.nzp_frm and p.nzp_prm=f.nzp_prm_rash and f.nzp_frm_typrs=7" +
                "   and p.is_actual<>100 and p.dat_s<" + gku.dat_po + " and p.dat_po>=" + gku.dat_s + " " +
                "   and 0<(select count(*) from " + gku.counters_xx + " s where t.nzp_kvar=s.nzp_kvar and s.nzp_type=3 and s.stek=3 " +
                "        and s.nzp_serv=25 and s.rashod>0) " +
                " group by 1,2,3,4 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            #endregion Тип расхода = 7 -- для Самары Эл/эн лифтов от кол-ва жильцов, если есть эл/снабжение

            #region Тип расхода = 512-- Эл/эн лифтов от кол-ва жильцов
            // тип расхода = 512-- Эл/эн лифтов от кол-ва жильцов
            // кол-во жильцов
            ret = ExecSQL(conn_db,
                " insert into t_rsh (nzp_kvar,nzp_frm,nzp_prm,nzp_frm_typ,rsh2,nzp_frm_typrs,rsh2_g)" +
                " select t.nzp_kvar,t.nzp_frm,0 nzp_prm,f.nzp_frm_typ,max( nvl(g.kg,0)+nvl(g.kvp,0)-nvl(g.knpl,0) ),512,max( nvl(g.kg_g,0)+nvl(g.kvp,0)-nvl(g.knpl,0) )" +
                " from " + gku.paramcalc.kernel_alias + "formuls_opis f, t_trf t" +
#if PG
                " left outer join t_rsh_kolgil g on t.nzp_kvar=g.nzp_kvar where " +
#else
                ",outer t_rsh_kolgil g where t.nzp_kvar=g.nzp_kvar and " +
#endif
                " t.nzp_frm=f.nzp_frm and f.nzp_frm_typrs=512" +
                " group by 1,2,3,4 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            // 1я норма на 1 человека - на базу
            ret = ExecSQL(conn_db,
                " update t_rsh set " +
#if PG
                " nzp_prm=(select max(p.nzp_prm)" +
                "   from " + gku.paramcalc.kernel_alias + "formuls_opis f," + gku.paramcalc.data_alias + "prm_5 p" +
                "   where p.nzp_prm=f.nzp_prm_rash and f.nzp_frm_typrs=512" +
                "   and p.is_actual<>100 and p.dat_s<" + gku.dat_po + " and p.dat_po>=" + gku.dat_s + ")," +
                " rsh1=  (select max(coalesce(p.val_prm,'0')::numeric)" +
                "   from " + gku.paramcalc.kernel_alias + "formuls_opis f," + gku.paramcalc.data_alias + "prm_5 p" +
                "   where p.nzp_prm=f.nzp_prm_rash and f.nzp_frm_typrs=512" +
                "   and p.is_actual<>100 and p.dat_s<" + gku.dat_po + " and p.dat_po>=" + gku.dat_s + ") " +
#else
                " (nzp_prm,rsh1)=" +
                "  ((select max(p.nzp_prm),max(nvl(p.val_prm,'0')+0)" +
                "   from " + gku.paramcalc.kernel_alias + "formuls_opis f," + gku.paramcalc.data_alias + "prm_5 p" +
                "   where p.nzp_prm=f.nzp_prm_rash and f.nzp_frm_typrs=512" +
                "   and p.is_actual<>100 and p.dat_s<" + gku.dat_po + " and p.dat_po>=" + gku.dat_s + ")) " +
#endif
                " where nzp_frm_typrs=512 and " +
                "  0<(select count(*)" +
                "   from " + gku.paramcalc.kernel_alias + "formuls_opis f," + gku.paramcalc.data_alias + "prm_5 p" +
                "   where p.nzp_prm=f.nzp_prm_rash and f.nzp_frm_typrs=512" +
                "   and p.is_actual<>100 and p.dat_s<" + gku.dat_po + " and p.dat_po>=" + gku.dat_s + ") "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            // 2я норма на 1 человека - на л/с среднее
            ret = ExecSQL(conn_db,
                " update t_rsh set" +
#if PG
                " nzp_prm=(select max(p.nzp_prm)" +
                "   from " + gku.paramcalc.kernel_alias + "formuls_opis f,t_par1 p" +
                "   where p.nzp_prm=f.nzp_prm_rash1 and f.nzp_frm_typrs=512)," +
                " rsh1=" +
                "  (select max(coalesce(p.val,'0')::numeric)" +
                "   from " + gku.paramcalc.kernel_alias + "formuls_opis f,t_par1 p" +
                "   where p.nzp_prm=f.nzp_prm_rash1 and f.nzp_frm_typrs=512)" +
#else
                " (nzp_prm,rsh1)=" +
                "  ((select max(p.nzp_prm),max(nvl(p.val,'0')+0)" +
                "   from " + gku.paramcalc.kernel_alias + "formuls_opis f,t_par1 p" +
                "   where p.nzp_prm=f.nzp_prm_rash1 and f.nzp_frm_typrs=512))" +
#endif
                " where nzp_frm_typrs=512 and " +
                "  0<(select count(*)" +
                "   from " + gku.paramcalc.kernel_alias + "formuls_opis f,t_par1 p" +
                "   where p.nzp_prm=f.nzp_prm_rash1 and f.nzp_frm_typrs=512) "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            // 3я норма на 1 человека - на л/с изменение
            ret = ExecSQL(conn_db,
                " update t_rsh set" +
#if PG
                " nzp_prm=(select max(p.nzp_prm)" +
                "   from " + gku.paramcalc.kernel_alias + "formuls_opis f,t_par1 p" +
                "   where p.nzp_prm=f.nzp_prm_rash2 and f.nzp_frm_typrs=512)," +
                " rsh1=" +
                "  (select max(coalesce(p.val,'0')::numeric)" +
                "   from " + gku.paramcalc.kernel_alias + "formuls_opis f,t_par1 p" +
                "   where p.nzp_prm=f.nzp_prm_rash2 and f.nzp_frm_typrs=512)" +
#else
                " (nzp_prm,rsh1)=" +
                "  ((select max(p.nzp_prm),max(nvl(p.val,'0')+0)" +
                "   from " + gku.paramcalc.kernel_alias + "formuls_opis f,t_par1 p" +
                "   where p.nzp_prm=f.nzp_prm_rash2 and f.nzp_frm_typrs=512))" +
#endif
                " where nzp_frm_typrs=512 and " +
                "  0<(select count(*)" +
                "   from " + gku.paramcalc.kernel_alias + "formuls_opis f,t_par1 p" +
                "   where p.nzp_prm=f.nzp_prm_rash2 and f.nzp_frm_typrs=512)"
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            // расход = норма на 1 человека * кол-во жильцов
            ret = ExecSQL(conn_db,
                " update t_rsh set rashod=rsh1*rsh2,rashod_g=rsh1*rsh2_g,valm=rsh1*rsh2 " +
                " where nzp_frm_typrs=512 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
            #endregion Тип расхода = 512-- Эл/эн лифтов от кол-ва жильцов
            
              #region Тип расхода = 5
            // тип расхода = 5
                       
            d_dat_charge = " and s.dat_charge is null ";
            if (!gku.paramcalc.b_cur)
            {
                d_dat_charge = " and s.dat_charge = " + MDY(gku.paramcalc.cur_mm, 28, gku.paramcalc.cur_yy);
            }
// Пост 307 и т.п.
            ret = ExecSQL(conn_db,
                " insert into t_rsh (nzp_kvar,nzp_frm,nzp_prm,rashod,rashod_g,is_device,nzp_frm_typrs,nzp_frm_typ,dlt_reval,valm,dop87,rash_norm_one)" +
                " select t.nzp_kvar,t.nzp_frm,0 nzp_prm,max(case when s.rashod>0 then s.rashod else 0 end)," +
                " max(case when s.cnt_stage in (1,9) then (case when s.rashod>0 then s.rashod else 0 end) else s.val1_g end)," +
                " max(s.cnt_stage),f.nzp_frm_typrs,f.nzp_frm_typ,max(s.dlt_reval),max(s.val2 + s.val1),min(s.dop87),max(rash_norm_one) " +
                " from t_tarifs t," + gku.paramcalc.kernel_alias + "formuls_opis f," + gku.counters_xx + " s" +
                " where t.nzp_kvar=s.nzp_kvar and (case when t.nzp_serv=14 then 9 else t.nzp_serv end)=s.nzp_serv " + d_dat_charge + 
                " and s.nzp_type=3 and s.stek=3 and not (s.kod_info in (21,22,23,26,27,31,32,33))" +
                " and t.nzp_frm=f.nzp_frm and f.nzp_frm_typrs in (5,440)" +
                " group by 1,2,7,8 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
// Пост 354 и ОДН<0 - нет
            ret = ExecSQL(conn_db,
                " insert into t_rsh (nzp_kvar,nzp_frm,nzp_prm,rashod,rashod_g,is_device,nzp_frm_typrs,nzp_frm_typ,valm,dop87,kod_info,dlt_reval,rash_norm_one)" +
                " select t.nzp_kvar,t.nzp_frm,0 nzp_prm,max(case when s.rashod>0 then s.rashod else 0 end)," +
                " max(case when s.cnt_stage in (1,9) then (case when s.rashod>0 then s.rashod else 0 end) else s.val1_g end)," +
                " max(s.cnt_stage),f.nzp_frm_typrs,f.nzp_frm_typ,max(s.val2 + s.val1),min(s.dop87),max(s.kod_info),min(s.dlt_reval),max(s.rash_norm_one) " +
                " from t_tarifs t," + gku.paramcalc.kernel_alias + "formuls_opis f," + gku.counters_xx + " s" +
                " where t.nzp_kvar=s.nzp_kvar and (case when t.nzp_serv=14 then 9 else t.nzp_serv end)=s.nzp_serv " + d_dat_charge +
                " and s.nzp_type=3 and s.stek=3 and (s.kod_info in (31,32,33))" +
                " and t.nzp_frm=f.nzp_frm and f.nzp_frm_typrs in (5,440)" +
                " group by 1,2,7,8 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
// Пост 354 и ОДН>0 - есть
            // основная услуга
            ret = ExecSQL(conn_db,
                " insert into t_rsh (nzp_kvar,nzp_frm,nzp_prm,rashod,rashod_g,is_device,nzp_frm_typrs,nzp_frm_typ,valm,dop87,kod_info,dlt_reval,rash_norm_one)" +
                " select t.nzp_kvar,t.nzp_frm,0 nzp_prm,max(s.val2 + s.val1)," +
                " max(s.val2 + (case when s.cnt_stage in (1,9) then s.val1 else s.val1_g end) )," +
                " max(s.cnt_stage),f.nzp_frm_typrs,f.nzp_frm_typ,max(s.val2 + s.val1),max(s.dop87),max(s.kod_info),max(s.dlt_reval),max(s.rash_norm_one)" +
                " from t_tarifs t," + gku.paramcalc.kernel_alias + "formuls_opis f," + gku.counters_xx + " s" +
                " where t.nzp_kvar=s.nzp_kvar and (case when t.nzp_serv=14 then 9 else t.nzp_serv end)=s.nzp_serv " + d_dat_charge +
                " and s.nzp_type=3 and s.stek=3 and (s.kod_info in (21,22,23,26,27))" +
                " and t.nzp_frm=f.nzp_frm and f.nzp_frm_typrs in (5,440)" +
                " group by 1,2,7,8 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            // услуга на ОДН
            ret = ExecSQL(conn_db,
                " insert into t_rsh (nzp_kvar,nzp_frm,nzp_prm,rashod,rashod_g,is_device,nzp_frm_typrs,nzp_frm_typ)" +
                " select t.nzp_kvar,t.nzp_frm,0 nzp_prm,max(case when s.dop87>0 then s.dop87 else 0 end)," +
                " max(case when s.dop87>0 then s.dop87 else 0 end),max(s.cnt_stage),f.nzp_frm_typrs,f.nzp_frm_typ " +
                " from t_tarifs t," + gku.paramcalc.kernel_alias + "formuls_opis f," + gku.counters_xx + " s," + gku.paramcalc.kernel_alias + "serv_odn n" +
                " where t.nzp_kvar=s.nzp_kvar and t.nzp_serv=n.nzp_serv and s.nzp_serv=(case when n.nzp_serv_link=14 then 9 else n.nzp_serv_link end) " + d_dat_charge +
                " and s.nzp_type=3 and s.stek=3 and (s.kod_info in (21,22,23,26,27))" +
                " and t.nzp_frm=f.nzp_frm and f.nzp_frm_typrs in (5,440)" +
                " group by 1,2,7,8 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }              

            #endregion Тип расхода = 5

              #region Тип расхода = 39 -КАН
            // тип расхода = 39 - КАН
            ret = ExecSQL(conn_db,
                " insert into t_rsh (nzp_kvar,nzp_frm,nzp_prm,nzp_frm_typ,rashod,rashod_g,rsh_hv,is_device,nzp_frm_typrs,rash_norm_one)" +
                " select t.nzp_kvar,t.nzp_frm,0 nzp_prm,f.nzp_frm_typ,max(case when s.rashod>0 then s.rashod else 0 end)," +
                " max(case when s.cnt_stage in (1,9) then (case when s.rashod>0 then s.rashod else 0 end) else s.val1_g end)," +
                " max(s.val4),max(s.cnt_stage),39,max(s.rash_norm_one)" +
                " from t_tarifs t," + gku.paramcalc.kernel_alias + "formuls_opis f," + gku.counters_xx + " s" +
                " where t.nzp_kvar=s.nzp_kvar and t.nzp_serv=s.nzp_serv and t.nzp_serv=7 " + d_dat_charge +
                " and s.nzp_type=3 and s.stek=3 and not (s.kod_info in (21,22,23,26,27))" +
                " and t.nzp_frm=f.nzp_frm and f.nzp_frm_typrs=39" +
                " group by 1,2,3,4 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
// Пост 354
            // основная услуга
            ret = ExecSQL(conn_db,
                " insert into t_rsh (nzp_kvar,nzp_frm,nzp_prm,nzp_frm_typ,rashod,rashod_g,rsh_hv,is_device,nzp_frm_typrs,rash_norm_one)" +
                " select t.nzp_kvar,t.nzp_frm,0 nzp_prm,f.nzp_frm_typ,max(s.val2 + s.val1)," +
                " max(s.val2 + (case when s.cnt_stage in (1,9) then s.val1 else s.val1_g end) )," +
                " max(s.val4),max(s.cnt_stage),39,max(s.rash_norm_one)" +
                " from t_tarifs t," + gku.paramcalc.kernel_alias + "formuls_opis f," + gku.counters_xx + " s" +
                " where t.nzp_kvar=s.nzp_kvar and t.nzp_serv=s.nzp_serv and t.nzp_serv=7 " + d_dat_charge +
                " and s.nzp_type=3 and s.stek=3 and (s.kod_info in (21,22,23,26,27))" +
                " and t.nzp_frm=f.nzp_frm and f.nzp_frm_typrs=39" +
                " group by 1,2,3,4 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
            // услуга на ОДН
            ret = ExecSQL(conn_db,
                " insert into t_rsh (nzp_kvar,nzp_frm,nzp_prm,nzp_frm_typ,rashod,rashod_g,rsh_hv,is_device,nzp_frm_typrs)" +
                " select t.nzp_kvar,t.nzp_frm,0 nzp_prm,f.nzp_frm_typ,max(case when s.dop87>0 then s.dop87 else 0 end)," +
                " max(case when s.dop87>0 then s.dop87 else 0 end),0,max(s.cnt_stage),39 " +
                " from t_tarifs t," + gku.paramcalc.kernel_alias + "formuls_opis f," + gku.counters_xx + " s," + gku.paramcalc.kernel_alias + "serv_odn n" +
                " where t.nzp_kvar=s.nzp_kvar and t.nzp_serv=n.nzp_serv and s.nzp_serv=7 and s.nzp_serv=n.nzp_serv_link " + d_dat_charge +
                " and s.nzp_type=3 and s.stek=3 and (s.kod_info in (21,22,23,26,27))" +
                " and t.nzp_frm=f.nzp_frm and f.nzp_frm_typrs=39" +
                " group by 1,2,3,4 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
            
            ret = ExecSQL(conn_db,
                " update t_rsh set valm=rashod " +
                " where nzp_frm_typrs=39 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

              #endregion Тип расхода = 39 -КАН

              #region Тип расхода 390 -КАН =ХВ+ГВ
            // тип расхода = 390 - КАН = ХВ + ГВ

#if PG
            ret = ExecSQL(conn_db,
                //" create table are.t_rsh390 (" +
                           " create temp table t_rsh390 (" +
                           " nzp_kvar integer," +
                           " nzp_frm  integer," +
                           " nzp_prm  integer default 0," +
                           " rashod   numeric(16,6) default 0," +
                           " rashod_g numeric(16,6) default 0," +
                           " rash_kv  numeric(16,6) default 0," +
                           " rash_odn numeric(16,6) default 0," +
                           " rsh_kn   numeric(16,6) default 0," +
                           " rsh_hv   numeric(16,6) default 0," +
                           " rsh_gv   numeric(16,6) default 0," +
                           " rsh_kn_g   numeric(16,6) default 0," +
                           " rsh_hv_g   numeric(16,6) default 0," +
                           " rsh_gv_g   numeric(16,6) default 0," +
                           " rsh_kv_kn    numeric(16,6) default 0," +
                           " rsh_kv_hv    numeric(16,6) default 0," +
                           " rsh_kv_gv    numeric(16,6) default 0," +
                           " rsh_odn_kn   numeric(16,6) default 0," +
                           " rsh_odn_hv   numeric(16,6) default 0," +
                           " rsh_odn_gv   numeric(16,6) default 0," +
                           " is_device    integer  default 0," +
                           " is_device_kn integer  default 0," +
                           " is_device_hv integer  default 0," +
                           " is_device_gv integer  default 0, " +
                           " kod_info     integer  default 0," +
                           " kod_info_kn  integer  default 0," +
                           " kod_info_hv  integer  default 0," +
                           " nzp_frm_typ  integer  default 0 not null, " +
                           " kod_info_gv  integer  default 0 " +
                //" ) "
                           " )  "
                           , true);
#else
            ret = ExecSQL(conn_db,
                //" create table are.t_rsh390 (" +
                " create temp table t_rsh390 (" +
                " nzp_kvar integer," +
                " nzp_frm  integer," +
                " nzp_prm  integer default 0," +
                " rashod   decimal(16,6) default 0," +
                " rashod_g decimal(16,6) default 0," +
                " rash_kv  decimal(16,6) default 0," +
                " rash_odn decimal(16,6) default 0," +
                " rsh_kn   decimal(16,6) default 0," +
                " rsh_hv   decimal(16,6) default 0," +
                " rsh_gv   decimal(16,6) default 0," +
                " rsh_kn_g   decimal(16,6) default 0," +
                " rsh_hv_g   decimal(16,6) default 0," +
                " rsh_gv_g   decimal(16,6) default 0," +
                " rsh_kv_kn    decimal(16,6) default 0," +
                " rsh_kv_hv    decimal(16,6) default 0," +
                " rsh_kv_gv    decimal(16,6) default 0," +
                " rsh_odn_kn   decimal(16,6) default 0," +
                " rsh_odn_hv   decimal(16,6) default 0," +
                " rsh_odn_gv   decimal(16,6) default 0," +
                " is_device    integer  default 0," +
                " is_device_kn integer  default 0," +
                " is_device_hv integer  default 0," +
                " is_device_gv integer  default 0, " +
                " kod_info     integer  default 0," +
                " kod_info_kn  integer  default 0," +
                " kod_info_hv  integer  default 0," +
                " nzp_frm_typ  integer  default 0 not null, " +   
                " kod_info_gv  integer  default 0 " +
                //" ) "
                " ) with no log "
                , true);
#endif
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
            // вставить все расходы по КАН, где есть формулы
#if PG
            ret = ExecSQL(conn_db,
                " insert into t_rsh390 (nzp_kvar,nzp_frm,nzp_frm_typ,nzp_prm,rsh_kn,rsh_kn_g,is_device_kn,kod_info_kn,rsh_kv_kn,rsh_odn_kn)" +
                " select t.nzp_kvar,t.nzp_frm,f.nzp_frm_typ,0 nzp_prm,max(coalesce(s.rashod,0))," +
                " max(case when s.cnt_stage in (1,9) then coalesce(s.rashod,0) else coalesce(s.val1_g,0) end)," +
                " max(coalesce(s.cnt_stage,0)),max(coalesce(s.kod_info,0))," +
                " max(coalesce(s.val2,0) + coalesce(s.val1,0)),max(coalesce(s.dop87,0))" +
                " from t_tarifs t left outer join " + gku.counters_xx + " s on t.nzp_kvar=s.nzp_kvar and t.nzp_serv=s.nzp_serv and s.nzp_type=3 and s.stek=3,"
                + gku.paramcalc.pref + "_kernel.formuls_opis f " +
                " where  t.nzp_serv in (7,324) " + d_dat_charge +
                " and t.nzp_frm=f.nzp_frm and f.nzp_frm_typrs=390" +
                " group by 1,2,3 "
                , true);
#else
            ret = ExecSQL(conn_db,
                " insert into t_rsh390 (nzp_kvar,nzp_frm,nzp_frm_typ,nzp_prm,rsh_kn,rsh_kn_g,is_device_kn,kod_info_kn,rsh_kv_kn,rsh_odn_kn)" +
                " select t.nzp_kvar,t.nzp_frm,f.nzp_frm_typ,0 nzp_prm,max(nvl(s.rashod,0))," +
                " max(case when s.cnt_stage in (1,9) then nvl(s.rashod,0) else nvl(s.val1_g,0) end)," +
                " max(nvl(s.cnt_stage,0)),max(nvl(s.kod_info,0))," +
                " max(nvl(s.val2,0) + nvl(s.val1,0)),max(nvl(s.dop87,0))" +
                " from t_tarifs t," + gku.paramcalc.pref + "_kernel:formuls_opis f,outer " + gku.counters_xx + " s" +
                " where t.nzp_kvar=s.nzp_kvar and t.nzp_serv=s.nzp_serv and t.nzp_serv in (7,324) " + d_dat_charge + 
                " and s.nzp_type=3 and s.stek=3" +
                " and t.nzp_frm=f.nzp_frm and f.nzp_frm_typrs=390" +
                " group by 1,2,3 "
                , true);
#endif
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            ret = ExecSQL(conn_db, " create index ixt_rsh1_390 on t_rsh390(nzp_kvar) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            ret = ExecSQL(conn_db, " create index ixt_rsh2_390 on t_rsh390(kod_info) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
#if PG
            ret = ExecSQL(conn_db, " analyze t_rsh390 ", true);
#else
            ret = ExecSQL(conn_db, " update statistics for table t_rsh390 ", true);
#endif
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
            // добавить расход по ХВС
#if PG
            ret = ExecSQL(conn_db,
                " update t_rsh390 set nzp_prm=1,"+
                " is_device_hv = (select max(s.cnt_stage) from " + gku.counters_xx + " s" +
                  " where t_rsh390.nzp_kvar=s.nzp_kvar and s.nzp_serv=6 " + d_dat_charge + " and s.nzp_type=3 and s.stek=3),"+
                " rsh_hv = (select max(s.rashod) from " + gku.counters_xx + " s" +
                  " where t_rsh390.nzp_kvar=s.nzp_kvar and s.nzp_serv=6 " + d_dat_charge + " and s.nzp_type=3 and s.stek=3),"+
                " rsh_hv_g = (select max(case when s.cnt_stage in (1,9) then s.rashod else s.val1_g end) from " + gku.counters_xx + " s" +
                  " where t_rsh390.nzp_kvar=s.nzp_kvar and s.nzp_serv=6 " + d_dat_charge + " and s.nzp_type=3 and s.stek=3),"+
                " kod_info_hv = (select max(s.kod_info) from " + gku.counters_xx + " s" +
                  " where t_rsh390.nzp_kvar=s.nzp_kvar and s.nzp_serv=6 " + d_dat_charge + " and s.nzp_type=3 and s.stek=3),"+
                " rsh_kv_hv = (select max(s.val2 + s.val1) from " + gku.counters_xx + " s" +
                  " where t_rsh390.nzp_kvar=s.nzp_kvar and s.nzp_serv=6 " + d_dat_charge + " and s.nzp_type=3 and s.stek=3),"+
                " rsh_odn_hv = (select max(s.dop87) from " + gku.counters_xx + " s" +
                  " where t_rsh390.nzp_kvar=s.nzp_kvar and s.nzp_serv=6 " + d_dat_charge + " and s.nzp_type=3 and s.stek=3 )" +
                " where kod_info<>69 and 0<( select count(*) from " + gku.counters_xx + " s" +
                  " where t_rsh390.nzp_kvar=s.nzp_kvar and s.nzp_serv=6 " + d_dat_charge + " and s.nzp_type=3 and s.stek=3 )"
                , true);
#else
            ret = ExecSQL(conn_db,
                " update t_rsh390 set nzp_prm=1,(is_device_hv,rsh_hv,rsh_hv_g,kod_info_hv,rsh_kv_hv,rsh_odn_hv)=" +
                " (( select max(s.cnt_stage),max(s.rashod)," +
                " max(case when s.cnt_stage in (1,9) then s.rashod else s.val1_g end)," +
                " max(s.kod_info),max(s.val2 + s.val1),max(s.dop87)" +
                " from " + gku.counters_xx + " s" +
                " where t_rsh390.nzp_kvar=s.nzp_kvar and s.nzp_serv=6 " + d_dat_charge + " and s.nzp_type=3 and s.stek=3 ))" +
                //" where kod_info<>69 and 0<( select count(*) from " + gku.counters_xx + " s" +
                " where 0<( select count(*) from " + gku.counters_xx + " s" +
                " where t_rsh390.nzp_kvar=s.nzp_kvar and s.nzp_serv=6 " + d_dat_charge + " and s.nzp_type=3 and s.stek=3 )"
                , true);
#endif
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
            // добавить расход по ГВС
#if PG
            ret = ExecSQL(conn_db,
                " update t_rsh390 set nzp_prm=nzp_prm+3,"+
                " is_device_gv = (select max(s.cnt_stage) from " + gku.counters_xx + " s" +
                  " where t_rsh390.nzp_kvar=s.nzp_kvar and s.nzp_serv=9 " + d_dat_charge + " and s.nzp_type=3 and s.stek=3 ),"+
                " rsh_gv = (select max(s.rashod) from " + gku.counters_xx + " s" +
                  " where t_rsh390.nzp_kvar=s.nzp_kvar and s.nzp_serv=9 " + d_dat_charge + " and s.nzp_type=3 and s.stek=3 ),"+
                " rsh_gv_g = (select max(case when s.cnt_stage in (1,9) then s.rashod else s.val1_g end) from " + gku.counters_xx + " s" +
                  " where t_rsh390.nzp_kvar=s.nzp_kvar and s.nzp_serv=9 " + d_dat_charge + " and s.nzp_type=3 and s.stek=3 ),"+
                " kod_info_gv = (select max(s.kod_info) from " + gku.counters_xx + " s" +
                  " where t_rsh390.nzp_kvar=s.nzp_kvar and s.nzp_serv=9 " + d_dat_charge + " and s.nzp_type=3 and s.stek=3 )," +
                " rsh_kv_gv = (select max(s.val2 + s.val1) from " + gku.counters_xx + " s" +
                  " where t_rsh390.nzp_kvar=s.nzp_kvar and s.nzp_serv=9 " + d_dat_charge + " and s.nzp_type=3 and s.stek=3 ),"+
                " rsh_odn_gv = ( select max(s.dop87) from " + gku.counters_xx + " s" +
                  " where t_rsh390.nzp_kvar=s.nzp_kvar and s.nzp_serv=9 " + d_dat_charge + " and s.nzp_type=3 and s.stek=3 )" +
                " where kod_info<>69 and 0<( select count(*) from " + gku.counters_xx + " s" +
                  " where t_rsh390.nzp_kvar=s.nzp_kvar and s.nzp_serv=9 " + d_dat_charge + " and s.nzp_type=3 and s.stek=3 )"
                , true);
#else
            ret = ExecSQL(conn_db,
                " update t_rsh390 set nzp_prm=nzp_prm+3,(is_device_gv,rsh_gv,rsh_gv_g,kod_info_gv,rsh_kv_gv,rsh_odn_gv)=" +
                " (( select max(s.cnt_stage),max(s.rashod)," +
                " max(case when s.cnt_stage in (1,9) then s.rashod else s.val1_g end)," +
                " max(s.kod_info),max(s.val2 + s.val1),max(s.dop87)" +
                " from " + gku.counters_xx + " s" +
                " where t_rsh390.nzp_kvar=s.nzp_kvar and s.nzp_serv=9 " + d_dat_charge + " and s.nzp_type=3 and s.stek=3 ))" +
                //" where kod_info<>69 and 0<( select count(*) from " + gku.counters_xx + " s" +
                " where 0<( select count(*) from " + gku.counters_xx + " s" +
                " where t_rsh390.nzp_kvar=s.nzp_kvar and s.nzp_serv=9 " + d_dat_charge + " and s.nzp_type=3 and s.stek=3 )"
                , true);
#endif
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
            // расход КАН = ХВС + ГВС
            ret = ExecSQL(conn_db,
                " update t_rsh390 set" +
                    " rsh_hv    = case when (kod_info_hv in (31,32,33)) and rsh_hv<0 then 0 else rsh_hv end," +
                    " rsh_hv_g  = case when (kod_info_hv in (31,32,33)) and rsh_hv_g<0 then 0 else rsh_hv_g end," +
                    " rsh_odn_hv= case when kod_info_hv in (31,32,33) then 0 else rsh_odn_hv end," +
                    " rsh_gv    = case when (kod_info_gv in (31,32,33)) and rsh_gv<0 then 0 else rsh_gv end," +
                    " rsh_gv_g  = case when (kod_info_gv in (31,32,33)) and rsh_gv_g<0 then 0 else rsh_gv_g end," +
                    " rsh_odn_gv= case when kod_info_gv in (31,32,33) then 0 else rsh_odn_gv end," +
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
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            ret = ExecSQL(conn_db,
                " update t_rsh390 set rash_kv=rsh_kv_hv+rsh_kv_gv,rash_odn=rsh_odn_hv+rsh_odn_gv,rashod_g=rsh_hv_g+rsh_gv_g" +
                //", is_device=(case when is_device_hv=1 then 1 else (case when is_device_gv=1 then 1 else 0 end) end)" +
                //" where kod_info<>69 "
                " where 1=1 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            ret = ExecSQL(conn_db,
                " update t_rsh390 set kod_info=kod_info_hv" +
                //" where kod_info<>69 and kod_info_hv in (21,22,23,26) and kod_info_gv in (21,22,23,26) "
                " where kod_info_hv in (21,22,23,26) and kod_info_gv in (21,22,23,26) "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            ret = ExecSQL(conn_db,
                " update t_rsh390 set rsh_hv=rsh_kv_hv, kod_info=kod_info_hv" +
                //" where kod_info<>69 and kod_info_hv in (21,22,23,26) and kod_info_gv not in (21,22,23,26) "
                " where kod_info_hv in (21,22,23,26) and kod_info_gv not in (21,22,23,26) "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            ret = ExecSQL(conn_db,
                " update t_rsh390 set rsh_gv=rsh_kv_gv, kod_info=kod_info_gv" +
                //" where kod_info<>69 and kod_info_hv not in (21,22,23,26) and kod_info_gv in (21,22,23,26) "
                " where kod_info_hv not in (21,22,23,26) and kod_info_gv in (21,22,23,26) "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            ret = ExecSQL(conn_db,
                " update t_rsh390 set rashod=rsh_hv+rsh_gv,rash_kv=rsh_hv+rsh_gv,nzp_prm=nzp_prm+100" +
                //" where kod_info<>69 "
                " where 1=1 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            /*
            ret = ExecSQL(conn_db,
                " update t_rsh390 set rashod=rsh_hv+rsh_gv,rash_odn=rsh_odn_hv+rsh_odn_gv,rash_kv=rsh_kv_hv+rsh_kv_gv,rashod_g=rsh_hv_g+rsh_gv_g," +
                " is_device=(case when is_device_hv=1 then 1 else (case when is_device_gv=1 then 1 else 0 end) end)" +
                " where kod_info<>69 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }
            */

            /*
            // перекрыть расходом КПУ по КАН, там где уже был посчитан  КАН = ХВС + ГВС - по kod_info=69
            ret = ExecSQL(conn_db,
                " update t_rsh390 set nzp_prm=2, rashod=rsh_kn, is_device=is_device_kn, rash_odn=rsh_odn_kn, rash_kv=rsh_kv_kn," +
                " kod_info=kod_info_kn, rashod_g=rsh_kn_g " +
                " where kod_info=69"
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db);  return false; }
            */

            // перекрыть расходом КПУ по КАН, там где они есть
            ret = ExecSQL(conn_db,
                " update t_rsh390 set nzp_prm=0, rashod=rsh_kn, is_device=1, rash_odn=rsh_odn_kn, rash_kv=rsh_kv_kn, kod_info=kod_info_kn" +
                " where is_device_kn=1 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
            
            // вставка итогового расхода по КАН
            ret = ExecSQL(conn_db,
                " insert into t_rsh (nzp_kvar,nzp_frm,nzp_prm,rashod,rashod_g,rsh_hv,is_device,nzp_frm_typrs,kod_info)" +
                " select t.nzp_kvar,t.nzp_frm,t.nzp_prm,t.rashod,t.rashod_g,t.rsh_hv,t.is_device,390,t.kod_info" +
                " from t_rsh390 t where not (kod_info in (21,22,23,26)) " 
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            // Пост 354
            // основная услуга
            ret = ExecSQL(conn_db,
                " insert into t_rsh (nzp_kvar,nzp_frm,nzp_prm,rashod,rashod_g,rsh_hv,is_device,nzp_frm_typrs,kod_info,nzp_frm_typ)" +
                " select t.nzp_kvar,t.nzp_frm,t.nzp_prm,t.rash_kv,t.rashod_g,t.rsh_kv_hv,t.is_device,390,t.kod_info,t.nzp_frm_typ" +
                " from t_rsh390 t where (kod_info in (21,22,23,26)) "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            // услуга на ОДН
            ret = ExecSQL(conn_db,
                " insert into t_rsh (nzp_kvar,nzp_frm,nzp_prm,nzp_frm_typ,rashod,rashod_g,rsh_hv,is_device,nzp_frm_typrs,kod_info)" +
                " select t.nzp_kvar,t.nzp_frm,0 nzp_prm,f.nzp_frm_typ,max(s.rash_odn),max(s.rash_odn),max(s.rsh_odn_hv),max(s.is_device),390,max(s.kod_info)" +
                " from t_tarifs t," + gku.paramcalc.pref + "_kernel"+
#if PG
 "."
#else
":"
#endif
                +"formuls_opis f,t_rsh390 s" +
                " where t.nzp_kvar=s.nzp_kvar and t.nzp_serv=511 " +
                " and (s.kod_info in (21,22,23,26))" +
                " and t.nzp_frm=f.nzp_frm and f.nzp_frm_typrs=390" +
                " group by 1,2,3,4 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
            
            ret = ExecSQL(conn_db,
                " update t_rsh set valm=rashod " +
                " where nzp_frm_typrs=390 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            #endregion Тип расхода 390 -КАН =ХВ+ГВ

              #region тип расхода = 814 (Отопление (от ГКал-СН по отопительной площади))
            // тип расхода = 814
            ret = ExecSQL(conn_db,
                " insert into t_rsh (nzp_kvar,nzp_frm,nzp_frm_typ,nzp_prm,rashod,rashod_g,rsh1,rsh2,is_device,nzp_frm_typrs)" +
                " select t.nzp_kvar,t.nzp_frm,f.nzp_frm_typ,0 nzp_prm,max(s.squ2),max(s.squ2)," +
                " max(case when s.kod_info>4 then (case when s.cnt_stage=0 then s.kf307n else s.kf307 end) else (case when s.cnt_stage=0 then s.val3 else s.val4 end) end)," +
                " max(s.rashod),max(s.cnt_stage),814" +
                " from t_tarifs t," + gku.paramcalc.kernel_alias + "formuls_opis f," + gku.counters_xx + " s" +
                " where t.nzp_kvar=s.nzp_kvar and t.nzp_serv=s.nzp_serv and t.nzp_serv=8 " + d_dat_charge +
                " and s.nzp_type=3 and s.stek=3" +
                " and t.nzp_frm=f.nzp_frm and f.nzp_frm_typrs=814" +
                " group by 1,2,3 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            #endregion тип расхода = 814 (Отопление (от ГКал-СН по отопительной площади))

              #region тип расхода = 514 (отопление Гкалл)
            // тип расхода = 514
            ret = ExecSQL(conn_db,
                " insert into t_rsh (nzp_kvar,nzp_frm,nzp_frm_typ,nzp_prm,rashod,rashod_g,rsh1,rsh2,is_device,nzp_frm_typrs)" +
                " select t.nzp_kvar,t.nzp_frm,f.nzp_frm_typ,0 nzp_prm,max(s.rashod),max(s.rashod)," +
                " max(case when s.kod_info>3 then (case when s.cnt_stage=0 then s.kf307n else s.kf307 end) else (case when s.cnt_stage=0 then s.val3 else s.val4 end) end)," +
                " max(s.squ2),max(s.cnt_stage),514" +
                " from t_tarifs t," + gku.paramcalc.kernel_alias + "formuls_opis f," + gku.counters_xx + " s" +
                " where t.nzp_kvar=s.nzp_kvar and t.nzp_serv=s.nzp_serv and t.nzp_serv=8 " + d_dat_charge +
                " and s.nzp_type=3 and s.stek=3" +
                " and t.nzp_frm=f.nzp_frm and f.nzp_frm_typrs=514" +
                " group by 1,2,3 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
           
            //
            // индексы для установки расходов
            ret = ExecSQL(conn_db, " create index ixt_rsh1 on t_rsh(nzp_kvar,nzp_frm) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            ret = ExecSQL(conn_db, " create index ixt_rsh2 on t_rsh(nzp_frm_typrs) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            ret = ExecSQL(conn_db, sUpdStat + " t_rsh ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            #endregion тип расхода = 514 (отопление Гкалл)

              
            #region тип расхода = 814 - Завершающие действия (+округление)
            // тип расхода = 814 - Завершающие действия
            ret = ExecSQL(conn_db, 
                " update t_prm set tarif = tarif_gkal * " +
                "  (select " + sNvlWord +
                "(r.rsh1,0) from t_rsh r where t_prm.nzp_kvar=r.nzp_kvar and t_prm.nzp_frm=r.nzp_frm and r.nzp_frm_typrs=814) " +
                " where nzp_frm_typ=814 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            /*
            //977|Не округлять тариф по отоплению|||bool||2||||
            IDbCommand cmd1988 = DBManager.newDbCommand(
                " select count(*) cnt " +
                " from " + paramcalc.data_alias + "prm_5 p " +
                " where p.nzp_prm=1988 and p.is_actual<>100 "
            , conn_db);
            bool bUchetRecalcRash = false;
            try
            {
                string scntvals = Convert.ToString(cmd1988.ExecuteScalar());
                bUchetRecalcRash = (Convert.ToInt32(scntvals) > 0);
            }
            catch
            {
                bUchetRecalcRash = false;
            }
            */
            // округление до двух знаков
            ret = ExecSQL(conn_db,
                " update t_prm set tarif = round(tarif * 100) / 100 " +
                " where nzp_frm_typ=814 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            #endregion тип расхода = 814 - Завершающие действия (+округление)

              #region тип расхода = 514 - Завершающие действия
            // тип расхода = 514 - Завершающие действия
            ret = ExecSQL(conn_db,
                " update t_prm set tarif = tarif_gkal " +
                " where nzp_frm_typ=514 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
            #endregion тип расхода = 514 - Завершающие действия

              #region Исключенные действия для типа расхода 39 (заремлено)
            /*
// тип расхода = 39 - Завершающие действия
            // КПУ ХВ
            ret = ExecSQL(conn_db,
                " update t_rsh set nzp_prm=1,is_device=1,rsh_hv=" +
                " ( select max(s.rashod) from " + gku.counters_xx + " s" +
                " where t_rsh.nzp_kvar=s.nzp_kvar and s.nzp_serv=6 " + d_dat_charge + " and s.nzp_type=3 and s.stek=3 and s.cnt_stage=1 )" +
                " where nzp_frm_typrs=39" +
                " and 0<( select count(*) from " + gku.counters_xx + " s" +
                " where t_rsh.nzp_kvar=s.nzp_kvar and s.nzp_serv=6 " + d_dat_charge + " and s.nzp_type=3 and s.stek=3 and s.cnt_stage=1 )"
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); conn_db.Close(); return false; }

            ret = ExecSQL(conn_db,
                " update t_rsh set nzp_prm=1,(rsh_hv,kf307hv,kod_info)=" +
                " (( select max(s.rashod),max(s.kf307),max(s.kod_info) from " + gku.counters_xx + " s" +
                " where t_rsh.nzp_kvar=s.nzp_kvar and s.nzp_serv=6 " + d_dat_charge + " and s.nzp_type=3 and s.stek=3 and s.kf307>0 and s.cnt_stage=0))" +
                " where nzp_frm_typrs=39" +
                " and 0<( select count(*) from " + gku.counters_xx + " s" +
                " where t_rsh.nzp_kvar=s.nzp_kvar and s.nzp_serv=6 " + d_dat_charge + " and s.nzp_type=3 and s.stek=3 and s.kf307>0 and s.cnt_stage=0 )"
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); conn_db.Close(); return false; }

            // КПУ ГВ
            ret = ExecSQL(conn_db,
                " update t_rsh set nzp_prm=nzp_prm+3,is_device=1,rsh_gv=" +
                " ( select max(s.rashod) from " + gku.counters_xx + " s" +
                " where t_rsh.nzp_kvar=s.nzp_kvar and s.nzp_serv=9 " + d_dat_charge + " and s.nzp_type=3 and s.stek=3 and s.cnt_stage=1 )" +
                " where nzp_frm_typrs=39" +
                " and 0<( select count(*) from " + gku.counters_xx + " s" +
                " where t_rsh.nzp_kvar=s.nzp_kvar and s.nzp_serv=9 " + d_dat_charge + " and s.nzp_type=3 and s.stek=3 and s.cnt_stage=1 )"
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); conn_db.Close(); return false; }

            ret = ExecSQL(conn_db,
                " update t_rsh set nzp_prm=nzp_prm+3,(rsh_gv,kf307gv,kod_info)=" +
                " (( select max(s.rashod),max(s.kf307),max(s.kod_info) from " + gku.counters_xx + " s" +
                " where t_rsh.nzp_kvar=s.nzp_kvar and s.nzp_serv=9 " + d_dat_charge + " and s.nzp_type=3 and s.stek=3 and s.kf307>0 and s.cnt_stage=0))" +
                " where nzp_frm_typrs=39" +
                " and 0<( select count(*) from " + gku.counters_xx + " s" +
                " where t_rsh.nzp_kvar=s.nzp_kvar and s.nzp_serv=9 " + d_dat_charge + " and s.nzp_type=3 and s.stek=3 and s.kf307>0 and s.cnt_stage=0 )"
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); conn_db.Close(); return false; }

            // добавить нормативы ХВ или ГВ если был КПУ по одной из услуг
            ret = ExecSQL(conn_db,
                " update t_rsh set rsh_gv=" +
                " ( select max(s.rashod) from " + gku.counters_xx + " s" +
                " where t_rsh.nzp_kvar=s.nzp_kvar and s.nzp_serv=9 " + d_dat_charge + " and s.nzp_type=3 and s.stek=3)" +
                " where nzp_frm_typrs=39 and nzp_prm=1" +
                " and 0<( select count(*) from " + gku.counters_xx + " s" +
                " where t_rsh.nzp_kvar=s.nzp_kvar and s.nzp_serv=9 " + d_dat_charge + " and s.nzp_type=3 and s.stek=3)"
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); conn_db.Close(); return false; }

            ret = ExecSQL(conn_db,
                " update t_rsh set rsh_hv=" +
                " ( select max(s.rashod) from " + gku.counters_xx + " s" +
                " where t_rsh.nzp_kvar=s.nzp_kvar and s.nzp_serv=6 " + d_dat_charge + " and s.nzp_type=3 and s.stek=3)" +
                " where nzp_frm_typrs=39 and nzp_prm=3" +
                " and 0<( select count(*) from " + gku.counters_xx + " s" +
                " where t_rsh.nzp_kvar=s.nzp_kvar and s.nzp_serv=6 " + d_dat_charge + " and s.nzp_type=3 and s.stek=3)"
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); conn_db.Close(); return false; }

            // КПУ КАН
            ret = ExecSQL(conn_db,
                " update t_rsh set nzp_prm=39,rsh1=0,rsh2=0,rsh_hv=0,is_device=1, rashod=" +
                " ( select max(s.rashod) from " + gku.counters_xx + " s" +
                " where t_rsh.nzp_kvar=s.nzp_kvar and s.nzp_serv=7 " + d_dat_charge + " and s.nzp_type=3 and s.stek=3 and s.cnt_stage=1 )" +
                " where nzp_frm_typrs=39" +
                " and 0<( select count(*) from " + gku.counters_xx + " s" +
                " where t_rsh.nzp_kvar=s.nzp_kvar and s.nzp_serv=7 " + d_dat_charge + " and s.nzp_type=3 and s.stek=3 and s.cnt_stage=1 )"
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); conn_db.Close(); return false; }

            // rsh1 - часть расхода на ХВ в КАН
            // rsh2 - часть расхода на ГВ в КАН
            ret = ExecSQL(conn_db, " update t_rsh set rsh1=0                              where nzp_frm_typrs=39 and rsh1 is null ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); conn_db.Close(); return false; }

            ret = ExecSQL(conn_db, " update t_rsh set rsh2=0                              where nzp_frm_typrs=39 and rsh2 is null ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); conn_db.Close(); return false; }

            ret = ExecSQL(conn_db, " update t_rsh set rsh_hv=0                            where nzp_frm_typrs=39 and rsh_hv is null ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); conn_db.Close(); return false; }

            ret = ExecSQL(conn_db, " update t_rsh set rsh_gv=0                            where nzp_frm_typrs=39 and rsh_gv is null ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); conn_db.Close(); return false; }

            ret = ExecSQL(conn_db, " update t_rsh set rsh1=rsh_hv,rsh2=rsh_gv             where nzp_frm_typrs=39 and nzp_prm in (0,1,3,4) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); conn_db.Close(); return false; }

            ret = ExecSQL(conn_db, " update t_rsh set rashod=rsh1+rsh2                    where nzp_frm_typrs=39 and nzp_prm in (0,1,3,4) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); conn_db.Close(); return false; }
*/
            #endregion Исключенные действия для типа расхода 39

          

            //
//          Завершающие действия по выборке расходов
//

            #region Установить расход по норме

            ret = ExecSQL(conn_db, sUpdStat + " t_rsh ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
            //

            // установить расход по норме для коммунальных услуг
            ret = ExecSQL(conn_db,
                " update t_rsh set set_norm=1,rashod_norm=" +
                " (select max(case when s.val1>0 then s.val1 else 0 end)" +
                 " from t_tarifs t," + gku.counters_xx + " s" +
                 " where t.nzp_kvar=s.nzp_kvar and (case when t.nzp_serv=14 then 9 else t.nzp_serv end)=s.nzp_serv " + d_dat_charge +
                 " and t_rsh.nzp_kvar=t.nzp_kvar and t_rsh.nzp_frm=t.nzp_frm and s.nzp_type=3 and s.stek=30)" +
                " where 0<" +
                " (select count(*)" +
                 " from t_tarifs t," + gku.counters_xx + " s" +
                 " where t.nzp_kvar=s.nzp_kvar and (case when t.nzp_serv=14 then 9 else t.nzp_serv end)=s.nzp_serv " + d_dat_charge +
                 " and t_rsh.nzp_kvar=t.nzp_kvar and t_rsh.nzp_frm=t.nzp_frm and s.nzp_type=3 and s.stek=30)"
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            // установить расход по норме для услуг по жильцам
            ret = ExecSQL(conn_db, " update t_rsh set set_norm=1,rashod_norm=rashod where set_norm=0 and nzp_frm_typrs=3 ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            // установить расход по норме для услуг по квартире (все неустановленные формулы кроме площадных)
            ret = ExecSQL(conn_db,
                " update t_rsh set set_norm=1,rashod_norm=1 " +
                " where set_norm=0 and 0<(select count(*) from " + gku.paramcalc.kernel_alias + "formuls f where t_rsh.nzp_frm=f.nzp_frm and f.nzp_measure<>1)"
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
                " ikg  integer default 0," +
                " iikg integer default 0" +
#if PG
                " ) "
#else
                " ) with no log "
#endif
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            ret = ExecSQL(conn_db,
                " Insert into t_kg_m2 (nzp_kvar,kg)" +
                " Select nzp_kvar,(kg+kvp) " +
                " From t_rsh_kolgil where 1=1 " 
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            ret = ExecSQL(conn_db, " create index ixt_kg_m2 on t_kg_m2 (nzp_kvar) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
            ret = ExecSQL(conn_db, sUpdStat + " t_kg_m2 ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            ret = ExecSQL(conn_db,
                " update t_kg_m2 set ikg= (case when Round(kg)-Trunc(kg)>0.5 then Round(kg) else Trunc(kg)+1 end) where 1=1 " 
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
            ret = ExecSQL(conn_db,
                " update t_kg_m2 set iikg= (case when ikg>5 then 5 else ikg end) where 1=1 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            #endregion Определить количество жильцов -округленно для выборки из таблицы нормативов

            ret = ExecSQL(conn_db,
                " update t_rsh set set_norm=1,rashod_norm=" +
                 " (select max((r.value" + sConvToNum + ")*g.ikg) from " + paramcalc.kernel_alias + "res_values r,t_kg_m2 g" +
                 " where g.nzp_kvar=t_rsh.nzp_kvar and r.nzp_x=2 " +
                 "  and r.nzp_res = (select max(p.val_prm)" + sConvToInt +
                                     " from " + paramcalc.data_alias + "prm_13 p where p.nzp_prm=171 and p.is_actual<>100" +
                                     " and p.dat_s<" + gku.dat_po + " and p.dat_po>=" + gku.dat_s + ") " +
                 " and r.nzp_y=g.iikg)" +
                " where set_norm=0 and 0<" +
                 " (select count(*) from " + paramcalc.kernel_alias + "res_values r,t_kg_m2 g" +
                 " where g.nzp_kvar=t_rsh.nzp_kvar and r.nzp_x=2 " +
                 "  and r.nzp_res = (select max(p.val_prm)" + sConvToInt +
                                     " from " + paramcalc.data_alias + "prm_13 p where p.nzp_prm=171 and p.is_actual<>100" +
                                     " and p.dat_s<" + gku.dat_po + " and p.dat_po>=" + gku.dat_s + ") " +
                 " and r.nzp_y=g.iikg)"
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            ret = ExecSQL(conn_db,
                " update t_rsh set set_norm=1,rashod_norm=" +
                 " (select max((r.value" + sConvToNum + ")*g.ikg) from " + paramcalc.kernel_alias + "res_values r,t_kg_m2 g" +
                 " where g.nzp_kvar=t_rsh.nzp_kvar and r.nzp_x=2 " +
                 "  and r.nzp_res = (select max(p.val_prm)" + sConvToInt +
                                     " from " + paramcalc.data_alias + "prm_13 p where p.nzp_prm=171 and p.is_actual<>100" +
                                     " and p.dat_s<" + gku.dat_po + " and p.dat_po>=" + gku.dat_s + ") " +
                 " and r.nzp_y=g.iikg)" +
                " where nzp_frm_typrs=814 and 0<" +
                 " (select count(*) from " + paramcalc.kernel_alias + "res_values r,t_kg_m2 g where g.nzp_kvar=t_rsh.nzp_kvar and r.nzp_x=2 " +
                 "  and r.nzp_res = (select max(p.val_prm)" + sConvToInt +
                                     " from " + paramcalc.data_alias + "prm_13 p where p.nzp_prm=171 and p.is_actual<>100" +
                                     " and p.dat_s<" + gku.dat_po + " and p.dat_po>=" + gku.dat_s + ") " +
                 " and r.nzp_y=g.iikg)"
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            ret = ExecSQL(conn_db, " update t_rsh set rashod_norm=0 where rashod_norm is null ", true);
            #endregion Если расход еще не определился взять -Нормативы по площади

            #endregion Создать сводную таблицу расходов t_rsh (плоский вид)

            #region Почистить временные таблицы
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
            #endregion Почистить временные таблицы

//
//          Запись результатов расчета в БД
//

            #region Выборка тарифов по приоритетам
            ret = ExecSQL(conn_db,
                " create temp table t_prm_calc (" +
                //" create table are.t_prm_calc (" +
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
                " nzp_frm_typ integer default 0 not null" +
#if PG
                " )  "
#else
                //" ) "
                " ) with no log "
#endif
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
            // выборка тарифов по выбранным приоритетам
            ret = ExecSQL(conn_db,
                " insert into t_prm_calc" +
                " (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,gil,gil_g,squ,tarif,tarif_gkal,norm_gkal,koef_gkal," +
                " is_poloten,is_neiztrp,tarif_m3,nzp_frm_typ)" +
                " select p.nzp_dom,p.nzp_kvar,p.nzp_supp,p.nzp_frm,p.nzp_prm,p.gil,p.gil_g,p.squ,p.tarif,p.tarif_gkal,p.norm_gkal,p.koef_gkal," +
                " p.is_poloten,p.is_neiztrp,tarif_m3,p.nzp_frm_typ" +
                " from t_prm p,t_prm_max m" +
                " Where p.nzp_kvar=m.nzp_kvar and p.nzp_supp=m.nzp_supp and p.nzp_frm=m.nzp_frm and p.priority=m.priority "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            ret = ExecSQL(conn_db, " create index ixt1_prm_calc on t_prm_calc(nzp_kvar,nzp_supp,nzp_frm) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            ret = ExecSQL(conn_db, " create index ixt2_prm_calc on t_prm_calc(nzp_kvar,nzp_frm,nzp_frm_typ) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
            ret = ExecSQL(conn_db, sUpdStat + " t_prm_calc ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
            #endregion Выборка тарифов по приоритетам

            #region Добавление услуг ОДН 
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
                " nzp_frm_typ integer default 0 not null" +
#if PG
                " ) "
#else
                // " ) "
                " ) with no log "
#endif
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            // услуги на ОДН с тарифом основной услуги
#if PG
            ret = ExecSQL(conn_db,
                            " insert into ttt_aid_virt0 (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,gil,gil_g,squ,tarif,tarif_gkal,norm_gkal,koef_gkal,is_poloten,is_neiztrp,tarif_m3,nzp_frm_typ)" +
                            " select p.nzp_dom,p.nzp_kvar,p.nzp_supp,t.nzp_frm,p.nzp_prm,p.gil,p.gil_g,p.squ,p.tarif,p.tarif_gkal,p.norm_gkal,p.koef_gkal,p.is_poloten,p.is_neiztrp,p.tarif_m3,p.nzp_frm_typ" +
                            " from t_prm_calc p,t_tarifs r,t_tarifs t," + gku.paramcalc.pref + "_kernel.serv_odn n" +
                            " Where r.nzp_kvar=p.nzp_kvar and r.nzp_frm=p.nzp_frm     and r.nzp_serv=n.nzp_serv_link" +
                            "   and t.nzp_kvar=p.nzp_kvar and t.nzp_frm=n.nzp_frm_eqv and t.nzp_serv=n.nzp_serv and p.nzp_frm_typ<>1140 " +
                            " group by 1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16 "
                            , true);
#else
ret = ExecSQL(conn_db,
                " insert into ttt_aid_virt0 (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,gil,gil_g,squ,tarif,tarif_gkal,norm_gkal,koef_gkal,is_poloten,is_neiztrp,tarif_m3,nzp_frm_typ)" +
                " select p.nzp_dom,p.nzp_kvar,p.nzp_supp,t.nzp_frm,p.nzp_prm,p.gil,p.gil_g,p.squ,p.tarif,p.tarif_gkal,p.norm_gkal,p.koef_gkal,p.is_poloten,p.is_neiztrp,p.tarif_m3,p.nzp_frm_typ" +
                " from t_prm_calc p,t_tarifs r,t_tarifs t," + gku.paramcalc.pref + "_kernel:serv_odn n" +
                " Where r.nzp_kvar=p.nzp_kvar and r.nzp_frm=p.nzp_frm     and r.nzp_serv=n.nzp_serv_link" +
                "   and t.nzp_kvar=p.nzp_kvar and t.nzp_frm=n.nzp_frm_eqv and t.nzp_serv=n.nzp_serv and p.nzp_frm_typ<>1140 " +
                " group by 1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16 "
                , true);
#endif
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

#if PG
            ret = ExecSQL(conn_db,
                " insert into ttt_aid_virt0 (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,gil,gil_g,squ,tarif,tarif_gkal,norm_gkal,koef_gkal,is_poloten,is_neiztrp,tarif_m3,nzp_frm_typ)" +
                " select p.nzp_dom,p.nzp_kvar,p.nzp_supp,t.nzp_frm,p.nzp_prm,p.gil,p.gil_g,p.squ,p.tarif,p.tarif_gkal,p.norm_gkal,p.koef_gkal,p.is_poloten,p.is_neiztrp,p.tarif_m3,p.nzp_frm_typ" +
                " from t_prm_calc p,t_tarifs r,t_tarifs t," + gku.paramcalc.pref + "_kernel.serv_odn n" +
                " Where r.nzp_kvar=p.nzp_kvar and r.nzp_frm=p.nzp_frm     and r.nzp_serv=n.nzp_serv_link" +
                "   and t.nzp_kvar=p.nzp_kvar and t.nzp_serv=n.nzp_serv and p.nzp_frm_typ=1140 " +
                " and t.nzp_frm not in (select nzp_frm from t_rsh where nzp_frm_typ=999) " +
                " group by 1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16 "
                , true);
#else
            ret = ExecSQL(conn_db,
                " insert into ttt_aid_virt0 (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,gil,gil_g,squ,tarif,tarif_gkal,norm_gkal,koef_gkal,is_poloten,is_neiztrp,tarif_m3,nzp_frm_typ)" +
                " select p.nzp_dom,p.nzp_kvar,p.nzp_supp,t.nzp_frm,p.nzp_prm,p.gil,p.gil_g,p.squ,p.tarif,p.tarif_gkal,p.norm_gkal,p.koef_gkal,p.is_poloten,p.is_neiztrp,p.tarif_m3,p.nzp_frm_typ" +
                " from t_prm_calc p,t_tarifs r,t_tarifs t," + gku.paramcalc.pref + "_kernel:serv_odn n" +
                " Where r.nzp_kvar=p.nzp_kvar and r.nzp_frm=p.nzp_frm     and r.nzp_serv=n.nzp_serv_link" +
                "   and t.nzp_kvar=p.nzp_kvar and t.nzp_serv=n.nzp_serv and p.nzp_frm_typ=1140 "+
                " and t.nzp_frm not in (select nzp_frm from t_rsh where nzp_frm_typ=999) " +
                " group by 1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16 "
                , true);
#endif
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

#if PG
            ExecSQL(conn_db, " analyze ttt_aid_virt0 ", true);
#else
            ExecSQL(conn_db, " Update statistics for table ttt_aid_virt0 ", true);
#endif
#if PG
            ret = ExecSQL(conn_db,
                            " insert into t_prm_calc (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,gil,gil_g,squ,tarif,tarif_gkal,norm_gkal,koef_gkal,is_poloten,is_neiztrp,tarif_m3,nzp_frm_typ)" +
                            " select p.nzp_dom,p.nzp_kvar,p.nzp_supp,p.nzp_frm,p.nzp_prm,p.gil,p.gil_g,p.squ,p.tarif,p.tarif_gkal,p.norm_gkal,p.koef_gkal,p.is_poloten,p.is_neiztrp,p.tarif_m3,p.nzp_frm_typ" +
                            " from ttt_aid_virt0 p"
                            , true);
#else
ret = ExecSQL(conn_db,
                " insert into t_prm_calc (nzp_dom,nzp_kvar,nzp_supp,nzp_frm,nzp_prm,gil,gil_g,squ,tarif,tarif_gkal,norm_gkal,koef_gkal,is_poloten,is_neiztrp,tarif_m3,nzp_frm_typ)" +
                " select p.nzp_dom,p.nzp_kvar,p.nzp_supp,p.nzp_frm,p.nzp_prm,p.gil,p.gil_g,p.squ,p.tarif,p.tarif_gkal,p.norm_gkal,p.koef_gkal,p.is_poloten,p.is_neiztrp,p.tarif_m3,p.nzp_frm_typ" +
                " from ttt_aid_virt0 p"
                , true);
#endif
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
#if PG
            ret = ExecSQL(conn_db, " analyze t_prm_calc ", true);
#else
            ret = ExecSQL(conn_db, " update statistics for table t_prm_calc ", true);
#endif
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            ExecSQL(conn_db, " Drop table ttt_aid_virt0 ", false);
            #endregion Добавление услуг ОДН

            #region Завершающие действия по формуле 440
            // тип расхода = 440 - Завершающие действия
            // записать коэф-т перевода в ГКал
            ret = ExecSQL(conn_db,
                " update t_rsh set rsh1 = rashod, rsh2 = " +
                        " (select max(koef_gkal*norm_gkal) from t_prm_calc r" +
                        " where t_rsh.nzp_kvar=r.nzp_kvar and t_rsh.nzp_frm=r.nzp_frm and r.nzp_frm_typ=440) " +
                " where nzp_frm_typrs=440 " +
                  " and 0<(select count(*) from t_prm_calc r" +
                        " where t_rsh.nzp_kvar=r.nzp_kvar and t_rsh.nzp_frm=r.nzp_frm and r.nzp_frm_typ=440) "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            // расчитать окончательный расход в Гкал ГВС = расход в м3 * расход в ГКал на нагрев 1 м3
            ret = ExecSQL(conn_db,
                " update t_rsh set rashod = rsh1 * rsh2 where nzp_frm_typrs=440 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            #endregion Завершающие действия по формуле 440

            #region Завершающие действия по формуле 1140
            
               #region Выставить признак округления пока только для формулы 1140&1814
       //     DataTable rta=  ViewTbl(conn_db, " select * from t_rsh ");
            // параметр округления для УК 
            ret = ExecSQL(conn_db,
                " update t_rsh set is_trunc =" +
                " (select max(" + sNvlWord + "(p.val_prm,'0')" + sConvToNum + ") " +
                " from " + paramcalc.data_alias + "prm_7 p" +
                " where p.nzp_prm=1233 and p.is_actual<>100 and p.dat_s<" + gku.dat_po + " and p.dat_po>=" + gku.dat_s +
                " ) where t_rsh.nzp_frm_typ in (1140,1814,1983) "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            // параметр округления на базу 

            ret = ExecSQL(conn_db,
                " update t_rsh set is_trunc =" +
                " (select max(" + sNvlWord + "(p.val_prm,'0')" + sConvToNum + ") " +
                " from " + paramcalc.data_alias + "prm_10 p" +
                " where p.nzp_prm=1232 and p.is_actual<>100 and p.dat_s<" + gku.dat_po + " and p.dat_po>=" + gku.dat_s +
                " ) where t_rsh.nzp_frm_typ in (1140,1814,1983) " +
                // Учесть что уже установлен параметр округления на базу данных
                " and 0=(select count(*) from " + paramcalc.data_alias + "prm_7 p1" +
                "     where p1.nzp_prm=1233 and p1.is_actual<>100 and p1.dat_s<" + gku.dat_po + " and p1.dat_po>=" + gku.dat_s + ") "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
            #endregion Выставить признак округления

               #region параметр норма гКалл на 1 м3
            // параметр норма гКалл на 1 м3
            ret = ExecSQL(conn_db,
             " update t_rsh set rsh_gkal =(select max(a.norm_gkal) from t_prm_calc a where a.nzp_kvar=t_rsh.nzp_kvar and a.nzp_frm_typ=1140) where nzp_frm_typ=1140  "
             , true);
            
            ret = ExecSQL(conn_db,
             " update t_rsh set rsh_gkal =(select max(a.norm_gkal) from t_prm_calc a where a.nzp_kvar=t_rsh.nzp_kvar and a.nzp_frm_typ=1140) where nzp_frm_typ=1983  " 
             , true);
            
            #endregion параметр норма гКалл на 1 м3
       //     rta = ViewTbl(conn_db, " select * from t_rsh ");
               #region записать коэф-т перевода m3 ГВС в ГКал
            // записать коэф-т перевода в ГКал
            ret = ExecSQL(conn_db,
                " update t_rsh set  rsh1 = rashod, rsh2 = " +
                        " (select max(koef_gkal*norm_gkal) from t_prm_calc r" +
                        " where t_rsh.nzp_kvar=r.nzp_kvar and t_rsh.nzp_frm=r.nzp_frm and r.nzp_frm_typ=1140) " +
                " where nzp_frm_typrs=5 and nzp_frm_typ=1140 " +
                  " and 0<(select count(*) from t_prm_calc r" +
                        " where t_rsh.nzp_kvar=r.nzp_kvar and t_rsh.nzp_frm=r.nzp_frm and r.nzp_frm_typ=1140) "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            ret = ExecSQL(conn_db,
                " update t_rsh set rsh1=rashod, rsh2=rsh_gkal where nzp_frm_typrs=5 and nzp_frm_typ=1983  "
                , true);

            #endregion записать коэф-т перевода m3 ГВС в ГКал
      //      rta = ViewTbl(conn_db, " select * from t_rsh ");
               #region расчитать окончательный расход в Гкал ГВС = расход в м3 * расход в ГКал на нагрев 1 м3

            // сохранить расходы в куб.м для ГВС - nzp_frm_typ in (1140,1983)
            ret = ExecSQL(conn_db,
                    " update t_rsh set valmk=valm, dop87k=dop87, dlt_revalk=dlt_reval, rash_norm_onek=rash_norm_one" +
                    " where nzp_frm_typrs=5 and nzp_frm_typ in (1140,1983) "
                    , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            // сначала для услуги ОДН - nzp_frm_typ=1983
            ret = ExecSQL(conn_db,
                    " update t_rsh set rashod = rsh1 * rsh2,rashod_norm=rashod_norm * rsh2" +
                    " , valm=valm * rsh2, dop87=dop87 * rsh2, dlt_reval=dlt_reval * rsh2" +
                    " , rash_norm_one=rash_norm_one * rsh2" +
                    " where nzp_frm_typrs=5 and nzp_frm_typ=1983 "
                    , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
       //     rta = ViewTbl(conn_db, " select * from t_rsh ");
            ret = ExecSQL(conn_db,
                " update t_rsh set rashod = round(rashod,is_trunc),rashod_norm=round(rashod_norm,is_trunc)" +
                " , valm=round(valm,is_trunc), dop87=round(dop87,is_trunc), dlt_reval=round(dlt_reval,is_trunc)" +
                " , rash_norm_one=round(rash_norm_one,is_trunc)" +
                " where nzp_frm_typrs=5 and nzp_frm_typ=1983 and is_trunc between 1 and 6 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
     //       rta = ViewTbl(conn_db, " select * from t_rsh ");
            // для основной услуги услуги ОДН - nzp_frm_typ=1140
            if (Points.IsSmr)
            {
                // для Самары округлить до 2х - если работаем по нормативу - решение от 28.08.2013 -> gil_smr>0! это кол-во жильцов!
                ret = ExecSQL(conn_db,
                    " update t_rsh set gil_smr = case when rash_norm_one>0 and is_device=0 then valm/rash_norm_one else 0 end " +
                    " where nzp_frm_typrs=5 and nzp_frm_typ=1140 and rsh2>0 "
                    , true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
            }
            ret = ExecSQL(conn_db,
                    " update t_rsh set rashod = rsh1 * rsh2,rashod_norm=rashod_norm * rsh2" +
                    " , valm=valm * rsh2, dop87=dop87 * rsh2, dlt_reval=dlt_reval * rsh2" +
                    " , rash_norm_one=rash_norm_one * rsh2" +
                    " where nzp_frm_typrs=5 and nzp_frm_typ=1140 and abs(gil_smr)<=0.000001 "
                    , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            string sss = " select val_prm" + sConvToInt + " cnt " +
                         " from " + gku.paramcalc.data_alias + "prm_10 p " +
                         " where p.nzp_prm=1279  " +
                         " and p.is_actual<>100 and p.dat_s<" + gku.dat_po + " and p.dat_po>=" + gku.dat_s + " ";
            ret = ExecRead(conn_db, out reader,sss, true);

            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*/conn_db.Close();// */     return false; }

            int isGvsTrunc = 2;
            if (reader.Read())
            {
                isGvsTrunc = (Convert.ToInt32(reader["cnt"]));
            }
            reader.Close();

       //     rta = ViewTbl(conn_db, " select * from t_rsh ");
            ret = ExecSQL(conn_db,
                    " update t_rsh set rash_norm_one=round(rash_norm_one * rsh2," + isGvsTrunc + ")" +
                    " , rashod      =" +
                       " case when dop87>=0" +
                            " then round(rash_norm_one * rsh2," + isGvsTrunc + ") * gil_smr" +
                            " else (case when round(rash_norm_one * rsh2," + isGvsTrunc + ") * gil_smr + dop87 * rsh2>0" +
                                       " then round(rash_norm_one * rsh2," + isGvsTrunc + ") * gil_smr + dop87 * rsh2 else 0 end) " +
                       " end " +
                    " , rashod_norm = round(rash_norm_one * rsh2," + isGvsTrunc + ") * gil_smr" +
                    " , valm        = round(rash_norm_one * rsh2," + isGvsTrunc + ") * gil_smr" +
                    " , dop87=dop87 * rsh2, dlt_reval=dlt_reval * rsh2 " +
                    " where nzp_frm_typrs=5 and nzp_frm_typ=1140 and abs(gil_smr)>0.000001 "
                    , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

        //    rta = ViewTbl(conn_db, " select * from t_rsh ");
            ret = ExecSQL(conn_db,
                " update t_rsh set rashod = round(rashod,is_trunc),rashod_norm=round(rashod_norm,is_trunc)" +
                " , valm=round(valm,is_trunc), dop87=round(dop87,is_trunc), dlt_reval=round(dlt_reval,is_trunc)" +
                " , rash_norm_one=round(rash_norm_one,is_trunc)" +
                " where nzp_frm_typrs=5 and nzp_frm_typ=1140 and is_trunc between 1 and 6 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

               #endregion расчитать окончательный расход в Гкал ГВС = расход в м3 * расход в ГКал на нагрев 1 м3

       //     rta = ViewTbl(conn_db, " select * from t_rsh ");
            #endregion Завершающие действия по формуле 1140

            #region Учет неотапливаемой площади для отопления

#if PG
            ret = ExecSQL(conn_db,
                            " create temp table t_rsh8 (" +
                            " nzp_kvar integer," +
                            " nzp_frm  integer," +
                            " rsh3     numeric(16,7) default 0" +
                            " )  "
                            , true);
#else
ret = ExecSQL(conn_db,
                " create temp table t_rsh8 (" +
                " nzp_kvar integer," +
                " nzp_frm  integer," +
                " rsh3     decimal(16,7) default 0" +
                " ) with no log "
                , true);
#endif
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
            // выбрать неотапливаемую площадь для отопления - nzp_serv=8 and nzp_prm=2010
#if PG
            ret = ExecSQL(conn_db,
                            " insert into t_rsh8 (nzp_kvar,nzp_frm,rsh3) " +
                            " select t.nzp_kvar,t.nzp_frm,max(p.val::numeric) " +
                            " from t_tarifs t," + gku.paramcalc.pref + "_kernel.formuls f,t_par1 p " +
                            " where t.nzp_kvar=p.nzp and t.nzp_frm=f.nzp_frm and t.nzp_serv=8 and p.nzp_prm=2010 and f.nzp_measure in (1,4) " +
                            " group by 1,2 "
                            , true);
#else
ret = ExecSQL(conn_db,
                " insert into t_rsh8 (nzp_kvar,nzp_frm,rsh3) " +
                " select t.nzp_kvar,t.nzp_frm,max(p.val+0) " +
                " from t_tarifs t," + gku.paramcalc.pref + "_kernel:formuls f,t_par1 p " +
                " where t.nzp_kvar=p.nzp and t.nzp_frm=f.nzp_frm and t.nzp_serv=8 and p.nzp_prm=2010 and f.nzp_measure in (1,4) " +
                " group by 1,2 "
                , true);
#endif
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            ret = ExecSQL(conn_db, " create index ixt_rsh8 on t_rsh8(nzp_kvar,nzp_frm) ", true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
#if PG
            ret = ExecSQL(conn_db, " analyze t_rsh8 ", true);
#else
            ret = ExecSQL(conn_db, " update statistics for table t_rsh8 ", true);
#endif
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            ret = ExecSQL(conn_db,
                " update t_rsh set rsh1 = rashod, rsh3 = (select r.rsh3 from t_rsh8 r where r.nzp_kvar=t_rsh.nzp_kvar and r.nzp_frm=t_rsh.nzp_frm)" +
                " where 0<(select count(*) from t_rsh8 r where r.nzp_kvar=t_rsh.nzp_kvar and r.nzp_frm=t_rsh.nzp_frm) "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            ret = ExecSQL(conn_db,
                " update t_rsh set rashod = case when rsh1-rsh3>0 then rsh1-rsh3 else 0 end " +
                " where 0<(select count(*) from t_rsh8 r where r.nzp_kvar=t_rsh.nzp_kvar and r.nzp_frm=t_rsh.nzp_frm) "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            ExecSQL(conn_db, " Drop table t_rsh8 ", false);

            #endregion Учет неотапливаемой площади для отопления

            #region Завершающие действия по формуле 1814
            // тип расхода = 1814 - Завершающие действия обнаружил что есть понятие неотапливаемой площади в квартире , это  что то новенькое , поэтому перенес 
            // завершающие действия ниже
              #region Признак округления для формулы 1814 выставлен ранее в 1140
            // сделано в завершающих действиях по типу 1440   (признаки округления одинаковы )         
            #endregion  Признак округления для формулы 1814 выставлен в 1140

              #region параметр норма Отопления гКалл на 1 м2 для каждого дома
            // параметр норма гКалл на 1 м3 для 

            ret = ExecSQL(conn_db,
                    " update t_rsh set rsh_gkal =" +
                    " (select max(" + sNvlWord + "(p.val_prm,'0')" + sConvToNum + ") " +
                      " from ttt_prm_2 p" +
                      " where p.nzp_prm=723 " +
                      " and p.nzp in (select nzp_dom from t_prm pp where pp.nzp_kvar=t_rsh.nzp_kvar) " +                    
                      " and p.dat_s<" + gku.dat_po + " and p.dat_po>=" + gku.dat_s + " )" +
                    " where t_rsh.nzp_frm_typ=1814 and  t_rsh.nzp_prm in (4) "
                    , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

            // в Самаре для коммуналок жилая площадь
            if (Points.IsSmr)
            {
                ret = ExecSQL(conn_db,
                    " update t_rsh set rsh_gkal =" +
                    " (select max(" + sNvlWord + "(p.val_prm,'0')" + sConvToNum + ") " +
                      " from ttt_prm_2 p" +
                      " where p.nzp_prm=2074 " +
                      " and p.nzp in (select nzp_dom from t_prm pp where pp.nzp_kvar=t_rsh.nzp_kvar) " +                    
                      " and 0<(select count(*) from t_kmnl r where t_rsh.nzp_kvar=r.nzp_kvar) " +
                      "  and p.dat_s<" + gku.dat_po + " and p.dat_po>=" + gku.dat_s + " )" +
                    " where t_rsh.nzp_frm_typ=1814  and  t_rsh.nzp_prm in (6)   "
                    // добавить условие на площадь жилую и общую
                    , true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
            }
            else
            {
                ret = ExecSQL(conn_db,
                    " update t_rsh set rsh_gkal =" +
                    " (select max(" + sNvlWord + "(p.val_prm,'0')" + sConvToNum + ") " +
                      " from ttt_prm_2 p" +
                      " where  p.nzp_prm=723 " +
                      " and p.nzp in (select nzp_dom from t_prm pp where pp.nzp_kvar=t_rsh.nzp_kvar) " +  
                      "  and p.dat_s<" + gku.dat_po + " and p.dat_po>=" + gku.dat_s + " )" +
                    " where t_rsh.nzp_frm_typ=1814 and t_rsh.nzp_prm in (133)  "
                    // добавить условие на площадь жилую и общую
                    , true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
            }

            #endregion параметр норма гКалл на 1 м3  для каждого дома

              #region перевод отопление из м2 в ГКал
            ret = ExecSQL(conn_db,
                " update t_rsh set  rsh1 = rashod, rsh2 = rsh_gkal" +
                " where nzp_frm_typrs=11 and nzp_frm_typ=1814 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // расчитать окончательный расход в Гкал отопление = расход в м2 * расход в ГКал на отопл 1 м2
            ret = ExecSQL(conn_db,
                " update t_rsh set rashod = rsh1 * rsh2 where nzp_frm_typrs=11 and nzp_frm_typ=1814  "
                , true);
            #endregion перевод отопление из м2 в ГКал

              #region округлить если надо
            // Нужно округлить до если есть признак округления Вот и все что здесь нового 
            ret = ExecSQL(conn_db,
                " update t_rsh set rashod = round(rashod,is_trunc) where nzp_frm_typrs=11 and nzp_frm_typ=1814 and is_trunc between 1 and 6 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }
            
            ret = ExecSQL(conn_db,
                " update t_rsh set valm = rashod where nzp_frm_typrs=11 and nzp_frm_typ=1814 and is_trunc between 1 and 6 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

              #endregion округлить если надо

            #endregion Завершающие действия по формуле 1814

            #region Специфика расчета дотаций
            // ... для расчета дотаций рассчитывается ПТ (ЭОТ - тариф по услуге в t_perc_pt) ...
            if (bIsCalcSubsidy)
            {
                // ... установка ПТ тарифов ...

#if PG
                ret = ExecSQL(conn_db,
                    //" create table are.t_perc_pt (" +
                    " create temp table t_perc_pt (" +
                    " nzp_dom  integer," +
                    " nzp_kvar integer," +
                    " nzp_serv integer," +
                    " nzp_supp integer," +
                    " nzp_vill NUMERIC(13,0) default 0," +
                    " nzp_frm  integer," +
                    " etag     integer," +
                    " perc     numeric(14,8) default 1," +
                    " tarif    numeric(17,7) default 0," + // eot
                    " norm_gkal  numeric(14,8) default 0," +
                    " koef_gkal  numeric(14,8) default 1," +
                    " tarif_m3   numeric(17,7) default 0," +
                    " nzp_frm_typ integer default 0," +
                    " tarif_f  numeric(17,7) default 0," +
                    " nzp_prm_pt  integer default 0" +
                    " ) "
                    //" ) "
                    , true);
#else
                    ret = ExecSQL(conn_db,
                    //" create table are.t_perc_pt (" +
                    " create temp table t_perc_pt (" +
                    " nzp_dom  integer," +
                    " nzp_kvar integer," +
                    " nzp_serv integer," +
                    " nzp_supp integer," +
                    " nzp_vill DECIMAL(13,0) default 0," +
                    " nzp_frm  integer," +
                    " etag     integer," +
                    " perc     decimal(14,8) default 1," +
                    " tarif    decimal(17,7) default 0," + // eot
                    " norm_gkal  decimal(14,8) default 0," +
                    " koef_gkal  decimal(14,8) default 1," +
                    " tarif_m3   decimal(17,7) default 0," +
                    " nzp_frm_typ integer default 0," +
                    " tarif_f  decimal(17,7) default 0," +
                    " nzp_prm_pt  integer default 0" +
                    " ) with no log "
                    //" ) "
                    , true);
#endif
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
#if PG
                ret = ExecSQL(conn_db, " analyze t_perc_pt ", true);
#else
                ret = ExecSQL(conn_db, " update statistics for table t_perc_pt ", true);
#endif
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
#if PG
                ret = ExecSQL(conn_db, " analyze t_perc_pt ", true);
#else
                ret = ExecSQL(conn_db, " update statistics for table t_perc_pt ", true);
#endif
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
                       " from " + paramcalc.data_alias + "rajon_vill v," + paramcalc.data_alias + "s_ulica u," + paramcalc.data_alias + "dom d " +
                       " where d.nzp_dom=t_perc_pt.nzp_dom and d.nzp_ul=u.nzp_ul and u.nzp_raj=v.nzp_raj)" +
                    " where 0<(select count(*) " +
                       " from " + paramcalc.data_alias + "rajon_vill v," + paramcalc.data_alias + "s_ulica u," + paramcalc.data_alias + "dom d " +
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
                    " update t_prm_calc set "+
#if PG
                      " nzp_prm_pt=(select max(p.nzp_prm_pt) from t_perc_pt p where t_prm_calc.nzp_kvar=p.nzp_kvar and t_prm_calc.nzp_supp=p.nzp_supp and t_prm_calc.nzp_frm=p.nzp_frm ),"+
                      " tarif_f=(select max(p.tarif_f) from t_perc_pt p where t_prm_calc.nzp_kvar=p.nzp_kvar and t_prm_calc.nzp_supp=p.nzp_supp and t_prm_calc.nzp_frm=p.nzp_frm ),"+
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
                    count = ExecScalar(conn_db,
                        " Select max(val_prm)" + sConvToNum + " From " + paramcalc.data_alias + "prm_5 Where nzp_prm=1196 and is_actual<>100 ",
                        out ret, true);
                    if (ret.result)
                    {
                        try { r1196 = Convert.ToDecimal(count) / 100; } catch { r1196 = 0; }
                    }
                    decimal r1197 = 0; // повышаюший коэффициент на просушку - Саха
                    count = ExecScalar(conn_db,
                        " Select max(val_prm)" + sConvToNum + " From " + paramcalc.data_alias + "prm_5 Where nzp_prm=1197 and is_actual<>100 ",
                        out ret, true);
                    if (ret.result)
                    {
                        try { r1197 = Convert.ToDecimal(count) / 100; } catch { r1197 = 0; }
                    }
                    decimal r1198 = 0; // повышаюший коэффициент на вентиляцию - Саха
                    count = ExecScalar(conn_db,
                        " Select max(val_prm)" + sConvToNum + " From " + paramcalc.data_alias + "prm_5 Where nzp_prm=1198 and is_actual<>100 ",
                        out ret, true);
                    if (ret.result)
                    {
                        try { r1198 = Convert.ToDecimal(count) / 100; } catch { r1198 = 0; }
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
            IDbCommand cmd1988 = DBManager.newDbCommand(
                " select count(*) cnt " +
                " from " + paramcalc.data_alias + "prm_5 p " +
                " where p.nzp_prm=1988 and p.is_actual<>100 "
            , conn_db);
            bool bUchetRecalcRash = false;
            try
            {
                string scntvals = Convert.ToString(cmd1988.ExecuteScalar());
                bUchetRecalcRash = (Convert.ToInt32(scntvals) > 0);
            }
            catch
            {
                bUchetRecalcRash = false;
            }
            if (bUchetRecalcRash &&
                (gku.paramcalc.cur_yy == gku.paramcalc.calc_yy) && (gku.paramcalc.cur_mm == gku.paramcalc.calc_mm))
            {

                //ViewTbl(conn_db, " select * from t_rsh where nzp_kvar=551956 ");

                // если ОДН меньше 0, то не применять его если отрицательный расход по перерасчету по ИПУ round(rashod,is_trunc)
                ret = ExecSQL(conn_db,
                    " update t_rsh set recalc_ipu=1," +
                    " rashod = valm+dlt_reval," +
                    " rsh1 = valmk+dlt_revalk" +
                    " where nzp_frm_typrs in (5,440) and abs(dlt_reval)>0.000001 and kod_info in (31,32,33) and (valm+dlt_reval)<0 "
                    , true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

                //ViewTbl(conn_db, " select * from t_rsh where nzp_kvar=551956 ");

                ret = ExecSQL(conn_db,
                    " update t_rsh set recalc_ipu=1," +
                    " rashod = case when valm+dlt_reval+dop87>0    then valm+dlt_reval+dop87    else 0 end, " +
                    " rsh1   = case when valmk+dlt_revalk+dop87k>0 then valmk+dlt_revalk+dop87k else 0 end" +
                    " where nzp_frm_typrs in (5,440) and abs(dlt_reval)>0.000001 and kod_info in (31,32,33) and (valm+dlt_reval)>=0 and rashod<=0.00000001 "
                    , true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

                //ViewTbl(conn_db, " select * from t_rsh where nzp_kvar=551956 ");

                ret = ExecSQL(conn_db,
                    " update t_rsh set recalc_ipu=1," +
                    " rashod = rashod + dlt_reval," +
                    " rsh1 = rsh1+dlt_revalk" +
                    " where nzp_frm_typrs in (5,440) and abs(dlt_reval)>0.000001 and kod_info in (31,32,33) and (valm+dlt_reval)>=0 and rashod>0.00000001 "
                    , true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

                //ViewTbl(conn_db, " select * from t_rsh where nzp_kvar=551956 ");

                ret = ExecSQL(conn_db,
                    " update t_rsh set recalc_ipu=1," +
                    " rashod = rashod + dlt_reval," +
                    " rsh1 = rsh1+dlt_revalk" +
                    " where nzp_frm_typrs in (5,440) and abs(dlt_reval)>0.000001 and kod_info not in (31,32,33) "
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
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
#if PG
                ret = ExecSQL(conn_db, " analyze t_rsh_recalc6 ", true);
#else
                ret = ExecSQL(conn_db, " update statistics for table t_rsh_recalc6 ", true);
#endif
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

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
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
#if PG
                ret = ExecSQL(conn_db, " analyze t_rsh_recalc9 ", true);
#else
                ret = ExecSQL(conn_db, " update statistics for table t_rsh_recalc9 ", true);
#endif
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

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

           //DataTable drr1= ViewTbl(conn_db, "select * from t_rsh ");
           //DataTable drr2= ViewTbl(conn_db, "select * from t_tarifs ");
           //DataTable drr3= ViewTbl(conn_db, "select * from t_prm_calc ");
            

            // вставка расчета в БД
            ssql =
                " Insert into " + gku.calc_gku_xx +
                " ( nzp_clc,nzp_dom,nzp_kvar,nzp_serv,nzp_supp,nzp_frm," + f_dat_charge +
                "nzp_frm_typ,gil,gil_g,squ,nzp_prm_tarif,tarif,trf1,trf2,trf3,trf4," +
                " nzp_frm_typrs,nzp_prm_rashod,rashod,rashod_g,rsh1,rsh2,rsh3,nzp_prm_trf_dt,tarif_f,rashod_norm,valm,dop87,is_device,rash_norm_one,dlt_reval ) " +
                " Select " +
                " 0 nzp_clc,t.nzp_dom,t.nzp_kvar,t.nzp_serv,t.nzp_supp,t.nzp_frm," + s_dat_charge +
                
                sNvlWord + "(p.nzp_frm_typ,0)," + sNvlWord + "(p.gil,0)," + sNvlWord + "(p.gil_g,0)," + sNvlWord + "(p.squ,0)," + sNvlWord + "(p.nzp_prm,0)," +
                sNvlWord + "(p.tarif,0)," + sNvlWord + "(p.tarif_gkal,0)," + sNvlWord + "(p.norm_gkal,0)," + sNvlWord + "(p.koef_gkal,0)," + sNvlWord + "(p.perc,0)," +
                
                sNvlWord + "(r.nzp_frm_typrs,0)," + sNvlWord + "(r.nzp_prm,0)," + sNvlWord + "(r.rashod,0)," + sNvlWord + "(r.rashod_g,0)," + sNvlWord + "(r.rsh1,0)," +
                sNvlWord + "(r.rsh2,0)," + sNvlWord + "(case when t.nzp_serv=8 then r.rsh3 else r.rsh_hv end,0) " +
                " ," + sNvlWord + "(p.nzp_prm_pt,0)," + sNvlWord + "(p.tarif_f,0)," + sNvlWord + "(r.rashod_norm,0)," + sNvlWord + "(r.valm,0)," + sNvlWord + "(r.dop87,0)," +
                sNvlWord + "(r.is_device,0)," + sNvlWord + "(r.rash_norm_one,0)," + sNvlWord + "(r.dlt_reval,0) " +
                
                " From t_tarifs t" +
#if PG
                " left outer join t_prm_calc p on t.nzp_kvar=p.nzp_kvar and t.nzp_frm=p.nzp_frm and t.nzp_supp=p.nzp_supp" +
                " left outer join t_rsh r on t.nzp_kvar=r.nzp_kvar and t.nzp_frm=r.nzp_frm " +
                " Where  1=1 ";
#else
                " ,outer t_prm_calc p,outer t_rsh r" +
                " Where t.nzp_kvar=p.nzp_kvar and t.nzp_supp=p.nzp_supp and t.nzp_frm=p.nzp_frm" +
                "   and t.nzp_kvar=r.nzp_kvar and t.nzp_frm=r.nzp_frm ";
#endif

            ExecByStep(conn_db, "t_tarifs", "nzp_key", ssql, 20000, "", out ret);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

                 

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
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

                ExecSQL(conn_db, " Drop table t_mustcalc_dop ", false);

                ret = ExecSQL(conn_db,
#if PG
                    " Create temp table t_mustcalc_dop (nzp_kvar integer) "
#else
                    " Create temp table t_mustcalc_dop (nzp_kvar integer) with no log "
#endif
                     , true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
                
                ret = ExecSQL(conn_db,
                    " Insert into t_mustcalc_dop (nzp_kvar) " +
                    " Select nzp_kvar From t_opn t " + ssqlfindls
                    , true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

                ExecSQL(conn_db, " Create index ix_mustcalc_dop on t_mustcalc_dop (nzp_kvar) ", true);
                ExecSQL(conn_db, sUpdStat + " t_mustcalc_dop ", true);

#if PG
                ret = ExecSQL(conn_db,
                    " Create temp table ttt_aid_virt0 (" +
                    " nzp_kvar integer," +
                    " nzp_supp integer," +
                    " sum_rcl  numeric(14,2) default 0.00 " +
                    " ) "
                    , true);
#else
                ret = ExecSQL(conn_db,
                    " Create temp table ttt_aid_virt0 (" +
                    " nzp_kvar integer," +
                    " nzp_supp integer," +
                    " sum_rcl  decimal(14,2) default 0.00 " +
                    " ) with no log "
                    , true);
#endif
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

                ret = ExecSQL(conn_db,
                    " Insert into ttt_aid_virt0 (nzp_kvar,nzp_supp)" +
                    " Select nzp_kvar,nzp_supp" +
                    " From " + gku.calc_gku_xx + " t" +
                    " Where nzp_serv = 17 and tarif * rashod > 0.0001 " +
                    "   and 0<(select count(*) from t_mustcalc_dop m where t.nzp_kvar=m.nzp_kvar) " +
                    " Group by 1,2 "
                    , true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

                ret = ExecSQL(conn_db, " Create index ixttt_aid_virt0 on ttt_aid_virt0(nzp_kvar) ", true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
                ExecSQL(conn_db, sUpdStat + " ttt_aid_virt0 ", true);

#if PG
                ret = ExecSQL(conn_db,
                    " Create temp table ttt_aid_virt1 (" +
                    " nzp_dom  integer," +
                    " nzp_kvar integer," +
                    " tarif    numeric(14,4) default 0.00," +
                    " squ      numeric(14,7) default 0.00," +
                    " koef_norm_odn numeric(14,7) default 0.00," +
                    " sum_odn  numeric(14,2) default 0.00 " +
                    " ) "
                    , true);
#else
                ret = ExecSQL(conn_db,
                    " Create temp table ttt_aid_virt1 (" +
                    " nzp_dom  integer," +
                    " nzp_kvar integer," +
                    " tarif    decimal(14,4) default 0.00," +
                    " squ      decimal(14,7) default 0.00," +
                    " koef_norm_odn decimal(14,7) default 0.00," +
                    " sum_odn  decimal(14,2) default 0.00 " +
                    " ) with no log "
                    , true);
#endif
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

                ret = ExecSQL(conn_db,
                    " Insert into ttt_aid_virt1 (nzp_dom,nzp_kvar,tarif,squ,sum_odn)" +
                    " Select nzp_dom,nzp_kvar,max(tarif),max(squ),sum(tarif * rashod)" +
                    " From " + gku.calc_gku_xx + " t" +
                    " Where nzp_serv = 515 and tarif * rashod > 0.0001 " +
                    "   and 0<(select count(*) from t_mustcalc_dop m where t.nzp_kvar=m.nzp_kvar) " +
                    " Group by 1,2 "
                    , true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
                
                // проставить норматив ОДН на 1 кв.метр
                ret = ExecSQL(conn_db,
                    " Update ttt_aid_virt1 set koef_norm_odn=" +
                    " (Select max(gl7kw)" +
                    " From " + gku.counters_xx + " s" +
                    " Where ttt_aid_virt1.nzp_dom=s.nzp_dom and s.stek = 354 and s.nzp_serv = 25 and s.gl7kw > 0.000001) " +
                    " Where 1=1 "
                    , true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

                ret = ExecSQL(conn_db,
                    " Update ttt_aid_virt1 set koef_norm_odn = 0" +
                    " Where koef_norm_odn is null "
                    , true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
                // обрезать перекидку нормативным расходом ОДН
                ret = ExecSQL(conn_db,
                    " Update ttt_aid_virt1 set sum_odn =" +
                    "  case when sum_odn<=squ*koef_norm_odn*tarif then sum_odn else squ*koef_norm_odn*tarif end" +
                    " Where koef_norm_odn > 0.000001 "
                    , true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

                ret = ExecSQL(conn_db, " create index ixttt_aid_virt1 on ttt_aid_virt1(nzp_kvar) ", true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
                ExecSQL(conn_db, sUpdStat + " ttt_aid_virt1 ", true);

                ret = ExecSQL(conn_db,
                    " Insert into " + gku.perekidka_xx +
                    " (nzp_kvar,num_ls,nzp_serv,nzp_supp,type_rcl,date_rcl,sum_rcl,month_,comment,nzp_user)" + 
                    " Select " +
                    "  t.nzp_kvar, t.num_ls, 17, a.nzp_supp, " + stypercl + ","+ sCurDate + ", (-1)*b.sum_odn," +
                    gku.paramcalc.cur_mm.ToString() + ",'Снятие ОДН по электроснабжению'," + inzpuser.ToString() + 
                    " From t_opn t,ttt_aid_virt0 a,ttt_aid_virt1 b" +
                    " Where t.nzp_kvar=a.nzp_kvar and t.nzp_kvar=b.nzp_kvar "
                    , true);
                if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }

                ExecSQL(conn_db, " Drop table t_mustcalc_dop ", false);
            }
            #endregion учет снятия ОДН в содержании жилья в ЕИРЦ г.Самаре - перекидками только для текущего расчетного месяца.

            #region Показать на как прошел расчет 
            ret = ExecRead(conn_db, out reader,
                " Select count(*) cnt From " + gku.calc_gku_xx
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false; }
            try
            {
                string mes = "";
                while (reader.Read())
                {
                    mes += "\r\n";

                    if (reader["cnt"] == DBNull.Value)
                        mes += " !!! cnt = no";
                    else
                        mes += " !!! кол-во serv л/с = " + Convert.ToString(reader["cnt"]);
                }
                reader.Close();

                ret.text = mes;
            }
            #endregion Показать на как прошел расчет

            #region Валимся сюда при любой ошибке
            catch (Exception ex)
            {
                reader.Close();
                ret.result = false; ret.text = ex.Message;
                CalcGkuXX_CloseTmp(conn_db); /*conn_db.Close();*/ return false;
            }
            #endregion Валимся сюда при любой ошибке

            #region Очистить временные таблицы , расчет окончен положительно
            CalcGkuXX_CloseTmp(conn_db);
            return true;
            
        }
            #endregion Очистить временные таблицы , расчет окончен положительно

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
            ExecSQL(conn_db, " Drop table t_tarifs ", false);
            ExecSQL(conn_db, " Drop table t_trf ", false);
            ExecSQL(conn_db, " Drop table t_prm ", false);
            ExecSQL(conn_db, " Drop table t_prm_max ", false);
            ExecSQL(conn_db, " Drop table t_rsh ", false);
            ExecSQL(conn_db, " Drop table t_rsh8 ", false);
            ExecSQL(conn_db, " Drop table t_par1 ", false);
            ExecSQL(conn_db, " Drop table t_par1d ", false);
            ExecSQL(conn_db, " Drop table t_par335 ", false);
            ExecSQL(conn_db, " Drop table t_par26f ", false);
            ExecSQL(conn_db, " Drop table t_par40f ", false);
            ExecSQL(conn_db, " Drop table t_par894 ", false);
            ExecSQL(conn_db, " Drop table t_kmnl ", false);
            ExecSQL(conn_db, " Drop table t_prm_calc ", false);
            ExecSQL(conn_db, " Drop table t_rsh390 ", false);
            ExecSQL(conn_db, " Drop table t_rsh_lift ", false);
            ExecSQL(conn_db, " Drop table t_par412 ", false);
            ExecSQL(conn_db, " Drop table t_prm400 ", false);
            ExecSQL(conn_db, " Drop table ttt_aid_virt0 ", false);
            ExecSQL(conn_db, " Drop table ttt_aid_virt1 ", false);
            ExecSQL(conn_db, " Drop table t_perc_pt ", false);
            ExecSQL(conn_db, " Drop table t_perc_pt_max ", false);
            ExecSQL(conn_db, " Drop table t_kg_m2 ", false);
            ExecSQL(conn_db, " Drop table t_rsh_privat ", false);
        }
        #endregion Удалить временные таблицы

        #region Генерация средних значений счетчиков
        //-----------------------------------------------------------------------------
        public void Call_GenSrZnKPU(int nzp_user, int p_year, int p_month, out Returns ret)
        //-----------------------------------------------------------------------------
        {
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
            Call_GenSrZnKPUWithOutConnect(conn_db, nzp_user, p_year, p_month, sNmTblLs, out ret);

            /*conn_db.Close();*/

            #endregion Выполнить расчет средних значений ИПУ
        }

        #region Сгенерировать средние значения счетчиков по группе 
        //-----------------------------------------------------------------------------
        public void Call_GenSrZnKPUWithOutConnect(IDbConnection conn_db, int nzp_user, int p_year, int p_month, string pNmTblLs, out Returns ret)
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
                " ) "+
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
            ret = ExecSQL(conn_db,
                " Insert Into t_ls_gen (nzp_dom,nzp_kvar,pref" + serial_fld + ")" +
                " Select nzp_dom,nzp_kvar,pref" + serial_val + 
                " From " + pNmTblLs + 
                " Where mark=1 group by 1,2,3 "
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
            ret = ExecRead(conn_db, out reader," Select pref,count(*) cnt From t_ls_gen Group by 1 Order by 1 ", true);
            if (!ret.result) { /*conn_db.Close();*/ return; }

            #endregion Определить лицевые счета для генерации

            #region Определить префиксы по которым нужно будет пройти положить в структуру sCurPref
            List<string> sPrefs = new List<string>();

            while (reader.Read())
            {
                if (reader["cnt"] != DBNull.Value)
                {
                    string sCurPref = Convert.ToString(reader["pref"]);
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
            /*
            if ((new DateTime(paramcalc.calc_yy, paramcalc.calc_mm, 01)) >= (new DateTime(2012, 09, 01)))
            {
                sKolGil = " * cnt1";
            }
            */
            //
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
                    " and dat_s <=" + MDY(p_month, 28, p_year)+
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
                if (iCntMn>0) 
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
                    ret = ExecSQL(conn_db,
                        " Insert Into t_doit (nzp_dom,nzp_kvar,nzp_counter,nzp_cnttype,count_days, saldo_date,nzp_serv)" +
                        " Select t.nzp_dom,t.nzp_kvar,s.nzp_counter,s.nzp_cnttype," + DateTime.DaysInMonth(p_year, p_month) + ",'28." + p_month.ToString("00") + "." + p_year.ToString("0000") + "'" +
                        " , s.nzp_serv " +
                        " From t_ls_gen t," + sDataAlias + "counters_spis s" +
                        " Where t.pref='" + sCurPref.Trim() + "' and t.nzp_kvar=s.nzp and s.nzp_type=3 and s.is_actual<>100 and s.dat_close is null " +
                          " and 0=(select count(*) from " + sDataAlias + "counters c where c.nzp_counter=s.nzp_counter " +
                          " and " + sNvlWord + "(c.dat_close," + MDY(1,1,1900) + ")>" + MDY(1,1,1950) + ")"
                        , true);
                    if (!ret.result) { /*conn_db.Close();*/ return; }

                    ret = ExecSQL(conn_db, " create index ix1_t_doit on t_doit (nzp_counter) ", true);
                    if (!ret.result) { /*conn_db.Close();*/ return; }
                    ret = ExecSQL(conn_db, " create index ix2_t_doit on t_doit (nzp_cnttype) ", true);
                    if (!ret.result) { /*conn_db.Close();*/ return; }
                    ret = ExecSQL(conn_db, " create index ix3_t_doit on t_doit (nzp_serv, nzp_kvar) ", true);
                    if (!ret.result) { /*conn_db.Close();*/ return; }
                    ExecSQL(conn_db, sUpdStat + " t_doit ", true);
                    //DataTable dt1 =ViewTbl(conn_db, "select * from t_doit ");
                    #endregion Выбрать показания по группе для индивидуальных приборов учета

                    #region Скинуть в архив предыдущие расчитанные средние для текущего месяца
                    ret = ExecSQL(conn_db,
                        " Update " + sDataAlias + "prm_17 Set is_actual=100 " +
                        " Where nzp_prm=979 and is_actual<>100 and dat_s >='" + dDtBeg.ToShortDateString() + "' " +
                        " and 0<(select count(*) from t_doit c where c.nzp_counter=" + sDataAlias + "prm_17.nzp) "
                        , true);
                    if (!ret.result) { /*conn_db.Close();*/ return; }

                    #endregion Скинуть в архив предыдущие расчитанные средние
                    

                    #region Обрезать дату действия среднего началом расчитываемого периода dDtBeg.
                    ret = ExecSQL(conn_db,
                        " Update " + sDataAlias + "prm_17 Set dat_po=date('" + dDtBeg.ToShortDateString() + "') " +
                        " Where nzp_prm=979 and is_actual<>100 and dat_s <'" + dDtBeg.ToShortDateString() + "' and dat_po>'" + dDtBeg.ToShortDateString() + "' " +
                        " and 0<(select count(*) from t_doit c where c.nzp_counter=" + sDataAlias + "prm_17.nzp) "
                        , true);
                    if (!ret.result) { /*conn_db.Close();*/ return; }

                    #endregion Обрезать дату действия среднего началом расчитываемого периода dDtBeg.

                    #region Выбрать максимальную дату учета в пределах действия параметра начала учета средних
                    ret = ExecSQL(conn_db,
                        " Update t_doit Set dk_uchet =" +
                        " (Select max(c.dat_uchet) From " + sDataAlias + "counters c" +
                         " Where c.nzp_counter=t_doit.nzp_counter and c.is_actual<>100 and c.dat_uchet>='" + dDtUseCnt.ToShortDateString() + "' and " + sNvlWord + "(c.ist,0) <> 7 ) " +
                        " Where 0<(Select count(*) From " + sDataAlias + "counters c" +
                         " Where c.nzp_counter=t_doit.nzp_counter and c.is_actual<>100 and c.dat_uchet>='" + dDtUseCnt.ToShortDateString() + "' and " + sNvlWord + "(c.ist,0) <> 7 ) "
                        , true);
                    if (!ret.result) { /*conn_db.Close();*/ return; }

                    #endregion Выбрать максимальную дату учета в пределах действия параметра начала учета средних




                    #region Выбрать минимальную дату учета в пределах действия параметра начала учета средних
                    //  dt1 = ViewTbl(conn_db, "select * from t_doit ");
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
                        " Where 0<(Select count(*) From " + sDataAlias + "counters c" +
                        " Where c.nzp_counter=t_doit.nzp_counter and c.is_actual<>100 and c.dat_uchet>='" + dDtUseCnt.ToShortDateString() + "' and " + sNvlWord + "(c.ist,0) <> 7  " +
                        " and c.dat_uchet>=t_doit.dk_uchet-" + sDopSql + " ) "
                        , true);
                    if (!ret.result) { /*conn_db.Close();*/ return; }



                    // исправить минимальную дату если не хватает до нормального периода
                    //  DataTable   dt1 = ViewTbl(conn_db, "select * from t_doit ");
#if PG
                    ret = ExecSQL(conn_db,
                        " Update t_doit Set dn_uchet =" +
                        " (Select max(c.dat_uchet) From " + sCurPref.Trim() + "_data.counters c" +
                         " Where c.nzp_counter=t_doit.nzp_counter and c.is_actual<>100  " +
                         " and c.dat_uchet>=t_doit.dk_uchet- interval '35 month'  and t_doit.dn_uchet>c.dat_uchet and coalesce(c.ist,0) <> 7 ) " +
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

                    // dt1 = ViewTbl(conn_db, "select * from t_doit ");
                    #endregion Выбрать минимальную дату учета в пределах действия параметра начала учета средних



                    #region выбрать начальное и конечное показание
                    ret = ExecSQL(conn_db,
                        " Update t_doit Set vn_cnt =" +
                        " (Select max(c.val_cnt) From " + sDataAlias + "counters c" +
                         " Where c.nzp_counter=t_doit.nzp_counter and c.is_actual<>100 and c.dat_uchet=t_doit.dn_uchet and t_doit.dn_uchet>'01.01.1900' and " + sNvlWord + "(c.ist,0) <> 7 ) " +
                        " Where 0<(Select count(*) From " + sDataAlias + "counters c" +
                         " Where c.nzp_counter=t_doit.nzp_counter and c.is_actual<>100 and c.dat_uchet=t_doit.dn_uchet and t_doit.dn_uchet>'01.01.1900' and " + sNvlWord + "(c.ist,0) <> 7 ) "
                        , true);
                       if (!ret.result) { /*conn_db.Close();*/ return; }
                    //    dt1 = ViewTbl(conn_db, "select * from t_doit ");
                    ret = ExecSQL(conn_db,
                        " Update t_doit Set vk_cnt =" +
                        " (Select max(c.val_cnt)   From " + sDataAlias + "counters c" +
                         " Where c.nzp_counter=t_doit.nzp_counter and c.is_actual<>100 and c.dat_uchet=t_doit.dk_uchet and t_doit.dk_uchet>'01.01.1900' and " + sNvlWord + "(c.ist,0) <> 7 ) " +
                        " Where 0<(Select count(*) From " + sDataAlias + "counters c" +
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
                 //   dt1 = ViewTbl(conn_db, "select * from t_doit ");
                    #region Выставить группу 61 или 65 (искл гр 63), подсчитать количество дней, "0Генерация средних расходов КПУ - установлен расход"
                
                    ret = ExecSQL(conn_db,
                        " Update t_doit Set nzp_group = (case when abs(vk_cnt-vn_cnt)>0 then 61 else 65 end), cnt_days_rs = (dk_uchet - dn_uchet)" + sConvToInt +
                        " Where dn_uchet>'01.01.1900' and dk_uchet>'01.01.1900' and dk_uchet>dn_uchet and nzp_group <> 63 "
                        , true);
                    if (!ret.result) { /*conn_db.Close();*/ return; }
                
                    #endregion Выставить группу 61 (искл гр 63),"0Генерация средних расходов КПУ - установлен расход"
                //        dt1 = ViewTbl(conn_db, "select * from t_doit ");
                    #region Определить разрядность счетчика и коэф трансф
                    ret = ExecSQL(conn_db,
                        " Update t_doit Set "+
                        " cnt_stage=(Select cnt_stage From " + sKernelAlias + "s_counttypes t Where t_doit.nzp_cnttype=t.nzp_cnttype)," +
                        " mmnog =(Select " + sNvlWord + "(formula,'1')" + sConvToNum + " From " + sKernelAlias + "s_counttypes t Where t_doit.nzp_cnttype=t.nzp_cnttype) " +
                        " Where 0<(Select count(*) From " + sKernelAlias + "s_counttypes t Where t_doit.nzp_cnttype=t.nzp_cnttype) "
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
                    #endregion Расчитать расход с учетом коэф трансф и перехода через 0
                //    dt1 = ViewTbl(conn_db, "select * from t_doit ");
                    #region Разделить на количество месяцев

                   // if (Points.IsSmr)  -- убрал потому что в днях считает неправильно , а в месяцах нормально 
                    {
                        #region Расход в месяцах

#if PG
                        #region Нужно отсечь показания не с начала месяца
                        ret = ExecSQL(conn_db,
                            " Update t_doit Set dn_uchet= "+
                            "( '01.' ||EXTRACT(month FROM (dn_uchet+interval '1 month')) ||'.'|| EXTRACT(year FROM (dn_uchet+interval '1 month')) )" + sConvToDate +
                            " Where nzp_group in (61,65) and EXTRACT(day FROM dn_uchet) > 1 and EXTRACT(day FROM dn_uchet)<31 "
                            , true);
                        ret = ExecSQL(conn_db,
                            " Update t_doit Set dn_uchet= " +
                            "( '01.' ||EXTRACT(month FROM (dn_uchet+interval '1 day'))   ||'.'|| EXTRACT(year FROM (dn_uchet+interval '1 day'))   )" + sConvToDate +
                            " Where nzp_group in (61,65) and EXTRACT(day FROM dn_uchet)=31 "
                            , true);
                        #endregion Нужно отсечь показания не с начала месяца
                        ret = ExecSQL(conn_db,
                            " Update t_doit Set "+
                            " kol_mes    = (( EXTRACT(month FROM dk_uchet)  +(EXTRACT(year FROM  dk_uchet)  *12))-( EXTRACT(month FROM dn_uchet)+(12*EXTRACT(year FROM dn_uchet)) ) ) " +
                            ",kol_mes_sr = (( EXTRACT(month FROM saldo_date)+(EXTRACT(year FROM  saldo_date)*12))-( EXTRACT(month FROM dk_uchet)+(12*EXTRACT(year FROM dk_uchet)) ) )" +
                            " Where nzp_group in (61,65) and (( EXTRACT(month FROM dk_uchet)+(EXTRACT(year FROM  dk_uchet)*12))-( EXTRACT(month FROM dn_uchet)+(12*EXTRACT(year FROM dn_uchet)) ) ) > 0 "
                            , true);
                        if (!ret.result) { /*conn_db.Close();*/ return; }
                        ret = ExecSQL(conn_db,
                            " Update t_doit Set gen_rash_pu = rashod/"+
                            " ( case when kol_mes_sr=0 then 0 else 0 end + " +
                            "   (EXTRACT(month FROM dk_uchet)+(EXTRACT(year FROM  dk_uchet)*12)) - (EXTRACT(month FROM dn_uchet)+(12*EXTRACT(year FROM dn_uchet)) " +
                            " )) " +
                            " Where nzp_group in (61,65) and " +
                            " ( (EXTRACT(month FROM dk_uchet)+(EXTRACT(year FROM  dk_uchet)*12)) - (EXTRACT(month FROM dn_uchet)+(12*EXTRACT(year FROM (dn_uchet)) ) )) > 0 "
                            , true);
                        if (!ret.result) { /*conn_db.Close();*/ return; }
                        ret = ExecSQL(conn_db,
                            " Update t_doit Set gen_rash_pu = round(gen_rash_pu,2) " +
                            " Where nzp_group in (61,65) "
                            , true);
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
                  //      dt1 = ViewTbl(conn_db, "select * from t_doit ");
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
                    ret = ExecSQL(conn_db, " Update t_doit Set kol_count =1 " +
                                           " where nzp_group in (65) and gen_rash_pu=0 and kol_mes_sr<=6 and kol_mes>=3   " +
                                           " and (select count(*) from t_doitR a where a.nzp_serv =t_doit.nzp_serv and a.nzp_kvar=t_doit.nzp_kvar )>0 "
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
                          " gen_rash_pu" + sConvToChar + ",1,1," + sCurDate + serial_val +
                        " from t_doit where nzp_group = 61  and gen_rash_pu>0 and kol_mes_sr<=6 and kol_mes>=3 "
                        , true);
                    if (!ret.result) { /*conn_db.Close();*/ return; }
                    //   dt1 = ViewTbl(conn_db, "select * from t_doit ");
                    // теперь добавим нулевые средние если есть не нулевые 
                    ret = ExecSQL(conn_db,
                        " insert into " + sDataAlias + "prm_17 (nzp,nzp_prm,dat_s,dat_po,val_prm,is_actual,nzp_user,dat_when" + serial_fld + ") " +
                        " select nzp_counter,979,'" + dDtBeg.ToShortDateString() + "','" + dDtEnd.ToShortDateString() + "', " +
                          " gen_rash_pu" + sConvToChar + ",1,1," + sCurDate + serial_val + 
                        " from t_doit where nzp_group in ( 65 )  and kol_count >0 and gen_rash_pu=0 and kol_mes_sr<=6 and kol_mes>=3  "
                        , true);
                    if (!ret.result) { /*conn_db.Close();*/ return; }
                    //    dt1 = ViewTbl(conn_db, "select * from t_doit ");
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

            ret = ExecSQL(conn_db,
                " delete from " + sDataAlias + "s_group where nzp_group in (61,62,63,64,65) "
                , true);
            if (!ret.result) { /*conn_db.Close();*/ return; }
            ret = ExecSQL(conn_db,
                " insert into " + sDataAlias + "s_group (nzp_group,ngroup) values (61,'0Генерация средних расходов ИПУ - установлен расход -" + sname_month + "') "
                , true);
            if (!ret.result) { /*conn_db.Close();*/ return; }
            ret = ExecSQL(conn_db,
                " insert into " + sDataAlias + "s_group (nzp_group,ngroup) values (62,'0Генерация средних расходов ИПУ - нет показаний для генерации -" + sname_month + "') "
                , true);
            if (!ret.result) { /*conn_db.Close();*/ return; }
            ret = ExecSQL(conn_db,
                " insert into " + sDataAlias + "s_group (nzp_group,ngroup) values (63,'0Генерация средних расходов ИПУ - есть показание ИПУ -" + sname_month + "') "
                , true);
            if (!ret.result) { /*conn_db.Close();*/ return; }
            // 64,"0Генерация средних расходов КПУ - нет КПУ"
            ret = ExecSQL(conn_db,
                " insert into " + sDataAlias + "s_group (nzp_group,ngroup) values (64,'0Генерация средних расходов ИПУ - нет ИПУ -" + sname_month + "') "
                , true);
            if (!ret.result) { /*conn_db.Close();*/ return; }
            ret = ExecSQL(conn_db,
                " insert into " + sDataAlias + "s_group (nzp_group,ngroup) values (65,'0Генерация средних расходов ИПУ - расход ИПУ равен 0 -" + sname_month + "') "
                , true);
            if (!ret.result) { /*conn_db.Close();*/ return; }

        }
        #endregion Создание наименований групп по средним значениям ПУ

        #region Наполнение группы по сгенерированным значениям счетчиков
        //-----------------------------------------------------------------------------
        void CrtGenSrZnKPUGroups(IDbConnection conn_db, string sGrpPref, out Returns ret)
        //-----------------------------------------------------------------------------
        {
            string sDataAlias = sGrpPref.Trim() + "_data" + tableDelimiter;

            ret = ExecSQL(conn_db,
                " delete from " + sDataAlias + "link_group where nzp_group in (61,62,63,64,65) "
                , false);
            ret = ExecSQL(conn_db,
                " insert into " + sDataAlias + "link_group (nzp_group,nzp) select nzp_group,nzp_kvar from t_doit group by 1,2 "
                , true);
            if (!ret.result) { /*conn_db.Close();*/ return; }
            ret = ExecSQL(conn_db,
                " insert into " + sDataAlias + "link_group (nzp_group,nzp)" +
                " select 64 nzp_group,g.nzp_kvar from t_ls_gen g " +
                " Where g.pref='" + sGrpPref.Trim() + "' and 0=(select count(*) from t_doit k where k.nzp_kvar=g.nzp_kvar)" +
                " group by 1,2 "
                , true);
            if (!ret.result) { /*conn_db.Close();*/ return; }

        }

        #endregion Наполнение группы по сгенерированным значениям счетчиков

        #endregion Генерация средних значений счетчиков

    }
    #endregion Полный цикл от выборки тарифов и расходов , до сохранения в БД
}
#endregion Здесь производится заполнение таблиц Calc_gku_XX