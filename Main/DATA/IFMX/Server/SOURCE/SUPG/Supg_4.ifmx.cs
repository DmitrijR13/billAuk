using FastReport;
using SevenZip;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;

namespace STCLINE.KP50.DataBase
{
    //----------------------------------------------------------------------
    public partial class Supg : DataBaseHead
    //----------------------------------------------------------------------
    {


        /// <summary>
        /// Выгрузка недопоставок в файл в формате, предусмотренном для загрузки в КП2
        /// </summary>
        /// <param name="nzp_user">пользователь</param>
        /// <param name="ret"></param>
        /// <returns>bool</returns>
        public bool NedopUnload(Journal finder, out Returns ret)
        {

            IDataReader reader = null;
            IDbConnection conn_db = null;
            IDbConnection conn_web = null;
            DbWorkUser db = new DbWorkUser();
            DateTime date = new DateTime(Points.CalcMonth.year_, Points.CalcMonth.month_, 1);
            string fname = "cds_ned_" + System.Convert.ToDateTime(DateTime.Now.ToString()).ToString("yyyy_MM_dd_HH_mm");
            string fullfname = Constants.Directories.ReportDir + fname + ".unl";
            string arcfilename = Constants.Directories.ReportDir + fname + ".7z";

            ret = Utils.InitReturns();

            //if (finder.nzp_user < 1)
            //{
            //    ret.result = false;
            //    ret.text = "Не определен пользователь";
            //    return false;
            //}

            try
            {
                conn_web = GetConnection(Constants.cons_Webdata);
                ret = OpenDb(conn_web, true);
                if (!ret.result) return false;

                conn_db = GetConnection(Constants.cons_Kernel);
                ret = OpenDb(conn_db, true);
                if (!ret.result) return false;

                //int nzp_User = db.GetSupgUser(conn_db, null, new Finder() { nzp_user = finder.nzp_user }, out ret);
                //if (!ret.result)
                //{
                //    MonitorLog.WriteLog("Ошибка определения локального пользователя :" + ret.text, MonitorLog.typelog.Error, true);
                //    return false;
                //}

                #region Выгрузка недопоставок

#if PG
                string sql =
                               "select n.nzp_nedop, n.nzp_kvar, n.nzp_serv, n.nzp_supp, n.dat_s, n.dat_po, n.tn, n.comment,  " +
                               "\'2\' is_actual, n.dat_when,  k.pkod, n.nzp_kind " +
                               " from " + Points.Pref + "_supg.nedop_kvar n," + Points.Pref + "_data.kvar k " +
                               " where n.nzp_kvar=k.nzp_kvar " +
                               " and n.nzp_jrn=" + finder.number;
#else
                string sql =
                               "select n.nzp_nedop, n.nzp_kvar, n.nzp_serv, n.nzp_supp, n.dat_s, n.dat_po, n.tn, n.comment,  " +
                               "\'2\' is_actual, n.dat_when,  k.pkod, n.nzp_kind " +
                               " from " + Points.Pref + "_supg:nedop_kvar n," + Points.Pref + "_data:kvar k " +
                               " where n.nzp_kvar=k.nzp_kvar " +
                               " and n.nzp_jrn=" + finder.number;
#endif
                if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки данных " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.text = "Ошибка выборки данных";
                    return false;
                }

                if (reader != null)
                {
                    StreamWriter sw = new StreamWriter(fullfname, true, Encoding.GetEncoding(1251));
                    while (reader.Read())
                    {
                        string unlstring = "";
                        string fstring = "";
                        fstring = "|"; if (reader["nzp_nedop"] != DBNull.Value) fstring = Convert.ToString(reader["nzp_nedop"]).Trim() + "|"; unlstring = unlstring + fstring;
                        fstring = "|"; if (reader["nzp_kvar"] != DBNull.Value) fstring = Convert.ToString(reader["nzp_kvar"]).Trim() + "|"; unlstring = unlstring + fstring;
                        fstring = "|"; if (reader["nzp_serv"] != DBNull.Value) fstring = Convert.ToString(reader["nzp_serv"]).Trim() + "|"; unlstring = unlstring + fstring;
                        fstring = "|"; if (reader["nzp_supp"] != DBNull.Value) fstring = Convert.ToString(reader["nzp_supp"]).Trim() + "|"; unlstring = unlstring + fstring;
                        fstring = "|"; if (reader["dat_s"] != DBNull.Value) fstring = System.Convert.ToDateTime(reader["dat_s"]).ToString("yyyy-MM-dd HH:mm") + "|"; unlstring = unlstring + fstring;
                        fstring = "|"; if (reader["dat_po"] != DBNull.Value) fstring = System.Convert.ToDateTime(reader["dat_po"]).ToString("yyyy-MM-dd HH:mm") + "|"; unlstring = unlstring + fstring;
                        fstring = "|"; if (reader["tn"] != DBNull.Value) fstring = Convert.ToString(reader["tn"]).Trim() + "|"; unlstring = unlstring + fstring;
                        fstring = "|"; if (reader["comment"] != DBNull.Value) fstring = Convert.ToString(reader["comment"]).Trim() + "|"; unlstring = unlstring + fstring;
                        fstring = "| |"; if (reader["is_actual"] != DBNull.Value) fstring = Convert.ToString(reader["is_actual"]).Trim() + "| |"; unlstring = unlstring + fstring;
                        fstring = "|"; if (reader["dat_when"] != DBNull.Value) fstring = System.Convert.ToDateTime(reader["dat_when"]).ToString("dd.MM.yyyy") + "|"; unlstring = unlstring + fstring;

                        //if (reader["pkod"] != DBNull.Value) unlstring = unlstring + Convert.ToString(reader["pkod"]).Trim() + "|";
                        string cur = "";
                        if (reader["pkod"] != DBNull.Value)
                        {
                            cur = Convert.ToString(reader["pkod"]).Trim();
                            if (cur.Length > 8)
                            {
                                cur = cur.Substring(2, cur.Length - 2);
                            }
                        }
                        unlstring = unlstring + cur + "|";

                        fstring = "|"; if (reader["nzp_kind"] != DBNull.Value) fstring = Convert.ToString(reader["nzp_kind"]).Trim() + "|"; unlstring = unlstring + fstring;
                        sw.WriteLine(unlstring.ToString());
                    }
                    sw.Close();
                }

                #endregion

                #region Архивация
                string[] filesarch = new string[1];
                filesarch[0] = fullfname;
                try
                {
                    SevenZipCompressor unlzip = new SevenZipCompressor();
                    unlzip.DirectoryStructure = false;
                    unlzip.CompressFiles(arcfilename, filesarch);
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка архивации. " + ex.Message, MonitorLog.typelog.Error, true);
                    ret.text = "Ошибка архивации";
                    return false;
                }

                try
                {
                    File.Delete(fullfname);
                }
                catch
                {
                }
                #endregion

                #region Запись в журнале
#if PG
                sql = " update " + Points.Pref + "_supg. jrn_upg_nedop set exc_path = \'" + fname + ".7z\' where no=" + finder.number;
#else
                sql = " update " + Points.Pref + "_supg: jrn_upg_nedop set exc_path = \'" + fname + ".7z\' where no=" + finder.number;
#endif
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка изменения записи в журнале " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.text = "Ошибка изменения записи в журнале";
                    return false;
                }
                #endregion

                return true;

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры  NedopUnload : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = "Ошибка формирования файла";
                return false;
            }
            finally
            {
                #region Закрытие соединений

                if (conn_db != null)
                {
                    conn_db.Close();
                }

                if (conn_web != null)
                {
                    conn_web.Close();
                }

                if (reader != null)
                {
                    reader.Close();
                }

                #endregion
            }
        }

        /// <summary>
        /// выставление признака формирования недопоставки
        /// </summary>
        /// <param name="nzp_user">пользователь</param>
        /// <param name="act_flag">флаг формирования признака недопоставки</param>
        /// <param name="ret"></param>
        /// <returns>количество выставленных/снятых признаков формирования недопоставок</returns>
        public int SetZakazActActual(SupgFinder finder, out Returns ret)
        {
            string sql = "";
            string tXX_supg = "t" + finder.nzp_user + "_supg";
            int count = 0;
            IDbConnection conn_web = null;
            IDbConnection conn_db = null;
            DbWorkUser db = new DbWorkUser();
            ret = Utils.InitReturns();

            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return -1;
            }
            try
            {
                conn_web = GetConnection(Constants.cons_Webdata);
                ret = OpenDb(conn_web, true);
                if (!ret.result) return -1;

                conn_db = GetConnection(Constants.cons_Kernel);
                ret = OpenDb(conn_db, true);
                if (!ret.result) return -1;

                //List<_Point> prefixs = new List<_Point>();
                //_Point point = new _Point();
                //if (finder.pref != "")
                //{
                //    point.pref = finder.pref;
                //    prefixs.Add(point);
                //}
                //else
                //{
                //    prefixs = Points.PointList;
                //}

                //foreach (_Point items in prefixs)
                //{
                //finder.pref = items.pref;
                int nzpUser = db.GetSupgUser(conn_db, null, new Finder() { nzp_user = finder.nzp_user, pref = finder.pref }, out ret);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка определения локального пользователя :" + ret.text, MonitorLog.typelog.Error, true);
                    return -1;
                }

                if (finder.act_flag == 1)
                {
#if PG
                    sql = " update " + Points.Pref + "_supg. zakaz " +
                          " set ds_actual = 1, ds_date = now(), ds_user = " + nzpUser +
                          " where " +
                          " nzp_zk in (select nzp_zk from " + "public" + "." + tXX_supg + ")" +
                          " and nzp_res = 5 " +
                          " and ds_actual in (0)";
#else
                        sql = " update " + Points.Pref + "_supg: zakaz " +
                                                    " set ds_actual = 1, ds_date = today, ds_user = " + nzpUser +
                                                    " where " +
                                                    " nzp_zk in (select nzp_zk from " + conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXX_supg + ")" +
                                                    " and nzp_res = 5 " +
                                                    " and ds_actual in (0)";
#endif
                }
                else
                {
                    if (finder.act_flag == -1)
                    {
#if PG
                        sql = " update " + Points.Pref + "_supg. zakaz " +
                                                     " set ds_actual = 0, ds_date=now(), ds_user = " + finder.nzp_user +
                                                     " where " +
                                                     " nzp_zk in (select nzp_zk from " + "public" + "." + tXX_supg + ")" +
                                                     " and nzp_res = 5 " +
                                                     " And ds_actual in (1)";
#else
                            sql = " update " + Points.Pref + "_supg: zakaz " +
                                                                                    " set ds_actual = 0, ds_date=today, ds_user = " + finder.nzp_user +
                                                                                    " where " +
                                                                                    " nzp_zk in (select nzp_zk from " + conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXX_supg + ")" +
                                                                                    " and nzp_res = 5 " +
                                                                                    " And ds_actual in (1)";
#endif
                    }
                }

                ret = ExecSQL(conn_db, sql, true);

                if (!ret.result)
                {
                    return -1;
                }
                else
                {
                    count += GetSerialValue2(conn_web);
                }
                //}
                return count;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры  SetZakazActActual : " + ex.Message, MonitorLog.typelog.Error, true);
                return -1;
            }
            finally
            {
                #region Закрытие соединений

                if (conn_web != null)
                {
                    conn_web.Close();
                }

                #endregion
            }
        }

        /// <summary>
        /// удаление записи из журнала
        /// </summary>
        /// <param name="nzp_user">пользователь</param>
        /// <param name="number">номер записи</param>
        /// <param name="ret"></param>
        /// <returns>bool</returns>
        public bool DeleteFromJournal(Journal finder, out Returns ret)
        {
            string sql = "";

            IDataReader reader = null;
            IDbConnection conn_db = null;
            ret = Utils.InitReturns();

            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return false;
            }

            try
            {
                conn_db = GetConnection(Constants.cons_Kernel);
                ret = OpenDb(conn_db, true);
                if (!ret.result) return false;

                #region Удалить файл, если он есть
#if PG
                sql = " select exc_path from " + Points.Pref + "_supg. jrn_upg_nedop where no = " + finder.number;
#else
                sql = " select exc_path from " + Points.Pref + "_supg: jrn_upg_nedop where no = " + finder.number;
#endif
                if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
                {
                    string err_text = "Ошибка при выборке данных из журнала ";
                    MonitorLog.WriteLog(err_text + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.text = err_text;
                    return false;
                }

                string exc_path = "";
                if (reader != null)
                {
                    while (reader.Read())
                    {
                        if (reader["exc_path"] != DBNull.Value)
                        {
                            exc_path = reader["exc_path"].ToString();
                            break;
                        }
                    }
                }

                if (exc_path != "")
                {
                    string arcfilename = Constants.Directories.ReportDir + exc_path;
                    try
                    {
                        File.Delete(arcfilename);
                    }
                    catch
                    {
                    }
                }
                #endregion

#if PG
                sql = " Delete from " + Points.Pref + "_supg. nedop_kvar where nzp_jrn = " + finder.number;
#else
                sql = " Delete from " + Points.Pref + "_supg: nedop_kvar where nzp_jrn = " + finder.number;
#endif
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                {
                    return false;
                }
#if PG
                sql = " Delete from " + Points.Pref + "_supg. jrn_upg_nedop where no = " + finder.number;
#else
                sql = " Delete from " + Points.Pref + "_supg: jrn_upg_nedop where no = " + finder.number;
#endif
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                {
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры  DeleteFromJournal : " + ex.Message, MonitorLog.typelog.Error, true);
                return false;
            }
            finally
            {
                #region Закрытие соединений

                if (conn_db != null)
                {
                    conn_db.Close();
                }

                #endregion
            }
        }

        /// <summary>
        /// удаление записи из журнала
        /// </summary>
        /// <param name="nzp_user">пользователь</param>
        /// <param name="number">номер записи</param>
        /// <param name="ret"></param>
        /// <returns>bool</returns>
        public bool UpdateJournal(Journal finder, out Returns ret)
        {
            string sql = "";

            IDbConnection conn_db = null;
            ret = Utils.InitReturns();

            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return false;
            }
            try
            {
                conn_db = GetConnection(Constants.cons_Kernel);
                ret = OpenDb(conn_db, true);
                if (!ret.result) return false;
#if PG
                sql = " update " + Points.Pref + "_supg. jrn_upg_nedop set status = " + finder.status.ToString() + " where no = " + finder.number;
#else
                sql = " update " + Points.Pref + "_supg: jrn_upg_nedop set status = " + finder.status.ToString() + " where no = " + finder.number;
#endif
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                {
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры  UppdateJournal : " + ex.Message, MonitorLog.typelog.Error, true);
                return false;
            }
            finally
            {
                #region Закрытие соединений

                if (conn_db != null)
                {
                    conn_db.Close();
                }

                #endregion
            }
        }

        /// <summary>
        /// процедура для получения отчета: "Заказ"
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public DataSet GetZakazReport(SupgFinder finder, string table_name, out Returns ret)
        {
            ret = Utils.InitReturns();
            IDataReader reader1;
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            DataSet res = new DataSet("zakaz");
            res.EnforceConstraints = false;
            Dictionary<int, string> nzp_zk_dict = new Dictionary<int, string>();//для каждого datatable в dataset

            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("GetZakazReport : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                return null;
            }

            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return null;
            }

            #region проверка полученных данных и заполнение nzp_zk_dict

            if (string.IsNullOrEmpty(table_name))
            {
                if (string.IsNullOrEmpty(finder.pref) || finder.nzp_zk == 0)
                {
                    if (string.IsNullOrEmpty(finder.pref))
                    {
                        ret.result = false;
                        ret.text = "Проверьте заполненность значения pref в GetZakazReport";
                        return null;
                    }
                    else
                    {
                        ret.result = false;
                        ret.text = "Проверьте заполненность значения nzp_zk в GetZakazReport";
                        return null;
                    }
                }
                else
                {
                    nzp_zk_dict.Add(finder.nzp_zk, finder.pref);
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(finder.pref) && finder.nzp_zk != 0)
                {
                    nzp_zk_dict.Add(finder.nzp_zk, finder.pref);
                }
                else
                {
                    //заполнение nzp_zk_dict остальными данными с помощью table_name
                }
            }

            #endregion

            try
            {
                foreach (KeyValuePair<int, string> item in nzp_zk_dict)
                {
                    #region получение данных

                    DataTable res_table = new DataTable("Q_master");
#if PG
                    string sql =
                                                   "select " +
                                                   "zk.nzp_zk, " +
                                                   "zk.order_date, " +
                                                   "z.demand_name, " +
                                                   "z.phone, " +
                                                   "z.comment, " +
                                                   "trim(coalesce(a.area,''))||' / '||trim(coalesce(g.geu,'')) as territory, " +
                                                   "k.pkod10 as num_ls, " +
                                                   "'ул.'||trim(u.ulica)||(case when d.ndom is null then ' ' else ' д.'||trim(d.ndom)|| " +
                                                   "(case when d.nkor='-' then ' ' else  ' корп.'||trim(d.nkor) end) end)|| " +
                                                   "(case when k.nkvar is null then ' ' else ' кв.'||trim(k.nkvar)|| " +
                                                   "(case when trim(k.nkvar_n)='-' then ' ' else ' комн.'||trim(k.nkvar_n) end) end ) adress, " +
                                                   "sp.name_supp supplier, " +
                                                   "ds.dest_name, " +
                                                   "zk.exec_date, " +
                                                   "zk.fact_date, " +
                                                   "zk.comment_n, " +
                                                   "(case when sv.service is null then ds.dest_name else sv.service end) service, " +
                                                   "n1.name as ned_name, " +
                                                   "(case when n1.name is null then null else (case when zk.nedop_s is null then date_trunc('hour',z.zvk_date) else zk.nedop_s end) end) nedop_s, " +
                                                   "(case when zk.act_actual=0 then '' else n2.name end) as act_ned_name, " +
                                                   "(case when zk.act_actual=0 then null else zk.act_s  end) act_s, " +
                                                   "(case when zk.act_actual=0 then null else zk.act_po end) act_po " +
                                                   "From  " +
                                                   //Points.Pref + "_supg. zvk z, " +
                                                   //Points.Pref + "_supg. zakaz zk, " +
                                                   //"outer (" + Points.Pref + "_data. kvar k, " +
                                                   //Points.Pref + "_data. dom d, " +
                                                   //Points.Pref + "_data. s_ulica u, " +
                                                   //"outer " + Points.Pref + "_data. s_area a, " +
                                                   //"outer " + Points.Pref + "_data. s_geu g), " +
                                                   //Points.Pref + "_supg. s_dest ds, " +
                                                   //"OUTER(" + Points.Pref + "_kernel. services sv),  " +
                                                   //Points.Pref + "_kernel. supplier sp, " +
                                                   //Points.Pref + "_supg. s_result r, " +
                                                   //"outer " + Points.Pref + "_data.upg_s_kind_nedop n1," +
                                                   //"outer " + Points.Pref + "_data.upg_s_kind_nedop n2 " +
                                                   Points.Pref + "_supg. zvk z  "+
                                                   " left outer join " + Points.Pref + "_data. kvar k on z.nzp_kvar = k.nzp_kvar " +
                                                   " left outer join " + Points.Pref + "_data. dom d on k.nzp_dom=d.nzp_dom "+
                                                   " left outer join " + Points.Pref + "_data. s_ulica u on d.nzp_ul=u.nzp_ul "+
                                                   " left outer join " + Points.Pref + "_data. s_area a on k.nzp_area = a.nzp_area "+
                                                   " left outer join " + Points.Pref + "_data. s_geu g on k.nzp_geu = g.nzp_geu,  "+
                                                   Points.Pref + "_supg. zakaz zk "+
                                                   " left outer join " + Points.Pref + "_data.upg_s_kind_nedop n2 on zk.act_num_nedop = n2.nzp_kind and n2.kod_kind = 1, "+
                                                   Points.Pref + "_supg. s_dest ds "+
                                                   " left outer join " + Points.Pref + "_data.upg_s_kind_nedop n1 on ds.num_nedop = n1.nzp_kind and n1.kod_kind = 1 " +
                                                   " left outer join " + Points.Pref + "_kernel.services sv on ds.nzp_serv=sv.nzp_serv,  " +
                                                   Points.Pref + "_kernel. supplier sp, " + Points.Pref + "_supg. s_result r "+
                                                   
                                                   "Where " +
                                                   "zk.nzp_zk = " + item.Key + " " +
                                                   //"and zk.nzp_zvk = z.nzp_zvk " +
                                                   //"and z.nzp_kvar = k.nzp_kvar and k.nzp_dom = d.nzp_dom and d.nzp_ul = u.nzp_ul " +
                                                   //"and k.nzp_area = a.nzp_area and k.nzp_geu = g.nzp_geu " +
                                                   //"and zk.nzp_dest = ds.nzp_dest and ds.nzp_serv = sv.nzp_serv and ds.num_nedop = n1.nzp_kind and n1.kod_kind = 1 " +
                                                   //"and zk.act_num_nedop = n2.nzp_kind and n2.kod_kind = 1 " +
                                                   //"and zk.nzp_res = r.nzp_res " +
                                                   //"and zk.nzp_supp = sp.nzp_supp  ";
                                                   " and zk.nzp_zvk = z.nzp_zvk  "+
                                                   " and zk.nzp_dest = ds.nzp_dest "+
                                                   " and zk.nzp_res = r.nzp_res and zk.nzp_supp = sp.nzp_supp "; 
#else
                    string sql =
                                                   "select " +
                                                   "zk.nzp_zk, " +
                                                   "zk.order_date, " +
                                                   "z.demand_name, " +
                                                   "z.phone, " +
                                                   "z.comment, " +
                                                   "trim(nvl(a.area,''))||' / '||trim(nvl(g.geu,'')) as territory, " +
                                                   "k.pkod10 as num_ls, " +
                                                   "'ул.'||trim(u.ulica)||(case when d.ndom is null then ' ' else ' д.'||trim(d.ndom)|| " +
                                                   "(case when d.nkor='-' then ' ' else  ' корп.'||trim(d.nkor) end) end)|| " +
                                                   "(case when k.nkvar is null then ' ' else ' кв.'||trim(k.nkvar)|| " +
                                                   "(case when trim(k.nkvar_n)='-' then ' ' else ' комн.'||trim(k.nkvar_n) end) end ) adress, " +
                                                   "sp.name_supp supplier, " +
                                                   "ds.dest_name, " +
                                                   "zk.exec_date, " +
                                                   "zk.fact_date, " +
                                                   "zk.comment_n, " +
                                                   "(case when sv.service is null then ds.dest_name else sv.service end) service, " +
                                                   "n1.name as ned_name, " +
                                                   "(case when n1.name is null then null else (case when zk.nedop_s is null then extend(z.zvk_date,year to hour) else zk.nedop_s end) end) nedop_s, " +
                                                   "(case when zk.act_actual=0 then '' else n2.name end) as act_ned_name, " +
                                                   "(case when zk.act_actual=0 then null else zk.act_s  end) act_s, " +
                                                   "(case when zk.act_actual=0 then null else zk.act_po end) act_po " +
                                                   "From  " +
                                                   Points.Pref + "_supg: zvk z, " +
                                                   Points.Pref + "_supg: zakaz zk, " +
                                                   "outer (" + Points.Pref + "_data: kvar k, " +
                                                   Points.Pref + "_data: dom d, " +
                                                   Points.Pref + "_data: s_ulica u, " +
                                                   "outer " + Points.Pref + "_data: s_area a, " +
                                                   "outer " + Points.Pref + "_data: s_geu g), " +
                                                   Points.Pref + "_supg: s_dest ds, " +
                                                   "OUTER(" + Points.Pref + "_kernel: services sv),  " +
                                                   Points.Pref + "_kernel: supplier sp, " +
                                                   Points.Pref + "_supg: s_result r, " +
                                                   "outer " + Points.Pref + "_data:upg_s_kind_nedop n1," +
                                                   "outer " + Points.Pref + "_data:upg_s_kind_nedop n2 " +
                                                   "Where " +
                                                   "zk.nzp_zk = " + item.Key + " " +
                                                   "and zk.nzp_zvk = z.nzp_zvk " +
                                                   "and z.nzp_kvar = k.nzp_kvar and k.nzp_dom = d.nzp_dom and d.nzp_ul = u.nzp_ul " +
                                                   "and k.nzp_area = a.nzp_area and k.nzp_geu = g.nzp_geu " +
                                                   "and zk.nzp_dest = ds.nzp_dest and ds.nzp_serv = sv.nzp_serv and ds.num_nedop = n1.nzp_kind and n1.kod_kind = 1 " +
                                                   "and zk.act_num_nedop = n2.nzp_kind and n2.kod_kind = 1 " +
                                                   "and zk.nzp_res = r.nzp_res " +
                                                   "and zk.nzp_supp = sp.nzp_supp  ";
#endif

                    DataTable table = ClassDBUtils.OpenSQL(sql, "Q_master", conn_db).GetData();


                    #region редактирование таблицы

                    res_table = table.Clone();
                    res_table.Columns["nedop_s"].DataType = typeof(String);
                    res_table.Columns["fact_date"].DataType = typeof(String);
                    res_table.Columns["act_s"].DataType = typeof(String);
                    res_table.Columns["act_po"].DataType = typeof(String);
                    res_table.Columns["exec_date"].DataType = typeof(String);

                    res_table.ImportRow(table.Rows[0]);

                    res_table.Rows[0]["nedop_s"] = res_table.Rows[0]["nedop_s"].ToString().Trim();
                    if (!String.IsNullOrEmpty(res_table.Rows[0]["nedop_s"].ToString()))
                    {
                        res_table.Rows[0]["nedop_s"] = res_table.Rows[0]["nedop_s"].ToString().Substring(0, res_table.Rows[0]["nedop_s"].ToString().IndexOf(':')) + "ч.";
                    }

                    res_table.Rows[0]["fact_date"] = res_table.Rows[0]["fact_date"].ToString().Trim();
                    if (!String.IsNullOrEmpty(res_table.Rows[0]["fact_date"].ToString()))
                    {
                        res_table.Rows[0]["fact_date"] = res_table.Rows[0]["fact_date"].ToString().Substring(0, res_table.Rows[0]["fact_date"].ToString().IndexOf(':')) + "ч.";
                    }

                    res_table.Rows[0]["act_s"] = res_table.Rows[0]["act_s"].ToString().Trim();
                    if (!String.IsNullOrEmpty(res_table.Rows[0]["act_s"].ToString()))
                    {
                        res_table.Rows[0]["act_s"] = res_table.Rows[0]["act_s"].ToString().Substring(0, res_table.Rows[0]["act_s"].ToString().IndexOf(':')) + "ч.";
                    }

                    res_table.Rows[0]["act_po"] = res_table.Rows[0]["act_po"].ToString().Trim();
                    if (!String.IsNullOrEmpty(res_table.Rows[0]["act_po"].ToString()))
                    {
                        res_table.Rows[0]["act_po"] = res_table.Rows[0]["act_po"].ToString().Substring(0, res_table.Rows[0]["act_po"].ToString().IndexOf(':')) + "ч.";
                    }

                    res_table.Rows[0]["exec_date"] = res_table.Rows[0]["exec_date"].ToString().Trim();
                    if (!String.IsNullOrEmpty(res_table.Rows[0]["exec_date"].ToString()))
                    {
                        res_table.Rows[0]["exec_date"] = res_table.Rows[0]["exec_date"].ToString().Substring(0, res_table.Rows[0]["exec_date"].ToString().IndexOf(':')) + "ч.";
                    }

                    #endregion

                    #region val_prm
#if PG
                    string sql_val_prm = "SELECT p.payer as val_prm from " + Points.Pref + "_kernel. s_payer p, " + Points.Pref + "_supg. zakaz zk WHERE p.nzp_payer = zk.nzp_payer and zk.nzp_zk = " + item.Key;
#else
                    string sql_val_prm = "SELECT p.payer as val_prm from " + Points.Pref + "_kernel: s_payer p, " + Points.Pref + "_supg: zakaz zk WHERE p.nzp_payer = zk.nzp_payer and zk.nzp_zk = " + item.Key;
#endif
                    if (!ExecRead(conn_db, out reader1, sql_val_prm, true).result)
                    {
                        MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        conn_db.Close();
                        sql.Remove(0, sql.Length);
                        ret.result = false;
                        return null;
                    }

                    res_table.Columns.Add(new DataColumn("val_prm", typeof(string)));
                    if (reader1 != null)
                    {
                        while (reader1.Read())
                        {
                            if (reader1["val_prm"] != DBNull.Value) res_table.Rows[0]["val_prm"] = reader1["val_prm"].ToString().Trim();
                        }
                    }

                    //????
                    if (res_table.Rows[0]["val_prm"] == DBNull.Value)
                        res_table.Rows[0]["val_prm"] = "";

                    #endregion
                    //}

                    #endregion

                    res.Tables.Add(res_table);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetZakazReport: " + ex.Message, MonitorLog.typelog.Error, true);
                ret.result = false;
                conn_db.Close();
                return null;
            }

            return res;
        }

        public void FindActs(SupgActFinder finder, out Returns ret)
        {
            #region проверка входящих данных
            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Не определен пользователь");
                ret.tag = -1;
                return;
            }

            if (finder.nzp_payer == 0)
            {
                ret = new Returns(false, "Не определена организация пользователя");
                ret.tag = -1;
                return;
            }

            DateTime date;
            if (finder.plan_date != "")
            {
                if (!DateTime.TryParse(finder.plan_date, out date))
                {
                    ret = new Returns(false, "Неверный формат поля plan_date");
                    return;
                }
            }

            if (finder.plan_date_to != "")
            {
                if (!DateTime.TryParse(finder.plan_date_to, out date))
                {
                    ret = new Returns(false, "Неверный формат поля plan_date_to");
                    return;
                }
            }

            if (finder.dat_s != "")
            {
                if (!DateTime.TryParse(finder.dat_s, out date))
                {
                    ret = new Returns(false, "Неверный формат поля dat_s");
                    return;
                }
            }

            if (finder.dat_s_to != "")
            {
                if (!DateTime.TryParse(finder.dat_s_to, out date))
                {
                    ret = new Returns(false, "Неверный формат поля dat_s_to");
                    return;
                }
            }

            if (finder.dat_po != "")
            {
                if (!DateTime.TryParse(finder.dat_po, out date))
                {
                    ret = new Returns(false, "Неверный формат поля dat_po");
                    return;
                }
            }

            if (finder.dat_po_to != "")
            {
                if (!DateTime.TryParse(finder.dat_po_to, out date))
                {
                    ret = new Returns(false, "Неверный формат поля dat_po_to");
                    return;
                }
            }

            #endregion

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return;
#if PG
            string tXX_supg_acts = "t" + Convert.ToString(finder.nzp_user) + "_supg_acts";
            string tXX_supg_acts_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + "." + tXX_supg_acts;
#else
            string tXX_supg_acts = "t" + Convert.ToString(finder.nzp_user) + "_supg_acts";
            string tXX_supg_acts_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXX_supg_acts;
#endif
            //создать кэш-таблицу
            if (TableInWebCashe(conn_web, tXX_supg_acts))
            {
                ExecSQL(conn_web, " Drop table " + tXX_supg_acts, false);
            }

#if PG
            ret = ExecSQL(conn_web,
                           " create table " + tXX_supg_acts + "(" +
                               "nzp_act int NOT NULL," +
                               "number CHAR(15)," +
                               "_date DATE," +
                               "plan_number CHAR(15)," +
                               "plan_date DATE," +
                               "nzp_supp INTEGER," +
                               "supplier char(100)," +
                               "nzp_serv INTEGER NOT NULL," +
                               "service char(100)," +
                               "dat_s timestamp," +
                               "dat_po timestamp," +
                               "comment VARCHAR(255)," +
                               "is_actual SMALLINT default 1)", true);
#else
 ret = ExecSQL(conn_web,
                " create table " + tXX_supg_acts + "(" +
                    "nzp_act int NOT NULL," +
                    "number CHAR(15)," +
                    "_date DATE," +
                    "plan_number CHAR(15)," +
                    "plan_date DATE," +
                    "nzp_supp INTEGER," +
                    "supplier char(100)," +
                    "nzp_serv INTEGER NOT NULL," +
                    "service char(100)," +
                    "dat_s DATETIME YEAR to HOUR," +
                    "dat_po DATETIME YEAR to HOUR," +
                    "comment NVARCHAR(255)," +
                    "is_actual SMALLINT default 1)", true);
#endif
            if (!ret.result)
            {
                conn_web.Close();
                return;
            }
            string connectionString = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                conn_web.Close();
                return;
            }

            #region учитываем шаблон поиска лиц счетов

            string spls = "";
            string spls_from = "";
            string spls_where = "";
            if (Utils.GetParams(finder.prms, Constants.act_findls))
            {
                //вызов следует из шаблона адресов, поэтому
                //надо прежде заполнить список адресов
                DbAdres db = new DbAdres();

                db.FindLs((Ls)finder, out ret);
                db.Close();

                if (!ret.result)
                {
                    return;
                }
                else
                {

#if PG
                    spls_from = ", " + Points.Pref + "_supg. act_obj ao, " + conn_web.Database + "@" + DBManager.getServer(conn_web) + ".t" + finder.nzp_user + "_spls ls ";
                    spls_where = " and a.nzp_act=ao.nzp_act and ao.nzp_ul=ls.nzp_ul and (ao.nzp_dom=0 or (ao.nzp_dom = ls.nzp_dom and (ao.nzp_kvar = ls.nzp_kvar or ao.nzp_kvar=0)))";
                    //spls =  " and a.nzp_act in "+
                    //        " (select distinct ao.nzp_act "+
                    //        " from " + Points.Pref + "_supg. act_obj ao, " + conn_web.Database + "@" + conn_web.Server + ".t" + finder.nzp_user + "_spls ls " +
                    //        " where ao.nzp_ul=ls.nzp_ul and (ao.nzp_dom=0 or (ao.nzp_dom = ls.nzp_dom and (ao.nzp_kvar = ls.nzp_kvar or ao.nzp_kvar=0))))";
#else
                    spls_from = ", " + Points.Pref + "_supg: act_obj ao, " + conn_web.Database + "@" + DBManager.getServer(conn_web) + ":t" + finder.nzp_user + "_spls ls " ;
                    spls_where = " and a.nzp_act=ao.nzp_act and ao.nzp_ul=ls.nzp_ul and (ao.nzp_dom=0 or (ao.nzp_dom = ls.nzp_dom and (ao.nzp_kvar = ls.nzp_kvar or ao.nzp_kvar=0)))";
                    //spls =  " and a.nzp_act in "+
                    //        " (select unique ao.nzp_act "+
                    //        " from " + Points.Pref + "_supg: act_obj ao, " + conn_web.Database + "@" + conn_web.Server + ":t" + finder.nzp_user + "_spls ls " +
                    //        " where ao.nzp_ul=ls.nzp_ul and (ao.nzp_dom=0 or (ao.nzp_dom = ls.nzp_dom and (ao.nzp_kvar = ls.nzp_kvar or ao.nzp_kvar=0))))";
#endif
                }
            }

            //if (finder.nzp_kvar < 1)
            //{
            //    spls = " and z.nzp_kvar = " + finder.nzp_kvar;
            //}

            #endregion

            string areas = "";
            string areas_from = "";

            #region роли

            if (finder.organization == BaseUser.OrganizationTypes.UK)
            {
#if PG
                if (spls_from == "")
                {
                    areas_from = ", " + Points.Pref + "_supg. act_obj ao, " + Points.Pref + "_data.dom d ";
                    areas += " and a.nzp_act = ao.nzp_act and " +
                                    " ao.nzp_ul=d.nzp_ul and ao.nzp_dom in (d.nzp_dom,0) and d.nzp_area in (" + finder.nzp_area + ")";
                }
                else
                {
                    areas += " and ls.nzp_area in (" + finder.nzp_area + ")";
                }
#else
if (spls_from == "")
                {
                    areas_from = ", " + Points.Pref + "_supg: act_obj ao, " + Points.Pref + "_data:dom d " ;
                    areas += " and a.nzp_act = ao.nzp_act and "+
                                    " ao.nzp_ul=d.nzp_ul and ao.nzp_dom in (d.nzp_dom,0) and d.nzp_area in (" + finder.nzp_area + ")";
                }
                else
                {
                    areas += " and ls.nzp_area in (" + finder.nzp_area + ")";
                }
#endif
            }
            else
            {
                if (finder.organization == BaseUser.OrganizationTypes.Supplier)
                {
                    areas += " and (a.nzp_supp = " + finder.nzp_supp + " or a.nzp_supp_plant = " + finder.nzp_supp + ")";
                }
            }

            #endregion
