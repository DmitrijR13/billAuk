using System;
using System.Collections.Generic;
using System.Text;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using System.Data;
using System.Linq;
using System.IO;

namespace STCLINE.KP50.DataBase
{
    abstract public class BaseBankUnloadReestr : DataBaseHead
    {
        protected int _nzpUser = 0;
        // количество строк в протоколе выгрузки
        protected int _protocolRecordCnt = 0;

        protected int _nzpExc = 0;

        /// <summary>
        /// Выгрузка данных в Сбербанк
        /// </summary>
        /// <param name="BanksList">Список банков данных</param>
        /// <returns>string</returns>
        public virtual Returns GetUploadReestr(Finder finder, List<int> BanksList, string statusLS)
        {
            Returns ret = new Returns();
            int numberOfPu = GetCounterCount();

            if (finder.nzp_user < 1)
            {
                return new Returns(false, "Не определен пользователь");
            }
            _nzpUser = finder.nzp_user;

            //Имя файла отчета
            string fileNameIn = "";
            int nzp_reestr = 0;

            IDbConnection conn_db = null;

            using (ExcelRep excelRepDb = new ExcelRep())
            {
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

                    // открыть соединение
                    conn_db = DBManager.newDbConnection(Points.GetConnByPref(Points.Pref));
                    ret = OpenDb(conn_db, true);
                    if (!ret.result) throw new Exception(ret.text);

                    // получить локального пользователя
                    //int nzpUser = GetUser(conn_db, finder);
                    int nzpUser = finder.nzp_user;

                    // получить список префиксов
                    List<string> prefs = GetPrefList(BanksList);

                    /// Получить список ЛС из выбранных локальных банков
                    GetLsData(conn_db, prefs, statusLS);

                    // создать временную таблицу tmp_reestr для данных реестра
                    CreateReestrTempTable(conn_db);


                    if (numberOfPu > 0)
                    {
                        // создать временную таблицу tmp_cnts для показаний ПУ
                        CreateCntsTempTable(conn_db);
                    }

                    // записать в таблицу tmp_reestr адреса
                    foreach (var pref in prefs)
                    {
                        SetReestrAddress(conn_db, pref);
                    }

                    /// Создать индексы для таблицы tmp_reestr
                    CreateReestrIndex(conn_db);

                    // обновить начисления
                    foreach (var pref in prefs)
                    {
                        //получаем расчетный месяц локального банка
                        var local_date = Points.GetCalcMonth(new CalcMonthParams(pref));

                        //получаем закрытый месяц локального банка
                        DateTime close_month = new DateTime(local_date.year_, local_date.month_, 1).AddMonths(-1);

                        // обновить начисления в таблице tmp_reestr
                        UpdateReestrSumCharge(conn_db, pref, (close_month.Year - 2000).ToString("00"), close_month.Month.ToString("00"));
                    }


                    if (numberOfPu > 0)
                    {
                        // получить список ПУ с показаниями
                        foreach (var pref in prefs)
                        {
                            //получаем расчетный месяц локального банка
                            var local_date = Points.GetCalcMonth(new CalcMonthParams(pref));
                            DateTime current_month = new DateTime(local_date.year_, local_date.month_, 1);

                            // записать данные по ПУ во временную таблицу tmp_cnts
                            SetCntsData(conn_db, pref, current_month);
                        }


                        /// Создать индексы для таблицы tmp_cnts
                        CreateCntsIndex(conn_db);

                        /// Получить протокол выгрузки 
                        GetUnloadProtocol(conn_db);

                        /// Проставить номера для приборов учета
                        SetCntsNumbers(conn_db);

                        /// Сохранить в реестр данные по ПУ
                        SetReestrCnts(conn_db);
                    }
                    //Удалить записи с дублирующимися pkod
                    ret = ExecSQL(conn_db, "DELETE FROM tmp_reestr WHERE nzp_kvar in (SELECT nzp_kvar FROM tmp_reestr GROUP BY nzp_kvar HAVING count(pkod)>1 )");
                    if (!ret.result) throw new Exception("Ошибка проверки уникальности платежных кодов\n" + ret.text);

                    Utils.setCulture();

                    // получить имена выгружамых банков
                    string unloading_date = GetUnloadingData(prefs);

                    // получить ключ реестра
                    nzp_reestr = InsertReestr(conn_db, unloading_date, nzpUser);

                    // сформировать имя файла
                    fileNameIn = GetFileName(nzp_reestr);

                    // cохранить имя файла
                    UpdateReestr(conn_db, fileNameIn, unloading_date, nzp_reestr);

                    // записать данные реестра в файл
                    WriteReestrToFile(conn_db, fileNameIn, excelRepDb, null);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка выгрузки данных";

                    // удалить реестр
                    DeleteReestr(conn_db, nzp_reestr);

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

                    if (conn_db != null) conn_db.Close();
                }
            }

