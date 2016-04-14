using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Bars.KP50.Utils;
using Globals.SOURCE.GLOBAL;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.DataImport.CHECK.Report
{
    class Report_CheckOpenedLSWithoutAccrual: ReportCheckTemplate
    {
        private string table = "t_check_big_payment_" + DateTime.Now.DayOfYear + DateTime.Now.Hour + DateTime.Now.Minute;
        private string tKvar;
        private string param = "";

        public Report_CheckOpenedLSWithoutAccrual(IDbConnection connection, CheckBeforeClosingParams inputParams)
        {
            conn_db = connection;
            reportFrxSource = "check_opened_ls_without_accrual.frx";
            fileName = "c" + inputParams.Bank.pref + "_" + "CheckOpenedLSWithoutAccrual.fpx";
            fullFileName = "Отчет открытые ЛС с услугами без начислений";
            User = inputParams.User;
            Bank = inputParams.Bank;
            Month = Points.GetCalcMonth(new CalcMonthParams { pref = inputParams.Bank.pref }).month_;
            Year = Points.GetCalcMonth(new CalcMonthParams { pref = inputParams.Bank.pref }).year_;

            tKvar = "t_tKvarOpenedLS" + inputParams.User.nzp_user;
        }

        protected override void CreateTempTable()
        {
            string sql;
            
            sql =
                " CREATE TEMP TABLE " + table +
                " (num_ls " + DBManager.sDecimalType + "(13,0)," +
                " pkod " + DBManager.sDecimalType + "(13,0), " +
                " adr CHAR(200), " +
                " serv CHAR(30)," +
                " rsum_tarif " + DBManager.sDecimalType + "(14,2), " +
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
            string chargeXX = Bank.pref + "_charge_" + Year.ToString().Substring(2, 2) +
                DBManager.tableDelimiter + "charge_" + Month.ToString("00");
            

            sql =
              " INSERT INTO " + tKvar +
              " (num_ls, nzp_kvar, pkod, adres)" +
              " SELECT DISTINCT k.num_ls, k.nzp_kvar, k.pkod," +
              " trim(r.rajon)||' ул.'||trim(u.ulica)||' д.'||trim(d.ndom)||'/'||trim(d.nkor)||' кв.'||trim(k.nkvar) as adres " +
              " FROM " + Bank.pref + DBManager.sDataAliasRest + "link_group g," +
              Bank.pref + DBManager.sDataAliasRest + "dom d," +
              Points.Pref + DBManager.sDataAliasRest + "s_ulica u, " +
              Points.Pref + DBManager.sDataAliasRest + "s_rajon r, " +
              Bank.pref + DBManager.sDataAliasRest + "kvar k " +
              " WHERE k.nzp_kvar = g.nzp" +
              " AND g.nzp_group in (" + (int)ECheckGroupId.OpenedLSWithoutAccrual + ")" +
              " AND k.nzp_dom = d.nzp_dom AND d.nzp_ul = u.nzp_ul AND r.nzp_raj = u.nzp_raj";
            DBManager.ExecSQL(conn_db, sql, true);

            sql =
                " INSERT INTO " + table + " (num_ls, pkod,  adr, serv, rsum_tarif, sum_outsaldo)" +
                " SELECT k.num_ls, k.pkod, k.adres, s.service_small, c1.rsum_tarif, sum_outsaldo" +
                " FROM " + tKvar + " k," +
                chargeXX + " c1," +
                Points.Pref + DBManager.sKernelAliasRest + "services s" +
                " WHERE c1.dat_charge is null AND c1.nzp_serv > 1 AND " +
                " c1.rsum_tarif = 0  " +
                " AND c1.nzp_kvar = k.nzp_kvar AND c1.nzp_serv = s.nzp_serv";
            DBManager.ExecSQL(conn_db, sql, true);
        }

        protected override void SetParamValues(FastReport.Report rep)
        {
            if (param != "")
            {
                rep.SetParameterValue("report_name", param);
                return;
            }
            string sql =
                " SELECT trim(ngroup) as ngroup " +
                " FROM " + Bank.pref + DBManager.sDataAliasRest + "s_group" +
                " WHERE nzp_group = " + (int)ECheckGroupId.OpenedLSWithoutAccrual + "";
            DataTable dt = DBManager.ExecSQLToTable(conn_db, sql);
            rep.SetParameterValue("report_name", dt.Rows.Count > 0 ? "Создана группа «" + dt.Rows[0]["ngroup"] + "»" : "");
        }

        protected override DataSet AddDataSource()
        {
            DataSet ds_rep = new DataSet();
            string sql =
                " SELECT num_ls, pkod,  adr,  serv, rsum_tarif, sum_outsaldo" +
                " FROM " + table + " ORDER BY adr, serv";

            DataTable dt = ClassDBUtils.OpenSQL(sql, conn_db).resultData;
            dt.TableName = "Q_master";
            ds_rep.Tables.Add(dt);
            return ds_rep;
        }

        protected override void SetNzpExc(int nzp_exc)
        {
            SetNzpExcInCheckChMon((int)ECheckGroupId.OpenedLSWithoutAccrual, nzp_exc);
        }
    }
}
