using System;
using System.Collections.Generic;
using System.Data;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using System.Text;
using System.IO;
using SevenZip;
using System.Data.Odbc;
using System.Data.OleDb;
using JCS;
using STCLINE.KP50.Utility;
using System.Text.RegularExpressions;


namespace STCLINE.KP50.DataBase
{
    public partial class DbAdmin : DbAdminClient
    {

#if PG
        //private readonly string pgDefaultDb = "public";
#else
#endif

        /// <summary>
        /// получение кол-ва жильцов
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<DownloadedData> GetFileGilec(Finder finder)
        {
            ReturnsObjectType<DownloadedData> ret = new ReturnsObjectType<DownloadedData>();

            #region Запись в БД
            IDbConnection con_db = GetConnection(Constants.cons_Kernel);
            try
            {
                var t = OpenDb(con_db, true);

                if (!t.result)
                {
                    MonitorLog.WriteLog("Ошибка открытия соединения", MonitorLog.typelog.Error, true);
                    ret.result = false;
                    ret.text = " Файл не загружен, отсутствует подключение к базе данных ";
                    ret.tag = -1;
                    return ret;
                }

                //todo Postgres

                string sql = "SELECT p.*, u.* FROM (SELECT count (*) as prib_kol FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_gilec WHERE nzp_file in " +
                             " (SELECT nzp_file FROM " + Points.Pref + DBManager.sUploadAliasRest + "files_selected" +
                             " WHERE pref = '" + finder.bank + "')" +
                             " AND nzp_tkrt = 1) p, " +
                             " (SELECT count (*) as ub_kol FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_gilec " +
                                " WHERE nzp_file in" +
                             " (SELECT nzp_file FROM " + Points.Pref + DBManager.sUploadAliasRest + "files_selected" +
                             " WHERE pref = '" + finder.bank + "') AND nzp_tkrt = 2) u";

                var dt = ClassDBUtils.OpenSQL(sql, con_db);
                ret.returnsData = new DownloadedData()
                {
                    kol_prib = Convert.ToInt32(dt.resultData.Rows[0]["prib_kol"]),
                    kol_ub = Convert.ToInt32(dt.resultData.Rows[0]["ub_kol"])
                };
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetFileGilec : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.result = false;
                return ret;
            }
            finally
            {
                con_db.Close();
            }
            #endregion

            ret.result = true;
            ret.text = "Выполнено.";
            ret.tag = -1;

            return ret;
        }

        /// <summary>
        /// получение кол-ва ипу
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<DownloadedData> GetFileIpu(Finder finder)
        {
            ReturnsObjectType<DownloadedData> ret = new ReturnsObjectType<DownloadedData>();

            #region Запись в БД
            IDbConnection con_db = GetConnection(Constants.cons_Kernel);

            try
            {
                var t = OpenDb(con_db, true);

                if (!t.result)
                {
                    MonitorLog.WriteLog("Ошибка открытия соединения", MonitorLog.typelog.Error, true);
                    ret.result = false;
                    ret.text = " Файл не загружен, отсутствует подключение к базе данных ";
                    ret.tag = -1;
                    return ret;
                }

                //todo Postgres

                string sql = " SELECT count(*) as kol, sum(val_cnt) as sum" +
                             " FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_ipu" +
                             " WHERE nzp_file in" +
                                  " (SELECT nzp_file FROM " + Points.Pref + DBManager.sUploadAliasRest + "files_selected" +
                                  " WHERE pref = '" + finder.bank + "')";

                var dt = ClassDBUtils.OpenSQL(sql, con_db);
                decimal sum = 0;
                if (dt.resultData.Rows[0]["sum"] != DBNull.Value)
                    sum = Convert.ToInt32(dt.resultData.Rows[0]["sum"]);
                ret.returnsData = new DownloadedData()
                {
                    kol = Convert.ToInt32(dt.resultData.Rows[0]["kol"]),
                    sum = sum
                };
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetFileIpu : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.result = false;
                return ret;
            }
            finally
            {
                con_db.Close();
            }
            #endregion

            ret.result = true;
            ret.text = "Выполнено.";
            ret.tag = -1;

            return ret;
        }


