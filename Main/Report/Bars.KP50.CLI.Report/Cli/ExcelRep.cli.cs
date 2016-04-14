using System;
using System.ServiceModel;
using System.Collections.Generic;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Global;



namespace STCLINE.KP50.Client
{
    public class cli_ExcelRep : cli_Base, I_ExcelRep
    {
        //I_ExcelRep remoteObject;

        public cli_ExcelRep(int nzp_server)
            : base()
        {
            //_cli_ExcelRep(nzp_server);
        }

        IExcelRepRemoteObject getRemoteObject()
        {
            return getRemoteObject<IExcelRepRemoteObject>(WCFParams.AdresWcfWeb.srvExcel);
        }

        //void _cli_ExcelRep(int nzp_server)
        //{
        //    string addrHost = "";
        //    //определить параметры доступа
        //    _RServer zap = MultiHost.GetServer(nzp_server);

        //    if (Points.IsMultiHost && nzp_server > 0)
        //    {
        //        addrHost = zap.ip_adr + WCFParams.AdresWcfWeb.srvExcel;
        //        remoteObject = HostChannel.CreateInstance<I_ExcelRep>(zap.login, zap.pwd, addrHost);
        //    }
        //    else
        //    {
        //        //по-умолчанию
        //        addrHost = WCFParams.AdresWcfWeb.Adres + WCFParams.AdresWcfWeb.srvExcel;
        //        zap.rcentr = "<локально>";
        //        remoteObject = HostChannel.CreateInstance<I_ExcelRep>(addrHost);
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

        //~cli_ExcelRep()
        //{
        //    try { if (remoteObject != null) HostChannel.CloseProxy(remoteObject); }
        //    catch { }
        //}

        //Генерация отчета Лицевые счета + домовые или квартирные параметры(Самара)
        public Returns CreateExcelReport_host(List<Prm> listprm, int nzp_user, string comment)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.CreateExcelReport_host(listprm, nzp_user, comment);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка CreateExcelReport_host\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка CreateExcelReport_host\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }


        // отчет по реестру счетчиков по лицевым счетам
        public Returns GetRegisterCounters(SupgFinder finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetRegisterCounters(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetRegisterCounters\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка GetRegisterCounters\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        //Отчет: Сверка расчетов с жильцом по состоянию на
        public Returns GetSaldoServices(SupgFinder finder, int supp)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetSaldoServices(finder, supp);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetSaldoServices\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка GetSaldoServices\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        //Отчет по начислениям по поставщикам 
        public Returns GetNachSupp(int supp, SupgFinder finder, int yearr, bool serv)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetNachSupp(supp, finder, yearr, serv);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetNachSupp\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка GetNachSupp\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        //Отчет: Сверка расчетов с жильцом по состоянию на
        public Returns GetVerifCalcs(Ls finder, string yy_from, string mm_from, string yy_to, string mm_to)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetVerifCalcs(finder, yy_from, mm_from, yy_to, mm_to);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetVerifCalcs\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка GetVerifCalcs\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        //Отчет: состояние жилого фонда по приборам учета за период
        public Returns GetStateGilFond(string yy_from, string mm_from, string yy_to, string mm_to)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetStateGilFond(yy_from, mm_from, yy_to, mm_to);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetStateGilFond\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка GetStateGilFond\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        //Отчет: Сверка расчетов с жильцом по состоянию на
        public Returns GetDebtCalcs(Ls finder, string yy_from, string mm_from, string yy_to, string mm_to)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetDebtCalcs(finder, yy_from, mm_from, yy_to, mm_to);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetDebtCalcs\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка GetDebtCalcs\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }


        //Отчет: извещение за месяц
        public Returns GetNoticeCalcs(Ls finder, string yy, string mm)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetNoticeCalcs(finder, yy, mm);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetNoticeCalcs\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка GetNoticeCalcs\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        //Отчет: список заявок
        public Returns GetOrderList(int nzp_user)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetOrderList(nzp_user);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetOrderList\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка GetOrderList\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        //Отчет: список плановых работ
        public Returns GetPlannedWorksList(int nzp_user, enSrvOper en)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetPlannedWorksList(nzp_user, en);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetPlannedWorksList\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка GetPlannedWorksList\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        //Отчет: 
        public Returns GetCountOrders(Ls finder, string _nzp, string _nzp_add, string s_date, string po_date)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetCountOrders(finder, _nzp, _nzp_add, s_date, po_date);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetCountOrders\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка GetCountOrders\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        //Отчет: 
        public Returns GetSupgReports(SupgFinder supg_finder, enSrvOper en)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetSupgReports(supg_finder, en);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetSupgReports\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка GetSupgReports\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }


