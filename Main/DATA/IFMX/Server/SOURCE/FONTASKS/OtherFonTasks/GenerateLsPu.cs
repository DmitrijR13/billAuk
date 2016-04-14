using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.IFMX.Server.SOURCE.FONTASKS.Unload
{
    public class GenerateLsPu : BaseFonTask
    {
        public override Returns StartTask()
        {
            Returns ret = Utils.InitReturns();
            try
            {
                var finder = JsonConvert.DeserializeObject<Finder>(_task.parameters);
                ret = Utils.InitReturns();
                using(var db = new DbAdresHard())
                {
                    ret = db.GenerateGroupLsPu(finder);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения функции " + System.Reflection.MethodBase.GetCurrentMethod().Name +
                                    Environment.NewLine +
                                    ex.Message + ex.StackTrace,
                    MonitorLog.typelog.Error, true);
                ret.text = "Ошибка вызова ф-ции генерация ИПУ.";
                ret.result = false;
                ret.tag = -1;
            }

            return ret;
        }

        public GenerateLsPu(CalcFonTask task)
            : base(task)
        {

        }
    }
}
