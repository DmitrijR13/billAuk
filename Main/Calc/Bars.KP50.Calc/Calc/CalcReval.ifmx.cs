using System;
using System.Linq;
using System.Text;
using System.Data;
using System.Collections.Generic;
using System.Globalization;
using Bars.KP50.Utils;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.IFMX.Kernel.source.CommonType;
using System.Diagnostics;

namespace STCLINE.KP50.DataBase
{
    public partial class DbCalcCharge : DataBaseHead
    {
        public struct PeriodMustCalc
        {
            public DateTime dat_s;
            public DateTime dat_po;
        }

        //-----------------------------------------------------------------------------
        public bool CalcRevalXX(CalcTypes.ParamCalc paramcalc, out Returns ret)
        //-----------------------------------------------------------------------------
        {
            string conn_kernel = Points.GetConnByPref(paramcalc.pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            //IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                return false;
            }

            //спозиционируемся сразу на data-базу
#if PG
            //PgExtensions.CheckDatabaseInSchema(paramcalc.pref.Trim() + "_data", conn_db);
            ret = ExecSQL(conn_db, " set search_path to '" + paramcalc.pref.Trim() + "_data'", true);
#else
            ret = ExecSQL(conn_db, " Database " + paramcalc.pref.Trim() + "_data", true);
#endif
            if (!ret.result)
            {
                conn_db.Close();
                return false;
            }
            string s = paramcalc.pref + " " + paramcalc.nzp_kvar + "/" + paramcalc.nzp_dom;
            MonitorLog.WriteLog(Environment.NewLine, MonitorLog.typelog.Info, 1, 2, true);
            MonitorLog.WriteLog("Старт! " + s, MonitorLog.typelog.Info, 1, 2, true);
            /*
            //TEST

            int inzp_serv = 25;
            int inzp_supp = 30;

            MakeProtCalcForMonth(conn_db, paramcalc, inzp_serv, inzp_supp,out ret);

            //
            */
            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            //Портал: заполнить при необходимости в первый раз charge_cnts, etc.
            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            if (paramcalc.isPortal && paramcalc.b_data)
            {
                Portal_LoadData(conn_db, paramcalc, out ret);
                ExecSQL(conn_db, " Drop table tmp_cnts ", false);

                if (!ret.result)
                {
                    conn_db.Close();
                    return false;
                }
            }
            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            //основной расчет
            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            bool b = _CalcRevalXX(conn_db, paramcalc, out ret);

            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            //Портал: записать результаты в charge_g
            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            if (b && ret.result && paramcalc.isPortal)
            {
                Portal_SaveCalc(conn_db, paramcalc, out ret); //сохранить результат расчета в charge_g
                b = ret.result;
            }

            //вернуть базу на главный kernel
#if PG
            Returns r = ExecSQL(conn_db, " set search_path to '" + Points.Pref + "_kernel'", true);
#else
            Returns r = ExecSQL(conn_db, " Database " + Points.Pref + "_kernel", true);
#endif
            conn_db.Close();

            return b;
        }

        void DropCalcRevalTmp(IDbConnection conn_db)
        //-----------------------------------------------------------------------------
        {
            ExecSQL(conn_db, " Drop table t_opn ", false);
            ExecSQL(conn_db, " Drop table t_selkvar ", false);
            ExecSQL(conn_db, " Drop table t_mustcalc ", false);
        }

        //-----------------------------------------------------------------------------
        bool _CalcRevalXX(IDbConnection conn_db, CalcTypes.ParamCalc paramcalc, out Returns ret)
        //-----------------------------------------------------------------------------
        {
            string s = paramcalc.pref + " " + paramcalc.nzp_kvar + "/" + paramcalc.nzp_dom;
            //  Constants.TraceLongQueries = true;
            #region Перерасчеты
            //выбрать множество лицевых счетов
            ChoiseTempKvar(conn_db, ref paramcalc, false, out ret); //пока без выборки t_mustcalc
            if (!ret.result) { DropCalcRevalTmp(conn_db); return false; }

            //чистим группу с nzp_group=13 - лс с большими показаниями 
            ClearGroupWithBigValPu(conn_db, paramcalc, ref ret);
            if (!ret.result) { DropCalcRevalTmp(conn_db); return false; }

            //препарированные таблицы counters,prm_1, counters_dom
            LoadTempTablesRashod(conn_db, ref paramcalc, out ret);
            if (!ret.result) { DropCalcRevalTmp(conn_db); return false; }

            paramcalc.b_loadtemp = false;

            //вызываем заполнение must_calc
            InsMustCalc(conn_db, paramcalc, out ret);
            if (!ret.result) { DropCalcRevalTmp(conn_db); return false; }

            //выбрать t_mustcalc
            ChoiseTempMustCalc(conn_db, paramcalc, out ret);
            if (!ret.result) { return false; }


            //определить перерасчитываемые месяцы
            List<DateTime> lcalc_months = new List<DateTime>();
            List<PeriodMustCalc> lcalc_months_portal = new List<PeriodMustCalc>();

            if (paramcalc.isPortal)
            {
                //считаем границы Порталовского перерасчета 
                Portal_MustCalc(conn_db, paramcalc, ref lcalc_months_portal, out ret);
            }

            IDataReader reader;
            DateTime mustDat_s = new DateTime(2001, 1, 1);
            DateTime mustDat_po = new DateTime(3001, 1, 1);

            #region для начала определим границы перерасчетов (для убыстрения последующей выборки)

            //получаем дату начала расчета/перерасчета
            var dateStartReval = DBManager.ExecScalar<DateTime>(conn_db,
                " SELECT MAX(CAST(val_prm as date)) date " +
                " FROM " + paramcalc.data_alias +
                " prm_10 WHERE nzp_prm IN (82, 771) and is_actual<>100 and '" +
                new DateTime(paramcalc.cur_yy, paramcalc.cur_mm, 01).ToShortDateString() + "' BETWEEN dat_s and dat_po",
                out ret, true);
            if (!ret.result) { DropCalcRevalTmp(conn_db); return false; }


            ret = ExecRead(conn_db, out reader,
                    " SELECT MIN(dat_s) as dat_s, MAX(dat_po) as dat_po " +
                    " FROM " + paramcalc.data_alias + "must_calc m " +
                    " WHERE year_ = " + paramcalc.cur_yy +
                    "   AND month_ =  " + paramcalc.cur_mm +
                    "   AND (dat_s >= " + Utils.EStrNull(dateStartReval.ToShortDateString()) + " " +
                    "        OR dat_po >= " + Utils.EStrNull(dateStartReval.ToShortDateString()) +
                    "        )" +
                    "   AND EXISTS (SELECT 1 FROM t_selkvar t WHERE t.nzp_kvar=m.nzp_kvar) ",
                      true);
            if (!ret.result) { DropCalcRevalTmp(conn_db); return false; }

            try
            {
                if (reader.Read())
                {
                    mustDat_s = CastValue<DateTime>(reader["dat_s"]);
                    mustDat_po = CastValue<DateTime>(reader["dat_po"]);

                    if (mustDat_s < dateStartReval)
                    {
                        mustDat_s = dateStartReval;
                    }
                }

            }
            catch (Exception ex)
            {
                MonitorLog.WriteException("Ошибка получения границ перерасчетов");
            }
            reader.Close();

            #endregion для начала определим границы перерасчетов (для убыстрения последующей выборки)


            paramcalc.b_reval = false;  //перерасчетный месяц
            paramcalc.b_must = false;  //НЕ учитываем must_calc в выборке
            CalcTypes.ChargeXX cur_chargeXX = new CalcTypes.ChargeXX(paramcalc); //текущий расчетный месяц


            #region получаем месяцы для расчета
            //получаем список месяцев для перерасчета
            var ListMonthsForCalc = CalcMonths(paramcalc);
            ret = GetRecalcMonthAndDeleteOldRecalcs(conn_db, paramcalc, ListMonthsForCalc, mustDat_s,
                mustDat_po, ref lcalc_months, ref lcalc_months_portal);
            if (!ret.result)
            {
                return false;
            }
            paramcalc.count_calc_months = lcalc_months.Count + 1; //число перерасчитываемых месяцев + 1 текущий
            cur_chargeXX.paramcalc.count_calc_months = lcalc_months.Count + 1; //число перерасчитываемых месяцев + 1 текущий
            #endregion получаем месяцы для расчета


            if (cur_chargeXX.paramcalc.nzp_kvar == 0)
                paramcalc.b_report = true;   //считаем report_xx

            CreateChargeXX(conn_db, cur_chargeXX, true, out ret);
            if (!ret.result) { DropCalcRevalTmp(conn_db); return false; }


            #region открываем цикл по перерасчитываемым месяцам; по идее, можно даже запустить в нескольких потоках одновременно

            foreach (DateTime d in lcalc_months)
            {
                paramcalc.calc_yy = d.Year;
                paramcalc.calc_mm = d.Month;

                paramcalc.b_report = false; //не считаем report_xx
                paramcalc.b_reval = true;  //перерасчетный месяц
                paramcalc.b_must = true;  //учитываем must_calc в выборке

                //выбрать мно-во лс в перерасчетном месяце
                ChoiseTempKvar(conn_db, ref paramcalc, out ret);
                SetStatusCalcTask(conn_db, paramcalc, (0.2m / (decimal)paramcalc.count_calc_months) * 0.5m, CalcTypes.CalcSteps.PrepareParams);
                if (!ret.result) { return false; }

                //выбрать во временные таблицы месячные данные
                LoadTempTablesForMonth(conn_db, ref  paramcalc, out ret);
                SetStatusCalcTask(conn_db, paramcalc, (0.2m / (decimal)paramcalc.count_calc_months) * 0.5m, CalcTypes.CalcSteps.PrepareMonthParams);
                if (!ret.result) { DropCalcRevalTmp(conn_db); return false; }

                //расчет целевых таблиц
                CalcFull(conn_db, paramcalc, out ret);
                if (!ret.result) { DropCalcRevalTmp(conn_db); return false; }

                //сравнить charge_xx и вычислить дельту
                LoadDeltaReval(conn_db, paramcalc, out ret);
                if (!ret.result) { DropCalcRevalTmp(conn_db); return false; }
            }
            #endregion открываем цикл по перерасчитываемым месяцам; по идее, можно даже запустить в нескольких потоках одновременно

            #endregion Перерасчеты

            #region Расчет текущего месяца
            //полный текущий расчет - пересоздадим таблицы-выборки
            SetStatusCalcTask(conn_db, paramcalc, (0.2m / (decimal)paramcalc.count_calc_months) * 0.5m, CalcTypes.CalcSteps.PrepareParams);
            ChoiseTempKvar(conn_db, ref cur_chargeXX.paramcalc, out ret);
            if (!ret.result) { return false; }

            #region lnk_charge_xx
            //удаление lnk_charge_xx прошло, когда удалялся reval_xx
            /*
            ret = ExecSQL(conn_db,
                    " Delete From " + cur_chargeXX.lnk_charge_xx +
                    " Where nzp_kvar in ( Select nzp_kvar From t_selkvar) "
                , true);
            if (!ret.result)
            {
                //conn_db.Close();
                return false;
            }
            */

            //вставить lnk_charge_xx

            if (cur_chargeXX.paramcalc.nzp_kvar > 0 || cur_chargeXX.paramcalc.nzp_dom > 0 || cur_chargeXX.paramcalc.list_dom)
            {

                string sql2 =
                            " Insert into " + cur_chargeXX.lnk_charge_xx + " (nzp_kvar,year_,month_) " +
                            " Select " + sUniqueWord +
                            " nzp_kvar,year_,month_ " +
                            " From " + cur_chargeXX.reval_xx +
                            " Where " + cur_chargeXX.where_kvar;
                //nzp_kvar in ( Select nzp_kvar From t_selkvar) "
                ret = ExecSQL(conn_db, sql2, true);
            }
            else
            {

                string sql2 =
                            " Insert into " + cur_chargeXX.lnk_charge_xx + " (nzp_kvar,year_,month_) " +
                            " Select " + sUniqueWord +
                            " nzp_kvar,year_,month_ " +
                            " From " + cur_chargeXX.reval_xx +
                            " Where 1=1 ";
                ExecByStep(conn_db, cur_chargeXX.reval_xx, "nzp_reval", sql2, 100000, " ", out ret);
            }
            if (!ret.result) { return false; }

            UpdateStatistics(false, cur_chargeXX.paramcalc, cur_chargeXX.lnk_tab, out ret);
            if (!ret.result) { return false; }
            #endregion lnk_charge_xx

            //выбрать во временные таблицы текущий  месяц
            SetStatusCalcTask(conn_db, paramcalc, (0.2m / (decimal)paramcalc.count_calc_months) * 0.5m, CalcTypes.CalcSteps.PrepareMonthParams);
            LoadTempTablesForMonth(conn_db, ref cur_chargeXX.paramcalc, out ret);
            if (!ret.result) { return false; }


            //полный расчет текущего месяца
            CalcFull(conn_db, cur_chargeXX.paramcalc, out ret);
            if (!ret.result) { return false; }
            #endregion Расчет текущего месяца

            SetStatusCalcTask(conn_db, paramcalc, 1, CalcTypes.CalcSteps.Complete);
            MonitorLog.WriteLog("Стоп! " + s, MonitorLog.typelog.Info, 1, 2, true);
            return true;
        }

        private bool ClearGroupWithBigValPu(IDbConnection conn_db, CalcTypes.ParamCalc paramcalc, ref Returns ret)
        {
            //удаляем из группы лс, которые сейчас расчитываем
            var sql = " DELETE FROM " + paramcalc.pref + sDataAliasRest + "link_group " +
                      " WHERE nzp_group = 13 " +
                      " AND nzp in (SELECT nzp_kvar FROM t_selkvar)";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, paramcalc.pref);
                return false;
            }
            return true;
        }

        private Returns GetRecalcMonthAndDeleteOldRecalcs(IDbConnection conn_db, CalcTypes.ParamCalc paramcalc,
            List<CalcMonth> ListMonthsForCalc, DateTime mustDat_s,
            DateTime mustDat_po, ref  List<DateTime> lcalc_months, ref  List<PeriodMustCalc> lcalc_months_portal)
        {
            Returns ret = Utils.InitReturns();
            IDataReader reader = null;

            foreach (var dateForCalc in ListMonthsForCalc)
            {
                var mm = dateForCalc.month;
                var yy = dateForCalc.year;

                string sFirst1
#if PG
 = "";
#else
            " first 1 ";
#endif
                //прежде почистим все перерасчеты по dat_charge = mdy(cur_mm,28,cur_yy)
                CalcTypes.ParamCalc pc = new CalcTypes.ParamCalc(paramcalc.nzp_kvar, paramcalc.nzp_dom, paramcalc.pref,
                    yy, mm, paramcalc.cur_yy, paramcalc.cur_mm);
                pc = paramcalc;
                pc.calc_mm = mm;
                pc.calc_yy = yy;
                // включить признак перерасчета в случае необходимости
                pc.b_reval = !(yy == paramcalc.cur_yy && mm == paramcalc.cur_mm);
                //
                CalcTypes.ChargeXX calc_chargeXX = new CalcTypes.ChargeXX(pc); //расчетная таблица начислений

                //если что, создадим таблицы
                CreateChargeXX(conn_db, calc_chargeXX, false, out ret);
                if (!ret.result)
                {
                    reader.Close();
                    DropCalcRevalTmp(conn_db);
                    return ret;
                }

                //сначало смотрим были ли они в данном месяце

                ret = ExecRead(conn_db, out reader,
                    " Select " + sFirst1 + " nzp_kvar" +
                    " From " + calc_chargeXX.charge_xx +
                    " Where nzp_kvar in ( Select nzp_kvar From t_selkvar ) " +
                    calc_chargeXX.paramcalc.per_dat_charge +
                    DBManager.Limit1
                    , false);
                if (ret.result)
                {
                    try
                    {
                        if (reader.Read())
                        {
                            var where_prehibited_recalc = " and not exists (select 1 from temp_prohibited_recalc_begin pr " +
                                          " where " + calc_chargeXX.charge_xx + ".nzp_serv = pr.nzp_serv and " + calc_chargeXX.charge_xx +
                                          ".nzp_supp = pr.nzp_supp " +
                                          " and pr.nzp_kvar = " + calc_chargeXX.charge_xx + ".nzp_kvar and " +
                                          "'" + new DateTime(paramcalc.calc_yy, paramcalc.calc_mm, 1).ToShortDateString() + "' between dat_s and dat_po)";
                            //удалить перерасчеты в данном месяце
                            if (calc_chargeXX.paramcalc.nzp_kvar > 0 || calc_chargeXX.paramcalc.nzp_dom > 0 ||
                                calc_chargeXX.paramcalc.list_dom)
                            {
                                string sql =
                                    " Delete From " + calc_chargeXX.charge_xx +
                                    " Where " + calc_chargeXX.where_kvar +
                                    //nzp_kvar in ( Select nzp_kvar From t_selkvar ) " +
                                    calc_chargeXX.paramcalc.per_dat_charge + where_prehibited_recalc;
                                ret = ExecSQL(conn_db, sql, true);
                            }
                            else
                            {
                                string sql =
                                    " Delete From " + calc_chargeXX.charge_xx +
                                    " Where 1=1 " + calc_chargeXX.paramcalc.per_dat_charge + where_prehibited_recalc;
                                ExecByStep(conn_db, calc_chargeXX.charge_xx, "nzp_charge", sql, 50000, " ", out ret);
                            }
                            if (!ret.result)
                            {
                                DropCalcRevalTmp(conn_db);
                                return ret;
                            }
                        }
                    }
                    catch
                    {
                        DropCalcRevalTmp(conn_db);
                        return ret;
                    }
                    finally
                    {
                        reader.Close();
                    }
                }

                //Внимание!!!
                //при настоящем расчете надо чистить также и исходный charge!!!
                if (!paramcalc.b_again)
                {
                    string schema = pc.pref + "_charge_" + (pc.calc_yy - 2000).ToString("00");
                    if (DBManager.SchemaExists(schema, conn_db))
                    {
                        ret = ExecRead(conn_db, out reader,
                            " Select " + sFirst1 + " nzp_kvar" +
                            " From " + calc_chargeXX.charge_xx_ishod +
                            " Where nzp_kvar in ( Select nzp_kvar From t_selkvar ) " +
                            calc_chargeXX.paramcalc.per_dat_charge +
                            DBManager.Limit1
                            , true);
                        if (!ret.result)
                        {
                            DropCalcRevalTmp(conn_db);
                            return ret;
                        }

                        try
                        {
                            if (reader.Read())
                            {
                                //удалить перерасчеты в данном месяце
                                var where_prehibited_recalc = " and not exists (select 1 from temp_prohibited_recalc_begin pr " +
                                         " where " + calc_chargeXX.charge_xx_ishod + ".nzp_serv = pr.nzp_serv and " + calc_chargeXX.charge_xx_ishod +
                                         ".nzp_supp = pr.nzp_supp  " +
                                         " and pr.nzp_kvar = " + calc_chargeXX.charge_xx_ishod + ".nzp_kvar and " +
                                         "'" + new DateTime(paramcalc.calc_yy, paramcalc.calc_mm, 1).ToShortDateString() + "' between dat_s and dat_po)";
                                if (calc_chargeXX.paramcalc.nzp_kvar > 0 || calc_chargeXX.paramcalc.nzp_dom > 0 ||
                                    calc_chargeXX.paramcalc.list_dom)
                                {
                                    string sql =
                                        " Delete From " + calc_chargeXX.charge_xx_ishod +
                                        " Where " + calc_chargeXX.where_kvar +
                                        //nzp_kvar in ( Select nzp_kvar From t_selkvar ) " +
                                        calc_chargeXX.paramcalc.per_dat_charge + where_prehibited_recalc;
                                    ret = ExecSQL(conn_db, sql, true);
                                }
                                else
                                {
                                    string sql =
                                        " Delete From " + calc_chargeXX.charge_xx_ishod +
                                        " Where 1 = 1 " + calc_chargeXX.paramcalc.per_dat_charge + where_prehibited_recalc;
                                    ExecByStep(conn_db, calc_chargeXX.charge_xx_ishod, "nzp_charge", sql, 50000, " ",
                                        out ret);
                                }

                                if (!ret.result)
                                {
                                    reader.Close();
                                    DropCalcRevalTmp(conn_db);
                                    return ret;
                                }
                            }
                        }
                        catch
                        {
                            reader.Close();
                            DropCalcRevalTmp(conn_db);
                            return ret;
                        }
                    }
                }

                bool b_portal_calc = true;

                //определим, имеются ли ссылка для перерасчета в данном месяце (при условии, что месяц попадает в интервал [mustDat_s..mustDat_po] )
                DateTime mustDat1 = new DateTime(yy, mm, 01);
                DateTime mustDat2 = new DateTime(yy, mm, DateTime.DaysInMonth(yy, mm));

                if (mustDat2 >= mustDat_s && mustDat1 <= mustDat_po)
                {
                    ret = ExecRead(conn_db, out reader,
                        " Select " + sFirst1 + " nzp_kvar From " + paramcalc.data_alias + "must_calc " +
                        " Where year_ = " + paramcalc.cur_yy +
                        "   and month_ =  " + paramcalc.cur_mm +
                        "   and dat_po >= " + MDY(mm, 1, yy) +
                        "   and dat_s  <= " + MDY(mm, 28, yy) +
                        "   and nzp_kvar in ( Select nzp_kvar From t_selkvar ) " +
                        DBManager.Limit1
                        , true);
                    if (!ret.result)
                    {
                        DropCalcRevalTmp(conn_db);
                        return ret;
                    }

                    try
                    {
                        if (reader.Read())
                        {
                            //занесем месяц для пересчета
                            DateTime d = new DateTime(yy, mm, 1);
                            lcalc_months.Add(d);

                            b_portal_calc = false;
                        }
                    }
                    catch
                    {
                        reader.Close();
                        DropCalcRevalTmp(conn_db);
                        return ret;
                    }
                    reader.Close();
                }

                if (b_portal_calc && paramcalc.isPortal)
                {
                    //не попал в must_calc, тогда если это Порталовский расчет, то надо проверить еще наличие перерасчета в charge_cnts 
                    if (Portal_YesMustCalc(mustDat1, lcalc_months_portal))
                    {
                        DateTime d = new DateTime(yy, mm, 1);
                        lcalc_months.Add(d);
                    }
                }
            }
            return ret;
        }

        /// <summary>
        /// Получение списка месяцев для расчета
        /// </summary>
        /// <param name="paramcalc"></param>
        /// <returns></returns>
        private List<CalcMonth> CalcMonths(CalcTypes.ParamCalc paramcalc)
        {
            var ListMonthsForCalc = new List<CalcMonth>();

            for (int i = 0; i < Points.PointList.Count; i++)
            {
                //позиционируемся на список расчетных месяцев данного nzp_wp
                if (Points.PointList[i].nzp_wp != paramcalc.nzp_wp)
                    continue;

                for (int j = 0; j < Points.PointList[i].CalcMonths.Count; j++)
                {
                    //проверим, есть ли данный месяц среди перерасчитываемых данных
                    int yy = Points.PointList[i].CalcMonths[j].year_;
                    int mm = Points.PointList[i].CalcMonths[j].month_;

                    if (yy == 0 || mm == 0)
                        continue;

                    //отсечем перерасчеты будущего
                    if (yy > paramcalc.cur_yy)
                        continue;
                    if (yy == paramcalc.cur_yy && mm >= paramcalc.cur_mm)
                        continue;

                    //отсечем перерасчеты до даты начала расчетов
                    if (yy < Points.PointList[i].BeginCalc.year_)
                        continue;
                    if (yy == Points.PointList[i].BeginCalc.year_ && mm < Points.PointList[i].BeginCalc.month_)
                        continue;
                    //если все проверки пройдены включаем в список месяцев для расчета/перерасчтета
                    ListMonthsForCalc.Add(new CalcMonth(yy, mm));
                }
            }
            return ListMonthsForCalc;
        }

        #region Сохранение перерасчетных данных
        //-----------------------------------------------------------------------------
        void LoadDeltaReval(IDbConnection conn_db, CalcTypes.ParamCalc paramcalc, out Returns ret) //сравнить charge_xx и вычислить дельту reval_xx
        //-----------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            DropTempTablesCharge(conn_db);

            string s = paramcalc.pref + " " + paramcalc.nzp_kvar + "/" + paramcalc.nzp_dom + "/" +
                       paramcalc.calc_yy + "/" + paramcalc.calc_mm + "/" +
                       paramcalc.cur_yy + "/" + paramcalc.cur_mm;

            MonitorLog.WriteLog("Старт LoadReval: " + s, MonitorLog.typelog.Info, 1, 2, true);

            //выбрать перерасчетный chargeYYMM_xx - последний посчитанный charge_xx
            CalcTypes.ChargeXX reval_chargeXX = new CalcTypes.ChargeXX(paramcalc);

            //выбрать текущий charge_xx (без алиаса) - с чем будем сравнивать
            //получается, что calc_chargeXX.charge_xx_ishod == calc_chargeXX.charge_xx !!!
            paramcalc.b_reval = false;
            CalcTypes.ChargeXX calc_chargeXX = new CalcTypes.ChargeXX(paramcalc);


            string p_dat_charge = MDY(calc_chargeXX.paramcalc.cur_mm, 28, calc_chargeXX.paramcalc.cur_yy);

            //выбрать исходный charge_xx во временную таблицу
            string swhere = " ";
            if (paramcalc.nzp_kvar > 0 || paramcalc.nzp_dom > 0 || calc_chargeXX.paramcalc.list_dom)
                swhere = " and nzp_kvar in ( Select nzp_kvar From t_selkvar ) ";

            string ssql =
