using Newtonsoft.Json;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using System;
using System.Data;


namespace STCLINE.KP50.DataBase
{
    
    /// <summary>
    /// Задача на разбор файла при загрузке
    /// </summary>
    public class LoadFileFonTask : BaseFonTask
    {
        public override Returns StartTask()
        {
            #region Подключение к БД

            IDbConnection conDB = DBManager.GetConnection(Constants.cons_Kernel);
            Returns ret = DBManager.OpenDb(conDB, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка открытия соединения в ф-ции CalcProc " +
                                    "при  при обработке задачи разбора файла (taskLoadFile)",
                    MonitorLog.typelog.Error, true);
                {
                    return ret;
                }
            }

            #endregion Подключение к БД

            try
            {
                var finder = JsonConvert.DeserializeObject<FilesImported>(_task.parameters);
                var fl = new DbFileLoader(conDB);
                ret = Utils.InitReturns();
                ret = fl.LoadFile(finder, ref ret);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры CalcProc " +
                                    "при обработке задачи разбора файла (taskLoadFile)" + Environment.NewLine +
                                    ex.Message + ex.StackTrace,
                    MonitorLog.typelog.Error, true);
                ret.text = "Ошибка вызова ф-ции загрузки.";
                ret.result = false;
                ret.tag = -1;
            }
            finally
            {
                conDB.Close();
            }
            return ret;
            
        }

        public LoadFileFonTask(CalcFonTask task)
            : base(task)
        {
            
        }

    }
}
