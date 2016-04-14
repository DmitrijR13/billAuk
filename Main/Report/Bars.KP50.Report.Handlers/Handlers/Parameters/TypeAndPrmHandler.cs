using System;
using System.Collections.Generic;
using System.Linq;
using Bars.KP50.Utils;
using STCLINE.KP50.Client;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

// ReSharper disable once CheckNamespace
namespace Bars.KP50.Report.Handlers.Parameters
{
    class TypeAndPrmHandler : JsonRequestHandler
    {
        public override void ProcessRequest(System.Web.HttpContext context) {
            try
            {
                object data = null;
                var operationId = context.Request["operationId"].ToStr().ToUpper();
                var selectedValues = context.Request["selectedValues"].To<List<int>>();
                switch (operationId)
                {
                    case "LIST_TYPES":
                        {
                            data = new List<object>
                        {                                           //номер prm-таблиц
                            new {Id = 1, Name = "Квартирный"},      //1, 3, 18, 19
                            new {Id = 2, Name = "Домовой"},         //2, 4
                            new {Id = 3, Name = "УК"},              //7
                            new {Id = 4, Name = "Отделения"},       //8
                            new {Id = 5, Name = "Поставщика"},      //11
                            new {Id = 6, Name = "Контрагента"},     //9
                            new {Id = 7, Name = "Услуги"},          //12
                            new {Id = 8, Name = "Улицы"},           //6
                            new {Id = 9, Name = "ПУ"},              //17
                            new {Id = 10, Name = "Общесистемные"},  //5, 10
                            new {Id = 11, Name = "Нормативные"},    //13
                            new {Id = 12, Name = "ПСС"}             //15
                        };
                            break;
                        }
                    case "LIST_PRMS":
                        {
                            if (selectedValues != null && selectedValues.Count != 0)
                            {
                                var listPrmByte = new List<int>();
                                foreach (int value in selectedValues)
                                {
                                    switch (value)
                                    {
                                                                                                    //номер prm-таблиц
                                        case 1: listPrmByte.AddRange(new []{1, 3, 18, 19}); break;  //1, 3, 18, 19
                                        case 2: listPrmByte.AddRange(new []{2, 4}); break;          //2, 4
                                        case 3: listPrmByte.Add(7); break;                          //7
                                        case 4: listPrmByte.Add(8); break;                          //8
                                        case 5: listPrmByte.Add(11); break;                         //11
                                        case 6: listPrmByte.Add(9); break;                          //9
                                        case 7: listPrmByte.Add(12); break;                         //12
                                        case 8: listPrmByte.Add(6); break;                          //6
                                        case 9: listPrmByte.Add(17); break;                         //17
                                        case 10: listPrmByte.AddRange(new []{5, 10}); break;        //5, 10
                                        case 11: listPrmByte.Add(13); break;                        //13
                                        case 12: listPrmByte.Add(15); break;                        //15
                                    }
                                }
                                if (listPrmByte.Count != 0)
                                {
                                    var finder = new Prm
                                    {
                                        webLogin = context.User.Identity.Name,
                                        dopFind = new List<string>
                                        {
                                            "(" + listPrmByte
                                                .Select(x => Convert.ToString(x))
                                                .Aggregate((val, next) => val + ("," + next)) + ")"
                                        }
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
                                }
                            }
                            break;
                        }
                }
                SetResponse(context, GetJson(new { success = true, data }));
            }
            catch (Exception ex)
            {
                MonitorLog.WriteException("Ошибка процедуры ProcessRequest ", ex);
            }
        }

        private List<_RolesVal> GetUserRoles(int p) {
            var listRoles = new List<_RolesVal>();
            var db = new DbUserClient();
            db.GetRolesVal(Constants.roleReport, p, ref listRoles);
            db.Close();
            return listRoles;

        }

        private static int GetUser(string userName) {
            var user = new User { login = userName };
            var ret = new DbAdminClient().GetUserBy(ref user, DbAdminClient.UserBy.Login);
            return ret.result ? user.nzp_user : 0;
        }
    }
}