        /// <summary>
        /// получение кол-ва ипу
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<DownloadedData> GetFileIpuP(Finder finder)
        {
            ReturnsObjectType<DownloadedData> ret = new ReturnsObjectType<DownloadedData>();

            #region Запись в БД
            IDbConnection con_db = GetConnection(Constants.cons_Kernel);

            try
            {
                var t = OpenDb(con_db, true);

                if (!t.result)
                {
                    MonitorLog.WriteLog("Ошибка открытия соединения", MonitorLog.typelog.Error, true);
                    ret.result = false;
                    ret.text = " Файл не загружен, отсутствует подключение к базе данных ";
                    ret.tag = -1;
                    return ret;
                }


                string sql = " SELECT count(*) as kol, sum(val_cnt) as sum" +
                             " FROM "+ Points.Pref + DBManager.sUploadAliasRest + "file_ipu_p" +
                             " WHERE nzp_file in" +
                                 " (SELECT nzp_file FROM " + Points.Pref + DBManager.sUploadAliasRest + "files_selected" +
                                  " WHERE pref = '" + finder.bank + "')";
                var dt = ClassDBUtils.OpenSQL(sql, con_db);
                decimal sum = 0;
                if (dt.resultData.Rows[0]["sum"] != DBNull.Value)
                    sum = Convert.ToInt32(dt.resultData.Rows[0]["sum"]);
                ret.returnsData = new DownloadedData()
                {
                    kol = Convert.ToInt32(dt.resultData.Rows[0]["kol"]),
                    sum = sum
                };
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetFileIpuP : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.result = false;
                return ret;
            }
            finally
            {
                con_db.Close();
            }
            #endregion

            ret.result = true;
            ret.text = "Выполнено.";
            ret.tag = -1;

            return ret;
        }

        /// <summary>
        /// получение кол-ва одпу
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<DownloadedData> GetFileOdpu(Finder finder)
        {
            ReturnsObjectType<DownloadedData> ret = new ReturnsObjectType<DownloadedData>();

            #region Запись в БД
            IDbConnection con_db = GetConnection(Constants.cons_Kernel);

            try
            {
                var t = OpenDb(con_db, true);

                if (!t.result)
                {
                    MonitorLog.WriteLog("Ошибка открытия соединения", MonitorLog.typelog.Error, true);
                    ret.result = false;
                    ret.text = " Файл не загружен, отсутствует подключение к базе данных ";
                    ret.tag = -1;
                    return ret;
                }

                string sql = "SELECT count(*) as kol, sum(val_cnt) as sum" +
                             " FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_odpu" +
                             " WHERE nzp_file in" +
                                  " (SELECT nzp_file FROM " + Points.Pref + DBManager.sUploadAliasRest + "files_selected" +
                                  " WHERE pref = '" + finder.bank + "')";
                var dt = ClassDBUtils.OpenSQL(sql, con_db);
                decimal sum = 0;
                if (dt.resultData.Rows[0]["sum"] != DBNull.Value)
                    sum = Convert.ToInt32(dt.resultData.Rows[0]["sum"]);
                ret.returnsData = new DownloadedData()
                {
                    kol = Convert.ToInt32(dt.resultData.Rows[0]["kol"]),
                    sum = sum
                };
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetFileOdpu : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.result = false;
                return ret;
            }
            finally
            {
                con_db.Close();
            }
            #endregion

            ret.result = true;
            ret.text = "Выполнено.";
            ret.tag = -1;

            return ret;
        }

