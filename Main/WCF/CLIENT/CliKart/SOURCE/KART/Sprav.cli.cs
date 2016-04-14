using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Net;
using System.Security.Principal;
using System;

using System.Collections.Generic;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Global;

namespace STCLINE.KP50.Client
{
    public class cli_Sprav : I_Sprav
    {
        I_Sprav remoteObject;

        public cli_Sprav(int nzp_server)
            : base()
        {
            _cli_Sprav(nzp_server);
        }

        void _cli_Sprav(int nzp_server)
        {
            string addrHost = "";
            //определить параметры доступа
            _RServer zap = MultiHost.GetServer(nzp_server);

            if (Points.IsMultiHost && nzp_server > 0)
            {
                addrHost = zap.ip_adr + WCFParams.AdresWcfWeb.srvSprav;
                remoteObject = HostChannel.CreateInstance<I_Sprav>(zap.login, zap.pwd, addrHost);
            }
            else
            {
                //по-умолчанию
                addrHost = WCFParams.AdresWcfWeb.Adres + WCFParams.AdresWcfWeb.srvSprav;
                zap.rcentr = "<локально>";
                remoteObject = HostChannel.CreateInstance<I_Sprav>(addrHost);
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

        ~cli_Sprav()
        {
            try { if (remoteObject != null) HostChannel.CloseProxy(remoteObject); }
            catch { }
        }

        //----------------------------------------------------------------------
        public bool TableExists(Finder finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            bool res = false;
            try
            {
                res = remoteObject.TableExists(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка при поиске данных";
                //ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка TableExists " + err, MonitorLog.typelog.Error, 2, 100, true);
            }
            return res;
        }
        //----------------------------------------------------------------------
        public Returns WebDataTable(Finder finder, enSrvOper srv)
        //----------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.WebDataTable(finder, srv);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка при поиске данных";
                //ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка WebDataTable " + err, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }
        //----------------------------------------------------------------------
        public List<_Point> PointLoad_WebData(Finder finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<_Point> list = new List<_Point>();
            try
            {
                list = remoteObject.PointLoad_WebData(finder, out ret);
                HostChannel.CloseProxy(remoteObject);

                return list;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка PointLoad_WebData " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }
        //----------------------------------------------------------------------
        public List<_Point> PointLoad(out Returns ret, out _PointWebData p)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<_Point> list = new List<_Point>();
            try
            {
                list = remoteObject.PointLoad(out ret, out p);
                HostChannel.CloseProxy(remoteObject);

                return list;
            }
            catch (Exception ex)
            {
                p = new _PointWebData(false);
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка PointLoad " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }
        //----------------------------------------------------------------------
        public string GetInfo(long kod, int tip, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                string s = remoteObject.GetInfo(kod, tip, out ret);
                HostChannel.CloseProxy(remoteObject);

                return s;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка GetInfo " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public List<_Service> ServiceLoad(Finder finder, out Returns ret)
        {
            Service service = new Service();
            finder.CopyTo(service);
            return ServiceLoad(service, out ret);
        }

        //----------------------------------------------------------------------
        public List<_Service> ServiceLoad(Service finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                Services spis = new Services();
                spis.ServiceList = remoteObject.ServiceLoad(finder, out ret);
                HostChannel.CloseProxy(remoteObject);

                return spis.ServiceList;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка ServiceLoad " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }
        //----------------------------------------------------------------------
        public List<_Service> CountsLoad(Finder finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                Services spis = new Services();
                spis.CountsList = remoteObject.CountsLoad(finder, out ret);
                HostChannel.CloseProxy(remoteObject);

                return spis.CountsList;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка CountsLoad " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }
        //--------------------------------------------------------------------------------------------------------------------
        public List<_Service> CountsLoadFilter(Finder finder, out Returns ret, int nzp_kvar)
        //--------------------------------------------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                Services spis = new Services();
                spis.CountsList = remoteObject.CountsLoadFilter(finder, out ret, nzp_kvar);
                HostChannel.CloseProxy(remoteObject);

                return spis.CountsList;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка CountsLoad " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }
        //----------------------------------------------------------------------
        public List<_ResY> ResYLoad(out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                //ICommunicationObject proxy = remoteObject as ICommunicationObject;
                //proxy.Open();

                ResYs.ResYList = remoteObject.ResYLoad(out ret);
                HostChannel.CloseProxy(remoteObject);

                return ResYs.ResYList;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                //string msg = "ResYLoad: ";
                //if (Points.IsMultiHost) 
                //{
                //    _RServer zap = MultiHost.GetServer(curt);
                //    msg = "подключения к серверу РЦ "+hadr2
                //}


                MonitorLog.WriteLog("Ошибка ResYLoad:" + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }
        //----------------------------------------------------------------------
        public List<_TypeAlg> TypeAlgLoad(out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                TypeAlgs.AlgList = remoteObject.TypeAlgLoad(out ret);
                HostChannel.CloseProxy(remoteObject);

                return TypeAlgs.AlgList;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка TypeAlgLoad " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }
        //----------------------------------------------------------------------
        public List<_Help> LoadHelp(int nzp_user, int cur_page, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                List<_Help> List = remoteObject.LoadHelp(nzp_user, cur_page, out ret);
                HostChannel.CloseProxy(remoteObject);

                return List;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка LoadHelp " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }


        //----------------------------------------------------------------------
        public List<_Supplier> SupplierLoad(Supplier finder, enTypeOfSupp type, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                List<_Supplier> spis = remoteObject.SupplierLoad(finder, type, out ret);
                HostChannel.CloseProxy(remoteObject);

                return spis;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка SupplierLoad " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }
        public List<_Supplier> SupplierLoad(Finder finder, enTypeOfSupp type, out Returns ret)
        {
            Supplier supp = new Supplier();
            finder.CopyTo(supp);
            return SupplierLoad(supp, type, out ret);
        }

        //----------------------------------------------------------------------
        public List<Payer> PayerLoad(Payer finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            return PayerBankLoad(finder, enSrvOper.Payer, out ret);
        }
        //----------------------------------------------------------------------
        public List<Payer> BankLoad(Payer finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            return PayerBankLoad(finder, enSrvOper.Bank, out ret);
        }
        //----------------------------------------------------------------------
        public List<Payer> PayerBankLoad(Payer finder, enSrvOper oper, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                List<Payer> spis = remoteObject.PayerBankLoad(finder, oper, out ret);
                HostChannel.CloseProxy(remoteObject);

                return spis;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка PayerBankLoad " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        /*
        /// <summary>
        /// Загрузка списка банков(подрядчиков)
        /// </summary>
        public List<Bank> BankLoad(Bank finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                List<Bank> spis = remoteObject.BankLoad(finder, out ret);
                HostChannel.CloseProxy(remoteObject);

                return spis;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка BankLoad " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }
        */

        /// <summary>
        /// Загрузка списка namereg
        /// </summary>
        public List<Namereg> NameregLoad(Namereg finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                List<Namereg> spis = remoteObject.NameregLoad(finder, out ret);
                HostChannel.CloseProxy(remoteObject);

                return spis;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка NameregLoad " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public List<Payer> LoadPayerTypes(Finder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                List<Payer> spis = remoteObject.LoadPayerTypes(finder, out ret);
                HostChannel.CloseProxy(remoteObject);

                return spis;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка LoadPayerTypes " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        /*/// <summary>
        /// Получить файл отчета
        /// </summary>
        public string GetFileRep(Int64 nzp)
        {

            return remoteObject.GetFileRep(nzp);
        }*/


        //потом исправить для мультихостинга
        //----------------------------------------------------------------------
        public static _Point DbGetPoint(int nzp_wp)
        //----------------------------------------------------------------------
        {
            /*
            DbSprav db = new DbSprav();
            _Point res = db.GetPoint(nzp_wp);
            db.Close();
            return res;
            */
            return Points.GetPoint(nzp_wp);
        }

        public _Point DbGetPoint(string pref)
        {
            /*
            DbSprav db = new DbSprav();
            _Point res = db.GetPoint(pref);
            db.Close();
            return res;
            */
            HostChannel.CloseProxy(remoteObject);
            return Points.GetPoint(pref);
        }


        public string DbGetSpravFile(out Returns ret, Int64 nzp_act)
        {
            DbSpravClient db = new DbSpravClient();
            string res = db.GetSpravFile(out ret, nzp_act);
            db.Close();
            return res;
        }


        //----------------------------------------------------------------------
        public List<_Service> DbServiceLoad_WebData(Finder finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            DbSpravClient db = new DbSpravClient();
            List<_Service> list = db.ServiceLoad_WebData(finder, out ret);
            db.Close();
            return list;
        }
        //----------------------------------------------------------------------
        public List<_Supplier> DbSupplierLoad_WebData(Finder finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            DbSpravClient db = new DbSpravClient();
            List<_Supplier> list = db.SupplierLoad_WebData(finder, out ret);
            db.Close();
            return list;
        }
        //----------------------------------------------------------------------
        public List<int> DbPointByAreaLoad(Dom finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            DbSpravClient db = new DbSpravClient();
            List<int> list = db.PointByAreaLoad(finder, out ret);
            db.Close();
            return list;
        }
        //----------------------------------------------------------------------
        public List<Payer> DbPayerBankLoad_WebData(Payer finder, bool is_bank, out Returns ret)
        //----------------------------------------------------------------------
        {
            DbSpravClient db = new DbSpravClient();
            List<Payer> list = db.PayerBankLoad_WebData(finder, is_bank, out ret);
            db.Close();
            return list;
        }


        public List<_Service> DbServiceAvailableForRole(Role finder, out Returns ret)
        {
            DbSpravClient db = new DbSpravClient();
            List<_Service> res = db.ServiceAvailableForRole(finder, out ret);
            db.Close();
            return res;
        }
        public List<_Supplier> DbSupplierAvailableForRole(Role finder, out Returns ret)
        {
            DbSpravClient db = new DbSpravClient();
            List<_Supplier> res = db.SupplierAvailableForRole(finder, out ret);
            db.Close();
            return res;
        }
        public List<_Point> DbPointAvailableForRole(Role finder, out Returns ret)
        {
            DbSpravClient db = new DbSpravClient();
            List<_Point> res = db.PointAvailableForRole(finder, out ret);
            db.Close();
            return res;
        }
        public List<_Prm> DbPrmAvailableForRole(Role finder, out Returns ret)
        {
            DbSpravClient db = new DbSpravClient();
            List<_Prm> res = db.PrmAvailableForRole(finder, out ret);
            db.Close();
            return res;
        }


        public List<_Help> DbLoadHelp(int nzp_user, int cur_page, out Returns ret)
        {
            DbSpravClient db = new DbSpravClient();
            List<_Help> res = db.LoadHelp(nzp_user, cur_page, out ret);
            db.Close();
            return res;
        }
        
        public static void DbStartApp()
        {
            Constants.Pages.Clear();
            Constants.PageShow.Clear();
            Constants.Actions.Clear();
            Constants.ActShow.Clear();

            DbSpravClient db = new DbSpravClient();
            try
            {
                db.StartApp();
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка при старте веб-приложения\n" + ex.Message, MonitorLog.typelog.Error, true);
            }
            finally
            {
                db.Close();
            }
        }


        public Returns InitApplication()
        {
            Returns ret = Utils.InitReturns();

            if (Points.IsMultiHost)
            {
                //загрузка списка доступных хостов
                //DbStartMulti(out ret);
                DbMultiHostClient db = new DbMultiHostClient();
                ret = db.LoadMultiHost();
                db.Close();

                Points.isInitSuccessfull = true;
            }
            else
            {
                Points.PointList.Clear();
                ResYs.ResYList.Clear();

                //загрузка данных из основной базы (points,расчетные месяцы, etc)
                _PointWebData p = new _PointWebData(false);

                try
                {
                    Points.PointList = remoteObject.PointLoad(out ret, out p);
                }
                catch (Exception ex)
                {
                    Points.isInitSuccessfull = false;

                    ret.result = false;
                    if (ex is System.ServiceModel.EndpointNotFoundException)
                    {
                        ret.text = Constants.access_error;
                        ret.tag = Constants.access_code;
                    }
                    else
                    {
                        ret.text = ex.Message;
                    }
                    MonitorLog.WriteLog("Ошибка InitApplication()" + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                }
                Points.SetPointWebData(p);

                if (Points.PointList == null || Points.PointList.Count == 0) //нет доступа к сервису, заполним по-умолчанию
                {
                    _Point zap = new _Point();
                    zap.nzp_wp = Constants.DefaultZap; ;
                    zap.point = "Локальный банк";
                    zap.pref = Points.Pref; ;

                    Points.PointList = new List<_Point>();
                    Points.PointList.Add(zap);
                }

                //состояние лицевого счета
                if (ret.result)
                {
                    try
                    {
                        ResYs.ResYList = remoteObject.ResYLoad(out ret);
                    }
                    catch (Exception ex)
                    {
                        Points.isInitSuccessfull = false;
                        ret.result = false;
                        if (ex is System.ServiceModel.EndpointNotFoundException)
                        {
                            ret.text = Constants.access_error;
                            ret.tag = Constants.access_code;
                        }
                        else
                        {
                            ret.text = ex.Message;
                        }
                        MonitorLog.WriteLog("Ошибка InitApplication()" + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                    }
                }

                if (ResYs.ResYList == null || ResYs.ResYList.Count == 0) //нет доступа к сервису, заполним по-умолчанию
                {
                    _ResY zap = new _ResY();
                    zap.nzp_res = Constants.DefaultZap; ;
                    zap.nzp_y = Constants.DefaultZap; ;
                    zap.name_y = "-";

                    ResYs.ResYList = new List<_ResY>();
                    ResYs.ResYList.Add(zap);
                }
            }

            return ret;
        }

        public Returns SaveSupplier(Supplier finder)
        {
            Returns ret;
            try
            {
                ret = remoteObject.SaveSupplier(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка SaveSupplier\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public Returns SavePayer(Payer finder)
        {
            Returns ret;
            try
            {
                ret = remoteObject.SavePayer(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка SavePayer\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public Returns SaveBank(Bank finder)
        {
            Returns ret;
            try
            {
                ret = remoteObject.SaveBank(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка SaveBank\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public PackDistributionParameters GetPackDistributionParameters(out Returns ret)
        {
            try
            {
                PackDistributionParameters prm = remoteObject.GetPackDistributionParameters(out ret);
                HostChannel.CloseProxy(remoteObject);
                return prm;
            }
            catch (Exception ex)
            {
                ret = new Returns(false);

                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка GetPackDistributionParameters\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public static PackDistributionParameters GetPackDistributionParameters(int nzp_server, out Returns ret)
        {
            ret = new Returns(true);
            if (Points.packDistributionParameters != null) return Points.packDistributionParameters;
            else
            {
                cli_Sprav cli = new cli_Sprav(nzp_server);
                return cli.GetPackDistributionParameters(out ret);
            }
        }

        public Returns RefreshSpravClone(Finder finder)
        {
            Returns ret;
            try
            {
                ret = remoteObject.RefreshSpravClone(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка RefreshSpravClone\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public List<Town> LoadTown(Town finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                List<Town> spis = remoteObject.LoadTown(finder, out ret);
                HostChannel.CloseProxy(remoteObject);

                return spis;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка LoadTown " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public List<_reestr_unloads> LoadUploadedReestrList(Finder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                List<_reestr_unloads> spis = remoteObject.LoadUploadedReestrList(finder, out ret);
                HostChannel.CloseProxy(remoteObject);

                return spis;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка LoadUploadedReestrList " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public List<unload_exchange_sz> LoadListExchangeSZ(Finder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                List<unload_exchange_sz> spis = remoteObject.LoadListExchangeSZ(finder, out ret);
                HostChannel.CloseProxy(remoteObject);

                return spis;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка LoadListExchangeSZ " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public List<_reestr_downloads> LoadDownloadedReestrList(Finder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                List<_reestr_downloads> spis = remoteObject.LoadDownloadedReestrList(finder, out ret);
                HostChannel.CloseProxy(remoteObject);

                return spis;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка LoadDownloadedReestrList " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public List<Measure> LoadMeasure(Measure finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                List<Measure> spis = remoteObject.LoadMeasure(finder, out ret);
                HostChannel.CloseProxy(remoteObject);

                return spis;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка LoadMeasure " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public List<CalcMethod> LoadCalcMethod(CalcMethod finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                List<CalcMethod> spis = remoteObject.LoadCalcMethod(finder, out ret);
                HostChannel.CloseProxy(remoteObject);

                return spis;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка LoadCalcMethod " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public List<Land> LoadLand(Land finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                List<Land> spis = remoteObject.LoadLand(finder, out ret);
                HostChannel.CloseProxy(remoteObject);

                return spis;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка LoadLand " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public List<Stat> LoadStat(Stat finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                List<Stat> spis = remoteObject.LoadStat(finder, out ret);
                HostChannel.CloseProxy(remoteObject);

                return spis;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка LoadStat " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public List<Town> LoadTown2(Town finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                List<Town> spis = remoteObject.LoadTown2(finder, out ret);
                HostChannel.CloseProxy(remoteObject);

                return spis;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка LoadTown2 " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public List<Rajon> LoadRajon(Rajon finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                List<Rajon> spis = remoteObject.LoadRajon(finder, out ret);
                HostChannel.CloseProxy(remoteObject);

                return spis;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка LoadRajon " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public List<PackTypes> LoadPackTypes(PackTypes finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                List<PackTypes> spis = remoteObject.LoadPackTypes(finder, out ret);
                HostChannel.CloseProxy(remoteObject);

                return spis;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка LoadPackTypes " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public Returns DeleteReestrTula(_reestr_unloads finder)
        {
            Returns ret;
            ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.DeleteReestrTula(finder);
                HostChannel.CloseProxy(remoteObject);

                return ret;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка DeleteReestrTula " + err, MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }
        public Returns DeleteDownloadReestrTula(Finder finder, int nzp_download)
        {
            Returns ret;
            ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.DeleteDownloadReestrTula(finder, nzp_download);
                HostChannel.CloseProxy(remoteObject);

                return ret;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка DeleteDownloadReestrTula " + err, MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }

        //----------------------------------------------------------------------
        public List<BankPayers> BankPayersLoad(Supplier finder, enTypeOfSupp type, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                List<BankPayers> spis = remoteObject.BankPayersLoad(finder, type, out ret);
                HostChannel.CloseProxy(remoteObject);

                return spis;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка SupplierLoad " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        //----------------------------------------------------------------------
        public List<BankPayers> BankPayersLoadBC(Supplier finder, enTypeOfSupp type, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                List<BankPayers> spis = remoteObject.BankPayersLoadBC(finder, type, out ret);
                HostChannel.CloseProxy(remoteObject);

                return spis;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка BankPayersLoadBC " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public List<Supplier> LoadSupplierSpis(Area_ls finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                List<Supplier> spis = remoteObject.LoadSupplierSpis(finder, out ret);
                HostChannel.CloseProxy(remoteObject);

                return spis;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка LoadSupplierSpis " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        //----------------------------------------------------------------------
        public List<Bank> GetBankType(Bank finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                List<Bank> spis = remoteObject.GetBankType(finder,out ret);
                HostChannel.CloseProxy(remoteObject);

                return spis;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка SupplierLoad " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public List<BCTypes> LoadBCTypes(BCTypes finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                List<BCTypes> spis = remoteObject.LoadBCTypes(finder, out ret);
                HostChannel.CloseProxy(remoteObject);

                return spis;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка LoadBCTypes " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public List<Formuls> GetFormuls(FormulsFinder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                List<Formuls> spis = remoteObject.GetFormuls(finder, out ret);
                HostChannel.CloseProxy(remoteObject);

                return spis;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка GetFormuls\n " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }
    }
}
