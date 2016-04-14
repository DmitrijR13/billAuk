using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.Tula.Properties;
using Bars.KP50.Utils;
using Newtonsoft.Json;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;

namespace Bars.KP50.Report.Tula.Reports
{
    class Report710118 : BaseSqlReport
    {
        public override string Name {
            get { return "71.1.18 Отчет по услугам с группировкой по нормативу"; }
        }

        public override string Description {
            get { return "71.1.18 Отчет по услугам с группировкой по нормативу"; }
        }

        public override IList<ReportGroup> ReportGroups {
            get {
                var result = new List<ReportGroup> { ReportGroup.Finans };
                return result;
            }
        }

        public override bool IsPreview {
            get { return false; }
        }

        protected override byte[] Template {
            get { return Resources.Report_71_1_18; }
        }

        public override IList<ReportKind> ReportKinds {
            get { return new List<ReportKind> { ReportKind.Base }; }
        }
        #region Параметры

        /// <summary>Месяц</summary>
        private int Month { get; set; }

        /// <summary>Год</summary>
        private int Year { get; set; }


        /// <summary>Услуги</summary>
        private List<long> Services { get; set; }

        /// <summary>Поставщики</summary>
        private string SupplierHeader { get; set; }

        /// <summary>Услуги</summary>
        private string ServiceHeader { get; set; }

        /// <summary>Заголовок территории</summary>
        protected string TerritoryHeader { get; set; }

        /// <summary>Поставщики, Агенты, Принципалы  </summary>
        protected BankSupplierParameterValue BankSupplier { get; set; }

        /// <summary>Превышение </summary>
        private bool RowCount { get; set; }

        /// <summary>Список префиксов банков в БД</summary>  
        private List<string> PrefBanks { get; set; }

        /// <summary>Дата с </summary>
        private DateTime DatS { get; set; }

        /// <summary>Дата по </summary>
        private DateTime DatPo { get; set; }

        #endregion


        public override List<UserParam> GetUserParams() {
            var curCalcMonthYear = DBManager.GetCurMonthYear();
            return new List<UserParam>
            {
                new MonthParameter { Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["month_"] : DateTime.Today.Month },
                new YearParameter { Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["yearr"] : DateTime.Today.Year },
                new BankSupplierParameter(),
                new ComboBoxParameter(true)
                {
                    Code = "Service",
                    Name = "Услуга",
                    Value = "6",
                    StoreData = new List<object>
                    {
                        new { Id = "6", Name = "Холодная вода" },
                        new { Id = "7", Name = "Водоотведение" },
                        new { Id = "9", Name = "Горячая вода" },
                        new { Id = "14", Name = "Холодная вода для нужд ГВС" }
                    }
                }   
            };
        }

        protected override void PrepareReport(FastReport.Report report) {
            var months = new[] {"","Январь","Февраль",
                 "Март","Апрель","Май","Июнь","Июль","Август","Сентябрь",
                 "Октябрь","Ноябрь","Декабрь"};
            report.SetParameterValue("dat", DateTime.Now.ToLongDateString());
            report.SetParameterValue("time", DateTime.Now.ToLongTimeString());
            report.SetParameterValue("month", months[Month]);
            report.SetParameterValue("year", Year);

            string headerParam = !string.IsNullOrEmpty(TerritoryHeader) ? "Территория: " + TerritoryHeader + "\n" : string.Empty;
            headerParam += !string.IsNullOrEmpty(SupplierHeader) ? "Поставщики: " + SupplierHeader + "\n" : string.Empty;
            headerParam += !string.IsNullOrEmpty(ServiceHeader) ? "Услуги: " + ServiceHeader : string.Empty;
            headerParam = headerParam.TrimEnd('\n');
            report.SetParameterValue("headerParam", headerParam);

            report.SetParameterValue("excel",
                RowCount
                    ? "Выборка записей ограничена первыми 70000 строками. Выберите другой формат экспортируемого файла, либо поставьте другие ограничения для отчета"
                    : "");
        }

