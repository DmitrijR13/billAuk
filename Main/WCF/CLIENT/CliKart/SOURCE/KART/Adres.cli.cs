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


namespace STCLINE.KP50.Client
{
    public class cli_Adres : I_Adres
    {
        I_Adres remoteObject;

        public cli_Adres(int nzp_server)
            : base()
        {
            _cli_Adres(nzp_server, 0);
        }

        public cli_Adres(int nzp_server, int nzp_role)
            : base()
        {
            _cli_Adres(nzp_server, nzp_role);
        }

        void _cli_Adres(int nzp_server, int nzp_role)
        {
            if (Points.IsMultiHost && nzp_server > 0)
            {
                //определить параметры доступа
                _RServer zap = MultiHost.GetServer(nzp_server);
                remoteObject = HostChannel.CreateInstance<I_Adres>(zap.login, zap.pwd, zap.ip_adr + WCFParams.AdresWcfWeb.srvAdres);
            }
            else
            {
                if (nzp_role == Constants.roleSupg)
                    remoteObject = HostChannel.CreateInstance<I_Adres>(WCFParams.AdresWcfWeb.SupgAdres + WCFParams.AdresWcfWeb.srvAdres);
                else
                    remoteObject = HostChannel.CreateInstance<I_Adres>(WCFParams.AdresWcfWeb.Adres + WCFParams.AdresWcfWeb.srvAdres);
            }
        }

        ~cli_Adres()
        {
            try { if (remoteObject != null) HostChannel.CloseProxy(remoteObject); }
            catch { }
        }

        List<Ulica> ulSpis = new List<Ulica>();
        List<_Area> arSpis = new List<_Area>();
        List<_Geu> geuSpis = new List<_Geu>();
        List<Ls> lsSpis = new List<Ls>();
        List<Dom> domSpis = new List<Dom>();
        _Rekvizit lsRekvizit = new _Rekvizit();

