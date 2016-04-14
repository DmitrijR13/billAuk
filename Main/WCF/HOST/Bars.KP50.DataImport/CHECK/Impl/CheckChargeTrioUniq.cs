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
    public class CheckChargeTrioUniq : CheckBeforeClosing
    {
        //таблицы для формирования групп link_group
        private string tResult;
        private string tDubl;


        public CheckChargeTrioUniq(CheckBeforeClosingParams Params) :
            base(Params)
        {
            IsCritical = true;
            Month = Points.GetCalcMonth(new CalcMonthParams { pref = inputParams.Bank.pref }).month_;
            Year = Points.GetCalcMonth(new CalcMonthParams { pref = inputParams.Bank.pref }).year_;

            tResult = "t_tResultCheckInOutSaldo" + inputParams.User.nzp_user;
            tDubl = "t_tForReportCheckInOutSaldo" + inputParams.User.nzp_user;
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
            string chargeXX = inputParams.Bank.pref + "_charge_" + Year.ToString().Substring(2, 2) +
                DBManager.tableDelimiter + "charge_" + Month.ToString("00");
            string sql;

            try
            {
                sql =
                    " INSERT INTO " + tDubl +
                    " (nzp_kvar, nzp_serv, nzp_supp, total)" +
                    " SELECT nzp_kvar, nzp_serv, nzp_supp, count(nzp_charge)" +
                    " FROM " + chargeXX +
                    " WHERE dat_charge is null " + //" and nzp_serv > 1" +
                    " GROUP BY nzp_kvar, nzp_serv, nzp_supp" +
                    " HAVING count(nzp_charge) > 1";
                ExecSQL(sql);

                sql =
                    " INSERT INTO " + tResult +
                    " (nzp_group, nzp)" +
                    " SELECT DISTINCT  " + (int)ECheckGroupId.DoubleChargeTrio + ", nzp_kvar" +
                    " FROM " + tDubl;
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
            InsertIntoCheckChMon((int)ECheckGroupId.DoubleChargeTrio, ret.result && count == 0, tResult);

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
                " CREATE TEMP TABLE " + tDubl +
                " (nzp_kvar INTEGER," +
                " nzp_serv INTEGER," +
                " nzp_supp INTEGER," +
                " total INTEGER)" +
                DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        private void DropTempTable()
        {
            string sql = " DROP TABLE " + tResult;
            ExecSQL(sql, false);

            sql = " DROP TABLE " + tDubl;
            ExecSQL(sql, false);
        }

        /// <summary>
        /// Создание отчета по выполненной проверке
        /// </summary>
        public override void CreateCheckBeforeClosingReport()
        {
            var report3 = new Report_CheckChargeTrioUniq(Connection, inputParams);
            report3.GetReport();
        }


    }
}

