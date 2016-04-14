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
    public class cli_ExcelRep : I_ExcelRep
    {
        I_ExcelRep remoteObject;

        public cli_ExcelRep(int nzp_server)
            : base()
        {
            _cli_ExcelRep(nzp_server);
        }

        void _cli_ExcelRep(int nzp_server)
        {
            string addrHost = "";
            //определить параметры доступа
            _RServer zap = MultiHost.GetServer(nzp_server);

            if (Points.IsMultiHost && nzp_server > 0)
            {
                addrHost = zap.ip_adr + WCFParams.AdresWcfWeb.srvExcel;
                remoteObject = HostChannel.CreateInstance<I_ExcelRep>(zap.login, zap.pwd, addrHost);
            }
            else
            {
                //по-умолчанию
                addrHost = WCFParams.AdresWcfWeb.Adres + WCFParams.AdresWcfWeb.srvExcel;
                zap.rcentr = "<локально>";
                remoteObject = HostChannel.CreateInstance<I_ExcelRep>(addrHost);
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

        ~cli_ExcelRep()
        {
            try { if (remoteObject != null) HostChannel.CloseProxy(remoteObject); }
            catch { }
        }

        //Генерация отчета Лицевые счета + домовые или квартирные параметры(Самара)
        public Returns CreateExcelReport_host(List<Prm> listprm, int nzp_user, string comment)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.CreateExcelReport_host(listprm, nzp_user, comment);
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
                    ret.text = "Ошибка вызова сервиса создания Excel отчетов";

                MonitorLog.WriteLog("Ошибка CreateExcelReport_host \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return ret;
            }
        }


        // отчет по реестру счетчиков по лицевым счетам
        public Returns GetRegisterCounters(SupgFinder finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GetRegisterCounters(finder);
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
                    ret.text = "Ошибка вызова сервиса создания Excel отчетов";

                MonitorLog.WriteLog("Ошибка GetRegisterCounters \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return ret;
            }
        }

        //Отчет: Сверка расчетов с жильцом по состоянию на
        public Returns GetSaldoServices(SupgFinder finder, int supp)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GetSaldoServices(finder, supp);
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
                    ret.text = "Ошибка вызова сервиса создания Excel отчетов";

                MonitorLog.WriteLog("Ошибка GetVerifCalcs \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return ret;
            }
        }

        //Отчет по начислениям по поставщикам 
        public Returns GetNachSupp(int supp, SupgFinder finder, int yearr, bool serv)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GetNachSupp(supp, finder, yearr, serv);
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
                    ret.text = "Ошибка вызова сервиса создания Excel отчетов";

                MonitorLog.WriteLog("Ошибка GetVerifCalcs \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return ret;
            }
        }

        //Отчет: Сверка расчетов с жильцом по состоянию на
        public Returns GetVerifCalcs(Ls finder, string yy_from, string mm_from, string yy_to, string mm_to)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GetVerifCalcs(finder, yy_from, mm_from, yy_to, mm_to);
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
                    ret.text = "Ошибка вызова сервиса создания Excel отчетов";

                MonitorLog.WriteLog("Ошибка GetVerifCalcs \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return ret;
            }
        }

        //Отчет: состояние жилого фонда по приборам учета за период
        public Returns GetStateGilFond(string yy_from, string mm_from, string yy_to, string mm_to)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GetStateGilFond(yy_from, mm_from, yy_to, mm_to);
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
                    ret.text = "Ошибка вызова сервиса создания Excel отчетов";

                MonitorLog.WriteLog("Ошибка GetStateGilFond \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return ret;
            }
        }

        //Отчет: Сверка расчетов с жильцом по состоянию на
        public Returns GetDebtCalcs(Ls finder, string yy_from, string mm_from, string yy_to, string mm_to)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GetDebtCalcs(finder, yy_from, mm_from, yy_to, mm_to);
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
                    ret.text = "Ошибка вызова сервиса создания Excel отчетов";

                MonitorLog.WriteLog("Ошибка GetDebtCalcs \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return ret;
            }
        }


        //Отчет: извещение за месяц
        public Returns GetNoticeCalcs(Ls finder, string yy, string mm)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GetNoticeCalcs(finder, yy, mm);
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
                    ret.text = "Ошибка вызова сервиса создания Excel отчетов";

                MonitorLog.WriteLog("Ошибка GetNoticeCalcs \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return ret;
            }
        }

        //Отчет: список заявок
        public Returns GetOrderList(int nzp_user)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GetOrderList(nzp_user);
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
                    ret.text = "Ошибка вызова сервиса создания Excel отчетов";

                MonitorLog.WriteLog("Ошибка GetOrderList \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return ret;
            }
        }

        //Отчет: список плановых работ
        public Returns GetPlannedWorksList(int nzp_user, enSrvOper en)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GetPlannedWorksList(nzp_user, en);
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
                    ret.text = "Ошибка вызова сервиса создания Excel отчетов";

                MonitorLog.WriteLog("Ошибка GetPlannedWorksList \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return ret;
            }
        }

        //Отчет: 
        public Returns GetCountOrders(Ls finder, string _nzp, string _nzp_add, string s_date, string po_date)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GetCountOrders(finder, _nzp, _nzp_add, s_date, po_date);
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
                    ret.text = "Ошибка вызова сервиса создания Excel отчетов";

                MonitorLog.WriteLog("Ошибка GetCountOrders \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return ret;
            }
        }

        //Отчет: 
        public Returns GetSupgReports(SupgFinder supg_finder, enSrvOper en)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GetSupgReports(supg_finder, en);
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
                    ret.text = "Ошибка вызова сервиса создания Excel отчетов";

                MonitorLog.WriteLog("Ошибка GetCountOrders \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return ret;
            }
        }


        //Отчет: список нарядов-заказов для выполнения
        public Returns GetIncomingJobOrders(int nzp_user)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GetIncomingJobOrders(nzp_user);
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
                    ret.text = "Ошибка вызова сервиса создания Excel отчетов";

                MonitorLog.WriteLog("Ошибка GetIncomingJobOrders \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return ret;
            }
        }

        //Отчет: список нарядов-заказов для выполнения
        public Returns GetRepNedopList(int nzp_user, int nzp_jrn)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GetRepNedopList(nzp_user, nzp_jrn);
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
                    ret.text = "Ошибка вызова сервиса создания Excel отчетов";

                MonitorLog.WriteLog("Ошибка GetRepNedopList \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return ret;
            }
        }

        //Отчет: оплата гражданами-получателями коммунальных услуг за поставленные услуги
        public Returns GetDeliveredServicesPayment(Ls finder, int nzp_supp, string yy, string mm)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GetDeliveredServicesPayment(finder, nzp_supp, yy, mm);
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
                    ret.text = "Ошибка вызова сервиса создания Excel отчетов";

                MonitorLog.WriteLog("Ошибка GetDeliveredServicesPayment \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return ret;
            }
        }


        //Отчет: получение начислений по дому
        public Returns GetDomCalcs(int Nzp_user, string mm, string yy)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GetDomCalcs(Nzp_user, mm, yy);
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
                    ret.text = "Ошибка вызова сервиса создания Excel отчетов";

                MonitorLog.WriteLog("Ошибка GetDomCalcs \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return ret;
            }
        }

        //Отчет: получение начислений по дому
        public Returns GetAnalisKart(List<Prm> listprm, int Nzp_user)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GetAnalisKart(listprm, Nzp_user);
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
                    ret.text = "Ошибка вызова сервиса создания Excel отчетов";

                MonitorLog.WriteLog("Ошибка GetAnalisKart \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return ret;
            }
        }

        //Отчет: калькуляция тарифа по содержанию жилья
        public Returns GetCalcTarif(Prm prm, int Nzp_user)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GetCalcTarif(prm, Nzp_user);
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
                    ret.text = "Ошибка вызова сервиса создания Excel отчетов";

                MonitorLog.WriteLog("Ошибка GetAnalisKart \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return ret;
            }
        }

        //Отчет: получение начислений по дому
        public Returns GetDomNach(List<Prm> listprm, int Nzp_user)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GetDomNach(listprm, Nzp_user);
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
                    ret.text = "Ошибка вызова сервиса создания Excel отчетов";

                MonitorLog.WriteLog("Ошибка GetDomNach \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return ret;
            }
        }


        //Отчет: Сальдовая оборотная ведомость
        public Returns GetSaldoRep10_14_3(Prm prm_, int Nzp_user)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GetSaldoRep10_14_3(prm_, Nzp_user);
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
                    ret.text = "Ошибка вызова сервиса создания Excel отчетов";

                MonitorLog.WriteLog("Ошибка GetSaldoRep10_14_3 \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return ret;
            }
        }

        //Отчет: Сальдовая оборотная ведомость
        public Returns GetSaldoRep10_14_1(Prm prm_, int Nzp_user)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GetSaldoRep10_14_1(prm_, Nzp_user);
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
                    ret.text = "Ошибка вызова сервиса создания Excel отчетов";

                MonitorLog.WriteLog("Ошибка GetSaldoRep10_14_1 \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return ret;
            }
        }

        //Отчет: Сверка поступлений за день
        public Returns GetSverkaDay(ExcelSverkaPeriod prm_, int Nzp_user)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GetSverkaDay(prm_, Nzp_user);
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
                    ret.text = "Ошибка вызова сервиса создания Excel отчетов";

                MonitorLog.WriteLog("Ошибка GetSverkaDay \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return ret;
            }
        }

        //Отчет: Сверка поступлений за месяц
        public Returns GetSverkaMonth(ExcelSverkaPeriod prm_, int Nzp_user)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GetSverkaMonth(prm_, Nzp_user);
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
                    ret.text = "Ошибка вызова сервиса создания Excel отчетов";

                MonitorLog.WriteLog("Ошибка GetSverkaMonth \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return ret;
            }
        }

        public Returns GetDataSaldoPoPerechisl(MoneyDistrib finder, int Nzp_user)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GetDataSaldoPoPerechisl(finder, Nzp_user);
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
                    ret.text = "Ошибка вызова сервиса создания Excel отчетов";

                MonitorLog.WriteLog("Ошибка GetDataSaldoPoPerechisl \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return ret;
            }
        }

        //Отчет: Начисления по поставщику
        public Returns GetCharges(Prm prm_, int Nzp_user)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GetCharges(prm_, Nzp_user);
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
                    ret.text = "Ошибка вызова сервиса создания Excel отчетов";

                MonitorLog.WriteLog("Ошибка GetCharges \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return ret;
            }
        }

        //Отчет: получение начислений по дому
        public Returns GetDomNachPere(List<Prm> listprm, int Nzp_user)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GetDomNachPere(listprm, Nzp_user);
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
                    ret.text = "Ошибка вызова сервиса создания Excel отчетов";

                MonitorLog.WriteLog("Ошибка GetDomNachPere \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return ret;
            }
        }

        //Отчет: справка по поставщикам
        public Returns GetSpravSuppNach(List<Prm> listprm, int Nzp_user)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GetSpravSuppNach(listprm, Nzp_user);
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
                    ret.text = "Ошибка вызова сервиса создания Excel отчетов";

                MonitorLog.WriteLog("Ошибка GetAnalisKart \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return ret;
            }
        }

        //Отчет: справка по поставщикам
        public Returns GetSpravSuppNachHar(Prm prm_, int Nzp_user)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GetSpravSuppNachHar(prm_, Nzp_user);
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
                    ret.text = "Ошибка вызова сервиса создания Excel отчетов";

                MonitorLog.WriteLog("Ошибка GetAnalisKart \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return ret;
            }
        }
        //Отчет: справка по поставщикам форма 3
        public Returns GetSpravSuppNachHar2(Prm prm_, int Nzp_user)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GetSpravSuppNachHar2(prm_, Nzp_user);
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
                    ret.text = "Ошибка вызова сервиса создания Excel отчетов";

                MonitorLog.WriteLog("Ошибка GetSpravSuppNachHar2 \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return ret;
            }
        }

        //Отчет: справка о наличии задолженности
        public Returns GetSpravHasDolg(Prm prm_, int Nzp_user)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GetSpravHasDolg(prm_, Nzp_user);
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
                    ret.text = "Ошибка вызова сервиса создания Excel отчетов";

                MonitorLog.WriteLog("Ошибка GetSpravHasDolg \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return ret;
            }
        }

        //Отчет: справка по лицевому счету
        public Returns GetLicSchetExcel(Ls finder, int year_, int month_)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GetLicSchetExcel(finder, year_, month_);
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
                    ret.text = "Ошибка вызова сервиса создания Excel отчетов";

                MonitorLog.WriteLog("Ошибка GetSpravHasDolg \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return ret;
            }
        }

        //Отчет: Акт сверки
        public Returns GetEnergoActSverki(Prm prm_, int nzp_supp, int Nzp_user)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GetEnergoActSverki(prm_, nzp_supp, Nzp_user);
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
                    ret.text = "Ошибка вызова сервиса создания Excel отчетов";

                MonitorLog.WriteLog("Ошибка GetEnergoAktSverki \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return ret;
            }
        }

        //Отчет: справка о наличии задолженности
        public Returns GetSpravPULs(Prm prm_, int Nzp_user)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GetSpravPULs(prm_, Nzp_user);
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
                    ret.text = "Ошибка вызова сервиса создания Excel отчетов";

                MonitorLog.WriteLog("Ошибка GetSpravPuLs \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return ret;
            }
        }

        //Отчет: Ведомость оплат по ЛС
        public Returns GetVedOplLs(Prm prm, int Nzp_user)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GetVedOplLs(prm, Nzp_user);
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
                    ret.text = "Ошибка вызова сервиса создания Excel отчетов";

                MonitorLog.WriteLog("Ошибка GetVedOplLs \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return ret;
            }
        }

        //Отчет: Ведомость перерасчетов
        public Returns GetVedPere(Prm prm, int Nzp_user)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GetVedPere(prm, Nzp_user);
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
                    ret.text = "Ошибка вызова сервиса создания Excel отчетов";

                MonitorLog.WriteLog("Ошибка GetVedPere \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return ret;
            }
        }

        //Отчет: Сведения о просроченной задолженности
        public Returns GetDolgSved(Prm prm, int Nzp_user)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GetDolgSved(prm, Nzp_user);
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
                    ret.text = "Ошибка вызова сервиса создания Excel отчетов";

                MonitorLog.WriteLog("Ошибка GetDolgSved \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return ret;
            }
        }

        //Отчет: Список должников
        public Returns GetDolgSpis(Prm prm, int Nzp_user)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GetDolgSpis(prm, Nzp_user);
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
                    ret.text = "Ошибка вызова сервиса создания Excel отчетов";

                MonitorLog.WriteLog("Ошибка GetDolgSpis \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return ret;
            }
        }

        //Отчет: Список Рассогласований с паспортисткой
        public Returns GetPaspRas(Prm prm, int Nzp_user)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GetPaspRas(prm, Nzp_user);
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
                    ret.text = "Ошибка вызова сервиса создания Excel отчетов";

                MonitorLog.WriteLog("Ошибка GetPaspRas \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return ret;
            }
        }

        /// <summary>
        /// Отчет: Рассогласований с паспортисткой для г.Губкина
        /// </summary>
        /// <param name="prm"></param>
        /// <param name="Nzp_user"></param>
        /// <returns></returns>
        public Returns GetPaspRasCommon(Prm prm, int Nzp_user)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GetPaspRasCommon(prm, Nzp_user);
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
                    ret.text = "Ошибка вызова сервиса создания Excel отчетов";

                MonitorLog.WriteLog("Ошибка GetPaspRasCommon \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return ret;
            }
        }

        //Отчет: Справка о начислении платы по виду услуги содержание жилья
        public Returns GetSpravSoderg(Prm prm, int Nzp_user)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GetSpravSoderg(prm, Nzp_user);
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
                    ret.text = "Ошибка вызова сервиса создания Excel отчетов";

                MonitorLog.WriteLog("Ошибка GetSpravSoderg \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return ret;
            }
        }

        //Отчет: Справка о начислении платы по виду услуги форма 2
        public Returns GetSpravSoderg2(Prm prm, int Nzp_user)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GetSpravSoderg2(prm, Nzp_user);
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
                    ret.text = "Ошибка вызова сервиса создания Excel отчетов";

                MonitorLog.WriteLog("Ошибка GetSpravSoderg \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return ret;
            }
        }

        //Отчет: Справка по услугам группы содержание жилья
        public Returns GetSpravGroupSodergGil(Prm prm, int Nzp_user)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GetSpravGroupSodergGil(prm, Nzp_user);
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
                    ret.text = "Ошибка вызова сервиса создания Excel отчетов";

                MonitorLog.WriteLog("Ошибка GetSpravSoderg \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return ret;
            }
        }
        //Отчет:Сводная ведомость по нормативам потребления
        public Returns GetVedNormPotr(Prm prm, int Nzp_user)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GetVedNormPotr(prm, Nzp_user);
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
                    ret.text = "Ошибка вызова сервиса создания Excel отчетов";

                MonitorLog.WriteLog("Ошибка GetVedNormPotr \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return ret;
            }
        }

        //Отчет: состояние жилого фонда
        public Returns GetSostGilFond(Prm prm, int Nzp_user)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GetSostGilFond(prm, Nzp_user);
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
                    ret.text = "Ошибка вызова сервиса создания Excel отчетов";

                MonitorLog.WriteLog("Ошибка GetSostGilFond \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return ret;
            }
        }

        //Отчет:Формирование квитанций на оплату
        public Returns GetFakturaFiles(Prm prm, int Nzp_user)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GetFakturaFiles(prm, Nzp_user);
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
                    ret.text = "Ошибка вызова сервиса создания Excel отчетов";

                MonitorLog.WriteLog("Ошибка GetFakturaFiles \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return ret;
            }
        }


        //Отчет: Справка по отключениям подачи коммунальных услуг
        public Returns GetSpravPoOtklUslug(Ls finder, int nzp_serv, int month, int year)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GetSpravPoOtklUslug(finder, nzp_serv, month, year);
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
                    ret.text = "Ошибка вызова сервиса создания Excel отчетов";

                MonitorLog.WriteLog("Ошибка GetSpravPoOtklUslug \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return ret;
            }
        }



        //Отчет: Справка по отключениям подачи коммунальных услуг по домам с указанием виновника
        public Returns GetSpravPoOtklUslugDomVinovnik(Prm prm)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GetSpravPoOtklUslugDomVinovnik(prm);
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
                    ret.text = "Ошибка вызова сервиса создания Excel отчетов";

                MonitorLog.WriteLog("Ошибка GetSpravPoOtklUslugDomVinovnik \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return ret;
            }
        }

        
        /// <summary>
        /// Отчет:50.1 Сальдовая ведомость по энергосбыту
        /// </summary>
        /// <param name="prm"></param>
        /// <returns></returns>
        public Returns GetSaldoVedEnergo(Prm prm)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GetSaldoVedEnergo(prm);
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
                    ret.text = "Ошибка вызова сервиса создания Excel отчетов";

                MonitorLog.WriteLog("Ошибка GetSaldoVedEnergo \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return ret;
            }
        }


        /// <summary>
        /// Отчет:3.70 Сводный отчет по начислениям для Тулы 
        /// </summary>
        /// <param name="prm"></param>
        /// <returns></returns>
        public Returns GetServSuppNach(ReportPrm prm)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GetServSuppNach(prm);
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
                    ret.text = "Ошибка вызова сервиса создания Excel отчетов";

                MonitorLog.WriteLog("Ошибка GetServSuppNach \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return ret;
            }
        }

        /// <summary>
        /// Отчет:3.71 Сводный отчет по поступлениям платежей для Тулы 
        /// </summary>
        /// <param name="prm"></param>
        /// <returns></returns>
        public Returns GetServSuppMoney(ReportPrm prm)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GetServSuppMoney(prm);
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
                    ret.text = "Ошибка вызова сервиса создания Excel отчетов";

                MonitorLog.WriteLog("Ошибка GetServSuppNach \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return ret;
            }
        }


        /// <summary>
        /// Отчет: Список домов с указанием количества распечатанных счетов
        /// </summary>
        /// <param name="prm"></param>
        /// <returns></returns>
        public Returns GetListDomFaktura(ReportPrm prm)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GetListDomFaktura(prm);
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
                    ret.text = "Ошибка вызова сервиса создания Excel отчетов";

                MonitorLog.WriteLog("Ошибка GetServSuppNach \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return ret;
            }
        }

        

        /// <summary>
        /// Отчет:Список временно зарегистрированных
        /// </summary>
        /// <param name="prm"></param>
        /// <returns></returns>
        public Returns GetVremZareg(Kart finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GetVremZareg(finder);
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
                    ret.text = "Ошибка вызова сервиса создания Excel отчетов";

                MonitorLog.WriteLog("Ошибка GetVremZareg \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return ret;
            }
        }

        /// <summary>
        /// Отчет:Список для военкомата
        /// </summary>
        /// <param name="prm"></param>
        /// <returns></returns>
        public Returns GetVoenkomat(Kart finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GetVoenkomat(finder);
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
                    ret.text = "Ошибка вызова сервиса создания Excel отчетов";

                MonitorLog.WriteLog("Ошибка GetVoenkomat \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return ret;
            }
        }

        /// <summary>
        /// Отчет: 50.2 Ведомость должников
        /// </summary>
        /// <param name="prm"></param>
        /// <returns></returns>
        public Returns GetDolgSpisEnergo(Prm prm)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GetDolgSpisEnergo(prm);
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
                    ret.text = "Ошибка вызова сервиса создания Excel отчетов";

                MonitorLog.WriteLog("Ошибка GetDolgSpisEnergo \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return ret;
            }
        }

        /// <summary>
        /// Отчет: протокол сверки данных
        /// </summary>
        /// <param name="prm"></param>
        /// <returns></returns>
        public Returns GetProtocolSverData(Finder finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GetProtocolSverData(finder);
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
                    ret.text = "Ошибка вызова сервиса создания Excel отчетов";

                MonitorLog.WriteLog("Ошибка GetProtocolSverData \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return ret;
            }
        }

        /// <summary>
        /// Отчет: протокол сверки данных: лиц. счетов и домов
        /// </summary>
        /// <param name="prm"></param>
        /// <returns></returns>
        public Returns GetProtocolSverDataLsDom(Prm p)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GetProtocolSverDataLsDom(p);
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
                    ret.text = "Ошибка вызова сервиса создания Excel отчетов";

                MonitorLog.WriteLog("Ошибка GetProtocolSverDataLsDom \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return ret;
            }
        }

        //Справка по поставщикам коммунальных услуг ф.3
        public Returns GetSpravSuppSamara(Prm prm)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GetSpravSuppSamara(prm);
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
                    ret.text = "Ошибка вызова сервиса создания Excel отчетов";

                MonitorLog.WriteLog("Ошибка GetSpravSuppSamara \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return ret;
            }
        }

        //Отчет: Справка по отключениям подачи коммунальных услуг по домам 
        public Returns GetSpravPoOtklUslugDom(Prm prm)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GetSpravPoOtklUslugDom(prm);
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
                    ret.text = "Ошибка вызова сервиса создания Excel отчетов";

                MonitorLog.WriteLog("Ошибка GetSpravPoOtklUslugDom \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return ret;
            }
        }


        //Отчет: Справка по отключениям подачи коммунальных услуг по ЖЭУ с указанием виновника
        public Returns GetSpravPoOtklUslugGeuVinovnik(Prm prm)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GetSpravPoOtklUslugGeuVinovnik(prm);
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
                    ret.text = "Ошибка вызова сервиса создания Excel отчетов";

                MonitorLog.WriteLog("Ошибка GetSpravPoOtklUslugGeuVinovnik \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return ret;
            }
        }

        //закомментрировал 24.06.2013 т.к. похоже ни где не используется
        //public static DataTable GetDataReportGenertor(out Returns ret, string Nzp_user)
        //{
        //    ExcelRepClient db = new ExcelRepClient();
        //    DataTable res = db.GetDataReportGenertor(out ret, Nzp_user);
        //    db.Close();
        //    return res;
        //}

        ////достать DataTable: получение начислений по дому
        //public DataTable GetDomCalcs_table(int Nzp_user, string mm, string yy)
        //{
        //    Returns ret = Utils.InitReturns();
        //    try
        //    {
        //        DataTable dt = remoteObject.GetDomCalcs_table(Nzp_user, mm, yy);
        //        HostChannel.CloseProxy(remoteObject);
        //        return dt;
        //    }
        //    catch (Exception ex)
        //    {

        //        MonitorLog.WriteLog("Ошибка GetDomCalcs_table \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

        //        return null;
        //    }
        //}

        public static List<ExcelUtility> GetListReport(ExcelUtility finder, out Returns ret)
        {
            ExcelRepClient db = new ExcelRepClient();
            List<ExcelUtility> res = db.GetListReport(finder, out ret);
            db.Close();
            return res;
        }

        /// <summary>
        /// Отчет: Информация по расчетам с начелением
        /// </summary>
        /// <param name="finder">Объект поиска типа Ls</param>
        /// <param name="month">Текущий месяц</param>
        /// <param name="year">Текущий год</param>
        /// <returns>Объект-результат Returns</returns>
        public Returns GetInfPoRaschetNasel(Ls finder, int nzp_supp, int month, int year)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GetInfPoRaschetNasel(finder, nzp_supp, month, year);
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
                    ret.text = "Ошибка вызова сервиса создания Excel отчетов";

                MonitorLog.WriteLog("Ошибка GetInfPoRaschetNasel \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return ret;
            }
        }

        /// <summary>
        /// Получение данных Отчет: Генератор по начислениям
        /// </summary>
        /// <param name="finder">Объект поиска</param>
        /// <param name="par">Список параметров-начислений которых необходимо вывести</param>
        /// <param name="month">Текущий месяц</param>
        /// <param name="year">Текущий год</param>
        /// <returns>объект Returns</returns>
        public Returns GetReportPrmNach(Ls finder, List<int> par, int month, int year, string comment, List<int> services, bool isShowExpanded)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GetReportPrmNach(finder, par, month, year, comment, services, isShowExpanded);
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
                    ret.text = "Ошибка вызова сервиса создания Excel отчетов";

                MonitorLog.WriteLog("Ошибка GetReportPrmNach \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return ret;
            }
        }

        public Returns GetControlDistribPayments(Payments pay)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GetControlDistribPayments(pay);
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
                    ret.text = "Ошибка вызова сервиса создания Excel отчетов";

                MonitorLog.WriteLog("Ошибка GetControlDistribPayments \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return ret;
            }
        }

        //Отчет: Протокол рассчитанных значений ОДН
        public Returns GetProtCalcOdn(Prm prm, int Nzp_user)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GetProtCalcOdn(prm, Nzp_user);
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
                    ret.text = "Ошибка вызова сервиса создания Excel отчетов";

                MonitorLog.WriteLog("Ошибка GetProtCalcOdn \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return ret;
            }
        }

        //Отчет: Протокол рассчитанных значений ОДН расширенный
        public Returns GetProtCalcOdn2(Prm prm, int Nzp_user)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GetProtCalcOdn2(prm, Nzp_user);
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
                    ret.text = "Ошибка вызова сервиса создания Excel отчетов";

                MonitorLog.WriteLog("Ошибка GetProtCalcOdn2 \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return ret;
            }
        }

        // Выгрузка сальдо в банк
        public Returns GetSaldo_v_bank(SupgFinder finder, string year, string month, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GetSaldo_v_bank(finder, year, month, out ret);
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
                else ret.text = "Ошибка выгрузки данных";

                MonitorLog.WriteLog("Ошибка GetSaldo_v_bank \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        // Загрузка адресного пространства из КЛАДР
        public Returns UploadKLADRAddrSpace(KLADRFinder finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.UploadKLADRAddrSpace(finder, out ret);
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
                else ret.text = "Ошибка выгрузки данных";

                MonitorLog.WriteLog("Ошибка UploadKLADRAddrSpace \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        // Выгрузка файла обмена
        public Returns GenerateExchange(SupgFinder finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GenerateExchange(finder, out ret);
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
                else ret.text = "Ошибка выгрузки данных";

                MonitorLog.WriteLog("Ошибка GenerateExchange \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        // Выгрузка начислений УЭС
        public Returns GenerateUESVigr(SupgFinder finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GenerateUESVigr(finder, out ret);
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
                else ret.text = "Ошибка выгрузки данных";

                MonitorLog.WriteLog("Ошибка GenerateUESVigr \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        // Выгрузка оплат МУРЦ
        public Returns GenerateMURCVigr(SupgFinder finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GenerateMURCVigr(finder, out ret);
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
                else ret.text = "Ошибка выгрузки данных";

                MonitorLog.WriteLog("Ошибка GenerateMURCVigr \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }        

         // Выгрузка показаний ПУ
        public Returns GetUploadCharge(SupgFinder finder, string year, string month, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GetUploadCharge(finder, year, month, out ret);
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
                else ret.text = "Ошибка выгрузки данных";

                MonitorLog.WriteLog("Ошибка GetUploadPU \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        // Выгрузка показаний ПУ
        public Returns GetUploadPU(SupgFinder finder, string year, string month, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GetUploadPU(finder, year, month, out ret);
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
                else ret.text = "Ошибка выгрузки данных";

                MonitorLog.WriteLog("Ошибка GetUploadPU \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

     


        // Выгрузка реестра для загрузки в БС
        public Returns GetUploadReestr(Finder finder, List<int> BanksList, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GetUploadReestr(finder, BanksList, out ret);
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
                else ret.text = "Ошибка выгрузки данных";

                MonitorLog.WriteLog("Ошибка GetUploadReestr \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }


        
  

        // Выгрузка в кассу 3.0
        public Returns GetUploadKassa(SupgFinder finder, string year, string month, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GetUploadKassa(finder, year, month, out ret);
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
                else ret.text = "Ошибка выгрузки данных";

                MonitorLog.WriteLog("Ошибка GetUploadKassa \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        // Отчет сальдовая ведомость 5.10
        public Returns GetSaldo_5_10(ChargeFind finder,out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GetSaldo_5_10(finder, out ret);
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
                else ret.text = "Ошибка выгрузки данных";

                MonitorLog.WriteLog("Ошибка GetSaldo_5_10 \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        // Выгрузка для обмена с соц.защитой (Тула)
        public Returns GetExchangeSZ(Finder finder, string year, string month,int nzp_ex_sz)
        //----------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GetExchangeSZ(finder, year, month,nzp_ex_sz);
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
                else ret.text = "Ошибка выгрузки данных";

                MonitorLog.WriteLog("Ошибка GetExchangeSZ \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public Returns GetUploadExchangeSZ(Finder finder, string file_name)
        {
            var ret = new Returns();
            try
            {
                ret = remoteObject.GetUploadExchangeSZ(finder, file_name);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = "\n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetUploadExchangeSZ" + err, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        // Выгрузка ЛС и адресов
        public Returns GetUploadLsAdr(Finder finder, string list_towns)
        //----------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GetUploadLsAdr(finder, list_towns);
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
                else ret.text = "Ошибка выгрузки данных";

                MonitorLog.WriteLog("Ошибка GetUploadLsAdr \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        // Протокол по оплатам для ВТБ24
        public Returns GetProtocolVTB24(ExFinder finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GetProtocolVTB24(finder, out ret);
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
                else ret.text = "Ошибка выгрузки данных";

                MonitorLog.WriteLog("Ошибка GetProtocolVTB24 \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        #region отчеты для системы должники
        /// <summary>
        /// напоминание
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public Returns GetReminderToDebitor(Deal finder, ReportType type)
        //----------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GetReminderToDebitor(finder, type);
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
                else ret.text = "Ошибка выгрузки данных";

                MonitorLog.WriteLog("Ошибка GetReminderToDebitor \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        //уведомление
        public Returns GetNoticeToDebitor(Deal finder, ReportType type)
        //----------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GetNoticeToDebitor(finder, type);
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
                else ret.text = "Ошибка выгрузки данных";

                MonitorLog.WriteLog("Ошибка GetNoticeToDebitor \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public Returns GetWarningToDebitor(Deal finder, ReportType type)
        //----------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GetWarningToDebitor(finder, type);
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
                else ret.text = "Ошибка выгрузки данных";

                MonitorLog.WriteLog("Ошибка GetWarningToDebitor \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        
        #endregion 


        #region Отчеты для Тулы

        /// <summary>
        /// 3.73 Сводный отчет по принятым и перечисленным средствам
        /// </summary>
        /// <param name="prm"></param>
        /// <returns></returns>
        public Returns GetServSuppMoney2(ReportPrm prm)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GetServSuppMoney2(prm);
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
                    ret.text = "Ошибка вызова сервиса создания Excel отчетов";

                MonitorLog.WriteLog("Ошибка GetServSuppMoney2 \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return ret;
            }
        }


        /// <summary>
        /// Отчет: Справка по должникам по Туле
        /// </summary>
        /// <param name="prm"></param>
        /// <returns></returns>
        public Returns GetSpravDolgTula(ReportPrm prm)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GetSpravDolgTula(prm);
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
                    ret.text = "Ошибка вызова сервиса создания Excel отчетов";

                MonitorLog.WriteLog("Ошибка GetSpravDolgTula \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return ret;
            }
        }


        /// <summary>
        ///  Отчет по должникам по Туле
        /// </summary>
        /// <param name="prm"></param>
        /// <returns></returns>
        public Returns GetListDolgTula(ReportPrm prm)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GetListDolgTula(prm);
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
                    ret.text = "Ошибка вызова сервиса создания Excel отчетов";

                MonitorLog.WriteLog("Ошибка GetListDolgTula \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return ret;
            }
        }


        /// <summary>
        /// Отчет справка по поставщикам Тула
        /// </summary>
        /// <param name="prm"></param>
        /// <returns></returns>
        public Returns GetSpravSuppTula(ReportPrm prm)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.GetSpravSuppTula(prm);
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
                    ret.text = "Ошибка вызова сервиса создания Excel отчетов";

                MonitorLog.WriteLog("Ошибка GetSpravSuppTula \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return ret;
            }
        }

        #endregion 

        #region Передача файла по WCF
        public byte[] GetFile(string path)
        {
            byte[] b = null;
            try
            {
                b = remoteObject.GetFile(path);
                HostChannel.CloseProxy(remoteObject);
                return b;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка GetFile \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return null;
            }
        }
        #endregion

        #region универсальный сервер отчетов

        public List<Dict> GetReportDicts(List<int> idDicts, bool loadDictsData, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                List<Dict> res = remoteObject.GetReportDicts(idDicts, loadDictsData, out ret);
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
                else ret.text = "";
                MonitorLog.WriteLog("Ошибка GetReportDicts \n " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        #endregion


        #region Смена УК для списка ЛС
        //----------------------------------------------------------------------
        public Returns ChangeArea(FinderChangeArea finder)
        //----------------------------------------------------------------------
        {
            Returns ret;
            ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.ChangeArea(finder);
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
                else ret.text = "Ошибка смены УК";

                MonitorLog.WriteLog("Ошибка ChangeArea \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }
        #endregion
    }
}
