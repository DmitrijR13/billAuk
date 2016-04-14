using System;
using System.Collections.Generic;
using System.Text;
using Bars.KP50.Utils;
using STCLINE.KP50.Interfaces;
using System.Data;
using STCLINE.KP50.Global;
using STCLINE.KP50.IFMX.Kernel.source.CommonType;

namespace STCLINE.KP50.DataBase
{
    public enum ServPeni
    {
        nzp_serv = 500
    }

    public enum PerekidkaType
    {
        nach_k_oplate = 1
    }
    
    public class DbKreditPay : DataBaseHeadServer
    {
        private IDbConnection _connDb = null;
        private DateTime calcMonth;
        private DateTime prevCalcMonth;
        private string _pref = "";

        public ReturnsType UpdateKredit(string pref, int inCalcYear, int inCalcMonth)
        {
            IDbConnection conn_db = null;

            try
            {
                conn_db = GetConnection(Points.GetConnByPref(pref));
                Returns ret = OpenDb(conn_db, true);
                if (!ret.result) throw new Exception(ret.text);
                return this.UpdateKredit(conn_db, pref, inCalcYear, inCalcMonth);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка в функции DbKreditPay.UpdateKredit" + (Constants.Viewerror ? ":\n" + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                return new ReturnsType(false, "Ошибка при пересчете графика рассрочки", -1, "");    
            }
            finally
            {
                if (conn_db != null)
                {
                    conn_db.Close();
                    conn_db.Dispose();
                }
            }
        }
  
        /// <summary>
        /// Обновить рассрочки и их графики
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="pref"></param>
        /// <param name="calcYear"></param>
        /// <param name="calcMonth"></param>
        /// <returns></returns>
        public ReturnsType UpdateKredit(IDbConnection conn_db, string pref, int inCalcYear, int inCalcMonth)
        {
            ReturnsType res = new ReturnsType();

            _connDb = conn_db;
            _pref = pref;

            try
            {
                calcMonth = new DateTime(inCalcYear, inCalcMonth, 1);
                prevCalcMonth = new DateTime(inCalcYear, inCalcMonth, 1).AddMonths(-1); 

                // обновить в графиках рассрочки колонку "Оплачено"
                UpdateKreditPaySumMoney();

                // установить признак, что рассрочка нарушена
                BrokenKredit();

                // сдвинуть графики рассрочки
                ChangeCreditPay();

                // закрыть рассрочки
                CloseKredit();

                return res;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка в функции DbKreditPay.UpdateKredit" + (Constants.Viewerror ? ":\n" + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                return new ReturnsType(false, "Ошибка при пересчете графика рассрочки", -1, ex.Message);
            }
        }

