using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Globals.SOURCE.GLOBAL;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.DataImport.CHECK.Report
{
    class Report_CheckRassoglOplat: ReportCheckTemplate
    { 
        private string tPaymentDistrib;
        private string tPaymentInSaldo;
        private string tPaymentPerekidka;
        private string tKvar;
        private string tPackLs;
        private string tFn;
        private string tFs;
        private string tCharge;

        public Report_CheckRassoglOplat(IDbConnection connection, CheckBeforeClosingParams inputParams)
        {
            conn_db = connection;
            reportFrxSource = "checks_rassogl_oplat.frx";
            fileName = "c" + inputParams.Bank.pref + "_" + "CheckRassoglOplat.fpx";
            fullFileName = "Отчет Рассогласование оплат";
            User = inputParams.User;
            Bank = inputParams.Bank;
            Month = Points.GetCalcMonth(new CalcMonthParams{pref = inputParams.Bank.pref}).month_;
            Year = Points.GetCalcMonth(new CalcMonthParams { pref = inputParams.Bank.pref }).year_;

            tPaymentDistrib = "t_check_payment_distrib_" + User.nzp_user;
            tPaymentInSaldo = "t_payment_in_saldo_" + User.nzp_user;
            tPaymentPerekidka = "t_payment_perekidka_" + User.nzp_user;
            tKvar = "t_kvar_check_rassog_opl_" + User.nzp_user;
            tPackLs = "t_pack_ls_check_rassog_opl_" + User.nzp_user;
            tFn = "t_fn_check_rassog_opl_" + User.nzp_user;
            tFs = "t_fs_check_rassog_opl_" + User.nzp_user;
            tCharge = "t_charge_check_rassog_opl_" + User.nzp_user;
        }

        protected override void CreateTempTable()
        {
            string sql =
                " CREATE TEMP TABLE " + tPaymentDistrib +
                " (num_ls " + DBManager.sDecimalType + "(13,0)," +
                " pkod " + DBManager.sDecimalType + "(13,0), " +
                " adres CHAR(200), " +
                " data_oplata CHAR(20)," +
                " kvit CHAR(30)," +
                " sum_post_oplat " + DBManager.sDecimalType + "(14,2)," +
                " sum_uchten_oplat " + DBManager.sDecimalType + "(14,2)," +
                " difference " + DBManager.sDecimalType + "(14,2)," +
                " type_name CHAR(60))" + 
                DBManager.sUnlogTempTable; 
            DBManager.ExecSQL(conn_db, sql, true);

            sql =
                " CREATE TEMP TABLE " + tPaymentInSaldo +
                " (num_ls " + DBManager.sDecimalType + "(13,0)," +
                " pkod " + DBManager.sDecimalType + "(13,0), " +
                " adres CHAR(200), " +
                " sum_oplata_agent " + DBManager.sDecimalType + "(14,2)," +
                " sum_oplata_uk " + DBManager.sDecimalType + "(14,2)," +
                " sum_post_oplat " + DBManager.sDecimalType + "(14,2)," +
                " sum_uchten_oplat " + DBManager.sDecimalType + "(14,2)," +
                " including_from_agent " + DBManager.sDecimalType + "(14,2)," +
                " including_from_uk " + DBManager.sDecimalType + "(14,2)," +
                " difference_from_agent " + DBManager.sDecimalType + "(14,2)," +
                " difference_from_uk " + DBManager.sDecimalType + "(14,2)," +
                " difference " + DBManager.sDecimalType + "(14,2) )" +
                DBManager.sUnlogTempTable;
            DBManager.ExecSQL(conn_db, sql, true);

            sql =
                " CREATE TEMP TABLE " + tPaymentPerekidka +
                " (num_ls " + DBManager.sDecimalType + "(13,0)," +
                " pkod " + DBManager.sDecimalType + "(13,0), " +
                " adres CHAR(200), " +
                " sum_oplata_agent " + DBManager.sDecimalType + "(14,2)," +
                " sum_uchten_oplat " + DBManager.sDecimalType + "(14,2)," +
                " difference " + DBManager.sDecimalType + "(14,2))" +
                DBManager.sUnlogTempTable;
            DBManager.ExecSQL(conn_db, sql, true);

            sql =
                " CREATE TEMP TABLE " + tKvar +
                " (num_ls " + DBManager.sDecimalType + "(13,0)," +
                " pkod " + DBManager.sDecimalType + "(13,0), " +
                " adres CHAR(200)," +
                " kod INTEGER)";
            DBManager.ExecSQL(conn_db, sql, true);

            sql = 
                " CREATE TEMP TABLE " + tPackLs +
                " (num_ls " + DBManager.sDecimalType + "(13,0)," +
                " data_oplata CHAR(20)," +
                " kvit_oplata CHAR(20)," +
                " sum_prih " + DBManager.sDecimalType + "(14,2)," +
                " nzp_pack_ls " + DBManager.sDecimalType + "(13,0)," +
                " kod_sum INTEGER)";
            DBManager.ExecSQL(conn_db, sql, true);

            sql =
                " CREATE TEMP TABLE " + tFn +
                " (num_ls " + DBManager.sDecimalType + "(13,0)," +
                " nzp_pack_ls " + DBManager.sDecimalType + "(13,0)," +
                " dat_uchet DATE," +
                " sum_prih " + DBManager.sDecimalType + "(14,2))";
            DBManager.ExecSQL(conn_db, sql, true);

            sql =
                " CREATE TEMP TABLE " + tFs +
                " (num_ls " + DBManager.sDecimalType + "(13,0)," +
                " nzp_pack_ls " + DBManager.sDecimalType + "(13,0)," +
                " dat_uchet DATE," +
                " sum_prih " + DBManager.sDecimalType + "(14,2))";
            DBManager.ExecSQL(conn_db, sql, true);

            sql =
                " CREATE TEMP TABLE " + tCharge +
                " (num_ls " + DBManager.sDecimalType + "(13,0)," +
                " money_to " + DBManager.sDecimalType + "(14,2)," +
                " money_from " + DBManager.sDecimalType + "(14,2)," +
                " money_del " + DBManager.sDecimalType + "(14,2))";
            DBManager.ExecSQL(conn_db, sql, true);
        }

        protected override void DropTempTable()
        {
            string sql = " DROP TABLE " + tPaymentDistrib;
            DBManager.ExecSQL(conn_db, sql, false);

            sql = " DROP TABLE " + tPaymentInSaldo;
            DBManager.ExecSQL(conn_db, sql, false);

            sql = " DROP TABLE " + tPaymentPerekidka;
            DBManager.ExecSQL(conn_db, sql, false);

            sql = " DROP TABLE " + tKvar;
            DBManager.ExecSQL(conn_db, sql, false);

            sql = " DROP TABLE " + tPackLs;
            DBManager.ExecSQL(conn_db, sql, false);

            sql = " DROP TABLE " + tFn;
            DBManager.ExecSQL(conn_db, sql, false);

            sql = " DROP TABLE " + tFs;
            DBManager.ExecSQL(conn_db, sql, false);

            sql = " DROP TABLE " + tCharge;
            DBManager.ExecSQL(conn_db, sql, false);
        }

        protected override void FillTempTable()
        {
            string sql;
            int nextMonth = (Month < 12 ? Month + 1 : 1);
            int nextYear = (Month < 12 ? Year : Year + 1);
            string fin = Points.Pref + "_fin_" + Year.ToString().Substring(2, 2) + DBManager.tableDelimiter;
            string charge = Bank.pref + "_charge_" + Year.ToString().Substring(2, 2) + DBManager.tableDelimiter;

            #region заполнение временных таблиц

            sql =
                " INSERT INTO " + tKvar +
                " (num_ls, pkod, adres, kod)" +
                " SELECT k.num_ls, k.pkod," +
                " " + DBManager.sNvlWord + "(trim(r.rajon),'')||' ул.'||" + DBManager.sNvlWord + "(trim(u.ulica),'')||" +
                "' д.'||" + DBManager.sNvlWord + "(trim(d.ndom),'')||'/'||" + DBManager.sNvlWord + "(trim(d.nkor),'')||" +
                "' кв.'||" + DBManager.sNvlWord + "(trim(k.nkvar),'') as adres," +
                " g.nzp_group" +
                " FROM " + Bank.pref + DBManager.sDataAliasRest + "link_group g," +
                Bank.pref + DBManager.sDataAliasRest + "dom d," +
                Points.Pref + DBManager.sDataAliasRest + "s_ulica u, " +
                Points.Pref + DBManager.sDataAliasRest + "s_rajon r, " +
                Bank.pref + DBManager.sDataAliasRest + "kvar k " + 
                " WHERE k.nzp_kvar = g.nzp" +
                " AND g.nzp_group in (" + (int)ECheckGroupId.RassogPaymentDistrib + "," +
                (int)ECheckGroupId.RassoglPaymentInSaldo + "," + (int)ECheckGroupId.RassoglPaymentPerekidka + ")" +
                " AND k.nzp_dom = d.nzp_dom AND d.nzp_ul = u.nzp_ul AND r.nzp_raj = u.nzp_raj";
            DBManager.ExecSQL(conn_db, sql, true);

            sql =
                " INSERT INTO " + tPackLs +
                " (num_ls, data_oplata, kvit_oplata, sum_prih, kod_sum, nzp_pack_ls)" +
                " SELECT num_ls, dat_uchet, info_num, g_sum_ls, kod_sum, nzp_pack_ls" +
                " FROM " + fin + "pack_ls" +
                " WHERE dat_uchet >= '01." + Month.ToString("00") + "." + Year + "'" +
                " AND dat_uchet < '01." + nextMonth.ToString("00") + "." + nextYear + "'" +
                " AND num_ls IN" +
                " (SELECT num_ls FROM " + tKvar + ")";
            ;DBManager.ExecSQL(conn_db, sql, true);

            sql = 
                " INSERT INTO " + tFn +
                " (num_ls, sum_prih, nzp_pack_ls, dat_uchet)" +
                " SELECT num_ls, sum(sum_prih), nzp_pack_ls, dat_uchet" +
                " FROM " + charge + "fn_supplier" + Month.ToString("00") + "" +
                " WHERE num_ls IN" +
                " (SELECT num_ls FROM " + tKvar + ")" +
                " GROUP BY num_ls, nzp_pack_ls, dat_uchet";
            DBManager.ExecSQL(conn_db, sql, true);

            sql =
                " INSERT INTO " + tFs +
                " (num_ls, sum_prih, nzp_pack_ls, dat_uchet)" +
                " SELECT num_ls, sum(sum_prih), nzp_pack_ls, dat_uchet" +
                " FROM " + charge + "from_supplier fs" +
                " WHERE dat_uchet >= '01." + Month.ToString("00") + "." + Year + "'" +
                " AND dat_uchet < '01." + nextMonth.ToString("00") + "." + nextYear + "'" +
                " AND num_ls IN" +
                " (SELECT num_ls FROM " + tKvar + ")" +
                " GROUP BY num_ls, nzp_pack_ls, dat_uchet";
            DBManager.ExecSQL(conn_db, sql, true);

            sql = 
                " INSERT INTO " + tCharge +
                " (num_ls, money_to, money_from, money_del)" +
                " SELECT num_ls, sum(money_to), sum(money_from), sum(money_del)" +
                " FROM " + charge + "charge_" + Month.ToString("00") +
                " WHERE num_ls IN" +
                " (SELECT num_ls FROM " + tKvar + ")" +
                " AND nzp_serv > 1 AND dat_charge IS NULL" +
                " GROUP BY num_ls";
            DBManager.ExecSQL(conn_db, sql, true);
            #endregion

            #region Отчет Рассогласование в распределении оплат

            sql =
                " INSERT INTO " + tPaymentDistrib +
                " (num_ls, pkod, adres,  data_oplata, kvit, sum_post_oplat, sum_uchten_oplat, difference, type_name)" +
                " SELECT k.num_ls, k.pkod, k.adres, p.data_oplata, p.kvit_oplata, " +
                " " + DBManager.sNvlWord + "(p.sum_prih,0), " + DBManager.sNvlWord + "(fn.sum_prih,0), " +
                " " + DBManager.sNvlWord + "(p.sum_prih,0) - " + DBManager.sNvlWord + "(fn.sum_prih,0)," +
                " 'Рассогласование в распределении оплат на счета агента'" +
                " FROM " + tKvar + " k" +
                " LEFT OUTER JOIN " + tPackLs + " p ON k.num_ls = p.num_ls AND p.kod_sum in" +
                "    (SELECT kod FROM " + Points.Pref + DBManager.sKernelAliasRest + "kodsum WHERE is_erc = 1)" +
                " LEFT OUTER JOIN " + tFn + " fn ON fn.num_ls = k.num_ls AND fn.nzp_pack_ls = p.nzp_pack_ls" +
                " WHERE k.kod = " + (int)ECheckGroupId.RassogPaymentDistrib +
                " AND CAST(" + DBManager.sNvlWord + "(p.sum_prih,0) - " + DBManager.sNvlWord + "(fn.sum_prih,0)" +
                " as " + DBManager.sDecimalType + "(14,2)) <> 0";
            DBManager.ExecSQL(conn_db, sql, true);

            sql =
                " INSERT INTO " + tPaymentDistrib +
                " (num_ls, pkod, adres,  data_oplata, kvit, sum_post_oplat, sum_uchten_oplat, difference, type_name)" +
                " SELECT k.num_ls, k.pkod, k.adres, fn.dat_uchet, '', 0," +
                " " + DBManager.sNvlWord + "(fn.sum_prih,0), " +
                " -" + DBManager.sNvlWord + "(fn.sum_prih,0)," +
                " 'Рассогласование в распределении оплат на счета агента'" +
                " FROM " + tKvar + " k" +
                " LEFT OUTER JOIN " + tFn + " fn ON fn.num_ls = k.num_ls AND NOT EXISTS" +
                "    (SELECT 1 FROM " + tPackLs + " p where p.nzp_pack_ls = fn.nzp_pack_ls AND p.kod_sum in" +
                "    (SELECT kod FROM " + Points.Pref + DBManager.sKernelAliasRest + "kodsum WHERE is_erc = 1)) " +
                " WHERE k.kod = " + (int)ECheckGroupId.RassogPaymentDistrib +
                " AND CAST(" + DBManager.sNvlWord + "(fn.sum_prih,0) as " + DBManager.sDecimalType + "(14,2)) <> 0";
            DBManager.ExecSQL(conn_db, sql, true);


            sql =
                " INSERT INTO " + tPaymentDistrib +
                " (num_ls, pkod, adres,  data_oplata, kvit, sum_post_oplat, sum_uchten_oplat, difference, type_name)" +
                " SELECT k.num_ls, k.pkod, k.adres, p.data_oplata, p.kvit_oplata, " +
                " " + DBManager.sNvlWord + "(p.sum_prih,0), " + DBManager.sNvlWord + "(fs.sum_prih,0)," +
                " " + DBManager.sNvlWord + "(p.sum_prih,0) - " + DBManager.sNvlWord + "(fs.sum_prih,0)," +
                " 'Рассогласование в распределении оплат от УК и ПУ'" +
                " FROM " + tKvar + " k" +
                " LEFT OUTER JOIN " + tPackLs + " p ON k.num_ls = p.num_ls AND p.kod_sum in" +
                "    (SELECT kod FROM " + Points.Pref + DBManager.sKernelAliasRest + "kodsum WHERE is_erc = 0)" +
                " LEFT OUTER JOIN " + tFs + " fs ON fs.num_ls = k.num_ls AND fs.nzp_pack_ls = p.nzp_pack_ls" +
                " WHERE k.kod = " + (int)ECheckGroupId.RassogPaymentDistrib +
                " AND CAST(" + DBManager.sNvlWord + "(p.sum_prih,0) - " + DBManager.sNvlWord + "(fs.sum_prih,0)" +
                " as " + DBManager.sDecimalType + "(14,2)) <> 0";
            DBManager.ExecSQL(conn_db, sql, true);

            sql =
                " INSERT INTO " + tPaymentDistrib +
                " (num_ls, pkod, adres,  data_oplata, kvit, sum_post_oplat, sum_uchten_oplat, difference, type_name)" +
                " SELECT k.num_ls, k.pkod, k.adres, fs.dat_uchet, '', 0," +
                " " + DBManager.sNvlWord + "(fs.sum_prih,0), " +
                " -" + DBManager.sNvlWord + "(fs.sum_prih,0)," +
                " 'Рассогласование в распределении оплат от УК и ПУ'" +
                " FROM " + tKvar + " k" +
                " LEFT OUTER JOIN " + tFs + " fs ON fs.num_ls = k.num_ls AND NOT EXISTS" +
                "    (SELECT 1 FROM " + tPackLs + " p where p.nzp_pack_ls = fs.nzp_pack_ls AND p.kod_sum in" +
                "    (SELECT kod FROM " + Points.Pref + DBManager.sKernelAliasRest + "kodsum WHERE is_erc = 0)) " +
                " WHERE k.kod = " + (int)ECheckGroupId.RassogPaymentDistrib +
                " AND CAST(" + DBManager.sNvlWord + "(fs.sum_prih,0) as " + DBManager.sDecimalType + "(14,2)) <> 0";
            DBManager.ExecSQL(conn_db, sql, true);

            #endregion

            #region Отчет Рассогласование учета оплат в сальдо
            sql =
                " INSERT INTO " + tPaymentInSaldo + " (num_ls, pkod, adres,  " +
                " sum_oplata_agent, sum_oplata_uk, sum_post_oplat," +
                " including_from_agent, including_from_uk, sum_uchten_oplat," +
                " difference_from_agent, difference_from_uk, difference)" +
                " SELECT k.num_ls, k.pkod, k.adres, " +
                " " + DBManager.sNvlWord + "(sum(fn.sum_prih),0), " + DBManager.sNvlWord + "(sum(fs.sum_prih),0)," +
                " " + DBManager.sNvlWord + "(sum(fn.sum_prih),0) + " + DBManager.sNvlWord + "(sum(fs.sum_prih),0)," +
                " " + DBManager.sNvlWord + "(c.money_to,0), " + DBManager.sNvlWord + "(c.money_from,0)," +
                " " + DBManager.sNvlWord + "(c.money_to,0) + " + DBManager.sNvlWord + "(c.money_from,0)," +
                " " + DBManager.sNvlWord + "(sum(fn.sum_prih),0) - " + DBManager.sNvlWord + "(c.money_to,0)," +
                " " + DBManager.sNvlWord + "(sum(fs.sum_prih),0) - " + DBManager.sNvlWord + "(c.money_from,0)," +
                " (" + DBManager.sNvlWord + "(sum(fn.sum_prih),0) + " + DBManager.sNvlWord + "(sum(fs.sum_prih),0)) - " +
                " (" + DBManager.sNvlWord + "(c.money_to,0) + " + DBManager.sNvlWord + "(c.money_from,0))" +
                " FROM " + tKvar + " k" +
                " LEFT OUTER JOIN " + tFn + " fn ON fn.num_ls = k.num_ls" +
                " LEFT OUTER JOIN " + tFs + " fs ON fs.num_ls = k.num_ls" +
                " LEFT OUTER JOIN " + tCharge + " c ON k.num_ls = c.num_ls" +
                " WHERE k.kod = " + (int)ECheckGroupId.RassoglPaymentInSaldo +
                " GROUP BY k.num_ls, k.pkod, k.adres, c.money_to, c.money_from";
            DBManager.ExecSQL(conn_db, sql, true);
           
            #endregion

            #region Отчет Рассогласование в перекидках оплат
            //в charge сумма не собвпадает с del_supplier
            sql =
                " INSERT INTO " + tPaymentPerekidka + " (num_ls, pkod, adres,  " +
                " sum_oplata_agent, " +
                " sum_uchten_oplat, difference)" +
                " SELECT DISTINCT k.num_ls, k.pkod, k.adres," +
                " " + DBManager.sNvlWord + "(sum(ds.sum_prih),0),  c.money_del," +
                " CAST(" + DBManager.sNvlWord + "(sum(ds.sum_prih),0)- c.money_del AS " + DBManager.sDecimalType + "(14,2)) " +
                " FROM " + tKvar + " k" +
                " LEFT OUTER JOIN " + charge + "charge_" + Month.ToString("00") + " c " +
                "    ON c.num_ls = k.num_ls " +
                " LEFT OUTER JOIN " + charge + "del_supplier ds " +
                "   ON ds.num_ls = k.num_ls AND ds.nzp_supp = c.nzp_supp AND ds.nzp_serv = c.nzp_serv" +
                "   AND ds.dat_account >= '01." + Month.ToString("00") + "." + Year + "'" +
                "   AND ds.dat_account < '01." + nextMonth.ToString("00") + "." + nextYear + "' " +
                " WHERE k.kod = " + (int)ECheckGroupId.RassoglPaymentPerekidka +
                " GROUP BY k.num_ls, k.pkod, k.adres, c.money_del" +
                " HAVING cast(" + DBManager.sNvlWord + "(sum(ds.sum_prih),0)- " + 
                DBManager.sNvlWord + "(c.money_del,0) AS " + DBManager.sDecimalType + "(14,2)) <> 0";
            DBManager.ExecSQL(conn_db, sql, true);
            //в del_supplier то, чего нет в charge
            sql =
                " INSERT INTO " + tPaymentPerekidka + " (num_ls, pkod, adres,  " +
                " sum_oplata_agent, difference)" +
                " SELECT DISTINCT k.num_ls, k.pkod, k.adres,," +
                " " + DBManager.sNvlWord + "(sum(ds.sum_prih),0),  " +
                " " + DBManager.sNvlWord + "(sum(ds.sum_prih),0)  " +
                " FROM " + tKvar + " k" +
                " LEFT OUTER JOIN " + charge + "del_supplier ds " +
                "   ON ds.num_ls = k.num_ls AND ds.dat_account >= '01." + Month.ToString("00") + "." + Year + "'" +
                "   AND ds.dat_account < '01." + nextMonth.ToString("00") + "." + nextYear + "' " +
                " WHERE k.kod = " + (int)ECheckGroupId.RassoglPaymentPerekidka +
                " AND NOT EXISTS" +
                "    (SELECT 1 FROM " + tPaymentPerekidka + " t" +
                "     WHERE k.num_ls = t.num_ls)" +
                " GROUP BY k.num_ls, k.pkod, adres ";
            DBManager.ExecSQL(conn_db, sql, true);
            #endregion

        }


        protected override void SetParamValues(FastReport.Report rep)
        {
            var months = new[] { "","Январь", "Февраль", "Март",
            "Апрель", "Май", "Июнь", "Июль", "Август", "Сентябрь",
            "Октябрь","Ноябрь","Декабрь"};
            rep.SetParameterValue("month", months[Month]);
            rep.SetParameterValue("year", Year);
            rep.SetParameterValue("bank",
                (from x in Points.PointList
                 where x.pref == Bank.pref
                 select x.point).First());
            #region сообщение "Создана группа..."


            string sql;
            DataTable dt;
            sql =
                " SELECT DISTINCT nzp_kvar FROM " + Bank.pref + "_charge_" + Year.ToString().Substring(2, 2) +
                DBManager.tableDelimiter + "charge_" + Month.ToString("00") +
                " WHERE num_ls IS NULL";
            dt = DBManager.ExecSQLToTable(conn_db, sql);
            if (dt.Rows.Count > 0)
            {
                List<string> kvars = new List<string>();
                foreach (DataRow row in dt.Rows)
                {
                    kvars.Add(row["nzp_kvar"].ToString());
                }
                rep.SetParameterValue("mess1", "Для лицевых счетов " + String.Join(", ", kvars) +
                    "  в перечне начислений не определен номер ЛС. Смотрите информационный журнал системы (error.log).");
                MonitorLog.WriteLog(
                    "Для лицевых счетов " + String.Join(", ", kvars) + " в таблице " +
                    Bank.pref + "_charge_" + Year.ToString().Substring(2, 2) +
                    DBManager.tableDelimiter + "charge_" + Month.ToString("00") +
                    " поле num_ls имеет значение null." + Environment.NewLine + sql, MonitorLog.typelog.Error, true);
                rep.SetParameterValue("mess2", "");
                rep.SetParameterValue("mess3", "");
            }
            else
            {
                sql =
                    " SELECT trim(ngroup) as ngroup " +
                    " FROM " + Bank.pref + DBManager.sDataAliasRest + "s_group" +
                    " WHERE nzp_group = " + (int)ECheckGroupId.RassogPaymentDistrib + "" +
                    " AND EXISTS" +
                    " (SELECT 1 FROM " + Bank.pref + DBManager.sDataAliasRest + "link_group " +
                    " WHERE nzp_group = " + (int)ECheckGroupId.RassogPaymentDistrib + ")";
                dt = DBManager.ExecSQLToTable(conn_db, sql);
                rep.SetParameterValue("mess1", dt.Rows.Count > 0
                    ? "Создана группа «" + dt.Rows[0]["ngroup"] + "»"
                    : "Проверка прошла успешно");

                sql =
                    " SELECT trim(ngroup) as ngroup " +
                    " FROM " + Bank.pref + DBManager.sDataAliasRest + "s_group" +
                    " WHERE nzp_group = " + (int)ECheckGroupId.RassoglPaymentInSaldo + "" +
                    " AND EXISTS" +
                    " (SELECT 1 FROM " + Bank.pref + DBManager.sDataAliasRest + "link_group " +
                    " WHERE nzp_group = " + (int)ECheckGroupId.RassoglPaymentInSaldo + ")";
                dt = DBManager.ExecSQLToTable(conn_db, sql);
                rep.SetParameterValue("mess2", dt.Rows.Count > 0
                    ? "Создана группа «" + dt.Rows[0]["ngroup"] + "»"
                    : "Проверка прошла успешно");

                sql =
                    " SELECT trim(ngroup) as ngroup " +
                    " FROM " + Bank.pref + DBManager.sDataAliasRest + "s_group" +
                    " WHERE nzp_group = " + (int)ECheckGroupId.RassoglPaymentPerekidka + "" +
                    " AND EXISTS" +
                    " (SELECT 1 FROM " + Bank.pref + DBManager.sDataAliasRest + "link_group " +
                    " WHERE nzp_group = " + (int)ECheckGroupId.RassoglPaymentPerekidka + ")";
                dt = DBManager.ExecSQLToTable(conn_db, sql);
                rep.SetParameterValue("mess3", dt.Rows.Count > 0
                    ? "Создана группа «" + dt.Rows[0]["ngroup"] + "»"
                    : "Проверка прошла успешно");
            }


            #endregion
        }

        protected override DataSet AddDataSource()
        {
            DataSet ds_rep = new DataSet();
            string sql =
                " SELECT num_ls, pkod, adres, kvit, data_oplata, sum_post_oplat, sum_uchten_oplat, difference, type_name" +
                " FROM " + tPaymentDistrib + 
                " ORDER BY adres";
            DataTable dt = ClassDBUtils.OpenSQL(sql, conn_db).resultData;
            dt.TableName = "Q_master";
            ds_rep.Tables.Add(dt);

            
            sql =
                " SELECT num_ls, pkod, adres,  " +
                " sum_oplata_agent, sum_oplata_uk, sum_post_oplat," +
                " including_from_agent, including_from_uk, sum_uchten_oplat," +
                " difference_from_agent, difference_from_uk, difference" +
                " FROM " + tPaymentInSaldo +
                " ORDER BY adres";
            DataTable dt2 = ClassDBUtils.OpenSQL(sql, conn_db).resultData;
            dt2.TableName = "Q_master2";
            ds_rep.Tables.Add(dt2);

            sql =
                " SELECT num_ls, pkod, adres, " +
                " sum_oplata_agent, " +
                " sum_uchten_oplat,  difference" +
                " FROM " + tPaymentPerekidka +
                " ORDER BY adres";
            DataTable dt3 = ClassDBUtils.OpenSQL(sql, conn_db).resultData;
            dt3.TableName = "Q_master3";
            ds_rep.Tables.Add(dt3);

            return ds_rep;
        }

        protected override void SetNzpExc(int nzp_exc)
        {
            SetNzpExcInCheckChMon((int)ECheckGroupId.RassogPaymentDistrib, nzp_exc);
            SetNzpExcInCheckChMon((int)ECheckGroupId.RassoglPaymentInSaldo, nzp_exc);
            SetNzpExcInCheckChMon((int)ECheckGroupId.RassoglPaymentPerekidka, nzp_exc);
        }
    }
}
