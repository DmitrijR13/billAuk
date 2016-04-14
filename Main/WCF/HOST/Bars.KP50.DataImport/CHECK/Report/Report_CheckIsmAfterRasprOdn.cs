using System;
using System.Data;
using Globals.SOURCE.GLOBAL;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.DataImport.CHECK.Report
{
    public class Report_CheckIsmAfterRasprOdn : ReportCheckTemplate
    {
        private string table = "t_check_b_close_" + DateTime.Now.DayOfYear + DateTime.Now.Hour + DateTime.Now.Minute;
        protected string tKvar;
        protected string t_table;
        
        public Report_CheckIsmAfterRasprOdn(IDbConnection connection, CheckBeforeClosingParams inputParams, string t_table_)
        {
            conn_db = connection;
            reportFrxSource = "checks_ism_pu_after_raspr_odn.frx";
            fileName = "c" + inputParams.Bank.pref + "_" + "CheckIsmAfterRasprOdn.fpx";
            fullFileName = "Отчет по проверке  изменений расхода ПУ  после уже распределенного  ОДН";
            User = inputParams.User;
            Bank = inputParams.Bank;
            Month = Points.GetCalcMonth(new CalcMonthParams { pref = inputParams.Bank.pref }).month_;
            Year = Points.GetCalcMonth(new CalcMonthParams { pref = inputParams.Bank.pref }).year_;

            t_table = t_table_;

            tKvar = "t_tKvarAfterRasprOdn" + inputParams.User.nzp_user;
        }

        protected override void CreateTempTable()
        {
            string sql =
                " CREATE TEMP TABLE " + table +
                " (num_ls " + DBManager.sDecimalType + "(13,0)," +
                " pkod " + DBManager.sDecimalType + "(13,0), " +
                " adr CHAR(200), " +
                " serv CHAR(100))" + DBManager.sUnlogTempTable; ;
            DBManager.ExecSQL(conn_db, sql, true);

            sql =
                " CREATE TEMP TABLE " + tKvar +
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

            sql = " DROP TABLE " + tKvar;
            DBManager.ExecSQL(conn_db, sql, false);
        }

        protected override void FillTempTable()
        {

            string sql;
            
            sql =
              " INSERT INTO " + tKvar +
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
              " AND g.nzp_group in (" + (int)ECheckGroupId.IsmAfterRasprOdn + ")" +
              " AND k.nzp_dom = d.nzp_dom AND d.nzp_ul = u.nzp_ul AND r.nzp_raj = u.nzp_raj";
            DBManager.ExecSQL(conn_db, sql, true);

            sql = " INSERT INTO " + table + " (num_ls, pkod, adr, serv)" +
                  " SELECT k.num_ls, k.pkod, k.adres, s.service" +
                  " FROM " + t_table + " t," +
                  Points.Pref + DBManager.sKernelAliasRest + "services s, " +
                  tKvar + " k" +
                  " WHERE t.num_ls = k.num_ls AND s.nzp_serv = t.nzp_serv";
            DBManager.ExecSQL(conn_db, sql, true);
            
        }

        protected override void SetParamValues(FastReport.Report rep)
        {
            string sql =
                " SELECT trim(ngroup) as ngroup " +
                " FROM " + Bank.pref + DBManager.sDataAliasRest + "s_group" +
                " WHERE nzp_group = " + (int)ECheckGroupId.IsmAfterRasprOdn + "";
            DataTable dt = DBManager.ExecSQLToTable(conn_db, sql);
            rep.SetParameterValue("report_name", dt.Rows.Count > 0 ? "Создана группа «" + dt.Rows[0]["ngroup"] + "»" : "");
        }

        protected override DataSet AddDataSource()
        {
            DataSet ds_rep = new DataSet();
            string sql =
                " SELECT num_ls, pkod, adr, serv" +
                " FROM " + table;

            DataTable dt = ClassDBUtils.OpenSQL(sql, conn_db).resultData;
            dt.TableName = "Q_master";
            ds_rep.Tables.Add(dt);
            return ds_rep;
        }

        protected override void SetNzpExc(int nzp_exc)
        {
            SetNzpExcInCheckChMon((int)ECheckGroupId.IsmAfterRasprOdn, nzp_exc);
        }
    }
}
