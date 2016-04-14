using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.DataBase;
using System.IO;
using System.Threading;
using System.Data.OleDb;
using STCLINE.KP50.Utility;


namespace STCLINE.KP50.DataBase
{
    public partial class DbCounter : DbCounterKernel
    {
        private CounterValDbf GetCounterValDbfFromReader(MyDataReader reader, string reason)
        {
            CounterValDbf cv = new CounterValDbf();
            if (reader["adr"] != DBNull.Value) cv.adr = Convert.ToString(reader["adr"]).Trim();
            if (reader["nuk"] != DBNull.Value) cv.nuk = Convert.ToString(reader["nuk"]).Trim();
            if (reader["predpr"] != DBNull.Value) cv.predpr = Convert.ToString(reader["predpr"]).Trim();
            if (reader["geu"] != DBNull.Value) cv.geu = Convert.ToString(reader["geu"]).Trim();
            if (reader["lc"] != DBNull.Value) cv.lc = Convert.ToString(reader["lc"]).Trim();
            if (reader["usl"] != DBNull.Value) cv.usl = Convert.ToString(reader["usl"]).Trim();
            if (reader["ns"] != DBNull.Value) cv.ns = Convert.ToString(reader["ns"]).Trim();
            if (reader["dold"] != DBNull.Value) cv.dold = Convert.ToString(reader["dold"]).Trim();
            if (reader["zold"] != DBNull.Value) cv.zold = Convert.ToDecimal(reader["zold"]);
            if (reader["dnew"] != DBNull.Value) cv.dnew = Convert.ToString(reader["dnew"]).Trim();
            if (reader["znew"] != DBNull.Value) cv.znew = Convert.ToDecimal(reader["znew"]);

            if (reader["nzp_kvar"] != DBNull.Value) cv.nzp_kvar = Convert.ToInt32(reader["nzp_kvar"]);
            if (reader["nzp_counter"] != DBNull.Value) cv.nzp_counter = Convert.ToInt32(reader["nzp_counter"]);

            if (cv.nzp_kvar < 1) cv.reason = "Не найден лицевой счет";
            else if (cv.nzp_counter < 1) cv.reason = "Не найден прибор учета (но лицевой счет найден)";
            else if (reason != "") cv.reason = reason;

            return cv;
        }

        private CounterValDbf GetCounterValDbfFromReader(IDataReader reader, string reason)
        {
            CounterValDbf cv = new CounterValDbf();
            if (reader["adr"] != DBNull.Value) cv.adr = Convert.ToString(reader["adr"]).Trim();
            if (reader["nuk"] != DBNull.Value) cv.nuk = Convert.ToString(reader["nuk"]).Trim();
            if (reader["predpr"] != DBNull.Value) cv.predpr = Convert.ToString(reader["predpr"]).Trim();
            if (reader["geu"] != DBNull.Value) cv.geu = Convert.ToString(reader["geu"]).Trim();
            if (reader["lc"] != DBNull.Value) cv.lc = Convert.ToString(reader["lc"]).Trim();
            if (reader["usl"] != DBNull.Value) cv.usl = Convert.ToString(reader["usl"]).Trim();
            if (reader["ns"] != DBNull.Value) cv.ns = Convert.ToString(reader["ns"]).Trim();
            if (reader["dold"] != DBNull.Value) cv.dold = Convert.ToString(reader["dold"]).Trim();
            if (reader["zold"] != DBNull.Value) cv.zold = Convert.ToDecimal(reader["zold"]);
            if (reader["dnew"] != DBNull.Value) cv.dnew = Convert.ToString(reader["dnew"]).Trim();
            if (reader["znew"] != DBNull.Value) cv.znew = Convert.ToDecimal(reader["znew"]);

            if (reader["nzp_kvar"] != DBNull.Value) cv.nzp_kvar = Convert.ToInt32(reader["nzp_kvar"]);
            if (reader["nzp_counter"] != DBNull.Value) cv.nzp_counter = Convert.ToInt32(reader["nzp_counter"]);

            if (cv.nzp_kvar < 1) cv.reason = "Не найден лицевой счет";
            else if (cv.nzp_counter < 1) cv.reason = "Не найден прибор учета (но лицевой счет найден)";
            else if (reason != "") cv.reason = reason;

            return cv;
        }

        public delegate Returns SaveUploadedCounterReadingsDelegate(Finder finder);

        public void ACallback(IAsyncResult ar)
        {
            // Because you passed your original delegate in the asyncState parameter
            // of the Begin call, you can get it back here to complete the call.
            //SaveUploadedCounterReadingsDelegate dlgt = (SaveUploadedCounterReadingsDelegate) ar.AsyncState;

            // Complete the call.
            //Returns ret = dlgt.EndInvoke(ar);

            //MonitorLog.WriteLog("Загрузка завершена", MonitorLog.typelog.Info, true);
        }

        public void DemoEndInvoke(Finder finder)
        {
            SaveUploadedCounterReadingsDelegate dlgt = new SaveUploadedCounterReadingsDelegate(this.SaveUploadedCounterReadings);

            //object o = new object();
            AsyncCallback cb = new AsyncCallback(ACallback);
            IAsyncResult ar = dlgt.BeginInvoke(finder, cb, dlgt);
        }

        //        public Returns SaveUploadedCounterReadings(Finder finder)
        //        {
        //            if (finder.nzp_user < 1) return new Returns(false, "Пользователь не задан");

        //            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
        //            Returns ret = OpenDb(conn_web, true);
        //            if (!ret.result) return ret;

        //            string tXX_uplcnt = "t" + Convert.ToString(finder.nzp_user) + "_uplcnt";
        //#if PG
        //            string tXX_uplcnt_full = conn_web.Database + ".public." + tXX_uplcnt;
        //#else
        //            string tXX_uplcnt_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXX_uplcnt;
        //#endif
        //            if (!TableInWebCashe(conn_web, tXX_uplcnt))
        //            {
        //                ret.result = false;
        //                ret.text = "Необходимо сначала загрузить электронный файл с показаниями";
        //                ret.tag = -1;
        //                conn_web.Close();
        //                return ret;
        //            }

        //            string sql;
        //            if (!isTableHasColumn(conn_web, tXX_uplcnt, "nzp_kvar"))
        //            {
        //                sql = "alter table " + tXX_uplcnt + " add nzp_kvar integer, add pref char(10), add nzp_counter integer, add subpkod char(11) ,add nzp_serv integer";
        //                ret = ExecSQL(conn_web, sql, true);
        //                if (!ret.result)
        //                {
        //                    conn_web.Close();
        //                    return ret;
        //                }
        //            }
        //            if (!isTableHasColumn(conn_web, tXX_uplcnt, "nzp_serv"))
        //            {
        //                sql = "alter table " + tXX_uplcnt + " add nzp_serv integer";
        //                ret = ExecSQL(conn_web, sql, true);
        //                if (!ret.result)
        //                {
        //                    conn_web.Close();
        //                    return ret;
        //                }
        //            }
        //            sql = "update " + tXX_uplcnt + " set subpkod = lpad(trim(predpr),3,'0') || lpad(trim(geu),2,'0') || lpad(trim(lc),5,'0') || lpad(trim(llc),1,'0')";
        //            ret = ExecSQL(conn_web, sql, true);
        //            if (!ret.result)
        //            {
        //                conn_web.Close();
        //                return ret;
        //            }

        //            string connectionString = Points.GetConnByPref(Points.Pref);
        //            IDbConnection conn_db = GetConnection(connectionString);
        //            ret = OpenDb(conn_db, true);
        //            if (!ret.result)
        //            {
        //                conn_web.Close();
        //                return ret;
        //            }

        //            IDataReader reader = null;
        //            IDataReader reader2 = null;

        //            int numVsego = 0, numNotFoundedPu = 0, numNotFoundedLs = 0, numNotUploadedOtherReason = 0;
        //            List<CounterValDbf> listNotUploadedReadings = new List<CounterValDbf>(); // список незагруженных показаний
        //            List<CounterValDbf> listUploadedReadingsWithWarning = new List<CounterValDbf>(); // список показаний, по которым есть предупреждения

        //            //добавим информацию о протоколе загрузки в мои файлы
        //            ExcelRepClient dbRep = new ExcelRepClient();
        //            ret = dbRep.AddMyFile(new ExcelUtility()
        //            {
        //                nzp_user = finder.nzp_user,
        //                status = ExcelUtility.Statuses.InProcess,
        //                rep_name = "Протокол загрузки показаний приборов учета от " + DateTime.Now.ToShortDateString() + " (" + finder.dopFind[0] + ")"
        //            });
        //            if (!ret.result)
        //            {
        //                conn_web.Close();
        //                return ret;
        //            }

        //            int nzpExc = ret.tag;

        //            try
        //            {
        //                ExecSQL(conn_db, "drop table tmpkvar", false);
        //#if PG
        //                sql = "select nzp_kvar, pref, substr(pkod||'',1,11) subkod into unlogged tmpkvar from " + Points.Pref + "_data.kvar ";
        //#else
        //                sql = "select nzp_kvar, pref, substr(pkod,1,11) subkod from " + Points.Pref + "_data:kvar into temp tmpkvar";
        //#endif
        //                ret = ExecSQL(conn_db, sql, true);
        //                if (!ret.result)
        //                {
        //                    return ret;
        //                }
        //                ExecSQL(conn_db, "create index ix_tmpkvar on tmpkvar(subkod)", true);
        //#if PG
        //                ExecSQL(conn_db, "analyze tmpkvar", true);
        //#else
        //                ExecSQL(conn_db, "update statistics for table tmpkvar", true);
        //#endif
        //                sql = "update " + tXX_uplcnt_full + " set nzp_kvar = (select nzp_kvar from tmpkvar where subkod = subpkod), pref = (select pref from tmpkvar where subkod = subpkod)";
        //                ret = ExecSQL(conn_db, sql, true);

        //                ExecSQL(conn_db, "drop table tmpkvar", false);
        //#if PG
        //                sql = "select distinct pref from " + tXX_uplcnt + " where pref is not null";
        //#else
        //                sql = "select unique pref from " + tXX_uplcnt + " where pref is not null";
        //#endif
        //                ret = ExecRead(conn_web, out reader, sql, true);
        //                if (!ret.result)
        //                {
        //                    return ret;
        //                }
        //#warning TODO PG: ошибка в ф-ции PREFIX.get_counter в бд smr34_data;

        //                while (reader.Read())
        //                {
        //                    if (reader["pref"] == DBNull.Value) continue;
        //#if PG
        //                    sql = "update " + tXX_uplcnt_full + " set nzp_counter = " + reader["pref"].ToString().Trim() + "_data.get_counter(nzp_kvar, usl, NS, dnew||'')" +
        //                                            " where pref = " + Utils.EStrNull(reader["pref"].ToString().Trim());
        //#else
        //                    sql = "update " + tXX_uplcnt_full + " set nzp_counter = " + reader["pref"].ToString().Trim() + "_data:get_counter(nzp_kvar, usl, NS, dnew||'')" +
        //                        " where pref = " + Utils.EStrNull(reader["pref"].ToString().Trim());
        //#endif
        //                    ret = ExecSQL(conn_db, sql, true);
        //                    if (!ret.result)
        //                    {
        //                        CloseReader(ref reader);
        //                        return ret;
        //                    }
        //                }
        //                CloseReader(ref reader);