#if PG
 "Create temp table ishod_charge as " +
#endif
 " Select t.*, " + sNvlWord + "(t.dat_charge, " + MDY(1, 1, 1901) + ") as idat_charge " +
                " From " + calc_chargeXX.charge_xx_ishod + " t " +
                " Where t.nzp_serv > 1 " +
                " and not exists (select 1 from temp_prohibited_recalc pr " +
                  " where t.nzp_serv = pr.nzp_serv and t.nzp_supp = pr.nzp_supp and pr.nzp_kvar = t.nzp_kvar)" +
                  " and NOT EXISTS (SELECT 1 FROM " + Points.Pref + sKernelAliasRest + "peni_settings s WHERE s.nzp_peni_serv=t.nzp_serv) " + //пени не пересчитываем
                swhere +
#if PG
 " ";
#else
                " Into temp ishod_charge With no log ";
#endif

            ret = ExecSQL(conn_db, ssql, true);
            if (!ret.result) { DropTempTablesCharge(conn_db); return; }

#if PG
            ExecSQL(conn_db, " Create index ix0_ishod_charge on ishod_charge (nzp_charge) ", true);
            ExecSQL(conn_db, " Create index ix1_ishod_charge on ishod_charge (nzp_kvar,nzp_serv,nzp_supp, idat_charge) ", true);
            ExecSQL(conn_db, " Create index ix2_ishod_charge on ishod_charge (nzp_kvar,num_ls,nzp_serv,nzp_supp) ", true);
            ExecSQL(conn_db, sUpdStat + " ishod_charge ", true);
#else
            ExecSQL(conn_db, " Create index ix0_ishod_charge on ishod_charge (nzp_charge) ", true);
            ExecSQL(conn_db, " Create index ix1_ishod_charge on ishod_charge (nzp_kvar,nzp_serv,nzp_supp, idat_charge) ", true);
            ExecSQL(conn_db, " Create index ix2_ishod_charge on ishod_charge (nzp_kvar,num_ls,nzp_serv,nzp_supp) ", true);
            ExecSQL(conn_db, sUpdStat + " ishod_charge ", true);
#endif

            ssql =
                " Select * " +
#if PG
 " Into temp reval_charge " +
#else
                " " +
#endif
 " From " + reval_chargeXX.charge_xx + " ch "+
                " Where nzp_serv > 1 and NOT EXISTS (SELECT 1 FROM " + Points.Pref + sKernelAliasRest + "peni_settings s" +
                "                        WHERE s.nzp_peni_serv=ch.nzp_serv) " + //пени не пересчитываем
                swhere +
#if PG
 " ";
#else
                " Into temp reval_charge With no log ";
