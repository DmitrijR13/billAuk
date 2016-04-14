using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.Tula.Properties;
using Bars.KP50.Utils;
using Newtonsoft.Json;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;

namespace Bars.KP50.Report.Tula.Reports
{
    public class Report71510 : BaseSqlReport
    {
        public override string Name
        {
            get { return "71.5.10 Сальдовая ведомость"; }
        }

        public override string Description
        {
            get { return "Сальдовая ведомость для Тулы"; }
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
            get { return Resources.Report_71_5_10; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base }; }
        }


        /// <summary>Расчетный месяц</summary>
        protected int Month { get; set; }

        /// <summary>Расчетный год</summary>
        protected int Year { get; set; }


        /// <summary>Услуги</summary>
        protected List<int> Services { get; set; }

        /// <summary>Управляющие компании</summary>
        protected List<int> Areas { get; set; }
        
        /// <summary>Заголовок отчета</summary>
        protected string SupplierHeader { get; set; }

        /// <summary>Заголовок услуг</summary>
        protected string ServiceHeader { get; set; }

        /// <summary>Заголовок отчета</summary>
        protected string AreaHeader { get; set; }

        /// <summary>Заголовок территории</summary>
        protected string TerritoryHeader { get; set; }

        /// <summary>Поставщики, Агенты, Принципалы  </summary>
        protected BankSupplierParameterValue BankSupplier { get; set; }


        public override List<UserParam> GetUserParams()
        {
            var curCalcMonthYear = DBManager.GetCurMonthYear();
            return new List<UserParam>
            {
                new MonthParameter {Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["month_"] : DateTime.Today.Month },
                new YearParameter {Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["yearr"] : DateTime.Today.Year },
                new AreaParameter(),
                new BankSupplierParameter(),
                new ServiceParameter()
            };
        }

