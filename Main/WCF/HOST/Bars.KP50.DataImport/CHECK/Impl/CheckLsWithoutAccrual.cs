using System;
using System.Collections.Generic;
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
    public class CheckLsWithoutAccrual: CheckBeforeClosing
    {
        //таблицы для формирования групп link_group
        private string tResult;
        private string tOpenLs;


        public CheckLsWithoutAccrual(CheckBeforeClosingParams Params) :
            base(Params)
        {
            IsCritical = false;
            Month = Points.GetCalcMonth(new CalcMonthParams { pref = inputParams.Bank.pref }).month_;
            Year = Points.GetCalcMonth(new CalcMonthParams { pref = inputParams.Bank.pref }).year_;

            tResult = "t_CheckLsWithoutAccrual" + inputParams.User.nzp_user;
            tOpenLs = "t_tOpenLsCheckLsWithoutAccrual" + inputParams.User.nzp_user;
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
            string kvar = inputParams.Bank.pref + DBManager.sDataAliasRest + "kvar";
            string prm_3 = inputParams.Bank.pref + DBManager.sDataAliasRest + "prm_3";
            string sql;
            decimal delta = 0.0001M;

            try
            {
                sql =
                    " INSERT INTO " + tOpenLs +
                    " (nzp_kvar)" +
                    " SELECT DISTINCT k.nzp_kvar" +
                    " FROM " + kvar + " k," + 
                    prm_3 + " p" +
                    " WHERE  k.nzp_kvar = p.nzp and p.is_actual <> 100" +
                    " AND p.nzp_prm = 51 AND p.val_prm = '1'" +
                    " AND p.dat_s<'<" + nextFirstDay + "' AND p.dat_po>='" + firstDay + "'";
                ExecSQL(sql);

                sql =
                    " CREATE INDEX ind_" + tOpenLs + "_nzp_kvar ON " + tOpenLs + "(nzp_kvar)";
                ExecSQL(sql);

                sql =
                    DBManager.sUpdStat + " " + tOpenLs;
                ExecSQL(sql);

                sql =
                    " INSERT INTO " + tResult +
                    " (nzp_group, nzp)" +
                    " SELECT DISTINCT  " + (int)ECheckGroupId.LsWithoutAccrual + ", c.nzp_kvar" +
                    " FROM " + chargeXX + " c" +
                    " WHERE c.dat_charge is null AND c.nzp_serv = 1 AND c.rsum_tarif < " + delta +
                    " AND EXISTS (SELECT 1 FROM " + tOpenLs + " k WHERE k.nzp_kvar = c.nzp_kvar)";
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
            InsertIntoCheckChMon((int)ECheckGroupId.LsWithoutAccrual, ret.result && count == 0, tResult);

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
                " CREATE TEMP TABLE " + tOpenLs +
                " (nzp_kvar INTEGER)" +
                DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        private void DropTempTable()
        {
            string sql = " DROP TABLE " + tResult;
            ExecSQL(sql, false);

            sql = " DROP TABLE " + tOpenLs;
            ExecSQL(sql, false);
        }

        /// <summary>
        /// Создание отчета по выполненной проверке
        /// </summary>
        public override void CreateCheckBeforeClosingReport()
        {
            var report3 = new Report_CheckLsWithoutAccrual(Connection, inputParams);
            report3.GetReport();
        }


    }
}