        //                #region Дооперделение не найденных приборов учета

        //#if PG
        //                sql = "update " + tXX_uplcnt_full + " set nzp_serv = (select nzp_serv from " +
        //                                   Points.Pref + "_data.services_smr serv_smr where nzp_area=(select nzp_area from " + Points.Pref + "_data.kvar k where "
        //                                   + tXX_uplcnt_full + ".nzp_kvar=k.nzp_kvar) and " + tXX_uplcnt_full + ".usl=serv_smr.kod_usl )";
        //                ret = ExecSQL(conn_web, sql, true);
        //                if (!ret.result) throw new Exception(ret.text);
        //#else
        //                  sql = "update " + tXX_uplcnt_full + " set nzp_serv = (select nzp_serv from " +
        //                    Points.Pref + "_data:services_smr serv_smr where nzp_area=(select nzp_area from " + Points.Pref + "_data:kvar k where "
        //                    + tXX_uplcnt_full + ".nzp_kvar=k.nzp_kvar) and " + tXX_uplcnt_full + ".usl=serv_smr.kod_usl )";
        //                ret = ExecSQL(conn_web, sql, true);
        //                if (!ret.result) throw new Exception(ret.text);
        //#endif
        //                sql = "drop table s_temp";
        //                ret = ExecSQL(conn_web, sql, false);
        //#if PG
        //                sql = "  create unlogged table  s_temp(" +
        //                                " nzp_kvar integer," +
        //                                " nzp_serv integer," +
        //                                " nzp_counter integer ) ";
        //                ret = ExecSQL(conn_web, sql, true);
        //#else
        //                 sql = "  create temp table  s_temp(" +
        //                 " nzp_kvar integer," +
        //                 " nzp_serv integer," +
        //                 " nzp_counter integer ) with no log";
        //                ret = ExecSQL(conn_web, sql, true);
        //#endif
        //                if (!ret.result) throw new Exception(ret.text);
        //                foreach (var pr in Points.PointList)
        //                {

        //#if PG
        //                    sql = " insert into s_temp (nzp_kvar, nzp_serv, nzp_counter) " +
        //                                            " select t.nzp_kvar, s.nzp_serv, max(s.nzp_counter) " +
        //                                            " from " + pr.pref + "_data.counters_spis s, " + tXX_uplcnt_full + " t where s.nzp_serv=t.nzp_serv " +
        //                                            " and t.nzp_kvar=nzp and MDY(date_part('month', now())::int, 1,  date_part('year', now())::int) >=dat_when and  " +
        //                                            " MDY(date_part('month', now())::int, 1,  date_part('year', now())::int) <=coalesce(dat_close,mdy(1,1,3000)) " +
        //                                            " and is_actual<>100 and nzp_type=3 " +
        //                                            " and t.nzp_counter = 0 " +
        //                                            " group by 1,2 having count(*) = 1 ";
        //#else
        //                    sql = " insert into s_temp (nzp_kvar, nzp_serv, nzp_counter) " +
        //                        " select t.nzp_kvar, s.nzp_serv, max(s.nzp_counter) " +
        //                        " from " + pr.pref + "_data:counters_spis s, " + tXX_uplcnt_full + " t where s.nzp_serv=t.nzp_serv " +
        //                        " and t.nzp_kvar=nzp and MDY(MONTH(TODAY), 1, YEAR(TODAY)) >=dat_when and  " +
        //                        " MDY(MONTH(TODAY), 1, YEAR(TODAY)) <=nvl(dat_close,mdy(1,1,3000)) " +
        //                        " and is_actual<>100 and nzp_type=3 " +
        //                        " and t.nzp_counter = 0 " +
        //                        " group by 1,2 having count(*) = 1 ";
        //#endif


        //                    ret = ExecSQL(conn_web, sql, true);
        //                    if (!ret.result) throw new Exception(ret.text);

        //                }
        //                sql = "update " + tXX_uplcnt_full +
        //                    " set nzp_counter = (select nzp_counter from s_temp t where t.nzp_kvar = " + tXX_uplcnt_full + ".nzp_kvar and t.nzp_serv = " + tXX_uplcnt_full + ".nzp_serv)" +
        //                    " where " + tXX_uplcnt_full + ".nzp_counter = 0";
        //                ret = ExecSQL(conn_web, sql, true);
        //                if (!ret.result) throw new Exception(ret.text);

        //                sql = "update " + tXX_uplcnt_full + " set nzp_counter=0 where nzp_counter is null";
        //                ret = ExecSQL(conn_web, sql, true);
        //                if (!ret.result) throw new Exception(ret.text);
        //                #endregion

        //                #region определение количества показаний всего, кол-во не найденных ПУ, ЛС и т.п.
        //                sql = "select count(*) as s1, sum(case when nzp_counter is null or nzp_counter < 1 then 1 else 0 end) as s2" +
        //                    ", sum(case when nzp_kvar is null or nzp_kvar < 1 then 1 else 0 end) as s3" +
        //                    " from " + tXX_uplcnt;
        //                ret = ExecRead(conn_web, out reader, sql, true);
        //                if (!ret.result) return ret;
        //                if (reader.Read())
        //                {
        //                    if (reader["s1"] != DBNull.Value) numVsego = Convert.ToInt32(reader["s1"]);
        //                    if (reader["s2"] != DBNull.Value) numNotFoundedPu = Convert.ToInt32(reader["s2"]);
        //                    if (reader["s3"] != DBNull.Value) numNotFoundedLs = Convert.ToInt32(reader["s3"]);
        //                }
        //                reader.Close();
        //                reader.Dispose();
        //                #endregion

        //                #region получение списка незагруженных показаний
        //                sql = "select adr, nuk, predpr, geu, lc, usl, ns, dold, zold, dnew, znew, nzp_kvar, pref, nzp_counter from " + tXX_uplcnt +
        //                    " where nzp_kvar is null or nzp_kvar < 1 or nzp_counter is null or nzp_counter < 1" +
        //                    " order by nuk, adr";
        //                ret = ExecRead(conn_web, out reader, sql, true);
        //                if (!ret.result) return ret;
        //                while (reader.Read())
        //                {
        //                    CounterValDbf cv = GetCounterValDbfFromReader(reader, "");
        //                    listNotUploadedReadings.Add(cv);
        //                }
        //                reader.Close();
        //                reader.Dispose();
        //                #endregion

        //                //public Returns AddToPoolThread(int nzp_user, string parametrs, int typeR, string repName, ref string time, string comment)

        //                //определим количество обрабатываемых показаний
        //                sql = "select count(*) as num from " + tXX_uplcnt +
        //                    " where pref is not null and trim(pref) <> '' and nzp_kvar is not null and nzp_kvar > 0 and nzp_counter is not null and nzp_counter > 0";
        //                object obj = ExecScalar(conn_web, sql, out ret, true);
        //                if (!ret.result) throw new Exception(ret.text);
        //                int numRecords = Convert.ToInt32(obj);

        //                //запрос обрабатываемых показаний
        //                sql = "select adr, nuk, predpr, geu, lc, usl, ns, dold, zold, dnew, znew, nzp_kvar, pref, nzp_counter from " + tXX_uplcnt +
        //                    " where pref is not null and trim(pref) <> '' and nzp_kvar is not null and nzp_kvar > 0 and nzp_counter is not null and nzp_counter > 0" +
        //                    " order by pref";
        //                ret = ExecRead(conn_web, out reader, sql, true);
        //                if (!ret.result) throw new Exception(ret.text);

        //                CounterVal val;
        //                DateTime date;
        //                string counters_vals = "";
        //                string pref = "", prev_pref = "";
        //                int nzpUser = 0;
        //                int i = 0;

        //                while (reader.Read())
        //                {
        //                    i++;
        //                    pref = Convert.ToString(reader["pref"]).Trim();

        //                    if (pref != prev_pref)
        //                    {
        //                        #region определение локального пользователя
        //                        DbWorkUser db = new DbWorkUser();
        //                        finder.pref = pref;
        //                        nzpUser = db.GetLocalUser(conn_db, finder, out ret);
        //                        db.Close();
        //                        if (!ret.result) return ret;
        //                        finder.nzp_user_main = nzpUser;
        //                        #endregion
        //#if PG
        //                        counters_vals = pref + "_charge_" + (Points.CalcMonth.year_ % 100).ToString("00") + ".counters_vals";
        //#else
        //                        counters_vals = pref + "_charge_" + (Points.CalcMonth.year_ % 100).ToString("00") + ":counters_vals";
        //#endif
        //                        prev_pref = pref;
        //                    }

        //                    val = new CounterVal();

        //                    val.nzp_user = finder.nzp_user;
        //                    val.pref = pref;
        //                    val.year_ = Points.CalcMonth.year_;
        //                    val.month_ = Points.CalcMonth.month_;
        //                    val.ist = (int)CounterVal.Ist.File;
        //                    val.nzp_user_main = finder.nzp_user_main;

        //                    if (reader["dnew"] != DBNull.Value)
        //                    {
        //                        if (!DateTime.TryParseExact(Convert.ToString(reader["dnew"]), "yyyyMMdd", new CultureInfo("ru-RU"), DateTimeStyles.None, out date))
        //                        {
        //                            listNotUploadedReadings.Add(GetCounterValDbfFromReader(reader, "Некорректная дата показания. Ожидалась дата в формате ГГГГММДД."));
        //                            numNotUploadedOtherReason++;
        //                            continue;
        //                        }
        //                    }
        //                    else
        //                    {
        //                        listNotUploadedReadings.Add(GetCounterValDbfFromReader(reader, "Не задана дата показания. Ожидалась дата в формате ГГГГММДД."));
        //                        numNotUploadedOtherReason++;
        //                        continue;
        //                    }

        //                    if (date <= new DateTime(Points.CalcMonth.year_, Points.CalcMonth.month_, 1))
        //                    {
        //                        listUploadedReadingsWithWarning.Add(GetCounterValDbfFromReader(reader, "Текущий расчетный месяц " + Points.CalcMonth.name + ". Показание будет учтено как показание за прошлые расчетные месяцы"));
        //                    }
        //                    else if (date > new DateTime(Points.CalcMonth.year_, Points.CalcMonth.month_, 1).AddMonths(1))
        //                    {
        //                        listUploadedReadingsWithWarning.Add(GetCounterValDbfFromReader(reader, "Текущий расчетный месяц " + Points.CalcMonth.name + ". Показание будет учтено как показание за будущие расчетные месяцы"));
        //                    }

        //                    DateTime monthPokaz = date.Day == 1 ? date.AddMonths(-1) : new DateTime(date.Year, date.Month, 1);

