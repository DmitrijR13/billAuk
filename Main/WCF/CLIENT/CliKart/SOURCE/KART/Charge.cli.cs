using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.DataBase;
using System.Data;

namespace STCLINE.KP50.Client
{
    public class cli_Charge : I_Charge
    {
        I_Charge remoteObject;

        public cli_Charge(int nzp_server)
            : base()
        {
            _cli_Charge("", nzp_server);
        }

        public cli_Charge(string timespan, int nzp_server)
            : base()
        {
            _cli_Charge(timespan, nzp_server);
        }

        void _cli_Charge(string timespan, int nzp_server)
        {
            if (timespan != "") HostChannel.timespan = timespan;
            if (Points.IsMultiHost && nzp_server > 0)
            {
                //определить параметры доступа
                _RServer zap = MultiHost.GetServer(nzp_server);
                remoteObject = HostChannel.CreateInstance<I_Charge>(zap.login, zap.pwd, zap.ip_adr + WCFParams.AdresWcfWeb.srvCharge);
            }
            else
            {
                //по-умолчанию
                remoteObject = HostChannel.CreateInstance<I_Charge>(WCFParams.AdresWcfWeb.Adres + WCFParams.AdresWcfWeb.srvCharge);
            }
        }

        ~cli_Charge()
        {
            try { if (remoteObject != null) HostChannel.CloseProxy(remoteObject); }
            catch { }
        }

        List<Saldo> saldoSpis = new List<Saldo>();
        List<Charge> chargeSpis = new List<Charge>();

        //----------------------------------------------------------------------
        public Returns CalcChargeDom(Dom finder, bool reval)
        //----------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();

