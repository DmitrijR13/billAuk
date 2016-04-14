using System;
using System.Data;
using System.Data.Common;
using System.Data.Odbc;
using System.Linq;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using System.Collections.Generic;
using STCLINE.KP50.IFMX.Kernel.source.CommonType;

namespace STCLINE.KP50.DataBase
{
    public partial class DbCounterKernel : DbCounterClient
    {
        /// <summary> Получить ПУ для редактирования
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="transaction"></param>
        /// <param name="newCounter"></param>
        /// <param name="ret"></param>
        /// <param name="nzpUser">код пользователя в основной БД</param>
        /// <returns></returns>
        protected Counter GetCounter(IDbConnection conn_db, IDbTransaction transaction, Counter newCounter, out Returns ret, int nzpUser)
        {
            string counters_spis = newCounter.pref +  DBManager.sDataAliasRest + "counters_spis";
            string counters_replaced = newCounter.pref + DBManager.sDataAliasRest + "counters_replaced";

            string sql = 
                " SELECT cs.nzp_type, cs.nzp, cs.nzp_serv, cs.nzp_cnt, cs.is_gkal, cs.nzp_cnttype, cs.num_cnt, cs.dat_prov, cs.dat_provnext," +
                " cs.dat_oblom, cs.dat_poch, cs.dat_close, cs.is_pl, cs.cnt_ls, cs.dat_block, cs.user_block, " +
                (tableDelimiter == ":" ? " (current year to second - " + Constants.users_min.ToString() + " units minute) " : 
                " (now() -  INTERVAL ' " + Constants.users_min.ToString() + " minutes ') ") +
                " as cur_dat, cs.comment, " + DBManager.sNvlWord + "(cr.nzp_counter_old,-1) as nzp_counter_old" +
                " FROM " + counters_spis + " cs" +
                " LEFT OUTER JOIN " + counters_replaced + " cr ON cs.nzp_counter = cr.nzp_counter_new AND cr.is_actual AND cr.nzp_counter_new <> cr.nzp_counter_old" +
                " WHERE nzp_counter = " + newCounter.nzp_counter.ToString();

            Counter oldCounter = new Counter();
            IDataReader reader;
            ret = ExecRead(conn_db, transaction, out reader, sql, true);
            if (!ret.result)
            {
                if (transaction != null) transaction.Rollback();
                conn_db.Close();
                return null;
            }

            if (reader.Read())
            {
                try
                {
                    DateTime dt_block = DateTime.MinValue;
                    DateTime dt_cur = DateTime.MinValue;
                    int nzp_user = 0;

                    if (reader["user_block"] != DBNull.Value) nzp_user = (int)reader["user_block"];
                    if (reader["dat_block"] != DBNull.Value) dt_block = Convert.ToDateTime(reader["dat_block"]);
                    if (reader["cur_dat"] != DBNull.Value) dt_cur = Convert.ToDateTime(reader["cur_dat"]);

                    if (nzp_user > 0 && dt_block != DateTime.MinValue) //заблокирован прибор учета
                    {
                        if (nzp_user != nzpUser && dt_cur < dt_block) //если заблокирована запись другим пользователем и 20 мин не прошло
                        {
                            ret.result = false;
                            ret.text = "Редактировать данные запрещено, поскольку с ними работает другой пользователь";
                            ret.tag = -4;
                            if (transaction != null) transaction.Rollback();
                            conn_db.Close();
                            return null;
                        }
                    }

                    ret = ExecSQL(conn_db, transaction, "update " + counters_spis + " set dat_block = " +
#if PG
 " now() " +
#else
 " current year to second " +
#endif
 ", user_block = " +
                                                        nzpUser + " where nzp_counter = " + newCounter.nzp_counter.ToString(), true);
                    if (!ret.result)
                    {
                        ret.result = false;
                        ret.text = "Ошибка обновления таблицы counters_spis";
                        if (transaction != null) transaction.Rollback();
                        conn_db.Close();
                        return null;
                    }

                    if (reader["nzp_type"] != DBNull.Value) oldCounter.nzp_type = (int)reader["nzp_type"];
                    if (reader["nzp"] != DBNull.Value) oldCounter.nzp = Convert.ToInt64(reader["nzp"]);
                    if (reader["nzp_serv"] != DBNull.Value) oldCounter.nzp_serv = (int)reader["nzp_serv"];
                    if (reader["nzp_cnt"] != DBNull.Value) oldCounter.nzp_cnt = (int)reader["nzp_cnt"];
                    if (reader["is_gkal"] != DBNull.Value) oldCounter.is_gkal = (int)reader["is_gkal"];
                    if (reader["nzp_cnttype"] != DBNull.Value) oldCounter.nzp_cnttype = (int)reader["nzp_cnttype"];
                    if (reader["num_cnt"] != DBNull.Value) oldCounter.num_cnt = (string)reader["num_cnt"];
                    if (reader["is_pl"] != DBNull.Value) oldCounter.is_pl = (int)reader["is_pl"];
                    else oldCounter.is_pl = -1;
                    if (reader["dat_prov"] != DBNull.Value) oldCounter.dat_prov = Convert.ToDateTime(reader["dat_prov"]).ToShortDateString();
                    if (reader["dat_provnext"] != DBNull.Value) oldCounter.dat_provnext = Convert.ToDateTime(reader["dat_provnext"]).ToShortDateString();
                    if (reader["dat_oblom"] != DBNull.Value) oldCounter.dat_oblom = Convert.ToDateTime(reader["dat_oblom"]).ToShortDateString();
                    if (reader["dat_poch"] != DBNull.Value) oldCounter.dat_poch = Convert.ToDateTime(reader["dat_poch"]).ToShortDateString();
                    if (reader["dat_close"] != DBNull.Value) oldCounter.dat_close = Convert.ToDateTime(reader["dat_close"]).ToShortDateString();
                    if (reader["comment"] != DBNull.Value) oldCounter.comment = ((string)reader["comment"]).Trim();
                    if (reader["nzp_counter_old"] != DBNull.Value) oldCounter.nzp_counter_old = (int)reader["nzp_counter_old"];

                    reader.Close();
                }
                catch (Exception ex)
                {
                    reader.Close();
                    ret.result = false;
                    ret.text = ex.Message;
                    if (transaction != null) transaction.Rollback();
                    conn_db.Close();
                    return null;
                }
            }

            return oldCounter;
        }

        /// <summary> Получить краткую информацию о ПУ
        /// </summary>
        protected Counter GetCounter(IDbConnection conn_db, IDbTransaction transaction, Counter finder, out Returns ret)
        {
            string counters_spis = finder.pref + "_data" + DBManager.tableDelimiter + "counters_spis";
            string s_counttypes = finder.pref + "_kernel" + DBManager.tableDelimiter + "s_counttypes";
            string kvar = finder.pref + "_data" + DBManager.tableDelimiter + "kvar";
            string sql = "Select c.nzp_counter, c.nzp_type, c.nzp, c.nzp_serv, c.nzp_cnttype, c.num_cnt, c.is_pl, c.is_gkal, mmnog, cnt_stage, c.dat_close ";
            sql += ", (select nzp_measure from " + finder.pref + "_kernel" + DBManager.tableDelimiter + "s_counts where nzp_cnt = c.nzp_cnt " +
                   " union select nzp_measure from " + finder.pref + "_kernel" + DBManager.tableDelimiter + "s_countsdop where nzp_cnt = c.nzp_cnt) nzp_measure ";
            if (finder.nzp_type == (int)CounterKinds.Kvar) sql += ", k.num_ls";
            sql += " From " + counters_spis + " c ";
            if (finder.nzp_type == (int)CounterKinds.Kvar) sql += " left outer join " + kvar + " k on c.nzp = k.nzp_kvar ";
            sql += " , " + s_counttypes + " ct ";
            sql += " Where ct.nzp_cnttype = c.nzp_cnttype and c.nzp_counter = " + finder.nzp_counter;

            IDataReader reader;
            ret = ExecRead(conn_db, transaction, out reader, sql, true);

            if (!ret.result) return null;

            if (reader.Read())
            {
                try
                {
                    Counter counter = new Counter();
                    if (reader["nzp_counter"] != DBNull.Value) counter.nzp_counter = Convert.ToInt32(reader["nzp_counter"]);
                    if (reader["nzp_measure"] != DBNull.Value) counter.nzp_measure = Convert.ToInt32(reader["nzp_measure"]);
                    if (reader["nzp_type"] != DBNull.Value) counter.nzp_type = (int)reader["nzp_type"];
                    if (counter.nzp_type == (int)CounterKinds.Kvar && reader["num_ls"] != DBNull.Value) counter.num_ls = (int)reader["num_ls"];
                    if (reader["nzp"] != DBNull.Value) counter.nzp = Convert.ToInt64(reader["nzp"]);
                    if (reader["nzp_serv"] != DBNull.Value) counter.nzp_serv = (int)reader["nzp_serv"];
                    if (reader["nzp_cnttype"] != DBNull.Value) counter.nzp_cnttype = (int)reader["nzp_cnttype"];
                    if (reader["mmnog"] != DBNull.Value) counter.mmnog = Convert.ToDecimal(reader["mmnog"]);
                    if (reader["cnt_stage"] != DBNull.Value) counter.cnt_stage = Convert.ToInt32(reader["cnt_stage"]);
                    if (reader["num_cnt"] != DBNull.Value) counter.num_cnt = ((string)reader["num_cnt"]).Trim();
                    if (reader["is_pl"] != DBNull.Value) counter.is_pl = (int)reader["is_pl"];
                    else counter.is_pl = -1;
                    if (reader["is_gkal"] != DBNull.Value) counter.is_gkal = (int)reader["is_gkal"];
                    if (reader["dat_close"] != DBNull.Value) counter.dat_close = ((DateTime)reader["dat_close"]).ToShortDateString();
                    else counter.is_gkal = -1;

                    reader.Close();
                    return counter;
                }
                catch (Exception ex)
                {
                    reader.Close();
                    ret = new Returns(false, ex.Message);
                    return null;
                }
            }
            else
            {
                reader.Close();
                ret = new Returns(false, "Прибор учета не найден");
                return null;
            }
        }

        [Obsolete("Функции, работающие с counters, counters_dom, counters_group через counters_vals. Не использовать")]
        public Returns CopyCounterReadingToRealBank(List<CounterVal> finder)
        {
            #region подключение к базе
            string conn_kernel = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            Returns ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;
            #endregion
            IDbTransaction transaction = conn_db.BeginTransaction();
            foreach (CounterVal cv in finder)
            {
                cv.dopFind = new List<string>();
                cv.dopFind.Add("All ist");
                ret = CopyCounterReadingToRealBank(conn_db, transaction, cv);
                if (!ret.result)
                {
                    transaction.Rollback();
                    conn_db.Close();
                    return ret;
                }
            }
            if (ret.result) transaction.Commit();
            else transaction.Rollback();
            conn_db.Close();
            return ret;
        }

        /// <summary>
        /// Сохранить показания ПУ
        /// </summary>
        [Obsolete("Функции, работающие с counters, counters_dom, counters_group через counters_vals. Не использовать")]
        public Returns SaveCountersVals(List<CounterVal> newVals)
        {
            Returns ret = Utils.InitReturns();

            #region Проверка входных параметров
            if (newVals == null || newVals.Count == 0)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Неверные входные параметры";
                return ret;
            }

            if (newVals[0].nzp_user < 1)
            {
                ret.result = false;
                ret.tag = -2;
                ret.text = "Не определен пользователь";
                return ret;
            }

            string pref;
            if (newVals[0].pref == "")
            {
                ret.result = false;
                ret.tag = -3;
                ret.text = "Не задан префикс базы данных";
                return ret;
            }
            else
                pref = newVals[0].pref.Trim();

            //Андрей К. 22.01.2014 Решил, что год и месяй должны соответствовать текущему расчетному месяцу
            //if (newVals[0].year_ < 1)
            //{
            //    ret.result = false;
            //    ret.tag = -4;
            //    ret.text = "Не задан расчетный год";
            //    return ret;
            //}
            //int month_ = newVals[0].month_;

            var calcMonth = Points.GetCalcMonth(new CalcMonthParams(pref));

            int ist = newVals[0].ist;
            if (ist <= 0)
                return new Returns(false, "Не задан источник данных");
            #endregion

            string connectionString = Points.GetConnByPref(newVals[0].pref);
            var conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;

            IDbTransaction transaction = null;

