using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Bars.KP50.DataImport.CHECK.Load_Report;
using Bars.KP50.Utils;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.DataImport.SOURCE
{
    public class LoadReport : DbAdminClient
    {
        public Returns MakeReportOfLoad(FilesImported finder)
        {
            #region Подключение к БД

            IDbConnection connDB = DBManager.GetConnection(Constants.cons_Kernel);
            Returns ret = DBManager.OpenDb(connDB, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка открытия соединения в ф-ции MakeReportOfLoad", MonitorLog.typelog.Error, true);
                ret.text = "Ошибка открытия соединения";
                ret.tag = -1;
                return ret;
            }

            #endregion
            
            try
            {
                for (int i = 0; i < finder.selectedFiles.Count; i++)
                {
                    try
                    {
                        var fileImport = new FilesImported
                        {
                            nzp_user = finder.nzp_user,
                            nzp_file = finder.selectedFiles[i]
                        };

                        var act = new ActLoadOfReports(connDB, fileImport);
                        act.InsertNzpKvarInGroup();

                        act.AddReport(new ReportSopLS());
                        act.AddReport(new ReportSopTypeParams());
                        act.AddReport(new ReportSopService());
                        act.AddReport(new ReportSopDogovor());
                        act.AddReport(new ReportSopUrlic());
                        act.AddReport(new ReportSopDom());
                        act.AddReport(new ReportSaldo());
                        act.AddReport(new Report_DataLoad());
                        act.AddReport(new ReportPuWithUncorrServ());

                        ret = act.ToGenerateAct();
                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog("MakeReportOfLoad: Произошла ошибка при формирование акта : " + ex.Message, MonitorLog.typelog.Error, true);
                        ret.text = "Имеются несформированные акты";
                        ret.tag = -1;
                    }
                }

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры MakeReportOfLoad : " + ex.Message,
                    MonitorLog.typelog.Error, true);
                ret.result = false;
                ret.text = "Ошибка выполнения формирования акта";
            }
            ret.result = true;
            return ret;
        }

        /// <summary>Получить список файлов с несформироваными актами</summary>
        /// <param name="finder">Доп.информация о файле</param>
        /// <param name="ret">Индикатор ошибок</param>
        /// <returns>Список файлов</returns>
        public List<int> GetFilesIsNotExistAct(FilesImported finder, out Returns ret)
        {
            //прооверка на пользователя
            if (finder.nzp_user < 0)
            {
                ret =  new Returns(false, "Не задан пользователь", -1);
                return null;
            }
            if (finder.selectedFiles == null || finder.selectedFiles.Count == 0)
            {
                ret = new Returns(true, "Не выбран файл(-ы)", -1);
                return null;
            }

            #region Подключение к БД

            IDbConnection connDB = DBManager.GetConnection(Constants.cons_Kernel);
            ret = DBManager.OpenDb(connDB, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка открытия соединения в ф-ции GetFilesIsNotExistAct", MonitorLog.typelog.Error, true);
                ret.text = "Ошибка открытия соединения";
                ret.tag = -1;
                return null;
            }

            #endregion

            var selectFileNE = new List<int>();
            string selectedFile = finder.selectedFiles.Aggregate(string.Empty,(current,value) => current + (value + ",")).TrimEnd(',');
            try
            {
                string sql = " SELECT nzp_file " +
                             " FROM " + Points.Pref + DBManager.sUploadAliasRest + "files_imported " +
                             " WHERE nzp_file IN (" + selectedFile + ") " +
                               " AND nzp_exc_rep_load IS NULL ";
                DataTable tt = DBManager.ExecSQLToTable(connDB, sql);
                selectFileNE.AddRange(from DataRow row in tt.Rows
                    where row["nzp_file"] != DBNull.Value
                    select Convert.ToInt32(row["nzp_file"]));
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка в ф-ции GetFilesIsNotExistAct.\n" + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = "Ошибка разбора";
                ret.result = false;
                ret.tag = -1;
                return null;
            }

            return selectFileNE;
        }
    }
}
