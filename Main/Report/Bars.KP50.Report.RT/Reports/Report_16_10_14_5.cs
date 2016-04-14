using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Bars.KP50.Report.Base;
using Bars.KP50.Utils;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using Bars.KP50.Report.RT.Properties;

namespace Bars.KP50.Report.RT.Reports
{
    class Report1610145 : BaseSqlReport
    {
        public override string Name
        {
            get { return "16.10.14.5 Сальдовая оборотная ведомасть начислений и оплат по услугам, арендаторы"; }
        }

        public override string Description
        {
            get { return "10.14.5 Сальдовая оборотная ведомасть начислений и оплат по услугам, арендаторы"; }
        }

        public override IList<ReportGroup> ReportGroups
        {
            get
            {
                List<ReportGroup> result = new List<ReportGroup> { ReportGroup.Reports };
                return result;
            }
        }

        public override bool IsPreview
        {
            get { return false; }
        }

        protected override byte[] Template
        {
            get { return Resources.Report_16_10_14_5; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base }; }
        }


        /// <summary>Расчетный месяц</summary>
        protected int Month { get; set; }

        /// <summary>Расчетный год</summary>
        protected int Year { get; set; }

        /// <summary>Районы</summary>
        protected List<int> Rajons { get; set; }

        /// <summary>Управляющие компании</summary>
        protected List<int> Areas { get; set; }

        /// <summary>Поставщики</summary>
        protected List<long> Suppliers { get; set; }

        /// <summary>Услуги</summary>
        protected List<int> Services { get; set; }

        /// <summary>Заголовок отчета</summary>
        protected string RajonHeader { get; set; }

        /// <summary>Заголовок отчета</summary>
        protected string AreaHeader { get; set; }

        /// <summary>Заголовок отчета</summary>
        protected string SupplierHeader { get; set; }


        public override List<UserParam> GetUserParams()
        {
            var curCalcMonthYear = DBManager.GetCurMonthYear();
            return new List<UserParam>
            {
                new MonthParameter {Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["month_"] : DateTime.Today.Month },
                new YearParameter {Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["yearr"] : DateTime.Today.Year },
                new RaionsParameter(),
                new AreaParameter(),
                new SupplierParameter(),
                new ServiceParameter()
            };
        }

        protected override void PrepareParams()
        {
            Month = UserParamValues["Month"].GetValue<int>();
            Year = UserParamValues["Year"].Value.To<int>();
            
            Areas = UserParamValues["Areas"].Value.To<List<int>>();
            Suppliers = UserParamValues["Suppliers"].Value.To<List<long>>();
            Services = UserParamValues["Services"].Value.To<List<int>>();
        }

        protected override void PrepareReport(FastReport.Report report)
        {
            const bool isShow = false;
            var months = new[]{
                                "", "Январь", "Февраль",
                                "Март", "Апрель", "Май", "Июнь", "Июль", "Август", "Сентябрь",
                                "Октябрь", "Ноябрь", "Декабрь"
                              };
            var monthsP = new[]{
                                "", "Январе", "Феврале",
                                "Марте", "Апреле", "Мае", "Июне", "Июле", "Августе", "Сентябре",
                                "Октябре", "Ноябре", "Декабре"
                              };
            int yearP = Month == 1 ? Year - 1 : Year,
                monthP = Month == 1 ? 12 : Month - 1;
            SupplierHeader = SupplierHeader != null ? "Поставщик: " + SupplierHeader : SupplierHeader;
            AreaHeader = AreaHeader != null && SupplierHeader != null ? "Балансодержатель: " + AreaHeader + "\n"
                                                                      : AreaHeader != null ? "Балансодержатель: " + AreaHeader 
                                                                                           : AreaHeader;
            report.SetParameterValue("supplier", SupplierHeader);
            report.SetParameterValue("area", AreaHeader);

            report.SetParameterValue("period", "за " + months[Month] + " " + Year + " г.");
            report.SetParameterValue("periodvz", "в " + monthsP[Month] + " " + Year + " г. за " + months[monthP] + " " + yearP + " г.");

            report.SetParameterValue("invisible_footer", isShow);
            report.SetParameterValue("DATE", DateTime.Now.ToShortDateString());
            report.SetParameterValue("TIME", DateTime.Now.ToLongTimeString());
        }

