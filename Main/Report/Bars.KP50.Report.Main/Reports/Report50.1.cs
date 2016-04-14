using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.Main.Properties;
using Bars.KP50.Utils;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;

namespace Bars.KP50.Report.Main.Reports
{
    public class Report501 : BaseSqlReport
    {
        public override string Name
        {
            get { return "50.1 Сальдовая ведомость для Энергосбыта"; }
        }

        public override string Description
        {
            get { return "50.1 Сальдовая ведомость для Энергосбыта"; }
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
            get { return Resources.report50_1; }
        }

        public override ReportKind ReportKind
        {
            get { return ReportKind.Base; }
        }


        /// <summary>Расчетный месяц</summary>
        protected int MonthS { get; set; }

        /// <summary>Расчетный год</summary>
        protected int YearS { get; set; }


        /// <summary>Расчетный месяц</summary>
        protected int MonthPo { get; set; }

        /// <summary>Расчетный год</summary>
        protected int YearPo { get; set; }

        /// <summary>Услуги</summary>
        protected List<int> Services { get; set; }

        /// <summary>Поставщики</summary>
        protected List<long> Suppliers { get; set; }

        /// <summary>Управляющие компании</summary>
        protected List<int> Areas { get; set; }

        /// <summary>Районы</summary>
        protected List<int> Rajons { get; set; }

        /// <summary>Заголовок отчета</summary>
        protected string SupplierHeader { get; set; }

        /// <summary>Заголовок отчета</summary>
        protected string AreaHeader { get; set; }

        /// <summary>Заголовок отчета</summary>
        protected string RajonHeader { get; set; }

        private string _ercName;

        public override List<UserParam> GetUserParams()
        {
            return new List<UserParam>
            {
                new RaionsParameter(),
                new MonthParameter {Name = "Месяц с", Value = DateTime.Today.Month },
                new YearParameter {Name = "Год с", Value = DateTime.Today.Year },
                new MonthParameter {Name = "Месяц по", Code = "Month1", Value = DateTime.Today.Month },
                new YearParameter {Name = "Год по", Code = "Year1", Value = DateTime.Today.Year },
                new ServiceParameter(),
                new SupplierParameter(),
                new AreaParameter()
            };
        }

        public override DataSet GetData()
        {
            _ercName = GetErcName();


            string sql = " select bd_kernel as pref " +
                         " from " + DBManager.GetFullBaseName(Connection) + DBManager.tableDelimiter + "s_point " +
                         " where nzp_wp>1 ";
            DataTable dpref = ExecSQLToTable(sql);

            foreach (DataRow dr in dpref.Rows)
            {
                if (dr["pref"] == null) continue;
                string pref = dr["pref"].ToString().Trim();
                for (int i = YearS*12 + MonthS; i < YearPo*12 + MonthPo + 1; i++)
                {
                    GetOneMonth(i, pref);
                }
            }

            

            sql = " select MDY(month_,01,year_) as dat_month, sum(sum_insaldo_n) as sum_insaldo_n, " +
                  "         sum(sum_in36) as sum_in36, " +
                  "         sum(sum_insaldo_a) as sum_insaldo_a, " +
                  "         sum(sum_insaldo_d) as sum_insaldo_d," +
                  "         sum(sum_insaldo) as sum_insaldo, " +
                  "         sum(sum_nach_n) as sum_nach_n, " +
                  "         sum(sum_nach_a) as sum_nach_a, " +
                  "         sum(sum_nach) as sum_nach, " +
                  "         sum(sum_money_n) as sum_money_n, " +
                  "         sum(sum_money_a) as sum_money_a, " +
                  "         sum(sum_money) as sum_money," +
                  "         sum(sum_outsaldo_n) as sum_outsaldo_n, " +
                  "         sum(sum_out36) as sum_out36, " +
                  "         sum(sum_outsaldo_a) as sum_outsaldo_a, " +
                  "         sum(sum_outsaldo_d) as sum_outsaldo_d, " +
                  "         sum(sum_outsaldo) as sum_outsaldo    " +
                  " FROM t_ensaldo  " +
                  " GROUP BY 1 " +
                  " ORDER BY 1 ";
            DataTable dt = ExecSQLToTable(sql);
            dt.TableName = "Q_master";

            var ds = new DataSet();
            ds.Tables.Add(dt);

            return ds;
        }

