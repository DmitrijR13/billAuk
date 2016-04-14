using System;
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
using System.Data;
using System.Reflection;

namespace STCLINE.KP50.Client
{
    public class cli_Adres : cli_Base, I_Adres
    {
        public cli_Adres(int nzp_server)
            : base()
        {
            //_cli_Adres(nzp_server, 0);
        }

        IAddressRemoteObject getRemoteObject()
        {
            return getRemoteObject<IAddressRemoteObject>(WCFParams.AdresWcfWeb.srvAdres);
        }


        public cli_Adres(int nzp_server, int nzp_role)
            : base()
        {
            //_cli_Adres(nzp_server, nzp_role);
        }

        //void _cli_Adres(int nzp_server, int nzp_role)
        //{
        //    if (Points.IsMultiHost && nzp_server > 0)
        //    {
        //        //определить параметры доступа
        //        _RServer zap = MultiHost.GetServer(nzp_server);
        //        remoteObject = HostChannel.CreateInstance<I_Adres>(zap.login, zap.pwd, zap.ip_adr + WCFParams.AdresWcfWeb.srvAdres);
        //    }
        //    else
        //    {
        //        if (nzp_role == Constants.roleSupg)
        //            remoteObject = HostChannel.CreateInstance<I_Adres>(WCFParams.AdresWcfWeb.SupgAdres + WCFParams.AdresWcfWeb.srvAdres);
        //        else
        //            remoteObject = HostChannel.CreateInstance<I_Adres>(WCFParams.AdresWcfWeb.Adres + WCFParams.AdresWcfWeb.srvAdres);
        //    }
        //}

        //~cli_Adres()
        //{
        //    try { if (remoteObject != null) HostChannel.CloseProxy(remoteObject); }
        //    catch { }
        //}

