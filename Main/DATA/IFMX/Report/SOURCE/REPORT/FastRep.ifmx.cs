
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using STCLINE.KP50.Global;
using FastReport;
using System.IO;
using System.Data.OleDb;
using SevenZip;

using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.DataBase
{
    //Класс для получения данных из генератора отчетов
    public partial class ExcelRep : ExcelRepClient
    {

        /// <summary>
        /// 50.1 Сальдовая ведомость для Энергосбыта
        /// </summary>
        /// <param name="prm"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public DataTable GetSaldoVedEnergo(Prm prm, out Returns ret)
        {
            ret = Utils.InitReturns();

            #region Подключение к БД
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);//new IDbConnection(Constants.cons_Webdata);
            IDataReader reader = null;
            IDataReader reader2;

            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("FastReport : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                return null;
            }

            #endregion

            StringBuilder sql = new StringBuilder();
            List<int> nzpServs = new List<int>() { prm.nzp_serv };//список идентификаторов услуг
            DataTable DT = new DataTable();
            DT.TableName = "Q_master";

            #region создание временной таблицы
            ExecSQL(conn_db, " drop table t_ensaldo ", false);
            sql.Remove(0, sql.Length);
            sql.Append(" create temp table t_ensaldo ( ");
            sql.Append(" month_ integer default 0,");
            sql.Append(" year_ integer default 0,");
            sql.Append(" sum_insaldo Decimal(14,2) default 0, ");
            sql.Append(" sum_insaldo_n Decimal(14,2) default 0, "); //Входящее сальдо по населению
            sql.Append(" sum_insaldo_a Decimal(14,2) default 0, ");// Входящее сальдо по арендаторам
            sql.Append(" sum_insaldo_d Decimal(14,2) default 0, ");//Входящее сальдо по тем у кого текущие начисления равны 0
            sql.Append(" sum_in36 Decimal(14,2) default 0, ");//Входящее сальдо по тому населению у которого долги больше 3 лет
            sql.Append(" sum_nach_a Decimal(14,2) default 0.00, ");//начисление по арендаторам
            sql.Append(" sum_nach_n Decimal(14,2) default 0.00, ");//начисление по населению
            sql.Append(" sum_nach Decimal(14,2) default 0.00, ");
            sql.Append(" sum_money_a Decimal(14,2) default 0, ");//Оплата по арендаторам
            sql.Append(" sum_money_n Decimal(14,2) default 0,   ");//Оплата по населению
            sql.Append(" sum_money Decimal(14,2) default 0,   ");//Всего оплата
            sql.Append(" sum_outsaldo_n Decimal(14,2) default 0, ");//Исходящее сальдо по населению
            sql.Append(" sum_outsaldo_a Decimal(14,2) default 0, ");//Исходящее сальдо по арендаторам
            sql.Append(" sum_outsaldo_d Decimal(14,2) default 0, ");//Исходящее сальдо по тем у кого начисления равны 0
            sql.Append(" sum_outsaldo Decimal(14,2) default 0, ");
            sql.Append(" sum_out36 Decimal(14,2) default 0) with no log ");
            if (ExecSQL(conn_db, sql.ToString(), true).result != true)
            {
                conn_db.Close();
                ret.result = false;
                return null;
            }
            #endregion

            try
            {
                #region Выборка по локальным банкам

                #region Ограничения
                string where_wp = "";
                string where_supp = "";
                string where_serv = "";
                string where_adr = "";

                if (prm.RolesVal != null)
                {
                    if (prm.RolesVal.Count > 0)
                    {
                        foreach (_RolesVal role in prm.RolesVal)
                        {
                            if (role.tip == Constants.role_sql)
                            {
                                if (role.kod == Constants.role_sql_serv)
                                {
                                    where_serv += " and nzp_serv in (" + role.val + ") ";
                                    nzpServs.Add(Convert.ToInt32(role.val));
                                }
                                if (role.kod == Constants.role_sql_supp)
                                    where_supp += " and nzp_supp in (" + role.val + ") ";
                                if (role.kod == Constants.role_sql_wp)
                                    where_wp += " and nzp_wp in (" + role.val + ") ";
                                if (role.kod == Constants.role_sql_area)
                                    where_adr += " and nzp_area in (" + role.val + ") ";
                                if (role.kod == Constants.role_sql_geu)
                                    where_adr += " and nzp_geu in (" + role.val + ") ";
                            }
                        }
                    }
                }
                if (prm.nzp_geu > 0) where_adr += " and nzp_geu = " + prm.nzp_geu.ToString();
                if (prm.nzp_serv > 0) where_adr += " and nzp_serv = " + prm.nzp_serv.ToString();
                if (prm.nzp_area > 0) where_adr += " and nzp_area = " + prm.nzp_area.ToString();
                if (prm.nzp_dom > 0) where_adr += " and nzp_dom = " + prm.nzp_dom.ToString();
                if (prm.nzp_ul > 0) where_adr += " and nzp_dom in (select nzp_dom from " +
                    Points.Pref + "_data" + "@" +
                    DBManager.getServer(conn_db) + ": dom where nzp_ul = " + prm.nzp_ul.ToString() + ")";
                #endregion

                sql.Remove(0, sql.Length);
                sql.Append(" select * ");
                sql.Append(" from  " + Points.Pref + "_kernel" + "@" + DBManager.getServer(conn_db) + ": s_point ");
                sql.Append(" where nzp_wp > 1 " + where_wp);


                if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    conn_db.Close();
                    sql.Remove(0, sql.Length);
                    ret.result = false;
                    return null;
                }
                List<string> dbs = new List<string>();
                while (reader.Read())
                {
                    dbs.Add(reader["bd_kernel"].ToString().ToLower().Trim());
                }
                reader.Close();

                decimal counter = 0.0M;

                int startMonth = System.Convert.ToDateTime(prm.dat_s).Month;
                int startYear = System.Convert.ToDateTime(prm.dat_s).Year;
                int endMonth = System.Convert.ToDateTime(prm.dat_po).Month;
                int endYear = System.Convert.ToDateTime(prm.dat_po).Year;

                decimal amount = ((endYear * 12 + endMonth + 1) - (startYear * 12 + startMonth)) * dbs.Count;

                for (int d = 0; d < dbs.Count; d++)
                {
                    for (int i = startYear * 12 + startMonth; i < endYear * 12 + endMonth + 1; i++)
                    {
                        int curYear = i / 12;
                        int curMonth = i - curYear * 12;
                        if (curMonth == 0)
                        {
                            curMonth = 12;
                            curYear--;
                        }

                        string pref = dbs[d];
                        string chargeXX = pref + "_charge_" + (curYear - 2000).ToString("00") +
                            ":charge_" + curMonth.ToString("00");

                        //Проверка на существование базы
                        sql.Remove(0, sql.Length);
                        sql.Append(" select * ");
                        sql.Append(" from sysmaster: sysdatabases ");
                        sql.Append(" where lower(name) = '" + pref + "_charge_" + (curYear - 2000).ToString("00") + "'");
                        if (ExecRead(conn_db, out reader2, sql.ToString(), true).result)
                        {
                            #region создание временной таблицы с индексами
                            ExecSQL(conn_db, " drop table t_temp_ensaldo ", false);
                            sql.Remove(0, sql.Length);
                            sql.Append(" CREATE TEMP TABLE t_temp_ensaldo (");
                            sql.Append(" month_ integer default 0,");
                            sql.Append(" year_ integer default 0,");
                            sql.Append(" typek INTEGER default 1 NOT NULL, ");
                            sql.Append(" sum_insaldo Decimal(14,2) DEFAULT 0, ");
                            sql.Append(" sum_real Decimal(14,2) DEFAULT 0, ");
                            sql.Append(" reval Decimal(14,2) DEFAULT 0.00, ");
                            sql.Append(" real_charge Decimal(14,2) DEFAULT 0.00, ");
                            sql.Append(" sum_money Decimal(14,2) DEFAULT 0, ");
                            sql.Append(" sum_outsaldo Decimal(14,2) default 0) ");
                            sql.Append(" WITH NO log; ");
                            if (!ExecSQL(conn_db, sql.ToString(), true).result)
                            {
                                conn_db.Close();
                                ret.result = false;
                                return null;
                            }
                            #endregion

                            sql.Remove(0, sql.Length);
                            sql.Append(" INSERT INTO t_temp_ensaldo (month_, year_, typek, sum_insaldo, sum_real, reval, real_charge, sum_money, sum_outsaldo) ");
                            sql.Append(" SELECT ");
                            sql.Append(curMonth + ", ");
                            sql.Append(curYear + ", ");
                            sql.Append(" typek, ");
                            sql.Append(" sum_insaldo, ");
                            sql.Append(" sum_real, ");
                            sql.Append(" reval, ");
                            sql.Append(" real_charge, ");
                            sql.Append(" sum_money, ");
                            sql.Append(" sum_outsaldo ");
                            sql.Append(" FROM  " + chargeXX + " a, " + pref + "_data: kvar k");
                            sql.Append(" WHERE a.nzp_kvar = k.nzp_kvar AND dat_charge IS NULL ");
                            sql.Append(where_adr + where_supp + where_serv);
                            if (!ExecSQL(conn_db, sql.ToString(), true).result)
                                return null;

                            ExecSQL(conn_db, "create index ixtmp_ensaldo_01 on t_temp_ensaldo(month_)", true);
                            ExecSQL(conn_db, "update statistics for table t_temp_ensaldo", true);

                            sql.Remove(0, sql.Length);
                            sql.Append(" INSERT INTO t_ensaldo (month_, year_, sum_insaldo_n, sum_in36, sum_insaldo_a, sum_insaldo_d, sum_insaldo, sum_nach_n, sum_nach_a, sum_nach, sum_money_n, sum_money_a, sum_money, sum_outsaldo_n, sum_out36, sum_outsaldo_a, sum_outsaldo_d, sum_outsaldo) ");
                            sql.Append(" SELECT " + curMonth + ", ");
                            sql.Append(" " + curYear + ", ");
                            sql.Append(" sum(CASE WHEN typek=1 THEN sum_insaldo ELSE 0 END) AS sum_insaldo_n, ");
                            sql.Append(" sum(CASE WHEN sum_real>0 ");
                            sql.Append(" AND (sum_insaldo/sum_real) > 36 THEN sum_insaldo ELSE 0 END) AS sum_in36, ");
                            sql.Append(" sum(CASE WHEN typek>1 THEN sum_insaldo ELSE 0 END) AS sum_insaldo_a, ");
                            sql.Append(" sum(CASE WHEN typek=1 ");
                            sql.Append(" AND sum_real = 0 THEN sum_insaldo ELSE 0 END) AS sum_insaldo_d, ");
                            sql.Append(" sum(sum_insaldo) AS sum_insaldo, ");
                            sql.Append(" sum(CASE WHEN typek=1 THEN sum_real+reval+real_charge ELSE 0 END) AS sum_nach_n, ");
                            sql.Append(" sum(CASE WHEN typek>1 THEN sum_real+reval+real_charge ELSE 0 END) AS sum_nach_a, ");
                            sql.Append(" sum(CASE WHEN typek>=1 THEN sum_real+reval+real_charge ELSE 0 END) AS sum_nach, ");
                            sql.Append(" sum(CASE WHEN typek=1 THEN nvl(sum_money,0) ELSE 0 END) AS sum_money_n, ");
                            sql.Append(" sum(CASE WHEN typek>1 THEN nvl(sum_money,0) ELSE 0 END) AS sum_money_a, ");
                            sql.Append(" sum(CASE WHEN typek>=1 THEN nvl(sum_money,0) ELSE 0 END) AS sum_money, ");
                            sql.Append(" sum(CASE WHEN typek=1 THEN sum_outsaldo ELSE 0 END) AS sum_outsaldo_n, ");
                            sql.Append(" sum(CASE WHEN typek=1 ");
                            sql.Append(" AND sum_real>0 ");
                            sql.Append(" AND ((sum_outsaldo-sum_real-reval-real_charge)/sum_real) > 36 THEN sum_outsaldo-sum_real-reval-real_charge ELSE 0 END) AS sum_out36, ");
                            sql.Append(" sum(CASE WHEN typek>1 THEN sum_outsaldo ELSE 0 END) AS sum_outsaldo_a, ");
                            sql.Append(" sum(CASE WHEN typek=1 ");
                            sql.Append(" AND sum_real=0 THEN sum_outsaldo ELSE 0 END) AS sum_outsaldo_d, ");
                            sql.Append(" sum(sum_outsaldo) AS sum_outsaldo ");
                            sql.Append(" FROM t_temp_ensaldo ");
                            sql.Append(" GROUP BY 1 ");
                            if (!ExecSQL(conn_db, sql.ToString(), true).result)
                                return null;
                        }
                        reader2.Close();

                        ExcelRepClient dbRep2 = new ExcelRepClient();
                        Utils.setCulture();
                        dbRep2.SetMyFileProgress(new ExcelUtility() { nzp_exc = prm.nzp_key, progress = (++counter / amount) });
                        dbRep2.Close();
                    }
                }


                #endregion

                #region Выборка на экран
                sql.Remove(0, sql.Length);
                sql.Append(" select month_, year_, sum(sum_insaldo_n) as sum_insaldo_n, ");
                sql.Append(" sum(sum_in36) as sum_in36, ");
                sql.Append(" sum(sum_insaldo_a) as sum_insaldo_a, ");
                sql.Append(" sum(sum_insaldo_d) as sum_insaldo_d,");
                sql.Append(" sum(sum_insaldo) as sum_insaldo, ");
                sql.Append(" sum(sum_nach_n) as sum_nach_n, ");
                sql.Append(" sum(sum_nach_a) as sum_nach_a, ");
                sql.Append(" sum(sum_nach) as sum_nach, ");
                sql.Append(" sum(sum_money_n) as sum_money_n, ");
                sql.Append(" sum(sum_money_a) as sum_money_a, ");
                sql.Append(" sum(sum_money) as sum_money,");
                sql.Append(" sum(sum_outsaldo_n) as sum_outsaldo_n, ");
                sql.Append(" sum(sum_out36) as sum_out36, ");
                sql.Append(" sum(sum_outsaldo_a) as sum_outsaldo_a, ");
                sql.Append(" sum(sum_outsaldo_d) as sum_outsaldo_d, ");
                sql.Append(" sum(sum_outsaldo) as sum_outsaldo    ");
                sql.Append(" from t_ensaldo  ");
                sql.Append(" group by 1,2 ");
                sql.Append(" order by 2,1 ");
                if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    conn_db.Close();
                    ret.result = false;
                    return null;
                }
                #endregion

                Utils.setCulture();

                if (reader != null)
                {
                    //заполнение DataTable
                    DT.Load(reader, LoadOption.OverwriteChanges);
                    reader.Close();

                    DbSprav db = new DbSprav();
                    Finder finder = new Finder() { nzp_user = prm.nzp_user, pref = prm.pref, RolesVal = prm.RolesVal };
                    List<_Service> services = db.ServiceLoad(finder, out ret);
                    string servText = "Услуга: ";
                    if (nzpServs.Count > 1)
                        servText = "Услуги: ";
                    prm.service = servText + String.Join(",", services.Where(x => nzpServs.Any(y => y == x.nzp_serv)).Select(z => z.service).ToArray());
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка формирования отчета Сальдо для Энергосбыта " +
                    ex.Message, MonitorLog.typelog.Error, true);
            }
            finally
            {
                ExecSQL(conn_db, " drop table t_ensaldo ", true);
                if (reader != null) reader.Close();
                conn_db.Close();
            }
            conn_db.Close();
            return DT;

        }

        /// <summary>
        /// Список временно зарегистрированных для паспортистки
        /// </summary>
        public DataTable GetChoosenData(Kart finder, out Returns ret)
        {
            ret = Utils.InitReturns();

            DataTable DT = new DataTable();
            DT.TableName = "Q_master";

            //todo: PG
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;

            string tab_gil;
            if (finder.nzp_gil.Trim() != "") tab_gil = "_gilhist";
            else if (finder.nzp_kvar == Constants._ZERO_) tab_gil = "_gil";
            else tab_gil = "_gilkvar";

            string tXX_cnt = "t" + Convert.ToString(finder.nzp_user).Trim() + tab_gil;
            if (!TableInWebCashe(conn_web, tXX_cnt))
            {
                ret.result = true;
                ret.text = "Данные не были выбраны! Выполните поиск.";
                ret.tag = -22;
                conn_web.Close();
                return null;
            }
            string table = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXX_cnt;
            conn_web.Close();

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            string sql = " drop table t_gil ";
            ClassDBUtils.ExecSQL(sql, conn_db, ClassDBUtils.ExecMode.Log);

            sql = " select fam, ima, otch, dat_rog, dok, serij, nomer, grgd, adr, pref, nzp_gil, nzp_kart, nzp_kvar from " + table +
                  " Into temp t_gil With no log ";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }

            sql = "select unique pref from t_gil";
            IDataReader reader;
            if (!ExecRead(conn_db, out reader, sql, true).result)
            {
                conn_db.Close();
                ret.result = false;
                return null;
            }

            sql = " drop table t_spisdata ";
            ClassDBUtils.ExecSQL(sql, conn_db, ClassDBUtils.ExecMode.Log);
            sql = " CREATE TEMP TABLE t_spisdata (" +
            "fam CHAR(40), ima CHAR(40), otch CHAR(40), fio CHAR(250), dat_rog DATE, dok CHAR(30), serij NCHAR(10), nomer NCHAR(7), " +
            "adr CHAR(80), grgd CHAR(60), jobname NCHAR(40), jobpost NCHAR(40),  dat_ofor DATE, dat_oprp DATE, nzp_kvar INTEGER, pref CHAR(20)) ";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }
            try
            {
                while (reader.Read())
                {
                    string pref = "";
                    if (reader["pref"] != DBNull.Value) pref = Convert.ToString(reader["pref"]).Trim();
                    if (pref == "")
                    {
                        reader.Close();
                        conn_db.Close();
                        ret.result = false;
                        ret.text = "Нет префикса в списке лс";
                        return null;
                    }

                    sql = " insert into t_spisdata (fam, ima, otch, dat_rog, dok, serij, nomer, adr, grgd, jobname, jobpost, dat_ofor, dat_oprp, nzp_kvar, pref)" +
                          " select k.fam, k.ima, k.otch, k.dat_rog, sd.dok, k.serij, k.nomer, t.adr, t.grgd, k.jobname, k.jobpost, k.dat_ofor, k.dat_oprp, t.nzp_kvar, '" + pref + "'" +
                          " from t_gil t, " + pref + "_data@" + DBManager.getServer(conn_db) + ":kart k, " +
                          " OUTER " + pref + "_data@" + DBManager.getServer(conn_db) + ":s_dok sd" +
                          " where t.nzp_kart = k.nzp_kart and k.nzp_dok = sd.nzp_dok and t.pref='" + pref + "'";
                    ret = ExecSQL(conn_db, sql, true);
                    if (!ret.result)
                    {
                        conn_db.Close();
                        return null;
                    }

                    if (finder.mode == 1)
                    {
                        sql = "update t_spisdata set fio = (" +
                                 " select max(trim(fam)||' '||trim(ima)||' '||otch)  from " + pref + "_data@" + DBManager.getServer(conn_db) + ":kart k, " + pref + "_data@" + DBManager.getServer(conn_db) + ":s_rod r" +
                                 " where k.nzp_rod=r.nzp_rod " +
                                 " and (upper(rod) matches 'НАНИМ*'  OR upper(rod) matches 'СОБСТ*' OR upper(rod) matches 'ВЛАД*' OR upper(rod) matches 'КВАРТИРО*' )" +
                                 " and k.nzp_kvar=t_spisdata.nzp_kvar " +
                                 " and isactual='1' and nzp_tkrt=1)" +
                               " where trim(nvl(fio,'')) = '' and pref = " + Utils.EStrNull(pref);
                        ret = ExecSQL(conn_db, sql, true);

                        sql = "update t_spisdata set fio = (select  max(trim(fam)||' '||trim(ima)||' '||otch) from " + pref + "_data@" + DBManager.getServer(conn_db) + ":sobstw k" +
                              " where k.nzp_kvar=t_spisdata.nzp_kvar " +
                              ") where trim(nvl(fio,'')) = ''  and pref = " + Utils.EStrNull(pref);
                        ret = ExecSQL(conn_db, sql, true);

                        sql = "update t_spisdata set fio = (select initcap(fio) as fio from " + pref + "_data@" + DBManager.getServer(conn_db) +
                            ":kvar k where k.nzp_kvar=t_spisdata.nzp_kvar) where trim(nvl(fio,'')) = '' and pref = " + Utils.EStrNull(pref);
                        ret = ExecSQL(conn_db, sql, true);
                    }
                }

                reader.Close();

                sql = "select  trim(fam) as fam, trim(ima) as ima, trim(otch) as otch,fio, dat_rog, dok, serij, nomer, adr, " +
                    "grgd, jobname, jobpost, nzp_kvar, pref, dat_ofor, " +
                    " dat_oprp from t_spisdata  order by fam, ima, otch";
                ret = ExecRead(conn_db, out reader, sql, true);
                if (!ret.result)
                {
                    conn_db.Close();
                    ret.result = false;
                    return null;
                }


                if (reader != null)
                {
                    //заполнение DataTable
                    DT.Load(reader, LoadOption.OverwriteChanges);
                    reader.Close();

                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка формирования отчета Список временно зарегистрированных " +
                    ex.Message, MonitorLog.typelog.Error, true);
            }
            finally
            {
                //   ExecSQL(conn_db, " drop table t_spisdata ", true);
                if (reader != null) reader.Close();
                conn_db.Close();
            }
            conn_db.Close();
            return DT;

        }

        /// <summary>
        /// 50.2 Ведомость должников 
        /// </summary>
        /// <param name="prm"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public DataTable GetDolgSpisEnergo(Prm prm, out Returns ret)
        {
            ret = Utils.InitReturns();

            #region Подключение к БД
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);//new IDbConnection(Constants.cons_Webdata);
            IDataReader reader = null;
            IDataReader reader2;

            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("FastReport : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                return null;
            }

            #endregion

            StringBuilder sql = new StringBuilder();
            DataTable DT = new DataTable();
            DT.TableName = "Q_master";
            List<int> nzpServs = new List<int>() { prm.nzp_serv };

            #region создание временной таблицы
            ExecSQL(conn_db, " drop table t_ensaldo ", false);
            sql.Remove(0, sql.Length);
            sql.Append(" create temp table t_ensaldo (     ");
            sql.Append(" num_ls integer default 0,");
            sql.Append(" sum_insaldo Decimal(14,2) default 0.00,");//Входящее сальдо по населению
            sql.Append(" sum_real Decimal(14,2) default 0.00 "); //Начислено за месяц
            sql.Append(" ) with no log ");
            if (ExecSQL(conn_db, sql.ToString(), true).result != true)
            {
                conn_db.Close();
                ret.result = false;
                return null;
            }
            #endregion

            try
            {
                #region Выборка по локальным банкам

                #region Ограничения
                string where_wp = "";
                string where_supp = "";
                string where_serv = "";
                string where_adr = "";

                if (prm.RolesVal != null)
                {
                    if (prm.RolesVal.Count > 0)
                    {
                        foreach (_RolesVal role in prm.RolesVal)
                        {
                            if (role.tip == Constants.role_sql)
                            {
                                if (role.kod == Constants.role_sql_serv)
                                {
                                    where_serv += " and nzp_serv in (" + role.val + ") ";
                                    nzpServs.Add(Convert.ToInt32(role.val));
                                }
                                if (role.kod == Constants.role_sql_supp)
                                    where_supp += " and nzp_supp in (" + role.val + ") ";
                                if (role.kod == Constants.role_sql_wp)
                                    where_wp += " and nzp_wp in (" + role.val + ") ";
                                if (role.kod == Constants.role_sql_area)
                                    where_adr += " and nzp_area in (" + role.val + ") ";
                                if (role.kod == Constants.role_sql_geu)
                                    where_adr += " and nzp_geu in (" + role.val + ") ";
                            }
                        }
                    }
                }
                if (prm.nzp_geu > 0) where_adr += " and nzp_geu=" + prm.nzp_geu.ToString();
                if (prm.nzp_area > 0) where_adr += " and nzp_area=" + prm.nzp_area.ToString();
                if (prm.nzp_dom > 0) where_adr += " and nzp_dom=" + prm.nzp_dom.ToString();
                if (prm.nzp_serv > 0) where_adr += " and nzp_serv=" + prm.nzp_serv.ToString();
                if (prm.nzp_ul > 0) where_adr += " and nzp_dom in (select nzp_dom from " +
                    Points.Pref + "_data" + "@" +
                    DBManager.getServer(conn_db) + ":dom where nzp_ul=" + prm.nzp_ul.ToString() + ")";
                #endregion

                sql.Remove(0, sql.Length);
                sql.Append(" select * ");
                sql.Append(" from  " + Points.Pref + "_kernel" + "@" + DBManager.getServer(conn_db) + ":s_point ");
                sql.Append(" where nzp_wp>1 " + where_wp);


                if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    conn_db.Close();
                    sql.Remove(0, sql.Length);
                    ret.result = false;
                    return null;
                }
                while (reader.Read())
                {
                    string pref = reader["bd_kernel"].ToString().ToLower().Trim();
                    string chargeXX = pref + "_charge_" + (prm.year_ - 2000).ToString("00") +
                        ":charge_" + prm.month_.ToString("00");

                    //Проверка на существование базы
                    sql.Remove(0, sql.Length);
                    sql.Append(" select * ");
                    sql.Append(" from sysmaster:sysdatabases ");
                    sql.Append(" where lower(name) = '" + pref + "_charge_" + (prm.year_ - 2000).ToString("00") + "'");
                    if (ExecRead(conn_db, out reader2, sql.ToString(), true).result)
                    {
                        sql.Remove(0, sql.Length);
                        sql.Append(" insert into t_ensaldo (num_ls, sum_insaldo,  sum_real )");
                        sql.Append(" select a.num_ls, sum(sum_insaldo-sum_money+reval+real_charge),");
                        sql.Append(" sum(sum_real) ");
                        sql.Append(" from " + chargeXX + " a, " + pref + "_data:kvar k");
                        sql.Append(" where a.nzp_kvar=k.nzp_kvar and dat_charge is null ");
                        sql.Append(where_adr + where_supp + where_serv);
                        sql.Append(" group by 1            ");
                        if (!ExecSQL(conn_db, sql.ToString(), true).result)
                            return null;
                    }
                    reader2.Close();
                }
                reader.Close();

                #endregion

                #region Выборка на экран
                sql.Remove(0, sql.Length);
                sql.Append(" select  ");
                sql.Append(" trim(ulica)|| ");
                sql.Append(" (case when ndom is null then '' else  ");
                sql.Append(" ' д.'||trim(ndom)||(case when nkor='-' then '' else ' корп.'||trim(nkor) end) end)|| ");
                sql.Append(" (case when nkvar is null then '' else  ");
                sql.Append(" ' кв.'||trim(nkvar)||(case when trim(nkvar_n)='-' then '' else ' комн.'||trim(nkvar_n) end)  ");
                sql.Append(" end ) as adr, ");
                sql.Append(" fio, t.num_ls,");
                sql.Append(" (case when sum_real > 0 then sum_insaldo/sum_real else 0 end) as koef, ");
                sql.Append(" sum_insaldo, ");
                sql.Append(" (case when sum_insaldo<sum_real*12 then sum_insaldo else sum_real*12 end) as sum_in6, ");
                sql.Append(" (case when sum_insaldo<sum_real*24 then sum_insaldo else sum_real*24 end) as sum_in12, ");
                sql.Append(" (case when sum_insaldo<sum_real*36 then sum_insaldo else sum_real*36 end) as sum_in24, ");
                sql.Append(" (case when sum_insaldo>=sum_real*36 then sum_insaldo-sum_real*36 else 0 end) as sum_in36, ");
                sql.Append(" sum_real ");
                sql.Append(" from t_ensaldo t, " +
                    Points.Pref + "_data:kvar k, " +
                    Points.Pref + "_data: dom d, " +
                    Points.Pref + "_data: s_ulica s ");
                sql.Append(" where t.num_ls = k.num_ls and k.nzp_dom = d.nzp_dom and d.nzp_ul = s.nzp_ul ");
                //sql.Append(" and sum_insaldo > sum_real*6 ");
                sql.Append(" order by 1,2,3,4,5,6,7,8                ");
                if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    conn_db.Close();
                    ret.result = false;
                    return null;
                }
                #endregion

                Utils.setCulture();

                if (reader != null)
                {
                    //заполнение DataTable
                    DT.Load(reader, LoadOption.OverwriteChanges);
                    reader.Close();

                    DbSprav db = new DbSprav();
                    Finder finder = new Finder() { nzp_user = prm.nzp_user, pref = prm.pref, RolesVal = prm.RolesVal };
                    List<_Service> services = db.ServiceLoad(finder, out ret);
                    string servText = "Услуга: ";
                    if (nzpServs.Count > 1)
                        servText = "Услуги: ";
                    prm.service = servText + String.Join(",", services.Where(x => nzpServs.Any(y => y == x.nzp_serv)).Select(z => z.service).ToArray());
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка формирования отчета 50.2 Ведомость должников " +
                    ex.Message, MonitorLog.typelog.Error, true);
            }
            finally
            {
                ExecSQL(conn_db, " drop table t_ensaldo ", true);

                if (reader != null) reader.Close();
                conn_db.Close();

            }
            conn_db.Close();
            return DT;
        }

        /// <summary>
        /// Отчет: Протокол сверки данных
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public DataSet GetProtocolSverData(Finder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            DataSet fDataSet = new DataSet();

            #region Подключение к БД
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);//new IDbConnection(Constants.cons_Webdata);
            IDataReader reader = null;
            IDataReader reader2 = null;

            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("FastReport : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                return null;
            }

            #endregion

            StringBuilder sql = new StringBuilder();
            DataTable DT1 = new DataTable();
            DT1.TableName = "Q_master1";

            DataTable DT2 = new DataTable();
            DT2.TableName = "Q_master2";

            try
            {
                #region первая выборка
                sql.Remove(0, sql.Length);
                sql.Append("SELECT ");
                sql.Append("serv_ls.adr, ");
                sql.Append("serv_ls.pkod, ");
                sql.Append("serv_ls.privat as serv_ls_privat, ");
                sql.Append("sobit_ls.privat as sobit_ls_privat, ");
                sql.Append("serv_ls.shtrih as serv_ls_shtrih, ");
                sql.Append("sobit_ls.shtrih as sobit_ls_shtrih, ");
                sql.Append("serv_ls.geu as serv_ls_geu, ");
                sql.Append("sobit_ls.geu as sobit_ls_geu, ");
                sql.Append("serv_ls.count_gil as serv_ls_count_gil, ");
                sql.Append("sobit_ls.count_gil as sobit_ls_count_gil, ");
                sql.Append("serv_ls.count_propis as serv_ls_count_propis, ");
                sql.Append("sobit_ls.count_propis as sobit_ls_count_propis, ");
                sql.Append("serv_ls.pl as serv_ls_pl, ");
                sql.Append("sobit_ls.pl as sobit_ls_pl, ");
                sql.Append("serv_ls.pl_dom as serv_ls_pl_dom, ");
                sql.Append("sobit_ls.pl_dom as sobit_ls_pl_dom, ");
                sql.Append("serv_ls.pl_mop as serv_ls_pl_mop, ");
                sql.Append("sobit_ls.pl_mop as sobit_ls_pl_mop, ");
                sql.Append("serv_ls.count_gil_all as serv_ls_count_gil_all, ");
                sql.Append("sobit_ls.count_gil_all as sobit_ls_count_gil_all, ");
                sql.Append("serv_ls.sum_charge as serv_ls_sum_charge, ");
                sql.Append("sobit_ls.sum_charge as sobit_ls_sum_charge ");
                sql.Append("FROM ");
                sql.Append(Points.Pref + "_data:a_serverls1 serv_ls, ");
                sql.Append(Points.Pref + "_data:a_sobitsls sobit_ls ");
                sql.Append("WHERE serv_ls.pkod = sobit_ls.pkod ");
                sql.Append("AND serv_ls.month_ = sobit_ls.month_ ");
                sql.Append("AND serv_ls.year_ = sobit_ls.year_ ");
                sql.Append("ORDER BY serv_ls.pkod");
                if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    conn_db.Close();
                    ret.result = false;
                    return null;
                }

                Utils.setCulture();

                if (reader != null)
                {
                    //заполнение DataTable
                    DT1.Load(reader, LoadOption.OverwriteChanges);
                    reader.Close();
                    fDataSet.Tables.Add(DT1);
                }
                #endregion

                #region вторая выборка
                sql.Remove(0, sql.Length);
                sql.Append("SELECT ");
                sql.Append("serv_ls_serv.pkod, ");
                sql.Append(" 'ул.'||trim(ulica)|| ");
                sql.Append(" (case when ndom is null then '' else  ");
                sql.Append(" ' д.'||trim(ndom)||(case when nkor='-' then '' else ' корп.'||trim(nkor) end) end)|| ");
                sql.Append(" (case when nkvar is null then '' else  ");
                sql.Append(" ' кв.'||trim(nkvar)||(case when trim(nkvar_n)='-' then '' else ' комн.'||trim(nkvar_n) end)  ");
                sql.Append(" end ) as adr, ");
                sql.Append("serv_ls_serv.rashod as serv_ls_serv_rashod, ");
                sql.Append("sobit_ls_serv.rashod as sobit_ls_serv_rashod, ");
                sql.Append("serv_ls_serv. type_rashod as serv_ls_serv_type_rashod, ");
                sql.Append("sobit_ls_serv.type_rashod as sobit_ls_serv_type_rashod, ");
                sql.Append("serv_ls_serv.rashod_odn as serv_ls_serv_rashod_odn, ");
                sql.Append("sobit_ls_serv.rashod_odn as sobit_ls_serv_rashod_odn, ");
                sql.Append("serv_ls_serv.type_rashod_odn as serv_ls_serv_type_rashod_odn, ");
                sql.Append("sobit_ls_serv.type_rashod_odn as sobit_ls_serv_type_rashod_odn, ");
                sql.Append("serv_ls_serv.tarif as serv_ls_serv_tarif, ");
                sql.Append("sobit_ls_serv.tarif as sobit_ls_serv_tarif, ");
                sql.Append("serv_ls_serv.sum_tarif as serv_ls_serv_sum_tarif, ");
                sql.Append("sobit_ls_serv.sum_tarif as sobit_ls_serv_sum_tarif, ");
                sql.Append("serv_ls_serv.sum_charge as serv_ls_serv_sum_charge, ");
                sql.Append("sobit_ls_serv.sum_charge as sobit_ls_serv_sum_charge, ");
                sql.Append("serv_ls_serv.sum_tarif_all as serv_ls_serv_sum_tarif_all, ");
                sql.Append("sobit_ls_serv.sum_tarif_all as sobit_ls_serv_sum_tarif_all, ");
                sql.Append("serv_ls_serv.sum_tarif_odn as serv_ls_serv_sum_tarif_odn, ");
                sql.Append("sobit_ls_serv.sum_tarif_odn as sobit_ls_serv_sum_tarif_odn, ");
                sql.Append("serv_ls_serv.sum_charge_odn as serv_ls_serv_sum_charge_odn, ");
                sql.Append("sobit_ls_serv.sum_charge_odn as sobit_ls_serv_sum_charge_odn, ");
                sql.Append("serv_ls_serv.sum_charge_all as serv_ls_serv_sum_charge_all, ");
                sql.Append("sobit_ls_serv.sum_charge_all as sobit_ls_serv_sum_charge_all, ");
                sql.Append("serv_ls_serv.kol_odn as serv_ls_serv_kol_odn, ");
                sql.Append("sobit_ls_serv.kol_odn as sobit_ls_serv_kol_odn, ");
                sql.Append("serv_ls_serv.kol_ind as serv_ls_serv_kol_ind, ");
                sql.Append("sobit_ls_serv.kol_ind as sobit_ls_serv_kol_ind, ");
                sql.Append("serv_ls_serv.norma as serv_ls_serv_norma, ");
                sql.Append("sobit_ls_serv.norma as sobit_ls_serv_norma, ");
                sql.Append("serv_ls_serv.norma_odn as serv_ls_serv_norma_odn, ");
                sql.Append("sobit_ls_serv.norma_odn as sobit_ls_serv_norma_odn ");
                sql.Append("FROM ");
                sql.Append(Points.Pref + "_data:a_serverlsserv1 serv_ls_serv, ");
                sql.Append(Points.Pref + "_data:a_sobitslsserv sobit_ls_serv, ");
                sql.Append(Points.Pref + "_data:kvar k, ");
                sql.Append(Points.Pref + "_data:dom d, ");
                sql.Append(Points.Pref + "_data:s_ulica s ");
                sql.Append("WHERE serv_ls_serv.pkod = sobit_ls_serv.pkod ");
                sql.Append("AND serv_ls_serv.nzp_serv = sobit_ls_serv.nzp_serv ");
                sql.Append("AND serv_ls_serv.month_ = sobit_ls_serv.month_ ");
                sql.Append("AND serv_ls_serv.year_ = sobit_ls_serv.year_ ");
                sql.Append("AND serv_ls_serv.pkod = k.pkod and k.nzp_dom = d.nzp_dom and d.nzp_ul = s.nzp_ul ");
                sql.Append("ORDER BY serv_ls_serv.pkod;");

                if (!ExecRead(conn_db, out reader2, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    conn_db.Close();
                    ret.result = false;
                    return null;
                }

                Utils.setCulture();

                if (reader2 != null)
                {
                    //заполнение DataTable
                    DT2.Load(reader2, LoadOption.OverwriteChanges);
                    reader2.Close();
                    fDataSet.Tables.Add(DT2);
                }
                #endregion
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка формирования отчета  " +
                    ex.Message, MonitorLog.typelog.Error, true);
            }
            finally
            {
                if (reader != null) reader.Close();
                if (reader2 != null) reader2.Close();
                conn_db.Close();
            }
            conn_db.Close();
            return fDataSet;
        }

        /// <summary>
        /// Отчет: Протокол сверки данных по лицевым счетам и домам
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public DataTable GetProtocolSverDataLsDom(Prm finder, out Returns ret)
        {
            ReportSverType type;
            finder.year_ = Convert.ToInt32(finder.year_.ToString().Substring(2, 2));
            string year = "20" + finder.year_.ToString();
            if (finder.typek == 1)
                type = ReportSverType.Ls;
            else
            {
                if (finder.typek == 2)
                    type = ReportSverType.Dom;
                else
                    type = ReportSverType.Service;
            }

            ret = Utils.InitReturns();

            #region Подключение к БД
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            IDataReader reader = null;
            string sqlStr = "";

            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("FastReport : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                return null;
            }

            #endregion

            StringBuilder sql = new StringBuilder();
            DataTable DT = new DataTable();

            //для теста
            List<string> prefixsOur = new List<string>();
            List<string> prefixsTheir = new List<string>() { "vlx34", "vlx35", "vlx36" };

            try
            {
                switch (type)
                {
                    case (ReportSverType.Ls):
                        {
                            #region получаем уникальные префиксы БД из нашей базы

                            sql.Remove(0, sql.Length);
#if PG
                            sql.Append("select distinct(pref) from " + Points.Pref + "_data.kvar");
#else
                            sql.Append("select unique(pref) from " + Points.Pref + "_data: kvar");
#endif

                            if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
                            {
                                MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                                ret.result = false;
                                return null;
                            }
                            if (reader != null)
                            {
                                while (reader.Read())
                                {
                                    string str = "";
                                    if (reader["pref"] != DBNull.Value) str = Convert.ToString(reader["pref"]);
                                    prefixsOur.Add(str.Trim());
                                }
                            }

                            #endregion

                            #region создание временной таблицы содержащей все лиц. счеты с адресом и платежным кодом

                            ExecSQL(conn_db, " Drop table temp_kvar_sver_" + finder.nzp_user, false);
#if PG
                            sqlStr = " CREATE unlogged TABLE temp_kvar_sver_" + finder.nzp_user + "(" +
                                "   _type INTEGER," +
                                "   pkod NUMERIC(13) default 0 NOT NULL," +
                                "   nzp_kvar SERIAL NOT NULL," +
                                "   adr CHAR(100)," +
                                "   val_prm_1_1 CHAR(20)," +
                                "   val_prm_1_2 CHAR(20)," +
                                "   val_prm_2_1 CHAR(20)," +
                                "   val_prm_2_2 CHAR(20)," +
                                "   val_prm_3_1 CHAR(20)," +
                                "   val_prm_3_2 CHAR(20)," +
                                "   val_prm_4_1 NUMERIC(10,2) DEFAULT 0," +
                                "   val_prm_4_2 NUMERIC(10,2) DEFAULT 0," +
                                "   val_prm_5_1 NUMERIC(10,2) DEFAULT 0," +
                                "   val_prm_5_2 NUMERIC(10,2) DEFAULT 0," +
                                "   val_prm_6_1 CHAR(20)," +
                                "   val_prm_6_2 CHAR(20)," +
                                "   val_prm_7_1 CHAR(20)," +
                                "   val_prm_7_2 CHAR(20)," +
                                "   val_prm_8_1 CHAR(20)," +
                                "   val_prm_8_2 CHAR(20)) ;";
#else
                            sqlStr = " CREATE temp TABLE temp_kvar_sver_" + finder.nzp_user + "(" +
                                "   _type INTEGER," +
                                "   pkod DECIMAL(13) default 0 NOT NULL," +
                                "   nzp_kvar SERIAL NOT NULL," +
                                "   adr CHAR(100)," +
                                "   val_prm_1_1 CHAR(20)," +
                                "   val_prm_1_2 CHAR(20)," +
                                "   val_prm_2_1 CHAR(20)," +
                                "   val_prm_2_2 CHAR(20)," +
                                "   val_prm_3_1 CHAR(20)," +
                                "   val_prm_3_2 CHAR(20)," +
                                "   val_prm_4_1 DECIMAL(10,2) DEFAULT 0," +
                                "   val_prm_4_2 DECIMAL(10,2) DEFAULT 0," +
                                "   val_prm_5_1 DECIMAL(10,2) DEFAULT 0," +
                                "   val_prm_5_2 DECIMAL(10,2) DEFAULT 0," +
                                "   val_prm_6_1 CHAR(20)," +
                                "   val_prm_6_2 CHAR(20)," +
                                "   val_prm_7_1 CHAR(20)," +
                                "   val_prm_7_2 CHAR(20)," +
                                "   val_prm_8_1 CHAR(20)," +
                                "   val_prm_8_2 CHAR(20)) with no log;";
#endif
                            ret = ExecSQL(conn_db, sqlStr, true);
                            if (!ret.result)
                            {
                                return null;
                            }
                            sqlStr = " INSERT into temp_kvar_sver_" + finder.nzp_user + "(_type, pkod, nzp_kvar, adr) " +
                                " SELECT 1, kv.pkod, kv.nzp_kvar, " +
                                " 'ул.'||trim(ulica)|| " +
                                " (case when ndom is null then '' else " +
                                " ' д.'||trim(ndom)||(case when nkor='-' then '' else ' корп.'||trim(nkor) end) end)|| " +
                                " (case when kv.nkvar is null then '' else " +
                                " ' кв.'||trim(kv.nkvar)||(case when trim(kv.nkvar_n)='-' then '' else ' комн.'||trim(kv.nkvar_n) end) " +
                                " end ) as adr " +
                                " FROM " +
#if PG
 Points.Pref + "_data. kvar kv, " +
                                Points.Pref + "_data. dom d, " +
                                Points.Pref + "_data. s_ulica s " +
#else
                                Points.Pref + "_data: kvar kv, " +
                                Points.Pref + "_data: dom d, " +
                                Points.Pref + "_data: s_ulica s " +
#endif
 " WHERE " +
                                " kv.nzp_dom = d.nzp_dom and d.nzp_ul = s.nzp_ul" +
                                (finder.nzp_area > 0 ? " and kv.nzp_area = " + finder.nzp_area : "");

                            ret = ExecSQL(conn_db, sqlStr, true);
                            if (!ret.result)
                            {
                                return null;
                            }

                            for (int i = 0; i < prefixsTheir.Count; i++)
                            {
                                sqlStr = " INSERT INTO temp_kvar_sver_" + finder.nzp_user + " (_type, pkod, nzp_kvar, adr) " +
                                    " SELECT 2, kv.pkod, kv.nzp_kvar, " +
                                    " 'ул.'||trim(ulica)|| " +
                                    " (case when ndom is null then '' else " +
                                    " ' д.'||trim(ndom)||(case when nkor='-' then '' else ' корп.'||trim(nkor) end) end)|| " +
                                    " (case when kv.nkvar is null then '' else " +
                                    " ' кв.'||trim(kv.nkvar)||(case when trim(kv.nkvar_n)='-' then '' else ' комн.'||trim(kv.nkvar_n) end) " +
                                    " end ) as adr " +
                                    " FROM " +
#if PG
 prefixsTheir[i] + "_data. kvar kv, " +
                                    prefixsTheir[i] + "_data. dom d, " +
                                    prefixsTheir[i] + "_data. s_ulica s " +
#else
                                    prefixsTheir[i] + "_data: kvar kv, " +
                                    prefixsTheir[i] + "_data: dom d, " +
                                    prefixsTheir[i] + "_data: s_ulica s " +
#endif
 " WHERE " +
                                    " kv.nzp_dom = d.nzp_dom AND d.nzp_ul = s.nzp_ul " +
                                    " AND kv.nzp_kvar NOT IN (SELECT nzp_kvar FROM temp_kvar_sver_" + finder.nzp_user + ")" +
                                    (finder.nzp_area > 0 ? " and kv.nzp_area = " + finder.nzp_area : "");
                                ret = ExecSQL(conn_db, sqlStr, true);
                                if (!ret.result)
                                {
                                    return null;
                                }
                            }

                            //построить индексы
                            ret = ExecSQL(conn_db, " Create index ix1_temp_kvar_sver on temp_kvar_sver_" + finder.nzp_user + " (nzp_kvar) ", true);
#if PG
                            ExecSQL(conn_db, " analyze temp_kvar_sver_" + finder.nzp_user, true);
#else
                            ExecSQL(conn_db, " Update statistics for table temp_kvar_sver_" + finder.nzp_user, true);
#endif

                            #endregion

                            #region создание вспомогательной таблицы для работы с характеристиками лиц. счетов нашей базы

                            ExecSQL(conn_db, " Drop table temp_prm_sver_our_" + finder.nzp_user, false);
                            ret = ExecSQL(conn_db,
#if PG
 " CREATE unlogged TABLE temp_prm_sver_our_" + finder.nzp_user + "(" +
                                "  nzp_kvar SERIAL NOT NULL," +
                                "  nzp_prm INTEGER," +
                                "  val_prm char(20), " +
                                "  val_prm_d decimal(10,2));"
#else
                                " CREATE temp TABLE temp_prm_sver_our_" + finder.nzp_user + "(" +
                                "  nzp_kvar SERIAL NOT NULL," +
                                "  nzp_prm INTEGER," +
                                "  val_prm char(20), " +
                                "  val_prm_d decimal(10,2)) with no log;"
#endif
, true);
                            if (!ret.result)
                            {
                                return null;
                            }
                            for (int i = 0; i < prefixsOur.Count; i++)
                            {
                                sqlStr = " INSERT INTO temp_prm_sver_our_" + finder.nzp_user + " (nzp_kvar, nzp_prm, val_prm) " +
                                    " SELECT nzp AS nzp_kvar, nzp_prm, val_prm " +
#if PG
 " FROM " + prefixsOur[i] + "_data. prm_1 WHERE nzp_prm IN(51, 3, 8, 2005, 5) AND " + prefixsOur[i] + "_data. prm_1.is_actual <> 100 " +
                                    " AND '01." + finder.month_.ToString("00") + "." + year + "' BETWEEN " + prefixsOur[i] + "_data. prm_1.dat_s AND " + prefixsOur[i] + "_data. prm_1.dat_po";
#else
                                    " FROM " + prefixsOur[i] + "_data: prm_1 WHERE nzp_prm IN(51, 3, 8, 2005, 5) AND " + prefixsOur[i] + "_data: prm_1.is_actual <> 100 " +
                                    " AND '01." + finder.month_.ToString("00") + "." + year + "' BETWEEN " + prefixsOur[i] + "_data: prm_1.dat_s AND " + prefixsOur[i] + "_data: prm_1.dat_po";
#endif
                                ret = ExecSQL(conn_db, sqlStr, true);
                                if (!ret.result)
                                {
                                    return null;
                                }

                                sqlStr = " INSERT INTO temp_prm_sver_our_" + finder.nzp_user + " (nzp_kvar, nzp_prm, val_prm_d) " +
                                    " SELECT nzp AS nzp_kvar, nzp_prm, val_prm " +
#if PG
 " FROM " + prefixsOur[i] + "_data. prm_1 WHERE nzp_prm IN(4, 6) AND " + prefixsOur[i] + "_data. prm_1.is_actual <> 100 " +
                                    " AND '01." + finder.month_.ToString("00") + "." + year + "' BETWEEN " + prefixsOur[i] + "_data. prm_1.dat_s AND " + prefixsOur[i] + "_data. prm_1.dat_po";
#else
                                    " FROM " + prefixsOur[i] + "_data: prm_1 WHERE nzp_prm IN(4, 6) AND " + prefixsOur[i] + "_data: prm_1.is_actual <> 100 " +
                                    " AND '01." + finder.month_.ToString("00") + "." + year + "' BETWEEN " + prefixsOur[i] + "_data: prm_1.dat_s AND " + prefixsOur[i] + "_data: prm_1.dat_po";
#endif
                                ret = ExecSQL(conn_db, sqlStr, true);
                                if (!ret.result)
                                {
                                    return null;
                                }
                            }
                            //построить индексы
                            ret = ExecSQL(conn_db, " Create index ix1_temp_temp_prm_sver_our on temp_prm_sver_our_" + finder.nzp_user + " (nzp_kvar) ", true);
#if PG
                            ExecSQL(conn_db, " analyze temp_prm_sver_our_" + finder.nzp_user, true);
#else
                            ExecSQL(conn_db, " Update statistics for table temp_prm_sver_our_" + finder.nzp_user, true);
#endif

                            #endregion

                            #region создание вспомогательной таблицы для работы с характеристиками лиц. счетов новой базы

                            ExecSQL(conn_db, " Drop table temp_prm_sver_their_" + finder.nzp_user, false);
                            ret = ExecSQL(conn_db,
#if PG
 " CREATE unlogged TABLE temp_prm_sver_their_" + finder.nzp_user + "(" +
                                "  nzp_kvar SERIAL NOT NULL," +
                                "  nzp_prm INTEGER," +
                                "  val_prm CHAR(20)," +
                                "  val_prm_d decimal(10,2));"
#else
                                " CREATE temp TABLE temp_prm_sver_their_" + finder.nzp_user + "(" +
                                "  nzp_kvar SERIAL NOT NULL," +
                                "  nzp_prm INTEGER," +
                                "  val_prm CHAR(20)," +
                                "  val_prm_d decimal(10,2)) with no log;"
#endif
, true);
                            if (!ret.result)
                            {
                                return null;
                            }
                            for (int i = 0; i < prefixsOur.Count; i++)
                            {
                                sqlStr = " INSERT INTO temp_prm_sver_their_" + finder.nzp_user + " (nzp_kvar, nzp_prm, val_prm) " +
                                    " SELECT nzp AS nzp_kvar, nzp_prm, val_prm " +
#if PG
 " FROM " + prefixsTheir[i] + "_data. prm_1 WHERE nzp_prm IN(51, 3, 8, 2005, 5) AND " + prefixsTheir[i] + "_data. prm_1.is_actual <> 100 " +
                                    " AND '01." + finder.month_.ToString("00") + "." + year + "' BETWEEN " + prefixsTheir[i] + "_data. prm_1.dat_s AND " + prefixsTheir[i] + "_data. prm_1.dat_po";
#else
                                    " FROM " + prefixsTheir[i] + "_data: prm_1 WHERE nzp_prm IN(51, 3, 8, 2005, 5) AND " + prefixsTheir[i] + "_data: prm_1.is_actual <> 100 " +
                                    " AND '01." + finder.month_.ToString("00") + "." + year + "' BETWEEN " + prefixsTheir[i] + "_data: prm_1.dat_s AND " + prefixsTheir[i] + "_data: prm_1.dat_po";
#endif
                                ret = ExecSQL(conn_db, sqlStr, true);
                                if (!ret.result) return null;

                                sqlStr = " INSERT INTO temp_prm_sver_their_" + finder.nzp_user + " (nzp_kvar, nzp_prm, val_prm_d) " +
                                    " SELECT nzp AS nzp_kvar, nzp_prm, val_prm " +
#if PG
 " FROM " + prefixsTheir[i] + "_data. prm_1 WHERE nzp_prm IN(4, 6) AND " + prefixsTheir[i] + "_data. prm_1.is_actual <> 100 " +
                                    " AND '01." + finder.month_.ToString("00") + "." + year + "' BETWEEN " + prefixsTheir[i] + "_data. prm_1.dat_s AND " + prefixsTheir[i] + "_data. prm_1.dat_po";
#else
                                    " FROM " + prefixsTheir[i] + "_data: prm_1 WHERE nzp_prm IN(4, 6) AND " + prefixsTheir[i] + "_data: prm_1.is_actual <> 100 " +
                                    " AND '01." + finder.month_.ToString("00") + "." + year + "' BETWEEN " + prefixsTheir[i] + "_data: prm_1.dat_s AND " + prefixsTheir[i] + "_data: prm_1.dat_po";
#endif
                                ret = ExecSQL(conn_db, sqlStr, true);
                                if (!ret.result) return null;
                            }
                            //построить индексы
                            ret = ExecSQL(conn_db, " Create index ix1_temp_prm_sver_their on temp_prm_sver_their_" + finder.nzp_user + " (nzp_kvar) ", true);
#if PG
                            ExecSQL(conn_db, " analyze temp_prm_sver_their_" + finder.nzp_user, true);
#else
                            ExecSQL(conn_db, " Update statistics for table temp_prm_sver_their_" + finder.nzp_user, true);
#endif

                            #endregion

                            #region чужие

                            ExecSQL(conn_db, "drop table temp_kvar_odpu_cnt_their", false);

#if PG
                            ExecSQL(conn_db, "CREATE unlogged TABLE temp_kvar_odpu_cnt_their (nzp_kvar INTEGER," +
                                "odpu_cnt integer) ", true);
#else
                            ExecSQL(conn_db, "CREATE temp TABLE temp_kvar_odpu_cnt_their (nzp_kvar INTEGER," +
                                "odpu_cnt integer) with no log", true);
#endif

                            for (int i = 0; i < prefixsTheir.Count; i++)
                            {
                                sqlStr = " INSERT INTO temp_kvar_odpu_cnt_their (nzp_kvar, odpu_cnt) " +
#if PG
 " SELECT nzp, count(*) from " + prefixsTheir[i] + "_data. counters_spis s " +
#else
                                    " SELECT nzp, count(*) from " + prefixsTheir[i] + "_data: counters_spis s " +
#endif
 " WHERE s.nzp_type = 3 and s.dat_close IS NOT NULL GROUP BY 1";
                                //" FROM temp_prm_sver_their_" + finder.nzp_user + " t";
                                ret = ExecSQL(conn_db, sqlStr, true);
                                if (!ret.result) return null;
                            }

                            ret = ExecSQL(conn_db, "create index ix_temp_kvar_odpu_cnt_their_1 on temp_kvar_odpu_cnt_their(nzp_kvar)", true);
                            if (!ret.result) return null;
#if PG
                            ExecSQL(conn_db, "analyze temp_kvar_odpu_cnt_their", true);
#else
                            ExecSQL(conn_db, "update statistics for table temp_kvar_odpu_cnt_their", true);
#endif
                            #endregion

                            #region наши
                            ExecSQL(conn_db, "drop table temp_kvar_odpu_cnt_our", false);

#if PG
                            ExecSQL(conn_db, "CREATE unlogged TABLE temp_kvar_odpu_cnt_our (nzp_kvar INTEGER," +
                                "odpu_cnt integer)", true);
#else
                            ExecSQL(conn_db, "CREATE temp TABLE temp_kvar_odpu_cnt_our (nzp_kvar INTEGER," +
                                "odpu_cnt integer) with no log", true);
#endif

                            for (int i = 0; i < prefixsOur.Count; i++)
                            {
                                sqlStr = " INSERT INTO temp_kvar_odpu_cnt_our (nzp_kvar, odpu_cnt) " +
#if PG
 " select nzp, count(*) from " + prefixsOur[i] + "_data. counters_spis s " +
#else
                                    " select nzp, count(*) from " + prefixsOur[i] + "_data: counters_spis s " +
#endif
 " WHERE s.nzp_type = 3 and s.dat_close IS NOT NULL GROUP BY 1";
                                //" FROM temp_prm_sver_our_" + finder.nzp_user + " t";
                                ret = ExecSQL(conn_db, sqlStr, true);
                                if (!ret.result) return null;
                            }

                            ret = ExecSQL(conn_db, "create index ix_temp_kvar_odpu_cnt_our_1 on temp_kvar_odpu_cnt_our(nzp_kvar)", true);
                            if (!ret.result) return null;
#if PG
                            ExecSQL(conn_db, "analyze temp_kvar_odpu_cnt_our", true);
#else
                            ExecSQL(conn_db, "update statistics for table temp_kvar_odpu_cnt_our", true);
#endif
                            #endregion

                            #region результирующая таблица по лицевым счетам

                            ret = ExecSQL(conn_db,
                                " INSERT INTO temp_prm_sver_their_" + finder.nzp_user + "  (nzp_kvar, nzp_prm, val_prm) " +
                                " SELECT nzp_kvar, 1112, cast(odpu_cnt as char(20)) FROM temp_kvar_odpu_cnt_their", true);

                            ret = ExecSQL(conn_db,
                                " INSERT into temp_prm_sver_our_" + finder.nzp_user + "  (nzp_kvar, nzp_prm, val_prm) " +
                                " SELECT nzp_kvar, 1112, cast(odpu_cnt as char(20)) FROM temp_kvar_odpu_cnt_our", true);

                            ExecSQL(conn_db, " Drop table temp_prm_sver_total_" + finder.nzp_user, false);
                            ret = ExecSQL(conn_db,
                                " SELECT t.nzp_kvar, t.nzp_prm, case when t.nzp_prm IN(6,4) then cast(t.val_prm_d as char(20)) else t.val_prm end val_prm, " +
                                " case when t.nzp_prm IN(6,4) then cast(o.val_prm_d as char(20)) else o.val_prm end as our_val_prm " +
#if PG
 " INTO TEMP temp_prm_sver_total_" + finder.nzp_user + " FROM temp_prm_sver_their_" + finder.nzp_user + " t, temp_prm_sver_our_" + finder.nzp_user + " o where t.nzp_kvar = o.nzp_kvar and t.nzp_prm = o.nzp_prm " +
                                (finder.prms == "show_differences" ? " AND ((t.nzp_prm IN(6,4) and coalesce(t.val_prm_d,0) <> coalesce(o.val_prm_d,0)) or (t.nzp_prm NOT IN(6,4) and t.val_prm <> o.val_prm))" : ""), true);

#else
                                " FROM temp_prm_sver_their_" + finder.nzp_user + " t, temp_prm_sver_our_" + finder.nzp_user + " o where t.nzp_kvar = o.nzp_kvar and t.nzp_prm = o.nzp_prm " +
                                (finder.prms == "show_differences" ? " AND ((t.nzp_prm IN(6,4) and nvl(t.val_prm_d,0) <> nvl(o.val_prm_d,0)) or (t.nzp_prm NOT IN(6,4) and t.val_prm <> o.val_prm))" : "") +
                                " INTO TEMP temp_prm_sver_total_" + finder.nzp_user + " with no log", true);
#endif
                            if (!ret.result)
                            {
                                return null;
                            }
                            ret = ExecSQL(conn_db, "create index ix_temp_prm_sver_total_" + finder.nzp_user + "_1 on temp_prm_sver_total_" + finder.nzp_user + "(nzp_kvar, nzp_prm)", true);
                            if (!ret.result) return null;
#if PG
                            ExecSQL(conn_db, "analyze temp_prm_sver_total_" + finder.nzp_user, true);
#else
                            ExecSQL(conn_db, "update statistics for table temp_prm_sver_total_" + finder.nzp_user, true);
#endif

                            #endregion

                            #region выгрузка результирующих данных по лицевым счетам
                            string temp_kvar_sver = "temp_kvar_sver_" + finder.nzp_user;

                            ret = ExecSQL(conn_db, "update " + temp_kvar_sver + " set val_prm_1_1 = (SELECT t.our_val_prm FROM temp_prm_sver_total_" + finder.nzp_user + " t WHERE t.nzp_kvar = " + temp_kvar_sver + ".nzp_kvar AND t.nzp_prm = 51)", true);
                            if (!ret.result) return null;
                            ret = ExecSQL(conn_db, "update " + temp_kvar_sver + " set val_prm_1_2 = (SELECT t.val_prm FROM temp_prm_sver_total_" + finder.nzp_user + " t WHERE t.nzp_kvar = " + temp_kvar_sver + ".nzp_kvar AND t.nzp_prm = 51)", true);
                            if (!ret.result) return null;

                            ret = ExecSQL(conn_db, "update " + temp_kvar_sver + " set val_prm_2_1 = (SELECT t.our_val_prm FROM temp_prm_sver_total_" + finder.nzp_user + " t WHERE t.nzp_kvar = " + temp_kvar_sver + ".nzp_kvar AND t.nzp_prm = 3)", true);
                            if (!ret.result) return null;
                            ret = ExecSQL(conn_db, "update " + temp_kvar_sver + " set val_prm_2_2 = (SELECT t.val_prm FROM temp_prm_sver_total_" + finder.nzp_user + " t WHERE t.nzp_kvar = " + temp_kvar_sver + ".nzp_kvar AND t.nzp_prm = 3)", true);
                            if (!ret.result) return null;

                            ret = ExecSQL(conn_db, "update " + temp_kvar_sver + " set val_prm_3_1 = (SELECT t.our_val_prm FROM temp_prm_sver_total_" + finder.nzp_user + " t WHERE t.nzp_kvar = " + temp_kvar_sver + ".nzp_kvar AND t.nzp_prm = 8)", true);
                            if (!ret.result) return null;
                            ret = ExecSQL(conn_db, "update " + temp_kvar_sver + " set val_prm_3_2 = (SELECT t.val_prm FROM temp_prm_sver_total_" + finder.nzp_user + " t WHERE t.nzp_kvar = " + temp_kvar_sver + ".nzp_kvar AND t.nzp_prm = 8)", true);
                            if (!ret.result) return null;

                            ret = ExecSQL(conn_db, "update " + temp_kvar_sver + " set val_prm_4_1 = (SELECT t.our_val_prm FROM temp_prm_sver_total_" + finder.nzp_user + " t WHERE t.nzp_kvar = " + temp_kvar_sver + ".nzp_kvar AND t.nzp_prm = 4)", true);
                            if (!ret.result) return null;
                            ret = ExecSQL(conn_db, "update " + temp_kvar_sver + " set val_prm_4_2 = (SELECT t.val_prm FROM temp_prm_sver_total_" + finder.nzp_user + " t WHERE t.nzp_kvar = " + temp_kvar_sver + ".nzp_kvar AND t.nzp_prm = 4)", true);
                            if (!ret.result) return null;

                            ret = ExecSQL(conn_db, "update " + temp_kvar_sver + " set val_prm_5_1 = (SELECT t.our_val_prm FROM temp_prm_sver_total_" + finder.nzp_user + " t WHERE t.nzp_kvar = " + temp_kvar_sver + ".nzp_kvar AND t.nzp_prm = 6)", true);
                            if (!ret.result) return null;
                            ret = ExecSQL(conn_db, "update " + temp_kvar_sver + " set val_prm_5_2 = (SELECT t.val_prm FROM temp_prm_sver_total_" + finder.nzp_user + " t WHERE t.nzp_kvar = " + temp_kvar_sver + ".nzp_kvar AND t.nzp_prm = 6)", true);
                            if (!ret.result) return null;

                            ret = ExecSQL(conn_db, "update " + temp_kvar_sver + " set val_prm_6_1 = (SELECT t.our_val_prm FROM temp_prm_sver_total_" + finder.nzp_user + " t WHERE t.nzp_kvar = " + temp_kvar_sver + ".nzp_kvar AND t.nzp_prm = 2005)", true);
                            if (!ret.result) return null;
                            ret = ExecSQL(conn_db, "update " + temp_kvar_sver + " set val_prm_6_2 = (SELECT t.val_prm FROM temp_prm_sver_total_" + finder.nzp_user + " t WHERE t.nzp_kvar = " + temp_kvar_sver + ".nzp_kvar AND t.nzp_prm = 2005)", true);
                            if (!ret.result) return null;

                            ret = ExecSQL(conn_db, "update " + temp_kvar_sver + " set val_prm_7_1 = (SELECT t.our_val_prm FROM temp_prm_sver_total_" + finder.nzp_user + " t WHERE t.nzp_kvar = " + temp_kvar_sver + ".nzp_kvar AND t.nzp_prm = 5)", true);
                            if (!ret.result) return null;
                            ret = ExecSQL(conn_db, "update " + temp_kvar_sver + " set val_prm_7_2 = (SELECT t.val_prm FROM temp_prm_sver_total_" + finder.nzp_user + " t WHERE t.nzp_kvar = " + temp_kvar_sver + ".nzp_kvar AND t.nzp_prm = 5)", true);
                            if (!ret.result) return null;

                            ret = ExecSQL(conn_db, "update " + temp_kvar_sver + " set val_prm_8_1 = (SELECT nvl(t.odpu_cnt,0) FROM temp_kvar_odpu_cnt_our   t WHERE t.nzp_kvar = " + temp_kvar_sver + ".nzp_kvar)", true);
                            if (!ret.result) return null;
                            ret = ExecSQL(conn_db, "update " + temp_kvar_sver + " set val_prm_8_2 = (SELECT nvl(t.odpu_cnt,0) FROM temp_kvar_odpu_cnt_their t WHERE t.nzp_kvar = " + temp_kvar_sver + ".nzp_kvar)", true);
                            if (!ret.result) return null;

                            sql.Remove(0, sql.Length);
                            sql.Append(" SELECT r.pkod, r.adr, ");
                            sql.Append("val_prm_1_1,");
                            sql.Append("val_prm_1_2,");
                            sql.Append("val_prm_2_1,");
                            sql.Append("val_prm_2_2,");
                            sql.Append("val_prm_3_1,");
                            sql.Append("val_prm_3_2,");
                            sql.Append("val_prm_4_1,");
                            sql.Append("val_prm_4_2,");
                            sql.Append("val_prm_5_1,");
                            sql.Append("val_prm_5_2,");
                            sql.Append("val_prm_6_1,");
                            sql.Append("val_prm_6_2,");
                            sql.Append("val_prm_7_1,");
                            sql.Append("val_prm_7_2, ");
                            //sql.Append("(SELECT t.our_val_prm FROM temp_prm_sver_total_" + finder.nzp_user + " t WHERE t.nzp_kvar = r.nzp_kvar AND t.nzp_prm = 1112) as val_prm_8_1,");
                            //sql.Append("(SELECT t.val_prm     FROM temp_prm_sver_total_" + finder.nzp_user + " t WHERE t.nzp_kvar = r.nzp_kvar AND t.nzp_prm = 1112) as val_prm_8_2 ");
                            sql.Append("val_prm_8_1,");
                            sql.Append("val_prm_8_2 ");
                            sql.Append("FROM temp_kvar_sver_" + finder.nzp_user + " r ");
                            sql.Append("WHERE (r.nzp_kvar IN (SELECT nzp_kvar FROM temp_prm_sver_total_" + finder.nzp_user + ")");
                            sql.Append("or r.nzp_kvar IN (SELECT nzp_kvar FROM temp_kvar_odpu_cnt_our)");
                            sql.Append("or r.nzp_kvar IN (SELECT nzp_kvar FROM temp_kvar_odpu_cnt_their))");
                            sql.Append(finder.prms == "show_differences" ? " and val_prm_8_1 <> val_prm_8_2" : "");
                            sql.Append(" ORDER BY 1");

                            ret = ExecRead(conn_db, out reader, sql.ToString(), true);
                            if (!ret.result) return null;

                            Utils.setCulture();

                            if (reader != null)
                            {
                                //заполнение DataTable
                                DT.TableName = "Q_master1";
                                DT.Load(reader, LoadOption.OverwriteChanges);
                            }

                            #endregion

                            break;
                        }
                    case (ReportSverType.Dom):
                        {
                            #region создание временной таблицы для результирующих данных по домам

                            ExecSQL(conn_db, " Drop table temp_dom_sver_" + finder.nzp_user, false);
                            ret = ExecSQL(conn_db,
#if PG
 " CREATE unlogged TABLE temp_dom_sver_" + finder.nzp_user + "(" +
                                " nzp SERIAL NOT NULL," +
                                " _type INTEGER," +
                                " nzp_dom INTEGER," +
                                " adr CHAR(100)," +
                                " val_prm_1_1 CHAR(20)," +
                                " val_prm_1_2 CHAR(20)," +
                                " val_prm_2_1 CHAR(20)," +
                                " val_prm_2_2 CHAR(20)," +
                                " val_prm_3_1 CHAR(20)," +
                                " val_prm_3_2 CHAR(20));"
#else
                                " CREATE temp TABLE temp_dom_sver_" + finder.nzp_user + "(" +
                                " nzp SERIAL NOT NULL," +
                                " _type INTEGER," +
                                " nzp_dom INTEGER," +
                                " adr CHAR(100)," +
                                " val_prm_1_1 CHAR(20)," +
                                " val_prm_1_2 CHAR(20)," +
                                " val_prm_2_1 CHAR(20)," +
                                " val_prm_2_2 CHAR(20)," +
                                " val_prm_3_1 CHAR(20)," +
                                " val_prm_3_2 CHAR(20)) with no log;"
#endif
, true);
                            if (!ret.result)
                            {
                                return null;
                            }
                            sqlStr = " INSERT into temp_dom_sver_" + finder.nzp_user + "(_type, nzp_dom, adr) " +
                                " SELECT UNIQUE 1, d.nzp_dom," +
                                " 'ул.'||trim(ulica)|| " +
                                " (case when ndom is null then '' else " +
                                " ' д.'||trim(ndom)||(case when nkor='-' then '' else ' корп.'||trim(nkor) end) end)|| " +
                                " (case when kv.nkvar is null then '' else " +
                                " ' кв.'||trim(kv.nkvar)||(case when trim(kv.nkvar_n)='-' then '' else ' комн.'||trim(kv.nkvar_n) end) " +
                                " end ) as adr " +
                                " FROM " +
#if PG
 Points.Pref + "_data. kvar kv, " +
                                Points.Pref + "_data. dom d, " +
                                Points.Pref + "_data. s_ulica s " +
#else
                                Points.Pref + "_data: kvar kv, " +
                                Points.Pref + "_data: dom d, " +
                                Points.Pref + "_data: s_ulica s " +
#endif
 " WHERE " +
                                " kv.nzp_dom = d.nzp_dom " +
                                " AND d.nzp_dom = kv.nzp_dom " +
                                " AND d.nzp_ul = s.nzp_ul";
                            ret = ExecSQL(conn_db, sqlStr, true);
                            if (!ret.result)
                            {
                                return null;
                            }

                            for (int i = 0; i < prefixsTheir.Count; i++)
                            {
                                sqlStr = " INSERT INTO temp_dom_sver_" + finder.nzp_user + " (_type, nzp_dom, adr) " +
                                    " SELECT UNIQUE 2, d.nzp_dom," +
                                    " 'ул.'||trim(ulica)|| " +
                                    " (case when ndom is null then '' else " +
                                    " ' д.'||trim(ndom)||(case when nkor='-' then '' else ' корп.'||trim(nkor) end) end)|| " +
                                    " (case when kv.nkvar is null then '' else " +
                                    " ' кв.'||trim(kv.nkvar)||(case when trim(kv.nkvar_n)='-' then '' else ' комн.'||trim(kv.nkvar_n) end) " +
                                    " end ) as adr " +
                                    " FROM " +
#if PG
 prefixsTheir[i] + "_data. kvar kv, " +
                                    prefixsTheir[i] + "_data. dom d, " +
                                    prefixsTheir[i] + "_data. s_ulica s " +
#else
                                    prefixsTheir[i] + "_data: kvar kv, " +
                                    prefixsTheir[i] + "_data: dom d, " +
                                    prefixsTheir[i] + "_data: s_ulica s " +
#endif
 " WHERE " +
                                    " kv.nzp_dom = d.nzp_dom AND d.nzp_ul = s.nzp_ul " +
                                    " AND d.nzp_dom NOT IN (SELECT nzp_dom FROM temp_dom_sver_" + finder.nzp_user + ")";
                                ret = ExecSQL(conn_db, sqlStr, true);
                                if (!ret.result)
                                {
                                    return null;
                                }
                            }

                            //построить индексы
                            ret = ExecSQL(conn_db, " Create index ix1_temp_dom_sver on temp_dom_sver_" + finder.nzp_user + " (nzp) ", true);
#if PG
                            ExecSQL(conn_db, " analyze temp_dom_sver_" + finder.nzp_user, true);
#else
                            ExecSQL(conn_db, " Update statistics for table temp_dom_sver_" + finder.nzp_user, true);
#endif

                            #endregion

                            #region создание вспомогательной таблицы для работы с характеристиками домов нашей базы

                            ExecSQL(conn_db, " Drop table temp_prm_dom_sver_our_" + finder.nzp_user, false);
                            ret = ExecSQL(conn_db,
#if PG
 " CREATE unlogged TABLE temp_prm_dom_sver_our_" + finder.nzp_user + "(" +
                                "  nzp SERIAL NOT NULL," +
                                "  nzp_dom INTEGER," +
                                "  nzp_prm INTEGER," +
                                "  val_prm CHAR(20)) ;"
#else
                                " CREATE temp TABLE temp_prm_dom_sver_our_" + finder.nzp_user + "(" +
                                "  nzp SERIAL NOT NULL," +
                                "  nzp_dom INTEGER," +
                                "  nzp_prm INTEGER," +
                                "  val_prm CHAR(20)) with no log;"
#endif
, true);
                            if (!ret.result)
                            {
                                return null;
                            }
                            for (int i = 0; i < prefixsOur.Count; i++)
                            {
                                sqlStr = " INSERT INTO temp_prm_dom_sver_our_" + finder.nzp_user + " (nzp_dom, nzp_prm, val_prm) " +
                                    " SELECT UNIQUE d.nzp_dom, p2.nzp_prm, p2.val_prm " +
#if PG
 " FROM " + prefixsOur[i] + "_data. prm_2 p2, " + prefixsOur[i] + "_data. dom d WHERE nzp_prm IN(40, 2001) AND p2.is_actual <> 100 " +
                                    " AND '01." + finder.month_.ToString("00") + "." + year + "' BETWEEN p2.dat_s AND p2.dat_po AND p2.nzp = d.nzp_dom;";
#else
                                    " FROM " + prefixsOur[i] + "_data: prm_2 p2, " + prefixsOur[i] + "_data: dom d WHERE nzp_prm IN(40, 2001) AND p2.is_actual <> 100 " +
                                    " AND '01." + finder.month_.ToString("00") + "." + year + "' BETWEEN p2.dat_s AND p2.dat_po AND p2.nzp = d.nzp_dom;";
#endif
                                ret = ExecSQL(conn_db, sqlStr, true);
                                if (!ret.result)
                                {
                                    return null;
                                }
                            }
                            //построить индексы
                            ret = ExecSQL(conn_db, " Create index ix1_temp_prm_sver_dom_our on temp_prm_dom_sver_our_" + finder.nzp_user + " (nzp) ", true);
#if PG
                            ExecSQL(conn_db, " analyze temp_prm_dom_sver_our_" + finder.nzp_user, true);
#else
                            ExecSQL(conn_db, " Update statistics for table temp_prm_dom_sver_our_" + finder.nzp_user, true);
#endif

                            #endregion

                            #region создание вспомогательной таблицы для работы с характеристиками домов новой базы

                            ExecSQL(conn_db, " Drop table temp_prm_dom_sver_their_" + finder.nzp_user, false);
                            ret = ExecSQL(conn_db,
#if PG
 " CREATE unlogged TABLE temp_prm_dom_sver_their_" + finder.nzp_user + "(" +
                                "  nzp SERIAL NOT NULL," +
                                "  nzp_dom INTEGER," +
                                "  nzp_prm INTEGER," +
                                "  val_prm CHAR(20)) ;"
#else
                                " CREATE temp TABLE temp_prm_dom_sver_their_" + finder.nzp_user + "(" +
                                "  nzp SERIAL NOT NULL," +
                                "  nzp_dom INTEGER," +
                                "  nzp_prm INTEGER," +
                                "  val_prm CHAR(20)) with no log;"
#endif
, true);
                            if (!ret.result)
                            {
                                return null;
                            }
                            for (int i = 0; i < prefixsTheir.Count; i++)
                            {
                                sqlStr = " INSERT INTO temp_prm_dom_sver_their_" + finder.nzp_user + " (nzp_dom, nzp_prm, val_prm) " +
                                    " SELECT UNIQUE d.nzp_dom, p2.nzp_prm, p2.val_prm " +
#if PG
 " FROM " + prefixsTheir[i] + "_data. prm_2 p2, " + prefixsTheir[i] + "_data. dom d WHERE nzp_prm IN(40, 2001) AND p2.is_actual <> 100 " +
                                    " AND '01." + finder.month_.ToString("00") + "." + year + "' BETWEEN p2.dat_s AND p2.dat_po AND p2.nzp = d.nzp_dom;";
#else
                                    " FROM " + prefixsTheir[i] + "_data: prm_2 p2, " + prefixsTheir[i] + "_data: dom d WHERE nzp_prm IN(40, 2001) AND p2.is_actual <> 100 " +
                                    " AND '01." + finder.month_.ToString("00") + "." + year + "' BETWEEN p2.dat_s AND p2.dat_po AND p2.nzp = d.nzp_dom;";
#endif
                                ret = ExecSQL(conn_db, sqlStr, true);
                                if (!ret.result) return null;
                            }

                            #region чужие

                            ExecSQL(conn_db, "drop table temp_dom_odpu_cnt_their", false);

#if PG
                            ExecSQL(conn_db, "CREATE unlogged TABLE temp_dom_odpu_cnt_their (nzp_dom INTEGER," +
                                "odpu_cnt integer)", true);
#else
                            ExecSQL(conn_db, "CREATE temp TABLE temp_dom_odpu_cnt_their (nzp_dom INTEGER," +
                                "odpu_cnt integer) with no log", true);
#endif


                            for (int i = 0; i < prefixsTheir.Count; i++)
                            {
                                sqlStr = " INSERT INTO temp_dom_odpu_cnt_their (nzp_dom, odpu_cnt) " +
#if PG
 " Select nzp_dom, (select count(*) from " + prefixsTheir[i] + "_data. counters_spis s where s.nzp_type = 1 and s.nzp = t.nzp_dom and s.dat_close IS NOT NULL) FROM temp_prm_dom_sver_their_" + finder.nzp_user + " t";
#else
                                    " Select nzp_dom, (select count(*) from " + prefixsTheir[i] + "_data: counters_spis s where s.nzp_type = 1 and s.nzp = t.nzp_dom and s.dat_close IS NOT NULL) FROM temp_prm_dom_sver_their_" + finder.nzp_user + " t";
#endif
                                ret = ExecSQL(conn_db, sqlStr, true);
                                if (!ret.result) return null;
                            }

                            #endregion

                            #region наши
                            ExecSQL(conn_db, "drop table temp_dom_odpu_cnt_our", false);

#if PG
                            ExecSQL(conn_db, "CREATE unlogged TABLE temp_dom_odpu_cnt_our (nzp_dom INTEGER," +
                                "odpu_cnt integer) ", true);
#else
                            ExecSQL(conn_db, "CREATE temp TABLE temp_dom_odpu_cnt_our (nzp_dom INTEGER," +
                                "odpu_cnt integer) with no log", true);
#endif


                            for (int i = 0; i < prefixsOur.Count; i++)
                            {
                                sqlStr = " INSERT INTO temp_dom_odpu_cnt_our (nzp_dom, odpu_cnt) " +
#if PG
 " Select nzp_dom, (select count(*) from " + prefixsOur[i] + "_data. counters_spis s where s.nzp_type = 1 and s.nzp = t.nzp_dom and s.dat_close IS NOT NULL) FROM temp_prm_dom_sver_our_" + finder.nzp_user + " t";
#else
                                    " Select nzp_dom, (select count(*) from " + prefixsOur[i] + "_data: counters_spis s where s.nzp_type = 1 and s.nzp = t.nzp_dom and s.dat_close IS NOT NULL) FROM temp_prm_dom_sver_our_" + finder.nzp_user + " t";
#endif
                                ret = ExecSQL(conn_db, sqlStr, true);
                                if (!ret.result) return null;
                            }
                            #endregion


                            //построить индексы
                            ret = ExecSQL(conn_db, " Create index ix1_temp_prm_dom_sver_their on temp_prm_dom_sver_their_" + finder.nzp_user + " (nzp) ", true);
#if PG
                            ExecSQL(conn_db, " analyze temp_prm_dom_sver_their_" + finder.nzp_user, true);
#else
                            ExecSQL(conn_db, " Update statistics for table temp_prm_dom_sver_their_" + finder.nzp_user, true);
#endif

                            #endregion

                            #region выгрузка результирующих данных по домам

                            ret = ExecSQL(conn_db,
                                " insert into temp_prm_dom_sver_their_" + finder.nzp_user + "  (nzp_dom, nzp_prm, val_prm) " +
                                " select nzp_dom, 1112, cast(odpu_cnt as char(20)) from temp_dom_odpu_cnt_their", true);

                            ret = ExecSQL(conn_db,
                                " insert into temp_prm_dom_sver_our_" + finder.nzp_user + "  (nzp_dom, nzp_prm, val_prm) " +
                                " select nzp_dom, 1112, cast(odpu_cnt as char(20)) from temp_dom_odpu_cnt_our", true);

                            ExecSQL(conn_db, " Drop table temp_prm_dom_total_" + finder.nzp_user, false);
                            ret = ExecSQL(conn_db,
#if PG
 " select t.nzp_dom, t.nzp_prm, t.val_prm, o.val_prm as our_val_prm into unlogged temp_prm_dom_total_" + finder.nzp_user + "from temp_prm_dom_sver_their_" + finder.nzp_user + " t, temp_prm_dom_sver_our_" + finder.nzp_user +
                                " o where t.nzp_dom = o.nzp_dom and t.nzp_prm = o.nzp_prm " +
                                (finder.prms == "show_differences" ? "and t.val_prm <> o.val_prm" : ""), true);
#else
                                " select t.nzp_dom, t.nzp_prm, t.val_prm, o.val_prm as our_val_prm from temp_prm_dom_sver_their_" + finder.nzp_user + " t, temp_prm_dom_sver_our_" + finder.nzp_user +
                                " o where t.nzp_dom = o.nzp_dom and t.nzp_prm = o.nzp_prm " +
                                (finder.prms == "show_differences" ? "and t.val_prm <> o.val_prm" : "") +
                                " into temp temp_prm_dom_total_" + finder.nzp_user + " with no log", true);
#endif
                            if (!ret.result)
                            {
                                return null;
                            }

                            sql.Remove(0, sql.Length);
                            sql.Append("SELECT r.adr, ");

                            sql.Append("(SELECT t.odpu_cnt FROM temp_dom_odpu_cnt_our   t WHERE t.nzp_dom = r.nzp_dom) as val_prm_1_1,");
                            sql.Append("(SELECT t.odpu_cnt FROM temp_dom_odpu_cnt_their t WHERE t.nzp_dom = r.nzp_dom) as val_prm_1_2,");

                            sql.Append("(SELECT t.our_val_prm FROM temp_prm_dom_total_" + finder.nzp_user + " t WHERE t.nzp_dom = r.nzp_dom AND t.nzp_prm = 40) as val_prm_2_1,");
                            sql.Append("(SELECT t.val_prm     FROM temp_prm_dom_total_" + finder.nzp_user + " t WHERE t.nzp_dom = r.nzp_dom AND t.nzp_prm = 40) as val_prm_2_2,");

                            sql.Append("(SELECT t.our_val_prm FROM temp_prm_dom_total_" + finder.nzp_user + " t WHERE t.nzp_dom = r.nzp_dom AND t.nzp_prm = 2001) as val_prm_3_1,");
                            sql.Append("(SELECT t.val_prm     FROM temp_prm_dom_total_" + finder.nzp_user + " t WHERE t.nzp_dom = r.nzp_dom AND t.nzp_prm = 2001) as val_prm_3_2 ");

                            sql.Append("FROM temp_dom_sver_" + finder.nzp_user + " r, temp_prm_dom_total_" + finder.nzp_user + " t ");
                            sql.Append("WHERE r.nzp_dom = t.nzp_dom ");

                            sql.Append("ORDER BY 1");

                            if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
                            {
                                MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                                conn_db.Close();
                                ret.result = false;
                                return null;
                            }

                            Utils.setCulture();

                            if (reader != null)
                            {
                                //заполнение DataTable
                                DT.TableName = "Q_master2";
                                DT.Load(reader, LoadOption.OverwriteChanges);
                                reader.Close();
                            }

                            #endregion

                            break;
                        }
                    case (ReportSverType.Service):
                        {
                            #region услуги

                            ExecSQL(conn_db, " Drop table temp_serv_sver_1" + finder.nzp_user, false);
#if PG
                            sqlStr = " CREATE unlogged TABLE temp_serv_sver_1" + finder.nzp_user + "(" +
                                "   _type INTEGER," +
                                "   nzp_serv INTEGER," +
                                "   nzp_supp INTEGER," +
                                "   nzp_kvar SERIAL NOT NULL," +
                                "   adr CHAR(100)," +
                                "   val_prm_1_1 NUMERIC(15,2) DEFAULT 0," +
                                "   val_prm_1_2 NUMERIC(15,2) DEFAULT 0);";
#else
                            sqlStr = " CREATE temp TABLE temp_serv_sver_1" + finder.nzp_user + "(" +
                                "   _type INTEGER," +
                                "   nzp_serv INTEGER," +
                                "   nzp_supp INTEGER," +
                                "   nzp_kvar SERIAL NOT NULL," +
                                "   adr CHAR(100)," +
                                "   val_prm_1_1 DECIMAL(15,2) DEFAULT 0," +
                                "   val_prm_1_2 DECIMAL(15,2) DEFAULT 0) with no log;";
#endif
                            ret = ExecSQL(conn_db, sqlStr, true);
                            if (!ret.result)
                            {
                                return null;
                            }

                            //построить индексы
                            ret = ExecSQL(conn_db, " Create index ix1_temp_serv_sver1 on temp_serv_sver_1" + finder.nzp_user + " (nzp_kvar) ", true);
                            ret = ExecSQL(conn_db, " Create index ix1_temp_serv_sver2 on temp_serv_sver_1" + finder.nzp_user + " (nzp_serv) ", true);
                            ret = ExecSQL(conn_db, " Create index ix1_temp_serv_sver3 on temp_serv_sver_1" + finder.nzp_user + " (nzp_supp) ", true);
#if PG
                            ExecSQL(conn_db, " analyze temp_serv_sver_1" + finder.nzp_user, true);
#else
                            ExecSQL(conn_db, " Update statistics for table temp_serv_sver_1" + finder.nzp_user, true);
#endif

                            if (!ret.result)
                            {
                                return null;
                            }

                            for (int i = 0; i < prefixsOur.Count; i++)
                            {
                                sql.Remove(0, sql.Length);
                                sqlStr = " INSERT INTO temp_serv_sver_1" + finder.nzp_user +
                                    " (nzp_kvar, _type, nzp_serv, nzp_supp, val_prm_1_1) " +
                                    " SELECT nzp_kvar, 1, nzp_serv, nzp_supp, SUM(sum_insaldo) FROM " +
#if PG
 prefixsOur[i] + "_charge_" + finder.year_ + ". charge_" + finder.month_.ToString("00") + " " +
#else
                                    prefixsOur[i] + "_charge_" + finder.year_ + ": charge_" + finder.month_.ToString("00") + " " +
#endif
 " GROUP BY 1, 2, 3, 4";
                                ret = ExecSQL(conn_db, sqlStr, true);
                                if (!ret.result) return null;
                            }

                            for (int i = 0; i < prefixsTheir.Count; i++)
                            {
                                sql.Remove(0, sql.Length);
                                sqlStr = " INSERT INTO temp_serv_sver_1" + finder.nzp_user +
                                    " (nzp_kvar, _type, nzp_serv, nzp_supp, val_prm_1_2) " +
                                    " SELECT nzp_kvar, 2, nzp_serv, nzp_supp, SUM(sum_insaldo) FROM " +
#if PG
 prefixsTheir[i] + "_charge_" + finder.year_ + ". charge_" + finder.month_.ToString("00") + " " +
#else
                                    prefixsTheir[i] + "_charge_" + finder.year_ + ": charge_" + finder.month_.ToString("00") + " " +
#endif
 " GROUP BY 1, 2, 3, 4";
                                ret = ExecSQL(conn_db, sqlStr, true);
                                if (!ret.result) return null;
                            }

                            ExecSQL(conn_db, " Drop table temp_serv_sver_" + finder.nzp_user, false);
#if PG
                            sqlStr = " CREATE unlogged TABLE temp_serv_sver_" + finder.nzp_user + "(" +
                                "   _type INTEGER," +
                                "   nzp_serv INTEGER," +
                                "   nzp_supp INTEGER," +
                                "   nzp_kvar SERIAL NOT NULL," +
                                "   adr CHAR(100)," +
                                "   val_prm_1_1 NUMERIC(15,2) DEFAULT 0," +
                                "   val_prm_1_2 NUMERIC(15,2) DEFAULT 0);";
#else
                            sqlStr = " CREATE temp TABLE temp_serv_sver_" + finder.nzp_user + "(" +
                                "   _type INTEGER," +
                                "   nzp_serv INTEGER," +
                                "   nzp_supp INTEGER," +
                                "   nzp_kvar SERIAL NOT NULL," +
                                "   adr CHAR(100)," +
                                "   val_prm_1_1 DECIMAL(15,2) DEFAULT 0," +
                                "   val_prm_1_2 DECIMAL(15,2) DEFAULT 0) with no log;";
#endif
                            ret = ExecSQL(conn_db, sqlStr, true);
                            if (!ret.result)
                            {
                                return null;
                            }

                            ret = ExecSQL(conn_db, " Create index ix1_temp_serv_sver4 on temp_serv_sver_" + finder.nzp_user + " (nzp_kvar) ", true);
                            ret = ExecSQL(conn_db, " Create index ix1_temp_serv_sver5 on temp_serv_sver_" + finder.nzp_user + " (nzp_serv) ", true);
                            ret = ExecSQL(conn_db, " Create index ix1_temp_serv_sver6 on temp_serv_sver_" + finder.nzp_user + " (nzp_supp) ", true);
#if PG
                            ExecSQL(conn_db, " analyze temp_serv_sver_" + finder.nzp_user, true);
#else
                            ExecSQL(conn_db, " Update statistics for table temp_serv_sver_" + finder.nzp_user, true);
#endif


                            sql.Remove(0, sql.Length);
                            sqlStr = " INSERT INTO temp_serv_sver_" + finder.nzp_user +
                                " (nzp_kvar, nzp_serv, nzp_supp, val_prm_1_1, val_prm_1_2) " +
                                " SELECT nzp_kvar, nzp_serv, nzp_supp, " +
                                " sum(case when _type = 1 then val_prm_1_1 else 0 end), " +
                                " sum(case when _type = 2 then val_prm_1_2 else 0 end) FROM temp_serv_sver_1" + finder.nzp_user +
                                " GROUP BY 1, 2, 3";
                            ret = ExecSQL(conn_db, sqlStr, true);
                            if (!ret.result) return null;

                            #region выгрузка результирующих данных по услугам

                            sql.Remove(0, sql.Length);
                            sql.Append("SELECT t.nzp_serv, kv.adr, ss.service, sp.name_supp, t.val_prm_1_1, t.val_prm_1_2 ");
                            sql.Append("FROM temp_serv_sver_" + finder.nzp_user + " t, temp_kvar_sver_" + finder.nzp_user + " kv, ");
#if PG
                            sql.Append(Points.Pref + "_kernel. services ss,");
                            sql.Append(Points.Pref + "_kernel. supplier sp ");
#else
                            sql.Append(Points.Pref + "_kernel: services ss,");
                            sql.Append(Points.Pref + "_kernel: supplier sp ");
#endif
                            sql.Append("WHERE sp.nzp_supp = t.nzp_supp ");
                            sql.Append("AND ss.nzp_serv = t.nzp_serv ");
                            sql.Append("AND t.nzp_kvar = kv.nzp_kvar ");
                            if (finder.prms == "show_differences") sql.Append(" and t.val_prm_1_1 <> t.val_prm_1_2 ");
                            sql.Append("ORDER BY t.nzp_serv;");

                            if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
                            {
                                MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                                conn_db.Close();
                                ret.result = false;
                                return null;
                            }

                            Utils.setCulture();

                            if (reader != null)
                            {
                                DT.TableName = "Q_master3";
                                DT.Load(reader, LoadOption.OverwriteChanges);
                                reader.Close();
                            }
                            #endregion

                            #endregion

                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка формирования отчета  " +
                    ex.Message, MonitorLog.typelog.Error, true);
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                    reader.Dispose();
                }
                conn_db.Close();
            }
            return DT;
        }

        public DataTable GetNoticeToDebitor(Deal finder, out Returns ret)
        {
            IDataReader reader = null;
            DataTable res_table = null;
            IDbConnection conn_db = null;

            ret = new Returns(true);

            try
            {
                #region Подключение к БД
                conn_db = GetConnection(Constants.cons_Kernel);//new IDbConnection(Constants.cons_Webdata);
                ret = OpenDb(conn_db, true);
                if (!ret.result) throw new Exception("FastReport : Ошибка при открытии соединения с БД " + ret.text);
                #endregion

                string sql = "";
                List<string> nzp_group = new List<string>() { finder.nzp_group.ToString() };
                //string nzp = "";

                #region получить список префиксов
                //**************************************************************************************************************
                string temp_table1 = "t1_" + finder.nzp_user;
                
                ExecSQL(conn_db, " drop table " + temp_table1, false);
                
                sql = " create temp table " + temp_table1 + " (pref varchar(10))";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) throw new Exception(ret.text);

                if (finder.nzp_deal == 0)
                {
                    sql = " insert into " + temp_table1 + " (pref) " +
                        " select trim(d.pref) " +
                        " from " + Points.Pref + "_debt" + DBManager.tableDelimiter + "deal d, " +
                        Points.Pref + "_debt" + DBManager.tableDelimiter + "groups_operations_details gd " +
                        " where d.nzp_deal = gd.nzp_deal " +
                        "   and gd.nzp_group = " + finder.nzp_group +
                        " group by 1";
                }
                else
                {
                    sql = " insert into " + temp_table1 + " (pref) " +
                        " select pref from  " + Points.Pref + "_debt" + DBManager.tableDelimiter + "deal " +
                        " where nzp_deal = " + finder.nzp_deal;
                }

                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) throw new Exception(ret.text);
                //**************************************************************************************************************
                #endregion

                var res = ClassDBUtils.OpenSQL("SELECT pref FROM " + temp_table1, conn_db);
                if (res.resultCode != 0) throw new Exception(res.resultMessage);
                
                string temp_table2 = "t2_" + finder.nzp_user;
                ExecSQL(conn_db, " drop table " + temp_table2, false);
                sql = "create temp table " + temp_table2 + " (" +
                    " phone      varchar(30)," +
                    " fio        varchar(100)," +
                    " fio_dir    varchar(160)," +
                    " nzp_deal   integer," +
                    " street     varchar(160)," +
                    " dom        varchar(20)," +
                    " kvnum      varchar(10)," +
                    " area       varchar(80)," +
                    " debt_money " + DBManager.sDecimalType + "(14,2), " +
                    " adr        varchar(255) " +
                    ")";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) throw new Exception(ret.text);
                

                if (finder.nzp_deal == 0)
                {
                    foreach (var pref in res.resultData.AsEnumerable().Select(x => Convert.ToString(x["pref"])).ToList())
                    {
                        sql = " insert into " + temp_table2 + " (phone, fio, fio_dir, nzp_deal, street, dom, kvnum, area, debt_money, adr) " +
                            " SELECT sr.phone,  kv.fio, fio_dir, de.nzp_deal, street, dom," +
                              "  trim(nkvar)||(case when trim(nkvar_n)='-' then '' else ' комн.'||trim(nkvar_n) end)," +
                              " area, debt_money, " +
                            "    trim(ulica) || (case when ndom is null then '' else  " +
                            "   ' д.'||trim(ndom)||(case when nkor='-' then '' else ' корп.'||trim(nkor) end) end)|| " +
                            "   (case when nkvar is null then '' else " +
                            "   ' кв.'||trim(nkvar)||(case when trim(nkvar_n)='-' then '' else ' комн.'||trim(nkvar_n) end) " +
                            "   end ) as adr " +
                            " from " +
                            Points.Pref + "_debt" + DBManager.tableDelimiter + "groups_operations_details gd, " +
                            Points.Pref + "_debt" + DBManager.tableDelimiter + "deal de, " +
                            pref + "_data" + DBManager.tableDelimiter + "s_area area, " +
                            pref + "_data" + DBManager.tableDelimiter + "dom d, " +
                            pref + "_data" + DBManager.tableDelimiter + "s_ulica u, " +
                            pref + "_data" + DBManager.tableDelimiter + "kvar kv " + 
                            "   LEFT OUTER JOIN  " + Points.Pref + "_debt" + DBManager.tableDelimiter + "settings_requisites sr on sr.nzp_area = kv.nzp_area " +
                            " where gd.nzp_deal   = de.nzp_deal " +
                            "   AND de.nzp_kvar   = kv.nzp_kvar " +
                            "   AND area.nzp_area = kv.nzp_area " +
                            "   AND kv.nzp_dom    = d.nzp_dom " +
                            "   AND d.nzp_ul      = u.nzp_ul " +
                            "   AND gd.nzp_group = " + finder.nzp_group;
 
                        ret = ExecSQL(conn_db, sql, true);
                        if (!ret.result) throw new Exception(ret.text);
                    }
                }
                else
                {
                    var pref = Convert.ToString(res.resultData.Rows[0]["pref"]);

                    sql = " insert into " + temp_table2 + " (phone, fio, fio_dir, nzp_deal, street, dom,kvnum, area, debt_money, adr) " +
                            " SELECT sr.phone,  kv.fio, fio_dir, de.nzp_deal, street, dom, " +
                          " trim(nkvar)||(case when trim(nkvar_n)='-' then '' else ' комн.'||trim(nkvar_n) end),area, debt_money, " +
                            "   trim(ulica) || (case when ndom is null then '' else  " +
                            "   ' д.'||trim(ndom)||(case when nkor='-' then '' else ' корп.'||trim(nkor) end) end)|| " +
                            "   (case when nkvar is null then '' else " +
                            "   ' кв.'||trim(nkvar)||(case when trim(nkvar_n)='-' then '' else ' комн.'||trim(nkvar_n) end) " +
                            "   end ) as adr " +
                            " from " +
                            Points.Pref + "_debt" + DBManager.tableDelimiter + "deal de, " +
                            pref + "_data" + DBManager.tableDelimiter + "s_area area, " +
                            pref + "_data" + DBManager.tableDelimiter + "dom d, " +
                            pref + "_data" + DBManager.tableDelimiter + "s_ulica u, " +
                            pref + "_data" + DBManager.tableDelimiter + "kvar kv " +
                            "   LEFT OUTER JOIN  " + Points.Pref + "_debt" + DBManager.tableDelimiter + "settings_requisites sr on sr.nzp_area = kv.nzp_area " +
                            " where de.nzp_kvar   = kv.nzp_kvar " +
                            "   AND area.nzp_area = kv.nzp_area " +
                            "   AND kv.nzp_dom    = d.nzp_dom " +
                            "   AND d.nzp_ul      = u.nzp_ul " +
                            "   AND de.nzp_deal   = " + finder.nzp_deal;

                    ret = ExecSQL(conn_db, sql, true);
                    if (!ret.result) throw new Exception(ret.text);
                }

                ret = ExecRead(conn_db, out reader, "SELECT * FROM " + temp_table2, true);
                if (!ret.result) throw new Exception(ret.text);
                

                if (reader != null)
                {
                    res_table = new DataTable("Q_master");
                    res_table.Load(reader, LoadOption.PreserveChanges);
                }
                
                ExecSQL(conn_db, "UPDATE " + Points.Pref + "_debt" + DBManager.tableDelimiter + "groups_operations SET nzp_status = " + s_opers_statuses.Success.GetHashCode() + " WHERE nzp_group = " + finder.nzp_group, true);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка при формировании отчета в функции GetNotificeToDebitor:" + ex.Message, MonitorLog.typelog.Error, true);

                ret = new Returns(false, ex.Message);

                if (conn_db != null)
                {
                    ExecSQL(conn_db, "UPDATE " + Points.Pref + "_debt" + DBManager.tableDelimiter + "groups_operations SET nzp_status = " + s_opers_statuses.Error.GetHashCode() + " WHERE nzp_group = " + finder.nzp_group, true);
                }
            }
            finally 
            {
                if (reader != null)
                { 
                    reader.Close();
                    reader.Dispose();
                }

                if (conn_db != null)
                {
                    conn_db.Close();
                    conn_db.Dispose();
                }
            }

            return res_table;
        }

        //соглашение вытягиваем таблицу платежей
        public DataTable GetAgreementTable(Agreement finder, out Returns ret)
        {

            #region Подключение к БД
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);//new IDbConnection(Constants.cons_Webdata);
            IDataReader reader = null;


            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("FastReport : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                return null;
            }
            #endregion

            StringBuilder sql = new StringBuilder();
            DataTable res_table = new DataTable();
            res_table.TableName = "Q_master";
            List<string> nzp_agr = new List<string>() { finder.nzp_agr.ToString() };




            try
            {
                var res = ClassDBUtils.OpenSQL(sql.ToString(), conn_db);
                foreach (var pref in res.resultData.AsEnumerable().Select(x => Convert.ToString(x["pref"])).ToList())
                {
                    sql.Append(" SELECT  ad.datemonth, ad.imcoming_balance, ad.outgoing_balance, ad.debt_money ");
                    sql.Append(" from " + Points.Pref + "_debt.deal de, " + Points.Pref + "_debt.agreement ag, " + Points.Pref + "_debt.agreement_details ad, " + Points.Pref + "_debt.settings_requisites sr, ");
                    sql.Append(pref + "_data.kvar kv, " + pref + "_data.dom d, " + pref + "_data.s_ulica ul ");
                    sql.Append("WHERE ag.nzp_deal = de.nzp_deal AND ag.nzp_agr = ad.nzp_agr AND sr.nzp_area = de.nzp_area AND ag.nzp_deal = de.nzp_deal");
                    sql.Append(" AND de.pref =" + pref + " AND ag.nzp_agr =" + finder.nzp_agr + "AND de.nzp_kvar=kv.nzp_kvar AND ul.nzp_ul= d.nzp_ul AND kv.nzp_dom=d.nzp_dom ");
                    sql.Append(" AND st.nzp_town=d.nzp_town ");
                    if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("Ошибка выборки GetNachSupp" + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);

                        ret.result = false;
                        return null;
                    }
                    if (reader != null)
                    {
                        res_table.Load(reader, LoadOption.PreserveChanges);
                    }
                    if (res.resultCode != 0)
                    {
                        MonitorLog.WriteLog("Ошибка выполнения процедуры Get_reminder_to_debtor : ", MonitorLog.typelog.Error, true);

                        return null;

                    }
                }
                return res_table;

            }

            catch (Exception ex)
            {

                ret.result = false;
                ret.text = "";
                MonitorLog.WriteLog("Ошибка при формировании отчета в функции GetAgreement: " + ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }


        }

        //соглашение вытягиваем данные о жильцах и  УК
        public DataTable GetAgreementData(Agreement finder, out Returns ret)
        {

            #region Подключение к БД
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);//new IDbConnection(Constants.cons_Webdata);
            IDataReader reader = null;


            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("FastReport : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                return null;
            }
            #endregion

            StringBuilder sql = new StringBuilder();
            DataTable res_table = new DataTable();
            res_table.TableName = "Q_master";
            List<string> nzp_agr = new List<string>() { finder.nzp_agr.ToString() };




            try
            {
                var res = ClassDBUtils.OpenSQL(sql.ToString(), conn_db);
                foreach (var pref in res.resultData.AsEnumerable().Select(x => Convert.ToString(x["pref"])).ToList())
                {
                    sql.Append(" SELECT DISTINCT	TRIM (ulica) || ( CASE 	WHEN ndom IS NULL THEN	''	ELSE ' д.' || TRIM (ndom) || (CASE	WHEN nkor = '-' THEN ''	ELSE ' корп.' || TRIM (nkor) END) END	)" +
                        " || (CASE	WHEN nkvar IS NULL THEN	 ''	ELSE ' кв.' || TRIM (nkvar) || (CASE WHEN TRIM (nkvar_n) = '-' THEN	 ''	ELSE ' комн.' || TRIM (nkvar_n) 	END	)	END	) AS adr," +
                        " 	de.debt_money, de.fio, de.serij, de.nomer, de.vid_mes, de.vid_dat,sr.fio_dir,	sr.town as uktown, st.town, sr.street,	sr.dom, de.fio, ag.agr_date, ag.agr_month_count");
                    sql.Append("from " + Points.Pref + "_debt.deal de, " + Points.Pref + "_debt.agreement ag, " + Points.Pref + "_debt.agreement_details ad, " + Points.Pref + "_debt.settings_requisites sr, ");
                    sql.Append(pref + "_data.kvar kv, " + pref + "_data.dom d, " + pref + "_data.s_ulica ul ");
                    sql.Append("WHERE ag.nzp_deal = de.nzp_deal AND ag.nzp_agr = ad.nzp_agr AND sr.nzp_area = de.nzp_area AND ag.nzp_deal = de.nzp_deal");
                    sql.Append(" AND de.pref =" + pref + " AND ag.nzp_agr =" + finder.nzp_agr + "AND de.nzp_kvar=kv.nzp_kvar AND ul.nzp_ul= d.nzp_ul AND kv.nzp_dom=d.nzp_dom ");
                    sql.Append(" AND st.nzp_town=d.nzp_town ");
                    if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("Ошибка выборки GetNachSupp" + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);

                        ret.result = false;
                        return null;
                    }
                    if (reader != null)
                    {
                        res_table.Load(reader, LoadOption.PreserveChanges);
                    }
                    if (res.resultCode != 0)
                    {
                        MonitorLog.WriteLog("Ошибка выполнения процедуры Get_reminder_to_debtor : ", MonitorLog.typelog.Error, true);

                        return null;

                    }
                }
                return res_table;

            }

            catch (Exception ex)
            {

                ret.result = false;
                ret.text = "";
                MonitorLog.WriteLog("Ошибка при формировании отчета в функции GetAgreement: " + ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }


        }

        // Исковое заявление
        public DataTable GetPetition(out Returns ret)
        {
            int group = 1;
            int user = 1;
            #region Подключение к БД
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);//new IDbConnection(Constants.cons_Webdata);
            IDataReader reader = null;
            // IDataReader reader2;

            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("FastReport : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                return null;
            }
            #endregion


            StringBuilder sql = new StringBuilder();
            DataTable DT = new DataTable();
            DataTable res_table = new DataTable();
            string temp_table = "t1_" + user;
            string temp_table2 = "t2_" + user;
            DT.TableName = "Q_master";
            List<string> nzp_group = new List<string>() { group.ToString() };

            sql.Append(" drop table " + temp_table + "; ");
            ExecSQL(conn_db, sql.ToString(), false);
            sql.Remove(0, sql.Length);
            sql.Append(" drop table " + temp_table2 + "; ");
            ExecSQL(conn_db, sql.ToString(), false);

            sql.Remove(0, sql.Length);


            sql.Append(" select pref, nzp_kvar into TEMP " + temp_table + " from  "
                       + Points.Pref + "_debt.deal where nzp_deal= " +
                       " (select nzp_deal from " + Points.Pref + "_debt.groups_operations_details god  where god.nzp_group=" + group + ");  ");
            ExecSQL(conn_db, sql.ToString(), false);

            sql.Remove(0, sql.Length);

            sql.Append("SELECT DISTINCT pref FROM " + temp_table + ";");
            // ExecSQL(conn_db, sql.ToString(), true);
            var res = ClassDBUtils.OpenSQL(sql.ToString(), conn_db);
            if (res.resultCode != 0)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetPetition : ", MonitorLog.typelog.Error, true);
                return null;
            }
            sql.Remove(0, sql.Length);
            foreach (var pref in res.resultData.AsEnumerable().Select(x => Convert.ToString(x["pref"])).ToList())
            {
                sql.Append(" select de.nzp_area, trim(fio_dir)as fio_dir, trim(street) as street, trim(dom) as dom, trim(kvnum) as kvnum, ");
                sql.Append(" trim(kv.fio) as fio, trim(ulica) as ulica, trim(ndom) as ndom, trim(nkvar) as nkvar, lawsuit_price, ");
                sql.Append(" tax, presenter , date_part('month',lawsuit_date)::char(10) as curmonth, ");
                sql.Append(" date_part('year',lawsuit_date) as curyear, floor(debt_money) as rub, substring((debt_money- floor(debt_money))::char(4) from 3 for 2) as kop ");
                sql.Append(" into " + temp_table2 + " from " + Points.Pref + "_debt.settings_requisites sr , ");
                sql.Append(" " + Points.Pref + "_debt.deal de, ");
                sql.Append(" " + pref + "_data.kvar kv , ");
                sql.Append(" " + pref + "_data.dom d, ");
                sql.Append(" " + pref + "_data.s_ulica u, ");
                sql.Append(" " + Points.Pref + "_debt.lawsuit l, " + temp_table);
                sql.Append(" where sr.nzp_area = d.nzp_area and de.nzp_kvar = kv.nzp_kvar  ");
                sql.Append(" and d.nzp_dom = kv.nzp_dom and d.nzp_ul = u.nzp_ul and de.nzp_deal=l.nzp_deal ");
                sql.Append(" AND kv.nzp_kvar IN (SELECT nzp_kvar FROM " + temp_table + " WHERE pref = '" + pref + "');  ");

                ExecSQL(conn_db, sql.ToString(), true);
            }
            if (!ExecRead(conn_db, out reader, "SELECT * FROM " + temp_table2, true).result)
            {
                MonitorLog.WriteLog("Ошибка выборки  " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                ret.result = false;
                return null;
            }
            if (reader != null)
            {
                res_table.Load(reader, LoadOption.PreserveChanges);
            }

            //замена идентификатора месяца на наименование
            foreach (DataRow row in res_table.Rows)
            {
                int month = 0;
                int.TryParse(row["curmonth"].ToString().Trim(), out month);
                switch (month)
                {
                    case 0: { row["curmonth"] = "-"; break; }
                    case 1: { row["curmonth"] = "января"; break; }
                    case 2: { row["curmonth"] = "февраля"; break; }
                    case 3: { row["curmonth"] = "марта"; break; }
                    case 4: { row["curmonth"] = "апреля"; break; }
                    case 5: { row["curmonth"] = "мая"; break; }
                    case 6: { row["curmonth"] = "июня"; break; }
                    case 7: { row["curmonth"] = "июля"; break; }
                    case 8: { row["curmonth"] = "августа"; break; }
                    case 9: { row["curmonth"] = "сентября"; break; }
                    case 10: { row["curmonth"] = "октября"; break; }
                    case 11: { row["curmonth"] = "ноября"; break; }
                    case 12: { row["curmonth"] = "декабря"; break; }
                }
            }

            if (res.resultCode != 0)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetPetition : ", MonitorLog.typelog.Error, true);
                return null;

            }
            return res_table;
        }

        // Отчет Список соглашений
        public DataTable GetListOfAgreemants(DateTime? startDate, DateTime? endDate, int user, out Returns ret)
        {
            //int group = 1;
            IDbConnection conn_db = null;
            IDataReader reader = null;
            ret = Utils.InitReturns();

            try
            {
                conn_db = GetConnection(Constants.cons_Kernel);                
                ret = OpenDb(conn_db, true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    return null;
                }


                StringBuilder sql = new StringBuilder();
                DataTable res_table = new DataTable();

                string temp_table = "t1_" + user;
                string temp_table2 = "t2_" + user;
                res_table.TableName = "Q_master";
                //List<string> nzp_group = new List<string>() { group.ToString() };

                //sql.Append(" drop table " + temp_table + "; ");
                //ExecSQL(conn_db, sql.ToString(), false);
                //sql.Remove(0, sql.Length);
                //sql.Append(" drop table " + temp_table2 + "; ");
                //ExecSQL(conn_db, sql.ToString(), false);

                //sql.Remove(0, sql.Length);


                sql.Append(" select distinct pref, nzp_kvar into TEMP " + temp_table + " from " + Points.Pref + "_debt.deal, " + Points.Pref + "_debt.groups_operations_details god where nzp_user=" + user);
                ret = ExecSQL(conn_db, sql.ToString(), true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка выборки:  " + sql, MonitorLog.typelog.Error, 20, 201, true);
                    return null;
                }
                
                var res = ClassDBUtils.OpenSQL("SELECT DISTINCT pref FROM " + temp_table + ";", conn_db);
                if (res.resultCode != 0)
                {
                    MonitorLog.WriteLog("Ошибка GetListOfAgreemants: выборка из  " + temp_table, MonitorLog.typelog.Error, true);
                    return null;
                }


                //создание temp_table2
                sql = new StringBuilder();
                sql.AppendFormat(" CREATE TEMP TABLE {0} ( ", temp_table2);
                sql.Append(" ulica VARCHAR (255), ");
                sql.Append(" ndom VARCHAR (255), ");
                sql.Append(" nkvar VARCHAR (255), ");
                sql.Append(" fio VARCHAR (255), ");
                sql.Append(" datemonth VARCHAR (10), ");
                sql.Append(" debt_money NUMERIC (14, 2), ");
                sql.Append(" imcoming_balance NUMERIC (14, 2), ");
                sql.Append(" outgoing_balance NUMERIC (14, 2) ");
                sql.Append(" ); ");
                ret = DBManager.ExecSQL(conn_db, sql.ToString(), true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка создания временной таблицы:  " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    return null;
                }


                foreach (var pref in res.resultData.AsEnumerable().Select(x => Convert.ToString(x["pref"])).ToList())
                {
                    sql = new StringBuilder();
                    sql.AppendFormat("INSERT into {0}", temp_table2);
                    sql.Append(" select 'ул. '||trim(ulica) as ulica ,'д. '||trim(ndom) as ndom,'кв. '||trim(nkvar) as nkvar ,trim(kv.fio) as fio, ");
                    sql.Append(" ad.datemonth::char(10),ad.debt_money,ad.imcoming_balance,ad.outgoing_balance ");
                    sql.Append(" from " + Points.Pref + "_debt.deal de, ");
                    sql.Append(" " + pref + "_data.s_ulica u, ");
                    sql.Append(" " + pref + "_data.kvar kv ,  ");
                    sql.Append(" " + pref + "_data.dom d, ");
                    sql.Append(" " + Points.Pref + "_debt.agreement a, ");
                    sql.Append(" " + Points.Pref + "_debt.agreement_details ad ");
                    sql.Append(" where kv.nzp_dom = d.nzp_dom  ");
                    sql.Append(" and d.nzp_ul = u.nzp_ul ");
                    sql.Append(" and de.nzp_deal=a.nzp_deal ");
                    sql.Append(" and a.nzp_agr=ad.nzp_agr ");
                    sql.Append(" and de.nzp_kvar=kv.nzp_kvar ");
                    sql.Append(" AND kv.nzp_kvar IN (SELECT nzp_kvar FROM " + temp_table + " WHERE pref = '" + pref + "')  ");
                    sql.Append(" and ad.datemonth>'" + startDate + "'  ");
                    sql.Append(" and ad.datemonth<'" + endDate + "'  ");

                    ret = ExecSQL(conn_db, sql.ToString(), true);
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("Ошибка:  " + sql, MonitorLog.typelog.Error, 20, 201, true);
                        return null;
                    }
                }


                sql = new StringBuilder();
                sql.Append(" insert into " + temp_table2 + " (ulica, fio,debt_money,imcoming_balance,outgoing_balance) ");
                sql.Append(" select 'Итого шт.:',count(fio)::char(10) as fio, sum(debt_money)as debt_money, ");
                sql.Append(" sum(imcoming_balance)as imcoming_balance,sum(outgoing_balance)as outgoing_balance ");
                sql.Append(" from " + temp_table2);
                ret = ExecSQL(conn_db, sql.ToString(), true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка выборки  " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    return null;
                }


                if (!ExecRead(conn_db, out reader, "SELECT * FROM " + temp_table2, true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки  " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return null;
                }
                if (reader != null)
                {
                    res_table.Load(reader, LoadOption.PreserveChanges);
                }

                if (res.resultCode != 0)
                {
                    MonitorLog.WriteLog("Ошибка выполнения процедуры GetListOfAgreemants : ", MonitorLog.typelog.Error, true);
                    return null;

                }
                return res_table;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteException("Ошибка в процедуре GetListOfAgreemants", ex);
                ret.result = false;
                return null;
            }
            finally
            {
                if (conn_db != null) conn_db.Close();
                if (reader != null)
                {
                    reader.Close();
                    reader.Dispose();
                }

            }           
        }
    }
}

