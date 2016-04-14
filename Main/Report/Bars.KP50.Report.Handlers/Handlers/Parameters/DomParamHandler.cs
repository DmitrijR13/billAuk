namespace Bars.KP50.Report.Handlers.Parameters
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using STCLINE.KP50.Client;
    using STCLINE.KP50.DataBase;
    using STCLINE.KP50.Global;
    using STCLINE.KP50.Interfaces;

    public class DomParamsHandler : JsonRequestHandler
    {
        public override void ProcessRequest(System.Web.HttpContext context)
        {
            
            try
            {
                object data = null;
                var finder = new Prm
                {
                    webLogin = context.User.Identity.Name,
                    dopFind = new List<string> { "(2)" } 
                };
                finder.nzp_user = GetUser(finder.webLogin);
                finder.RolesVal = GetUserRoles(finder.nzp_user);
                finder.nzp_role = Constants.role_sql_wp;
                var cliPrm = new cli_Prm(0);
                Returns ret;
                var listPrm = cliPrm.GetPrm(finder, enSrvOper.SrvLoad, out ret) ?? new List<Prm>();
                if (!ret.result) throw new Exception(ret.text);
                data = listPrm.Count > 0
                    ? new List<object>(listPrm.Select(x => new
                    {
                        Id = x.nzp_prm,
                        Name = x.name_prm.Trim()
                    }).ToArray())
                    : null;
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