            try
            {
                #region определение локального пользователя
                int nzpUser = newVals[0].nzp_user;

                /*DbWorkUser db = new DbWorkUser();
                int nzpUser = db.GetLocalUser(conn_db, newVals[0], out ret);
                db.Close();
                if (!ret.result)
                {
                    return ret;
                }*/

                int webNzpUser = newVals[0].nzp_user;
                string webLogin = newVals[0].webLogin;
                string webUname = newVals[0].webUname;
                #endregion

                //вытащить данные counters_spis
                /*
                if (Points.IsSmr)
                {
                    ret = ExecRead(conn_db, out reader,
                    " Select * From " + pref + "_data:counters_spis " +
                    " Where nzp_counter = " + val.nzp_counter +
                    "   and is_actual <> 100 "
                    , true);
                    if (!ret.result)
                    {
                        conn_db.Close();
                        return ret;
                    }
                    else
                    {
                        if (reader.Read())
                        {
                        }
                    }
                    reader.Close();
                }
                */

                var counters_vals = pref + "_charge_" + (calcMonth.year_ % 100).ToString("00") + DBManager.tableDelimiter + "counters_vals";


                string sql, dat_uchet, val_cnt = "";

                foreach (CounterVal val in newVals) // ЦИКЛ по пришедшим показаниям
                {
                    #region проверка входных параметров
                    DateTime dat = new DateTime();
                    if (DateTime.TryParse(val.dat_uchet, out dat))
                    {
                        dat_uchet = ", dat_uchet = '" + dat.ToShortDateString() + "'";
                    }
                    else
                    {
                        ret.result = false;
                        ret.tag = -6;
                        ret.text = "Неверные входные параметры";
                        return ret;
                    }

                    decimal value_ = 0;

                    if (val.val_cnt_s == "")
                    {
                        if (val.ngp_cnt > 0)
                        {
                            return new Returns(false, "Нельзя указывать расход по нежилым помещениям без задания показания прибора учета", -7);
                        }

                        if (val.ngp_lift > 0)
                        {
                            return new Returns(false, "Нельзя указывать расход по электроснабжению лифтов без задания показания прибора учета", -7);
                        }
                    }
                    else
                    {
                        if (decimal.TryParse(val.val_cnt_s, out value_))
                        {
                            val_cnt = value_.ToString();
                        }
                        else
                        {
                            ret.result = false;
                            ret.tag = -5;
                            ret.text = "Неверные входные параметры";
                            return ret;
                        }
                    }
                    #endregion

                    #region определим, отличается ли новое значение от сохраненного
                    if (val.nzp_cv > 0)
                    {
                        sql = " Select nzp_cv, nzp_counter, nzp_type, dat_uchet, val_cnt, ngp_cnt, ngp_lift, iscalc " +
                            " From " + counters_vals +
                            " Where nzp_cv = " + val.nzp_cv;
                    }
                    else
                    {
                        sql = " Select nzp_cv, nzp_counter, nzp_type, dat_uchet, val_cnt, ngp_cnt, ngp_lift, iscalc " +
                            " From " + counters_vals +
                            " Where nzp_counter = " + val.nzp_counter + " and month_ = " + val.month_ +
                                " and dat_uchet = " + Utils.EStrNull(val.dat_uchet);
                    }
                    MyDataReader reader;
                    ret = ExecRead(conn_db, out reader, sql, true);
                    if (!ret.result)
                    {
                        return ret;
                    }

                    bool isChanged = false;
                    int nzpCounter = 0;
                    int nzpType = 0;
                    int isCalc = 0;

                    string tab_cnt = pref + "_data" + tableDelimiter;

                    int nzpCv = 0;
                    bool newPu = false; //если добавлена новая запись в counters_vals

                    if (reader.Read())
                    {
                        if (reader["nzp_counter"] != DBNull.Value) nzpCounter = Convert.ToInt32(reader["nzp_counter"]);

                        if (reader["nzp_cv"] != DBNull.Value) nzpCv = Convert.ToInt32(reader["nzp_cv"]);
                        if (val.nzp_cv < 1) val.nzp_cv = nzpCv;

                        if (reader["iscalc"] != DBNull.Value) isCalc = Convert.ToInt32(reader["iscalc"]);
                        if (reader["nzp_type"] != DBNull.Value) nzpType = (int)reader["nzp_type"];

                        if (val.val_cnt_s == "")
                        {
                            isChanged = reader["val_cnt"] != DBNull.Value;

                            if (!isChanged && reader["dat_uchet"] != DBNull.Value && (DateTime)reader["dat_uchet"] != dat)
                                isChanged = true;
                        }
                        else
                        {
                            if (reader["dat_uchet"] != DBNull.Value)
                                if ((DateTime)reader["dat_uchet"] != dat)
                                    isChanged = true;
                            if (!isChanged)
                                if (reader["val_cnt"] != DBNull.Value)
                                {
                                    if (Convert.ToDecimal(reader["val_cnt"]) != value_)
                                        isChanged = true;
                                }
                                else isChanged = true;
                            if (!isChanged)
                                if (reader["ngp_cnt"] != DBNull.Value)
                                    if (Convert.ToDecimal(reader["ngp_cnt"]) != val.ngp_cnt)
                                        isChanged = true;
                            if (!isChanged)
                                if (reader["ngp_lift"] != DBNull.Value)
                                    if (Convert.ToDecimal(reader["ngp_lift"]) != val.ngp_lift)
                                        isChanged = true;
                        }
                    }
                    else
                    {
                        // показания не найдены, следовательно, это новые показания, которые нужно вставить
                        nzpCounter = val.nzp_counter;
                        nzpType = val.nzp_type;
                        isChanged = true;
                    }

                    reader.Close();
                    #endregion

                    try { transaction = conn_db.BeginTransaction(); }
                    catch { transaction = null; }

                    var nzp_dict_event = 0;
                    sql = "";
                    if (val.val_cnt_s == "")
                    {
                        sql = " Update " + counters_vals +
                              " Set val_cnt = null, ngp_cnt = null, ngp_lift = null" + (isChanged ? ", is_new = 1" : "") + dat_uchet +
                              " Where nzp_cv = " + val.nzp_cv;

                        nzp_dict_event = 6491;
                    }
                    else if (isChanged)
                    {
                        if (val.nzp_cv > 0)
                        {
                            sql = " Update " + counters_vals +
                                  " Set val_cnt = " + val_cnt + dat_uchet +
                                     ", ngp_cnt = " + val.ngp_cnt +
                                     ", ngp_lift = " + val.ngp_lift +
                                     ", nzp_user = " + nzpUser +
                                    ", dat_when = " + sCurDateTime +
                                    ", ist = " + ist +
                                     ", is_new = 1" +
                                  " Where nzp_cv = " + val.nzp_cv;
                            nzp_dict_event = 6491;
                        }
                        else
                        {
                            if (val.ist == (int)CounterVal.Ist.Bank) //если выгрузка от банка то пишем новую запись
                            {
                                sql = "insert into " + counters_vals + " " +
                                " ( nzp, nzp_type, nzp_counter, month_, dat_uchet, val_cnt,  nzp_user, ist) " +
                                "values (" + val.num_ls + "," + (int)CounterKinds.Kvar + "," + val.nzp_counter + "," + calcMonth.month_ + ",'" + val.dat_uchet + "'," + val.val_cnt_s + "," + val.nzp_user + "," + val.ist + ")";
                                newPu = true; //новая запись
                                //todo: Проверить сохранение показаний ИПУ, ОДПУ, ввода показаний по списку
                                nzp_dict_event = 6489;
                            }
                        }
                    }
                    if (sql != "")
                    {
                        ret = ExecSQL(conn_db, transaction, sql, true);
                        if (!ret.result)
                        {
                            return ret;
                        }
                        if (newPu)
                        {
                            int nzp_cv = GetSerialValue(conn_db, transaction);
                            val.nzp_cv = nzp_cv;
                        }
                    }

                    #region Добавление в sys_events событий 'Изменение показания ПУ' и 'Добавление показания ПУ'

                    InsertSysEvent(new SysEvents()
                    {
                        pref = val.pref,
                        nzp_user = webNzpUser,
                        nzp_dict = nzp_dict_event,
                        nzp_obj = val.nzp_counter,
                        note = "Значение: " + (val.val_cnt_s != "" ? val.val_cnt_s : "удалено") + ", дата учета: " + val.dat_uchet
                    }, transaction, conn_db);

                    #endregion

                    // подсчитать количество показаний на текущую дату
                    sql = "select count(*) from " + counters_vals + " where dat_uchet = " + Utils.EStrNull(val.dat_uchet) + " and nzp_counter = " + val.nzp_counter;
                    object obj = ExecScalar(conn_db, transaction, sql, out ret, true);
                    if (!ret.result)
                    {
                        return ret;
                    }

                    int cnt = 0;
                    try
                    {
                        cnt = Convert.ToInt32(obj);
                    }
                    catch
                    {
                        cnt = 0;
                    }

                    // если показаний больше одного, то еще раз сохраняем действующее показание
                    if (cnt > 1) isChanged = true;

                    if (isChanged && Points.SaveCounterReadingsToRealBank)
                    {
                        // с этой проверкой не сохраняются показания в середине месяца 
                        //if (val.nzp_cv > 0)
                        {
                            val.nzp_user = newVals[0].nzp_user;
                            val.webLogin = newVals[0].webLogin;
                            val.webUname = newVals[0].webUname;
                            val.year_ = calcMonth.year_;
                            val.pref = newVals[0].pref;
                            CopyCounterReadingToRealBank(conn_db, transaction, val);
                        }

                        if (val.val_cnt_s == "" && val.nzp_cv > 0)
                        {
                            sql = "delete from " + counters_vals + " where nzp_cv = " + val.nzp_cv;

                            ret = ExecSQL(conn_db, transaction, sql, true);
                            if (!ret.result)
                            {
                                return ret;
                            }

                            #region Добавление в sys_events события 'Удаление показания ПУ'

                            InsertSysEvent(new SysEvents()
                            {
                                pref = val.pref,
                                nzp_user = webNzpUser,
                                nzp_dict = 6490,
                                nzp_obj = val.nzp_counter,
                                note = "Показание успешно удалено"
                            }, transaction, conn_db);

                            #endregion
                        }
                    }
                    if (transaction != null) transaction.Commit();
                }
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
            }
            finally
            {
                if (!ret.result && transaction != null) transaction.Rollback();
                conn_db.Close();
            }
            return ret;
        }

