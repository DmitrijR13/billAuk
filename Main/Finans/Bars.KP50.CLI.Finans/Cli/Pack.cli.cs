using System;
using System.Data;
using System.Collections.Generic;

using STCLINE.KP50.Global;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Interfaces;
using System.ServiceModel;

namespace STCLINE.KP50.Client
{
    public class cli_Pack : cli_Base, I_Pack  //реализация клиента сервиса Кассы
    {
        //I_Pack remoteObject;

        public cli_Pack(int nzp_server)
            : base()
        {
            //_cli_Pack(nzp_server);
        }

        IPackRemoteObject getRemoteObject()
        {
            return getRemoteObject<IPackRemoteObject>(WCFParams.AdresWcfWeb.srvPack);
        }

        //void _cli_Pack(int nzp_server)
        //{
        //    if (Points.IsMultiHost && nzp_server > 0)
        //    {
        //        //определить параметры доступа
        //        _RServer zap = MultiHost.GetServer(nzp_server);
        //        //remoteObject = HostChannel.CreateInstance<I_Pack>(zap.login, zap.pwd, zap.ip_adr + WCFParams.srvPack);
        //        remoteObject = HostChannel.CreateInstance<I_Pack>(zap.login, zap.pwd, zap.ip_adr + WCFParams.AdresWcfWeb.srvPack);
        //    }
        //    else
        //    {
        //        //по-умолчанию
        //        //remoteObject = HostChannel.CreateInstance<I_Pack>(WCFParams.Adres + WCFParams.srvPack);
        //        remoteObject = HostChannel.CreateInstance<I_Pack>(WCFParams.AdresWcfWeb.Adres + WCFParams.AdresWcfWeb.srvPack);
        //    }
        //}

        //~cli_Pack()
        //{
        //    try { if (remoteObject != null) HostChannel.CloseProxy(remoteObject); }
        //    catch { }
        //}

