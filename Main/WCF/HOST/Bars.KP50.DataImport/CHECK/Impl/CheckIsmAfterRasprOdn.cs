using System;
using Bars.KP50.DataImport.CHECK.Report;
using Bars.KP50.Utils;
using Globals.SOURCE.GLOBAL;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.DataImport.CHECK.Impl
{
    /// <summary>
    /// Проверка превышения предельных значений показаний ПУ 
    /// </summary>
    public class CheckIsmAfterRasprOdn : CheckBeforeClosing
    {
        //таблицы для формирования групп link_group
        private string tResult;
        private string tForReport;

        private string tPCountersXX;
        private string tPcounter;

        public CheckIsmAfterRasprOdn(CheckBeforeClosingParams Params) :
            base(Params)
        {
            IsCritical = false;
            Month = Points.GetCalcMonth(new CalcMonthParams { pref = inputParams.Bank.pref }).month_;
            Year = Points.GetCalcMonth(new CalcMonthParams { pref = inputParams.Bank.pref }).year_;

            tResult = "t_tResultAfterRasprOdn" + inputParams.User.nzp_user;
            tForReport = "t_tForReportAfterRasprOdn" + inputParams.User.nzp_user;

            tPCountersXX = "t_tAfterRasprOdnCountersXX" + inputParams.User.nzp_user;
            tPcounter = "t_tAfterRasprOdnCounter" + inputParams.User.nzp_user;
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
            string counterXX = inputParams.Bank.pref + "_charge_" + Year.ToString().Substring(2, 2) + DBManager.tableDelimiter +
                "counters_" + Month.ToString("00");
            string kvar_calc = inputParams.Bank.pref + "_charge_" + Year.ToString().Substring(2, 2) + DBManager.tableDelimiter +
                "kvar_calc_" + Month.ToString("00");
            string counters_link = inputParams.Bank.pref + DBManager.sDataAliasRest + "counters_link";
            string sql;

            try
            {
                //распределение ОД
                sql =
                    " INSERT INTO " + tPCountersXX +
                    " (num_ls, nzp_serv, dat_calc)" +
                    " SELECT k.num_ls, c.nzp_serv, kc.dat_calc " +
                    " FROM " + counterXX + " c," +
                    inputParams.Bank.pref + DBManager.sDataAliasRest + "kvar k," +
                    kvar_calc + " kc" +
                    " WHERE c.nzp_type = 1 AND c.kod_info > 0 AND stek = 3 AND c.nzp_dom = k.nzp_dom AND k.nzp_kvar = kc.nzp_kvar";
                ExecSQL(sql);

                //распределение групповых
                sql =
                    " INSERT INTO " + tPCountersXX +
                    " (num_ls, nzp_serv, dat_calc)" +
                    " SELECT kc.num_ls, c.nzp_serv, kc.dat_calc " +
                    " FROM " + counterXX + " c," +
                    counters_link + " l," +
                    kvar_calc + " kc" +
                    " WHERE nzp_type = 2 AND kod_info > 0 AND stek = 3 AND c.nzp_counter = l.nzp_counter AND l.nzp_kvar = kc.nzp_kvar";
                ExecSQL(sql);

                sql = " CREATE INDEX inx_tAfterRasprOdn_1" + inputParams.User.nzp_user + " on " + tPCountersXX + " (num_ls)";
                ExecSQL(sql, false);
                sql = " CREATE INDEX inx_tAfterRasprOdn_2" + inputParams.User.nzp_user + " on " + tPCountersXX + " (num_ls, nzp_serv, dat_calc)";
                ExecSQL(sql, false);
                sql = DBManager.sUpdStat + " " + tPCountersXX;
                ExecSQL(sql, false);

                //показания ИПУ
                sql =
                    " INSERT INTO " + tPcounter +
                    " (num_ls, nzp_serv, dat_when)" +
                    " SELECT c.num_ls, c.nzp_serv, max(c.dat_when) " +
                    " FROM " + inputParams.Bank.pref + DBManager.sDataAliasRest + "counters c," +
                    tPCountersXX + " cxx " +
                    " WHERE c.dat_when >= '" + firstDay + "' AND c.dat_when < '" + nextFirstDay + "'" +
                    " AND c.is_actual <> 100 AND cxx.num_ls = c.num_ls" +
                    " GROUP BY 1,2";
                ExecSQL(sql);

                sql = " CREATE INDEX inx_tAfterRasprOdn_3" + inputParams.User.nzp_user + " on " + tPcounter + " (num_ls, nzp_serv, dat_when)";
                ExecSQL(sql, false);
                sql = DBManager.sUpdStat + " " + tPcounter;
                ExecSQL(sql, false);

                //ищут те, у которых есть показания ИПУ после распредения ОД или группового
                sql =
                    " INSERT INTO " + tResult +
                    " (nzp_group, nzp)" +
                    " SELECT DISTINCT  " + (int)ECheckGroupId.IsmAfterRasprOdn + ", cxx.num_ls" +
                    " FROM " + tPCountersXX + " cxx, " +
                    tPcounter + " c" +
                    " WHERE cxx.num_ls = c.num_ls AND cxx.nzp_serv = c.nzp_serv AND c.dat_when > cxx.dat_calc";
                ExecSQL(sql);

            }
            catch (Exception ex)
            {
                ret.result = false;
                MonitorLog.WriteLog(ret.text + " " + ex.Message + " " + ex.StackTrace, MonitorLog.typelog.Error, true);
            }
            sql = " SELECT count(*) FROM " + tResult;
            int count = ExecScalar(sql).ToInt();
            ret.result = ret.result && (count == 0);
            InsertIntoCheckChMon((int)ECheckGroupId.IsmAfterRasprOdn, ret.result && count == 0, tResult);

            if (!ret.result)
            {
                sql =
                    " INSERT INTO " + tForReport +
                    " (num_ls, nzp_serv)" +
                    " SELECT DISTINCT cxx.num_ls, cxx.nzp_serv " +
                    " FROM " + tPCountersXX + " cxx, " +
                    tPcounter + " c" +
                    " WHERE cxx.num_ls = c.num_ls AND cxx.nzp_serv = c.nzp_serv AND c.dat_when > cxx.dat_calc";
                ExecSQL(sql);
            }

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
                " CREATE TEMP TABLE " + tPCountersXX +
                " (num_ls INTEGER," +
                " nzp_serv INTEGER," +
                " dat_calc DATE)" +
                DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql =
                " CREATE TEMP TABLE " + tPcounter +
                " (num_ls INTEGER," +
                " nzp_serv INTEGER," +
                " dat_when DATE)" +
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
            
            sql = " DROP TABLE " + tPCountersXX;
            ExecSQL(sql, false);

            sql = " DROP TABLE " + tPcounter;
            ExecSQL(sql, false);

            sql = " DROP TABLE " + tForReport;
            ExecSQL(sql, false);
        }

        /// <summary>
        /// Создание отчета по выполненной проверке
        /// </summary>
        public override void CreateCheckBeforeClosingReport()
        {
            Report_CheckIsmAfterRasprOdn report3 = new Report_CheckIsmAfterRasprOdn(Connection, inputParams, tForReport);
            report3.GetReport();
        }


    }
}
