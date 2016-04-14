using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Global;
using System.ServiceModel;

namespace STCLINE.KP50.Client
{
    public class cli_SubsidyRequest : I_SubsidyRequest
    {
        I_SubsidyRequest remoteObject;

        public cli_SubsidyRequest(int nzp_server)
        {
            _cli_SubsidyRequest(nzp_server, Constants.roleSubsidy);
        }

        public void _cli_SubsidyRequest(int nzp_server, int nzp_role)
        {
            string addrHost = "";
            //определить параметры доступа
            _RServer zap = MultiHost.GetServer(nzp_server);

            if (Points.IsMultiHost && nzp_server > 0)
            {
                addrHost = zap.ip_adr + WCFParams.AdresWcfWeb.srvSubsidyRequest;
                remoteObject = HostChannel.CreateInstance<I_SubsidyRequest>(zap.login, zap.pwd, addrHost);
            }
            else
            {
                //по-умолчанию
                if (nzp_role == Constants.roleSubsidy)
                    addrHost = WCFParams.AdresWcfWeb.Adres + WCFParams.AdresWcfWeb.srvSubsidyRequest;
                else addrHost = WCFParams.AdresWcfWeb.Adres + WCFParams.AdresWcfWeb.srvSubsidyRequest;
                zap.rcentr = "<локально>";
                remoteObject = HostChannel.CreateInstance<I_SubsidyRequest>(addrHost);
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

        /// <summary>
        /// Добавляет заявку на финансирование и ставит задание в очередь
        /// </summary>
        /// <param name="finRequest">Атрибуты заявки на финансирование</param>
        /// <returns>Результат</returns>
        public Returns AddSubsidyRequest(ref FinRequest finRequest)
        {
            Returns ret = Utils.InitReturns();
            
            try
            {
                ret = remoteObject.AddSubsidyRequest(ref finRequest);
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
                    ret.text = "Ошибка получения данных списка к перечислению";

                MonitorLog.WriteLog("Ошибка AddSubsidyRequest \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return ret;
            }
        }

        /// <summary>
        /// Обновляет атрибуты заявки на финансирование
        /// </summary>
        /// <param name="finRequest">трибуты заявки на финансирование</param>
        /// <returns>Результат</returns>
        public Returns UpdateSubsidyRequest(FinRequest finRequest)
        {
            Returns ret = Utils.InitReturns();

            try
            {
                ret = remoteObject.UpdateSubsidyRequest(finRequest);
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
                    ret.text = "Ошибка изменения данных списка к перечислению";

                MonitorLog.WriteLog("Ошибка UpdateSubsidyRequest \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return ret;
            }
        }

        /// <summary>
        /// Возвращает доступные для перехода статусы
        /// </summary>
        /// <param name="nzpStatus">Текущий статус заявки</param>
        /// <param name="ret"></param>
        /// <returns>Список статусов, куда можено перейти из текущего</returns>
        public List<int> ListAvailableSubsidyStatus(int nzpStatus, out Returns ret)
        {
            ret = Utils.InitReturns();

            try
            {
                List<int> listStatus = remoteObject.ListAvailableSubsidyStatus(nzpStatus,out ret);
                HostChannel.CloseProxy(remoteObject);
                return listStatus;
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
                    ret.text = "Ошибка получения доступных статусов списка к перечислению";

                MonitorLog.WriteLog("Ошибка ListAvailableSubsidyStatus \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return null;
            }
        }


        /// <summary>
        /// Проверяет текущий статус заявки и при необходимости меняет на перечислено
        /// или частично перечислено
        /// </summary>
        /// <param name="finRequest">Атрибуты заявки на финансирование, код (nzp_req) и год</param>
        /// <returns>Результат</returns>
        public Returns CheckSubsidyRequestStatus(FinRequest finRequest)
        {
            Returns ret = Utils.InitReturns();

            try
            {
                ret = remoteObject.CheckSubsidyRequestStatus(finRequest);
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
                    ret.text = "Ошибка получения данных завления";

                MonitorLog.WriteLog("Ошибка CheckSubsidyRequestStatus \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return ret;
            }
        }

        /// <summary>
        /// Расчет сальдо подрядчиклв частично, по списку подрядчиков
        /// </summary>
        /// <param name="finRequest">Текущий список перечисления</param>
        /// <param name="listpayers">Список подрядчиков</param>
        /// <returns>Результат</returns>
        public Returns CalcPartSaldoSubsidy(FinRequest finRequest, List<Payer> listpayers)
        {
            Returns ret = Utils.InitReturns();

            try
            {
                ret = remoteObject.CalcPartSaldoSubsidy(finRequest, listpayers);
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
                    ret.text = "Ошибка получения данных расчета сальдо";

                MonitorLog.WriteLog("Ошибка CalcPartSaldoSubsidy \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return ret;
            }
        }


        public Returns RequestFonTasks(int nzpReq, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                remoteObject.RequestFonTasks(nzpReq, out ret);
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
                    ret.text = "Ошибка расчета данных для аналитики";

                MonitorLog.WriteLog("Ошибка RequestFonTasksCalc \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }
    }
}
