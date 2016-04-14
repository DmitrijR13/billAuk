using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using Bars.KP50.Utils;
using Globals.SOURCE.Utility;
using SevenZip;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using System.Data;
using System.Linq;
using System.IO;
using STCLINE.KP50.Utility;
using Excel = Microsoft.Office.Interop.Excel; 


namespace STCLINE.KP50.DataBase
{
    /// <summary>
    /// Выгрузка реестра для Байкальска (ВСТКБ)
    /// </summary>
    public class BankDownloadReestrBaikalskVstkb : BaseBankUnloadReestr
    {
        protected List<string> Prefs;
        protected int IdReestr = 0;

        /// <summary>
        /// Создать временную таблицу tmp_reestr для данных реестра
        /// </summary>
        /// <param name="connDb">Соединение</param>
        /// <returns>Returns</returns>
        protected override Returns CreateReestrTempTable(IDbConnection connDb)
        {
            var ret = new Returns();

            ExecSQL(connDb, "drop table tmp_reestr", false);

            string sql = String.Concat("create temp table tmp_reestr (",
                " fio char(50), ",
                " adr char(100), ",
                " pkod " + DBManager.sDecimalType + "(13), ",
                " sum_insaldo " + DBManager.sDecimalType + "(14,2), " +
                " service integer, " +
                " date1 char(10), " +
                " date2 char(10), " +
                " sum_charge " + DBManager.sDecimalType + "(14,2) ");

            sql += ", pref char(10)) ";

            ret = ExecSQL(connDb, sql.ToString(), true);
            if (!ret.result)
            {
                ret.text = "Ошибка создания таблицы tmp_reestr\n" + ret.text;
            }

            return ret;
        }

        protected override Returns SetReestrAddress(IDbConnection conn_db, string pref)
        {
            throw new NotImplementedException();
        }

        protected override Returns SetCntsData(IDbConnection conn_db, string pref, DateTime calc_month)
        {
            throw new NotImplementedException();
        }

        protected override Returns UpdateReestrSumCharge(IDbConnection conn_db, string pref, string year, string month)
        {
            throw new NotImplementedException();
        }

        protected override int GetCounterCount()
        {
            return 0;
        }

        /// <summary>
        /// Отправка реестра
        /// </summary>
        /// <param name="connDb"></param>
        /// <param name="filename"></param>
        protected virtual void SendReestr(IDbConnection connDb, string filename)
        {

        }

        /// <summary>
        /// Cформировать строчку для записи в файл
        /// </summary>
        /// <param name="row">DataRow</param>
        /// <returns>string</returns>
        override protected string AssemblyOneString(DataRow row)
        {
            var idServ = row["service"] != DBNull.Value ? (Int32)row["service"] : 0;
            if (idServ == 8)
            {
                idServ = 800;
            }
            else if (idServ == 9)
            {
                idServ = 900;
            }
            string s = (row["fio"] != DBNull.Value ? ((string)row["fio"]).Trim() + ";" : ";") +
                (row["adr"] != DBNull.Value ? ((string)row["adr"]).Trim() + ";" : ";") +
                (row["pkod"] != DBNull.Value ? ((Decimal)row["pkod"]).ToString("0").Trim() + ";" : ";") +
                (row["sum_insaldo"] != DBNull.Value ? ((Decimal)row["sum_insaldo"]).ToString("0.00") + ";" : ";") +
                (idServ.ToString("0") + ";") +
                (row["date1"] != DBNull.Value ? ((string)row["date1"]).Trim() + ";" : ";") +
                (row["date2"] != DBNull.Value ? ((string)row["date2"]).Trim() + ";" : ";") +
                (row["sum_charge"] != DBNull.Value ? "1:" + ((Decimal)row["sum_charge"]).ToString("0.00") + ":" : "");

            return s;
        }


        protected override string GetFileName(int nzpReestr)
        {
            return DateTime.Now.ToShortDateString().Replace(".", "") + "_" + nzpReestr.ToString("000") + ".txt";
        }