        /// <summary>
        /// Копирует показание в основной банк данных 
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        /// <param name="finder">обязательные параметры: pref, year_, nzp_user, webLogin, webUname, nzp_cv</param>
        /// <returns></returns>
        [Obsolete("Функции, работающие с counters, counters_dom, counters_group через counters_vals. Не использовать")]
        protected Returns CopyCounterReadingToRealBank(IDbConnection connection, IDbTransaction transaction, CounterVal finder)
        {
            string revalFieldsForInsert = "";
            string revalValuesForInsert = "";

            var counters_vals = finder.pref + "_charge_" + (finder.year_ % 100).ToString("00") + DBManager.tableDelimiter + "counters_vals";
            var counters_spis = finder.pref + "_data" + DBManager.tableDelimiter + "counters_spis";

            if (!TempTableInWebCashe(connection, transaction, counters_vals))
            {
                return new Returns(false, "Таблица с показаниями не найдена");
            }

            Returns ret;
            CounterVal val = GetCounterVal(connection, transaction, finder, out ret);
            if (!ret.result) return ret;

            CounterKinds counterKind = CounterKind.GetKindById(val.nzp_type);
            DateTime datUchet = val.dat_uchet != "" ? Convert.ToDateTime(val.dat_uchet) : DateTime.MinValue;
            CounterVal.Ist ist = CounterVal.GetIstById(val.ist);

            IDataReader reader;
            string sql;

            #region Проверка наличия более приоритетного показания за данный период
            //Источники показаний в порядке убывания приоритета
            //CounterVal.Ist.Operator
            //CounterVal.Ist.File
            //CounterVal.Ist.Gilec
            bool check = true;
            if (finder.dopFind != null && finder.dopFind.Count > 0)
            {
                foreach (string s in finder.dopFind)
                {
                    if (s == "All ist")
                    {
                        check = false;
                        break;
                    }
                }
            }
            if (check)
                if (ist != CounterVal.Ist.Operator)
                {
                    DateTime dat_uchet = DateTime.Parse(val.dat_uchet);
                    if (dat_uchet.Day == 1) dat_uchet = dat_uchet.AddMonths(-1);
                    sql = "select nzp_cv, val_cnt, dat_uchet from " + counters_vals +
                        " where nzp_counter = " + val.nzp_counter +
                        " and ist = " + (int)CounterVal.Ist.Operator + "" +
                        " and dat_uchet between " + Utils.EStrNull(dat_uchet.AddDays(1).ToShortDateString()) + " and " + Utils.EStrNull(dat_uchet.AddMonths(1).ToShortDateString()) +
                        " and val_cnt is not null";
                    ret = ExecRead(connection, transaction, out reader, sql, true);
                    if (!ret.result) return ret;
                    if (reader.Read())
                    {
                        ret.text = "Показание из файла (" + val.val_cnt_s + " от " + val.dat_uchet + ") не учтено" +
                            ", т.к. есть показание, введенное оператором (" + Convert.ToDecimal(reader["val_cnt"]) + " от " + Convert.ToDateTime(reader["dat_uchet"]).ToShortDateString() + ")";
                        ret.tag = -1;
                        reader.Close();
                        reader.Dispose();
                        return ret;
                    }
                    reader.Close();
                    reader.Dispose();
                }
            #endregion

            if (val.nzp_counter < 1)
            {
                ret.result = false;
                ret.text = "Прибора учета не определен";
                return ret;
            }

            if (datUchet == DateTime.MinValue)
            {
                ret.result = false;
                ret.text = "Дата показания прибора учета не определена";
                return ret;
            }

            string tab_cnt_short = "";
            switch (counterKind)
            {
                case CounterKinds.Kvar:
                    tab_cnt_short = "counters";
                    break;
                case CounterKinds.Group:
                case CounterKinds.Communal:
                    tab_cnt_short = "counters_group";
                    break;
                case CounterKinds.Dom:
                    tab_cnt_short = "counters_dom";
                    break;
                default:
                    ret.result = false;
                    ret.text = "Вид прибора учета не определен";
                    return ret;
            }
            string tab_cnt = finder.pref + "_data" + DBManager.tableDelimiter + tab_cnt_short;

            Counter finderCounter = new Counter();
            finderCounter.pref = finder.pref;
            finderCounter.nzp_counter = val.nzp_counter;
            finderCounter.nzp_type = (int)counterKind;

            Counter counter = GetCounter(connection, transaction, finderCounter, out ret);
            if (!ret.result) return ret;

            if (finder.nzp_user_main < 1)
            {
                finder.nzp_user_main = finder.nzp_user;

                /*#region определение локального пользователя
                DbWorkUser db = new DbWorkUser();
                int nzpUser = db.GetLocalUser(connection, transaction, finder, out ret);
                db.Close();
                if (!ret.result) return ret;
                finder.nzp_user_main = nzpUser;
                #endregion*/
            }

            RecordMonth calcMonth = Points.GetCalcMonth(new CalcMonthParams(finder.pref));

            DateTime? revalBeginDate = null;

            //Определить предыдущее показание, чтобы установить месяц начала перерасчета
            sql = "select dat_uchet from " + tab_cnt + " where nzp_counter = " + val.nzp_counter +
                //" and case when day(dat_uchet) = 1 then dat_uchet else date(mdy(month(dat_uchet), 1, year(dat_uchet)) + 1 units month) end < " + Utils.EStrNull(datUchet.ToShortDateString()) +
                " and dat_uchet < " + Utils.EStrNull(datUchet.ToShortDateString()) +
                " and is_actual <> 100" +
                " order by dat_uchet desc";

            ret = ExecRead(connection, transaction, out reader, sql, true);
            if (!ret.result) return ret;

            if (reader.Read())
            {
                if (reader["dat_uchet"] != DBNull.Value)
                {
                    revalBeginDate = Convert.ToDateTime(reader["dat_uchet"]);
                }
            }
            reader.Close();
            reader.Dispose();

            if (revalBeginDate == null) revalBeginDate = datUchet;

            string revalFieldsForUpdate = "";

            //если месяц начала перерасчета предшествует текущему расчетному месяцу,
            //то определим месяц окончания перерасчета
            if (revalBeginDate.Value < new DateTime(calcMonth.year_, calcMonth.month_, 1))
            {
                //Определить последующее показание, чтобы установить месяц окончания перерасчета
                sql = "select dat_uchet from " + tab_cnt + " where nzp_counter = " + val.nzp_counter +
                    //" and case when day(dat_uchet) = 1 then dat_uchet else date(mdy(month(dat_uchet), 1, year(dat_uchet)) + 1 units month) end > " + Utils.EStrNull(datUchet.ToShortDateString()) +
                    " and dat_uchet > " + Utils.EStrNull(datUchet.ToShortDateString()) +
                    " and is_actual <> 100" +
                    " order by dat_uchet asc";

                ret = ExecRead(connection, transaction, out reader, sql, true);
                if (!ret.result) return ret;

                DateTime? revalEndDate = null;

                if (reader.Read())
                {
                    if (reader["dat_uchet"] != DBNull.Value)
                    {
                        revalEndDate = Convert.ToDateTime(reader["dat_uchet"]);
                    }
                }
                reader.Close();
                reader.Dispose();

                //если последующее показание отсутствует, то считаем месяцем окончания перерасчета месяц, за который вносится показание
                if (revalEndDate == null) revalEndDate = datUchet.AddMonths(-1);

                //если месяц окончания перерасчета превышает или равен текущему расчетному месяцу,
                //то устанавливаем его равным предыдущему расчетному месяцу
                if (revalEndDate.Value >= new DateTime(calcMonth.year_, calcMonth.month_, 1))
                    revalEndDate = revalEndDate.Value.AddMonths(-1);

                //заполняем поля для выставления перерасчета
                revalFieldsForUpdate = ", month_calc = mdy(" + calcMonth.month_ + ",1," + calcMonth.year_ + ")" +
                    ", dat_s = " + Utils.EStrNull(revalBeginDate.Value.ToShortDateString()) +
                    ", dat_po = " + Utils.EStrNull(revalEndDate.Value.ToShortDateString());
                revalFieldsForInsert = ", month_calc, dat_s, dat_po";
                revalValuesForInsert = ", mdy(" + calcMonth.month_ + ",1," + calcMonth.year_ + "), " + Utils.EStrNull(revalBeginDate.Value.ToShortDateString()) + ", " + Utils.EStrNull(revalEndDate.Value.ToShortDateString());
            }
            else revalBeginDate = null;

            //удалить показание на дату
            sql = " Update " + tab_cnt +
                " Set is_actual = 100 " +
                    ", user_del = " + finder.nzp_user_main +
                    ", dat_del = " + sCurDateTime +
                revalFieldsForUpdate +
                " Where nzp_counter = " + val.nzp_counter +
                "   and is_actual <> 100 " +
                //"   and case when day(dat_uchet) = 1 then dat_uchet else date(mdy(month(dat_uchet), 1, year(dat_uchet)) + 1 units month) end = " + Utils.EStrNull(datUchet.ToShortDateString());
                "   and dat_uchet = " + Utils.EStrNull(datUchet.ToShortDateString());

            ret = ExecSQL(connection, transaction, sql, true);
            if (!ret.result) return ret;

            if (val.val_cnt_s != "")
            {
                string tab_insert = " Insert into " + tab_cnt;

                if (counterKind == CounterKinds.Kvar)
                {

                    tab_insert +=
                        " (nzp_counter, nzp_kvar, num_ls, nzp_serv, nzp_cnttype, num_cnt, dat_uchet, val_cnt " +
                        (Points.IsIpuHasNgpCnt ? ", ngp_cnt" : "") +
                        ", is_actual, nzp_user, dat_when" + revalFieldsForInsert + ") " +
                        " Values ( " + val.nzp_counter + "," + counter.nzp + "," + counter.num_ls + "," + counter.nzp_serv + "," +
                                    counter.nzp_cnttype + "," + Utils.EStrNull(counter.num_cnt, "") + ", " + Utils.EStrNull(datUchet.ToShortDateString()) + "," + val.val_cnt_s +
                                    (Points.IsIpuHasNgpCnt ? ", " + val.ngp_cnt : "") +
                                    ", 1," + finder.nzp_user_main + "," + sCurDate + revalValuesForInsert + ")";
                }
                else if (counterKind == CounterKinds.Group || counterKind == CounterKinds.Communal)
                {
                    tab_insert +=
                        " (nzp_counter, dat_uchet, val_cnt, is_pl, is_actual, nzp_user, dat_when, is_gkal" + revalFieldsForInsert + ") " +
                        " Values (" + val.nzp_counter + "," + Utils.EStrNull(datUchet.ToShortDateString()) + "," + val.val_cnt_s + ", " + counter.is_pl + ", 1, " + finder.nzp_user_main + ", " + sCurDate +
                        // признак ГКал для ГВС (измение под КОМПЛАТ 2.0)
                        "," + (counter.is_gkal > -1 ? counter.is_gkal.ToString() : "null") +
                        revalValuesForInsert + ")";
                }
                else if (counterKind == CounterKinds.Dom)
                {
                    tab_insert +=
                        " (nzp_counter, nzp_dom, nzp_serv, nzp_measure, nzp_cnttype, num_cnt, dat_uchet, val_cnt, ngp_cnt, ngp_lift, is_pl, is_actual, nzp_user, dat_when, is_gkal, sum_otopl" + revalFieldsForInsert + ") " +
                        " Values ( " + val.nzp_counter + "," + counter.nzp + "," + counter.nzp_serv + "," + val.nzp_measure + "," +
                        counter.nzp_cnttype + "," + Utils.EStrNull(counter.num_cnt, "") + ", " + Utils.EStrNull(datUchet.ToShortDateString()) + ", " + val.val_cnt_s + ", " +
                        val.ngp_cnt + "," + val.ngp_lift + "," + (counter.is_pl > -1 ? counter.is_pl.ToString() : "null") + "," +
                        "1," + finder.nzp_user_main + "," + sCurDate +
                        // признак ГКал для ГВС (измение под КОМПЛАТ 2.0)
                        "," + (counter.is_gkal > -1 ? counter.is_gkal.ToString() : "null") + ", " + CalcArea(val, connection, transaction, finder.pref, counter.nzp, out ret) +
                        revalValuesForInsert + ")";
                }

                ret = ExecSQL(connection, transaction, tab_insert, true);
                if (!ret.result) return ret;


                if (Points.RecalcMode == RecalcModes.AutomaticWithCancelAbility)
                {
                    #region добавление признака перерасчета
                    EditInterDataMustCalc eid = new EditInterDataMustCalc();

                    eid.nzp_wp = Points.GetPoint(finder.pref).nzp_wp;
                    eid.pref = finder.pref;
                    eid.nzp_user = finder.nzp_user;
                    eid.webLogin = finder.webLogin;
                    eid.webUname = finder.webUname;
                    //eid.dat_s = "'" + new DateTime(Points.CalcMonth.year_, Points.CalcMonth.month_, 1).ToShortDateString() + "'";
                    //eid.dat_po = "'" + new DateTime(Points.CalcMonth.year_, Points.CalcMonth.month_, 1).AddMonths(1).AddDays(-1).ToShortDateString() + "'";
                    eid.intvType = enIntvType.intv_Day;
                    eid.table = tab_cnt_short;

                    if (counterKind == CounterKinds.Kvar)
                    {
                        eid.primary = "nzp_cr";
                        eid.mcalcType = enMustCalcType.Counter;
                    }
                    else if (counterKind == CounterKinds.Dom)
                    {
                        eid.primary = "nzp_crd";
                        eid.mcalcType = enMustCalcType.DomCounter;
                    }
                    else if (counterKind == CounterKinds.Group || counterKind == CounterKinds.Communal)
                    {
                        eid.primary = "nzp_cg";
                        eid.mcalcType = enMustCalcType.GroupCounter;
                    }
                    eid.kod2 = counter.nzp_counter;

                    eid.dopFind = new List<string>();
                    eid.dopFind.Add(" and nzp_counter = " + counter.nzp_counter);
                    eid.keys = new Dictionary<string, string>();
                    eid.vals = new Dictionary<string, string>();
                    eid.comment_action = finder.comment_action;
                    DbEditInterData db2 = new DbEditInterData();
                    db2.MustCalc(connection, transaction, eid, out ret);
                    db2.Close();
                    #endregion
                }
            }
            return ret;
        }

        [Obsolete("Функция - дубликат. Не использовать")]
        private decimal CalcArea(CounterVal newVal, IDbConnection conn_db, IDbTransaction transaction_id, string pref, long nzp_dom, out Returns ret)
        {
            ret = Utils.InitReturns();
            DateTime datUchet = Convert.ToDateTime(newVal.dat_uchet);
            DateTime dat_s = new DateTime(datUchet.Year, datUchet.Month, 1).AddMonths(-1);
            DateTime dat_po = new DateTime(datUchet.Year, datUchet.Month, 1).AddDays(-1);
            try
            {
                var dt =
                    ClassDBUtils.OpenSQL(
                        "select count(*) as cnt from " + pref + "_data" + tableDelimiter +
                        "prm_10 where nzp_prm = 1288 and is_actual <> 100 " +
                        " and " + Utils.EStrNull(dat_po.ToShortDateString()) + " >=dat_s " +
                        " and " + Utils.EStrNull(dat_s.ToShortDateString()) + " <= dat_po" +
                        " and val_prm = '1'",
                        conn_db, transaction_id);

                if (dt.resultCode < 0) throw new Exception(dt.resultMessage);

                if (dt.resultData.Rows[0]["cnt"].ToString().Trim() != "0")
                {
                    string str = " SELECT sum(replace(val_prm,\",\",\".\")" + DBManager.sConvToNum + ") sum_pl " +
                         " FROM " + pref + "_data" + tableDelimiter + "prm_1 WHERE nzp_prm=133 and is_actual<>100 " +
                         " and '" + dat_po.ToString("dd.MM.yyyy") + "' >=dat_s and '" + dat_s.ToString("dd.MM.yyyy") + "' <= dat_po " +
                         " and nzp in (select distinct t.nzp_kvar from " + pref + "_data" + tableDelimiter + "tarif t," + pref + "_data" + tableDelimiter + "kvar k,"
                         + pref + "_data" + tableDelimiter + "dom d " +
                         " WHERE nzp_serv = 8 and is_actual<>100 and t.nzp_kvar=k.nzp_kvar and k.nzp_dom=d.nzp_dom and d.nzp_dom=" + nzp_dom + " " +
                         " and '" + dat_po.ToString("dd.MM.yyyy") + "' >= dat_s and '" + dat_s.ToString("dd.MM.yyyy") + "' <=dat_po) " +
                         " and nzp in (select nzp from " + pref + "_data: prm_3 " +
                         " WHERE nzp_prm = 51 and is_actual<>100 and (val_prm = '1' or val_prm = '3'))";
                    dt = ClassDBUtils.OpenSQL(str, conn_db, transaction_id);

                    if (dt.resultCode < 0) throw new Exception(dt.resultMessage);

                    if (dt.resultData == null || dt.resultData.Rows.Count == 0) return 0m;

                    return (decimal)dt.resultData.Rows[0][0];
                }
                else return 0;
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                return 0;
            }
        }


