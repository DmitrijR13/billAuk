using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using Bars.KP50.Report.Base;
using STCLINE.KP50.DataBase;
using Bars.KP50.Report.RT.Properties;

namespace Bars.KP50.Report.RT.Reports
{
    class Report1683 : BaseSqlReport
    {
        public override string Name
        {
            get { return "16.8.3 Справка по начислениям"; }
        }

        public override string Description
        {
            get { return "Справка по начислениям на лицевой счет за период"; }
        }

        public override IList<ReportGroup> ReportGroups
        {
            get
            {
                List<ReportGroup> result = new List<ReportGroup>();
                result.Add(ReportGroup.Cards);
                return result;
            }
        }

        public override bool IsPreview
        {
            get { return false; }
        }

        protected override byte[] Template
        {
            get { return Resources.Web_sprav_nach; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.LC }; }
        }

        /// <summary>
        /// Месяц с 
        /// </summary>
        protected int Month_s { get; set; }

        /// <summary>
        /// Месяц по 
        /// </summary>
        protected int Month_po { get; set; }

        /// <summary>
        /// Год с 
        /// </summary>
        protected int Year_s { get; set; }

        /// <summary>
        /// Год по 
        /// </summary>
        protected int Year_po { get; set; }

        /// <summary>
        /// Фамилия
        /// </summary>
        protected string fio { get; set; }
        /// <summary>
        /// Адрес
        /// </summary>
        protected string Adres { get; set; }

        public override List<UserParam> GetUserParams()
        {
            var curCalcMonthYear = DBManager.GetCurMonthYear();
            DateTime datS = curCalcMonthYear != null
                ? new DateTime(Convert.ToInt32(curCalcMonthYear.Rows[0]["yearr"]),
                    Convert.ToInt32(curCalcMonthYear.Rows[0]["month_"]), 1)
                : DateTime.Now;
            DateTime datPo = curCalcMonthYear != null
                ? datS.AddMonths(1).AddDays(-1)
                : DateTime.Now;
            return new List<UserParam>
            {
                new PeriodParameter(datS, datPo),
            };
        }

