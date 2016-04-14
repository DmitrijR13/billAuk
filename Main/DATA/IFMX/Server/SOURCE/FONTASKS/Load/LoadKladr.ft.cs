using Newtonsoft.Json;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using System;
using System.Data;


namespace STCLINE.KP50.DataBase
{
    
    /// <summary>
    /// Загрузка кладра 
    /// </summary>
    public class LoadKladrFonTask : BaseFonTask
    {
        public override Returns StartTask()
        {
            Returns ret = Utils.InitReturns();
            var finder = new FilesImported();

            #region Подключение к БД

            IDbConnection con_db = DBManager.GetConnection(Global.Constants.cons_Kernel);
            ret = DBManager.OpenDb(con_db, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка открытия соединения в ф-ции CalcProc " +
                                    "при  при обработке задачи разбора файла (taskLoadKladr)",
                    MonitorLog.typelog.Error, true);
                {
                    return ret;
                }
            }

            #endregion Подключение к БД

            try
            {
                finder = JsonConvert.DeserializeObject<FilesImported>(_task.parameters);
                DbKladr kl = new DbKladr(con_db);
                ret = Utils.InitReturns();
                ret = kl.RefreshKLADRFile(finder, ref ret);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры CalcProc " +
                                    "при обработке задачи разбора файла (taskLoadKladr)" + Environment.NewLine +
                                    ex.Message + ex.StackTrace,
                    MonitorLog.typelog.Error, true);
                ret.result = false;
                ret.tag = -1;
            }
            finally
            {
                con_db.Close();
            }
            return ret;
            
        }

        public LoadKladrFonTask(CalcFonTask task)
            : base(task)
        {
            
        }

    }
}
