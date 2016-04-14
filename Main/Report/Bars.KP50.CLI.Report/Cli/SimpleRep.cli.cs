using System;
using System.Data;
using System.ServiceModel;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Global;


namespace STCLINE.KP50.Client
{
    public class cli_SimpleRep : cli_Base, I_SimpleRep
    {
        //I_SimpleRep remoteObject;

        ISimpleRepRemoteObject getRemoteObject()
        {
            return getRemoteObject<ISimpleRepRemoteObject>(WCFParams.AdresWcfWeb.srvSimpleRep);
        }

        public cli_SimpleRep(int nzp_server)
            : base()
        {
            //remoteObject = HostChannel.CreateInstance<I_SimpleRep>(WCFParams.AdresWcfWeb.Adres + WCFParams.AdresWcfWeb.srvSimpleRep); 
        }

        //void _cli_SimpleRep(int nzp_server)
        //{
        //    string addrHost = "";
        //    //определить параметры доступа
        //    _RServer zap = MultiHost.GetServer(nzp_server);

        //    if (Points.IsMultiHost && nzp_server > 0)
        //    {
        //        addrHost = zap.ip_adr + WCFParams.AdresWcfWeb.srvSimpleRep;
        //        remoteObject = HostChannel.CreateInstance<I_SimpleRep>(zap.login, zap.pwd, addrHost);
        //    }
        //    else
        //    {
        //        //по-умолчанию
        //        addrHost = WCFParams.AdresWcfWeb.Adres + WCFParams.AdresWcfWeb.srvSimpleRep;
        //        zap.rcentr = "<локально>";
        //        remoteObject = HostChannel.CreateInstance<I_SimpleRep>(addrHost);
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

        //~cli_SimpleRep()
        //{
        //    try { if (remoteObject != null) HostChannel.CloseProxy(remoteObject); }
        //    catch { }
        //}

        /// <summary> 
        /// Статусы дел 
        /// </summary>
        public enum EnumDealStatuses
        {
            /// <summary>
            /// Тип не задан
            /// </summary>
            None = 0,

            /// <summary>
            /// "Закрыто"
            /// </summary>
            Close = 1,

            /// <summary>
            /// Списан долг
            /// </summary>
            Debt = 2,

            /// <summary>
            /// "Зарегистрировано"
            /// </summary>
            Registered = 3,

            /// <summary>
            /// "Напоминание выдано"
            /// </summary>
            Reminder = 4,

            /// <summary>
            /// "Уведомление выдано"
            /// </summary>
            Notice = 5,

            /// <summary>
            /// "Предупреждение выдано"
            /// </summary>
            Warning = 6,

            /// <summary>
            /// ""Соглашение подписано""
            /// </summary>
            AgreementSigned = 7,

            /// <summary>
            /// "Соглашение нарушено"
            /// </summary>
            AgreementViolated = 8,

            /// <summary>
            /// "Судебный приказ сформирован"
            /// </summary>
            OrderFormed = 9,

            /// <summary>
            /// "Иск подан в суд"
            /// </summary>
            LawsuitSubmitted = 10

        }

        /// <summary>
        /// Отчет:Выписка из лицевого счета по счетчикам
        /// </summary>
        /// <param name="prm"></param>
        /// <returns></returns>
        public DataTable GetCountersSprav(ReportPrm prm)
        {
            Returns ret = Utils.InitReturns();
            DataTable DT = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    DT = ro.GetCountersSprav(prm);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetCountersSprav\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка вызова сервиса создания простых отчетов";
                MonitorLog.WriteLog("Ошибка GetCountersSprav\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return DT;
        }

        /// <summary>
        /// Возвращает набор данных для неотложного отчета
        /// </summary>
        /// <param name="prm"></param>
        /// <returns></returns>
        public DataTable GetReportTable(ReportPrm prm)
        {
            DataTable dt = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    ro.GetReportTable(prm);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                MonitorLog.WriteLog("Ошибка GetReportTable\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка GetReportTable\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return dt;
        }
    }
}