        protected override void PrepareParams() {
            Month = UserParamValues["Month"].GetValue<int>();
            Year = UserParamValues["Year"].GetValue<int>();
            Services = UserParamValues["Service"].GetValue<List<long>>();
            BankSupplier = JsonConvert.DeserializeObject<BankSupplierParameterValue>(UserParamValues["BankSupplier"].Value.ToString());
        }

        public override DataSet GetData() {
            string sql;
            string centalKernel = ReportParams.Pref + DBManager.sKernelAliasRest;
            string whereSupp = GetWhereSupp();
            string whereServ = GetWhereServ();
            GetwhereWp();

            DatS = new DateTime(Year, Month, 1);
            DatPo = DatS.AddMonths(1).AddDays(-1);

            foreach (var pref in PrefBanks)
            {
                string prefData = pref + DBManager.sDataAliasRest,
                        prefKernel = pref + DBManager.sKernelAliasRest;
                string dataBaseCharge = pref + "_charge_" + (Year - 2000).ToString("00") + DBManager.tableDelimiter;
                string chargeTable = dataBaseCharge + "charge_" + Month.ToString("00");
                string countersTable = dataBaseCharge + "counters_" + Month.ToString("00");
                string gilTable = dataBaseCharge + "gil_" + Month.ToString("00");
                string perekidkaTable = dataBaseCharge + "perekidka ";
                string gkuTable = dataBaseCharge + "calc_gku_" + Month.ToString("00");

                if (TempTableInWebCashe(chargeTable) && TempTableInWebCashe(gkuTable) && TempTableInWebCashe(countersTable))
                {
                    #region заполнение временных таблиц

                    //заполнение информации из charge
                    sql = " INSERT INTO t_report_71_1_18( nzp_kvar, nzp_dom, nzp_supp, nzp_serv, is_device, isdel, " +
                          " tarif, sum_insaldo, money_to, " +
                          " sum_tarif, sum_charge, real_charge, " +
                          " reval, sum_outsaldo) " +
                          " SELECT c.nzp_kvar, " +
                          " nzp_dom, " +
                          " nzp_supp, " +
                          " nzp_serv, " +
                          " (CASE WHEN is_device = 0 THEN 0 " +
                          " WHEN is_device > 0 THEN 1 end) AS is_device, " +
                          " isdel, " +
                          " tarif, " +
                          " sum_insaldo, " +
                          " money_to, " +
                          " sum_tarif, " +
                          " (sum_tarif + reval) AS sum_charge, " +
                          " real_charge, " +
                          " reval, " +
                          " sum_outsaldo " +
                          " FROM " + chargeTable + " c INNER JOIN " + prefData + "kvar k ON k.nzp_kvar = c.nzp_kvar " +
                          " WHERE dat_charge IS NULL " + whereServ + whereSupp +
                          " AND ABS(c.sum_insaldo) + ABS(c.sum_tarif) + ABS(c.reval) + " +
                          " ABS(c.real_charge) + ABS(c.sum_money) + ABS(c.sum_outsaldo) > 0.001 ";
                    ExecSQL(sql);
                    ExecSQL(DBManager.sUpdStat + " t_report_71_1_18");

                    #region Добавление значения норматива

                    sql = " UPDATE t_report_71_1_18 SET val_norm = (SELECT MAX(rash_norm_one) " +
                          " FROM " + gkuTable + " g " +
                          " WHERE g.nzp_serv = t_report_71_1_18.nzp_serv " +
                          " AND g.nzp_supp = t_report_71_1_18.nzp_supp " +
                          " AND stek = 3 " +
                          " AND dat_charge IS NULL " +
                          " AND is_device = 0 " +
                          " AND g.nzp_kvar = t_report_71_1_18.nzp_kvar) ";
                    ExecSQL(sql);

                    #endregion

                    #region Добавляем корректировки начислений

                    if (TempTableInWebCashe("t_perekidka")) ExecSQL("DROP TABLE t_perekidka");

                    sql = " SELECT nzp_kvar, nzp_supp, nzp_serv, SUM(sum_rcl) AS sum_rcl INTO TEMP t_perekidka " +
                          " FROM " + perekidkaTable + " c " +
                          " WHERE type_rcl NOT IN (100,20) " +
                            " AND month_= " + Month + whereSupp + whereServ +
                          " GROUP BY 1,2,3 ";
                    ExecSQL(sql);

                    ExecSQL("CREATE INDEX ixt_t_perekidka_01 ON t_perekidka(nzp_kvar, nzp_serv)");
                    ExecSQL(DBManager.sUpdStat + " t_perekidka");

                    sql = " UPDATE t_report_71_1_18 SET sum_charge = sum_charge + (SELECT SUM(sum_rcl)" +
                          " FROM t_perekidka a" +
                          " WHERE t_report_71_1_18.nzp_kvar = a.nzp_kvar " +
                          " AND t_report_71_1_18.nzp_supp = a.nzp_supp " +
                          " AND t_report_71_1_18.nzp_serv = a.nzp_serv)" +
                          " WHERE 0 < (SELECT COUNT(*) " +
                          " FROM t_perekidka a" +
                          " WHERE t_report_71_1_18.nzp_kvar = a.nzp_kvar " +
                          " AND t_report_71_1_18.nzp_supp = a.nzp_supp " +
                          " AND t_report_71_1_18.real_charge <> 0 " +
                          " AND t_report_71_1_18.nzp_serv = a.nzp_serv )";
                    ExecSQL(sql);

                    #endregion

                    #region заполнение о кол-во проживающих, прибывших, выбивших

                    //Добавляем количество жильцов
                    sql = " UPDATE t_report_71_1_18 SET propis_count = " +
                          " (SELECT MAX(ROUND(gil)) " +
                           " FROM " + gkuTable + " a " +
                           " WHERE a.nzp_serv = t_report_71_1_18.nzp_serv " +
                             " AND a.nzp_supp = t_report_71_1_18.nzp_supp " +
                             " AND stek = 3 " +
                             " AND a.nzp_kvar = t_report_71_1_18.nzp_kvar) ";
                    ExecSQL(sql);
                    ExecSQL("DELETE FROM t_prm_71_1_18 ");

                    sql = "  UPDATE t_report_71_1_18 SET gil_prib = (SELECT MAX(val3) " +
                          " FROM " + gilTable + " g " +
                          " WHERE g.nzp_kvar = t_report_71_1_18.nzp_kvar " +
                          " AND dat_charge IS NULL " +
                          " AND stek = 3), " +
                          " gil_ub = (SELECT MAX(val5) " +
                          " FROM " + gilTable + " g " +
                          " WHERE g.nzp_kvar = t_report_71_1_18.nzp_kvar " +
                          " AND dat_charge IS NULL " +
                          " AND stek = 3) ";
                    ExecSQL(sql);

                    #endregion

                    #region состояние счета
                    
                    sql = " INSERT INTO t_prm_71_1_18 (nzp_kvar, val_prm) " +
                          " SELECT nzp AS nzp_kvar, " +
                                 " TRIM(val_prm) " +
                          " FROM " + prefData + "prm_3 " +
                          " WHERE nzp_prm = 51 " +
                            " AND is_actual <> 100 " +
                            " AND dat_s <= '" + DatPo.ToShortDateString() + "' " +
                            " AND dat_po >= '" + DatS.ToShortDateString() + "' ";
                    ExecSQL(sql);
                    ExecSQL(DBManager.sUpdStat + " t_prm_71_1_18 ");

                    sql = " UPDATE t_report_71_1_18 SET statusls = " +
                          " (SELECT MAX(CASE val_prm WHEN '1' THEN 1 " +
                                                   " WHEN '2' THEN 2 END) " +
                           " FROM t_prm_71_1_18 t " +
                           " WHERE t.nzp_kvar = t_report_71_1_18.nzp_kvar) ";
                    ExecSQL(sql);
                    ExecSQL("TRUNCATE t_prm_71_1_18 ");

                    #endregion

                    #region Добавляем нормативы

                    sql = " UPDATE t_report_71_1_18 set cnt2= (" +
                          " SELECT MAX(cnt2) " +
                          " FROM " + countersTable + " a " +
                          " WHERE stek=3 AND nzp_type=3 " +
                          "  AND  a.nzp_kvar = t_report_71_1_18.nzp_kvar " +
                          "  AND  a.nzp_serv = t_report_71_1_18.nzp_serv " +
                          "  )";

                    ExecSQL(sql);

                    sql = " UPDATE t_report_71_1_18 set cnt2 = (" +
                          " SELECT MAX(val_prm" + DBManager.sConvToNum + ") " +
                          " FROM " + pref + DBManager.sDataAliasRest + "prm_1 p " +
                          " WHERE p.nzp = t_report_71_1_18.nzp_kvar " +
                          " AND p.nzp_prm = 7 " +
                          " AND p.is_actual <> 100 " +
                          " AND dat_s <= DATE('" + DatPo.ToShortDateString() + "') " +
                          " AND dat_po >= DATE('" + DatS.ToShortDateString() + "')) " +
                          " WHERE cnt2 is null ";
                    ExecSQL(sql);

                    sql = " UPDATE t_report_71_1_18 set cnt2 = 0 where cnt2 is null ";
                    ExecSQL(sql);

                    sql = " UPDATE t_report_71_1_18 set cnt3 =38 " +
                          " WHERE nzp_serv in (6,7,324,353) and cnt2 is not null ";
                    ExecSQL(sql);

                    sql = " UPDATE t_report_71_1_18 set cnt3 =16 " +
                          " WHERE nzp_serv in (9) and cnt2 is not null ";
                    ExecSQL(sql);

                    sql = " UPDATE t_report_71_1_18 set cnt2 = 0 where cnt2 is null ";
                    ExecSQL(sql);
                    #endregion

                    #region наименование нормативов

                    sql = " INSERT INTO t_res_y_71_1_6(nzp_y, name_y) " +
                          " SELECT nzp_y, " +
                                 " name_y " +
                          " FROM " + prefKernel + "res_y " +
                          " WHERE nzp_res = (SELECT val_prm " + DBManager.sConvToInt +
                                           " FROM " + prefData + "prm_13 " +
                                           " WHERE nzp_prm = 172 " +
                                             " AND is_actual <> 100 " +
                                             " AND dat_s = (SELECT MAX(dat_s) " +
                                                          " FROM " + prefData + "prm_13 " +
                                                          " WHERE is_actual <> 100 " +
                                                            " AND nzp_prm = 172 " +
                                                            " AND dat_s <= DATE('" + DatPo.ToShortDateString() + "') " +
                                                            " AND dat_po >= DATE('" + DatS.ToShortDateString() + "')) " +
                                             " AND dat_s <= DATE('" + DatPo.ToShortDateString() + "') " +
                                             " AND dat_po >= DATE('" + DatS.ToShortDateString() + "')) ";
                    ExecSQL(sql);

                    sql = " UPDATE t_report_71_1_18 SET name_norm = " +
                          " (SELECT MAX(TRIM(name_y)) " +
                          " FROM t_res_y_71_1_6 t  " +
                          " WHERE  t.nzp_y= t_report_71_1_18.cnt2)" +
                          " WHERE name_norm IS NULL " +
                          " AND statusls = 1 " +
                          " AND is_device = 0 " +
                          " AND isdel = 0 ";
                    ExecSQL(sql);  
                    #endregion

                    #region Батарея тарифов

                    if (TempTableInWebCashe("t_tarif")) ExecSQL("DROP TABLE t_tarif");

                    sql = " SELECT nzp_dom, nzp_serv, nzp_supp, MAX(tarif) AS tarif INTO TEMP t_tarif " +
                          " FROM t_report_71_1_18 " +
                          " GROUP BY 1,2,3";
                    ExecSQL(sql);

                    if (TempTableInWebCashe("t_servtarif")) ExecSQL("DROP TABLE t_servtarif");
                    sql = " SELECT nzp_serv, nzp_supp, max(tarif) as tarif INTO TEMP t_servtarif " +
                          " FROM t_tarif" +
                          " GROUP BY 1,2 ";
                    ExecSQL(sql);

                    sql = " UPDATE t_report_71_1_18 SET tarif = " +
                          " (SELECT tarif " +
                           " FROM t_tarif " +
                           " WHERE t_report_71_1_18.nzp_dom = t_tarif.nzp_dom " +
                             " AND t_report_71_1_18.nzp_supp = t_tarif.nzp_supp " +
                             " AND t_report_71_1_18.nzp_serv = t_tarif.nzp_serv) " +
                          " WHERE tarif = 0 ";
                    ExecSQL(sql);

                    sql = " UPDATE t_report_71_1_18 SET tarif = " +
                          " (SELECT tarif" +
                           " FROM t_servtarif t " +
                           " WHERE t_report_71_1_18.nzp_serv = t.nzp_serv" +
                             " AND t_report_71_1_18.nzp_supp = t.nzp_supp) " +
                          " WHERE tarif IS NULL OR tarif = 0";
                    ExecSQL(sql);

                    #endregion

                    #region Добавляем счетчик по канализации

                    if (TempTableInWebCashe("t_device")) ExecSQL("DROP TABLE t_device");
                    sql = "CREATE TEMP TABLE t_device (nzp_kvar INTEGER, is_device INTEGER)" + DBManager.sUnlogTempTable;
                    ExecSQL(sql);

                    sql = " INSERT INTO t_device " +
                          " SELECT nzp_kvar, MAX(is_device) AS is_device " +
                          " FROM t_report_71_1_18 " +
                          " WHERE is_device > 0  " +
                            " AND nzp_serv IN (6,9)" +
                          " GROUP BY 1";
                    ExecSQL(sql);

                    // устанавливаем счетчик за канализацию, там где есть водопроводный счетчик
                    sql = " UPDATE t_report_71_1_18 " +
                          " SET is_device = 1 " +
                          " WHERE nzp_kvar IN (SELECT nzp_kvar FROM t_device WHERE is_device = 1 )   " +
                            " AND nzp_serv = 7 AND is_device = 0  ";
                    ExecSQL(sql);

                    sql = " UPDATE t_report_71_1_18 SET val_norm = 0 " +
                          " WHERE is_device > 0 AND val_norm <> 0 ";
                    ExecSQL(sql);

                    #endregion

                    //установка наименование нормативов
                    ExecSQL(" UPDATE t_report_71_1_18 SET name_norm = 'по счетчику' WHERE is_device > 0"); 
                    ExecSQL(" UPDATE t_report_71_1_18 SET name_norm = 'закрытые услуги/ЛС' WHERE isdel = 1 OR statusls = 2 ");
                    ExecSQL(" UPDATE t_report_71_1_18 SET name_norm = 'Не определено'  WHERE name_norm IS NULL ");
                    //неопределённые записи
                    ExecSQL(" UPDATE t_report_71_1_18 SET val_norm = 0 WHERE name_norm = 'Не определено' AND val_norm IS NULL ");
                    //сортировка
                    ExecSQL(" UPDATE t_report_71_1_18 SET ordering = 1 WHERE is_device = 0 ");
                    ExecSQL(" UPDATE t_report_71_1_18 SET ordering = 2 WHERE name_norm = 'Не определено' ");
                    ExecSQL(" UPDATE t_report_71_1_18 SET ordering = 3 WHERE is_device > 0 ");
                    ExecSQL(" UPDATE t_report_71_1_18 SET ordering = 4 WHERE name_norm = 'закрытые услуги/ЛС' ");

                    sql =
                        " INSERT INTO t_svod_71_1_18( nzp_supp, nzp_serv, name_norm, is_device, val_norm, ordering, " +
                        " sum_insaldo, money_to, rashod, sum_tarif, real_charge, reval, sum_outsaldo, " +
                        " propis_count, gil_prib, gil_ub, count_ls )" +
                        " SELECT nzp_supp, " +
                        " nzp_serv, " +
                        " name_norm, " +
                        " is_device, " +
                        " val_norm, " +
                        " ordering, " +
                        " SUM(sum_insaldo), " +
                        " SUM(money_to), " +
                        " SUM(CASE WHEN tarif > 0 THEN sum_charge / tarif ELSE 0 END) AS rashod, " +
                        " SUM(sum_tarif), " +
                        " SUM(real_charge), " +
                        " SUM(reval), " +
                        " SUM(sum_outsaldo), " +
                        " SUM(propis_count), " +
                        " SUM(gil_prib), " +
                        " SUM(gil_ub) , " +
                        " COUNT(nzp_kvar)" +
                        " FROM t_report_71_1_18 " +
                        " GROUP BY 1,2,3,4,5,6 ";
                    ExecSQL(sql);

                    ExecSQL("TRUNCATE t_report_71_1_18 ");

                    #endregion
                }
            }

            sql = " SELECT t.ordering, " +
                         " TRIM(p.payer) AS principal," +
                         " TRIM(service) AS service, " +
                         " TRIM(name_supp) AS name_supp, " +
                         " TRIM(name_norm) AS name_norm, " +
                         " val_norm, " +
                         " SUM(sum_insaldo) AS sum_insaldo, " +
                         " SUM(money_to) AS money_to, " +
                         " SUM(rashod) AS rashod, " +
                         " SUM(sum_tarif) AS sum_tarif, " +
                         " SUM(real_charge) AS real_charge, " +
                         " SUM(reval) AS reval, " +
                         " SUM(sum_outsaldo) AS sum_outsaldo, " +
                         " SUM(propis_count) AS gil_count, " +
                         " SUM(gil_prib) AS gil_prib, " +
                         " SUM(gil_ub) AS gil_ub, " +
                         " SUM(count_ls) AS count_ls " +
                  " FROM t_svod_71_1_18 t, " +
                         centalKernel + "services sv, " +
                         centalKernel + "supplier sp,  " +
                         centalKernel + "s_payer p  " +
                  " WHERE sv.nzp_serv = t.nzp_serv " +
                    " AND sp.nzp_supp = t.nzp_supp " +
                    " AND sp.nzp_payer_princip = p.nzp_payer " +
                  " GROUP BY principal, service, name_supp, name_norm, val_norm, t.ordering " +
                  " ORDER BY principal, service, name_supp, t.ordering, name_norm, val_norm ";

            DataTable dt = ExecSQLToTable(sql);
            dt.TableName = "Q_master";

            if (dt.Rows.Count > 65000 && ReportParams.ExportFormat == ExportFormat.Excel2007)
            {
                var dtr = dt.Rows.Cast<DataRow>().Skip(65000).ToArray();
                dtr.ForEach(dt.Rows.Remove);
            }

            var ds = new DataSet();
            ds.Tables.Add(dt);
            return ds;

        }

