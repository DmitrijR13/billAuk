using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.Base.Parameters;
using Bars.KP50.Report.Main.Properties;
using Bars.KP50.Utils;
using Castle.Core.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using Constants = STCLINE.KP50.Global.Constants;

namespace Bars.KP50.Report.Main.Reports
{
    class Report_CheckPayments : BaseSqlReport
    {
        public override string Name
        {
            get { return "Проверка рассогласования оплат"; }
        }

        public override string Description
        {
            get { return "Проверка рассогласования оплат"; }
        }

        public override IList<ReportGroup> ReportGroups
        {
            get
            {
                var result = new List<ReportGroup> { ReportGroup.Finans };
                return result;
            }
        }

        public override bool IsPreview
        {
            get { return false; }
        }

        protected override byte[] Template
        {
            get { return Resources.Report_CheckPayments; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base }; }
        }

        //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        /// <summary>Месяц</summary>
        private int Month { get; set; }

        /// <summary>Год</summary>
        private int Year { get; set; }

        /// <summary>Территория</summary>
        protected int Banks { get; set; }

        /// <summary>Список банков в БД</summary>  
        protected string TerritoryHeader { get; set; }


        private string mess1;
        private string mess2;
        private string mess3;

        
        //для проверки
        private string tResultPaymentDistrib = "t_1_PaymentDistrib";
        private string tResultPaymentInSaldo = "t_2_PaymentInSaldo";
        private string tResultPaymentPerekidka = "t_3_PaymentPerekidka";

        private string tPDistrib = "t_1_CheckPayments";
        private string tPInSaldo = "t_2_CheckPayments";
        private string tPPerekidka = "t_3_CheckPayments";

        //для отчета
        private string tPaymentDistrib = "t_check_payment_distrib";
        private string tPaymentInSaldo = "t_payment_in_saldo";
        private string tPaymentPerekidka = "t_payment_perekidka";
        private string tKvar = "t_kvar_check_rassog_opl";
        private string tPackLs = "t_pack_ls_check_rassog_opl";
        private string tFn = "t_fn_check_rassog_opl";
        private string tFs = "t_fs_check_rassog_opl";
        private string tCharge = "t_charge_check_rassog_opl";


        //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        public override List<UserParam> GetUserParams()
        {
            var curCalcMonthYear = DBManager.GetCurMonthYear();
            return new List<UserParam>
			{
				new MonthParameter {Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["month_"] : DateTime.Today.Month },
				new YearParameter {Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["yearr"] : DateTime.Today.Year },
				new BankParameter(false)
			};
        }

        protected override void PrepareReport(FastReport.Report report)
        {
            var months = new[] {"","Январь","Февраль",
				 "Март","Апрель","Май","Июнь","Июль","Август","Сентябрь",
				 "Октябрь","Ноябрь","Декабрь"};
            report.SetParameterValue("month", months[Month]);
            report.SetParameterValue("year", Year);
            report.SetParameterValue("bank", TerritoryHeader);

            report.SetParameterValue("mess1", mess1);
            report.SetParameterValue("mess2", mess2);
            report.SetParameterValue("mess3", mess3);

        }

        protected override void PrepareParams()
        {
            Month = UserParamValues["Month"].GetValue<int>();
            Year = UserParamValues["Year"].GetValue<int>();
            Banks = UserParamValues["Banks"].GetValue<int>();

        }

