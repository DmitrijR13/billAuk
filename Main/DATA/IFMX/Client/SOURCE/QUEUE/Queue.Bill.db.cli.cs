using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.DataBase
{
    public class DbBillQueueClient : DbQueueClient
    {
        public override Returns PrepareQueue(IDbConnection conn_web, TaskQueue queue)
        {
            return prepareQueue(conn_web, queue);
        }

        private static Returns prepareQueue(IDbConnection conn_web, TaskQueue queue)
        {
            string tab = "bill_fon";
            string sql;
            Returns ret = Utils.InitReturns();

#if PG
            DBManager.ExecSQL(conn_web, "set search_path to 'public'", true);
#endif

            if (!DBManager.TempTableInWebCashe(conn_web, tab))
            {
                sql = "Create table " + tab + " (" +
                    " nzp_key            serial  not null," +
                    " nzp_area           integer default 0 not null, " +
                    " nzp_geu            integer default 0 not null, " +
                    " nzp_wp             integer default 0 not null, " +
                    " year_              integer default 0 not null, " +
                    " month_             integer default 0 not null, " +
                    " kod_info           integer default 0 not null, " +
                    " dat_in             " + DBManager.sDateTimeType + ", " +
                    " dat_work           " + DBManager.sDateTimeType + ",  " +
                    " dat_out            " + DBManager.sDateTimeType + ", " +
                    " txt                char(255)," +
                    " nzp_user           integer default 0 not null," +
                    " count_list_in_pack integer default 0 not null," +
                    " kod_sum_faktura    integer default 0 not null, " +
                    " result_file_type   char(10)," +
                    " id_faktura         integer default 0 not null," +
                    " with_dolg          smallint," +
                    " with_geu          smallint," +
                    " with_uk          smallint," +
                    " with_uchastok          smallint," +
                    " file_name          char(200)," +
                    " progress           " + DBManager.sDecimalType + "(6,4) default 0," +
                    " ip_adr             char(15) " +
                    ")" + DBManager.sLockMode;
                ret = DBManager.ExecSQL(conn_web, sql, true);
                if (!ret.result) return ret;

                DBManager.ExecSQL(conn_web, "Create unique index ix_" + tab + "_1 on " + tab + " (nzp_key)", true);
                DBManager.ExecSQL(conn_web, "Create        index ix_" + tab + "_2 on " + tab + " (nzp_key,year_,month_,kod_info)", true);
                DBManager.ExecSQL(conn_web, "Create        index ix_" + tab + "_3 on " + tab + " (nzp_user,kod_info)", true);
            }

            ret = ReQueueOldTasks(conn_web, tab);

            return ret;
        }

        public override Returns AddTask(IDbConnection conn_web, IDbTransaction transaction, FonTask fonTask)
        {
            return new Returns(false, "Не реализовано");
        }

        public override Returns CloseTask(IDbConnection conn_web, IDbTransaction transaction, FonTask fonTask)
        {
            return new Returns(false, "Не реализовано");
        }

        private string makeWhereForProcess(BillFonTask finder, string alias, ref Returns ret)
        {
            string where = makeWhereForProcess(finder as FonTaskWithYearMonth, alias, ref ret);

            if (alias != "") alias = alias + ".";

            if (finder.nzp_key > 0) where += " and " + alias + "nzp_key = " + finder.nzp_key;
            if (finder.nzp_area > 0) where += " and " + alias + "nzp_area = " + finder.nzp_area;
            if (finder.nzp_geu > 0) where += " and " + alias + "nzp_geu = " + finder.nzp_geu;
            if (finder.nzp_wp > 0) where += " and " + alias + "nzp_wp = " + finder.nzp_wp;

            return where;
        }

        /// <summary>
        /// Получить список заданий на формирование платежных документов
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public List<BillFonTask> GetProcessBill(BillFonTask finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            string where = makeWhereForProcess(finder, "p", ref ret);
            if (!ret.result) return null;

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;

            string table = "bill_fon";

            int total_record_count = 0;
            object count = ExecScalar(conn_web, " Select count(*) From " + table + " p Where 1=1 " + where, out ret,
                true);
            if (ret.result)
            {
                try
                {
                    total_record_count = Convert.ToInt32(count);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = ex.Message;
                    return null;
                }
            }

            string skip = "";
#if PG
            if (finder.skip > 0) skip = " offset " + finder.skip.ToString();
            string sql = " Select p.nzp_key, p.nzp_area, a.area, p.nzp_geu, b.geu, p.nzp_wp, c.point, p.year_, p.month_, p.kod_info, p.dat_in, p.dat_work, p.dat_out, p.txt, p.file_name, p.progress " +
                " From " + table + " p left outer join s_area a on (a.nzp_area = p.nzp_area) left outer join s_geu b on (p.nzp_geu = b.nzp_geu) left outer join s_point c on (p.nzp_wp = c.nzp_wp) " +
                " Where 1=1 " + where +
                " Order by dat_in desc, year_, month_, c.point, a.area, b.geu" + skip;
#else
            if (finder.skip > 0) skip = " skip " + finder.skip.ToString();
            string sql = " Select " + skip +
                         " p.nzp_key, p.nzp_area, a.area, p.nzp_geu, b.geu, p.nzp_wp, c.point, p.year_, p.month_, p.kod_info, p.dat_in, p.dat_work, p.dat_out, p.txt, p.file_name, p.progress " +
                         " From " + table + " p, outer s_area a, outer s_geu b, outer s_point c" +
                         " Where a.nzp_area = p.nzp_area and p.nzp_geu = b.nzp_geu and p.nzp_wp = c.nzp_wp " + where +
                         " Order by dat_in desc, year_, month_, c.point, a.area, b.geu";
#endif

            IDataReader reader;

            ret = ExecRead(conn_web, out reader, sql, true);
            if (!ret.result)
            {
                conn_web.Close();
                return null;
            }
            try
            {
                List<BillFonTask> Spis = new List<BillFonTask>();

                int i = 0;
                while (reader.Read())
                {
                    BillFonTask zap = new BillFonTask();

                    zap.num = ++i + finder.skip;

                    if (reader["nzp_key"] != DBNull.Value) zap.nzp_key = Convert.ToInt32(reader["nzp_key"]);
                    if (reader["nzp_area"] != DBNull.Value) zap.nzp_area = Convert.ToInt32(reader["nzp_area"]);
                    if (reader["area"] != DBNull.Value) zap.area = Convert.ToString(reader["area"]).Trim();
                    if (reader["nzp_geu"] != DBNull.Value) zap.nzp_geu = Convert.ToInt32(reader["nzp_geu"]);
                    if (reader["geu"] != DBNull.Value) zap.geu = Convert.ToString(reader["geu"]).Trim();

                    if (reader["nzp_wp"] != DBNull.Value) zap.nzp_wp = Convert.ToInt32(reader["nzp_wp"]);
                    if (reader["point"] != DBNull.Value) zap.point = Convert.ToString(reader["point"]).Trim();

                    if (reader["year_"] != DBNull.Value) zap.year_ = Convert.ToInt32(reader["year_"]);
                    if (reader["month_"] != DBNull.Value) zap.month_ = Convert.ToInt32(reader["month_"]);
                    if (reader["kod_info"] != DBNull.Value) zap.KodInfo = Convert.ToInt32(reader["kod_info"]);
                    if (reader["dat_in"] != DBNull.Value)
                        zap.dat_in = String.Format("{0:dd.MM.yyyy HH:mm}", reader["dat_in"]);
                    if (reader["dat_work"] != DBNull.Value)
                        zap.dat_work = String.Format("{0:dd.MM.yyyy HH:mm}", reader["dat_work"]);
                    if (reader["dat_out"] != DBNull.Value)
                        zap.dat_out = String.Format("{0:dd.MM.yyyy HH:mm}", reader["dat_out"]);
                    if (reader["txt"] != DBNull.Value) zap.txt = Convert.ToString(reader["txt"]).Trim();
                    if (reader["file_name"] != DBNull.Value)
                        zap.file_name = Convert.ToString(reader["file_name"]).Trim();
                    if (reader["progress"] != DBNull.Value) zap.progress = Convert.ToDecimal(reader["progress"]);

                    Spis.Add(zap);

                    if (i >= finder.rows) break;
                }

                reader.Close();
                conn_web.Close();
                ret.tag = total_record_count;
                return Spis;
            }
            catch (Exception ex)
            {
                reader.Close();
                conn_web.Close();

                ret.result = false;
                ret.text = ex.Message;

                MonitorLog.WriteLog(
                    "Ошибка заполнения списка заданий на формирование платежных документов\n" + ex.Message,
                    MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }
        }

        /// <summary>
        /// Удалить задания на формирование платежных документов
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        public Returns DeleteProcessBill(BillFonTask finder)
        {
            Returns ret = Utils.InitReturns();
            string where = makeWhereForProcess(finder, "", ref ret);
            if (!ret.result) return ret;

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return ret;

            string table = "bill_fon";
            if (!TableInWebCashe(conn_web, table))
            {
                ret.result = false;
                ret.text = "Таблицы со списком заданий на формирование платежных документов не существует";
                MonitorLog.WriteLog(ret.text, MonitorLog.typelog.Error, true);
                return ret;
            }

            string sql = "delete from " + table + " Where 1=1 " + where;
            ret = ExecSQL(conn_web, sql, true);

            conn_web.Close();
            return ret;
        }

        /// <summary>
        /// Добавить или изменить задания на формирование платежных документов
        /// </summary>
        /// <param name="tasks"></param>
        /// <returns></returns>
        public Returns SaveProcessBill(List<BillFonTask> tasks)
        {
            string ipAdr = String.Empty;

            #if DEBUG
            try
            {
                System.Net.IPAddress[] ips = System.Net.Dns.GetHostAddresses(System.Net.Dns.GetHostName());
                foreach (System.Net.IPAddress ip in ips)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        ipAdr = ip.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("ProcessBillFon\nОшибка при определении ip адреса\n" + ex, MonitorLog.typelog.Error, true);
                ipAdr = String.Empty;
            }
            #endif

            if (tasks == null) return new Returns(false, "Неверно заданы входные параметры");
            if (tasks.Count == 0) return new Returns(false, "Неверно заданы входные параметры");
            if (tasks[0].nzp_user < 1) return new Returns(false, "Не определен пользователь");

            Returns ret;

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return ret;

            string table = "bill_fon";

            if (!TableInWebCashe(conn_web, table))
            {
                conn_web.Close();
                return new Returns(false, "Не найдена таблица для сохранения задания");
            }

            string sql;

            IDbTransaction transaction;
            try
            {
                transaction = conn_web.BeginTransaction();
            }
            catch
            {
                transaction = null;
            }

            foreach (BillFonTask bill in tasks)
            {
                if (bill.nzp_key > 0)
                {
                    sql = "Update " + table + " set txt = " + Utils.EStrNull(bill.txt.Trim(), "") +
                          " Where nzp_key = " + bill.nzp_key;
                }
                else
                {
                    sql = "insert into " + table +
                          " (nzp_key, nzp_area, nzp_geu, nzp_wp, year_, month_, kod_info, dat_in, txt, " +
                          "nzp_user, count_list_in_pack, kod_sum_faktura, result_file_type," +
                          " id_faktura, with_dolg, with_uk, with_geu, with_uchastok, close_ls, zero_nach" +
                          (ipAdr.Trim() != String.Empty ? ", ip_adr" : String.Empty) + ")" +
#if PG
                        " values (default, " + bill.nzp_area +
#else
 " values (0, " + bill.nzp_area +
#endif
                        ", " + bill.nzp_geu +
                          ", " + bill.nzp_wp +
                          ", " + bill.year_ +
                          ", " + bill.month_ +
                          ", " + (int) FonTask.Statuses.InQueue +
                          ", " + DBManager.sCurDateTime +
                          ", " + Utils.EStrNull(bill.txt.Trim(), "") +
                          ", " + bill.nzp_user +
                          ", " + bill.count_list_in_pack +
                          ", " + bill.kod_sum_faktura +
                          ", " + Utils.EStrNull(bill.result_file_type.Trim()) +
                          ", " + bill.id_faktura +
                          ", " + (bill.with_dolg ? "1" : "0") +
                          ", " + (bill.with_uk ? "1" : "0") +
                          ", " + (bill.with_geu ? "1" : "0") +
                          ", " + (bill.with_uchastok ? "1" : "0") +
                          ", " + (bill.with_close_ls ? "1" : "0") +
                          ", " + (bill.with_zero ? "1" : "0") +
                          (ipAdr != String.Empty ? ", '" + ipAdr.Trim() + "'" : String.Empty) + ")";
                }
                ret = ExecSQL(conn_web, transaction, sql, true);
                if (!ret.result)
                {
                    if (transaction != null) transaction.Rollback();
                    conn_web.Close();
                    return ret;
                }
            }

            if (transaction != null) transaction.Commit();
            conn_web.Close();

            ret.tag = tasks.Count;

            return ret;
        }

        /// <summary>
        /// Обновляет процент выполнения задания по формированию платежных документов
        /// </summary>
        /// <param name="finder">Необходимо заполнить поля nzp_key, progress (от 0 до 1)</param>
        /// <returns></returns>
        public Returns SetTaskProgress(int taskId, decimal progress)
        {
            return SetTaskProgress(taskId, progress, "bill_fon");
        }
    }
}