        /// <summary>
        /// получение кол-ва одпу
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<DownloadedData> GetFileOdpuP(Finder finder)
        {
            ReturnsObjectType<DownloadedData> ret = new ReturnsObjectType<DownloadedData>();

            #region Запись в БД
            IDbConnection con_db = GetConnection(Constants.cons_Kernel);

            try
            {
                var t = OpenDb(con_db, true);

                if (!t.result)
                {
                    MonitorLog.WriteLog("Ошибка открытия соединения", MonitorLog.typelog.Error, true);
                    ret.result = false;
                    ret.text = " Файл не загружен, отсутствует подключение к базе данных ";
                    ret.tag = -1;
                    return ret;
                }

                string sql = " SELECT count(*) as kol, sum(val_cnt) as sum" +
                             " FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_odpu_p" +
                             " WHERE nzp_file in" +
                                  " (SELECT nzp_file FROM " + Points.Pref + DBManager.sUploadAliasRest + "files_selected" +
                                  " WHERE pref = '" + finder.bank + "')";
                var dt = ClassDBUtils.OpenSQL(sql, con_db);
                decimal sum = 0;
                if (dt.resultData.Rows[0]["sum"] != DBNull.Value)
                    sum = Convert.ToInt64(dt.resultData.Rows[0]["sum"]);
                ret.returnsData = new DownloadedData()
                {
                    kol = Convert.ToInt32(dt.resultData.Rows[0]["kol"]),
                    sum = sum
                };
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetFileOdpuP : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.result = false;
                return ret;
            }
            finally
            {
                con_db.Close();
            }
            #endregion

            ret.result = true;
            ret.text = "Выполнено.";
            ret.tag = -1;

            return ret;
        }

        /// <summary>
        /// получение кол-ва недопоставок
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<DownloadedData> GetFileNedopost(Finder finder)
        {
            ReturnsObjectType<DownloadedData> ret = new ReturnsObjectType<DownloadedData>();

            #region Запись в БД
            IDbConnection con_db = GetConnection(Constants.cons_Kernel);

            try
            {
                var t = OpenDb(con_db, true);

                if (!t.result)
                {
                    MonitorLog.WriteLog("Ошибка открытия соединения", MonitorLog.typelog.Error, true);
                    ret.result = false;
                    ret.text = " Файл не загружен, отсутствует подключение к базе данных ";
                    ret.tag = -1;
                    return ret;
                }

                //todo Postgres

                string sql = " SELECT count(*) as kol, sum(sum_ned) as sum" +
                             " FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_nedopost" +
                             " WHERE nzp_file in" +
                                  " (SELECT nzp_file FROM " + Points.Pref + DBManager.sUploadAliasRest + "files_selected" +
                                  " WHERE pref = '" + finder.bank + "')";

                var dt = ClassDBUtils.OpenSQL(sql, con_db);
                decimal sum = 0;
                if (dt.resultData.Rows[0]["sum"] != DBNull.Value)
                    sum = Convert.ToInt32(dt.resultData.Rows[0]["sum"]);
                ret.returnsData = new DownloadedData()
                {
                    kol = Convert.ToInt32(dt.resultData.Rows[0]["kol"]),
                    sum = sum
                };
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetFileNedopost : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.result = false;
                return ret;
            }
            finally
            {
                con_db.Close();
            }
            #endregion

            ret.result = true;
            ret.text = "Выполнено.";
            ret.tag = -1;

            return ret;
        }

        /// <summary>
        /// получение кол-ва одпу
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<DownloadedData> GetFileOplats(Finder finder)
        {
            ReturnsObjectType<DownloadedData> ret = new ReturnsObjectType<DownloadedData>();

            #region Запись в БД
            IDbConnection con_db = GetConnection(Constants.cons_Kernel);

            try
            {
                var t = OpenDb(con_db, true);

                if (!t.result)
                {
                    MonitorLog.WriteLog("Ошибка открытия соединения", MonitorLog.typelog.Error, true);
                    ret.result = false;
                    ret.text = " Файл не загружен, отсутствует подключение к базе данных ";
                    ret.tag = -1;
                    return ret;
                }

                //todo Postgres

                string sql = " SELECT count(*) as kol, sum(sum_oplat) as sum" +
                             " FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_oplats" +
                             " WHERE nzp_file in" +
                                  " (SELECT nzp_file FROM " + Points.Pref + DBManager.sUploadAliasRest + "files_selected" +
                                  " WHERE pref = '" + finder.bank + "')";

                var dt = ClassDBUtils.OpenSQL(sql, con_db);
                decimal sum = 0;
                if (dt.resultData.Rows[0]["sum"] != DBNull.Value)
                    sum = Convert.ToInt32(dt.resultData.Rows[0]["sum"]);
                ret.returnsData = new DownloadedData()
                {
                    kol = Convert.ToInt32(dt.resultData.Rows[0]["kol"]),
                    sum = sum
                };
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetFileOplats : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.result = false;
                return ret;
            }
            finally
            {
                con_db.Close();
            }
            #endregion

            ret.result = true;
            ret.text = "Выполнено.";
            ret.tag = -1;

            return ret;
        }