        public override DataSet GetData()
        {
            MyDataReader reader;

            //WhereStringForFindCommon(finder, "a", ref whereStr);

            string whereSupp = GetWhereSupp("nzp_supp");
            string whereServ = GetWhereServ();
            string whereArea = GetWhereArea();

            string sql = " select point AS bank_name,bd_kernel as pref " +
                  " FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point " +
                  " where nzp_wp>1 " + GetwhereWp();

            ExecRead(out reader, sql);


            while (reader.Read())
            {
                if (reader["pref"] == null) continue;

                string pref = reader["pref"].ToStr().Trim(),
                        bankName = reader["bank_name"].ToStr().Trim();
                    string tableCharge = pref + "_charge_" + (Year - 2000).ToString("00") +
                                         DBManager.tableDelimiter + "charge_" +
                                         Month.ToString("00");
                    string prevMonth;
                    int prevYear;
                    if (Month == 01) { prevMonth = "12"; prevYear = Year - 1; } 
                    else { prevMonth = (Month-1).ToString("00"); prevYear = Year; }

                    string tableSupFn = pref + "_charge_" + (Year - 2000).ToString("00") +
                          DBManager.tableDelimiter + "fn_supplier" +
                          Month.ToString("00");
                    string tableSupTo = pref + "_charge_" + (Year - 2000).ToString("00") +
                          DBManager.tableDelimiter + "to_supplier" +
                          Month.ToString("00");
                    string tableSupFnPrev = pref + "_charge_" + (prevYear - 2000).ToString("00") +
                          DBManager.tableDelimiter + "fn_supplier" +
                          prevMonth;
                    string tableSupToPrev = pref + "_charge_" + (prevYear - 2000).ToString("00") +
                          DBManager.tableDelimiter + "to_supplier" +
                          prevMonth;



                    if (TempTableInWebCashe(tableCharge))
                    {
                        //sql = " create temp table t_kdu( " +
                        //      " nzp_kvar integer," +
                        //      " nzp_dom integer," +
                        //      " nzp_ul integer," +
                        //      " nzp_area integer," +
                        //      " nzp_raj integer," +
                        //      " num_ls integer)";
                        //ExecSQL(sql);

                        //sql = " insert into t_kdu " +
                        //      " select k.nzp_kvar, d.nzp_dom, u.nzp_ul, k.nzp_area, u.nzp_raj, k.num_ls " +
                        //      " from " + pref + DBManager.sDataAliasRest + "kvar k, " +
                        //      pref + DBManager.sDataAliasRest + "dom d, " +
                        //      pref + DBManager.sDataAliasRest + "s_ulica u " +
                        //      " where k.nzp_dom = d.nzp_dom and d.nzp_ul = u.nzp_ul ";
                        //ExecSQL(sql);
                        sql = " insert into t_svod(pref, nzp_serv, sum_insaldo_k, sum_insaldo_d, " +
                              " sum_insaldo, sum_real, reval, real_charge, sum_money, " +
                              " sum_outsaldo_k, sum_outsaldo_d, sum_outsaldo)" +
                              " select '" + bankName + "' AS pref, c.nzp_serv, " +
                              " sum(case when sum_insaldo<0 then sum_insaldo else 0 end) as sum_insaldo_k, " +
                              " sum(case when sum_insaldo<0 then 0 else sum_insaldo end) as sum_insaldo_d, " +
                              " sum(sum_insaldo) as sum_insaldo, " +
                              " sum(sum_real) as sum_real, " +
                              " sum(reval) as reval, " +
                              " sum(real_charge) as real_charge, " +
                              " sum(sum_money) as sum_money, " +
                              " sum(case when sum_outsaldo<0 then sum_outsaldo else 0 end) as sum_outsaldo_k, " +
                              " sum(case when sum_outsaldo<0 then 0 else sum_outsaldo end) as sum_outsaldo_d, " +
                              " sum(sum_outsaldo) as sum_outsaldo " +
                              " from " + pref + DBManager.sDataAliasRest + "kvar " + " t, " + tableCharge + " c " +
                              " where c.nzp_kvar=t.nzp_kvar and  c.nzp_serv >1 and c.dat_charge is null " +
                                whereArea + whereServ + whereSupp +
                              " group by 1,2";
                        ExecSQL(sql);
                        sql = " insert into t_svod (pref, nzp_serv, sum_money_now) " +
                              " select '" + bankName + "' AS pref, fns.nzp_serv,  sum(fns.sum_prih)  " +
                              "from " + pref + DBManager.sDataAliasRest + "kvar " + " t, " + tableSupFn + " fns  " +
                              " where 0 =(select count(*) from " + tableSupTo + " tos " +
                              " where fns.nzp_pack_ls = tos.nzp_pack_ls) " +
                              " and fns.num_ls = t.num_ls " + whereArea +
                               whereServ +  whereSupp + " and abs(fns.sum_prih)>0.001 "+
                              " group by 1, 2";
                        ExecSQL(sql);
                        sql = " insert into t_svod (pref, nzp_serv, sum_money_prev) " +
                              " select '" + bankName + "' AS pref, fns.nzp_serv,  sum(fns.sum_prih)  " +
                              " from " + pref + DBManager.sDataAliasRest + "kvar " + " t, " + tableSupFnPrev + " fns  " +
                              " where 0 =(select count(*) from " + tableSupToPrev + " tos " +
                              " where fns.nzp_pack_ls = tos.nzp_pack_ls) " +
                              " and fns.num_ls = t.num_ls " + whereArea +
                               whereServ + whereSupp + " and abs(fns.sum_prih)>0.001 "+
                              " group by 1,2 ";
                        ExecSQL(sql);
                        ExecSQL("drop table t_kdu");
                    }
            }

            reader.Close();


            sql = " select service , sum(sum_insaldo_k) as sum_insaldo_k," +
                  " sum(sum_insaldo_d) as sum_insaldo_d," +
                  " sum(sum_insaldo) as sum_insaldo," +
                  " sum(sum_real) as sum_real," +
                  " sum(reval) as reval," +
                  " sum(real_charge) as real_charge," +
                  " sum(reval) + sum(real_charge) as reval_charge," +
                  " sum(sum_money) as sum_money," +
                  " sum(sum_money_prev) as sum_money_prev," +
                  " sum(sum_money_now) as sum_money_now," +
                  " sum(sum_outsaldo_k) as sum_outsaldo_k," +
                  " sum(sum_outsaldo_d) as sum_outsaldo_d," +
                  " sum(sum_outsaldo) as sum_outsaldo" +
                  " from t_svod a, " +
                  DBManager.GetFullBaseName(Connection) + DBManager.tableDelimiter + "services s " +
                  " where a.nzp_serv=s.nzp_serv " +
                  " and abs("+DBManager.sNvlWord+"(sum_insaldo,0))+ "+
                  "  abs(" + DBManager.sNvlWord + "(sum_real,0))+ " +
                  "  abs(" + DBManager.sNvlWord + "(real_charge,0))+ " +
                  "  abs(" + DBManager.sNvlWord + "(reval,0))+ " +
                  "  abs(" + DBManager.sNvlWord + "(sum_money,0))+ " +
                  "  abs(" + DBManager.sNvlWord + "(sum_money_prev,0))+ " +
                  "  abs(" + DBManager.sNvlWord + "(sum_money_now,0))+ " +
                  "  abs(" + DBManager.sNvlWord + "(sum_outsaldo,0))>0.001 " +
                  " group by service " +
                  " order by service ";
            DataTable dt = ExecSQLToTable(sql);
            dt.TableName = "Q_master";
            if (dt.Rows.Count > 65000 && ReportParams.ExportFormat == ExportFormat.Excel2007)
            {
                var dtr = dt.Rows.Cast<DataRow>().Skip(65000).ToArray();
                dtr.ForEach(dt.Rows.Remove);
            }

            sql = " select pref, service , sum(sum_insaldo_k) as sum_insaldo_k," +
                  " sum(sum_insaldo_d) as sum_insaldo_d," +
                  " sum(sum_insaldo) as sum_insaldo," +
                  " sum(sum_real) as sum_real," +
                  " sum(reval) as reval," +
                  " sum(real_charge) as real_charge," +
                  " sum(reval) + sum(real_charge) as reval_charge," +
                  " sum(sum_money) as sum_money," +
                  " sum(sum_money_prev) as sum_money_prev," +
                  " sum(sum_money_now) as sum_money_now," +
                  " sum(sum_outsaldo_k) as sum_outsaldo_k," +
                  " sum(sum_outsaldo_d) as sum_outsaldo_d," +
                  " sum(sum_outsaldo) as sum_outsaldo" +
                  " from t_svod a, " +
                  DBManager.GetFullBaseName(Connection) + DBManager.tableDelimiter + "services s " +
                  " where a.nzp_serv=s.nzp_serv " +
                  " and abs(" + DBManager.sNvlWord + "(sum_insaldo,0))+ " +
                  "  abs(" + DBManager.sNvlWord + "(sum_real,0))+ " +
                  "  abs(" + DBManager.sNvlWord + "(real_charge,0))+ " +
                  "  abs(" + DBManager.sNvlWord + "(reval,0))+ " +
                  "  abs(" + DBManager.sNvlWord + "(sum_money,0))+ " +
                  "  abs(" + DBManager.sNvlWord + "(sum_money_prev,0))+ " +
                  "  abs(" + DBManager.sNvlWord + "(sum_money_now,0))+ " +
                  "  abs(" + DBManager.sNvlWord + "(sum_outsaldo,0))>0.001 " +
                  " group by pref, service " +
                  " order by pref, service ";
            DataTable dt1 = ExecSQLToTable(sql);
            dt1.TableName = "Q_master1";
            if (dt1.Rows.Count > 65000 && ReportParams.ExportFormat == ExportFormat.Excel2007)
            {
                var dtr = dt1.Rows.Cast<DataRow>().Skip(65000).ToArray();
                dtr.ForEach(dt1.Rows.Remove);
            }
            var ds = new DataSet();
            ds.Tables.Add(dt);
            ds.Tables.Add(dt1);

            return ds;
        }

