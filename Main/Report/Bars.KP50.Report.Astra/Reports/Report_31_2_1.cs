using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Text;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.Astra.Properties;
using STCLINE.KP50.DataBase;

namespace Bars.KP50.Report.Astra.Reports
{
    class Report3121 : BaseSqlReportWithDates
    {
        public override string Name
        {
            get { return "31.2.1 Справка о задолженности"; }
        }

        public override string Description
        {
            get { return "Справка о задолженности"; }
        }

        public override IList<ReportGroup> ReportGroups
        {
            get
            {
                List<ReportGroup> result = new List<ReportGroup> { ReportGroup.Cards };
                return result;
            }
        }

        public override bool IsPreview
        {
            get { return false; }
        }

        protected override byte[] Template
        {
            get { return Resources.Report_31_2_1; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.LC }; }
        }


        /// <summary>
        /// Паспортные данные или реквизиты организации
        /// </summary>
        protected string InfoPass { get; set; }

        /// <summary>
        /// ФИО жильца
        /// </summary>
        protected string Fio { get; set; }

        /// <summary>
        /// Адрес
        /// </summary>
        protected string Adres { get; set; }

        /// <summary>
        /// Информация о долге
        /// </summary>
        protected string InfoDolg { get; set; }

        public override List<UserParam> GetUserParams()
        {
            return new List<UserParam>
            {
                new StringParameter{ Code = "pass", Name = "Паспортные данные"}
            };
        }

        protected override void PrepareParams()
        {
            InfoPass = UserParamValues["pass"].GetValue<string>();
        }

        protected override void PrepareReport(FastReport.Report report)
        {
            report.SetParameterValue("fio", Fio);
            report.SetParameterValue("Adres", Adres);
            report.SetParameterValue("ne", InfoDolg);
            report.SetParameterValue("pasp_string", InfoPass);
            report.SetParameterValue("Date", DateTime.Now.ToLongDateString() + "г. " + DateTime.Now.ToLongTimeString());
        }

        public override DataSet GetData()
        {
            string sql;
            MyDataReader reader;

            if (!String.IsNullOrEmpty(ReportParams.NzpObject.ToString(CultureInfo.InvariantCulture)))
            {
                sql = " SELECT fio " +
                      " FROM " + ReportParams.Pref + DBManager.sDataAliasRest + "kvar " +
                      " WHERE num_ls = " + ReportParams.NzpObject;
                DataTable fioTable = ExecSQLToTable(sql);
                Fio = fioTable.Rows[0]["fio"].ToString().Trim();

                sql = " SELECT town, rajon ,ulica, ndom, nkor, nkvar " +
                      " FROM " + ReportParams.Pref + DBManager.sDataAliasRest + "kvar k, " +
                                 ReportParams.Pref + DBManager.sDataAliasRest + "dom d, " +
                                 ReportParams.Pref + DBManager.sDataAliasRest + "s_ulica u, " +
                                 ReportParams.Pref + DBManager.sDataAliasRest + "s_rajon r, " +
                                 ReportParams.Pref + DBManager.sDataAliasRest + "s_town t " +
                      " WHERE k.nzp_dom = d.nzp_dom " +
                        " AND d.nzp_ul = u.nzp_ul " +
                        " AND u.nzp_raj = r.nzp_raj " +
                        " AND r.nzp_town = t.nzp_town " +
                        " AND num_ls = " + ReportParams.NzpObject;
                DataTable adresTable = ExecSQLToTable(sql);
                Adres = adresTable.Rows[0]["town"].ToString().Trim();
                Adres += ", " + adresTable.Rows[0]["rajon"].ToString().Trim();
                Adres += ", Ул. " + adresTable.Rows[0]["ulica"].ToString().Trim();
                Adres += ", д. " + adresTable.Rows[0]["ndom"].ToString().Trim();
                if (adresTable.Rows[0]["nkor"].ToString().Trim() != "0" && adresTable.Rows[0]["nkor"].ToString().Trim() != "-")
                {
                    Adres += " корп. " + adresTable.Rows[0]["nkor"].ToString().Trim();
                }
                if (adresTable.Rows[0]["nkvar"].ToString().Trim() != "0" && adresTable.Rows[0]["nkvar"].ToString().Trim() != "-")
                {
                    Adres += " кв. " + adresTable.Rows[0]["nkvar"].ToString().Trim();
                }

                sql = " SELECT pref AS pref " +
                      " FROM " + ReportParams.Pref + DBManager.sDataAliasRest + "kvar " +
                      " WHERE num_ls = " + ReportParams.NzpObject;
                ExecRead(out reader, sql);

                while (reader.Read())
                {
                    var d = GetCurSaldoDay();
                    DateTime Operday = d == null ? DateTime.Now : Convert.ToDateTime(d.Rows[0]["dat_oper"]);
                    string pref = reader["pref"].ToString().Trim();
                    string chargeXx = pref + "_charge_" + (DateTime.Now.Year - 2000).ToString("00") +
                            DBManager.tableDelimiter +"charge_" + Operday.Month.ToString("00");
                    if (TempTableInWebCashe(chargeXx))
                    {

                        sql = " INSERT INTO t_sum_dolg ( sum_dolg ) " +
                              " SELECT (sum_outsaldo - sum_real) AS sum_dolg " +
                              " FROM " + chargeXx +
                              " WHERE dat_charge is null and nzp_serv>1 and nzp_kvar = " + ReportParams.NzpObject;
                    }
                    ExecSQL(sql);
                }

                sql = " SELECT SUM(sum_dolg) AS sum_dolg FROM t_sum_dolg ";
                DataTable dt = ExecSQLToTable(sql);
                InfoDolg = dt.Rows[0]["sum_dolg"].ToString().Trim();
                decimal isDolg;
                if (!Decimal.TryParse(InfoDolg, out isDolg)) isDolg = 0;
                if (isDolg > 0)
                {
                    InfoDolg = "имеет задолжность в размере " + InfoDolg + " рублей";
                }
                else
                {
                    InfoDolg = "не имеет задолженности";
                }
            }

            return new DataSet();
        }

        protected override void CreateTempTable()
        {
            var sql = new StringBuilder();
            sql.Append(" CREATE TEMP TABLE t_sum_dolg (sum_dolg " + DBManager.sDecimalType + "(14,2))");
            ExecSQL(sql.ToString());
        }

        protected override void DropTempTable()
        {
            var sql = new StringBuilder();
            sql.Append(" drop table t_sum_dolg ");
            ExecSQL(sql.ToString());
        }

    }
}