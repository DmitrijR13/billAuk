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
    /// <summary>
    /// ВЫГРУЗКА в Сбербанк для Марий Эл
    /// </summary>
    class BankDownloadReestrVersion3 : BaseBankUnloadReestr
    {
        /// <summary>
        /// Создать временную таблицу tmp_reestr для данных реестра
        /// </summary>
        /// <param name="conn_db">Соединение</param>
        /// <returns>Returns</returns>
        override protected Returns CreateReestrTempTable(IDbConnection conn_db)
        {
            Returns ret = new Returns();

            ExecSQL(conn_db, "drop table tmp_reestr", false);

            string sql = String.Concat("create temp table tmp_reestr (",
                " nzp_kvar integer, ",
                " act_month char(10), " +
                " pkod " + DBManager.sDecimalType + "(13), ",
                " adr char(100), ",
                " sum_charge " + DBManager.sDecimalType + "(14,2) ");

            for (int i = 1; i <= 10; i++)
            {
                sql += ", cnt" + i + " char(100), " + " val_cnt" + i + " " + DBManager.sDecimalType + " (14,2) ";
            }

            sql += ", pref char(10)) ";

            ret = ExecSQL(conn_db, sql.ToString(), true);
            if (!ret.result) throw new Exception(ret.text);
            
            return ret;
        }

        /// <summary>
        /// Записать адреса во временную таблицу tmp_reestr реестра
        /// </summary>
        /// <param name="conn_db">Соединение</param>
        /// <returns>Returns</returns>
        override protected Returns SetReestrAddress(IDbConnection conn_db, string pref)
        {
            string sql = String.Concat(" insert into tmp_reestr (nzp_kvar, pkod, adr, pref) ",
                " select distinct k.nzp_kvar, k.pkod, ",
                sNvlWord + "(d.ndom, '') || (case when trim(" + sNvlWord + "(d.nkor,'')) = '-' then '' else trim(" + sNvlWord + "(d.nkor,'')) end) ",
                "||' - ' || k.nkvar || (case when trim(" + sNvlWord + "(k.nkvar_n,'')) = '-' then '' else '/' || trim(" + sNvlWord + "(k.nkvar_n,'')) end) as adr, ",
                Utils.EStrNull(pref),
                " from tmp_spis_ls k ",
                " left outer join " + Points.Pref + sDataAliasRest + "dom d on k.nzp_dom = d.nzp_dom ",
                " where k.pref = " + Utils.EStrNull(pref));

            Returns ret = ExecSQL(conn_db, sql.ToString(), true);
            if (!ret.result) throw new Exception("Ошибка записи в таблице tmp_reestr\n" + ret.text);

            RecordMonth local_date;
            DateTime close_month;

            // получить расчетный месяц
            local_date = Points.GetCalcMonth(new CalcMonthParams(pref));
            //сформировать закрытый месяц локального банка
            close_month = new DateTime(local_date.year_, local_date.month_, 1).AddMonths(-1);

            // установить месяц и год актуальности данных
            sql = String.Concat(" update tmp_reestr set act_month = ", Utils.EStrNull(close_month.Month.ToString("00") + close_month.Year.ToString().Substring(2)),
                " where pref = ", Utils.EStrNull(pref));
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result) throw new Exception("Ошибка записи в таблице tmp_reestr\n" + ret.text);

            return ret;
        }

        /// <summary>
        /// Получить имя файла
        /// </summary>
        /// <param name="nzp_reestr">Код реестра</param>
        /// <returns>string</returns>
        override protected string GetFileName(int nzp_reestr)
        {
            return nzp_reestr.ToString("000") + ".csv";
        }

        /// <summary>
        /// Записать данные по ПУ во временную таблицу tmp_cnts
        /// </summary>
        /// <param name="conn_db">Соединение</param>
        /// <param name="pref">Префикс</param>
        /// <param name="dat_uchet">Дата учета</param>
        /// <returns>Returns</returns>
        override protected Returns SetCntsData(IDbConnection conn_db, string pref, DateTime calc_month)
        {
            Returns ret = new Returns();

            string sql = String.Concat(
                " insert into tmp_cnts (service, name_type, cnt, nzp, nzp_serv, nzp_counter, val_cnt) ",
                " select s.service, sc.name_type, trim(" + DBManager.sNvlWord + "(cs.num_cnt,'')) as cnt, cs.nzp, cs.nzp_serv, cs.nzp_counter,",
                " max(c.val_cnt) ",
                " from " +
                    pref + "_kernel" + DBManager.tableDelimiter + "services s, " +
                    pref + "_kernel" + DBManager.tableDelimiter + "s_counttypes sc, " +
                    pref + "_data" + DBManager.tableDelimiter + "counters_spis cs, " +
                    pref + "_data" + DBManager.tableDelimiter + "counters c, " +
                    " tmp_reestr tmp ",
                " where cs.nzp_serv = s.nzp_serv ",
                "   and cs.nzp_cnttype = sc.nzp_cnttype ",
                "   and c.nzp_counter = cs.nzp_counter ",
                "   and tmp.nzp_kvar = cs.nzp ",
                "   and tmp.pref = " + Utils.EStrNull(pref),
                "   and cs.nzp_type in (3,4) ",
                "   and cs.is_actual = 1 ",
                "   and cs.dat_close is null ",
                "   and c.is_actual=1 ",
                "   and c.dat_uchet = (select max(pv.dat_uchet) from " + pref + "_data" + DBManager.tableDelimiter + "counters pv",
                "           where pv.nzp_counter = cs.nzp_counter and pv.dat_uchet <= " + Utils.EStrNull(calc_month.ToShortDateString()) + " and pv.is_actual = 1)",
                " group by 1,2,3,4,5,6 ",
                " order by 1,2,3");

            ret = ExecSQL(conn_db, sql.ToString(), true);
            if (!ret.result) throw new Exception("Ошибка записи счетчиков в таблицу tmp_cnts." + ret.text);
            
            return ret;
        }

        /// <summary>
        /// Получить количество ПУ в выгрузке
        /// </summary>
        /// <returns>int</returns>
        override protected int GetCounterCount()
        {
            return 10;
        }

        /// <summary>
        /// Cформировать название ПУ
        /// </summary>
        /// <param name="counter">Название ПУ</param>
        /// <returns>string</returns>
        private string CounterName(string counter)
        {
            if (counter == "") return "";

            string name = counter;

            if (counter.IndexOf('_') != -1)
            {
                if (counter.IndexOf('_') == counter.LastIndexOf('_'))
                {
                    name = counter.Substring(counter.IndexOf('_') + 1, counter.Length - counter.IndexOf('_') - 1);
                }
                else
                {
                    name = counter.Substring(counter.IndexOf('_') + 1, counter.LastIndexOf('_') - counter.IndexOf('_') - 1);
                    name = name + " " + counter.Substring(counter.LastIndexOf('_') + 1, counter.Length - counter.LastIndexOf('_') - 1);
                }
            }

            return name;
        }

        /// <summary>
        /// Cформировать строчку для записи в файл
        /// </summary>
        /// <param name="row">DataRow</param>
        /// <returns>string</returns>
        override protected string AssemblyOneString(DataRow row)
        {
            string s = (row["pkod"] != DBNull.Value ? ((Decimal)row["pkod"]).ToString("0").Trim().PadLeft(13, '0') : "") + ";" +
                (row["adr"] != DBNull.Value ? ((string)row["adr"]).Trim().Replace(";", "") : "") + ";" +
                (row["act_month"] != DBNull.Value ? ((string)row["act_month"]).Trim().Replace(";", "") : "") + ";" +
                (row["sum_charge"] != DBNull.Value ? ((Decimal)row["sum_charge"]).ToString("0.00") : "");

            for (int i = 1; i <= 10; i++)
            {
                s += ";" + CounterName(row["cnt" + i] != DBNull.Value ? ((string)row["cnt" + i]).Trim() : "");
                s += ";" + (row["val_cnt" + i] != DBNull.Value ? ((Decimal)row["val_cnt" + i]).ToString("").Trim() : "");
            }

            return s;
        }

        /// <summary>
        /// Обновить начисления в таблице tmp_reestr. Берется sum_outsaldo
        /// </summary>
        /// <param name="conn_db">Соединение</param>
        /// <param name="pref">Префикс</param>
        /// <param name="year">Год</param>
        /// <param name="month">Месяц</param>
        /// <returns>Returns</returns>
        override protected Returns UpdateReestrSumCharge(IDbConnection conn_db, string pref, string year, string month)
        {
            string errorMessage = "Ошибка обновления начислений в таблице tmp_reestr.";

            ExecSQL(conn_db, "drop table tmp_charge", false);

            string sql = String.Concat(" create temp table tmp_charge (",
                   " nzp_kvar integer, ",
                   " sum_outsaldo ", DBManager.sDecimalType, "(15,2))");
            Returns ret = ExecSQL(conn_db, sql.ToString(), true);
            if (!ret.result) throw new Exception(errorMessage + ret.text);

            sql = " insert into tmp_charge (nzp_kvar, sum_outsaldo) " +
                " select nzp_kvar, sum(ch.sum_outsaldo) from " + pref + "_charge_" + year + DBManager.tableDelimiter + "charge_" + month + " ch " +
                "   where ch.dat_charge is null " +
                "       and ch.nzp_serv > 1 " +
                " group by 1";
            ret = ExecSQL(conn_db, sql.ToString(), true);
            if (!ret.result) throw new Exception(errorMessage + ret.text);

            sql = "create index ix_tmp_charge_1 on tmp_charge(nzp_kvar)";
            ret = ExecSQL(conn_db, sql.ToString(), true);
            if (!ret.result) throw new Exception(ret.text);

            sql = DBManager.sUpdStat + " tmp_charge";
            ret = ExecSQL(conn_db, sql.ToString(), true);
            if (!ret.result) throw new Exception(errorMessage + ret.text);

            sql = " update tmp_reestr a set " +
                " sum_charge = (select (case when b.sum_outsaldo < 0 then 0 else b.sum_outsaldo end) from tmp_charge b where a.nzp_kvar = b.nzp_kvar) " +
                " where a.pref = " + Utils.EStrNull(pref);
            ret = ExecSQL(conn_db, sql.ToString(), true);
            if (!ret.result) throw new Exception(errorMessage + ret.text);

            return ret;
        }

        protected override void WriteReestrToFile(IDbConnection conn_db, string fileNameIn, ExcelRep excelRepDb, string[] additionLines)
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