        //                    val.dat_uchet = monthPokaz.AddMonths(1).ToShortDateString();    // меняем дату показания на 1 число следующего месяца
        //                    //например, дата показания 23.09.2012, будет 01.10.2012.
        //                    if (reader["znew"] != DBNull.Value) val.val_cnt = Convert.ToDecimal(reader["znew"]);
        //                    if (reader["nzp_kvar"] != DBNull.Value) val.nzp_kvar = Convert.ToInt32(reader["nzp_kvar"]);
        //                    if (reader["nzp_counter"] != DBNull.Value) val.nzp_counter = Convert.ToInt32(reader["nzp_counter"]);

        //                    sql = "select nzp_cv, iscalc from " + counters_vals +
        //                        " where ist = " + val.ist +
        //                        " and nzp_counter = " + val.nzp_counter +
        //                        " and month_ = " + Points.CalcMonth.month_ +
        //                        " and dat_uchet between mdy(" + monthPokaz.Month + ",2," + monthPokaz.Year + ") and mdy(" + monthPokaz.AddMonths(1).Month + ",1," + monthPokaz.AddMonths(1).Year + ")" +
        //                        " and iscalc = 0";

        //                    ret = ExecRead(conn_web, out reader2, sql, true);
        //                    if (!ret.result) throw new Exception(ret.text);

        //                    if (reader2.Read())
        //                    {
        //                        if (reader2["nzp_cv"] != DBNull.Value) val.nzp_cv = Convert.ToInt32(reader2["nzp_cv"]);
        //                    }
        //                    reader2.Close();

        //                    if (val.nzp_cv < 1) // добавляем, если показание новое, или имеющееся за данный период показание учтено в расчетах
        //                    {
        //                        sql = "Insert into " + counters_vals + "(nzp_cv, nzp, nzp_type, nzp_counter, month_, dat_uchet, val_cnt, nzp_user, dat_when, ist, is_new)" +
        //#if PG
        //                            " values (default," + val.nzp_kvar +
        //#else
        //                            " values (0," + val.nzp_kvar +
        //#endif
        //                            ", " + (int)CounterKinds.Kvar +
        //                            ", " + val.nzp_counter +
        //                            ", " + Points.CalcMonth.month_ +
        //                            ", " + Utils.EStrNull(val.dat_uchet) +
        //                            ", " + val.val_cnt +
        //                            ", " + nzpUser +
        //                            ", " + Utils.EStrNull(date.ToShortDateString()) +
        //                            ", " + val.ist +
        //                            ", 1)";
        //                        ret = ExecSQL(conn_db, sql, true);
        //                        if (!ret.result) throw new Exception(ret.text);
        //                        val.nzp_cv = GetSerialValue(conn_db, null);
        //                    }
        //                    else
        //                    {
        //                        sql = "Update " + counters_vals +
        //                            " set val_cnt = " + val.val_cnt +
        //                            ", dat_uchet = " + Utils.EStrNull(val.dat_uchet) +
        //                            ", nzp_user = " + nzpUser +
        //                            ", dat_when = " + Utils.EStrNull(date.ToShortDateString()) +
        //                            ", ist = " + val.ist +
        //                            ", is_new = 1" +
        //                            " where nzp_cv = " + val.nzp_cv;
        //                        ret = ExecSQL(conn_db, sql, true);
        //                        if (!ret.result) throw new Exception(ret.text);
        //                    }


        //                    if (Points.SaveCounterReadingsToRealBank)
        //                    {
        //                        ret = CopyCounterReadingToRealBank(conn_db, null, val);
        //                        if (!ret.result) throw new Exception(ret.text);
        //                        else if (ret.result && ret.tag < 0)
        //                            listUploadedReadingsWithWarning.Add(GetCounterValDbfFromReader(reader, ret.text));
        //                    }

        //                    if (i % 50 == 0)
        //                        dbRep.SetMyFileProgress(new ExcelUtility() { nzp_exc = nzpExc, progress = ((decimal)i) / numRecords });
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                ret.result = false;
        //                ret.text = ex.Message;
        //                MonitorLog.WriteLog("Ошибка в функции SaveUploadedCounterReadings:\n" + (ex.Message != "" ? ex.Message : ret.text), MonitorLog.typelog.Error, true);
        //            }
        //            finally
        //            {
        //                dbRep.Close();

        //                CloseReader(ref reader);
        //                CloseReader(ref reader2);

        //                conn_db.Close();
        //                conn_web.Close();
        //            }
        //            if (ret.result)
        //            {
        //                ret.text = "Всего загружено показаний: " + (numVsego - numNotFoundedPu - numNotUploadedOtherReason).ToString() + " из " + numVsego +
        //                    ".<br> Не найдено приборов учета: " + numNotFoundedPu +
        //                    ", в том числе не найдено лицевых счетов: " + numNotFoundedLs +
        //                    ".<br> Не загружено по другим причинам: " + numNotUploadedOtherReason + ".";
        //                ret.tag = -1;
        //            }

        //            //сохранение результатов в файл
        //            int k = 0;
        //            string filename = "prot_pu_upload_u" + finder.nzp_user + "_";
        //            while (System.IO.File.Exists(Constants.ExcelDir + filename + k )) k++;
        //            filename = filename + k + ".txt";
        //            using (System.IO.StreamWriter file = new System.IO.StreamWriter(Constants.ExcelDir + filename, true, Encoding.GetEncoding(1251)))
        //            {
        //                file.WriteLine("Протокол загрузки показаний приборов учета от " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"));
        //                file.WriteLine("Наименование файла: " + finder.dopFind[0]);
        //                file.WriteLine("Пользователь: " + finder.webLogin + " (код " + finder.nzp_user + ")");
        //                file.WriteLine("Всего загружено показаний: " + (numVsego - numNotFoundedPu - numNotUploadedOtherReason).ToString() + " из " + numVsego + ".");
        //                if (numNotFoundedPu > 0) file.WriteLine("Не найдено приборов учета: " + numNotFoundedPu + ", в том числе не найдено лицевых счетов: " + numNotFoundedLs);
        //                if (numNotUploadedOtherReason > 0) file.WriteLine("Не загружено по другим причинам: " + numNotUploadedOtherReason + ".");

        //                if (listNotUploadedReadings.Count > 0)
        //                {
        //                    file.WriteLine();
        //                    file.WriteLine("Список незагруженных показаний");
        //                    file.WriteLine();
        //                    file.WriteLine(CounterValDbf.GetHeaderString());
        //                    foreach (CounterValDbf val in listNotUploadedReadings)
        //                    {
        //                        file.WriteLine(val.ToString());
        //                    }
        //                }

        //                if (listUploadedReadingsWithWarning.Count > 0)
        //                {
        //                    file.WriteLine();
        //                    file.WriteLine("Список показаний, загруженных с предупреждениями");
        //                    file.WriteLine();
        //                    file.WriteLine(CounterValDbf.GetHeaderString());
        //                    foreach (CounterValDbf val in listUploadedReadingsWithWarning)
        //                    {
        //                        file.WriteLine(val.ToString());
        //                    }
        //                }
        //            }

        //            ExcelRepClient dbRep2 = new ExcelRepClient();
        //            dbRep2.SetMyFileProgress(new ExcelUtility() { nzp_exc = nzpExc, progress = 1 });
        //            dbRep2.SetMyFileState(new ExcelUtility() { nzp_exc = nzpExc, status = ExcelUtility.Statuses.Success, exc_path = filename });
        //            dbRep2.Close();

        //            return ret;
        //        }

        public Returns SaveUploadedCounterReadings(Finder finder)
        {
            if (finder.nzp_user < 1) return new Returns(false, "Пользователь не задан");

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            Returns ret = OpenDb(conn_web, true);
            if (!ret.result) return ret;

            string tXX_uplcnt = "t" + Convert.ToString(finder.nzp_user) + "_uplcnt";
#if PG
            string tXX_uplcnt_full = conn_web.Database + ".public." + tXX_uplcnt;
#else
            string tXX_uplcnt_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXX_uplcnt;
#endif
            if (!TableInWebCashe(conn_web, tXX_uplcnt))
            {
                ret.result = false;
                ret.text = "Необходимо сначала загрузить электронный файл с показаниями";
                ret.tag = -1;
                conn_web.Close();
                return ret;
            }

            string sql;

            string connectionString = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                conn_web.Close();
                return ret;
            }

            MyDataReader reader = null;
            MyDataReader reader2 = null;

            int numVsego = 0, numNotFoundedPu = 0, numNotFoundedLs = 0, numNotUploadedOtherReason = 0;
            List<CounterValDbf> listNotUploadedReadings = new List<CounterValDbf>(); // список незагруженных показаний
            List<CounterValDbf> listUploadedReadingsWithWarning = new List<CounterValDbf>(); // список показаний, по которым есть предупреждения

            //добавим информацию о протоколе загрузки в мои файлы

            DBMyFiles dbRep = new DBMyFiles();

            ret = dbRep.AddFile(new ExcelUtility()
            {
                nzp_user = finder.nzp_user,
                status = ExcelUtility.Statuses.InProcess,
                rep_name = "Протокол загрузки показаний приборов учета от " + DateTime.Now.ToShortDateString() + " (" + finder.dopFind[0] + ")"
            });
            if (!ret.result)
            {
                conn_web.Close();
                return ret;
            }

            int nzpExc = ret.tag;

