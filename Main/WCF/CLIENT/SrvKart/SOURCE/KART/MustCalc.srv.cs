using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Global;
using STCLINE.KP50.Client;
using System.Data;

namespace STCLINE.KP50.Server
{
    public class srv_MustCalc : srv_Base, I_MustCalc
    {
        public Returns OperationsWithMustCalc(MustCalc finder, MustCalcOperations operation)
        {
            Returns ret;
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_MustCalc cli = new cli_MustCalc(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.OperationsWithMustCalc(finder, operation);
            }
            else
            {
                DbMustCalc db = new DbMustCalc();
                try
                {
                    switch (operation)
                    {
                        case MustCalcOperations.Save:
                            ret = db.SaveMustCalc(finder);
                            break;
                        case MustCalcOperations.Delete:
                            ret = db.DeleteMustCalc(finder);
                            break;
                        default:
                            ret = new Returns(false, "Неизвестная операция");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка OperationsWithMustCalc(" + operation + ")\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    ret = new Returns(false, ex.Message);
                }
                db.Close();
            }
            return ret;
        }

        public Returns SaveSpLsMustCalc(MustCalc finder, List<Service> services)
        {
            Returns ret;
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_MustCalc cli = new cli_MustCalc(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.SaveSpLsMustCalc(finder, services);
            }
            else
            {
                DbMustCalc db = new DbMustCalc();
                try
                {

                    ret = db.SaveMustCalcTXXspls(finder, services);

                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка SaveSpLsMustCalc()\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    ret = new Returns(false, ex.Message);
                }
                db.Close();
            }
            return ret;
        }

        public List<MustCalc> LoadMustCalc(MustCalc finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<MustCalc> list = null;
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_MustCalc cli = new cli_MustCalc(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.LoadMustCalc(finder, out ret);
            }
            else
            {
                DbMustCalc db = new DbMustCalc();
                try
                {

                    list = db.LoadMustCalc(finder, out ret);

                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка LoadMustCalc()\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    ret = new Returns(false, ex.Message);
                }
                db.Close();
            }
            return list;
        }

        public List<ProhibitedMustCalc> GetProhibitedMustCalc(ProhibitedMustCalc finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<ProhibitedMustCalc> list = null;
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                var cli = new cli_MustCalc(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.GetProhibitedMustCalc(finder, out ret);
            }
            else
            {
                var db = new DbMustCalc();
                try
                {

                    list = db.GetProhibitedMustCalc(finder, out ret);

                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка GetProhibitedMustCalc()\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    ret = new Returns(false, ex.Message);
                }
                db.Close();
            }
            return list;
        }

        public void SaveProhibitedMustCalc(ProhibitedMustCalc finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                var cli = new cli_MustCalc(WCFParams.AdresWcfHost.CurT_Server);
                cli.SaveProhibitedMustCalc(finder, out ret);
            }
            else
            {
                var db = new DbMustCalc();
                try
                {
                    db.SaveProhibitedMustCalc(finder, out ret);
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка SaveProhibitedMustCalc()\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    ret = new Returns(false, ex.Message);
                }
                db.Close();
            }
            return;
        }

        public void DeleteProhibitedMustCalc(ProhibitedMustCalc finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                var cli = new cli_MustCalc(WCFParams.AdresWcfHost.CurT_Server);
                cli.DeleteProhibitedMustCalc(finder, out ret);
            }
            else
            {
                var db = new DbMustCalc();
                try
                {
                    db.DeleteProhibitedMustCalc(finder, out ret);
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка DeleteProhibitedMustCalc()\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    ret = new Returns(false, ex.Message);
                }
                db.Close();
            }
            return;
        }


        public Returns SaveDisableMustCalcTXXspls(MustCalc finder, List<Service> services)
        {
            var ret = Utils.InitReturns();
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                var cli = new cli_MustCalc(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.SaveDisableMustCalcTXXspls(finder, services);
            }
            else
            {
                var db = new DbMustCalc();
                try
                {
                    ret = db.SaveDisableMustCalcTxXspls(finder, services);
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка SaveDisableMustCalcTXXspls()\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    ret = new Returns(false, ex.Message);
                }
                db.Close();
            }
            return ret;
        }

        public List<Service> LoadSuppliersForDisableMustCalcLs(Service finder, out Returns ret)
        {
            var list = new List<Service>();
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                var cli = new cli_MustCalc(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.LoadSuppliersForDisableMustCalcLs(finder, out ret);
            }
            else
            {
                var db = new DbMustCalc();
                try
                {
                    list = db.LoadSuppliersForDisableMustCalcLs(finder, out ret);
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка LoadSuppliersForDisableMustCalcLs()\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    ret = new Returns(false, ex.Message);
                }
                db.Close();
            }
            return list;
        }

        public List<Service> LoadServiceForDisableMustCalcLs(Service finder, out Returns ret)
        {
            var list = new List<Service>();
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                var cli = new cli_MustCalc(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.LoadServiceForDisableMustCalcLs(finder, out ret);
            }
            else
            {
                var db = new DbMustCalc();
                try
                {
                    list = db.LoadServiceForDisableMustCalcLs(finder, out ret);
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка LoadServiceForDisableMustCalcLs()\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    ret = new Returns(false, ex.Message);
                }
                db.Close();
            }
            return list;
        }
    }
}
