﻿namespace Bars.KP50.Report.Handlers.Parameters
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web;

    using STCLINE.KP50.Client;
    using STCLINE.KP50.Global;
    using STCLINE.KP50.Interfaces;
    using STCLINE.KP50.DataBase;

    public class SupplierHandler : JsonRequestHandler
    {
        public override void ProcessRequest(HttpContext context)
        {
            var result = new List<object>();
            
            try
            {
                var cliSprav = new cli_Sprav(0);

                //var pref = context.Request["pref"];
                //if (string.IsNullOrEmpty(pref))
                //{
                //    pref = Points.Pref ?? "ftul";
                //}

                Returns ret;
               // var servList = cliSprav.SupplierLoad(new Finder { webLogin = context.User.Identity.Name, pref = pref }, enTypeOfSupp.None, out ret);

                var finder = new Finder();
                finder.webLogin = context.User.Identity.Name;
                finder.nzp_user = GetUser(finder.webLogin);
                finder.RolesVal = GetUserRoles(finder.nzp_user);


                var servList = cliSprav.SupplierLoad(finder, enTypeOfSupp.None, out ret);
                if (servList != null)
                {
                    result.AddRange(servList.Select(x => new { Id = x.nzp_supp, Name = x.name_supp }).ToArray());
                }

                SetResponse(context, GetJson(new { data = result }));
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка загрузки услуг по недопоставкам LoadServ : " + ex.Message, MonitorLog.typelog.Error, true);
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