        /// <summary>
        /// Копирует показания из counters_vals в банк данные
        /// </summary>
        public Returns CopyCounterValueToRealBank(List<CounterValLight> finder)
        {
            if (finder == null || finder.Count == 0) throw new Exception("Список показаний пуст");
            if (finder[0].pref == "") throw new Exception("Не задан префикс базы данных");

            #region подключение к базе
            string conn_kernel = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            Returns ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;
            #endregion

            string pref = finder[0].pref.Trim();
            var calcMonth = Points.GetCalcMonth(new CalcMonthParams(pref));

            string counters_vals = finder[0].pref + "_charge_" + (calcMonth.year_ % 100).ToString("00") + DBManager.tableDelimiter + "counters_vals";
            if (!TempTableInWebCashe(conn_db, counters_vals))
            {
                return new Returns(false, "Таблица с показаниями не найдена");
            }

            #region определение локального пользователя
            //----------------------------------------------------------------------------------------------------------------------------------
            int nzpUser = finder[0].nzp_user;

            /*DbWorkUser db = new DbWorkUser();
            int nzpUser = db.GetLocalUser(conn_db, finder[0], out ret);
            db.Close();
            if (!ret.result) return ret;*/
            //----------------------------------------------------------------------------------------------------------------------------------
            #endregion

            foreach (CounterValLight cv in finder)
            {
                cv.dopFind = new List<string>();
                cv.dopFind.Add("All ist");
                cv.nzp_user = nzpUser;

                // сохранить показание с удалением существующих показаний (последний параметр = true)
                ret = SaveCounterValueToRealBank(conn_db, cv, true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return ret;
                }
            }

            conn_db.Close();
            return ret;
        }

        /// <summary>
        /// Удалить показания ПУ
        /// </summary>
        public Returns DeleteCounterVal(CounterValLight val)
        {
            Returns ret = new Returns();
            IDbConnection conn_db = null;

            try
            {
                if (val.nzp_user < 1) throw new Exception("Не определен пользователь");
                if (val.pref == "") throw new Exception("Не задан префикс базы данных");
                if (val.nzp_key <= 0) throw new Exception("Показание не определено");

                string pref = val.pref.Trim();
                var calcMonth = Points.GetCalcMonth(new CalcMonthParams(pref));

                #region подключение к базе
                string connectionString = Points.GetConnByPref(pref);
                conn_db = GetConnection(connectionString);
                ret = OpenDb(conn_db, true);
                if (!ret.result) return ret;
                #endregion

                #region определение локального пользователя
                //----------------------------------------------------------------------------------------------------------------------------------
                int nzpUser = val.nzp_user;

                /*DbWorkUser db = new DbWorkUser();
                int nzpUser = db.GetLocalUser(conn_db, val, out ret);
                db.Close();
                if (!ret.result) return ret;*/
                //----------------------------------------------------------------------------------------------------------------------------------
                #endregion

                val.nzp_user_web = val.nzp_user;
                val.year_ = calcMonth.year_;
                val.month_ = calcMonth.month_;
                val.nzp_user = nzpUser;
                val.val_cnt_s = "";

                string counters_vals = val.pref + "_charge_" + (calcMonth.year_ % 100).ToString("00") + DBManager.tableDelimiter + "counters_vals";
                if (!TempTableInWebCashe(conn_db, counters_vals))
                {
                    return new Returns(false, "Таблица с показаниями не найдена");
                }

                ret = SaveCounterValueToRealBank(conn_db, val);
                if (!ret.result) throw new Exception(ret.text);
                return ret;
            }
            catch (Exception ex)
            {
                if (conn_db != null) conn_db.Close();
                return new Returns(false, ex.Message, -1);
            }
        }

        /// <summary>
        /// Сохранить показания ПУ
        /// </summary>
        /// <param name="newVals">
        /// nzp_user - идентификатор пользователя
        /// pref - префикс банка данных
        /// webLogin - логин пользоваетля
        /// webUname - имя пользователя
        /// nzp_counter - идентификатор прибора учета
        /// val_cnt_s - показание ПУ
        /// nzp_type - тип ПУ
        /// nzp_serv - идентификатор услуги
        /// nzp_kvar - идентификатор лс
        /// nzp_counter - идентификатор ПУ
        /// ist - источник показания(CounterVal.Ist)
        /// dat_uchet - дата учета
        /// </param>
        /// <returns></returns>
        public Returns SaveCountersValsLight(List<CounterValLight> newVals)
        {
            List<CounterValLight> failVals;
            return SaveCountersValsLight(newVals, out failVals);
        }

        /// <summary>
        /// Сохранить показания ПУ с возвратом списка не загруженных показаний и причины
        /// </summary>
        public Returns SaveCountersValsLight(List<CounterValLight> newVals, out List<CounterValLight> failVals)
        {
            Returns ret = new Returns();
            IDbConnection conn_db = null;
            failVals = new List<CounterValLight>();

            try
            {
                if (newVals == null || newVals.Count == 0) throw new Exception("Список показаний пуст");
                if (newVals[0].nzp_user < 1) throw new Exception("Не определен пользователь");
                if (newVals[0].pref == "") throw new Exception("Не задан префикс базы данных");

                string pref = newVals[0].pref.Trim();
                var calcMonth = Points.GetCalcMonth(new CalcMonthParams(pref));

                #region подключение к базе
                string connectionString = Points.GetConnByPref(pref);
                conn_db = GetConnection(connectionString);
                ret = OpenDb(conn_db, true);
                if (!ret.result) return ret;
                #endregion

                string counters_vals = newVals[0].pref + "_charge_" + (calcMonth.year_ % 100).ToString("00") + DBManager.tableDelimiter + "counters_vals";
                if (!TempTableInWebCashe(conn_db, counters_vals))
                {
                    return new Returns(false, "Таблица с показаниями не найдена");
                }

                #region определение локального пользователя
                //------------------------------------------------------------------------------------------------------------------------------
                int nzpUser = newVals[0].nzp_user;

                /*DbWorkUser db = new DbWorkUser();
                int nzpUser = db.GetLocalUser(conn_db, newVals[0], out ret);
                db.Close();
                if (!ret.result) return ret;*/
                //------------------------------------------------------------------------------------------------------------------------------
                #endregion

                int notSuccessCount = 0;

                foreach (CounterValLight val in newVals) // ЦИКЛ по пришедшим показаниям
                {
                    val.nzp_user_web = newVals[0].nzp_user;
                    val.nzp_user = nzpUser;
                    val.webLogin = newVals[0].webLogin;
                    val.webUname = newVals[0].webUname;
                    val.year_ = calcMonth.year_;
                    val.month_ = calcMonth.month_;
                    val.pref = newVals[0].pref;
                    ret = SaveCounterValueToRealBank(conn_db, val, true);
                    if (!ret.result || ret.tag == -1)
                    {
                        if (!ret.result)
                            notSuccessCount++;
                        //возвращаем список незагруженных показаний с указанием причины
                        val.upload_result = ret.text;
                        failVals.Add(val);
                    }
                }

                ret.tag = (-1) * notSuccessCount;
                return ret;
            }
            catch (Exception ex)
            {
                if (conn_db != null) conn_db.Close();
                return new Returns(false, ex.Message, -1);
            }
        }
        
        /// <summary>
        /// Сохранить в реальный банк
        /// </summary>
        protected Returns SaveCounterValueToRealBank(IDbConnection connection, CounterValLight finder)
        {
            return SaveCounterValueToRealBank(connection, finder, false);
        }

