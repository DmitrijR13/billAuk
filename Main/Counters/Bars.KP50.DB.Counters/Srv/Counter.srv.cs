using System;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Net;
using System.Security.Principal;

using System.Collections.Generic;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Global;
using STCLINE.KP50.Client;
using System.Data;
using System.IO;

namespace STCLINE.KP50.Server
{
    //----------------------------------------------------------------------
    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class srv_Counter : srv_Base, I_Counter //сервис ПУ
    //----------------------------------------------------------------------
    {
        List<Counter> cntSpis = new List<Counter>();

        //----------------------------------------------------------------------
        public List<Counter> GetPu(Counter finder, enSrvOper oper, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            cntSpis.Clear();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Counter cli = new cli_Counter(WCFParams.AdresWcfHost.CurT_Server);
                cntSpis = cli.GetPu(finder, oper, out ret);
            }
            else
            {
                using (DbCounter db = new DbCounter())
                {
                    try
                    {
                        switch (oper)
                        {
                            case enSrvOper.SrvFind:
                                db.FindPu(finder, out ret);
                                if (ret.result)
                                    cntSpis = db.GetPu(finder, out ret);
                                else
                                    cntSpis = null;
                                break;
                            case enSrvOper.SrvGet:
                                cntSpis = db.GetPu(finder, out ret);
                                break;
                            //case enSrvOper.SrvFindVal:
                            //    cntSpis = db.FindVal(finder, out ret);
                            //    break;
                            case enSrvOper.SrvLoadCntTypeUchet:
                                cntSpis = db.LoadCntTypeUchet(finder, out ret);
                                break;
                            case enSrvOper.SrvLoad:
                                cntSpis = db.FindIPU(finder, out ret);
                                break;
                            default:
                                ret = new Returns(false, "Неверное наименование операции");
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        ret.result = false;
                        ret.text = "Ошибка вызова функции получения ПУ";
                        if (Constants.Viewerror) ret.text += " : " + ex.Message;
                        if (Constants.Debug)
                            MonitorLog.WriteLog("Ошибка GetPu() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100,
                                true);
                    }
                }
            }
            return cntSpis;
        }
        //----------------------------------------------------------------------
        public List<CounterCnttypeLight> LoadCntType(Counter finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<CounterCnttypeLight> list = null;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Counter cli = new cli_Counter(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.LoadCntType(finder, out ret);
            }
            else
            {
                using (DbCounter db = new DbCounter())
                {
                    try
                    {
                        list = db.LoadCntType(finder, out ret);
                    }
                    catch (Exception ex)
                    {
                        ret.result = false;
                        ret.text = "Ошибка вызова функции получения типов ПУ";
                        if (Constants.Viewerror) ret.text += " : " + ex.Message;
                        if (Constants.Debug)
                            MonitorLog.WriteLog("Ошибка LoadCntType() \n  " + ex.Message, MonitorLog.typelog.Error, 2,
                                100, true);
                    }
                }
            }
            return list;
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
            ret = Utils.InitReturns();
            var list = new List<ReplacedCounter>();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Counter cli = new cli_Counter(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.LoadPuForReplacing(finder, out ret);
            }
            else
            {
                using (DbCounter db = new DbCounter())
                {
                    try
                    {
                        list = db.LoadPuForReplacing(finder, out ret);
                    }
                    catch (Exception ex)
                    {
                        ret.result = false;
                        ret.text = "Ошибка вызова функции получения списка закрытых ПУ, которые могут быть заменены текущим ПУ";
                        if (Constants.Viewerror) ret.text += " : " + ex.Message;
                        if (Constants.Debug)
                            MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n  " + 
                                ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    }
                }
            }
            return list;
        }
        //----------------------------------------------------------------------
        public Returns SaveCntType(CounterCnttype finder)
        //----------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();
            using (DbCounter db = new DbCounter())
            {
                try
                {
                    ret = db.SaveCntType(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug)
                        MonitorLog.WriteLog("Ошибка SaveCntType() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100,
                            true);
                }
            }
            return ret;
        }
        //----------------------------------------------------------------------
        public List<CounterOrd> GetCountersOrd(CounterOrd finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<CounterOrd> list = null;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Counter cli = new cli_Counter(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.GetCountersOrd(finder, out ret);
            }
            else
            {
                using (DbCounter db = new DbCounter())
                {
                    try
                    {
                        list = db.GetCountersOrd(finder, out ret);
                    }
                    catch (Exception ex)
                    {
                        ret.result = false;
                        ret.text = "Ошибка вызова функции";
                        if (Constants.Viewerror) ret.text += " : " + ex.Message;
                        if (Constants.Debug)
                            MonitorLog.WriteLog("Ошибка GetCountersOrd() \n  " + ex.Message, MonitorLog.typelog.Error, 2,
                                100, true);
                    }
                }
            }
            return list;
        }
//        //----------------------------------------------------------------------
//#warning функции, работающие с counters, counters_dom, counters_group через counters_vals. Не использовать
//        public List<CounterVal> GetCountersVals(CounterVal finder, enSrvOper oper, out Returns ret)
//        //----------------------------------------------------------------------
//        {
//            ret = Utils.InitReturns();
//            List<CounterVal> list = null;

//            if (SrvRun.isBroker)
//            {
//                //надо продублировать вызов к внутреннему хосту
//                cli_Counter cli = new cli_Counter(WCFParams.AdresWcfHost.CurT_Server);
//                list = cli.GetCountersVals(finder, oper, out ret);
//            }
//            else
//            {

//                using (DbCounter db = new DbCounter())
//                {
//                    try
//                    {
//                        switch (oper)
//                        {
//                            case enSrvOper.SrvFind:
//                                list = db.GetCountersVals(finder, out ret);
//                                break;
//                            case enSrvOper.SrvFindLastCntVal:
//                                db.FindLastCntVal(finder, out ret);
//                                if (ret.result) list = db.GetLastCntVal(finder, out ret);
//                                else list = null;
//                                break;
//                            case enSrvOper.SrvGetLastCntVal:
//                                list = db.GetLastCntVal(finder, out ret);
//                                break;
//                            case enSrvOper.SrvFindUserVals:
//                                list = db.GetCountersUserVals(finder, out ret);
//                                break;
//                            case enSrvOper.SrvGetOdpuRashod:
//                                list = db.GetOdpuRashod(finder, out ret);
//                                break;
//                            default:
//                                ret = new Returns(false, "Неверное наименование операции");
//                                break;
//                        }
//                    }
//                    catch (Exception ex)
//                    {
//                        ret.result = false;
//                        ret.text = "Ошибка вызова функции";
//                        if (Constants.Viewerror) ret.text += " : " + ex.Message;
//                        if (Constants.Debug)
//                            MonitorLog.WriteLog("Ошибка GetCountersVals() \n  " + ex.Message, MonitorLog.typelog.Error,
//                                2, 100, true);
//                    }
//                }
//            }
//            return list;
//        }
        //----------------------------------------------------------------------
        public Returns FindCountersOrd(CounterOrd finder)
        //----------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();
            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Counter cli = new cli_Counter(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.FindCountersOrd(finder);
            }
            else
            {
                using (DbCounter db = new DbCounter())
                {
                    try
                    {
                        ret = db.FindCountersOrd(finder);
                    }
                    catch (Exception ex)
                    {
                        ret.result = false;
                        ret.text = "Ошибка вызова функции";
                        if (Constants.Viewerror) ret.text += " : " + ex.Message;
                        if (Constants.Debug)
                            MonitorLog.WriteLog("Ошибка FindCountersOrd() \n  " + ex.Message, MonitorLog.typelog.Error,
                                2, 100, true);
                    }
                }
            }
            return ret;
        }
        //----------------------------------------------------------------------
        public Returns SaveCountersOrd(List<CounterOrd> finder)
        //----------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();
            using (DbCounter db = new DbCounter())
            {
                try
                {
                    ret = db.SaveCountersOrd(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug)
                        MonitorLog.WriteLog("Ошибка SaveCountersOrd() \n  " + ex.Message, MonitorLog.typelog.Error, 2,
                            100, true);
                }
            }
            return ret;
        }
        //----------------------------------------------------------------------
