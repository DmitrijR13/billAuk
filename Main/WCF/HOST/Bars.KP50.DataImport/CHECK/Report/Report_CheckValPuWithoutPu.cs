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
    public class Report_CheckValPuWithoutPu : ReportCheckTemplate
    {
        private string table = "t_check_val_without_pu" + DateTime.Now.Ticks;
        private string tKvar;
        //если нет параметров
        private string no_param = "";

        public Report_CheckValPuWithoutPu(IDbConnection connection, CheckBeforeClosingParams inputParams)
        {
            conn_db = connection;
            reportFrxSource = "check_val_pu_without_pu.frx";
            fileName = "c" + inputParams.Bank.pref + "_" + "CheckValPuWithoutPu.fpx";
            fullFileName = "Отчет по показаниям ПУ, для которых не существует ПУ ";
            User = inputParams.User;
            Bank = inputParams.Bank;
            Month = Points.GetCalcMonth(new CalcMonthParams {pref = inputParams.Bank.pref}).month_;
            Year = Points.GetCalcMonth(new CalcMonthParams {pref = inputParams.Bank.pref}).year_;

            tKvar = "t_kvar_check_pu_val_" + User.nzp_user;
        }

        protected override void CreateTempTable()
        {
            string sql =
                " CREATE TEMP TABLE " + table +
                " (show_order INTEGER," +
                " pu_type CHAR(20), " +
                " group_name CHAR(200), " +
                " nzp_counter " + DBManager.sDecimalType + "(13,0)," +
                " adr CHAR(200))" +
                DBManager.sUnlogTempTable;
            DBManager.ExecSQL(conn_db, sql, true);

            sql =
                " CREATE TEMP TABLE " + tKvar +
                " (num_ls " + DBManager.sDecimalType + "(13,0)," +
                " nzp_kvar " + DBManager.sDecimalType + "(13,0), " +
                " nzp_dom " + DBManager.sDecimalType + "(13,0), " +
                " pkod " + DBManager.sDecimalType + "(13,0), " +
                " adres CHAR(200), " +
                " adres_dom CHAR(200))";
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
            Returns ret;
            string counters_spis = Bank.pref + DBManager.sDataAliasRest + "counters_spis";
            string counters = Bank.pref + DBManager.sDataAliasRest + "counters";
            string counters_dom = Bank.pref + DBManager.sDataAliasRest + "counters_dom";
            string counters_group = Bank.pref + DBManager.sDataAliasRest + "counters_group";
            string sql;


            sql =
                " INSERT INTO " + tKvar +
                " (num_ls, nzp_kvar, nzp_dom, pkod, adres, adres_dom)" +
                " SELECT DISTINCT k.num_ls, k.nzp_kvar, k.nzp_dom, k.pkod," +
                " " + DBManager.sNvlWord + "(trim(r.rajon),'')||' ул.'||" + DBManager.sNvlWord + "(trim(u.ulica),'')||" +
                "' д.'||" + DBManager.sNvlWord + "(trim(d.ndom),'')||'/'||" + DBManager.sNvlWord + "(trim(d.nkor),'')||" +
                "' кв.'||" + DBManager.sNvlWord + "(trim(k.nkvar),'') as adres," +
                " " + DBManager.sNvlWord + "(trim(r.rajon),'')||' ул.'||" + DBManager.sNvlWord + "(trim(u.ulica),'')||" +
                "' д.'||" + DBManager.sNvlWord + "(trim(d.ndom),'')||'/'||" + DBManager.sNvlWord + "(trim(d.nkor),'') as adres_dom " +
                " FROM " + Bank.pref + DBManager.sDataAliasRest + "link_group g," +
                Bank.pref + DBManager.sDataAliasRest + "dom d," +
                Points.Pref + DBManager.sDataAliasRest + "s_ulica u, " +
                Points.Pref + DBManager.sDataAliasRest + "s_rajon r, " +
                Bank.pref + DBManager.sDataAliasRest + "kvar k " +
                " WHERE k.nzp_kvar = g.nzp" +
                " AND g.nzp_group in (" + (int)ECheckGroupId.ValIPUWithoutPU + "," + 
                (int)ECheckGroupId.ValODPUWithoutODPU + "," + (int)ECheckGroupId.ValGrPUWithoutGrPU + ")" +
                " AND g.nzp IS NOT NULL" +
                " AND k.nzp_dom = d.nzp_dom AND d.nzp_ul = u.nzp_ul AND r.nzp_raj = u.nzp_raj";
            ret = DBManager.ExecSQL(conn_db, sql, true);

            sql =
                " INSERT INTO " + table + " (show_order, pu_type, nzp_counter, adr)" +
                " SELECT DISTINCT 1, 'Показания ОДПУ', " + counters_dom + ".nzp_counter, k.adres_dom " +
                " FROM " + counters_dom + "" +
                " LEFT OUTER JOIN " + tKvar + " k ON k.nzp_dom = " + counters_dom + ".nzp_dom " +
                " WHERE is_actual <> 100  AND NOT EXISTS " +
                " (SELECT 1 FROM " + counters_spis + " cs" +
                " WHERE cs.nzp_counter = " + counters_dom + ".nzp_counter AND cs.nzp_type = 1) ";
            ret = DBManager.ExecSQL(conn_db, sql, true);


            sql =
                " INSERT INTO  " + table + " (show_order, pu_type, nzp_counter, adr)" +
                " SELECT DISTINCT 2, 'Показания Груп. ПУ', " + counters_group + ".nzp_counter, k.adres_dom " +
                " FROM " + counters_group +
                " LEFT OUTER JOIN " + Bank.pref + DBManager.sDataAliasRest + "counters_link l" +
                " ON l.nzp_counter = " + counters_group + ".nzp_counter" +
                " LEFT OUTER JOIN " + tKvar + " k ON  l.nzp_kvar = k.nzp_kvar" +
                " WHERE is_actual <> 100 AND NOT EXISTS" +
                " (SELECT 1 FROM " + counters_spis + " cs" +
                " WHERE cs.nzp_counter = " + counters_group + ".nzp_counter AND cs.nzp_type in (2,4)) ";
            ret = DBManager.ExecSQL(conn_db, sql, true);

            sql =
                " INSERT INTO  " + table + " (show_order, pu_type, nzp_counter, adr)" +
                " SELECT DISTINCT 3, 'Показания ИПУ', " + counters + ".nzp_counter, k.adres " +
                " FROM " + counters + " " +
                " LEFT OUTER JOIN " + tKvar + " k ON " + counters + ".nzp_kvar = k.nzp_kvar " +
                " WHERE is_actual <> 100 AND NOT EXISTS" +
                " (SELECT 1 FROM " + counters_spis + " cs" +
                " WHERE cs.nzp_counter = " + counters + ".nzp_counter AND cs.nzp_type = 3) ";
            ret = DBManager.ExecSQL(conn_db, sql, true);

            #region название группы

            //ИПУ------------------------------------------------------
            sql =
                " UPDATE " + table + " SET group_name = " +
                " (SELECT 'По данному отчету создана группа «'||trim(ngroup)||'»' " +
                "  FROM " + Bank.pref + DBManager.sDataAliasRest + "s_group" +
                "  WHERE nzp_group = " + (int) ECheckGroupId.ValODPUWithoutODPU + " )" +
                " WHERE show_order = 1 ";
            ret = DBManager.ExecSQL(conn_db, sql, true);
            //Квартирные ПУ--------------------------------------------
            sql =
                " UPDATE " + table + " SET group_name = " +
                " (SELECT 'По данному отчету создана группа «'||trim(ngroup)||'»' " +
                "  FROM " + Bank.pref + DBManager.sDataAliasRest + "s_group" +
                "  WHERE nzp_group = " + (int) ECheckGroupId.ValGrPUWithoutGrPU + " )" +
                " WHERE show_order = 2 ";
            ret = DBManager.ExecSQL(conn_db, sql, true);
            //Групповые ПУ--------------------------------------------
            sql =
                " UPDATE " + table + " SET group_name = " +
                " (SELECT 'По данному отчету создана группа «'||trim(ngroup)||'»' " +
                "  FROM " + Bank.pref + DBManager.sDataAliasRest + "s_group" +
                "  WHERE nzp_group = " + (int) ECheckGroupId.ValIPUWithoutPU + " )" +
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
                " SELECT DISTINCT trim(pu_type) as pu_type,  trim(group_name) as group_name," +
                " show_order, adr, nzp_counter" +
                " FROM " + table +
                " ORDER BY show_order, adr";

            DataTable dt = ClassDBUtils.OpenSQL(sql, conn_db).resultData;
            dt.TableName = "Q_master";
            ds_rep.Tables.Add(dt);
            return ds_rep;
        }

        protected override void SetNzpExc(int nzp_exc)
        {
            SetNzpExcInCheckChMon((int) ECheckGroupId.ValIPUWithoutPU, nzp_exc);
            SetNzpExcInCheckChMon((int) ECheckGroupId.ValODPUWithoutODPU, nzp_exc);
            SetNzpExcInCheckChMon((int) ECheckGroupId.ValGrPUWithoutGrPU, nzp_exc);
        }
    }
}
