using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Bars.KP50.Report.Base;
using Bars.KP50.Utils;
using Bars.KP50.Report.Tula.Properties;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using Newtonsoft.Json;

namespace Bars.KP50.Report.Tula.Reports
{
    class Report71111 : BaseSqlReport
    {
        public override string Name
        {
            get { return "71.1.11 Отчет по объемам тепла"; }
        }

        public override string Description
        {
            get { return "Информация об объемах услуг водоснабжения, оказываемых населению, и количестве проживающих"; }
        }

        public override IList<ReportGroup> ReportGroups
        {
            get
            {
                var result = new List<ReportGroup> { ReportGroup.Reports };
                return result;
            }
        }

        public override bool IsPreview
        {
            get { return false; }
        }

        protected override byte[] Template
        {
            get { return Resources.Report_71_1_11; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base }; }
        }



        /// <summary>Заголовок поставщиков</summary>
        protected string SupplierHeader { get; set; }

        /// <summary>Поставщики, Агенты, Принципалы  </summary>
        protected BankSupplierParameterValue BankSupplier { get; set; }

        /// <summary>Заголовок территории</summary>
        protected string TerritoryHeader { get; set; }

        /// <summary>Год</summary>
        private int Year { get; set; }

        /// <summary>Месяц</summary>
        private int Month { get; set; }

        /// <summary> Код услуги </summary>
        private string IdService { get; set; }

        public override List<UserParam> GetUserParams()
        {
            return new List<UserParam>
            {
                new MonthParameter {Name = "Месяц с", Value = DateTime.Today.Month},
                new YearParameter {Name = "Год с", Value = DateTime.Today.Year},
                new BankSupplierParameter(),
                new ComboBoxParameter
                {
                    Code = "Service",
                    Name = "Услуга",
                    Value = "6",
                    StoreData = new List<object>
                    {
                        new { Id = "6", Name = "Холодная вода" },
                        new { Id = "7", Name = "Водоотведение" },
                        new { Id = "9", Name = "Горячая вода" }
                    }
                }
            };
        }

        protected override void PrepareParams()
        {
            Month = UserParamValues["Month"].GetValue<int>();
            Year = UserParamValues["Year"].Value.To<int>();
            BankSupplier = JsonConvert.DeserializeObject<BankSupplierParameterValue>(UserParamValues["BankSupplier"].Value.ToString());
            IdService = UserParamValues["Service"].GetValue<int>().ToString(CultureInfo.InvariantCulture);
        }

        protected override void PrepareReport(FastReport.Report report)
        {
            string service = string.Empty;
            switch (IdService)
            {
                case "6": service = "Холодная вода"; break;
                case "7": service = "Водоотведение"; break;
                case "9": service = "Горячая вода"; break;
            }
            report.SetParameterValue("service", service);
            var months = new[] {"","Январь","Февраль",
                 "Март","Апрель","Май","Июнь","Июль","Август","Сентябрь",
                 "Октябрь","Ноябрь","Декабрь"};
            report.SetParameterValue("period","за " + months[Month] + " " + Year + " г.");
            report.SetParameterValue("date", DateTime.Now.ToShortDateString());
            report.SetParameterValue("time", DateTime.Now.ToLongTimeString());

            string headerParam =  !string.IsNullOrEmpty(SupplierHeader) ? "Поставщики: " + SupplierHeader : string.Empty;
            report.SetParameterValue("headerParam", headerParam);
        }

