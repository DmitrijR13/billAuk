using System;
using System.Data;
using Globals.SOURCE.GLOBAL;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.DataImport.CHECK.Report
{
    class Report_CheckInOutSaldo: ReportCheckTemplate
    {
        private string table = "t_check_in_out_saldo_" + DateTime.Now.Ticks;
        private string tKvar;
        private string tCharge;
        private string tPrevCharge;

        public Report_CheckInOutSaldo(IDbConnection connection, CheckBeforeClosingParams inputParams)
        {
            conn_db = connection;
            reportFrxSource = "check_in_out_saldo.frx";
            fileName = "c" + inputParams.Bank.pref + "_" + "CheckInOutSaldo.fpx";
            fullFileName = "Отчет входящее сальдо текущего месяца равно исходящему сальдо предыдущего";
            User = inputParams.User;
            Bank = inputParams.Bank;
            Month = Points.GetCalcMonth(new CalcMonthParams { pref = inputParams.Bank.pref }).month_;
            Year = Points.GetCalcMonth(new CalcMonthParams { pref = inputParams.Bank.pref }).year_;

            tKvar = "t_tKvarInOut" + DateTime.Now.Ticks;
            tCharge = "t_in_out_s_ch" + DateTime.Now.Ticks;
            tPrevCharge = "t_in_out_s_prevch" + DateTime.Now.Ticks;
        }                                                      

        protected override void CreateTempTable()
        {
            string sql;
            
            sql =
                " CREATE TEMP TABLE " + table +
                " (num_ls " + DBManager.sDecimalType + "(13,0)," +
                " pkod " + DBManager.sDecimalType + "(13,0), " +
                " adr CHAR(200), " +
                " sum_insaldo " + DBManager.sDecimalType + "(14,2), " +
                " sum_outsaldo " + DBManager.sDecimalType + "(14,2))" + 
                DBManager.sUnlogTempTable;
            DBManager.ExecSQL(conn_db, sql, true);

            sql =
                " CREATE TEMP TABLE " + tKvar +
                " (num_ls " + DBManager.sDecimalType + "(13,0)," +
                " nzp_kvar " + DBManager.sDecimalType + "(13,0), " +
                " pkod " + DBManager.sDecimalType + "(13,0), " +
                " adres CHAR(200))";
            DBManager.ExecSQL(conn_db, sql, true);

            sql =
                " CREATE TEMP TABLE " + tCharge +
                " (nzp_kvar " + DBManager.sDecimalType + "(13,0), " +
                " sum_insaldo " + DBManager.sDecimalType + "(14,2))";
            DBManager.ExecSQL(conn_db, sql, true);

            sql =
                " CREATE TEMP TABLE " + tPrevCharge +
                " (nzp_kvar " + DBManager.sDecimalType + "(13,0), " +
                " sum_outsaldo " + DBManager.sDecimalType + "(14,2))";
            DBManager.ExecSQL(conn_db, sql, true);
        }

        protected override void DropTempTable()
        {
            string sql = " DROP TABLE " + table;
            DBManager.ExecSQL(conn_db, sql, false);

            sql = " DROP TABLE " + tKvar;
            DBManager.ExecSQL(conn_db, sql, false);

            sql = " DROP TABLE " + tCharge;
            DBManager.ExecSQL(conn_db, sql, false);

            sql = " DROP TABLE " + tPrevCharge;
            DBManager.ExecSQL(conn_db, sql, false);
        }

        protected override void FillTempTable()
        {
            string sql;
            int prevMonth = (Month == 1 ? 12 : Month - 1);
            int prevYear = (Month == 1 ? Year - 1 : Year);
            string chargeXX = Bank.pref + "_charge_" + Year.ToString().Substring(2, 2) +
                DBManager.tableDelimiter + "charge_" + Month.ToString("00");
            string prevChargeXX = Bank.pref + "_charge_" + prevYear.ToString().Substring(2, 2) +
                DBManager.tableDelimiter + "charge_" + prevMonth.ToString("00");

            sql =
              " INSERT INTO " + tKvar +
              " (num_ls, nzp_kvar, pkod, adres)" +
              " SELECT DISTINCT k.num_ls, g.nzp, k.pkod," +
              " " + DBManager.sNvlWord + "(trim(r.rajon),'')||' ул.'||" + DBManager.sNvlWord + "(trim(u.ulica),'')||" +
              "' д.'||" + DBManager.sNvlWord + "(trim(d.ndom),'')||'/'||" + DBManager.sNvlWord + "(trim(d.nkor),'')||" +
              "' кв.'||" + DBManager.sNvlWord + "(trim(k.nkvar),'') as adres " +
              " FROM " + Bank.pref + DBManager.sDataAliasRest + "link_group g" +
              " LEFT OUTER JOIN " + Bank.pref + DBManager.sDataAliasRest + "kvar k ON  k.nzp_kvar = g.nzp" +
              " LEFT OUTER JOIN " + Bank.pref + DBManager.sDataAliasRest + "dom d ON k.nzp_dom = d.nzp_dom" +
              " LEFT OUTER JOIN " + Points.Pref + DBManager.sDataAliasRest + "s_ulica u ON d.nzp_ul = u.nzp_ul " +
              " LEFT OUTER JOIN " + Points.Pref + DBManager.sDataAliasRest + "s_rajon r ON r.nzp_raj = u.nzp_raj " +
              " WHERE g.nzp_group in (" + (int)ECheckGroupId.InOutSaldo + ") ";
            DBManager.ExecSQL(conn_db, sql, true);

            sql = " CREATE INDEX " + tKvar + "_1" + " on " + tKvar + " (nzp_kvar)";
            DBManager.ExecSQL(conn_db, sql, true);
            sql = DBManager.sUpdStat + " " + tKvar;
            DBManager.ExecSQL(conn_db, sql, true);


            sql =
                " INSERT INTO " + tCharge +
                " (nzp_kvar, sum_insaldo)" +
                " SELECT nzp_kvar, sum(sum_insaldo)" +
                " FROM " + chargeXX + "" +
                " WHERE dat_charge is null AND nzp_serv > 1" +
                " AND EXISTS " +
                " (SELECT 1 FROM " + Bank.pref + DBManager.sDataAliasRest + "link_group g" +
                " WHERE  g.nzp_group in (" + (int)ECheckGroupId.InOutSaldo + ")" +
                " AND g.nzp = " + chargeXX + ".nzp_kvar )" +
                " GROUP BY 1";
            DBManager.ExecSQL(conn_db, sql, true);

            sql = " CREATE INDEX " + tCharge + "_1" + " on " + tCharge + " (nzp_kvar)";
            DBManager.ExecSQL(conn_db, sql, true);
            sql = DBManager.sUpdStat + " " + tCharge;
            DBManager.ExecSQL(conn_db, sql, true);

            sql =
                " INSERT INTO " + tPrevCharge +
                " (nzp_kvar, sum_outsaldo)" +
                " SELECT nzp_kvar, sum(sum_outsaldo)" +
                " FROM " + prevChargeXX + "" +
                " WHERE dat_charge is null AND nzp_serv > 1" +
                " AND EXISTS " +
                " (SELECT 1 FROM " + Bank.pref + DBManager.sDataAliasRest + "link_group g" +
                " WHERE  g.nzp_group in (" + (int)ECheckGroupId.InOutSaldo + ")" +
                " AND g.nzp = " + prevChargeXX + ".nzp_kvar )" +
                " GROUP BY 1";
            DBManager.ExecSQL(conn_db, sql, true);

            sql = " CREATE INDEX " + tPrevCharge + "_1" + " on " + tPrevCharge + " (nzp_kvar)";
            DBManager.ExecSQL(conn_db, sql, true);
            sql = DBManager.sUpdStat + " " + tPrevCharge;
            DBManager.ExecSQL(conn_db, sql, true);

            sql =
                " INSERT INTO " + table + " (num_ls, pkod,  adr,  sum_insaldo, sum_outsaldo)" +
                " SELECT k.num_ls, k.pkod, k.adres, c1.sum_insaldo, c2.sum_outsaldo" +
                " FROM " + tCharge + " c1," +
                 tKvar + " k, " +
                tPrevCharge + " c2" +
                " WHERE c2.nzp_kvar = c1.nzp_kvar AND c1.nzp_kvar = k.nzp_kvar";
            DBManager.ExecSQL(conn_db, sql, true);
        }

        protected override void SetParamValues(FastReport.Report rep)
        {
            string sql =
                " SELECT trim(ngroup) as ngroup " +
                " FROM " + Bank.pref + DBManager.sDataAliasRest + "s_group" +
                " WHERE nzp_group = " + (int)ECheckGroupId.InOutSaldo + "";
            DataTable dt = DBManager.ExecSQLToTable(conn_db, sql);
            rep.SetParameterValue("report_name", dt.Rows.Count > 0 ? "Создана группа «" + dt.Rows[0]["ngroup"] + "»" : "");
        }

        protected override DataSet AddDataSource()
        {
            DataSet ds_rep = new DataSet();
            string sql =
                " SELECT num_ls, pkod,  adr,  sum_insaldo, sum_outsaldo" +
                " FROM " + table;

            DataTable dt = ClassDBUtils.OpenSQL(sql, conn_db).resultData;
            dt.TableName = "Q_master";
            ds_rep.Tables.Add(dt);
            return ds_rep;
        }

        protected override void SetNzpExc(int nzp_exc)
        {
            SetNzpExcInCheckChMon((int)ECheckGroupId.InOutSaldo, nzp_exc);
        }
    }
}