        public override DataSet GetData()
        {
            string sql;
            
           string pref = "";

           if (Banks == null || Banks == 0)
           {
               MonitorLog.WriteLog("Отчет 'Проверка рассогласования оплат' - не выбран банк ", MonitorLog.typelog.Error, false);
               return null;
           }

            sql = " SELECT bd_kernel as pref " +
                  " FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point " +
                  " WHERE nzp_wp>1 " + GetWhereWp(string.Empty);
            DataTable prefDt = ExecSQLToTable(sql);
            if (prefDt.Rows.Count == 0)
            {
                MonitorLog.WriteLog("Отчет 'Проверка рассогласования оплат' - не подтянулся префикс ", MonitorLog.typelog.Error, false);
                return null;
            }
            pref = prefDt.Rows[0]["pref"].ToString().Trim();


            Returns ret = new Returns(true);
            int nextMonth = (Month < 12 ? Month + 1 : 1);
            int nextYear = (Month < 12 ? Year : Year + 1);
            string fin = Points.Pref + "_fin_" + Year.ToString().Substring(2, 2) + DBManager.tableDelimiter;
            string charge = pref + "_charge_" + Year.ToString().Substring(2, 2) + DBManager.tableDelimiter;

            #region проверка
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
                    " AND inbasket = 0 " +
                    " AND " + DBManager.sNvlWord + "(alg,'0') <> '0' " + 
                    " AND num_ls IN" +
                    "  (SELECT num_ls FROM " + pref + DBManager.sDataAliasRest + "kvar)" +
                    " GROUP BY num_ls";
                ExecSQL(sql);
                //из fn_supplier
                sql =
                    " INSERT INTO " + tPDistrib +
                    " (num_ls, sum_prih)" +
                    " SELECT num_ls, -sum(sum_prih)" +
                    " FROM " + charge + "fn_supplier" + Month.ToString("00") + " f "+
                    " WHERE f.dat_uchet >= '01." + Month.ToString("00") + "." + Year + "'" +
                    " AND f.dat_uchet < '01." + nextMonth.ToString("00") + "." + nextYear + "'" +
                    " and exists (select 1 from " + fin + "pack_ls pls, " + fin + "pack p where p.nzp_pack = pls.nzp_pack and f.nzp_pack_ls = pls.nzp_pack_ls and p.pack_type = 10) " +
                    " GROUP BY num_ls";
                ExecSQL(sql);
                //из from_supplier
                sql =
                    " INSERT INTO " + tPDistrib +
                    " (num_ls, sum_prih)" +
                    " SELECT num_ls, -sum(sum_prih)" +
                    " FROM " + charge + "from_supplier f " +
                    " WHERE dat_uchet >= '01." + Month.ToString("00") + "." + Year + "'" +
                    " AND dat_uchet < '01." + nextMonth.ToString("00") + "." + nextYear + "'" +
                    " and exists (select 1 from " + fin + "pack_ls pls, " + fin + "pack p where p.nzp_pack = pls.nzp_pack and f.nzp_pack_ls = pls.nzp_pack_ls and p.pack_type = 20) "+
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
                    " SELECT 1, num_ls" +
                    " FROM " + tPDistrib +
                    " GROUP BY  num_ls " +
                    " HAVING CAST(sum(sum_prih) as " + DBManager.sDecimalType + "(14,2)) <> 0";
                ExecSQL(sql);

                #endregion

                #region Проверка Рассогласование учета оплат в сальдо

                sql =
                    " INSERT INTO " + tResultPaymentInSaldo +
                    " (nzp_group, nzp)" +
                    " SELECT 2, num_ls" +
                    " FROM " + tPInSaldo +
                    " GROUP BY  num_ls " +
                    " HAVING CAST(sum(sum_prih) as " + DBManager.sDecimalType + "(14,2)) <> 0";
                ExecSQL(sql);

                #endregion

                #region проверка Рассогласование в перекидках оплат

                sql =
                    " INSERT INTO " + tResultPaymentPerekidka +
                    " (nzp_group, nzp)" +
                    " SELECT 3, num_ls" +
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
            #endregion

            #region Анализ результата

            Returns ret1 = new Returns(true);
            sql = " SELECT count(*) FROM " + tResultPaymentDistrib;
            int countPaymentDistrib = ExecScalar(sql).ToInt();
            ret1.result = ret.result && (countPaymentDistrib == 0);
            if (!ret1.result) mess1 = "Проверка рассогласования в распределении оплат не прошла";
            else mess1 = "Проверка рассогласования в распределении оплат прошла успешно";

            Returns ret2 = new Returns(true);
            sql = " SELECT count(*) FROM " + tResultPaymentInSaldo;
            int countPaymentInSaldo = ExecScalar(sql).ToInt();
            ret2.result = ret.result && (countPaymentInSaldo == 0);
            if (!ret2.result) mess2 = "Проверка рассогласования учета оплат в сальдо не прошла";
            else mess2 = "Проверка рассогласования учета оплат в сальдо прошла успешно";

            Returns ret3 = new Returns(true);
            sql = " SELECT count(*) FROM " + tResultPaymentPerekidka;
            int countPaymentPerekidka = ExecScalar(sql).ToInt();
            ret3.result = ret.result && (countPaymentPerekidka == 0);
            if (!ret3.result) mess3 = "Проверка рассогласования в перекидках оплат не прошла";
            else mess3 = "Проверка рассогласования в перекидках оплат прошла успешно";
            
            #endregion

            #region заполнение временных таблиц

            if (!ret1.result)
            {
                sql =
                    " INSERT INTO " + tKvar +
                    " (num_ls, pkod, adres, kod)" +
                    " SELECT k.num_ls, k.pkod," +
                    " trim(r.rajon)||' ул.'||trim(u.ulica)||' д.'||trim(d.ndom)||'/'||trim(d.nkor)||' кв.'||trim(k.nkvar) as adres," +
                    " t.nzp_group" + //1
                    " FROM " + tResultPaymentDistrib + " t," +
                    pref + DBManager.sDataAliasRest + "dom d," +
                    Points.Pref + DBManager.sDataAliasRest + "s_ulica u, " +
                    Points.Pref + DBManager.sDataAliasRest + "s_rajon r, " +
                    pref + DBManager.sDataAliasRest + "kvar k " +
                    " WHERE k.nzp_kvar = t.nzp" +
                    " AND k.nzp_dom = d.nzp_dom AND d.nzp_ul = u.nzp_ul AND r.nzp_raj = u.nzp_raj";
                ExecSQL(sql);
            }

            if (!ret2.result)
            {
                sql =
                    " INSERT INTO " + tKvar +
                    " (num_ls, pkod, adres, kod)" +
                    " SELECT k.num_ls, k.pkod," +
                    " trim(r.rajon)||' ул.'||trim(u.ulica)||' д.'||trim(d.ndom)||'/'||trim(d.nkor)||' кв.'||trim(k.nkvar) as adres," +
                    " t.nzp_group" + //2
                    " FROM " + tResultPaymentInSaldo + " t," +
                    pref + DBManager.sDataAliasRest + "dom d," +
                    Points.Pref + DBManager.sDataAliasRest + "s_ulica u, " +
                    Points.Pref + DBManager.sDataAliasRest + "s_rajon r, " +
                    pref + DBManager.sDataAliasRest + "kvar k " +
                    " WHERE k.nzp_kvar = t.nzp" +
                    " AND k.nzp_dom = d.nzp_dom AND d.nzp_ul = u.nzp_ul AND r.nzp_raj = u.nzp_raj";
                ExecSQL(sql);
            }

            if (!ret3.result)
            {
                sql =
                    " INSERT INTO " + tKvar +
                    " (num_ls, pkod, adres, kod)" +
                    " SELECT k.num_ls, k.pkod," +
                    " trim(r.rajon)||' ул.'||trim(u.ulica)||' д.'||trim(d.ndom)||'/'||trim(d.nkor)||' кв.'||trim(k.nkvar) as adres," +
                    " t.nzp_group" + //3
                    " FROM " + tResultPaymentPerekidka + " t," +
                    pref + DBManager.sDataAliasRest + "dom d," +
                    Points.Pref + DBManager.sDataAliasRest + "s_ulica u, " +
                    Points.Pref + DBManager.sDataAliasRest + "s_rajon r, " +
                    pref + DBManager.sDataAliasRest + "kvar k " +
                    " WHERE k.nzp_kvar = t.nzp" +
                    " AND k.nzp_dom = d.nzp_dom AND d.nzp_ul = u.nzp_ul AND r.nzp_raj = u.nzp_raj";
                ExecSQL(sql);
            }

            sql =
                " INSERT INTO " + tPackLs +
                " (num_ls, data_oplata, kvit_oplata, sum_prih, kod_sum, nzp_pack_ls)" +
                " SELECT num_ls, dat_uchet, info_num, g_sum_ls, kod_sum, nzp_pack_ls" +
                " FROM " + fin + "pack_ls" +
                " WHERE dat_uchet >= '01." + Month.ToString("00") + "." + Year + "'" +
                " AND dat_uchet < '01." + nextMonth.ToString("00") + "." + nextYear + "'" +
                " AND num_ls IN" +
                " (SELECT num_ls FROM " + tKvar + ")";
            ExecSQL(sql);

            sql =
                " INSERT INTO " + tFn +
                " (num_ls, sum_prih, nzp_pack_ls, dat_uchet)" +
                " SELECT num_ls, sum(sum_prih), nzp_pack_ls, dat_uchet" +
                " FROM " + charge + "fn_supplier" + Month.ToString("00") + "" +
                " WHERE num_ls IN" +
                " (SELECT num_ls FROM " + tKvar + ")" +
                " GROUP BY num_ls, nzp_pack_ls, dat_uchet";
            ExecSQL(sql);

            sql =
                " INSERT INTO " + tFs +
                " (num_ls, sum_prih, nzp_pack_ls, dat_uchet)" +
                " SELECT num_ls, sum(sum_prih), nzp_pack_ls, dat_uchet" +
                " FROM " + charge + "from_supplier fs" +
                " WHERE dat_uchet >= '01." + Month.ToString("00") + "." + Year + "'" +
                " AND dat_uchet < '01." + nextMonth.ToString("00") + "." + nextYear + "'" +
                " AND num_ls IN" +
                " (SELECT num_ls FROM " + tKvar + ")" +
                " GROUP BY num_ls, nzp_pack_ls, dat_uchet";
            ExecSQL(sql);

            sql =
                " INSERT INTO " + tCharge +
                " (num_ls, money_to, money_from, money_del)" +
                " SELECT num_ls, sum(money_to), sum(money_from), sum(money_del)" +
                " FROM " + charge + "charge_" + Month.ToString("00") +
                " WHERE num_ls IN" +
                " (SELECT num_ls FROM " + tKvar + ")" +
                " AND nzp_serv > 1 AND dat_charge IS NULL" +
                " GROUP BY num_ls";
            ExecSQL(sql);
            #endregion

            if (!ret1.result)
            {
                #region Отчет Рассогласование в распределении оплат

                sql =
                    " INSERT INTO " + tPaymentDistrib +
                    " (num_ls, pkod, adres,  data_oplata, kvit, sum_post_oplat, sum_uchten_oplat, difference, type_name)" +
                    " SELECT k.num_ls, k.pkod, k.adres, p.data_oplata, p.kvit_oplata, " +
                    " " + DBManager.sNvlWord + "(p.sum_prih,0), " + DBManager.sNvlWord + "(fn.sum_prih,0), " +
                    " " + DBManager.sNvlWord + "(p.sum_prih,0) - " + DBManager.sNvlWord + "(fn.sum_prih,0)," +
                    " 'Рассогласование в распределении оплат на счета агента'" +
                    " FROM " + tKvar + " k" +
                    " LEFT OUTER JOIN " + tPackLs + " p ON k.num_ls = p.num_ls AND p.kod_sum in" +
                    "    (SELECT kod FROM " + Points.Pref + DBManager.sKernelAliasRest + "kodsum WHERE is_erc = 1)" +
                    " LEFT OUTER JOIN " + tFn + " fn ON fn.num_ls = k.num_ls AND fn.nzp_pack_ls = p.nzp_pack_ls" +
                    " WHERE k.kod = 1" +
                    " AND CAST(" + DBManager.sNvlWord + "(p.sum_prih,0) - " + DBManager.sNvlWord + "(fn.sum_prih,0)" +
                    " as " + DBManager.sDecimalType + "(14,2)) <> 0";
                ExecSQL(sql);

                sql =
                    " INSERT INTO " + tPaymentDistrib +
                    " (num_ls, pkod, adres,  data_oplata, kvit, sum_post_oplat, sum_uchten_oplat, difference, nzp_pack_ls, type_name)" +
                    " SELECT k.num_ls, k.pkod, k.adres, fn.dat_uchet, '', 0," +
                    " " + DBManager.sNvlWord + "(fn.sum_prih,0), " +
                    " -" + DBManager.sNvlWord + "(fn.sum_prih,0), fn.nzp_pack_ls," +
                    " 'Рассогласование в распределении оплат на счета агента'" +
                    " FROM " + tKvar + " k" +
                    " LEFT OUTER JOIN " + tFn + " fn ON fn.num_ls = k.num_ls AND NOT EXISTS" +
                    "    (SELECT 1 FROM " + tPackLs + " p where p.nzp_pack_ls = fn.nzp_pack_ls AND p.kod_sum in" +
                    "    (SELECT kod FROM " + Points.Pref + DBManager.sKernelAliasRest + "kodsum WHERE is_erc = 1)) " +
                    " WHERE k.kod = 1 " + 
                    " AND CAST(" + DBManager.sNvlWord + "(fn.sum_prih,0) as " + DBManager.sDecimalType + "(14,2)) <> 0";
                ExecSQL(sql);


                sql =
                    " INSERT INTO " + tPaymentDistrib +
                    " (num_ls, pkod, adres,  data_oplata, kvit, sum_post_oplat, sum_uchten_oplat, difference, type_name)" +
                    " SELECT k.num_ls, k.pkod, k.adres, p.data_oplata, p.kvit_oplata, " +
                    " " + DBManager.sNvlWord + "(p.sum_prih,0), " + DBManager.sNvlWord + "(fs.sum_prih,0)," +
                    " " + DBManager.sNvlWord + "(p.sum_prih,0) - " + DBManager.sNvlWord + "(fs.sum_prih,0)," +
                    " 'Рассогласование в распределении оплат от УК и ПУ'" +
                    " FROM " + tKvar + " k" +
                    " LEFT OUTER JOIN " + tPackLs + " p ON k.num_ls = p.num_ls AND p.kod_sum in" +
                    "    (SELECT kod FROM " + Points.Pref + DBManager.sKernelAliasRest + "kodsum WHERE is_erc = 0)" +
                    " LEFT OUTER JOIN " + tFs + " fs ON fs.num_ls = k.num_ls AND fs.nzp_pack_ls = p.nzp_pack_ls" +
                    " WHERE k.kod = 1 " + 
                    " AND CAST(" + DBManager.sNvlWord + "(p.sum_prih,0) - " + DBManager.sNvlWord + "(fs.sum_prih,0)" +
                    " as " + DBManager.sDecimalType + "(14,2)) <> 0";
                ExecSQL(sql);

                sql =
                    " INSERT INTO " + tPaymentDistrib +
                    " (num_ls, pkod, adres,  data_oplata, kvit, sum_post_oplat, sum_uchten_oplat, difference, nzp_pack_ls, type_name)" +
                    " SELECT k.num_ls, k.pkod, k.adres, fs.dat_uchet, '', 0," +
                    " " + DBManager.sNvlWord + "(fs.sum_prih,0), " +
                    " -" + DBManager.sNvlWord + "(fs.sum_prih,0), fs.nzp_pack_ls," +
                    " 'Рассогласование в распределении оплат от УК и ПУ'" +
                    " FROM " + tKvar + " k" +
                    " LEFT OUTER JOIN " + tFs + " fs ON fs.num_ls = k.num_ls AND NOT EXISTS" +
                    "    (SELECT 1 FROM " + tPackLs + " p where p.nzp_pack_ls = fs.nzp_pack_ls AND p.kod_sum in" +
                    "    (SELECT kod FROM " + Points.Pref + DBManager.sKernelAliasRest + "kodsum WHERE is_erc = 0)) " +
                    " WHERE k.kod = 1 " + 
                    " AND CAST(" + DBManager.sNvlWord + "(fs.sum_prih,0) as " + DBManager.sDecimalType + "(14,2)) <> 0";
                ExecSQL(sql);


                sql =
                    " UPDATE " + tPaymentDistrib +
                    " SET kvit = " +
                    " (SELECT p.kvit_oplata FROM " + tPackLs + " p WHERE nzp_pack_ls = " + tPaymentDistrib + ".nzp_pack_ls) " +
                    " WHERE " + DBManager.sNvlWord + "(kvit, '') = '' AND nzp_pack_ls is not null ";
                ExecSQL(sql);

                #endregion
            }

            if (!ret2.result)
            {
                #region Отчет Рассогласование учета оплат в сальдо

                sql =
                    " INSERT INTO " + tPaymentInSaldo + " (num_ls, pkod, adres,  " +
                    " sum_oplata_agent, sum_oplata_uk, sum_post_oplat," +
                    " including_from_agent, including_from_uk, sum_uchten_oplat," +
                    " difference_from_agent, difference_from_uk, difference)" +
                    " SELECT k.num_ls, k.pkod, k.adres, " +
                    " " + DBManager.sNvlWord + "(sum(fn.sum_prih),0), " + DBManager.sNvlWord + "(sum(fs.sum_prih),0)," +
                    " " + DBManager.sNvlWord + "(sum(fn.sum_prih),0) + " + DBManager.sNvlWord + "(sum(fs.sum_prih),0)," +
                    " " + DBManager.sNvlWord + "(c.money_to,0), " + DBManager.sNvlWord + "(c.money_from,0)," +
                    " " + DBManager.sNvlWord + "(c.money_to,0) + " + DBManager.sNvlWord + "(c.money_from,0)," +
                    " " + DBManager.sNvlWord + "(sum(fn.sum_prih),0) - " + DBManager.sNvlWord + "(c.money_to,0)," +
                    " " + DBManager.sNvlWord + "(sum(fs.sum_prih),0) - " + DBManager.sNvlWord + "(c.money_from,0)," +
                    " (" + DBManager.sNvlWord + "(sum(fn.sum_prih),0) + " + DBManager.sNvlWord +
                    "(sum(fs.sum_prih),0)) - " +
                    " (" + DBManager.sNvlWord + "(c.money_to,0) + " + DBManager.sNvlWord + "(c.money_from,0))" +
                    " FROM " + tKvar + " k" +
                    " LEFT OUTER JOIN " + tFn + " fn ON fn.num_ls = k.num_ls" +
                    " LEFT OUTER JOIN " + tFs + " fs ON fs.num_ls = k.num_ls" +
                    " LEFT OUTER JOIN " + tCharge + " c ON k.num_ls = c.num_ls" +
                    " WHERE k.kod = 2 " + 
                    " GROUP BY k.num_ls, k.pkod, k.adres, c.money_to, c.money_from";
                ExecSQL(sql);

                #endregion
            }

            if (!ret3.result)
            {
                #region Отчет Рассогласование в перекидках оплат

                //в charge сумма не собвпадает с del_supplier
                sql =
                    " INSERT INTO " + tPaymentPerekidka + " (num_ls, pkod, adres,  " +
                    " sum_oplata_agent, " +
                    " sum_uchten_oplat, difference)" +
                    " SELECT DISTINCT k.num_ls, k.pkod, k.adres," +
                    " " + DBManager.sNvlWord + "(sum(ds.sum_prih),0),  c.money_del," +
                    " CAST(" + DBManager.sNvlWord + "(sum(ds.sum_prih),0)- c.money_del AS " + DBManager.sDecimalType +
                    "(14,2)) " +
                    " FROM " + tKvar + " k" +
                    " LEFT OUTER JOIN " + charge + "charge_" + Month.ToString("00") + " c " +
                    "    ON c.num_ls = k.num_ls " +
                    " LEFT OUTER JOIN " + charge + "del_supplier ds " +
                    "   ON ds.num_ls = k.num_ls AND ds.nzp_supp = c.nzp_supp AND ds.nzp_serv = c.nzp_serv" +
                    "   AND ds.dat_account >= '01." + Month.ToString("00") + "." + Year + "'" +
                    "   AND ds.dat_account < '01." + nextMonth.ToString("00") + "." + nextYear + "' " +
                    " WHERE k.kod = 3 " + 
                    " GROUP BY k.num_ls, k.pkod, k.adres, c.money_del" +
                    " HAVING cast(" + DBManager.sNvlWord + "(sum(ds.sum_prih),0)- " +
                    DBManager.sNvlWord + "(c.money_del,0) AS " + DBManager.sDecimalType + "(14,2)) <> 0";
                ExecSQL(sql);
                //в del_supplier то, чего нет в charge
                sql =
                    " INSERT INTO " + tPaymentPerekidka + " (num_ls, pkod, adres,  " +
                    " sum_oplata_agent, difference)" +
                    " SELECT DISTINCT k.num_ls, k.pkod, k.adres,," +
                    " " + DBManager.sNvlWord + "(sum(ds.sum_prih),0),  " +
                    " " + DBManager.sNvlWord + "(sum(ds.sum_prih),0)  " +
                    " FROM " + tKvar + " k" +
                    " LEFT OUTER JOIN " + charge + "del_supplier ds " +
                    "   ON ds.num_ls = k.num_ls AND ds.dat_account >= '01." + Month.ToString("00") + "." + Year + "'" +
                    "   AND ds.dat_account < '01." + nextMonth.ToString("00") + "." + nextYear + "' " +
                    " WHERE k.kod = 3 " + 
                    " AND NOT EXISTS" +
                    "    (SELECT 1 FROM " + tPaymentPerekidka + " t" +
                    "     WHERE k.num_ls = t.num_ls)" +
                    " GROUP BY k.num_ls, k.pkod, adres ";
                ExecSQL(sql);

                #endregion
            }

            #region  готовим DataSet для отчета
            DataSet ds_rep = new DataSet();
            sql =
                " SELECT num_ls, pkod, adres, kvit, data_oplata, sum_post_oplat, sum_uchten_oplat, difference, type_name" +
                " FROM " + tPaymentDistrib +
                " ORDER BY adres";
            DataTable dt = ExecSQLToTable(sql);
            dt.TableName = "Q_master";
            ds_rep.Tables.Add(dt);


            sql =
                " SELECT num_ls, pkod, adres,  " +
                " sum_oplata_agent, sum_oplata_uk, sum_post_oplat," +
                " including_from_agent, including_from_uk, sum_uchten_oplat," +
                " difference_from_agent, difference_from_uk, difference" +
                " FROM " + tPaymentInSaldo +
                " ORDER BY adres";
            DataTable dt2 = ExecSQLToTable(sql);
            dt2.TableName = "Q_master2";
            ds_rep.Tables.Add(dt2);

            sql =
                " SELECT num_ls, pkod, adres, " +
                " sum_oplata_agent, " +
                " sum_uchten_oplat,  difference" +
                " FROM " + tPaymentPerekidka +
                " ORDER BY adres";
            DataTable dt3 = ExecSQLToTable(sql);
            dt3.TableName = "Q_master3";
            ds_rep.Tables.Add(dt3);
            #endregion

            return ds_rep;

        }



