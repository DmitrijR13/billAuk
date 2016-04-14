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
    public class CheckValPuWithoutPu : CheckBeforeClosing
    {
        private string tResultIPU;
        private string tResultODPU;
        private string tResultGroupPU;
        public CheckValPuWithoutPu(CheckBeforeClosingParams Params) :
            base(Params)
        {
            IsCritical = false;
            Month = Points.GetCalcMonth(new CalcMonthParams { pref = inputParams.Bank.pref }).month_;
            Year = Points.GetCalcMonth(new CalcMonthParams { pref = inputParams.Bank.pref }).year_;

            tResultIPU = "t_tResIPUCheckValPuWithoutPu" + inputParams.User.nzp_user;
            tResultODPU = "t_tResODPUCheckValPuWithoutPu" + inputParams.User.nzp_user;
            tResultGroupPU = "t_tResGPUCheckValPuWithoutPu" + inputParams.User.nzp_user;
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

            string counters_spis = inputParams.Bank.pref + DBManager.sDataAliasRest + "counters_spis";
            string counters = inputParams.Bank.pref + DBManager.sDataAliasRest + "counters";
            string counters_dom = inputParams.Bank.pref + DBManager.sDataAliasRest + "counters_dom";
            string counters_group = inputParams.Bank.pref + DBManager.sDataAliasRest + "counters_group";

            string sql;

            try
            {
                sql = 
                    " INSERT INTO " + tResultIPU + 
                    " (nzp_group, nzp)" +
                    " SELECT DISTINCT " + (int)ECheckGroupId.ValIPUWithoutPU + ", " + DBManager.sNvlWord + "(nzp_kvar,0)" +
                    " FROM " + counters +  
                    " WHERE is_actual <> 100 AND NOT EXISTS" +
                    " (SELECT 1 FROM " + counters_spis + " cs" +
                    " WHERE cs.nzp_counter = " + counters + ".nzp_counter AND cs.nzp_type = 3) ";
                ExecSQL(sql);

                sql =
                    " INSERT INTO " + tResultODPU +
                    " (nzp_group, nzp)" +
                    " SELECT DISTINCT " + (int)ECheckGroupId.ValODPUWithoutODPU + ", " + DBManager.sNvlWord + "(k.nzp_kvar,0)" +
                    " FROM " + counters_dom + "" +
                    " LEFT OUTER JOIN " + inputParams.Bank.pref + DBManager.sDataAliasRest + "kvar k" +
                    " ON k.nzp_dom = " + counters_dom + ".nzp_dom" +
                    " WHERE is_actual <> 100 AND NOT EXISTS " +
                    " (SELECT 1 FROM " + counters_spis + " cs" +
                    " WHERE cs.nzp_counter = " + counters_dom + ".nzp_counter AND cs.nzp_type = 1) ";
                ExecSQL(sql);

                sql =
                    " INSERT INTO " + tResultGroupPU +
                    " (nzp_group, nzp)" +
                    " SELECT DISTINCT " + (int)ECheckGroupId.ValGrPUWithoutGrPU + ", " + DBManager.sNvlWord + "(l.nzp_kvar,0)" +
                    " FROM " + counters_group + 
                    " LEFT OUTER JOIN " + inputParams.Bank.pref + DBManager.sDataAliasRest + "counters_link l" +
                    " ON l.nzp_counter = " + counters_group + ".nzp_counter" +
                    " WHERE is_actual <> 100 AND NOT EXISTS" +
                    " (SELECT 1 FROM " + counters_spis + " cs" +
                    " WHERE cs.nzp_counter = " + counters_group + ".nzp_counter AND cs.nzp_type in (2,4)) ";
                ExecSQL(sql);
            }
            catch (Exception ex)
            {
                ret.result = false;
                MonitorLog.WriteLog(ret.text + " " + ex.Message + " " + ex.StackTrace, MonitorLog.typelog.Error, true);
            }

            //Анализ результата
            Returns ret1;
            sql = " SELECT count(*) FROM " + tResultIPU;
            int count = ExecScalar(sql).ToInt();
            ret1.result = ret.result && (count == 0);
            InsertIntoCheckChMon((int)ECheckGroupId.ValIPUWithoutPU, ret.result && (count == 0), tResultIPU);

            Returns ret2;
            sql = " SELECT count(*) FROM " + tResultODPU;
            count = ExecScalar(sql).ToInt();
            ret2.result = ret.result && (count == 0);
            InsertIntoCheckChMon((int)ECheckGroupId.ValODPUWithoutODPU, ret.result && (count == 0), tResultODPU);

            Returns ret3;
            sql = " SELECT count(*) FROM " + tResultGroupPU;
            count = ExecScalar(sql).ToInt();
            ret3.result = ret.result && (count == 0);
            InsertIntoCheckChMon((int)ECheckGroupId.ValGrPUWithoutGrPU, ret.result && (count == 0), tResultGroupPU);

            ret.result = ret1.result && ret2.result && ret3.result;
            return ret;
        }

        private void CreateTempTable()
        {
            string sql =
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
                  " CREATE TEMP TABLE " + tResultGroupPU +
                  " (nzp_group INTEGER," +
                  " nzp INTEGER)" +
                  DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        private void DropTempTable()
        {
            string sql = " DROP TABLE " + tResultIPU;
            ExecSQL(sql, false);

            sql = " DROP TABLE " + tResultODPU;
            ExecSQL(sql, false);

            sql = " DROP TABLE " + tResultGroupPU;
            ExecSQL(sql, false);
        }

        public override void CreateCheckBeforeClosingReport()
        {
            Report_CheckValPuWithoutPu report3 = new Report_CheckValPuWithoutPu(Connection, inputParams);
            report3.GetReport();
        }
    }
}
