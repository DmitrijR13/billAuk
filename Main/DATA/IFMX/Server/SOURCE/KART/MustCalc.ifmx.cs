using System;
using System.Data;
using System.Linq;
using System.Xml.Linq;
using System.IO;
using System.Text;
using System.Collections.Generic;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;


namespace STCLINE.KP50.DataBase
{
    //----------------------------------------------------------------------
    public partial class DbMustCalc : DataBaseHead
    //----------------------------------------------------------------------
    {
        //----------------------------------------------------------------------
        public bool BufCalcInsert(_BufCalc zap, out Returns ret) //вытащить ПУ для грида
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            List<Counter> Spis = new List<Counter>();

            Spis.Clear();

            //выбрать общее кол-во
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return false;

            string tbuf = "bufcalc";
            string sql;

            if (!TableInWebCashe(conn_web, tbuf))
            {
                //создать таблицу bifcalc
#if PG
                sql =
                               " Create table " + tbuf +
                               " ( nzp_kvar integer, " +
                               "   nzp_serv integer, " +
                               "   nzp_supp integer, " +
                               "   dat_s    date, " +
                               "   dat_po   date, " +
                               "   cnt_add  smallint, " +
                               "   kod1 integer default 0, " +
                               "   kod2 integer default 0, " +
                               "   done integer default 0, " +
                               "   nzp_user integer, " +
                               "   dat_when date ) ";
#else
                sql =
                               " Create table " + tbuf +
                               " ( nzp_kvar integer, " +
                               "   nzp_serv integer, " +
                               "   nzp_supp integer, " +
                               "   dat_s    date, " +
                               "   dat_po   date, " +
                               "   cnt_add  smallint, " +
                               "   kod1 integer default 0, " +
                               "   kod2 integer default 0, " +
                               "   done integer default 0, " +
                               "   nzp_user integer, " +
                               "   dat_when date ) ";
#endif

                ret = ExecSQL(conn_web, sql, true);
                if (!ret.result)
                {
                    conn_web.Close();
                    return false;
                }

                sql = " Create index ix_bfc_1 on " + tbuf + " (nzp_kvar,nzp_serv,nzp_supp,dat_s,dat_po) ";
                ret = ExecSQL(conn_web, sql, true);
                if (!ret.result)
                {
                    conn_web.Close();
                    return false;
                }
            }

#if PG
            sql =
                       " Insert into " + tbuf +
                       "  ( nzp_kvar, nzp_serv, nzp_supp, dat_s, dat_po, cnt_add, kod1, kod2, nzp_user, dat_when ) " +
                       " Values (" + zap.nzp_kvar.ToString() + "," +
                                     zap.nzp_serv.ToString() + "," +
                                     zap.nzp_supp.ToString() + "," +
                                     Utils.EStrNull(zap.dat_s) + "," +
                                     Utils.EStrNull(zap.dat_po) + "," +
                                     zap.cnt_add.ToString() + "," +
                                     zap.kod1.ToString() + "," +
                                     zap.kod2.ToString() + "," +
                                     zap.nzp_user.ToString() + ", today ) ";
#else
            sql =
                       " Insert into " + tbuf +
                       "  ( nzp_kvar, nzp_serv, nzp_supp, dat_s, dat_po, cnt_add, kod1, kod2, nzp_user, dat_when ) " +
                       " Values (" + zap.nzp_kvar.ToString() + "," +
                                     zap.nzp_serv.ToString() + "," +
                                     zap.nzp_supp.ToString() + "," +
                                     Utils.EStrNull(zap.dat_s) + "," +
                                     Utils.EStrNull(zap.dat_po) + "," +
                                     zap.cnt_add.ToString() + "," +
                                     zap.kod1.ToString() + "," +
                                     zap.kod2.ToString() + "," +
                                     zap.nzp_user.ToString() + ", today ) ";
#endif
            ret = ExecSQL(conn_web, sql, true);
            if (!ret.result)
            {
                conn_web.Close();
                return false;
            }

            return true;

        }//BufCalcInsert

