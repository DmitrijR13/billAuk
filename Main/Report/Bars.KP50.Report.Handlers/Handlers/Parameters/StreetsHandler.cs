namespace Bars.KP50.Report.Handlers.Parameters
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web;

    using STCLINE.KP50.Client;
    using STCLINE.KP50.Global;
    using STCLINE.KP50.Interfaces;

    public class StreetsHandler : JsonRequestHandler
    {
        public override void ProcessRequest(HttpContext context)
        {
            var result = new List<object>();

            try
            {

                //result.Add(new { Id = 0, Name = "Не реализовано" });
                var cliAdres = new cli_Adres(0);

                //var pref = context.Request["pref"];
                //if (string.IsNullOrEmpty(pref))
                //{
                //    pref = Points.Pref ?? "ftul";
                //}

                Returns ret;

                Dom finder = new Dom();
                finder.webLogin = context.User.Identity.Name;
               // finder.pref = pref;
                finder.list_nzp_wp = new List<int>();
                foreach (_Point p in Points.PointList)
                {
                    if (p.nzp_wp > 1)
                        finder.list_nzp_wp.Add(p.nzp_wp);
                }
                
                var streetList = cliAdres.GetUlica(finder, enSrvOper.SrvLoad, out ret);

                if (streetList != null)
                {
                    result.AddRange(streetList.Select(x => new { Id = x.nzp_ul, Name = x.rajon + x.ulica }).ToArray());
                }

                SetResponse(context, GetJson(new { data = result }));
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка загрузки улиц: " + ex.Message, MonitorLog.typelog.Error, true);
            }
        }
    }
}