#if PG
            string sql = "Insert into public." + tXX_supg_acts +
                " select distinct a.nzp_act, a.number, a._date, a.plan_number, a.plan_date, a.nzp_supp, supp.name_supp, a.nzp_serv, serv.service, a.dat_s, a.dat_po, a.comment, a.is_actual" +
                " from " + Points.Pref + "_supg.act a left outer join " + Points.Pref + "_kernel.supplier supp ON  a.nzp_supp = supp.nzp_supp "+
                " left outer join " + Points.Pref + "_kernel.services serv ON a.nzp_serv = serv.nzp_serv " + spls_from + areas_from +
                " where 1=1 " + spls_where + areas;
            //номер документа
            if (finder.plan_number != "") sql += " and a.plan_number = " + Utils.EStrNull(finder.plan_number, "");
            //дата документа
            if (finder.plan_date != "" && finder.plan_date_to == "")
                sql += " and a.plan_date >= " + Utils.EStrNull(finder.plan_date);
            else if (finder.plan_date != "" && finder.plan_date_to != "")
                sql += " and a.plan_date between " + Utils.EStrNull(finder.plan_date) + " and " + Utils.EStrNull(finder.plan_date_to);
            //начало периода
            if (finder.dat_s != "" && finder.dat_s_to == "")
                sql += " and date(a.dat_s) >= " + Utils.EStrNull(finder.dat_s);
            else if (finder.dat_s != "" && finder.dat_s_to != "")
                sql += " and date(a.dat_s) between " + Utils.EStrNull(finder.dat_s) + " and " + Utils.EStrNull(finder.dat_s_to);
            //конец периода
            if (finder.dat_po != "" && finder.dat_po_to == "")
                sql += " and date(a.dat_po) >= " + Utils.EStrNull(finder.dat_po);
            else if (finder.dat_po != "" && finder.dat_po_to != "")
                sql += " and date(a.dat_po) between " + Utils.EStrNull(finder.dat_po) + " and " + Utils.EStrNull(finder.dat_po_to);
            //организация
            if (finder.nzp_supp_plant > 0) sql += " and a.nzp_supp_plant = " + finder.nzp_supp_plant;
            //причина недопоставки
            if (finder.nzp_work_type > 0)
            {
                sql += " and a.nzp_work_type = " + finder.nzp_work_type;
            }
            else
            {
                if (finder.work_type_list != null)
                {
                    if (finder.work_type_list.Count > 0)
                    {
                        sql += " and a.nzp_work_type in (-1 ";
                        foreach (int point in finder.work_type_list)
                        {
                            sql += "," + point.ToString();
                        }
                        sql += ")";
                    }
                }
            }
            //услуга
            if (finder.nzp_serv > 0) sql += " and a.nzp_serv = " + finder.nzp_serv;
            //тип недопоставки
            if (finder.nzp_kind > 0) sql += " and a.nzp_kind = " + finder.nzp_kind;
            //температура с - по
            if (finder.tn != 0 && finder.tn_to == Constants._ZERO_) sql += " and a.tn == " + finder.tn;
            else if (finder.tn != 0 && finder.tn_to != Constants._ZERO_) sql += " and a.tn between " + finder.tn + " and " + finder.tn_to;
            //акт действителен            
            if (finder.is_actual == 1) sql += " and a.is_actual <> 100";
            else if (finder.is_actual == 100) sql += " and a.is_actual = 100";
            //номер акта
            if (finder.number != "") sql += " and a.number = " + Utils.EStrNull(finder.number, "");
            //дата акта
            if (finder._date != "" && finder._date_to == "")
                sql += " and a._date >= " + Utils.EStrNull(finder._date);
            else if (finder._date != "" && finder._date_to != "")
                sql += " and a._date between " + Utils.EStrNull(finder._date) + " and " + Utils.EStrNull(finder._date_to);
            //дата регистрации
            if (finder.reply_date != "" && finder.reply_date_to == "")
                sql += " and a.reply_date >= " + Utils.EStrNull(finder.reply_date);
            else if (finder.reply_date != "" && finder.reply_date_to != "")
                sql += " and a.reply_date between " + Utils.EStrNull(finder.reply_date) + " and " + Utils.EStrNull(finder.reply_date_to);
            //поставщик услуг
            if (finder.nzp_serv_supp > 0) sql += " and a.nzp_supp = " + finder.nzp_serv_supp;
            sql += spls;
            ret = ExecSQL(conn_db, sql, true);
