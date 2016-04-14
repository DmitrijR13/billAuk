using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Net;
using System.Security.Principal;
using System;

using System.Collections.Generic;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Global;

namespace STCLINE.KP50.Client
{
    public class cli_Sprav : cli_Base, I_Sprav
    {
        //I_Sprav remoteObject;

        ISpravRemoteObject getRemoteObject()
        {
            return getRemoteObject<ISpravRemoteObject>(WCFParams.AdresWcfWeb.srvSprav);
        }

        public cli_Sprav(int nzp_server)
            : base()
        {
            //_cli_Sprav(nzp_server);
        }

        //void _cli_Sprav(int nzp_server)
        //{
        //    string addrHost = "";
        //    //определить параметры доступа
        //    _RServer zap = MultiHost.GetServer(nzp_server);

        //    if (Points.IsMultiHost && nzp_server > 0)
        //    {
        //        addrHost = zap.ip_adr + WCFParams.AdresWcfWeb.srvSprav;
        //        remoteObject = HostChannel.CreateInstance<I_Sprav>(zap.login, zap.pwd, addrHost);
        //    }
        //    else
        //    {
        //        //по-умолчанию
        //        addrHost = WCFParams.AdresWcfWeb.Adres + WCFParams.AdresWcfWeb.srvSprav;
        //        zap.rcentr = "<локально>";
        //        remoteObject = HostChannel.CreateInstance<I_Sprav>(addrHost);
        //    }


        //    //Попытка открыть канал связи
        //    try
        //    {
        //        ICommunicationObject proxy = remoteObject as ICommunicationObject;
        //        proxy.Open();
        //    }
        //    catch (Exception ex)
        //    {
        //        MonitorLog.WriteLog(string.Format("Ошибка открытия Proxy {0} по адресу {1}. Сервер {2} (Код РЦ {3}) (Код сервера {4}): {5}",
        //                            System.Reflection.MethodBase.GetCurrentMethod().Name,
        //                            addrHost,
        //                            zap.rcentr,
        //                            zap.nzp_rc,
        //                            nzp_server,
        //                            ex.Message),
        //                            MonitorLog.typelog.Error, 2, 100, true);
        //    }
        //}

        //~cli_Sprav()
        //{
        //    try { if (remoteObject != null) HostChannel.CloseProxy(remoteObject); }
        //    catch { }
        //}

