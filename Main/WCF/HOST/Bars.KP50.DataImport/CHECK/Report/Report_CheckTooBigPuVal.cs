using System;
using System.Data;
using Globals.SOURCE.GLOBAL;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.DataImport.CHECK.Report
{
    class Report_CheckTooBigPuVal: ReportCheckTemplate
    {
        private string table = "t_check_too_big_pu_values_" + DateTime.Now.DayOfYear + DateTime.Now.Hour + DateTime.Now.Minute;
        private string t_table_from_check;
        private string tKvar;
        //если нет параметров
        private string no_param = "";

        public Report_CheckTooBigPuVal(IDbConnection connection, CheckBeforeClosingParams inputParams, string temp_table)
        {
            conn_db = connection;
            reportFrxSource = "check_too_big_pu_values.frx";
            fileName = "c" + inputParams.Bank.pref + "_" + "CheckTooBigPuVal.fpx";
            fullFileName = "Отчет по показаниям ПУ,  расходы по которым превышают предельные  значения ";
            User = inputParams.User;
            Bank = inputParams.Bank;
            Month = Points.GetCalcMonth(new CalcMonthParams { pref = inputParams.Bank.pref }).month_;
            Year = Points.GetCalcMonth(new CalcMonthParams { pref = inputParams.Bank.pref }).year_;
            
            tKvar = "t_kvar_check_pu_val_" + User.nzp_user;
            t_table_from_check = temp_table;
        }

        protected override void CreateTempTable()
        {
            string sql =
                " CREATE TEMP TABLE " + table +
                " (show_order INTEGER," +
                " pu_type CHAR(20), " +
                " group_name CHAR(200), " +
                " num_ls " + DBManager.sDecimalType + "(13,0)," +
                " pkod " + DBManager.sDecimalType + "(13,0), " +
                " adr CHAR(200), " +
                " count_num CHAR(20), " +
                " serv CHAR(100), " +
                " dat_s DATE," +
                " count_val_s " + DBManager.sDecimalType + "(14,2)," +
                " dat_po DATE," +
                " count_val_po " + DBManager.sDecimalType + "(14,2)," +
                " limit_val " + DBManager.sDecimalType + "(14,2)," +
                " rashod " + DBManager.sDecimalType + "(14,2))" + 
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
            int nextMonth = (Month < 12 ? Month + 1 : 1);
            int nextYear = (Month < 12 ? Year : Year + 1);
            string firstDay = "01." + Month.ToString("00") + "." + Year;
            string nextFirstDay = "01." + nextMonth.ToString("00") + "." + nextYear;
            string sql;

            #region Проверяем заполненность всех параметров

            sql =
                " SELECT * FROM  " + Bank.pref + DBManager.sDataAliasRest + "prm_10" +
                " WHERE is_actual<>100" +
                " AND dat_s <='" + nextFirstDay + "' and dat_po>='" + firstDay + "'" +
                " AND nzp_prm in(2081,2082,2083,2084,2085,2086,2087,2088,1457)";
            DataTable dt = DBManager.ExecSQLToTable(conn_db, sql);
            if (dt.Rows.Count < 9)
            {
                no_param = "Не заполнены все предельные значения для счетчиков!";
                return;
            }
            #endregion

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
              " AND g.nzp_group in (" + (int)ECheckGroupId.TooBigIPUVal + "," + (int)ECheckGroupId.TooBigODPUVal + 
              "," + (int)ECheckGroupId.TooBigKvarPUVal + "," + (int)ECheckGroupId.TooBigGroupVal + ")" +
              " AND k.nzp_dom = d.nzp_dom AND d.nzp_ul = u.nzp_ul AND r.nzp_raj = u.nzp_raj";
            DBManager.ExecSQL(conn_db, sql, true);

            sql =
                " INSERT INTO " + table + " (pu_type, show_order, num_ls, pkod,  adr, count_num, serv, " +
                " dat_s, count_val_s, dat_po, count_val_po, limit_val, rashod)" +
                " SELECT t.pu_type, t.show_order, k.num_ls, k.pkod, k.adres, t.count_num, s.service," +
                " t.dat_s, t.count_val_s, t.dat_po, t.count_val_po, t.limit_val, t.rashod  " +
                " FROM " + tKvar + " k," +
                t_table_from_check + " t, " +
                Bank.pref + DBManager.sKernelAliasRest + "services s " +
                " WHERE s.nzp_serv = t.nzp_serv AND k.nzp_kvar = t.nzp_kvar" ;
            DBManager.ExecSQL(conn_db, sql, true);
            
            #region название группы
            //ИПУ------------------------------------------------------
            sql =
                " UPDATE " + table + " SET group_name = " +
                " (SELECT 'По данному отчету создана группа «'||trim(ngroup)||'»' " +
                "  FROM " + Bank.pref + DBManager.sDataAliasRest + "s_group" +
                "  WHERE nzp_group = " + (int)ECheckGroupId.TooBigIPUVal + " )" +
                " WHERE show_order = 1 ";
            DBManager.ExecSQL(conn_db, sql, true);
            //Квартирные ПУ--------------------------------------------
            sql =
                " UPDATE " + table + " SET group_name = " +
                " (SELECT 'По данному отчету создана группа «'||trim(ngroup)||'»' " +
                "  FROM " + Bank.pref + DBManager.sDataAliasRest + "s_group" +
                "  WHERE nzp_group = " + (int)ECheckGroupId.TooBigKvarPUVal + " )" +
                " WHERE show_order = 2 ";
            DBManager.ExecSQL(conn_db, sql, true);
            //Групповые ПУ--------------------------------------------
            sql =
                " UPDATE " + table + " SET group_name = " +
                " (SELECT 'По данному отчету создана группа «'||trim(ngroup)||'»' " +
                "  FROM " + Bank.pref + DBManager.sDataAliasRest + "s_group" +
                "  WHERE nzp_group = " + (int)ECheckGroupId.TooBigGroupVal + " )" +
                " WHERE show_order = 3 ";
            DBManager.ExecSQL(conn_db, sql, true);
            //ОДПУ-----------------------------------------------------
            sql =
                " UPDATE " + table + " SET group_name = " +
                " (SELECT 'По данному отчету создана группа «'||trim(ngroup)||'»' " +
                "  FROM " + Bank.pref + DBManager.sDataAliasRest + "s_group" +
                "  WHERE nzp_group = " + (int)ECheckGroupId.TooBigODPUVal + " )" +
                " WHERE show_order = 4 ";
            DBManager.ExecSQL(conn_db, sql, true);
            #endregion
            
        }

        protected override void SetParamValues(FastReport.Report rep)
        {
            rep.SetParameterValue("no_param", no_param);
        }

        protected override DataSet AddDataSource()
        {
            DataSet ds_rep = new DataSet();
            string sql =
                " SELECT DISTINCT trim(pu_type) as pu_type,  trim(group_name) as group_name," +
                " show_order, num_ls, pkod,  adr, count_num, serv, " +
                " dat_s, count_val_s, dat_po, count_val_po, limit_val, rashod" +
                " FROM " + table +
                " ORDER BY show_order, adr";

            DataTable dt = ClassDBUtils.OpenSQL(sql, conn_db).resultData;
            dt.TableName = "Q_master";
            ds_rep.Tables.Add(dt);
            return ds_rep;
        }

        protected override void SetNzpExc(int nzp_exc)
        {
            SetNzpExcInCheckChMon((int)ECheckGroupId.TooBigIPUVal, nzp_exc);
            SetNzpExcInCheckChMon((int)ECheckGroupId.TooBigODPUVal, nzp_exc);
            SetNzpExcInCheckChMon((int)ECheckGroupId.TooBigGroupVal, nzp_exc);
            SetNzpExcInCheckChMon((int)ECheckGroupId.TooBigKvarPUVal, nzp_exc);
        }
    }
}