        /// <summary>
        /// Добовляет в таблицу tmp_reestr начисления, разделенные по услугам
        /// </summary>
        /// <param name="connDb">Соединение</param>
        /// <param name="pref">Префикс</param>
        /// <param name="year">Год</param>
        /// <param name="month">Месяц</param>
        /// <returns>Returns</returns>
        protected virtual Returns CreateReestrCharge(IDbConnection connDb, string pref, DateTime date)
        {
            Returns ret = new Returns();

            string year = (date.Year - 2000).ToString("00");
            string month = date.Month.ToString("00");

            string sql = " insert into tmp_reestr (fio, adr, pkod, sum_insaldo, service, date1, date2, sum_charge, pref) " +
                " select ls.fio as fio, " +
                "       trim(" + DBManager.sNvlWord + "(r.rajon,''))||','||trim(" + DBManager.sNvlWord + "(u.ulica,''))||','||trim(" + DBManager.sNvlWord + "(ndom,''))||','||trim(" + DBManager.sNvlWord + "(ls.nkvar,'')) as adr," +
                "       ls.pkod as pkod, " + DBManager.sNvlWord + "(ch.sum_insaldo + ch.reval + ch.real_charge - ch.sum_money,0) as sum_insaldo, " + DBManager.sNvlWord + "(ch.nzp_serv,0) as service, '" + "01/" + month + "/" + date.Year.ToString() + "' as date1, " +
                "       '" + DateTime.DaysInMonth(date.Year, date.Month).ToString("00") + "/" + month + "/" + date.Year.ToString() + "' as date2, " +
                "       " + DBManager.sNvlWord + "(ch.sum_tarif,0) as sum_charge, " + Utils.EStrNull(pref) + " as pref" +
                " from tmp_spis_ls ls " +
                " inner join " + pref + "_charge_" + year + DBManager.tableDelimiter + "charge_" + month + " ch on (ch.nzp_kvar=ls.nzp_kvar and ch.dat_charge is null and ch.nzp_serv > 1)" +
                " inner join " + pref + sDataAliasRest + "dom d on d.nzp_dom = ls.nzp_dom " +
                " inner join " + pref + sDataAliasRest + "s_ulica u on d.nzp_ul = u.nzp_ul " +
                " inner join " + pref + sDataAliasRest + "s_rajon r on u.nzp_raj = r.nzp_raj ";
//                " inner join " + pref + sDataAliasRest + "s_town t on r.nzp_town=t.nzp_town ";

            ret = ExecSQL(connDb, sql.ToString(), true);

            if (!ret.result)
            {
                ret.text = "Ошибка обновления начислений в таблице tmp_reestr" + ret.text;
                return ret;
            }

            return ret;
        }

        virtual protected string[] GetAdditionLines(IDbConnection connDb)
        {
            var dtSum = ClassDBUtils.OpenSQL("select sum(sum_insaldo) as sum_insaldo from tmp_reestr where pkod > 0", connDb);
            if (dtSum.resultCode < 0) return null;

            string[] additionLines =
                {
                    "#FILESUM " + (dtSum.resultData.Rows[0]["sum_insaldo"] != DBNull.Value ? ((Decimal)dtSum.resultData.Rows[0]["sum_insaldo"]).ToString("0.00") : "0.00"),
                    "#TYPE 7",
                    "#SERVICE",
                    "#NOTE"
                };
            return additionLines;
        }
       
        /// <summary>
        /// Выгрузка данных в Сбербанк/ВСТКБ
        /// </summary>
        /// <param name="BanksList">Список банков данных</param>
        /// <returns>string</returns>
        public override Returns GetUploadReestr(Finder finder, List<int> BanksList, string statusLS)
        {
            Returns ret = new Returns();

            if (finder.nzp_user < 1)
            {
                return new Returns(false, "Не определен пользователь");
            }

            //Имя файла отчета
            string fileNameIn = "";
            ExcelRep excelRepDb = new ExcelRep();
            int nzpReestr = 0;

            IDbConnection connDb = null;

            try
            {
                //запись в БД о постановкe в поток(статус 0)
                ret = excelRepDb.AddMyFile(new ExcelUtility()
                {
                    nzp_user = finder.nzp_user,
                    status = ExcelUtility.Statuses.InProcess,
                    rep_name = "Выгрузка реестра для загрузки в БС",
                    is_shared = 1
                });
                if (!ret.result) throw new Exception(ret.text);
                _nzpExc = ret.tag;

                Prefs = GetPrefList(BanksList);

                string connKernel = Points.GetConnByPref(Points.Pref);
                connDb = DBManager.newDbConnection(connKernel);
                ret = OpenDb(connDb, true);
                if (!ret.result) throw new Exception(ret.text);

                #region определение локального пользователя
                /*DbWorkUser db = new DbWorkUser();
                finder.pref = Points.Pref;
                int nzpUser = db.GetLocalUser(connDb, finder, out ret);
                db.Close();
                if (!ret.result) throw new Exception(ret.text);*/
                int nzpUser = finder.nzp_user;
                //finder.nzp_user_main = nzpUser;
                #endregion

                #region реестр для загрузки в БС

                /// Получить список ЛС из выбранных локальных банков
                ret = GetLsData(connDb, Prefs, statusLS);
                if (!ret.result) throw new Exception(ret.text);

                // создать временную таблицу tmp_reestr для данных реестра
                ret = CreateReestrTempTable(connDb);
                if (!ret.result) throw new Exception(ret.text);

                // обновить начисления
                foreach (var pref in Prefs)
                {
                    //получаем расчетный месяц локального банка
                    var localDate = Points.GetCalcMonth(new CalcMonthParams(pref));

                    //получаем закрытый месяц локального банка
                    DateTime closeMonth = new DateTime(localDate.year_, localDate.month_, 1).AddMonths(-1);

                    // обновить начисления в таблице tmp_reestr
                    ret = CreateReestrCharge(connDb, pref, closeMonth);
                    if (!ret.result) throw new Exception(ret.text);
            }

                // сохранить список выгружаемых банков
                Utils.setCulture();
                string sql = " insert into " + Points.Pref + "_data" + DBManager.tableDelimiter + "tula_reestr_unloads (date_unload, unloading_date, user_unloaded, is_actual, nzp_exc) " +
                    " values (" + Utils.EStrNull(DateTime.Now.ToShortDateString()) + ", " + Utils.EStrNull(GetUnloadingData(Prefs)) + ", " + nzpUser + ", 1, " + _nzpExc + ");";
                ret = ExecSQL(connDb, sql);
                if (!ret.result) throw new Exception(ret.text);

                // получить ключ
                nzpReestr = Convert.ToInt32(ClassDBUtils.GetSerialKey(connDb, null));

                fileNameIn = GetFileName(nzpReestr);

                // сохранить имя файла
                sql = "update " + Points.Pref + "_data" + DBManager.tableDelimiter + "tula_reestr_unloads set name_file = " + Utils.EStrNull(fileNameIn) + " where nzp_reestr = " + nzpReestr;
                ret = ExecSQL(connDb, sql);
                if (!ret.result) throw new Exception("Ошибка обновления имени файла в таблице tula_reestr_unloads\n" + ret.text);

                // запись в файл
                WriteReestrToFile(connDb, fileNameIn, excelRepDb, GetAdditionLines(connDb));

                #endregion
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка выгрузки данных";

                // удалить реестр
                DeleteReestr(connDb, nzpReestr);

                excelRepDb.SetMyFileState(new ExcelUtility() { nzp_exc = _nzpExc, status = ExcelUtility.Statuses.Failed });

                MonitorLog.WriteLog("Ошибка в функции GetUploadReestr:\n" + ex.Message, MonitorLog.typelog.Error, true);
            }
            finally
            {
                if (ret.result)
                {
                    excelRepDb.SetMyFileProgress(new ExcelUtility() { nzp_exc = _nzpExc, progress = 1 });
                    excelRepDb.SetMyFileState(new ExcelUtility() { nzp_exc = _nzpExc, status = ExcelUtility.Statuses.Success, exc_path = fileNameIn });
                }

                excelRepDb.Close();

                if (connDb != null) connDb.Close();
            }

            return ret;
        }
    
    }

