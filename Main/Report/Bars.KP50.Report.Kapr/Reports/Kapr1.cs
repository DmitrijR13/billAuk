namespace Bars.KP50.Report.Kapr
{
    using Bars.KP50.Report.Base;
    using STCLINE.KP50.DataBase;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Text;
    using FastReport;
    using Bars.KP50.Report.Kapr.Properties;

    public class Kapr1 : BaseSqlReport
    {
        public override string Name
        {
            get { return "Сводный отчет по поступлениям по Капитальному ремонту"; }
        }

        public override string Description
        {
            get { return "Сводный отчет по поступлениям по Капитальному ремонту"; }
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

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base }; }
        }
        public override bool IsPreview
        {
            get { return false; }
        }

        protected override byte[] Template
        {
            get { return Resources.Kapr_1; }
        }


        /// <summary>Расчетный месяц</summary>
        protected int Month { get; set; }

        /// <summary>Расчетный год</summary>
        protected int Year { get; set; }


        public override List<UserParam> GetUserParams()
        {
            var curCalcMonthYear = DBManager.GetCurMonthYear();
            return new List<UserParam>
            {
                new MonthParameter {Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["month_"] : DateTime.Today.Month },
                new YearParameter {Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["yearr"] : DateTime.Today.Year },
            };
        }

        public override DataSet GetData()
        {

            MyDataReader reader;


            var sql = new StringBuilder();


            sql.Remove(0, sql.Length);
            sql.Append(" SELECT * ");
            sql.Append(" FROM  " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point ");
            sql.Append(" WHERE nzp_wp>1 ");

            ExecRead(out reader, sql.ToString());

            while (reader.Read())
            {
                var pref = reader["bd_kernel"].ToString().ToLower().Trim();

                var chargeXX = pref + "_charge_" + (Year - 2000).ToString("00") + DBManager.tableDelimiter + "charge_" + Month.ToString("00");

                sql.Remove(0, sql.Length);
                sql.Append(" INSERT INTO t_nach (nzp_ul, sum_insaldo, ");
                sql.Append(" sum_tarif, real_charge, reval, sum_money, sum_outsaldo) ");
                sql.Append(" SELECT nzp_ul, sum(sum_insaldo), sum(sum_tarif),  ");
                sql.Append(" 	    sum(reval), sum(real_charge),  ");
                sql.Append(" 	    sum(sum_money), sum(sum_outsaldo)  ");
                sql.Append(" FROM " + chargeXX + " a, " + 
                    pref + DBManager.sDataAliasRest + "kvar k, "+
                    pref + DBManager.sDataAliasRest + "dom d ");
                sql.Append(" WHERE a.nzp_kvar=k.nzp_kvar and k.nzp_dom=d.nzp_dom ");
                sql.Append("        AND dat_charge is null ");
                sql.Append("        AND a.nzp_serv>1 ");
                //sql.Append(where_adr + where_supp + where_serv);
                sql.Append(" GROUP BY  1            ");

                ExecSQL(sql.ToString());
            }

            reader.Close();

            sql.Remove(0, sql.Length);
            sql.Append(" SELECT st.town, sr.rajon,  ");
            sql.Append("         sum(sum_insaldo) as sum_insaldo, sum(sum_tarif) as sum_tarif,  ");
            sql.Append("         sum(reval) as reval, sum(real_charge) as real_charge,  ");
            sql.Append("         sum(sum_money) as sum_money, sum(sum_outsaldo) as sum_outsaldo ");
            sql.Append(" FROM t_nach t, ");
            sql.Append(ReportParams.Pref + DBManager.sDataAliasRest + "s_ulica su, ");
            sql.Append(ReportParams.Pref + DBManager.sDataAliasRest + "s_rajon sr, ");
            sql.Append(ReportParams.Pref + DBManager.sDataAliasRest + "s_town st ");
            sql.Append(" WHERE t.nzp_ul = su.nzp_ul ");
            sql.Append("        AND su.nzp_raj = sr.nzp_raj ");
            sql.Append("        AND st.nzp_town = sr.nzp_town ");
            sql.Append(" GROUP BY 1,2 ");
            sql.Append(" ORDER BY 1,2 ");

            DataTable dt = ExecSQLToTable(sql.ToString());
            dt.TableName = "Q_master";


            var ds = new DataSet();
            ds.Tables.Add(dt);

            return ds;
        }

        protected override void PrepareReport(Report report)
        {
            string[] months = new string[] {"","Январь","Февраль",
                 "Март","Апрель","Май","Июнь","Июль","Август","Сентябрь",
                 "Октябрь","Ноябрь","Декабрь"};
            report.SetParameterValue("month", months[Month]);
            report.SetParameterValue("year", Year);
        }

        protected override void PrepareParams()
        {
            Month = UserParamValues["Month"].GetValue<int>();
            Year = UserParamValues["Year"].GetValue<int>();


            if (Month == 0)
            {
                throw new ReportException("Не определено значение \"Расчетный месяц\"");
            }

            if (Year == 0)
            {
                throw new ReportException("Не определено значение \"Расчетный год\"");
            }
        }

        protected override void CreateTempTable()
        {
            var sql = new StringBuilder();
            sql.Remove(0, sql.Length);
            sql.Append(" create temp table t_nach (     ");
            sql.Append(" nzp_ul integer default 0,");
            sql.Append(" sum_insaldo " + DBManager.sDecimalType + "(14,2) default 0.00, ");
            sql.Append(" sum_tarif " + DBManager.sDecimalType + "(14,2) default 0.00, ");
            sql.Append(" reval " + DBManager.sDecimalType + "(14,2) default 0.00, ");
            sql.Append(" real_charge " + DBManager.sDecimalType + "(14,2) default 0.00, ");
            sql.Append(" sum_money " + DBManager.sDecimalType + "(14,2) default 0.00, ");
            sql.Append(" sum_outsaldo " + DBManager.sDecimalType + "(14,2) default 0.00 ");
            sql.Append(" ) " + DBManager.sUnlogTempTable);

            ExecSQL(sql.ToString());
        }

        protected override void DropTempTable()
        {
            ExecSQL(" drop table t_nach ", true);
        }
    }
}
