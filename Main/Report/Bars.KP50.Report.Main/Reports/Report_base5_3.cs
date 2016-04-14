using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.Main.Properties;
using Bars.KP50.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.Report.Main.Reports
{
    public class Report53 : BaseSqlReport
    {
        public override string Name
        {
            get { return "Базовый - Сальдовая ведомость по домам"; }
        }

        public override string Description
        {
            get { return "5.3 Сальдовая ведомость по домам"; }
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
        protected string SuppliersHeader { get; set; }

        /// <summary>Заголовок отчета</summary>
        protected string AreasHeader { get; set; }

        /// <summary>Заголовок отчета</summary>
        protected string ServicesHeader { get; set; }

        /// <summary>Заголовок отчета</summary>
        protected string GroupsHeader { get; set; }

        /// <summary>Банки данных</summary>
        protected List<int> Banks { get; set; }

        public override List<UserParam> GetUserParams()
        {
            var curCalcMonthYear = DBManager.GetCurMonthYear();
            return new List<UserParam>
            {
                new MonthParameter {Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["month_"] : DateTime.Today.Month },
                new YearParameter {Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["yearr"] : DateTime.Today.Year },
                new SupplierAndBankParameter(),
                new ServiceParameter(),
                new AreaParameter()
            };
        }

        public override DataSet GetData()
        {
            IDataReader reader;

            var sql = new StringBuilder();

            //WhereStringForFindCommon(finder, "a", ref whereStr);

            string whereSupp = GetWhereSupp();
            string whereServ = GetWhereServ();
            string whereArea = GetWhereArea();
            string whereGroup = GetWhereGroup();
            //string where_city = "";

            #region выборка в temp таблицу
            sql.Remove(0, sql.Length);
            sql.Append(" SELECT bd_kernel as pref ");
            sql.Append(" FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point ");
            sql.Append(" WHERE nzp_wp>1 " + GetWhereWp());

            ExecRead(out reader, sql.ToString());

            while (reader.Read())
            {
                if (reader["pref"] != null)
                {
                    string pref = reader["pref"].ToString().Trim();

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
                    sql.Append(" FROM " + pref + "_charge_" + (Year - 2000).ToString("00") + DBManager.tableDelimiter + "charge_" + Month.ToString("00") + " a, ");
                    sql.Append(pref + DBManager.sDataAliasRest + "kvar k ");
                    sql.Append(" WHERE a.dat_charge is null AND nzp_supp>0 ");
                    sql.Append(" AND a.nzp_kvar=k.nzp_kvar AND a.nzp_serv>1 ");
                    sql.Append(whereArea + whereServ + whereSupp);
                    sql.Append(" GROUP BY 2,1");

                    ExecSQL(sql.ToString());
                }
            }

            reader.Close();
            #endregion

            sql.Remove(0, sql.Length);
            sql.Append(" SELECT c.nzp_dom, case when rajon='-' then town else trim(town)||', '||trim(rajon) end as rajon, " +
                       " ulica, idom, ndom, ");
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
            sql.Append(" sum(CASE WHEN s4<>0 THEN CAST((((s7-s4)/s4)*100) AS numeric(14,2)) ELSE 0 END) AS s11 ");
            sql.Append(" FROM t_svod c, ");
            sql.Append(ReportParams.Pref + DBManager.sDataAliasRest + "dom d, ");
            sql.Append(ReportParams.Pref + DBManager.sDataAliasRest + "s_rajon sr, ");
            sql.Append(ReportParams.Pref + DBManager.sDataAliasRest + "s_town st, ");
            sql.Append(ReportParams.Pref + DBManager.sDataAliasRest + "s_ulica u");
            sql.Append(" WHERE  u.nzp_raj=sr.nzp_raj AND  sr.nzp_town=st.nzp_town");
            sql.Append(" AND c.nzp_dom=d.nzp_dom AND u.nzp_ul=d.nzp_ul ");
            sql.Append(" GROUP BY 1,2,3,4,5,6 ");
            sql.Append(" ORDER BY 2,3,4,5,6 ");

            DataTable dt = ExecSQLToTable(sql.ToString());
            dt.TableName = "Q_master";
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
            string whereArea = String.Empty;
            whereArea = Areas != null ? Areas.Aggregate(whereArea, (current, nzpArea) => current + (nzpArea + ",")) : ReportParams.GetRolesCondition(Constants.role_sql_area);
            whereArea = whereArea.TrimEnd(',');
            whereArea = !String.IsNullOrEmpty(whereArea) ? " AND k.nzp_area in (" + whereArea + ")" : String.Empty;
            if (!String.IsNullOrEmpty(whereArea))
            {
                string sql = " SELECT area from " + ReportParams.Pref + DBManager.sDataAliasRest + "s_area k  WHERE k.nzp_area > 0 " + whereArea;
                DataTable area = ExecSQLToTable(sql);
                foreach (DataRow dr in area.Rows)
                {
                    AreasHeader += dr["area"].ToString().Trim() + ", ";
                }
                AreasHeader = AreasHeader.TrimEnd(',', ' ');
            }
            return whereArea;
        }

        /// <summary>
        /// Получить условия органичения по поставщикам
        /// </summary>
        /// <returns></returns>
        private string GetWhereSupp()
        {
            string whereSupp = String.Empty;
            whereSupp = Suppliers != null ? Suppliers.Aggregate(whereSupp, (current, nzpSupp) => current + (nzpSupp + ",")) : ReportParams.GetRolesCondition(Constants.role_sql_supp);
            whereSupp = whereSupp.TrimEnd(',');
            whereSupp = !String.IsNullOrEmpty(whereSupp) ? " AND nzp_supp in (" + whereSupp + ")" : String.Empty;
            if (!String.IsNullOrEmpty(whereSupp))
            {
                string sql = " SELECT name_supp from " + ReportParams.Pref + DBManager.sKernelAliasRest + "supplier  WHERE nzp_supp > 0 " + whereSupp;
                DataTable supp = ExecSQLToTable(sql);
                foreach (DataRow dr in supp.Rows)
                {
                    SuppliersHeader += dr["name_supp"].ToString().Trim() + ", ";
                }
                SuppliersHeader = SuppliersHeader.TrimEnd(',', ' ');
            }
            return whereSupp;
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
                string sql = " SELECT service from " + ReportParams.Pref + DBManager.sKernelAliasRest + "services  WHERE nzp_serv > 0 " + whereServ;
                DataTable serv = ExecSQLToTable(sql);
                foreach (DataRow dr in serv.Rows)
                {
                    ServicesHeader += dr["service"].ToString().Trim() + ", ";
                }
                ServicesHeader = ServicesHeader.TrimEnd(',', ' ');

            }
            return whereServ;
        }

        /// <summary>
        /// Получить условия органичения по группам
        /// </summary>
        /// <returns></returns>
        private string GetWhereGroup()
        {
            string whereGroup = String.Empty;
            whereGroup = Groups != null ? Groups.Aggregate(whereGroup, (current, nzpGroup) => current + (nzpGroup + ",")) : "";
            whereGroup = whereGroup.TrimEnd(',');
            whereGroup = !String.IsNullOrEmpty(whereGroup) ? " AND nzp_group in (" + whereGroup + ")" : String.Empty;
            if (!String.IsNullOrEmpty(whereGroup))
            {
                string sql = " SELECT ngroup from " + ReportParams.Pref + DBManager.sDataAliasRest + "s_group WHERE nzp_group > 0 " + whereGroup;
                DataTable group = ExecSQLToTable(sql);
                foreach (DataRow dr in group.Rows)
                {
                    GroupsHeader += dr["ngroup"].ToString().Trim() + ", ";
                }
                GroupsHeader = GroupsHeader.TrimEnd(',', ' ');
            }
            return whereGroup;
        }

        private string GetWhereWp()
        {
            string whereWp = String.Empty;
            if (Banks != null)
            {
                whereWp = Banks.Aggregate(whereWp, (current, nzpWp) => current + (nzpWp + ","));
            }
            else
            {
                whereWp = ReportParams.GetRolesCondition(Constants.role_sql_wp);
            }
            whereWp = whereWp.TrimEnd(',');
            whereWp = !String.IsNullOrEmpty(whereWp) ? " AND nzp_wp in (" + whereWp + ")" : String.Empty;
            return whereWp;
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
            report.SetParameterValue("su", SuppliersHeader);
            report.SetParameterValue("ar", AreasHeader);
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
            Areas = UserParamValues["Areas"].Value.To<List<int>>();

            var values = JsonConvert.DeserializeObject<Dictionary<string, object>>(UserParamValues["SupplierAndBank"].GetValue<string>());
            Suppliers = values["Streets"] != null
                ? values["Streets"].To<JArray>().Select(x => x.Value<long>()).ToList()
                : null;
            Banks = values["Raions"] != null
                ? values["Raions"].To<JArray>().Select(x => x.Value<int>()).ToList()
                : null;

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
