namespace Bars.KP50.Report.Handlers.Parameters
{
    using System;
    using System.Collections.Generic;
    using System.Web;
    using System.Linq;

    using Utils;
    using STCLINE.KP50.Client;
    using STCLINE.KP50.Global;
    using STCLINE.KP50.Interfaces;
    using STCLINE.KP50.DataBase;

    public class BankSupplierHandler : JsonRequestHandler
    {
        public override void ProcessRequest(HttpContext context)
        {
            try
            {
                object data;
                var operationId = context.Request["operationId"].ToStr().ToUpper();
                var selectedValues = context.Request["selectedValues"].To<List<int>>();
                switch (operationId)
                {
                       case "LIST_BANKS":
                        {
                            var finder = new Role {webLogin = context.User.Identity.Name};
                            finder.nzp_user = GetUser(finder.webLogin);
                            finder.RolesVal = GetUserRoles(finder.nzp_user);
                            finder.nzp_role = Constants. role_sql_wp;  ;
                            Returns ret;
                            var prefList = cli_Sprav.DbPointAvailableForRole(finder, out ret);


                            List<_Point> prefs = prefList.FindAll(x => x.nzp_wp == Points.PointList.Find(y => y.nzp_wp == x.nzp_wp).nzp_wp);
                            data = prefs.Count > 0 ? new List<object>(prefs.Select(x => new { Id = x.nzp_wp, Name = x.point.Trim() }).ToArray()) : null;
                            //    data = new List<object>(Points.PointList.Select(x => new { Id = x.nzp_wp, Name = x.point.Trim() }).ToArray());
                            break;
                        }
                    case "LIST_AGENTS":
                        {
                            var allList = new List<Payer>();
                            if (selectedValues.Count == 0)
                            {
                                allList.AddRange(SetAgents(context.User.Identity.Name));
                            }
                            else
                            {
                                foreach (var p in selectedValues)
                                {
                                    allList.AddRange(SetAgents(context.User.Identity.Name, p));
                                }
                            }

                            allList = allList.Distinct(x => x.payer).ToList();
                            data = allList.Select(x => new { Id = x.nzp_payer, Name = x.payer }).ToArray();
 
                            break;
                        }
                    case "LIST_PRINCIPALS":
                        {
                            var allList = new List<Payer>();
                            if (selectedValues.Count == 0)
                            {
                                allList.AddRange(SetPrincipals(context.User.Identity.Name));
                            }
                            else
                            {
                                foreach (var p in selectedValues)
                                {
                                    allList.AddRange(SetPrincipals(context.User.Identity.Name, p));
                                }
                            }

                            allList = allList.Distinct(x => x.payer).ToList();
                            data = allList.Select(x => new { Id = x.nzp_payer, Name = x.payer }).ToArray();

                            break;
                        }
                    case "LIST_SUPPLIERS":
                        {
                            var allList = new List<Payer>();
                            if (selectedValues.Count == 0)
                            {
                                allList.AddRange(SetSupplierReal(context.User.Identity.Name));
                            }
                            else
                            {
                                foreach (var p in selectedValues)
                                {
                                    allList.AddRange(SetSupplierReal(context.User.Identity.Name, p));
                                }
                            }

                            allList = allList.Distinct(x => x.payer).ToList();
                            data = allList.Select(x => new { Id = x.nzp_payer, Name = x.payer }).ToArray();

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

        /// <summary>
        /// Получить список агентов по банку данных
        /// </summary>
        /// <param name="userName">Логин текущего пользователя</param>
        /// <param name="nzpWp">код банка ( если не указан -1)</param>
        private IEnumerable<Payer> SetAgents(string userName, int nzpWp = -1)
        {
            Returns ret;
            var cliSprav = new cli_Sprav(0);
            var finder = new Payer {webLogin = userName};
            finder.nzp_user = GetUser(finder.webLogin);
            finder.RolesVal = GetUserRoles(finder.nzp_user);
            if (nzpWp > 1)
                finder.nzp_wp = nzpWp;
            var locList = cliSprav.PayerBankLoad(finder, enSrvOper.Agent, out ret);
            if (locList == null) return new List<Payer>();
            return locList;
        }

        /// <summary>
        /// Получить список принципалов по банку данных
        /// </summary>
        /// <param name="userName">Логин текущего пользователя</param>
        /// <param name="nzpWp">код банка ( если не указан -1)</param>
        private IEnumerable<Payer> SetPrincipals(string userName, int nzpWp = -1)
        {
            Returns ret;
            var cliSprav = new cli_Sprav(0);
            var finder = new Payer {webLogin = userName};
            finder.nzp_user = GetUser(finder.webLogin);
            finder.RolesVal = GetUserRoles(finder.nzp_user);
            if (nzpWp > 1)
                finder.nzp_wp = nzpWp;
            var locList = cliSprav.PayerBankLoad(finder, enSrvOper.Principal, out ret);
            if (locList == null) return new List<Payer>();
            return locList;
        }

        /// <summary>
        /// Получить список принципалов по банку данных
        /// </summary>
        /// <param name="userName">Логин текущего пользователя</param>
        /// <param name="nzpWp">код банка ( если не указан -1)</param>
        private IEnumerable<Payer> SetSupplierReal(string userName, int nzpWp = -1)
        {
            Returns ret;
            var cliSprav = new cli_Sprav(0);
            var finder = new Payer {webLogin = userName};
            finder.nzp_user = GetUser(finder.webLogin);
            finder.RolesVal = GetUserRoles(finder.nzp_user);
            if (nzpWp > 1)
                finder.nzp_wp = nzpWp;
            var locList = cliSprav.PayerBankLoad(finder, enSrvOper.Supplier, out ret);
            if (locList == null) return new List<Payer>();
            return locList;
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