            try
            {
                ret = remoteObject.CalcChargeDom(finder, reval);
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
                else ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка CalcChargeDom " + err, MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }
        /// <summary>
        /// расчет по списку домов
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="reval"></param>
        /// <returns></returns>
        public Returns CalcChargeListDom(Dom finder, bool reval)
        //----------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();

            try
            {
                ret = remoteObject.CalcChargeListDom(finder, reval);
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
                else ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка CalcChargeListDom " + err, MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }
        //----------------------------------------------------------------------
        public Returns CalcChargeLs(Ls finder, bool reval, bool again, string alias, int id_bill)
        //----------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();

            try
            {
                ret = remoteObject.CalcChargeLs(finder, reval, again, alias, id_bill);
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
                else ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка CalcChargeLs " + err, MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }
        //----------------------------------------------------------------------
        public List<Charge> GetCharge(ChargeFind finder, enSrvOper oper, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                chargeSpis = remoteObject.GetCharge(finder, oper, out ret);
                HostChannel.CloseProxy(remoteObject);
                return chargeSpis;
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

                MonitorLog.WriteLog("Ошибка GetCharge(" + oper.ToString() + ") " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }
        //----------------------------------------------------------------------
        public List<PrmTypes> LoadUsersPercentDom(Finder finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            List<PrmTypes> list =new List<PrmTypes>();
            ret = Utils.InitReturns();
            try
            {
                list = remoteObject.LoadUsersPercentDom(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return list;
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

                MonitorLog.WriteLog("Ошибка  " + System.Reflection.MethodBase.GetCurrentMethod().Name + err,
                    MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }
        //----------------------------------------------------------------------
        public List<PrmTypes> LoadOperTypesPercentDom(Finder finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            List<PrmTypes> list = new List<PrmTypes>();
            ret = Utils.InitReturns();
            try
            {
                list = remoteObject.LoadOperTypesPercentDom(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return list;
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

                MonitorLog.WriteLog("Ошибка  " + System.Reflection.MethodBase.GetCurrentMethod().Name + err,
                    MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        //----------------------------------------------------------------------
        _RecordSzFin CallSrvCalcSz(ChargeFind finder, enSrvOper oper, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            _RecordSzFin zap = new _RecordSzFin();

            zap.ls_edv = 0;
            zap.ls_lgota = 0;
            zap.ls_smo = 0;
            zap.ls_teplo = 0;

            try
            {
                zap = remoteObject.GetCalcSz(finder, oper, out ret);
                HostChannel.CloseProxy(remoteObject);
                return zap;
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
                    ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка CallSrvCalcSz(" + oper.ToString() + ") " + err, MonitorLog.typelog.Error, 2, 100, true);
                return zap;
            }
        }

        public _RecordSzFin GetCalcSz(ChargeFind finder, enSrvOper oper, out Returns ret)
        {
            return CallSrvCalcSz(finder, oper, out ret);
        }

        //----------------------------------------------------------------------
        public List<SaldoRep> FillRep(ChargeFind finder, out Returns ret, int num_rep)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<SaldoRep> ListRecRep = new List<SaldoRep>();
            try
            {
                ListRecRep = remoteObject.FillRep(finder, out ret, num_rep);
                HostChannel.CloseProxy(remoteObject);

                return ListRecRep;
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

                MonitorLog.WriteLog("Ошибка FillRep " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }


        public List<_RecordDomODN> FillRepProtokolOdn(ChargeFind finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<_RecordDomODN> ListRecRep = new List<_RecordDomODN>();
            try
            {
                ListRecRep = remoteObject.FillRepProtokolOdn(finder, out ret);
                HostChannel.CloseProxy(remoteObject);

                return ListRecRep;
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

                MonitorLog.WriteLog("Ошибка FillRep " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        //----------------------------------------------------------------------
        public Returns PrepareReport(ChargeFind finder, int num_rep)
        //----------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.PrepareReport(finder, num_rep);
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
                else ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка FillRep " + err, MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }

        //----------------------------------------------------------------------
        public List<SaldoRep> FillRepServ(ChargeFind finder, out Returns ret, int num_rep)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<SaldoRep> ListRecRep = new List<SaldoRep>();
            try
            {
                ListRecRep = remoteObject.FillRepServ(finder, out ret, num_rep);
                HostChannel.CloseProxy(remoteObject);

                return ListRecRep;
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

                MonitorLog.WriteLog("Ошибка FillRepServ " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }
        //----------------------------------------------------------------------
        public List<Saldo> GetSaldo(Saldo finder, out Returns ret, out _RecordSaldo itog)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            try
            {
                saldoSpis = remoteObject.GetSaldo(finder, out ret, out itog);
                HostChannel.CloseProxy(remoteObject);

                return saldoSpis;
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

                MonitorLog.WriteLog("Ошибка GetSaldo " + err, MonitorLog.typelog.Error, 2, 100, true);
                itog = new _RecordSaldo();
                return null;
            }
        }
        //----------------------------------------------------------------------
        public void SaldoFon()
        //----------------------------------------------------------------------
        {
            try
            {
                remoteObject.SaldoFon();
                HostChannel.CloseProxy(remoteObject); //
            }
            catch (Exception ex)
            {
                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка SaldoFon " + err, MonitorLog.typelog.Error, 2, 100, true);
            }
        }

        public List<MoneyDistrib> GetMoneyDistrib(MoneyDistrib finder, enSrvOper oper, out Returns ret)
        {
            try
            {
                List<MoneyDistrib> list = remoteObject.GetMoneyDistrib(finder, oper, out ret);
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
                MonitorLog.WriteLog("Ошибка GetMoneyDistrib(" + oper.ToString() + ") " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                //ret = new Returns(false, ex.Message);
                return null;
            }
        }

        public List<MoneyDistrib> GetMoneyDistribDom(MoneyDistrib finder, enSrvOper oper, out Returns ret)
        {
            try
            {
                List<MoneyDistrib> list = remoteObject.GetMoneyDistribDom(finder, oper, out ret);
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
                MonitorLog.WriteLog("Ошибка GetMoneyDistribDom(" + oper.ToString() + ") " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                //ret = new Returns(false, ex.Message);
                return null;
            }
        }

        public List<MoneySended> GetMoneySended(MoneySended finder, enSrvOper oper, out Returns ret)
        {
            try
            {
                List<MoneySended> list = remoteObject.GetMoneySended(finder, oper, out ret);
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
                MonitorLog.WriteLog("Ошибка GetMoneySended(" + oper.ToString() + ") " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public List<MoneyNaud> GetMoneyNaud(MoneyNaud finder, enSrvOper oper, out Returns ret)
        {
            try
            {
                List<MoneyNaud> list = remoteObject.GetMoneyNaud(finder, oper, out ret);
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
                MonitorLog.WriteLog("Ошибка GetMoneyNaud(" + oper.ToString() + ") " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                //ret = new Returns(false, ex.Message);
                return null;
            }
        }

        public List<Perekidka> LoadPerekidki(Perekidka finder, out Returns ret)
        {
            try
            {
                List<Perekidka> list = remoteObject.LoadPerekidki(finder, out ret);
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
                MonitorLog.WriteLog("Ошибка LoadPerekidki " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }
        public List<DelSupplier> LoadPerekidkiOplatami(DelSupplier finder, out Returns ret)
        {
            try
            {
                List<DelSupplier> list = remoteObject.LoadPerekidkiOplatami(finder, out ret);
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
                MonitorLog.WriteLog("Ошибка LoadPerekidkiOplatami " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public Returns SavePerekidkiOplatami(DelSupplier finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.SavePerekidkiOplatami(finder);
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
                MonitorLog.WriteLog("Ошибка SavePerekidkiOplatami " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }

        public Returns DeletePerekidkaOplatami(DelSupplier finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.DeletePerekidkaOplatami(finder);
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
                MonitorLog.WriteLog("Ошибка DeletePerekidkaOplatami " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }

        public Returns SavePerekidka(Perekidka finder)
        {
            try
            {
                Returns ret = remoteObject.SavePerekidka(finder);
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
                MonitorLog.WriteLog("Ошибка SavePerekidka " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }

        public Returns DeletePerekidka(Perekidka finder)
        {
            try
            {
                Returns ret = remoteObject.DeletePerekidka(finder);
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
                MonitorLog.WriteLog("Ошибка DeletePerekidka " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }

        public List<Perekidka> LoadSumsPerekidkaLs(Perekidka finder, out Returns ret)
        {
            try
            {
                List<Perekidka> list = remoteObject.LoadSumsPerekidkaLs(finder, out ret);
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
                MonitorLog.WriteLog("Ошибка LoadSumsPerekidkaLs " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public Returns SaveSumsPerekidkaLs(List<Perekidka> listfinder)
        {
            try
            {
                Returns ret = remoteObject.SaveSumsPerekidkaLs(listfinder);
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
                MonitorLog.WriteLog("Ошибка SaveSumsPerekidkaLs " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }

        #region Отчеты
        ////-----------------------------------------------------------------------------------------------------
        //public string GetLicChetData1(Object rep ,ref Kart finder, out Returns ret, int y, int m, DateTime date)
        ////-----------------------------------------------------------------------------------------------------
        //{
        //    ret = Utils.InitReturns();
        //    ReportS report = new ReportS();
        //    int y_ = y - 2000;
        //    //Список начислений
        //    List<ChargeFind> list = remoteObject.GetLicChetData(ref finder, out ret, y_, m);

        //    try
        //    {
        //        ret.result = true;               
        //        return report.Fill_licShet((Report)rep, y, m, date, finder, list);
        //    }
        //    catch
        //    {
        //        ret.text = "Ошибка заполнения отчета.";
        //        ret.result = false;
        //        return "";
        //    }
        //}
        //------------------------------------------------------------------------------------
        public List<Charge> GetLicChetData(ref Kart finder, out Returns ret, int y, int m)
        //------------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            return remoteObject.GetLicChetData(ref finder, out ret, y, m);
        }

        ////------------------------------------------------------------------------------------------------
        //public string Fill_web_fin_ls(Object rep, out Returns ret, int y, int m, ChargeFind finder)
        ////------------------------------------------------------------------------------------------------
        //{
        //    ret = Utils.InitReturns();
        //    ReportS report = new ReportS();
        //    int y_ = y - 2000;
        //    //Список
        //    List<Charge> charges = null;
        //    charges = this.GetCharge(finder, enSrvOper.SrvGetBillCharge, out ret);

        //    try
        //    {
        //        ret.result = true;
        //        return report.Fill_web_fin_ls((Report)rep, y, m,finder,charges);
        //    }
        //    catch
        //    {
        //        ret.text = "Ошибка заполнения отчета.";
        //        ret.result = false;
        //        return "";
        //    }

        //}

        ////---------------------------------------------------------------------------------------------------
        //public string Fill_web_s_nodolg(Object rep, out Returns ret, int y, int m, ChargeFind finder)
        ////---------------------------------------------------------------------------------------------------
        //{
        //    ret = Utils.InitReturns();
        //    ReportS report = new ReportS();
        //    int y_ = y - 2000;
        //    //Список
        //    List<Charge> charges = null;
        //    charges = this.GetCharge(finder, enSrvOper.SrvGetBillCharge, out ret);

        //    try
        //    {
        //        ret.result = true;
        //        return report.Fill_web_s_nodolg ((Report)rep, y, m, finder, charges);
        //    }
        //    catch
        //    {
        //        ret.text = "Ошибка заполнения отчета.";
        //        ret.result = false;
        //        return "";
        //    }
        //}
        ////---------------------------------------------------------------------------------------------------
        //public string Fill_web_sparv_nach(Object rep, out Returns ret, int y_, int m_, ChargeFind finder)
        ////---------------------------------------------------------------------------------------------------
        //{
        //    ret = Utils.InitReturns();
        //    ReportS report = new ReportS();          
        //    //Список
        //    List<Charge> charges = null;
        //    charges = this.GetCharge(finder, enSrvOper.SrvGet, out ret);

        //    try
        //    {
        //        ret.result = true;
        //        return report.Fill_web_sparv_nach((Report)rep, y_, m_, finder, charges);
        //    }
        //    catch
        //    {
        //        ret.text = "Ошибка заполнения отчета.";
        //        ret.result = false;
        //        return "";
        //    }
        //}

        ////--------------------------------------------------------------------------------------------------
        //public string Fill_web_saldo_rep5_10(Object rep, out Returns ret, int y_, int m_, ChargeFind finder)
        ////--------------------------------------------------------------------------------------------------
        //{
        //    ret = Utils.InitReturns();
        //    ReportS report = new ReportS();
        //    //Список
        //    List<SaldoRep> listRepData = null;
        //    listRepData = this.FillRepServ(finder, out ret, 1);
        //    try
        //    {
        //        ret.result = true;
        //        return report.Fill_web_saldo_rep5_10((Report)rep, y_, m_, finder, listRepData);
        //    }
        //    catch
        //    {
        //        ret.text = "Ошибка заполнения отчета.";
        //        ret.result = false;
        //        return "";
        //    }
        //}
        ////--------------------------------------------------------------------------------------------------
        //public string Fill_web_saldo_rep5_20(Object rep, out Returns ret, int y_, int m_, ChargeFind finder)
        ////--------------------------------------------------------------------------------------------------
        //{
        //    ret = Utils.InitReturns();
        //    ReportS report = new ReportS();
        //    //Список
        //    List<SaldoRep> listRepData = null;
        //    listRepData = this.FillRep(finder, out ret, 2);
        //    try
        //    {
        //        ret.result = true;
        //        return report.Fill_web_saldo_rep5_20((Report)rep, y_, m_, finder, listRepData);
        //    }
        //    catch
        //    {
        //        ret.text = "Ошибка заполнения отчета.";
        //        ret.result = false;
        //        return "";
        //    }
        //}



        #endregion

        public bool IsTableFilledIn(ChargeFind finder, TableName table, out Returns ret)
        {
            try
            {
                bool result = remoteObject.IsTableFilledIn(finder, table, out ret);
                HostChannel.CloseProxy(remoteObject);
                return result;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка IsTableFilledIn(" + table.ToString() + ") " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                ret = new Returns(false, ex.Message);
                return false;
            }
        }

        public string DbMakeWhereString(List<ChargeFind> finder, out Returns ret, enDopFindType tip)
        {
            DbChargeClient db = new DbChargeClient();
            string s = db.MakeWhereString(finder, out ret, tip);
            db.Close();
            return s;
        }

        public string DbMakeWhereStringDop(List<ChargeFind> finder, out Returns ret, enDopFindType tip)
        {
            DbChargeClient db = new DbChargeClient();
            string s = db.MakeWhereString2(finder, out ret, tip);
            db.Close();
            return s;
        }

        //для FBD
        //public List<SaldoRep> FillRep_5_10(ChargeFind finder, out Returns ret, int num_rep)
        //{
        //    List<SaldoRep> retList = null;
        //    ret = Utils.InitReturns();
        //    try
        //    {
        //        retList = remoteObject.FillRep_5_10(finder, out ret, num_rep);
        //        HostChannel.CloseProxy(remoteObject);
        //        return retList;
        //    }
        //    catch (Exception ex)
        //    {
        //        ret.result = false;
        //        if (ex is System.ServiceModel.EndpointNotFoundException)
        //        {
        //            ret.text = Constants.access_error;
        //            ret.tag = Constants.access_code;
        //        }
        //        else ret.text = ex.Message;
        //        MonitorLog.WriteLog("Ошибка SavePerekidka " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
        //        return null;
        //    }
        //}

        /*public decimal GetSumKOplate(Saldo finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                decimal sumKOplate = remoteObject.GetSumKOplate(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return sumKOplate;
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

                MonitorLog.WriteLog("Ошибка GetSumKOplate() " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return 0;
            }
        }*/

        public ReturnsObjectType<DataTable> LoadSended(MoneySended finder)
        {
            try
            {
                ReturnsObjectType<DataTable> rez = remoteObject.LoadSended(finder);
                HostChannel.CloseProxy(remoteObject);
                return rez;
            }
            catch (Exception ex)
            {
                ReturnsObjectType<DataTable> rez = new ReturnsObjectType<DataTable>();
                rez.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    rez.text = Constants.access_error;
                    rez.tag = Constants.access_code;
                }
                else rez.text = ex.Message;
                MonitorLog.WriteLog("Ошибка LoadSended " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return rez;
            }
        }

        public Returns SaveMoneySended(List<MoneySended> list)
        {
            Returns ret;
            try
            {
                ret = remoteObject.SaveMoneySended(list);
                HostChannel.CloseProxy(remoteObject);
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

                MonitorLog.WriteLog("Ошибка SaveMoneySended() " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public List<FnPercent> GetFnPercent(FnPercent finder, enSrvOper oper, out Returns ret)
        {
            try
            {
                List<FnPercent> list = remoteObject.GetFnPercent(finder, oper, out ret);
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
                MonitorLog.WriteLog("Ошибка GetFnPercent(" + oper.ToString() + ") " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                //ret = new Returns(false, ex.Message);
                return null;
            }
        }

        public Returns SaveFnPercent(FnPercent finder)
        {
            try
            {
                Returns ret = remoteObject.SaveFnPercent(finder);
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
                MonitorLog.WriteLog("Ошибка SaveFnPercent " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }

        public Returns SaveFnPercentDom(FnPercent finder)
        {
            try
            {
                Returns ret = remoteObject.SaveFnPercentDom(finder);
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
                MonitorLog.WriteLog("Ошибка SaveFnPercentDom " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }

        public Returns DelFnPercent(FnPercent finder)
        {
            try
            {
                Returns ret = remoteObject.DelFnPercent(finder);
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
                MonitorLog.WriteLog("Ошибка DelFnPercent " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }

        public Returns DelFnPercentDom(FnPercent finder)
        {
            try
            {
                Returns ret = remoteObject.DelFnPercentDom(finder);
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
                MonitorLog.WriteLog("Ошибка DelFnPercentDom " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }

        public ReturnsType UpdateKredit(string pref, int inCalcYear, int inCalcMonth)
        {
            ReturnsType ret = new ReturnsType();
            
            try
            {
                ret = remoteObject.UpdateKredit(pref, inCalcYear, inCalcMonth);
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
                else ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка UpdateKredit() " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public List<Credit> GetCredit(Credit finder, out Returns ret)
        {
            try
            {
                List<Credit> list = remoteObject.GetCredit(finder, out ret);
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
                MonitorLog.WriteLog("Ошибка GetCredit() " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public void SaveCredit(Credit finder, out Returns ret)
        {
            try
            {
                remoteObject.SaveCredit(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
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
                MonitorLog.WriteLog("Ошибка SaveCredit() " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
            }
        }


        public List<CreditDetails> GetCreditDetails(CreditDetails finder, out Returns ret)
        {
            try
            {
                List<CreditDetails> list = remoteObject.GetCreditDetails(finder, out ret);
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
                MonitorLog.WriteLog("Ошибка GetCreditDetails() " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public Returns IsAllowCorrectSaldo(Ls finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.IsAllowCorrectSaldo(finder);
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
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка IsAllowCorrectSaldo() " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }

        public Returns SaveCorrectSaldo(List<Saldo> finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.SaveCorrectSaldo(finder);
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
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка SaveCorrectSaldo() " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }

        public Returns MakeProtCalc(Calcs finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.MakeProtCalc(finder);
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
                else ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка MakeProtCalc() " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }

        public bool IsNowCalcCharge(long nzp_dom, string pref, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                bool b = remoteObject.IsNowCalcCharge(nzp_dom, pref, out ret);
                HostChannel.CloseProxy(remoteObject);
                return b;
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

                MonitorLog.WriteLog("Ошибка MakeProtCalc() " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return false;
            }
        }

        public ReturnsType MakeOperation(ChargeOperations operation, Finder finder)
        {
            ReturnsType ret = new ReturnsType();
            try
            {
                ret = remoteObject.MakeOperation(operation, finder);
                HostChannel.CloseProxy(remoteObject);
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

                MonitorLog.WriteLog("Ошибка MakeOperation(" + operation + ")\n " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public List<TypeRcl> LoadTypeRcl(TypeRcl finder, out Returns ret)
        {
            try
            {
                List<TypeRcl> list = remoteObject.LoadTypeRcl(finder, out ret);
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
                MonitorLog.WriteLog("Ошибка LoadTypeRcl " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public List<TypeDoc> LoadTypeDoc(TypeDoc finder, out Returns ret)
        {
            try
            {
                List<TypeDoc> list = remoteObject.LoadTypeDoc(finder, out ret);
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
                MonitorLog.WriteLog("Ошибка LoadTypeDoc " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public List<CheckChMon> LoadCheckChMon(CheckChMon finder, out Returns ret)
        {
            try
            {
                List<CheckChMon> list = remoteObject.LoadCheckChMon(finder, out ret);
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
                MonitorLog.WriteLog("Ошибка LoadCheckChMon " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public List<GroupsPerekidki> PrepareSplsPerekidki(ParamsForGroupPerekidki finder, out Returns ret)
        {
            try
            {
                List<GroupsPerekidki> list = remoteObject.PrepareSplsPerekidki(finder, out ret);
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
                MonitorLog.WriteLog("Ошибка PrepareSplsPerekidki " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public Returns BackComments()
        {
            Returns ret;
            try
            {
                ret = remoteObject.BackComments();
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
                MonitorLog.WriteLog("Ошибка BackComments " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }

        public Returns PerenosReestrPerekidok()
        {
            Returns ret;
            try
            {
                ret = remoteObject.PerenosReestrPerekidok();
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
                MonitorLog.WriteLog("Ошибка PerenosReestrPerekidok " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }

        public Returns SaveGroupPerekidki(ParamsForGroupPerekidki finder)
        {
            Returns ret;
            try
            {
                ret = remoteObject.SaveGroupPerekidki(finder);
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
                MonitorLog.WriteLog("Ошибка SaveGroupPerekidki " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }

        public List<ParamsForGroupPerekidki> LoadReestrPerekidok(ParamsForGroupPerekidki finder, out Returns ret)
        {
            try
            {
                List<ParamsForGroupPerekidki> list = remoteObject.LoadReestrPerekidok(finder, out ret);
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
                MonitorLog.WriteLog("Ошибка LoadReestrPerekidok " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public Returns DeleteFromReestrPerekidok(ParamsForGroupPerekidki finder)
        {
            Returns ret;
            try
            {
                ret = remoteObject.DeleteFromReestrPerekidok(finder);
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
                MonitorLog.WriteLog("Ошибка DeleteFromReestrPerekidok " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }

        public List<PerekidkaLsToLs> LoadSumsForPerekidkaLsToLs(PerekidkaLsToLs finder, out Returns ret)
        {
            try
            {
                List<PerekidkaLsToLs> list = remoteObject.LoadSumsForPerekidkaLsToLs(finder, out ret);
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
                MonitorLog.WriteLog("Ошибка LoadSumsForPerekidkaLsToLs " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public Returns SavePerekidkiLsToLs (List<PerekidkaLsToLs> listfinder, ParamsForGroupPerekidki reestr)
        {
            Returns ret;
            try
            {
                ret = remoteObject.SavePerekidkiLsToLs(listfinder, reestr);
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
                MonitorLog.WriteLog("Ошибка SavePerekidkiLsToLs " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }

        public Returns FindLsForReestrPerekidok(ParamsForGroupPerekidki finder)
        {
            Returns ret;
            try
            {
                ret = remoteObject.FindLsForReestrPerekidok(finder);
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
                MonitorLog.WriteLog("Ошибка FindLsForReestrPerekidok\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }

        public Returns GetNachisl(int mode, int month_, int year_, int user_)
        {
            Returns ret;
            try
            {
                ret = remoteObject.GetNachisl(mode, month_, year_, user_);
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
                MonitorLog.WriteLog("Ошибка GetNachisl\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }
         
        public List<OverPayment> GetOverPayments(OverPayment finder, enSrvOper srv, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<OverPayment> Spis = null;
            try
            {
                Spis = remoteObject.GetOverPayments(finder, srv, out ret);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                ret.result = false;

                MonitorLog.WriteLog("Ошибка GetOverPayments (" + srv + ") \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                Spis = null;
            }

            return Spis;
        }

        public Returns ChangeMarksSpisOverPayment(Finder finder, List<OverPayment> list0, List<OverPayment> list1)
        {
            DbChargeClient db = new DbChargeClient();
            Returns ret = db.ChangeMarksSpisOverPayment(finder, list0, list1);
            db.Close();
            return ret;
        }

        public Returns SaveAddressToForOverPay(OverPayment finder)
        {
            Returns ret;
            try
            {
                ret = remoteObject.SaveAddressToForOverPay(finder);
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
                MonitorLog.WriteLog("Ошибка SaveAddressToForOverPay\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }

        public List<PeniNoCalc> GetPeniNoCalcList(PeniNoCalc finder, out Returns ret)
        {
            List<PeniNoCalc> lpnc= new List<PeniNoCalc>();
            try
            {
                lpnc = remoteObject.GetPeniNoCalcList(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return lpnc;
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
                MonitorLog.WriteLog("Ошибка GetPeniNoCalcList\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return lpnc;
            }
        }

        public List<Prov> GetListProvs(ProvFinder finder, out Returns ret)
        {
            var res = new List<Prov>();
            try
            {
                res = remoteObject.GetListProvs(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return res;
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
                MonitorLog.WriteLog("Ошибка GetListProvs\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return res;
            }
        }

        public Dictionary<int, string> GetTypesProvs(out Returns ret)
        {
            var res = new Dictionary<int, string>();
            try
            {
                res = remoteObject.GetTypesProvs(out ret);
                HostChannel.CloseProxy(remoteObject);
                return res;
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
                MonitorLog.WriteLog("Ошибка GetTypesProvs\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return res;
            }
        }
    }
}