        private void GetOneMonth(int i, string pref)
        {
            int curMonth = i%12;
            int curYear = curMonth == 0 ? (i/12) - 1 : (i/12);
            if (curMonth == 0) curMonth = 1;
            string baseData = pref + DBManager.sDataAliasRest;
            string sumInsaldo = ((curMonth == MonthS) & (curYear == YearS)) ? "sum_insaldo" : "0";
            string sumOutsaldo = ((curMonth == MonthPo) & (curYear == YearPo)) ? "sum_outsaldo" : "0";
            string tableCharge = pref + "_charge_" + (curYear - 2000).ToString("00") +
                                 DBManager.tableDelimiter + "charge_" +
                                 curMonth.ToString("00");
            if (TempTableInWebCashe(tableCharge))
            {
           

                string sql = " CREATE TEMP TABLE t_temp_ensaldo (" +
                             " month_ integer default 0," +
                             " year_ integer default 0," +
                             " typek INTEGER default 1 NOT NULL, " +
                             " sum_insaldo " + DBManager.sDecimalType + "(14,2) DEFAULT 0, " +
                             " sum_real  " + DBManager.sDecimalType + "(14,2) DEFAULT 0, " +
                             " reval  " + DBManager.sDecimalType + "(14,2) DEFAULT 0.00, " +
                             " real_charge  " + DBManager.sDecimalType + "(14,2) DEFAULT 0.00, " +
                             " sum_money  " + DBManager.sDecimalType + "(14,2) DEFAULT 0, " +
                             " sum_outsaldo  " + DBManager.sDecimalType + "(14,2) default 0) " +
                             DBManager.sUnlogTempTable;
                ExecSQL(sql);

                sql = " INSERT INTO t_temp_ensaldo (month_, year_, typek, " +
                      " sum_insaldo, sum_real, reval, real_charge, sum_money, sum_outsaldo) " +
                      " SELECT " + curMonth + ", " + curYear + ", " +
                      " typek, sum(" + sumInsaldo + "), " +
                      " sum(sum_real), sum(reval), sum(real_charge), sum(sum_money), " +
                      " sum(" + sumOutsaldo + ") " +
                      " FROM  " + tableCharge + " a, " + baseData + " kvar k" +
                      " WHERE a.nzp_kvar = k.nzp_kvar AND dat_charge IS NULL " +
                      GetWhereAdr() + GetWhereServ() + GetWhereSupp() +
                      " GROUP BY 1,2,3";

                ExecSQL(sql);

                ExecSQL("create index ixtmp_ensaldo_01 on t_temp_ensaldo(month_)");
                ExecSQL("update statistics for table t_temp_ensaldo");

                sql =
                    " INSERT INTO t_ensaldo (month_, year_, sum_insaldo_n, sum_in36, sum_insaldo_a, sum_insaldo_d, sum_insaldo, sum_nach_n, sum_nach_a, sum_nach, sum_money_n, sum_money_a, sum_money, sum_outsaldo_n, sum_out36, sum_outsaldo_a, sum_outsaldo_d, sum_outsaldo) " +
                    " SELECT month_, year_, " +
                    "       sum(CASE WHEN typek=1 THEN sum_insaldo ELSE 0 END) AS sum_insaldo_n, " +
                    "       sum(CASE WHEN sum_real>0 " +
                    "           AND (sum_insaldo/sum_real) > 36 THEN sum_insaldo ELSE 0 END) AS sum_in36, " +
                    "       sum(CASE WHEN typek>1 THEN sum_insaldo ELSE 0 END) AS sum_insaldo_a, " +
                    "       sum(CASE WHEN typek=1 " +
                    "           AND sum_real = 0 THEN sum_insaldo ELSE 0 END) AS sum_insaldo_d, " +
                    "       sum(sum_insaldo) AS sum_insaldo, " +
                    "       sum(CASE WHEN typek=1 THEN sum_real+reval+real_charge ELSE 0 END) AS sum_nach_n, " +
                    "       sum(CASE WHEN typek>1 THEN sum_real+reval+real_charge ELSE 0 END) AS sum_nach_a, " +
                    "       sum(CASE WHEN typek>=1 THEN sum_real+reval+real_charge ELSE 0 END) AS sum_nach, " +
                    "       sum(CASE WHEN typek=1 THEN nvl(sum_money,0) ELSE 0 END) AS sum_money_n, " +
                    "       sum(CASE WHEN typek>1 THEN nvl(sum_money,0) ELSE 0 END) AS sum_money_a, " +
                    "       sum(CASE WHEN typek>=1 THEN nvl(sum_money,0) ELSE 0 END) AS sum_money, " +
                    "       sum(CASE WHEN typek=1 THEN sum_outsaldo ELSE 0 END) AS sum_outsaldo_n, " +
                    "       sum(CASE WHEN typek=1 " +
                    "           AND sum_real>0 " +
                    "           AND ((sum_outsaldo-sum_real-reval-real_charge)/sum_real) > 36 THEN sum_outsaldo-sum_real-reval-real_charge ELSE 0 END) AS sum_out36, " +
                    "       sum(CASE WHEN typek>1 THEN sum_outsaldo ELSE 0 END) AS sum_outsaldo_a, " +
                    "       sum(CASE WHEN typek=1 " +
                    "           AND sum_real=0 THEN sum_outsaldo ELSE 0 END) AS sum_outsaldo_d, " +
                    "           sum(sum_outsaldo) AS sum_outsaldo " +
                    " FROM t_temp_ensaldo " +
                    " GROUP BY 1,2 ";
                ExecSQL(sql);
                ExecSQL("drop table t_temp_ensaldo");
            }
        }