        /// <summary>
        /// получение кол-ва одпу
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<DownloadedData> GetFileParamDom(Finder finder)
        {
            ReturnsObjectType<DownloadedData> ret = new ReturnsObjectType<DownloadedData>();

            #region Запись в БД
            IDbConnection con_db = GetConnection(Constants.cons_Kernel);

            try
            {
                var t = OpenDb(con_db, true);

                if (!t.result)
                {
                    MonitorLog.WriteLog("Ошибка открытия соединения", MonitorLog.typelog.Error, true);
                    ret.result = false;
                    ret.text = " Файл не загружен, отсутствует подключение к базе данных ";
                    ret.tag = -1;
                    return ret;
                }

                //todo Postgres

                string sql = " SELECT count(*) as kol" +
                             " FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_paramsdom" +
                             " WHERE nzp_file in" +
                                  " (SELECT nzp_file FROM " + Points.Pref + DBManager.sUploadAliasRest + "files_selected" +
                                  " WHERE pref = '" + finder.bank + "')";

                var dt = ClassDBUtils.OpenSQL(sql, con_db);
                ret.returnsData = new DownloadedData()
                {
                    kol = Convert.ToInt32(dt.resultData.Rows[0]["kol"]),
                };
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetFileParamDom : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.result = false;
                return ret;
            }
            finally
            {
                con_db.Close();
            }
            #endregion

            ret.result = true;
            ret.text = "Выполнено.";
            ret.tag = -1;

            return ret;
        }

        /// <summary>
        /// получение кол-ва одпу
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<DownloadedData> GetFileParamLs(Finder finder)
        {
            ReturnsObjectType<DownloadedData> ret = new ReturnsObjectType<DownloadedData>();

            #region Запись в БД
            IDbConnection con_db = GetConnection(Constants.cons_Kernel);

            try
            {
                var t = OpenDb(con_db, true);

                if (!t.result)
                {
                    MonitorLog.WriteLog("Ошибка открытия соединения", MonitorLog.typelog.Error, true);
                    ret.result = false;
                    ret.text = " Файл не загружен, отсутствует подключение к базе данных ";
                    ret.tag = -1;
                    return ret;
                }

                //todo Postgres

                string sql = " SELECT count(*) as kol" +
                             " FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_paramsls" +
                             " WHERE nzp_file in" +
                                  " (SELECT nzp_file FROM " + Points.Pref + DBManager.sUploadAliasRest + "files_selected" +
                                  " WHERE pref = '" + finder.bank + "')";

                var dt = ClassDBUtils.OpenSQL(sql, con_db);
                ret.returnsData = new DownloadedData()
                {
                    kol = Convert.ToInt32(dt.resultData.Rows[0]["kol"]),
                };
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetFileParamLs : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.result = false;
                return ret;
            }
            finally
            {
                con_db.Close();
            }
            #endregion

            ret.result = true;
            ret.text = "Выполнено.";
            ret.tag = -1;

            return ret;
        }

        /// <summary>
        /// получение кол-ва одпу
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<DownloadedData> GetFileTypeNedop(Finder finder)
        {
            ReturnsObjectType<DownloadedData> ret = new ReturnsObjectType<DownloadedData>();

            #region Запись в БД
            IDbConnection con_db = GetConnection(Constants.cons_Kernel);

            try
            {
                var t = OpenDb(con_db, true);

                if (!t.result)
                {
                    MonitorLog.WriteLog("Ошибка открытия соединения", MonitorLog.typelog.Error, true);
                    ret.result = false;
                    ret.text = " Файл не загружен, отсутствует подключение к базе данных ";
                    ret.tag = -1;
                    return ret;
                }

                //todo Postgres

                string sql = " SELECT count(*) as kol" +
                             " FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_typenedopost" +
                             " WHERE nzp_file in" +
                                  " (SELECT nzp_file FROM " + Points.Pref + DBManager.sUploadAliasRest + "files_selected" +
                                  " WHERE pref = '" + finder.bank + "')";

                var dt = ClassDBUtils.OpenSQL(sql, con_db);
                ret.returnsData = new DownloadedData()
                {
                    kol = Convert.ToInt32(dt.resultData.Rows[0]["kol"]),
                };
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetFileTypeNedop : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.result = false;
                return ret;
            }
            finally
            {
                con_db.Close();
            }
            #endregion

            ret.result = true;
            ret.text = "Выполнено.";
            ret.tag = -1;

            return ret;
        }