    /// <summary>
    /// Выгрузка реестра для Байкальска (Сбер)
    /// </summary>
    public class BankDownloadReestrBaikalskSber : BankDownloadReestrBaikalskVstkb
    {

        /// <summary>
        /// Добовляет в таблицу tmp_reestr начисления, разделенные по услугам
        /// </summary>
        /// <param name="connDb">Соединение</param>
        /// <param name="pref">Префикс</param>
        /// <param name="year">Год</param>
        /// <param name="month">Месяц</param>
        /// <returns>Returns</returns>
        protected override Returns CreateReestrCharge(IDbConnection connDb, string pref, DateTime date)
        {
            Returns ret = new Returns();

            string year = (date.Year - 2000).ToString("00");
            string month = date.Month.ToString("00");

            string sql = " insert into tmp_reestr (fio, adr, pkod, sum_insaldo, service, date1, date2, sum_charge, pref) " +
                " select ls.fio as fio, " +
                "       left(trim(" + DBManager.sNvlWord + "(r.rajon,'')), strpos(trim(" + DBManager.sNvlWord + "(r.rajon,'')), ' ')-1)||','||trim(" + DBManager.sNvlWord + "(u.ulica,''))||','||trim(" + DBManager.sNvlWord + "(ndom,''))||','||trim(" + DBManager.sNvlWord + "(ls.nkvar,'')) as adr," +
                "       ls.pkod as pkod, " + DBManager.sNvlWord + "(ch.sum_insaldo + ch.reval + ch.real_charge - ch.sum_money,0) as sum_insaldo, " + DBManager.sNvlWord + "(ch.nzp_serv,0) as service, '" + "01/" + month + "/" + date.Year.ToString() + "' as date1, " +
                "       '" + DateTime.DaysInMonth(date.Year, date.Month).ToString("00") + "/" + month + "/" + date.Year.ToString() + "' as date2, " +
                "       " + DBManager.sNvlWord + "(ch.sum_tarif,0) as sum_charge, " + Utils.EStrNull(pref) + " as pref" +
                " from tmp_spis_ls ls " +
                " inner join " + pref + "_charge_" + year + DBManager.tableDelimiter + "charge_" + month + " ch on (ch.nzp_kvar=ls.nzp_kvar and ch.dat_charge is null and ch.nzp_serv > 1)" +
                " inner join " + pref + sDataAliasRest + "dom d on d.nzp_dom = ls.nzp_dom " +
                " inner join " + pref + sDataAliasRest + "s_ulica u on d.nzp_ul = u.nzp_ul " +
                " inner join " + pref + sDataAliasRest + "s_rajon r on u.nzp_raj = r.nzp_raj ";
//                " inner join " + pref + sDataAliasRest + "s_town t on r.nzp_town=t.nzp_town ";

            ret = ExecSQL(connDb, sql.ToString(), true);

            if (!ret.result)
            {
                ret.text = "Ошибка обновления начислений в таблице tmp_reestr" + ret.text;
                return ret;
            }

            return ret;
        }

