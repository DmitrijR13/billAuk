using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.RT.Properties;
using Bars.KP50.Utils;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;

namespace Bars.KP50.Report.RT.Reports
{
    class Report16520 : BaseSqlReport
    {
        public override string Name
        {
            get { return "16.5.20 Сальдовая ведомость по лицевым счетам"; }
        }

        public override string Description
        {
            get { return "5.20 Сальдовая ведомость по лицевым счетам (с квартиросъемщиками)"; }
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
            get { return Resources.SaldoRepLS; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base }; }
        }


        /// <summary>Заголовок отчета</summary>
        protected int ReportTitle { get; set; }

        /// <summary>Расчетный месяц</summary>
        protected int Month { get; set; }

        /// <summary>Расчетный год</summary>
        protected int Year { get; set; }

        /// <summary>Услуги</summary>
        protected List<int> Services { get; set; }

        /// <summary>Поставщики</summary>
        protected List<long> Suppliers { get; set; }

        /// <summary>Управляющие компании</summary>
        protected List<int> Areas { get; set; }

        /// <summary>Список услуг в заголовке</summary>
        protected string ServicesHeader { get; set; }

        /// <summary>Список Поставщиков в заголовке</summary>
        protected string SuppliersHeader { get; set; }

        /// <summary>Список балансодержателей (Управляющих компаний)</summary>
        protected string AreasHeader { get; set; }

        /// <summary>Список групп</summary>
        protected string GroupsHeader { get; set; }



        public override List<UserParam> GetUserParams()
        {
            var curCalcMonthYear = DBManager.GetCurMonthYear();
            return new List<UserParam>
            {
                new MonthParameter {Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["month_"] : DateTime.Today.Month },
                new YearParameter {Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["yearr"] : DateTime.Today.Year },
                new SupplierParameter(),
                new AreaParameter(),
                new ServiceParameter()              
            };
        }

        public override DataSet GetData()
        {

            #region Выборка по локальным банкам

            MyDataReader reader;

            string sql = " select bd_kernel as pref " +
                         " FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point " +
                         " where nzp_wp>1 ";

            ExecRead(out reader, sql);
            while (reader.Read())
            {
                string pref = reader["pref"].ToStr().Trim();

                sql = " insert into t_saldoLS (num_ls, ulica, ndom, nkor, " +
                      " nkvar, fio, sum_insaldo_k, sum_insaldo_d, sum_insaldo, sum_real, " +
                      " reval_charge, sum_money, money_del, sum_outsaldo_k, sum_outsaldo_d, sum_outsaldo ) " +
                      " select c.num_ls, ulica, ndom, (case when nkor<>'-' then nkor end) as nkor, nkvar, fio, " +
                      " sum(case when c.sum_insaldo<0 then sum_insaldo else 0 end) as sum_insaldo_k, " +
                      " sum(case when c.sum_insaldo>0 then sum_insaldo else 0 end) as sum_insaldo_d, " +
                      " sum(sum_insaldo) as sum_insaldo, " +
                      " sum(sum_real) as sum_real, " +
                      " sum(real_charge) + sum(reval) as reval_charge, " +
                      " sum(sum_money) as sum_money, " +
                      " sum(money_del) as money_del, " +
                      " sum(case when c.sum_outsaldo<0 then sum_outsaldo else 0 end) as sum_outsaldo_k, " +
                      " sum(case when c.sum_outsaldo>0 then sum_outsaldo else 0 end) as sum_outsaldo_d, " +
                      " sum(sum_outsaldo) as sum_outsaldo " +
                      " from " + ReportParams.Pref + DBManager.sDataAliasRest + " kvar k, " +
                      ReportParams.Pref + DBManager.sDataAliasRest + " dom d, " +
                      ReportParams.Pref + DBManager.sDataAliasRest + " s_ulica u, " +
                      pref + "_charge_" + (Year - 2000).ToString("00") + DBManager.tableDelimiter + "charge_" +
                      Month.ToString("00") + " c " +
                      " where k.nzp_dom = d.nzp_dom " +
                      " and d.nzp_ul = u.nzp_ul " +
                      " and c.dat_charge is null " +
                      " and c.nzp_serv > 1 " +
                      " and k.nzp_kvar = c.nzp_kvar " +
                      GetWhereServ() + GetWhereSupp() + GetWhereArea() +
                      " group by 1,2,3,4,5,6 ";
                ExecSQL(sql);
            }

            reader.Close();

            #endregion

            #region Выборка на экран


            sql =
                "select num_ls, ulica, ndom, nkor, (case when (nkvar<>'0' and nkvar<>'-') then 'Кв.'||nkvar end) as nkvar, fio, sum_insaldo_k, sum_insaldo_d, sum_insaldo, sum_real, " +
                " reval_charge, sum_money, money_del, sum_outsaldo_k, sum_outsaldo_d, sum_outsaldo " +
                " from t_saldoLS order by 1,2,3,4,5,6";
            DataTable dt = ExecSQLToTable(sql);
            dt.TableName = "Q_master";
            #endregion


            var ds = new DataSet();
            ds.Tables.Add(dt);

            return ds;
        }

