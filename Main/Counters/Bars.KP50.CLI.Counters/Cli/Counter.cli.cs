using System;
using System.Collections.Generic;
using System.Data;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.DataBase;
using System.ServiceModel;

namespace STCLINE.KP50.Client
{
    public class cli_Counter : cli_Base, I_Counter  //реализация клиента сервиса Кассы
    {
        List<Counter> cntSpis = new List<Counter>();



        ICounterRemoteObject getRemoteObject()
        {
            return getRemoteObject<ICounterRemoteObject>(WCFParams.AdresWcfWeb.srvCounter);
        }

        public cli_Counter(int nzp_server)
            : base(nzp_server)
        {

        }


        //public cli_Counter(int nzp_server)
        //    : base()
        //{
        //    _cli_Counter(nzp_server);
        //}

        //void _cli_Counter(int nzp_server)
        //{
        //    if (Points.IsMultiHost && nzp_server > 0)
        //    {
        //        //определить параметры доступа
        //        _RServer zap = MultiHost.GetServer(nzp_server);
        //        //                remoteObject = HostChannel.CreateInstance<I_Counter>(zap.login, zap.pwd, zap.ip_adr + WCFParams.srvCounter);
        //        remoteObject = HostChannel.CreateInstance<I_Counter>(zap.login, zap.pwd, zap.ip_adr + WCFParams.AdresWcfWeb.srvCounter);
        //    }
        //    else
        //    {
        //        //по-умолчанию
        //        //                remoteObject = HostChannel.CreateInstance<I_Counter>(WCFParams.Adres + WCFParams.srvCounter);
        //        remoteObject = HostChannel.CreateInstance<I_Counter>(WCFParams.AdresWcfWeb.Adres + WCFParams.AdresWcfWeb.srvCounter);
        //    }
        //}

        //~cli_Counter()
        //{
        //    try { if (remoteObject != null) HostChannel.CloseProxy(remoteObject); }
        //    catch { }
        //}

