namespace Bars.KP50.Report.Handlers.Parameters
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web;

    using STCLINE.KP50.Global;
    using STCLINE.KP50.Client;
    using STCLINE.KP50.Interfaces;

    public class RaionsDomsHandler : JsonRequestHandler
    {
        public override void ProcessRequest(HttpContext context)
        {
            var result = new List<object>();

            try
            {
                Returns ret;
                var cliAdres = new cli_Adres(0);
                var rajonList = cliAdres.LoadRajonDom(new RajonDom { nzp_raj_dom = 1 }, out ret);

                if (rajonList != null)
                    result.AddRange(rajonList.Select(x => new { Id = x.nzp_raj_dom, Name = x.rajon_dom }).ToArray());
                else
                    result.Add(new { Id = -1, Name = "Не указано" });


                SetResponse(context, GetJson(new { data = result }));
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка загрузки райнов: " + ex.Message, MonitorLog.typelog.Error, true);
            }
        }
    }
}