        protected override void CreateTempTable()
        {
            const string sql = " create temp table t_ensaldo ( " +
                               " month_ integer default 0," +
                               " year_ integer default 0," +
                               " sum_insaldo  " + DBManager.sDecimalType + "(14,2) default 0, " +
                               " sum_insaldo_n  " + DBManager.sDecimalType + "(14,2) default 0, " + //Входящее сальдо по населению
                               " sum_insaldo_a  " + DBManager.sDecimalType + "(14,2) default 0, " + // Входящее сальдо по арендаторам
                               " sum_insaldo_d  " + DBManager.sDecimalType + "(14,2) default 0, " +
                               //Входящее сальдо по тем у кого текущие начисления равны 0
                               " sum_in36  " + DBManager.sDecimalType + "(14,2) default 0, " +
                               //Входящее сальдо по тому населению у которого долги больше 3 лет
                               " sum_nach_a  " + DBManager.sDecimalType + "(14,2) default 0.00, " + //начисление по арендаторам
                               " sum_nach_n  " + DBManager.sDecimalType + "(14,2) default 0.00, " + //начисление по населению
                               " sum_nach  " + DBManager.sDecimalType + "(14,2) default 0.00, " +
                               " sum_money_a  " + DBManager.sDecimalType + "(14,2) default 0, " + //Оплата по арендаторам
                               " sum_money_n  " + DBManager.sDecimalType + "(14,2) default 0,   " + //Оплата по населению
                               " sum_money  " + DBManager.sDecimalType + "(14,2) default 0,   " + //Всего оплата
                               " sum_outsaldo_n  " + DBManager.sDecimalType + "(14,2) default 0, " + //Исходящее сальдо по населению
                               " sum_outsaldo_a  " + DBManager.sDecimalType + "(14,2) default 0, " + //Исходящее сальдо по арендаторам
                               " sum_outsaldo_d  " + DBManager.sDecimalType + "(14,2) default 0, " + //Исходящее сальдо по тем у кого начисления равны 0
                               " sum_outsaldo  " + DBManager.sDecimalType + "(14,2) default 0, " +
                               " sum_out36  " + DBManager.sDecimalType + "(14,2) default 0) "+
                               DBManager.sUnlogTempTable;

            ExecSQL(sql);
        }

        protected override void DropTempTable()
        {
            try { ExecSQL("drop table t_temp_ensaldo"); } catch (Exception e)
            {
               MonitorLog.WriteLog("Отчет 50.2 "+e.Message,MonitorLog.typelog.Error,false);
            }


            try { ExecSQL("drop table t_ensaldo"); }
            catch (Exception e)
            {
                MonitorLog.WriteLog("Отчет 50.2 " + e.Message, MonitorLog.typelog.Error, false);
            }

            
        }


        protected override void PrepareReport(FastReport.Report report)
        {
            var months = new[] {"","Январь","Февраль",
                 "Март","Апрель","Май","Июнь","Июль","Август","Сентябрь",
                 "Октябрь","Ноябрь","Декабрь"};

            string headers = "Сальзовая ведомость за " ;
            if ((MonthS == MonthPo) & (YearS == YearPo))
            {
                headers +=  months[MonthS] + " " + YearS;
            }
            else
            {
               headers += "период  " + months[MonthS] + " " + YearS +
                                                         "г. - " + months[MonthPo] + " " + YearPo;

            }

            if (!String.IsNullOrEmpty(AreaHeader)) headers += Environment.NewLine + AreaHeader;
            if (!String.IsNullOrEmpty(SupplierHeader))
            {
                if (Suppliers.Count > 1)
                {
                    headers += Environment.NewLine + " поставщикам: " + SupplierHeader;
                }
                else
                {
                    headers += Environment.NewLine + " поставщику: " + SupplierHeader;
                }
            }
            if (!String.IsNullOrEmpty(RajonHeader))
            {
                if (Rajons.Count > 1)
                {
                    headers += Environment.NewLine + " районам: " + RajonHeader;
                }
                else
                {
                    headers += Environment.NewLine + " району: " + RajonHeader;
                }
            }

            headers += Environment.NewLine + _ercName;
            report.SetParameterValue("header", headers);
            
        }