        List<Ulica> ulSpis = new List<Ulica>();
        List<Dom> domSpis = new List<Dom>();
        _Rekvizit lsRekvizit = new _Rekvizit();

     
    

  
        public void UpdateGroupDom(Dom finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ro.UpdateGroupDom(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка UpdateGroupDom\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса изменения домов групповая операция";

                MonitorLog.WriteLog("Ошибка UpdateGroupDom\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
        }

        //----------------------------------------------------------------------
        public void UpdateGroupLs(Ls finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ro.UpdateGroupLs(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка UpdateGroupLs\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса изменения лицевых счетов групповая операция";

                MonitorLog.WriteLog("Ошибка UpdateGroupLs\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
        }

        //----------------------------------------------------------------------
        public List<Ulica> GetUlica(Dom finder, enSrvOper srv, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    if (finder.rows > 0) ulSpis = ro.GetUlica(finder, srv, out ret);
                    else
                    {
                        finder.rows = 500;
                        for (ret = new Returns(true, null, finder.skip + 1); finder.skip < ret.tag && ret.result; finder.skip += finder.rows)
                        {
                            List<Ulica> tmplist = ro.GetUlica(finder, srv, out ret);
                            if (tmplist != null) ulSpis.AddRange(tmplist);
                        }
                    }
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetUlica\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса улиц";

                MonitorLog.WriteLog("Ошибка GetUlica (" + srv + ") \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ulSpis;
        }
        //----------------------------------------------------------------------

        /// <summary>
        /// Возвращает список районов город по указанной базе данных и текущему региону
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="srv"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public List<Town> GetTownList(Town finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<Town> result = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    result = ro.GetTownList(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetTownList\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса улиц";

                MonitorLog.WriteLog("Ошибка GetTownList\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return result;
        }

        /// <summary>
        /// Возвращает список населенных по указанной базе данных и текущему району
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="srv"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public List<Rajon> GetRajonList(Rajon finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            List<Rajon> result = null;
            ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    result = ro.GetRajonList(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetRajonList\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса улиц";
                MonitorLog.WriteLog("Ошибка GetRajonList\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return result;
        }

        //----------------------------------------------------------------------
        public List<Dom> GetDom(Dom finder, enSrvOper srv, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    domSpis = ro.GetDom(finder, srv, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetDom\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса домов";
                MonitorLog.WriteLog("Ошибка GetDom (" + srv + ")\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                domSpis = null;
            }
            return domSpis;
        }

        public List<Dom> GetDom2(Dom finder, Service servfinder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    domSpis = ro.GetDom2(finder, servfinder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetDom2\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса домов";
                MonitorLog.WriteLog("Ошибка GetDom2 ()\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                domSpis = null;
            }
            return domSpis;
        }

        //----------------------------------------------------------------------
        public _Rekvizit GetLsRevizit(Ls finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    lsRekvizit = ro.GetLsRevizit(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка CallSrvLsRekvizit\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса реквизитов лицевого счета";
                MonitorLog.WriteLog("Ошибка CallSrvLsRekvizit\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                lsRekvizit = null;
            }
            return lsRekvizit;
        }

        //----------------------------------------------------------------------
        public bool SaveLsRevizit(string pref, _Rekvizit uk, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            bool res = false;
            try
            {
                using (var ro = getRemoteObject())
                {
                    res = ro.SaveLsRevizit(pref, uk, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка CallSrvSaveLsRekvizit\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса сохранения реквизитов лицевого счета";
                MonitorLog.WriteLog("Ошибка CallSrvSaveLsRekvizit\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                res = false;
            }
            return res;
        }

    
        //----------------------------------------------------------------------
        public string GetKolGil(MonthLs finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            string s = "0";
            try
            {
                using (var ro = getRemoteObject())
                {
                    s = ro.GetKolGil(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetKolGil\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка сервиса количества проживающих";
                MonitorLog.WriteLog("Ошибка GetKolGil\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return s;
        }
    
        //----------------------------------------------------------------------
        public List<_Geu> GetGeu(Finder finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            Geus spis = new Geus();
            try
            {
                using (var ro = getRemoteObject())
                {
                    BeforeStartQuery("GetGeu");
                    spis.GeuList = ro.GetGeu(finder, out ret);
                    AfterStopQuery();
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetGeu\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка сервиса списка участков";
                MonitorLog.WriteLog("Ошибка GetGeu\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return spis.GeuList;
        }
        //----------------------------------------------------------------------
        public Dom FindDomFromPm(_Placemark placemark, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            Dom d = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    d = ro.FindDomFromPm(placemark, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка FindDomFromPm\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка сервиса карт";

                MonitorLog.WriteLog("Ошибка FindDomFromPm\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return d;
        }
        //----------------------------------------------------------------------
        public string GetMapKey(out Returns ret)
        //----------------------------------------------------------------------
        {
            string result = "";
            ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    result = ro.GetMapKey(out ret);
                    if (!ret.result) result = "";
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetMapKey\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка сервиса карт";
                MonitorLog.WriteLog("Ошибка GetMapKey\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return result;
        }
        //----------------------------------------------------------------------
        public _Placemark GetDefaultPlacemark(out Returns ret)
        //----------------------------------------------------------------------
        {
            _Placemark result = new _Placemark();
            ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    result = ro.GetDefaultPlacemark(out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetDefaultPlacemark\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка сервиса карт";
                MonitorLog.WriteLog("Ошибка GetDefaultPlacemark\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return result;
        }
     
        //----------------------------------------------------------------------
        public bool SaveMapObjects(List<MapObject> mapObjects, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ro.SaveMapObjects(mapObjects, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка SaveMapObjects\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка сервиса карт";
                MonitorLog.WriteLog("Ошибка SaveMapObjects\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret.result;
        }
        //----------------------------------------------------------------------
        public bool DeleteMapObjects(MapObject finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ro.DeleteMapObjects(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка DeleteMapObjects\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка сервиса карт";

                MonitorLog.WriteLog("Ошибка DeleteMapObjects\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret.result;
        }
        //----------------------------------------------------------------------
        public List<Group> GetListGroup(Group finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<Group> list = new List<Group>();
            try
            {
                using (var ro = getRemoteObject())
                {
                    list = ro.GetListGroup(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetListGroup\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                list = null;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка сервиса списка групп";
                MonitorLog.WriteLog("Ошибка GetListGroup\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                list = null;
            }
            return list;
        }

        //----------------------------------------------------------------------
        public List<Group> GetGroupLs(Group finder, enSrvOper srv, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<Group> groupList = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    groupList = ro.GetGroupLs(finder, srv, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetGroupLs\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                groupList = null;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса группы лицевых счетов";
                MonitorLog.WriteLog("Ошибка GetGroupLs\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                groupList = null;
            }
            return groupList;
        }
        //----------------------------------------------------------------------
        public List<Group> LoadCurrentLsGroup(Group finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            List<Group> groupList = null;
            ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    groupList = ro.LoadCurrentLsGroup(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка LoadCurrentLsGroup\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                groupList = null;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса лицевого счета";
                MonitorLog.WriteLog("Ошибка LoadCurrentLsGroup\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                groupList = null;
            }
            return groupList;
        }

        public List<Area_ls> LoadCurrentLsSupplier(Area_ls finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<Area_ls> supplerList = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    supplerList = ro.LoadCurrentLsSupplier(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка RemoveUserLock\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                supplerList = null;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса лицевого счета";
                MonitorLog.WriteLog("Ошибка RemoveUserLock\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                supplerList = null;
            }
            return supplerList;
        }

        public void DeleteSupplierLs(Area_ls finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ro.DeleteSupplierLs(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка DeleteSupplierLs\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса лицевого счета";
                MonitorLog.WriteLog("Ошибка DeleteSupplierLs\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
        }

        public Area_ls LoadCurrentAliasLs(Area_ls finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            Area_ls supplerList = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    supplerList = ro.LoadCurrentAliasLs(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка LoadCurrentAliasLs\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса лицевого счета";
                MonitorLog.WriteLog("Ошибка LoadCurrentAliasLs\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return supplerList;
        }

        public void SaveSupplierLs(Area_ls finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ro.SaveSupplierLs(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка SaveSupplierLs\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса лицевого счета";
                MonitorLog.WriteLog("Ошибка SaveSupplierLs\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
        }

        //----------------------------------------------------------------------
        public bool SaveLsGroup(Group finder, List<string> groupList, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ro.SaveLsGroup(finder, groupList, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка SaveLsGroup\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка SaveLsGroup\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret.result;
        }
        //----------------------------------------------------------------------
        public Returns CreateNewGroup(Group finder)
        //----------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.CreateNewGroup(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка CreateNewGroup\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ret.text = "Ошибка вызова сервиса лицевого счета";
                MonitorLog.WriteLog("Ошибка CreateNewGroup\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public Returns DeleteGroup(Group finder)
        //----------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.DeleteGroup(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка DeleteGroup\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ret.text = "Ошибка вызова сервиса лицевого счета";
                MonitorLog.WriteLog("Ошибка DeleteGroup\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        //----------------------------------------------------------------------
        public List<Finder> GetPointsLs(Finder finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<Finder> points = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    points = ro.GetPointsLs(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetPointsLs\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса";
                MonitorLog.WriteLog("Ошибка GetPointsLs\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return points;
        }

        public List<Finder> GetPointsDom(Finder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Finder> points = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    points = ro.GetPointsDom(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetPointsDom\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса";
                MonitorLog.WriteLog("Ошибка GetPointsDom\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return points;
        }

        //----------------------------------------------------------------------
        public List<Search_Info> GetSearchInfo(Ls finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<Search_Info> res_list = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    res_list = ro.GetSearchInfo(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetSearchInfo\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ret.text = "Ошибка вызова сервиса";
                MonitorLog.WriteLog("Ошибка GetSearchInfo\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return res_list;
        }

   
        //процедура генератора отчетов
        //----------------------------------------------------------------------
        public Returns Generator(List<Prm> listprm, int nzp_user)
        //----------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.Generator(listprm, nzp_user);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка Generator\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса";
                MonitorLog.WriteLog("Ошибка Generator\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        //----------------------------------------------------------------------
        public Returns Generator2(List<int> listint, int nzp_user, int yy, int mm)
        //----------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.Generator2(listint, nzp_user, yy, mm);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка Generator2\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ret.text = "Ошибка вызова сервиса";
                MonitorLog.WriteLog("Ошибка Generator2\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        //----------------------------------------------------------------------
        public List<_RajonDom> FindRajonDom(Finder finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            List<_RajonDom> list = null;
            ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    list = ro.FindRajonDom(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка FindRajonDom\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса получения списка районов";
                MonitorLog.WriteLog("Ошибка FindRajonDom\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return list;
        }



    
        //----------------------------------------------------------------------
        public List<Dom> DbGetDom(Dom finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            List<Dom> list = new List<Dom>();
            ret = Utils.InitReturns();
            if (Points.IsMultiHost)
            {
                //вызвать сервис
                list = GetDom(finder, enSrvOper.SrvGet, out ret);
            }
            else
            {
                DbAdresClient db = new DbAdresClient();
                list = db.GetDom(finder, out ret);
                db.Close();
            }
            return list;
        }
        //----------------------------------------------------------------------
        public List<_Area> DbLoadArea2(Finder finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            DbAdresClient db = new DbAdresClient();
            List<_Area> list = db.LoadArea2(finder, out ret);
            db.Close();
            return list;
        }
        //----------------------------------------------------------------------
        public List<_Geu> DbLoadGeu2(Finder finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            DbAdresClient db = new DbAdresClient();
            List<_Geu> list = db.LoadGeu2(finder, out ret);
            db.Close();
            return list;
        }
        
        //----------------------------------------------------------------------
        public List<Ulica> DbGetUlica(Ulica finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            DbAdresClient db = new DbAdresClient();
            List<Ulica> list = db.GetUlica(finder, out ret);
            db.Close();
            return list;
        }
        //----------------------------------------------------------------------
        public bool DbCasheExists(string tab) //вытащить адреса для грида
        //----------------------------------------------------------------------
        {
            DbAdresClient db = new DbAdresClient();
            bool res = db.CasheExists(tab);
            db.Close();
            return res;
        }
        //----------------------------------------------------------------------
        public Returns DbChangeMarksSpisLs(Finder finder, List<Ls> list0, List<Ls> list1)
        //----------------------------------------------------------------------
        {
            DbAdresClient db = new DbAdresClient();
            Returns ret = db.ChangeMarksSpisLs(finder, list0, list1);
            db.Close();
            return ret;
        }

        public static List<_Area> DbLoadAreaAvailableForRole(Role finder, out Returns ret)
        {
            List<_Area> list;
            using (DbAdresClient db = new DbAdresClient())
            {
                list = db.LoadAreaAvailableForRole(finder, out ret);
            }
            return list;
        }

        public static List<_Geu> DbLoadGeuAvailableForRole(Role finder, out Returns ret)
        {
            List<_Geu> list;
            using (DbAdresClient db = new DbAdresClient())
            {
                list = db.LoadGeuAvailableForRole(finder, out ret);
            }
            return list;
        }

        public static string MakeWhereStringGroup(List<Group> finder)
        {
            return DbAdresClient.MakeWhereStringGroup(finder);
        }

        public bool SaveListGroup(Group finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ro.SaveListGroup(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка SaveListGroup\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса лицевого счета";
                MonitorLog.WriteLog("Ошибка SaveListGroup\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret.result;
        }

        public Returns SaveArea(Area finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.SaveArea(finder);
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
                ret.text = "Ошибка сохранения управляющей организации";
                MonitorLog.WriteLog("Ошибка RemoveUserLock\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public Returns SaveAreaPayer(Area finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.SaveAreaPayer(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка SaveAreaPayer\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка сохранения управляющей организации";
                MonitorLog.WriteLog("Ошибка SaveAreaPayer\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public Returns DeleteArea(Area finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.DeleteArea(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка DeleteArea\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка сохранения управляющей организации";
                MonitorLog.WriteLog("Ошибка DeleteArea\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        //----------------------------------------------------------------------
        public List<Ulica> UlicaLoad(Ulica finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<Ulica> list = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    if (finder.rows > 0) list = ro.UlicaLoad(finder, out ret);
                    else
                    {
                        list = new List<Ulica>();
                        finder.rows = 500;
                        for (ret = new Returns(true, null, finder.skip + 1); finder.skip < ret.tag && ret.result; finder.skip += finder.rows)
                        {
                            IList<Ulica> tmplist = ro.UlicaLoad(finder, out ret);
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
                MonitorLog.WriteLog("Ошибка UlicaLoad\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка UlicaLoad\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return list;
        }

        //----------------------------------------------------------------------
        public GetSelectListDomInfo GetSelectListDomInfo(Finder finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            GetSelectListDomInfo spis = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    spis = ro.GetSelectListDomInfo(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetSelectListDomInfo\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка GetSelectListDomInfo\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return spis;
        }

        //----------------------------------------------------------------------
        public Prefer GetPrefer(out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            Prefer prfr = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    prfr = ro.GetPrefer(out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetPrefer\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetPrefer\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return prfr;
        }

        public Returns MakeOperation(Finder finder, Operations oper)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.MakeOperation(finder, oper);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка MakeOperation\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка MakeOperation\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        //----------------------------------------------------------------------
        public List<Ls> GetUniquePointAreaGeu(Ls finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<Ls> spis = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    spis = ro.GetUniquePointAreaGeu(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetUniquePointAreaGeu\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка GetUniquePointAreaGeu\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return spis;
        }

        //----------------------------------------------------------------------
        public List<Ulica> DbLoadUlica(Dom finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = new Returns(false, "Функция не реализована", -1);
            return new List<Ulica>();
        }

        public List<Vill> LoadVill(Vill finder, out Returns ret)     
        {
            ret = Utils.InitReturns();
            List<Vill> spis = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    spis = ro.LoadVill(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка LoadVill\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка LoadVill\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return spis;
        }

        public List<Rajon> LoadVillRajon(Rajon finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Rajon> spis = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    spis = ro.LoadVillRajon(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка LoadVillRajon\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка LoadVillRajon\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return spis;
        }

        public List<Ls> LoadLsData(Ls finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Ls> spis = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    spis = ro.LoadLsData(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка LoadLsData\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка LoadLsData\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return spis;
        }

        public List<KvarPkodes> GetPkodes(Ls finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<KvarPkodes> spis = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    spis = ro.GetPkodes(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetPkodes\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetPkodes\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return spis;
        }

        /// <summary>
        /// Загружает список районов из таблицы s_rajon_dom
        /// </summary>
        /// <param name="finder">Обязательно nzp_user   </param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public List<RajonDom> LoadRajonDom(RajonDom finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<RajonDom> spis = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    spis = ro.LoadRajonDom(finder, out ret);
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

        /// <summary>
        /// Загружает список районов
        /// </summary>
        /// <param name="finder">Обязательно nzp_user   </param>
        /// <param name="ret"></param>
        /// <returns></returns>
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

        public Returns SaveVillRajon(Rajon finder, List<Rajon> list_checked)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.SaveVillRajon(finder, list_checked);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка SaveVillRajon\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка SaveVillRajon\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public Returns GeneratePkod()
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GeneratePkod();
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GeneratePkod\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GeneratePkod\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public DataTable PrepareLsPuVipiska(Ls finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            DataTable dt = null;

            try
            {
                using (var ro = getRemoteObject())
                {
                    dt = ro.PrepareLsPuVipiska(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка PrepareLsPuVipiska\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка PrepareLsPuVipiska\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return dt;
        }

        public DataTable PrepareGubCurrCharge(Charge finder, int reportId, out Returns ret)
        {
            ret = Utils.InitReturns();
            DataTable dt = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    dt = ro.PrepareGubCurrCharge(finder, reportId, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка PrepareGubCurrCharge\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка PrepareGubCurrCharge\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return dt;
        }

        /// <summary>
        /// Подготавливает список лицевых счетов для выполнения групповой операции
        /// </summary>
        /// <param name="finder"></param>
        /// <returns>В поле tag возвращает номер списка выбранных лицевых счетов</returns>
        public static Returns PrepareSelectedListLs(Ls finder)
        {
            var db = GetCacheTablesControl.GetInstance<CacheTablesControlLs>();
            return db.PrepareSelectedList(finder);

        }

        public Returns UpdateAddressPrefer(Ls finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.UpdateAddressPrefer(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка UpdateAddressPrefer\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса";
                MonitorLog.WriteLog("Ошибка UpdateAddressPrefer\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public Returns ChangeAddressLs(Finder finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.ChangeAddressLs(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка ChangeAddressLs\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса";
                MonitorLog.WriteLog("Ошибка ChangeAddressLs\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public Ls LoadAddressPrefer(Finder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            Ls prfr = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    prfr = ro.LoadAddressPrefer(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка LoadAddressPrefer\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка LoadAddressPrefer\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return prfr;
        }

        public List<Dom> LoadDom(Dom finder, out Returns ret)
        {
            List<Dom> spis = null;
            ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    spis = ro.LoadDom(finder, out ret);
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
            return spis;
        }


        public Returns DeleteLs(Ls finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.DeleteLs(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка DeleteLs\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса";
                MonitorLog.WriteLog("Ошибка DeleteLs\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public bool SaveListGroupLSBySelectedDoms(Group finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ro.SaveListGroupLSBySelectedDoms(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка SaveListGroupLSBySelectedDoms\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса лицевого счета";
                MonitorLog.WriteLog("Ошибка SaveListGroupLSBySelectedDoms\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret.result;
        }

        public Returns GetCountLSBySelectedDom(Group finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetCountLSBySelectedDom(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetCountLSBySelectedDom\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса";
                MonitorLog.WriteLog("Ошибка GetCountLSBySelectedDom\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public Returns FindLsFromDeptorSpis(Ls finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.FindLsFromDeptorSpis(finder);
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
                ret.text = "Ошибка вызова сервиса";
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + 
                    ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }
    }
}
