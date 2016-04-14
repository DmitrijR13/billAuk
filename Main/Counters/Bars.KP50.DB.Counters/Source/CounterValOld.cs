namespace STCLINE.KP50.DataBase
{
    //----------------------------------------------------------------------
    public partial class DbCounter : DbCounterKernel
    //----------------------------------------------------------------------
    {
//#warning функции, работающие с counters, counters_dom, counters_group через counters_vals. Не использовать
//        /// <summary>
//        /// Копирование показателя ПУ
//        /// </summary>
//        private void CopyCounterVal(CounterVal _new, CounterVal _old)
//        {
//            _new.dat_uchet = _old.dat_uchet;
//            _new.nzp_user = _old.nzp_user;
//            _new.webLogin = _old.webLogin;
//            _new.webUname = _old.webUname;
//            _new.ist = _old.ist;

//            _new.year_ = _old.year_;
//            _new.month_ = _old.month_;
//            _new.pref = _old.pref;
//            _new.cnt_stage = _old.cnt_stage;
//            _new.val_cnt_s = _old.val_cnt_s;
//            _new.nzp_cv = _old.nzp_cv;
//            _new.nzp_counter = _old.nzp_counter;

//            _new.nzp_type = _old.nzp_type;
//            _new.ngp_cnt = _old.ngp_cnt;
//            _new.ngp_lift = _old.ngp_lift;
//            _new.sred_rashod = _old.sred_rashod;
//        }

//        /// <summary>
//        /// Сохранить текущие показания ПУ
//        /// </summary>
//#warning функции, работающие с counters, counters_dom, counters_group через counters_vals. Не использовать
//        public Returns SaveCountersCurrVals(List<CounterVal> newVals)
//        {
//            Returns ret = Utils.InitReturns();

//            #region проверка параметров
//            //------------------------------------------------------------
//            if (newVals[0].nzp_user < 1)
//            {
//                return new Returns(false, "Не определен пользователь", -1);
//            }
//            //------------------------------------------------------------
//            #endregion

//            #region установка подключений
//            //------------------------------------------------------------
//            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
//            ret = OpenDb(conn_web, true);
//            if (!ret.result) return ret;
//#if PG
//            ExecSQL(conn_web, "set search_path to 'public'", false);
//#endif

//            string conn_kernel = Points.GetConnByPref(Points.Pref);
//            IDbConnection conn_db = GetConnection(conn_kernel);

//            ret = OpenDb(conn_db, true);
//            if (!ret.result)
//            {
//                conn_web.Close();
//                return ret;
//            }
//            //------------------------------------------------------------
//            #endregion

//            #region копирование списка
//            //------------------------------------------------------------
//            List<CounterVal> tmpCounterVal = new List<CounterVal>();
//            for (int i = 0; i < newVals.Count; i++)
//            {
//                CounterVal tmp_cv = new CounterVal();
//                CopyCounterVal(tmp_cv, newVals[i]);
//                tmpCounterVal.Add(tmp_cv);
//            }
//            //------------------------------------------------------------
//            #endregion

//            #region сохранение данных в банк
//            //------------------------------------------------------------
//            List<CounterVal> prefCounterVal = new List<CounterVal>();
//            CounterVal cur_cv = new CounterVal();

//            DbParameters db = new DbParameters();
//            Param prm = new Param();
//            DateTime monthBegin = new DateTime(newVals[0].year_, newVals[0].month_, 1);
//            DateTime monthEnd = monthBegin.AddMonths(1).AddDays(-1);

//            while (true)
//            {
//                if (newVals.Count <= 0) break;

//                // формирование списка
//                //----------------------------------------------------------------
//                cur_cv.pref = newVals[0].pref;
//                prefCounterVal.Clear();

//                int i = 0;
//                while (true)
//                {
//                    if (newVals[i].pref == cur_cv.pref)
//                    {
//                        CounterVal tmp_cv = new CounterVal();
//                        CopyCounterVal(tmp_cv, newVals[i]);
//                        prefCounterVal.Add(tmp_cv);
//                        newVals.RemoveAt(i);
//                    }
//                    else i++;

//                    if (!(i < newVals.Count)) break;
//                }
//                //----------------------------------------------------------------

//                if (prefCounterVal.Count > 0)
//                {
//                    ret = SaveCountersVals(prefCounterVal);
//                    if (!ret.result) break;
//                }

//                #region сохранить средний расход для общедомовых приборов учета
//                //------------------------------------------------------------
//                for (int j = 0; j < prefCounterVal.Count; j++)
//                {
//                    if (tmpCounterVal[j].nzp_type == (int)CounterKinds.Dom)
//                    {
//                        prm.dat_s = monthBegin.ToShortDateString();
//                        prm.dat_po = monthEnd.ToShortDateString();
//                        prm.nzp_user = prefCounterVal[j].nzp_user;
//                        prm.webLogin = prefCounterVal[j].webLogin;
//                        prm.webUname = prefCounterVal[j].webUname;
//                        prm.pref = prefCounterVal[j].pref;
//                        prm.nzp = prefCounterVal[j].nzp_counter;
//                        prm.nzp_prm = 979;
//                        prm.val_prm = prefCounterVal[j].sred_rashod.ToString();
//                        prm.prm_num = 17;

//                        if (prefCounterVal[j].sred_rashod.Length > 0)
//                        {
//                            ret = db.SavePrm(conn_db, null, prm);
//                            if (!ret.result) break;
//                        }
//                        else
//                        {
//                            prm.prms = Constants.act_del_val.ToString();
//                            ret = db.SavePrm(conn_db, null, prm);
//                            if (!ret.result) break;
//                        }
//                    }
//                }
//                //------------------------------------------------------------
//                #endregion
//            }
//            //------------------------------------------------------------
//            #endregion
//            if (!ret.result) return ret;

//            string tXX_cv = "t" + Convert.ToString(tmpCounterVal[0].nzp_user) + "_cv";
//            string sql = "";

//            #region обновление данных в кэше
//            //----------------------------------------------------------------------------
//            for (int i = 0; i < tmpCounterVal.Count; i++)
//            {
//                if (tmpCounterVal[i].nzp_type == (int)CounterKinds.Kvar)
//                {
//                    tXX_cv = "t" + Convert.ToString(tmpCounterVal[0].nzp_user) + "_cv";

//                    sql = " Update " + tXX_cv + " Set dat_uchet = " + Utils.EStrNull(tmpCounterVal[i].dat_uchet) +
//                            ", val_cnt = " + ((tmpCounterVal[i].val_cnt_s == "") ? " null " : tmpCounterVal[i].val_cnt_s) +
//                            ", user_ = " + Utils.EStrNull(tmpCounterVal[i].webUname) + ", dat_when = " + DBManager.sCurDateTime +
//                            (Points.IsIpuHasNgpCnt ? ", ngp_cnt = " + tmpCounterVal[i].ngp_cnt : "") +
//                        " Where nzp_cv = " + tmpCounterVal[i].nzp_cv.ToString();
//                }
//                else if (tmpCounterVal[i].nzp_type == (int)CounterKinds.Dom)
//                {
//                    tXX_cv = "t" + Convert.ToString(tmpCounterVal[0].nzp_user) + "_dom_cv";

//                    sql = " Update " + tXX_cv + " Set dat_uchet = " + Utils.EStrNull(tmpCounterVal[i].dat_uchet) +
//                            ", val_cnt = " + ((tmpCounterVal[i].val_cnt_s == "") ? " null " : tmpCounterVal[i].val_cnt_s) +
//                            ", ngp_cnt = " + tmpCounterVal[i].ngp_cnt +
//                            ", ngp_lift = " + tmpCounterVal[i].ngp_lift +
//                            (tmpCounterVal[i].sred_rashod.Length > 0 ? ", sred_rashod = " + tmpCounterVal[i].sred_rashod : ", sred_rashod = null") +
//                            ", user_ = " + Utils.EStrNull(tmpCounterVal[i].webUname) + ", dat_when = " + DBManager.sCurDateTime +
//                        " Where nzp_cv = " + tmpCounterVal[i].nzp_cv.ToString();
//                }

//                ret = ExecSQL(conn_web, sql, true);

//                if (!ret.result)
//                {
//                    conn_web.Close();
//                    break;
//                }
//            }
//            //----------------------------------------------------------------------------
//            #endregion

//            return ret;
//        }
        
//        /// <summary>
//        /// Проверить возможность сохранения текущих показаний ПУ - НЕ ИСПОЛЬЗУЕТСЯ
//        /// </summary>
//#warning функции, работающие с counters, counters_dom, counters_group через counters_vals. Не использовать
//        public List<string> CheckSaveCounterVals(List<CounterVal> newVals, out Returns ret)
//        {
//            ret = Utils.InitReturns();
//            List<string> err = new List<string>();

//            #region проверка параметров
//            //------------------------------------------------------------
//            if (newVals[0].nzp_user < 1)
//            {
//                ret.result = false;
//                ret.tag = -1;
//                ret.text = "Не определен пользователь";
//                return null;
//            }
//            //------------------------------------------------------------
//            #endregion

//            #region проверка параметров
//            //------------------------------------------------------------    
//            string conn_kernel = Points.GetConnByPref(Points.Pref);
//            IDbConnection conn_db = GetConnection(conn_kernel);

//            ret = OpenDb(conn_db, true);
//            if (!ret.result) return null;
//            //------------------------------------------------------------
//            #endregion

//            object obj;
//            string sql = "";
//            int cnt;

//            try
//            {
//                for (int i = 0; i < newVals.Count; i++)
//                {
//                    sql = "Select count(*) From " + newVals[i].pref + "_charge_" + (DateTime.Parse(newVals[i].dat_uchet).Year % 100).ToString("00") + DBManager.tableDelimiter + "counters_vals v" +
//                        " Where v.dat_uchet = " + Utils.EStrNull(DateTime.Parse(newVals[i].dat_uchet).ToShortDateString()) +
//                        "   and v.ist = " + (int)CounterVal.Ist.Operator +
//                        "   and v.nzp_counter = " + newVals[i].nzp_counter + " and v.nzp_cv <> " + newVals[i].nzp_cv;

//                    obj = ExecScalar(conn_db, sql, out ret, true);
//                    if (!ret.result) throw new Exception(ret.text);

//                    try
//                    { cnt = Convert.ToInt32(obj); }
//                    catch
//                    { cnt = 0; }

//                    if (cnt > 0) err.Add("Для прибора учета в строке " + newVals[i].num + " существует показание на дату " + newVals[i].dat_uchet);
//                }
//            }
//            catch (Exception ex)
//            {
//                err.Clear();
//                ret = new Returns(false, ex.Message, -1);
//                conn_db.Close();
//                return null;
//            }

//            conn_db.Close();
//            return err;
//        }

//#warning функции, работающие с counters, counters_dom, counters_group через counters_vals. Не использовать
//        public List<CounterVal> GetCountersVals(CounterVal finder, out Returns ret)
//        {
//            if (Constants.Trace) Utility.ClassLog.WriteLog("Старт функции GetCountersVals");
//            ret = Utils.InitReturns();
//            IDataReader reader = null;
//            IDataReader reader2 = null;
//            IDbConnection conn_db = null;
//            try
//            {
//                if (finder.ist == (int)CounterVal.Ist.Operator)
//                {
//                    ret = PrepareCountersVals(finder, PrepareCounterValMode.SpisVal); // создается таблица counters_vals, в нее записываются значения в соответствии с параметрами finder
//                }
//                else
//                {
//                    ret = ValidateCountersValsParams(finder);
//                }
//                if (!ret.result) return null;
//                List<CounterVal> Spis = new List<CounterVal>();

//                #region Соединение с БД
//                string connectionString = Points.GetConnByPref(finder.pref);
//                conn_db = GetConnection(connectionString);
//                ret = OpenDb(conn_db, true);
//                if (!ret.result) return null;
//                #endregion

//                #region определение локального пользователя
//                DbWorkUser db = new DbWorkUser();
//                int nzpUser = db.GetLocalUser(conn_db, finder, out ret);
//                db.Close();
//                if (!ret.result)
//                {
//                    conn_db.Close();
//                    return null;
//                }
//                #endregion
//#if PG
//                string counters_vals = finder.pref + "_charge_" + (finder.year_ % 100).ToString("00") + ".counters_vals";
//#else
//                string counters_vals = finder.pref + "_charge_" + (finder.year_ % 100).ToString("00") + ":counters_vals";
//#endif
//                // Алгоритм выбора показаний
//                // Шаг 1. Получить упорядоченный список ПУ
//                // Шаг 2. Для каждого ПУ получить список показаний: все показания за выбранный месяц + 3 предыдущих показания

//                // Шаг 1
//                StringBuilder sql = new StringBuilder();
//#if PG

//                sql.Append("Select distinct b.nzp_counter, s.service, b.nzp_serv, b.num_cnt, b.dat_prov, b.dat_provnext, b.dat_oblom, b.dat_poch, b.dat_close");
//                sql.Append(", sc.cnt_stage, sc.mmnog, sc.name_type, b.dat_block, b.user_block, u.comment as user_name_block, (now() - INTERVAL  '" + Constants.users_min.ToString() + " minutes') as cur_dat ");

//                sql.Append(" From " + counters_vals + " a, ");
//                sql.Append(finder.pref + "_kernel.services s, ");
//                sql.Append(finder.pref + "_kernel.s_counttypes sc,  ");
//                sql.Append(finder.pref + "_data.counters_spis b ");
//                sql.Append(" left outer join " + finder.pref + "_data.users u on b.user_block = u.nzp_user ");
//                sql.Append(" Where a.nzp_counter = b.nzp_counter ");
//                sql.Append(" and b.nzp_serv = s.nzp_serv ");
//                sql.Append(" and b.nzp_cnttype = sc.nzp_cnttype ");
//                sql.Append(" and a.ist = " + finder.ist);
//                sql.Append(" and a.month_ = " + finder.month_);
//                sql.Append(" and a.nzp_type = " + finder.nzp_type);

//#else
//                sql.Append("Select unique b.nzp_counter, s.service, b.nzp_serv, b.num_cnt, b.dat_prov, b.dat_provnext, b.dat_oblom, b.dat_poch, b.dat_close");
//                sql.Append(", sc.cnt_stage, sc.mmnog, sc.name_type, b.dat_block, b.user_block, u.comment as user_name_block, (current year to second - " + Constants.users_min.ToString() + " units minute) as cur_dat ");
//                //sql.Append(" , tu.name_uchet ");
//                sql.Append(" From " + counters_vals + " a, ");
//                sql.Append(finder.pref + "_data:counters_spis b, ");
//                sql.Append(finder.pref + "_kernel:services s, ");
//                sql.Append(finder.pref + "_kernel:s_counttypes sc ");
//                sql.Append(" , outer " + finder.pref + "_data:users u ");
//                //sql.Append(", outer " + finder.pref + "_kernel:s_typeuchet tu ");
//                sql.Append(" Where a.nzp_counter = b.nzp_counter ");
//                sql.Append(" and b.nzp_serv = s.nzp_serv ");
//                sql.Append(" and b.nzp_cnttype = sc.nzp_cnttype ");
//                sql.Append(" and a.ist = " + finder.ist);
//                sql.Append(" and a.month_ = " + finder.month_);
//                sql.Append(" and a.nzp_type = " + finder.nzp_type);
//                //sql.Append(" and b.is_pl = tu.is_pl ");
//                sql.Append(" and b.user_block = u.nzp_user ");
//#endif

//                switch (finder.nzp_type)
//                {
//                    case (int)CounterKinds.Kvar:     // показания квартирных ПУ
//                        sql.Append(" and a.nzp = " + finder.nzp_kvar);
//                        break;
//                    case (int)CounterKinds.Dom:      // показания домовых ПУ
//                        sql.Append(" and a.nzp = " + finder.nzp_dom);
//                        break;
//                    case (int)CounterKinds.Group:     // показания групповых ПУ
//                    case (int)CounterKinds.Communal: // показания коммунальных ПУ
//                        if (finder.nzp_kvar > 0)
//                        {
//#if PG
//                            sql.Append(" and exists (select nzp_kvar from " + finder.pref + "_data.counters_link cl where cl.nzp_counter = a.nzp_counter and cl.nzp_kvar = " + finder.nzp_kvar + ")");
//#else
//                            sql.Append(" and exists (select nzp_kvar from " + finder.pref + "_data:counters_link cl where cl.nzp_counter = a.nzp_counter and cl.nzp_kvar = " + finder.nzp_kvar + ")");
//#endif
//                        }
//                        else if (finder.nzp_dom > 0)
//                        {
//                            //sql.Append(" and exists (select cl.nzp_kvar from " + finder.pref + "_data:counters_link cl, " + finder.pref + "_data:kvar k where cl.nzp_counter = a.nzp_counter and cl.nzp_kvar = k.nzp_kvar and k.nzp_dom = " + finder.nzp_dom + ")");
//                            sql.Append(" and a.nzp = " + finder.nzp_dom);
//                        }
//                        break;
//                }

//                if (finder.dat_close == "") sql.Append(" and b.dat_close is null ");
//                if (finder.nzp_serv > 0) sql.Append(" and b.nzp_serv = " + finder.nzp_serv);

//                if (finder.nzp_counter > 0) sql.Append(" and b.nzp_counter = " + finder.nzp_counter.ToString());

//                string dat_uchet = "'" + DateTime.Parse(finder.dat_uchet).ToShortDateString() + "'";
//                string dat_uchet_po;
//                string dat_uchet_s;
//                // количество предыдущих показаний
//                int k = 3;

//                DateTime dat_po = DateTime.Parse(finder.dat_uchet);
//                DateTime dat_s = dat_po.AddMonths(-1).AddDays(1);

//                if (finder.RolesVal != null)
//                    foreach (_RolesVal role in finder.RolesVal)
//                        if (role.tip == Constants.role_sql && role.kod == Constants.role_sql_serv) sql.Append(" and b.nzp_serv in (" + role.val + ") ");

//                sql.Append(" Order by s.service asc, b.num_cnt asc, sc.name_type ");

//                ret = ExecRead(conn_db, out reader, sql.ToString(), true);
//                if (!ret.result)
//                {
//                    conn_db.Close();
//                    return null;
//                }


//                string bl;
//                DateTime dt_block = DateTime.MinValue;
//                DateTime dt_cur = DateTime.MinValue;
//                int user_block = 0;
//                string userNameBlock = "";
//                int i = 0;
//                while (reader.Read())
//                {
//                    bl = "";
//                    dt_block = DateTime.MinValue;
//                    dt_cur = DateTime.MinValue;
//                    user_block = 0;
//                    userNameBlock = "";

//                    if (reader["user_block"] != DBNull.Value) user_block = (int)reader["user_block"]; //пользователь, который заблокирован
//                    if (reader["user_name_block"] != DBNull.Value) userNameBlock = ((string)reader["user_name_block"]).Trim(); //пользователь, который заблокирован
//                    if (reader["dat_block"] != DBNull.Value) dt_block = Convert.ToDateTime(reader["dat_block"]);//дата блокировки
//                    if (reader["cur_dat"] != DBNull.Value) dt_cur = Convert.ToDateTime(reader["cur_dat"]);//текущее время/дата - 20 мин

//                    if (user_block > 0 && dt_block != DateTime.MinValue) //заблокирован прибор учета
//                        if (user_block != nzpUser && dt_cur < dt_block) //если заблокирована запись другим пользователем и 20 мин не прошло
//                            bl = "Прибор учета заблокирован пользователем " + userNameBlock + " " + dt_block.ToString("dd.MM.yyyy в HH:mm") + ". Редактировать данные запрещено.";

//                    if (bl == "")
//                    {
//                        if (finder.prm == Constants.act_mode_edit.ToString()) //если берут данные на изменение
//                        {
//                            string str = "update " + finder.pref +
//                                         "_data.counters_spis set dat_block = now(), user_block = " + nzpUser +
//                                         " where nzp_counter = " + Convert.ToString(reader["nzp_counter"]);
//#if PG
//                            ret = ExecSQL(conn_db, str, true);
//#else
//    ret = ExecSQL(conn_db, "update " + finder.pref + "_data:counters_spis set dat_block = current year to second, user_block = " + nzpUser + " where nzp_counter = " + Convert.ToString(reader["nzp_counter"]), true);
//#endif
//                        }
//                        else //если  на просмотр
//                        {
//#if PG
//                            ret = ExecSQL(conn_db, "update " + finder.pref + "_data.counters_spis set dat_block = null, user_block = null where nzp_counter = " + Convert.ToString(reader["nzp_counter"]), true);
//#else
//ret = ExecSQL(conn_db, "update " + finder.pref + "_data:counters_spis set dat_block = null, user_block = null where nzp_counter = " + Convert.ToString(reader["nzp_counter"]), true);
//#endif
//                        }

//                        if (!ret.result) throw new Exception("Ошибка обновления таблицы counters_spis");
//                    }

//                    // определить одно ближайшее будущее показание
//                    sql = new StringBuilder();
//#if PG
//                    sql.Append(" Select  a.dat_uchet");
//                    sql.Append(" From " + counters_vals + " a ");
//                    sql.Append(" Where a.nzp_counter = " + Convert.ToString(reader["nzp_counter"]));
//                    sql.Append(" and a.dat_uchet > " + dat_uchet);
//                    sql.Append(" and a.val_cnt is not null");
//                    sql.Append(" and a.ist = " + finder.ist);
//                    sql.Append(" and a.month_ = " + finder.month_);
//                    sql.Append(" Order by a.dat_uchet asc limit 1 ");
//#else
//                    sql.Append(" Select first 1 a.dat_uchet");
//                    sql.Append(" From " + counters_vals + " a ");
//                    sql.Append(" Where a.nzp_counter = " + Convert.ToString(reader["nzp_counter"]));
//                    sql.Append(" and a.dat_uchet > " + dat_uchet);
//                    sql.Append(" and a.val_cnt is not null");
//                    sql.Append(" and a.ist = " + finder.ist);
//                    sql.Append(" and a.month_ = " + finder.month_);
//                    sql.Append(" Order by a.dat_uchet asc ");
//#endif

//                    ret = ExecRead(conn_db, out reader2, sql.ToString(), true);
//                    if (!ret.result) throw new Exception(ret.text);

//                    dat_uchet_po = dat_uchet;
//                    while (reader2.Read())
//                    {
//                        if (reader2["dat_uchet"] != DBNull.Value)
//                        {
//                            dat_uchet_po = Utils.EStrNull(Convert.ToDateTime(reader2["dat_uchet"]).ToShortDateString());
//                            break;
//                        }
//                    }
//                    reader2.Close();

//                    #region определить дату начала учета для 3 предыдущих значений
//                    //----------------------------------------------------------------------------------------------------------------
//                    sql = new StringBuilder();
//#if PG
//                    sql.Append(" Select  a.dat_uchet");
//                    sql.Append(" From " + counters_vals + " a ");
//                    sql.Append(" Where a.nzp_counter = " + Convert.ToString(reader["nzp_counter"]));
//                    sql.Append(" and a.dat_uchet < " + "'" + DateTime.Parse(finder.dat_uchet).AddMonths(-1).ToShortDateString() + "'");
//                    sql.Append(" and a.val_cnt is not null");
//                    sql.Append(" and a.ist = " + finder.ist);
//                    sql.Append(" and a.month_ = " + finder.month_);
//                    sql.Append(" Order by a.dat_uchet desc limit " + k);
//#else
//                    sql.Append(" Select first " + k + " a.dat_uchet");
//                    sql.Append(" From " + counters_vals + " a ");
//                    sql.Append(" Where a.nzp_counter = " + Convert.ToString(reader["nzp_counter"]));
//                    sql.Append(" and a.dat_uchet < " + "'" + DateTime.Parse(finder.dat_uchet).AddMonths(-1).ToShortDateString() + "'");
//                    sql.Append(" and a.val_cnt is not null");
//                    sql.Append(" and a.ist = " + finder.ist);
//                    sql.Append(" and a.month_ = " + finder.month_);
//                    sql.Append(" Order by a.dat_uchet desc ");
//#endif


//                    ret = ExecRead(conn_db, out reader2, sql.ToString(), true);
//                    if (!ret.result) throw new Exception(ret.text);

//                    dat_uchet_s = "'" + DateTime.Parse(finder.dat_uchet).AddMonths(-1).ToShortDateString() + "'";
//                    while (reader2.Read())
//                    {
//                        if (reader2["dat_uchet"] != DBNull.Value) dat_uchet_s = Utils.EStrNull(Convert.ToDateTime(reader2["dat_uchet"]).ToShortDateString());
//                    }
//                    reader2.Close();
//                    //----------------------------------------------------------------------------------------------------------------
//                    #endregion

//                    sql = new StringBuilder();

//#if PG
//                    sql.Append(" Select a.nzp_cv, a.dat_uchet, a.val_cnt, a.nzp, a.ngp_cnt, a.ngp_lift, a.dat_when, u.comment as user_name ");
//                    sql.Append(", b.nzp_counter, s.service, b.nzp_serv, b.num_cnt, b.dat_prov, b.dat_provnext, b.dat_oblom, b.dat_poch, b.dat_close ");
//                    sql.Append(", sc.cnt_stage, sc.mmnog, sc.name_type From ");
//                    sql.Append(finder.pref + "_data.counters_spis b, ");
//                    sql.Append(finder.pref + "_kernel.services s, ");
//                    sql.Append(finder.pref + "_kernel.s_counttypes sc, ");
//                    sql.Append(counters_vals + " a ");
//                    sql.Append("left outer join " + finder.pref + "_data.users u on a.nzp_user = u.nzp_user ");
//                    sql.Append(" Where a.nzp_counter = b.nzp_counter ");
//                    sql.Append(" and b.nzp_serv = s.nzp_serv ");
//                    sql.Append(" and b.nzp_cnttype = sc.nzp_cnttype ");
//                    sql.Append(" and a.nzp_counter = " + Convert.ToString(reader["nzp_counter"]));
//                    sql.Append(" and a.dat_uchet <= " + dat_uchet_po);
//                    sql.Append(" and a.dat_uchet >= " + dat_uchet_s);
//#else
//sql.Append(" Select a.nzp_cv, a.dat_uchet, a.val_cnt, a.nzp, a.ngp_cnt, a.ngp_lift, a.dat_when, u.comment as user_name ");
//                    sql.Append(", b.nzp_counter, s.service, b.nzp_serv, b.num_cnt, b.dat_prov, b.dat_provnext, b.dat_oblom, b.dat_poch, b.dat_close ");
//                    sql.Append(", sc.cnt_stage, sc.mmnog, sc.name_type From ");
//                    sql.Append(counters_vals + " a, ");
//                    sql.Append(finder.pref + "_data:counters_spis b, ");
//                    sql.Append(finder.pref + "_kernel:services s, ");
//                    sql.Append(finder.pref + "_kernel:s_counttypes sc ");
//                    sql.Append(", outer " + finder.pref + "_data:users u ");
//                    sql.Append(" Where a.nzp_counter = b.nzp_counter ");
//                    sql.Append(" and a.nzp_user = u.nzp_user ");
//                    sql.Append(" and b.nzp_serv = s.nzp_serv ");
//                    sql.Append(" and b.nzp_cnttype = sc.nzp_cnttype ");
//                    sql.Append(" and a.nzp_counter = " + Convert.ToString(reader["nzp_counter"]));
//                    sql.Append(" and a.dat_uchet <= " + dat_uchet_po);
//                    sql.Append(" and a.dat_uchet >= " + dat_uchet_s);
//#endif
//                    if (finder.prm == Constants.act_mode_view.ToString() || bl != "")
//                        sql.Append(" and a.val_cnt is not null");
//                    else
//                    {
//                        sql.Append(" and case when a.dat_uchet between " + Utils.EStrNull(dat_s.ToShortDateString()) + " and " + Utils.EStrNull(dat_po.ToShortDateString()) + " or a.val_cnt is not null then 1 else 0 end = 1 ");
//                    }
//                    sql.Append(" and a.ist = " + finder.ist);
//                    sql.Append(" and a.month_ = " + finder.month_);
//                    if (finder.date_begin != "") sql.Append(" and a.dat_uchet >= " + Utils.EStrNull(finder.date_begin));
//                    sql.Append(" Order by a.dat_uchet desc ");

//                    ret = ExecRead(conn_db, out reader2, sql.ToString(), true);
//                    if (!ret.result) throw new Exception(ret.text);

//                    int cnt = Spis.Count;
//                    long nzp_counter = Constants._ZERO_;
//                    while (reader2.Read())
//                    {
//                        i++;
//                        CounterVal zap = new CounterVal();

//                        zap.num = i.ToString();

//                        switch (finder.nzp_type)
//                        {
//                            case (int)CounterKinds.Kvar:
//                                zap.nzp_kvar = finder.nzp_kvar;
//                                break;
//                            case (int)CounterKinds.Dom:
//                                zap.nzp_dom = finder.nzp_dom;
//                                if (reader2["ngp_cnt"] != DBNull.Value) zap.ngp_cnt = Convert.ToDecimal(reader2["ngp_cnt"]);
//                                if (reader2["ngp_lift"] != DBNull.Value) zap.ngp_lift = Convert.ToDecimal(reader2["ngp_lift"]);
//                                break;
//                            case (int)CounterKinds.Group:
//                            case (int)CounterKinds.Communal:
//                                break;
//                        }

//                        zap.nzp_type = finder.nzp_type;
//                        zap.cnt_type = CounterKind.GetKindNameById(zap.nzp_type);

//                        zap.block = bl;

//                        if (reader2["nzp_cv"] != DBNull.Value) zap.nzp_cv = Convert.ToInt32(reader2["nzp_cv"]);
//                        if (reader2["service"] != DBNull.Value) zap.service = Convert.ToString(reader2["service"]);
//                        if (reader2["nzp_serv"] != DBNull.Value) zap.nzp_serv = Convert.ToInt32(reader2["nzp_serv"]);
//                        if (reader2["num_cnt"] != DBNull.Value) zap.num_cnt = Convert.ToString(reader2["num_cnt"]);
//                        if (reader2["mmnog"] != DBNull.Value) zap.mmnog = Convert.ToDecimal(reader2["mmnog"]);
//                        if (reader2["dat_close"] != DBNull.Value) zap.dat_close = String.Format("{0:dd.MM.yyyy}", reader2["dat_close"]);
//                        if (reader2["dat_prov"] != DBNull.Value) zap.dat_prov = String.Format("{0:dd.MM.yyyy}", reader2["dat_prov"]);
//                        if (reader2["dat_provnext"] != DBNull.Value) zap.dat_provnext = String.Format("{0:dd.MM.yyyy}", reader2["dat_provnext"]);
//                        if (reader2["dat_oblom"] != DBNull.Value) zap.dat_oblom = String.Format("{0:dd.MM.yyyy}", reader2["dat_oblom"]);
//                        if (reader2["dat_poch"] != DBNull.Value) zap.dat_poch = String.Format("{0:dd.MM.yyyy}", reader2["dat_poch"]);
//                        if (reader2["name_type"] != DBNull.Value) zap.name_type = Convert.ToString(reader2["name_type"]);
//                        //if (reader["name_uchet"] != DBNull.Value) zap.name_uchet = Convert.ToString(reader["name_uchet"]);
//                        if (reader2["dat_uchet"] != DBNull.Value)
//                        {
//                            zap.dat_uchet = String.Format("{0:dd.MM.yyyy}", reader2["dat_uchet"]);
//                            if ((DateTime)reader2["dat_uchet"] >= dat_s && (DateTime)reader2["dat_uchet"] <= dat_po)
//                                zap.is_editable = (zap.block == "");
//                        }
//                        if (reader2["cnt_stage"] != DBNull.Value) zap.cnt_stage = Convert.ToInt32(reader2["cnt_stage"]);
//                        if (reader2["nzp_counter"] != DBNull.Value) zap.nzp_counter = Convert.ToInt32(reader2["nzp_counter"]);

//                        if (reader2["dat_when"] == DBNull.Value) zap.dat_when = "";
//                        else
//                        {
//                            zap.dat_when = String.Format("{0:dd.MM.yyyy}", reader2["dat_when"]);
//                            if (reader2["user_name"] != DBNull.Value)
//                                if (Convert.ToString(reader2["user_name"]).Trim() != "")
//                                    zap.dat_when += " (" + Convert.ToString(reader2["user_name"]).Trim() + ")";
//                        }

//                        if (reader2["val_cnt"] != DBNull.Value)
//                        {
//                            zap.val_cnt = Convert.ToDecimal(reader2["val_cnt"]);
//                            zap.val_cnt_s = zap.val_cnt.ToString();
//                        }
//                        else if (!zap.is_editable ||                            // фиктивные показания заблокированного ПУ не показываем
//                            finder.prm == Constants.act_mode_view.ToString())   // в режиме просмотра невведенные или удаленные показания не показываем
//                            continue;

//                        if (nzp_counter != zap.nzp_counter)
//                        {
//                            nzp_counter = zap.nzp_counter;

//                            if (finder.year_ > 0 && finder.month_ > 0)
//                            {
//                                // определить плановый расход ПУ (среднее значение показания ПУ)
//                                DbParameters dbparam = new DbParameters();
//                                Prm finderPrm = new Prm();
//                                finderPrm.nzp_user = finder.nzp_user;
//                                finderPrm.pref = finder.pref;
//                                finderPrm.prm_num = 17;
//                                finderPrm.nzp_prm = 979;
//                                finderPrm.nzp = zap.nzp_counter;
//                                finderPrm.month_ = finder.month_;
//                                finderPrm.year_ = finder.year_;
//                                finderPrm = dbparam.FindSimplePrmValue(conn_db, finderPrm, out ret);
//                                dbparam.Close();
//                                if (!ret.result) throw new Exception(ret.text);

//                                zap.plan_rashod = finderPrm.val_prm;

//                                if (finder.nzp_type == (int)CounterKinds.Kvar)
//                                {
//                                    Counter findercnt = new Counter();
//                                    findercnt.pref = finder.pref;
//                                    findercnt.month_ = finder.month_;
//                                    findercnt.year_ = finder.year_;
//                                    findercnt.nzp_type = (int)CounterKinds.Kvar;
//                                    findercnt.nzp_kvar = zap.nzp_kvar;
//                                    findercnt.nzp_serv = zap.nzp_serv;
//                                    zap.normativ = GetNormative(conn_db, findercnt);
//                                    zap.rashod_k_opl = GetRashodKOplate(conn_db, findercnt);
//                                }
//                            }
//                        }

//                        Spis.Add(zap);
//                    }
//                    reader2.Close();

//                    // Если показаний больше одного, то удалим записи без показаний
//                    /*if (Spis.Count - cnt > 1 && bl != "")
//                    {
//                        for (int j = Spis.Count - 1; j >= cnt; j--)
//                        {
//                            if (Spis[j].val_cnt_s == "")
//                                Spis.RemoveAt(j);
//                        }
//                    }*/
//                }

//                if (finder.nzp_type == (int)CounterKinds.Kvar)
//                {
//                    ret = DefineMaxRashod(conn_db, finder, Spis);
//                    if (!ret.result) throw new Exception(ret.text);
//                }

//                reader.Close();
//                conn_db.Close();
//                if (Constants.Trace) Utility.ClassLog.WriteLog("Cтоп функции GetCountersVals");
//                return Spis;
//            }
//            catch (Exception ex)
//            {
//                reader.Close();
//                if (reader2 != null) reader2.Close();
//                conn_db.Close();

//                ret.result = false;
//                ret.text = ex.Message;

//                string err;
//                if (Constants.Viewerror) err = " \n " + ex.Message;
//                else err = "";

//                MonitorLog.WriteLog("Ошибка в GetCountersVals " + err, MonitorLog.typelog.Error, 20, 201, true);

//                return null;
//            }

//        }

//#warning функции, работающие с counters, counters_dom, counters_group через counters_vals. Не использовать
//        private Returns ValidateCountersValsParams(CounterVal finder)
//        {
//            if (finder.nzp_user < 1) return new Returns(false, "Не определен пользователь");
//            if (finder.pref == "") return new Returns(false, "Не задан префикс базы данных");
//            if (finder.ist <= 0) return new Returns(false, "Не задан источник");
//            if (finder.year_ < 1) return new Returns(false, "Не задан расчетный год");
//            if (finder.month_ < 1) return new Returns(false, "Не задан расчетный месяц");

//            if (!((finder.nzp_type == (int)CounterKinds.Dom && finder.nzp_dom > 0) ||
//                  (finder.nzp_type == (int)CounterKinds.Kvar && finder.nzp_kvar > 0) ||
//                  (finder.nzp_type == (int)CounterKinds.Group && (finder.nzp_kvar > 0 || finder.nzp_dom > 0)) ||
//                  (finder.nzp_type == (int)CounterKinds.Communal && finder.nzp_dom > 0)
//                ))
//            {
//                return new Returns(false, "Неверные входные параметры");
//            }

//            string dat_uchet_s;
//            string dat_uchet_po;

//            if (finder.dat_uchet != "")
//            {
//                DateTime dat;
//                if (DateTime.TryParse(finder.dat_uchet, out dat))
//                {
//                    dat_uchet_s = "'" + dat.AddMonths(-1).AddDays(1).ToShortDateString() + "'";
//                    dat_uchet_po = "'" + dat.ToShortDateString() + "'";
//                }
//                else
//                {
//                    return new Returns(false, "Месяц за который редактируются показания, имеет неправильный формат");
//                }
//            }
//            else
//            {
//                return new Returns(false, "Не задан месяц за который редактируются показания");
//            }
//            return new Returns(true);
//        }

//#warning функции, работающие с counters, counters_dom, counters_group через counters_vals. Не использовать
//        public Returns PrepareCountersVals(CounterVal finder, PrepareCounterValMode mode)
//        {
//            return PrepareCountersVals(finder, mode, "", null);
//        }

//#warning функции, работающие с counters, counters_dom, counters_group через counters_vals. Не использовать
//        public Returns PrepareCountersVals(CounterVal finder, PrepareCounterValMode mode, string select_pu, IDbConnection connectionId)
//        {
//            try
//            {
//                Returns ret = Utils.InitReturns();

//                ret = ValidateCountersValsParams(finder);
//                if (!ret.result) return ret;

//                if (mode == PrepareCounterValMode.CountersReadings)
//                {
//                    if (select_pu == "") return new Returns(false, "Не заданы условия для приборов учета", -1);
//                }

//                string dat_uchet_s = "'" + DateTime.Parse(finder.dat_uchet).AddMonths(-1).AddDays(1).ToShortDateString() + "'";
//                string dat_uchet_po = "'" + DateTime.Parse(finder.dat_uchet).ToShortDateString() + "'";

//                string connectionString = Points.GetConnByPref(finder.pref);
//                bool closeConnDb = false;

//                if (connectionId == null)
//                {
//                    closeConnDb = true;
//                    connectionId = GetConnection(connectionString);
//                    ret = OpenDb(connectionId, true);
//                    if (!ret.result) return ret;
//                }

//                IDbConnection conn_db = connectionId;
//                //conn_db.ChangeDatabase(finder.pref + "_charge_" + (finder.year_ % 100).ToString("00"));

//                #region определение локального пользователя
//                //-----------------------------------------------------------------------------------------
//                DbWorkUser db = new DbWorkUser();
//                int nzpUser = db.GetLocalUser(conn_db, finder, out ret);
//                db.Close();
//                if (!ret.result)
//                {
//                    conn_db.Close();
//                    return ret;
//                }
//                //-----------------------------------------------------------------------------------------
//                #endregion

//                #region проверка существования БД
//                //-----------------------------------------------------------------------------------------
//#if PG
//                ret = ExecSQL(conn_db, "set search_path to '" + finder.pref + "_charge_" + (finder.year_ % 100).ToString("00") + "'", true);
//#else
//            ret = ExecSQL(conn_db, "database " + finder.pref + "_charge_" + (finder.year_ % 100).ToString("00"), true);
//#endif
//                if (!ret.result)
//                {
//                    conn_db.Close();
//                    return ret;
//                }
//                //-----------------------------------------------------------------------------------------
//                #endregion

//#if PG
//                string counters_vals = "counters_vals";
//                // Задать режим блокировки записей таблицы
//                ExecSQL(conn_db, "alter table " + counters_vals + " lock mode (row)", false);
//                string counters_vals_full = finder.pref + "_charge_" + (finder.year_ % 100).ToString("00") + "." + counters_vals;
//                string counters = finder.pref + "_data." + "counters";
//                string counters_dom = finder.pref + "_data." + "counters_dom";
//                string counters_group = finder.pref + "_data." + "counters_group";
//                string counters_spis = finder.pref + "_data." + "counters_spis";
//                string counters_link = finder.pref + "_data." + "counters_link";
//                //string kvar = finder.pref + "_data."  + "kvar";
//                //string services = finder.pref + "_kernel."  + "services";
//                string filter = "";
//                if (finder.RolesVal != null)
//                    foreach (_RolesVal role in finder.RolesVal)
//                        if (role.tip == Constants.role_sql && role.kod == Constants.role_sql_serv) filter += " and a.nzp_serv in (" + role.val + ") ";
//#else
//                string owner = "are.";
//                string counters_vals = "counters_vals";
//                // Задать режим блокировки записей таблицы
//                ExecSQL(conn_db, "alter table " + counters_vals + " lock mode (row)", false);
//                string counters_vals_full = finder.pref + "_charge_" + (finder.year_ % 100).ToString("00") + ":" + counters_vals;
//                string counters = finder.pref + "_data:" + owner + "counters";
//                string counters_dom = finder.pref + "_data:" + owner + "counters_dom";
//                string counters_group = finder.pref + "_data:" + owner + "counters_group";
//                string counters_spis = finder.pref + "_data:" + owner + "counters_spis";
//                string counters_link = finder.pref + "_data:" + owner + "counters_link";
//                //string kvar = finder.pref + "_data:" + owner + "kvar";
//                //string services = finder.pref + "_kernel:" + owner + "services";
//                string filter = "";
//                if (finder.RolesVal != null)
//                    foreach (_RolesVal role in finder.RolesVal)
//                        if (role.tip == Constants.role_sql && role.kod == Constants.role_sql_serv) filter += " and a.nzp_serv in (" + role.val + ") ";
//#endif

//                // Алгоритм добавления показаний:
//                // Шаг 1. Удалить неизменившиеся показания (в том числе фиктивные)
//                // Шаг 2. Для приборов, имеющих хоть одно показание, добавить показание за текущий расчетный месяц
//                // Шаг 3. Если за текущий расчетный месяц показаний нет, то добавить фиктивное показание за текущий расчетный месяц
//                // Шаг 4. Для приборов, имеющих хоть одно показание, загрузить последние k показаний из прошлого и одно из будущего для каждого прибора учета
//                // Шаг 5. Если данные открываются на просмотр, то удалить фиктивные строки

//                StringBuilder sql;

//                #region Шаг 1. Удалить неизменившиеся показания (в том числе фиктивные)
//                //--------------------------------------------------------------------------------------------------------------------
//                sql = new StringBuilder();
//#if PG
//                sql.Append(" Delete From " + counters_vals_full + " Where month_ = " + finder.month_ + " and is_new is null and nzp_type = " + finder.nzp_type);
//                switch (finder.nzp_type)
//                {
//                    case (int)CounterKinds.Kvar:
//                        //--------------------------------------------------------------------------------------------------------------------------------
//                        if (mode == PrepareCounterValMode.SpisVal) sql.Append(" and nzp = " + finder.nzp_kvar);
//                        else sql.Append(" and nzp_counter in " + select_pu);
//                        //--------------------------------------------------------------------------------------------------------------------------------
//                        break;
//                    case (int)CounterKinds.Dom:
//                        //--------------------------------------------------------------------------------------------------------------------------------
//                        if (mode == PrepareCounterValMode.SpisVal) sql.Append(" and nzp = " + finder.nzp_dom);
//                        else sql.Append(" and nzp_counter in " + select_pu);
//                        //--------------------------------------------------------------------------------------------------------------------------------
//                        break;
//                    case (int)CounterKinds.Group:
//                    case (int)CounterKinds.Communal:
//                        if (finder.nzp_kvar > 0)
//                        {
//                            sql.Append(" and nzp_counter in (select distinct cl.nzp_counter from " + counters_link + " cl where cl.nzp_kvar = " + finder.nzp_kvar + ")");
//                        }
//                        else if (finder.nzp_dom > 0)
//                        {
//                            //sql.Append(" and nzp_counter in (select distinct cl.nzp_counter from " + counters_link + " cl, " + kvar + " k where cl.nzp_kvar = k.nzp_kvar and k.nzp_dom = " + finder.nzp_dom + ")");
//                            sql.Append(" and nzp = " + finder.nzp_dom);
//                        }
//                        break;
//                }
//#else
//                sql.Append(" Delete From " + counters_vals_full + " Where month_ = " + finder.month_ + " and is_new is null and nzp_type = " + finder.nzp_type);
//                switch (finder.nzp_type)
//                {
//                    case (int)CounterKinds.Kvar:
//                        //--------------------------------------------------------------------------------------------------------------------------------
//                        if (mode == PrepareCounterValMode.SpisVal) sql.Append(" and nzp = " + finder.nzp_kvar);
//                        else sql.Append(" and nzp_counter in " + select_pu);
//                        //--------------------------------------------------------------------------------------------------------------------------------
//                        break;
//                    case (int)CounterKinds.Dom:
//                        //--------------------------------------------------------------------------------------------------------------------------------
//                        if (mode == PrepareCounterValMode.SpisVal) sql.Append(" and nzp = " + finder.nzp_dom);
//                        else sql.Append(" and nzp_counter in " + select_pu);
//                        //--------------------------------------------------------------------------------------------------------------------------------
//                        break;
//                    case (int)CounterKinds.Group:
//                    case (int)CounterKinds.Communal:
//                        if (finder.nzp_kvar > 0)
//                        {
//                            sql.Append(" and nzp_counter in (select distinct cl.nzp_counter from " + counters_link + " cl where cl.nzp_kvar = " + finder.nzp_kvar + ")");
//                        }
//                        else if (finder.nzp_dom > 0)
//                        {
//                            //sql.Append(" and nzp_counter in (select distinct cl.nzp_counter from " + counters_link + " cl, " + kvar + " k where cl.nzp_kvar = k.nzp_kvar and k.nzp_dom = " + finder.nzp_dom + ")");
//                            sql.Append(" and nzp = " + finder.nzp_dom);
//                        }
//                        break;
//                }
//#endif

//                #region условие, что удалять можно незаблокированные ПУ, заблокированные самим этим пользователем, или приборы с просроченной блокировкой
//                //-------------------------------------------------------------------------------------------------------------------- 
//#if PG
//                sql.Append(" and nzp_counter in (select distinct a.nzp_counter from " + counters_spis + " a where a.nzp_type = " + finder.nzp_type);
//#else
//sql.Append(" and nzp_counter in (select distinct a.nzp_counter from " + counters_spis + " a where a.nzp_type = " + finder.nzp_type);
//#endif

//                switch (finder.nzp_type)
//                {
//                    case (int)CounterKinds.Kvar:
//                        //--------------------------------------------------------------------------------------------------------------------------------
//                        if (mode == PrepareCounterValMode.SpisVal) sql.Append(" and a.nzp = " + finder.nzp_kvar);
//                        else sql.Append(" and a.nzp_counter in " + select_pu);
//                        //--------------------------------------------------------------------------------------------------------------------------------
//                        break;
//                    case (int)CounterKinds.Dom:
//                        //--------------------------------------------------------------------------------------------------------------------------------
//                        if (mode == PrepareCounterValMode.SpisVal) sql.Append(" and a.nzp = " + finder.nzp_dom);
//                        else sql.Append(" and a.nzp_counter in " + select_pu);
//                        //--------------------------------------------------------------------------------------------------------------------------------
//                        break;
//                    case (int)CounterKinds.Group:
//                    case (int)CounterKinds.Communal:
//                        if (finder.nzp_kvar > 0)
//                        {
//                            sql.Append(" and a.nzp_counter in (select distinct cl.nzp_counter from " + counters_link + " cl where cl.nzp_kvar = " + finder.nzp_kvar + ")");
//                        }
//                        else if (finder.nzp_dom > 0)
//                        {
//                            //sql.Append(" and nzp_counter in (select distinct cl.nzp_counter from " + counters_link + " cl, " + kvar + " k where cl.nzp_kvar = k.nzp_kvar and k.nzp_dom = " + finder.nzp_dom + ")");
//                            sql.Append(" and a.nzp = " + finder.nzp_dom);
//                        }
//                        break;
//                }
//                sql.Append(filter);
//#if PG
//                sql.Append(" and (a.user_block is null or a.user_block = " + nzpUser + " or (a.user_block <> " + nzpUser + " and now() - a.dat_block > INTERVAL '" + Constants.users_min + " minutes'  )))");
//#else
//    sql.Append(" and (a.user_block is null or a.user_block = " + nzpUser + " or (a.user_block <> " + nzpUser + " and current year to second - a.dat_block > " + Constants.users_min + " units minute)))");
//#endif//--------------------------------------------------------------------------------------------------------------------
//                #endregion

//                if (mode == PrepareCounterValMode.CountersReadings && Constants.Trace)
//                    Utility.ClassLog.WriteLog("PrepareCountersVals: ************************ НАЧАЛО ********************* ");

//                // выполнить удаление
//                ret = ExecSQL(conn_db, sql.ToString(), true);
//                if (!ret.result)
//                {
//                    conn_db.Close();
//                    return ret;
//                }

//                if (mode == PrepareCounterValMode.CountersReadings && Constants.Trace)
//                    Utility.ClassLog.WriteLog("PrepareCountersVals: Шаг 1 (удаление): " + sql.ToString());
//                //--------------------------------------------------------------------------------------------------------------------
//                #endregion

//                #region Шаг 2. Для приборов, имеющих хоть одно показание, добавить показание за текущий расчетный месяц
//                //--------------------------------------------------------------------------------------------------------------------
//                sql = new StringBuilder();

//                string where_main_dat_uchet = /*(mode == PrepareCounterValMode.SpisVal ?*/" and cv.dat_uchet = c.dat_uchet"; // :
//                //" and case when day(cv.dat_uchet) = 1 then cv.dat_uchet else date(mdy(month(cv.dat_uchet), 1, year(cv.dat_uchet)) + 1 units month) end = case when day(c.dat_uchet) = 1 then c.dat_uchet else date(mdy(month(c.dat_uchet), 1, year(c.dat_uchet)) + 1 units month) end");

//                sql.Append("Insert into " + counters_vals_full + " (nzp,nzp_type,nzp_counter,month_,dat_uchet,val_cnt,ngp_cnt,ngp_lift,nzp_user,dat_when,ist)");

//                switch (finder.nzp_type)
//                {
//                    case (int)CounterKinds.Kvar:
//                        sql.Append(" Select c.nzp_kvar, " + (int)CounterKinds.Kvar + ", c.nzp_counter," + finder.month_ + ", c.dat_uchet, c.val_cnt");
//                        //sql.Append(",0,0,c.nzp_user,c.dat_when,c.ist");
//                        sql.Append((Points.IsIpuHasNgpCnt ? ", c.ngp_cnt" : ", 0"));
//                        sql.Append(", 0, c.nzp_user, c.dat_when," + finder.ist);
//                        sql.Append(" From " + counters + " c, " + counters_spis + " a ");
//                        //--------------------------------------------------------------------------------------------------------------------------------
//                        if (mode == PrepareCounterValMode.SpisVal) sql.Append(" Where c.nzp_kvar = " + finder.nzp_kvar);
//                        else sql.Append(" Where c.nzp_counter in " + select_pu);
//                        //--------------------------------------------------------------------------------------------------------------------------------
//                        break;
//                    case (int)CounterKinds.Dom:
//                        sql.Append(" Select c.nzp_dom," + (int)CounterKinds.Dom + ", c.nzp_counter, " + finder.month_ + ", c.dat_uchet, c.val_cnt");
//                        sql.Append(", c.ngp_cnt, c.ngp_lift, c.nzp_user, c.dat_when, " + finder.ist);
//                        sql.Append(" From " + counters_dom + " c, " + counters_spis + " a ");
//                        //--------------------------------------------------------------------------------------------------------------------------------
//                        if (mode == PrepareCounterValMode.SpisVal) sql.Append(" Where c.nzp_dom = " + finder.nzp_dom);
//                        else sql.Append(" Where c.nzp_counter in " + select_pu);
//                        //--------------------------------------------------------------------------------------------------------------------------------
//                        break;
//                    case (int)CounterKinds.Group:
//                    case (int)CounterKinds.Communal:
//                        sql.Append(" Select 0," + finder.nzp_type + ", c.nzp_counter, " + finder.month_ + ", c.dat_uchet, c.val_cnt");
//                        sql.Append(", 0, 0, c.nzp_user, c.dat_when, " + finder.ist);
//                        sql.Append(" From " + counters_group + " c, " + counters_spis + " a");
//                        if (finder.nzp_kvar > 0)
//                        {
//                            sql.Append(" Where exists (select cl.nzp_kvar from " + counters_link + " cl where cl.nzp_counter = c.nzp_counter and cl.nzp_kvar = " + finder.nzp_kvar + ")");
//                        }
//                        else if (finder.nzp_dom > 0)
//                        {
//                            //sql.Append(" Where exists (select cl.nzp_kvar from " + counters_link + " cl, " + kvar + " k where cl.nzp_counter = c.nzp_counter and cl.nzp_kvar = k.nzp_kvar and k.nzp_dom = " + finder.nzp_dom + ")");
//                            sql.Append(" Where exists (select * from " + counters_spis + " cs where cs.nzp_counter = c.nzp_counter and cs.nzp = " + finder.nzp_dom + ")");
//                        }
//                        break;
//                }

//                sql.Append(" and c.nzp_counter = a.nzp_counter");
//                sql.Append(filter);
//                sql.Append(" and c.dat_uchet between " + dat_uchet_s + " and " + dat_uchet_po);
//                sql.Append(" and (select count(*) from " + counters_vals_full + " cv where c.nzp_counter = cv.nzp_counter" +
//                    " and cv.month_ = " + finder.month_ +
//                    " and cv.ist =" + finder.ist + where_main_dat_uchet + ") = 0 " +
//                    " and c.is_actual <> 100 ");

//                if (finder.date_begin != "") sql.Append(" and c.dat_uchet >= " + Utils.EStrNull(finder.date_begin));

//                // выполнить вставку
//                ret = ExecSQL(conn_db, sql.ToString(), true);
//                if (!ret.result)
//                {
//                    conn_db.Close();
//                    return ret;
//                }

//                if (mode == PrepareCounterValMode.CountersReadings && Constants.Trace)
//                    Utility.ClassLog.WriteLog("PrepareCountersVals: Шаг 2 (вставка): " + sql.ToString());
//                //--------------------------------------------------------------------------------------------------------------------
//                #endregion

//                #region Шаг 3. Если за текущий расчетный месяц показаний нет, то добавить фиктивное показание за текущий расчетный месяц
//                //--------------------------------------------------------------------------------------------------------------------            
//                sql = new StringBuilder();
//                sql.Append("Insert into " + counters_vals_full + " (nzp,nzp_type,nzp_counter,month_,dat_uchet,ist)");

//                switch (finder.nzp_type)
//                {
//                    case (int)CounterKinds.Kvar:
//#if PG
//                        sql.Append(" Select distinct a.nzp, " + (int)CounterKinds.Kvar + ", a.nzp_counter, " + finder.month_ + ", " + dat_uchet_po + "::date, " + finder.ist);
//                        sql.Append(" From " + counters_spis + " a");
//                        sql.Append(" Where a.nzp_type = " + (int)CounterKinds.Kvar);
//#else
//sql.Append(" Select unique a.nzp, " + (int)CounterKinds.Kvar + ", a.nzp_counter, " + finder.month_ + ", " + dat_uchet_po + ", " + finder.ist);
//                        sql.Append(" From " + counters_spis + " a");
//                        sql.Append(" Where a.nzp_type = " + (int)CounterKinds.Kvar);
//#endif
//                        //--------------------------------------------------------------------------------------------------------------------------------
//                        if (mode == PrepareCounterValMode.SpisVal) sql.Append(" and a.nzp = " + finder.nzp_kvar);
//                        else sql.Append(" and a.nzp_counter in " + select_pu);
//                        //--------------------------------------------------------------------------------------------------------------------------------
//                        //sql.Append(" and (select count(*) from " + counters + " c5 where c5.nzp_counter = a.nzp_counter and c5.dat_uchet between " + dat_uchet_s + " and " + dat_uchet_po + " and c5.is_actual <> 100) = 0");
//                        sql.Append(" and (select count(*) from " + counters + " c5 where c5.nzp_counter = a.nzp_counter and c5.dat_uchet = " + dat_uchet_po + " and c5.is_actual <> 100) = 0 ");
//                        break;
//                    case (int)CounterKinds.Dom:
//#if PG
//                        sql.Append(" Select distinct a.nzp, " + (int)CounterKinds.Dom + ", a.nzp_counter, " + finder.month_ + ", " + dat_uchet_po + "::date, " + finder.ist);
//                        sql.Append(" From " + counters_spis + " a");
//                        sql.Append(" Where a.nzp_type = " + (int)CounterKinds.Dom);
//#else
//                        sql.Append(" Select unique a.nzp, " + (int)CounterKinds.Dom + ", a.nzp_counter, " + finder.month_ + ", " + dat_uchet_po + ", " + finder.ist);
//                        sql.Append(" From " + counters_spis + " a");
//                        sql.Append(" Where a.nzp_type = " + (int)CounterKinds.Dom);
//#endif
//                        //--------------------------------------------------------------------------------------------------------------------------------
//                        if (mode == PrepareCounterValMode.SpisVal) sql.Append(" and a.nzp = " + finder.nzp_dom);
//                        else sql.Append(" and a.nzp_counter in " + select_pu);
//                        //--------------------------------------------------------------------------------------------------------------------------------
//#if PG
//                        sql.Append(" and (select count(*) from " + counters_dom + " c5 where c5.nzp_counter = a.nzp_counter and c5.dat_uchet between " + dat_uchet_s + "::date and " + dat_uchet_po + "::date and c5.is_actual <> 100) = 0");

//#else
//                        sql.Append(" and (select count(*) from " + counters_dom + " c5 where c5.nzp_counter = a.nzp_counter and c5.dat_uchet between " + dat_uchet_s + " and " + dat_uchet_po + " and c5.is_actual <> 100) = 0");
						
//#endif

//                        break;
//                    case (int)CounterKinds.Group:
//                    case (int)CounterKinds.Communal:
//#if PG
//                        sql.Append(" Select distinct a.nzp, " + finder.nzp_type + ", a.nzp_counter, " + finder.month_ + ", " + dat_uchet_po + "::date, " + finder.ist);
//                        sql.Append(" From " + counters_spis + " a where a.nzp_type =" + finder.nzp_type);
//#else
//                        sql.Append(" Select unique a.nzp, " + finder.nzp_type + ", a.nzp_counter, " + finder.month_ + ", " + dat_uchet_po + ", " + finder.ist);
//                        sql.Append(" From " + counters_spis + " a where a.nzp_type =" + finder.nzp_type);
//#endif
//                        if (finder.nzp_kvar > 0)
//                        {
//                            sql.Append(" and exists (select cl.nzp_kvar from " + counters_link + " cl where cl.nzp_counter = a.nzp_counter and cl.nzp_kvar = " + finder.nzp_kvar + ")");
//                        }
//                        else if (finder.nzp_dom > 0)
//                        {
//                            //sql.Append(" and exists (select cl.nzp_kvar from " + counters_link + " cl, " + kvar + " k where cl.nzp_counter = c.nzp_counter and cl.nzp_kvar = k.nzp_kvar and k.nzp_dom = " + finder.nzp_dom + ")");
//                            sql.Append(" and a.nzp = " + finder.nzp_dom);
//                        }
//#if PG
//                        sql.Append(" and not exists (select * from " + counters_group + " c5 where c5.nzp_counter = a.nzp_counter and c5.dat_uchet between " + dat_uchet_s + "::date and " + dat_uchet_po + "::date and c5.is_actual <> 100)");

//#else
//                        sql.Append(" and not exists (select * from " + counters_group + " c5 where c5.nzp_counter = a.nzp_counter and c5.dat_uchet between " + dat_uchet_s + " and " + dat_uchet_po + " and c5.is_actual <> 100)");
						
//#endif


//                        break;
//                }
//                sql.Append(" and a.is_actual <> 100 ");
//                sql.Append(filter);
//                sql.Append(" and (select count(*) from " + counters_vals_full + " cv where a.nzp_counter = cv.nzp_counter" +
//                    " and cv.month_ = " + finder.month_ +
//                    " and cv.ist =" + finder.ist +
//                    //" and " + dat_uchet_po + " = case when day(cv.dat_uchet) = 1 then cv.dat_uchet else date(mdy(month(cv.dat_uchet),1,year(cv.dat_uchet)) + 1 units month) end) = 0 ");
//                    " and " + dat_uchet_po + " = cv.dat_uchet) = 0 ");

//                // вставить фиктивные показания
//                ret = ExecSQL(conn_db, sql.ToString(), true);
//                if (!ret.result)
//                {
//                    conn_db.Close();
//                    return ret;
//                }

//                if (mode == PrepareCounterValMode.CountersReadings && Constants.Trace)
//                    Utility.ClassLog.WriteLog("PrepareCountersVals: Шаг 3 (фиктивные показания): " + sql.ToString());
//                //--------------------------------------------------------------------------------------------------------------------            
//                #endregion

//                int pastValCount = 3;
//                if (mode == PrepareCounterValMode.CountersReadings) pastValCount = 1;

//                #region Шаг 4. Для приборов, имеющих хоть одно показание, загрузить последние k показаний из прошлого и одно из будущего для каждого прибора учета
//                //--------------------------------------------------------------------------------------------------------------------            
//                try
//                {
//                    #region получить коды просматриваемых приборов учета и сложить их во временную таблицу tmp_pcv_0, туда же положить dat_uchet_s
//                    //--------------------------------------------------------------------------------------------------------------------            
//                    // удалить временную таблицу
//#if PG
//                    ExecSQL(conn_db, "Drop table tmp_pcv_0", false);
//                    sql = new StringBuilder();
//                    sql.Append("Select distinct cv.nzp_counter," +
//                        " min(cv.dat_uchet) as dat_uchet " +
//                         "into unlogged tmp_pcv_0 " +
//                        " From " + counters_vals_full + " cv, " + counters_spis + " a");
//                    sql.Append(" Where cv.month_ = " + finder.month_);
//                    sql.Append(" and cv.nzp_counter = a.nzp_counter");
//                    sql.Append(filter);
//                    sql.Append(" and cv.dat_uchet between " + dat_uchet_s + " and " + dat_uchet_po);
//                    sql.Append(" and cv.ist = " + finder.ist);
//                    //sql.Append(" and cv.nzp_type = " + finder.nzp_type);
//#else
//    ExecSQL(conn_db, "Drop table tmp_pcv_0", false);
//                    sql = new StringBuilder();
//                    sql.Append("Select distinct cv.nzp_counter," +
//                        " min(cv.dat_uchet) as dat_uchet " +
//                        " From " + counters_vals_full + " cv, " + counters_spis + " a");
//                    sql.Append(" Where cv.month_ = " + finder.month_);
//                    sql.Append(" and cv.nzp_counter = a.nzp_counter");
//                    sql.Append(filter);
//                    sql.Append(" and cv.dat_uchet between " + dat_uchet_s + " and " + dat_uchet_po);
//                    sql.Append(" and cv.ist = " + finder.ist);
//                    //sql.Append(" and cv.nzp_type = " + finder.nzp_type);
//#endif
//                    switch (finder.nzp_type)
//                    {
//                        case (int)CounterKinds.Kvar:
//                            //--------------------------------------------------------------------------------------------------------------------------------
//                            if (mode == PrepareCounterValMode.SpisVal) sql.Append(" and cv.nzp = " + finder.nzp_kvar);
//                            else sql.Append(" and cv.nzp_counter in " + select_pu);
//                            //--------------------------------------------------------------------------------------------------------------------------------
//                            break;
//                        case (int)CounterKinds.Dom:
//                            //--------------------------------------------------------------------------------------------------------------------------------
//                            if (mode == PrepareCounterValMode.SpisVal) sql.Append(" and cv.nzp = " + finder.nzp_dom);
//                            else sql.Append(" and cv.nzp_counter in " + select_pu);
//                            //--------------------------------------------------------------------------------------------------------------------------------
//                            break;
//                        case (int)CounterKinds.Group:
//                        case (int)CounterKinds.Communal:
//                            if (finder.nzp_kvar > 0)
//                            {
//                                sql.Append(" and exists (select cl.nzp_kvar from " + counters_link + " cl where cl.nzp_counter = cv.nzp_counter and cl.nzp_kvar = " + finder.nzp_kvar + ")");
//                            }
//                            else if (finder.nzp_dom > 0)
//                            {
//                                //sql.Append(" and exists (select cl.nzp_kvar from " + counters_link + " cl, " + kvar + " k where cl.nzp_counter = cv.nzp_counter and cl.nzp_kvar = k.nzp_kvar and k.nzp_dom = " + finder.nzp_dom + ")");
//                                sql.Append(" and cv.nzp = " + finder.nzp_dom);
//                            }
//                            break;
//                    }
//                    sql.Append(" Group by 1 ");
//#if PG
//                    //ничего не надо добавлять
//#else
//                    sql.Append(" into temp tmp_pcv_0 with no log ");
//#endif



//                    ret = ExecSQL(conn_db, sql.ToString(), true);
//                    if (!ret.result) throw new Exception(ret.text);

//                    if (mode == PrepareCounterValMode.CountersReadings && Constants.Trace)
//                        Utility.ClassLog.WriteLog("PrepareCountersVals: Шаг 4 (выборка в временную таблицу): " + sql.ToString());
//                    //--------------------------------------------------------------------------------------------------------------------            
//                    #endregion

//                    #region получить информацию о предыдущих показаниях
//                    //--------------------------------------------------------------------------------------------------------------------            
//                    // cформировать условие для даты учета предыдущих показаний
//                    // в таблице tmp_pcv_0 находятся коды обрабатываемых ПУ и dat_uchet_s - максимальная дата учета первого предыдущего показания
//                    // в таблицу tmp_pcv_1 попадет информация о вторых предыдущих показаниях ПУ
//                    // у вторых предыдущих показания дата учета должна быть ближайшей к дате учета первого предыдущего показания
//                    // и т.д.

//                    string where_dat_uchet = "Select max(b.dat_uchet) From ";

//                    switch (finder.nzp_type)
//                    {
//                        case (int)CounterKinds.Kvar:
//                            where_dat_uchet += counters;
//                            break;
//                        case (int)CounterKinds.Dom:
//                            where_dat_uchet += counters_dom;
//                            break;
//                        case (int)CounterKinds.Group:
//                        case (int)CounterKinds.Communal:
//                            where_dat_uchet += counters_group;
//                            break;
//                    }

//                    where_dat_uchet += " b Where t.nzp_counter = b.nzp_counter" +
//                        " and b.dat_uchet < t.dat_uchet" +
//                        " and b.is_actual <> 100 ";
//                    if (finder.date_begin != "") where_dat_uchet += " and b.dat_uchet >= " + Utils.EStrNull(finder.date_begin);

//                    //if (mode == PrepareCounterValMode.CountersReadings) where_dat_uchet += " and b.dat_uchet < " + dat_uchet_s; 

//                    string _where = " and (select count(*) from " + counters_vals_full + " cv where c.nzp_counter = cv.nzp_counter" +
//                            " and cv.month_ = " + finder.month_ +
//                            " and cv.ist = " + finder.ist +
//                            where_main_dat_uchet + ") = 0 ";
//                    if (finder.date_begin != "") _where += " and c.dat_uchet >= " + Utils.EStrNull(finder.date_begin);

//                    for (int k = 0; k < pastValCount; k++)
//                    {
//                        #region сохранить во временную таблицу информацию о k-ых предыдущих показаниях ПУ (математики)
//                        //--------------------------------------------------------------------------------------------------------------------            
//                        // удалить временную таблицу
//                        ExecSQL(conn_db, "Drop table tmp_pcv_" + Convert.ToString(k + 1), false);


//                        sql = new StringBuilder();
//                        sql.Append("Select ");

//                        switch (finder.nzp_type)
//                        {
//                            case (int)CounterKinds.Kvar:
//                                sql.Append("c.nzp_kvar as nzp, " + (int)CounterKinds.Kvar + " as nzp_type, c.nzp_counter, " + finder.month_ + " as month_, c.dat_uchet, c.val_cnt ");
//                                sql.Append((Points.IsIpuHasNgpCnt ? ", c.ngp_cnt" : ", 0") + " as ngp_cnt,");

//                                sql.Append("0 as ngp_lift, c.nzp_user, c.dat_when, " + finder.ist + " as ist ");
//#if PG
//                                sql.Append("   into unlogged tmp_pcv_" + Convert.ToString(k + 1));
//#endif

//                                sql.Append(" From " + counters);
//                                break;
//                            case (int)CounterKinds.Dom:
//                                sql.Append("c.nzp_dom as nzp," + (int)CounterKinds.Dom + " as nzp_type, c.nzp_counter, " + finder.month_ + " as month_, c.dat_uchet, c.val_cnt, ");
//                                sql.Append("c.ngp_cnt, c.ngp_lift, c.nzp_user, c.dat_when, " + finder.ist + " as ist");
//#if PG
//                                sql.Append("   into unlogged tmp_pcv_" + Convert.ToString(k + 1));
//#endif
//                                sql.Append(" From " + counters_dom);
//                                break;
//                            case (int)CounterKinds.Group:
//                            case (int)CounterKinds.Communal:
//                                sql.Append("0 as nzp," + finder.nzp_type + "as nzp_type, c.nzp_counter, " + finder.month_ + " as month_, c.dat_uchet, c.val_cnt, ");
//                                sql.Append("0 as ngp_cnt, 0 as ngp_lift, c.nzp_user, c.dat_when, " + finder.ist + " as ist");
//#if PG
//                                sql.Append("   into unlogged tmp_pcv_" + Convert.ToString(k + 1));
//#endif
//                                sql.Append(" From " + counters_group);
//                                break;
//                        }

//                        sql.Append(" c, tmp_pcv_" + k + " t ");
//                        sql.Append(" Where c.nzp_counter = t.nzp_counter ");
//                        sql.Append(" and c.dat_uchet = (" + where_dat_uchet + ") ");
//                        sql.Append(" and c.is_actual <> 100 ");
//#if PG

//#else                        
//                        sql.Append(" into temp tmp_pcv_" + Convert.ToString(k + 1) + " with no log");
//#endif


//                        ret = ExecSQL(conn_db, sql.ToString(), true);
//                        if (!ret.result) throw new Exception(ret.text);

//                        if (mode == PrepareCounterValMode.CountersReadings && Constants.Trace)
//                            Utility.ClassLog.WriteLog("PrepareCountersVals: Шаг 4.1." + (k + 1) + " (выборка в временную таблицу): " + sql.ToString());
//                        //--------------------------------------------------------------------------------------------------------------------            
//                        #endregion

//                        #region вставить данные из временной таблицы в counters_vals
//                        //--------------------------------------------------------------------------------------------------------------------            
//                        sql = new StringBuilder();

//#if PG
//                        sql.Append(" Insert into " + counters_vals_full + " (nzp, nzp_type, nzp_counter, month_, dat_uchet, val_cnt, ngp_cnt, ngp_lift, nzp_user, dat_when, ist) ");
//                        sql.Append(" Select c.nzp, c.nzp_type, c.nzp_counter, c.month_, c.dat_uchet, c.val_cnt, c.ngp_cnt, c.ngp_lift, c.nzp_user, c.dat_when, c.ist ");
//                        sql.Append(" From tmp_pcv_" + Convert.ToString(k + 1) + " c ");
//                        sql.Append(" Where 1=1 " + _where);
//#else
//sql.Append(" Insert into " + counters_vals_full + " (nzp, nzp_type, nzp_counter, month_, dat_uchet, val_cnt, ngp_cnt, ngp_lift, nzp_user, dat_when, ist) ");
//                        sql.Append(" Select c.nzp, c.nzp_type, c.nzp_counter, c.month_, c.dat_uchet, c.val_cnt, c.ngp_cnt, c.ngp_lift, c.nzp_user, c.dat_when, c.ist ");
//                        sql.Append(" From tmp_pcv_" + Convert.ToString(k + 1) + " c ");
//                        sql.Append(" Where 1=1 " + _where);
//#endif

//                        ret = ExecSQL(conn_db, sql.ToString(), true);
//                        if (!ret.result) throw new Exception(ret.text);

//                        if (mode == PrepareCounterValMode.CountersReadings && Constants.Trace)
//                            Utility.ClassLog.WriteLog("PrepareCountersVals: Шаг 4.2." + (k + 1) + " (вставка из временной таблицы): " + sql.ToString());
//                        //--------------------------------------------------------------------------------------------------------------------            
//                        #endregion

//                        // удалить предыдущую временную таблицу, кроме tmp_pcv_0, в которой лежат коды обрабатываемых ПУ
//                        if (k != 0 || mode == PrepareCounterValMode.CountersReadings) ExecSQL(conn_db, "Drop table tmp_pcv_" + Convert.ToString(k), false);
//                    }

//                    // удалить последнюю временную таблицу
//                    ExecSQL(conn_db, "Drop table tmp_pcv_" + pastValCount, false);
//                    //--------------------------------------------------------------------------------------------------------------------            
//                    #endregion

//                    #region сохранить первые ближайшие будущие показания
//                    //--------------------------------------------------------------------------------------------------------------------            
//                    if (mode == PrepareCounterValMode.SpisVal)
//                    {
//                        //--------------------------------------------------------------------------------------------------------------------            
//                        // cформировать условие для даты учета будущего показания
//                        where_dat_uchet = "Select min(b.dat_uchet) From ";

//                        switch (finder.nzp_type)
//                        {
//                            case (int)CounterKinds.Kvar:
//                                where_dat_uchet += counters;
//                                break;
//                            case (int)CounterKinds.Dom:
//                                where_dat_uchet += counters_dom;
//                                break;
//                            case (int)CounterKinds.Group:
//                            case (int)CounterKinds.Communal:
//                                where_dat_uchet += counters_group;
//                                break;
//                        }
//                        where_dat_uchet += " b Where t.nzp_counter = b.nzp_counter" +
//                            " and b.dat_uchet > " + dat_uchet_po +
//                            " and b.is_actual <> 100 ";
//                        if (finder.date_begin != "") where_dat_uchet += " and b.dat_uchet >= " + Utils.EStrNull(finder.date_begin);
//                        //--------------------------------------------------------------------------------------------------------------------            

//                        sql = new StringBuilder();
//                        sql.Append(" Insert into " + counters_vals_full + " (nzp, nzp_type, nzp_counter, month_, dat_uchet, val_cnt, ngp_cnt, ngp_lift, nzp_user, dat_when, ist)");
//                        sql.Append(" Select ");

//                        switch (finder.nzp_type)
//                        {
//                            case (int)CounterKinds.Kvar:
//                                sql.Append("c.nzp_kvar, " + (int)CounterKinds.Kvar + ", c.nzp_counter, " + finder.month_ + ", c.dat_uchet, c.val_cnt ");
//                                sql.Append((Points.IsIpuHasNgpCnt ? ", c.ngp_cnt" : ", 0") + ",");
//                                sql.Append("0, c.nzp_user, c.dat_when, " + finder.ist);
//                                sql.Append(" From " + counters);
//                                break;
//                            case (int)CounterKinds.Dom:
//                                sql.Append("c.nzp_dom," + (int)CounterKinds.Dom + ", c.nzp_counter, " + finder.month_ + ", c.dat_uchet, c.val_cnt, ");
//                                sql.Append("c.ngp_cnt, c.ngp_lift, c.nzp_user, c.dat_when, " + finder.ist);
//                                sql.Append(" From " + counters_dom);
//                                break;
//                            case (int)CounterKinds.Group:
//                            case (int)CounterKinds.Communal:
//                                sql.Append("0," + finder.nzp_type + ", c.nzp_counter, " + finder.month_ + ", c.dat_uchet, c.val_cnt, ");
//                                sql.Append("0, 0, c.nzp_user, c.dat_when, " + finder.ist);
//                                sql.Append(" From " + counters_group);
//                                break;
//                        }

//                        sql.Append(" c, tmp_pcv_0 t ");
//                        sql.Append(" Where c.nzp_counter = t.nzp_counter ");
//                        sql.Append(" and c.dat_uchet = (" + where_dat_uchet + ") ");
//                        sql.Append(_where);
//                        if (finder.date_begin != "") sql.Append(" and c.dat_uchet >= " + Utils.EStrNull(finder.date_begin));
//                        sql.Append("    and c.is_actual <> 100");

//                        ret = ExecSQL(conn_db, sql.ToString(), true);
//                        if (!ret.result) throw new Exception(ret.text);

//                        // удалить временную таблицу
//                        ExecSQL(conn_db, "Drop table tmp_pcv_0", false);
//                    }

//                    //--------------------------------------------------------------------------------------------------------------------            
//                    #endregion
//                }
//                catch (Exception ex)
//                {
//                    ExecSQL(conn_db, "Drop table tmp_pcv_0", false);

//                    for (int i = 0; i < pastValCount; i++) ExecSQL(conn_db, "Drop table tmp_pcv_" + Convert.ToString(i + 1), false);

//                    conn_db.Close();
//                    ret = new Returns(false, ex.Message);
//                    return ret;
//                }
//                //--------------------------------------------------------------------------------------------------------------------            
//                #endregion

//                #region Шаг 5
//                /*if (finder.prm == Constants.act_mode_view.ToString())
//            {
//                sql = new StringBuilder();
//                sql.Append(" Delete From " + counters_vals_full);
//                sql.Append(" Where month_ = " + finder.month_);
//                sql.Append(" and val_cnt is null");
//                sql.Append(" and nzp_type = " + finder.nzp_type);
//                switch (finder.nzp_type)
//                {
//                    case (int)CounterKinds.Kvar:
//                        sql.Append(" and nzp = " + finder.nzp_kvar);
//                        break;
//                    case (int)CounterKinds.Dom:
//                        sql.Append(" and nzp = " + finder.nzp_dom);
//                        break;
//                    case (int)CounterKinds.Group:
//                    case (int)CounterKinds.Communal:
//                        if (finder.nzp_kvar > 0)
//                        {
//                            sql.Append(" and nzp_counter in (select distinct cl.nzp_counter from " + counters_link + " cl where cl.nzp_kvar = " + finder.nzp_kvar + ")");
//                        }
//                        else if (finder.nzp_dom > 0)
//                        {
//                            //sql.Append(" and nzp_counter in (select distinct cl.nzp_counter from " + counters_link + " cl, " + kvar + " k where cl.nzp_kvar = k.nzp_kvar and k.nzp_dom = " + finder.nzp_dom + ")");
//                            sql.Append(" and nzp = " + finder.nzp_dom);
//                        }
//                        break;
//                }
//                ret = ExecSQL(conn_db, sql.ToString(), true);
//                if (!ret.result)
//                {
//                    conn_db.Close();
//                    return ret;
//                }
//            }*/
//                #endregion
//#if PG
//                Returns tmp = ExecSQL(conn_db, "set search_path to '" + finder.pref + "_charge_" + (finder.year_ % 100).ToString("00") + "'", false);
//#else
//                Returns tmp = ExecSQL(conn_db, "database " + finder.pref + "_charge_" + (finder.year_ % 100).ToString("00"), false);
//#endif
//                if (tmp.result)
//                {
//#if PG
//                    ret = ExecSQL(conn_db, " analyze  " + counters_vals, true);
//#else
//ret = ExecSQL(conn_db, " Update statistics for table  " + counters_vals, true);
//#endif
//                    if (closeConnDb) conn_db.Close();
//                }

//                return ret;
//            }
//            catch (Exception ex)
//            {
//                MonitorLog.WriteLog("Ошибка в PrepareCountersVals :" + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
//                Returns ret = Utils.InitReturns();
//                ret.result = false;
//                return ret;
//            }
//        }

//        /// <summary>
//        /// Получить префиксы по коду дома или коду улицы
//        /// </summary>
//#warning функции, работающие с counters, counters_dom, counters_group через counters_vals. Не использовать
//        private List<string> FindCounterValPrefList(CounterVal finder, out Returns ret)
//        {
//            ret = new Returns();
//            return null;
//        }

//        /// <summary>
//        /// Получить последние показания приборов учета
//        /// </summary>
//#warning функции, работающие с counters, counters_dom, counters_group через counters_vals. Не использовать
//        public void FindLastCntVal(CounterVal finder, out Returns ret)
//        {
//            if (Constants.Trace) Utility.ClassLog.WriteLog("Старт функции FindLastCntVal");
//            IDbConnection conn_db = null;
//            IDbConnection conn_web = null;
//            List<string> prefList = null;
//            int key = 0;
//            try
//            {
//                ret = Utils.InitReturns();

//                #region проверка значений
//                //-----------------------------------------------------------------------
//                // проверка наличия пользователя
//                if (finder.nzp_user < 1)
//                {
//                    ret = new Returns(false, "Не определен пользователь", -1);
//                    return;
//                }

//                // проверка месяца
//                if (finder.month_ <= 0)
//                {
//                    ret = new Returns(false, "Не определен месяц", -3);
//                    return;
//                }

//                // проверка месяца
//                if (finder.year_ <= 0)
//                {
//                    ret = new Returns(false, "Не определен год", -4);
//                    return;
//                }

//                // проверка даты учета
//                if (finder.dat_uchet.Trim().Length <= 0)
//                {
//                    ret = new Returns(false, "Не определена дата учета", -5);
//                    return;
//                }

//                // определить список префиксов и кодов домов по улице и дому
//                prefList = FindCounterValPrefList(finder, out ret);
//                if (!ret.result) return;

//                //-----------------------------------------------------------------------
//                #endregion

//                DateTime curMonth = Convert.ToDateTime(finder.dat_uchet);

//                #region создать кэш-таблицу
//                //-----------------------------------------------------------------------           
//                conn_web = GetConnection(Constants.cons_Webdata);
//                ret = OpenDb(conn_web, true);
//                if (!ret.result)
//                {
//                    prefList.Clear();
//                    return;
//                }

//#if PG
//                ExecSQL(conn_web, "set search_path to 'public'", false);
//#endif
//                string tXX_cv = "";
//                if (finder.nzp_type == (int)CounterKinds.Kvar)
//                {
//                    // квартирные ПУ
//                    tXX_cv = "t" + Convert.ToString(finder.nzp_user) + "_cv";

//                    if (TableInWebCashe(conn_web, tXX_cv)) ExecSQL(conn_web, " drop table " + tXX_cv, false);

//                    CreateTableWebKvarPuLastCntVal(conn_web, tXX_cv, true, out ret);
//                    if (!ret.result)
//                    {
//                        prefList.Clear();
//                        conn_web.Close();
//                        return;
//                    }
//                }
//                else if (finder.nzp_type == (int)CounterKinds.Dom)
//                {
//                    // домовые ПУ
//                    tXX_cv = "t" + Convert.ToString(finder.nzp_user) + "_dom_cv";

//                    if (TableInWebCashe(conn_web, tXX_cv)) ExecSQL(conn_web, " drop table " + tXX_cv, false);

//                    CreateTableWebDomPuLastCntVal(conn_web, tXX_cv, true, out ret);
//                    if (!ret.result)
//                    {
//                        prefList.Clear();
//                        conn_web.Close();
//                        return;
//                    }
//                }
//                //-----------------------------------------------------------------------
//                #endregion

//                //заполнить webdata
//                string conn_kernel = Points.GetConnByPref(Points.Pref);
//                conn_db = GetConnection(conn_kernel);

//                ret = OpenDb(conn_db, true);
//                if (!ret.result)
//                {
//                    prefList.Clear();
//                    conn_web.Close();
//                    return;
//                }

//#if PG

//                //string tXX_cv_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + "." + tXX_cv;
//                //данное подключение не проверено на корректность:
//                string tXX_cv_full = "public." + tXX_cv;

//#else
//            string tXX_cv_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXX_cv;
//#endif
//                string sql = "";

//                #region сформировать условие
//                //----------------------------------------------------------------------------            
//                // месяц текущих показаний
//#if PG
//                string _where = " and coalesce(cs.dat_close, public.mdy(1,1,3000)) >= " + "'" + curMonth.AddMonths(1).ToShortDateString() + "'";
//#else
//                string _where = " and nvl(cs.dat_close, mdy(1,1,3000)) >= " + "'" + curMonth.AddMonths(1).ToShortDateString() + "'";
//#endif

//                // услуга
//                if (finder.nzp_serv > 0) _where += " and cs.nzp_serv = " + finder.nzp_serv.ToString();
//                // улица
//                if (finder.nzp_ul > 0) _where += " and d.nzp_ul = " + finder.nzp_ul.ToString();
//                // дом
//                if (finder.nzp_dom > 0) _where += " and k.nzp_dom = " + finder.nzp_dom.ToString();
//                // квартира
//                if (finder.nzp_kvar > 0) _where += " and k.nzp_kvar = " + finder.nzp_kvar;
//                // территория
//                if (finder.nzp_area > 0) _where += " and k.nzp_area = " + finder.nzp_area;
//                // роли
//                if (finder.RolesVal != null)
//                {
//                    foreach (_RolesVal role in finder.RolesVal)
//                        if (role.tip == Constants.role_sql)
//                            switch (role.kod)
//                            {
//                                case Constants.role_sql_serv:
//                                    _where += " and cs.nzp_serv in (" + role.val + ")";
//                                    break;
//                                case Constants.role_sql_area:
//                                    _where += " and k.nzp_area in (" + role.val + ")";
//                                    break;
//                                case Constants.role_sql_geu:
//                                    _where += " and k.nzp_geu in (" + role.val + ")";
//                                    break;
//                            }
//                }
//                // только открытые приборы учета
//                _where += " and cs.dat_close is null";
//                // открытые ПУ
//                _where += " and cs.is_actual <> 100 ";
//                //----------------------------------------------------------------------------
//                #endregion

//                string _where_group = "";
//                string _from_group = "";

//                Finder userFinder = new Finder();
//                DbWorkUser dbWorkUser = new DbWorkUser();



//                foreach (string cur_pref in prefList)
//                {
//                    #region сформировать условие для группы
//                    //----------------------------------------------------------------------------
//                    _from_group = "";
//                    if (finder.group_pref.Length > 0 && finder.nzp_group > 0)
//                    {
//                        if (finder.group_pref == cur_pref)
//                        {
//#if PG
//                            _from_group = cur_pref + "_data.link_group l ";
//                            _where_group = " and k.nzp_kvar = l.nzp and l.nzp_group = " + finder.nzp_group;
//#else
//_from_group = cur_pref + "_data:link_group l ";
//                            _where_group = " and k.nzp_kvar = l.nzp and l.nzp_group = " + finder.nzp_group;
//#endif
//                        }
//                    }
//                    else
//                    {
//                        if (finder.ngroup.Length > 0)
//                        {
//#if PG
//                            _from_group = cur_pref + "_data.link_group l, " + cur_pref + "_data.s_group sg ";
//                            _where_group = " and k.nzp_kvar = l.nzp and l.nzp_group = sg.nzp_group and upper(trim(sg.ngroup)) = '" + finder.ngroup.Trim().ToUpper() + "'";
//#else
//_from_group = cur_pref + "_data:link_group l, " + cur_pref + "_data:s_group sg ";
//                            _where_group = " and k.nzp_kvar = l.nzp and l.nzp_group = sg.nzp_group and upper(trim(sg.ngroup)) = '" + finder.ngroup.Trim().ToUpper() + "'";
//#endif
//                        }
//                    }
//                    if (_from_group.Length > 0) _from_group = _from_group + ", ";
//                    //----------------------------------------------------------------------------
//                    #endregion

//                    #region получить код пользователя
//                    //----------------------------------------------------------------------------
//                    userFinder.pref = cur_pref;
//                    userFinder.nzp_user = finder.nzp_user;
//                    userFinder.nzp_user = dbWorkUser.GetLocalUser(conn_db, userFinder, out ret);
//                    //----------------------------------------------------------------------------
//                    #endregion

//                    #region сформировать sql для записи в кэш-таблицу
//                    //----------------------------------------------------------------------------
//                    if (finder.nzp_type == (int)CounterKinds.Kvar)
//                    {
//                        #region
//                        //----------------------------------------------------------------------------
//#if PG
//                        sql = " Insert into " + tXX_cv_full + " (" +
//                                                " pref, nzp_counter, " +
//                                                " nzp_dom, nzp_serv, " +
//                                                " ulica, ndom, nkor, idom,  " +
//                                                " nkvar, ikvar, num_ls, smrlitera, " +
//                                                " fio, nzp, service," +
//                                                " mmnog, cnt_stage," +
//                                                " comment, num_cnt, name_type, " +
//                                                " dat_prov, dat_provnext, " +
//                                                " blocked, show, prepared) " +
//                                              " Select distinct '" + cur_pref + "', cs.nzp_counter, " +
//                                                " d.nzp_dom, cs.nzp_serv, " +
//                                                " ul.ulica, d.ndom, d.nkor, d.idom,  " +
//                                                " trim(coalesce(k.nkvar,''))||'  ком. '||trim(coalesce(k.nkvar_n,'')), k.ikvar, " +
//                                                (Points.IsSmr ? "substr(pkod, 6, 5) as num_ls, case when substr(pkod, 11, 1) = '0' then '' else substr(pkod, 11, 1) end  as litera " : "k.num_ls, '' as litera ") +
//                                                " , k.fio, cs.nzp, cc.name, " +
//                                                " t.mmnog, t.cnt_stage, " +
//                                                " cs.comment, cs.num_cnt, t.name_type, " +
//                                                " cs.dat_prov, cs.dat_provnext, " +
//                            //признак блокировки
//                                                " (case when cs.user_block is not null and cs.user_block <> " + userFinder.nzp_user +
//                                                       " and cs.dat_block is not null and (now() -  INTERVAL '20 minutes') < cs.dat_block then 1 " +
//                                                " else 0 end), " +
//                            // записи не просматриваются    
//                                                " 0, 0 " +
//                                              " From " + cur_pref + "_data.kvar k, " +
//                                                         cur_pref + "_data.dom d, " +
//                                                         cur_pref + "_data.s_ulica ul, " +
//                                                         cur_pref + "_kernel.s_counts cc, " +
//                                                         cur_pref + "_kernel.s_counttypes t, " +
//                                                         cur_pref + "_kernel.services s, " +
//                                                         _from_group +
//                                                         cur_pref + "_data.counters_spis cs " +
//                                             " Where cs.nzp = k.nzp_kvar " +
//                                                 " and k.nzp_dom = d.nzp_dom " +
//                                                 " and d.nzp_ul = ul.nzp_ul " +
//                                                 " and cs.nzp_cnttype = t.nzp_cnttype " +
//                                                 " and cs.nzp_serv = s.nzp_serv " +
//                                                 " and cs.nzp_serv = cc.nzp_serv " +
//                            // квартирные ПУ
//                                                 " and cs.nzp_type = 3 " +
//                            // только открытые лицевые счета
//                                                 " and (select count(*) from " + cur_pref + "_data.prm_3 p3 where p3.nzp_prm = 51 " +
//                                                 " and p3.val_prm = '1' and p3.dat_s <= " + "'" + curMonth.AddMonths(1).ToShortDateString() + "'" +
//                                                 " and p3.dat_po >= " + "'" + curMonth.ToShortDateString() + "'" + " and p3.nzp = k.nzp_kvar and p3.is_actual <> 100) > 0 " + _where + _where_group;
//#else
//    sql = " Insert into " + tXX_cv_full + " (" +
//                            " pref, nzp_counter, " +
//                            " nzp_dom, nzp_serv, " +
//                            " ulica, ndom, nkor, idom,  " +
//                            " nkvar, ikvar, num_ls, smrlitera, " +
//                            " fio, nzp, service," +
//                            " mmnog, cnt_stage," +
//                            " comment, num_cnt, name_type, " +
//                            " dat_prov, dat_provnext, " +
//                            " blocked, show, prepared) " +
//                          " Select distinct '" + cur_pref + "', cs.nzp_counter, " +
//                            " d.nzp_dom, cs.nzp_serv, " +
//                            " ul.ulica, d.ndom, d.nkor, d.idom,  " +
//                            " trim(nvl(k.nkvar,''))||'  ком. '||trim(nvl(k.nkvar_n,'')), k.ikvar, " +
//                            (Points.IsSmr ? "substr(pkod, 6, 5) as num_ls, case when substr(pkod, 11, 1) = '0' then '' else substr(pkod, 11, 1) end  as litera " : "k.num_ls, '' as litera ") +
//                            " , k.fio, cs.nzp, cc.name, " +
//                            " t.mmnog, t.cnt_stage, " +
//                            " cs.comment, cs.num_cnt, t.name_type, " +
//                            " cs.dat_prov, cs.dat_provnext, " +
//                            //признак блокировки
//                            " (case when cs.user_block is not null and cs.user_block <> " + userFinder.nzp_user +
//                                   " and cs.dat_block is not null and (current year to second - 20 units minute) < cs.dat_block then 1 " +
//                            " else 0 end), " +
//                            // записи не просматриваются    
//                            " 0, 0 " +
//                          " From " + cur_pref + "_data:kvar k, " +
//                                     cur_pref + "_data:dom d, " +
//                                     cur_pref + "_data:s_ulica ul, " +
//                                     cur_pref + "_kernel:s_counts cc, " +
//                                     cur_pref + "_kernel:s_counttypes t, " +
//                                     cur_pref + "_kernel:services s, " +
//                                     _from_group +
//                                     cur_pref + "_data:counters_spis cs " +
//                         " Where cs.nzp = k.nzp_kvar " +
//                             " and k.nzp_dom = d.nzp_dom " +
//                             " and d.nzp_ul = ul.nzp_ul " +
//                             " and cs.nzp_cnttype = t.nzp_cnttype " +
//                             " and cs.nzp_serv = s.nzp_serv " +
//                             " and cs.nzp_serv = cc.nzp_serv " +
//                            // квартирные ПУ
//                             " and cs.nzp_type = 3 " +
//                            // только открытые лицевые счета
//                             " and (select count(*) from " + cur_pref + "_data:prm_3 p3 where p3.nzp_prm = 51 " +
//                             " and p3.val_prm = '1' and p3.dat_s <= " + "'" + curMonth.AddMonths(1).ToShortDateString() + "'" +
//                             " and p3.dat_po >= " + "'" + curMonth.ToShortDateString() + "'" + " and p3.nzp = k.nzp_kvar and p3.is_actual <> 100) > 0 " + _where + _where_group;
//#endif
//                        //----------------------------------------------------------------------------
//                        #endregion
//                    }
//                    else if (finder.nzp_type == (int)CounterKinds.Dom)
//                    {
//                        #region
//                        //----------------------------------------------------------------------------
//#if PG
//                        sql = " Insert into " + tXX_cv_full + " (" +
//                                                    " pref, nzp_counter, " +
//                                                    " nzp_dom, nzp_serv, " +
//                                                    " ulica, ndom, nkor, idom,  " +
//                                                    " nzp, service," +
//                                                    " mmnog, cnt_stage," +
//                                                    " num_cnt, name_type, " +
//                                                    " blocked, show, prepared) " +
//                                                  " Select distinct '" + cur_pref + "', cs.nzp_counter, " +
//                                                    " d.nzp_dom, cs.nzp_serv, " +
//                                                    " ul.ulica, d.ndom, d.nkor, d.idom, " +
//                                                    " cs.nzp, cc.name, " +
//                                                    " t.mmnog, t.cnt_stage, " +
//                                                    " cs.num_cnt, t.name_type, " +
//                            //признак блокировки
//                                                    " (case when cs.user_block is not null and cs.user_block <> " + userFinder.nzp_user +
//                                                           " and cs.dat_block is not null and (now() -  INTERVAL '20 minutes') < cs.dat_block then 1 " +
//                                                    " else 0 end), " +
//                            // записи не просматриваются    
//                                                    " 0, 0 " +
//                                                  " From " + cur_pref + "_data.kvar k, " +
//                                                             cur_pref + "_data.dom d, " +
//                                                             cur_pref + "_data.s_ulica ul, " +
//                                                             cur_pref + "_kernel.s_counts cc, " +
//                                                             cur_pref + "_kernel.s_counttypes t, " +
//                                                             cur_pref + "_kernel.services s, " +
//                                                             _from_group +
//                                                             cur_pref + "_data.counters_spis cs " +
//                                                 " Where cs.nzp = d.nzp_dom " +
//                                                     " and k.nzp_dom = d.nzp_dom " +
//                                                     " and d.nzp_ul = ul.nzp_ul " +
//                                                     " and cs.nzp_cnttype = t.nzp_cnttype " +
//                                                     " and cs.nzp_serv = s.nzp_serv " +
//                                                     " and cs.nzp_serv = cc.nzp_serv " +
//                            // домовые ПУ
//                                                     " and cs.nzp_type = 1 " +
//                                                 _where + _where_group;
//#else
//sql = " Insert into " + tXX_cv_full + " (" +
//                            " pref, nzp_counter, " +
//                            " nzp_dom, nzp_serv, " +
//                            " ulica, ndom, nkor, idom,  " +
//                            " nzp, service," +
//                            " mmnog, cnt_stage," +
//                            " num_cnt, name_type, " +
//                            " blocked, show, prepared) " +
//                          " Select distinct '" + cur_pref + "', cs.nzp_counter, " +
//                            " d.nzp_dom, cs.nzp_serv, " +
//                            " ul.ulica, d.ndom, d.nkor, d.idom, " +
//                            " cs.nzp, cc.name, " +
//                            " t.mmnog, t.cnt_stage, " +
//                            " cs.num_cnt, t.name_type, " +
//                            //признак блокировки
//                            " (case when cs.user_block is not null and cs.user_block <> " + userFinder.nzp_user +
//                                   " and cs.dat_block is not null and (current year to second - 20 units minute) < cs.dat_block then 1 " +
//                            " else 0 end), " +
//                            // записи не просматриваются    
//                            " 0, 0 " +
//                          " From " + cur_pref + "_data:kvar k, " +
//                                     cur_pref + "_data:dom d, " +
//                                     cur_pref + "_data:s_ulica ul, " +
//                                     cur_pref + "_kernel:s_counts cc, " +
//                                     cur_pref + "_kernel:s_counttypes t, " +
//                                     cur_pref + "_kernel:services s, " +
//                                     _from_group +
//                                     cur_pref + "_data:counters_spis cs " +
//                         " Where cs.nzp = d.nzp_dom " +
//                             " and k.nzp_dom = d.nzp_dom " +
//                             " and d.nzp_ul = ul.nzp_ul " +
//                             " and cs.nzp_cnttype = t.nzp_cnttype " +
//                             " and cs.nzp_serv = s.nzp_serv " +
//                             " and cs.nzp_serv = cc.nzp_serv " +
//                            // домовые ПУ
//                             " and cs.nzp_type = 1 " +
//                         _where + _where_group;
//#endif
//                        //----------------------------------------------------------------------------
//                        #endregion
//                    }
//                    //----------------------------------------------------------------------------
//                    #endregion

//                    //записать текст sql в лог-журнал
//                    key = LogSQL(conn_db, finder.nzp_user, sql);

//                    if (Constants.Trace) Utility.ClassLog.WriteLog("FINDLASCNTVAL: Запись в кэш-таблицу: старт");

//                    ret = ExecSQL(conn_db, sql, true);
//                    if (!ret.result) throw new Exception(ret.text);

//                    if (Constants.Trace) Utility.ClassLog.WriteLog("FINDLASCNTVAL: Запись в кэш-таблицу: стоп");
//                }

//                //создать индексы на tXX_cv
//                if (finder.nzp_type == (int)CounterKinds.Kvar) CreateTableWebKvarPuLastCntVal(conn_web, tXX_cv, false, out ret);
//                else if (finder.nzp_type == (int)CounterKinds.Dom) CreateTableWebDomPuLastCntVal(conn_web, tXX_cv, false, out ret);
//                if (!ret.result) throw new Exception(ret.text);

//            }
//            catch (Exception ex)
//            {
//                ret = Utils.InitReturns();
//                ret.result = false;
//                if (key > 0) LogSQL_Error(conn_web, key, ex.Message);
//                conn_db.Close();
//                conn_web.Close();
//                prefList.Clear();
//                return;
//            }

//            conn_db.Close(); //закрыть соединение с основной базой
//            conn_web.Close();
//            prefList.Clear();
//            if (Constants.Trace) Utility.ClassLog.WriteLog("Финиш функции FindLastCntVal");
//            return;
//        }

//        /// <summary>
//        /// Создать кэш-таблицу для последних показаний индивидуальных приборов учета
//        /// </summary>
//#warning функции, работающие с counters, counters_dom, counters_group через counters_vals. Не использовать
//        private void CreateTableWebKvarPuLastCntVal(IDbConnection conn_web, string tXX_cv, bool onCreate, out Returns ret) //
//        {
//            if (onCreate)
//            {
//                if (TableInWebCashe(conn_web, tXX_cv))
//                {
//                    ExecSQL(conn_web, " Drop table " + tXX_cv, false);
//                }

//                //создать таблицу webdata:tXX_cv
//#if PG
//                ExecSQL(conn_web, "set search_path to 'public'", false);

//                ret = ExecSQL(conn_web,
//                                  " Create table " + tXX_cv + "(" +
//                                  " nzp_serial     serial, " +
//                                  " pref           char(20), " +
//                                  " nzp_cv         integer," +
//                                  " nzp_counter    integer," +
//                                  " nzp_dom        integer," +
//                                  " nzp_serv       integer," +
//                                  " ulica          char(40)," +
//                                  " ndom           char(15)," +
//                                  " idom           integer," +
//                                  " nkor           char(15)," +
//                                  " nkvar          char(20)," +
//                                  " ikvar          integer," +
//                                  " num_ls         integer," +
//                    // самарская литера для различения комнат в коммуналках 
//                                  " smrlitera      char(1)," +
//                                  " fio            nchar(40)," +
//                                  " nzp            integer," +
//                                  " service        char(100)," +
//                                  " val_cnt        float," +
//                                  " dat_uchet      date," +
//                                  " val_cnt_pred   float," +
//                                  " dat_uchet_pred date," +
//                    // расход на нежилые помещения
//                                  (Points.IsIpuHasNgpCnt ? " ngp_cnt numeric(14,7)," : "") +
//                                  " mmnog          numeric," +
//                                  " cnt_stage      integer," +
//                                  " comment        char(60)," +
//                                  " num_cnt        char(20)," +
//                                  " name_type      char(40)," +
//                                  " user_          nchar(100)," +
//                                  " dat_when       date," +
//                                  " dat_prov       date, " +
//                                  " dat_provnext   date, " +
//                                  " blocked        integer, " +
//                                  " show           integer," +
//                                  " prepared       integer)", true);
//#else
//    ret = ExecSQL(conn_web,
//                      " Create table " + tXX_cv + "(" +
//                      " nzp_serial     serial(1), " +
//                      " pref           char(20), " +    
//                      " nzp_cv         integer," +
//                      " nzp_counter    integer," +
//                      " nzp_dom        integer," + 
//                      " nzp_serv       integer," +
//                      " ulica          char(40)," +
//                      " ndom           char(15)," +
//                      " idom           integer," + 
//                      " nkor           char(15)," +
//                      " nkvar          char(10)," +
//                      " ikvar          integer," +
//                      " num_ls         integer," +
//                      // самарская литера для различения комнат в коммуналках 
//                      " smrlitera      char(1)," +
//                      " fio            nchar(40)," +
//                      " nzp            integer," +
//                      " service        char(100)," +
//                      " val_cnt        float," +
//                      " dat_uchet      date," +
//                      " val_cnt_pred   float," +
//                      " dat_uchet_pred date," +
//                      // расход на нежилые помещения
//                      (Points.IsIpuHasNgpCnt ? " ngp_cnt decimal(14,7)," : "") +
//                      " mmnog          decimal," +
//                      " cnt_stage      integer," +
//                      " comment        char(60)," +
//                      " num_cnt        char(20)," +
//                      " name_type      char(40)," +
//                      " user_          nchar(100)," +
//                      " dat_when       date," +
//                      " dat_prov       date, " +
//                      " dat_provnext   date, " + 
//                      " blocked        integer, " + 
//                      " show           integer," +
//                      " prepared       integer)", true);
//#endif

//                if (!ret.result)
//                {
//                    conn_web.Close();
//                    return;
//                }
//            }
//            else
//            {
//                ret = ExecSQL(conn_web, " Create index ix1_" + tXX_cv + " on " + tXX_cv + " (nzp_serial) ", true);
//                if (ret.result) { ret = ExecSQL(conn_web, " Create index ix_2" + tXX_cv + " on " + tXX_cv + " (nzp_counter) ", true); }
//                if (ret.result) { ret = ExecSQL(conn_web, " Create index ix_3" + tXX_cv + " on " + tXX_cv + " (pref) ", true); }
//                if (ret.result) { ret = ExecSQL(conn_web, " Create index ix_4" + tXX_cv + " on " + tXX_cv + " (show) ", true); }
//                if (ret.result) { ret = ExecSQL(conn_web, " Create index ix_5" + tXX_cv + " on " + tXX_cv + " (prepared) ", true); }
//                if (ret.result) { ret = ExecSQL(conn_web, " Create index ix_6" + tXX_cv + " on " + tXX_cv + " (ulica, idom, ndom, nkor, ikvar, nkvar, num_ls, service, name_type, num_cnt, nzp_counter) ", true); }
//            }
//        }

//        /// <summary>
//        /// Создать кэш-таблицу для последних показаний индивидуальных приборов учета
//        /// </summary>
//#warning функции, работающие с counters, counters_dom, counters_group через counters_vals. Не использовать
//        private void CreateTableWebDomPuLastCntVal(IDbConnection conn_web, string tXX_cv, bool onCreate, out Returns ret) //
//        {
//            if (onCreate)
//            {
//                if (TableInWebCashe(conn_web, tXX_cv))
//                {
//                    ExecSQL(conn_web, " Drop table " + tXX_cv, false);
//                }

//                //создать таблицу webdata:tXX_cv
//#if PG
//                ret = ExecSQL(conn_web,
//                                  " Create table " + tXX_cv + "(" +
//                                  " nzp_serial     serial, " +
//                                  " pref           char(20), " +
//                                  " nzp_cv         integer," +
//                                  " nzp_counter    integer," +
//                                  " ulica          char(40)," +
//                                  " nzp_dom        integer," +
//                                  " nzp_serv       integer," +
//                                  " ndom           char(15)," +
//                                  " idom           integer," +
//                                  " nkor           char(15)," +
//                                  " nzp            integer," +
//                                  " service        char(100)," +
//                                  " val_cnt        float," +
//                                  " dat_uchet      date," +
//                                  " ngp_cnt        numeric(14,7)," +
//                                  " ngp_lift       numeric(14,7), " +
//                                  " sred_rashod    float, " +
//                                  " val_cnt_pred   float," +
//                                  " dat_uchet_pred date," +
//                                  " mmnog          numeric," +
//                                  " cnt_stage      integer," +
//                                  " num_cnt        char(20)," +
//                                  " name_type      char(40)," +
//                                  " user_          nchar(100)," +
//                                  " dat_when       date," +
//                                  " blocked        integer, " +
//                                  " show           integer," +
//                                  " prepared       integer)", true);
//#else
//    ret = ExecSQL(conn_web,
//                      " Create table " + tXX_cv + "(" +
//                      " nzp_serial     serial(1), " +
//                      " pref           char(20), " +
//                      " nzp_cv         integer," +
//                      " nzp_counter    integer," +
//                      " ulica          char(40)," +
//                      " nzp_dom        integer," +
//                      " nzp_serv       integer," +
//                      " ndom           char(15)," +
//                      " idom           integer," +
//                      " nkor           char(15)," +
//                      " nzp            integer," +
//                      " service        char(100)," +
//                      " val_cnt        float," +
//                      " dat_uchet      date," +
//                      " ngp_cnt        decimal(14,7)," +
//                      " ngp_lift       decimal(14,7), " +
//                      " sred_rashod    float, " +
//                      " val_cnt_pred   float," +
//                      " dat_uchet_pred date," +
//                      " mmnog          decimal," +
//                      " cnt_stage      integer," +
//                      " num_cnt        char(20)," +
//                      " name_type      char(40)," +
//                      " user_          nchar(100)," +
//                      " dat_when       date," +
//                      " blocked        integer, " +
//                      " show           integer," +
//                      " prepared       integer)", true);
//#endif

//                if (!ret.result)
//                {
//                    conn_web.Close();
//                    return;
//                }
//            }
//            else
//            {
//                ret = ExecSQL(conn_web, " Create index ix1_" + tXX_cv + " on " + tXX_cv + " (nzp_serial) ", true);
//                if (ret.result) { ret = ExecSQL(conn_web, " Create index ix_2" + tXX_cv + " on " + tXX_cv + " (nzp_counter) ", true); }
//                if (ret.result) { ret = ExecSQL(conn_web, " Create index ix_3" + tXX_cv + " on " + tXX_cv + " (pref) ", true); }
//                if (ret.result) { ret = ExecSQL(conn_web, " Create index ix_4" + tXX_cv + " on " + tXX_cv + " (show) ", true); }
//                if (ret.result) { ret = ExecSQL(conn_web, " Create index ix_5" + tXX_cv + " on " + tXX_cv + " (prepared) ", true); }
//                if (ret.result) { ret = ExecSQL(conn_web, " Create index ix_6" + tXX_cv + " on " + tXX_cv + " (ulica, idom, ndom, nkor, service, name_type, num_cnt, nzp_counter) ", true); }
//            }
//        }

//        /// <summary>
//        /// Получить список показаний ПУ из кэша
//        /// </summary>
//#warning функции, работающие с counters, counters_dom, counters_group через counters_vals. Не использовать
//        public List<CounterVal> GetLastCntVal(CounterVal finder, out Returns ret)
//        {
//            if (Constants.Trace) Utility.ClassLog.WriteLog("Старт функции GetLastCntVal");
//            IDataReader reader = null;
//            IDataReader reader2 = null;
//            IDbConnection conn_web = null;
//            IDbConnection conn_db = null;

//            string sql = "";
//            object count;
//            int pu_count = 0;
//            int total = 0;
//            string skip = "";
//            string first = "";
//            string order = "";
//            string tXX_cv = "";

//            try
//            {
//                #region проверка значений
//                //------------------------------------------------------------------
//                ret = Utils.InitReturns();
//                if (finder.nzp_user < 1)
//                {
//                    ret = new Returns(false, "Не определен пользователь", -1);
//                    return null;
//                }
//                //------------------------------------------------------------------
//                #endregion

//                #region подключение к web + проверка, что данные были выбраны
//                //------------------------------------------------------------------
//                conn_web = GetConnection(Constants.cons_Webdata);
//                ret = OpenDb(conn_web, true);
//                if (!ret.result) return null;
//#if PG
//                ExecSQL(conn_web, "set search_path to 'public'", false);
//#endif

//                if (finder.nzp_type == (int)CounterKinds.Kvar) tXX_cv = "t" + Convert.ToString(finder.nzp_user) + "_cv";
//                else if (finder.nzp_type == (int)CounterKinds.Dom) tXX_cv = "t" + Convert.ToString(finder.nzp_user) + "_dom_cv";

//                if (!TableInWebCashe(conn_web, tXX_cv))
//                {
//                    conn_web.Close();
//                    ret = new Returns(false, "Данные не были выбраны", -22);
//                    return null;
//                }
//                //------------------------------------------------------------------
//                #endregion

//#if PG
//                tXX_cv = "public." + tXX_cv;
//                if (finder.skip > 0) skip = " offset " + finder.skip;
//                if (finder.rows > 0) first = " limit " + finder.rows;
//#else
//                if (finder.skip > 0) skip = " skip " + finder.skip;
//                if (finder.rows > 0) first = " first " + finder.rows;
//#endif

//                if (finder.nzp_type == (int)CounterKinds.Kvar) order = " ulica, idom, ndom, nkor, ikvar, nkvar, num_ls, service, name_type, num_cnt, nzp_counter ";
//                else if (finder.nzp_type == (int)CounterKinds.Dom) order = " ulica, idom, ndom, nkor, service, name_type, num_cnt, nzp_counter ";

//                #region подключиться к базе
//                //--------------------------------------------------------------------            
//                string connectionString = Points.GetConnByPref(finder.pref);
//                conn_db = GetConnection(connectionString);
//                ret = OpenDb(conn_db, true);

//                if (!ret.result)
//                {
//                    conn_web.Close();
//                    return null;
//                }
//                //--------------------------------------------------------------------            
//                #endregion

//                DateTime curMonth = Convert.ToDateTime(finder.dat_uchet);

//                if (Constants.Trace) Utility.ClassLog.WriteLog("GETLASCNTVAL: ************************* НАЧАЛО *****************************");

//                #region Шаг 1 подсчитать общее количество записей
//                //------------------------------------------------------------------
//                count = ExecScalar(conn_web, " Select count(*) From " + tXX_cv + " Where 1=1 ", out ret, true);
//                if (!ret.result) throw new Exception(ret.text);
//                total = Convert.ToInt32(count);
//                if (Constants.Trace) Utility.ClassLog.WriteLog("GETLASCNTVAL: Шаг 1 (подсчитать общее количество записей): " + sql);
//                //-------------------------------------------------
//                #endregion

//                #region Шаг 2 снять пометку о том, что записи просматриваются
//                //------------------------------------------------------------------
//                sql = " Update " + tXX_cv + " Set show = 0 Where show = 1 ";

//                ret = ExecSQL(conn_web, sql, true);
//                if (!ret.result) throw new Exception(ret.text);
//                if (Constants.Trace) Utility.ClassLog.WriteLog("GETLASCNTVAL: Шаг 2 (снять пометку о том, что записи просматриваются): " + sql);
//                //------------------------------------------------------------------
//                #endregion

//                #region Шаг 3 в кэше сохранить во временную таблицу коды просматриваемых приборов учета
//                //------------------------------------------------------------------
//                // удалить временную таблицу, ошибки не обрабатывать
//                ExecSQL(conn_web, " Drop table tmp_show_cv_web ", false);
//#if PG
//                sql = " Select " + order + ", nzp_serial into temp tmp_show_cv_web From " + tXX_cv + " Order by " + order + first + skip;
//#else
//                sql = " Select " + skip + first + order + ", nzp_serial From " + tXX_cv + " Order by " + order + " into temp tmp_show_cv_web with no log ";
//#endif
//                ret = ExecSQL(conn_web, sql, true);
//                if (!ret.result) throw new Exception(ret.text);

//                if (Constants.Trace) Utility.ClassLog.WriteLog("GETLASCNTVAL: Шаг 3 (в кэше сохранить во временную таблицу коды просматриваемых приборов учета): " + sql);
//                //------------------------------------------------------------------
//                #endregion

//                #region Шаг 4 обновить в кэше информацию о том, какие приборы учета просматриваются
//                //------------------------------------------------------------------
//                sql = " Update " + tXX_cv + " Set show = 1 Where nzp_serial in (Select nzp_serial From tmp_show_cv_web)";

//                ret = ExecSQL(conn_web, sql, true);
//                if (!ret.result) throw new Exception(ret.text);
//                if (Constants.Trace) Utility.ClassLog.WriteLog("GETLASCNTVAL: Шаг 4 (сохранить в кэше информацию о том, какие приборы учета просматриваются): " + sql);
//                //------------------------------------------------------------------
//                #endregion

//                #region Шаг 5 подсчитать количество ПУ, которые просматриваются и данные для которых не подготовлены
//                //--------------------------------------------------------------------------
//                sql = " Select count(*) From " + tXX_cv + " Where show = 1 and prepared = 0";

//                count = ExecScalar(conn_web, sql, out ret, true);
//                if (!ret.result) throw new Exception(ret.text);
//                pu_count = Convert.ToInt32(count);
//                if (Constants.Trace) Utility.ClassLog.WriteLog("GETLASCNTVAL: Шаг 5 (подсчитать количество ПУ, которые просматриваются и данные для которых не подготовлены): " + sql);
//                //--------------------------------------------------------------------------
//                #endregion

//                if (pu_count > 0 || finder.prm == Constants.act_mode_edit.ToString())
//                {
//                    #region Шаг 6 в банке создать временную таблицу
//                    //-------------------------------------------------------------------------- 
//                    ExecSQL(conn_db, " Drop table tmp_show_cv_db ", false);

//                    sql = "Create temp table tmp_show_cv_db" +
//                        "( nzp_counter integer, " +
//                        "  pref        char(20), " +
//                        "  prepared     integer " +
//                        ")";
//                    if (DBManager.tableDelimiter == ":") sql += " with no log";

//                    ret = ExecSQL(conn_db, sql, true);
//                    if (!ret.result) throw new Exception(ret.text);
//                    if (Constants.Trace) Utility.ClassLog.WriteLog("GETLASCNTVAL: Шаг 6 (в банке создать временную таблицу): " + sql);
//                    //-------------------------------------------------------------------------- 
//                    #endregion

//                    #region Шаг 7 получить коды, префиксы и признак подготовленных данных для просматриваемых ПУ
//                    //-------------------------------------------------------------------------- 
//                    sql = " Select t.pref, t.nzp_counter, t.prepared From " + tXX_cv + " t Where t.show = 1 ";
//                    ret = ExecRead(conn_web, out reader, sql, true);
//                    if (!ret.result) throw new Exception(ret.text);
//                    if (Constants.Trace) Utility.ClassLog.WriteLog("GETLASCNTVAL: Шаг 7 (получить коды, префиксы и признак подготовленных данных для просматриваемых ПУ): " + sql);
//                    //-------------------------------------------------------------------------- 
//                    #endregion

//                    #region Шаг 8 сохранить во временную таблицу банка коды, префиксы и признак подготовленных данных для просматриваемых ПУ
//                    //-------------------------------------------------------------------------- 
//                    string pref = "";
//                    int nzp_counter = 0;
//                    int prepared = 0;

//                    while (reader.Read())
//                    {
//                        if (reader["pref"] != DBNull.Value) pref = Convert.ToString(reader["pref"]).Trim();
//                        if (reader["nzp_counter"] != DBNull.Value) nzp_counter = Convert.ToInt32(reader["nzp_counter"]);
//                        if (reader["prepared"] != DBNull.Value) prepared = Convert.ToInt32(reader["prepared"]);

//                        sql = "Insert into tmp_show_cv_db (pref, nzp_counter, prepared) values (" + Utils.EStrNull(pref) + ", " + nzp_counter + ", " + prepared + ")";
//                        ret = ExecSQL(conn_db, sql, true);
//                        if (!ret.result) throw new Exception(ret.text);
//                        if (Constants.Trace) Utility.ClassLog.WriteLog("GETLASCNTVAL: Шаг 8 (сохранить во временную таблицу банка коды, префиксы и признак подготовленных данных для просматриваемых ПУ): " + sql);
//                    }
//                    //-------------------------------------------------------------------------- 
//                    #endregion
//                }

//                // если есть ПУ, данные для которых не подготовлены,
//                if (pu_count > 0)
//                {
//                    #region Шаг 9 получить список префиксов просматриваемых ПУ, данные для которых не подготовлены
//                    //------------------------------------------------------------------
//                    List<string> prefList = new List<string>();
//                    sql = "Select distinct pref From tmp_show_cv_db Where prepared = 0 Order by 1";
//                    ret = ExecRead(conn_db, out reader, sql, true);
//                    if (!ret.result) throw new Exception(ret.text);
//                    if (Constants.Trace) Utility.ClassLog.WriteLog("GETLASCNTVAL: Шаг 9 (получить список префиксов просматриваемых ПУ, данные для которых не подготовлены): " + sql);

//                    while (reader.Read())
//                    {
//                        if (reader["pref"] != DBNull.Value) prefList.Add(Convert.ToString(reader["pref"]).Trim());
//                    }
//                    reader.Close();
//                    //------------------------------------------------------------------
//                    #endregion

//                    #region Шаг 10 подготовить данные c помощью функции PrepareCountersVals
//                    //-----------------------------------------------------------------------
//                    CounterVal newFinder = new CounterVal();
//                    newFinder.nzp_type = finder.nzp_type;
//                    // добавить месяц к дате учета (особенности функции PrepareCountersVals)
//                    newFinder.dat_uchet = curMonth.AddMonths(1).ToShortDateString();
//                    newFinder.ist = (int)CounterVal.Ist.Operator;
//                    newFinder.month_ = finder.month_;
//                    newFinder.year_ = finder.year_;
//                    newFinder.nzp_user = finder.nzp_user;

//                    // значения с потолка
//                    //--------------------------------------------------------------------------
//                    newFinder.nzp_kvar = 1000;
//                    newFinder.nzp_dom = 1000;
//                    //--------------------------------------------------------------------------

//                    foreach (string curPref in prefList)
//                    {
//                        newFinder.pref = curPref;
//                        string select_pu = " (Select t.nzp_counter From tmp_show_cv_db t Where t.pref = " + Utils.EStrNull(curPref) + " and t.prepared = 0) ";
//                        ret = PrepareCountersVals(newFinder, PrepareCounterValMode.CountersReadings, select_pu, conn_db);
//                        if (!ret.result) throw new Exception(ret.text);
//                        if (Constants.Trace) Utility.ClassLog.WriteLog("GETLASCNTVAL: Шаг 10 (подготовить данные с помощью функции PrepareCountersVals): " + select_pu);
//                    }
//                    //-----------------------------------------------------------------------
//                    #endregion

//                    #region добавить информацию о показаниях
//                    //-----------------------------------------------------------------------
//                    string counters_vals = "";
//                    string selectSredRashod = "";

//                    CounterVal tmpVal = new CounterVal();

//                    foreach (string curPref in prefList)
//                    {
//                        #region Шаг 11 получить информацию о показаниях
//                        //-----------------------------------------------------------------------
//                        counters_vals = curPref + "_charge_" + (finder.year_ % 100).ToString("00") + DBManager.tableDelimiter + "counters_vals";

//                        // средний расход для общедомовых приборов учета
//                        if (finder.nzp_type == (int)CounterKinds.Dom)
//                        {

//                            selectSredRashod = "(Select p17.val_prm " +
//                                " From " + curPref + "_data" + DBManager.tableDelimiter + "prm_17 p17 " +
//                                " Where p17.nzp = cv.nzp_counter " +
//                                    " and p17.nzp_prm = 979 " +
//                                    " and p17.is_actual = 1 " +
//                                    " and p17.dat_s >= " + Utils.EStrNull(curMonth.ToShortDateString()) +
//                                    " and p17.dat_po <= " + Utils.EStrNull(curMonth.AddMonths(1).AddDays(-1).ToShortDateString()) + ")";
//                        }

//#if PG
//                        sql = " Select cv.nzp_cv, cv.nzp_counter, cv.val_cnt, cv.dat_uchet, a.val_cnt as val_cnt_pred, a.dat_uchet as dat_uchet_pred, " +
//                            // расход на нежилые помещения
//                                                        ((finder.nzp_type == (int)CounterKinds.Dom) || (finder.nzp_type == (int)CounterKinds.Kvar && Points.IsIpuHasNgpCnt) ? "cv.ngp_cnt, " : "") +
//                            // расход на электроснабжение лифтов для общедомовых приборов учета
//                                                        (finder.nzp_type == (int)CounterKinds.Dom ? "cv.ngp_lift," : "") +
//                            // пользователь, которые изменил данные
//                                                        " uu.comment, cv.dat_when " +
//                            // средний расход для общедомовых приборов учета
//                                                        (finder.nzp_type == (int)CounterKinds.Dom ? ", " + selectSredRashod + " as sred_rashod" : "") +
//                                                    " From tmp_show_cv_db t, " +
//                                                        counters_vals + " cv " +
//                                                        " left outer join " + curPref + "_data.users uu on uu.nzp_user = cv.nzp_user " +
//                                                        " left outer join " + counters_vals + "      a  on a.nzp_counter = cv.nzp_counter " +
//                                                    " Where t.nzp_counter = cv.nzp_counter" +
//                                                        " and t.pref = " + Utils.EStrNull(curPref) +
//                                                        " and t.prepared = 0 " +
//                                                        " and cv.dat_uchet between " + Utils.EStrNull(curMonth.AddDays(1).ToShortDateString()) + " and " + Utils.EStrNull(curMonth.AddMonths(1).ToShortDateString()) +
//                                                        " and a.dat_uchet = (select max(b.dat_uchet) from " + counters_vals + " b where b.nzp_counter = a.nzp_counter " +
//                                                                                " and b.dat_uchet <= " + Utils.EStrNull(curMonth.ToShortDateString()) +
//                                                                                " and b.month_ = " + finder.month_ + ")" +
//                            // источник данных - оператор
//                                                        " and cv.ist = " + (int)CounterVal.Ist.Operator +
//                                                        " and cv.month_ = " + finder.month_ +
//                                                        " and a.month_ = " + finder.month_;
//#else
//                        sql = " Select cv.nzp_cv, cv.nzp_counter, cv.val_cnt, cv.dat_uchet, a.val_cnt as val_cnt_pred, a.dat_uchet as dat_uchet_pred, " +
//                            // расход на нежилые помещения
//                                ((finder.nzp_type == (int)CounterKinds.Dom) || (finder.nzp_type == (int)CounterKinds.Kvar && Points.IsIpuHasNgpCnt) ? "cv.ngp_cnt, " : "") +
//                            // расход на электроснабжение лифтов для общедомовых приборов учета
//                                (finder.nzp_type == (int)CounterKinds.Dom ? "cv.ngp_lift," : "") +
//                            // пользователь, которые изменил данные
//                                " uu.comment, cv.dat_when " +
//                            // средний расход для общедомовых приборов учета
//                                (finder.nzp_type == (int)CounterKinds.Dom ? ", " + selectSredRashod + " as sred_rashod" : "") +
//                            " From tmp_show_cv_db t, " +
//                                counters_vals + " cv, " +
//                                " outer " + curPref + "_data:users uu,  " +
//                                " outer " + counters_vals + " a " +
//                            " Where t.nzp_counter = cv.nzp_counter" +
//                                " and t.pref = " + Utils.EStrNull(curPref) +
//                                " and t.prepared = 0 " +
//                                " and cv.dat_uchet between " + Utils.EStrNull(curMonth.AddDays(1).ToShortDateString()) + " and " + Utils.EStrNull(curMonth.AddMonths(1).ToShortDateString()) +
//                                " and uu.nzp_user = cv.nzp_user " +
//                                " and a.nzp_counter = cv.nzp_counter " +
//                                " and a.dat_uchet = (select max(b.dat_uchet) from " + counters_vals + " b where b.nzp_counter = a.nzp_counter " +
//                                                        " and b.dat_uchet <= " + Utils.EStrNull(curMonth.ToShortDateString()) +
//                                                        " and b.month_ = " + finder.month_ + ")" +
//                            // источник данных - оператор
//                                " and cv.ist = " + (int)CounterVal.Ist.Operator +
//                                " and cv.month_ = " + finder.month_ +
//                                " and a.month_ = " + finder.month_;
//#endif

//                        ret = ExecRead(conn_db, out reader, sql, true);
//                        if (!ret.result) throw new Exception(ret.text);
//                        if (Constants.Trace) Utility.ClassLog.WriteLog("GETLASCNTVAL: Шаг 11 (получить информацию о показаниях): " + sql);
//                        //-----------------------------------------------------------------------
//                        #endregion

//                        while (reader.Read())
//                        {
//                            #region Шаг 12 сохранить информацию о показаниях
//                            //-----------------------------------------------------------------------
//                            if (reader["nzp_counter"] != DBNull.Value) tmpVal.nzp_counter = Convert.ToInt32(reader["nzp_counter"]);
//                            if (reader["nzp_cv"] != DBNull.Value) tmpVal.nzp_cv = Convert.ToInt32(reader["nzp_cv"]);

//                            if (reader["val_cnt"] != DBNull.Value) tmpVal.val_cnt_s = Convert.ToString(reader["val_cnt"]).Trim();
//                            else tmpVal.val_cnt_s = "";

//                            if (reader["dat_uchet"] != DBNull.Value) tmpVal.dat_uchet = Convert.ToDateTime(reader["dat_uchet"]).ToShortDateString();
//                            else tmpVal.dat_uchet = "";

//                            if (reader["val_cnt_pred"] != DBNull.Value) tmpVal.val_cnt_pred_s = Convert.ToString(reader["val_cnt_pred"]).Trim();
//                            else tmpVal.val_cnt_pred_s = "";

//                            if (reader["dat_uchet_pred"] != DBNull.Value) tmpVal.dat_uchet_pred = Convert.ToDateTime(reader["dat_uchet_pred"]).ToShortDateString();
//                            else tmpVal.dat_uchet_pred = "";

//                            if (reader["comment"] != DBNull.Value) tmpVal.comment = Convert.ToString(reader["comment"]).Trim();
//                            else tmpVal.comment = "";

//                            if (reader["dat_when"] != DBNull.Value) tmpVal.dat_when = Convert.ToDateTime(reader["dat_when"]).ToShortDateString();
//                            else tmpVal.dat_when = "";

//                            if ((finder.nzp_type == (int)CounterKinds.Dom) || (finder.nzp_type == (int)CounterKinds.Kvar && Points.IsIpuHasNgpCnt))
//                            {
//                                if (reader["ngp_cnt"] != DBNull.Value) tmpVal.ngp_cnt = Convert.ToDecimal(reader["ngp_cnt"]);
//                                else tmpVal.ngp_cnt = 0;
//                            }

//                            if (finder.nzp_type == (int)CounterKinds.Dom)
//                            {
//                                if (reader["ngp_lift"] != DBNull.Value) tmpVal.ngp_lift = Convert.ToDecimal(reader["ngp_lift"]);
//                                else tmpVal.ngp_cnt = 0;

//                                #region средний расход
//                                //----------------------------------------------------------------------------------------------
//                                if (reader["sred_rashod"] != DBNull.Value) tmpVal.sred_rashod = Convert.ToString(reader["sred_rashod"]).Trim();
//                                else tmpVal.sred_rashod = "";
//                                //----------------------------------------------------------------------------------------------
//                                #endregion
//                            }

//                            sql = " Update " + tXX_cv + " Set prepared = 1, " +
//                                    " nzp_cv = " + tmpVal.nzp_cv +
//                                    (tmpVal.val_cnt_s.Length > 0 ? ", val_cnt = " + tmpVal.val_cnt_s : "") +
//                                    (tmpVal.dat_uchet.Length > 0 ? ", dat_uchet = " + Utils.EStrNull(tmpVal.dat_uchet) : "") +
//                                // предыдущее показание
//                                    (tmpVal.val_cnt_pred_s.Length > 0 ? ", val_cnt_pred = " + tmpVal.val_cnt_pred_s : "") +
//                                // дата снятия предыдущего показания
//                                    (tmpVal.dat_uchet_pred.Length > 0 ? ", dat_uchet_pred = " + Utils.EStrNull(tmpVal.dat_uchet_pred) : "") +
//                                // пользователь
//                                    (tmpVal.comment.Length > 0 ? ", user_ = " + Utils.EStrNull(tmpVal.comment) : "") +
//                                // дата изменения значений ПУ
//                                    (tmpVal.dat_when.Length > 0 ? ", dat_when = " + Utils.EStrNull(tmpVal.dat_when) : "") +
//                                // расход на нежилые помещения
//                                    ((finder.nzp_type == (int)CounterKinds.Dom) || (finder.nzp_type == (int)CounterKinds.Kvar && Points.IsIpuHasNgpCnt) ? ", ngp_cnt = " + tmpVal.ngp_cnt : "") +
//                                // расход на электороснабжение лифтов ОДПУ
//                                    (finder.nzp_type == (int)CounterKinds.Dom ? ", ngp_lift = " + tmpVal.ngp_lift : "") +
//                                // средний расход ОДПУ
//                                    (finder.nzp_type == (int)CounterKinds.Dom && tmpVal.sred_rashod.Length > 0 ? ", sred_rashod = " + tmpVal.sred_rashod : "") +
//                                " Where nzp_counter = " + tmpVal.nzp_counter +
//                                    " and pref = " + Utils.EStrNull(curPref);

//                            ret = ExecSQL(conn_web, sql, true);
//                            if (!ret.result) throw new Exception(ret.text);
//                            if (Constants.Trace) Utility.ClassLog.WriteLog("GETLASCNTVAL: Шаг 12 (сохранить информацию о показаниях): " + sql);
//                            //-----------------------------------------------------------------------
//                            #endregion
//                        }

//                        reader.Close();
//                    }
//                    //-----------------------------------------------------------------------
//                    #endregion

//                    ExecSQL(conn_web, " Drop table tmp_show_cv_web ", false);
//                }

//                string _where_nzp_counter = "";

//                // если данные берутся на изменение
//                if (finder.prm == Constants.act_mode_edit.ToString())
//                {
//                    #region Шаг 13 снять в кэше признак блокировки с просматриваемых ПУ
//                    //--------------------------------------------------------------------------
//                    sql = " Update " + tXX_cv + " Set blocked = 0 Where show = 1 ";

//                    ret = ExecSQL(conn_web, sql, true);
//                    if (!ret.result) throw new Exception(ret.text);
//                    if (Constants.Trace) Utility.ClassLog.WriteLog("GETLASCNTVAL: Шаг 13 (снять признак блокировки с просматриваемых ПУ в кэше): " + sql);
//                    //--------------------------------------------------------------------------
//                    #endregion

//                    #region обновить информацию о блокировке приборов учета
//                    //------------------------------------------------------------------
//                    Finder userFinder = new Finder();
//                    DbWorkUser dbWorkUser = new DbWorkUser();

//                    #region Шаг 14 получить префиксы просматриваемых ПУ
//                    //--------------------------------------------------------------------------
//                    sql = "Select distinct pref From tmp_show_cv_db Order by 1";
//                    ret = ExecRead(conn_db, out reader, sql, true);
//                    if (!ret.result) throw new Exception(ret.text);
//                    if (Constants.Trace) Utility.ClassLog.WriteLog("GETLASCNTVAL: Шаг 14 (получить префиксы просматриваемых ПУ): " + sql);
//                    //--------------------------------------------------------------------------
//                    #endregion

//                    while (reader.Read())
//                    {
//                        // получить префикс банка данных и код локального пользователя из этого банка
//                        if (reader["pref"] != DBNull.Value) userFinder.pref = Convert.ToString(reader["pref"]).Trim();
//                        userFinder.nzp_user = finder.nzp_user;
//                        userFinder.nzp_user = dbWorkUser.GetLocalUser(conn_db, userFinder, out ret);

//                        #region Шаг 15 снять блокировку с приборов учета, заблокированных текущим пользователем в текущем банке данных
//                        //------------------------------------------------------------------
//#if PG
//                        sql = " Update " + userFinder.pref + "_data.counters_spis Set dat_block = null, user_block = null " +
//                                                " Where user_block is not null and user_block = " + userFinder.nzp_user;
//#else
//    sql = " Update " + userFinder.pref + "_data:counters_spis Set dat_block = null, user_block = null " +
//                            " Where user_block is not null and user_block = " + userFinder.nzp_user;
//#endif

//                        ret = ExecSQL(conn_db, sql, true);
//                        if (!ret.result) throw new Exception(ret.text);
//                        if (Constants.Trace) Utility.ClassLog.WriteLog("GETLASCNTVAL: Шаг 15 (снять блокировку с приборов учета, заблокированных текущим пользователем в текущем банке данных): " + sql);
//                        //------------------------------------------------------------------
//                        #endregion

//                        #region Шаг 16 сохранить во временную таблицу коды заблокированных ПУ, которые просматриваются
//                        //--------------------------------------------------------------------------
//                        // условие для выборки заблокированных ПУ
//#if PG
//                        _where_nzp_counter = " Select cs.nzp_counter " +
//                                            " From " + userFinder.pref + "_data.counters_spis cs " +
//                                            " Where cs.user_block is not null and cs.user_block <> " + userFinder.nzp_user +
//                                                " and (now() -  INTERVAL '20 minutes') < cs.dat_block ";
//#else
//        _where_nzp_counter = " Select cs.nzp_counter " +
//                            " From " + userFinder.pref + "_data:counters_spis cs " +
//                            " Where cs.user_block is not null and cs.user_block <> " + userFinder.nzp_user +
//                                " and (current year to second - 20 units minute) < cs.dat_block ";
//#endif

//                        // удалить временную таблицу, ошибки не обрабатывать
//                        ExecSQL(conn_db, " Drop table tmp_show_cv_blocked ", false);
//#if PG
//                        sql = " Select t.nzp_counter into unlogged tmp_show_cv_blocked From tmp_show_cv_db t " +
//                                                    " Where t.nzp_counter in (" + _where_nzp_counter + ")" +
//                                                    " and t.pref = " + Utils.EStrNull(userFinder.pref);
//#else
//                        sql = " Select t.nzp_counter From tmp_show_cv_db t " +
//                            " Where t.nzp_counter in (" + _where_nzp_counter + ")" +
//                            " and t.pref = " + Utils.EStrNull(userFinder.pref) +
//                            " into temp tmp_show_cv_blocked with no log ";
//#endif

//                        ret = ExecSQL(conn_db, sql, true);
//                        if (!ret.result) throw new Exception(ret.text);
//                        if (Constants.Trace) Utility.ClassLog.WriteLog("GETLASCNTVAL: Шаг 16 (сохранить во временную таблицу (НА СТОРОНЕ БАНКА) коды заблокированных ПУ, которые просматриваются): " + sql);
//                        //--------------------------------------------------------------------------
//                        #endregion

//                        #region Шаг 17 обновить в банке информацию о блокировке приборов учета
//                        //------------------------------------------------------------------
//                        // заблокировать в банке просматриваемые приборы учета, которые не заблокированы другими пользователями                        
//#if PG
//                        sql = " Update " + userFinder.pref + "_data.counters_spis " +
//                                                    " Set dat_block = now(), user_block = " + userFinder.nzp_user +
//                                                    " Where nzp_counter in (Select t.nzp_counter From tmp_show_cv_db t) " +
//                                                        " and nzp_counter not in (Select b.nzp_counter From tmp_show_cv_blocked b)";     // приборы учета, которые НЕ заблокированы
//#else
//sql = " Update " + userFinder.pref + "_data:counters_spis " +
//                            " Set dat_block = current year to second, user_block = " + userFinder.nzp_user +
//                            " Where nzp_counter in (Select t.nzp_counter From tmp_show_cv_db t) " +
//                                " and nzp_counter not in (Select b.nzp_counter From tmp_show_cv_blocked b)";     // приборы учета, которые НЕ заблокированы
//#endif
//                        ret = ExecSQL(conn_db, sql, true);
//                        if (!ret.result) throw new Exception(ret.text);
//                        if (Constants.Trace) Utility.ClassLog.WriteLog("GETLASCNTVAL: Шаг 17 (заблокировать просматриваемые приборы учета из кэша, которые не заблокированы другими пользователями): " + sql);
//                        //------------------------------------------------------------------
//                        #endregion

//                        #region Шаг 18 получить коды просматриваемых ПУ, которые заблокированы другим пользователем
//                        //--------------------------------------------------------------------------
//                        sql = " Select b.nzp_counter From tmp_show_cv_blocked b ";
//                        ret = ExecRead(conn_db, out reader2, sql, true);
//                        if (!ret.result) throw new Exception(ret.text);
//                        if (Constants.Trace) Utility.ClassLog.WriteLog("GETLASCNTVAL: Шаг 18 (получить коды просматриваемых ПУ, которые заблокированы другим пользователем): " + sql);
//                        //--------------------------------------------------------------------------
//                        #endregion

//                        #region Шаг 19 установить признак блокировки у ПУ в кэше, которые выводятся на просмотр и относятся к текущему банку
//                        //--------------------------------------------------------------------------
//                        while (reader2.Read())
//                        {
//                            if (reader2["nzp_counter"] != DBNull.Value)
//                            {
//                                sql = "Update " + tXX_cv + " Set blocked = 1 Where nzp_counter = " + Convert.ToInt32(reader2["nzp_counter"]) +
//                                    " and pref = " + Utils.EStrNull(userFinder.pref) +
//                                    " and show = 1";
//                                ret = ExecSQL(conn_web, sql, true);
//                                if (!ret.result) throw new Exception(ret.text);
//                                if (Constants.Trace) Utility.ClassLog.WriteLog("GETLASCNTVAL: Шаг 19 (установить признак блокировки у ПУ в кэше, которые выводятся на просмотр, заблокированы и относятся к текущему банку): " + sql);
//                            }
//                        }
//                        reader2.Close();
//                        //--------------------------------------------------------------------------
//                        #endregion
//                    }

//                    reader.Close();
//                    //------------------------------------------------------------------
//                    #endregion
//                }
//                else if (finder.prm == Constants.act_mode_view.ToString())
//                {
//                    Finder userFinder = new Finder();
//                    DbWorkUser dbWorkUser = new DbWorkUser();

//                    // получить префиксы из кэш-таблицы, чтобы получить доступ к нужным банкам данных
//                    sql = "Select distinct pref From " + tXX_cv;
//                    ret = ExecRead(conn_web, out reader, sql, true);
//                    if (!ret.result) throw new Exception(ret.text);

//                    while (reader.Read())
//                    {
//                        if (reader["pref"] != DBNull.Value) userFinder.pref = Convert.ToString(reader["pref"]).Trim();
//                        userFinder.nzp_user = finder.nzp_user;
//                        userFinder.nzp_user = dbWorkUser.GetLocalUser(conn_db, userFinder, out ret);

//                        // снять блокировку с приборов учета, заблокированных текущим пользователем в текущем банке данных
//                        sql = " Update " + userFinder.pref + "_data" + DBManager.tableDelimiter + "counters_spis Set dat_block = null, user_block = null " +
//                            " Where user_block is not null and user_block = " + userFinder.nzp_user;
//                        ret = ExecSQL(conn_db, sql, true);
//                        if (!ret.result) throw new Exception(ret.text);
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                MonitorLog.WriteLog("Ошибка GetLastCntVal : " + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
//                if (reader != null) reader.Close();

//                ExecSQL(conn_web, " Drop table tmp_show_cv_web ", false);
//                ExecSQL(conn_db, " Drop table tmp_show_cv_db ", false);
//                ExecSQL(conn_db, " Drop table tmp_show_cv_blocked ", false);

//                conn_web.Close();
//                conn_db.Close();
//                ret = new Returns(false, ex.Message);
//                return null;
//            }

//            #region получить список ПУ с показаниями
//            //------------------------------------------------------------------
//            List<CounterVal> Spis = new List<CounterVal>();
//#if PG
//            sql = " Select  * From " + tXX_cv + " Where 1=1 " + " Order by " + order + skip;
//#else
//            sql = " Select " + skip + " * From " + tXX_cv + " Where 1=1 " + " Order by " + order;
//#endif
//            int blocked;

//            try
//            {
//                if (Constants.Trace) Utility.ClassLog.WriteLog("GETLASCNTVAL: Шаг 20 старт (загрузка данных): " + sql);

//                ret = ExecRead(conn_web, out reader, sql, true);
//                if (!ret.result) throw new Exception(ret.text);

//                int i = 0;
//                while (reader.Read())
//                {
//                    i = i + 1;
//                    CounterVal zap = new CounterVal();
//                    zap.num = (i + finder.skip).ToString();
//                    zap.nzp_type = finder.nzp_type;

//                    if (finder.nzp_type == (int)CounterKinds.Kvar)
//                    {
//                        if (reader["nkvar"] != DBNull.Value) zap.nkvar = ((string)reader["nkvar"]).Trim();
//                        if (reader["num_ls"] != DBNull.Value) zap.num_ls = Convert.ToInt32(reader["num_ls"]);
//                        if (reader["smrlitera"] != DBNull.Value) zap.pkod = zap.num_ls + " " + Convert.ToString(reader["smrlitera"]).Trim();

//                        if (reader["fio"] != DBNull.Value) zap.fio = ((string)reader["fio"]).Trim();
//                        if (reader["comment"] != DBNull.Value) zap.comment = ((string)reader["comment"]).Trim();
//                        if (reader["dat_prov"] != DBNull.Value) zap.dat_prov = Convert.ToDateTime(reader["dat_prov"]).ToShortDateString();
//                        if (reader["dat_provnext"] != DBNull.Value) zap.dat_provnext = Convert.ToDateTime(reader["dat_provnext"]).ToShortDateString();
//                    }
//                    else if (finder.nzp_type == (int)CounterKinds.Dom)
//                    {
//                        if (reader["ngp_lift"] != DBNull.Value) zap.ngp_lift = Convert.ToDecimal(reader["ngp_lift"]);
//                        if (reader["sred_rashod"] != DBNull.Value) zap.sred_rashod = Convert.ToString(reader["sred_rashod"]).Trim();
//                    }

//                    // расход на нежилые помещения
//                    if ((finder.nzp_type == (int)CounterKinds.Dom) || (finder.nzp_type == (int)CounterKinds.Kvar && Points.IsIpuHasNgpCnt))
//                    {
//                        if (reader["ngp_cnt"] != DBNull.Value) zap.ngp_cnt = Convert.ToDecimal(reader["ngp_cnt"]);
//                    }

//                    if (reader["pref"] != DBNull.Value) zap.pref = (string)reader["pref"];
//                    if (reader["nzp_cv"] != DBNull.Value) zap.nzp_cv = Convert.ToInt32(reader["nzp_cv"]);
//                    if (reader["nzp_counter"] != DBNull.Value) zap.nzp_counter = Convert.ToInt32(reader["nzp_counter"]);
//                    if (reader["nzp_dom"] != DBNull.Value) zap.nzp_dom = Convert.ToInt32(reader["nzp_dom"]);
//                    if (reader["nzp_serv"] != DBNull.Value) zap.nzp_serv = Convert.ToInt32(reader["nzp_serv"]);

//                    if (reader["ulica"] != DBNull.Value) zap.ulica = ((string)reader["ulica"]).Trim();
//                    if (reader["ndom"] != DBNull.Value) zap.ndom = ((string)reader["ndom"]).Trim();
//                    if (reader["nkor"] != DBNull.Value) zap.nkor = ((string)reader["nkor"]).Trim();
//                    if (reader["nzp"] != DBNull.Value) zap.nzp = Convert.ToInt64(reader["nzp"]);
//                    if (reader["service"] != DBNull.Value) zap.service = ((string)reader["service"]).Trim();

//                    if (reader["val_cnt"] != DBNull.Value)
//                    {
//                        zap.val_cnt_s = Convert.ToString(reader["val_cnt"]).Trim();
//                        zap.val_cnt = Convert.ToDecimal(reader["val_cnt"]);
//                    }
//                    else zap.val_cnt_s = "";

//                    if (reader["dat_uchet"] != DBNull.Value) zap.dat_uchet = Convert.ToDateTime(reader["dat_uchet"]).ToShortDateString();
//                    if (reader["val_cnt_pred"] != DBNull.Value)
//                    {
//                        zap.val_cnt_pred_s = Convert.ToString(reader["val_cnt_pred"]);
//                        zap.val_cnt_pred = Convert.ToDecimal(reader["val_cnt_pred"]);
//                    }
//                    else zap.val_cnt_pred_s = "";

//                    if (reader["dat_uchet_pred"] != DBNull.Value) zap.dat_uchet_pred = Convert.ToDateTime(reader["dat_uchet_pred"]).ToShortDateString();
//                    if (reader["num_cnt"] != DBNull.Value) zap.num_cnt = ((string)reader["num_cnt"]).Trim();
//                    if (reader["name_type"] != DBNull.Value) zap.name_type = ((string)reader["name_type"]).Trim();
//                    if (reader["mmnog"] != DBNull.Value) zap.mmnog = Convert.ToDecimal(reader["mmnog"]);
//                    if (reader["cnt_stage"] != DBNull.Value) zap.cnt_stage = Convert.ToInt32(reader["cnt_stage"]);

//                    zap.smmnog = zap.cnt_stage.ToString() + " / " + zap.mmnog.ToString();

//                    if (finder.prm == Constants.act_mode_edit.ToString())
//                    {
//                        blocked = 0;
//                        if (reader["blocked"] != DBNull.Value) blocked = Convert.ToInt32(reader["blocked"]);
//                        if (blocked == 1) zap.block = "Прибор учета заблокирован";
//                    }

//                    if (zap.val_cnt_s.Length > 0)
//                    {
//                        zap.rashod_d = zap.calculatedRashod;
//                        zap.rashod = zap.rashod_d.ToString();
//                    }

//                    if (reader["user_"] != DBNull.Value) zap.dat_when = ((string)reader["user_"]).Trim();
//                    if (reader["dat_when"] != DBNull.Value) zap.dat_when += " (" + Convert.ToDateTime(reader["dat_when"]).ToShortDateString() + ")";

//                    Spis.Add(zap);

//                    if (i >= finder.rows) break;
//                }

//                #region определить что нужно показывать колонку расход на электроснабжение лифтов
//                //------------------------------------------------------------------
//                if (Spis.Count > 0 && finder.nzp_type == (int)CounterKinds.Dom)
//                {
//                    Spis[0].show_ngp_lift = ShowNgpLift(tXX_cv, conn_web, out ret);
//                    if (!ret.result) throw new Exception(ret.text);
//                }
//                //------------------------------------------------------------------
//                #endregion

//                if (finder.nzp_type == (int)CounterKinds.Kvar)
//                {
//                    ret = DefineMaxRashod(conn_db, finder, Spis);
//                    if (!ret.result) throw new Exception(ret.text);
//                }
//            }
//            catch (Exception ex)
//            {
//                MonitorLog.WriteLog("Ошибка GetLastCntVal: " + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
//                reader.Close();
//                conn_web.Close();
//                conn_db.Close();
//                ret = new Returns(false, ex.Message);
//                return null;
//            }
//            //------------------------------------------------------------------
//            #endregion

//            reader.Close();
//            conn_web.Close();
//            conn_db.Close();
//            ret.tag = total;

//            if (Constants.Trace) Utility.ClassLog.WriteLog("GETLASCNTVAL: ************************* КОНЕЦ *************************");


//            return Spis;
//        }

//        /// <summary>
//        /// Определить максимальные значения для ПУ (Первое приближение)
//        /// </summary>
//#warning функции, работающие с counters, counters_dom, counters_group через counters_vals. Не использовать
//        private Returns DefineMaxRashod(IDbConnection conn_db, CounterVal finder, List<CounterVal> Spis)
//        {
//            for (int i = 0; i < Spis.Count; i++)
//            {
//                if (Spis[i].pref.Trim() == "") Spis[i].pref = finder.pref;
//            }

//            List<string> pref_list = Spis.Select(x => x.pref).Distinct().ToList<string>();

//            Returns ret = new Returns(true, "", 0);
//            IDataReader reader;

//            double val_prm = 0;
//            Int32 nzp_serv = 0;

//            DateTime dd;

//            try
//            {
//                dd = Convert.ToDateTime(finder.dat_uchet);
//            }
//            catch
//            {
//                return new Returns(false, "Ошибка конвертации даты учета при определении величины максимальных расходов", -1);
//            }

//            for (int i = 0; i < pref_list.Count; i++)
//            {
//                string sql = " select val_prm, 9 as nzp_serv from " + pref_list[i] + "_data" + DBManager.tableDelimiter + "prm_10 p " +
//                    " where p.nzp_prm = 2082 " +
//                    "   and " + DBManager.MDY(dd.Month, 1, dd.Year) + " between p.dat_s and p.dat_po " +
//                    " union all " +
//                    " select val_prm, 6 as nzp_serv from " + pref_list[i] + "_data" + DBManager.tableDelimiter + "prm_10 p " +
//                    " where p.nzp_prm = 2083 " +
//                    "   and " + DBManager.MDY(dd.Month, 1, dd.Year) + " between p.dat_s and p.dat_po ";

//                ret = ExecRead(conn_db, out reader, sql, true);
//                if (!ret.result) throw new Exception(ret.text);

//                while (reader.Read())
//                {
//                    nzp_serv = Convert.ToInt32(reader["nzp_serv"]);

//                    try
//                    { val_prm = Convert.ToDouble(reader["val_prm"]); }
//                    catch
//                    { val_prm = 0; }

//                    if (val_prm <= 0) continue;

//                    for (int j = 0; j < Spis.Count; j++)
//                    {
//                        if (Spis[j].pref == pref_list[i] && Spis[j].nzp_serv == nzp_serv)
//                        {
//                            Spis[j].max_rashod = val_prm;
//                        }
//                    }

//                }

//            }

//            return ret;
//        }

//#warning функции, работающие с counters, counters_dom, counters_group через counters_vals. Не использовать
//        public List<CounterVal> GetPuData(Ls finder, out Returns ret)
//        {
//            ret = Utils.InitReturns();
//            if (finder.nzp_kvar <= 0)
//            {
//                ret.result = false;
//                ret.text = "Не указан номер ЛС";
//                ret.tag = -1;
//                return null;
//            }
//            //  DateTime firstDayNextMonth = new DateTime(Year, Month, 1).AddMonths(1);
//            List<CounterVal> CountersList = new List<CounterVal>();
//            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
//            IDataReader reader;

//            try
//            {
//                ret = OpenDb(conn_db, true);
//                if (!ret.result) throw new Exception(ret.text);

//                StringBuilder sql = new StringBuilder();
//                sql.Append(" drop table t_counter; ");
//                ExecSQL(conn_db, sql.ToString(), false);

//                sql.Remove(0, sql.Length);

//                #region 1. Сохранить во временную таблицу информацию о счетчиках
//                //-----------------------------------------------------------------------
//#if PG
//                sql.Append("Select " + Utils.EStrNull(finder.pref) + " pref, cs.nzp_counter, cs.nzp_serv, t.nzp_cnttype, " +
//                    " cs.nzp, cc.name as service, t.mmnog, t.cnt_stage, cs.num_cnt, t.name_type, cs.dat_prov, cs.dat_provnext, " +
//                    " (case when cs.user_block is not null and cs.user_block <> 1 " +
//                    " and cs.dat_block is not null and (now() - INTERVAL '20 minutes' MINUTE) < cs.dat_block then 1 " +
//                    " else 0 end) as blocked, cs.comment  into temp t_counter " +
//                    "From " + finder.pref + "_kernel.s_counts cc, " +
//                    finder.pref + "_kernel.s_counttypes t, " +
//                    finder.pref + "_kernel.services s, " +
//                    finder.pref + "_data.counters_spis cs " +
//                    " Where cs.nzp_cnttype = t.nzp_cnttype " +
//                    " and cs.nzp_serv = s.nzp_serv " +
//                    " and cs.nzp_serv = cc.nzp_serv " +
//                    " and cs.nzp_type = 3 " +
//                    " and cs.nzp = " + finder.nzp_kvar + " and cs.dat_close is null and cs.is_actual <> 100" +
//                    " order by service, name_type, num_cnt, nzp_counter");

//#else
//                sql.Append("Select " + Utils.EStrNull(finder.pref) + " pref, cs.nzp_counter, cs.nzp_serv, t.nzp_cnttype, " +
//                    " cs.nzp, cc.name as service, t.mmnog, t.cnt_stage, cs.num_cnt, t.name_type, cs.dat_prov, cs.dat_provnext, " +
//                    " (case when cs.user_block is not null and cs.user_block <> 1 " +
//                    " and cs.dat_block is not null and (current year to second - 20 units minute) < cs.dat_block then 1 " +
//                    " else 0 end) as blocked, cs.comment " +
//                    "From " + finder.pref + "_kernel:s_counts cc, " +
//                    finder.pref + "_kernel:s_counttypes t, " +
//                    finder.pref + "_kernel:services s, " + 
//                    finder.pref + "_data:counters_spis cs " +
//                    " Where cs.nzp_cnttype = t.nzp_cnttype " +
//                    " and cs.nzp_serv = s.nzp_serv " +
//                    " and cs.nzp_serv = cc.nzp_serv " +
//                    " and cs.nzp_type = 3 " +
//                    " and cs.nzp = " + finder.nzp_kvar + " and cs.dat_close is null and cs.is_actual <> 100" +
//                    " order by service, name_type, num_cnt, nzp_counter" +
//                    " into temp t_counter with no log");
//#endif
//                ret = ExecSQL(conn_db, sql.ToString(), true);
//                if (!ret.result) throw new Exception(ret.text);
//                //-----------------------------------------------------------------------
//                #endregion

//                #region 2. Подготовить данные c помощью функции PrepareCountersVals
//                //-----------------------------------------------------------------------
//                CounterVal newFinder = new CounterVal();
//                newFinder.nzp_type = (int)CounterKinds.Kvar;

//                // получить текущий расчетный месяц локального банка
//                CalcMonthParams cmp = new CalcMonthParams();
//                cmp.pref = finder.pref;
//                RecordMonth rm = Points.GetCalcMonth(cmp);
//                DateTime dat_uchet = new DateTime(rm.year_, rm.month_, 1);

//                // добавить месяц к дате учета (особенности функции PrepareCountersVals)
//                newFinder.dat_uchet = dat_uchet.AddMonths(1).ToShortDateString();
//                newFinder.ist = (int)CounterVal.Ist.Operator;
//                newFinder.month_ = rm.month_;
//                newFinder.year_ = rm.year_;
//                newFinder.nzp_user = finder.nzp_user;

//                newFinder.pref = finder.pref;
//                newFinder.nzp_kvar = finder.nzp_kvar;

//                // значение с потолка
//                newFinder.nzp_dom = 1000;

//                ret = PrepareCountersVals(newFinder, PrepareCounterValMode.SpisVal);
//                if (!ret.result) throw new Exception(ret.text);
//                //-----------------------------------------------------------------------
//                #endregion

//                #region 3. Получить информацию о счетчиках и показаниях
//                //-----------------------------------------------------------------------
//                string counters_vals = "";
//                sql.Remove(0, sql.Length);

//#if PG
//                counters_vals = finder.pref + "_charge_" + (rm.year_ % 100).ToString("00") + ".counters_vals";

//                sql.Append(" Select distinct t.pref:: TEXT as pref, " +
//                            " t.nzp_counter, t.nzp_serv, t.nzp_cnttype, t.nzp, t.service, t.mmnog, " +
//                            "t.cnt_stage, t.num_cnt, t.name_type, t.dat_prov, t.dat_provnext, t.blocked, t.comment, " +
//                            "cv.nzp_cv, cv.val_cnt, cv.dat_uchet, a.val_cnt as val_cnt_pred, a.dat_uchet as dat_uchet_pred, " +
//                    // пользователь, которые изменил данные
//                    " uu.comment as user_, cv.dat_when " +
//                    " From t_counter t, " + counters_vals + " cv " +
//                        " left outer join " + counters_vals + " a on A .nzp_counter = cv.nzp_counter " +
//                        " left outer join " + finder.pref + "_data.users uu on uu.nzp_user = cv.nzp_user " +
//                    " Where t.nzp_counter = cv.nzp_counter" +
//                        " and cv.dat_uchet between " + Utils.EStrNull(dat_uchet.AddDays(1).ToShortDateString()) + " and " + Utils.EStrNull(dat_uchet.AddMonths(1).ToShortDateString()) +
//                        " and a.dat_uchet = (select max(b.dat_uchet) from " + counters_vals + " b where b.nzp_counter = a.nzp_counter " +
//                        " and b.dat_uchet <= " + Utils.EStrNull(dat_uchet.ToShortDateString()) +
//                        " and b.month_ = " + rm.month_ + ")" +
//                    // источник данных - оператор
//                        " and cv.ist = " + (int)CounterVal.Ist.Operator +
//                        " and cv.month_ = " + rm.month_ +
//                        " and a.month_ = " + rm.month_ +
//                        " order by t.service, t.name_type, t.num_cnt, t.nzp_counter");

//#else
//                counters_vals = finder.pref + "_charge_" + (rm.year_ % 100).ToString("00") + ":counters_vals";

//                sql.Append(" Select distinct t.*, cv.nzp_cv, cv.val_cnt, cv.dat_uchet, a.val_cnt as val_cnt_pred, a.dat_uchet as dat_uchet_pred, " +
//                    // пользователь, которые изменил данные
//                    " uu.comment as user_, cv.dat_when " +
//                    " From t_counter t, " + counters_vals + " cv, " +
//                        " outer " + finder.pref + "_data:users uu,  " +
//                        " outer " + counters_vals + " a " +
//                    " Where t.nzp_counter = cv.nzp_counter" +
//                        " and cv.dat_uchet between " + Utils.EStrNull(dat_uchet.AddDays(1).ToShortDateString()) + " and " + Utils.EStrNull(dat_uchet.AddMonths(1).ToShortDateString()) +
//                        " and uu.nzp_user = cv.nzp_user " +
//                        " and a.nzp_counter = cv.nzp_counter " +
//                        " and a.dat_uchet = (select max(b.dat_uchet) from " + counters_vals + " b where b.nzp_counter = a.nzp_counter " +
//                        " and b.dat_uchet <= " + Utils.EStrNull(dat_uchet.ToShortDateString()) +
//                        " and b.month_ = " + rm.month_ + ")" +
//                    // источник данных - оператор
//                        " and cv.ist = " + (int)CounterVal.Ist.Operator +
//                        " and cv.month_ = " + rm.month_ +
//                        " and a.month_ = " + rm.month_ + 
//                        " order by t.service, t.name_type, t.num_cnt, t.nzp_counter");
//#endif

//                ret = ExecRead(conn_db, out reader, sql.ToString(), true);
//                if (!ret.result) throw new Exception(ret.text);

//                bool found = false;

//                while (reader.Read())
//                {
//                    CounterVal tmpVal = new CounterVal();

//                    // получить счетчик и показание
//                    if (reader["nzp_counter"] != DBNull.Value) tmpVal.nzp_counter = Convert.ToInt32(reader["nzp_counter"]);
//                    if (reader["val_cnt"] != DBNull.Value)
//                    {
//                        tmpVal.val_cnt_s = Convert.ToString(reader["val_cnt"]).Trim();
//                        tmpVal.val_cnt = Convert.ToDecimal(reader["val_cnt"]);
//                    }
//                    else tmpVal.val_cnt_s = "";

//                    // поискать счетчик в списке
//                    found = false;
//                    for (int i = 0; i < CountersList.Count; i++)
//                    {
//                        if (tmpVal.nzp_counter == CountersList[i].nzp_counter)
//                        {
//                            found = true;
//                            break;
//                        }
//                    }

//                    // если счетчик найден и показание по счетчику не введено, то пропусить это показание и в список не добавлять
//                    if (found && tmpVal.ngp_cnt_s == "") continue;

//                    if (reader["dat_prov"] != DBNull.Value) tmpVal.dat_prov = Convert.ToDateTime(reader["dat_prov"]).ToShortDateString();
//                    if (reader["dat_provnext"] != DBNull.Value) tmpVal.dat_provnext = Convert.ToDateTime(reader["dat_provnext"]).ToShortDateString();

//                    if (reader["nzp_cv"] != DBNull.Value) tmpVal.nzp_cv = Convert.ToInt32(reader["nzp_cv"]);
//                    if (reader["nzp_serv"] != DBNull.Value) tmpVal.nzp_serv = Convert.ToInt32(reader["nzp_serv"]);
//                    if (reader["nzp_cnttype"] != DBNull.Value) tmpVal.nzp_cnttype = Convert.ToInt32(reader["nzp_cnttype"]);

//                    if (reader["service"] != DBNull.Value) tmpVal.service = ((string)reader["service"]).Trim();

//                    if (reader["dat_uchet"] != DBNull.Value) tmpVal.dat_uchet = Convert.ToDateTime(reader["dat_uchet"]).ToShortDateString();

//                    if (reader["dat_uchet_pred"] != DBNull.Value) tmpVal.dat_uchet_pred = Convert.ToDateTime(reader["dat_uchet_pred"]).ToShortDateString();
//                    if (reader["val_cnt_pred"] != DBNull.Value)
//                    {
//                        tmpVal.val_cnt_pred_s = Convert.ToString(reader["val_cnt_pred"]);
//                        tmpVal.val_cnt_pred = Convert.ToDecimal(reader["val_cnt_pred"]);
//                    }
//                    else tmpVal.val_cnt_pred_s = "";

//                    if (reader["num_cnt"] != DBNull.Value) tmpVal.num_cnt = ((string)reader["num_cnt"]).Trim();
//                    if (reader["name_type"] != DBNull.Value) tmpVal.name_type = ((string)reader["name_type"]).Trim();
//                    if (reader["pref"] != DBNull.Value) tmpVal.pref = ((string)reader["pref"]).Trim();
//                    if (reader["mmnog"] != DBNull.Value) tmpVal.mmnog = Convert.ToDecimal(reader["mmnog"]);
//                    if (reader["cnt_stage"] != DBNull.Value) tmpVal.cnt_stage = Convert.ToInt32(reader["cnt_stage"]);

//                    tmpVal.smmnog = tmpVal.cnt_stage.ToString() + " / " + tmpVal.mmnog.ToString();

//                    /*if (finder.prm == Constants.act_mode_edit.ToString())
//                    {
//                        blocked = 0;
//                        if (reader["blocked"] != DBNull.Value) blocked = Convert.ToInt32(reader["blocked"]);
//                        if (blocked == 1) zap.block = "Прибор учета заблокирован";
//                    }*/

//                    if (tmpVal.val_cnt_s.Length > 0)
//                    {
//                        tmpVal.rashod_d = tmpVal.calculatedRashod;
//                        tmpVal.rashod = tmpVal.rashod_d.ToString();
//                    }

//                    if (reader["user_"] != DBNull.Value) tmpVal.dat_when = ((string)reader["user_"]).Trim();
//                    if (reader["dat_when"] != DBNull.Value) tmpVal.dat_when += " (" + Convert.ToDateTime(reader["dat_when"]).ToShortDateString() + ")";
//                    // примечание к счетчику
//                    if (reader["comment"] != DBNull.Value) tmpVal.comment = ((string)reader["comment"]).Trim();

//                    CountersList.Add(tmpVal);
//                }
//                //-----------------------------------------------------------------------
//                #endregion

//                CounterVal cv = new CounterVal();
//                cv.dat_uchet = new DateTime(rm.year_, rm.month_, 1).ToShortDateString();

//                ret = DefineMaxRashod(conn_db, cv, CountersList);
//                if (!ret.result) throw new Exception(ret.text);

//                return CountersList;
//            }
//            catch
//            {
//                conn_db.Close();
//                return null;
//            }
//        }

//        /// <summary>
//        /// Получить итоги для ОДПУ
//        /// </summary>
//        /// <param name="finder"></param>
//        /// <param name="ret"></param>
//        /// <returns></returns>
//#warning функции, работающие с counters, counters_dom, counters_group через counters_vals. Не использовать
//        public List<CounterVal> GetOdpuRashod(CounterVal finder, out Returns ret)
//        {
//            if (Constants.Trace) Utility.ClassLog.WriteLog("Старт функции GetOdpuRashod");
//            IDbConnection conn_web = null;
//            IDbConnection conn_db = null;
//            List<string> prefList = null;
//            List<_Service> serviceList = null;
//            List<CounterVal> valList = null;
//            IDataReader reader;
//            try
//            {
//                #region проверка значений
//                //------------------------------------------------------------------
//                ret = Utils.InitReturns();
//                if (finder.nzp_user < 1)
//                {
//                    ret = new Returns(false, "Не определен пользователь", -1);
//                    return null;
//                }

//                // проверка месяца
//                if (finder.month_ <= 0)
//                {
//                    ret = new Returns(false, "Не определен месяц", -3);
//                    return null;
//                }

//                // проверка месяца
//                if (finder.year_ <= 0)
//                {
//                    ret = new Returns(false, "Не определен год", -4);
//                    return null;
//                }

//                // проверка даты учета
//                if (finder.dat_uchet.Trim().Length <= 0)
//                {
//                    ret = new Returns(false, "Не определена дата учета", -5);
//                    return null;
//                }
//                //------------------------------------------------------------------
//                #endregion

//                #region подключение к web + проверка, что данные были выбраны
//                //------------------------------------------------------------------
//                conn_web = GetConnection(Constants.cons_Webdata);
//                ret = OpenDb(conn_web, true);
//                if (!ret.result) return null;

//                string tXX_cv = "t" + Convert.ToString(finder.nzp_user) + "_dom_cv";

//                if (!TableInWebCashe(conn_web, tXX_cv))
//                {
//                    conn_web.Close();
//                    ret = new Returns(false, "Данные не были выбраны", -22);
//                    return null;
//                }
//                //------------------------------------------------------------------
//                #endregion

//                #region подключиться к банку
//                //--------------------------------------------------------------------            
//                string connectionString = Points.GetConnByPref(finder.pref);
//                conn_db = GetConnection(connectionString);
//                ret = OpenDb(conn_db, true);

//                if (!ret.result)
//                {
//                    conn_web.Close();
//                    return null;
//                }
//                //--------------------------------------------------------------------            
//                #endregion

//                string sql = "";


//                _Service service;
//                serviceList = new List<_Service>();

//                string pref = "";
//                prefList = new List<string>();

//                valList = new List<CounterVal>();
//                CounterVal val;

//                #region получить из кэша отсортированный список услуг
//                //---------------------------------------------------------------------------------------------------

//                sql = "Select distinct nzp_serv, service From " + tXX_cv + " Order by 2";
//                ret = ExecRead(conn_web, out reader, sql, true);
//                if (!ret.result) throw new Exception(ret.text);

//                while (reader.Read())
//                {
//                    service = new _Service();
//                    if (reader["nzp_serv"] != DBNull.Value) service.nzp_serv = Convert.ToInt32(reader["nzp_serv"]);
//                    if (reader["service"] != DBNull.Value) service.service = Convert.ToString(reader["service"]).Trim();
//                    serviceList.Add(service);
//                }
//                reader.Close();
//                //---------------------------------------------------------------------------------------------------
//                #endregion

//                #region получить из кэша список префиксов
//                //---------------------------------------------------------------------------------------------------
//                sql = "Select distinct pref From " + tXX_cv + " Order by 1";

//                ret = ExecRead(conn_web, out reader, sql, true);
//                if (!ret.result) throw new Exception(ret.text);

//                while (reader.Read())
//                {
//                    if (reader["pref"] != DBNull.Value) pref = Convert.ToString(reader["pref"]).Trim();
//                    prefList.Add(pref);
//                }
//                reader.Close();
//                //---------------------------------------------------------------------------------------------------
//                #endregion

//                DateTime curMonth = Convert.ToDateTime(finder.dat_uchet);

//                #region сформировать условие
//                //----------------------------------------------------------------------------            
//#if PG
//                string _where = " and coalesce(cs.dat_close, public.mdy(1,1,3000)) >= " + "'" + curMonth.AddMonths(1).ToShortDateString() + "'";
//#else
//                string _where = " and nvl(cs.dat_close, mdy(1,1,3000)) >= " + "'" + curMonth.AddMonths(1).ToShortDateString() + "'";
//#endif
//                // улица
//                if (finder.nzp_ul > 0) _where += " and d.nzp_ul = " + finder.nzp_ul.ToString();
//                // дом
//                if (finder.nzp_dom > 0) _where += " and k.nzp_dom = " + finder.nzp_dom.ToString();
//                // квартира
//                if (finder.nzp_kvar > 0) _where += " and k.nzp_kvar = " + finder.nzp_kvar;
//                // территория
//                if (finder.nzp_area > 0) _where += " and k.nzp_area = " + finder.nzp_area;
//                // роли
//                if (finder.RolesVal != null)
//                {
//                    foreach (_RolesVal role in finder.RolesVal)
//                        if (role.tip == Constants.role_sql)
//                            switch (role.kod)
//                            {
//                                case Constants.role_sql_area:
//                                    _where += " and k.nzp_area in (" + role.val + ")";
//                                    break;
//                                case Constants.role_sql_geu:
//                                    _where += " and k.nzp_geu in (" + role.val + ")";
//                                    break;
//                            }
//                }
//                // только открытые приборы учета
//                _where += " and cs.dat_close is null";
//                // открытые ПУ
//                _where += " and cs.is_actual <> 100 ";
//                //----------------------------------------------------------------------------
//                #endregion

//                string _from_group = "";
//                string _where_group = "";

//                #region создать временную таблицу
//                //---------------------------------------------------------------------------------------------------
//                // удалить временную таблицу, ошибки не обрабатывать
//                ExecSQL(conn_db, " Drop table tmp_rashod_dom ", false);
//#if PG
//                sql = "Create unlogged table tmp_rashod_dom" +
//                                    "( val_cnt       float, " +
//                                    "  ngp_cnt       numeric(14,7), " +
//                                    "  ngp_lift      numeric(14,7), " +
//                                    "  val_cnt_pred  float, " +
//                                    "  sred_rashod   float, " +
//                                    "  cnt_stage     integer, " +
//                                    "  mmnog         numeric(14,7), " +
//                                    "  dat_uchet     date, " +
//                                    "  dat_uchet_pred date ";
//#else
//sql = "Create temp table tmp_rashod_dom" +
//                    "( val_cnt       float, " +
//                    "  ngp_cnt       decimal(14,7), " +
//                    "  ngp_lift      decimal(14,7), " +
//                    "  val_cnt_pred  float, " +
//                    "  sred_rashod   float, " +
//                    "  cnt_stage     integer, " +
//                    "  mmnog         decimal(14,7), " +
//                    "  dat_uchet     date, " +
//                    "  dat_uchet_pred date " +
//                    ") With no log ";
//#endif
//                ret = ExecSQL(conn_db, sql, true);
//                if (!ret.result) throw new Exception(ret.text);
//                //---------------------------------------------------------------------------------------------------
//                #endregion

//                decimal sred_rashod;

//                foreach (_Service serv in serviceList)
//                {
//                    sql = "delete From tmp_rashod_dom";
//                    ret = ExecSQL(conn_db, sql, true);
//                    if (!ret.result) throw new Exception(ret.text);

//                    sred_rashod = 0;

//                    foreach (string cur_pref in prefList)
//                    {
//                        ExecSQL(conn_db, " Drop table tmp_insert_cv ", false);

//                        #region сформировать условие для группы
//                        //----------------------------------------------------------------------------
//                        _from_group = "";
//                        if (finder.group_pref.Length > 0 && finder.nzp_group > 0)
//                        {
//                            if (finder.group_pref == cur_pref)
//                            {
//#if PG
//                                _from_group = cur_pref + "_data.link_group l ";
//                                _where_group = " and k.nzp_kvar = l.nzp and l.nzp_group = " + finder.nzp_group;
//#else
//                                 _from_group = cur_pref + "_data:link_group l ";
//                                _where_group = " and k.nzp_kvar = l.nzp and l.nzp_group = " + finder.nzp_group;
//#endif
//                            }
//                        }
//                        else
//                        {
//                            if (finder.ngroup.Length > 0)
//                            {
//#if PG
//                                _from_group = cur_pref + "_data.link_group l, " + cur_pref + "_data.s_group sg ";
//                                _where_group = " and k.nzp_kvar = l.nzp and l.nzp_group = sg.nzp_group and upper(trim(sg.ngroup)) = '" + finder.ngroup.Trim().ToUpper() + "'";
//#else
//                                _from_group = cur_pref + "_data:link_group l, " + cur_pref + "_data:s_group sg ";
//                                _where_group = " and k.nzp_kvar = l.nzp and l.nzp_group = sg.nzp_group and upper(trim(sg.ngroup)) = '" + finder.ngroup.Trim().ToUpper() + "'";
//#endif
//                            }
//                        }
//                        if (_from_group.Length > 0) _from_group = _from_group + ", ";
//                        //----------------------------------------------------------------------------
//                        #endregion

//                        #region cформировать запрос
//                        //---------------------------------------------------------------------------------------------------------
//#if PG
//                        sql = " Select cs.nzp_counter, v.ngp_cnt, v.ngp_lift, t.cnt_stage, t.mmnog, " +
//                                                    " p.val_cnt as val_cnt_pred, p.dat_uchet as dat_uchet_pred, " +
//                                                    " v.val_cnt, max(v.dat_uchet) as dat_uchet into unlogged tmp_insert_cv " +
//                                                "   From " + cur_pref + "_kernel.s_counttypes t, " +
//                                                            cur_pref + "_data. dom d, " +
//                                                            cur_pref + "_data.kvar k, " +
//                                                            cur_pref + "_data.counters_spis cs, " +
//                                                            cur_pref + "_data.counters_dom v, " +
//                                                            "outer " + cur_pref + "_data.counters_dom p " + _from_group +
//                                                " Where cs.nzp_cnttype = t.nzp_cnttype " +
//                                                    " and d.nzp_dom = cs.nzp " +
//                                                    " and d.nzp_dom = k.nzp_dom " +
//                                                    " and cs.nzp_counter = v.nzp_counter " +
//                                                    " and cs.nzp_counter = p.nzp_counter " +
//                                                    " and cs.nzp_type = 1 " +
//                                                    " and cs.nzp_serv = " + serv.nzp_serv +
//                                                    " and v.dat_uchet >= " + Utils.EStrNull(curMonth.AddDays(1).ToShortDateString()) + " and v.dat_uchet <= " + Utils.EStrNull(curMonth.AddMonths(1).ToShortDateString()) +
//                                                    " and v.val_cnt is not null " +
//                                                    " and v.is_actual <> 100 " +
//                                                    " and p.dat_uchet = (select max(a.dat_uchet) from " + cur_pref + "_data.counters_dom a where a.nzp_counter = p.nzp_counter " +
//                                                                                                                                           " and a.dat_uchet <= " + Utils.EStrNull(curMonth.ToShortDateString()) +
//                                                                                                                                           " and a.is_actual <> 100 and a.val_cnt is not null) " +
//                                                    " and p.is_actual <> 100 " +
//                                                    " and p.val_cnt is not null " +
//                                                    _where + _where_group +
//                                                    " group by 1,2,3,4,5,6,7,8 ";
//#else
//    sql = " Select cs.nzp_counter, v.ngp_cnt, v.ngp_lift, t.cnt_stage, t.mmnog, " +
//                                " p.val_cnt as val_cnt_pred, p.dat_uchet as dat_uchet_pred, " +
//                                " v.val_cnt, max(v.dat_uchet) as dat_uchet " +
//                            "  From " + cur_pref + "_kernel:s_counttypes t, " +
//                                        cur_pref + "_data: dom d, " +
//                                        cur_pref + "_data:kvar k, " +
//                                        cur_pref + "_data:counters_spis cs, " +
//                                        cur_pref + "_data:counters_dom v, " +
//                                        "outer " + cur_pref + "_data:counters_dom p " + _from_group +
//                            " Where cs.nzp_cnttype = t.nzp_cnttype " +
//                                " and d.nzp_dom = cs.nzp " +
//                                " and d.nzp_dom = k.nzp_dom " +
//                                " and cs.nzp_counter = v.nzp_counter " +
//                                " and cs.nzp_counter = p.nzp_counter " +
//                                " and cs.nzp_type = 1 " +
//                                " and cs.nzp_serv = " + serv.nzp_serv +
//                                " and v.dat_uchet >= " + Utils.EStrNull(curMonth.AddDays(1).ToShortDateString()) + " and v.dat_uchet <= " + Utils.EStrNull(curMonth.AddMonths(1).ToShortDateString()) +
//                                " and v.val_cnt is not null " +
//                                " and v.is_actual <> 100 " +
//                                " and p.dat_uchet = (select max(a.dat_uchet) from " + cur_pref + "_data:counters_dom a where a.nzp_counter = p.nzp_counter " +
//                                                                                                                       " and a.dat_uchet <= " + Utils.EStrNull(curMonth.ToShortDateString()) +
//                                                                                                                       " and a.is_actual <> 100 and a.val_cnt is not null) " +
//                                " and p.is_actual <> 100 " +
//                                " and p.val_cnt is not null " +
//                                _where + _where_group +
//                                " group by 1,2,3,4,5,6,7,8 into temp tmp_insert_cv with no log ";
//#endif

//                        ret = ExecSQL(conn_db, sql, true);
//                        if (!ret.result) throw new Exception(ret.text);
//                        //---------------------------------------------------------------------------------------------------------
//                        #endregion

//                        #region вставить данные из одной временной таблицы в другую временную таблицу
//                        //---------------------------------------------------------------------------------------------------
//                        sql = "Insert into tmp_rashod_dom (val_cnt, ngp_cnt, ngp_lift, val_cnt_pred, cnt_stage, mmnog, dat_uchet, dat_uchet_pred) " +
//                            " Select val_cnt, ngp_cnt, ngp_lift, val_cnt_pred, cnt_stage, mmnog, dat_uchet, dat_uchet_pred From tmp_insert_cv";

//                        ret = ExecSQL(conn_db, sql, true);
//                        if (!ret.result) throw new Exception(ret.text);
//                        //---------------------------------------------------------------------------------------------------
//                        #endregion

//                        #region получить средний расход
//                        //---------------------------------------------------------------------------------------------------
//                        ExecSQL(conn_db, " Drop table tmp_sred_rashod ", false);
//#if PG
//                        sql = " Select distinct cs.nzp_counter, CAST(p17.val_prm as float) as sred_rashod " +
//                            "into unlogged tmp_sred_rashod " +
//                            "  From " + cur_pref + "_data. dom d, " +
//                                        cur_pref + "_data.kvar k, " +
//                                        cur_pref + "_data.counters_spis cs, " +
//                                        cur_pref + "_data.prm_17 p17 " + _from_group +
//                            " Where d.nzp_dom = cs.nzp " +
//                                " and d.nzp_dom = k.nzp_dom " +
//                                " and p17.nzp = cs.nzp_counter " +
//                                " and cs.nzp_type = 1 " +
//                                " and cs.nzp_serv = " + serv.nzp_serv +
//                                " and p17.nzp_prm = 979 " +
//                                " and p17.dat_s >= " + Utils.EStrNull(curMonth.ToShortDateString()) +
//                                " and p17.dat_po <= " + Utils.EStrNull(curMonth.AddMonths(1).AddDays(-1).ToShortDateString()) +
//                                " and p17.is_actual = 1 " +
//                                _where + _where_group;
//#else
//                        sql = " Select distinct cs.nzp_counter, CAST(p17.val_prm as float) as sred_rashod " +
//                            "  From " + cur_pref + "_data: dom d, " +
//                                        cur_pref + "_data:kvar k, " +
//                                        cur_pref + "_data:counters_spis cs, " +
//                                        cur_pref + "_data:prm_17 p17 " + _from_group +
//                            " Where d.nzp_dom = cs.nzp " +
//                                " and d.nzp_dom = k.nzp_dom " +
//                                " and p17.nzp = cs.nzp_counter " +
//                                " and cs.nzp_type = 1 " +
//                                " and cs.nzp_serv = " + serv.nzp_serv +
//                                " and p17.nzp_prm = 979 " +
//                                " and p17.dat_s >= " + Utils.EStrNull(curMonth.ToShortDateString()) +
//                                " and p17.dat_po <= " + Utils.EStrNull(curMonth.AddMonths(1).AddDays(-1).ToShortDateString()) +
//                                " and p17.is_actual = 1 " +
//                                _where + _where_group + " into temp tmp_sred_rashod with no log ";
//#endif
//                        ret = ExecSQL(conn_db, sql, true);
//                        if (!ret.result) throw new Exception(ret.text);

//                        sql = " Select sum(sred_rashod) From tmp_sred_rashod ";
//                        object sums = ExecScalar(conn_db, sql, out ret, true);
//                        if (!ret.result) throw new Exception(ret.text);

//                        try
//                        {
//                            if (sums != DBNull.Value) sred_rashod += Convert.ToDecimal(sums);
//                            else sred_rashod += 0;
//                        }
//                        catch (Exception ex)
//                        {
//                            throw new Exception(ex.Message);
//                        }
//                        //---------------------------------------------------------------------------------------------------
//                        #endregion
//                    }

//                    #region подсчитать итоги
//                    //---------------------------------------------------------------------------------------------------
//#if PG
//                    sql = "select sum(case when cv.val_cnt >= cv.val_cnt_pred then (cv.val_cnt - cv.val_cnt_pred) * cv.mmnog - cv.ngp_cnt - cv.ngp_lift " +
//                                            " else (pow(10, cv.cnt_stage) + cv.val_cnt - cv.val_cnt_pred) * cv.mmnog - cv.ngp_cnt - cv.ngp_lift end) as rashod, " +
//                                          " sum(cv.ngp_cnt) as ngp_cnt, sum(cv.ngp_lift) as ngp_lift " +
//                                          " from tmp_rashod_dom cv";
//#else
//                    sql = "select sum(case when cv.val_cnt >= cv.val_cnt_pred then (cv.val_cnt - cv.val_cnt_pred) * cv.mmnog - cv.ngp_cnt - cv.ngp_lift " +
//                            " else (pow(10, cv.cnt_stage) + cv.val_cnt - cv.val_cnt_pred) * cv.mmnog - cv.ngp_cnt - cv.ngp_lift end) as rashod, " +
//                          " sum(cv.ngp_cnt) as ngp_cnt, sum(cv.ngp_lift) as ngp_lift " +
//                          " from tmp_rashod_dom cv";
//#endif
//                    ret = ExecRead(conn_db, out reader, sql, true);
//                    if (!ret.result) throw new Exception(ret.text);

//                    if (reader.Read())
//                    {
//                        val = new CounterVal();
//                        val.service = serv.service;

//                        if (reader["rashod"] != DBNull.Value) val.rashod = Convert.ToString(reader["rashod"]);
//                        else val.rashod = "0";

//                        if (reader["ngp_cnt"] != DBNull.Value) val.ngp_cnt_s = Convert.ToString(reader["ngp_cnt"]).Trim();
//                        else val.ngp_cnt_s = "0";

//                        if (reader["ngp_lift"] != DBNull.Value) val.ngp_lift_s = Convert.ToString(reader["ngp_lift"]).Trim();
//                        else val.ngp_lift_s = "0";

//                        // средний расход
//                        val.sred_rashod = sred_rashod.ToString();

//                        val.num = "итого";
//                        val.nzp_type = (int)CounterKinds.Dom;

//                        valList.Add(val);
//                    }
//                    //---------------------------------------------------------------------------------------------------
//                    #endregion
//                }

//                #region определить что нужно показывать колонку расход на электроснабжение лифтов
//                //------------------------------------------------------------------
//                if (valList.Count > 0 && finder.nzp_type == (int)CounterKinds.Dom)
//                {
//                    valList[0].ndom = "ИТОГО";
//                    if (!ret.result) throw new Exception(ret.text);
//                }
//                //------------------------------------------------------------------
//                #endregion
//                if (Constants.Trace) Utility.ClassLog.WriteLog("Финиш функции GetOdpuRashod");
//            }
//            catch (Exception ex)
//            {
//                ret = Utils.InitReturns();
//                ret.result = false;
//                ret.text = ex.Message;

//                conn_db.Close();
//                conn_web.Close();
//                prefList.Clear();
//                serviceList.Clear();
//                return null;
//            }

//            conn_db.Close();
//            conn_web.Close();
//            reader.Close();
//            prefList.Clear();
//            serviceList.Clear();

//            return valList;
//        }

//        /// <summary> Загрузить показания ПУ для просмотра
//        /// </summary>
//        /// <param name="finder"></param>
//        /// <param name="ret"></param>
//        /// <returns></returns>
//#warning функции, работающие с counters, counters_dom, counters_group через counters_vals. Не использовать
//        public List<Counter> FindVal(Counter finder, out Returns ret) //найти и заполнить список показаний ПУ
//        {
//            ret = Utils.InitReturns();
//            if (finder.nzp_user < 1)
//            {
//                ret.result = false;
//                ret.text = "Не определен пользователь";
//                return null;
//            }

//            if (!((finder.nzp_type == (int)CounterKinds.Dom && finder.nzp_dom > 0) ||
//                (finder.nzp_type == (int)CounterKinds.Kvar && finder.nzp_kvar > 0) ||
//                (finder.nzp_type == (int)CounterKinds.Group && (finder.nzp_kvar > 0 || finder.nzp_dom > 0)) ||
//                (finder.nzp_type == (int)CounterKinds.Communal && finder.nzp_dom > 0)
//                ))
//            {
//                ret.result = false;
//                ret.text = "Неверные входные параметры";
//                return null;
//            }

//            List<Counter> Spis = new List<Counter>();

//            string cur_pref = finder.pref;

//            #region Соединение с БД
//            string connectionString = Points.GetConnByPref(finder.pref);
//            IDbConnection conn_db = GetConnection(connectionString);
//            ret = OpenDb(conn_db, true);
//            if (!ret.result) return Spis;
//            #endregion

//            #region определение локального пользователя
//            DbWorkUser db = new DbWorkUser();
//            int nzpUser = db.GetLocalUser(conn_db, finder, out ret);
//            db.Close();
//            if (!ret.result)
//            {
//                conn_db.Close();
//                return null;
//            }
//            #endregion


//#if PG
//            StringBuilder sql = new StringBuilder();
//            sql.Append(" Select ");
//            sql.Append(" (case when a.is_actual <> 100 then 0 else 1 end) as is_archive, a.is_actual, a.val_cnt, a.dat_uchet, a.dat_when, a.nzp_user, u1.comment as user_name ");
//            sql.Append(" , b.nzp_counter, s.service, b.nzp_serv, b.num_cnt, b.nzp_cnttype, b.dat_prov, b.dat_provnext, b.dat_oblom, b.dat_poch, b.dat_close,  b.dat_block, b.user_block, ");
//            sql.Append("u.comment as user_name_block, (now() - interval' " + Constants.users_min.ToString() + " minutes') as cur_dat ");
//            sql.Append(" , sc.cnt_stage, sc.mmnog, sc.name_type ");

//            switch (finder.nzp_type)
//            {
//                case (int)CounterKinds.Kvar: // показания квартирных ПУ
//                    sql.Append(", a.num_ls");
//                    sql.Append(", a.nzp_cr as nzp_cv");
//                    sql.Append(" From ");
//                    sql.Append(cur_pref + "_data.counters_spis b ");
//                    sql.Append(" left outer join " + cur_pref + "_data.counters a on  b.nzp_counter = a.nzp_counter ");
//                    // is_actual = 0 - не показывать архивные, > 0 - показывать и архивные и неархивные
//                    // dat_close = "" - показывать только открытые, иначе - и открытые и закрытые
//                    if (finder.dat_uchet != "") sql.Append(" and a.dat_uchet >= " + Utils.EStrNull(finder.dat_uchet));
//                    if (finder.dat_uchet_po != "") sql.Append(" and a.dat_uchet <= " + Utils.EStrNull(finder.dat_uchet_po));
//                    if (finder.date_begin != "") sql.Append(" and a.dat_uchet >= " + Utils.EStrNull(finder.date_begin));
//                    if (finder.is_actual == 0) sql.Append(" and a.is_actual <> 100 ");

//                    sql.Append(" left outer join " + cur_pref + "_data.users u1 on   a.nzp_user=u1.nzp_user ");
//                    sql.Append(" left outer join " + cur_pref + "_kernel.services s on  b.nzp_serv=s.nzp_serv ");
//                    sql.Append(" left outer join " + cur_pref + "_kernel.s_counttypes sc on  b.nzp_cnttype=sc.nzp_cnttype ");
//                    sql.Append(" left outer join " + cur_pref + "_data.users u on  b.user_block=u.nzp_user ");
//                    sql.Append(" Where b.nzp = " + finder.nzp_kvar);
//                    sql.Append(" and b.nzp_type = " + finder.nzp_type + " ");

//                    if (finder.dat_close == "") sql.Append(" and b.dat_close is null ");
//                    if (finder.nzp_counter > 0) sql.Append(" and b.nzp_counter = " + finder.nzp_counter.ToString());
//                    if (finder.nzp_serv > 0) sql.Append(" and b.nzp_serv=" + finder.nzp_serv);

//                    if (finder.RolesVal != null)
//                        foreach (_RolesVal role in finder.RolesVal)
//                            if (role.tip == Constants.role_sql && role.kod == Constants.role_sql_serv) sql.Append(" and b.nzp_serv in (" + role.val + ") ");
//                    sql.Append(" Order by a.num_ls asc, s.service, b.num_cnt, sc.name_type, b.nzp_cnttype, b.nzp_counter, a.dat_uchet desc, is_archive asc ");
//                    break;

//                case (int)CounterKinds.Dom: // показания домовых ПУ
//                    sql.Append(" , a.nzp_dom, a.is_uchet_ls, m.measure, a.ngp_cnt, a.ngp_lift, a.is_doit, tu.name_uchet, a.nzp_crd as nzp_cv ");
//                    sql.Append(" From ");
//                    sql.Append(cur_pref + "_data.counters_spis b ");
//                    sql.Append(" left outer join " + cur_pref + "_data.counters_dom a on b.nzp_counter = a.nzp_counter ");

//                    // is_actual = 0 - не показывать архивные, > 0 - показывать и архивные и неархивные
//                    // dat_close = "" - показывать только открытые, иначе - и открытые и закрытые       
//                    if (finder.is_actual == 0) sql.Append(" and a.is_actual <> 100 ");
//                    if (finder.dat_uchet != "") sql.Append(" and a.dat_uchet >= " + Utils.EStrNull(finder.dat_uchet));
//                    if (finder.dat_uchet_po != "") sql.Append(" and a.dat_uchet <= " + Utils.EStrNull(finder.dat_uchet_po));
//                    if (finder.date_begin != "") sql.Append(" and a.dat_uchet >= " + Utils.EStrNull(finder.date_begin));

//                    sql.Append(" left outer join " + cur_pref + "_kernel.s_measure m on a.nzp_measure = m.nzp_measure ");
//                    sql.Append(" left outer join " + cur_pref + "_kernel.s_typeuchet tu on a.is_pl = tu.is_pl ");
//                    sql.Append(" left outer join " + cur_pref + "_data.users u1 on  a.nzp_user=u1.nzp_user ");
//                    sql.Append(" left outer join " + cur_pref + "_kernel.services s on  b.nzp_serv=s.nzp_serv ");
//                    sql.Append(" left outer join " + cur_pref + "_kernel.s_counttypes sc on  b.nzp_cnttype=sc.nzp_cnttype ");
//                    sql.Append(" left outer join " + cur_pref + "_data.users u on  b.user_block=u.nzp_user ");
//                    sql.Append(" Where b.nzp = " + finder.nzp_dom);
//                    sql.Append(" and b.nzp_type = " + finder.nzp_type);

//                    if (finder.dat_close == "") sql.Append(" and b.dat_close is null ");
//                    if (finder.nzp_counter > 0) sql.Append(" and b.nzp_counter = " + finder.nzp_counter.ToString());
//                    if (finder.nzp_serv > 0) sql.Append(" and b.nzp_serv=" + finder.nzp_serv);
//                    if (finder.RolesVal != null)
//                        foreach (_RolesVal role in finder.RolesVal)
//                            if (role.tip == Constants.role_sql && role.kod == Constants.role_sql_serv) sql.Append(" and b.nzp_serv in (" + role.val + ") ");
//                    sql.Append(" Order by a.nzp_dom asc, s.service, b.num_cnt, sc.name_type, b.nzp_cnttype, b.nzp_counter, a.dat_uchet desc, is_archive asc ");
//                    break;

//                case (int)CounterKinds.Group: // показания групповых ПУ

//                case (int)CounterKinds.Communal: // показания коммунальных ПУ
//                    sql.Append(", a.is_uchet_ls, a.is_doit, tu.name_uchet, a.nzp_cg as nzp_cv ");
//                    sql.Append(" From ");
//                    sql.Append(cur_pref + "_data.counters_spis b ");
//                    sql.Append(" left outer join " + cur_pref + "_data.counters_group a on a.nzp_counter = b.nzp_counter ");
//                    // is_actual = 0 - не показывать архивные, > 0 - показывать и архивные и неархивные                        
//                    if (finder.is_actual == 0) sql.Append(" and a.is_actual <> 100 ");
//                    if (finder.dat_uchet != "") sql.Append(" and a.dat_uchet >= " + Utils.EStrNull(finder.dat_uchet));
//                    if (finder.dat_uchet_po != "") sql.Append(" and a.dat_uchet <= " + Utils.EStrNull(finder.dat_uchet_po));
//                    if (finder.date_begin != "") sql.Append(" and a.dat_uchet >= " + Utils.EStrNull(finder.date_begin));
//                    sql.Append(" left outer join " + cur_pref + "_kernel.s_typeuchet tu on  a.is_pl = tu.is_pl ");
//                    sql.Append(" left outer join " + cur_pref + "_data.users u1 on a.nzp_user=u1.nzp_user ");
//                    sql.Append(" left outer join " + cur_pref + "_kernel.services s on  b.nzp_serv=s.nzp_serv ");
//                    sql.Append(" left outer join " + cur_pref + "_kernel.s_counttypes sc on b.nzp_cnttype=sc.nzp_cnttype ");
//                    sql.Append(" left outer join " + cur_pref + "_data.users u on  b.user_block=u.nzp_user ");
//                    if (finder.nzp_kvar > 0)
//                    {
//                        sql.Append(" Where exists (select nzp_kvar from " + cur_pref + "_data.counters_link cl ");
//                        sql.Append(" where cl.nzp_counter = a.nzp_counter and cl.nzp_kvar = " + finder.nzp_kvar + ")");
//                    }
//                    else if (finder.nzp_dom > 0)
//                    {
//                        sql.Append(" Where b.nzp = " + finder.nzp_dom);
//                    }
//                    sql.Append(" and b.nzp_type = " + finder.nzp_type);
//                    // dat_close = "" - показывать только открытые, иначе - и открытые и закрытые
//                    if (finder.dat_close == "") sql.Append(" and b.dat_close is null ");
//                    if (finder.nzp_counter > 0) sql.Append(" and b.nzp_counter = " + finder.nzp_counter.ToString());
//                    if (finder.nzp_serv > 0) sql.Append(" and b.nzp_serv=" + finder.nzp_serv);
//                    if (finder.RolesVal != null)
//                        foreach (_RolesVal role in finder.RolesVal)
//                            if (role.tip == Constants.role_sql && role.kod == Constants.role_sql_serv) sql.Append(" and b.nzp_serv in (" + role.val + ") ");
//                    sql.Append(" Order by s.service, b.num_cnt, sc.name_type, b.nzp_cnttype, b.nzp_counter, a.dat_uchet desc, is_archive asc ");
//                    break;
//            }


//#else
//            StringBuilder sql = new StringBuilder();
//            sql.Append(" Select ");
//            sql.Append(" (case when a.is_actual <> 100 then 0 else 1 end) as is_archive, a.is_actual, a.val_cnt, a.dat_uchet, a.dat_when, a.nzp_user, u1.comment as user_name ");
//            sql.Append(" , b.nzp_counter, s.service, b.nzp_serv, b.num_cnt, b.nzp_cnttype, b.dat_prov, b.dat_provnext, b.dat_oblom, b.dat_poch, b.dat_close,  b.dat_block, b.user_block, u.comment as user_name_block, (current year to second - " + Constants.users_min.ToString() + " units minute) as cur_dat ");
//            sql.Append(" , sc.cnt_stage, sc.mmnog, sc.name_type");
//            switch (finder.nzp_type)
//            {
//                case (int)CounterKinds.Kvar: // показания квартирных ПУ
//                    sql.Append(", a.num_ls");
//                    sql.Append(", a.nzp_cr as nzp_cv");
//                    sql.Append(" From ");
//                    sql.Append(cur_pref + "_data:counters_spis b, ");
//                    sql.Append(" outer (" + cur_pref + "_data:counters a, outer " + cur_pref + "_data:users u1) ");
//                    sql.Append(", " + cur_pref + "_kernel:services s ");
//                    sql.Append(", " + cur_pref + "_kernel:s_counttypes sc ");
//                    sql.Append(" , outer " + cur_pref + "_data:users u ");
//                    sql.Append(" Where b.nzp = " + finder.nzp_kvar);
//                    break;
//                case (int)CounterKinds.Dom: // показания домовых ПУ
//                    sql.Append(" , a.nzp_dom, a.is_uchet_ls, m.measure, a.ngp_cnt, a.ngp_lift, a.is_doit, tu.name_uchet, a.nzp_crd as nzp_cv ");
//                    sql.Append(" From ");
//                    sql.Append(cur_pref + "_data:counters_spis b, ");
//                    sql.Append(" outer (" + cur_pref + "_data:counters_dom a, outer " + cur_pref + "_kernel:s_measure m, outer " + cur_pref + "_kernel:s_typeuchet tu, outer " + cur_pref + "_data:users u1) ");
//                    sql.Append(", " + cur_pref + "_kernel:services s ");
//                    sql.Append(", " + cur_pref + "_kernel:s_counttypes sc ");
//                    sql.Append(" , outer " + cur_pref + "_data:users u ");
//                    sql.Append(" Where b.nzp = " + finder.nzp_dom);
//                    break;
//                case (int)CounterKinds.Group: // показания групповых ПУ
//                case (int)CounterKinds.Communal: // показания коммунальных ПУ
//                    sql.Append(", a.is_uchet_ls, a.is_doit, tu.name_uchet, a.nzp_cg as nzp_cv ");
//                    sql.Append(" From ");
//                    sql.Append(cur_pref + "_data:counters_spis b, ");
//                    sql.Append(" outer (" + cur_pref + "_data:counters_group a, " + cur_pref + "_kernel:s_typeuchet tu, outer " + cur_pref + "_data:users u1) ");
//                    sql.Append(", " + cur_pref + "_kernel:services s ");
//                    sql.Append(", " + cur_pref + "_kernel:s_counttypes sc ");
//                    sql.Append(" , outer " + cur_pref + "_data:users u ");
//                    if (finder.nzp_kvar > 0)
//                    {
//                        sql.Append(" Where exists (select nzp_kvar from " + cur_pref + "_data:counters_link cl where cl.nzp_counter = a.nzp_counter and cl.nzp_kvar = " + finder.nzp_kvar + ")");
//                    }
//                    else if (finder.nzp_dom > 0)
//                    {
//                        //sql.Append(" Where exists (select cl.nzp_kvar from " + cur_pref + "_data:counters_link cl, " + cur_pref + "_data:kvar k where cl.nzp_counter = a.nzp_counter and cl.nzp_kvar = k.nzp_kvar and k.nzp_dom = " + finder.nzp_dom + ")");
//                        sql.Append(" Where b.nzp = " + finder.nzp_dom);
//                    }
//                    break;
//            }
//            sql.Append(" and b.nzp_type = " + finder.nzp_type);
//            sql.Append(" and a.nzp_counter = b.nzp_counter ");
//            sql.Append(" and a.nzp_user=u1.nzp_user ");
//            sql.Append(" and b.nzp_serv=s.nzp_serv ");
//            sql.Append(" and b.nzp_cnttype=sc.nzp_cnttype ");
//            sql.Append(" and b.user_block=u.nzp_user ");

//            // is_actual = 0 - не показывать архивные, > 0 - показывать и архивные и неархивные
//            // dat_close = "" - показывать только открытые, иначе - и открытые и закрытые
//            /*if      (finder.is_actual == 0 && finder.dat_close == "")   sql.Append(" and a.is_actual <> 100 and b.dat_close is null ");
//            else if (finder.is_actual != 0 && finder.dat_close == "")   sql.Append(" and (a.is_actual = 100 or b.dat_close is null) ");
//            else if (finder.is_actual == 0 && finder.dat_close != "")   sql.Append(" and a.is_actual <> 100 ");*/
			
//            if (finder.is_actual == 0)      sql.Append(" and a.is_actual <> 100 ");    
//            if (finder.dat_close == "")     sql.Append(" and b.dat_close is null ");

//            if (finder.nzp_counter > 0) sql.Append(" and b.nzp_counter = "+finder.nzp_counter.ToString());

//            if (finder.nzp_serv > 0) sql.Append(" and b.nzp_serv=" + finder.nzp_serv);
//            if (finder.dat_uchet != "") sql.Append(" and a.dat_uchet >= " + Utils.EStrNull(finder.dat_uchet));
//            if (finder.dat_uchet_po != "") sql.Append(" and a.dat_uchet <= " + Utils.EStrNull(finder.dat_uchet_po));
//            if (finder.date_begin != "") sql.Append(" and a.dat_uchet >= " + Utils.EStrNull(finder.date_begin));

//            //if (finder.get_koss)            sql.Append(" and b.nzp_serv in (25,210,11,242) ");
//            if (finder.RolesVal != null)
//                foreach (_RolesVal role in finder.RolesVal)
//                    if (role.tip == Constants.role_sql && role.kod == Constants.role_sql_serv) sql.Append(" and b.nzp_serv in (" + role.val + ") ");

//            switch (finder.nzp_type)
//            {
//                case (int)CounterKinds.Dom: 
//                    sql.Append(" and a.is_pl = tu.is_pl ");
//                    sql.Append(" and a.nzp_measure = m.nzp_measure ");
//                    sql.Append(" Order by a.nzp_dom asc, ");
//                    break;
//                case (int)CounterKinds.Kvar:
//                    sql.Append(" Order by a.num_ls asc, "); 
//                    break;
//                case (int)CounterKinds.Group:
//                case (int)CounterKinds.Communal:
//                    sql.Append(" and a.is_pl = tu.is_pl ");
//                    sql.Append(" Order by ");
//                    break;
//            }

//            sql.Append("s.service, b.num_cnt, sc.name_type, b.nzp_cnttype, b.nzp_counter, a.dat_uchet desc, is_archive asc "); 
//#endif


//            if (sql.ToString() == "")
//            {
//                conn_db.Close();
//                return null;
//            }

//            IDataReader reader;
//            ret = ExecRead(conn_db, out reader, sql.ToString(), true);
//            if (!ret.result)
//            {
//                conn_db.Close();
//                return null;
//            }

//            DateTime dt_block = DateTime.MinValue;
//            DateTime dt_cur = DateTime.MinValue;
//            int nzp_user = 0;
//            string userNameBlock = "";
//            string bl = "";

//            try
//            {
//                bool isCountersExist = false;
//                int i = 0;
//                long nzp_counter = Constants._ZERO_;
//                while (reader.Read())
//                {
//                    isCountersExist = true;
//                    if (reader["nzp_cv"] == DBNull.Value) continue;

//                    dt_block = DateTime.MinValue;
//                    dt_cur = DateTime.MinValue;
//                    nzp_user = 0;
//                    userNameBlock = "";
//                    bl = "";
//                    if (reader["user_block"] != DBNull.Value) nzp_user = (int)reader["user_block"]; //пользователь, который заблокирован
//                    if (reader["user_name_block"] != DBNull.Value) userNameBlock = ((string)reader["user_name_block"]).Trim(); //имя пользователя, который заблокирован
//                    if (reader["dat_block"] != DBNull.Value) dt_block = Convert.ToDateTime(reader["dat_block"]);//дата блокировки
//                    if (reader["cur_dat"] != DBNull.Value) dt_cur = Convert.ToDateTime(reader["cur_dat"]);//текущее время/дата - 20 мин

//                    if (nzp_user > 0 && dt_block != DateTime.MinValue) //заблокирован прибор учета
//                        if (nzp_user != nzpUser && dt_cur < dt_block) //если заблокирована запись другим пользователем и 20 мин не прошло
//                            bl = "Прибор учета заблокирован пользователем " + userNameBlock + " " + dt_block.ToString("dd.MM.yyyy в HH:mm") + ". Редактировать данные запрещено.";

//                    if (bl == "")
//                    {
//#if PG
//                        sql = new StringBuilder();
//                        if (finder.prm == Constants.act_mode_edit.ToString()) //если берут данные на изменение
//                        {
//                            sql.Append("update " + finder.pref + "_data.counters_spis set dat_block = now(), user_block = " + nzpUser + " where nzp_counter = " + Convert.ToString(reader["nzp_counter"]));
//                        }
//                        else //если  на просмотр
//                        {
//                            sql.Append("update " + finder.pref + "_data.counters_spis set dat_block = null, user_block = null where nzp_counter = " + Convert.ToString(reader["nzp_counter"]));
//                        }
//                        ret = ExecSQL(conn_db, sql.ToString(), true);
//                        if (!ret.result) throw new Exception("Ошибка обновления таблицы counters_spis");
//#else
//                        sql = new StringBuilder();
//                        if (finder.prm == Constants.act_mode_edit.ToString()) //если берут данные на изменение
//                        {
//                            sql.Append("update " + finder.pref + "_data:counters_spis set dat_block = current year to second, user_block = " + nzpUser + " where nzp_counter = " + Convert.ToString(reader["nzp_counter"]));
//                        }
//                        else //если  на просмотр
//                        {
//                            sql.Append("update " + finder.pref + "_data:counters_spis set dat_block = null, user_block = null where nzp_counter = " + Convert.ToString(reader["nzp_counter"]));
//                        }
//                        ret = ExecSQL(conn_db, sql.ToString(), true);
//                        if (!ret.result) throw new Exception("Ошибка обновления таблицы counters_spis");
//#endif
//                    }

//                    i++;
//                    Counter zap = new Counter();

//                    zap.num = i.ToString();
//                    if (reader["nzp_serv"] != DBNull.Value) zap.nzp_serv = Convert.ToInt32(reader["nzp_serv"]);
//                    switch (finder.nzp_type)
//                    {
//                        case (int)CounterKinds.Kvar:
//                            if (reader["num_ls"] == DBNull.Value)
//                                zap.num_ls = 0;
//                            else
//                                zap.num_ls = (int)reader["num_ls"];

//                            break;
//                        case (int)CounterKinds.Dom:
//                            if (reader["nzp_dom"] != DBNull.Value) zap.nzp_dom = Convert.ToInt32(reader["nzp_dom"]);
//                            if (reader["name_uchet"] != DBNull.Value) zap.name_uchet = Convert.ToString(reader["name_uchet"]);
//                            if (reader["is_uchet_ls"] != DBNull.Value) zap.is_uchet_ls = Convert.ToInt32(reader["is_uchet_ls"]);
//                            if (reader["measure"] != DBNull.Value) zap.measure = Convert.ToString(reader["measure"]);
//                            if (reader["ngp_cnt"] != DBNull.Value) zap.ngp_cnt = Convert.ToDecimal(reader["ngp_cnt"]);
//                            if (reader["ngp_lift"] != DBNull.Value) zap.ngp_lift = Convert.ToDecimal(reader["ngp_lift"]);
//                            if (reader["is_doit"] != DBNull.Value) zap.is_doit = Convert.ToInt32(reader["is_doit"]);
//                            break;
//                        case (int)CounterKinds.Group:
//                        case (int)CounterKinds.Communal:
//                            if (reader["name_uchet"] != DBNull.Value) zap.name_uchet = Convert.ToString(reader["name_uchet"]);
//                            if (reader["is_uchet_ls"] != DBNull.Value) zap.is_uchet_ls = Convert.ToInt32(reader["is_uchet_ls"]);
//                            if (reader["is_doit"] != DBNull.Value) zap.is_doit = Convert.ToInt32(reader["is_doit"]);
//                            break;
//                    }

//                    zap.nzp_type = finder.nzp_type;
//                    zap.cnt_type = CounterKind.GetKindNameById(zap.nzp_type);
//                    zap.block = bl;
//                    if (reader["service"] != DBNull.Value) zap.service = Convert.ToString(reader["service"]);

//                    if (reader["num_cnt"] != DBNull.Value) zap.num_cnt = Convert.ToString(reader["num_cnt"]);
//                    if (reader["mmnog"] != DBNull.Value) zap.mmnog = Convert.ToDecimal(reader["mmnog"]);
//                    if (reader["dat_close"] != DBNull.Value) zap.dat_close = String.Format("{0:dd.MM.yyyy}", reader["dat_close"]);
//                    if (reader["dat_prov"] != DBNull.Value) zap.dat_prov = String.Format("{0:dd.MM.yyyy}", reader["dat_prov"]);
//                    if (reader["dat_provnext"] != DBNull.Value) zap.dat_provnext = String.Format("{0:dd.MM.yyyy}", reader["dat_provnext"]);
//                    if (reader["dat_oblom"] != DBNull.Value) zap.dat_oblom = String.Format("{0:dd.MM.yyyy}", reader["dat_oblom"]);
//                    if (reader["dat_poch"] != DBNull.Value) zap.dat_poch = String.Format("{0:dd.MM.yyyy}", reader["dat_poch"]);
//                    if (reader["name_type"] != DBNull.Value) zap.name_type = Convert.ToString(reader["name_type"]);
//                    if (reader["dat_uchet"] != DBNull.Value) zap.dat_uchet = String.Format("{0:dd.MM.yyyy}", reader["dat_uchet"]);
//                    if (reader["cnt_stage"] != DBNull.Value) zap.cnt_stage = Convert.ToInt32(reader["cnt_stage"]);
//                    if (reader["nzp_counter"] != DBNull.Value) zap.nzp_counter = Convert.ToInt32(reader["nzp_counter"]);
//                    if (reader["val_cnt"] != DBNull.Value) zap.val_cnt = Convert.ToDecimal(reader["val_cnt"]);
//                    if (reader["is_actual"] != DBNull.Value) zap.is_actual = Convert.ToInt32(reader["is_actual"]);

//                    if (reader["dat_when"] == DBNull.Value) zap.dat_when = "";
//                    else
//                    {
//                        zap.dat_when = String.Format("{0:dd.MM.yyyy}", reader["dat_when"]);
//                        if (reader["user_name"] != DBNull.Value)
//                            if (Convert.ToString(reader["user_name"]).Trim() != "")
//                                zap.dat_when += " (" + Convert.ToString(reader["user_name"]).Trim() + ")";
//                    }

//                    if (nzp_counter != zap.nzp_counter)
//                    {
//                        nzp_counter = zap.nzp_counter;

//                        if (finder.year_ > 0 && finder.month_ > 0)
//                        {
//                            // определить плановый расход ПУ (среднее значение показания ПУ)
//                            DbParameters dbparam = new DbParameters();
//                            Prm finderPrm = new Prm();
//                            finderPrm.nzp_user = finder.nzp_user;
//                            finderPrm.pref = finder.pref;
//                            finderPrm.prm_num = 17;
//                            finderPrm.nzp_prm = 979;
//                            finderPrm.nzp = zap.nzp_counter;
//                            finderPrm.month_ = finder.month_;
//                            finderPrm.year_ = finder.year_;
//                            finderPrm = dbparam.FindSimplePrmValue(conn_db, finderPrm, out ret);
//                            dbparam.Close();
//                            if (!ret.result) throw new Exception(ret.text);

//                            zap.plan_rashod = finderPrm.val_prm;

//                            if (finder.nzp_type == (int)CounterKinds.Kvar)
//                            {
//                                Counter findercnt = new Counter();
//                                findercnt.pref = finder.pref;
//                                findercnt.month_ = finder.month_;
//                                findercnt.year_ = finder.year_;
//                                findercnt.nzp_type = (int)CounterKinds.Kvar;
//                                findercnt.nzp_kvar = finder.nzp_kvar;
//                                findercnt.nzp_serv = zap.nzp_serv;
//                                zap.normativ = GetNormative(conn_db, findercnt);
//                                zap.rashod_k_opl = GetRashodKOplate(conn_db, findercnt);
//                            }
//                        }
//                    }
//                    Spis.Add(zap);
//                }

//                reader.Close();
//                conn_db.Close();

//                if (Spis.Count == 0 && !isCountersExist)
//                {
//                    ret.result = false;
//                    ret.tag = -1;
//                    ret.text = "Приборы учета не найдены";
//                }

//                return Spis;
//            }
//            catch (Exception ex)
//            {
//                if (reader != null) reader.Close();
//                conn_db.Close();
//                ret = new Returns(false, ex.Message);
//                MonitorLog.WriteLog("Ошибка заполнения списка показаний ПУ " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
//                return null;
//            }

//        }//FindVal

//        /// <summary>
//        /// Получить показания ПУ введенных пользователем
//        /// </summary>
//        /// <param name="finder"></param>
//        /// <param name="ret"></param>
//        /// <returns></returns>
//#warning функции, работающие с counters, counters_dom, counters_group через counters_vals. Не использовать
//        public List<CounterVal> GetCountersUserVals(CounterVal finder, out Returns ret)
//        {
//            if (Constants.Trace) Utility.ClassLog.WriteLog("Старт функции GetCountersUserVals");
//            List<CounterVal> retList = new List<CounterVal>();

//            ret = Utils.InitReturns();
//            IDbConnection con_web = null;
//            IDbConnection con_db = null;
//            IDataReader reader = null;
//            StringBuilder sql = new StringBuilder();

//            try
//            {
//                #region Открытие соединения с БД

//                con_web = GetConnection(Constants.cons_Webdata);
//                con_db = GetConnection(Constants.cons_Kernel);

//                ret = OpenDb(con_web, true);
//                if (ret.result)
//                {
//                    ret = OpenDb(con_db, true);
//                }
//                if (!ret.result)
//                {
//                    MonitorLog.WriteLog("Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
//                    return null;
//                }

//                #endregion

//                #region Получение бд
//#if PG
//                var tempObj = ExecScalar(con_db, " SELECT dbname from " + Points.Pref + "_kernel.s_baselist where idtype=8", out ret, true);
//#else
//                var tempObj = ExecScalar(con_db, " SELECT dbname from " + Points.Pref + "_kernel:s_baselist where idtype=8", out ret, true);
//#endif
//                if (!ret.result)
//                {
//                    MonitorLog.WriteLog("Ошибка получение webfon bd:" + ret.text, MonitorLog.typelog.Error, true);
//                    throw new Exception(ret.text);
//                }
//                string webFonDbName = tempObj.ToString().Trim();
//                #endregion


//                sql.Append(" select co.service, co.num_cnt, co.nzp_cnttype, co.prev_dat, co.prev_val, co.cur_dat, co.cur_val, co.cnt_stage, co. formula, k.pref ");
//#if PG
//                sql.Append(" from " + webFonDbName + ". counters_ord co, ");
//                sql.Append("  " + Points.Pref + "_data.  kvar k ");
//                sql.Append(" where co.num_ls = k.num_ls ");
//                //sql.Append(" and dat_month = '" + Convert.ToDateTime(Points.CalcMonth.name).ToShortDateString() + "' ");
//                sql.Append(" and dat_month = '" + Convert.ToDateTime(finder.dat_uchet).ToShortDateString() + "' ");
//                sql.Append(" and k.nzp_kvar = " + finder.nzp_kvar + " ");
//                sql.Append(" and  co.prev_val is not null and co.cur_val is not null ");
//#else
//sql.Append(" from " + webFonDbName + ": counters_ord co, ");
//                sql.Append("  " + Points.Pref + "_data:  kvar k ");                
//                sql.Append(" where co.num_ls = k.num_ls ");                
//                //sql.Append(" and dat_month = '" + Convert.ToDateTime(Points.CalcMonth.name).ToShortDateString() + "' ");
//                sql.Append(" and dat_month = '" + Convert.ToDateTime(finder.dat_uchet).ToShortDateString() + "' ");
//                sql.Append(" and k.nzp_kvar = " + finder.nzp_kvar + " ");
//                sql.Append(" and  co.prev_val is not null and co.cur_val is not null ");
//#endif

//                if (!ExecRead(con_db, out reader, sql.ToString(), false).result)
//                {
//                    MonitorLog.WriteLog("Ошибка получения показаний пользовательских ПУ", MonitorLog.typelog.Error, true);
//                    throw new Exception(ret.text);
//                }

//                while (reader.Read())
//                {
//                    CounterVal cv = new CounterVal();

//                    cv.service = reader["service"] != DBNull.Value ? Convert.ToString(reader["service"]) : "";
//                    cv.num_cnt = reader["num_cnt"] != DBNull.Value ? Convert.ToString(reader["num_cnt"]) : "";
//                    cv.nzp_cnttype = reader["nzp_cnttype"] != DBNull.Value ? Convert.ToInt32(reader["nzp_cnttype"]) : 0;
//                    cv.dat_uchet_pred = reader["prev_dat"] != DBNull.Value ? Convert.ToDateTime(reader["prev_dat"]).ToShortDateString() : "-";
//                    cv.val_cnt_pred = reader["prev_val"] != DBNull.Value ? Decimal.Round(Convert.ToDecimal(reader["prev_val"]), 4) : -1;
//                    cv.dat_uchet = reader["cur_dat"] != DBNull.Value ? Convert.ToDateTime(reader["cur_dat"]).ToShortDateString() : "-";
//                    cv.val_cnt = reader["cur_val"] != DBNull.Value ? Decimal.Round(Convert.ToDecimal(reader["cur_val"]), 4) : -1;
//                    cv.cnt_stage = reader["cnt_stage"] != DBNull.Value ? Convert.ToInt32(reader["cnt_stage"]) : 0;
//                    string localDb = reader["pref"] != DBNull.Value ? Convert.ToString(reader["pref"]).Trim() : "";

//                    #region Определение типа прибора учета
//                    object tempCnt = null;
//                    if (localDb != "")
//                    {
//#if PG
//                        tempCnt = ExecScalar(con_db, " select name_type from " +
//                                                    localDb + "_kernel.s_counttypes where nzp_cnttype=" + cv.nzp_cnttype + " ", out ret, true);
//#else
//                        tempCnt = ExecScalar(con_db, " select name_type from " +
//                            localDb + "_kernel:s_counttypes where nzp_cnttype=" + cv.nzp_cnttype + " ", out ret, true);
//#endif
//                        if (!ret.result)
//                        {
//                            MonitorLog.WriteLog("Ошибка получение типа прибора учета:" + ret.text, MonitorLog.typelog.Error, true);
//                            throw new Exception(ret.text);
//                        }
//                    }
//                    cv.name_type = tempCnt != null ? tempCnt.ToString().Trim() : "-";
//                    #endregion

//                    string formulaStr = reader["formula"] != DBNull.Value ? Convert.ToString(reader["formula"]) : "";

//                    #region Расчет формулы
//                    if (cv.val_cnt_pred != -1 && cv.val_cnt != -1)
//                    {
//                        decimal rashod = cv.val_cnt - cv.val_cnt_pred;
//                        if (rashod < 0)
//                        {
//                            int formula = 0;
//                            try
//                            {
//                                formula = Convert.ToInt32(formulaStr);
//                            }
//                            catch (Exception)
//                            {
//                                formula = 1;
//                            }

//                            rashod = Convert.ToDecimal(Math.Pow(10, cv.cnt_stage)) * Convert.ToDecimal(formula) - cv.val_cnt_pred + cv.val_cnt;
//                        }
//                        cv.rashod = Decimal.Round(rashod, 4).ToString();
//                    }
//                    else
//                    {
//                        cv.rashod = "-";
//                    }


//                    #endregion

//                    //заполнение списка
//                    retList.Add(cv);
//                }
//                if (Constants.Trace) Utility.ClassLog.WriteLog("Финиш функции GetCountersUserVals");
//                return retList;

//            }
//            catch (Exception ex)
//            {
//                MonitorLog.WriteLog("Ошибка выполнения процедуры GetCountersUserVals : " + ex.Message, MonitorLog.typelog.Error, true);
//                return null;
//            }
//            finally
//            {
//                #region Закрытие соединений

//                if (con_db != null)
//                {
//                    con_db.Close();
//                }

//                if (con_web != null)
//                {
//                    con_web.Close();
//                }

//                if (reader != null)
//                {
//                    reader.Close();
//                }

//                sql.Remove(0, sql.Length);

//                #endregion
//            }
//        }
    }
}