        public override DataSet GetData()
        {
            MyDataReader reader;

            string whereSupp = GetWhereSupp("b.nzp_supp");

            string sql = " SELECT bd_kernel AS pref, point " +
                 " FROM  " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point " +
                 " WHERE nzp_wp>1 " + GetWhereWp();
            ExecRead(out reader, sql);

            #region --заполнение временной таблицы
            while (reader.Read())
            {
                string point = reader["point"].ToString().Trim();
                string pref = reader["pref"].ToString().Trim(),
                        prefData = pref + DBManager.sDataAliasRest,
                            prefKernel = pref + DBManager.sKernelAliasRest;
                string gkuTable = pref + "_charge_" + (Year - 2000).ToString("00") + DBManager.tableDelimiter + "calc_gku_" + Month.ToString("00"),
                        countersTable = pref + "_charge_" + (Year - 2000).ToString("00") + DBManager.tableDelimiter + "counters_" + Month.ToString("00"),
                          chargeTable = pref + "_charge_" + (Year - 2000).ToString("00") + DBManager.tableDelimiter + "charge_" + Month.ToString("00");

                if (TempTableInWebCashe(chargeTable))
                {

                    sql = " INSERT INTO t_report_71_1_11(nzp_dom, nzp_kvar, nzp_supp, is_device, c_calc) " +
                          " SELECT k.nzp_dom, " +
                                 " k.nzp_kvar, " +
                                 " nzp_supp, " +
                                 " is_device , " +
                                 " SUM(c_calc)  " +
                          " FROM " + pref + DBManager.sDataAliasRest + "kvar k INNER JOIN " + chargeTable + " b ON k.nzp_kvar = b.nzp_kvar " +
                          " WHERE b.nzp_serv = " + IdService + whereSupp +
                            " AND b.dat_charge IS NULL " +
                            " AND b.tarif > 0 " +
                            " AND ABS(b.sum_insaldo) + " +
                                  " ABS(b.sum_tarif) + " +
                                    " ABS(b.reval) + " +
                                      " ABS(b.real_charge) + " +
                                        " ABS(b.sum_money) + " +
                                          " ABS(b.sum_outsaldo) > 0.001" +
                          " GROUP BY 1,2,3,4";
                    ExecSQL(sql);

                    ExecSQL(DBManager.sUpdStat + " t_report_71_1_11 ");

                    for (int nMonth = 1; nMonth <= Month; nMonth++)
                    {
                        sql = " INSERT INTO tyear_report_71_1_11 (nzp_kvar, nzp_supp, nzp_serv, c_calc) " +
                              " SELECT nzp_kvar, nzp_supp, nzp_serv, SUM(c_calc) " +
                              " FROM " + pref + "_charge_" + (Year - 2000).ToString("00") + DBManager.tableDelimiter + "charge_" + nMonth.ToString("00") + " b " +
                              " WHERE nzp_serv = " + IdService + whereSupp +
                                " AND dat_charge IS NULL " +
                                " AND b.tarif > 0 " +
                                " AND ABS(sum_insaldo) + " +
                                      " ABS(sum_tarif) + " +
                                        " ABS(reval) + " +
                                          " ABS(real_charge) + " +
                                            " ABS(sum_money) + " +
                                              " ABS(sum_outsaldo) > 0.001" +
                              " GROUP BY 1,2,3";
                        ExecSQL(sql);
                    }

                    ExecSQL(DBManager.sUpdStat + " tyear_report_71_1_11 ");

                    sql = " UPDATE t_report_71_1_11 SET c_calc_year = " +
                          " (SELECT SUM(c_calc) " +
                           " FROM tyear_report_71_1_11 a " +
                           " WHERE t_report_71_1_11.nzp_kvar = a.nzp_kvar " +
                            " AND t_report_71_1_11.nzp_supp = a.nzp_supp " +
                            " AND a.nzp_serv = " + IdService + " ) ";
                    ExecSQL(sql);

                    ExecSQL(" DELETE FROM tyear_report_71_1_11 ");

                    sql = " UPDATE t_report_71_1_11 SET gil = " +
                          " (SELECT MAX(gil) " +
                           " FROM " + gkuTable + " a " +
                           " WHERE t_report_71_1_11.nzp_kvar = a.nzp_kvar " +
                            " AND t_report_71_1_11.nzp_supp = a.nzp_supp " +
                            " AND a.nzp_serv = " + IdService + " ) ";
                    ExecSQL(sql);

                    sql = " UPDATE t_report_71_1_11 SET norm = " +
                          " (SELECT MAX(rash_norm_one) " +
                           " FROM " + gkuTable + " a " +
                           " WHERE t_report_71_1_11.nzp_kvar = a.nzp_kvar " +
                             " AND t_report_71_1_11.nzp_supp = a.nzp_supp" +
                             " AND a.nzp_serv = " + IdService + ")";
                    ExecSQL(sql);

                    ExecSQL(" update t_report_71_1_11 set norm = 0 where norm is null ");
                    ExecSQL(" update t_report_71_1_11 set gil = 0 where gil is null ");

                    sql = " UPDATE t_report_71_1_11 SET is_mkd = 1 " +
                          " WHERE nzp_dom IN (SELECT nzp " +
                          " FROM " + prefData + "prm_2 " +
                          " WHERE nzp_prm = 2030 " +
                            " AND is_actual = 1 " +
                            " AND val_prm = '1')";
                    ExecSQL(sql);

                    sql = " UPDATE t_report_71_1_11 SET name_norm = (SELECT MAX(name_y) " +
                          " FROM " + countersTable + " a INNER JOIN " + pref + DBManager.sKernelAliasRest + "res_y y ON y.nzp_res = a.cnt3 " +
                          " WHERE nzp_serv = 6 " +
                            " AND a.cnt2 = y.nzp_y " +
                            " AND t_report_71_1_11.nzp_kvar = a.nzp_kvar " +
                            " AND stek = 3) ";
                    ExecSQL(sql);

                    string tableNum = IdService == "9" ? "177" : "172";

                    sql = " SELECT val_prm  " +
                          " FROM " + prefData + "prm_13 a " +
                          " WHERE nzp_prm = " + tableNum +
                            " AND is_actual = 1 " +
                            " AND dat_s <= " + DBManager.MDY(Month, 01, Year) +
                            " AND dat_po >= " + DBManager.MDY(Month, 01, Year);
                    object obj = ExecScalar(sql);

                    int nzpRes = obj != null ? Convert.ToInt32(obj) : 38;

                    string nzpPrm = IdService == "9" ? "463" : "7";
                    string nzpX = IdService == "7"
                        ? " in (3,4) "
                        : IdService == "9" ? " = 2" : " = 1 ";

                    sql = " UPDATE t_report_71_1_11 SET nzp_y = " +
                          " (SELECT MAX(val_prm" + DBManager.sConvToNum + ") " +
                           " FROM " + prefData + "prm_1 p " +
                           " WHERE nzp_prm = " + nzpPrm +
                             " AND is_actual = 1 " +
                             " AND t_report_71_1_11.nzp_kvar = p.nzp " +
                             " AND dat_s <= " + DBManager.MDY(Month, 01, Year) +
                             " AND dat_po >= " + DBManager.MDY(Month, 01, Year) + ")" +
                          " WHERE name_norm IS NULL ";
                    ExecSQL(sql);

                    if (IdService == "9")
                    {
                        sql = " UPDATE t_report_71_1_11 SET nzp_y = " +
                              " (SELECT MAX(val_prm" + DBManager.sConvToNum + ") " +
                               " FROM " + prefData + "prm_2 p " +
                               " WHERE nzp_prm = 38 " +
                                 " AND is_actual = 1 " +
                                 " AND t_report_71_1_11.nzp_dom = p.nzp " +
                                 " AND dat_s <= " + DBManager.MDY(Month, 01, Year) +
                                 " AND dat_po >= " + DBManager.MDY(Month, 01, Year) + ")" +
                              " WHERE name_norm IS NULL AND nzp_y IS NULL";
                        ExecSQL(sql);
                    }

                    sql = " UPDATE t_report_71_1_11 SET norm = " +
                          " (SELECT MAX(value" + DBManager.sConvToNum + ") " +
                           " FROM  " + prefKernel + "res_values y " +
                           " WHERE t_report_71_1_11.nzp_y = y.nzp_y " +
                             " AND nzp_res = " + nzpRes + "  " +
                             " AND nzp_x " + nzpX + ") " +
                          " WHERE nzp_y IS NOT NULL " +
                            " AND name_norm IS NULL ";
                    ExecSQL(sql);

                    sql = " UPDATE t_report_71_1_11 SET name_norm = " +
                          " (SELECT MAX(name_y) " +
                           " FROM " + prefKernel + "res_y y " +
                           " WHERE t_report_71_1_11.nzp_y = y.nzp_y " +
                             " AND nzp_res = " + nzpRes + ") " +
                          " WHERE nzp_y IS NOT NULL " +
                            " AND name_norm IS NULL ";
                    ExecSQL(sql);


                    sql = " UPDATE t_report_71_1_11 SET name_norm = " +
                          " (SELECT MAX(name_y) " +
                           " FROM " + prefKernel + "res_y y " +
                           " WHERE t_report_71_1_11.nzp_y = y.nzp_y " +
                             " AND nzp_res = " + nzpRes + ") " +
                          " WHERE nzp_y IS NOT NULL " +
                            " AND name_norm IS NULL ";
                    ExecSQL(sql);

                    ExecSQL(" UPDATE t_report_71_1_11 SET name_norm = 'Не определен' WHERE name_norm IS NULL ");

                    sql = " INSERT INTO tall_report_71_1_11(point, name_norm, norm, mkd_c_calc_norm, mkd_c_calc_ipu, " +
                                                                " chs_c_calc_norm, chs_c_calc_ipu, mkd_gil_norm, chs_gil_norm, mkd_c_calc_year, chs_c_calc_year) " +
                          " SELECT '" + point + "' AS point, name_norm, norm, " +
                                 " SUM(CASE WHEN is_mkd = 1 AND is_device = 0 THEN c_calc ELSE 0 END) AS mkd_c_calc_norm, " +
                                 " SUM(CASE WHEN is_mkd = 1 AND is_device > 0 THEN c_calc ELSE 0 END) AS mkd_c_calc_ipu, " +
                                 " SUM(CASE WHEN is_mkd = 0 AND is_device = 0 THEN c_calc ELSE 0 END) AS chs_c_calc_norm, " +
                                 " SUM(CASE WHEN is_mkd = 0 AND is_device > 0 THEN c_calc ELSE 0 END) AS chs_c_calc_ipu, " +
                                 " SUM(CASE WHEN is_mkd = 1 AND is_device = 0 THEN gil ELSE 0 END) AS mkd_gil_norm, " +
                                 " SUM(CASE WHEN is_mkd = 0 AND is_device = 0 THEN gil ELSE 0 END) AS chs_gil_norm, " +
                                 " SUM(CASE WHEN is_mkd = 1 THEN c_calc_year ELSE 0 END) AS mkd_c_calc_year, " +
                                 " SUM(CASE WHEN is_mkd = 0 THEN c_calc_year ELSE 0 END) AS chs_c_calc_year " +
                          " FROM t_report_71_1_11 a, " +
                                 ReportParams.Pref + DBManager.sKernelAliasRest + "supplier s, " +
                                 ReportParams.Pref + DBManager.sKernelAliasRest + "s_payer p, " +
                                 ReportParams.Pref + DBManager.sKernelAliasRest + "s_payer pp " +
                          " WHERE a.nzp_supp = s.nzp_supp " +
                            " AND s.nzp_payer_supp = p.nzp_payer " +
                            " AND s.nzp_payer_princip = pp.nzp_payer " +
                          " GROUP BY 1,2,3 ";
                    ExecSQL(sql);

                    ExecSQL(" DELETE FROM t_report_71_1_11 ");
                }
            }
            #endregion

            sql = " SELECT TRIM(point) AS point,TRIM(name_norm) AS name_norm, norm, mkd_c_calc_norm, mkd_c_calc_ipu, " +
                        " chs_c_calc_norm, chs_c_calc_ipu, mkd_gil_norm, chs_gil_norm, mkd_c_calc_year, chs_c_calc_year " +
                  " FROM tall_report_71_1_11 " +
                  " ORDER BY 1,2 ";

            var ds = new DataSet();
            DataTable dt = ExecSQLToTable(sql);
            dt.TableName = "Q_master";

            ds.Tables.Add(dt);

            return ds;
        }