        /// <summary>
        /// Обновить сумму оплачено в рассрочке
        /// </summary>
        private void UpdateKreditPaySumMoney()
        {
            ExecSQL(_connDb, "drop table tmp_sum_money_kredit", false);

            string sql = " create temp table tmp_sum_money_kredit (" +
                " nzp_kredx integer, " +
                " nzp_kvar integer, " +
                " nzp_serv integer, " +
                " nzp_serv_main integer, " +
                " sum_money_perc " + DBManager.sDecimalType + " (14,2), " +
                " sum_money_main  " + DBManager.sDecimalType + " (14,2), " +
                " sum_charge " + DBManager.sDecimalType + " (14,2), " +
                " sum_charge_main " + DBManager.sDecimalType + " (14,2), " +
                " sum_money_kredit " + DBManager.sDecimalType + " (14,2) " +
                ") ";
            ExecSQLWE(_connDb, sql);

            var tempOpl = "temp_opl_" + DateTime.Now.Ticks;

            sql = " create temp table " + tempOpl + " (" +
                " nzp_serv integer, " +
                " num_ls integer, " +
                " nzp_kvar integer, " +
                " sum_prih double precision" +
                ") ";
            ExecSQLWE(_connDb, sql);

            var prevprevmonth = prevCalcMonth.AddMonths(-1);
            var listMonth = new List<DateTime> {prevCalcMonth, prevprevmonth};

            var tempKred = "temp_kred_" + DateTime.Now.Ticks;

            sql = " select distinct kx.nzp_kvar, kv.num_ls, kx.nzp_serv " + 
                  " into temp " + tempKred +
                  " from " + Points.Pref + DBManager.sDebtAliasRest + "kredit k, " + Points.Pref + DBManager.sDebtAliasRest + "kredit_pay kx, " +
                  Points.Pref + "_data" + tableDelimiter + "kvar kv " +
                  " where k.nzp_kredit = kx.nzp_kredit and kx.calc_month = " + Utils.EStrNull(prevprevmonth.ToShortDateString()) + " and k.valid = 1" +
                  " and k.nzp_kvar = kv.nzp_kvar";
            ExecSQLWE(_connDb, sql);
            
            sql = " insert into " + tempKred +
                  " select t.nzp_kvar, t.num_ls, so.nzp_serv_link from " + tempKred + " t, " +
                  Points.Pref + DBManager.sKernelAliasRest + "serv_odn so " +
                  " where t.nzp_serv = so.nzp_serv_repay";
            ExecSQLWE(_connDb, sql);

            ExecSQLWE(_connDb, " create index ix_" + tempKred + "_1 on " + tempKred + " (nzp_kvar, nzp_serv) ");
            ExecSQLWE(_connDb, DBManager.sUpdStat + " " + tempKred);

            var usl = " and case when calc_month is null then dat_uchet between " +
                      "'" + prevCalcMonth.ToShortDateString() + "' and '" +
                      new DateTime(prevCalcMonth.Year, prevCalcMonth.Month,
                          DateTime.DaysInMonth(prevCalcMonth.Year, prevCalcMonth.Month)).ToShortDateString()
                      + "' else calc_month = '" + prevCalcMonth.ToShortDateString() + "' end ";

            foreach (var dat in listMonth)
            {
                sql = "insert into " + tempOpl + " (nzp_serv, num_ls, nzp_kvar, sum_prih) " +
                      " select f.nzp_serv, f.num_ls, k.nzp_kvar, sum(f.sum_prih) " +
                      " from " + _pref + "_charge_" + (dat.Year%100).ToString("00") + DBManager.tableDelimiter +
                      "fn_supplier" + dat.Month.ToString("00") + " f, " + tempKred + " k " +
                      " where k.num_ls = f.num_ls and k.nzp_serv = f.nzp_serv " + usl +
                      " group by 1,2,3";
                ExecSQLWE(_connDb, sql);
            }

            if (prevprevmonth.Year != prevCalcMonth.Year)
            {
                foreach (var dat in listMonth)
                {
                    sql = "insert into " + tempOpl + " (nzp_serv, num_ls, nzp_kvar, sum_prih) " +
                          " select f.nzp_serv, f.num_ls, k.nzp_kvar, sum(f.sum_prih) " +
                          " from " + _pref + "_charge_" + (dat.Year % 100).ToString("00") + DBManager.tableDelimiter +"from_supplier f, " +
                           tempKred + " k " +
                          " where k.num_ls = f.num_ls and k.nzp_serv = f.nzp_serv " + usl +
                          " group by 1, 2, 3";
                    ExecSQLWE(_connDb, sql);
                }
            }
            else sql = "insert into " + tempOpl + " (nzp_serv, num_ls, nzp_kvar, sum_prih) " +
                          " select f.nzp_serv, f.num_ls, k.nzp_kvar, sum(f.sum_prih) " +
                          " from " + _pref + "_charge_" + (prevCalcMonth.Year % 100).ToString("00") + DBManager.tableDelimiter + "from_supplier f, " +
                           tempKred + " k " +
                          " where k.num_ls = f.num_ls and k.nzp_serv = f.nzp_serv  " + usl +
                          " group by 1, 2, 3";
            ExecSQLWE(_connDb, sql);

            ExecSQLWE(_connDb, " create index ix_" + tempOpl + "_1 on " + tempOpl + " (nzp_kvar, nzp_serv) ");
            ExecSQLWE(_connDb, DBManager.sUpdStat + " " + tempOpl);

            // ситуация: месяц начала действия рассрочки = текущий расчетный месяц 
            // график сформирован с марта
            // prevCalcMonth = февраль
            // в tmp_sum_money_kredit не попадет ни одна строка

            // ситуация: месяц начала действия рассрочки < текущий расчетный месяц 
            // график сформирован с марта
            // текущий расчетный месяц - апрель
            // prevCalcMonth = март
            // в tmp_sum_money_kredit попадут строки за март
            
            sql = "insert into tmp_sum_money_kredit (nzp_kredx, nzp_kvar, nzp_serv) " +
                " select kx.nzp_kredx, kx.nzp_kvar, kx.nzp_serv " +
                " from " + Points.Pref + DBManager.sDebtAliasRest + "kredit k, " + Points.Pref + DBManager.sDebtAliasRest + "kredit_pay kx " +
                " where k.nzp_kredit = kx.nzp_kredit " +
                "   and kx.calc_month = " + Utils.EStrNull(prevprevmonth.ToShortDateString()) +
                // рассрочка действует
                "   and k.valid = 1 " +
                " group by 1";
            ExecSQLWE(_connDb, sql);

            // создать индексы
            ExecSQLWE(_connDb, " create index ix_tmp_sum_money_kredit_nzp_kredx on tmp_sum_money_kredit (nzp_kredx) ");
            ExecSQLWE(_connDb, " create index ix_tmp_sum_money_kredit_nzp_serv on tmp_sum_money_kredit (nzp_serv) ");
            ExecSQLWE(_connDb, DBManager.sUpdStat + " tmp_sum_money_kredit");
            
            // определить основные услуги
            sql = "update tmp_sum_money_kredit t set " +
                " nzp_serv_main = (select max(so.nzp_serv_link) from " + _pref + DBManager.sKernelAliasRest + "serv_odn so where t.nzp_serv = so.nzp_serv_repay)";
            ExecSQLWE(_connDb, sql);
            ExecSQLWE(_connDb, " create index ix_tmp_sum_money_kredit_nzp_serv_main on tmp_sum_money_kredit (nzp_serv_main) ");
            ExecSQLWE(_connDb, DBManager.sUpdStat + " tmp_sum_money_kredit");

            // определить суммы оплачено по услугам % за рассрочку
            sql = "update tmp_sum_money_kredit t set " +
                " sum_money_perc = (select sum(c.sum_prih) from " + tempOpl + " c where t.nzp_serv = c.nzp_serv and t.nzp_kvar = c.nzp_kvar)";
            ExecSQLWE(_connDb, sql);

            // определить суммы оплачено по основным услугам
            sql = "update tmp_sum_money_kredit t set " +
                " sum_money_main = (select sum(c.sum_prih) from " + tempOpl + " c where t.nzp_serv_main = c.nzp_serv and t.nzp_kvar = c.nzp_kvar)";
            ExecSQLWE(_connDb, sql);

            var chargeXXT = _pref + "_charge_" + (prevprevmonth.Year % 100).ToString("00") + DBManager.tableDelimiter + "charge_" + prevprevmonth.Month.ToString("00") + "_t";

            // определить сумм начислено по основным услугам из прошлого месяца
            sql = "update tmp_sum_money_kredit a set sum_charge = (select sum(b.sum_charge) from " + chargeXXT + " b where a.nzp_kvar = b.nzp_kvar and (a.nzp_serv_main = b.nzp_serv or a.nzp_serv = b.nzp_serv))," +
                  " sum_charge_main = (select sum(b.sum_charge) from " + chargeXXT + " b where a.nzp_kvar = b.nzp_kvar and a.nzp_serv_main = b.nzp_serv) ";
            ExecSQLWE(_connDb, sql);

            // рассчитать сумму оплачено для графика рассрочки
            sql = "update tmp_sum_money_kredit set " +
                " sum_money_kredit = " + DBManager.sNvlWord + "(sum_money_perc,0) + " + DBManager.sNvlWord + "(sum_money_main,0) - " + DBManager.sNvlWord + "(sum_charge,0)";
            ExecSQLWE(_connDb, sql);
            
            // проставить суммы оплачено в графике
            sql = " update " + Points.Pref + DBManager.sDebtAliasRest + "kredit_pay b set " +
                    " sum_money = sum_odna12 + sum_perc + (select sum(a.sum_money_kredit) from tmp_sum_money_kredit a where a.nzp_kredx = b.nzp_kredx)," +
                    " sum_money_main = " + DBManager.sNvlWord + "(sum_odna12,0) + (select sum(" + DBManager.sNvlWord + "(a.sum_money_main,0)) - sum(" + DBManager.sNvlWord + "(sum_charge_main,0)) from tmp_sum_money_kredit a where a.nzp_kredx = b.nzp_kredx)" +
                  " where b.nzp_kredx in (select nzp_kredx from tmp_sum_money_kredit) and b.calc_month = " + Utils.EStrNull(prevprevmonth.ToShortDateString());
            ExecSQLWE(_connDb, sql, true);

            ExecSQL(_connDb, "drop table tmp_sum_money_kredit", false);
            ExecSQL(_connDb, "drop table " + tempOpl, false);
            ExecSQL(_connDb, "drop table " + tempKred, false);
        }

        

        /// <summary>
        /// закрыть рассрочки
        /// </summary>
        private void CloseKredit()
        {
            ExecSQL(_connDb, "drop table tmp_close_kredit", false);

            string sql = "create temp table tmp_close_kredit (" +
                " nzp_kredit integer, " +
                " nzp_kvar integer, " +
                " nzp_serv integer " + ")";
            ExecSQLWE(_connDb, sql);

            sql = "insert into tmp_close_kredit (nzp_kredit, nzp_kvar, nzp_serv) " +
                " select b.nzp_kredit, b.nzp_kvar, b.nzp_serv " +
                " from " + Points.Pref + DBManager.sDebtAliasRest + "kredit_pay a, " + Points.Pref + DBManager.sDebtAliasRest + "kredit b " +
                " Where a.nzp_kredit = b.nzp_kredit " +
                    " and a.sum_outdolg <= 0 " + // исходящий долг погашен
                    " and a.sum_charge <= a.sum_money " + // есть оплата за месяц
                    " and a.calc_month = " + Utils.EStrNull(prevCalcMonth.AddMonths(-1).ToShortDateString()) + // месяц
                    " and b.valid = 1"; // рассрочка предоставляется
            ExecSQLWE(_connDb, sql);

            sql = " create index ix_tmp_close_kredit_1 on tmp_close_kredit (nzp_kredit) ";
            ExecSQLWE(_connDb, sql);

            sql = " create index ix_tmp_close_kredit_2 on tmp_close_kredit (nzp_kvar, nzp_serv) ";
            ExecSQLWE(_connDb, sql);

            ExecSQLWE(_connDb, DBManager.sUpdStat + " tmp_close_kredit");

            // закрыть рассрочки
            sql = " update " + Points.Pref + DBManager.sDebtAliasRest + "kredit b set " + 
                " valid = 4 " +
                " where exists (select 1 from tmp_close_kredit a where b.nzp_kredit = a.nzp_kredit " + DBManager.Limit1 + ")";
            ExecSQLWE(_connDb, sql);

            // закрыть проценты по услугам
            CloseKreditServices("tmp_close_kredit");

            ExecSQL(_connDb, "drop table tmp_close_kredit", false);
        }