#endif

            ret = ExecSQL(conn_db, ssql, true);
            if (!ret.result) { DropTempTablesCharge(conn_db); return; }

            ExecSQL(conn_db, " Create index ix1_reval_charge on reval_charge (nzp_kvar,nzp_serv,nzp_supp) ", true);
            ExecSQL(conn_db, " Create index ix2_reval_charge on reval_charge (nzp_charge) ", true);
            ExecSQL(conn_db, sUpdStat + " reval_charge ", true);

            //Анэс попросил убрать врэмэнно!! отмена связной недопоставки
            int is_svaz_nedo = 0;

            IDbCommand cmd = DBManager.newDbCommand(
                      " Select count(*) From " + paramcalc.data_alias + "prm_5 Where nzp_prm = 1998 and is_actual<>100" +
                      " and dat_s  <= " + MDY(paramcalc.cur_mm, 1, paramcalc.cur_yy) +
                      " and dat_po >= " + MDY(paramcalc.cur_mm, 1, paramcalc.cur_yy)
                      , conn_db);

            try
            {
                string scountvals = Convert.ToString(cmd.ExecuteScalar());
                is_svaz_nedo = Convert.ToInt32(scountvals);
            }
            catch
            {
                is_svaz_nedo = 0;
            }

            string uni_serv = "";
            if (is_svaz_nedo > 0)
            {
                uni_serv = "  and " +
                    " ( " +
                        " ( a.nzp_serv > 1 and m.nzp_serv in (1) ) " +
                    " or " +
                        " ( a.nzp_serv = m.nzp_serv ) " +
                    " ) ";
            }
            else
            {
                uni_serv = "  and " +
                    " ( " +
                        " ( a.nzp_serv > 1 and m.nzp_serv in (1) ) " +
                    " or " +
                        " ( a.nzp_serv = m.nzp_serv ) " +
                    " or " +
                        " ( a.nzp_serv in (23,270,210,410) and m.nzp_serv in (25) ) " +
                    " or " +
                        " ( a.nzp_serv = 7 and m.nzp_serv in (9,6) ) " +
                    " or " +
                        " ( a.nzp_serv in (6,14) and m.nzp_serv in (9) ) " +

                    " or (exists (select 1 from " + Points.Pref + "_kernel.serv_norm_koef d " +
                        " Where a.nzp_serv=d.nzp_serv and m.nzp_serv=d.nzp_serv_link )) " +

                    " ) ";
            }


            //выбрать текущие данные (последний посчитанный charge)
            ret = ExecSQL(conn_db,
                " create temp table t_ins_ch (" +
                " nzp_kvar integer not null," +
                " num_ls   integer not null," +
                " nzp_supp integer not null," +
                " nzp_serv integer not null, " +
                " nzp_key  integer not null default 0," +
                " dat_charge date not null " +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesCharge(conn_db); return; }


            ssql =
                " insert into t_ins_ch (nzp_kvar,num_ls,nzp_supp,nzp_serv, dat_charge ) " +
                " Select a.nzp_kvar,a.num_ls,a.nzp_supp,a.nzp_serv, max( idat_charge ) as dat_charge " +
                " From ishod_charge a, t_mustcalc m " +
                " Where a.nzp_kvar = m.nzp_kvar " +
                "   and a.nzp_serv > 1 " +
                uni_serv +
                "   and idat_charge < " + p_dat_charge +
                " Group by 1,2,3,4 ";
            ret = ExecSQL(conn_db, ssql, true);
            if (!ret.result) { DropTempTablesCharge(conn_db); return; }

            ExecSQL(conn_db, " Create unique index ix1_t_ins_ch on t_ins_ch (nzp_kvar,nzp_serv,nzp_supp, dat_charge) ", true);
            ExecSQL(conn_db, sUpdStat + " t_ins_ch ", true);

            //и проставить ссылки на nzp_charge

            ret = ExecSQL(conn_db,
                " Update t_ins_ch " +
                " Set nzp_key = ( Select max(nzp_charge) From ishod_charge ch " +
                                " Where t_ins_ch.nzp_kvar = ch.nzp_kvar " +
                                "   and t_ins_ch.nzp_serv = ch.nzp_serv " +
                                "   and t_ins_ch.nzp_supp = ch.nzp_supp " +
                                "   and t_ins_ch.dat_charge = ch.idat_charge ) " +
                " Where exists  ( Select 1 From ishod_charge ch " +
                                " Where t_ins_ch.nzp_kvar = ch.nzp_kvar " +
                                "   and t_ins_ch.nzp_serv = ch.nzp_serv " +
                                "   and t_ins_ch.nzp_supp = ch.nzp_supp " +
                                "   and t_ins_ch.dat_charge = ch.idat_charge ) "
                , true);
            if (!ret.result) { DropTempTablesCharge(conn_db); return; }

            ExecSQL(conn_db, " Create index ix2_t_ins_ch on t_ins_ch (nzp_key) ", true);
            ExecSQL(conn_db, sUpdStat + " t_ins_ch ", true);

            //выбираем суммы для сравнения

            ExecSQL(conn_db, " Drop table t_prev_ch ", false);

            ret = ExecSQL(conn_db,
                " Create temp table t_prev_ch " +
                "  ( nzp_key    serial not null, " +
                "    nzp_kvar   integer, " +
                "    num_ls     integer, " +
                "    nzp_supp   integer, " +
                "    nzp_serv   integer, " +
                "    dat_charge date not null," +
                "    nzp_frm    integer default 0, " +
                "    tarif       " + sDecimalType + "(14,3) default 0.000, " +
                "    sum_tarif   " + sDecimalType + "(14,2) default 0.00, " +
                "    tarif_f     " + sDecimalType + "(14,3) default 0.000, " +
                "    sum_tarif_f " + sDecimalType + "(14,2) default 0.00, " +
                "    sum_tarif_sn_eot " + sDecimalType + "(14,2) default 0.00, " +
                "    sum_tarif_sn_f   " + sDecimalType + "(14,2) default 0.00, " +
                "    c_calc    " + sDecimalType + "(14,4) default 0.00, " +
                "    sum_lgota " + sDecimalType + "(14,2) default 0.00, " +
                "    sum_nedop " + sDecimalType + "(14,2) default 0.00  " +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesCharge(conn_db); return; }

            ret = ExecSQL(conn_db,
                " insert into t_prev_ch (nzp_kvar,num_ls,nzp_supp,nzp_serv," +
                    " dat_charge,nzp_frm,tarif,sum_tarif,tarif_f,sum_tarif_f,sum_tarif_sn_eot,sum_tarif_sn_f,c_calc,sum_lgota,sum_nedop) " +
                " Select a.nzp_kvar,a.num_ls,a.nzp_supp,a.nzp_serv, " +
                    " max(ch.dat_charge) as dat_charge, " +
                    " max(nzp_frm) as nzp_frm, " +
                    " max(tarif) as tarif, " +
                    " max(sum_tarif) as sum_tarif, " +
                    " max(tarif_f) as tarif_f, " +
                    " max(sum_tarif_f) as sum_tarif_f, " +
                    " max(sum_tarif_sn_eot) as sum_tarif_sn_eot, " +
                    " max(sum_tarif_sn_f) as sum_tarif_sn_f, " +
                    " max(c_calc) as c_calc, " +
                    " max(sum_lgota) as sum_lgota, " +
                    " max(sum_nedop) as sum_nedop  " +     //" max(sum_dlt_tarif) as sum_dlt_tarif " +
                " From ishod_charge a, t_ins_ch ch " +
                " Where a.nzp_charge = ch.nzp_key " +
                " Group by 1,2,3,4 "
                , true);
            if (!ret.result) { DropTempTablesCharge(conn_db); return; }

            ExecSQL(conn_db, " Create unique index ix1_t_prev_ch on t_prev_ch (nzp_kvar,nzp_serv,nzp_supp) ", true);
            ExecSQL(conn_db, sUpdStat + " t_prev_ch ", true);

            //выбрать ключи перерасчитанных данных из reval_chargeXX, чтобы затем быстрее соединяться
            ret = ExecSQL(conn_db,
                " Create temp table t_ins_ch1 ( nzp_charge integer ) " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesCharge(conn_db); return; }

            ret = ExecSQL(conn_db,
                " insert into t_ins_ch1 (nzp_charge) " +
                " Select " + sUniqueWord + " a.nzp_charge " +
                " From reval_charge a, t_mustcalc m " +
                " Where a.nzp_kvar = m.nzp_kvar " +
                "   and a.nzp_serv > 1 " +
                uni_serv + " "
                , true);
            if (!ret.result) { DropTempTablesCharge(conn_db); return; }

            ExecSQL(conn_db, " Create unique index ix1_t_ins_ch1 on t_ins_ch1 (nzp_charge) ", true);
            ExecSQL(conn_db, sUpdStat + " t_ins_ch1 ", true);

            //+++++++++++++++++++++++++++++++++++++++++++++++
            //начинаем сравнивать и заполнять поле order_print в reval_chargeXX
            // 1 - есть дельта (_p)
            // 2 - новая строка в reval_chargeXX (добавили задним числом услугу)
            // 3 - надо снять строку в calc_chargeXX (сняли задним числом услугу)
            //+++++++++++++++++++++++++++++++++++++++++++++++

            // ... nzp_prm=1991 -> запретить перерасчет ОДН - можно учитывать ОДН только в текущем расчетном месяце
            bool bIsNotCalcODN =
              (CheckValBoolPrmWithVal(conn_db, calc_chargeXX.paramcalc.data_alias, 1991, "5", "1", calc_chargeXX.paramcalc.dat_s, calc_chargeXX.paramcalc.dat_po));
            string sNotODNServs = "";
            if (bIsNotCalcODN)
            {
                sNotODNServs = " and not exists (select 1 from " + calc_chargeXX.paramcalc.kernel_alias + "serv_odn s where s.nzp_serv=a.nzp_serv) " +
                                "and not exists (select 1 from " + calc_chargeXX.paramcalc.kernel_alias + "serv_odn s, " + Points.Pref + sKernelAliasRest + "serv_norm_koef k " +
                                "where k.nzp_serv_link=s.nzp_serv and k.nzp_serv=a.nzp_serv)";
            }

            ret = ExecSQL(conn_db,
                " Create temp table t_itog " +
                "  ( nzp_key    serial not null, " +
                "    nzp_charge integer not null, " +
                "    nzp_dom    integer not null default 0, " +
                "    nzp_kvar   integer not null, " +
                "    num_ls     integer not null, " +
                "    nzp_supp   integer not null, " +
                "    nzp_serv   integer not null, " +
                "    nzp_frm    integer default 0, " +
                "    nzp_frm_p  integer default 0, " +
                "    tarif          " + sDecimalType + "(14,3) default 0.000, " +
                "    tarif_p        " + sDecimalType + "(14,3) default 0.000, " +
                "    rsum_tarif     " + sDecimalType + "(14,2) default 0.000, " +
                "    rsum_lgota     " + sDecimalType + "(14,2) default 0.00, " +
                "    sum_tarif      " + sDecimalType + "(14,2) default 0.00, " +
                "    sum_dlt_tarif  " + sDecimalType + "(14,2) default 0.00, " +
                "    sum_dlt_tarif_p " + sDecimalType + "(14,2) default 0.00, " +
                "    sum_tarif_p     " + sDecimalType + "(14,2) default 0.00, " +
                "    sum_lgota       " + sDecimalType + "(14,2) default 0.00, " +
                "    sum_dlt_lgota   " + sDecimalType + "(14,2) default 0.00, " +
                "    sum_dlt_lgota_p " + sDecimalType + "(14,2) default 0.00, " +
                "    sum_lgota_p    " + sDecimalType + "(14,2) default 0.00, " +
                "    sum_nedop      " + sDecimalType + "(14,2) default 0.00, " +
                "    sum_nedop_p    " + sDecimalType + "(14,2) default 0.00, " +
                "    sum_real       " + sDecimalType + "(14,2) default 0.00, " +
                "    sum_charge     " + sDecimalType + "(14,2) default 0.00, " +
                "    reval          " + sDecimalType + "(14,2) default 0.00, " +
                "    real_pere      " + sDecimalType + "(14,2) default 0.00, " +
                "    sum_pere       " + sDecimalType + "(14,2) default 0.00, " +
                "    real_charge    " + sDecimalType + "(14,2) default 0.00, " +
                "    sum_money      " + sDecimalType + "(14,2) default 0.00, " +
                "    money_to       " + sDecimalType + "(14,2) default 0.00, " +
                "    money_from     " + sDecimalType + "(14,2) default 0.00, " +
                "    money_del      " + sDecimalType + "(14,2) default 0.00, " +
                "    sum_fakt       " + sDecimalType + "(14,2) default 0.00, " +
                "    fakt_to        " + sDecimalType + "(14,2) default 0.00, " +
                "    fakt_from      " + sDecimalType + "(14,2) default 0.00, " +
                "    fakt_del       " + sDecimalType + "(14,2) default 0.00, " +
                "    sum_insaldo    " + sDecimalType + "(14,2) default 0.00, " +
                "    izm_saldo         " + sDecimalType + "(14,2) default 0.00, " +
                "    sum_outsaldo      " + sDecimalType + "(14,2) default 0.00, " +
                "    sum_subsidy       " + sDecimalType + "(14,2) default 0.00, " +
                "    sum_subsidy_p     " + sDecimalType + "(14,2) default 0.00, " +
                "    sum_subsidy_reval " + sDecimalType + "(14,2) default 0.00, " +
                "    sum_subsidy_all   " + sDecimalType + "(14,2) default 0.00, " +
                "    tarif_f           " + sDecimalType + "(14,3) default 0.000, " +
                "    tarif_f_p         " + sDecimalType + "(14,3) default 0.000, " +
                "    sum_tarif_f        " + sDecimalType + "(14,2) default 0.00, " +
                "    sum_tarif_f_p      " + sDecimalType + "(14,2) default 0.00, " +
                "    sum_tarif_sn_eot   " + sDecimalType + "(14,2) default 0.00, " +
                "    sum_tarif_sn_eot_p " + sDecimalType + "(14,2) default 0.00, " +
                "    sum_tarif_sn_f     " + sDecimalType + "(14,2) default 0.00, " +
                "    sum_tarif_sn_f_p   " + sDecimalType + "(14,2) default 0.00, " +
                "    isblocked      integer default 0, " +
                "    is_device      integer default 0, " +
                "    c_calc         " + sDecimalType + "(17,7) default 0.00, " +
                "    c_calcm_p      " + sDecimalType + "(17,7) default 0.00, " +
                "    c_calc_p       " + sDecimalType + "(17,7) default 0.00, " +
                "    c_sn           " + sDecimalType + "(17,7) default 0.00, " +
                "    c_okaz         " + sDecimalType + "(14,2) default 0.00, " +
                "    c_nedop        " + sDecimalType + "(14,2) default 0.00, " +
                "    isdel          integer default 0, " +
                "    c_reval        " + sDecimalType + "(17,7) default 0.00, " +
                "    order_print    integer default 0, " +
                "    is_use         integer default 0, " +
                "    is_calc_gku    integer default 0, " +
                "    nzp_prm_rashod integer default 0, " +
                "    nzp_mea   integer default 0, " +
                "    nzp_mea_p integer default 0, " +
                "    reval_rsh " + sDecimalType + "(15,7) default 0.00, " +
                "    valm      " + sDecimalType + "(15,7) default 0.00, " +
                "    valm_p    " + sDecimalType + "(15,7) default 0.00, " +
                "    dop87     " + sDecimalType + "(15,7) default 0.00, " +
                "    dop87_p   " + sDecimalType + "(15,7) default 0.00, " +
                "    rsh1      " + sDecimalType + "(15,7) default 0.00, " +
                "    rsh1_p    " + sDecimalType + "(15,7) default 0.00, " +
                "    rsh2      " + sDecimalType + "(15,7) default 0.00, " +
                "    rsh2_p    " + sDecimalType + "(15,7) default 0.00, " +
                "    month_p   integer default 0, " +
                "    year_p    integer default 0  " +
                " )  " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesCharge(conn_db); return; }

            // 1 - есть дельты

            ssql =
                " Insert into t_itog " +
                "  ( nzp_charge,nzp_kvar,num_ls,nzp_supp,nzp_serv,nzp_frm,nzp_frm_p, " +
                "    reval,tarif,sum_tarif, sum_lgota,sum_nedop,tarif_p,sum_tarif_p,sum_lgota_p, sum_nedop_p, " +
                "    tarif_f, tarif_f_p, sum_tarif_f, sum_tarif_f_p, sum_tarif_sn_eot, sum_tarif_sn_eot_p, sum_tarif_sn_f, sum_tarif_sn_f_p, " +
                "    rsum_tarif, rsum_lgota, sum_dlt_tarif, sum_dlt_tarif_p, sum_dlt_lgota, sum_dlt_lgota_p,  sum_real, sum_charge, real_pere, sum_pere, " +
                "    real_charge, sum_money, money_to, money_from, money_del, sum_fakt, fakt_to, fakt_from, fakt_del, sum_insaldo, izm_saldo, " +
                "    sum_outsaldo, sum_subsidy, sum_subsidy_p, sum_subsidy_reval, sum_subsidy_all, isblocked, is_device, c_calc, c_calc_p, c_sn, c_okaz, " +
                "    c_nedop, isdel, c_reval, order_print, month_p, year_p ) " +
                " Select a.nzp_charge, a.nzp_kvar,a.num_ls,a.nzp_supp,a.nzp_serv,a.nzp_frm,b.nzp_frm, " +
                "(a.sum_tarif-b.sum_tarif + a.sum_lgota-b.sum_lgota) as reval, " +
                " a.tarif, " +
                " a.sum_tarif, " +
                " a.sum_lgota, " +
                " a.sum_nedop, " +
                " b.tarif as tarif_p, " +
                " b.sum_tarif as sum_tarif_p, " +
                " b.sum_lgota as sum_lgota_p, " +
                " b.sum_nedop as sum_nedop_p, " +
                " a.tarif_f, " +
                " b.tarif_f tarif_f_p, " +
                " a.sum_tarif_f, " +
                " b.sum_tarif_f sum_tarif_f_p, " +
                " a.sum_tarif_sn_eot, " +
                " b.sum_tarif_sn_eot sum_tarif_sn_eot_p, " +
                " a.sum_tarif_sn_f, " +
                " b.sum_tarif_sn_f sum_tarif_sn_f_p, " +
                "   a.rsum_tarif, a.rsum_lgota, a.sum_dlt_tarif, a.sum_dlt_tarif_p, a.sum_dlt_lgota, a.sum_dlt_lgota_p,  a.sum_real, a.sum_charge, a.real_pere, a.sum_pere, " +
                "   a.real_charge, a.sum_money, a.money_to, a.money_from, a.money_del, a.sum_fakt, a.fakt_to, a.fakt_from, a.fakt_del, 0 sum_insaldo, 0 izm_saldo, " +
                "   0 sum_outsaldo, a.sum_subsidy, a.sum_subsidy_p, a.sum_subsidy_reval, a.sum_subsidy_all, a.isblocked, a.is_device, a.c_calc, b.c_calc, a.c_sn, a.c_okaz, " +
                "   a.c_nedop, a.isdel, a.c_reval, a.order_print," +
                DBManager.sMonthFromDate + "b.dat_charge)" + sConvToInt + "," +
                DBManager.sYearFromDate + " b.dat_charge)" + sConvToInt +
                " From reval_charge a, t_prev_ch b " +
                " Where a.nzp_kvar = b.nzp_kvar " +
                "   and a.nzp_serv = b.nzp_serv " +
                "   and a.nzp_supp = b.nzp_supp " +
                "   and ( abs(a.tarif-b.tarif)>0.0001 or abs(a.sum_tarif-b.sum_tarif)>0.0001 or abs(a.sum_tarif_f-b.sum_tarif_f)>0.0001 or " +
                " abs(a.sum_lgota-b.sum_lgota)>0.0001 or abs(a.sum_nedop-b.sum_nedop)>0.0001 )" + sNotODNServs;

            ret = ExecSQL(conn_db, ssql, true, 6000);
            if (!ret.result) { DropTempTablesCharge(conn_db); return; }

            ExecSQL(conn_db, sUpdStat + " t_itog ", true);

            // 2 - новая строка в reval_chargeXX (строки нет в calc_charge, т.е. t_prev_ch)

            ssql =
                " Insert into t_itog " +
                "  ( nzp_charge,nzp_kvar,num_ls,nzp_supp,nzp_serv,nzp_frm,nzp_frm_p, " +
                "    reval,tarif,sum_tarif, sum_lgota,sum_nedop,tarif_p, sum_tarif_p,sum_lgota_p, sum_nedop_p, " +
                "    tarif_f, tarif_f_p, sum_tarif_f, sum_tarif_f_p, sum_tarif_sn_eot, sum_tarif_sn_eot_p, sum_tarif_sn_f, sum_tarif_sn_f_p, " +
                "    rsum_tarif, rsum_lgota, sum_dlt_tarif, sum_dlt_tarif_p, sum_dlt_lgota, sum_dlt_lgota_p,  sum_real, sum_charge, real_pere, sum_pere, " +
                "    real_charge, sum_money, money_to, money_from, money_del, sum_fakt, fakt_to, fakt_from, fakt_del, sum_insaldo, izm_saldo, " +
                "    sum_outsaldo, sum_subsidy, sum_subsidy_p, sum_subsidy_reval, sum_subsidy_all, isblocked, is_device, c_calc, c_calc_p, c_sn, c_okaz, " +
                "    c_nedop, isdel, c_reval, order_print, month_p, year_p ) " +
                " Select a.nzp_charge, a.nzp_kvar,a.num_ls,a.nzp_supp,a.nzp_serv,a.nzp_frm,0 nzp_frm_p, " +
                "(a.sum_tarif + a.sum_lgota) as reval, " +
                " a.tarif, " +
                " a.sum_tarif, " +
                " a.sum_lgota, " +
                " a.sum_nedop, " +
                " 0.00 as tarif_p, " +
                " 0.00 as sum_tarif_p, " +
                " 0.00 as sum_lgota_p, " +
                " 0.00 as sum_nedop_p, " +
                " a.tarif_f, " +
                " 0.00 tarif_f_p, " +
                " a.sum_tarif_f, " +
                " 0.00 sum_tarif_f_p, " +
                " a.sum_tarif_sn_eot, " +
                " 0.00 sum_tarif_sn_eot_p, " +
                " a.sum_tarif_sn_f, " +
                " 0.00 sum_tarif_sn_f_p, " +
                "   a.rsum_tarif, a.rsum_lgota, a.sum_dlt_tarif, a.sum_dlt_tarif_p, a.sum_dlt_lgota, a.sum_dlt_lgota_p,  a.sum_real, a.sum_charge, a.real_pere, a.sum_pere, " +
                "   a.real_charge, a.sum_money, a.money_to, a.money_from, a.money_del, a.sum_fakt, a.fakt_to, a.fakt_from, a.fakt_del, 0 sum_insaldo, 0 izm_saldo, " +
                "   0 sum_outsaldo, a.sum_subsidy, a.sum_subsidy_p, a.sum_subsidy_reval, a.sum_subsidy_all, a.isblocked, a.is_device, a.c_calc,0 c_calc_p, a.c_sn, a.c_okaz, " +
                "   a.c_nedop, a.isdel, a.c_reval, a.order_print, 1, 1901 " +
                " From reval_charge a, t_ins_ch1 t " +
                " Where a.nzp_charge = t.nzp_charge " +
                "   and not exists ( Select 1 From t_prev_ch b " +
                " Where a.nzp_kvar = b.nzp_kvar " +
                "   and a.nzp_serv = b.nzp_serv " +
                "   and a.nzp_supp = b.nzp_supp " +
                "   and ( abs(a.tarif)>0.0001 or abs(a.sum_tarif)>0.0001 or abs(a.sum_lgota)>0.0001 or abs(a.sum_nedop)>0.0001 ) " +
                ")" + sNotODNServs;

            ret = ExecSQL(conn_db, ssql, true, 6000);
            if (!ret.result) { DropTempTablesCharge(conn_db); return; }

            ExecSQL(conn_db, sUpdStat + " t_itog ", true);

            // ... для расчета дотаций ...
            //" from " + reval_chargeXX.paramcalc.pref + "_data:prm_5 p where p.nzp_prm=1992 and p.val_prm='1' " +
            bool bIsCalcSubsidy = Points.IsCalcSubsidy;

            // формула расчета перерасчетного начисления
            string sqql = " (-1) * (a.sum_tarif - a.sum_lgota) ";
            if (bIsCalcSubsidy)
            {
                sqql = " (-1) * (a.sum_tarif - a.sum_tarif_f - a.sum_lgota) ";
            }

            // 3 - надо удалить строку в calc_chargeXX (строки нет в reval_charge)

            ret = ExecSQL(conn_db,
                            " Insert into t_itog " +
                            " ( nzp_charge,nzp_kvar,num_ls,nzp_supp,nzp_serv,nzp_frm,nzp_frm_p, " +
                            "   reval,tarif,sum_tarif, sum_lgota,sum_nedop, tarif_p, sum_tarif_p,sum_lgota_p, sum_nedop_p, " +
                            "   tarif_f, tarif_f_p, sum_tarif_f, sum_tarif_f_p, sum_tarif_sn_eot, sum_tarif_sn_eot_p, " +
                            "   sum_tarif_sn_f, sum_tarif_sn_f_p, c_calc, c_calc_p, month_p, year_p ) " +
                            " Select 0, a.nzp_kvar,a.num_ls,a.nzp_supp,a.nzp_serv, 0,a.nzp_frm,  " +
                                sqql + " as reval, " +
                                " 0," + //" a.tarif, " +
                                " 0," + //" (-1) * a.sum_tarif, " +
                                " 0," + //" (-1) * a.sum_lgota, " +
                                " 0," + //" (-1) * a.sum_nedop, " +
                                " a.tarif as tarif_p, " +
                                " a.sum_tarif as sum_tarif_p, " +
                                " a.sum_lgota as sum_lgota_p, " +
                                " a.sum_nedop as sum_nedop_p, " +
                                " 0," + //" a.tarif_f, " +
                                " a.tarif_f tarif_f_p, " +
                                " 0," + //" (-1) * a.sum_tarif_f, " +
                                " a.sum_tarif_f sum_tarif_f_p, " +
                                " 0," + //" (-1) * a.sum_tarif_sn_eot, " +
                                " a.sum_tarif_sn_eot sum_tarif_sn_eot_p, " +
                                " 0," + //" (-1) * a.sum_tarif_sn_f, " +
                                " a.sum_tarif_sn_f sum_tarif_sn_f_p," +
                                " 0 c_calc, a.c_calc, " +
                                DBManager.sMonthFromDate + "a.dat_charge)" + sConvToInt + "," +
                                DBManager.sYearFromDate + " a.dat_charge)" + sConvToInt +
                            " From t_prev_ch a " +
                " Where not exists ( Select 1 From reval_charge b " +
                                        " Where a.nzp_kvar = b.nzp_kvar " +
                                        "   and a.nzp_serv = b.nzp_serv " +
                            "   and a.nzp_supp = b.nzp_supp ) " + sNotODNServs
                            , true);
            if (!ret.result) { DropTempTablesCharge(conn_db); return; }


            ExecSQL(conn_db, " Create unique index ix0_t_itog on t_itog (nzp_key) ", true);
            ExecSQL(conn_db, " Create index ix1_t_itog on t_itog (nzp_kvar,nzp_serv,nzp_supp) ", true);
            ExecSQL(conn_db, " Create index ix2_t_itog on t_itog (nzp_charge) ", true);
            ExecSQL(conn_db, " Create index ix3_t_itog on t_itog (nzp_frm) ", true);
            ExecSQL(conn_db, sUpdStat + " t_itog ", true);

            //вставить измененные суммы
            string sql =
                " Insert into " + calc_chargeXX.charge_xx +
                " ( dat_charge, nzp_kvar, num_ls, nzp_serv, nzp_supp, nzp_frm, " +
                     " tarif, tarif_p, sum_tarif, sum_tarif_p, sum_lgota, sum_lgota_p, sum_nedop, sum_nedop_p, reval, " +
                     " tarif_f, tarif_f_p, sum_tarif_f, sum_tarif_f_p, sum_tarif_sn_eot, sum_tarif_sn_eot_p, sum_tarif_sn_f, sum_tarif_sn_f_p, " +
                     " rsum_tarif, rsum_lgota, sum_dlt_tarif, sum_dlt_tarif_p, sum_dlt_lgota, sum_dlt_lgota_p,  sum_real, sum_charge, real_pere, sum_pere, " +
                     " real_charge, sum_money, money_to, money_from, money_del, sum_fakt, fakt_to, fakt_from, fakt_del, sum_insaldo, izm_saldo, " +
                     " sum_outsaldo, sum_subsidy, sum_subsidy_p, sum_subsidy_reval, sum_subsidy_all, isblocked, is_device, c_calc, c_sn, c_okaz, " +
                     " c_nedop, isdel, c_reval, order_print) " +
                " Select " + p_dat_charge + ", nzp_kvar,num_ls, nzp_serv, nzp_supp, nzp_frm, " +
                     " tarif, tarif_p, sum_tarif, sum_tarif_p, sum_lgota, sum_lgota_p, sum_nedop, sum_nedop_p, reval, " +
                     " tarif_f, tarif_f_p, sum_tarif_f, sum_tarif_f_p, sum_tarif_sn_eot, sum_tarif_sn_eot_p, sum_tarif_sn_f, sum_tarif_sn_f_p, " +
                     " rsum_tarif, rsum_lgota, sum_dlt_tarif, sum_dlt_tarif_p, sum_dlt_lgota, sum_dlt_lgota_p,  sum_real, sum_charge, real_pere, sum_pere, " +
                     " real_charge, sum_money, money_to, money_from, money_del, sum_fakt, fakt_to, fakt_from, fakt_del, sum_insaldo, izm_saldo, " +
                     " sum_outsaldo, sum_subsidy, sum_subsidy_p, sum_subsidy_reval, sum_subsidy_all, isblocked, is_device, c_calc, c_sn, c_okaz, " +
                     " c_nedop, isdel, c_reval, order_print " +
                " From t_itog Where 1=1 ";

            if (calc_chargeXX.paramcalc.nzp_kvar > 0 || calc_chargeXX.paramcalc.nzp_dom > 0)
                ret = ExecSQL(conn_db, sql, true);
            else
                //надо вставить в текущий charge, а не в calc_chargeXX.charge_xx_ishod, хотя в данном случае они совпали charge_xx = charge_xx_ishod!!! Прикол!
                ExecByStep(conn_db, "t_itog", "nzp_key", sql, 50000, " ", out ret);
            if (!ret.result) { DropTempTablesCharge(conn_db); return; }

            #region установить данные для формирования дельты для коммунальных услуг

            // дельты расходов применять для коммунальных услуг
            ret = ExecSQL(conn_db,
                " Update t_itog " +
                " Set is_use=1 " +
                " Where exists (select 1 from " + paramcalc.kernel_alias + "s_counts s where s.nzp_serv=t_itog.nzp_serv) "
                , true);
            if (!ret.result) { DropTempTablesCharge(conn_db); return; }

            // дельты расходов применять для услуг на ОДН
            ret = ExecSQL(conn_db,
                " Update t_itog " +
                " Set is_use=2 " +
                " Where exists (select 1 from " + paramcalc.kernel_alias + "serv_odn s where s.nzp_serv=t_itog.nzp_serv) "
                , true);
            if (!ret.result) { DropTempTablesCharge(conn_db); return; }

            ExecSQL(conn_db, " Create index ix4_t_itog on t_itog (is_use) ", true);
            ExecSQL(conn_db, sUpdStat + " t_itog ", true);

            // проставить дом для возможности выборки тарифа по ЛС дома для дельты расходов, если тарифа в прошлом нет
            ret = ExecSQL(conn_db,
                " Update t_itog Set" +
                " nzp_dom=(select k.nzp_dom from " + paramcalc.data_alias + "kvar k where k.nzp_kvar=t_itog.nzp_kvar) " +
                " Where is_use>0 "
                , true);
            if (!ret.result) { DropTempTablesCharge(conn_db); return; }

            // проставить единицы измерения текущего расчета
            ret = ExecSQL(conn_db,
                " Update t_itog Set" +
                " nzp_mea=(select f.nzp_measure from " + paramcalc.kernel_alias + "formuls f where f.nzp_frm=t_itog.nzp_frm) " +
                " Where is_use>0 and exists (select 1 from " + paramcalc.kernel_alias + "formuls f where f.nzp_frm=t_itog.nzp_frm) "
                , true);
            if (!ret.result) { DropTempTablesCharge(conn_db); return; }

            // проставить единицы измерения прошлого расчета
            ret = ExecSQL(conn_db,
                " Update t_itog Set" +
                " nzp_mea_p=(select f.nzp_measure from " + paramcalc.kernel_alias + "formuls f where f.nzp_frm=t_itog.nzp_frm_p) " +
                " Where is_use>0 and exists (select 1 from " + paramcalc.kernel_alias + "formuls f where f.nzp_frm=t_itog.nzp_frm_p) "
                , true);
            if (!ret.result) { DropTempTablesCharge(conn_db); return; }

            #endregion установить данные для формирования дельты для коммунальных услуг

            #region вычислить объемы / расходы при нехватке информации - начисления м.б. были загружены!

            // проставить старый тариф, если его нет, а начисление есть (м.б. так загружено?)
            ret = ExecSQL(conn_db,
                " Update t_itog Set " +
                " isblocked=2, nzp_mea_p=nzp_mea, " +
                " tarif_p  = case when abs(sum_tarif_p)>0.001 then tarif else 0 end, " +
                " c_calc_p = case when abs(sum_tarif_p)>0.001 then sum_tarif_p / tarif else 0 end " +
                " Where is_use>0 and abs(tarif)>0.001 and abs(tarif_p)<0.001 "
                , true);
            if (!ret.result) { DropTempTablesCharge(conn_db); return; }

            // проставить прошлый расход и формулу для загруженных начислений: nzp_mea_p=0
            ret = ExecSQL(conn_db,
                " Update t_itog Set " +
                " isblocked=3, nzp_mea_p=nzp_mea, " +
                " c_calc_p = sum_tarif_p / tarif_p " +
                " Where is_use>0 and nzp_mea_p=0 and abs(sum_tarif_p)>0.001 and abs(tarif_p)>=0.001 "
                , true);
            if (!ret.result) { DropTempTablesCharge(conn_db); return; }

            ExecSQL(conn_db, " drop table t_dom_tarifs ", false);
            ret = ExecSQL(conn_db,
                " Create temp table t_dom_tarifs" +
                " ( nzp_dom  integer not null, " +
                "   nzp_serv integer not null, " +
                "   nzp_mea  integer not null, " +
                "   tarif       " + sDecimalType + "(15,7) default 0.00 " +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesCharge(conn_db); return; }

            ret = ExecSQL(conn_db,
                " insert into t_dom_tarifs (nzp_dom,nzp_serv,nzp_mea,tarif) " +
                " select b.nzp_dom,b.nzp_serv,max(f.nzp_measure),max(b.tarif) " +
                " from " + reval_chargeXX.calc_gku_xx + " b, " + paramcalc.kernel_alias + "formuls f, t_itog i" +
                " where f.nzp_frm=b.nzp_frm and b.stek = 3 and abs(b.tarif) > 0.0001 " +
                " and i.nzp_dom=b.nzp_dom and i.nzp_serv=b.nzp_serv and i.is_use>0 and abs(i.sum_tarif_p)>0.001 and abs(i.tarif_p)<0.001 " +
                " group by 1,2 "
                , true);
            if (!ret.result) { DropTempTablesCharge(conn_db); return; }

            ret = ExecSQL(conn_db, " create index ix_t_dom_tarifs on t_dom_tarifs(nzp_dom,nzp_serv) ", true);
            if (!ret.result) { DropTempTablesCharge(conn_db); return; }
            ret = ExecSQL(conn_db, sUpdStat + " t_dom_tarifs ", true);
            if (!ret.result) { DropTempTablesCharge(conn_db); return; }

            ret = ExecSQL(conn_db,
                " Update t_itog Set isblocked=4, " +
                " nzp_mea_p=(select f.nzp_mea from t_dom_tarifs f where f.nzp_dom=t_itog.nzp_dom and f.nzp_serv=t_itog.nzp_serv)," +
                " tarif_p  =(select f.tarif   from t_dom_tarifs f where f.nzp_dom=t_itog.nzp_dom and f.nzp_serv=t_itog.nzp_serv) " +
                " Where is_use>0 and exists (select 1 from t_dom_tarifs f where f.nzp_dom=t_itog.nzp_dom and f.nzp_serv=t_itog.nzp_serv) " +
                " and abs(sum_tarif_p)>0.001 and abs(tarif_p)<0.001 "
                , true);
            if (!ret.result) { DropTempTablesCharge(conn_db); return; }

            ret = ExecSQL(conn_db,
                " Update t_itog " +
                " Set c_calc_p = sum_tarif_p / tarif_p " +
                " Where is_use>0 and isblocked=4 "
                , true);
            if (!ret.result) { DropTempTablesCharge(conn_db); return; }

            ret = ExecSQL(conn_db,
                " Update t_itog Set isblocked=5, " +
                " nzp_mea_p=(select f.nzp_mea from t_dom_tarifs f where f.nzp_dom=t_itog.nzp_dom and f.nzp_serv=t_itog.nzp_serv) " +
                " Where is_use>0 and nzp_mea_p=0 and exists (select 1 from t_dom_tarifs f where f.nzp_dom=t_itog.nzp_dom and f.nzp_serv=t_itog.nzp_serv) "
                , true);
            if (!ret.result) { DropTempTablesCharge(conn_db); return; }

            ExecSQL(conn_db, " drop table t_dom_tarifs ", false);

            // приведение расхода - пример: проставить перевод в ГКал для прошлого расхода если ГВС начисляется от ГКал, а прошлое начисление с куб.м и наоборот
            ret = ExecSQL(conn_db,
                " Update t_itog Set isblocked = isblocked + 100," +
                " c_calcm_p = sum_tarif_p / tarif " +
                " Where nzp_mea<>nzp_mea_p and abs(tarif)>=0.00001 "
                , true);
            if (!ret.result) { DropTempTablesCharge(conn_db); return; }

            #endregion вычислить объемы / расходы при нехватке информации - начисления м.б. были загружены!

            // формула расчета перерасчетного начисления
            sqql = " sum_tarif-sum_tarif_p ";
            if (bIsCalcSubsidy)
            {
                sqql = " (sum_tarif - sum_tarif_f) - (sum_tarif_p - sum_tarif_f_p) ";
            }
            ret = ExecSQL(conn_db,
                " Insert into " + reval_chargeXX.reval_xx +
                " (nzp_kvar,nzp_serv,nzp_supp, year_,month_, reval, delta1,delta2," +
                "  tarif,tarif_p,sum_tarif,sum_tarif_p,sum_nedop,sum_nedop_p,c_calc,c_calcm_p,c_calc_p," +
                "  nzp_frm,nzp_frm_p,type_rsh,month_p,year_p) " +
                " Select nzp_kvar, nzp_serv,nzp_supp, " + paramcalc.calc_yy + "," + paramcalc.calc_mm +
                ", sum(reval), sum(" + sqql + "), sum(sum_nedop-sum_nedop_p) " +
                ", max(tarif),max(tarif_p),sum(sum_tarif),sum(sum_tarif_p),sum(sum_nedop),sum(sum_nedop_p)" +
                ", sum(c_calc) c_calc,sum(c_calcm_p) c_calcm_p,sum(c_calc_p) c_calc_p," +
                "  max(nzp_frm),max(nzp_frm_p),max(isblocked),max(month_p),max(year_p) " +
                " From t_itog Where abs(reval)>0.001 " +
                " Group by 1,2,3,4,5 "
                , true);
            if (!ret.result) { DropTempTablesCharge(conn_db); return; }

            MonitorLog.WriteLog("Стоп LoadReval: " + s, MonitorLog.typelog.Info, 1, 2, true);

            #region взять расходы по коммунальным услугам и ОДН

            ret = ExecSQL(conn_db,
                " Select * " +
#if PG
 " Into temp t_reval_ku " +
#else
                " " +
#endif
 " From t_itog " +
                " Where is_use>0 and abs(reval)>0.001 " +
#if PG
 " "
#else
                " Into temp t_reval_ku With no log "
#endif
, true);
            if (!ret.result) { DropTempTablesCharge(conn_db); return; }

            ExecSQL(conn_db, " Create index ix1_t_reval_ku on t_reval_ku (nzp_kvar,nzp_serv,nzp_supp) ", true);
            ExecSQL(conn_db, " Create index ix2_t_reval_ku on t_reval_ku (year_p, month_p, nzp_serv) ", true);
            ExecSQL(conn_db, sUpdStat + " t_reval_ku ", true);

            ret = ExecSQL(conn_db,
                " update t_reval_ku a " +
                " set is_calc_gku = 1," +
                    " nzp_prm_rashod = b.nzp_prm_rashod,  " +
                    " valm  = b.valm, " +
                    " dop87 = b.dop87," +
                    " rsh1  = b.rsh1, " +
                    " rsh2  = b.rsh2  " +
                " from " + reval_chargeXX.calc_gku_xx + " b " +
                " where a.nzp_kvar=b.nzp_kvar and a.nzp_serv=b.nzp_serv and a.nzp_supp=b.nzp_supp and b.stek = 3 "
                , true);
            if (!ret.result) { DropTempTablesCharge(conn_db); return; }

            // 1. проставим расходы за текущий месяц расчета (начисляемый)
            MyDataReader reader;

            // текущий месяц перерасчета = paramcalc.calc_yy + "," + paramcalc.calc_mm
            ret = ExecRead(conn_db, out reader,
                " Select year_p, month_p, nzp_serv, nzp_supp " +
                " From t_reval_ku " +
                " Group by 1,2,3,4 " +
                " Order by 1,2,3,4 "
                , true);
            if (!ret.result) { DropTempTablesCharge(conn_db); return; }

            try
            {
                while (reader.Read())
                {
                    //
                    int tek_yy = 0;
                    int tek_mm = 0;
                    int tek_serv = 0;
                    int tek_supp = 0;
                    if (reader["month_p"] != DBNull.Value)
                    {
                        tek_mm = System.Convert.ToInt32(reader["month_p"]);

                    }
                    if (reader["year_p"] != DBNull.Value)
                    {
                        tek_yy = System.Convert.ToInt32(reader["year_p"]);

                    }
                    if (reader["nzp_serv"] != DBNull.Value)
                    {
                        tek_serv = System.Convert.ToInt32(reader["nzp_serv"]);

                    }
                    if (reader["nzp_supp"] != DBNull.Value)
                    {
                        tek_supp = System.Convert.ToInt32(reader["nzp_supp"]);

                    }
                    if ((tek_mm > 0) && (tek_yy > 0) && (tek_serv > 0) && (tek_supp > 0))
                    {
                        string sNmTblPref = (tek_yy - 2000).ToString("00") + (tek_mm).ToString("00");
                        if ((tek_mm == 1) && (tek_yy == 1901))
                        {
                            // для первого рассчитанного месяца
                            sNmTblPref = "";
                            tek_yy = paramcalc.calc_yy;
                            tek_mm = paramcalc.calc_mm;
                        }
                        string calc_gkuXX_tab =
                            reval_chargeXX.paramcalc.pref + "_charge_" + (reval_chargeXX.paramcalc.calc_yy - 2000).ToString("00") + tableDelimiter +
                            "calc_gku" + sNmTblPref + "_" + (reval_chargeXX.paramcalc.calc_mm).ToString("00");

                        ret = ExecSQL(conn_db,
                            " update t_reval_ku a " +
                            " set is_calc_gku = is_calc_gku + 100, " +
                                " valm_p  = b.valm," +
                                " dop87_p = b.dop87," +
                                " rsh1_p  = b.rsh1," +
                                " rsh2_p  = b.rsh2 " +
                            " from " + calc_gkuXX_tab + " b " +
                            " where a.nzp_kvar=b.nzp_kvar and a.nzp_serv=b.nzp_serv and a.nzp_supp=b.nzp_supp and b.stek = 3 "
                            , true);
                        if (!ret.result) { DropTempTablesCharge(conn_db); return; }
                    }
                    //
                }
            }
            catch
            {
                ret.result = false;
            }
            reader.Close();

            ret = ExecSQL(conn_db,
                " Update t_reval_ku " +
                " Set rsh2_p = rsh2 " +
                " Where abs(rsh2_p)<0.00001 and abs(rsh2)>0.00001 "
                , true);
            if (!ret.result) { DropTempTablesCharge(conn_db); return; }

            ret = ExecSQL(conn_db,
                " Update t_reval_ku " +
                " Set rsh2 = rsh2_p " +
                " Where abs(rsh2_p)>0.00001 and abs(rsh2)<0.00001 "
                , true);
            if (!ret.result) { DropTempTablesCharge(conn_db); return; }

            ret = ExecSQL(conn_db,
                " Update t_reval_ku " +
                " Set is_calc_gku = is_calc_gku + 200, " +
                    " valm_p = case when isblocked >= 100 then c_calcm_p else  c_calc_p end " +
                " Where is_calc_gku = 1 and abs(sum_tarif_p) > 0.001 "
                , true);
            if (!ret.result) { DropTempTablesCharge(conn_db); return; }

            ret = ExecSQL(conn_db,
                " Update t_reval_ku " +
                " Set is_calc_gku = is_calc_gku + 1," +
                    " valm = c_calc " +
                " Where is_calc_gku = 100 and abs(sum_tarif) > 0.001 "
                , true);
            if (!ret.result) { DropTempTablesCharge(conn_db); return; }

            ret = ExecSQL(conn_db,
                " Update t_reval_ku " +
                //" Set reval_rsh  = valm - valm_p " +
                " Set reval_rsh  = c_calc - c_calc_p " +
                " Where is_calc_gku > 100 "
                , true);
            if (!ret.result) { DropTempTablesCharge(conn_db); return; }

            #endregion взять расходы по коммунальным услугам и ОДН

            //сравнить дельту расходов
            LoadDeltaRashod(conn_db, paramcalc, out ret);
            if (!ret.result) { DropTempTablesCharge(conn_db); return; }

            DropTempTablesCharge(conn_db);
        }

        //-----------------------------------------------------------------------------
        void LoadDeltaRashod(IDbConnection conn_db, CalcTypes.ParamCalc paramcalc, out Returns ret) //вычислить дельту delta_xx
        //-----------------------------------------------------------------------------
        {
            string s = paramcalc.pref + " " + paramcalc.nzp_kvar + "/" + paramcalc.nzp_dom + "/" +
                       paramcalc.calc_yy + "/" + paramcalc.calc_mm + "/" +
                       paramcalc.cur_yy + "/" + paramcalc.cur_mm;


            MonitorLog.WriteLog("Старт LoadDelta: " + s, MonitorLog.typelog.Info, 1, 2, true);

            //выбрать перерасчетный countersYYMM_xx - последний посчитанный расход из 3-го стека
            CalcTypes.ChargeXX chargeXX = new CalcTypes.ChargeXX(paramcalc);

            /* включить алгоритм учета дельт расходов при расчете ОДН - для ЕИРЦ Самара val_prm='2'! - временно пока  здесь ! - перенести в sprav.ifmx.cs */
            bool b_is_delta_smr =
              CheckValBoolPrmWithVal(conn_db, paramcalc.data_alias, 1994, "5", "2", paramcalc.dat_s, paramcalc.dat_po);

            if (b_is_delta_smr)
            {
                #region если включен самарский алгоритм учета перерасчета расходов в расходе тек.месяца

                ret = Utils.InitReturns();
                Rashod delta_XX = new Rashod(paramcalc);

                string str;
                //пока вытащим дельту по counters_xx

                string swhere = " ";
                if (paramcalc.nzp_kvar > 0 || paramcalc.nzp_dom > 0 || paramcalc.list_dom)
                    swhere = " and nzp_kvar in ( Select nzp_kvar From t_selkvar ) ";

                ExecSQL(conn_db, " drop table reval_charge ", false);
                str =
                        " Create temp table reval_charge" +
                        "  ( nzp_cntx   integer not null, " +
                        "    nzp_kvar   integer not null, " +
                        "    nzp_dom    integer not null, " +
                        "    nzp_serv   integer not null, " +
                        "    val1       " + sDecimalType + "(15,7) default 0.00, " +
                        "    val2       " + sDecimalType + "(15,7) default 0.00, " +
                        "    dlt        " + sDecimalType + "(15,7) default 0.00, " +
                        "    cnt_stage  integer not null, " +
                        "    kod_info   integer not null, " +
                        "    kod        integer default 0 " +
                        "  )  " + sUnlogTempTable;
                ret = ExecSQL(conn_db, str, true);
                if (!ret.result) { DropTempTablesCharge(conn_db); return; }

                str =
                        " Insert into reval_charge" +
                        " (nzp_cntx, nzp_kvar,nzp_dom,nzp_serv, val1, val2, dlt, kod, kod_info, cnt_stage) " +
                        " Select nzp_cntx, nzp_kvar,nzp_dom,nzp_serv, val1, val2, val1*0 as dlt, 0 as kod, kod_info, cnt_stage " +
                        " From " + delta_XX.counters_xx +
                        " Where nzp_type = 3 and stek = 3 " +
                            swhere;

                ret = ExecSQL(conn_db, str, true);
                if (!ret.result) { DropTempTablesCharge(conn_db); return; }

                ExecSQL(conn_db, " Create unique index ix0_reval_charge on reval_charge (nzp_cntx) ", true);
                ExecSQL(conn_db, " Create        index ix1_reval_charge on reval_charge (nzp_kvar,nzp_serv, kod) ", true);
                ExecSQL(conn_db, " Create        index ix2_reval_charge on reval_charge (kod) ", true);
                ExecSQL(conn_db, sUpdStat + " reval_charge ", true);

                //нетривиальный сбор предыдущих посчитанных расходов
                //начинаем считать снизу вверх: countersYY(MM-1) ~~> counters_MM
                //в dlt_in, dlt_out начнем складывать предыдущий расход, kod = 1, значит нашли

                //выберем для начала все доступные таблицы counters**_xx 
                string dbs = paramcalc.pref + "_charge_" + (paramcalc.calc_yy - 2000).ToString("00");
                List<string> l = GetTables(conn_db, dbs, "counters**_" + paramcalc.calc_mm.ToString("00"));

                for (int yy = paramcalc.cur_yy; yy >= paramcalc.calc_yy; yy--)
                {
                    for (int mm = 12; mm > 0; mm--)
                    {
                        //отсечем будущее
                        if (yy > paramcalc.cur_yy)
                            continue;
                        if (yy == paramcalc.cur_yy && mm >= paramcalc.cur_mm)
                            continue;

                        string alis = (yy - 2000).ToString() + mm.ToString("00");
                        string tab = "counters" + alis + "_" + paramcalc.calc_mm.ToString("00");

                        if (l.Contains(tab))
                        {
                            GetDeltaRashod(conn_db, dbs + tableDelimiter + tab, out ret);
                            if (!ret.result) { return; }
                        }
                    }
                }

                // и наконец сравнить с начальным counters_xx
                GetDeltaRashod(conn_db, dbs + tableDelimiter + "counters_" + paramcalc.calc_mm.ToString("00"), out ret);
                if (!ret.result) { return; }
                //Rust
                // функция подделки данных из charge_xx, сначала выбрать уникальные строки , затем изменить тариф и расходы , поскольку расчит значений нет , то все положим в расход по нормативу Val2
                GetDeltaRashodCharge(conn_db, "", paramcalc, out ret);
                if (!ret.result) { return; }
                // Выбрать только текущий месяц для анализа в delta во временную таблицу 

                // подсчитать дельты с подделанным counters_dop 
                GetDeltaRashod(conn_db, "t_counters_dop ", out ret);
                if (!ret.result) { return; }
                //rust

                // Вставка итоговых дельт 

                str =
                        " Insert into " + chargeXX.delta_xx +
                        " (nzp_kvar, nzp_dom, nzp_serv, year_,month_, stek, val1,val2, kod_info, cnt_stage) " +
                     " Select nzp_kvar, nzp_dom, nzp_serv, " + paramcalc.calc_yy + "," + paramcalc.calc_mm + ", 1," +
                     " case when cnt_stage>0 then 0 else dlt end, case when cnt_stage>0 then dlt else 0 end, kod_info, cnt_stage " +
                     " From reval_charge " +
                     " Where abs(dlt)>0.001 ";

                ret = ExecSQL(conn_db, str, true);
                if (!ret.result) { DropTempTablesCharge(conn_db); return; }

                #endregion если включен самарский алгоритм учета перерасчета расходов в расходе тек.месяца
            }
            else
            {
                // ... стандартный расчет ...year_p, month_p

                // Вставка итоговых дельт в стек = 3 - они уже сформированы в t_itog -> t_reval_ku в LoadDeltaReval() - Сохранение перерасчетных данных

                ret = ExecSQL(conn_db,
                    " Insert into " + chargeXX.delta_xx +
                      " (nzp_kvar,nzp_dom,nzp_serv,year_,month_,stek,val1,val2,val3,cnt_stage,kod_info,valm,valm_p,dop87,dop87_p) " +
                    " Select " +
                      "  nzp_kvar, nzp_dom, nzp_serv, " + paramcalc.calc_yy + "," + paramcalc.calc_mm + ", 3 as stek," +
                    " sum(reval_rsh) as val1, " +
                    " max(rsh2)   as val2," +
                    " max(rsh2_p) as val3," +
                    " max(is_device), max(nzp_mea), sum(valm), sum(valm_p), sum(dop87), sum(dop87_p) " +
                    " From t_reval_ku " +
                    " Where abs(reval)>0.001 " +
                    " group by 1,2,3,4,5 "
                    , true);
                if (!ret.result) { DropTempTablesCharge(conn_db); return; }
            }

            DropTempTablesCharge(conn_db);

            MonitorLog.WriteLog("Стоп LoadDelta: " + s, MonitorLog.typelog.Info, 1, 2, true);
        }

        //-----------------------------------------------------------------------------
        void GetDeltaRashodCharge(IDbConnection conn_db, string counters_xx, CalcTypes.ParamCalc paramcalc, out Returns ret) //вычислить поддельный counters_xx положить counters_dop
        //-----------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            string str;
            // ограничить обрабатываемые строки 
            // вставить данные из charge 
            str = " Drop table t_reval_charge ";
            ExecSQL(conn_db, str, false);

            string sCntDopName = "counters_dop";
            if (paramcalc.id_bill_pref.IsNotEmpty())
            {
                sCntDopName = "counters_" + paramcalc.id_bill_pref.Trim() + "_dop";
            }

            // ограниченные reval_charge
            ret = ExecSQL(conn_db,
                " create temp table t_reval_charge ( " +
                " nzp_kvar integer, " +
                " nzp_serv integer " +
                " )  " + sUnlogTempTable
                , true);

            //ExecSQL(conn_db, " select nzp_kvar, nzp_serv from reval_charge where kod=0 into temp t_reval_charge with no log ", true);
            str =
                " insert into t_reval_charge (nzp_kvar, nzp_serv) " +
                " select nzp_kvar, nzp_serv " +
                " from reval_charge where kod=0 ";
            ExecSQL(conn_db, str, true);
            ExecSQL(conn_db, " Create index ixt_rvl_charge on t_reval_charge (nzp_kvar, nzp_serv) ", true);
            ExecSQL(conn_db, sUpdStat + " t_reval_charge ", true);

            // Rust проверить есть ли в данном месяце таблица counters_dop 
            string dbs1 = paramcalc.pref + "_charge_" + (paramcalc.calc_yy - 2000).ToString("00");

            //List<string> l1 = GetTables(conn_db, dbs1, "counters_dop_" + paramcalc.calc_mm.ToString("00"));
            List<string> l1 = GetTables(conn_db, dbs1, sCntDopName);
            if (!l1.Contains(sCntDopName))
            {

#if PG
                ret = ExecSQL(conn_db, " set search_path to '" + dbs1 + "'", true);
#else
                ret = ExecSQL(conn_db, " Database " + dbs1, true);
#endif
                ret = ExecSQL(conn_db,
                    "  create table " + tbluser + sCntDopName +
                    "  ( nzp_cntx   serial  not null,  " +
                    "    nzp_kvar   integer not null,  " +
                    "    nzp_dom    integer not null,  " +
                    "    nzp_serv   integer not null,  " +
                    "    sum_tarif  " + sDecimalType + "(15,7) default 0.00,  " +
                    "    tarif      " + sDecimalType + "(15,7) default 0.00,  " +
                    "    val1       " + sDecimalType + "(15,7) default 0.00,  " +
                    "    val2       " + sDecimalType + "(15,7) default 0.00,  " +
                    "    dlt        " + sDecimalType + "(15,7) default 0.00,  " +
                    "    cnt_stage  integer not null,  " +
                    "    nzp_type   integer default 3,  " +
                    "    kod_info   integer not null,  " +
                    "    kod        integer default 0,  " +
                    "    month_     integer, " +
                    "    stek       integer default 3 " +
                    "  )  "
                    , true);
                ExecSQL(conn_db, " Create unique index " + tbluser + "ix0_" + sCntDopName + " on " + sCntDopName + " (nzp_cntx) ", true);
                ExecSQL(conn_db, " Create        index " + tbluser + "ix1_" + sCntDopName + " on " + sCntDopName + " (nzp_kvar,nzp_serv, kod) ", true);
                ExecSQL(conn_db, " Create        index " + tbluser + "ix2_" + sCntDopName + " on " + sCntDopName + " (kod) ", true);
                ExecSQL(conn_db, " Create        index " + tbluser + "ix3_" + sCntDopName + " on " + sCntDopName + " (month_,nzp_kvar) ", true);
                ExecSQL(conn_db, sUpdStat + " counters_dop ", true);
            }
            // Rust

            // Удалить талицу которая только для данного месяца и создать тутже 
            ExecSQL(conn_db, " Drop table t_counters_dop ", false);

            ret = ExecSQL(conn_db,
                "  create temp table t_counters_dop " +
                "  ( nzp_cntx   serial  not null,  " +
                "    nzp_kvar   integer not null,  " +
                "    nzp_dom    integer not null,  " +
                "    nzp_serv   integer not null,  " +
                "    sum_tarif  " + sDecimalType + "(15,7) default 0.00,  " +
                "    tarif      " + sDecimalType + "(15,7) default 0.00,  " +
                "    val1       " + sDecimalType + "(15,7) default 0.00,  " +
                "    val2       " + sDecimalType + "(15,7) default 0.00,  " +
                "    dlt        " + sDecimalType + "(15,7) default 0.00,  " +
                "    cnt_stage  integer not null,  " +
                "    nzp_type   integer default 3,  " +
                "    kod_info   integer not null,  " +
                "    kod        integer default 0,  " +
                "    month_     integer, " +
                "    stek       integer default 3 " +
                "  )  " + sUnlogTempTable
                , true);

            ExecSQL(conn_db, " create index ix00tcounters_dop on  t_counters_dop (nzp_kvar, nzp_serv, nzp_dom )", true);
            ExecSQL(conn_db, sUpdStat + " t_counters_dop ", true);

            str =
                " insert into t_counters_dop " +
                "          (nzp_kvar, nzp_dom , nzp_serv, sum_tarif , tarif, val1, val2, dlt , cnt_stage, kod_info, kod, month_, stek , nzp_type) " +
                " select   b.nzp_kvar, 0 as nzp_dom , b.nzp_serv,b.sum_tarif ,0 as tarif,0 as val1, 0 as val2 ,0 as dlt ," +
                         " 0 as cnt_stage,0 as kod_info,0 as kod, " + paramcalc.calc_mm + ",3 ,3 " +
                " from " + dbs1 + tableDelimiter +
                "charge_" + paramcalc.calc_mm.ToString("00") + " b, t_reval_charge a " +
                " where a.nzp_kvar =b.nzp_kvar and a.nzp_serv =b.nzp_serv " +
                " and b.sum_tarif>0 and b.dat_charge is null ";
            ExecSQL(conn_db, str, true);

            // Выставить  выставить поля nzp_dom 
            str = " update t_counters_dop " +
                  " set nzp_dom =(select c.nzp_dom from " + paramcalc.data_alias + "kvar c where c.nzp_kvar = t_counters_dop.nzp_kvar) ";
            ExecSQL(conn_db, str, true);

            // поля тариф из calc_gku ,
            string sCalcGkuName = "calc_gku";
            if (paramcalc.id_bill_pref.IsNotEmpty())
            {
                sCalcGkuName = "calc_gku_" + paramcalc.id_bill_pref;
            }

            // поля тариф из calc_gku ,
            str = " update t_counters_dop " +
                  " set tarif=(select max(c.tarif) " +
                             " from " + dbs1 + tableDelimiter +
                             sCalcGkuName.Trim() + (paramcalc.cur_yy - 2000).ToString("00") + paramcalc.cur_mm.ToString("00") + "_" + paramcalc.calc_mm.ToString("00") + " c " +
                             " where c.nzp_kvar = t_counters_dop.nzp_kvar and c.nzp_serv = t_counters_dop.nzp_serv and c.tarif>0 ) ";
            ExecSQL(conn_db, str, true);

            str = " update t_counters_dop set tarif=0 where tarif is null ";
            ExecSQL(conn_db, str, true);

            str = " update t_counters_dop set val2 =sum_tarif/tarif where tarif>0 ";
            ExecSQL(conn_db, str, true);

            str = " delete from " + dbs1 + tableDelimiter + sCntDopName +
                  " where month_=" + paramcalc.calc_mm;
            ExecSQL(conn_db, str, true);

            // переложить в постоянную таблицу
            str =
            " insert into  " + dbs1 + tableDelimiter + sCntDopName +
                  " (nzp_kvar,  nzp_dom , nzp_serv, sum_tarif , tarif,  val1,  val2 , dlt ,  cnt_stage, kod_info, kod, month_ , stek, nzp_type) " +
            " select nzp_kvar,  nzp_dom , nzp_serv, sum_tarif , tarif,  val1,  val2 , dlt ,  cnt_stage, kod_info, kod, month_ , stek, nzp_type " +
            " from t_counters_dop ";

            ExecSQL(conn_db, str, true);
        }
        //-----------------------------------------------------------------------------
        void GetDeltaRashod(IDbConnection conn_db, string counters_xx, out Returns ret) //вычислить дельту 
        //-----------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            //сравнить rashod и reval_charge, если есть дельта (val1,val2), то их положить в dlt_in, dlt_out и код выставить в 1
            //сначала проверим, что есть необработанные записи kod=0

            IDataReader reader;
            bool b = true;
            ret = ExecRead(conn_db, out reader,
                " Select * From reval_charge Where kod = 0 "
                , true);
            if (!ret.result)
            {
                return;
            }
            try
            {
                if (reader.Read())
                {
                    //есть необработанные записи
                    b = false;
                }
            }
            catch
            {
            }
            reader.Close();

            if (b) return;

            ExecSQL(conn_db, " Drop table t_lnk_reval ", false);
            ExecSQL(conn_db, " Drop table ttt_charge ", false);

            //мы уверены, что в countersMM_XX гарантированно есть строки по услугам, которые присутствуют в charge_xx
            //поэтому смело можем сравнивать разные counters'ы

            ret = ExecSQL(conn_db,
                " create temp table t_lnk_reval ( " +
                " nzp_kvar integer, " +
                " nzp_serv integer, " +
                " nzp_cntx integer, " +
                " dlt  " + sDecimalType + "(15,7) default 0.00, " +
                " kod_info integer  " +
                " )  " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesCharge(conn_db); return; }

            ret = ExecSQL(conn_db,
                " insert into t_lnk_reval (nzp_kvar, nzp_serv, nzp_cntx, dlt, kod_info) " +
                " Select a.nzp_kvar, a.nzp_serv, max(a.nzp_cntx) as nzp_cntx," +
                " sum( (a.val1+a.val2) - (b.val1+b.val2)) as dlt, max(b.kod_info) kod_info " +
 " From reval_charge a, " + counters_xx + " b " +
                " Where a.nzp_kvar = b.nzp_kvar " +
                "   and a.nzp_serv = b.nzp_serv " +
                "   and b.nzp_type = 3 " +
                "   and b.stek = 3 " +
                "   and ( abs((a.val1+a.val2) - (b.val1+b.val2)) > 0.0001 ) " +
                "   and a.kod = 0 " + //еще не обработанные записи
                " Group by 1,2 "
, true);
            if (!ret.result) { DropTempTablesCharge(conn_db); return; }

            ExecSQL(conn_db, " Create unique index ix1_t_lnk_reval on t_lnk_reval (nzp_cntx) ", true);
            ExecSQL(conn_db, sUpdStat + " t_lnk_reval ", true);

            // проставить дельты
            ret = ExecSQL(conn_db,
                " Update reval_charge " +
                " Set kod=1, dlt = (Select dlt From t_lnk_reval Where t_lnk_reval.nzp_cntx = reval_charge.nzp_cntx) " +
                " Where nzp_cntx in ( Select nzp_cntx From t_lnk_reval ) "
                , true);
            if (!ret.result) { DropTempTablesCharge(conn_db); return; }

            // если был kod_info, то пусть остается - это просто признак наличия расчета ОДН - был так был! Но новый kod_info тем у кого не было проставим

            ret = ExecSQL(conn_db,
                 " Update reval_charge " +
                 " Set kod_info = ( Select kod_info From t_lnk_reval Where t_lnk_reval.nzp_cntx = reval_charge.nzp_cntx and kod_info>0 ) " +
                 " Where kod_info=0 and nzp_cntx in ( Select nzp_cntx From t_lnk_reval where kod_info>0 ) "
                 , true);
            if (!ret.result) { DropTempTablesCharge(conn_db); return; }

            ExecSQL(conn_db, sUpdStat + " reval_charge ", true);

            ret = ExecSQL(conn_db,
                " Update reval_charge " +
                " Set kod = 1 " +
                " Where kod = 0 " +
                "   and exists ( " +
                    " Select 1 From " + counters_xx + " a " +
                    " Where a.nzp_kvar = reval_charge.nzp_kvar and a.nzp_serv = reval_charge.nzp_serv " +
                    "   and a.nzp_type = 3 and a.stek = 3 " +
                    " ) "
                , true);
            if (!ret.result) { DropTempTablesCharge(conn_db); return; }

        }
        #endregion Сохранение перерасчетных данных

        #region Определение периодов перерасчетов
        //заполнение must_calc
        //-----------------------------------------------------------------------------
        void CountersUpdateDatS(IDbConnection conn_db, string table, string nzp, CalcTypes.ParamCalc paramcalc, string ravno, string t_selkvar, out Returns ret)
        //-----------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            //поле, отвечающее за перерасчет указанного периода
            if (!isTableHasColumn(conn_db, table, "month_calc"))
            {
                string sql = "alter table " + table + " add month_calc date";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                {
                    ret.text = "Ошибка изменения структуры таблицы " + table + " при добавлении поля month_calc ";

                    //conn_db.Close();
                    return;
                }
                sql = "create index " + tbluser + "x3_" + table + " on " + table + " (month_calc) ";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                {
                    ret.text = "Ошибка создания индекса для" + table + ".month_calc ";

                    //conn_db.Close();
                    return;
                }
            }

            if (ret.result) ret = AddFieldToTable(conn_db, table, "dat_s", "date");
            if (ret.result) ret = AddFieldToTable(conn_db, table, "dat_po", "date");

            string month_calc = MDY(paramcalc.cur_mm, 1, paramcalc.cur_yy);

            //выбрать все показания по month_calc, где dat_s не заполнен
            ExecSQL(conn_db, " Drop table t_cnts ", false);

            //выбрать пред. показание
            //ret = ExecSQL(conn_db, " set search_path to '" + paramcalc.pref.Trim() + "_data'", true);

            ret = ExecSQL(conn_db,
                " Select a." + nzp + ", max(b.dat_uchet) as dat_s " +
#if PG
 " Into temp t_cnts " +
#else
                " " +
#endif
 " From " + table + " a, " + table + " b " +
                " Where a.nzp_counter = b.nzp_counter " +
                    ravno +
                "   and a.month_calc = " + month_calc +
                "   and a.dat_s is null " +
                "   and a.is_actual <> 100 " +
                "   and b.is_actual <> 100 " +
                "   and a.dat_uchet > b.dat_uchet " +
                " Group by 1 " +
#if PG
 " "
#else
                " Into temp t_cnts With no log "
#endif
, true);

            if (!ret.result)
            {
                return;
            }

            ExecSQL(conn_db, " create unique index ix_t_cnts on t_cnts (" + nzp + ") ", true);
            ExecSQL(conn_db, sUpdStat + " t_cnts ", true);
            //вставить период, с которого надо пересчитать
            //ret = ExecSQL(conn_db,
            string sql2 =
                " Update " + table +
                " Set dat_s = ( Select dat_s From t_cnts t " +
                              " Where " + table + "." + nzp + " = t." + nzp + ")" +
                " Where 1 = 1 " + t_selkvar +
