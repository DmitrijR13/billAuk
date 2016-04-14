using Newtonsoft.Json;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using System;


namespace STCLINE.KP50.DataBase
{
    
    /// <summary>
    /// Проверки перед закрытием месяца 
    /// </summary>
    public class CheckBeforeClosingMonth : BaseFonTask
    {
        public override Returns StartTask()
        {
            Returns ret = Utils.InitReturns();
            try
            {
                Finder finder = JsonConvert.DeserializeObject<FilesImported>(_task.parameters);
                ret = Utils.InitReturns();
                using (var db = new DbCharge())
                {
                    db.MakeChecksBeforeCloseCalcMonth(finder, finder.dopFind);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры CalcProc " +
                                    "при проверках перед закрытием месяца (CheckBeforeClosingMonth)" +
                                    Environment.NewLine +
                                    ex.Message + ex.StackTrace,
                    MonitorLog.typelog.Error, true);
                ret.text = "Ошибка вызова ф-ции проверки перед закрытием месяца.";
                ret.result = false;
                ret.tag = -1;
            }

            return ret;
        }

        public CheckBeforeClosingMonth(CalcFonTask task)
            : base(task)
        {
            
        }

    }
}