            try
            {
                #region определение количества показаний всего, кол-во не найденных ПУ, ЛС и т.п.
                sql = "select count(*) as s1, sum(case when nzp_counter is null or nzp_counter < 1 then 1 else 0 end) as s2" +
                    ", sum(case when nzp_kvar is null or nzp_kvar < 1 then 1 else 0 end) as s3" +
                    " from " + tXX_uplcnt;
                ret = ExecRead(conn_web, out reader, sql, true);
                if (!ret.result) return ret;
                if (reader.Read())
                {
                    if (reader["s1"] != DBNull.Value) numVsego = Convert.ToInt32(reader["s1"]);
                    if (reader["s2"] != DBNull.Value) numNotFoundedPu = Convert.ToInt32(reader["s2"]);
                    if (reader["s3"] != DBNull.Value) numNotFoundedLs = Convert.ToInt32(reader["s3"]);
                }
                reader.Close();
                reader = null;
                #endregion

                #region получение списка незагруженных показаний
                sql = "select adr, nuk, predpr, geu, lc, usl, ns, dold, zold, dnew, znew, nzp_kvar, pref, nzp_counter from " + tXX_uplcnt +
                    " where nzp_kvar is null or nzp_kvar < 1 or nzp_counter is null or nzp_counter < 1" +
                    " order by nuk, adr";
                ret = ExecRead(conn_web, out reader, sql, true);
                if (!ret.result) return ret;
                while (reader.Read())
                {
                    CounterValDbf cv = GetCounterValDbfFromReader(reader, "");
                    listNotUploadedReadings.Add(cv);
                }
                reader.Close();
                reader = null;
                #endregion

                //public Returns AddToPoolThread(int nzp_user, string parametrs, int typeR, string repName, ref string time, string comment)

                //определим количество обрабатываемых показаний
                sql = "select count(*) as num from " + tXX_uplcnt +
                    " where pref is not null and trim(pref) <> '' and nzp_kvar is not null and nzp_kvar > 0 and nzp_counter is not null and nzp_counter > 0";
                object obj = ExecScalar(conn_web, sql, out ret, true);
                if (!ret.result) throw new Exception(ret.text);
                int numRecords = Convert.ToInt32(obj);

                //запрос обрабатываемых показаний
                ExecSQL(conn_db, "drop table tmp_cnt", false);

                sql = "select adr, nuk, predpr, geu, lc, usl, ns, dold, zold, dnew, znew, nzp_kvar, pref, nzp_counter from " + tXX_uplcnt +
                    " where pref is not null and trim(pref) <> '' and nzp_kvar is not null and nzp_kvar > 0 and nzp_counter is not null and nzp_counter > 0" +
                    " into temp tmp_cnt with no log";
                ret = ExecSQL(conn_web, sql, true);
                if (!ret.result) throw new Exception(ret.text);

                sql = "select adr, nuk, predpr, geu, lc, usl, ns, dold, zold, dnew, znew, nzp_kvar, pref, nzp_counter from tmp_cnt order by pref";
                ret = ExecRead(conn_web, out reader, sql, true);
                if (!ret.result) throw new Exception(ret.text);

                CounterValLight val;
                DateTime date;
                string counters_vals = "";
                string pref = "", prev_pref = "";
                int nzpUser = 0;
                int i = 0;

                while (reader.Read())
                {
                    i++;
                    pref = Convert.ToString(reader["pref"]).Trim();

                    if (pref != prev_pref)
                    {
                        #region определение локального пользователя
                        nzpUser = finder.nzp_user;
                        /*DbWorkUser db = new DbWorkUser();
                        finder.pref = pref;
                        nzpUser = db.GetLocalUser(conn_db, finder, out ret);
                        db.Close();
                        if (!ret.result) return ret;*/
                        finder.nzp_user_main = nzpUser;
                        #endregion
#if PG
                        counters_vals = pref + "_charge_" + (Points.CalcMonth.year_ % 100).ToString("00") + ".counters_vals";
#else
                        counters_vals = pref + "_charge_" + (Points.CalcMonth.year_ % 100).ToString("00") + ":counters_vals";
#endif
                        prev_pref = pref;
                    }

                    val = new CounterValLight
                    {
                        nzp_user = finder.nzp_user,
                        pref = pref,
                        year_ = Points.CalcMonth.year_,
                        month_ = Points.CalcMonth.month_,
                        ist = (int) CounterVal.Ist.FileSamara,
                        nzp_user_main = finder.nzp_user_main
                    };

                    if (reader["dnew"] != DBNull.Value)
                    {
                        if (!DateTime.TryParseExact(Convert.ToString(reader["dnew"]), "yyyyMMdd", new CultureInfo("ru-RU"), DateTimeStyles.None, out date))
                        {
                            listNotUploadedReadings.Add(GetCounterValDbfFromReader(reader, "Некорректная дата показания. Ожидалась дата в формате ГГГГММДД."));
                            numNotUploadedOtherReason++;
                            continue;
                        }
                    }
                    else
                    {
                        listNotUploadedReadings.Add(GetCounterValDbfFromReader(reader, "Не задана дата показания. Ожидалась дата в формате ГГГГММДД."));
                        numNotUploadedOtherReason++;
                        continue;
                    }

                    if (date <= new DateTime(Points.CalcMonth.year_, Points.CalcMonth.month_, 1))
                    {
                        listUploadedReadingsWithWarning.Add(GetCounterValDbfFromReader(reader, "Текущий расчетный месяц " + Points.CalcMonth.name + ". Показание будет учтено как показание за прошлые расчетные месяцы"));
                    }
                    else if (date > new DateTime(Points.CalcMonth.year_, Points.CalcMonth.month_, 1).AddMonths(1))
                    {
                        listUploadedReadingsWithWarning.Add(GetCounterValDbfFromReader(reader, "Текущий расчетный месяц " + Points.CalcMonth.name + ". Показание будет учтено как показание за будущие расчетные месяцы"));
                    }

                    DateTime monthPokaz = date.Day == 1 ? date.AddMonths(-1) : new DateTime(date.Year, date.Month, 1);

                    val.dat_uchet = monthPokaz.AddMonths(1).ToShortDateString();    // меняем дату показания на 1 число следующего месяца
                    //например, дата показания 23.09.2012, будет 01.10.2012.
                    if (reader["znew"] != DBNull.Value) val.val_cnt = Convert.ToDecimal(reader["znew"]);
                    if (reader["nzp_kvar"] != DBNull.Value) val.nzp_kvar = Convert.ToInt32(reader["nzp_kvar"]);
                    if (reader["nzp_counter"] != DBNull.Value) val.nzp_counter = Convert.ToInt32(reader["nzp_counter"]);

                    sql = "select nzp_cv, iscalc from " + counters_vals +
                        " where ist = " + val.ist +
                        " and nzp_counter = " + val.nzp_counter +
                        " and month_ = " + Points.CalcMonth.month_ +
                        " and dat_uchet between mdy(" + monthPokaz.Month + ",2," + monthPokaz.Year + ") and mdy(" + monthPokaz.AddMonths(1).Month + ",1," + monthPokaz.AddMonths(1).Year + ")" +
                        " and iscalc = 0";

                    ret = ExecRead(conn_web, out reader2, sql, true);
                    if (!ret.result) throw new Exception(ret.text);

                    if (reader2.Read())
                    {
                        if (reader2["nzp_cv"] != DBNull.Value) val.nzp_cv = Convert.ToInt32(reader2["nzp_cv"]);
                    }
                    reader2.Close();

                    if (val.nzp_cv < 1) // добавляем, если показание новое, или имеющееся за данный период показание учтено в расчетах
                    {
                        sql = "Insert into " + counters_vals + "(nzp_cv, nzp, nzp_type, nzp_counter, month_, dat_uchet, val_cnt, nzp_user, dat_when, ist, is_new)" +
#if PG
 " values (default," + val.nzp_kvar +
#else
 " values (0," + val.nzp_kvar +
#endif
 ", " + (int)CounterKinds.Kvar +
                            ", " + val.nzp_counter +
                            ", " + Points.CalcMonth.month_ +
                            ", " + Utils.EStrNull(val.dat_uchet) +
                            ", " + val.val_cnt +
                            ", " + nzpUser +
                            ", " + Utils.EStrNull(date.ToShortDateString()) +
                            ", " + val.ist +
                            ", 1)";
                        ret = ExecSQL(conn_db, sql, true);
                        if (!ret.result) throw new Exception(ret.text);
                        val.nzp_cv = GetSerialValue(conn_db, null);
                    }
                    else
                    {
                        sql = "Update " + counters_vals +
                            " set val_cnt = " + val.val_cnt +
                            ", dat_uchet = " + Utils.EStrNull(val.dat_uchet) +
                            ", nzp_user = " + nzpUser +
                            ", dat_when = " + Utils.EStrNull(date.ToShortDateString()) +
                            ", ist = " + val.ist +
                            ", is_new = 1" +
                            " where nzp_cv = " + val.nzp_cv;
                        ret = ExecSQL(conn_db, sql, true);
                        if (!ret.result) throw new Exception(ret.text);
                    }


                    if (Points.SaveCounterReadingsToRealBank)
                    {
                        ret = SaveCounterValueToRealBank(conn_db, val);
                        if (!ret.result) throw new Exception(ret.text);
                        else if (ret.result && ret.tag < 0)
                            listUploadedReadingsWithWarning.Add(GetCounterValDbfFromReader(reader, ret.text));
                    }

                    if (i % 50 == 0)
                        dbRep.SetFileProgress(nzpExc, ((decimal)i) / numRecords);
                }
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка в функции SaveUploadedCounterReadings:\n" + (ex.Message != "" ? ex.Message : ret.text), MonitorLog.typelog.Error, true);
            }
            finally
            {
                dbRep.Close();

                if (reader != null) reader.Close();
                reader = null;

                if (reader2 != null) reader2.Close();
                reader2 = null;

                conn_db.Close();
                conn_web.Close();
            }
            if (ret.result)
            {
                ret.text = "Всего загружено показаний: " + (numVsego - numNotFoundedPu - numNotUploadedOtherReason).ToString() + " из " + numVsego +
                    ".<br> Не найдено приборов учета: " + numNotFoundedPu +
                    ", в том числе не найдено лицевых счетов: " + numNotFoundedLs +
                    ".<br> Не загружено по другим причинам: " + numNotUploadedOtherReason + ".";
                ret.tag = -1;
            }

            //сохранение результатов в файл        
            string filename = "prot_pu_upload_u" + finder.nzp_user + "_" + DateTime.Now.ToString("dd_MM_yyyy_HH_mm_ss");
            filename = filename + ".txt";
            string path = "";
            if (InputOutput.useFtp)
            {
                path = InputOutput.GetOutputDir();
            }
            else
            {
                path = Constants.ExcelDir;
            }

            using (var file = new StreamWriter(path + filename, true, Encoding.GetEncoding(1251)))
            {
                file.WriteLine("Протокол загрузки показаний приборов учета от " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"));
                file.WriteLine("Наименование файла: " + finder.dopFind[0]);
                file.WriteLine("Пользователь: " + finder.webLogin + " (код " + finder.nzp_user + ")");
                file.WriteLine("Всего загружено показаний: " + (numVsego - numNotFoundedPu - numNotUploadedOtherReason).ToString() + " из " + numVsego + ".");
                if (numNotFoundedPu > 0) file.WriteLine("Не найдено приборов учета: " + numNotFoundedPu + ", в том числе не найдено лицевых счетов: " + numNotFoundedLs);
                if (numNotUploadedOtherReason > 0) file.WriteLine("Не загружено по другим причинам: " + numNotUploadedOtherReason + ".");

                if (listNotUploadedReadings.Count > 0)
                {
                    file.WriteLine();
                    file.WriteLine("Список незагруженных показаний");
                    file.WriteLine();
                    file.WriteLine(CounterValDbf.GetHeaderString());
                    foreach (CounterValDbf val in listNotUploadedReadings)
                    {
                        file.WriteLine(val.ToString());
                    }
                }

                if (listUploadedReadingsWithWarning.Count > 0)
                {
                    file.WriteLine();
                    file.WriteLine("Список показаний, загруженных с предупреждениями");
                    file.WriteLine();
                    file.WriteLine(CounterValDbf.GetHeaderString());
                    foreach (CounterValDbf val in listUploadedReadingsWithWarning)
                    {
                        file.WriteLine(val.ToString());
                    }
                }
            }
            //перенос  на ftp сервер
            if (InputOutput.useFtp)
                InputOutput.SaveOutputFile(Path.Combine(path, filename));

            using (var dbRep2 = new DBMyFiles())
            {
                dbRep2.SetFileProgress(nzpExc, 1);
                dbRep2.SetFileState(new ExcelUtility()
                {
                    nzp_exc = nzpExc,
                    status = ExcelUtility.Statuses.Success,
                    exc_path = filename
                });
            }

            return ret;
        }