        /// <summary>
        /// Получить условия органичения по УК
        /// </summary>
        /// <returns></returns>
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
            whereArea = !String.IsNullOrEmpty(whereArea) ? " AND t.nzp_area in (" + whereArea + ")" : String.Empty;
            if (!String.IsNullOrEmpty(whereArea))
            {
                AreaHeader = String.Empty;
                string sql = " SELECT area from " + ReportParams.Pref + DBManager.sDataAliasRest + "s_area t  WHERE t.nzp_area > 0 " + whereArea;
                DataTable area = ExecSQLToTable(sql);
                foreach (DataRow dr in area.Rows)
                {
                    AreaHeader += dr["area"].ToString().Trim() + ", ";
                }
                AreaHeader = AreaHeader.TrimEnd(',', ' ');
            }
            return whereArea;
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
                //whereSupp = Suppliers.Aggregate(whereSupp, (current, nzpSupp) => current + (nzpSupp + ","));
                whereSupp += " and nzp_payer_supp in (" + supp.TrimEnd(',') + ")";
            }

            if (BankSupplier != null && BankSupplier.Principals != null)
            {

                string supp = string.Empty;
                supp = BankSupplier.Principals.Aggregate(supp, (current, nzpSupp) => current + (nzpSupp + ","));
                //whereSupp = Suppliers.Aggregate(whereSupp, (current, nzpSupp) => current + (nzpSupp + ","));
                whereSupp += " and nzp_payer_princip in (" + supp.TrimEnd(',') + ")";
            }
            if (BankSupplier != null && BankSupplier.Agents != null)
            {

                string supp = string.Empty;
                supp = BankSupplier.Agents.Aggregate(supp, (current, nzpSupp) => current + (nzpSupp + ","));
                //whereSupp = Suppliers.Aggregate(whereSupp, (current, nzpSupp) => current + (nzpSupp + ","));
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
                SupplierHeader = SupplierHeader.TrimEnd(',',' ');
            }
            return " and " + fieldPref + " in (select nzp_supp from " +
                   ReportParams.Pref + DBManager.sKernelAliasRest + "supplier " +
                   " where nzp_supp>0 " + whereSupp + ")";
        }

        /// <summary>
        /// Получить условия органичения по услугам
        /// </summary>
        /// <returns></returns>
        private string GetWhereServ()
        {
            string whereServ = String.Empty;
            if (Services != null)
            {
                whereServ = Services.Aggregate(whereServ, (current, nzpServ) => current + (nzpServ + ","));
            }
            else
            {
                whereServ = ReportParams.GetRolesCondition(Constants.role_sql_serv);
            }
            whereServ = whereServ.TrimEnd(',');
            whereServ = !String.IsNullOrEmpty(whereServ) ? " AND nzp_serv in (" + whereServ + ")" : String.Empty;

            if (!string.IsNullOrEmpty(whereServ) && string.IsNullOrEmpty(ServiceHeader))
            {
                ServiceHeader = String.Empty;
                string sql = " SELECT service FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "services WHERE nzp_serv > 0 " + whereServ;
                DataTable servTable = ExecSQLToTable(sql);
                foreach (DataRow row in servTable.Rows)
                {
                    ServiceHeader += row["service"].ToString().Trim() + ", ";
                }
                ServiceHeader = ServiceHeader.TrimEnd(',', ' ');
            }

            return whereServ;
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
                TerritoryHeader = String.Empty;
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
            report.SetParameterValue("now_month", months[Month]);
            if (Month == 1)
            {
                report.SetParameterValue("prev_month", months[12]);
            }
            else
            {
                report.SetParameterValue("prev_month", months[Month - 1]);
            }
            report.SetParameterValue("year", Year);
            report.SetParameterValue("dat", DateTime.Now.ToShortDateString());
            report.SetParameterValue("time", DateTime.Now.ToShortTimeString());

            string headerParam = !string.IsNullOrEmpty(TerritoryHeader) ? "Территория: " + TerritoryHeader + "\n" : string.Empty;
            headerParam += !string.IsNullOrEmpty(SupplierHeader) ? "Поставщики: " + SupplierHeader + "\n" : string.Empty;
            headerParam += !string.IsNullOrEmpty(AreaHeader) ? "Балансодержатель: " + AreaHeader + "\n" : string.Empty;
            headerParam += !string.IsNullOrEmpty(ServiceHeader) ? "Услуги: " + ServiceHeader : string.Empty;
            headerParam = headerParam.TrimEnd('\n');
            report.SetParameterValue("headerParam", headerParam);
        }

        protected override void PrepareParams()
        {
            Month = UserParamValues["Month"].GetValue<int>();
            Year = UserParamValues["Year"].Value.To<int>();

            Services = UserParamValues["Services"].Value.To<List<int>>();
            Areas = UserParamValues["Areas"].Value.To<List<int>>();

            BankSupplier = JsonConvert.DeserializeObject<BankSupplierParameterValue>(UserParamValues["BankSupplier"].Value.ToString());

        }

        protected override void CreateTempTable()
        {
            string sql = " create temp table t_svod( " +
                               " pref CHARACTER(100), " +
                               " nzp_serv integer," +
                               " sum_insaldo " + DBManager.sDecimalType + "(14,2)," +
                               " sum_insaldo_k " + DBManager.sDecimalType + "(14,2)," +
                               " sum_insaldo_d " + DBManager.sDecimalType + "(14,2)," +
                               " sum_real " + DBManager.sDecimalType + "(14,2)," +
                               " reval " + DBManager.sDecimalType + "(14,2)," +
                               " reval_charge " + DBManager.sDecimalType + "(14,2)," +
                               " real_charge " + DBManager.sDecimalType + "(14,2)," +
                               " sum_outsaldo " + DBManager.sDecimalType + "(14,2)," +
                               " sum_outsaldo_k " + DBManager.sDecimalType + "(14,2)," +
                               " sum_outsaldo_d " + DBManager.sDecimalType + "(14,2)," +
                               " sum_money_prev " + DBManager.sDecimalType + "(14,2)," +
                               " sum_money_now " + DBManager.sDecimalType + "(14,2)," +
                               " sum_money " + DBManager.sDecimalType + "(14,2))";
            ExecSQL(sql);

        }

        protected override void DropTempTable()
        {
            try
            {
                
                if (TempTableInWebCashe("t_svod"))
                {    
                    ExecSQL("drop table t_svod; ");
                }
                if (TempTableInWebCashe("t_kdu"))
                {
                    ExecSQL("drop table t_kdu; ");
                }
            }
            catch
            {
               // MonitorLog.WriteLog("Отчет 'Генератор по начислениям' " + e.Message, MonitorLog.typelog.Error, false);
            }
        }
    }
}