#if PG
 "   and '01.01.1900' <= " +
#else
                "   and 0 < " +
#endif
 "( Select dat_s From t_cnts t " +
                            " Where " + table + "." + nzp + " = t." + nzp + ")";


            if (paramcalc.nzp_kvar > 0 || paramcalc.nzp_dom > 0)
                ret = ExecSQL(conn_db, sql2, true);
            else
                ExecByStep(conn_db, table, nzp, sql2, 100000, " ", out ret);

            if (!ret.result)
            {
                return;
            }


            ExecSQL(conn_db, " Drop table t_cnts ", false);
            //пустые dat_s исправить на dat_po, чтобы не пересчитывать с начала веков!!
            ret = ExecSQL(conn_db,
                " Select " + nzp +
#if PG
 " Into temp t_cnts " +
#else
                " " +
#endif
 " From " + table +
                " Where 1 = 1 " + t_selkvar +
                "   and is_actual <> 100 " +
                "   and dat_s is null " +
                "   and month_calc = " + month_calc +
#if PG
 " "
#else
                " Into temp t_cnts With no log "
#endif
, true);

            if (!ret.result)
            {
                return;
            }

            ExecSQL(conn_db, " create unique index ix_t_cnts on t_cnts (" + nzp + ") ", true);
            ExecSQL(conn_db, sUpdStat + " t_cnts ", true);
            ret = ExecSQL(conn_db,
                " Update " + table +
                " Set dat_s = dat_po " +
                " Where " + nzp + " in ( Select " + nzp + " From t_cnts ) "
                , true);


        }
        //-----------------------------------------------------------------------------
        void InsMustCalc(IDbConnection conn_db, CalcTypes.ParamCalc paramcalc, out Returns ret) //заполнение must_calc
        //-----------------------------------------------------------------------------
        {
            //учтем оплаченные charge_cnts за предыдущий закрытый, вставим в pref_data:counters (выставим текущий month_calc)
            if (!paramcalc.isPortal)
            {
                Portal_UchetCounters(conn_db, paramcalc, out ret);
                if (!ret.result)
                {
                    return;
                }

                Portal_UchetNedop(conn_db, paramcalc, out ret); //тоже самое и с charge_nedo
                if (!ret.result)
                {
                    return;
                }

                //при paramcalc.isPortal=true данная функция вызывается в CalcRashod, там уже меняется temp_counters
            }


            //проставим dat_s в counters по текущему month_calc
            string where_kvar = "nzp_kvar in ( Select nzp_kvar From t_selkvar) ";
            if (paramcalc.nzp_kvar > 0)
                where_kvar = "nzp_kvar = " + paramcalc.nzp_kvar;

            string where_dom = "nzp_dom in ( Select nzp_dom From t_selkvar) ";
            if (paramcalc.nzp_dom > 0)
                where_dom = "nzp_dom = " + paramcalc.nzp_dom;

            CountersUpdateDatS(conn_db, "counters", "nzp_cr", paramcalc, //paramcalc.cur_yy, paramcalc.cur_mm,
                //" and a.nzp_kvar = b.nzp_kvar and a.nzp_kvar in ( Select nzp_kvar From t_selkvar) ", " and nzp_kvar in ( Select nzp_kvar From t_selkvar) " 
                " and a.nzp_kvar = b.nzp_kvar and a." + where_kvar, " and " + where_kvar
                , out ret);
            if (!ret.result)
            {
                return;
            }
            CountersUpdateDatS(conn_db, "counters_dom", "nzp_crd", paramcalc, //paramcalc.cur_yy, paramcalc.cur_mm,
                //" and a.nzp_dom = b.nzp_dom and a.nzp_dom in ( Select nzp_dom From t_selkvar) ", " and nzp_dom in ( Select nzp_dom From t_selkvar) "
                " and a.nzp_dom = b.nzp_dom and a." + where_dom, " and " + where_dom
                , out ret);
            if (!ret.result)
            {
                return;
            }
            CountersUpdateDatS(conn_db, "counters_group", "nzp_cg", paramcalc, //paramcalc.cur_yy, paramcalc.cur_mm, 
                "", "", out ret);
            if (!ret.result)
            {
                return;
            }

            if (Points.RecalcMode == RecalcModes.Automatic)
            {
                DbEditInterData dbEdit = new DbEditInterData();
                EditInterDataMustCalc editData = new EditInterDataMustCalc();

                editData.nzp_wp = paramcalc.nzp_wp;
                editData.pref = paramcalc.pref;
                editData.nzp_user = 1;

                editData.dat_s = paramcalc.dat_s;
                editData.dat_po = paramcalc.dat_po;
                editData.intvType = enIntvType.intv_Day;

                editData.dopFind = new List<string>();
                editData.keys = new Dictionary<string, string>();
                editData.vals = new Dictionary<string, string>();

                //перечислим все целевые таблицы
                //+++++++++++++++++++++++++++++++++++++++++++++++
                //прямая выборка в must_calc (nzp_kvar,nzp_serv)
                //+++++++++++++++++++++++++++++++++++++++++++++++
                editData.dopFind.Add(" and nzp_kvar in ( Select nzp_kvar From t_selkvar )");
                editData.keys.Add("+++", "");
                editData.vals.Add("+++", "");

                editData.table = "tarif";
                editData.primary = "nzp_tarif";
                editData.mcalcType = enMustCalcType.mcalc_Serv;
                dbEdit.MustCalc(conn_db, null, editData, out ret);
                if (!ret.result)
                {
                    dbEdit.Close();
                    return;
                }

                editData.table = "counters";
                editData.primary = "nzp_cr";
                editData.mcalcType = enMustCalcType.Counter;
                dbEdit.MustCalc(conn_db, null, editData, out ret);
                if (!ret.result)
                {
                    dbEdit.Close();
                    return;
                }
                editData.table = "nedop_kvar";
                editData.primary = "nzp_nedop";
                editData.mcalcType = enMustCalcType.Nedop;
                dbEdit.MustCalc(conn_db, null, editData, out ret);
                if (!ret.result)
                {
                    dbEdit.Close();
                    return;
                }

                //+++++++++++++++++++++++++++++++++++++++++++++++
                //выборка из квартирных prm_1,3 (nzp)
                //+++++++++++++++++++++++++++++++++++++++++++++++
                editData.dopFind.Clear();
                editData.dopFind.Add(" and p.nzp in ( Select nzp_kvar From t_selkvar )");
                editData.table = "prm_1";
                editData.primary = "nzp_key";
                editData.mcalcType = enMustCalcType.mcalc_Prm1;
                dbEdit.MustCalc(conn_db, null, editData, out ret);
                if (!ret.result)
                {
                    dbEdit.Close();
                    return; ;
                }
                editData.table = "prm_3";
                editData.primary = "nzp_key";
                dbEdit.MustCalc(conn_db, null, editData, out ret);
                if (!ret.result)
                {
                    dbEdit.Close();
                    return;
                }

                //+++++++++++++++++++++++++++++++++++++++++++++++
                //выборка из домовых prm_2,4 (nzp)
                //+++++++++++++++++++++++++++++++++++++++++++++++
                editData.dopFind.Clear();
                editData.dopFind.Add(" and k.nzp_kvar in ( Select nzp_kvar From t_selkvar )");
                editData.table = "prm_2";
                editData.primary = "nzp_key";
                editData.mcalcType = enMustCalcType.mcalc_Prm2;
                dbEdit.MustCalc(conn_db, null, editData, out ret);
                if (!ret.result)
                {
                    dbEdit.Close();
                    return;
                }
                editData.table = "prm_4";
                editData.primary = "nzp_key";
                dbEdit.MustCalc(conn_db, null, editData, out ret);
                if (!ret.result)
                {
                    dbEdit.Close();
                    return;
                }

                //+++++++++++++++++++++++++++++++++++++++++++++++
                //жильцовые 
                //+++++++++++++++++++++++++++++++++++++++++++++++
                editData.dopFind.Clear();
                editData.dopFind.Add(" and p.nzp_kvar in ( Select nzp_kvar From t_selkvar )");
                editData.table = "pere_gilec";
                editData.primary = "nzp_pere_gilec";
                editData.mcalcType = enMustCalcType.mcalc_Gil;
                dbEdit.MustCalc(conn_db, null, editData, out ret);
                if (!ret.result)
                {
                    dbEdit.Close();
                    return;
                }

                dbEdit.Close();
            }
        }
        void ChoiseTempMustCalc(IDbConnection conn_db, CalcTypes.ParamCalc paramcalc, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ExecSQL(conn_db, " Drop table t_mustcalc ", false);

            ret = ExecSQL(conn_db,
                " Create temp table t_mustcalc " +
                " ( nzp_kvar integer," +
                "   nzp_serv integer " +
                " ) " + sUnlogTempTable
            , true);
            if (!ret.result) { return; }

            if (paramcalc.b_reval)
            {
                string portal_must_calc = "";

                if (paramcalc.isPortal)
                {
                    CalcTypes.ChargeXX calc_chargeXX = new CalcTypes.ChargeXX(paramcalc); //расчетная таблица начислений

                    portal_must_calc =
                        " Union " +
                        " Select nzp_kvar, nzp_serv From " + calc_chargeXX.charge_cnts +
                        " Where nzp_kvar  = " + paramcalc.nzp_kvar +
                        "   and id_bill = " + paramcalc.id_bill +
                        "   and dat_charge = " + paramcalc.portal_dat_charge;
                }

                //пока сделал так, что если задан конкретный дом, то пересчитываются все лицевые счета дома (т.е. must_calc игнорируется)
                //+++++++++++++++++++++++++++++++++++++++++++++++++++
                //выбрать мно-во лицевых счетов из must_calc!
                //+++++++++++++++++++++++++++++++++++++++++++++++++++
                //" Insert into t_mustcalc (nzp_kvar,nzp_serv) " + //не проходит
                ret = ExecSQL(conn_db,
                    " Insert Into t_mustcalc (nzp_kvar,nzp_serv) " +
                    " Select " + sUniqueWord + " k.nzp_kvar, m.nzp_serv " +
                    " From " + paramcalc.data_alias + "must_calc m, " + paramcalc.data_alias + "kvar k " +
                    " Where k.nzp_kvar = m.nzp_kvar " +
                    "   and k." + paramcalc.where_z +
                    "   and m.year_ = " + paramcalc.cur_yy +
                    "   and m.month_ = " + paramcalc.cur_mm +
                    "   and m.dat_s  <= " + sDefaultSchema + "MDY(" + paramcalc.calc_mm + "," +
                    DateTime.DaysInMonth(paramcalc.calc_yy, paramcalc.calc_mm) + "," + paramcalc.calc_yy + ")" +
                    "   and m.dat_po >= " + sDefaultSchema + "MDY(" + paramcalc.calc_mm + ",1," + paramcalc.calc_yy + ")"
                    + portal_must_calc
                    , true);
                if (!ret.result) { return; }
            }

            ExecSQL(conn_db, " Create unique index ix_t_mustcalc on t_mustcalc (nzp_kvar,nzp_serv) ", true);
            ExecSQL(conn_db, sUpdStat + " t_mustcalc ", true);
        }

        #endregion Определение периодов перерасчетов

        #region Функции работы с Порталовским таблицыми (charge_cnts etc.)
        //Функции работы с Порталовским таблицыми (charge_cnts etc.)

        //----------------------------------------------------------------------------- 
        void Portal_SaveCalc(IDbConnection conn_db, CalcTypes.ParamCalc paramcalc, out Returns ret) //сохранить результат расчета в charge_g
        //-----------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            CalcTypes.ChargeXX calc_chargeXX = new CalcTypes.ChargeXX(paramcalc); //расчетная таблица начислений

            ret = ExecSQL(conn_db,
                " Delete From " + calc_chargeXX.charge_g +
                " Where nzp_kvar  = " + paramcalc.nzp_kvar +
                "   and id_bill = " + paramcalc.id_bill +
                "   and dat_charge = " + paramcalc.portal_dat_charge
                , true);
            if (!ret.result) { return; }

            ret = ExecSQL(conn_db,
                " Insert into " + calc_chargeXX.charge_g +
                " ( nzp_kvar, num_ls, nzp_serv, nzp_supp, nzp_frm, kod_sum, dat_charge, id_bill, " +
                "   tarif, tarif_p, rsum_tarif, rsum_lgota, sum_tarif, sum_dlt_tarif, sum_dlt_tarif_p, sum_tarif_p, " +
                "   sum_lgota, sum_dlt_lgota, sum_dlt_lgota_p, sum_lgota_p, sum_nedop, sum_nedop_p, sum_real, sum_charge, " +
                "   reval, real_pere, sum_pere, real_charge, sum_money, money_to, money_from, money_del, sum_fakt, fakt_to, " +
                "   fakt_from, fakt_del, sum_insaldo, izm_saldo, sum_outsaldo, sum_subsidy, sum_subsidy_p, sum_subsidy_reval, " +
                "   sum_subsidy_all, isblocked, is_device, c_calc, c_sn, c_okaz, c_nedop, dat_save, dat_calc, isdel, c_reval ) " +
                " Select nzp_kvar, num_ls, nzp_serv, nzp_supp, nzp_frm, 55, " + paramcalc.portal_dat_charge + ",  " + paramcalc.id_bill + "," +
                "   tarif, tarif_p, rsum_tarif, rsum_lgota, sum_tarif, sum_dlt_tarif, sum_dlt_tarif_p, sum_tarif_p, " +
                "   sum_lgota, sum_dlt_lgota, sum_dlt_lgota_p, sum_lgota_p, sum_nedop, sum_nedop_p, sum_real, sum_charge, " +
                "   reval, real_pere, sum_pere, real_charge, sum_money, money_to, money_from, money_del, sum_fakt, fakt_to, " +
                "   fakt_from, fakt_del, sum_insaldo, izm_saldo, sum_outsaldo, sum_subsidy, sum_subsidy_p, sum_subsidy_reval, " +
                "   sum_subsidy_all, isblocked, is_device, c_calc, c_sn, c_okaz, c_nedop, " + sCurDate + ", " +
                paramcalc.portal_dat_charge + ", isdel, c_reval " +
                " From " + calc_chargeXX.charge_xx +
                " Where nzp_kvar  = " + paramcalc.nzp_kvar
                , true);
            if (!ret.result) { return; }
        }
        //-----------------------------------------------------------------------------
        bool Portal_YesMustCalc(DateTime d, List<PeriodMustCalc> list)
        //-----------------------------------------------------------------------------
        {
            foreach (PeriodMustCalc l in list)
            {
                if (d <= l.dat_po && d >= l.dat_s)
                    return true;
            }
            return false;
        }
        //-----------------------------------------------------------------------------
        void Portal_MustCalc(IDbConnection conn_db, CalcTypes.ParamCalc paramcalc, ref List<PeriodMustCalc> list, out Returns ret)
        //-----------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            CalcTypes.ChargeXX calc_chargeXX = new CalcTypes.ChargeXX(paramcalc); //расчетная таблица начислений

            IDataReader reader;

            //определим границы перерасчетов charge_cnts, charge_nedo

            ret = ExecRead(conn_db, out reader,
                " Select prev_dat, cur_dat, prev2_dat From " + calc_chargeXX.charge_cnts +
                " Where nzp_kvar  = " + paramcalc.nzp_kvar +
                "   and dat_charge = " + paramcalc.portal_dat_charge +
                "   and prev_dat is not null and prev_val is not null " +
                "   and cur_dat is not null and cur_val is not null " +
                "   and cur_dat > prev_dat " +
                " Union " +
                " Select date(dat_s) as prev_dat, date(dat_po) as cur_dat, date(null) as prev2_dat From " + calc_chargeXX.charge_nedo +
                " Where nzp_kvar  = " + paramcalc.nzp_kvar +
                "   and dat_charge = " + paramcalc.portal_dat_charge +
                "   and dat_s is not null and dat_po is not null " +
                "   and dat_s > dat_po "
                , true);
            if (!ret.result) { return; }

            //дата начала расчета (by default)
            int beg_yy = Points.BeginWork.year_;
            int beg_mm = Points.BeginWork.month_;

            try
            {
                while (reader.Read())
                {
                    PeriodMustCalc d = new PeriodMustCalc();

                    d.dat_s = new DateTime(beg_yy, beg_mm, 1);
                    d.dat_po = new DateTime(beg_yy, beg_mm, 1);

                    if (reader["prev_dat"] != DBNull.Value)
                        d.dat_s = (DateTime)reader["prev_dat"];
                    if (reader["cur_dat"] != DBNull.Value)
                        d.dat_po = (DateTime)reader["cur_dat"];

                    list.Add(d); //границы перерасчета

                    if (reader["prev2_dat"] != DBNull.Value)
                    {
                        d.dat_po = d.dat_s;
                        d.dat_s = (DateTime)reader["prev2_dat"];

                        list.Add(d); //границы перерасчета
                    }
                }
            }
            catch
            {
                ret.result = false;
            }

            reader.Close();
        }
        //-----------------------------------------------------------------------------
        void Portal_UchetCounters(IDbConnection conn_db, CalcTypes.ParamCalc paramcalc, out Returns ret) //учет charge_cnts в counters
        //-----------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            CalcTypes.ChargeXX calc_chargeXX = new CalcTypes.ChargeXX(paramcalc); //расчетная таблица начислений

            //выбрать мно-во оплат по kod_sum=55
            ExecSQL(conn_db, " Drop table tmp_ks55 ", false);
            ExecSQL(conn_db, " Drop table tmp_785 ", false);

            string table_counters = ""; //какой counters исправляем
            string charge_cnts = ""; //какой charge_cnts учитывается

            bool b_785 = false;

            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            if (!paramcalc.isPortal)
            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            {
                //настоящий расчет (==> paramcalc.isPortal <> true)

                table_counters = paramcalc.data_alias + "counters"; //реальная таблица counters
                charge_cnts = calc_chargeXX.charge_cnts_prev; //учет предыдущего (закрытого) charge_cnts

                //только оплаченые расчеты
                string pack_ls = Points.Pref + "_fin_" + (paramcalc.cur_yy - 2000).ToString("00") + tableDelimiter + "pack_ls ";

                ret = ExecSQL(conn_db,
                    " Select " + sUniqueWord + " a.nzp_chcnts, a.nzp_counter, a.num_ls, a.nzp_kvar, a.nzp_serv, a.nzp_cnttype, a.num_cnt, " +
                           " a.prev_val, a.cur_val, a.prev_dat, a.cur_dat " +
#if PG
 " Into temp tmp_ks55 " +
#endif
 " From " + charge_cnts + " a, " + pack_ls + " p, t_selkvar b " +
                    " Where a.nzp_kvar = b.nzp_kvar " +
                    "   and a.dat_charge = " + paramcalc.portal_dat_charge +
                    "   and a.iscalc = 0 " + //которые еще не учтены
                    "   and p.num_ls = b.num_ls " +
                    "   and a.id_bill = p.id_bill " +
                    "   and p.inbasket = 0 " +
                    "   and p.kod_sum = 55 " +
                    "   and p.dat_uchet >= " + paramcalc.dat_s +
                    "   and p.dat_uchet <= " + paramcalc.dat_po +
#if PG
 " "
#else
                    " Into temp tmp_ks55 With no log "
#endif
, true);
                if (!ret.result) { return; }

                //добавим условный учет counters_vals 
                string prm_5 = paramcalc.data_alias + "prm_5 ";
                string counters_spis = paramcalc.data_alias + "counters_spis ";

                IDataReader reader;

                ret = ExecRead(conn_db, out reader,
                    " Select * From " + prm_5 +
                    " Where nzp_prm = 785 " +
                    "   and is_actual <> 100 " +
                    "   and dat_s <= " + calc_chargeXX.paramcalc.dat_s +
                    "   and dat_po >= " + calc_chargeXX.paramcalc.dat_s
                    , true);
                if (!ret.result) { return; }

                try
                {
                    if (reader.Read())
                    {
                        b_785 = true; //учесть counters_vals, но запрещено замещать существующие показания counters
                    }
                }
                catch
                {
                }
                reader.Close();

                ret = ExecSQL(conn_db,
                    " Select " + sUniqueWord + " a.nzp_cv, a.nzp_counter, b.num_ls, b.nzp_kvar, c.nzp_serv, c.nzp_cnttype, c.num_cnt, " +
                      " a.dat_uchet, a.val_cnt, 0 as nzp_cr, 0 as kod " +
#if PG
 " Into temp tmp_785 " +
#endif
 " From " + calc_chargeXX.counters_vals + " a, t_selkvar b, " + counters_spis + " c " +
                    " Where a.nzp = b.nzp_kvar " +
                    "   and a.nzp_counter = c.nzp_counter " +
                    "   and a.month_ = " + paramcalc.calc_mm +
                    "   and a.iscalc in (0,2) " + //которые еще не учтены, 2 - значит был уже вызов, но не учелся из-за b_785=false
                    "   and a.nzp_type = 3 " +
                    "   and a.is_new = 1 and a.val_cnt is not null " + //надо предусмотреть, что если val_cnt = null, то значит показание было удалено
#if PG
                    ""
#else
                    " Into temp tmp_785 With no log "
#endif
, true);
                if (!ret.result) { return; }

                ret = ExecSQL(conn_db, " Create index ix1_tmp_785 on tmp_785 (nzp_kvar, nzp_counter) "
                    , true);
                if (!ret.result)
                {
                    ExecSQL(conn_db, " Drop table tmp_ks55 ", false);
                    ExecSQL(conn_db, " Drop table tmp_785 ", false);
                    return;
                }
                ret = ExecSQL(conn_db, " Create index ix2_tmp_785 on tmp_785 (nzp_cr,kod ) "
                    , true);
                if (!ret.result)
                {
                    ExecSQL(conn_db, " Drop table tmp_ks55 ", false);
                    ExecSQL(conn_db, " Drop table tmp_785 ", false);
                    return;
                }

                ExecSQL(conn_db, sUpdStat + " tmp_785 ", true);
            }
            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            else
            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            {
                //billpro'шный расчет (==> paramcalc.isPortal = true)

                table_counters = "temp_counters"; //временный counters
                charge_cnts = calc_chargeXX.charge_cnts; //учет текущего charge_cnts

                //безусловный учет данных текущего charge_cnts

                ret = ExecSQL(conn_db,
                    " Select " + sUniqueWord + " a.nzp_chcnts, a.nzp_counter, a.num_ls, a.nzp_kvar, a.nzp_serv, a.nzp_cnttype, a.num_cnt, " +
                      " a.prev_val, a.cur_val, a.prev_dat, a.cur_dat " +
#if PG
 " Into temp tmp_ks55 " +
#endif
 " From " + charge_cnts + " a, t_selkvar b " +
                    " Where a.nzp_kvar = b.nzp_kvar " +
                    "   and a.dat_charge = " + paramcalc.portal_dat_charge +
#if PG
 " "
#else
                    " Into temp tmp_ks55 With no log "
#endif
, true);
                if (!ret.result) { return; }
            }

            //построить индексы
            ret = ExecSQL(conn_db, " Create index ix2_tmp_ks55 on tmp_ks55 (nzp_chcnts) "
                , true);
            if (!ret.result)
            {
                ExecSQL(conn_db, " Drop table tmp_ks55 ", false);
                ExecSQL(conn_db, " Drop table tmp_785 ", false);
                return;
            }
            ret = ExecSQL(conn_db, " Create index ix1_tmp_ks55 on tmp_ks55 (nzp_kvar) "
                , true);
            if (!ret.result)
            {
                ExecSQL(conn_db, " Drop table tmp_ks55 ", false);
                ExecSQL(conn_db, " Drop table tmp_785 ", false);
                return;
            }
            ExecSQL(conn_db, sUpdStat + " tmp_ks55 ", true);

            string month_calc = MDY(Points.CalcMonth.month_, 1, Points.CalcMonth.year_);
            string sel_kvar = "";

            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            //сначала учтем counters_vals
            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

            ret = ExecSQL(conn_db,
                  " Update tmp_785 " + //=counters_vals
                  " Set nzp_cr = ( Select max(nzp_cr) From " + table_counters + " a " +
                                 " Where tmp_785.nzp_kvar = a.nzp_kvar " +
                                 "   and tmp_785.nzp_counter = a.nzp_counter " +
                                 "   and tmp_785.dat_uchet = a.dat_uchet " +
                                 "   and a.is_actual <> 100) " +
                 " Where 0 < ( Select max(1) From " + table_counters + " a " +
                                 " Where tmp_785.nzp_kvar = a.nzp_kvar " +
                                 "   and tmp_785.nzp_counter = a.nzp_counter " +
                                 "   and tmp_785.dat_uchet = a.dat_uchet " +
                                 "   and a.is_actual <> 100) "
                  , true);

            if (!ret.result)
            {
                ExecSQL(conn_db, " Drop table tmp_cnts ", false);
                ExecSQL(conn_db, " Drop table tmp_ks55 ", false);
                ExecSQL(conn_db, " Drop table tmp_785 ", false);
                return;
            }

            //отмечаем строки, где не совпали значения

            ret = ExecSQL(conn_db,
                " Update tmp_785 " +
                " Set kod = ( Select max(1) From " + table_counters + " a " +
                            " Where tmp_785.nzp_cr = a.nzp_cr " +
                            "   and abs(tmp_785.val_cnt - a.val_cnt) > 0.1 ) " +
               " Where 0 < ( Select max(1) From " + table_counters + " a " +
                            " Where tmp_785.nzp_cr = a.nzp_cr " +
                            "   and abs(tmp_785.val_cnt - a.val_cnt) > 0.1 ) "
                , true);

            if (!ret.result)
            {
                ExecSQL(conn_db, " Drop table tmp_cnts ", false);
                ExecSQL(conn_db, " Drop table tmp_ks55 ", false);
                ExecSQL(conn_db, " Drop table tmp_785 ", false);
                return;
            }

            if (b_785)  //учесть условно!
            {
                //менять существующие показания нельзя, внести только новые показания

                ret = ExecSQL(conn_db,
                           " Insert into " + table_counters +
                           " ( nzp_kvar, num_ls, nzp_serv, nzp_cnttype, num_cnt, dat_uchet, val_cnt, is_actual, nzp_user, dat_when, nzp_counter, ist, month_calc ) " +
                           " Select nzp_kvar, num_ls, nzp_serv, nzp_cnttype, num_cnt, dat_uchet, val_cnt, 1, -1, " +
                           sCurDate + ", nzp_counter, 4, " + month_calc +
                           " From tmp_785 " +
                           " Where nzp_cr = 0 "
                           , true);

                if (!ret.result)
                {
                    ExecSQL(conn_db, " Drop table tmp_cnts ", false);
                    ExecSQL(conn_db, " Drop table tmp_ks55 ", false);
                    ExecSQL(conn_db, " Drop table tmp_785 ", false);
                    return;
                }
            }
            else
            {
                //показания замещаются безусловно

                if (paramcalc.nzp_kvar > 0 || paramcalc.nzp_dom > 0 || calc_chargeXX.paramcalc.list_dom)
                    sel_kvar = "nzp_kvar in ( Select nzp_kvar From t_selkvar ) and ";

                //кинем в архив эти строки
                ret = ExecSQL(conn_db,
                    " Update " + table_counters +
                    " Set is_actual = 100 " +
                    " Where " +//nzp_kvar in ( Select nzp_kvar From t_selkvar ) and " +
                    sel_kvar +
                    "    0 < ( Select max(1) From tmp_785 " +
                             " Where tmp_785.nzp_cr = " + table_counters + ".nzp_cr " +
                             "   and tmp_785.kod = 1 ) "
                    , true);
                //, 100000, " ", out ret);

                if (!ret.result)
                {
                    ExecSQL(conn_db, " Drop table tmp_cnts ", false);
                    ExecSQL(conn_db, " Drop table tmp_ks55 ", false);
                    ExecSQL(conn_db, " Drop table tmp_785 ", false);
                    return;
                }

                //и наконец вставим строки, которых нет или изменены жителем

                ret = ExecSQL(conn_db,
                             " Insert into " + table_counters +
                             " ( nzp_kvar, num_ls, nzp_serv, nzp_cnttype, num_cnt, dat_uchet, val_cnt, is_actual, nzp_user, dat_when, nzp_counter, ist, month_calc ) " +
                             " Select nzp_kvar, num_ls, nzp_serv, nzp_cnttype, num_cnt, dat_uchet, val_cnt, 1, -1, " +
                             sCurDate + ", nzp_counter, 4, " + month_calc +
                             " From tmp_785 " +
                             " Where nzp_cr = 0 or (nzp_cr > 0 and kod = 1) "
                             , true);

                if (!ret.result)
                {
                    ExecSQL(conn_db, " Drop table tmp_cnts ", false);
                    ExecSQL(conn_db, " Drop table tmp_ks55 ", false);
                    ExecSQL(conn_db, " Drop table tmp_785 ", false);
                    return;
                }
            }


            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            //затем учтем charge_cnts
            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            ExecSQL(conn_db, " Drop table tmp_cnts ", false);

            //сначала выберем prev_dat

            ret = ExecSQL(conn_db,
                " Select " + sUniqueWord + " a.nzp_chcnts, a.nzp_counter, a.num_ls, a.nzp_kvar, a.nzp_serv, a.nzp_cnttype, a.num_cnt, " +
                  " a.prev_val as cur_val, a.prev_dat as cur_dat, 0 as nzp_cr, 0 as kod " +
#if PG
 " Into temp tmp_cnts " +
#endif
 " From tmp_ks55 a " +
                " Where a.prev_dat is not null and a.prev_val is not null " +
                "   and a.cur_dat is not null and a.cur_val is not null " +
                "   and a.cur_dat > a.prev_dat " +
#if PG
 " "
#else
                " Into temp tmp_cnts With no log "
#endif
, true);

            if (!ret.result)
            {
                ExecSQL(conn_db, " Drop table tmp_ks55 ", false);
                ExecSQL(conn_db, " Drop table tmp_785 ", false);
                return;
            }

            //затем cur_dat

            ret = ExecSQL(conn_db,
                    " Insert into tmp_cnts " +
                    " Select " + sUniqueWord + " a.nzp_chcnts, a.nzp_counter, a.num_ls, a.nzp_kvar, a.nzp_serv, a.nzp_cnttype, a.num_cnt, " +
                           " a.cur_val, a.cur_dat, 0 as nzp_cr, 0 as kod " +
                    " From tmp_ks55 a " +
                    " Where a.cur_dat is not null and a.cur_val is not null "
                    , true);

            if (!ret.result)
            {
                ExecSQL(conn_db, " Drop table tmp_cnts ", false);
                ExecSQL(conn_db, " Drop table tmp_ks55 ", false);
                ExecSQL(conn_db, " Drop table tmp_785 ", false);
                return;
            }


            ret = ExecSQL(conn_db, " Create index ix1_tmp_cnts on tmp_cnts (nzp_kvar, nzp_serv, nzp_cnttype, num_cnt, cur_dat) ", true);
            if (!ret.result)
            {
                ExecSQL(conn_db, " Drop table tmp_cnts ", false);
                ExecSQL(conn_db, " Drop table tmp_ks55 ", false);
                ExecSQL(conn_db, " Drop table tmp_785 ", false);
                return;
            }
            ret = ExecSQL(conn_db, " Create index ix2_tmp_cnts on tmp_cnts (nzp_cr, kod) "
                , true);
            if (!ret.result)
            {
                ExecSQL(conn_db, " Drop table tmp_cnts ", false);
                ExecSQL(conn_db, " Drop table tmp_ks55 ", false);
                ExecSQL(conn_db, " Drop table tmp_785 ", false);
                return;
            }

            ExecSQL(conn_db, sUpdStat + " tmp_cnts ", true);

            //далее выбрать строки из counters, которые будут безусловно исправляться

            ret = ExecSQL(conn_db,
                " Update tmp_cnts " + //= charge_cnts
                " Set nzp_cr = ( Select max(nzp_cr) From " + table_counters + " a " +
                               " Where tmp_cnts.nzp_kvar = a.nzp_kvar " +
                               "   and tmp_cnts.nzp_serv = a.nzp_serv " +
                               "   and tmp_cnts.nzp_cnttype = a.nzp_cnttype " +
                               "   and tmp_cnts.num_cnt = a.num_cnt " +
                               "   and tmp_cnts.cur_dat = a.dat_uchet " +
                               "   and a.is_actual <> 100) " +
               " Where 0 < ( Select max(1) From " + table_counters + " a " +
                               " Where tmp_cnts.nzp_kvar = a.nzp_kvar " +
                               "   and tmp_cnts.nzp_serv = a.nzp_serv " +
                               "   and tmp_cnts.nzp_cnttype = a.nzp_cnttype " +
                               "   and tmp_cnts.cur_dat = a.dat_uchet " +
                               "   and a.is_actual <> 100) "
                , true);

            if (!ret.result)
            {
                ExecSQL(conn_db, " Drop table tmp_cnts ", false);
                ExecSQL(conn_db, " Drop table tmp_ks55 ", false);
                ExecSQL(conn_db, " Drop table tmp_785 ", false);
                return;
            }

            //отмечаем строки, где не совпали значения

            ret = ExecSQL(conn_db,
                         " Update tmp_cnts " +
                         " Set kod = ( Select max(1) From " + table_counters + " a " +
                                     " Where tmp_cnts.nzp_cr = a.nzp_cr " +
                                     "   and abs(tmp_cnts.cur_val - a.val_cnt) > 0.1 ) " +
                        " Where 0 < ( Select max(1) From " + table_counters + " a " +
                                     " Where tmp_cnts.nzp_cr = a.nzp_cr " +
                                     "   and abs(tmp_cnts.cur_val - a.val_cnt) > 0.1 ) "
                         , true);

            if (!ret.result)
            {
                ExecSQL(conn_db, " Drop table tmp_cnts ", false);
                ExecSQL(conn_db, " Drop table tmp_ks55 ", false);
                ExecSQL(conn_db, " Drop table tmp_785 ", false);
                return;
            }

            sel_kvar = "";
            if (paramcalc.nzp_kvar > 0 || paramcalc.nzp_dom > 0 || calc_chargeXX.paramcalc.list_dom)
                sel_kvar = "nzp_kvar in ( Select nzp_kvar From t_selkvar ) and ";

            //кинем в архив эти строки

            ret = ExecSQL(conn_db,
                " Update " + table_counters +
                " Set is_actual = 100 " +
                " Where " + //+nzp_kvar in ( Select nzp_kvar From t_selkvar ) and " +
                sel_kvar +
                "   0 < ( Select max(1) From tmp_cnts " +
                            " Where tmp_cnts.nzp_cr = " + table_counters + ".nzp_cr " +
                            "   and tmp_cnts.kod = 1 ) "
                , true);

            if (!ret.result)
            {
                ExecSQL(conn_db, " Drop table tmp_cnts ", false);
                ExecSQL(conn_db, " Drop table tmp_ks55 ", false);
                ExecSQL(conn_db, " Drop table tmp_785 ", false);
                return;
            }

            //и наконец вставим строки, которых нет или изменены жителем

            ret = ExecSQL(conn_db,
                  " Insert into " + table_counters +
                  " ( nzp_kvar, num_ls, nzp_serv, nzp_cnttype, num_cnt, dat_uchet, val_cnt, is_actual, nzp_user, dat_when, nzp_counter, ist, month_calc ) " +
                  " Select nzp_kvar, num_ls, nzp_serv, nzp_cnttype, num_cnt, cur_dat, cur_val, 1, -1, " +
                  sCurDate + ", nzp_counter, 1, " + month_calc +
                  " From tmp_cnts " +
                  " Where nzp_cr = 0 or (nzp_cr > 0 and kod = 1) "
                  , true);

            if (!ret.result)
            {
                ExecSQL(conn_db, " Drop table tmp_cnts ", false);
                ExecSQL(conn_db, " Drop table tmp_ks55 ", false);
                ExecSQL(conn_db, " Drop table tmp_785 ", false);
                return;
            }

            //на всякий случай проставим nzp_counter, где =0, поскольку в billpro charge_cnts.nzp_counter раньше не заполнялся

            ret = ExecSQL(conn_db,
                " Update " + table_counters +
                " Set nzp_counter = ( " +
                               " Select max(nzp_counter) From " + paramcalc.data_alias + "counters_spis a " +
                               " Where " + table_counters + ".nzp_kvar = a.nzp " +
                               "   and " + table_counters + ".nzp_serv = a.nzp_serv " +
                               "   and " + table_counters + ".nzp_cnttype = a.nzp_cnttype " +
                               "   and " + table_counters + ".num_cnt = a.num_cnt " +
                               "   and a.is_actual <> 100 and a.nzp_type = 3 ) " +
                " Where nzp_kvar in ( Select nzp_kvar From t_selkvar ) " +
                "   and nzp_counter = 0 and is_actual <> 100 and dat_when = " + sCurDate +
                "   and 0 < ( Select max(1) From " + paramcalc.data_alias + "counters_spis a " +
                            " Where " + table_counters + ".nzp_kvar = a.nzp " +
                            "   and " + table_counters + ".nzp_serv = a.nzp_serv " +
                            "   and " + table_counters + ".nzp_cnttype = a.nzp_cnttype " +
                            "   and " + table_counters + ".num_cnt = a.num_cnt " +
                            "   and a.is_actual <> 100 and a.nzp_type = 3 ) "
                , true);

            if (!ret.result)
            {
                ExecSQL(conn_db, " Drop table tmp_cnts ", false);
                ExecSQL(conn_db, " Drop table tmp_ks55 ", false);
                ExecSQL(conn_db, " Drop table tmp_785 ", false);
                return;
            }

            //проставить флаг, в случае реального учета
            if (!paramcalc.isPortal)
            {
                ret = ExecSQL(conn_db,
                    " Update " + charge_cnts +
                    " Set iscalc = 1 " +
                    " Where nzp_chcnts in ( Select nzp_chcnts From tmp_ks55 ) "
                    , true);
                if (!ret.result)
                {
                    ExecSQL(conn_db, " Drop table tmp_cnts ", false);
                    ExecSQL(conn_db, " Drop table tmp_ks55 ", false);
                    ExecSQL(conn_db, " Drop table tmp_785 ", false);
                    return;
                }

                //counters_vals
                string s = "1";
                if (b_785) s = "2"; //не учлось, но запуск был

                ret = ExecSQL(conn_db,
                    " Update " + calc_chargeXX.counters_vals +
                    " Set iscalc = " + s +
                    " Where nzp_cv in ( Select nzp_cv From tmp_785 ) "
                    , true);

                if (!ret.result)
                {
                    ExecSQL(conn_db, " Drop table tmp_cnts ", false);
                    ExecSQL(conn_db, " Drop table tmp_ks55 ", false);
                    ExecSQL(conn_db, " Drop table tmp_785 ", false);
                    return;
                }
            }

            ExecSQL(conn_db, " Drop table tmp_cnts ", false);
            ExecSQL(conn_db, " Drop table tmp_ks55 ", false);
            ExecSQL(conn_db, " Drop table tmp_785 ", false);
        }
        //-----------------------------------------------------------------------------
        void Portal_UchetNedop(IDbConnection conn_db, CalcTypes.ParamCalc paramcalc, out Returns ret) //учет charge_nedo в nedop_kvar
        //-----------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            CalcTypes.ChargeXX calc_chargeXX = new CalcTypes.ChargeXX(paramcalc); //расчетная таблица начислений

            //выбрать мно-во оплат по kod_sum=55
            ExecSQL(conn_db, " Drop table tmp_ks55 ", false);

            string table_nedo = ""; //какой nedop_kvar исправляем
            string charge_nedo = ""; //какой charge_nedo учитывается

            if (!paramcalc.isPortal)
            {
                //настоящий расчет (==> paramcalc.isPortal <> true)

                table_nedo = paramcalc.data_alias + "nedop_kvar"; //реальная таблица nedop_kvar
                charge_nedo = calc_chargeXX.charge_nedo_prev; //учет предыдущего (закрытого) charge_nedo

                //только оплаченые расчеты
                string pack_ls = Points.Pref + "_fin_" + (paramcalc.cur_yy - 2000).ToString("00") + tableDelimiter + "pack_ls ";

                ret = ExecSQL(conn_db,
                    " Select " + sUniqueWord + " a.nzp_chnedo, a.num_ls, a.nzp_kvar, a.nzp_serv, a.nzp_kind, a.dat_s, a.dat_po, a.tn, 0 as nzp_nedop " +
#if PG
 " Into temp tmp_ks55 " +
#endif
 " From " + charge_nedo + " a, " + pack_ls + " p, t_selkvar b " +
                    " Where a.nzp_kvar = b.nzp_kvar " +
                    "   and a.dat_charge = " + paramcalc.portal_dat_charge +
                    "   and a.iscalc = 0 " + //которые еще не учтены
                    "   and p.num_ls = b.num_ls " +
                    "   and a.id_bill = p.id_bill " +
                    "   and p.inbasket = 0 " +
                    "   and p.kod_sum = 55 " +
                    "   and p.dat_uchet >= " + paramcalc.dat_s +
                    "   and p.dat_uchet <= " + paramcalc.dat_po +
#if PG
 " "
#else
                    " Into temp tmp_ks55 With no log "
#endif
, true);
                if (!ret.result) { return; }
            }
            else
            {
                //billpro'шный расчет (==> paramcalc.isPortal = true)

                table_nedo = "temp_table_nedop"; //временный nedop_kvar
                charge_nedo = calc_chargeXX.charge_nedo; //учет текущего charge_nedo

                //безусловный учет данных текущего charge_nedo

                ret = ExecSQL(conn_db,
                    " Select " + sUniqueWord + " a.nzp_chnedo, a.num_ls, a.nzp_kvar, a.nzp_serv, a.nzp_kind, a.dat_s, a.dat_po, a.tn, 0 as nzp_nedop " +
#if PG
 " Into temp tmp_ks55 " +
#endif
 " From " + charge_nedo + " a, t_selkvar b " +
                    " Where a.nzp_kvar = b.nzp_kvar " +
                    "   and a.dat_charge = " + paramcalc.portal_dat_charge +
#if PG
 " "
#else
                    " Into temp tmp_ks55 With no log "
#endif
, true);
                if (!ret.result) { return; }
            }

            //построить индексы
            ret = ExecSQL(conn_db, " Create index ix2_tmp_ks55 on tmp_ks55 (nzp_chnedo) ", true);
            if (!ret.result) { ExecSQL(conn_db, " Drop table tmp_ks55 ", false); return; }

            ret = ExecSQL(conn_db, " Create index ix1_tmp_ks55 on tmp_ks55 (nzp_kvar) ", true);
            if (!ret.result) { ExecSQL(conn_db, " Drop table tmp_ks55 ", false); return; }

            ret = ExecSQL(conn_db, " Create index ix3_tmp_ks55 on tmp_ks55 (nzp_nedop) ", true);
            if (!ret.result) { ExecSQL(conn_db, " Drop table tmp_ks55 ", false); return; }

            ExecSQL(conn_db, sUpdStat + " tmp_ks55 ", true);

            //далее выбрать строки из nedop_kvar, которые были внесены ранее (is_actual = 5)

            ret = ExecSQL(conn_db,
                        " Update tmp_ks55 " +
                        " Set nzp_nedop = ( Select max(nzp_nedop) From " + table_nedo + " a " +
                                       " Where tmp_ks55.nzp_kvar = a.nzp_kvar " +
                                       "   and tmp_ks55.nzp_serv = a.nzp_serv " +
                                       "   and tmp_ks55.dat_s = a.dat_s " +
                                       "   and tmp_ks55.dat_po = a.dat_po " +
                                       "   and tmp_ks55.nzp_kind = a.nzp_kind " +
                                       "   and a.is_actual = 5) " +
                       " Where 0 < ( Select max(1) From " + table_nedo + " a " +
                                       " Where tmp_ks55.nzp_kvar = a.nzp_kvar " +
                                       "   and tmp_ks55.nzp_serv = a.nzp_serv " +
                                       "   and tmp_ks55.dat_s = a.dat_s " +
                                       "   and tmp_ks55.dat_po = a.dat_po " +
                                       "   and tmp_ks55.nzp_kind = a.nzp_kind " +
                                       "   and a.is_actual = 5) "
                        , true);
            if (!ret.result) { ExecSQL(conn_db, " Drop table tmp_ks55 ", false); return; }

            ExecSQL(conn_db, sUpdStat + " tmp_ks55 ", true);

            //удалить эти строки
            /*
            ExecByStep(conn_db, table_nedo, "nzp_nedop",
                 " Delete From " + table_nedo +
                 " Where " + //nzp_kvar in ( Select nzp_kvar From t_selkvar ) and " +
                 sel_kvar +
                 "   0 < ( Select max(1) From tmp_ks55 " +
                             " Where tmp_ks55.nzp_nedop = " + table_nedo + ".nzp_nedop ) "
                //, true);
                 , 100000, " ", out ret);
            */

            if (calc_chargeXX.paramcalc.nzp_kvar > 0 || calc_chargeXX.paramcalc.nzp_dom > 0 || calc_chargeXX.paramcalc.list_dom)
            {

                string sql =
                              " Delete From " + table_nedo +
                              " Where " + //nzp_kvar in ( Select nzp_kvar From t_selkvar ) and " +
                                 calc_chargeXX.where_kvar +
                              "  and  0 < ( Select max(1) From tmp_ks55 " +
                                          " Where tmp_ks55.nzp_nedop = " + table_nedo + ".nzp_nedop ) ";

                ret = ExecSQL(conn_db, sql, true);
            }
            else
            {
                string sql =
                     " Delete From " + table_nedo +
                     " Where 0 < ( Select max(1) From tmp_ks55 " +
                                 " Where tmp_ks55.nzp_nedop = " + table_nedo + ".nzp_nedop ) ";
                ExecByStep(conn_db, table_nedo, "nzp_nedop", sql, 50000, " ", out ret);
            }
            if (!ret.result) { ExecSQL(conn_db, " Drop table tmp_ks55 ", false); return; }

            //и наконец вставим строки, которых нет или изменены жителем
            string month_calc = MDY(Points.CalcMonth.month_, 1, Points.CalcMonth.year_);

            ret = ExecSQL(conn_db,
                    " Insert into " + table_nedo +
                    " ( nzp_kvar, nzp_serv, nzp_supp, dat_s, dat_po, tn, is_actual, nzp_user, dat_when, nzp_kind, month_calc ) " +
                    " Select nzp_kvar, nzp_serv, 0, dat_s, dat_po, tn, 5, -1, " +
                    sCurDate + ", nzp_kind, " + month_calc +
                    " From tmp_ks55 "
                    , true);
            if (!ret.result) { ExecSQL(conn_db, " Drop table tmp_ks55 ", false); return; }

            //проставить флаг, в случае реального учета
            if (!paramcalc.isPortal)
            {
                ret = ExecSQL(conn_db,
                    " Update " + charge_nedo +
                    " Set iscalc = 1 " +
                    " Where nzp_chnedo in ( Select nzp_chnedo From tmp_ks55 ) "
                    , true);
                if (!ret.result) { ExecSQL(conn_db, " Drop table tmp_ks55 ", false); return; }
            }
            ExecSQL(conn_db, " Drop table tmp_ks55 ", false);
        }
        //-----------------------------------------------------------------------------
        void Portal_LoadData(IDbConnection conn_db, CalcTypes.ParamCalc paramcalc, out Returns ret) //загрузить charge_cnts
        //-----------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            CalcTypes.ChargeXX calc_chargeXX = new CalcTypes.ChargeXX(paramcalc); //расчетная таблица начислений

            //DbSprav dbs = new DbSprav();
            //string webfon = dbs.GetDbPortal();
            //dbs.Close();

            ret = ExecSQL(conn_db,
                " Delete From " + calc_chargeXX.charge_cnts +
                " Where nzp_kvar  = " + paramcalc.nzp_kvar +
                "   and id_bill = " + paramcalc.id_bill +
                "   and dat_charge = " + paramcalc.portal_dat_charge
                , true);
            if (!ret.result) { return; }

            if (paramcalc.id_bill != 1)
            {
                //НЕ первый расчет, надо скопировать данные из пред. расчетов
                IDataReader reader;

                //определим последний id_bill

                ret = ExecRead(conn_db, out reader,
                    " Select max(id_bill) as id_bill " +
                    " From " + calc_chargeXX.charge_cnts +
                    " Where nzp_kvar  = " + paramcalc.nzp_kvar +
                    "   and id_bill < " + paramcalc.id_bill +
                    "   and dat_charge = " + paramcalc.portal_dat_charge +
                    " Union " +
                    " Select max(id_bill) as id_bill " +
                    " From " + calc_chargeXX.charge_nedo +
                    " Where nzp_kvar  = " + paramcalc.nzp_kvar +
                    "   and id_bill < " + paramcalc.id_bill +
                    "   and dat_charge = " + paramcalc.portal_dat_charge
                    , true);
                if (!ret.result) { return; }

                int prev_id_bill = 0;
                try
                {
                    while (reader.Read())
                    {
                        int i = 0;
                        if (reader["id_bill"] != DBNull.Value)
                            i = (int)reader["id_bill"];
                        if (i > prev_id_bill)
                            prev_id_bill = i;
                    }
                }
                catch
                {
                }
                reader.Close();

                if (prev_id_bill > 0) //значит были расчеты, скопируем
                {
                    ExecSQL(conn_db, " Drop table tmp_cnts ", false);


                    ret = ExecSQL(conn_db,
                        " Select num_ls,nzp_kvar,id_bill,dat_charge,nzp_user,dat_when, " +
                        "   nzp_serv,nzp_cnttype,num_cnt,cur_dat,cur_val,prev_dat,prev_val,rashod, prev2_dat,nzp_counter " +
#if PG
 " Into temp tmp_cnts " +
#endif
 " From " + calc_chargeXX.charge_cnts +
                        " Where nzp_kvar  = " + paramcalc.nzp_kvar +
                        "   and id_bill = " + prev_id_bill +
                        "   and dat_charge = " + paramcalc.portal_dat_charge +
#if PG
 "  "
#else
                        " Into temp tmp_cnts With no log "
#endif
, true);
                    if (!ret.result) { return; }

                    ret = ExecSQL(conn_db,
                        " Insert into " + calc_chargeXX.charge_cnts +
                        " ( num_ls,nzp_kvar,id_bill,dat_charge,nzp_user,dat_when, " +
                        "   nzp_serv,nzp_cnttype,num_cnt,cur_dat,cur_val,prev_dat,prev_val,rashod, prev2_dat,nzp_counter )" +
                        " Select num_ls,nzp_kvar," + paramcalc.id_bill + ",dat_charge,nzp_user,dat_when, " +
                        "   nzp_serv,nzp_cnttype,num_cnt,cur_dat,cur_val,prev_dat,prev_val,rashod, prev2_dat,nzp_counter " +
                        " From tmp_cnts "
                        , true);
                    if (!ret.result) { return; }

                    ExecSQL(conn_db, " Drop table tmp_cnts ", false);


                    ret = ExecSQL(conn_db,
                        " Select num_ls,nzp_kvar,id_bill,dat_charge,nzp_serv,nzp_kind,dat_s,dat_po,tn,nzp_user,dat_when " +
#if PG
 " Into temp tmp_cnts " +
#endif
 " From " + calc_chargeXX.charge_nedo +
                        " Where nzp_kvar  = " + paramcalc.nzp_kvar +
                        "   and id_bill = " + prev_id_bill +
                        "   and dat_charge = " + paramcalc.portal_dat_charge +
#if PG
 " "
#else
                        " Into temp tmp_cnts With no log "
#endif
, true);
                    if (!ret.result) { return; }


                    ret = ExecSQL(conn_db,
                        " Insert into " + calc_chargeXX.charge_nedo +
                        " ( num_ls,nzp_kvar,id_bill,dat_charge,nzp_serv,nzp_kind,dat_s,dat_po,tn,nzp_user,dat_when ) " +
                        " Select num_ls,nzp_kvar," + paramcalc.id_bill + ",dat_charge,nzp_serv,nzp_kind,dat_s,dat_po,tn,nzp_user,dat_when " +
                        " From tmp_cnts "
                        , true);
                    if (!ret.result) { return; }

                    ExecSQL(conn_db, " Drop table tmp_cnts ", false);
                }
            }
            else
            {
                //первый расчет, заполним charge_cnts, charge_nedo

                //показание из будущего

                ret = ExecSQL(conn_db,
                    " Insert into " + calc_chargeXX.charge_cnts +
                    " ( nzp_chcnts,num_ls,nzp_kvar,id_bill,dat_charge,nzp_user,dat_when, " +
                    "   nzp_serv,nzp_cnttype,num_cnt,cur_dat,cur_val,prev_dat,prev_val,rashod, prev2_dat,nzp_counter ) " +
                    " Select 0, a.num_ls, " + paramcalc.nzp_kvar + "," + paramcalc.id_bill + "," + paramcalc.portal_dat_charge + "," +
                       "-1," + sCurDate + "," + "a.nzp_serv,a.nzp_cnttype,a.num_cnt,a.dat_uchet,a.val_cnt,'','','-', '',nzp_counter " +
                    " From " + paramcalc.data_alias + "counters a  " +
                    " Where " + sNvlWord + "(a.dat_prov," + MDY(1, 1, 1980) + ")<>" + MDY(1, 1, 1990) +
                    "   and a.is_actual <> 100 " +
                    "   and a.nzp_kvar = " + paramcalc.nzp_kvar +
                    "   and a.dat_close is null " +
                    "   and a.dat_uchet = ( " +
                    "     Select max(dat_uchet) From " + paramcalc.data_alias + "counters c " +
                    "     Where a.nzp_kvar=c.nzp_kvar       " +
                    "       and a.nzp_counter=c.nzp_counter       " +
                    "       and c.dat_uchet > " + paramcalc.dat_po +
                    "       and a.is_actual <> 100 and c.is_actual <> 100 " +
                    "       and c.dat_close is null )   " +
                        "   and 1 > ( Select count(*) From " + paramcalc.data_alias + "counters a1 " +
                                    " Where a.nzp_kvar    = a1.nzp_kvar " +
                                    "   and a.nzp_counter = a1.nzp_counter " +
                                    "   and a1.is_actual<>100           " +
                                    "   and a1.dat_close is not null )  "
                    , true);
                if (!ret.result) { return; }

                ret = ExecSQL(conn_db,
                        " Update " + calc_chargeXX.charge_cnts +
#if PG
 " Set prev_dat=" +
                        "     (Select a.dat_uchet " +
                        "     From " + paramcalc.pref + "_dat.counters a " +
                        "     Where " + calc_chargeXX.charge_cnts + ".nzp_kvar=a.nzp_kvar        " +
                        "       and " + calc_chargeXX.charge_cnts + ".nzp_counter=a.nzp_counter  " +
                        "       and coalesce(a.dat_prov, " + MDY(1, 1, 1980) + ")<>" + MDY(1, 1, 1990) +
                        "       and a.is_actual <> 100  " +
                        "       and a.dat_uchet=(                   " +
                        "           Select max(dat_uchet) From " + paramcalc.pref + "_data.counters c " +
                        "           Where a.nzp_kvar=c.nzp_kvar       " +
                        "             and a.nzp_counter=c.nzp_counter       " +
                        "             and c.dat_uchet <= " + paramcalc.dat_po +
                        "             and a.is_actual <> 100 and c.is_actual <> 100 " +
                        "             and c.dat_close is null ))   " +
                        " ,prev_val =   " +
                        "     (Select a.val_cnt " +
                        "     From " + paramcalc.pref + "_dat.counters a " +
                        "     Where " + calc_chargeXX.charge_cnts + ".nzp_kvar=a.nzp_kvar        " +
                        "       and " + calc_chargeXX.charge_cnts + ".nzp_counter=a.nzp_counter  " +
                        "       and coalesce(a.dat_prov," + MDY(1, 1, 1980) + ")<>" + MDY(1, 1, 1990) +
                        "       and a.is_actual <> 100  " +
                        "       and a.dat_uchet=(                   " +
                        "           Select max(dat_uchet) From " + paramcalc.pref + "_data.counters c " +
                        "           Where a.nzp_kvar=c.nzp_kvar       " +
                        "             and a.nzp_counter=c.nzp_counter       " +
                        "             and c.dat_uchet <= " + paramcalc.dat_po +
                        "             and a.is_actual <> 100 and c.is_actual <> 100 " +
                        "             and c.dat_close is null ))   " +
#else
                        " Set (prev_dat,prev_val) = ((  " +
                        "     Select a.dat_uchet,a.val_cnt " +
                        "     From " + paramcalc.pref + "_dat:counters a " +
                        "     Where " + calc_chargeXX.charge_cnts + ".nzp_kvar=a.nzp_kvar        " +
                        "       and " + calc_chargeXX.charge_cnts + ".nzp_counter=a.nzp_counter  " +
                        "       and nvl(a.dat_prov,mdy(1,1,1980))<>mdy(1,1,1990)" +
                        "       and a.is_actual <> 100  " +
                        "       and a.dat_uchet=(                   " +
                        "           Select max(dat_uchet) From " + paramcalc.pref + "_data:counters c " +
                        "           Where a.nzp_kvar=c.nzp_kvar       " +
                        "             and a.nzp_counter=c.nzp_counter       " +
                        "             and c.dat_uchet <= " + paramcalc.dat_po +
                        "             and a.is_actual <> 100 and c.is_actual <> 100 " +
                        "             and c.dat_close is null )   " +
                        "     )) " +
#endif
 " Where nzp_kvar  = " + paramcalc.nzp_kvar +
                        "   and id_bill = " + paramcalc.id_bill +
                        "   and dat_charge = " + paramcalc.portal_dat_charge
                        , true);
                if (!ret.result) { return; }

                ExecSQL(conn_db, " Drop table tmp_cnts ", false);

                ret = ExecSQL(conn_db,
                    " Select " + sUniqueWord + " num_ls,nzp_kvar, nzp_serv,nzp_cnttype,num_cnt,dat_uchet,val_cnt, nzp_counter " +
                    " From " + paramcalc.data_alias + "counters a " +
                    " Where nzp_kvar=" + paramcalc.nzp_kvar +
                    "   and is_actual <> 100 " +
                    "   and a.dat_close is null " +
                    "   and nzp_kvar not in ( " +
                    "   Select nzp_kvar From " + calc_chargeXX.charge_cnts + " b " +
                    "   Where a.nzp_kvar = b.nzp_kvar " +
                    "     and a.nzp_counter = b.nzp_counter " +
                    "     and id_bill = " + paramcalc.id_bill +
                    "     and dat_charge = " + paramcalc.portal_dat_charge +
                    "     and a.num_cnt = b.num_cnt      )  " +
                    "     and a.dat_uchet=(                   " +
                    "     Select max(dat_uchet) From " + paramcalc.data_alias + "counters c " +
                    "     Where a.nzp_kvar=c.nzp_kvar  " +
                    "       and a.nzp_counter=c.nzp_counter  " +
                    "       and c.dat_uchet <= " + paramcalc.dat_s +
                    "       and a.is_actual <> 100 and c.is_actual <> 100 " +
                    "       and c.dat_close is null )   " +
                    "   and 1 > ( Select count(*) From " + paramcalc.data_alias + "counters a1 " +
                        " Where a.nzp_kvar    = a1.nzp_kvar " +
                        "   and a.nzp_counter = a1.nzp_counter " +
                        "   and a1.is_actual<>100           " +
                        "   and a1.dat_close is not null )  " +
                    " Into temp tmp_cnts "
                    , true);
                if (!ret.result) { return; }

                ret = ExecSQL(conn_db,
                    " Insert into " + calc_chargeXX.charge_cnts +
                        " ( nzp_chcnts,num_ls,nzp_kvar,id_bill,dat_charge,nzp_user,dat_when, " +
                        "   nzp_serv,nzp_cnttype,num_cnt,cur_dat,cur_val,prev_dat,prev_val,rashod, nzp_counter ) " +
                    " Select 0,num_ls,nzp_kvar," + paramcalc.id_bill + "," + paramcalc.portal_dat_charge + ",-1," + sCurDate + ", " +
                    "   nzp_serv,nzp_cnttype,num_cnt," + paramcalc.next_dat_s + ",'',dat_uchet,val_cnt,'-', nzp_counter " +
                    " From tmp_cnts "
                    , true);
                if (!ret.result) { return; }

                ret = ExecSQL(conn_db,
                           " Update " + calc_chargeXX.charge_cnts +
                           " Set (prev2_dat) = ((  " +
                           "     Select max(a.dat_uchet) " +
                           "     From " + paramcalc.data_alias + "counters a " +
                           "     Where " + calc_chargeXX.charge_cnts + ".nzp_kvar=a.nzp_kvar       " +
                           "       and " + calc_chargeXX.charge_cnts + ".nzp_counter=a.nzp_counter " +
                           "       and " + sNvlWord + "(a.dat_prov," + MDY(1, 1, 1980) + ")<>" + MDY(1, 1, 1990) +
                           "       and a.is_actual <> 100  " +
                           "       and a.dat_close is null " +
                           "       and a.dat_uchet < " + calc_chargeXX.charge_cnts + ".prev_dat " +
                           "     )) " +
                           " Where nzp_kvar  = " + paramcalc.nzp_kvar +
                           "   and id_bill = " + paramcalc.id_bill +
                           "   and dat_charge = " + paramcalc.portal_dat_charge
                           , true);
                if (!ret.result) { return; }

                ExecSQL(conn_db, " Drop table tmp_cnts ", false);
            }
        }
        #endregion Функции работы с Порталовским таблицыми (charge_cnts etc.)

        #region выборка ЛС для расчетов - ChoiseTempKvar
        //--------------------------------------------------------------------------------
        public void ChoiseTempKvar(IDbConnection conn_db, ref CalcTypes.ParamCalc paramcalc, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ChoiseTempKvar(conn_db, ref paramcalc, true, out ret);
        }
        //--------------------------------------------------------------------------------

        public void ChoiseTempKvar(IDbConnection conn_db, ref CalcTypes.ParamCalc paramcalc, bool must_calc, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            if (Constants.Trace) Utility.ClassLog.WriteLog("Старт функции ChoiseTempKvar");

            #region подготовка выборки ЛС
            string ssql = "";
            ret = Utils.InitReturns();

            ExecSQL(conn_db, " Drop table t_opn ", false);  // CASCADE
            ExecSQL(conn_db, " Drop table t_selkvar ", false); // CASCADE
            ExecSQL(conn_db, " Drop table ttt_prm_3 ", false);  // CASCADE
            //препарированные таблицы
            if (paramcalc.b_loadtemp)
            {
                DropTempTables(conn_db, paramcalc.pref);

            }

            ret = ExecSQL(conn_db, " select * from ttt_prm_3 ", false);
            if (!ret.result)
            {
                ssql =
                    " Create temp table ttt_prm_3 " +
                    " ( nzp      integer, " +
                    "   nzp_dom  integer, " +
                    "   num_ls   integer, " +
                    "   dat_s    date not null, " +
                    "   dat_po   date not null  " +
                    " )" + sUnlogTempTable;
            }
            else
            {
                ssql = " truncate table ttt_prm_3 ";
            }
            ret = ExecSQL(conn_db, ssql, true);
            if (!ret.result) { return; }

            ret = ExecSQL(conn_db, " select * from t_selkvar ", false);
            if (!ret.result)
            {
                ssql =
                    " Create temp table t_selkvar " +
                    " ( nzp_key   serial not null, " +
                    "   nzp_kvar  integer, " +
                    "   num_ls    integer, " +
                    "   nzp_dom   integer, " +
                    "   nzp_area  integer, " +
                    "   nzp_geu   integer  " +
                    " )" + sUnlogTempTable;
            }
            else
            {
                ssql = " truncate table t_selkvar ";
            }
            ret = ExecSQL(conn_db, ssql, true);
            if (!ret.result) { return; }

            ret = ExecSQL(conn_db, " select * from t_opn ", false);
            if (!ret.result)
            {
                ssql =
                    " Create temp table t_opn " +
                    " ( nzp_kvar integer," +
                    "   nzp_dom  integer," +
                    "   num_ls   integer, " +
                    "   is_day_calc integer default 0, " +
                    "   dat_s    date not null," +
                    "   dat_po   date not null" +
                    " )" + sUnlogTempTable;
            }
            else
            {
                ssql = " truncate table t_opn ";
            }
            ret = ExecSQL(conn_db, ssql, true);
            if (!ret.result) { return; }

            ExecSQL(conn_db, " Drop table t_mc_ls ", false);

            #endregion подготовка выборки ЛС

            #region определение объема выборки ЛС
            string s_must = "";

            if (must_calc)
            {
                ChoiseTempMustCalc(conn_db, paramcalc, out ret);
                if (!ret.result) { return; }
            }

            if (paramcalc.b_must && must_calc) //доп. ограничение на выборку данных
            {
                s_must = " and k.nzp_kvar in ( Select nzp_kvar From t_mc_ls ) ";

                ExecSQL(conn_db, " Drop table t_mc_doms ", false);
                ret = ExecSQL(conn_db, " Create temp table t_mc_doms ( nzp_dom integer )" + sUnlogTempTable, true);
                if (!ret.result) { return; }

                ret = ExecSQL(conn_db,
                    " Insert into t_mc_doms (nzp_dom) " +
                    " select k.nzp_dom " +
                    " from " + paramcalc.data_alias + "prm_2 p, " + paramcalc.data_alias + "kvar k, t_mustcalc m " +
                    " Where k." + paramcalc.where_z +
                    " and k.nzp_kvar=m.nzp_kvar and k.nzp_dom=p.nzp and p.nzp_prm in (2031,2035,2039,2045,2062) " +
                    " and p.is_actual<>100 and p.dat_s<" + paramcalc.dat_po + " and p.dat_po>=" + paramcalc.dat_s +
                    " group by 1 "
                    , true);
                if (!ret.result) { return; }
                ExecSQL(conn_db, " Create index ix_t_mc_doms on t_mc_doms (nzp_dom) ", false);
                ExecSQL(conn_db, sUpdStat + " t_mc_doms ", true);

                IDbCommand cmdTstDoms = DBManager.newDbCommand(" select count(*) cnt from t_mc_doms ", conn_db);

                bool bmust_calc;
                try
                {
                    string scntvals = Convert.ToString(cmdTstDoms.ExecuteScalar());
                    bmust_calc = (Convert.ToInt32(scntvals) > 0);
                }
                catch
                {
                    bmust_calc = false;
                }

                ret = ExecSQL(conn_db, " Create temp table t_mc_ls ( nzp_kvar integer )" + sUnlogTempTable, true);
                if (!ret.result) { return; }

                if (bmust_calc)
                {
                    ret = ExecSQL(conn_db,
                        " Insert into t_mc_ls (nzp_kvar)" +
                        " select k.nzp_kvar" +
                        " from t_mc_doms d, " + paramcalc.data_alias + "kvar k " +
                        " where d.nzp_dom=k.nzp_dom " +
                        " group by 1 "
                        , true);
                    if (!ret.result) { return; }
                }
                ExecSQL(conn_db, " Create index ix_t_mc_ls on t_mc_ls (nzp_kvar) ", false);
                ExecSQL(conn_db, sUpdStat + " t_mc_ls ", true);

                ret = ExecSQL(conn_db,
                    " Insert into t_mc_ls (nzp_kvar) " +
                    " select m.nzp_kvar " +
                    " from t_mustcalc m " +
                    " where 0=(select count(*) from t_mc_ls b where b.nzp_kvar=m.nzp_kvar) "
                    , true);
                if (!ret.result) { return; }

                ExecSQL(conn_db, sUpdStat + " t_mc_ls ", true);
                ExecSQL(conn_db, " Drop table t_tst_doms ", false);
            }
            //расчет сальдо после распределения пачки
            if (paramcalc.nzp_pack_saldo > 0)
            {
                string pack_ls_table = Points.Pref + "_fin_" +
                    (paramcalc.calc_yy - 2000).ToString("00") + paramcalc.ol_srv + tableDelimiter + "pack_ls ";
                s_must = " and k.num_ls in ( Select num_ls From " + pack_ls_table +
                    " Where nzp_pack = " + paramcalc.nzp_pack_saldo + " ) ";
            }

            if (paramcalc.nzp_par_pack > 0)
            {
                string pack_ls_table = Points.Pref + "_fin_" +
                    (paramcalc.calc_yy - 2000).ToString("00") + paramcalc.ol_srv + tableDelimiter + "pack_ls ";
                string pack_table = Points.Pref + "_fin_" +
                   (paramcalc.calc_yy - 2000).ToString("00") + paramcalc.ol_srv + tableDelimiter + "pack ";

                s_must = string.Format(" and k.num_ls in ( Select kv.num_ls From {0} pls, {1} p, {2} kv where pls.num_ls = kv.num_ls and " +
                                       " p.nzp_pack = pls.nzp_pack and par_pack <> p.nzp_pack and par_pack = {3} and kv.pref = '{4}')",
                                       pack_ls_table, pack_table, Points.Pref + sDataAliasRest + "kvar", paramcalc.nzp_par_pack, paramcalc.pref);
            }

            if (paramcalc.nzp_reestr > 0)
            {
                if (paramcalc.temp_table != "")
                {
                    s_must = " and k.num_ls in (select nzp_kvar from " + paramcalc.temp_table + " where pref = '" + paramcalc.pref + "')";
                }
                else
                {
                    string perekidka = paramcalc.pref + "_charge_" + (paramcalc.calc_yy - 2000).ToString("00") + paramcalc.ol_srv + tableDelimiter + "perekidka";
                    s_must = " and k.num_ls in (select num_ls from " + perekidka +
                        " Where nzp_reestr = " + paramcalc.nzp_reestr + ")";
                }
            }
            #endregion определение объема выборки ЛС

            #region выборка всех ЛС (открытых/закрытых/неопределенных) - t_selkvar
            //+++++++++++++++++++++++++++++++++++++++++++++++++++
            //выбрать множество лицевых счетов
            //+++++++++++++++++++++++++++++++++++++++++++++++++++
            string s_find =
                " From " + paramcalc.data_alias + "kvar k " +
                " Where " + paramcalc.where_z + s_must;

            if (paramcalc.nzp_dom > 0 || paramcalc.list_dom)
            {
                if (!paramcalc.list_dom)
                {
                    s_find =
                        " From " + paramcalc.data_alias + "kvar k " +
                        " Where k.nzp_dom=" + paramcalc.nzp_dom + s_must;

                    // ... для расчета связанные дома ... 
                    IDbCommand cmd_link = DBManager.newDbCommand(
                        " Select max(nzp_dom_base) From " + paramcalc.data_alias + "link_dom_lit p " +
                        " Where nzp_dom=" + paramcalc.nzp_dom + " "
                    , conn_db);

                    int nzp_dom_base = 0;
                    try
                    {
                        string sndb = Convert.ToString(cmd_link.ExecuteScalar());
                        nzp_dom_base = Convert.ToInt32(sndb);
                    }
                    catch
                    {
                        nzp_dom_base = 0;
                    }

                    if (nzp_dom_base > 0)
                    {

                        s_find =
                        " From " + paramcalc.data_alias + "kvar k, " + paramcalc.data_alias + "link_dom_lit l " +
                        " Where k.nzp_dom=l.nzp_dom and l.nzp_dom_base=" + nzp_dom_base + s_must;
                    }
                }
                else //расчет по списку домов, в которых оказались связанные дома
                {
                    //список базовых домов
                    ExecSQL(conn_db, "drop table t_list_base_houses", false);
                    var sql = "create temp table t_list_base_houses (nzp_dom integer)" + sUnlogTempTable;
                    ret = ExecSQL(conn_db, sql, true);
                    if (!ret.result) { return; }

                    //необходимый список домов 
                    ExecSQL(conn_db, "drop table t_list_need_houses", false);
                    sql = "create temp table t_list_need_houses (nzp_dom integer)" + sUnlogTempTable;
                    ret = ExecSQL(conn_db, sql, true);
                    if (!ret.result) { return; }

                    //получили список базовых домов
                    sql = " insert into t_list_base_houses (nzp_dom ) " +
                          " select l.nzp_dom_base from " + paramcalc.data_alias + "link_dom_lit l where " +
                          paramcalc.where_z + " group by 1";
                    ret = ExecSQL(conn_db, sql, true);
                    if (!ret.result) { return; }

                    //записали в список домов связанные дома 
                    sql = " insert into t_list_need_houses (nzp_dom) " +
                          " select nzp_dom from " + paramcalc.data_alias + "link_dom_lit " +
                          " where nzp_dom_base in (select nzp_dom from t_list_base_houses)";
                    ret = ExecSQL(conn_db, sql, true);
                    if (!ret.result) { return; }

                    //записали выбранные дома
                    sql = " insert into t_list_need_houses (nzp_dom) " +
                          " select nzp_dom from " + paramcalc.data_alias + "dom " +
                          " where " + paramcalc.where_z;
                    ret = ExecSQL(conn_db, sql, true);
                    if (!ret.result) { return; }

                    //конечный список домов 
                    ExecSQL(conn_db, "drop table t_list_final_houses", false);
                    sql = "create temp table t_list_final_houses (nzp_dom integer)" + sUnlogTempTable;
                    ret = ExecSQL(conn_db, sql, true);
                    if (!ret.result) { return; }

                    //получили окончательный список
                    sql = " insert into t_list_final_houses (nzp_dom) " +
                          " select nzp_dom from t_list_need_houses group by 1";
                    ret = ExecSQL(conn_db, sql, true);
                    if (!ret.result) { return; }

                    s_find = " From " + paramcalc.data_alias + "kvar k, t_list_final_houses l " +
                       " Where k.nzp_dom=l.nzp_dom ";
                }
            }
            if (paramcalc.nzp_kvar > 0)
            {
                s_find =
                    " From " + paramcalc.data_alias + "kvar k " +
                    " Where k.nzp_kvar=" + paramcalc.nzp_kvar + s_must;
            }


            ret = ExecSQL(conn_db,
                " Insert into t_selkvar (nzp_kvar,num_ls,nzp_dom, nzp_area,nzp_geu) " +
                " Select k.nzp_kvar,k.num_ls,k.nzp_dom,k.nzp_area,k.nzp_geu " + s_find
                , true);
            if (!ret.result) { return; }

            ExecSQL(conn_db, " Create unique index ix1_t_selkvar on t_selkvar (nzp_key) ", false);
            ExecSQL(conn_db, " Create unique index ix2_t_selkvar on t_selkvar (nzp_kvar) ", false);
            ExecSQL(conn_db, " Create        index ix3_t_selkvar on t_selkvar (num_ls) ", false);
            ExecSQL(conn_db, " Create        index ix4_t_selkvar on t_selkvar (nzp_dom) ", false);
            ExecSQL(conn_db, sUpdStat + " t_selkvar ", true);

            //ViewTbl(conn_db, " select * from t_selkvar order by nzp_dom,nzp_kvar ");

            #endregion выборка всех ЛС (открытых/закрытых/неопределенных) - t_selkvar

            #region выборка открытых ЛС - t_opn
            ExecSQL(conn_db, " Drop table t_mc_ls ", false);
            //+++++++++++++++++++++++++++++++++++++++++++++++++++
            // выбрать открытые лицевые счета по t_selkvar
            //+++++++++++++++++++++++++++++++++++++++++++++++++++

            ret = ExecSQL(conn_db,
                " Insert into ttt_prm_3 (nzp, nzp_dom, num_ls, dat_s, dat_po)" +
                " Select k.nzp_kvar, k.nzp_dom, k.num_ls, " +
                sNvlWord + "(p.dat_s," + MDY(12, 31, 1899) + "), " + sNvlWord + "(p.dat_po," + MDY(1, 1, 3000) + ") " +
                " From t_selkvar k," + paramcalc.data_alias + "prm_3 p" +
                " Where k.nzp_kvar=p.nzp and p.nzp_prm=51 and p.val_prm in ('1','3') " +
                "   and p.is_actual<>100 " +
                "   and p.dat_s  <= " + paramcalc.dat_po +
                "   and p.dat_po >= " + paramcalc.dat_s +
                " group by 1,2,3,4,5 "
                , true);
            if (!ret.result) { return; }

            ExecSQL(conn_db, " Create unique index ix1_ttt_prm_3 on ttt_prm_3 (nzp,nzp_dom,num_ls) ", false);
            ExecSQL(conn_db, " Create        index ix2_ttt_prm_3 on ttt_prm_3 (nzp,dat_s,dat_po) ", false);
            ExecSQL(conn_db, " Create        index ix3_ttt_prm_3 on ttt_prm_3 (dat_s,dat_po) ", false);
            ExecSQL(conn_db, sUpdStat + " ttt_prm_3 ", true);

            bool bDayCalc =
                CheckValBoolPrmWithVal(conn_db, paramcalc.data_alias, 89, "10", "1", paramcalc.dat_s, paramcalc.dat_po);

            if (bDayCalc)
            {
                ret = ExecSQL(conn_db,
                " Insert into t_opn (nzp_kvar, nzp_dom, num_ls, dat_s, dat_po, is_day_calc) " +
                " Select k.nzp, k.nzp_dom, k.num_ls, min(k.dat_s), max(k.dat_po), 1 " +
                " From ttt_prm_3 k " +
                " group by 1,2,3 "
                , true);
                if (!ret.result) { return; }

                ExecSQL(conn_db, " Create unique index ix1_t_opn on t_opn (nzp_kvar) ", false);
            }
            else
            {
                ret = ExecSQL(conn_db,
                " Insert into t_opn (nzp_kvar, nzp_dom, num_ls, dat_s, dat_po, is_day_calc) " +
                " Select k.nzp, k.nzp_dom, k.num_ls, min(k.dat_s), max(k.dat_po), 1 " +
                " From ttt_prm_3 k, " + paramcalc.data_alias + "prm_1 p " +
                " Where k.nzp=p.nzp and p.nzp_prm=90 and p.val_prm='1' " +
                "   and p.is_actual<>100 " +
                "   and p.dat_s  <= " + paramcalc.dat_po +
                "   and p.dat_po >= " + paramcalc.dat_s +
                " group by 1,2,3 "
                , true);
                if (!ret.result) { return; }

                ExecSQL(conn_db, " Create unique index ix1_t_opn on t_opn (nzp_kvar) ", false);
                ExecSQL(conn_db, sUpdStat + " t_opn ", true);

                ret = ExecSQL(conn_db,
                " Insert into t_opn (nzp_kvar, nzp_dom, num_ls, dat_s, dat_po, is_day_calc) " +
                " Select k.nzp, k.nzp_dom, k.num_ls, min(k.dat_s), max(k.dat_po), 0 " +
                " From ttt_prm_3 k " +
                " where 0=( select count(*) From t_opn n Where k.nzp=n.nzp_kvar ) " +
                " group by 1,2,3 "
                , true);
                if (!ret.result) { return; }
            }

            ExecSQL(conn_db, " Create        index ix2_t_opn on t_opn (num_ls) ", false);
            ExecSQL(conn_db, " Create        index ix3_t_opn on t_opn (nzp_dom) ", false);
            ExecSQL(conn_db, sUpdStat + " t_opn ", true);

            #endregion выборка открытых ЛС - t_opn

            #region переопределим nzp_dom для расчета одного ЛС
            //переопределим nzp_dom
            if (paramcalc.nzp_kvar > 0)
            {
                MyDataReader reader;
                ret = ExecRead(conn_db, out reader,
                    " Select nzp_dom From t_selkvar Where nzp_kvar = " + paramcalc.nzp_kvar
                    , true);
                if (!ret.result) { return; }
                try
                {
                    if (reader.Read())
                    {
                        paramcalc.nzp_dom = (int)reader["nzp_dom"];
                    }
                }
                catch
                {
                    ret.result = false;
                    reader.Close();
                    return;
                }
                reader.Close();
            }
            #endregion переопределим nzp_dom для расчета одного ЛС

            if (Constants.Trace) Utility.ClassLog.WriteLog("Стоп функции ChoiseTempKvar");
        }
        #endregion выборка ЛС для расчетов - ChoiseTempKvar

        #region Выполнение расчета с установкой соединения и предварительной установкой параметров расчета
        public void CalcFull(int nzp_kvar, string pref, int calc_yy, int calc_mm, int cur_yy, int cur_mm, bool[] clc, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            CalcTypes.ParamCalc paramcalc = new CalcTypes.ParamCalc(nzp_kvar, 0, pref, calc_yy, calc_mm, cur_yy, cur_mm);
            //paramcalc.b_again = clc[6];
            paramcalc.b_reval = clc[7];
            paramcalc.b_must = false;
            if (clc.Count() > 8)
                paramcalc.b_must = clc[8];

            #region Выполнить расчет с установкой соединения
            CalcFull(paramcalc, out ret);
            #endregion Выполнить расчет с установкой соединения
        }
        #endregion Выполнение расчета с установкой соединения и предварительной установкой параметров расчета
        //--------------------------------------------------------------------------------
        #region Выполнение расчета с установкой соединения
        public void CalcFull(CalcTypes.ParamCalc paramcalc, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            #region Получить данные по соединению
            string conn_kernel = Points.GetConnByPref(paramcalc.pref);
            IDbConnection conn_db = GetConnection(conn_kernel);

            ret = OpenDb(conn_db, true);
            if (!ret.result) { return; }

            #endregion Получить данные по соединению

            #region выбрать мно-во лицевых счетов

            ChoiseTempKvar(conn_db, ref paramcalc, out ret);
            if (!ret.result) { conn_db.Close(); return; }

            #endregion выбрать мно-во лицевых счетов

            #region Выполнить расчет

            CalcFull(conn_db, paramcalc, out ret);
            if (!ret.result) { conn_db.Close(); return; }

            #endregion Выполнить расчет

            #region Удалить временные таблицы и закрыть соединение

            ExecSQL(conn_db, " Drop table t_opn ", false);
            ExecSQL(conn_db, " Drop table t_selkvar ", false);
            ExecSQL(conn_db, " Drop table t_mustcalc ", false);

            DropTempTables(conn_db, paramcalc.pref);

            conn_db.Close();

            #endregion Удалить временные таблицы
        }
        #endregion Выполнение расчета с установкой соединения
        //--------------------------------------------------------------------------------
        #region Выполнение расчета без установки соединения
        public void CalcFull(IDbConnection conn_db, CalcTypes.ParamCalc paramcalc, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            #region Выполнение расчета по этапам
            ret = Utils.InitReturns();
            try
            {

                string s = paramcalc.pref + " " + paramcalc.nzp_kvar + "/" + paramcalc.nzp_dom + "/" +
                           paramcalc.calc_yy + "/" + paramcalc.calc_mm + "/" +
                           paramcalc.cur_yy + "/" + paramcalc.cur_mm;

                MonitorLog.WriteLog("Старт! " + s, MonitorLog.typelog.Info, 1, 2, true);

                if (paramcalc.b_refresh)
                {

                }
                else
                {
                    var cur_month = (paramcalc.cur_yy == paramcalc.calc_yy) && (paramcalc.cur_mm == paramcalc.calc_mm);
                    //расчеты
                    if (paramcalc.b_gil)
                    {
                        if (paramcalc.count_calc_months > 0)
                            SetStatusCalcTask(conn_db, paramcalc, (0.8m / (decimal)paramcalc.count_calc_months) * 0.1m, CalcTypes.CalcSteps.CalcGil);
                        MonitorLog.WriteLog("Старт CalcGilXX: " + s, MonitorLog.typelog.Info, 1, 2, true);
                        if (!CalcGilXX(conn_db, paramcalc, out ret))
                        {
                            conn_db.Close();
                            return;
                        }
                    }
                    if (paramcalc.b_rashod)
                    {
                        if (paramcalc.count_calc_months > 0)
                            SetStatusCalcTask(conn_db, paramcalc, (0.8m / (decimal)paramcalc.count_calc_months) * 0.3m, CalcTypes.CalcSteps.CalcRashod);
                        MonitorLog.WriteLog("Старт CalcRashod: " + s, MonitorLog.typelog.Info, 1, 2, true);
                        if (!CalcRashod(conn_db, paramcalc, out ret))
                        {
                            conn_db.Close();
                            return;
                        }
                    }
                    if (paramcalc.b_nedo)
                    {
                        if (paramcalc.count_calc_months > 0)
                            SetStatusCalcTask(conn_db, paramcalc, (0.8m / (decimal)paramcalc.count_calc_months) * 0.1m, CalcTypes.CalcSteps.CalcNedo);
                        MonitorLog.WriteLog("Старт CalcNedo: " + s, MonitorLog.typelog.Info, 1, 2, true);
                        if (!CalcNedoXX(conn_db, paramcalc, out ret))
                        {
                            conn_db.Close();
                            return;
                        }
                    }
                    if (paramcalc.b_gku)
                    {
                        if (paramcalc.count_calc_months > 0)
                            SetStatusCalcTask(conn_db, paramcalc, (0.8m / (decimal)paramcalc.count_calc_months) * 0.1m, CalcTypes.CalcSteps.CalcGku);
                        MonitorLog.WriteLog("Старт CalcGkuXX: " + s, MonitorLog.typelog.Info, 1, 2, true);
                        if (!CalcGkuXX(conn_db, paramcalc, out ret))
                        {
                            conn_db.Close();
                            return;
                        }
                        #region Вызов расчета ПЕНИ
                        if (paramcalc.count_calc_months > 0)
                            SetStatusCalcTask(conn_db, paramcalc, (0.8m / (decimal)paramcalc.count_calc_months) * 0.2m, CalcTypes.CalcSteps.CalcPeni);
                        MonitorLog.WriteLog("Старт CalcRasPeni: " + s, MonitorLog.typelog.Info, 1, 2, true);
                        // Важное условие входа функцию , расчет текущего сальдового месяца , а не перерасчетных месяцев
                        if (cur_month)
                        {
                            using (var DbCalcPeni = new DbCalcPeni())
                            {
                                //расчет пени по новому алгоритму
                                if (DbCalcPeni.GetParamIsNewPeni(conn_db, paramcalc, 1382))
                                {
                                    if (!DbCalcPeni.CalcPeniMain(conn_db, paramcalc, out ret))
                                    {
                                        conn_db.Close();
                                        return;
                                    }
                                }
                                else
                                {
                                    if (!CalcRasPeni(conn_db, paramcalc, out ret))
                                    {
                                        conn_db.Close();
                                        return;
                                    }
                                }
                            }
                        }
                        #endregion Вызов расчета ПЕНИ
                    }

                    if (paramcalc.b_charge)
                    {
                        if (paramcalc.count_calc_months > 0)
                            SetStatusCalcTask(conn_db, paramcalc, (0.8m / (decimal)paramcalc.count_calc_months) * 0.2m, CalcTypes.CalcSteps.CalcCharge);
                        MonitorLog.WriteLog("Старт CalcChargeXX: " + s, MonitorLog.typelog.Info, 1, 2, true);
                        if (!CalcChargeXX(conn_db, paramcalc, out ret))
                        {
                            conn_db.Close();
                            return;
                        }
                    }

                    if (paramcalc.b_report)
                    {
                        if (paramcalc.calcfon.callReportAlone)
                        {
                            //вызывется после всех через отдельный поток
                        }
                        else
                        {
                            if (!paramcalc.b_reval)
                            {
                                MonitorLog.WriteLog("Старт CalcReportXX: " + s, MonitorLog.typelog.Info, 1, 2, true);
                                CalcReportXX(conn_db, paramcalc, out ret);
                            }
                        }
                    }
                }
                MonitorLog.WriteLog("Стоп! " + s, MonitorLog.typelog.Info, 1, 2, true);
            }
            catch (Exception ex)
            {

                Returns ret11 = STCLINE.KP50.Utility.ExceptionUtility.OnException(ex, System.Reflection.MethodBase.GetCurrentMethod().Name);
            }
            #endregion Выполнение расчета по этапам

        }
        #endregion Выполнение расчета без установки соединения

        #region Функции обработки запрета расчета по связке ЛС/Услуга/Поставщик(договор)
        protected void CreateTempProhibitedBegin(IDbConnection conn_db, out Returns ret)
        {
            ExecSQL(conn_db, " DROP TABLE temp_prohibited_recalc_begin; ", false);
            ExecSQL(conn_db,
             " CREATE TEMP TABLE temp_prohibited_recalc_begin (" +
             "   nzp_kvar  integer," +
             "   nzp_dom   integer," +
             "   nzp_serv  integer," +
             "   nzp_supp  integer," +
             "   dat_s  date, " +
             "   dat_po  date" +
             " ) " + sUnlogTempTable
             , true);
            ret = ExecSQL(conn_db,
              " Create index itemp_prohibited_recalc_begin on temp_prohibited_recalc_begin (nzp_kvar,nzp_serv,nzp_supp); ",
              true);
            if (!ret.result)
            {
                conn_db.Close();
                return;
            }

            var sql = "INSERT INTO temp_prohibited_recalc_begin" +
                 "(nzp_kvar,nzp_dom,nzp_serv,nzp_supp,dat_s,dat_po) " +
                 " select nzp_kvar,nzp_dom,nzp_serv,nzp_supp,dat_s,dat_po FROM " + Points.Pref + sDataAliasRest + "prohibited_recalc  " +
                 " WHERE is_actual<>100 and " +
                 " nzp_kvar in ( Select nzp_kvar From t_selkvar)";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result) { return; }
        }


        public void CreateTempProhibitedRecalc(IDbConnection conn_db, CalcTypes.ParamCalc paramcalc, out Returns ret)
        {
            Utils.InitReturns();
            ExecSQL(conn_db, " DROP TABLE temp_prohibited_recalc; ", false);
            CreateTempTable(conn_db, out ret);
        }

        public void CreateTempTable(IDbConnection conn_db, out Returns ret)
        {
            ExecSQL(conn_db,
               " CREATE TEMP TABLE temp_prohibited_recalc (" +
               "   nzp_kvar  integer," +
               "   nzp_dom   integer," +
               "   nzp_serv  integer," +
               "   nzp_supp  integer," +
               "   nzp_serv_odn  integer," +
               "   rashod  " + sDecimalType + "(14,7) default 0," +
               "   rashod_odn  " + sDecimalType + "(14,7) default 0," +
               "   rashod_all  " + sDecimalType + "(14,7) default 0," +
               "   calc_month  date" +
               " ) " + sUnlogTempTable
               , true);

            ret = ExecSQL(conn_db,
                " Create index itemp_prohibited_recalc_main  on temp_prohibited_recalc (nzp_kvar,nzp_serv,nzp_supp); ",
                true);
            if (!ret.result)
            {
                conn_db.Close();
                return;
            }
            ret = ExecSQL(conn_db,
                " Create index itemp_prohibited_recalc_odn  on temp_prohibited_recalc (nzp_kvar,nzp_serv_odn,nzp_supp); ",
                true);
            if (!ret.result)
            {
                conn_db.Close();
                return;
            }
            ret = ExecSQL(conn_db,
               " Create index itemp_prohibited_recalc_odn_calc_month  on temp_prohibited_recalc (nzp_kvar,nzp_serv,nzp_supp,calc_month); ",
               true);
            if (!ret.result)
            {
                conn_db.Close();
                return;
            }
            ret = ExecSQL(conn_db,
              " Create index itemp_prohibited_recalc_odn_calc_month_not_nzp_supp  on temp_prohibited_recalc (nzp_kvar,nzp_serv,,calc_month); ",
              true);
            if (!ret.result)
            {
                conn_db.Close();
                return;
            }
        }

        public void CreateTempProhibitedRecalcForCharge(IDbConnection conn_db, CalcTypes.ParamCalc paramcalc, out Returns ret)
        {
            ret = Utils.InitReturns();
            var is_exists = DBManager.ExecScalar<bool>(conn_db, "SELECT COUNT(1)<>-1 FROM temp_prohibited_recalc LIMIT 1 ");
            if (is_exists) return;

            CreateTempProhibitedBegin(conn_db, out ret);
            if (!ret.result) return;
            CreateTempTable(conn_db, out ret);
            InsertIntoTempTableProhibitedRecalc(conn_db, new CalcTypes.ChargeXX(paramcalc), out ret);
        }


        public void InsertIntoTempTableProhibitedRecalc(IDbConnection conn_db, CalcTypes.ChargeXX chargeXX, out Returns ret)
        {
            Utils.InitReturns();
            var paramcalc = chargeXX.paramcalc;
            var calc_date = new DateTime(paramcalc.calc_yy, paramcalc.calc_mm, 1);
            var charge_table = chargeXX.charge_xx_ishod;

            ExecSQL(conn_db, " DROP TABLE t_recalc_select; ", false);
            var sql =
                " create temp table t_recalc_select as select ch.nzp_kvar,ch.nzp_serv,ch.nzp_supp,coalesce(max(ch.dat_charge),'01.01.1900') " +
                " dat_charge from " + charge_table + " ch, temp_prohibited_recalc_begin t where ch.nzp_kvar=t.nzp_kvar " +
                " group by 1,2,3 ;";

            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result) { return; }

            ret = ExecSQL(conn_db,
            " Create index it_recalc_select_1 on t_recalc_select (nzp_kvar,nzp_serv,nzp_supp,dat_charge); ", true);
            if (!ret.result) { conn_db.Close(); return; }

            ret = ExecSQL(conn_db,
            " Create index it_recalc_select_2  on t_recalc_select (nzp_kvar,nzp_serv,nzp_supp); ", true);
            if (!ret.result) { conn_db.Close(); return; }


            sql = "INSERT INTO temp_prohibited_recalc" +
                  "(nzp_kvar,nzp_dom,nzp_serv,nzp_supp,nzp_serv_odn,rashod,rashod_all,calc_month) " +
                  " SELECT rc.nzp_kvar,rc.nzp_dom,rc.nzp_serv,rc.nzp_supp,sodn.nzp_serv,(CASE WHEN tarif<>0 THEN ch.rsum_tarif/ch.tarif ELSE 0 END),(CASE WHEN tarif<>0 THEN ch.rsum_tarif/ch.tarif ELSE 0 END),'" +
                   calc_date.ToShortDateString() + "' " +
                  " FROM temp_prohibited_recalc_begin rc LEFT OUTER JOIN " + Points.Pref +
                  sKernelAliasRest + "serv_odn sodn ON rc.nzp_serv = sodn.nzp_serv_link,"
                  + charge_table + " ch,t_recalc_select trs " +
                  " WHERE " +
                  " ch.nzp_serv = rc.nzp_serv AND ch.nzp_kvar = rc.nzp_kvar AND ch.nzp_supp = rc.nzp_supp AND trs.nzp_serv = ch.nzp_serv " +
                  " AND trs.nzp_supp = ch.nzp_supp AND coalesce(ch.dat_charge,'01.01.1900') = trs.dat_charge AND " +
                  " trs.nzp_kvar = ch.nzp_kvar and '" + calc_date.ToShortDateString() + "' between rc.dat_s and rc.dat_po";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result) { return; }

            ExecSQL(conn_db, " DROP TABLE t_charge_proh_recalc; ", false);
            sql = "SELECT ch.nzp_kvar,ch.rsum_tarif,ch.tarif,ch.nzp_serv,ch.nzp_supp,max(ch.dat_charge) into temp t_charge_proh_recalc FROM " + charge_table + " ch, t_recalc_select trs WHERE " +
            " ch.nzp_supp = trs.nzp_supp AND trs.nzp_serv = ch.nzp_serv AND trs.nzp_kvar = ch.nzp_kvar AND coalesce(ch.dat_charge,'01.01.1900')" +
            " = trs.dat_charge GROUP BY 1,2,3,4,5 ";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result) { return; }

            ret = ExecSQL(conn_db,
            " Create index it_charge_proh_recalc on t_charge_proh_recalc (nzp_kvar,nzp_serv,nzp_supp); ", true);
            if (!ret.result) { conn_db.Close(); return; }

            sql = " UPDATE temp_prohibited_recalc tmp SET rashod_odn = " + sNvlWord + "((CASE WHEN tarif<>0 THEN ch.rsum_tarif/ch.tarif ELSE 0 END),0),rashod_all = "
                + sNvlWord + "((CASE WHEN tarif<>0 THEN ch.rsum_tarif/ch.tarif ELSE 0 END),0)+" + sNvlWord + "(tmp.rashod,0) " +
            " FROM " +
            " t_charge_proh_recalc ch " +
            " WHERE ch.nzp_kvar = tmp.nzp_kvar AND ch.nzp_supp = tmp.nzp_supp AND ch.nzp_serv = tmp.nzp_serv_odn;";
            ret = ExecSQL(conn_db, sql, true);
        }

        public void UpdateCounters(IDbConnection conn_db, Rashod rashod, out Returns ret)
        {
            var paramcalc = rashod.paramcalc;
            var calc_date = new DateTime(paramcalc.calc_yy, paramcalc.calc_mm, 1);
            var counters_table = paramcalc.pref + "_charge_" + (paramcalc.calc_yy - 2000).ToString("00") + tableDelimiter + "counters_" + paramcalc.calc_mm.ToString("00");
            var sql = "UPDATE " + counters_table + " c SET " +
                      " kod_info = 1, " +
                      " rashod = rc.rashod_all " +
                      " FROM " +
                      " temp_prohibited_recalc rc " +
                      " WHERE c.nzp_kvar =  rc.nzp_kvar AND c.stek = 3 AND c.nzp_serv = rc.nzp_serv and rc.calc_month = '" + calc_date.ToShortDateString() + "'";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result) { return; }

            ExecSQL(conn_db, " DROP TABLE t_recalc_counters_select; ", false);
            sql =
                 " create temp table t_recalc_counters_select as " +
                 " select c.nzp_kvar,c.nzp_serv,coalesce(min(c.dat_s),'01.01.1900')" +
                 " dat_s from " + counters_table + " c, t_selkvar t  where c.nzp_kvar=t.nzp_kvar  group by 1,2 ";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result) { return; }

            ExecSQL(conn_db, " DROP TABLE temp_counters_record; ", false);

            sql = "CREATE TEMP TABLE temp_counters_record AS " +
                  " SELECT c.nzp_cntx, rc.rashod_all rashod, c.dat_s,rc.nzp_serv,rc.nzp_kvar " +
                  " FROM  " + counters_table + " c,temp_prohibited_recalc rc " +
                  " WHERE c.nzp_kvar = rc.nzp_kvar AND c.stek = 20 AND c.nzp_serv = rc.nzp_serv and rc.calc_month = '" + calc_date.ToShortDateString() + "'";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result) { return; }

            sql = "UPDATE temp_counters_record tcr SET " +
                  " rashod = 0 " +
                  " WHERE not exists (select 1 from t_recalc_counters_select tcs where tcs.nzp_serv = tcr.nzp_serv and tcs.nzp_kvar = tcr.nzp_kvar and tcs.dat_s = " +
                  " coalesce(tcr.dat_s,'01.01.1900'))";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result) { return; }

            sql = "UPDATE " + counters_table + " c SET " +
                  " rashod = rc.rashod," +
                  " kod_info = 1" +
                  " FROM " +
                  " (SELECT * FROM temp_counters_record) rc " +
                  " WHERE c.nzp_cntx =  rc.nzp_cntx;";
            ret = ExecSQL(conn_db, sql, true);
        }

        #endregion Функции обработки запрета расчета по связке ЛС/Услуга/Поставщик(договор)

        protected Returns SetStatusCalcTask(IDbConnection conn_db, CalcTypes.ParamCalc paramcalc, decimal progres, CalcTypes.CalcSteps step)
        {
            var text = "";
            var date = new DateTime(paramcalc.calc_yy, paramcalc.calc_mm, 1).ToString("MMMM") + " " + paramcalc.calc_yy;
            switch (step)
            {
                case CalcTypes.CalcSteps.CalcGil:
                    {
                        text = ":Анализ информации о жильцах за " + date;
                        break;
                    }
                case CalcTypes.CalcSteps.CalcRashod:
                    {
                        text = ":Получение расходов за " + date;
                        break;
                    }
                case CalcTypes.CalcSteps.CalcNedo:
                    {
                        text = ":Расчет недопоставок за " + date;
                        break;
                    }
                case CalcTypes.CalcSteps.CalcGku:
                    {
                        text = ":Применение формул для расчета за " + date;
                        break;
                    }
                case CalcTypes.CalcSteps.CalcPeni:
                    {
                        text = ":Расчет пени за " + date;
                        break;
                    }
                case CalcTypes.CalcSteps.CalcCharge:
                    {
                        text = ":Сохранение начислений за " + date;
                        break;
                    }
                case CalcTypes.CalcSteps.PrepareParams:
                    {
                        text = ":Определение объема выборки для расчета за " + date;
                        break;
                    }
                case CalcTypes.CalcSteps.PrepareMonthParams:
                    {
                        text = ":Получение квартирных параметров за " + date;
                        break;
                    }
                case CalcTypes.CalcSteps.Complete:
                    {
                        text = "";
                        break;
                    }
            }

            return SetStatusCalcTask(conn_db, paramcalc, progres, text);
        }



        protected Returns SetStatusCalcTask(IDbConnection conn_db, CalcTypes.ParamCalc paramCalc, decimal progres, string text = "")
        {
            if (paramCalc.calcfon.TaskType != CalcFonTask.Types.taskWithReval
                && paramCalc.calcfon.TaskType != CalcFonTask.Types.taskWithRevalOntoListHouses)
            {
                return new Returns();
            }
            var tab = sDefaultSchema + "calc_fon_" + paramCalc.calcfon.QueueNumber;

            text = DbCalcQueueClient.CalcFonComment(conn_db, 0, paramCalc.calcfon.nzpt, paramCalc.nzp_dom, false) + "\r\n" + text;

            var ret = DBManager.ExecSQL(conn_db,
                string.Format(" Update {0} Set progress=CASE WHEN 1<" + sNvlWord + "(progress,0)+{1} " +
                              "THEN 1 ELSE " + sNvlWord + "(progress,0)+{1} END," +
                              " txt={3} Where nzp_key ={2}",
                tab, progres.ToString(CultureInfo.InvariantCulture), paramCalc.calcfon.nzp_key, Utils.EStrNull(text))
                , true);
            return ret;
        }

        protected struct CalcMonth
        {
            public int year;
            public int month;

            public CalcMonth(int _year, int _month)
            {
                year = _year;
                month = _month;
            }
        }

    }
}