        /// <summary>
        /// Получить условия органичения по поставщикам
        /// </summary>
        /// <returns></returns>
        private string GetWhereSupp() {
            string whereSupp = String.Empty;
            if (BankSupplier != null && BankSupplier.Suppliers != null)
            {

                string supp = string.Empty;
                supp = BankSupplier.Suppliers.Aggregate(supp, (current, nzpSupp) => current + (nzpSupp + ","));
                whereSupp += " and nzp_payer_supp in (" + supp.TrimEnd(',') + ")";
            }

            if (BankSupplier != null && BankSupplier.Principals != null)
            {

                string supp = string.Empty;
                supp = BankSupplier.Principals.Aggregate(supp, (current, nzpSupp) => current + (nzpSupp + ","));
                whereSupp += " and nzp_payer_princip in (" + supp.TrimEnd(',') + ")";
            }

            if (BankSupplier != null && BankSupplier.Agents != null)
            {

                string supp = string.Empty;
                supp = BankSupplier.Agents.Aggregate(supp, (current, nzpSupp) => current + (nzpSupp + ","));
                whereSupp += " and nzp_payer_agent in (" + supp.TrimEnd(',') + ")";
            }

            string oldsupp = ReportParams.GetRolesCondition(Constants.role_sql_supp);

            whereSupp = whereSupp.TrimEnd(',');


            if (!String.IsNullOrEmpty(whereSupp) || !String.IsNullOrEmpty(oldsupp))
            {
                if (!String.IsNullOrEmpty(oldsupp))
                    whereSupp += " AND nzp_supp in (" + oldsupp + ")";

                //Поставщики
                SupplierHeader = String.Empty;
                string sql = " SELECT name_supp from " +
                             ReportParams.Pref + DBManager.sKernelAliasRest + "supplier " +
                             " WHERE nzp_supp > 0 " + whereSupp;
                DataTable supp = ExecSQLToTable(sql);
                foreach (DataRow dr in supp.Rows)
                {
                    SupplierHeader += "(" + dr["name_supp"].ToString().Trim() + "), ";
                }
                SupplierHeader = SupplierHeader.TrimEnd(',', ' ');
            }
            return " and nzp_supp in (select nzp_supp from " +
                   ReportParams.Pref + DBManager.sKernelAliasRest + "supplier " +
                   " where nzp_supp>0 " + whereSupp + ")";
        }

