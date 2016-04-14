using System;
using System.Data;
using STCLINE.KP50.DataBase;

namespace Bars.KP50.DataImport.CHECK.Load_Report
{
    class ReportSopDogovor : ReportLoadTemplate
    {
        /// <summary>Сопоставленные договоры</summary>
        public ReportSopDogovor(){
            reportFrxSource = "zniSopDogovor.frx";
            fileName = "Сопоставленные договоры";
        }

        protected override void CreateTempTable() {
            string sql = " CREATE TEMP TABLE t_zni_sop_dogovor( " +
                         " nzp_dog_f INTEGER, " +
                         " nzp_agent_f INTEGER, " +
                         " nzp_princip_f INTEGER, " +
                         " nzp_supp_f INTEGER, " +
                         " dogovor_f CHARACTER(60), " +
                         " nzp_dog INTEGER, " +
                         " nzp_agent INTEGER, " +
                         " nzp_princip INTEGER, " +
                         " nzp_supp INTEGER, " +
                         " dogovor CHARACTER(100)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        protected override void DropTempTable() {
            ExecSQL(" DROP TABLE t_zni_sop_dogovor ");
        }

        protected override void FillTempTable() {
            string sql = " INSERT INTO t_zni_sop_dogovor(nzp_dog_f, nzp_agent_f, nzp_princip_f, nzp_supp_f, dogovor_f, nzp_dog)" +
                         " SELECT id_supp AS nzp_dog_f, " +
                                " id_agent AS nzp_agent_f," +
                                " id_urlic_p AS nzp_princip_f," +
                                " id_supp AS nzp_supp_f, " +
                                " dog_name AS dogovor_f, " +
                                " nzp_supp " +
                         " FROM " + PrefUpload + "file_dog " +
                         " WHERE nzp_file = " + NzpFile +
                           " AND nzp_supp IS NOT NULL ";
            ExecSQL(sql);

            sql = " UPDATE t_zni_sop_dogovor SET dogovor = " +
                  " (SELECT TRIM(name_supp) " +
                   " FROM " + PrefKernel + "supplier " +
                   " WHERE nzp_supp = t_zni_sop_dogovor.nzp_dog), " +
                                               " nzp_agent = " +
                  " (SELECT nzp_payer_agent " +
                   " FROM " + PrefKernel + "supplier " +
                   " WHERE nzp_supp = t_zni_sop_dogovor.nzp_dog), " +
                                               " nzp_princip = " +
                  " (SELECT nzp_payer_princip " +
                   " FROM " + PrefKernel + "supplier " +
                   " WHERE nzp_supp = t_zni_sop_dogovor.nzp_dog), " +
                                               " nzp_supp = " +
                  " (SELECT nzp_payer_supp " +
                   " FROM " + PrefKernel + "supplier " +
                   " WHERE nzp_supp = t_zni_sop_dogovor.nzp_dog) ";
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
            string sql = " SELECT '(' || nzp_dog_f || ')' || TRIM(dogovor_f) || '(' || nzp_agent_f || ')' || '(' || nzp_princip_f || ')' || '(' || nzp_supp_f || ')'  AS dogovor_f, " +
                                " '(' || nzp_dog || ')' || TRIM(dogovor) || '(' || nzp_agent || ')' || '(' || nzp_princip || ')' || '(' || nzp_supp || ')' AS dogovor " +
                         " FROM t_zni_sop_dogovor " +
                         " ORDER BY 2,1 ";
            DataTable dt = ExecSQLToTable(sql);
            dt.TableName = "Q_master";

            var ds = new DataSet();
            ds.Tables.Add(dt);

            return ds;
        }
    }
}
