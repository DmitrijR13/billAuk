using System;
using System.Data;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using globalsUtils = STCLINE.KP50.Global.Utils;
using System.Collections.Generic;

namespace Bars.KP50.DataImport.SOURCE.COMPARE
{
    public partial class LinkData : SelectedFiles
    {
        
        /// <summary>
        /// сохранение nzp_area
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsType LinkArea(UncomparedAreas finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType(true, "Выполнено", -1);

            using (IDbConnection conn_db = GetConnection(Constants.cons_Kernel))
            {
                try
                {
                    Returns t = OpenDb(conn_db, true);
                    if (!t.result) throw new Exception(t.text);

                    string sql = " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "file_area SET " +
                        " nzp_area = " + globalsUtils.EStrNull(finder.nzp_area) +
                        " WHERE nzp_area IS NULL " +
                        "   AND UPPER(TRIM(area)) = " + globalsUtils.EStrNull(finder.area.ToUpper()) +
                        WhereNzpFile(selectedFiles);
                    ExecSQLWE(conn_db, sql, true);
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка выполнения процедуры LinkArea : " + ex.Message, MonitorLog.typelog.Error, true);
                    ret = new ReturnsType(false, "Ошибка выполнения", -1);
                }
            }

            return ret;
        }

        /// <summary>
        /// сохранение nzp_supp
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsType LinkSupp(UncomparedSupps finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType(true, "Выполнено", -1);

            using (IDbConnection conn_db = GetConnection(Constants.cons_Kernel))
            {
                try
                {
                    Returns t = OpenDb(conn_db, true);
                    if (!t.result) throw new Exception(t.text);

                    string sql = " update " + Points.Pref + DBManager.sUploadAliasRest + "file_supp set " +
                        " nzp_supp = " + globalsUtils.EStrNull(finder.nzp_supp) +
                        " where nzp_supp is null " +
                        "   and upper(trim(supp_name)) = " + globalsUtils.EStrNull(finder.supp.ToUpper()) +
                        WhereNzpFile(selectedFiles);
                    ExecSQLWE(conn_db, sql, true);
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка выполнения процедуры LinkSupp : " + ex.Message, MonitorLog.typelog.Error, true);
                    ret = new ReturnsType(false, "Ошибка выполнения", -1);
                }
            }

            return ret;
        }

        /// <summary>
        /// сохранение nzp_vill
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsType LinkMO(UncomparedVills finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType(true, "Выполнено", -1);

            using (IDbConnection conn_db = GetConnection(Constants.cons_Kernel))
            {
                try
                {
                    Returns t = OpenDb(conn_db, true);
                    if (!t.result) throw new Exception(t.text);

                    string sql = " update " + Points.Pref + DBManager.sUploadAliasRest + "file_mo set " +
                        "nzp_vill = " + globalsUtils.EStrNull(finder.nzp_vill) +
                        " where nzp_vill is null " +
                        "   and upper(vill) = " + globalsUtils.EStrNull(finder.vill.ToUpper()) +
                        WhereNzpFile(selectedFiles);
                    ExecSQLWE(conn_db, sql, true);
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка выполнения процедуры LinkMO : " + ex.Message, MonitorLog.typelog.Error, true);
                    ret = new ReturnsType(false, "Ошибка выполнения", -1);
                }
            }

            return ret;
        }

        /// <summary>
        /// сохранение nzp_serv
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsType LinkServ(UncomparedServs finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType(true, "Выполнено", -1);

            using (IDbConnection conn_db = GetConnection(Constants.cons_Kernel))
            {
                try
                {
                    Returns t = OpenDb(conn_db, true);
                    if (!t.result) throw new Exception(t.text);

                    string sql = " update " + Points.Pref + DBManager.sUploadAliasRest + "file_services set " +
                        " nzp_serv = " + globalsUtils.EStrNull(finder.nzp_serv) +
                        " where nzp_serv is null " +
                        "   and upper(service) = " + globalsUtils.EStrNull(finder.serv.ToUpper()) +
                        WhereNzpFile(selectedFiles);
                    ExecSQLWE(conn_db, sql, true);
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка выполнения процедуры LinkServ : " + ex.Message, MonitorLog.typelog.Error, true);
                    ret = new ReturnsType(false, "Ошибка выполнения", -1);
                }
            }

            return ret;
        }

