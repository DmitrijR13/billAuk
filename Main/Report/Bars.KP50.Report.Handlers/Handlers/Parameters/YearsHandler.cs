namespace Bars.KP50.Report.Handlers.Parameters
{
    using System;
    using System.Collections.Generic;
    using System.Web;
    using STCLINE.KP50.Global;
    
    public class YearsHandler : JsonRequestHandler
    {
        public override void ProcessRequest(HttpContext context)
        {
            try
            {
                var years = new List<object>();
                for (var year = 2006; year < (DateTime.Now.Year + 2); year++)
                {
                    years.Add(new { Id = year, Name = year });
                }

                SetResponse(context, GetJson(new { data = years }));
            }
            catch (Exception exc)
            {
                MonitorLog.WriteException("Ошибка процедуры ProcessRequest: ", exc);
            }
        }
    }
}