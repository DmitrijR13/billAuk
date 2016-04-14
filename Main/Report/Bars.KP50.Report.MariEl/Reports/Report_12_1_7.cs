using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Bars.KP50.Report.Base;
using Bars.KP50.Utils;
using Newtonsoft.Json;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using Bars.KP50.Report.MariEl.Properties;

namespace Bars.KP50.Report.MariEl.Reports
{
  public  class Report120107 : BaseSqlReport
    {
        public override string Name
        {
            get { return "12.1.7 - Свод начислений по поставщикам"; }
        }

        public override string Description
        {
            get { return "12.1.7 - Свод начислений по поставщикам."; }
        }

        public override IList<ReportGroup> ReportGroups
        {
            get
            {
                var result = new List<ReportGroup> {ReportGroup.Reports};
                return result;
            }
        }

        public override bool IsPreview
        {
            get { return false; }
        }

        protected override byte[] Template
        {
            get { return Resources.Report_12_1_7; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base }; }
        }


        #region Параметры
        /// <summary>Расчетный месяц</summary>
        protected int Month { get; set; }

        /// <summary>Расчетный год</summary>
        protected int Year { get; set; }

        /// <summary>Услуги</summary>
        protected List<int> Services { get; set; }

        /// <summary>Список услуг в заголовке</summary>
        protected string ServiceHeader { get; set; }

        /// <summary>Список Поставщиков в заголовке</summary>
        protected string SupplierHeader { get; set; }

        /// <summary>Заголовок территории</summary>
        protected string TerritoryHeader { get; set; }

        /// <summary>Ук</summary>
        protected List<int> Areas { get; set; }
        /// <summary>Список УК в заголовке</summary>
        protected string AreaHeader { get; set; }

        /// <summary>Поставщики, Агенты, Принципалы  </summary>
        protected BankSupplierParameterValue BankSupplier { get; set; }

        private int _Group;

        #endregion

        public override List<UserParam> GetUserParams()
        {
            var curCalcMonthYear = DBManager.GetCurMonthYear();
            return new List<UserParam>
            {
                new MonthParameter {Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["month_"] : DateTime.Today.Month },
                new YearParameter {Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["yearr"] : DateTime.Today.Year }, 
                new BankSupplierParameter(),
                new AreaParameter(),
                new ServiceParameter(),
                new ComboBoxParameter(false)
                {
                    Code = "Group",
                    Name = "В разрезе",
                    Value = 1,
                    Require = true,
                    StoreData = new List<object>
                    {
                        new {Id = 1, Name = "Поставщиков"},
                        new {Id = 2, Name = "Поставщиков и услуг"},
                    }
                }
            };
        }

