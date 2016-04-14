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
    /// ВЫГРУЗКА в Сбербанк для Тулы
    /// </summary>
    public class BankUnloadKaluga : BankDownloadReestrVersion21
    {
        private string _tmpCounterLog = "tmp_bank_reestr_counter_log";
        private int _alreadyLoggedCnt = 0;
        private int _logNzpExc = 0;
        
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

            string sql = " insert into tmp_cnts(nzp, nzp_serv, nzp_counter, cnt, val_cnt) " + 
                " select cs.nzp, cs.nzp_serv, cs.nzp_counter, cs.num_cnt, max(c.val_cnt) " + 
                " from " + 
                    pref + "_data" + DBManager.tableDelimiter + "counters_spis cs, " + 
                    pref + "_data" + DBManager.tableDelimiter + "counters c, " + 
                    " tmp_reestr tmp " +
                " where c.nzp_counter = cs.nzp_counter " + 
                    " and tmp.nzp_kvar = cs.nzp " + 
                    " and tmp.pref = " + Utils.EStrNull(pref) + 
                    " and cs.nzp_type in (3,4) " + 
                    " and cs.is_actual = 1 " + 
                    " and cs.dat_close is null " + 
                    " and c.is_actual=1 " + 
                    " and c.dat_uchet = (select max(pv.dat_uchet) " + 
                        " from " + pref + "_data" + DBManager.tableDelimiter + "counters pv " + 
                        " where pv.nzp_counter = cs.nzp_counter  " +
                            " and pv.dat_uchet <= " + Utils.EStrNull(calc_month.ToShortDateString()) + " and pv.is_actual = 1)" + 
                " group by 1, 2, 3, 4 ";
            ret = ExecSQL(conn_db, sql.ToString(), true);
            if (!ret.result) throw new Exception(ret.text);

            ret = ExecSQL(conn_db, "create index ix_tmp_cnts_nzp_counter on tmp_cnts (nzp_counter)", true);
            if (!ret.result) throw new Exception(ret.text);

            ret = ExecSQL(conn_db, "create index ix_tmp_cnts_nzp_serv on tmp_cnts (nzp_serv)", true);
            if (!ret.result) throw new Exception(ret.text);

            ret = ExecSQL(conn_db, DBManager.sUpdStat + " tmp_cnts", true);
            if (!ret.result) throw new Exception(ret.text);

            // псевдоним ПУ
            sql = "update tmp_cnts a set pseudonym = (select max(p17.val_prm ) From " + pref + "_data" + DBManager.tableDelimiter + "prm_17 p17 " +
                " Where p17.nzp = a.nzp_counter " +
                    " and p17.nzp_prm = 1426 " +
                    " and p17.is_actual = 1 " +
                    " and " + Utils.EStrNull(calc_month.ToShortDateString()) + " between p17.dat_s and p17.dat_po)";
            ret = ExecSQL(conn_db, sql.ToString(), true);
            if (!ret.result) throw new Exception(ret.text);

            sql = "update tmp_cnts a set pseudonym = upper(trim(" + DBManager.sNvlWord + "(pseudonym, '')))";
            ret = ExecSQL(conn_db, sql.ToString(), true);
            if (!ret.result) throw new Exception(ret.text);

            sql = "update tmp_cnts a set cnt = cnt || ' (' || pseudonym || ')' where pseudonym <> '' ";
            ret = ExecSQL(conn_db, sql.ToString(), true);
            if (!ret.result) throw new Exception(ret.text);

            // поправить псевдонимы 
            // Например из "ХВС_1" убрать "С" и "_" 
            sql = " update tmp_cnts a set pseudonym = " + 
                " (case " + 
                    " when nzp_serv = 6 then regexp_replace(pseudonym, '[^ХВ12]+', '', 'g') " +
                    " when nzp_serv = 9 then regexp_replace(pseudonym, '[^ГВ12]+', '', 'g') " +
                    " when nzp_serv = 25 then regexp_replace(pseudonym, '[^ЭЛН12]+', '', 'g') " +
                    " else '' end)";
            ret = ExecSQL(conn_db, sql.ToString(), true);
            if (!ret.result) throw new Exception(ret.text);

            return ret;
        }

        /// <summary>
        /// Получить протокол выгрузки
        /// </summary>
        /// <param name="conn_db"></param>
        /// <returns></returns>
        protected override Returns GetUnloadProtocol(IDbConnection conn_db)
        {
            GetWrongCounters(conn_db);
            WriteWrongCountersToFile(conn_db);
            DeleteWrongCounters(conn_db);

            return new Returns(true);
        }

        /// <summary>
        /// Найти строки, которые не должны попасть в выгрузку из-за ПУ
        /// </summary>
        /// <returns></returns>
        private Returns GetWrongCounters(IDbConnection conn_db)
        {
            ExecSQL(conn_db, "drop table " + _tmpCounterLog, false);

            string sql = "create temp table " + _tmpCounterLog + " (" +
                " warning_code integer, " +
                " nzp_kvar     integer, " +
                " nzp_serv     integer) " + DBManager.sUnlogTempTable;
            Returns ret =  ExecSQL(conn_db, sql, true);
            if (!ret.result) throw new Exception(ret.text);

            // 1. Найти ЛС, у которых есть ПУ по услугам, кроме ХВС, ГВС, Эл/эн
            sql = "insert into " + _tmpCounterLog + " (nzp_kvar, nzp_serv, warning_code) " + 
                " select a.nzp, a.nzp_serv, 1 " + 
                " from tmp_cnts a " + 
                " where a.nzp_serv not in (6,9,25) " +
                " group by 1,2";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result) throw new Exception(ret.text);

            // 2. Получить количество ПУ по ХВС, ГВС, Эл/эн
            ExecSQL(conn_db, "drop table tmp_kaluga_counters_stat", false);

            sql = "create temp table tmp_kaluga_counters_stat (" + 
                " nzp_kvar      integer, " +
                " nzp_serv      integer, "  +  
                " counter_cnt   integer, "  +
                " pseudonym_cnt integer " +
                ") " + DBManager.sUnlogTempTable;
            ret = ExecSQL(conn_db, sql);
            if (!ret.result) throw new Exception(ret.text);

            sql = " insert into tmp_kaluga_counters_stat (nzp_kvar, nzp_serv, counter_cnt) " + 
                " select nzp, nzp_serv, count(*) " +
                " from tmp_cnts " +
                " where nzp_serv in (6,9,25) " +
                " group by 1,2";
            ret = ExecSQL(conn_db, sql);
            if (!ret.result) throw new Exception(ret.text);

            // 3. Получить ЛС, у которых кол-во ПУ по ХВС, ГВС, Эл/эн больше 2 
            sql = "insert into " + _tmpCounterLog + "(warning_code, nzp_kvar, nzp_serv) " +
                " select 2, nzp_kvar, nzp_serv from tmp_kaluga_counters_stat " +
                " where counter_cnt > 2";
            ret = ExecSQL(conn_db, sql);
            if (!ret.result) throw new Exception(ret.text);

            // 4. Получить ЛС, у которых кол-во ПУ не совпадает с кол-вом псевдонимов
            // псевдоним задан неверно или не задан вовсе
            sql = "update tmp_kaluga_counters_stat t set " +
                " pseudonym_cnt = " +
                " (case " + 
                    " when t.nzp_serv = 6  then (select count(*) from tmp_cnts a where a.pseudonym in ('ХВ1', 'ХВ2') and t.nzp_kvar = a.nzp and t.nzp_serv = a.nzp_serv) " + 
                    " when t.nzp_serv = 9  then (select count(*) from tmp_cnts a where a.pseudonym in ('ГВ1', 'ГВ2') and t.nzp_kvar = a.nzp and t.nzp_serv = a.nzp_serv) " +
                    " when t.nzp_serv = 25 then (select count(*) from tmp_cnts a where a.pseudonym in ('ЭЛЭН1', 'ЭЛЭН2') and t.nzp_kvar = a.nzp and t.nzp_serv = a.nzp_serv) " +
                " else 0 end) ";
            ret = ExecSQL(conn_db, sql);
            if (!ret.result) throw new Exception(ret.text);

            sql = "insert into " + _tmpCounterLog + "(warning_code, nzp_kvar, nzp_serv) " +
                " select 3, nzp_kvar, nzp_serv from tmp_kaluga_counters_stat " +
                " where counter_cnt <= 2 " +
                    " and counter_cnt <> pseudonym_cnt";
            ret = ExecSQL(conn_db, sql);
            if (!ret.result) throw new Exception(ret.text);
           
            return ret;
        }

        /// <summary>
        /// Удалить строки из выгрузку
        /// </summary>
        /// <param name="conn_db"></param>
        /// <returns></returns>
        private Returns DeleteWrongCounters(IDbConnection conn_db)
        {
            string sql = "delete from tmp_reestr t where exists (select 1 from " + _tmpCounterLog + " l where t.nzp_kvar = l.nzp_kvar " + DBManager.Limit1 + ")";
            Returns ret = ExecSQL(conn_db, sql);
            if (!ret.result) throw new Exception(ret.text);

            sql = "delete from tmp_cnts t where exists (select 1 from " + _tmpCounterLog + " l where t.nzp = l.nzp_kvar " + DBManager.Limit1 + ")";
            ret = ExecSQL(conn_db, sql);
            if (!ret.result) throw new Exception(ret.text);

            return ret;
        }

        /// <summary>
        /// Сбросить в протокол предупреждения
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="calc_month"></param>
        /// <returns></returns>
        private Returns WriteWrongCountersToFile(IDbConnection conn_db)
        {
            Returns ret = new Returns(true);
            _protocolRecordCnt = Convert.ToInt32(ExecScalar(conn_db, "select count(*) from " + _tmpCounterLog, out ret, true));
            if (!ret.result) throw new Exception(ret.text);

            if (_protocolRecordCnt == 0)
            { 
                return ret; 
            }

            string protocol = "KalugaBankUnloadProtocol_" + DateTime.Now.Ticks;
            protocol = Path.Combine(Constants.Directories.ReportDir, protocol) + ".txt";
            string comment = "Протокол выгрузки реестра в Сбербанк для Калужской области";
            
            using (ExcelRep excelRepDb = new ExcelRep())
            {
                ret = excelRepDb.AddMyFile(new ExcelUtility
                {
                    nzp_user = _nzpUser,
                    status = ExcelUtility.Statuses.InProcess,
                    rep_name = comment.Length > 100 ? comment.Substring(0, 100) : comment,
                    file_name = Path.GetFileName(protocol),
                    exc_path = Path.GetFileName(protocol)
                });

                if (!ret.result) return ret;
                _logNzpExc = ret.tag;

                string dir = Path.Combine(Constants.ExcelDir, protocol);
                FileStream memstr = new FileStream(dir, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
                StreamWriter writer = new StreamWriter(memstr, Encoding.GetEncoding(1251));

                try
                {                   
                    string sql = "select r.adr, r.pkod " +
                        " from tmp_reestr r, " + _tmpCounterLog + " l " + 
                        " where r.nzp_kvar = l.nzp_kvar " + 
                            " and l.warning_code = 1 " + 
                        " order by 1,2";
                    IntfResultTableType rt = ClassDBUtils.OpenSQL(sql, conn_db);
                    if (rt.resultCode < 0) throw new Exception(rt.resultMessage);
                    WriteDataTableToFile("Найдены ЛС, у которых есть ПУ по услугам, кроме ХВС, ГВС, Эл/эн", rt.resultData, writer, excelRepDb);

                    sql = "select r.adr, r.pkod " +
                        " from tmp_reestr r, " + _tmpCounterLog + " l " +
                        " where r.nzp_kvar = l.nzp_kvar " + 
                            " and l.warning_code = 2 " +
                        " order by 1,2";
                    rt = ClassDBUtils.OpenSQL(sql, conn_db);
                    if (rt.resultCode < 0) throw new Exception(rt.resultMessage);
                    WriteDataTableToFile("Найдены ЛС, у которых больше 2-х ПУ по услугам ХВС, ГВС, Эл/эн", rt.resultData, writer, excelRepDb);

                    sql = "select r.adr, r.pkod, c.cnt " +
                        " from tmp_reestr r, tmp_cnts c, " + _tmpCounterLog + " l " +
                        " where r.nzp_kvar = l.nzp_kvar " + 
                            " and r.nzp_kvar = c.nzp " +
                            " and l.warning_code = 3 " +
                        " order by 1,2";
                    rt = ClassDBUtils.OpenSQL(sql, conn_db);
                    if (rt.resultCode < 0) throw new Exception(rt.resultMessage);
                    WriteDataTableToFile("Найдены ЛС, у которых не заданы или заданы неверно псевдонимы ПУ по услугам ХВС, ГВС, Эл/эн", rt.resultData, writer, excelRepDb);

                    writer.Flush();
                    writer.Close();
                    memstr.Close();

                    if (InputOutput.useFtp)
                    {
                        protocol = InputOutput.SaveOutputFile(dir);
                    }
                }
                catch (Exception ex)
                {
                    writer.Flush();
                    writer.Close();
                    memstr.Close();

                    excelRepDb.SetMyFileState(new ExcelUtility() { nzp_exc = _logNzpExc, status = ExcelUtility.Statuses.Failed, progress = 1 });

                    throw new Exception(ex.Message); 
                }
                finally
                {
                    if (ret.result)
                    {
                        excelRepDb.SetMyFileState(new ExcelUtility() { nzp_exc = _logNzpExc, progress = 1, status = ExcelUtility.Statuses.Success, exc_path = Path.GetFileName(protocol) });
                    }
                }

                return ret;
            }
        }

        /// <summary>
        /// Запись содержимого DataTable в файл
        /// </summary>
        /// <param name="notice"></param>
        /// <param name="dataTable"></param>
        /// <param name="writer"></param>
        /// <param name="excelRepDb"></param>
        private void WriteDataTableToFile(string notice, DataTable dataTable, StreamWriter writer, ExcelRep excelRepDb)
        {
            DataRow row;
            string logString = "";
            bool hasCntColumns = dataTable.Columns.Contains("cnt");
            
            if (dataTable.Rows.Count > 0)
            {
                writer.WriteLine(notice);

                for (int j = 0; j < dataTable.Rows.Count; j++)
                {
                    row = dataTable.Rows[j];
                    logString =  "Плат. код: " + (row["pkod"] != DBNull.Value ? (row["pkod"]).ToString() : "") +
                        (hasCntColumns ? ", ПУ: " + ((string)row["cnt"]).Trim() : "") +
                        ", адрес: " + (row["adr"] != DBNull.Value ? ((string)row["adr"]).Trim() : "");
                    writer.WriteLine(logString);

                    _alreadyLoggedCnt++;
                    if (_alreadyLoggedCnt % 100 == 0) excelRepDb.SetMyFileProgress(new ExcelUtility() { nzp_exc = _logNzpExc, progress = (decimal)(_alreadyLoggedCnt) / _protocolRecordCnt });
                }

                writer.WriteLine("");
                writer.WriteLine("##########################################################################################################################");
            }
        }

        /// <summary>
        /// Нумерация ПУ
        /// </summary>
        /// <param name="conn_db"></param>
        /// <returns></returns>
        override protected Returns SetCntsNumbers(IDbConnection conn_db)
        {
            string sql = " update tmp_cnts a set num = " +
                " (case " +
                    " when pseudonym = 'ХВ1'   then 1 " +
                    " when pseudonym = 'ХВ2'   then 2 " +
                    " when pseudonym = 'ГВ1'   then 3 " +
                    " when pseudonym = 'ГВ2'   then 4 " +
                    " when pseudonym = 'ЭЛЭН1' then 5 " +
                    " when pseudonym = 'ЭЛЭН2' then 6 " +
                " else 0 end) ";

            Returns ret = ExecSQL(conn_db, sql);
            if (!ret.result) throw new Exception(ret.text);

            sql = "update tmp_cnts set cnt = num||''";
            ret = ExecSQL(conn_db, sql);
            if (!ret.result) throw new Exception(ret.text);

            return ret;
        }
    }
}