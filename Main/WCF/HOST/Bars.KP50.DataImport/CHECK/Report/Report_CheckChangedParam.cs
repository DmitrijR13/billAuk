using System;
using System.Collections.Generic;
using System.Data;
using Globals.SOURCE.GLOBAL;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.DataImport.CHECK.Report
{
    class Report_CheckChangedParam: ReportCheckTemplate
    {
        
        private string table = "t_check_changed_param_" + DateTime.Now.DayOfYear + DateTime.Now.Hour + DateTime.Now.Minute;
        private string t_table;
        protected string tKvar;

        public Report_CheckChangedParam(IDbConnection connection, CheckBeforeClosingParams inputParams, string temp_t)
        {
            conn_db = connection;
            reportFrxSource = "check_changed_param.frx";
            fileName = "c" + inputParams.Bank.pref + "_" + "CheckChangedParam.fpx";
            fullFileName = "Отчет ЛС с измен.пар-ров или сальдо и не рассчитанных";
            User = inputParams.User;
            Bank = inputParams.Bank;
            Month = Points.GetCalcMonth(new CalcMonthParams { pref = inputParams.Bank.pref }).month_;
            Year = Points.GetCalcMonth(new CalcMonthParams { pref = inputParams.Bank.pref }).year_;
            t_table = temp_t;

            tKvar = "t_tKvarChangedParam" + inputParams.User.nzp_user;
        }

        protected override void CreateTempTable()
        {
            string sql =
                " CREATE TEMP TABLE " + table +
                " (num_ls " + DBManager.sDecimalType + "(13,0)," +
                " pkod " + DBManager.sDecimalType + "(13,0), " +
                " adr CHAR(200), " +
                " param CHAR(1000))" + 
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
              " AND g.nzp_group in (" + (int)ECheckGroupId.ChangedParam + ")" +
              " AND k.nzp_dom = d.nzp_dom AND d.nzp_ul = u.nzp_ul AND r.nzp_raj = u.nzp_raj";
            DBManager.ExecSQL(conn_db, sql, true);

            sql =
                " INSERT INTO " + table + " (num_ls, pkod,  adr)" +
                " SELECT DISTINCT k.num_ls, k.pkod, k.adres" +
                " FROM " + t_table + " t" +
                " LEFT OUTER JOIN " + tKvar + " k ON k.nzp_kvar = t.nzp_kvar";
            DBManager.ExecSQL(conn_db, sql, true);

            DataTable dt = DBManager.ExecSQLToTable(conn_db, " SELECT num_ls FROM " + table);
            foreach (DataRow r in dt.Rows)
            {
                sql =
                    " SELECT trim(name_prm) as name_prm" +
                    " FROM " + Points.Pref + DBManager.sKernelAliasRest + "prm_name" +
                    " WHERE EXISTS " +
                    " (SELECT 1 FROM " + t_table + " t WHERE t.nzp_kvar = " + r["num_ls"] + 
                    " AND t.nzp_prm = " + Points.Pref + DBManager.sKernelAliasRest + "prm_name.nzp_prm)" +
                    " UNION" +
                    " SELECT 'изменение сальдо' as name_prm " +
                    " WHERE EXISTS " +
                    " (SELECT 1 FROM " + t_table + " t WHERE t.nzp_kvar = " + r["num_ls"] + " AND t.nzp_prm = -1) ";
                DataTable dtParam = DBManager.ExecSQLToTable(conn_db, sql);

                List<string> pstr = new List<string>();
                foreach (DataRow rParam in dtParam.Rows)
                {
                    pstr.Add(rParam["name_prm"].ToString());
                }
                if (pstr.Count > 0)
                {
                    sql =
                        " UPDATE " + table + " SET param = '" + string.Join(", ", pstr) + "'" +
                        " WHERE num_ls = " + r["num_ls"];
                    DBManager.ExecSQL(conn_db, sql, true);
                }
            }

        }

        protected override void SetParamValues(FastReport.Report rep)
        {
            string sql =
                " SELECT trim(ngroup) as ngroup " +
                " FROM " + Bank.pref + DBManager.sDataAliasRest + "s_group" +
                " WHERE nzp_group = " + (int)ECheckGroupId.ChangedParam + "";
            DataTable dt = DBManager.ExecSQLToTable(conn_db, sql);
            rep.SetParameterValue("report_name", dt.Rows.Count > 0 ? "Создана группа «" + dt.Rows[0]["ngroup"] + "»" : "");
        }

        protected override DataSet AddDataSource()
        {
            DataSet ds_rep = new DataSet();
            string sql =
                " SELECT num_ls, pkod, adr, param" +
                " FROM " + table;

            DataTable dt = ClassDBUtils.OpenSQL(sql, conn_db).resultData;
            dt.TableName = "Q_master";
            ds_rep.Tables.Add(dt);
            return ds_rep;
        }

        protected override void SetNzpExc(int nzp_exc)
        {
            SetNzpExcInCheckChMon((int)ECheckGroupId.ChangedParam, nzp_exc);
        }
    }
}