        public override DataSet GetData()
        {
            IDataReader reader;

            #region выборка в temp таблицу
            string sql = " SELECT bd_kernel as pref " +
                         " FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point " +
                         " WHERE nzp_wp>1 " + GetWhereWp();

            ExecRead(out reader, sql);
            while (reader.Read())
            {
                string pref = reader["pref"].ToStr().Trim();

                string chargeTable = pref + "_charge_" + (Year - 2000).ToString("00") + DBManager.tableDelimiter + "charge_" + Month.ToString("00");
                string prefData = pref + DBManager.sDataAliasRest;

                if (TempTableInWebCashe(chargeTable))
                {
                    
                   sql =" INSERT INTO t_sald_obor_vedom (nzp_area, nzp_serv, nzp_supp, sum_insaldo, sum_potarif, rsum_lgota, sum_real, sum_chrev, " +
                                                         " sum_money, money_from, money_del, sum_outsaldo, sum_nedop, sum_nedop_p) " +
                        " SELECT nzp_area, " +
                               " (CASE WHEN nzp_serv=210 THEN 25 ELSE nzp_serv END) AS nzp_serv , " +
                               " nzp_supp, " +
                               " SUM(sum_insaldo) AS sum_insaldo, " +
                               " SUM(sum_tarif + sum_nedop + sum_lgota - rsum_lgota) AS sum_potarif, " +
                               " SUM(rsum_lgota) AS rsum_lgota, " +
                               " SUM(sum_real) AS sum_real, " +
                               " SUM(" + DBManager.sNvlWord + "(real_charge,0) + " + DBManager.sNvlWord + "(reval,0)) AS sum_chrev, " +
                               " SUM(sum_money) AS sum_money, " +
                               " SUM(money_from) AS money_from, " +
                               " SUM(money_del) AS money_del, " +
                               " SUM(sum_outsaldo) AS sum_outsaldo, " +
                               " SUM(sum_nedop + sum_lgota - rsum_lgota) AS sum_nedop, " +
                               " SUM(0) AS sum_nedop_p " +
                        " FROM " + chargeTable + " a, " +
                                   prefData + "kvar b " +
                        " WHERE nzp_serv>1 " +
                          " AND typek=3 " +
                          " AND dat_charge IS NULL " +
                          " AND a.nzp_kvar=b.nzp_kvar  " +
                          " AND nzp_dom in (SELECT nzp_dom " +
                                            " FROM " + prefData + "dom d " +
                                            " WHERE b.nzp_dom=d.nzp_dom " + GetWhereRaj() + ") " +
                            GetWhereSupp() + GetWhereArea() + GetWhereServ() +
                        " GROUP BY 1,2,3 ";
                    ExecSQL(sql);

                    sql = " INSERT INTO t_sald_obor_vedom (nzp_area,nzp_supp,nzp_serv,sum_nedop_p) " +
                          " SELECT nzp_area, " +
                                 " c.nzp_supp, " +
                                 " c.nzp_serv, " +
                                 " SUM(sum_nedop) - SUM(sum_nedop_p) AS sum_nedop_p " +
                          " FROM " + chargeTable + " c, " +
                                     prefData + "kvar k " +
                          " WHERE typek=3 " +
                            " AND c.dat_charge IS NULL " +
                            " AND c.nzp_kvar=k.nzp_kvar " +
                            " AND k.nzp_dom in (SELECT nzp_dom " +
                                            " FROM " + prefData + "dom d " +
                                            " WHERE k.nzp_dom=d.nzp_dom " + GetWhereRaj() + ") " +
                              GetWhereSupp() + GetWhereArea() + GetWhereServ() +
                          " GROUP BY 1,2,3 ";
                    ExecSQL(sql);
                }
            }
                reader.Close();
            #endregion

                sql = " INSERT INTO t_sald_obor_vedom_all (nzp_area, nzp_serv, nzp_supp, sum_insaldo, sum_potarif, rsum_lgota, sum_real, " +
                                                        " sum_chrev, sum_money, money_from, money_del, sum_outsaldo, sum_nedop, sum_nedop_p )" +
                     " SELECT nzp_area, " +
                            " nzp_serv, " +
                            " nzp_supp, " +
                            " SUM(sum_insaldo) AS sum_insaldo, " +
                            " SUM(sum_potarif) AS sum_potarif, " +
                            " SUM(rsum_lgota) AS rsum_lgota, " +
                            " SUM(sum_real) AS sum_real, " +
                            " SUM(sum_chrev) AS sum_chrev, " +
                            " SUM(sum_money) AS sum_money, " +
                            " SUM(money_from) AS money_from, " +
                            " SUM(money_del) AS money_del, " +
                            " SUM(sum_outsaldo) AS sum_outsaldo, " +
                            " SUM(sum_nedop) AS sum_nedop, " +
                            " SUM(sum_nedop_p) AS sum_nedop_p " +
                     " FROM t_sald_obor_vedom " +
                     " GROUP BY 1,2,3 ";
                ExecSQL(sql);

                sql = " SELECT area," +
                             " service, " +
                             " SUM(sum_insaldo) AS sum_insaldo, " +
                             " SUM(sum_potarif) AS sum_potarif, " +
                             " SUM(rsum_lgota) AS rsum_lgota, " +
                             " SUM(sum_real) AS sum_real, " +
                             " SUM(sum_chrev) AS sum_chrev, " +
                             " SUM(sum_money) AS sum_money, " +
                             " SUM(money_from) AS money_from, " +
                             " SUM(money_del) AS money_del,  " +
                             " SUM(sum_outsaldo) AS sum_outsaldo, " +
                             " SUM(sum_nedop) AS sum_nedop," +
                             " SUM(sum_nedop_p) AS sum_nedop_p " +
                      " FROM t_sald_obor_vedom_all a " +
                           " LEFT OUTER JOIN " + ReportParams.Pref + DBManager.sKernelAliasRest + "services c ON (a.nzp_serv=c.nzp_serv) " +
                           " LEFT OUTER JOIN " + ReportParams.Pref + DBManager.sDataAliasRest + "s_area sa ON (a.nzp_area=sa.nzp_area) " +
                      " WHERE ABS(" + DBManager.sNvlWord + "(sum_insaldo,0))+" +
                             " ABS(" + DBManager.sNvlWord + "(sum_potarif,0))+" +
                              " ABS(" + DBManager.sNvlWord + "(rsum_lgota,0))+" +
                               " ABS(" + DBManager.sNvlWord + "(sum_real,0))+" +
                                " ABS(" + DBManager.sNvlWord + "(sum_chrev,0))+" +
                                 " ABS(" + DBManager.sNvlWord + "(sum_money,0))+" +
                                  " ABS(" + DBManager.sNvlWord + "(money_from,0))+" +
                                   " ABS(" + DBManager.sNvlWord + "(money_del,0))+" +
                                    " ABS(" + DBManager.sNvlWord + "(sum_outsaldo,0))+ " +
                                     " ABS(" + DBManager.sNvlWord + "(sum_nedop,0))+" +
                                      " ABS(" + DBManager.sNvlWord + "(sum_nedop_p,0))>0 " +
                      " GROUP BY 1,2 " +
                      " ORDER BY 1,2";

                DataTable dt = ExecSQLToTable(sql);
                dt.TableName = "Q_master";
                var ds = new DataSet();
                ds.Tables.Add(dt);
                return ds;
            
        }

