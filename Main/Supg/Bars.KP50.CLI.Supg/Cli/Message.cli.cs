using System;
using System.Collections.Generic;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Global;
using System.ServiceModel;

namespace STCLINE.KP50.Client
{
    public class cli_SmsMessage : cli_Base, I_SmsMessage
    {
        //I_SmsMessage remoteObject;

        ISmsMessageRemoteObject getRemoteObject()
        {
            return getRemoteObject<ISmsMessageRemoteObject>(WCFParams.AdresWcfWeb.srvSmsMessage);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nzp_server">Код удаленного сервера</param>
        /// <param name="nzp_role">Код подсистемы</param>
        public cli_SmsMessage(int nzp_server)
            : base()
        {
            //_cli_SmsMessage(nzp_server);
        }

        //void _cli_SmsMessage(int nzp_server)
        //{
        //    if (Points.IsMultiHost && nzp_server > 0)
        //    {
        //        //определить параметры доступа
        //        _RServer zap = MultiHost.GetServer(nzp_server);
        //        remoteObject = HostChannel.CreateInstance<I_SmsMessage>(zap.login, zap.pwd, zap.ip_adr + WCFParams.AdresWcfWeb.srvSmsMessage);
        //    }
        //    else
        //    {
        //        //по-умолчанию
        //        remoteObject = HostChannel.CreateInstance<I_SmsMessage>(WCFParams.AdresWcfWeb.Adres + WCFParams.AdresWcfWeb.srvSmsMessage);
        //    }
        //}

        //~cli_SmsMessage()
        //{
        //    try { if (remoteObject != null) HostChannel.CloseProxy(remoteObject); }
        //    catch { }
        //}

        /// <summary>
        /// Получить список смс
        /// </summary>
        public List<SmsMessage> GetSmsMessage(SmsMessage finder, enSrvOper oper, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<SmsMessage> smsList = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    smsList = ro.GetSmsMessage(finder, oper, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetSmsMessage\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка получения смс-сообщений";
                MonitorLog.WriteLog("Ошибка GetSmsMessage\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return smsList;
        }

        /// <summary>
        /// Получить справочник состояний смс
        /// </summary>
        public List<SmsMessageStatus> GetSmsMessageStatus(out Returns ret)
        {
            ret = Utils.InitReturns();
            List<SmsMessageStatus> list = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    list = ro.GetSmsMessageStatus(out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetSmsMessageStatus\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка получения cостояний смс-сообщений";
                MonitorLog.WriteLog("Ошибка GetSmsMessageStatus\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return list;
        }

        /// <summary>
        /// Получить справочник получателей сообщений
        /// </summary>
        public List<SmsReceiver> GetSmsReceiver(out Returns ret)
        {
            ret = Utils.InitReturns();
            List<SmsReceiver> list = null;
            try
            {
                using (var ro = getRemoteObject())
                {
                    list = ro.GetSmsReceiver(out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка GetSmsReceiver\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка получения получателей смс-сообщений";
                MonitorLog.WriteLog("Ошибка GetSmsReceiver\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return list;
        }

        /// <summary>
        /// Отправить сообщение
        /// </summary>
        public Returns SendSmsMessage(SmsMessage finder, List<SmsReceiver> list)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.SendSmsMessage(finder, list);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка SendSmsMessage\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка отправки смс-сообщения";
                MonitorLog.WriteLog("Ошибка SendSmsMessage\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        /// <summary>
        /// Cохранить получателя
        /// </summary>
        public Returns SaveSmsReceiver(SmsReceiver finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.SaveSmsReceiver(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка SaveSmsReceiver\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка сохранения получателя";
                MonitorLog.WriteLog("Ошибка SaveSmsReceiver\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        /// <summary>
        /// Удалить получателя
        /// </summary>
        public Returns DeleteSmsReceiver(SmsReceiver finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.DeleteSmsReceiver(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка DeleteSmsReceiver\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка удаления получателя";
                MonitorLog.WriteLog("Ошибка DeleteSmsReceiver\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        /// <summary>
        /// Переотправить сообщения
        /// </summary>
        public Returns ResendSmsMessage(SmsMessage finder, List<SmsMessage> list)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.ResendSmsMessage(finder, list);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка ResendSmsMessage\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка повторной отправки сообщений";
                MonitorLog.WriteLog("Ошибка ResendSmsMessage\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        /// <summary>
        /// Удалить сообщения
        /// </summary>
        public Returns DeleteSmsMessage(List<SmsMessage> list)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var ro = getRemoteObject())
                {
                    ret = ro.DeleteSmsMessage(list);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret.result = false;
                ret.text = Constants.access_error;
                ret.tag = Constants.access_code;
                MonitorLog.WriteLog("Ошибка DeleteSmsMessage\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка удаления сообщений";
                MonitorLog.WriteLog("Ошибка DeleteSmsMessage\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }
    }
}
