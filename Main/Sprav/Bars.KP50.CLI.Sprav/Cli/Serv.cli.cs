using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.DataBase;

namespace STCLINE.KP50.Client
{
    public class cli_Serv : I_Serv  //реализация клиента сервиса Кассы
    {
        I_Serv remoteObject;

        public cli_Serv(int nzp_server)
            : base()
        {
            _cli_Serv(nzp_server);
        }

        void _cli_Serv(int nzp_server)
        {
            if (Points.IsMultiHost && nzp_server > 0)
            {
                //определить параметры доступа
                _RServer zap = MultiHost.GetServer(nzp_server);
                //remoteObject = HostChannel.CreateInstance<I_Serv>(zap.login, zap.pwd, zap.ip_adr + WCFParams.srvServ);
                remoteObject = HostChannel.CreateInstance<I_Serv>(zap.login, zap.pwd, zap.ip_adr + WCFParams.AdresWcfWeb.srvServ);
            }
            else
            {
                //по-умолчанию
                //remoteObject = HostChannel.CreateInstance<I_Serv>(WCFParams.Adres + WCFParams.srvServ);
                remoteObject = HostChannel.CreateInstance<I_Serv>(WCFParams.AdresWcfWeb.Adres + WCFParams.AdresWcfWeb.srvServ);
            }
        }

        ~cli_Serv()
        {
            try { if (remoteObject != null) HostChannel.CloseProxy(remoteObject); }
            catch { }
        }

