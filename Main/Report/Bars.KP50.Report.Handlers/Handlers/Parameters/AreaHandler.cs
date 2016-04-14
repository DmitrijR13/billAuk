﻿using STCLINE.KP50.DataBase;

namespace Bars.KP50.Report.Handlers.Parameters
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web;

    using STCLINE.KP50.Client;
    using STCLINE.KP50.Global;
    using STCLINE.KP50.Interfaces;

    public class AreaHandler : JsonRequestHandler
    {
        public override void ProcessRequest(HttpContext context)
        {
            var result = new List<object>();
            
            try
            {
                //var pref = context.Request["pref"];
                //if (string.IsNullOrEmpty(pref))
                //{
                //    pref = Points.Pref ?? "ftul";
                //}

                var finder = new Finder();
                finder.webLogin = context.User.Identity.Name;
                finder.nzp_user = GetUser(finder.webLogin);
                finder.RolesVal = GetUserRoles(finder.nzp_user);

                Returns ret;
                var cliAdres = new cli_AdresHard(0);
              //  var areaList = cliAdres.GetArea(new Finder { webLogin = context.User.Identity.Name, pref = pref }, out ret);
                var areaList = cliAdres.GetArea(finder, out ret);

                result.AddRange(areaList.Select(x => new { Id = x.nzp_area, Name = x.area }).ToArray());

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