        /// <summary>
        /// получение кол-ва одпу
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<DownloadedData> GetFileTypeParams(Finder finder)
        {
            ReturnsObjectType<DownloadedData> ret = new ReturnsObjectType<DownloadedData>();

            #region Запись в БД
            IDbConnection con_db = GetConnection(Constants.cons_Kernel);

            try
            {
                var t = OpenDb(con_db, true);

                if (!t.result)
                {
                    MonitorLog.WriteLog("Ошибка открытия соединения", MonitorLog.typelog.Error, true);
                    ret.result = false;
                    ret.text = " Файл не загружен, отсутствует подключение к базе данных ";
                    ret.tag = -1;
                    return ret;
                }

                //todo Postgres

                string sql = " SELECT count(*) as kol" +
                             " FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_typeparams" +
                             " WHERE nzp_file in" +
                                  " (SELECT nzp_file FROM " + Points.Pref + DBManager.sUploadAliasRest + "files_selected" +
                                  " WHERE pref = '" + finder.bank + "')";

                var dt = ClassDBUtils.OpenSQL(sql, con_db);
                ret.returnsData = new DownloadedData()
                {
                    kol = Convert.ToInt32(dt.resultData.Rows[0]["kol"]),
                };
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetFileTypeParams : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.result = false;
                return ret;
            }
            finally
            {
                con_db.Close();
            }
            #endregion

            ret.result = true;
            ret.text = "Выполнено.";
            ret.tag = -1;

            return ret;
        }

        /// <summary>
        /// получение nzp_kvar
        /// </summary>

        public Returns GetNZP_Kvar(int intStreetID, string strHomeNo, int intFlatNo)
        {
            Returns retData = Utils.InitReturns();
            #region Create and execute SQL query.
            IDbConnection con_db = GetConnection(Constants.cons_Kernel);

            try
            {
                var t = OpenDb(con_db, true);

                if (!t.result)
                {
                    MonitorLog.WriteLog("Ошибка открытия соединения", MonitorLog.typelog.Error, true);
                    retData.result = false;
                    retData.text = " Файл не загружен, отсутствует подключение к базе данных ";
                    retData.tag = -1;
                    return retData;
                }


                string strQuery = String.Format("SELECT MIN({0}_data" + tableDelimiter + "kvar.nzp_kvar) AS _NZP_KVAR" +
                                                " FROM {0}_data" + tableDelimiter + "kvar, {0}_data" + tableDelimiter + "dom" +
                                                " WHERE {0}_data" + tableDelimiter + "dom.nzp_dom = {0}_data" + tableDelimiter + "kvar.nzp_dom" +
                                                " AND {0}_data" + tableDelimiter + "dom.nzp_ul = {1} AND TRIM(UPPER({0}_data" + tableDelimiter + "dom.ndom)) = '{2}'" +
                                                " AND {0}_data" + tableDelimiter + "kvar.nkvar = {3};", Points.Pref, intStreetID, strHomeNo, intFlatNo);

                DataTable dtResult = ClassDBUtils.OpenSQL(strQuery, con_db, ClassDBUtils.ExecMode.Exception).GetData();
                retData.result = true;
                retData.text = Convert.ToString(dtResult.Rows[0]["_NZP_KVAR"]);
                retData.tag = 0;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("GetNZP_Kvar\n" + ex.Message, MonitorLog.typelog.Error, true);
                retData.result = false;
                retData.text = ex.Message;
            }
            finally
            {
                con_db.Close();
            }
            #endregion
            return retData;
        }