        //----------------------------------------------------------------------
        public List<Counter> GetPu(Counter finder, enSrvOper oper, out Returns ret)
        //----------------------------------------------------------------------
        {
            try
            {
                using (var remoteObject = getRemoteObject())
                {
                    cntSpis = remoteObject.GetPu(finder, oper, out ret);
                }
                return cntSpis;
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret = new Returns(false, Constants.access_error, Constants.access_code);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return null;
        }
        //----------------------------------------------------------------------
        public List<CounterOrd> GetCountersOrd(CounterOrd finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            List<CounterOrd> cOrd = null;
            try
            {
                using (var remoteObject = getRemoteObject())
                {
                    cOrd = remoteObject.GetCountersOrd(finder, out ret);
                    return cOrd;
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret = new Returns(false, Constants.access_error, Constants.access_code);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return cOrd;
        }
        //----------------------------------------------------------------------
        //public List<CounterVal> GetCountersVals(CounterVal finder, enSrvOper oper, out Returns ret)
        ////----------------------------------------------------------------------
        //{
        //    List<CounterVal> cVal = null;

        //    try
        //    {
        //        using (var remoteObject = getRemoteObject())
        //        {
        //            cVal = remoteObject.GetCountersVals(finder, oper, out ret);
        //        }
        //    }
        //    catch (CommunicationObjectFaultedException ex)
        //    {
        //        ret = new Returns(false, Constants.access_error, Constants.access_code);
        //        MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
        //    }
        //    catch (Exception ex)
        //    {
        //        ret = new Returns(false, ex.Message);
        //        MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
        //    }

        //    return cVal;



        //    //ret = Utils.InitReturns();
        //    //try
        //    //{
        //    //    List<CounterVal> cVal = remoteObject.GetCountersVals(finder, oper, out ret);
        //    //    HostChannel.CloseProxy(remoteObject);
        //    //    return cVal;
        //    //}
        //    //catch (Exception ex)
        //    //{
        //    //    if (ex is System.ServiceModel.EndpointNotFoundException)
        //    //    {
        //    //        ret.text = Constants.access_error;
        //    //        ret.tag = Constants.access_code;
        //    //    }
        //    //    else
        //    //    {
        //    //        ret.result = false;
        //    //        ret.result = false;
        //    //        ret.text = ex.Message;
        //    //    }

        //    //    string err = "";
        //    //    if (Constants.Viewerror) err = " \n " + ex.Message;

        //    //    MonitorLog.WriteLog("Ошибка GetCountersVal() " + err, MonitorLog.typelog.Error, 2, 100, true);
        //    //    return null;
        //    //}
        //}
        ////----------------------------------------------------------------------
        //public Returns SaveCountersVals(List<CounterVal> newVals, enSrvOper oper)
        ////----------------------------------------------------------------------
        //{
        //    Returns ret;
        //    try
        //    {
        //        using (var remoteObject = getRemoteObject())
        //        {
        //            ret = remoteObject.SaveCountersVals(newVals, oper);
        //        }
        //    }
        //    catch (CommunicationObjectFaultedException ex)
        //    {
        //        ret = new Returns(false, Constants.access_error, Constants.access_code);
        //        MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
        //    }
        //    catch (Exception ex)
        //    {
        //        ret = new Returns(false, ex.Message);
        //        MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
        //    }
        //    return ret;
        //}
        //----------------------------------------------------------------------
        public Returns SaveCounter(Counter newCounter, string dat_calc)
        //----------------------------------------------------------------------
        {
            Returns ret;
            try
            {
                using (var remoteObject = getRemoteObject())
                {
                    ret = remoteObject.SaveCounter(newCounter, dat_calc);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret = new Returns(false, Constants.access_error, Constants.access_code);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }
        //----------------------------------------------------------------------
        public Returns FindCountersOrd(CounterOrd finder)
        //----------------------------------------------------------------------
        {
            Returns ret;
            try
            {
                using (var remoteObject = getRemoteObject())
                {
                    ret = remoteObject.FindCountersOrd(finder);
                }
                return ret;
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret = new Returns(false, Constants.access_error, Constants.access_code);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }
        //----------------------------------------------------------------------
        public Counter LoadCounter(Counter finder, enSrvOper oper, out Returns ret)
        //----------------------------------------------------------------------
        {
            try
            {
                using (var remoteObject = getRemoteObject())
                {
                    Counter counter = remoteObject.LoadCounter(finder, oper, out ret);
                    return counter;
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret = new Returns(false, Constants.access_error, Constants.access_code);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return null;
        }
        //----------------------------------------------------------------------
        public Returns SaveCountersOrd(List<CounterOrd> finder)
        //----------------------------------------------------------------------
        {
            Returns ret;
            try
            {
                using (var remoteObject = getRemoteObject())
                {
                    ret = remoteObject.SaveCountersOrd(finder);
                }
                return ret;
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret = new Returns(false, Constants.access_error, Constants.access_code);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }
        //----------------------------------------------------------------------
        public List<CounterCnttypeLight> LoadCntType(Counter finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            try
            {
                using (var remoteObject = getRemoteObject())
                {
                    List<CounterCnttypeLight> list = remoteObject.LoadCntType(finder, out ret);
                    return list;
                }

            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret = new Returns(false, Constants.access_error, Constants.access_code);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return null;
        }
        //----------------------------------------------------------------------
        /// <summary>
        /// Получение списка закрытых ПУ, которые могут быть заменены текущим ПУ
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public List<ReplacedCounter> LoadPuForReplacing(Counter finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            try
            {
                using (var remoteObject = getRemoteObject())
                {
                    List<ReplacedCounter> list = remoteObject.LoadPuForReplacing(finder, out ret);
                    return list;
                }

            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret = new Returns(false, Constants.access_error, Constants.access_code);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return new List<ReplacedCounter>();
        }
        //----------------------------------------------------------------------
        public Returns SaveCntType(CounterCnttype finder)
        //----------------------------------------------------------------------
        {
            Returns ret;
            try
            {
                using (var remoteObject = getRemoteObject())
                {
                    ret = remoteObject.SaveCntType(finder);
                }
                return ret;
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret = new Returns(false, Constants.access_error, Constants.access_code);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }


        public List<Ls> GetLsGroupCounter(Counter finder, enSrvOper oper, out Returns ret)
        {
            try
            {
                using (var remoteObject = getRemoteObject())
                {
                    List<Ls> list = remoteObject.GetLsGroupCounter(finder, oper, out ret);
                    return list;
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret = new Returns(false, Constants.access_error, Constants.access_code);
                MonitorLog.WriteLog(
                    "Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message,
                    MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog(
                    "Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message,
                    MonitorLog.typelog.Error, 2, 100, true);
            }
            return null;
        }


        public Returns ChangeLsForGroupCnt(Counter finder, List<int> list_nzp_kvar, string dat_calc, enSrvOper oper)
        {
            Returns ret;
            try
            {
                using (var remoteObject = getRemoteObject())
                {
                    ret = remoteObject.ChangeLsForGroupCnt(finder, list_nzp_kvar, dat_calc, oper);
                }
                return ret;
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret = new Returns(false, Constants.access_error, Constants.access_code);
                MonitorLog.WriteLog(
                    "Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message,
                    MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog(
                    "Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message,
                    MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }


        public Returns UnlockCounter(CounterVal finder)
        //----------------------------------------------------------------------
        {
            Returns ret;
            try
            {
                using (var remoteObject = getRemoteObject())
                {
                    ret = remoteObject.UnlockCounter(finder);
                }
                return ret;
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret = new Returns(false, Constants.access_error, Constants.access_code);
                MonitorLog.WriteLog(
                    "Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message,
                    MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog(
                    "Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message,
                    MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }


        public List<Counter> DbGetPu(Counter finder, out Returns ret)
        {
            List<Counter> list;
            if (Points.IsMultiHost)
            {
                //вызвать сервис
                finder.nzp_wp = 0; //банк обнулим, чтобы при мультихостинге искалось во всех локальных банках, потом что-нить придумаем
                list = GetPu(finder, enSrvOper.SrvGet, out ret);
            }
            else
            {
                DbCounterClient db = new DbCounterClient();
                list = db.GetPu(finder, out ret);
                db.Close();
                return list;
            }
            return list;
        }



        public Returns DeleteCounter(Counter finder)
        //----------------------------------------------------------------------
        {
            Returns ret;
            try
            {
                using (var remoteObject = getRemoteObject())
                {
                    ret = remoteObject.DeleteCounter(finder);
                }
                return ret;

            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret = new Returns(false, Constants.access_error, Constants.access_code);
                MonitorLog.WriteLog(
                    "Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message,
                    MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog(
                    "Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message,
                    MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public string DbMakeWhereString(Counter finder, out Returns ret, enDopFindType tip)
        {
            DbCounterClient db = new DbCounterClient();
            string s = db.MakeWhereString(finder, out ret, tip);
            db.Close();
            return s;
        }

        public Returns DbUploadCounterFile(Finder finder, DataTable dt)
        {
            DbCounterClient db = new DbCounterClient();
            Returns ret = db.UploadCounterFile(finder, dt);
            db.Close();
            return ret;
        }

        public static DataTable DbGetUploadedCounterFile(Finder finder, out Returns ret)
        {
            DbCounterClient db = new DbCounterClient();
            DataTable dt = db.GetUploadedCounterFile(finder, out ret);
            db.Close();
            return dt;
        }

        public Returns SaveUploadedCounterReadings(Finder finder)
        {
            Returns ret;
            try
            {
                using (var remoteObject = getRemoteObject())
                {
                    ret = remoteObject.SaveUploadedCounterReadings(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret = new Returns(false, Constants.access_error, Constants.access_code);
                MonitorLog.WriteLog(
                    "Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message,
                    MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog(
                    "Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message,
                    MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        //----------------------------------------------------------------------
        public List<Group> GetAllLocalLsGroup(Group finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            try
            {
                using (var remoteObject = getRemoteObject())
                {
                    List<Group> cGroup = remoteObject.GetAllLocalLsGroup(finder, out ret);
                    return cGroup;
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret = new Returns(false, Constants.access_error, Constants.access_code);
                MonitorLog.WriteLog(
                    "Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message,
                    MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog(
                    "Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message,
                    MonitorLog.typelog.Error, 2, 100, true);
            }
            return null;
        }
        //----------------------------------------------------------------------

        //----------------------------------------------------------------------
        public Returns PrepareReportPuData(CounterVal finder, List<Dom> houseList)
        //----------------------------------------------------------------------
        {
            Returns ret;
            try
            {
                using (var remoteObject = getRemoteObject())
                {
                    ret = remoteObject.PrepareReportPuData(finder, houseList);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret = new Returns(false, Constants.access_error, Constants.access_code);
                MonitorLog.WriteLog(
                    "Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message,
                    MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog(
                    "Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message,
                    MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }


        public DataTable PrepareReportRashodPu(CounterVal finder, List<Dom> houseList, out Returns ret)
        {
            DataTable dt = null;
            var stopW = new System.Diagnostics.Stopwatch();
            try
            {
                using (var remoteObject = getRemoteObject())
                {
                    stopW.Start();
                    dt = remoteObject.PrepareReportRashodPu(finder, houseList, out ret);
                    stopW.Stop();
                }
                return dt;
            }
            catch (CommunicationObjectFaultedException ex)
            {
                if (stopW.Elapsed > new TimeSpan(0, 0, 0, 25))
                {
                    ret = new Returns(false, "Слишком большой объем данных для отчета, ограничьте параметрами выборку лицевых счетов", -1);
                }
                else
                {
                    ret = new Returns(false, Constants.access_error, Constants.access_code);
                    MonitorLog.WriteLog(
                        "Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message,
                        MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog(
                    "Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message,
                    MonitorLog.typelog.Error, 2, 100, true);
            }
            return dt;
        }


        //public List<string> CheckSaveCounterVals(List<CounterVal> newVals, out Returns ret)
        //{

        //    List<string> errList = null;

        //    try
        //    {
        //        using (var remoteObject = getRemoteObject())
        //        {
        //            errList = remoteObject.CheckSaveCounterVals(newVals, out ret);
        //        }
        //    }
        //    catch (CommunicationObjectFaultedException ex)
        //    {
        //        ret = new Returns(false, Constants.access_error, Constants.access_code);
        //        MonitorLog.WriteLog(
        //            "Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message,
        //            MonitorLog.typelog.Error, 2, 100, true);
        //    }
        //    catch (Exception ex)
        //    {
        //        ret = new Returns(false, ex.Message);
        //        MonitorLog.WriteLog(
        //            "Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message,
        //            MonitorLog.typelog.Error, 2, 100, true);
        //    }
        //    return errList;
        //}

        public Returns CalcForPU(Ls finder, int p_year, int p_month)
        {
            Returns ret;
            try
            {
                using (var remoteObject = getRemoteObject())
                {
                    ret = remoteObject.CalcForPU(finder, p_year, p_month);
                }
                return ret;
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret = new Returns(false, Constants.access_error, Constants.access_code);
                MonitorLog.WriteLog(
                    "Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message,
                    MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog(
                    "Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message,
                    MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }




        public static Returns DbUpdateOneUploadedCounterReading(int recordID, int nzp_counter, int nzp_user)
        {
            DbCounterClient db = new DbCounterClient();
            Returns ret = db.UpdateOneUploadedCounterReading(recordID, nzp_counter, nzp_user);
            db.Close();
            return ret;
        }

        //Список для быстрого ввода показаний ПУ
        public List<Ls> LoadForFastPu(Ls finder, enSrvOper oper, out Returns ret)
        {

            List<Ls> ls = null;
            try
            {
                using (var remoteObject = getRemoteObject())
                {
                    ls = remoteObject.LoadForFastPu(finder, oper, out ret);
                }
                return ls;
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret = new Returns(false, Constants.access_error, Constants.access_code);
                MonitorLog.WriteLog(
                    "Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message,
                    MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog(
                    "Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message,
                    MonitorLog.typelog.Error, 2, 100, true);
            }
            return ls;
        }

        //public List<CounterVal> GetPuData(Ls finder, out Returns ret)
        //{
        //    List<CounterVal> counter = null;
        //    //Counter counter = null;
        //    try
        //    {
        //        using (var remoteObject = getRemoteObject())
        //        {
        //            counter = remoteObject.GetPuData(finder, out ret);
        //        }
        //        return counter;
        //    }
        //    catch (CommunicationObjectFaultedException ex)
        //    {
        //        ret = new Returns(false, Constants.access_error, Constants.access_code);
        //        MonitorLog.WriteLog(
        //            "Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message,
        //            MonitorLog.typelog.Error, 2, 100, true);
        //    }
        //    catch (Exception ex)
        //    {
        //        ret = new Returns(false, ex.Message);
        //        MonitorLog.WriteLog(
        //            "Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message,
        //            MonitorLog.typelog.Error, 2, 100, true);
        //    }
        //    return counter;
        //}

        //public Returns CopyCounterReadingToRealBank(List<CounterVal> finder)
        //{
        //    Returns ret;
        //    try
        //    {
        //        using (var remoteObject = getRemoteObject())
        //        {
        //            ret = remoteObject.CopyCounterReadingToRealBank(finder);
        //        }
        //    }
        //    catch (CommunicationObjectFaultedException ex)
        //    {
        //        ret = new Returns(false, Constants.access_error, Constants.access_code);
        //        MonitorLog.WriteLog(
        //            "Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message,
        //            MonitorLog.typelog.Error, 2, 100, true);
        //    }
        //    catch (Exception ex)
        //    {
        //        ret = new Returns(false, ex.Message);
        //        MonitorLog.WriteLog(
        //            "Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message,
        //            MonitorLog.typelog.Error, 2, 100, true);
        //    }

        //    return ret;
        //}

        public Returns CopyCounterValueToRealBank(List<CounterValLight> newVals)
        {
            Returns ret;
            try
            {
                using (var remoteObject = getRemoteObject())
                {
                    ret = remoteObject.CopyCounterValueToRealBank(newVals);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret = new Returns(false, Constants.access_error, Constants.access_code);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public Returns DeleteCounterVal(CounterValLight val)
        {
            Returns ret;
            try
            {
                using (var remoteObject = getRemoteObject())
                {
                    ret = remoteObject.DeleteCounterVal(val);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret = new Returns(false, Constants.access_error, Constants.access_code);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public Returns SaveCounterListVals(List<CounterValLight> newVals)
        {
            Returns ret;
            try
            {
                using (var remoteObject = getRemoteObject())
                {
                    ret = remoteObject.SaveCounterListVals(newVals);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret = new Returns(false, Constants.access_error, Constants.access_code);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public Returns SaveCountersValsLight(List<CounterValLight> newVals)
        {
            Returns ret;
            try
            {
                using (var remoteObject = getRemoteObject())
                {
                    ret = remoteObject.SaveCountersValsLight(newVals);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret = new Returns(false, Constants.access_error, Constants.access_code);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public List<CounterReading> GetLsPuData(Ls finder, out Returns ret)
        {
            List<CounterReading> cVal = null;

            try
            {
                using (var remoteObject = getRemoteObject())
                {
                    cVal = remoteObject.GetLsPuData(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret = new Returns(false, Constants.access_error, Constants.access_code);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }

            return cVal;
        }

        public List<CounterValLight> GetCountersUserVals(CounterValLight finder, out Returns ret)
        {
            List<CounterValLight> cVal = null;

            try
            {
                using (var remoteObject = getRemoteObject())
                {
                    cVal = remoteObject.GetCountersUserVals(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret = new Returns(false, Constants.access_error, Constants.access_code);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }

            return cVal;
        }

        //public Returns PrintCounterValBlank(CounterVal finder)
        //{
        //    Returns ret;
        //    try
        //    {
        //        using (var remoteObject = getRemoteObject())
        //        {
        //            ret = remoteObject.PrintCounterValBlank(finder);
        //        }
        //    }
        //    catch (CommunicationObjectFaultedException ex)
        //    {
        //        ret = new Returns(false, Constants.access_error, Constants.access_code);
        //        MonitorLog.WriteLog(
        //            "Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message,
        //            MonitorLog.typelog.Error, 2, 100, true);
        //    }
        //    catch (Exception ex)
        //    {
        //        ret = new Returns(false, ex.Message);
        //        MonitorLog.WriteLog(
        //            "Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message,
        //            MonitorLog.typelog.Error, 2, 100, true);
        //    }

        //    return ret;
        //}

        public List<CounterValLight> GetCountersValsForView(CounterValLight finder, out Returns ret)
        {
            List<CounterValLight> cVal = null;

            try
            {
                using (var remoteObject = getRemoteObject())
                {
                    cVal = remoteObject.GetCountersValsForView(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret = new Returns(false, Constants.access_error, Constants.access_code);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }

            return cVal;
        }

        public List<CounterValLight> GetCountersValsForEdit(CounterValLight finder, out Returns ret)
        {
            List<CounterValLight> cVal = null;

            try
            {
                using (var remoteObject = getRemoteObject())
                {
                    cVal = remoteObject.GetCountersValsForEdit(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret = new Returns(false, Constants.access_error, Constants.access_code);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }

            return cVal;
        }

        public Returns PrintCounterValBlankLight(CounterValLight finder)
        {
            Returns ret;
            try
            {
                using (var remoteObject = getRemoteObject())
                {
                    ret = remoteObject.PrintCounterValBlankLight(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret = new Returns(false, Constants.access_error, Constants.access_code);
                MonitorLog.WriteLog(
                    "Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message,
                    MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog(
                    "Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message,
                    MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public Returns GetRashodIPU(Counter finder, out string rashod_k_opl)
        {
            Returns ret;
            rashod_k_opl = "";
            try
            {
                using (var remoteObject = getRemoteObject())
                {
                    ret = remoteObject.GetRashodIPU(finder, out rashod_k_opl);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret = new Returns(false, Constants.access_error, Constants.access_code);
                MonitorLog.WriteLog(
                    "Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message,
                    MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog(
                    "Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message,
                    MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }

        public List<CounterReading> GetPuListVals(CounterValLight finder, enSrvOper oper, out Returns ret)
        {
            List<CounterReading> list = null;

            try
            {
                using (var remoteObject = getRemoteObject())
                {
                    list = remoteObject.GetPuListVals(finder, oper, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret = new Returns(false, Constants.access_error, Constants.access_code);
                MonitorLog.WriteLog(
                    "Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message,
                    MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog(
                    "Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message,
                    MonitorLog.typelog.Error, 2, 100, true);
            }

            return list;
        }


        public List<CounterBounds> GetCounterBoundses(CounterBounds finder, out Returns ret)
        {
            List<CounterBounds> list = null;

            try
            {
                using (var remoteObject = getRemoteObject())
                {
                    list = remoteObject.GetCounterBoundses(finder, out ret);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret = new Returns(false, Constants.access_error, Constants.access_code);
                MonitorLog.WriteLog(
                    "Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message,
                    MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog(
                    "Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message,
                    MonitorLog.typelog.Error, 2, 100, true);
            }

            return list;
        }

        public Returns SaveCounterBounds(CounterBounds finder)
        {
            Returns ret;
            try
            {
                using (var remoteObject = getRemoteObject())
                {
                    ret = remoteObject.SaveCounterBounds(finder);
                }
            }
            catch (CommunicationObjectFaultedException ex)
            {
                ret = new Returns(false, Constants.access_error, Constants.access_code);
                MonitorLog.WriteLog(
                    "Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message,
                    MonitorLog.typelog.Error, 2, 100, true);
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog(
                    "Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message,
                    MonitorLog.typelog.Error, 2, 100, true);
            }

            return ret;
        }


        public Returns PrepareDataForGenerationLsPu(Finder finder, List<Counter> listCounters, List<puRooms> selectedRooms)
        {
            Returns ret = Utils.InitReturns();
            using (DbCounterClient db = new DbCounterClient())
            {
                ret = db.PrepareDataForGenerationLsPu(finder, listCounters, selectedRooms);
            }
            return ret;
        }

    }
}