        /// <summary> Получить условия органичения по банкам данных </summary>
        private string GetWhereWp(string pref)
        {
            string whereWp = String.Empty;
            whereWp =  " AND " + pref + "nzp_wp = " + Banks + "";
            if (string.IsNullOrEmpty(TerritoryHeader) && !string.IsNullOrEmpty(whereWp))
            {
                string sql = " SELECT point FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point " + pref.TrimEnd('.') + " WHERE nzp_wp > 0 " + whereWp;
                DataTable terrTable = ExecSQLToTable(sql);
                foreach (DataRow row in terrTable.Rows)
                {
                    TerritoryHeader += row["point"].ToString().Trim() + ", ";
                }
                TerritoryHeader = TerritoryHeader.TrimEnd(',', ' ');
            }
            return whereWp;
        }

        protected override void CreateTempTable()
        {
            string sql =
                " CREATE TEMP TABLE " + tPDistrib +
                "( num_ls INTEGER," +
                " sum_prih " + DBManager.sDecimalType + "(10,2))" +
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

            sql =
                " CREATE TEMP TABLE " + tPaymentDistrib +
                " (num_ls " + DBManager.sDecimalType + "(13,0)," +
                " pkod " + DBManager.sDecimalType + "(13,0), " +
                " adres CHAR(200), " +
                " data_oplata CHAR(20)," +
                " kvit CHAR(30)," +
                " nzp_pack_ls " + DBManager.sDecimalType + "(13,0)," +
                " sum_post_oplat " + DBManager.sDecimalType + "(14,2)," +
                " sum_uchten_oplat " + DBManager.sDecimalType + "(14,2)," +
                " difference " + DBManager.sDecimalType + "(14,2)," +
                " type_name CHAR(60))" +
                DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql =
                " CREATE TEMP TABLE " + tPaymentInSaldo +
                " (num_ls " + DBManager.sDecimalType + "(13,0)," +
                " pkod " + DBManager.sDecimalType + "(13,0), " +
                " adres CHAR(200), " +
                " sum_oplata_agent " + DBManager.sDecimalType + "(14,2)," +
                " sum_oplata_uk " + DBManager.sDecimalType + "(14,2)," +
                " sum_post_oplat " + DBManager.sDecimalType + "(14,2)," +
                " sum_uchten_oplat " + DBManager.sDecimalType + "(14,2)," +
                " including_from_agent " + DBManager.sDecimalType + "(14,2)," +
                " including_from_uk " + DBManager.sDecimalType + "(14,2)," +
                " difference_from_agent " + DBManager.sDecimalType + "(14,2)," +
                " difference_from_uk " + DBManager.sDecimalType + "(14,2)," +
                " difference " + DBManager.sDecimalType + "(14,2) )" +
                DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql =
                " CREATE TEMP TABLE " + tPaymentPerekidka +
                " (num_ls " + DBManager.sDecimalType + "(13,0)," +
                " pkod " + DBManager.sDecimalType + "(13,0), " +
                " adres CHAR(200), " +
                " sum_oplata_agent " + DBManager.sDecimalType + "(14,2)," +
                " sum_uchten_oplat " + DBManager.sDecimalType + "(14,2)," +
                " difference " + DBManager.sDecimalType + "(14,2))" +
                DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql =
                " CREATE TEMP TABLE " + tKvar +
                " (num_ls " + DBManager.sDecimalType + "(13,0)," +
                " pkod " + DBManager.sDecimalType + "(13,0), " +
                " adres CHAR(200)," +
                " kod INTEGER)";
            ExecSQL(sql);

            sql =
                " CREATE TEMP TABLE " + tPackLs +
                " (num_ls " + DBManager.sDecimalType + "(13,0)," +
                " data_oplata CHAR(20)," +
                " kvit_oplata CHAR(20)," +
                " sum_prih " + DBManager.sDecimalType + "(14,2)," +
                " nzp_pack_ls " + DBManager.sDecimalType + "(13,0)," +
                " kod_sum INTEGER)";
            ExecSQL(sql);

            sql =
                " CREATE TEMP TABLE " + tFn +
                " (num_ls " + DBManager.sDecimalType + "(13,0)," +
                " nzp_pack_ls " + DBManager.sDecimalType + "(13,0)," +
                " dat_uchet DATE," +
                " sum_prih " + DBManager.sDecimalType + "(14,2))";
            ExecSQL(sql);

            sql =
                " CREATE TEMP TABLE " + tFs +
                " (num_ls " + DBManager.sDecimalType + "(13,0)," +
                " nzp_pack_ls " + DBManager.sDecimalType + "(13,0)," +
                " dat_uchet DATE," +
                " sum_prih " + DBManager.sDecimalType + "(14,2))";
            ExecSQL(sql);

            sql =
                " CREATE TEMP TABLE " + tCharge +
                " (num_ls " + DBManager.sDecimalType + "(13,0)," +
                " money_to " + DBManager.sDecimalType + "(14,2)," +
                " money_from " + DBManager.sDecimalType + "(14,2)," +
                " money_del " + DBManager.sDecimalType + "(14,2))";
            ExecSQL(sql);
        
        }


