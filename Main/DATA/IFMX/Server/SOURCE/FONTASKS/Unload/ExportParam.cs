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
    public class ExportParam : BaseFonTask
    {
        public override Returns StartTask()
        {
            Returns ret = Utils.InitReturns();
            try
            {
                var finder = JsonConvert.DeserializeObject<ExportParamsFinder>(_task.parameters);
                ret = Utils.InitReturns();
                using (var db = new DbExchange())
                {
                    ret = db.ExportParam(finder);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения функции экспорт параметров (ExportParam)" +
                                    Environment.NewLine +
                                    ex.Message + ex.StackTrace,
                    MonitorLog.typelog.Error, true);
                ret.text = "Ошибка вызова ф-ции экспорта парметров.";
                ret.result = false;
                ret.tag = -1;
            }

            return ret;
        }

        public ExportParam(CalcFonTask task)
            : base(task)
        {

        }
    }
}
