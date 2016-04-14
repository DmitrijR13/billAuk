using System;
using System.Collections.Generic;
using System.Data;
using Bars.KP50.DataImport.SOURCE;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.DataBase
{
    public class DbCompareFile : DbAdminClient
    {
        #region Сопоставление : Функции сопоставления

        private Int32 PmaxVisible = 200;

        #region Функции получения данных для отображения

        /// <summary>
        /// получение сопоставленных городов
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<List<ComparedAreas>> GetComparedArea(Finder finder)
        {
            var ret = new ReturnsObjectType<List<ComparedAreas>>();

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

                string sql =
                    "select distinct upper(f.area) as area_file, upper(b.area) as area_base, f.nzp_area " +
                    " from " + Points.Pref + DBManager.sUploadAliasRest + "file_area f, " + Points.Pref +
                    DBManager.sDataAliasRest + "s_area b " +
                    " where f.nzp_area = b.nzp_area and f.nzp_area is not null " +
                    " and  nzp_file in " +
                    " ( " +
                    " select nzp_file from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected " +
                    " where trim(pref)='" + finder.bank + "' and nzp_user = " + finder.nzp_user +
                    " ) ";

                var dt = ClassDBUtils.OpenSQL(sql, con_db);
                List<ComparedAreas> list = new List<ComparedAreas>();

                for (int i = 0; i < dt.resultData.Rows.Count; i++)
                {
                    list.Add(new ComparedAreas()
                    {
                        area_base = dt.resultData.Rows[i]["area_base"].ToString().Trim(),
                        area_file = dt.resultData.Rows[i]["area_file"].ToString().Trim(),
                        nzp_area = dt.resultData.Rows[i]["nzp_area"].ToString().Trim()
                    });
                    if (PmaxVisible < i)
                    {
                        break;
                    }
                }
                ret.returnsData = list;

                Returns retVar = new Returns();
                sql =
                    " select count(*) " +
                    " from (select distinct upper(f.area) as area_file, upper(b.area) as area_base, f.nzp_area " +
                    " from " + Points.Pref + DBManager.sUploadAliasRest + "file_area f, " +
                    finder.bank + DBManager.sDataAliasRest + "s_area b " +
                    " where f.nzp_area = b.nzp_area " +
                    " and f.nzp_area is not null " +
                    " and nzp_file in " +
                    " (select nzp_file from " +
                    Points.Pref + DBManager.sUploadAliasRest + "files_selected " +
                    " where trim(pref)='" + finder.bank + "' and nzp_user = " + finder.nzp_user + "))" +
#if PG
 " as foo " +
#else
#endif
                        "";
                object count = ExecScalar(con_db, sql, out retVar, true);
                if (retVar.result)
                {
                    ret.tag = Convert.ToInt32(count);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetComparedArea : " + ex.Message,
                    MonitorLog.typelog.Error, true);
                ret.text = "Ошибка выполнения";
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
        /// получение сопоставленных городов
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<List<ComparedSupps>> GetComparedSupp(Finder finder)
        {

            var ret = new ReturnsObjectType<List<ComparedSupps>>();

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

#if PG
                string skip = finder.skip >= 0 && finder.rows >= 0 ? " offset " + finder.skip + " limit " + finder.rows : String.Empty;
#else
                string skip = finder.skip >= 0 && finder.rows >= 0
                    ? " skip " + finder.skip + " first " + finder.rows
                    : String.Empty;
#endif
                string sql =
                    "select distinct upper(f.supp_name) as supp_file, upper(b.name_supp) as supp_base, f.nzp_supp " +
                    " from " + Points.Pref + DBManager.sUploadAliasRest + "file_supp f, " + finder.bank + DBManager.sKernelAliasRest + "supplier b " +
                    " where f.nzp_supp = b.nzp_supp and f.nzp_supp is not null " +
                    " and nzp_file in " +
                    " (select nzp_file from " +
                    Points.Pref + DBManager.sUploadAliasRest + "files_selected " +
                    " where trim(pref)='" + finder.bank + "' and nzp_user = "+finder.nzp_user+") ";

                var dt = ClassDBUtils.OpenSQL(sql, con_db);
                List<ComparedSupps> list = new List<ComparedSupps>();
                for (int i = 0; i < dt.resultData.Rows.Count; i++)
                {
                    list.Add(new ComparedSupps()
                    {
                        supp_base = dt.resultData.Rows[i]["supp_base"].ToString().Trim(),
                        supp_file = dt.resultData.Rows[i]["supp_file"].ToString().Trim(),
                        nzp_supp = dt.resultData.Rows[i]["nzp_supp"].ToString().Trim()
                    });
                    if (PmaxVisible < i)
                    {
                        break;
                    }
                }
                ret.returnsData = list;

                Returns retVar = new Returns();
                sql =
                    " select count(*) from (select distinct upper(f.supp_name) as supp_file, upper(b.name_supp) as supp_base, f.nzp_supp " +
                    " from " + Points.Pref + DBManager.sUploadAliasRest + "file_supp f, " + finder.bank + "_kernel" +
                    tableDelimiter + "supplier b " +
                    " where f.nzp_supp = b.nzp_supp and f.nzp_supp is not null " +
                    " and nzp_file in " +
                    " (select nzp_file from " +
                    Points.Pref + DBManager.sUploadAliasRest + "files_selected " +
                    " where trim(pref)='" + finder.bank + "' and nzp_user = "+finder.nzp_user+"))" +
#if PG
 " as foo  " +
#else
#endif
                        "";


                object count = ExecScalar(con_db, sql, out retVar, true);
                if (retVar.result)
                {
                    ret.tag = Convert.ToInt32(count);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetComparedSupp : " + ex.Message,
                    MonitorLog.typelog.Error, true);
                ret.text = "Ошибка выполнения";
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
        /// получение сопоставленных МО
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<List<ComparedVills>> GetComparedMO(Finder finder)
        {

            var ret = new ReturnsObjectType<List<ComparedVills>>();

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

#if PG
                string skip = finder.skip >= 0 && finder.rows >= 0 ? " offset " + finder.skip + " limit " + finder.rows : String.Empty;
#else
                string skip = finder.skip >= 0 && finder.rows >= 0
                    ? " skip " + finder.skip + " first " + finder.rows
                    : String.Empty;
#endif
                string sql =
                    "select distinct upper(f.vill) as vill_file, upper(b.vill) as vill_base, f.nzp_vill " +
                    " from " + Points.Pref + DBManager.sUploadAliasRest + "file_mo f, " + finder.bank + DBManager.sKernelAliasRest + "s_vill b " +
                    " where f.nzp_vill = b.nzp_vill and f.nzp_vill is not null" +
                    " and nzp_file in " +
                    " (select nzp_file from " +
                    Points.Pref + DBManager.sUploadAliasRest + "files_selected " +
                    " where trim(pref)='" + finder.bank + "' and nzp_user = " + finder.nzp_user + ") ";

                var dt = ClassDBUtils.OpenSQL(sql, con_db);
                List<ComparedVills> list = new List<ComparedVills>();
                for (int i = 0; i < dt.resultData.Rows.Count; i++)
                {
                    list.Add(new ComparedVills()
                    {
                        vill_base = dt.resultData.Rows[i]["vill_base"].ToString().Trim(),
                        vill_file = dt.resultData.Rows[i]["vill_file"].ToString().Trim(),
                        nzp_vill = dt.resultData.Rows[i]["nzp_vill"].ToString().Trim()
                    });
                    if (PmaxVisible < i)
                    {
                        break;
                    }
                }
                ret.returnsData = list;

                Returns retVar = new Returns();
                sql =
                    " select count(*) from (select distinct upper(f.vill) as vill_file, upper(b.vill) as vill_base, f.nzp_vill " +
                    " from " + Points.Pref + DBManager.sUploadAliasRest + "file_mo f, " + finder.bank + DBManager.sKernelAliasRest + "s_vill b " +
                    " where f.nzp_vill = b.nzp_vill and f.nzp_vill is not null " +
                    " and nzp_file in " +
                    " (select nzp_file from " +
                    Points.Pref + DBManager.sUploadAliasRest + "files_selected " +
                    " where trim(pref)='" + finder.bank + "' and nzp_user = "+finder.nzp_user+"))" +
#if PG
 " as foo  " +
#else
#endif
                        "";


                object count = ExecScalar(con_db, sql, out retVar, true);
                if (retVar.result)
                {
                    ret.tag = Convert.ToInt32(count);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetComparedMO : " + ex.Message,
                    MonitorLog.typelog.Error, true);
                ret.text = "Ошибка выполнения";
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
        /// получение сопоставленных счетчиков
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<List<ComparedServs>> GetComparedServ(Finder finder)
        {

            var ret = new ReturnsObjectType<List<ComparedServs>>();

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


#if PG
                string skip = finder.skip >= 0 && finder.rows >= 0 ? " offset " + finder.skip + " limit " + finder.rows : String.Empty;
#else
                string skip = finder.skip >= 0 && finder.rows >= 0
                    ? " skip " + finder.skip + " first " + finder.rows
                    : String.Empty;
#endif
                string sql =
                    "select distinct upper(f.service) as serv_file, upper(b.service) as serv_base, f.nzp_serv " +
                    " from " + Points.Pref + DBManager.sUploadAliasRest + "file_services f, " + finder.bank + "_kernel" +
                    tableDelimiter + "services b " +
                    " where f.nzp_serv = b.nzp_serv and f.nzp_serv is not null " +
                     " and nzp_file in " +
                    " (select nzp_file from " +
                    Points.Pref + DBManager.sUploadAliasRest + "files_selected " +
                    " where trim(pref)='" + finder.bank + "' and nzp_user = " + finder.nzp_user + ") ";

                var dt = ClassDBUtils.OpenSQL(sql, con_db);
                List<ComparedServs> list = new List<ComparedServs>();
                for (int i = 0; i < dt.resultData.Rows.Count; i++)
                {
                    list.Add(new ComparedServs()
                    {
                        serv_base = dt.resultData.Rows[i]["serv_base"].ToString().Trim(),
                        serv_file = dt.resultData.Rows[i]["serv_file"].ToString().Trim(),
                        nzp_serv = dt.resultData.Rows[i]["nzp_serv"].ToString().Trim()
                    });
                    if (PmaxVisible < i)
                    {
                        break;
                    }
                }
                ret.returnsData = list;

                Returns retVar = new Returns();
                sql =
                    " select count(*) from (select distinct upper(f.service) as serv_file, upper(b.service) as serv_base, f.nzp_serv " +
                    " from " + Points.Pref + DBManager.sUploadAliasRest + "file_services f, " + finder.bank + DBManager.sKernelAliasRest + "services b " +
                    " where f.nzp_serv = b.nzp_serv and f.nzp_serv is not null " +
                     " and nzp_file in " +
                    " (select nzp_file from " +
                    Points.Pref + DBManager.sUploadAliasRest + "files_selected " +
                    " where trim(pref)='" + finder.bank + "' and nzp_user = " + finder.nzp_user + "))" +
#if PG
 " as foo  " +
#else
#endif
                        "";


                object count = ExecScalar(con_db, sql, out retVar, true);
                if (retVar.result)
                {
                    ret.tag = Convert.ToInt32(count);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetComparedServ : " + ex.Message,
                    MonitorLog.typelog.Error, true);
                ret.text = "Ошибка выполнения";
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
        /// получение сопоставленных единиц измерения
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<List<ComparedMeasures>> GetComparedMeasure(Finder finder)
        {

            var ret = new ReturnsObjectType<List<ComparedMeasures>>();

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

#if PG
                string skip = finder.skip >= 0 && finder.rows >= 0 ? " offset " + finder.skip + " limit " + finder.rows : String.Empty;
#else
                string skip = finder.skip >= 0 && finder.rows >= 0
                    ? " skip " + finder.skip + " first " + finder.rows
                    : String.Empty;
#endif
                string sql =
                    "select distinct upper(f.measure) as measure_file, upper(b.measure_long) as measure_base, f.nzp_measure " +
                    " from " + Points.Pref + DBManager.sUploadAliasRest + "file_measures f, " + finder.bank + DBManager.sKernelAliasRest + "s_measure b " +
                    " where f.nzp_measure = b.nzp_measure and f.nzp_measure is not null " +
                    " and nzp_file in " +
                    " (select nzp_file from " +
                    Points.Pref + DBManager.sUploadAliasRest + "files_selected " +
                    " where trim(pref)='" + finder.bank + "' and nzp_user = " + finder.nzp_user + ") ";

                var dt = ClassDBUtils.OpenSQL(sql, con_db);
                List<ComparedMeasures> list = new List<ComparedMeasures>();
                for (int i = 0; i < dt.resultData.Rows.Count; i++)
                {
                    list.Add(new ComparedMeasures()
                    {
                        measure_base = dt.resultData.Rows[i]["measure_base"].ToString().Trim(),
                        measure_file = dt.resultData.Rows[i]["measure_file"].ToString().Trim(),
                        nzp_measure = dt.resultData.Rows[i]["nzp_measure"].ToString().Trim()
                    });
                    if (PmaxVisible < i)
                    {
                        break;
                    }
                }
                ret.returnsData = list;

                Returns retVar = new Returns();
                sql =
                    " select count(*) from (select distinct upper(f.measure) as measure_file, upper(b.measure_long) as measure_base, f.nzp_measure " +
                    " from " + Points.Pref + DBManager.sUploadAliasRest + "file_measures f, " + finder.bank + "_kernel" +
                    tableDelimiter + "s_measure b " +
                    " where f.nzp_measure = b.nzp_measure and f.nzp_measure is not null" +
                     " and nzp_file in " +
                    " (select nzp_file from " +
                    Points.Pref + DBManager.sUploadAliasRest + "files_selected " +
                    " where trim(pref)='" + finder.bank + "' and nzp_user = " + finder.nzp_user + ") " +
                    " and f.nzp_measure is not null)" +
#if PG
 " as foo  " +
#else
#endif
                        "";


                object count = ExecScalar(con_db, sql, out retVar, true);
                if (retVar.result)
                {
                    ret.tag = Convert.ToInt32(count);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetComparedMesaure : " + ex.Message,
                    MonitorLog.typelog.Error, true);
                ret.text = "Ошибка выполнения";
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
        /// получение сопоставленных типов доп. параметров
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<List<ComparedParTypes>> GetComparedParType(Finder finder)
        {

            var ret = new ReturnsObjectType<List<ComparedParTypes>>();

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

                string sql =
                    "select distinct upper(a.prm_name) as name_prm_file, upper(name_prm) as name_prm_base, a.nzp_prm as id_prm " +
                    " from " + Points.Pref + DBManager.sUploadAliasRest + "file_typeparams a, " + finder.bank + DBManager.sKernelAliasRest + "prm_name b " +
                    " where a.nzp_prm = b.nzp_prm and a.nzp_prm is not null " +
                    " and nzp_file in " +
                    " (select nzp_file from " +
                    Points.Pref + DBManager.sUploadAliasRest + "files_selected " +
                    " where trim(pref)='" + finder.bank + "' and nzp_user = " + finder.nzp_user + ") ";
                var dt = ClassDBUtils.OpenSQL(sql, con_db);
                List<ComparedParTypes> list = new List<ComparedParTypes>();
                for (int i = 0; i < dt.resultData.Rows.Count; i++)
                {
                    list.Add(new ComparedParTypes()
                    {
                        name_prm_file = dt.resultData.Rows[i]["name_prm_file"].ToString().Trim(),
                        name_prm_base = dt.resultData.Rows[i]["name_prm_base"].ToString().Trim(),
                        nzp_prm = dt.resultData.Rows[i]["id_prm"].ToString().Trim()
                    });
                }
                ret.returnsData = list;
                Returns retVar = new Returns();
                sql =
                    " select count(*) from (select distinct upper(a.prm_name) as name_prm_file, upper(name_prm) as name_prm_base, a.nzp_prm as id_prm " +
                    " from " + Points.Pref + DBManager.sUploadAliasRest + "file_typeparams a, " + finder.bank +DBManager.sKernelAliasRest + "prm_name b " +
                    " where a.nzp_prm = b.nzp_prm and a.nzp_prm is not null " +
                     " and nzp_file in " +
                    " (select nzp_file from " +
                    Points.Pref + DBManager.sUploadAliasRest + "files_selected " +
                    " where trim(pref)='" + finder.bank + "' and nzp_user = " + finder.nzp_user + ")) " +
#if PG
 " as foo  " +
#else
#endif
                        "";


                object count = ExecScalar(con_db, sql, out retVar, true);
                if (retVar.result)
                {
                    ret.tag = Convert.ToInt32(count);
                }

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetComparedParType : " + ex.Message,
                    MonitorLog.typelog.Error, true);
                ret.text = "Ошибка выполнения";
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
        /// получение сопоставленных домов по благоустройству
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<List<ComparedParTypes>> GetComparedParBlag(Finder finder)
        {

            var ret = new ReturnsObjectType<List<ComparedParTypes>>();

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

#if PG
                string skip = finder.skip >= 0 && finder.rows >= 0 ? " offset " + finder.skip + " limit " + finder.rows : String.Empty;
#else
                string skip = finder.skip >= 0 && finder.rows >= 0
                    ? " skip " + finder.skip + " first " + finder.rows
                    : String.Empty;
#endif
                string sql =
                    "select distinct upper(f.name) as name_prm_file, upper(b.name_prm) as name_prm_base, f.nzp_prm " +
                    " from " + Points.Pref + DBManager.sUploadAliasRest + "file_blag f, " + finder.bank +
                    DBManager.sKernelAliasRest + "prm_name b " +
                    " where f.nzp_prm = b.nzp_prm and f.nzp_prm is not null " + 
                    " and nzp_file in " +
                    " (select nzp_file from " +
                    Points.Pref + DBManager.sUploadAliasRest + "files_selected " +
                    " where trim(pref)='" + finder.bank + "' and nzp_user = " + finder.nzp_user + ") ";

                var dt = ClassDBUtils.OpenSQL(sql, con_db);
                List<ComparedParTypes> list = new List<ComparedParTypes>();
                for (int i = 0; i < dt.resultData.Rows.Count; i++)
                {
                    list.Add(new ComparedParTypes()
                    {
                        name_prm_file = dt.resultData.Rows[i]["name_prm_file"].ToString().Trim(),
                        name_prm_base = dt.resultData.Rows[i]["name_prm_base"].ToString().Trim(),
                        nzp_prm = dt.resultData.Rows[i]["nzp_prm"].ToString().Trim()
                    });
                    if (PmaxVisible < i)
                    {
                        break;
                    }
                }
                ret.returnsData = list;

                Returns retVar = new Returns();
                sql =
                    " select count(*) from (select distinct upper(f.name) as name_prm_file, upper(b.name_prm) as name_prm_base, f.nzp_prm " +
                    " from " + Points.Pref + DBManager.sUploadAliasRest + "file_blag f, " + finder.bank + "_kernel" +
                    tableDelimiter + "prm_name b " +
                    " where f.nzp_prm = b.nzp_prm and f.nzp_prm is not null " +
                    " and nzp_file in " +
                    " (select nzp_file from " +
                    Points.Pref + DBManager.sUploadAliasRest + "files_selected " +
                    " where trim(pref)='" + finder.bank + "' and nzp_user = "+finder.nzp_user+") )" +
#if PG
 " as foo  " +
#else
#endif
                        "";


                object count = ExecScalar(con_db, sql, out retVar, true);
                if (retVar.result)
                {
                    ret.tag = Convert.ToInt32(count);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetComparedParBlag : " + ex.Message,
                    MonitorLog.typelog.Error, true);
                ret.text = "Ошибка выполнения";
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
        /// получение сопоставленных домов по газу
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<List<ComparedParTypes>> GetComparedParGas(Finder finder)
        {

            var ret = new ReturnsObjectType<List<ComparedParTypes>>();

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

#if PG
                string skip = finder.skip >= 0 && finder.rows >= 0 ? " offset " + finder.skip + " limit " + finder.rows : String.Empty;
#else
                string skip = finder.skip >= 0 && finder.rows >= 0
                    ? " skip " + finder.skip + " first " + finder.rows
                    : String.Empty;
#endif
                string sql =
                    "select distinct upper(f.name) as name_prm_file, upper(b.name_prm) as name_prm_base, f.nzp_prm " +
                    " from " + Points.Pref + DBManager.sUploadAliasRest + "file_gaz f, " + finder.bank + DBManager.sKernelAliasRest + "prm_name b " +
                    " where f.nzp_prm = b.nzp_prm and f.nzp_prm is not null  "+
                     " and nzp_file in " +
                    " (select nzp_file from " +
                    Points.Pref + DBManager.sUploadAliasRest + "files_selected " +
                    " where trim(pref)='" + finder.bank + "' and nzp_user = " + finder.nzp_user + ") ";

                var dt = ClassDBUtils.OpenSQL(sql, con_db);
                List<ComparedParTypes> list = new List<ComparedParTypes>();
                for (int i = 0; i < dt.resultData.Rows.Count; i++)
                {
                    list.Add(new ComparedParTypes()
                    {
                        name_prm_file = dt.resultData.Rows[i]["name_prm_file"].ToString().Trim(),
                        name_prm_base = dt.resultData.Rows[i]["name_prm_base"].ToString().Trim(),
                        nzp_prm = dt.resultData.Rows[i]["nzp_prm"].ToString().Trim()
                    });
                    if (PmaxVisible < i)
                    {
                        break;
                    }
                }
                ret.returnsData = list;

                Returns retVar = new Returns();

                sql =
                    " select count(*) from (select distinct upper(f.name) as name_prm_file, upper(b.name_prm) as name_prm_base, f.nzp_prm " +
                    " from " + Points.Pref + DBManager.sUploadAliasRest + "file_gaz f, " + finder.bank + "_kernel" +
                    tableDelimiter + "prm_name b " +
                    " where f.nzp_prm = b.nzp_prm and f.nzp_prm is not null "+
                     " and nzp_file in " +
                    " (select nzp_file from " +
                    Points.Pref + DBManager.sUploadAliasRest + "files_selected " +
                    " where trim(pref)='" + finder.bank + "' and nzp_user = " + finder.nzp_user + ") )" +
#if PG
 " as foo  " +
#else
#endif
                        "";

                object count = ExecScalar(con_db, sql, out retVar, true);
                if (retVar.result)
                {
                    ret.tag = Convert.ToInt32(count);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetComparedParGas : " + ex.Message,
                    MonitorLog.typelog.Error, true);
                ret.text = "Ошибка выполнения";
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
        /// получение сопоставленных домов по воде
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<List<ComparedParTypes>> GetComparedParWater(Finder finder)
        {

            var ret = new ReturnsObjectType<List<ComparedParTypes>>();

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

#if PG
                string skip = finder.skip >= 0 && finder.rows >= 0 ? " offset " + finder.skip + " limit " + finder.rows : String.Empty;
#else
                string skip = finder.skip >= 0 && finder.rows >= 0
                    ? " skip " + finder.skip + " first " + finder.rows
                    : String.Empty;
#endif
                string sql = "select " +
#if PG
#else
                    skip +
#endif
                    " distinct upper(f.name) as name_prm_file, upper(b.name_prm) as name_prm_base, f.nzp_prm " +
                             " from " + Points.Pref + DBManager.sUploadAliasRest + "file_voda f, " + finder.bank +
                             DBManager.sKernelAliasRest + "prm_name b " +
                             " where f.nzp_prm = b.nzp_prm and f.nzp_prm is not null " +
                             " and nzp_file in " +
                             " (select nzp_file from " +
                             Points.Pref + DBManager.sUploadAliasRest + "files_selected " +
                             " where trim(pref)='" + finder.bank + "' and nzp_user = " + finder.nzp_user + ") " +
#if PG
                    skip +
#else
#endif
                    "";

                var dt = ClassDBUtils.OpenSQL(sql, con_db);
                List<ComparedParTypes> list = new List<ComparedParTypes>();
                for (int i = 0; i < dt.resultData.Rows.Count; i++)
                {
                    list.Add(new ComparedParTypes()
                    {
                        name_prm_file = dt.resultData.Rows[i]["name_prm_file"].ToString().Trim(),
                        name_prm_base = dt.resultData.Rows[i]["name_prm_base"].ToString().Trim(),
                        nzp_prm = dt.resultData.Rows[i]["nzp_prm"].ToString().Trim()
                    });
                    if (PmaxVisible < i)
                    {
                        break;
                    }
                }
                ret.returnsData = list;

                Returns retVar = new Returns();
                sql =
                    " select count(*) from (select distinct upper(f.name) as name_prm_file, upper(b.name_prm) as name_prm_base, f.nzp_prm " +
                    " from " + Points.Pref + DBManager.sUploadAliasRest + "file_voda f, " + finder.bank + "_kernel" +
                    tableDelimiter + "prm_name b " +
                    " where f.nzp_prm = b.nzp_prm and f.nzp_prm is not null"+
                    " and nzp_file in " +
                    " (select nzp_file from " +
                    Points.Pref + DBManager.sUploadAliasRest + "files_selected " +
                    " where trim(pref)='" + finder.bank + "' and nzp_user = " + finder.nzp_user + ") )" +
#if PG
 " as foo  " +
#else
#endif
                        "";
                object count = ExecScalar(con_db, sql, out retVar, true);
                if (retVar.result)
                {
                    ret.tag = Convert.ToInt32(count);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetComparedParWater : " + ex.Message,
                    MonitorLog.typelog.Error, true);
                ret.text = "Ошибка выполнения";
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
        /// получение сопоставленных городов
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<List<ComparedTowns>> GetComparedTown(Finder finder)
        {

            var ret = new ReturnsObjectType<List<ComparedTowns>>();

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

#if PG
                string skip = finder.skip >= 0 && finder.rows >= 0 ? " offset " + finder.skip + " limit " + finder.rows : String.Empty;
#else
                string skip = finder.skip >= 0 && finder.rows >= 0
                    ? " skip " + finder.skip + " first " + finder.rows
                    : String.Empty;
#endif
                string sql = "select " +
#if PG
#else
                    skip +
#endif
                    " distinct upper(f.town) as town_file, upper(town.town) as town_base, f.nzp_town " +
                             " from " + Points.Pref + DBManager.sUploadAliasRest + "file_dom f, " + Points.Pref +
                             DBManager.sDataAliasRest + "s_town town " +
                             " where f.nzp_town = town.nzp_town and f.nzp_town is not null " +
                             " and nzp_file in " +
                             " (select nzp_file from " +
                             Points.Pref + DBManager.sUploadAliasRest + "files_selected " +
                             " where trim(pref)='" + finder.bank + "' and nzp_user = " + finder.nzp_user + ") " +
#if PG
                    skip +
#else
#endif
                    "";

                var dt = ClassDBUtils.OpenSQL(sql, con_db);
                List<ComparedTowns> list = new List<ComparedTowns>();
                for (int i = 0; i < dt.resultData.Rows.Count; i++)
                {
                    list.Add(new ComparedTowns()
                    {
                        town_base = dt.resultData.Rows[i]["town_base"].ToString().Trim(),
                        town_file = dt.resultData.Rows[i]["town_file"].ToString().Trim(),
                        nzp_town = dt.resultData.Rows[i]["nzp_town"].ToString().Trim()
                    });
                    if (PmaxVisible < i)
                    {
                        break;
                    }
                }
                ret.returnsData = list;

                Returns retVar = new Returns();
                sql =
                    " select count(*) from (select distinct upper(f.town) as town_file, upper(town.town) as town_base, f.nzp_town " +
                    " from " + Points.Pref + DBManager.sUploadAliasRest + "file_dom f, " + finder.bank + "_data" +
                    tableDelimiter + "s_town town " +
                    " where f.nzp_town = town.nzp_town and f.nzp_town is not null " +
                     " and nzp_file in " +
                    " (select nzp_file from " +
                    Points.Pref + DBManager.sUploadAliasRest + "files_selected " +
                    " where trim(pref)='" + finder.bank + "' and nzp_user = " + finder.nzp_user + ")  )" +
#if PG
 " as foo  " +
#else
#endif
                        "";

                object count = ExecScalar(con_db, sql, out retVar, true);
                if (retVar.result)
                {
                    ret.tag = Convert.ToInt32(count);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetComparedTown : " + ex.Message,
                    MonitorLog.typelog.Error, true);
                ret.text = "Ошибка выполнения";
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
        /// получение сопоставленных населенных пунктов
        /// </summary>
        /// <returns>результат</returns>
        public ReturnsObjectType<List<ComparedRajons>> GetComparedRajon(Finder finder)
        {
            var ret = new ReturnsObjectType<List<ComparedRajons>>();

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


#if PG
                string skip = finder.skip >= 0 && finder.rows >= 0 ? " offset " + finder.skip + " limit " + finder.rows : String.Empty;
#else
                string skip = finder.skip >= 0 && finder.rows >= 0
                    ? " skip " + finder.skip + " first " + finder.rows
                    : String.Empty;
#endif
                string sql = "select " +
#if PG
#else
                    skip +
#endif
                    " distinct f.nzp_raj, upper(f.rajon) as rajon_file, upper(f.town) as town_file, upper(raj.rajon) as rajon_base, upper(town.town) as town_base " +
                             " from " + Points.Pref + DBManager.sUploadAliasRest + "file_dom f, " + Points.Pref +
                             "_data" + tableDelimiter + "s_rajon raj, " + Points.Pref + "_data" + tableDelimiter +
                             "s_town town " +
                             " where f.nzp_raj = raj.nzp_raj and raj.nzp_town = town.nzp_town and f.nzp_raj is not null  " +
                              " and nzp_file in " +
                    " (select nzp_file from " +
                    Points.Pref + DBManager.sUploadAliasRest + "files_selected " +
                    " where trim(pref)='" + finder.bank + "' and nzp_user = " + finder.nzp_user + ") "+
#if PG
 skip +
#else
#endif
                    "";


                var dt = ClassDBUtils.OpenSQL(sql, con_db);
                List<ComparedRajons> list = new List<ComparedRajons>();
                for (int i = 0; i < dt.resultData.Rows.Count; i++)
                {
                    var town_file = "";
                    if (dt.resultData.Rows[i]["town_file"] != DBNull.Value &&
                        dt.resultData.Rows[i]["town_file"].ToString().Trim() != "-" &&
                        dt.resultData.Rows[i]["town_file"].ToString().Trim() != "")
                        town_file = dt.resultData.Rows[i]["town_file"].ToString().Trim();

                    var town_base = "";
                    if (dt.resultData.Rows[i]["town_base"] != DBNull.Value &&
                        dt.resultData.Rows[i]["town_base"].ToString().Trim() != "-" &&
                        dt.resultData.Rows[i]["town_base"].ToString().Trim() != "")
                        town_base = dt.resultData.Rows[i]["town_base"].ToString().Trim();

                    list.Add(new ComparedRajons()
                    {
                        rajon_base = dt.resultData.Rows[i]["rajon_base"].ToString().Trim() + " (" + town_base + ")",
                        rajon_file = dt.resultData.Rows[i]["rajon_file"].ToString().Trim() + " (" + town_file + ")",
                        nzp_raj = dt.resultData.Rows[i]["nzp_raj"].ToString().Trim()
                    });
                    if (PmaxVisible < i)
                    {
                        break;
                    }
                }
                ret.returnsData = list;

                Returns retVar = new Returns();
                sql =
                    " select count(*) from (select distinct f.nzp_raj, upper(f.rajon) as rajon_file, upper(f.town) as town_file, upper(raj.rajon) as rajon_base, upper(town.town) as town_base " +
                    " from " + Points.Pref + DBManager.sUploadAliasRest + "file_dom f, " + 
                    Points.Pref + DBManager.sDataAliasRest + "s_rajon raj, " + 
                    Points.Pref + "_data" + tableDelimiter + "s_town town " +
                    " where f.nzp_raj = raj.nzp_raj and raj.nzp_town = town.nzp_town and f.nzp_raj is not null" +
                     " and nzp_file in " +
                    " (select nzp_file from " +
                    Points.Pref + DBManager.sUploadAliasRest + "files_selected " +
                    " where trim(pref)='" + finder.bank + "' and nzp_user = " + finder.nzp_user + "))" +
#if PG
 " as foo  " +
#else
#endif
                        "";

                object count = ExecScalar(con_db, sql, out retVar, true);
                if (retVar.result)
                {
                    ret.tag = Convert.ToInt32(count);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetComparedRajon : " + ex.Message,
                    MonitorLog.typelog.Error, true);
                ret.text = "Ошибка выполнения";
                ret.result = false;
                return ret;
            }
            finally
            {
                con_db.Close();
            }

            ret.result = true;
            ret.text = "Выполнено.";

            return ret;
        }

        /// <summary>
        /// получение сопоставленных улиц
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<List<ComparedStreets>> GetComparedStreets(Finder finder)
        {

            var ret = new ReturnsObjectType<List<ComparedStreets>>();

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
#if PG
                string skip = finder.skip >= 0 && finder.rows >= 0 ? " offset " + finder.skip + " limit " + finder.rows : String.Empty;
#else
                string skip = finder.skip >= 0 && finder.rows >= 0
                    ? " skip " + finder.skip + " first " + finder.rows
                    : String.Empty;
#endif

                string sql = "select " +
#if PG
#else
                    skip +
#endif
                    " distinct upper(f.ulica) as ulica_file, upper(b.ulica) as ulica_base, b.nzp_ul, " +
                             " upper(f.rajon) as rajon_file, upper(f.town) as town_file, upper(raj.rajon) as rajon_base, upper(town.town) as town_base " +
                             " from " + Points.Pref + DBManager.sUploadAliasRest + "file_dom f, " + 
                             Points.Pref + DBManager.sDataAliasRest + "s_ulica b, " + 
                             Points.Pref + DBManager.sDataAliasRest + "s_rajon raj, " + 
                             Points.Pref + "_data" + tableDelimiter + "s_town town " +
                             " where b.nzp_raj = raj.nzp_raj and raj.nzp_town = town.nzp_town and f.nzp_ul = b.nzp_ul and f.nzp_ul is not null " +
                             " and nzp_file in " +
                             " (select nzp_file from " +
                             Points.Pref + DBManager.sUploadAliasRest + "files_selected " +
                             " where trim(pref)='" + finder.bank + "' and nzp_user = " + finder.nzp_user + ") " +
#if PG
                    skip +
#else
#endif
                    "";

                var dt = ClassDBUtils.OpenSQL(sql, con_db);
                List<ComparedStreets> list = new List<ComparedStreets>();
                for (int i = 0; i < dt.resultData.Rows.Count; i++)
                {
                    var rajon_file = "";
                    if (dt.resultData.Rows[i]["rajon_file"] != DBNull.Value &&
                        dt.resultData.Rows[i]["rajon_file"].ToString().Trim() != "-" &&
                        dt.resultData.Rows[i]["rajon_file"].ToString().Trim() != "")
                        rajon_file = ", " + dt.resultData.Rows[i]["rajon_file"].ToString().Trim();
                    var town_file = "";
                    if (dt.resultData.Rows[i]["town_file"] != DBNull.Value &&
                        dt.resultData.Rows[i]["town_file"].ToString().Trim() != "-" &&
                        dt.resultData.Rows[i]["town_file"].ToString().Trim() != "")
                        town_file = dt.resultData.Rows[i]["town_file"].ToString().Trim();

                    var rajon_base = "";
                    if (dt.resultData.Rows[i]["rajon_base"] != DBNull.Value &&
                        dt.resultData.Rows[i]["rajon_base"].ToString().Trim() != "-" &&
                        dt.resultData.Rows[i]["rajon_base"].ToString().Trim() != "")
                        rajon_base = ", " + dt.resultData.Rows[i]["rajon_base"].ToString().Trim();
                    var town_base = "";
                    if (dt.resultData.Rows[i]["town_base"] != DBNull.Value &&
                        dt.resultData.Rows[i]["town_base"].ToString().Trim() != "-" &&
                        dt.resultData.Rows[i]["town_base"].ToString().Trim() != "")
                        town_base = dt.resultData.Rows[i]["town_base"].ToString().Trim();

                    list.Add(new ComparedStreets()
                    {
                        ulica_base =
                            dt.resultData.Rows[i]["ulica_base"].ToString().Trim() + " (" + town_base + rajon_base + ")",
                        ulica_file =
                            dt.resultData.Rows[i]["ulica_file"].ToString().Trim() + " (" + town_file + rajon_file + ")",
                        nzp_ul = dt.resultData.Rows[i]["nzp_ul"].ToString().Trim()
                    });
                    if (PmaxVisible < i)
                    {
                        break;
                    }
                }
                ret.returnsData = list;

                Returns retVar = new Returns();
                sql =
                    " select count(*) from (select distinct upper(f.ulica) as ulica_file, upper(b.ulica) as ulica_base, b.nzp_ul, " +
                    " upper(f.rajon) as rajon_file, upper(f.town) as town_file, upper(raj.rajon) as rajon_base, upper(town.town) as town_base " +
                    " from " + Points.Pref + DBManager.sUploadAliasRest + "file_dom f, " +
                    Points.Pref + DBManager.sDataAliasRest + "s_ulica b, " +
                    Points.Pref + DBManager.sDataAliasRest + "s_rajon raj, " +
                    Points.Pref + DBManager.sDataAliasRest + "s_town town " +
                    " where b.nzp_raj = raj.nzp_raj and raj.nzp_town = town.nzp_town and f.nzp_ul = b.nzp_ul and f.nzp_ul is not null  " +
                    " and nzp_file in " +
                    " (select nzp_file from " +
                    Points.Pref + DBManager.sUploadAliasRest + "files_selected " +
                    " where trim(pref)='" + finder.bank + "' and nzp_user = " + finder.nzp_user + ")  )" +
#if PG
                        " as foo  " +
#else
#endif
                        "";

                object count = ExecScalar(con_db, sql, out retVar, true);
                if (retVar.result)
                {
                    ret.tag = Convert.ToInt32(count);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetComparedStreets : " + ex.Message,
                    MonitorLog.typelog.Error, true);
                ret.text = "Ошибка выполнения";
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
        /// получение сопоставленных домов
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<List<ComparedHouses>> GetComparedHouse(Finder finder)
        {

            var ret = new ReturnsObjectType<List<ComparedHouses>>();

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

#if PG
                string skip = finder.skip >= 0 && finder.rows >= 0 ? " offset " + finder.skip + " limit " + finder.rows : String.Empty;
#else
                string skip = finder.skip >= 0 && finder.rows >= 0
                    ? " skip " + finder.skip + " first " + finder.rows
                    : String.Empty;
#endif
                string sql = "select " +
#if PG
#else
                    skip +
#endif
                    "  upper(f.ndom) as ndom_file, upper(f.nkor) as nkor_file, upper(b.ndom) as ndom_base, upper(b.nkor) as nkor_base, b.nzp_dom, " +
                             " upper(f.ulica) as ulica_file, upper(ul.ulica) as ulica_base, upper(f.rajon) as rajon_file, " +
                             " upper(f.town) as town_file, upper(raj.rajon) as rajon_base, upper(town.town) as town_base " +
                             " from " + Points.Pref + DBManager.sUploadAliasRest + "file_dom f, " +
                             finder.bank + DBManager.sDataAliasRest + "dom b, " +
                             Points.Pref + DBManager.sDataAliasRest + "s_ulica ul, " +
                             Points.Pref + DBManager.sDataAliasRest + "s_rajon raj, " +
                             Points.Pref + DBManager.sDataAliasRest + "s_town town " +
                             " where b.nzp_ul = ul.nzp_ul and ul.nzp_raj = raj.nzp_raj " +
                             " and raj.nzp_town = town.nzp_town " +
                             " and f.nzp_dom = b.nzp_dom and f.nzp_dom is not null " +
                             " and nzp_file in " +
                             " (select nzp_file from " +
                             Points.Pref + DBManager.sUploadAliasRest + "files_selected " +
                             " where trim(pref)='" + finder.bank + "' and nzp_user = " + finder.nzp_user + ") " +
#if PG
                    skip +
#else
#endif
                    "";

                var dt = ClassDBUtils.OpenSQL(sql, con_db);
                List<ComparedHouses> list = new List<ComparedHouses>();
                for (int i = 0; i < dt.resultData.Rows.Count; i++)
                {
                    var dom_file = dt.resultData.Rows[i]["ndom_file"].ToString().Trim();
                    if (dt.resultData.Rows[i]["nkor_file"] != DBNull.Value &&
                        dt.resultData.Rows[i]["nkor_file"].ToString().Trim() != "-" &&
                        dt.resultData.Rows[i]["nkor_file"].ToString().Trim() != "")
                        dom_file += "/" + dt.resultData.Rows[i]["nkor_file"].ToString().Trim();
                    var ulica_file = "";
                    if (dt.resultData.Rows[i]["ulica_file"] != DBNull.Value &&
                        dt.resultData.Rows[i]["ulica_file"].ToString().Trim() != "-" &&
                        dt.resultData.Rows[i]["ulica_file"].ToString().Trim() != "")
                        ulica_file = dt.resultData.Rows[i]["ulica_file"].ToString().Trim();
                    var rajon_file = "";
                    if (dt.resultData.Rows[i]["rajon_file"] != DBNull.Value &&
                        dt.resultData.Rows[i]["rajon_file"].ToString().Trim() != "-" &&
                        dt.resultData.Rows[i]["rajon_file"].ToString().Trim() != "")
                        rajon_file = ", " + dt.resultData.Rows[i]["rajon_file"].ToString().Trim();
                    var town_file = "";
                    if (dt.resultData.Rows[i]["town_file"] != DBNull.Value &&
                        dt.resultData.Rows[i]["town_file"].ToString().Trim() != "-" &&
                        dt.resultData.Rows[i]["town_file"].ToString().Trim() != "")
                        town_file = ", " + dt.resultData.Rows[i]["town_file"].ToString().Trim();

                    var dom_base = dt.resultData.Rows[i]["ndom_base"].ToString().Trim();
                    if (dt.resultData.Rows[i]["nkor_base"] != DBNull.Value &&
                        dt.resultData.Rows[i]["nkor_base"].ToString().Trim() != "-" &&
                        dt.resultData.Rows[i]["nkor_base"].ToString().Trim() != "")
                        dom_base += "/" + dt.resultData.Rows[i]["nkor_base"].ToString().Trim();
                    var ulica_base = "";
                    if (dt.resultData.Rows[i]["ulica_base"] != DBNull.Value &&
                        dt.resultData.Rows[i]["ulica_base"].ToString().Trim() != "-" &&
                        dt.resultData.Rows[i]["ulica_base"].ToString().Trim() != "")
                        ulica_base = dt.resultData.Rows[i]["ulica_base"].ToString().Trim();
                    var rajon_base = "";
                    if (dt.resultData.Rows[i]["rajon_base"] != DBNull.Value &&
                        dt.resultData.Rows[i]["rajon_base"].ToString().Trim() != "-" &&
                        dt.resultData.Rows[i]["rajon_base"].ToString().Trim() != "")
                        rajon_base = ", " + dt.resultData.Rows[i]["rajon_base"].ToString().Trim();
                    var town_base = "";
                    if (dt.resultData.Rows[i]["town_base"] != DBNull.Value &&
                        dt.resultData.Rows[i]["town_base"].ToString().Trim() != "-" &&
                        dt.resultData.Rows[i]["town_base"].ToString().Trim() != "")
                        town_base = ", " + dt.resultData.Rows[i]["town_base"].ToString().Trim();

                    list.Add(new ComparedHouses()
                    {
                        dom_base = dom_base + " (" + ulica_base + town_base + rajon_base + ")",
                        dom_file = dom_file + " (" + ulica_file + town_file + rajon_file + ")",
                        nzp_dom = dt.resultData.Rows[i]["nzp_dom"].ToString().Trim()
                    });
                }
                ret.returnsData = list;

                Returns retVar = new Returns();
                sql =
                    " select count(*) from (select  upper(f.ndom) as ndom_file, upper(f.nkor) as nkor_file, upper(b.ndom) as ndom_base, upper(b.nkor) as nkor_base, b.nzp_dom, " +
                    " upper(f.ulica) as ulica_file, upper(ul.ulica) as ulica_base, upper(f.rajon) as rajon_file, upper(f.town) as town_file, upper(raj.rajon) as rajon_base, upper(town.town) as town_base " +
                    " from " + Points.Pref + DBManager.sUploadAliasRest + "file_dom f, " +
                    finder.bank + DBManager.sDataAliasRest + "dom b, " +
                    Points.Pref + DBManager.sDataAliasRest + "s_ulica ul, " +
                    Points.Pref + DBManager.sDataAliasRest + "s_rajon raj, " +
                    Points.Pref + "_data" + tableDelimiter + "s_town town " +
                    " where b.nzp_ul = ul.nzp_ul and ul.nzp_raj = raj.nzp_raj " +
                    " and raj.nzp_town = town.nzp_town " +
                    " and f.nzp_dom = b.nzp_dom and f.nzp_dom is not null " +
                    " and nzp_file in " +
                    " (select nzp_file from " +
                    Points.Pref + DBManager.sUploadAliasRest + "files_selected " +
                    " where trim(pref)='" + finder.bank + "' and nzp_user = " + finder.nzp_user + "))" +
#if PG
                        " as foo  " +
#else
#endif
                        "";

                object count = ExecScalar(con_db, sql, out retVar, true);
                if (retVar.result)
                {
                    ret.tag = Convert.ToInt32(count);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetComparedHouse : " + ex.Message,
                    MonitorLog.typelog.Error, true);
                ret.text = "Ошибка выполнения";
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
        /// получение сопоставленных лицевых счетов
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<List<ComparedLS>> GetComparedLS(Finder finder)
        {

            var ret = new ReturnsObjectType<List<ComparedLS>>();

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

#if PG
                string skip = finder.skip >= 0 && finder.rows >= 0 ? " offset " + finder.skip + " limit " + finder.rows : String.Empty;
#else
                string skip = finder.skip >= 0 && finder.rows >= 0
                    ? " skip " + finder.skip + " first " + finder.rows
                    : String.Empty;
#endif
                string sql = "select " +
#if PG
#else
                    skip +
#endif
                             " upper(f.ndom) as ndom_file, upper(f.nkor) as nkor_file, upper(b.ndom) as ndom_base, upper(b.nkor) as nkor_base, b.nzp_dom, " +
                             " upper(f.ulica) as ulica_file, upper(ul.ulica) as ulica_base, upper(f.rajon) as rajon_file," +
                             " upper(f.town) as town_file, upper(raj.rajon) as rajon_base, upper(town.town) as town_base, " +
                             " fk.id as ls_file, fk.nzp_kvar as nzp_kvar, fk.nkvar as nkvar_file, kvar.nkvar as nkvar_base," +
                             " fk.nkvar_n as kom_file, kvar.nkvar_n as kom_base, " +
                             " upper(kvar.fio) as fio_base, upper(fk.fam) as fam_file, upper(fk.ima) as ima_file,  " +
                             " upper(fk.otch) as otch_file, kvar.num_ls as ls_base " +
                             " from " + Points.Pref + DBManager.sUploadAliasRest + "file_dom f, " +
                             finder.bank + DBManager.sDataAliasRest + "dom b, " +
                             Points.Pref + DBManager.sDataAliasRest + "s_ulica ul, " +
                             Points.Pref + DBManager.sDataAliasRest + "s_rajon raj, " +
                             Points.Pref + DBManager.sDataAliasRest + "s_town town, " +
                             Points.Pref + DBManager.sUploadAliasRest + "file_kvar fk, " +
                             finder.bank + DBManager.sDataAliasRest + "kvar kvar" +
                             " where b.nzp_ul = ul.nzp_ul and ul.nzp_raj = raj.nzp_raj " +
                             " and raj.nzp_town = town.nzp_town and " +
                             " f.nzp_dom = b.nzp_dom and f.nzp_dom is not null and " +
                             " kvar.nzp_kvar = fk.nzp_kvar and fk.nzp_kvar is not null " +
                             " and fk.nzp_dom = f.nzp_dom and fk.nzp_file = f.nzp_file and " +
                             " f.nzp_file in " +
                             " (select nzp_file from " +
                             Points.Pref + DBManager.sUploadAliasRest + "files_selected " +
                             " where trim(pref)='" + finder.bank + "' and nzp_user = " + finder.nzp_user + ")" +
                            // " order by nzp_kvar " +
#if PG
                    skip +
#else
#endif
                    "";

                var dt = ClassDBUtils.OpenSQL(sql, con_db);
                List<ComparedLS> list = new List<ComparedLS>();
                for (int i = 0; i < dt.resultData.Rows.Count; i++)
                {
                    var dom_file = dt.resultData.Rows[i]["ndom_file"].ToString().Trim();
                    if (dt.resultData.Rows[i]["nkor_file"] != DBNull.Value &&
                        dt.resultData.Rows[i]["nkor_file"].ToString().Trim() != "-" &&
                        dt.resultData.Rows[i]["nkor_file"].ToString().Trim() != "")
                        dom_file += "/" + dt.resultData.Rows[i]["nkor_file"].ToString().Trim();
                    var ulica_file = "";
                    if (dt.resultData.Rows[i]["ulica_file"] != DBNull.Value &&
                        dt.resultData.Rows[i]["ulica_file"].ToString().Trim() != "-" &&
                        dt.resultData.Rows[i]["ulica_file"].ToString().Trim() != "")
                        ulica_file = dt.resultData.Rows[i]["ulica_file"].ToString().Trim() + ", ";
                    var rajon_file = "";
                    if (dt.resultData.Rows[i]["rajon_file"] != DBNull.Value &&
                        dt.resultData.Rows[i]["rajon_file"].ToString().Trim() != "-" &&
                        dt.resultData.Rows[i]["rajon_file"].ToString().Trim() != "")
                        rajon_file = dt.resultData.Rows[i]["rajon_file"].ToString().Trim() + ", ";
                    var town_file = "";
                    if (dt.resultData.Rows[i]["town_file"] != DBNull.Value &&
                        dt.resultData.Rows[i]["town_file"].ToString().Trim() != "-" &&
                        dt.resultData.Rows[i]["town_file"].ToString().Trim() != "")
                        town_file = dt.resultData.Rows[i]["town_file"].ToString().Trim() + ", ";
                    var nkvar_file = "";
                    if (dt.resultData.Rows[i]["nkvar_file"] != DBNull.Value &&
                        /*dt.resultData.Rows[i]["nkvar_file"].ToString().Trim() != "-" &&*/
                        dt.resultData.Rows[i]["nkvar_file"].ToString().Trim() != "")
                        nkvar_file = dt.resultData.Rows[i]["nkvar_file"].ToString().Trim();
                    var fam_file = "";
                    if (dt.resultData.Rows[i]["fam_file"] != DBNull.Value &&
                        dt.resultData.Rows[i]["fam_file"].ToString().Trim() != "")
                        fam_file = ", " + dt.resultData.Rows[i]["fam_file"].ToString().Trim();
                    var ima_file = "";
                    if (dt.resultData.Rows[i]["ima_file"] != DBNull.Value &&
                        dt.resultData.Rows[i]["ima_file"].ToString().Trim() != "")
                        ima_file = " " + dt.resultData.Rows[i]["ima_file"].ToString().Trim();
                    var otch_file = "";
                    if (dt.resultData.Rows[i]["otch_file"] != DBNull.Value &&
                        dt.resultData.Rows[i]["otch_file"].ToString().Trim() != "")
                        otch_file = " " + dt.resultData.Rows[i]["otch_file"].ToString().Trim();
                    var ls_file = "";
                    if (dt.resultData.Rows[i]["ls_file"] != DBNull.Value &&
                        dt.resultData.Rows[i]["ls_file"].ToString().Trim() != "")
                        ls_file = dt.resultData.Rows[i]["ls_file"].ToString().Trim();
                    var kom_file = "";
                    if (dt.resultData.Rows[i]["kom_file"] != DBNull.Value &&
                        dt.resultData.Rows[i]["kom_file"].ToString().Trim() != "")
                        kom_file = dt.resultData.Rows[i]["kom_file"].ToString().Trim();


                    var dom_base = dt.resultData.Rows[i]["ndom_base"].ToString().Trim();
                    if (dt.resultData.Rows[i]["nkor_base"] != DBNull.Value &&
                        dt.resultData.Rows[i]["nkor_base"].ToString().Trim() != "-" &&
                        dt.resultData.Rows[i]["nkor_base"].ToString().Trim() != "")
                        dom_base += "/" + dt.resultData.Rows[i]["nkor_base"].ToString().Trim();
                    var ulica_base = "";
                    if (dt.resultData.Rows[i]["ulica_base"] != DBNull.Value &&
                        dt.resultData.Rows[i]["ulica_base"].ToString().Trim() != "-" &&
                        dt.resultData.Rows[i]["ulica_base"].ToString().Trim() != "")
                        ulica_base = dt.resultData.Rows[i]["ulica_base"].ToString().Trim() + ", ";
                    var rajon_base = "";
                    if (dt.resultData.Rows[i]["rajon_base"] != DBNull.Value &&
                        dt.resultData.Rows[i]["rajon_base"].ToString().Trim() != "-" &&
                        dt.resultData.Rows[i]["rajon_base"].ToString().Trim() != "")
                        rajon_base = dt.resultData.Rows[i]["rajon_base"].ToString().Trim() + ", ";
                    var town_base = "";
                    if (dt.resultData.Rows[i]["town_base"] != DBNull.Value &&
                        dt.resultData.Rows[i]["town_base"].ToString().Trim() != "-" &&
                        dt.resultData.Rows[i]["town_base"].ToString().Trim() != "")
                        town_base = dt.resultData.Rows[i]["town_base"].ToString().Trim() + ", ";
                    var nkvar_base = "";
                    if (dt.resultData.Rows[i]["nkvar_base"] != DBNull.Value &&
                        /*dt.resultData.Rows[i]["nkvar_file"].ToString().Trim() != "-" &&*/
                        dt.resultData.Rows[i]["nkvar_base"].ToString().Trim() != "")
                        nkvar_base = dt.resultData.Rows[i]["nkvar_base"].ToString().Trim();
                    var fio_base = "";
                    if (dt.resultData.Rows[i]["fio_base"] != DBNull.Value &&
                        dt.resultData.Rows[i]["fio_base"].ToString().Trim() != "")
                        fio_base = ", " + dt.resultData.Rows[i]["fio_base"].ToString().Trim();
                    var ls_base = "";
                    if (dt.resultData.Rows[i]["ls_base"] != DBNull.Value &&
                        dt.resultData.Rows[i]["ls_base"].ToString().Trim() != "")
                        ls_base = dt.resultData.Rows[i]["ls_base"].ToString().Trim();
                    var kom_base = "";
                    if (dt.resultData.Rows[i]["kom_base"] != DBNull.Value &&
                        dt.resultData.Rows[i]["kom_base"].ToString().Trim() != "")
                        kom_base = dt.resultData.Rows[i]["kom_base"].ToString().Trim();


                    list.Add(new ComparedLS()
                    {
                        kvar_base =
                            ls_base + fio_base + " (" + town_base + rajon_base + ulica_base + dom_base +
                            ", КВ " + nkvar_base + ", КОМ " + kom_file + ")",
                        kvar_file =
                            ls_file + fam_file + ima_file + otch_file + " (" + town_file + rajon_file + ulica_file +
                            dom_file + ", КВ " + nkvar_file + ", КОМ " + kom_file + ")",
                        nzp_kvar = dt.resultData.Rows[i]["nzp_kvar"].ToString().Trim()
                    });
                }
                ret.returnsData = list;

                Returns retVar = new Returns();
                sql =
                    " select count(*) from (select upper(f.ndom) as ndom_file, upper(f.nkor) as nkor_file, upper(b.ndom) as ndom_base, upper(b.nkor) as nkor_base, b.nzp_dom, " +
                    " upper(f.ulica) as ulica_file, upper(ul.ulica) as ulica_base, upper(f.rajon) as rajon_file, upper(f.town) as town_file, upper(raj.rajon) as rajon_base, upper(town.town) as town_base, " +
                    " fk.id as ls_file, fk.nzp_kvar as nzp_kvar, fk.nkvar as nkvar_file, kvar.nkvar as nkvar_base, upper(kvar.fio) as fio_base, upper(fk.fam) as fam_file, upper(fk.ima) as ima_file,  " +
                    " upper(fk.otch) as otch_file " +
                    " from " + Points.Pref + DBManager.sUploadAliasRest + "file_dom f, " +
                    finder.bank + DBManager.sDataAliasRest + "dom b, " +
                    Points.Pref + DBManager.sDataAliasRest + "s_ulica ul, " +
                    Points.Pref + DBManager.sDataAliasRest + "s_rajon raj, " +
                    Points.Pref + DBManager.sDataAliasRest + "s_town town, " +
                    Points.Pref + DBManager.sUploadAliasRest + "file_kvar fk, " +
                    finder.bank + DBManager.sDataAliasRest + "kvar kvar" +
                    " where b.nzp_ul = ul.nzp_ul and ul.nzp_raj = raj.nzp_raj and raj.nzp_town = town.nzp_town and f.nzp_dom = b.nzp_dom and f.nzp_dom is not null and " +
                    " kvar.nzp_kvar = fk.nzp_kvar and fk.nzp_kvar is not null and fk.nzp_dom = f.nzp_dom and fk.nzp_file = f.nzp_file and " +
                    " f.nzp_file in" +
                    " (select nzp_file from " +
                    Points.Pref + DBManager.sUploadAliasRest + "files_selected " +
                    " where trim(pref)='" + finder.bank + "' and nzp_user = " + finder.nzp_user + "))" +
#if PG
                        " as foo  " +
#else
#endif
                        "";

                object count = ExecScalar(con_db, sql, out retVar, true);
                if (retVar.result)
                {
                    ret.tag = Convert.ToInt32(count);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetComparedLS : " + ex.Message,
                    MonitorLog.typelog.Error, true);
                ret.text = "Ошибка выполнения";
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
        /// получение несопоставленных участков
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<List<UncomparedAreas>> GetUncomparedArea(Finder finder)
        {

            ReturnsObjectType<List<UncomparedAreas>> ret = new ReturnsObjectType<List<UncomparedAreas>>();

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
#if PG
                string skip = finder.skip >= 0 && finder.rows >= 0 ? " offset " + finder.skip + " limit " + finder.rows : String.Empty;
#else
                string skip = finder.skip >= 0 && finder.rows >= 0
                    ? " skip " + finder.skip + " first " + finder.rows
                    : String.Empty;
#endif
                string sql = "select " +
#if PG
#else
                    skip +
#endif

                    " distinct upper(area) as area from " + Points.Pref + DBManager.sUploadAliasRest + "file_area  " +
                             " where nzp_area is null " +
                             " and nzp_file in " +
                             "(" +
                             " select l.nzp_file from " + Points.Pref + DBManager.sUploadAliasRest +
                             "files_selected l, "
                             + Points.Pref + DBManager.sUploadAliasRest + "files_imported fi " +
                             " where fi.nzp_file=l.nzp_file and trim(fi.pref)='" + finder.bank + "' " +
                             //запараллеливание пользователей
                             " and l.nzp_user = " + finder.nzp_user +
                             ") " +
#if PG
 skip +
#else
#endif
                    "";

                var dt = ClassDBUtils.OpenSQL(sql, con_db);
                List<UncomparedAreas> list = new List<UncomparedAreas>();
                for (int i = 0; i < dt.resultData.Rows.Count; i++)
                {
                    list.Add(new UncomparedAreas()
                    {
                        area = dt.resultData.Rows[i]["area"].ToString().Trim()
                    });
                    if (PmaxVisible < i)
                    {
                        break;
                    }
                }
                ret.returnsData = list;

                Returns retVar = new Returns();
                sql =
                    " select count(*) from " +
                    "(" +
                    "select distinct upper(area) as area from " + Points.Pref + DBManager.sUploadAliasRest +
                    "file_area " +
                    " where nzp_area is null and " +
                    " nzp_file in " +
                    " (" +
                    " select l.nzp_file from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected l, "
                    + Points.Pref + DBManager.sUploadAliasRest + "files_imported fi " +
                    " where fi.nzp_file=l.nzp_file " +
                    " and trim(fi.pref)='" + finder.bank + "'" +
                    //запараллеливание пользователей
                    " and l.nzp_user = " + finder.nzp_user +
                    " ) " +
                    ")" +
#if PG
 " as foo " +
#else
#endif

                        "";
                object count = ExecScalar(con_db, sql, out retVar, true);
                if (retVar.result)
                {
                    ret.tag = Convert.ToInt32(count);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetUncomparedArea : " + ex.Message,
                    MonitorLog.typelog.Error, true);
                ret.text = "Ошибка выполнения";
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
        /// получение несопоставленных поставщиков
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<List<UncomparedSupps>> GetUncomparedSupp(Finder finder)
        {

            ReturnsObjectType<List<UncomparedSupps>> ret = new ReturnsObjectType<List<UncomparedSupps>>();

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

#if PG
                string skip = finder.skip >= 0 && finder.rows >= 0 ? " offset " + finder.skip + " limit " + finder.rows : String.Empty;
#else
                string skip = finder.skip >= 0 && finder.rows >= 0
                    ? " skip " + finder.skip + " first " + finder.rows
                    : String.Empty;
#endif
                string sql = "select " +
#if PG
#else
                    skip +
#endif
                    " distinct upper(supp_name) as supp_name from " + Points.Pref + DBManager.sUploadAliasRest +
                             "file_supp " +
                             " where nzp_supp is null and " +
                             " nzp_file in " +
                             " (select l.nzp_file from " + Points.Pref + DBManager.sUploadAliasRest +
                             "files_selected l, "
                             + Points.Pref + DBManager.sUploadAliasRest + "files_imported fi " +
                             " where fi.nzp_file=l.nzp_file " +
                             " and trim(fi.pref)='" + finder.bank + "' " +
                             //запараллеливание пользователей
                             " and l.nzp_user = " + finder.nzp_user +
                             ") " +
#if PG
 skip +
#else
#endif
                    "";

                var dt = ClassDBUtils.OpenSQL(sql, con_db);
                List<UncomparedSupps> list = new List<UncomparedSupps>();
                for (int i = 0; i < dt.resultData.Rows.Count; i++)
                {
                    list.Add(new UncomparedSupps()
                    {
                        supp = dt.resultData.Rows[i]["supp_name"].ToString().Trim()
                    });
                    if (PmaxVisible < i)
                    {
                        break;
                    }
                }
                ret.returnsData = list;

                Returns retVar = new Returns();
                sql =
                    " select count(*) from " +
                    "(" +
                    "select distinct upper(supp_name) as supp_name " +
                    "from " + Points.Pref + DBManager.sUploadAliasRest + "file_supp  where nzp_supp is null and " +
                    " nzp_file in " +
                    " (select l.nzp_file from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected l, " +
                    Points.Pref + DBManager.sUploadAliasRest + "files_imported fi " +
                    " where fi.nzp_file=l.nzp_file and trim(fi.pref)='" + finder.bank + "'" +
                    //запараллеливание пользователей
                    " and l.nzp_user = " + finder.nzp_user +
                    "))" +
#if PG
 " as foo  " +
#else
#endif
                        "";
                object count = ExecScalar(con_db, sql, out retVar, true);
                if (retVar.result)
                {
                    ret.tag = Convert.ToInt32(count);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetUncomparedSupp : " + ex.Message,
                    MonitorLog.typelog.Error, true);
                ret.text = "Ошибка выполнения";
                ret.result = false;
                return ret;
            }

            #endregion


            ret.result = true;
            ret.text = "Выполнено.";

            return ret;
        }

        /// <summary>
        /// получение несопоставленных МО
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<List<UncomparedVills>> GetUncomparedMO(Finder finder)
        {
            ReturnsObjectType<List<UncomparedVills>> ret = new ReturnsObjectType<List<UncomparedVills>>();

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

#if PG
                string skip = finder.skip >= 0 && finder.rows >= 0 ? " offset " + finder.skip + " limit " + finder.rows : String.Empty;
#else
                string skip = finder.skip >= 0 && finder.rows >= 0
                    ? " skip " + finder.skip + " first " + finder.rows
                    : String.Empty;
#endif
                string sql =
                    "select " +
#if PG
#else
                        skip +
#endif
                        " distinct upper(vill) as vill from " + Points.Pref + DBManager.sUploadAliasRest + "file_mo " +
                    " where nzp_vill is null and " +
                    " nzp_file in " +
                    "(select l.nzp_file from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected l, " +
                    Points.Pref + DBManager.sUploadAliasRest + "files_imported fi" +
                    " where fi.nzp_file=l.nzp_file and trim(fi.pref)='" + finder.bank + "'" +
                    //запараллеливание пользователей
                    " and l.nzp_user = " + finder.nzp_user +
                    " ) " +
#if PG
 skip +
#else
#endif
                        "";
                var dt = ClassDBUtils.OpenSQL(sql, con_db);
                List<UncomparedVills> list = new List<UncomparedVills>();
                for (int i = 0; i < dt.resultData.Rows.Count; i++)
                {
                    list.Add(new UncomparedVills()
                    {
                        vill = dt.resultData.Rows[i]["vill"].ToString().Trim()
                    });
                    if (PmaxVisible < i)
                    {
                        break;
                    }
                }
                ret.returnsData = list;

                Returns retVar = new Returns();
                sql =
                    " select count(*) from (select distinct upper(vill) as vill " +
                    " from " + Points.Pref + DBManager.sUploadAliasRest + "file_mo " +
                    " where nzp_vill is null and nzp_file in " +
                    " (select l.nzp_file from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected l, "
                    + Points.Pref + DBManager.sUploadAliasRest + "files_imported fi " +
                    " where fi.nzp_file=l.nzp_file and trim(fi.pref)='" + finder.bank + "'" +
                    //запараллеливание пользователей
                    " and l.nzp_user = " + finder.nzp_user +
                    "))" +
#if PG
 " as foo  " +
#else
#endif
                        "";

                object count = ExecScalar(con_db, sql, out retVar, true);
                if (retVar.result)
                {
                    ret.tag = Convert.ToInt32(count);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetUncomparedMO : " + ex.Message,
                    MonitorLog.typelog.Error, true);
                ret.text = "Ошибка выполнения";
                ret.result = false;
                return ret;
            }

            #endregion

            ret.result = true;
            ret.text = "Выполнено.";

            return ret;
        }

        /// <summary>
        /// получение несопоставленных услуг
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<List<UncomparedServs>> GetUncomparedServ(Finder finder)
        {

            ReturnsObjectType<List<UncomparedServs>> ret = new ReturnsObjectType<List<UncomparedServs>>();

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

#if PG
                string skip = finder.skip >= 0 && finder.rows >= 0 ? " offset " + finder.skip + " limit " + finder.rows : String.Empty;
#else
                string skip = finder.skip >= 0 && finder.rows >= 0
                    ? " skip " + finder.skip + " first " + finder.rows
                    : String.Empty;
#endif
                string sql = "select " +
#if PG
#else
                    skip +
#endif
                    " " +
                             " distinct upper(service) as service " +
                             " from " + Points.Pref + DBManager.sUploadAliasRest + "file_services " +
                             " where nzp_serv is null and nzp_file in " +
                             " (select l.nzp_file from " + Points.Pref + DBManager.sUploadAliasRest +
                             "files_selected l, " +
                             Points.Pref + DBManager.sUploadAliasRest + "files_imported fi " +
                             " where fi.nzp_file=l.nzp_file and trim(fi.pref)='" + finder.bank + "'" +
                             //запараллеливание пользователей
                             " and l.nzp_user = " + finder.nzp_user +
                             ") " +
#if PG
 skip +
#else
#endif
                    "";

                var dt = ClassDBUtils.OpenSQL(sql, con_db);
                List<UncomparedServs> list = new List<UncomparedServs>();
                for (int i = 0; i < dt.resultData.Rows.Count; i++)
                {
                    list.Add(new UncomparedServs()
                    {
                        serv = dt.resultData.Rows[i]["service"].ToString().Trim()
                    });
                    if (PmaxVisible < i)
                    {
                        break;
                    }
                }
                ret.returnsData = list;

                Returns retVar = new Returns();
                sql =
                    " select count(*) from " +
                    "(select distinct upper(service) as service " +
                    " from " + Points.Pref + DBManager.sUploadAliasRest + "file_services " +
                    " where nzp_serv is null and  nzp_file in " +
                    " (select l.nzp_file from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected l, " +
                    Points.Pref + DBManager.sUploadAliasRest + "files_imported fi " +
                    " where fi.nzp_file=l.nzp_file and trim(fi.pref)='" + finder.bank + "'" +
                    //запараллеливание пользователей
                    " and l.nzp_user = " + finder.nzp_user +
                    "))" + //and nzp_measure is not null)";
#if PG
" as foo  " +
#else
#endif
                        "";

                object count = ExecScalar(con_db, sql, out retVar, true);
                if (retVar.result)
                {
                    ret.tag = Convert.ToInt32(count);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetUncomparedServ : " + ex.Message,
                    MonitorLog.typelog.Error, true);
                ret.text = "Ошибка выполнения";
                ret.result = false;
                return ret;
            }

            #endregion

            ret.result = true;
            ret.text = "Выполнено.";

            return ret;
        }

        /// <summary>
        /// получение несопоставленных типов доп. параметров
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<List<UncomparedParTypes>> GetUncomparedParType(Finder finder)
        {

            ReturnsObjectType<List<UncomparedParTypes>> ret = new ReturnsObjectType<List<UncomparedParTypes>>();

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

#if PG
                string skip = finder.skip >= 0 && finder.rows >= 0 ? " offset " + finder.skip + " limit " + finder.rows : String.Empty;
#else
                string skip = finder.skip >= 0 && finder.rows >= 0
                    ? " skip " + finder.skip + " first " + finder.rows
                    : String.Empty;
#endif
                string sql =
                    " select distinct upper(prm_name) as prm_name from " + Points.Pref + DBManager.sUploadAliasRest +
                    "file_typeparams " +
                    " where nzp_prm is null and nzp_file in " +
                    " (select l.nzp_file " +
                    " from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected l, " + Points.Pref +
                    DBManager.sUploadAliasRest + "files_imported fi " +
                    " where fi.nzp_file=l.nzp_file and trim(fi.pref)='" + finder.bank + "'" +
                    //запараллеливание пользователей
                    " and l.nzp_user = " + finder.nzp_user +
                    ") ";
                var dt = ClassDBUtils.OpenSQL(sql, con_db);
                List<UncomparedParTypes> list = new List<UncomparedParTypes>();
                for (int i = 0; i < dt.resultData.Rows.Count; i++)
                {
                    list.Add(new UncomparedParTypes()
                    {
                        name_prm = dt.resultData.Rows[i]["prm_name"].ToString().Trim()
                    });
                }
                ret.returnsData = list;

                Returns retVar = new Returns();
                sql =
                    " select count(*) " +
                    " from (select distinct upper(prm_name) as prm_name " +
                    " from " + Points.Pref + DBManager.sUploadAliasRest + "file_typeparams " +
                    " where nzp_prm is null and nzp_file in " +
                    "(select l.nzp_file " +
                    " from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected l, " + Points.Pref +
                    DBManager.sUploadAliasRest + "files_imported fi " +
                    " where fi.nzp_file=l.nzp_file and trim(fi.pref)='" + finder.bank + "'" +
                    " and l.nzp_user = " + finder.nzp_user + //запараллеливание пользователей
                    "))" +
#if PG
 " as foo  " +
#else
#endif
                        "";
                object count = ExecScalar(con_db, sql, out retVar, true);
                if (retVar.result)
                {
                    ret.tag = Convert.ToInt32(count);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetUncomparedParType : " + ex.Message,
                    MonitorLog.typelog.Error, true);
                ret.text = "Ошибка выполнения";
                ret.result = false;
                return ret;
            }

            #endregion

            ret.result = true;
            ret.text = "Выполнено.";

            return ret;
        }

        // <summary>
        /// получение несопоставленных типов благоустройства дома
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<List<UncomparedParTypes>> GetUncomparedParBlag(Finder finder)
        {

            ReturnsObjectType<List<UncomparedParTypes>> ret = new ReturnsObjectType<List<UncomparedParTypes>>();

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
#if PG
                string skip = finder.skip >= 0 && finder.rows >= 0 ? " offset " + finder.skip + " limit " + finder.rows : String.Empty;
#else
                string skip = finder.skip >= 0 && finder.rows >= 0
                    ? " skip " + finder.skip + " first " + finder.rows
                    : String.Empty;
#endif
                string sql = " select " +
#if PG
#else
                    skip +
#endif
                    " distinct upper(name) as name_prm from " + Points.Pref + DBManager.sUploadAliasRest + "file_blag " +
                             " where nzp_prm is null and nzp_file in " +
                             " (select l.nzp_file " +
                             " from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected l, " + Points.Pref +
                             DBManager.sUploadAliasRest + "files_imported fi " +
                             " where fi.nzp_file=l.nzp_file and trim(fi.pref)='" + finder.bank + "'" +
                             " and l.nzp_user = " + finder.nzp_user + //запараллеливание пользователей
                             ") " +
#if PG
 skip +
#else
#endif
                    "";

                var dt = ClassDBUtils.OpenSQL(sql, con_db);
                List<UncomparedParTypes> list = new List<UncomparedParTypes>();
                for (int i = 0; i < dt.resultData.Rows.Count; i++)
                {
                    list.Add(new UncomparedParTypes()
                    {
                        name_prm = dt.resultData.Rows[i]["name_prm"].ToString().Trim()
                    });
                    if (PmaxVisible < i)
                    {
                        break;
                    }
                }
                ret.returnsData = list;

                Returns retVar = new Returns();
                sql =
                    " select count(*) from (select distinct upper(name) as name_prm " +
                    " from " + Points.Pref + DBManager.sUploadAliasRest + "file_blag " +
                    " where nzp_prm is null and nzp_file in " +
                    " (select l.nzp_file " +
                    " from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected l, " + Points.Pref +
                    DBManager.sUploadAliasRest + "files_imported fi " +
                    " where fi.nzp_file=l.nzp_file and trim(fi.pref)='" + finder.bank + "'" +
                    " and l.nzp_user = " + finder.nzp_user + //запараллеливание пользователей
                    "))" +
#if PG
 " as foo  " +
#else
#endif
                        "";
                object count = ExecScalar(con_db, sql, out retVar, true);
                if (retVar.result)
                {
                    ret.tag = Convert.ToInt32(count);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetUncomparedParWater : " + ex.Message,
                    MonitorLog.typelog.Error, true);
                ret.text = "Ошибка выполнения";
                ret.result = false;
                return ret;
            }

            #endregion

            ret.result = true;
            ret.text = "Выполнено.";

            return ret;
        }

        // <summary>
        /// получение несопоставленных типов дома по газу
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<List<UncomparedParTypes>> GetUncomparedParGas(Finder finder)
        {

            ReturnsObjectType<List<UncomparedParTypes>> ret = new ReturnsObjectType<List<UncomparedParTypes>>();

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
#if PG
                string skip = finder.skip >= 0 && finder.rows >= 0 ? " offset " + finder.skip + " limit " + finder.rows : String.Empty;
#else
                string skip = finder.skip >= 0 && finder.rows >= 0
                    ? " skip " + finder.skip + " first " + finder.rows
                    : String.Empty;
#endif
                string sql = " select " +
#if PG
#else
                    skip +
#endif
                    " distinct upper(name) as name_prm from " + Points.Pref + DBManager.sUploadAliasRest + "file_gaz " +
                             " where nzp_prm is null and nzp_file in " +
                             " (select l.nzp_file " +
                             " from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected l, " + Points.Pref +
                             DBManager.sUploadAliasRest + "files_imported fi " +
                             " where fi.nzp_file=l.nzp_file and trim(fi.pref)='" + finder.bank + "'" +
                             " and l.nzp_user = " + finder.nzp_user + //запараллеливание пользователей
                             ") " +
#if PG
 skip +
#else
#endif
                    "";

                var dt = ClassDBUtils.OpenSQL(sql, con_db);
                List<UncomparedParTypes> list = new List<UncomparedParTypes>();
                for (int i = 0; i < dt.resultData.Rows.Count; i++)
                {
                    list.Add(new UncomparedParTypes()
                    {
                        name_prm = dt.resultData.Rows[i]["name_prm"].ToString().Trim()
                    });
                    if (PmaxVisible < i)
                    {
                        break;
                    }
                }
                ret.returnsData = list;

                Returns retVar = new Returns();
                sql =
                    " select count(*) from (select distinct upper(name) as name_prm from " + Points.Pref +
                    DBManager.sUploadAliasRest + "file_gaz " +
                    " where nzp_prm is null and nzp_file in " +
                    " (select l.nzp_file " +
                    " from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected l, " + Points.Pref +
                    DBManager.sUploadAliasRest + "files_imported fi " +
                    " where fi.nzp_file=l.nzp_file and trim(fi.pref)='" + finder.bank + "'" +
                    " and l.nzp_user = " + finder.nzp_user + //запараллеливание пользователей
                    "))" +
#if PG
 " as foo  " +
#else
#endif
                        "";

                object count = ExecScalar(con_db, sql, out retVar, true);
                if (retVar.result)
                {
                    ret.tag = Convert.ToInt32(count);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetUncomparedParGas : " + ex.Message,
                    MonitorLog.typelog.Error, true);
                ret.text = "Ошибка выполнения";
                ret.result = false;
                return ret;
            }

            #endregion

            ret.result = true;
            ret.text = "Выполнено.";

            return ret;
        }

        // <summary>
        /// получение несопоставленных типов дома по воде
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<List<UncomparedParTypes>> GetUncomparedParWater(Finder finder)
        {

            ReturnsObjectType<List<UncomparedParTypes>> ret = new ReturnsObjectType<List<UncomparedParTypes>>();

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
#if PG
                string skip = finder.skip >= 0 && finder.rows >= 0 ? " offset " + finder.skip + " limit " + finder.rows : String.Empty;
#else
                string skip = finder.skip >= 0 && finder.rows >= 0
                    ? " skip " + finder.skip + " first " + finder.rows
                    : String.Empty;
#endif
                string sql = " select " +
#if PG
#else
                    skip +
#endif
                    " distinct upper(name) as name_prm " +
                             " from " + Points.Pref + DBManager.sUploadAliasRest + "file_voda " +
                             " where nzp_prm is null and nzp_file in " +
                             " (select l.nzp_file " +
                             " from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected l, " + Points.Pref +
                             DBManager.sUploadAliasRest + "files_imported fi " +
                             " where fi.nzp_file=l.nzp_file and trim(fi.pref)='" + finder.bank + "'" +
                             " and l.nzp_user = " + finder.nzp_user + //запараллеливание пользователей
                             ") " +
#if PG
 skip +
#else
#endif
                    "";

                var dt = ClassDBUtils.OpenSQL(sql, con_db);
                List<UncomparedParTypes> list = new List<UncomparedParTypes>();
                for (int i = 0; i < dt.resultData.Rows.Count; i++)
                {
                    list.Add(new UncomparedParTypes()
                    {
                        name_prm = dt.resultData.Rows[i]["name_prm"].ToString().Trim()
                    });
                    if (PmaxVisible < i)
                    {
                        break;
                    }
                }
                ret.returnsData = list;

                Returns retVar = new Returns();
                sql =
                    " select count(*) from (select distinct upper(name) as name_prm " +
                    " from " + Points.Pref + DBManager.sUploadAliasRest + "file_voda " +
                    " where nzp_prm is null and nzp_file in " +
                    " (select l.nzp_file " +
                    " from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected l, " + Points.Pref +
                    DBManager.sUploadAliasRest + "files_imported fi " +
                    " where fi.nzp_file=l.nzp_file and trim(fi.pref)='" + finder.bank + "'" +
                    " and l.nzp_user = " + finder.nzp_user + //запараллеливание пользователей
                    "))" +
#if PG
 " as foo  " +
#else
#endif
                        "";

                object count = ExecScalar(con_db, sql, out retVar, true);
                if (retVar.result)
                {
                    ret.tag = Convert.ToInt32(count);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetUncomparedParWater : " + ex.Message,
                    MonitorLog.typelog.Error, true);
                ret.text = "Ошибка выполнения";
                ret.result = false;
                return ret;
            }

            #endregion

            ret.result = true;
            ret.text = "Выполнено.";

            return ret;
        }

        /// <summary>
        /// получение несопоставленных единиц измерения
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<List<UncomparedMeasures>> GetUncomparedMeasure(Finder finder)
        {

            ReturnsObjectType<List<UncomparedMeasures>> ret = new ReturnsObjectType<List<UncomparedMeasures>>();

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
#if PG
                string skip = finder.skip >= 0 && finder.rows >= 0 ? " offset " + finder.skip + " limit " + finder.rows : String.Empty;
#else
                string skip = finder.skip >= 0 && finder.rows >= 0
                    ? " skip " + finder.skip + " first " + finder.rows
                    : String.Empty;
#endif
                string sql = "select " +
#if PG
#else
                    skip +
#endif
                    " distinct upper(measure) as measure from " + Points.Pref + DBManager.sUploadAliasRest +
                             "file_measures " +
                             " where nzp_measure is null and nzp_file in " +
                             " (select l.nzp_file " +
                             " from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected l, " + Points.Pref +
                             DBManager.sUploadAliasRest + "files_imported fi " +
                             " where fi.nzp_file=l.nzp_file and trim(fi.pref)='" + finder.bank + "'" +
                             " and l.nzp_user = " + finder.nzp_user + //запараллеливание пользователей
                             ")" +
#if PG
 skip +
#else
#endif
                    "";
                var dt = ClassDBUtils.OpenSQL(sql, con_db);
                List<UncomparedMeasures> list = new List<UncomparedMeasures>();
                for (int i = 0; i < dt.resultData.Rows.Count; i++)
                {
                    list.Add(new UncomparedMeasures()
                    {
                        measure = dt.resultData.Rows[i]["measure"].ToString().Trim()
                    });
                    if (PmaxVisible < i)
                    {
                        break;
                    }
                }
                ret.returnsData = list;

                Returns retVar = new Returns();
                sql =
                    " select count(*) from (select distinct upper(measure) as measure " +
                    " from " + Points.Pref + DBManager.sUploadAliasRest + "file_measures " +
                    " where nzp_measure is null and nzp_file in " +
                    " (select l.nzp_file " +
                    " from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected l, " + Points.Pref +
                    DBManager.sUploadAliasRest + "files_imported fi " +
                    " where fi.nzp_file=l.nzp_file and trim(fi.pref)='" + finder.bank + "'" +
                    " and l.nzp_user = " + finder.nzp_user + //запараллеливание пользователей
                    "))" +
#if PG
 " as foo  " +
#else
#endif
                        "";
                object count = ExecScalar(con_db, sql, out retVar, true);
                if (retVar.result)
                {
                    ret.tag = Convert.ToInt32(count);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetUncomparedMeasure : " + ex.Message,
                    MonitorLog.typelog.Error, true);
                ret.text = "Ошибка выполнения";
                ret.result = false;
                return ret;
            }

            #endregion


            ret.result = true;
            ret.text = "Выполнено.";

            return ret;
        }

        /// <summary>
        /// получение несопоставленных городов
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<List<UncomparedTowns>> GetUncomparedTown(Finder finder)
        {

            ReturnsObjectType<List<UncomparedTowns>> ret = new ReturnsObjectType<List<UncomparedTowns>>();

            #region Работа с БД

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
#if PG
                string skip = finder.skip >= 0 && finder.rows >= 0 ? " offset " + finder.skip + " limit " + finder.rows : String.Empty;
#else
                string skip = finder.skip >= 0 && finder.rows >= 0
                    ? " skip " + finder.skip + " first " + finder.rows
                    : String.Empty;
#endif
                string sql = " select " +
#if PG
#else
                    skip +
#endif
                    " distinct upper(town) as town from " + Points.Pref + DBManager.sUploadAliasRest + "file_dom " +
                             "  where nzp_town is null and  nzp_file in " +
                             " (select l.nzp_file " +
                             " from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected l, " + Points.Pref +
                             DBManager.sUploadAliasRest + "files_imported fi " +
                             " where fi.nzp_file=l.nzp_file and trim(fi.pref)='" + finder.bank + "'" +
                             " and l.nzp_user = " + finder.nzp_user + //запараллеливание пользователей
                             ") " +
#if PG
 skip +
#else
#endif
                    "";
                var dt = ClassDBUtils.OpenSQL(sql, con_db);
                List<UncomparedTowns> list = new List<UncomparedTowns>();
                for (int i = 0; i < dt.resultData.Rows.Count; i++)
                {
                    list.Add(new UncomparedTowns()
                    {
                        town = dt.resultData.Rows[i]["town"].ToString().Trim()
                    });
                    if (PmaxVisible < i)
                    {
                        break;
                    }
                }
                ret.returnsData = list;

                Returns retVar = new Returns();
                sql =
                    " select count(*) from " +
                    " (select distinct upper(town) as town " +
                    " from " + Points.Pref + DBManager.sUploadAliasRest + "file_dom " +
                    "  where nzp_town is null and  nzp_file in " +
                    " (select l.nzp_file " +
                    " from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected l, " + Points.Pref +
                    DBManager.sUploadAliasRest + "files_imported fi " +
                    " where fi.nzp_file=l.nzp_file and trim(fi.pref)='" + finder.bank + "'" +
                    " and l.nzp_user = " + finder.nzp_user + //запараллеливание пользователей
                    "))" +
#if PG
 " as foo  " +
#else
#endif
                        "";
                object count = ExecScalar(con_db, sql, out retVar, true);
                if (retVar.result)
                {
                    ret.tag = Convert.ToInt32(count);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetUncomparedTown : " + ex.Message,
                    MonitorLog.typelog.Error, true);
                ret.text = "Ошибка выполнения";
                ret.result = false;
                return ret;
            }

            #endregion


            ret.result = true;
            ret.text = "Выполнено.";

            return ret;
        }

        /// <summary>
        /// получение несопоставленных улиц
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<List<UncomparedRajons>> GetUncomparedRajon(Finder finder)
        {

            ReturnsObjectType<List<UncomparedRajons>> ret = new ReturnsObjectType<List<UncomparedRajons>>();

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
#if PG
                string skip = finder.skip >= 0 && finder.rows >= 0 ? " offset " + finder.skip + " limit " + finder.rows : String.Empty;
#else
                string skip = finder.skip >= 0 && finder.rows >= 0
                    ? " skip " + finder.skip + " first " + finder.rows
                    : String.Empty;
#endif
                string sql = " select " +
#if PG
#else
                    skip +
#endif
                    " distinct upper(rajon) as rajon, nzp_raj, nzp_town, upper(town) as town " +
                             " from " + Points.Pref + DBManager.sUploadAliasRest + "file_dom f " +
                             " where ( nzp_raj is null or (select count(*) from " + Points.Pref + "_data" +
                             tableDelimiter + "s_rajon b where b.nzp_raj = f.nzp_raj) = 0 ) " +
                             " and nzp_file in " +
                             " (select l.nzp_file " +
                             " from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected l, " + Points.Pref +
                             DBManager.sUploadAliasRest + "files_imported fi " +
                             " where fi.nzp_file=l.nzp_file and trim(fi.pref)='" + finder.bank + "'" +
                             " and l.nzp_user = " + finder.nzp_user + //запараллеливание пользователей
                             ") " +
#if PG
 skip +
#else
#endif
                    "";

                var dt = ClassDBUtils.OpenSQL(sql, con_db);
                List<UncomparedRajons> list = new List<UncomparedRajons>();
                for (int i = 0; i < dt.resultData.Rows.Count; i++)
                {
                    var town = "";
                    if (dt.resultData.Rows[i]["town"] != DBNull.Value &&
                        dt.resultData.Rows[i]["town"].ToString().Trim() != "-" &&
                        dt.resultData.Rows[i]["town"].ToString().Trim() != "")
                        town = dt.resultData.Rows[i]["town"].ToString().Trim();
                    var nzp_raj = "";
                    if (dt.resultData.Rows[i]["nzp_raj"] != DBNull.Value &&
                        dt.resultData.Rows[i]["nzp_raj"].ToString().Trim() != "-" &&
                        dt.resultData.Rows[i]["nzp_raj"].ToString().Trim() != "")
                        nzp_raj = dt.resultData.Rows[i]["nzp_raj"].ToString().Trim();
                    list.Add(new UncomparedRajons()
                    {
                        show_data = dt.resultData.Rows[i]["rajon"].ToString().Trim() + " (" + town + ")",
                        rajon = dt.resultData.Rows[i]["rajon"].ToString().Trim(),
                        nzp_town = dt.resultData.Rows[i]["nzp_town"].ToString().Trim(),
                        nzp_raj = nzp_raj
                    });
                    if (PmaxVisible < i)
                    {
                        break;
                    }
                }
                ret.returnsData = list;

                Returns retVar = new Returns();
                sql =
                    "select count(*) from " +
                    " (SELECT distinct upper(rajon) as rajon, nzp_raj, nzp_town, upper(town) as town " +
                    "  FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_dom f " +
                    "  WHERE ( nzp_raj is null or (select count(*) from " + Points.Pref + "_data" + tableDelimiter +
                    "s_rajon b where b.nzp_raj = f.nzp_raj) = 0 ) " +
                    "   and nzp_file in " +
                    "  (select l.nzp_file " +
                    "    from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected l, " + Points.Pref +
                    DBManager.sUploadAliasRest + "files_imported fi " +
                    "    where fi.nzp_file=l.nzp_file and trim(fi.pref)='" + finder.bank + "'" +
                    "   and l.nzp_user = " + finder.nzp_user + //запараллеливание пользователей
                    "  )" +
                    " )" +
#if PG
 " as foo  " +
#else
#endif
                        "";
                object count = ExecScalar(con_db, sql, out retVar, true);
                if (retVar.result)
                {
                    ret.tag = Convert.ToInt32(count);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetUncomparedRajon : " + ex.Message,
                    MonitorLog.typelog.Error, true);
                ret.text = "Ошибка выполнения";
                ret.result = false;
                return ret;
            }

            #endregion

            ret.result = true;
            ret.text = "Выпонено.";

            return ret;
        }

        /// <summary>
        /// получение несопоставленных улиц
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<List<UncomparedStreets>> GetUncomparedStreets(Finder finder)
        {

            ReturnsObjectType<List<UncomparedStreets>> ret = new ReturnsObjectType<List<UncomparedStreets>>();

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
#if PG
                string skip = finder.skip >= 0 && finder.rows >= 0 ? " offset " + finder.skip + " limit " + finder.rows : String.Empty;
#else
                string skip = finder.skip >= 0 && finder.rows >= 0
                    ? " skip " + finder.skip + " first " + finder.rows
                    : String.Empty;
#endif
                string sql = " select " +
#if PG
#else
                    skip +
#endif
                    " distinct UPPER(ulica) as ulica, nzp_ul, nzp_town, nzp_raj, UPPER(rajon) as rajon, UPPER(town) as town " +
                             " from " + Points.Pref + DBManager.sUploadAliasRest + "file_dom f " +
                             " where (nzp_ul is null or (select count(*) from " + Points.Pref + "_data" + tableDelimiter +
                             "s_ulica b where b.nzp_ul = f.nzp_ul) = 0) " +
                             " and nzp_file in " +
                             " (select l.nzp_file " +
                             " from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected l, " + Points.Pref +
                             DBManager.sUploadAliasRest + "files_imported fi " +
                             " where fi.nzp_file=l.nzp_file and trim(fi.pref)='" + finder.bank + "'" +
                             " and l.nzp_user = " + finder.nzp_user + //запараллеливание пользователей
                             ") " +
#if PG
 skip +
#else
#endif
                    "";
                var dt = ClassDBUtils.OpenSQL(sql, con_db);
                List<UncomparedStreets> list = new List<UncomparedStreets>();
                for (int i = 0; i < dt.resultData.Rows.Count; i++)
                {
                    var rajon = "";
                    if (dt.resultData.Rows[i]["rajon"] != DBNull.Value &&
                        dt.resultData.Rows[i]["rajon"].ToString().Trim() != "-" &&
                        dt.resultData.Rows[i]["rajon"].ToString().Trim() != "")
                        rajon = ", " + dt.resultData.Rows[i]["rajon"].ToString().Trim();
                    var town = "";
                    if (dt.resultData.Rows[i]["town"] != DBNull.Value &&
                        dt.resultData.Rows[i]["town"].ToString().Trim() != "-" &&
                        dt.resultData.Rows[i]["town"].ToString().Trim() != "")
                        town = dt.resultData.Rows[i]["town"].ToString().Trim();
                    var nzp_ul = "";
                    if (dt.resultData.Rows[i]["nzp_ul"] != DBNull.Value &&
                        dt.resultData.Rows[i]["nzp_ul"].ToString().Trim() != "-" &&
                        dt.resultData.Rows[i]["nzp_ul"].ToString().Trim() != "")
                        nzp_ul = dt.resultData.Rows[i]["nzp_ul"].ToString().Trim();
                    list.Add(new UncomparedStreets()
                    {
                        show_data = dt.resultData.Rows[i]["ulica"].ToString().Trim() + " (" + town + rajon + ")",
                        ulica = dt.resultData.Rows[i]["ulica"].ToString().Trim().Replace('Ё', 'Е').Replace('ё', 'е'),
                        nzp_town = dt.resultData.Rows[i]["nzp_town"].ToString().Trim(),
                        nzp_raj = dt.resultData.Rows[i]["nzp_raj"].ToString().Trim(),
                        nzp_ul = nzp_ul
                    });
                }
                ret.returnsData = list;

                Returns retVar = new Returns();
                sql =
                    "select count(*) from " +
                    "(select distinct UPPER(ulica) as ulica, nzp_ul, nzp_town, nzp_raj, UPPER(rajon) as rajon, UPPER(town) as town " +
                    " from " + Points.Pref + DBManager.sUploadAliasRest + "file_dom f " +
                    " where (nzp_ul is null or (select count(*) from " + Points.Pref + "_data" + tableDelimiter +
                    "s_ulica b where b.nzp_ul = f.nzp_ul) = 0) " +
                    " and nzp_file in " +
                    " (select l.nzp_file " +
                    " from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected l, " + Points.Pref +
                    DBManager.sUploadAliasRest + "files_imported fi " +
                    " where fi.nzp_file=l.nzp_file and trim(fi.pref)='" + finder.bank + "'" +
                    " and l.nzp_user = " + finder.nzp_user + //запараллеливание пользователей
                    ")) " +
#if PG
 " as foo  " +
#else
#endif
                        "";

                object count = ExecScalar(con_db, sql, out retVar, true);
                if (retVar.result)
                {
                    ret.tag = Convert.ToInt32(count);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetUncomparedStreets : " + ex.Message,
                    MonitorLog.typelog.Error, true);
                ret.text = "Ошибка выполнения";
                ret.result = false;
                return ret;
            }

            #endregion


            ret.result = true;
            ret.text = "Выполнено.";

            return ret;
        }

        /// <summary>
        /// получение несопоставленных домов
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<List<UncomparedHouses>> GetUncomparedHouse(Finder finder)
        {

            ReturnsObjectType<List<UncomparedHouses>> ret = new ReturnsObjectType<List<UncomparedHouses>>();

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
#if PG
                string skip = finder.skip >= 0 && finder.rows >= 0 ? " offset " + finder.skip + " limit " + finder.rows : String.Empty;
#else
                string skip = finder.skip >= 0 && finder.rows >= 0
                    ? " skip " + finder.skip + " first " + finder.rows
                    : String.Empty;
#endif
                string sql = " select " +
#if PG
#else
                    skip +
#endif
                    //" distinct" +
                             " UPPER(ndom) as ndom, UPPER(nkor) as nkor, nzp_dom, nzp_town, nzp_raj, nzp_ul, UPPER(rajon) as rajon, UPPER(town) as town, UPPER(ulica) as ulica " +
                             " from " + Points.Pref + DBManager.sUploadAliasRest + "file_dom f " +
                             " where ( nzp_dom is null  or (select count(*) from " + finder.bank + "_data" +
                             tableDelimiter + "dom b where b.nzp_dom = f.nzp_dom) = 0 ) " +
                             " and nzp_file in " +
                             " (select l.nzp_file " +
                             " from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected l, " + Points.Pref +
                             DBManager.sUploadAliasRest + "files_imported fi " +
                             " where fi.nzp_file=l.nzp_file and trim(fi.pref)='" + finder.bank + "'" +
                             " and l.nzp_user = " + finder.nzp_user + //запараллеливание пользователей
                             ") "+
                             //"order by  ndom, town, rajon, ulica " +
#if PG
 skip +
#else
#endif
                    "";


                var dt = ClassDBUtils.OpenSQL(sql, con_db);
                List<UncomparedHouses> list = new List<UncomparedHouses>();
                for (int i = 0; i < dt.resultData.Rows.Count; i++)
                {
                    var rajon = "";
                    if (dt.resultData.Rows[i]["rajon"] != DBNull.Value &&
                        dt.resultData.Rows[i]["rajon"].ToString().Trim() != "-" &&
                        dt.resultData.Rows[i]["rajon"].ToString().Trim() != "")
                        rajon = ", " + dt.resultData.Rows[i]["rajon"].ToString().Trim();
                    var town = "";
                    if (dt.resultData.Rows[i]["town"] != DBNull.Value &&
                        dt.resultData.Rows[i]["town"].ToString().Trim() != "-" &&
                        dt.resultData.Rows[i]["town"].ToString().Trim() != "")
                        town = ", " + dt.resultData.Rows[i]["town"].ToString().Trim();
                    var ulica = "";
                    if (dt.resultData.Rows[i]["ulica"] != DBNull.Value &&
                        dt.resultData.Rows[i]["ulica"].ToString().Trim() != "-" &&
                        dt.resultData.Rows[i]["ulica"].ToString().Trim() != "")
                        ulica = dt.resultData.Rows[i]["ulica"].ToString().Trim();
                    var nkor = "";
                    if (dt.resultData.Rows[i]["nkor"] != DBNull.Value &&
                        dt.resultData.Rows[i]["nkor"].ToString().Trim() != "")
                        nkor = "/" + dt.resultData.Rows[i]["nkor"].ToString().Trim();
                    var nzp_dom = "";
                    if (dt.resultData.Rows[i]["nzp_dom"] != DBNull.Value &&
                        dt.resultData.Rows[i]["nzp_dom"].ToString().Trim() != "-" &&
                        dt.resultData.Rows[i]["nzp_dom"].ToString().Trim() != "")
                        nzp_dom = dt.resultData.Rows[i]["nzp_dom"].ToString().Trim();
                    list.Add(new UncomparedHouses()
                    {
                        show_data =
                            dt.resultData.Rows[i]["ndom"].ToString().Trim() + nkor + " (" + ulica + town + rajon + ")",
                        dom = dt.resultData.Rows[i]["ndom"].ToString().Trim() + nkor,
                        nzp_town = dt.resultData.Rows[i]["nzp_town"].ToString().Trim(),
                        nzp_raj = dt.resultData.Rows[i]["nzp_raj"].ToString().Trim(),
                        nzp_ul = dt.resultData.Rows[i]["nzp_ul"].ToString().Trim(),
                        nzp_dom = nzp_dom
                    });
                }
                ret.returnsData = list;

                Returns retVar = new Returns();
                sql =
                    "select count(*) from " +
                    "(select " +
                    //"distinct " +
                    "UPPER(ndom) as ndom, UPPER(nkor) as nkor, nzp_dom, nzp_town, nzp_raj, nzp_ul, UPPER(rajon) as rajon, UPPER(town) as town, UPPER(ulica) as ulica " +
                    " from " + Points.Pref + DBManager.sUploadAliasRest + "file_dom f " +
                    " where ( nzp_dom is null  or (select count(*) from " + finder.bank + "_data" + tableDelimiter +
                    "dom b where b.nzp_dom = f.nzp_dom) = 0 ) " +
                    " and nzp_file in " +
                    " (select l.nzp_file " +
                    " from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected l, " + Points.Pref +
                    DBManager.sUploadAliasRest + "files_imported fi " +
                    " where fi.nzp_file=l.nzp_file and trim(fi.pref)='" + finder.bank + "'" +
                    " and l.nzp_user = " + finder.nzp_user + //запараллеливание пользователей
                    ")) " +
#if PG
 " as foo  " +
#else
#endif
                        "";

                object count = ExecScalar(con_db, sql, out retVar, true);
                if (retVar.result)
                {
                    ret.tag = Convert.ToInt32(count);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetUncomparedHouse : " + ex.Message,
                    MonitorLog.typelog.Error, true);
                ret.text = "Ошибка выполнения";
                ret.result = false;
                return ret;
            }

            #endregion


            ret.result = true;
            ret.text = "Выполнено.";

            return ret;
        }

        /// <summary>
        /// получение несопоставленных домов
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<List<UncomparedLS>> GetUncomparedLS(Finder finder)
        {

            ReturnsObjectType<List<UncomparedLS>> ret = new ReturnsObjectType<List<UncomparedLS>>();

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
#if PG
                string skip = finder.skip >= 0 && finder.rows >= 0 ? " offset " + finder.skip + " limit " + finder.rows : String.Empty;
#else
                string skip = finder.skip >= 0 && finder.rows >= 0
                    ? " skip " + finder.skip + " first " + finder.rows
                    : String.Empty;
#endif
                string sql = " select " +
#if PG
#else
                    skip +
#endif
                    " distinct UPPER(ndom) as ndom, UPPER(nkor) as nkor, f.nzp_dom, nzp_town, nzp_raj, nzp_ul," +
                             " UPPER(rajon) as rajon, UPPER(town) as town, UPPER(ulica) as ulica, " +
                             " fk.id as ls, upper(fk.fam) as fam, upper(fk.ima) as ima, upper(fk.otch) as otch," +
                             " fk.nzp_kvar as nzp_kvar, fk.nkvar as kvar, fk.nkvar_n as kom, " +
                             Points.Pref + "_data" + tableDelimiter + "sortnum(fk.id) as sort_field " +
                             " from " + Points.Pref + DBManager.sUploadAliasRest + "file_dom f,  " + Points.Pref +
                             DBManager.sUploadAliasRest + "file_kvar fk " +
                             " where f.nzp_dom is not null  " +
                             " and f.nzp_file = fk.nzp_file and f.nzp_dom = fk.nzp_dom " +
                             " and (fk.nzp_kvar is null  or (select count(*) from " + finder.bank + "_data" +
                             tableDelimiter + "kvar b where b.nzp_kvar = fk.nzp_kvar) = 0 )" +
                             " and f.nzp_file in " +
                             " (select l.nzp_file " +
                             " from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected l, " + Points.Pref +
                             DBManager.sUploadAliasRest + "files_imported fi " +
                             " where fi.nzp_file=l.nzp_file and trim(fi.pref)='" + finder.bank + "'" +
                             " and l.nzp_user = " + finder.nzp_user + //запараллеливание пользователей
                             ") "+
                             //"order by sort_field " +
#if PG
 skip +
#else
#endif
                    "";


                var dt = ClassDBUtils.OpenSQL(sql, con_db);
                List<UncomparedLS> list = new List<UncomparedLS>();
                for (int i = 0; i < dt.resultData.Rows.Count; i++)
                {
                    var rajon = "";
                    if (dt.resultData.Rows[i]["rajon"] != DBNull.Value &&
                        dt.resultData.Rows[i]["rajon"].ToString().Trim() != "-" &&
                        dt.resultData.Rows[i]["rajon"].ToString().Trim() != "")
                        rajon = dt.resultData.Rows[i]["rajon"].ToString().Trim() + ", ";
                    var town = "";
                    if (dt.resultData.Rows[i]["town"] != DBNull.Value &&
                        dt.resultData.Rows[i]["town"].ToString().Trim() != "-" &&
                        dt.resultData.Rows[i]["town"].ToString().Trim() != "")
                        town = dt.resultData.Rows[i]["town"].ToString().Trim() + ", ";
                    var ulica = "";
                    if (dt.resultData.Rows[i]["ulica"] != DBNull.Value &&
                        dt.resultData.Rows[i]["ulica"].ToString().Trim() != "-" &&
                        dt.resultData.Rows[i]["ulica"].ToString().Trim() != "")
                        ulica = dt.resultData.Rows[i]["ulica"].ToString().Trim() + ", ";
                    var nkor = "";
                    if (dt.resultData.Rows[i]["nkor"] != DBNull.Value &&
                        dt.resultData.Rows[i]["nkor"].ToString().Trim() != "")
                        nkor = "/" + dt.resultData.Rows[i]["nkor"].ToString().Trim();
                    var nzp_dom = "";
                    if (dt.resultData.Rows[i]["nzp_dom"] != DBNull.Value &&
                        dt.resultData.Rows[i]["nzp_dom"].ToString().Trim() != "-" &&
                        dt.resultData.Rows[i]["nzp_dom"].ToString().Trim() != "")
                        nzp_dom = dt.resultData.Rows[i]["nzp_dom"].ToString().Trim();
                    var nzp_kvar = "";
                    if (dt.resultData.Rows[i]["nzp_kvar"] != DBNull.Value &&
                        dt.resultData.Rows[i]["nzp_kvar"].ToString().Trim() != "")
                        nzp_kvar = dt.resultData.Rows[i]["nzp_kvar"].ToString().Trim();
                    var kvar = "";
                    if (dt.resultData.Rows[i]["kvar"] != DBNull.Value &&
                        dt.resultData.Rows[i]["kvar"].ToString().Trim() != "")
                        kvar = dt.resultData.Rows[i]["kvar"].ToString().Trim();
                    var ls = "";
                    if (dt.resultData.Rows[i]["ls"] != DBNull.Value &&
                        dt.resultData.Rows[i]["ls"].ToString().Trim() != "-" &&
                        dt.resultData.Rows[i]["ls"].ToString().Trim() != "")
                        ls = dt.resultData.Rows[i]["ls"].ToString().Trim() + ", ";
                    var fam = "";
                    if (dt.resultData.Rows[i]["fam"] != DBNull.Value &&
                        dt.resultData.Rows[i]["fam"].ToString().Trim() != "-" &&
                        dt.resultData.Rows[i]["fam"].ToString().Trim() != "")
                        fam = dt.resultData.Rows[i]["fam"].ToString().Trim() + " ";
                    var ima = "";
                    if (dt.resultData.Rows[i]["ima"] != DBNull.Value &&
                        dt.resultData.Rows[i]["ima"].ToString().Trim() != "-" &&
                        dt.resultData.Rows[i]["ima"].ToString().Trim() != "")
                        ima = dt.resultData.Rows[i]["ima"].ToString().Trim() + " ";
                    var otch = "";
                    if (dt.resultData.Rows[i]["otch"] != DBNull.Value &&
                        dt.resultData.Rows[i]["otch"].ToString().Trim() != "-" &&
                        dt.resultData.Rows[i]["otch"].ToString().Trim() != "")
                        otch = dt.resultData.Rows[i]["otch"].ToString().Trim() + " ";
                    var kom = "";
                    if (dt.resultData.Rows[i]["kom"] != DBNull.Value &&
                        dt.resultData.Rows[i]["kom"].ToString().Trim() != "")
                        kom = dt.resultData.Rows[i]["kom"].ToString().Trim() + " ";

                    list.Add(new UncomparedLS()
                    {
                        show_data =
                            ls + fam + ima + otch + " (" + town + rajon + ulica +
                            dt.resultData.Rows[i]["ndom"].ToString().Trim() + nkor + ", КВ" + kvar + ", КОМ " + kom + ")",
                        dom = dt.resultData.Rows[i]["ndom"].ToString().Trim() + nkor,
                        nzp_town = dt.resultData.Rows[i]["nzp_town"].ToString().Trim(),
                        nzp_raj = dt.resultData.Rows[i]["nzp_raj"].ToString().Trim(),
                        nzp_ul = dt.resultData.Rows[i]["nzp_ul"].ToString().Trim(),
                        nzp_dom = nzp_dom,
                        nzp_kvar = nzp_kvar,
                        kvar = kvar
                    });
                }
                ret.returnsData = list;

                Returns retVar = new Returns();
                sql =
                    "select count(*) from (select  distinct " +
                    //"UPPER(ndom) as ndom, UPPER(nkor) as nkor, f.nzp_dom, nzp_town, nzp_raj, nzp_ul, UPPER(rajon) as rajon, UPPER(town) as town, UPPER(ulica) as ulica, " +
                    " fk.id as ls, " +
                    //", upper(fk.fam) as fam, upper(fk.ima) as ima, upper(fk.otch) as otch, fk.nzp_kvar as nzp_kvar, fk.nkvar as kvar, " +
                    Points.Pref + "_data" + tableDelimiter + "sortnum(fk.id) as sort_field " +
                    " from " + Points.Pref + DBManager.sUploadAliasRest + "file_dom f,  " + Points.Pref +
                    DBManager.sUploadAliasRest + "file_kvar fk " +
                    " where f.nzp_dom is not null  " +
                    " and f.nzp_file = fk.nzp_file and f.nzp_dom = fk.nzp_dom " +
                    " and (fk.nzp_kvar is null  or (select count(*) from " + finder.bank + "_data" + tableDelimiter +
                    "kvar b where b.nzp_kvar = fk.nzp_kvar) = 0 )" +
                    " and f.nzp_file in " +
                    " (select l.nzp_file " +
                    " from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected l, " + Points.Pref +
                    DBManager.sUploadAliasRest + "files_imported fi " +
                    " where fi.nzp_file=l.nzp_file and trim(fi.pref)='" + finder.bank + "'" +
                    " and l.nzp_user = " + finder.nzp_user + //запараллеливание пользователей
                    ")) " +
#if PG
 " as foo  " +
#else
#endif
                        "";

                object count = ExecScalar(con_db, sql, out retVar, true);
                if (retVar.result)
                {
                    ret.tag = Convert.ToInt32(count);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetUncomparedLS : " + ex.Message,
                    MonitorLog.typelog.Error, true);
                ret.text = "Ошибка выполнения";
                ret.result = false;
                return ret;
            }

            #endregion


            ret.result = true;
            ret.text = "Выполнено.";

            return ret;
        }


        /// <summary>
        /// получение участков по фильтру
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<List<UncomparedAreas>> GetAreaByFilter(UncomparedAreas finder)
        {

            ReturnsObjectType<List<UncomparedAreas>> ret = new ReturnsObjectType<List<UncomparedAreas>>();

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

                string sql = "select * from " + finder.bank + "_data" + tableDelimiter + "s_area " +
                             " where area " + DataImportUtils.plike + " '" + finder.area + "'";

                var dt = ClassDBUtils.OpenSQL(sql, con_db);

                List<UncomparedAreas> list = new List<UncomparedAreas>();
                for (int i = 0; i < dt.resultData.Rows.Count; i++)
                {
                    list.Add(new UncomparedAreas()
                    {
                        area = dt.resultData.Rows[i]["area"].ToString().Trim(),
                        nzp_area = dt.resultData.Rows[i]["nzp_area"].ToString().Trim()
                    });
                    if (PmaxVisible < i)
                    {
                        break;
                    }
                }
                ret.returnsData = list;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetAreaByFilter : " + ex.Message,
                    MonitorLog.typelog.Error, true);
                ret.text = "Ошибка выполнения";
                ret.result = false;
                return ret;
            }

            #endregion


            ret.result = true;
            ret.text = "Выполнено.";
            ret.tag = -1;

            return ret;
        }

        /// <summary>
        /// получение поставщиков по фильтру
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<List<UncomparedSupps>> GetSuppByFilter(UncomparedSupps finder)
        {
            ReturnsObjectType<List<UncomparedSupps>> ret = new ReturnsObjectType<List<UncomparedSupps>>();

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

                string sql = "select * from " + finder.bank + "_kernel" + tableDelimiter + "supplier " +
                             " where name_supp " + DataImportUtils.plike + " '" + finder.supp + "'";
                var dt = ClassDBUtils.OpenSQL(sql, con_db);

                List<UncomparedSupps> list = new List<UncomparedSupps>();
                for (int i = 0; i < dt.resultData.Rows.Count; i++)
                {
                    list.Add(new UncomparedSupps()
                    {
                        supp = dt.resultData.Rows[i]["name_supp"].ToString().Trim(),
                        nzp_supp = dt.resultData.Rows[i]["nzp_supp"].ToString().Trim()
                    });
                    if (PmaxVisible < i)
                    {
                        break;
                    }
                }
                ret.returnsData = list;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetSuppByFilter : " + ex.Message,
                    MonitorLog.typelog.Error, true);
                ret.text = "Ошибка выполнения";
                ret.result = false;
                return ret;
            }

            #endregion


            ret.result = true;
            ret.text = "Выполнено.";
            ret.tag = -1;

            return ret;
        }

        /// <summary>
        /// получение МО по фильтру
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<List<UncomparedVills>> GetMOByFilter(UncomparedVills finder)
        {

            ReturnsObjectType<List<UncomparedVills>> ret = new ReturnsObjectType<List<UncomparedVills>>();

            #region Запись в БД

            // using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            //{
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
                string sql = "select * from " + finder.bank + "_kernel" + tableDelimiter + "s_vill " +
                             " where upper(vill) " + DataImportUtils.plike + "  '" + finder.vill + "'";
                var dt = ClassDBUtils.OpenSQL(sql, con_db);

                List<UncomparedVills> list = new List<UncomparedVills>();
                for (int i = 0; i < dt.resultData.Rows.Count; i++)
                {
                    list.Add(new UncomparedVills()
                    {
                        vill = dt.resultData.Rows[i]["vill"].ToString().Trim(),
                        nzp_vill = dt.resultData.Rows[i]["nzp_vill"].ToString().Trim()
                    });
                    if (PmaxVisible < i)
                    {
                        break;
                    }
                }
                ret.returnsData = list;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetMOByFilter : " + ex.Message,
                    MonitorLog.typelog.Error, true);
                ret.text = "Ошибка выполнения";
                ret.result = false;
                return ret;
            }
            //}

            #endregion


            ret.result = true;
            ret.text = "Выполнено.";
            ret.tag = -1;

            return ret;
        }

        /// <summary>
        /// получение услуг по фильтру
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<List<UncomparedServs>> GetServByFilter(UncomparedServs finder)
        {

            ReturnsObjectType<List<UncomparedServs>> ret = new ReturnsObjectType<List<UncomparedServs>>();

            #region Запись в БД

            // using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            //{
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

                string sql = "select * from " + finder.bank + "_kernel" + tableDelimiter + "services " +
                             " where upper(service) " + DataImportUtils.plike + "  upper('" + finder.serv + "')";
                var dt = ClassDBUtils.OpenSQL(sql, con_db);

                List<UncomparedServs> list = new List<UncomparedServs>();
                for (int i = 0; i < dt.resultData.Rows.Count; i++)
                {
                    list.Add(new UncomparedServs()
                    {
                        serv = dt.resultData.Rows[i]["service"].ToString().Trim(),
                        nzp_serv = dt.resultData.Rows[i]["nzp_serv"].ToString().Trim()
                    });
                    if (PmaxVisible < i)
                    {
                        break;
                    }
                }
                ret.returnsData = list;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetServByFilter : " + ex.Message,
                    MonitorLog.typelog.Error, true);
                ret.text = "Ошибка выполнения";
                ret.result = false;
                return ret;
            }
            //}

            #endregion


            ret.result = true;
            ret.text = "Выполнено.";
            ret.tag = -1;

            return ret;
        }

        /// <summary>
        /// получение типов благоустройства дома по фильтру
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<List<UncomparedParTypes>> GetParTypeByFilter(UncomparedParTypes finder)
        {

            ReturnsObjectType<List<UncomparedParTypes>> ret = new ReturnsObjectType<List<UncomparedParTypes>>();

            #region Запись в БД

            // using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            //{
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

                string sql = "select * from " + finder.bank + "_kernel" + tableDelimiter + "prm_name " +
                             " where upper(name_prm) " + DataImportUtils.plike + "  upper('" + finder.name_prm + "')";

                var dt = ClassDBUtils.OpenSQL(sql, con_db);

                List<UncomparedParTypes> list = new List<UncomparedParTypes>();
                for (int i = 0; i < dt.resultData.Rows.Count; i++)
                {
                    list.Add(new UncomparedParTypes()
                    {
                        name_prm = dt.resultData.Rows[i]["name_prm"].ToString().Trim(),
                        nzp_prm = dt.resultData.Rows[i]["nzp_prm"].ToString().Trim()
                    });
                    if (PmaxVisible < i)
                    {
                        break;
                    }
                }
                ret.returnsData = list;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetParTypeByFilter : " + ex.Message,
                    MonitorLog.typelog.Error, true);
                ret.text = "Ошибка выполнения";
                ret.result = false;
                return ret;
            }
            //}

            #endregion

            ret.result = true;
            ret.text = "Выполнено.";
            ret.tag = -1;

            return ret;
        }

        /// <summary>
        /// получение типов благоустройства дома по фильтру
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<List<UncomparedParTypes>> GetParBlagByFilter(UncomparedParTypes finder)
        {

            ReturnsObjectType<List<UncomparedParTypes>> ret = new ReturnsObjectType<List<UncomparedParTypes>>();

            #region Запись в БД

            // using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            //{
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

                string sql = "select * from " + finder.bank + "_kernel" + tableDelimiter + "prm_name " +
                             " where upper(name_prm) " + DataImportUtils.plike + " upper( '" + finder.name_prm + "')";
                var dt = ClassDBUtils.OpenSQL(sql, con_db);

                List<UncomparedParTypes> list = new List<UncomparedParTypes>();
                for (int i = 0; i < dt.resultData.Rows.Count; i++)
                {
                    list.Add(new UncomparedParTypes()
                    {
                        name_prm = dt.resultData.Rows[i]["name_prm"].ToString().Trim(),
                        nzp_prm = dt.resultData.Rows[i]["nzp_prm"].ToString().Trim()
                    });
                    if (PmaxVisible < i)
                    {
                        break;
                    }
                }
                ret.returnsData = list;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetParBlagByFilter : " + ex.Message,
                    MonitorLog.typelog.Error, true);
                ret.text = "Ошибка выполнения";
                ret.result = false;
                return ret;
            }
            //}

            #endregion


            ret.result = true;
            ret.text = "Выполнено.";
            ret.tag = -1;

            return ret;
        }

        /// <summary>
        /// получение типов дома по газу по фильтру
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<List<UncomparedParTypes>> GetParGasByFilter(UncomparedParTypes finder)
        {

            ReturnsObjectType<List<UncomparedParTypes>> ret = new ReturnsObjectType<List<UncomparedParTypes>>();

            #region Запись в БД

            // using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            //{
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

                string sql = "select * from " + finder.bank + "_kernel" + tableDelimiter + "prm_name " +
                             " where upper(name_prm) " + DataImportUtils.plike + "  upper('" + finder.name_prm + "')";
                var dt = ClassDBUtils.OpenSQL(sql, con_db);

                List<UncomparedParTypes> list = new List<UncomparedParTypes>();
                for (int i = 0; i < dt.resultData.Rows.Count; i++)
                {
                    list.Add(new UncomparedParTypes()
                    {
                        name_prm = dt.resultData.Rows[i]["name_prm"].ToString().Trim(),
                        nzp_prm = dt.resultData.Rows[i]["nzp_prm"].ToString().Trim()
                    });
                    if (PmaxVisible < i)
                    {
                        break;
                    }
                }
                ret.returnsData = list;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetParGasByFilter : " + ex.Message,
                    MonitorLog.typelog.Error, true);
                ret.text = "Ошибка выполнения";
                ret.result = false;
                return ret;
            }
            //}

            #endregion


            ret.result = true;
            ret.text = "Выполнено.";
            ret.tag = -1;

            return ret;
        }

        /// <summary>
        /// получение типов  дома по воде по фильтру
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<List<UncomparedParTypes>> GetParWaterByFilter(UncomparedParTypes finder)
        {

            ReturnsObjectType<List<UncomparedParTypes>> ret = new ReturnsObjectType<List<UncomparedParTypes>>();

            #region Запись в БД

            // using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            //{
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

                string sql = "select * from " + finder.bank + "_kernel" + tableDelimiter + "prm_name " +
                             " where upper(name_prm) " + DataImportUtils.plike + "  upper('" + finder.name_prm + "')";
                var dt = ClassDBUtils.OpenSQL(sql, con_db);

                List<UncomparedParTypes> list = new List<UncomparedParTypes>();
                for (int i = 0; i < dt.resultData.Rows.Count; i++)
                {
                    list.Add(new UncomparedParTypes()
                    {
                        name_prm = dt.resultData.Rows[i]["name_prm"].ToString().Trim(),
                        nzp_prm = dt.resultData.Rows[i]["nzp_prm"].ToString().Trim()
                    });
                    if (PmaxVisible < i)
                    {
                        break;
                    }
                }
                ret.returnsData = list;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetParWaterByFilter : " + ex.Message,
                    MonitorLog.typelog.Error, true);
                ret.text = "Ошибка выполнения";
                ret.result = false;
                return ret;
            }
            //}

            #endregion

            ret.result = true;
            ret.text = "Выполнено.";
            ret.tag = -1;

            return ret;
        }

        /// <summary>
        /// получение единиц измерения по фильтру
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<List<UncomparedMeasures>> GetMeasureByFilter(UncomparedMeasures finder)
        {

            ReturnsObjectType<List<UncomparedMeasures>> ret = new ReturnsObjectType<List<UncomparedMeasures>>();

            #region Запись в БД

            // using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            //{
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

                string sql = "select * from " + finder.bank + "_kernel" + tableDelimiter + "s_measure " +
                             " where upper(measure_long) " + DataImportUtils.plike + " upper( '" + finder.measure + "')";
                var dt = ClassDBUtils.OpenSQL(sql, con_db);

                List<UncomparedMeasures> list = new List<UncomparedMeasures>();
                for (int i = 0; i < dt.resultData.Rows.Count; i++)
                {
                    list.Add(new UncomparedMeasures()
                    {
                        measure = dt.resultData.Rows[i]["measure_long"].ToString().Trim(),
                        nzp_measure = dt.resultData.Rows[i]["nzp_measure"].ToString().Trim()
                    });
                    if (PmaxVisible < i)
                    {
                        break;
                    }
                }
                ret.returnsData = list;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetMeasureByFilter : " + ex.Message,
                    MonitorLog.typelog.Error, true);
                ret.text = "Ошибка выполнения";
                ret.result = false;
                return ret;
            }
            //}

            #endregion


            ret.result = true;
            ret.text = "Выполнено.";
            ret.tag = -1;

            return ret;
        }

        /// <summary>
        /// получение улиц по фильтру
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<List<UncomparedTowns>> GetTownByFilter(UncomparedTowns finder)
        {

            ReturnsObjectType<List<UncomparedTowns>> ret = new ReturnsObjectType<List<UncomparedTowns>>();

            #region Запись в БД

            // using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            //{
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
                string sql = "select * from " + Points.Pref + "_data" + tableDelimiter + "s_town " +
                             " where town " + DataImportUtils.plike + " upper( '" + finder.town + "')";
                var dt = ClassDBUtils.OpenSQL(sql, con_db);

                List<UncomparedTowns> list = new List<UncomparedTowns>();
                for (int i = 0; i < dt.resultData.Rows.Count; i++)
                {
                    list.Add(new UncomparedTowns()
                    {
                        town = dt.resultData.Rows[i]["town"].ToString().Trim(),
                        nzp_town = dt.resultData.Rows[i]["nzp_town"].ToString().Trim()
                    });
                    if (PmaxVisible < i)
                    {
                        break;
                    }
                }
                ret.returnsData = list;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetTownByFilter : " + ex.Message,
                    MonitorLog.typelog.Error, true);
                ret.text = "Ошибка выполнения";
                ret.result = false;
                return ret;
            }
            //}

            #endregion


            ret.result = true;
            ret.text = "Выполнено.";
            ret.tag = -1;

            return ret;
        }

        /// <summary>
        /// получение населенных пунктов по фильтру
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<List<UncomparedRajons>> GetRajonByFilter(UncomparedRajons finder)
        {
            ReturnsObjectType<List<UncomparedRajons>> ret = new ReturnsObjectType<List<UncomparedRajons>>();

            #region Запись в БД

            // using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            //{
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

                string sql = "";
                if (finder.nzp_town != null && finder.nzp_town != "")
                    sql = "select raj.*, town.* from " + Points.Pref + "_data" + tableDelimiter + "s_rajon raj, " +
                          Points.Pref + "_data" + tableDelimiter + "s_town town " +
                          " where raj.nzp_town = town.nzp_town and raj.rajon " + DataImportUtils.plike + "  upper('" +
                          finder.rajon + "') and raj.nzp_town = " + finder.nzp_town;
                else
                    sql = "select raj.*, town.* from " + Points.Pref + "_data" + tableDelimiter + "s_rajon raj, " +
                          Points.Pref + "_data" + tableDelimiter + "s_town town " +
                          " where raj.nzp_town = town.nzp_town and upper(rajon) " + DataImportUtils.plike + "  upper('" +
                          finder.rajon + "')";
                var dt = ClassDBUtils.OpenSQL(sql, con_db);

                List<UncomparedRajons> list = new List<UncomparedRajons>();
                for (int i = 0; i < dt.resultData.Rows.Count; i++)
                {
                    var town = "";
                    if (dt.resultData.Rows[i]["town"] != DBNull.Value &&
                        dt.resultData.Rows[i]["town"].ToString().Trim() != "-" &&
                        dt.resultData.Rows[i]["town"].ToString().Trim() != "")
                        town = dt.resultData.Rows[i]["town"].ToString().Trim();
                    list.Add(new UncomparedRajons()
                    {
                        rajon = dt.resultData.Rows[i]["rajon"].ToString().Trim() + " (" + town + ")",
                        nzp_raj = dt.resultData.Rows[i]["nzp_raj"].ToString().Trim()
                    });
                    if (PmaxVisible < i)
                    {
                        break;
                    }
                }
                ret.returnsData = list;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetRajonByFilter : " + ex.Message,
                    MonitorLog.typelog.Error, true);
                ret.text = "Ошибка выполнения";
                ret.result = false;
                return ret;
            }
            //}

            #endregion


            ret.result = true;
            ret.text = "Выполнено.";
            ret.tag = -1;

            return ret;
        }

        /// <summary>
        /// получение улиц по фильтру
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<List<UncomparedStreets>> GetStreetsByFilter(UncomparedStreets finder)
        {

            ReturnsObjectType<List<UncomparedStreets>> ret = new ReturnsObjectType<List<UncomparedStreets>>();

            #region Запись в БД

            // using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            //{
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

                finder.ulica = finder.ulica.Replace('Ё', 'Е').Replace('ё', 'е');

                string sql = "";
                if (finder.nzp_raj != null && finder.nzp_raj != "")
                    sql = "select ul.*, raj.*, town.* from " + Points.Pref + "_data" + tableDelimiter + "s_ulica ul, " +
                          Points.Pref + "_data" + tableDelimiter + "s_rajon raj, " + Points.Pref + "_data" +
                          tableDelimiter + "s_town town " +
                          " where ul.nzp_raj = raj.nzp_raj and raj.nzp_town = town.nzp_town and upper(ulica) " +
                          DataImportUtils.plike + "  upper('" + finder.ulica + "') and raj.nzp_raj = " + finder.nzp_raj;
                else if (finder.nzp_town != null && finder.nzp_town != "")
                    sql = "select ul.*, raj.*, town.* from " + Points.Pref + "_data" + tableDelimiter + "s_ulica ul, " +
                          Points.Pref + "_data" + tableDelimiter + "s_rajon raj, " + Points.Pref + "_data" +
                          tableDelimiter + "s_town town " +
                          " where ul.nzp_raj = raj.nzp_raj and raj.nzp_town = town.nzp_town and upper(ul.ulica) " +
                          DataImportUtils.plike + "  upper('" + finder.ulica + "') and raj.nzp_town = " +
                          finder.nzp_town;
                else
                    sql = "select ul.*, raj.*, town.* from " + Points.Pref + "_data" + tableDelimiter + "s_ulica ul, " +
                          Points.Pref + "_data" + tableDelimiter + "s_rajon raj, " + Points.Pref + "_data" +
                          tableDelimiter + "s_town town " +
                          " where ul.nzp_raj = raj.nzp_raj and raj.nzp_town = town.nzp_town and upper(ulica) " +
                          DataImportUtils.plike + "  upper('" + finder.ulica + "')";
                var dt = ClassDBUtils.OpenSQL(sql, con_db);

                List<UncomparedStreets> list = new List<UncomparedStreets>();
                for (int i = 0; i < dt.resultData.Rows.Count; i++)
                {
                    var rajon = "";
                    if (dt.resultData.Rows[i]["rajon"] != DBNull.Value &&
                        dt.resultData.Rows[i]["rajon"].ToString().Trim() != "-" &&
                        dt.resultData.Rows[i]["rajon"].ToString().Trim() != "")
                        rajon = ", " + dt.resultData.Rows[i]["rajon"].ToString().Trim();
                    var town = "";
                    if (dt.resultData.Rows[i]["town"] != DBNull.Value &&
                        dt.resultData.Rows[i]["town"].ToString().Trim() != "-" &&
                        dt.resultData.Rows[i]["town"].ToString().Trim() != "")
                        town = dt.resultData.Rows[i]["town"].ToString().Trim();
                    var ulicareg = "";
                    if (dt.resultData.Rows[i]["ulicareg"] != DBNull.Value && dt.resultData.Rows[i]["ulicareg"].ToString().Trim() != "-" && dt.resultData.Rows[i]["ulicareg"].ToString().Trim() != "")
                        ulicareg = dt.resultData.Rows[i]["ulicareg"].ToString().Trim();
                    list.Add(new UncomparedStreets()
                    {
                        ulica = dt.resultData.Rows[i]["ulica"].ToString().Trim() + " " + ulicareg + " (" + town + rajon + ")",
                        nzp_ul = dt.resultData.Rows[i]["nzp_ul"].ToString().Trim()
                    });
                    if (PmaxVisible < i)
                    {
                        break;
                    }
                }
                ret.returnsData = list;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetStreetsByFilter : " + ex.Message,
                    MonitorLog.typelog.Error, true);
                ret.text = "Ошибка выполнения";
                ret.result = false;
                return ret;
            }
            //}

            #endregion


            ret.result = true;
            ret.text = "Выполнено.";
            ret.tag = -1;

            return ret;
        }

        /// <summary>
        /// получение домов по фильтру
        /// 1</summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<List<UncomparedHouses>> GetHouseByFilter(UncomparedHouses finder)
        {

            ReturnsObjectType<List<UncomparedHouses>> ret = new ReturnsObjectType<List<UncomparedHouses>>();

            #region Запись в БД

            // using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            //{
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
                var ndom = "";

                if (finder.dom.Split('/').Length > 1)
                {
                    for (int i = 0; i < finder.dom.Split('/').Length - 1; i++)
                        if (i == 0) ndom += finder.dom.Split('/')[i];
                        else ndom += "/" + finder.dom.Split('/')[i];

                }

                string sql = "select dom.*, ul.*, raj.*, town.* from " + Points.Pref + "_data" + tableDelimiter +
                             "s_ulica ul, " + Points.Pref + "_data" + tableDelimiter + "s_rajon raj, " + Points.Pref +
                             "_data" + tableDelimiter + "s_town town, " + finder.bank + "_data" + tableDelimiter +
                             "dom dom " +
                             " where ul.nzp_raj = raj.nzp_raj and raj.nzp_town = town.nzp_town and dom.nzp_ul = ul.nzp_ul and upper(ndom) " +
                             DataImportUtils.plike + "  upper('" + ndom + "') and ul.nzp_ul = '" + finder.nzp_ul + "'";

                if (finder.nzp_ul != null && finder.nzp_ul != "")
                    sql += " and ul.nzp_ul = " + finder.nzp_ul;
                if (finder.nzp_raj != null && finder.nzp_raj != "")
                    sql += " and raj.nzp_raj = " + finder.nzp_raj;
                if (finder.nzp_town != null && finder.nzp_town != "")
                    sql += " and raj.nzp_town = " + finder.nzp_town;

                var dt = ClassDBUtils.OpenSQL(sql, con_db);

                List<UncomparedHouses> list = new List<UncomparedHouses>();
                for (int i = 0; i < dt.resultData.Rows.Count; i++)
                {
                    var ulica = "";
                    if (dt.resultData.Rows[i]["ulica"] != DBNull.Value &&
                        dt.resultData.Rows[i]["ulica"].ToString().Trim() != "-" &&
                        dt.resultData.Rows[i]["ulica"].ToString().Trim() != "")
                        ulica = dt.resultData.Rows[i]["ulica"].ToString().Trim();
                    var rajon = "";
                    if (dt.resultData.Rows[i]["rajon"] != DBNull.Value &&
                        dt.resultData.Rows[i]["rajon"].ToString().Trim() != "-" &&
                        dt.resultData.Rows[i]["rajon"].ToString().Trim() != "")
                        rajon = ", " + dt.resultData.Rows[i]["rajon"].ToString().Trim();
                    var town = "";
                    if (dt.resultData.Rows[i]["town"] != DBNull.Value &&
                        dt.resultData.Rows[i]["town"].ToString().Trim() != "-" &&
                        dt.resultData.Rows[i]["town"].ToString().Trim() != "")
                        town = ", " + dt.resultData.Rows[i]["town"].ToString().Trim();
                    var nkor = "";
                    if (dt.resultData.Rows[i]["nkor"] != DBNull.Value &&
                        dt.resultData.Rows[i]["nkor"].ToString().Trim() != "" &&
                        dt.resultData.Rows[i]["nkor"].ToString().Trim() != "-")
                        nkor = "/" + dt.resultData.Rows[i]["nkor"].ToString().Trim();
                    list.Add(new UncomparedHouses()
                    {
                        dom = dt.resultData.Rows[i]["ndom"].ToString().Trim() + nkor + " (" + ulica + town + rajon + ")",
                        nzp_dom = dt.resultData.Rows[i]["nzp_dom"].ToString().Trim()
                    });
                    if (PmaxVisible < i)
                    {
                        break;
                    }
                }
                ret.returnsData = list;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetHouseByFilter : " + ex.Message,
                    MonitorLog.typelog.Error, true);
                ret.text = "Ошибка выполнения";
                ret.result = false;
                return ret;
            }
            //}

            #endregion


            ret.result = true;
            ret.text = "Выполнено.";
            ret.tag = -1;

            return ret;
        }

        /// <summary>
        /// получение ЛС по фильтру
        /// 1</summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<List<UncomparedLS>> GetLsByFilter(UncomparedLS finder)
        {

            ReturnsObjectType<List<UncomparedLS>> ret = new ReturnsObjectType<List<UncomparedLS>>();

            #region Поиск в БД

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

                string sql = "select distinct dom.*, ul.*, raj.*, town.*, k.*, k.remark as idk from " + Points.Pref + "_data" + tableDelimiter +
                             "s_ulica ul, " + Points.Pref + "_data" + tableDelimiter + "s_rajon raj, " + Points.Pref +
                             "_data" + tableDelimiter + "s_town town, " + finder.bank + "_data" + tableDelimiter + "dom dom, " +
                             finder.bank + "_data" + tableDelimiter + "kvar k " +
                             " where raj.nzp_town = town.nzp_town and ul.nzp_raj = raj.nzp_raj and dom.nzp_ul = ul.nzp_ul " +
                             " and dom.nzp_dom = k.nzp_dom and k.nzp_dom = '" + finder.nzp_dom + "' and k.nkvar " + DataImportUtils.plike + " '" + finder.kvar + "'";

                var dt = ClassDBUtils.OpenSQL(sql, con_db);

                List<UncomparedLS> list = new List<UncomparedLS>();
                for (int i = 0; i < dt.resultData.Rows.Count; i++)
                {
                    var ulica = "";
                    if (dt.resultData.Rows[i]["ulica"] != DBNull.Value &&
                        dt.resultData.Rows[i]["ulica"].ToString().Trim() != "-" &&
                        dt.resultData.Rows[i]["ulica"].ToString().Trim() != "")
                        ulica = dt.resultData.Rows[i]["ulica"].ToString().Trim();
                    var rajon = "";
                    if (dt.resultData.Rows[i]["rajon"] != DBNull.Value &&
                        dt.resultData.Rows[i]["rajon"].ToString().Trim() != "-" &&
                        dt.resultData.Rows[i]["rajon"].ToString().Trim() != "")
                        rajon = dt.resultData.Rows[i]["rajon"].ToString().Trim();
                    var town = "";
                    if (dt.resultData.Rows[i]["town"] != DBNull.Value &&
                        dt.resultData.Rows[i]["town"].ToString().Trim() != "-" &&
                        dt.resultData.Rows[i]["town"].ToString().Trim() != "")
                        town = dt.resultData.Rows[i]["town"].ToString().Trim();
                    var nkor = "";
                    if (dt.resultData.Rows[i]["nkor"] != DBNull.Value &&
                        dt.resultData.Rows[i]["nkor"].ToString().Trim() != "" &&
                        dt.resultData.Rows[i]["nkor"].ToString().Trim() != "-")
                        nkor =  dt.resultData.Rows[i]["nkor"].ToString().Trim();
                    var ndom = "";
                    if (dt.resultData.Rows[i]["ndom"] != DBNull.Value &&
                        dt.resultData.Rows[i]["ndom"].ToString().Trim() != "" &&
                        dt.resultData.Rows[i]["ndom"].ToString().Trim() != "-")
                        ndom = dt.resultData.Rows[i]["ndom"].ToString().Trim();
                    var nkvar = "";
                    if (dt.resultData.Rows[i]["nkvar"] != DBNull.Value &&
                        dt.resultData.Rows[i]["nkvar"].ToString().Trim() != "" )
                        nkvar = dt.resultData.Rows[i]["nkvar"].ToString().Trim();
                    var fio = "";
                    if (dt.resultData.Rows[i]["fio"] != DBNull.Value &&
                        dt.resultData.Rows[i]["fio"].ToString().Trim() != "" &&
                        dt.resultData.Rows[i]["fio"].ToString().Trim() != "-")
                        fio = dt.resultData.Rows[i]["fio"].ToString().Trim().ToUpper();
                    var id = "";
                    if (dt.resultData.Rows[i]["idk"] != DBNull.Value &&
                        dt.resultData.Rows[i]["idk"].ToString().Trim() != "" &&
                        dt.resultData.Rows[i]["idk"].ToString().Trim() != "-")
                        id = dt.resultData.Rows[i]["idk"].ToString().Trim();
                    var kom = "";
                    if (dt.resultData.Rows[i]["nkvar_n"] != DBNull.Value &&
                        dt.resultData.Rows[i]["nkvar_n"].ToString().Trim() != "")
                        kom = dt.resultData.Rows[i]["nkvar_n"].ToString().Trim();

                    list.Add(new UncomparedLS()
                    {
                        kvar = dt.resultData.Rows[i]["nzp_kvar"].ToString() + ", " + fio + " (" + town + ", " + rajon + ", " + ulica + 
                        " " + ndom + "/" + nkor + ", КВ " + nkvar + ", КОМ " + kom + ")",
                        nzp_kvar = dt.resultData.Rows[i]["nzp_kvar"].ToString().Trim(),
                        nzp_dom = dt.resultData.Rows[i]["nzp_dom"].ToString().Trim(),
                        id = id
                    });
                    if (PmaxVisible < i)
                    {
                        break;
                    }
                }
                ret.returnsData = list;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetLsByFilter : " + ex.Message,
                    MonitorLog.typelog.Error, true);
                ret.text = "Ошибка выполнения";
                ret.result = false;
                return ret;
            }

            #endregion


            ret.result = true;
            ret.text = "Выполнено.";
            ret.tag = -1;

            return ret;
        }

        /// <summary>
        /// изменение города для района
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        public ReturnsType ChangeTownForRajon(UncomparedRajons finder)
        {
            ReturnsType ret = new ReturnsType();

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
                string sql =
                    " update " + Points.Pref + DBManager.sUploadAliasRest +
                    "file_dom set town = trim(town )||', '||trim(rajon) " +
                    " where upper(rajon) " + DataImportUtils.plike + " upper('" + finder.rajon + "') " +
                    " and nzp_file in " +
                    "(select l.nzp_file from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected l, " +
                    Points.Pref + DBManager.sUploadAliasRest + "files_imported fi " +
                    " where fi.nzp_file=l.nzp_file and trim(fi.pref)='" + finder.bank + "'" +
                    " and l.nzp_user = +" + finder.nzp_user + ")";
                IDbCommand IfxCommand = DBManager.newDbCommand(sql, con_db);
                IfxCommand.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры ChangeTownForRajon : " + ex.Message,
                    MonitorLog.typelog.Error, true);
                ret.text = "Ошибка выполнения";
                ret.result = false;
                return ret;
            }

            #endregion

            ret.result = true;
            ret.text = "Выполнено.";

            return ret;
        }

        /// <summary>
        /// получение несопоставленных домов
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public
            Returns AddAllHouse(FilesImported finder)
        {

            Returns ret = new Returns();

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

                IDbCommand IfxCommand;
                IDbTransaction tr_id = con_db.BeginTransaction();

                string sql = " update " + Points.Pref + DBManager.sUploadAliasRest + "file_dom " +
                             " set nzp_dom = (select nzp_dom from " + finder.bank + "_data" + tableDelimiter +
                             "dom a where a.nzp_ul=" + Points.Pref + DBManager.sUploadAliasRest + "file_dom.nzp_ul " +
                             " and trim(upper(a.ndom))=trim(upper(" + Points.Pref + DBManager.sUploadAliasRest +
                             "file_dom.ndom)) " +
                             " and trim(upper(a.nkor))=trim(upper(" + Points.Pref + DBManager.sUploadAliasRest +
                             "file_dom.nkor)) and a.nzp_ul>0 and a.ndom is not null and a.nkor is not null ) " +
                             " where " + Points.Pref + DBManager.sUploadAliasRest + "file_dom.nzp_dom is null and " +
                             Points.Pref + DBManager.sUploadAliasRest + "file_dom.nzp_file in (select l.nzp_file from " +
                             Points.Pref + DBManager.sUploadAliasRest + "files_selected l, " +
                             Points.Pref + DBManager.sUploadAliasRest +
                             "files_imported fi where fi.nzp_file=l.nzp_file and trim(fi.pref)='" + finder.bank + "')";
                IfxCommand = DBManager.newDbCommand(sql, con_db, tr_id);
                IfxCommand.ExecuteNonQuery();
                tr_id.Commit();


                sql =
                    "select distinct UPPER(ndom) as ndom, UPPER(nkor) as nkor, nzp_dom, nzp_town, nzp_raj, nzp_ul, UPPER(rajon) as rajon, UPPER(town) as town, UPPER(ulica) as ulica " +
                    " from " + Points.Pref + DBManager.sUploadAliasRest +
                    "file_dom f where ( nzp_dom is null  or (select count(*) from " + Points.Pref + "_data" +
                    tableDelimiter + "dom b where b.nzp_dom = f.nzp_dom) = 0 ) " +
                    " and nzp_file in (select l.nzp_file from " + Points.Pref + DBManager.sUploadAliasRest +
                    "files_selected l, " +
                    Points.Pref + DBManager.sUploadAliasRest +
                    "files_imported fi where fi.nzp_file=l.nzp_file and trim(fi.pref)='" + finder.bank +
                    "') order by  ndom, town, rajon, ulica ";

                var dt = ClassDBUtils.OpenSQL(sql, con_db);

                foreach (DataRow rr in dt.resultData.Rows)
                {
                    UncomparedHouses finder_house = new UncomparedHouses()
                    {
                        dom = rr["ndom"].ToString() + "/" + rr["nkor"],
                        nzp_raj = rr["nzp_raj"].ToString(),
                        nzp_ul = rr["nzp_ul"].ToString(),
                        nzp_dom = rr["nzp_dom"].ToString(),
                        nzp_town = rr["nzp_town"].ToString(),
                        bank = finder.bank
                    };

                    AddNewHouse(finder_house, con_db);
                }

                sql =
                " UPDATE  " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar " +
                " SET nzp_dom =" +
                    "( SELECT max(nzp_dom ) FROM " + Points.Pref + DBManager.sUploadAliasRest + " file_dom " +
                    " WHERE id =" + Points.Pref + DBManager.sUploadAliasRest + "file_kvar.dom_id AND nzp_file IN" +
                        " (SELECT l.nzp_file FROM " + Points.Pref + DBManager.sUploadAliasRest + "files_selected l, " +
                        Points.Pref + DBManager.sUploadAliasRest + "files_imported fi" +
                        " WHERE fi.nzp_file=l.nzp_file and trim(fi.pref)='" + finder.bank + "')) " +
                " WHERE nzp_file IN" +
                    " (SELECT l.nzp_file FROM " + Points.Pref + DBManager.sUploadAliasRest +"files_selected l, " +
                    Points.Pref + DBManager.sUploadAliasRest + "files_imported fi" +
                    " WHERE fi.nzp_file=l.nzp_file AND trim(fi.pref)='" + finder.bank + "')" +
                " AND nzp_dom IS NULL";

                ClassDBUtils.ExecSQL(sql, con_db, ClassDBUtils.ExecMode.Exception);

                ClassDBUtils.ExecSQL(DBManager.sUpdStat + " " + Points.Pref + DBManager.sUploadAliasRest + "file_dom ", con_db, ClassDBUtils.ExecMode.Log);
                ClassDBUtils.ExecSQL(DBManager.sUpdStat + " " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar ", con_db, ClassDBUtils.ExecMode.Log);

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры AddAllHouse : " + ex.Message, MonitorLog.typelog.Error,
                    true);
                ret.text = "Ошибка выполнения";
                ret.result = false;
                return ret;
            }

            #endregion


            ret.result = true;
            ret.text = "Выполнено.";

            return ret;
        }

        /// <summary>
        /// автоматическое сопоставление улиц
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public Returns LinkStreetAutom(FilesImported finder)
        {

            Returns ret = new Returns();

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

                IDbCommand IfxCommand;
                IDbTransaction tr_id = con_db.BeginTransaction();
                string sql;

                string[] postfix = new string[] { "", " УЛ", " УЛ.", "  УЛ.", " ПЕР", " ПЕР.", " ПР-КТ", " КМ", " ПРОЕЗД", " ПРОСПЕКТ", " КМ УЛ." };
                foreach (string post in postfix)
                {
                    sql = "update " + Points.Pref + DBManager.sUploadAliasRest + "file_dom " +
                                          "  set nzp_ul = (select max(nzp_ul) from " + Points.Pref + "_data" + tableDelimiter + "s_ulica a " +
                                          " where trim(upper(REPLACE(" + Points.Pref + DBManager.sUploadAliasRest +
                                          "file_dom.ulica, 'ё', 'е'))) =trim(upper(trim(a.ulica))||'" + post + "') and " +
                                          " a.nzp_raj = " + Points.Pref + DBManager.sUploadAliasRest + "file_dom .nzp_raj) " +
                                          " where " + Points.Pref + DBManager.sUploadAliasRest +
                                          "file_dom.nzp_file in  (select l.nzp_file from " + Points.Pref + DBManager.sUploadAliasRest +
                                          "files_selected l, " +
                                          Points.Pref + DBManager.sUploadAliasRest +
                                          "files_imported fi where fi.nzp_file=l.nzp_file and trim(fi.pref)='" + finder.bank + "') and " +
                                          sNvlWord + "(" + Points.Pref + DBManager.sUploadAliasRest + "file_dom.nzp_ul,0)=0 " +
                                          " and " + Points.Pref + DBManager.sUploadAliasRest + "file_dom.nzp_raj is not null";
                    IfxCommand = DBManager.newDbCommand(sql, con_db, tr_id);
                    IfxCommand.ExecuteNonQuery();


                    sql = "update " + Points.Pref + DBManager.sUploadAliasRest + "file_dom " +
                          "  set nzp_ul = (select max(nzp_ul) from " + Points.Pref + "_data" + tableDelimiter + "s_ulica a " +
                          " where trim(upper(" + Points.Pref + DBManager.sUploadAliasRest +
                          "file_dom.ulica)) =trim('" + post + "'||upper(trim(a.ulica))) and " +
                          " a.nzp_raj = " + Points.Pref + DBManager.sUploadAliasRest + "file_dom .nzp_raj) " +
                          " where " + Points.Pref + DBManager.sUploadAliasRest +
                          "file_dom.nzp_file in  (select l.nzp_file from " + Points.Pref + DBManager.sUploadAliasRest +
                          "files_selected l, " +
                          Points.Pref + DBManager.sUploadAliasRest +
                          "files_imported fi where fi.nzp_file=l.nzp_file and trim(fi.pref)='" + finder.bank + "') and " +
                          sNvlWord + "(" + Points.Pref + DBManager.sUploadAliasRest + "file_dom.nzp_ul,0)=0 " +
                          " and " + Points.Pref + DBManager.sUploadAliasRest + "file_dom.nzp_raj is not null";
                    IfxCommand = DBManager.newDbCommand(sql, con_db, tr_id);
                    IfxCommand.ExecuteNonQuery();
                }


                tr_id.Commit();

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры LinkStreetAutom : " + ex.Message,
                    MonitorLog.typelog.Error, true);
                ret.text = "Ошибка выполнения";
                ret.result = false;
                return ret;
            }

            #endregion


            ret.result = true;
            ret.text = "Выполнено.";

            return ret;
        }

        /// <summary>
        /// автоматическое сопоставление населенных пунктов
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public Returns LinkRajonAutom(FilesImported finder)
        {

            Returns ret = new Returns();

            #region Запись в БД

            // using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            //{
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

                IDbCommand IfxCommand;
                IDbTransaction tr_id = con_db.BeginTransaction();
                string sql;

                sql = "update " + Points.Pref + DBManager.sUploadAliasRest + "file_dom " +
                      "  set nzp_raj = (select nzp_raj from " + Points.Pref + "_data" + tableDelimiter + "s_rajon a " +
                      " where trim(upper(" + Points.Pref + DBManager.sUploadAliasRest +
                      "file_dom.rajon)) =trim(upper(a.rajon)) and " +
                      " a.nzp_town = " + Points.Pref + DBManager.sUploadAliasRest + "file_dom .nzp_town) " +
                      " where " + Points.Pref + DBManager.sUploadAliasRest +
                      "file_dom.nzp_file in  (select l.nzp_file from " + Points.Pref + DBManager.sUploadAliasRest +
                      "files_selected l, " +
                      Points.Pref + DBManager.sUploadAliasRest +
                      "files_imported fi where fi.nzp_file=l.nzp_file and trim(fi.pref)='" + finder.bank + "') and " +
                      sNvlWord + "(nzp_raj,0)=0 " +
                      " and " + Points.Pref + DBManager.sUploadAliasRest + "file_dom.nzp_town is not null";
                IfxCommand = DBManager.newDbCommand(sql, con_db, tr_id);
                IfxCommand.ExecuteNonQuery();


                tr_id.Commit();

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры LinkRajonAutom : " + ex.Message,
                    MonitorLog.typelog.Error, true);
                ret.text = "Ошибка выполнения";
                ret.result = false;
                return ret;
            }
            //}

            #endregion


            ret.result = true;
            ret.text = "Выполнено.";

            return ret;
        }

        /// <summary>
        /// автоматическое сопоставление ЛС
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public Returns LinkLSAutom(FilesImported finder)
        {

            Returns ret = new Returns();

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

                IDbCommand IfxCommand;
                IDbTransaction tr_id = con_db.BeginTransaction();
                string sql;

                sql = "update " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar " +
                      "  set nzp_kvar = (select nzp_kvar from " + finder.bank + "_data" + tableDelimiter + "kvar a " +
                      " where trim(upper(" + Points.Pref + DBManager.sUploadAliasRest +
                      "file_kvar.nkvar)) =trim(upper(a.nkvar)) and " +
                      " trim(upper(" + Points.Pref + DBManager.sUploadAliasRest +
                      "file_kvar.nkvar_n)) =trim(upper(a.nkvar_n)) and" +
                      " trim(upper(" + Points.Pref + DBManager.sUploadAliasRest +
                      "file_kvar.id)) =trim(upper(a.remark)) and" +
                      " a.nzp_dom = " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar.nzp_dom) " +
                      " where " + Points.Pref + DBManager.sUploadAliasRest +
                      "file_kvar.nzp_file in  (select l.nzp_file from " + Points.Pref + DBManager.sUploadAliasRest +
                      "files_selected l, " +
                      Points.Pref + DBManager.sUploadAliasRest +
                      "files_imported fi where fi.nzp_file=l.nzp_file and trim(fi.pref)='" + finder.bank + "') and " +
                      sNvlWord + "(nzp_kvar,0)=0 " +
                      " and " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar.nzp_dom is not null";
                IfxCommand = DBManager.newDbCommand(sql, con_db, tr_id);
                IfxCommand.ExecuteNonQuery();

                //для file_type = 99 делаем update без remark

                sql = "update " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar " +
                      "  set nzp_kvar = (select nzp_kvar from " + finder.bank + "_data" + tableDelimiter + "kvar a " +
                      " where trim(upper(" + Points.Pref + DBManager.sUploadAliasRest +
                      "file_kvar.nkvar)) =trim(upper(a.nkvar)) and " +
                      " trim(upper(" + Points.Pref + DBManager.sUploadAliasRest +
                      "file_kvar.nkvar_n)) =trim(upper(a.nkvar_n)) and" +
                      " a.nzp_dom = " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar.nzp_dom) " +
                      " where " + Points.Pref + DBManager.sUploadAliasRest +
                      "file_kvar.nzp_file in  (select l.nzp_file from " + Points.Pref + DBManager.sUploadAliasRest +
                      "files_selected l, " +
                      Points.Pref + DBManager.sUploadAliasRest +
                      "files_imported fi where fi.nzp_file=l.nzp_file and trim(fi.pref)='" + finder.bank + "' " +
                      " AND fi.file_type = 99 ) and " +
                      sNvlWord + "(nzp_kvar,0)=0 " +
                      " and " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar.nzp_dom is not null " +
                      " AND 1 = " +
                      "( SELECT count(*) FROM " + finder.bank + "_data" + tableDelimiter + "kvar a " +
                      " WHERE trim(upper(" + Points.Pref + DBManager.sUploadAliasRest +
                      "file_kvar.nkvar)) =trim(upper(a.nkvar)) and " +
                      " trim(upper(" + Points.Pref + DBManager.sUploadAliasRest +
                      "file_kvar.nkvar_n)) =trim(upper(a.nkvar_n)) and" +
                      " a.nzp_dom = " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar.nzp_dom)";
                IfxCommand = DBManager.newDbCommand(sql, con_db, tr_id);
                IfxCommand.ExecuteNonQuery();


                tr_id.Commit();

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры LinkLSAutom : " + ex.Message,
                    MonitorLog.typelog.Error, true);
                ret.text = "Ошибка выполнения";
                ret.result = false;
                return ret;
            }

            #endregion


            ret.result = true;
            ret.text = "Выполнено.";

            return ret;
        }

        #endregion Функции получения данных для отображения

        #region Функции сохранения

        /// <summary>
        /// сохранение nzp_area
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsType LinkArea(UncomparedAreas finder)
        {

            ReturnsType ret = new ReturnsType();

            #region Запись в БД

            // using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            //{
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

                string sql = " update " + Points.Pref + DBManager.sUploadAliasRest + "file_area set nzp_area = '" +
                             finder.nzp_area + "' where nzp_area is null and " +
                             " upper(area) = '" + finder.area.ToUpper() + "' and nzp_file in (select l.nzp_file from " +
                             Points.Pref + DBManager.sUploadAliasRest + "files_selected l, " +
                             Points.Pref + DBManager.sUploadAliasRest +
                             "files_imported fi where fi.nzp_file=l.nzp_file and trim(fi.pref)='" + finder.bank + "')";
                IDbCommand IfxCommand = DBManager.newDbCommand(sql, con_db);
                IfxCommand.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры LinkArea : " + ex.Message, MonitorLog.typelog.Error,
                    true);
                ret.text = "Ошибка выполнения";
                ret.result = false;
                return ret;
            }
            //}

            #endregion


            ret.result = true;
            ret.text = "Выполнено.";
            ret.tag = -1;

            return ret;
        }

        /// <summary>
        /// сохранение nzp_supp
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsType LinkSupp(UncomparedSupps finder)
        {

            ReturnsType ret = new ReturnsType();

            #region Запись в БД

            // using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            //{
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
                string sql = " update " + Points.Pref + DBManager.sUploadAliasRest + "file_supp set nzp_supp = '" +
                             finder.nzp_supp + "' where nzp_supp is null and " +
                             " upper(supp_name) = '" + finder.supp.ToUpper() +
                             "' and nzp_file in (select l.nzp_file from " + Points.Pref + DBManager.sUploadAliasRest +
                             "files_selected l, " +
                             Points.Pref + DBManager.sUploadAliasRest +
                             "files_imported fi where fi.nzp_file=l.nzp_file and trim(fi.pref)='" + finder.bank + "')";

                IDbCommand IfxCommand = DBManager.newDbCommand(sql, con_db);
                IfxCommand.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры LinkSupp : " + ex.Message, MonitorLog.typelog.Error,
                    true);
                ret.text = "Ошибка выполнения";
                ret.result = false;
                return ret;
            }
            //}

            #endregion


            ret.result = true;
            ret.text = "Выполнено.";
            ret.tag = -1;

            return ret;
        }

        /// <summary>
        /// сохранение nzp_vill
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsType LinkMO(UncomparedVills finder)
        {

            ReturnsType ret = new ReturnsType();

            #region Запись в БД

            // using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            //{
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

                string sql = " update " + Points.Pref + DBManager.sUploadAliasRest + "file_mo set nzp_vill = '" +
                             finder.nzp_vill + "' where nzp_vill is null and " +
                             " upper(vill) = '" + finder.vill.ToUpper() + "' and nzp_file in (select l.nzp_file from " +
                             Points.Pref + DBManager.sUploadAliasRest + "files_selected l, " +
                             Points.Pref + DBManager.sUploadAliasRest +
                             "files_imported fi where fi.nzp_file=l.nzp_file and trim(fi.pref)='" + finder.bank + "')";

                IDbCommand IfxCommand = DBManager.newDbCommand(sql, con_db);
                IfxCommand.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры LinkMO : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = "Ошибка выполнения";
                ret.result = false;
                return ret;
            }
            //}

            #endregion


            ret.result = true;
            ret.text = "Выполнено.";
            ret.tag = -1;

            return ret;
        }

        /// <summary>
        /// сохранение nzp_serv
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsType LinkServ(UncomparedServs finder)
        {

            ReturnsType ret = new ReturnsType();

            #region Запись в БД

            // using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            //{
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
                string sql = " update " + Points.Pref + DBManager.sUploadAliasRest + "file_services set nzp_serv = '" +
                             finder.nzp_serv + "' where nzp_serv is null and " +
                             " upper(service) = '" + finder.serv.ToUpper() +
                             "' and nzp_file in (select l.nzp_file from " + Points.Pref + DBManager.sUploadAliasRest +
                             "files_selected l, " +
                             Points.Pref + DBManager.sUploadAliasRest +
                             "files_imported fi where fi.nzp_file=l.nzp_file and trim(fi.pref)='" + finder.bank + "')";

                IDbCommand IfxCommand = DBManager.newDbCommand(sql, con_db);
                IfxCommand.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры LinkServ : " + ex.Message, MonitorLog.typelog.Error,
                    true);
                ret.text = "Ошибка выполнения";
                ret.result = false;
                return ret;
            }
            //}

            #endregion


            ret.result = true;
            ret.text = "Выполнено.";
            ret.tag = -1;

            return ret;
        }

        /// <summary>
        /// сохранение типа доп. параметров
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsType LinkParType(UncomparedParTypes finder)
        {

            ReturnsType ret = new ReturnsType();

            #region Запись в БД

            // using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            //{
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

                string sql = " update " + Points.Pref + DBManager.sUploadAliasRest + "file_typeparams set nzp_prm = '" +
                             finder.nzp_prm + "' where nzp_prm is null and " +
                             " upper(prm_name) = '" + finder.name_prm.ToUpper() +
                             "' and nzp_file in (select l.nzp_file from " + Points.Pref + DBManager.sUploadAliasRest +
                             "files_selected l, " +
                             Points.Pref + DBManager.sUploadAliasRest +
                             "files_imported fi where fi.nzp_file=l.nzp_file and trim(fi.pref)='" + finder.bank + "')";
                IDbCommand IfxCommand = DBManager.newDbCommand(sql, con_db);
                IfxCommand.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры LinkParType : " + ex.Message, MonitorLog.typelog.Error,
                    true);
                ret.text = "Ошибка выполнения";
                ret.result = false;
                return ret;
            }
            //}

            #endregion


            ret.result = true;
            ret.text = "Выполнено.";
            ret.tag = -1;

            return ret;
        }

        /// <summary>
        /// сохранение типа благоустройства дома
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsType LinkParBlag(UncomparedParTypes finder)
        {

            ReturnsType ret = new ReturnsType();

            #region Запись в БД

            // using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            //{
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
                string sql = " update " + Points.Pref + DBManager.sUploadAliasRest + "file_blag set nzp_prm = '" +
                             finder.nzp_prm + "' where nzp_prm is null and " +
                             " upper(name) = '" + finder.name_prm.ToUpper() +
                             "' and nzp_file in (select l.nzp_file from " + Points.Pref + DBManager.sUploadAliasRest +
                             "files_selected l, " +
                             Points.Pref + DBManager.sUploadAliasRest +
                             "files_imported fi where fi.nzp_file=l.nzp_file and trim(fi.pref)='" + finder.bank + "')";
                IDbCommand IfxCommand = DBManager.newDbCommand(sql, con_db);
                IfxCommand.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры LinkParBlag : " + ex.Message, MonitorLog.typelog.Error,
                    true);
                ret.text = "Ошибка выполнения";
                ret.result = false;
                return ret;
            }
            //}

            #endregion


            ret.result = true;
            ret.text = "Выполнено.";
            ret.tag = -1;

            return ret;
        }

        /// <summary>
        /// сохранение типа дома по газу
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsType LinkParGas(UncomparedParTypes finder)
        {

            ReturnsType ret = new ReturnsType();

            #region Запись в БД

            // using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            //{
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
                string sql = " update " + Points.Pref + DBManager.sUploadAliasRest + "file_gaz set nzp_prm = '" +
                             finder.nzp_prm + "' where nzp_prm is null and " +
                             " upper(name) = '" + finder.name_prm.ToUpper() +
                             "' and nzp_file in (select l.nzp_file from " + Points.Pref + DBManager.sUploadAliasRest +
                             "files_selected l, " +
                             Points.Pref + DBManager.sUploadAliasRest +
                             "files_imported fi where fi.nzp_file=l.nzp_file and trim(fi.pref)='" + finder.bank + "')";

                IDbCommand IfxCommand = DBManager.newDbCommand(sql, con_db);
                IfxCommand.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры LinkParGas : " + ex.Message, MonitorLog.typelog.Error,
                    true);
                ret.text = "Ошибка выполнения";
                ret.result = false;
                return ret;
            }
            //}

            #endregion


            ret.result = true;
            ret.text = "Выполнено.";
            ret.tag = -1;

            return ret;
        }

        /// <summary>
        /// сохранение типа дома по воде
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsType LinkParWater(UncomparedParTypes finder)
        {

            ReturnsType ret = new ReturnsType();

            #region Запись в БД

            // using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            //{
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

                string sql = " update " + Points.Pref + DBManager.sUploadAliasRest + "file_voda set nzp_prm = '" +
                             finder.nzp_prm + "' where nzp_prm is null and " +
                             " upper(name) = '" + finder.name_prm.ToUpper() +
                             "' and nzp_file in (select l.nzp_file from " + Points.Pref + DBManager.sUploadAliasRest +
                             "files_selected l, " +
                             Points.Pref + DBManager.sUploadAliasRest +
                             "files_imported fi where fi.nzp_file=l.nzp_file and trim(fi.pref)='" + finder.bank + "')";

                IDbCommand IfxCommand = DBManager.newDbCommand(sql, con_db);
                IfxCommand.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры LinkParWater : " + ex.Message, MonitorLog.typelog.Error,
                    true);
                ret.text = "Ошибка выполнения";
                ret.result = false;
                return ret;
            }
            //}

            #endregion


            ret.result = true;
            ret.text = "Выполнено.";
            ret.tag = -1;

            return ret;
        }

        /// <summary>
        /// сохранение nzp_measure
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsType LinkMeasure(UncomparedMeasures finder)
        {

            ReturnsType ret = new ReturnsType();

            #region Запись в БД

            // using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            //{
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

                string sql = " update " + Points.Pref + DBManager.sUploadAliasRest + "file_measures set nzp_measure = '" +
                             finder.nzp_measure + "' where nzp_measure is null and " +
                             " upper(measure) = '" + finder.measure.ToUpper() +
                             "' and nzp_file in (select l.nzp_file from " + Points.Pref + DBManager.sUploadAliasRest +
                             "files_selected l, " +
                             Points.Pref + DBManager.sUploadAliasRest +
                             "files_imported fi where fi.nzp_file=l.nzp_file and trim(fi.pref)='" + finder.bank + "')";

                IDbCommand IfxCommand = DBManager.newDbCommand(sql, con_db);
                IfxCommand.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры LinkMeasure : " + ex.Message, MonitorLog.typelog.Error,
                    true);
                ret.text = "Ошибка выполнения";
                ret.result = false;
                return ret;
            }
            //}

            #endregion


            ret.result = true;
            ret.text = "Выполнено.";
            ret.tag = -1;

            return ret;
        }

        /// <summary>
        /// сохранение nzp_town
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsType LinkTown(UncomparedTowns finder)
        {

            ReturnsType ret = new ReturnsType();

            #region Запись в БД

            // using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            //{
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

                string sql = " update " + Points.Pref + DBManager.sUploadAliasRest + "file_dom set nzp_town = '" +
                             finder.nzp_town + "' where nzp_town is null and " +
                             " upper(town) = '" + finder.town.ToUpper() + "' and nzp_file in (select l.nzp_file from " +
                             Points.Pref + DBManager.sUploadAliasRest + "files_selected l, " +
                             Points.Pref + DBManager.sUploadAliasRest +
                             "files_imported fi where fi.nzp_file=l.nzp_file and trim(fi.pref)='" + finder.bank + "')";
                IDbCommand IfxCommand = DBManager.newDbCommand(sql, con_db);
                IfxCommand.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры LinkTown : " + ex.Message, MonitorLog.typelog.Error,
                    true);
                ret.text = "Ошибка выполнения";
                ret.result = false;
                return ret;
            }
            //}

            #endregion


            ret.result = true;
            ret.text = "Выполнено.";
            ret.tag = -1;

            return ret;
        }

        /// <summary>
        /// сохранение nzp_ul
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsType LinkRajon(UncomparedRajons finder)
        {

            ReturnsType ret = new ReturnsType();

            #region Запись в БД

            // using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            //{
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
                string param = "";
                if (finder.nzp_town != null)
                    param = " and nzp_town = " + finder.nzp_town + " ";
                string rajon_param = "";
                if (finder.rajon != "")
                    rajon_param = " and upper(rajon) = '" + finder.rajon.ToUpper() + "' ";
                else
                    rajon_param = " and rajon is null ";
                string sql = "update " + Points.Pref + DBManager.sUploadAliasRest + "file_dom set nzp_raj = '" +
                             finder.nzp_raj + "' where nzp_raj is null and " +
                             " nzp_file in (select l.nzp_file from " + Points.Pref + DBManager.sUploadAliasRest +
                             "files_selected l, " +
                             Points.Pref + DBManager.sUploadAliasRest +
                             "files_imported fi where fi.nzp_file=l.nzp_file and trim(fi.pref)='" + finder.bank + "') " +
                             param + rajon_param;

                IDbCommand IfxCommand = DBManager.newDbCommand(sql, con_db);
                IfxCommand.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры LinkRajon : " + ex.Message, MonitorLog.typelog.Error,
                    true);
                ret.text = "Ошибка выполнения";
                ret.result = false;
                return ret;
            }
            //}

            #endregion


            ret.result = true;
            ret.text = "Выполнено.";
            ret.tag = -1;

            return ret;
        }

        /// <summary>
        /// сохранение nzp_ul
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsType LinkNzpStreet(UncomparedStreets finder)
        {

            ReturnsType ret = new ReturnsType();

            #region Запись в БД

            // using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            //{
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

                string sql = "update " + Points.Pref + DBManager.sUploadAliasRest + "file_dom set nzp_ul = '" +
                             finder.nzp_ul + "' where nzp_ul is null and " +
                             " trim(replace(upper(ulica), 'Ё', 'Е')) = upper(trim('" + finder.ulica.ToUpper() + "')) and nzp_raj = " +
                             finder.nzp_raj + " and nzp_file in (select l.nzp_file from " + Points.Pref +
                             DBManager.sUploadAliasRest + "files_selected l, " +
                             Points.Pref + DBManager.sUploadAliasRest +
                             "files_imported fi where fi.nzp_file=l.nzp_file and trim(fi.pref)='" + finder.bank + "')";

                IDbCommand IfxCommand = DBManager.newDbCommand(sql, con_db);
                IfxCommand.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры LinkNzpStreet : " + ex.Message,
                    MonitorLog.typelog.Error, true);
                ret.text = "Ошибка выполнения";
                ret.result = false;
                return ret;
            }
            //}

            #endregion


            ret.result = true;
            ret.text = "Выполнено.";
            ret.tag = -1;

            return ret;
        }

        /// <summary>
        /// сохранение nzp_dom
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsType LinkNzpDom(UncomparedHouses finder)
        {

            ReturnsType ret = new ReturnsType();

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
                var ndom = "";
                var nkor = "";
                if (finder.dom.Split('/').Length > 1)
                {
                    for (int i = 0; i < finder.dom.Split('/').Length - 1; i++)
                        if (i == 0) ndom += finder.dom.Split('/')[i];
                        else ndom += "/" + finder.dom.Split('/')[i];
                    nkor = " and upper(nkor) = '" + finder.dom.Split('/')[finder.dom.Split('/').Length - 1] + "'";
                }

                string sql = " update " + Points.Pref + DBManager.sUploadAliasRest + "file_dom set nzp_dom = '" +
                             finder.nzp_dom + "' where nzp_dom is null and upper(ndom) = '" + ndom + "'" + nkor +
                             " and nzp_ul = '" + finder.nzp_ul + "' and nzp_file in (select l.nzp_file from " +
                             Points.Pref + DBManager.sUploadAliasRest + "files_selected l, " +
                             Points.Pref + DBManager.sUploadAliasRest +
                             "files_imported fi where fi.nzp_file=l.nzp_file and trim(fi.pref)='" + finder.bank + "')";

                IDbCommand IfxCommand = DBManager.newDbCommand(sql, con_db);
                IfxCommand.ExecuteNonQuery();

                sql =
                     " UPDATE  " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar " +
                     " SET nzp_dom =" +
                          "( SELECT max(nzp_dom ) FROM " + Points.Pref + DBManager.sUploadAliasRest + " file_dom " +
                          " WHERE id =" + Points.Pref + DBManager.sUploadAliasRest + "file_kvar.dom_id AND nzp_file IN" +
                             " (SELECT l.nzp_file FROM " + Points.Pref + DBManager.sUploadAliasRest + "files_selected l, " +
                             Points.Pref + DBManager.sUploadAliasRest + "files_imported fi" +
                             " WHERE fi.nzp_file=l.nzp_file and trim(fi.pref)='" + finder.bank + "')) " +
                     " WHERE nzp_file IN" +
                          " (SELECT l.nzp_file FROM " + Points.Pref + DBManager.sUploadAliasRest + "files_selected l, " +
                          Points.Pref + DBManager.sUploadAliasRest + "files_imported fi" +
                          " WHERE fi.nzp_file=l.nzp_file AND trim(fi.pref)='" + finder.bank + "')" +
                     " AND nzp_dom IS NULL";

                IfxCommand = DBManager.newDbCommand(sql, con_db);
                IfxCommand.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры LinkNzpDom : " + ex.Message, MonitorLog.typelog.Error,
                    true);
                ret.text = "Ошибка выполнения";
                ret.result = false;
                return ret;
            }

            #endregion


            ret.result = true;
            ret.text = "Выполнено.";
            ret.tag = -1;

            return ret;
        }

        /// <summary>
        /// сохранение nzp_kvar
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsType LinkLs(UncomparedLS finder)
        {

            ReturnsType ret = new ReturnsType();

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

                string sql = " update " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar" +
                             " set nzp_kvar = '" + finder.nzp_kvar + "'" +
                             " where nzp_dom = '" +finder.nzp_dom + "' and id = " + finder.id + " and nzp_file in" +
                             " (select l.nzp_file from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected l, " +
                             Points.Pref + DBManager.sUploadAliasRest + "files_imported fi" +
                             " where fi.nzp_file=l.nzp_file and trim(fi.pref)='" + finder.bank + "')";

                IDbCommand IfxCommand = DBManager.newDbCommand(sql, con_db);
                IfxCommand.ExecuteNonQuery();

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры LinkLs : " + ex.Message, MonitorLog.typelog.Error,
                    true);
                ret.text = "Ошибка выполнения";
                ret.result = false;
                return ret;
            }

            #endregion


            ret.result = true;
            ret.text = "Выполнено.";
            ret.tag = -1;

            return ret;
        }

        #endregion Функции сохранения

        #region Функции добавления значений в страницах сопоставления

        /// <summary>
        /// добавление нового участка в базу
        /// </summary>
        /// <returns>результат</returns>
        public ReturnsType AddNewArea(UncomparedAreas finder)
        {
            ReturnsType ret = new ReturnsType();

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
                IDbTransaction tr_id = con_db.BeginTransaction();
                try
                {
                    string sql = "";
                    IDbCommand IfxCommand;

                    decimal nzp_area = 0;
                    decimal nzp_supp = 0;
                    //decimal nzp_payer = 0;
                    string seq = "";
                    decimal nzp_supp_payer = 0;
                    Returns ret1 = new Returns();

                    sql = " select count(*) from " + finder.bank + "_data" + tableDelimiter +
                          "s_area where UPPER(area) = '" + finder.area.ToUpper() + "'";
                    IfxCommand = DBManager.newDbCommand(sql, con_db, tr_id);
                    if (Convert.ToInt32(IfxCommand.ExecuteScalar()) != 0)
                    {
                        tr_id.Rollback();
                        ret.text = "Участок с таким наименованием уже добавлен в базу";
                        ret.result = false;
                        return ret;
                    }


                    #region supplier

                    sql = "select max(nzp_supp) from " + finder.bank + "_kernel" + tableDelimiter + "supplier" +
                          " where trim(upper(name_supp)) = '" + finder.area.ToUpper().Trim() + "'";
                    var nzp_s = ExecScalar(con_db, tr_id, sql, out ret1, true);
                    if (nzp_s != DBNull.Value && Convert.ToDecimal(nzp_s) != 0) nzp_supp = Convert.ToDecimal(nzp_s);
                    else
                    {
                        #region если нет, добавляем в supplier

                        seq = Points.Pref + "_kernel" + tableDelimiter + "supplier_nzp_supp_seq";
                        string strNzp_supp;
#if PG
                        strNzp_supp = " nextval('" + seq + "') ";
#else
                        strNzp_supp = seq + ".nextval ";
#endif
                        sql = " insert into " + finder.bank + "_kernel" + tableDelimiter + "supplier " +
                              "(nzp_supp, name_supp, adres_supp,  kod_supp) " +
                              " values (" + strNzp_supp + " ,'" + finder.area.ToUpper() + "', " +
                              " (select max(inn) from " + Points.Pref + DBManager.sUploadAliasRest + "file_area " +
                              " where  upper(area) = '" + finder.area.ToUpper() +
                              "' and nzp_file in (select l.nzp_file from " + Points.Pref + DBManager.sUploadAliasRest +
                              "files_selected l, " +
                              Points.Pref + DBManager.sUploadAliasRest +
                              "files_imported fi where fi.nzp_file=l.nzp_file and trim(fi.pref)='" + finder.bank +
                              "')), " +
                              " (select max(id) from " + Points.Pref + DBManager.sUploadAliasRest + "file_area " +
                              " where  upper(area) = '" + finder.area.ToUpper() +
                              "' and nzp_file in (select l.nzp_file from " + Points.Pref + DBManager.sUploadAliasRest +
                              "files_selected l, " +
                              Points.Pref + DBManager.sUploadAliasRest +
                              "files_imported fi where fi.nzp_file=l.nzp_file and trim(fi.pref)='" + finder.bank +
                              "')))";
                        
                        ClassDBUtils.ExecSQL(sql, con_db, tr_id, ClassDBUtils.ExecMode.Exception);
#if PG
                        sql = "SELECT currval('" + seq + "')";
#else
                        sql = "SELECT " + seq + ".CURRVAL from  " + Points.Pref + "_data" + tableDelimiter + "dual";
#endif
                        nzp_supp = Convert.ToDecimal(ExecScalar(con_db, tr_id, sql, out ret1, true));

                        sql = "insert into " + Points.Pref + "_kernel" + tableDelimiter +
                              "supplier ( nzp_supp, name_supp, adres_supp,  kod_supp ) " +
                              " select nzp_supp, name_supp, adres_supp,  kod_supp from " + finder.bank + "_kernel" +
                              tableDelimiter + "supplier where nzp_supp = " + nzp_supp;


                        ClassDBUtils.ExecSQL(sql, con_db, tr_id, ClassDBUtils.ExecMode.Exception);
                    }

                    #endregion

                    #endregion


                    #region добавляем в s_area

                    //if (finder.nzp_area == "" || finder.nzp_area == null || finder.nzp_area == "0")
                    //{
                    seq = Points.Pref + "_data" + tableDelimiter + "s_area_nzp_area_seq";
                    string strNzp_area;
#if PG
                    strNzp_area = " nextval('" + seq + "') ";
#else
                    strNzp_area = seq + ".nextval ";
#endif


                    sql = " insert into " + finder.bank + "_data" + tableDelimiter +
                          "s_area ( nzp_area, area, nzp_supp ) values (" + strNzp_area + " ,'" + finder.area.ToUpper() +
                          "', " + nzp_supp + " )";
                    ClassDBUtils.ExecSQL(sql, con_db, tr_id, ClassDBUtils.ExecMode.Exception);

#if PG
                    sql = "SELECT currval('" + seq + "')";
#else
                    sql = "SELECT " + seq + ".CURRVAL from  " + Points.Pref + "_data" + tableDelimiter + "dual";
#endif

                    nzp_area = Convert.ToDecimal(ExecScalar(con_db, tr_id, sql, out ret1, true));

                    sql = " update " + Points.Pref + DBManager.sUploadAliasRest + "file_area set nzp_area = '" +
                          nzp_area +
                          "' where upper(area) = '" + finder.area.ToUpper() + "' and " +
                          " nzp_area is null and nzp_file in (select l.nzp_file from " + Points.Pref +
                          DBManager.sUploadAliasRest + "files_selected l, " +
                          Points.Pref + DBManager.sUploadAliasRest +
                          "files_imported fi where fi.nzp_file=l.nzp_file and trim(fi.pref)='" + finder.bank + "')";
                    ClassDBUtils.ExecSQL(sql, con_db, tr_id, ClassDBUtils.ExecMode.Exception);

                    sql = "insert into " + Points.Pref + "_data" + tableDelimiter +
                          "s_area ( nzp_area, area, nzp_supp ) " +
                          " select nzp_area, area, nzp_supp from " + finder.bank + "_data" + tableDelimiter +
                          "s_area where nzp_area = " + nzp_area;
                    ClassDBUtils.ExecSQL(sql, con_db, tr_id, ClassDBUtils.ExecMode.Exception);

                    #endregion



                    #region добавляем в s_payer

                    sql = "select nzp_supp from " + Points.Pref + "_kernel" + tableDelimiter + "s_payer " +
                          " where trim(upper(payer)) = '" + finder.area.ToUpper().Trim() + "'";

                    nzp_s = ExecScalar(con_db, tr_id, sql, out ret1, true);
                    if (Convert.ToDecimal(nzp_s) != 0)
                    {
                        nzp_supp_payer = Convert.ToDecimal(nzp_s);

                        ////если есть уже с таким наименованием, но с другим nzp_supp, то меняем nzp_supp
                        //if (nzp_supp_payer != nzp_supp)
                        //{
                        //    sql = "update " + finder.bank + "_kernel" + tableDelimiter + "s_payer" + 
                        //        " set nzp_supp =" + nzp_supp + " where nzp_supp =" + nzp_supp_payer;
                        //    IfxCommand = DBManager.newDbCommand(sql, con_db, tr_id);
                        //    IfxCommand.ExecuteNonQuery();

                        //    sql = "update " + Points.Pref + "_kernel" + tableDelimiter + "s_payer" +
                        //        " set nzp_supp =" + nzp_supp + " where nzp_supp =" + nzp_supp_payer;
                        //    IfxCommand = DBManager.newDbCommand(sql, con_db, tr_id);
                        //    IfxCommand.ExecuteNonQuery();
                        //}
                    }
                    else
                    {

                        seq = Points.Pref + "_kernel" + tableDelimiter + "s_payer_nzp_payer_seq";
                        string strNzp_payer;

#if PG
                        strNzp_payer = " nextval('" + seq + "') ";
#else
                        strNzp_payer = seq + ".nextval ";
#endif
                        sql = "insert into " + Points.Pref + "_kernel" + tableDelimiter +
                              "s_payer (nzp_payer, payer, npayer,  nzp_supp, inn, kpp,  bik, ks) " +
                              " select " + strNzp_payer + ", '" + finder.area.ToUpper() + "', '" + finder.area.ToUpper() +
                              "', " + nzp_supp +
                              ", inn, kpp, bik, ks  from " + Points.Pref + "_data" + tableDelimiter +
                              "file_area where nzp_area = " + nzp_area;
                        ClassDBUtils.ExecSQL(sql, con_db, tr_id, ClassDBUtils.ExecMode.Exception);

                        //в нижнем банке такой таблички нет
                        //#if PG
                        //            sql = "SELECT currval('" +seq+ "')";
                        //#else
                        //                            sql = "SELECT " + seq + ".CURRVAL from  " + Points.Pref + "_data" + tableDelimiter + "dual";
                        //#endif

                        //                            nzp_payer = Convert.ToDecimal(ExecScalar(con_db, tr_id, sql, out ret1, true));


                        //sql = "insert into " + finder.bank + "_kernel" + tableDelimiter + "s_payer (nzp_payer, payer, npayer,  nzp_supp, is_erc, inn, kpp,  bik, ks) " +
                        //        " select nzp_payer, payer, npayer,  nzp_supp, is_erc, inn, kpp,  bik, ks from " + finder.bank + "_kernel" + tableDelimiter + "s_payer where nzp_payer = " + nzp_payer;

                        //IfxCommand = DBManager.newDbCommand(sql, con_db, tr_id);
                        //IfxCommand.ExecuteNonQuery();
                    }

                    #endregion

                    //}
                    //else
                    //{
                    //    sql = " select count(*) from " + finder.bank + "_data" + tableDelimiter + "s_area where nzp_area = '" + finder.nzp_area + "'";
                    //    IfxCommand = DBManager.newDbCommand(sql, con_db, tr_id);
                    //    if (Convert.ToInt32(IfxCommand.ExecuteScalar()) != 0)
                    //    {
                    //        tr_id.Rollback();
                    //        ret.text = "Участок с таким идентификационным номером уже добавлен в базу";
                    //        ret.result = false;
                    //        return ret;
                    //    }

                    //    sql = " insert into " + finder.bank + "_data" + tableDelimiter + "s_area ( nzp_area, area ) values ( '" + finder.nzp_area + "', '" + finder.area.ToUpper() + "' )";

                    //    IfxCommand = DBManager.newDbCommand(sql, con_db, tr_id);
                    //    IfxCommand.ExecuteNonQuery();
                    //}

                    tr_id.Commit();
                }
                catch (Exception ex)
                {
                    tr_id.Rollback();
                    MonitorLog.WriteLog("Ошибка выполнения процедуры AddNewArea : " + ex.Message,
                        MonitorLog.typelog.Error, true);
                    ret.text = "Ошибка выполнения";
                    ret.result = false;
                    return ret;
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры AddNewArea : " + ex.Message, MonitorLog.typelog.Error,
                    true);
                ret.text = "Ошибка выполнения";
                ret.result = false;
                return ret;
            }
            //}

            #endregion


            ret.result = true;
            ret.text = "Выполнено.";
            ret.tag = -1;

            return ret;
        }

        /// <summary>
        /// добавление нового поставщика в базу
        /// </summary>
        /// <returns>результат</returns>
        public ReturnsType AddNewSupp(UncomparedSupps finder)
        {
            ReturnsType ret = new ReturnsType();

            #region Запись в БД

            // using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            //{
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
                IDbTransaction tr_id = con_db.BeginTransaction();
                try
                {
                    string sql = "";
                    IDbCommand IfxCommand;

                    sql = " select count(*) from " + finder.bank + "_kernel" + tableDelimiter +
                          "supplier where UPPER(name_supp) = '" + finder.supp.ToUpper() + "'";
                    IfxCommand = DBManager.newDbCommand(sql, con_db, tr_id);
                    if (Convert.ToInt32(IfxCommand.ExecuteScalar()) != 0)
                    {
                        tr_id.Rollback();
                        ret.text = "Поставщик с таким наименованием уже добавлен в базу";
                        ret.result = false;
                        return ret;
                    }

                    decimal nzp_supp = 0;
                    //if (finder.nzp_supp == "" || finder.nzp_supp == null)
                    //{
                    Returns ret1 = new Returns();

                    string seq = Points.Pref + "_kernel" + tableDelimiter + "supplier_nzp_supp_seq";
                    string strNzp_supp;
#if PG
                    strNzp_supp = " nextval('" + seq + "') ";
#else
                    strNzp_supp = seq + ".nextval ";
#endif

                    sql = " insert into " + finder.bank + "_kernel" + tableDelimiter +
                          "supplier (nzp_supp, name_supp ) values (" + strNzp_supp + " ,'" + finder.supp.ToUpper() +
                          "' )";

                    ClassDBUtils.ExecSQL(sql, con_db, tr_id, ClassDBUtils.ExecMode.Exception);

#if PG
                    sql = "SELECT currval('" + seq + "')";
#else
                    sql = "SELECT " + seq + ".CURRVAL from  " + Points.Pref + "_data" + tableDelimiter + "dual";
#endif

                    nzp_supp = Convert.ToDecimal(ExecScalar(con_db, tr_id, sql, out ret1, true));

                    sql = " update " + Points.Pref + DBManager.sUploadAliasRest + "file_supp set nzp_supp = '" +
                          nzp_supp +
                          "'" +
                          " where upper(supp_name) = '" + finder.supp.ToUpper() + "' and " +
                          " nzp_supp is null and nzp_file in (select l.nzp_file from " + Points.Pref +
                          DBManager.sUploadAliasRest + "files_selected l, " +
                          Points.Pref + DBManager.sUploadAliasRest +
                          "files_imported fi where fi.nzp_file=l.nzp_file and trim(fi.pref)='" + finder.bank + "')";
                    ClassDBUtils.ExecSQL(sql, con_db, tr_id, ClassDBUtils.ExecMode.Exception);


                    sql = "insert into  " + Points.Pref + "_kernel" + tableDelimiter +
                          "supplier (nzp_supp, name_supp ) " +
                          " select nzp_supp, name_supp from " + finder.bank + "_kernel" + tableDelimiter +
                          "supplier where nzp_supp = " + nzp_supp;
                    ClassDBUtils.ExecSQL(sql, con_db, tr_id, ClassDBUtils.ExecMode.Exception);

                    //}
                    //else
                    //{
                    //    sql = " select count(*) from " + finder.bank + "_kernel" + tableDelimiter + "supplier where UPPER(nzp_supp) = '" + finder.nzp_supp + "'";
                    //    IfxCommand = DBManager.newDbCommand(sql, con_db, tr_id);
                    //    if (Convert.ToInt32(IfxCommand.ExecuteScalar()) != 0)
                    //    {
                    //        tr_id.Rollback();
                    //        ret.text = "Поставщик с таким идентификационным номером уже добавлен в базу";
                    //        ret.result = false;
                    //        return ret;
                    //    }
                    //    sql = " insert into " + finder.bank + "_kernel" + tableDelimiter + "supplier ( nzp_supp, name_supp ) values ( '" + finder.nzp_supp + "', '" + finder.supp.ToUpper() + "' )";
                    //    IfxCommand = DBManager.newDbCommand(sql, con_db, tr_id);
                    //    IfxCommand.ExecuteNonQuery();
                    //}

                    tr_id.Commit();
                }
                catch (Exception ex)
                {
                    tr_id.Rollback();
                    MonitorLog.WriteLog("Ошибка выполнения процедуры AddNewSupp : " + ex.Message,
                        MonitorLog.typelog.Error, true);
                    ret.text = "Ошибка выполнения";
                    ret.result = false;
                    return ret;
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры AddNewSupp : " + ex.Message, MonitorLog.typelog.Error,
                    true);
                ret.text = "Ошибка выполнения";
                ret.result = false;
                return ret;
            }
            //}

            #endregion


            ret.result = true;
            ret.text = "Выполнено.";
            ret.tag = -1;

            return ret;
        }


        /// <summary>
        /// добавление нового МО в базу
        /// </summary>
        /// <returns>результат</returns>
        public ReturnsType AddNewMO(UncomparedVills finder)
        {
            ReturnsType ret = new ReturnsType();

            #region Запись в БД

            // using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            //{
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
                IDbTransaction tr_id = con_db.BeginTransaction();
                try
                {
                    string sql = "";
                    IDbCommand IfxCommand;
                    sql = " select count(*) from " + finder.bank + "_kernel" + tableDelimiter +
                          "s_vill where UPPER(vill) = '" + finder.vill.ToUpper() + "'";
                    IfxCommand = DBManager.newDbCommand(sql, con_db, tr_id);
                    if (Convert.ToInt32(IfxCommand.ExecuteScalar()) != 0)
                    {
                        tr_id.Rollback();
                        ret.text = "МО с таким наименованием уже добавлен в базу";
                        ret.result = false;
                        return ret;
                    }

                    //decimal nzp_vill = 0;
                    if (finder.nzp_vill == "" || finder.nzp_vill == null)
                    {
                        sql = " insert into " + finder.bank + "_kernel" + tableDelimiter +
                              "s_vill ( vill, nzp_user, dat_when, nzp_vill ) values " +
                              " ( '" + finder.vill.ToUpper() + "', '" + finder.nzp_user + "'," +
#if PG
 " current_date, coalesce((select max(nzp_vill) " +
#else
                            " today, nvl((select max(nzp_vill) " +
#endif
                            "from " + finder.bank + "_kernel" + tableDelimiter + "s_vill),0) + 1 )";
                        ClassDBUtils.ExecSQL(sql, con_db, tr_id);
                        sql = " update " + Points.Pref + DBManager.sUploadAliasRest +
#if PG
 "file_mo set nzp_vill =  coalesce((select max(nzp_vill) " +
#else
                            "file_mo set nzp_vill =  nvl((select max(nzp_vill) " +
#endif
                            "from " + finder.bank + "_kernel" + tableDelimiter + "s_vill),0) " +
                              " where upper(vill) = '" + finder.vill.ToUpper() + "' and " +
                              " nzp_vill is null and nzp_file in (select l.nzp_file from " + Points.Pref +
                              DBManager.sUploadAliasRest + "files_selected l, " +
                              Points.Pref + DBManager.sUploadAliasRest +
                              "files_imported fi where fi.nzp_file=l.nzp_file and trim(fi.pref)='" + finder.bank + "')";
                        ClassDBUtils.ExecSQL(sql, con_db, tr_id, ClassDBUtils.ExecMode.Exception);
                    }
                    else
                    {
                        sql = " select count(*) from " + finder.bank + "_kernel" + tableDelimiter +
                              "s_vill where UPPER(nzp_vill) = '" + finder.nzp_vill + "'";
                        IfxCommand = DBManager.newDbCommand(sql, con_db, tr_id);
                        if (Convert.ToInt32(IfxCommand.ExecuteScalar()) != 0)
                        {
                            tr_id.Rollback();
                            ret.text = "МО с таким идентификационным номером уже добавлен в базу";
                            ret.result = false;
                            return ret;
                        }
                        sql = " insert into " + finder.bank + "_kernel" + tableDelimiter +
                              "s_vill ( nzp_vill, vill, nzp_user, dat_when ) values " +
                              " ( '" + finder.nzp_vill + "', '" + finder.vill.ToUpper() + "', '" + finder.nzp_user +
#if PG
 "', current_day )" +
#else
                            "', today )" +
#endif
                            "";

                        ClassDBUtils.ExecSQL(sql, con_db, tr_id, ClassDBUtils.ExecMode.Exception);
                    }

                    tr_id.Commit();
                }
                catch (Exception ex)
                {
                    tr_id.Rollback();
                    MonitorLog.WriteLog("Ошибка выполнения процедуры AddNewMO : " + ex.Message, MonitorLog.typelog.Error,
                        true);
                    ret.text = "Ошибка выполнения";
                    ret.result = false;
                    return ret;
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры AddNewMO : " + ex.Message, MonitorLog.typelog.Error,
                    true);
                ret.text = "Ошибка выполнения";
                ret.result = false;
                return ret;
            }
            //}

            #endregion


            ret.result = true;
            ret.text = "Выполнено.";
            ret.tag = -1;

            return ret;
        }

        /// <summary>
        /// добавление новой услуги в базу
        /// </summary>
        /// <returns>результат</returns>
        public ReturnsType AddNewServ(UncomparedServs finder)
        {
            ReturnsType ret = new ReturnsType();

            #region Запись в БД

            IDbTransaction tr_id = null;
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


                try
                {
                    string sql = "";
                    IDbCommand IfxCommand;
                    sql = " select count(*) from " + finder.bank + "_kernel" + tableDelimiter +
                          "services where trim(UPPER(service)) = '" + finder.serv.ToUpper().Trim() + "'";
                    IfxCommand = DBManager.newDbCommand(sql, con_db, null);
                    if (Convert.ToInt32(IfxCommand.ExecuteScalar()) != 0)
                    {
                        // tr_id.Rollback();
                        ret.text = "Услуга с таким наименованием уже добавлена в базу";
                        ret.result = false;
                        return ret;
                    }

                    decimal nzp_serv = 0;
                    if (finder.nzp_serv == "" || finder.nzp_serv == null)
                    {
                        string seq = Points.Pref + "_kernel" + tableDelimiter + "services_nzp_serv_seq";
                        Returns ret1 = new Returns();

                        #region создание и настройка sequence

                        try
                        {
                            sql = "create sequence " + seq;
                            IfxCommand = DBManager.newDbCommand(sql, con_db, null);
                            IfxCommand.ExecuteNonQuery();

                            sql = "select max(nzp_serv) from " + Points.Pref + "_kernel" + tableDelimiter + "services";
                            int maxNzpV = Convert.ToInt32(ExecScalar(con_db, sql, out ret1, true));
                            sql = "select max(nzp_serv) from " + finder.bank + "_kernel" + tableDelimiter + "services";
                            int maxNzpN = Convert.ToInt32(ExecScalar(con_db, sql, out ret1, true));

                            int maxNzp = Math.Max(maxNzpN, maxNzpV);
                            maxNzp = Math.Max(maxNzp, 1010000);

                            sql = " alter sequence " + seq +
                                  " restart " + maxNzp;
                            IfxCommand = DBManager.newDbCommand(sql, con_db, null);
                            IfxCommand.ExecuteNonQuery();
                        }
                        catch
                        {
                        }

                        tr_id = con_db.BeginTransaction();

                        #endregion

                        string strNzp_serv;
#if PG
                        strNzp_serv = " nextval('" + seq + "') ";
#else
                        strNzp_serv = seq + ".nextval ";
#endif

                        sql = " insert into " + Points.Pref + "_kernel" + tableDelimiter +
                              "services (nzp_serv, service, service_small, service_name ) values " +
                              " (" + strNzp_serv + ", '" + finder.serv.ToUpper() + "', '" + finder.serv.ToUpper() +
                              "', '" + finder.serv.ToUpper() + "')";

                        ClassDBUtils.ExecSQL(sql, con_db, tr_id, ClassDBUtils.ExecMode.Exception);

#if PG
                        sql = "SELECT currval('" + seq + "')";
#else
                        sql = "SELECT " + seq + ".CURRVAL from  " + Points.Pref + "_data" + tableDelimiter + "dual";
#endif

                        nzp_serv = Convert.ToDecimal(ExecScalar(con_db, tr_id, sql, out ret1, true));


                        sql = " insert into " + finder.bank + "_kernel" + tableDelimiter +
                             "services (nzp_serv, service, service_small, service_name ) values " +
                             " (" + nzp_serv + ", '" + finder.serv.ToUpper() + "', '" + finder.serv.ToUpper() + "', '" +
                             finder.serv.ToUpper() + "')";
                        ClassDBUtils.ExecSQL(sql, con_db, tr_id, ClassDBUtils.ExecMode.Exception);

                        sql = " update " + Points.Pref + DBManager.sUploadAliasRest + "file_services set nzp_serv = '" +
                              nzp_serv + "' where trim(upper(service2)) = '" + finder.serv.ToUpper().Trim() + "' and " +
                              " nzp_serv is null and nzp_file in (select l.nzp_file from " + Points.Pref +
                              DBManager.sUploadAliasRest + "files_selected l, " +
                              Points.Pref + DBManager.sUploadAliasRest +
                              "files_imported fi where fi.nzp_file=l.nzp_file and trim(fi.pref)='" + finder.bank + "')";
                        ClassDBUtils.ExecSQL(sql, con_db, tr_id, ClassDBUtils.ExecMode.Exception);

                       
                    }
                    else
                    {
                        //sql = " select count(*) from " + finder.bank + "_kernel" + tableDelimiter + "services where nzp_serv = '" + finder.nzp_serv + "'";
                        //IfxCommand = DBManager.newDbCommand(sql, con_db, tr_id);
                        //if (Convert.ToInt32(IfxCommand.ExecuteScalar()) != 0)
                        //{
                        tr_id.Rollback();
                        ret.text = "Услуга с таким идентификационным номером уже добавлена в базу";
                        ret.result = false;
                        return ret;
                        //}
                        //sql = " insert into " + finder.bank + "_kernel" + tableDelimiter + "services ( nzp_serv, service, service_small, service_name ) values " +
                        //      " ( '" + finder.nzp_serv + "', '" + finder.serv.ToUpper() + "', '" + finder.serv.ToUpper() + "','" + finder.serv.ToUpper() + "' )";
                        //IfxCommand = DBManager.newDbCommand(sql, con_db, tr_id);
                        //IfxCommand.ExecuteNonQuery();
                    }

                    tr_id.Commit();
                }
                catch (Exception ex)
                {
                    tr_id.Rollback();
                    MonitorLog.WriteLog("Ошибка выполнения процедуры AddNewServ : " + ex.Message,
                        MonitorLog.typelog.Error, true);
                    ret.text = "Ошибка выполнения";
                    ret.result = false;
                    return ret;
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры AddNewServ : " + ex.Message, MonitorLog.typelog.Error,
                    true);
                ret.text = "Ошибка выполнения";
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
        /// добавление нового типа параметра
        /// </summary>
        /// <returns>результат</returns>
        public ReturnsType AddNewParType(UncomparedParTypes finder)
        {
            ReturnsType ret = new ReturnsType();

            #region Запись в БД

            // using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            //{
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
                IDbTransaction tr_id = con_db.BeginTransaction();
                try
                {
                    string sql = "";
                    IDbCommand IfxCommand;
                    sql = " select count(*) from " + finder.bank + "_kernel" + tableDelimiter +
                          "prm_name where UPPER(name_prm) = '" + finder.name_prm.ToUpper() + "'";
                    IfxCommand = DBManager.newDbCommand(sql, con_db, tr_id);
                    if (Convert.ToInt32(IfxCommand.ExecuteScalar()) != 0)
                    {
                        tr_id.Rollback();
                        ret.text = "Параметр с таким наименованием уже добавлен в базу";
                        ret.result = false;
                        return ret;
                    }

                    Returns ret1 = new Returns();
                    sql = "select max(nzp_prm) from " + Points.Pref + "_kernel" + tableDelimiter + "prm_name";
                    var nzp_prm = ExecScalar(con_db, tr_id, sql, out ret1, true);

                    if (Convert.ToDecimal(nzp_prm) < 1000000)
                    {
                        sql =
                            " insert into " + Points.Pref + "_kernel" + tableDelimiter +
                            "prm_name ( nzp_prm, name_prm ) " +
                            "values ( '1000000', 'Локальные значения nzp_prm' )";
                    }

                    if (finder.nzp_prm == "" || finder.nzp_prm == null)
                    {
                        string seq = Points.Pref + "_kernel" + tableDelimiter + "prm_name_nzp_prm_seq";

                        #region создание и настройка sequence - в случае ошибки обрывает транзакцию, так что закомментировала

                        //try
                        //{
                        //    sql = "create sequence " + seq;
                        //    IfxCommand = DBManager.newDbCommand(sql, con_db, tr_id);
                        //    IfxCommand.ExecuteNonQuery();

                        //    sql = "select max(nzp_prm) from " + Points.Pref + "_kernel" + tableDelimiter + "prm_name";
                        //    int maxNzpV = Convert.ToInt32(ExecScalar(con_db, tr_id, sql, out ret1, true));
                        //    sql = "select max(nzp_prm) from " + finder.bank + "_kernel" + tableDelimiter + "prm_name";
                        //    int maxNzpN = Convert.ToInt32(ExecScalar(con_db, tr_id, sql, out ret1, true));

                        //    int maxNzp = Math.Max(maxNzpN, maxNzpV);
                        //    maxNzp = Math.Max(maxNzp, 1010000);

                        //    sql = " alter sequence " + seq +
                        //          " restart " + maxNzp;
                        //    IfxCommand = DBManager.newDbCommand(sql, con_db, tr_id);
                        //    IfxCommand.ExecuteNonQuery();
                        //}
                        //catch
                        //{
                        //}

                        #endregion

                        string strNzp_prm;
#if PG
                        strNzp_prm = " nextval('" + seq + "') ";
#else
                        strNzp_prm = seq + ".nextval ";
#endif


                        sql = " insert into " + Points.Pref + "_kernel" + tableDelimiter +
                              "prm_name (nzp_prm, name_prm ) values " +
                              " ( " + strNzp_prm + ",'" + finder.name_prm.ToUpper() + "' )";
                        ClassDBUtils.ExecSQL(sql, con_db, tr_id, ClassDBUtils.ExecMode.Exception);

#if PG
                        sql = "SELECT currval('" + seq + "')";
#else
                        sql = "SELECT " + seq + ".CURRVAL from  " + Points.Pref + "_data" + tableDelimiter + "dual";
#endif
                        nzp_prm = Convert.ToDecimal(ExecScalar(con_db, tr_id, sql, out ret1, true));


                        sql = " insert into " + finder.bank + "_kernel" + tableDelimiter +
                              "prm_name ( name_prm, nzp_prm ) values " +
                              " ( '" + finder.name_prm.ToUpper() + "', " + nzp_prm + " )";
                        ClassDBUtils.ExecSQL(sql, con_db, tr_id, ClassDBUtils.ExecMode.Exception);

                        sql = " update " + Points.Pref + DBManager.sUploadAliasRest + "file_typeparams set nzp_prm = '" +
                              nzp_prm + "' where trim(upper( prm_name)) = '" + finder.name_prm.ToUpper().Trim() +
                              "' and " +
                              " nzp_prm is null and nzp_file in (select l.nzp_file from " + Points.Pref +
                              DBManager.sUploadAliasRest + "files_selected l, " +
                              Points.Pref + DBManager.sUploadAliasRest +
                              "files_imported fi where fi.nzp_file=l.nzp_file and trim(fi.pref)='" + finder.bank + "')";
                        ClassDBUtils.ExecSQL(sql, con_db, tr_id, ClassDBUtils.ExecMode.Exception);
                    }
                    else
                    {
                        //sql = " select count(*) from " + finder.bank + "_kernel" + tableDelimiter + "prm_name where nzp_prm = '" + finder.nzp_prm + "'";
                        //IfxCommand = DBManager.newDbCommand(sql, con_db, tr_id);
                        //if (Convert.ToInt32(IfxCommand.ExecuteScalar()) != 0)
                        //{
                        tr_id.Rollback();
                        ret.text = "Параметр с таким идентификационным номером уже добавлен в базу";
                        ret.result = false;
                        return ret;
                        //}
                        //sql = " insert into " + finder.bank + "_kernel" + tableDelimiter + "prm_name ( nzp_prm, name_prm ) values " +
                        //      " ( '" + finder.nzp_prm + "', '" + finder.name_prm.ToUpper() + "' )";
                        //IfxCommand = DBManager.newDbCommand(sql, con_db, tr_id);
                        //IfxCommand.ExecuteNonQuery();
                    }

                    tr_id.Commit();
                }
                catch (Exception ex)
                {
                    tr_id.Rollback();
                    MonitorLog.WriteLog("Ошибка выполнения процедуры AddNewParType : " + ex.Message,
                        MonitorLog.typelog.Error, true);
                    ret.text = "Ошибка выполнения";
                    ret.result = false;
                    return ret;
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры AddNewParType : " + ex.Message,
                    MonitorLog.typelog.Error, true);
                ret.text = "Ошибка выполнения";
                ret.result = false;
                return ret;
            }
            //}

            #endregion

            ret.result = true;
            ret.text = "Выполнено.";
            ret.tag = -1;

            return ret;
        }

        /// <summary>
        /// добавление нового типа благоустройства в базу
        /// </summary>
        /// <returns>результат</returns>
        public ReturnsType AddNewParBlag(UncomparedParTypes finder)
        {
            ReturnsType ret = new ReturnsType();

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
                IDbTransaction tr_id = con_db.BeginTransaction();
                try
                {
                    string sql = "";
                    IDbCommand IfxCommand;
                    sql = " select count(*) from " + finder.bank + "_kernel" + tableDelimiter +
                          "prm_name where UPPER(name_prm) = '" + finder.name_prm.ToUpper() + "'";
                    IfxCommand = DBManager.newDbCommand(sql, con_db, tr_id);
                    if (Convert.ToInt32(IfxCommand.ExecuteScalar()) != 0)
                    {
                        tr_id.Rollback();
                        ret.text = "Параметр с таким наименованием уже добавлен в базу";
                        ret.result = false;
                        return ret;
                    }

                    decimal nzp_prm = 0;
                    if (finder.nzp_prm == "" || finder.nzp_prm == null)
                    {
                        string seq = Points.Pref + "_kernel" + tableDelimiter + "prm_name_nzp_prm_seq";
                        Returns ret1 = new Returns();

                        #region создание и настройка sequence  - в случае ошибки обрывает транзакцию, так что закомментировала

                        //try
                        //{
                        //    sql = "create sequence " + seq;
                        //    if (ExecSQL(con_db, tr_id, sql, false).result)
                        //    {
                        //        sql = "select max(nzp_prm) from " + Points.Pref + "_kernel" + tableDelimiter +
                        //              "prm_name";
                        //        int maxNzpV = Convert.ToInt32(ExecScalar(con_db, tr_id, sql, out ret1, true));
                        //        sql = "select max(nzp_prm) from " + finder.bank + "_kernel" + tableDelimiter +
                        //              "prm_name";
                        //        int maxNzpN = Convert.ToInt32(ExecScalar(con_db, tr_id, sql, out ret1, true));

                        //        int maxNzp = Math.Max(maxNzpN, maxNzpV);
                        //        maxNzp = Math.Max(maxNzp, 1010000);

                        //        sql = " alter sequence " + seq +
                        //              " restart " + maxNzp;
                        //        IfxCommand = DBManager.newDbCommand(sql, con_db, tr_id);
                        //        IfxCommand.ExecuteNonQuery();
                        //    }
                        //}
                        //catch
                        //{
                        //}
                        #endregion

                        string strNzp_prm;
#if PG
                        strNzp_prm = " nextval('" + seq + "') ";
#else
                        strNzp_prm = seq + ".nextval ";
#endif


                        sql = " insert into " + Points.Pref + "_kernel" + tableDelimiter +
                              "prm_name ( nzp_prm, name_prm ) values " +
                              " ( " + strNzp_prm + ",'" + finder.name_prm.ToUpper() + "' )";
                        ClassDBUtils.ExecSQL(sql, con_db, tr_id, ClassDBUtils.ExecMode.Exception);

#if PG
                        sql = "SELECT currval('" + seq + "')";
#else
                        sql = "SELECT " + seq + ".CURRVAL from  " + Points.Pref + "_data" + tableDelimiter + "dual";
#endif
                        nzp_prm = Convert.ToDecimal(ExecScalar(con_db, tr_id, sql, out ret1, true));

                        sql = " insert into " + finder.bank + "_kernel" + tableDelimiter +
                              "prm_name ( name_prm, nzp_prm ) values " +
                              " ( '" + finder.name_prm.ToUpper() + "', " + nzp_prm + " )";
                        ClassDBUtils.ExecSQL(sql, con_db, tr_id, ClassDBUtils.ExecMode.Exception);

                        sql = " update " + Points.Pref + DBManager.sUploadAliasRest + "file_blag set nzp_prm = '" +
                              nzp_prm + "' where upper(name) = '" + finder.name_prm.ToUpper() + "' and " +
                              " nzp_prm is null and nzp_file in (select l.nzp_file from " + Points.Pref +
                              DBManager.sUploadAliasRest + "files_selected l, " +
                              Points.Pref + DBManager.sUploadAliasRest +
                              "files_imported fi where fi.nzp_file=l.nzp_file and trim(fi.pref)='" + finder.bank + "')";
                        ClassDBUtils.ExecSQL(sql, con_db, tr_id, ClassDBUtils.ExecMode.Exception);
                    }
                    else
                    {
                        sql = " select count(*) from " + finder.bank + "_kernel" + tableDelimiter +
                              "prm_name where nzp_prm = '" + finder.nzp_prm + "'";
                        IfxCommand = DBManager.newDbCommand(sql, con_db, tr_id);
                        if (Convert.ToInt32(IfxCommand.ExecuteScalar()) != 0)
                        {
                            tr_id.Rollback();
                            ret.text = "Параметр с таким идентификационным номером уже добавлен в базу";
                            ret.result = false;
                            return ret;
                        }
                        sql = " insert into " + finder.bank + "_kernel" + tableDelimiter +
                              "prm_name ( nzp_prm, name_prm ) values " +
                              " ( '" + finder.nzp_prm + "', '" + finder.name_prm.ToUpper() + "' )";
                        ClassDBUtils.ExecSQL(sql, con_db, tr_id, ClassDBUtils.ExecMode.Exception);
                    }

                    tr_id.Commit();
                }
                catch (Exception ex)
                {
                    tr_id.Rollback();
                    MonitorLog.WriteLog("Ошибка выполнения процедуры AddNewParBlag : " + ex.Message,
                        MonitorLog.typelog.Error, true);
                    ret.text = "Ошибка выполнения";
                    ret.result = false;
                    return ret;
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры AddNewParBlag : " + ex.Message,
                    MonitorLog.typelog.Error, true);
                ret.text = "Ошибка выполнения";
                ret.result = false;
                return ret;
            }
            

            #endregion

            ret.result = true;
            ret.text = "Выполнено.";
            ret.tag = -1;

            return ret;
        }

        /// <summary>
        /// добавление нового типа параметров по газу в базу
        /// </summary>
        /// <returns>результат</returns>
        public ReturnsType AddNewParGas(UncomparedParTypes finder)
        {
            ReturnsType ret = new ReturnsType();

            #region Запись в БД

            // using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            //{
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
                IDbTransaction tr_id = con_db.BeginTransaction();
                try
                {
                    string sql = "";
                    IDbCommand IfxCommand;
                    sql = " select count(*) from " + finder.bank + "_kernel" + tableDelimiter +
                          "prm_name where UPPER(name_prm) = '" + finder.name_prm.ToUpper() + "'";
                    IfxCommand = DBManager.newDbCommand(sql, con_db, tr_id);
                    if (Convert.ToInt32(IfxCommand.ExecuteScalar()) != 0)
                    {
                        tr_id.Rollback();
                        ret.text = "Параметр с таким наименованием уже добавлен в базу";
                        ret.result = false;
                        return ret;
                    }

                    decimal nzp_prm = 0;
                    if (finder.nzp_prm == "" || finder.nzp_prm == null)
                    {
                        string seq = Points.Pref + "_kernel" + tableDelimiter + "prm_name_nzp_prm_seq";
                        Returns ret1 = new Returns();

                        #region создание и настройка sequence  - в случае ошибки обрывает транзакцию, так что закомментировала

                        //try
                        //{
                        //    sql = "create sequence " + seq;
                        //    ExecSQL(con_db, sql);

                        //    sql = "select max(nzp_prm) from " + Points.Pref + "_kernel" + tableDelimiter + "prm_name";
                        //    int maxNzpV = Convert.ToInt32(ExecScalar(con_db, tr_id, sql, out ret1, true));
                        //    sql = "select max(nzp_prm) from " + finder.bank + "_kernel" + tableDelimiter + "prm_name";
                        //    int maxNzpN = Convert.ToInt32(ExecScalar(con_db, tr_id, sql, out ret1, true));

                        //    int maxNzp = Math.Max(maxNzpN, maxNzpV);
                        //    maxNzp = Math.Max(maxNzp, 1010000);

                        //    sql = " alter sequence " + seq +
                        //          " restart " + maxNzp;
                        //    IfxCommand = DBManager.newDbCommand(sql, con_db, tr_id);
                        //    IfxCommand.ExecuteNonQuery();
                        //}
                        //catch
                        //{
                        //}

                        #endregion

                        string strNzp_prm;
#if PG
                        strNzp_prm = " nextval('" + seq + "') ";
#else
                        strNzp_prm = seq + ".nextval ";
#endif


                        sql = " insert into " + Points.Pref + "_kernel" + tableDelimiter +
                              "prm_name ( nzp_prm, name_prm ) values " +
                              " ( " + strNzp_prm + ",'" + finder.name_prm.ToUpper() + "' )";
                        ClassDBUtils.ExecSQL(sql, con_db, tr_id, ClassDBUtils.ExecMode.Exception);

#if PG
                        sql = "SELECT currval('" + seq + "')";
#else
                        sql = "SELECT " + seq + ".CURRVAL from  " + Points.Pref + "_data" + tableDelimiter + "dual";
#endif
                        nzp_prm = Convert.ToDecimal(ExecScalar(con_db, tr_id, sql, out ret1, true));

                        sql = " insert into " + finder.bank + "_kernel" + tableDelimiter +
                              "prm_name ( name_prm, nzp_prm ) values " +
                              " ( '" + finder.name_prm.ToUpper() + "', " + nzp_prm + " )";
                        ClassDBUtils.ExecSQL(sql, con_db, tr_id, ClassDBUtils.ExecMode.Exception);

                        sql = " update " + Points.Pref + DBManager.sUploadAliasRest + "file_gaz set nzp_prm = '" +
                              nzp_prm +
                              "' where upper(name) = '" + finder.name_prm.ToUpper() + "' and " +
                              " nzp_prm is null and nzp_file in (select l.nzp_file from " + Points.Pref +
                              DBManager.sUploadAliasRest + "files_selected l, " +
                              Points.Pref + DBManager.sUploadAliasRest +
                              "files_imported fi where fi.nzp_file=l.nzp_file and trim(fi.pref)='" + finder.bank + "')";
                        ClassDBUtils.ExecSQL(sql, con_db, tr_id, ClassDBUtils.ExecMode.Exception);
                    }
                    else
                    {
                        //sql = " select count(*) from " + finder.bank + "_kernel" + tableDelimiter + "prm_name where nzp_prm = '" + finder.nzp_prm + "'";
                        //IfxCommand = DBManager.newDbCommand(sql, con_db, tr_id);
                        //if (Convert.ToInt32(IfxCommand.ExecuteScalar()) != 0)
                        //{
                        tr_id.Rollback();
                        ret.text = "Параметр с таким идентификационным номером уже добавлен в базу";
                        ret.result = false;
                        return ret;
                        //}
                        //sql = " insert into " + finder.bank + "_kernel" + tableDelimiter + "prm_name ( nzp_prm, name_prm ) values " +
                        //      " ( '" + finder.nzp_prm + "', '" + finder.name_prm.ToUpper() + "' )";
                        //IfxCommand = DBManager.newDbCommand(sql, con_db, tr_id);
                        //IfxCommand.ExecuteNonQuery();
                    }

                    tr_id.Commit();
                }
                catch (Exception ex)
                {
                    tr_id.Rollback();
                    MonitorLog.WriteLog("Ошибка выполнения процедуры AddNewParGas : " + ex.Message,
                        MonitorLog.typelog.Error, true);
                    ret.text = "Ошибка выполнения";
                    ret.result = false;
                    return ret;
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры AddNewParGas : " + ex.Message, MonitorLog.typelog.Error,
                    true);
                ret.text = "Ошибка выполнения";
                ret.result = false;
                return ret;
            }
            //}

            #endregion

            ret.result = true;
            ret.text = "Выполнено.";
            ret.tag = -1;

            return ret;
        }

        /// <summary>
        /// добавление нового типа параметров по воде в базу
        /// </summary>
        /// <returns>результат</returns>
        public ReturnsType AddNewParWater(UncomparedParTypes finder)
        {
            ReturnsType ret = new ReturnsType();

            #region Запись в БД

            // using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            //{
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
                IDbTransaction tr_id = con_db.BeginTransaction();
                try
                {
                    string sql = "";
                    IDbCommand IfxCommand;
                    sql = " select count(*) from " + finder.bank + "_kernel" + tableDelimiter +
                          "prm_name where UPPER(name_prm) = '" + finder.name_prm.ToUpper() + "'";
                    IfxCommand = DBManager.newDbCommand(sql, con_db, tr_id);
                    if (Convert.ToInt32(IfxCommand.ExecuteScalar()) != 0)
                    {
                        tr_id.Rollback();
                        ret.text = "Параметр с таким наименованием уже добавлен в базу";
                        ret.result = false;
                        return ret;
                    }

                    decimal nzp_prm = 0;
                    if (finder.nzp_prm == "" || finder.nzp_prm == null)
                    {
                        string seq = Points.Pref + "_kernel" + tableDelimiter + "prm_name_nzp_prm_seq";
                        Returns ret1 = new Returns();

                        #region создание и настройка sequence  - в случае ошибки обрывает транзакцию, так что закомментировала

                        //try
                        //{
                        //    sql = "create sequence " + seq;
                        //    ret1 = ExecSQL(con_db, tr_id, sql, false);
                        //    if (ret1.result)
                        //    {
                        //        sql = "select max(nzp_prm) from " + Points.Pref + "_kernel" + tableDelimiter +
                        //              "prm_name";
                        //        int maxNzpV = Convert.ToInt32(ExecScalar(con_db, tr_id, sql, out ret1, true));
                        //        sql = "select max(nzp_prm) from " + finder.bank + "_kernel" + tableDelimiter +
                        //              "prm_name";
                        //        int maxNzpN = Convert.ToInt32(ExecScalar(con_db, tr_id, sql, out ret1, true));

                        //        int maxNzp = Math.Max(maxNzpN, maxNzpV);
                        //        maxNzp = Math.Max(maxNzp, 1010000);

                        //        sql = " alter sequence " + seq +
                        //              " restart " + maxNzp;
                        //        IfxCommand = DBManager.newDbCommand(sql, con_db, tr_id);
                        //        IfxCommand.ExecuteNonQuery();
                        //    }
                        //}
                        //catch
                        //{
                        //}

                        #endregion

                        string strNzp_prm;
#if PG
                        strNzp_prm = " nextval('" + seq + "') ";
#else
                        strNzp_prm = seq + ".nextval ";
#endif


                        sql = " insert into " + Points.Pref + "_kernel" + tableDelimiter +
                              "prm_name ( nzp_prm, name_prm ) values " +
                              " ( " + strNzp_prm + ",'" + finder.name_prm.ToUpper() + "' )";
                        IfxCommand = DBManager.newDbCommand(sql, con_db, tr_id);
                        IfxCommand.ExecuteNonQuery();

#if PG
                        sql = "SELECT currval('" + seq + "')";
#else
                        sql = "SELECT " + seq + ".CURRVAL from  " + Points.Pref + "_data" + tableDelimiter + "dual";
#endif
                        nzp_prm = Convert.ToDecimal(ExecScalar(con_db, tr_id, sql, out ret1, true));

                        sql = " insert into " + finder.bank + "_kernel" + tableDelimiter +
                              "prm_name ( name_prm, nzp_prm ) values " +
                              " ( '" + finder.name_prm.ToUpper() + "', " + nzp_prm + " )";
                        ClassDBUtils.ExecSQL(sql, con_db, tr_id, ClassDBUtils.ExecMode.Exception);

                        sql = " update " + Points.Pref + DBManager.sUploadAliasRest + "file_voda set nzp_prm = '" +
                              nzp_prm + "' where upper(name) = '" + finder.name_prm.ToUpper() + "' and " +
                              " nzp_prm is null and nzp_file in (select l.nzp_file from " + Points.Pref +
                              DBManager.sUploadAliasRest + "files_selected l, " +
                              Points.Pref + DBManager.sUploadAliasRest +
                              "files_imported fi where fi.nzp_file=l.nzp_file and trim(fi.pref)='" + finder.bank + "')";
                        ClassDBUtils.ExecSQL(sql, con_db, tr_id, ClassDBUtils.ExecMode.Exception);
                    }
                    else
                    {
                        sql = " select count(*) from " + finder.bank + "_kernel" + tableDelimiter +
                              "prm_name where nzp_prm = '" + finder.nzp_prm + "'";
                        IfxCommand = DBManager.newDbCommand(sql, con_db, tr_id);
                        if (Convert.ToInt32(IfxCommand.ExecuteScalar()) != 0)
                        {
                            tr_id.Rollback();
                            ret.text = "Параметр с таким идентификационным номером уже добавлен в базу";
                            ret.result = false;
                            return ret;
                        }
                        sql = " insert into " + finder.bank + "_kernel" + tableDelimiter +
                              "prm_name ( nzp_prm, name_prm ) values " +
                              " ( '" + finder.nzp_prm + "', '" + finder.name_prm.ToUpper() + "' )";
                        ClassDBUtils.ExecSQL(sql, con_db, tr_id, ClassDBUtils.ExecMode.Exception);
                    }

                    tr_id.Commit();
                }
                catch (Exception ex)
                {
                    tr_id.Rollback();
                    MonitorLog.WriteLog("Ошибка выполнения процедуры AddNewParWater : " + ex.Message,
                        MonitorLog.typelog.Error, true);
                    ret.text = "Ошибка выполнения";
                    ret.result = false;
                    return ret;
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры AddNewParWater : " + ex.Message,
                    MonitorLog.typelog.Error, true);
                ret.text = "Ошибка выполнения";
                ret.result = false;
                return ret;
            }
            //}

            #endregion

            ret.result = true;
            ret.text = "Выполнено.";
            ret.tag = -1;

            return ret;
        }

        /// <summary>
        /// добавление новой единицы измерения в базу
        /// </summary>
        /// <returns>результат</returns>
        public ReturnsType AddNewMeasure(UncomparedMeasures finder)
        {
            ReturnsType ret = new ReturnsType();

            #region Запись в БД

            // using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            //{
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
                IDbTransaction tr_id = con_db.BeginTransaction();
                try
                {
                    string sql = "";
                    IDbCommand IfxCommand;
                    sql = " select count(*) from " + finder.bank + "_kernel" + tableDelimiter +
                          "s_measure where UPPER(measure_long) = '" + finder.measure.ToUpper() + "'";
                    IfxCommand = DBManager.newDbCommand(sql, con_db, tr_id);
                    if (Convert.ToInt32(IfxCommand.ExecuteScalar()) != 0)
                    {
                        tr_id.Rollback();
                        ret.text = "Единица измерения с таким наименованием уже добавлена в базу";
                        ret.result = false;
                        return ret;
                    }

                    decimal nzp_measure = 0;
                    if (finder.nzp_measure == "" || finder.nzp_measure == null)
                    {
                        sql = " insert into " + finder.bank + "_kernel" + tableDelimiter +
                              "s_measure ( measure, measure_long ) values " +
                              " ( '" + finder.measure.ToUpper() + "', '" + finder.measure.ToUpper() + "' )";
                        ClassDBUtils.ExecSQL(sql, con_db, tr_id, ClassDBUtils.ExecMode.Exception);
                        nzp_measure = ClassDBUtils.GetSerialKey(con_db, tr_id);
                        sql = " update " + Points.Pref + DBManager.sUploadAliasRest +
                              "file_measures set nzp_measure = '" +
                              nzp_measure + "' where upper(measure) = '" + finder.measure.ToUpper() + "' and " +
                              " nzp_measure is null and nzp_file in (select l.nzp_file from " + Points.Pref +
                              DBManager.sUploadAliasRest + "files_selected l, " +
                              Points.Pref + DBManager.sUploadAliasRest +
                              "files_imported fi where fi.nzp_file=l.nzp_file and trim(fi.pref)='" + finder.bank + "')";
                        ClassDBUtils.ExecSQL(sql, con_db, tr_id, ClassDBUtils.ExecMode.Exception);
                    }
                    else
                    {
                        sql = " select count(*) from " + finder.bank + "_kernel" + tableDelimiter +
                              "s_measure where nzp_measure = '" + finder.nzp_measure + "'";
                        IfxCommand = DBManager.newDbCommand(sql, con_db, tr_id);
                        if (Convert.ToInt32(IfxCommand.ExecuteScalar()) != 0)
                        {
                            tr_id.Rollback();
                            ret.text = "Единица измерения с таким идентификационным номером уже добавлена в базу";
                            ret.result = false;
                            return ret;
                        }
                        sql = " insert into " + finder.bank + "_kernel" + tableDelimiter +
                              "s_measure ( nzp_measure, measure, measure_long ) values " +
                              " ( '" + finder.nzp_measure + "', '" + finder.measure.ToUpper() + "', '" +
                              finder.measure.ToUpper() + "' )";
                        ClassDBUtils.ExecSQL(sql, con_db, tr_id, ClassDBUtils.ExecMode.Exception);
                    }

                    tr_id.Commit();
                }
                catch (Exception ex)
                {
                    tr_id.Rollback();
                    MonitorLog.WriteLog("Ошибка выполнения процедуры AddNewMeasure : " + ex.Message,
                        MonitorLog.typelog.Error, true);
                    ret.text = "Ошибка выполнения";
                    ret.result = false;
                    return ret;
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры AddNewMeasure : " + ex.Message,
                    MonitorLog.typelog.Error, true);
                ret.text = "Ошибка выполнения";
                ret.result = false;
                return ret;
            }
            //}

            #endregion


            ret.result = true;
            ret.text = "Выполнено.";
            ret.tag = -1;

            return ret;
        }

        /// <summary>
        /// добавление нового населенного пункта в базу
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsType AddNewRajon(UncomparedRajons finder)
        {
            ReturnsType ret = new ReturnsType();

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
                IDbTransaction tr_id = con_db.BeginTransaction();
                try
                {
                    string sql = "";
                    IDbCommand IfxCommand;
                    sql = " select count(*) from " + finder.bank + "_data" + tableDelimiter +
                          "s_rajon where UPPER(rajon) = '" + finder.rajon.ToUpper() + "' and nzp_town = " +
                          finder.nzp_town;
                    IfxCommand = DBManager.newDbCommand(sql, con_db, tr_id);
                    if (Convert.ToInt32(IfxCommand.ExecuteScalar()) != 0)
                    {
                        tr_id.Rollback();
                        ret.text = "Населенный пункт с таким наименованием в данном районе/городе уже добавлена в базу";
                        ret.result = false;
                        return ret;
                    }
                    sql = " select count(*) from " + Points.Pref + "_data" + tableDelimiter + "s_town where nzp_town = " +
                          finder.nzp_town;
                    IfxCommand = DBManager.newDbCommand(sql, con_db, tr_id);
                    if (Convert.ToInt32(IfxCommand.ExecuteScalar()) == 0)
                    {
                        ret.result = false;
                        ret.text = "Для данного населенного пункта не задан соответствующий населенный район/город";
                        return ret;
                    }

                    decimal nzp_raj = 0;
                    if (finder.nzp_raj == "" || finder.nzp_raj == null)
                    {
                        Returns ret1 = new Returns();

                        sql = "select " + sNvlWord + "(max(nzp_raj), 0) from " + Points.Pref + "_data" + tableDelimiter +
                              "s_rajon";
                        nzp_raj = Convert.ToDecimal(ExecScalar(con_db, tr_id, sql, out ret1, true));

                        foreach (_Point p in Points.PointList)
                        {
                            sql = "select " + sNvlWord + "(max(nzp_raj), 0) from " + p.pref + "_data" + tableDelimiter +
                                  "s_rajon";
                            decimal nzp_raj1 = Convert.ToDecimal(ExecScalar(con_db, tr_id, sql, out ret1, true));
                            if (nzp_raj1 > nzp_raj) nzp_raj = nzp_raj1;
                        }
                        nzp_raj ++;

                        sql = " insert into " + Points.Pref + "_data" + tableDelimiter +
                              "s_rajon ( nzp_raj, rajon ,nzp_town ) values ( " + nzp_raj + ",'" +
                              finder.rajon.ToUpper() + "', '" + finder.nzp_town + "' )";
                        ClassDBUtils.ExecSQL(sql, con_db, tr_id, ClassDBUtils.ExecMode.Exception);


                        sql = "update " + Points.Pref + DBManager.sUploadAliasRest + "file_dom set nzp_raj = '" +
                              nzp_raj +
                              "' where upper(rajon) = '" + finder.rajon.ToUpper() + "' and " +
                              " nzp_town = '" + finder.nzp_town + "' and nzp_file in (select l.nzp_file from " +
                              Points.Pref + DBManager.sUploadAliasRest + "files_selected l, " +
                              Points.Pref + DBManager.sUploadAliasRest +
                              "files_imported fi where fi.nzp_file=l.nzp_file and trim(fi.pref)='" + finder.bank + "')";
                        ClassDBUtils.ExecSQL(sql, con_db, tr_id, ClassDBUtils.ExecMode.Exception);

                    }
                    else
                    {
                        ret.text = "Населенный пункт с таким идентификационным номером уже добавлена в базу";
                        ret.result = false;
                        return ret;
                    }

                    tr_id.Commit();
                }
                catch (Exception ex)
                {
                    tr_id.Rollback();
                    MonitorLog.WriteLog("Ошибка выполнения процедуры AddNewRajon : " + ex.Message,
                        MonitorLog.typelog.Error, true);
                    ret.text = "Ошибка выполнения";
                    ret.result = false;
                    return ret;
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры AddNewRajon : " + ex.Message, MonitorLog.typelog.Error,
                    true);
                ret.text = "Ошибка выполнения";
                ret.result = false;
                return ret;
            }

            #endregion


            ret.result = true;
            ret.text = "Выполнено.";
            ret.tag = -1;

            return ret;
        }

        /// <summary>
        /// добавление новой улицы в базу
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsType AddNewStreet(UncomparedStreets finder)
        {
            ReturnsType ret = new ReturnsType();

            #region Запись в БД

            // using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            //{
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
                IDbTransaction tr_id = con_db.BeginTransaction();
                try
                {
                    string sql = "";
                    IDbCommand IfxCommand;
                    sql = " select count(*) from " + Points.Pref + "_data" + tableDelimiter +
                          "s_ulica where UPPER(ulica) = '" + finder.ulica.ToUpper() + "' and nzp_raj = " +
                          finder.nzp_raj;
                    IfxCommand = DBManager.newDbCommand(sql, con_db, tr_id);
                    if (Convert.ToInt32(IfxCommand.ExecuteScalar()) != 0)
                    {
                        tr_id.Rollback();
                        ret.text = "Улица с таким наименованием в данном населенном пункте уже добавлена в базу";
                        ret.result = false;
                        return ret;
                    }
                    sql = " select count(*) from " + Points.Pref + "_data" + tableDelimiter + "s_rajon where nzp_raj = " +
                          finder.nzp_raj;
                    IfxCommand = DBManager.newDbCommand(sql, con_db, tr_id);
                    if (Convert.ToInt32(IfxCommand.ExecuteScalar()) == 0)
                    {
                        ret.result = false;
                        ret.text = "Для данной улицы не задан соответствующий населенный пункт";
                        return ret;
                    }

                    decimal nzp_ul = 0;
                    if (finder.nzp_ul == "" || finder.nzp_ul == null)
                    {
                        Returns ret1 = new Returns();
                        string seq = Points.Pref + "_data" + tableDelimiter + "s_ulica_nzp_ul_seq";
                        string strNzp_ul;
#if PG
                        strNzp_ul = " nextval('" + seq + "') ";
#else
                        strNzp_ul = seq + ".nextval ";
#endif
                        sql = " insert into " + Points.Pref + "_data" + tableDelimiter +
                              "s_ulica ( nzp_ul, ulica,nzp_raj ) values ( " + strNzp_ul + ",'" +
                              finder.ulica.ToUpper() + "', '" + finder.nzp_raj + "' )";
                        ClassDBUtils.ExecSQL(sql, con_db, tr_id, ClassDBUtils.ExecMode.Exception);

#if PG
                        sql = "SELECT currval('" + seq + "')";
#else
                        sql = "SELECT " + seq + ".CURRVAL from  " + Points.Pref + "_data" + tableDelimiter + "dual";
#endif
                        

                        nzp_ul = Convert.ToDecimal(ExecScalar(con_db, tr_id, sql, out ret1, true));

                        sql = " insert into " + finder.bank + "_data" + tableDelimiter +
                              "s_ulica ( nzp_ul, ulica,nzp_raj ) values ( " + nzp_ul + ",'" +
                              finder.ulica.ToUpper() + "', '" + finder.nzp_raj + "' )";
                        IfxCommand = DBManager.newDbCommand(sql, con_db, tr_id);
                        IfxCommand.ExecuteNonQuery();

                        sql = "update " + Points.Pref + DBManager.sUploadAliasRest + "file_dom set nzp_ul = '" + nzp_ul +
                              "' where upper(ulica) = '" + finder.ulica.ToUpper() + "' and " +
                              " nzp_raj = '" + finder.nzp_raj + "' and nzp_file in (select l.nzp_file from " +
                              Points.Pref + DBManager.sUploadAliasRest + "files_selected l, " +
                              Points.Pref + DBManager.sUploadAliasRest +
                              "files_imported fi where fi.nzp_file=l.nzp_file and trim(fi.pref)='" + finder.bank + "')";
                        ClassDBUtils.ExecSQL(sql, con_db, tr_id, ClassDBUtils.ExecMode.Exception);

                    }
                    else
                    {
                        ret.text = "Улица с таким идентификационным номером уже добавлена в базу";
                        ret.result = false;
                        return ret;
                    }

                    tr_id.Commit();
                }
                catch (Exception ex)
                {
                    tr_id.Rollback();
                    MonitorLog.WriteLog("Ошибка выполнения процедуры AddNewStreet : " + ex.Message,
                        MonitorLog.typelog.Error, true);
                    ret.text = "Ошибка выполнения";
                    ret.result = false;
                    return ret;
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры AddNewStreet : " + ex.Message, MonitorLog.typelog.Error,
                    true);
                ret.text = "Ошибка выполнения";
                ret.result = false;
                return ret;
            }

            #endregion


            ret.result = true;
            ret.text = "Выполнено.";
            ret.tag = -1;

            return ret;
        }

        /// <summary>
        /// добавление нового дома в базу
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsType AddNewHouse(UncomparedHouses finder)
        {
            ReturnsType ret = new ReturnsType();

            #region Запись в БД

            // using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            //{
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
                IDbTransaction tr_id = con_db.BeginTransaction();
                try
                {
                    Returns ret1 = new Returns();
                    string sql = "";
                    IDbCommand IfxCommand;
                    decimal nzp_dom = 0;

                    var ndom = "";
                    var nkor = "-";
                    if (finder.dom.Split('/').Length > 1)
                    {
                        for (int i = 0; i < finder.dom.Split('/').Length - 1; i++)
                            if (i == 0) ndom += finder.dom.Split('/')[i];
                            else ndom += "/" + finder.dom.Split('/')[i];
                        nkor = finder.dom.Split('/')[finder.dom.Split('/').Length - 1];
                    }

                    string nkor_sql = "";
                    if (nkor == "-" || nkor == "" || nkor == null)
                    {
                        nkor_sql = "(UPPER(nkor) = '-' or upper(nkor) = '' or nkor is null)";
                    }
                    else
                        nkor_sql = "UPPER(nkor) = '" + nkor.ToUpper() + "'";
                    sql = " select count(*)  from " + finder.bank + "_data" + tableDelimiter +
                          "dom where UPPER(ndom) = '" + ndom.ToUpper() + "' and " + nkor_sql + " and nzp_ul = '" +
                          finder.nzp_ul + "'";
                    IfxCommand = DBManager.newDbCommand(sql, con_db, tr_id);
                    if (Convert.ToInt32(IfxCommand.ExecuteScalar()) > 0)
                    {
                        //tr_id.Rollback();
                        //ret.text = "Дом с таким наименованием по данной улице уже добавлена в базу";
                        //ret.result = false;


                        if (nkor == "-" || nkor == "" || nkor == null)
                        {
                            nkor_sql = "(UPPER(a.nkor) = '-' or upper(a.nkor) = '' or nkor is null)";
                        }
                        else
                            nkor_sql = "UPPER(a.nkor) = '" + nkor.ToUpper() + "'";

                        sql = " update " + Points.Pref + DBManager.sUploadAliasRest + "file_dom " +
                              " set nzp_dom = (select nzp_dom from " + finder.bank + "_data" + tableDelimiter + "dom a " +
                              " where a.nzp_ul=" + Points.Pref + DBManager.sUploadAliasRest + "file_dom.nzp_ul and " +
                              " trim(upper(a.ndom))=  '" + ndom.ToUpper() + "' and  " + nkor_sql + " )" +
                              " where " + Points.Pref + DBManager.sUploadAliasRest + "file_dom.nzp_dom is null and " +
                              Points.Pref + DBManager.sUploadAliasRest + "file_dom.nzp_file in (select l.nzp_file from " +
                              Points.Pref + DBManager.sUploadAliasRest + "files_selected l, " +
                              Points.Pref + DBManager.sUploadAliasRest +
                              "files_imported fi where fi.nzp_file=l.nzp_file and trim(fi.pref)='" + finder.bank + "')";
                        ClassDBUtils.ExecSQL(sql, con_db, tr_id, ClassDBUtils.ExecMode.Exception);
                        //return ret;

                        sql = " update " + Points.Pref + DBManager.sUploadAliasRest + "file_dom " +
                              " set nzp_dom = (select nzp_dom from " + finder.bank + "_data" + tableDelimiter +
                              "dom a where a.nzp_ul=" + Points.Pref + DBManager.sUploadAliasRest + "file_dom.nzp_ul " +
                              " and trim(upper(a.ndom))=trim(upper(" + Points.Pref + DBManager.sUploadAliasRest +
                              "file_dom.ndom)) " +
                              " and trim(upper(a.nkor))=trim(upper(" + Points.Pref + DBManager.sUploadAliasRest +
                              "file_dom.nkor)) and a.nzp_ul>0 and a.ndom is not null and a.nkor is not null ) " +
                              " where " + Points.Pref + DBManager.sUploadAliasRest + "file_dom.nzp_dom is null and " +
                              Points.Pref + DBManager.sUploadAliasRest + "file_dom.nzp_file in (select l.nzp_file from " +
                              Points.Pref + DBManager.sUploadAliasRest + "files_selected l, " +
                              Points.Pref + DBManager.sUploadAliasRest +
                              "files_imported fi where fi.nzp_file=l.nzp_file and trim(fi.pref)='" + finder.bank + "')";
                        ClassDBUtils.ExecSQL(sql, con_db, tr_id, ClassDBUtils.ExecMode.Exception);
                        return ret;
                    }

                    sql = " select count(*) from " + Points.Pref + "_data" + tableDelimiter + "s_ulica where nzp_ul = '" +
                          finder.nzp_ul + "'";
                    IfxCommand = DBManager.newDbCommand(sql, con_db, tr_id);
                    if (Convert.ToInt32(IfxCommand.ExecuteScalar()) == 0)
                    {
                        ret.result = false;
                        ret.text = "Для данного дома не задана соответствующая улица";
                        return ret;
                    }
                    sql = " select count(*) from " + Points.Pref + "_data" + tableDelimiter +
                          "s_rajon where nzp_raj = '" + finder.nzp_raj + "'";
                    IfxCommand = DBManager.newDbCommand(sql, con_db, tr_id);
                    if (Convert.ToInt32(IfxCommand.ExecuteScalar()) == 0)
                    {
                        ret.result = false;
                        ret.text = "Для данного дома не задан соответствующий населенный пункт";
                        return ret;
                    }


                    if (finder.nzp_dom == "" || finder.nzp_dom == null || finder.nzp_dom == "0")
                    {
                        string seq = Points.Pref + "_data" + tableDelimiter + "dom_nzp_dom_seq";
                        string strNzp_dom;
#if PG
                        strNzp_dom = " nextval('" + seq + "') ";
#else
                        strNzp_dom = seq + ".nextval ";
#endif

                        sql = " insert into " + Points.Pref + "_data" + tableDelimiter +
                              "dom (nzp_dom, ndom, nkor ,nzp_raj, nzp_ul, pref, nzp_wp) values " +
                              " ( " + strNzp_dom + ",'" + ndom + "', '" + nkor + "', '" + finder.nzp_raj + "', '" +
                              finder.nzp_ul + "', '" + finder.bank + "'," +
                              "(select nzp_wp from " + Points.Pref + "_kernel" + tableDelimiter +
                              "s_point where bd_kernel = '" + finder.bank + "'))";
                        ClassDBUtils.ExecSQL(sql, con_db, tr_id, ClassDBUtils.ExecMode.Exception);

#if PG
                        sql = "SELECT currval('" + seq + "')";
#else
                        sql = "SELECT " + seq + ".CURRVAL from  " + Points.Pref + "_data" + tableDelimiter + "dual";
#endif

                        nzp_dom = Convert.ToDecimal(ExecScalar(con_db, tr_id, sql, out ret1, true));
                        string korStr = " upper(nkor) = '" + nkor.ToUpper() + "'";
                        if (nkor == "")
                            korStr = " (upper(nkor) = '' or nkor is null) ";
                        sql = "update " + Points.Pref + DBManager.sUploadAliasRest + "file_dom set nzp_dom = '" +
                              nzp_dom +
                              "' where upper(ndom) = '" + ndom.ToUpper() + "' and " + korStr + " and " +
                              " nzp_raj = '" + finder.nzp_raj + "' and nzp_ul = '" + finder.nzp_ul +
                              "' and nzp_file in (select l.nzp_file from " + Points.Pref + DBManager.sUploadAliasRest +
                              "files_selected l, " +
                              Points.Pref + DBManager.sUploadAliasRest +
                              "files_imported fi where fi.nzp_file=l.nzp_file and trim(fi.pref)='" + finder.bank + "')";
                        ClassDBUtils.ExecSQL(sql, con_db, tr_id, ClassDBUtils.ExecMode.Exception);

                        sql = "insert into " + finder.bank + "_data" + tableDelimiter +
                              "dom (nzp_dom, ndom, nkor, nzp_raj, nzp_ul, nzp_wp, pref) " +
                              " select nzp_dom, ndom, nkor, nzp_raj, nzp_ul, nzp_wp, pref from " + Points.Pref + "_data" +
                              tableDelimiter + "dom " +
                              " where nzp_dom = " + nzp_dom;
                        ClassDBUtils.ExecSQL(sql, con_db, tr_id, ClassDBUtils.ExecMode.Exception);

                    }
                    else
                    {
                        ret.text = "Дом с таким идентификационным номером уже добавлен в базу";
                        ret.result = false;
                        return ret;
                    }

                    tr_id.Commit();
                }
                catch (Exception ex)
                {
                    tr_id.Rollback();
                    MonitorLog.WriteLog("Ошибка выполнения процедуры AddNewHouse : " + ex.Message,
                        MonitorLog.typelog.Error, true);
                    ret.text = "Ошибка выполнения";
                    ret.result = false;
                    return ret;
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры AddNewHouse : " + ex.Message, MonitorLog.typelog.Error,
                    true);
                ret.text = "Ошибка выполнения";
                ret.result = false;
                return ret;
            }
            //}

            #endregion


            ret.result = true;
            ret.text = "Выполнено.";
            ret.tag = -1;

            return ret;
        }

        /// <summary>
        /// добавление нового дома в базу
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsType AddNewHouse(UncomparedHouses finder, IDbConnection con_db)
        {
            ReturnsType ret = new ReturnsType();

            #region Запись в БД

            try
            {
                try
                {
                    Returns ret1 = new Returns();
                    string sql = "";
                    IDbCommand IfxCommand;
                    decimal nzp_dom = 0;

                    var ndom = "";
                    var nkor = "-";
                    if (finder.dom.Split('/').Length > 1)
                    {
                        for (int i = 0; i < finder.dom.Split('/').Length - 1; i++)
                            if (i == 0) ndom += finder.dom.Split('/')[i];
                            else ndom += "/" + finder.dom.Split('/')[i];
                        nkor = finder.dom.Split('/')[finder.dom.Split('/').Length - 1];
                    }

                    string nkor_sql = "";
                    if (nkor == "-" || nkor == "" || nkor == null)
                    {
                        nkor_sql = "(UPPER(nkor) = '-' or upper(nkor) = '' or nkor is null)";
                    }
                    else
                        nkor_sql = "UPPER(nkor) = '" + nkor.ToUpper() + "'";
                    sql = " select count(*)  from " + finder.bank + "_data" + tableDelimiter +
                          "dom where UPPER(ndom) = '" + ndom.ToUpper() + "' and " + nkor_sql + " and nzp_ul = '" +
                          finder.nzp_ul + "'";
                    IfxCommand = DBManager.newDbCommand(sql, con_db, null);
                    if (Convert.ToInt32(IfxCommand.ExecuteScalar()) > 0)
                    {
                        //tr_id.Rollback();
                        //ret.text = "Дом с таким наименованием по данной улице уже добавлена в базу";
                        //ret.result = false;


                        if (nkor == "-" || nkor == "" || nkor == null)
                        {
                            nkor_sql = "(UPPER(a.nkor) = '-' or upper(a.nkor) = '' or nkor is null)";
                        }
                        else
                            nkor_sql = "UPPER(a.nkor) = '" + nkor.ToUpper() + "'";

                        sql = " update " + Points.Pref + DBManager.sUploadAliasRest + "file_dom " +
                              " set nzp_dom = (select nzp_dom from " + finder.bank + "_data" + tableDelimiter + "dom a " +
                              " where a.nzp_ul=" + Points.Pref + DBManager.sUploadAliasRest + "file_dom.nzp_ul and " +
                              " trim(upper(a.ndom))=  '" + ndom.ToUpper() + "' and  " + nkor_sql + " )" +
                              " where " + Points.Pref + DBManager.sUploadAliasRest + "file_dom.nzp_dom is null and " +
                              Points.Pref + DBManager.sUploadAliasRest + "file_dom.nzp_file in (select l.nzp_file from " +
                              Points.Pref + DBManager.sUploadAliasRest + "files_selected l, " +
                              Points.Pref + DBManager.sUploadAliasRest +
                              "files_imported fi where fi.nzp_file=l.nzp_file and trim(fi.pref)='" + finder.bank + "')";
                        ClassDBUtils.ExecSQL(sql, con_db, null, ClassDBUtils.ExecMode.Exception);
                        

                        sql = " update " + Points.Pref + DBManager.sUploadAliasRest + "file_dom " +
                              " set nzp_dom = (select nzp_dom from " + finder.bank + "_data" + tableDelimiter +
                              "dom a where a.nzp_ul=" + Points.Pref + DBManager.sUploadAliasRest + "file_dom.nzp_ul " +
                              " and trim(upper(a.ndom))=trim(upper(" + Points.Pref + DBManager.sUploadAliasRest +
                              "file_dom.ndom)) " +
                              " and trim(upper(a.nkor))=trim(upper(" + Points.Pref + DBManager.sUploadAliasRest +
                              "file_dom.nkor)) and a.nzp_ul>0 and a.ndom is not null and a.nkor is not null ) " +
                              " where " + Points.Pref + DBManager.sUploadAliasRest + "file_dom.nzp_dom is null and " +
                              Points.Pref + DBManager.sUploadAliasRest + "file_dom.nzp_file in (select l.nzp_file from " +
                              Points.Pref + DBManager.sUploadAliasRest + "files_selected l, " +
                              Points.Pref + DBManager.sUploadAliasRest +
                              "files_imported fi where fi.nzp_file=l.nzp_file and trim(fi.pref)='" + finder.bank + "')";
                        ClassDBUtils.ExecSQL(sql, con_db, null, ClassDBUtils.ExecMode.Exception);
                        return ret;
                    }

                    sql = " select count(*) from " + Points.Pref + "_data" + tableDelimiter + "s_ulica where nzp_ul = '" +
                          finder.nzp_ul + "'";
                    IfxCommand = DBManager.newDbCommand(sql, con_db, null);
                    if (Convert.ToInt32(IfxCommand.ExecuteScalar()) == 0)
                    {
                        ret.result = false;
                        ret.text = "Для данного дома не задана соответствующая улица";
                        return ret;
                    }
                    sql = " select count(*) from " + Points.Pref + "_data" + tableDelimiter +
                          "s_rajon where nzp_raj = '" + finder.nzp_raj + "'";
                    IfxCommand = DBManager.newDbCommand(sql, con_db, null);
                    if (Convert.ToInt32(IfxCommand.ExecuteScalar()) == 0)
                    {
                        ret.result = false;
                        ret.text = "Для данного дома не задан соответствующий населенный пункт";
                        return ret;
                    }


                    if (finder.nzp_dom == "" || finder.nzp_dom == null || finder.nzp_dom == "0")
                    {
                        string seq = Points.Pref + "_data" + tableDelimiter + "dom_nzp_dom_seq";
                        string strNzp_dom;
#if PG
                        strNzp_dom = " nextval('" + seq + "') ";
#else
                        strNzp_dom = seq + ".nextval ";
#endif

                        sql = " insert into " + Points.Pref + "_data" + tableDelimiter +
                              "dom (nzp_dom, ndom, nkor ,nzp_raj, nzp_ul, pref, nzp_wp) values " +
                              " ( " + strNzp_dom + ",'" + ndom + "', '" + nkor + "', '" + finder.nzp_raj + "', '" +
                              finder.nzp_ul + "', '" + finder.bank + "'," +
                              "(select nzp_wp from " + Points.Pref + "_kernel" + tableDelimiter +
                              "s_point where bd_kernel = '" + finder.bank + "'))";
                        ClassDBUtils.ExecSQL(sql, con_db, null, ClassDBUtils.ExecMode.Exception);

#if PG
                        sql = "SELECT currval('" + seq + "')";
#else
                        sql = "SELECT " + seq + ".CURRVAL from  " + Points.Pref + "_data" + tableDelimiter + "dual";
#endif

                        nzp_dom = Convert.ToDecimal(ExecScalar(con_db, null, sql, out ret1, true));
                        string korStr = " upper(nkor) = '" + nkor.ToUpper() + "'";
                        if (nkor == "")
                            korStr = " (upper(nkor) = '' or nkor is null) ";
                        sql = "update " + Points.Pref + DBManager.sUploadAliasRest + "file_dom set nzp_dom = '" +
                              nzp_dom +
                              "' where upper(ndom) = '" + ndom.ToUpper() + "' and " + korStr + " and " +
                              " nzp_raj = '" + finder.nzp_raj + "' and nzp_ul = '" + finder.nzp_ul +
                              "' and nzp_file in (select l.nzp_file from " + Points.Pref + DBManager.sUploadAliasRest +
                              "files_selected l, " +
                              Points.Pref + DBManager.sUploadAliasRest +
                              "files_imported fi where fi.nzp_file=l.nzp_file and trim(fi.pref)='" + finder.bank + "')";
                        ClassDBUtils.ExecSQL(sql, con_db, null, ClassDBUtils.ExecMode.Exception);

                        sql = "insert into " + finder.bank + "_data" + tableDelimiter +
                              "dom (nzp_dom, ndom, nkor, nzp_raj, nzp_ul, nzp_wp, pref) " +
                              " select nzp_dom, ndom, nkor, nzp_raj, nzp_ul, nzp_wp, pref from " + Points.Pref + "_data" +
                              tableDelimiter + "dom " +
                              " where nzp_dom = " + nzp_dom;
                        ClassDBUtils.ExecSQL(sql, con_db, null, ClassDBUtils.ExecMode.Exception);

                    }
                    else
                    {
                        ret.text = "Дом с таким идентификационным номером уже добавлен в базу";
                        ret.result = false;
                        return ret;
                    }

                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка выполнения процедуры AddNewHouse : " + ex.Message,
                        MonitorLog.typelog.Error, true);
                    ret.text = "Ошибка выполнения";
                    ret.result = false;
                    return ret;
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры AddNewHouse : " + ex.Message, MonitorLog.typelog.Error,
                    true);
                ret.text = "Ошибка выполнения";
                ret.result = false;
                return ret;
            }
            #endregion


            ret.result = true;
            ret.text = "Выполнено.";
            ret.tag = -1;

            return ret;
        }

        #endregion Функции добавления значений в страницах сопоставления

        #region Функции удаления сопоставлений (домов участков и тд)

        /// <summary>
        /// удаление сопоставления участков
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsType UnlinkArea(ComparedAreas finder)
        {

            ReturnsType ret = new ReturnsType();

            #region Запись в БД

            // using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            //{
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

                string sql =
                    "update " + Points.Pref + DBManager.sUploadAliasRest + "file_area set nzp_area = null " +
                    " where nzp_area = " + finder.nzp_area +
                    " and nzp_file in (select l.nzp_file from " + Points.Pref + DBManager.sUploadAliasRest +
                    "files_selected l, " +
                    Points.Pref + DBManager.sUploadAliasRest +
                    "files_imported fi where fi.nzp_file=l.nzp_file and trim(fi.pref)='" + finder.bank + "')";
                IDbCommand IfxCommand = DBManager.newDbCommand(sql, con_db);
                IfxCommand.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры UnlinkTown : " + ex.Message, MonitorLog.typelog.Error,
                    true);
                ret.text = "Ошибка выполнения";
                ret.result = false;
                return ret;
            }
            //}

            #endregion


            ret.result = true;
            ret.text = "Выполнено.";
            ret.tag = -1;

            return ret;
        }

        /// <summary>
        /// удаление сопоставления поставщиков
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsType UnlinkSupp(ComparedSupps finder)
        {

            ReturnsType ret = new ReturnsType();

            #region Запись в БД

            // using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            //{
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
                string sql = "update " + Points.Pref + DBManager.sUploadAliasRest +
                             "file_supp set nzp_supp = null where nzp_supp = " + finder.nzp_supp +
                             " and nzp_file in (select l.nzp_file from " + Points.Pref + DBManager.sUploadAliasRest +
                             "files_selected l, " +
                             Points.Pref + DBManager.sUploadAliasRest +
                             "files_imported fi where fi.nzp_file=l.nzp_file and trim(fi.pref)='" + finder.bank + "')";
                IDbCommand IfxCommand = DBManager.newDbCommand(sql, con_db);
                IfxCommand.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры UnlinkSupp : " + ex.Message, MonitorLog.typelog.Error,
                    true);
                ret.text = "Ошибка выполнения";
                ret.result = false;
                return ret;
            }
            //}

            #endregion


            ret.result = true;
            ret.text = "Выполнено.";
            ret.tag = -1;

            return ret;
        }

        /// <summary>
        /// удаление сопоставления МО
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsType UnlinkMO(ComparedVills finder)
        {

            ReturnsType ret = new ReturnsType();

            #region Запись в БД

            // using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            //{
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
                string sql = "update " + Points.Pref + DBManager.sUploadAliasRest +
                             "file_mo set nzp_vill = null where nzp_vill = " + finder.nzp_vill +
                             " and nzp_file in (select l.nzp_file from " + Points.Pref + DBManager.sUploadAliasRest +
                             "files_selected l, " +
                             Points.Pref + DBManager.sUploadAliasRest +
                             "files_imported fi where fi.nzp_file=l.nzp_file and trim(fi.pref)='" + finder.bank + "')";
                IDbCommand IfxCommand = DBManager.newDbCommand(sql, con_db);
                IfxCommand.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры UnlinkMO : " + ex.Message, MonitorLog.typelog.Error,
                    true);
                ret.text = "Ошибка выполнения";
                ret.result = false;
                return ret;
            }
            //}

            #endregion


            ret.result = true;
            ret.text = "Выполнено.";
            ret.tag = -1;

            return ret;
        }

        /// <summary>
        /// удаление сопоставления услуг
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsType UnlinkServ(ComparedServs finder)
        {

            ReturnsType ret = new ReturnsType();

            #region Запись в БД

            // using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            //{
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

                string sql = "update " + Points.Pref + DBManager.sUploadAliasRest +
                             "file_services set nzp_serv = null where nzp_serv = " + finder.nzp_serv +
                             " and nzp_file in (select l.nzp_file from " + Points.Pref + DBManager.sUploadAliasRest +
                             "files_selected l, " +
                             Points.Pref + DBManager.sUploadAliasRest +
                             "files_imported fi where fi.nzp_file=l.nzp_file and trim(fi.pref)='" + finder.bank + "')";
                IDbCommand IfxCommand = DBManager.newDbCommand(sql, con_db);
                IfxCommand.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры UnlinkServ : " + ex.Message, MonitorLog.typelog.Error,
                    true);
                ret.text = "Ошибка выполнения";
                ret.result = false;
                return ret;
            }
            //}

            #endregion


            ret.result = true;
            ret.text = "Выполнено.";
            ret.tag = -1;

            return ret;
        }

        /// <summary>
        /// удаление сопоставления типоа доп. параметров
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsType UnlinkParType(ComparedParTypes finder)
        {

            ReturnsType ret = new ReturnsType();

            #region Запись в БД

            // using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            //{
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
                string sql = "update " + Points.Pref + DBManager.sUploadAliasRest +
                             "file_typeparams set nzp_prm = null where nzp_prm = " + finder.nzp_prm +
                             " and nzp_file in (select l.nzp_file from " + Points.Pref + DBManager.sUploadAliasRest +
                             "files_selected l, " +
                             Points.Pref + DBManager.sUploadAliasRest +
                             "files_imported fi where fi.nzp_file=l.nzp_file and trim(fi.pref)='" + finder.bank + "')";
                IDbCommand IfxCommand = DBManager.newDbCommand(sql, con_db);
                IfxCommand.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры UnlinkParType : " + ex.Message,
                    MonitorLog.typelog.Error, true);
                ret.text = "Ошибка выполнения";
                ret.result = false;
                return ret;
            }
            //}

            #endregion


            ret.result = true;
            ret.text = "Выполнено.";
            ret.tag = -1;

            return ret;
        }

        /// <summary>
        /// удаление сопоставления типоа благоустройства дома
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsType UnlinkParBlag(ComparedParTypes finder)
        {

            ReturnsType ret = new ReturnsType();

            #region Запись в БД

            // using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            //{
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
                string sql = "update " + Points.Pref + DBManager.sUploadAliasRest +
                             "file_blag set nzp_prm = null where nzp_prm = " + finder.nzp_prm +
                             " and nzp_file in (select l.nzp_file from " + Points.Pref + DBManager.sUploadAliasRest +
                             "files_selected l, " +
                             Points.Pref + DBManager.sUploadAliasRest +
                             "files_imported fi where fi.nzp_file=l.nzp_file and trim(fi.pref)='" + finder.bank + "')";
                IDbCommand IfxCommand = DBManager.newDbCommand(sql, con_db);
                IfxCommand.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры UnlinkParBlag : " + ex.Message,
                    MonitorLog.typelog.Error, true);
                ret.text = "Ошибка выполнения";
                ret.result = false;
                return ret;
            }
            //}

            #endregion


            ret.result = true;
            ret.text = "Выполнено.";
            ret.tag = -1;

            return ret;
        }

        /// <summary>
        /// удаление сопоставления типоа доп. параметров
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsType UnlinkParGas(ComparedParTypes finder)
        {

            ReturnsType ret = new ReturnsType();

            #region Запись в БД

            // using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            //{
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
                string sql = "update " + Points.Pref + DBManager.sUploadAliasRest +
                             "file_gaz set nzp_prm = null where nzp_prm = " + finder.nzp_prm +
                             " and nzp_file in (select l.nzp_file from " + Points.Pref + DBManager.sUploadAliasRest +
                             "files_selected l, " +
                             Points.Pref + DBManager.sUploadAliasRest +
                             "files_imported fi where fi.nzp_file=l.nzp_file and trim(fi.pref)='" + finder.bank + "')";
                IDbCommand IfxCommand = DBManager.newDbCommand(sql, con_db);
                IfxCommand.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры UnlinkParGas : " + ex.Message, MonitorLog.typelog.Error,
                    true);
                ret.text = "Ошибка выполнения";
                ret.result = false;
                return ret;
            }
            //}

            #endregion


            ret.result = true;
            ret.text = "Выполнено.";
            ret.tag = -1;

            return ret;
        }

        /// <summary>
        /// удаление сопоставления типоа доп. параметров
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsType UnlinkParWater(ComparedParTypes finder)
        {

            ReturnsType ret = new ReturnsType();

            #region Запись в БД

            // using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            //{
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

                string sql = "update " + Points.Pref + DBManager.sUploadAliasRest +
                             "file_voda set nzp_prm = null where nzp_prm = " + finder.nzp_prm +
                             " and nzp_file in (select l.nzp_file from " + Points.Pref + DBManager.sUploadAliasRest +
                             "files_selected l, " +
                             Points.Pref + DBManager.sUploadAliasRest +
                             "files_imported fi where fi.nzp_file=l.nzp_file and trim(fi.pref)='" + finder.bank + "')";
                IDbCommand IfxCommand = DBManager.newDbCommand(sql, con_db);
                IfxCommand.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры UnlinkParWater : " + ex.Message,
                    MonitorLog.typelog.Error, true);
                ret.text = "Ошибка выполнения";
                ret.result = false;
                return ret;
            }
            //}

            #endregion


            ret.result = true;
            ret.text = "Выполнено.";
            ret.tag = -1;

            return ret;
        }


        /// <summary>
        /// удаление сопоставления единиц измерения
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsType UnlinkMeasure(ComparedMeasures finder)
        {

            ReturnsType ret = new ReturnsType();

            #region Запись в БД

            // using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            //{
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
                string sql = "update " + Points.Pref + DBManager.sUploadAliasRest +
                             "file_measures set nzp_measure = null where nzp_measure = " + finder.nzp_measure +
                             " and nzp_file in (select l.nzp_file from " + Points.Pref + DBManager.sUploadAliasRest +
                             "files_selected l, " +
                             Points.Pref + DBManager.sUploadAliasRest +
                             "files_imported fi where fi.nzp_file=l.nzp_file and trim(fi.pref)='" + finder.bank + "')";
                IDbCommand IfxCommand = DBManager.newDbCommand(sql, con_db);
                IfxCommand.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры UnlinkMeasure : " + ex.Message,
                    MonitorLog.typelog.Error, true);
                ret.text = "Ошибка выполнения";
                ret.result = false;
                return ret;
            }
            //}

            #endregion


            ret.result = true;
            ret.text = "Выполнено.";
            ret.tag = -1;

            return ret;
        }

        /// <summary>
        /// удаление сопоставления городов
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsType UnlinkTown(ComparedTowns finder)
        {

            ReturnsType ret = new ReturnsType();

            #region Запись в БД

            // using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            //{
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
                string sql = "update " + Points.Pref + DBManager.sUploadAliasRest +
                             "file_dom set nzp_town = null where nzp_town = " + finder.nzp_town +
                             " and nzp_file in (select l.nzp_file from " + Points.Pref + DBManager.sUploadAliasRest +
                             "files_selected l, " +
                             Points.Pref + DBManager.sUploadAliasRest +
                             "files_imported fi where fi.nzp_file=l.nzp_file and trim(fi.pref)='" + finder.bank + "')";

                IDbCommand IfxCommand = DBManager.newDbCommand(sql, con_db);
                IfxCommand.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры UnlinkTown : " + ex.Message, MonitorLog.typelog.Error,
                    true);
                ret.text = "Ошибка выполнения";
                ret.result = false;
                return ret;
            }
            //}

            #endregion


            ret.result = true;
            ret.text = "Выполнено.";
            ret.tag = -1;

            return ret;
        }

        /// <summary>
        /// удаление сопоставления насеоенных пунктов
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsType UnlinkRajon(ComparedRajons finder)
        {

            ReturnsType ret = new ReturnsType();

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
                string sql =
                    " update " + Points.Pref + DBManager.sUploadAliasRest +
                    "file_dom set nzp_raj = null where nzp_raj = " +
                    finder.nzp_raj +
                    " and nzp_file in " +
                    "(select l.nzp_file from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected l, " +
                    Points.Pref + DBManager.sUploadAliasRest + "files_imported fi " +
                    " where fi.nzp_file=l.nzp_file and trim(fi.pref)='" + finder.bank + "')";
                IDbCommand IfxCommand = DBManager.newDbCommand(sql, con_db);
                IfxCommand.ExecuteNonQuery();

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры UnlinkStreet : " + ex.Message, MonitorLog.typelog.Error,
                    true);
                ret.text = "Ошибка выполнения";
                ret.result = false;
                return ret;
            }

            #endregion


            ret.result = true;
            ret.text = "Выполнено.";
            ret.tag = -1;

            return ret;
        }

        /// <summary>
        /// удаление сопоставления улиц
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsType UnlinkStreet(ComparedStreets finder)
        {

            ReturnsType ret = new ReturnsType();

            #region Запись в БД

            // using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            //{
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
                string sql = "update " + Points.Pref + DBManager.sUploadAliasRest +
                             "file_dom set nzp_ul = null where nzp_ul = " + finder.nzp_ul +
                             " and nzp_file in (select l.nzp_file from " + Points.Pref + DBManager.sUploadAliasRest +
                             "files_selected l, " +
                             Points.Pref + DBManager.sUploadAliasRest +
                             "files_imported fi where fi.nzp_file=l.nzp_file and trim(fi.pref)='" + finder.bank + "')";

                IDbCommand IfxCommand = DBManager.newDbCommand(sql, con_db);
                IfxCommand.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры UnlinkStreet : " + ex.Message, MonitorLog.typelog.Error,
                    true);
                ret.text = "Ошибка выполнения";
                ret.result = false;
                return ret;
            }
            //}

            #endregion


            ret.result = true;
            ret.text = "Выолнено.";
            ret.tag = -1;

            return ret;
        }

        /// <summary>
        /// удаление сопоставления домов
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsType UnlinkHouse(ComparedHouses finder)
        {

            ReturnsType ret = new ReturnsType();

            #region Запись в БД

            // using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            //{
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
                string sql = "update " + Points.Pref + DBManager.sUploadAliasRest +
                             "file_dom set nzp_dom = null where nzp_dom = " + finder.nzp_dom +
                             " and nzp_file in (select l.nzp_file from " + Points.Pref + DBManager.sUploadAliasRest +
                             "files_selected l, " +
                             Points.Pref + DBManager.sUploadAliasRest +
                             "files_imported fi where fi.nzp_file=l.nzp_file and trim(fi.pref)='" + finder.bank + "')";

                IDbCommand IfxCommand = DBManager.newDbCommand(sql, con_db);
                IfxCommand.ExecuteNonQuery();

                sql = "update " + Points.Pref + DBManager.sUploadAliasRest +
                             "file_kvar set (nzp_dom, nzp_kvar, ukas) = (null,null,null)" +
                             " where nzp_dom = " + finder.nzp_dom +
                             " and nzp_file in (select l.nzp_file from " + Points.Pref + DBManager.sUploadAliasRest +
                             "files_selected l, " +
                             Points.Pref + DBManager.sUploadAliasRest +
                             "files_imported fi where fi.nzp_file=l.nzp_file and trim(fi.pref)='" + finder.bank + "')";

                IfxCommand = DBManager.newDbCommand(sql, con_db);
                IfxCommand.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры UnlinkHouse : " + ex.Message, MonitorLog.typelog.Error,
                    true);
                ret.text = "Ошибка выполнения";
                ret.result = false;
                return ret;
            }
            //}

            #endregion


            ret.result = true;
            ret.text = "Выолнено.";
            ret.tag = -1;

            return ret;
        }

        /// <summary>
        /// удаление сопоставления ЛС
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsType UnlinkLS(ComparedLS finder)
        {

            ReturnsType ret = new ReturnsType();

            #region Запись в БД

            // using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            //{
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
                string sql = "update " + Points.Pref + DBManager.sUploadAliasRest +
                             "file_kvar set (nzp_kvar, ukas) = (null, null) where nzp_kvar = " + finder.nzp_kvar +
                             " and nzp_file in (select l.nzp_file from " + Points.Pref + DBManager.sUploadAliasRest +
                             "files_selected l, " +
                             Points.Pref + DBManager.sUploadAliasRest +
                             "files_imported fi where fi.nzp_file=l.nzp_file and trim(fi.pref)='" + finder.bank + "')";

                IDbCommand IfxCommand = DBManager.newDbCommand(sql, con_db);
                IfxCommand.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры UnlinkLS : " + ex.Message, MonitorLog.typelog.Error,
                    true);
                ret.text = "Ошибка выполнения";
                ret.result = false;
                return ret;
            }
            // }

            #endregion


            ret.result = true;
            ret.text = "Выолнено.";
            ret.tag = -1;

            return ret;
        }

        #endregion Функции удаления сопоставлений (домов участков и тд)

        #endregion Функции сопоставления
    }
}

