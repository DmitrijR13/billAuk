using System;
using System.Data;
using System.Collections.Generic;
using System.Globalization;
using Bars.KP50.Report.Base;
using STCLINE.KP50.DataBase;
using Bars.KP50.Report.Tula.Properties;

namespace Bars.KP50.Report.Tula.Reports
{
    class Report7123 : BaseSqlReportWithDates
    {
        public override string Name
        {
            get { return "71.2.3 Выписка из лицевого счета Тула"; }
        }

        public override string Description
        {
            get { return "Выписка из лицевого счета Тула"; }
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

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.LC }; }
        }

        protected override byte[] Template
        {
            get { return Resources.Report_71_2_3; }
        }

        /// <summary> ФИО </summary>
        private string FIO { get; set; }

        /// <summary> Лицевой счет </summary>
        private string LS { get; set; }

        /// <summary> Адрес </summary>
        private string Address { get; set; }

        /// <summary> Количество проживающих </summary>
        private string QuantityGil { get; set; }

        /// <summary> Общая площадь </summary>
        private string TotalArea { get; set; }

        /// <summary> Жилая площадь </summary>
        private string FloorSpace { get; set; }

        /// <summary> Должность паспортиски </summary>
        private string PasportistkaPost { get; set; }

        /// <summary> Наименование расчетного центра </summary>
        private string ERC { get; set; }


        public override List<UserParam> GetUserParams()
        {
            return new List<UserParam>();
        }

        protected override void PrepareParams()
        {
 
        }

        protected override void PrepareReport(FastReport.Report report)
        {
            report.SetParameterValue("erc",ERC);
            report.SetParameterValue("person",FIO);
            report.SetParameterValue("lic", LS);
            report.SetParameterValue("address",Address);
            report.SetParameterValue("count_person", QuantityGil + " чел.");
            report.SetParameterValue("ob_pl", TotalArea + " м²");
            report.SetParameterValue("gil_pl", FloorSpace + " м²");
            report.SetParameterValue("dolgnost_pasport",PasportistkaPost);
            report.SetParameterValue("name_pasport", STCLINE.KP50.Global.Utils.GetCorrectFIO(ReportParams.User.uname));
            report.SetParameterValue("date", Operday.ToShortDateString());
        }

        public override DataSet GetData()
        {
            MyDataReader reader;
            string fpref = ReportParams.Pref + DBManager.sDataAliasRest;
            string date = Operday.ToShortDateString();

            var sql = " SELECT k.pref, TRIM(fio) AS fio,num_ls, TRIM(town) AS town, TRIM(rajon) AS rajon, " +
                             " TRIM(ulica) AS ulica, TRIM(ndom) AS ndom, TRIM(nkor) AS nkor, TRIM(nkvar) AS nkvar " +
                      " FROM  " + fpref + "kvar k INNER JOIN " + fpref + "dom d ON d.nzp_dom = k.nzp_dom " +
                                                " INNER JOIN " + fpref + "s_ulica u ON u.nzp_ul = d.nzp_ul " +
                                                " INNER JOIN " + fpref + "s_rajon r ON r.nzp_raj = u.nzp_raj " +
                                                " INNER JOIN " + fpref + "s_town t ON t.nzp_town = r.nzp_town " +
                      " WHERE nzp_kvar = " + ReportParams.NzpObject;
            ExecRead(out reader, sql);

            if(reader.Read())
            {
                string pref = reader["pref"].ToString().Trim();
                string prefKernel = pref + DBManager.sKernelAliasRest,
                        prefData = pref + DBManager.sDataAliasRest;
                string prefCharge = pref + "_charge_" + Operday.Year.ToString(CultureInfo.InvariantCulture).Substring(2,2) + DBManager.tableDelimiter +
                                                "charge_" + Operday.Month.ToString("00");
                #region --заполнение параметров
                FIO = reader["fio"].ToString();
                LS = reader["num_ls"].ToString();
                Address = reader["town"] + ", " +
                          (reader["rajon"].ToString() == "-" ? "" : reader["rajon"] + ", ") +
                          "ул. " +reader["ulica"] + ", " +
                          "д. " +reader["ndom"] + ", " +
                          (reader["nkor"].ToString() == "-" ? "" : "кор. " + reader["nkor"] + ", ") +
                          (reader["nkvar"].ToString() == "-" ? "" : "кв. " + reader["nkvar"]);

                sql = " SELECT TRIM(val_prm) " +
                      " FROM " + prefData + " prm_1 " +
                      " WHERE is_actual = 1 " +
                        " AND nzp_prm = 5 " +
                        " AND dat_s <= '" + date + "' " +
                        " AND dat_po >= '" + date + "' " +
                        " AND nzp = " + ReportParams.NzpObject;
                QuantityGil = ExecScalar(sql) == null ? "" : ExecScalar(sql).ToString();

                sql = " SELECT TRIM(val_prm) " +
                      " FROM " + prefData + " prm_1 " +
                      " WHERE is_actual = 1 " +
                        " AND nzp_prm = 4 " +
                        " AND dat_s <= '" + date + "' " +
                        " AND dat_po >= '" + date + "' " +
                        " AND nzp = " + ReportParams.NzpObject;
                TotalArea = ExecScalar(sql) == null ? "" : ExecScalar(sql).ToString();

                sql = " SELECT TRIM(val_prm) " +
                      " FROM " + prefData + " prm_1 " +
                      " WHERE is_actual = 1 " +
                        " AND nzp_prm = 6 " +
                        " AND dat_s <= '" + date + "' " +
                        " AND dat_po >= '" + date + "' " +
                        " AND nzp = " + ReportParams.NzpObject;
                FloorSpace = ExecScalar(sql) == null ? "" : ExecScalar(sql).ToString();

                sql = " SELECT TRIM(val_prm) AS post " +
                      " FROM " + prefData + " prm_10 " +
                      " WHERE is_actual = 1 " +
                        " AND dat_s <= '" + date + "' " +
                        " AND dat_po >= '" + date + "' " +
                        " AND nzp_prm = 578 ";
                PasportistkaPost = ExecScalar(sql) == null ? "" : ExecScalar(sql).ToString();

                sql = " SELECT TRIM(val_prm) AS post " +
                      " FROM " + prefData + " prm_10 " +
                      " WHERE is_actual = 1 " +
                        " AND dat_s <= '" + date + "' " +
                        " AND dat_po >= '" + date + "' " +
                        " AND nzp_prm = 80 ";
                ERC = ExecScalar(sql) == null ? "" : ExecScalar(sql).ToString();
                #endregion

                if (TempTableInWebCashe(prefCharge))
                {
                    sql = " INSERT INTO t_report_71_2_3(name_param, summa) " +
                          " SELECT 'Сальдо на 1-е число месяца' AS name_param, SUM(sum_insaldo) AS summa " +
                          " FROM " + prefCharge +
                          " WHERE nzp_kvar = " + ReportParams.NzpObject + 
                            " AND nzp_serv > 1 " +
                          " GROUP BY 1 ";
                    ExecSQL(sql);

                    sql = " INSERT INTO t_report_71_2_3(name_param, summa) " +
                          " SELECT 'Начислено за месяц' AS name_param, SUM(sum_tarif) AS summa " +
                          " FROM " + prefCharge +
                          " WHERE nzp_kvar = " + ReportParams.NzpObject +
                            " AND nzp_serv > 1 " +
                          " GROUP BY 1 ";
                    ExecSQL(sql);

                    sql = " INSERT INTO t_report_71_2_3(name_param) " +
                          " VALUES ('В том числе:') ";
                    ExecSQL(sql);

                    sql = " INSERT INTO t_report_71_2_3(name_param, summa) " +
                          " SELECT service AS name_param, SUM(sum_tarif) AS summa " +
                          " FROM " + prefCharge + " c INNER JOIN " + prefKernel + "services s ON s.nzp_serv = c.nzp_serv" +
                          " WHERE nzp_kvar = " + ReportParams.NzpObject +
                            " AND c.nzp_serv > 1 " +
                          " GROUP BY 1 ";
                    ExecSQL(sql);

                    sql = " INSERT INTO t_report_71_2_3(name_param, summa) " +
                          " SELECT 'Изменения' AS name_param, SUM(sum_nedop) + SUM(reval) + SUM(real_charge) AS summa " +
                          " FROM " + prefCharge +
                          " WHERE nzp_kvar = " + ReportParams.NzpObject +
                            " AND nzp_serv > 1 " +
                          " GROUP BY 1 ";
                    ExecSQL(sql);

                    sql = " INSERT INTO t_report_71_2_3(name_param, summa) " +
                          " SELECT 'Всего к оплате с учетом изменений' AS name_param, SUM(sum_charge) AS summa " +
                          " FROM " + prefCharge +
                          " WHERE nzp_kvar = " + ReportParams.NzpObject +
                            " AND nzp_serv > 1 " +
                          " GROUP BY 1 ";
                    ExecSQL(sql);

                    sql = " INSERT INTO t_report_71_2_3(name_param, summa) " +
                          " SELECT 'Оплачено' AS name_param, SUM(sum_money) AS summa " +
                          " FROM " + prefCharge +
                          " WHERE nzp_kvar = " + ReportParams.NzpObject +
                            " AND nzp_serv > 1 " +
                          " GROUP BY 1 ";
                    ExecSQL(sql);
                }
            }
            reader.Close();

            sql = " SELECT TRIM(name_param) AS name_param, summa " +
                  " FROM t_report_71_2_3 ";

            DataTable dt = ExecSQLToTable(sql);
            dt.TableName = "Q_master";

            var ds = new DataSet();
            ds.Tables.Add(dt);
            return ds;
        }

        protected override void CreateTempTable()
        {
            const string sql = " CREATE TEMP TABLE t_report_71_2_3(" +
                         " name_param CHARACTER(100), " +
                         " summa " + DBManager.sDecimalType + "(14,2)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        protected override void DropTempTable()
        {
            ExecSQL(" DROP TABLE t_report_71_2_3 ");
        }
    }
}
