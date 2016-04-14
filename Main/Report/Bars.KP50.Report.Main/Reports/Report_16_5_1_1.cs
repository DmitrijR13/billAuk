namespace Bars.KP50.Report.Main
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Text;
    using Report;
    using Base;
    using Properties;
    using Utils;

    using FastReport;

    using STCLINE.KP50.DataBase;

    class ServiceSum : BaseSqlReport
    {
        public override string Name
        {
            get { return "16.5.1.1. Сумма услуг по ЖЭУ"; }
        }

        public override string Description
        {
            get { return "5.1.1. Сумма услуг по ЖЭУ"; }
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

        /// <summary>Расчетный месяц</summary>
        protected int Month { get; set; }

        /// <summary>Расчетный год</summary>
        protected int Year { get; set; }

        public override List<UserParam> GetUserParams()
        {
            return new List<UserParam>
            {
                new MonthParameter{ Value = DateTime.Now.Month },
                new YearParameter{ Value = DateTime.Now.Year },   
            };
        }

        protected override void PrepareReport(Report report)
        {
            var months = new[] { "","Январь", "Февраль", "Март",
            "Апрель", "Май", "Июнь", "Июль", "Август", "Сентябрь",
            "Октябрь","Ноябрь","Декабрь"}; 
            report.SetParameterValue("printDate", DateTime.Now.ToLongDateString());
            report.SetParameterValue("printTime", DateTime.Now.ToLongTimeString());
            report.SetParameterValue("month", months[Month]);
            report.SetParameterValue("year", Year);
        }

        protected override byte[] Template
        {
            get { return Resources.Report_16_5_1_1; }
        }

        public override ReportKind ReportKind
        {
            get { return ReportKind.Base; }
        }

        protected override void PrepareParams()
        {
            Month = UserParamValues["Month"].GetValue<int>();
            Year = UserParamValues["Year"].GetValue<int>();
        }

        public override DataSet GetData()
        {
            var sql = new StringBuilder();
            MyDataReader reader;
            sql.Remove(0, sql.Length);
            sql.Append(" select bd_kernel as pref ");
            sql.AppendFormat(" from {0}{1}s_point ", DBManager.GetFullBaseName(Connection), DBManager.tableDelimiter);
            sql.Append(" where nzp_wp>1 ");

            ExecRead(out reader, sql.ToString());
            while (reader.Read())
            {
                string pref = reader["pref"].ToStr().Trim();
                sql.Remove(0, sql.Length);
                sql.Append(" insert into service_sum (nzp_kvar, nzp_supp, nzp_serv, dat_charge, sum_insaldo, sum_real, real_charge, reval, sum_reval, sum_money, sum_outsaldo) " +
                    " select nzp_kvar, nzp_supp, nzp_serv, dat_charge, " +
                    " sum_insaldo, " +
                    " sum_real, " +
                    " real_charge, " +
                    " reval, " +
                    " (sum_real+real_charge+reval) as sum_reval, " +
                    " sum_money, " +
                    " sum_outsaldo " +
                    " from  " + pref + "_charge_" + (Year-2000).ToString("00") + DBManager.tableDelimiter + "charge_" + Month.ToString("00") +" ch ");
                ExecSQL(sql.ToString());
            }

            reader.Close();
            sql.Remove(0, sql.Length);
            sql.Append(" select geu, name_supp, service, " + 
                       " sum(sum_insaldo) as sum_insaldo, " +
                       " sum(sum_real) as sum_real, " +
                       " sum(real_charge) as real_charge, " +
                       " sum(reval) as reval, " +
                       " sum(sum_reval) as sum_reval, " +
                       " sum(sum_money) as sum_money, " +
                       " sum(sum_outsaldo) as sum_outsaldo " +
                       " from service_sum ch, " +
                         ReportParams.Pref + DBManager.sKernelAliasRest + "supplier su, " +
                         ReportParams.Pref + DBManager.sKernelAliasRest + "services se, " +
                         ReportParams.Pref + DBManager.sDataAliasRest + "kvar k, " +
                         ReportParams.Pref + DBManager.sDataAliasRest + "s_geu g  " +
                       " where su.nzp_supp = ch.nzp_supp " +
                       " and se.nzp_serv = ch.nzp_serv " +
                       " and ch.nzp_kvar = k.nzp_kvar " +
                       " and k.nzp_geu = g.nzp_geu " +
                       " and ch.nzp_serv>1 " +
                       " and ch.nzp_supp>1 " +
                       " and ch.dat_charge is null " +
                       " group by 1,2,3 " +
                       " order by 1,2,3 ");
            DataTable dt = ExecSQLToTable(sql.ToString());
            dt.TableName = "Q_master";
            var ds = new DataSet();
            ds.Tables.Add(dt);

            return ds;
        }

        protected override void CreateTempTable()
        {
            var sql = new StringBuilder();
            sql.Append("create temp table service_sum (" +
                " nzp_kvar integer, " +
                " nzp_supp integer, " +
                " nzp_serv integer, " +
                " dat_charge date, " +
                " sum_insaldo " + DBManager.sDecimalType + "(14,2), " +
                " sum_real " + DBManager.sDecimalType + "(14,2), " +
                " real_charge " + DBManager.sDecimalType + "(14,2), " +
                " reval " + DBManager.sDecimalType + "(14,2), " +
                " sum_reval " + DBManager.sDecimalType + "(14,2), " +
                " sum_money " + DBManager.sDecimalType + "(14,2), " +
                " sum_outsaldo " + DBManager.sDecimalType + "(14,2) " +
                ")");
            ExecSQL(sql.ToString());
        }

        protected override void DropTempTable()
        {
            ExecSQL(" drop table service_sum ");
        }

    }
}