        public Returns CorrelateUploadedCounterReadings(Finder finder)
        {
            if (finder.nzp_user < 1) return new Returns(false, "Пользователь не задан");

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            Returns ret = OpenDb(conn_web, true);
            if (!ret.result) return ret;

            string tXX_uplcnt = "t" + Convert.ToString(finder.nzp_user) + "_uplcnt";

            string tXX_tempreestr = "t" + Convert.ToString(finder.nzp_user) + "_tempreestr";
#if PG
            string tXX_uplcnt_full = conn_web.Database + ".public." + tXX_uplcnt;
#else
            string tXX_uplcnt_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXX_uplcnt;
#endif
            if (!TableInWebCashe(conn_web, tXX_uplcnt))
            {
                ret.result = false;
                ret.text = "Необходимо сначала загрузить электронный файл с показаниями";
                ret.tag = -1;
                conn_web.Close();
                return ret;
            }

            string sql;
            if (!isTableHasColumn(conn_web, tXX_uplcnt, "nzp_kvar"))
            {
                sql = "alter table " + tXX_uplcnt + " add nzp_kvar integer, add pref char(10), add nzp_counter integer, add subpkod char(11) ,add nzp_serv integer";
                ret = ExecSQL(conn_web, sql, true);
                if (!ret.result)
                {
                    conn_web.Close();
                    return ret;
                }
            }
            if (!isTableHasColumn(conn_web, tXX_uplcnt, "nzp_serv"))
            {
                sql = "alter table " + tXX_uplcnt + " add nzp_serv integer";
                ret = ExecSQL(conn_web, sql, true);
                if (!ret.result)
                {
                    conn_web.Close();
                    return ret;
                }
            }
            sql = "update " + tXX_uplcnt + " set subpkod = lpad(trim(" + sNvlWord + "(predpr,0)),3,'0') || lpad(trim(" + sNvlWord + "(geu,0)),2,'0') || lpad(trim(" + sNvlWord + "(lc,0)),5,'0') || lpad(trim(" + sNvlWord + "(llc,0)),1,'0')";
            ret = ExecSQL(conn_web, sql, true);
            if (!ret.result)
            {
                conn_web.Close();
                return ret;
            }

            string connectionString = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                conn_web.Close();
                return ret;
            }

            MyDataReader reader = null;

            int numVsego = 0, numNotFoundedPu = 0, numNotFoundedLs = 0;
            List<CounterValDbf> listNotUploadedReadings = new List<CounterValDbf>(); // список незагруженных показаний
            List<CounterValDbf> listUploadedReadingsWithWarning = new List<CounterValDbf>(); // список показаний, по которым есть предупреждения

            try
            {
                ExecSQL(conn_db, "DROP TABLE tmpkvar", false);
                sql = "create temp table tmpkvar " +
                                    " ( nzp_kvar integer," +
                                    " pref char(10)," +
                                    " subkod char(11))" + DBManager.sUnlogTempTable;
                ExecSQL(conn_db, sql, true);


                sql = " insert into tmpkvar(nzp_kvar, pref, subkod)" +
                      "select nzp_kvar, pref, substr(pkod||'',1,11) subkod " +
                      " from " + Points.Pref + DBManager.sDataAliasRest + "kvar " +
                      " where pkod is not null and pkod > 0";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                {
                    return ret;
                }
                ExecSQL(conn_db, "create index ix_tmpkvar on tmpkvar(subkod)", true);
#if PG
                ExecSQL(conn_db, "analyze tmpkvar", true);
#else
                ExecSQL(conn_db, "update statistics for table tmpkvar", true);
#endif
                sql = "update " + tXX_uplcnt_full + " set nzp_kvar = (select nzp_kvar from tmpkvar where subkod = subpkod), pref = (select pref from tmpkvar where subkod = subpkod)";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return ret;

                ExecSQL(conn_db, "drop table tmpkvar", false);




                #region Старый алгоритм
                //#if PG
                //                sql = "select distinct pref from " + tXX_uplcnt + " where pref is not null";
                //#else
                //                sql = "SELECT UNIQUE pref FROM " + tXX_uplcnt + " WHERE pref IS NOT NULL";
                //#endif
                //                ret = ExecRead(conn_web, out reader, sql, true);
                //                if (!ret.result)
                //                {
                //                    MonitorLog.WriteLog("Ошибка функции CorrelateUploadedCounterReadings", MonitorLog.typelog.Error, true);
                //                    return ret;
                //                }

                //                while (reader.Read())
                //                {
                //                    if (reader["pref"] == DBNull.Value) continue;
                //#if PG
                //                    sql = "update " + tXX_uplcnt_full + " set nzp_counter = " + reader["pref"].ToString().Trim() + "_data.get_counter(nzp_kvar, usl, NS, dnew||'')" +
                //                                            " where pref = " + Utils.EStrNull(reader["pref"].ToString().Trim());
                //#else
                //                    sql = "update " + tXX_uplcnt_full + " set nzp_counter = " + reader["pref"].ToString().Trim() + "_data:get_counter(nzp_kvar, usl, NS, dnew||'')" +
                //                        " where pref = " + Utils.EStrNull(reader["pref"].ToString().Trim()) + " and (nzp_counter is null or nzp_counter < 1)";
                //#endif
                //                    ret = ExecSQL(conn_db, sql, true);
                //                    if (!ret.result)
                //                    {
                //                        reader.Close();
                //                        return ret;
                //                    }
                //                }
                //                reader.Close();

                //                #region Дооперделение не найденных приборов учета

                //#if PG
                //                sql = "update " + tXX_uplcnt_full + " set nzp_serv = (select nzp_serv from " +
                //                                   Points.Pref + "_data.services_smr serv_smr where nzp_area=(select nzp_area from " + Points.Pref + "_data.kvar k where "
                //                                   + tXX_uplcnt_full + ".nzp_kvar=k.nzp_kvar) and " + tXX_uplcnt_full + ".usl=serv_smr.kod_usl )";
                //                ret = ExecSQL(conn_web, sql, true);
                //                if (!ret.result) throw new Exception(ret.text);
                //#else
                //                sql = "update " + tXX_uplcnt_full + " set nzp_serv = (select nzp_serv from " +
                //                  Points.Pref + "_data:services_smr serv_smr where nzp_area=(select nzp_area from " + Points.Pref + "_data:kvar k where "
                //                  + tXX_uplcnt_full + ".nzp_kvar=k.nzp_kvar) and " + tXX_uplcnt_full + ".usl=serv_smr.kod_usl )";
                //                ret = ExecSQL(conn_web, sql, true);
                //                if (!ret.result) throw new Exception(ret.text);

                //                if (Points.IsSmr)
                //                {
                //                    sql = "update " + tXX_uplcnt_full + " set nzp_serv = nzp_serv - 900 where nzp_serv between 900 and 1000";
                //                    ret = ExecSQL(conn_web, sql, true);
                //                    if (!ret.result) throw new Exception(ret.text);
                //                }
                //#endif
                //                sql = "drop table s_temp";
                //                ret = ExecSQL(conn_web, sql, false);
                //#if PG
                //                sql = "  create unlogged table  s_temp(" +
                //                                " nzp_kvar integer," +
                //                                " nzp_serv integer," +
                //                                " nzp_counter integer ) ";
                //                ret = ExecSQL(conn_web, sql, true);
                //#else
                //                sql = "  create temp table  s_temp(" +
                //                " nzp_kvar integer," +
                //                " nzp_serv integer," +
                //                " nzp_counter integer ) with no log";
                //                ret = ExecSQL(conn_web, sql, true);
                //#endif
                //                if (!ret.result) throw new Exception(ret.text);
                //                foreach (var pr in Points.PointList)
                //                {

                //#if PG
                //                    sql = " insert into s_temp (nzp_kvar, nzp_serv, nzp_counter) " +
                //                                            " select t.nzp_kvar, s.nzp_serv, max(s.nzp_counter) " +
                //                                            " from " + pr.pref + "_data.counters_spis s, " + tXX_uplcnt_full + " t where s.nzp_serv=t.nzp_serv " +
                //                                            " and t.nzp_kvar=nzp and MDY(date_part('month', now())::int, 1,  date_part('year', now())::int) >=dat_when and  " +
                //                                            " MDY(date_part('month', now())::int, 1,  date_part('year', now())::int) <=coalesce(dat_close,mdy(1,1,3000)) " +
                //                                            " and is_actual<>100 and nzp_type=3 " +
                //                                            " and t.nzp_counter = 0 " +
                //                                            " group by 1,2 having count(*) = 1 ";
                //#else
                //                    sql = " insert into s_temp (nzp_kvar, nzp_serv, nzp_counter) " +
                //                        " select t.nzp_kvar, s.nzp_serv, max(s.nzp_counter) " +
                //                        " from " + pr.pref + "_data:counters_spis s, " + tXX_uplcnt_full + " t where s.nzp_serv=t.nzp_serv " +
                //                        " and t.nzp_kvar=nzp and MDY(MONTH(TODAY), 1, YEAR(TODAY)) >=dat_when and  " +
                //                        " MDY(MONTH(TODAY), 1, YEAR(TODAY)) <=nvl(dat_close,mdy(1,1,3000)) " +
                //                        " and is_actual<>100 and nzp_type=3 " +
                //                        " and t.nzp_counter = 0 " +
                //                        " group by 1,2 having count(*) = 1 ";
                //#endif


                //                    ret = ExecSQL(conn_web, sql, true);
                //                    if (!ret.result) throw new Exception(ret.text);

                //                }
                //                sql = "update " + tXX_uplcnt_full +
                //                    " set nzp_counter = (select nzp_counter from s_temp t where t.nzp_kvar = " + tXX_uplcnt_full + ".nzp_kvar and t.nzp_serv = " + tXX_uplcnt_full + ".nzp_serv)" +
                //                    " where " + tXX_uplcnt_full + ".nzp_counter = 0";
                //                ret = ExecSQL(conn_web, sql, true);
                //                if (!ret.result) throw new Exception(ret.text);

                #endregion

                //получаем список актуальных реестров
                sql = "SELECT * FROM " + Points.Pref + sDataAliasRest + "reestr_upload_pu WHERE is_actual<>100";
                DataTable reestrs = ClassDBUtils.OpenSQL(sql, conn_db).resultData;


                //создаем темповую таблицу для актуальных выгруженных реестров
                ExecSQL(conn_db, "drop table " + tXX_tempreestr, false);

                sql = "CREATE temp table " + tXX_tempreestr + "(" +
                    " nzp_kvar integer," +
                    " nzp_serv integer," +
                    " nzp_counter integer," +
                    " ns char(2)," +
                    " usl char(4)" +
                    ") " + sUnlogTempTable;

                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) throw new Exception(ret.text);

