using System;
using System.Collections.Generic;
using System.Data;
using System.ServiceModel;
using Newtonsoft.Json;
using STCLINE.KP50.Client;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.Server
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerCall)]
    public class srv_Charge : srv_Base, I_Charge //
    {//
        //----------------------------------------------------------------------
        public List<Saldo> GetSaldo(Saldo finder, out Returns ret, out _RecordSaldo itog)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<Saldo> sp = new List<Saldo>();
            itog = new _RecordSaldo();
            List<Saldo> list = new List<Saldo>();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Charge cli = new cli_Charge(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.GetSaldo(finder, out ret, out itog);
            }
            else
            {
                using (var db = new DbCharge())
                {
                    list = db.GetSaldo(finder, out ret, ref itog);
                }
            }
            return list;
        }
        //----------------------------------------------------------------------
        public List<Charge> GetCharge(ChargeFind finder, enSrvOper oper, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            List<Charge> list = null;

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Charge cli = new cli_Charge(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.GetCharge(finder, oper, out ret);
            }
            else
            {
                using (var db = new DbCharge())
                {
                    switch (oper)
                    {
                        case enSrvOper.SrvFind:
                            db.FindCharge(finder, out ret);
                            if (ret.result)
                                list = db.GetCharge(finder, out ret);
                            break;
                        case enSrvOper.SrvGet:
                            list = db.GetCharge(finder, out ret);
                            break;
                        case enSrvOper.SrvGetBillCharge:
                            finder.find_from_the_start = 1;
                            db.FindCharge(finder, out ret);
                            if (ret.result)
                                list = db.GetBillCharge(finder, out ret);
                            break;
                        case enSrvOper.SrvGetNewBillCharge:
                            finder.find_from_the_start = 1;
                            list = db.GetNewBillCharge(finder, out ret);
                            break;
                        case enSrvOper.SrvFindChargeStatistics:
                            db.FindChargeStatistics(finder, out ret);
                            if (ret.result)
                                list = db.GetChargeStatistics(finder, out ret);
                            break;
                        case enSrvOper.SrvGetChargeStatistics:
                            list = db.GetChargeStatistics(finder, out ret);
                            break;
                        case enSrvOper.SrvFindChargeStatisticsSupp:
                            db.FindChargeStatisticsSupp(finder, out ret);
                            if (ret.result)
                                list = db.GetChargeStatisticsSupp(finder, out ret);
                            break;
                        case enSrvOper.SrvGetChargeStatisticsSupp:
                            list = db.GetChargeStatisticsSupp(finder, out ret);
                            break;
                    }
                }
            }
            return list;
        }

        //----------------------------------------------------------------------
        public List<MoneyDistrib> GetMoneyDistrib(MoneyDistrib finder, enSrvOper oper, out Returns ret)
        //----------------------------------------------------------------------
        {
            List<MoneyDistrib> list = null;

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Charge cli = new cli_Charge(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.GetMoneyDistrib(finder, oper, out ret);
            }
            else
            {
                try
                {
                    using (var db = new DbCharge())
                    {

                        switch (oper)
                        {
                            case enSrvOper.SrvFind:
                                db.FindMoneyDistrib(finder, out ret);
                                if (ret.result) list = db.GetMoneyDistrib(finder, out ret);
                                break;
                            case enSrvOper.SrvGet:
                                list = db.GetMoneyDistrib(finder, out ret);
                                break;
                            default:
                                ret = new Returns(false, "Неверное наименование операции");
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog(
                        "Ошибка GetMoneyDistrib(" + oper.ToString() + ") " +
                        (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                    ret = new Returns(false, ex.Message);
                }

            }
            return list;
        }

        public List<MoneyDistrib> GetMoneyDistribDom(MoneyDistrib finder, enSrvOper oper, out Returns ret)
        //----------------------------------------------------------------------
        {
            List<MoneyDistrib> list = null;

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Charge cli = new cli_Charge(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.GetMoneyDistribDom(finder, oper, out ret);
            }
            else
            {
                try
                {
                    using (var db = new DbCharge())
                    {
                        switch (oper)
                        {
                            case enSrvOper.SrvFind:
                                db.FindMoneyDistribDom(finder, out ret);
                                if (ret.result) list = db.GetMoneyDistribDom(finder, out ret);
                                break;
                            case enSrvOper.SrvGet:
                                list = db.GetMoneyDistribDom(finder, out ret);
                                break;
                            default:
                                ret = new Returns(false, "Неверное наименование операции");
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog(
                        "Ошибка GetMoneyDistribDom(" + oper.ToString() + ") " +
                        (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                    ret = new Returns(false, ex.Message);
                }

            }
            return list;
        }

        //----------------------------------------------------------------------
        public List<MoneySended> GetMoneySended(MoneySended finder, enSrvOper oper, out Returns ret)
        //----------------------------------------------------------------------
        {
            List<MoneySended> list = null;

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Charge cli = new cli_Charge(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.GetMoneySended(finder, oper, out ret);
            }
            else
            {
                try
                {
                    using (var db = new DbCharge())
                    {
                        switch (oper)
                        {
                            case enSrvOper.SrvFind:
                                list = db.FindMoneySended(finder, out ret);
                                break;
                            default:
                                ret = new Returns(false, "Неверное наименование операции");
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog(
                        "Ошибка GetMoneySended(" + oper.ToString() + ") " +
                        (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                    ret = new Returns(false, ex.Message);
                }

            }
            return list;
        }

        //----------------------------------------------------------------------
        public List<MoneyNaud> GetMoneyNaud(MoneyNaud finder, enSrvOper oper, out Returns ret)
        //----------------------------------------------------------------------
        {
            List<MoneyNaud> list = null;

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Charge cli = new cli_Charge(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.GetMoneyNaud(finder, oper, out ret);
            }
            else
            {

                try
                {
                    using (var db = new DbCharge())
                    {
                        switch (oper)
                        {
                            case enSrvOper.SrvFind:
                                list = db.FindMoneyNaud(finder, out ret);
                                break;
                            default:
                                ret = new Returns(false, "Неверное наименование операции");
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog(
                        "Ошибка GetMoneyNaud(" + oper.ToString() + ") " +
                        (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                    ret = new Returns(false, ex.Message);
                }

            }
            return list;
        }




        //----------------------------------------------------------------------
        public void SaldoFon()
        //----------------------------------------------------------------------
        {
            try
            {
                Returns ret;
                using (var db = new DbCharge())
                {
                    db.SaldoFon(out ret);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка SaldoFonOther " + ex.Message, MonitorLog.typelog.Error, 30, 301, true);
            }
        }
        public Returns CalcChargeListDom(Dom finder, bool reval)
        {
            Returns ret = Utils.InitReturns();

            try
            {
                bool allow = false;
                using (var dba = new DbAdminClient())
                {
                    allow = finder.pref == "AllBases" ? true : dba.IsAllowCalcByPref(finder.pref, out ret);
                    if (!ret.result) return ret;
                }

                if (allow)
                {
                    using (var db = new DbCalc())
                    {
                        var listPoints = new List<_Point>();
                        using (var db2 = new DbCharge())
                        {
                            //организуем цикл по выбранным банкам, получив их по списку домов
                            listPoints = db2.getListCalculatePoints(finder, out ret);
                            foreach (var point in listPoints)
                            {
                                db.CalcOnFon(
                                    finder.nzp_dom,
                                    point.pref,
                                    finder.nzp_user,
                                    CalcFonTask.Types.taskWithRevalOntoListHouses,
                                    out ret);
                                if (!ret.result)
                                    return ret;

                                ret = db2.InsertListHousesForCalc(
                                        new Dom() { nzp_user = finder.nzp_user, nzp_wp = point.nzp_wp }, ret.tag);
                                if (!ret.result)
                                    return ret;
                            }
                        }
                    }
                }
                else
                {
                    ret.result = false;
                    ret.text = "Расчет начислений не доступен";
                    ret.tag = -1;
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка CalcChargeListDom " + ex.Message, MonitorLog.typelog.Error, 30, 301, true);
            }

            return ret;
        }

        public Returns CalcChargeDom(Dom finder, bool reval)
        {
            Returns ret = Utils.InitReturns();

            try
            {
                bool allow = false;
                using (var dba = new DbAdminClient())
                {
                    allow = finder.pref == "AllBases" ? true : dba.IsAllowCalcByPref(finder.pref, out ret);
                    if (!ret.result) return ret;
                }

                if (allow)
                {
                    using (var db = new DbCalc())
                    {
                        db.CalcOnFon(
                            finder.nzp_dom,
                            finder.pref,
                            //Points.CalcMonth.year_, Points.CalcMonth.month_,
                            //Points.CalcMonth.year_, Points.CalcMonth.month_,
                            reval,
                            out ret);
                    }
                    /*
                    DbCalc.CalcDom (
                        finder.nzp_dom, 
                        finder.pref,
                        Points.CalcMonth.year_, Points.CalcMonth.month_,
                        Points.CalcMonth.year_, Points.CalcMonth.month_,
                        out ret);
                    */

                    //#region Добавление в sys_events события 'Расчёт начислений по дому'
                    //if (ret.result)
                    //{
                    //    try
                    //    {
                    //        using (var admin = new DbAdmin())
                    //        {
                    //            admin.InsertSysEvent(new SysEvents()
                    //            {
                    //                pref = Points.Pref,
                    //                nzp_user = finder.nzp_user,
                    //                nzp_dict = 6594,
                    //                nzp_obj = finder.nzp_dom,
                    //                note = "Добавлена задача на расчет начислений" + (finder.nzp_dom > 0 ? " по дому с кодом " + finder.nzp_dom : " по всем банкам")
                    //            });
                    //        }
                    //    }
                    //    catch (Exception ex)
                    //    {
                    //        MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                    //    }
                    //}
                    //#endregion
                }
                else
                {
                    ret.result = false;
                    ret.text = "Расчет начислений не доступен";
                    ret.tag = -1;
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка CalcChargeDom " + ex.Message, MonitorLog.typelog.Error, 30, 301, true);
            }

            return ret;
        }

        public Returns CalcChargeLs(Ls finder, bool reval, bool again, string alias, int id_bill)
        {
            Returns ret = Utils.InitReturns();

            try
            {
                bool allow;
                using (var dba = new DbAdminClient())
                {
                    allow = dba.IsAllowCalcByPref(finder.pref, out ret);

                    if (!ret.result) return ret;
                }

                if (allow)
                {
                    RecordMonth r_m = Points.GetCalcMonth(new CalcMonthParams(finder.pref));
                    //Для расчета произвольного месяца, нужно для тестового расчета
                    if (!String.IsNullOrEmpty(finder.dat_calc))
                    {
                        DateTime d;
                        if (DateTime.TryParse(finder.dat_calc, out d))
                        {
                            r_m.month_ = d.Month;
                            r_m.year_ = d.Year;
                        }
                    }


                    using (var db = new DbCalc())
                    {
                        db.CalcLs(finder.nzp_kvar,
                            finder.pref,
                            r_m.year_, r_m.month_,
                            r_m.year_, r_m.month_,
                            reval,
                            again,
                            alias, id_bill,
                            out ret);
                    }
                    //using (var db = new DbGilec())
                    //{
                    //    //Kart kart = new Kart();
                    //    //kart.nzp_user = finder.nzp_user;
                    //    //kart.pref = finder.pref;
                    //    //kart.nzp_kvar = finder.nzp_kvar;
                    //    //db.ReSaveGilPrm(kart);
                    //}
                    //#region Добавление в sys_events события 'Расчёт начислений ЛС'
                    //try
                    //{
                    //    var admin = new DbAdmin();
                    //    admin.InsertSysEvent(new SysEvents()
                    //    {
                    //        pref = Points.Pref,
                    //        nzp_user = finder.nzp_user,
                    //        nzp_dict = 6595,
                    //        nzp_obj = finder.nzp_kvar,
                    //        note = "Расчет был произведен"
                    //    });
                    //}
                    //catch (Exception ex)
                    //{
                    //    MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                    //}
                    //#endregion
                }
                else
                {
                    ret.result = false;
                    ret.text = "Расчет начислений не доступен";
                    ret.tag = -1;
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка CalcChargeLs " + ex.Message, MonitorLog.typelog.Error, 30, 301, true);
            }

            return ret;
        }

        //----------------------------------------------------------------------
        public _RecordSzFin GetCalcSz(ChargeFind finder, enSrvOper oper, out Returns ret)
        //----------------------------------------------------------------------
        {
            return CallSrvCalcSz(finder, oper, out ret);
        }
        //----------------------------------------------------------------------
        public List<SaldoRep> FillRep(ChargeFind finder, out Returns ret, int num_rep)
        //----------------------------------------------------------------------
        {
            List<SaldoRep> list;
            ret = Utils.InitReturns();
            using (var db = new DbCharge())
            {
                list = db.FillRep(finder, out ret, num_rep);
            }
            return list;
        }

        //----------------------------------------------------------------------
        public List<_RecordDomODN> FillRepProtokolOdn(ChargeFind finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<_RecordDomODN> list;
            using (var db = new DbCharge())
            {
                list = db.FillRep_Protokol_odn(finder, out ret);
            }
            return list;
        }
        //----------------------------------------------------------------------
        public Returns PrepareReport(ChargeFind finder, int num_rep)
        //----------------------------------------------------------------------
        {
            Returns ret;
            using (var db = new DbCharge())
            {
                switch (num_rep)
                {
                    case 2:
                        ret = db.PrepareReport5_20(finder);
                        break;
                    default:
                        ret = new Returns(false, "Неверный номер отчета");
                        break;
                }
            }
            return ret;
        }
        //----------------------------------------------------------------------
        public List<SaldoRep> FillRepServ(ChargeFind finder, out Returns ret, int num_rep)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<SaldoRep> list;
            using (var db = new DbCharge())
            {
                list = db.FillRepServ(finder, out ret, num_rep);
            }
            return list;
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

            using (var db = new DbCharge())
            {
                switch (oper)
                {
                    case enSrvOper.SrvFindCalcSz:
                        db.FindCalcSz(finder, out ret);
                        if (ret.result) zap = db.GetCalcSz(finder, out ret);
                        break;
                    case enSrvOper.SrvGetCalcSz:
                        zap = db.GetCalcSz(finder, out ret);
                        break;
                }
            }
            return zap;
        }

        //----------------------------------------------------------------------
        public List<Perekidka> LoadPerekidki(Perekidka finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            List<Perekidka> list = null;
            try
            {
                using (var db = new DbCharge())
                {
                    list = db.LoadPerekidki(finder, out ret);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка LoadPerekidki " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                ret = new Returns(false, ex.Message);
            }
            return list;
        }


        //----------------------------------------------------------------------
        public List<DelSupplier> LoadPerekidkiOplatami(DelSupplier finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            List<DelSupplier> list = null;
            try
            {
                using (var db1 = new DbPerekidkaServer())
                {
                    list = db1.LoadPerekidkiOplatami(finder, out ret);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка LoadPerekidkiOplatami " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                ret = new Returns(false, ex.Message);
            }
            return list;
        }

        public Returns SavePerekidkiOplatami(DelSupplier finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var db1 = new DbPerekidkaServer())
                {
                    ret = db1.SavePerekidkiOplatami(finder);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка SavePerekidkiOplatami " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                ret = new Returns(false, ex.Message);
            }

            return ret;
        }

        public Returns SavePerekidka(Perekidka finder)
        {
            Returns ret;
            try
            {
                using (var db = new DbCharge())
                {
                    ret = db.SavePerekidka(finder);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка SavePerekidka " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                ret = new Returns(false, ex.Message);
            }
            return ret;
        }

        public Returns DeletePerekidkaOplatami(DelSupplier finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                using (var db1 = new DbPerekidkaServer())
                {
                    ret = db1.DeletePerekidkaOplatami(finder);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка DeletePerekidkaOplatami " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                ret = new Returns(false, ex.Message);
            }

            return ret;
        }

        public Returns DeletePerekidka(Perekidka finder)
        //----------------------------------------------------------------------
        {
            Returns ret;
            try
            {
                using (var db = new DbCharge())
                {
                    ret = db.DeletePerekidka(finder);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка DeletePerekidka " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                ret = new Returns(false, ex.Message);
            }
            return ret;
        }
        public List<Perekidka> LoadSumsPerekidkaLs(Perekidka finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            List<Perekidka> list = null;
            try
            {
                using (var db = new DbCharge())
                {
                    list = db.LoadSumsPerekidkaLs(finder, out ret);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка LoadSumsPerekidkaLs " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                ret = new Returns(false, ex.Message);
            }
            return list;
        }
        public Returns SaveSumsPerekidkaLs(List<Perekidka> listfinder)
        //----------------------------------------------------------------------
        {
            Returns ret;
            try
            {
                using (var db = new DbCharge())
                {
                    ret = db.SaveSumsPerekidkaLs(listfinder);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка SaveSumsPerekidkaLs " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                ret = new Returns(false, ex.Message);
            }
            return ret;
        }

        #region Отчеты
        //--------------------------справка_лиц_сч---------------------------------------------
        public List<Charge> GetLicChetData(ref Kart finder, out Returns ret, int y, int m)
        //-------------------------------------------------------------------------------------
        {
            List<Charge> list;
            using (var db = new DbCharge())
            {
                list = db.GetLicChetData(ref finder, out ret, y, m);
            }
            return list;
        }
        ////-------------------------------------------------------------------------------------------------
        //public string GetLicChetData1(Object rep, ref Kart finder, out Returns ret, int y, int m, DateTime date)
        ////-------------------------------------------------------------------------------------------------
        //{
        //    //для инициализации интерфейса
        //    ret = Utils.InitReturns();
        //    return null;
        //}
        ////--------------------------------------------------------------------------------------------
        //public string Fill_web_fin_ls(Object rep, out Returns ret, int y_, int m_, ChargeFind finder)
        ////--------------------------------------------------------------------------------------------
        //{
        //    //для инициализации интерфейса
        //    ret = Utils.InitReturns();
        //    return null;
        //}
        ////--------------------------------------------------------------------------------------------
        //public string Fill_web_s_nodolg(Object rep, out Returns ret, int y_, int m_, ChargeFind finder)
        ////---------------------------------------------------------------------------------------------
        //{
        //    //для инициализации интерфейса
        //    ret = Utils.InitReturns();
        //    return null;
        //}
        ////-----------------------------------------------------------------------------------------------
        //public string Fill_web_sparv_nach(Object rep, out Returns ret, int y_, int m_, ChargeFind finder)
        ////-----------------------------------------------------------------------------------------------
        //{
        //    //для инициализации интерфейса
        //    ret = Utils.InitReturns();
        //    return null;
        //}
        ////------------------------------------------------------------------------------------------------
        //public string Fill_web_saldo_rep5_10(Object rep, out Returns ret, int y_, int m_, ChargeFind finder)
        ////---------------------------------------------------------------------------------------------------
        //{
        //    //для инициализации интерфейса
        //    ret = Utils.InitReturns();
        //    return null;
        //}

        ////--------------------------------------------------------------------------------------------------
        //public string Fill_web_saldo_rep5_20(Object rep, out Returns ret, int y_, int m_, ChargeFind finder)
        ////--------------------------------------------------------------------------------------------------
        //{
        //    //для инициализации интерфейса
        //    ret = Utils.InitReturns();
        //    return null;
        //}
        #endregion

        //----------------------------------------------------------------------
        public bool IsTableFilledIn(ChargeFind finder, TableName table, out Returns ret)
        //----------------------------------------------------------------------
        {
            bool b = false;

            try
            {
                using (var db = new DbCharge())
                {
                    b = db.IsTableFilledIn(finder, table, out ret);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка IsTableFilledIn(" + table.ToString() + ") " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                ret = new Returns(false, ex.Message);
            }
            return b;
        }

        //для FBD
        ////----------------------------------------------------------------------
        //public List<SaldoRep> FillRep_5_10(ChargeFind finder, out Returns ret, int num_rep)
        ////----------------------------------------------------------------------
        //{
        //    List<SaldoRep> retList = null;
        //    DbCharge db = new DbCharge();
        //    try
        //    {
        //        retList = db.FillRep_5_10(finder, out ret, num_rep);
        //    }
        //    catch (Exception ex)
        //    {
        //        MonitorLog.WriteLog("Ошибка FillRep_5_10" + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
        //        ret = new Returns(false, ex.Message);
        //    }
        //    db.Close();
        //    return retList;
        //}

        /*public decimal GetSumKOplate(Saldo finder, out Returns ret)
        {
            ret = Utils.InitReturns();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Charge cli = new cli_Charge(WCFParams.AdresWcfHost.CurT_Server);
                return cli.GetSumKOplate(finder, out ret);
            }
            else
            {
                try
                {
                    DbCharge dbcharge = new DbCharge();
                    decimal sumKOplate = dbcharge.GetSumKOplate(finder, out ret);
                    dbcharge.Close();
                    return sumKOplate;
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова операции с пачкой оплат";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetSumKOplate()\n " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                return 0;
            }
        }*/

        public ReturnsObjectType<DataTable> LoadSended(MoneySended finder)
        {
            ReturnsObjectType<DataTable> rez;
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Charge cli = new cli_Charge(WCFParams.AdresWcfHost.CurT_Server);
                rez = cli.LoadSended(finder);
            }
            else
            {
                try
                {
                    using (var db = new DbCharge())
                    {
                        rez = new ClassDB().RunSqlAction(finder, db.LoadSended);
                    }
                }
                catch (Exception ex)
                {
                    Returns ret = Utility.ExceptionUtility.OnException(ex, System.Reflection.MethodBase.GetCurrentMethod().Name);
                    rez = new ReturnsObjectType<DataTable>(ret.result, ret.text);
                }
            }
            return rez;
        }

        public Returns SaveMoneySended(List<MoneySended> list)
        {
            Returns ret;
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Charge cli = new cli_Charge(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.SaveMoneySended(list);
            }
            else
            {
                try
                {
                    using (var db = new DbCharge())
                    {
                        ret = db.SaveMoneySended(list);
                    }
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка SaveMoneySended " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                    ret = new Returns(false, ex.Message);
                }
            }
            return ret;
        }

        public List<FnPercent> GetFnPercent(FnPercent finder, enSrvOper oper, out Returns ret)
        {
            List<FnPercent> list = null;

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Charge cli = new cli_Charge(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.GetFnPercent(finder, oper, out ret);
            }
            else
            {
                try
                {
                    using (var db = new DbCharge())
                    {
                        switch (oper)
                        {
                            case enSrvOper.SrvFind:
                                list = db.FindFnPercent(finder, out ret);
                                break;
                            case enSrvOper.SrvFindPercentDom:
                                list = db.FindFnPercentDom(finder, out ret);
                                break;
                            case enSrvOper.SrvGetPercentDomLog:
                                list = db.GetFnPercentDomLog(finder, out ret);
                                break;
                            default:
                                ret = new Returns(false, "Неверное наименование операции");
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка GetFnPercent(" + oper.ToString() + ") " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                    ret = new Returns(false, ex.Message);
                }
            }
            return list;
        }

        public Returns SaveFnPercent(FnPercent finder)
        {
            try
            {
                Returns ret;
                using (var db = new DbCharge())
                {
                    ret = new ClassDB().RunSqlAction(finder, db.SaveFnPercent).GetReturns();
                }
                return ret;
            }

            catch (Exception ex)
            {
                return Utility.ExceptionUtility.OnException(ex, System.Reflection.MethodBase.GetCurrentMethod().Name);
            }

            //Returns ret;
            //DbCharge db = new DbCharge();
            //try
            //{
            //    ret = db.SaveFnPercent(finder);
            //}
            //catch (Exception ex)
            //{
            //    MonitorLog.WriteLog("Ошибка SaveFnPercent " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
            //    ret = new Returns(false, ex.Message);
            //}
            //db.Close();
            //return ret;
        }

        public Returns SaveFnPercentDom(FnPercent finder)
        {
            try
            {
                Returns ret;
                using (var db = new DbCharge())
                {
                    ret = new ClassDB().RunSqlAction(finder, db.SaveFnPercentDom).GetReturns();
                }
                return ret;
            }

            catch (Exception ex)
            {
                return Utility.ExceptionUtility.OnException(ex, System.Reflection.MethodBase.GetCurrentMethod().Name);
            }

            //Returns ret;
            //DbCharge db = new DbCharge();
            //try
            //{
            //    ret = db.SaveFnPercent(finder);
            //}
            //catch (Exception ex)
            //{
            //    MonitorLog.WriteLog("Ошибка SaveFnPercent " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
            //    ret = new Returns(false, ex.Message);
            //}
            //db.Close();
            //return ret;
        }

        //public Returns DelFnPercent(FnPercent finder)
        //{
        //    Returns ret;
        //    DbCharge db = new DbCharge();
        //    try
        //    {
        //        ret = db.DelFnPercent(finder);
        //    }
        //    catch (Exception ex)
        //    {
        //        MonitorLog.WriteLog("Ошибка DelFnPercent " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
        //        ret = new Returns(false, ex.Message);
        //    }
        //    db.Close();
        //    return ret;
        //}


        public Returns DelFnPercent(FnPercent finder)
        {
            try
            {
                Returns ret;
                using (var db = new DbCharge())
                {
                    ret = new ClassDB().RunSqlAction(finder, db.DelFnPercent).GetReturns();
                }
                return ret;
            }

            catch (Exception ex)
            {
                return Utility.ExceptionUtility.OnException(ex, System.Reflection.MethodBase.GetCurrentMethod().Name);
            }
        }

        public Returns DelFnPercentDom(FnPercent finder)
        {
            try
            {
                Returns ret;
                using (var db = new DbCharge())
                {
                    ret = new ClassDB().RunSqlAction(finder, db.DelFnPercentDom).GetReturns();
                }
                return ret;
            }

            catch (Exception ex)
            {
                return Utility.ExceptionUtility.OnException(ex, System.Reflection.MethodBase.GetCurrentMethod().Name);
            }
        }

        public ReturnsType UpdateKredit(string pref, int inCalcYear, int inCalcMonth)
        {
            ReturnsType ret = new ReturnsType();

            try
            {
                using (var db = new DbKreditPay())
                {
                    ret = db.UpdateKredit(pref, inCalcYear, inCalcMonth);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка UpdateKredit() " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                ret = new ReturnsType(false, ex.Message);
            }

            return ret;
        }

        public List<Credit> GetCredit(Credit finder, out Returns ret)
        {
            List<Credit> list = null;

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Charge cli = new cli_Charge(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.GetCredit(finder, out ret);
            }
            else
            {
                try
                {
                    using (var db = new DbKredit())
                    {
                        list = db.GetCredit(finder, out ret);
                    }
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка GetCredit() " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                    ret = new Returns(false, ex.Message);
                }
            }
            return list;
        }

        public void SaveCredit(Credit finder, out Returns ret)
        {
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Charge cli = new cli_Charge(WCFParams.AdresWcfHost.CurT_Server);
                cli.SaveCredit(finder, out ret);
            }
            else
            {
                try
                {
                    using (var db = new DbKredit())
                    {
                        db.SaveCredit(finder, out ret);
                    }
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка SaveCredit() " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                    ret = new Returns(false, ex.Message);
                }
            }
        }

        public List<CreditDetails> GetCreditDetails(CreditDetails finder, out Returns ret)
        {
            List<CreditDetails> list = null;

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Charge cli = new cli_Charge(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.GetCreditDetails(finder, out ret);
            }
            else
            {
                try
                {
                    using (var db = new DbKredit())
                    {
                        list = db.GetCreditDetails(finder, out ret);
                    }
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка GetCredit() " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                    ret = new Returns(false, ex.Message);
                }
            }
            return list;
        }

        public Returns IsAllowCorrectSaldo(Ls finder)
        {
            Returns ret = Utils.InitReturns();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Charge cli = new cli_Charge(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.IsAllowCorrectSaldo(finder);
            }
            else
            {
                try
                {
                    using (var db = new DbCharge())
                    {
                        ret =
                            new ClassDB().RunSqlAction(new FinderObjectType<Ls>(finder),
                                db.IsAllowCorrectSaldo).GetReturns();
                    }
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции проверки возможности корректировать сальдо";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка IsAllowCorrectSaldo()\n " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return ret;
        }

        public Returns SaveCorrectSaldo(List<Saldo> finder)
        {
            Returns ret = Utils.InitReturns();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Charge cli = new cli_Charge(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.SaveCorrectSaldo(finder);
            }
            else
            {
                try
                {
                    using (var db = new DbCharge())
                    {
                        ret =
                            new ClassDB().RunSqlAction(new FinderObjectType<List<Saldo>>(finder),
                                db.SaveCorrectSaldo).GetReturns();
                    }
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции сохранения скорректированного сальдо";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка SaveCorrectSaldo()\n " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return ret;
        }

        public Returns MakeProtCalc(Calcs finder)
        {
            Returns ret = Utils.InitReturns();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Charge cli = new cli_Charge(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.MakeProtCalc(finder);
            }
            else
            {
                try
                {
                    using (var db = new DbCharge())
                    {
                        ret =
                            new ClassDB().RunSqlAction(new FinderObjectType<Calcs>(finder), db.MakeProtCalc)
                                .GetReturns();
                    }
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции для протоколарасчета";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка MakeProtCalc()\n " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return ret;
        }

        //----------------------------------------------------------------------
        public bool IsNowCalcCharge(long nzp_dom, string pref, out Returns ret)
        {
            ret = Utils.InitReturns();
            bool b = false;
            try
            {
                using (var db = new DbCalc())
                {
                    b =
                        db.IsNowCalcCharge(nzp_dom, pref, out ret);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка IsNowCalcCharge " + ex.Message, MonitorLog.typelog.Error, 30, 301, true);
            }
            return b;
        }

        public ReturnsType MakeOperation(ChargeOperations operation, Finder finder)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Charge cli = new cli_Charge(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.MakeOperation(operation, finder);
            }
            else
            {
                try
                {
                    using (var db = new DbCharge())
                    {
                        switch (operation)
                        {
                            case ChargeOperations.CheckingBeforeCloseMonth:
                                ret = db.MakeChecksBeforeCloseCalcMonth(finder, finder.dopFind);
                                break;

                            case ChargeOperations.CloseCalcMonth:
                                ret = db.MakeChecksBeforeCloseCalcMonth(finder);
                                if (ret.result)
                                    ret = db.CloseCalcMonth(finder);
                                //если перевод месяца успешен - вызываем запись проводок
                                if (ret.result)
                                {
                                    //пишем проводки по всем банкам!!
                                    foreach (var point in Points.PointList)
                                    {
                                        var provFinder = new Finder();
                                        finder.CopyTo(provFinder);
                                        provFinder.nzp_wp = point.nzp_wp;
                                        provFinder.pref = point.pref;

                                        var ret1 = Utils.InitReturns();
                                        provFinder.listNumber = (int)operation;
                                        AddFonTaskProv(provFinder, CalcFonTask.Types.taskInsertProvOnClosedCalcMonth, out ret1);
                                        if (!ret1.result)
                                        {
                                            ret.text = "Функция закрытия месяца выполнена успешно, но при записи проводок произошла ошибка.";
                                        }
                                    }
                                }

                                break;

                            case ChargeOperations.OpenCalcMonth:
                                ret = db.OpenCalcMonth();
                                //вызываем сброс в архив проводок по месяцу который откатили
                                if (ret.result)
                                {
                                    foreach (var point in Points.PointList)
                                    {
                                        var provFinder = new Finder();
                                        finder.CopyTo(provFinder);
                                        provFinder.nzp_wp = point.nzp_wp;
                                        provFinder.pref = point.pref;
                                        var ret1 = Utils.InitReturns();
                                        provFinder.listNumber = (int)operation;

                                        AddFonTaskProv(provFinder, CalcFonTask.Types.taskInsertProvOnClosedCalcMonth,
                                            out ret1);
                                        if (!ret1.result)
                                        {
                                            ret.text =
                                                "Функция выполнения отката месяца выполнена успешно, но при архивации проводок за этот месяц произошла ошибка.";
                                        }
                                    }
                                }

                                break;

                            default:
                                ret = new ReturnsType(false, "Неверное наименование операции");
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = ex.Message;
                    MonitorLog.WriteLog("Ошибка MakeOperation(" + operation + ")\n" + ex.Message, MonitorLog.typelog.Error, 30, 301, true);
                }
            }
            return ret;
        }

        /// <summary>
        /// Запись проводок в фоновой задаче
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        public void AddFonTaskProv(Finder finder, CalcFonTask.Types type, out Returns ret)
        {
            CalcFonTask calcfon = new CalcFonTask(Points.GetCalcNum(0));
            calcfon.TaskType = type;
            calcfon.Status = FonTask.Statuses.New; //на выполнение                         
            calcfon.nzp_user = finder.nzp_user;
            calcfon.nzp = 0; //потому что по банку
            calcfon.nzpt = finder.nzp_wp;
            calcfon.pref = finder.pref;
            calcfon.txt = calcfon.processName;
            calcfon.parameters = JsonConvert.SerializeObject(finder);
            var db = new DbCalcQueueClient();
            ret = db.AddTask(calcfon);
            db.Close();
        }

        public List<TypeRcl> LoadTypeRcl(TypeRcl finder, out Returns ret)
        {
            ret = new Returns();
            List<TypeRcl> list = null;
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Charge cli = new cli_Charge(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.LoadTypeRcl(finder, out ret);
            }
            else
            {
                try
                {
                    using (var db = new DbCharge())
                    {
                        list = db.LoadTypeRcl(finder, out ret);
                    }
                }
                catch (Exception ex)
                {
                    list = null;
                    ret.result = false;
                    ret.text = ex.Message;
                    MonitorLog.WriteLog("Ошибка LoadTypeRcl\n" + ex.Message, MonitorLog.typelog.Error, 30, 301, true);
                }
            }
            return list;
        }

        public List<TypeDoc> LoadTypeDoc(TypeDoc finder, out Returns ret)
        {
            ret = new Returns();
            List<TypeDoc> list = null;
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Charge cli = new cli_Charge(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.LoadTypeDoc(finder, out ret);
            }
            else
            {
                try
                {
                    using (var db = new DbCharge())
                    {
                        list = db.LoadTypeDoc(finder, out ret);
                    }
                }
                catch (Exception ex)
                {
                    list = null;
                    ret.result = false;
                    ret.text = ex.Message;
                    MonitorLog.WriteLog("Ошибка LoadTypeDoc\n" + ex.Message, MonitorLog.typelog.Error, 30, 301, true);
                }
            }
            return list;
        }

        public List<CheckChMon> LoadCheckChMon(CheckChMon finder, out Returns ret)
        {
            ret = new Returns();
            List<CheckChMon> list = null;
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Charge cli = new cli_Charge(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.LoadCheckChMon(finder, out ret);
            }
            else
            {
                try
                {
                    using (var db = new DbCharge())
                    {
                        list = db.LoadCheckChMon(finder, out ret);
                    }
                }
                catch (Exception ex)
                {
                    list = null;
                    ret.result = false;
                    ret.text = ex.Message;
                    MonitorLog.WriteLog("Ошибка LoadCheckChMon\n" + ex.Message, MonitorLog.typelog.Error, 30, 301, true);
                }
            }
            return list;
        }

        public List<GroupsPerekidki> PrepareSplsPerekidki(ParamsForGroupPerekidki finder, out Returns ret)
        {
            ret = new Returns();
            List<GroupsPerekidki> list = null;
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Charge cli = new cli_Charge(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.PrepareSplsPerekidki(finder, out ret);
            }
            else
            {
                try
                {
                    using (var db1 = new DbPerekidkaServer())
                    {
                        list = db1.PrepareSplsPerekidki(finder, out ret);
                    }
                }
                catch (Exception ex)
                {
                    list = null;
                    ret.result = false;
                    ret.text = ex.Message;
                    MonitorLog.WriteLog("Ошибка PrepareSplsPerekidki\n" + ex.Message, MonitorLog.typelog.Error, 30, 301, true);
                }
            }
            return list;
        }


        public Returns BackComments()
        {
            Returns ret = new Returns();
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Charge cli = new cli_Charge(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.BackComments();
            }
            else
            {
                try
                {
                    using (var db1 = new DbPerekidkaServer())
                    {
                        ret = db1.BackComments();
                    }
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = ex.Message;
                    MonitorLog.WriteLog("Ошибка BackComments\n" + ex.Message, MonitorLog.typelog.Error, 30, 301, true);
                }
            }
            return ret;
        }

        public Returns PerenosReestrPerekidok()
        {
            Returns ret = new Returns();
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Charge cli = new cli_Charge(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.PerenosReestrPerekidok();
            }
            else
            {
                try
                {
                    using (var db1 = new DbPerekidkaServer())
                    {
                        ret = db1.PerenosReestrPerekidok();
                    }
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = ex.Message;
                    MonitorLog.WriteLog("Ошибка PerenosReestrPerekidok\n" + ex.Message, MonitorLog.typelog.Error, 30, 301, true);
                }
            }
            return ret;
        }

        public Returns SaveGroupPerekidki(ParamsForGroupPerekidki finder)
        {
            Returns ret = new Returns();
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Charge cli = new cli_Charge(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.SaveGroupPerekidki(finder);
            }
            else
            {
                try
                {
                    using (var db = new DbCharge())
                    {
                        ret = db.SaveGroupPerekidki(finder);
                    }
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = ex.Message;
                    MonitorLog.WriteLog("Ошибка SaveGroupPerekidki\n" + ex.Message, MonitorLog.typelog.Error, 30, 301, true);
                }
            }
            return ret;
        }

        public Returns DeleteFromReestrPerekidok(ParamsForGroupPerekidki finder)
        {
            Returns ret = new Returns();
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Charge cli = new cli_Charge(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.DeleteFromReestrPerekidok(finder);
            }
            else
            {
                try
                {
                    using (var db = new DbCharge())
                    {
                        ret = db.DeleteFromReestrPerekidok(finder);
                    }
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = ex.Message;
                    MonitorLog.WriteLog("Ошибка DeleteFromReestrPerekidok\n" + ex.Message, MonitorLog.typelog.Error, 30, 301, true);
                }
            }
            return ret;
        }

        public List<ParamsForGroupPerekidki> LoadReestrPerekidok(ParamsForGroupPerekidki finder, out Returns ret)
        {
            ret = new Returns();
            List<ParamsForGroupPerekidki> list = null;
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Charge cli = new cli_Charge(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.LoadReestrPerekidok(finder, out ret);
            }
            else
            {
                try
                {
                    using (var db = new DbCharge())
                    {
                        list = db.LoadReestrPerekidok(finder, out ret);
                    }
                }
                catch (Exception ex)
                {
                    list = null;
                    ret.result = false;
                    ret.text = ex.Message;
                    MonitorLog.WriteLog("Ошибка LoadReestrPerekidok\n" + ex.Message, MonitorLog.typelog.Error, 30, 301, true);
                }
            }
            return list;
        }

        public List<PerekidkaLsToLs> LoadSumsForPerekidkaLsToLs(PerekidkaLsToLs finder, out Returns ret)
        {
            ret = new Returns();
            List<PerekidkaLsToLs> list = null;
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Charge cli = new cli_Charge(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.LoadSumsForPerekidkaLsToLs(finder, out ret);
            }
            else
            {
                try
                {
                    using (var db = new DbCharge())
                    {
                        list = db.LoadSumsForPerekidkaLsToLs(finder, out ret);
                    }
                }
                catch (Exception ex)
                {
                    list = null;
                    ret.result = false;
                    ret.text = ex.Message;
                    MonitorLog.WriteLog("Ошибка LoadSumsForPerekidkaLsToLs\n" + ex.Message, MonitorLog.typelog.Error, 30, 301, true);
                }
            }
            return list;
        }

        public Returns SavePerekidkiLsToLs(List<PerekidkaLsToLs> listfinder, ParamsForGroupPerekidki reestr)
        {
            Returns ret = new Returns();
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Charge cli = new cli_Charge(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.SavePerekidkiLsToLs(listfinder, reestr);
            }
            else
            {
                try
                {
                    using (DbCharge db1 = new DbCharge())
                    {
                        ret = db1.SavePerekidkiLsToLs(listfinder, reestr);
                    }
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = ex.Message;
                    MonitorLog.WriteLog("Ошибка SavePerekidkiLsToLs\n" + ex.Message, MonitorLog.typelog.Error, 30, 301, true);
                }
            }
            return ret;
        }

        public Returns FindLsForReestrPerekidok(ParamsForGroupPerekidki finder)
        {
            Returns ret = new Returns();
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Charge cli = new cli_Charge(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.FindLsForReestrPerekidok(finder);
            }
            else
            {
                try
                {
                    using (var db = new DbCharge())
                    {
                        ret = db.FindLsForReestrPerekidok(finder);
                    }
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = ex.Message;
                    MonitorLog.WriteLog("Ошибка FindLsForReestrPerekidok\n" + ex.Message, MonitorLog.typelog.Error, 30, 301, true);
                }
            }
            return ret;
        }

        public Returns GetNachisl(int mode, int month_, int year_, int user_)
        {
            Returns ret = new Returns();
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Charge cli = new cli_Charge(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetNachisl(mode, month_, year_, user_);
            }
            else
            {
                try
                {
                    using (var db = new DbCharge())
                    {
                        //ret = db.GetNachisl(mode, month_, year_, user_);

                        db._Invoke_(mode, month_, year_, user_);
                    }
                    ret = new Returns(true, "Задание отправлено на выполнение. Протокол загрузки доступен через пункт меню \"Мои файлы\"", -1);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = ex.Message;
                    MonitorLog.WriteLog("Ошибка GetNachisl\n" + ex.Message, MonitorLog.typelog.Error, 30, 301, true);//2,100
                }
            }
            return ret;
        }

        public List<OverPayment> GetOverPayments(OverPayment finder, enSrvOper srv, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<OverPayment> Spis = new List<OverPayment>();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Charge cli = new cli_Charge(WCFParams.AdresWcfHost.CurT_Server);
                Spis = cli.GetOverPayments(finder, srv, out ret);
            }
            else
            {
                try
                {
                    using (var db = new DbCharge())
                    {
                        switch (srv)
                        {
                            case enSrvOper.SrvFind:
                                db.FindOverPayments(finder, out ret);
                                if (ret.result)
                                    Spis = db.GetOverPayments(finder, out ret);
                                else
                                    Spis = null;
                                break;
                            case enSrvOper.SrvGet:
                                Spis = db.GetOverPayments(finder, out ret);
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции получения л/c";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetOverPayments() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return Spis;
        }

        public Returns SaveAddressToForOverPay(OverPayment finder)
        {
            Returns ret = new Returns();
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Charge cli = new cli_Charge(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.SaveAddressToForOverPay(finder);
            }
            else
            {
                try
                {
                    using (var db1 = new DbCharge())
                    {
                        ret = db1.SaveAddressToForOverPay(finder);
                    }
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = ex.Message;
                    MonitorLog.WriteLog("Ошибка SaveAddressToForOverPay\n" + ex.Message, MonitorLog.typelog.Error, 30, 301, true);
                }
            }
            return ret;
        }

        public List<PeniNoCalc> GetPeniNoCalcList(PeniNoCalc finder, out Returns ret)
        {
            List<PeniNoCalc> peniNocalc = new List<PeniNoCalc>();
            ret = new Returns();
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Charge cli = new cli_Charge(WCFParams.AdresWcfHost.CurT_Server);
                peniNocalc = cli.GetPeniNoCalcList(finder, out ret);
            }
            else
            {
                try
                {
                    using (var db1 = new DbCharge())
                    {
                        peniNocalc = db1.GetPeniNoCalcList(finder, out ret);
                    }
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = ex.Message;
                    MonitorLog.WriteLog("Ошибка GetPeniNoCalcList\n" + ex.Message, MonitorLog.typelog.Error, 30, 301, true);
                }
            }
            return peniNocalc;
        }

        public List<Prov> GetListProvs(ProvFinder finder, out Returns ret)
        {
            var res = new List<Prov>();
            ret = new Returns();
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Charge cli = new cli_Charge(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.GetListProvs(finder, out ret);
            }
            else
            {
                try
                {
                    using (var db1 = new DbCharge())
                    {
                        res = db1.GetListProvs(finder, out ret);
                    }
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = ex.Message;
                    MonitorLog.WriteLog("Ошибка GetListProvs\n" + ex.Message, MonitorLog.typelog.Error, 30, 301, true);
                }
            }
            return res;
        }
        public Dictionary<int, string> GetTypesProvs(out Returns ret)
        {
            var res = new Dictionary<int, string>();
            ret = new Returns();
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Charge cli = new cli_Charge(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.GetTypesProvs(out ret);
            }
            else
            {
                try
                {
                    using (var db1 = new DbCharge())
                    {
                        res = db1.GetTypesProvs(out ret);
                    }
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = ex.Message;
                    MonitorLog.WriteLog("Ошибка GetTypesProvs\n" + ex.Message, MonitorLog.typelog.Error, 30, 301, true);
                }
            }
            return res;
        }



        public List<PrmTypes> LoadUsersPercentDom(Finder finder, out Returns ret)
        {
            List<PrmTypes> list = new List<PrmTypes>();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Charge cli = new cli_Charge(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.LoadUsersPercentDom(finder, out ret);
            }
            else
            {
                try
                {
                    using (var db = new DbCharge())
                    {
                        list = db.LoadUsersPercentDom(finder, out ret);
                    }
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + " " + 
                        (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                    ret = new Returns(false, ex.Message);
                }
            }
            return list;
        }

        public List<PrmTypes> LoadOperTypesPercentDom(Finder finder, out Returns ret)
        {
            List<PrmTypes> list = new List<PrmTypes>();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Charge cli = new cli_Charge(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.LoadOperTypesPercentDom(finder, out ret);
            }
            else
            {
                try
                {
                    using (var db = new DbCharge())
                    {
                        list = db.LoadOperTypesPercentDom(finder, out ret);
                    }
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + " " +
                        (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                    ret = new Returns(false, ex.Message);
                }
            }
            return list;
        }
    }

}