        /// <summary>
        /// закрыть проценты по услугам
        /// </summary>
        /// <param name="tempTable"></param>
        private void CloseKreditServices(string tempTable)
        {
            string sql = "update " + _pref + DBManager.sDataAliasRest + "tarif t set " +
                    " dat_po = " + Utils.EStrNull(calcMonth.AddDays(-1).ToShortDateString()) + "," +
                    " nzp_user = " + Constants.AutoDefeferredPayUserID + "," +
                    " dat_when = " + DBManager.sCurDate +
                " where exists (select 1 " +
                    " FROM " + tempTable + " a " +
                    " WHERE   a.nzp_kvar = t.nzp_kvar " +
                        " AND a.nzp_serv = t.nzp_serv " + DBManager.Limit1 + ") ";
            ExecSQLWE(_connDb, sql);
        }

        /// <summary>
        /// установить признак рассрочка нарушена для тех, у кого оплачено меньше, чем начислено
        /// </summary>
        private void BrokenKredit()
        {
            ExecSQL(_connDb, "drop table tmp_broken_kredit", false);

            string sql = "create temp table tmp_broken_kredit (" +
                " nzp_kredit integer, " +
                " nzp_serv integer, " +
                " nzp_kvar integer)";
            ExecSQLWE(_connDb, sql);

            sql = "insert into tmp_broken_kredit (nzp_kredit, nzp_kvar, nzp_serv) " +
                " select b.nzp_kredit, b.nzp_kvar, b.nzp_serv " +
                " from " + Points.Pref + DBManager.sDebtAliasRest + "kredit_pay a, " + Points.Pref + DBManager.sDebtAliasRest + "kredit b " +
                " Where a.nzp_kredit = b.nzp_kredit " +
                    " and a.sum_charge > a.sum_money " + // оплачено меньше, чем начислено
                    " and a.calc_month = " + Utils.EStrNull(prevCalcMonth.AddMonths(-1).ToShortDateString()) + // месяц
                    " and b.valid = 1"; // рассрочка предоставляется
            ExecSQLWE(_connDb, sql);

            sql = " create index ix_tmp_broken_kredit_1 on tmp_broken_kredit (nzp_kredit) ";
            ExecSQLWE(_connDb, sql);

            sql = " create index ix_tmp_broken_kredit_2 on tmp_broken_kredit (nzp_kvar, nzp_serv) ";
            ExecSQLWE(_connDb, sql);

            ExecSQLWE(_connDb, DBManager.sUpdStat + " tmp_broken_kredit");
            
            sql = "update " + Points.Pref + DBManager.sDebtAliasRest + "kredit b set " + 
                " valid = 3 " +
               " where exists (select 1 from tmp_broken_kredit a where b.nzp_kredit = a.nzp_kredit " + DBManager.Limit1 + ")";
            ExecSQLWE(_connDb, sql);

            // подвинуть дату начала действия услуги Пени, которая была открыта при заключении договора на рассрочку
            sql = " update " + _pref + DBManager.sDataAliasRest + "tarif t set dat_s = " + Utils.EStrNull(calcMonth.ToShortDateString()) +
                " where user_del = " + Constants.AutoDefeferredPayUserID +
                "   and dat_s >= " + Utils.EStrNull(calcMonth.ToShortDateString()) +
                 "  and exists (select 1 FROM tmp_broken_kredit a WHERE a.nzp_kvar = t.nzp_kvar " + DBManager.Limit1 + ") " +
                 "  and nzp_serv = " + (int)ServPeni.nzp_serv;
            ExecSQLWE(_connDb, sql);
            
            // закрыть проценты по услугам
            CloseKreditServices("tmp_broken_kredit");
            
            ExecSQL(_connDb, "drop table tmp_broken_kredit", false);
        }

