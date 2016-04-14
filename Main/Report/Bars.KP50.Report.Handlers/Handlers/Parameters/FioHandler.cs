using System.Linq;
using Bars.KP50.Utils;
using STCLINE.KP50.Client;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.Report.Handlers.Parameters
{
    using System;
    using System.Collections.Generic;
    using System.Web;
    using STCLINE.KP50.Global;

    public class FioHandler : JsonRequestHandler
    {
        public override void ProcessRequest(HttpContext context)
        {
            try
            {
                object data;
                Returns ret;
                    var cliAdres = new cli_Gilec(0);
                    var kart = cliAdres.GetDataFromKart(new Kart { nzp_user = 1, nzp_kvar = Convert.ToInt32(context.Session["CurT_nzp_kvar"]) }, out ret);
                if (kart != null)
                    data =
                        new List<object>(
                            kart.Select(
                                x =>
                                    new
                                    {
                                        Id = x.nzp_kart,
                                        Name =
                                            x.fam.Trim() + " " + 
                                            x.ima.Trim() + " " + 
                                            x.otch.Trim() + " " +
                                            Convert.ToDateTime(x.dat_rog).ToLongDateString()
                                    }).ToArray());
                    else
                        data = (new { Id = -1, Name = "Карта не найдена" });
                    SetResponse(context, GetJson(new { success = true, data }));
            }
            catch (Exception exc)
            {
                MonitorLog.WriteException("Ошибка в процедуре ProcessRequest: ", exc);
            }
        }
    }
}