        /// <summary>
        /// Получате условия органичения по услугам
        /// </summary>
        /// <returns></returns>
        private string GetWhereServ()
        {
            string whereServ = String.Empty;
            if (Services != null)
            {
                whereServ = Services.Aggregate(whereServ, (current, nzpServ) => current + (nzpServ + ","));
            }
            whereServ = whereServ.TrimEnd(',');
            whereServ = !String.IsNullOrEmpty(whereServ) ? " AND nzp_serv in (" + whereServ + ")" : String.Empty;
          
            return whereServ;
        }

        /// <summary>
        /// Получает условия ограничения по территории
        /// </summary>
        /// <returns></returns>
        private string GetWhereAdr()
        {
            string whereAdr = String.Empty;
            if (Areas != null)
            {
                whereAdr = Areas.Aggregate(whereAdr, (current, nzpArea) => current + (nzpArea + ","));
            }

            whereAdr = whereAdr.TrimEnd(',');
            if (!String.IsNullOrEmpty(whereAdr))
            {
                whereAdr = " AND nzp_area in (" + whereAdr + ")";


                if (!String.IsNullOrEmpty(AreaHeader))
                {
                    string sql = " SELECT area from " +
                                 ReportParams.Pref + DBManager.sDataAliasRest + "s_area " +
                                 " WHERE nzp_area > 0 " + whereAdr;
                    DataTable area = ExecSQLToTable(sql);
                    foreach (DataRow dr in area.Rows)
                    {
                        AreaHeader += dr["area"].ToString().Trim() + ",";
                    }
                    AreaHeader = AreaHeader.TrimEnd(',');
                }
            }
            else
            {
                whereAdr = " AND nzp_area in (" + whereAdr + ")";
            }
            return whereAdr;
        }

        /// <summary>
        /// Получает условия ограничения по поставщику
        /// </summary>
        /// <returns></returns>
        private string GetWhereSupp()
        {
            string whereSupp = String.Empty;
            if (Suppliers != null)
                whereSupp = Suppliers.Aggregate(whereSupp, (current, nzpSupp) => current + (nzpSupp + ","));
            whereSupp = whereSupp.TrimEnd(',');


            if (!String.IsNullOrEmpty(whereSupp) )
            {
                whereSupp = " AND nzp_supp in (" + whereSupp + ")";

                //Поставщики
                if (!String.IsNullOrEmpty(SupplierHeader))
                {
                    string sql = " SELECT name_supp from " +
                                 ReportParams.Pref + DBManager.sKernelAliasRest + "supplier " +
                                 " WHERE nzp_supp > 0 " + whereSupp;
                    DataTable supp = ExecSQLToTable(sql);
                    foreach (DataRow dr in supp.Rows)
                    {
                        SupplierHeader += dr["name_supp"].ToString().Trim() + ",";
                    }
                    SupplierHeader = SupplierHeader.TrimEnd(',');
                }
            }
            return whereSupp;
        }

     

        /// <summary>
        /// Получает условия ограничения по району
        /// </summary>
        /// <returns></returns>
        private string GetErcName()
        {
            string result = "Не определено наименование Расчетного центра";
            string sql = " select val_prm " +
                         " from " + ReportParams.Pref + DBManager.sDataAliasRest + "prm_10 " +
                         " where nzp_prm=80 and is_actual=1 and dat_s<=" + DBManager.sCurDate +
                         " and dat_po>=" + DBManager.sCurDate;
            DataTable erc = ExecSQLToTable(sql);
            if (erc != null)
                if (erc.Rows.Count > 0)
                {
                    result = erc.Rows[0]["val_prm"].ToString().Trim();
                }


            return result;
        }

        protected override void PrepareParams()
        {
            MonthS = UserParamValues["Month"].GetValue<int>();
            YearS = UserParamValues["Year"].Value.To<int>();
            MonthPo = UserParamValues["Month1"].GetValue<int>();
            YearPo = UserParamValues["Year1"].Value.To<int>();
            
            Services = UserParamValues["Services"].Value.To<List<int>>();
            Suppliers = UserParamValues["Suppliers"].Value.To<List<long>>();
            Areas = UserParamValues["Areas"].Value.To<List<int>>();
            Rajons = UserParamValues["Raions"].Value.To<List<int>>();
    
        }
    }
}