            return ret;
        }

        /// <summary>
        /// Получить список префиксов
        /// </summary>
        /// <param name="BanksList"></param>
        /// <returns></returns>
        protected List<string> GetPrefList(List<int> BanksList)
        {
            List<string> prefs = new List<string>();

            foreach (var point in Points.PointList)
            {
                for (int i = 0; i < BanksList.Count; i++)
                {
                    if (point.nzp_wp == BanksList[i])
                    {
                        prefs.Add(point.pref);
                    }
                }
            }

            return prefs;
        }

        /*private int GetUser(IDbConnection conn_db, Finder finder)
        {
            int nzpUser = 0;
            
            using (DbWorkUser db = new DbWorkUser())
            {
                Returns ret = new Returns(true);
                
                finder.pref = Points.Pref;
                nzpUser = db.GetLocalUser(conn_db, finder, out ret);
                db.Close();
                if (!ret.result) throw new Exception(ret.text);
            }

            return nzpUser;
        }*/

        /// <summary>
        /// Создать временную таблицу tmp_reestr для данных реестра
        /// </summary>
        /// <param name="conn_db">Соединение</param>
        /// <returns>Returns</returns>
        protected abstract Returns CreateReestrTempTable(IDbConnection conn_db);

        /// <summary>
        /// Записать адреса во временную таблицу tmp_reestr для реестра
        /// </summary>
        /// <param name="conn_db">Соединение</param>
        /// <returns>Returns</returns>
        protected abstract Returns SetReestrAddress(IDbConnection conn_db, string pref);

        /// <summary>
        /// Создать временную таблицу tmp_cnts для показаний ПУ
        /// </summary>
        /// <param name="conn_db">Соединение</param>
        /// <returns>Returns</returns>
        protected Returns CreateCntsTempTable(IDbConnection conn_db)
        {
            Returns ret = new Returns();

            ExecSQL(conn_db, "drop table tmp_cnts", false);

            string sql = "create temp table tmp_cnts (" +
                "  nzp_serial  serial, " +
                "  service     varchar(100), " +
                "  name_type   varchar(40), " +
                "  nzp         integer, " +
                "  nzp_serv    integer, " +
                "  nzp_counter integer, " +
                "  num         integer," +
                "  val_cnt     float, " +
                "  pseudonym   varchar(40), " +
                "  cnt         char(200) ) ";

            ret = ExecSQL(conn_db, sql.ToString(), true);
            if (!ret.result) throw new Exception(ret.text);

            return ret;
        }

        /// <summary>
        /// Записать данные по ПУ во временную таблицу tmp_cnts
        /// </summary>
        /// <param name="conn_db">Соединение</param>
        /// <param name="pref">Префикс</param>
        /// <param name="dat_uchet">Дата учета</param>
        /// <returns>Returns</returns>
        protected abstract Returns SetCntsData(IDbConnection conn_db, string pref, DateTime calc_month);

        /// <summary>
        /// Создать индексы для таблицы с данных по ПУ
        /// </summary>
        /// <param name="conn_db">Соединение</param>
        /// <returns>Returns</returns>
        protected Returns CreateCntsIndex(IDbConnection conn_db)
        {
            Returns ret = ExecSQL(conn_db, "  create index ix_tmp_cnts_1 on tmp_cnts(nzp);", true);
            if (!ret.result) throw new Exception(ret.text);

            ret = ExecSQL(conn_db, DBManager.sUpdStat + "  tmp_cnts;", true);
            if (!ret.result) throw new Exception(ret.text);

            return ret;
        }

        /// <summary>
        /// Создать индексы для реестра
        /// </summary>
        /// <param name="conn_db">Соединение</param>
        /// <returns>Returns</returns>
        protected Returns CreateReestrIndex(IDbConnection conn_db)
        {
            Returns ret = ExecSQL(conn_db, "create index ind_x1 on tmp_reestr(nzp_kvar)");
            if (!ret.result) throw new Exception(ret.text);

            ret = ExecSQL(conn_db, DBManager.sUpdStat + " tmp_reestr");
            if (!ret.result) throw new Exception(ret.text);

            return ret;
        }

