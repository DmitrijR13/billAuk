using System;
using System.Data;
using STCLINE.KP50.DataBase;

namespace Bars.KP50.DataImport.CHECK.Load_Report
{
    class ReportSopDom : ReportLoadTemplate
    {
        /// <summary>Сопоставленные дома</summary>
        public ReportSopDom(){
            reportFrxSource = "zniSopDom.frx";
            fileName = "Сопоставленные дома";
        }

        protected override void CreateTempTable() {
            string sql = " CREATE TEMP TABLE t_zni_sop_dom( " +
                         " nzp_dom_f CHARACTER(20), " +
                         " town_f CHARACTER(30), " +
                         " rajon_f CHARACTER(30), " +
                         " ulica_f CHARACTER(40), " +
                         " ndom_f CHARACTER(10), " +
                         " nkor_f CHARACTER(3), " +
                         " nzp_dom INTEGER, " +
                         " square_f " + DBManager.sDecimalType + "(14,2), " +
                         " square  " + DBManager.sDecimalType + "(14,2)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql = " CREATE TEMP TABLE t_zni_kvar (" +
                   " nzp_kvar INTEGER," +
                   " nzp_dom INTEGER) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql = " CREATE TEMP TABLE t_zni_sop_dom_adrs (" +
                   " nzp_dom INTEGER," +
                   " town CHARACTER(30), " +
                   " rajon CHARACTER(30), " +
                   " ulica CHARACTER(40), " +
                   " ndom CHARACTER(10), " +
                   " nkor CHARACTER(3)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql = " CREATE TEMP TABLE t_zni_sop_dom_es(" +
                  " nzp_dom_k INTEGER, " +
                  " nzp_dom_d INTEGER, " +
                  " address_f CHARACTER(150), " +
                  " address_d CHARACTER(150), " +
                  " address_k CHARACTER(150)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        protected override void DropTempTable() {
            ExecSQL(" DROP TABLE t_zni_sop_dom ");
            ExecSQL(" DROP TABLE t_zni_kvar ");
            ExecSQL(" DROP TABLE t_zni_sop_dom_adrs ");
            ExecSQL(" DROP TABLE t_zni_sop_dom_es ");
        }

        protected override void FillTempTable() {
            string sql = " INSERT INTO t_zni_sop_dom(nzp_dom_f, nzp_dom, town_f, rajon_f, ulica_f, ndom_f, nkor_f, square_f)" +
                         " SELECT local_id" + DBManager.sConvToInt + " AS nzp_dom_f, " +
                                " nzp_dom, " +
                                " TRIM(town) AS town_f, " +
                                " TRIM(rajon) AS rajon_f, " +
                                " TRIM(ulica) AS ulica_f, " +
                                " TRIM(ndom) AS ndom_f, " +
                                " TRIM(nkor) AS nkor_f, " +
                                " total_square AS square_f " +
                         " FROM " + PrefUpload + "file_dom " +
                         " WHERE nzp_file = " + NzpFile +
                           " AND nzp_dom IS NOT NULL ";
            ExecSQL(sql);

            sql = " INSERT INTO t_zni_sop_dom_adrs(nzp_dom, town, rajon, ulica, ndom, nkor) " +
                  " SELECT nzp_dom, " +
                         " TRIM(town), " +
                         " TRIM(rajon), " +
                         " TRIM(ulica), " +
                         " TRIM(ndom), " +
                         " TRIM(nkor) " +
                   " FROM " + PrefData + "dom d INNER JOIN " + PrefData + "s_ulica u ON u.nzp_ul = d.nzp_ul " +
                                              " INNER JOIN " + PrefData + "s_rajon r ON r.nzp_raj = u.nzp_raj " +
                                              " INNER JOIN " + PrefData + "s_town t ON t.nzp_town = r.nzp_town " +
                   " WHERE d.nzp_dom IN (SELECT nzp_dom FROM t_zni_sop_dom) ";
            ExecSQL(sql);

            sql = " INSERT INTO t_zni_kvar(nzp_kvar, nzp_dom) " +
                  " SELECT nzp_kvar, nzp_dom " +
                  " FROM " + PrefData + "kvar " +
                  " WHERE nzp_dom IN (SELECT nzp_dom FROM t_zni_sop_dom) ";
            ExecSQL(sql);

            sql = " UPDATE t_zni_sop_dom SET square = " +
                  " (SELECT SUM(val_prm " + DBManager.sConvToNum + ") " +
                   " FROM " + LoadPrefData + "prm_1 " +
                   " WHERE nzp IN (SELECT nzp_kvar FROM t_zni_kvar WHERE nzp_dom = t_zni_sop_dom.nzp_dom)" +
                     " AND is_actual <> 100 " +
                     " AND nzp_prm = 4 " +
                     " AND dat_s <= '" + CalcMonth.ToShortDateString() + "' " +
                     " AND dat_po >= '" + CalcMonth.ToShortDateString() + "') ";
            ExecSQL(sql);

            sql = " INSERT INTO t_zni_sop_dom_es (nzp_dom_k, nzp_dom_d, address_f) " +
                  " SELECT k.nzp_dom, d.nzp_dom, " +
                         " TRIM(town) || " +
                         " (CASE WHEN TRIM(rajon) = '-' OR rajon IS NULL THEN '' ELSE ', ' || TRIM(rajon) END) || " +
                         " (CASE WHEN TRIM(ulica) = '-' OR ulica IS NULL THEN '' ELSE ', ' || TRIM(ulica) END) || " +
                         " (CASE WHEN TRIM(ndom) = '-' OR ndom IS NULL THEN '' ELSE ', д. ' || TRIM(ndom) END) || " +
                         " (CASE WHEN TRIM(nkor) = '' OR TRIM(nkor) = '-' OR nkor IS NULL THEN '' ELSE ', корп. ' || TRIM(nkor) END) " +
                  " FROM " + PrefUpload + "file_kvar k INNER JOIN " + PrefUpload + "file_dom d ON TRIM(d.local_id) = TRIM(k.dom_id_char) " +
                  " WHERE k.nzp_dom IS NOT NULL " +
                    " AND d.nzp_dom IS NOT NULL " +
                    " AND k.nzp_dom <> d.nzp_dom " +
                    " AND k.nzp_file = d.nzp_file " +
                    " AND k.nzp_file = " + NzpFile;
            ExecSQL(sql);

            sql = " UPDATE t_zni_sop_dom_es SET address_k = " +
                  " (SELECT TRIM(town) || " +
                          " (CASE WHEN TRIM(rajon) = '-' OR rajon IS NULL THEN '' ELSE ', ' || TRIM(rajon) END) || " +
                          " (CASE WHEN TRIM(ulicareg) = '' OR TRIM(ulicareg) = '-' OR ulicareg IS NULL THEN ', ' ELSE ', ' || TRIM(ulicareg) || ' ' END) || " +
                          " (CASE WHEN TRIM(ulica) = '-' OR ulica IS NULL THEN '' ELSE TRIM(ulica) END) || " +
                          " (CASE WHEN TRIM(ndom) = '-' OR ndom IS NULL THEN '' ELSE ', д. ' || TRIM(ndom) END) || " +
                          " (CASE WHEN TRIM(nkor) = '' OR TRIM(nkor) = '-' OR nkor IS NULL THEN '' ELSE ', корп. ' || TRIM(nkor) END) " +
                   " FROM " + PrefData + "dom d INNER JOIN " + PrefData + "s_ulica u ON u.nzp_ul = d.nzp_ul " +
                                              " INNER JOIN " + PrefData + "s_rajon r ON r.nzp_raj = u.nzp_raj " +
                                              " INNER JOIN " + PrefData + "s_town t ON t.nzp_town = r.nzp_town  " +
                   " WHERE d.nzp_dom = t_zni_sop_dom_es.nzp_dom_k ) ";
            ExecSQL(sql);

            sql = " UPDATE t_zni_sop_dom_es SET address_d = " +
                  " (SELECT TRIM(town) || " +
                          " (CASE WHEN TRIM(rajon) = '-' OR rajon IS NULL THEN '' ELSE ', ' || TRIM(rajon) END) || " +
                          " (CASE WHEN TRIM(ulicareg) = '' OR TRIM(ulicareg) = '-' OR ulicareg IS NULL THEN ', ' ELSE ', ' || TRIM(ulicareg) || ' ' END) || " +
                          " (CASE WHEN TRIM(ulica) = '-' OR ulica IS NULL THEN '' ELSE TRIM(ulica) END) || " +
                          " (CASE WHEN TRIM(ndom) = '-' OR ndom IS NULL THEN '' ELSE ', д. ' || TRIM(ndom) END) || " +
                          " (CASE WHEN TRIM(nkor) = '' OR TRIM(nkor) = '-' OR nkor IS NULL THEN '' ELSE ', корп. ' || TRIM(nkor) END) " +
                   " FROM " + PrefData + "dom d INNER JOIN " + PrefData + "s_ulica u ON u.nzp_ul = d.nzp_ul " +
                                              " INNER JOIN " + PrefData + "s_rajon r ON r.nzp_raj = u.nzp_raj " +
                                              " INNER JOIN " + PrefData + "s_town t ON t.nzp_town = r.nzp_town  " +
                   " WHERE d.nzp_dom = t_zni_sop_dom_es.nzp_dom_d ) ";
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
            string sql = " SELECT TRIM(town_f) AS town_f, " +
                                " TRIM(rajon_f) AS rajon_f, " +
                                " TRIM(ulica_f) AS ulica_f, " +
                                " TRIM(ndom_f) AS ndom_f, " +
                                " TRIM(nkor_f) AS nkor_f, " +
                                " TRIM(town) AS town, " +
                                " TRIM(rajon) AS rajon, " +
                                " TRIM(ulica) AS ulica, " +
                                " TRIM(ndom) AS ndom, " +
                                " TRIM(nkor) AS nkor, " +
                                " square_f, " +
                                " square " +
                  " FROM t_zni_sop_dom d INNER JOIN t_zni_sop_dom_adrs a ON a.nzp_dom = d.nzp_dom " +
                  " ORDER BY 6,7,8,9,10,1,2,3,4,5 ";
            DataTable dt = ExecSQLToTable(sql);
            dt.TableName = "Q_master";

            sql = " SELECT TRIM(address_f) AS address_f, " +
                         " TRIM(address_d) AS address_d, " +
                         " TRIM(address_k) AS address_k " +
                  " FROM t_zni_sop_dom_es " +
                  " ORDER BY 1 ";
            DataTable dt1 = ExecSQLToTable(sql);
            dt1.TableName = "Q_master1";

            var ds = new DataSet();
            ds.Tables.Add(dt);
            ds.Tables.Add(dt1);

            return ds;
        }
    }
}
