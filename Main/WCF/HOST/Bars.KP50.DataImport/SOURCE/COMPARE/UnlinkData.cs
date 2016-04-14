using System;
using System.Data;
using System.Collections.Generic;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.DataImport.SOURCE.COMPARE
{
    public class UnlinkData : SelectedFiles
    {
        private ReturnsType DeleteDataLink(string bufferTable, string bufferField, string bufferFieldID, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();

            using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            {
                try
                {
                    var t = OpenDb(con_db, true);
                    if (!t.result) throw new Exception(ret.text);

                    string sql =
                        " update " + Points.Pref + DBManager.sUploadAliasRest + bufferTable + " set " + bufferField + " = null " +
                        " where " + bufferField + " = " + bufferFieldID +
                        WhereNzpFile(selectedFiles);

                    ExecSQLWE(con_db, sql);
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка выполнения процедуры UnlinkData : " + ex.Message, MonitorLog.typelog.Error, true);
                    ret = new ReturnsType(false, "Ошибка выполнения");
                }
            }

            return ret;
        }
        
        
        
        /// <summary>
        /// удаление сопоставления участков
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsType UnlinkArea(ComparedAreas finder, List<int> selectedFiles)
        {
            return DeleteDataLink("file_area", "nzp_area", finder.nzp_area, selectedFiles);
        }

        /// <summary>
        /// удаление сопоставления поставщиков
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsType UnlinkSupp(ComparedSupps finder, List<int> selectedFiles)
        {
            return DeleteDataLink("file_supp", "nzp_supp", finder.nzp_supp, selectedFiles);
        }

        /// <summary>
        /// удаление сопоставления МО
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsType UnlinkMO(ComparedVills finder, List<int> selectedFiles)
        {
            return DeleteDataLink("file_mo", "nzp_vill", finder.nzp_vill, selectedFiles);
        }

        /// <summary>
        /// удаление сопоставления услуг
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsType UnlinkServ(ComparedServs finder, List<int> selectedFiles)
        {
            return DeleteDataLink("file_services", "nzp_serv", finder.nzp_serv, selectedFiles);
        }

        /// <summary>
        /// удаление сопоставления типоа доп. параметров
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsType UnlinkParType(ComparedParTypes finder, List<int> selectedFiles)
        {
            return DeleteDataLink("file_typeparams", "nzp_prm", finder.nzp_prm, selectedFiles);
        }

        /// <summary>
        /// удаление сопоставления типоа благоустройства дома
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsType UnlinkParBlag(ComparedParTypes finder, List<int> selectedFiles)
        {
            return DeleteDataLink("file_blag", "nzp_prm", finder.nzp_prm, selectedFiles);
        }

        /// <summary>
        /// удаление сопоставления типоа доп. параметров
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsType UnlinkParGas(ComparedParTypes finder, List<int> selectedFiles)
        {
            return DeleteDataLink("file_gaz", "nzp_prm", finder.nzp_prm, selectedFiles);
        }

        /// <summary>
        /// удаление сопоставления типоа доп. параметров
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsType UnlinkParWater(ComparedParTypes finder, List<int> selectedFiles)
        {
            return DeleteDataLink("file_voda", "nzp_prm", finder.nzp_prm, selectedFiles);
        }

        /// <summary>
        /// удаление сопоставления единиц измерения
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsType UnlinkMeasure(ComparedMeasures finder, List<int> selectedFiles)
        {
            return DeleteDataLink("file_measures", "nzp_measure", finder.nzp_measure, selectedFiles);
        }

        /// <summary>
        /// удаление сопоставления городов
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsType UnlinkTown(ComparedTowns finder, List<int> selectedFiles)
        {
            return DeleteDataLink("file_dom", "nzp_town", finder.nzp_town, selectedFiles);
        }

        /// <summary>
        /// удаление сопоставления насеоенных пунктов
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsType UnlinkRajon(ComparedRajons finder, List<int> selectedFiles)
        {
            return DeleteDataLink("file_dom", "nzp_raj", finder.nzp_raj, selectedFiles);
        }

        /// <summary>
        /// удаление сопоставления улиц
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsType UnlinkStreet(ComparedStreets finder, List<int> selectedFiles)
        {
            return DeleteDataLink("file_dom", "nzp_ul", finder.nzp_ul, selectedFiles);
        }

        /// <summary>
        /// удаление сопоставления домов
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsType UnlinkHouse(ComparedHouses finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();
            
            using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            {
                try
                {
                    var t = OpenDb(con_db, true);
                    if (!t.result) throw new Exception(t.text);
                    
                    string sql = "update " + Points.Pref + DBManager.sUploadAliasRest + "file_dom " + 
                        " set nzp_dom = null " + 
                        " where nzp_dom = " + finder.nzp_dom +
                        WhereNzpFile(selectedFiles);
                    ExecSQLWE(con_db, sql);

                    sql = "update " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar " + 
                        " set (nzp_dom, nzp_kvar, ukas) = (null,null,null) " +
                        " where nzp_dom = " + finder.nzp_dom +
                        WhereNzpFile(selectedFiles);
                    ExecSQLWE(con_db, sql);
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка выполнения процедуры UnlinkHouse : " + ex.Message, MonitorLog.typelog.Error, true);
                    ret = new ReturnsType(false, "Ошибка выполнения");
                }
            }

            return ret;
        }

        /// <summary>
        /// удаление сопоставления ЛС
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsType UnlinkLS(ComparedLS finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();

            using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            {
                try
                {
                    var t = OpenDb(con_db, true);
                    if (!t.result) throw new Exception(t.text);

                    string sql = "update " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar " + 
                        " set (nzp_kvar, ukas) = (null, null) where nzp_kvar = " + finder.nzp_kvar +
                        WhereNzpFile(selectedFiles);
                    ExecSQLWE(con_db, sql);
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка выполнения процедуры UnlinkHouse : " + ex.Message, MonitorLog.typelog.Error, true);
                    ret = new ReturnsType(false, "Ошибка выполнения");
                }
            }

            return ret;
        }

        /// <summary>
        /// удаление сопоставления юридических лиц
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsType UnlinkPayer(ComparedPayer finder, List<int> selectedFiles)
        {
            return DeleteDataLink("file_urlic", "nzp_payer", finder.nzp_payer, selectedFiles);
        }
    }
}