        /// <summary>
        /// Проставить номера для приборов учета
        /// </summary>
        /// <param name="conn_db">Соединение</param>
        /// <returns>Returns</returns>
        virtual protected Returns SetCntsNumbers(IDbConnection conn_db)
        {
            ExecSQL(conn_db, "drop table tmp_num;", false);

            string sql = String.Concat(
                " create temp table tmp_num (",
                " nzp         integer, ",
                " nzp_counter integer, ",
                " num         integer, ",
                " val_cnt     float, ",
                " cnt         char(200)", ") ");

            Returns ret = ExecSQL(conn_db, sql);
            if (!ret.result) throw new Exception(ret.text);

            sql = String.Concat(
                " insert into tmp_num (nzp, nzp_counter, cnt, val_cnt, num) ",
                " select a.nzp, a.nzp_counter, a.cnt, a.val_cnt, (select count(*) from tmp_cnts b where a.nzp = b.nzp  and a.nzp_serial >= b.nzp_serial) as num  ",
                " from tmp_cnts a ",
                " order by 5 "
                );

            ret = ExecSQL(conn_db, sql);
            if (!ret.result) throw new Exception(ret.text);

            ret = ExecSQL(conn_db, " create index ind_xn1 on tmp_num(nzp)");
            if (!ret.result) throw new Exception(ret.text);

            ret = ExecSQL(conn_db, DBManager.sUpdStat + "  tmp_num", true);
            if (!ret.result) throw new Exception(ret.text);

            sql = " update tmp_cnts a set num = (select b.num from tmp_num b where b.nzp_counter = a.nzp_counter and b.nzp = a.nzp) ";
            ret = ExecSQL(conn_db, sql);
            if (!ret.result) throw new Exception(ret.text);

            return ret;
        }

        /// <summary>
        /// Получить протокол выгрузки
        /// </summary>
        /// <param name="conn_db"></param>
        /// <returns></returns>
        virtual protected Returns GetUnloadProtocol(IDbConnection conn_db)
        {
            _protocolRecordCnt = 0;
            return new Returns(true);
        }


        /// <summary>
        /// Обновить начисления в таблице tmp_reestr
        /// </summary>
        /// <param name="conn_db">Соединение</param>
        /// <param name="pref">Префикс</param>
        /// <param name="year">Год</param>
        /// <param name="month">Месяц</param>
        /// <returns>Returns</returns>
        abstract protected Returns UpdateReestrSumCharge(IDbConnection conn_db, string pref, string year, string month);

        /// <summary>
        /// Получить количество ПУ в выгрузке
        /// </summary>
        /// <param name="conn_db">Соединение</param>
        /// <returns>Returns</returns>
        protected abstract int GetCounterCount();

        /// <summary>
        /// Сохранить в реестр данные по ПУ
        /// </summary>
        /// <param name="conn_db">Соединение</param>
        /// <returns>Returns</returns>
        protected Returns SetReestrCnts(IDbConnection conn_db)
        {
            // получить количество счетчиков
            int counterCount = GetCounterCount();

            ExecSQL(conn_db, " drop table tmp_tab_for_update;", false);

            // создать временную таблицу для вывода ПУ в строку: № 1-го ПУ, показани 1-го ПУ, № 2-го ПУ, показание 2-го ПУ, ... 
            string sql = String.Concat("create temp table tmp_tab_for_update (",
                " nzp_kvar integer ");
            for (int i = 1; i <= counterCount; i++)
            {
                sql += ", cnt" + i + " char(200), val_cnt" + i + " float ";
            }

            sql += ")";

            Returns ret = ExecSQL(conn_db, sql);
            if (!ret.result) throw new Exception(ret.text);

            // вставить данные во временную таблицу
            sql = " insert into tmp_tab_for_update (nzp_kvar";
            for (int i = 1; i <= counterCount; i++)
            {
                sql += ", cnt" + i + ", val_cnt" + i;
            }

            sql += ") select a.nzp ";

            for (int i = 1; i <= counterCount; i++)
            {
                sql += ", max(case when a.num = " + i + "  then a.cnt end) as cnt" + i + ", max(case when a.num = " + i + " then a.val_cnt end) as val_cnt" + i;
            }
            sql += " from tmp_cnts a group by 1 ";

            ret = ExecSQL(conn_db, sql);
            if (!ret.result) throw new Exception(ret.text);

            // сохранить номера ПУ и показания в реестре
            sql = " update tmp_reestr set (";

            for (int i = 1; i <= counterCount; i++)
            {
                sql += ", cnt" + i + ", val_cnt" + i;
            }

            sql += ") = (";

            for (int i = 1; i <= counterCount; i++)
            {
                sql += ", t.cnt" + i + ", t.val_cnt" + i;
            }

            sql += ") from tmp_tab_for_update t where tmp_reestr.nzp_kvar = t.nzp_kvar ";
            ret = ExecSQL(conn_db, sql);
            if (!ret.result) throw new Exception(ret.text);

            return ret;
        }

