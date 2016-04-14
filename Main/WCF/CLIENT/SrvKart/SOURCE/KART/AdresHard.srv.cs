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

namespace STCLINE.KP50.Server
{
    //----------------------------------------------------------------------
    public class srv_AdresHard : srv_Base, I_AdresHard
    {
        List<Ls> lsSpis = new List<Ls>();



        //----------------------------------------------------------------------
        public List<Ls> LoadLs(Ls finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<Ls> list = new List<Ls>();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_AdresHard cli = new cli_AdresHard(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.LoadLs(finder, out ret);
            }
            else
            {
                DbAdresHard db = new DbAdresHard();
                try
                {
                    list = db.LoadLs(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LoadCurrentLsGroup() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return list;
        }

        //----------------------------------------------------------------------
        public int UpdateDom(Dom finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            int res = Constants._ZERO_;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_AdresHard cli = new cli_AdresHard(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.UpdateDom(finder, out ret);
            }
            else
            {
                DbAdresHard db = new DbAdresHard();
                try
                {
                    res = db.Update(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции изменения дома";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка UpdateDom \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = Constants._ZERO_;
                }
                db.Close();
            }
            return res;
        }

        public List<SplitLsParams> ExecuteSplitLS(List<SplitLsParams> listPrm, List<Perekidka> listPerekidka, List<Kart> listGilec, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<SplitLsParams> res = null;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_AdresHard cli = new cli_AdresHard(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.ExecuteSplitLS(listPrm, listPerekidka, listGilec, out ret);
            }
            else
            {
                DbAdresHard db = new DbAdresHard();
                try
                {
                    res = db.ExecuteSplitLS(listPrm, listPerekidka, listGilec, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции разделения ЛС";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка ExecuteSplitLS \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = null;
                }
                db.Close();
            }
            return res;
        }

        //----------------------------------------------------------------------
        public int UpdateLs(Ls finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            int res = Constants._ZERO_;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_AdresHard cli = new cli_AdresHard(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.UpdateLs(finder, out ret);
            }
            else
            {
                DbAdresHard db = new DbAdresHard();
                try
                {
                    res = db.Update(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции изменения л/c";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка UpdateLs \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = Constants._ZERO_;
                }
                db.Close();
            }
            return res;
        }

        //----------------------------------------------------------------------
        public List<Ls> GetLs(Ls finder, enSrvOper srv, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            lsSpis.Clear();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_AdresHard cli = new cli_AdresHard(WCFParams.AdresWcfHost.CurT_Server);
                lsSpis = cli.GetLs(finder, srv, out ret);
            }
            else
            {
                DbAdresHard db = new DbAdresHard();
                try
                {
                    switch (srv)
                    {
                        case enSrvOper.SrvFind:
                        {
                            using (var db1 = new DbAdres())
                            {
                                using (var db2 = new DbAdresClient())
                                {
                                    db1.FindLs(finder, out ret);
                                    lsSpis = ret.result ? db2.GetLs(finder, out ret) : null;
                                }
                            }

                        }
                            break;
                        case enSrvOper.SrvGet:
                        {
                            using (var db2 = new DbAdresClient())
                            {
                                lsSpis = db2.GetLs(finder, out ret);
                            }
                        }
                            break;
                        case enSrvOper.SrvLoad:
                            lsSpis = db.LoadLs(finder, out ret);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции получения л/c";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetLs() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return lsSpis;
        }

        public List<Ls> GetLs2(Ls finder, Service servfinder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            lsSpis.Clear();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_AdresHard cli = new cli_AdresHard(WCFParams.AdresWcfHost.CurT_Server);
                lsSpis = cli.GetLs2(finder, servfinder, out ret);
            }
            else
            {
                DbAdresHard db = new DbAdresHard();
                try
                {
                    using (DbAdres db1 = new DbAdres())
                    {
                        using (DbAdresClient db2 = new DbAdresClient())
                        {
                            using (DbFindAddress db3 = new DbFindAddress())
                            {
                                db1.FindLs(finder, out ret);
                                if (servfinder != null)
                                    if (ret.result) ret = db3.FindLsForServ(finder, servfinder);

                                lsSpis = ret.result ? db2.GetLs(finder, out ret) : null;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции получения л/c";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetLs() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return lsSpis;
        }

        //----------------------------------------------------------------------
        public string GetFakturaName(Ls finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            string res = "";

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_AdresHard cli = new cli_AdresHard(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.GetFakturaName(finder, out ret);
            }
            else
            {
                DbAdresHard db = new DbAdresHard();
                try
                {
                    res = db.GetFakturaName(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetFakturaName() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = "";
                }
                db.Close();
            }
            return res;
        }

        //----------------------------------------------------------------------
        public List<MapObject> GetMapObjects(MapObject finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<MapObject> res = null;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_AdresHard cli = new cli_AdresHard(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.GetMapObjects(finder, out ret);
            }
            else
            {
                DbAdresHard db = new DbAdresHard();
                try
                {
                    res = db.GetMapObjects(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetMapObjects() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = null;
                }
                db.Close();
            }
            return res;
        }

        public List<_Area> LoadAreaPayer(Finder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<_Area> res = null;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_AdresHard cli = new cli_AdresHard(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.LoadAreaPayer(finder, out ret);
            }
            else
            {
                DbAdresHard db = new DbAdresHard();
                try
                {
                    res = db.LoadAreaPayer(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LoadAreaPayer() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = null;
                }
                db.Close();
            }
            return res;
        }

        //----------------------------------------------------------------------
        public List<_Area> GetArea(Finder finder, out Returns ret, out DateTime serverBeginTime, out DateTime serverEndTime)
        //----------------------------------------------------------------------
        {
            BeforeStartQuery("GetArea");

            serverBeginTime = DateTime.Now;

            ret = Utils.InitReturns();
            Areas spis = new Areas();
            spis.AreaList.Clear();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_AdresHard cli = new cli_AdresHard(WCFParams.AdresWcfHost.CurT_Server);
                spis.AreaList = cli.GetArea(finder, out ret);
            }
            else
            {
                DbAdresHard db = new DbAdresHard();
                try
                {
                    spis.AreaList = db.LoadArea(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции получения Управляющих организаций";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetArea() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }

            serverEndTime = DateTime.Now;

            AfterStopQuery();

            return spis.AreaList;
        }

        //----------------------------------------------------------------------
        public List<_Area> LoadAreaForKvar(Finder finder, out Returns ret)
            //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<_Area> result;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                var cli = new cli_AdresHard(WCFParams.AdresWcfHost.CurT_Server);
                result = cli.LoadAreaForKvar(finder, out ret);
            }
            else
            {
                var db = new DbAdresHard();
                result = new List<_Area>();
                try
                {
                    result = db.LoadAreaForKvar(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LoadAreaForKvar() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return result;
        }

        //----------------------------------------------------------------------
        public Returns UpdateLsInCache(Ls finder)
        //----------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_AdresHard cli = new cli_AdresHard(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.UpdateLsInCache(finder);
            }
            else
            {
                DbAdresHard db = new DbAdresHard();
                try
                {
                    ret = db.UpdateLsInCache(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка UpdateLsInCache() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public Returns SaveGeu(Geu finder)
        {
            Returns ret = Utils.InitReturns();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_AdresHard cli = new cli_AdresHard(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.SaveGeu(finder);
            }
            else
            {
                DbAdresHard db = new DbAdresHard();
                try
                {
                    ret = db.SaveGeu(finder);
                }
                catch (Exception ex)
                {
                    ret = new Returns(false);
                    ret.text = "Ошибка функции SaveGeu" + (Constants.Viewerror ? ": " + ex.Message : "");
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка SaveGeu()\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }


        public Returns SaveUlica(Ulica finder)
        {
            Returns ret = Utils.InitReturns();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_AdresHard cli = new cli_AdresHard(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.SaveUlica(finder);
            }
            else
            {
                DbAdresHard db = new DbAdresHard();
                try
                {
                    ret = db.SaveUlica(finder);
                }
                catch (Exception ex)
                {
                    ret = new Returns(false);
                    ret.text = "Ошибка функции SaveUlica" + (Constants.Viewerror ? ": " + ex.Message : "");
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка SaveUlica()\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public Returns GeneratePkodFon(Finder finder)
        {
            Returns ret = Utils.InitReturns();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_AdresHard cli = new cli_AdresHard(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GeneratePkodFon(finder);
            }
            else
            {
                DbAdresHard db = new DbAdresHard();
                try
                {
                    ret = db.GeneratePkodFonAddTask(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции присвоения платежного кода";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GeneratePkodFon" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public Returns GenerateLsPu(Ls finder, List<Counter> CounterList)
        {
            Returns ret;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_AdresHard cli = new cli_AdresHard(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GenerateLsPu(finder, CounterList);
            }
            else
            {
                DbAdresHard db = new DbAdresHard();
                try
                {
                    ret = db.GenerateLsPu(finder, CounterList);
                }
                catch (Exception ex)
                {
                    ret = new Returns(false, "Ошибка вызова функции группового добавления л/c");
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GenerateLsPu \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }
        //----------------------------------------------------------------------
        public Returns GenerateLsPu(List<Counter> CounterList)
        //----------------------------------------------------------------------
        {
            return GenerateLsPu(null, CounterList);
        }
        //----------------------------------------------------------------------
        public Returns GenerateLsPu(Ls finder)
        //----------------------------------------------------------------------
        {
            return GenerateLsPu(finder, null);
        }

        ////----------------------------------------------------------------------
        //public List<Ls> DbGetLs(Ls finder, out Returns ret)
        ////----------------------------------------------------------------------
        //{
        //    List<Ls> list = new List<Ls>();
        //    ret = Utils.InitReturns();
        //    if (Points.IsMultiHost)
        //    {
        //        //вызвать сервис
        //        list = GetLs(finder, enSrvOper.SrvGet, out ret);
        //    }
        //    else
        //    {
        //        //DbAdresClient db = new DbAdresClient();
        //        //list = db.GetLs(finder, out ret);


        //        DbAdresHard db = new DbAdresHard();
        //        list = db.GetLs(finder, out ret);
        //        db.Close();
        //    }
        //    return list;
        //}
    }
}
