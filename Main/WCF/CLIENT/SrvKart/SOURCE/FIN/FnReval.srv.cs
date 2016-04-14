using System;
using System.Data;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Net;
using System.Security.Principal;
using System.Collections.Generic;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Global;
using STCLINE.KP50.Client;
using STCLINE.KP50.Utility;

namespace STCLINE.KP50.Server
{
    public class srv_FnReval : srv_Base, I_FnReval //сервис Кассы
    {
        public List<FnReval> LoadFnReval(FnRevalFinder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<FnReval> list = null;

            try
            {
                if (SrvRun.isBroker)
                {
                    // продублировать вызов к внутреннему хосту
                    cli_FnReval cli = new cli_FnReval(WCFParams.AdresWcfHost.CurT_Server);
                    list = cli.LoadFnReval(finder, out ret);
                }
                else
                {
                    ReturnsObjectType<DataTable> r = new ClassDB().RunSqlAction(finder, new DbFnReval().GetFnRevalList);
                    ret = r.GetReturns();
                    if (r.returnsData != null)
                        list = OrmConvert.ConvertDataRows<FnReval>(r.returnsData.Rows, DbFnReval.ToFnRevalValue);
                }
            }
            catch (Exception ex)
            {
                ret = Utility.ExceptionUtility.OnException(ex, System.Reflection.MethodBase.GetCurrentMethod().Name);
            }

            return list;
        }

        public List<FnReval> LoadFnRevalSupp(FnRevalFinder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<FnReval> list = null;

            try
            {
                if (SrvRun.isBroker)
                {
                    // продублировать вызов к внутреннему хосту
                    cli_FnReval cli = new cli_FnReval(WCFParams.AdresWcfHost.CurT_Server);
                    list = cli.LoadFnRevalSupp(finder, out ret);
                }
                else
                {
                    ReturnsObjectType<DataTable> r = new ClassDB().RunSqlAction(finder, new DbFnReval().GetFnRevalListSupp);
                    ret = r.GetReturns();
                    if (r.returnsData != null)
                        list = OrmConvert.ConvertDataRows<FnReval>(r.returnsData.Rows, DbFnReval.ToFnRevalValueSupp);
                }
            }
            catch (Exception ex)
            {
                ret = Utility.ExceptionUtility.OnException(ex, System.Reflection.MethodBase.GetCurrentMethod().Name);
            }

            return list;
        }
        
        public Returns OperateWithFnReval(FnReval finder, FnReval.Operations oper)
        {
            Returns ret = Utils.InitReturns();
            
            if (SrvRun.isBroker)
            {
                // продублировать вызов к внутреннему хосту
                cli_FnReval cli = new cli_FnReval(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.OperateWithFnReval(finder, oper);
            }
            else
            {
                try
                {
                    switch (oper)
                    {
                        case FnReval.Operations.Save :
                            ret = new ClassDB().RunSqlAction(finder, new DbFnReval().SaveFnReval).GetReturns(); break;
                        case FnReval.Operations.Delete :
                            ret = new ClassDB().RunSqlAction(finder, new DbFnReval().DelFnReval).GetReturns(); break;
                        default :
                            ret = new Returns(false, "Неверная операция", -1); break;
                    }
                }
                catch (Exception ex)
                {
                    ret = Utility.ExceptionUtility.OnException(ex, System.Reflection.MethodBase.GetCurrentMethod().Name);
                }
            }

            return ret;
        }

        public Returns OperateWithFnRevalSupp(FnReval finder, FnReval.Operations oper)
        {
            Returns ret = Utils.InitReturns();

            if (SrvRun.isBroker)
            {
                // продублировать вызов к внутреннему хосту
                cli_FnReval cli = new cli_FnReval(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.OperateWithFnRevalSupp(finder, oper);
            }
            else
            {
                try
                {
                    switch (oper)
                    {
                        case FnReval.Operations.Save:
                            ret = new ClassDB().RunSqlAction(finder, new DbFnReval().SaveFnRevalSupp).GetReturns(); break;
                        case FnReval.Operations.Delete:
                            ret = new ClassDB().RunSqlAction(finder, new DbFnReval().DelFnRevalSupp).GetReturns(); break;
                        default:
                            ret = new Returns(false, "Неверная операция", -1); break;
                    }
                }
                catch (Exception ex)
                {
                    ret = Utility.ExceptionUtility.OnException(ex, System.Reflection.MethodBase.GetCurrentMethod().Name);
                }
            }

            return ret;
        }
    }
}