        private string GetWhereServ() {
            string whereServ = String.Empty;
            whereServ = (Services != null && Services.Count != 0) ? Services.Aggregate(whereServ, (current, nzpServ) => current + (nzpServ + ",")) : " 6, 7, 9 ";
            whereServ = whereServ.TrimEnd(',');
            whereServ = !String.IsNullOrEmpty(whereServ) ? " AND c.nzp_serv in (" + whereServ + ")" : String.Empty;

            if (Services != null && Services.Count != 0 && Services.Count != 3)
            {
                ServiceHeader = string.Empty;
                string sql = " SELECT service from " + ReportParams.Pref + DBManager.sKernelAliasRest + "services c  WHERE nzp_serv > 0 " + whereServ;
                DataTable serv = ExecSQLToTable(sql);
                foreach (DataRow dr in serv.Rows)
                {
                    ServiceHeader += dr["service"].ToString().Trim() + ", ";
                }
                ServiceHeader = ServiceHeader.TrimEnd(',', ' ');
            }
            return whereServ;
        }

        private void GetwhereWp() {
            string whereWp = String.Empty;
            if (BankSupplier != null && BankSupplier.Banks != null)
            {
                whereWp = BankSupplier.Banks.Aggregate(whereWp, (current, nzpWp) => current + (nzpWp + ","));
            }
            else
            {
                whereWp = ReportParams.GetRolesCondition(Constants.role_sql_wp);
            }
            whereWp = whereWp.TrimEnd(',');
            whereWp = !String.IsNullOrEmpty(whereWp) ? " AND nzp_wp in (" + whereWp + ")" : String.Empty;
            PrefBanks = new List<string>();
            if (!string.IsNullOrEmpty(whereWp))
            {
                TerritoryHeader = String.Empty;
                string sql = " SELECT point, bd_kernel FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point WHERE nzp_wp > 0 " + whereWp;
                DataTable terrTable = ExecSQLToTable(sql);
                foreach (DataRow row in terrTable.Rows)
                {
                    TerritoryHeader += row["point"].ToString().Trim() + ", ";
                    PrefBanks.Add(row["bd_kernel"].ToString().Trim());
                }
                TerritoryHeader = TerritoryHeader.TrimEnd(',', ' ');
            }
            else
            {
                string sql = " SELECT bd_kernel FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point WHERE nzp_wp > 0 ";
                DataTable terrTable = ExecSQLToTable(sql);
                foreach (DataRow row in terrTable.Rows)
                {
                    PrefBanks.Add(row["bd_kernel"].ToString().Trim());
                }
            }
        }

