
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
    public class CheckDublValPU : CheckBeforeClosing
    {
        //таблицы для формирования групп link_group
        private string tResultPU;
        private string tDublPU;
        private string tResultODPU;
        private string tDublODPU;
        private string tResultGrPU;
        private string tDublGrPU;

        private string tCountersForReport;

        public CheckDublValPU(CheckBeforeClosingParams Params) :
            base(Params)
        {
            IsCritical = false;
            Month = Points.GetCalcMonth(new CalcMonthParams { pref = inputParams.Bank.pref }).month_;
            Year = Points.GetCalcMonth(new CalcMonthParams { pref = inputParams.Bank.pref }).year_;

            tResultPU = "t_tResultPUCheckDublValPU" + inputParams.User.nzp_user;
            tDublPU = "t_tDublPUCheckDublValPU" + inputParams.User.nzp_user;
            tResultODPU = "t_tResultODPUCheckDublValPU" + inputParams.User.nzp_user;
            tDublODPU = "t_tDublODPUCheckDublValPU" + inputParams.User.nzp_user;
            tResultGrPU = "t_tResultGrPUCheckDublValPU" + inputParams.User.nzp_user;
            tDublGrPU = "t_tDublGrPUCheckDublValPU" + inputParams.User.nzp_user;
            tCountersForReport = "t_tCounForRepCheckDublValPU" + inputParams.User.nzp_user;
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
            string counters = inputParams.Bank.pref + DBManager.sDataAliasRest + "counters";
            string counters_dom = inputParams.Bank.pref + DBManager.sDataAliasRest + "counters_dom";
            string counters_group = inputParams.Bank.pref + DBManager.sDataAliasRest + "counters_group";
            string sql;

            try
            {

                InsertDublRows(tDublPU, counters, "nzp_kvar");
                InsertDublRows(tDublODPU, counters_dom, "nzp_dom");
                InsertDublRows(tDublGrPU, counters_group, "1");
           
                sql =
                    " INSERT INTO " + tResultPU +
                    " (nzp_group, nzp)" +
                    " SELECT DISTINCT  " + (int)ECheckGroupId.DoubleValIPU + ", nzp" +
                    " FROM " + tDublPU;
                ExecSQL(sql);

                sql =
                    " INSERT INTO " + tResultODPU +
                    " (nzp_group, nzp)" +
                    " SELECT DISTINCT  " + (int)ECheckGroupId.DoubleValODPU + ", k.nzp_kvar" +
                    " FROM " + tDublODPU + " d," +
                     inputParams.Bank.pref + DBManager.sDataAliasRest + "kvar k" +
                    " WHERE d.nzp = k.nzp_dom";
                ExecSQL(sql);

                sql =
                    " INSERT INTO " + tResultODPU +
                    " (nzp_group, nzp)" +
                    " SELECT DISTINCT  " + (int)ECheckGroupId.DoubleValGrPU + ", k.nzp_kvar" +
                    " FROM " + tDublODPU + " d," +
                     inputParams.Bank.pref + DBManager.sDataAliasRest + "counters_link k" +
                    " WHERE d.nzp = k.nzp_counter";
                ExecSQL(sql);

            }
            catch (Exception ex)
            {
                ret.result = false;
                MonitorLog.WriteLog(ret.text + " " + ex.Message + " " + ex.StackTrace, MonitorLog.typelog.Error, true);
            }

            //Анализ результата
            Returns ret1;
            sql = " SELECT count(*) FROM " + tResultPU;
            int count = ExecScalar(sql).ToInt();
            ret1.result = ret.result && (count == 0);
            InsertIntoCheckChMon((int)ECheckGroupId.DoubleValIPU, ret.result && count == 0, tResultPU);

            Returns ret2;
            sql = " SELECT count(*) FROM " + tResultODPU;
            count = ExecScalar(sql).ToInt();
            ret2.result = ret.result && (count == 0);
            InsertIntoCheckChMon((int)ECheckGroupId.DoubleValODPU, ret.result && count == 0, tResultODPU);

            Returns ret3;
            sql = " SELECT count(*) FROM " + tResultGrPU;
            count = ExecScalar(sql).ToInt();
            ret3.result = ret.result && (count == 0);
            InsertIntoCheckChMon((int)ECheckGroupId.DoubleValGrPU, ret.result && count == 0, tResultGrPU);

            ret.result = ret1.result && ret2.result && ret3.result;

            if (!ret.result)
            {
                //ИПУ
                sql =
                    " INSERT INTO " + tCountersForReport +
                    " ( show_order, pu_type, nzp_kvar, count_num, nzp_counter, dat_uchet)" +
                    " SELECT DISTINCT 1, 'ИПУ', s.nzp, s.count_num, s.nzp_counter, s.dat_uchet " +
                    " FROM " + tDublPU + " s";
                ExecSQL(sql);
                //Групповые
                sql =
                    " INSERT INTO " + tCountersForReport +
                    " ( show_order, pu_type, nzp_kvar, count_num, nzp_counter, dat_uchet)" +
                    " SELECT DISTINCT 2, 'Групповые', k.nzp_kvar, s.count_num, s.nzp_counter, s.dat_uchet " +
                    " FROM " + tDublGrPU + " s," +
                     inputParams.Bank.pref + DBManager.sDataAliasRest + "counters_link k" +
                    " WHERE s.nzp_counter = k.nzp_counter";
                ExecSQL(sql);
                //домовые
                sql =
                    " INSERT INTO " + tCountersForReport +
                    " ( show_order, pu_type, nzp_kvar, count_num, nzp_counter, dat_uchet)" +
                    " SELECT DISTINCT 3, 'ОДПУ', k.nzp_kvar, s.count_num, s.nzp_counter, s.dat_uchet " +
                    " FROM " + tDublODPU + " s," +
                     inputParams.Bank.pref + DBManager.sDataAliasRest + "kvar k" +
                    " WHERE k.nzp_dom = s.nzp";
                ExecSQL(sql);
            }

            return ret;
        }

        private void InsertDublRows(string tDubl, string counters, string field)
        {
            string sql;

            if (counters.Contains("counters_group"))
            {
                sql =
                      " INSERT INTO " + tDubl +
                      " (nzp, nzp_counter, dat_uchet, count_num)" +
                      " SELECT " + field + ", c.nzp_counter, c.dat_uchet, s.num_cnt" +
                      " FROM " + counters + " c," +
                      inputParams.Bank.pref + DBManager.sDataAliasRest + "counters_spis s" +
                      " WHERE s.nzp_counter = c.nzp_counter AND  c.is_actual <> 100 " +
                      " GROUP BY c.nzp_counter, c.dat_uchet, s.num_cnt" +
                      " HAVING count(*) > 1";
                ExecSQL(sql);
            }
            else
            {
                sql =
                      " INSERT INTO " + tDubl +
                      " (nzp, nzp_counter, dat_uchet, count_num)" +
                      " SELECT " + field + ", nzp_counter, dat_uchet, num_cnt" +
                      " FROM " + counters + 
                      " WHERE is_actual <> 100" +
                      " GROUP BY " + field + ", nzp_counter, dat_uchet, num_cnt" +
                      " HAVING count(*) > 1";
                ExecSQL(sql);
            }
        }

        private void CreateTempTable()
        {
            string sql =
                " CREATE TEMP TABLE " + tResultPU +
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
                " CREATE TEMP TABLE " + tResultGrPU +
                " (nzp_group INTEGER," +
                " nzp INTEGER)" +
                DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql =
                " CREATE TEMP TABLE " + tDublPU +
                " (nzp INTEGER," +
                " nzp_counter INTEGER," +
                " count_num CHAR(40), " +
                " dat_uchet DATE)" +
                DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql =
                " CREATE TEMP TABLE " + tDublODPU +
                " (nzp INTEGER," +
                " nzp_counter INTEGER," +
                " count_num CHAR(40), " +
                " dat_uchet DATE)" +
                DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql =
                " CREATE TEMP TABLE " + tDublGrPU +
                " (nzp INTEGER," +
                " nzp_counter INTEGER," +
                " count_num CHAR(40), " +
                " dat_uchet DATE)" +
                DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql =
                " CREATE TEMP TABLE " + tCountersForReport +
                " ( nzp_kvar " + DBManager.sDecimalType + "(13,0)," +
                " pu_type CHAR(20), " +
                " count_num CHAR(40), " +
                " nzp_counter INTEGER," +
                " dat_uchet DATE, " +
                " show_order INTEGER)" +
                DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        private void DropTempTable()
        {
            string sql = " DROP TABLE " + tResultPU;
            ExecSQL(sql, false);

            sql = " DROP TABLE " + tResultODPU;
            ExecSQL(sql, false);

            sql = " DROP TABLE " + tResultGrPU;
            ExecSQL(sql, false);

            sql = " DROP TABLE " + tDublPU;
            ExecSQL(sql, false);

            sql = " DROP TABLE " + tDublODPU;
            ExecSQL(sql, false);

            sql = " DROP TABLE " + tDublGrPU;
            ExecSQL(sql, false);

            sql = " DROP TABLE " + tCountersForReport;
            ExecSQL(sql, false);
        }

        /// <summary>
        /// Создание отчета по выполненной проверке
        /// </summary>
        public override void CreateCheckBeforeClosingReport()
        {
            var report3 = new Report_CheckDublValPU(Connection, inputParams, tCountersForReport);
            report3.GetReport();
        }


    }
}