        /// <summary>
        /// Сохранить в реальный банк, перегрузка функции
        /// </summary>
        protected Returns SaveCounterValueToRealBank(IDbConnection connection, CounterValLight finder, bool CanDeleteExistingValues)
        {
            // ВАЖНО! nzp_user должен быть уже определен из локального банка данных

            Returns ret = new Returns();
            IDataReader reader = null;
            object count;
            int cnt = 0;

            DateTime dat;
            decimal value_;
            string nzp_key = "";

            try
            {
                #region проверка данных
                //-----------------------------------------------------------------------------------------------------------------------------------------------
                if (finder.nzp_user < 1) return new Returns(false, "Не определен пользователь", -1);
                if (finder.pref == "") return new Returns(false, "Не задан префикс базы данных", -1);

                if (finder.year_ < 1) return new Returns(false, "Не указан год", -1);
                if (finder.month_ < 1) return new Returns(false, "Не указан месяц", -1);

                if (!DateTime.TryParse(finder.dat_uchet, out dat)) return new Returns(false, "Неверно указана дата учета", -1);

                if (finder.val_cnt_s == "" && finder.nzp_key > 0)
                {
                    if (finder.ngp_cnt > 0) return new Returns(false, "Нельзя указывать расход по нежилым помещениям без задания показания прибора учета", -1);
                    if (finder.ngp_lift > 0) return new Returns(false, "Нельзя указывать расход по электроснабжению лифтов без задания показания прибора учета", -1);
                }
                else
                {
                    if (!decimal.TryParse(finder.val_cnt_s, out value_)) return new Returns(false, "Неверное значение показания прибора учета", -1);
                }

                if (finder.nzp_type != (int)CounterKinds.Kvar &&
                    finder.nzp_type != (int)CounterKinds.Dom &&
                    finder.nzp_type != (int)CounterKinds.Group &&
                    finder.nzp_type != (int)CounterKinds.Communal) return new Returns(false, "Неверно определен тип прибора учета", -1);

                if (finder.ist <= 0) return new Returns(false, "Не задан источник данных", -1);
                if (finder.nzp_counter < 1) return new Returns(false, "Не определен прибор учета", -1);
                //-----------------------------------------------------------------------------------------------------------------------------------------------
                #endregion

                string tab_cnt = "";
                switch (finder.nzp_type)
                {
                    case (int)CounterKinds.Kvar:
                        tab_cnt = "counters";
                        nzp_key = "nzp_cr";
                        break;
                    case (int)CounterKinds.Group:
                    case (int)CounterKinds.Communal:
                        tab_cnt = "counters_group";
                        nzp_key = "nzp_cg";
                        break;
                    case (int)CounterKinds.Dom:
                        tab_cnt = "counters_dom";
                        nzp_key = "nzp_crd";
                        break;
                }

                tab_cnt = finder.pref + "_data" + DBManager.tableDelimiter + tab_cnt;

                string sql = "";
                DateTime dat_uchet;

                string revalFieldsForUpdate = "";
                string revalFieldsForInsert = "";
                string revalValuesForInsert = "";

                // получить информацию по прибору учета
                Counter finderCounter = new Counter();
                finderCounter.pref = finder.pref;
                finderCounter.nzp_counter = finder.nzp_counter;
                finderCounter.nzp_type = finder.nzp_type;

                Counter counter = GetCounter(connection, null, finderCounter, out ret);
                if (!ret.result) return ret;

                #region проверка более приоритетного показания
                //----------------------------------------------------------------------------------------------------
                bool checkPriorityVal = true;
                if (finder.dopFind != null && finder.dopFind.Count > 0)
                {
                    foreach (string s in finder.dopFind)
                    {
                        if (s == "All ist")
                        {
                            checkPriorityVal = false;
                            break;
                        }
                    }
                }
                // При условии что данные не удаляются
                // проверить более приоритетное показание для индивидуального ПУ
                // если источник показания - не оператор и не контроллер 
                if (finder.nzp_key <= 0 || finder.val_cnt_s.Trim() != "")
                {
                    if (checkPriorityVal && finder.nzp_type == (int)CounterKinds.Kvar &&
                        finder.ist != (int)CounterVal.Ist.Operator && finder.ist != (int)CounterVal.Ist.Controller)
                    {


                        #region если дата учета показания больше дата закрытия ПУ

                        var dateClose = DateTime.MinValue;
                        var dateUchet = DateTime.MinValue;
                        DateTime.TryParse(counter.dat_close, out dateClose);
                        DateTime.TryParse(finder.dat_uchet, out dateUchet);
                        if (dateClose != DateTime.MinValue && dateUchet != DateTime.MinValue)
                        {
                            if (dateUchet > dateClose) //такие показания не сохраняем 
                            {
                                ret.text = "Показание из файла (" + finder.val_cnt_s + " от " + finder.dat_uchet +
                                           ") не учтено" +
                                           ", т.к. дата учета больше даты закрытия (" + counter.dat_close + ")";
                                ret.tag = -1;
                                return ret;
                            }
                        }
                        #endregion


                        sql = "select val_cnt, dat_uchet from " + tab_cnt +
                              " where nzp_counter = " + finder.nzp_counter +
                              "   and ist = " + (int)CounterVal.Ist.Operator + "" +
                              "   and dat_uchet = " + Utils.EStrNull(finder.dat_uchet) +
                              "   and val_cnt is not null "+
                              "   and is_actual<>100";

                        ret = ExecRead(connection, out reader, sql, true);
                        if (!ret.result) throw new Exception(ret.text);
                        if (reader.Read())
                        {
                            ret.text = "Показание из файла (" + finder.val_cnt_s + " от " + finder.dat_uchet +
                                       ") не учтено" +
                                       ", т.к. есть показание, введенное оператором (" +
                                       Convert.ToDecimal(reader["val_cnt"]) + " от " +
                                       Convert.ToDateTime(reader["dat_uchet"]).ToShortDateString() + ")";
                            ret.tag = -1;
                            reader.Close();
                            reader.Dispose();
                            return ret;
                        }
                        reader.Close();
                        reader.Dispose();
                    }
                }
                //----------------------------------------------------------------------------------------------------
                #endregion
                               
                #region если указано, что новое показание может перезатереть старые (используется только для функции CopyCounterValueToRealBank)
                //----------------------------------------------------------------------------------------------------
                if (CanDeleteExistingValues && finder.nzp_key <= 0)
                {
                    sql = " Update " + tab_cnt + " Set is_actual = 100, user_del = " + finder.nzp_user + ", dat_del = " + DBManager.sCurDateTime +
                        " where nzp_counter = " + finder.nzp_counter +
                        "   and dat_uchet = " + Utils.EStrNull(finder.dat_uchet) +
                        "   and is_actual <> 100 " +
                        "   and " + DBManager.sNvlWord + "(val_cnt, -1) > -1 ";
                    ret = ExecSQL(connection, sql);
                    if (!ret.result) throw new Exception(ret.text);
                }
                //----------------------------------------------------------------------------------------------------
                #endregion

                #region проверка возможности сохранения показания, которое не удаляется и не перезатирает старые показания
                //----------------------------------------------------------------------------------------------------
                if (finder.val_cnt_s != "" && !CanDeleteExistingValues)
                {
                    sql = "select count(*) as cnt from " + tab_cnt +
                        " where nzp_counter = " + finder.nzp_counter +
                        "   and dat_uchet = " + Utils.EStrNull(finder.dat_uchet) +
                        "   and is_actual <> 100 " +
                        "   and " + DBManager.sNvlWord + "(val_cnt, -1) > -1 " +
                        "   and " + nzp_key + " <> " + finder.nzp_key;

                    // для контрольного показания добавить источник - контроллер
                    if (finder.ist == (int)CounterVal.Ist.Controller)
                    {
                        sql += " and ist = " + finder.ist;
                    }

                    count = ExecScalar(connection, sql, out ret, true);
                    if (!ret.result) throw new Exception(ret.text);
                    cnt = 0;

                    try
                    { cnt = Convert.ToInt32(count); }
                    catch
                    { cnt = 0; }

                    if (cnt > 0) throw new Exception("На указанную дату есть показание");
                }
                //----------------------------------------------------------------------------------------------------
                #endregion

                #region подготовка данных перед сохранением контрольного показания
                //----------------------------------------------------------------------------------------------------
                if (finder.ist == (int)CounterVal.Ist.Controller)
                {
                    finder.mmnog = counter.mmnog;
                    finder.cnt_stage = counter.cnt_stage;

                    ret = PrepareDataForControlValCnt(connection, finder, tab_cnt, nzp_key);
                    if (!ret.result) throw new Exception(ret.text);
                }
                //----------------------------------------------------------------------------------------------------
                #endregion

                #region подготовка данных перед сохранением показания с режим сохранения "Сохранить весь расход в текущем расчетном месяце"
                //----------------------------------------------------------------------------------------------------
                if (finder.isSaveRashodInCurPeriod == 1)
                {
                    ret = PrepareDataForSaveRashodInCurPeriod(connection, finder, tab_cnt, nzp_key);
                    if (!ret.result) throw new Exception(ret.text);
                }
                //----------------------------------------------------------------------------------------------------
                #endregion

                #region вставка признака перерасчета
                //----------------------------------------------------------------------------------------------------
                if (Points.RecalcMode == RecalcModes.AutomaticWithCancelAbility)
                {
                    RecordMonth calcMonth = Points.GetCalcMonth(new CalcMonthParams(finder.pref));
                    DateTime? DatS = null;
                    DateTime? DatPo = null;

                    dat_uchet = Convert.ToDateTime(finder.dat_uchet);

                    #region определить дату начала перерасчета
                    //----------------------------------------------------------------------------------------------------
                    sql = "select max(dat_uchet) as dat_uchet from " + tab_cnt +
                        " where nzp_counter = " + finder.nzp_counter +
                        "   and dat_uchet < " + Utils.EStrNull(dat_uchet.ToShortDateString()) +
                        "   and is_actual <> 100 ";
                    ret = ExecRead(connection, out reader, sql, true);
                    if (!ret.result) throw new Exception(ret.text);

                    if (reader.Read())
                    {
                        if (reader["dat_uchet"] != DBNull.Value) DatS = Convert.ToDateTime(reader["dat_uchet"]);
                    }

                    reader.Close();
                    reader.Dispose();
                    if (DatS == null) DatS = dat_uchet;
                    //----------------------------------------------------------------------------------------------------
                    #endregion

                    #region определить дату окончания перерасчета
                    //----------------------------------------------------------------------------------------------------
                    //если месяц начала перерасчета предшествует текущему расчетному месяцу, то определим месяц окончания перерасчета
                    if (DatS < new DateTime(calcMonth.year_, calcMonth.month_, 1))
                    {
                        //Определить последующее показание, чтобы установить месяц окончания перерасчета
                        sql = "select min(dat_uchet) dat_uchet from " + tab_cnt +
                            " where nzp_counter = " + finder.nzp_counter +
                            "   and dat_uchet > " + Utils.EStrNull(dat_uchet.ToShortDateString()) +
                            "   and is_actual <> 100 ";

                        ret = ExecRead(connection, out reader, sql, true);
                        if (!ret.result) return ret;

                        if (reader.Read())
                        {
                            if (reader["dat_uchet"] != DBNull.Value) DatPo = Convert.ToDateTime(reader["dat_uchet"]);
                        }
                        reader.Close();
                        reader.Dispose();

                        //если последующее показание отсутствует, то считаем месяцем окончания перерасчета месяц, за который вносится показание
                        if (DatPo == null) DatPo = dat_uchet.AddDays(-1);

                        //если месяц окончания перерасчета превышает или равен текущему расчетному месяцу,
                        //то устанавливаем его равным предыдущему расчетному месяцу
                        if (DatPo.Value >= new DateTime(calcMonth.year_, calcMonth.month_, 1)) DatPo = new DateTime(calcMonth.year_, calcMonth.month_, 1).AddDays(-1);

                        //заполняем поля для выставления перерасчета
                        // для вставки
                        revalFieldsForInsert = ", month_calc, dat_s, dat_po";
                        revalValuesForInsert = "," + DBManager.MDY(calcMonth.month_, 1, calcMonth.year_) + "," + Utils.EStrNull(DatS.Value.ToShortDateString()) + "," + Utils.EStrNull(DatPo.Value.ToShortDateString());

                        // для обновления
                        revalFieldsForUpdate = ", month_calc = " + DBManager.MDY(calcMonth.month_, 1, calcMonth.year_) +
                            ", dat_s = " + Utils.EStrNull(DatS.Value.ToShortDateString()) +
                            ", dat_po = " + Utils.EStrNull(DatPo.Value.ToShortDateString());
                    }
                    else DatPo = null;
                    //----------------------------------------------------------------------------------------------------
                    #endregion

                    #region вставка признака
                    //----------------------------------------------------------------------------------------------------
                    if (DatS < new DateTime(calcMonth.year_, calcMonth.month_, 1))
                    {
                        MustCalcTable must = new MustCalcTable();
                        must.NzpKvar = Convert.ToInt32(counter.nzp);
                        must.NzpServ = counter.nzp_serv;
                        must.NzpSupp = 0;
                        must.Month = calcMonth.month_;
                        must.Year = calcMonth.year_;
                        must.DatS = DatS.Value;
                        must.DatPo = DatPo.Value;

                        if (finder.nzp_type == (int)CounterKinds.Dom) must.Reason = MustCalcReasons.DomCounter;
                        else must.Reason = MustCalcReasons.Counter;

                        must.Kod2 = 0;
                        must.NzpUser = finder.nzp_user;
                        must.Comment = finder.comment_action;

                        DbMustCalcNew db = new DbMustCalcNew(connection);

                        if (finder.nzp_type == (int)CounterKinds.Kvar)
                        {
                            ret = db.InsertReason(finder.pref + "_data", must);
                        }
                        else if (finder.nzp_type == (int)CounterKinds.Dom)
                        {
                            ret = db.InsertReasonDomCounter(finder.pref + "_data", must, counter.nzp);
                        }
                        else
                        {
                            ret = db.InsertReasonGroupCounter(finder.pref + "_data", must, counter.nzp_counter);
                        }

                        if (!ret.result) throw new Exception(ret.text);

                        #region вставка признака перерасчета по связанным услугам

                        var slave_services = new List<int>();
                        //-------------------------------------------------------------------------------------------------------------------------------
                        // определение связанных услуг
                        sql = String.Format("SELECT DISTINCT nzp_serv_slave FROM {0}_data{4}dep_servs " +
                            "WHERE nzp_serv = {1} AND is_actual = 1 " +
                            "AND '{2}' >= dat_s AND '{3}' < dat_po and nzp_dep = 1", Points.Pref, counter.nzp_serv, DatS.Value.ToShortDateString(), DatPo.Value.ToShortDateString(), DBManager.tableDelimiter);

                        IntfResultTableType rt = ClassDBUtils.OpenSQL(sql, connection);
                        if (rt.resultCode < 0) throw new Exception(rt.resultMessage);
                        if (rt.resultData.Rows.Count > 0)
                        {
                            for (int i = 0; i < rt.resultData.Rows.Count; i++)
                            {
                                slave_services.Add(CastValue<int>(rt.resultData.Rows[i][0]));
                            }
                        }

                        string where_nzp_kvar = "";
                        if (finder.nzp_type == (int)CounterKinds.Kvar)
                        {
                            where_nzp_kvar = must.NzpKvar.ToString();
                        }
                        else if (finder.nzp_type == (int)CounterKinds.Dom)
                        {
                            where_nzp_kvar = "SELECT nzp_kvar FROM " + finder.pref + sDataAliasRest + "kvar  where nzp_dom = " + counter.nzp;
                        }
                        else
                        {
                            where_nzp_kvar = "SELECT nzp_kvar FROM " + finder.pref + sDataAliasRest + "counters_link  WHERE nzp_counter = " + counter.nzp_counter;
                        }


                        //получаем список активных услуг в период перерасчета для ЛС или списка ЛС
                        var active_services = new List<int>();
                        sql = " SELECT DISTINCT nzp_serv from " + finder.pref + sDataAliasRest + "tarif " +
                              " WHERE is_actual<>100 AND " + Utils.EStrNull(must.DatS.ToShortDateString()) +
                              "<=dat_po AND dat_s<=" +
                              Utils.EStrNull(must.DatPo.ToShortDateString()) +
                              " AND nzp_kvar IN (" + where_nzp_kvar + ")";
                        var dt = ClassDBUtils.OpenSQL(sql, connection).resultData;
                        if (dt.Rows.Count > 0)
                        {
                            for (int i = 0; i < dt.Rows.Count; i++)
                            {
                                active_services.Add(CastValue<int>(dt.Rows[i][0]));
                            }
                        }

                        //получаем активные(в периоде перерасчета) связанные услуги
                        slave_services = slave_services.Intersect(active_services).ToList();

                        // вставка признка перерасчета по связанным услугам
                        for (int i = 0; i < slave_services.Count; i++)
                        {
                            must.NzpServ = Convert.ToInt32(slave_services[i]);

                            if (finder.nzp_type == (int)CounterKinds.Kvar)
                            {
                                ret = db.InsertReason(finder.pref + "_data", must);
                            }
                            else if (finder.nzp_type == (int)CounterKinds.Dom)
                            {
                                ret = db.InsertReasonDomCounter(finder.pref + "_data", must, counter.nzp);
                            }
                            else
                            {
                                ret = db.InsertReasonGroupCounter(finder.pref + "_data", must, counter.nzp_counter);
                            }

                            if (!ret.result) throw new Exception(ret.text);
                        }
                        //-------------------------------------------------------------------------------------------------------------------------------
                        #endregion
                    }
                    //----------------------------------------------------------------------------------------------------
                    #endregion
                }
                //----------------------------------------------------------------------------------------------------
                #endregion

                #region сохранение показаний в банк данных
                //----------------------------------------------------------------------------------------------------
                // код события
                int nzp_dict_event = 0;
                // описание события
                string comment = "";

                if (finder.nzp_key == 0)
                {
                    nzp_dict_event = 6489;
                    comment = "Показание ПУ: " + finder.val_cnt_s + " добавлено, дата учета: " + finder.dat_uchet;

                    #region вставка
                    //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
                    if (finder.nzp_type == (int)CounterKinds.Kvar)
                    {
                        sql = " Insert into " + tab_cnt + " (nzp_counter, ist, dat_uchet, val_cnt, is_actual, nzp_user, dat_when " + revalFieldsForInsert + "," +
                            // параметры индивидуального ПУ
                            "   nzp_kvar, num_ls, nzp_serv, nzp_cnttype, num_cnt " +
                            // расход на нежилые помещения
                            (Points.IsIpuHasNgpCnt ? ", ngp_cnt " : "") +
                            " ) Values (" +
                            finder.nzp_counter + "," + finder.ist + "," + Utils.EStrNull(finder.dat_uchet) + "," + finder.val_cnt_s + ",1, " + finder.nzp_user + "," + DBManager.sCurDate + revalValuesForInsert + "," +
                            // параметры индивидуального ПУ
                            counter.nzp + "," + counter.num_ls + "," + counter.nzp_serv + "," + counter.nzp_cnttype + "," + Utils.EStrNull(counter.num_cnt, "") +
                            // расход на нежилые помещения
                            (Points.IsIpuHasNgpCnt ? "," + finder.ngp_cnt : "") + ")";
                    }
                    else if (finder.nzp_type == (int)CounterKinds.Group || finder.nzp_type == (int)CounterKinds.Communal)
                    {
                        sql = " Insert into " + tab_cnt + " (nzp_counter, ist, dat_uchet, val_cnt, is_actual, nzp_user, dat_when " + revalFieldsForInsert + "," +
                            // параметры группового ПУ
                            "   is_pl, is_gkal, ngp_cnt) " +
                            " Values (" + finder.nzp_counter + "," + finder.ist + "," + Utils.EStrNull(finder.dat_uchet) + "," + finder.val_cnt_s + ",1, " + finder.nzp_user + "," + DBManager.sCurDate + revalValuesForInsert + "," +
                            // параметры группового ПУ
                            counter.is_pl + "," + (counter.is_gkal > -1 ? counter.is_gkal.ToString() : "null") + "," + finder.ngp_cnt + ")";
                    }
                    else if (finder.nzp_type == (int)CounterKinds.Dom)
                    {
                        decimal sum_otopl = CalcArea(finder, connection, null, finder.pref, counter.nzp, out ret);
                        if (!ret.result) throw new Exception(ret.text);

                        sql = " Insert into " + tab_cnt + " (nzp_counter, ist, dat_uchet, val_cnt, is_actual, nzp_user, dat_when " + revalFieldsForInsert + "," +
                            // параметры домового ПУ
                            "   ngp_cnt, ngp_lift, " +
                            "   nzp_dom, nzp_serv, nzp_measure, nzp_cnttype, num_cnt, " +
                            "   is_pl, is_gkal, sum_otopl) " +
                            " Values (" + finder.nzp_counter + "," + finder.ist + "," + Utils.EStrNull(finder.dat_uchet) + "," + finder.val_cnt_s + ",1, " + finder.nzp_user + "," + DBManager.sCurDate + revalValuesForInsert + "," +
                            // параметры домового ПУ
                            finder.ngp_cnt + "," + finder.ngp_lift + "," +
                            counter.nzp + "," + counter.nzp_serv + "," +
                            counter.nzp_measure + "," + counter.nzp_cnttype + "," + Utils.EStrNull(counter.num_cnt, "") + "," +
                            (counter.is_pl > -1 ? counter.is_pl.ToString() : "null") + "," + (counter.is_gkal > -1 ? counter.is_gkal.ToString() : "null") + "," + sum_otopl + ")";
                    }
                    //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
                    #endregion
                }
                else if (finder.nzp_key > 0 && finder.val_cnt_s.Trim() != "")
                {
                    nzp_dict_event = 6491;
                    comment = "Показание ПУ: " + finder.val_cnt_s + " изменено, дата учета: " + finder.dat_uchet;

                    #region обновление
                    //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
                    sql = " Update " + tab_cnt + " set dat_uchet = " + Utils.EStrNull(finder.dat_uchet) + ", val_cnt = " + finder.val_cnt_s;

                    if (finder.nzp_type == (int)CounterKinds.Kvar)
                    {
                        if (Points.IsIpuHasNgpCnt) sql += ", ngp_cnt = " + finder.ngp_cnt;
                    }
                    else if (finder.nzp_type == (int)CounterKinds.Dom || finder.nzp_type == (int)CounterKinds.Group || finder.nzp_type == (int)CounterKinds.Communal)
                    {
                        sql += ", ngp_cnt = " + finder.ngp_cnt;
                        if (finder.nzp_type == (int)CounterKinds.Dom) sql += ", ngp_lift = " + finder.ngp_lift;
                    }

                    if (finder.nzp_type == (int)CounterKinds.Kvar || finder.nzp_type == (int)CounterKinds.Communal)
                        sql += ", ist = " + finder.ist;
                    sql += revalFieldsForUpdate + ", nzp_user = " + finder.nzp_user + ", dat_when = " + DBManager.sCurDate +
                    " where " + nzp_key + " = " + finder.nzp_key;
                    //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
                    #endregion
                }
                else if (finder.nzp_key > 0 && finder.val_cnt_s.Trim() == "")
                {
                    nzp_dict_event = 6490;
                    comment = "Показание ПУ: " + finder.val_cnt_s + " удалено, дата учета: " + finder.dat_uchet;

                    // удаление
                    sql = " Update " + tab_cnt + " Set is_actual = 100, user_del = " + finder.nzp_user + ", dat_del = " + sCurDateTime + revalFieldsForUpdate +
                        " where " + nzp_key + " = " + finder.nzp_key;
                }

                ret = ExecSQL(connection, sql, true);
                if (!ret.result) return ret;

                //контрольные показания
                if ((finder.nzp_type == (int)CounterKinds.Kvar || finder.nzp_type == (int)CounterKinds.Communal) && Points.isUchetContrPu)
                {
                    sql = "";
                    if (finder.nzp_key == 0 && finder.ist == (int)CounterVal.Ist.Controller)//было добавлено
                    {
                        int newNzpKey = GetSerialValue(connection);
                        sql = " insert into " + finder.pref + "_data" + tableDelimiter + "counters_comment (is_actual, nzp_cr, comment, nzp_cnttype) " +
                              " values (1, " + newNzpKey + ", '" + finder.comment_new + "'," + finder.nzp_type + ")";
                    }
                    else
                    {
                        if (finder.nzp_key > 0 && finder.val_cnt_s.Trim() == "")//было удаление
                        {
                            sql = " Update " + finder.pref + "_data" + tableDelimiter + "counters_comment " +
                                  "Set is_actual = 0 where nzp_cr = " + finder.nzp_key + " and nzp_cnttype = " + finder.nzp_type;
                        }
                        else //изменение
                        {
                            if (finder.ist == (int)CounterVal.Ist.Controller)
                            {
                                sql = " Update " + finder.pref + "_data" + tableDelimiter + "counters_comment Set " +
                                " comment = '" + finder.comment_new + "'" +
                                " where nzp_cr = " + finder.nzp_key + " and nzp_cnttype = " + finder.nzp_type;
                            }
                            else
                            {
                                sql = " Update " + finder.pref + "_data" + tableDelimiter + "counters_comment " +
                                  "Set is_actual = 0 where nzp_cr = " + finder.nzp_key + " and nzp_cnttype = " + finder.nzp_type;
                            }
                        }
                    }
                    ret = ExecSQL(connection, sql, true);
                    if (!ret.result) return ret;
                }

                if (nzp_dict_event > 0)
                {
                    InsertSysEvent(new SysEvents()
                    {
                        pref = finder.pref,
                        nzp_user = finder.nzp_user_web,
                        nzp_dict = nzp_dict_event,
                        nzp_obj = finder.nzp_counter,
                        note = comment
                    }, null, connection);
                }
                //----------------------------------------------------------------------------------------------------
                #endregion

                #region дублирование показаний в counters_vals
                //----------------------------------------------------------------------------------------------------
                string counters_vals = finder.pref + "_charge_" + (finder.year_ % 100).ToString("00") + DBManager.tableDelimiter + "counters_vals";

                string _where = " and dat_uchet = " + Utils.EStrNull(finder.dat_uchet) +
                        "   and nzp_counter = " + finder.nzp_counter +
                        "   and ist = " + finder.ist +
                        "   and month_ = " + finder.month_;

                if (finder.val_cnt_s != "")
                {
                    // проверка: на дату есть показание ПУ от указанного источника в указанном месяце
                    sql = " select count(*) as cnt from " + counters_vals +
                        " where 1=1 " + _where;

                    count = ExecScalar(connection, sql, out ret, true);
                    if (!ret.result) throw new Exception(ret.text);
                    cnt = 0;

                    try
                    { cnt = Convert.ToInt32(count); }
                    catch
                    { cnt = 0; }

                    if (cnt > 0)
                    {
                        #region обновление
                        sql = " Update " + counters_vals + " set dat_uchet = " + Utils.EStrNull(finder.dat_uchet) +
                            ", val_cnt = " + finder.val_cnt_s +
                            ", ngp_cnt = " + finder.ngp_cnt +
                            ", ngp_lift = " + finder.ngp_lift +
                            ", nzp_user = " + finder.nzp_user +
                            ", dat_when = " + DBManager.sCurDate +
                        " where 1 = 1 " + _where;
                        #endregion
                    }
                    else
                    {
                        #region вставка
                        sql = " Insert into " + counters_vals + " (nzp, nzp_type, nzp_counter, month_, dat_uchet, val_cnt, " +
                            "   ngp_cnt, ngp_lift, nzp_user, dat_when, ist) Values (" +
                            counter.nzp + "," + finder.nzp_type + "," + finder.nzp_counter + "," + finder.month_ + "," + Utils.EStrNull(finder.dat_uchet) + "," + finder.val_cnt_s + "," +
                            finder.ngp_cnt + "," + finder.ngp_lift + "," + finder.nzp_user + "," + DBManager.sCurDate + "," + finder.ist + ")";
                        #endregion
                    }
                }
                else
                {
                    // удаление
                    sql = " Update " + counters_vals + " Set val_cnt = null, ngp_cnt = null, ngp_lift = null, is_new = 1 where 1=1 " + _where;
                }

                ret = ExecSQL(connection, sql, true);
                if (!ret.result) return ret;
                //----------------------------------------------------------------------------------------------------
                #endregion

                return ret;
            }
            catch (Exception ex)
            {
                if (reader != null) reader.Close();
                return new Returns(false, ex.Message);
            }
        }