        //----------------------------------------------------------------------
        public int UpdateDom(Dom finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                int b = remoteObject.UpdateDom(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return b;
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
                    ret.text = "Ошибка вызова сервиса изменения домов";

                MonitorLog.WriteLog("Ошибка UpdateDom \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return 0;
            }
        }
        //----------------------------------------------------------------------
        public int UpdateLs(Ls finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            try
            {
                int res = remoteObject.UpdateLs(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return res;
            }
            catch (Exception ex)
            {
                ret = new Returns(false, "Ошибка вызова сервиса изменения лицевых счетов");
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }

                MonitorLog.WriteLog("Ошибка UpdateLs \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return 0;
            }
        }
        //----------------------------------------------------------------------
        public Returns GenerateLsPu(List<Counter> CounterList)
        //----------------------------------------------------------------------
        {
            return GenerateLsPu(null, CounterList);
        }
        //----------------------------------------------------------------------
        public Returns GenerateLsPu(Ls finder)
        //----------------------------------------------------------------------
        {
            return GenerateLsPu(finder, null);
        }

        public Returns GenerateLsPu(Ls finder, List<Counter> CounterList)
        {
            Returns ret;
            try
            {
                ret = remoteObject.GenerateLsPu(finder, CounterList);
                HostChannel.CloseProxy(remoteObject);
                return ret;
            }
            catch (Exception ex)
            {
                ret = new Returns(false, "Ошибка вызова сервиса группового добавления лицевых счетов");
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }

                MonitorLog.WriteLog("Ошибка GenerateLsPu \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }
        //----------------------------------------------------------------------
        public void UpdateGroupDom(Dom finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                remoteObject.UpdateGroupDom(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return;
            }
            catch (Exception ex)
            {
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = "Ошибка вызова сервиса изменения домов групповая операция";

                MonitorLog.WriteLog("Ошибка UpdateGroupDom \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return;
            }
        }
        //----------------------------------------------------------------------
        public void UpdateGroupLs(Ls finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                remoteObject.UpdateGroupLs(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return;
            }
            catch (Exception ex)
            {
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = "Ошибка вызова сервиса изменения лицевых счетов групповая операция";

                MonitorLog.WriteLog("Ошибка UpdateGroupLs \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return;
            }
        }
        //----------------------------------------------------------------------
        public List<Ulica> GetUlica(Dom finder, enSrvOper srv, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                ulSpis = remoteObject.GetUlica(finder, srv, out ret);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = "Ошибка вызова сервиса улиц";

                MonitorLog.WriteLog("Ошибка GetUlica (" + srv + ") \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                ulSpis = null;
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
            List<Town> result = null;

            ret = Utils.InitReturns();
            try
            {
                result = remoteObject.GetTownList(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = "Ошибка вызова сервиса улиц";

                MonitorLog.WriteLog("Ошибка GetTownList \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
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
                result = remoteObject.GetRajonList(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = "Ошибка вызова сервиса улиц";

                MonitorLog.WriteLog("Ошибка GetRajonList \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return result;
        }

        public List<Ls> GetLs(Ls finder, enSrvOper srv, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                lsSpis = remoteObject.GetLs(finder, srv, out ret);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = "Ошибка вызова сервиса лицевого счета";

                MonitorLog.WriteLog("Ошибка GetLs (" + srv + ") \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                lsSpis = null;
            }

            return lsSpis;
        }
        //----------------------------------------------------------------------
        public List<Dom> GetDom(Dom finder, enSrvOper srv, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                domSpis = remoteObject.GetDom(finder, srv, out ret);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = "Ошибка вызова сервиса домов";

                MonitorLog.WriteLog("Ошибка GetDom (" + srv + ") \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
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
                lsRekvizit = remoteObject.GetLsRevizit(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = "Ошибка вызова сервиса реквизитов лицевого счета";

                MonitorLog.WriteLog("Ошибка CallSrvLsRekvizit \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
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
                res = remoteObject.SaveLsRevizit(pref, uk, out ret);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = "Ошибка вызова сервиса сохранения реквизитов лицевого счета";

                MonitorLog.WriteLog("Ошибка CallSrvSaveLsRekvizit \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                res = false;
            }

            return res;
        }

        //----------------------------------------------------------------------
        public string GetFakturaName(Ls finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            string s = "";
            try
            {
                s = remoteObject.GetFakturaName(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = "Ошибка вызова сервиса счета-фактуры";

                MonitorLog.WriteLog("Ошибка GetFakturaName \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }

            return s;
        }
        //----------------------------------------------------------------------
        public string GetKolGil(MonthLs finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            string s = "0";

            try
            {
                s = remoteObject.GetKolGil(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = "Ошибка сервиса количества проживающих";

                MonitorLog.WriteLog("Ошибка GetKolGil \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return s;
        }
        //----------------------------------------------------------------------
        public List<_Area> GetArea(Finder finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            Areas spis = new Areas();
            try
            {
                spis.AreaList = remoteObject.GetArea(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = "Ошибка сервиса списка УК";

                MonitorLog.WriteLog("Ошибка GetArea \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                spis.AreaList = null;
            }

            return spis.AreaList;
        }
        //----------------------------------------------------------------------
        public List<_Geu> GetGeu(Finder finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            Geus spis = new Geus();
            try
            {
                spis.GeuList = remoteObject.GetGeu(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false; if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else
                    ret.text = "Ошибка сервиса списка участков";

                MonitorLog.WriteLog("Ошибка GetGeu \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                spis.GeuList = null;
            }

            return spis.GeuList;
        }
        //----------------------------------------------------------------------
        public Dom FindDomFromPm(_Placemark placemark, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                Dom d = remoteObject.FindDomFromPm(placemark, out ret);
                HostChannel.CloseProxy(remoteObject);
                return d;
            }
            catch (Exception ex)
            {
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = "Ошибка сервиса карт";

                MonitorLog.WriteLog("Ошибка FindDomFromPm \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return null;
        }
        //----------------------------------------------------------------------
        public string GetMapKey(out Returns ret)
        //----------------------------------------------------------------------
        {
            string result = "";
            ret = Utils.InitReturns();
            try
            {
                result = remoteObject.GetMapKey(out ret);
                if (!ret.result) result = "";
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = "Ошибка сервиса карт";

                MonitorLog.WriteLog("Ошибка GetMapKey \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return result;
        }
        //----------------------------------------------------------------------
        public _Placemark GetDefaultPlacemark(out Returns ret)
        //----------------------------------------------------------------------
        {
            _Placemark result;
            ret = Utils.InitReturns();
            try
            {
                result = remoteObject.GetDefaultPlacemark(out ret);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                result = new _Placemark();
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = "Ошибка сервиса карт";

                MonitorLog.WriteLog("Ошибка GetDefaultPlacemark \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return result;
        }
        //----------------------------------------------------------------------
        public List<MapObject> GetMapObjects(MapObject finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            List<MapObject> result;
            ret = Utils.InitReturns();
            try
            {
                result = remoteObject.GetMapObjects(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                result = new List<MapObject>();
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = "Ошибка сервиса карт";

                MonitorLog.WriteLog("Ошибка GetMapObjects \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
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
                remoteObject.SaveMapObjects(mapObjects, out ret);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = "Ошибка сервиса карт";

                MonitorLog.WriteLog("Ошибка SaveMapObjects \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
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
                remoteObject.DeleteMapObjects(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = "Ошибка сервиса карт";

                MonitorLog.WriteLog("Ошибка DeleteMapObjects \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
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
                list = remoteObject.GetListGroup(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = "Ошибка сервиса списка групп";

                MonitorLog.WriteLog("Ошибка GetListGroup \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                list = null;
            }

            return list;
        }

        //----------------------------------------------------------------------
        public List<Group> GetGroupLs(Group finder, enSrvOper srv, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                List<Group> groupList = remoteObject.GetGroupLs(finder, srv, out ret);
                HostChannel.CloseProxy(remoteObject);
                return groupList;
            }
            catch (Exception ex)
            {
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = "Ошибка вызова сервиса группы лицевых счетов";

                MonitorLog.WriteLog("Ошибка GetGroupLs (" + srv + ") \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }
        //----------------------------------------------------------------------
        public List<Group> LoadCurrentLsGroup(Group finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                List<Group> groupList = remoteObject.LoadCurrentLsGroup(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return groupList;
            }
            catch (Exception ex)
            {
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = "Ошибка вызова сервиса лицевого счета";

                MonitorLog.WriteLog("Ошибка LoadCurrentLsGroup\n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public List<Area_ls> LoadCurrentLsSupplier(Area_ls finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                List<Area_ls> supplerList = remoteObject.LoadCurrentLsSupplier(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return supplerList;
            }
            catch (Exception ex)
            {
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = "Ошибка вызова сервиса лицевого счета";

                MonitorLog.WriteLog("Ошибка LoadCurrentLsSupplier\n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public void DeleteSupplierLs(Area_ls finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                remoteObject.DeleteSupplierLs(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return;
            }
            catch (Exception ex)
            {
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = "Ошибка вызова сервиса лицевого счета";

                MonitorLog.WriteLog("Ошибка LoadCurrentLsSupplier\n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return;
            }
        }

        public Area_ls LoadCurrentAliasLs(Area_ls finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                Area_ls supplerList = remoteObject.LoadCurrentAliasLs(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return supplerList;
            }
            catch (Exception ex)
            {
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = "Ошибка вызова сервиса лицевого счета";

                MonitorLog.WriteLog("Ошибка LoadCurrentAliasLs\n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public void SaveSupplierLs(Area_ls finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                remoteObject.SaveSupplierLs(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return;
            }
            catch (Exception ex)
            {
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = "Ошибка вызова сервиса лицевого счета";

                MonitorLog.WriteLog("Ошибка SaveSupplierLs\n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return ;
            }
        }

        //----------------------------------------------------------------------
        public List<Ls> LoadLs(Ls finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                List<Ls> LsInf = remoteObject.LoadLs(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return LsInf;
            }
            catch (Exception ex)
            {
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = "Ошибка вызова функции получения информации о лицевом счете";

                MonitorLog.WriteLog("Ошибка LoadLs\n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        //----------------------------------------------------------------------
        public bool SaveLsGroup(Group finder, List<string> groupList, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                remoteObject.SaveLsGroup(finder, groupList, out ret);
                HostChannel.CloseProxy(remoteObject);
                return true;
            }
            catch (Exception ex)
            {
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = "Ошибка вызова сервиса лицевого счета";

                MonitorLog.WriteLog("Ошибка SaveLsGroup\n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return false;
            }
        }
        //----------------------------------------------------------------------
        public Returns CreateNewGroup(Group finder)
        //----------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.CreateNewGroup(finder);
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
                else ret.text = "Ошибка вызова сервиса лицевого счета";

                MonitorLog.WriteLog("Ошибка CreateNewGroup\n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }

        //----------------------------------------------------------------------
        public List<Finder> GetPointsLs(Finder finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                List<Finder> points = remoteObject.GetPointsLs(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return points;
            }
            catch (Exception ex)
            {
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = "Ошибка вызова сервиса";

                MonitorLog.WriteLog("Ошибка GetPointsLs() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        //----------------------------------------------------------------------
        public List<Search_Info> GetSearchInfo(Ls finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                List<Search_Info> res_list = remoteObject.GetSearchInfo(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return res_list;
            }
            catch (Exception ex)
            {
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = "Ошибка вызова сервиса";

                MonitorLog.WriteLog("Ошибка GetSearchInfo() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        //----------------------------------------------------------------------
        public Returns UpdateLsInCache(Ls finder)
        //----------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.UpdateLsInCache(finder);
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
                else ret.text = "Ошибка вызова сервиса";

                MonitorLog.WriteLog("Ошибка UpdateLsInCache() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }
        //процедура генератора отчетов
        //----------------------------------------------------------------------
        public Returns Generator(List<Prm> listprm, int nzp_user)
        //----------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.Generator(listprm, nzp_user);
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
                else ret.text = "Ошибка вызова сервиса";

                MonitorLog.WriteLog("Ошибка Generator \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }
        //----------------------------------------------------------------------
        public Returns Generator2(List<int> listint, int nzp_user, int yy, int mm)
        //----------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.Generator2(listint, nzp_user, yy, mm);
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
                else ret.text = "Ошибка вызова сервиса";

                MonitorLog.WriteLog("Ошибка Generator2 \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }
        //----------------------------------------------------------------------
        public List<_RajonDom> FindRajonDom(Finder finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            List<_RajonDom> list = null;
            ret = Utils.InitReturns();
            try
            {
                list = remoteObject.FindRajonDom(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = "Ошибка вызова сервиса получения списка районов";

                MonitorLog.WriteLog("Ошибка FindRajonDom\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return list;
        }



        //----------------------------------------------------------------------
        public List<Ls> DbGetLs(Ls finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            List<Ls> list = new List<Ls>();
            ret = Utils.InitReturns();
            if (Points.IsMultiHost)
            {
                //вызвать сервис
                list = GetLs(finder, enSrvOper.SrvGet, out ret);
            }
            else
            {
                DbAdresClient db = new DbAdresClient();
                list = db.GetLs(finder, out ret);
                db.Close();
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
        //----------------------------------------------------------------------
        public List<_Area> DbLoadAreaAvailableForRole(Role finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            DbAdresClient db = new DbAdresClient();
            List<_Area> list = db.LoadAreaAvailableForRole(finder, out ret);
            db.Close();
            return list;
        }
        //----------------------------------------------------------------------
        public List<_Geu> DbLoadGeuAvailableForRole(Role finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            DbAdresClient db = new DbAdresClient();
            List<_Geu> list = db.LoadGeuAvailableForRole(finder, out ret);
            db.Close();
            return list;
        }

        public static string MakeWhereStringGroup(Group finder)
        {
            return DbAdresClient.MakeWhereStringGroup(finder);
        }

        public bool SaveListGroup(Group finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                remoteObject.SaveListGroup(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return true;
            }
            catch (Exception ex)
            {
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = "Ошибка вызова сервиса лицевого счета";

                MonitorLog.WriteLog("Ошибка SaveListGroup\n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return false;
            }
        }

        public Returns SaveArea(Area finder)
        {
            Returns ret;
            try
            {
                ret = remoteObject.SaveArea(finder);
                HostChannel.CloseProxy(remoteObject);
                return ret;
            }
            catch (Exception ex)
            {
                ret = new Returns(false);
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = "Ошибка сохранения управляющей организации";

                MonitorLog.WriteLog("Ошибка SaveArea\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }

        public Returns SaveGeu(Geu finder)
        {
            Returns ret;
            try
            {
                ret = remoteObject.SaveGeu(finder);
                HostChannel.CloseProxy(remoteObject);
                return ret;
            }
            catch (Exception ex)
            {
                ret = new Returns(false);
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = "Ошибка сохранения отделения";

                MonitorLog.WriteLog("Ошибка SaveGeu\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }

        public Returns SaveUlica(Ulica finder)
        {
            Returns ret;
            try
            {
                ret = remoteObject.SaveUlica(finder);
                HostChannel.CloseProxy(remoteObject);
                return ret;
            }
            catch (Exception ex)
            {
                ret = new Returns(false);
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = "Ошибка сохранения улиц";

                MonitorLog.WriteLog("Ошибка SaveUlica\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }
        //----------------------------------------------------------------------
        public List<Ulica> UlicaLoad(Ulica finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
              //  List<Ulica> spis = remoteObject.UlicaLoad(finder, out ret);

                List<Ulica> tmplist;
                List<Ulica> list = new List<Ulica>();
                if (finder.rows > 0) list = remoteObject.UlicaLoad(finder, out ret);
                else
                {
                    finder.rows = 500;

                    int _skip = 0;
                    while (true)
                    {
                        tmplist = remoteObject.UlicaLoad(finder, out ret);
                        if (!ret.result) break;
                        if (tmplist != null)
                            list.AddRange(tmplist);


                        _skip += 500;
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
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка UlicaLoad " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        //----------------------------------------------------------------------
        public GetSelectListDomInfo GetSelectListDomInfo(Finder finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                GetSelectListDomInfo spis = remoteObject.GetSelectListDomInfo(finder, out ret);
                HostChannel.CloseProxy(remoteObject);

                return spis;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetSelectListDomInfo " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }
        //-

       

    
        //----------------------------------------------------------------------
        public Prefer GetPrefer(out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                Prefer prfr = remoteObject.GetPrefer(out ret);
                HostChannel.CloseProxy(remoteObject);

                return prfr;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetPrefer " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public Returns MakeOperation(Finder finder, Operations oper)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.MakeOperation(finder, oper);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка MakeOperation(" + oper + ") " + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        //----------------------------------------------------------------------
        public List<Ls> GetUniquePointAreaGeu(Ls finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                List<Ls> spis = remoteObject.GetUniquePointAreaGeu(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return spis;
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message, -1);
                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetUniquePointAreaGeu " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
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
            try
            {
                List<Vill> spis = remoteObject.LoadVill(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return spis;
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message, -1);
                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка LoadVill " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public List<Rajon> LoadVillRajon(Rajon finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                List<Rajon> spis = remoteObject.LoadVillRajon(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return spis;
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message, -1);
                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка LoadVillRajon " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        /// <summary>
        /// Загружает список районов
        /// </summary>
        /// <param name="finder">Обязательно nzp_user   </param>
        /// <param name="ret"></param>
        /// <returns</returns>
        public List<Rajon> LoadRajon(Rajon finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                List<Rajon> spis = remoteObject.LoadRajon(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return spis;
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message, -1);
                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка LoadVillRajon " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public Returns SaveVillRajon(Rajon finder, List<Rajon> list_checked)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.SaveVillRajon(finder, list_checked);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка SaveVillRajon" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }


        public Returns GeneratePkod()
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GeneratePkod();
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GeneratePkod" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public Returns GeneratePkodFon(Finder finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GeneratePkodFon(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GeneratePkodFon" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public DataTable PrepareLsPuVipiska(Ls finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            DataTable dt = null;

            try
            {
                dt = remoteObject.PrepareLsPuVipiska(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return dt;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка PrepareLsPuVipiska" + err, MonitorLog.typelog.Error, 7, 100, true);
            }

            return dt;
        }


        
        public Returns UpdateSosLS(Ls finder)
        //----------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.UpdateSosLS(finder);
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
                else ret.text = "Ошибка вызова сервиса";

                MonitorLog.WriteLog("Ошибка UpdateSosLS() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }

        public DataTable PrepareGubCurrCharge(Charge finder, int reportId, out Returns ret)
        {
            ret = Utils.InitReturns();
            DataTable dt = null;

            try
            {
                dt = remoteObject.PrepareGubCurrCharge(finder, reportId, out ret);
                HostChannel.CloseProxy(remoteObject);
                return dt;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка PrepareGubCurrCharge" + err, MonitorLog.typelog.Error, 7, 100, true);
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
            DbAdresClient db = new DbAdresClient();
            Returns ret = db.PrepareSelectedListLs(finder);
            db.Close();
            return ret;
        }

        public Returns UpdateAddressPrefer(Ls finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.UpdateAddressPrefer(finder);
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
                else ret.text = "Ошибка вызова сервиса";

                MonitorLog.WriteLog("Ошибка UpdateAddressPrefer() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }

        public Ls LoadAddressPrefer(Finder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                Ls prfr = remoteObject.LoadAddressPrefer(finder, out ret);
                HostChannel.CloseProxy(remoteObject);

                return prfr;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка LoadAddressPrefer " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public List<Dom> LoadDom(Dom finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                List<Dom> spis = remoteObject.LoadDom(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return spis;
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message, -1);
                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка LoadDom " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }
    }
}
