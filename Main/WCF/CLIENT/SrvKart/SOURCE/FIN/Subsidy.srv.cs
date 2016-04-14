using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Global;
using STCLINE.KP50.Client;
using STCLINE.KP50.DataBase;
using System.IO;

namespace STCLINE.KP50.Server
{
    public class srv_Subsidy : srv_Base, I_Subsidy
    {
        /// <summary>
        /// Получить список заявок на финансирование
        /// </summary>
        /// <param name="finder">Объект поиска</param>
        /// <returns>список заявок на финансирование</returns>
        public List<FinRequest> GetFinRequestsList(FinRequest finder, en_Supsidy_oper oper, out Returns ret)
        {
            List<FinRequest> retList = new List<FinRequest>();
            ret = Utils.InitReturns();
            string funcName = "";

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Subsidy cli = new cli_Subsidy(WCFParams.AdresWcfHost.CurT_Server);
                retList = cli.GetFinRequestsList(finder, oper, out ret);
            }
            else
            {
                DbSubsidy db = new DbSubsidy();
                try
                {
                    switch (oper)
                    {
                        case en_Supsidy_oper.GetRequests:
                            funcName = "Получение списка заявок на финансирование";
                            retList = db.GetRequests(finder, out ret);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    ret = new Returns(false, "Ошибка функции: " + funcName);
                    if (Constants.Viewerror) ret.text += ": " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка SRV:" + funcName + "() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                finally
                {
                    db.Close();
                }
            }

            return retList;

        }

        public List<FnSubsReqDetails> LoadSubsReqDetails(FnSubsReqDetailsForSearch finder, en_Supsidy_oper oper, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<FnSubsReqDetails> list = null;
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Subsidy cli = new cli_Subsidy(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.LoadSubsReqDetails(finder, oper, out ret);
            }
            else
            {

                DbSubsidy db = new DbSubsidy();

                try
                {
                    switch (oper)
                    {
                        case en_Supsidy_oper.LoadSubsReqDetails:
                            list = db.LoadSubsReqDetails(finder, out ret);
                            break;
                        case en_Supsidy_oper.LoadPayersFromSubsReqDetails:
                            list = db.LoadPayersFromSubsReqDetails(finder, out ret);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка LoadSubsReqDetails " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                    ret = new Returns(false, ex.Message);
                }
                db.Close();
            }
            return list;
        }

        public Returns SaveSubsReqDetails(List<FnSubsReqDetails> listfinder)
        {
            Returns ret;
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Subsidy cli = new cli_Subsidy(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.SaveSubsReqDetails(listfinder);
            }
            else
            {
                DbSubsidy db = new DbSubsidy();
                try
                {
                    ret = db.SaveSubsReqDetails(listfinder);
                    if (ret.result)
                    {
                        FinRequest finRequest = new FinRequest();
                        finRequest.nzp_user = listfinder[0].nzp_user;
                        finRequest.year = listfinder[0].year;
                        finRequest.nzp_req = listfinder[0].nzp_req;
                        List<Payer> list = new List<Payer>();
                        foreach (FnSubsReqDetails payer in listfinder)
                        {
                            Payer npayer = new Payer();
                            npayer.nzp_payer = payer.nzp_payer;
                            npayer.payer = payer.payer;
                            list.Add(npayer);
                        }

                        db.CalcPartSaldoSubsidy(finRequest, list);
                    }
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка SaveSubsReqDetails " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                    ret = new Returns(false, ex.Message);
                }
                db.Close();
            }
            return ret;
        }
               
        public List<FnSubsOrder> LoadSubsOrder(FnSubsOrder finder, out Returns ret)
        { 
            List<FnSubsOrder> list = null;
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Subsidy cli = new cli_Subsidy(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.LoadSubsOrder(finder, out ret);
            }
            else
            {
                DbSubsidy db = new DbSubsidy();

                try
                {
                    list = db.LoadSubsOrder(finder, out ret);
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка LoadSubsOrder " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                    ret = new Returns(false, ex.Message);
                }
                db.Close();
            }
            return list;
        }
       
        public Returns SaveSubsOrder(FnSubsOrderForSave finder)
        {
            Returns ret;
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Subsidy cli = new cli_Subsidy(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.SaveSubsOrder(finder);
            }
            else
            {
                DbSubsidy db = new DbSubsidy();
                try
                {
                    ret = db.SaveSubsOrder(finder);
                    if(ret.result)
                    {
                        FinRequest finRequest = new FinRequest();
                        finRequest.nzp_user = finder.nzp_user;
                        finRequest.year = finder.year;
                        finRequest.nzp_req = finder.nzp_req;
                        db.CheckSubsidyRequestStatus(finRequest);
                        List<Payer> list = new List<Payer>();
                        foreach(FnSubsReqDetails payer in finder.nzp_payers)
                        {
                            Payer npayer = new Payer();
                            npayer.nzp_payer = payer.nzp_payer;
                            npayer.payer = payer.payer;
                            list.Add(npayer);
                        }
                                               
                        db.CalcPartSaldoSubsidy(finRequest, list);
                    }
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка SaveSubsOrder " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                    ret = new Returns(false, ex.Message);
                }
                db.Close();
            }
            return ret;
        }
       
        public List<FnSubsSaldo> GetSubsSaldo(FnSubsSaldo finder, enSrvOper oper, out Returns ret)
        {
            ret = Utils.InitReturns();
            DbSubsidy db = new DbSubsidy();
            List<FnSubsSaldo> list = null;

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Subsidy cli = new cli_Subsidy(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.GetSubsSaldo(finder, oper, out ret);
            }
            else
            {
                switch (oper)
                {
                    case enSrvOper.SrvFind:
                        db.FindSubsSaldo(finder, out ret);
                        if (ret.result)
                            list = db.GetSubsSaldo(finder, out ret);
                        break;
                    case enSrvOper.SrvGet:
                        list = db.GetSubsSaldo(finder, out ret);
                        break;
                }
                db.Close();
            }
            return list;
        }

        public List<FnSubsContract> LoadSubsContract(FnSubsContractForSearch finder, en_Supsidy_oper oper, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<FnSubsContract> list = null;
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Subsidy cli = new cli_Subsidy(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.LoadSubsContract(finder, oper, out ret);
            }
            else
            {
                DbSubsidy db = new DbSubsidy();

                try
                {
                    switch (oper)
                    {
                        case en_Supsidy_oper.LoadSubsContract:
                            list = db.LoadSubsContract(finder, out ret);
                            break;
                        case en_Supsidy_oper.LoadSubContractForPayer:
                            list = db.LoadSubContractForPayer(finder, out ret);
                            break;
                    }
                    
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка LoadSubsContract " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                    ret = new Returns(false, ex.Message);
                }
                db.Close();
            }
            return list;
        }

        public List<FnAgreement> GetAgreementsList(FnAgreement finder, out Returns ret)
        {
            List<FnAgreement> retList = new List<FnAgreement>();
            ret = Utils.InitReturns();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Subsidy cli = new cli_Subsidy(WCFParams.AdresWcfHost.CurT_Server);
                retList = cli.GetAgreementsList(finder, out ret);
            }
            else
            {
                DbSubsidy db = new DbSubsidy();
                try
                {
                    retList = db.GetAgreementsList(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret = new Returns(false, "Ошибка функции: GetAgreementsList ");
                    if (Constants.Viewerror) ret.text += ": " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка SRV:" + "GetAgreementsList" + "() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                finally
                {
                    db.Close();
                }
            }

            return retList;
        }

        public List<FnAgreement> GetAgreementTypes(out Returns ret)
        {
            List<FnAgreement> retList = new List<FnAgreement>();
            ret = Utils.InitReturns();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Subsidy cli = new cli_Subsidy(WCFParams.AdresWcfHost.CurT_Server);
                retList = cli.GetAgreementTypes(out ret);
            }
            else
            {
                DbSubsidy db = new DbSubsidy();
                try
                {
                    retList = db.GetAgreementTypes(out ret);
                }
                catch (Exception ex)
                {
                    ret = new Returns(false, "Ошибка функции: GetAgreementTypes ");
                    if (Constants.Viewerror) ret.text += ": " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка SRV:" + "GetAgreementTypes" + "() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                finally
                {
                    db.Close();
                }
            }

            return retList;
        }

        public FnAgreement AddUpdateAgreement(FnAgreement finder, out Returns ret)
        {
            FnAgreement agr = new FnAgreement();
            ret = Utils.InitReturns();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Subsidy cli = new cli_Subsidy(WCFParams.AdresWcfHost.CurT_Server);
                agr = cli.AddUpdateAgreement(finder,out ret);
            }
            else
            {
                DbSubsidy db = new DbSubsidy();
                try
                {
                    agr = db.AddUpdateAgreement(finder,out ret);
                }
                catch (Exception ex)
                {
                    ret = new Returns(false, "Ошибка функции: AddUpdateAgreement ");
                    if (Constants.Viewerror) ret.text += ": " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка SRV:" + "AddUpdateAgreement" + "() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                finally
                {
                    db.Close();
                }
            }

            return agr;
        }

        public bool DelAgreement(FnAgreement finder, out Returns ret)
        {
            bool res = false;
            ret = Utils.InitReturns();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Subsidy cli = new cli_Subsidy(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.DelAgreement(finder, out ret);
            }
            else
            {
                DbSubsidy db = new DbSubsidy();
                try
                {
                    res = db.DelAgreement(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret = new Returns(false, "Ошибка функции: DelAgreement ");
                    if (Constants.Viewerror) ret.text += ": " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка SRV:" + "DelAgreement" + "() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                finally
                {
                    db.Close();
                }
            }

            return res;
        }

        public Returns MakeOperation(Finder finder, SubsidyOperations operation)
        {
            Returns ret;
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Subsidy cli = new cli_Subsidy(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.MakeOperation(finder, operation);
            }
            else
            {
                DbSubsidy db = new DbSubsidy();
                try
                {
                    switch (operation)
                    {                         
                        default:
                            ret = new Returns(false, "Неизвестная операция");
                            break;
                    }
                    
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка MakeOperation("+operation+")\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    ret = new Returns(false, ex.Message);
                }
                db.Close();
            }
            return ret;
        }

        public Returns OperateWithOrder(FnSubsOrder finder, SubsidyOrderOperations operation)
        {
            Returns ret;
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Subsidy cli = new cli_Subsidy(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.OperateWithOrder(finder, operation);
            }
            else
            {
                DbSubsidy db = new DbSubsidy();
                try
                {
                    switch (operation)
                    {
                        case SubsidyOrderOperations.CancelDistribution:
                            ret = db.CancelOrderDistribution(finder);
                            break;
                        case SubsidyOrderOperations.DistributeOrders:
                            ret = db.DistributeFin(finder);
                            break;
                        case SubsidyOrderOperations.Delete:
                            ret = db.DeleteOrder(finder);
                            break;
                        default:
                            ret = new Returns(false, "Неизвестная операция");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка OperateWithOrder(" + operation + ")\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    ret = new Returns(false, ex.Message);
                }
                db.Close();
            }
            return ret;
        }

        public List<PercPt> GetPercPtList(PercPt finder, out Returns ret)
        {
            List<PercPt> retList = new List<PercPt>();
            ret = Utils.InitReturns();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Subsidy cli = new cli_Subsidy(WCFParams.AdresWcfHost.CurT_Server);
                retList = cli.GetPercPtList(finder, out ret);
            }
            else
            {
                DbSubsidy db = new DbSubsidy();
                try
                {
                    retList = db.GetPercPtList(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret = new Returns(false, "Ошибка функции: GetPercPtList ");
                    if (Constants.Viewerror) ret.text += ": " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка SRV:" + "GetPercPtList" + "() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                finally
                {
                    db.Close();
                }
            }

            return retList;
        }


        /// <summary>
        /// Добавление нового кассового плана
        /// </summary>
        /// <param name="kassPlanFileString">файл кассового плана строкой</param>
        /// <param name="subsContract">Соглашение, заполнить nzp_contract</param>
        /// <param name="kassPlan">Добавленный кассовый план</param> 
        /// <returns>результат</returns>
        public Returns LoadKassPlan(string kassPlanFileString, FnSubsContract subsContract, out FnSubsKassPlan kassPlan)
        {
            Returns ret = Utils.InitReturns();


            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Subsidy cli = new cli_Subsidy(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.LoadKassPlan(kassPlanFileString, subsContract, out kassPlan);
            }
            else
            {
                DbSubsidy db = new DbSubsidy();
                try
                {

                    ret = db.LoadKassPlan(kassPlanFileString, subsContract, out kassPlan);

                }
                catch (Exception ex)
                {
                    ret = new Returns(false, "Ошибка функции:  LoadKassPlan");
                    if (Constants.Viewerror) ret.text += ": " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка SRV: LoadKassPlan () \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    kassPlan = new FnSubsKassPlan();
                }
                finally
                {
                    db.Close();
                }
            }

            

            return ret;

        }


        /// <summary>
        /// Удаление кассового плана
        /// </summary>
        /// <param name="subsKassPlan">Кассовый план с заполненным кодом и датой date_begin</param>
        /// <returns>результат</returns>
        public Returns DeleteKassPlan(FnSubsKassPlan subsKassPlan)
        {
            Returns ret = Utils.InitReturns();


            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Subsidy cli = new cli_Subsidy(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.DeleteKassPlan(subsKassPlan);
            }
            else
            {
                DbSubsidy db = new DbSubsidy();
                try
                {

                    ret = db.DeleteKassPlan(subsKassPlan);

                }
                catch (Exception ex)
                {
                    ret = new Returns(false, "Ошибка функции:  DeleteKassPlan");
                    if (Constants.Viewerror) ret.text += ": " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка SRV: DeleteKassPlan () \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                finally
                {
                    db.Close();
                }
            }

            return ret;

        }

        /// <summary>
        /// Добавление нового помесячного плана распределения плана
        /// </summary>
        /// <param name="monthPlanFileString">файл кассового плана строкой</param>
        /// <param name="subsContract">Соглашение, заполнить nzp_contract</param>
        /// <param name="monthPlan">Добавленный помесячный план</param>
        /// <returns>результат</returns>
        public Returns LoadMonthPlan(string monthPlanFileString, FnSubsContract subsContract, out FnSubsMonthPlan monthPlan)
        {
            Returns ret = Utils.InitReturns();


            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Subsidy cli = new cli_Subsidy(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.LoadMonthPlan(monthPlanFileString, subsContract, out monthPlan);
            }
            else
            {
                DbSubsidy db = new DbSubsidy();
                try
                {

                    ret = db.LoadMonthPlan(monthPlanFileString, subsContract, out monthPlan);

                }
                catch (Exception ex)
                {
                    ret = new Returns(false, "Ошибка функции:  LoadKassPlan");
                    if (Constants.Viewerror) ret.text += ": " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка SRV: LoadKassPlan () \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    monthPlan = new FnSubsMonthPlan();

                }
                finally
                {
                    db.Close();
                }
            }
        

            return ret;

        }

        /// <summary>
        /// Удаление помесячного плана
        /// </summary>
        /// <param name="subsKassPlan">Кассовый план с заполненным кодом и датой date_begin</param>
        /// <returns>результат</returns>
        public Returns DeleteMonthPlan(FnSubsMonthPlan subsMonthPlan)
        {
            Returns ret = Utils.InitReturns();


            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Subsidy cli = new cli_Subsidy(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.DeleteMonthPlan(subsMonthPlan);
            }
            else
            {
                DbSubsidy db = new DbSubsidy();
                try
                {

                    ret = db.DeleteMonthPlan(subsMonthPlan);

                }
                catch (Exception ex)
                {
                    ret = new Returns(false, "Ошибка функции:  DeleteMonthPlan");
                    if (Constants.Viewerror) ret.text += ": " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка SRV: DeleteMonthPlan () \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                finally
                {
                    db.Close();
                }
            }

            return ret;

        }


        public PercPt UpdatePercPt(PercPt finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            PercPt perc = new PercPt();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Subsidy cli = new cli_Subsidy(WCFParams.AdresWcfHost.CurT_Server);
                perc = cli.UpdatePercPt(finder, out ret);
            }
            else
            {
                DbSubsidy db = new DbSubsidy();
                try
                {

                    perc = db.UpdatePercPt(finder, out ret);

                }
                catch (Exception ex)
                {
                    ret = new Returns(false, "Ошибка функции:  UpdatePercPt");
                    if (Constants.Viewerror) ret.text += ": " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка SRV: UpdatePercPt () \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    return null;
                }
                finally
                {
                    db.Close();
                }
            }

            return perc;

        }


        public List<FnSubsKassPlan> GetSubsKassPlan(FnSubsKassPlan finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<FnSubsKassPlan> retList = new List<FnSubsKassPlan>();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Subsidy cli = new cli_Subsidy(WCFParams.AdresWcfHost.CurT_Server);
                retList = cli.GetSubsKassPlan(finder, out ret);
            }
            else
            {
                DbSubsidy db = new DbSubsidy();
                try
                {

                    retList = db.GetSubsKassPlan(finder, out ret);

                }
                catch (Exception ex)
                {
                    ret = new Returns(false, "Ошибка функции:  GetSubsKassPlan");
                    if (Constants.Viewerror) ret.text += ": " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка SRV: GetSubsKassPlan () \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    return null;
                }
                finally
                {
                    db.Close();
                }
            }

            return retList;

        }

        public FnSubsMonthPlan GetFnSubsMonthPlans(FnSubsMonthPlan finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            FnSubsMonthPlan retPlan = new FnSubsMonthPlan();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Subsidy cli = new cli_Subsidy(WCFParams.AdresWcfHost.CurT_Server);
                retPlan = cli.GetFnSubsMonthPlans(finder, out ret);
            }
            else
            {
                DbSubsidy db = new DbSubsidy();
                try
                {
                    retPlan = db.GetFnSubsMonthPlans(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret = new Returns(false, "Ошибка функции:  GetFnSubsMonthPlans");
                    if (Constants.Viewerror) ret.text += ": " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка SRV: GetFnSubsMonthPlans () \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    return null;
                }
                finally
                {
                    db.Close();
                }
            }

            return retPlan;

        }

        public List<FnSubsAct> LoadSubsAct(FnSubsActForSearch finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<FnSubsAct> list = null;
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Subsidy cli = new cli_Subsidy(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.LoadSubsAct(finder, out ret);
            }
            else
            {
                DbSubsidy db = new DbSubsidy();

                try
                {

                    list = db.LoadSubsAct(finder, out ret);

                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка LoadSubsAct " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                    ret = new Returns(false, ex.Message);
                }
                db.Close();
            }
            return list;
        }

        public Returns LoadTehHarGilFond(string fileName, FnSubsContract subsContract)
        {
            Returns ret = Utils.InitReturns();         
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Subsidy cli = new cli_Subsidy(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.LoadTehHarGilFond(fileName, subsContract);
            }
            else
            {
                DbSubsidy db = new DbSubsidy();
                try
                {

                    ret = db.LoadTehHarGilFond(fileName, subsContract);

                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка LoadTehHarGilFond " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                    ret = new Returns(false, ex.Message);
                }
                db.Close();
            }
            return ret;
        }

        /// <summary>
        /// Получить список характеристик жилого фонда
        /// </summary>
        /// <param name="finder">объект поисковик</param>
        /// <returns>Список характеристик жф</returns>
        public List<FnSubsTehHarGilFond> GetHarGilFondList(FnSubsTehHarGilFond finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<FnSubsTehHarGilFond> list = null;
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Subsidy cli = new cli_Subsidy(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.GetHarGilFondList(finder, out ret);
            }
            else
            {
                DbSubsidy db = new DbSubsidy();

                try
                {

                    list = db.GetHarGilFondList(finder, out ret);

                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка GetHarGilFondList " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                    ret = new Returns(false, ex.Message);
                }
                db.Close();
            }
            return list;
        }

        public FnSubsTehHarGilFond UpdateHarGilFond(FnSubsTehHarGilFond finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            FnSubsTehHarGilFond retHarGilFond = new FnSubsTehHarGilFond();
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Subsidy cli = new cli_Subsidy(WCFParams.AdresWcfHost.CurT_Server);
                retHarGilFond = cli.UpdateHarGilFond(finder, out ret);
            }
            else
            {
                DbSubsidy db = new DbSubsidy();

                try
                {

                    retHarGilFond = db.UpdateHarGilFond(finder, out ret);

                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка UpdateHarGilFond " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                    ret = new Returns(false, ex.Message);
                }
                db.Close();
            }
            return retHarGilFond;
        }

        public ReturnsType LoadAktSubsidy_rust(string filename, FnSubsActForSearch finder)
        {
            try
            {
                if (SrvRun.isBroker)
                {
                    // продублировать вызов к внутреннему хосту
                    cli_Subsidy cli = new cli_Subsidy(WCFParams.AdresWcfHost.CurT_Server);
                    return cli.LoadAktSubsidy_rust(filename, finder);
                }
                else
                {
                    DbSubsidy db = new DbSubsidy();
                    return db.LoadAktSubsidy_rust(filename, finder);
                }
            }
            catch (Exception ex)
            {
                Returns ret = Utility.ExceptionUtility.OnException(ex, System.Reflection.MethodBase.GetCurrentMethod().Name);
                return new ReturnsType(ret.result, ret.text, ret.tag);
            }
        }

        public Returns DeleteSubsAct(FnSubsActForSearch finder)
        {
            Returns ret = Utils.InitReturns();


            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Subsidy cli = new cli_Subsidy(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.DeleteSubsAct(finder);
            }
            else
            {
                DbSubsidy db = new DbSubsidy();
                try
                {

                    ret = db.DeleteSubsAct(finder);

                }
                catch (Exception ex)
                {
                    ret = new Returns(false, "Ошибка функции:  DeleteSubsAct");
                    if (Constants.Viewerror) ret.text += ": " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка SRV: DeleteSubsAct () \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                finally
                {
                    db.Close();
                }
            }

            return ret;

        }

        public Returns UseActOfSupply(FnSubsActForSearch finder)
        {
            Returns ret = Utils.InitReturns();


            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Subsidy cli = new cli_Subsidy(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.UseActOfSupply(finder);
            }
            else
            {
                DbSubsidy db = new DbSubsidy();
                try
                {

                    ret = db.UseActOfSupply(finder);

                }
                catch (Exception ex)
                {
                    ret = new Returns(false, "Ошибка функции:  UseActOfSupply");
                    if (Constants.Viewerror) ret.text += ": " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка SRV: UseActOfSupply () \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                finally
                {
                    db.Close();
                }
            }

            return ret;

        }

        public Returns UseTehHarGilFond(FnSubsTehHarGilFond finder)
        {
            Returns ret = Utils.InitReturns();


            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Subsidy cli = new cli_Subsidy(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.UseTehHarGilFond(finder);
            }
            else
            {
                DbSubsidy db = new DbSubsidy();
                try
                {

                    ret = db.UseTehHarGilFond(finder);

                }
                catch (Exception ex)
                {
                    ret = new Returns(false, "Ошибка функции:  UseTehHarGilFond");
                    if (Constants.Viewerror) ret.text += ": " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка SRV: UseTehHarGilFond () \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                finally
                {
                    db.Close();
                }
            }

            return ret;

        }

        public Returns GetCountContractFromTables(FnAgreement finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            Returns result = Utils.InitReturns();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Subsidy cli = new cli_Subsidy(WCFParams.AdresWcfHost.CurT_Server);
                result = cli.GetCountContractFromTables(finder, out ret);
            }
            else
            {
                DbSubsidy db = new DbSubsidy();
                try
                {
                    result = db.GetCountContractFromTables(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret = new Returns(false, "Ошибка функции:  GetCountContractFromTables");
                    if (Constants.Viewerror) ret.text += ": " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка SRV: GetCountContractFromTables () \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                finally
                {
                    db.Close();
                }
            }

            return result;
        }
    }
}
