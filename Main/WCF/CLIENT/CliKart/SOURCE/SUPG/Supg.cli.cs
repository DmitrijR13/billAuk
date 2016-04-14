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
    public class cli_Supg : I_Supg
    {
        I_Supg remoteObject;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nzp_server">Код удаленного сервера</param>
        /// <param name="nzp_role">Код подсистемы</param>
        public cli_Supg(int nzp_server)
            : base()
        {
            _cli_Supg(nzp_server, Constants.roleSupg);
        }

        void _cli_Supg(int nzp_server, int nzp_role)
        {
            string addrHost = "";
            //определить параметры доступа
            _RServer zap = MultiHost.GetServer(nzp_server);

            if (Points.IsMultiHost && nzp_server > 0)
            {
                addrHost = zap.ip_adr + WCFParams.AdresWcfWeb.srvSupg;
                remoteObject = HostChannel.CreateInstance<I_Supg>(zap.login, zap.pwd, addrHost);
            }
            else
            {
                //по-умолчанию
                if (nzp_role == Constants.roleSupg)
                    addrHost = WCFParams.AdresWcfWeb.Adres + WCFParams.AdresWcfWeb.srvSupg;
                else addrHost = WCFParams.AdresWcfWeb.Adres + WCFParams.AdresWcfWeb.srvSupg;
                zap.rcentr = "<локально>";
                remoteObject = HostChannel.CreateInstance<I_Supg>(addrHost);
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

        ~cli_Supg()
        {
            try { if (remoteObject != null) HostChannel.CloseProxy(remoteObject); }
            catch { }
        }

        public bool NedopService(int proc, JobOrder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            bool flag = false;
            try
            {

                switch (proc)
                {
                    case 1:
                        {
                            flag = remoteObject.NedopService(proc, finder, out ret);
                            break;
                        }
                }

                HostChannel.CloseProxy(remoteObject);
                return flag;
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
                    ret.text = "Ошибка";

                MonitorLog.WriteLog("Ошибка \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return false;
            }
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
                dict = remoteObject.GetClassMessage(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return dict;
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
                    ret.text = "Ошибка получения Классификакции сообщения";

                MonitorLog.WriteLog("Ошибка GetClassMessage \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return null;
            }
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
                res = remoteObject.FindZvk(finder, out ret);
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
                else
                    ret.text = "Ошибка при формировании результата поиска FindZvk ";

                MonitorLog.WriteLog("Ошибка FindZvk \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return res;
            }
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
                res = remoteObject.FastFindZk(finder, out ret);
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
                else
                    ret.text = "Ошибка при формировании результата поиска FastFindZk ";

                MonitorLog.WriteLog("Ошибка FastFindZk \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return null;
            }
        }


        public DataSet GetZakazReport(SupgFinder finder, string table_name, out Returns ret)
        {
            ret = Utils.InitReturns();
            DataSet res = new DataSet();
            try
            {
                res = remoteObject.GetZakazReport(finder, table_name, out ret);
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
                else
                    ret.text = "Ошибка при формировании результата поиска GetZakazReport ";

                MonitorLog.WriteLog("Ошибка GetZakazReport \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return null;
            }
        }



        public ZvkFinder GetCarousel(SupgFinder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            ZvkFinder res = new ZvkFinder();
            try
            {
                res = remoteObject.GetCarousel(finder, out ret);
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
                else
                    ret.text = "Ошибка при формировании результата поиска GetCarousel ";

                MonitorLog.WriteLog("Ошибка FastFindZk \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return null;
            }
        }


        public List<ZvkFinder> GetFindZvk(SupgFinder finder, int flag, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<ZvkFinder> resList = null;
            try
            {
                resList = remoteObject.GetFindZvk(finder, flag, out ret);
                HostChannel.CloseProxy(remoteObject);
                return resList;
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
                    ret.text = "Ошибка получения списка заявок при поиске";

                MonitorLog.WriteLog("Ошибка GetFindZvk \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return null;
            }
        }


        public string DbMakeWhereString(SupgFinder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            string str = "";
            try
            {
                str = remoteObject.DbMakeWhereString(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return str;
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
                    ret.text = "Ошибка формирования запроса при поиске лицевых счетов из шаблона поиска по заявкам";

                MonitorLog.WriteLog("Ошибка DbMakeWhereString \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return null;
            }
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
                res = remoteObject.AddOrder(finder);
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
                else
                    ret.text = "Ошибка добавления заявки";

                MonitorLog.WriteLog("Ошибка AddOrder \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return -1;
            }
        }

        public List<OrderContainer> Find_Orders(OrderContainer finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<OrderContainer> resList = null;
            try
            {
                resList = remoteObject.Find_Orders(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return resList;
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
                    ret.text = "Ошибка получения списка заявок";

                MonitorLog.WriteLog("Ошибка Find_Orders \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return null;
            }
        }


        public Dictionary<string, Dictionary<int, string>> GetSupgLists(SupgFinder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            Dictionary<string, Dictionary<int, string>> List = null;
            try
            {
                List = remoteObject.GetSupgLists(finder, out ret);
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

                MonitorLog.WriteLog("Ошибка GetSupgLists \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return null;
            }
        }


        public List<Dest> GetDestName(int nzp_serv, enSrvOper oper, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Dest> List = null;
            try
            {
                List = remoteObject.GetDestName(nzp_serv, oper, out ret);
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

                MonitorLog.WriteLog("Ошибка GetDestName \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return null;
            }
        }



        public OrderContainer Result_Generating_Procedure(OrderContainer finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            OrderContainer resList = null;
            try
            {
                resList = remoteObject.Result_Generating_Procedure(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return resList;
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
                    ret.text = "Ошибка получения результата, срока выполнения, факта выполнения заявки";

                MonitorLog.WriteLog("Ошибка Result_Generating_Procedure \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return null;
            }
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
                listServices = remoteObject.GetServices(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return listServices;
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
                    ret.text = "Ошибка получения списка заявок";

                MonitorLog.WriteLog("Ошибка GetServices \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return null;
            }

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
                res = remoteObject.AddReaddress(finder, out ret);
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
                else
                    ret.text = "Ошибка добавления переадресации";

                MonitorLog.WriteLog("Ошибка AddReaddress \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return false;
            }
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
                listReadress = remoteObject.GetReadress(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return listReadress;
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
                    ret.text = "Ошибка получения списка переадресаций";

                MonitorLog.WriteLog("Ошибка GetReadress \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return null;
            }
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
                service = remoteObject.GetServiceForward_One(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return service;
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
                    ret.text = "Ошибка получения информации переадресации";

                MonitorLog.WriteLog("Ошибка GetServiceForward_One \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return null;
            }
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
                res = remoteObject.SaveCommentsReadress(finder, out ret);
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
                else
                    ret.text = "Ошибка обновления информации переадресации";

                MonitorLog.WriteLog("Ошибка SaveCommentsReadress \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return false;
            }
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
                order = remoteObject.Find_Orders_One(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return order;
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
                    ret.text = "Ошибка получения информации о выбранном заявлении";

                MonitorLog.WriteLog("Ошибка Find_Orders_One \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return null;
            }
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
                res = remoteObject.UpdateZvk(finder, out ret);
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
                else
                    ret.text = "Ошибка обновления информации о выбранной заявке";

                MonitorLog.WriteLog("Ошибка UpdateZvk \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return false;
            }
        }

        public Dictionary<int, string> GetAllServices(int nzp_user, out Returns ret)
        {
            ret = Utils.InitReturns();
            Dictionary<int, string> retDic = new Dictionary<int, string>();
            try
            {
                retDic = remoteObject.GetAllServices(nzp_user, out ret);
                HostChannel.CloseProxy(remoteObject);
                return retDic;
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
                    ret.text = "Ошибка получения информации о выбранных услугах";

                MonitorLog.WriteLog("Ошибка GetAllServices \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return null;
            }
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
            Dictionary<int, string> retDic = new Dictionary<int, string>();
            try
            {
                retDic = remoteObject.GetDest(nzp_serv, nzp_user, out ret);
                HostChannel.CloseProxy(remoteObject);
                return retDic;
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
                    ret.text = "Ошибка получения списка неисправностей по конкретной услуге";

                MonitorLog.WriteLog("Ошибка GetDest \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return null;
            }
        }

        public Dest GetNedops(Dest finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            Dest retNedop = null;
            try
            {
                retNedop = remoteObject.GetNedops(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return retNedop;
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
                    ret.text = "Ошибка получения недопоставки";

                MonitorLog.WriteLog("Ошибка GetNedops \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return null;
            }
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
            string retStr = "";
            try
            {
                retStr = remoteObject.GetSupplier(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return retStr;
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
                    ret.text = "Ошибка получения поставщика";

                MonitorLog.WriteLog("Ошибка GetSupplier \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return null;
            }
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
                retDic = remoteObject.GetSuppliersAll(nzp_user, supp_filter, pref, out ret);
                HostChannel.CloseProxy(remoteObject);
                return retDic;
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
                    ret.text = "Ошибка получения списка поставщиков";

                MonitorLog.WriteLog("Ошибка GetSuppliersAll \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return null;
            }
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
                res = remoteObject.AddJobOrder(ref finder, out ret);
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
                else
                    ret.text = "Ошибка добавления наряд-заказа";

                MonitorLog.WriteLog("Ошибка AddJobOrder \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return false;
            }
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
                retList = remoteObject.GetJobOrders(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return retList;
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
                    ret.text = "Ошибка получения наряд-заказов";

                MonitorLog.WriteLog("Ошибка GetJobOrders \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return null;
            }
        }

        public JobOrder GetJobOrderForm(JobOrder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            JobOrder jo;
            try
            {
                jo = remoteObject.GetJobOrderForm(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return jo;
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
                    ret.text = "Ошибка получения выбранного наряд-заказа";

                MonitorLog.WriteLog("Ошибка GetJobOrderForm \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return null;
            }
        }

        public Dictionary<int, string> GetJobOrderResultsAll(Finder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            Dictionary<int, string> retDic = null;
            try
            {
                retDic = remoteObject.GetJobOrderResultsAll(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return retDic;
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
                    ret.text = "Ошибка";

                MonitorLog.WriteLog("Ошибка  \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return null;
            }
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
                retDolg = remoteObject.GetDolgLs(finder, dat_y, dat_m, out ret);
                HostChannel.CloseProxy(remoteObject);
                return retDolg;
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
                    ret.text = "Ошибка";

                MonitorLog.WriteLog("Ошибка  \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return -1;
            }
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
                retDic = remoteObject.GetAttistation(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return retDic;
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
                    ret.text = "Ошибка";

                MonitorLog.WriteLog("Ошибка  \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return null;
            }
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
                res = remoteObject.AddRepeatedJobOrder(ref finder, out ret);
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
                else
                    ret.text = "Ошибка";

                MonitorLog.WriteLog("Ошибка  \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return false;
            }
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
                res = remoteObject.IsOrderClose(finder, out ret);
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
                else
                    ret.text = "Ошибка";

                MonitorLog.WriteLog("Ошибка  \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return false;
            }
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
                retList = remoteObject.GetNedopsAll(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return retList;
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
                    ret.text = "Ошибка";

                MonitorLog.WriteLog("Ошибка  \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return null;
            }
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
                res = remoteObject.CopyFields_WhenResultChanged(finder, out ret);
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
                else
                    ret.text = "Ошибка";

                MonitorLog.WriteLog("Ошибка  \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return false;
            }
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
                res = remoteObject.AddNedopJobOrder(finder, out ret);
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
                else
                    ret.text = "Ошибка";

                MonitorLog.WriteLog("Ошибка  \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return false;
            }
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
                res = remoteObject.UpdateZvk_armOperator(finder, out ret);
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
                else
                    ret.text = "Ошибка";

                MonitorLog.WriteLog("Ошибка  \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return false;
            }
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
                ret = remoteObject.DbChangeMarksSpisSupg(finder, list0, list1);
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
                else
                    ret.text = "Ошибка";

                MonitorLog.WriteLog("Ошибка  DbChangeMarksSpisSupg\n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return ret;
            }
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
                ret = remoteObject.GetSupgStatistics(finder);
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
                else
                    ret.text = "Ошибка";

                MonitorLog.WriteLog("Ошибка  GetSupgStatistics\n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return ret;
            }
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
                res = remoteObject.UpdateStatusJobOrder(finder, status, out ret);
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
                else
                    ret.text = "Ошибка";

                MonitorLog.WriteLog("Ошибка  UpdateStatusJobOrder\n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return res;
            }
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
                res = remoteObject.UpdateJobOrder(finder, proc, out ret);
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
                else
                    ret.text = "Ошибка";

                MonitorLog.WriteLog("Ошибка  UpdateJobOrder\n" + proc + ":" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return res;
            }
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
                journal = remoteObject.GetJournal(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return journal;
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
                    ret.text = "Ошибка";

                MonitorLog.WriteLog("Ошибка GetJournal\n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return null;
            }
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
                flag = remoteObject.NedopForming(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return flag;
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
                    ret.text = "Ошибка";

                MonitorLog.WriteLog("Ошибка NedopForming\n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return false;
            }
        }

        public bool NedopPlacement(Journal finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            bool flag = false;
            try
            {
                flag = remoteObject.NedopPlacement(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return flag;
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
                    ret.text = "Ошибка";

                MonitorLog.WriteLog("Ошибка NedopPlacement\n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return false;
            }
        }

        public bool NedopUnload(Journal finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            bool flag = false;
            try
            {
                flag = remoteObject.NedopUnload(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return flag;
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
                    ret.text = "Ошибка";

                MonitorLog.WriteLog("Ошибка NedopUnload\n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return false;
            }
        }
        
        public List<JobOrder> GetSpisNedop(JobOrder job_ord, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<JobOrder> list = null;
            try
            {
                list = remoteObject.GetSpisNedop(job_ord, out ret);
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
                else
                    ret.text = "Ошибка";

                MonitorLog.WriteLog("Ошибка GetSpisNedop\n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return null;
            }
        }



        public int SetZakazActActual(SupgFinder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            int count = -1;
            try
            {
                count = remoteObject.SetZakazActActual(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return count;
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
                    ret.text = "Ошибка";

                MonitorLog.WriteLog("Ошибка SetZakazActActual\n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return -1;
            }
        }


        public bool DeleteFromJournal(Journal finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            bool flag = false;
            try
            {
                flag = remoteObject.DeleteFromJournal(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return flag;
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
                    ret.text = "Ошибка";

                MonitorLog.WriteLog("Ошибка DeleteFromJournal\n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return false;
            }
        }

        public bool UpdateJournal(Journal finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            bool flag = false;
            try
            {
                flag = remoteObject.UpdateJournal(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return flag;
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
                    ret.text = "Ошибка";

                MonitorLog.WriteLog("Ошибка UpdateJournal\n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return false;
            }
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
                retDic = remoteObject.GetAnswers(out ret);
                HostChannel.CloseProxy(remoteObject);
                return retDic;
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
                    ret.text = "Ошибка";

                MonitorLog.WriteLog("Ошибка  \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return null;
            }
        }

        public List<SupgAct> GetActs(SupgActFinder finder, enSrvOper oper, out Returns ret)
        {
            try
            {
                List<SupgAct> list = remoteObject.GetActs(finder, oper, out ret);
                HostChannel.CloseProxy(remoteObject);
                return list;
            }
            catch (Exception ex)
            {
                ret = new Returns(false);
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else
                    ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetActs(" + oper.ToString() + ")\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public bool CheckToClose(JobOrder finder, string ord, out Returns ret)
        {
            try
            {
                bool res = remoteObject.CheckToClose(finder, ord, out ret);
                HostChannel.CloseProxy(remoteObject);
                return res;
            }
            catch (Exception ex)
            {
                ret = new Returns(false);
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else
                    ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка CheckToClose" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return false;
            }
        }

        public bool UpdatePlannedWorks(ref SupgAct finder, enSrvOper oper, out Returns ret)
        {
            try
            {
                bool res = remoteObject.UpdatePlannedWorks(ref finder, oper, out ret);
                HostChannel.CloseProxy(remoteObject);
                return res;
            }
            catch (Exception ex)
            {
                ret = new Returns(false);
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else
                    ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка UpdatePlannedWorks" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return false;
            }

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
                listServices = remoteObject.GetServiceCatalog(out ret);
                HostChannel.CloseProxy(remoteObject);
                return listServices;
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
                    ret.text = "Ошибка получения списка справочника служб";

                MonitorLog.WriteLog("Ошибка GetServiceCatalog \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return null;
            }
        }

        public bool UpdateServiceCatalog(ServiceForwarding finder, out Returns ret)
        {
            ret = Utils.InitReturns();

            try
            {
                bool res = remoteObject.UpdateServiceCatalog(finder, out ret);
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
                else
                    ret.text = "Ошибка обновления справочника служб";

                MonitorLog.WriteLog("Ошибка UpdateServiceCatalog \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return false;
            }
        }

        public Dictionary<int, string> GetPhoneList(string pref, int nzp_kvar, out Returns ret)
        {
            ret = Utils.InitReturns();
            Dictionary<int, string> Phones = null;

            try
            {
                Phones = remoteObject.GetPhoneList(pref, nzp_kvar, out ret);
                HostChannel.CloseProxy(remoteObject);
                return Phones;
            }
            catch (Exception ex)
            {
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else
                    ret.text = "Ошибка выборки списка возможных телефонов";

                MonitorLog.WriteLog("Ошибка GetPhoneList \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return Phones;
            }
        }

        public bool GetSuppEMail(string nzp_supp, out Returns ret)
        {
            ret = Utils.InitReturns();
            bool res = false;

            try
            {
                res = remoteObject.GetSuppEMail(nzp_supp, out ret);
                HostChannel.CloseProxy(remoteObject);
                return res;
            }
            catch (Exception ex)
            {
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else
                    ret.text = "Ошибка выборки электронного адреса";

                MonitorLog.WriteLog("Ошибка GetSuppEMail \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return res;
            }
        }


        /// <summary>
        /// Обновление справочников
        /// </summary>
        /// <param name="finder">объект поиска</param>
        /// <param name="oper">операция</param>        
        /// <returns>результат</returns>
        public bool UpdateSpravSupg (Dest finder, enSrvOper oper, out Returns ret)
        {
            ret = Utils.InitReturns();
            bool res = false;

            try
            {
                res = remoteObject.UpdateSpravSupg(finder, oper , out ret);
                HostChannel.CloseProxy(remoteObject);
                return res;
            }
            catch (Exception ex)
            {
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else
                    ret.text = "Ошибка функции обновления справочников";

                MonitorLog.WriteLog("Ошибка UpdateSpravSupg \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return res;
            }
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
                flag = remoteObject.FillLSSaldo(out ret);
                HostChannel.CloseProxy(remoteObject);
                return flag;
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
                    ret.text = "Ошибка";

                MonitorLog.WriteLog("Ошибка FillLSSaldo\n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return false;
            }
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
                flag = remoteObject.FillLSTarif(out ret);
                HostChannel.CloseProxy(remoteObject);
                return flag;
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
                    ret.text = "Ошибка";

                MonitorLog.WriteLog("Ошибка FillLSTarif\n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return false;
            }
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
                themes = remoteObject.GetThemesCatalog(out ret);
                HostChannel.CloseProxy(remoteObject);
                return themes;
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
                    ret.text = "Ошибка";

                MonitorLog.WriteLog("Ошибка GetThemesCatalog\n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return null;
            }
        }

        /// <summary>
        /// Изменить справочник Классификация сообщений
        /// </summary>
        /// <param name="ret"></param>
        /// <returns>bool</returns>
        public bool UpdateThemesCatalog(Sprav finder, out Returns ret)
        {
            bool res = false;
            ret = Utils.InitReturns();
            try
            {
                res = remoteObject.UpdateThemesCatalog(finder, out ret);
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
                else
                    ret.text = "Ошибка";

                MonitorLog.WriteLog("Ошибка UpdateThemesCatalog\n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return false;
            }
        }

    }
}
