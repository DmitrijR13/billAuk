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
    public class cli_Money : I_Money
    {
        I_Money remoteObject;

        public cli_Money(int nzp_server)
            : base()
        {
            _cli_Money(nzp_server);
        }

        void _cli_Money(int nzp_server)
        {
            string addrHost = "";
            //определить параметры доступа
            _RServer zap = MultiHost.GetServer(nzp_server);

            if (Points.IsMultiHost && nzp_server > 0)
            {
                addrHost = zap.ip_adr + WCFParams.AdresWcfWeb.srvMoney;
                remoteObject = HostChannel.CreateInstance<I_Money>(zap.login, zap.pwd, addrHost);
            }
            else
            {
                //по-умолчанию
                addrHost = WCFParams.AdresWcfWeb.Adres + WCFParams.AdresWcfWeb.srvMoney;
                zap.rcentr = "<локально>";
                remoteObject = HostChannel.CreateInstance<I_Money>(addrHost);
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

        ~cli_Money()
        {
            try { if (remoteObject != null) HostChannel.CloseProxy(remoteObject); }
            catch { }
        }

        List<Money> moneySpis = new List<Money>();

        //----------------------------------------------------------------------
        public List<Money> CallSrvMoney(Money finder, out Returns ret, byte tip)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                switch (tip)
                {
                    case 0:
                        {
                            moneySpis = remoteObject.FindMoney(finder, out ret);
                            break;
                        }
                    case 1:
                        {
                            moneySpis = remoteObject.GetMoney(finder, out ret);
                            break;
                        }
                }

                HostChannel.CloseProxy(remoteObject);
                return moneySpis;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка CallSrvMoney(" + tip.ToString() + ") " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }
        //----------------------------------------------------------------------
        public List<Money> GetMoney(Money finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            return CallSrvMoney(finder, out ret, 1);
        }
        //----------------------------------------------------------------------
        public List<Money> FindMoney(Money finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            return CallSrvMoney(finder, out ret, 0);
        }

        //----------------------------------------------------------------------
        public Money LoadMoney(Money finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                Money mn = remoteObject.LoadMoney(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return mn;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка LoadMoney " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        //----------------------------------------------------------------------
        public List<Money> GetMoneyUchet(Money finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                List<Money> mn = remoteObject.GetMoneyUchet(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return mn;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetMoneyUchet " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        //----------------------------------------------------------------------
        public void CalcDistrib(TransferBalanceFinder finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                remoteObject.CalcDistrib(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка CalcDistrib " + err, MonitorLog.typelog.Error, 2, 100, true);
                return;
            }
        }
        /// <summary>
        /// Учесть к перечислению
        /// </summary>
        /// <param name="dat_s">дата начала</param>
        /// <param name="dat_po">дата окончания</param>
        /// <param name="ret">результат</param>
        public void CalcDistribFon(TransferBalanceFinder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                remoteObject.CalcDistribFon(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка CalcDistribFon " + err, MonitorLog.typelog.Error, 2, 100, true);
                return;
            }
        }

        //----------------------------------------------------------------------
        public string CheckCalcMoney(int cur_yy, int cur_mm)
        //----------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();
            string s = "";
            try
            {
                s = remoteObject.CheckCalcMoney(cur_yy, cur_mm);
                HostChannel.CloseProxy(remoteObject);
                return s;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка CheckCalcMoney " + err, MonitorLog.typelog.Error, 2, 100, true);
                return "Ошибка выполнения";
            }
        }

        //----------------------------------------------------------------------
        public List<AccountPayment> GetAccountPayment(AccountPayment finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                List<AccountPayment> ap = remoteObject.GetAccountPayment(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return ap;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetAccountPayment " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public Returns DeletePayments(int nzpUser, List<int> files)
        //----------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.DeletePayments(nzpUser, files);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка DeletePayments " + err, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public Returns DeleteAllPayments(int nzpUser, int id)
        //----------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.DeleteAllPayments(nzpUser, id);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка DeleteAllPayments " + err, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;

        }
    }
}
