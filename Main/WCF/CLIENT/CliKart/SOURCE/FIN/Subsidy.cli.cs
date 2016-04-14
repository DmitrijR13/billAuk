using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Global;
using System.ServiceModel;

namespace STCLINE.KP50.Client
{
    public class cli_Subsidy : I_Subsidy
    {
        I_Subsidy remoteObject;

        public cli_Subsidy(int nzp_server)
        {
            _cli_Subsidy(nzp_server, Constants.roleSubsidy);
        }

        public void _cli_Subsidy(int nzp_server, int nzp_role)
        {
            string addrHost = "";
            //определить параметры доступа
            _RServer zap = MultiHost.GetServer(nzp_server);

            if (Points.IsMultiHost && nzp_server > 0)
            {
                addrHost = zap.ip_adr + WCFParams.AdresWcfWeb.srvSubsidy;
                remoteObject = HostChannel.CreateInstance<I_Subsidy>(zap.login, zap.pwd, addrHost);
            }
            else
            {
                //по-умолчанию
                if (nzp_role == Constants.roleSubsidy)
                    addrHost = WCFParams.AdresWcfWeb.Adres + WCFParams.AdresWcfWeb.srvSubsidy;
                else addrHost = WCFParams.AdresWcfWeb.Adres + WCFParams.AdresWcfWeb.srvSubsidy;
                zap.rcentr = "<локально>";
                remoteObject = HostChannel.CreateInstance<I_Subsidy>(addrHost);
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

        /// <summary>
        /// Получить список заявок на финансирование
        /// </summary>
        /// <param name="finder">Объект поиска</param>
        /// <param name="oper"></param>
        /// <param name="ret"></param>
        /// <returns>список заявок на финансирование</returns>
        public List<FinRequest> GetFinRequestsList(FinRequest finder, en_Supsidy_oper oper, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<FinRequest> requsetsList = new List<FinRequest>();
            try
            {
                requsetsList = remoteObject.GetFinRequestsList(finder, oper ,out ret);
                HostChannel.CloseProxy(remoteObject);
                return requsetsList;
            }
            catch (Exception ex)
            {
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else
                    ret.text = "Ошибка получения данных завления";

                MonitorLog.WriteLog("Ошибка GetFinRequestsList \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return null;
            }
        }

        public List<FnSubsReqDetails> LoadSubsReqDetails(FnSubsReqDetailsForSearch finder, en_Supsidy_oper oper, out Returns ret)
        {
            try
            {
                List<FnSubsReqDetails> list = remoteObject.LoadSubsReqDetails(finder, oper, out ret);
                HostChannel.CloseProxy(remoteObject);
                return list;
            }
            catch (Exception ex)
            {
                ret = Utils.InitReturns();
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка LoadSubsReqDetails " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public Returns SaveSubsReqDetails(List<FnSubsReqDetails> listfinder)
        {
            try
            {
                Returns ret = remoteObject.SaveSubsReqDetails(listfinder);
                HostChannel.CloseProxy(remoteObject);
                return ret;
            }
            catch (Exception ex)
            {
                Returns ret = Utils.InitReturns();
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка SaveSubsReqDetails " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }
            
        public List<FnSubsOrder> LoadSubsOrder(FnSubsOrder finder, out Returns ret)
            {
                try
                {
                    List<FnSubsOrder> list = remoteObject.LoadSubsOrder(finder, out ret);
                    HostChannel.CloseProxy(remoteObject);
                    return list;
                }
                catch (Exception ex)
                {
                    ret = Utils.InitReturns();
                    ret.result = false;
                    if (ex is System.ServiceModel.EndpointNotFoundException)
                    {
                        ret.text = Constants.access_error;
                        ret.tag = Constants.access_code;
                    }
                    else ret.text = ex.Message;
                    MonitorLog.WriteLog("Ошибка LoadSubsOrder " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                    return null;
                }
            }

        public Returns SaveSubsOrder(FnSubsOrderForSave finder)
            {
                try
                {
                    Returns ret = remoteObject.SaveSubsOrder(finder);
                    HostChannel.CloseProxy(remoteObject);
                    return ret;
                }
                catch (Exception ex)
                {
                    Returns ret = Utils.InitReturns();
                    ret.result = false;
                    if (ex is System.ServiceModel.EndpointNotFoundException)
                    {
                        ret.text = Constants.access_error;
                        ret.tag = Constants.access_code;
                    }
                    else ret.text = ex.Message;
                    MonitorLog.WriteLog("Ошибка SaveSubsOrder " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                    return ret;
                }
            }

        public List<FnSubsSaldo> GetSubsSaldo(FnSubsSaldo finder, enSrvOper oper, out Returns ret)
            {
                ret = Utils.InitReturns();
                try
                {
                    List<FnSubsSaldo> Spis = new List<FnSubsSaldo>();
                    Spis = remoteObject.GetSubsSaldo(finder, oper, out ret);
                    HostChannel.CloseProxy(remoteObject);
                    return Spis;
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

                    string err = "";
                    if (Constants.Viewerror) err = " \n " + ex.Message;

                    MonitorLog.WriteLog("Ошибка GetSubsSaldo(" + oper.ToString() + ") " + err, MonitorLog.typelog.Error, 2, 100, true);
                    return null;
                }
            }

        public List<FnSubsContract> LoadSubsContract(FnSubsContractForSearch finder, en_Supsidy_oper oper, out Returns ret)
        {
            try
            {
                List<FnSubsContract> list = remoteObject.LoadSubsContract(finder, oper, out ret);
                HostChannel.CloseProxy(remoteObject);
                return list;
            }
            catch (Exception ex)
            {
                ret = Utils.InitReturns();
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка LoadSubsContract " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }


        public List<FnAgreement> GetAgreementsList(FnAgreement finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<FnAgreement> requsetsList = new List<FnAgreement>();
            try
            {
                requsetsList = remoteObject.GetAgreementsList(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return requsetsList;
            }
            catch (Exception ex)
            {
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else
                    ret.text = "Ошибка получения данных соглашения с подрадчиком";

                MonitorLog.WriteLog("Ошибка GetAgreementsList \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return null;
            }
        }

        public List<FnAgreement> GetAgreementTypes(out Returns ret)
        {
            ret = Utils.InitReturns();
            List<FnAgreement> requsetsList = new List<FnAgreement>();
            try
            {
                requsetsList = remoteObject.GetAgreementTypes(out ret);
                HostChannel.CloseProxy(remoteObject);
                return requsetsList;
            }
            catch (Exception ex)
            {
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else
                    ret.text = "Ошибка получения справочных данных типов соглашений с подрадчиком";

                MonitorLog.WriteLog("Ошибка GetAgreementsList \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return null;
            }
        }
        
        
        public FnAgreement AddUpdateAgreement(FnAgreement finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            FnAgreement agr = new FnAgreement();
            try
            {
                agr = remoteObject.AddUpdateAgreement(finder,out ret);
                HostChannel.CloseProxy(remoteObject);
                return agr;
            }
            catch (Exception ex)
            {
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else
                    ret.text = "Ошибка добавления/обновления соглашения с подрадчиком";

                MonitorLog.WriteLog("Ошибка AddUpdateAgreement \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return null;
            }
        }

        public bool DelAgreement(FnAgreement finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            bool res = false;
            try
            {
                res = remoteObject.DelAgreement(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return res;
            }
            catch (Exception ex)
            {
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else
                    ret.text = "Ошибка удаления соглашения с подрадчиком";

                MonitorLog.WriteLog("Ошибка DelAgreement \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return false;
            }
        }

        public Returns MakeOperation(Finder finder, SubsidyOperations operation)
        {
            try
            {
                Returns ret = remoteObject.MakeOperation(finder, operation);
                HostChannel.CloseProxy(remoteObject);
                return ret;
            }
            catch (Exception ex)
            {
                Returns ret = Utils.InitReturns();
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка MakeOperation(" + operation + ")\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }

        public Returns OperateWithOrder(FnSubsOrder finder, SubsidyOrderOperations operation)
        {
            try
            {
                Returns ret = remoteObject.OperateWithOrder(finder, operation);
                HostChannel.CloseProxy(remoteObject);
                return ret;
            }
            catch (Exception ex)
            {
                Returns ret = Utils.InitReturns();
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка OperateWithOrder(" + operation + ")\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }


        public List<PercPt> GetPercPtList(PercPt finder, out Returns ret)
        {
            List<PercPt> retList = new List<PercPt>();
            ret = Utils.InitReturns();

            try
            {
                retList = remoteObject.GetPercPtList(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return retList;
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
                MonitorLog.WriteLog("Ошибка GetPercPtList" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return retList;
            }
        }

        /// <summary>
        /// Удаление кассового плана
        /// </summary>
        /// <param name="subsKassPlan">Кассовый план с заполненным кодом и датой date_begin</param>
        /// <returns>результат</returns>
        public Returns DeleteKassPlan(FnSubsKassPlan subsKassPlan)
        {
            Returns ret = Utils.InitReturns();

            try
            {
                ret = remoteObject.DeleteKassPlan(subsKassPlan);
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
                else
                    ret.text = "Ошибка получения данных списка к перечислению";

                MonitorLog.WriteLog("Ошибка DeleteKassPlan \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return ret;
            }
        }


        /// <summary>
        /// Добавление нового кассового плана
        /// </summary>
        /// <param name="kassPlanFileString">файл кассового плана строкой</param>
        /// <param name="subsContract">Соглашение, заполнить nzp_contract</param>
        /// /// <param name="kassPlan">Добавленный кассовый план</param> 
        /// <returns>результат</returns>
        public Returns LoadKassPlan(string kassPlanFileString, FnSubsContract subsContract, out FnSubsKassPlan kassPlan)
        {
            Returns ret = Utils.InitReturns();

            try
            {
                ret = remoteObject.LoadKassPlan(kassPlanFileString, subsContract, out kassPlan);
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
                else
                    ret.text = "Ошибка получения данных списка к перечислению";

                MonitorLog.WriteLog("Ошибка LoadKassPlan \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                kassPlan = new FnSubsKassPlan();
                return ret;
            }
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

            try
            {
                ret = remoteObject.LoadMonthPlan(monthPlanFileString, subsContract, out monthPlan);
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
                else
                    ret.text = "Ошибка получения данных списка к перечислению";

                MonitorLog.WriteLog("Ошибка LoadKassPlan \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                monthPlan = new FnSubsMonthPlan();
                return ret;
            }
        }

        /// <summary>
        /// Удаление кассового плана
        /// </summary>
        /// <param name="subsKassPlan">Кассовый план с заполненным кодом и датой date_begin</param>
        /// <returns>результат</returns>
        public Returns DeleteMonthPlan(FnSubsMonthPlan subsMonthPlan)
        {
            Returns ret = Utils.InitReturns();

            try
            {
                ret = remoteObject.DeleteMonthPlan(subsMonthPlan);
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
                else
                    ret.text = "Ошибка получения данных списка к перечислению";

                MonitorLog.WriteLog("Ошибка DeleteMonthPlan \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return ret;
            }
        }


        /// <summary>
        /// Обновить, добавить, удалить данные
        /// в справочник Уровень платежей граждан
        /// </summary>
        /// <param name="finder">объект поиска</param>
        /// <returns>Измененный объект</returns>
        public PercPt UpdatePercPt(PercPt finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            PercPt perc = new PercPt();
            try
            {
                perc = remoteObject.UpdatePercPt(finder,out ret);
                HostChannel.CloseProxy(remoteObject);
                return perc;
            }
            catch (Exception ex)
            {
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else
                    ret.text = "Ошибка изменения уровня платежей граждан";

                MonitorLog.WriteLog("Ошибка UpdatePercPt \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return null;
            }
        }


        public List<FnSubsKassPlan> GetSubsKassPlan(FnSubsKassPlan finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<FnSubsKassPlan> retList = new List<FnSubsKassPlan>();
            try
            {
                retList = remoteObject.GetSubsKassPlan(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return retList;
            }
            catch (Exception ex)
            {
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else
                    ret.text = "Ошибка получения кассового плана";

                MonitorLog.WriteLog("Ошибка GetSubsKassPlan \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return null;
            }
        }

        /// <summary>
        /// получения помесячного распределения субсидий
        /// </summary>
        /// <param name="finder">поисковик</param>
        /// <returns>Помесячное распределение субсидий</returns>
        public FnSubsMonthPlan GetFnSubsMonthPlans(FnSubsMonthPlan finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            FnSubsMonthPlan retPlan = new FnSubsMonthPlan();
            try
            {
                retPlan = remoteObject.GetFnSubsMonthPlans(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return retPlan;
            }
            catch (Exception ex)
            {
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else
                    ret.text = "Ошибка получения помесячного распределения субсидий";

                MonitorLog.WriteLog("Ошибка GetFnSubsMonthPlans \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return null;
            }
        }

        public List<FnSubsAct> LoadSubsAct(FnSubsActForSearch finder, out Returns ret)
        {
            try
            {
                List<FnSubsAct> list = remoteObject.LoadSubsAct(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return list;
            }
            catch (Exception ex)
            {
                ret = Utils.InitReturns();
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка LoadSubsAct " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public Returns LoadTehHarGilFond(string fileName,FnSubsContract subsContract)
        {
            Returns ret = Utils.InitReturns();

            try
            {
                ret = remoteObject.LoadTehHarGilFond(fileName, subsContract);
                HostChannel.CloseProxy(remoteObject);
                return ret;
            }
            catch (Exception ex)
            {
                ret = Utils.InitReturns();
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка LoadTehHarGilFond " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }

        public List<FnSubsTehHarGilFond> GetHarGilFondList(FnSubsTehHarGilFond finder, out Returns ret)
        {
            try
            {
                List<FnSubsTehHarGilFond> list = remoteObject.GetHarGilFondList(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return list;
            }
            catch (Exception ex)
            {
                ret = Utils.InitReturns();
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка GetHarGilFondList " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public FnSubsTehHarGilFond UpdateHarGilFond(FnSubsTehHarGilFond finder, out Returns ret)
        {
            try
            {
                FnSubsTehHarGilFond retHarGilFond = remoteObject.UpdateHarGilFond(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return retHarGilFond;
            }
            catch (Exception ex)
            {
                ret = Utils.InitReturns();
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка UpdateHarGilFond " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public ReturnsType LoadAktSubsidy_rust(string filename, FnSubsActForSearch finder)
        {
            try
            {
                ReturnsType ret = remoteObject.LoadAktSubsidy_rust(filename, finder);
                HostChannel.CloseProxy(remoteObject);
                return ret;
            }
            catch (Exception ex)
            {
                ReturnsType ret = new ReturnsType(false);

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка LoadAktSubsidy_rust" + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }

        public Returns DeleteSubsAct(FnSubsActForSearch finder)
        {
            Returns ret = Utils.InitReturns();

            try
            {
                ret = remoteObject.DeleteSubsAct(finder);
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
                else
                    ret.text = "Ошибка удаления акта о фактической поставке";

                MonitorLog.WriteLog("Ошибка DeleteSubsAct \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return ret;
            }
        }

        public Returns UseActOfSupply(FnSubsActForSearch finder)
        {
            Returns ret = Utils.InitReturns();

            try
            {
                ret = remoteObject.UseActOfSupply(finder);
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
                else
                    ret.text = "Ошибка";

                MonitorLog.WriteLog("Ошибка UseActOfSupply \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return ret;
            }
        }

        public Returns UseTehHarGilFond(FnSubsTehHarGilFond finder)
        {
            Returns ret = Utils.InitReturns();

            try
            {
                ret = remoteObject.UseTehHarGilFond(finder);
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
                else
                    ret.text = "Ошибка ";

                MonitorLog.WriteLog("Ошибка UseTehHarGilFond \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return ret;
            }
        }

        public Returns GetCountContractFromTables(FnAgreement finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            Returns result = Utils.InitReturns();

            try
            {
                result = remoteObject.GetCountContractFromTables(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return result;
            }
            catch (Exception ex)
            {
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else
                    ret.text = "Ошибка ";

                MonitorLog.WriteLog("Ошибка GetCountContractFromTables \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                return result;
            }
        }
    }

}
