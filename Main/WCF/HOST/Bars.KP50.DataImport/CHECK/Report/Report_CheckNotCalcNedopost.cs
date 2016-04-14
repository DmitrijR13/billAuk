using System;
using System.Data;
using Globals.SOURCE.GLOBAL;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.DataImport.CHECK.Report
{
    class Report_CheckNotCalcNedopost: ReportCheckTemplate
    {
        private string table = "t_check_not_calc_nedopost_" + DateTime.Now.DayOfYear + DateTime.Now.Hour + DateTime.Now.Minute;
        private string tKvarNed;

        public Report_CheckNotCalcNedopost(IDbConnection connection, CheckBeforeClosingParams inputParams)
        {
            conn_db = connection;
            reportFrxSource = "check_not_calc_nedopost.frx";
            fileName = "c" + inputParams.Bank.pref + "_" + "CheckNotCalcNedopost.fpx";
            fullFileName = "Проверка по выставленным недопоставкам текущего месяца, но не рассчитанным в текущем месяце";
            User = inputParams.User;
            Bank = inputParams.Bank;
            Month = Points.GetCalcMonth(new CalcMonthParams { pref = inputParams.Bank.pref }).month_;
            Year = Points.GetCalcMonth(new CalcMonthParams { pref = inputParams.Bank.pref }).year_;

            tKvarNed = "t_tKvarNed" + inputParams.User.nzp_user;
        }

        protected override void CreateTempTable()
        {
            string sql;
            
            sql =
                " CREATE TEMP TABLE " + table +
                " (num_ls " + DBManager.sDecimalType + "(13,0)," +
                " pkod " + DBManager.sDecimalType + "(13,0), " +
                " adr CHAR(200), " +
                " serv CHAR(100), " +
                " contractor CHAR(100), " +
                " dat_s DATE," +
                " dat_po DATE," +
                " time_length  CHAR(100)," +
                " type_nedopost CHAR(100)," +
                " percent CHAR(100))" + 
                DBManager.sUnlogTempTable;
            DBManager.ExecSQL(conn_db, sql, true);

            sql =
                " CREATE TEMP TABLE " + tKvarNed +
                " (num_ls " + DBManager.sDecimalType + "(13,0)," +
                " nzp_kvar " + DBManager.sDecimalType + "(13,0), " +
                " pkod " + DBManager.sDecimalType + "(13,0), " +
                " adres CHAR(200))";
            DBManager.ExecSQL(conn_db, sql, true);
        }

        protected override void DropTempTable()
        {
            string sql = " DROP TABLE " + table;
            DBManager.ExecSQL(conn_db, sql, false);

            sql = " DROP TABLE " + tKvarNed;
            DBManager.ExecSQL(conn_db, sql, false);
        }

        protected override void FillTempTable()
        {
            string sql;
            int nextMonth = (Month < 12 ? Month + 1 : 1);
            int nextYear = (Month < 12 ? Year : Year + 1);
            string firstDay = "01." + Month.ToString("00") + "." + Year;
            string nextFirstDay = "01." + nextMonth.ToString("00") + "." + nextYear;

            sql =
              " INSERT INTO " + tKvarNed +
              " (num_ls, nzp_kvar, pkod, adres)" +
              " SELECT DISTINCT k.num_ls, k.nzp_kvar, k.pkod," +
              " " + DBManager.sNvlWord + "(trim(r.rajon),'')||' ул.'||" + DBManager.sNvlWord + "(trim(u.ulica),'')||" +
              "' д.'||" + DBManager.sNvlWord + "(trim(d.ndom),'')||'/'||" + DBManager.sNvlWord + "(trim(d.nkor),'')||" +
              "' кв.'||" + DBManager.sNvlWord + "(trim(k.nkvar),'') as adres " +
              " FROM " + Bank.pref + DBManager.sDataAliasRest + "link_group g," +
              Bank.pref + DBManager.sDataAliasRest + "dom d," +
              Points.Pref + DBManager.sDataAliasRest + "s_ulica u, " +
              Points.Pref + DBManager.sDataAliasRest + "s_rajon r, " +
              Bank.pref + DBManager.sDataAliasRest + "kvar k " +
              " WHERE k.nzp_kvar = g.nzp" +
              " AND g.nzp_group in (" + (int)ECheckGroupId.NotCalcNedop + ")" +
              " AND k.nzp_dom = d.nzp_dom AND d.nzp_ul = u.nzp_ul AND r.nzp_raj = u.nzp_raj";
            DBManager.ExecSQL(conn_db, sql, true);

            sql =
                " INSERT INTO " + table + " (num_ls, pkod,  adr,  serv, contractor," +
                " dat_s, dat_po, time_length, type_nedopost, percent)" +
                " SELECT k.num_ls, k.pkod, k.adres, s.service, p.payer," +
                " n.dat_s, n.dat_po, age(n.dat_po, n.dat_s)," +
                " kn.name, n.tn" +
                " FROM " + tKvarNed + " k," +
                Bank.pref + DBManager.sDataAliasRest + "nedop_kvar n, " +
                Bank.pref + DBManager.sKernelAliasRest + "services s, " +
                Bank.pref + DBManager.sDataAliasRest + "upg_s_kind_nedop kn, " +
                Points.Pref + DBManager.sKernelAliasRest + "supplier su," +
                Points.Pref + DBManager.sKernelAliasRest + "s_payer p" +
                " WHERE k.nzp_kvar = n.nzp_kvar AND n.nzp_serv = s.nzp_serv " +
                " AND kn.nzp_kind = n.nzp_kind AND kn.kod_kind = 1 AND kn.nzp_parent = n.nzp_serv" +
                " AND n.nzp_supp = su.nzp_supp AND su.nzp_payer_supp = p.nzp_payer" +
                " AND n.dat_s < '" + nextFirstDay + "' AND n.dat_po > '" + firstDay + "' AND n.is_actual <> 100";
            DBManager.ExecSQL(conn_db, sql, true);
        }

        protected override void SetParamValues(FastReport.Report rep)
        {
            string sql =
                " SELECT trim(ngroup) as ngroup " +
                " FROM " + Bank.pref + DBManager.sDataAliasRest + "s_group" +
                " WHERE nzp_group = " + (int)ECheckGroupId.NotCalcNedop + "";
            DataTable dt = DBManager.ExecSQLToTable(conn_db, sql);
            rep.SetParameterValue("report_name", dt.Rows.Count > 0 ? "Создана группа «" + dt.Rows[0]["ngroup"] + "»" : "");
        }

        protected override DataSet AddDataSource()
        {
            DataSet ds_rep = new DataSet();
            string sql =
                " SELECT num_ls, pkod,  adr,  serv, contractor, dat_s," +
                " dat_po, time_length, type_nedopost, percent" +
                " FROM " + table;

            DataTable dt = ClassDBUtils.OpenSQL(sql, conn_db).resultData;
            dt.TableName = "Q_master";
            ds_rep.Tables.Add(dt);
            return ds_rep;
        }

        protected override void SetNzpExc(int nzp_exc)
        {
            SetNzpExcInCheckChMon((int)ECheckGroupId.NotCalcNedop, nzp_exc);
        }
    }
}
