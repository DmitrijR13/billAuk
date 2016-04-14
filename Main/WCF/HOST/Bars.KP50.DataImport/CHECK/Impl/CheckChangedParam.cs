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
    public class CheckChangedParam : CheckBeforeClosing
    {
        //таблицы для формирования групп link_group
        private string tResult;

        private string tNzpPrm;
        private string tPrmX;

        public CheckChangedParam(CheckBeforeClosingParams Params) :
            base(Params)
        {
            IsCritical = false;
            Month = Points.GetCalcMonth(new CalcMonthParams { pref = inputParams.Bank.pref }).month_;
            Year = Points.GetCalcMonth(new CalcMonthParams { pref = inputParams.Bank.pref }).year_;

            tResult = "t_tResultChangedParam" + inputParams.User.nzp_user;

            tNzpPrm = "t_tNzpPrmChangedParam" + inputParams.User.nzp_user;
            tPrmX = "t_tPrmXChangedParam" + inputParams.User.nzp_user;
        }

        /// <summary>
        /// Запуск проверки
        /// </summary>
        /// <param name="inputParams"></param>
        public override Returns StartCheck()
        {
            try
            {
                Returns ret = STCLINE.KP50.Global.Utils.InitReturns();
                DropTempTable();
                CreateTempTable();
                ret = Run();
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
            string prm_frm = inputParams.Bank.pref + DBManager.sKernelAliasRest + "prm_frm";
            string prm_1 = inputParams.Bank.pref + DBManager.sDataAliasRest + "prm_1";
            string prm_2 = inputParams.Bank.pref + DBManager.sDataAliasRest + "prm_2";
            string kvar_calc = inputParams.Bank.pref + "_charge_" + Year.ToString().Substring(2, 2) + DBManager.tableDelimiter +
                "kvar_calc_" + Month.ToString("00");
            string perekidka = inputParams.Bank.pref + "_charge_" + Year.ToString().Substring(2, 2) + DBManager.tableDelimiter +
                "perekidka";
            string sql;

            try
            {
                #region находим измененные параметры

                //выбираем те номера параметров, которые влияют на расчет
                sql =
                    " INSERT INTO " + tNzpPrm +
                    " (nzp_prm)" +
                    " SELECT DISTINCT nzp_prm" +
                    " FROM  " + prm_frm +
                    " WHERE nzp_prm > 0 AND trim(operation) = 'FLD' AND EXISTS" +
                    " (SELECT 1 FROM " + Points.Pref + DBManager.sKernelAliasRest + "prm_name p" +
                    " WHERE p.prm_num in (1,2) AND p.nzp_prm = " + prm_frm + ".nzp_prm)";
                ExecSQL(sql);

                //отбираем все измененные параметры из prm_1,prm_2 за этот месяц, которые влияют на расчет
                sql =
                    " INSERT INTO " + tPrmX +
                    " (nzp_kvar, nzp_prm)" +
                    " SELECT nzp, nzp_prm " +
                    " FROM " + prm_1 +
                    " WHERE is_actual <> 100  and dat_when is not null " +
                    " AND EXISTS (SELECT 1 FROM " + kvar_calc + " k " +
                    " WHERE k.nzp_kvar = " + prm_1 + ".nzp AND " + prm_1 + ".dat_when > k.dat_calc)" + 
                    " AND EXISTS" +
                    " (SELECT 1 FROM " + tNzpPrm + " n " +
                    "  WHERE n.nzp_prm = " + prm_1 + ".nzp_prm)";
                ExecSQL(sql);

                sql = 
                    " INSERT INTO " + tPrmX +
                    " (nzp_kvar, nzp_prm)" +
                    " SELECT nzp, nzp_prm " +
                    " FROM " + prm_2 +
                    " WHERE  is_actual <> 100  and dat_when is not null " +
                    " AND EXISTS (SELECT 1 FROM " + kvar_calc + " k " +
                    " WHERE k.nzp_kvar = " + prm_2 + ".nzp AND " + prm_2 + ".dat_when > k.dat_calc)" +
                    " AND EXISTS" +
                    " (SELECT 1 FROM " + tNzpPrm + " n " +
                    "  WHERE n.nzp_prm = " + prm_2 + ".nzp_prm)";
                ExecSQL(sql);


                #endregion

                #region отбираем перекидки, которые были сделаны после расчета ЛС

                sql =
                    " INSERT INTO " + tPrmX +
                    " (nzp_kvar, nzp_prm)" +
                    " SELECT nzp_kvar, -1" +
                    " FROM " + perekidka +
                    " WHERE month_ = " + Month +
                    " AND EXISTS (SELECT 1 FROM " + kvar_calc + " k " +
                    " WHERE k.nzp_kvar = " + perekidka + ".num_ls AND " + perekidka + ".date_rcl > k.dat_calc)";
                ExecSQL(sql);

                #endregion

            sql =
                " INSERT INTO " + tResult +
                " (nzp_group, nzp)" +
                " SELECT DISTINCT " + (int)ECheckGroupId.ChangedParam + ", nzp_kvar" +
                " FROM " + tPrmX;
            ExecSQL(sql);
            }
            catch (Exception ex)
            {
                ret.result = false;
                MonitorLog.WriteLog(ret.text + " " + ex.Message + " " + ex.StackTrace, MonitorLog.typelog.Error, true);
            }

            #region анализ результата

            sql = " SELECT count(*) FROM " + tResult;
            int count = ExecScalar(sql).ToInt();
            ret.result = ret.result && (count == 0);
            InsertIntoCheckChMon((int)ECheckGroupId.ChangedParam, ret.result && count == 0, tResult);
            #endregion

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
                " CREATE TEMP TABLE " + tNzpPrm +
                " (nzp_prm INTEGER)" +
                DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql =
                " CREATE TEMP TABLE " + tPrmX +
                " (nzp_kvar INTEGER," +
                " nzp_prm INTEGER)" +
                DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        private void DropTempTable()
        {
            string sql = " DROP TABLE " + tResult;
            ExecSQL(sql, false);

            sql = " DROP TABLE " + tNzpPrm;
            ExecSQL(sql, false);

            sql = " DROP TABLE " + tPrmX;
            ExecSQL(sql, false);
        }

        /// <summary>
        /// Создание отчета по выполненной проверке
        /// </summary>
        public override void CreateCheckBeforeClosingReport()
        {
            DataTable dt = ExecSQLToTable("SELECT * FROM " + tPrmX);
            Report_CheckChangedParam report3 = new Report_CheckChangedParam(Connection, inputParams, tPrmX);
            report3.GetReport();
        }


    }
}