        public override DataSet GetData()
        {

            var sql = new StringBuilder();
            MyDataReader reader = new MyDataReader();


            if (!String.IsNullOrEmpty(ReportParams.NzpObject.ToString()))
            {
                sql.Remove(0, sql.Length);
                sql.Append("select fio from " + ReportParams.Pref + DBManager.sDataAliasRest + " kvar where num_ls = " + ReportParams.NzpObject);
                DataTable fioTable = ExecSQLToTable(sql.ToString());
                fio = fioTable.Rows[0]["fio"].ToString().Trim();

                sql.Remove(0, sql.Length);
                sql.Append("select town, rajon ,ulica, ndom, nkor, nkvar " +
                    " from " + ReportParams.Pref + DBManager.sDataAliasRest + "kvar k, " +
                    ReportParams.Pref + DBManager.sDataAliasRest + "dom d, " +
                    ReportParams.Pref + DBManager.sDataAliasRest + "s_ulica u, " +
                    ReportParams.Pref + DBManager.sDataAliasRest + "s_rajon r, " +
                    ReportParams.Pref + DBManager.sDataAliasRest + "s_town t " +
                    " where k.nzp_dom = d.nzp_dom and d.nzp_ul = u.nzp_ul and u.nzp_raj = r.nzp_raj and r.nzp_town = t.nzp_town " +
                    " and num_ls = " + ReportParams.NzpObject
                    );
                DataTable AdresTable = ExecSQLToTable(sql.ToString());
                Adres = AdresTable.Rows[0]["town"].ToString().Trim();
                Adres += AdresTable.Rows[0]["rajon"].ToString().Trim();
                Adres += " Ул. " + AdresTable.Rows[0]["ulica"].ToString().Trim();
                Adres += " д. " + AdresTable.Rows[0]["ndom"].ToString().Trim();
                if (AdresTable.Rows[0]["nkor"].ToString().Trim() != "0" && AdresTable.Rows[0]["nkor"].ToString().Trim() != "-")
                {
                    Adres += " корп. " + AdresTable.Rows[0]["nkor"].ToString().Trim();
                }
                if (AdresTable.Rows[0]["nkvar"].ToString().Trim() != "0" && AdresTable.Rows[0]["nkvar"].ToString().Trim() != "-")
                {
                    Adres += " кв. " + AdresTable.Rows[0]["nkvar"].ToString().Trim();
                }
            }

            sql.Remove(0, sql.Length);
            sql.Append(" select bd_kernel as pref ");
            sql.AppendFormat(" from {0}{1}s_point ", DBManager.GetFullBaseName(Connection), DBManager.tableDelimiter);
            sql.Append(" where nzp_wp>1 ");
            ExecRead(out reader, sql.ToString());

            while(reader.Read())
            {
                string pref = reader["pref"].ToString().Trim();
                for (int i = Year_s * 12 + Month_s; i < Year_po * 12 + Month_po + 1; i++)
                {
                    int year_ = i / 12;
                    int month_ = i % 12;
                    if (month_ == 0)
                    {
                        year_--;
                        month_ = 12;
                    }
                    string chargeXX = pref + "_charge_" + (year_ - 2000).ToString("00") +
                            DBManager.tableDelimiter + "charge_" + month_.ToString("00");

                    sql.Remove(0, sql.Length);
                    sql.Append("insert into t_sprav_nach " +
                        " ( month_, year_, sum_insaldo, sum_tarif, sum_real, reval, " +
                        " sum_money, real_charge,  sum_outsaldo, sum_charge ) " +
                        " select '" + month_.ToString("00") + "' as month_, '" + year_ + "' as year_, sum (sum_insaldo) as sum_insaldo, sum(sum_tarif) as sum_tarif," +
                        " sum(sum_real) as sum_real, sum(reval) reval, sum(sum_money) as sum_money, sum(real_charge) as real_charge," +
                        " sum(sum_outsaldo) as sum_outsaldo, sum(sum_charge) as sum_charge  " +
                        " from " + chargeXX +
                        " where nzp_kvar = " + ReportParams.NzpObject +
                        " and dat_charge is null " +
                        " and nzp_serv > 1 " +
                        " group by 1,2 "
                        );
                    ExecSQL(sql.ToString());
                }
            }

            sql.Remove(0, sql.Length);
            sql.Append(" Select month_, year_, sum_insaldo, sum_tarif, sum_real, reval, sum_money, real_charge, sum_outsaldo, sum_charge " +
                " from t_sprav_nach ");

            DataTable dt = ExecSQLToTable(sql.ToString());
            dt.TableName = "Q_master";

            DataSet ds = new DataSet();
            ds.Tables.Add(dt);
            return ds;
        }

        protected override void PrepareReport(FastReport.Report report)
        {
            report.SetParameterValue("Lschet", ReportParams.NzpObject.ToString());
            report.SetParameterValue("fio", fio);
            report.SetParameterValue("Adres", Adres);
            report.SetParameterValue("m1", Month_s.ToString("00"));
            report.SetParameterValue("y1", Year_s);
            report.SetParameterValue("m2", Month_po.ToString("00"));
            report.SetParameterValue("y2", Year_po);
            report.SetParameterValue("printDate", DateTime.Now.ToLongDateString());
            report.SetParameterValue("printTime", DateTime.Now.ToLongTimeString());
        }

        protected override void PrepareParams()
        {
            DateTime begin;
            DateTime end;
            string period = UserParamValues["Period"].GetValue<string>();
            PeriodParameter.GetValues(period, out begin, out end);
            Month_s = begin.Month;
            Month_po = end.Month;
            Year_s = begin.Year;
            Year_po = end.Year;
        }

        protected override void CreateTempTable()
        {
            var sql = new StringBuilder();
            sql.Append("create temp table t_sprav_nach" +
                " (month_ char(2), " +
                " year_ char(4), " +
                " sum_insaldo " + DBManager.sDecimalType + "(14,2)," +
                " sum_tarif " + DBManager.sDecimalType + "(14,2)," +
                " sum_real " + DBManager.sDecimalType + "(14,2)," +
                " reval " + DBManager.sDecimalType + "(14,2)," +
                " sum_money " + DBManager.sDecimalType + "(14,2)," +
                " real_charge " + DBManager.sDecimalType + "(14,2)," +
                " sum_outsaldo " + DBManager.sDecimalType + "(14,2)," +
                " sum_charge " + DBManager.sDecimalType + "(14,2))" + DBManager.sUnlogTempTable
                );
            ExecSQL(sql.ToString());
        }

        protected override void DropTempTable()
        {
            ExecSQL("drop table t_sprav_nach");
        }
    }
}