        public Returns SaveMustCalcTXXspls(MustCalc finder, List<Service> services)
        {
            Returns ret = Utils.InitReturns();
            var result_text = "";
            if (finder.nzp_user <= 0) return new Returns(false, "Не задан пользователь");

            #region соединение conn_web
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return ret;
            #endregion

            #region наименование таблиц tXX_spls/tXX_spls_full
            string tXX_spls = "t" + Convert.ToString(finder.nzp_user) + "_spls";
#if PG
            string tXX_spls_full = "public." + tXX_spls;
#else
            string tXX_spls_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXX_spls;
#endif
            #endregion

            #region проверка существования таблицы tXX_spls в БД
            if (!TempTableInWebCashe(conn_web, tXX_spls_full))
            {
                ret.result = false;
                ret.text = "Нет таблицы " + tXX_spls;
                conn_web.Close();
                MonitorLog.WriteLog("Ошибка SaveMustCalc: Нет таблицы " + tXX_spls, MonitorLog.typelog.Error, 20, 201, true);
                return ret;
            }
            #endregion
#if PG
            string sql = "select distinct pref from " + tXX_spls_full + " where mark = 1";
#else
            string sql = "select unique pref from " + tXX_spls_full + " where mark = 1";
#endif
            IDbConnection conn_db = null;
            IDataReader reader = null;
            try
            {
                #region Открываем соединение с базами
                conn_db = GetConnection(Constants.cons_Kernel);
                ret = OpenDb(conn_db, true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка открытия соединения", MonitorLog.typelog.Error, true);
                    return ret;
                }
                #endregion

                if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
                {
                    ret.text = "Ошибка получения списка л/с";
                    MonitorLog.WriteLog(ret.text + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return ret;
                }

                //DbWorkUser db = new DbWorkUser();
                int localUser = 0;

                DbTables tables = new DbTables(conn_db);

                string nzp = "";
                foreach (Service serv in services)
                {
                    if (nzp == "") nzp += serv.nzp_serv;
                    else nzp += "," + serv.nzp_serv;
                }
                DateTime DateFrom, DateTo;
                DateTime.TryParse(finder.dat_s, out DateFrom);
                DateTime.TryParse(finder.dat_po, out DateTo);
                string suppWhere = finder.nzp_supp > 0 ? " and t.nzp_supp=" + finder.nzp_supp : "";
                if (reader != null)
                {
                    int i = 0;
                    while (reader.Read())
                    {
                        i++;
                        var mc = new MustCalc();

                        if (reader["pref"] != DBNull.Value) mc.pref = Convert.ToString(reader["pref"]).Trim();
                        // определить пользователя
                        finder.pref = mc.pref;
                        localUser = finder.nzp_user;
                        //localUser = db.GetLocalUser(conn_db, finder, out ret);

                        mc.nzp_supp = finder.nzp_supp;
                        RecordMonth r_m = Points.GetCalcMonth(new CalcMonthParams(mc.pref));
                        mc.month_ = r_m.month_;
                        mc.year_ = r_m.year_;

                        //проверка и ограничение периода перерасчета
                        ret = CheckPeriodForMustCalc(conn_db, mc, ref DateFrom, ref DateTo);
                        if (!ret.result)
                        {
                            return ret;
                        }
                        result_text = ret.text;

                        DateTime dt = new DateTime(r_m.year_, r_m.month_, 1);
                        if (DateFrom > DateTo)
                        {
                            ret.text = "Не верно задан период";
                            ret.result = false;
                            ret.tag = -1;
                            conn_db.Close();
                            conn_web.Close();
                            return ret;
                        }
                        if (dt < DateTo)
                        {
                            ret.text = "Период перерасчета должен затрагивать только прошлые периоды и оканчиваться до " + dt.ToShortDateString();
                            ret.result = false;
                            ret.tag = -1;
                            conn_db.Close();
                            conn_web.Close();
                            return ret;
                        }

                        mc.dat_s = DateFrom.ToShortDateString();
                        mc.dat_po = DateTo.ToShortDateString();
                        mc.kod1 = 7;
                        mc.kod2 = 0;

#if PG
                        string dbn = mc.pref + "_data.";
#else
                        string dbn = mc.pref + "_data" + "@" + DBManager.getServer(conn_db)+":";
#endif
                        sql = "insert into " + dbn + "must_calc " +
                         "(nzp_kvar,nzp_serv,nzp_supp,month_,year_,dat_s,dat_po,kod1," +
                         "kod2,nzp_user,dat_when) " +
                         "select distinct ls.nzp_kvar, t.nzp_serv, " + mc.nzp_supp + ", " + mc.month_ + ", " + mc.year_ + ", " +
                         "cast ('" + mc.dat_s + "' as date), cast ('" + mc.dat_po + "' as date), " + mc.kod1 + ", " + mc.kod2 + ", " + localUser +
                         ", " + sCurDateTime + " from " + tXX_spls_full + " ls, " + mc.pref + "_data.tarif t where mark = 1 " + suppWhere + " and ls.nzp_kvar=t.nzp_kvar and pref = '" + mc.pref + "' " +
                         " and t.nzp_serv in (" + nzp + ")";

                        if (!ExecSQL(conn_db, sql.ToString(), true).result)
                        {
                            ret.text = "Ошибка сохранения перерасчетов";
                            MonitorLog.WriteLog(ret.text + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                            ret.result = false;
                            conn_db.Close();
                            conn_web.Close();
                            return ret;
                        }
                    }

                    if (i == 0) ret = new Returns(false, "Нет выбранных л/с", -1);

                    reader.Close();
                    conn_db.Close();
                    conn_web.Close();
                }

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры SaveMustCalcTXXspls : " + ex.Message, MonitorLog.typelog.Error, true);
                return ret;
            }
            ret.text = result_text;
            return ret;
        }

        public Returns SaveMustCalc(MustCalc finder)
        {
            #region соединение conn_db
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            Returns ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;
            #endregion

            ret = SaveMustCalc(finder, conn_db);
            conn_db.Close();
            return ret;
        }

        public Returns SaveMustCalc(MustCalc finder, IDbConnection conn_db)
        {
            Returns ret = Utils.InitReturns();
            var result_text = "";
            if (finder.pref == "") return new Returns(false, "Не задан префикс");
            if (finder.nzp_kvar <= 0) return new Returns(false, "Не задан л/с");
            if (finder.nzp_serv <= 0) return new Returns(false, "Не задана услуга");
            //if (finder.nzp_supp <= 0) return new Returns(false, "Не задан поставщик");
            if (finder.month_ <= 0) return new Returns(false, "Не задан месяц");
            if (finder.year_ <= 0) return new Returns(false, "Не задан год");

            //период 
            DateTime dt_s;
            DateTime.TryParse(finder.dat_s, out dt_s);
            DateTime dt_po;
            DateTime.TryParse(finder.dat_po, out dt_po);

            //перерасчет ведется помесячно, приводим все даты в соответствии с этим правилом
            if (dt_s == DateTime.MinValue) return new Returns(false, "Не задан период");
            var dat_s = new DateTime(dt_s.Year, dt_s.Month, 1);
            if (dt_po == DateTime.MinValue) return new Returns(false, "Не задан период");
            var dat_po = new DateTime(dt_po.Year, dt_po.Month, DateTime.DaysInMonth(dt_po.Year, dt_po.Month));

            //проверка периода перерасчета и наложение ограничений на него
            ret = CheckPeriodForMustCalc(conn_db, finder, ref dat_s, ref dat_po);
            if (!ret.result)
            {
                return ret;
            }
            result_text = ret.text;

            //при включенном условии пишем периоды перерасчетов и по связанным услугам
            var services = new List<int>() { finder.nzp_serv };
            if (finder.for_slave_serv)
            {
                using (var srv = new DbServKernel())
                {
                    services.AddRange(srv.GetDependenciesServicesList(conn_db, null, finder.nzp_serv, 0, out ret));
                }
            }

            var active_services = new List<int>();
            //получаем список активных услуг в период перерасчета для этого ЛС
            var sql = " SELECT DISTINCT nzp_serv from " + finder.pref + sDataAliasRest + "tarif " +
                      " WHERE 1=1 "+
           // " is_actual<>100 "+
                      " AND " + Utils.EStrNull(dat_s.ToShortDateString()) + "<=dat_po AND dat_s<=" +
                      Utils.EStrNull(dat_po.ToShortDateString()) +
                      " AND nzp_kvar =" + finder.nzp_kvar;
            var dt = ClassDBUtils.OpenSQL(sql, conn_db).resultData;
            if (dt.Rows.Count > 0)
            {
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    active_services.Add(CastValue<int>(dt.Rows[i][0]));
                }
            }
            //получаем связанные услуги 
            services = services.Intersect(active_services).ToList();

            foreach (var service in services)
            {
                finder.nzp_serv = service;
                ret = CheckAndInsertMustCalc(finder, conn_db, dat_s, dat_po);
                if (!ret.result && ret.tag != 1)
                {
                    ret.text = "Ошибка сохранения периодов перерасчета";
                    return ret;
                }
            }

            ret.text = result_text;
            return ret;
        }

        private Returns CheckAndInsertMustCalc(MustCalc finder, IDbConnection conn_db, DateTime dat_s, DateTime dat_po)
        {
            Returns ret;
#if PG
            string dbn = finder.pref + "_data.";
#else
            string dbn = finder.pref + "_data" + "@" + DBManager.getServer(conn_db)+":";
#endif
            string sql = "select count(*) from " + dbn + "must_calc " +
                         " where nzp_kvar = " + finder.nzp_kvar + " and nzp_serv = " + finder.nzp_serv +
                         " and nzp_supp = " + finder.nzp_supp + " and month_ = " + finder.month_ + " and year_ = " +
                         finder.year_ + " and dat_s = '" + dat_s.ToShortDateString() + "' and dat_po = '" +
                         dat_po.ToShortDateString() + "' and kod1 = " +
                         finder.kod1 + " and kod2 = " + finder.kod2;

            object count = ExecScalar(conn_db, sql, out ret, true);
            int recordsTotalCount;
            try
            {
                recordsTotalCount = Convert.ToInt32(count);
            }
            catch (Exception e)
            {
                ret = new Returns(false, "Ошибка при определении количества записей: " + (Constants.Debug ? e.Message : ""));
                MonitorLog.WriteLog("Ошибка SaveMustCalc " + (Constants.Viewerror ? "\n" + e.Message : ""),
                    MonitorLog.typelog.Error, 20, 201, true);
                return ret;
            }

            if (recordsTotalCount > 0)
            {
                return new Returns(false, "Такая запись уже существует", -1);
            }

            int localUser = finder.nzp_user;

            /*DbWorkUser db = new DbWorkUser();
            int localUser = db.GetLocalUser(conn_db, finder, out ret);*/

#if PG
            dbn = finder.pref + "_data.";
#else
            dbn = finder.pref + "_data" + "@" + DBManager.getServer(conn_db)+":";
#endif
            sql = "insert  into " + dbn + "must_calc " +
                  "(nzp_kvar,nzp_serv,nzp_supp,month_,year_,dat_s,dat_po,kod1," +
                  "kod2,nzp_user,dat_when, comment) values " +
                  "(" + finder.nzp_kvar + "," + finder.nzp_serv + "," + finder.nzp_supp + "," + finder.month_ + "," +
                  finder.year_ + ",'" + dat_s.ToShortDateString() + "','" + dat_po.ToShortDateString() + "'," + finder.kod1 +
                  "," + finder.kod2 + "," +
                  localUser + "," + sCurDateTime + ",'" + finder.comment_action + "')";
            if (!ExecSQL(conn_db, sql, true).result)
            {
                ret.text = "Ошибка сохранения перерасчета";
                MonitorLog.WriteLog(ret.text + sql, MonitorLog.typelog.Error, 20, 201, true);
                ret.result = false;
                return ret;
            }

            #region Добавление в sys_events события 'Добавление перерасчёта'

            try
            {
#if PG
                dbn = Points.Pref + "_kernel.";
#else
                dbn = Points.Pref + "_kernel" + "@" + DBManager.getServer(conn_db)+":";
#endif
                var tsql = "select trim(service) from " + dbn + "services where nzp_serv = " + finder.nzp_serv;
                var serv = ExecScalar(conn_db, tsql, out ret, true);

                DbAdmin.InsertSysEvent(new SysEvents()
                {
                    pref = finder.pref,
                    nzp_user = finder.nzp_user,
                    nzp_dict = 6600,
                    nzp_obj = finder.nzp_kvar,
                    note = "Услуга: " + serv.ToString().Trim() + ". Период с " + finder.dat_s + " по " + finder.dat_po
                }, conn_db);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
            }

            #endregion

            return ret;
        }

        /// <summary>
        /// Функция проверки периода для перерасчета
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="finder"></param>
        /// <param name="dat_s"></param>
        /// <param name="dat_po"></param>
        /// <returns></returns>
        private Returns CheckPeriodForMustCalc(IDbConnection conn_db, MustCalc finder, ref DateTime dat_s, ref DateTime dat_po)
        {
            Returns ret;

            if (finder.pref == "") return new Returns(false, "Не задан префикс", -1);
            if (finder.month_ <= 0) return new Returns(false, "Не задан месяц", -1);
            if (finder.year_ <= 0) return new Returns(false, "Не задан год", -1);
            if (dat_s == DateTime.MinValue || dat_po == DateTime.MinValue) return new Returns(false, "Не задан период", -1);
            //текущий расчетный месяц
            DateTime dtCalcYear = new DateTime(finder.year_, finder.month_, 1);
            //максимальная дата из дат начала расчета системы и даты начала перерасчета
            var dateStartSystemOrRecalc =
                DBManager.CastValue<DateTime>(
                ExecScalar(conn_db, "SELECT max(val_prm) FROM " + finder.pref + sDataAliasRest +
                                    "prm_10  WHERE nzp_prm IN (82, 771) AND is_actual = 1", out ret, true));
            if (!ret.result)
            {
                ret.text =
                    "Ошибка получения параметров: \"Дата начала работы системы\" или \"Дата начала расчета/перерасчета \"";
                ret.tag = -1;
                return ret;
            }
            //перерасчет ведется помесячно, приводим все даты в соответствии с этим правилом
            dateStartSystemOrRecalc = new DateTime(dateStartSystemOrRecalc.Year, dateStartSystemOrRecalc.Month, 1);


            //проверка на корректность данных
            if (dat_s >= dat_po)
            {
                ret.tag = -1;
                ret.result = false;
                ret.text = "Дата начала периода перерасчета не может быть больше даты окончания периода";
                return ret;
            }

            //если период перерасчета целиком находится до даты начала расчета  
            //1) |--------------------|
            //   ----------------------------------(dateStart)---------------------------------------------
            if (dateStartSystemOrRecalc >= dat_po && dateStartSystemOrRecalc >= dat_s)
            {
                ret.tag = -1;
                ret.result = false;
                ret.text =
                    "Не возможно выставить период перерасчета до \"Дата начала работы системы\" или \"Дата начала расчета/перерасчета \"";
                return ret;
            }

            //если дата начала периода перерасчета < даты начала расчета, дата конца периода > даты начала расчета
            //2)                              |--------------------|
            //   ----------------------------------(dateStart)---------------------------------------------
            if (dateStartSystemOrRecalc < dat_po && dateStartSystemOrRecalc > dat_s)
            {
                ret.text =
                    "Дата начала периода перерасчета была ограничена параметром \"Дата начала работы системы\" или \"Дата начала расчета/перерасчета \"";
                //режем дату начала перерасчета
                dat_s = dateStartSystemOrRecalc;
            }
            //3)
            //ограничение по дате текущего расчетного месяца, в случае если период целиком выходит за пределы 
            if (dtCalcYear < dat_s && dtCalcYear < dat_po)
            {
                ret.tag = -1;
                ret.result = false;
                ret.text = "Период перерасчет не может быть больше, чем дата текущего расчетного месяца";
                return ret;
            }
            //4)
            //ограничение по дате окончания периода, когда дата начала меньше даты текущего РС, а дата окончания больше текущей даты РС
            if (dtCalcYear < dat_po)
            {
                ret.text = "Период действия перерасчета был ограничен датой текущего расчетного месяца";
                dat_po = dtCalcYear.AddDays(-1);
            }
            return ret;
        }

        public List<MustCalc> LoadMustCalc(MustCalc finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<MustCalc> list = new List<MustCalc>();

            if (finder.nzp_user <= 0)
            {
                ret.result = false;
                ret.text = "Не задан пользователь";
                return null;
            }

            if (finder.nzp_kvar <= 0)
            {
                ret.result = false;
                ret.text = "Не задан л/с";
                return null;
            }

            if (finder.pref == "")
            {
                ret.result = false;
                ret.text = "Не задан префикс";
                return null;
            }

            #region соединение conn_db
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;
            #endregion
            DbTables tables = new DbTables(conn_db);
            var temp_table = "t_must_calc_" + DateTime.Now.Ticks;
            var where = "";
            if (finder.month_ != 0 & finder.year_ != 0)
            {
                where = " and mc.year_=" + finder.year_ + " and mc.month_=" + finder.month_;
            }


            string sql =
                "SELECT mc.nzp_serv, max(mc.comment)  as mccomment, mc.nzp_supp, mc.month_, mc.year_, mc.dat_s, mc.dat_po, " +
                " mc.kod1, mc.kod2, max(s.service) as service" +
                ", max(sp.name_supp) as name_supp, max(mc.dat_when) as dat_when , max(u.comment) as comment, max(r.reason) as reason " +
                " INTO TEMP TABLE " + temp_table +
                " FROM " + finder.pref + "_data.must_calc mc " +
                " LEFT OUTER JOIN " + tables.services + " s ON mc.nzp_serv = s.nzp_serv " +
                " LEFT OUTER JOIN " + tables.supplier + " sp ON mc.nzp_supp = sp.nzp_supp " +
                " LEFT OUTER JOIN " + Points.Pref + "_data.users u ON u.nzp_user = mc.nzp_user " +
                " LEFT OUTER JOIN " + tables.reason + " r ON r.nzp_reason = mc.kod1 " +
                " WHERE mc.nzp_kvar = " + finder.nzp_kvar + " " + where + " " +
                " GROUP BY mc.nzp_serv,mc.nzp_supp, mc.year_ , mc.month_ ,  mc.dat_s,  mc.dat_po, mc.kod1, mc.kod2 " +
                " ORDER BY mc.year_ desc, mc.month_ desc, mc.nzp_serv, mc.dat_s";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
            {
                return list;
            }


            //Определить общее количество записей

            sql = "SELECT COUNT(*) FROM " + temp_table;
            object count = ExecScalar(conn_db, sql, out ret, true);
            int recordsTotalCount;
            try { recordsTotalCount = Convert.ToInt32(count); }
            catch (Exception e)
            {
                ret = new Returns(false, "Ошибка при определении количества записей: " + (Constants.Debug ? e.Message : ""));
                MonitorLog.WriteLog("Ошибка LoadMustCalc " + (Constants.Viewerror ? "\n" + e.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                conn_db.Close();
                return null;
            }


            sql = " SELECT nzp_serv, mccomment, nzp_supp, month_, year_, dat_s, dat_po, " +
                  " kod1, kod2,  service" +
                  ", name_supp,  dat_when ,  comment, reason " +
                  " FROM " + temp_table;

            IDataReader reader, reader2;
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }


            ExecSQL(conn_db, "DROP TABLE " + temp_table, false);

            try
            {
                int i = 0;
                while (reader.Read())
                {
                    i++;
                    if (i <= finder.skip) continue;
                    MustCalc mc = new MustCalc();

                    mc.nzp_kvar = finder.nzp_kvar;
                    mc.pref = finder.pref;
                    if (reader["nzp_serv"] != DBNull.Value) mc.nzp_serv = Convert.ToInt32(reader["nzp_serv"]);
                    if (reader["nzp_supp"] != DBNull.Value) mc.nzp_supp = Convert.ToInt32(reader["nzp_supp"]);
                    if (reader["service"] != DBNull.Value) mc.service = Convert.ToString(reader["service"]).Trim();
                    if (reader["name_supp"] != DBNull.Value) mc.supplier = Convert.ToString(reader["name_supp"]).Trim();
                    if (reader["dat_s"] != DBNull.Value)
                    {
                        mc.dat = String.Format("{0:dd.MM.yyyy}", reader["dat_s"]) + " - " +
                                 String.Format("{0:dd.MM.yyyy}", reader["dat_po"]);
                        mc.dat_s = String.Format("{0:dd.MM.yyyy}", reader["dat_s"]);
                        mc.dat_po = String.Format("{0:dd.MM.yyyy}", reader["dat_po"]);
                    }
                    if (reader["kod1"] != DBNull.Value) mc.kod1 = Convert.ToInt32(reader["kod1"]);
                    if (reader["reason"] != DBNull.Value) mc.kod1_str = Convert.ToString(reader["reason"]);
                    if (reader["mccomment"] != DBNull.Value)
                        mc.kod1_str += ": " + Convert.ToString(reader["mccomment"]);
                    if (reader["kod2"] != DBNull.Value) mc.kod2 = Convert.ToInt32(reader["kod2"]);
                    if (reader["month_"] != DBNull.Value)
                    {
                        mc.month_ = Convert.ToInt32(reader["month_"]);
                        string mnth = "";
                        switch (mc.month_)
                        {
                            case 1:
                                mnth = "январь";
                                break;
                            case 2:
                                mnth = "февраль";
                                break;
                            case 3:
                                mnth = "март";
                                break;
                            case 4:
                                mnth = "апрель";
                                break;
                            case 5:
                                mnth = "май";
                                break;
                            case 6:
                                mnth = "июнь";
                                break;
                            case 7:
                                mnth = "июль";
                                break;
                            case 8:
                                mnth = "август";
                                break;
                            case 9:
                                mnth = "сентябрь";
                                break;
                            case 10:
                                mnth = "октябрь";
                                break;
                            case 11:
                                mnth = "ноябрь";
                                break;
                            case 12:
                                mnth = "декабрь";
                                break;
                        }
                        mc.calcmonth = mnth + " " + Convert.ToString(reader["year_"]);
                        mc.year_ = Convert.ToInt32(reader["year_"]);
                    }
                    if (reader["dat_when"] != DBNull.Value)
                        mc.webUname = String.Format("{0:dd.MM.yyyy}", reader["dat_when"]);
                    if (reader["comment"] != DBNull.Value)
                        mc.webUname += " (" + Convert.ToString(reader["comment"]).Trim() + ")";

                    if (mc.kod1 == MustCalcReasons.Parameter.GetHashCode())
                    {
                        sql = "select name_prm from " + tables.prm_name + " where nzp_prm = " + mc.kod2;
                        ret = ExecRead(conn_db, out reader2, sql, true);
                        if (!ret.result)
                        {
                            reader.Close();
                            conn_db.Close();
                            return null;
                        }
                        if (reader2.Read())
                        {
                            if (reader2["name_prm"] != DBNull.Value)
                                mc.kod2_str = Convert.ToString(reader2["name_prm"]);
                        }
                        reader2.Close();
                    }
                    else if (mc.kod1 == MustCalcReasons.Counter.GetHashCode() ||
                             mc.kod1 == MustCalcReasons.DomCounter.GetHashCode())
                    {
#if PG
                        sql = "select cs.num_cnt, ct.cnt_stage, ct.name_type, cs.nzp_type from " + finder.pref +
                              "_data.counters_spis cs " +
                              " left outer join " + finder.pref +
                              "_kernel.s_counttypes ct on cs.nzp_cnttype = ct.nzp_cnttype " +
                              " where nzp_counter = " + mc.kod2;
#else
                        sql = "select cs.num_cnt, ct.cnt_stage, ct.name_type, cs.nzp_type from " + finder.pref + "_data@" + DBManager.getServer(conn_db) + ":counters_spis cs, " +
                                                      " outer " + finder.pref + "_kernel@" + DBManager.getServer(conn_db) + ":s_counttypes ct where " +
                                                      " cs.nzp_cnttype = ct.nzp_cnttype and nzp_counter = " + mc.kod2;
#endif
                        ret = ExecRead(conn_db, out reader2, sql, true);
                        if (!ret.result)
                        {
                            reader.Close();
                            conn_db.Close();
                            return null;
                        }
                        if (reader2.Read())
                        {
                            if (reader2["nzp_type"] != DBNull.Value)
                            {
                                if (Convert.ToInt32(reader2["nzp_type"]) == CounterKinds.Kvar.GetHashCode())
                                    mc.kod2_str = CounterKind.GetKindName(CounterKinds.Kvar);
                                if (Convert.ToInt32(reader2["nzp_type"]) == CounterKinds.Dom.GetHashCode())
                                    mc.kod2_str = CounterKind.GetKindName(CounterKinds.Dom);
                                if (Convert.ToInt32(reader2["nzp_type"]) == CounterKinds.Communal.GetHashCode())
                                    mc.kod2_str = CounterKind.GetKindName(CounterKinds.Communal);
                                if (Convert.ToInt32(reader2["nzp_type"]) == CounterKinds.Group.GetHashCode())
                                    mc.kod2_str = CounterKind.GetKindName(CounterKinds.Group);
                            }

                            mc.kod2_str += " прибор учета";

                            if (reader2["num_cnt"] != DBNull.Value)
                                mc.kod2_str += " № " + Convert.ToString(reader2["num_cnt"]).Trim();
                            if (reader2["name_type"] != DBNull.Value)
                                mc.kod2_str += ", тип " + Convert.ToString(reader2["name_type"]).Trim();
                            if (reader2["cnt_stage"] != DBNull.Value)
                                mc.kod2_str += " (разр. " + Convert.ToString(reader2["cnt_stage"]).Trim() + ")";

                        }
                        reader2.Close();
                    }



                    list.Add(mc);
                    if (finder.rows > 0 && i >= finder.skip + finder.rows) break;
                }
                ret.tag = recordsTotalCount;

                return list;
            }
            catch (Exception ex)
            {
                CloseReader(ref reader);
                conn_db.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка получения LoadMustCalc " + err, MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
        }

        public Returns DeleteMustCalc(MustCalc finder)
        {
            if (finder.nzp_user <= 0) return new Returns(false, "Пользователь не задан");
            if (finder.nzp_kvar <= 0) return new Returns(false, "nzp_kvar не задан");
            if (finder.nzp_serv <= 0) return new Returns(false, "nzp_serv не задан");
            //  if (finder.nzp_supp <= 0) return new Returns(false, "nzp_supp не задан");
            if (finder.month_ <= 0) return new Returns(false, "month_ не задан");
            if (finder.year_ <= 0) return new Returns(false, "year_ не задан");
            if (finder.dat_s == "") return new Returns(false, "dat_s не задан");
            if (finder.dat_po == "") return new Returns(false, "dat_po не задан");
            if (finder.pref == "") return new Returns(false, "pref не задан");

            #region соединение conn_db
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            Returns ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;
            #endregion

#if PG
            string dbn = finder.pref + "_data.";
#else
            string dbn = finder.pref + "_data@" + DBManager.getServer(conn_db) + ":";
#endif
            string sql = "delete from " + dbn + "must_calc " +
                         " where nzp_kvar =" + finder.nzp_kvar + " and nzp_serv = " + finder.nzp_serv + " and " +
                         " nzp_supp = " + finder.nzp_supp + " and month_ = " + finder.month_ + " and " +
                         " year_ = " + finder.year_ + " and dat_s = '" + finder.dat_s + "' and " +
                         " dat_po = '" + finder.dat_po + "' and kod1 = " + finder.kod1 + " and " +
                         " kod2 = " + finder.kod2;

            if (!ExecSQL(conn_db, sql.ToString(), true).result)
            {
                ret.text = "Ошибка удаления перерасчета";
                MonitorLog.WriteLog(ret.text + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                ret.result = false;
                return ret;
            }

            #region Добавление в sys_events события 'Удаление перерасчёта'
            try
            {
#if PG
                dbn = Points.Pref + "_kernel.";
#else
                dbn = Points.Pref + "_kernel" + "@" + DBManager.getServer(conn_db)+":";
#endif
                var tsql = "select service from " + dbn + "services where nzp_serv = " + finder.nzp_serv;
                var serv = ExecScalar(conn_db, tsql, out ret, true);

                DbAdmin.InsertSysEvent(new SysEvents()
                {
                    pref = finder.pref,
                    nzp_user = finder.nzp_user,
                    nzp_dict = 6601,
                    nzp_obj = finder.nzp_kvar,
                    note = "Услуга: " + serv.ToString().Trim() + ". Период с " + finder.dat_s + " по " + finder.dat_po
                }, conn_db);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
            }
            #endregion

            conn_db.Close();
            return ret;
        }

        [Obsolete("Заменен на CheckPeriodForMustCalc")]
        private Returns checkDateForMustCalc(IDbConnection conn_db, ref DateTime datS, DateTime datPo, string pref)
        {
            Returns ret = Utils.InitReturns();
            #region Проверка выхода задаваемой даты за пределы даты работы системы
            string sql =
                "select max(val_prm) as prm from " + pref + "_data.prm_10 where nzp_prm=771 OR nzp_prm=82";

            string sysDatS = "";
            //string sysDatPo = "";
            IDataReader reader = null;
            try
            {

                ret = ExecRead(conn_db, out reader, sql, true);
                if (!ret.result)
                {
                    ret.text = "Ошибка сохранения перерасчета";
                    return ret;
                }
                while (reader.Read())
                {
                    if (reader["prm"] != DBNull.Value) sysDatS = reader["prm"].ToString();
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                ret.tag = -1;
                ret.text = ex.Message;
                ret.result = false;
                MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
            }
            finally
            {
                if (reader != null) reader.Close();
                conn_db.Close();
            }
            DateTime parsedDatPrm;
            if (sysDatS != "")
            {
                if (!DateTime.TryParse(sysDatS, out parsedDatPrm))
                {
                    ret.text = "Ошибка сохранения перерасчета";
                    MonitorLog.WriteLog("Ошибка преобразования переменной типа string sysDatS в тип DateTime в методе checkDateForMustCalc()", MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return ret;
                }
                if (datPo <= parsedDatPrm)
                {
                    ret.result = false;
                    ret.tag = -1;
                    ret.text = "Конечная дата должна быть больше, чем " + parsedDatPrm.ToShortDateString();
                    return ret;
                }
                if (datS < parsedDatPrm)
                {
                    datS = parsedDatPrm;
                }
            }
            #endregion

            return ret;
        }

        public List<ProhibitedMustCalc> GetProhibitedMustCalc(ProhibitedMustCalc finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            var list = new List<ProhibitedMustCalc>();
            if (finder.nzp_user <= 0)
            {
                ret.result = false;
                ret.text = "Не задан пользователь";
                return null;
            }

            if (finder.nzp_kvar <= 0)
            {
                ret.result = false;
                ret.text = "Не задан л/с";
                return null;
            }

            if (finder.pref == "")
            {
                ret.result = false;
                ret.text = "Не задан префикс";
                return null;
            }

            #region соединение conn_db
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;
            #endregion
            IDataReader reader = null;
            try
            {

                var sql = "select mc.*,s.service,sp.name_supp supplier from " + Points.Pref + sDataAliasRest + "prohibited_recalc mc " +
                          "LEFT OUTER JOIN " + Points.Pref + sKernelAliasRest +
                          "services s ON mc.nzp_serv = s.nzp_serv " +
                          "LEFT OUTER JOIN " + Points.Pref + sKernelAliasRest +
                          "supplier sp ON mc.nzp_supp = sp.nzp_supp where nzp_kvar = "
                          + finder.nzp_kvar + " and is_actual <> 100 order by dat_s,dat_po";
                ret = ExecRead(conn_db, out reader, sql, true);
                if (!ret.result)
                {
                    return null;
                }
                while (reader.Read())
                {
                    list.Add(new ProhibitedMustCalc
                    {
                        nzp_serv = CastValue<int>(reader["nzp_serv"]),
                        nzp_supp = CastValue<int>(reader["nzp_supp"]),
                        supplier = CastValue<string>(reader["supplier"]),
                        service = CastValue<string>(reader["service"]),
                        dat = CastValue<DateTime>(reader["dat_s"]).ToShortDateString() + " - " + CastValue<DateTime>(reader["dat_po"]).ToShortDateString(),
                        dat_s = CastValue<DateTime>(reader["dat_s"]).ToShortDateString(),
                        dat_po = CastValue<DateTime>(reader["dat_po"]).ToShortDateString(),
                        nzp_kvar = CastValue<int>(reader["nzp_kvar"]),
                        nzp_dom = CastValue<int>(reader["nzp_dom"]),
                        prohibited_id = CastValue<int>(reader["id"])
                    });
                }
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
            return list;
        }

        public void SaveProhibitedMustCalc(ProhibitedMustCalc finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            if (finder.nzp_user <= 0)
            {
                ret.result = false;
                ret.text = "Не задан пользователь";
                return;
            }

            if (finder.nzp_kvar <= 0)
            {
                ret.result = false;
                ret.text = "Не задан л/с";
                return;
            }

            if (finder.pref == "")
            {
                ret.result = false;
                ret.text = "Не задан префикс";
                return;
            }
            DateTime dt_s = DateTime.MinValue, dat_s;
            DateTime.TryParse(finder.dat_s, out dt_s);
            if (dt_s == DateTime.MinValue)
            {
                ret = new Returns(false, "Не задан период");
                return;
            }
            DateTime dt_po = DateTime.MinValue, dat_po;
            DateTime.TryParse(finder.dat_po, out dt_po);
            if (dt_po == DateTime.MinValue)
            {
                ret = new Returns(false, "Не задан период");
                return;
            }
            if (dt_po < dt_s)
            {
                ret = new Returns(false, "Не верно задан период");
                return;
            }
            #region соединение conn_db
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return;
            #endregion
            try
            {
                if (finder.nzp_serv > 0 && finder.nzp_supp > 0)
                {
                    var sql =
                        string.Format(
                            " select count(*) from {0}prohibited_recalc where is_actual <> 100 and nzp_dom = {1} and nzp_kvar = {2} and nzp_serv = {3} and nzp_supp = {4} " +
                            " and (('{5}' between dat_s and dat_po) or ('{6}' between dat_s and dat_po))",
                            Points.Pref + sDataAliasRest, finder.nzp_dom, finder.nzp_kvar, finder.nzp_serv,
                            finder.nzp_supp, finder.dat_s, finder.dat_po);
                    var count = CastValue<int>(ExecScalar(conn_db, sql, out ret, true));
                    if (count > 0)
                    {
                        ret = new Returns(false, "Данный период уже используется");
                        return;
                    }
                    sql = "insert into " + Points.Pref + sDataAliasRest +
                          "prohibited_recalc(nzp_kvar,nzp_dom,nzp_serv,nzp_supp,dat_s,dat_po,is_actual) values" +
                          " (" + finder.nzp_kvar + "," + finder.nzp_dom + "," + finder.nzp_serv + "," + finder.nzp_supp +
                          ",'" + finder.dat_s + "','" + finder.dat_po + "',1)";
                    ret = ExecSQL(conn_db, sql, true);
                }
                else
                {
                    var where = "";
                    if (finder.nzp_serv > 0)
                    {
                        where += " and t.nzp_serv = " + finder.nzp_serv;
                    }
                    else if (finder.nzp_supp > 0)
                    {
                        where += " and t.nzp_supp = " + finder.nzp_supp;
                    }

                    var sql = string.Format("insert into {4}prohibited_recalc(nzp_kvar,nzp_dom,nzp_serv,nzp_supp,dat_s,dat_po,is_actual)" +
                          " select t.nzp_kvar,k.nzp_dom,t.nzp_serv,t.nzp_supp,'{0}','{1}',1 from {2}tarif t,{2}kvar k where t.nzp_kvar = k.nzp_kvar and " +
                          " (('{0}' between t.dat_s and t.dat_po) or ('{1}' between t.dat_s and t.dat_po)) and t.nzp_kvar = {5} and t.is_actual <> 100 and " +
                          " not exists ( select 1 from {4}prohibited_recalc pr where ((pr.dat_po between t.dat_s and t.dat_po) or (pr.dat_s between t.dat_s and t.dat_po))" +
                          " and pr.nzp_kvar = t.nzp_kvar and pr.nzp_serv = t.nzp_serv and pr.nzp_supp = t.nzp_supp and pr.is_actual <> 100) "
                          + where, finder.dat_s, finder.dat_po, finder.pref + sDataAliasRest, finder.nzp_serv, Points.Pref + sDataAliasRest, finder.nzp_kvar);
                    ret = ExecSQL(conn_db, sql, true);
                }
            }
            finally
            {
                conn_db.Close();
            }
        }

        public void DeleteProhibitedMustCalc(ProhibitedMustCalc finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            if (finder.nzp_user <= 0)
            {
                ret.result = false;
                ret.text = "Не задан пользователь";
                return;
            }

            #region соединение conn_db
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return;
            #endregion

            try
            {
                var sql = "delete from " + Points.Pref + sDataAliasRest + "prohibited_recalc where id =" + finder.prohibited_id;
                ret = ExecSQL(conn_db, sql, true);
            }
            finally
            {
                conn_db.Close();
            }
        }

        public Returns SaveDisableMustCalcTxXspls(MustCalc finder, List<Service> services)
        {
            var ret = Utils.InitReturns();
            if (finder.nzp_user <= 0) return new Returns(false, "Не задан пользователь");

            #region соединение conn_web
            var conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return ret;
            #endregion

            #region наименование таблиц tXX_spls/tXX_spls_full
            var tXX_spls = "t" + Convert.ToString(finder.nzp_user) + "_spls";
#if PG
            var tXX_spls_full = "public." + tXX_spls;
#else
            string tXX_spls_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXX_spls;
#endif
            #endregion

            #region проверка существования таблицы tXX_spls в БД
            if (!TempTableInWebCashe(conn_web, tXX_spls_full))
            {
                ret.result = false;
                ret.text = "Нет таблицы " + tXX_spls;
                conn_web.Close();
                MonitorLog.WriteLog("Ошибка SaveDisableMustCalcTXXspls: Нет таблицы " + tXX_spls, MonitorLog.typelog.Error, 20, 201, true);
                return ret;
            }
            #endregion
#if PG
            var sql = "select distinct pref from " + tXX_spls_full + " where mark = 1";
#else
            var sql = "select unique pref from " + tXX_spls_full + " where mark = 1";
#endif
            IDbConnection conn_db = null;
            IDataReader reader = null;
            try
            {
                #region Открываем соединение с базами

                conn_db = GetConnection(Constants.cons_Kernel);
                ret = OpenDb(conn_db, true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка открытия соединения", MonitorLog.typelog.Error, true);
                    return ret;
                }

                #endregion

                if (!ExecRead(conn_db, out reader, sql, true).result)
                {
                    ret.text = "Ошибка получения списка л/с";
                    MonitorLog.WriteLog(ret.text + sql, MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return ret;
                }

                var nzp = "";
                foreach (Service serv in services)
                {
                    if (nzp == "") nzp += serv.nzp_serv;
                    else nzp += "," + serv.nzp_serv;
                }
                DateTime DateFrom, DateTo;
                DateTime.TryParse(finder.dat_s, out DateFrom);
                DateTime.TryParse(finder.dat_po, out DateTo);
                var suppWhere = finder.nzp_supp > 0 ? " and t.nzp_supp=" + finder.nzp_supp : "";
                if (reader != null)
                {
                    var i = 0;
                    while (reader.Read())
                    {
                        i++;
                        var pref = "";
                        if (reader["pref"] != DBNull.Value) pref = Convert.ToString(reader["pref"]).Trim();
                        var r_m = Points.GetCalcMonth(new CalcMonthParams(pref));
                        var mc = new MustCalc
                        {
                            nzp_supp = finder.nzp_supp,
                            pref = pref,
                            month_ = r_m.month_,
                            year_ = r_m.year_,
                            dat_s = DateFrom.ToShortDateString(),
                            dat_po = DateTo.ToShortDateString()
                        };

                        finder.pref = mc.pref;
                        if (reader["pref"] != DBNull.Value) mc.pref = Convert.ToString(reader["pref"]).Trim();
#if PG
                        var dbn = mc.pref + "_data.";
#else
                        var dbn = mc.pref + "_data" + "@" + DBManager.getServer(conn_db)+":";
#endif
                        sql = "delete from " + Points.Pref + sDataAliasRest +
                              " prohibited_recalc t where t. nzp_kvar in (select nzp_kvar from  " + tXX_spls_full +
                              ") " + suppWhere +
                              " and (cast ('" + mc.dat_s + "' as date) between dat_s and dat_po) and (cast ('" +
                              mc.dat_po + "' as date) between dat_s and dat_po)" +
                              " and t.nzp_serv in (" + nzp + ")";
                        ret = ExecSQL(conn_db, sql, true);
                        if (!ret.result)
                        {
                            ret.text = "Ошибка сохранения периодов запрета перерасчета";
                            MonitorLog.WriteLog(ret.text + sql, MonitorLog.typelog.Error, 20, 201, true);
                            conn_db.Close();
                            conn_web.Close();
                            return ret;
                        }

#if PG
                        dbn = mc.pref + "_data.";
#else
                         dbn = mc.pref + "_data" + "@" + DBManager.getServer(conn_db)+":";
#endif
                        sql = "insert into " + Points.Pref + sDataAliasRest +
                              " prohibited_recalc(nzp_kvar,nzp_dom,nzp_serv,nzp_supp,dat_s,dat_po,is_actual) " +
                              " select distinct ls.nzp_kvar,ls.nzp_dom, t.nzp_serv, " + mc.nzp_supp + ", " +
                              " cast ('" + mc.dat_s + "' as date), cast ('" + mc.dat_po + "' as date),1 from " +
                              tXX_spls_full + " ls, "
                              + mc.pref + "_data.tarif t where mark = 1 " + suppWhere +
                              " and ls.nzp_kvar=t.nzp_kvar and pref = '" + mc.pref + "' " +
                              " and t.nzp_serv in (" + nzp + ")";

                        ret = ExecSQL(conn_db, sql, true);
                        if (!ret.result)
                        {
                            ret.text = "Ошибка сохранения перерасчетов";
                            MonitorLog.WriteLog(ret.text + sql, MonitorLog.typelog.Error, 20, 201, true);
                            conn_db.Close();
                            conn_web.Close();
                            return ret;
                        }
                    }

                    if (i == 0) ret = new Returns(false, "Нет выбранных л/с", -1);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры SaveDisableMustCalcTXXspls : " + ex.Message,
                    MonitorLog.typelog.Error, true);
                return ret;
            }
            finally
            {
                reader.Close();
                conn_db.Close();
                conn_web.Close();
            }
            return ret;
        }

        public List<Service> LoadSuppliersForDisableMustCalcLs(Service finder, out Returns ret)
        {
            var listSuppliers = new List<Service>();
            #region Проверка входных параметров
            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Не определен пользователь");
                return listSuppliers;
            }
            #endregion
            #region наименование таблиц tXX_spls/tXX_spls_full
            var tXX_spls = "t" + Convert.ToString(finder.nzp_user) + "_spls";
#if PG
            var tXX_spls_full = "public." + tXX_spls;
#else
            string tXX_spls_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXX_spls;
#endif
            #endregion
            ret = Utils.InitReturns();
            #region соединение
            var connectionString = Points.GetConnByPref(Points.Pref);
            var conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;
            IDataReader reader;
            var sql = "select distinct pref from " + tXX_spls_full + " where mark = 1";
            if (!ExecRead(conn_db, out reader, sql, true).result)
            {
                ret.text = "Ошибка получения списка л/с";
                MonitorLog.WriteLog(ret.text + sql, MonitorLog.typelog.Error, 20, 201, true);
                ret.result = false;
                return listSuppliers;
            }
            while (reader.Read())
            {
            #endregion

                sql = "select distinct t.nzp_supp, su.name_supp from " + CastValue<string>(reader["pref"]).Trim() + "_data.tarif t, " + CastValue<string>(reader["pref"]).Trim() +
                      "_kernel.supplier su " +
                      "WHERE t.nzp_kvar in (select nzp_kvar from " + tXX_spls_full +
                      ") and t.nzp_supp=su.nzp_supp order by name_supp";
                try
                {
                    listSuppliers = Query<Service>(conn_db, sql, out ret);
                }
                catch (Exception ex)
                {
                    ret.tag = -1;
                    ret.text = ex.Message;
                    ret.result = false;
                    MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                }
                finally
                {
                    conn_db.Close();
                }
            }
            reader.Close();
            return listSuppliers.Distinct().ToList();
        }


        public List<Service> LoadServiceForDisableMustCalcLs(Service finder, out Returns ret)
        {
            var listServices = new List<Service>();
            #region Проверка входных параметров
            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Не определен пользователь");
                return listServices;
            }
            #endregion
            #region наименование таблиц tXX_spls/tXX_spls_full
            var tXX_spls = "t" + Convert.ToString(finder.nzp_user) + "_spls";
#if PG
            var tXX_spls_full = "public." + tXX_spls;
#else
            string tXX_spls_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXX_spls;
#endif
            #endregion
            ret = Utils.InitReturns();
            #region соединение
            var connectionString = Points.GetConnByPref(Points.Pref);
            var conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;
            #endregion
            IDataReader reader;
            var sql = "select distinct pref from " + tXX_spls_full + " where mark = 1";
            if (!ExecRead(conn_db, out reader, sql, true).result)
            {
                ret.text = "Ошибка получения списка л/с";
                MonitorLog.WriteLog(ret.text + sql, MonitorLog.typelog.Error, 20, 201, true);
                ret.result = false;
                return listServices;
            }
            while (reader.Read())
            {
                sql = "select distinct t.nzp_serv, s.service_name  from " + CastValue<string>(reader["pref"]).Trim() + "_data.tarif t, " +
                      Points.Pref + "_kernel.services s " +
                      "WHERE t.nzp_kvar in (select nzp_kvar from " + tXX_spls_full +
                      ") and t.nzp_serv=s.nzp_serv order by service_name";
                try
                {
                    listServices = Query<Service>(conn_db, sql, out ret);
                }
                catch (Exception ex)
                {
                    ret.tag = -1;
                    ret.text = ex.Message;
                    ret.result = false;
                    MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                }
                finally
                {
                    conn_db.Close();
                }
            }
            reader.Close();
            return listServices.Distinct().ToList();
        }
    }
}