        /// <summary>
        /// получение несопоставленных участков
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<List<ServFormulFinder>> GetServFormul(Finder finder)
        {

            ReturnsObjectType<List<ServFormulFinder>> ret = new ReturnsObjectType<List<ServFormulFinder>>();

            #region Запись в БД
            IDbConnection con_db = GetConnection(Constants.cons_Kernel);

            try
            {
                var t = OpenDb(con_db, true);
                string sql = "";

                if (!t.result)
                {
                    MonitorLog.WriteLog("Ошибка открытия соединения", MonitorLog.typelog.Error, true);
                    ret.result = false;
                    ret.text = " Файл не загружен, отсутствует подключение к базе данных ";
                    ret.tag = -1;
                    return ret;
                }

                try
                {
                    sql = "drop table tmp_serv; ";
                    ClassDBUtils.ExecSQL(sql, con_db);
                }
                catch (Exception)
                {
                }

                //создаем временную таблицу
                sql = " create temp table tmp_serv ( nzp_serv integer, service char(100), ed_izmer char(100), nzp_ops integer, nzp_frm integer, nzp_measure integer, nzp_prm_tarif_ls integer, " +
                        " nzp_prm_tarif_lsp integer, nzp_prm_tarif_dm integer, " +
                        " nzp_prm_tarif_su integer, nzp_prm_tarif_bd integer, nzp_supp integer, name_supp char(100), name_frm char(100), toChange integer ) ";
                ClassDBUtils.ExecSQL(sql, con_db);

                //определяем префиксы
                sql = " select distinct pref from " + Points.Pref + DBManager.sUploadAliasRest + "rust_load where pref is not null and pref <> '' ";
                var tdt = ClassDBUtils.OpenSQL(sql, con_db);
                //заполняем временную таблицу
                foreach (DataRow tr in tdt.resultData.Rows)
                {
                    sql = "insert into tmp_serv " +
                         " select distinct b.nzp_serv, service, ed_izmer, nzp_ops, aa.nzp_frm, b.nzp_measure, nzp_prm_tarif_ls, nzp_prm_tarif_lsp, " +
                        " nzp_prm_tarif_dm, nzp_prm_tarif_su, nzp_prm_tarif_bd, t.nzp_supp, supp.name_supp, a.name_frm,  " +
                        " (case when (select count(*) from " + Points.Pref + DBManager.sUploadAliasRest + "file_serv_tuning where nzp_serv = b.nzp_serv and nzp_supp = supp.nzp_supp and " +
                        " nzp_measure = b.nzp_measure and nzp_frm = a.nzp_frm) <> 0 then 1 else 0 end) as toChange  " +
                    " from  " + tr["pref"] + "_kernel" + tableDelimiter + "formuls_opis aa, " + tr["pref"] + "_kernel" + tableDelimiter + "formuls a, " +
                    Points.Pref + "_kernel" + tableDelimiter + "services b, " + tr["pref"] + "_kernel" + tableDelimiter + "l_foss t, " +
                        Points.Pref + DBManager.sUploadAliasRest + "file_serv fs, " + Points.Pref + "_kernel" + tableDelimiter + "supplier supp   " +
                        " where a.nzp_measure=b.nzp_measure  and t.nzp_serv=b.nzp_serv and t.nzp_frm =a.nzp_frm and a.nzp_frm<>1  and a.nzp_frm=aa.nzp_frm  and fs.nzp_serv = b.nzp_serv " +
                        " and supp.nzp_supp = t.nzp_supp and  fs.nzp_file in (select nzp_file from " + Points.Pref + DBManager.sUploadAliasRest + "rust_load where pref is not null and pref = '" + tr["pref"] + "') ";
                    ClassDBUtils.ExecSQL(sql, con_db);
                }
#if PG
                string skip = finder.skip >= 0 && finder.rows >= 0 ? " offset " + finder.skip + " limit " + finder.rows : String.Empty;
#else
                string skip = finder.skip >= 0 && finder.rows >= 0 ? " skip " + finder.skip + " first " + finder.rows : String.Empty;
#endif
                sql = " select * from tmp_serv; ";

                var dt = ClassDBUtils.OpenSQL(sql, con_db);
                List<ServFormulFinder> list = new List<ServFormulFinder>();
                for (int i = 0; i < dt.resultData.Rows.Count; i++)
                {
                    list.Add(new ServFormulFinder()
                    {
                        serv_name = dt.resultData.Rows[i]["service"].ToString().Trim(),
                        supplier_name = dt.resultData.Rows[i]["name_supp"].ToString().Trim(),
                        measure_name = dt.resultData.Rows[i]["ed_izmer"].ToString().Trim(),
                        formul_name = dt.resultData.Rows[i]["name_frm"].ToString().Trim(),
                        toChange = dt.resultData.Rows[i]["toChange"].ToString().Trim() == "1" ? true : false,
                        nzp_serv = Convert.ToInt32(dt.resultData.Rows[i]["nzp_serv"]),
                        nzp_measure = Convert.ToInt32(dt.resultData.Rows[i]["nzp_measure"]),
                        nzp_supp = Convert.ToInt32(dt.resultData.Rows[i]["nzp_supp"]),
                        nzp_frm = Convert.ToInt32(dt.resultData.Rows[i]["nzp_frm"])
                    });
                    if (PmaxVisible < i) { break; }
                }
                ret.returnsData = list;

                Returns retVar = new Returns();
#if PG
                sql = " select count(*) from (select distinct upper(area) as area from " + Points.Pref + "_data.file_area  where nzp_area is null and " +
                                                " nzp_file in (select nzp_file from " + Points.Pref + "_data.rust_load))";
#else
                sql = " select count(*) from tmp_serv ";
#endif
                object count = ExecScalar(con_db, sql, out retVar, true);
                if (retVar.result)
                {
                    ret.tag = Convert.ToInt32(count);
                }

                sql = "drop table tmp_serv; ";
                ClassDBUtils.ExecSQL(sql, con_db);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetServFormul : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.result = false;
                return ret;
            }
            finally
            {
                con_db.Close();
            }
            #endregion


            ret.result = true;
            ret.text = "Выполнено.";

            return ret;
        }