#else
            string sql = "Insert into " + tXX_supg_acts_full +
                " select unique a.nzp_act, a.number, a._date, a.plan_number, a.plan_date, a.nzp_supp, supp.name_supp, a.nzp_serv, serv.service, a.dat_s, a.dat_po, a.comment, a.is_actual" +
                " from " + Points.Pref + "_supg:act a, outer " + Points.Pref + "_kernel:supplier supp, outer " + Points.Pref + "_kernel:services serv " + spls_from + areas_from +
                " where a.nzp_supp = supp.nzp_supp and a.nzp_serv = serv.nzp_serv " + spls_where + areas;
            //номер документа
            if (finder.plan_number != "") sql += " and a.plan_number = " + Utils.EStrNull(finder.plan_number, "");
            //дата документа
            if (finder.plan_date != "" && finder.plan_date_to == "")
                sql += " and a.plan_date >= " + Utils.EStrNull(finder.plan_date);
            else if (finder.plan_date != "" && finder.plan_date_to != "")
                sql += " and a.plan_date between " + Utils.EStrNull(finder.plan_date) + " and " + Utils.EStrNull(finder.plan_date_to);
            //начало периода
            if (finder.dat_s != "" && finder.dat_s_to == "")
                sql += " and date(a.dat_s) >= " + Utils.EStrNull(finder.dat_s);
            else if (finder.dat_s != "" && finder.dat_s_to != "")
                sql += " and date(a.dat_s) between " + Utils.EStrNull(finder.dat_s) + " and " + Utils.EStrNull(finder.dat_s_to);
            //конец периода
            if (finder.dat_po != "" && finder.dat_po_to == "")
                sql += " and date(a.dat_po) >= " + Utils.EStrNull(finder.dat_po);
            else if (finder.dat_po != "" && finder.dat_po_to != "")
                sql += " and date(a.dat_po) between " + Utils.EStrNull(finder.dat_po) + " and " + Utils.EStrNull(finder.dat_po_to);
            //организация
            if (finder.nzp_supp_plant > 0) sql += " and a.nzp_supp_plant = " + finder.nzp_supp_plant;
            //причина недопоставки
            if (finder.nzp_work_type > 0)
            {
                sql += " and a.nzp_work_type = " + finder.nzp_work_type;
            }
            else
            {
                if (finder.work_type_list != null)
                {
                    if (finder.work_type_list.Count > 0)
                    {
                        sql += " and a.nzp_work_type in (-1 ";
                        foreach (int point in finder.work_type_list)
                        {
                            sql += "," + point.ToString();
                        }
                        sql += ")";
                    }
                }
            }
            //услуга
            if (finder.nzp_serv > 0) sql += " and a.nzp_serv = " + finder.nzp_serv;
            //тип недопоставки
            if (finder.nzp_kind > 0) sql += " and a.nzp_kind = " + finder.nzp_kind;
            //температура с - по
            if (finder.tn != 0 && finder.tn_to == Constants._ZERO_) sql += " and a.tn == " + finder.tn;
            else if (finder.tn != 0 && finder.tn_to != Constants._ZERO_) sql += " and a.tn between " + finder.tn + " and " + finder.tn_to;
            //акт действителен            
            if (finder.is_actual == 1) sql += " and a.is_actual <> 100";
            else if (finder.is_actual == 100) sql += " and a.is_actual = 100";
            //номер акта
            if (finder.number != "") sql += " and a.number = " + Utils.EStrNull(finder.number, "");
            //дата акта
            if (finder._date != "" && finder._date_to == "")
                sql += " and a._date >= " + Utils.EStrNull(finder._date);
            else if (finder._date != "" && finder._date_to != "")
                sql += " and a._date between " + Utils.EStrNull(finder._date) + " and " + Utils.EStrNull(finder._date_to);
            //дата регистрации
            if (finder.reply_date != "" && finder.reply_date_to == "")
                sql += " and a.reply_date >= " + Utils.EStrNull(finder.reply_date);
            else if (finder.reply_date != "" && finder.reply_date_to != "")
                sql += " and a.reply_date between " + Utils.EStrNull(finder.reply_date) + " and " + Utils.EStrNull(finder.reply_date_to);
            //поставщик услуг
            if (finder.nzp_serv_supp > 0) sql += " and a.nzp_supp = " + finder.nzp_serv_supp;
            sql += spls;
            ret = ExecSQL(conn_db, sql, true);