//#warning функции, работающие с counters, counters_dom, counters_group через counters_vals. Не использовать
//        public Returns SaveCountersVals(List<CounterVal> newVals, enSrvOper oper)
//        //----------------------------------------------------------------------
//        {
//            Returns ret = Utils.InitReturns();
//            using (DbCounter db = new DbCounter())
//            {
//                try
//                {
//                    switch (oper)
//                    {
//                        case enSrvOper.srvSave:
//                            ret = db.SaveCountersVals(newVals);
//                            break;
//                        case enSrvOper.srvSaveCountersCurrVals:
//                            ret = db.SaveCountersCurrVals(newVals);
//                            break;

//                        default:
//                            ret = new Returns(false, "Неверное наименование операции");
//                            break;
//                    }
//                }
//                catch (Exception ex)
//                {
//                    ret.result = false;
//                    ret.text = "Ошибка вызова функции";
//                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
//                    if (Constants.Debug)
//                        MonitorLog.WriteLog("Ошибка SaveCountersVals() \n  " + ex.Message, MonitorLog.typelog.Error, 2,
//                            100, true);
//                }
//            }
//            return ret;
//        }
        //----------------------------------------------------------------------
        public Returns SaveCounter(Counter newCounter, string dat_calc)
        //----------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();
            using (DbCounter db = new DbCounter())
            {
                try
                {
                    ret = db.SaveCounter(newCounter, dat_calc);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции сохранения ПУ";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug)
                        MonitorLog.WriteLog("Ошибка SaveCounter() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100,
                            true);
                }
            }
            return ret;
        }
        //----------------------------------------------------------------------
        public Counter LoadCounter(Counter finder, enSrvOper oper, out Returns ret)
        //----------------------------------------------------------------------
        {
            Counter cnt = null;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Counter cli = new cli_Counter(WCFParams.AdresWcfHost.CurT_Server);
                cnt = cli.LoadCounter(finder, oper, out ret);
            }
            else
            {
                using (DbCounter db = new DbCounter())
                {
                    try
                    {
                        switch (oper)
                        {
                            case enSrvOper.SrvLoad:
                                cnt = db.LoadCounter(finder, out ret);
                                break;
                            case enSrvOper.SrvGetMaxDatUchet:
                                cnt = db.FindMaxDatUchet(finder, out ret);
                                break;
                            default:
                                ret = new Returns(false, "Неверное наименование операции");
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        ret = new Returns(false, "Ошибка LoadCounter(" + oper.ToString() + ")");
                        if (Constants.Viewerror) ret.text += ": " + ex.Message;

                        if (Constants.Debug)
                            MonitorLog.WriteLog("Ошибка LoadCounter(" + oper.ToString() + ") " + ex.Message,
                                MonitorLog.typelog.Error, 2, 100, true);
                    }
                }
            }
            return cnt;
        }
        //----------------------------------------------------------------------
        public List<Ls> GetLsGroupCounter(Counter finder, enSrvOper oper, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<Ls> list = null;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Counter cli = new cli_Counter(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.GetLsGroupCounter(finder, oper, out ret);
            }
            else
            {
                using (DbCounter db = new DbCounter())
                {
                    try
                    {
                        switch (oper)
                        {
                            case enSrvOper.SrvGetLsGroupCounter:
                                list = db.GetLsGroupCounter(finder, out ret);
                                break;
                            case enSrvOper.SrvGetLsDomNotGroupCnt:
                                list = db.GetLsDomNotGroupCnt(finder, out ret);
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
                        if (Constants.Debug)
                            MonitorLog.WriteLog("Ошибка GetLsGroupCounter() \n  " + ex.Message, MonitorLog.typelog.Error,
                                2, 100, true);
                    }
                }
            }
            return list;
        }
        //----------------------------------------------------------------------
        public Returns ChangeLsForGroupCnt(Counter finder, List<int> list_nzp_kvar, string dat_calc, enSrvOper oper)
        //----------------------------------------------------------------------
        {
            Returns ret;
            using (DbCounter db = new DbCounter())
            {
                try
                {
                    switch (oper)
                    {
                        case enSrvOper.SrvAddLsForGroupCnt:
                            ret = db.AddLsForGroupCnt(finder, list_nzp_kvar, dat_calc);
                            break;
                        case enSrvOper.SrvDelLsFromGroupCnt:
                            ret = db.DelLsFromGroupCnt(finder, list_nzp_kvar, dat_calc);
                            break;
                        default:
                            ret = new Returns(false, "Неверное наименование операции");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    ret = new Returns(false, "Ошибка вызова функции");
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug)
                        MonitorLog.WriteLog("Ошибка ChangeLsForGroupCnt() \n  " + ex.Message, MonitorLog.typelog.Error,
                            2, 100, true);
                }
            }
            return ret;
        }
        //----------------------------------------------------------------------
        public Returns UnlockCounter(CounterVal finder)
        //----------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();
            using (DbCounter db = new DbCounter())
            {
                try
                {
                    ret = db.UnlockCounter(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug)
                        MonitorLog.WriteLog("Ошибка UnlockCounter() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100,
                            true);
                }
            }
            return ret;
        }
        //----------------------------------------------------------------------
        public Returns DeleteCounter(Counter finder)
        //----------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();
            using (DbCounter db = new DbCounter())
            {
                try
                {
                    ret = db.DeleteCounter(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += ": " + ex.Message;
                    if (Constants.Debug)
                        MonitorLog.WriteLog("Ошибка DeleteCounter()\n " + ex.Message, MonitorLog.typelog.Error, 2, 100,
                            true);
                }
            }
            return ret;
        }

        public Returns SaveUploadedCounterReadings(Finder finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                if (finder.dopFind != null && finder.dopFind.Count > 0 && finder.dopFind[0] == "correlate")
                {
                    using (DbCounter db = new DbCounter())
                    {
                        ret = db.CorrelateUploadedCounterReadings(finder);
                    }
                }
                else
                {
                    using (DbCounter db2 = new DbCounter())
                    {
                        db2.DemoEndInvoke(finder);
                        ret = new Returns(true,
                            "Задание отправлено на выполнение. Протокол загрузки доступен через пункт меню \"Мои файлы\"",
                            -1);
                    }
                }
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка вызова функции";
                if (Constants.Viewerror) ret.text += ": " + ex.Message;
                if (Constants.Debug) MonitorLog.WriteLog("Ошибка SaveUploadedCounterReadings()\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        //----------------------------------------------------------------------
        public List<Group> GetAllLocalLsGroup(Group finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<Group> list = null;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Counter cli = new cli_Counter(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.GetAllLocalLsGroup(finder, out ret);
            }
            else
            {
                using (DbCounter db = new DbCounter())
                {
                    try
                    {
                        list = db.GetAllLocalLsGroup(finder, out ret);
                    }
                    catch (Exception ex)
                    {
                        ret.result = false;
                        ret.text = "Ошибка вызова функции";
                        if (Constants.Viewerror) ret.text += " : " + ex.Message;
                        if (Constants.Debug)
                            MonitorLog.WriteLog("Ошибка GetAllLocalLsGroup() \n  " + ex.Message,
                                MonitorLog.typelog.Error, 2, 100, true);
                    }
                }
            }
            return list;
        }
        //----------------------------------------------------------------------

        //----------------------------------------------------------------------
        public Returns PrepareReportPuData(CounterVal finder, List<Dom> houseList)
        //----------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Counter cli = new cli_Counter(WCFParams.AdresWcfHost.CurT_Server);
                return cli.PrepareReportPuData(finder, houseList);
            }
            else
            {
                using (DbCounter db = new DbCounter())
                {
                    try
                    {
                        return db.PrepareReportPuData(finder, houseList);
                    }
                    catch (Exception ex)
                    {
                        ret.result = false;
                        ret.text = "Ошибка вызова функции";
                        if (Constants.Viewerror) ret.text += " : " + ex.Message;
                        if (Constants.Debug)
                            MonitorLog.WriteLog("Ошибка PrepareReportPuData() serv \n  " + ex.Message,
                                MonitorLog.typelog.Error, 2, 100, true);
                    }
                }
                return ret;
            }
        }

        //----------------------------------------------------------------------
        public DataTable PrepareReportRashodPu(CounterVal finder, List<Dom> houseList, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            DataTable dt = null;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Counter cli = new cli_Counter(WCFParams.AdresWcfHost.CurT_Server);
                dt = cli.PrepareReportRashodPu(finder, houseList, out ret);
            }
            else
            {
                using (DbCounter db = new DbCounter())
                {
                    try
                    {
                        dt = db.PrepareReportRashodPu(finder, houseList, out ret);
                    }
                    catch (Exception ex)
                    {
                        ret.result = false;
                        ret.text = "Ошибка вызова функции";
                        if (Constants.Viewerror) ret.text += " : " + ex.Message;
                        if (Constants.Debug)
                            MonitorLog.WriteLog("Ошибка PrepareReportRashodPu() \n  " + ex.Message,
                                MonitorLog.typelog.Error, 2, 100, true);
                    }
                }
            }
            return dt;
        }

        ////----------------------------------------------------------------------
        //public List<string> CheckSaveCounterVals(List<CounterVal> newVals, out Returns ret)
        ////----------------------------------------------------------------------
        //{
        //    ret = Utils.InitReturns();
        //    List<string> err = null;
        //    using (DbCounter db = new DbCounter())
        //    {
        //        try
        //        {
        //            err = db.CheckSaveCounterVals(newVals, out ret);
        //        }
        //        catch (Exception ex)
        //        {
        //            ret.result = false;
        //            ret.text = "Ошибка вызова функции проверки существования показаний ПУ";
        //            if (Constants.Viewerror) ret.text += " : " + ex.Message;
        //            if (Constants.Debug)
        //                MonitorLog.WriteLog("Ошибка CheckSaveCounterVals() \n  " + ex.Message, MonitorLog.typelog.Error,
        //                    2, 100, true);
        //        }
        //    }
        //    return err;
        //}

        public Returns CalcForPU(Ls finder, int p_year, int p_month)
        {
            Returns ret = Utils.InitReturns();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Counter cli = new cli_Counter(WCFParams.AdresWcfHost.CurT_Server);
                cli.CalcForPU(finder, p_year, p_month);
            }
            else
            {
                using (DbCalcCharge db = new DbCalcCharge())
                {
                    try
                    {
                        db.Call_GenSrZnKPU(finder, p_year, p_month, out ret);
                    }
                    catch (Exception ex)
                    {
                        ret.result = false;
                        ret.text = "Ошибка вызова функции";
                        if (Constants.Viewerror) ret.text += " : " + ex.Message;
                        if (Constants.Debug)
                            MonitorLog.WriteLog("Ошибка CalcForPU() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100,
                                true);
                    }
                }
            }
            return ret;
        }


        public List<Ls> LoadForFastPu(Ls finder, enSrvOper oper, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Ls> ls = new List<Ls>();
            if (SrvRunProgramRole.IsBroker)
            {
                cli_Counter cli = new cli_Counter(WCFParams.AdresWcfWeb.CurT_Server);

                ls = cli.LoadForFastPu(finder, oper, out ret);
            }
            else
            {
                try
                {
                    using (DbCounter dbcounter = new DbCounter())
                    {
                        switch (oper)
                        {
                            case enSrvOper.SrvFind:
                                ret = dbcounter.LoadForFastPu(finder);
                                if (ret.result) ls = dbcounter.GetForFastPu(finder, out ret);
                                else ls = null;
                                break;
                            case enSrvOper.SrvGet:
                                ls = dbcounter.GetForFastPu(finder, out ret);
                                break;
                            default:
                                ret = new Returns(false, "Неверное наименование операции");
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {

                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки данных л/с";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LoadForFastP()\n " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }

            }
            return ls;

        }
//#warning функции, работающие с counters, counters_dom, counters_group через counters_vals. Не использовать
//        public List<CounterVal> GetPuData(Ls finder, out Returns ret)
//        {
//            ret = Utils.InitReturns();
//            //  List<Ls> ls = new List<Ls>();
//            List<CounterVal> counter = new List<CounterVal>();
//            if (SrvRun.isBroker)
//            {
//                cli_Counter cli = new cli_Counter(WCFParams.AdresWcfWeb.CurT_Server);

//                counter = cli.GetPuData(finder, out ret);
//            }
//            else
//            {
//                try
//                {
//                    using (DbCounter dbcounter = new DbCounter())
//                    {
//                        //  ls = dbcounter.LoadForFastPu(finder, out ret);
//                        //   ls = dbcounter.LoadForFastPu(finder, out ret);
//                        counter = dbcounter.GetPuData(finder, out ret);
//                    }
//                }
//                catch (Exception ex)
//                {

//                    ret.result = false;
//                    ret.text = "Ошибка вызова функции загрузки данных л/с";
//                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
//                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetPuData()\n " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
//                }

//            }
//            return counter;
//        }
//#warning функции, работающие с counters, counters_dom, counters_group через counters_vals. Не использовать
//        public Returns CopyCounterReadingToRealBank(List<CounterVal> finder)
//        {
//            Returns ret = Utils.InitReturns();

//            if (SrvRun.isBroker)
//            {
//                //надо продублировать вызов к внутреннему хосту
//                cli_Counter cli = new cli_Counter(WCFParams.AdresWcfHost.CurT_Server);
//                ret = cli.CopyCounterReadingToRealBank(finder);
//            }
//            else
//            {
//                using (DbCounter db = new DbCounter())
//                {
//                    try
//                    {
//                        ret = db.CopyCounterReadingToRealBank(finder);
//                    }
//                    catch (Exception ex)
//                    {
//                        ret.result = false;
//                        ret.text = "Ошибка вызова функции утверждения введенных показаний";
//                        if (Constants.Viewerror) ret.text += " : " + ex.Message;
//                        if (Constants.Debug)
//                            MonitorLog.WriteLog("Ошибка CopyCounterReadingToRealBank" + "\n" + ex.Message,
//                                MonitorLog.typelog.Error, 2, 100, true);
//                    }
//                }
//            }
//            return ret;
//        }

//#warning функции, работающие с counters, counters_dom, counters_group через counters_vals. Не использовать
//        public Returns PrintCounterValBlank(CounterVal finder)
//        {
//            Returns ret = Utils.InitReturns();

//            if (SrvRun.isBroker)
//            {
//                //надо продублировать вызов к внутреннему хосту
//                cli_Counter cli = new cli_Counter(WCFParams.AdresWcfHost.CurT_Server);
//                ret = cli.PrintCounterValBlank(finder);
//            }
//            else
//            {
//                using (DbCounter db = new DbCounter())
//                {
//                    try
//                    {
//                        ret = db.PrintCounterValBlank(finder);
//                    }
//                    catch (Exception ex)
//                    {
//                        ret.result = false;
//                        ret.text = "Ошибка вызова функции печати бланка регистрации показаний";
//                        if (Constants.Viewerror) ret.text += " : " + ex.Message;
//                        if (Constants.Debug)
//                            MonitorLog.WriteLog("Ошибка PrintCounterValBlank" + "\n" + ex.Message,
//                                MonitorLog.typelog.Error, 2, 100, true);
//                    }
//                }
//            }
//            return ret;
//        }

        // новые
        public Returns CopyCounterValueToRealBank(List<CounterValLight> newVals)
        {
            Returns ret = new Returns();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Counter cli = new cli_Counter(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.CopyCounterValueToRealBank(newVals);
            }
            else
            {
                using (DbCounter db = new DbCounter())
                {
                    try
                    {
                        ret = db.CopyCounterValueToRealBank(newVals);
                    }
                    catch (Exception ex)
                    {
                        ret.result = false;
                        ret.text = "Ошибка вызова функции";
                        if (Constants.Viewerror) ret.text += " : " + ex.Message;
                        if (Constants.Debug)
                            MonitorLog.WriteLog("Ошибка SaveCountersValsLight() \n  " + ex.Message,
                                MonitorLog.typelog.Error, 2, 100, true);
                    }
                }
            }
            return ret;
        }

        public Returns DeleteCounterVal(CounterValLight val)
        {
            Returns ret = new Returns();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Counter cli = new cli_Counter(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.DeleteCounterVal(val);
            }
            else
            {
                using (DbCounter db = new DbCounter())
                {
                    try
                    {
                        ret = db.DeleteCounterVal(val);
                    }
                    catch (Exception ex)
                    {
                        ret.result = false;
                        ret.text = "Ошибка вызова функции";
                        if (Constants.Viewerror) ret.text += " : " + ex.Message;
                        if (Constants.Debug)
                            MonitorLog.WriteLog("Ошибка SaveCountersValsLight() \n  " + ex.Message,
                                MonitorLog.typelog.Error, 2, 100, true);
                    }
                }
            }
            return ret;
        }

        public Returns SaveCounterListVals(List<CounterValLight> newVals)
        {
            Returns ret = new Returns();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Counter cli = new cli_Counter(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.SaveCounterListVals(newVals);
            }
            else
            {
                using (DbCounter db = new DbCounter())
                {
                    try
                    {
                        ret = db.SaveCounterListVals(newVals);
                    }
                    catch (Exception ex)
                    {
                        ret.result = false;
                        ret.text = "Ошибка вызова функции";
                        if (Constants.Viewerror) ret.text += " : " + ex.Message;
                        if (Constants.Debug)
                            MonitorLog.WriteLog("Ошибка SaveCounterCurrentValsLight() \n  " + ex.Message,
                                MonitorLog.typelog.Error, 2, 100, true);
                    }
                }
            }
            return ret;
        }

        public Returns SaveCountersValsLight(List<CounterValLight> newVals)
        {
            Returns ret = new Returns();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Counter cli = new cli_Counter(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.SaveCountersValsLight(newVals);
            }
            else
            {
                using (DbCounter db = new DbCounter())
                {
                    try
                    {
                        ret = db.SaveCountersValsLight(newVals);
                    }
                    catch (Exception ex)
                    {
                        ret.result = false;
                        ret.text = "Ошибка вызова функции";
                        if (Constants.Viewerror) ret.text += " : " + ex.Message;
                        if (Constants.Debug)
                            MonitorLog.WriteLog("Ошибка SaveCountersValsLight() \n  " + ex.Message,
                                MonitorLog.typelog.Error, 2, 100, true);
                    }
                }
            }
            return ret;
        }

        public List<CounterValLight> GetCountersUserVals(CounterValLight finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<CounterValLight> list = null;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Counter cli = new cli_Counter(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.GetCountersUserVals(finder, out ret);
            }
            else
            {
                using (DbCounter db = new DbCounter())
                {
                    try
                    {
                        list = db.GetCountersUserVals(finder, out ret);
                    }
                    catch (Exception ex)
                    {
                        ret.result = false;
                        ret.text = "Ошибка вызова функции";
                        if (Constants.Viewerror) ret.text += " : " + ex.Message;
                        if (Constants.Debug)
                            MonitorLog.WriteLog("Ошибка GetCountersUserVals() \n  " + ex.Message,
                                MonitorLog.typelog.Error, 2, 100, true);
                    }
                }
            }
            return list;
        }

        public List<CounterReading> GetLsPuData(Ls finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<CounterReading> list = null;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Counter cli = new cli_Counter(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.GetLsPuData(finder, out ret);
            }
            else
            {

                using (DbCounter db = new DbCounter())
                {
                    try
                    {
                        list = db.GetLsPuData(finder, out ret);
                    }
                    catch (Exception ex)
                    {
                        ret.result = false;
                        ret.text = "Ошибка вызова функции";
                        if (Constants.Viewerror) ret.text += " : " + ex.Message;
                        if (Constants.Debug)
                            MonitorLog.WriteLog("Ошибка GetLsPuData() \n  " + ex.Message, MonitorLog.typelog.Error, 2,
                                100, true);
                    }
                }
            }
            return list;
        }

        public List<CounterValLight> GetCountersValsForView(CounterValLight finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<CounterValLight> list = null;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Counter cli = new cli_Counter(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.GetCountersValsForView(finder, out ret);
            }
            else
            {
                using (DbCounter db = new DbCounter())
                {
                    try
                    {
                        list = db.GetCountersValsForView(finder, out ret);
                    }
                    catch (Exception ex)
                    {
                        ret.result = false;
                        ret.text = "Ошибка вызова функции";
                        if (Constants.Viewerror) ret.text += " : " + ex.Message;
                        if (Constants.Debug)
                            MonitorLog.WriteLog("Ошибка GetCountersValsForView() \n  " + ex.Message,
                                MonitorLog.typelog.Error, 2, 100, true);
                    }
                }
            }
            return list;
        }

        public List<CounterValLight> GetCountersValsForEdit(CounterValLight finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<CounterValLight> list = null;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Counter cli = new cli_Counter(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.GetCountersValsForEdit(finder, out ret);
            }
            else
            {
                using (DbCounter db = new DbCounter())
                {
                    try
                    {
                        list = db.GetCountersValsForEdit(finder, out ret);
                    }
                    catch (Exception ex)
                    {
                        ret.result = false;
                        ret.text = "Ошибка вызова функции";
                        if (Constants.Viewerror) ret.text += " : " + ex.Message;
                        if (Constants.Debug)
                            MonitorLog.WriteLog("Ошибка GetCountersValsForEdit() \n  " + ex.Message,
                                MonitorLog.typelog.Error, 2, 100, true);
                    }
                }
            }
            return list;
        }

        public Returns PrintCounterValBlankLight(CounterValLight finder)
        {
            Returns ret = Utils.InitReturns();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Counter cli = new cli_Counter(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.PrintCounterValBlankLight(finder);
            }
            else
            {
                using (DbCounter db = new DbCounter())
                {
                    try
                    {
                        ret = db.PrintCounterValBlankLight(finder);
                    }
                    catch (Exception ex)
                    {
                        ret.result = false;
                        ret.text = "Ошибка вызова функции печати бланка регистрации показаний";
                        if (Constants.Viewerror) ret.text += " : " + ex.Message;
                        if (Constants.Debug)
                            MonitorLog.WriteLog("Ошибка PrintCounterValBlank" + "\n" + ex.Message,
                                MonitorLog.typelog.Error, 2, 100, true);
                    }
                }
            }
            return ret;
        }

        public Returns GetRashodIPU(Counter finder, out string rashod_k_opl)
        {
            rashod_k_opl = "";
            Returns ret = Utils.InitReturns();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Counter cli = new cli_Counter(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetRashodIPU(finder, out rashod_k_opl);
            }
            else
            {
                using (DbCounter db = new DbCounter())
                {
                    try
                    {
                        ret = db.GetRashodIPU(finder, out rashod_k_opl);
                    }
                    catch (Exception ex)
                    {
                        ret.result = false;
                        ret.text = "Ошибка вызова функции печати бланка регистрации показаний";
                        if (Constants.Viewerror) ret.text += " : " + ex.Message;
                        if (Constants.Debug)
                            MonitorLog.WriteLog("Ошибка PrintCounterValBlank" + "\n" + ex.Message,
                                MonitorLog.typelog.Error, 2, 100, true);
                    }
                }
            }
            return ret;
        }

        public List<CounterReading> GetPuListVals(CounterValLight finder, enSrvOper oper, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<CounterReading> list = null;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Counter cli = new cli_Counter(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.GetPuListVals(finder, oper, out ret);
            }
            else
            {
                using (DbCounter db = new DbCounter())
                {
                    try
                    {
                        switch (oper)
                        {
                            case enSrvOper.SrvFind:
                                ret = db.FindPuList(finder);
                                if (ret.result) list = db.GetPuListVals(finder, out ret);
                                else list = null;
                                break;
                            case enSrvOper.SrvGet:
                                list = db.GetPuListVals(finder, out ret);
                                break;
                            case enSrvOper.SrvGetOdpuRashod:
                                list = db.GetOdpuRashod(finder, out ret);
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
                        if (Constants.Debug)
                            MonitorLog.WriteLog("Ошибка GetPuListVals() \n  " + ex.Message, MonitorLog.typelog.Error, 2,
                                100, true);
                    }
                }
            }
            return list;
        }

        public List<CounterBounds> GetCounterBoundses(CounterBounds finder, out Returns ret)
        {
            ret = new Returns();
            var res = new List<CounterBounds>();
            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Counter cli = new cli_Counter(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.GetCounterBoundses(finder, out ret);
            }
            else
            {
                using (DbCounter db = new DbCounter())
                {
                    try
                    {
                        res = db.GetCounterBoundses(finder, out ret);
                    }
                    catch (Exception ex)
                    {
                        ret.result = false;
                        ret.text = "Ошибка вызова функции получения списка периодов для ПУ";
                        if (Constants.Viewerror) ret.text += " : " + ex.Message;
                        if (Constants.Debug)
                            MonitorLog.WriteLog("Ошибка GetCounterBoundses" + "\n" + ex.Message,
                                MonitorLog.typelog.Error, 2, 100, true);
                    }
                }
            }
            return res;
        }


        public Returns SaveCounterBounds(CounterBounds finder)
        {
            var ret = new Returns();
            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Counter cli = new cli_Counter(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.SaveCounterBounds(finder);
            }
            else
            {
                using (DbCounter db = new DbCounter())
                {
                    try
                    {
                        ret = db.SaveCounterBounds(finder);
                    }
                    catch (Exception ex)
                    {
                        ret.result = false;
                        ret.text = "Ошибка вызова функции сохранения периодов для ПУ";
                        if (Constants.Viewerror) ret.text += " : " + ex.Message;
                        if (Constants.Debug)
                            MonitorLog.WriteLog("Ошибка GetCounterBoundses" + "\n" + ex.Message,
                                MonitorLog.typelog.Error, 2, 100, true);
                    }
                }
            }
            return ret;
        }
    }

}