        override protected string[] GetAdditionLines(IDbConnection connDb)
        {
            var dtSum = ClassDBUtils.OpenSQL("select sum(sum_insaldo) as sum_insaldo from tmp_reestr where pkod > 0", connDb);
            if (dtSum.resultCode < 0) return null;

            string[] additionLines =
                {
                    "#FILESUM " + (dtSum.resultData.Rows[0]["sum_insaldo"] != DBNull.Value ? ((Decimal)dtSum.resultData.Rows[0]["sum_insaldo"]).ToString("0.00") : "0.00"),
                    "#TYPE 7",
                    "#SERVICE 8751",
                    "#NOTE"
                };
            return additionLines;
        }

    }

    /// <summary>
    /// Выгрузка реестра для Байкальска (Соцзащита)
    /// </summary>
    public class BankDownloadReestrBaikalskSocProtect : BankDownloadReestrBaikalskVstkb
    {
        private readonly int _numAcurals;
        private readonly int _numFormat;
        private string _taxInfo = "0000000000_000000000";
        private static List<ServiceQueryGenerator> _services;

        static BankDownloadReestrBaikalskSocProtect()
        {
            // порядок услуг важен
            _services = new List<ServiceQueryGenerator>
                {
                    new ServiceQueryGenerator(2, 16, 15, 206), // Размер начисленной платы за  жилое помещение
                    new ServiceQueryGenerator(25), // Размер начисленной платы за  электроснабжение (индив.потребл.)");
                    new ServiceQueryGenerator(515), // Размер начисленной платы за  электроснабжение (ОДН)
                    new ServiceQueryGenerator(10), // Размер начисленной платы за  газоснабжение
                    new ServiceQueryGenerator(6), // Размер начисленной платы за  холодное водоснабжение (индив.потребл.)
                    new ServiceQueryGenerator(510), // Размер начисленной платы за  холодное водоснабжение (ОДН)
                    new ServiceQueryGenerator(7), // Размер начисленной платы за  водоотведение (канализация)
                    new ServiceQueryGenerator(9, 1001009), // Размер начисленной платы за  горячее водоснабжение (индив.потребл.)
                    new ServiceQueryGenerator(513, 1001513), // Размер начисленной платы за  горячее водоснабжение (ОДН)
                    new ServiceQueryGenerator(8, 1001008, 1000808, 1001808), // Размер начисленной платы за  отопление (индив.потребл.)
                    new ServiceQueryGenerator(0), // Размер начисленной платы за  отопление (ОДН)
                    new ServiceQueryGenerator(324) // Размер начисленной платы за очистку стоков
                };
        }

        public BankDownloadReestrBaikalskSocProtect(int numAcurals, int numFormat)
        {
            this._numAcurals = numAcurals;
            this._numFormat = numFormat;
        }

        protected override string[] GetAdditionLines(IDbConnection connDb)
        {
            return null;
        }


        protected override Returns CreateReestrTempTable(IDbConnection connDb)
        {
            var ret = new Returns();

            ExecSQL(connDb, "drop table tmp_reestr", false);

            string sql = "create temp table tmp_reestr (" +
                " ls integer," +
                " town char(50)," +
                " street char(50)," +
                " house char(50)," +
                " housing char(50)," +
                " flat char(50)," +
                " square " + DBManager.sDecimalType + "(10,2), " +
                " ownership char(50)," +
                " rg_count integer," +
                " ft_count integer," +
                " period char(50)";

            for (var i = 1; i <= _numAcurals; i++)
            {
                sql += ", acrual" + i + " " + DBManager.sDecimalType + "(14,2) ";

            }
                sql += ", acrual_sum " + DBManager.sDecimalType + "(14,2), " +
                " dbt_mont integer";

            sql += ", pref char(10)) ";

            ret = ExecSQL(connDb, sql.ToString(), true);
            if (!ret.result)
            {
                ret.text = "Ошибка создания таблицы tmp_reestr\n" + ret.text;
            }

            return ret;
        }

