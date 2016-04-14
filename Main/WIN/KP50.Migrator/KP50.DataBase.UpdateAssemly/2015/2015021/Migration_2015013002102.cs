using System;
using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015021
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2015013002102, MigrateDataBase.CentralBank)]
    public class Migration_2015013002102_CentralBank : Migration
    {
        public override void Apply()
        {
            if (Database.ProviderName != "PostgreSQL") return;

            string fin_15 = CurrentPrefix + "_fin_15";
            Object obj = Database.ExecuteScalar("select count(*) from information_schema.schemata where schema_name = '" + fin_15 + "'");

            int result = 0;
            try
            { result = Convert.ToInt32(obj); }
            catch
            { result = 0; }

            if (result == 0) return;

            CreateFnPaXXPart(fin_15);
            CreateFnDistribXXPart(fin_15);
            CreateFnNaudPart(fin_15);
            CreateFnPercDom(fin_15);
        }

        private void CreateFnPaXXPart(string fin_15)
        {
            DateTime dateFrom;

            string sOperDate = "";
            string sql = "";

            string parent_fn_pa_dom_xx = "";
            string parent_fn_pa_dom_xx_short = "";

            string fn_pa_dom_xx = "";
            string fn_pa_dom_xx_short = "";

            int cnt = 0;

            for (int monthCnt = 1; monthCnt < 13; monthCnt++)
            {
                parent_fn_pa_dom_xx = fin_15 + ".fn_pa_dom_" + monthCnt.ToString("00");
                parent_fn_pa_dom_xx_short = "fn_pa_dom_" + monthCnt.ToString("00");

                dateFrom = new DateTime(2015, monthCnt, 1);
                cnt = dateFrom.AddMonths(1).AddDays(-1).Day - dateFrom.Day + 1;

                for (int dayCnt = 1; dayCnt <= cnt; dayCnt++)
                {
                    sOperDate = "'" + dateFrom.Year + "-" + dateFrom.Month.ToString("00") + "-" + dayCnt.ToString("00") + "'";

                    fn_pa_dom_xx = parent_fn_pa_dom_xx + "_" + dayCnt.ToString("00");
                    fn_pa_dom_xx_short = parent_fn_pa_dom_xx_short + "_" + dayCnt.ToString("00");

                    if (!TableExists(fin_15, fn_pa_dom_xx_short))
                    {
                        sql = " CREATE TABLE " + fn_pa_dom_xx + " (CONSTRAINT CNSTR_" + fn_pa_dom_xx_short + "_dat_oper CHECK (dat_oper = " + sOperDate + ")) " +
                            " INHERITS (" + parent_fn_pa_dom_xx + ") WITHOUT OIDS";
                        Database.ExecuteNonQuery(sql);

                        sql = "ALTER TABLE " + fn_pa_dom_xx + " ADD CONSTRAINT PK_" + fn_pa_dom_xx_short + " PRIMARY KEY (nzp_pk)";
                        Database.ExecuteNonQuery(sql);

                        sql = "create UNIQUE index IX_" + fn_pa_dom_xx_short + "_nzp_pk on " + fn_pa_dom_xx + " (nzp_pk)";
                        Database.ExecuteNonQuery(sql);

                        sql = "create index IX_" + fn_pa_dom_xx_short + "_dat_oper on " + fn_pa_dom_xx + " (dat_oper)";
                        Database.ExecuteNonQuery(sql);

                        sql = "create index IX_" + fn_pa_dom_xx_short + "_nzp_supp on " + fn_pa_dom_xx + " (nzp_supp)";
                        Database.ExecuteNonQuery(sql);

                        sql = "create index IX_" + fn_pa_dom_xx_short + "_nzp_dom on " + fn_pa_dom_xx + " (nzp_dom)";
                        Database.ExecuteNonQuery(sql);

                        sql = "INSERT INTO " + fn_pa_dom_xx + " select * from " + parent_fn_pa_dom_xx + " where dat_oper = " + sOperDate;
                        Database.ExecuteNonQuery(sql);
                    }
                }

                sql = "TRUNCATE ONLY " + parent_fn_pa_dom_xx;
                Database.ExecuteNonQuery(sql);
            }
        }

        private void CreateFnDistribXXPart(string fin_15)
        {
            DateTime dateFrom;

            string parent_fn_distrib_dom_xx = "";
            string parent_fn_distrib_dom_xx_short = "";

            string fn_distrib_dom_xx = "";
            string fn_distrib_dom_xx_short = "";

            string sql = "";
            string sOperDate = "";

            int cnt = 0;

            for (int monthCnt = 1; monthCnt < 13; monthCnt++)
            {
                parent_fn_distrib_dom_xx = fin_15 + ".fn_distrib_dom_" + monthCnt.ToString("00");
                parent_fn_distrib_dom_xx_short = "fn_distrib_dom_" + monthCnt.ToString("00");

                dateFrom = new DateTime(2015, monthCnt, 1);
                dateFrom = new DateTime(2015, monthCnt, 1);
                cnt = dateFrom.AddMonths(1).AddDays(-1).Day - dateFrom.Day + 1;

                for (int dayCnt = 1; dayCnt <= cnt; dayCnt++)
                {
                    sOperDate = "'" + dateFrom.Year + "-" + dateFrom.Month.ToString("00") + "-" + dayCnt.ToString("00") + "'";

                    fn_distrib_dom_xx = parent_fn_distrib_dom_xx + "_" + dayCnt.ToString("00");
                    fn_distrib_dom_xx_short = parent_fn_distrib_dom_xx_short + "_" + dayCnt.ToString("00");

                    if (!TableExists(fin_15, fn_distrib_dom_xx_short))
                    {
                        sql = " CREATE TABLE " + fn_distrib_dom_xx + " (CONSTRAINT CNSTR_" + fn_distrib_dom_xx_short + "_dat_oper CHECK (dat_oper = " + sOperDate + ")) " +
                            " INHERITS (" + parent_fn_distrib_dom_xx + ") WITHOUT OIDS";

                        Database.ExecuteNonQuery(sql);

                        sql = "ALTER TABLE " + fn_distrib_dom_xx + " ADD CONSTRAINT PK_" + fn_distrib_dom_xx_short + " PRIMARY KEY (nzp_dis)";
                        Database.ExecuteNonQuery(sql);

                        sql = "create UNIQUE index IX_" + fn_distrib_dom_xx_short + "_nzp_dis on " + fn_distrib_dom_xx + " (nzp_dis)";
                        Database.ExecuteNonQuery(sql);

                        sql = "create index IX_" + fn_distrib_dom_xx_short + "_dat_oper on " + fn_distrib_dom_xx + " (dat_oper)";
                        Database.ExecuteNonQuery(sql);

                        sql = "create index IX_" + fn_distrib_dom_xx_short + "_nzp_payer on " + fn_distrib_dom_xx + " (nzp_payer)";
                        Database.ExecuteNonQuery(sql);

                        sql = "create index IX_" + fn_distrib_dom_xx_short + "_nzp_dom on " + fn_distrib_dom_xx + " (nzp_dom)";
                        Database.ExecuteNonQuery(sql);

                        sql = "INSERT INTO " + fn_distrib_dom_xx + " select * from " + parent_fn_distrib_dom_xx + " where dat_oper = " + sOperDate;
                        Database.ExecuteNonQuery(sql);
                    }
                }

                sql = "TRUNCATE ONLY " + parent_fn_distrib_dom_xx;
                Database.ExecuteNonQuery(sql);
            }
        }

        private void CreateFnNaudPart(string fin_15)
        {
            DateTime dateFrom;
            DateTime dateTo;

            string parent_fn_naud_dom = "";
            string parent_fn_naud_dom_short = "";

            string fn_naud_dom = "";
            string fn_naud_dom_short = "";

            string sql = "";
            string sDateFrom = "";
            string sDateTo = "";

            parent_fn_naud_dom = fin_15 + ".fn_naud_dom";
            parent_fn_naud_dom_short = "fn_naud_dom";

            for (int monthCnt = 1; monthCnt < 13; monthCnt++)
            {
                dateFrom = new DateTime(2015, monthCnt, 1);
                dateTo = dateFrom.AddMonths(1);

                sDateFrom = "'" + dateFrom.Year + "-" + dateFrom.Month.ToString("00") + "-" + dateFrom.Day.ToString("00") + "'";
                sDateTo = "'" + dateTo.Year + "-" + dateTo.Month.ToString("00") + "-" + dateTo.Day.ToString("00") + "'";

                fn_naud_dom = parent_fn_naud_dom + "_" + monthCnt.ToString("00");
                fn_naud_dom_short = parent_fn_naud_dom_short + "_" + monthCnt.ToString("00");

                if (!TableExists(fin_15, fn_naud_dom_short))
                {
                    sql = " CREATE TABLE " + fn_naud_dom + " (CONSTRAINT CNSTR_" + fn_naud_dom_short + "_dat_oper CHECK (dat_oper >= " + sDateFrom + " and dat_oper < " + sDateTo + ")) " +
                        " INHERITS (" + parent_fn_naud_dom + ") WITHOUT OIDS";
                    Database.ExecuteNonQuery(sql);

                    sql = "ALTER TABLE " + fn_naud_dom + " ADD CONSTRAINT PK_" + fn_naud_dom_short + " PRIMARY KEY (nzp_naud)";
                    Database.ExecuteNonQuery(sql);

                    sql = "create UNIQUE index IX_" + fn_naud_dom_short + "_nzp_naud on " + fn_naud_dom + " (nzp_naud)";
                    Database.ExecuteNonQuery(sql);

                    sql = "create index IX_" + fn_naud_dom_short + "_dat_oper on " + fn_naud_dom + " (dat_oper)";
                    Database.ExecuteNonQuery(sql);

                    sql = "create index IX_" + fn_naud_dom_short + "_nzp_dom on " + fn_naud_dom + " (nzp_dom)";
                    Database.ExecuteNonQuery(sql);

                    sql = "INSERT INTO " + fn_naud_dom + " select * from " + parent_fn_naud_dom + " where dat_oper >= " + sDateFrom + " and dat_oper < " + sDateTo;
                    Database.ExecuteNonQuery(sql);
                }
            }

            sDateFrom = "'2015-01-01'";

            sql = "DELETE FROM ONLY " + parent_fn_naud_dom + " WHERE dat_oper >= " + sDateFrom;
            Database.ExecuteNonQuery(sql);
        }

        private void CreateFnPercDom(string fin_15)
        {
            DateTime dateFrom;
            DateTime dateTo;

            string parent_fn_perc_dom = "";
            string parent_fn_perc_dom_short = "";

            string fn_perc_dom = "";
            string fn_perc_dom_short = "";

            string sql = "";
            string sDateFrom = "";
            string sDateTo = "";

            parent_fn_perc_dom = fin_15 + ".fn_perc_dom";
            parent_fn_perc_dom_short = "fn_perc_dom";

            for (int monthCnt = 1; monthCnt < 13; monthCnt++)
            {
                dateFrom = new DateTime(2015, monthCnt, 1);
                dateTo = dateFrom.AddMonths(1);

                sDateFrom = "'" + dateFrom.Year + "-" + dateFrom.Month.ToString("00") + "-" + dateFrom.Day.ToString("00") + "'";
                sDateTo = "'" + dateTo.Year + "-" + dateTo.Month.ToString("00") + "-" + dateTo.Day.ToString("00") + "'";

                fn_perc_dom = parent_fn_perc_dom + "_" + monthCnt.ToString("00");
                fn_perc_dom_short = parent_fn_perc_dom_short + "_" + monthCnt.ToString("00");

                if (!TableExists(fin_15, fn_perc_dom_short))
                {
                    sql = " CREATE TABLE " + fn_perc_dom + " (CONSTRAINT CNSTR_" + fn_perc_dom_short + "_dat_oper CHECK (dat_oper >= " + sDateFrom + " and dat_oper < " + sDateTo + ")) " +
                        " INHERITS (" + parent_fn_perc_dom + ") WITHOUT OIDS";
                    Database.ExecuteNonQuery(sql);

                    sql = "ALTER TABLE " + fn_perc_dom + " ADD CONSTRAINT PK_" + fn_perc_dom_short + " PRIMARY KEY (nzp_pr)";
                    Database.ExecuteNonQuery(sql);

                    sql = "create UNIQUE index IX_" + fn_perc_dom_short + "_nzp_pr on " + fn_perc_dom + " (nzp_pr)";
                    Database.ExecuteNonQuery(sql);

                    sql = "create index IX_" + fn_perc_dom_short + "_dat_oper on " + fn_perc_dom + " (dat_oper)";
                    Database.ExecuteNonQuery(sql);

                    sql = "create index IX_" + fn_perc_dom_short + "_nzp_dom on " + fn_perc_dom + " (nzp_dom)";
                    Database.ExecuteNonQuery(sql);

                    sql = "INSERT INTO " + fn_perc_dom + " select * from " + parent_fn_perc_dom + " where dat_oper >= " + sDateFrom + " and dat_oper < " + sDateTo;
                    Database.ExecuteNonQuery(sql);
                }
            }

            sDateFrom = "'2015-01-01'";

            sql = "DELETE FROM ONLY " + parent_fn_perc_dom + " WHERE dat_oper >= " + sDateFrom;
            Database.ExecuteNonQuery(sql);
        }

        private bool TableExists(string schm, string tbl)
        {
            string sql = "select 1 from information_schema.tables where table_schema = '" + schm + "'" +
                " and table_name = '" + tbl + "' limit 1";

            using (IDataReader reader = Database.ExecuteReader(sql))
            {
                if (reader.Read())
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
    }
}