        /// <summary>
        /// сохранение типа доп. параметров
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsType LinkParType(UncomparedParTypes finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType(true, "Выполнено", -1);

            using (IDbConnection conn_db = GetConnection(Constants.cons_Kernel))
            {
                try
                {
                    Returns t = OpenDb(conn_db, true);
                    if (!t.result) throw new Exception(t.text);

                    string sql = " update " + Points.Pref + DBManager.sUploadAliasRest + "file_typeparams set " +
                        " nzp_prm = " + globalsUtils.EStrNull(finder.nzp_prm) +
                        " where nzp_prm is null " +
                        "   and upper(prm_name) = " + globalsUtils.EStrNull(finder.name_prm.ToUpper()) +
                        WhereNzpFile(selectedFiles);
                    ExecSQLWE(conn_db, sql, true);
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка выполнения процедуры LinkParType : " + ex.Message, MonitorLog.typelog.Error, true);
                    ret = new ReturnsType(false, "Ошибка выполнения", -1);
                }
            }

            return ret;
        }

        /// <summary>
        /// Cохранение параметров
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        private ReturnsType LinkParameters(UncomparedParTypes finder, string tableParams, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType(true, "Выполнено", -1);

            using (IDbConnection conn_db = GetConnection(Constants.cons_Kernel))
            {
                try
                {
                    Returns t = OpenDb(conn_db, true);
                    if (!t.result) throw new Exception(t.text);

                    string sql = " update " + Points.Pref + DBManager.sUploadAliasRest + tableParams + " set " +
                        " nzp_prm = " + globalsUtils.EStrNull(finder.nzp_prm) +
                        " where nzp_prm is null " +
                        "   and upper(name) = " + globalsUtils.EStrNull(finder.name_prm.ToUpper()) +
                        WhereNzpFile(selectedFiles);
                    ExecSQLWE(conn_db, sql, true);
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка выполнения процедуры LinkParameters : " + ex.Message, MonitorLog.typelog.Error, true);
                    ret = new ReturnsType(false, "Ошибка выполнения", -1);
                }
            }

            return ret;
        }

        /// <summary>
        /// сохранение типа благоустройства дома
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsType LinkParBlag(UncomparedParTypes finder, List<int> selectedFiles)
        {
            return LinkParameters(finder, "file_blag", selectedFiles);
        }

        /// <summary>
        /// сохранение типа дома по газу
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsType LinkParGas(UncomparedParTypes finder, List<int> selectedFiles)
        {
            return LinkParameters(finder, "file_gaz", selectedFiles);
        }

        /// <summary>
        /// сохранение типа дома по воде
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsType LinkParWater(UncomparedParTypes finder, List<int> selectedFiles)
        {
            return LinkParameters(finder, "file_voda", selectedFiles);
        }

        /// <summary>
        /// сохранение nzp_measure
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsType LinkMeasure(UncomparedMeasures finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType(true, "Выполнено", -1);

            using (IDbConnection conn_db = GetConnection(Constants.cons_Kernel))
            {
                try
                {
                    Returns t = OpenDb(conn_db, true);
                    if (!t.result) throw new Exception(t.text);

                    string sql = " update " + Points.Pref + DBManager.sUploadAliasRest + "file_measures set " +
                        " nzp_measure = " + globalsUtils.EStrNull(finder.nzp_measure) +
                        " where nzp_measure is null " +
                        "   and upper(measure) = " + globalsUtils.EStrNull(finder.measure.ToUpper()) +
                        WhereNzpFile(selectedFiles);
                    ExecSQLWE(conn_db, sql, true);
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка выполнения процедуры LinkMeasure : " + ex.Message, MonitorLog.typelog.Error, true);
                    ret = new ReturnsType(false, "Ошибка выполнения", -1);
                }
            }

            return ret;
        }

