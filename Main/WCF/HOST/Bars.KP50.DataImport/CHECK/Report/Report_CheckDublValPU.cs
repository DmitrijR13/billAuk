using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Globals.SOURCE.GLOBAL;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.DataImport.CHECK.Report
{
    class Report_CheckDublValPU: ReportCheckTemplate
    {
        private string table = "t_check_in_out_saldo_" + DateTime.Now.DayOfYear + DateTime.Now.Hour + DateTime.Now.Minute;
        private string t_table_from_check;
        private string tKvar;

        public Report_CheckDublValPU(IDbConnection connection, CheckBeforeClosingParams inputParams, string temp_table)
        {
            conn_db = connection;
            reportFrxSource = "check_dubl_val_pu.frx";
            fileName = "c" + inputParams.Bank.pref + "_" + "CheckDublValPU.fpx";
            fullFileName = "Отчет по проверке наличия дублированных показаний ИПУ";
            User = inputParams.User;
            Bank = inputParams.Bank;
            Month = Points.GetCalcMonth(new CalcMonthParams { pref = inputParams.Bank.pref }).month_;
            Year = Points.GetCalcMonth(new CalcMonthParams { pref = inputParams.Bank.pref }).year_;

            tKvar = "t_tKvar_dubl_val_pu" + inputParams.User.nzp_user;
            t_table_from_check = temp_table;
        }

        protected override void CreateTempTable()
        {
            string sql;

            sql =
               " CREATE TEMP TABLE " + table +
               " (show_order INTEGER," +
               " pu_type CHAR(20), " +
               " group_name CHAR(200), " +
               " num_ls " + DBManager.sDecimalType + "(13,0)," +
               " pkod " + DBManager.sDecimalType + "(13,0), " +
               " adr CHAR(200)," +
               " dat_uchet CHAR(10), " +
               " count_num CHAR(20))" +
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
            Returns ret;

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
              " AND g.nzp_group in (" + (int)ECheckGroupId.DoubleValIPU + "," + (int)ECheckGroupId.DoubleValODPU +
              "," + (int)ECheckGroupId.DoubleValGrPU + ")" +
              " AND k.nzp_dom = d.nzp_dom AND d.nzp_ul = u.nzp_ul AND r.nzp_raj = u.nzp_raj";
            ret = DBManager.ExecSQL(conn_db, sql, true);

            sql =
                " INSERT INTO " + table + " (pu_type, show_order, num_ls, pkod,  adr, count_num, dat_uchet)" +
                " SELECT t.pu_type, t.show_order, k.num_ls, k.pkod, k.adres, t.count_num," +
                " CASE WHEN length(t.dat_uchet" + DBManager.sConvToVarChar + ") > 8" +
                " THEN substr(t.dat_uchet" + DBManager.sConvToVarChar + ",1,10) ELSE t.dat_uchet" + DBManager.sConvToVarChar + " END  " +
                " FROM " + t_table_from_check + " t " +
                " LEFT OUTER JOIN " + tKvar + " k ON k.nzp_kvar = t.nzp_kvar";
            ret = DBManager.ExecSQL(conn_db, sql, true);

            #region название группы
            //ИПУ------------------------------------------------------
            sql =
                " UPDATE " + table + " SET group_name = " +
                " (SELECT 'По данному отчету создана группа «'||trim(ngroup)||'»' " +
                "  FROM " + Bank.pref + DBManager.sDataAliasRest + "s_group" +
                "  WHERE nzp_group = " + (int)ECheckGroupId.DoubleValIPU + " )" +
                " WHERE show_order = 1 ";
            ret = DBManager.ExecSQL(conn_db, sql, true);
            //Групповые ПУ--------------------------------------------
            sql =
                " UPDATE " + table + " SET group_name = " +
                " (SELECT 'По данному отчету создана группа «'||trim(ngroup)||'»' " +
                "  FROM " + Bank.pref + DBManager.sDataAliasRest + "s_group" +
                "  WHERE nzp_group = " + (int)ECheckGroupId.DoubleValGrPU + " )" +
                " WHERE show_order = 2 ";
            ret = DBManager.ExecSQL(conn_db, sql, true);
            //ОДПУ--------------------------------------------
            sql =
                " UPDATE " + table + " SET group_name = " +
                " (SELECT 'По данному отчету создана группа «'||trim(ngroup)||'»' " +
                "  FROM " + Bank.pref + DBManager.sDataAliasRest + "s_group" +
                "  WHERE nzp_group = " + (int)ECheckGroupId.DoubleValODPU + " )" +
                " WHERE show_order = 3 ";
            ret = DBManager.ExecSQL(conn_db, sql, true);
            #endregion
        }

        protected override void SetParamValues(FastReport.Report rep)
        {
        }

        protected override DataSet AddDataSource()
        {
            DataSet ds_rep = new DataSet();
            string sql =
                " SELECT pu_type, show_order, group_name, num_ls, pkod, dat_uchet, adr, count_num" +
                " FROM " + table + " ORDER BY num_ls";

            DataTable dt = ClassDBUtils.OpenSQL(sql, conn_db).resultData;
            dt.TableName = "Q_master";
            ds_rep.Tables.Add(dt);
            return ds_rep;
        }

        protected override void SetNzpExc(int nzp_exc)
        {
            SetNzpExcInCheckChMon((int)ECheckGroupId.DoubleValIPU, nzp_exc);
            SetNzpExcInCheckChMon((int)ECheckGroupId.DoubleValGrPU, nzp_exc);
            SetNzpExcInCheckChMon((int)ECheckGroupId.DoubleValODPU, nzp_exc);
        }
    }
}