        //----------------------------------------------------------------------
        public bool TableExists(Finder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            bool res = false;
            try
            {
                using (var ro = getRemoteObject())
                {
                    res = ro.TableExists(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка TableExists\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка при поиске данных";
                MonitorLog.WriteLog("Ошибка TableExists\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return res;
        }

        public Returns WebDataTable(Finder finder, enSrvOper srv)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.WebDataTable(finder, srv);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка WebDataTable\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка при поиске данных";
                MonitorLog.WriteLog("Ошибка WebDataTable\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public List<_Point> PointLoad_WebData(Finder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<_Point> list = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    list = ro.PointLoad_WebData(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка PointLoad_WebData\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка при поиске данных";
                MonitorLog.WriteLog("Ошибка PointLoad_WebData\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return list;
        }

        public List<_Point> PointLoad(out Returns ret, out _PointWebData p)
        {
            ret = Utils.InitReturns();
            List<_Point> list = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    list = ro.PointLoad(out ret, out p);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                p = new _PointWebData(false);
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка PointLoad\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                p = new _PointWebData(false);
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка при поиске данных";
                MonitorLog.WriteLog("Ошибка PointLoad\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return list;
        }

        public string GetInfo(long kod, int tip, out Returns ret)
        {
            ret = Utils.InitReturns();
            string s = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    s = ro.GetInfo(kod, tip, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetInfo\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка при поиске данных";
                MonitorLog.WriteLog("Ошибка GetInfo\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return s;
        }

        public List<_Service> ServiceLoad(Finder finder, out Returns ret)
        {
            Service service = new Service();
            finder.CopyTo(service);
            return ServiceLoad(service, out ret);
        }

        public List<_Service> ServiceLoad(Service finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<_Service> ServiceList = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    ServiceList = ro.ServiceLoad(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка ServiceLoad\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка при поиске данных";
                MonitorLog.WriteLog("Ошибка ServiceLoad\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ServiceList;
        }

        public List<_Service> CountsLoad(Finder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<_Service> CountsList = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    CountsList = ro.CountsLoad(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка CountsLoad\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка при поиске данных";
                MonitorLog.WriteLog("Ошибка CountsLoad\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return CountsList;
        }

        public List<_Service> CountsLoadFilter(Finder finder, out Returns ret, int nzp_kvar)
        {
            ret = Utils.InitReturns();
            List<_Service> CountsList = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    CountsList = ro.CountsLoadFilter(finder, out ret, nzp_kvar);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка CountsLoadFilter\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка при поиске данных";
                MonitorLog.WriteLog("Ошибка CountsLoadFilter\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return CountsList;
        }

        public List<_ResY> ResYLoad(out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    BeforeStartQuery("ResYLoad");
                    ResYs.ResYList = ro.ResYLoad(out ret);
                    AfterStopQuery();
                }
                return ResYs.ResYList;
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка ResYLoad\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка ResYLoad\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return null;
        }

        public List<_TypeAlg> TypeAlgLoad(out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    TypeAlgs.AlgList = ro.TypeAlgLoad(out ret);
                }
                return TypeAlgs.AlgList;
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка TypeAlgLoad\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка TypeAlgLoad\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return null;
        }

        public List<_Help> LoadHelp(int nzp_user, int cur_page, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<_Help> List = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    List = ro.LoadHelp(nzp_user, cur_page, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка LoadHelp\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка LoadHelp\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return List;
        }

        public List<_Supplier> SupplierLoad(Supplier finder, enTypeOfSupp type, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<_Supplier> spis = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    spis = ro.SupplierLoad(finder, type, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка SupplierLoad\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка SupplierLoad\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return spis;
        }

        public List<_Supplier> SupplierLoad(Finder finder, enTypeOfSupp type, out Returns ret)
        {
            Supplier supp = new Supplier();
            finder.CopyTo(supp);
            return SupplierLoad(supp, type, out ret);
        }

        public List<_Supplier> LoadSupplierByArea(Supplier finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<_Supplier> spis = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    spis = ro.LoadSupplierByArea(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка SupplierLoad\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка SupplierLoad\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return spis;
        }

        public List<ContractClass> ContractsLoad(ContractFinder finder, enTypeOfSupp type, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<ContractClass> spis = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    spis = ro.ContractsLoad(finder, type, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка ContractsLoad\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка ContractsLoad\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return spis;
        }

        public List<ContractClass> NewFdContractsLoad(ContractFinder finder, enTypeOfSupp type, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<ContractClass> spis = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    spis = ro.NewFdContractsLoad(finder, type, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка NewFdContractsLoad\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка NewFdContractsLoad\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return spis;
        }

        public List<int> BanksForOneSuppLoad(Supplier finder, out Returns ret, out bool IfCanChangePayers)
        {
            ret = Utils.InitReturns();
            IfCanChangePayers = false;
            List<int> spis = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    spis = ro.BanksForOneSuppLoad(finder, out ret, out IfCanChangePayers);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка BanksForOneSuppLoad\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка BanksForOneSuppLoad\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return spis;
        }

        public List<Payer> PayersLoad(Payer finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Payer> spis = null;
            try
            {
                using (var ro = getRemoteObject())
                {   
                    if (finder.rows > 0) spis = ro.PayersLoad(finder, out ret);
                    else
                    {
                        spis = new List<Payer>();
                        finder.rows = 100;
                        for (ret = new Returns(true, null, finder.skip + 1); finder.skip < ret.tag && ret.result; finder.skip += finder.rows)
                        {
                            IList<Payer> tmplist = ro.PayersLoad(finder, out ret);
                            if (tmplist != null) spis.AddRange(tmplist);
                        }

                    }
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка PayersLoad\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка PayerLoad\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return spis;
        }



        //----------------------------------------------------------------------
        public List<FileName> FileNameLoad(FileName finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<FileName> spis = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    spis = ro.FileNameLoad(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка FileNameLoad\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка FileNameLoad\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return spis;
        }
        public List<FileName> FileNameLoad(Finder finder, out Returns ret)
        {
            FileName filename = new FileName();
            return FileNameLoad(filename, out ret);
        }

        //----------------------------------------------------------------------
        public List<Payer> PayerLoad(Payer finder, out Returns ret)
        {
            return PayerBankLoad(finder, enSrvOper.Payer, out ret);
        }

        public List<Payer> BankLoad(Payer finder, out Returns ret)
        {
            return PayerBankLoad(finder, enSrvOper.Bank, out ret);
        }

        public List<Payer> PayerBankLoad(Payer finder, enSrvOper oper, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Payer> spis = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    spis = ro.PayerBankLoad(finder, oper, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка PayerBankLoad\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка PayerBankLoad\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return spis;
        }

        public List<Payer> BankPayerLoad(Payer finder,  out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Payer> spis = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    spis = ro.BankPayerLoad(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка BankPayerLoad\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка BankPayerLoad\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return spis;
        }


        public List<Payer> LoadPayers(Payer finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Payer> spis = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    if (finder.rows > 0) spis = ro.LoadPayers(finder, out ret);
                    else
                    {
                        spis = new List<Payer>();
                        finder.rows = 100;
                        for (ret = new Returns(true, null, finder.skip + 1); finder.skip < ret.tag && ret.result; finder.skip += finder.rows)
                        {
                            IList<Payer> tmplist = ro.LoadPayers(finder, out ret);
                            if (tmplist != null) spis.AddRange(tmplist);
                        }

                    }
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка LoadPayers\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка LoadPayers\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return spis;
        }

        public List<Payer> LoadPayersNewFd(Payer finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Payer> spis = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    if (finder.rows > 0) spis = ro.LoadPayersNewFd(finder, out ret);
                    else
                    {
                        spis = new List<Payer>();
                        finder.rows = 100;
                        for (ret = new Returns(true, null, finder.skip + 1); finder.skip < ret.tag && ret.result; finder.skip += finder.rows)
                        {
                            IList<Payer> tmplist = ro.LoadPayersNewFd(finder, out ret);
                            if (tmplist != null) spis.AddRange(tmplist);
                        }

                    }
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка LoadPayersNewFd\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка LoadPayersNewFd\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return spis;
        }

        public void PayerBankForIssrpF101(out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ro.PayerBankForIssrpF101(out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка PayerBankForIssrpF101\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка PayerBankForIssrpF101\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
        }


        public List<Payer> PayerBankLoadContract(Payer finder, enSrvOper oper, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Payer> spis = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    spis = ro.PayerBankLoadContract(finder, oper, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка PayerBankLoadContract\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка PayerBankLoadContract\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return spis;
        }

        /*
        /// <summary>
        /// Загрузка списка банков(подрядчиков)
        /// </summary>
        public List<Bank> BankLoad(Bank finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                List<Bank> spis = remoteObject.BankLoad(finder, out ret);
                HostChannel.CloseProxy(remoteObject);

                return spis;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка BankLoad " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }
        */

        /// <summary>
        /// Загрузка списка namereg
        /// </summary>
        public List<Namereg> NameregLoad(Namereg finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Namereg> spis = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    spis = ro.NameregLoad(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка NameregLoad\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка NameregLoad\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return spis;
        }

        public List<Payer> LoadPayerTypes(Finder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Payer> spis = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    spis = ro.LoadPayerTypes(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка LoadPayerTypes\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка LoadPayerTypes\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return spis;
        }

        public Returns ContrRenameDog(Payer finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.ContrRenameDog(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка ContrRenameDog\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка ContrRenameDog\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        /*/// <summary>
        /// Получить файл отчета
        /// </summary>
        public string GetFileRep(Int64 nzp)
        {

            return remoteObject.GetFileRep(nzp);
        }*/

        //потом исправить для мультихостинга
        public static _Point DbGetPoint(int nzp_wp)
        {
            /*
            DbSprav db = new DbSprav();
            _Point res = db.GetPoint(nzp_wp);
            db.Close();
            return res;
            */
            return Points.GetPoint(nzp_wp);
        }

        public _Point DbGetPoint(string pref)
        {
            return Points.GetPoint(pref);
        }

        public string DbGetSpravFile(out Returns ret, Int64 nzp_act)
        {
            DbSpravClient db = new DbSpravClient();
            string res = db.GetSpravFile(out ret, nzp_act);
            db.Close();
            return res;
        }

        public List<_Service> DbServiceLoad_WebData(Finder finder, out Returns ret)
        {
            DbSpravClient db = new DbSpravClient();
            List<_Service> list = db.ServiceLoad_WebData(finder, out ret);
            db.Close();
            return list;
        }

        public List<_Supplier> DbSupplierLoad_WebData(Finder finder, out Returns ret)
        {
            DbSpravClient db = new DbSpravClient();
            List<_Supplier> list = db.SupplierLoad_WebData(finder, out ret);
            db.Close();
            return list;
        }

        public List<int> DbPointByAreaLoad(Dom finder, out Returns ret)
        {
            DbSpravClient db = new DbSpravClient();
            List<int> list = db.PointByAreaLoad(finder, out ret);
            db.Close();
            return list;
        }

        public List<Payer> DbPayerBankLoad_WebData(Payer finder, bool is_bank, out Returns ret)
        {
            DbSpravClient db = new DbSpravClient();
            List<Payer> list = db.PayerBankLoad_WebData(finder, is_bank, out ret);
            db.Close();
            return list;
        }

        public static List<_Service> DbServiceAvailableForRole(Role finder, out Returns ret)
        {
            List<_Service> res;
            using (DbSpravClient db = new DbSpravClient())
            {
                res = db.ServiceAvailableForRole(finder, out ret);
            }
            return res;
        }

        public static List<_Supplier> DbSupplierAvailableForRole(Role finder, out Returns ret)
        {
            List<_Supplier> res;
            using (DbSpravClient db = new DbSpravClient())
            {
                res = db.SupplierAvailableForRole(finder, out ret);
            }
            return res;
        }

        public static List<_Point> DbPointAvailableForRole(Role finder, out Returns ret)
        {
            List<_Point> res;
            using (DbSpravClient db = new DbSpravClient())
            {
                res = db.PointAvailableForRole(finder, out ret);
            }
            return res;
        }
        public static List<_Prm> DbPrmAvailableForRole(Role finder, out Returns ret)
        {
            List<_Prm> res;
            using (DbSpravClient db = new DbSpravClient())
            {
                res = db.PrmAvailableForRole(finder, out ret);
            }
            return res;
        }

        public List<_Help> DbLoadHelp(int nzp_user, int cur_page, out Returns ret)
        {
            DbSpravClient db = new DbSpravClient();
            List<_Help> res = db.LoadHelp(nzp_user, cur_page, out ret);
            db.Close();
            return res;
        }

        public static void DbStartApp()
        {
            Constants.Pages.Clear();
            Constants.Actions.Clear();
            Constants.ActShow.Clear();

            DbSpravClient db = new DbSpravClient();
            try
            {
                db.StartApp();
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка при старте веб-приложения\n" + ex.Message, MonitorLog.typelog.Error, true);
            }
            finally
            {
                db.Close();
            }
        }

        public Returns InitApplication()
        {
            Returns ret = Utils.InitReturns();

            if (Points.IsMultiHost)
            {
                //загрузка списка доступных хостов
                //DbStartMulti(out ret);
                DbMultiHostClient db = new DbMultiHostClient();
                ret = db.LoadMultiHost();
                db.Close();

                Points.isInitSuccessfull = true;
            }
            else
            {
                Points.PointList.Clear();
                ResYs.ResYList.Clear();

                //загрузка данных из основной базы (points,расчетные месяцы, etc)
                _PointWebData p = new _PointWebData(false);
                try
                {
                    using (var ro = getRemoteObject())
                    {
                        Points.PointList = ro.PointLoad(out ret, out p);
                    }
                }
                catch (CommunicationObjectFaultedException ex)
                {
                    ret.result = false;
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                    MonitorLog.WriteLog("Ошибка CreateExcelReport_host\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.tag = -1;
                    ret.text = ex.Message;
                    MonitorLog.WriteLog("Ошибка CreateExcelReport_host\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }

                Points.SetPointWebData(p);

                if (Points.PointList == null || Points.PointList.Count == 0) //нет доступа к сервису, заполним по-умолчанию
                {
                    _Point zap = new _Point();
                    zap.nzp_wp = Constants.DefaultZap;
                    zap.point = "Локальный банк";
                    zap.pref = Points.Pref;

                    Points.PointList = new List<_Point>();
                    Points.PointList.Add(zap);
                }

                //состояние лицевого счета
                if (ret.result)
                {
                    try
                    {
                        using (var ro = getRemoteObject())
                        {
                            ResYs.ResYList = ro.ResYLoad(out ret);
                        }
                    }
                    catch (CommunicationObjectFaultedException ex)
                    {
                        ret.result = false;
                        ret.text = Constants.access_error;
                        ret.tag = Constants.access_code;
                        MonitorLog.WriteLog("Ошибка ResYLoad\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    }
                    catch (EndpointNotFoundException ex)
                    {
                        ret.result = false;
                        ret.text = Constants.access_error;
                        ret.tag = Constants.access_code;
                        MonitorLog.WriteLog("Ошибка ResYLoad\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    }
                    catch (Exception ex)
                    {
                        ret.result = false;
                        ret.tag = -1;
                        ret.text = ex.Message;
                        MonitorLog.WriteLog("Ошибка InitApplication()\n" + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                    }
                }

                if (ResYs.ResYList == null || ResYs.ResYList.Count == 0) //нет доступа к сервису, заполним по-умолчанию
                {
                    _ResY zap = new _ResY();
                    zap.nzp_res = Constants.DefaultZap; ;
                    zap.nzp_y = Constants.DefaultZap; ;
                    zap.name_y = "-";

                    ResYs.ResYList = new List<_ResY>();
                    ResYs.ResYList.Add(zap);
                }
            }
            return ret;
        }

        public Returns SaveSupplier(Supplier finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.SaveSupplier(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка SaveSupplier\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка SaveSupplier\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public Returns SaveContract(ContractFinder finder, enSrvOper oper)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.SaveContract(finder, oper);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка SaveContract\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка SaveContract\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public Returns SaveContractAllowOv(Finder finder, enSrvOper oper, List<ContractClass> list)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.SaveContractAllowOv(finder, oper, list);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + 
                    ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + 
                    ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public Returns SavePayer(Payer finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.SavePayer(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка SavePayer\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка SavePayer\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public Returns SavePayerContract(Payer finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.SavePayerContract(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка SavePayerContract\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка SavePayerContract\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public Returns SavePayerContractNewFd(Payer finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.SavePayerContractNewFd(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка SavePayerContractNewFd\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка SavePayerContractNewFd\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public Returns DeletePayerContract(Payer finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.DeletePayerContract(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка DeletePayerContract\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка DeletePayerContract\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public Returns SaveBank(Bank finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.SaveBank(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка SaveBank\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка SaveBank\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public PackDistributionParameters GetPackDistributionParameters(out Returns ret)
        {
            ret = Utils.InitReturns();
            PackDistributionParameters prm = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    prm = ro.GetPackDistributionParameters(out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetPackDistributionParameters\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка GetPackDistributionParameters\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return prm;
        }

        public TDocumentBase GetDocumentBase(TDocumentBase finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            TDocumentBase prm = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    prm = ro.GetDocumentBase(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetDocumentBase\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка GetDocumentBase\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return prm;
        }

        public List<KodSum> GeListKodSum(KodSum finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<KodSum> res = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    res = ro.GeListKodSum(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GeListKodSum\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка GeListKodSum\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return res;
        }

        public static PackDistributionParameters GetPackDistributionParameters(int nzp_server, out Returns ret)
        {
            ret = new Returns(true);
            if (Points.packDistributionParameters != null) return Points.packDistributionParameters;
            else
            {
                cli_Sprav cli = new cli_Sprav(nzp_server);
                return cli.GetPackDistributionParameters(out ret);
            }
        }

        public Returns RefreshSpravClone(Finder finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.RefreshSpravClone(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка RefreshSpravClone\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка RefreshSpravClone\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public List<Town> LoadTown(Town finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Town> spis = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    BeforeStartQuery("LoadTown");
                    spis = ro.LoadTown(finder, out ret);
                    AfterStopQuery();
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка LoadTown\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка LoadTown\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return spis;
        }

        public List<_reestr_unloads> LoadUploadedReestrList(Finder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<_reestr_unloads> spis = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    spis = ro.LoadUploadedReestrList(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка LoadUploadedReestrList\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка LoadUploadedReestrList\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return spis;
        }

        public List<unload_exchange_sz> LoadListExchangeSZ(Finder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<unload_exchange_sz> spis = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    spis = ro.LoadListExchangeSZ(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка LoadListExchangeSZ\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка LoadListExchangeSZ\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return spis;
        }

        public List<_reestr_downloads> LoadDownloadedReestrList(Finder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<_reestr_downloads> spis = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    spis = ro.LoadDownloadedReestrList(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка LoadDownloadedReestrList\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка LoadDownloadedReestrList\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return spis;
        }

        public List<Measure> LoadMeasure(Measure finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Measure> spis = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    spis = ro.LoadMeasure(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка LoadMeasure\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка LoadMeasure\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return spis;
        }

        public List<CalcMethod> LoadCalcMethod(CalcMethod finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<CalcMethod> spis = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    spis = ro.LoadCalcMethod(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка LoadCalcMethod\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка LoadCalcMethod\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return spis;
        }

        public List<Land> LoadLand(Land finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Land> spis = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    spis = ro.LoadLand(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка LoadLand\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка LoadLand\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return spis;
        }

        public List<Stat> LoadStat(Stat finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Stat> spis = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    spis = ro.LoadStat(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка LoadStat\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка LoadStat\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return spis;
        }

        public List<Town> LoadTown2(Town finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Town> spis = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    spis = ro.LoadTown2(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка LoadTown2\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка LoadTown2\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return spis;
        }

        public List<Rajon> LoadRajon(Rajon finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Rajon> spis = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    spis = ro.LoadRajon(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка LoadRajon\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка LoadRajon\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return spis;
        }

        public List<PackTypes> LoadPackTypes(PackTypes finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<PackTypes> spis = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    spis = ro.LoadPackTypes(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка LoadPackTypes\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка LoadPackTypes\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return spis;
        }

        public Returns DeleteReestrTula(_reestr_unloads finder)
        {
            Returns ret;
            ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.DeleteReestrTula(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка DeleteReestrTula\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка DeleteReestrTula\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public Returns DeleteDownloadReestrTula(Finder finder, int nzp_download)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.DeleteDownloadReestrTula(finder, nzp_download);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка DeleteDownloadReestrTula\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка DeleteDownloadReestrTula\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public Returns DeleteDownloadReestrMariyEl(Finder finder, int nzp_download)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.DeleteDownloadReestrMariyEl(finder, nzp_download);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка DeleteDownloadReestrMariyEl\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка DeleteDownloadReestrMariyEl\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }


        public List<BankPayers> BankPayersLoad(Supplier finder, enTypeOfSupp type, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<BankPayers> spis = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    spis = ro.BankPayersLoad(finder, type, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка BankPayersLoad\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка BankPayersLoad\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return spis;
        }

        public List<BankPayers> BankPayersLoadBC(Supplier finder, enTypeOfSupp type, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<BankPayers> spis = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    spis = ro.BankPayersLoadBC(finder, type, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка BankPayersLoadBC\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка BankPayersLoadBC\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return spis;
        }

        public List<Payer> GetPayersDogovor(int nzpUser, Payer.ContragentTypes typePayer, out Returns ret) {
            ret = Utils.InitReturns();
            List<Payer> payers = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    payers = ro.GetPayersDogovor(nzpUser, typePayer, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetPayersDogovor\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка GetPayersDogovor\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return payers;
        }

        public List<Supplier> LoadSupplierSpis(Area_ls finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Supplier> spis = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    spis = ro.LoadSupplierSpis(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка LoadSupplierSpis\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка LoadSupplierSpis\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return spis;
        }

        public List<Bank> GetBanksExecutingPayments(int nzpUser, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Bank> spis = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    spis = ro.GetBanksExecutingPayments(nzpUser, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetBanksExecutingPayments\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка GetBanksExecutingPayments\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return spis;
        }

        public List<BCTypes> LoadBCTypes(BCTypes finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<BCTypes> spis = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    spis = ro.LoadBCTypes(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка LoadBCTypes\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка LoadBCTypes\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return spis;
        }

        public List<Formuls> GetFormuls(FormulsFinder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Formuls> spis = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    spis = ro.GetFormuls(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetFormuls\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка GetFormuls\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return spis;
        }

        public List<tula_s_bank> LoadPayerAgents(Finder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<tula_s_bank> spis = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    spis = ro.LoadPayerAgents(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка LoadPayerAgents\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка LoadPayerAgents\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return spis;
        }

        public Returns DeletePayerAgent(Finder finder, int id)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.DeletePayerAgent(finder, id);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка DeletePayerAgent\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка DeletePayerAgent\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }
        
        public Returns AddPayerAgent(Finder finder, tula_s_bank agent)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.AddPayerAgent(finder, agent);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка AddPayerAgent\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка AddPayerAgent\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public Returns SavePayerAgent(Finder finder, tula_s_bank agent)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.SavePayerAgent(finder, agent);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка SavePayerAgent\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка SavePayerAgent\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }


        public List<Payer> LoadPayersContragents(Payer finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Payer> spis = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    spis = ro.LoadPayersContragents(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка LoadPayerContragents\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка LoadPayerContragents\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return spis;
        }


        //----------------------------------------------------------------------
        public Returns MergeContr(Payer finder, List<int> list)
        //----------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();
            //List<FileName> spis = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.MergeContr(finder, list);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                {
                    MonitorLog.WriteLog("Ошибка MergeContr\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            catch
                (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка MergeContr\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
            }

        public List<int> LoadListUchastok(Finder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            var listUch = new List<int>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    listUch = ro.LoadListUchastok(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                {
                    MonitorLog.WriteLog("Ошибка LoadListUchastok\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            catch
                (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка LoadListUchastok\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return listUch;
        }



        public Returns UpdateCashSpravTable(Finder finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.UpdateCashSpravTable(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                {
                    MonitorLog.WriteLog("Ошибка UpdateCashSpravTable\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            catch
                (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка UpdateCashSpravTable\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public List<Supplier> LoadDogovorByPoints(Finder finder,out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Supplier> list= new List<Supplier>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    list = ro.LoadDogovorByPoints(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                {
                    MonitorLog.WriteLog("Ошибка LoadDogovorByPoints\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            catch
                (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка LoadDogovorByPoints\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return list;
        }
       
        public List<Finder> LoadPointsByScopeDogovor(ScopeAdress finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Finder> list = new List<Finder>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    list = ro.LoadPointsByScopeDogovor(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                {
                    MonitorLog.WriteLog("Ошибка LoadPointsByScopeDogovor\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            catch
                (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка LoadPointsByScopeDogovor\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return list;
        }

        public Returns AddNewToScopeAdress(ScopeAdress finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.AddNewToScopeAdress(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                {
                    MonitorLog.WriteLog("Ошибка AddNewToScopeAdress\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            catch
                (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка AddNewToScopeAdress\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }


        public List<ScopeAdress> GetAdressesByScope(ScopeAdress finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<ScopeAdress> list = new List<ScopeAdress>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    list = ro.GetAdressesByScope(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                {
                    MonitorLog.WriteLog("Ошибка GetAdressesByScope\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            catch
                (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка GetAdressesByScope\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return list;
        }

        public Returns DeleteAdressFromScope(ScopeAdress finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.DeleteAdressFromScope(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                {
                    MonitorLog.WriteLog("Ошибка DeleteAdressFromScope\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            catch
                (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка DeleteAdressFromScope\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public Returns CheckUsingScopeByChilds(ScopeAdress finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.CheckUsingScopeByChilds(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                {
                    MonitorLog.WriteLog("Ошибка CheckUsingScopeByChilds\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            catch
                (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка CheckUsingScopeByChilds\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public Returns SaveSupplierChanges(ContractFinder finder, enSrvOper oper)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.SaveSupplierChanges(finder, oper);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка SaveSupplierChanges\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка SaveSupplierChanges\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public RecordMonth GetCalcMonth()
        {
            RecordMonth rm = new RecordMonth();
            try
            {
                using (var ro = getRemoteObject())
                {
                    rm = ro.GetCalcMonth();
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name +
                    "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name +
                    "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return rm;
        }

        public List<PrmTypes> LoadKodSum(Finder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<PrmTypes> listKodSum = new List<PrmTypes>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    listKodSum = ro.LoadKodSum(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + 
                    "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + 
                    "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return listKodSum;
        }


        /// <summary>
        /// получение справочных значений параметров из resY по nzp_res
        /// </summary>
        /// <param name="find_nzp_res"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public List<_ResY> LoadResY(string find_nzp_res, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<_ResY> listResY = new List<_ResY>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    listResY = ro.LoadResY(find_nzp_res, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name +
                    "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name +
                    "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return listResY;
        }

        public List<PrmTypes> GetListNzpCntServ(Finder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<PrmTypes> ListNzpCntServ = new List<PrmTypes>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ListNzpCntServ = ro.GetListNzpCntServ(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name +
                    "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name +
                    "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ListNzpCntServ;
        }
        public List<OverpaymentForDistrib> GetOverpaymentForDistrib(OverpaymentForDistrib finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            var list = new List<OverpaymentForDistrib>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    list = ro.GetOverpaymentForDistrib(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name +
                    "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return new List<OverpaymentForDistrib>();
            }
            catch (Exception ex)
            {
                ret.result = false;
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name +
                    "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return new List<OverpaymentForDistrib>();
            }
            return list;
        }
        public Returns SaveSelectedOverpaymentForDistrib(OverpaymentForDistrib finder, List<OverpaymentForDistrib> list)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.SaveSelectedOverpaymentForDistrib(finder, list);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name +
                    "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
            catch (Exception ex)
            {
                ret.result = false;
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name +
                    "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public Returns GetSelectedDogForDistribOv(OverpaymentForDistrib finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetSelectedDogForDistribOv(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name +
                    "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
            catch (Exception ex)
            {
                ret.result = false;
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name +
                    "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }
        public Returns InterruptOverpaymentProcess(OverpaymentForDistrib finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.InterruptOverpaymentProcess(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name +
                    "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
            catch (Exception ex)
            {
                ret.result = false;
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name +
                    "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public List<House_kodes> GetAliasDomList(House_kodes finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            var list = new List<House_kodes>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    list = ro.GetAliasDomList(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name +
                    "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return new List<House_kodes>();
            }
            catch (Exception ex)
            {
                ret.result = false;
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name +
                    "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return new List<House_kodes>();
            }
            return list;
        }


        public Returns EditAliasDomList(House_kodes finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.EditAliasDomList(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name +
                                    "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
            catch (Exception ex)
            {
                ret.result = false;
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name +
                                    "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public Returns DeleteAliasDomList(House_kodes finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.DeleteAliasDomList(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name +
                                    "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
            catch (Exception ex)
            {
                ret.result = false;
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name +
                                    "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public Returns RefreshBanksForContract(ScopeAdress finderScopeAdress)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.RefreshBanksForContract(finderScopeAdress);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name +
                                    "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
            catch (Exception ex)
            {
                ret.result = false;
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name +
                                    "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public Returns CheckToOpenServOnLSByAdress(List<ScopeAdress> scopeAdress)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.CheckToOpenServOnLSByAdress(scopeAdress);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name +
                                    "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
            catch (Exception ex)
            {
                ret.result = false;
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name +
                                    "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }
    }
}