        /// <summary>
        /// Cформировать строчку для записи в файл
        /// </summary>
        /// <param name="row">DataRow</param>
        /// <returns>string</returns>
        protected abstract string AssemblyOneString(DataRow row);

        /// <summary>
        /// Удалить реестр из базы
        /// </summary>
        /// <param name="conn_db">Соединение</param>
        /// <param name="nzp_reestr">Код реестра</param>
        protected void DeleteReestr(IDbConnection conn_db, int nzp_reestr)
        {
            ExecSQL(conn_db, " delete  from " + Points.Pref + "_data" + DBManager.tableDelimiter + "tula_reestr_unloads where nzp_reestr=" + nzp_reestr, false);
        }

        /// <summary>
        /// Получить список ЛС из выбранных локальных банков
        /// </summary>
        /// <param name="conn_db">Соединение</param>
        /// <param name="BanksList"></param>
        /// <returns></returns>
        public Returns GetLsData(IDbConnection conn_db, List<string> prefs, string statusLS)
        {
            ExecSQL(conn_db, "drop table tmp_spis_ls", false);

            string sql = String.Concat("create temp table tmp_spis_ls (",
                " nzp_kvar integer, ",
                " nzp_area integer, ",
                " fio char(100), ",
                " pkod " + DBManager.sDecimalType + "(13), ",
                " nkvar char(10), ",
                " nkvar_n char(10), ",
                " nzp_dom integer, " +
                " pref  char(10) )");

            Returns ret = ExecSQL(conn_db, sql);
            if (!ret.result) throw new Exception(ret.text);

            //редактирование вх.параметра со статусом лицевого счета для sql-запроса
            var status = statusLS.Split(',');
            if (status.Length > 1)
                statusLS =
                    status.Aggregate(string.Empty, (current, value) => current + ("'" + value + "'" + ","))
                        .TrimEnd(',');
            else if (status.Length == 0) statusLS = "'0'";
            else statusLS = status[0] != string.Empty ? "'" + status[0] + "'" : "'0'";

            for (int i = 0; i < prefs.Count; i++)
            {
                sql = String.Concat(" insert into tmp_spis_ls (nzp_kvar, nzp_area, fio, pkod, nkvar, nkvar_n, pref, nzp_dom) ",
                    " select nzp_kvar, nzp_area, fio, pkod, nkvar, nkvar_n, ", Utils.EStrNull(prefs[i]), ", nzp_dom ",
                    " from ", prefs[i], sDataAliasRest, "kvar k " +
                    " where 1=1 ",
                    " and exists (select 1 from ", prefs[i], "_data" + DBManager.tableDelimiter + "prm_3 p3 where p3.nzp_prm = 51 " +
                            " and p3.val_prm in (", statusLS, ") and p3.dat_s <= " + DBManager.sCurDate,
                            " and p3.dat_po >= ", DBManager.sCurDate, " and p3.nzp = k.nzp_kvar and p3.is_actual <> 100) ");
                ret = ExecSQL(conn_db, sql);
                if (!ret.result) throw new Exception(ret.text);
            }

            // создать индекс
            ret = ExecSQL(conn_db, "create index ind_ls1 on tmp_spis_ls(nzp_kvar)", true);
            if (!ret.result) throw new Exception(ret.text);

            // обновить статистику
            ret = ExecSQL(conn_db, DBManager.sUpdStat + " tmp_spis_ls", true);
            if (!ret.result) throw new Exception(ret.text);

            return ret;
        }