#endif
            //сохранение параметров успешного поиска
            DbNedop nedop = new DbNedop();
            ret = nedop.SaveFinder(finder, Constants.page_planned_works);

            if (!ret.result)
            {
                conn_web.Close();
                conn_db.Close();
                return;
            }

            conn_db.Close();

#if PG
            ExecSQL(conn_web, " Create unique index ix1_" + tXX_supg_acts + " on " + tXX_supg_acts + " (nzp_act) ", true);
            ExecSQL(conn_web, " analyze  " + tXX_supg_acts, true);

#else
            ExecSQL(conn_web, " Create unique index ix1_" + tXX_supg_acts + " on " + tXX_supg_acts + " (nzp_act) ", true);
            ExecSQL(conn_web, " Update statistics for table  " + tXX_supg_acts, true);

#endif

            conn_web.Close();
        }

        /// <summary>
        /// возвращает список плановых работ лицевого счета
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public List<SupgAct> GetKvarActs(SupgActFinder finder, out Returns ret)
        {
            string sql = "";
            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Не определен пользователь");
                return null;
            }

            IDbConnection con_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(con_db, true);
            if (!ret.result) return null;

            #region определяем общее количестов строк

#if PG
            string sql_count = "select count(*) from " +
                                   Points.Pref + "_supg.act_obj o, " +
                                   Points.Pref + "_supg.act p 	LEFT OUTER JOIN  " +
                                   Points.Pref + "_kernel.supplier as  sp "
                                   + "  on P .nzp_supp = sp.nzp_supp, " +

                                   Points.Pref + "_kernel.services sv, " +
                                   Points.Pref + "_data.dom d, " +
                                   Points.Pref + "_data.kvar k " +
                                   "where p.nzp_act=o.nzp_act " +

                                   "and p.nzp_serv=sv.nzp_serv " +
                                   "and o.nzp_dom=d.nzp_dom " +
                                   "and d.nzp_dom=k.nzp_dom " +
                                   "and k.nzp_kvar= " + finder.nzp_kvar + " " +
                                   "and (o.nzp_ul=d.nzp_ul) " +
                                   "and (o.nzp_dom=d.nzp_dom or o.nzp_dom=0) " +
                                   "and (o.nzp_kvar=k.nzp_kvar or o.nzp_kvar=0)";
