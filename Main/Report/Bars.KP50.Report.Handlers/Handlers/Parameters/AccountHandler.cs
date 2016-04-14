namespace Bars.KP50.Report.Handlers.Parameters
{
    using System;
    using System.Collections.Generic;
    using System.Web;
    using System.Linq;
    using STCLINE.KP50.Global;
    using STCLINE.KP50.Client;
    using STCLINE.KP50.Interfaces;

    public class AccountHandler : JsonRequestHandler
    {
        public override void ProcessRequest(HttpContext context)
        {
            try
            {
                object data;
                var cli = new cli_Pack(0);
                var finder = new Pack_ls();
                finder.skip = 0;
                finder.rows = 100;
                Returns ret;
                var list = cli.GetRS(finder, out ret);
                int iterator = -1;

                if (list != null)
                    data = new List<object>(list.Select(x => new { Id = list[++iterator] , Name = list[iterator] }).ToArray());
                else
                    data = (new { Id = -1, Name = "Место не найдено" });
                SetResponse(context, GetJson(new { success = true, data }));
            }
            catch (Exception exc)
            {
                MonitorLog.WriteException("Ошибка в процедуре ProcessRequest: ", exc);
            }
        }
    }
}
