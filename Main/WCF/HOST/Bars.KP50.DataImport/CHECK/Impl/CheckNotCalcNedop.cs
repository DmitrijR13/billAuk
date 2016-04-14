using System;
using System.Data;
using Bars.KP50.DataImport.CHECK;
using Bars.KP50.DataImport.CHECK.Report;
using Bars.KP50.Utils;
using Globals.SOURCE.GLOBAL;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.IFMX.Server.SOURCE.CHECK.Impl
{
    /// <summary>
    /// Проверка превышения предельных значений показаний ПУ 
    /// </summary>
    public class CheckNotCalcNedop : CheckBeforeClosing
    {
        //таблицы для формирования групп link_group
        private string tResultNedop;

        private string tPNedop;
        private string tPCharge;

        public CheckNotCalcNedop(CheckBeforeClosingParams Params) :
            base(Params)
        {
            IsCritical = false;
            Month = Points.GetCalcMonth(new CalcMonthParams { pref = inputParams.Bank.pref }).month_;
            Year = Points.GetCalcMonth(new CalcMonthParams { pref = inputParams.Bank.pref }).year_;

            tResultNedop = "t_tResultNedop" + inputParams.User.nzp_user;

            tPNedop = "t_tPNedop" + inputParams.User.nzp_user;
            tPCharge = "t_tPNedopCharge" + inputParams.User.nzp_user;
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
            Returns ret = Utils.InitReturns();
            int nextMonth = (Month < 12 ? Month + 1 : 1);
            int nextYear = (Month < 12 ? Year : Year + 1);
            string firstDay = "01." + Month.ToString("00") + "." + Year;
            string nextFirstDay = "01." + nextMonth.ToString("00") + "." + nextYear;
            string charge = inputParams.Bank.pref + "_charge_" + Year.ToString().Substring(2, 2) + DBManager.tableDelimiter +
                "charge_" + Month.ToString("00");
            string sql;

            try
            {
                #region выборка данных для анализа

                sql =
                    " INSERT INTO " + tPCharge +
                    " (nzp_kvar, nzp_serv, nzp_supp)" +
                    " SELECT nzp_kvar, nzp_serv, nzp_supp " +
                    " FROM " + charge +
                    " WHERE nzp_serv > 1 AND dat_charge IS NULL AND sum_nedop > 0";
                ExecSQL(sql);


                sql = " CREATE INDEX inx_tNedop_1" + inputParams.User.nzp_user + " on " + tPCharge + " (nzp_kvar, nzp_serv, nzp_supp)";
                ExecSQL(sql, false);
                sql = DBManager.sUpdStat + " " + tPCharge;
                ExecSQL(sql, false);

                sql =
                    " INSERT INTO " + tPNedop +
                    " (nzp_kvar, nzp_serv, nzp_supp)" +
                    " SELECT nzp_kvar, nzp_serv, nzp_supp " +
                    " FROM " + inputParams.Bank.pref + DBManager.sDataAliasRest + "nedop_kvar" +
                    " WHERE dat_s < '" + nextFirstDay + "' AND dat_po > '" + firstDay + "' AND is_actual <> 100";
                ExecSQL(sql);

                sql = " CREATE INDEX inx_tNedop_2" + inputParams.User.nzp_user + " on " + tPNedop + " (nzp_kvar, nzp_serv, nzp_supp)";
                ExecSQL(sql, false);
                sql = DBManager.sUpdStat + " " + tPNedop;
                ExecSQL(sql, false);

                #endregion

                #region Результат

                sql =
                    " INSERT INTO " + tResultNedop +
                    " (nzp_group, nzp)" +
                    " SELECT " + (int) ECheckGroupId.NotCalcNedop + ",nzp_kvar " +
                    " FROM " + tPNedop +
                    " WHERE NOT EXISTS " +
                    " (SELECT 1 FROM " + tPCharge + " c" +
                    "  WHERE c.nzp_kvar = " + tPNedop + ".nzp_kvar" +
                    " AND c.nzp_serv = " + tPNedop + ".nzp_serv " +
                    " AND c.nzp_supp = " + tPNedop + ".nzp_supp )";
                ExecSQL(sql);

                sql =
                    " INSERT INTO " + tResultNedop +
                    " (nzp_group, nzp)" +
                    " SELECT nzp_kvar, " + (int) ECheckGroupId.NotCalcNedop +
                    " FROM " + tPCharge +
                    " WHERE NOT EXISTS " +
                    " (SELECT 1 FROM " + tPNedop + " n" +
                    "  WHERE n.nzp_kvar = " + tPCharge + ".nzp_kvar" +
                    " AND n.nzp_serv = " + tPCharge + ".nzp_serv " +
                    " AND n.nzp_supp = " + tPCharge + ".nzp_supp )";
                ExecSQL(sql);

            }
            catch (Exception ex)
            {
                ret.result = false;
                MonitorLog.WriteLog(ret.text + " " + ex.Message + " " + ex.StackTrace, MonitorLog.typelog.Error, true);
            }
            sql = " SELECT count(*) FROM " + tResultNedop;
            int countNedop = ExecScalar(sql).ToInt();
            ret.result = ret.result && (countNedop == 0);
            InsertIntoCheckChMon((int)ECheckGroupId.NotCalcNedop, ret.result && countNedop == 0, tResultNedop);

            #endregion

            return ret;
        }

        private void CreateTempTable()
        {
            string sql =
                " CREATE TEMP TABLE " + tResultNedop +
                " (nzp_group INTEGER," +
                " nzp INTEGER)" +
                DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql =
                " CREATE TEMP TABLE " + tPNedop +
                " (nzp_kvar INTEGER," +
                " nzp_serv INTEGER," +
                " nzp_supp INTEGER)" +
                DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql =
                " CREATE TEMP TABLE " + tPCharge +
                " (nzp_kvar INTEGER," +
                " nzp_serv INTEGER," +
                " nzp_supp INTEGER)" +
                DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        private void DropTempTable()
        {
            string sql = " DROP TABLE " + tResultNedop;
            ExecSQL(sql, false);

            sql = " DROP TABLE " + tPNedop;
            ExecSQL(sql, false);

            sql = " DROP TABLE " + tPCharge;
            ExecSQL(sql, false);
        }

        /// <summary>
        /// Создание отчета по выполненной проверке
        /// </summary>
        public override void CreateCheckBeforeClosingReport()
        {
            Report_CheckNotCalcNedopost report3 = new Report_CheckNotCalcNedopost(Connection, inputParams);
            report3.GetReport();
        }


    }
}
