using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Text;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.RSO.Properties;
using STCLINE.KP50.DataBase;

namespace Bars.KP50.Report.RSO.Reports
{
    class Report1525 : BaseSqlReport
    {
        public override string Name
        {
            get { return "15.2.5 Справка о задолженности"; }
        }

        public override string Description
        {
            get { return "Справка о задолженности"; }
        }

        public override IList<ReportGroup> ReportGroups
        {
            get
            {
                var result = new List<ReportGroup> { ReportGroup.Cards };
                return result;
            }
        }

        public override bool IsPreview
        {
            get { return false; }
        }

        protected override byte[] Template
        {
            get { return Resources.Report_15_2_5; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.LC }; }
        }

        private static string _fio, _address, _infoDolg;

        /// <summary> ФИО жильца </summary>
        protected string Fio
        {
            get
            {
                return _fio;
            }
            set
            {
                _fio = string.IsNullOrEmpty(value) ? "__________________" : value;
            }
        }

        /// <summary> Адрес </summary>
        protected string Adres
        {
            get
            {
                return _address;
            }
            set
            {
                _address = string.IsNullOrEmpty(value) ? "_____________________" : value;
            }
        }

        /// <summary> Должность пасспортистки </summary>
        private string PostPassport { get; set; }

        public override List<UserParam> GetUserParams()
        {
            return new List<UserParam>();
        }

        protected override void PrepareParams(){}

        protected override void PrepareReport(FastReport.Report report)
        {
            report.SetParameterValue("pers_name", Fio);
            report.SetParameterValue("address", Adres);
            report.SetParameterValue("post_pasport", PostPassport);
            report.SetParameterValue("name_pasport", STCLINE.KP50.Global.Utils.GetCorrectFIO(ReportParams.User.uname));
            report.SetParameterValue("date", DateTime.Now.ToLongDateString());
        }

        public override DataSet GetData()
        {
            string sql;
            if (!String.IsNullOrEmpty(ReportParams.NzpObject.ToString(CultureInfo.InvariantCulture)))
            {
                sql = " SELECT nzp_kvar, fio, pref " +
                      " FROM " + ReportParams.Pref + DBManager.sDataAliasRest + "kvar " +
                      " WHERE num_ls = " + ReportParams.NzpObject;
                DataTable fioTable = ExecSQLToTable(sql);
                if (fioTable.Rows.Count != 0)
                {
                    string pref = fioTable.Rows[0]["pref"].ToString().Trim();
                    string chargeXX = pref + "_charge_" + (DateTime.Now.Year - 2000).ToString("00") +
                                      DBManager.tableDelimiter + "charge_" + DateTime.Now.Month.ToString("00");

                    Fio = fioTable.Rows[0]["fio"].ToString().Trim();    //ФИО должника

                    #region Адрес должника

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
                    if (adresTable.Rows.Count != 0)
                    {
                        Adres = adresTable.Rows[0]["town"].ToString().Trim();
                        Adres += ", " + adresTable.Rows[0]["rajon"].ToString().Trim();
                        Adres += ", Ул. " + adresTable.Rows[0]["ulica"].ToString().Trim();
                        Adres += ", д. " + adresTable.Rows[0]["ndom"].ToString().Trim();
                        if (adresTable.Rows[0]["nkor"].ToString().Trim() != "0" &&
                            adresTable.Rows[0]["nkor"].ToString().Trim() != "-")
                        {
                            Adres += " корп. " + adresTable.Rows[0]["nkor"].ToString().Trim();
                        }
                        if (adresTable.Rows[0]["nkvar"].ToString().Trim() != "0" &&
                            adresTable.Rows[0]["nkvar"].ToString().Trim() != "-")
                        {
                            Adres += " кв. " + adresTable.Rows[0]["nkvar"].ToString().Trim();
                        }
                    }

                    #endregion

                    #region Должность пасспортистки

                    sql = " SELECT val_prm" +
                          " FROM " + ReportParams.Pref + DBManager.sDataAliasRest + "prm_10 " +
                          " WHERE is_actual = 1" +
                          " AND nzp_prm = 578 " +
                          " AND dat_s <= '" + DateTime.Now.ToShortDateString() + "' " +
                          " AND dat_po >= '" + DateTime.Now.ToShortDateString() + "' ";
                    DataTable postTable = ExecSQLToTable(sql);
                    if (postTable.Rows.Count != 0)
                        PostPassport = postTable.Rows[0]["val_prm"].ToString().TrimEnd();

                    #endregion

                    #region Сумма долга
                    sql = " INSERT INTO t_report_15_2_5 (service, rsum_tarif, sum_dolg ) " +
                          " SELECT service, MAX(rsum_tarif) AS rsum_tarif, MAX(sum_outsaldo - sum_real) AS sum_dolg " +
                          " FROM " + chargeXX + " c INNER JOIN " + pref + DBManager.sDataAliasRest + "kvar k ON k.nzp_kvar = c.nzp_kvar " +
                                                  " INNER JOIN " + pref + DBManager.sKernelAliasRest + "services s ON s.nzp_serv = c.nzp_serv " +
                          " WHERE k.num_ls = " + ReportParams.NzpObject +
                          " GROUP BY 1 ";
                    ExecSQL(sql);
                    #endregion 
                }
            }

            sql = " SELECT TRIM(service) AS service, rsum_tarif, sum_dolg  FROM t_report_15_2_5 ORDER BY 1 ";
            DataTable dt = ExecSQLToTable(sql);
            dt.TableName = "Q_master";
            var ds = new DataSet();
            ds.Tables.Add(dt);

            return ds;
        }

        protected override void CreateTempTable()
        {
            var sql = new StringBuilder();
            sql.Append(" CREATE TEMP TABLE t_report_15_2_5 (" +
                            " service CHARACTER(100), " +
                            " rsum_tarif " + DBManager.sDecimalType + "(14,2), " +
                            " sum_dolg " + DBManager.sDecimalType + "(14,2)) " + DBManager.sUnlogTempTable);
            ExecSQL(sql.ToString());
        }

        protected override void DropTempTable()
        {
            ExecSQL(" drop table t_report_15_2_5 ");
        }

    }
}