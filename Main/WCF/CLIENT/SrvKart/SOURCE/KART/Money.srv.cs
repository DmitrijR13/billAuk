using System;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Net;
using System.Security.Principal;
using System.Collections.Generic;
//using IBM.Data.Informix;

using STCLINE.KP50.DataBase;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Global;
using STCLINE.KP50.Server;
using STCLINE.KP50.Client;

namespace STCLINE.KP50.Server
{
    //----------------------------------------------------------------------
    public class srv_Money : srv_Base, I_Money //сервис Оплат
    //----------------------------------------------------------------------
    {
        List<Money> moneySpis = new List<Money>();

        //----------------------------------------------------------------------
        public string CheckCalcMoney(int cur_yy, int cur_mm)
        //----------------------------------------------------------------------
        {
            string s;
            using (var dbc = new DbCalcPack())
            {
                s = dbc.CheckCalcMoney(cur_yy, cur_mm);
            }

            return s;
        }

        public void CalcDistrib(TransferBalanceFinder finder, out Returns ret)
        {
            ret = Utils.InitReturns();

            DbCalcPack dbc = new DbCalcPack();

            try
            {
                TransferBalanceFinder distribFinder = new TransferBalanceFinder()
                {
                    dat_s = finder.dat_s,
                    dat_po = finder.dat_po,
                    nzp_wp = finder.nzp_wp
                };

                dbc.DistribPaXX_1(distribFinder, out ret, null);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка CalcDistrib\n " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            finally
            {
                dbc.Close();
            }
        }

        /// <summary>
        /// Учесть к перечислению
        /// </summary>
        /// <param name="dat_s">дата начала </param>
        /// <param name="dat_po">дата окончания</param>
        /// <param name="ret">результат</param>
        public void CalcDistribFon(TransferBalanceFinder finder, out Returns ret)
        {          
            ret = Utils.InitReturns();

            DbCalcPack dbc = new DbCalcPack();

            try
            {
                dbc.CalcDistribFon(finder, out ret);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка CalcDistribFon\n " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            finally
            {
            dbc.Close();
        }
        }

        //----------------------------------------------------------------------
        public List<Money> CallSrvMoney(Money finder, out Returns ret, bool find)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            moneySpis.Clear();

            using (DbMoneys db = new DbMoneys())
            {
                if (find)
                    db.FindMoney(finder, out ret);
                if (ret.result)
                    moneySpis = db.GetMoney(finder, out ret);
                else
                    moneySpis = null;
            }

            return moneySpis;
        }
        //----------------------------------------------------------------------
        public List<Money> GetMoney(Money finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Money cli = new cli_Money(WCFParams.AdresWcfHost.CurT_Server);
                moneySpis = cli.GetMoney(finder, out ret);
            }
            else
            {
                moneySpis = CallSrvMoney(finder, out ret, false);
            }
            return moneySpis;
        }
        //----------------------------------------------------------------------
        public List<Money> FindMoney(Money finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Money cli = new cli_Money(WCFParams.AdresWcfHost.CurT_Server);
                moneySpis = cli.FindMoney(finder, out ret);
            }
            else
            {
                moneySpis = CallSrvMoney(finder, out ret, true);
            }
            return moneySpis;

            //return CallSrvMoney(finder, out ret, true);
        }
        //----------------------------------------------------------------------
        public Money LoadMoney(Money finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            Money res = new Money();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Money cli = new cli_Money(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.LoadMoney(finder, out ret);
            }
            else
            {
                using (var db = new DbMoneys())
                {
                    res = db.LoadMoney(finder, out ret);
                }
            }
            return res;
        }
        //----------------------------------------------------------------------
        public List<Money> GetMoneyUchet(Money finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<Money> res = new List<Money>();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Money cli = new cli_Money(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.GetMoneyUchet(finder, out ret);
            }
            else
            {
                using (var db = new DbMoneys())
                {
                    res = db.GetMoneyUchet(finder, out ret);
                }
            }
            return res;
        }
        public List<AccountPayment> GetAccountPayment(AccountPayment finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<AccountPayment> res = new List<AccountPayment>();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Money cli = new cli_Money(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.GetAccountPayment(finder, out ret);
            }
            else
            {
                using (var db = new DbMoneys())
                {
                    res = db.GetAccountPayment(finder, out ret);
                }
            }
            return res;
        }

        public Returns DeletePayments(int nzpUser, List<int> files)
        //----------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                var cli = new cli_Money(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.DeletePayments(nzpUser, files);
            }
            else
            {
                try
                {
                    using (var db = new DbMoneys())
                    {
                        ret = db.DeletePayments(nzpUser, files);
                    }
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка удаление файлов";
                    string err = string.Empty;
                    if (Constants.Viewerror) err = "\n" + ex.Message;
                    MonitorLog.WriteLog("Ошибка DeletePayments(). " + err, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return ret;
        }

        public Returns DeleteAllPayments(int nzpUser, int id)
        //----------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                var cli = new cli_Money(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.DeleteAllPayments(nzpUser, id);
            }
            else
            {
                try
                {
                    using (var db = new DbMoneys())
                    {
                        ret = db.DeleteAllPayments(nzpUser, id);
                    }
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка удаление данных";
                    string err = string.Empty;
                    if (Constants.Viewerror) err = "\n" + ex.Message;
                    MonitorLog.WriteLog("Ошибка DeleteAllPayments(). " + err, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return ret;
        }
    }
}
