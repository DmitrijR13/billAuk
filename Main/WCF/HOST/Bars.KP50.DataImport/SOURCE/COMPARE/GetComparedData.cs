
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
    public class GetComparedData : SelectedFiles
    {
        /// <summary>
        /// получение сопоставленных городов
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<List<ComparedAreas>> GetComparedArea(FilesImported finder)
        {
            ReturnsObjectType<List<ComparedAreas>> ret;
            using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            {
                try
                {
                    Returns t = OpenDb(con_db, true);
                    if (!t.result) throw new Exception(t.text);

                    string sql =
                        " select distinct upper(f.area) as area_file, upper(b.area) as area_base, f.nzp_area " +
                        " from " + Points.Pref + DBManager.sUploadAliasRest + "file_area f, " +
                            Points.Pref + DBManager.sDataAliasRest + "s_area b " +
                        " where f.nzp_area = b.nzp_area " +
                        "   and f.nzp_area is not null " +
                        WhereNzpFile(finder.selectedFiles);

                    var dt = ClassDBUtils.OpenSQL(SetLimitOffset(sql + " order by 1,2 ", finder), con_db);
                    List<ComparedAreas> list = new List<ComparedAreas>();

                    for (int i = 0; i < dt.resultData.Rows.Count; i++)
                    {
                        list.Add(new ComparedAreas()
                        {
                            area_base = dt.resultData.Rows[i]["area_base"].ToString().Trim(),
                            area_file = dt.resultData.Rows[i]["area_file"].ToString().Trim(),
                            nzp_area = dt.resultData.Rows[i]["nzp_area"].ToString().Trim()
                        });
                    }

                    ret = new ReturnsObjectType<List<ComparedAreas>>() { returnsData = list, tag = GetRecordCount(sql, con_db), result = true, text = "Выполнено" }; 
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка выполнения процедуры GetComparedArea : " + ex.Message, MonitorLog.typelog.Error, true);
                    ret = new ReturnsObjectType<List<ComparedAreas>>() { returnsData = null, result = false, text = "Ошибка выполнения" }; 
                }
            }

            return ret;
        }

        /// <summary>
        /// получение сопоставленных городов
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<List<ComparedSupps>> GetComparedSupp(FilesImported finder)
        {
            ReturnsObjectType<List<ComparedSupps>> ret;

            using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            {
                try
                {
                    Returns t = OpenDb(con_db, true);
                    if (!t.result) throw new Exception(t.text);

                    string sql = "select distinct upper(f.supp_name) as supp_file, upper(b.name_supp) as supp_base, f.nzp_supp " +
                        " from " + Points.Pref + DBManager.sUploadAliasRest + "file_supp f, " + 
                        finder.bank + DBManager.sKernelAliasRest + "supplier b " +
                        " where f.nzp_supp = b.nzp_supp and f.nzp_supp is not null " +
                        WhereNzpFile(finder.selectedFiles);

                    var dt = ClassDBUtils.OpenSQL(SetLimitOffset(sql + " order by 1,2 ", finder), con_db);
                    List<ComparedSupps> list = new List<ComparedSupps>();
                    for (int i = 0; i < dt.resultData.Rows.Count; i++)
                    {
                        list.Add(new ComparedSupps()
                        {
                            supp_base = dt.resultData.Rows[i]["supp_base"].ToString().Trim(),
                            supp_file = dt.resultData.Rows[i]["supp_file"].ToString().Trim(),
                            nzp_supp = dt.resultData.Rows[i]["nzp_supp"].ToString().Trim()
                        });
                    }

                    ret = new ReturnsObjectType<List<ComparedSupps>>() { returnsData = list, tag = GetRecordCount(sql, con_db), result = true, text = "Выполнено" }; 
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка выполнения процедуры GetComparedSupp : " + ex.Message, MonitorLog.typelog.Error, true);
                    ret = new ReturnsObjectType<List<ComparedSupps>>() { returnsData = null, result = false, text = "Ошибка выполнения" }; 
                }
            }

            return ret;
        }

        /// <summary>
        /// получение сопоставленных МО
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<List<ComparedVills>> GetComparedMO(FilesImported finder)
        {
            ReturnsObjectType<List<ComparedVills>> ret;

            using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            {
                try
                {
                    Returns t = OpenDb(con_db, true);
                    if (!t.result) throw new Exception(t.text);

                    string sql =
                        "select distinct upper(f.vill) as vill_file, upper(b.vill) as vill_base, f.nzp_vill " +
                        " from " + Points.Pref + DBManager.sUploadAliasRest + "file_mo f, " + finder.bank + DBManager.sKernelAliasRest + "s_vill b " +
                        " where f.nzp_vill = b.nzp_vill and f.nzp_vill is not null" +
                        WhereNzpFile(finder.selectedFiles);

                    var dt = ClassDBUtils.OpenSQL(SetLimitOffset(sql + " order by 1,2 ", finder), con_db);
                    List<ComparedVills> list = new List<ComparedVills>();
                    
                    for (int i = 0; i < dt.resultData.Rows.Count; i++)
                    {
                        list.Add(new ComparedVills()
                        {
                            vill_base = dt.resultData.Rows[i]["vill_base"].ToString().Trim(),
                            vill_file = dt.resultData.Rows[i]["vill_file"].ToString().Trim(),
                            nzp_vill = dt.resultData.Rows[i]["nzp_vill"].ToString().Trim()
                        });
                    }

                    ret = new ReturnsObjectType<List<ComparedVills>>() { returnsData = list, tag = GetRecordCount(sql, con_db), result = true, text = "Выполнено" };
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка выполнения процедуры GetComparedMO : " + ex.Message, MonitorLog.typelog.Error, true);
                    ret = new ReturnsObjectType<List<ComparedVills>>() { returnsData = null, result = false, text = "Ошибка выполнения" }; 
                }
            }

            return ret;
        }

        /// <summary>
        /// получение сопоставленных счетчиков
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<List<ComparedServs>> GetComparedServ(FilesImported finder)
        {
            ReturnsObjectType<List<ComparedServs>> ret;

            using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            {
                try
                {
                    Returns t = OpenDb(con_db, true);
                    if (!t.result) throw new Exception(t.text);

                    string sql =
                        "select distinct upper(f.service) as serv_file, upper(b.service) as serv_base, f.nzp_serv " +
                        " from " + Points.Pref + DBManager.sUploadAliasRest + "file_services f, " +
                        Points.Pref + sKernelAliasRest + "services b " +
                        " where f.nzp_serv = b.nzp_serv and f.nzp_serv is not null " +
                        WhereNzpFile(finder.selectedFiles);

                    var dt = ClassDBUtils.OpenSQL(SetLimitOffset(sql + " order by 1,2 ", finder), con_db);
                    List<ComparedServs> list = new List<ComparedServs>();
                    
                    for (int i = 0; i < dt.resultData.Rows.Count; i++)
                    {
                        list.Add(new ComparedServs()
                        {
                            serv_base = dt.resultData.Rows[i]["serv_base"].ToString().Trim(),
                            serv_file = dt.resultData.Rows[i]["serv_file"].ToString().Trim(),
                            nzp_serv = dt.resultData.Rows[i]["nzp_serv"].ToString().Trim()
                        });
                    }

                    ret = new ReturnsObjectType<List<ComparedServs>>() { returnsData = list, tag = GetRecordCount(sql, con_db), result = true, text = "Выполнено" };
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка выполнения процедуры GetComparedServ : " + ex.Message, MonitorLog.typelog.Error, true);
                    ret = new ReturnsObjectType<List<ComparedServs>>() { returnsData = null, result = false, text = "Ошибка выполнения" };
                }
            }
            
            return ret;
        }

        /// <summary>
        /// получение сопоставленных единиц измерения
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<List<ComparedMeasures>> GetComparedMeasure(FilesImported finder)
        {
            ReturnsObjectType<List<ComparedMeasures>> ret;

            using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            {
                try
                {
                    Returns t = OpenDb(con_db, true);
                    if (!t.result) throw new Exception(t.text);

                    string sql =
                        "select distinct upper(f.measure) as measure_file, upper(b.measure_long) as measure_base, f.nzp_measure " +
                        " from " + Points.Pref + DBManager.sUploadAliasRest + "file_measures f, " +
                        finder.bank + DBManager.sKernelAliasRest + "s_measure b " +
                        " where f.nzp_measure = b.nzp_measure and f.nzp_measure is not null " +
                        WhereNzpFile(finder.selectedFiles);
                    
                    var dt = ClassDBUtils.OpenSQL(SetLimitOffset(sql + " order by 1,2 ", finder), con_db);
                    List<ComparedMeasures> list = new List<ComparedMeasures>();
                    for (int i = 0; i < dt.resultData.Rows.Count; i++)
                    {
                        list.Add(new ComparedMeasures()
                        {
                            measure_base = dt.resultData.Rows[i]["measure_base"].ToString().Trim(),
                            measure_file = dt.resultData.Rows[i]["measure_file"].ToString().Trim(),
                            nzp_measure = dt.resultData.Rows[i]["nzp_measure"].ToString().Trim()
                        });
                    }

                    ret = new ReturnsObjectType<List<ComparedMeasures>>() { returnsData = list, tag = GetRecordCount(sql, con_db), result = true, text = "Выполнено" };
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка выполнения процедуры GetComparedMesaure : " + ex.Message, MonitorLog.typelog.Error, true);
                    ret = new ReturnsObjectType<List<ComparedMeasures>>() { returnsData = null, result = false, text = "Ошибка выполнения" };
                }
            }

            return ret;
        }

        /// <summary>
        /// получение сопоставленных типов доп. параметров
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<List<ComparedParTypes>> GetComparedParType(FilesImported finder)
        {
            ReturnsObjectType<List<ComparedParTypes>> ret;

            using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            { 
                try
                {
                    Returns t = OpenDb(con_db, true);
                    if (!t.result) throw new Exception(t.text);

                    string sql =
                        "select distinct upper(a.prm_name) as name_prm_file, upper(name_prm) as name_prm_base, a.nzp_prm as id_prm " +
                        " from " + Points.Pref + DBManager.sUploadAliasRest + "file_typeparams a, " + finder.bank + DBManager.sKernelAliasRest + "prm_name b " +
                        " where a.nzp_prm = b.nzp_prm and a.nzp_prm is not null " +
                        WhereNzpFile(finder.selectedFiles);

                    var dt = ClassDBUtils.OpenSQL(SetLimitOffset(sql + " order by 1,2 ", finder), con_db);
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

                    ret = new ReturnsObjectType<List<ComparedParTypes>>() { returnsData = list, tag = GetRecordCount(sql, con_db), result = true, text = "Выполнено" };
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка выполнения процедуры GetComparedParType : " + ex.Message, MonitorLog.typelog.Error, true);
                    ret = new ReturnsObjectType<List<ComparedParTypes>>() { returnsData = null, result = false, text = "Ошибка выполнения" };
                }
            }

            return ret;
        }

        /// <summary>
        /// получение сопоставленных параметров
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<List<ComparedParTypes>> GetComparedParameters(FilesImported finder, string bufferParTable)
        {
            ReturnsObjectType<List<ComparedParTypes>> ret;

            using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            { 
                try
                {
                    Returns t = OpenDb(con_db, true);
                    if (!t.result) throw new Exception(t.text);

                    string sql =
                        "select distinct upper(f.name) as name_prm_file, upper(b.name_prm) as name_prm_base, f.nzp_prm " +
                        " from " + Points.Pref + DBManager.sUploadAliasRest + bufferParTable + " f, " + 
                        finder.bank + DBManager.sKernelAliasRest + "prm_name b " +
                        " where f.nzp_prm = b.nzp_prm and f.nzp_prm is not null " +
                        WhereNzpFile(finder.selectedFiles);

                    var dt = ClassDBUtils.OpenSQL(SetLimitOffset(sql + " order by 1,2 ", finder), con_db);
                    List<ComparedParTypes> list = new List<ComparedParTypes>();
                    for (int i = 0; i < dt.resultData.Rows.Count; i++)
                    {
                        list.Add(new ComparedParTypes()
                        {
                            name_prm_file = dt.resultData.Rows[i]["name_prm_file"].ToString().Trim(),
                            name_prm_base = dt.resultData.Rows[i]["name_prm_base"].ToString().Trim(),
                            nzp_prm = dt.resultData.Rows[i]["nzp_prm"].ToString().Trim()
                        });
                    }

                    ret = new ReturnsObjectType<List<ComparedParTypes>>() { returnsData = list, tag = GetRecordCount(sql, con_db), result = true, text = "Выполнено" };
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка выполнения процедуры GetComparedParBlag : " + ex.Message, MonitorLog.typelog.Error, true);
                    ret = new ReturnsObjectType<List<ComparedParTypes>>() { returnsData = null, result = false, text = "Ошибка выполнения" };
                }
            }

            return ret;
        }

        /// <summary>
        /// получение сопоставленных домов по благоустройству
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<List<ComparedParTypes>> GetComparedParBlag(FilesImported finder)
        {
            return GetComparedParameters(finder, "file_blag");
        }

        /// <summary>
        /// получение сопоставленных домов по газу
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<List<ComparedParTypes>> GetComparedParGas(FilesImported finder)
        {
            return GetComparedParameters(finder, "file_gaz");
        }

        /// <summary>
        /// получение сопоставленных домов по воде
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<List<ComparedParTypes>> GetComparedParWater(FilesImported finder)
        {
            return GetComparedParameters(finder, "file_voda");
        }

        /// <summary>
        /// получение сопоставленных городов
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<List<ComparedTowns>> GetComparedTown(FilesImported finder)
        {
            var ret = new ReturnsObjectType<List<ComparedTowns>>();

            using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            { 
                try
                {
                    Returns t = OpenDb(con_db, true);
                    if (!t.result) throw new Exception(t.text);

                    string sql = "select distinct upper(f.town) as town_file, upper(town.town) as town_base, f.nzp_town " +
                        " from " + Points.Pref + DBManager.sUploadAliasRest + "file_dom f, " + 
                        Points.Pref + DBManager.sDataAliasRest + "s_town town " +
                        " where f.nzp_town = town.nzp_town and f.nzp_town is not null " +
                        WhereNzpFile(finder.selectedFiles);

                    var dt = ClassDBUtils.OpenSQL(SetLimitOffset(sql + " order by 1,2 ", finder), con_db);
                    List<ComparedTowns> list = new List<ComparedTowns>();
                    for (int i = 0; i < dt.resultData.Rows.Count; i++)
                    {
                        list.Add(new ComparedTowns()
                        {
                            town_base = dt.resultData.Rows[i]["town_base"].ToString().Trim(),
                            town_file = dt.resultData.Rows[i]["town_file"].ToString().Trim(),
                            nzp_town = dt.resultData.Rows[i]["nzp_town"].ToString().Trim()
                        });
                    }

                    ret = new ReturnsObjectType<List<ComparedTowns>>() { returnsData = list, tag = GetRecordCount(sql, con_db), result = true, text = "Выполнено" };
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка выполнения процедуры GetComparedTown : " + ex.Message, MonitorLog.typelog.Error, true);
                    ret = new ReturnsObjectType<List<ComparedTowns>>() { returnsData = null, result = false, text = "Ошибка выполнения" };
                }
            }

            return ret;
        }

        /// <summary>
        /// получение сопоставленных населенных пунктов
        /// </summary>
        /// <returns>результат</returns>
        public ReturnsObjectType<List<ComparedRajons>> GetComparedRajon(FilesImported finder)
        {
            var ret = new ReturnsObjectType<List<ComparedRajons>>();

            using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            { 
                try
                {
                    Returns t = OpenDb(con_db, true);
                    if (!t.result) throw new Exception(t.text);
                
                    string sql = " select distinct  " +
                        "   upper(f.rajon) as rajon_file,   upper(f.town) as town_file, " +
                        "   upper(raj.rajon) as rajon_base, upper(town.town) as town_base, f.nzp_raj " +
                        " from " + 
                            Points.Pref + DBManager.sUploadAliasRest + "file_dom f, " + 
                            Points.Pref + "_data" + tableDelimiter + "s_rajon raj, " + 
                            Points.Pref + "_data" + tableDelimiter + "s_town town " +
                        " where f.nzp_raj = raj.nzp_raj " + 
                        "   and raj.nzp_town = town.nzp_town " + 
                        "   and f.nzp_raj is not null  " +
                        WhereNzpFile(finder.selectedFiles);

                    var dt = ClassDBUtils.OpenSQL(SetLimitOffset(sql + " order by 1,2 ", finder), con_db);
                    List<ComparedRajons> list = new List<ComparedRajons>();
                    for (int i = 0; i < dt.resultData.Rows.Count; i++)
                    {
                        var town_file = "";
                        var town_base = "";

                        if (dt.resultData.Rows[i]["town_file"] != DBNull.Value) town_file = dt.resultData.Rows[i]["town_file"].ToString().Trim();
                        if (dt.resultData.Rows[i]["town_base"] != DBNull.Value) town_base = dt.resultData.Rows[i]["town_base"].ToString().Trim();

                        list.Add(new ComparedRajons()
                        {
                            rajon_base = dt.resultData.Rows[i]["rajon_base"].ToString().Trim() + " (" + GetAddress(town_base) + ")",
                            rajon_file = dt.resultData.Rows[i]["rajon_file"].ToString().Trim() + " (" + GetAddress(town_file) + ")",
                            nzp_raj = dt.resultData.Rows[i]["nzp_raj"].ToString().Trim()
                        });
                    }

                    ret = new ReturnsObjectType<List<ComparedRajons>>() { returnsData = list, tag = GetRecordCount(sql, con_db), result = true, text = "Выполнено" };
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка выполнения процедуры GetComparedRajon : " + ex.Message, MonitorLog.typelog.Error, true);
                    ret = new ReturnsObjectType<List<ComparedRajons>>() { returnsData = null, result = false, text = "Ошибка выполнения" };
                }
            }

            return ret;
        }

        /// <summary>
        /// получение сопоставленных улиц
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<List<ComparedStreets>> GetComparedStreets(FilesImported finder)
        {
            var ret = new ReturnsObjectType<List<ComparedStreets>>();

            using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            { 
                try
                {
                    Returns t = OpenDb(con_db, true);
                    if (!t.result) throw new Exception(t.text);

                    string sql = " select distinct " +
                        "   upper(f.ulica) as ulica_file, upper(f.town) as town_file,    upper(f.rajon) as rajon_file,  " +
                        "   upper(b.ulica) as ulica_base, upper(town.town) as town_base, upper(raj.rajon) as rajon_base,  " +
                        "   b.nzp_ul  " +
                        " from " + Points.Pref + DBManager.sUploadAliasRest + "file_dom f, " +
                        Points.Pref + DBManager.sDataAliasRest + "s_ulica b, " +
                        Points.Pref + DBManager.sDataAliasRest + "s_rajon raj, " +
                        Points.Pref + "_data" + tableDelimiter + "s_town town " +
                        " where b.nzp_raj = raj.nzp_raj " + 
                        " and raj.nzp_town = town.nzp_town " + 
                        " and f.nzp_ul = b.nzp_ul " + 
                        " and f.nzp_ul is not null " +
                        WhereNzpFile(finder.selectedFiles);

                    var dt = ClassDBUtils.OpenSQL(SetLimitOffset(sql + " order by 1,2,3", finder), con_db);
                    List<ComparedStreets> list = new List<ComparedStreets>();
                    for (int i = 0; i < dt.resultData.Rows.Count; i++)
                    {
                        var town_file = "";
                        var rajon_file = "";
                        var town_base = "";
                        var rajon_base = "";

                        if (dt.resultData.Rows[i]["town_file"] != DBNull.Value) town_file = dt.resultData.Rows[i]["town_file"].ToString().Trim();
                        if (dt.resultData.Rows[i]["rajon_file"] != DBNull.Value) rajon_file = dt.resultData.Rows[i]["rajon_file"].ToString().Trim();

                        if (dt.resultData.Rows[i]["town_base"] != DBNull.Value) town_base = dt.resultData.Rows[i]["town_base"].ToString().Trim();
                        if (dt.resultData.Rows[i]["rajon_base"] != DBNull.Value) rajon_base = dt.resultData.Rows[i]["rajon_base"].ToString().Trim();
                    
                        list.Add(new ComparedStreets()
                        {
                            ulica_base = dt.resultData.Rows[i]["ulica_base"].ToString().Trim() + " (" + GetAddress(town_base, rajon_base) + ")",
                            ulica_file = dt.resultData.Rows[i]["ulica_file"].ToString().Trim() + " (" + GetAddress(town_file, rajon_file) + ")",
                            nzp_ul = dt.resultData.Rows[i]["nzp_ul"].ToString().Trim()
                        });
                    }

                    ret = new ReturnsObjectType<List<ComparedStreets>>() { returnsData = list, tag = GetRecordCount(sql, con_db), result = true, text = "Выполнено" };
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка выполнения процедуры GetComparedStreets : " + ex.Message, MonitorLog.typelog.Error, true);
                    ret = new ReturnsObjectType<List<ComparedStreets>>() { returnsData = null, result = false, text = "Ошибка выполнения" };
                }
            }

            return ret;
        }

        /// <summary>
        /// получение сопоставленных домов
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<List<ComparedHouses>> GetComparedHouse(FilesImported finder)
        {
            var ret = new ReturnsObjectType<List<ComparedHouses>>();

            using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            { 
                try
                {
                    Returns t = OpenDb(con_db, true);
                    if (!t.result) throw new Exception(t.text);

                    string sql = "select " +
                        " upper(f.ndom) as ndom_file, upper(f.nkor) as nkor_file, upper(f.ulica) as ulica_file,  upper(f.town) as town_file,    upper(f.rajon) as rajon_file,  " +
                        " upper(b.ndom) as ndom_base, upper(b.nkor) as nkor_base, upper(ul.ulica) as ulica_base, upper(town.town) as town_base, upper(raj.rajon) as rajon_base,   " +
                        " b.nzp_dom " +
                        " from " + Points.Pref + DBManager.sUploadAliasRest + "file_dom f, " +
                        Points.Pref + DBManager.sDataAliasRest + "dom b, " +
                        Points.Pref + DBManager.sDataAliasRest + "s_ulica ul, " +
                        Points.Pref + DBManager.sDataAliasRest + "s_rajon raj, " +
                        Points.Pref + DBManager.sDataAliasRest + "s_town town " +
                        " where b.nzp_ul = ul.nzp_ul and ul.nzp_raj = raj.nzp_raj " +
                        " and raj.nzp_town = town.nzp_town " +
                        " and f.nzp_dom = b.nzp_dom and f.nzp_dom is not null " +
                        WhereNzpFile(finder.selectedFiles);

                    var dt = ClassDBUtils.OpenSQL(SetLimitOffset(sql + " order by 1,2,3,4,5 ", finder), con_db);
                    List<ComparedHouses> list = new List<ComparedHouses>();
                    for (int i = 0; i < dt.resultData.Rows.Count; i++)
                    {
                        var town_file = "";
                        var rajon_file = "";
                        var ulica_file = "";
                        var ndom_file = "";
                        var nkor_file = "";
                        
                        if (dt.resultData.Rows[i]["ndom_file"] != DBNull.Value) ndom_file = dt.resultData.Rows[i]["ndom_file"].ToString().Trim();
                        if (dt.resultData.Rows[i]["nkor_file"] != DBNull.Value) nkor_file = dt.resultData.Rows[i]["nkor_file"].ToString().Trim();
                        if (dt.resultData.Rows[i]["ulica_file"] != DBNull.Value) ulica_file = dt.resultData.Rows[i]["ulica_file"].ToString().Trim();
                        if (dt.resultData.Rows[i]["rajon_file"] != DBNull.Value) rajon_file = dt.resultData.Rows[i]["rajon_file"].ToString().Trim();
                        if (dt.resultData.Rows[i]["town_file"] != DBNull.Value) town_file = dt.resultData.Rows[i]["town_file"].ToString().Trim();

                        var town_base = "";
                        var rajon_base = "";
                        var ulica_base = "";
                        var ndom_base = "";
                        var nkor_base = "";

                        if (dt.resultData.Rows[i]["ndom_base"] != DBNull.Value) ndom_base = dt.resultData.Rows[i]["ndom_base"].ToString().Trim();
                        if (dt.resultData.Rows[i]["nkor_base"] != DBNull.Value) nkor_base = dt.resultData.Rows[i]["nkor_base"].ToString().Trim();
                        if (dt.resultData.Rows[i]["town_base"] != DBNull.Value) town_base = dt.resultData.Rows[i]["town_base"].ToString();
                        if (dt.resultData.Rows[i]["rajon_base"] != DBNull.Value) rajon_base = dt.resultData.Rows[i]["rajon_base"].ToString();
                        if (dt.resultData.Rows[i]["ulica_base"] != DBNull.Value) ulica_base = dt.resultData.Rows[i]["ulica_base"].ToString().Trim();

                        // GetHouse

                        list.Add(new ComparedHouses()
                        {
                            dom_base = GetHouse(ndom_base, nkor_base) + " (" + GetHouseAddress(ulica_base, town_base, rajon_base) + ")",
                            dom_file = GetHouse(ndom_file, nkor_file) + " (" + GetHouseAddress(ulica_file, town_file, rajon_file) + ")",
                            nzp_dom = dt.resultData.Rows[i]["nzp_dom"].ToString().Trim()
                        });
                    }

                    ret = new ReturnsObjectType<List<ComparedHouses>>() { returnsData = list, tag = GetRecordCount(sql, con_db), result = true, text = "Выполнено" };  
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка выполнения процедуры GetComparedHouse : " + ex.Message, MonitorLog.typelog.Error, true);
                    ret = new ReturnsObjectType<List<ComparedHouses>>() { returnsData = null, result = false, text = "Ошибка выполнения" };
                }
            }

            return ret;
        }

        /// <summary>
        /// получение сопоставленных лицевых счетов
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<List<ComparedLS>> GetComparedLS(FilesImported finder)
        {
            var ret = new ReturnsObjectType<List<ComparedLS>>();

            using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            {
                try
                {
                    Returns t = OpenDb(con_db, true);
                    if (!t.result) throw new Exception(t.text);

                    string sql = "select distinct " + 
                        " fk.id as ls_file, upper(fk.fam) as fam_file, upper(fk.ima) as ima_file, upper(fk.otch) as otch_file, " +
                        " upper(f.town) as town_file,    upper(f.rajon) as rajon_file,   upper(f.ulica) as ulica_file,   upper(f.ndom) as ndom_file, upper(f.nkor) as nkor_file, " +  
                        " fk.nkvar as nkvar_file,        fk.nkvar_n as kom_file,    " + 
                        
                        " kvar.num_ls as ls_base, upper(kvar.fio) as fio_base, " +
                        " upper(town.town) as town_base, upper(raj.rajon) as rajon_base, upper(ul.ulica) as ulica_base,  upper(b.ndom) as ndom_base, upper(b.nkor) as nkor_base, " + 
                        " kvar.nkvar as nkvar_base,      kvar.nkvar_n as kom_base,  " + 
                        
                        " b.nzp_dom, fk.nzp_kvar as nzp_kvar " +
                        " from " + Points.Pref + DBManager.sUploadAliasRest + "file_dom f, " +
                        Points.Pref + DBManager.sDataAliasRest + "dom b, " +
                        Points.Pref + DBManager.sDataAliasRest + "s_ulica ul, " +
                        Points.Pref + DBManager.sDataAliasRest + "s_rajon raj, " +
                        Points.Pref + DBManager.sDataAliasRest + "s_town town, " +
                        Points.Pref + DBManager.sUploadAliasRest + "file_kvar fk, " +
                        Points.Pref + DBManager.sDataAliasRest + "kvar kvar" +
                        " where b.nzp_ul = ul.nzp_ul " + 
                        "   and ul.nzp_raj = raj.nzp_raj " +
                        "   and raj.nzp_town = town.nzp_town " + 
                        "   and f.nzp_dom = b.nzp_dom " + 
                        "   and f.nzp_dom is not null " + 
                        "   and kvar.nzp_kvar = fk.nzp_kvar " + 
                        "   and fk.nzp_kvar is not null " +
                        "   and fk.nzp_dom = f.nzp_dom " + 
                        "   and fk.nzp_file = f.nzp_file " +
                        WhereNzpFile(finder.selectedFiles, "fk");

                    var town_file = "";
                    var rajon_file = "";
                    var ulica_file = "";
                    var ndom_file = "";
                    var nkor_file = "";
                    var nkvar_file = "";
                    var kom_file = "";

                    var town_base = "";
                    var rajon_base = "";
                    var ulica_base = "";
                    var ndom_base = "";
                    var nkor_base = "";
                    var nkvar_base = "";
                    var kom_base = "";

                    var fam_file = "";
                    var ima_file = "";
                    var otch_file = "";

                    var fio_base = "";
                    var fio_file = "";

                    var ls_file = "";
                    var ls_base = "";

                    var dt = ClassDBUtils.OpenSQL(SetLimitOffset(sql + " order by 1,2,3,4,5,6,7,8,9,10,11 ", finder), con_db);
                    List<ComparedLS> list = new List<ComparedLS>();
                    for (int i = 0; i < dt.resultData.Rows.Count; i++)
                    {
                        town_file = rajon_file = ulica_file = ndom_file = nkor_file = nkvar_file = kom_file = "";
                        town_base = rajon_base = ulica_base = ndom_base = nkor_base = nkvar_base = kom_base = "";
                        fam_file = ima_file = otch_file = fio_file = fio_base = ls_file = ls_base = "";
                        
                        if (dt.resultData.Rows[i]["town_file"] != DBNull.Value) town_file = dt.resultData.Rows[i]["town_file"].ToString();
                        if (dt.resultData.Rows[i]["rajon_file"] != DBNull.Value) rajon_file = dt.resultData.Rows[i]["rajon_file"].ToString();
                        if (dt.resultData.Rows[i]["ulica_file"] != DBNull.Value) ulica_file = dt.resultData.Rows[i]["ulica_file"].ToString();
                        if (dt.resultData.Rows[i]["ndom_file"] != DBNull.Value) ndom_file = dt.resultData.Rows[i]["ndom_file"].ToString();
                        if (dt.resultData.Rows[i]["nkor_file"] != DBNull.Value) nkor_file = dt.resultData.Rows[i]["nkor_file"].ToString();
                        if (dt.resultData.Rows[i]["nkvar_file"] != DBNull.Value) nkvar_file = dt.resultData.Rows[i]["nkvar_file"].ToString().Trim();
                        if (dt.resultData.Rows[i]["kom_file"] != DBNull.Value) kom_file = dt.resultData.Rows[i]["kom_file"].ToString().Trim();

                        if (dt.resultData.Rows[i]["town_base"] != DBNull.Value) town_base = dt.resultData.Rows[i]["town_base"].ToString();
                        if (dt.resultData.Rows[i]["rajon_base"] != DBNull.Value) rajon_base = dt.resultData.Rows[i]["rajon_base"].ToString();
                        if (dt.resultData.Rows[i]["ulica_base"] != DBNull.Value) ulica_base = dt.resultData.Rows[i]["ulica_base"].ToString();
                        if (dt.resultData.Rows[i]["ndom_base"] != DBNull.Value) ndom_base = dt.resultData.Rows[i]["ndom_base"].ToString();
                        if (dt.resultData.Rows[i]["nkor_base"] != DBNull.Value) nkor_base = dt.resultData.Rows[i]["nkor_base"].ToString();
                        if (dt.resultData.Rows[i]["nkvar_base"] != DBNull.Value) nkvar_base = dt.resultData.Rows[i]["nkvar_base"].ToString().Trim();
                        if (dt.resultData.Rows[i]["kom_base"] != DBNull.Value) kom_base = dt.resultData.Rows[i]["kom_base"].ToString().Trim();
                        
                        if (dt.resultData.Rows[i]["fam_file"] != DBNull.Value) fam_file = dt.resultData.Rows[i]["fam_file"].ToString().Trim();
                        if (dt.resultData.Rows[i]["ima_file"] != DBNull.Value) ima_file = dt.resultData.Rows[i]["ima_file"].ToString().Trim();
                        if (dt.resultData.Rows[i]["otch_file"] != DBNull.Value) otch_file = dt.resultData.Rows[i]["otch_file"].ToString().Trim();
                        if (dt.resultData.Rows[i]["fio_base"] != DBNull.Value) fio_base = ", " + dt.resultData.Rows[i]["fio_base"].ToString().Trim();
                    
                        if (dt.resultData.Rows[i]["ls_file"] != DBNull.Value) ls_file = dt.resultData.Rows[i]["ls_file"].ToString().Trim();
                        if (dt.resultData.Rows[i]["ls_base"] != DBNull.Value) ls_base = dt.resultData.Rows[i]["ls_base"].ToString().Trim();

                        fio_file = GetFio(fam_file, ima_file, otch_file);
                        if (fio_file != "") fio_file = ", " + fio_file;

                        #region Состояния ЛС
                        string ls_state = "";

                        sql =
                            " SELECT val_prm as value " +
                            " FROM " + finder.bank + DBManager.sDataAliasRest + "prm_3 " +
                            " WHERE nzp_prm = 51 " +
                            " AND nzp = " + dt.resultData.Rows[i]["nzp_kvar"];
                        var dt1 = ClassDBUtils.OpenSQL(sql, con_db);
                        if (dt1.resultData.Rows.Count != 0)
                        {
                            if (dt1.resultData.Rows[0]["value"] != DBNull.Value)
                                ls_state = dt1.resultData.Rows[0]["value"].ToString().Trim() == "1"
                                    ? "ОТКРЫТ"
                                    : dt1.resultData.Rows[0]["value"].ToString().Trim() == "2" ? "ЗАКРЫТ" : "НЕОПР.";
                            else ls_state = "";
                        }
                        #endregion Состояния ЛС
                        
                        list.Add(new ComparedLS()
                        {
                            kvar_base = ls_base + fio_base + " (" + GetAddress(town_base, rajon_base, ulica_base, ndom_base, nkor_base, nkvar_base, kom_base) + ") " + ls_state,
                            kvar_file = ls_file + fio_file + " (" + GetAddress(town_file, rajon_file, ulica_file, ndom_file, nkor_file, nkvar_file, kom_file) + ")",
                            nzp_kvar = dt.resultData.Rows[i]["nzp_kvar"].ToString().Trim()
                        });
                    }
                    ret = new ReturnsObjectType<List<ComparedLS>>() { returnsData = list, tag = GetRecordCount(sql, con_db), result = true, text = "Выполнено" }; 
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка выполнения процедуры GetComparedLS : " + ex.Message, MonitorLog.typelog.Error, true);
                    ret = new ReturnsObjectType<List<ComparedLS>>() { returnsData = null, result = false, text = "Ошибка выполнения" };
                }
            }
            
            return ret;
        }
        
        /// <summary>
        /// получение сопоставленных юридических лиц
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<List<ComparedPayer>> GetComparedPayer(FilesImported finder)
        {
            var ret = new ReturnsObjectType<List<ComparedPayer>>();

            using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            { 
                try
                {
                    Returns t = OpenDb(con_db, true);
                    if (!t.result) throw new Exception(t.text);

                    string sql =
                        "select distinct upper(f.urlic_name) as payer_file, trim(upper(b.npayer))||' ('||trim(upper(b.payer))||')' as payer_base, f.nzp_payer " +
                        " from " + Points.Pref + DBManager.sUploadAliasRest + "file_urlic f, " +
                        Points.Pref + DBManager.sKernelAliasRest + "s_payer b " +
                        " where f.nzp_payer = b.nzp_payer and f.nzp_payer is not null " +
                        WhereNzpFile(finder.selectedFiles);

                    var dt = ClassDBUtils.OpenSQL(SetLimitOffset(sql + " order by 1,2 ", finder), con_db);
                    List<ComparedPayer> list = new List<ComparedPayer>();
                    for (int i = 0; i < dt.resultData.Rows.Count; i++)
                    {
                        list.Add(new ComparedPayer()
                        {
                            payer_base = dt.resultData.Rows[i]["payer_base"].ToString().Trim(),
                            payer_file = dt.resultData.Rows[i]["payer_file"].ToString().Trim(),
                            nzp_payer = dt.resultData.Rows[i]["nzp_payer"].ToString().Trim()
                        });
                    }

                    ret = new ReturnsObjectType<List<ComparedPayer>>() { returnsData = list, tag = GetRecordCount(sql, con_db), result = true, text = "Выполнено" }; 
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка выполнения процедуры GetComparedPayer : " + ex.Message, MonitorLog.typelog.Error, true);
                    ret = new ReturnsObjectType<List<ComparedPayer>>() { returnsData = null, result = false, text = "Ошибка выполнения" };
                }
            }

            return ret;
        }
    }
}
