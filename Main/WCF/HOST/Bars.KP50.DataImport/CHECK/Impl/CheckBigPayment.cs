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
    public class CheckBigPayment : CheckBeforeClosing
    {
        private string tResult;
        public CheckBigPayment(CheckBeforeClosingParams Params) :
            base(Params)
        {
            IsCritical = false;
            Month = Points.GetCalcMonth(new CalcMonthParams { pref = inputParams.Bank.pref }).month_;
            Year = Points.GetCalcMonth(new CalcMonthParams { pref = inputParams.Bank.pref }).year_;

            tResult = "t_tResCheckBigPayment" + inputParams.User.nzp_user;
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
            decimal maxVal;
            string sql;

            try
            {
                sql =
                    " SELECT val_prm, nzp_key FROM  " + inputParams.Bank.pref + DBManager.sDataAliasRest + "prm_5" +
                    " WHERE is_actual<>100" +
                    " AND dat_s <='" + nextFirstDay + "' and dat_po>='" + firstDay + "'" +
                    " AND nzp_prm = 1046" +
                    " ORDER BY nzp_key DESC";
                DataTable dt = ExecSQLToTable(sql);
                if (dt.Rows.Count < 1)
                {
                    InsertIntoCheckChMon((int)ECheckGroupId.BigPayment, false, tResult);

                    return new Returns(false);
                }
                maxVal = dt.Rows[0]["val_prm"].ToDecimal();

                sql = 
                    " INSERT INTO " + tResult + 
                    " (nzp_group, nzp)" +
                    " SELECT DISTINCT " + (int)ECheckGroupId.BigPayment + ", nzp_kvar" +
                    " FROM " + chargeXX +
                    " WHERE  dat_charge is null AND nzp_serv > 1 AND " +
                    " (rsum_tarif > " + maxVal + " OR reval > " + maxVal + ") ";
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
            InsertIntoCheckChMon((int)ECheckGroupId.BigPayment, ret.result && count == 0, tResult);

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
        }

        private void DropTempTable()
        {
            string sql = " DROP TABLE " + tResult;
            ExecSQL(sql, false);
        }

        public override void CreateCheckBeforeClosingReport()
        {
            Report_CheckBigPayment report3 = new Report_CheckBigPayment(Connection, inputParams);
            report3.GetReport();
        }
    }
}
