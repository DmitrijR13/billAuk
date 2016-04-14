using System;
using System.Data;
using System.Collections.Generic;
using STCLINE.KP50.Global;
//using STCLINE.KP50.DataBase;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.Client
{
    public class cli_FnReval : I_FnReval  //реализация клиента сервиса Кассы
    {
        I_FnReval remoteObject;

        public cli_FnReval(int nzp_server)
            : base()
        {
            _cli_FnReval(nzp_server);
        }

        void _cli_FnReval(int nzp_server)
        {
            if (Points.IsMultiHost && nzp_server > 0)
            {
                //определить параметры доступа
                _RServer zap = MultiHost.GetServer(nzp_server);
                //remoteObject = HostChannel.CreateInstance<I_FnReval>(zap.login, zap.pwd, zap.ip_adr + WCFParams.srvFnReval);
                remoteObject = HostChannel.CreateInstance<I_FnReval>(zap.login, zap.pwd, zap.ip_adr + WCFParams.AdresWcfWeb.srvFnReval);
            }
            else
            {
                //по-умолчанию
                //remoteObject = HostChannel.CreateInstance<I_FnReval>(WCFParams.Adres + WCFParams.srvFnReval);
                remoteObject = HostChannel.CreateInstance<I_FnReval>(WCFParams.AdresWcfWeb.Adres + WCFParams.AdresWcfWeb.srvFnReval);
            }
        }

        ~cli_FnReval()
        {
            try { if (remoteObject != null) HostChannel.CloseProxy(remoteObject); }
            catch { }
        }

        public Returns OperateWithFnReval(FnReval finder, FnReval.Operations oper)
        {
            Returns ret = Utils.InitReturns();
            
            try
            {
                ret = remoteObject.OperateWithFnReval(finder, oper);
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
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка OperateWithFnReval(" + oper.ToString() + ")" + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }

        public Returns OperateWithFnRevalSupp(FnReval finder, FnReval.Operations oper)
        {
            Returns ret = Utils.InitReturns();

            try
            {
                ret = remoteObject.OperateWithFnRevalSupp(finder, oper);
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
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка OperateWithFnRevalSupp(" + oper.ToString() + ")" + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }

        public List<FnReval> LoadFnReval(FnRevalFinder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                List<FnReval> list = remoteObject.LoadFnReval(finder, out ret);
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
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка LoadFnReval" + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public List<FnReval> LoadFnRevalSupp(FnRevalFinder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                List<FnReval> list = remoteObject.LoadFnRevalSupp(finder, out ret);
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
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка LoadFnRevalSupp" + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }
    }
}
