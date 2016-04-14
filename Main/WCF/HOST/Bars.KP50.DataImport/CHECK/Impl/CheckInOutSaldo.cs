using System;
using Bars.KP50.DataImport.CHECK.Report;
using Bars.KP50.Utils;
using Globals.SOURCE.GLOBAL;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.DataImport.CHECK.Impl
{
   
    public class CheckInOutSaldo : CheckBeforeClosing
    {
        //таблицы для формирования групп link_group
        private string tResult;
        private string tForReport;

        private string tPInSaldo;
        private string tPOutSaldo;

        public CheckInOutSaldo(CheckBeforeClosingParams Params) :
            base(Params)
        {
            IsCritical = true;
            Month = Points.GetCalcMonth(new CalcMonthParams { pref = inputParams.Bank.pref }).month_;
            Year = Points.GetCalcMonth(new CalcMonthParams { pref = inputParams.Bank.pref }).year_;

            tResult = "t_tResultCheckInOutSaldo" + inputParams.User.nzp_user;
            tForReport = "t_tForReportCheckInOutSaldo" + inputParams.User.nzp_user;

            tPInSaldo = "t_tCheckInOutSaldoPInSaldo" + inputParams.User.nzp_user;
            tPOutSaldo = "t_tCheckInOutSaldoPOutSaldo" + inputParams.User.nzp_user;
        }

        /// <summary>
        /// Запуск проверки
        /// </summary>
        /// <param name="inputParams"></param>
        public override Returns StartCheck()
        {
            try
            {
                DropTempTable();
                CreateTempTable();
                Returns ret = Run();
                if (!ret.result) 
                    CreateCheckBeforeClosingReport();
                return ret;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog(
                    "Ошибка выполнения процедуры: " + System.Reflection.MethodInfo.GetCurrentMethod().Name +
                    Environment.NewLine + ex.Message + ex.StackTrace, MonitorLog.typelog.Error, true);
                return new Returns(false);
            }
            finally
            {
                DropTempTable();
            }
        }

        /// <summary>
        /// Тело проверки
        /// </summary>
        /// <returns></returns>
        private Returns Run()
        {
            Returns ret = STCLINE.KP50.Global.Utils.InitReturns();
            int prevMonth = (Month == 1 ? 12 : Month - 1);
            int prevYear = (Month == 1 ? Year - 1 : Year);
            string chargeXX = inputParams.Bank.pref + "_charge_" + Year.ToString().Substring(2, 2) +
                DBManager.tableDelimiter + "charge_" + Month.ToString("00");
            string prevChargeXX = inputParams.Bank.pref + "_charge_" + prevYear.ToString().Substring(2, 2) +
                DBManager.tableDelimiter + "charge_" + prevMonth.ToString("00");
            string sql;
            decimal delta = 0.0001M;

            try
            {
                sql =
                    " INSERT INTO " + tPInSaldo +
                    " (nzp_kvar, sum_insaldo)" +
                    " SELECT nzp_kvar, sum(sum_insaldo)" +
                    " FROM " + chargeXX +
                    " WHERE dat_charge is null and nzp_serv > 1" +
                    " GROUP BY nzp_kvar";
                ExecSQL(sql);

                sql = " CREATE INDEX inx_ChInOutSaldo_in" + inputParams.User.nzp_user + " on " + tPInSaldo + " (nzp_kvar)";
                ExecSQL(sql, false);
                sql = DBManager.sUpdStat + " " + tPInSaldo;
                ExecSQL(sql, false);


                sql =
                    " INSERT INTO " + tPOutSaldo +
                    " (nzp_kvar, sum_outsaldo)" +
                    " SELECT nzp_kvar, sum(sum_outsaldo)" +
                    " FROM " + prevChargeXX +
                    " WHERE dat_charge is null and nzp_serv > 1" +
                    " GROUP BY nzp_kvar";
                ExecSQL(sql);

                sql = " CREATE INDEX inx_ChInOutSaldo_out" + inputParams.User.nzp_user + " on " + tPOutSaldo + " (nzp_kvar)";
                ExecSQL(sql, false);
                sql = DBManager.sUpdStat + " " + tPInSaldo;
                ExecSQL(sql, false);

                sql =
                    " INSERT INTO " + tResult +
                    " (nzp_group, nzp)" +
                    " SELECT DISTINCT  " + (int)ECheckGroupId.InOutSaldo + ", i.nzp_kvar" +
                    " FROM " + tPInSaldo + " i," +
                    tPOutSaldo + " o " +
                    " WHERE i.nzp_kvar = o.nzp_kvar AND ABS (i.sum_insaldo - o.sum_outsaldo) > " + delta;
                ExecSQL(sql);

            }
            catch (Exception ex)
            {
                ret.result = false;
                MonitorLog.WriteLog(ret.text + " " + ex.Message + " " + ex.StackTrace, MonitorLog.typelog.Error, true);
            }

            //Анализ результата
            sql = " SELECT count(*) FROM " + tResult;
            int count = ExecScalar(sql).ToInt();
            ret.result = ret.result && (count == 0);
            InsertIntoCheckChMon((int)ECheckGroupId.InOutSaldo, ret.result && count == 0, tResult);

            return ret;
        }

        private void CreateTempTable()
        {
            string sql =
                " CREATE TEMP TABLE " + tResult +
                " (nzp_group INTEGER," +
                " nzp INTEGER)" +
                DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql =
                " CREATE TEMP TABLE " + tPInSaldo +
                " (nzp_kvar INTEGER," +
                " sum_insaldo " + DBManager.sDecimalType + "(14,2))" +
                DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql =
                " CREATE TEMP TABLE " + tPOutSaldo +
                " (nzp_kvar INTEGER," +
                " sum_outsaldo " + DBManager.sDecimalType + "(14,2))" +
                DBManager.sUnlogTempTable;
            ExecSQL(sql);
            
            sql =
                " CREATE TEMP TABLE " + tForReport +
                " (num_ls INTEGER," +
                " nzp_serv INTEGER)" +
                DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        private void DropTempTable()
        {
            string sql = " DROP TABLE " + tResult;
            ExecSQL(sql, false);

            sql = " DROP TABLE " + tForReport;
            ExecSQL(sql, false);

            sql = " DROP TABLE " + tPInSaldo;
            ExecSQL(sql, false);

            sql = " DROP TABLE " + tPOutSaldo;
            ExecSQL(sql, false);
        }

        /// <summary>
        /// Создание отчета по выполненной проверке
        /// </summary>
        public override void CreateCheckBeforeClosingReport()
        {
            Report_CheckInOutSaldo report3 = new Report_CheckInOutSaldo(Connection, inputParams);
            report3.GetReport();
        }


    }
}