                //заполняем актуальными данными из реестров
                for (int i = 0; i < reestrs.Rows.Count; i++)
                {
                    string month = reestrs.Rows[i]["month"] != DBNull.Value ? Convert.ToInt32(reestrs.Rows[i]["month"]).ToString("00") : "";
                    string year = reestrs.Rows[i]["year"] != DBNull.Value ? (Convert.ToInt32(reestrs.Rows[i]["year"]) - 2000).ToString("00") : "";
                    int nzp_reestr = reestrs.Rows[i]["nzp_reestr"] != DBNull.Value ? Convert.ToInt32(reestrs.Rows[i]["nzp_reestr"]) : 0;

                    sql = "INSERT INTO " + tXX_tempreestr + "(nzp_kvar,nzp_serv,nzp_counter,ns,usl) " +
                        "SELECT nzp_kvar,nzp_serv,nzp_counter,ns,usl FROM " + Points.Pref + "_fin_" + year + tableDelimiter + "upload_pu_" + month + " WHERE nzp_reestr=" + nzp_reestr;
                    ret = ExecSQL(conn_db, sql, true);
                    if (!ret.result) throw new Exception(ret.text);
                }
                //индексы
                DBManager.CreateIndexIfNotExists(conn_db, "ix1_" + tXX_tempreestr, Points.Pref + "_kernel" + tableDelimiter + tXX_tempreestr, "nzp_kvar,nzp_counter,ns,usl");
                ExecSQL(conn_db, DBManager.sUpdStat + tXX_tempreestr, false);
                //сопоставляем счетчики
                sql = " UPDATE " + tXX_uplcnt_full + " SET (nzp_counter,nzp_serv) =" +
                    "(( SELECT a.nzp_counter,a.nzp_serv FROM " + tXX_tempreestr + " a" +
                    " WHERE " + tXX_uplcnt_full + ".nzp_kvar=a.nzp_kvar and " + tXX_uplcnt_full + ".usl=a.usl and cast(" + tXX_uplcnt_full + ".ns as integer) = cast(a.ns as integer) ))";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) throw new Exception(ret.text);


                sql = "update " + tXX_uplcnt_full + " set nzp_counter=0 where nzp_counter is null";
                ret = ExecSQL(conn_web, sql, true);
                if (!ret.result) throw new Exception(ret.text);