        /// <summary>
        /// Получить список закрытых месяцев с банками данных
        /// </summary>
        /// <param name="prefs">Префиксы</param>
        /// <returns>string</returns>
        protected string GetUnloadingData(List<string> prefs)
        {
            List<DateTime> ListMonths = new List<DateTime>();
            foreach (var pref in prefs)
            {
                //получаем расчетный месяц локального банка
                var local_date = Points.GetCalcMonth(new CalcMonthParams(pref));
                //получаем закрытый месяц локального банка
                DateTime close_month = new DateTime(local_date.year_, local_date.month_, 1).AddMonths(-1);
                ListMonths.Add(close_month);
            }

            // получаем только уникальные закрытые месяцы
            ListMonths = ListMonths.Distinct().ToList();

            string unloading_date = ""; //строчка записываемая в таблицу

            for (int i = 0; i < ListMonths.Count; i++)
            {
                string strMonthName = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(ListMonths[i].Month);
                int year = ListMonths[i].Year;
                unloading_date += strMonthName + " " + year + " для";
                for (int j = 0; j < prefs.Count; j++)
                {
                    //получаем расчетный месяц локального банка
                    var local_date = Points.GetCalcMonth(new CalcMonthParams(prefs[j]));

                    //получаем закрытый месяц локального банка
                    DateTime close_month = new DateTime(local_date.year_, local_date.month_, 1).AddMonths(-1);

                    if (ListMonths[i] == close_month)
                    {
                        unloading_date += " " + Points.GetPoint(prefs[j]).point + (j != prefs.Count - 1 ? "," : "");
                    }
                }
            }

            return CutUnloadingDate(unloading_date);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="unloading_date"></param>
        /// <returns></returns>
        private string CutUnloadingDate(string unloading_date)
        {
            if (unloading_date.Length > 255)
            {
                unloading_date = unloading_date.Substring(0, 254);
            }

            return unloading_date;
        }

        /// <summary>
        /// Получить имя файла
        /// </summary>
        /// <param name="nzp_reestr">Код реестра</param>
        /// <returns>string</returns>
        protected abstract string GetFileName(int nzp_reestr);

        private int InsertReestr(IDbConnection conn_db, string unloading_date, int nzpUser)
        {
            // сохранить список выгружаемых банков
            string sql = " insert into " + Points.Pref + "_data" + DBManager.tableDelimiter + "tula_reestr_unloads (date_unload, unloading_date, user_unloaded, is_actual, nzp_exc) " +
                " values (" + Utils.EStrNull(DateTime.Now.ToShortDateString()) + ", " + Utils.EStrNull(unloading_date) + ", " + nzpUser + ", 1, " + _nzpExc + ");";
            Returns ret = ExecSQL(conn_db, sql);
            if (!ret.result) throw new Exception(ret.text);

            // получить ключ
            int nzp_reestr = Convert.ToInt32(ClassDBUtils.GetSerialKey(conn_db, null));

            return nzp_reestr;
        }

        private void UpdateReestr(IDbConnection conn_db, string fileNameIn, string unloading_date, int nzp_reestr)
        {
            // сохранить имя файла
            string sql = "update " + Points.Pref + "_data" + DBManager.tableDelimiter + "tula_reestr_unloads set " +
                " name_file = " + Utils.EStrNull(fileNameIn) +
                // если был сформирован протокол
                (_protocolRecordCnt > 0 ? ", unloading_date = " + Utils.EStrNull(CutUnloadingDate("Есть предупреждения (См. протокол в моих файлах) " + unloading_date)) : "") +
                " where nzp_reestr = " + nzp_reestr;
            Returns ret = ExecSQL(conn_db, sql);
            if (!ret.result) throw new Exception("Ошибка обновления имени файла в таблице tula_reestr_unloads\n" + ret.text);
        }

        /// <summary>
        /// Запись реестра в файл
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="fileNameIn"></param>
        /// <param name="excelRepDb"></param>
        /// <param name="nzpExc"></param>
        protected virtual void WriteReestrToFile(IDbConnection conn_db, string fileNameIn, ExcelRep excelRepDb, string[] additionLines)
        {
            string dir = Path.Combine(Directory.CreateDirectory(Constants.ExcelDir).FullName, fileNameIn);
            FileStream memstr = new FileStream(dir, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
            StreamWriter writer = new StreamWriter(memstr, Encoding.GetEncoding(1251));

            try
            {
                var dt = ClassDBUtils.OpenSQL("select * from tmp_reestr where pkod > 0 order by pkod", conn_db);
                if (dt.resultCode < 0) throw new Exception(dt.resultMessage);
                if (additionLines != null)
                    foreach (var additionLine in additionLines)
                    {
                        writer.WriteLine(additionLine);
                    }

                int num = dt.resultData.Rows.Count;

                for (int j = 0; j < dt.resultData.Rows.Count; j++)
                {
                    /// Cформировать строчку для записи в файл
                    writer.WriteLine(AssemblyOneString(dt.resultData.Rows[j]));
                    if (j % 100 == 0) excelRepDb.SetMyFileProgress(new ExcelUtility() { nzp_exc = _nzpExc, progress = ((decimal)j) / num });
                }

                writer.Flush();
                writer.Close();
                memstr.Close();

                if (InputOutput.useFtp)
                {
                    fileNameIn = InputOutput.SaveOutputFile(dir);
                }
            }
            catch (Exception ex)
            {
                writer.Flush();
                writer.Close();
                memstr.Close();

                // удалить реестр
                throw new Exception("Ошибка при записи данных в writer,функция GetUploadReestr " + ex.Message);
            }
        }
    }
}
