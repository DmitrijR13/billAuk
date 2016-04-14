using System;

using System.Collections.Generic;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Global;
using STCLINE.KP50.Client;

namespace STCLINE.KP50.Server
{
    public class srv_Nedop : srv_Base, I_Nedop //сервис Недопоставок
    {
        public List<Nedop> GetNedop(Nedop finder, enSrvOper oper, out Returns ret)
        {
            ret = Utils.InitReturns();            
            List<Nedop> list = null;

            if (SrvRunProgramRole.IsBroker)
            {
                //cli_Admin cliAdmin = new cli_Admin();
                //ret = cliAdmin.DbGetRemoteUser((Finder)finder);

                //if (ret.result)
                //{
                    //надо продублировать вызов к внутреннему хосту
                    cli_Nedop cli = new cli_Nedop(WCFParams.AdresWcfHost.CurT_Server);
                    list = cli.GetNedop(finder, oper, out ret);
                //}
            }
            else
            {
                DbNedop db = new DbNedop();
                try
                {
                    switch (oper)
                    {
                        case enSrvOper.SrvFind:
                            db.FindNedop(finder, out ret);
                            if (ret.result)
                            {
                                list = db.GetNedop(finder, out ret);
                            }
                            break;
                        case enSrvOper.SrvGet:
                            list = db.GetNedop(finder, out ret);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    ret = new Returns(false, "Ошибка вызова функции");
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetNedop(" + oper + ") \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    list = null;
                }
                db.Close();
            }
            return list;
        }

        public List<_Service> GetServicesForNedop(Nedop finder, out Returns ret)
        {
            List<_Service> l = new List<_Service>();
            ret = Utils.InitReturns();
            if (SrvRunProgramRole.IsBroker)
            {
                //ret = cli_Admin.DbGetRemoteUser((Finder)finder);

                //if (ret.result)
                //{
                    //надо продублировать вызов к внутреннему хосту
                    cli_Nedop cli = new cli_Nedop(WCFParams.AdresWcfHost.CurT_Server);
                    l = cli.GetServicesForNedop(finder, out ret);
                //}
            }
            else
            {
                DbNedop db = new DbNedop();
                try
                {
                    
                    l = db.GetServicesForNedop(finder, out ret);
                    db.Close();                   
                }
                catch (Exception ex)
                {
                    db.Close();

                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetServicesForNedop() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    return null;
                }
            } 
            return l;
        }

        public List<NedopType> GetNedopTypeForNedop(Nedop finder, out Returns ret)
        {
            List<NedopType> l = new List<NedopType>();
            ret = Utils.InitReturns();
            if (SrvRunProgramRole.IsBroker)
            {
                //ret = cli_Admin.DbGetRemoteUser((Finder)finder);

                //if (ret.result)
                //{
                    //надо продублировать вызов к внутреннему хосту
                    cli_Nedop cli = new cli_Nedop(WCFParams.AdresWcfHost.CurT_Server);
                    l = cli.GetNedopTypeForNedop(finder, out ret);
                //}
            }
            else
            {
                DbNedop db = new DbNedop();
                try
                {
                    
                    l = db.GetNedopTypeForNedop(finder, out ret);
                    db.Close();                   
                }
                catch (Exception ex)
                {
                    db.Close();

                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetNedopTypeForNedop() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    return null;
                }
            } 
            return l;
        }


        public List<NedopType> GetNedopWorkType(Nedop finder, out Returns ret)
        {
            List<NedopType> l = new List<NedopType>();
            ret = Utils.InitReturns();
            if (SrvRunProgramRole.IsBroker)
            {
                //ret = cli_Admin.DbGetRemoteUser((Finder)finder);

                //if (ret.result)
                //{
                    //надо продублировать вызов к внутреннему хосту
                    cli_Nedop cli = new cli_Nedop(WCFParams.AdresWcfHost.CurT_Server);
                    l = cli.GetNedopWorkType(finder, out ret);
                //}
            }
            else
            {
                DbNedop db = new DbNedop();
                try
                {

                    l = db.GetNedopWorkType(finder, out ret);
                    db.Close();
                }
                catch (Exception ex)
                {
                    db.Close();

                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetNedopWorkType() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    return null;
                }
            }
            return l;
        }


        public void SaveNedop(Nedop finder, Nedop additionalFinder, out Returns ret)
        {
            ret = Utils.InitReturns();
            DbNedop db = new DbNedop();
            try
            {
                ret = db.SaveNedop(finder, additionalFinder);
                db.Close();
            }
            catch (Exception ex)
            {
                db.Close();

                ret.result = false;
                ret.text = "Ошибка вызова функции";
                if (Constants.Viewerror) ret.text += " : " + ex.Message;
                if (Constants.Debug) MonitorLog.WriteLog("Ошибка SaveNedop() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return;
            }
        }

        public Returns UnlockNedop(Nedop finder)
        {
            Returns ret;
            if (SrvRunProgramRole.IsBroker)
            {
                //ret = cli_Admin.DbGetRemoteUser((Finder)finder);

                //if (ret.result)
                //{
                    //надо продублировать вызов к внутреннему хосту
                    cli_Nedop cli = new cli_Nedop(WCFParams.AdresWcfHost.CurT_Server);
                    ret = cli.UnlockNedop(finder);
                //}
            }
            else
            {
                DbNedop db = new DbNedop();
                try
                {
                    ret = db.UnlockNedop(finder);
                }
                catch (Exception ex)
                {
                    ret = new Returns(false, "Ошибка вызова функции");
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка UnlockNedop() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public Returns FindLSDomFromDomNedop(Nedop finder)
        {
            Returns ret = Utils.InitReturns();
            if (SrvRunProgramRole.IsBroker)
            {
                //ret = cli_Admin.DbGetRemoteUser((Finder)finder);

                //if (ret.result)
                //{
                    //надо продублировать вызов к внутреннему хосту
                    cli_Nedop cli = new cli_Nedop(WCFParams.AdresWcfHost.CurT_Server);
                    ret = cli.FindLSDomFromDomNedop(finder);
                //}
            }
            else
            {
                DbNedop db = new DbNedop();
                ret = db.FindLSDomFromDomNedop(finder);
                db.Close();
            }
            return ret;
        }
    }
}