        protected override void CreateTempTable() {
            string sql = " CREATE TEMP TABLE t_report_71_1_18( " +
                            " nzp_kvar INTEGER, " +
                            " nzp_dom INTEGER, " +
                            " nzp_supp INTEGER, " +
                            " nzp_serv INTEGER, " +
                            " statusls INTEGER, " +
                            " is_device INTEGER, " +
                            " propis_count INTEGER, " +
                            " gil_prib INTEGER, " +
                            " gil_ub INTEGER, " +
                            " isdel INTEGER, " +
                            " cnt2 INTEGER, " +
                            " cnt3 INTEGER, " +
                            " name_norm CHAR(100), " +
                            " ordering INTEGER, " +
                            " val_norm " + DBManager.sDecimalType + "(14,4), " +
                            " sum_insaldo " + DBManager.sDecimalType + "(14,2), " +
                            " tarif " + DBManager.sDecimalType + "(14,4), " +
                            " rashod " + DBManager.sDecimalType + "(14,7), " +
                            " sum_charge " + DBManager.sDecimalType + "(14,2), " +
                            " sum_tarif " + DBManager.sDecimalType + "(14,2), " +
                            " money_to " + DBManager.sDecimalType + "(14,2), " +
                            " real_charge " + DBManager.sDecimalType + "(14,2), " +
                            " reval " + DBManager.sDecimalType + "(14,2), " +
                            " sum_outsaldo " + DBManager.sDecimalType + "(14,2))" + DBManager.sUnlogTempTable;
            ExecSQL(sql);
            ExecSQL(" CREATE INDEX ix_t_report_71_1_18_1 ON t_report_71_1_18(nzp_kvar) ");
            ExecSQL(" CREATE INDEX ix_t_report_71_1_18_2 ON t_report_71_1_18(is_device, isdel) ");

            sql = " CREATE TEMP TABLE t_svod_71_1_18( " +
                        " isdel INTEGER, " +
                        " is_device INTEGER, " +
                        " count_ls INTEGER, " +
                        " propis_count INTEGER, " +
                        " gil_prib INTEGER, " +
                        " gil_ub INTEGER, " +
                        " nzp_supp INTEGER, " +
                        " nzp_pricipal INTEGER, " +
                        " nzp_serv INTEGER, " +
                        " name_norm CHAR(100), " +
                        " ordering INTEGER, " +
                        " val_norm " + DBManager.sDecimalType + "(14,4), " +
                        " sum_insaldo " + DBManager.sDecimalType + "(14,2), " +
                        " tarif " + DBManager.sDecimalType + "(14,4), " +
                        " rashod " + DBManager.sDecimalType + "(14,7), " +
                        " sum_tarif " + DBManager.sDecimalType + "(14,2), " +
                        " money_to " + DBManager.sDecimalType + "(14,2), " +
                        " real_charge " + DBManager.sDecimalType + "(14,2), " +
                        " reval " + DBManager.sDecimalType + "(14,2), " +
                        " sum_outsaldo " + DBManager.sDecimalType + "(14,2)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);
            ExecSQL(" CREATE INDEX ix_t_svod_71_1_18_1 ON t_svod_71_1_18(nzp_supp) ");

            sql = "CREATE TEMP TABLE t_prm_71_1_18 (" +
                    " nzp_kvar INTEGER, " +
                    " val_prm CHARACTER(128)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);
            ExecSQL(" CREATE INDEX ix_t_prm_71_1_18_1 ON t_prm_71_1_18(nzp_kvar) ");

            sql = " CREATE TEMP TABLE t_res_y_71_1_6(" +
                  " nzp_y INTEGER, " +
                  " name_y CHARACTER(250)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        protected override void DropTempTable() {
            ExecSQL(" DROP TABLE t_report_71_1_18 ");
            ExecSQL(" DROP TABLE t_prm_71_1_18 ");
            ExecSQL(" DROP TABLE t_svod_71_1_18 ");
            ExecSQL(" drop table t_res_y_71_1_6 ", true);


        }
    }
}