        /// <summary> 
        /// Получить предыдущее показание
        /// </summary>
        protected CounterValLight GetPrevCounterVal(IDbConnection connection, string tab_cnt, string nzp_key, CounterValLight finder, out Returns ret)
        {
            IDataReader reader = null;
            ret = new Returns();

            try
            {
                string lastDatUchet = "";

                // получить наибольшую дату последнего учтенного показания, которое меньше даты текущего показания finder
                string sql = "select max(dat_uchet) as dat_uchet from " + tab_cnt +
                    " where nzp_counter = " + finder.nzp_counter +
                    "   and dat_uchet < " + Utils.EStrNull(finder.dat_uchet) +
                    "   and is_actual <> 100 " +
                    "   and ist <> " + finder.ist;

                ret = ExecRead(connection, out reader, sql, true);
                if (!ret.result) throw new Exception(ret.text);

                if (reader.Read())
                {
                    if (reader["dat_uchet"] != DBNull.Value) lastDatUchet = Convert.ToDateTime(reader["dat_uchet"]).ToShortDateString();
                }
                else
                {
                    return null;
                }
                if (lastDatUchet == "")
                {
                    return null;
                }

                // получить код последнего учтенного показания на дату lastDatUchet
                sql = "select max(" + nzp_key + ") as nzp_cv from " + tab_cnt +
                    " where nzp_counter = " + finder.nzp_counter +
                    "   and dat_uchet = '" + lastDatUchet + "'" +
                    "   and is_actual <> 100 " +
                    "   and ist <> " + finder.ist;
                ret = ExecRead(connection, out reader, sql, true);
                if (!ret.result) throw new Exception(ret.text);

                int nzp_cv = 0;
                if (reader.Read())
                {
                    if (reader["nzp_cv"] != DBNull.Value) nzp_cv = Convert.ToInt32(reader["nzp_cv"]);
                }

                // получить значение и дату показания с кодом nzp_cv
                CounterValLight prevVal = null;

                if (nzp_cv > 0)
                {
                    sql = "select val_cnt, dat_uchet from " + tab_cnt + " where " + nzp_key + " = " + nzp_cv;
                    ret = ExecRead(connection, out reader, sql, true);
                    if (!ret.result) throw new Exception(ret.text);

                    if (reader.Read())
                    {
                        prevVal = new CounterValLight();
                        prevVal.nzp_key = nzp_cv;

                        if (reader["val_cnt"] != DBNull.Value) prevVal.val_cnt = Convert.ToDecimal(reader["val_cnt"]);
                        if (reader["dat_uchet"] != DBNull.Value) prevVal.dat_uchet = Convert.ToDateTime(reader["dat_uchet"]).ToShortDateString();
                    }
                }

                return prevVal;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                return null;
            }
        }