        /// <summary>
        /// Получить условия органичения по УК
        /// </summary>
        /// <returns></returns>
        private string GetWhereArea()
        {
            string where_area = String.Empty;
            if (Areas != null)
            {
                where_area = Areas.Aggregate(where_area, (current, nzpArea) => current + (nzpArea + ","));
            }
            else
            {
                where_area = ReportParams.GetRolesCondition(Constants.role_sql_area);
            }
            where_area = where_area.TrimEnd(',');
            where_area = !String.IsNullOrEmpty(where_area) ? " AND k.nzp_area in (" + where_area + ")" : String.Empty;
            if (!String.IsNullOrEmpty(where_area))
            {
                string sql = " SELECT area from " + ReportParams.Pref + DBManager.sDataAliasRest + "s_area k  WHERE k.nzp_area > 0 " + where_area;
                DataTable area = ExecSQLToTable(sql.ToString());
                foreach (DataRow dr in area.Rows)
                {
                    AreasHeader += dr["area"].ToString().Trim() + ", ";
                }
                AreasHeader = AreasHeader.TrimEnd(',', ' ');
            }
            return where_area;
        }

        /// <summary>
        /// Получить условия органичения по поставщикам
        /// </summary>
        /// <returns></returns>
        private string GetWhereSupp()
        {
            string where_supp = String.Empty;
            if (Suppliers != null)
            {
                where_supp = Suppliers.Aggregate(where_supp, (current, nzpSupp) => current + (nzpSupp + ","));
            }
            else
            {
                where_supp = ReportParams.GetRolesCondition(Constants.role_sql_supp);
            }
            where_supp = where_supp.TrimEnd(',');
            where_supp = !String.IsNullOrEmpty(where_supp) ? " AND nzp_supp in (" + where_supp + ")" : String.Empty;
            if (!String.IsNullOrEmpty(where_supp))
            {
                string sql = " SELECT name_supp from " + ReportParams.Pref + DBManager.sKernelAliasRest + "supplier  WHERE nzp_supp > 0 " + where_supp;
                DataTable supp = ExecSQLToTable(sql.ToString());
                foreach (DataRow dr in supp.Rows)
                {
                    SuppliersHeader += dr["name_supp"].ToString().Trim() + ", ";
                }
                SuppliersHeader = SuppliersHeader.TrimEnd(',', ' ');
            }
            return where_supp;
        }

        /// <summary>
        /// Получить условия органичения по услугам
        /// </summary>
        /// <returns></returns>
        private string GetWhereServ()
        {
            string where_serv = String.Empty;
            if (Services != null)
            {
                where_serv = Services.Aggregate(where_serv, (current, nzpServ) => current + (nzpServ + ","));
            }
            else
            {
                where_serv = ReportParams.GetRolesCondition(Constants.role_sql_serv);
            }
            where_serv = where_serv.TrimEnd(',');
            where_serv = !String.IsNullOrEmpty(where_serv) ? " AND nzp_serv in (" + where_serv + ")" : String.Empty;
            if (!String.IsNullOrEmpty(where_serv))
            {
                string sql = " SELECT service from " + ReportParams.Pref + DBManager.sKernelAliasRest + "services  WHERE nzp_serv > 0 " + where_serv;
                DataTable serv = ExecSQLToTable(sql.ToString());
                foreach (DataRow dr in serv.Rows)
                {
                    ServicesHeader += dr["service"].ToString().Trim() + ", ";
                }
                ServicesHeader = ServicesHeader.TrimEnd(',', ' ');

            }
            return where_serv;
        }


        protected override void PrepareReport(FastReport.Report report)
        {
            report.SetParameterValue("supp", SuppliersHeader);
            report.SetParameterValue("area", AreasHeader);
            report.SetParameterValue("services", ServicesHeader);
            report.SetParameterValue("printDate", DateTime.Now.ToLongDateString());
            report.SetParameterValue("printTime", DateTime.Now.ToLongTimeString());


        }

        protected override void PrepareParams()
        {
            //using (var sw = new StreamWriter(@"D:\1.txt")) sw.WriteLine("Begin!");
            //System.Threading.Thread.Sleep(new TimeSpan(0, 15, 0));
            Services = UserParamValues["Services"].GetValue<List<int>>();
            Suppliers = UserParamValues["Suppliers"].GetValue<List<long>>();
            Areas = UserParamValues["Areas"].GetValue<List<int>>();
            Month = UserParamValues["Month"].GetValue<int>();
            Year = UserParamValues["Year"].GetValue<int>();
        }

        protected override void CreateTempTable()
        {

            string sql = " create temp table t_saldoLS (  " +
                         " num_ls integer default 0, " +
                         " ulica character(40), " +
                         " ndom character(10), " +
                         " nkor character(3), " +
                         " nkvar character(10), " +
                         " fio character(40), " +
                         " sum_insaldo_k " + DBManager.sDecimalType + "(14,2), " +
                         " sum_insaldo_d " + DBManager.sDecimalType + "(14,2), " +
                         " sum_insaldo " + DBManager.sDecimalType + "(14,2), " +
                         " sum_real " + DBManager.sDecimalType + "(14,2), " +
                         " reval_charge " + DBManager.sDecimalType + "(14,2), " +
                         " sum_money " + DBManager.sDecimalType + "(14,2), " +
                         " money_del " + DBManager.sDecimalType + "(14,2), " +
                         " sum_outsaldo_k " + DBManager.sDecimalType + "(14,2), " +
                         " sum_outsaldo_d " + DBManager.sDecimalType + "(14,2), " +
                         " sum_outsaldo " + DBManager.sDecimalType + "(14,2) " +
                         " ) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        protected override void DropTempTable()
        {
            ExecSQL(" drop table t_saldoLS ", true);
        }

    }
}
