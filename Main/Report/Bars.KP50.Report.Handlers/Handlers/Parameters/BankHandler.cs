namespace Bars.KP50.Report.Handlers.Parameters
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using STCLINE.KP50.Client;
    using STCLINE.KP50.DataBase;
    using STCLINE.KP50.Global;
    using STCLINE.KP50.Interfaces;

    public class BankHandler : JsonRequestHandler
    {
        public override void ProcessRequest(System.Web.HttpContext context)
        {
            try
            {
                object data;
                var finder = new Role { webLogin = context.User.Identity.Name };
                finder.nzp_user = GetUser(finder.webLogin);
                finder.RolesVal = GetUserRoles(finder.nzp_user);
                finder.nzp_role = Constants.role_sql_wp; 
                Returns ret;
                var prefList = cli_Sprav.DbPointAvailableForRole(finder, out ret);


                List<_Point> prefs = prefList.FindAll(x => x.nzp_wp == Points.PointList.Find(y => y.nzp_wp == x.nzp_wp).nzp_wp);
                data = prefs.Count > 0 ? new List<object>(prefs.Select(x => new { Id = x.nzp_wp, Name = x.point.Trim() }).ToArray()) : null;
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