        /// <summary> 
        /// Алгоритм подготовки данных при вводе контрольного показания
        /// </summary>
        protected Returns PrepareDataForControlValCnt(IDbConnection connection, CounterValLight finder, string tab_cnt, string nzp_key)
        {
            Returns ret = new Returns();
            IDataReader reader = null;

            try
            {
                #region проверить, что установлена параметр "Включить алгоритм учета контрольных показаний"
                //----------------------------------------------------------------------------------------------------
                //string sql = "select count(*) as cnt from " + Points.Pref + "_data" + DBManager.tableDelimiter + "prm_10 " +
                //    " where nzp_prm = 1368 " +
                //    "   and is_actual = 1 " +
                //    "   and trim(val_prm) = '1' " +
                //    "   and " + Utils.EStrNull(finder.dat_uchet) + " between dat_s and dat_po ";
                //object obj = ExecScalar(connection, sql, out ret, true);
                //if (!ret.result) throw new Exception("Ошибка при считывании параметра! " + ret.text);

                //int cnt = 0;

                //try
                //{ cnt = Convert.ToInt32(obj); }
                //catch { cnt = 0; }

                //// параметр не установлен
                //if (cnt == 0) return ret;
                if (!Points.isUchetContrPu) return ret;
                //----------------------------------------------------------------------------------------------------
                #endregion

                #region Удалить показания на дату контрольного показания
                //----------------------------------------------------------------------------------------------------------------------------------------------------------
                //Удаление показаний
                string sql = " Update " + tab_cnt + " Set is_actual = 100, user_del = " + finder.nzp_user + ", dat_del = " + sCurDateTime +
                    " where nzp_counter = " + finder.nzp_counter +
                    "   and ist <> " + (int)CounterVal.Ist.Controller +
                    "   and dat_uchet = " + Utils.EStrNull(finder.dat_uchet) +
                    "   and " + nzp_key + " <> " + finder.nzp_key;
                ret = ExecSQL(connection, sql, true);
                if (!ret.result) return ret;

                string note = " дата учета: " + finder.dat_uchet + " удалено из-за ввода контрольного показания: " + finder.val_cnt + ", дата учета: " + finder.dat_uchet;

                // Вставка записей в историю изменений
                sql = " insert into " + finder.pref + "_data" + DBManager.tableDelimiter + "sys_events(DATE_, NZP_USER, NZP_DICT_EVENT, NZP, NOTE) " +
                    " select " + Utils.EStrNull(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")) + "," + finder.nzp_user + ", 6490, " + finder.nzp_counter +
                    ", 'Показание ПУ: ' || a.val_cnt || " + Utils.EStrNull(note) +
                    " from " + tab_cnt + " a " +
                    " where a.nzp_counter = " + finder.nzp_counter +
                    "   and a.ist <> " + (int)CounterVal.Ist.Controller +
                    "   and a.dat_uchet = " + Utils.EStrNull(finder.dat_uchet) +
                    "   and a." + nzp_key + " <> " + finder.nzp_key;
                ret = ExecSQL(connection, sql, true);
                if (!ret.result) return ret;

                //----------------------------------------------------------------------------------------------------------------------------------------------------------
                #endregion

                // 2. Получить последнее показание
                // 2.1. Получить ключ последнего показания
                CounterValLight prevVal = GetPrevCounterVal(connection, tab_cnt, nzp_key, finder, out ret);
                if (!ret.result) return ret;

                // показание найдено
                if (prevVal != null)
                {
                    // значение последнего показания больше значения контрольного показания
                    decimal val = 0;
                    Decimal.TryParse(finder.val_cnt_s, out val);
                    finder.val_cnt = val;
                    if (prevVal.val_cnt > finder.val_cnt)
                    {
                        // вычислить расход
                        finder.val_cnt_pred = prevVal.val_cnt;
                        finder.dat_uchet_pred = prevVal.dat_uchet;

                        Double rashod = finder.calculatedRashod;

                        // инициализировать поле max_rashod для расчета половины диапазона значений
                        finder.max_rashod = 0;

                        // если расход больше половины диапазона
                        if (rashod > finder.GetMaxValue)
                        {
                            #region получить список предыдущих показаний
                            //---------------------------------------------------------------------------------------
                            sql = "select " + nzp_key + " as nzp_key, val_cnt, ist, dat_uchet from " + tab_cnt +
                                " where nzp_counter = " + finder.nzp_counter +
                                "   and dat_uchet < " + Utils.EStrNull(finder.dat_uchet) +
                                "   and is_actual <> 100 ";

                            ExecRead(connection, out reader, sql, true);
                            if (!ret.result) throw new Exception(ret.text);

                            List<CounterValLight> prevValList = new List<CounterValLight>();

                            while (reader.Read())
                            {
                                prevVal = new CounterValLight();

                                if (reader["nzp_key"] != DBNull.Value) prevVal.nzp_key = Convert.ToInt32(reader["nzp_key"]);
                                if (reader["val_cnt"] != DBNull.Value) prevVal.val_cnt = Convert.ToDecimal(reader["val_cnt"]);
                                if (reader["val_cnt"] != DBNull.Value) prevVal.val_cnt = Convert.ToDecimal(reader["val_cnt"]);
                                if (reader["dat_uchet"] != DBNull.Value) prevVal.dat_uchet = Convert.ToDateTime(reader["dat_uchet"]).ToShortDateString();
                                if (reader["ist"] != DBNull.Value) prevVal.ist = Convert.ToInt32(reader["ist"]);
                                prevValList.Add(prevVal);
                            }
                            //---------------------------------------------------------------------------------------
                            #endregion

                            #region проверить есть ли в списке контрольные показания, которые больше сохраняемого контрольного показания
                            //---------------------------------------------------------------------------------------
                            for (int i = 0; i < prevValList.Count; i++)
                            {
                                if (prevValList[i].ist == (int)CounterVal.Ist.Controller && prevValList[i].val_cnt > finder.val_cnt)
                                {
                                    throw new Exception("Контрольное показание " + finder.val_cnt + " на дату " + finder.dat_uchet +
                                        " не может быть сохранено, т.к. найдено другое контрольное показание " + prevValList[i].val_cnt + " на дату " + prevValList[i].dat_uchet + ", которое больше сохраняемого");
                                }
                            }
                            //---------------------------------------------------------------------------------------
                            #endregion

                            #region удаление предыдущих показаний
                            //---------------------------------------------------------------------------------------
                            for (int i = 0; i < prevValList.Count; i++)
                            {
                                if (prevValList[i].ist != (int)CounterVal.Ist.Controller && prevValList[i].val_cnt > finder.val_cnt)
                                {
                                    // Удаление показаний
                                    sql = " Update " + tab_cnt + " Set is_actual = 100, user_del = " + finder.nzp_user + ", dat_del = " + sCurDateTime +
                                        " where " + nzp_key + " = " + prevValList[i].nzp_key;
                                    ret = ExecSQL(connection, sql, true);
                                    if (!ret.result) return ret;

                                    note = "Показание ПУ: " + prevValList[i].val_cnt + ", дата учета: " + finder.dat_uchet +
                                        " удалено из-за ввода контрольного показания: " + finder.val_cnt + ", дата учета: " + finder.dat_uchet;

                                    // Вставка записей в историю изменений
                                    sql = " insert into " + finder.pref + "_data" + DBManager.tableDelimiter + "sys_events(DATE_, NZP_USER, NZP_DICT_EVENT, NZP, NOTE) values " +
                                        " (" + Utils.EStrNull(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")) + "," + finder.nzp_user + ", 6490, " + finder.nzp_counter + ", " + Utils.EStrNull(note) + ") ";
                                    ret = ExecSQL(connection, sql, true);
                                    if (!ret.result) return ret;
                                }
                            }
                            //---------------------------------------------------------------------------------------
                            #endregion
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (reader != null) reader.Close();
                return new Returns(false, ex.Message);
            }

            return ret;
        }

        /// <summary> 
        /// Подготовка данных при режиме: Сохранить весь расход в текущем расчетном месяце
        /// </summary>
        protected Returns PrepareDataForSaveRashodInCurPeriod(IDbConnection connection, CounterValLight finder, string tab_cnt, string nzp_key)
        {
            var ret = new Returns();
            IDataReader reader = null;

            try
            {
                DateTime dateUchet;
                DateTime.TryParse(finder.dat_uchet, out dateUchet);
                var prevDateUchet1 = new DateTime(dateUchet.AddMonths(-1).Year, dateUchet.AddMonths(-1).Month, 1);
                var prevDateUchet2 = new DateTime(prevDateUchet1.Year, prevDateUchet1.Month, DateTime.DaysInMonth(prevDateUchet1.Year, prevDateUchet1.Month));

                #region проверить, что в предыдущем месяце не было показаний
                var sql = "select count(*) from " + tab_cnt +
                             " where nzp_counter = " + finder.nzp_counter +
                             "   and dat_uchet between " + Utils.EStrNull(prevDateUchet1.ToShortDateString()) +
                             "      and " + Utils.EStrNull(prevDateUchet2.ToShortDateString()) +
                             "   and is_actual <> 100";
                var obj = ExecScalar(connection, sql, out ret, true);
                if (!ret.result) return ret;

                int cnt;
                Int32.TryParse(obj.ToString(), out cnt);
                if (cnt > 0) return ret;
                #endregion

                // получить дату последнего учтенного показания
                sql = "select max(dat_uchet) as dat_uchet from " + tab_cnt +
                    " where nzp_counter = " + finder.nzp_counter +
                    "   and dat_uchet < " + Utils.EStrNull(finder.dat_uchet) +
                    "   and is_actual <> 100 ";
                ret = ExecRead(connection, out reader, sql, true);
                if (!ret.result) return ret;
                var lastDatUchet = "";
                if (reader.Read())
                {
                    if (reader["dat_uchet"] != DBNull.Value) lastDatUchet = Convert.ToDateTime(reader["dat_uchet"]).ToShortDateString();
                }
                else return ret;
                if (lastDatUchet == "") return ret;

                // получить код последнего учтенного показания на дату lastDatUchet
                sql = "select max(" + nzp_key + ") as nzp_cv from " + tab_cnt +
                    " where nzp_counter = " + finder.nzp_counter +
                    "   and dat_uchet = '" + lastDatUchet + "'" +
                    "   and is_actual <> 100 ";
                ret = ExecRead(connection, out reader, sql, true);
                if (!ret.result) throw new Exception(ret.text);
                var nzpCv = 0;
                if (reader.Read())
                {
                    if (reader["nzp_cv"] != DBNull.Value) nzpCv = Convert.ToInt32(reader["nzp_cv"]);
                }
                if (nzpCv <= 0) return ret;

                if (finder.nzp_type == (int)CounterKinds.Kvar)
                {
                    sql = " Insert into " + tab_cnt + " (nzp_counter, ist, dat_uchet, val_cnt, is_actual, nzp_user, dat_when," +
                          "   nzp_kvar, num_ls, nzp_serv, nzp_cnttype, num_cnt " + (Points.IsIpuHasNgpCnt ? ", ngp_cnt " : "") + " ) " +
                          " select nzp_counter, " + CounterVal.Ist.Program.GetHashCode() + ", " + Utils.EStrNull(prevDateUchet1.ToShortDateString()) + ", val_cnt, 1, " + finder.nzp_user +
                          " , now(), nzp_kvar, num_ls, nzp_serv, nzp_cnttype, num_cnt " + (Points.IsIpuHasNgpCnt ? ", ngp_cnt " : "") +
                          " from " + tab_cnt + " where " + nzp_key + " = " + nzpCv;
                }
                else if (finder.nzp_type == (int)CounterKinds.Group || finder.nzp_type == (int)CounterKinds.Communal)
                {
                    sql = " Insert into " + tab_cnt + " (nzp_counter, ist, dat_uchet, val_cnt, is_actual, nzp_user, dat_when," +
                            " is_pl, is_gkal, ngp_cnt) " +
                            " select nzp_counter, " + CounterVal.Ist.Program.GetHashCode() + ", " + Utils.EStrNull(prevDateUchet1.ToShortDateString()) + ", val_cnt, 1, " + finder.nzp_user +
                            " , now(), is_pl, is_gkal, ngp_cnt" +
                            " from " + tab_cnt + " where " + nzp_key + " = " + nzpCv;
                }
                else if (finder.nzp_type == (int)CounterKinds.Dom)
                {
                    sql = " Insert into " + tab_cnt + " (nzp_counter, ist, dat_uchet, val_cnt, is_actual, nzp_user, dat_when," +
                        "   ngp_cnt, ngp_lift, nzp_dom, nzp_serv, nzp_measure, nzp_cnttype, num_cnt, is_pl, is_gkal, sum_otopl) " +
                        " select nzp_counter, " + CounterVal.Ist.Program.GetHashCode() + ", " + Utils.EStrNull(prevDateUchet1.ToShortDateString()) + ", val_cnt, 1, " + finder.nzp_user +
                        " , now(), ngp_cnt, ngp_lift, nzp_dom, nzp_serv, nzp_measure, nzp_cnttype, num_cnt, is_pl, is_gkal, sum_otopl " +
                        " from " + tab_cnt + " where " + nzp_key + " = " + nzpCv;
                }
                ret = ExecSQL(connection, sql, true);
                if (!ret.result) return ret;

                var note = "Показание ПУ: дата учета: " + prevDateUchet1.ToShortDateString() +
                    " сгенерированно программой из-за установленного признака Сохранить весь расход в текущем расчетном месяце";

                // Вставка записей в историю изменений
                sql = " insert into " + finder.pref + "_data" + DBManager.tableDelimiter + "sys_events(DATE_, NZP_USER, NZP_DICT_EVENT, NZP, NOTE) values " +
                    " (" + Utils.EStrNull(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")) + "," + finder.nzp_user + ", 6489, " + finder.nzp_counter + ", " + Utils.EStrNull(note) + ") ";
                ret = ExecSQL(connection, sql, true);
                if (!ret.result) return ret;
            }
            catch (Exception ex)
            {
                if (reader != null) reader.Close();
                return new Returns(false, ex.Message);
            }

            return ret;
        }

        /// <summary> Получить краткую информацию о показании ПУ
        /// </summary>
        protected CounterVal GetCounterVal(IDbConnection connection, IDbTransaction transaction, CounterVal finder, out Returns ret)
        {
            string counters_vals = finder.pref + "_charge_" + (finder.year_ % 100).ToString("00") + DBManager.tableDelimiter + "counters_vals";
            string counters_spis = finder.pref + "_data" + DBManager.tableDelimiter + "counters_spis";
            string s_counts = finder.pref + "_kernel" + DBManager.tableDelimiter + "s_counts";

            if (!TempTableInWebCashe(connection, transaction, counters_vals))
            {
                ret = new Returns(false, "Таблица с показаниями не найдена");
                return null;
            }

            string sql = "";

            if (finder.nzp_cv > 0)
            {
                sql = " SELECT a.nzp_counter, a.ist, a.month_, a.dat_uchet, a.val_cnt, a.ngp_cnt, a.ngp_lift, c.nzp_measure, b.nzp_type" +
                    " FROM " + counters_vals + " a, " + counters_spis + " b left outer join " + s_counts + " c on b.nzp_cnt = c.nzp_cnt" +
                    " WHERE a.nzp_cv = " + finder.nzp_cv + " AND a.nzp_counter = b.nzp_counter";
            }
            else
            {
                sql = " SELECT b.nzp_counter, " + finder.ist + " as ist, " + finder.month_ + " as month_, " + Utils.EStrNull(finder.dat_uchet) + " as dat_uchet, " +
                    finder.val_cnt + " as val_cnt, " + finder.ngp_cnt + " as ngp_cnt, " + finder.ngp_lift + " as ngp_lift, c.nzp_measure, b.nzp_type " +
                    " FROM " + counters_spis + " b " +
                    "   left outer join " + s_counts + " c on b.nzp_cnt = c.nzp_cnt " +
                    " WHERE b.nzp_counter = " + finder.nzp_counter;
            }

            MyDataReader reader;
            ret = ExecRead(connection, transaction, out reader, sql, true);
            if (!ret.result) return null;

            if (!reader.Read())
            {
                ret.result = false;
                ret.text = "Показание не найдено";
                return null;
            }

            try
            {
                CounterVal counter = new CounterVal();
                counter.nzp_cv = finder.nzp_cv;
                if (reader["nzp_type"] != DBNull.Value) counter.nzp_type = Convert.ToInt32(reader["nzp_type"]);
                if (reader["nzp_counter"] != DBNull.Value) counter.nzp_counter = Convert.ToInt32(reader["nzp_counter"]);
                if (reader["dat_uchet"] != DBNull.Value) counter.dat_uchet = Convert.ToDateTime(reader["dat_uchet"]).ToShortDateString();
                if (reader["ist"] != DBNull.Value) counter.ist = Convert.ToInt32(reader["ist"]);
                if (reader["val_cnt"] != DBNull.Value) counter.val_cnt_s = Convert.ToDecimal(reader["val_cnt"]).ToString();
                if (reader["ngp_cnt"] != DBNull.Value) counter.ngp_cnt = Convert.ToDecimal(reader["ngp_cnt"]);
                if (reader["nzp_measure"] != DBNull.Value) counter.nzp_measure = Convert.ToInt32(reader["nzp_measure"]);
                if (reader["ngp_lift"] != DBNull.Value) counter.ngp_lift = Convert.ToDecimal(reader["ngp_lift"]);

                return counter;
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                return null;
            }
            finally
            {
                reader.Close();
            }
        }

        private decimal CalcArea(CounterValLight newVal, IDbConnection conn_db, IDbTransaction transaction_id, string pref, long nzp_dom, out Returns ret)
        {
            ret = Utils.InitReturns();
            DateTime datUchet = Convert.ToDateTime(newVal.dat_uchet);
            DateTime dat_s = new DateTime(datUchet.Year, datUchet.Month, 1).AddMonths(-1);
            DateTime dat_po = new DateTime(datUchet.Year, datUchet.Month, 1).AddDays(-1);
            try
            {
                var dt =
                    ClassDBUtils.OpenSQL(
                        "select count(*) as cnt from " + pref + "_data" + tableDelimiter +
                        "prm_10 where nzp_prm = 1288 and is_actual <> 100 " +
                        " and " + Utils.EStrNull(dat_po.ToShortDateString()) + " >=dat_s " +
                        " and " + Utils.EStrNull(dat_s.ToShortDateString()) + " <= dat_po" +
                        " and val_prm = '1'",
                        conn_db, transaction_id);

                if (dt.resultCode < 0) throw new Exception(dt.resultMessage);

                if (dt.resultData.Rows[0]["cnt"].ToString().Trim() != "0")
                {
                    string str = " SELECT sum(replace(val_prm,\",\",\".\")" + DBManager.sConvToNum + ") sum_pl " +
                         " FROM " + pref + "_data" + tableDelimiter + "prm_1 WHERE nzp_prm=133 and is_actual<>100 " +
                         " and '" + dat_po.ToString("dd.MM.yyyy") + "' >=dat_s and '" + dat_s.ToString("dd.MM.yyyy") + "' <= dat_po " +
                         " and nzp in (select distinct t.nzp_kvar from " + pref + "_data" + tableDelimiter + "tarif t," + pref + "_data" + tableDelimiter + "kvar k,"
                         + pref + "_data" + tableDelimiter + "dom d " +
                         " WHERE nzp_serv = 8 and is_actual<>100 and t.nzp_kvar=k.nzp_kvar and k.nzp_dom=d.nzp_dom and d.nzp_dom=" + nzp_dom + " " +
                         " and '" + dat_po.ToString("dd.MM.yyyy") + "' >= dat_s and '" + dat_s.ToString("dd.MM.yyyy") + "' <=dat_po) " +
                         " and nzp in (select nzp from " + pref + "_data: prm_3 " +
                         " WHERE nzp_prm = 51 and is_actual<>100 and (val_prm = '1' or val_prm = '3'))";
                    dt = ClassDBUtils.OpenSQL(str, conn_db, transaction_id);

                    if (dt.resultCode < 0) throw new Exception(dt.resultMessage);

                    if (dt.resultData == null || dt.resultData.Rows.Count == 0) return 0m;

                    return (decimal)dt.resultData.Rows[0][0];
                }
                else return 0;
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                return 0;
            }
        }

        //распределение показаний ИПУ         
        public Returns PuValsToCountersVals(IDbConnection conn_db, IDbTransaction transaction, List<int> list_pack_ls, int nzp_user, CalcTypes.ParamCalc paramcalc)
        {

            MonitorLog.WriteLog("Вызов функции PuValsToCountersVals c параметрами : list_pack_ls count=" + list_pack_ls.Count +
                ", nzp_user =" + nzp_user + ", " + paramcalc.calc_yy + "," + paramcalc.calc_mm + "", MonitorLog.typelog.Info, true);
            Returns ret = Utils.InitReturns();
            var newCntVal = new List<CounterValLight>();
            string s_year = (paramcalc.calc_yy - 2000).ToString("00");
            DateTime next_month = new DateTime(Points.CalcMonth.year_, Points.CalcMonth.month_, 1).AddMonths(1);

            //пробегаемся по всем nzp_pack_ls
            for (int i = 0; i < list_pack_ls.Count; i++)
            {

                string sql = "select nzp_counter,num_ls, val_cnt, dat_month from " + Points.Pref + "_fin_" + s_year + tableDelimiter + "pu_vals where nzp_pack_ls = " + list_pack_ls[i] + "  ";

                var puvals = ClassDBUtils.OpenSQL(sql, conn_db, transaction);
                if (puvals.resultCode == -1)
                {
                    MonitorLog.WriteLog("Ошибка получения показаний ПУ, sql: " + sql, MonitorLog.typelog.Error, true);
                    ret.result = false;
                    ret.text = "Ошибка получения показаний ПУ";
                    return ret;
                }
                //пробегаемся по всем ПУ для данного nzp_pack_ls
                for (int j = 0; j < puvals.resultData.Rows.Count; j++)
                {
                    if (puvals.resultData.Rows[j]["num_ls"].ToString() != "" && puvals.resultData.Rows[j]["num_ls"] != DBNull.Value && puvals.resultData.Rows[j]["nzp_counter"] != DBNull.Value)
                    {
                        var cnt = new CounterValLight();
                        cnt.nzp_user = nzp_user;
                        cnt.year_ = Points.CalcMonth.year_;
                        cnt.month_ = Points.CalcMonth.month_;
                        cnt.ist = (int)CounterVal.Ist.Bank;
                        //cnt.dat_uchet = next_month.ToShortDateString();
                        cnt.dat_uchet = (puvals.resultData.Rows[j]["dat_month"] != DBNull.Value ? Convert.ToDateTime(puvals.resultData.Rows[j]["dat_month"]) : next_month).ToShortDateString();
                        cnt.num_ls = ((int)puvals.resultData.Rows[j]["num_ls"]);
                        cnt.val_cnt_s = ((decimal)puvals.resultData.Rows[j]["val_cnt"]).ToString("0.0000");
                        cnt.nzp_counter = ((int)puvals.resultData.Rows[j]["nzp_counter"]);
                        cnt.nzp_type = (int)CounterKinds.Kvar;
                        sql = "select k.pref from " + Points.Pref + "_data" + tableDelimiter + "kvar k  where k.nzp_kvar =" + cnt.num_ls + " ";
                        var pref = ClassDBUtils.OpenSQL(sql, conn_db, transaction);
                        if (pref.resultCode == -1)
                        {
                            MonitorLog.WriteLog("Ошибка получения префикса ЛС, sql: " + sql, MonitorLog.typelog.Error, true);
                            ret.result = false;
                            ret.text = "Ошибка получения данных";
                            return ret;
                        }
                        if (pref.resultData.Rows.Count > 0)
                        {
                            cnt.pref = pref.resultData.Rows[0]["pref"].ToString().Trim();
                        }

                        newCntVal.Add(cnt);
                    }
                }


            }

            MonitorLog.WriteLog("Вызов функции SaveCountersValsLight, число показаний ПУ:" + newCntVal.Count, MonitorLog.typelog.Info, true);
            //вызываем функцию переноса из pu_vals в counters_vals
            if (newCntVal.Count > 0)
            {
                var failVals = new List<CounterValLight>();
                ret = SaveCountersValsLight(newCntVal, out failVals);

                foreach (var failVal in failVals)
                {
                    var packLog = new PackLog()
                    {
                        message = "Номер лицевого счета " + failVal.num_ls + ", ПУ с кодом " + failVal.nzp_counter +
                            ",  дата учета \"" + failVal.dat_uchet + "\" :" + failVal.upload_result,
                        nzp_pack = paramcalc.nzp_pack
                    };
                    if (!MessageInPackLog(conn_db, packLog).result)
                    {
                        MonitorLog.WriteLog("Ошибка логирования распределения показаний ПУ: nzp_counter=" + failVal.nzp_counter +
                            ",val=" + failVal.val_cnt + ",upload_result=" + failVal.upload_result, MonitorLog.typelog.Error, true);
                    }
                }
            }
            return ret;
        }

        public static bool InsertSysEvent(SysEvents finder, IDbTransaction transaction, IDbConnection conn)
        {
            try
            {
                if (finder.pref == null || finder.pref == "")
                    finder.pref = Points.Pref;
                var bank = finder.bank;
                if (bank == null)
                    bank = finder.pref + "_data";

                string sSQL_Text = " insert into " + bank + tableDelimiter + "sys_events(DATE_, NZP_USER, NZP_DICT_EVENT, NZP, NOTE) " +
                    " values(" + Utils.EStrNull(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")) + "," + finder.nzp_user + "," +
                    finder.nzp_dict + "," + finder.nzp_obj + "," + Utils.EStrNull(finder.note) + ") ";

                ClassDBUtils.ExecSQL(sSQL_Text, conn, transaction);
                return true;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка добавления события в sys_events" + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                return false;
            }
        }


        /// <summary>
        /// Запись в лог пачки оплат
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        /// <param name="pLog"></param>
        /// <returns></returns>
        protected Returns MessageInPackLog(IDbConnection connection, PackLog pLog)
        {
            var schema = Points.Pref + "_fin_" + (Points.DateOper.Year - 2000).ToString("00");
            pLog.tableName = schema + tableDelimiter + "pack_log";
            if (!DBManager.TableInBase(connection, null, schema, "pack_log"))
            {
                return new Returns { result = false, text = "Таблица с логами для пачек оплат не найдена" };
            }

            var sql = " INSERT INTO " + pLog.tableName +
                      " (nzp_pack,nzp_pack_ls,dat_oper,dat_log,nzp_wp, txt_log,tip_log) " +
                      " VALUES ( " + pLog.nzp_pack + "," +
                      pLog.nzp_pack_ls + "," +
                      Utils.EStrNull(Points.DateOper.ToShortDateString()) + ", " + sCurDateTime + ", " +
                      pLog.nzp_wp + ", " +
                      Utils.EStrNull(pLog.message) + "," +
                      (pLog.err ? "1" : "0") + " ) ";

            return ExecSQL(connection, sql, true);

        }

    }
}