        /// <summary>
        /// Изменить графики рассрочки
        /// </summary>
        private void ChangeCreditPay()
        {
            string sql = "";
            var prevprevmonth = prevCalcMonth.AddMonths(-1);
            // обновить сальдо исходящее на месяц расчета
            sql = " update " + Points.Pref + DBManager.sDebtAliasRest + "kredit_pay a set " +
                " sum_outdolg = sum_indolg - (case when sum_money > sum_charge then sum_money else sum_charge end), " +
                " sum_outdolg_main = sum_indolg_main - sum_money_main " +
                " where a.calc_month = " + Utils.EStrNull(prevprevmonth.ToShortDateString()) + 
                  " and a.nzp_kredit in (select nzp_kredit from " + Points.Pref + DBManager.sDebtAliasRest + "kredit where valid = 1)";
            ExecSQLWE(_connDb, sql);

            sql = " update " + Points.Pref + DBManager.sDebtAliasRest + "kredit_pay a set " +
                " sum_outdolg = 0 " +
                " where a.calc_month = " + Utils.EStrNull(prevprevmonth.ToShortDateString()) +
                "   and a.sum_outdolg < 0";
            ExecSQLWE(_connDb, sql);

            sql = " update " + Points.Pref + DBManager.sDebtAliasRest + "kredit_pay a set " +
                " sum_outdolg_main = 0 " +
                " where a.calc_month = " + Utils.EStrNull(prevprevmonth.ToShortDateString()) +
                "   and a.sum_outdolg_main < 0";
            ExecSQLWE(_connDb, sql);

            DateTime dat_s = prevprevmonth.AddMonths(1);
            
            // изменить графики рассрочки для всех оставшихся
            for (int i = 0; i < 11; i++)
            {
                // получить исходящее сальдо из прошлого месяца
                ExecSQL(_connDb, "drop table tmp_sum_outsaldo_kredit");

                sql = "create temp table tmp_sum_outsaldo_kredit (" + 
                    " sum_outdolg " + DBManager.sDecimalType + "(14,2), " +
                    " sum_outdolg_main " + DBManager.sDecimalType + "(14,2), " +
                    " nzp_kredit integer, " +
                    " nzp_kvar integer, " +
                    " nzp_serv integer) ";
                ExecSQL(_connDb, sql);

                sql = "insert into tmp_sum_outsaldo_kredit (sum_outdolg, sum_outdolg_main, nzp_kredit, nzp_kvar, nzp_serv) " +
                    " select sum(sum_outdolg), sum(sum_outdolg_main), nzp_kredit, nzp_kvar, nzp_serv " + 
                    " from " + Points.Pref + DBManager.sDebtAliasRest + "kredit_pay a " +
                    " where a.calc_month = " + Utils.EStrNull(dat_s.AddMonths(-1).ToShortDateString()) +
                        " and a.nzp_kredit in (select nzp_kredit from " + Points.Pref + DBManager.sDebtAliasRest + "kredit where valid = 1)" +
                    " group by 3,4,5";
                ExecSQL(_connDb, sql);

                // обновить входящее сальдо
                sql = " update " + Points.Pref + DBManager.sDebtAliasRest + "kredit_pay a set " +
                " sum_indolg = (select t.sum_outdolg from tmp_sum_outsaldo_kredit t " +
                    " Where t.nzp_kredit = a.nzp_kredit "  + 
                    "   and t.nzp_kvar = a.nzp_kvar " + 
                    "   and t.nzp_serv = a.nzp_serv), " +
                    " sum_indolg_main = (select t.sum_outdolg_main from tmp_sum_outsaldo_kredit t " +
                    " Where t.nzp_kredit = a.nzp_kredit " +
                    "   and t.nzp_kvar = a.nzp_kvar " +
                    "   and t.nzp_serv = a.nzp_serv) " +
                    " where exists (select 1 from tmp_sum_outsaldo_kredit b " +
                    " where a.nzp_kredit = b.nzp_kredit " +
                        " and a.nzp_kvar = b.nzp_kvar " +
                        " and a.nzp_serv = b.nzp_serv) " +
                    " and a.calc_month = " + Utils.EStrNull(dat_s.ToShortDateString());
                ExecSQLWE(_connDb, sql);

                // обновить сумму долга
                sql = " update " + Points.Pref + DBManager.sDebtAliasRest + "kredit_pay a set " +
                    " sum_dolg = sum_indolg " +
                    " where exists (select 1 from tmp_sum_outsaldo_kredit b " +
                    " where a.nzp_kredit = b.nzp_kredit " +
                        " and a.nzp_kvar = b.nzp_kvar " +
                        " and a.nzp_serv = b.nzp_serv) " +
                    " and a.calc_month = " + Utils.EStrNull(dat_s.ToShortDateString());
                ExecSQLWE(_connDb, sql);
                                
                // обновить сальдо исходящее на месяц расчета
                sql = " update " + Points.Pref + DBManager.sDebtAliasRest + "kredit_pay a set " +
                    " sum_outdolg = sum_indolg - (case when sum_money > sum_charge then sum_money else sum_charge end), " +
                    " sum_outdolg_main = sum_indolg_main - (case when sum_money_main > sum_odna12 then sum_money_main else sum_odna12 end)" +
                    " where a.calc_month = " + Utils.EStrNull(dat_s.ToShortDateString()) +
                        " and a.nzp_kredit in (select nzp_kredit from " + Points.Pref + DBManager.sDebtAliasRest + "kredit where valid = 1)";
                ExecSQLWE(_connDb, sql);

                sql = " update " + Points.Pref + DBManager.sDebtAliasRest + "kredit_pay a set " +
                    " sum_outdolg = 0 " +
                    " where a.calc_month = " + Utils.EStrNull(dat_s.ToShortDateString()) +
                    "   and a.sum_outdolg < 0";
                ExecSQLWE(_connDb, sql);

                sql = " update " + Points.Pref + DBManager.sDebtAliasRest + "kredit_pay a set " +
                   " sum_outdolg_main = 0 " +
                   " where a.calc_month = " + Utils.EStrNull(dat_s.ToShortDateString()) +
                   "   and a.sum_outdolg_main < 0";
                ExecSQLWE(_connDb, sql);

                dat_s = dat_s.AddMonths(1);
            }

            ExecSQL(_connDb, "drop table tmp_sum_outsaldo_kredit");
        }
    }
       
    public class DbKredit: DataBaseHeadServer
    {
        private string perekidka;
        private string chargePeni;
        
