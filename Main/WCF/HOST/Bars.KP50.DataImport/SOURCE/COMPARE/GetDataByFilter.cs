using System;
using System.Collections.Generic;
using System.Data;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using globalsUtils = STCLINE.KP50.Global.Utils;

namespace Bars.KP50.DataImport.SOURCE.COMPARE
{
    public class GetDataByFilter : DataBaseHeadServer
    {
        private int PmaxVisible = 200;
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

                string sql = "select * from " + Points.Pref+ DBManager.sKernelAliasRest + "services " +
                             " where upper(service) " + DataImportUtils.plike + "  upper('" + finder.serv + "')";
                var dt = ClassDBUtils.OpenSQL(sql, con_db);

                List<UncomparedServs> list = new List<UncomparedServs>();
                for (int i = 0; i < dt.resultData.Rows.Count; i++)
                {
                    list.Add(new UncomparedServs()
                    {
                        serv = dt.resultData.Rows[i]["service"].ToString().Trim() + " (" + dt.resultData.Rows[i]["nzp_serv"].ToString().Trim() + ")",
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

                string sql = "select * from " + Points.Pref + DBManager.sKernelAliasRest + "prm_name " +
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

                string sql = "select * from " + Points.Pref + DBManager.sKernelAliasRest + "prm_name " +
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

                string sql = "select * from " + Points.Pref + DBManager.sKernelAliasRest + "prm_name " +
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

                string sql = "select * from " + Points.Pref + DBManager.sKernelAliasRest + "prm_name " +
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
            ReturnsObjectType<List<UncomparedStreets>> ret = new ReturnsObjectType<List<UncomparedStreets>>() { text = "Выполнено", result = true }; 
            Returns t = new Returns(true);

            using (IDbConnection conn_db = GetConnection(Constants.cons_Kernel))
            {
                try
                {
                    t = OpenDb(conn_db, true);
                    if (!t.result) throw new Exception(t.text);

                    finder.ulica = finder.ulica.Replace('Ё', 'Е').Replace('ё', 'е');
                    if (finder.nzp_raj == null) finder.nzp_raj = "";
                    if (finder.nzp_town == null) finder.nzp_town = "";

                    string sql = "select ul.nzp_ul, ul.nzp_raj, ul.ulica, " +
                        " (case when trim(ul.ulicareg) = '-' then '' else ul.ulicareg end) ulicareg, " +
                        " (case when trim(raj.rajon) = '-' then '' else raj.rajon end) rajon, " +
                        " (case when trim(town.town) = '-' then '' else town.town end) town " +
                        " from " + Points.Pref + DBManager.sDataAliasRest + "s_ulica ul, " +
                        Points.Pref + DBManager.sDataAliasRest + "s_rajon raj, " +
                        Points.Pref + DBManager.sDataAliasRest + "s_town town " +
                        " where ul.nzp_raj = raj.nzp_raj " +
                        "   and raj.nzp_town = town.nzp_town " +
                        "   and upper(ulica) " + DataImportUtils.plike + " upper(" + globalsUtils.EStrNull(finder.ulica) + ") " +
                        (finder.nzp_raj != "" ? " and raj.nzp_raj = " + finder.nzp_raj : "") +
                        (finder.nzp_town != "" ? " and raj.nzp_town = " + finder.nzp_town : "") +
                        " order by ulica, ulicareg, town, rajon ";
                        
                    var dt = ClassDBUtils.OpenSQL(sql, conn_db);

                    List<UncomparedStreets> list = new List<UncomparedStreets>();
                    
                    string town = "";
                    string rajon = "";
                    string ulicareg = "";
                    
                    for (int i = 0; i < dt.resultData.Rows.Count; i++)
                    {
                        town = rajon = ulicareg = "";
                        
                        if (dt.resultData.Rows[i]["town"] != DBNull.Value) town = dt.resultData.Rows[i]["town"].ToString().Trim();
                        if (dt.resultData.Rows[i]["rajon"] != DBNull.Value) rajon = dt.resultData.Rows[i]["rajon"].ToString().Trim();
                        if (dt.resultData.Rows[i]["ulicareg"] != DBNull.Value) ulicareg = dt.resultData.Rows[i]["ulicareg"].ToString().Trim();
                        
                        // УЛ (Намский улус, Намцы с)
                        if (rajon != "")
                        {
                            if (town != "") town += ", ";
                            town += rajon;
                        }
                        if (town != "") town = "(" + town + ")";
                        if (ulicareg != "") town = " " + ulicareg + town;
                        
                        list.Add(new UncomparedStreets()
                        {
                            nzp_ul = dt.resultData.Rows[i]["nzp_ul"].ToString().Trim(),
                            nzp_raj = dt.resultData.Rows[i]["nzp_raj"].ToString().Trim(),
                            ulica = dt.resultData.Rows[i]["ulica"].ToString().Trim() + town,
                            show_data = dt.resultData.Rows[i]["ulica"].ToString().Trim() + town
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
                    MonitorLog.WriteLog("Ошибка выполнения процедуры GetStreetsByFilter : " + ex.Message, MonitorLog.typelog.Error, true);
                    ret = new ReturnsObjectType<List<UncomparedStreets>>() { text = "Ошибка выполнения", result = false }; 
                }
            }

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
                        nkor = dt.resultData.Rows[i]["nkor"].ToString().Trim();
                    var ndom = "";
                    if (dt.resultData.Rows[i]["ndom"] != DBNull.Value &&
                        dt.resultData.Rows[i]["ndom"].ToString().Trim() != "" &&
                        dt.resultData.Rows[i]["ndom"].ToString().Trim() != "-")
                        ndom = dt.resultData.Rows[i]["ndom"].ToString().Trim();
                    var nkvar = "";
                    if (dt.resultData.Rows[i]["nkvar"] != DBNull.Value &&
                        dt.resultData.Rows[i]["nkvar"].ToString().Trim() != "")
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
        /// получение юридических лиц по фильтру
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<List<UncomparedPayer>> GetPayerByFilter(UncomparedPayer finder)
        {
            ReturnsObjectType<List<UncomparedPayer>> ret = new ReturnsObjectType<List<UncomparedPayer>>();

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

                string sql = "select * from " + Points.Pref + DBManager.sKernelAliasRest + "s_payer " +
                             " where npayer " + DataImportUtils.plike + " '" + finder.payer + "'" +
                             " union " +
                             "select * from " + Points.Pref + DBManager.sKernelAliasRest + "s_payer " +
                             " where payer " + DataImportUtils.plike + " '" + finder.payer + "'";


                var dt = ClassDBUtils.OpenSQL(sql, con_db);

                List<UncomparedPayer> list = new List<UncomparedPayer>();
                for (int i = 0; i < dt.resultData.Rows.Count; i++)
                {
                    list.Add(new UncomparedPayer()
                    {
                        payer = dt.resultData.Rows[i]["npayer"].ToString().Trim() + " (" + dt.resultData.Rows[i]["payer"].ToString().Trim()+")",
                        nzp_payer = dt.resultData.Rows[i]["nzp_payer"].ToString().Trim()
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
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetPayerByFilter : " + ex.Message,
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
    }
}
