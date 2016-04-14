using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using System.Collections;
using System.Data;
using STCLINE.KP50.Interfaces;

using STCLINE.KP50.Client;

namespace STCLINE.KP50.Server
{
    public class srv_Debitor : srv_Base, I_Debitor
    {

        public Deal LoadDealInfo(DealFinder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            Deal res = null;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Debitor cli = new cli_Debitor(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.LoadDealInfo(finder, out ret);
            }
            else
            {
                Debitor db = new Debitor();
                try
                {
                    res = db.LoadDealInfo(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LoadDealInfo() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = null;
                }
                db.Close();
            }
            return res;
        }


        public List<Agreement> GetAgreements(DealFinder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Agreement> res = null;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Debitor cli = new cli_Debitor(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.GetAgreements(finder, out ret);
            }
            else
            {
                Debitor db = new Debitor();
                try
                {
                    res = db.GetAgreements(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetAgreements() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = null;
                }
                db.Close();
            }
            return res;
        }

        public List<Deal> GetDealStatuses(DealFinder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Deal> res = null;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Debitor cli = new cli_Debitor(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.GetDealStatuses(finder, out ret);
            }
            else
            {
                Debitor db = new Debitor();
                try
                {
                    res = db.GetDealStatuses(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetDealStatuses() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = null;
                }
                db.Close();
            }
            return res;
        }

        public Returns SaveDebtChanges(Deal finder)
        {
            Returns ret = Utils.InitReturns();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Debitor cli = new cli_Debitor(WCFParams.AdresWcfHost.CurT_Server);
                //ret = cli.SaveDebtChanges(finder);
            }
            else
            {
                Debitor db = new Debitor();
                try
                {
                    //  ret = db.SaveDebtChanges(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetDebtStatuses() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    return ret;
                }
                db.Close();
            }
            return ret;
        }

        public Returns SaveDealChanges(Deal finder)
        {
            Returns ret = Utils.InitReturns();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Debitor cli = new cli_Debitor(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.SaveDealChanges(finder);
            }
            else
            {
                Debitor db = new Debitor();
                try
                {
                    ret = db.SaveDealChanges(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetDealStatuses() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    return ret;
                }
                db.Close();
            }
            return ret;
        }

        public List<Deal> GetArgStatus(Deal finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Deal> res = null;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Debitor cli = new cli_Debitor(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.GetArgStatus(finder, out ret);
            }
            else
            {
                Debitor db = new Debitor();
                try
                {
                    res = db.GetArgStatus(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetArgStatus() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = null;
                }
                db.Close();
            }
            return res;
        }

        public List<AgreementDetails> GetArgDetail(Agreement finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<AgreementDetails> res = null;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Debitor cli = new cli_Debitor(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.GetArgDetail(finder, out ret);
            }
            else
            {
                Debitor db = new Debitor();
                try
                {
                    res = db.GetArgDetail(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetArgDetail() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = null;
                }
                db.Close();
            }
            return res;
        }

        public List<SettingsRequisites> GetSettingArea(SettingsRequisites finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<SettingsRequisites> res = null;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Debitor cli = new cli_Debitor(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.GetSettingArea(finder, out ret);
            }
            else
            {
                Debitor db = new Debitor();
                try
                {
                    res = db.GetSettingArea(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetSettingArea() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = null;
                }
                db.Close();
            }
            return res;
        }

        /// <summary>
        /// Сохранение соглашения
        /// </summary>
        /// <param name="finder">список расчета. В каждом элементе информация о соглашении</param>
        /// <param name="ret">результат</param>
        public void AgreementOpers(List<AgreementDetails> finder, enSrvOper oper, out Returns ret)
        {
            ret = Utils.InitReturns();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Debitor cli = new cli_Debitor(WCFParams.AdresWcfHost.CurT_Server);
                cli.AgreementOpers(finder, oper, out ret);
            }
            else
            {
                Debitor db = new Debitor();
                try
                {
                    switch (oper)
                    {
                        case enSrvOper.srvAddAgreement:
                            {
                                db.SaveAgreement(finder, out ret);
                                break;
                            }
                        case enSrvOper.srvDelAgreement:
                            {
                                db.DeleteAgreement(finder, out ret);
                                break;
                            }
                    }
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка SaveGetArg() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
        }

        /// <summary>
        /// Список должников
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public List<Debt> GetDebitors(DebtFinder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Debt> lsDebs = new List<Debt>();
            Debitor db = new Debitor();
            try
            {
                lsDebs.Clear();

                if (SrvRunProgramRole.IsBroker)
                {
                    //надо продублировать вызов к внутреннему хосту
                    cli_Debitor cli = new cli_Debitor(WCFParams.AdresWcfHost.CurT_Server);
                    lsDebs = cli.GetDebitors(finder, out ret);
                }
                else
                {
                    switch (finder.operation)
                    {
                        case DebtFinder.Operations.Find:
                            db.FindDebt(finder, out ret);
                            if (ret.result)
                                lsDebs = db.GetDebitors(finder, out ret);
                            else
                                lsDebs = null;
                            break;
                        case DebtFinder.Operations.Get:
                            lsDebs = db.GetDebitors(finder, out ret);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка вызова функции получения должников";
                if (Constants.Viewerror) ret.text += " : " + ex.Message;
                if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetDebitors() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            finally
            {

                db.Close();
            }
            return lsDebs;
        }

        public Returns SaveDealChecked(List<Deal> finder)
        {
            Returns ret = Utils.InitReturns();
            var db = new Debitor();
            try
            {
                if (SrvRunProgramRole.IsBroker)
                {
                    //надо продублировать вызов к внутреннему хосту
                    var cli = new cli_Debitor(WCFParams.AdresWcfHost.CurT_Server);
                    ret = cli.SaveDealChecked(finder);
                }
                else
                {
                    ret = db.SaveDealChecked(finder);
                }
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка вызова функции";
                if (Constants.Viewerror) ret.text += " : " + ex.Message;
                if (Constants.Debug) MonitorLog.WriteLog("Ошибка SaveDealChecked() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            finally
            {

                db.Close();
            }
            return ret;
        }


        /// <summary>
        /// Список дел
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>

        public List<Deal> GetDeals(DealFinder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Deal> lsDeals = new List<Deal>();
            lsDeals.Clear();

            Debitor db = new Debitor();
            try
            {
                switch (finder.operation)
                {
                    case DealFinder.Operations.Find:
                        db.FindDeal(finder, out ret);
                        if (ret.result)
                            lsDeals = db.GetDeals(finder, out ret);
                        else
                            lsDeals = null;
                        break;
                    case DealFinder.Operations.Get:
                        lsDeals = db.GetDeals(finder, out ret);
                        break;
                }
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка вызова функции получения должников";
                if (Constants.Viewerror) ret.text += " : " + ex.Message;
                if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetDeals() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            db.Close();
            return lsDeals;
        }

        /// <summary>
        /// Получает библиотеку со списками для заполнения полей поиска
        /// </summary>
        /// <returns>Библиотека со списками для заполнения полей поиска </returns>
        public Dictionary<string, Dictionary<int, string>> GetDebitorLists(DealFinder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            Debitor db = new Debitor();
            try
            {
                return db.GetDebitorLists(finder, out ret);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }
            finally
            {
                db.Close();
            }
        }

        public lawsuit_Data GetLavsuit(int nzp_lawsuit, int nzp_deal, out Returns ret)
        {
            ret = Utils.InitReturns();
            lawsuit_Data retData = new lawsuit_Data();
            retData.nzp_deal = nzp_deal;
            using (var db = new Debitor())
            {
                db.GetLawsuitData(nzp_lawsuit, ref retData, out ret);
                if (!ret.result) return null;
            }
            return retData;
        }

        public void SetLavsuit(lawsuit_Data Data, out Returns ret)
        {
            ret = Utils.InitReturns();
            lawsuit_Data retData = new lawsuit_Data();
            using (var db = new Debitor())
            {
                db.SetLawsuitData(Data, out ret);
            }
        }

        public void DeleteLavsuit(int nzp_lawsuit, out Returns ret)
        {
            ret = Utils.InitReturns();
            using (var db = new Debitor())
            {
                db.DeleteLawsuitData(nzp_lawsuit, out ret);
            }
        }

        public void SaveSetting(List<SettingsRequisites> finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Debitor cli = new cli_Debitor(WCFParams.AdresWcfHost.CurT_Server);
                cli.SaveSetting(finder, out ret);
            }
            else
            {
                Debitor db = new Debitor();
                try
                {
                    db.SaveSetting(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetDealHistory() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
        }

        public List<deal_states_history> GetDealHistory(Deal finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<deal_states_history> res = null;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Debitor cli = new cli_Debitor(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.GetDealHistory(finder, out ret);
            }
            else
            {
                Debitor db = new Debitor();
                try
                {
                    res = db.GetDealHistory(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetDealHistory() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = null;
                }
                db.Close();
            }
            return res;
        }

        public List<DealCharge> GetDealCharges(Deal finder, int yy, int mm, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<DealCharge> res = null;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Debitor cli = new cli_Debitor(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.GetDealCharges(finder, yy, mm, out ret);
            }
            else
            {
                Debitor db = new Debitor();
                try
                {
                    res = db.GetDealCharges(finder, yy, mm, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetDealCharges() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = null;
                }
                db.Close();
            }
            return res;
        }
        #region Получить список услуг
        public List<Service> GetService(Deal finder, int nzp_supp, out Returns ret)
        {
            ret = Utils.InitReturns();
            Debitor db = new Debitor();
            try
            {
                // Дописать реализацию
                return db.GetListServices(finder, nzp_supp, out ret);

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка : " + ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }
            finally
            {
                db.Close();
            }
        }
        #endregion

        #region Получить список поставщиков
        public List<Supplier> GetSupplier(Deal finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            Debitor db = new Debitor();
            try
            {
                // Дописать реализацию
                return db.GetListSupplier(finder, out ret);

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка : " + ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }
            finally
            {
                db.Close();
            }
        }
        #endregion

        public Returns GetDDLstDealOperations(out lawsuit_Files lstPreCourt, out lawsuit_Files lstCourt)
        {
            Returns ret = Utils.InitReturns();
            lstPreCourt = new lawsuit_Files();
            lstCourt = new lawsuit_Files();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Debitor cli = new cli_Debitor(WCFParams.AdresWcfHost.CurT_Server);
                // res = cli.GetDealHistory(finder, out ret);
            }
            else
            {
                Debitor db = new Debitor();
                try
                {
                    ret = db.GetDDLstData(out lstPreCourt, out lstCourt);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetDDLstDealOperations() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public List<lawsuit_Data> GetLawSuits(Deal finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<lawsuit_Data> res = null;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Debitor cli = new cli_Debitor(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.GetLawSuits(finder, out ret);
            }
            else
            {
                Debitor db = new Debitor();
                try
                {
                    res = db.GetLawSuits(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetLawSuits() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = null;
                }
                db.Close();
            }
            return res;
        }

        public int AddGroupOperation(Deal finder, int nzp_oper, ReportType type, out Returns ret)
        {
            ret = Utils.InitReturns();
            Debitor db = new Debitor();
            int res = -1;

            try
            {
                res = db.AddGroupOperation(finder, nzp_oper, type, out ret);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка вызова функции";
                if (Constants.Viewerror) ret.text += " : " + ex.Message;
                if (Constants.Debug) MonitorLog.WriteLog("Ошибка AddGroupOperation() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            db.Close();

            return res;
        }

        #region Уменьшение величины долга
        public Returns AddPerekidka(Deal finder, decimal money)
        {
            Returns ret = Utils.InitReturns();
            Debitor db = new Debitor();
            try
            {
                // Дописать реализацию
                return db.AddPerekidka(finder, money);

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.result = false;
                return ret;
            }
            finally
            {
                db.Close();
            }
        }
        #endregion

        public Returns CloseDeal(Deal finder)
        {
            Returns ret = Utils.InitReturns();


            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Debitor cli = new cli_Debitor(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.CloseDeal(finder);
            }
            else
            {
                Debitor db = new Debitor();
                try
                {
                    ret = db.CloseDeal(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetDealCharges() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    return ret;
                }
                db.Close();
            }
            return ret;
        }


        public Deal CreateDeal(Deal finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            Deal res = new Deal();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Debitor cli = new cli_Debitor(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.CreateDeal(finder, out ret);
            }
            else
            {
                Debitor db = new Debitor();
                try
                {
                    res = db.CreateDeal(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка CreateDeal() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    return null;
                }
                db.Close();
            }
            return res;
        }

        public decimal GetLawsuitPrice(int nzp_deal, out Returns ret)
        {
            Debitor db = new Debitor();
            decimal res = -1;
            ret = Utils.InitReturns();
            try
            {
                res = db.GetLawsuitPrice(nzp_deal, out ret);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка вызова функции";
                if (Constants.Viewerror) ret.text += " : " + ex.Message;
                if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetLawsuitPrice() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            db.Close();
            return res;
        }

        public bool ExistDeal(int nzp_kvar, out Returns ret)
        {
            ret = Utils.InitReturns();
            bool res = true;

            Debitor db = new Debitor();
            try
            {
                res = db.ExistDeal(nzp_kvar, out ret);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка вызова функции";
                if (Constants.Viewerror) ret.text += " : " + ex.Message;
                if (Constants.Debug) MonitorLog.WriteLog("Ошибка ExistDeal() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return false;
            }
            db.Close();

            return res;
        }

        public string GetDebtorList(ChargeFind finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            string res;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                var cli = new cli_Debitor(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.GetDebtorList(finder, out ret);
            }
            else
            {
                var db = new Debitor();
                try
                {
                    res = db.GetDebtorList(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetDebtorList() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    return null;
                }
                db.Close();
            }
            return res;
        }

    }
}
