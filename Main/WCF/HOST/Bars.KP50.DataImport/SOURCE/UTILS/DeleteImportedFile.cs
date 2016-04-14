using System;
using System.Data;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.DataImport.SOURCE
{
    public class DbDeleteImportedFile : DbAdminClient
    {
        private int _commandTime = 3600;
        private readonly IDbConnection _conDb;
        private int nzp_file = -1;
        public DbDeleteImportedFile(IDbConnection conDb)
        {
            _conDb = conDb;
        }

        /// <summary>
        /// Удалить загруженный файл
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public Returns DeleteImportedFile(FilesDisassemble finder)
        {
            Returns ret = STCLINE.KP50.Global.Utils.InitReturns();
            try
            {
                if (finder.selectedFiles.Count != 1)
                {
                    ret.text = "Для удаления должен быть выбран один файл. Выбрано:" + finder.selectedFiles.Count;
                    ret.result = false;
                    ret.tag = -1;
                    return ret;
                }
                
                finder.nzp_file = Convert.ToInt32(finder.selectedFiles[0]);

                //обновление статуса файла
                string sql = " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "files_imported SET nzp_status = " + (int)FilesImported.Statuses.Deleted +
                    " WHERE nzp_file = " + finder.nzp_file;

                ret = ExecSQL(_conDb, sql, true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка удаления файла nzp_file = " + nzp_file, MonitorLog.typelog.Error, true);
                    ret.text = " Ошибка удаления файла.";
                    ret.tag = -1;
                    return ret;
                }

                DeleteDataFromTbl("file_area");
                DeleteDataFromTbl("file_blag");
                DeleteDataFromTbl("file_dom");
                DeleteDataFromTbl("file_gaz");
                DeleteDataFromTbl("file_gilec");
                DeleteDataFromTbl("file_head");
                DeleteDataFromTbl("file_ipu");
                DeleteDataFromTbl("file_ipu_p");
                DeleteDataFromTbl("file_kvar");
                DeleteDataFromTbl("file_kvarp");
                DeleteDataFromTbl("file_measures");
                DeleteDataFromTbl("file_mo");
                DeleteDataFromTbl("file_nedopost");
                DeleteDataFromTbl("file_odpu");
                DeleteDataFromTbl("file_odpu_p");
                DeleteDataFromTbl("file_oplats");
                DeleteDataFromTbl("file_pack");
                DeleteDataFromTbl("file_paramsdom");
                DeleteDataFromTbl("file_paramsls");
                DeleteDataFromTbl("file_serv");
                DeleteDataFromTbl("file_services");
                DeleteDataFromTbl("file_servls");
                DeleteDataFromTbl("file_servp");
                DeleteDataFromTbl("file_supp");
                DeleteDataFromTbl("file_typenedopost");
                DeleteDataFromTbl("file_typeparams");
                DeleteDataFromTbl("file_urlic");
                DeleteDataFromTbl("file_voda");
                DeleteDataFromTbl("file_vrub");
                DeleteDataFromTbl("file_pack");
                DeleteDataFromTbl("files_selected");
                DeleteDataFromTbl("file_section");
                DeleteDataFromTbl("file_sql");
                DeleteDataFromTbl("file_dog");
                DeleteDataFromTbl("file_reestr_ls");
                DeleteDataFromTbl("file_rs");
                DeleteDataFromTbl("file_agreement");
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры DeleteImportedFile : " + ex.Message  + ex.StackTrace, MonitorLog.typelog.Error, true);
                ret.result = false;
                ret.text = "Ошибка при удалении файла.";
                return ret;
            }
            ret.text = "Успешно удалено.";
            ret.result = true;
            return ret;
        }

        public void DeleteDataFromTbl(string tableName)
        {
            string sql =
                " DELETE FROM  " + Points.Pref + DBManager.sUploadAliasRest + tableName + 
                " WHERE nzp_file =" + nzp_file;
            DBManager.ExecSQL(_conDb, null, sql, true, _commandTime);
        }
    }
}