#else
   string sql_count = "select count(*) from " +
                        Points.Pref + "_supg:act p, " +
                        Points.Pref + "_supg:act_obj o, " +
                        "outer(" + Points.Pref + "_kernel:supplier sp), " +
                        Points.Pref + "_kernel:services sv, " +
                        Points.Pref + "_data:dom d, " +
                        Points.Pref + "_data:kvar k " +
                        "where p.nzp_act=o.nzp_act " +
                        "and p.nzp_supp=sp.nzp_supp " +
                        "and p.nzp_serv=sv.nzp_serv " +
                        "and o.nzp_dom=d.nzp_dom " +
                        "and d.nzp_dom=k.nzp_dom " +
                        "and k.nzp_kvar= " + finder.nzp_kvar + " " +
                        "and (o.nzp_ul=d.nzp_ul) "+
                        "and (o.nzp_dom=d.nzp_dom or o.nzp_dom=0) " +
                        "and (o.nzp_kvar=k.nzp_kvar or o.nzp_kvar=0)";
#endif
            object obj = ExecScalar(con_db, sql_count, out ret, true);
            if (!ret.result)
            {
                con_db.Close();
                return null;
            }
            int quantity;
            try { quantity = Convert.ToInt32(obj); }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка в функции GetKvarActs:\n" + (ex.Message != "" ? ex.Message : ret.text), MonitorLog.typelog.Error, true);
                con_db.Close();
                return null;
            }

            #endregion

#if PG
            sql = "select distinct " +
                    " p.nzp_act, " +
                    " p.number, " +
                    " p._date, " +
                    " p.plan_number, " +
                    " p.plan_date, " +
                    " p.nzp_supp, " +
                    " sp.name_supp supplier, " +
                    " p.nzp_serv, " +
                    " sv.service, " +
                    " p.dat_s, " +
                    " p.dat_po, " +
                    " p.comment, " +
                    " p.is_actual " +
                    " from " +
                    Points.Pref + "_supg.act_obj o, " +
                    Points.Pref + "_supg.act p " +

                    "LEFT OUTER JOIN " + Points.Pref + "_kernel.supplier as  sp  on p.nzp_supp=sp.nzp_supp, " +
                    Points.Pref + "_kernel.services sv, " +
                    Points.Pref + "_data.dom d, " +
                    Points.Pref + "_data.kvar k " +
                    "where p.nzp_act=o.nzp_act " +
                  
                    "and p.nzp_serv=sv.nzp_serv " +
                    "and d.nzp_dom=k.nzp_dom " +
                    "and k.nzp_kvar= " + finder.nzp_kvar + " " +
                    "and (o.nzp_ul=d.nzp_ul " +
                    "and (o.nzp_dom=d.nzp_dom or o.nzp_dom=0) " +
                    "and (o.nzp_kvar=k.nzp_kvar or o.nzp_kvar=0)) " +
                    "order by p.dat_po desc";
#else
            sql =   "select distinct " + 
                    " p.nzp_act, " +
                    " p.number, " +
                    " p._date, " +
                    " p.plan_number, " +
                    " p.plan_date, " + 
                    " p.nzp_supp, " +
                    " sp.name_supp supplier, " + 
                    " p.nzp_serv, " +
                    " sv.service, " + 
                    " p.dat_s, " +
                    " p.dat_po, " +
                    " p.comment, " +
                    " p.is_actual " + 
                    " from " +
                    Points.Pref + "_supg:act p, " +
                    Points.Pref + "_supg:act_obj o, " + 
                    "outer(" + Points.Pref +"_kernel:supplier sp), " + 
                    Points.Pref +"_kernel:services sv, " +
                    Points.Pref + "_data:dom d, " +
                    Points.Pref + "_data:kvar k " + 
                    "where p.nzp_act=o.nzp_act " + 
                    "and p.nzp_supp=sp.nzp_supp " + 
                    "and p.nzp_serv=sv.nzp_serv " +
                    "and d.nzp_dom=k.nzp_dom " +
                    "and k.nzp_kvar= " + finder.nzp_kvar + " " +
                    "and (o.nzp_ul=d.nzp_ul " +
                    "and (o.nzp_dom=d.nzp_dom or o.nzp_dom=0) " + 
                    "and (o.nzp_kvar=k.nzp_kvar or o.nzp_kvar=0)) "+
                    "order by p.dat_po desc";
