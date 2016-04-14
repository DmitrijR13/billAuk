using System;
using System.Data;
using STCLINE.KP50.DataBase;

namespace Bars.KP50.DataImport.CHECK.Load_Report
{
    class ReportSaldo : ReportLoadTemplate
    {
        /// <summary>Сальдовая ведомость в разрезе услуг</summary>
        public ReportSaldo() 
        {
            reportFrxSource = "zniSaldo.frx";
            fileName = "Сальдовая ведомость";
        }

        protected override void CreateTempTable() {
            string sql = " CREATE TEMP TABLE t_akt_report_saldo( " +
                         " nzp_serv_f INTEGER, " +
                         " service_f CHARACTER(100), " +
                         " nzp_serv INTEGER, " +
                         " service CHARACTER(100), " +
                         " sum_insaldo " + DBManager.sDecimalType + "(14,2), " +
                         " sum_nach " + DBManager.sDecimalType + "(14,2), " +
                         " sum_money " + DBManager.sDecimalType + "(14,2), " +
                         " sum_reval " + DBManager.sDecimalType + "(14,2), " +
                         " sum_perekidka " + DBManager.sDecimalType + "(18,5), " +
                         " sum_outsaldo " + DBManager.sDecimalType + "(14,2)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        protected override void DropTempTable() {
            ExecSQL(" DROP TABLE t_akt_report_saldo ");
        }

        protected override void FillTempTable() {
            string sql = " INSERT INTO t_akt_report_saldo(nzp_serv_f, nzp_serv, sum_insaldo, sum_nach, sum_money, sum_reval, sum_perekidka, sum_outsaldo)" +
                         " SELECT id_serv_epd, " +
                                " nzp_serv, " +
                                " SUM(sum_insaldo), " +
                                " SUM(sum_nach), " +
                                " SUM(sum_money), " +
                                " SUM(sum_reval), " +
                                " SUM(sum_perekidka), " +
                                " SUM(sum_outsaldo) " +
                         " FROM " + PrefUpload + "file_serv " +
                         " WHERE nzp_file = " + NzpFile +
                         " GROUP BY 1,2 ";
            ExecSQL(sql);

            sql = " UPDATE t_akt_report_saldo SET service_f = " +
                  " (SELECT MAX(service) " +
                   " FROM " + PrefUpload + "file_services s " +
                   " WHERE s.id_serv = t_akt_report_saldo.nzp_serv_f) ";
            ExecSQL(sql);

            sql = " UPDATE t_akt_report_saldo SET service = " +
                  " (SELECT service " +
                   " FROM " + PrefKernel + "services s " +
                   " WHERE s.nzp_serv = t_akt_report_saldo.nzp_serv) ";
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
            string sql = " SELECT TRIM(service_f) || '(' || nzp_serv_f || ')' AS service_f, " +
                                " TRIM(service) || '(' || nzp_serv || ')' AS service, " +
                                " sum_insaldo, " +
                                " sum_nach, " +
                                " sum_money, " +
                                " sum_reval, " +
                                " sum_perekidka, " +
                                " sum_outsaldo " +
                  " FROM t_akt_report_saldo " +
                  " ORDER BY 2,1 ";
            DataTable dt = ExecSQLToTable(sql);
            dt.TableName = "Q_master";

            var ds = new DataSet();
            ds.Tables.Add(dt);

            return ds;
        }
    }
}