        /// <summary>
        /// Получить условия органичения по локальному банку
        /// </summary>
        private string GetWhereWp()
        {
            var result = ReportParams.GetRolesCondition(Constants.role_sql_wp);
            return !String.IsNullOrEmpty(result) ? " AND nzp_wp IN (" + result + ") " : "";
        }

        /// <summary>
        /// Получить условия органичения по районам
        /// </summary>
        private string GetWhereRaj()
        {
            var result = String.Empty;
            if (Rajons != null)
            {
                result = Rajons.Aggregate(result, (current, nzpRaj) => current + (nzpRaj + ","));
            }
            result = result.TrimEnd(',');

            if (!String.IsNullOrEmpty(result))
            {
                RajonHeader = String.Empty;
                var sql =
                    " select trim(t.town) || '/' || trim(u.rajon) as rajon, u.nzp_raj " +
                    " from " + ReportParams.Pref + DBManager.sDataAliasRest + "s_rajon u, " +
                    ReportParams.Pref + DBManager.sDataAliasRest + "s_town t " +
                    " where t.nzp_town = u.nzp_town   " +
                    " AND u.nzp_raj in (" + result + ")" +
                    " group by 1,2 " +
                    " order by 1,2";
                DataTable raj = ExecSQLToTable(sql);
                foreach (DataRow dr in raj.Rows)
                {
                    RajonHeader += dr["rajon"].ToString().Trim() + ",";
                }
                RajonHeader = RajonHeader.TrimEnd(',');
                result = " AND nzp_raj in (" + result + ")" ;
            }
            return result;
        }

        /// <summary>
        /// Получить условия органичения по поставщикам
        /// </summary>
        private string GetWhereSupp()
        {
            string whereSupp = String.Empty;

            if (Suppliers != null)
            {
                whereSupp = Suppliers.Aggregate(whereSupp, (current, nzpSupp) => current + (nzpSupp + ","));
            }
            else
            {
                whereSupp = ReportParams.GetRolesCondition(Constants.role_sql_supp);
            }

            whereSupp = whereSupp.TrimEnd(',');
            if (!String.IsNullOrEmpty(whereSupp))
            {
                whereSupp = " AND nzp_supp in (" + whereSupp + ")";
                SupplierHeader = String.Empty;

                string sql = " SELECT name_supp from " +
                             ReportParams.Pref + DBManager.sKernelAliasRest + "supplier" +
                             " WHERE nzp_supp > 0 " + whereSupp;
                DataTable supp = ExecSQLToTable(sql);
                foreach (DataRow dr in supp.Rows)
                {
                    SupplierHeader += dr["name_supp"].ToString().Trim() + ",";
                }
                SupplierHeader = SupplierHeader.TrimEnd(',');
            }
            return whereSupp;
        }

