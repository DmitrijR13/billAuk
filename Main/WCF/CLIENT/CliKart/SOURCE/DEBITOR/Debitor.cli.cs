using System;
using System.Data;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Net;
using System.Security.Principal;
using System.Collections;
using System.Collections.Generic;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Global;

using STCLINE.KP50.Utility;

namespace STCLINE.KP50.Client
{
    public class cli_Debitor : I_Debitor
    {
        I_Debitor remoteObject;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nzp_server">Код удаленного сервера</param>
        /// <param name="nzp_role">Код подсистемы</param>
        public cli_Debitor(int nzp_server)
            : base()
        {
            _cli_Debitor(nzp_server, Constants.roleDebt);
        }

        void _cli_Debitor(int nzp_server, int nzp_role)
        {
            string addrHost = "";
            //определить параметры доступа
            _RServer zap = MultiHost.GetServer(nzp_server);

            if (Points.IsMultiHost && nzp_server > 0)
            {
                addrHost = zap.ip_adr + WCFParams.AdresWcfWeb.srvDebitor;
                remoteObject = HostChannel.CreateInstance<I_Debitor>(zap.login, zap.pwd, addrHost);
            }
            else
            {
                //по-умолчанию
                if (nzp_role == Constants.roleDebt)
                    addrHost = WCFParams.AdresWcfWeb.Adres + WCFParams.AdresWcfWeb.srvDebitor;
                else
                    addrHost = WCFParams.AdresWcfWeb.Adres + WCFParams.AdresWcfWeb.srvLicense;
                zap.rcentr = "<локально>";
                remoteObject = HostChannel.CreateInstance<I_Debitor>(Constants.Login, Constants.Password, addrHost, "Debitor");
            }


            //Попытка открыть канал связи
            try
            {
                ICommunicationObject proxy = remoteObject as ICommunicationObject;
                proxy.Open();
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog(string.Format("Ошибка открытия Proxy {0} по адресу {1}. Сервер {2} (Код РЦ {3}) (Код сервера {4}): {5}",
                                    System.Reflection.MethodBase.GetCurrentMethod().Name,
                                    addrHost,
                                    zap.rcentr,
                                    zap.nzp_rc,
                                    nzp_server,
                                    ex.Message),
                                    MonitorLog.typelog.Error, 2, 100, true);
            }
        }

        ~cli_Debitor()
        {
            try { if (remoteObject != null) HostChannel.CloseProxy(remoteObject); }
            catch { }
        }


        public Deal LoadDealInfo(DealFinder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                Deal dealInfo = remoteObject.LoadDealInfo(finder, out ret);
                HostChannel.CloseProxy(remoteObject);

                return dealInfo;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка LoadDealInfo " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public List<Agreement> GetAgreements(DealFinder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                List<Agreement> agrs = remoteObject.GetAgreements(finder, out ret);
                HostChannel.CloseProxy(remoteObject);

                return agrs;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetAgreements " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
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
                remoteObject.AgreementOpers(finder, oper ,out ret);
                HostChannel.CloseProxy(remoteObject);                
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка SaveGetArg " + err, MonitorLog.typelog.Error, 2, 100, true);                
            }
        }