        /// <summary>
        /// Добовляет в таблицу tmp_reestr начисления, разделенные по услугам
        /// </summary>
        /// <param name="connDb">Соединение</param>
        /// <param name="pref">Префикс</param>
        /// <param name="date">Дата</param>
        /// <returns>Returns</returns>
        protected override Returns CreateReestrCharge(IDbConnection connDb, string pref, DateTime date)
        {
            Returns ret = new Returns();

            var year = (date.Year - 2000).ToString("00");
            var month = date.Month.ToString("00");
            var pathToChargeTable = string.Format("{0}_charge_{1}{2}charge_{3}", pref, year, DBManager.tableDelimiter, month);
            var pathToGilTable = string.Format("{0}_charge_{1}{2}gil_{3}", pref, year, DBManager.tableDelimiter, month);
            var dataSchemaName = pref + sDataAliasRest;

            var currentMonth = DBManager.MDY(date.Month, 1, date.Year);
            var nextDate = date.AddMonths(1);
            var nextMonth = DBManager.MDY(nextDate.Month, 1, nextDate.Year);

            // parameters
            ExecSQL(connDb, "DROP TABLE IF EXISTS tmp_prm_1", false);

            ret = ExecSQL(connDb, string.Format(@"
CREATE TEMP TABLE tmp_prm_1 (
  nzp INTEGER NOT NULL,
  nzp_prm INTEGER NOT NULL,
  val_prm VARCHAR(20) NULL)"), true);
            if (!ret.result)
            {
                ret.text = "Ошибка создания таблицы tmp_prm_1\n" + ret.text;
                return ret;
            }

            ret = ExecSQL(connDb, string.Format(@"
INSERT INTO tmp_prm_1 (nzp, nzp_prm, val_prm)
SELECT nzp, nzp_prm, MAX(val_prm)
FROM {0}prm_1 p
     INNER JOIN tmp_spis_ls tl ON tl.nzp_kvar = p.nzp
WHERE nzp_prm IN (4, 8, 2004) AND is_actual = 1 AND dat_s < {1} AND dat_po >= {2}
GROUP BY nzp, nzp_prm", dataSchemaName, nextMonth, currentMonth), true);

            if (!ret.result)
            {
                ret.text = "Ошибка заполнения таблицы tmp_prm_1\n" + ret.text;
                return ret;
            }

            // insert
            var sqlBuilder = new StringBuilder(" INSERT INTO tmp_reestr (ls, town, street, house, housing, flat, square, ownership, rg_count, ft_count, period");
            for (var i = 1; i <= _numAcurals; i++)
            {
                sqlBuilder.Append(", acrual");
                sqlBuilder.Append(i);
            }
            sqlBuilder.Append(", acrual_sum, dbt_mont, pref)");
            sqlBuilder.AppendLine();

            // select
            sqlBuilder.AppendFormat(@"SELECT {0}(poid.val_prm::int, k.nzp_kvar) as ls, 
r.rajon town,
CONCAT_WS(' ', TRIM(u.ulicareg), TRIM(u.ulica)) as street,
UPPER(d.ndom) as house,
CASE WHEN d.nkor = '-' THEN NULL ELSE UPPER(d.nkor) END as housing,
CASE WHEN k.nkvar = '-' THEN NULL ELSE k.nkvar END as flat_number,
{0}(ps.val_prm::numeric, 0) as square,
CASE WHEN pp.val_prm::int = 1 THEN 'Приватизированная' ELSE 'Не в собственности' END as ownership,
{0}(g.people_count, 0) as rg_count,
{0}(g.real_people_count, 0) as ft_count,
'{1} {2}' as period,", DBManager.sNvlWord, CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(date.Month), date.Year);
            sqlBuilder.AppendLine();

            foreach (var service in _services)
            {
                sqlBuilder.Append(service.GetChargeSelect());
                sqlBuilder.AppendLine(",");
            }

            for (var i = 0; i < _services.Count; i++)
            {
                var service = _services[i];

                if (i != 0)
                {
                    sqlBuilder.Append(" + ");
                }

                sqlBuilder.Append(service.GetChargeSelect());
            }
            sqlBuilder.AppendLine(",");

            sqlBuilder.AppendFormat(@"CASE WHEN ch.debt > 0 AND ch.charge > 0.01 THEN CEIL(ch.debt / ch.charge) WHEN ch.debt > 0 AND ch.charge <= 0.01 THEN 1 ELSE 0 END as dbt_mont,
{0} as pref
FROM {0}_data.kvar k
     INNER JOIN tmp_spis_ls tl ON tl.nzp_kvar = k.nzp_kvar
     INNER JOIN {1}dom d ON d.nzp_dom = k.nzp_dom
     INNER JOIN {1}s_ulica u ON u.nzp_ul = d.nzp_ul 
     INNER JOIN {1}s_rajon r ON r.nzp_raj = u.nzp_raj 
     LEFT JOIN tmp_prm_1 ps ON ps.nzp = k.nzp_kvar AND ps.nzp_prm = 4
     LEFT JOIN tmp_prm_1 pp ON pp.nzp = k.nzp_kvar AND pp.nzp_prm = 8
     LEFT JOIN tmp_prm_1 poid ON poid.nzp = k.nzp_kvar AND poid.nzp_prm = 2004
     LEFT JOIN (SELECT nzp_kvar, COUNT(nzp_gil) people_count, SUM(CASE WHEN g.temp_absense = 0 THEN 1 ELSE 0 END) real_people_count
                FROM (SELECT g.nzp_kvar, g.nzp_gil, SUM(CASE WHEN gp.nzp_glp IS NULL THEN 0 ELSE 1 END) temp_absense
                      FROM {4} g
						   LEFT JOIN baikal_data.gil_periods gp ON gp.nzp_gilec = g.nzp_gil AND gp.dat_s < {2} AND gp.dat_po >= {3} AND gp.nzp_tkrt = 2 AND gp.is_actual = 1
					  WHERE g.stek = 1 AND g.dat_s < {3} AND g.dat_po >= {2}
                      GROUP BY g.nzp_kvar, g.nzp_gil) g
                GROUP BY nzp_kvar) g ON g.nzp_kvar = k.nzp_kvar", Utils.EStrNull(pref), dataSchemaName, currentMonth, nextMonth, pathToGilTable);
            sqlBuilder.AppendLine();

            foreach (var service in _services)
            {
                sqlBuilder.AppendLine(service.GetChargeJoin(pathToChargeTable, "k"));
            }

            sqlBuilder.AppendFormat(@"LEFT JOIN (SELECT nzp_kvar, SUM(sum_insaldo - sum_money + reval + real_charge) debt, SUM(sum_tarif) charge
FROM {0} ch
WHERE nzp_serv > 1 AND dat_charge IS NULL
GROUP BY nzp_kvar) ch ON ch.nzp_kvar = k.nzp_kvar", pathToChargeTable);

            ret = ExecSQL(connDb, sqlBuilder.ToString(), true);

            if (!ret.result)
            {
                ret.text = "Ошибка обновления начислений в таблице tmp_reestr" + ret.text;
            }
            else
            {
                string sqlStr = " SELECT distinct inn||'_'||kpp as info FROM "+Points.Pref+"_kernel.s_payer p "+
                                " inner join "+Points.Pref+"_kernel.s_bank b on b.nzp_payer = p.nzp_payer "+
                                " inner join "+Points.Pref+"_data.tula_s_bank tb on tb.nzp_bank = b.nzp_bank and tb.is_actual <> 100 and tb.download_format_number = " + _numFormat;

                object obj = ExecScalar(connDb, sqlStr, out ret, true);
                if (obj != null && obj != DBNull.Value)
                {
                    _taxInfo = Convert.ToString(obj);
                }
            }

            ExecSQL(connDb, "DROP TABLE IF EXISTS tmp_prm_1", false);

                return ret;
            }

        protected override string AssemblyOneString(DataRow row)
        {
            throw new NotImplementedException();
        }

        protected string GetAddCode()
        {
            return "_20";
        }

        protected override string GetFileName(int nzpReestr)
        {
            var date = DateTime.Now;
            var localDate = Points.GetCalcMonth(new CalcMonthParams(Prefs[0]));

            return "100_" + date.Year.ToString("0000").Substring(2) + date.Month.ToString("00") + date.Day.ToString("00") + "_" + _taxInfo + GetAddCode() + "_" + (localDate.month_ - 1).ToString("00") + ".xls";
        }

        protected override void WriteReestrToFile(IDbConnection conn_db, string fileNameIn, ExcelRep excelRepDb, string[] additionLines)
        {
            var ret = new Returns();
            string dir = Path.Combine(Constants.ExcelDir, fileNameIn);

            var exlApp = new Microsoft.Office.Interop.Excel.Application();
            exlApp.DisplayAlerts = false;
            var exlWb = exlApp.Workbooks.Add(System.Reflection.Missing.Value);
            var exlWs = (Excel.Worksheet)exlWb.Worksheets.get_Item(1);
            exlWs.Visible = Excel.XlSheetVisibility.xlSheetVisible;

            try
            {
                int sheetCount = exlWb.Worksheets.Count;
                for (var i = 1; i < sheetCount; i++)
                {
                    // Удаляем всегда второй лист
                    Excel.Worksheet sheet = exlWb.Worksheets[2];
                    sheet.Delete();
                }
                exlWs.Name = "LGO_v4";

                var dt = ClassDBUtils.OpenSQL("select * from tmp_reestr", conn_db);
                if (dt.resultCode < 0)
                {
                    throw new Exception(dt.resultMessage);
                }

                int num = dt.resultData.Rows.Count;

                //подготовка работы с  Excel

                //проставляем цифры в первой строке
                for (var i = 1; i <= 26; i++)
                {
                    exlWs.Cells[1, i] = i.ToString();
                }
                //заголовок во второй
                exlWs.Cells[2, 1] = "Номер лицевого счета";
                exlWs.Cells[2, 2] = "Населенный пункт";
                exlWs.Cells[2, 3] = "Улица";
                exlWs.Cells[2, 4] = "Дом";
                exlWs.Cells[2, 5] = "Корпус";
                exlWs.Cells[2, 6] = "Квартира";
                exlWs.Cells[2, 7] = "Размер занимаемой общей площади";
                exlWs.Cells[2, 8] = "Вид собственности";
                exlWs.Cells[2, 9] = "Количество граждан, зарегистрированных по адресу";
                exlWs.Cells[2, 10] =
                    "Количество граждан, на которых осуществляется начисление платы за коммунальные услуги";
                exlWs.Cells[2, 11] = "Месяц";
                exlWs.Cells[2, 12] = "Размер начисленной платы за  жилое помещение";
                exlWs.Cells[2, 13] = "Размер начисленной платы за  электроснабжение (индив.потребл.)";
                exlWs.Cells[2, 14] = "Размер начисленной платы за  электроснабжение (ОДН)";
                exlWs.Cells[2, 15] = "Размер начисленной платы за  газоснабжение";
                exlWs.Cells[2, 16] = "Размер начисленной платы за  холодное водоснабжение (индив.потребл.)";
                exlWs.Cells[2, 17] = "Размер начисленной платы за  холодное водоснабжение (ОДН)";
                exlWs.Cells[2, 18] = "Размер начисленной платы за  водоотведение (канализация)";
                exlWs.Cells[2, 19] = "Размер начисленной платы за  горячее водоснабжение (индив.потребл.)";
                exlWs.Cells[2, 20] = "Размер начисленной платы за  горячее водоснабжение (ОДН)";
                exlWs.Cells[2, 21] = "Размер начисленной платы за  отопление (индив.потребл.)";
                exlWs.Cells[2, 22] = "Размер начисленной платы за  отопление (ОДН)";
                exlWs.Cells[2, 23] = "Размер начисленной платы за очистку стоков";
                exlWs.Cells[2, 24] = "Итого сумма начисленной платы за ЖКУ (12+13+14+15+16+17+18+19+20+21+22+23)";
                exlWs.Cells[2, 25] =
                    "Количество месяцев, за которые имеется задолженность по оплате жилищно- коммунальных услуг";
                exlWs.Cells[2, 26] = "Примечания";

                for (int j = 0; j < num; j++)
                {
                    DataRow row = dt.resultData.Rows[j];
                    Excel.Range range;

                    range = exlWs.Cells[j + 3, 1] as Excel.Range;
                    if (range != null)
                    {
                        range.NumberFormat = "@";
                        range.Value2 = row["ls"].ToString().Trim();
                    }

                    range = exlWs.Cells[j + 3, 2] as Excel.Range;
                    if (range != null)
                    {
                        range.NumberFormat = "@";
                        range.Value2 = row["town"].ToString().Trim();
                    }

                    range = exlWs.Cells[j + 3, 3] as Excel.Range;
                    if (range != null)
                    {
                        range.NumberFormat = "@";
                        range.Value2 = row["street"].ToString().Trim();
                    }

                    range = exlWs.Cells[j + 3, 4] as Excel.Range;
                    if (range != null)
                    {
                        range.NumberFormat = "@";
                        range.Value2 = row["house"].ToString().Trim();
                    }

                    range = exlWs.Cells[j + 3, 5] as Excel.Range;
                    if (range != null)
                    {
                        range.NumberFormat = "@";
                        range.Value2 = row["housing"].ToString().Trim();
                    }

                    range = exlWs.Cells[j + 3, 6] as Excel.Range;
                    if (range != null)
                    {
                        range.NumberFormat = "@";
                        range.Value2 = row["flat"].ToString().Trim();
                    }

                    range = exlWs.Cells[j + 3, 7] as Excel.Range;
                    if (range != null)
                    {
                        range.NumberFormat = "0.00";
                        range.Value2 = Decimal.Parse(row["square"].ToString().Trim());
                    }

                    range = exlWs.Cells[j + 3, 8] as Excel.Range;
                    if (range != null)
                    {
                        range.NumberFormat = "@";
                        range.Value2 = row["ownership"].ToString().Trim();
                    }

                    range = exlWs.Cells[j + 3, 9] as Excel.Range;
                    if (range != null)
                    {
                        range.NumberFormat = "0";
                        range.Value2 = Int32.Parse(row["rg_count"].ToString().Trim());
                    }

                    range = exlWs.Cells[j + 3, 10] as Excel.Range;
                    if (range != null)
                    {
                        range.NumberFormat = "0";
                        range.Value2 = Int32.Parse(row["ft_count"].ToString().Trim());
                    }

                    range = exlWs.Cells[j + 3, 11] as Excel.Range;
                    if (range != null)
                    {
                        range.NumberFormat = "@";
                        range.Value2 = row["period"].ToString();
                    }

                    range = exlWs.Cells[j + 3, 12] as Excel.Range;
                    if (range != null)
                    {
                        range.NumberFormat = "0.00";
                        range.Value2 = Decimal.Parse(row["acrual1"].ToString().Trim());
                    }

                    range = exlWs.Cells[j + 3, 13] as Excel.Range;
                    if (range != null)
                    {
                        range.NumberFormat = "0.00";
                        range.Value2 = Decimal.Parse(row["acrual2"].ToString().Trim());
                    }

                    range = exlWs.Cells[j + 3, 14] as Excel.Range;
                    if (range != null)
                    {
                        range.NumberFormat = "0.00";
                        range.Value2 = Decimal.Parse(row["acrual3"].ToString().Trim());
                    }

                    range = exlWs.Cells[j + 3, 15] as Excel.Range;
                    if (range != null)
                    {
                        range.NumberFormat = "0.00";
                        range.Value2 = Decimal.Parse(row["acrual4"].ToString().Trim());
                    }

                    range = exlWs.Cells[j + 3, 16] as Excel.Range;
                    if (range != null)
                    {
                        range.NumberFormat = "0.00";
                        range.Value2 = Decimal.Parse(row["acrual5"].ToString().Trim());
                    }

                    range = exlWs.Cells[j + 3, 17] as Excel.Range;
                    if (range != null)
                    {
                        range.NumberFormat = "0.00";
                        range.Value2 = Decimal.Parse(row["acrual6"].ToString().Trim());
                    }

                    range = exlWs.Cells[j + 3, 18] as Excel.Range;
                    if (range != null)
                    {
                        range.NumberFormat = "0.00";
                        range.Value2 = Decimal.Parse(row["acrual7"].ToString().Trim());
                    }

                    range = exlWs.Cells[j + 3, 19] as Excel.Range;
                    if (range != null)
                    {
                        range.NumberFormat = "0.00";
                        range.Value2 = Decimal.Parse(row["acrual8"].ToString().Trim());
                    }

                    range = exlWs.Cells[j + 3, 20] as Excel.Range;
                    if (range != null)
                    {
                        range.NumberFormat = "0.00";
                        range.Value2 = Decimal.Parse(row["acrual9"].ToString().Trim());
                    }

                    range = exlWs.Cells[j + 3, 21] as Excel.Range;
                    if (range != null)
                    {
                        range.NumberFormat = "0.00";
                        range.Value2 = Decimal.Parse(row["acrual10"].ToString().Trim());
                    }

                    range = exlWs.Cells[j + 3, 22] as Excel.Range;
                    if (range != null)
                    {
                        range.NumberFormat = "0.00";
                        range.Value2 = Decimal.Parse(row["acrual11"].ToString().Trim());
                    }

                    range = exlWs.Cells[j + 3, 23] as Excel.Range;
                    if (range != null)
                    {
                        range.NumberFormat = "0.00";
                        range.Value2 = Decimal.Parse(row["acrual12"].ToString().Trim());
                    }

                    range = exlWs.Cells[j + 3, 24] as Excel.Range;
                    if (range != null)
                    {
                        range.NumberFormat = "0.00";
                        range.Value2 = row["acrual_sum"].ToString().Trim();
                    }

                    range = exlWs.Cells[j + 3, 25] as Excel.Range;
                    if (range != null)
                    {
                        range.NumberFormat = "0";
                        range.Value2 = row["dbt_mont"].ToString().Trim();
                    }

                    range = exlWs.Cells[j + 3, 26] as Excel.Range;
                    if (range != null)
                    {
                        range.NumberFormat = "@";
                        range.Value2 = "";
                    }

                    if (j%100 == 0)
                        excelRepDb.SetMyFileProgress(new ExcelUtility() {nzp_exc = _nzpExc, progress = ((decimal) j)/num});
                }

                exlApp.DefaultSaveFormat = Excel.XlFileFormat.xlWorkbookNormal;
                //???
                exlWb.Saved = true;
                //Не Отображать сообщение о замене существующего
                exlApp.DisplayAlerts = false;
                //Формат сохраняемого файла
                //CurrentExcellApp.DefaultSaveFormat = Excel.XlFileFormat.xlExcel9795;

                exlWb.SaveAs(dir, Excel.XlFileFormat.xlWorkbookNormal, //Excel.XlFileFormat.xlExcel9795,
                    Type.Missing, Type.Missing, Type.Missing,
                    Type.Missing, Excel.XlSaveAsAccessMode.xlNoChange, Type.Missing,
                    Type.Missing, Type.Missing, Type.Missing, Type.Missing);
                exlWb.Save();
            }
            catch (Exception ex)
            {
                // удалить реестр
                throw new Exception("Ошибка при записи данных, функция GetUploadReestr " + ex.Message);
            }
            finally
            {
                if (exlWb != null)
                {
                    Marshal.ReleaseComObject(exlWs);
                    exlWb.Close(false, Type.Missing, Type.Missing);
                    //ExlWb.Close(Type.Missing, Type.Missing, Type.Missing);
                    Marshal.ReleaseComObject(exlWb);
                    exlWb = null;
                }
                ///////////////////////////////
                //добавить какой нибудь рестарт
                ///////////////////////////////
                exlApp.Quit();
                Marshal.ReleaseComObject(exlApp);
                exlApp = null;
            }

            if (InputOutput.useFtp)
            {
                try
                {
                    InputOutput.SaveOutputFile(dir);
                }
                catch (Exception ex)
                {
                    // удалить реестр
                    throw new Exception("Ошибка при передаче данных на web-сервер, функция GetUploadReestr " + ex.Message);
                }
            }
        }

        #region CLASSES
        private class ServiceQueryGenerator
        {
            private const string CHARGE_TABLE_ALIAS = "ch";

            public ServiceQueryGenerator(int baseServiceId, params int[] includedServices)
            {
                BaseService = baseServiceId;
                IncludedServices = new List<int>(includedServices)
                    {
                        baseServiceId
                    };
            }

            public int BaseService { get; private set; }

            private List<int> IncludedServices { get; set; }

            public string GetChargeSelect()
            {
                if (BaseService == 0)
                    return "0";

                return string.Format("{0}({1}{2}.charge, 0)", DBManager.sNvlWord, CHARGE_TABLE_ALIAS, BaseService);
            }

            public string GetChargeJoin(string chargeTableName, string joinTableAlias)
            {
                if (BaseService == 0)
                    return string.Empty;

                return string.Format(
                    "LEFT JOIN (SELECT nzp_kvar, SUM(sum_tarif + reval + real_charge) charge FROM {0} WHERE nzp_serv IN ({1}) AND dat_charge IS NULL GROUP BY nzp_kvar) {2}{3} ON {2}{3}.nzp_kvar = {4}.nzp_kvar",
                    chargeTableName, string.Join(", ", IncludedServices), CHARGE_TABLE_ALIAS, BaseService,
                    joinTableAlias);
            }
        } 
        #endregion

        override protected Returns UpdateReestrSumCharge(IDbConnection conn_db, string pref, string year, string month)
        {
            throw new NotImplementedException();
        }
    }
}