        /// <summary>
        /// Получить условия органичения по УК
        /// </summary>
        private string GetWhereArea()
        {
            string whereArea = String.Empty;
            if (Areas != null)
            {
                whereArea = Areas.Aggregate(whereArea, (current, nzpArea) => current + (nzpArea + ","));
            }
            else
            {
                whereArea = ReportParams.GetRolesCondition(Constants.role_sql_area);
            }

            whereArea = whereArea.TrimEnd(',');
            if (!String.IsNullOrEmpty(whereArea))
            {
                whereArea = " AND nzp_area in (" + whereArea + ")";
                AreaHeader = String.Empty;

                string sql = " SELECT area from " +
                             ReportParams.Pref + DBManager.sDataAliasRest + "s_area " +
                             " WHERE nzp_area > 0 " + whereArea;
                DataTable area = ExecSQLToTable(sql);
                foreach (DataRow dr in area.Rows)
                {
                    AreaHeader += dr["area"].ToString().Trim() + ",";
                }
                AreaHeader = AreaHeader.TrimEnd(',');
            }
            return whereArea;
        }

        /// <summary>
        /// Получить условия органичения по услугам
        /// </summary>
        private string GetWhereServ()
        {
            var result = String.Empty;
            if (Services != null)
            {
                result = Services.Aggregate(result, (current, nzpServ) => current + (nzpServ + ","));
            }
            else
            {
                result = ReportParams.GetRolesCondition(Constants.role_sql_serv);
            }
            result = result.TrimEnd(',');
            result = !String.IsNullOrEmpty(result) ? " AND nzp_serv in (" + result + ")" : String.Empty;
            return result;
        }

        protected override void CreateTempTable()
        {
            string sql = " CREATE TEMP TABLE t_sald_obor_vedom  ( " +
                            " nzp_area INTEGER, " +
                            " nzp_serv INTEGER, " +
                            " nzp_supp INTEGER, " +
                            " sum_insaldo " + DBManager.sDecimalType + "(14,2), " +
                            " sum_potarif " + DBManager.sDecimalType + "(14,2), " +
                            " rsum_lgota " + DBManager.sDecimalType + "(14,2), " +
                            " sum_real " + DBManager.sDecimalType + "(14,2), " +
                            " sum_chrev " + DBManager.sDecimalType + "(14,2), " +
                            " sum_money " + DBManager.sDecimalType + "(14,2)," +
                            " money_from " + DBManager.sDecimalType + "(14,2)," +
                            " money_del " + DBManager.sDecimalType + "(14,2), " +
                            " sum_outsaldo " + DBManager.sDecimalType + "(14,2)," +
                            " sum_nedop " + DBManager.sDecimalType + "(14,2)," +
                            " sum_nedop_p " + DBManager.sDecimalType + "(14,2)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql = " CREATE TEMP TABLE t_sald_obor_vedom_all  ( " +
                        " nzp_area INTEGER, " +
                        " nzp_serv INTEGER, " +
                        " nzp_supp INTEGER, " +
                        " sum_insaldo " + DBManager.sDecimalType + "(14,2), " +
                        " sum_potarif " + DBManager.sDecimalType + "(14,2), " +
                        " rsum_lgota " + DBManager.sDecimalType + "(14,2), " +
                        " sum_real " + DBManager.sDecimalType + "(14,2), " +
                        " sum_chrev " + DBManager.sDecimalType + "(14,2), " +
                        " sum_money " + DBManager.sDecimalType + "(14,2), " +
                        " money_from " + DBManager.sDecimalType + "(14,2), " +
                        " money_del " + DBManager.sDecimalType + "(14,2), " +
                        " sum_outsaldo " + DBManager.sDecimalType + "(14,2), " +
                        " sum_nedop " + DBManager.sDecimalType + "(14,2), " +
                        " sum_nedop_p " + DBManager.sDecimalType + "(14,2)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        protected override void DropTempTable()
        {
            ExecSQL(" DROP TABLE t_sald_obor_vedom ");
            ExecSQL(" DROP TABLE t_sald_obor_vedom_all ");
        }
    }
}