        /// <summary>
        /// сохранение в таблицу file_serv_tuning
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public Returns SetToChange(ServFormulFinder finder)
        {

            Returns ret = new Returns();

            #region Запись в БД
            IDbConnection con_db = GetConnection(Constants.cons_Kernel);

            try
            {
                var t = OpenDb(con_db, true);
                string sql = "";

                if (!t.result)
                {
                    MonitorLog.WriteLog("Ошибка открытия соединения", MonitorLog.typelog.Error, true);
                    ret.result = false;
                    ret.text = " Файл не загружен, отсутствует подключение к базе данных ";
                    ret.tag = -1;
                    return ret;
                }

                sql = " select count(*) from " + Points.Pref + DBManager.sUploadAliasRest + "file_serv_tuning where nzp_serv = " + finder.nzp_serv + " and nzp_measure = " + finder.nzp_measure + " and nzp_supp = " +
                        finder.nzp_supp + " and nzp_frm = " + finder.nzp_frm;

                var count = Convert.ToInt32(ExecScalar(con_db, sql, out ret, true));
                if (count == 0)
                {
                    sql = "insert into " + Points.Pref + DBManager.sUploadAliasRest + "file_serv_tuning (nzp_serv, nzp_measure, nzp_supp, nzp_frm) values " +
                        " ( " + finder.nzp_serv + ", " + finder.nzp_measure + ", " + finder.nzp_supp + ", " + finder.nzp_frm + " ) ";
                    ClassDBUtils.ExecSQL(sql, con_db);
                }
                else
                {
                    sql = "delete from " + Points.Pref + DBManager.sUploadAliasRest + "file_serv_tuning where nzp_serv = " + finder.nzp_serv + " and nzp_measure = " + finder.nzp_measure + " and nzp_supp = " +
                        finder.nzp_supp + " and nzp_frm = " + finder.nzp_frm;
                    ClassDBUtils.ExecSQL(sql, con_db);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры SetToChange : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.result = false;
                return ret;
            }
            finally
            {
                con_db.Close();
            }
            #endregion


            ret.result = true;
            ret.text = "Выполнено.";

            return ret;
        }




    }
}