        /// <summary>
        /// Сохранить рассрочку
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        public void SaveCredit(Credit finder, out Returns ret)
        {
            IDbConnection conn_db = null;
            IDbTransaction transaction = null;
            IDataReader reader = null;

            string sql = "";

            try
            {
                conn_db = GetConnection(Points.GetConnByPref(Points.Pref));
                ret = OpenDb(conn_db, true);
                if (!ret.result) throw new Exception(ret.text);

                RecordMonth curCalcMonth = Points.GetPoint(finder.pref).CalcMonth;
                DateTime currentCalcMonth = new DateTime(curCalcMonth.year_, curCalcMonth.month_, 1);
                DateTime dat_po = new DateTime(curCalcMonth.year_, curCalcMonth.month_, 1).AddMonths(12).AddDays(-1);

                ret = CheckSaveData(conn_db, finder, currentCalcMonth);
                if (!ret.result) return;

                Credit credit = GetCreditById(conn_db, finder.nzp_kredit);
                finder.dat_month = credit.dat_month;
                finder.nzp_kvar = credit.nzp_kvar;
                finder.nzp_serv = credit.nzp_serv;
                finder.sum_dolg = credit.sum_dolg;
                
                transaction = conn_db.BeginTransaction();

                // cохранить данные
                sql = "update " + Points.Pref + DBManager.sDebtAliasRest + "kredit set " + 
                    " (valid, perc, dog_num, dog_dat, dat_s, dat_po) = " +
                    " (" + finder.valid + "," + finder.perc + "," + Utils.EStrNull(finder.dog_num) + "," +
                    Utils.EStrNull(Convert.ToDateTime(finder.dog_dat).ToString("dd.MM.yyyy")) + "," +
                    Utils.EStrNull(Convert.ToDateTime(currentCalcMonth).ToString("dd.MM.yyyy")) + "," +
                    Utils.EStrNull(Convert.ToDateTime(dat_po).ToString("dd.MM.yyyy")) + ")" +
                    " where nzp_kredit = " + finder.nzp_kredit;
                ExecSQLWE(conn_db, transaction, sql);

                if (finder.valid == 1)
                {
                    // создать таблицу под график рассрочки
                    CreateKreditPay(conn_db, transaction);

                    // закрыть услугу пени
                    ClosePeni(finder, currentCalcMonth, conn_db, transaction);

                    // перекинуть сумму пени
                    AddPeniPerekidka(finder, currentCalcMonth, conn_db, transaction);

                    // рассчитать график рассрочки
                    CalcKreditXX(finder, currentCalcMonth, conn_db, transaction);

                    // уменьшить сумму к оплате
                    using (DbCalcCharge db = new DbCalcCharge())
                    {
                        string charge_XX = finder.pref + "_charge_" + (currentCalcMonth.Year % 100) + tableDelimiter + "charge_" + currentCalcMonth.Month.ToString("00");
                        db.SetPayToPrev(conn_db, currentCalcMonth.Month, currentCalcMonth.Year, finder.pref, charge_XX, " nzp_kvar = " + finder.nzp_kvar, false, out ret);
                        if (!ret.result) throw new Exception();    
                    }
                }

                transaction.Commit();
            }
            catch (Exception ex)
            {
                if (transaction != null) transaction.Rollback();
                MonitorLog.WriteLog("Ошибка в функции DbCharge.SaveCredit" + (Constants.Viewerror ? ":\n" + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                ret = new Returns(false, "Ошибка при сохранении рассрочки");
            }
            finally
            {
                if (transaction != null)
                    transaction.Dispose();

                if (conn_db != null)
                {
                    conn_db.Close();
                    conn_db.Dispose();
                }

                if (reader != null)
                {
                    reader.Close();
                    reader.Dispose();
                }   
            }
        }

        /// <summary>
        /// Создать таблицу kredit_pay
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="kreditXX"></param>
        /// <returns></returns>
        private void CreateKreditPay(IDbConnection conn_db, IDbTransaction transaction)
        {
            if (TempTableInWebCashe(conn_db, transaction, Points.Pref + DBManager.sDebtAliasRest + "kredit_pay")) return;

            string sql = "";
#if PG
            sql = " set search_path to " + Points.Pref + DBManager.tableDelimiter + "_debt";
#else
            sql = " Database " + Points.Pref + DBManager.tableDelimiter + "_debt";
#endif
            ExecSQLWE(conn_db, transaction, sql);

            sql = " Create table kredit_pay " +
                " (nzp_kredx      serial not null, " +
                "  nzp_kvar       integer not null, " +
                "  nzp_serv       integer not null, " +
                "  nzp_kredit     integer not null, " +
                "  calc_month     date not null, " +
                "  sum_indolg     " + DBManager.sDecimalType + "(14,2) default 0.00, " +  //остаток по кредиту их прошлого месяца - сколько оставалось погасить
                "  sum_dolg       " + DBManager.sDecimalType + "(14,2) default 0.00, " +  //сумма основного долга в первом месяце, в остальных месяцах = 0
                "  sum_odna12     " + DBManager.sDecimalType + "(14,2) default 0.00, " +  //ежемесячный платеж (1/12)
                "  sum_perc       " + DBManager.sDecimalType + "(14,2) default 0.00, " +  //процент пользования - уходит в charge_xx в отдельную статью
                "  sum_charge     " + DBManager.sDecimalType + "(14,2) default 0.00, " +  //к оплате жителем (= sum_odna12 + sum_perc)
                "  sum_outdolg    " + DBManager.sDecimalType + "(14,2) default 0.00, " +  //остаток по кредиту (= sum_indolg - sum_odna12, в последнем месяце надо выровнить общую сумму с kredit.sum_dolg)
                "  sum_money      " + DBManager.sDecimalType + "(14,2) default 0.00) ";
            ExecSQLWE(conn_db, transaction, sql);

            sql = " Create unique index ix_kredit_pay_nzp_kredx on kredit_pay (nzp_kredx)";
            ExecSQLWE(conn_db, transaction, sql);

            sql = " Create index ix_kredit_pay_1 on kredit_pay (nzp_kvar, nzp_serv)";
            ExecSQLWE(conn_db, transaction, sql);

            sql = " Create index ix_kredit_pay_nzp_kredit on kredit_pay (nzp_kredit)";
            ExecSQLWE(conn_db, transaction, sql);

            sql = " Create index ix_kredit_pay_calc_month on kredit_pay (calc_month)";
            ExecSQLWE(conn_db, transaction, sql);
        }

        /// <summary>
        /// Рассчитать график рассрочки
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="transaction"></param>
        /// <param name="finder"></param>
        /// <returns></returns>
        public void CalcKreditXX(Credit finder, DateTime currentCalcMonth, IDbConnection conn_db, IDbTransaction transaction)
        {
            DateTime dat_s = new DateTime(currentCalcMonth.Year, currentCalcMonth.Month, 1).AddMonths(-1);
            List<decimal> sumOdna12List = new List<decimal>();
            List<decimal> sumPercList = new List<decimal>();
            decimal sum_odna12 = Math.Round((finder.sum_dolg / 12 + 0.005m), 2);

            decimal sum_dolg_perc = Math.Round(finder.sum_dolg * finder.perc / 100, 2);
            decimal sum_perc = Math.Round((sum_dolg_perc / 12 + 0.005m), 2);

            for (int i = 0; i < 12; i++)
            {
                sumOdna12List.Add(sum_odna12);
                sumPercList.Add(sum_perc);
            }

            sumOdna12List[11] = sum_odna12 - (sum_odna12 * 12 - finder.sum_dolg);
            sumPercList[11] = sum_perc - (sum_perc * 12 - sum_dolg_perc);

            decimal sum_dolg = finder.sum_dolg + sum_dolg_perc;
            var sumDolgMain = finder.sum_dolg;

            string sql = "delete from " + Points.Pref + DBManager.sDebtAliasRest + "kredit_pay where nzp_kredit = " + finder.nzp_kredit;
            ExecSQLWE(conn_db, sql);

            if (finder.valid != 1) return;

            sql = "insert into " + Points.Pref + DBManager.sDebtAliasRest + "kredit_pay (nzp_kvar, nzp_serv, nzp_kredit, calc_month, " +
                  "   sum_indolg, sum_dolg, sum_odna12, sum_perc, sum_outdolg, sum_charge, sum_money_main, sum_indolg_main, sum_outdolg_main) values " +
                  " (" + finder.nzp_kvar + "," + finder.nzp_serv + "," + finder.nzp_kredit + "," + Utils.EStrNull(dat_s.ToShortDateString()) + ", " +sum_dolg_perc +
                  ", " + sum_dolg_perc + ", 0,0,0," + (-1) * finder.sum_dolg + ", 0, 0, 0)";
            ExecSQLWE(conn_db, sql);

            for (int i = 0; i < 12; i++)
            {
                decimal sum_charge = sumOdna12List[i] + sumPercList[i];
                decimal sum_outdolg = Math.Max(sum_dolg - sum_charge, 0m);
                var sumoutdolgmain = Math.Max(sumDolgMain - sumOdna12List[i], 0m);

                dat_s = dat_s.AddMonths(1);

                sql = "insert into " + Points.Pref + DBManager.sDebtAliasRest +
                      "kredit_pay (nzp_kvar, nzp_serv, nzp_kredit, calc_month, " +
                      "   sum_indolg, sum_dolg, sum_odna12, sum_perc, sum_outdolg, sum_charge, sum_outdolg_main, sum_indolg_main) values " +
                      " (" + finder.nzp_kvar + "," + finder.nzp_serv + "," + finder.nzp_kredit + "," +
                      Utils.EStrNull(dat_s.ToShortDateString()) + "," +
                      sum_dolg + "," + sum_dolg + "," + sumOdna12List[i] + "," + sumPercList[i] + "," + sum_outdolg +
                      "," + sum_charge + ", " + sumoutdolgmain + "," + sumDolgMain + ")";
                ExecSQLWE(conn_db, sql);

                sum_dolg = sum_outdolg;
                sumDolgMain = sumoutdolgmain;
            }
        }

        /// <summary>
        /// получить данные по перекидке
        /// </summary>
        /// <param name="tempTable"></param>
        /// <param name="isExists"></param>
        /// <param name="finder"></param>
        /// <param name="currentCalcMonth"></param>
        /// <param name="conn_db"></param>
        /// <param name="transaction"></param>
        private void GetPerekidkaData(string tempTable, bool notExists, int nzp_kvar, DateTime currentCalcMonth, IDbConnection conn_db, IDbTransaction transaction)
        {
            string sql = "drop table " + tempTable;
            ExecSQL(conn_db, transaction, sql, false);

            sql = "create temp table " + tempTable + " (" +
                " nzp_kvar integer, " +
                " num_ls   integer, " +
                " nzp_serv integer, " +
                " nzp_supp integer, " +
                " sum_charge " + DBManager.sDecimalType + " (14,2) )";
            ExecSQLWE(conn_db, transaction, sql);

            sql = "insert into " + tempTable + " (nzp_kvar, num_ls, nzp_serv, nzp_supp, sum_charge) " +
                " select nzp_kvar, num_ls, nzp_serv, nzp_supp, sum_charge " +
                " from " + chargePeni + " c " +
                " where c.nzp_serv = " + (int)ServPeni.nzp_serv +
                    " and c.nzp_kvar = " + nzp_kvar +
                    " and " + (notExists ? " not " : "") + " exists " + 
                        " (select 1 from " + perekidka + " p " +
                        " where   c.nzp_kvar = p.nzp_kvar " +
                            " and c.nzp_serv = p.nzp_serv " +
                            " and p.type_rcl = " + (int)PerekidkaType.nach_k_oplate +
                            " and p.month_ = " + currentCalcMonth.Month +
                            " and c.nzp_supp = p.nzp_supp) ";
            ExecSQLWE(conn_db, transaction, sql);

            sql = " create index ix_" + tempTable + "_1 on " + tempTable + " (nzp_kvar, nzp_serv, nzp_supp)";
            ExecSQLWE(conn_db, transaction, sql);

            sql = DBManager.sUpdStat + " " + tempTable;
            ExecSQLWE(conn_db, transaction, sql);
        }

        /// <summary>
        /// добавить сумму перекидки
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="conn_db"></param>
        /// <param name="transaction"></param>
        private void AddPeniPerekidka(Credit finder, DateTime currentCalcMonth, IDbConnection conn_db, IDbTransaction transaction)
        {
            perekidka = finder.pref + "_charge_" + (currentCalcMonth.Year % 100) + tableDelimiter + "perekidka ";
            DateTime dd = currentCalcMonth.AddMonths(-1);
            chargePeni = finder.pref + "_charge_" + (dd.Year % 100) + tableDelimiter + "charge_" + dd.Month.ToString("00");

            // получить данные о начислениях, которые уже есть в перекидках
            GetPerekidkaData("tmp_kredit_perekidka_exists", false, finder.nzp_kvar, currentCalcMonth, conn_db, transaction);

            // обновить суммы перекидки
            string sql = "update " + perekidka + " p set " + 
                " sum_rcl = (select (-1) * sum_charge from tmp_kredit_perekidka_exists t " + 
                    " where   p.nzp_kvar = t.nzp_kvar " +
                        " and p.nzp_serv = t.nzp_serv " +
                        " and p.nzp_supp = t.nzp_supp), " +
                " date_rcl = " + DBManager.sCurDate + "," + 
                " comment = 'Автоматическая перекидка из-за рассрочки', " + 
                " nzp_user = " + Constants.AutoDefeferredPayUserID + 
                " WHERE p.type_rcl = " + (int)PerekidkaType.nach_k_oplate +
                    " and p.month_ = " + currentCalcMonth.Month + 
                    " and exists (select 1 from tmp_kredit_perekidka_exists t " + 
                        " where   p.nzp_kvar = t.nzp_kvar " +
                            " and p.nzp_serv = t.nzp_serv " +
                            " and p.nzp_supp = t.nzp_supp) ";
            ExecSQLWE(conn_db, transaction, sql);

            // получить данные о начислениях, которых нет в перекидках
            GetPerekidkaData("tmp_kredit_perekidka_not_exists", true, finder.nzp_kvar, currentCalcMonth, conn_db, transaction);
            
            // вставить перекидки
            sql = "insert into " + perekidka +
                " (nzp_kvar, num_ls, nzp_serv, nzp_supp, type_rcl, date_rcl, sum_rcl, month_, comment, nzp_user) " +
                " select nzp_kvar, num_ls, nzp_serv, nzp_supp, " + (int)PerekidkaType.nach_k_oplate + "," + DBManager.sCurDate + ", (-1)*sum_charge, " + currentCalcMonth.Month + "," +
                " 'Автоматическая перекидка из-за рассрочки', " + Constants.AutoDefeferredPayUserID +
                " from tmp_kredit_perekidka_not_exists ";
            ExecSQLWE(conn_db, transaction, sql);

            ExecSQL(conn_db, transaction, "drop table tmp_kredit_perekidka_exists", false);
            ExecSQL(conn_db, transaction, "drop table tmp_kredit_perekidka_not_exists", false);
        }

        /// <summary>
        /// Проверка входных данных
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        private Returns CheckSaveData(IDbConnection conn_db, Credit finder, DateTime currentCalcMonth)
        {
            if (finder.nzp_kvar < 1)
                return new Returns(false, "Не указан лицевой счет");

            if (finder.nzp_kredit < 1)
                return new Returns(false, "Не указана рассрочка");
            
            if (finder.valid == 1)
            {
                Credit credit = GetCreditById(conn_db, finder.nzp_kredit);
                DateTime kreditDatMonth;
                if (!DateTime.TryParse(credit.dat_month, out kreditDatMonth)) return new Returns(false, "Не удалось определить месяц возникновения рассрочки");

                if (currentCalcMonth.Year == kreditDatMonth.Year && currentCalcMonth.Month == kreditDatMonth.Month)
                {
                    return new Returns(false, "Рассрочка не может быть предоставлена в месяце возникновения рассрочки. Рассрочка предоставляется в следующем месяце");
                }
            }

            return new Returns(true);
        }

        /// <summary>
        /// Получить данные по пени 
        /// </summary>
        /// <param name="tempTableName"></param>
        private void GetPeniData(string tempTable, string where, Credit finder, IDbConnection conn_db, IDbTransaction transaction)
        {
            ExecSQL(conn_db, transaction, "Drop table " + tempTable, false);

            string sql = "create temp table " + tempTable + " (" +
                " nzp_tarif integer, " +
                " nzp_kvar  integer, " +
                " num_ls    integer, " +
                " nzp_serv  integer, " +
                " nzp_supp  integer, " +
                " nzp_frm   integer, " +
                " tarif     float, " +
                " nzp_wp    integer " + ")";
            ExecSQLWE(conn_db, transaction, sql);

            sql = "insert into " + tempTable + " (nzp_tarif, nzp_kvar, num_ls, nzp_serv, nzp_supp, nzp_frm, tarif, nzp_wp) " +
                "select nzp_tarif, nzp_kvar, num_ls, nzp_serv, nzp_supp, nzp_frm, tarif, nzp_wp " +
                " from " + finder.pref + DBManager.sDataAliasRest + "tarif " +
                " where nzp_kvar = " + finder.nzp_kvar +
                "   and nzp_serv = " + (int)ServPeni.nzp_serv +
                "   and is_actual = 1 " +
                where;
            ExecSQLWE(conn_db, transaction, sql);

            sql = " create index ix_" + tempTable + "_nzp_tarif on " + tempTable + " (nzp_tarif)";
            ExecSQLWE(conn_db, transaction, sql);

            sql = " create index ix_" + tempTable + "_1 on " + tempTable + " (nzp_kvar, nzp_serv, nzp_supp, nzp_frm)";
            ExecSQLWE(conn_db, transaction, sql);

            sql = DBManager.sUpdStat + " " + tempTable;
            ExecSQLWE(conn_db, transaction, sql);
        }

        /// <summary>
        /// Закрыть услугу пени
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="conn_db"></param>
        /// <param name="transaction"></param>
        private void ClosePeni(Credit finder, DateTime currentCalcMonth, IDbConnection conn_db, IDbTransaction transaction)
        {
            // Закрыть в текущем расчетном месяце услугу пени
            // ... получить информацию о пени в текущем расчетном месяце
            GetPeniData("tmp_kredit_peni_current", " and " + Utils.EStrNull(currentCalcMonth.ToShortDateString()) + " between dat_s and dat_po", finder, conn_db, transaction);
            
            // ...закрыть пени в текущем расчетном месяце
            string sql = " update " + finder.pref + DBManager.sDataAliasRest + "tarif set " +
                " dat_po = " + Utils.EStrNull(currentCalcMonth.AddDays(-1).ToShortDateString()) + "," +
                " nzp_user = " + Constants.AutoDefeferredPayUserID + "," +
                " user_del = " + Constants.AutoDefeferredPayUserID + "," +
                " dat_when = " + DBManager.sCurDate +
                " where nzp_tarif in (select nzp_tarif from tmp_kredit_peni_current)";
            ExecSQLWE(conn_db, transaction, sql);

            // ... сбросить в архив услугу пени, если дата начала больше даты окончания
            sql = " update " + finder.pref + DBManager.sDataAliasRest + "tarif set " +
                " is_actual = 100, " +
                " dat_del = " + DBManager.sCurDate +
                " where nzp_tarif in (select nzp_tarif from tmp_kredit_peni_current) " +
                "   and dat_s > dat_po ";
            ExecSQLWE(conn_db, transaction, sql);

            // Открыть услугу пени в будущем
            // ... получить информацию о пени, дата начала действия которого больше текущего расчетного месяца
            GetPeniData("tmp_kredit_peni_future", " and dat_s >= " + Utils.EStrNull(currentCalcMonth.ToShortDateString()), finder, conn_db, transaction);
            
            // ... подвинуть дату начала действия услуги пени
            DateTime futurePeniDatS = currentCalcMonth.AddMonths(12);

            sql = " update " + finder.pref + DBManager.sDataAliasRest + "tarif a set " +
                " dat_s  = " + Utils.EStrNull(futurePeniDatS.ToShortDateString()) + "," +
                " dat_when = " + DBManager.sCurDate +
                " where nzp_tarif in (select nzp_tarif from tmp_kredit_peni_future) ";
            ExecSQLWE(conn_db, sql);

            // добавить услугу пени
            // ... вставить строки из текущего расчетного месяца, которых нет в будущем  
            sql = " insert into " + finder.pref + DBManager.sDataAliasRest + "tarif " +
                " (nzp_kvar, num_ls, nzp_serv, nzp_supp, nzp_frm, tarif, nzp_wp, " +
                " dat_s, dat_po, is_actual, nzp_user, user_del, dat_when) " +
                " select nzp_kvar, num_ls, nzp_serv, nzp_supp, nzp_frm, tarif, nzp_wp, " +
                Utils.EStrNull(futurePeniDatS.ToShortDateString()) + ", " + // dat_s
                " '01.01.3000', " + //dat_po
                " 1, " + // is_actual
                Constants.AutoDefeferredPayUserID + "," + // nzp_user
                Constants.AutoDefeferredPayUserID + "," + // user_del
                DBManager.sCurDate + // dat_when
                " from tmp_kredit_peni_current t " +
                " WHERE not exists (select 1 from tmp_kredit_peni_future a " + 
                    " where   t.nzp_kvar = a.nzp_kvar " +
                        " and t.nzp_supp = a.nzp_supp " +
                        " and t.nzp_frm  = a.nzp_frm " +
                        " and t.nzp_serv = a.nzp_serv)";
            ExecSQLWE(conn_db, sql);

            ExecSQL(conn_db, "Drop table tmp_kredit_peni_current", false);
            ExecSQL(conn_db, "Drop table tmp_kredit_peni_future", false);
        }

        /// <summary>
        /// Проверка входных данных
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        private Returns CheckGetData(Credit finder)
        {
            if (finder.nzp_kvar < 1)
                return new Returns(false, "Не указан лицевой счет");

            if (finder.year_ < 1 || finder.month_ < 1)
                return new Returns(false, "Не указан расчетный месяц");

            return new Returns(true);
        }

        /// <summary>
        /// Получить ставку рефинансирования
        /// </summary>
        /// <param name="pref"></param>
        /// <param name="conn_db"></param>
        /// <param name="refinance"></param>
        /// <returns></returns>
        private Returns GetRefinance(string pref, IDbConnection conn_db, out decimal refinance)
        {
            refinance = 0;
            IDataReader reader = null;

            try
            {
                string sql = "select val_prm from " + pref + DBManager.sDataAliasRest + "prm_10 " + 
                    " where nzp_prm = 85 " + 
                    "   and is_actual <> 100 " +   
                    "   and " + DBManager.sCurDateTime + " between dat_s and dat_po";

                Returns ret = ExecRead(conn_db, out reader, sql, true);
                if (!ret.result) throw new Exception(ret.text);

                if (!reader.Read())
                {
                    return new Returns(false, "Не определен процент пени (1/300 от ставки рефинансирования)", 100);
                }
                else
                {
                    refinance = Convert.ToDecimal(reader["val_prm"]);    
                }

            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                    reader.Dispose();
                }           
            }

            return new Returns(true);
        }
        
        /// <summary>
        /// Получить данные по рассрочке
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public List<Credit> GetCredit(Credit finder, out Returns ret)
        {
            ret = CheckGetData(finder);
            if (!ret.result) return null;

            IDataReader reader = null;
            IDbConnection conn_db = null;
            List<Credit> list = new List<Credit>();

            try
            {
                conn_db = GetConnection(Points.GetConnByPref(Points.Pref));
                ret = OpenDb(conn_db, true);
                if (!ret.result) throw new Exception(ret.text);

                decimal refinance = 0;
                ret = GetRefinance(finder.pref, conn_db, out refinance);
                if (!ret.result && ret.tag == 100) return null;
                
                if (!ret.result) throw new Exception(ret.text);

                string sql =
                    "select k.nzp_kredit, k.nzp_serv, s.service, k.dat_month, k.dat_s, k.dat_po, k.valid, k.sum_dolg, k.perc, k.dog_num, k.dog_dat " +
                    " from " + Points.Pref + DBManager.sDebtAliasRest + "kredit k " +
                    "   left outer join " + Points.Pref + DBManager.sKernelAliasRest + "services s on k.nzp_serv = s.nzp_serv " +
                    " where k.nzp_kvar = " + finder.nzp_kvar +
                    "   and k.valid is not null " +
                    "   and " + DBManager.MDY(finder.month_, 1, finder.year_) + " between k.dat_s and k.dat_po " +
                    " order by s.service, k.dat_month, k.dat_s, k.dat_po, k.nzp_kredit ";

                ret = ExecRead(conn_db, out reader, sql, true);
                if (!ret.result) throw new Exception(ret.text);

                Credit credit;
                while (reader.Read())
                {
                    credit = new Credit();
                    credit.nzp_kvar = finder.nzp_kvar;
                    credit.service = "";

                    if (reader["nzp_kredit"] != DBNull.Value) credit.nzp_kredit = Convert.ToInt32(reader["nzp_kredit"]);
                    if (reader["nzp_serv"] != DBNull.Value) credit.nzp_serv = Convert.ToInt32(reader["nzp_serv"]);
                    if (reader["service"] != DBNull.Value) credit.service = Convert.ToString(reader["service"]).Trim();
                    if (reader["dat_month"] != DBNull.Value) credit.dat_month = Convert.ToDateTime(reader["dat_month"]).ToString("MM-yyyy");
                    if (reader["dat_s"] != DBNull.Value) credit.dat_s = Convert.ToDateTime(reader["dat_s"]).ToShortDateString();
                    if (reader["dat_po"] != DBNull.Value) credit.dat_po = Convert.ToDateTime(reader["dat_po"]).ToShortDateString();
                    if (reader["valid"] != DBNull.Value) credit.valid = Convert.ToInt32(reader["valid"]);
                    if (reader["sum_dolg"] != DBNull.Value) credit.sum_dolg = Convert.ToDecimal(reader["sum_dolg"]);
                    if (reader["perc"] != DBNull.Value) credit.perc = Convert.ToDecimal(reader["perc"]);
                    if (reader["dog_num"] != DBNull.Value) credit.dog_num = Convert.ToString(reader["dog_num"]);
                    if (reader["dog_dat"] != DBNull.Value) credit.dog_dat = Convert.ToDateTime(reader["dog_dat"]);
                    
                    switch (credit.valid)
                    {
                        case 0:
                            credit.state_id = "Рассрочка может быть предоставлена";
                            break;
                        case 1:
                            credit.state_id = "Рассрочка предоставляется";
                            break;
                        case 3:
                            credit.state_id = "Рассрочка нарушена";
                            break;
                        case 4:
                            credit.state_id = "Рассрочка закрыта";
                            break;
                    }

                    credit.refinance_state = refinance;
                    list.Add(credit);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка в функции DbCharge.SaveCredit" + (Constants.Viewerror ? ":\n" + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                ret = new Returns(false, "Ошибка при получении рассрочки");               
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                    reader.Dispose();
                }
                
                if (conn_db != null)
                {
                    conn_db.Close();
                    conn_db.Dispose();
                }
            }

            if (!ret.result) return null;
            else return list;
        }

        /// <summary>
        /// Получить данные по рассрочке
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        private Credit GetCreditById(IDbConnection conn_db, int nzp_kredit)
        {
            string sql = "select nzp_kredit, nzp_kvar, nzp_serv, dat_month, dat_s, dat_po, valid, dog_num, dog_dat, sum_dolg, perc, sum_real_p  " +
                    " from " + Points.Pref + DBManager.sDebtAliasRest + "kredit " +
                    " where nzp_kredit = " + nzp_kredit;

            IDataReader reader = null;
            ExecRead(conn_db, out reader, sql, true);

            Credit credit = new Credit();
            if (reader.Read())
            {
                if (reader["nzp_kredit"] != DBNull.Value) credit.nzp_kredit = Convert.ToInt32(reader["nzp_kredit"]);
                if (reader["nzp_kvar"] != DBNull.Value) credit.nzp_kvar = Convert.ToInt32(reader["nzp_kvar"]);
                if (reader["nzp_serv"] != DBNull.Value) credit.nzp_serv = Convert.ToInt32(reader["nzp_serv"]);
                if (reader["dat_month"] != DBNull.Value) credit.dat_month = Convert.ToDateTime(reader["dat_month"]).ToShortDateString();
                if (reader["dat_s"] != DBNull.Value) credit.dat_s = Convert.ToDateTime(reader["dat_s"]).ToShortDateString();
                if (reader["dat_po"] != DBNull.Value) credit.dat_po = Convert.ToDateTime(reader["dat_po"]).ToShortDateString();
                if (reader["valid"] != DBNull.Value) credit.valid = Convert.ToInt32(reader["valid"]);
                if (reader["dog_num"] != DBNull.Value) credit.dog_num = Convert.ToString(reader["dog_num"]);
                if (reader["dog_dat"] != DBNull.Value) credit.dog_dat = Convert.ToDateTime(reader["dog_dat"]);
                if (reader["sum_dolg"] != DBNull.Value) credit.sum_dolg = Convert.ToDecimal(reader["sum_dolg"]);
                if (reader["perc"] != DBNull.Value) credit.perc = Convert.ToDecimal(reader["perc"]);
            }

            return credit;
        }

        /// <summary>
        /// Получить график погашения рассрочки
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public List<CreditDetails> GetCreditDetails(CreditDetails finder, out Returns ret)
        {
            if (finder.nzp_kredit < 1)
            {
                ret = new Returns(false, "Не указан код рассрочки");
                return null;
            }

            IDataReader reader = null;
            IDbConnection conn_db = null;
            List<CreditDetails> list = new List<CreditDetails>();

            try
            {
                conn_db = GetConnection(Points.GetConnByPref(Points.Pref));
                ret = OpenDb(conn_db, true);
                if (!ret.result) throw new Exception(ret.text);

                decimal refinance = 0;
                ret = GetRefinance(finder.pref, conn_db, out refinance);

                string sql =
                    "select calc_month, sum_indolg, sum_dolg, sum_odna12, sum_perc, sum_charge, sum_outdolg, sum_money " +
                    " from " + Points.Pref + DBManager.sDebtAliasRest + "kredit_pay " +
                    " where nzp_kredit = " + finder.nzp_kredit +
                    " order by calc_month ";
                ret = ExecRead(conn_db, out reader, sql, true);
                if (!ret.result) throw new Exception(ret.text);

                CreditDetails credit;
                DateTime calc_month;

                while (reader.Read())
                {
                    credit = new CreditDetails();
                    if (reader["calc_month"] != DBNull.Value)
                    {
                        calc_month = Convert.ToDateTime(reader["calc_month"]);
                        credit.month_ = calc_month.Month;
                        credit.year_ = calc_month.Year;
                    }
                    
                    if (reader["sum_indolg"] != DBNull.Value) credit.sum_indolg = Convert.ToDecimal(reader["sum_indolg"]);
                    if (reader["sum_dolg"] != DBNull.Value) credit.sum_dolg = Convert.ToDecimal(reader["sum_dolg"]);
                    if (reader["sum_odna12"] != DBNull.Value) credit.sum_odna12 = Convert.ToDecimal(reader["sum_odna12"]);
                    if (reader["sum_perc"] != DBNull.Value) credit.sum_perc = Convert.ToDecimal(reader["sum_perc"]);
                    if (reader["sum_charge"] != DBNull.Value) credit.sum_charge = Convert.ToDecimal(reader["sum_charge"]);
                    if (reader["sum_outdolg"] != DBNull.Value) credit.sum_outdolg = Convert.ToDecimal(reader["sum_outdolg"]);
                    if (reader["sum_money"] != DBNull.Value) credit.sum_money = Convert.ToDecimal(reader["sum_money"]);
                    list.Add(credit);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка в функции DbCharge.SaveCredit" + (Constants.Viewerror ? ":\n" + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                ret = new Returns(false, "Ошибка при получении графика погашения");
            }
            finally
            {
                if (conn_db != null)
                {
                    conn_db.Close();
                    conn_db.Dispose();
                }
            }

            if (!ret.result) return null;
            else return list;
        }

    }
}
