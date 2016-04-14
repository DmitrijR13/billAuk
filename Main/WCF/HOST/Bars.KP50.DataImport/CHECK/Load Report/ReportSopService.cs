using System;
using System.Data;
using STCLINE.KP50.DataBase;

namespace Bars.KP50.DataImport.CHECK.Load_Report
{
    class ReportSopService : ReportLoadTemplate
    {
        /// <summary>Сопоставленные услуги</summary>
        public ReportSopService()
        {
            reportFrxSource = "zniSopService.frx";
            fileName = "Сопоставленные услуги";
        }

        protected override void CreateTempTable()
        {
            string sql = " CREATE TEMP TABLE t_zni_sop_service( " +
                         " nzp_serv_f INTEGER, " +
                         " service_f CHARACTER(100), " +
                         " nzp_serv INTEGER, " +
                         " service CHARACTER(100)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        protected override void DropTempTable()
        {
            ExecSQL(" DROP TABLE t_zni_sop_service ");
        }

        protected override void FillTempTable()
        {
            string sql = " INSERT INTO t_zni_sop_service(nzp_serv_f, service_f, nzp_serv)" +
                         " SELECT id_serv AS nzp_serv_f, " +
                                " service AS service_f, " +
                                " nzp_serv " +
                         " FROM " + PrefUpload + "file_services " +
                         " WHERE nzp_file = " + NzpFile +
                           " AND nzp_serv IS NOT NULL ";
            ExecSQL(sql);

            sql = " UPDATE t_zni_sop_service SET service = " +
                  " (SELECT TRIM(service) " +
                   " FROM " + PrefKernel + "services " +
                   " WHERE nzp_serv = t_zni_sop_service.nzp_serv) ";
            ExecSQL(sql);
        }

        protected override void SetParamValues(FastReport.Report rep)
        {
            var months = new[] {"","Январь","Февраль",
                                "Март","Апрель","Май","Июнь","Июль","Август","Сентябрь",
                                "Октябрь","Ноябрь","Декабрь"};
            rep.SetParameterValue("period", " за " + months[Month]);
            rep.SetParameterValue("DATE", DateTime.Now.ToShortDateString());
            rep.SetParameterValue("TIME", DateTime.Now.ToShortTimeString());

        }

        protected override DataSet AddDataSource()
        {
            string sql = " SELECT DISTINCT TRIM(service_f) AS service_f, " +
                                " TRIM(service) AS service," +
                                " nzp_serv_f, " +
                                " nzp_serv    " +
                         " FROM t_zni_sop_service " +
                         " ORDER BY 2,1 ";
            DataTable dt = ExecSQLToTable(sql);
            dt.TableName = "Q_master";

            var ds = new DataSet();
            ds.Tables.Add(dt);

            return ds;
        }
    }
}