        /// <summary>
        /// Быстрая проверка загрузки оплат из реестра или проверка квитанции от банка
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public FilesImported FastCheck(FilesImported finder, out Returns ret)
        {
            FilesImported result = new FilesImported();
            try
            {
                using (var remoteObject = getRemoteObject())
                {
                    result = remoteObject.FastCheck(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret = new Returns(false, Constants.access_error, Constants.access_code);
                MonitorLog.WriteLog(
                    "Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message,
                    MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog(
                    "Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message,
                    MonitorLog.typelog.Error, 2, 100, true);
            }
            return result;
        }

        /// <summary>
        /// Загрузка квитанции о реестре оплат
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        public Returns UploadKvitReestr(FilesImported finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var remoteObject = getRemoteObject())
                {
                    ret = remoteObject.UploadKvitReestr(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret = new Returns(false, Constants.access_error, Constants.access_code);
                MonitorLog.WriteLog(
                    "Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message,
                    MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog(
                    "Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message,
                    MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public Pack OperateWithPackAndGetIt(PackFinder finder, enSrvOper oper, out Returns ret)
        {
            ret = Utils.InitReturns();
            Pack pack = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    pack = ro.OperateWithPackAndGetIt(finder, oper, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка OperateWithPack\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка OperateWithPack\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return pack;
        }

        public Returns ChangeChoosenPlsInCase(Finder finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.ChangeChoosenPlsInCase(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка ChangeChoosenPlsInCase\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка ChangeChoosenPlsInCase\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public Returns OperateWithPack(PackFinder finder, Pack.PackOperations oper)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.OperateWithPack(finder, oper);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка OperateWithPack\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка OperateWithPack\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public Returns ChangeCasePack(PackFinder finder, List<Pack_ls> list)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.ChangeCasePack(finder, list);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка ChangeCasePack\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка ChangeCasePack\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public Returns UploadPackFromDBF(string nzp_user, string fileName)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.UploadPackFromDBF(nzp_user, fileName);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка UploadPackFromDBF\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка UploadPackFromDBF\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }


        public Returns UploadPackFromWeb(int nzpPack, int nzp_user)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.UploadPackFromWeb(nzpPack, nzp_user);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка UploadPackFromWeb\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка UploadPackFromWeb\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public Returns PutTaskDistribLs(Dictionary<int, int> listPackLs, int nzp_user)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.PutTaskDistribLs(listPackLs, nzp_user);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка PutTaskDistribLs\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка PutTaskDistribLs\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public static Returns DbSaveUniversalFormatToWeb(ref Pack superPack)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                DbPackClient dbpClient = new DbPackClient();
                AddedPacksInfo packsInfo= new AddedPacksInfo();
                ret = dbpClient.SaveUniversalFormatToWeb(ref superPack, ref packsInfo);
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


        /// <summary>
        /// Загрузка файла в универсальном формате
        /// </summary>
        
        /// <returns></returns>
        public string LoadUniversalFormat(string body, string filename)
        {
           var ret = Utils.InitReturns();
            
           string result = "";
            try
            {
                using (var ro = getRemoteObject())
                {
                    result = ro.LoadUniversalFormat(body, filename);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка LoadUniversalFormat\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка LoadUniversalFormat\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return result;
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
            List<Bank> list = new List<Bank>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    list = ro.LoadBankForKassa(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка LoadBankForKassa\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка LoadBankForKassa\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return list;
        }

        public List<Bank> LoadListBanks(Bank finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Bank> list = new List<Bank>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    list = ro.LoadListBanks(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка LoadListBanks\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка LoadListBanks\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return list;
        }

        public Pack_ls OperateWithPackLsAndGetIt(Pack_ls finder, Pack_ls.OperationsWithGetting oper, out Returns ret)
        {
            ret = Utils.InitReturns();
            Pack_ls packls = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    packls = ro.OperateWithPackLsAndGetIt(finder, oper, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка OperateWithPackLsAndGetIt\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка OperateWithPackLsAndGetIt\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return packls;
        }

        public List<Ulica> LoadUlica(Ulica finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Ulica> list = new List<Ulica>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    if (finder.rows > 0) list = ro.LoadUlica(finder, out ret);
                    else
                    {
                        finder.rows = 500;
                        for (ret = new Returns(true, null, finder.skip + 1); finder.skip < ret.tag && ret.result; finder.skip += finder.rows)
                        {
                            IList<Ulica> tmplist = ro.LoadUlica(finder, out ret);
                            if (tmplist != null) list.AddRange(tmplist);
                        }

                    }
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                list = null;
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка LoadUlica\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                list = null;
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка LoadUlica\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return list;
        }

        public List<Ls> GetPackLsList(string finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Ls> list = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    list = ro.GetPackLsList(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetPackLsList\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка GetPackLsList\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return list;
        }

        public List<Dom> LoadDom(Dom finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Dom> list = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    list = ro.LoadDom(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка LoadDom\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка LoadDom\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return list;
        }

        public List<Ls> LoadKvar(Ls finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Ls> list = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    if (finder.rows > 0) list = ro.LoadKvar(finder, out ret);
                    else
                    {
                        finder.skip = 0;
                        finder.rows = 300;
                        list = ro.LoadKvar(finder, out ret);
                        int all = 0;
                        if (ret.result) all = ret.tag;

                        if (all > 0)
                        {
                            int pages = all / finder.rows;
                            if (pages > 1)
                            {
                                if (pages * finder.rows < all) pages++;
                                for (int i = 1; i < pages; i++)
                                {
                                    finder.skip = i * finder.rows;
                                    list.AddRange(ro.LoadKvar(finder, out ret));
                                }
                            }
                        }
                    }
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка LoadKvar\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка LoadKvar\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return list;
        }       

        public List<Pack_errtype> LoadErrorTypes(Finder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Pack_errtype> list = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    list = ro.LoadErrorTypes(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка LoadErrorTypes\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка LoadErrorTypes\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return list;
        }

        public List<Pack_errtype> GetBasketErr(Pack_ls finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Pack_errtype> list = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    list = ro.GetBasketErr(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetBasketErr\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка GetBasketErr\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return list;
        }

        public List<Ls> LoadLsForKassa(Ls finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Ls> ls = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    ls = ro.LoadLsForKassa(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка LoadLsForKassa\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка LoadLsForKassa\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ls;
        }

        public Returns OperateWithPackLs(Pack_ls finder, Pack_ls.OperationsWithoutGetting oper)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.OperateWithPackLs(finder, oper);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка OperateWithPackLs\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка OperateWithPackLs\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public Returns OperateWithListPackLs(List<Pack_ls> finder, enSrvOper oper)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.OperateWithListPackLs(finder, oper);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка OperateWithListPackLs\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка OperateWithListPackLs\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public List<Pack_ls> GetPackLs(Pack_ls finder, enSrvOper oper, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Pack_ls> list = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    list = new List<Pack_ls>();
                    List<Pack_ls> list2;
                    if (finder.rows > 0)
                    {
                        list = ro.GetPackLs(finder, oper, out ret);
                    }
                    else
                    {
                        while (true)
                        {
                        
                            finder.rows = 100;
                            int uploaded = 0;
                            finder.skip = uploaded;
                            list2 = ro.GetPackLs(finder, oper, out ret);
                            if (ret.result)
                            {
                                if (list2 != null && list2.Count > 0)
                                {
                                    list.AddRange(list2);
                                    uploaded += list.Count;
                                }
                                else break;

                                if (ret.tag <= uploaded) break;
                                else finder.skip = uploaded;
                            }
                            else break;
                        }
                    }
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetPackLs\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка GetPackLs\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return list;
        }

        public List<Pack> GetPack(PackFinder finder, enSrvOper oper, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Pack> list = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    list = ro.GetPack(finder, oper, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetPack\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка GetPack\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return list;
        }

        public List<PackStatus> GetPackStatus(PackStatus finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<PackStatus> list = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    list = ro.GetPackStatus(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetPackStatus\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка GetPackStatus\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return list;
        }

        public Returns СreateUploading(FilterForBC finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.СreateUploading(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка СreateUploading\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка СreateUploading\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public Returns SaveCheckSend(int nzpUser, List<FilesUploadingBC> files)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.SaveCheckSend(nzpUser, files);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка SaveCheckSend\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка SaveCheckSend\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public Returns PackLsInCaseChangeMark(Finder finder, List<Pack_ls> listChecked, List<Pack_ls> listUnchecked)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.PackLsInCaseChangeMark(finder, listChecked, listUnchecked);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка PackLsInCaseChangeMark\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка PackLsInCaseChangeMark\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public Returns SaveOperDay(Pack finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.SaveOperDay(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка SaveOperDay\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка SaveOperDay\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public DateTime GetOperDay(out Returns ret)
        {
            ret = Utils.InitReturns();
            DateTime dt = DateTime.MinValue;
            try
            {
                using (var ro = getRemoteObject())
                {
                    dt = ro.GetOperDay(out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetOperDay\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка GetOperDay\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return dt;
        }

        public DateTime GetOperDay()
        {
            Returns ret;
            return GetOperDay(out  ret);
        }

        public Returns CancelPlat(Finder finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.CancelPlat(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка CancelPlat\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка CancelPlat\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        //public Returns CancelPlat2(Finder finder, List<Pack_ls> list)
        //{
        //    Returns ret = Utils.InitReturns();
        //    try
        //    {
        //        using (var ro = getRemoteObject())
        //        {
        //            ret = ro.CancelPlat2(finder, list);
        //        }
        //    }
        //    catch (CommunicationObjectFaultedException ex)
        //    {
        //        ret.result = false;
        //        ret.text = Constants.access_error;
        //        ret.tag = Constants.access_code;
        //        MonitorLog.WriteLog("Ошибка CancelPlat\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
        //    }
        //    catch (Exception ex)
        //    {
        //        ret.result = false;
        //        ret.tag = -1;
        //        ret.text = ex.Message;
        //        MonitorLog.WriteLog("Ошибка CancelPlat\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
        //    }
        //    return ret;
        //}


        public List<Pack_log> GetPackLog(Pack_log finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Pack_log> list = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    list = ro.GetPackLog(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetPackLog\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка GetPackLog\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return list;
        }

        public List<BankRequisites> GetBankRequisites(BankRequisites finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<BankRequisites> list = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    list = ro.GetBankRequisites(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetBankRequisites\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка GetBankRequisites\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return list;
        }

        public List<BankRequisites> NewFdGetBankRequisites(BankRequisites finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<BankRequisites> list = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    list = ro.NewFdGetBankRequisites(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка NewFdGetBankRequisites\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка NewFdGetBankRequisites\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return list;
        }

        public List<BankRequisites> GetRsForERCDogovor(BankRequisites finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<BankRequisites> list = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    list = ro.GetRsForERCDogovor(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetRsForERCDogovor\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка GetRsForERCDogovor\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return list;
        }

        public List<DogovorRequisites> GetDogovorRequisites(DogovorRequisites finder, enSrvOper oper, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<DogovorRequisites> list = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    list = ro.GetDogovorRequisites(finder, oper, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetDogovorRequisites\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка GetDogovorRequisites\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return list;
        }

        public List<DogovorRequisites> GetDogovorERCList(DogovorRequisites finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<DogovorRequisites> list = new List<DogovorRequisites>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    if (finder.rows > 0) list = ro.GetDogovorERCList(finder, out ret);
                    else
                    {
                        finder.rows = 300;
                        for (ret = new Returns(true, null, finder.skip + 1);
                            finder.skip < ret.tag && ret.result;
                            finder.skip += finder.rows)
                        {
                            IList<DogovorRequisites> tmplist = ro.GetDogovorERCList(finder, out ret);
                            if (tmplist != null) list.AddRange(tmplist);
                        }
                    }
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetDogovorERCList\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка GetDogovorERCList\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return list;
        }

        public List<DogovorRequisites> GetDogovorRequisitesSupp(DogovorRequisites finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<DogovorRequisites> list = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    list = ro.GetDogovorRequisitesSupp(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetDogovorRequisites\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка GetDogovorRequisitesSupp\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return list;
        }

        public List<BankRequisites> GetSourceBankList(BankRequisites finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<BankRequisites> list = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    list = ro.GetSourceBankList(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetFaktura\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка GetFaktura\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return list;
        }

        public List<ContractRequisites> GetContractRequisites(ContractRequisites finder, enSrvOper oper, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<ContractRequisites> list = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    list = ro.GetContractRequisites(finder, oper, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetFaktura\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка GetFaktura\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return list;
        }

        /// <summary>
        /// Изменение банк. реквизитов подрядчика
        /// </summary>
        public bool ChangeBankRequisites(BankRequisites finder, enSrvOper oper, out Returns ret)
        {
            ret = Utils.InitReturns();
            bool res = false;
            try
            {
                using (var ro = getRemoteObject())
                {
                    res = ro.ChangeBankRequisites(finder, oper, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка ChangeBankRequisites\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка ChangeBankRequisites\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return res;
        }

        /// <summary>
        /// Изменение реквизитов договора подрядчика
        /// </summary>
        public bool ChangeDogovorRequisites(DogovorRequisites finder, enSrvOper oper, out Returns ret)
        {
            ret = Utils.InitReturns();
            bool res = false;
            try
            {
                using (var ro = getRemoteObject())
                {
                    res = ro.ChangeDogovorRequisites(finder, oper, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка ChangeDogovorRequisites\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка ChangeDogovorRequisites\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return res;
        }

        public bool ChangeDogovorRequisitesSupp(DogovorRequisites finder, enSrvOper oper, out Returns ret)
        {
            ret = Utils.InitReturns();
            bool res = false;
            try
            {
                using (var ro = getRemoteObject())
                {
                    res = ro.ChangeDogovorRequisitesSupp(finder, oper, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка ChangeDogovorRequisitesSupp\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка ChangeDogovorRequisitesSupp\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return res;
        }

        /// <summary>
        /// Изменение реквизитов контракта
        /// </summary>
        public bool ChangeContractRequisites(ContractRequisites finder, enSrvOper oper, out Returns ret)
        {
            ret = Utils.InitReturns();
            bool res = false;
            try
            {
                using (var ro = getRemoteObject())
                {
                    res = ro.ChangeContractRequisites(finder, oper, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка ChangeContractRequisites\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка ChangeContractRequisites\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return res;
        }

        public List<FnSupplier> GetFnSupplier(FnSupplier finder, enSrvOper oper, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<FnSupplier> list = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    list = ro.GetFnSupplier(finder, oper, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetFnSupplier\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка GetFnSupplier\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return list;
        }

        public DataSet GetDistribLog(PackFinder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            DataSet ds = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    ds = ro.GetDistribLog(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetDistribLog\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка GetDistribLog\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ds;
        }

        public ReturnsType FindErrorInPackLs(PackFinder finder)
        {
            ReturnsType ret = new ReturnsType(false);
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.FindErrorInPackLs(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка FindErrorInPackLs\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка FindErrorInPackLs\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsType FindErrorInFnSupplier(PackFinder finder)
        {
            ReturnsType ret = new ReturnsType(false);
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.FindErrorInFnSupplier(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка FindErrorInFnSupplier\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка FindErrorInFnSupplier\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public ReturnsType GenContDistribPayments(Payments finder)
        {
            ReturnsType ret = new ReturnsType(false);
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GenContDistribPayments(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GenContDistribPayments\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка GenContDistribPayments\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public Returns MakeContDistribPayments(Payments finder)
        {
            Returns ret = new Returns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.MakeContDistribPayments(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret = new Returns(false, Constants.access_error, Constants.access_code);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public ReturnsType GenContDistribPaymentsPDF(Payments finder)
        {
            ReturnsType ret = new ReturnsType(false);
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GenContDistribPaymentsPDF(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GenContDistribPayments\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка GenContDistribPayments\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public decimal GetLsSum(Saldo finder, GetLsSumOperations operation, out Returns ret)
        {
            ret = Utils.InitReturns();
            decimal sumKOplate = 0;
            try
            {
                using (var ro = getRemoteObject())
                {
                    sumKOplate = ro.GetLsSum(finder, operation, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetLsSum\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка GetLsSum\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return sumKOplate;
        }

        public List<ChargeForDistribSum> GetSumsForDistrib(ChargeForDistribSum finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<ChargeForDistribSum> list = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    list = ro.GetSumsForDistrib(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetSumsForDistrib\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка GetSumsForDistrib\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return list;
        }

        public Returns SaveManualDistrib(List<ChargeForDistribSum> listfinder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.SaveManualDistrib(listfinder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка SaveManualDistrib\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка SaveManualDistrib\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public Returns DeleteManualDistrib(Pack_ls finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.DeleteManualDistrib(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка DeleteManualDistrib\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка DeleteManualDistrib\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public Returns GetPrincipForManualDistrib(List<ChargeForDistribSum> listfinder, out List<ChargeForDistribSum> res)
        {
            Returns ret = Utils.InitReturns();
            res = new List<ChargeForDistribSum>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetPrincipForManualDistrib(listfinder, out res);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetPrincipForManualDistrib\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка GetPrincipForManualDistrib\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public Returns CreatePackOverPayment(Pack finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.CreatePackOverPayment(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка CreatePackOverPayment\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка CreatePackOverPayment\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public List<FormatBC> GetFormats(int nzpUser, out Returns ret, List<int> formats) {
            ret = Utils.InitReturns();
            var list = new List<FormatBC>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    list = ro.GetFormats(nzpUser, out ret, formats);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetFormats\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка GetFormats\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return list;
        }

        public List<FormatBC> GetFormats(int nzpUser, out Returns ret)
        {
            ret = Utils.InitReturns();
            var list = new List<FormatBC>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    list = ro.GetFormats(nzpUser, out ret, null);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetFormats\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка GetFormats\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return list;
        }

        public FormatBC GetFormat(int nzpUser, int idFormat, out Returns ret) {
            ret = Utils.InitReturns();
            var format = new FormatBC();
            try
            {
                using (var ro = getRemoteObject())
                {
                    format = ro.GetFormat(nzpUser, idFormat, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetFormat\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка GetFormat\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return format;
        }

        public int AddFormat(int nzpUser, string nameFormat, out Returns ret)
        {
            ret = Utils.InitReturns();
            int nzpFormat = 0;
            try
            {
                using (var ro = getRemoteObject())
                {
                    nzpFormat = ro.AddFormat(nzpUser, nameFormat, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка AddFormat\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка AddFormat\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return nzpFormat;
        }

        public Returns SaveFormat(int nzpUser, FormatBC typ)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.SaveFormat(nzpUser, typ);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка SaveFormat\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка SaveFormat\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public Returns DeleteFormat(int nzpUser, int idFormat)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.DeleteFormat(nzpUser, idFormat);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка DeleteFormat\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка DeleteFormat\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public List<TagBC> GetTags(int nzpUser, int indexFormat, out Returns ret)
        {
            ret = Utils.InitReturns();
            var list = new List<TagBC>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    list = ro.GetTags(nzpUser, indexFormat, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetTags\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка GetTags\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return list;
        }

        public TagBC GetTag(int nzpUser, int idTag, out Returns ret)
        {
            ret = Utils.InitReturns();
            var tag = new TagBC();
            try
            {
                using (var ro = getRemoteObject())
                {
                    tag = ro.GetTag(nzpUser, idTag, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetTag\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка GetTag\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return tag;
        }

        public int AddTag(int nzpUser, TagBC tag, out Returns ret)
        {
            ret = Utils.InitReturns();
            int idTag = 0;
            try
            {
                using (var ro = getRemoteObject())
                {
                    idTag = ro.AddTag(nzpUser, tag, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка AddTag\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка AddTag\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return idTag;
        }

        public Returns DeleteTag(int nzpUser, int idTag)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.DeleteTag(nzpUser, idTag);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка DeleteTag\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка DeleteTag\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public Returns SaveTag(int nzpUser, TagBC tag)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.SaveTag(nzpUser, tag);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка SaveTag\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка SaveTag\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public Returns UpTag(int nzpUser, int idTag)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.UpTag(nzpUser, idTag);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка UpTag\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка UpTag\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public Returns DownTag(int nzpUser, int idTag)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.DownTag(nzpUser, idTag);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка DownTag\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка DownTag\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public List<ValueTagBC> GetTagValues(int nzpUser, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<ValueTagBC> list = null; 
            try
            {
                using (var ro = getRemoteObject())
                {
                    list = ro.GetTagValues(nzpUser, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetTagValues\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка GetTagValues\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return list;
        }

        public List<TypeTagBC> GetTagTypes(int nzpUser, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<TypeTagBC> list = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    list = ro.GetTagTypes(nzpUser, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetTagTypes\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка GetTagTypes\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return list;
        }

        //----------------------------------------------------------------------
        public List<FilesUploadingOnWebBC> GetFilesUploading(int nzpUser, int idReestr, int skip, int rows, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<FilesUploadingOnWebBC> list = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    list = ro.GetFilesUploading(nzpUser, idReestr, skip, rows, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetFilesUploading\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка GetFilesUploading\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return list;
        }

        //----------------------------------------------------------------------
        public List<UploadingOnWebBC> GetListUploading(int nzpUser, int skip, int rows, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<UploadingOnWebBC> list = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    list = ro.GetListUploading(nzpUser, skip, rows, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetListUploading\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка GetListUploading\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return list;
        }

        public UploadingOnWebBC GetUploading(int nzpUser, int idReestr, out Returns ret) {
            ret = Utils.InitReturns();
            UploadingOnWebBC uploading = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    uploading = ro.GetUploading(nzpUser, idReestr, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetUploading\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка GetUploading\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return uploading;
        }

        public Returns FormPacksSbPay(EFSReestr finder, PackFinder packfinder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.FormPacksSbPay(finder, packfinder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка FormPacksSbPay\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка FormPacksSbPay\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public Returns UploadChangesServSupp(ReestrChangesServSupp finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.UploadChangesServSupp(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка UploadChangesServSupp\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка UploadChangesServSupp\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public List<ReestrChangesServSupp> GetReestrChangesServSupp(ReestrChangesServSupp finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<ReestrChangesServSupp> list = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    list = ro.GetReestrChangesServSupp(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetReestrChangesServSupp\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка GetReestrChangesServSupp\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return list;
        }

        public Returns CheckingReturnOnPrevDay()
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.CheckingReturnOnPrevDay();
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка CheckingReturnOnPrevDay\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка CheckingReturnOnPrevDay\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public Returns ReDistributePackLs(Finder finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.ReDistributePackLs(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка ReDistributePackLs\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка ReDistributePackLs\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public Pack GetOperDaySettings(Finder packFinder, out Returns ret)
        {
            ret = Utils.InitReturns();
            Pack pack = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    pack = ro.GetOperDaySettings(packFinder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetOperDaySettings\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка GetOperDaySettings\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return pack;
        }

        public Returns SaveOperDaySettings(Pack finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.SaveOperDaySettings(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка SaveOperDaySettings\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка SaveOperDaySettings\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public Returns ChangeOperDay(OperDayFinder finder, out string date_oper, out string filename, out RecordMonth calcmonth)
        {
            date_oper = "";
            filename = ""; 
            calcmonth = Points.CalcMonth;
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.ChangeOperDay(finder, out date_oper, out filename, out calcmonth);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка ChangeOperDay\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка ChangeOperDay\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public List<string> GetRS(Pack_ls finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<string> list = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    list = ro.GetRS(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetRS\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка GetRS\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return list;
        }

        public List<Pack_ls> GetKodSumList(Pack_ls finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Pack_ls> list = new List<Pack_ls>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    list = ro.GetKodSumList(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetKodSumList\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка GetKodSumList\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return list;
        }

        public List<Pack> GetFilesName(Pack finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Pack> list = new List<Pack>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    list = ro.GetFilesName(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetFilesName\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка GetFilesName\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return list;
        }

        public SupplierInfo GetSupplierInfo(SupplierInfo finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            SupplierInfo si = new SupplierInfo();
            try
            {
                using (var ro = getRemoteObject())
                {
                    si = ro.GetSupplierInfo(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetSupplierInfo\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка GetSupplierInfo\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return si;
        }

        public Returns UpdateSupplierScope(SupplierInfo finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.UpdateSupplierScope(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка UpdateSupplierScope\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка UpdateSupplierScope\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public List<int> GetDogovorERCChildsScope(SupplierInfo finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<int> list = new List<int>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    list = ro.GetDogovorERCChildsScope(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetDogovorERCChildsScope\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка GetDogovorERCChildsScope\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return list;
        }

        public List<DogovorRequisites> GetListDogERCByAgentAndPrincip(DogovorRequisites finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<DogovorRequisites> list = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    list = ro.GetListDogERCByAgentAndPrincip(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetListDogERCByAgentAndPrincip\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка GetListDogERCByAgentAndPrincip\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return list;
        }

        public  Returns CheckPackLsToDeleting(Pack_ls finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.CheckPackLsToDeleting(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка CheckPackLsToDeleting\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка CheckPackLsToDeleting\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public List<InfoPayerBankClient> GetInfoPayers(FilterForBC finder, out Returns ret) {
            ret = Utils.InitReturns();
            List<InfoPayerBankClient> spis = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    spis = ro.GetInfoPayers(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetInfoPayers\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка GetInfoPayers\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return spis;
        }

        public List<InfoPayerBankClient> GetTransfersPayer(FilterForBC finder, out Returns ret) {
            ret = Utils.InitReturns();
            List<InfoPayerBankClient> spis = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    spis = ro.GetTransfersPayer(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetTransfersPayer\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка GetTransfersPayer\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return spis;
        }

        public List<InfoPayerBankClient> GetDogovorsWithTransfers(FilterForBC finder, out Returns ret) {
            ret = Utils.InitReturns();
            List<InfoPayerBankClient> spis = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    spis = ro.GetDogovorsWithTransfers(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetDogovorsWithTransfers\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка GetDogovorsWithTransfers\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return spis;
        }

        public Returns SelectOverPayments(OverPaymentsParams finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var remoteObject = getRemoteObject())
                {
                    ret = remoteObject.SelectOverPayments(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret = new Returns(false, Constants.access_error, Constants.access_code);
                MonitorLog.WriteLog(
                    "Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message,
                    MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog(
                    "Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message,
                    MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }
        public List<OverpaymentStatusFinder> GetOverpaymentManStatus(Finder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<OverpaymentStatusFinder> spis = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    spis = ro.GetOverpaymentManStatus(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + 
                    "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + 
                    "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return spis;
        }

        public Returns SetStatusOverpaymentManProc(OverpaymentStatusFinder finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var remoteObject = getRemoteObject())
                {
                    ret = remoteObject.SetStatusOverpaymentManProc(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret = new Returns(false, Constants.access_error, Constants.access_code);
                MonitorLog.WriteLog(
                    "Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message,
                    MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog(
                    "Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message,
                    MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public Returns CheckChoosenOverPyment(OverpaymentStatusFinder finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var remoteObject = getRemoteObject())
                {
                    ret = remoteObject.CheckChoosenOverPyment(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret = new Returns(false, Constants.access_error, Constants.access_code);
                MonitorLog.WriteLog(
                    "Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message,
                    MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog(
                    "Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message,
                    MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public List<SettingsPackPrms> OperateSettingsPack(SettingsPackPrms finder, enSrvOper oper, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<SettingsPackPrms> list = new List<SettingsPackPrms>();
            try
            {
                using (var remoteObject = getRemoteObject())
                {
                    list = remoteObject.OperateSettingsPack(finder, oper, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret = new Returns(false, Constants.access_error, Constants.access_code);
                MonitorLog.WriteLog(
                    "Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message,
                    MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog(
                    "Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message,
                    MonitorLog.typelog.Error, 2, 100, true);
            }
            return list;
        }
    }
}