                #region определение количества показаний всего, кол-во не найденных ПУ, ЛС и т.п.
                sql = "select count(*) as s1, sum(case when nzp_counter is null or nzp_counter < 1 then 1 else 0 end) as s2" +
                    ", sum(case when nzp_kvar is null or nzp_kvar < 1 then 1 else 0 end) as s3" +
                    " from " + tXX_uplcnt;
                ret = ExecRead(conn_web, out reader, sql, true);
                if (!ret.result) return ret;
                if (reader.Read())
                {
                    if (reader["s1"] != DBNull.Value) numVsego = Convert.ToInt32(reader["s1"]);
                    if (reader["s2"] != DBNull.Value) numNotFoundedPu = Convert.ToInt32(reader["s2"]);
                    if (reader["s3"] != DBNull.Value) numNotFoundedLs = Convert.ToInt32(reader["s3"]);
                }
                reader.Close();
                #endregion
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка в функции CorrelateUploadedCounterReadings:\n" + (ex.Message != "" ? ex.Message : ret.text), MonitorLog.typelog.Error, true);
            }
            finally
            {
                if (reader != null) reader.Close();

                conn_db.Close();
                conn_web.Close();
            }
            if (ret.result)
            {
                ret.text = "Всего показаний: " + numVsego + ".<br> Не найдено приборов учета: " + numNotFoundedPu + ", не найдено лицевых счетов: " + numNotFoundedLs;
                ret.tag = -1;
            }

            return ret;
        }




        public Returns HouseCounters(List<string> pref, StreamWriter writer, IDbConnection conn)
        {
            Returns ret = Utils.InitReturns();

            string sqlString = "";

            if (!ExecSQL(conn,
            " drop table temp_counters;", false).result) { }
#if PG
            sqlString = "create temp table temp_counters ( nzp_dom integer, nzp_serv integer,tip_rash integer, " +
                         "tip_usl integer,nzp_cnttype char(25), cnt_stage integer,formula integer, num_cnt char(20), " +
                         "dat_uchet date,val_cnt decimal,nzp_measure integer,dat_prov date,dat_provnext date " +
                         ") ";
#else
            sqlString = "create temp table temp_counters ( nzp_dom integer, nzp_serv integer,tip_rash integer, " +
                         "tip_usl integer,nzp_cnttype char(25), cnt_stage integer,formula integer, num_cnt char(20), " +
                         "dat_uchet date,val_cnt decimal,nzp_measure integer,dat_prov date,dat_provnext date " +
                         ") with no log ";
#endif

            ClassDBUtils.OpenSQL(sqlString, conn);

            foreach (var p in pref)
            {
#if PG
                sqlString = " insert into temp_counters ( nzp_dom,nzp_serv,nzp_cnttype,num_cnt,dat_uchet,val_cnt,nzp_measure,dat_prov,dat_provnext) select c.nzp_dom, c.nzp_serv,c.nzp_cnttype, c.num_cnt, c.dat_uchet, c.val_cnt, c.nzp_measure, c.dat_prov, c.dat_provnext from " + p + "_data.counters_dom c where c.is_actual<>100  ";
#else
                sqlString = " insert into temp_counters ( nzp_dom,nzp_serv,nzp_cnttype,num_cnt,dat_uchet,val_cnt,nzp_measure,dat_prov,dat_provnext) select c.nzp_dom, c.nzp_serv,c.nzp_cnttype, c.num_cnt, c.dat_uchet, c.val_cnt, c.nzp_measure, c.dat_prov, c.dat_provnext from " + p + "_data:counters_dom c where c.is_actual<>100  ";
#endif
                ClassDBUtils.OpenSQL(sqlString, conn);
            }

            sqlString = "update temp_counters set tip_rash=1 ";
            ClassDBUtils.OpenSQL(sqlString, conn);

            sqlString = "update temp_counters set tip_rash=1 ";
            ClassDBUtils.OpenSQL(sqlString, conn);

#if PG
            foreach (var p in pref)
            {
                sqlString = " update temp_counters set cnt_stage=(select cnt_stage from " + p + "_kernel.s_counttypes c where temp_counters.nzp_cnttype=c.nzp_cnttype) ";
                ClassDBUtils.OpenSQL(sqlString, conn);
                sqlString = " update temp_counters set formula=(select formula from " + p + "_kernel.s_counttypes c where temp_counters.nzp_cnttype=c.nzp_cnttype) ";
                ClassDBUtils.OpenSQL(sqlString, conn);
            }
#else
            foreach (var p in pref)
            {
                sqlString = " update temp_counters set cnt_stage=(select cnt_stage from " + p + "_kernel:s_counttypes c where temp_counters.nzp_cnttype=c.nzp_cnttype) ";
                ClassDBUtils.OpenSQL(sqlString, conn);
                sqlString = " update temp_counters set formula=(select formula from " + p + "_kernel:s_counttypes c where temp_counters.nzp_cnttype=c.nzp_cnttype) ";
                ClassDBUtils.OpenSQL(sqlString, conn);
            }
#endif

            ret.result = true;
            return ret;
        }

        public int WriteHouseCounters(StreamWriter writer, IDbConnection conn)
        {
            IDataReader reader = null;
            Returns ret = Utils.InitReturns();
            string sqlString = "select * from temp_counters order by nzp_dom, nzp_serv";
            int i = 0;
            if (!ExecRead(conn, out reader, sqlString, true).result)
            {
                conn.Close();
                ret.result = false;
                return 0;
            }
            try
            {
                if (reader != null)
                {

                    while (reader.Read())
                    {
                        string str = "9|" +
                  (reader["nzp_dom"] != DBNull.Value ? ((int)reader["nzp_dom"]) + "|" : "|") +
                  (reader["nzp_serv"] != DBNull.Value ? ((int)reader["nzp_serv"]) + "|" : "|") +
                  (reader["tip_rash"] != DBNull.Value ? ((int)reader["tip_rash"]) + "|" : "|") +
                  (reader["tip_usl"] != DBNull.Value ? ((int)reader["tip_usl"]) + "|" : "|") +
                  (reader["nzp_cnttype"] != DBNull.Value ? ((string)reader["nzp_cnttype"]).ToString().Trim() + "|" : "|") +
                  (reader["cnt_stage"] != DBNull.Value ? ((int)reader["cnt_stage"]) + "|" : "|") +
                  (reader["formula"] != DBNull.Value ? ((int)reader["formula"]) + "|" : "|") +
                  (reader["num_cnt"] != DBNull.Value ? ((string)reader["num_cnt"]).ToString().Trim() + "|" : "|") +
                  (reader["dat_uchet"] != DBNull.Value ? ((DateTime)reader["dat_uchet"]).ToString("dd.MM.yyyy") + "|" : "|") +
                  (reader["val_cnt"] != DBNull.Value ? ((Decimal)reader["val_cnt"]).ToString("0.00").Trim() + "|" : "|") +
                  (reader["nzp_measure"] != DBNull.Value ? ((int)reader["nzp_measure"]) + "|" : "|") +
                  (reader["dat_prov"] != DBNull.Value ? ((DateTime)reader["dat_prov"]).ToString("dd.MM.yyyy") + "|" : "|") +
                  (reader["dat_provnext"] != DBNull.Value ? ((DateTime)reader["dat_provnext"]).ToString("dd.MM.yyyy") + "|" : "|");

                        writer.WriteLine(str);
                        i++;
                    }

                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка при записи данных  " + ex.Message, MonitorLog.typelog.Error, true);
                reader.Close();
                ret.result = false;
                return 0;
            }

            writer.Flush();
            ret.result = true;
            return i;
        }

        public Returns IndivCounters(List<string> pref, StreamWriter writer, IDbConnection conn)
        {
            Returns ret = Utils.InitReturns();

            string sqlString = "";

            if (!ExecSQL(conn,
            " drop table temp_counters_ind;", false).result) { }

#if PG
            sqlString = "create temp table temp_counters_ind ( num_ls char(20), nzp_serv integer,tip_rash integer, nzp_counter integer, " +
                      "tip_usl integer,nzp_cnttype char(25), cnt_stage integer,formula integer, num_cnt char(20), " +
                      "dat_uchet date,val_cnt decimal,nzp_measure integer,dat_prov date,dat_provnext date )";
#else
            sqlString = "create temp table temp_counters_ind ( num_ls char(20), nzp_serv integer,tip_rash integer, nzp_counter integer, " +
                     "tip_usl integer,nzp_cnttype char(25), cnt_stage integer,formula integer, num_cnt char(20), " +
                     "dat_uchet date,val_cnt decimal,nzp_measure integer,dat_prov date,dat_provnext date " +
                     ") with no log ";
#endif
            ClassDBUtils.OpenSQL(sqlString, conn);

            foreach (var p in pref)
            {
#if PG
                sqlString = " insert into temp_counters_ind ( num_ls,nzp_serv,nzp_cnttype,num_cnt,nzp_counter,dat_uchet,val_cnt,dat_prov,dat_provnext) select c.num_ls, c.nzp_serv,c.nzp_cnttype, c.num_cnt,c.nzp_counter, c.dat_uchet, c.val_cnt, c.dat_prov, c.dat_provnext from " + p + "_data.counters c  where c.is_actual<>100  ";
#else
                sqlString = " insert into temp_counters_ind ( num_ls,nzp_serv,nzp_cnttype,num_cnt,nzp_counter,dat_uchet,val_cnt,dat_prov,dat_provnext) select c.num_ls, c.nzp_serv,c.nzp_cnttype, c.num_cnt,c.nzp_counter, c.dat_uchet, c.val_cnt, c.dat_prov, c.dat_provnext from " + p + "_data:counters c  where c.is_actual<>100  ";
#endif
                ClassDBUtils.OpenSQL(sqlString, conn);
            }

            sqlString = "update temp_counters_ind set tip_rash=1 ";
            ClassDBUtils.OpenSQL(sqlString, conn);

            sqlString = "update temp_counters_ind set tip_rash=1 ";
            ClassDBUtils.OpenSQL(sqlString, conn);

#if PG
            foreach (var p in pref)
            {
                sqlString = " update temp_counters_ind set cnt_stage=(select cnt_stage from " + p + "_kernel.s_counttypes c where temp_counters_ind.nzp_cnttype=c.nzp_cnttype) ";
                ClassDBUtils.OpenSQL(sqlString, conn);
                sqlString = " update temp_counters_ind set formula=(select formula from " + p + "_kernel.s_counttypes c where temp_counters_ind.nzp_cnttype=c.nzp_cnttype) ";
                ClassDBUtils.OpenSQL(sqlString, conn);
                sqlString = " update temp_counters_ind set nzp_measure=(select nzp_measure from " + p + "_kernel.s_counts c where (select nzp_cnt from " + p + "_data.counters_spis where nzp_counter=temp_counters_ind.nzp_counter)=nzp_cnt)";
                ClassDBUtils.OpenSQL(sqlString, conn);
            }
#else
            foreach (var p in pref)
            {
                sqlString = " update temp_counters_ind set cnt_stage=(select cnt_stage from " + p + "_kernel:s_counttypes c where temp_counters_ind.nzp_cnttype=c.nzp_cnttype) ";
                ClassDBUtils.OpenSQL(sqlString, conn);
                sqlString = " update temp_counters_ind set formula=(select formula from " + p + "_kernel:s_counttypes c where temp_counters_ind.nzp_cnttype=c.nzp_cnttype) ";
                ClassDBUtils.OpenSQL(sqlString, conn);
                sqlString = " update temp_counters_ind set nzp_measure=(select nzp_measure from " + p + "_kernel:s_counts c where (select nzp_cnt from " + p + "_data:counters_spis where nzp_counter=temp_counters_ind.nzp_counter)=nzp_cnt)";
                ClassDBUtils.OpenSQL(sqlString, conn);
            }
#endif

            ret.result = true;
            return ret;
        }

        public int WriteIndivCounters(StreamWriter writer, IDbConnection conn)
        {
            IDataReader reader = null;
            Returns ret = Utils.InitReturns();
            string sqlString = "select * from temp_counters_ind order by num_ls, nzp_serv";
            int i = 0;
            if (!ExecRead(conn, out reader, sqlString, true).result)
            {
                conn.Close();
                ret.result = false;
                return 0;
            }
            try
            {
                if (reader != null)
                {

                    while (reader.Read())
                    {
                        string str = "10|" +
                  (reader["num_ls"] != DBNull.Value ? ((string)reader["num_ls"]).ToString().Trim() + "|" : "|") +
                  (reader["nzp_serv"] != DBNull.Value ? ((int)reader["nzp_serv"]) + "|" : "|") +
                  (reader["tip_rash"] != DBNull.Value ? ((int)reader["tip_rash"]) + "|" : "|") +
                  (reader["tip_usl"] != DBNull.Value ? ((int)reader["tip_usl"]) + "|" : "|") +
                  (reader["nzp_cnttype"] != DBNull.Value ? ((string)reader["nzp_cnttype"]).ToString().Trim() + "|" : "|") +
                  (reader["cnt_stage"] != DBNull.Value ? ((int)reader["cnt_stage"]) + "|" : "|") +
                  (reader["formula"] != DBNull.Value ? ((int)reader["formula"]) + "|" : "|") +
                  (reader["num_cnt"] != DBNull.Value ? ((string)reader["num_cnt"]).ToString().Trim() + "|" : "|") +
                  (reader["dat_uchet"] != DBNull.Value ? ((DateTime)reader["dat_uchet"]).ToString("dd.MM.yyyy") + "|" : "|") +
                  (reader["val_cnt"] != DBNull.Value ? ((Decimal)reader["val_cnt"]).ToString("0.00").Trim() + "|" : "|") +
                  (reader["nzp_measure"] != DBNull.Value ? ((int)reader["nzp_measure"]) + "|" : "|") +
                  (reader["dat_prov"] != DBNull.Value ? ((DateTime)reader["dat_prov"]).ToString("dd.MM.yyyy") + "|" : "|") +
                  (reader["dat_provnext"] != DBNull.Value ? ((DateTime)reader["dat_provnext"]).ToString("dd.MM.yyyy") + "|" : "|");

                        writer.WriteLine(str);
                        i++;
                    }

                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка при записи данных  " + ex.Message, MonitorLog.typelog.Error, true);
                reader.Close();
                ret.result = false;
                return 0;
            }

            writer.Flush();
            ret.result = true;
            return i;
        }

        public List<Counter> FindIPU(Counter finder, out Returns ret)
        {
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                return null;
            }

            string pref = finder.pref;

            DateTime dt = new DateTime(Points.CalcMonth.year_, Points.CalcMonth.month_, 1).AddMonths(1);

            string sql = "select a.nzp_counter, a.nzp_serv, s.service, a.num_cnt, t.name_type, t.mmnog, t.cnt_stage, a.dat_close, a.dat_prov, a.dat_provnext " +
                ", (select max(dat_uchet) from " + pref + "_data:counters where nzp_counter = a.nzp_counter and is_actual <> 100 and dat_uchet <= mdy(" + dt.Month + ",1," + dt.Year + ")) dat_uchet" +
                ", (select max(val_cnt) from " + pref + "_data:counters where nzp_counter = a.nzp_counter and is_actual <> 100 and dat_uchet = ((select max(dat_uchet) from " + pref + "_data:counters where nzp_counter = a.nzp_counter and is_actual <> 100 and dat_uchet <= mdy(" + dt.Month + ",1," + dt.Year + ")))) val_cnt" +
                " from " + pref + "_data:counters_spis a, " + Points.Pref + "_kernel:services s, " + pref + "_kernel:s_counttypes t " +
                " where a.nzp_serv = s.nzp_serv and a.nzp_cnttype = t.nzp_cnttype" +
                " and a.nzp_type = 3 and a.nzp = " + finder.nzp_kvar;

            MyDataReader reader;
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }

            List<Counter> list = new List<Counter>();
            try
            {
                Counter counter;
                while (reader.Read())
                {
                    counter = new Counter();
                    counter.pref = pref;
                    //if (reader["nzp_type"] != DBNull.Value) counter.nzp_type = Convert.ToInt32(reader["nzp_type"]);
                    if (reader["service"] != DBNull.Value) counter.service = Convert.ToString(reader["service"]).Trim();
                    if (reader["num_cnt"] != DBNull.Value) counter.num_cnt = Convert.ToString(reader["num_cnt"]).Trim();
                    if (reader["name_type"] != DBNull.Value) counter.name_type = Convert.ToString(reader["name_type"]).Trim();
                    if (reader["mmnog"] != DBNull.Value) counter.mmnog = Convert.ToInt32(reader["mmnog"]);
                    if (reader["cnt_stage"] != DBNull.Value) counter.cnt_stage = Convert.ToInt32(reader["cnt_stage"]);
                    if (reader["nzp_counter"] != DBNull.Value) counter.nzp_counter = Convert.ToInt32(reader["nzp_counter"]);
                    if (reader["dat_uchet"] != DBNull.Value) counter.dat_uchet = Convert.ToDateTime(reader["dat_uchet"]).ToShortDateString();
                    if (reader["dat_close"] != DBNull.Value) counter.dat_close = Convert.ToDateTime(reader["dat_close"]).ToShortDateString();
                    if (reader["dat_prov"] != DBNull.Value) counter.dat_prov = Convert.ToDateTime(reader["dat_prov"]).ToShortDateString();
                    if (reader["dat_provnext"] != DBNull.Value) counter.dat_provnext = Convert.ToDateTime(reader["dat_provnext"]).ToShortDateString();
                    if (reader["val_cnt"] != DBNull.Value) counter.val_cnt_s = Convert.ToDecimal(reader["val_cnt"]).ToString();

                    list.Add(counter);
                }
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                MonitorLog.WriteLog("FindIPU\n" + ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }
            finally
            {
                conn_db.Close();
            }
            return list;
        }


        public List<CounterBounds> GetCounterBoundses(CounterBounds finder, out Returns ret)
        {
            var list = new List<CounterBounds>();

            #region Проверки
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                return list;
            }
            if (finder.pref.Length == 0)
            {
                ret.result = false;
                ret.text = "Не задан префикс локального банка";
                return list;
            }
            if (finder.nzp_user <= 0)
            {
                ret.result = false;
                ret.text = "Код пользователя не определен";
                return list;
            }
            if (finder.nzp_counter <= 0)
            {
                ret.result = false;
                ret.text = "Код ПУ не определен";
                return list;
            }
            string sql = "";
            string skip = "";
            string rows = "";

            #endregion
            #region Ограничения для пагинации

            if (finder.skip != 0)
            {
                skip = " offset " + finder.skip;
            }
            if (finder.rows != 0)
            {
                rows = " limit " + finder.rows;
            }
            #endregion


            sql = " SELECT c.*,u.name||'('||u.comment||')' as user_created, u1.name||'('||u1.comment||')' as user_changed " +
                  " FROM " + finder.pref + sDataAliasRest + "counters_bounds c" +
                  " LEFT OUTER JOIN " + Points.Pref + sDataAliasRest + "users u ON c.created_by=u.web_user " +
                  " LEFT OUTER JOIN " + Points.Pref + sDataAliasRest + "users u1 ON c.changed_by=u1.web_user " +
                  " WHERE nzp_counter=" + finder.nzp_counter + " AND is_actual=true  AND type_id=" + finder.type_id +
                  " ORDER BY id DESC  " + skip + " " + rows + " ";

            MyDataReader reader;
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }

            try
            {
                var row_num = 0;
                CounterBounds rec;
                while (reader.Read())
                {
                    row_num++;
                    rec = new CounterBounds
                    {
                        id = CastValue<int>(reader["id"]),
                        nzp_counter = CastValue<int>(reader["nzp_counter"]),
                        type_id = CastValue<int>(reader["type_id"]),
                        date_from = CastValue<DateTime>(reader["date_from"]),
                        date_to = CastValue<DateTime>(reader["date_to"]),
                        created_by = CastValue<int>(reader["created_by"]),
                        changed_by = CastValue<int>(reader["changed_by"]),
                        created_on = CastValue<DateTime>(reader["created_on"]),
                        changed_on = CastValue<DateTime>(reader["changed_on"]),
                        user_changed = CastValue<string>(reader["user_changed"]),
                        user_created = CastValue<string>(reader["user_created"]),
                        row_num = row_num
                    };
                    rec.s_changed_on = rec.changed_on.ToShortDateString();
                    rec.s_created_on = rec.created_on.ToShortDateString();
                    list.Add(rec);
                }

                ret.tag = DBManager.ExecScalar<int>(conn_db,
                    " SELECT count(*) FROM " + finder.pref + sDataAliasRest + "counters_bounds" +
                    " WHERE nzp_counter=" + finder.nzp_counter + " AND is_actual=true  AND type_id=" + finder.type_id);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                MonitorLog.WriteLog("GetCounterBoundses\n" + ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }
            finally
            {
                conn_db.Close();
            }
            return list;
        }
        /// <summary>
        /// Функция добавления, редактирования, удаления периодов для счетчиков
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        public Returns SaveCounterBounds(CounterBounds finder)
        {
            #region Проверки
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            Returns ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                return ret;
            }
            if (finder.pref.Length == 0)
            {
                ret.result = false;
                ret.tag = Constants.access_code;
                ret.text = "Не задан префикс локального банка";
                return ret;
            }
            if (finder.nzp_user <= 0)
            {
                ret.result = false;
                ret.tag = Constants.access_code;
                ret.text = "Код пользователя не определен";
                return ret;
            }
            if (finder.nzp_counter <= 0)
            {
                ret.result = false;
                ret.tag = Constants.access_code;
                ret.text = "Код ПУ не определен";
                return ret;
            }
            if (finder.date_from == DateTime.MinValue || finder.date_to == DateTime.MinValue)
            {
                ret.result = false;
                ret.tag = Constants.access_code;
                ret.text = "Период для счетчика не определен";
                return ret;
            }
            if (finder.type_id == 0)
            {
                ret.result = false;
                ret.tag = Constants.access_code;
                ret.text = "Не определен тип периода";
                return ret;
            }
            if (finder.date_from > finder.date_to)
            {
                ret.result = false;
                ret.tag = Constants.access_code;
                ret.text = "Дата начала периода не может быть больше даты окончания периода";
                return ret;
            }
            #endregion

            if (finder.date_from == finder.date_to)
            {
                ret.result = false; 
                ret.tag = Constants.access_code; 
                ret.text = "Дата начала периода не может совпадать с датой окончания"; 
                return ret;
            }

            var sql = "";

            //проверка на пересечение периодов поломок 
            if (finder.type_id == (int)TypeBoundsCounters.Breaking && finder.is_actual)
            {
                sql = " SELECT count(*) FROM " + finder.pref + sDataAliasRest + "counters_bounds " +
                      " WHERE nzp_counter=" + finder.nzp_counter + " AND type_id=" + (int)TypeBoundsCounters.Breaking +
                      " AND is_actual=true AND " + Utils.EStrNull(finder.date_from.ToShortDateString()) + "<date_to" +
                      " AND " + Utils.EStrNull(finder.date_to.ToShortDateString()) + ">date_from" +
                      " AND id<>" + finder.id;
                var count = DBManager.ExecScalar<int>(conn_db, sql);
                if (count > 0)
                {
                    ret.result = false;
                    ret.tag = Constants.access_code;
                    ret.text = "\nПериоды поломок пересекаться не могут!";
                    return ret;
                }
            } 

            //обновление существующего периода
            if (finder.id > 0)
            {
                // При удалении межповерочного интервала или периода поломки, выставить признак перерасчета
                if ((finder.type_id == (int) TypeBoundsCounters.Verification || finder.type_id == (int)TypeBoundsCounters.Breaking) &&  !finder.is_actual) 
                {
                    ret = setMustCalc(conn_db, finder);
                    if (!ret.result) return ret;
                }


                sql =
                    string.Format(
                        " UPDATE {0}{1}counters_bounds SET date_from={2}, date_to={3},is_actual={4},changed_by={5},changed_on={6} WHERE id={7}",
                        finder.pref, sDataAliasRest,
                        Utils.EStrNull(finder.date_from.ToShortDateString()),
                        Utils.EStrNull(finder.date_to.ToShortDateString()),
                        finder.is_actual, finder.nzp_user, DBManager.sCurDateTime,
                        finder.id);
            }
            else //новый период
            {
                sql =
                    string.Format(
                        "INSERT INTO {0}{1}counters_bounds (nzp_counter,type_id,date_from,date_to,created_by) " +
                        "VALUES ({2},{3},{4},{5},{6})",
                        finder.pref, sDataAliasRest, finder.nzp_counter,
                        finder.type_id, Utils.EStrNull(finder.date_from.ToShortDateString()),
                        Utils.EStrNull(finder.date_to.ToShortDateString()),
                        finder.nzp_user);
            }
            return ExecSQL(conn_db, sql, true);
        }


         /// <summary>
        /// Устанавливает признак перерасчета при удалении выбранного межповерочного периода или периода поломки
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="finder"></param>
        /// <returns></returns>
        private Returns setMustCalc(IDbConnection conn_db, CounterBounds finder)
        {
            Returns ret = Utils.InitReturns();
            // вытащить текущий расчетный месяц
            RecordMonth rm = Points.GetCalcMonth(new CalcMonthParams(finder.pref));

            #region Вставка признака перерасчета основной услуге
            MustCalcTable must = new MustCalcTable();
            int nzp_kvar;
            int nzp_serv;
            string name_period = "";
            switch (finder.type_id)
            {
                case (int) TypeBoundsCounters.Verification:
                    // Если дата поверки больше или равна дате текущего расчетного месяца выйти
                    if (finder.date_from >= rm.RecordDateTime) return ret;
                    must.DatS = finder.date_from < Points.BeginWork.RecordDateTime ? Points.BeginWork.RecordDateTime : finder.date_from;
                    must.DatPo = new DateTime(rm.RecordDateTime.Year, rm.RecordDateTime.Month, 1).AddDays(-1);
                    name_period = "межповерочного периода";
                    break;
                case (int) TypeBoundsCounters.Breaking:
                    // Дата начала поломки больше или равна дате текущего расчетного месяца
                    if (finder.date_from >= rm.RecordDateTime) return ret;
                    // Дата окончания поломки меньше или равна дате начала работы системы
                    if (finder.date_to <= Points.BeginWork.RecordDateTime) return ret;
                    // ограничить дату начала поломки датой начала работы системы
                    if (finder.date_from < Points.BeginWork.RecordDateTime)
                    {
                        finder.date_from = Points.BeginWork.RecordDateTime;
                    }
                    // ограничить дату окончания поломки датой расчетного месяца
                    if (finder.date_to >= rm.RecordDateTime)
                    {
                        finder.date_to = rm.RecordDateTime.AddMonths(-1);
                    }
                    must.DatS = new DateTime(finder.date_from.Year, finder.date_from.Month, 1);
                    must.DatPo = new DateTime(finder.date_to.Year, finder.date_to.Month, DateTime.DaysInMonth(finder.date_to.Year, finder.date_to.Month)); 
                    name_period = "периода поломки";
                    break;
            }
            ret = GetNzpKvarAndServ(conn_db, finder, name_period, out nzp_kvar, out nzp_serv);
            if (!ret.result)
            {
                return ret;
            }
            must.NzpKvar = nzp_kvar;
            must.NzpServ = nzp_serv;
            must.Month = rm.month_;
            must.Year = rm.year_;
            must.Reason = MustCalcReasons.Counter;
            must.Kod2 = 0;
            must.NzpUser = finder.nzp_user;
            must.Comment = "Удаление " + name_period;
            DbMustCalcNew dbMustCalc = new DbMustCalcNew(conn_db);
            ret = dbMustCalc.InsertReason(finder.pref + "_data", must);
            if (!ret.result) throw new Exception(ret.text);
            #endregion

           #region вставка признака перерасчета по связанным услугам

             // определение связанных услуг
            string query = String.Format("SELECT nzp_serv_slave FROM {0}_data{4}dep_servs " +
                                         "WHERE nzp_serv = {1} AND is_actual = 1 " +
                                         "AND '{2}' >= dat_s AND '{3}' < dat_po and nzp_dep = 1", Points.Pref, nzp_serv, must.DatS.ToShortDateString(), must.DatPo.ToShortDateString(), DBManager.tableDelimiter);

            IntfResultTableType rt = ClassDBUtils.OpenSQL(query, conn_db);
            if (rt.resultCode < 0) throw new Exception(rt.resultMessage);

            // вставка признка перерасчета по связанным услугам
            for (int i = 0; i < rt.resultData.Rows.Count; i++)
            {
                must.NzpServ = Convert.ToInt32(rt.resultData.Rows[i]["nzp_serv_slave"]);
                ret = dbMustCalc.InsertReason(finder.pref + "_data", must);
                if (!ret.result) return ret;
            }

           #endregion

            return ret;
        }

        /// <summary>
        /// Получает ЛС и усулгу для выбранного межповерочного периода или периода поломки
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="cb"></param>
        /// <param name="nzp_kvar"></param>
        /// <param name="nzp_serv"></param>
        /// <returns></returns>
        private Returns GetNzpKvarAndServ(IDbConnection conn_db, CounterBounds cb, string name_period, out int nzp_kvar, out int nzp_serv)
        {

            nzp_kvar = 0;
            nzp_serv = 0;
            string query = "SELECT cs.nzp as nzp_kvar, cs.nzp_serv FROM " + cb.pref + sDataAliasRest + "counters_bounds cb, " + cb.pref + sDataAliasRest + "counters_spis cs " +
                           "WHERE cs.nzp_counter=cb.nzp_counter AND cs.is_actual<>100 AND cb.id=" + cb.id;
            IDataReader reader = null;
            Returns ret = Utils.InitReturns();
            try
            {
                ExecRead(conn_db, out reader, query, true);
                while (reader.Read())
                {
                    if (reader["nzp_kvar"] == DBNull.Value)
                    {
                        throw new UserException("Для выбранного "+ name_period + " не удалось определить ЛС ");
                    }
                    nzp_kvar = (int) reader["nzp_kvar"];
                    if (reader["nzp_serv"] == DBNull.Value)
                    {
                        throw new UserException("Для выбранного " + name_period + " не удалось определить ЛС ");
                    }
                    nzp_serv = (int) reader["nzp_serv"];
                    if (nzp_kvar == 0 || nzp_serv == 0)
                    {
                        throw new UserException("Для выбранного " + name_period + " не найдено услуги или ЛС ");

                    }
                }
            }
            catch (UserException ex)
            {
                MonitorLog.WriteLog("GetNzpKvarAndServ\n" + ex.Message+ "(таблица counters_bounds, id = " + cb.id + ")", MonitorLog.typelog.Error, true);
                ret.result = false;
                ret.text = ex.Message;
                return ret;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("GetNzpKvarAndServ\n" + ex.Message, MonitorLog.typelog.Error, true);
                ret.result = false;
                return ret;
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                }
            }

            return ret;
        }
    }
}
