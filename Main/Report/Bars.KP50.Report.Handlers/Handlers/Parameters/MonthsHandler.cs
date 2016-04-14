namespace Bars.KP50.Report.Handlers.Parameters
{
    using System;
    using System.Web;
    using STCLINE.KP50.Global;
    
    public class MonthsHandler : JsonRequestHandler
    {
        public override void ProcessRequest(HttpContext context)
        {
            try
            {
                SetResponse(
                    context,
                    GetJson(new
                    {
                        data = new[]
                        {
                            new { Id = 1, Name = "январь" },
                            new { Id = 2, Name = "февраль" },
                            new { Id = 3, Name = "март" },
                            new { Id = 4, Name = "апрель" },
                            new { Id = 5, Name = "май" },
                            new { Id = 6, Name = "июнь" },
                            new { Id = 7, Name = "июль" },
                            new { Id = 8, Name = "август" },
                            new { Id = 9, Name = "сентябрь" },
                            new { Id = 10, Name = "октябрь" },
                            new { Id = 11, Name = "ноябрь" },
                            new { Id = 12, Name = "декабрь" }
                        }
                    }));
            }
            catch (Exception exc)
            {
                MonitorLog.WriteException("Ошибка процедуры ProcessRequest ", exc);
            }
        }
    }
}