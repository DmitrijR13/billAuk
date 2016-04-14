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
    public class srv_Sprav : srv_Base, I_Sprav //сервис справочников
    //----------------------------------------------------------------------
    {
        //----------------------------------------------------------------------
        public bool TableExists(Finder finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            bool res = false;

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Sprav cli = new cli_Sprav(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.TableExists(finder, out ret);
            }
            else
            {
                DbAdres db = new DbAdres();
                res = db.CasheExists(finder.database);
                db.Close();
            }
            return res;
        }
        //----------------------------------------------------------------------
        public string GetInfo(long kod, int tip, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            string res = "";

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Sprav cli = new cli_Sprav(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.GetInfo(kod, tip, out ret);
            }
            else
            {
                DbSprav db = new DbSprav();
                try
                {
                    res = db.GetNameById(kod, tip, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetInfo() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = "";
                }

                db.Close();
            }
            return res;
        }
        //----------------------------------------------------------------------
        public List<_Service> ServiceLoad(Service finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<_Service> res = null;

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Sprav cli = new cli_Sprav(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.ServiceLoad(finder, out ret);
            }
            else
            {
                DbSprav db = new DbSprav();
                try
                {
                    res = db.ServiceLoad(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка ServiceLoad() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = null;
                }
                db.Close();
            }
            return res;
        }
        //----------------------------------------------------------------------
        public List<_Service> CountsLoad(Finder finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<_Service> res = null;
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Sprav cli = new cli_Sprav(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.CountsLoad(finder, out ret);
            }
            else
            {
                DbSprav db = new DbSprav();
                try
                {
                    res = db.CountsLoad(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка CountsLoad() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = null;
                }
                db.Close();
            }
            return res;
        }
        //--------------------------------------------------------------------------------------------------------------------
        public List<_Service> CountsLoadFilter(Finder finder, out Returns ret, int nzp_kvar)
        //--------------------------------------------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<_Service> res = null;
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Sprav cli = new cli_Sprav(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.CountsLoadFilter(finder, out ret, nzp_kvar);
            }
            else
            {
                DbSprav db = new DbSprav();
                try
                {
                    res = db.CountsLoad(finder, out ret, nzp_kvar);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка CountsLoad() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = null;
                }
                db.Close();
            }
            return res;
        }
        //----------------------------------------------------------------------
        public List<_ResY> ResYLoad(out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Sprav cli = new cli_Sprav(WCFParams.AdresWcfHost.CurT_Server);
                ResYs.ResYList = cli.ResYLoad(out ret);
            }
            else
            {
                if (ResYs.ResYList == null) ResYs.ResYList = new List<_ResY>(); else ResYs.ResYList.Clear();
                DbSprav db = new DbSprav();
                try
                {
                    ResYs.ResYList = db.LoadResY((int)ResYs.ResTypes.LsType + "," + (int)ResYs.ResTypes.LsState, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    //   ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка ResYLoad() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                finally
                {
                    db.Close();
                }
            }
            return ResYs.ResYList;
        }
        //----------------------------------------------------------------------
        public Returns WebDataTable(Finder finder, enSrvOper srv)
        //----------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Sprav cli = new cli_Sprav(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.WebDataTable(finder, srv);
            }
            else
            {

                DbAdres db = new DbAdres();
                DbSprav dbs = new DbSprav();
                try
                {
                    switch (srv)
                    {
                        case enSrvOper.SrvWebArea: return db.WebArea();
                        case enSrvOper.SrvWebGeu: return db.WebGeu();
                        case enSrvOper.SrvWebServ: return dbs.WebService();
                        case enSrvOper.SrvWebSupp: return dbs.WebSupplier();
                        case enSrvOper.SrvWebPoint: return dbs.WebPoint();
                        case enSrvOper.SrvWebPrm: return dbs.WebPrm();
                    }
                    ret.result = false;
                    ret.text = "Не определен параметр WebDataTable";

                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка WebDataTable() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                }
                db.Close();
                dbs.Close();
            }
            return ret;
        }

        //----------------------------------------------------------------------
        public List<_Point> PointLoad_WebData(Finder finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<_Point> res = null;

            if (SrvRun.isBroker)
            {
                //надо вызвать дальше другой хост
                cli_Sprav cli = new cli_Sprav(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.PointLoad_WebData(finder, out ret);
            }
            else
            {
                DbSprav db = new DbSprav();
                try
                {
                    res = db.PointLoad_WebData(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка PointLoad_WebData() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = null;
                }
                db.Close();
            }
            return res;
        }
        //----------------------------------------------------------------------
        public List<_Point> PointLoad(out Returns ret, out _PointWebData p) 
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<_Point> res = null;
            if (SrvRun.isBroker)
            {
                //надо вызвать дальше другой хост
                cli_Sprav cli = new cli_Sprav(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.PointLoad(out ret, out p);
            }
            else
            {
                DbSprav db = new DbSprav();
                p = new _PointWebData(false);
                try
                {
                    Points.PointList.Clear();

                    Points.isInitSuccessfull = db.PointLoad(GlobalSettings.WorkOnlyWithCentralBank, out ret);
                    p = Points.GetPointWebData();
                    res = Points.PointList;
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка PointLoad() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

                    res = null;
                }

                db.Close();
            }
            return res;
            
        }
        //----------------------------------------------------------------------
        public List<_TypeAlg> TypeAlgLoad(out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            if (SrvRun.isBroker)
            {
                //надо вызвать дальше другой хост
                cli_Sprav cli = new cli_Sprav(WCFParams.AdresWcfHost.CurT_Server);
                TypeAlgs.AlgList = cli.TypeAlgLoad(out ret);
            }
            else
            {
                TypeAlgs.AlgList.Clear();
                DbSprav db = new DbSprav();
                try
                {
                    db.LoadTypeAlg(out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка TypeAlgLoad() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return TypeAlgs.AlgList;
        }
        //----------------------------------------------------------------------
        public List<_Help> LoadHelp(int nzp_user, int cur_page, out Returns ret)      
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<_Help> res = null;

            if (SrvRun.isBroker)
            {
                //надо вызвать дальше другой хост
                cli_Sprav cli = new cli_Sprav(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.LoadHelp(nzp_user, cur_page, out ret);
            }
            else
            {
                DbSprav db = new DbSprav();
                try
                {
                    res = db.LoadHelp(nzp_user, cur_page, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LoadHelp() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = null;
                }
                db.Close();
            }
            return res;
        }

        //----------------------------------------------------------------------
        public List<_Supplier> SupplierLoad(Supplier finder, enTypeOfSupp type, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<_Supplier> res = null;

            if (SrvRun.isBroker)
            {
                //надо вызвать дальше другой хост
                cli_Sprav cli = new cli_Sprav(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.SupplierLoad(finder,type, out ret);
            }
            else
            {
                DbSprav db = new DbSprav();
                try
                {
                    res = db.SupplierLoad(finder,type, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка SupplierLoad() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = null;
                }
                db.Close();
            }
            return res;
        }


        /*//----------------------------------------------------------------------
        public List<Payer> PayerLoad(Payer finder, enSrvOper oper, out Returns ret)
        //----------------------------------------------------------------------
        {
            return PayerLoadBank(finder, oper, out ret);
        }*/
        //----------------------------------------------------------------------
        public List<Payer> PayerBankLoad(Payer finder, enSrvOper oper, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<Payer> res = null;

            if (SrvRun.isBroker)
            {
                //надо вызвать дальше другой хост
                cli_Sprav cli = new cli_Sprav(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.PayerBankLoad(finder, oper, out ret);
            }
            else
            {
                DbSprav db = new DbSprav();
                try
                {
                    res = db.PayerBankLoad(finder, oper, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка PayerBankLoad() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = null;
                }
                db.Close();
            }
            return res;
        }

        /*//----------------------------------------------------------------------
        public List<Payer> BankLoad(Payer finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            return PayerLoadBank(finder, true, out ret);
        }*/

        /*
        /// <summary>
        /// Загрузка списка банков(подрядчиков)
        /// </summary>
        //----------------------------------------------------------------------
        public List<Bank> BankLoad(Bank finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<Bank> res = null;

            if (SrvRun.isBroker)
            {
                //надо вызвать дальше другой хост
                cli_Sprav cli = new cli_Sprav();
                res = cli.BankLoad(finder, out ret);
            }
            else
            {
                DbSprav db = new DbSprav();
                try
                {
                    res = db.LoadBank(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка BankLoad() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = null;
                }
                db.Close();
            }
            return res;
        }
        */

        /// <summary>
        /// Загрузка списка namereg
        /// </summary>
        //----------------------------------------------------------------------
        public List<Namereg> NameregLoad(Namereg finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<Namereg> res = null;

            if (SrvRun.isBroker)
            {
                //надо вызвать дальше другой хост
                cli_Sprav cli = new cli_Sprav(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.NameregLoad(finder, out ret);
            }
            else
            {
                DbSprav db = new DbSprav();
                try
                {
                    res = db.LoadNamereg(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка NameregLoad() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = null;
                }
                db.Close();
            }
            return res;
        }

        public List<Payer> LoadPayerTypes(Finder finder, out Returns ret)     
        {
            ret = Utils.InitReturns();
            List<Payer> res = null;

            if (SrvRun.isBroker)
            {
                //надо вызвать дальше другой хост
                cli_Sprav cli = new cli_Sprav(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.LoadPayerTypes(finder, out ret);
            }
            else
            {
                DbSprav db = new DbSprav();
                try
                {
                    res = db.LoadPayerTypes(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LoadPayerTypes() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = null;
                }
                db.Close();
            }
            return res;
        }


        public Returns SaveSupplier(Supplier finder)
        {
            Returns ret;

            if (SrvRun.isBroker)
            {
                //надо вызвать дальше другой хост
                cli_Sprav cli = new cli_Sprav(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.SaveSupplier(finder);
            }
            else
            {
                DbSprav db = new DbSprav();
                try
                {
                    ret = db.SaveSupplier(finder);
                }
                catch (Exception ex)
                {
                    ret = new Returns(false, ex.Message);
                    MonitorLog.WriteLog("Ошибка SaveSupplier()\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        //----------------------------------------------------------------------
        public Returns SavePayer(Payer finder)
        //----------------------------------------------------------------------
        {
            Returns ret;

            if (SrvRun.isBroker)
            {
                //надо вызвать дальше другой хост
                cli_Sprav cli = new cli_Sprav(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.SavePayer(finder);
            }
            else
            {
                DbSprav db = new DbSprav();
                try
                {
                    ret = db.SavePayer(finder);
                }
                catch (Exception ex)
                {
                    ret = new Returns(false, ex.Message);
                    MonitorLog.WriteLog("Ошибка SavePayer()\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        //----------------------------------------------------------------------
        public Returns SaveBank(Bank finder)
        //----------------------------------------------------------------------
        {
            Returns ret;

            if (SrvRun.isBroker)
            {
                //надо вызвать дальше другой хост
                cli_Sprav cli = new cli_Sprav(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.SaveBank(finder);
            }
            else
            {
                DbSprav db = new DbSprav();
                try
                {
                    ret = db.SaveBank(finder);
                }
                catch (Exception ex)
                {
                    ret = new Returns(false, ex.Message);
                    MonitorLog.WriteLog("Ошибка SaveBank()\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        ///<summary>Получить файл отчета
        /// </summary>
        /*public string GetFileRep(Int64 nzp)
        {
            Returns ret = Utils.InitReturns();
            return DbSprav.GetSpravFile(out ret,nzp);
        }*/

        public PackDistributionParameters GetPackDistributionParameters(out Returns ret)
        {
            ret = new Returns(true);
            return Points.packDistributionParameters;
        }


        public Returns RefreshSpravClone(Finder finder)
        //----------------------------------------------------------------------
        {
            Returns ret;

            if (SrvRun.isBroker)
            {
                //надо вызвать дальше другой хост
                cli_Sprav cli = new cli_Sprav(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.RefreshSpravClone(finder);
            }
            else
            {
                DbSprav db = new DbSprav();
                try
                {
                    ret = db.UpdateSpravTables(finder);
                }
                catch (Exception ex)
                {
                    ret = new Returns(false, ex.Message);
                    MonitorLog.WriteLog("Ошибка RefreshSpravClone()\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public List<Town> LoadTown(Town finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<Town> res = null;

            if (SrvRun.isBroker)
            {
                //надо вызвать дальше другой хост
                cli_Sprav cli = new cli_Sprav(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.LoadTown(finder, out ret);
            }
            else
            {
                DbSprav db = new DbSprav();
                try
                {
                    res = db.LoadTown(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LoadTown() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = null;
                }
                db.Close();
            }
            return res;
        }

        public List<_reestr_unloads> LoadUploadedReestrList(Finder finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<_reestr_unloads> res = null;

            if (SrvRun.isBroker)
            {
                //надо вызвать дальше другой хост
                cli_Sprav cli = new cli_Sprav(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.LoadUploadedReestrList(finder, out ret);
            }
            else
            {
                DbSprav db = new DbSprav();
                try
                {
                    res = db.LoadUploadedReestrList(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LoadUploadedReestrList() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = null;
                }
                db.Close();
            }
            return res;
        }

        public List<unload_exchange_sz> LoadListExchangeSZ(Finder finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<unload_exchange_sz> res = null;

            if (SrvRun.isBroker)
            {
                //надо вызвать дальше другой хост
                cli_Sprav cli = new cli_Sprav(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.LoadListExchangeSZ(finder, out ret);
            }
            else
            {
                DbSprav db = new DbSprav();
                try
                {
                    res = db.LoadListExchangeSZ(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LoadListExchangeSZ() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = null;
                }
                db.Close();
            }
            return res;
        }

        public List<_reestr_downloads> LoadDownloadedReestrList(Finder finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<_reestr_downloads> res = null;

            if (SrvRun.isBroker)
            {
                //надо вызвать дальше другой хост
                cli_Sprav cli = new cli_Sprav(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.LoadDownloadedReestrList(finder, out ret);
            }
            else
            {
                DbSprav db = new DbSprav();
                try
                {
                    res = db.LoadDownloadedReestrList(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LoadDownloadedReestrList() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = null;
                }
                db.Close();
            }
            return res;
        }

        public List<Measure> LoadMeasure(Measure finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Measure> res = null;

            if (SrvRun.isBroker)
            {
                //надо вызвать дальше другой хост
                cli_Sprav cli = new cli_Sprav(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.LoadMeasure(finder, out ret);
            }
            else
            {
                DbSprav db = new DbSprav();
                try
                {
                    res = db.LoadMeasure(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LoadMeasure() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = null;
                }
                db.Close();
            }
            return res;
        }

        public List<CalcMethod> LoadCalcMethod(CalcMethod finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<CalcMethod> res = null;

            if (SrvRun.isBroker)
            {
                //надо вызвать дальше другой хост
                cli_Sprav cli = new cli_Sprav(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.LoadCalcMethod(finder, out ret);
            }
            else
            {
                DbSprav db = new DbSprav();
                try
                {
                    res = db.LoadCalcMethod(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LoadCalcMethod() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = null;
                }
                db.Close();
            }
            return res;
        }

        public List<Land> LoadLand(Land finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Land> res = null;

            if (SrvRun.isBroker)
            {
                //надо вызвать дальше другой хост
                cli_Sprav cli = new cli_Sprav(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.LoadLand(finder, out ret);
            }
            else
            {
                DbSprav db = new DbSprav();
                try
                {
                    res = db.LoadLand(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LoadLand() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = null;
                }
                db.Close();
            }
            return res;
        }

        public List<Stat> LoadStat(Stat finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Stat> res = null;

            if (SrvRun.isBroker)
            {
                //надо вызвать дальше другой хост
                cli_Sprav cli = new cli_Sprav(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.LoadStat(finder, out ret);
            }
            else
            {
                DbSprav db = new DbSprav();
                try
                {
                    res = db.LoadStat(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LoadStat() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = null;
                }
                db.Close();
            }
            return res;
        }

        public List<Town> LoadTown2(Town finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Town> res = null;

            if (SrvRun.isBroker)
            {
                //надо вызвать дальше другой хост
                cli_Sprav cli = new cli_Sprav(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.LoadTown2(finder, out ret);
            }
            else
            {
                DbSprav db = new DbSprav();
                try
                {
                    res = db.LoadTown2(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LoadTown2() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = null;
                }
                db.Close();
            }
            return res;
        }

        public List<Rajon> LoadRajon(Rajon finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Rajon> res = null;

            if (SrvRun.isBroker)
            {
                //надо вызвать дальше другой хост
                cli_Sprav cli = new cli_Sprav(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.LoadRajon(finder, out ret);
            }
            else
            {
                DbSprav db = new DbSprav();
                try
                {
                    res = db.LoadRajon(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LoadRajon() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = null;
                }
                db.Close();
            }
            return res;
        }

        public List<PackTypes> LoadPackTypes(PackTypes finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<PackTypes> res = null;

            if (SrvRun.isBroker)
            {
                //надо вызвать дальше другой хост
                cli_Sprav cli = new cli_Sprav(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.LoadPackTypes(finder, out ret);
            }
            else
            {
                DbSprav db = new DbSprav();
                try
                {
                    res = db.LoadPackTypes(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LoadPackTypes() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = null;
                }
                db.Close();
            }
            return res;
        }

        public Returns DeleteReestrTula(_reestr_unloads finder)
        {
            Returns ret;
            ret = Utils.InitReturns();          
     
                DbSprav db = new DbSprav();
                try
                {
                    ret = db.DeleteReestrTula(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка DeleteReestrTula() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                   
                }
                db.Close();
            
            return ret;
        }

        public Returns DeleteDownloadReestrTula(Finder finder, int nzp_download)
        {
            Returns ret;
            ret = Utils.InitReturns();

            DbSprav db = new DbSprav();
            try
            {
                ret = db.DeleteDownloadReestrTula(finder, nzp_download);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка вызова функции";
                if (Constants.Viewerror) ret.text += " : " + ex.Message;
                if (Constants.Debug) MonitorLog.WriteLog("Ошибка DeleteDownloadReestrTula() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

            }
            db.Close();

            return ret;
        }


        //----------------------------------------------------------------------
        public List<BankPayers> BankPayersLoad(Supplier finder, enTypeOfSupp type, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<BankPayers> res = null;

            if (SrvRun.isBroker)
            {
                //надо вызвать дальше другой хост
                cli_Sprav cli = new cli_Sprav(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.BankPayersLoad(finder, type, out ret);
            }
            else
            {
                DbSprav db = new DbSprav();
                try
                {
                    res = db.BankPayersLoad(finder, type, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка SupplierLoad() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = null;
                }
                db.Close();
            }
            return res;
        }

        //----------------------------------------------------------------------
        public List<BankPayers> BankPayersLoadBC(Supplier finder, enTypeOfSupp type, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<BankPayers> res = null;

            if (SrvRun.isBroker)
            {
                //надо вызвать дальше другой хост
                cli_Sprav cli = new cli_Sprav(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.BankPayersLoadBC(finder, type, out ret);
            }
            else
            {
                DbSprav db = new DbSprav();
                try
                {
                    res = db.BankPayersLoadBC(finder, type, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка BankPayersLoadBC() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = null;
                }
                db.Close();
            }
            return res;
        }

        public List<Supplier> LoadSupplierSpis(Area_ls finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<Supplier> res = null;

            if (SrvRun.isBroker)
            {
                //надо вызвать дальше другой хост
                cli_Sprav cli = new cli_Sprav(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.LoadSupplierSpis(finder, out ret);
            }
            else
            {
                DbSprav db = new DbSprav();
                try
                {
                    res = db.LoadSupplierSpis(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LoadSupplierSpis() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = null;
                }
                db.Close();
            }
            return res;
        }

        //----------------------------------------------------------------------
        public List<Bank> GetBankType(Bank finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<Bank> res = null;

            if (SrvRun.isBroker)
            {
                //надо вызвать дальше другой хост
                cli_Sprav cli = new cli_Sprav(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.GetBankType(finder,out ret);
            }
            else
            {
                DbMoneys db = new DbMoneys();
                try
                {
                    res = db.GetBankType(finder,out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка SupplierLoad() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = null;
                }
                db.Close();
            }
            return res;
        }

        public List<BCTypes> LoadBCTypes(BCTypes finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<BCTypes> res = null;

            if (SrvRun.isBroker)
            {
                //надо вызвать дальше другой хост
                cli_Sprav cli = new cli_Sprav(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.LoadBCTypes(finder, out ret);
            }
            else
            {
                DbSprav db = new DbSprav();
                try
                {
                    res = db.LoadBCTypes(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LoadBCTypes() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = null;
                }
                db.Close();
            }
            return res;
        }

        public List<Formuls> GetFormuls(FormulsFinder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Formuls> res = null;

            if (SrvRun.isBroker)
            {
                //надо вызвать дальше другой хост
                cli_Sprav cli = new cli_Sprav(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.GetFormuls(finder, out ret);
            }
            else
            {
                DbSprav db = new DbSprav();
                try
                {
                    res = db.GetFormuls(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetFormuls() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = null;
                }
                db.Close();
            }
            return res;
        }
    }
}
