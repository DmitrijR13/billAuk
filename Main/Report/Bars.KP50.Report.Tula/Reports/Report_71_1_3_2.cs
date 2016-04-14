using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.Tula.Properties;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.EPaspXsd;
using STCLINE.KP50.Global;
using Bars.KP50.Utils;
using Newtonsoft.Json;

namespace Bars.KP50.Report.Tula.Reports
{
    public class Report71132 : BaseSqlReport
    {
        public override string Name
        {
            get { return "71.1.3 Справка об образовавшейся задолженности "; }
        }

        public override string Description
        {
            get { return "Справка об образовавшейся задолженности ЖКУ за данный период, по заданному адресу для Тулы"; }
        }

        public override IList<ReportGroup> ReportGroups
        {
            get { return new List<ReportGroup> { ReportGroup.Cards }; }
        }

        public override bool IsPreview
        {
            get { return false; }
        }

        protected override byte[] Template
        {
            get { return Resources.Report_71_1_3_2; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.LC}; }}



        /// <summary>Расчетный месяц</summary>
        protected int MonthS { get; set; }

        /// <summary>Расчетный год</summary>
        protected int YearS { get; set; }  

        /// <summary>Расчетный месяц</summary>
        protected int MonthPo { get; set; }

        /// <summary>Расчетный год</summary>
        protected int YearPo { get; set; }

        /// <summary>Поставщики, Агенты, Принципалы  </summary>
        protected BankSupplierParameterValue BankSupplier { get; set; }

        /// <summary>Поставщики</summary>
        private string SupplierHeader { get; set; }

        /// <summary>Заголовок территории</summary>
        protected string TerritoryHeader { get; set; }
        /// <summary>Адрес</summary>
        protected string AddressHeader { get; set; }

        public override List<UserParam> GetUserParams()
        {
            var curCalcMonthYear = DBManager.GetCurMonthYear();
            return new List<UserParam>
            {
                new MonthParameter {Name = "Месяц с", Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["month_"] : DateTime.Today.Month },
				new YearParameter {Name = "Год с", Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["yearr"] : DateTime.Today.Year },
				new MonthParameter {Name = "Месяц по", Code = "Month1", Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["month_"] : DateTime.Today.Month },
				new YearParameter {Name = "Год по", Code = "Year1", Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["yearr"] : DateTime.Today.Year },
                new BankSupplierParameter()
            };
        }


