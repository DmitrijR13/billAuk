using System;
using System.Data;
using STCLINE.KP50.DataBase;

namespace Bars.KP50.DataImport.CHECK.Load_Report
{
    class ReportSopLS : ReportLoadTemplate
    {
        /// <summary>Сопоставленные лицевые счета</summary>
        public ReportSopLS() {
            reportFrxSource = "zniSopLS.frx";
            fileName = "Сопоставленные лицевые счета";
        }

        protected override void CreateTempTable() {
            string sql = " CREATE TEMP TABLE t_zni_sop_ls( " +
                         " nzp_file INTEGER, " +
                         " num_ls_f CHARACTER(20), " +
                         " nzp_kvar INTEGER, " +
                         " nzp_area_f " + DBManager.sDecimalType + "(18,0), " +
                         " nzp_dom_f CHARACTER(20), " +
                         " fam CHARACTER(40), " +
                         " ima CHARACTER(40), " +
                         " otch CHARACTER(40), " +
                         " num_ls INTEGER, " +
                         " nzp_area INTEGER, " +
                         " area_f CHARACTER(100), " +
                         " area CHARACTER(40), " +
                         " town CHARACTER(30), " +
                         " rajon CHARACTER(30), " +
                         " ulica CHARACTER(40), " +
                         " ndom CHARACTER(10), " +
                         " nkor CHARACTER(3), " +
                         " nkvar CHARACTER(10), " +
                         " nkvar_n CHARACTER(3)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql = " CREATE TEMP TABLE t_zni_sop_ls_adrs(" +
                    " nzp_kvar INTEGER, " +
                    " town_s CHARACTER(30), " +
                    " rajon_s CHARACTER(30), " +
                    " ulica_s CHARACTER(40), " +
                    " ndom_s CHARACTER(10), " +
                    " nkor_s CHARACTER(3), " +
                    " nkvar_s CHARACTER(10), " +
                    " nkvar_n_s CHARACTER(3)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        protected override void DropTempTable() {
            ExecSQL(" DROP TABLE t_zni_sop_ls ");
            ExecSQL(" DROP TABLE t_zni_sop_ls_adrs ");
        }

        protected override void FillTempTable() {
            string sql = " INSERT INTO t_zni_sop_ls(nzp_file, num_ls_f, nzp_kvar, nzp_area_f, nzp_dom_f, fam, ima, otch, nkvar, nkvar_n)" +
                         " SELECT nzp_file, " +
                                " id AS num_ls_f, " +
                                " nzp_kvar, " +
                                " id_urlic_pass_dom " + DBManager.sConvToNum + " AS nzp_area_f, " +
                                " dom_id_char AS nzp_dom_f, " +
                                " fam, " +
                                " ima, " +
                                " otch, " +
                                " nkvar, " +
                                " nkvar_n " +
                         " FROM " + PrefUpload + "file_kvar " +
                         " WHERE nzp_file = " + NzpFile;
            ExecSQL(sql);

            sql = " INSERT INTO t_zni_sop_ls_adrs(nzp_kvar, town_s, rajon_s, ulica_s, ndom_s, nkor_s, nkvar_s, nkvar_n_s) " +
                  " SELECT nzp_kvar, " +
                         " TRIM(town), " +
                         " TRIM(rajon), " +
                         " TRIM(ulica), " +
                         " TRIM(ndom), " +
                         " TRIM(nkor), " +
                         " TRIM(nkvar), " +
                         " TRIM(nkvar_n) " +
                   " FROM " + PrefData + "kvar k INNER JOIN " + PrefData + "dom d ON d.nzp_dom = k.nzp_dom " +
                                               " INNER JOIN " + PrefData + "s_ulica u ON u.nzp_ul = d.nzp_ul " +
                                               " INNER JOIN " + PrefData + "s_rajon r ON r.nzp_raj = u.nzp_raj " +
                                               " INNER JOIN " + PrefData + "s_town t ON t.nzp_town = r.nzp_town " +
                   " WHERE k.nzp_kvar IN (SELECT nzp_kvar FROM t_zni_sop_ls) ";
            ExecSQL(sql);

            sql = " UPDATE t_zni_sop_ls SET nkvar = NULL " +
                  " WHERE nkvar = '' OR TRIM(nkvar) = '-' ";
            ExecSQL(sql);

            sql = " UPDATE t_zni_sop_ls SET nkvar_n = NULL " +
                  " WHERE nkvar_n = '' OR TRIM(nkvar_n) = '-' ";
            ExecSQL(sql);

            sql = " UPDATE t_zni_sop_ls SET fam = NULL " +
                  " WHERE fam = '' OR TRIM(fam) = '-' ";
            ExecSQL(sql);

            sql = " UPDATE t_zni_sop_ls SET ima = NULL " +
                 " WHERE ima = '' OR TRIM(ima) = '-' ";
            ExecSQL(sql);

            sql = " UPDATE t_zni_sop_ls SET otch = NULL " +
                 " WHERE otch = '' OR TRIM(otch) = '-' ";
            ExecSQL(sql);

            sql = " UPDATE t_zni_sop_ls SET " +
                  " num_ls = (SELECT num_ls " +
                            " FROM " + LoadPrefData + "kvar k " +
                            " WHERE k.nzp_kvar = t_zni_sop_ls.nzp_kvar), " +
                  " nzp_area = (SELECT nzp_area " +
                              " FROM " + LoadPrefData + "kvar k " +
                              " WHERE k.nzp_kvar = t_zni_sop_ls.nzp_kvar) ";
            ExecSQL(sql);

            sql = " UPDATE t_zni_sop_ls SET area_f = " +
                  " (SELECT TRIM(urlic_name)" +
                   " FROM " + PrefUpload + "file_urlic u " +
                   " WHERE u.nzp_file = t_zni_sop_ls.nzp_file " +
                     " AND u.urlic_id = t_zni_sop_ls.nzp_area_f) ";
            ExecSQL(sql);

            sql = " UPDATE t_zni_sop_ls SET area = " +
                  " (SELECT TRIM(area) " +
                   " FROM " + LoadPrefData + "s_area a " +
                   " WHERE a.nzp_area = t_zni_sop_ls.nzp_area) ";
            ExecSQL(sql);

            sql = " UPDATE t_zni_sop_ls SET " +
                  " town = (SELECT town " +
                          " FROM " + PrefUpload + "file_dom d " +
                          " WHERE d.nzp_file = t_zni_sop_ls.nzp_file " +
                            " AND d.local_id = t_zni_sop_ls.nzp_dom_f), " +
                  " rajon = (SELECT rajon " +
                           " FROM " + PrefUpload + "file_dom d " +
                           " WHERE d.nzp_file = t_zni_sop_ls.nzp_file " +
                             " AND d.local_id = t_zni_sop_ls.nzp_dom_f), " +
                  " ulica = (SELECT ulica " +
                           " FROM " + PrefUpload + "file_dom d " +
                           " WHERE d.nzp_file = t_zni_sop_ls.nzp_file " +
                             " AND d.local_id = t_zni_sop_ls.nzp_dom_f), " +
                  " ndom = (SELECT ndom " +
                          " FROM " + PrefUpload + "file_dom d " +
                          " WHERE d.nzp_file = t_zni_sop_ls.nzp_file " +
                            " AND d.local_id = t_zni_sop_ls.nzp_dom_f), " +
                  " nkor = (SELECT nkor " +
                          " FROM " + PrefUpload + "file_dom d " +
                          " WHERE d.nzp_file = t_zni_sop_ls.nzp_file " +
                            " AND d.local_id = t_zni_sop_ls.nzp_dom_f) ";
            ExecSQL(sql);

            sql = " UPDATE t_zni_sop_ls SET town = NULL " +
                  " WHERE town = '' OR TRIM(town) = '-' ";
            ExecSQL(sql);

            sql = " UPDATE t_zni_sop_ls SET rajon = NULL " +
                  " WHERE rajon = '' OR TRIM(rajon) = '-' ";
            ExecSQL(sql);

            sql = " UPDATE t_zni_sop_ls SET ulica = NULL " +
                  " WHERE ulica = '' OR TRIM(ulica) = '-' ";
            ExecSQL(sql);

            sql = " UPDATE t_zni_sop_ls SET ndom = NULL " +
                  " WHERE ndom = '' OR TRIM(ndom) = '-' ";
            ExecSQL(sql);

            sql = " UPDATE t_zni_sop_ls SET nkor = NULL " +
                  " WHERE nkor = '' OR TRIM(nkor) = '-' ";
            ExecSQL(sql);
        }

        protected override void SetParamValues(FastReport.Report rep) {
            var months = new[] {"","Январь","Февраль",
                                "Март","Апрель","Май","Июнь","Июль","Август","Сентябрь",
                                "Октябрь","Ноябрь","Декабрь"};
            rep.SetParameterValue("period", " за " + months[Month]);
            rep.SetParameterValue("DATE", DateTime.Now.ToShortDateString());
            rep.SetParameterValue("TIME", DateTime.Now.ToShortTimeString());

        }

        protected override DataSet AddDataSource() {
            string sql = " SELECT TRIM(num_ls_f) AS num_ls_f, " +
                                " num_ls , " +
                                " nzp_area_f, " +
                                " TRIM(area_f) AS area_f, " +
                                " nzp_area, " +
                                " TRIM(area) AS area, " +
                                " (CASE WHEN town IS NULL THEN '' ELSE TRIM(town) END) || " +
                                " (CASE WHEN rajon IS NULL THEN '' ELSE ', ' || TRIM(rajon) END) || " +
                                " (CASE WHEN ulica IS NULL THEN '' ELSE ', ' || TRIM(ulica) END) || " +
                                " (CASE WHEN ndom IS NULL THEN '' ELSE ', д. ' || TRIM(ndom) END) || " +
                                " (CASE WHEN nkor IS NULL THEN '' ELSE ', кор. ' || TRIM(nkor) END) || " +
                                " (CASE WHEN nkvar IS NULL THEN '' ELSE ', кв. ' || TRIM(nkvar) END) || " +
                                " (CASE WHEN nkvar_n IS NULL THEN '' ELSE ', ком. ' || TRIM(nkvar_n) END) AS address_f, " +
                                " (CASE WHEN fam IS NULL THEN '' ELSE TRIM(fam) END) || ' ' || " +
                                " (CASE WHEN ima IS NULL THEN '' ELSE TRIM(ima) END) || ' ' || " +
                                " (CASE WHEN otch IS NULL THEN '' ELSE TRIM(otch) END) AS fio, " +
                                " TRIM(town_s) AS town, " +
                                " TRIM(rajon_s) AS rajon, " +
                                " TRIM(ulica_s) AS ulica, " +
                                " TRIM(ndom_s) AS ndom, " +
                                " TRIM(nkor_s) AS nkor, " +
                                " TRIM(nkvar_s) AS nkvar, " +
                                " TRIM(nkvar_n_s) AS nkvar_n " +
                         " FROM t_zni_sop_ls t LEFT OUTER JOIN t_zni_sop_ls_adrs a ON a.nzp_kvar = t.nzp_kvar " +
                         " ORDER BY 2,1 ";
            DataTable dt = ExecSQLToTable(sql);
            dt.TableName = "Q_master";

            var ds = new DataSet();
            ds.Tables.Add(dt);

            return ds;
        }
    }
}