        public override DataSet GetData()
        {  
            string whereServ = GetWhereServ(),
                whereSupp=GetWhereSupp("c.nzp_supp"),
                whereArea=GetWhereArea();

            MyDataReader reader;

            string sql = " SELECT bd_kernel AS pref " +
                         " FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point " +
                         " WHERE nzp_wp > 1 " + GetWhereWp();  
            ExecRead(out reader, sql);
            while (reader.Read())
            {
                string pref = reader["pref"].ToStr().Trim();

                string chargeYY = pref + "_charge_" + (Year - 2000).ToString("00") + DBManager.tableDelimiter +
                                  "charge_" + Month.ToString("00"),
                    kvar = pref + DBManager.sDataAliasRest + "kvar ";

                    if (TempTableInWebCashe(chargeYY))
                    {
                        sql =
                            " INSERT INTO t_report_12_1_7 (nzp_area, nzp_serv, nzp_supp, sum_nach ) " +
                            " SELECT k.nzp_area, " + (_Group == 1 ? " 0, " : " c.nzp_serv, ") + " c.nzp_supp,  " +
                            " SUM(sum_tarif + reval + real_charge) " +
                            " FROM " + kvar + " k, " +
                            chargeYY + " c " +
                            " WHERE c.dat_charge IS NULL " +
                            " AND c.nzp_serv > 1 " +
                            " AND k.num_ls = c.num_ls " +
                            whereServ + whereSupp + whereArea +
                            " GROUP BY 1,2,3 ";
                        ExecSQL(sql);
                    }     
            } 
            reader.Close();     

            sql =
                " SELECT area, payer as name_supp, service, " +
                " SUM(sum_nach) AS sum_nach " +
                " FROM t_report_12_1_7 t INNER JOIN  " +
                        ReportParams.Pref + DBManager.sKernelAliasRest + "supplier su ON  t.nzp_supp = su.nzp_supp INNER JOIN " + 
                        ReportParams.Pref + DBManager.sKernelAliasRest + "s_payer p ON su.nzp_payer_supp = p.nzp_payer INNER JOIN  " +
                        ReportParams.Pref + DBManager.sDataAliasRest + "s_area a ON t.nzp_area = a.nzp_area LEFT JOIN " +
                        ReportParams.Pref + DBManager.sKernelAliasRest + "services s ON t.nzp_serv = s.nzp_serv " +     
                " GROUP BY 1,2,3 "+
                " ORDER BY 1,2,3 ";
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

        protected override void PrepareReport(FastReport.Report report)
        {
            var months = new[] {"","Январь","Февраль",
                 "Март","Апрель","Май","Июнь","Июль","Август","Сентябрь",
                 "Октябрь","Ноябрь","Декабрь"};     

            report.SetParameterValue("print_date", DateTime.Now.ToShortDateString());
            report.SetParameterValue("print_time", DateTime.Now.ToShortTimeString());
            report.SetParameterValue("gr", _Group);


            string headerParam = months[Month] + " " + Year+"\n";
            headerParam += _Group == 1 ? "Группировка строк: Поставщик;\n" : "Группировка строк: Поставщик; Вид услуги;\n";
            headerParam += "Группировка строк: Управляющая компания;\n";
            headerParam += "Показатели: Начислено;\n";
            headerParam += "Отбор:\n";
            headerParam += !string.IsNullOrEmpty(AreaHeader) ? "Управляющие компании : " + AreaHeader + ";\n" : string.Empty;
            headerParam += !string.IsNullOrEmpty(SupplierHeader) ? "Поставщики: " + SupplierHeader + ";\n" : string.Empty;
            headerParam += !string.IsNullOrEmpty(ServiceHeader) ? "Услуги: " + ServiceHeader + ";\n" : string.Empty;
            
            report.SetParameterValue("headerParam", headerParam);
        }

        protected override void PrepareParams()
        { 
            Month = UserParamValues["Month"].GetValue<int>();
            Year = UserParamValues["Year"].Value.To<int>();
            _Group = UserParamValues["Group"].GetValue<int>();  
            BankSupplier = JsonConvert.DeserializeObject<BankSupplierParameterValue>(UserParamValues["BankSupplier"].Value.ToString());
            Services = UserParamValues["Services"].GetValue<List<int>>(); 
            Areas = UserParamValues["Areas"].GetValue<List<int>>();
        }

        protected override void CreateTempTable()
        {
            string sql = " CREATE TEMP TABLE t_report_12_1_7 (  " +
                               " nzp_area INTEGER DEFAULT 0, " +
                               " nzp_supp INTEGER DEFAULT 0, " +
                               " nzp_serv INTEGER DEFAULT 0, " +
                               " sum_nach " + DBManager.sDecimalType + "(14,2)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            ExecSQL(" CREATE INDEX ix_t_report_12_1_7_1 on t_report_12_1_7(nzp_supp) ");
            ExecSQL(" CREATE INDEX ix_t_report_12_1_7_2 on t_report_12_1_7(nzp_serv) ");
            ExecSQL(" CREATE INDEX ix_t_report_12_1_7_3 on t_report_12_1_7(nzp_area) ");
        }

        protected override void DropTempTable()
        {
            ExecSQL(" DROP TABLE t_report_12_1_7 ", true);
        }


        #region Фильтры
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
            whereArea = !String.IsNullOrEmpty(whereArea) ? " AND k.nzp_area in (" + whereArea + ")" : String.Empty;
            if (!String.IsNullOrEmpty(whereArea))
            {
                AreaHeader = String.Empty;
                string sql = " SELECT area from " + ReportParams.Pref + DBManager.sDataAliasRest + "s_area k  WHERE k.nzp_area > 0 " + whereArea;
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
        /// Получить условия органичения по поставщикам
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

            whereSupp = whereSupp.TrimEnd(',');


            if (!String.IsNullOrEmpty(whereSupp) || !String.IsNullOrEmpty(oldsupp))
            {
                if (!String.IsNullOrEmpty(oldsupp))
                    whereSupp += " AND nzp_supp in (" + oldsupp + ")";

                //Поставщики
                if (String.IsNullOrEmpty(SupplierHeader))
                {
                    SupplierHeader = string.Empty;
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
            whereServ = Services != null ? Services.Aggregate(whereServ, (current, nzpServ) => current + (nzpServ + ",")) : ReportParams.GetRolesCondition(Constants.role_sql_serv);
            whereServ = whereServ.TrimEnd(',');
            whereServ = !String.IsNullOrEmpty(whereServ) ? " AND nzp_serv in (" + whereServ + ")" : String.Empty;
            if (!String.IsNullOrEmpty(whereServ))
            {
                if (String.IsNullOrEmpty(ServiceHeader))
                {
                    ServiceHeader = string.Empty;
                    string sql = " SELECT service from " + ReportParams.Pref + DBManager.sKernelAliasRest +
                                 "services  WHERE nzp_serv > 0 " + whereServ;
                    DataTable serv = ExecSQLToTable(sql);
                    foreach (DataRow dr in serv.Rows)
                    {
                        ServiceHeader += dr["service"].ToString().Trim() + ", ";
                    }
                    ServiceHeader = ServiceHeader.TrimEnd(',', ' ');
                }
            }
            return whereServ;
        }

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
        #endregion

    }
}