        public override DataSet GetData()
        {

            #region Выборка по локальным банкам
            string whereSupp = GetWhereSupp("a.nzp_supp"); 
            MyDataReader reader;
            string sql;

            string kvar = ReportParams.Pref + DBManager.sDataAliasRest + "kvar ";
                    
            sql = " SELECT * " +
                         " FROM  " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point " +
                         " WHERE nzp_wp>1 " + GetwhereWp();
            ExecRead(out reader, sql);
                while (reader.Read())
                {
                    var pref = reader["bd_kernel"].ToString().ToLower().Trim();


                    for (int i = YearS * 12 + MonthS; i < YearPo * 12 + MonthPo + 1; i++)
                    {
                        var year = i/12;
                        var month = i%12;
                        if (month == 0)
                        {
                            year--;
                            month = 12;
                        }
                        string chargeXX = pref + "_charge_" + (year - 2000).ToString("00") +
                                          DBManager.tableDelimiter + "charge_" + month.ToString("00");

                        string tablePerekidka = pref + "_charge_" + (year - 2000).ToString("00") + DBManager.tableDelimiter +
                        "perekidka";

                        if (ReportParams.CurrentReportKind != ReportKind.ListLC && ReportParams.CurrentReportKind != ReportKind.LC)
                            kvar = pref + DBManager.sDataAliasRest + "kvar ";

                        if (TempTableInWebCashe(chargeXX))
                        {

                            sql = " insert into t_svod(pref, month_, year_, nzp_kvar, nzp_serv, nzp_supp, sum_insaldo, rsum_tarif, " +
                                  " sum_tarif, reval,  sum_money, sum_outsaldo)  " +
                                  " select '" + pref + "'," + month + "," + year +
                                  ", a.nzp_kvar,a.nzp_serv, a.nzp_supp, sum(sum_insaldo) , sum(rsum_tarif), " +
                                  "       sum(sum_tarif) as sum_tarif, " +
                                  "       sum(reval) as reval, " +
                                  "       sum(sum_money) as sum_money, " +
                                  "       sum(sum_outsaldo) as sum_outsaldo " +
                                  "       from " + chargeXX + " a " +
                                  " where nzp_serv>1 and dat_charge is null and a.nzp_kvar="+ReportParams.NzpObject + whereSupp +
                                  " GROUP BY  1,2,3,4,5,6  ";
                        ExecSQL(sql);

                            sql = " UPDATE t_svod " +
                                  " SET real_insaldo = ( SELECT SUM(sum_rcl)" +
                                  " FROM " + tablePerekidka + " p " +
                                  " WHERE p.type_rcl in ( 100,20) " +
                                  " AND t_svod.nzp_kvar = p.nzp_kvar " +
                                  " AND p.nzp_supp = t_svod.nzp_supp " +
                                  " AND p.nzp_serv = t_svod.nzp_serv " + 
                                  " AND p.month_ = " + month +
                                  ") " +
                                  " WHERE month_ = " + month + "" +
                                  " AND year_= " + year +
                                  " AND pref= '" + pref + "'";
                        ExecSQL(sql);
                            sql = " UPDATE t_svod " +
                                  " SET real_charge = ( SELECT SUM(sum_rcl)" +
                                  " FROM " + tablePerekidka + " p " +
                                  " WHERE p.type_rcl not in ( 100,20) " +
                                  " AND t_svod.nzp_kvar = p.nzp_kvar " +
                                  " AND p.nzp_supp = t_svod.nzp_supp " +
                                  " AND p.nzp_serv = t_svod.nzp_serv " + 
                                  " AND p.month_ = " + month +
                                  ") " +
                                  " WHERE month_ = " + month + "" +
                                  " AND year_= " + year +
                                  " AND pref= '" + pref + "'";
                        ExecSQL(sql);
                    }
                }

                }
            reader.Close();
            sql = " UPDATE t_svod " +
                  " SET real_charge = 0 " +
                  " WHERE real_charge is null ";
            ExecSQL(sql);

            sql = " UPDATE t_svod " +
                  " SET real_insaldo = 0 " +
                  " WHERE real_insaldo is null ";
            ExecSQL(sql);

            #endregion

            #region Выборка на экран

            sql = " SELECT MDY(month_,01,year_) as dat_month,  service, name_supp, a.nzp_serv," +
                  "        CASE WHEN ps.id>0 THEN 1 ELSE 0 END as nzp_peni, " +
                  "        sum(sum_insaldo) as sum_insaldo, " +
                  "        sum(sum_tarif) as sum_tarif, " +
                  "        sum(reval) as reval, " +
                  "        sum(sum_tarif+real_charge+reval) as sum_nach, " +
                  "        sum(real_charge) as real_charge, " +
                  "        sum(real_insaldo) as real_insaldo, " +
                  "        sum(sum_money) as sum_money, " +
                  "        sum(sum_outsaldo) as sum_outsaldo" +
                  " FROM t_svod a JOIN " +
                  ReportParams.Pref + DBManager.sKernelAliasRest + "supplier su ON a.nzp_supp=su.nzp_supp JOIN  " +
                  ReportParams.Pref + DBManager.sKernelAliasRest + "services s ON a.nzp_serv=s.nzp_serv LEFT JOIN " +
                  ReportParams.Pref + DBManager.sKernelAliasRest + "peni_settings ps ON s.nzp_serv=ps.nzp_serv " +
                  " GROUP BY 1,2,3,4,5 " +
                  " ORDER BY 1,3,2 ";
            DataTable dt = ExecSQLToTable(sql);
            dt.TableName = "Q_master";
            #endregion

            if (dt.Rows.Count > 65000 && ReportParams.ExportFormat == ExportFormat.Excel2007)
            {
                var dtr = dt.Rows.Cast<DataRow>().Skip(65000).ToArray();
                dtr.ForEach(dt.Rows.Remove);
            }

            var ds = new DataSet();
            ds.Tables.Add(dt);

            return ds;
        }

