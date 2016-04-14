using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Net;
using System.Security.Principal;

using System.Collections.Generic;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Global;
using STCLINE.KP50.Server;
using STCLINE.KP50.Client;

namespace STCLINE.KP50.Server
{
    //----------------------------------------------------------------------
    public class srv_Serv: srv_Base, I_Serv //сервис Услуг
    //----------------------------------------------------------------------
    {
        //----------------------------------------------------------------------
        public List<Service> FindLsServices(Service finder, enSrvOper oper, out Returns ret)
        //----------------------------------------------------------------------
        {
           
            List<Service> res = null;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Serv cli = new cli_Serv(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.FindLsServices(finder, oper, out ret);
            }
            else
            {
                using (DbLsServices db = new DbLsServices())
                {
                    switch (oper)
                    {
                        case enSrvOper.SrvFind:
                            res = db.FindLsServices(finder, out ret);
                            break;
                        case enSrvOper.NewFdSrvFind:
                            res = db.NewFdFindLsServices(finder, out ret);
                            break;
                        case enSrvOper.SrvFindLsServicePeriods:
                            res = db.FindLsServicePeriods(finder, out ret);
                            break;
                        case enSrvOper.NewFdSrvFindLsServicePeriods:
                            res = db.NewFdFindLsServicePeriods(finder, out ret);
                            break;
                        case enSrvOper.FindAvailableServices:
                            res = db.FindAvailableServices(finder, out ret);
                            break;
                        case enSrvOper.FindAvailableServNewDoc:
                            res = db.FindAvailableServNewDog(finder, out ret);
                            break;
                        default:
                            ret = new Returns(false, "Неверное наименование операции");
                            res = null;
                            break;
                    }
                }
            }
            return res;
        }
        //----------------------------------------------------------------------
        public List<_Supplier> FindServiceSuppliers(Service finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            List<_Supplier> res;
            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Serv cli = new cli_Serv(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.FindServiceSuppliers(finder, out ret);
            }
            else
            {
                using (var db = new DbLsServices())
                {
                    res = db.FindServiceSuppliers(finder, out ret);
                }
            }
            return res;
        }    //----------------------------------------------------------------------
        public List<_Supplier> FindServDogovorERC(Service finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            List<_Supplier> res;
            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Serv cli = new cli_Serv(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.FindServDogovorERC(finder, out ret);
            }
            else
            {
                using (var db = new DbLsServices())
                {
                    res = db.FindServDogovorERC(finder, out ret);
                }
            }
            return res;
        }
        //----------------------------------------------------------------------
        public List<_Formula> FindSupplierFormuls(Service finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            List<_Formula> res;
            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Serv cli = new cli_Serv(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.FindSupplierFormuls(finder, out ret);
            }
            else
            {
                using (var db = new DbLsServices())
                {
                    res = db.FindSupplierFormuls(finder, out ret);
                }
            }
            return res;
        }

        public List<_Formula> LoadFormulsAllPoints(Service finder, out Returns ret)
        {
            List<_Formula> res;
            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Serv cli = new cli_Serv(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.LoadFormulsAllPoints(finder, out ret);
            }
            else
            {
                using (var db = new DbLsServices())
                {
                    res = db.LoadFormulsAllPoints(finder, out ret);
                }
            }
            return res;
        }
        //----------------------------------------------------------------------
        public Returns SaveService(Service finder, Service primfinder)
        //----------------------------------------------------------------------
        {
            DbLsServices db = new DbLsServices();
            Returns ret = Utils.InitReturns();
            try
            {
                ret = db.SaveService(finder, primfinder);
            }
            catch (Exception ex)
            {
                if (Constants.Debug) MonitorLog.WriteLog("Ошибка SaveService() \n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                ret = new Returns(false, "Ошибка вызова функции сохранения услуги" + (Constants.Viewerror ? ": " + ex.Message : ""));
            }
            db.Close();
            return ret;
        }
        //----------------------------------------------------------------------
        public List<Service> FindDomService(Service finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            List<Service> res = null;
            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Serv cli = new cli_Serv(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.FindDomService(finder, out ret);
            }
            else
            {
                using (var db = new DbLsServices())
                {
                    res = db.FindDomService(finder, out ret);
                }
            }
            return res;
        }
        //----------------------------------------------------------------------
        public List<Service> FindDomServiceNewDog(Service finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            List<Service> res = null;
            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Serv cli = new cli_Serv(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.FindDomServiceNewDog(finder, out ret);
            }
            else
            {
                using (var db = new DbLsServices())
                {
                    res = db.FindDomServiceNewDog(finder, out ret);
                }
            }
            return res;
        }
        //----------------------------------------------------------------------
        public Returns FindLSDomFromDomService(Service finder)
        //----------------------------------------------------------------------
        {
            Returns res = Utils.InitReturns();
            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Serv cli = new cli_Serv(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.FindLSDomFromDomService(finder);
            }
            else
            {
                using (var db = new DbLsServices())
                {
                    res = db.FindLSDomFromDomService(finder);
                }
            }
            return res;
        }

        public List<Service> GetGroupServ(Service finder, out Returns ret)
        {
            List<Service> res;
            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Serv cli = new cli_Serv(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.GetGroupServ(finder, out ret);
            }
            else
            {
                using (var db = new DbLsServices())
                {
                    res = db.GetGroupServ(finder, out ret);
                    //res = null;
                    //ret = Utils.InitReturns();
                }
            }
            return res;
        }

        public Returns ServiceIntoServpriority(ServPriority finder, enSrvOper oper)
        {
            
            Returns ret = Utils.InitReturns();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Serv cli = new cli_Serv(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.ServiceIntoServpriority(finder, oper);
            }
            else
            {
                using (var db = new DbLsServices())
                {
                    switch (oper)
                    {
                        case enSrvOper.SrvAdd:
                            ret = db.AddServiceIntoServpriority(finder);
                            break;
                        case enSrvOper.SrvChangePrioritet:
                            ret = db.ChangeOrderIntoServpriority(finder);
                            break;
                        case enSrvOper.srvDelete:
                            ret = db.DeleteServiceFromServpriority(finder);
                            break;
                        default:
                            ret = new Returns(false, "Неверное наименование операции");
                            break;
                    }
                }
            }
            return ret;
        }

        public List<ServPriority> GetServpriority(ServPriority finder, enSrvOper oper, out Returns ret)
        {
            
            ret = Utils.InitReturns();
            List<ServPriority> list = new List<ServPriority>();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Serv cli = new cli_Serv(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.GetServpriority(finder, oper, out ret);
            }
            else
            {
                using (var db = new DbLsServices())
                {
                    switch (oper)
                    {
                        case enSrvOper.SrvLoad:
                            list = db.LoadServpriority(finder, out ret);
                            break;
                        case enSrvOper.SrvGet:
                            list = db.GetServicesForAdd(finder, out ret);
                            break;
                        default:
                            ret = new Returns(false, "Неверное наименование операции");
                            break;
                    }
                }
            }
            return list;
        }

        public Returns SaveServFormula(ServFormula finder)
        {
            Returns res = Utils.InitReturns();
            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Serv cli = new cli_Serv(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.SaveServFormula(finder);
            }
            else
            {
                using (var db = new DbLsServices())
                {
                    res = db.SaveServFormula(finder);
                }
            }
            return res; 
        }

        public List<ServFormula> LoadSevFormuls(ServFormula finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<ServFormula> res = null;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Serv cli = new cli_Serv(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.LoadSevFormuls(finder, out ret);
            }
            else
            {
                DbLsServices db = new DbLsServices();
                try
                {
                    res = db.LoadSevFormuls(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LoadSevFormuls() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = null;
                }
                db.Close();
            }
            return res;
        }

        public Returns GetDependencie(out List<Dependencie> Dependencie)
        {
            Returns ret = Utils.InitReturns();
            Dependencie = null;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Serv cli = new cli_Serv(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetDependencie(out Dependencie);
            }
            else
            {
                DbLsServices db = new DbLsServices();
                try
                {
                    ret = db.GetDependencie(out Dependencie);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetDependencie() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public Returns SetDependencie(Dependencie Dependencie)
        {
            Returns ret = Utils.InitReturns();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Serv cli = new cli_Serv(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.SetDependencie(Dependencie);
            }
            else
            {
                DbLsServices db = new DbLsServices();
                try
                {
                    ret = db.SetDependencie(Dependencie);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка SetDependencie() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public Returns FillDependencieList(out List<Returns> lst)
        {
            Returns ret = Utils.InitReturns();
            lst = new List<Returns>();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Serv cli = new cli_Serv(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.FillDependencieList(out lst);
            }
            else
            {
                DbLsServices db = new DbLsServices();
                try
                {
                    ret = db.FillDependencieList(out lst);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка FillDependencieList() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public Returns CopyFormulsToLocalBD(Service finder)
        {
            Returns ret = Utils.InitReturns();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Serv cli = new cli_Serv(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.CopyFormulsToLocalBD(finder);
            }
            else
            {
                DbLsServices db = new DbLsServices();
                try
                {
                    ret = db.CopyFormulsToLocalBD(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка CopyFormulsToLocalBD() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public List<Service> LoadServicesBySupplier(Service finder, out Returns ret)
        {
           ret = Utils.InitReturns();
            List<Service> listServices= new List<Service>();
            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Serv cli = new cli_Serv(WCFParams.AdresWcfHost.CurT_Server);
                listServices = cli.LoadServicesBySupplier(finder, out ret);
            }
            else
            {
                DbLsServices db = new DbLsServices();
                try
                {
                    listServices = db.LoadServicesBySupplier(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LoadServicesBySupplier() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return listServices;
        }


        public double[] CountGilsForCalc(Service finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            double[] countGil = { 0, 0, 0 };
            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Serv cli = new cli_Serv(WCFParams.AdresWcfHost.CurT_Server);
                countGil = cli.CountGilsForCalc(finder, out ret);
            }
            else
            {
                DbLsServices db = new DbLsServices();
                try
                {
                    countGil = db.CountGilsForCalc(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка CountGilsForCalc() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return countGil;
        }
        public List<Service> LoadServicesAndSuppliersForMustCalcLS(Service finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Service> listServices = new List<Service>();
            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Serv cli = new cli_Serv(WCFParams.AdresWcfHost.CurT_Server);
                listServices = cli.LoadServicesAndSuppliersForMustCalcLS(finder, out ret);
            }
            else
            {
                DbLsServices db = new DbLsServices();
                try
                {
                    listServices = db.LoadServicesAndSuppliersForMustCalcLS(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LoadServicesBySupplier() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return listServices;
        }
    }
}