#endif

            IDataReader reader;
            ret = ExecRead(con_db, out reader, sql, true);
            if (!ret.result)
            {
                con_db.Close();
                return null;
            }

            SupgAct act;
            List<SupgAct> list = new List<SupgAct>();
            try
            {
                int i = 0, count = 0;
                while (reader.Read())
                {
                    i++;
                    if (i < finder.skip) continue;
                    act = new SupgAct();
                    if (reader["nzp_act"] != DBNull.Value) act.nzp_act = Convert.ToInt32(reader["nzp_act"]);
                    if (reader["number"] != DBNull.Value) act.number = reader["number"].ToString().Trim();
                    if (reader["_date"] != DBNull.Value) act._date = Convert.ToDateTime(reader["_date"]).ToShortDateString();
                    if (reader["plan_number"] != DBNull.Value) act.plan_number = reader["plan_number"].ToString().Trim();
                    if (reader["plan_date"] != DBNull.Value) act.plan_date = Convert.ToDateTime(reader["plan_date"]).ToShortDateString();
                    if (reader["nzp_supp"] != DBNull.Value) act.nzp_supp = Convert.ToInt32(reader["nzp_supp"]);
                    if (reader["supplier"] != DBNull.Value) act.supplier = reader["supplier"].ToString().Trim();
                    if (reader["nzp_serv"] != DBNull.Value) act.nzp_serv = Convert.ToInt32(reader["nzp_serv"]);
                    if (reader["service"] != DBNull.Value) act.service = reader["service"].ToString().Trim();
                    if (reader["dat_s"] != DBNull.Value) act.dat_s = Convert.ToDateTime(reader["dat_s"]).ToString("dd.MM.yyyy HH:mm");
                    if (reader["dat_po"] != DBNull.Value) act.dat_po = Convert.ToDateTime(reader["dat_po"]).ToString("dd.MM.yyyy HH:mm");
                    list.Add(act);
                    count++;
                    if (count >= finder.rows) break;
                }
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка в функции GetKvarActs:\n" + (ex.Message != "" ? ex.Message : ret.text), MonitorLog.typelog.Error, true);
            }
            finally
            {
                CloseReader(ref reader);
                con_db.Close();
            }
            if (ret.result) ret.tag = quantity;
            return list;
        }


        //1.1. 
        public List<ZvkFinder> GetInfoFromService(SupgFinder finder, enSrvOper en, out Returns ret)
        {
            ret = Utils.InitReturns();
            IDbConnection con_web = null;
            IDbConnection con_db = null;
            IDataReader reader = null;
            StringBuilder sql = new StringBuilder();
            string temp_table_name = "tmpReport1_" + finder.nzp_user;

            List<ZvkFinder> res = new List<ZvkFinder>();
            List<_Point> points = Points.PointList;

            try
            {
                #region Открытие соединения с БД

                con_web = GetConnection(Constants.cons_Webdata);
                con_db = GetConnection(Constants.cons_Kernel);

                ret = OpenDb(con_web, true);
                if (ret.result)
                {
                    ret = OpenDb(con_db, true);
                }
                if (!ret.result)
                {
                    MonitorLog.WriteLog("ExcelReport : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    return null;
                }

                #endregion

                ExecSQL(con_db, "Drop table " + temp_table_name, false);

                #region цикл по префиксам + создание временной таблицы

                //создание временной таблицы
#if PG
                sql.Remove(0, sql.Length);
                sql.Append(" create unlogged table " + temp_table_name + " (" +
                           " type       integer, " +
                           " nzp_ztype  integer, " +
                           " nzp_slug   integer, " +
                           " nzp_dest   integer, " +
                           " nzp_serv   integer, " +
                           " cnt        integer " +
                           " ) "
                          );
#else
  sql.Remove(0, sql.Length);
                sql.Append(" create temp table " + temp_table_name + " (" +
                           " type       integer, " +
                           " nzp_ztype  integer, " +
                           " nzp_slug   integer, " + 
                           " nzp_dest   integer, " +
                           " nzp_serv   integer, " +
                           " cnt        integer " +
                           " ) with no log "
                          );
#endif

                ret = ExecSQL(con_db, sql.ToString(), true);

                //проверка создания
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка создания временной таблицы " + temp_table_name + " : " + ret.text, MonitorLog.typelog.Error, true);
                    return null;
                }

                //foreach (_Point point in points)
                //{
                #region заполнение временной таблицы

                sql.Remove(0, sql.Length);

                #region первая вставка

#if PG
                sql.Append(
                                       " insert into " + temp_table_name + " (type, nzp_ztype, cnt) " +
                                       " select " +
                                       " 1, " +
                                       " z.nzp_ztype, " +
                                       " count(*) " +
                                       " from public. t" + finder.nzp_user + "_supg t, " +
                                       Points.Pref + "_supg. zvk z " +
                                       " where " +
                                       " t.nzp_zvk=z.nzp_zvk " +
                                       " and z.nzp_res=1 " +
                                       " group by 1,2 "
                                               );
#else
 sql.Append(
                        " insert into " + temp_table_name + " (type, nzp_ztype, cnt) " + 
                        " select " +
                        " 1, " + 
                        " z.nzp_ztype, " + 
                        " count(*) " +  
                        " from " +
                        con_web.Database + "@" + DBManager.getServer(con_web) + ": t" + finder.nzp_user + "_supg t, " +
                        Points.Pref + "_supg: zvk z " + 
                        " where " +  
                        " t.nzp_zvk=z.nzp_zvk " +  
                        " and z.nzp_res=1 " +  
                        " group by 1,2 "
                                );
#endif

                ret = ExecSQL(con_db, sql.ToString(), true);


                //проверка на успех создания
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка во время заполения временной таблицы " + temp_table_name + " : " + ret.text, MonitorLog.typelog.Error, true);
                    return null;
                }

                #endregion

                #region вторая вставка

                sql.Remove(0, sql.Length);
#if PG
                sql.Append(
                                                " insert into " + temp_table_name + " (type, nzp_ztype, nzp_slug, cnt) " +
                                                " select 1, " +
                                                " (case when r.nzp_slug in (1,9) then z.nzp_ztype else null end), " +
                                                " r.nzp_slug, " +
                                                " count(*) " +
                                                " from public. t" + finder.nzp_user + "_supg t, " +
                                                Points.Pref + "_supg. zvk z, " +
                                                Points.Pref + "_supg. readdress r " +
                                                " where " +
                                                " t.nzp_zvk=z.nzp_zvk  " +
                                                " and t.nzp_zvk=r.nzp_zvk " +
                                                " group by 1,2,3;"
                                                );
#else
sql.Append(
                                " insert into " + temp_table_name + " (type, nzp_ztype, nzp_slug, cnt) " +  
                                " select 1, " +
                                " (case when r.nzp_slug in (1,9) then z.nzp_ztype else null end), " +
                                " r.nzp_slug, " +
                                " count(*) " +  
                                " from " +
                                con_web.Database + "@" + DBManager.getServer(con_web) + ": t" + finder.nzp_user + "_supg t, " +
                                Points.Pref + "_supg: zvk z, " +
                                Points.Pref + "_supg: readdress r " +  
                                " where " +  
                                " t.nzp_zvk=z.nzp_zvk  " +
                                " and t.nzp_zvk=r.nzp_zvk " +  
                                " group by 1,2,3;"
                                );
#endif
                ret = ExecSQL(con_db, sql.ToString(), true);


                //проверка на успех создания
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка во время заполения временной таблицы " + temp_table_name + " : " + ret.text, MonitorLog.typelog.Error, true);
                    return null;
                }

                #endregion

                #region третья вставка

                sql.Remove(0, sql.Length);
#if PG
                sql.Append(
                                                " insert into " + temp_table_name + " (type, cnt) " +
                                                " select 2, " +
                                                " count(*) " +
                                                " from public. t" + finder.nzp_user + "_supg t, " +
                                                Points.Pref + "_supg. zakaz zk, " +
                                                Points.Pref + "_supg. zvk z " +
                                                "where " +
                                                "t.nzp_zvk=z.nzp_zvk " +
                                                "and t.nzp_zvk=zk.nzp_zvk " +
                                                "group by 1;"
                                                );
#else
sql.Append(
                                " insert into " + temp_table_name + " (type, cnt) " + 
                                " select 2, " +
                                " count(*) " +
                                " from " +
                                con_web.Database + "@" + DBManager.getServer(con_web) + ": t" + finder.nzp_user + "_supg t, " +
                                Points.Pref + "_supg: zakaz zk, " +
                                Points.Pref + "_supg: zvk z " +
                                "where " +
                                "t.nzp_zvk=z.nzp_zvk " +
                                "and t.nzp_zvk=zk.nzp_zvk " +
                                "group by 1;"
                                );
#endif
                ret = ExecSQL(con_db, sql.ToString(), true);


                //проверка на успех создания
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка во время заполения временной таблицы " + temp_table_name + " : " + ret.text, MonitorLog.typelog.Error, true);
                    return null;
                }

                #endregion

                #region чертвертая вставка

                sql.Remove(0, sql.Length);
#if PG
                sql.Append(
                                              " insert into " + temp_table_name + " (type, nzp_dest, nzp_serv, cnt) " +
                                              " select 3, " +
                                              " (case when d.nzp_serv=1000 then d.nzp_dest else null end), " +
                                              " d.nzp_serv, " +
                                              " count(*) " +
                                              " from public. t" + finder.nzp_user + "_supg t, " +
                                              Points.Pref + "_supg. zakaz zk, " +
                                              Points.Pref + "_supg. zvk z, " +
                                              Points.Pref + "_supg. s_dest d " +
                                              " where " +
                                              " t.nzp_zvk=z.nzp_zvk  " +
                                              " and t.nzp_zvk=zk.nzp_zvk " +
                                              " and zk.nzp_dest=d.nzp_dest " +
                                              " group by 1,2,3;"
                                              );
#else
  sql.Append(
                                " insert into " + temp_table_name + " (type, nzp_dest, nzp_serv, cnt) " + 
                                " select 3, " + 
                                " (case when d.nzp_serv=1000 then d.nzp_dest else null end), " +  
                                " d.nzp_serv, " + 
                                " count(*) " + 
                                " from " +
                                con_web.Database + "@" + DBManager.getServer(con_web) + ": t" + finder.nzp_user + "_supg t, " +
                                Points.Pref + "_supg: zakaz zk, " +
                                Points.Pref + "_supg: zvk z, " +
                                Points.Pref + "_supg: s_dest d " + 
                                " where " + 
                                " t.nzp_zvk=z.nzp_zvk  " + 
                                " and t.nzp_zvk=zk.nzp_zvk " +
                                " and zk.nzp_dest=d.nzp_dest " + 
                                " group by 1,2,3;"
                                );
#endif
                ret = ExecSQL(con_db, sql.ToString(), true);


                //проверка на успех создания
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка во время заполения временной таблицы " + temp_table_name + " : " + ret.text, MonitorLog.typelog.Error, true);
                    return null;
                }

                #endregion

                #endregion
                //}

                #region выборка данных для отчета

                sql.Remove(0, sql.Length);
#if PG
                sql.Append(
                                    " select t.type, " +
                                    " ((case when t.type=2 then 'Итого по услугам ЖКХ всего.' else '' end)||  " +
                                    " (case when t.nzp_ztype is not null then trim(zt.zvk_type)|| " +
                                    " (case when t.nzp_slug is not null then ' (' else '' end) else '' end)||  " +
                                    " (case when t.nzp_slug is not null then trim(sl.slug_name) else '' end)|| " +
                                    " (case when t.nzp_ztype is not null and t.nzp_slug is not null then ')' else '' end)|| " +
                                    " (case when t.nzp_dest is not null then d.dest_name else '' end)||  " +
                                    " (case when coalesce(t.nzp_serv,1000) <>1000 then sv.service else '' end)) as name,  " +
                                    " sum(t.cnt) cnt " +

                                    " FROM " + 
	                                temp_table_name + " t " + 
	                                " LEFT OUTER JOIN " + Points.Pref + "_supg.s_zvktype zt ON t.nzp_ztype = zt.nzp_ztype " +  
	                                " LEFT OUTER JOIN " + Points.Pref + "_supg.s_slug sl ON t.nzp_slug = sl.nzp_slug " + 
	                                " LEFT OUTER JOIN " + Points.Pref + "_supg.s_dest d ON t.nzp_dest = d.nzp_dest " + 
	                                "LEFT OUTER JOIN " + Points.Pref + "_kernel.services sv ON t.nzp_serv = sv.nzp_serv " + 
                                    " GROUP BY " + 
	                                " 1, " + 
	                                " 2 " + 
                                    " ORDER BY " + 
	                                " 1, " + 
	                                " 2");
#else
sql.Append(
                    " select t.type, " + 
                    " (case when t.type=2 then 'Итого по услугам ЖКХ всего:' else '' end)||  " +
                    " (case when t.nzp_ztype is not null then trim(zt.zvk_type)|| " +
                    " (case when t.nzp_slug is not null then ' (' else '' end) else '' end)||  " +
                    " (case when t.nzp_slug is not null then trim(sl.slug_name) else '' end)|| " +
                    " (case when t.nzp_ztype is not null and t.nzp_slug is not null then ')' else '' end)|| " +
                    " (case when t.nzp_dest is not null then d.dest_name else '' end)||  " +
                    " (case when nvl(t.nzp_serv,1000) <>1000 then sv.service else '' end) name,  " +
                    " sum(t.cnt) cnt " +
                    " from " + temp_table_name + " t, " +
                    " outer(" + Points.Pref + "_supg:s_zvktype zt), " + 
                    " outer(" + Points.Pref + "_supg:s_slug sl), " + 
                    " outer(" + Points.Pref + "_supg:s_dest d), " + 
                    " outer(" + Points.Pref + "_kernel:services sv) " +
                    " where " +
                    " t.nzp_ztype=zt.nzp_ztype and " +
                    " t.nzp_slug=sl.nzp_slug and " +
                    " t.nzp_dest=d.nzp_dest and " +
                    " t.nzp_serv=sv.nzp_serv " +
                    " group by 1,2 " +
                    " order by 1,2" 
                    );
