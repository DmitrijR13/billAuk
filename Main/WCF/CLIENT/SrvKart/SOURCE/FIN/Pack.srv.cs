using System;
using System.Collections.Generic;
using System.Data;
using STCLINE.KP50.Client;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.Server
{
    public class srv_Pack : srv_Base, I_Pack //сервис Кассы
    {
        public Pack OperateWithPackAndGetIt(PackFinder finder, enSrvOper oper, out Returns ret)
        {
            ret = Utils.InitReturns();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                return cli.OperateWithPackAndGetIt(finder, oper, out ret);
            }
            else
            {
                try
                {
                    DbPack dbpack = new DbPack();
                    Pack pack = null;
                    switch (oper)
                    {
                        case enSrvOper.SrvLoad: pack = dbpack.LoadPack(finder, out ret); break;
                        case enSrvOper.srvSave: pack = dbpack.SavePack(finder, out ret); break;
                        case enSrvOper.srvSaveMain: pack = dbpack.SavePackMain(finder, out ret); break;
                        case enSrvOper.srvFinanceLoad:
                            List<Pack> list = dbpack.FindPack(finder, out ret);
                            if (ret.result && list != null && list.Count > 0) pack = list[0];
                            break;
                        default: ret = new Returns(false, "Неверное наименование операции"); break;
                    }
                    
                    dbpack.Close();
                    return pack;
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова операции с пачкой оплат";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка OperateWithPackAndGetIt(" + oper.ToString() + ")\n " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                return null;
            }
        }

        public Returns OperateWithPack(PackFinder finder, Pack.PackOperations oper)
        {
            Returns ret = Utils.InitReturns();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.OperateWithPack(finder, oper);
            }
            else
            {
                try
                {
                    DbPack dbpack = new DbPack();

                    switch (oper)
                    {
                        case Pack.PackOperations.CloseKassaPack: ret = dbpack.ClosePack(finder); break;
                        case Pack.PackOperations.Delete: ret = dbpack.DeletePack(finder); break;
                        case Pack.PackOperations.GetNextNumPackForOverPay: ret = dbpack.GetNextNumPackForOverPay(finder); break;
                        case Pack.PackOperations.Distribute:
                            if (!finder.is_super_pack)
                            {
                                DbCalcPack db1 = new DbCalcPack();
                                db1.PackFonTasks(finder.nzp_pack, finder.nzp_user, FonTaskTypeIds.DistributePack, out ret);  // Отдаем пачку на распределение
                                
                                finder.flag = Pack.Statuses.WaitingForDistribution.GetHashCode();
                                if (ret.result)
                                {
                                    db1.UpdatePackStatus(finder);
                                }
                                db1.Close();
                            }
                            else
                            {
                                finder.nzp_pack = 0; // иначе найдется только сама суперпачка, а не ее подпачки
                                finder.isCalcItogo = false;
                                List<Pack> list = GetPack(finder, enSrvOper.SrvFind, out ret);
                                if (ret.result)
                                {
                                    if (list != null && list.Count > 0)
                                    {
                                        DbCalcPack db1 = new DbCalcPack();
                                        foreach (Pack pack in list)
                                        {
                                            db1.PackFonTasks(pack.nzp_pack,pack.nzp_user, FonTaskTypeIds.DistributePack, out ret);  // Отдаем пачку на распределение
                                            if (!ret.result) break;
                                            else
                                            {
                                                pack.nzp_user = finder.nzp_user;
                                                pack.flag = Pack.Statuses.WaitingForDistribution.GetHashCode();
                                                db1.UpdatePackStatus(pack);
                                            }
                                        }
                                        
                                        if (ret.result)
                                        {
                                            finder.flag = Pack.Statuses.WaitingForDistribution.GetHashCode();
                                            db1.UpdatePackStatus(finder);
                                        }
                                        db1.Close();
                                    }
                                }
                            }
                            
                            break;
                        case Pack.PackOperations.CancelDistribution:
                            if (!finder.is_super_pack)
                            {
                                DbCalcPack db2 = new DbCalcPack();
                                db2.PackFonTasks(finder.nzp_pack,finder.nzp_user, FonTaskTypeIds.CancelPackDistribution, out ret);  // Отдаем пачку на распределение
                                
                                finder.flag = Pack.Statuses.WaitingForCancellationOfDistribution.GetHashCode();
                                if (ret.result)
                                {
                                    db2.UpdatePackStatus(finder);
                                }
                                db2.Close();
                            }
                            else
                            {
                                finder.nzp_pack = 0; // иначе найдется только сама суперпачка, а не ее подпачки
                                finder.isCalcItogo = false;
                                List<Pack> list = GetPack(finder, enSrvOper.SrvFind, out ret);
                                if (ret.result)
                                {
                                    if (list != null && list.Count > 0)
                                    {
                                        DbCalcPack db2 = new DbCalcPack();
                                        foreach (Pack pack in list)
                                        {
                                            db2.PackFonTasks(pack.nzp_pack, finder.nzp_user, FonTaskTypeIds.CancelPackDistribution, out ret);  // Отдаем пачку на отмену распределения
                                            if (!ret.result) break;
                                            else
                                            {
                                                pack.nzp_user = finder.nzp_user;
                                                pack.flag = Pack.Statuses.WaitingForCancellationOfDistribution.GetHashCode();
                                                db2.UpdatePackStatus(pack);
                                            }
                                        }
                                        
                                        if (ret.result)
                                        {
                                            finder.flag = Pack.Statuses.WaitingForCancellationOfDistribution.GetHashCode();
                                            db2.UpdatePackStatus(finder);
                                        }
                                        db2.Close();
                                    }
                                }
                            }
                            break;
                        case Pack.PackOperations.CancelDistributionAndDelete:
                            if (!finder.is_super_pack)
                            {
                                DbCalcPack db2 = new DbCalcPack();
                                db2.PackFonTasks(finder.nzp_pack, finder.nzp_user, FonTaskTypeIds.CancelDistributionAndDeletePack, out ret);  // Отдаем пачку на распределение
                                
                                finder.flag = Pack.Statuses.WaitingForCancellationOfDistribution.GetHashCode();
                                if (ret.result)
                                {
                                    db2.UpdatePackStatus(finder);
                                }
                                db2.Close();
                            }
                            else
                            {
                                finder.nzp_pack = 0; // иначе найдется только сама суперпачка, а не ее подпачки
                                finder.isCalcItogo = false;
                                List<Pack> list = GetPack(finder, enSrvOper.SrvFind, out ret);
                                if (ret.result)
                                {
                                    if (list != null && list.Count > 0)
                                    {
                                        DbCalcPack db2 = new DbCalcPack();
                                        foreach (Pack pack in list)
                                        {
                                            db2.PackFonTasks(pack.nzp_pack, finder.nzp_user, FonTaskTypeIds.CancelDistributionAndDeletePack, out ret);  // Отдаем пачку на отмену распределения
                                            if (!ret.result) break;
                                            else
                                            {
                                                pack.nzp_user = finder.nzp_user;
                                                pack.flag = Pack.Statuses.WaitingForCancellationOfDistribution.GetHashCode();
                                                db2.UpdatePackStatus(pack);
                                            }
                                        }
                                        
                                        if (ret.result)
                                        {
                                            finder.flag = Pack.Statuses.WaitingForCancellationOfDistribution.GetHashCode();
                                            db2.UpdatePackStatus(finder);
                                        }
                                        db2.Close();
                                    }
                                }
                            }
                            break;
                        default: ret = new Returns(false, "Неверное наименование операции"); break;
                    }
                    dbpack.Close();
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции получения пачки оплат";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка OperateWithPack(" + oper.ToString() + ")\n " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return ret;
        }

        public Pack_ls OperateWithPackLsAndGetIt(Pack_ls finder, Pack_ls.OperationsWithGetting oper, out Returns ret)
        {
            ret = Utils.InitReturns();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                return cli.OperateWithPackLsAndGetIt(finder, oper, out ret);
            }
            else
            {
                try
                {
                    DbPack dbpack = new DbPack();
                    Pack_ls pack = null;
                    switch (oper)
                    {
                        case Pack_ls.OperationsWithGetting.LoadFromKassa: pack = dbpack.LoadKassaPackLs(finder, out ret); break;
                        case Pack_ls.OperationsWithGetting.SaveToKassa: pack = dbpack.SaveKassaPackLs(finder, out ret); break;
                        case Pack_ls.OperationsWithGetting.SavePackLs: pack = dbpack.SavePackLs(finder, out ret); break;
                        default: ret = new Returns(false, "Неверное наименование операции"); break;
                    }

                    dbpack.Close();
                    return pack;
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова операции с пачкой оплат";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка OperateWithPackLsAndGetIt(" + oper.ToString() + ")\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                return null;
            }
        }

        public Returns OperateWithPackLs(Pack_ls finder, Pack_ls.OperationsWithoutGetting oper)
        {
            Returns ret = Utils.InitReturns();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.OperateWithPackLs(finder, oper);
            }
            else
            {
                try
                {
                    DbPack dbpack = new DbPack();

                    switch (oper)
                    {
                        case Pack_ls.OperationsWithoutGetting.DeleteFromKassa: ret = dbpack.DeleteKassaPackLs(finder); break;
                        case Pack_ls.OperationsWithoutGetting.DeleteFromFinances: ret = dbpack.DeleteFinancePackLs(finder); break;
                        case Pack_ls.OperationsWithoutGetting.FinanceSave: ret = dbpack.SaveFinancePackLs(finder); break;
                        case Pack_ls.OperationsWithoutGetting.ChangeCase: ret = dbpack.ChangeCasePackLs(finder); break;
                        case Pack_ls.OperationsWithoutGetting.ShowInCase: ret = dbpack.ShowButtonInCase(finder); break;
                        case Pack_ls.OperationsWithoutGetting.CheckPkod: ret = dbpack.CheckPkod(finder); break;
                        case Pack_ls.OperationsWithoutGetting.Distribute:
                        case Pack_ls.OperationsWithoutGetting.CancelDistribution:
                            ret = dbpack.DistributeOrCancelPack(finder, oper, true);
                            break;
                        default: ret = new Returns(false, "Неверное наименование операции"); break;
                    }
                    dbpack.Close();
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции работы с оплатой";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка OperateWithPackLs(" + oper.ToString() + ")\n " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return ret;
        }

        public Returns OperateWithListPackLs(List<Pack_ls> finder, enSrvOper oper)
        {
            Returns ret = Utils.InitReturns();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.OperateWithListPackLs(finder, oper);
            }
            else
            {
                string oper_text = "";
                try
                {
                    DbPack dbpack = new DbPack();

                    switch (oper)
                    {
                        case enSrvOper.srvBasketDistribute:
                            {
                                oper_text = "распределения оплаты в корзине";
                                ret = new ClassDB().RunSqlAction(new FinderObjectType<List<Pack_ls>>(finder), new DbPack().DistributePackLs).GetReturns();
                                break;
                            }
                        case enSrvOper.srvBasketRepair:
                            {
                                //ret = dbpack.RepairPackLs(finder); break;
                                //Finder finder;
                                oper_text = "исправления оплаты в корзине";
                                ret = new ClassDB().RunSqlAction(new FinderObjectType<List<Pack_ls>>(finder), new DbPack().RepairPackLs).GetReturns();
                                break;
                            };

                        case enSrvOper.srvReallocatePackInBasket:
                            {
                                oper_text = "Перераспределение оплат";
                                ret = dbpack.ReallocatePacks(finder);
                                break;
                            }
                        case enSrvOper.srvDeleteListPackLS:
                            {
                                oper_text = "Удаление списка оплат";
                                ret = dbpack.DeleteListPackLs(finder);
                                break;
                            }
                        default: ret = new Returns(false, "Неверное наименование операции"); break;
                    }
                    dbpack.Close();
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции " + oper_text;
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка OperateWithListPackLs(" + oper.ToString() + ")\n " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return ret;
        }

        public Returns UploadPackFromDBF(string nzp_user, string fileName)
        {
            Returns ret = Utils.InitReturns();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.UploadPackFromDBF(nzp_user, fileName);
            }
            else
            {
                try
                {
                    DbPack dbpack = new DbPack();
                    ret = dbpack.UploadPackFromDBF(nzp_user, fileName);
                    dbpack.Close();
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки файла оплат DBF";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка UploadPackFromDBF()\n " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return ret;
        }

        public Returns UploadPackFromWeb(int nzpPack, int nzp_user)
        {
            Returns ret = Utils.InitReturns();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.UploadPackFromWeb(nzpPack, nzp_user);
            }
            else
            {
                try
                {
                    DbPack dbpack = new DbPack();
                    ret = dbpack.UploadPackFromWeb(nzpPack, nzp_user);
                    dbpack.Close();
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки файла оплат ";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка UploadPackFromWeb()\n " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return ret;
        }

        public Returns PutTaskDistribLs(Dictionary<int, int> listPackLs, int nzp_user)
        {
             Returns ret = Utils.InitReturns();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.PutTaskDistribLs(listPackLs, nzp_user);
            }
            else
            {
                try
                {
                    DbPack dbpack = new DbPack();
                    ret = dbpack.PutTaskDistribLs(listPackLs, nzp_user);
                    dbpack.Close();
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки файла оплат ";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка PutTaskDistribLs()\n " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return ret;
        }
        

        public List<Bank> LoadBankForKassa(Bank finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Bank> list = null;
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.LoadBankForKassa(finder, out ret);
            }
            else
            {
                try
                {
                    DbPack dbpack = new DbPack();
                    list = dbpack.LoadBankForKassa(finder, out ret);
                    dbpack.Close();
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки банков";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LoadBankForKassa()\n " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return list;
        }

        public List<Ulica> LoadUlica(Ulica finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Ulica> list = null;
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.LoadUlica(finder, out ret);
            }
            else
            {
                try
                {
                    DbPack dbpack = new DbPack();
                    list = dbpack.LoadUlica(finder, out ret);
                    dbpack.Close();
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки улиц";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LoadUlica()\n " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return list;
        }

        public List<Ls> GetPackLsList(string finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Ls> list = null;
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.GetPackLsList(finder, out ret);
            }
            else
            {
                try
                {
                    DbPack dbpack = new DbPack();
                    list = dbpack.GetPackLsList(finder, out ret);
                    dbpack.Close();
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка загрузки";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetPackLsList()\n " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return list;
        }

        public List<Dom> LoadDom(Dom finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Dom> list = null;
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.LoadDom(finder, out ret);
            }
            else
            {
                try
                {
                    DbPack dbpack = new DbPack();
                    list = dbpack.LoadDom(finder, out ret);
                    dbpack.Close();
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки домов";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LoadDom()\n " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return list;
        }

        public List<Ls> LoadKvar(Ls finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Ls> list = null;
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.LoadKvar(finder, out ret);
            }
            else
            {
                try
                {
                    DbPack dbpack = new DbPack();
                    list = dbpack.LoadKvar(finder, out ret);
                    dbpack.Close();
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки квартир";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LoadKvar()\n " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return list;
        }     

        public List<Ls> LoadLsForKassa(Ls finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Ls> ls = null;
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                ls = cli.LoadLsForKassa(finder, out ret);
            }
            else
            {
                try
                {
                    DbPack dbpack = new DbPack();
                    ls = dbpack.LoadLsForKassa(finder, out ret);
                    dbpack.Close();
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки данных л/с";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LoadLsForKassa()\n " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return ls;
        }

        public List<Pack_errtype> LoadErrorTypes(Finder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Pack_errtype> list = null;
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.LoadErrorTypes(finder, out ret);
            }
            else
            {
                try
                {
                    DbPack dbpack = new DbPack();
                    list = dbpack.LoadErrorTypes(finder, out ret);
                    dbpack.Close();
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки типов ошибок";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LoadErrorTypes()\n " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return list;
        }

        public List<Pack_errtype> GetBasketErr(Pack_ls finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Pack_errtype> list = null;
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.GetBasketErr(finder, out ret);
            }
            else
            {
                try
                {
                    DbPack dbpack = new DbPack();
                    list = dbpack.GetBasketErr(finder, out ret);
                    dbpack.Close();
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки типов ошибок";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetBasketErr()\n " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return list;
        }

        public List<Pack_ls> GetPackLs(Pack_ls finder, enSrvOper oper, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Pack_ls> list = null;
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.GetPackLs(finder, oper, out ret);
            }
            else
            {
                try
                {
                    DbPack dbpack = new DbPack();
                    switch (oper)
                    {
                        case enSrvOper.srvFinanceFind: list = dbpack.FindFinancePackLs(finder, out ret); break;
                        case enSrvOper.srvKassaFind: list = dbpack.GetPackLs(finder, out ret); break;
                        case enSrvOper.srvGetCase: list = dbpack.GetCasePack_ls(finder, out ret); break;
                        case enSrvOper.srvGetBasket: list = dbpack.GetBasketPack_ls(finder, out ret); break;
                    }
                    dbpack.Close();
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции получения списка оплат";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetPackLs(" + oper.ToString() + ")\n " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return list;
        }       

        public List<Pack> GetPack(PackFinder finder, enSrvOper oper, out Returns ret)
        {
            ret = Utils.InitReturns();
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                return cli.GetPack(finder, oper, out ret);
            }
            else
            {
                try
                {
                    DbPack dbpack = new DbPack();
                    List<Pack> list = null;
                    switch (oper)
                    {
                        case enSrvOper.SrvFind: list = dbpack.FindPack(finder, out ret); break;
                        default: ret = new Returns(false, "Неверное наименование операции"); break;
                    }

                    dbpack.Close();
                    return list;
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка функции получения списка пачек";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetPack(" + oper.ToString() + ")\n " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                return null;
            }
        }

        public List<PackStatus> GetPackStatus(PackStatus finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<PackStatus> list = null;
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                //надо продублировать вызов к внутреннему хосту
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.GetPackStatus(finder, out ret);
            }
            else
            {
                try
                {
                    DbPack db = new DbPack();
                    list = db.GetPackStatus(finder, out ret);
                    db.Close();
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка в функции получения списка статусов пачек";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetPackStatus()\n " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return list;
        }

        public Returns SaveOperDay(Pack finder)
        {
            Returns ret = Utils.InitReturns();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.SaveOperDay(finder);
            }
            else
            {
                try
                {
                    DbPack dbpack = new DbPack();
                    ret = dbpack.SaveOperDay(finder);
                    dbpack.Close();
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции сохранения операционного дня";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка SaveOperDay()\n " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return ret;
        }

        public Returns CancelPlat(Finder finder, List<Pack_ls> list)
        {
            Returns ret = Utils.InitReturns();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.CancelPlat(finder, list);
            }
            else
            {
                try
                {
                    DbPack dbpack = new DbPack();
                    ret = dbpack.CancelPlat(finder, list);
                    dbpack.Close();
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции отмены платежей";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка CancelPlat()\n " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return ret;
        }

        public List<Pack_log> GetPackLog(Pack_log finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Pack_log> list = null;
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.GetPackLog(finder, out ret);
            }
            else
            {
                try
                {
                    DbPack dbpack = new DbPack();
                    list = dbpack.FindPackLog(finder, out ret);
                    dbpack.Close();
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка в функции получения списка операций с пачкой";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetPackLog()\n " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return list;
        }

        public List<BankRequisites> GetBankRequisites(BankRequisites finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<BankRequisites> list = null;
            if (SrvRun.isBroker)
            {
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.GetBankRequisites(finder, out ret);
            }
            else
            {
                try
                {
                    DbPack dbpack = new DbPack();
                    list = dbpack.GetBankRequisites(finder, out ret);
                    dbpack.Close();
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка в функции получения списка банковских реквизитов";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetBankRequisites()\n " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return list;
        }

        public List<BankRequisites> GetSourceBankList(BankRequisites finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<BankRequisites> list = null;
            if (SrvRun.isBroker)
            {
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.GetSourceBankList(finder, out ret);
            }
            else
            {
                try
                {
                    DbPack dbpack = new DbPack();
                    list = dbpack.GetSourceBankList(finder, out ret);
                    dbpack.Close();
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка в функции получения списка банковских реквизитов";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetSourceBankList()\n " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return list;
        }

        public List<DogovorRequisites> GetDogovorRequisites(DogovorRequisites finder, enSrvOper oper, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<DogovorRequisites> list = null;
            if (SrvRun.isBroker)
            {
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.GetDogovorRequisites(finder, oper ,out ret);
            }
            else
            {
                try
                {
                    DbPack dbpack = new DbPack();
                    switch(oper)
                    {
                        case enSrvOper.GetDogovorList : list = dbpack.GetDogovorList(finder, out ret); break; 
                        case enSrvOper.GetOsnovList : list = dbpack.GetOsnovList(finder,out ret); break;
                    }
                    
                    dbpack.Close();
                
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка в функции получения списка реквизитов договоров";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetDogovorRequisites()\n " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return list;
        }

        public List<DogovorRequisites> GetSourceBankList(DogovorRequisites finder, enSrvOper oper, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<DogovorRequisites> list = null;
            if (SrvRun.isBroker)
            {
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.GetSourceBankList(finder, oper, out ret);
            }
            else
            {
                try
                {
                    DbPack dbpack = new DbPack();
                    list = dbpack.GetSourceBankList(finder, out ret); break;

                    dbpack.Close();

                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка в функции получения списка реквизитов договоров";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetDogovorRequisites()\n " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return list;
        }



        public List<ContractRequisites> GetContractRequisites(ContractRequisites finder, enSrvOper oper, out Returns ret)
        {
            ret = Utils.InitReturns(); 
            List<ContractRequisites> list = null;
            if (SrvRun.isBroker)
            {
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.GetContractRequisites(finder, oper, out ret);
            }
            else
            {
                try
                {
                    DbPack dbpack = new DbPack();
                    switch (oper)
                    {
                        case enSrvOper.GetContractList: list = dbpack.GetContractList(finder, out ret); break;
                        case enSrvOper.GetSupp: list = dbpack.GetSupp(finder, out ret); break;
                        case enSrvOper.GetAreaLS: list = dbpack.GetAreaLS(finder, out ret); break;
                        case enSrvOper.GetBanks: list = dbpack.GetBanks(finder, out ret); break;
                    }
                    dbpack.Close();
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка в функции получения списка реквизитов контрактов";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetContractRequisites()\n " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return list;
        } 



        /// <summary>
        /// Изменение банк. реквизитов подрядчика
        /// </summary>
        public bool ChangeBankRequisites(BankRequisites finder, enSrvOper oper, out Returns ret)
        {
            ret = Utils.InitReturns();
            bool res = false;
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.ChangeBankRequisites(finder, oper, out ret);
            }
            else
            {
                try
                {
                    DbPack dbpack = new DbPack();
                    switch (oper)
                    {
                        case enSrvOper.AddBankRequisites: res = dbpack.AddBankRequisites(finder, out ret); break;
                        case enSrvOper.DelBankRequisites: res = dbpack.DelBankRequisites(finder, out ret); break;
                        case enSrvOper.UpdateBankRequisites: res = dbpack.UpdateBankRequisites(finder, out ret); break;
                    }
                    dbpack.Close();
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции операции с банковскими реквизитами подрядчика";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка ChangeBankRequisites(" + oper.ToString() + ")\n " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return res;
        }


        /// <summary>
        /// Изменение реквизитов договора подрядчика
        /// </summary>
        public bool ChangeDogovorRequisites(DogovorRequisites finder, enSrvOper oper, out Returns ret)
        {
            ret = Utils.InitReturns();
            bool res = false;
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.ChangeDogovorRequisites(finder, oper, out ret);
            }
            else
            {
                try
                {
                    DbPack dbpack = new DbPack();
                    switch (oper)
                    {
                        case enSrvOper.AddDogovorRequisites: res = dbpack.AddDogovorRequisites(finder, out ret); break;
                        case enSrvOper.DelDogovorRequisites: res = dbpack.DelDogovorRequisites(finder, out ret); break;
                        case enSrvOper.UpdateDogovorRequisites: res = dbpack.UpdateDogovorRequisites(finder, out ret); break;
                    }
                    dbpack.Close();
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции операции с банковскими реквизитами подрядчика";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка ChangeDogovorRequisites(" + oper.ToString() + ")\n " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return res;
        }

        /// <summary>
        /// Изменение реквизитов контракта
        /// </summary>
        public bool ChangeContractRequisites(ContractRequisites finder, enSrvOper oper, out Returns ret)
        {
            ret = Utils.InitReturns();
            bool res = false;
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.ChangeContractRequisites(finder, oper, out ret);
            }
            else
            {
                try
                {
                    DbPack dbpack = new DbPack();
                    switch (oper)
                    {
                        case enSrvOper.AddContractRequisites: res = dbpack.AddContractRequisites(finder, out ret); break;
                        case enSrvOper.DelContractRequisites: res = dbpack.DelContractRequisites(finder, out ret); break;
                        case enSrvOper.UpdateContractRequisites: res = dbpack.UpdateContractRequisites(finder, out ret); break;
                    }
                    dbpack.Close();
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции операции с реквизитами контракта";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка ChangeContractRequisites(" + oper.ToString() + ")\n " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return res;
        }

        public List<FnSupplier> GetFnSupplier(FnSupplier finder, enSrvOper oper, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<FnSupplier> list = null;
            if (SrvRun.isBroker)
            {
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.GetFnSupplier(finder, oper, out ret);
            }
            else
            {
                try
                {
                    DbPack dbpack = new DbPack();
                    list = dbpack.FindFnSupplier(finder, out ret);
                    dbpack.Close();
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка в функции получения списка распределений оплат лицевого счета";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetFnSupplier(" + oper.ToString() + ")\n " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return list;
        }

        public DataSet GetDistribLog(PackFinder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            DataSet ds = null;

            try
            {
                if (SrvRun.isBroker)
                {
                    // продублировать вызов к внутреннему хосту
                    cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                    ds = cli.GetDistribLog(finder, out ret);
                }
                else
                {
                    ReturnsObjectType<DataSet> r = new ClassDB().RunSqlAction(finder, new DbPack().GetDistribLog);
                    ret = r.GetReturns();
                    ds = r.returnsData;
                }
            }
            catch (Exception ex)
            {
                ret = Utility.ExceptionUtility.OnException(ex, System.Reflection.MethodBase.GetCurrentMethod().Name);
            }

            return ds;
        }

        public ReturnsType FindErrorInPackLs(PackFinder finder)
        {
            try
            {
                if (SrvRun.isBroker)
                {
                    // продублировать вызов к внутреннему хосту
                    cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                    return cli.FindErrorInPackLs(finder);
                }
                else
                {
                    return new ClassDB().RunSqlAction(finder, new DbPack().FindErrorInPackLs);
                }
            }
            catch (Exception ex)
            {
                Returns ret = Utility.ExceptionUtility.OnException(ex, System.Reflection.MethodBase.GetCurrentMethod().Name);
                return new ReturnsType(ret.result, ret.text, ret.tag);
            }
        }

        public ReturnsType FindErrorInFnSupplier(PackFinder finder)
        {
            try
            {
                if (SrvRun.isBroker)
                {
                    // продублировать вызов к внутреннему хосту
                    cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                    return cli.FindErrorInFnSupplier(finder);
                }
                else
                {
                    return new ClassDB().RunSqlAction(finder, new DbPack().FindErrorInFnSupplier);
                }
            }
            catch (Exception ex)
            {
                Returns ret = Utility.ExceptionUtility.OnException(ex, System.Reflection.MethodBase.GetCurrentMethod().Name);
                return new ReturnsType(ret.result, ret.text, ret.tag);
            }
        }

        public ReturnsType GenContDistribPayments(Payments finder)
        {
            try
            {
                if (SrvRun.isBroker)
                {
                    // продублировать вызов к внутреннему хосту
                    cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                    return cli.GenContDistribPayments(finder);
                }
                else
                {
                    DbPack db = new DbPack();
                    Returns ret = db.GenConDistrPayments(finder);
                    return new ReturnsType(ret.result, ret.text, ret.tag);
                }
            }
            catch (Exception ex)
            {
                Returns ret = Utility.ExceptionUtility.OnException(ex, System.Reflection.MethodBase.GetCurrentMethod().Name);
                return new ReturnsType(ret.result, ret.text, ret.tag);
            }
        }

        public decimal GetLsSum(Saldo finder, GetLsSumOperations operation, out Returns ret)
        {
            ret = Utils.InitReturns();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                return cli.GetLsSum(finder, operation, out ret);
            }
            else
            {
                decimal sum = 0;
                try
                {
                    switch (operation)
                    { 
                        case GetLsSumOperations.GetNachKOplate:
                            DbCharge dbcharge = new DbCharge();
                            sum = dbcharge.GetSumKOplate(finder, out ret);
                            dbcharge.Close();
                            return sum;
                        
                        case GetLsSumOperations.GetCurrentDolg:
                            DbCharge dbcharge2 = new DbCharge();
                            sum = dbcharge2.GetSumKOplate(finder, out ret);
                            dbcharge2.Close();
                            if (!ret.result) return 0;  
                            return sum;

                        case GetLsSumOperations.GetSumOutSaldo:
                            DbCharge dbcharge3 = new DbCharge();
                            sum = dbcharge3.GetSumFromCharge(finder, GetLsSumOperations.GetSumOutSaldo, out ret);
                            dbcharge3.Close();
                            if (!ret.result) return 0;
                            return sum;
                    }
                    
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
        }

        public List<ChargeForDistribSum> GetSumsForDistrib(ChargeForDistribSum finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<ChargeForDistribSum> list = null;
            if (SrvRun.isBroker)
            {
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.GetSumsForDistrib(finder, out ret);
            }
            else
            {
                try
                {
                    DbPack dbpack = new DbPack();
                    list = dbpack.GetSumsForDistrib(finder, out ret);
                    dbpack.Close();
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка в функции получения списка сумм";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetSumsForDistrib()\n " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return list;
        }

        public Returns SaveManualDistrib(List<ChargeForDistribSum> listfinder)
        {
            Returns ret = Utils.InitReturns();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.SaveManualDistrib(listfinder);
            }
            else
            {
                try
                {
                    DbPack dbpack = new DbPack();
                    ret = dbpack.SaveManualDistrib(listfinder);
                    dbpack.Close();
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции сохранения";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка SaveManualDistrib()\n " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return ret;
        }

        public Returns CreatePackOverPayment(Pack finder)
        {
            Returns ret = Utils.InitReturns();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.CreatePackOverPayment(finder);
            }
            else
            {
                try
                {
                    DbCalcPack dbpack = new DbCalcPack();
                    ret = dbpack.CreatePackOverPayment(finder);
                    dbpack.Close();
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции создания пачки с переплатами";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка CreatePackOverPayment()\n " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return ret;
        }

        //----------------------------------------------------------------------
        public Returns BankPayment(FinderAddPackage finder)
        //----------------------------------------------------------------------
        {
            Returns ret;

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.BankPayment(finder);
            }
            else
            {
                DbPack db = new DbPack();
                try
                {
                    ret = db.BankPayment(finder);
                }
                catch (Exception ex)
                {
                    ret = new Returns(false);
                    ret.result = false;
                    ret.text = "Ошибка вызова функции добавления новой выгрузки";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка BankPayment \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }

            return ret;
        }

        public Returns ChangeOperDay(OperDayFinder finder, out string date_oper, out string filename, out RecordMonth calcmonth)
        {
            Returns ret;

            date_oper = "";
            filename = "";
            calcmonth = Points.CalcMonth;

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.ChangeOperDay(finder, out date_oper, out filename, out calcmonth);
            }
            else
            {
                DbPack db = new DbPack();
                try
                {
                    ret = db.ChangeOperDay(finder, out date_oper, out filename, out calcmonth);
                }
                catch (Exception ex)
                {
                    ret = new Returns(false);
                    ret.result = false;
                    ret.text = "Ошибка вызова функции смены операционного дня";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка ChangeOperDay \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }

            return ret;
        }

        //----------------------------------------------------------------------
        public Returns SaveCheckSend(Delete_payment finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.SaveCheckSend(finder,out ret);
            }
            else
            {
                DbPack db = new DbPack();
                try
                {
                    ret = db.SaveCheckSend(finder,out ret);
                }
                catch (Exception ex)
                {
                    ret = new Returns(false);
                    ret.result = false;
                    ret.text = "Ошибка вызова функции - сохранение платежей обработаны банком";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка SaveCheckSend \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }

            return ret;
        }

        //----------------------------------------------------------------------
        public List<BankPayers> FormingSpisok(List<BankPayers> allSpisok, FinderAddPackage finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<BankPayers> list = null;
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.FormingSpisok(allSpisok, finder, out ret);
            }
            else
            {
                try
                {
                    DbPack dbpack = new DbPack();
                    list = dbpack.FormingSpisok(allSpisok, finder, out ret);
                    dbpack.Close();
                }
                catch (Exception ex)
                {
                    ret = new Returns(false);
                    ret.result = false;
                    ret.text = "Ошибка вызова функции - фильтрация контрагентов";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка FormingSpisok \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return list;
        }

        //----------------------------------------------------------------------
        public List<_TypeBC> LoadTypeBC(out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<_TypeBC> list = null;
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.LoadTypeBC(out ret);
            }
            else
            {
                try
                {
                    DbPack dbpack = new DbPack();
                    list = dbpack.LoadTypeBC(out ret);
                    dbpack.Close();
                }
                catch (Exception ex)
                {
                    ret = new Returns(false);
                    ret.result = false;
                    ret.text = "Ошибка вызова функции - загрузка списка форматов";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LoadTypeBC \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return list;
        }

        //----------------------------------------------------------------------
        public Returns AddFormat()
        //----------------------------------------------------------------------
        {
            Returns ret;

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.AddFormat();
            }
            else
            {
                DbPack db = new DbPack();
                try
                {
                    ret = db.AddFormat();
                }
                catch (Exception ex)
                {
                    ret = new Returns(false);
                    ret.result = false;
                    ret.text = "Ошибка вызова функции - добавления нового формата";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка AddFormat() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }

            return ret;
        }

        //----------------------------------------------------------------------
        public Returns SaveFormat(_TypeBC typ)
        //----------------------------------------------------------------------
        {
            Returns ret;

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.SaveFormat(typ);
            }
            else
            {
                DbPack db = new DbPack();
                try
                {
                    ret = db.SaveFormat(typ);
                }
                catch (Exception ex)
                {
                    ret = new Returns(false);
                    ret.result = false;
                    ret.text = "Ошибка вызова функции - изменение формата";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка SaveFormat() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }

            return ret;
        }

        //----------------------------------------------------------------------
        public Returns DeleteFormat(_TypeBC typ)
        //----------------------------------------------------------------------
        {
            Returns ret;

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.DeleteFormat(typ);
            }
            else
            {
                DbPack db = new DbPack();
                try
                {
                    ret = db.DeleteFormat(typ);
                }
                catch (Exception ex)
                {
                    ret = new Returns(false);
                    ret.result = false;
                    ret.text = "Ошибка вызова функции - удаление формата";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка DeleteFormat() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }

            return ret;
        }

        //----------------------------------------------------------------------
        public List<_TagsBC> GetListTags(int indexFormat, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<_TagsBC> list = null;
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.GetListTags(indexFormat, out ret);
            }
            else
            {
                try
                {
                    DbPack dbpack = new DbPack();
                    list = dbpack.GetListTags(indexFormat, out ret);
                    dbpack.Close();
                }
                catch (Exception ex)
                {
                    ret = new Returns(false);
                    ret.result = false;
                    ret.text = "Ошибка вызова функции - загрузка тегов формата";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetListTags \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return list;
        }

        //----------------------------------------------------------------------
        public Returns AddTag(int indexFormat)
        //----------------------------------------------------------------------
        {
            Returns ret;

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.AddTag(indexFormat);
            }
            else
            {
                DbPack db = new DbPack();
                try
                {
                    ret = db.AddTag(indexFormat);
                }
                catch (Exception ex)
                {
                    ret = new Returns(false);
                    ret.result = false;
                    ret.text = "Ошибка вызова функции - добавление тега";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка AddTag() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }

            return ret;
        }

        //----------------------------------------------------------------------
        public Returns DeleteTag(int indexTag)
        //----------------------------------------------------------------------
        {
            Returns ret;

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.DeleteTag(indexTag);
            }
            else
            {
                DbPack db = new DbPack();
                try
                {
                    ret = db.DeleteTag(indexTag);
                }
                catch (Exception ex)
                {
                    ret = new Returns(false);
                    ret.result = false;
                    ret.text = "Ошибка вызова функции - удаления тега";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка DeleteTag() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }

            return ret;
        }

        //----------------------------------------------------------------------
        public Returns SaveTag( _TagsBC tag)
        //----------------------------------------------------------------------
        {
            Returns ret;

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.SaveTag( tag);
            }
            else
            {
                DbPack db = new DbPack();
                try
                {
                    ret = db.SaveTag(tag);
                }
                catch (Exception ex)
                {
                    ret = new Returns(false);
                    ret.result = false;
                    ret.text = "Ошибка вызова функции - сохраниения тега";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка SaveTag() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }

            return ret;
        }

        //----------------------------------------------------------------------
        public Returns UpTag(_TagsBC finder)
        //----------------------------------------------------------------------
        {
            Returns ret;

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.UpTag(finder);
            }
            else
            {
                DbPack db = new DbPack();
                try
                {
                    ret = db.UpTag(finder);
                }
                catch (Exception ex)
                {
                    ret = new Returns(false);
                    ret.result = false;
                    ret.text = "Ошибка вызова функции - изменения тега";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка UpTag() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }

            return ret;
        }

        //----------------------------------------------------------------------
        public Returns DownTag(_TagsBC finder)
        //----------------------------------------------------------------------
        {
            Returns ret;

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.DownTag(finder);
            }
            else
            {
                DbPack db = new DbPack();
                try
                {
                    ret = db.DownTag(finder);
                }
                catch (Exception ex)
                {
                    ret = new Returns(false);
                    ret.result = false;
                    ret.text = "Ошибка вызова функции - добавление тега";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка DownTag() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }

            return ret;
        }

        //----------------------------------------------------------------------
        public List<CalcMethod> ListBcFields(Finder finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<CalcMethod> list = null;
            if (SrvRun.isBroker)
            {
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.ListBcFields(finder, out ret);
            }
            else
            {
                try
                {
                    DbPack dbpack = new DbPack();
                    list = dbpack.ListBcFields(finder, out ret);
                    dbpack.Close();
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка в функции получения списка заполняемых полей";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка ListBcFields()\n " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return list;
        }

        //----------------------------------------------------------------------
        public List<CalcMethod> ListBcRowType(Finder finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<CalcMethod> list = null;
            if (SrvRun.isBroker)
            {
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.ListBcRowType(finder, out ret);
            }
            else
            {
                try
                {
                    DbPack dbpack = new DbPack();
                    list = dbpack.ListBcRowType(finder, out ret);
                    dbpack.Close();
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка в функции получения списка типа тега";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка ListBcRowType()\n " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return list;
        }
        public Returns FormPacksSbPay(EFSReestr finder, PackFinder packfinder)
        {
            Returns ret;

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.FormPacksSbPay(finder, packfinder);
            }
            else
            {
                DbPack db = new DbPack();
                try
                {
                    ret = db.FormPacksSbPay(finder, packfinder);
                }
                catch (Exception ex)
                {
                    ret = new Returns(false);
                    ret.result = false;
                    ret.text = "Ошибка вызова функции - формирования пачек";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка FormPacksSbPay() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }

            return ret;
        }

        public Returns UploadChangesServSupp(ReestrChangesServSupp finder)
        {
            Returns ret;

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.UploadChangesServSupp(finder);
            }
            else
            {
                try
                {
                    AUploadChangesServSuppDelegate dlgt = new AUploadChangesServSuppDelegate(this.AUploadChangesServSupp);

                    //object o = new object();
                    AsyncCallback cb = new AsyncCallback(ACallback);
                    IAsyncResult ar = dlgt.BeginInvoke(finder, cb, dlgt);              
                    ret = Utils.InitReturns();
                 //   ret = db.UploadChangesServSupp(finder);
                }
                catch (Exception ex)
                {
                    ret = new Returns(false);
                    ret.result = false;
                    ret.text = "Ошибка вызова функции - выгрузка в банк";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка UploadChangesServSupp() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
               // db.Close();
            }

            return ret;
        }

        public Returns AUploadChangesServSupp(ReestrChangesServSupp finder)
        {
            Returns ret = Utils.InitReturns();
            DbPack db = new DbPack();
            try
            {
                db.UploadChangesServSupp(finder);
            }
            catch (Exception ex)
            {
                ret = new Returns(false);
                ret.result = false;
                ret.text = "Ошибка вызова функции - выгрузка в банк";
                if (Constants.Viewerror) ret.text += " : " + ex.Message;
                if (Constants.Debug) MonitorLog.WriteLog("Ошибка AUploadChangesServSupp() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            finally
            {
                db.Close();
                
            }
            return ret;
        }

        public delegate Returns AUploadChangesServSuppDelegate(ReestrChangesServSupp finder);

        public void ACallback(IAsyncResult ar)
        {
            // Because you passed your original delegate in the asyncState parameter
            // of the Begin call, you can get it back here to complete the call.
            //SaveUploadedCounterReadingsDelegate dlgt = (SaveUploadedCounterReadingsDelegate) ar.AsyncState;

            // Complete the call.
            //Returns ret = dlgt.EndInvoke(ar);

            //MonitorLog.WriteLog("Загрузка завершена", MonitorLog.typelog.Info, true);
        }

        public List<ReestrChangesServSupp> GetReestrChangesServSupp(ReestrChangesServSupp finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<ReestrChangesServSupp> list = null;
            if (SrvRun.isBroker)
            {
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.GetReestrChangesServSupp(finder, out ret);
            }
            else
            {
                try
                {
                    DbPack dbpack = new DbPack();
                    list = dbpack.GetReestrChangesServSupp(finder, out ret);
                    dbpack.Close();
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка в функции получения списка выгрузок в банк";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetReestrChangesServSupp()\n " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return list;
        }

        public Returns CheckingReturnOnPrevDay()
        {
            Returns ret;

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.CheckingReturnOnPrevDay();
            }
            else
            {
                DbPack db = new DbPack();
                try
                {
                    ret = db.CheckingReturnOnPrevDay();
                }
                catch (Exception ex)
                {
                    ret = new Returns(false);
                    ret.result = false;
                    ret.text = "Ошибка вызова функции - проверка возможности перейти на предыдущий операционных день";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка CheckingReturnOnPrevDay() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }

            return ret;
        }

        public Returns AReDistributePackLs(Finder finder)
        {
            Returns ret = Utils.InitReturns();
            DbPack db = new DbPack();
            try
            {
                db.ReDistributePackLs(finder);
            }
            catch (Exception ex)
            {
                ret = new Returns(false);
                ret.result = false;
                ret.text = "Ошибка вызова функции - Повторное распределение оплат с отсутствием распределений по услугам";
                if (Constants.Viewerror) ret.text += " : " + ex.Message;
                if (Constants.Debug) MonitorLog.WriteLog("Ошибка ReDistributePackLs() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            finally
            {
                db.Close();

            }
            return ret;
        }

        public Returns ReDistributePackLs(Finder finder)
        {
            Returns ret;

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.ReDistributePackLs(finder);
            }
            else
            {              
                try
                {
                    AReDistributePackLsDelegate dlgt = new AReDistributePackLsDelegate(this.AReDistributePackLs);

                    //object o = new object();
                    AsyncCallback cb = new AsyncCallback(ACallbackReDistribute);
                    IAsyncResult ar = dlgt.BeginInvoke(finder, cb, dlgt);
                    ret = Utils.InitReturns();
                }
                catch (Exception ex)
                {
                    ret = new Returns(false);
                    ret.result = false;
                    ret.text = "Ошибка вызова функции - Повторное распределение оплат с отсутствием распределений по услугам";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка ReDistributePackLs() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
              
            }

            return ret;
        }

        public delegate Returns AReDistributePackLsDelegate(Finder finder);

        public void ACallbackReDistribute(IAsyncResult ar)
        {
            // Because you passed your original delegate in the asyncState parameter
            // of the Begin call, you can get it back here to complete the call.
            //SaveUploadedCounterReadingsDelegate dlgt = (SaveUploadedCounterReadingsDelegate) ar.AsyncState;

            // Complete the call.
            //Returns ret = dlgt.EndInvoke(ar);

            //MonitorLog.WriteLog("Загрузка завершена", MonitorLog.typelog.Info, true);
        }

        public Pack GetOperDaySettings(Finder packFinder, out Returns ret)
        {
            ret = Utils.InitReturns();
            Pack pack = null;

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                pack = cli.GetOperDaySettings(packFinder, out ret);
                return pack;
            }
            else
            {
                DbPack db = new DbPack();

                try
                {
                    pack = db.GetOperDaySettings(packFinder, out ret);
                }
                catch (Exception ex)
                {
                    ret = new Returns(false, "Ошибка вызова функции - Загрузка настроек смены операционного дня" + (Constants.Viewerror ? " : " + ex.Message : ""));
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetOperDaySettings() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    pack = null;
                }

                db.Close();
                return pack;
            }
        }

        public Returns SaveOperDaySettings(Pack finder)
        {
            Returns ret = new Returns();
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.SaveOperDaySettings(finder);
                return ret;
            }
            else
            {
                DbPack db = new DbPack();

                try
                {
                    ret = db.SaveOperDaySettings(finder);
                }
                catch (Exception ex)
                {
                    ret = new Returns(false, "Ошибка вызова функции - Сохранение настроек смены операционного дня" + (Constants.Viewerror ? " : " + ex.Message : ""));
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка SaveOperDaySettings() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }

                db.Close();
                return ret;
            }
        }
    }
}