        private string GetwhereWp()
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
            if (!string.IsNullOrEmpty(whereWp))
            {
                TerritoryHeader = string.Empty;
                string sql = " SELECT point FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point WHERE nzp_wp > 0 " + whereWp;
                DataTable terrTable = ExecSQLToTable(sql);
                foreach (DataRow row in terrTable.Rows)
                {
                    TerritoryHeader += row["point"].ToString().Trim() + ", ";
                }
                TerritoryHeader = TerritoryHeader.TrimEnd(',', ' ');
            }
            return whereWp;
        }

        protected override void PrepareReport(FastReport.Report report)
        {
            var months = new[] {"","Январь","Февраль",
				 "Март","Апрель","Май","Июнь","Июль","Август","Сентябрь",
				 "Октябрь","Ноябрь","Декабрь"};
            report.SetParameterValue("month", months[MonthS]);
            report.SetParameterValue("year", YearS);
            if ((MonthS == MonthPo) & (YearS == YearPo))
            {
                report.SetParameterValue("period_month", months[MonthS] + " " + YearS);
            }
            else
            {
                report.SetParameterValue("period_month", "период с " + months[MonthS] + " " + YearS +
                                                         "г. по " + months[MonthPo] + " " + YearPo);
            }
            GetWhereAdr();
            AddressHeader = !string.IsNullOrEmpty(AddressHeader) ? "Адрес: " + AddressHeader : string.Empty; 
            report.SetParameterValue("address", AddressHeader);

            report.SetParameterValue("agent", ExecSQLToTable("select distinct val_prm from " +
                                                             ReportParams.Pref + DBManager.sDataAliasRest +
                                                             "prm_10 where nzp_prm = 80 ").Rows[0][0].ToString().Trim());     

            string area = !string.IsNullOrEmpty(TerritoryHeader) ? "Территория: " + TerritoryHeader : string.Empty;
            area += !string.IsNullOrEmpty(SupplierHeader) ? "\nПоставщики: " + SupplierHeader  : string.Empty;
            
            report.SetParameterValue("area", !string.IsNullOrEmpty(area) ? area : string.Empty);
        }

        protected override void PrepareParams()
        {
            MonthS = UserParamValues["Month"].GetValue<int>();
            YearS = UserParamValues["Year"].Value.To<int>();
            MonthPo = UserParamValues["Month1"].GetValue<int>();
            YearPo = UserParamValues["Year1"].Value.To<int>();
            BankSupplier = JsonConvert.DeserializeObject<BankSupplierParameterValue>(UserParamValues["BankSupplier"].Value.ToString());
        }

        protected override void CreateTempTable()
        {
            const string sql = " create temp table t_svod (     " +
                               " pref char(20), " +
                               " month_ integer, " +
                               " year_ integer, " +
                               " nzp_kvar integer, " +
                               " nzp_serv integer, " +
                               " nzp_supp integer, " +
                               " sum_insaldo " + DBManager.sDecimalType + "(14,2)  default 0, " +
                               " sum_tarif " + DBManager.sDecimalType + "(14,2)  default 0, " +
                               " rsum_tarif " + DBManager.sDecimalType + "(14,2)  default 0, " +
                               " reval " + DBManager.sDecimalType + "(14,2) default 0, " +
                               " sum_real " + DBManager.sDecimalType + "(14,2)  default 0, " +
                               " real_charge " + DBManager.sDecimalType + "(14,2)  default 0, " +
                               " real_insaldo " + DBManager.sDecimalType + "(14,2) default 0, " +
                               " sum_money " + DBManager.sDecimalType + "(14,2) default 0, " +
                               " sum_outsaldo " + DBManager.sDecimalType + "(14,2) default 0)  " +
                               DBManager.sUnlogTempTable;
            ExecSQL(sql);
            ExecSQL(" CREATE INDEX ix_t_svod ON t_svod(nzp_supp,nzp_serv,month_,year_) ");

        }

        private void GetWhereAdr()
        {

            string prefData = ReportParams.Pref + DBManager.sDataAliasRest;
            var sql = " SELECT TRIM(town) AS  town, TRIM(rajon) AS rajon, TRIM(" + DBManager.sNvlWord +
                      "(ulicareg,''))||' '||TRIM(ulica) AS ulica, TRIM(ndom) AS ndom, TRIM(nkor) AS nkor,k.nkvar, k.nkvar_n, k.num_ls, k.fio, idom, ikvar  " +
                      " FROM " + prefData + "dom d INNER JOIN " + prefData + "s_ulica u ON u.nzp_ul = d.nzp_ul " +
                      " INNER JOIN " + prefData + "s_rajon r ON r.nzp_raj = u.nzp_raj " +
                      " INNER JOIN " + prefData + "s_town t ON t.nzp_town = r.nzp_town" +
                      " INNER JOIN " + prefData + "kvar k ON k.nzp_dom = d.nzp_dom" +
                      " WHERE  k.nzp_kvar =" + ReportParams.NzpObject +
                      " ORDER BY town, rajon, ulica, idom, ikvar ";
                DataTable addressTable = ExecSQLToTable(sql);
                foreach (DataRow row in addressTable.Rows)
                {
                    AddressHeader += !string.IsNullOrEmpty(row["town"].ToString().Trim())
                        ? row["town"].ToString().Trim() + "/"
                        : ", -/";
                    AddressHeader += !string.IsNullOrEmpty(row["rajon"].ToString().Trim())
                        ? row["rajon"].ToString().Trim() + ", "
                        : "-,";
                    AddressHeader += !string.IsNullOrEmpty(row["ulica"].ToString().Trim())
                        ? row["ulica"].ToString().Trim() + ","
                        : string.Empty;
                    AddressHeader += !string.IsNullOrEmpty(row["ndom"].ToString().Trim())
                        ? row["ndom"].ToString().Trim() != "-"
                            ? " д. " + row["ndom"].ToString().Trim() + ","
                            : string.Empty
                        : string.Empty;
                    AddressHeader += !string.IsNullOrEmpty(row["nkor"].ToString().Trim())
                        ? row["nkor"].ToString().Trim() != "-"
                            ? " кор. " + row["nkor"].ToString().Trim() + ","
                            : string.Empty
                        : string.Empty;
                    AddressHeader += !String.IsNullOrEmpty(row["nkvar"].ToString().Trim())
                        ? " кв. " + row["nkvar"].ToString().Trim() + ","
                        : string.Empty;
                    AddressHeader += !string.IsNullOrEmpty(row["nkvar_n"].ToString().Trim())
                        ? row["nkvar_n"].ToString().Trim() != "-"
                            ? " ком. " + row["nkvar_n"].ToString().Trim()
                            : string.Empty
                        : string.Empty;
                    AddressHeader += !string.IsNullOrEmpty(row["num_ls"].ToString().Trim())
                        ? " ЛС " + row["num_ls"].ToString().Trim() + ","
                        : string.Empty;
                    AddressHeader += !string.IsNullOrEmpty(row["fio"].ToString().Trim())
                        ? row["fio"].ToString().Trim() + ","
                        : string.Empty;
                    AddressHeader = AddressHeader + '\n';
                    if (!string.IsNullOrEmpty(AddressHeader))
                    {
                        AddressHeader = AddressHeader.TrimEnd('\n');
                        AddressHeader = AddressHeader.TrimEnd(',');
                    }
                }  

        }

        protected override void DropTempTable()
        {
            ExecSQL(" drop table t_svod ", true);
        }

        private string GetWhereSupp(string fieldPref)
        {
            string whereSupp = String.Empty;
            if (BankSupplier != null)
            {
                if (BankSupplier.Suppliers != null)
                {

                    string supp = string.Empty;
                    supp = BankSupplier.Suppliers.Aggregate(supp, (current, nzpSupp) => current + (nzpSupp + ","));
                    whereSupp += " and nzp_payer_supp in (" + supp.TrimEnd(',') + ")";
                }

                if (BankSupplier.Principals != null)
                {

                    string supp = string.Empty;
                    supp = BankSupplier.Principals.Aggregate(supp, (current, nzpSupp) => current + (nzpSupp + ","));
                    whereSupp += " and nzp_payer_princip in (" + supp.TrimEnd(',') + ")";
                }

                if (BankSupplier.Agents != null)
                {

                    string supp = string.Empty;
                    supp = BankSupplier.Agents.Aggregate(supp, (current, nzpSupp) => current + (nzpSupp + ","));
                    whereSupp += " and nzp_payer_agent in (" + supp.TrimEnd(',') + ")";
                }
            }

            string oldsupp = ReportParams.GetRolesCondition(Constants.role_sql_supp);

            whereSupp = whereSupp.TrimEnd(',');

            if (!String.IsNullOrEmpty(whereSupp) || !String.IsNullOrEmpty(oldsupp))
            {
                if (!String.IsNullOrEmpty(oldsupp))
                    whereSupp += " AND nzp_supp in (" + oldsupp + ")";

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


            return String.IsNullOrEmpty(whereSupp) ? string.Empty : " and " + fieldPref + " in (select nzp_supp from " +
                   ReportParams.Pref + DBManager.sKernelAliasRest + "supplier " +
                   " where nzp_supp>0 " + whereSupp + ")";
        }

    }
}
