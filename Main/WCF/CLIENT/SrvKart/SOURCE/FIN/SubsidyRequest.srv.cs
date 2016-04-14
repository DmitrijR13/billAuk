using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Global;
using STCLINE.KP50.Client;
using STCLINE.KP50.DataBase;

namespace STCLINE.KP50.Server
{
    public class srv_SubsidyRequest : srv_Base, I_SubsidyRequest
    {
       
        public Returns AddSubsidyRequest(ref FinRequest finRequest)
        {
           Returns ret = Utils.InitReturns();
            

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_SubsidyRequest cli = new cli_SubsidyRequest(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.AddSubsidyRequest(ref finRequest);
            }
            else
            {
                DbSubsidy db = new DbSubsidy();
                try
                {

                    ret = db.AddSubsidyRequest(ref finRequest);
                     
                }
                catch (Exception ex)
                {
                    ret = new Returns(false, "Ошибка функции:  AddSubsidyRequest");
                    if (Constants.Viewerror) ret.text += ": " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка SRV: AddSubsidyRequest () \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                finally
                {
                    db.Close();
                }
            }

            return ret;

        }

        public Returns UpdateSubsidyRequest(FinRequest finRequest)
        {
            Returns ret = Utils.InitReturns();


            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_SubsidyRequest cli = new cli_SubsidyRequest(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.UpdateSubsidyRequest(finRequest);
            }
            else
            {
                DbSubsidy db = new DbSubsidy();
                try
                {

                    ret = db.UpdateSubsidyRequest(finRequest);

                }
                catch (Exception ex)
                {
                    ret = new Returns(false, "Ошибка функции:  UpdateSubsidyRequest");
                    if (Constants.Viewerror) ret.text += ": " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка SRV: UpdateSubsidyRequest () \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                finally
                {
                    db.Close();
                }
            }

            return ret;

        }

        public List<int> ListAvailableSubsidyStatus(int nzpStatus, out Returns ret)
        {
            ret = Utils.InitReturns();

            List<int> listStatus = new List<int>();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_SubsidyRequest cli = new cli_SubsidyRequest(WCFParams.AdresWcfHost.CurT_Server);
                    listStatus = cli.ListAvailableSubsidyStatus(nzpStatus, out ret);
            }
            else
            {
                DbSubsidy db = new DbSubsidy();
                try
                {

                    listStatus = db.ListAvailableSubsidyStatus(nzpStatus, out ret);

                }
                catch (Exception ex)
                {
                    ret = new Returns(false, "Ошибка функции:  UpdateSubsidyRequest");
                    if (Constants.Viewerror) ret.text += ": " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка SRV: UpdateSubsidyRequest () \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                finally
                {
                    db.Close();
                }
            }

            return listStatus;

        }

        public Returns CheckSubsidyRequestStatus(FinRequest finRequest)
        {
            Returns ret = Utils.InitReturns();


            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_SubsidyRequest cli = new cli_SubsidyRequest(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.CheckSubsidyRequestStatus(finRequest);
            }
            else
            {
                DbSubsidy db = new DbSubsidy();
                try
                {

                    ret = db.CheckSubsidyRequestStatus(finRequest);

                }
                catch (Exception ex)
                {
                    ret = new Returns(false, "Ошибка функции:  UpdateSubsCheckSubsidyRequestStatusidyRequest");
                    if (Constants.Viewerror) ret.text += ": " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка SRV: CheckSubsidyRequestStatus () \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                finally
                {
                    db.Close();
                }
            }

            return ret;

        }

        public Returns CalcPartSaldoSubsidy(FinRequest finRequest, List<Payer> listPayer)
        {
            Returns ret = Utils.InitReturns();


            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_SubsidyRequest cli = new cli_SubsidyRequest(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.CalcPartSaldoSubsidy(finRequest, listPayer);
            }
            else
            {
                DbSubsidy db = new DbSubsidy();
                try
                {

                    ret = db.CalcPartSaldoSubsidy(finRequest, listPayer);

                }
                catch (Exception ex)
                {
                    ret = new Returns(false, "Ошибка функции:  CalcPartSaldoSubsidy");
                    if (Constants.Viewerror) ret.text += ": " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка SRV: CalcPartSaldoSubsidy () \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                finally
                {
                    db.Close();
                }
            }

            return ret;

        }

        public Returns RequestFonTasks(int nzpReq, out Returns ret)
        {
            ret = Utils.InitReturns();
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_SubsidyRequest cli = new cli_SubsidyRequest(WCFParams.AdresWcfHost.CurT_Server);
                cli.RequestFonTasks(nzpReq, out ret);
            }
            else
            {
                DbCalcCharge db = new DbCalcCharge();
                try
                {
                    db.RequestFonTasks(nzpReq, out ret);
                }
                catch (Exception ex)
                {
                    ret = new Returns(false, "Ошибка функции: RequestFonTasks");
                    if (Constants.Viewerror) ret.text += ": " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка SRV: RequestFonTasks () \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                finally
                {
                    db.Close();
                }
            }
            return ret;

        }
    }
}
