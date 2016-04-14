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
    using STCLINE.KP50.DataBase;

    public class SupplierAndBankHandler : JsonRequestHandler
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
                    case "LIST_BANKS":
                    {
                        var prefs = new List<_Point>();
                        var finder = new Role();
                        finder.webLogin = context.User.Identity.Name;
                        finder.nzp_user = GetUser(finder.webLogin);
                        finder.RolesVal = GetUserRoles(finder.nzp_user);
                        finder.nzp_role = 101;
                        var prefList = cli_Sprav.DbPointAvailableForRole(finder, out ret);

                        
                        prefs = prefList.FindAll(x => x.nzp_wp == Points.PointList.Find(y => y.nzp_wp == x.nzp_wp).nzp_wp);
                        if (prefs.Count > 0)
                        {
                            //prefs.AddRange(prefList);
                            data = new List<object>(prefs.Select(x => new {Id = x.nzp_wp, Name = x.point.Trim()}).ToArray());
                        }
                        else data = null;
                        //    data = new List<object>(Points.PointList.Select(x => new { Id = x.nzp_wp, Name = x.point.Trim() }).ToArray());
                            break;
                        }
                    case "LIST_SUPPLIERS":
                        {
                            List<string> pref = new List<string>();
                            foreach (var p in selectedValues)
                            {
                                pref.Add(Points.PointList.Find(x => x.nzp_wp == (int)p).pref);
                            }
                            var AllList = new List<_Supplier>();
                          
                            foreach (var pr in pref)
                            {
                                var cliSprav = new cli_Sprav(0);
                                var finder = new Finder();
                                finder.webLogin = context.User.Identity.Name;
                                finder.nzp_user = GetUser(finder.webLogin);
                                finder.RolesVal = GetUserRoles(finder.nzp_user);
                                finder.pref = pr;
                                var servList = cliSprav.SupplierLoad(finder, enTypeOfSupp.None, out ret);
                                if (servList != null)
                                    AllList.AddRange(servList);
                            }
                            AllList = AllList.Distinct(x => x.name_supp).ToList();
                            if (AllList != null)
                            {
                                data = AllList.Select(x => new { Id = x.nzp_supp, Name = x.name_supp }).ToArray();
                            }
                            else data = null;

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

        private List<_RolesVal> GetUserRoles(int p)
        {
            var listRoles = new List<_RolesVal>();
            var db = new DbUserClient();
            db.GetRolesVal(Constants.roleReport, p, ref listRoles);
            db.Close();
            return listRoles;

        }

        private static int GetUser(string userName)
        {
            var user = new User { login = userName };
            var ret = new DbAdminClient().GetUserBy(ref user, DbAdminClient.UserBy.Login);
            return ret.result ? user.nzp_user : 0;
        }
    }

}