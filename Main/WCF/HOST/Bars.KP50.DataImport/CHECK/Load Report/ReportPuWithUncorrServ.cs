using System;
using System.Data;
using STCLINE.KP50.DataBase;

namespace Bars.KP50.DataImport.CHECK.Load_Report
{
    class ReportPuWithUncorrServ : ReportLoadTemplate
    {
        /// <summary>Сопоставленные услуги</summary>
        public ReportPuWithUncorrServ()
        {
            reportFrxSource = "zniPuUncorrServ.frx";
            fileName = "Приборы учета с некорректными услугами";
        }

        protected override void CreateTempTable()
        {
            string sql = " CREATE TEMP TABLE t_zni_pu_ucorr_serv( " +
                         " num_ls CHARACTER(20), " +
                         " nzp_kvar INTEGER, " +
                         " kod_pu CHARACTER(20), " +
                         " kod_serv CHARACTER(20), " +
                         " service_f CHARACTER(100), " +
                         " nzp_serv INTEGER, " +
                         " service_s CHARACTER(100)" +
                         ") " + DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        protected override void DropTempTable()
        {
            ExecSQL(" DROP TABLE t_zni_pu_ucorr_serv ");
        }

        protected override void FillTempTable()
        {
            string sql = " INSERT INTO t_zni_pu_ucorr_serv(num_ls, nzp_kvar, kod_pu, kod_serv)" +
                         " SELECT ls_id AS num_ls, " +
                                " nzp_kvar AS num_ls_new, " +
                                " local_id AS kod_pu, " +
                                " kod_serv" +
                         " FROM " + PrefUpload + "file_ipu " +
                         " WHERE nzp_file = " + NzpFile + 
                         " AND kod_serv::int IN " +
                         "  (SELECT id_serv " +
                         "   FROM " + PrefUpload+ "file_services " +
                         "   WHERE nzp_file = " + NzpFile + 
                         "   AND nzp_serv NOT IN" +
                         "      (SELECT nzp_serv " +
                         "       FROM " + PrefKernel + "s_counts))";
            ExecSQL(sql);

            //проставляем код некорректной услуги из файла
            //sql = " UPDATE t_zni_pu_ucorr_serv SET kod_serv = " +
            //      " (SELECT distinct(id_serv) " +
            //      " FROM " + PrefUpload + "file_services " +
            //      " WHERE nzp_serv NOT IN " +
            //      "     (SELECT nzp_serv " +
            //      "      FROM " + PrefKernel + "s_counts)" +
            //      " AND id_serv = t_zni_pu_ucorr_serv.kod_serv::int " +
            //      " AND nzp_file = " + NzpFile + 
            //      " )";
            //ExecSQL(sql);

            //проставляем услугу из файла
            sql = " UPDATE t_zni_pu_ucorr_serv SET service_f = " +
                  " (SELECT TRIM(service) " +
                  " FROM " + PrefUpload + "file_services " +
                  " WHERE id_serv = t_zni_pu_ucorr_serv.kod_serv::int" +
                  " AND nzp_file = " + NzpFile + "" +
                  " )";
            ExecSQL(sql);

            //проставляем код некорректной услуги из системы
            sql = " UPDATE t_zni_pu_ucorr_serv SET nzp_serv = " +
                  " (SELECT nzp_serv " +
                  " FROM " + PrefUpload + "file_services " +
                  " WHERE id_serv = t_zni_pu_ucorr_serv.kod_serv::int " +
                  " AND nzp_file = " + NzpFile + "" +
                  " )";
            ExecSQL(sql); 

            //проставляем услугу из системы
            sql = " UPDATE t_zni_pu_ucorr_serv SET service_s = " +
                  " (SELECT TRIM(service) " +
                  " FROM " + PrefKernel + "services " +
                  " WHERE nzp_serv = t_zni_pu_ucorr_serv.nzp_serv" +
                  " )";
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
            string sql = " SELECT DISTINCT num_ls AS num_ls_old, " +
                                " nzp_kvar AS num_ls_new," +
                                " kod_pu AS kod_pu, " +
                                " kod_serv AS nzp_serv_f, " +
                                " service_f AS serv_f, " +
                                " nzp_serv AS nzp_serv_s," +
                                " service_s AS serv_s" +
                         " FROM t_zni_pu_ucorr_serv " +
                         " ORDER BY 2,1 ";
            DataTable dt = ExecSQLToTable(sql);
            dt.TableName = "Q_master";

            var ds = new DataSet();
            ds.Tables.Add(dt);

            return ds;
        }
    }
}
