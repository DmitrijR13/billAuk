using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Bars.KP50.Utils;
using Globals.SOURCE.GLOBAL;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.DataImport.CHECK.Report
{
    public class Report_CheckBigPayment: ReportCheckTemplate
    {
        private string table = "t_check_big_payment_" + DateTime.Now.DayOfYear + DateTime.Now.Hour + DateTime.Now.Minute;
        private string tKvar;
        private string param = "";

        public Report_CheckBigPayment(IDbConnection connection, CheckBeforeClosingParams inputParams)
        {
            conn_db = connection;
            reportFrxSource = "check_big_payment.frx";
            fileName = "c" + inputParams.Bank.pref + "_" + "CheckBigPayment.fpx";
            fullFileName = "Отчет большие начисления";
            User = inputParams.User;
            Bank = inputParams.Bank;
            Month = Points.GetCalcMonth(new CalcMonthParams { pref = inputParams.Bank.pref }).month_;
            Year = Points.GetCalcMonth(new CalcMonthParams { pref = inputParams.Bank.pref }).year_;

            tKvar = "t_tKvarBigPayment" + inputParams.User.nzp_user;
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
                " reval " + DBManager.sDecimalType + "(14,2))" + 
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
            int nextMonth = (Month < 12 ? Month + 1 : 1);
            int nextYear = (Month < 12 ? Year : Year + 1);
            string firstDay = "01." + Month.ToString("00") + "." + Year;
            string nextFirstDay = "01." + nextMonth.ToString("00") + "." + nextYear;
            string chargeXX = Bank.pref + "_charge_" + Year.ToString().Substring(2, 2) +
                DBManager.tableDelimiter + "charge_" + Month.ToString("00");
            decimal maxVal;

            sql =
                    " SELECT val_prm, nzp_key FROM  " + Bank.pref + DBManager.sDataAliasRest + "prm_5" +
                    " WHERE is_actual<>100" +
                    " AND dat_s <='" + nextFirstDay + "' and dat_po>='" + firstDay + "'" +
                    " AND nzp_prm = 1046" +
                    " ORDER BY nzp_key DESC";
            DataTable dt = DBManager.ExecSQLToTable(conn_db, sql);
            if (dt.Rows.Count < 1)
            {
                param = "Не заполнен параметр \"Предельная сумма начисления/перерасчета для анализа и проверок\".";
                return;
            }
            else maxVal = dt.Rows[0]["val_prm"].ToDecimal();

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
              " AND g.nzp_group in (" + (int)ECheckGroupId.BigPayment + ")" +
              " AND k.nzp_dom = d.nzp_dom AND d.nzp_ul = u.nzp_ul AND r.nzp_raj = u.nzp_raj";
            DBManager.ExecSQL(conn_db, sql, true);

            sql =
                " INSERT INTO " + table + " (num_ls, pkod,  adr, serv, rsum_tarif, reval)" +
                " SELECT k.num_ls, k.pkod, k.adres, s.service_small, sum(c1.rsum_tarif), sum(c1.reval)" +
                " FROM " + chargeXX + " c1" +
                " LEFT OUTER JOIN " + tKvar + " k ON c1.nzp_kvar = k.nzp_kvar," +
                Points.Pref + DBManager.sKernelAliasRest + "services s" +
                " WHERE c1.dat_charge is null AND c1.nzp_serv > 1 AND " +
                " (c1.rsum_tarif > " + maxVal + " OR c1.reval > " + maxVal + ") " +
                " AND c1.nzp_serv = s.nzp_serv" +
                " GROUP BY k.num_ls, k.pkod, k.adres, s.service_small";
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
                " WHERE nzp_group = " + (int)ECheckGroupId.BigPayment + "";
            DataTable dt = DBManager.ExecSQLToTable(conn_db, sql);
            rep.SetParameterValue("report_name", dt.Rows.Count > 0 ? "Создана группа «" + dt.Rows[0]["ngroup"] + "»" : "");
        }

        protected override DataSet AddDataSource()
        {
            DataSet ds_rep = new DataSet();
            string sql =
                " SELECT num_ls, pkod,  adr,  serv, rsum_tarif, reval" +
                " FROM " + table;

            DataTable dt = ClassDBUtils.OpenSQL(sql, conn_db).resultData;
            dt.TableName = "Q_master";
            ds_rep.Tables.Add(dt);
            return ds_rep;
        }

        protected override void SetNzpExc(int nzp_exc)
        {
            SetNzpExcInCheckChMon((int)ECheckGroupId.BigPayment, nzp_exc);
        }
    }
}
