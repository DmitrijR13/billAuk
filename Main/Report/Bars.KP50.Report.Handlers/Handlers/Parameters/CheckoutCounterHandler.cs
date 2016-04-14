

namespace Bars.KP50.Report.Handlers.Parameters
{
    using System;
    using System.Collections.Generic;
    using System.Web;
    using System.Linq;
    using STCLINE.KP50.Global;
    using STCLINE.KP50.Client;
    using STCLINE.KP50.Interfaces;

    public class CheckoutCounterHandler : JsonRequestHandler
    {
        public override void ProcessRequest(HttpContext context)
        {
            try
            {
                object data;
                Payer finder = new Payer();
                finder.nzp_user = 999;
                Returns ret;
                cli_Sprav cli = new cli_Sprav(0);
                var list = cli.BankPayerLoad(finder, out ret);


                if (list != null)
                    data =
                        new List<object>(list.Select(x => new { Id = x.nzp_bank, Name = x.bank }).ToArray());
                else
                    data = (new { Id = -1, Name = "Место не найдено." });
                SetResponse(context, GetJson(new { success = true, data }));
            }
            catch (Exception exc)
            {
                MonitorLog.WriteException("Ошибка в процедуре ProcessRequest: ", exc);
            }
        }
    }
}