        /// <summary>
        /// сохранение nzp_town
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsType LinkTown(UncomparedTowns finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType(true, "Выполнено", -1);

            using (IDbConnection conn_db = GetConnection(Constants.cons_Kernel))
            {
                try
                {
                    Returns t = OpenDb(conn_db, true);
                    if (!t.result) throw new Exception(t.text);

                    string sql = " update " + Points.Pref + DBManager.sUploadAliasRest + "file_dom set " +
                        " nzp_town = " + globalsUtils.EStrNull(finder.nzp_town) +
                        " where nzp_town is null " +
                        "   and upper(town) = " + globalsUtils.EStrNull(finder.town.ToUpper()) +
                        WhereNzpFile(selectedFiles);
                    ExecSQLWE(conn_db, sql, true);
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка выполнения процедуры LinkTown : " + ex.Message, MonitorLog.typelog.Error, true);
                    ret = new ReturnsType(false, "Ошибка выполнения", -1);
                }
            }

            return ret;
        }

        /// <summary>
        /// сохранение nzp_ul
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsType LinkRajon(UncomparedRajons finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType(true, "Выполнено", -1);

            if (finder.nzp_town.Trim() == "")
            {
                return new ReturnsType(false, "Не указан город/район");
            }

            using (IDbConnection conn_db = GetConnection(Constants.cons_Kernel))
            {
                try
                {
                    Returns t = OpenDb(conn_db, true);
                    if (!t.result) throw new Exception(t.text);

                    string sql = "update " + Points.Pref + DBManager.sUploadAliasRest + "file_dom set " +
                        " nzp_raj = " + globalsUtils.EStrNull(finder.nzp_raj) +
                        " where nzp_raj is null " +
                        "   and upper(rajon) = " + globalsUtils.EStrNull(finder.rajon.ToUpper()) +
                        "   and nzp_town = " + finder.nzp_town +
                        WhereNzpFile(selectedFiles);
                    ExecSQLWE(conn_db, sql, true);
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка выполнения процедуры LinkRajon : " + ex.Message, MonitorLog.typelog.Error, true);
                    ret = new ReturnsType(false, "Ошибка выполнения", -1);
                }
            }

            return ret;
        }

        /// <summary>
        /// сохранение nzp_ul
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsType LinkStreet(UncomparedStreets finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType(true, "Выполнено", -1);

            if (finder.nzp_raj.Trim() == "")
            {
                return new ReturnsType(false, "Не указан населенный пункт");
            }

            using (IDbConnection conn_db = GetConnection(Constants.cons_Kernel))
            {
                try
                {
                    Returns t = OpenDb(conn_db, true);
                    if (!t.result) throw new Exception(t.text);

                    string sql = "update " + Points.Pref + DBManager.sUploadAliasRest + "file_dom set " +
                        " nzp_ul = " + globalsUtils.EStrNull(finder.nzp_ul) +
                        " where nzp_ul is null " +
                        "   and trim(replace(upper(ulica), 'Ё', 'Е')) = upper(trim(" + globalsUtils.EStrNull(finder.ulica.ToUpper()) + ")) " +
                        "   and nzp_raj = " + finder.nzp_raj +
                        WhereNzpFile(selectedFiles);
                    ExecSQLWE(conn_db, sql, true);
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка выполнения процедуры LinkStreet : " + ex.Message, MonitorLog.typelog.Error, true);
                    ret = new ReturnsType(false, "Ошибка выполнения", -1);
                }
            }

            return ret;
        }

