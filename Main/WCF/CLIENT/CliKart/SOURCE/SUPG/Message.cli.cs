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
    public class cli_SmsMessage : I_SmsMessage
    {
        I_SmsMessage remoteObject;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nzp_server">Код удаленного сервера</param>
        /// <param name="nzp_role">Код подсистемы</param>
        public cli_SmsMessage(int nzp_server)
            : base()
        {
            _cli_SmsMessage(nzp_server);
        }

        void _cli_SmsMessage(int nzp_server)
        {
            if (Points.IsMultiHost && nzp_server > 0)
            {
                //определить параметры доступа
                _RServer zap = MultiHost.GetServer(nzp_server);
                remoteObject = HostChannel.CreateInstance<I_SmsMessage>(zap.login, zap.pwd, zap.ip_adr + WCFParams.AdresWcfWeb.srvSmsMessage);
            }
            else
            {
                //по-умолчанию
                remoteObject = HostChannel.CreateInstance<I_SmsMessage>(WCFParams.AdresWcfWeb.Adres + WCFParams.AdresWcfWeb.srvSmsMessage);
            }
        }

        ~cli_SmsMessage()
        {
            try { if (remoteObject != null) HostChannel.CloseProxy(remoteObject); }
            catch { }
        }


        /// <summary>
        /// Получить список смс
        /// </summary>
        public List<SmsMessage> GetSmsMessage(SmsMessage finder, enSrvOper oper, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<SmsMessage> smsList = null;
            try
            {
                smsList = remoteObject.GetSmsMessage(finder, oper, out ret);
                HostChannel.CloseProxy(remoteObject);
                return smsList;
            }
            catch (Exception ex)
            {
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = "Ошибка получения смс-сообщений";

                MonitorLog.WriteLog("Ошибка GetSmsMessage\n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
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
                list = remoteObject.GetSmsMessageStatus(out ret);
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
                else ret.text = "Ошибка получения cостояний смс-сообщений";

                MonitorLog.WriteLog("Ошибка GetSmsMessageStatus\n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
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
                list = remoteObject.GetSmsReceiver(out ret);
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
                else ret.text = "Ошибка получения получателей смс-сообщений";

                MonitorLog.WriteLog("Ошибка GetSmsReceiver\n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        /// <summary>
        /// Отправить сообщение
        /// </summary>
        public Returns SendSmsMessage(SmsMessage finder, List<SmsReceiver> list)
        {
            Returns ret = Utils.InitReturns();
            
            try
            {
                ret = remoteObject.SendSmsMessage(finder, list);
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
                else ret.text = "Ошибка отправки смс-сообщения";
                
                MonitorLog.WriteLog("Ошибка SendSmsMessage\n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }

        /// <summary>
        /// Cохранить получателя
        /// </summary>
        public Returns SaveSmsReceiver(SmsReceiver finder)
        {
            Returns ret = Utils.InitReturns();

            try
            {
                ret = remoteObject.SaveSmsReceiver(finder);
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
                else ret.text = "Ошибка сохранения получателя";

                MonitorLog.WriteLog("Ошибка SaveSmsReceiver\n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }

        /// <summary>
        /// Удалить получателя
        /// </summary>
        public Returns DeleteSmsReceiver(SmsReceiver finder)
        {
            Returns ret = Utils.InitReturns();

            try
            {
                ret = remoteObject.DeleteSmsReceiver(finder);
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
                else ret.text = "Ошибка удаления получателя";

                MonitorLog.WriteLog("Ошибка DeleteSmsReceiver\n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }

        /// <summary>
        /// Переотправить сообщения
        /// </summary>
        public Returns ResendSmsMessage(SmsMessage finder, List<SmsMessage> list)
        {
            Returns ret = Utils.InitReturns();

            try
            {
                ret = remoteObject.ResendSmsMessage(finder, list);
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
                else ret.text = "Ошибка повторной отправки сообщений";

                MonitorLog.WriteLog("Ошибка ResendSmsMessage\n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }

        /// <summary>
        /// Удалить сообщения
        /// </summary>
        public Returns DeleteSmsMessage(List<SmsMessage> list)
        {
            Returns ret = Utils.InitReturns();

            try
            {
                ret = remoteObject.DeleteSmsMessage(list);
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
                else ret.text = "Ошибка удаления сообщений";

                MonitorLog.WriteLog("Ошибка DeleteSmsMessage\n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }
    }
}
