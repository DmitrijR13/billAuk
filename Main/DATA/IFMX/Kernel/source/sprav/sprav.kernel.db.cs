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
    public partial class DbSpravKernel : DbSpravClient
    {
        //пример обращения для получения кодов 1,2,10 -nzp_kvar,num_ls,pkod10
        // Series series = new Series( new int[]{1,2,10} );
        // DbEditInterData.GetSeries('vas', series, out ret);
        // if (ret.result) 
        // {
        //      val  = series.GetSeries(1)
        //      if (val.cur_val != Zero) nzp_kvar = val.cur_val
        //      kod2  = series.GetSeries(2);
        //      kod10 = series.GetSeries(10);
        // }
        //----------------------------------------------------------------------
        public void GetSeries(string pref, Series series, out Returns ret)
        {
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                return;
            }

            GetSeries(conn_db, pref, series, out ret);

            conn_db.Close();
        }

        public void GetSeries(IDbConnection connection, string pref, Series series, out Returns ret)
        {
            ret = Utils.InitReturns();

            IDbTransaction trans = connection.BeginTransaction();
            GetSeries(connection, trans, pref, series, out ret);
            if (ret.result) trans.Commit();
            else trans.Rollback();
        }

        public void GetSeries(IDbConnection connection, IDbTransaction trans, string pref, Series series, out Returns ret)
        {
            if (series == null)
            {
                ret = new Returns(false, "Входной параметр series не задан");
                return;
            }


            if (Points.isUseSeries) // работа с таблицей series
            {
                GetSeriesUseSeries(connection, trans, pref, series, out ret);
            }
            else
            {
                GetSeriesWithoutSeries(connection, trans, pref, series, out ret);
            }

        }

        public void GetSeriesUseSeries(IDbConnection connection, IDbTransaction trans, string pref, Series series, out Returns ret)
        {

            /*
                        // Rust 
                        // Получить значения ключей 
                        IDataReader reader;
                        string sql = "select " + pref + "_data:get_seq_sel(kod,1) as cur_val, kod, v_min,v_max from " + pref + "_data:series where kod in ( " + series.GetStringKod() + ")";            
                        ret = ExecRead(connection, trans, out reader, sql, true);
                        if (!ret.result)
                        {
                            ret.text = "Ошибка блокирования series";
                           // ExecSQL(connection, trans, " SET ISOLATION TO DIRTY READ ", true);
                            return;
                        }
                        // все остальное как обычно
                        while (reader.Read())
                        {
                            _Series val = series.EmptyVal();
                            val = series.GetSeries((int)reader["kod"]);
                            if (val.kod == Constants._ZERO_)
                            {
                                ret.text = "Внутренняя ошибка series";
                                ret.result = false;
                                reader.Close();
                                reader.Dispose();

                                return;
                            }

                            val.v_min = (int)reader["v_min"];
                            val.v_max = (int)reader["v_max"];
                            val.cur_val = (int)reader["cur_val"];
                            series.PutVal(val);

                            if (val.getAndInc)
                            {
                                if (val.cur_val < 1 || val.cur_val < val.v_min)
                                {
                                    reader.Close();
                                    reader.Dispose();
                                    ret.text = "Значение series с кодом " + val.kod + " в банке данных " + pref + " выходит за границы допустимого диапазона";
                                    ret.result = false;
                                    if (Constants.Debug) MonitorLog.WriteLog("Функция GetSeries()\n" + ret.text, MonitorLog.typelog.Error, 2, 100, true);
                                    return;
                                }

                                if (val.cur_val > val.v_max)
                                {
                                    reader.Close();
                                    reader.Dispose();
                                    ret.text = "Лимит series с кодом " + val.kod + " в банке данных " + pref + " исчерпан";
                                    ret.result = false;
                                    if (Constants.Debug) MonitorLog.WriteLog("Функция GetSeries()\n" + ret.text, MonitorLog.typelog.Error, 2, 100, true);
                                    return;
                                }

                          //      ret = ExecSQL(connection, trans,
                          //          " Update " + pref + "_data:series " +
                          //          " Set cur_val = cur_val + 1 " +
                          //          " Where kod = " + val.kod
                          //          , true);
                                if (!ret.result)
                                {
                                    reader.Close();
                                    reader.Dispose();
                                    ret.text = "Ошибка выделения series";
                                    ret.result = false;
                                    return;
                                }
                            }
                        }
                        reader.Close();
                        reader.Dispose();


              }
                    */
            // Rust


#if PG
            ret = ExecSQL(connection, trans, "begin; SET TRANSACTION ISOLATION LEVEL READ COMMITTED; ", true);
#else
            ret = ExecSQL(connection, trans, " SET ISOLATION TO COMMITTED READ ", true);
#endif
            if (!ret.result)
            {
                ret.text = "Не удалось установить уровень изоляции на COMMITTED READ: " + ret.text;
                return;
            }

            //попытка заблокировать series
#if PG
            ret = ExecSQL(connection, trans, " Lock table " + pref + "_data.series in exclusive mode ", true);
#else
            ret = ExecSQL(connection, trans, " Lock table " + pref + "_data:series in exclusive mode ", true);
#endif
            if (!ret.result)
            {
                ret.text = "Ошибка блокирования series";
#if PG
                ExecSQL(connection, trans, "SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED ", true);
#else
                ExecSQL(connection, trans, " SET ISOLATION TO DIRTY READ ", true);
#endif
                return;
            }

            MyDataReader reader = null;

            try
            {
                //вытащим заказанные коды
#if PG
                string sql = " Select * From " + pref + "_data.series " +
#else
                string sql = " Select * From " + pref + "_data:series " +
#endif
 " Where kod in (" + series.GetStringKod() + ") Order by kod ";
                ret = ExecRead(connection, trans, out reader, sql, true);
                if (!ret.result) return;

                while (reader.Read())
                {
                    _Series val = series.EmptyVal();
                    val = series.GetSeries((int)reader["kod"]);

                    if (val.kod == Constants._ZERO_)
                    {
                        ret.text = "Внутренняя ошибка series";
                        ret.result = false;
                        return;
                    }

                    val.v_min = (int)reader["v_min"];
                    val.v_max = (int)reader["v_max"];
                    val.cur_val = (int)reader["cur_val"];

                    series.PutVal(val);

                    if (val.getAndInc)
                    {
                        if (val.cur_val < 1 || val.cur_val < val.v_min)
                        {
                            ret.text = "Значение series с кодом " + val.kod + " в банке данных " + pref + " выходит за границы допустимого диапазона";
                            ret.result = false;
                            if (Constants.Debug) MonitorLog.WriteLog("Функция GetSeries()\n" + ret.text, MonitorLog.typelog.Error, 2, 100, true);
                            return;
                        }

                        if (val.cur_val > val.v_max)
                        {
                            ret.text = "Лимит series с кодом " + val.kod + " в банке данных " + pref + " исчерпан";
                            ret.result = false;
                            if (Constants.Debug) MonitorLog.WriteLog("Функция GetSeries()\n" + ret.text, MonitorLog.typelog.Error, 2, 100, true);
                            return;
                        }

                        ret = ExecSQL(connection, trans,
#if PG
                        " Update " + pref + "_data.series " +
#else
 " Update " + pref + "_data:series " +
#endif
 " Set cur_val = cur_val + 1 " +
                            " Where kod = " + val.kod
                            , true);
                        if (!ret.result)
                        {
                            ret.text = "Ошибка выделения series";
                            ret.result = false;
                            return;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка GetSeries()\n" + ret.text, MonitorLog.typelog.Error, 2, 100, true);
            }
            finally
            {
                if (reader != null) reader.Close();
#if PG
                ExecSQL(connection, trans, " commit; SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED; ", true);
#else
                ExecSQL(connection, trans, " SET ISOLATION TO DIRTY READ ", true);
#endif
            }
        }

        public void GetSeriesWithoutSeries(IDbConnection connection, IDbTransaction trans, string pref, Series series, out Returns ret)
        {
            ret = Utils.InitReturns();
            MyDataReader reader = null;

            try
            {
                List<int> list = series.GetListKod();
                if (list == null || list.Count <= 0)
                {
                    ret.text = "Внутренняя ошибка series";
                    ret.result = false;
                    return;
                }
                string sql = "";

                //вытащим заказанные коды
#if PG
                sql = " SELECT nextval('"+Points.Pref+"_data.kvar_nzp_kvar_seq') as nzp_kvar, "+
                             " nextval('" + Points.Pref + "_data.kvar_num_ls_seq') as num_ls, " +
                             " nextval('" + Points.Pref + "_data.dom_nzp_dom_seq') as nzp_dom, " +
                             " nextval('" + Points.Pref + "_data.s_ulica_nzp_ul_seq') as nzp_ul, " +
                             " nextval('" + Points.Pref + "_data.s_geu_nzp_geu_seq') as nzp_geu, " +
                             " nextval('" + Points.Pref + "_data.s_area_nzp_area_seq') as nzp_area, " +
                             " nextval('" + Points.Pref + "_kernel.s_payer_nzp_payer_seq') as nzp_payer, " +
                             " nextval('" + Points.Pref + "_kernel.supplier_nzp_supp_seq') as nzp_supp, " +
                             " nextval('" + Points.Pref + "_data.counters_spis_nzp_counter_seq') as nzp_counter";
#else
                sql = " SELECT " + Points.Pref + "_data:kvar_nzp_kvar_seq.nextval as nzp_kvar, " +
                           Points.Pref + "_data:kvar_num_ls_seq.nextval as num_ls, " +
                           Points.Pref + "_data:dom_nzp_dom_seq.nextval as nzp_dom, " +
                           Points.Pref + "_data:s_ulica_nzp_ul_seq.nextval as nzp_ul, " +
                           Points.Pref + "_data:s_geu_nzp_geu_seq.nextval as nzp_geu, " +
                           Points.Pref + "_data:s_area_nzp_area_seq.nextval as nzp_area, " +
                           Points.Pref + "_kernel:s_payer_nzp_payer_seq.nextval as nzp_payer, " +
                           Points.Pref + "_kernel:supplier_nzp_supp_seq.nextval as nzp_supp, " +
                           Points.Pref + "_data:counters_spis_nzp_counter_seq.nextval as nzp_counter from " + Points.Pref + "_data:dual";
#endif
                ret = ExecRead(connection, trans, out reader, sql, true);
                if (!ret.result) return;

                if (reader.Read())
                {
                    foreach (int kod in list)
                    {
                        _Series val = series.EmptyVal();
                        val = series.GetSeries(kod);

                        if (val.kod == Constants._ZERO_)
                        {
                            ret.text = "Внутренняя ошибка series";
                            ret.result = false;
                            return;
                        }

                        val.v_min = 1;
                        val.v_max = 2147483647;

                        if (kod == Series.Types.Area.GetHashCode()) val.cur_val = Convert.ToInt32(reader["nzp_area"]);
                        else if (kod == Series.Types.Counter.GetHashCode()) val.cur_val = Convert.ToInt32(reader["nzp_counter"]);
                        else if (kod == Series.Types.Dom.GetHashCode()) val.cur_val = Convert.ToInt32(reader["nzp_dom"]);
                        else if (kod == Series.Types.Geu.GetHashCode()) val.cur_val = Convert.ToInt32(reader["nzp_geu"]);
                        else if (kod == Series.Types.Kvar.GetHashCode()) val.cur_val = Convert.ToInt32(reader["nzp_kvar"]);
                        else if (kod == Series.Types.NumLs.GetHashCode()) val.cur_val = Convert.ToInt32(reader["num_ls"]);
                        else if (kod == Series.Types.Payer.GetHashCode()) val.cur_val = Convert.ToInt32(reader["nzp_payer"]);
                        else if (kod == Series.Types.Supplier.GetHashCode()) val.cur_val = Convert.ToInt32(reader["nzp_supp"]);
                        else if (kod == Series.Types.Ulica.GetHashCode()) val.cur_val = Convert.ToInt32(reader["nzp_ul"]);
                        series.PutVal(val);

                        if (val.getAndInc)
                        {
                            /* if (kod == Series.Types.NumLs.GetHashCode())
                              {
                                  ret = ExecSQL(connection, trans, "SELECT setval('"+Points.Pref+"_data.kvar_num_ls_seq', max(num_ls)) FROM fsmr_data.kvar", true);
                                  if (!ret.result)
                                  {
                                      ret.text = "Ошибка выделения series";
                                      ret.result = false;
                                      return;
                                  }
                              }*/
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка GetSeries()\n" + ret.text, MonitorLog.typelog.Error, 2, 100, true);
            }
        }


        public Returns GetNewId(IDbConnection connection, IDbTransaction transaction, Series.Types number, string pref)
        {
            if (pref.Trim() == "" &&
                (number == Series.Types.Counter ||
                number == Series.Types.Dom ||
                number == Series.Types.Kvar ||
                number == Series.Types.NumLs ||
                number == Series.Types.PKod10))
            {
                return new Returns(false, "Не задан префикс базы данных");
            }

            string prefix = pref == null || pref.Trim() == "" ? Points.Pref : pref.Trim();

            if (number == Series.Types.Area ||
                number == Series.Types.Geu ||
                number == Series.Types.Payer ||
                number == Series.Types.Supplier ||
                number == Series.Types.Ulica)
            {
                prefix = Points.Pref;
            }

            Returns ret;

            Series series = new Series(new int[] { (int)number });
            GetSeries(connection, transaction, prefix, series, out ret);

            if (!ret.result) return ret;

            _Series val = series.GetSeries((int)number);
            if (val.cur_val < 1)
            {
                ret.text = "Не определен series с кодом " + (int)number;
                ret.result = false;
                MonitorLog.WriteLog(ret.text, MonitorLog.typelog.Error, true);
                return ret;
            }

            ret.tag = val.cur_val;
            return ret;
        }

        public Returns GetNewId(IDbConnection connection, IDbTransaction transaction, Series.Types number)
        {
            return GetNewId(connection, transaction, number, Points.Pref);
        }

        public Returns GetNewId(IDbConnection connection, Series.Types number, string pref)
        {
            Returns ret = Utils.InitReturns();

            IDbTransaction trans = connection.BeginTransaction();
            ret = GetNewId(connection, trans, number, pref);
            if (ret.result) trans.Commit();
            else trans.Rollback();

            return ret;
        }

        public Returns GetNewId(IDbConnection connection, Series.Types number)
        {
            return GetNewId(connection, number, Points.Pref);
        }

        /// <summary>
        /// Получить наименование объекта по уникальному коду
        /// </summary>
        /// <param name="kod"></param>
        /// <param name="tip"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public string GetNameById(long kod, int tip, out Returns ret)
        {
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            string s = GetNameById(conn_db, null, kod, tip, out ret);

            conn_db.Close();
            return s;
        }

        public string GetNameById(IDbConnection connection, IDbTransaction transaction, long kod, int tip, out Returns ret)
        {
            ret = Utils.InitReturns();

            //выбрать список
            string sql = "";

            if (tip == Constants.getInfo_supp)
#if PG
                sql = " Select name_supp as name From " + Points.Pref + "_kernel.supplier Where nzp_supp = " + kod;
#else
                sql = " Select name_supp as name From " + Points.Pref + "_kernel:supplier Where nzp_supp = " + kod;
#endif
            else
#if PG
                sql = " Select area as name From " + Points.Pref + "_data.s_area Where nzp_area = " + kod;
#else
                sql = " Select area as name From " + Points.Pref + "_data:s_area Where nzp_area = " + kod;
#endif

            IDataReader reader;
            ret = ExecRead(connection, transaction, out reader, sql, true);
            if (!ret.result)
            {
                return "";
            }

            string s = "";
            try
            {
                if (reader.Read())
                {
                    if (reader["name"] != DBNull.Value) s = ((string)reader["name"]).Trim();
                }
                else
                {
                    ret.result = false;
                    ret.text = "Данные не найдены";
                }
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка выборки данных GetNameById(" + tip + ".id=" + kod + ")\n" + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
            }
            reader.Close();
            reader.Dispose();

            return s;
        }
    }
}
