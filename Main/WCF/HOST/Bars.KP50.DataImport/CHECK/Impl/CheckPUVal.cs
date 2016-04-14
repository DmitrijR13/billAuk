using System;
using System.Data;
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
    public class CheckPUVal : CheckBeforeClosing
    {
        protected int GroupIPU { get; set; }
        protected int GroupKvarPU { get; set; }
        protected int GroupGroupPU { get; set; }
        protected int GroupODPU { get; set; }

        private bool isSuccessIPU;
        private bool isSuccessKvarPU;
        private bool isSuccessGroupPU;
        private bool isSuccessODPU;
        //таблицы для формирования групп link_group
        private string tResultIPU;
        private string tResultKvarPU;
        private string tResultGroupPU;
        private string tResultODPU;

        private string tPIPU;
        private string tPGroupPU;
        private string tPODPU;

        private string tCountersSpis;
        private string tLimitValues;
        private string tCounters;
        private string tCountersDom;
        private string tCountersGroup;
        
        private string tCountersForReport;

        private string tPeriodRecalc;
        private string tPeriodRecalcFirstly;

        private string counters;
        private string counters_dom;
        private string counters_group;

        public CheckPUVal(CheckBeforeClosingParams Params) :
            base(Params)
        {
            GroupIPU = (int)ECheckGroupId.TooBigIPUVal;
            GroupKvarPU = (int)ECheckGroupId.TooBigKvarPUVal;
            GroupGroupPU = (int)ECheckGroupId.TooBigGroupVal;
            GroupODPU = (int)ECheckGroupId.TooBigODPUVal;
            IsCritical = false;
            Month =  Points.GetCalcMonth(new CalcMonthParams { pref = inputParams.Bank.pref }).month_;
            Year = Points.GetCalcMonth(new CalcMonthParams { pref = inputParams.Bank.pref }).year_;

            tResultIPU = "t_tResultIPU" + inputParams.User.nzp_user;
            tResultKvarPU = "t_tResultKvarPU" + inputParams.User.nzp_user;
            tResultGroupPU = "t_tResultGroupPU" + inputParams.User.nzp_user;
            tResultODPU = "t_tResultODPU" + inputParams.User.nzp_user;

            tPIPU = "t_tPIPU" + inputParams.User.nzp_user;
            tPGroupPU = "t_tPGroupPU" + inputParams.User.nzp_user;
            tPODPU = "t_tPODPU" + inputParams.User.nzp_user;

            tCountersSpis = "t_tCountersSpis" + inputParams.User.nzp_user;
            tLimitValues = "t_tLimitValues" + inputParams.User.nzp_user;
            tCounters = "t_tCounters" + inputParams.User.nzp_user;
            tCountersDom = "t_tCountersDom" + inputParams.User.nzp_user;
            tCountersGroup = "t_tCountersGroup" + inputParams.User.nzp_user;

            tCountersForReport = "t_tCountersForReport" + inputParams.User.nzp_user;

            tPeriodRecalc = "t_tPeriodRecalc" + inputParams.User.nzp_user;
            tPeriodRecalcFirstly = "t_ttPeriodRecalcFirstly" + inputParams.User.nzp_user;

            counters = "t_tCountersCounters" + inputParams.User.nzp_user;
            counters_dom = "t_tCountersCounters_dom" + inputParams.User.nzp_user;
            counters_group = "t_tCountersCounters_group" + inputParams.User.nzp_user;
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
            string sql;

            try
            {
                #region Проверяем заполненность всех параметров

                sql =
                    " SELECT * FROM  " + inputParams.Bank.pref + DBManager.sDataAliasRest + "prm_10" +
                    " WHERE is_actual<>100" +
                    " AND dat_s <='" + nextFirstDay + "' and dat_po>='" + firstDay + "'" +
                    " AND nzp_prm in(2081,2082,2083,2084,2085,2086,2087,2088,1457)";
                DataTable dt = ExecSQLToTable(sql);
                if (dt.Rows.Count < 9)
                {
                    InsertIntoCheckChMon(GroupIPU, false, tResultIPU);
                    InsertIntoCheckChMon(GroupIPU, false, tResultODPU);
                    InsertIntoCheckChMon(GroupIPU, false, tResultKvarPU);
                    InsertIntoCheckChMon(GroupIPU, false, tResultGroupPU);

                    return new Returns(false);
                }

                #endregion

                #region Получение предельных значений

                sql =
                    " INSERT INTO " + tLimitValues +
                    " (nzp_serv, is_ipu, value)" +
                    " VALUES " +
                    " ( 25, 1," + DBManager.sNvlWord + "((SELECT val_prm " + DBManager.sConvToNum +
                    " FROM " + inputParams.Bank.pref + DBManager.sDataAliasRest + "prm_10" +
                    " WHERE  nzp_prm = 2081 and is_actual<>100" +
                    " AND dat_s <='" + nextFirstDay + "' and dat_po>='" + firstDay + "'),0))";
                ExecSQL(sql);

                sql =
                    " INSERT INTO " + tLimitValues +
                    " (nzp_serv, is_ipu, value)" +
                    " VALUES " +
                    " (9, 1," + DBManager.sNvlWord + "((SELECT  val_prm " + DBManager.sConvToNum +
                    " FROM " + inputParams.Bank.pref + DBManager.sDataAliasRest + "prm_10" +
                    " WHERE  nzp_prm = 2082 and is_actual<>100" +
                    " AND dat_s <='" + nextFirstDay + "' and dat_po>='" + firstDay + "'),0))";
                ExecSQL(sql);

                sql =
                    " INSERT INTO " + tLimitValues +
                    " (nzp_serv, is_ipu, value)" +
                    " VALUES " +
                    " (6, 1," + DBManager.sNvlWord + "((SELECT  val_prm " + DBManager.sConvToNum +
                    " FROM " + inputParams.Bank.pref + DBManager.sDataAliasRest + "prm_10" +
                    " WHERE  nzp_prm = 2083 and is_actual<>100" +
                    " AND dat_s <='" + nextFirstDay + "' and dat_po>='" + firstDay + "'),0))";
                ExecSQL(sql);

                sql =
                    " INSERT INTO " + tLimitValues +
                    " (nzp_serv, is_ipu, value)" +
                    " VALUES " +
                    " (10, 1, " + DBManager.sNvlWord + "((SELECT val_prm " + DBManager.sConvToNum +
                    " FROM " + inputParams.Bank.pref + DBManager.sDataAliasRest + "prm_10" +
                    " WHERE  nzp_prm = 2084 and is_actual<>100" +
                    " AND dat_s <='" + nextFirstDay + "' and dat_po>='" + firstDay + "'),0))";
                ExecSQL(sql);

                sql =
                    " INSERT INTO " + tLimitValues +
                    " (nzp_serv, is_ipu, value)" +
                    " VALUES " +
                    " (25, 0, " + DBManager.sNvlWord + "((SELECT val_prm " + DBManager.sConvToNum +
                    " FROM " + inputParams.Bank.pref + DBManager.sDataAliasRest + "prm_10" +
                    " WHERE  nzp_prm = 2085 and is_actual<>100" +
                    " AND dat_s <='" + nextFirstDay + "' and dat_po>='" + firstDay + "'),0))";
                ExecSQL(sql);

                sql =
                    " INSERT INTO " + tLimitValues +
                    " (nzp_serv, is_ipu, value)" +
                    " VALUES " +
                    " (9, 0, " + DBManager.sNvlWord + "((SELECT val_prm " + DBManager.sConvToNum +
                    " FROM " + inputParams.Bank.pref + DBManager.sDataAliasRest + "prm_10" +
                    " WHERE  nzp_prm = 2086 and is_actual<>100" +
                    " AND dat_s <='" + nextFirstDay + "' and dat_po>='" + firstDay + "'),0))";
                ExecSQL(sql);

                sql =
                    " INSERT INTO " + tLimitValues +
                    " (nzp_serv, is_ipu, value)" +
                    " VALUES " +
                    " (6, 0, " + DBManager.sNvlWord + "((SELECT val_prm " + DBManager.sConvToNum +
                    " FROM " + inputParams.Bank.pref + DBManager.sDataAliasRest + "prm_10" +
                    " WHERE  nzp_prm = 2087 and is_actual<>100" +
                    " AND dat_s <='" + nextFirstDay + "' and dat_po>='" + firstDay + "'),0))";
                ExecSQL(sql);

                sql =
                    " INSERT INTO " + tLimitValues +
                    " (nzp_serv, is_ipu, value)" +
                    " VALUES " +
                    " (10, 0, " + DBManager.sNvlWord + "((SELECT val_prm " + DBManager.sConvToNum +
                    " FROM " + inputParams.Bank.pref + DBManager.sDataAliasRest + "prm_10" +
                    " WHERE  nzp_prm = 2088 and is_actual<>100" +
                    " AND dat_s <='" + nextFirstDay + "' and dat_po>='" + firstDay + "'),0))";
                ExecSQL(sql);

                sql =
                    " INSERT INTO " + tLimitValues +
                    " (value)" +
                    " VALUES " +
                    " (" + DBManager.sNvlWord + "((SELECT val_prm " + DBManager.sConvToNum +
                    " FROM " + inputParams.Bank.pref + DBManager.sDataAliasRest + "prm_10" +
                    " WHERE  nzp_prm = 1457 and is_actual<>100" +
                    " AND dat_s <='" + nextFirstDay + "' and dat_po>='" + firstDay + "'),0))";
                ExecSQL(sql);

                #endregion

                #region находим периоды перерасчетов, для этих ЛС период проверки будет шире
                //ИПУ
                sql =
                    " INSERT INTO " + tPeriodRecalcFirstly +
                    " (nzp_counter, date_s)" +
                    " SELECT DISTINCT c.nzp_counter, max(m.dat_s)" +
                    " FROM " + inputParams.Bank.pref + DBManager.sDataAliasRest + ".counters_spis c, " +
                    inputParams.Bank.pref + DBManager.sDataAliasRest + ".must_calc m" +
                    " WHERE year_ = " + Year + " AND month_ = " + Month + " AND m.nzp_kvar = c.nzp " +
                    " GROUP BY c.nzp_counter";
                ExecSQL(sql);
                //ОДПУ
                sql =
                    " INSERT INTO " + tPeriodRecalcFirstly +
                    " (nzp_counter, date_s)" +
                    " SELECT DISTINCT c.nzp_counter, max(m.dat_s)" +
                    " FROM " + inputParams.Bank.pref + DBManager.sDataAliasRest + ".counters_spis c, " +
                    inputParams.Bank.pref + DBManager.sDataAliasRest + ".must_calc m," +
                    inputParams.Bank.pref + DBManager.sDataAliasRest + ".kvar k" +
                    " WHERE year_ = " + Year + " AND month_ = " + Month + 
                    " AND m.nzp_kvar = k.nzp_kvar AND c.nzp = k.nzp_dom " +
                    " GROUP BY c.nzp_counter";
                ExecSQL(sql);
                //Групповые
                sql =
                    " INSERT INTO " + tPeriodRecalcFirstly +
                    " (nzp_counter, date_s)" +
                    " SELECT DISTINCT c.nzp_counter, max(m.dat_s)" +
                    " FROM " + inputParams.Bank.pref + DBManager.sDataAliasRest + ".counters_spis c, " +
                    inputParams.Bank.pref + DBManager.sDataAliasRest + ".must_calc m," +
                    inputParams.Bank.pref + DBManager.sDataAliasRest + ".counters_link l" +
                    " WHERE year_ = " + Year + " AND month_ = " + Month +
                    " AND m.nzp_kvar = l.nzp_kvar AND c.nzp_counter = l.nzp_counter " +
                    " GROUP BY c.nzp_counter";
                ExecSQL(sql);

                sql =
                    " INSERT INTO " + tPeriodRecalc +
                    " (nzp_counter, date_s)" +
                    " SELECT DISTINCT nzp_counter, date_s FROM " + tPeriodRecalcFirstly;
                ExecSQL(sql);

                sql = " CREATE INDEX inx_ttPeriodRecalc_1" + inputParams.User.nzp_user + " on " + tPeriodRecalc + " (nzp_counter)";
                ExecSQL(sql, false);
                sql = DBManager.sUpdStat + " " + tCountersSpis;
                ExecSQL(sql, false);

                #endregion

                #region Выборка показаний за последние 2 года

                sql =
                    " INSERT INTO " + counters +
                    " (nzp_counter, val_cnt, dat_uchet, nzp_serv)" +
                    " SELECT nzp_counter, val_cnt, dat_uchet, nzp_serv" +
                    " FROM " + inputParams.Bank.pref + DBManager.sDataAliasRest + "counters" +
                    " WHERE is_actual <> 100 " +
                    " AND dat_uchet > '" + "01." + Month.ToString("00") + "." + (Year-2) + "'";
                ExecSQL(sql);

                sql = " CREATE INDEX inx_ttCounters_19" + inputParams.User.nzp_user + " on " + counters + " (nzp_counter)";
                ExecSQL(sql, false);
                sql = " CREATE INDEX inx_ttCounters_29" + inputParams.User.nzp_user + " on " + counters + " (nzp_counter, dat_uchet)";
                ExecSQL(sql, false);
                sql = DBManager.sUpdStat + " " + counters;
                ExecSQL(sql, false);

                sql =
                    " INSERT INTO " + counters_dom +
                    " (nzp_counter, val_cnt, dat_uchet, nzp_serv)" +
                    " SELECT nzp_counter, val_cnt, dat_uchet, nzp_serv" +
                    " FROM " + inputParams.Bank.pref + DBManager.sDataAliasRest + "counters_dom" +
                    " WHERE is_actual <> 100 " +
                    " AND dat_uchet > '" + "01." + Month.ToString("00") + "." + (Year - 2) + "'";
                ExecSQL(sql);

                sql = " CREATE INDEX inx_ttCountersdom_19" + inputParams.User.nzp_user + " on " + counters_dom + " (nzp_counter)";
                ExecSQL(sql, false);
                sql = " CREATE INDEX inx_ttCountersdom_29" + inputParams.User.nzp_user + " on " + counters_dom + " (nzp_counter, dat_uchet)";
                ExecSQL(sql, false);
                sql = DBManager.sUpdStat + " " + counters_dom;
                ExecSQL(sql, false);

                sql =
                    " INSERT INTO " + counters_group +
                    " (nzp_counter, val_cnt, dat_uchet)" +
                    " SELECT nzp_counter, val_cnt, dat_uchet" +
                    " FROM " + inputParams.Bank.pref + DBManager.sDataAliasRest + "counters_group" +
                    " WHERE is_actual <> 100 " +
                    " AND dat_uchet > '" + "01." + Month.ToString("00") + "." + (Year - 2) + "'";
                ExecSQL(sql);

                sql = " CREATE INDEX inx_ttCountersgr_19" + inputParams.User.nzp_user + " on " + counters_group + " (nzp_counter)";
                ExecSQL(sql, false);
                sql = " CREATE INDEX inx_ttCountersgr_29" + inputParams.User.nzp_user + " on " + counters_group + " (nzp_counter, dat_uchet)";
                ExecSQL(sql, false);
                sql = DBManager.sUpdStat + " " + counters_group;
                ExecSQL(sql, false);
                #endregion

                #region Получение данных о счетчиках

                sql =
                    " INSERT INTO " + tCountersSpis +
                    " (nzp_kvar, nzp_counter, num_cnt, nzp_type, cnt_stage, mmnog, nzp_serv)" +
                    " SELECT c.nzp, c.nzp_counter, c.num_cnt, c.nzp_type, t.cnt_stage, t.mmnog, c.nzp_serv" +
                    " FROM " + inputParams.Bank.pref + DBManager.sDataAliasRest + "counters_spis c," +
                    inputParams.Bank.pref + DBManager.sKernelAliasRest + "s_counttypes t" +
                    " WHERE c.nzp_cnttype = t.nzp_cnttype" +
                    " AND is_actual <> 100";
                ExecSQL(sql);

                sql = " CREATE INDEX inx_ttCounters_1" + inputParams.User.nzp_user + " on " + tCountersSpis + " (nzp_counter)";
                ExecSQL(sql, false);
                sql = DBManager.sUpdStat + " " + tCountersSpis;
                ExecSQL(sql, false);

                #endregion

                #region tCounters

                //ближайшее показание до месяца проверки
                sql =
                    " INSERT INTO " + tCounters +
                    " (nzp_counter,  dat_uchet, nzp_serv)" +
                    " SELECT DISTINCT nzp_counter, MAX(dat_uchet) as dat_uchet, nzp_serv" +
                    " FROM " + counters +
                    " WHERE dat_uchet < " +
                    DBManager.sNvlWord + "((SELECT MAX(p.date_s) FROM " + tPeriodRecalc + " p" +
                    " WHERE p.nzp_counter = " + counters + ".nzp_counter),'" + firstDay + "') " +
                    " GROUP BY nzp_counter, nzp_serv";
                ExecSQL(sql);

                //ближайшее показание после месяца проверки
                sql =
                    " INSERT INTO " + tCounters +
                    " (nzp_counter, dat_uchet, nzp_serv)" +
                    " SELECT DISTINCT nzp_counter, MIN(dat_uchet) as dat_uchet, nzp_serv" +
                    " FROM " + inputParams.Bank.pref + DBManager.sDataAliasRest + "counters" +
                    " WHERE dat_uchet >= '" + nextFirstDay + "' " +
                    " GROUP BY nzp_counter, nzp_serv";
                ExecSQL(sql);
                
                //показания вставлем 
                sql =
                    " UPDATE " + tCounters +
                    " SET val_cnt = " +
                    " (SELECT max(c.val_cnt) FROM " + counters + " c" +
                    " WHERE c.nzp_counter = " + tCounters + ".nzp_counter AND c.dat_uchet = " + tCounters + ".dat_uchet)";
                ExecSQL(sql);

                //показания внутри месяца проверки
                sql =
                    " INSERT INTO " + tCounters +
                    " (nzp_counter, val_cnt, dat_uchet, nzp_serv)" +
                    " SELECT DISTINCT nzp_counter, val_cnt, dat_uchet, nzp_serv" +
                    " FROM " + counters +
                    " WHERE dat_uchet >= " +
                    DBManager.sNvlWord + "((SELECT MAX(p.date_s) FROM " + tPeriodRecalc + " p" +
                    " WHERE p.nzp_counter = " + counters + ".nzp_counter),'" + firstDay + "')  " +
                    " AND dat_uchet < '" + nextFirstDay + "'";
                ExecSQL(sql);

                sql = " CREATE INDEX inx_ttCounters_2" + inputParams.User.nzp_user + " on " + tCounters + " (nzp_counter, dat_uchet)";
                ExecSQL(sql, false);
                sql = DBManager.sUpdStat + " " + tCounters;
                ExecSQL(sql, false);

                #endregion

                #region tCountersDom

                //ближайшее показание до месяца проверки
                sql =
                    " INSERT INTO " + tCountersDom +
                    " (nzp_counter,  dat_uchet, nzp_serv)" +
                    " SELECT DISTINCT nzp_counter, MAX(dat_uchet) as dat_uchet, nzp_serv" +
                    " FROM " + counters_dom +
                    " WHERE dat_uchet < " +
                    DBManager.sNvlWord + "((SELECT MAX(p.date_s) FROM " + tPeriodRecalc + " p" +
                    " WHERE p.nzp_counter = " + counters_dom + ".nzp_counter),'" + firstDay + "')  " +
                    " GROUP BY nzp_counter, nzp_serv";
                ExecSQL(sql);

                //ближайшее показание после месяца проверки
                sql =
                    " INSERT INTO " + tCountersDom +
                    " (nzp_counter, dat_uchet, nzp_serv)" +
                    " SELECT DISTINCT nzp_counter, MIN(dat_uchet) as dat_uchet, nzp_serv" +
                    " FROM " + counters_dom +
                    " WHERE dat_uchet >= '" + nextFirstDay + "' " +
                    " GROUP BY nzp_counter, nzp_serv";
                ExecSQL(sql);

                //показания вставлем 
                sql =
                    " UPDATE " + tCountersDom +
                    " SET val_cnt = " +
                    " (SELECT max(c.val_cnt) FROM " + counters_dom + " c" +
                    " WHERE c.nzp_counter = " + tCountersDom + ".nzp_counter AND c.dat_uchet = " + tCountersDom +".dat_uchet)";
                ExecSQL(sql);

                //показания внутри месяца проверки
                sql =
                    " INSERT INTO " + tCountersDom +
                    " (nzp_counter, val_cnt, dat_uchet, nzp_serv)" +
                    " SELECT DISTINCT nzp_counter, val_cnt, dat_uchet, nzp_serv" +
                    " FROM " + counters_dom +
                    " WHERE dat_uchet >= " +
                    DBManager.sNvlWord + "((SELECT MAX(p.date_s) FROM " + tPeriodRecalc + " p" +
                    " WHERE p.nzp_counter = " + counters_dom + ".nzp_counter),'" + firstDay + "')  " +
                    " AND dat_uchet < '" + nextFirstDay + "'";
                ExecSQL(sql);

                sql = " CREATE INDEX inx_ttCounters_3" + inputParams.User.nzp_user + " on " + tCountersDom + " (nzp_counter, dat_uchet)";
                ExecSQL(sql, false);
                sql = DBManager.sUpdStat + " " + tCountersDom;
                ExecSQL(sql, false);

                #endregion

                #region tCountersGroup

                //ближайшее показание до месяца проверки
                sql =
                    " INSERT INTO " + tCountersGroup +
                    " (nzp_counter,  dat_uchet)" +
                    " SELECT DISTINCT nzp_counter, MAX(dat_uchet) as dat_uchet" +
                    " FROM " + counters_group +
                    " WHERE dat_uchet < " +
                    DBManager.sNvlWord + "((SELECT MAX(p.date_s) FROM " + tPeriodRecalc + " p" +
                    " WHERE p.nzp_counter = " + counters_group + ".nzp_counter),'" + firstDay + "')  " +
                    " GROUP BY nzp_counter";
                ExecSQL(sql);

                //ближайшее показание после месяца проверки
                sql =
                    " INSERT INTO " + tCountersGroup +
                    " (nzp_counter, dat_uchet)" +
                    " SELECT DISTINCT nzp_counter, MIN(dat_uchet) as dat_uchet" +
                    " FROM " + counters_group +
                    " WHERE dat_uchet >= '" + nextFirstDay + "' " +
                    " GROUP BY nzp_counter";
                ExecSQL(sql);

                //показания вставлем 
                sql =
                    " UPDATE " + tCountersGroup +
                    " SET val_cnt = " +
                    " (SELECT max(c.val_cnt) FROM " + counters_group  + " c" +
                    " WHERE c.nzp_counter = " + tCountersGroup + ".nzp_counter AND c.dat_uchet = " + tCountersGroup +".dat_uchet)";
                ExecSQL(sql);

                //показания внутри месяца проверки
                sql =
                    " INSERT INTO " + tCountersGroup +
                    " (nzp_counter, val_cnt, dat_uchet)" +
                    " SELECT DISTINCT nzp_counter, val_cnt, dat_uchet" +
                    " FROM " + counters_group +
                    " WHERE dat_uchet >= " +
                    DBManager.sNvlWord + "((SELECT MAX(p.date_s) FROM " + tPeriodRecalc + " p" +
                    " WHERE p.nzp_counter = " + counters_group + ".nzp_counter),'" + firstDay + "')  " +
                    " AND dat_uchet < '" + nextFirstDay + "'";
                ExecSQL(sql);

                //код услуги
                sql =
                    " UPDATE " + tCountersGroup +
                    " SET nzp_serv = " +
                    " (SELECT max(c.nzp_serv) FROM " + tCountersSpis + " c" +
                    " WHERE c.nzp_counter = " + tCountersGroup + ".nzp_counter)";
                ExecSQL(sql);

                sql = " CREATE INDEX inx_ttCounters_5" + inputParams.User.nzp_user + " on " + tCountersGroup + " (nzp_counter, dat_uchet)";
                ExecSQL(sql, false);
                sql = DBManager.sUpdStat + " " + tCountersGroup;
                ExecSQL(sql, false);

                #endregion

                #region Получение значений счетчиков за этот месяц

                sql =
                    " INSERT INTO " + tPIPU +
                    " (nzp_counter, val_cnt, dat_uchet, nzp_serv)" +
                    " SELECT nzp_counter, val_cnt, dat_uchet, nzp_serv" +
                    " FROM " + tCounters +
                    " WHERE dat_uchet >= " +
                    DBManager.sNvlWord + "((SELECT MAX(p.date_s) FROM " + tPeriodRecalc + " p" +
                    " WHERE p.nzp_counter = " + tCounters + ".nzp_counter),'" + firstDay + "')  ";
                ExecSQL(sql);


                sql =
                    " INSERT INTO " + tPODPU +
                    " (nzp_counter, val_cnt, dat_uchet, nzp_serv)" +
                    " SELECT nzp_counter, val_cnt, dat_uchet, nzp_serv" +
                    " FROM " + tCountersDom +
                    " WHERE dat_uchet >= " +
                    DBManager.sNvlWord + "((SELECT MAX(p.date_s) FROM " + tPeriodRecalc + " p" +
                    " WHERE p.nzp_counter = " + tCountersDom + ".nzp_counter),'" + firstDay + "') ";
                ExecSQL(sql);


                sql =
                    " INSERT INTO " + tPGroupPU +
                    " (nzp_counter, val_cnt, dat_uchet, nzp_serv)" +
                    " SELECT nzp_counter, val_cnt, dat_uchet, nzp_serv" +
                    " FROM " + tCountersGroup +
                    " WHERE dat_uchet >= " +
                    DBManager.sNvlWord + "((SELECT MAX(p.date_s) FROM " + tPeriodRecalc + " p" +
                    " WHERE p.nzp_counter = " + tCountersGroup + ".nzp_counter),'" + firstDay + "') ";
                ExecSQL(sql);
                
                #endregion

                #region предыдущее значение

                sql =
                    " UPDATE " + tPIPU +
                    " SET p_val_cnt = " +
                    " (SELECT val_cnt " +
                    " FROM " + tCounters +
                    " WHERE dat_uchet < " + tPIPU + ".dat_uchet" +
                    " AND nzp_counter = " + tPIPU + ".nzp_counter" +
                    " ORDER BY dat_uchet DESC" +
                    " LIMIT 1 )";
                ExecSQL(sql);

                sql =
                    " UPDATE " + tPODPU +
                    " SET p_val_cnt = " +
                    " (SELECT val_cnt " +
                    " FROM " + tCountersDom +
                    " WHERE dat_uchet < " + tPODPU + ".dat_uchet" +
                    " AND nzp_counter = " + tPODPU + ".nzp_counter" +
                    " ORDER BY dat_uchet DESC" +
                    " LIMIT 1 )";
                ExecSQL(sql);

                sql =
                    " UPDATE " + tPGroupPU +
                    " SET p_val_cnt = " +
                    " (SELECT val_cnt " +
                    " FROM " + tCountersGroup +
                    " WHERE dat_uchet < " + tPGroupPU + ".dat_uchet" +
                    " AND nzp_counter = " + tPGroupPU + ".nzp_counter" +
                    " ORDER BY dat_uchet DESC" +
                    " LIMIT 1 )";
                ExecSQL(sql);

                #endregion

                #region Находим расход

                // IPU -------------------------------------------------------------------------------
                sql =
                    " UPDATE " + tPIPU +
                    " SET limit_val = " +
                    " (SELECT value FROM " + tLimitValues + " WHERE is_ipu = 1 AND " + tPIPU + ".nzp_serv = nzp_serv) ";
                ExecSQL(sql);

                sql =
                    " UPDATE " + tPIPU +
                    " SET limit_val = " +
                    " (SELECT value FROM " + tLimitValues + " WHERE nzp_serv is null)" +
                    " WHERE limit_val IS NULL ";
                ExecSQL(sql);

                sql =
                    " UPDATE " + tPIPU +
                    " SET  diff = " +
                    " (SELECT (" + tPIPU + ".val_cnt - " + tPIPU + ".p_val_cnt)*c.mmnog" +
                    " FROM " + tCountersSpis + " c WHERE c.nzp_counter = " + tPIPU + ".nzp_counter )" +
                    " WHERE val_cnt >= p_val_cnt ";
                ExecSQL(sql);

                sql =
                    " UPDATE " + tPIPU +
                    " SET diff = " +
                    " (SELECT (POWER(10, cnt_stage) - " + tPIPU + ".p_val_cnt + " + tPIPU + ".val_cnt)*c.mmnog" +
                    " FROM " + tCountersSpis + " c WHERE c.nzp_counter = " + tPIPU + ".nzp_counter )" +
                    " WHERE val_cnt < p_val_cnt ";
                ExecSQL(sql);

                // ODPU --------------------------------------------------------------------------------
                sql =
                    " UPDATE " + tPODPU +
                    " SET limit_val = " +
                    " (SELECT value FROM " + tLimitValues + " WHERE is_ipu = 0 AND " + tPODPU + ".nzp_serv = nzp_serv) ";
                ExecSQL(sql);

                sql =
                    " UPDATE " + tPODPU +
                    " SET limit_val = " +
                    " (SELECT value FROM " + tLimitValues + " WHERE nzp_serv is null)" +
                    " WHERE limit_val IS NULL ";
                ExecSQL(sql);

                sql =
                    " UPDATE " + tPODPU +
                    " SET  diff = " +
                    " (SELECT (" + tPODPU + ".val_cnt - " + tPODPU + ".p_val_cnt)*c.mmnog" +
                    " FROM " + tCountersSpis + " c WHERE c.nzp_counter = " + tPODPU + ".nzp_counter )" +
                    " WHERE val_cnt >= p_val_cnt ";
                ExecSQL(sql);

                sql =
                    " UPDATE " + tPODPU +
                    " SET diff = " +
                    " (SELECT (POWER(10, cnt_stage) - " + tPODPU + ".p_val_cnt + " + tPODPU + ".val_cnt)*c.mmnog" +
                    " FROM " + tCountersSpis + " c WHERE c.nzp_counter = " + tPODPU + ".nzp_counter )" +
                    " WHERE val_cnt < p_val_cnt ";
                ExecSQL(sql);

                // GroupPU -------------------------------------------------------------------------------
                sql =
                    " UPDATE " + tPGroupPU +
                    " SET limit_val = " +
                    " (SELECT value FROM " + tLimitValues + " WHERE is_ipu = 0 AND " + tPGroupPU +
                    ".nzp_serv = nzp_serv) " +
                    " WHERE 2 =" +
                    " (SELECT t.nzp_type FROM " + tCountersSpis + " t WHERE t.nzp_counter = " + tPGroupPU +
                    ".nzp_counter) ";
                ExecSQL(sql);

                sql =
                    " UPDATE " + tPGroupPU +
                    " SET limit_val = " +
                    " (SELECT value FROM " + tLimitValues + " WHERE nzp_serv is null)" +
                    " WHERE limit_val IS NULL ";
                ExecSQL(sql);

                sql =
                    " UPDATE " + tPGroupPU +
                    " SET limit_val = " +
                    " (SELECT value FROM " + tLimitValues + " WHERE is_ipu = 1 AND " + tPGroupPU +
                    ".nzp_serv = nzp_serv) " +
                    " WHERE limit_val IS NULL ";
                ExecSQL(sql);

                sql =
                    " UPDATE " + tPGroupPU +
                    " SET  diff = " +
                    " (SELECT (" + tPGroupPU + ".val_cnt - " + tPGroupPU + ".p_val_cnt)*c.mmnog" +
                    " FROM " + tCountersSpis + " c WHERE c.nzp_counter = " + tPGroupPU + ".nzp_counter )" +
                    " WHERE val_cnt >= p_val_cnt ";
                ExecSQL(sql);

                sql =
                    " UPDATE " + tPGroupPU +
                    " SET diff = " +
                    " (SELECT (POWER(10, cnt_stage) - " + tPGroupPU + ".p_val_cnt + " + tPGroupPU + ".val_cnt)*c.mmnog" +
                    " FROM " + tCountersSpis + " c WHERE c.nzp_counter = " + tPGroupPU + ".nzp_counter )" +
                    " WHERE val_cnt < p_val_cnt ";
                ExecSQL(sql);

                #endregion

                #region выбираем те показания, которые превосходят

                sql =
                    " INSERT INTO " + tResultIPU +
                    " (nzp_group, nzp)" +
                    " SELECT DISTINCT " + GroupIPU + ", nzp_kvar " +
                    " FROM " + tCountersSpis + "" +
                    " WHERE EXISTS" +
                    " (SELECT 1 FROM " + tPIPU + " p " +
                    " WHERE p.nzp_counter = " + tCountersSpis + ".nzp_counter AND diff > limit_val)";
                ExecSQL(sql);

                sql =
                    " INSERT INTO " + tResultODPU +
                    " (nzp_group, nzp)" +
                    " SELECT DISTINCT " + GroupODPU + ", k.nzp_kvar " +
                    " FROM " + tCountersSpis + " ," +
                     inputParams.Bank.pref + DBManager.sDataAliasRest + "kvar k" +
                    " WHERE " + tCountersSpis + ".nzp_kvar = k.nzp_dom AND EXISTS" +
                    " (SELECT 1 FROM " + tPODPU + " p " +
                    " WHERE p.nzp_counter = " + tCountersSpis + ".nzp_counter AND diff > limit_val)";
                ExecSQL(sql);
                
                sql =
                    " INSERT INTO " + tResultKvarPU +
                    " (nzp_group, nzp)" +
                    " SELECT DISTINCT " + GroupKvarPU + ", k.nzp_kvar " +
                    " FROM " + tCountersSpis + "," +
                     inputParams.Bank.pref + DBManager.sDataAliasRest + "counters_link k" +
                    " WHERE " + tCountersSpis + ".nzp_counter = k.nzp_counter AND EXISTS" +
                    " (SELECT 1 FROM " + tPGroupPU + " p " +
                    " WHERE p.nzp_counter = " + tCountersSpis + ".nzp_counter AND diff > limit_val) " +
                    " AND nzp_type = 4";
                ExecSQL(sql);

                sql =
                    " INSERT INTO " + tResultGroupPU +
                    " (nzp_group, nzp)" +
                    " SELECT DISTINCT " + GroupGroupPU + ", k.nzp_kvar " +
                    " FROM " + tCountersSpis + "," +
                     inputParams.Bank.pref + DBManager.sDataAliasRest + "counters_link k" +
                    " WHERE " + tCountersSpis + ".nzp_counter = k.nzp_counter AND EXISTS" +
                    " (SELECT 1 FROM " + tPGroupPU + " p " +
                    " WHERE p.nzp_counter = " + tCountersSpis + ".nzp_counter AND diff > limit_val) " +
                    " AND nzp_type = 2";
                ExecSQL(sql);


                #endregion

            }
            catch (Exception ex)
            {
                ret.result = false;
                MonitorLog.WriteLog(ret.text + " " + ex.Message + " " + ex.StackTrace, MonitorLog.typelog.Error, true);
            }

            #region Анализ результатов

            Returns ret1 = STCLINE.KP50.Global.Utils.InitReturns();

            sql = " SELECT count(*) FROM " + tResultIPU;
            int countIPU = ExecScalar(sql).ToInt();
            ret1.result = ret.result && (countIPU == 0);
            InsertIntoCheckChMon(GroupIPU, ret1.result && countIPU == 0, tResultIPU);

            sql = " SELECT count(*) FROM " + tResultODPU;
            int countODPU = ExecScalar(sql).ToInt();
            ret1.result = ret.result && (countODPU == 0);
            InsertIntoCheckChMon(GroupODPU, ret1.result && countODPU == 0, tResultODPU);

            sql = " SELECT count(*) FROM " + tResultKvarPU;
            int countKvar = ExecScalar(sql).ToInt();
            ret1.result = ret.result && (countKvar == 0);
            InsertIntoCheckChMon(GroupKvarPU, ret1.result && countKvar == 0, tResultKvarPU);

            sql = " SELECT count(*) FROM " + tResultGroupPU;
            int countGroup = ExecScalar(sql).ToInt();
            ret1.result = ret.result && (countGroup == 0);
            InsertIntoCheckChMon(GroupGroupPU, ret1.result && countGroup == 0, tResultGroupPU);
            #endregion

            #region для отчета

            ret.result = ret.result && (countIPU + countODPU + countKvar + countGroup) == 0;
            if (!ret.result)
            {
                //ИПУ
                sql =
                    " INSERT INTO " + tCountersForReport +
                    " ( show_order, pu_type, nzp_kvar, count_num, nzp_counter, nzp_serv,  count_val_s," +
                    " dat_po, count_val_po, limit_val, rashod)" +
                    " SELECT DISTINCT 1, 'ИПУ', s.nzp_kvar, s.num_cnt, s.nzp_counter, p.nzp_serv, p.p_val_cnt, " +
                    " p.dat_uchet, p.val_cnt, p.limit_val, p.diff " +
                    " FROM " + tCountersSpis + " s," +
                    " " + tPIPU + " p " +
                    " WHERE p.nzp_counter = s.nzp_counter AND p.diff > p.limit_val";
                ExecSQL(sql);
                //Квартирные
                sql =
                    " INSERT INTO " + tCountersForReport +
                    " ( show_order, pu_type, nzp_kvar, count_num, nzp_counter, nzp_serv, count_val_s," +
                    " dat_po, count_val_po, limit_val, rashod)" +
                    " SELECT DISTINCT 2, 'Квартирные', k.nzp_kvar, s.num_cnt, s.nzp_counter, p.nzp_serv, p.p_val_cnt, " +
                    " p.dat_uchet, p.val_cnt, p.limit_val, p.diff " +
                    " FROM " + tCountersSpis + " s," +
                    " " + tPGroupPU + " p " + "," +
                     inputParams.Bank.pref + DBManager.sDataAliasRest + "counters_link k" +
                    " WHERE p.nzp_counter = s.nzp_counter AND p.diff > p.limit_val" +
                    " AND s.nzp_type = 4 AND s.nzp_counter = k.nzp_counter";
                ExecSQL(sql);
                //Групповые
                sql =
                    " INSERT INTO " + tCountersForReport +
                    " ( show_order, pu_type, nzp_kvar, count_num, nzp_counter, nzp_serv, count_val_s," +
                    " dat_po, count_val_po, limit_val, rashod)" +
                    " SELECT DISTINCT 3, 'Групповые', k.nzp_kvar, s.num_cnt, s.nzp_counter, p.nzp_serv, p.p_val_cnt, " +
                    " p.dat_uchet, p.val_cnt, p.limit_val, p.diff " +
                    " FROM " + tCountersSpis + " s," +
                    " " + tPGroupPU + " p " + "," +
                     inputParams.Bank.pref + DBManager.sDataAliasRest + "counters_link k" +
                    " WHERE p.nzp_counter = s.nzp_counter AND p.diff > p.limit_val" +
                    " AND s.nzp_type = 2 AND s.nzp_counter = k.nzp_counter";
                ExecSQL(sql);

                //ОДПУ
                sql =
                    " INSERT INTO " + tCountersForReport +
                    " ( show_order, pu_type, nzp_kvar, count_num, nzp_counter, nzp_serv, count_val_s," +
                    " dat_po, count_val_po, limit_val, rashod)" +
                    " SELECT DISTINCT 4,  'ОДПУ', k.nzp_kvar, s.num_cnt, s.nzp_counter, p.nzp_serv, p.p_val_cnt, " +
                    " p.dat_uchet, p.val_cnt, p.limit_val, p.diff " +
                    " FROM " + tCountersSpis + " s," +
                    " " + tPODPU + " p " + " ," +
                     inputParams.Bank.pref + DBManager.sDataAliasRest + "kvar k" +
                    " WHERE p.nzp_counter = s.nzp_counter AND p.diff > p.limit_val" +
                    " AND k.nzp_dom = s.nzp_kvar";
                ExecSQL(sql);

                //дата предыдущего показания
                sql =
                    " UPDATE " + tCountersForReport +
                    " SET  dat_s =" +
                    " (SELECT dat_uchet " +
                    " FROM " + tCounters +
                    " WHERE dat_uchet < " + tCountersForReport + ".dat_po" +
                    " AND nzp_counter = " + tCountersForReport + ".nzp_counter" +
                    " ORDER BY dat_uchet DESC" +
                    " LIMIT 1 )" +
                    " WHERE dat_s IS NULL";
                ExecSQL(sql);

                sql =
                    " UPDATE " + tCountersForReport +
                    " SET  dat_s =" +
                    " (SELECT dat_uchet " +
                    " FROM " + tCountersDom +
                    " WHERE dat_uchet < " + tCountersForReport + ".dat_po" +
                    " AND nzp_counter = " + tCountersForReport + ".nzp_counter" +
                    " ORDER BY dat_uchet DESC" +
                    " LIMIT 1 )" +
                    " WHERE dat_s IS NULL";
                ExecSQL(sql);

                sql =
                    " UPDATE " + tCountersForReport +
                    " SET  dat_s =" +
                    " (SELECT dat_uchet " +
                    " FROM " + tCountersGroup +
                    " WHERE dat_uchet < " + tCountersForReport + ".dat_po" +
                    " AND nzp_counter = " + tCountersForReport + ".nzp_counter" +
                    " ORDER BY dat_uchet DESC" +
                    " LIMIT 1 )" +
                    " WHERE dat_s IS NULL";
                ExecSQL(sql);

            }

            #endregion

            return ret;
        }

        private void CreateTempTable()
        {
            #region для отбора информации
            string sql =
                " CREATE TEMP TABLE " + tPIPU +
                " (nzp_counter INTEGER," +
                " dat_uchet DATE," +
                " nzp_serv INTEGER," +
                " val_cnt " + DBManager.sDecimalType + "(14,2)," +
                " p_val_cnt " + DBManager.sDecimalType + "(14,2)," +
                " diff " + DBManager.sDecimalType + "(14,2)," +
                " limit_val " + DBManager.sDecimalType + "(14,2))" +
                DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql =
                " CREATE TEMP TABLE " + tPODPU +
                " (nzp_counter INTEGER," +
                " dat_uchet DATE," +
                " nzp_serv INTEGER," +
                " val_cnt " + DBManager.sDecimalType + "(14,2)," +
                " p_val_cnt " + DBManager.sDecimalType + "(14,2)," +
                " diff " + DBManager.sDecimalType + "(14,2)," +
                " limit_val " + DBManager.sDecimalType + "(14,2))" +
                DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql =
                " CREATE TEMP TABLE " + tPGroupPU +
                " (nzp_counter INTEGER," +
                " dat_uchet DATE," +
                " nzp_serv INTEGER," +
                " val_cnt " + DBManager.sDecimalType + "(14,2)," +
                " p_val_cnt " + DBManager.sDecimalType + "(14,2)," +
                " diff " + DBManager.sDecimalType + "(14,2)," +
                " limit_val " + DBManager.sDecimalType + "(14,2)," +
                " nzp_type INTEGER)" +
                DBManager.sUnlogTempTable;
            ExecSQL(sql);
            #endregion

            #region для результата
            sql =
                " CREATE TEMP TABLE " + tResultIPU +
                " (nzp_group INTEGER," +
                " nzp INTEGER)" +
                DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql =
                " CREATE TEMP TABLE " + tResultODPU +
                " (nzp_group INTEGER," +
                " nzp INTEGER)" +
                DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql =
                " CREATE TEMP TABLE " + tResultKvarPU +
                " (nzp_group INTEGER," +
                " nzp INTEGER)" +
                DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql =
                " CREATE TEMP TABLE " + tResultGroupPU +
                " (nzp_group INTEGER," +
                " nzp INTEGER)" +
                DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql =
                " CREATE TEMP TABLE " + tCountersForReport +
                " ( nzp_kvar " + DBManager.sDecimalType + "(13,0)," +
                " pu_type CHAR(20), " +
                " count_num CHAR(20), " +
                " nzp_counter INTEGER," +
                " nzp_serv INTEGER, " +
                " show_order INTEGER, " +
                " dat_s DATE," +
                " count_val_s " + DBManager.sDecimalType + "(14,2)," +
                " dat_po DATE," +
                " count_val_po " + DBManager.sDecimalType + "(14,2)," +
                " limit_val " + DBManager.sDecimalType + "(14,2)," +
                " rashod " + DBManager.sDecimalType + "(14,2))" +
                DBManager.sUnlogTempTable;
            ExecSQL(sql);
            #endregion

            #region счетчики и показания
            sql =
                " CREATE TEMP TABLE " + tCountersSpis +
                " (nzp_kvar INTEGER," +
                " nzp_counter INTEGER," +
                " num_cnt CHAR(40)," +
                " nzp_type INTEGER," +
                " nzp_serv INTEGER," +
                " cnt_stage INTEGER," +
                " mmnog INTEGER)" +
                DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql =
                " CREATE TEMP TABLE " + tLimitValues +
                " (nzp_serv INTEGER," +
                " is_ipu INTEGER," +
                " value " + DBManager.sDecimalType + "(10,2))" +
                DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql =
                " CREATE TEMP TABLE " + tCounters +
                " (nzp_counter INTEGER," +
                " val_cnt " + DBManager.sDecimalType + "(14,2)," +
                " dat_uchet DATE," +
                " nzp_serv INTEGER)" +
                DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql =
                " CREATE TEMP TABLE " + tCountersDom +
                " (nzp_counter INTEGER," +
                " val_cnt " + DBManager.sDecimalType + "(14,2)," +
                " dat_uchet DATE," +
                " nzp_serv INTEGER)" +
                DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql =
                " CREATE TEMP TABLE " + tCountersGroup +
                " (nzp_counter INTEGER," +
                " val_cnt " + DBManager.sDecimalType + "(14,2)," +
                " dat_uchet DATE," +
                " nzp_serv INTEGER)" +
                DBManager.sUnlogTempTable;
            ExecSQL(sql);
            #endregion

            #region Выборка показаний за последние 2 года
            sql =
                " CREATE TEMP TABLE " + counters +
                " (nzp_counter INTEGER," +
                " val_cnt " + DBManager.sDecimalType + "(14,2)," +
                " dat_uchet DATE," +
                " nzp_serv INTEGER)" +
                DBManager.sUnlogTempTable;
            ExecSQL(sql);


            sql =
                " CREATE TEMP TABLE " + counters_dom +
                " (nzp_counter INTEGER," +
                " val_cnt " + DBManager.sDecimalType + "(14,2)," +
                " dat_uchet DATE," +
                " nzp_serv INTEGER)" +
                DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql =
                " CREATE TEMP TABLE " + counters_group +
                " (nzp_counter INTEGER," +
                " val_cnt " + DBManager.sDecimalType + "(14,2)," +
                " dat_uchet DATE," +
                " nzp_serv INTEGER)" +
                DBManager.sUnlogTempTable;
            ExecSQL(sql);
            #endregion

            #region выбор периодов перерасчета
            sql =
                " CREATE TEMP TABLE " + tPeriodRecalcFirstly +
                " (nzp_counter INTEGER," +
                " date_s DATE)" +
                DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql =
                " CREATE TEMP TABLE " + tPeriodRecalc +
                " (nzp_counter INTEGER," +
                " date_s DATE)" +
                DBManager.sUnlogTempTable;
            ExecSQL(sql);
            #endregion
        }

        private void DropTempTable()
        {
            string sql = " DROP TABLE " + tPIPU;
            ExecSQL(sql, false);
            
            sql = " DROP TABLE " + tPODPU;
            ExecSQL(sql, false);

            sql = " DROP TABLE " + tPGroupPU;
            ExecSQL(sql, false);

            sql = " DROP TABLE " + tCountersSpis;
            ExecSQL(sql, false);

            sql = " DROP TABLE " + tCounters;
            ExecSQL(sql, false);

            sql = " DROP TABLE " + tCountersDom;
            ExecSQL(sql, false);

            sql = " DROP TABLE " + tCountersGroup;
            ExecSQL(sql, false);

            sql = " DROP TABLE " + tLimitValues;
            ExecSQL(sql, false);

            sql = " DROP TABLE " + tResultIPU;
            ExecSQL(sql, false);

            sql = " DROP TABLE " + tResultODPU;
            ExecSQL(sql, false);

            sql = " DROP TABLE " + tResultGroupPU;
            ExecSQL(sql, false);

            sql = " DROP TABLE " + tResultKvarPU;
            ExecSQL(sql, false);

            sql = " DROP TABLE " + tCountersForReport;
            ExecSQL(sql, false);

            sql = " DROP TABLE " + counters;
            ExecSQL(sql, false);

            sql = " DROP TABLE " + counters_dom;
            ExecSQL(sql, false);

            sql = " DROP TABLE " + counters_group;
            ExecSQL(sql, false);

            sql = " DROP TABLE " + tPeriodRecalc;
            ExecSQL(sql, false);

            sql = " DROP TABLE " + tPeriodRecalcFirstly;
            ExecSQL(sql, false);
        }

        /// <summary>
        /// Создание отчета по выполненной проверке
        /// </summary>
        public override void CreateCheckBeforeClosingReport()
        {
            Report_CheckTooBigPuVal report3 = new Report_CheckTooBigPuVal(Connection, inputParams, tCountersForReport);
            report3.GetReport();
        }


    }
}
