using System;
using System.Data;
using System.Collections.Generic;

using STCLINE.KP50.Global;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.Client
{
    public class cli_Pack : I_Pack  //реализация клиента сервиса Кассы
    {
        I_Pack remoteObject;

        public cli_Pack(int nzp_server)
            : base()
        {
            _cli_Pack(nzp_server);
        }

        void _cli_Pack(int nzp_server)
        {
            if (Points.IsMultiHost && nzp_server > 0)
            {
                //определить параметры доступа
                _RServer zap = MultiHost.GetServer(nzp_server);
                //remoteObject = HostChannel.CreateInstance<I_Pack>(zap.login, zap.pwd, zap.ip_adr + WCFParams.srvPack);
                remoteObject = HostChannel.CreateInstance<I_Pack>(zap.login, zap.pwd, zap.ip_adr + WCFParams.AdresWcfWeb.srvPack);
            }
            else
            {
                //по-умолчанию
                //remoteObject = HostChannel.CreateInstance<I_Pack>(WCFParams.Adres + WCFParams.srvPack);
                remoteObject = HostChannel.CreateInstance<I_Pack>(WCFParams.AdresWcfWeb.Adres + WCFParams.AdresWcfWeb.srvPack);
            }
        }

        ~cli_Pack()
        {
            try { if (remoteObject != null) HostChannel.CloseProxy(remoteObject); }
            catch { }
        }

        public Pack OperateWithPackAndGetIt(PackFinder finder, enSrvOper oper, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                Pack pack = remoteObject.OperateWithPackAndGetIt(finder, oper, out ret);
                HostChannel.CloseProxy(remoteObject);
                return pack;
            }
            catch (Exception ex)
            {
                ret.result = false;

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка OperateWithPack(" + oper.ToString() + ") " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public Returns OperateWithPack(PackFinder finder, Pack.PackOperations oper)
        {
            Returns ret;
            try
            {
                ret = remoteObject.OperateWithPack(finder, oper);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret = new Returns(false);

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка OperateWithPack(" + oper.ToString() + ") " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public Returns UploadPackFromDBF(string nzp_user, string fileName)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.UploadPackFromDBF(nzp_user, fileName);
                HostChannel.CloseProxy(remoteObject);
                return ret;
            }
            catch (Exception ex)
            {
                ret.result = false;

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка UploadPackDBF() " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }


        public Returns UploadPackFromWeb(int nzpPack, int nzp_user)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.UploadPackFromWeb(nzpPack, nzp_user);
                HostChannel.CloseProxy(remoteObject);
                return ret;
            }
            catch (Exception ex)
            {
                ret.result = false;

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка UploadPackFromWeb() " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }

        public Returns PutTaskDistribLs(Dictionary<int, int> listPackLs, int nzp_user)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.PutTaskDistribLs(listPackLs, nzp_user);
                HostChannel.CloseProxy(remoteObject);
                return ret;
            }
            catch (Exception ex)
            {
                ret.result = false;

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка PutTaskDistribLs() " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }

        public static Returns DbSaveUniversalFormatToWeb(ref Pack superPack)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                DbPackClient dbpClient = new DbPackClient();
                ret = dbpClient.SaveUniversalFormatToWeb(ref superPack);
                return ret;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка SaveUniversalFormatToWeb() " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }


        public Returns DBUploadDBFPacktoCache(Finder finder, DataTable dt)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                DbPackClient dbPack = new DbPackClient();
                ret = dbPack.UploadDBFPacktoCache(finder, dt);
                dbPack.Close();
                return ret;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка UploadDBFPacktoCache() " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }

        public List<Bank> LoadBankForKassa(Bank finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                List<Bank> list = remoteObject.LoadBankForKassa(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return list;
            }
            catch (Exception ex)
            {
                ret.result = false;

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка LoadBankForKassa() " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public Pack_ls OperateWithPackLsAndGetIt(Pack_ls finder, Pack_ls.OperationsWithGetting oper, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                Pack_ls packls = remoteObject.OperateWithPackLsAndGetIt(finder, oper, out ret);
                HostChannel.CloseProxy(remoteObject);
                return packls;
            }
            catch (Exception ex)
            {
                ret.result = false;

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка OperateWithPackLsAndGetIt(" + oper.ToString() + ") " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public List<Ulica> LoadUlica(Ulica finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                List<Ulica> tmplist;
                List<Ulica> list = new List<Ulica>();
                if (finder.rows > 0) 
                    list = remoteObject.LoadUlica(finder, out ret);
                else
                {
                    finder.rows = 500;

                    int _skip = 0;
                    while (true)
                    {
                        tmplist = remoteObject.LoadUlica(finder, out ret);
                        if (!ret.result) break;
                        if (tmplist != null)
                            list.AddRange(tmplist);


                        _skip += finder.rows;
                        finder.skip = _skip;

                        if (_skip >= ret.tag) break;
                    }
                }
                HostChannel.CloseProxy(remoteObject);
                return list;
            }
            catch (Exception ex)
            {
                ret.result = false;

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;
                HostChannel.CloseProxy(remoteObject);
                MonitorLog.WriteLog("Ошибка LoadUlica() " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public List<Ls> GetPackLsList(string finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                List<Ls> list = remoteObject.GetPackLsList(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return list;
            }
            catch (Exception ex)
            {
                ret.result = false;

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetPackLsList() " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public List<Dom> LoadDom(Dom finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                List<Dom> list = remoteObject.LoadDom(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return list;
            }
            catch (Exception ex)
            {
                ret.result = false;

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка LoadDom() " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public List<Ls> LoadKvar(Ls finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                List<Ls> list = new List<Ls>();
                if (finder.rows > 0) list = remoteObject.LoadKvar(finder, out ret);
                else
                {
                    finder.skip = 0;
                    finder.rows = 300;
                    list = remoteObject.LoadKvar(finder, out ret);
                    int all = 0;
                    if (ret.result) all = ret.tag;

                    if (all > 0)
                    {
                        int pages = all / finder.rows;
                        if (pages > 1)
                        {
                            if (pages * finder.rows < all) pages++;
                            for(int i=1; i<pages;i++)
                            {
                                finder.skip = i * finder.rows;
                                list.AddRange(remoteObject.LoadKvar(finder, out ret));
                            }
                        }
                    }
                }

                HostChannel.CloseProxy(remoteObject);
                return list;
            }
            catch (Exception ex)
            {
                ret.result = false;

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка LoadKvar() " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }       

        public List<Pack_errtype> LoadErrorTypes(Finder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                List<Pack_errtype> list = remoteObject.LoadErrorTypes(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return list;
            }
            catch (Exception ex)
            {
                ret.result = false;

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка LoadErrorTypes() " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public List<Pack_errtype> GetBasketErr(Pack_ls finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                List<Pack_errtype> list = remoteObject.GetBasketErr(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return list;
            }
            catch (Exception ex)
            {
                ret.result = false;

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetBasketErr() " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public List<Ls> LoadLsForKassa(Ls finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                List<Ls> ls = remoteObject.LoadLsForKassa(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return ls;
            }
            catch (Exception ex)
            {
                ret.result = false;

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка LoadLsForKassa() " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public Returns OperateWithPackLs(Pack_ls finder, Pack_ls.OperationsWithoutGetting oper)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.OperateWithPackLs(finder, oper);
                HostChannel.CloseProxy(remoteObject);
                return ret;
            }
            catch (Exception ex)
            {
                ret.result = false;

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка OperateWithPackLs(" + oper.ToString() + ") " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }

        public Returns OperateWithListPackLs(List<Pack_ls> finder, enSrvOper oper)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.OperateWithListPackLs(finder, oper);
                HostChannel.CloseProxy(remoteObject);
                return ret;
            }
            catch (Exception ex)
            {
                ret.result = false;

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка OperateWithListPackLs(" + oper.ToString() + ") " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }

        public List<Pack_ls> GetPackLs(Pack_ls finder, enSrvOper oper, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                List<Pack_ls> list = remoteObject.GetPackLs(finder, oper, out ret);
                HostChannel.CloseProxy(remoteObject);
                return list;
            }
            catch (Exception ex)
            {
                ret.result = false;

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetPackLs(" + oper.ToString() + ") " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public List<Pack> GetPack(PackFinder finder, enSrvOper oper, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                List<Pack> list = remoteObject.GetPack(finder, oper, out ret);
                HostChannel.CloseProxy(remoteObject);
                return list;
            }
            catch (Exception ex)
            {
                ret.result = false;

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetPack(" + oper.ToString() + ") " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public List<PackStatus> GetPackStatus(PackStatus finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                List<PackStatus> list = remoteObject.GetPackStatus(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return list;
            }
            catch (Exception ex)
            {
                ret.result = false;

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetPackStatus() " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        //----------------------------------------------------------------------
        public Returns BankPayment(FinderAddPackage finder)
        //----------------------------------------------------------------------
        {
            Returns ret;
            try
            {
                ret = remoteObject.BankPayment(finder);
                HostChannel.CloseProxy(remoteObject);

                return ret;
            }
            catch (Exception ex)
            {
                ret = new Returns(false);
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка BankPayment " + err, MonitorLog.typelog.Error, 2, 100, true);

                return ret;
            }
        }

        //----------------------------------------------------------------------
        public Returns SaveCheckSend(Delete_payment finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            try
            {
                ret = remoteObject.SaveCheckSend(finder,out ret);
                HostChannel.CloseProxy(remoteObject);

                return ret;
            }
            catch (Exception ex)
            {
                ret = new Returns(false);
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка SaveCheckSend " + err, MonitorLog.typelog.Error, 2, 100, true);

                return ret;
            }
        }

        //----------------------------------------------------------------------
        public List<BankPayers> FormingSpisok(List<BankPayers> allSpisok, FinderAddPackage finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                List<BankPayers> list = remoteObject.FormingSpisok(allSpisok, finder, out ret);
                HostChannel.CloseProxy(remoteObject);

                return list;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка FormingSpisok " + err, MonitorLog.typelog.Error, 2, 100, true);

                return null;
            }
        }

        public Returns SaveOperDay(Pack finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.SaveOperDay(finder);
                HostChannel.CloseProxy(remoteObject);
                return ret;
            }
            catch (Exception ex)
            {
                ret.result = false;

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка SaveOperDay() " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }

        public Returns CancelPlat(Finder finder, List<Pack_ls> list)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.CancelPlat(finder, list);
                HostChannel.CloseProxy(remoteObject);
                return ret;
            }
            catch (Exception ex)
            {
                ret.result = false;

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка CancelPlat() " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }

        public List<Pack_log> GetPackLog(Pack_log finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                List<Pack_log> list = remoteObject.GetPackLog(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return list;
            }
            catch (Exception ex)
            {
                ret.result = false;

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetPackLog() " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public List<BankRequisites> GetBankRequisites(BankRequisites finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                List<BankRequisites> list = remoteObject.GetBankRequisites(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return list;
            }
            catch (Exception ex)
            {
                ret.result = false;

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка BankRequisites() " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }


        public List<BankRequisites> GetSourceBankList(BankRequisites finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                List<BankRequisites> list = remoteObject.GetSourceBankList(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return list;
            }
            catch (Exception ex)
            {
                ret.result = false;

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetSourceBankList() " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }



        public List<DogovorRequisites> GetDogovorRequisites(DogovorRequisites finder, enSrvOper oper, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                List<DogovorRequisites> list = remoteObject.GetDogovorRequisites(finder, oper ,out ret);
                HostChannel.CloseProxy(remoteObject);
                return list;
            }
            catch (Exception ex)
            {
                ret.result = false;

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetDogovorRequisites() " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }



        public List<ContractRequisites> GetContractRequisites(ContractRequisites finder, enSrvOper oper, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                List<ContractRequisites> list = remoteObject.GetContractRequisites(finder, oper, out ret);
                HostChannel.CloseProxy(remoteObject);
                return list;
            }
            catch (Exception ex)
            {
                ret.result = false;

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetContractRequisites() " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }


        /// <summary>
        /// Изменение банк. реквизитов подрядчика
        /// </summary>
        public bool ChangeBankRequisites(BankRequisites finder, enSrvOper oper, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                bool res = remoteObject.ChangeBankRequisites(finder, oper, out ret);
                HostChannel.CloseProxy(remoteObject);
                return res;
            }
            catch (Exception ex)
            {
                ret.result = false;

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка ChangeBankRequisites(" + oper.ToString() + ") " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return false;
            }
        }

        /// <summary>
        /// Изменение реквизитов договора подрядчика
        /// </summary>
        public bool ChangeDogovorRequisites(DogovorRequisites finder, enSrvOper oper, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                bool res = remoteObject.ChangeDogovorRequisites(finder, oper, out ret);
                HostChannel.CloseProxy(remoteObject);
                return res;
            }
            catch (Exception ex)
            {
                ret.result = false;

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка ChangeDogovorRequisites(" + oper.ToString() + ") " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return false;
            }
        }

        /// <summary>
        /// Изменение реквизитов контракта
        /// </summary>
        public bool ChangeContractRequisites(ContractRequisites finder, enSrvOper oper, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                bool res = remoteObject.ChangeContractRequisites(finder, oper, out ret);
                HostChannel.CloseProxy(remoteObject);
                return res;
            }
            catch (Exception ex)
            {
                ret.result = false;

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка ChangeContractRequisites(" + oper.ToString() + ") " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return false;
            }
        }

        public List<FnSupplier> GetFnSupplier(FnSupplier finder, enSrvOper oper, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                List<FnSupplier> list = remoteObject.GetFnSupplier(finder, oper, out ret);
                HostChannel.CloseProxy(remoteObject);
                return list;
            }
            catch (Exception ex)
            {
                ret.result = false;

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetFnSupplier("+oper.ToString()+") " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public DataSet GetDistribLog(PackFinder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                DataSet ds = remoteObject.GetDistribLog(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return ds;
            }
            catch (Exception ex)
            {
                ret.result = false;

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка getDistribLog" + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public ReturnsType FindErrorInPackLs(PackFinder finder)
        {
            try
            {
                ReturnsType ret = remoteObject.FindErrorInPackLs(finder);
                HostChannel.CloseProxy(remoteObject);
                return ret;
            }
            catch (Exception ex)
            {
                ReturnsType ret = new ReturnsType(false);

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка FindErrorInPackLs" + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }

        public ReturnsType FindErrorInFnSupplier(PackFinder finder)
        {
            try
            {
                ReturnsType ret = remoteObject.FindErrorInFnSupplier(finder);
                HostChannel.CloseProxy(remoteObject);
                return ret;
            }
            catch (Exception ex)
            {
                ReturnsType ret = new ReturnsType(false);

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка FindErrorInFnSupplier" + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }

        public ReturnsType GenContDistribPayments(Payments finder)
        {
            try
            {
                ReturnsType ret = remoteObject.GenContDistribPayments(finder);
                HostChannel.CloseProxy(remoteObject);
                return ret;
            }
            catch (Exception ex)
            {
                ReturnsType ret = new ReturnsType(false);

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GenContDistribPayments" + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }

        public decimal GetLsSum(Saldo finder, GetLsSumOperations operation, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                decimal sumKOplate = remoteObject.GetLsSum(finder, operation, out ret);
                HostChannel.CloseProxy(remoteObject);
                return sumKOplate;
            }
            catch (Exception ex)
            {
                ret.result = false;

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetLsSum(" + operation + ")\n " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return 0;
            }
        }

        public List<ChargeForDistribSum> GetSumsForDistrib(ChargeForDistribSum finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                List<ChargeForDistribSum> list = remoteObject.GetSumsForDistrib(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return list;
            }
            catch (Exception ex)
            {
                ret.result = false;

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetSumsForDistrib() " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public Returns SaveManualDistrib(List<ChargeForDistribSum> listfinder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.SaveManualDistrib(listfinder);
                HostChannel.CloseProxy(remoteObject);
                return ret;
            }
            catch (Exception ex)
            {
                ret.result = false;

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка SaveManualDistrib() " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }

        public Returns CreatePackOverPayment(Pack finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.CreatePackOverPayment(finder);
                HostChannel.CloseProxy(remoteObject);
                return ret;
            }
            catch (Exception ex)
            {
                ret.result = false;

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка CreatePackOverPayment() " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }

        public List<_TypeBC> LoadTypeBC(out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                List<_TypeBC> list = remoteObject.LoadTypeBC(out ret);
                HostChannel.CloseProxy(remoteObject);
                return list;
            }
            catch (Exception ex)
            {
                ret.result = false;

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка LoadTypeBC() " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public Returns AddFormat()
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.AddFormat();
                HostChannel.CloseProxy(remoteObject);
                return ret;
            }
            catch (Exception ex)
            {
                ret.result = false;

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка AddFormat() " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }

        public Returns SaveFormat(_TypeBC typ)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.SaveFormat(typ);
                HostChannel.CloseProxy(remoteObject);
                return ret;
            }
            catch (Exception ex)
            {
                ret.result = false;

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка SaveFormat() " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }

        public Returns DeleteFormat(_TypeBC typ)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.DeleteFormat(typ);
                HostChannel.CloseProxy(remoteObject);
                return ret;
            }
            catch (Exception ex)
            {
                ret.result = false;

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка DeleteFormat() " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }

        public List<_TagsBC> GetListTags(int indexFormat, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                List<_TagsBC> list = remoteObject.GetListTags(indexFormat, out ret);
                HostChannel.CloseProxy(remoteObject);
                return list;
            }
            catch (Exception ex)
            {
                ret.result = false;

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetListTags() " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public Returns AddTag(int indexFormat)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.AddTag(indexFormat);
                HostChannel.CloseProxy(remoteObject);
                return ret;
            }
            catch (Exception ex)
            {
                ret.result = false;

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка AddTag() " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }

        public Returns DeleteTag(int indexTag)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.DeleteTag(indexTag);
                HostChannel.CloseProxy(remoteObject);
                return ret;
            }
            catch (Exception ex)
            {
                ret.result = false;

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка DeleteTag() " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }

        public Returns SaveTag(_TagsBC tag)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.SaveTag(tag);
                HostChannel.CloseProxy(remoteObject);
                return ret;
            }
            catch (Exception ex)
            {
                ret.result = false;

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка SaveTag() " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }

        public Returns UpTag(_TagsBC finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.UpTag(finder);
                HostChannel.CloseProxy(remoteObject);
                return ret;
            }
            catch (Exception ex)
            {
                ret.result = false;

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка UpTag() " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }

        public Returns DownTag(_TagsBC finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.DownTag(finder);
                HostChannel.CloseProxy(remoteObject);
                return ret;
            }
            catch (Exception ex)
            {
                ret.result = false;

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка DownTag() " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }

        public List<CalcMethod> ListBcFields(Finder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                List<CalcMethod> list = remoteObject.ListBcFields(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return list;
            }
            catch (Exception ex)
            {
                ret.result = false;

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка ListBcFields() " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public List<CalcMethod> ListBcRowType(Finder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                List<CalcMethod> list = remoteObject.ListBcRowType(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return list;
            }
            catch (Exception ex)
            {
                ret.result = false;

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка ListBcRowType() " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public Returns FormPacksSbPay(EFSReestr finder, PackFinder packfinder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.FormPacksSbPay(finder, packfinder);
                HostChannel.CloseProxy(remoteObject);
                return ret;
            }
            catch (Exception ex)
            {
                ret.result = false;

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка FormPacksSbPay() " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }

        public Returns UploadChangesServSupp(ReestrChangesServSupp finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.UploadChangesServSupp(finder);
                HostChannel.CloseProxy(remoteObject);
                return ret;
            }
            catch (Exception ex)
            {
                ret.result = false;

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка UploadChangesServSupp() " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }

        public List<ReestrChangesServSupp> GetReestrChangesServSupp(ReestrChangesServSupp finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                List<ReestrChangesServSupp> list = remoteObject.GetReestrChangesServSupp(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return list;
            }
            catch (Exception ex)
            {
                ret.result = false;

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка ReestrChangesServSupp() " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public Returns CheckingReturnOnPrevDay()
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.CheckingReturnOnPrevDay();
                HostChannel.CloseProxy(remoteObject);
                return ret;
            }
            catch (Exception ex)
            {
                ret.result = false;

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка CheckingReturnOnPrevDay() " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }

        public Returns ReDistributePackLs(Finder finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.ReDistributePackLs(finder);
                HostChannel.CloseProxy(remoteObject);
                return ret;
            }
            catch (Exception ex)
            {
                ret.result = false;

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка ReDistributePackLs() " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }

        public Pack GetOperDaySettings(Finder packFinder, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                Pack pack = remoteObject.GetOperDaySettings(packFinder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return pack;
            }
            catch (Exception ex)
            {
                ret.result = false;

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetOperDaySettings() " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public Returns SaveOperDaySettings(Pack finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.SaveOperDaySettings(finder);
                HostChannel.CloseProxy(remoteObject);
                return ret;
            }
            catch (Exception ex)
            {
                ret.result = false;

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка SaveOperDaySettings() " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }

        public Returns ChangeOperDay(OperDayFinder finder, out string date_oper, out string filename, out RecordMonth calcmonth)
        {
            date_oper = "";
            filename = ""; 
            calcmonth = Points.CalcMonth;
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.ChangeOperDay(finder, out date_oper, out filename, out calcmonth);
                HostChannel.CloseProxy(remoteObject);
                return ret;
            }
            catch (Exception ex)
            {
                ret.result = false;

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка ChangeOperDay() " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }

    }
}
