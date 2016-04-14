using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.RT.Properties;
using Bars.KP50.Utils;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;

namespace Bars.KP50.Report.RT.Reports
{
    public class Report1653 : BaseSqlReport
    {
        public override string Name
        {
            get { return "16.5.3 Сальдовая ведомость по домам"; }
        }

        public override string Description
        {
            get { return "5.3 Сальдовая ведомость по домам"; }
        }

        public override IList<ReportGroup> ReportGroups
        {
            get
            {
                List<ReportGroup> result = new List<ReportGroup>();
                result.Add(ReportGroup.Reports);
                return result;
            }
        }

        public override bool IsPreview
        {
            get { return false; }
        }

        protected override byte[] Template
        {
            get { return Resources.Report_16_5_3; }
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

        /// <summary>Поставщики</summary>
        protected List<long> Suppliers { get; set; }

        /// <summary>Группы</summary>
        protected List<int> Groups { get; set; }

        /// <summary>Управляющие компании</summary>
        protected List<int> Areas { get; set; }

        /// <summary>Город</summary>
        protected List<int> Cities { get; set; }

        /// <summary>Заголовок отчета</summary>
        protected string SupplierHeader { get; set; }

        /// <summary>Заголовок отчета</summary>
        protected string AreaHeader { get; set; }

        /// <summary>Заголовок отчета</summary>
        protected string ServicesHeader { get; set; }

        /// <summary>Заголовок отчета</summary>
        protected string GroupsHeader { get; set; }

        public override List<UserParam> GetUserParams()
        {
            var curCalcMonthYear = DBManager.GetCurMonthYear();
            return new List<UserParam>
            {
                new MonthParameter {Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["month_"] : DateTime.Today.Month },
                new YearParameter {Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["yearr"] : DateTime.Today.Year },
                new ServiceParameter(),
                new SupplierParameter(),
                new AreaParameter()
            };
        }

        public override DataSet GetData()
        {
            IDataReader reader;

            var sql = new StringBuilder();

            //WhereStringForFindCommon(finder, "a", ref whereStr);

            string whereWp = GetWhereWp();
            string whereSupp = GetWhereSupp();
            string whereServ = GetWhereServ();
            string whereArea = GetWhereArea();
            string whereGroup = "";


            #region Группы
            if (GroupsHeader != null)
            {
                whereGroup = Groups.Aggregate(whereGroup, (current, nzpGroup) => current + (nzpGroup.ToString(CultureInfo.InvariantCulture) + ","));
                whereGroup = whereGroup.TrimEnd(',');
            }

            if (!String.IsNullOrEmpty(whereGroup))
            {
                whereGroup = " and nzp_group in(" + whereGroup + ")";
                sql.Remove(0, sql.Length);
                sql.Append(" SELECT ngroup from ");
                sql.Append(ReportParams.Pref + DBManager.sDataAliasRest + "s_group ");
                sql.Append(" WHERE nzp_group > 0 " + whereGroup);
                DataTable group = ExecSQLToTable(sql.ToString());
                foreach (DataRow dr in group.Rows)
                {
                    GroupsHeader += dr["ngroup"].ToString().Trim() + ",";
                }
                if (GroupsHeader != null) GroupsHeader = GroupsHeader.TrimEnd(',');
            }
            #endregion


            #region выборка в temp таблицу
            sql.Remove(0, sql.Length);
            sql.Append(" SELECT bd_kernel as pref ");
            sql.Append(" FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point ");
            sql.Append(" WHERE nzp_wp>1 " + whereWp);

            ExecRead(out reader, sql.ToString());

            while (reader.Read())
            {
                if (reader["pref"] != null)
                {
                    string pref = reader["pref"].ToStr().Trim();
                    string chargeTable = pref + "_charge_" + (Year - 2000).ToString("00") + DBManager.tableDelimiter + "charge_" + Month.ToString("00");
                    if (TempTableInWebCashe(chargeTable))
                    {
                        sql.Remove(0, sql.Length);
                        sql.Append(" INSERT INTO t_svod(nzp_dom,adr, s1,s2,s3,s4,s5,s6,s7,s8,s9,s10,s11) ");
                        sql.Append(" SELECT k.nzp_dom,'' as adr,");
                        sql.Append(" sum(a.sum_insaldo) as s1, ");
                        sql.Append(" sum(a.sum_tarif)   as s2, ");
                        sql.Append(" sum(a.sum_lgota)   as s3, ");
                        sql.Append(" sum(a.sum_real)    as s4, ");
                        sql.Append(" sum(a.real_charge+ a.reval) as s5,  ");
                        sql.Append(" sum(a.sum_money)   as s6, ");
                        sql.Append(" sum(CASE WHEN a.sum_outsaldo>0 then a.sum_outsaldo else 0 end) as s7, ");
                        sql.Append(" sum(CASE WHEN a.sum_outsaldo<0 then a.sum_outsaldo else 0 end) as s8, ");
                        sql.Append(" sum(a.sum_outsaldo) as s9, 0 as s10,0 as s11");
                        sql.Append(" FROM " + chargeTable + " a, ");
                        sql.Append(pref + DBManager.sDataAliasRest + "kvar k ");
                        sql.Append(" WHERE a.dat_charge is null AND nzp_supp>0 ");
                        sql.Append(" AND a.nzp_kvar=k.nzp_kvar AND a.nzp_serv>1 ");
                        sql.Append(whereArea + whereServ + whereSupp);
                        sql.Append(" GROUP BY 2,1");

                        ExecSQL(sql.ToString());
                    }
                }
            }

            reader.Close();
            #endregion

            sql.Remove(0, sql.Length);
            sql.Append(" SELECT c.nzp_dom, ");
            sql.Append(" trim(u.ulica)||' д.'||trim((d.ndom))||' '||trim(CASE WHEN d.nkor='-' then '' else  'к. ' ||''||d.nkor end) as adr, ");
            sql.Append(" sum(s1) AS s1, ");
            sql.Append(" sum(s2) AS s2, ");
            sql.Append(" sum(s3) AS s3, ");
            sql.Append(" sum(s4) AS s4, ");
            sql.Append(" sum(s5) AS s5, ");
            sql.Append(" sum(s6) AS s6, ");
            sql.Append(" sum(s7) AS s7, ");
            sql.Append(" sum(s8) AS s8, ");
            sql.Append(" sum(s9) AS s9, ");
            sql.Append(" sum(s9) AS s9, ");
            sql.Append(" sum(s7-s4) AS s10, ");
            sql.Append(" sum(CASE WHEN s4<>0 THEN CAST((((s7-s4)/s4)*100) AS " + DBManager.sDecimalType + "(14,2)) ELSE 0 END) AS s11 ");
            sql.Append(" FROM t_svod c, ");
            sql.Append(ReportParams.Pref + DBManager.sDataAliasRest + "dom d, ");
            sql.Append(ReportParams.Pref + DBManager.sDataAliasRest + "s_rajon sr, ");
            sql.Append(ReportParams.Pref + DBManager.sDataAliasRest + "s_ulica u");
            sql.Append(" WHERE  u.nzp_raj=sr.nzp_raj ");
            sql.Append(" AND c.nzp_dom=d.nzp_dom AND u.nzp_ul=d.nzp_ul ");
            sql.Append(" GROUP BY 1,2 ");
            sql.Append(" ORDER BY 2 ");

            DataTable dt = ExecSQLToTable(sql.ToString());
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
            var whereServ = String.Empty;
            if (Services != null)
            {
                whereServ = Services.Aggregate(whereServ, (current, nzpServ) => current + (nzpServ + ","));
            }
            else
            {
                whereServ = ReportParams.GetRolesCondition(Constants.role_sql_serv);
            }
            whereServ = whereServ.TrimEnd(',');

            if (!String.IsNullOrEmpty(whereServ))
            {
                whereServ = " AND nzp_serv in (" + whereServ + ")";
                ServicesHeader = String.Empty;

                string sql = " SELECT service from " +
                             ReportParams.Pref + DBManager.sKernelAliasRest + "services"  +
                             " WHERE nzp_serv > 0 " + whereServ;
                DataTable ser = ExecSQLToTable(sql);
                foreach (DataRow dr in ser.Rows)
                {
                    ServicesHeader += dr["service"].ToString().Trim() + ",";
                }
                ServicesHeader = ServicesHeader.TrimEnd(',');
            }

            return whereServ;
        }

        protected override void CreateTempTable()
        {
            var sql2 = new StringBuilder();
            sql2.Append(" create temp table t_svod( ");
            sql2.Append(" nzp_dom integer,");//Уникальный код дома
            sql2.Append(" adr character(100),");//Уникальный код дома
            sql2.Append(" s1 " + DBManager.sDecimalType + "(14,2),"); //Вхлдящее сальдо
            sql2.Append(" s2 " + DBManager.sDecimalType + "(14,2),"); //Начислено по тарифу
            sql2.Append(" s3 " + DBManager.sDecimalType + "(14,2),"); //Скидка по льготе
            sql2.Append(" s4 " + DBManager.sDecimalType + "(14,2),"); //Начислено с учётом льгот
            sql2.Append(" s5 " + DBManager.sDecimalType + "(14,2),"); //Изменения и перерасчёты
            sql2.Append(" s6 " + DBManager.sDecimalType + "(14,2),"); //Поступления за сальдовый период
            sql2.Append(" s7 " + DBManager.sDecimalType + "(14,2),"); //Положительная часть исходящего сальдо
            sql2.Append(" s8 " + DBManager.sDecimalType + "(14,2),"); //Отрицательная часть исходящего сальдо
            sql2.Append(" s9 " + DBManager.sDecimalType + "(14,2),"); //Исходящее сальдо
            sql2.Append(" s10 " + DBManager.sDecimalType + "(14,2),"); //Задолжность
            sql2.Append(" s11 " + DBManager.sDecimalType + "(14,2))"+ DBManager.sUnlogTempTable); //Процент
            ExecSQL(sql2.ToString());
        }

        protected override void DropTempTable()
        {
            ExecSQL("drop table t_svod");
        }


        protected override void PrepareReport(FastReport.Report report)
        {
            string[] months = {"","Январь","Февраль",
                "Март","Апрель","Май","Июнь","Июль","Август","Сентябрь",
                "Октябрь","Ноябрь","Декабрь"};
            report.SetParameterValue("month", months[Month]);
            report.SetParameterValue("year", Year);
            report.SetParameterValue("su", SupplierHeader);
            report.SetParameterValue("ar", AreaHeader);
            report.SetParameterValue("se","Услуги: " + ServicesHeader);
            //report.SetParameterValue("gr", GroupsHeader);
            report.SetParameterValue("DATE", DateTime.Now.ToShortDateString());
            report.SetParameterValue("TIME", DateTime.Now.ToLongTimeString());
        }

        protected override void PrepareParams()
        {
            Month = UserParamValues["Month"].GetValue<int>();
            Year = UserParamValues["Year"].Value.To<int>();

            Services = UserParamValues["Services"].Value.To<List<int>>();
            Suppliers = UserParamValues["Suppliers"].Value.To<List<long>>();
            Areas = UserParamValues["Areas"].Value.To<List<int>>();

            if (Month == 0)
            {
                throw new ReportException("Не определено значение \"Расчетный месяц\"");
            }

            if (Year == 0)
            {
                throw new ReportException("Не определено значение \"Расчетный год\"");
            }
        }
    }
}
