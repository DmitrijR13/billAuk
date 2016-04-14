using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using System.Collections;
using System.Data;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.REPORT;
using STCLINE.KP50.Client;

namespace STCLINE.KP50.Server
{
    public class srv_SmsMessage : srv_Base, I_SmsMessage
    {
        //----------------------------------------------------------------------
        public List<SmsMessage> GetSmsMessage(SmsMessage finder, enSrvOper oper, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<SmsMessage> list = null;

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_SmsMessage cli = new cli_SmsMessage(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.GetSmsMessage(finder, oper, out ret);
            }
            else
            {
                DbSmsMessage db = new DbSmsMessage();
                try
                {
                    switch (oper)
                    {
                        case enSrvOper.SrvFind:
                            db.FindSmsMessage(finder, out ret);
                            if (ret.result) list = db.GetSmsMessage(finder, out ret);
                            else list = null;
                            break;
                        case enSrvOper.SrvGet:
                            list = db.GetSmsMessage(finder, out ret);
                            break;
                        default:
                            ret = new Returns(false, "Неверное наименование операции");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetMessage() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return list;
        }

        //----------------------------------------------------------------------
        public List<SmsMessageStatus> GetSmsMessageStatus(out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<SmsMessageStatus> list = null;

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_SmsMessage cli = new cli_SmsMessage(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.GetSmsMessageStatus(out ret);
            }
            else
            {
                DbSmsMessage db = new DbSmsMessage();
                try
                {
                    list = db.GetSmsMessageStatus(out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка SmsMessageStatus() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return list;
        }

        //----------------------------------------------------------------------
        public List<SmsReceiver> GetSmsReceiver(out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<SmsReceiver> list = null;

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_SmsMessage cli = new cli_SmsMessage(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.GetSmsReceiver(out ret);
            }
            else
            {
                DbSmsMessage db = new DbSmsMessage();
                try
                {
                    list = db.GetSmsReceiver(out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetSmsReceiver() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return list;
        }

        //----------------------------------------------------------------------
        public Returns SendSmsMessage(SmsMessage finder, List<SmsReceiver> list)
        //----------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();
            
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_SmsMessage cli = new cli_SmsMessage(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.SendSmsMessage(finder, list);
            }
            else
            {
                DbSmsMessage db = new DbSmsMessage();
                try
                {
                    ret = db.SendSmsMessage(finder, list);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка SendMessage() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }

            return ret;
        }

        //----------------------------------------------------------------------
        public Returns SaveSmsReceiver(SmsReceiver finder)
        //----------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_SmsMessage cli = new cli_SmsMessage(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.SaveSmsReceiver(finder);
            }
            else
            {
                DbSmsMessage db = new DbSmsMessage();
                try
                {
                    ret = db.SaveSmsReceiver(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка SaveSmsReceiver() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }

            return ret;
        }

        //----------------------------------------------------------------------
        public Returns DeleteSmsReceiver(SmsReceiver finder)
        //----------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_SmsMessage cli = new cli_SmsMessage(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.DeleteSmsReceiver(finder);
            }
            else
            {
                DbSmsMessage db = new DbSmsMessage();
                try
                {
                    ret = db.DeleteSmsReceiver(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка DeleteSmsReceiver() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }

            return ret;
        }

        //----------------------------------------------------------------------
        public Returns ResendSmsMessage(SmsMessage finder, List<SmsMessage> list)
        //----------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_SmsMessage cli = new cli_SmsMessage(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.ResendSmsMessage(finder, list);
            }
            else
            {
                DbSmsMessage db = new DbSmsMessage();
                try
                {
                    ret = db.ResendSmsMessage(finder, list);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка ResendSmsMessage() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }

            return ret;
        }

        //----------------------------------------------------------------------
        public Returns DeleteSmsMessage(List<SmsMessage> list)
        //----------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_SmsMessage cli = new cli_SmsMessage(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.DeleteSmsMessage(list);
            }
            else
            {
                DbSmsMessage db = new DbSmsMessage();
                try
                {
                    ret = db.DeleteSmsMessage(list);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка DeleteSmsMessage() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }

            return ret;
        }
    }
}