#endif
                ret = ExecRead(con_db, out reader, sql.ToString(), true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка выборки данных " + temp_table_name + " " + ret.text, MonitorLog.typelog.Error, 20, 201, true);
                    con_db.Close();
                    con_web.Close();
                    return null;
                }

                #region запись выгрузки

                try
                {
                    if (reader != null)
                    {
                        while (reader.Read())
                        {
                            ZvkFinder item = new ZvkFinder();
                            if (reader["type"] != DBNull.Value) item.type = Convert.ToString(reader["type"]).Trim();
                            if (reader["name"] != DBNull.Value) item.service = Convert.ToString(reader["name"]).Trim();
                            if (reader["cnt"] != DBNull.Value) item.cnt = Convert.ToString(reader["cnt"]).Trim();

                            res.Add(item);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка при записи данных в GetAppInfoFromService " + ex.Message, MonitorLog.typelog.Error, true);
                    con_db.Close();
                    reader.Close();
                    return null;
                }
                #endregion

                #endregion

                return res;

                #endregion

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetAppInfoFromService : " + ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }
            finally
            {
                #region Закрытие соединений
                //удаляем временную таблицу
#if PG
                ExecSQL(con_db, " Drop table " + temp_table_name + "; ", false);
#else
ExecSQL(con_db, " Drop table " + temp_table_name + "; ", false);
#endif
                if (con_db != null)
                {
                    con_db.Close();
                }

                if (con_web != null)
                {
                    con_web.Close();
                }

                if (reader != null)
                {
                    reader.Close();
                }

                sql.Remove(0, sql.Length);

                #endregion
            }
        }

        //1.2.
        public List<ZvkFinder> GetAppInfoFromService(SupgFinder finder, enSrvOper en, out Returns ret)
        {
            ret = Utils.InitReturns();
            IDbConnection con_web = null;
            IDbConnection con_db = null;
            IDataReader reader = null;
            StringBuilder sql = new StringBuilder();
            string temp_table_name = "tmpReport2_" + finder.nzp_user;

            List<ZvkFinder> res = new List<ZvkFinder>();
            List<_Point> points = Points.PointList;

            try
            {
                #region Открытие соединения с БД

                con_web = GetConnection(Constants.cons_Webdata);
                con_db = GetConnection(Constants.cons_Kernel);

                ret = OpenDb(con_web, true);
                if (ret.result)
                {
                    ret = OpenDb(con_db, true);
                }
                if (!ret.result)
                {
                    MonitorLog.WriteLog("ExcelReport : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    return null;
                }

                #endregion

                ExecSQL(con_db, "Drop table " + temp_table_name, false);

                #region цикл по префиксам + создание временной таблицы

                //создание временной таблицы
                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" create unlogged table " + temp_table_name + " (" +
                                          " nzp_zvk        integer, " +
                                          " zvk_date       timestamp, " +
                                          " comment        text, " +
                                          " addr           varchar(255), " +
                                          " demand_name    char(50), " +
                                          " phone          char(50), " +
                                          " nzp_slug       integer, " +
                                          " r_comment      varchar(255), " +
                                          " result_comment varchar(255) " +
                                          " )"
                                         );
#else
 sql.Append(" create temp table " + temp_table_name + " (" +
                           " nzp_zvk        integer, " +
                           " zvk_date       datetime year to second, " +
                           " comment        text, " +
                           " addr           varchar(255), " +
                           " demand_name    char(25), " +
                           " phone          char(25), " +
                           " nzp_slug       integer, " +
                           " r_comment      varchar(255), " +
                           " result_comment varchar(255) " +
                           " ) with no log"
                          );
#endif

                ret = ExecSQL(con_db, sql.ToString(), true);

                //проверка создания
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка создания временной таблицы " + temp_table_name + " : " + ret.text, MonitorLog.typelog.Error, true);
                    return null;
                }

                //foreach (_Point point in points)
                //{
                #region заполнение временной таблицы

                sql.Remove(0, sql.Length);

#if PG
                sql.Append(
                                           " insert  into  " + temp_table_name + " (nzp_zvk, zvk_date, addr, demand_name, comment, phone, nzp_slug, r_comment, result_comment) " +
                                           " select " +
                                           " z.nzp_zvk , " +
                                           " z.zvk_date , " +
                                           "'ул.'||trim(u.ulica)||(case when d.ndom is null then ' ' else ' д.'||trim(d.ndom)|| " +
                                           "(case when d.nkor='-' then ' ' else  ' корп.'||trim(d.nkor) end) end)||" +
                                           "(case when k.nkvar is null then ' ' else ' кв.'||trim(k.nkvar)|| " +
                                           "(case when trim(k.nkvar_n)='-' then ' ' else ' комн.'||trim(k.nkvar_n) end) end ) addr, " +
                                           " z.demand_name, z.comment, replace(z.phone,'-','') phone, " +
                                           " r.nzp_slug, r.comment, trim(coalesce(r.result_comment,''))||' '||trim(coalesce(z.result_comment,'')) result_comment " +
                                           " FROM " +
                                           " kama1_supg.readdress r " +
                                           " LEFT OUTER JOIN " + Points.Pref + "_supg.zvk z ON z.nzp_zvk = r.nzp_zvk " +
                                           " LEFT OUTER JOIN " + Points.Pref + "_data.kvar K ON z.nzp_kvar = K .nzp_kvar " +
                                           " LEFT OUTER JOIN " + Points.Pref + "_data.dom d ON K .nzp_dom = d.nzp_dom " +
                                           " LEFT OUTER JOIN " + Points.Pref + "_data.s_ulica u ON d.nzp_ul = u.nzp_ul " +
                                           " LEFT OUTER JOIN PUBLIC.t3_supg s ON z.nzp_zvk = s.nzp_zvk;");
#else
 sql.Append(
                            " insert  into  " + temp_table_name + " (nzp_zvk, zvk_date, addr, demand_name, comment, phone, nzp_slug, r_comment, result_comment) " +
                            " select " +
                            " z.nzp_zvk , " +
                            " z.zvk_date , " +
                            "'ул.'||trim(u.ulica)||(case when d.ndom is null then ' ' else ' д.'||trim(d.ndom)|| " +
                            "(case when d.nkor='-' then ' ' else  ' корп.'||trim(d.nkor) end) end)||" +
                            "(case when k.nkvar is null then ' ' else ' кв.'||trim(k.nkvar)|| " +
                            "(case when trim(k.nkvar_n)='-' then ' ' else ' комн.'||trim(k.nkvar_n) end) end ) addr, " +
                            " z.demand_name, z.comment, replace(z.phone,'-','') phone, " +
                            " r.nzp_slug, r.comment, trim(nvl(r.result_comment,''))||' '||trim(nvl(z.result_comment,'')) result_comment " +
                            " from " +
                            Points.Pref + "_supg: zvk z, " +
                            Points.Pref + "_supg: readdress r, " +
                            "outer(" + Points.Pref + "_data: kvar k, "+
                            Points.Pref + "_data:dom d, "+
                            Points.Pref + "_data:s_ulica u), " +
                            con_web.Database + "@" + DBManager.getServer(con_web) + ": t" + finder.nzp_user + "_supg s " +
                            " where " +
                            " z.nzp_zvk = s.nzp_zvk "+
                            " and z.nzp_zvk=r.nzp_zvk" + 
                            " and z.nzp_kvar=k.nzp_kvar "+
                            " and k.nzp_dom=d.nzp_dom "+
                            " and d.nzp_ul=u.nzp_ul "
                                    );
#endif
                ret = ExecSQL(con_db, sql.ToString(), true);

                //проверка на успех создания
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка во время заполения временной таблицы " + temp_table_name + " : " + ret.text, MonitorLog.typelog.Error, true);
                    return null;
                }

                #endregion
                //}

                #region выборка данных для отчета

                sql.Remove(0, sql.Length);
#if PG
                sql.Append(
                                    " select " +
                                    " sl.slug_name, " +
                                    " t1.nzp_zvk, " +
                                    " t1.zvk_date, " +
                                    " t1.demand_name, " +
                                    " t1.phone, " +
                                    " t1.r_comment, " +
                                    " t1.result_comment, " +
                                    " t1.addr, " +
                                    " t1.comment " +
                                    " from " + temp_table_name + " t1, " +
                                    Points.Pref + "_supg.s_slug sl " +
                                    " where " +
                                    " t1.nzp_slug=sl.nzp_slug " +
                                    " order by slug_name, nzp_zvk"
                                    );
#else
sql.Append(
                    " select " +
                    " sl.slug_name, " +
                    " t1.nzp_zvk, " +
                    " t1.zvk_date, " +
                    " t1.demand_name, " +
                    " t1.phone, " +
                    " t1.r_comment, " +
                    " t1.result_comment, " +
                    " t1.addr, " +
                    " t1.comment " +
                    " from " + temp_table_name + " t1, " +
                    Points.Pref + "_supg:s_slug sl " +
                    " where " +
                    " t1.nzp_slug=sl.nzp_slug " +
                    " order by slug_name, nzp_zvk"
                    );
#endif

                ret = ExecRead(con_db, out reader, sql.ToString(), true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка выборки данных " + temp_table_name + " " + ret.text, MonitorLog.typelog.Error, 20, 201, true);
                    con_db.Close();
                    con_web.Close();
                    return null;
                }

                #region запись выгрузки

                try
                {
                    if (reader != null)
                    {
                        while (reader.Read())
                        {
                            ZvkFinder item = new ZvkFinder();
                            if (reader["slug_name"] != DBNull.Value) item.slug_name = Convert.ToString(reader["slug_name"]).Trim();
                            if (reader["nzp_zvk"] != DBNull.Value) item.nzp_zvk = Convert.ToInt32(reader["nzp_zvk"].ToString().Trim());
                            if (reader["zvk_date"] != DBNull.Value) item.zvk_date = Convert.ToString(reader["zvk_date"]).Trim();
                            if (reader["demand_name"] != DBNull.Value) item.fio = Convert.ToString(reader["demand_name"]).Trim();
                            if (reader["phone"] != DBNull.Value) item.phone = Convert.ToString(reader["phone"]).Trim();
                            if (reader["r_comment"] != DBNull.Value) item.r_comment = Convert.ToString(reader["r_comment"]).Trim();
                            if (reader["result_comment"] != DBNull.Value) item.result_comment = Convert.ToString(reader["result_comment"]).Trim();
                            if (reader["addr"] != DBNull.Value) item.adr = Convert.ToString(reader["addr"]).Trim();
                            if (reader["comment"] != DBNull.Value) item.comment = Convert.ToString(reader["comment"]).Trim();

                            res.Add(item);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка при записи данных в GetAppInfoFromService " + ex.Message, MonitorLog.typelog.Error, true);
                    con_db.Close();
                    reader.Close();
                    return null;
                }
                #endregion

                #endregion

                return res;

                #endregion

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetAppInfoFromService : " + ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }
            finally
            {
                #region Закрытие соединений
                //удаляем временную таблицу
                ExecSQL(con_db, " Drop table " + temp_table_name + "; ", false);

                if (con_db != null)
                {
                    con_db.Close();
                }

                if (con_web != null)
                {
                    con_web.Close();
                }

                if (reader != null)
                {
                    reader.Close();
                }

                sql.Remove(0, sql.Length);

                #endregion
            }
        }

        //1.4.
        public List<ZvkFinder> GetJoborderPeriodOutstand(SupgFinder finder, enSrvOper en, out Returns ret)
        {
            ret = Utils.InitReturns();
            IDbConnection con_web = null;
            IDbConnection con_db = null;
            IDataReader reader = null;
            StringBuilder sql = new StringBuilder();
            string temp_table_name = "tmpReport3_" + finder.nzp_user;

            List<ZvkFinder> res = new List<ZvkFinder>();
            List<_Point> points = Points.PointList;

            try
            {
                #region Открытие соединения с БД

                con_web = GetConnection(Constants.cons_Webdata);
                con_db = GetConnection(Constants.cons_Kernel);

                ret = OpenDb(con_web, true);
                if (ret.result)
                {
                    ret = OpenDb(con_db, true);
                }
                if (!ret.result)
                {
                    MonitorLog.WriteLog("ExcelReport : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    return null;
                }

                #endregion

                ExecSQL(con_db, "Drop table " + temp_table_name, false);


                #region цикл по префиксам + создание временной таблицы

                //создание временной таблицы
                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" create unlogged table " + temp_table_name + " (" +
                                          " demand_name    char(100), " +
                                          " phone          char(100), " +
                                          " nzp_zk         integer, " +
                                          " order_date     timestamp, " +
                                          " address        char(200), " +
                                          " nzp_res        integer, " +
                                          " nzp_dest       integer, " +
                                          " nzp_supp       integer, " +
                                          " fact_date      timestamp, " +
                                          " control_date   timestamp " +
                                            " ) "
                                         );
#else
 sql.Append(" create temp table " + temp_table_name + " (" +
                           " demand_name    char(25), " +
                           " phone          char(25), " +
                           " nzp_zk         integer, " +
                           " order_date     datetime year to minute, " +
                           " address        char(200), " +
                           " nzp_res        integer, " +
                           " nzp_dest       integer, " +
                           " nzp_supp       integer, " +
                           " fact_date      datetime year to minute, " +
                           " control_date   datetime year to minute " +
                             " ) with no log"
                          );
#endif

                ret = ExecSQL(con_db, sql.ToString(), true);

                //проверка создания
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка создания временной таблицы " + temp_table_name + " : " + ret.text, MonitorLog.typelog.Error, true);
                    return null;
                }

                #region заполнение временной таблицы

                // выяснить, выполнялся ли запрос по нарядам-заказам (ExSelZk = true: выполнялся)
                bool ExSelZk = false;
                sql.Remove(0, sql.Length);
#if PG
                sql.Append("select count(*) cnt from public. t" + finder.nzp_user + "_supg " + " where nzp_zk is not null");
#else
                sql.Append("select count(*) cnt from " + con_web.Database + "@" + DBManager.getServer(con_web) + ": t" + finder.nzp_user + "_supg " + " where nzp_zk is not null");
#endif
                if (ExecRead(con_db, out reader, sql.ToString(), true).result)
                {
                    if (reader != null)
                    {
                        if (reader.Read())
                            if (reader["cnt"] != DBNull.Value) ExSelZk = (Convert.ToInt32(reader["cnt"]) > 0);

                    }
                }
                else
                {
                    MonitorLog.WriteLog("Ошибка выборки GetJoborderPeriodOutstand" + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return null;
                }

                sql.Remove(0, sql.Length);
#if PG
                sql.Append(
                                       " insert  into " + temp_table_name + " (nzp_zk, address, demand_name, phone, " +
                                       " order_date,  nzp_res,  fact_date,  control_date, " +
                                       " nzp_dest, nzp_supp) " +
                                       " select " +
                                       " t.nzp_zk, " +
                                       " t.adr, " +
                                       " z.demand_name, replace(z.phone,'-','') phone, " +
                                       " zk.order_date, zk.nzp_res, zk.fact_date, zk.control_date, " +
                                       " zk.nzp_dest, zk.nzp_supp " +
                                       " from " +
                                       "public.t" + finder.nzp_user + "_supg t, " +
                                       Points.Pref + "_supg. zvk z, " + Points.Pref + "_supg. zakaz zk ");
                if (ExSelZk)
                {
                    sql.Append(
                        " where " +
                        " t.nzp_zvk=z.nzp_zvk " +
                        " and zk.nzp_zk=t.nzp_zk " +
                        " and zk.nzp_plan_no is null and zk.nzp_res in (3,5,2) " +
                        " and (date(zk.fact_date)> '" + finder._date_to + "' or zk.fact_date is null)"
                        );
                }
                else
                {
                    sql.Append(
                        " where " +
                        " t.nzp_zvk=z.nzp_zvk " +
                        " and zk.nzp_zvk=z.nzp_zvk " +
                        " and zk.nzp_plan_no is null and zk.nzp_res in (3,5,2) " +
                        " and (date(zk.fact_date)> '" + finder._date_to + "' or zk.fact_date is null)"
                        );
                }
#else
 sql.Append(
                        " insert  into " + temp_table_name + " (nzp_zk, address, demand_name, phone, " +  
                        " order_date,  nzp_res,  fact_date,  control_date, " +  
                        " nzp_dest, nzp_supp) " +  
                        " select " +  
                        " t.nzp_zk, " +
                        " t.adr, " +  
                        " z.demand_name, replace(z.phone,'-','') phone, " +    
                        " zk.order_date, zk.nzp_res, zk.fact_date, zk.control_date, " +  
                        " zk.nzp_dest, zk.nzp_supp " +  
                        " from " +
                        con_web.Database + "@" + DBManager.getServer(con_web) + ": t" + finder.nzp_user + "_supg t, " +
                        Points.Pref + "_supg: zvk z, " + Points.Pref + "_supg: zakaz zk " );
                if (ExSelZk)
                {
                    sql.Append(
                        " where " +
                        " t.nzp_zvk=z.nzp_zvk " +
                        " and zk.nzp_zk=t.nzp_zk " +
                        " and zk.nzp_plan_no is null and zk.nzp_res in (3,5,2) " +
                        " and (date(zk.fact_date)> '" + finder._date_to + "' or zk.fact_date is null)"
                        );
                }
                else
                {
                    sql.Append(
                        " where " +
                        " t.nzp_zvk=z.nzp_zvk " +
                        " and zk.nzp_zvk=z.nzp_zvk " +
                        " and zk.nzp_plan_no is null and zk.nzp_res in (3,5,2) " +
                        " and (date(zk.fact_date)> '" + finder._date_to + "' or zk.fact_date is null)"
                        );
                }
#endif
                ret = ExecSQL(con_db, sql.ToString(), true);

                //проверка на успех создания
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка во время заполения временной таблицы " + temp_table_name + " : " + ret.text, MonitorLog.typelog.Error, true);
                    return null;
                }

                #endregion

                #region выборка данных для отчета

                sql.Remove(0, sql.Length);
#if PG
                sql.Append(
                                   " select " +
                                   " case when (v.service is null or v.service='-') then ds.dest_name else v.service end service, " +
                                   " t.nzp_zk, t.address, " +
                                   " t.demand_name, replace(t.phone,'-','') phone, " +
                                   " t.order_date, rs.res_name result, t.fact_date, t.control_date, " +
                                   " ds.nzp_serv, " +
                                   " sp.name_supp " +


                                   " from " + temp_table_name + " t " +
                                   "left outer join " + Points.Pref + "_kernel.supplier sp ON T .nzp_supp = sp.nzp_supp, " +
                                   Points.Pref + "_supg.s_dest ds " +
                                   " LEFT OUTER JOIN " + Points.Pref + "_kernel.services v ON ds.nzp_serv = v.nzp_serv," + 
                                   Points.Pref + "_supg.s_result rs " +
                                   " where " +
                                   " t.nzp_dest=ds.nzp_dest " +                                                                     
                                   " and t.nzp_res=rs.nzp_res " +
                                   " order by name_supp, service, nzp_zk");
#else
 sql.Append(
                    " select " + 
                    " case when (v.service is null or v.service='-') then ds.dest_name else v.service end service, " + 
                    " t.nzp_zk, t.address, " + 
                    " t.demand_name, replace(t.phone,'-','') phone, " + 
                    " t.order_date, rs.res_name result, t.fact_date, t.control_date, " + 
                    " ds.nzp_serv, " + 
                    " sp.name_supp " + 
                    " from " + temp_table_name + " t, " +
                    " outer(" + Points.Pref + "_kernel:services v), outer(" + Points.Pref + "_kernel:supplier sp), " + Points.Pref + "_supg:s_dest ds, " + Points.Pref + "_supg:s_result rs " + 
                    " where " + 
                    " t.nzp_dest=ds.nzp_dest " + 
                    " and ds.nzp_serv=v.nzp_serv " + 
                    " and t.nzp_supp=sp.nzp_supp " +
                    " and t.nzp_res=rs.nzp_res " + 
                    " order by name_supp, service, nzp_zk");
#endif

                ret = ExecRead(con_db, out reader, sql.ToString(), true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка выборки данных " + temp_table_name + " " + ret.text, MonitorLog.typelog.Error, 20, 201, true);
                    con_db.Close();
                    con_web.Close();
                    return null;
                }

                #region запись выгрузки

                try
                {
                    if (reader != null)
                    {
                        while (reader.Read())
                        {
                            ZvkFinder item = new ZvkFinder();
                            if (reader["service"] != DBNull.Value) item.service = Convert.ToString(reader["service"]).Trim();
                            if (reader["nzp_zk"] != DBNull.Value) item.nzp_zk = Convert.ToString(reader["nzp_zk"]).Trim();
                            if (reader["address"] != DBNull.Value) item.adr = Convert.ToString(reader["address"]).Trim();
                            if (reader["demand_name"] != DBNull.Value) item.fio = Convert.ToString(reader["demand_name"]).Trim();
                            if (reader["phone"] != DBNull.Value) item.phone = Convert.ToString(reader["phone"]).Trim();
                            if (reader["order_date"] != DBNull.Value) item.order_date = Convert.ToString(reader["order_date"]).Trim();
                            if (reader["result"] != DBNull.Value) item.res_name = Convert.ToString(reader["result"]).Trim();
                            if (reader["fact_date"] != DBNull.Value) item.fact_date = Convert.ToString(reader["fact_date"]).Trim();
                            if (reader["control_date"] != DBNull.Value) item.control_date = Convert.ToString(reader["control_date"]).Trim();
                            if (reader["nzp_serv"] != DBNull.Value) item.nzp_serv = Convert.ToString(reader["nzp_serv"]).Trim();
                            if (reader["name_supp"] != DBNull.Value) item.name_supp = Convert.ToString(reader["name_supp"]).Trim();
                            res.Add(item);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка при записи данных в GetJoborderPeriodOutstand " + ex.Message, MonitorLog.typelog.Error, true);
                    con_db.Close();
                    reader.Close();
                    return null;
                }
                #endregion

                #endregion

                return res;

                #endregion

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetJoborderPeriodOutstand : " + ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }
            finally
            {
                #region Закрытие соединений
                //удаляем временную таблицу
                ExecSQL(con_db, " Drop table " + temp_table_name + "; ", false);

                if (con_db != null)
                {
                    con_db.Close();
                }

                if (con_web != null)
                {
                    con_web.Close();
                }

                if (reader != null)
                {
                    reader.Close();
                }

                sql.Remove(0, sql.Length);

                #endregion
            }
        }
    }
}