        protected override void DropTempTable()
        {
            try
            {
                string sql;
                if (TempTableInWebCashe(tPDistrib))
                {
                    sql = " DROP TABLE " + tPDistrib;
                    ExecSQL(sql, false);
                }
                if (TempTableInWebCashe(tPInSaldo))
                {
                    sql = " DROP TABLE " + tPInSaldo;
                    ExecSQL(sql, false);
                }
                if (TempTableInWebCashe(tPPerekidka))
                {
                    sql = " DROP TABLE " + tPPerekidka;
                    ExecSQL(sql, false);
                }
                if (TempTableInWebCashe(tResultPaymentDistrib))
                {
                    sql = " DROP TABLE " + tResultPaymentDistrib;
                    ExecSQL(sql, false);
                }
                if (TempTableInWebCashe(tResultPaymentInSaldo))
                {
                    sql = " DROP TABLE " + tResultPaymentInSaldo;
                    ExecSQL(sql, false);
                }
                if (TempTableInWebCashe(tResultPaymentPerekidka))
                {
                    sql = " DROP TABLE " + tResultPaymentPerekidka;
                    ExecSQL(sql, false);
                }
                if (TempTableInWebCashe(tPaymentDistrib))
                {
                    sql = " DROP TABLE " + tPaymentDistrib;
                    ExecSQL(sql, false);
                }
                if (TempTableInWebCashe(tPaymentInSaldo))
                {
                    sql = " DROP TABLE " + tPaymentInSaldo;
                    ExecSQL(sql, false);
                }
                if (TempTableInWebCashe(tPaymentPerekidka))
                {
                    sql = " DROP TABLE " + tPaymentPerekidka;
                    ExecSQL(sql, false);
                }
                if (TempTableInWebCashe(tKvar))
                {
                    sql = " DROP TABLE " + tKvar;
                    ExecSQL(sql, false);
                }
                if (TempTableInWebCashe(tPackLs))
                {
                    sql = " DROP TABLE " + tPackLs;
                    ExecSQL(sql, false);
                }
                if (TempTableInWebCashe(tFn))
                {
                    sql = " DROP TABLE " + tFn;
                    ExecSQL(sql, false);
                }
                if (TempTableInWebCashe(tFs))
                {
                    sql = " DROP TABLE " + tFs;
                    ExecSQL(sql, false);
                }
                if (TempTableInWebCashe(tCharge))
                {
                    sql = " DROP TABLE " + tCharge;
                    ExecSQL(sql, false);
                }
            }
            catch (Exception e)
            {
                MonitorLog.WriteLog("Отчет 'Проверка рассогласования оплат', удаление таблиц " + e.Message, MonitorLog.typelog.Error, false);
            }
        }
    }
}