        #region Справочник статусов Дела
        public List<Deal> GetDealStatuses(DealFinder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                List<Deal> agrs = remoteObject.GetDealStatuses(finder, out ret);
                HostChannel.CloseProxy(remoteObject);

                return agrs;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetDealStatuses " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }
        #endregion

        #region Сохранение изменений в Деле
        public Returns SaveDealChanges(Deal finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.SaveDealChanges(finder);
                HostChannel.CloseProxy(remoteObject);

                return ret;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка SaveDealChanges " + err, MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }
        #endregion

        #region Сохранение изменений в Долге
        public Returns SaveDebtChanges(Deal finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.SaveDebtChanges(finder);
                HostChannel.CloseProxy(remoteObject);

                return ret;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка SaveDebtChanges " + err, MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }
        #endregion

        #region Справочник статусов Соглашений
        public List<Deal> GetArgStatus(Deal finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                List<Deal> agrs = remoteObject.GetArgStatus(finder, out ret);
                HostChannel.CloseProxy(remoteObject);

                return agrs;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetArgStatus " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }
        #endregion

        #region Справочник управляющих компаний
        public List<SettingsRequisites> GetSettingArea(SettingsRequisites finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                List<SettingsRequisites> area = remoteObject.GetSettingArea(finder, out ret);
                HostChannel.CloseProxy(remoteObject);

                return area;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetSettingArea " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }
        #endregion

        public List<AgreementDetails> GetArgDetail(Agreement finder ,out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                List<AgreementDetails> agrs = remoteObject.GetArgDetail(finder, out ret);
                HostChannel.CloseProxy(remoteObject);

                return agrs;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetArgDetail " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public List<deal_states_history> GetDealHistory(Deal finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                List<deal_states_history> res = remoteObject.GetDealHistory(finder, out ret);
                HostChannel.CloseProxy(remoteObject);

                return res;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetDealHistoory " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        #region отображение списка должников
        public List<Debt> GetDebitors(DebtFinder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                List<Debt> list = remoteObject.GetDebitors(finder, out ret);
                HostChannel.CloseProxy(remoteObject);

                return list;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка LoadDealInfo " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }

        }
        #endregion

        #region отображение списка дел
        public List<Deal> GetDeals(DealFinder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                List<Deal> agrs = remoteObject.GetDeals(finder, out ret);
                HostChannel.CloseProxy(remoteObject);

                return agrs;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetDeals " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }

        }

        #endregion

        public Dictionary<string, Dictionary<int, string>> GetDebitorLists(DealFinder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            Dictionary<string, Dictionary<int, string>> List = null;
            try
            {
                List = remoteObject.GetDebitorLists(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return List;
            }
            catch (Exception ex)
            {
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else
                    ret.text = "Ошибка добавления заявки";

                MonitorLog.WriteLog("Ошибка GetDebitorLists \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return null;
            }
        }

        public lawsuit_Data GetLavsuit(int nzp_lawsuit, int nzp_deal, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                lawsuit_Data Data = remoteObject.GetLavsuit(nzp_lawsuit, nzp_deal, out ret);
                HostChannel.CloseProxy(remoteObject);

                return Data;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetLawsuit " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public void SetLavsuit(lawsuit_Data Data, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                remoteObject.SetLavsuit(Data, out ret);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка SetLawsuit " + err, MonitorLog.typelog.Error, 2, 100, true);
            }
        }

        public void SaveSetting(List<SettingsRequisites> finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                remoteObject.SaveSetting(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка SaveSetting " + err, MonitorLog.typelog.Error, 2, 100, true);
            }
        }

        public void DeleteLavsuit(int nzp_lawsuit, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                remoteObject.DeleteLavsuit(nzp_lawsuit, out ret);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка DeleteLawsuit " + err, MonitorLog.typelog.Error, 2, 100, true);
            }
        }

        public List<DealCharge> GetDealCharges(Deal finder, int yy, int mm, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                List<DealCharge> res = remoteObject.GetDealCharges(finder, yy, mm, out ret);
                HostChannel.CloseProxy(remoteObject);

                return res;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetDealCharges " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }

        }


        #region получить поставщика
        public List<Supplier> GetSupplier(Deal finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                List<Supplier> listSu = remoteObject.GetSupplier(finder, out ret);
                HostChannel.CloseProxy(remoteObject);

                return listSu;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetSupplier " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }
        #endregion

        #region получить услугу
        public List<Service> GetService(Deal finder, int nzp_supp, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                List<Service> listSe = remoteObject.GetService(finder, nzp_supp, out ret);
                HostChannel.CloseProxy(remoteObject);

                return listSe;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetService " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }
        #endregion

        public Returns GetDDLstDealOperations(out lawsuit_Files lstPreCourt, out lawsuit_Files lstCourt)
        {
            Returns ret = Utils.InitReturns();
            lstPreCourt = new lawsuit_Files();
            lstCourt = new lawsuit_Files();

            try
            {
                remoteObject.GetDDLstDealOperations(out lstPreCourt, out lstCourt);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка DeleteLawsuit " + err, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public List<lawsuit_Data> GetLawSuits(Deal finder,  out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                List<lawsuit_Data> res = remoteObject.GetLawSuits(finder, out ret);
                HostChannel.CloseProxy(remoteObject);

                return res;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetLawSuits " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }

        }

        public int AddGroupOperation(Deal finder, int nzp_oper, ReportType type, out Returns ret)
        {
            ret = Utils.InitReturns();
            int res = -1;

            try
            {
                res = remoteObject.AddGroupOperation(finder, nzp_oper, type, out  ret);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка DeleteLawsuit " + err, MonitorLog.typelog.Error, 2, 100, true);
            }
            return res;
        }

        #region Уменьшение величины долга
        public Returns AddPerekidka(Deal finder, decimal money)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.AddPerekidka(finder, money);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;
                MonitorLog.WriteLog("Ошибка AddPerekidka " + err, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }
        #endregion

        public Returns CloseDeal(Deal finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.CloseDeal(finder);
                HostChannel.CloseProxy(remoteObject);

                return ret;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка CloseDeal " + err, MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }

        public Deal CreateDeal(Deal finder, out Returns ret)
        {
             ret = Utils.InitReturns();
             Deal res = new Deal();
            try
            {
                res = remoteObject.CreateDeal(finder, out ret);
                HostChannel.CloseProxy(remoteObject);

                return res;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка CreateDeal " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public decimal GetLawsuitPrice(int nzp_deal, out Returns ret)
        {
            ret = Utils.InitReturns();
            decimal res = -1;
            try
            {
                res = remoteObject.GetLawsuitPrice(nzp_deal, out ret);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetLawsuitPrice " + err, MonitorLog.typelog.Error, 2, 100, true);
            }
            return res;
        }

        public bool ExistDeal(int nzp_kvar, out Returns ret)
        {
            ret = Utils.InitReturns();
            bool res = true;
            try
            {
                res = remoteObject.ExistDeal(nzp_kvar, out ret);
                HostChannel.CloseProxy(remoteObject);

                return res;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка ExistDeal " + err, MonitorLog.typelog.Error, 2, 100, true);
                return false;
            }
        }

         

        public Returns GetAllAgrementsReport(DateTime? dat_s, DateTime? dat_po, int user, int area)
        {
            Returns ret = Utils.InitReturns();           
            try
            {
                return ret = remoteObject.GetAllAgrementsReport(dat_s, dat_po, user, area);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка ExistDeal " + err, MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }
    }
}
