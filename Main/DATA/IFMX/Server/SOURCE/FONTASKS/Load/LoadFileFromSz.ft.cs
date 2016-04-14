using Newtonsoft.Json;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using System;


namespace STCLINE.KP50.DataBase
{
    
    /// <summary>
    /// Загрузка файла из соцзащиты 
    /// </summary>
    public class LoadFileFromSzFonTask : BaseFonTask
    {
        public override Returns StartTask()
        {
           //загрузка файла из СЗ
            Returns ret = Utils.InitReturns();
            try
            {
                var finder = JsonConvert.DeserializeObject<FilesImported>(_task.parameters);
                var iSz = new DbLoadFileFromSZ();
                ret = Utils.InitReturns();
                ret = iSz.LoadFileFromSz(finder, ref ret);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры CalcProc " +
                                    "при обработке загрузки файла из СЗ (taskLoadFileFromSZ)" +
                                    Environment.NewLine +
                                    ex.Message + ex.StackTrace,
                    MonitorLog.typelog.Error, true);
                ret.text = "Ошибка вызова ф-ции загрузки файла из СЗ.";
                ret.result = false;
                ret.tag = -1;
            }

            return ret;
            
        }

        public LoadFileFromSzFonTask(CalcFonTask task)
            : base(task)
        {
            
        }

    }
}
