namespace Bars.KP50.Report.Handlers.Parameters
{
    using System;
    using System.Collections.Generic;
    using System.Web;
    using STCLINE.KP50.Global;

    public class DaysHandler : JsonRequestHandler
    {
        public override void ProcessRequest(HttpContext context)
        {
            try
            {
                var days = new List<object>();
                for (var day = 1; day < 32; day++)
                {
                    days.Add(new { Id = day, Name = day });
                }

                SetResponse(context, GetJson(new { data = days }));
            }
            catch (Exception exc)
            {
                MonitorLog.WriteException("Ошибка в процедуре ProcessRequest: ", exc);
            }
        }
    }
}