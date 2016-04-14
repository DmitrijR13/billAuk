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
    /// Проверка правильности распределения оплат
    /// </summary>
    public class CheckPayments : CheckBeforeClosing
    {
        protected int GroupPaymentDistribId { get; set; }
        protected int GroupPaymentInSaldoId { get; set; }
        protected int GroupPaymentPerekidkaId { get; set; }

        private bool isSuccessPaymentDistribCheck;
        private bool isSuccessPaymentInSaldoCheck;
        private bool isSuccessPaymentPerekidkaCheck;
        //таблицы для формирования групп link_group
        private string tResultPaymentDistrib;
        private string tResultPaymentInSaldo;
        private string tResultPaymentPerekidka;

        private string tPDistrib;
        private string tPInSaldo;
        private string tPPerekidka;

        public CheckPayments(CheckBeforeClosingParams Params):
            base(Params)
        {
            GroupPaymentDistribId = (int)ECheckGroupId.RassogPaymentDistrib;
            GroupPaymentInSaldoId = (int) ECheckGroupId.RassoglPaymentInSaldo;
            GroupPaymentPerekidkaId = (int) ECheckGroupId.RassoglPaymentPerekidka;
            IsCritical = true;
            Month = Points.GetCalcMonth(new CalcMonthParams{pref = inputParams.Bank.pref}).month_;
            Year = Points.GetCalcMonth(new CalcMonthParams { pref = inputParams.Bank.pref }).year_;

            tResultPaymentDistrib = "t_1_PaymentDistrib_" + inputParams.User.nzp_user;
            tResultPaymentInSaldo = "t_2_PaymentInSaldo_" + inputParams.User.nzp_user;
            tResultPaymentPerekidka = "t_3_PaymentPerekidka_" + inputParams.User.nzp_user;

            tPDistrib = "t_1_CheckPayments" + inputParams.User.nzp_user;
            tPInSaldo = "t_2_CheckPayments" + inputParams.User.nzp_user;
            tPPerekidka = "t_3_CheckPayments" + inputParams.User.nzp_user;
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
            string fin = Points.Pref + "_fin_" + Year.ToString().Substring(2, 2) + DBManager.tableDelimiter;
            string charge = inputParams.Bank.pref + "_charge_" + Year.ToString().Substring(2, 2) + DBManager.tableDelimiter;
            string sql;

            try
            {
                #region заполняем tPDistrib

                //из pack_ls
                sql =
                    " INSERT INTO " + tPDistrib +
                    " (num_ls, sum_prih)" +
                    " SELECT num_ls, sum(g_sum_ls)" +
                    " FROM " + fin + "pack_ls" +
                    " WHERE dat_uchet >= '01." + Month.ToString("00") + "." + Year + "'" +
                    " AND dat_uchet < '01." + nextMonth.ToString("00") + "." + nextYear + "'" +
                    " AND num_ls IN" +
                    "  (SELECT num_ls FROM " + inputParams.Bank.pref + DBManager.sDataAliasRest + "kvar)" +
                    " GROUP BY num_ls";
                ExecSQL(sql);
                //из fn_supplier
                sql =
                    " INSERT INTO " + tPDistrib +
                    " (num_ls, sum_prih)" +
                    " SELECT num_ls, -sum(sum_prih)" +
                    " FROM " + charge + "fn_supplier" + Month.ToString("00") +
                    " GROUP BY num_ls";
                ExecSQL(sql);
                //из from_supplier
                sql =
                    " INSERT INTO " + tPDistrib +
                    " (num_ls, sum_prih)" +
                    " SELECT num_ls, -sum(sum_prih)" +
                    " FROM " + charge + "from_supplier" +
                    " WHERE dat_uchet >= '01." + Month.ToString("00") + "." + Year + "'" +
                    " AND dat_uchet < '01." + nextMonth.ToString("00") + "." + nextYear + "'" +
                    " GROUP BY num_ls";
                ExecSQL(sql);

                #endregion

                #region заполняем tPInSaldo

                //из charge
                sql =
                    " INSERT INTO " + tPInSaldo +
                    " (num_ls, sum_prih)" +
                    " SELECT num_ls, sum(money_to) + sum(money_from)" +
                    " FROM " + charge + "charge_" + Month.ToString("00") +
                    " WHERE NOT (money_to = 0 AND money_from = 0)" +
                    " AND nzp_serv > 1 AND dat_charge IS NULL" +
                    " GROUP BY num_ls";
                ExecSQL(sql);
                //из fn_supplier
                sql =
                    " INSERT INTO " + tPInSaldo +
                    " (num_ls, sum_prih)" +
                    " SELECT num_ls, -sum(sum_prih)" +
                    " FROM " + charge + "fn_supplier" + Month.ToString("00") +
                    " GROUP BY num_ls";
                ExecSQL(sql);
                //из from_supplier
                sql =
                    " INSERT INTO " + tPInSaldo +
                    " (num_ls, sum_prih)" +
                    " SELECT num_ls, -sum(sum_prih)" +
                    " FROM " + charge + "from_supplier" +
                    " WHERE dat_uchet >= '01." + Month.ToString("00") + "." + Year + "'" +
                    " AND dat_uchet < '01." + nextMonth.ToString("00") + "." + nextYear + "'" +
                    " GROUP BY num_ls";
                ExecSQL(sql);

                #endregion

                #region заполняем tPPerekidka

                //из charge
                sql =
                    " INSERT INTO " + tPPerekidka +
                    " (num_ls, sum_prih)" +
                    " SELECT num_ls, sum(money_del) " +
                    " FROM " + charge + "charge_" + Month.ToString("00") +
                    " WHERE money_del > 0" +
                    " AND nzp_serv > 1 AND dat_charge IS NULL" +
                    " GROUP BY num_ls";
                ExecSQL(sql);
                //из del_supplier
                sql =
                    " INSERT INTO " + tPPerekidka +
                    " (num_ls, sum_prih)" +
                    " SELECT num_ls, -sum(sum_prih) as sum_prih" +
                    " FROM " + charge + "del_supplier" +
                    " WHERE dat_account >= '01." + Month.ToString("00") + "." + Year + "'" +
                    " AND dat_account < '01." + nextMonth.ToString("00") + "." + nextYear + "'" +
                    " GROUP BY num_ls ";
                ExecSQL(sql);

                #endregion

                #region Проверка Рассогласование в распределении оплат 

                sql =
                    " INSERT INTO " + tResultPaymentDistrib +
                    " (nzp_group, nzp)" +
                    " SELECT " + GroupPaymentDistribId + ", num_ls" +
                    " FROM " + tPDistrib +
                    " GROUP BY  num_ls " +
                    " HAVING CAST(sum(sum_prih) as " + DBManager.sDecimalType + "(14,2)) <> 0";
                ExecSQL(sql);

                #endregion

                #region Проверка Рассогласование учета оплат в сальдо

                sql =
                    " INSERT INTO " + tResultPaymentInSaldo +
                    " (nzp_group, nzp)" +
                    " SELECT " + GroupPaymentInSaldoId + ", num_ls" +
                    " FROM " + tPInSaldo +
                    " GROUP BY  num_ls " +
                    " HAVING CAST(sum(sum_prih) as " + DBManager.sDecimalType + "(14,2)) <> 0";
                ExecSQL(sql);

                #endregion

                #region проверка Рассогласование в перекидках оплат

                sql =
                    " INSERT INTO " + tResultPaymentPerekidka +
                    " (nzp_group, nzp)" +
                    " SELECT " + GroupPaymentPerekidkaId + ", num_ls" +
                    " FROM " + tPPerekidka +
                    " GROUP BY  num_ls " +
                    " HAVING CAST(sum(sum_prih) as " + DBManager.sDecimalType + "(14,2)) <> 0";
                ExecSQL(sql);

                #endregion

            }
            catch (Exception ex)
            {
                ret.result = false;
                MonitorLog.WriteLog(ret.text + " " + ex.Message + " " + ex.StackTrace, MonitorLog.typelog.Error, true);
            }

            #region Анализ результата

            Returns ret1 = Utils.InitReturns();
            sql = " SELECT count(*) FROM " + tResultPaymentDistrib;
            int countPaymentDistrib = ExecScalar(sql).ToInt();
            ret1.result = ret.result && (countPaymentDistrib == 0);
            InsertIntoCheckChMon(GroupPaymentDistribId, ret1.result, tResultPaymentDistrib);

            Returns ret2 = Utils.InitReturns();
            sql = " SELECT count(*) FROM " + tResultPaymentInSaldo;
            int countPaymentInSaldo = ExecScalar(sql).ToInt();
            ret2.result = ret.result && (countPaymentInSaldo == 0);
            InsertIntoCheckChMon(GroupPaymentInSaldoId, ret2.result, tResultPaymentInSaldo);

            Returns ret3 = Utils.InitReturns();
            sql = " SELECT count(*) FROM " + tResultPaymentPerekidka;
            int countPaymentPerekidka = ExecScalar(sql).ToInt();
            ret3.result = ret.result && (countPaymentPerekidka == 0);
            InsertIntoCheckChMon(GroupPaymentPerekidkaId, ret3.result, tResultPaymentPerekidka);

            ret.result = ret1.result && ret2.result && ret3.result;

            #endregion

            return ret;
        }

        private void CreateTempTable()
        {
            string sql = 
                " CREATE TEMP TABLE " + tPDistrib +
                "( num_ls INTEGER," +
                " sum_prih " + DBManager.sDecimalType +  "(10,2))" + 
                DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql =
                " CREATE TEMP TABLE " + tPInSaldo +
                "( num_ls INTEGER," +
                " sum_prih " + DBManager.sDecimalType + "(10,2))" +
                DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql =
                " CREATE TEMP TABLE " + tPPerekidka +
                "( num_ls INTEGER," +
                " sum_prih " + DBManager.sDecimalType + "(10,2))" +
                DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql =
                " CREATE TEMP TABLE " + tResultPaymentDistrib +
                "( nzp_group INTEGER," +
                " nzp INTEGER)" +
                DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql =
                " CREATE TEMP TABLE " + tResultPaymentInSaldo +
                "( nzp_group INTEGER," +
                " nzp INTEGER)" +
                DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql =
                " CREATE TEMP TABLE " + tResultPaymentPerekidka +
                "( nzp_group INTEGER," +
                " nzp INTEGER)" +
                DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        private void DropTempTable()
        {
            string sql = " DROP TABLE " + tPDistrib;
            ExecSQL(sql, false);

            sql = " DROP TABLE " + tPInSaldo;
            ExecSQL(sql, false);

            sql = " DROP TABLE " + tPPerekidka;
            ExecSQL(sql, false);

            sql = " DROP TABLE " + tResultPaymentDistrib;
            ExecSQL(sql, false);

            sql = " DROP TABLE " + tResultPaymentInSaldo;
            ExecSQL(sql,false);

            sql = " DROP TABLE " + tResultPaymentPerekidka;
            ExecSQL(sql, false);
        }

        /// <summary>
        /// Создание отчета по выполненной проверке
        /// </summary>
        public override void CreateCheckBeforeClosingReport()
        {
            Report_CheckRassoglOplat report3 = new Report_CheckRassoglOplat(Connection, inputParams);
            report3.GetReport();
        }
        
    }
}
