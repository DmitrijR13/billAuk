using System;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Net;
using System.Security.Principal;

using System.Collections.Generic;
using Bars.KP50.DB.Sprav.Source;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.IFMX.Kernel.source.kart;
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

            
            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Sprav cli = new cli_Sprav(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.TableExists(finder, out ret);
            }
            else
            {
                using (var db = new DbAdresClient())
                {
                    res = db.CasheExists(finder.database);
                }
            }
            return res;
        }

    
        //----------------------------------------------------------------------
        public string GetInfo(long kod, int tip, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            string res = "";

            if (SrvRunProgramRole.IsBroker)
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

            if (SrvRunProgramRole.IsBroker)
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

        public List<KodSum> GeListKodSum(KodSum finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<KodSum> res = null;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Sprav cli = new cli_Sprav(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.GeListKodSum(finder, out ret);
            }
            else
            {
                DbSprav db = new DbSprav();
                try
                {
                    res = db.GeListKodSum(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GeListKodSum() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = null;
                }
                db.Close();
            }
            return res;
        }

        public TDocumentBase GetDocumentBase(TDocumentBase finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            TDocumentBase res = null;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Sprav cli = new cli_Sprav(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.GetDocumentBase(finder, out ret);
            }
            else
            {
                DbSprav db = new DbSprav();
                try
                {
                    res = db.GetDocumentBase(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetDocumentBase() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
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
            if (SrvRunProgramRole.IsBroker)
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
            if (SrvRunProgramRole.IsBroker)
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
            if (SrvRunProgramRole.IsBroker)
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
            if (SrvRunProgramRole.IsBroker)
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

            if (SrvRunProgramRole.IsBroker)
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
            if (SrvRunProgramRole.IsBroker)
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
            if (SrvRunProgramRole.IsBroker)
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

            if (SrvRunProgramRole.IsBroker)
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

            if (SrvRunProgramRole.IsBroker)
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

        public List<_Supplier> LoadSupplierByArea(Supplier finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<_Supplier> res = null;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Sprav cli = new cli_Sprav(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.LoadSupplierByArea(finder, out ret);
            }
            else
            {
                DbSprav db = new DbSprav();
                try
                {
                    res = db.LoadSupplierByArea(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LoadSupplierByArea() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = null;
                }
                db.Close();
            }
            return res;
        }


        public List<ContractClass> ContractsLoad(ContractFinder finder, enTypeOfSupp type, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<ContractClass> res = null;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Sprav cli = new cli_Sprav(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.ContractsLoad(finder, type, out ret);
            }
            else
            {
                DbSprav db = new DbSprav();
                try
                {
                    res = db.ContractsLoad(finder, type, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка ContractsLoad() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = null;
                }
                db.Close();
            }
            return res;
        }

        public List<ContractClass> NewFdContractsLoad(ContractFinder finder, enTypeOfSupp type, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<ContractClass> res = null;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Sprav cli = new cli_Sprav(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.NewFdContractsLoad(finder, type, out ret);
            }
            else
            {
                DbSprav db = new DbSprav();
                try
                {
                    res = db.NewFdContractsLoad(finder, type, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка NewFdContractsLoad() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = null;
                }
                db.Close();
            }
            return res;
        }

        public List<int> BanksForOneSuppLoad(Supplier finder, out Returns ret, out bool IfCanChangePayers)
        {
            ret = Utils.InitReturns();
            IfCanChangePayers = false;
            List<int> res = null;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Sprav cli = new cli_Sprav(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.BanksForOneSuppLoad(finder, out ret, out IfCanChangePayers);
            }
            else
            {
                DbSpravKernel db = new DbSpravKernel();
                try
                {
                    res = db.BanksForOneSuppLoad(finder, out ret, out IfCanChangePayers);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка BanksForOneSuppLoad() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = null;
                }
                db.Close();
            }
            return res;
        }
        
        public List<Payer> PayersLoad(Payer finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Payer> res = null;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Sprav cli = new cli_Sprav(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.PayersLoad(finder, out ret);
            }
            else
            {
                DbSprav db = new DbSprav();
                try
                {
                    res = db.PayerLoad(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка PayerLoad() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = null;
                }
                db.Close();
            }
            return res;
        }


        //----------------------------------------------------------------------
        public List<FileName> FileNameLoad(FileName finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<FileName> res = null;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Sprav cli = new cli_Sprav(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.FileNameLoad(finder, out ret);
            }
            else
            {
                DbSprav db = new DbSprav();
                try
                {
                    res = db.FileNameLoad(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка FileNameLoad() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
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

            if (SrvRunProgramRole.IsBroker)
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

        public List<Payer> BankPayerLoad(Payer finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<Payer> res = null;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Sprav cli = new cli_Sprav(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.BankPayerLoad(finder, out ret);
            }
            else
            {
                DbSprav db = new DbSprav();
                try
                {
                    res = db.BankPayerLoad(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка BankPayer() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = null;
                }
                db.Close();
            }
            return res;
        }

        public List<Payer> LoadPayers(Payer finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Payer> res = null;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Sprav cli = new cli_Sprav(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.LoadPayers(finder,  out ret);
            }
            else
            {
                DbSprav db = new DbSprav();
                try
                {
                    res = db.LoadPayers(finder,  out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LoadPayers() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = null;
                }
                db.Close();
            }
            return res;
        }

        public List<Payer> LoadPayersNewFd(Payer finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Payer> res = null;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Sprav cli = new cli_Sprav(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.LoadPayersNewFd(finder, out ret);
            }
            else
            {
                DbSprav db = new DbSprav();
                try
                {
                    res = db.LoadPayersNewFd(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LoadPayersNewFd() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = null;
                }
                db.Close();
            }
            return res;
        }

        public List<Payer> LoadPayersContragents(Payer finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Payer> res = null;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Sprav cli = new cli_Sprav(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.LoadPayersContragents(finder, out ret);
            }
            else
            {
                DbSprav db = new DbSprav();
                try
                {
                    res = db.LoadPayersContragents(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LoadPayers() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = null;
                }
                db.Close();
            }
            return res;
        }

        public void PayerBankForIssrpF101(out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<Payer> res = null;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Sprav cli = new cli_Sprav(WCFParams.AdresWcfHost.CurT_Server);
                cli.PayerBankForIssrpF101(out ret);
            }
            else
            {
                DbSprav db = new DbSprav();
                try
                {
                    db.PayerBankForIssrpF101(out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка PayerBankForIssrpF101() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = null;
                }
                db.Close();
            }
        }


        public List<Payer> PayerBankLoadContract(Payer finder, enSrvOper oper, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<Payer> res = null;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Sprav cli = new cli_Sprav(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.PayerBankLoadContract(finder, oper, out ret);
            }
            else
            {
                DbSprav db = new DbSprav();
                try
                {
                    res = db.PayerBankLoadContract(finder, oper, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка PayerBankLoadContract() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
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

            if (SrvRunProgramRole.IsBroker)
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

            if (SrvRunProgramRole.IsBroker)
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

            if (SrvRunProgramRole.IsBroker)
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

        public Returns ContrRenameDog(Payer finder)    
        {
            Returns ret = Utils.InitReturns();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Sprav cli = new cli_Sprav(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.ContrRenameDog(finder);
            }
            else
            {
                DbSprav db = new DbSprav();
                try
                {
                    ret = db.ContrRenameDog(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug)
                        MonitorLog.WriteLog("Ошибка ContrRenameDog() \n  " + ex.Message, MonitorLog.typelog.Error, 2,
                            100, true);
                }
                db.Close();
            }
            return ret;
        }


        public Returns SaveSupplier(Supplier finder)
        {
            Returns ret;

            if (SrvRunProgramRole.IsBroker)
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

        public Returns SaveContract(ContractFinder finder, enSrvOper oper)
        {
            Returns ret;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Sprav cli = new cli_Sprav(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.SaveContract(finder, oper);
            }
            else
            {
                DbSprav db = new DbSprav();
                try
                {
                    ret = new Returns(false);
                    switch(oper)
                    {
                        case enSrvOper.srvSave: ret = db.SaveContract(finder); break;
                        case enSrvOper.srvDelete: ret = db.DeleteContract(finder); break;
                    }
                }
                catch (Exception ex)
                {
                    ret = new Returns(false, ex.Message);
                    MonitorLog.WriteLog("Ошибка SaveContractor()\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
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

            if (SrvRunProgramRole.IsBroker)
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

        public Returns SavePayerContract(Payer finder)
        //----------------------------------------------------------------------
        {
            Returns ret;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Sprav cli = new cli_Sprav(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.SavePayerContract(finder);
            }
            else
            {
                DbSprav db = new DbSprav();
                try
                {
                    ret = db.SavePayerContract(finder);
                }
                catch (Exception ex)
                {
                    ret = new Returns(false, ex.Message);
                    MonitorLog.WriteLog("Ошибка SavePayerContract()\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public Returns SavePayerContractNewFd(Payer finder)
        //----------------------------------------------------------------------
        {
            Returns ret;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Sprav cli = new cli_Sprav(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.SavePayerContractNewFd(finder);
            }
            else
            {
                DbSprav db = new DbSprav();
                try
                {
                    ret = db.SavePayerContractNewFd(finder);
                }
                catch (Exception ex)
                {
                    ret = new Returns(false, ex.Message);
                    MonitorLog.WriteLog("Ошибка SavePayerContract()\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public Returns DeletePayerContract(Payer finder)
        {
            Returns ret;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Sprav cli = new cli_Sprav(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.DeletePayerContract(finder);
            }
            else
            {
                DbSprav db = new DbSprav();
                try
                {
                    ret = db.DeletePayerContract(finder);
                }
                catch (Exception ex)
                {
                    ret = new Returns(false, ex.Message);
                    MonitorLog.WriteLog("Ошибка DeletePayerContract()\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
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

            if (SrvRunProgramRole.IsBroker)
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

            if (SrvRunProgramRole.IsBroker)
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

            if (SrvRunProgramRole.IsBroker)
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

            if (SrvRunProgramRole.IsBroker)
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

            if (SrvRunProgramRole.IsBroker)
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

            if (SrvRunProgramRole.IsBroker)
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

            if (SrvRunProgramRole.IsBroker)
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

            if (SrvRunProgramRole.IsBroker)
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

            if (SrvRunProgramRole.IsBroker)
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

            if (SrvRunProgramRole.IsBroker)
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

            if (SrvRunProgramRole.IsBroker)
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

            if (SrvRunProgramRole.IsBroker)
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

            if (SrvRunProgramRole.IsBroker)
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

        public Returns DeleteDownloadReestrMariyEl(Finder finder, int nzp_download)
        {
            Returns ret;
            ret = Utils.InitReturns();

            DbSprav db = new DbSprav();
            try
            {
                ret = db.DeleteDownloadReestrMariyEl(finder, nzp_download);
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

            if (SrvRunProgramRole.IsBroker)
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

            if (SrvRunProgramRole.IsBroker)
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

        //----------------------------------------------------------------------
        public List<Payer> GetPayersDogovor(int nzpUser, Payer.ContragentTypes typePayer, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<Payer> payers;

            if (SrvRunProgramRole.IsBroker)
            {
                var cli = new cli_Sprav(WCFParams.AdresWcfHost.CurT_Server);
                payers = cli.GetPayersDogovor(nzpUser, typePayer, out ret);
            }
            else
            {
                try
                {
                    using (var db = new DbSprav())
                    {
                        payers = db.GetPayersDogovor(nzpUser, typePayer, out ret);
                    }
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetPayersDogovor() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    payers = null;
                }
            }
            return payers;
        }

        public List<Supplier> LoadSupplierSpis(Area_ls finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<Supplier> res = null;

            if (SrvRunProgramRole.IsBroker)
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
        public List<Bank> GetBanksExecutingPayments(int nzpUser, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<Bank> banks;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                var cli = new cli_Sprav(WCFParams.AdresWcfHost.CurT_Server);
                banks = cli.GetBanksExecutingPayments(nzpUser, out ret);
            }
            else
            {
                try
                {
                    using (var db = new DbMoneys())
                    {
                        banks = db.GetBanksExecutingPayments(nzpUser, out ret);
                    }
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetBanksExecutingPayments() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    banks = null;
                }
            }
            return banks;
        }

        public List<BCTypes> LoadBCTypes(BCTypes finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<BCTypes> res = null;

            if (SrvRunProgramRole.IsBroker)
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

            if (SrvRunProgramRole.IsBroker)
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


        public List<tula_s_bank> LoadPayerAgents(Finder finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<tula_s_bank> res = null;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Sprav cli = new cli_Sprav(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.LoadPayerAgents(finder, out ret);
            }
            else
            {
                DbReestrTula db = new DbReestrTula();
                try
                {
                    res = db.LoadPayerAgents(finder, out ret);
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


        public Returns DeletePayerAgent(Finder finder, int id)
        {
            Returns ret;
            ret = Utils.InitReturns();

            DbReestrTula db = new DbReestrTula();
            try
            {
                ret = db.DeletePayerAgent(finder, id);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка вызова функции";
                if (Constants.Viewerror) ret.text += " : " + ex.Message;
                if (Constants.Debug) MonitorLog.WriteLog("Ошибка DeletePayerAgent() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

            }
            db.Close();

            return ret;
        }

        public Returns AddPayerAgent(Finder finder, tula_s_bank agent)
        {
            Returns ret;
            ret = Utils.InitReturns();

            DbReestrTula db = new DbReestrTula();
            try
            {
                ret = db.AddPayerAgent(finder, agent);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка вызова функции";
                if (Constants.Viewerror) ret.text += " : " + ex.Message;
                if (Constants.Debug) MonitorLog.WriteLog("Ошибка AddPayerAgent() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

            }
            db.Close();

            return ret;
        }

        public Returns SavePayerAgent(Finder finder, tula_s_bank agent)
        {
            Returns ret;
            ret = Utils.InitReturns();

            DbReestrTula db = new DbReestrTula();
            try
            {
                ret = db.SavePayerAgent(finder, agent);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка вызова функции";
                if (Constants.Viewerror) ret.text += " : " + ex.Message;
                if (Constants.Debug) MonitorLog.WriteLog("Ошибка SavePayerAgent() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);

            }
            db.Close();

            return ret;
        }

        public Returns MergeContr(Payer finder, List<int> list)
            //----------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Sprav cli = new cli_Sprav(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.MergeContr(finder, list);
            }
            else
            {
                using (DbSprav db = new DbSprav())
                {
                    try
                    {
                        ret = db.MergeContr(finder, list);
                    }
                    catch (Exception ex)
                    {
                        ret.result = false;
                        ret.text = "Ошибка вызова функции";
                        if (Constants.Viewerror) ret.text += " : " + ex.Message;
                        if (Constants.Debug)
                            MonitorLog.WriteLog("Ошибка MergeContrt() \n  " + ex.Message, MonitorLog.typelog.Error, 2,
                                100, true);
                    }
                }

            }
            return ret;
        }

        public List<int> LoadListUchastok(Finder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            var listUch = new List<int>();
            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                var cli = new cli_Sprav(WCFParams.AdresWcfHost.CurT_Server);
                listUch = cli.LoadListUchastok(finder, out ret);
            }
            else
            {
                using (var db = new DbSprav())
                {
                    try
                    {
                        listUch = db.LoadListUchastok(finder, out ret);
                    }
                    catch (Exception ex)
                    {
                        ret.result = false;
                        ret.text = "Ошибка вызова функции";
                        if (Constants.Viewerror) ret.text += " : " + ex.Message;
                        if (Constants.Debug)
                            MonitorLog.WriteLog("Ошибка LoadListUchastok() \n  " + ex.Message, MonitorLog.typelog.Error, 2,
                                100, true);
                    }
                }

            }
            return listUch;
        }

        public Returns UpdateCashSpravTable(Finder finder)
        {
          Returns ret = Utils.InitReturns();
            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                var cli = new cli_Sprav(WCFParams.AdresWcfHost.CurT_Server);
                ret= cli.UpdateCashSpravTable(finder);
            }
            else
            {
                using (var db = new DbSprav())
                {
                    try
                    {
                        ret = db.UpdateCashSpravTable(finder);
                    }
                    catch (Exception ex)
                    {
                        ret.result = false;
                        ret.text = "Ошибка вызова функции";
                        if (Constants.Viewerror) ret.text += " : " + ex.Message;
                        if (Constants.Debug)
                            MonitorLog.WriteLog("Ошибка UpdateSpravTables() \n  " + ex.Message, MonitorLog.typelog.Error, 2,
                                100, true);
                    }
                }

            }
            return ret;
        }

        public List<Supplier> LoadDogovorByPoints(Finder finder, out Returns ret)
        {
             ret = Utils.InitReturns();
             List<Supplier> list = new List<Supplier>();
            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                var cli = new cli_Sprav(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.LoadDogovorByPoints(finder, out ret);
            }
            else
            {
                using (var db = new DbSprav())
                {
                    try
                    {
                        list = db.LoadDogovorByPoints(finder, out ret);
                    }
                    catch (Exception ex)
                    {
                        ret.result = false;
                        ret.text = "Ошибка вызова функции";
                        if (Constants.Viewerror) ret.text += " : " + ex.Message;
                        if (Constants.Debug)
                            MonitorLog.WriteLog("Ошибка UpdateSpravTables() \n  " + ex.Message, MonitorLog.typelog.Error, 2,
                                100, true);
                    }
                }

            }
            return list;
        }
        public List<Finder> LoadPointsByScopeDogovor(ScopeAdress finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Finder> list = new List<Finder>();
            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                var cli = new cli_Sprav(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.LoadPointsByScopeDogovor(finder, out ret);
            }
            else
            {
                using (var db = new DbSprav())
                {
                    try
                    {
                        list = db.LoadPointsByScopeDogovor(finder, out ret);
                    }
                    catch (Exception ex)
                    {
                        ret.result = false;
                        ret.text = "Ошибка вызова функции";
                        if (Constants.Viewerror) ret.text += " : " + ex.Message;
                        if (Constants.Debug)
                            MonitorLog.WriteLog("Ошибка LoadPointsByScopeDogovor() \n  " + ex.Message, MonitorLog.typelog.Error, 2,
                                100, true);
                    }
                }

            }
            return list;
        }

        public Returns AddNewToScopeAdress(ScopeAdress finder)
        {
            Returns ret = Utils.InitReturns();
            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                var cli = new cli_Sprav(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.AddNewToScopeAdress(finder);
            }
            else
            {
                using (var db = new DbScopeAddress())
                {
                    try
                    {
                        ret = db.AddNewToScopeAdress(finder);
                    }
                    catch (Exception ex)
                    {
                        ret.result = false;
                        ret.text = "Ошибка вызова функции";
                        if (Constants.Viewerror) ret.text += " : " + ex.Message;
                        if (Constants.Debug)
                            MonitorLog.WriteLog("Ошибка AddNewToScopeAdress() \n  " + ex.Message, MonitorLog.typelog.Error, 2,
                                100, true);
                    }
                }

            }
            return ret;
        }

        public List<ScopeAdress> GetAdressesByScope(ScopeAdress finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<ScopeAdress> list = new List<ScopeAdress>();
            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                var cli = new cli_Sprav(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.GetAdressesByScope(finder, out ret);
            }
            else
            {
                using (var db = new DbScopeAddress())
                {
                    try
                    {
                        list = db.GetAdressesByScope(finder, out ret);
                    }
                    catch (Exception ex)
                    {
                        ret.result = false;
                        ret.text = "Ошибка вызова функции";
                        if (Constants.Viewerror) ret.text += " : " + ex.Message;
                        if (Constants.Debug)
                            MonitorLog.WriteLog("Ошибка GetAdressesByScope() \n  " + ex.Message, MonitorLog.typelog.Error, 2,
                                100, true);
                    }
                }

            }
            return list;
        }

        public Returns DeleteAdressFromScope(ScopeAdress finder)
        {
            Returns ret = Utils.InitReturns();
            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                var cli = new cli_Sprav(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.DeleteAdressFromScope(finder);
            }
            else
            {
                using (var db = new DbScopeAddress())
                {
                    try
                    {
                        ret = db.DeleteAdressFromScope(finder);
                    }
                    catch (Exception ex)
                    {
                        ret.result = false;
                        ret.text = "Ошибка вызова функции";
                        if (Constants.Viewerror) ret.text += " : " + ex.Message;
                        if (Constants.Debug)
                            MonitorLog.WriteLog("Ошибка AddNewToScopeAdress() \n  " + ex.Message, MonitorLog.typelog.Error, 2,
                                100, true);
                    }
                }

            }
            return ret;
        }

        public Returns CheckUsingScopeByChilds(ScopeAdress finder)
        {
            Returns ret = Utils.InitReturns();
            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                var cli = new cli_Sprav(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.CheckUsingScopeByChilds(finder);
            }
            else
            {
                using (var db = new DbScopeAddress())
                {
                    try
                    {
                        ret = db.CheckUsingScopeByChilds(finder);
                    }
                    catch (Exception ex)
                    {
                        ret.result = false;
                        ret.text = "Ошибка вызова функции";
                        if (Constants.Viewerror) ret.text += " : " + ex.Message;
                        if (Constants.Debug)
                            MonitorLog.WriteLog("Ошибка AddNewToScopeAdress() \n  " + ex.Message, MonitorLog.typelog.Error, 2,
                                100, true);
                    }
                }
            }
            return ret; 
        }

        public Returns SaveSupplierChanges(ContractFinder finder, enSrvOper oper)
        {
            Returns ret;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Sprav cli = new cli_Sprav(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.SaveSupplierChanges(finder, oper);
            }
            else
            {
                DbSupplierNew db = new DbSupplierNew();
                try
                {
                    ret = new Returns(false);
                    switch (oper)
                    {
                        case enSrvOper.srvSave: ret = db.SaveContract(finder); break;
                        case enSrvOper.srvDelete: ret = db.DeleteContract(finder); break;
                    }
                }
                catch (Exception ex)
                {
                    ret = new Returns(false, ex.Message);
                    MonitorLog.WriteLog("Ошибка SaveContractor()\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }


        public RecordMonth GetCalcMonth()
        {
            RecordMonth rm = new RecordMonth();
            
            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Sprav cli = new cli_Sprav(WCFParams.AdresWcfHost.CurT_Server);
                rm = cli.GetCalcMonth();
            }
            else
            {
                var db = new DbSprav();
                try
                {
                    rm = db.GetCalcMonth();
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name +
                    "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return rm;
        }

        public List<PrmTypes> LoadKodSum(Finder finder, out Returns ret)
        {
            List<PrmTypes> listKodSum = new List<PrmTypes>();
            ret = Utils.InitReturns();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Sprav cli = new cli_Sprav(WCFParams.AdresWcfHost.CurT_Server);
                listKodSum = cli.LoadKodSum(finder, out ret);
            }
            else
            {
                using (var db = new DbSprav())
                {
                    try
                    {
                        listKodSum = db.LoadKodSum(finder, out ret);
                    }
                    catch (Exception ex)
                    {
                        ret.result = false;
                        MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name +
                                            "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    }
                }
            }
            return listKodSum;
        }
        /// <summary>
        /// получение справочных значений параметров из resY по nzp_res
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public List<_ResY> LoadResY(string find_nzp_res, out Returns ret)
        {
            List<_ResY> listResY = new List<_ResY>();
            ret = Utils.InitReturns();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Sprav cli = new cli_Sprav(WCFParams.AdresWcfHost.CurT_Server);
                listResY = cli.LoadResY(find_nzp_res, out ret);
            }
            else
            {
                using (var db = new DbSprav())
                {
                    try
                    {
                        listResY = db.LoadResY(find_nzp_res, out ret);
                    }
                    catch (Exception ex)
                    {
                        ret.result = false;
                        MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name +
                                            "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    }
                }
            }
            return listResY;
        }

        public List<PrmTypes> GetListNzpCntServ(Finder finder, out Returns ret)
        {
            List<PrmTypes> ListNzpCnt = new List<PrmTypes>();
            ret = Utils.InitReturns();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Sprav cli = new cli_Sprav(WCFParams.AdresWcfHost.CurT_Server);
                ListNzpCnt = cli.GetListNzpCntServ(finder, out ret);
            }
            else
            {
                using (var db = new DbSprav())
                {
                    try
                    {
                        ListNzpCnt = db.GetListNzpCntServ(finder, out ret);
                    }
                    catch (Exception ex)
                    {
                        ret.result = false;
                        MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name +
                                            "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    }
                }
            }
            return ListNzpCnt;
        }

        //----------------------------------------------------------------------
        public Returns SaveContractAllowOv(Finder finder, enSrvOper oper, List<ContractClass> list)
        //----------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Sprav cli = new cli_Sprav(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.SaveContractAllowOv(finder, oper, list);
            }
            else
            {
                DbSprav db = new DbSprav();
                try
                {
                    ret = db.SaveContractAllowOv(finder, oper, list);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name +
                                        "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                finally
                {
                    db.Close();
                }
            }
            return ret;
        }
        //----------------------------------------------------------------------
        public List<OverpaymentForDistrib> GetOverpaymentForDistrib(OverpaymentForDistrib finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            var list = new List<OverpaymentForDistrib>();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Sprav cli = new cli_Sprav(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.GetOverpaymentForDistrib(finder, out ret);
            }
            else
            {
                using (DbSprav db = new DbSprav())
                {
                    try
                    {
                        list = db.GetOverpaymentForDistrib(finder, out ret);
                    }
                    catch (Exception ex)
                    {
                        ret.result = false;
                        MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name +
                                            "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    }
                }
            }
            return list;
        }
        //----------------------------------------------------------------------
        public Returns SaveSelectedOverpaymentForDistrib(OverpaymentForDistrib finder, List<OverpaymentForDistrib> list)
            //----------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Sprav cli = new cli_Sprav(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.SaveSelectedOverpaymentForDistrib(finder, list);
            }
            else
            {
                using (var db = new DbSprav())
                {
                    try
                    {
                        ret = db.SaveSelectedOverpaymentForDistrib(finder, list);
                    }
                    catch (Exception ex)
                    {
                        ret.result = false;
                        ret.text = "Ошибка вызова функции";
                        MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name +
                                            "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    }
                }
            }
            return ret;
        }
        //----------------------------------------------------------------------
        public Returns GetSelectedDogForDistribOv(OverpaymentForDistrib finder)
        //----------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Sprav cli = new cli_Sprav(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetSelectedDogForDistribOv(finder);
            }
            else
            {
                using (var db = new DbSprav())
                {
                    try
                    {
                        ret = db.GetSelectedDogForDistribOv(finder);
                    }
                    catch (Exception ex)
                    {
                        ret.result = false;
                        ret.text = "Ошибка вызова функции";
                        MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name +
                                            "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    }
                }
            }
            return ret;
        }
        //----------------------------------------------------------------------
        public Returns InterruptOverpaymentProcess(OverpaymentForDistrib finder)
        //----------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Sprav cli = new cli_Sprav(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.InterruptOverpaymentProcess(finder);
            }
            else
            {
                using (var db = new DbSprav())
                {
                    try
                    {
                        ret = db.InterruptOverpaymentProcess(finder);
                    }
                    catch (Exception ex)
                    {
                        ret.result = false;
                        ret.text = "Ошибка вызова функции";
                        MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name +
                                            "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    }
                }
            }
            return ret;
        }
        //----------------------------------------------------------------------
        public List<House_kodes> GetAliasDomList(House_kodes finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            var list = new List<House_kodes>();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Sprav cli = new cli_Sprav(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.GetAliasDomList(finder, out ret);
            }
            else
            {
                using (DbSprav db = new DbSprav())
                {
                    try
                    {
                        list = db.GetAliasDomList(finder, out ret);
                    }
                    catch (Exception ex)
                    {
                        ret.result = false;
                        MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name +
                                            "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    }
                }
            }
            return list;
        }
        //----------------------------------------------------------------------
        public Returns EditAliasDomList(House_kodes finder)
        //----------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Sprav cli = new cli_Sprav(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.EditAliasDomList(finder);
            }
            else
            {
                using (var db = new DbSprav())
                {
                    try
                    {
                        ret = db.EditAliasDomList(finder);
                    }
                    catch (Exception ex)
                    {
                        ret.result = false;
                        ret.text = "Ошибка вызова функции";
                        MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name +
                                            "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    }
                }
            }
            return ret;
        }
        //----------------------------------------------------------------------
        public Returns DeleteAliasDomList(House_kodes finder)
        //----------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Sprav cli = new cli_Sprav(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.DeleteAliasDomList(finder);
            }
            else
            {
                using (var db = new DbSprav())
                {
                    try
                    {
                        ret = db.DeleteAliasDomList(finder);
                    }
                    catch (Exception ex)
                    {
                        ret.result = false;
                        ret.text = "Ошибка вызова функции";
                        MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name +
                                            "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    }
                }
            }
            return ret;
        }

        public Returns RefreshBanksForContract(ScopeAdress finderScopeAdress)
        {
            //----------------------------------------------------------------------

            Returns ret = Utils.InitReturns();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Sprav cli = new cli_Sprav(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.RefreshBanksForContract(finderScopeAdress);
            }
            else
            {
                try
                {
                    using (DbSupplierNew supplier = new DbSupplierNew())
                    {
                        ret = supplier.RefreshBanksForContract(finderScopeAdress);
                    }
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name +
                                        "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return ret;
        }

        public Returns CheckToOpenServOnLSByAdress(List<ScopeAdress> scopeAdress)
        {
            Returns ret = Utils.InitReturns();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Sprav cli = new cli_Sprav(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.CheckToOpenServOnLSByAdress(scopeAdress);
            }
            else
            {
                try
                {
                    using (DbSprav supplier = new DbSprav())
                    {
                        ret = supplier.CheckToOpenServOnLSByAdress(scopeAdress);
                    }
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name +
                                        "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return ret;
        }
    }
}
