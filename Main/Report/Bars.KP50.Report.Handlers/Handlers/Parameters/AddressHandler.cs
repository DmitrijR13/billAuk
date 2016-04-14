namespace Bars.KP50.Report.Handlers.Parameters
{
    using System;
    using System.Collections.Generic;
    using System.Web;
    using System.Linq;

    using Bars.KP50.Utils;
    using STCLINE.KP50.Client;
    using STCLINE.KP50.Global;
    using STCLINE.KP50.Interfaces;

    public class AddressHandler : JsonRequestHandler
    {
        public override void ProcessRequest(HttpContext context)
        {
            try
            {
                object data;
                Returns ret;
                var operationId = context.Request["operationId"].ToStr().ToUpper();
                var selectedValues = context.Request["selectedValues"].To<List<int>>();
                switch (operationId)
                {
                    case "LIST_RAIONS":
                        {
                            var cliAdres = new cli_Adres(0);
                            var rajonList = cliAdres.LoadRajon(new Rajon { nzp_user = 1 }, out ret);
                            if (rajonList != null)
                                data = new List<object>(rajonList.Select(x => new { Id = x.nzp_raj, Name = x.rajon }).ToArray());
                            else
                                data = (new { Id = -1, Name = "Район не определен" });
                            break;
                        }
                    case "LIST_STREETS":
                        {
                            var cliAdres = new cli_Adres(0);
                            Dom finder = new Dom();
                            finder.nzp_rajs = string.Join(",", selectedValues.Select(n => n.ToString()).ToArray());
                            finder.webLogin = context.User.Identity.Name;
                            finder.list_nzp_wp = new List<int>();
                            foreach (_Point p in Points.PointList)
                            {
                                if (p.nzp_wp > 1)
                                    finder.list_nzp_wp.Add(p.nzp_wp);
                            }
                            var streetList = cliAdres.GetUlica(finder, enSrvOper.SrvLoad, out ret);
                            if (streetList != null)
                                data = new List<object>(streetList.Select(x => new { Id = x.nzp_ul, Name = x.ulica_short }).ToArray());
                            else
                                data = (new { Id = -1, Name = "Улицы не определены" });
                            break;
                        }
                    case "LIST_HOUSES":
                        {
                            data = new List<object>();
                            foreach (var u in selectedValues)
                            {
                                var cliDom = new cli_Adres(0);
                                Dom finder = new Dom() { nzp_user = 1 };
                                finder.nzp_ul = u;
                                var housesList = cliDom.LoadDom(finder, out ret);
                                if (housesList != null)
                                    ((List<object>)data).AddRange(new List<object>(housesList.Select(x => new { Id = x.nzp_dom, Name = x.ulica_short + " д." + x.ndom + ((x.nkor != null && x.nkor != "-") ? (" кор." + x.nkor) : "") }).ToArray()));
                            }

                            //if (selectedValues.Count > 1)
                            //    data = new List<object>();
                            //else
                            //{
                            //    var cliDom = new cli_Adres(0);
                            //    Dom finder = new Dom() { nzp_user = 1 };
                            //    finder.nzp_ul = selectedValues[0];
                            //    var housesList = cliDom.LoadDom(finder, out ret);
                            //    if (housesList != null)
                            //        data = new List<object>(housesList.Select(x => new { Id = x.nzp_dom, Name = " д." + x.ndom + " кор." + x.nkor }).ToArray());
                            //    else
                            //        data = (new { Id = -1, Name = "Дома не определены" });
                            //}
                            break;
                        }
                    default:
                        data = null;
                        break;
                }

                SetResponse(context, GetJson(new { success = true, data }));
            }
            catch (Exception exc)
            {
                MonitorLog.WriteException("Ошибка процедуры ProcessRequest ", exc);
            }
        }
    }
}