        /// <summary>Ограничение по банкам данных</summary>
        private string GetWhereWp()
        {

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
            return whereWp;
        }

        /// <summary>
        /// Получает условия ограничения по поставщику
        /// </summary>
        /// <returns></returns>
        private string GetWhereSupp(string fieldPref)
        {
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
            return " and " + fieldPref + " in (select nzp_supp from " +
                   ReportParams.Pref + DBManager.sKernelAliasRest + "supplier " +
                   " where nzp_supp>0 " + whereSupp + ")";
        }

        protected override void CreateTempTable()
        {
            string sql = " CREATE TEMP TABLE t_report_71_1_11( " +
                         " nzp_dom INTEGER, " +
                         " nzp_kvar INTEGER, " +
                         " nzp_supp INTEGER, " +
                         " nzp_y INTEGER DEFAULT 0, " +
                         " is_mkd INTEGER DEFAULT 0, " +
                         " name_norm char(100), " +
                         " norm " + DBManager.sDecimalType + "(14,4), " +
                         " c_calc " + DBManager.sDecimalType + "(14,2), " +
                         " c_calc_year " + DBManager.sDecimalType + "(14,2), " +
                         " is_device INTEGER, " +
                         " gil INTEGER)" + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql = " CREATE TEMP TABLE tall_report_71_1_11( " +
                  " point char(100), " +
                  " name_norm char(100), " +
                  " norm  " + DBManager.sDecimalType + "(14,4), " +
                  " mkd_c_calc_norm  " + DBManager.sDecimalType + "(14,2), " +
                  " mkd_c_calc_ipu  " + DBManager.sDecimalType + "(14,2), " +
                  " chs_c_calc_norm  " + DBManager.sDecimalType + "(14,2), " +
                  " chs_c_calc_ipu  " + DBManager.sDecimalType + "(14,2), " +
                  " mkd_gil_norm  " + DBManager.sDecimalType + "(14,2), " +
                  " chs_gil_norm  " + DBManager.sDecimalType + "(14,2), " +
                  " mkd_c_calc_year " + DBManager.sDecimalType + "(14,2), " +
                  " chs_c_calc_year " + DBManager.sDecimalType + "(14,2)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql = " CREATE TEMP TABLE tyear_report_71_1_11( " +
                  " nzp_kvar INTEGER, " +
                  " nzp_supp INTEGER, " +
                  " nzp_serv INTEGER, " +
                  " c_calc " + DBManager.sDecimalType + "(14,2)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            ExecSQL(" CREATE INDEX ix_t_report_71_1_1101 ON t_report_71_1_11(nzp_kvar, nzp_supp) ");
            ExecSQL(" CREATE INDEX ix_t_report_71_1_1102 ON t_report_71_1_11(nzp_dom) ");
            ExecSQL(" CREATE INDEX ix_t_report_71_1_1103 ON t_report_71_1_11(nzp_kvar) ");

            ExecSQL(" CREATE INDEX ix_t_tyear_report_71_1_11 ON tyear_report_71_1_11(nzp_kvar, nzp_supp) ");
        }

        protected override void DropTempTable()
        {
            ExecSQL("DROP TABLE t_report_71_1_11 ");
            ExecSQL("DROP TABLE tall_report_71_1_11 ");
            ExecSQL("DROP TABLE tyear_report_71_1_11 ");
        }


    }
}