        /// <summary>
        /// сохранение nzp_dom
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsType LinkDom(UncomparedHouses finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();

            if (finder.nzp_ul.Trim() == "")
            {
                return new ReturnsType(false, "Не указана улица");
            }

            using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            {
                try
                {
                    var t = OpenDb(con_db, true);
                    if (!t.result) throw new Exception(t.text);

                    var ndom = "";
                    var nkor = "";
                    if (finder.dom.Split('/').Length > 1)
                    {
                        for (int i = 0; i < finder.dom.Split('/').Length - 1; i++)
                            if (i == 0) ndom += finder.dom.Split('/')[i];
                            else ndom += "/" + finder.dom.Split('/')[i];
                        nkor = " and upper(nkor) = " + globalsUtils.EStrNull(finder.dom.Split('/')[finder.dom.Split('/').Length - 1]);
                    }

                    string sql = " update " + Points.Pref + DBManager.sUploadAliasRest + "file_dom set " + 
                        " nzp_dom = " + finder.nzp_dom + 
                        " where nzp_dom is null " + 
                        "   and nzp_ul = " + finder.nzp_ul +
                        "   and upper(ndom) = " + globalsUtils.EStrNull(ndom) + 
                        nkor +
                        WhereNzpFile(selectedFiles);
                    ExecSQLWE(con_db, sql);
    
                    sql =
                        " UPDATE  " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar k SET " + 
                        " nzp_dom = (SELECT max(d.nzp_dom) FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_dom d " +
                        "   WHERE d.id = k.dom_id " + WhereNzpFile(selectedFiles, "d") + ") " +
                        " WHERE k.nzp_dom IS NULL " + WhereNzpFile(selectedFiles, "k");
                    ExecSQLWE(con_db, sql);
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка выполнения процедуры LinkDom : " + ex.Message, MonitorLog.typelog.Error, true);
                    ret = new ReturnsType(false, "Ошибка выполнения", -1);
                }
            }

            return ret;
        }

        /// <summary>
        /// сохранение nzp_kvar
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsType LinkLs(UncomparedLS finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType(true, "Выполнено", -1);

            if (finder.nzp_dom.Trim() == "")
            {
                return new ReturnsType(false, "Не указан дом");
            }

            using (IDbConnection conn_db = GetConnection(Constants.cons_Kernel))
            {
                try
                {
                    Returns t = OpenDb(conn_db, true);
                    if (!t.result) throw new Exception(t.text);

                    string sql = " update " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar set " +
                        " nzp_kvar = " + globalsUtils.EStrNull(finder.nzp_kvar) +
                        " where nzp_dom = " + globalsUtils.EStrNull(finder.nzp_dom) +
                        "   and id = " + globalsUtils.EStrNull(finder.id) +
                        WhereNzpFile(selectedFiles);
                    ExecSQLWE(conn_db, sql, true);
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка выполнения процедуры LinkLs : " + ex.Message, MonitorLog.typelog.Error, true);
                    ret = new ReturnsType(false, "Ошибка выполнения", -1);
                }
            }

            return ret;
        }

        /// <summary>
        /// сохранение nzp_payer
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsType LinkPayer(UncomparedPayer finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType(true, "Выполнено", -1);

            using (IDbConnection conn_db = GetConnection(Constants.cons_Kernel))
            {
                try
                {
                    Returns t = OpenDb(conn_db, true);
                    if (!t.result) throw new Exception(t.text);

                    string sql = " update " + Points.Pref + DBManager.sUploadAliasRest + "file_urlic set " +
                        " nzp_payer = " + globalsUtils.EStrNull(finder.nzp_payer) +
                        " where nzp_payer is null " +
                        "   and upper(trim(urlic_name)) = " + globalsUtils.EStrNull(finder.payer.ToUpper()) +
                        WhereNzpFile(selectedFiles);
                    ExecSQLWE(conn_db, sql, true);
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка выполнения процедуры LinkLs : " + ex.Message, MonitorLog.typelog.Error, true);
                    ret = new ReturnsType(false, "Ошибка выполнения", -1);
                }
            }

            return ret;
        }

        /// <summary>
        /// изменение города для района
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        public ReturnsType ChangeTownForRajon(UncomparedRajons finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType(true, "Выполнено", -1);

            using (IDbConnection conn_db = GetConnection(Constants.cons_Kernel))
            {
                try
                {
                    Returns t = OpenDb(conn_db, true);
                    if (!t.result) throw new Exception(t.text);

                    string sql = " update " + Points.Pref + DBManager.sUploadAliasRest + "file_dom set " +
                        " town = trim(town )||', '||trim(rajon) " +
                        " where upper(rajon) " + DataImportUtils.plike + " " + globalsUtils.EStrNull(finder.rajon.ToUpper()) +
                        WhereNzpFile(selectedFiles);
                    ExecSQLWE(conn_db, sql, true);
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка выполнения процедуры LinkLs : " + ex.Message, MonitorLog.typelog.Error, true);
                    ret = new ReturnsType(false, "Ошибка выполнения", -1);
                }
            }

            return ret;
        }
    }
}
