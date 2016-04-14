using Newtonsoft.Json;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using System;


namespace STCLINE.KP50.DataBase
{
    
    /// <summary>
    /// Загрузка файла из соцзащиты 
    /// </summary>
    public class UnloadFileForSzFonTask : BaseFonTask
    {
        public override Returns StartTask()
        {
            //выгрузка файла для СЗ
            Returns ret = Utils.InitReturns();
            var finder = new FilesImported();
            try
            {
                finder = JsonConvert.DeserializeObject<FilesImported>(_task.parameters);
                StartUnl su = new StartUnl();
                ret = Utils.InitReturns();
                ret = su.Run(finder);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры CalcProc " +
                                    "при обработке выгрузки файла для СЗ (taskUnloadFileForSZ)" +
                                    Environment.NewLine +
                                    ex.Message + ex.StackTrace,
                    MonitorLog.typelog.Error, true);
                ret.text = "Ошибка вызова ф-ции выгрузки файла для СЗ.";
                ret.result = false;
                ret.tag = -1;
            }
            return ret;
            
        }

        public UnloadFileForSzFonTask(CalcFonTask task)
            : base(task)
        {
            
        }

    }
}