        //Отчет: список нарядов-заказов для выполнения
        public Returns GetIncomingJobOrders(int nzp_user)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetIncomingJobOrders(nzp_user);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetIncomingJobOrders\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка GetIncomingJobOrders\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        //Отчет: список нарядов-заказов для выполнения
        public Returns GetRepNedopList(int nzp_user, int nzp_jrn)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetRepNedopList(nzp_user, nzp_jrn);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetRepNedopList\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка GetRepNedopList\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        //Отчет: оплата гражданами-получателями коммунальных услуг за поставленные услуги
        public Returns GetDeliveredServicesPayment(Ls finder, int nzp_supp, string yy, string mm)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetDeliveredServicesPayment(finder, nzp_supp, yy, mm);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetDeliveredServicesPayment\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка GetDeliveredServicesPayment\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        //Отчет: получение начислений по дому
        public Returns GetDomCalcs(int Nzp_user, string mm, string yy)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetDomCalcs(Nzp_user, mm, yy);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetDomCalcs\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка GetDomCalcs\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        //Отчет: получение начислений по дому
        public Returns GetAnalisKart(List<Prm> listprm, int Nzp_user)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetAnalisKart(listprm, Nzp_user);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetAnalisKart\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка GetAnalisKart\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        //Отчет: калькуляция тарифа по содержанию жилья
        public Returns GetCalcTarif(Prm prm, int Nzp_user)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetCalcTarif(prm, Nzp_user);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetCalcTarif\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка GetCalcTarif\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        //Отчет: получение начислений по дому
        public Returns GetDomNach(List<Prm> listprm, int Nzp_user)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetDomNach(listprm, Nzp_user);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetDomNach\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка GetDomNach\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }


        //Отчет: Сальдовая оборотная ведомость
        public Returns GetSaldoRep10_14_3(Prm prm_, int Nzp_user)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetSaldoRep10_14_3(prm_, Nzp_user);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetSaldoRep10_14_3\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка GetSaldoRep10_14_3\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        //Отчет: Сальдовая оборотная ведомость
        public Returns GetSaldoRep10_14_1(Prm prm_, int Nzp_user)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetSaldoRep10_14_1(prm_, Nzp_user);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetSaldoRep10_14_1\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка GetSaldoRep10_14_1\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        //Отчет: Сверка поступлений за день
        public Returns GetSverkaDay(ExcelSverkaPeriod prm_, int Nzp_user)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetSverkaDay(prm_, Nzp_user);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetSverkaDay\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка GetSverkaDay\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        //Отчет: Сверка поступлений за месяц
        public Returns GetSverkaMonth(ExcelSverkaPeriod prm_, int Nzp_user)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetSverkaMonth(prm_, Nzp_user);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetSverkaMonth\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка GetSverkaMonth\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public Returns GetDataSaldoPoPerechisl(MoneyDistrib finder, int Nzp_user)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetDataSaldoPoPerechisl(finder, Nzp_user);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetDataSaldoPoPerechisl\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка GetDataSaldoPoPerechisl\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        //Отчет: Начисления по поставщику
        public Returns GetCharges(Prm prm_, int Nzp_user)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetCharges(prm_, Nzp_user);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetCharges\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка GetCharges\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        //Отчет: получение начислений по дому
        public Returns GetDomNachPere(List<Prm> listprm, int Nzp_user)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetDomNachPere(listprm, Nzp_user);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetDomNachPere\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка GetDomNachPere\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        //Отчет: справка по поставщикам
        public Returns GetSpravSuppNach(List<Prm> listprm, int Nzp_user)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetSpravSuppNach(listprm, Nzp_user);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetSpravSuppNach\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка GetSpravSuppNach\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        //Отчет: справка по поставщикам
        public Returns GetSpravSuppNachHar(Prm prm_, int Nzp_user)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetSpravSuppNachHar(prm_, Nzp_user);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetSpravSuppNachHar\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка GetSpravSuppNachHar\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        //Отчет: справка по поставщикам форма 3
        public Returns GetSpravSuppNachHar2(Prm prm_, int Nzp_user)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetSpravSuppNachHar2(prm_, Nzp_user);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetSpravSuppNachHar2\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка GetSpravSuppNachHar2\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        //Отчет: справка о наличии задолженности
        public Returns GetSpravHasDolg(Prm prm_, int Nzp_user)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetSpravHasDolg(prm_, Nzp_user);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetSpravHasDolg\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка GetSpravHasDolg\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        //Отчет: справка по лицевому счету
        public Returns GetLicSchetExcel(Ls finder, int year_, int month_)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetLicSchetExcel(finder, year_, month_);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetLicSchetExcel\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка GetLicSchetExcel\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        //Отчет: Акт сверки
        public Returns GetEnergoActSverki(Prm prm_, int nzp_supp, int Nzp_user)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetEnergoActSverki(prm_, nzp_supp, Nzp_user);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetEnergoActSverki\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка GetEnergoActSverki\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        //Отчет: справка о наличии задолженности
        public Returns GetSpravPULs(Prm prm_, int Nzp_user)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetSpravPULs(prm_, Nzp_user);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetSpravPULs\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка GetSpravPULs\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        //Отчет: Ведомость оплат по ЛС
        public Returns GetVedOplLs(Prm prm, int Nzp_user)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetVedOplLs(prm, Nzp_user);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetVedOplLs\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка GetVedOplLs\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        //Отчет: Ведомость перерасчетов
        public Returns GetVedPere(Prm prm, int Nzp_user)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetVedPere(prm, Nzp_user);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetVedPere\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка GetVedPere\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        //Отчет: Сведения о просроченной задолженности
        public Returns GetDolgSved(Prm prm, int Nzp_user)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetDolgSved(prm, Nzp_user);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetDolgSved\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка GetDolgSved\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        //Отчет: Список должников
        public Returns GetDolgSpis(Prm prm, int Nzp_user)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetDolgSpis(prm, Nzp_user);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetDolgSpis\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка GetDolgSpis\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        //Отчет: Список Рассогласований с паспортисткой
        public Returns GetPaspRas(Prm prm, int Nzp_user)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetPaspRas(prm, Nzp_user);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetPaspRas\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка GetPaspRas\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
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
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetPaspRasCommon(prm, Nzp_user);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetPaspRasCommon\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка GetPaspRasCommon\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        //Отчет: Справка о начислении платы по виду услуги содержание жилья
        public Returns GetSpravSoderg(Prm prm, int Nzp_user)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetSpravSoderg(prm, Nzp_user);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetSpravSoderg\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка GetSpravSoderg\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        //Отчет: Справка о начислении платы по виду услуги форма 2
        public Returns GetSpravSoderg2(Prm prm, int Nzp_user)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetSpravSoderg2(prm, Nzp_user);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetSpravSoderg2\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка GetSpravSoderg2\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        //Отчет: Справка по услугам группы содержание жилья
        public Returns GetSpravGroupSodergGil(Prm prm, int Nzp_user)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetSpravGroupSodergGil(prm, Nzp_user);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetSpravGroupSodergGil\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка GetSpravGroupSodergGil\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        //Отчет:Сводная ведомость по нормативам потребления
        public Returns GetVedNormPotr(Prm prm, int Nzp_user)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetVedNormPotr(prm, Nzp_user);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetVedNormPotr\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка GetVedNormPotr\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        //Отчет: состояние жилого фонда
        public Returns GetSostGilFond(Prm prm, int Nzp_user)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetSostGilFond(prm, Nzp_user);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetSostGilFond\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка GetSostGilFond\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        //Отчет:Формирование квитанций на оплату
        public Returns GetFakturaFiles(Prm prm, int Nzp_user)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetFakturaFiles(prm, Nzp_user);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetFakturaFiles\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка GetFakturaFiles\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }


        //Отчет: Справка по отключениям подачи коммунальных услуг
        public Returns GetSpravPoOtklUslug(Ls finder, int nzp_serv, int month, int year)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetSpravPoOtklUslug(finder, nzp_serv, month, year);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetSpravPoOtklUslug\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка GetSpravPoOtklUslug\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }



        //Отчет: Справка по отключениям подачи коммунальных услуг по домам с указанием виновника
        public Returns GetSpravPoOtklUslugDomVinovnik(Prm prm)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetSpravPoOtklUslugDomVinovnik(prm);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetSpravPoOtklUslugDomVinovnik\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка GetSpravPoOtklUslugDomVinovnik\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
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
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetSaldoVedEnergo(prm);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetSaldoVedEnergo\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка GetSaldoVedEnergo\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
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
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetServSuppNach(prm);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetServSuppNach\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка GetServSuppNach\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
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
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetServSuppMoney(prm);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetServSuppMoney\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка GetServSuppMoney\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
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
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetListDomFaktura(prm);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetListDomFaktura\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка GetListDomFaktura\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
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
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetVremZareg(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetVremZareg\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка GetVremZareg\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
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
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetVoenkomat(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetVoenkomat\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка GetVoenkomat\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
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
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetDolgSpisEnergo(prm);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetDolgSpisEnergo\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка GetDolgSpisEnergo\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
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
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetProtocolSverData(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetProtocolSverData\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка GetProtocolSverData\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
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
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetProtocolSverDataLsDom(p);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetProtocolSverDataLsDom\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка GetProtocolSverDataLsDom\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        //Справка по поставщикам коммунальных услуг ф.3
        public Returns GetSpravSuppSamara(Prm prm)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetSpravSuppSamara(prm);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetSpravSuppSamara\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка GetSpravSuppSamara\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        //Отчет: Справка по отключениям подачи коммунальных услуг по домам 
        public Returns GetSpravPoOtklUslugDom(Prm prm)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetSpravPoOtklUslugDom(prm);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetSpravPoOtklUslugDom\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка GetSpravPoOtklUslugDom\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }


        //Отчет: Справка по отключениям подачи коммунальных услуг по ЖЭУ с указанием виновника
        public Returns GetSpravPoOtklUslugGeuVinovnik(Prm prm)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetSpravPoOtklUslugGeuVinovnik(prm);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetSpravPoOtklUslugGeuVinovnik\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка GetSpravPoOtklUslugGeuVinovnik\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
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
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetInfPoRaschetNasel(finder, nzp_supp, month, year);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetInfPoRaschetNasel\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка GetInfPoRaschetNasel\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
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
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetReportPrmNach(finder, par, month, year, comment, services, isShowExpanded);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetReportPrmNach\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка GetReportPrmNach\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public Returns GetControlDistribPayments(Payments pay)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetControlDistribPayments(pay);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetControlDistribPayments\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка GetControlDistribPayments\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        //Отчет: Протокол рассчитанных значений ОДН
        public Returns GetProtCalcOdn(Prm prm, int Nzp_user)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetProtCalcOdn(prm, Nzp_user);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetProtCalcOdn\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка GetProtCalcOdn\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        //Отчет: Протокол рассчитанных значений ОДН расширенный
        public Returns GetProtCalcOdn2(Prm prm, int Nzp_user)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetProtCalcOdn2(prm, Nzp_user);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetProtCalcOdn2\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка GetProtCalcOdn2\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        // Выгрузка сальдо в банк
        public Returns GetSaldo_v_bank(SupgFinder finder, string year, string month, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetSaldo_v_bank(finder, year, month, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetSaldo_v_bank\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка GetSaldo_v_bank\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
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
                using (var ro = getRemoteObject())
                {
                    ret = ro.UploadKLADRAddrSpace(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка UploadKLADRAddrSpace\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка UploadKLADRAddrSpace\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
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
                using (var ro = getRemoteObject())
                {
                    ret = ro.GenerateExchange(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GenerateExchange\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка GenerateExchange\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
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
                using (var ro = getRemoteObject())
                {
                    ret = ro.GenerateUESVigr(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GenerateUESVigr\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка GenerateUESVigr\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
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
                using (var ro = getRemoteObject())
                {
                    ret = ro.GenerateMURCVigr(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GenerateMURCVigr\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка GenerateMURCVigr\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
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
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetUploadCharge(finder, year, month, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetUploadCharge\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка GetUploadCharge\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
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
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetUploadPU(finder, year, month, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetUploadPU\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка GetUploadPU\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }




        // Выгрузка реестра для загрузки в БС
        public Returns GetUploadReestr(Finder finder, List<int> BanksList, string unloadVersionFormat, string statusLS, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetUploadReestr(finder, BanksList, unloadVersionFormat, statusLS, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetUploadReestr\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка GetUploadReestr\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
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
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetUploadKassa(finder, year, month, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetUploadKassa\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка GetUploadKassa\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        /// <summary>
        /// Выгрузка по принятым для перечисления денежным средствам
        /// </summary>
        /// <param name="finder">Параметры выгрузки</param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public Returns GetChargeUnload(ChargeUnloadPrm finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetChargeUnload(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetChargeUnload\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка GetChargeUnload\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        /// <summary>
        /// Получить ключи адреса (nzp_town, nzp_raj, nzp_ul, nzp_dom) по ключу nzp_kvar
        /// </summary>
        public Returns GetAddressID(int nzp_kvar, out Ls ls)
        {
            Returns ret = new Returns(true);
            ls = new Ls();

            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetAddressID(nzp_kvar, out ls);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetAddressID\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка GetAddressID\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        /// <summary>
        /// Расчет задолженности по оплате за ЖКУ по адресу
        /// </summary>
        /// <param name="finder">Параметры отчета</param>
        /// <returns></returns>
        public Returns GetCalcAddressDeptReport(Dept finder)
        {
            Returns ret = new Returns(true);
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetCalcAddressDeptReport(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetCalcAddressDeptReport\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка GetCalcAddressDeptReport\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        // Отчет сальдовая ведомость 5.10
        public Returns GetSaldo_5_10(ChargeFind finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetSaldo_5_10(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetSaldo_5_10\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка GetSaldo_5_10\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        // Выгрузка для обмена с соц.защитой (Тула)
        public Returns GetExchangeSZ(Finder finder, string year, string month, int nzp_ex_sz, bool isPkodInLs)
        //----------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetExchangeSZ(finder, year, month, nzp_ex_sz, isPkodInLs);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetExchangeSZ\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка GetExchangeSZ\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public Returns GetUploadExchangeSZ(Finder finder, string fileName, string fileNameFull, string encodingValue, List<int> listWP)
        {
            var ret = new Returns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetUploadExchangeSZ(finder, fileName, fileNameFull, encodingValue, listWP);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetUploadExchangeSZ\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка GetUploadExchangeSZ\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }


        public Returns UploadReestrInFon(FilesImported finder)
        {
            var ret = new Returns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.UploadReestrInFon(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка UploadReestrInFon\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса распределения оплат";
                MonitorLog.WriteLog("Ошибка ReadReestr\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }


        public Returns GetAllAgrementsReport(DateTime? dat_s, DateTime? dat_po, int user, int area)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetAllAgrementsReport(dat_s, dat_po, user, area);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetAllAgrementsReport\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка GetAllAgrementsReport\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public Returns LoadOneTime(FilesImported finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.LoadOneTime(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка LoadOneTime\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка LoadOneTime\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
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
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetProtocolVTB24(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetProtocolVTB24\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка GetProtocolVTB24\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
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
        public string GetReminderToDebitor(Deal finder, ReportType type, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            string str = string.Empty;
            try
            {
                using (var ro = getRemoteObject())
                {
                    str = ro.GetReminderToDebitor(finder, type, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetReminderToDebitor\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка выгрузки данных";
                MonitorLog.WriteLog("Ошибка GetReminderToDebitor\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return str;
        }

        //уведомление
        public string GetNoticeToDebitor(Deal finder, ReportType type, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            string str = string.Empty;
            try
            {
                using (var ro = getRemoteObject())
                {
                    str = ro.GetNoticeToDebitor(finder, type, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetNoticeToDebitor\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка выгрузки данных";
                MonitorLog.WriteLog("Ошибка GetNoticeToDebitor\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return str;
        }

        public string GetWarningToDebitor(Deal finder, ReportType type, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            string str = string.Empty;
            try
            {
                using (var ro = getRemoteObject())
                {
                    str = ro.GetWarningToDebitor(finder, type, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetWarningToDebitor\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка выгрузки данных";
                MonitorLog.WriteLog("Ошибка GetWarningToDebitor\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return str;
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
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetServSuppMoney2(prm);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetServSuppMoney2\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка GetServSuppMoney2\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
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
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetSpravDolgTula(prm);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetSpravDolgTula\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка GetSpravDolgTula\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
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
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetListDolgTula(prm);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetListDolgTula\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка GetListDolgTula\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
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
                using (var ro = getRemoteObject())
                {
                    ret = ro.GetSpravSuppTula(prm);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetSpravSuppTula\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка GetSpravSuppTula\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }
        #endregion

        #region Передача файла по WCF
        public byte[] GetFile(string path)
        {
            byte[] b = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    b = ro.GetFile(path);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                MonitorLog.WriteLog("Ошибка CreateExcelReport_host\n" + ex.Message + "\naccess_error: " + Constants.access_error, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка CreateExcelReport_host\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return b;
        }
        #endregion

        #region универсальный сервер отчетов
        public List<Dict> GetReportDicts(List<int> idDicts, bool loadDictsData, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Dict> res = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    res = ro.GetReportDicts(idDicts, loadDictsData, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetReportDicts\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка GetReportDicts\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return res;
        }
        #endregion

        #region Смена УК для списка ЛС
        public Returns ChangeArea(FinderChangeArea finder)
        {
            Returns ret;
            ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.ChangeArea(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка ChangeArea\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка ChangeArea\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }
        #endregion

        #region Обмен со сторонними поставщиками
        // Выгрузка ЛС и адресов
        public Returns FileSyncLS(ExFinder finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.FileSyncLS(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка FileSyncLS\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка FileSyncLS\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        // Выгрузка изменений параметров ЛС
        public Returns FileChangeLS(ExFinder finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.FileChangeLS(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка FileChangeLS\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания Excel отчетов";
                MonitorLog.WriteLog("Ошибка FileChangeLS\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }
        #endregion

        public Returns StartTransfer(TransferParams finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.StartTransfer(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка StartTransfer\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка StartTransfer\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }
        #region Загрузка оплат от ВТБ24
        public Returns UploadVTB24(FilesImported finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.UploadVTB24(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка UploadVTB24\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка UploadVTB24\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }
        #endregion
    }
}
