namespace Bars.KP50.Report.Main
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Data;
    using System.Text;
    using Bars.KP50.Report;
    using Bars.KP50.Report.Base;
    using Bars.KP50.Report.Main.Properties;
    using Bars.KP50.Utils;

    using FastReport;

    using STCLINE.KP50.DataBase;
    using STCLINE.KP50.Global;
    class Web_s_nodolg : BaseSqlReport
    {
        public override string Name
        {
            get { return "16.8.6 Справка о задолженности"; }
        }

        public override string Description
        {
            get { return "Справка о задолженности"; }
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
            get { return Resources.Web_s_nodolg; }
        }

        public override ReportKind ReportKind
        {
            get { return ReportKind.LC; }
        }


        /// <summary>
        /// Паспортные данные или реквизиты организации
        /// </summary>
        protected string pass { get; set; }

        /// <summary>
        /// ФИО жильца
        /// </summary>
        protected string fio { get; set; }

        /// <summary>
        /// Адрес
        /// </summary>
        protected string Adres { get; set; }

        /// <summary>
        /// Информация о долге
        /// </summary>
        protected string ne { get; set; }

        public override List<UserParam> GetUserParams()
        {
            return new List<UserParam>
            {
                new StringParameter{ Code = "pass", Name = "Паспортные данные"}
            };
        }

        protected override void CreateTempTable()
        {
            var sql = new StringBuilder();
            sql.Append(" create temp table t_sum_dolg (sum_dolg " + DBManager.sDecimalType + "(14,2))");
            ExecSQL(sql.ToString());
        }

        protected override void DropTempTable()
        {
            var sql = new StringBuilder();
            sql.Append(" drop table t_sum_dolg ");
            ExecSQL(sql.ToString());
        }

        protected override void PrepareParams()
        {
            pass = UserParamValues["pass"].GetValue<string>();
        }

        protected override void PrepareReport(Report report)
        {
            report.SetParameterValue("fio", fio);
            report.SetParameterValue("Adres", Adres);
            report.SetParameterValue("ne", ne);
            report.SetParameterValue("pasp_string", pass);
            report.SetParameterValue("Date", DateTime.Now.ToLongDateString() + "г. " + DateTime.Now.ToLongTimeString());
        }

        public override DataSet GetData()
        {
            var sql = new StringBuilder();
            MyDataReader reader = new MyDataReader();

            if (!String.IsNullOrEmpty(ReportParams.NzpObject.ToString()))
            {
                sql.Remove(0, sql.Length);
                sql.Append("select fio from " + ReportParams.Pref + DBManager.sDataAliasRest + "kvar where num_ls = " + ReportParams.NzpObject);
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

                sql.Remove(0, sql.Length);
                sql.Append(" select bd_kernel as pref ");
                sql.AppendFormat(" from {0}{1}s_point ", DBManager.GetFullBaseName(Connection), DBManager.tableDelimiter);
                sql.Append(" where nzp_wp>1 ");
                ExecRead(out reader, sql.ToString());

                while (reader.Read())
                {
                    string pref = reader["pref"].ToString().Trim();
                    string chargeXX = pref + "_charge_" + (DateTime.Now.Year - 2000).ToString("00") +
                            DBManager.tableDelimiter + "charge_" + DateTime.Now.Month.ToString("00");

                    sql.Remove(0, sql.Length);
                    sql.Append("insert into t_sum_dolg " +
                        " ( sum_dolg ) " +
                        " select (sum_outsaldo - sum_real) as sum_dolg " +
                        " from " + chargeXX +
                        " where nzp_kvar = " + ReportParams.NzpObject
                        );
                    ExecSQL(sql.ToString());

                }
                sql.Remove(0, sql.Length);
                sql.Append("select sum(sum_dolg) as sum_dolg from t_sum_dolg");
                DataTable dt  = ExecSQLToTable(sql.ToString());
                ne = dt.Rows[0]["sum_dolg"].ToString().Trim();
                decimal isDolg = Convert.ToDecimal(ne);
                if (isDolg > 0)
                {
                    ne = "имеет задолжность в размере " + ne + " рублей";
                }
                else
                {
                    ne = "не имеет задолженности";
                }
            }

            return new DataSet();
        }
    }
}