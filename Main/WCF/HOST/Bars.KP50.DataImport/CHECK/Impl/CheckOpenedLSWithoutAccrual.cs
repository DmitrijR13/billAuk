using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Bars.KP50.DataImport.CHECK.Report;
using Bars.KP50.Utils;
using Globals.SOURCE.GLOBAL;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.DataImport.CHECK.Impl
{
    public class CheckOpenedLSWithoutAccrual : CheckBeforeClosing
    {
        private string tResult;
        private string tOpenedLS;
        public CheckOpenedLSWithoutAccrual(CheckBeforeClosingParams Params) :
            base(Params)
        {
            IsCritical = false;
            Month = Points.GetCalcMonth(new CalcMonthParams { pref = inputParams.Bank.pref }).month_;
            Year = Points.GetCalcMonth(new CalcMonthParams { pref = inputParams.Bank.pref }).year_;

            tResult = "t_tOpenedLSWithoutAccrual" + inputParams.User.nzp_user;
            tOpenedLS = "t_tOpenedLSWAccrualOpenLs" + inputParams.User.nzp_user;
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
            int nextMonth = (Month < 12 ? Month + 1 : 1);
            int nextYear = (Month < 12 ? Year : Year + 1);
            string firstDay = "01." + Month.ToString("00") + "." + Year;
            string nextFirstDay = "01." + nextMonth.ToString("00") + "." + nextYear;
            string chargeXX = inputParams.Bank.pref + "_charge_" + Year.ToString().Substring(2, 2) +
                DBManager.tableDelimiter + "charge_" + Month.ToString("00");
            string sql;

            try
            {
                sql =
                    " INSERT INTO " + tOpenedLS +
                    " SELECT DISTINCT nzp FROM  " + inputParams.Bank.pref + DBManager.sDataAliasRest + "prm_3" +
                    " WHERE is_actual<>100" +
                    " AND dat_s <='" + nextFirstDay + "' and dat_po>='" + firstDay + "'" +
                    " AND nzp_prm = 51";
                ExecSQL(sql);

                sql = 
                    " INSERT INTO " + tResult + 
                    " (nzp_group, nzp)" +
                    " SELECT DISTINCT " + (int)ECheckGroupId.OpenedLSWithoutAccrual + ", c.nzp_kvar" +
                    " FROM " + chargeXX + " c," + 
                    tOpenedLS + " k" +
                    " WHERE  dat_charge is null AND nzp_serv > 1 AND" +
                    " c.rsum_tarif = 0  AND c.nzp_kvar = k.nzp";
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
            InsertIntoCheckChMon((int)ECheckGroupId.OpenedLSWithoutAccrual, ret.result && count == 0, tResult);

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
                   " CREATE TEMP TABLE " + tOpenedLS +
                   " (nzp INTEGER)" +
                   DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        private void DropTempTable()
        {
            string sql = " DROP TABLE " + tResult;
            ExecSQL(sql, false);

            sql = " DROP TABLE " + tOpenedLS;
            ExecSQL(sql, false);
        }

        public override void CreateCheckBeforeClosingReport()
        {
            Report_CheckOpenedLSWithoutAccrual report3 = new Report_CheckOpenedLSWithoutAccrual(Connection, inputParams);
            report3.GetReport();
        }
    }
}
