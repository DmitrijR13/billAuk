using System;
using System.Data;
using STCLINE.KP50.DataBase;

namespace Bars.KP50.DataImport.CHECK.Load_Report
{
    class ReportSopUrlic : ReportLoadTemplate
    {
        /// <summary>Сопоставленные юр.лиц</summary>
        public ReportSopUrlic()
        {
            reportFrxSource = "zniSopUrlic.frx";
            fileName = "Сопоставленные юр.лица";
        }

        protected override void CreateTempTable()
        {
            string sql = " CREATE TEMP TABLE t_zni_sop_urlic( " +
                         " nzp_payer_f " + DBManager.sDecimalType + "(18,0), " +
                         " payer_f CHARACTER(100), " +
                         " inn_f CHARACTER(20), " +
                         " kpp_f CHARACTER(20), " +
                         " inn_s CHARACTER(12), " +
                         " kpp_s CHARACTER(9), " +
                         " nzp_payer INTEGER, " +
                         " payer CHARACTER(40)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        protected override void DropTempTable()
        {
            ExecSQL(" DROP TABLE t_zni_sop_urlic ");
        }

        protected override void FillTempTable()
        {
            string sql = " INSERT INTO t_zni_sop_urlic(nzp_payer_f, payer_f, inn_f, kpp_f, nzp_payer)" +
                         " SELECT urlic_id AS nzp_payer_f, " +
                                " urlic_name AS payer_f, " +
                                " inn, kpp, " +
                                " nzp_payer " +
                         " FROM " + PrefUpload + "file_urlic " +
                         " WHERE nzp_file = " + NzpFile +
                           " AND nzp_payer IS NOT NULL ";
            ExecSQL(sql);

            //Проставляем инн\кпп из системы
            sql =
                " UPDATE t_zni_sop_urlic " +
                " SET inn_s = " +
                "   (SELECT TRIM(inn)" +
                "   FROM " + PrefKernel + "s_payer " +
                "   WHERE nzp_payer = t_zni_sop_urlic.nzp_payer) ";
            ExecSQL(sql);

            sql =
                " UPDATE t_zni_sop_urlic " +
                " SET kpp_s = " +
                "   (SELECT TRIM(kpp)" +
                "   FROM " + PrefKernel + "s_payer " +
                "   WHERE nzp_payer = t_zni_sop_urlic.nzp_payer) ";
            ExecSQL(sql);

            //Проставляем юр.лицо из системы
            sql = " UPDATE t_zni_sop_urlic SET payer = " +
                  " (SELECT TRIM(payer) " +
                   " FROM " + PrefKernel + "s_payer " +
                   " WHERE nzp_payer = t_zni_sop_urlic.nzp_payer) ";
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
            string sql = " SELECT TRIM(payer_f) AS payer_f, " +
                                " TRIM(payer) AS payer, " +
                                " TRIM(inn_f) || '/' || TRIM(kpp_f) AS inn_kpp_f, " +
                                " inn_s || '/' || kpp_s AS inn_kpp_s," +
                                " nzp_payer_f," +
                                " nzp_payer " +
                         " FROM t_zni_sop_urlic " +
                         " ORDER BY 2,1 ";
            DataTable dt = ExecSQLToTable(sql);
            dt.TableName = "Q_master";

            var ds = new DataSet();
            ds.Tables.Add(dt);

            return ds;
        }
    }
}
