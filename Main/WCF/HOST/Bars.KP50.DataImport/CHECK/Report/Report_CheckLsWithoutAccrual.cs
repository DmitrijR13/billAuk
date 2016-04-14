using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Globals.SOURCE.GLOBAL;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.DataImport.CHECK.Report
{
    public class Report_CheckLsWithoutAccrual: ReportCheckTemplate
    {
        private string tKvar;

        public Report_CheckLsWithoutAccrual(IDbConnection connection, CheckBeforeClosingParams inputParams)
        {
            conn_db = connection;
            reportFrxSource = "check_ls_without_accrual.frx";
            fileName = "c" + inputParams.Bank.pref + "_" + "CheckLsWithoutAccrual.fpx";
            fullFileName = "Отчет по проверке наличия открытых ЛС без начислений";
            User = inputParams.User;
            Bank = inputParams.Bank;
            Month = Points.GetCalcMonth(new CalcMonthParams { pref = inputParams.Bank.pref }).month_;
            Year = Points.GetCalcMonth(new CalcMonthParams { pref = inputParams.Bank.pref }).year_;

            tKvar = "t_tKvarLsWithoutAccrual" + inputParams.User.nzp_user;
        }

        protected override void CreateTempTable()
        {
            string sql =
                " CREATE TEMP TABLE " + tKvar +
                " (num_ls " + DBManager.sDecimalType + "(13,0)," +
                " nzp_kvar " + DBManager.sDecimalType + "(13,0), " +
                " pkod " + DBManager.sDecimalType + "(13,0), " +
                " adres CHAR(200))";
            DBManager.ExecSQL(conn_db, sql, true);
        }

        protected override void DropTempTable()
        {
            string sql = " DROP TABLE " + tKvar;
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
              " AND g.nzp_group in (" + (int)ECheckGroupId.LsWithoutAccrual + ")" +
              " AND k.nzp_dom = d.nzp_dom AND d.nzp_ul = u.nzp_ul AND r.nzp_raj = u.nzp_raj";
            DBManager.ExecSQL(conn_db, sql, true);
        }

        protected override void SetParamValues(FastReport.Report rep)
        {
            string sql =
                " SELECT trim(ngroup) as ngroup " +
                " FROM " + Bank.pref + DBManager.sDataAliasRest + "s_group" +
                " WHERE nzp_group = " + (int)ECheckGroupId.LsWithoutAccrual + "";
            DataTable dt = DBManager.ExecSQLToTable(conn_db, sql);
            rep.SetParameterValue("report_name", dt.Rows.Count > 0 ? "Создана группа «" + dt.Rows[0]["ngroup"] + "»" : "");
        }

        protected override DataSet AddDataSource()
        {
            DataSet ds_rep = new DataSet();
            string sql =
                " SELECT num_ls, pkod,  adres as adr" +
                " FROM " + tKvar;

            DataTable dt = ClassDBUtils.OpenSQL(sql, conn_db).resultData;
            dt.TableName = "Q_master";
            ds_rep.Tables.Add(dt);
            return ds_rep;
        }

        protected override void SetNzpExc(int nzp_exc)
        {
            SetNzpExcInCheckChMon((int)ECheckGroupId.LsWithoutAccrual, nzp_exc);
        }
    }
}
