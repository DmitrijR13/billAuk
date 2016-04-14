
using System;
using System.Collections.Generic;
using System.Data;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.DataImport.SOURCE.COMPARE
{
    /// <summary>
    /// получение данных для отображения
    /// </summary>
    public class GetUncomparedData : SelectedFiles
    {
        private int PmaxVisible = 200;

        /// <summary>
        /// получение несопоставленных участков
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<List<UncomparedAreas>> GetUncomparedArea(FilesImported finder)
        {
            ReturnsObjectType<List<UncomparedAreas>> ret = new ReturnsObjectType<List<UncomparedAreas>>();

            using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            {
                try
                {
                    var t = OpenDb(con_db, true);
                    if (!t.result) throw new Exception(t.text);

                    string sql = "select distinct upper(area) as area " + 
                        " from " + Points.Pref + DBManager.sUploadAliasRest + "file_area  " +
                        " where nzp_area is null " +
                        WhereNzpFile(finder.selectedFiles);

                    var dt = ClassDBUtils.OpenSQL(SetLimitOffset(sql + " order by 1", finder), con_db);
                    List<UncomparedAreas> list = new List<UncomparedAreas>();
                    for (int i = 0; i < dt.resultData.Rows.Count; i++)
                    {
                        list.Add(new UncomparedAreas()
                        {
                            area = dt.resultData.Rows[i]["area"].ToString().Trim()
                        });
                    }

                    ret = new ReturnsObjectType<List<UncomparedAreas>>() { returnsData = list, tag = GetRecordCount(sql, con_db), result = true, text = "Выполнено" };
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка выполнения процедуры GetUncomparedArea : " + ex.Message, MonitorLog.typelog.Error, true);
                    ret = new ReturnsObjectType<List<UncomparedAreas>>() { returnsData = null, result = false, text = "Ошибка выполнения" };
                }
            }

            return ret;
        }

        /// <summary>
        /// получение несопоставленных поставщиков
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<List<UncomparedSupps>> GetUncomparedSupp(FilesImported finder)
        {
            ReturnsObjectType<List<UncomparedSupps>> ret = new ReturnsObjectType<List<UncomparedSupps>>();

            using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            {
                try
                {
                    var t = OpenDb(con_db, true);
                    if (!t.result) throw new Exception(t.text);

                    string sql = "select distinct upper(supp_name) as supp_name " + 
                        " from " + Points.Pref + DBManager.sUploadAliasRest + "file_supp " +
                        " where nzp_supp is null " +
                        WhereNzpFile(finder.selectedFiles);

                    var dt = ClassDBUtils.OpenSQL(SetLimitOffset(sql + " order by 1", finder), con_db);
                    List<UncomparedSupps> list = new List<UncomparedSupps>();
                    for (int i = 0; i < dt.resultData.Rows.Count; i++)
                    {
                        list.Add(new UncomparedSupps()
                        {
                            supp = dt.resultData.Rows[i]["supp_name"].ToString().Trim()
                        });
                    }

                    ret = new ReturnsObjectType<List<UncomparedSupps>>() { returnsData = list, tag = GetRecordCount(sql, con_db), result = true, text = "Выполнено" };
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка выполнения процедуры GetUncomparedSupp : " + ex.Message, MonitorLog.typelog.Error, true);
                    ret = new ReturnsObjectType<List<UncomparedSupps>>() { returnsData = null, result = false, text = "Ошибка выполнения" };
                }
            }

            return ret;
        }

        /// <summary>
        /// получение несопоставленных МО
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<List<UncomparedVills>> GetUncomparedMO(FilesImported finder)
        {
            ReturnsObjectType<List<UncomparedVills>> ret = new ReturnsObjectType<List<UncomparedVills>>();

            using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            {
                try
                {
                    var t = OpenDb(con_db, true);
                    if (!t.result) throw new Exception(t.text);

                    string sql = "select distinct upper(vill) as vill " + 
                        " from " + Points.Pref + DBManager.sUploadAliasRest + "file_mo " +
                        " where nzp_vill is null " +
                        WhereNzpFile(finder.selectedFiles);

                    var dt = ClassDBUtils.OpenSQL(SetLimitOffset(sql + " order by 1", finder), con_db);
                    List<UncomparedVills> list = new List<UncomparedVills>();
                    for (int i = 0; i < dt.resultData.Rows.Count; i++)
                    {
                        list.Add(new UncomparedVills()
                        {
                            vill = dt.resultData.Rows[i]["vill"].ToString().Trim()
                        });
                    }

                    ret = new ReturnsObjectType<List<UncomparedVills>>() { returnsData = list, tag = GetRecordCount(sql, con_db), result = true, text = "Выполнено" };
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка выполнения процедуры GetUncomparedMO : " + ex.Message, MonitorLog.typelog.Error, true);
                    ret = new ReturnsObjectType<List<UncomparedVills>>() { returnsData = null, result = false, text = "Ошибка выполнения" };
                }
            }

            return ret;
        }

        /// <summary>
        /// получение несопоставленных услуг
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<List<UncomparedServs>> GetUncomparedServ(FilesImported finder)
        {
            ReturnsObjectType<List<UncomparedServs>> ret = new ReturnsObjectType<List<UncomparedServs>>();

            using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            {
                try
                {
                    var t = OpenDb(con_db, true);
                    if (!t.result) throw new Exception(t.text);

                    string sql = "select distinct upper(service) as service " +
                        " from " + Points.Pref + DBManager.sUploadAliasRest + "file_services " +
                        " where nzp_serv is null " +
                        WhereNzpFile(finder.selectedFiles);

                    var dt = ClassDBUtils.OpenSQL(SetLimitOffset(sql + " order by 1", finder), con_db);
                    List<UncomparedServs> list = new List<UncomparedServs>();
                    for (int i = 0; i < dt.resultData.Rows.Count; i++)
                    {
                        list.Add(new UncomparedServs()
                        {
                            serv = dt.resultData.Rows[i]["service"].ToString().Trim()
                        });
                    }

                    ret = new ReturnsObjectType<List<UncomparedServs>>() { returnsData = list, tag = GetRecordCount(sql, con_db), result = true, text = "Выполнено" };
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка выполнения процедуры GetUncomparedServ : " + ex.Message, MonitorLog.typelog.Error, true);
                    ret = new ReturnsObjectType<List<UncomparedServs>>() { returnsData = null, result = false, text = "Ошибка выполнения" };
                }
            }

            return ret;
        }

        /// <summary>
        /// получение несопоставленных типов доп. параметров
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<List<UncomparedParTypes>> GetUncomparedParType(FilesImported finder)
        {
            ReturnsObjectType<List<UncomparedParTypes>> ret = new ReturnsObjectType<List<UncomparedParTypes>>();

            using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            {
                try
                {
                    var t = OpenDb(con_db, true);
                    if (!t.result) throw new Exception(t.text);

                    string sql = " select distinct upper(prm_name) as prm_name from " + 
                        Points.Pref + DBManager.sUploadAliasRest + "file_typeparams " +
                        " where nzp_prm is null " +
                        WhereNzpFile(finder.selectedFiles);

                    var dt = ClassDBUtils.OpenSQL(SetLimitOffset(sql + " order by 1", finder), con_db);
                    List<UncomparedParTypes> list = new List<UncomparedParTypes>();
                    for (int i = 0; i < dt.resultData.Rows.Count; i++)
                    {
                        list.Add(new UncomparedParTypes()
                        {
                            name_prm = dt.resultData.Rows[i]["prm_name"].ToString().Trim()
                        });
                    }

                    ret = new ReturnsObjectType<List<UncomparedParTypes>>() { returnsData = list, tag = GetRecordCount(sql, con_db), result = true, text = "Выполнено" };
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка выполнения процедуры GetUncomparedParType : " + ex.Message, MonitorLog.typelog.Error, true);
                    ret = new ReturnsObjectType<List<UncomparedParTypes>>() { returnsData = null, result = false, text = "Ошибка выполнения" };
                }
            }

            return ret;
        }

        /// <summary>
        /// получение несопоставленных параметров
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        protected ReturnsObjectType<List<UncomparedParTypes>> GetUncomparedParameters(FilesImported finder, string bufferTable)
        {
            ReturnsObjectType<List<UncomparedParTypes>> ret = new ReturnsObjectType<List<UncomparedParTypes>>();

            using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            {
                try
                {
                    var t = OpenDb(con_db, true);
                    if (!t.result) throw new Exception(t.text);

                    string sql = " select distinct upper(name) as name_prm " + 
                        " from " + Points.Pref + DBManager.sUploadAliasRest + bufferTable +
                             " where nzp_prm is null  " +
                        WhereNzpFile(finder.selectedFiles);

                    var dt = ClassDBUtils.OpenSQL(SetLimitOffset(sql + " order by 1", finder), con_db);
                    List<UncomparedParTypes> list = new List<UncomparedParTypes>();
                    for (int i = 0; i < dt.resultData.Rows.Count; i++)
                    {
                        list.Add(new UncomparedParTypes()
                        {
                            name_prm = dt.resultData.Rows[i]["name_prm"].ToString().Trim()
                        });
                    }

                    ret = new ReturnsObjectType<List<UncomparedParTypes>>() { returnsData = list, tag = GetRecordCount(sql, con_db), result = true, text = "Выполнено" };
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка выполнения процедуры GetUncomparedParameters : " + ex.Message, MonitorLog.typelog.Error, true);
                    ret = new ReturnsObjectType<List<UncomparedParTypes>>() { returnsData = null, result = false, text = "Ошибка выполнения" };
                }
            }

            return ret;
        }


        // <summary>
        /// получение несопоставленных типов благоустройства дома
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<List<UncomparedParTypes>> GetUncomparedParBlag(FilesImported finder)
        {
            return GetUncomparedParameters(finder, "file_blag");
        }

        // <summary>
        /// получение несопоставленных типов дома по газу
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<List<UncomparedParTypes>> GetUncomparedParGas(FilesImported finder)
        {
            return GetUncomparedParameters(finder, "file_gaz");
        }

        // <summary>
        /// получение несопоставленных типов дома по воде
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<List<UncomparedParTypes>> GetUncomparedParWater(FilesImported finder)
        {
            return GetUncomparedParameters(finder, "file_voda");
        }

        /// <summary>
        /// получение несопоставленных единиц измерения
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<List<UncomparedMeasures>> GetUncomparedMeasure(FilesImported finder)
        {
            ReturnsObjectType<List<UncomparedMeasures>> ret = new ReturnsObjectType<List<UncomparedMeasures>>();

            using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            {
                try
                {
                    var t = OpenDb(con_db, true);
                    if (!t.result) throw new Exception(t.text);

                    string sql = sql = "select distinct upper(measure) as measure " +
                        " from " + Points.Pref + DBManager.sUploadAliasRest + "file_measures " +
                        " where nzp_measure is null " +
                        WhereNzpFile(finder.selectedFiles);

                    var dt = ClassDBUtils.OpenSQL(SetLimitOffset(sql + " order by 1", finder), con_db);
                    List<UncomparedMeasures> list = new List<UncomparedMeasures>();
                    for (int i = 0; i < dt.resultData.Rows.Count; i++)
                    {
                        list.Add(new UncomparedMeasures()
                        {
                            measure = dt.resultData.Rows[i]["measure"].ToString().Trim()
                        });
                    }

                    ret = new ReturnsObjectType<List<UncomparedMeasures>>() { returnsData = list, tag = GetRecordCount(sql, con_db), result = true, text = "Выполнено" };
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка выполнения процедуры GetUncomparedMeasure : " + ex.Message, MonitorLog.typelog.Error, true);
                    ret = new ReturnsObjectType<List<UncomparedMeasures>>() { returnsData = null, result = false, text = "Ошибка выполнения" };
                }
            }

            return ret;
        }

        /// <summary>
        /// получение несопоставленных городов
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<List<UncomparedTowns>> GetUncomparedTown(FilesImported finder)
        {
            ReturnsObjectType<List<UncomparedTowns>> ret = new ReturnsObjectType<List<UncomparedTowns>>();

            using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            {
                try
                {
                    var t = OpenDb(con_db, true);
                    if (!t.result) throw new Exception(t.text);

                    string sql = " select distinct upper(town) as town " + 
                        " from " + Points.Pref + DBManager.sUploadAliasRest + "file_dom " +
                        "  where nzp_town is null " +
                        WhereNzpFile(finder.selectedFiles);

                    var dt = ClassDBUtils.OpenSQL(SetLimitOffset(sql + " order by 1", finder), con_db);
                    List<UncomparedTowns> list = new List<UncomparedTowns>();
                    for (int i = 0; i < dt.resultData.Rows.Count; i++)
                    {
                        list.Add(new UncomparedTowns()
                        {
                            town = dt.resultData.Rows[i]["town"].ToString().Trim()
                        });
                    }

                    ret = new ReturnsObjectType<List<UncomparedTowns>>() { returnsData = list, tag = GetRecordCount(sql, con_db), result = true, text = "Выполнено" };
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка выполнения процедуры GetUncomparedTown : " + ex.Message, MonitorLog.typelog.Error, true);
                    ret = new ReturnsObjectType<List<UncomparedTowns>>() { returnsData = null, result = false, text = "Ошибка выполнения" };
                }
            }

            return ret;
        }

        /// <summary>
        /// получение несопоставленных улиц
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<List<UncomparedRajons>> GetUncomparedRajon(FilesImported finder)
        {
            ReturnsObjectType<List<UncomparedRajons>> ret = new ReturnsObjectType<List<UncomparedRajons>>();

            using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            {
                try
                {
                    var t = OpenDb(con_db, true);
                    if (!t.result) throw new Exception(t.text);

                    string sql = " select distinct upper(rajon) as rajon, upper(town) as town, nzp_town, nzp_raj " +
                        " from " + Points.Pref + DBManager.sUploadAliasRest + "file_dom f " +
                        " where ( nzp_raj is null or (select count(*) from " + Points.Pref + DBManager.sDataAliasRest + "s_rajon b where b.nzp_raj = f.nzp_raj) = 0 ) " +
                        WhereNzpFile(finder.selectedFiles);

                    var dt = ClassDBUtils.OpenSQL(SetLimitOffset(sql + " order by 1,2", finder), con_db);
                    List<UncomparedRajons> list = new List<UncomparedRajons>();
                    for (int i = 0; i < dt.resultData.Rows.Count; i++)
                    {
                        var town = "";
                        var nzp_raj = "";

                        if (dt.resultData.Rows[i]["town"] != DBNull.Value) town = dt.resultData.Rows[i]["town"].ToString().Trim();
                        if (dt.resultData.Rows[i]["nzp_raj"] != DBNull.Value) nzp_raj = dt.resultData.Rows[i]["nzp_raj"].ToString().Trim();

                        list.Add(new UncomparedRajons()
                        {
                            show_data = dt.resultData.Rows[i]["rajon"].ToString().Trim() + " (" + town + ")",
                            rajon = dt.resultData.Rows[i]["rajon"].ToString().Trim(),
                            nzp_town = dt.resultData.Rows[i]["nzp_town"].ToString().Trim(),
                            nzp_raj = nzp_raj
                        });

                    }

                    ret = new ReturnsObjectType<List<UncomparedRajons>>() { returnsData = list, tag = GetRecordCount(sql, con_db), result = true, text = "Выполнено" };
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка выполнения процедуры GetUncomparedRajon : " + ex.Message, MonitorLog.typelog.Error, true);
                    ret = new ReturnsObjectType<List<UncomparedRajons>>() { returnsData = null, result = false, text = "Ошибка выполнения" };
                }
            }

            return ret;
        }

        /// <summary>
        /// получение несопоставленных улиц
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<List<UncomparedStreets>> GetUncomparedStreets(FilesImported finder)
        {
            ReturnsObjectType<List<UncomparedStreets>> ret = new ReturnsObjectType<List<UncomparedStreets>>();

            using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            {
                try
                {
                    var t = OpenDb(con_db, true);
                    if (!t.result) throw new Exception(t.text);

                    string sql = " select distinct UPPER(ulica) as ulica, UPPER(town) as town, UPPER(rajon) as rajon, nzp_ul, nzp_town, nzp_raj " +
                        " from " + Points.Pref + DBManager.sUploadAliasRest + "file_dom f " +
                        " where (nzp_ul is null or (select count(*) from " + Points.Pref + "_data" + tableDelimiter +
                        "s_ulica b where b.nzp_ul = f.nzp_ul) = 0) " +
                        WhereNzpFile(finder.selectedFiles);

                    var dt = ClassDBUtils.OpenSQL(SetLimitOffset(sql + " order by 1,2,3", finder), con_db);
                    List<UncomparedStreets> list = new List<UncomparedStreets>();
                    for (int i = 0; i < dt.resultData.Rows.Count; i++)
                    {
                        var town = "";
                        var rajon = "";
                        var nzp_ul = "";
                        
                        if (dt.resultData.Rows[i]["rajon"] != DBNull.Value) rajon = dt.resultData.Rows[i]["rajon"].ToString().Trim();
                        if (dt.resultData.Rows[i]["town"] != DBNull.Value) town = dt.resultData.Rows[i]["town"].ToString().Trim();
                        if (dt.resultData.Rows[i]["nzp_ul"] != DBNull.Value) nzp_ul = dt.resultData.Rows[i]["nzp_ul"].ToString().Trim();
                        
                        list.Add(new UncomparedStreets()
                        {
                            show_data = dt.resultData.Rows[i]["ulica"].ToString().Trim() + " (" + GetAddress(town, rajon) + ")",
                            ulica = dt.resultData.Rows[i]["ulica"].ToString().Trim().Replace('Ё', 'Е').Replace('ё', 'е'),
                            nzp_town = dt.resultData.Rows[i]["nzp_town"].ToString().Trim(),
                            nzp_raj = dt.resultData.Rows[i]["nzp_raj"].ToString().Trim(),
                            nzp_ul = nzp_ul
                        });
                    }

                    ret = new ReturnsObjectType<List<UncomparedStreets>>() { returnsData = list, tag = GetRecordCount(sql, con_db), result = true, text = "Выполнено" };
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка выполнения процедуры GetUncomparedStreets : " + ex.Message, MonitorLog.typelog.Error, true);
                    ret = new ReturnsObjectType<List<UncomparedStreets>>() { returnsData = null, result = false, text = "Ошибка выполнения" };
                }
            }

            return ret;
        }

        /// <summary>
        /// получение несопоставленных домов
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<List<UncomparedHouses>> GetUncomparedHouse(FilesImported finder)
        {
            ReturnsObjectType<List<UncomparedHouses>> ret = new ReturnsObjectType<List<UncomparedHouses>>();

            using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            {
                try
                {
                    var t = OpenDb(con_db, true);
                    if (!t.result) throw new Exception(t.text);

                    string sql = " select distinct UPPER(ndom) as ndom, UPPER(nkor) as nkor, UPPER(ulica) as ulica, UPPER(town) as town, UPPER(rajon) as rajon, " + 
                        " nzp_dom, nzp_town, nzp_raj, nzp_ul " +
                        " from " + Points.Pref + DBManager.sUploadAliasRest + "file_dom f " +
                        " where ( nzp_dom is null  or (select count(*) from " + Points.Pref + DBManager.sDataAliasRest + "dom b where b.nzp_dom = f.nzp_dom) = 0 ) " +
                        WhereNzpFile(finder.selectedFiles);

                    var dt = ClassDBUtils.OpenSQL(SetLimitOffset(sql + " order by 1,2,3,4,5", finder), con_db);
                    List<UncomparedHouses> list = new List<UncomparedHouses>();

                    var town = "";
                    var rajon = "";
                    var ulica = "";
                    var nkor = "";
                    var ndom = "";
                    var nzp_dom = "";

                    for (int i = 0; i < dt.resultData.Rows.Count; i++)
                    {
                        town = rajon = ulica = nkor = ndom = nzp_dom = "";

                        if (dt.resultData.Rows[i]["town"] != DBNull.Value) town = dt.resultData.Rows[i]["town"].ToString().Trim();
                        if (dt.resultData.Rows[i]["rajon"] != DBNull.Value) rajon = dt.resultData.Rows[i]["rajon"].ToString().Trim();
                        if (dt.resultData.Rows[i]["ulica"] != DBNull.Value) ulica = dt.resultData.Rows[i]["ulica"].ToString().Trim();
                        if (dt.resultData.Rows[i]["nkor"] != DBNull.Value) nkor = dt.resultData.Rows[i]["nkor"].ToString().Trim();
                        if (dt.resultData.Rows[i]["ndom"] != DBNull.Value) ndom = dt.resultData.Rows[i]["ndom"].ToString().Trim();
                        if (dt.resultData.Rows[i]["nzp_dom"] != DBNull.Value) nzp_dom = dt.resultData.Rows[i]["nzp_dom"].ToString().Trim();

                        list.Add(new UncomparedHouses()
                        {
                            show_data = GetHouse(ndom, nkor) + "(" + GetHouseAddress(ulica, town, rajon) + ")",
                            dom = GetHouse(ndom, nkor),
                            nzp_town = dt.resultData.Rows[i]["nzp_town"].ToString().Trim(),
                            nzp_raj = dt.resultData.Rows[i]["nzp_raj"].ToString().Trim(),
                            nzp_ul = dt.resultData.Rows[i]["nzp_ul"].ToString().Trim(),
                            nzp_dom = nzp_dom
                        });
                    }

                    ret = new ReturnsObjectType<List<UncomparedHouses>>() { returnsData = list, tag = GetRecordCount(sql, con_db), result = true, text = "Выполнено" };
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка выполнения процедуры GetUncomparedHouse : " + ex.Message, MonitorLog.typelog.Error, true);
                    ret = new ReturnsObjectType<List<UncomparedHouses>>() { returnsData = null, result = false, text = "Ошибка выполнения" };
                }
            }

            return ret;
        }

        /// <summary>
        /// получение несопоставленных домов
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<List<UncomparedLS>> GetUncomparedLS(FilesImported finder)
        {
            ReturnsObjectType<List<UncomparedLS>> ret = new ReturnsObjectType<List<UncomparedLS>>();

            using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            {
                try
                {
                    var t = OpenDb(con_db, true);
                    if (!t.result) throw new Exception(t.text);
                    
                    string sql = " select distinct " + 
                        " fk.id as ls, upper(fk.fam) as fam, upper(fk.ima) as ima, upper(fk.otch) as otch, " +
                        " UPPER(town) as town, UPPER(rajon) as rajon, UPPER(ulica) as ulica, UPPER(ndom) as ndom, UPPER(nkor) as nkor, fk.nkvar as kvar, fk.nkvar_n as kom, " +
                        " f.nzp_dom, nzp_town, nzp_raj, nzp_ul, fk.nzp_kvar  " +
                        " from " + Points.Pref + DBManager.sUploadAliasRest + "file_dom f, " + 
                        Points.Pref + DBManager.sUploadAliasRest + "file_kvar fk " +
                        " where f.nzp_dom is not null  " +
                        "   and f.nzp_file = fk.nzp_file " + 
                        "   and f.nzp_dom = fk.nzp_dom " +
                        "   and (fk.nzp_kvar is null " + 
                        "   or (select count(*) from " + Points.Pref + DBManager.sDataAliasRest + "kvar b where b.nzp_kvar = fk.nzp_kvar) = 0 )" +
                        WhereNzpFile(finder.selectedFiles, "fk");

                    var dt = ClassDBUtils.OpenSQL(SetLimitOffset(sql + " order by 1,2,3,4,5,6,7,8,9,10,11", finder), con_db);
                    List<UncomparedLS> list = new List<UncomparedLS>();

                    var town = "";
                    var rajon = "";
                    var ulica = "";
                    var ndom = "";
                    var nkor = "";
                    var kvar = "";
                    var kom = "";
                    var nzp_dom = "";
                    var nzp_kvar = "";
                    var ls = "";
                    var fam = "";
                    var ima = "";
                    var otch = "";
                    var fio = "";

                    for (int i = 0; i < dt.resultData.Rows.Count; i++)
                    {
                        town = rajon = ulica = ndom = nkor = kvar = kom = "";
                        nzp_dom = nzp_kvar = "";
                        ls = fam = ima = otch = fio = "";

                        if (dt.resultData.Rows[i]["rajon"] != DBNull.Value) rajon = dt.resultData.Rows[i]["rajon"].ToString().Trim();
                        if (dt.resultData.Rows[i]["town"] != DBNull.Value) town = dt.resultData.Rows[i]["town"].ToString().Trim();
                        if (dt.resultData.Rows[i]["ulica"] != DBNull.Value) ulica = dt.resultData.Rows[i]["ulica"].ToString().Trim();
                        if (dt.resultData.Rows[i]["ndom"] != DBNull.Value) ndom = dt.resultData.Rows[i]["ndom"].ToString().Trim();
                        if (dt.resultData.Rows[i]["nkor"] != DBNull.Value) nkor = dt.resultData.Rows[i]["nkor"].ToString().Trim();
                        if (dt.resultData.Rows[i]["kvar"] != DBNull.Value) kvar = dt.resultData.Rows[i]["kvar"].ToString().Trim();
                        if (dt.resultData.Rows[i]["kom"] != DBNull.Value) kom = dt.resultData.Rows[i]["kom"].ToString().Trim();

                        if (dt.resultData.Rows[i]["ls"] != DBNull.Value) ls = dt.resultData.Rows[i]["ls"].ToString().Trim();
                        if (dt.resultData.Rows[i]["fam"] != DBNull.Value) fam = dt.resultData.Rows[i]["fam"].ToString().Trim();
                        if (dt.resultData.Rows[i]["ima"] != DBNull.Value) ima = dt.resultData.Rows[i]["ima"].ToString().Trim();
                        if (dt.resultData.Rows[i]["otch"] != DBNull.Value) otch = dt.resultData.Rows[i]["otch"].ToString().Trim();

                        if (dt.resultData.Rows[i]["nzp_dom"] != DBNull.Value) nzp_dom = dt.resultData.Rows[i]["nzp_dom"].ToString().Trim();
                        if (dt.resultData.Rows[i]["nzp_kvar"] != DBNull.Value) nzp_kvar = dt.resultData.Rows[i]["nzp_kvar"].ToString().Trim();

                        fio = GetFio(fam, ima, otch);
                        if (fio != "") fio = ", " + fio;

                        list.Add(new UncomparedLS()
                        {
                            show_data = ls + fio + " (" + GetAddress(town, rajon, ulica, ndom, nkor, kvar, kom),
                            dom = GetHouse(ndom, nkor),
                            nzp_town = dt.resultData.Rows[i]["nzp_town"].ToString().Trim(),
                            nzp_raj = dt.resultData.Rows[i]["nzp_raj"].ToString().Trim(),
                            nzp_ul = dt.resultData.Rows[i]["nzp_ul"].ToString().Trim(),
                            nzp_dom = nzp_dom,
                            nzp_kvar = nzp_kvar,
                            kvar = kvar
                        });
                    }

                    ret = new ReturnsObjectType<List<UncomparedLS>>() { returnsData = list, tag = GetRecordCount(sql, con_db), result = true, text = "Выполнено" };
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка выполнения процедуры GetUncomparedLS : " + ex.Message, MonitorLog.typelog.Error, true);
                    ret = new ReturnsObjectType<List<UncomparedLS>>() { returnsData = null, result = false, text = "Ошибка выполнения" };
                }
            }

            return ret;
        }


        /// <summary>
        /// получение несопоставленных юридических лиц
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<List<UncomparedPayer>> GetUncomparedPayer(FilesImported finder)
        {
            ReturnsObjectType<List<UncomparedPayer>> ret = new ReturnsObjectType<List<UncomparedPayer>>();

            using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            {
                try
                {
                    var t = OpenDb(con_db, true);
                    if (!t.result) throw new Exception(t.text);

                    string sql = "select distinct upper(urlic_name) as urlic_name " +
                        " from " + Points.Pref + DBManager.sUploadAliasRest + "file_urlic " +
                        " where nzp_payer is null " +
                        WhereNzpFile(finder.selectedFiles);

                    var dt = ClassDBUtils.OpenSQL(SetLimitOffset(sql + " order by 1", finder), con_db);
                    List<UncomparedPayer> list = new List<UncomparedPayer>();
                    for (int i = 0; i < dt.resultData.Rows.Count; i++)
                    {
                        list.Add(new UncomparedPayer()
                        {
                            payer = dt.resultData.Rows[i]["urlic_name"].ToString().Trim()
                        });
                    }

                    ret = new ReturnsObjectType<List<UncomparedPayer>>() { returnsData = list, tag = GetRecordCount(sql, con_db), result = true, text = "Выполнено" };
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка выполнения процедуры GetUncomparedPayer : " + ex.Message, MonitorLog.typelog.Error, true);
                    ret = new ReturnsObjectType<List<UncomparedPayer>>() { returnsData = null, result = false, text = "Ошибка выполнения" };
                }
            }

            return ret;
        }
    }
}
