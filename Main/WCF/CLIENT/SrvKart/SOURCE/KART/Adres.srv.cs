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

namespace STCLINE.KP50.Server
{
    /*
    //----------------------------------------------------------------------
    public class srv_HttpAdres : srv_Bapublic static string DbMakeWhereString(Service finder, out Returns ret)
        {
            return DbLsServices.MakeWhereString(finder, out ret);
        }se, I_HttpAdres //сервис аресов
    //----------------------------------------------------------------------
    {

        List<Ls> lsSpis = new List<Ls>();
        List<Dom> domSpis = new List<Dom>();
        List<Ulica> ulSpis = new List<Ulica>();

        //----------------------------------------------------------------------
        public string XMLData(string id)
        //----------------------------------------------------------------------
        {
            return "enter " + id;
        }
        //----------------------------------------------------------------------
        public string JSONData(string id)
        //----------------------------------------------------------------------
        {
            return "enter2 " + id;
        }
        //----------------------------------------------------------------------
        public string JSONData_1()
        //----------------------------------------------------------------------
        {
            return "Example_JsonData";
        }
    }
    */

    //----------------------------------------------------------------------
    public class srv_Adres : srv_Base, I_Adres //сервис аресов
    //----------------------------------------------------------------------
    {

        List<Ls> lsSpis = new List<Ls>();
        List<Dom> domSpis = new List<Dom>();
        List<Ulica> ulSpis = new List<Ulica>();

        //----------------------------------------------------------------------
        public int UpdateDom(Dom finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            int res = Constants._ZERO_;

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Adres cli = new cli_Adres(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.UpdateDom(finder, out ret);
            }
            else
            {
                DbAdres db = new DbAdres();
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
        //----------------------------------------------------------------------
        public int UpdateLs(Ls finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            int res = Constants._ZERO_;

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Adres cli = new cli_Adres(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.UpdateLs(finder, out ret);
            }
            else
            {
                DbAdres db = new DbAdres();
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

        public Returns GenerateLsPu(Ls finder, List<Counter> CounterList)
        {
            Returns ret;

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Adres cli = new cli_Adres(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GenerateLsPu(finder, CounterList);
            }
            else
            {
                DbAdres db = new DbAdres();
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
        public void UpdateGroupDom(Dom finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Adres cli = new cli_Adres(WCFParams.AdresWcfHost.CurT_Server);
                cli.UpdateGroupDom(finder, out ret);
            }
            else
            {
                DbAdres db = new DbAdres();
                try
                {
                    db.UpdateGroup(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции изменения домов групповая операция";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка UpdateDom \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
        }
        //----------------------------------------------------------------------
        public void UpdateGroupLs(Ls finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Adres cli = new cli_Adres(WCFParams.AdresWcfHost.CurT_Server);
                cli.UpdateGroupLs(finder, out ret);
            }
            else
            {
                DbAdres db = new DbAdres();
                try
                {
                    ret = db.UpdateGroup(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции изменения лицевых счетов групповая операция";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка UpdateLs \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
        }
        //----------------------------------------------------------------------
        public List<Ls> GetLs(Ls finder, enSrvOper srv, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            lsSpis.Clear();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Adres cli = new cli_Adres(WCFParams.AdresWcfHost.CurT_Server);
                lsSpis = cli.GetLs(finder, srv, out ret);
            }
            else
            {
                DbAdres db = new DbAdres();
                try
                {
                    switch (srv)
                    {
                        case enSrvOper.SrvFind:
                            db.FindLs(finder, out ret);
                            if (ret.result)
                                lsSpis = db.GetLs(finder, out ret);
                            else
                                lsSpis = null;
                            break;
                        case enSrvOper.SrvGet:
                            lsSpis = db.GetLs(finder, out ret);
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
        //----------------------------------------------------------------------
        public List<Dom> GetDom(Dom finder, enSrvOper srv, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            domSpis.Clear();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Adres cli = new cli_Adres(WCFParams.AdresWcfHost.CurT_Server);
                domSpis = cli.GetDom(finder, srv, out ret);
            }
            else
            {
                DbAdres db = new DbAdres();
                try
                {
                    switch (srv)
                    {
                        case enSrvOper.SrvFind:
                            db.FindDom(finder, out ret);
                            if (ret.result)
                                domSpis = db.GetDom(finder, out ret);
                            else
                                domSpis = null;
                            break;
                        default:
                            domSpis = db.GetDom(finder, out ret);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции получения домов";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetDom() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return domSpis;
        }
        //----------------------------------------------------------------------
        public List<Ulica> GetUlica(Dom finder, enSrvOper srv, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            ulSpis.Clear();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Adres cli = new cli_Adres(WCFParams.AdresWcfHost.CurT_Server);
                ulSpis = cli.GetUlica(finder, srv, out ret);
            }
            else
            {
                DbAdres db = new DbAdres();
                try
                {
                    switch (srv)
                    {
                        case enSrvOper.SrvLoad:
                            //ulSpis = db.LoadUlica(finder, out ret);
                            ReturnsObjectType<List<Ulica>> r = new ClassDB().RunSqlAction(finder, new DbAdres().LoadUlica);
                            ret = r.GetReturns();
                            ulSpis = r.returnsData;
#warning Ошибка если количестов улиц более 1000
                            if (ulSpis != null && ulSpis.Count > 1000) ulSpis.RemoveRange(1000, ulSpis.Count);
                            break;
                        case enSrvOper.SrvFind:
                            {
                                db.FindUlica(finder, out ret);
                                if (ret.result)
                                    ulSpis = db.GetUlica(finder, out ret);
                                else
                                    ulSpis = null;
                                break;
                            }
                        case enSrvOper.SrvGet:
                            ulSpis = db.GetUlica(finder, out ret);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции получения улиц";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetUlica() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ulSpis;
        }

        /// <summary>
        /// Возвращает список районов город по указанной базе данных и текущему региону
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public List<Town> GetTownList(Town finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            var result = new List<Town>();

            var pref = GetPrefer(out ret);

            finder.nzp_stat = pref.nzp_stat;

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                var cli = new cli_Adres(WCFParams.AdresWcfHost.CurT_Server);
                result = cli.GetTownList(finder, out ret);
            }
            else
            {
                DbAdres db = new DbAdres();
                try
                {
                    result = db.GetTownList(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции получения списка районов, городов";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug)
                        MonitorLog.WriteLog("Ошибка GetTownList() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100,
                            true);
                }
                finally
                {
                    db.Close();
                }
            }
            return result;
        }

        /// <summary>
        /// Возвращает список населенных пунктов по указанной базе данных и текущему району
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public List<Rajon> GetRajonList(Rajon finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            var result = new List<Rajon>();

            var pref = GetPrefer(out ret);

            finder.nzp_stat = pref.nzp_stat;

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                var cli = new cli_Adres(WCFParams.AdresWcfHost.CurT_Server);
                result = cli.GetRajonList(finder, out ret);
            }
            else
            {
                DbAdres db = new DbAdres();
                try
                {
                    result = db.GetRajonList(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции получения списка районов, городов";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug)
                        MonitorLog.WriteLog("Ошибка GetRajonList() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100,
                            true);
                }
                finally
                {
                    db.Close();
                }
            }
            return result;
        }

        //----------------------------------------------------------------------
        public GetSelectListDomInfo GetSelectListDomInfo(Finder finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            GetSelectListDomInfo info = new GetSelectListDomInfo();
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Adres cli = new cli_Adres(WCFParams.AdresWcfHost.CurT_Server);
                info = cli.GetSelectListDomInfo(finder, out ret);
            }
            else
            {
                DbAdres db = new DbAdres();
                try
                {
                    info = db.GetSelectListDomInfo(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции получения данных по выбранному списку домов";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetSelectListDomInfo \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }

            return info;
        }



        //----------------------------------------------------------------------
        public List<_Area> GetArea(Finder finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            Areas spis = new Areas();
            spis.AreaList.Clear();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Adres cli = new cli_Adres(WCFParams.AdresWcfHost.CurT_Server);
                spis.AreaList = cli.GetArea(finder, out ret);
            }
            else
            {
                DbAdres db = new DbAdres();
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
            return spis.AreaList;
        }
        //----------------------------------------------------------------------
        public List<_Geu> GetGeu(Finder finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            Geus spis = new Geus();
            spis.GeuList.Clear();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Adres cli = new cli_Adres(WCFParams.AdresWcfHost.CurT_Server);
                spis.GeuList = cli.GetGeu(finder, out ret);
            }
            else
            {
                DbAdres db = new DbAdres();
                try
                {
                    spis.GeuList = db.LoadGeu(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции получения ЖЭУ";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetGeu() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return spis.GeuList;
        }
        //----------------------------------------------------------------------
        public Dom FindDomFromPm(_Placemark placemark, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            Dom res = null;

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Adres cli = new cli_Adres(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.FindDomFromPm(placemark, out ret);
            }
            else
            {
                DbAdres db = new DbAdres();
                try
                {
                    res = db.FindDomFromPm(placemark, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка FindDomFromPm() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = null;
                }
                db.Close();
            }
            return res;
        }
        //----------------------------------------------------------------------
        public _Rekvizit GetLsRevizit(Ls finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            _Rekvizit res = null;

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Adres cli = new cli_Adres(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.GetLsRevizit(finder, out ret);
            }
            else
            {
                DbAdres db = new DbAdres();
                try
                {
                    res = db.GetLsRevizit(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetLsRevizit() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = null;
                }
                db.Close();
            }
            return res;
        }


        //----------------------------------------------------------------------
        public bool SaveLsRevizit(string pref, _Rekvizit uk, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            bool res = false;

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Adres cli = new cli_Adres(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.SaveLsRevizit(pref, uk, out ret);
            }
            else
            {
                DbAdres db = new DbAdres();
                try
                {
                    res = db.SaveLsRevizit(pref, uk, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка SaveLsRevizit() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = false;
                }
                db.Close();
            }
            return res;
        }
        //----------------------------------------------------------------------
        public string GetFakturaName(Ls finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            string res = "";

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Adres cli = new cli_Adres(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.GetFakturaName(finder, out ret);
            }
            else
            {
                DbAdres db = new DbAdres();
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
        public string GetKolGil(MonthLs finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            string res = null;

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Adres cli = new cli_Adres(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.GetKolGil(finder, out ret);
            }
            else
            {
                DbAdres db = new DbAdres();
                try
                {
                    res = db.GetKolGil(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetKolGil() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = null;
                }
                db.Close();
            }
            return res;
        }
        //----------------------------------------------------------------------
        public string GetMapKey(out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            string res = null;

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Adres cli = new cli_Adres(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.GetMapKey(out ret);
            }
            else
            {
                DbAdres db = new DbAdres();
                try { res = db.GetMapKey(out ret); }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetMapKey() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = null;
                }
                db.Close();
            }
            return res;
        }
        //----------------------------------------------------------------------
        public _Placemark GetDefaultPlacemark(out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            _Placemark res = new _Placemark();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Adres cli = new cli_Adres(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.GetDefaultPlacemark(out ret);
            }
            else
            {
                DbAdres db = new DbAdres();
                try
                {
                    res = db.GetDefaultPlacemark(out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetDefaultPlacemark() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = new _Placemark();
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

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Adres cli = new cli_Adres(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.GetMapObjects(finder, out ret);
            }
            else
            {
                DbAdres db = new DbAdres();
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
        //----------------------------------------------------------------------
        public bool SaveMapObjects(List<MapObject> mapObjects, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            bool res = false;

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Adres cli = new cli_Adres(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.SaveMapObjects(mapObjects, out ret);
            }
            else
            {
                DbAdres db = new DbAdres();
                try
                {
                    res = db.SaveMapObjects(mapObjects, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка SaveMapObjects() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = false;
                }
                db.Close();
            }
            return res;
        }
        //----------------------------------------------------------------------
        public bool DeleteMapObjects(MapObject finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            bool res = false;

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Adres cli = new cli_Adres(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.DeleteMapObjects(finder, out ret);
            }
            else
            {
                DbAdres db = new DbAdres();
                try
                {
                    res = db.DeleteMapObjects(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка DeleteMapObjects() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = false;
                }
                db.Close();
            }
            return res;
        }
        //----------------------------------------------------------------------
        public List<Group> GetListGroup(Group finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<Group> list = new List<Group>();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Adres cli = new cli_Adres(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.GetListGroup(finder, out ret);
            }
            else
            {
                DbAdres db = new DbAdres();
                try
                {
                    list = db.GetListGroup(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetListGroup() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return list;
        }
        //----------------------------------------------------------------------
        public List<Group> GetGroupLs(Group finder, enSrvOper srv, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<Group> res = null;

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Adres cli = new cli_Adres(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.GetGroupLs(finder, srv, out ret);
            }
            else
            {
                DbAdres db = new DbAdres();
                try
                {
                    if (srv == enSrvOper.SrvFind)
                        db.FindGroupLs(finder, out ret);
                    if (ret.result)
                        res = db.GetGroupLs(finder, out ret);
                    else
                        res = null;
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetGroupLs() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = null;
                }
                db.Close();
            }
            return res;
        }
        //----------------------------------------------------------------------
        public List<Group> LoadCurrentLsGroup(Group finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<Group> list = new List<Group>();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Adres cli = new cli_Adres(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.LoadCurrentLsGroup(finder, out ret);
            }
            else
            {
                DbAdres db = new DbAdres();
                try
                {
                    list = db.LoadCurrentLsGroup(finder, out ret);
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

        public List<Area_ls> LoadCurrentLsSupplier(Area_ls finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<Area_ls> list = new List<Area_ls>();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Adres cli = new cli_Adres(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.LoadCurrentLsSupplier(finder, out ret);
            }
            else
            {
                DbAdres db = new DbAdres();
                try
                {
                    list = db.LoadCurrentLsSupplier(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LoadCurrentLsSupplier() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return list;
        }

        public void DeleteSupplierLs(Area_ls finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Adres cli = new cli_Adres(WCFParams.AdresWcfHost.CurT_Server);
                DeleteSupplierLs(finder, out ret);
            }
            else
            {
                DbAdres db = new DbAdres();
                try
                {
                    db.DeleteSupplierLs(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка DeleteSupplierLs() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return;
        }

        public Area_ls LoadCurrentAliasLs(Area_ls finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            Area_ls alias = new Area_ls();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Adres cli = new cli_Adres(WCFParams.AdresWcfHost.CurT_Server);
                alias = cli.LoadCurrentAliasLs(finder, out ret);
            }
            else
            {
                DbAdres db = new DbAdres();
                try
                {
                    alias = db.LoadCurrentAliasLs(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LoadCurrentLsSupplier() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return alias;
        }

        public void SaveSupplierLs(Area_ls finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Adres cli = new cli_Adres(WCFParams.AdresWcfHost.CurT_Server);
                cli.SaveSupplierLs(finder, out ret);
            }
            else
            {
                DbAdres db = new DbAdres();
                try
                {
                    db.SaveSupplierLs(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка SaveSupplierLs() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return;
        }


        //----------------------------------------------------------------------
        public List<Ls> LoadLs(Ls finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<Ls> list = new List<Ls>();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Adres cli = new cli_Adres(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.LoadLs(finder, out ret);
            }
            else
            {
                DbAdres db = new DbAdres();
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
        public bool SaveLsGroup(Group finder, List<string> groupList, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            bool saveResult = false;

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Adres cli = new cli_Adres(WCFParams.AdresWcfHost.CurT_Server);
                saveResult = cli.SaveLsGroup(finder, groupList, out ret);
            }
            else
            {
                DbAdres db = new DbAdres();
                try
                {
                    saveResult = db.SaveLsGroup(finder, groupList, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка SaveLsGroup() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return saveResult;
        }
        //----------------------------------------------------------------------
        public Returns CreateNewGroup(Group finder)
        //----------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Adres cli = new cli_Adres(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.CreateNewGroup(finder);
            }
            else
            {
                DbAdres db = new DbAdres();
                try
                {
                    ret = db.CreateNewGroup(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка CreateNewGroup() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }
        //----------------------------------------------------------------------
        public List<Finder> GetPointsLs(Finder finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<Finder> res = null;

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Adres cli = new cli_Adres(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.GetPointsLs(finder, out ret);
            }
            else
            {
                DbAdres db = new DbAdres();
                try
                {
                    res = db.GetPointsLs(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetPointsLs() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = null;
                }
                db.Close();
            }
            return res;
        }

        //----------------------------------------------------------------------
        public List<Search_Info> GetSearchInfo(Ls finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<Search_Info> res = null;

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Adres cli = new cli_Adres(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.GetSearchInfo(finder, out ret);
            }
            else
            {
                DbAdres db = new DbAdres();
                try
                {
                    res = db.GetSearchInfo(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetSearchInfo() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = null;
                }
                db.Close();
            }
            return res;
        }

        //----------------------------------------------------------------------
        public Returns UpdateLsInCache(Ls finder)
        //----------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Adres cli = new cli_Adres(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.UpdateLsInCache(finder);
            }
            else
            {
                DbAdres db = new DbAdres();
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
        //----------------------------------------------------------------------
        //процедура генератора отчетов
        //----------------------------------------------------------------------
        public Returns Generator(List<Prm> listprm, int nzp_user)
        {
            Returns ret = Utils.InitReturns();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Adres cli = new cli_Adres(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.Generator(listprm, nzp_user);
            }
            else
            {
                DbAdres db = new DbAdres();
                ret = db.Generator(listprm, nzp_user);
                db.Close();
            }
            return ret;
        }
        //----------------------------------------------------------------------
        public Returns Generator2(List<int> listint, int nzp_user, int yy, int mm)
        //----------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Adres cli = new cli_Adres(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.Generator2(listint, nzp_user, yy, mm);
            }
            else
            {
                DbAdres db = new DbAdres();
                ret = db.Generator(listint, nzp_user, yy, mm);
                db.Close();
            }
            return ret;
        }
        //----------------------------------------------------------------------
        public List<_RajonDom> FindRajonDom(Finder finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<_RajonDom> list = null;

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Adres cli = new cli_Adres(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.FindRajonDom(finder, out ret);
            }
            else
            {
                DbAdres db = new DbAdres();
                try
                {
                    list = db.FindRajonDom(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции получения списка районов";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка FindRajonDom()\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return list;
        }
        //----------------------------------------------------------------------
        //Процедура генератора параметров-начислений
        //----------------------------------------------------------------------
        public bool GenerateSaldoAll(string conn_web, string conn_db, List<int> listint, int nzp_user, int yy, int mm, out Returns ret)
        {
            ret = Utils.InitReturns();
            bool res = false;

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                //cli_Adres cli = new cli_Adres();
                //res = cli.GenerateSaldoAll(conn_web, conn_db, listint, nzp_user, yy, mm, out ret);
            }
            else
            {
                DbAdres db = new DbAdres();
                try
                {
                    res = db.FindSaldoAll(conn_web, conn_db, listint, nzp_user, yy, mm, out ret);
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка создания таблицы t" + nzp_user + "_saldoall : " + ex.Message, MonitorLog.typelog.Error, true);
                    return false;
                }
            }

            return res;
        }

        public bool SaveListGroup(Group finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            bool saveResult = false;

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Adres cli = new cli_Adres(WCFParams.AdresWcfHost.CurT_Server);
                saveResult = cli.SaveListGroup(finder, out ret);
            }
            else
            {
                DbAdres db = new DbAdres();
                try
                {
                    saveResult = db.SaveListGroup(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка SaveListGroup() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return saveResult;
        }

        public Returns SaveArea(Area finder)
        {
            Returns ret = Utils.InitReturns();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Adres cli = new cli_Adres(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.SaveArea(finder);
            }
            else
            {
                DbAdres db = new DbAdres();
                try
                {
                    ret = db.SaveArea(finder);
                }
                catch (Exception ex)
                {
                    ret = new Returns(false);
                    ret.text = "Ошибка функции SaveArea" + (Constants.Viewerror ? ": " + ex.Message : "");
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка SaveArea()\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public Returns SaveGeu(Geu finder)
        {
            Returns ret = Utils.InitReturns();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Adres cli = new cli_Adres(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.SaveGeu(finder);
            }
            else
            {
                DbAdres db = new DbAdres();
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

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Adres cli = new cli_Adres(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.SaveUlica(finder);
            }
            else
            {
                DbAdres db = new DbAdres();
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

        //----------------------------------------------------------------------
        public List<Ulica> UlicaLoad(Ulica finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<Ulica> res = null;

            if (SrvRun.isBroker)
            {
                //надо вызвать дальше другой хост
                cli_Adres cli = new cli_Adres(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.UlicaLoad(finder, out ret);
            }
            else
            {
                DbAdres db = new DbAdres();
                try
                {
                    res = db.UlicaLoad(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка UlicaLoad() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = null;
                }
                db.Close();
            }
            return res;
        }

        //----------------------------------------------------------------------
        public Prefer GetPrefer(out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            Prefer res = null;

            if (SrvRun.isBroker)
            {
                //надо вызвать дальше другой хост
                cli_Adres cli = new cli_Adres(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.GetPrefer(out ret);
            }
            else
            {
                DbAdres db = new DbAdres();
                try
                {
                    res = db.GetPrefer(out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetPrefer() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = null;
                }
                db.Close();
            }
            return res;
        }

        public Returns MakeOperation(Finder finder, Operations oper)
        {
            Returns ret = Utils.InitReturns();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Adres cli = new cli_Adres(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.MakeOperation(finder, oper);
            }
            else
            {
                DbAdres db = new DbAdres();
                try
                {
                    ret = db.RefreshAP(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции обновления адресного пространства";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка MakeOperation(" + oper + ")\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        //----------------------------------------------------------------------
        public List<Ls> GetUniquePointAreaGeu(Ls finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<Ls> list = new List<Ls>();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Adres cli = new cli_Adres(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.GetUniquePointAreaGeu(finder, out ret);
            }
            else
            {
                DbAdres db = new DbAdres();
                try
                {
                    list = db.GetUniquePointAreaGeu(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetUniquePointAreaGeu() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return list;
        }

        public List<Vill> LoadVill(Vill finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Vill> list = new List<Vill>();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Adres cli = new cli_Adres(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.LoadVill(finder, out ret);
            }
            else
            {
                DbAdres db = new DbAdres();
                try
                {
                    list = db.LoadVill(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LoadVill() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return list;
        }

        public List<Rajon> LoadVillRajon(Rajon finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Rajon> list = new List<Rajon>();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Adres cli = new cli_Adres(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.LoadVillRajon(finder, out ret);
            }
            else
            {
                DbAdres db = new DbAdres();
                try
                {
                    list = db.LoadVillRajon(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LoadVillRajon() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return list;
        }

        public List<Rajon> LoadRajon(Rajon finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Rajon> list = new List<Rajon>();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Adres cli = new cli_Adres(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.LoadRajon(finder, out ret);
            }
            else
            {
                DbAdres db = new DbAdres();
                try
                {
                    list = db.LoadRajon(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LoadRajon() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return list;
        }

        public Returns SaveVillRajon(Rajon finder, List<Rajon> list_checked)
        {
            Returns ret = Utils.InitReturns();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Adres cli = new cli_Adres(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.SaveVillRajon(finder, list_checked);
            }
            else
            {
                DbAdres db = new DbAdres();
                try
                {
                    ret = db.SaveVillRajon(finder, list_checked);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции обновления адресного пространства";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка SaveVillRajon" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public Returns GeneratePkod()
        {
            Returns ret = Utils.InitReturns();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Adres cli = new cli_Adres(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GeneratePkod();
            }
            else
            {
                DbAdres db = new DbAdres();
                try
                {
                    ret = db.GeneratePkodToLs();
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции присвоения платежного кода";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GeneratePkod" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public Returns GeneratePkodFon(Finder finder)
        {
            Returns ret = Utils.InitReturns();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Adres cli = new cli_Adres(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GeneratePkodFon(finder);
            }
            else
            {
                DbAdres db = new DbAdres();
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

        public DataTable PrepareLsPuVipiska(Ls finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            DataTable dt = null;

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Adres cli = new cli_Adres(WCFParams.AdresWcfHost.CurT_Server);
                dt = cli.PrepareLsPuVipiska(finder, out ret);
            }
            else
            {
                DbAdres db = new DbAdres();
                try
                {
                    dt = db.PrepareLsPuVipiska(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции для получения данных для отчета 'Выписка из ЛС о поданных показаниях КПУ'";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка PrepareLsPuVipiska" + "\n" + ex.Message, MonitorLog.typelog.Error, 7, 100, true);
                }
                db.Close();
            }
            return dt;
        }

        public Returns UpdateSosLS(Ls finder)
        //----------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Adres cli = new cli_Adres(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.UpdateSosLS(finder);
            }
            else
            {
                DbAdres db = new DbAdres();
                try
                {
                    ret = db.UpdateSosLS(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка UpdateSosLS() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public DataTable PrepareGubCurrCharge(Charge finder, int reportId, out Returns ret)
        {
            ret = Utils.InitReturns();
            DataTable dt = null;

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Adres cli = new cli_Adres(WCFParams.AdresWcfHost.CurT_Server);
                dt = cli.PrepareGubCurrCharge(finder, reportId, out ret);
            }
            else
            {
                DbAdres db = new DbAdres();
                try
                {
                    dt = db.PrepareGubCurrCharge(finder, reportId, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции для получения данных для отчета " + (reportId == Constants.act_report_gub_curr_charge ? " 'Список текущих начиcлений по домам'" : " 'Итоги оплат по домам (ЕПД)'");
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка PrepareGubCurrCharge" + "\n" + ex.Message, MonitorLog.typelog.Error, 7, 100, true);
                }
                db.Close();
            }
            return dt;
        }


        public Ls LoadAddressPrefer(Finder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            Ls res = null;

            if (SrvRun.isBroker)
            {
                //надо вызвать дальше другой хост
                cli_Adres cli = new cli_Adres(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.LoadAddressPrefer(finder, out ret);
            }
            else
            {
                DbAdres db = new DbAdres();
                try
                {
                    res = db.LoadAddressPrefer(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LoadAddressPrefer() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = null;
                }
                db.Close();
            }
            return res;
        }

        public Returns UpdateAddressPrefer(Ls finder)
        {
            Returns ret = Utils.InitReturns();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Adres cli = new cli_Adres(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.UpdateAddressPrefer(finder);
            }
            else
            {
                DbAdres db = new DbAdres();
                try
                {
                    ret = db.UpdateAddressPrefer(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка UpdateAddressPrefer() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public List<Dom> LoadDom(Dom finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Dom> list = new List<Dom>();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Adres cli = new cli_Adres(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.LoadDom(finder, out ret);
            }
            else
            {
                DbAdres db = new DbAdres();
                try
                {
                    list = db.LoadDom(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LoadDom() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return list;
        }
    }

    /*
    //----------------------------------------------------------------------
    public class cli_HttpAdres : I_HttpAdres  //реализация клиента сервиса списка лс
    //----------------------------------------------------------------------
    {
        I_HttpAdres remoteObject;

        //----------------------------------------------------------------------
        public cli_HttpAdres()
            : base()
        //----------------------------------------------------------------------
        {
            remoteObject = HttpHostBase.CreateInstance<srv_HttpAdres, I_HttpAdres>(AdresWCF.HttpAdres + AdresWCF.srvAdres);
        }
        //----------------------------------------------------------------------
        public string XMLData(string id)
        //----------------------------------------------------------------------
        {
            string s = "";
            try
            {
                s = remoteObject.XMLData(id);
                HttpHostBase.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка XMLData \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return s;
        }
        //----------------------------------------------------------------------
        public string JSONData(string id)
        //----------------------------------------------------------------------
        {
            string s = "";
            try
            {
                s = remoteObject.JSONData(id);
                HttpHostBase.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка JSONData \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return s;
        }
    }
    */
}
