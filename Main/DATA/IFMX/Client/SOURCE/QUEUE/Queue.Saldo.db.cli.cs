using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.DataBase
{
    public class DbSaldoQueueClient : DbQueueClient
    {
        public override Returns PrepareQueue(IDbConnection conn_web, TaskQueue queue)
        {
            return prepareQueue(conn_web, queue);
        }

        public static Returns PrepareQueue(IDbConnection conn_web)
        {
            return prepareQueue(conn_web, null);
        }

        private static Returns prepareQueue(IDbConnection conn_web, TaskQueue queue)
        {
            string tab = "saldo_fon";
            Returns ret = Utils.InitReturns();

#if PG
            DBManager.ExecSQL(conn_web, "set search_path to 'public'", true);
#endif

            if (!DBManager.TempTableInWebCashe(conn_web, tab))
            {
                ret = DBManager.ExecSQL(conn_web,
                    " Create table  " + tab +
                    " ( nzp_key   serial  not null, " +
                    "   nzp_area  integer default 0 not null, " +
                    "   year_     integer default 0 not null, " +
                    "   month_    integer default 0 not null, " +
                    "   kod_info  integer default 0 not null, " +
                    "   dat_in    " + DBManager.sDateTimeType + ", " +
                    "   dat_work  " + DBManager.sDateTimeType + ",  " +
                    "   dat_out   " + DBManager.sDateTimeType + ", " +
                    "   txt       char(255) ," +
                    "   progress  " + sDecimalType + "(6,4) default 0 " +
                    " ) " + sLockMode, true);

                if (!ret.result)
                {
                    return ret;
                }

                if (ret.result) { ret = DBManager.ExecSQL(conn_web, " Create unique index ix_" + tab + "_1 on saldo_fon (nzp_key) ", true); }
                if (ret.result) { ret = DBManager.ExecSQL(conn_web, " Create        index ix_" + tab + "_2 on saldo_fon (nzp_area,year_,month_,kod_info) ", true); }
                if (ret.result) { ret = DBManager.ExecSQL(conn_web, " Create        index ix_" + tab + "_3 on saldo_fon (kod_info) ", true); }
                if (ret.result) { ret = DBManager.ExecSQL(conn_web, DBManager.sUpdStat + " " + tab, true); }
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
        private string makeWhereForProcess(SaldoFonTask finder, string alias, ref Returns ret)
        {
            string where = makeWhereForProcess(finder as FonTaskWithYearMonth, alias, ref ret);

            if (alias != "") alias = alias + ".";

            if (finder.nzp_area > 0) where += " and " + alias + "nzp_area = " + finder.nzp_area;

            return where;
        }

        /// <summary> Получить список фоновых процессов расчета сальдо
        /// </summary>
        public List<SaldoFonTask> GetProcessSaldo(SaldoFonTask finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            string where = makeWhereForProcess(finder, "p", ref ret);
            if (!ret.result) return null;

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;

            int total_record_count = 0;
            object count = ExecScalar(conn_web, " Select count(*) From saldo_fon p Where 1=1 " + where, out ret, true);
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

            string sql =
                " SELECT p.nzp_key, p.nzp_area, p.year_, p.month_, p.kod_info, p.dat_in, p.dat_work, p.dat_out, p.txt, a.area " +
                " FROM saldo_fon p" + 
                " LEFT OUTER JOIN  s_area a ON a.nzp_area = p.nzp_area " +
                " WHERE 1=1 " + where +
                " ORDER BY dat_in desc, a.area, year_, month_" + skip;
#else
            if (finder.skip > 0) skip = " skip " + finder.skip.ToString();

            string sql = " Select " + skip +
                         " p.nzp_key, p.nzp_area, p.year_, p.month_, p.kod_info, p.dat_in, p.dat_work, p.dat_out, p.txt, a.area " +
                         " From saldo_fon p, outer s_area a" +
                         " Where a.nzp_area = p.nzp_area" + where +
                         " Order by dat_in desc, a.area, year_, month_";
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
                List<SaldoFonTask> Spis = new List<SaldoFonTask>();

                int i = 0;
                while (reader.Read())
                {
                    SaldoFonTask zap = new SaldoFonTask();

                    zap.num = ++i + finder.skip;

                    if (reader["nzp_key"] != DBNull.Value) zap.nzp_key = Convert.ToInt32(reader["nzp_key"]);
                    if (reader["nzp_area"] != DBNull.Value) zap.nzp_area = Convert.ToInt32(reader["nzp_area"]);
                    if (reader["area"] != DBNull.Value) zap.area = Convert.ToString(reader["area"]).Trim();
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

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка заполнения списка процессов " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }
        }

        public Returns DeleteProcessSaldo(SaldoFonTask proc)
        {
            Returns ret = Utils.InitReturns();
            string where = makeWhereForProcess(proc, "", ref ret);
            if (!ret.result) return ret;

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return ret;

            string sql = "delete from saldo_fon Where 1=1 " + where;
            ret = ExecSQL(conn_web, sql, true);

            conn_web.Close();

            return ret;
        }

        public Returns SaveProcessSaldo(SaldoFonTask proc)
        {
            Returns ret = Utils.InitReturns();

            if (proc.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return ret;
            }

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return ret;

            string sql;

            if (proc.nzp_key > 0)
            {


                sql = "update saldo_fon set txt = " + Utils.EStrNull(proc.txt.Trim(), "") +
                      " Where nzp_key = " + proc.nzp_key;
                ret = ExecSQL(conn_web, sql, true);
            }
            else
            {
                IDbTransaction transaction;
                try
                {
                    transaction = conn_web.BeginTransaction();
                }
                catch
                {
                    transaction = null;
                }

                int numMonths;
                if (proc.year_ == proc.year_po)
                    numMonths = proc.month_po - proc.month_ + 1;
                else if (proc.year_po > proc.year_)
                    numMonths = proc.year_po * 12 + proc.month_po - proc.year_ * 12 - proc.month_ + 1;
                else
                    numMonths = 0;

                int y = proc.year_, m = proc.month_;
                for (int i = 0; i < numMonths; i++)
                {
                    sql = "insert into saldo_fon (nzp_area, year_, month_, kod_info, dat_in, txt)";
                    if (proc.nzp_area < 1)
                    {
                        sql += " select nzp_area, " + y + ", " + m + ", " +
                               FonTask.getKodInfo(Constants.act_process_in_queue) +
                               ", current, " + Utils.EStrNull(proc.txt.Trim(), "") + " from s_area a " +
                               " where (select count(*) from saldo_fon b where a.nzp_area = b.nzp_area " +
                               " and b.year_ = " + y + " and b.month_ = " + m + " and b.kod_info = " +
                               FonTask.getKodInfo(Constants.act_process_in_queue) + ") = 0";
                        ret = ExecSQL(conn_web, transaction, sql, true);
                    }
                    else
                    {
                        object num = ExecScalar(conn_web, transaction,
                            "select count(*) from saldo_fon b where b.nzp_area = " + proc.nzp_area +
                            " and b.year_ = " + y + " and b.month_ = " + m + " and b.kod_info = " +
                            FonTask.getKodInfo(Constants.act_process_in_queue) + "", out ret, true);
                        if (!ret.result)
                        {
                            if (transaction != null) transaction.Rollback();
                            conn_web.Close();
                            return ret;
                        }
                        if (Convert.ToInt32(num) == 0)
                        {
                            sql += " values (" + proc.nzp_area + ", " + y + ", " + m + ", " +
                                   FonTask.getKodInfo(Constants.act_process_in_queue) +
                                   ", current, " + Utils.EStrNull(proc.txt.Trim(), "") + ")";
                            ret = ExecSQL(conn_web, transaction, sql, true);
                        }
                    }

                    if (!ret.result)
                    {
                        if (transaction != null) transaction.Rollback();
                        conn_web.Close();
                        return ret;
                    }

                    if (m < 12) m++;
                    else
                    {
                        m = 1;
                        y++;
                    }
                }

                if (transaction != null) transaction.Commit();
            }
            conn_web.Close();

            return ret;
        }
    }
}
