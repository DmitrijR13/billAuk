using System;
using System.Data;
using STCLINE.KP50.DataBase;

namespace Bars.KP50.DataImport.CHECK.Load_Report
{
    class ReportSopTypeParams : ReportLoadTemplate
    {
        /// <summary>Сопоставленные типы параметра</summary>
        public ReportSopTypeParams() {
            reportFrxSource = "zniSopTypeParams.frx";
            fileName = "Сопоставленные типы параметра";
        }

        protected override void CreateTempTable() {
            string sql = " CREATE TEMP TABLE t_zni_sop_typeparams( " +
                         " nzp_prm_f INTEGER, " +
                         " name_prm_f CHARACTER(100), " +
                         " nzp_prm INTEGER, " +
                         " name_prm CHARACTER(100)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        protected override void DropTempTable() {
            ExecSQL(" DROP TABLE t_zni_sop_typeparams ");
        }

        protected override void FillTempTable() {
            string sql = " INSERT INTO t_zni_sop_typeparams(nzp_prm_f, name_prm_f, nzp_prm)" +
                         " SELECT id_prm AS nzp_serv_f, " +
                                " prm_name AS name_prm_f, " +
                                " nzp_prm " +
                         " FROM " + PrefUpload + "file_typeparams " +
                         " WHERE nzp_file = " + NzpFile +
                           " AND nzp_prm IS NOT NULL ";
            ExecSQL(sql);

            sql = " UPDATE t_zni_sop_typeparams SET name_prm = " +
                  " (SELECT TRIM(name_prm) " +
                   " FROM " + PrefKernel + "prm_name " +
                   " WHERE nzp_prm = t_zni_sop_typeparams.nzp_prm) ";
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
            string sql = " SELECT TRIM(name_prm_f) AS name_prm_f, " +
                                " TRIM(name_prm) AS name_prm " +
                         " FROM t_zni_sop_typeparams " +
                         " ORDER BY 2,1 ";
            DataTable dt = ExecSQLToTable(sql);
            dt.TableName = "Q_master";

            var ds = new DataSet();
            ds.Tables.Add(dt);

            return ds;
        }
    }
}