        public List<Service> FindLsServices(Service finder, enSrvOper oper, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Service> list = new List<Service>();
            try
            {
                if (finder.rows > 0) list = remoteObject.FindLsServices(finder, oper, out ret);
                else
                {
                    finder.rows = 500;
                    for (ret = new Returns(true, null, finder.skip + 1); finder.skip < ret.tag && ret.result; finder.skip += finder.rows)
                    {
                        List<Service> tmplist = remoteObject.FindLsServices(finder, oper, out ret);
                        if (tmplist != null) list.AddRange(tmplist);
                    }
                }
                HostChannel.CloseProxy(remoteObject);
                return list;
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка FindLsServices(" + oper.ToString() + ") " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public List<_Supplier> FindServiceSuppliers(Service finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                List<_Supplier> list = remoteObject.FindServiceSuppliers(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return list;
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка FindServiceSuppliers() " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public List<_Supplier> FindServDogovorERC(Service finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                List<_Supplier> list = remoteObject.FindServDogovorERC(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return list;
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка FindServDogovorERC() " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public List<_Formula> FindSupplierFormuls(Service finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                List<_Formula> list = remoteObject.FindSupplierFormuls(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return list;
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка FindSupplierFormuls() " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public List<_Formula> LoadFormulsAllPoints(Service finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                List<_Formula> list = new List<_Formula>();


                if (finder.rows > 0) list = remoteObject.LoadFormulsAllPoints(finder, out ret);
                else
                {
                    finder.rows = 500;
                    for (ret = new Returns(true, null, finder.skip + 1); finder.skip < ret.tag && ret.result; finder.skip += finder.rows)
                    {
                        IList<_Formula> tmplist = remoteObject.LoadFormulsAllPoints(finder, out ret);
                        if (tmplist != null) list.AddRange(tmplist);
                    }

                }
              
                HostChannel.CloseProxy(remoteObject);
                return list;
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка LoadFormulsAllPoints() " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public Returns SaveService(Service finder)
        {
            return SaveService(finder, null);
        }

        public Returns SaveService(Service finder, Service primfinder)
        {
            try
            {
                Returns ret = remoteObject.SaveService(finder, primfinder);
                HostChannel.CloseProxy(remoteObject);
                return ret;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка SaveService() " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return new Returns(false, ex.Message);
            }
        }

        public static string DbMakeWhereString(Service finder, out Returns ret)
        {
            return DbLsServicesClient.MakeWhereString(finder, out ret);
        }

        public List<Service> FindDomService(Service finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                List<Service> list = remoteObject.FindDomService(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return list;
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка FindDomServices()" + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public List<Service> FindDomServiceNewDog(Service finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                List<Service> list = remoteObject.FindDomServiceNewDog(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return list;
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка FindDomServiceNewDog()" + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public Returns FindLSDomFromDomService(Service finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.FindLSDomFromDomService(finder);
                HostChannel.CloseProxy(remoteObject);
                return ret;
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка FindLSDomFromDomService()" + err, MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }

        public List<Service> GetGroupServ(Service finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                List<Service> servs = remoteObject.GetGroupServ(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return servs;
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetGroupServ()" + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public Returns ServiceIntoServpriority(ServPriority finder, enSrvOper oper)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.ServiceIntoServpriority(finder, oper);
                HostChannel.CloseProxy(remoteObject);
                return ret;
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка ServiceIntoServpriority(" + oper.ToString() + ") " + err, MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }

        public List<ServPriority> GetServpriority(ServPriority finder, enSrvOper oper, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<ServPriority> list = new List<ServPriority>();
            try
            {
                list = remoteObject.GetServpriority(finder, oper, out ret);
                HostChannel.CloseProxy(remoteObject);
                return list;
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetServpriority(" + oper.ToString() + ") " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public Returns SaveServFormula(ServFormula finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.SaveServFormula(finder);
                HostChannel.CloseProxy(remoteObject);
                return ret;
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка SaveServFormula()" + err, MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }

        public List<ServFormula> LoadSevFormuls(ServFormula finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                List<ServFormula> spis = remoteObject.LoadSevFormuls(finder, out ret);
                HostChannel.CloseProxy(remoteObject);

                return spis;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка LoadSevFormuls " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public Returns GetDependencie(out List<Dependencie> Dependencie)
        {
            Dependencie = null;
            Returns ret = Utils.InitReturns();
            try
            {
                List<Dependencie> lst = new List<Dependencie>();
                ret = remoteObject.GetDependencie(out lst);
                HostChannel.CloseProxy(remoteObject);
                if (ret.result) Dependencie = lst;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetDependencie " + err, MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
            return ret;
        }

        public Returns SetDependencie(Dependencie Dependencie)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.SetDependencie(Dependencie);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetDependencie " + err, MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
            return ret;
        }

        public Returns FillDependencieList(out List<Returns> lst)
        {
            Returns ret = Utils.InitReturns();
            lst = new List<Returns>();
            try
            {
                ret = remoteObject.FillDependencieList(out lst);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка FillDependencieList " + err, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public Returns CopyFormulsToLocalBD(Service finder)
        {
            Returns ret = Utils.InitReturns();           
            try
            {
                ret = remoteObject.CopyFormulsToLocalBD(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка CopyFormulsToLocalBD " + err, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public List<Service> LoadServicesBySupplier(Service finder, out Returns ret)
        {
            ret = Utils.InitReturns(); 
            List<Service> listServices= new List<Service>();
            try
            {
                    listServices = remoteObject.LoadServicesBySupplier(finder, out ret);
                    HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка функции LoadServicesBySupplier() " + err, MonitorLog.typelog.Error, 2, 100, true);
            }
            return listServices;
        }


        public List<Service> LoadServicesAndSuppliersForMustCalcLS(Service finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Service> listServices = new List<Service>();
            try
            {
                listServices = remoteObject.LoadServicesAndSuppliersForMustCalcLS(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка функции LoadServicesAndSuppliersForMustCalcLS() " + err, MonitorLog.typelog.Error, 2, 100, true);
            }
            return listServices;
        }

        /// <summary>
        /// Кол-во жильцов на которое произведен рассчет
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public double[] CountGilsForCalc(Service finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            double[] countGil = { 0, 0, 0 };
            try
            {
                countGil = remoteObject.CountGilsForCalc(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка функции CountGilsForCalc() " + err, MonitorLog.typelog.Error, 2, 100, true);
            }
            return countGil;
        }
    }
}
