using System;
using Globals.SOURCE.GLOBAL;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.DataImport.CHECK.Impl
{
    public class CheckFinMonthOperDay : CheckBeforeClosing
    {
        private string tResult;
        public CheckFinMonthOperDay(CheckBeforeClosingParams Params) :
            base(Params)
        {
            IsCritical = false;
            Month = Points.GetCalcMonth(new CalcMonthParams { pref = inputParams.Bank.pref }).month_;
            Year = Points.GetCalcMonth(new CalcMonthParams { pref = inputParams.Bank.pref }).year_;

            tResult = "t_tResFinMonthOperDay" + inputParams.User.nzp_user;
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

            int finyear = Points.DateOper.Year;
            int finmonth = Points.DateOper.Month;

            if (Year * 12 + Month - finyear * 12 - finmonth > 0)
            {
                ret = new Returns(false, "Финансовый месяц не может отличаться от" +
                                         " расчетного более чем на 1 месяц (финансовый:" + finmonth.ToString("00") + "." + finyear + "," +
                                         " расчетный " + Month.ToString("00") + "." + Year + ")", -1);

                MonitorLog.WriteLog(
                    "Проверка не прошла: " + System.Reflection.MethodInfo.GetCurrentMethod().Name +
                    Environment.NewLine + "Финансовый месяц не может отличаться от" +
                                         " расчетного более чем на 1 месяц (финансовый:" + finmonth.ToString("00") + "." + finyear + "," +
                                         " расчетный " + Month.ToString("00") + "." + Year + ")", MonitorLog.typelog.Error, true);
            }


            InsertIntoCheckChMon((int)ECheckGroupId.FinMonthOperDay, ret.result , tResult);

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
        }
    }
}
