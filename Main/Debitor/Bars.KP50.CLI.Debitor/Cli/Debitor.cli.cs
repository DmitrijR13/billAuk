using System;
using System.ServiceModel;
using System.Collections.Generic;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Global;

namespace STCLINE.KP50.Client
{
    public class cli_Debitor : cli_Base, I_Debitor
    {
        IDebitorRemoteObject getRemoteObject()
        {
            return getRemoteObject<IDebitorRemoteObject>(WCFParams.AdresWcfWeb.srvDebitor);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nzp_server">Код удаленного сервера</param>
        /// <param name="nzp_role">Код подсистемы</param>
        public cli_Debitor(int nzp_server)
            : base()
        {
            //_cli_Debitor(nzp_server, Constants.roleDebt);
        }

        //void _cli_Debitor(int nzp_server, int nzp_role)
        //{
        //    string addrHost = "";
        //    //определить параметры доступа
        //    _RServer zap = MultiHost.GetServer(nzp_server);

        //    if (Points.IsMultiHost && nzp_server > 0)
        //    {
        //        addrHost = zap.ip_adr + WCFParams.AdresWcfWeb.srvDebitor;
        //        remoteObject = HostChannel.CreateInstance<I_Debitor>(zap.login, zap.pwd, addrHost);
        //    }
        //    else
        //    {
        //        //по-умолчанию
        //        if (nzp_role == Constants.roleDebt)
        //            addrHost = WCFParams.AdresWcfWeb.Adres + WCFParams.AdresWcfWeb.srvDebitor;
        //        else
        //            addrHost = WCFParams.AdresWcfWeb.Adres + WCFParams.AdresWcfWeb.srvLicense;
        //        zap.rcentr = "<локально>";
        //        remoteObject = HostChannel.CreateInstance<I_Debitor>(Constants.Login, Constants.Password, addrHost, "Debitor");
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

        //~cli_Debitor()
        //{
        //    try { if (remoteObject != null) HostChannel.CloseProxy(remoteObject); }
        //    catch { }
        //}


        public Deal LoadDealInfo(DealFinder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            Deal dealInfo = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    dealInfo = ro.LoadDealInfo(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка LoadDealInfo\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка LoadDealInfo\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return dealInfo;
        }

        public List<Agreement> GetAgreements(DealFinder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Agreement> agrs = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    agrs = ro.GetAgreements(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetAgreements\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetAgreements\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return agrs;
        }

        /// <summary>
        /// Сохранение соглашения
        /// </summary>
        /// <param name="finder">список расчета. В каждом элементе информация о соглашении</param>
        /// <param name="ret">результат</param>
        public void AgreementOpers(List<AgreementDetails> finder, enSrvOper oper, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ro.AgreementOpers(finder, oper, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка SaveGetArg\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка SaveGetArg\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
        }

        #region Справочник статусов Дела
        public List<Deal> GetDealStatuses(DealFinder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Deal> agrs = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    agrs = ro.GetDealStatuses(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetDealStatuses\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetDealStatuses\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return agrs;
        }
        #endregion

        #region Сохранение изменений в Деле
        public Returns SaveDealChanges(Deal finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.SaveDealChanges(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка SaveDealChanges\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка SaveDealChanges\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }
        #endregion

        #region Сохранение изменений в Долге
        public Returns SaveDebtChanges(Deal finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.SaveDebtChanges(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка SaveDebtChanges\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка SaveDebtChanges\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }
        #endregion

        #region Справочник статусов Соглашений
        public List<Deal> GetArgStatus(Deal finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Deal> agrs = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    agrs = ro.GetArgStatus(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetArgStatus\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetArgStatus\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return agrs;
        }
        #endregion

        #region Справочник управляющих компаний
        public List<SettingsRequisites> GetSettingArea(SettingsRequisites finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<SettingsRequisites> area = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    area = ro.GetSettingArea(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка RemoveUserLock\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка RemoveUserLock\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return area;
        }
        #endregion

        public List<AgreementDetails> GetArgDetail(Agreement finder ,out Returns ret)
        {
            ret = Utils.InitReturns();
            List<AgreementDetails> agrs = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    agrs = ro.GetArgDetail(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetArgDetail\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetArgDetail\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return agrs;
        }

        public List<deal_states_history> GetDealHistory(Deal finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<deal_states_history> res = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    res = ro.GetDealHistory(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetDealHistory\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetDealHistory\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return res;
        }

        #region отображение списка должников
        public List<Debt> GetDebitors(DebtFinder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            // Если поля ЛС, платежный код, nzp_dom, nzp_kvar не заполнены
            if ((finder.num_ls <= 0) && (String.IsNullOrWhiteSpace(finder.pkod) || finder.pkod == "0") &&
                finder.nzp_dom <= 0 && finder.nzp_kvar <= 0)
            {
                // проверяем выбранные банки
                if (finder.nzp_wp == 0 && (finder.dopPointList == null || finder.dopPointList.Count <= 0))
                {
                    ret.text = "В шаблоне поиска по адресу не выбран банк данных";
                    ret.result = false;
                    ret.tag = -1;
                    return new List<Debt>();
                }
            }
            List<Debt> list = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    list = ro.GetDebitors(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetDebitors\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetDebitors\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return list;
        }
        #endregion

        public Returns SaveDealChecked(List<Deal> finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.SaveDealChecked(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка SaveDealChecked\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка SaveDealChecked\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        #region отображение списка дел
        public List<Deal> GetDeals(DealFinder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            // Если поля ЛС, платежный код, nzp_dom, nzp_kvar не заполнены
            if ((finder.num_ls <= 0) && (String.IsNullOrWhiteSpace(finder.pkod) || finder.pkod == "0") &&
                finder.nzp_dom <= 0 && finder.nzp_kvar <= 0)
            {
                // проверяем выбранные банки
                if (finder.nzp_wp == 0 && (finder.dopPointList == null || finder.dopPointList.Count <= 0))
                {
                    ret.text = "В шаблоне поиска по адресу не выбран банк данных";
                    ret.result = false;
                    ret.tag = -1;
                    return new List<Deal>();
                    
                }
            }
            List<Deal> agrs = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    agrs = ro.GetDeals(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetDeals\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetDeals\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return agrs;
        }

        #endregion

        public Dictionary<string, Dictionary<int, string>> GetDebitorLists(DealFinder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            Dictionary<string, Dictionary<int, string>> List = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    List = ro.GetDebitorLists(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetDebitorLists\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка добавления заявки";

                MonitorLog.WriteLog("Ошибка GetDebitorLists\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return List;
        }

        public lawsuit_Data GetLavsuit(int nzp_lawsuit, int nzp_deal, out Returns ret)
        {
            ret = Utils.InitReturns();
            lawsuit_Data Data = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    Data = ro.GetLavsuit(nzp_lawsuit, nzp_deal, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetLawsuit\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetLawsuit\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return Data;
        }

        public void SetLavsuit(lawsuit_Data Data, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ro.SetLavsuit(Data, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка SetLawsuit\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка SetLawsuit\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
        }

        public void SaveSetting(List<SettingsRequisites> finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ro.SaveSetting(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка SaveSetting\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка SaveSetting\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
        }

        public void DeleteLavsuit(int nzp_lawsuit, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ro.DeleteLavsuit(nzp_lawsuit, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка DeleteLawsuit\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка DeleteLawsuit\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
        }

        public List<DealCharge> GetDealCharges(Deal finder, int yy, int mm, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<DealCharge> res = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    res = ro.GetDealCharges(finder, yy, mm, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetDealCharges\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetDealCharges\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return res;
        }


        #region получить поставщика
        public List<Supplier> GetSupplier(Deal finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Supplier> listSu = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    listSu = ro.GetSupplier(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetSupplier\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetSupplier\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return listSu;
        }
        #endregion

        #region получить услугу
        public List<Service> GetService(Deal finder, int nzp_supp, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Service> listSe = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    listSe = ro.GetService(finder, nzp_supp, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetService\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetService\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return listSe;
        }
        #endregion

        public Returns GetDDLstDealOperations(out lawsuit_Files lstPreCourt, out lawsuit_Files lstCourt)
        {
            Returns ret = Utils.InitReturns();
            lstPreCourt = new lawsuit_Files();
            lstCourt = new lawsuit_Files();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetDDLstDealOperations(out lstPreCourt, out lstCourt);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetDDLstDealOperations\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetDDLstDealOperations\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public List<lawsuit_Data> GetLawSuits(Deal finder,  out Returns ret)
        {
            ret = Utils.InitReturns();
            List<lawsuit_Data> res = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    res = ro.GetLawSuits(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetLawSuits\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetLawSuits\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return res;
        }

        public int AddGroupOperation(Deal finder, int nzp_oper, ReportType type, out Returns ret)
        {
            ret = Utils.InitReturns();
            int res = -1;
            try
            {
                using (var ro = getRemoteObject())
                {
                    res = ro.AddGroupOperation(finder, nzp_oper, type, out  ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка DeleteLawsuit\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка DeleteLawsuit\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return res;
        }

        #region Уменьшение величины долга
        public Returns AddPerekidka(Deal finder, decimal money)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.AddPerekidka(finder, money);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка AddPerekidka\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка AddPerekidka\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }
        #endregion

        public Returns CloseDeal(Deal finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.CloseDeal(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка CloseDeal\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка CloseDeal\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public Deal CreateDeal(Deal finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            Deal res = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    res = ro.CreateDeal(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка CreateDeal\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка CreateDeal\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return res;
        }

        public decimal GetLawsuitPrice(int nzp_deal, out Returns ret)
        {
            ret = Utils.InitReturns();
            decimal res = -1;
            try
            {
                using (var ro = getRemoteObject())
                {
                    res = ro.GetLawsuitPrice(nzp_deal, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetLawsuitPrice\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetLawsuitPrice\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return res;
        }

        public bool ExistDeal(int nzp_kvar, out Returns ret)
        {
            ret = Utils.InitReturns();
            bool res = true;
            try
            {
                using (var ro = getRemoteObject())
                {
                    res = ro.ExistDeal(nzp_kvar, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                res = false;
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка ExistDeal\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                res = false;
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка ExistDeal\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return res;
        }

        public string GetDebtorList(ChargeFind finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            string res = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    res = ro.GetDebtorList(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetDebtorList\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetDebtorList\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return res;
        }
    }
}
