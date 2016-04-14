using System;
using System.Data;
using System.ServiceModel;
using System.Collections.Generic;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Global;


namespace STCLINE.KP50.Client
{
    public class cli_Supg : cli_Base, I_Supg
    {
        //I_Supg remoteObject;

        ISupgRemoteObject getRemoteObject()
        {
            return getRemoteObject<ISupgRemoteObject>(WCFParams.AdresWcfWeb.srvSupg);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nzp_server">Код удаленного сервера</param>
        /// <param name="nzp_role">Код подсистемы</param>
        public cli_Supg(int nzp_server)
            : base()
        {
            //_cli_Supg(nzp_server, Constants.roleSupg);
        }

        //void _cli_Supg(int nzp_server, int nzp_role)
        //{
        //    string addrHost = "";
        //    //определить параметры доступа
        //    _RServer zap = MultiHost.GetServer(nzp_server);

        //    if (Points.IsMultiHost && nzp_server > 0)
        //    {
        //        addrHost = zap.ip_adr + WCFParams.AdresWcfWeb.srvSupg;
        //        remoteObject = HostChannel.CreateInstance<I_Supg>(zap.login, zap.pwd, addrHost);
        //    }
        //    else
        //    {
        //        //по-умолчанию
        //        if (nzp_role == Constants.roleSupg)
        //            addrHost = WCFParams.AdresWcfWeb.Adres + WCFParams.AdresWcfWeb.srvSupg;
        //        else addrHost = WCFParams.AdresWcfWeb.Adres + WCFParams.AdresWcfWeb.srvSupg;
        //        zap.rcentr = "<локально>";
        //        remoteObject = HostChannel.CreateInstance<I_Supg>(addrHost);
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

        //~cli_Supg()
        //{
        //    try { if (remoteObject != null) HostChannel.CloseProxy(remoteObject); }
        //    catch { }
        //}

        public bool NedopService(int proc, JobOrder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            bool flag = false;
            try
            {
                using (var ro = getRemoteObject())
                {
                    switch (proc)
                    {
                        case 1:
                            flag = ro.NedopService(proc, finder, out ret);
                            break;
                    }
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка NedopService\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка NedopService\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return flag;
        }

        /// <summary>
        /// Получает классификацию сообщения
        /// </summary>
        /// <param name="finder">Объект поиска</param>
        /// <param name="ret">результат</param>
        /// <returns>Список классиффикаций сообщения</returns>
        public Dictionary<int, string> GetClassMessage(OrderContainer finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            Dictionary<int, string> dict = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    dict = ro.GetClassMessage(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetClassMessage\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка получения Классификакции сообщения";
                MonitorLog.WriteLog("Ошибка GetClassMessage\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return dict;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nzp_user">текущий пользователь</param>
        /// <param name="finder">Объект поиска</param>
        /// <param name="ret">результат</param>
        public int FindZvk(SupgFinder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            int res = -1;
            try
            {
                using (var ro = getRemoteObject())
                {
                    res = ro.FindZvk(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка FindZvk\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка при формировании результата поиска FindZvk ";
                MonitorLog.WriteLog("Ошибка FindZvk\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return res;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nzp_user">текущий пользователь</param>
        /// <param name="finder">Объект поиска</param>
        /// <param name="ret">результат</param>
        public ZvkFinder FastFindZk(SupgFinder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            ZvkFinder res = new ZvkFinder();
            try
            {
                using (var ro = getRemoteObject())
                {
                    res = ro.FastFindZk(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка FastFindZk\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка при формировании результата поиска FastFindZk ";
                MonitorLog.WriteLog("Ошибка FastFindZk\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return res;
        }

        public DataSet GetZakazReport(SupgFinder finder, string table_name, out Returns ret)
        {
            ret = Utils.InitReturns();
            DataSet res = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    res = ro.GetZakazReport(finder, table_name, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetZakazReport\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка при формировании результата поиска GetZakazReport ";
                MonitorLog.WriteLog("Ошибка GetZakazReport\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return res;
        }

        public ZvkFinder GetCarousel(SupgFinder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            ZvkFinder res = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    res = ro.GetCarousel(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetCarousel\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка при формировании результата поиска GetCarousel ";
                MonitorLog.WriteLog("Ошибка GetCarousel\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return res;
        }


        public List<ZvkFinder> GetFindZvk(SupgFinder finder, int flag, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<ZvkFinder> resList = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    resList = ro.GetFindZvk(finder, flag, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetFindZvk\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка получения списка заявок при поиске";
                MonitorLog.WriteLog("Ошибка GetFindZvk\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return resList;
        }

        public string DbMakeWhereString(SupgFinder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            string str = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    str = ro.DbMakeWhereString(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка DbMakeWhereString\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка формирования запроса при поиске лицевых счетов из шаблона поиска по заявкам";
                MonitorLog.WriteLog("Ошибка DbMakeWhereString\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return str;
        }

        /// <summary>
        /// Добавления заказа
        /// </summary>
        /// <param name="finder">Объект поиска</param>
        /// <returns>результат добавления</returns>
        public int AddOrder(OrderContainer finder)
        {
            Returns ret = Utils.InitReturns();
            int res = -1;
            try
            {
                using (var ro = getRemoteObject())
                {
                    res = ro.AddOrder(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка AddOrder\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка добавления заявки";
                MonitorLog.WriteLog("Ошибка AddOrder\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return res;
        }

        public List<OrderContainer> Find_Orders(OrderContainer finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<OrderContainer> resList = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    resList = ro.Find_Orders(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка Find_Orders\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка получения списка заявок";
                MonitorLog.WriteLog("Ошибка Find_Orders\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return resList;
        }

        public Dictionary<string, Dictionary<int, string>> GetSupgLists(SupgFinder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            Dictionary<string, Dictionary<int, string>> List = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    List = ro.GetSupgLists(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetSupgLists\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка добавления заявки";
                MonitorLog.WriteLog("Ошибка GetSupgLists\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return List;
        }

        public List<Dest> GetDestName(int nzp_serv, enSrvOper oper, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Dest> List = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    List = ro.GetDestName(nzp_serv, oper, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetDestName\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка добавления заявки";
                MonitorLog.WriteLog("Ошибка GetDestName\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return List;
        }

        public OrderContainer Result_Generating_Procedure(OrderContainer finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            OrderContainer resList = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    resList = ro.Result_Generating_Procedure(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка Result_Generating_Procedure\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка получения результата, срока выполнения, факта выполнения заявки";
                MonitorLog.WriteLog("Ошибка Result_Generating_Procedure\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return resList;
        }

        /// <summary>
        /// Получение списка служб
        /// </summary>
        /// <param name="finder">Объект поиска</param>
        /// <param name="ret">Результат</param>
        /// <returns>Список служб</returns>
        public List<ServiceForwarding> GetServices(OrderContainer finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<ServiceForwarding> listServices = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    listServices = ro.GetServices(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetServices\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка получения списка заявок";
                MonitorLog.WriteLog("Ошибка GetServices\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return listServices;
        }

        /// <summary>
        /// Добавление новой переадресации
        /// </summary>
        /// <param name="finder">параметр поиска</param>
        /// <param name="ret">результат</param>
        /// <returns>Результат добавления переадресации</returns>
        public bool AddReaddress(ServiceForwarding finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            bool res = false;
            try
            {
                using (var ro = getRemoteObject())
                {
                    res = ro.AddReaddress(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка AddReaddress\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка добавления переадресации";
                MonitorLog.WriteLog("Ошибка AddReaddress\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return res;
        }

        /// <summary>
        /// Получение саписка переадресаций
        /// </summary>
        /// <param name="finder">Объект поиска</param>
        /// <param name="ret">результат</param>
        /// <returns>Список переадресаций</returns>
        public List<ServiceForwarding> GetReadress(ServiceForwarding finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<ServiceForwarding> listReadress = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    listReadress = ro.GetReadress(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetReadress\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка получения списка переадресаций";
                MonitorLog.WriteLog("Ошибка GetReadress\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return listReadress;
        }

        /// <summary>
        /// Возвращает информацию по переадрсации
        /// </summary>
        /// <param name="finder">Объект поиска</param>
        /// <param name="ret">результат</param>
        /// <returns>Информация о переадресации</returns>
        public ServiceForwarding GetServiceForward_One(ServiceForwarding finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            ServiceForwarding service = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    service = ro.GetServiceForward_One(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetServiceForward_One\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка получения информации переадресации";
                MonitorLog.WriteLog("Ошибка GetServiceForward_One\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return service;
        }

        /// <summary>
        /// сохраняет результат редактирования переадресации
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public bool SaveCommentsReadress(ServiceForwarding finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            bool res = false;
            try
            {
                using (var ro = getRemoteObject())
                {
                    res = ro.SaveCommentsReadress(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка SaveCommentsReadress\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка обновления информации переадресации";
                MonitorLog.WriteLog("Ошибка SaveCommentsReadress\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return res;
        }

        /// <summary>
        /// получить информацию о выбранном заявлении
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public OrderContainer Find_Orders_One(OrderContainer finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            OrderContainer order = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    order = ro.Find_Orders_One(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка Find_Orders_One\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка получения информации о выбранном заявлении";
                MonitorLog.WriteLog("Ошибка Find_Orders_One\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return order;
        }

        /// <summary>
        /// информация о выбранной заявке
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public bool UpdateZvk(OrderContainer finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            bool res = false;
            try
            {
                using (var ro = getRemoteObject())
                {
                    res = ro.UpdateZvk(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка UpdateZvk\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка обновления информации о выбранной заявке";
                MonitorLog.WriteLog("Ошибка UpdateZvk\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return res;
        }

        public Dictionary<int, string> GetAllServices(int nzp_user, out Returns ret)
        {
            ret = Utils.InitReturns();
            Dictionary<int, string> retDic = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    retDic = ro.GetAllServices(nzp_user, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetAllServices\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка получения информации о выбранных услугах";
                MonitorLog.WriteLog("Ошибка GetAllServices\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return retDic;
        }

        /// <summary>
        /// Получение списка неисправностей по крнкретной услуге
        /// </summary>
        /// <param name="nzp_serv">Услуга</param>
        /// <param name="nzp_user">Пользователь</param>
        /// <param name="ret">Htpekmnfn</param>
        /// <returns>Список неисправностей</returns>
        public Dictionary<int, string> GetDest(int nzp_serv, int nzp_user, out Returns ret)
        {
            ret = Utils.InitReturns();
            Dictionary<int, string> retDic = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    retDic = ro.GetDest(nzp_serv, nzp_user, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetDest\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка GetDest\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return retDic;
        }

        public Dest GetNedops(Dest finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            Dest retNedop = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    retNedop = ro.GetNedops(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetNedops\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка GetNedops\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return retNedop;
        }

        /// <summary>
        /// Получение поставщика
        /// </summary>
        /// <param name="nzp_kvar"></param>
        /// <param name="nzp_user"></param>
        /// <param name="nzp_serv"></param>
        /// <param name="act_date"></param>
        /// <param name="ret"></param>
        /// <returns>поставщик</returns>
        //public string GetSupplier(int nzp_kvar, int nzp_user, int nzp_serv, string act_date, out Returns ret)
        public string GetSupplier(JobOrder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            string retStr = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    retStr = ro.GetSupplier(finder, out ret);
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
            return retStr;
        }

        /// <summary>
        /// получение списка поставщиков
        /// </summary>
        /// <param name="nzp_user">пользователь</param>
        /// <param name="ret">результат</param>
        /// <returns>список поставщиков</returns>
        public Dictionary<int, string> GetSuppliersAll(int nzp_user, int supp_filter, string pref, out Returns ret)
        {
            ret = Utils.InitReturns();
            Dictionary<int, string> retDic = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    retDic = ro.GetSuppliersAll(nzp_user, supp_filter, pref, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetSuppliersAll\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка получения списка поставщиков";
                MonitorLog.WriteLog("Ошибка GetSuppliersAll\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return retDic;
        }

        /// <summary>
        /// Добавление наряд- заказа
        /// </summary>
        /// <param name="finder">поисковик</param>
        /// <param name="ret">результат</param>
        /// <returns>результат</returns>
        public bool AddJobOrder(ref JobOrder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            bool res = false;
            try
            {
                using (var ro = getRemoteObject())
                {
                    res = ro.AddJobOrder(ref finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка AddJobOrder\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка добавления наряд-заказа";
                MonitorLog.WriteLog("Ошибка AddJobOrder\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return res;
        }

        /// <summary>
        /// Получение наряд заказов по заявке
        /// </summary>
        /// <param name="finder">nzp_user, nzp_zvk</param>
        /// <param name="ret">результат</param>
        /// <returns>список наряд-заказов</returns>
        public List<JobOrder> GetJobOrders(OrderContainer finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<JobOrder> retList = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    retList = ro.GetJobOrders(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetJobOrders\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка получения наряд-заказов";
                MonitorLog.WriteLog("Ошибка GetJobOrders\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return retList;
        }

        public JobOrder GetJobOrderForm(JobOrder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            JobOrder jo = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    jo = ro.GetJobOrderForm(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetJobOrderForm\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка получения выбранного наряд-заказа";
                MonitorLog.WriteLog("Ошибка GetJobOrderForm\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return jo;
        }

        public Dictionary<int, string> GetJobOrderResultsAll(Finder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            Dictionary<int, string> retDic = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    retDic = ro.GetJobOrderResultsAll(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetJobOrderResultsAll\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка";
                MonitorLog.WriteLog("Ошибка GetJobOrderResultsAll\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return retDic;
        }

        /// <summary>
        /// Получить долг по лс
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="dat_y"></param>
        /// <param name="dat_m"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public decimal GetDolgLs(Ls finder, int dat_y, int dat_m, out Returns ret)
        {
            ret = Utils.InitReturns();
            decimal retDolg = -1;
            try
            {
                using (var ro = getRemoteObject())
                {
                    retDolg = ro.GetDolgLs(finder, dat_y, dat_m, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetDolgLs\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка GetDolgLs\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return retDolg;
        }

        /// <summary>
        /// Получить список значений подтвержден факт выполнения
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public Dictionary<int, string> GetAttistation(Finder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            Dictionary<int, string> retDic = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    retDic = ro.GetAttistation(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetAttistation\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка";
                MonitorLog.WriteLog("Ошибка GetAttistation\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return retDic;
        }


        //public bool UpdateZakaz(JobOrder finder, out Returns ret)
        //{
        //    ret = Utils.InitReturns();
        //    bool res = false;
        //    try
        //    {
        //        res = remoteObject.UpdateZakaz(finder, out ret);
        //        HostChannel.CloseProxy(remoteObject);
        //        return res;
        //    }
        //    catch (Exception ex)
        //    {
        //        ret.result = false;
        //        if (ex is System.ServiceModel.EndpointNotFoundException)
        //        {
        //            ret.text = Constants.access_error;
        //            ret.tag = Constants.access_code;
        //        }
        //        else
        //            ret.text = "Ошибка";

        //        MonitorLog.WriteLog("Ошибка  \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

        //        return false;
        //    }
        //}

        /// <summary>
        /// Добавить повторный наряд-заказ
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public bool AddRepeatedJobOrder(ref JobOrder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            bool res = false;
            try
            {
                using (var ro = getRemoteObject())
                {
                    res = ro.AddRepeatedJobOrder(ref finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка AddRepeatedJobOrder\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка";
                MonitorLog.WriteLog("Ошибка AddRepeatedJobOrder\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return res;
        }

        /// <summary>
        /// Прооверка закрыт ли наряд-заказ для редактирования
        /// </summary>
        /// <param name="finder">pref, nzp_zk</param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public bool IsOrderClose(JobOrder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            bool res = false;
            try
            {
                using (var ro = getRemoteObject())
                {
                    res = ro.IsOrderClose(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка IsOrderClose\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка";
                MonitorLog.WriteLog("Ошибка IsOrderClose\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return res;
        }

        /// <summary>
        /// Получить все недопоставки по конкретной услуги
        /// </summary>
        /// <param name="finder">nzp_serv</param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public List<JobOrder> GetNedopsAll(JobOrder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<JobOrder> retList = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    retList = ro.GetNedopsAll(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetNedopsAll\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка";
                MonitorLog.WriteLog("Ошибка GetNedopsAll\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return retList;
        }

        /// <summary>
        /// Копирует одни поля в другие при смене результата
        /// </summary>
        /// <param name="finder">результат</param>
        /// <param name="ret"></param>
        /// <returns>Успех выполнения</returns>
        public bool CopyFields_WhenResultChanged(JobOrder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            bool res = false;
            try
            {
                using (var ro = getRemoteObject())
                {
                    res = ro.CopyFields_WhenResultChanged(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка CopyFields_WhenResultChanged\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка";
                MonitorLog.WriteLog("Ошибка CopyFields_WhenResultChanged\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return res;
        }

        /// <summary>
        /// Добавление данных о недопоставке
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns>результат</returns>
        public bool AddNedopJobOrder(JobOrder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            bool res = false;
            try
            {
                using (var ro = getRemoteObject())
                {
                    res = ro.AddNedopJobOrder(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка AddNedopJobOrder\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка";
                MonitorLog.WriteLog("Ошибка AddNedopJobOrder\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return res;
        }

        /// <summary>
        /// Обновление заявки (арм оператор)
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns>результат</returns>
        public bool UpdateZvk_armOperator(OrderContainer finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            bool res = false;
            try
            {
                using (var ro = getRemoteObject())
                {
                    res = ro.UpdateZvk_armOperator(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка UpdateZvk_armOperator\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка";
                MonitorLog.WriteLog("Ошибка UpdateZvk_armOperator\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return res;
        }

        /// <summary>
        /// обновление статуса выбранного наряда - заказа
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns>результат</returns>
        public Returns DbChangeMarksSpisSupg(SupgFinder finder, List<SupgFinder> list0, List<SupgFinder> list1)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.DbChangeMarksSpisSupg(finder, list0, list1);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка DbChangeMarksSpisSupg\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка";
                MonitorLog.WriteLog("Ошибка DbChangeMarksSpisSupg\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        /// <summary>
        /// формирование гистограммы
        /// </summary>
        /// <param name="finder"></param>
        /// <returns>результат</returns>
        public Returns GetSupgStatistics(SupgFinder finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetSupgStatistics(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetSupgStatistics\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка";
                MonitorLog.WriteLog("Ошибка GetSupgStatistics\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        /// <summary>
        /// Обеновление статуса наряд-заказа
        /// </summary>
        /// <param name="finder">nzp_zk</param>
        /// <param name="status">статус</param>
        /// <param name="ret"></param>
        /// <returns>результат</returns>
        public bool UpdateStatusJobOrder(JobOrder finder, int status, out Returns ret)
        {
            ret = Utils.InitReturns();
            bool res = false;
            try
            {
                using (var ro = getRemoteObject())
                {
                    res = ro.UpdateStatusJobOrder(finder, status, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка UpdateStatusJobOrder\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка";
                MonitorLog.WriteLog("Ошибка UpdateStatusJobOrder\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return res;
        }

        /// <summary>
        /// Обновление данных наряд-заказац
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public bool UpdateJobOrder(JobOrder finder, enSupgProc proc, out Returns ret)
        {
            ret = Utils.InitReturns();
            bool res = false;
            try
            {
                using (var ro = getRemoteObject())
                {
                    res = ro.UpdateJobOrder(finder, proc, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка UpdateJobOrder\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка";
                MonitorLog.WriteLog("Ошибка UpdateJobOrder\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return res;
        }


        /// <summary>
        /// Получение журнала выгрузок наряда-заказа
        /// </summary>
        /// <param name="nzp_user"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public List<Journal> GetJournal(Journal finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Journal> journal = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    journal = ro.GetJournal(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetJournal\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка";
                MonitorLog.WriteLog("Ошибка GetJournal\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return journal;
        }

        /// <summary>
        /// выборка нарядов-заказов
        /// </summary>
        /// <param name="nzp_user">пользователь</param>
        /// <param name="ret"></param>
        /// <returns>bool</returns>
        public bool NedopForming(Journal finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            bool flag = false;
            try
            {
                using (var ro = getRemoteObject())
                {
                    flag = ro.NedopForming(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка NedopForming\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка";
                MonitorLog.WriteLog("Ошибка NedopForming\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return flag;
        }

        public bool NedopPlacement(Journal finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            bool flag = false;
            try
            {
                using (var ro = getRemoteObject())
                {
                    flag = ro.NedopPlacement(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка NedopPlacement\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка";
                MonitorLog.WriteLog("Ошибка NedopPlacement\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return flag;
        }

        public bool NedopUnload(Journal finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            bool flag = false;
            try
            {
                using (var ro = getRemoteObject())
                {
                    flag = ro.NedopUnload(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка NedopUnload\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка";
                MonitorLog.WriteLog("Ошибка NedopUnload\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return flag;
        }

        public List<JobOrder> GetSpisNedop(JobOrder job_ord, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<JobOrder> list = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    list = ro.GetSpisNedop(job_ord, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetSpisNedop\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка";
                MonitorLog.WriteLog("Ошибка GetSpisNedop\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return list;
        }

        public int SetZakazActActual(SupgFinder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            int count = -1;
            try
            {
                using (var ro = getRemoteObject())
                {
                    count = ro.SetZakazActActual(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка SetZakazActActual\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка";
                MonitorLog.WriteLog("Ошибка SetZakazActActual\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return count;
        }

        public bool DeleteFromJournal(Journal finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            bool flag = false;
            try
            {
                using (var ro = getRemoteObject())
                {
                    flag = ro.DeleteFromJournal(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка DeleteFromJournal\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка";
                MonitorLog.WriteLog("Ошибка DeleteFromJournal\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return flag;
        }

        public bool UpdateJournal(Journal finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            bool flag = false;
            try
            {
                using (var ro = getRemoteObject())
                {
                    flag = ro.UpdateJournal(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка UpdateJournal\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка";
                MonitorLog.WriteLog("Ошибка UpdateJournal\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return flag;
        }

        /// <summary>
        /// Процедура получения справочника "Дополнительные отметки"
        /// </summary>
        public Dictionary<int, string> GetAnswers(out Returns ret)
        {
            ret = Utils.InitReturns();
            Dictionary<int, string> retDic = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    retDic = ro.GetAnswers(out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetAnswers\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка";
                MonitorLog.WriteLog("Ошибка GetAnswers\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return retDic;
        }

        public List<SupgAct> GetActs(SupgActFinder finder, enSrvOper oper, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<SupgAct> list = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    list = ro.GetActs(finder, oper, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetActs\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка";
                MonitorLog.WriteLog("Ошибка GetActs\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return list;
        }

        public bool CheckToClose(JobOrder finder, string ord, out Returns ret)
        {
            ret = Utils.InitReturns();
            bool res = false;
            try
            {
                using (var ro = getRemoteObject())
                {
                    res = ro.CheckToClose(finder, ord, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка CheckToClose\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка";
                MonitorLog.WriteLog("Ошибка CheckToClose\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return res;
        }

        public bool UpdatePlannedWorks(ref SupgAct finder, enSrvOper oper, out Returns ret)
        {
            ret = Utils.InitReturns();
            bool res = false;
            try
            {
                using (var ro = getRemoteObject())
                {
                    res = ro.UpdatePlannedWorks(ref finder, oper, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка UpdatePlannedWorks\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка";
                MonitorLog.WriteLog("Ошибка UpdatePlannedWorks\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return res;
        }

        /// <summary>
        /// Получение справочника служб
        /// </summary>
        /// <param name="finder">Объект поиска</param>
        /// <param name="ret">Результат</param>
        /// <returns>Список служб</returns>
        public List<ServiceForwarding> GetServiceCatalog(out Returns ret)
        {
            ret = Utils.InitReturns();
            List<ServiceForwarding> listServices = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    listServices = ro.GetServiceCatalog(out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetServiceCatalog\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка";
                MonitorLog.WriteLog("Ошибка GetServiceCatalog\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return listServices;
        }

        public bool UpdateServiceCatalog(ServiceForwarding finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            bool res = false;
            try
            {
                using (var ro = getRemoteObject())
                {
                    res = ro.UpdateServiceCatalog(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка UpdateServiceCatalog\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка";
                MonitorLog.WriteLog("Ошибка UpdateServiceCatalog\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return res;
        }

        public Dictionary<int, string> GetPhoneList(string pref, int nzp_kvar, out Returns ret)
        {
            ret = Utils.InitReturns();
            Dictionary<int, string> Phones = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    Phones = ro.GetPhoneList(pref, nzp_kvar, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetPhoneList\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка выборки списка возможных телефонов";
                MonitorLog.WriteLog("Ошибка GetPhoneList\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return Phones;
        }

        public bool GetSuppEMail(string nzp_supp, out Returns ret)
        {
            ret = Utils.InitReturns();
            bool res = false;
            try
            {
                using (var ro = getRemoteObject())
                {
                    res = ro.GetSuppEMail(nzp_supp, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetSuppEMail\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка выборки электронного адреса";
                MonitorLog.WriteLog("Ошибка GetSuppEMail\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return res;
        }

        /// <summary>
        /// Обновление справочников
        /// </summary>
        /// <param name="finder">объект поиска</param>
        /// <param name="oper">операция</param>        
        /// <returns>результат</returns>
        public bool UpdateSpravSupg(Dest finder, enSrvOper oper, out Returns ret)
        {
            ret = Utils.InitReturns();
            bool res = false;
            try
            {
                using (var ro = getRemoteObject())
                {
                    res = ro.UpdateSpravSupg(finder, oper, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка UpdateSpravSupg\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка функции обновления справочников";
                MonitorLog.WriteLog("Ошибка UpdateSpravSupg\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return res;
        }

        /// <summary>
        /// Заполнение таблицы в БД Супг ls_saldo
        /// </summary>
        /// <param name="ret"></param>
        /// <returns>bool</returns>
        public bool FillLSSaldo(out Returns ret)
        {
            ret = Utils.InitReturns();
            bool flag = false;
            try
            {
                using (var ro = getRemoteObject())
                {
                    flag = ro.FillLSSaldo(out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка FillLSSaldo\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка";
                MonitorLog.WriteLog("Ошибка FillLSSaldo\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return flag;
        }

        /// <summary>
        /// Заполнение таблицы в БД Супг ls_tarif
        /// </summary>
        /// <param name="ret"></param>
        /// <returns>bool</returns>
        public bool FillLSTarif(out Returns ret)
        {
            ret = Utils.InitReturns();
            bool flag = false;
            try
            {
                using (var ro = getRemoteObject())
                {
                    flag = ro.FillLSTarif(out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка FillLSTarif\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка";
                MonitorLog.WriteLog("Ошибка FillLSTarif\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return flag;
        }

        /// <summary>
        /// Получить справочник Классификация сообщений
        /// </summary>
        /// <param name="ret"></param>
        /// <returns>bool</returns>
        public List<Sprav> GetThemesCatalog(out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Sprav> themes = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    themes = ro.GetThemesCatalog(out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetThemesCatalog\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка";
                MonitorLog.WriteLog("Ошибка GetThemesCatalog\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return themes;
        }

        /// <summary>
        /// Изменить справочник Классификация сообщений
        /// </summary>
        /// <param name="ret"></param>
        /// <returns>bool</returns>
        public bool UpdateThemesCatalog(Sprav finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            bool res = false;
            try
            {
                using (var ro = getRemoteObject())
                {
                    res = ro.UpdateThemesCatalog(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка UpdateThemesCatalog\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка";
                MonitorLog.WriteLog("Ошибка UpdateThemesCatalog\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return res;
        }
    }
}
