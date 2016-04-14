using System;
using System.Collections.Generic;
using System.Data;
using Bars.KP50.DB.Finans.SettingsPack;
using Bars.KP50.DB.Finans.Source;
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

            if (SrvRunProgramRole.IsBroker)
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

            if (SrvRunProgramRole.IsBroker)
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
                        case Pack.PackOperations.Edit: ret = dbpack.SetPackStatusNotClosed(finder); break;
                        case Pack.PackOperations.GetNextNumPackForOverPay: ret = dbpack.GetNextNumPackForOverPay(finder); break;
                        case Pack.PackOperations.Distribute:
                          

                            if (!finder.is_super_pack)
                            {  
                                var finderp = new Pack_ls {nzp_pack = finder.nzp_pack, year_ = finder.year_};
                            Returns retp = dbpack.CheckPAckStatus(finderp);
                            if (!retp.result) return retp;
                                DbCalcPack db1 = new DbCalcPack();
                                db1.PackFonTasks(finder.nzp_pack, finder.year_, finder.nzp_user, CalcFonTask.Types.DistributePack, out ret);  // Отдаем пачку на распределение

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
                                        int i = 0;
                                        DbCalcPack db1 = new DbCalcPack();
                                        foreach (Pack pack in list)
                                        {
                                            var finderp = new Pack_ls { nzp_pack = pack.nzp_pack, year_ = pack.year_ };
                                            Returns retp = dbpack.CheckPAckStatus(finderp);
                                            if (!retp.result) continue;
                                            db1.PackFonTasks(pack.nzp_pack, pack.year_, pack.nzp_user, CalcFonTask.Types.DistributePack, out ret);  // Отдаем пачку на распределение
                                            if (!ret.result) break;
                                            else
                                            {
                                                i++;
                                                pack.nzp_user = finder.nzp_user;
                                                pack.flag = Pack.Statuses.WaitingForDistribution.GetHashCode();
                                                db1.UpdatePackStatus(pack);
                                            }
                                        }

                                        if (ret.result)
                                        {
                                            ret.text = i.ToString() +" из " + list.Count + " пачек поставлено в очередь";
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
                                var finderp2 = new Pack_ls { nzp_pack = finder.nzp_pack, year_ = finder.year_ };
                                Returns retp2 = dbpack.CheckPAckStatus(finderp2);
                                if (!retp2.result) return retp2;
                                DbCalcPack db2 = new DbCalcPack();
                                db2.PackFonTasks(finder.nzp_pack, finder.year_, finder.nzp_user, CalcFonTask.Types.CancelPackDistribution, out ret);  // Отдаем пачку на распределение

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
                                        int i = 0;
                                        DbCalcPack db2 = new DbCalcPack();
                                        foreach (Pack pack in list)
                                        {
                                            var finderp2 = new Pack_ls { nzp_pack = pack.nzp_pack, year_ = pack.year_ };
                                            Returns retp2 = dbpack.CheckPAckStatus(finderp2);
                                            if (!retp2.result) continue;
                                 
                                            db2.PackFonTasks(pack.nzp_pack, pack.year_, finder.nzp_user, CalcFonTask.Types.CancelPackDistribution, out ret);  // Отдаем пачку на отмену распределения
                                            if (!ret.result) break;
                                            else
                                            {
                                                i++;
                                                pack.nzp_user = finder.nzp_user;
                                                pack.flag = Pack.Statuses.WaitingForCancellationOfDistribution.GetHashCode();
                                                db2.UpdatePackStatus(pack);
                                            }
                                        }

                                        if (ret.result)
                                        {
                                            ret.text = i.ToString() + " из " + list.Count + " пачек поставлено в очередь ";
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
                                var finderp1 = new Pack_ls { nzp_pack = finder.nzp_pack, year_ = finder.year_ };
                                Returns retp1 = dbpack.CheckPAckStatus(finderp1);
                                if (!retp1.result) return retp1;

                                DbCalcPack db2 = new DbCalcPack();
                                db2.PackFonTasks(finder.nzp_pack, finder.year_, finder.nzp_user, CalcFonTask.Types.CancelDistributionAndDeletePack, out ret);  // Отдаем пачку на распределение

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
                                        int i = 0;
                                        foreach (Pack pack in list)
                                        {
                                            var finderp1 = new Pack_ls { nzp_pack = finder.nzp_pack, year_ = finder.year_ };
                                            Returns retp1 = dbpack.CheckPAckStatus(finderp1);
                                            if (!retp1.result) continue;

                                            db2.PackFonTasks(pack.nzp_pack, finder.year_, finder.nzp_user, CalcFonTask.Types.CancelDistributionAndDeletePack, out ret);  // Отдаем пачку на отмену распределения
                                            if (!ret.result) break;
                                            else
                                            {
                                                i++;
                                                pack.nzp_user = finder.nzp_user;
                                                pack.flag = Pack.Statuses.WaitingForCancellationOfDistribution.GetHashCode();
                                                db2.UpdatePackStatus(pack);
                                            }
                                        }

                                        if (ret.result)
                                        {
                                            ret.text = i.ToString() + " из " + list.Count +
                                                       " пачек поставлены в очередь";
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

        public Returns ChangeCasePack(PackFinder finder, List<Pack_ls> list)
        {
            Returns ret = Utils.InitReturns();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.ChangeCasePack(finder, list);
            }
            else
            {
                try
                {
                    DbPack dbpack = new DbPack();
                    ret = dbpack.ChangeCasePack(finder, list);
                    dbpack.Close();
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции изменения состояния оплат пачки";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка ChangeCasePack()\n " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return ret;
        }

        public Pack_ls OperateWithPackLsAndGetIt(Pack_ls finder, Pack_ls.OperationsWithGetting oper, out Returns ret)
        {
            ret = Utils.InitReturns();

            if (SrvRunProgramRole.IsBroker)
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
                        case Pack_ls.OperationsWithGetting.AutoSavePackLs: pack = dbpack.AutoSavePackLs(finder, out ret); break;
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

            if (SrvRunProgramRole.IsBroker)
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
                            Returns retp = dbpack.CheckPAckStatus(finder);
                            if (!retp.result) return retp;
                            ret = dbpack.DistributeOrCancelPack(finder, oper, true); 
                            break;
                        case Pack_ls.OperationsWithoutGetting.BlockPackLs: ret = dbpack.BlockPackLs(finder); break;
                        case Pack_ls.OperationsWithoutGetting.UnBlockPackLs: ret = dbpack.DeleteBlockFromPackLs(finder); break;
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

        public Returns ChangeChoosenPlsInCase(Finder finder)
        {
            Returns ret = Utils.InitReturns();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.ChangeChoosenPlsInCase(finder);
            }
            else
            {
                try
                {
                    DbPack dbpack = new DbPack();
                    ret = dbpack.ChangeChoosenPlsInCase(finder);
                    dbpack.Close();
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка ChangeChoosenPlsInCase()\n " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return ret;
        }

        public Returns OperateWithListPackLs(List<Pack_ls> finder, enSrvOper oper)
        {
            Returns ret = Utils.InitReturns();

            if (SrvRunProgramRole.IsBroker)
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
                        case enSrvOper.srvReplaceToCurFinYear:
                            {
                                oper_text = "Перенос оплат в текущий финансовый год";
                                ret = dbpack.ReplacePackLs(finder);
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

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.UploadPackFromDBF(nzp_user, fileName);
            }
            else
            {
                try
                {
                    AddedPacksInfo insertedPAckInfo;
                    DbPack dbpack = new DbPack();
                    ret = dbpack.UploadPackFromDBF(nzp_user, fileName, out insertedPAckInfo);
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

            if (SrvRunProgramRole.IsBroker)
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
                    AddedPacksInfo packsInfo= new AddedPacksInfo();
                    ret = dbpack.UploadPackFromWeb(nzpPack, nzp_user, ref packsInfo);
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

            if (SrvRunProgramRole.IsBroker)
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
            if (SrvRunProgramRole.IsBroker)
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


        public string LoadUniversalFormat(string body, string filename)
        {
            string result = "";

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                result = cli.LoadUniversalFormat(body, filename);
            }
            else
            {
                try
                {
                    DbPack dbpack = new DbPack();
                    DataTable res = dbpack.LoadUniversalFormat(body, filename);
                    if (res != null)
                    {
                        foreach (DataRow dr in res.Rows)
                            result += dr["number_string"].ToString().Trim() + "`" + dr["mes"].ToString().Trim() +
                                Environment.NewLine;
                    }

                    dbpack.Close();
                }
                catch (Exception ex)
                {

                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LoadUniversalFormat()\n " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return result;
        }

        public List<Bank> LoadListBanks(Bank finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Bank> list = null;
            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.LoadListBanks(finder, out ret);
            }
            else
            {
                try
                {
                    DbPack dbpack = new DbPack();
                    list = dbpack.LoadListBanks(finder, out ret);
                    dbpack.Close();
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки банков";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LoadListBanks()\n " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return list;
        }

        public List<Ulica> LoadUlica(Ulica finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Ulica> list = null;
            if (SrvRunProgramRole.IsBroker)
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
            if (SrvRunProgramRole.IsBroker)
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
            if (SrvRunProgramRole.IsBroker)
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
            if (SrvRunProgramRole.IsBroker)
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
            if (SrvRunProgramRole.IsBroker)
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
            if (SrvRunProgramRole.IsBroker)
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
            if (SrvRunProgramRole.IsBroker)
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
            if (SrvRunProgramRole.IsBroker)
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
                        case enSrvOper.srvGetCase: list = dbpack.GetPackLsInCase(finder, out ret); break;//dbpack.GetCasePack_ls(finder, out ret); break;
                        case enSrvOper.srvFindCase: ret = dbpack.FindPackLsInCase(finder);
                            if (ret.result) list = dbpack.GetPackLsInCase(finder, out ret);
                            else list = null;
                            break;
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

        public Returns PackLsInCaseChangeMark(Finder finder, List<Pack_ls> listChecked, List<Pack_ls> listUnchecked)
        {
            Returns ret = Utils.InitReturns();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.PackLsInCaseChangeMark(finder, listChecked, listUnchecked);
            }
            else
            {
                try
                {
                    DbPack dbpack = new DbPack();
                    ret = dbpack.PackLsInCaseChangeMark(finder, listChecked, listUnchecked);
                    dbpack.Close();
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции PackLsInCaseChangeMark";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка PackLsInCaseChangeMark()\n " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return ret;
        }

        public List<Pack> GetPack(PackFinder finder, enSrvOper oper, out Returns ret)
        {
            ret = Utils.InitReturns();
            if (SrvRunProgramRole.IsBroker)
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
            if (SrvRunProgramRole.IsBroker)
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

            if (SrvRunProgramRole.IsBroker)
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

        public DateTime GetOperDay(out Returns ret)
        {
            ret = Utils.InitReturns();
            DateTime dt = DateTime.MinValue;
            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                dt = cli.GetOperDay(out ret);
            }
            else
            {
                try
                {
                    using (var dbpack = new DbPack())
                    {
                        dt = dbpack.GetOperDay(out ret);
                    }
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка получения операционного дня";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetOperDay()\n " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return dt;
        }


        /// <summary>
        /// Откат платежей по выбранному пользователем списку ЛС
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        public Returns CancelPlat(Finder finder)
        {
            Returns ret = Utils.InitReturns();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.CancelPlat(finder);
            }
            else
            {
                try
                {
                    DbPack dbpack = new DbPack();
                    ret = dbpack.CancelPlat(finder);
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


        ///// <summary>
        ///// Откат платежей по списку ЛС
        ///// </summary>
        ///// <param name="finder"></param>
        ///// <param name="list">Список платежей</param>
        ///// <returns></returns>
        //public Returns CancelPlat2(Finder finder, List<Pack_ls> list)
        //{
        //    Returns ret = Utils.InitReturns();

        //    if (SrvRunProgramRole.IsBroker)
        //    {
        //        //надо продублировать вызов к внутреннему хосту
        //        cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
        //        ret = cli.CancelPlat2(finder, list);
        //    }
        //    else
        //    {
        //        try
        //        {
        //            DbPack dbpack = new DbPack();
        //            ret = dbpack.CancelPlat2(finder, list);
        //            dbpack.Close();
        //        }
        //        catch (Exception ex)
        //        {
        //            ret.result = false;
        //            ret.text = "Ошибка вызова функции отмены платежей";
        //            if (Constants.Viewerror) ret.text += " : " + ex.Message;
        //            if (Constants.Debug) MonitorLog.WriteLog("Ошибка CancelPlat()\n " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
        //        }
        //    }
        //    return ret;
        //}

        public List<Pack_log> GetPackLog(Pack_log finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Pack_log> list = null;
            if (SrvRunProgramRole.IsBroker)
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
            if (SrvRunProgramRole.IsBroker)
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

        public List<BankRequisites> NewFdGetBankRequisites(BankRequisites finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<BankRequisites> list2 = null;
            if (SrvRunProgramRole.IsBroker)
            {
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                list2 = cli.NewFdGetBankRequisites(finder, out ret);
            }
            else
            {
                try
                {
                    DbPack dbpack = new DbPack();
                    list2 = dbpack.NewFdGetBankRequisites(finder, out ret);
                    dbpack.Close();
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка в функции получения списка банковских реквизитов";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug)
                        MonitorLog.WriteLog("Ошибка NewFdGetBankRequisites()\n " + ex.Message, MonitorLog.typelog.Error,
                            2, 100, true);
                }
            }
            return list2;
        }

        public List<BankRequisites> GetRsForERCDogovor(BankRequisites finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<BankRequisites> list2 = null;
            if (SrvRunProgramRole.IsBroker)
            {
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                list2 = cli.GetRsForERCDogovor(finder, out ret);
            }
            else
            {
                try
                {
                    DbPack dbpack = new DbPack();
                    list2 = dbpack.GetRsForERCDogovor(finder, out ret);
                    dbpack.Close();
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка в функции получения списка банковских реквизитов";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug)
                        MonitorLog.WriteLog("Ошибка GetRsForERCDogovor()\n " + ex.Message, MonitorLog.typelog.Error,
                            2, 100, true);
                }
            }
            return list2;
        }

        public List<DogovorRequisites> GetDogovorRequisites(DogovorRequisites finder, enSrvOper oper, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<DogovorRequisites> list = null;
            if (SrvRunProgramRole.IsBroker)
            {
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.GetDogovorRequisites(finder, oper, out ret);
            }
            else
            {
                try
                {
                    DbPack dbpack = new DbPack();
                    switch (oper)
                    {
                        case enSrvOper.GetDogovorList: list = dbpack.GetDogovorList(finder, out ret); break;
                        case enSrvOper.GetOsnovList: list = dbpack.GetOsnovList(finder, out ret); break;
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

        public List<DogovorRequisites> GetDogovorRequisitesSupp(DogovorRequisites finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<DogovorRequisites> list = null;
            if (SrvRunProgramRole.IsBroker)
            {
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.GetDogovorRequisitesSupp(finder, out ret);
            }
            else
            {
                try
                {
                    DbPack dbpack = new DbPack();
                    list = dbpack.GetDogovorListSupp(finder, out ret);
                    dbpack.Close();

                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка в функции получения списка реквизитов договоров";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetDogovorRequisitesSupp()\n " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return list;
        }

        public List<DogovorRequisites> GetDogovorERCList(DogovorRequisites finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<DogovorRequisites> list = null;
            if (SrvRunProgramRole.IsBroker)
            {
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.GetDogovorERCList(finder, out ret);
            }
            else
            {
                try
                {
                    DbPack dbpack = new DbPack();
                    list = dbpack.GetDogovorERCList(finder, out ret);
                    dbpack.Close();

                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка в функции получения списка договоров";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetDogovorERCList()\n " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return list;
        }

        public List<BankRequisites> GetSourceBankList(BankRequisites finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<BankRequisites> list = null;
            if (SrvRunProgramRole.IsBroker)
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
                    ret.text = "Ошибка в функции получения списка реквизитов договоров";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetSourceBankList()\n " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return list;
        }

        public List<ContractRequisites> GetContractRequisites(ContractRequisites finder, enSrvOper oper, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<ContractRequisites> list = null;
            if (SrvRunProgramRole.IsBroker)
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
            if (SrvRunProgramRole.IsBroker)
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
                        case enSrvOper.DelBankRequisitesNewFd: res = dbpack.DelBankRequisitesNewFd(finder, out ret); break;
                        case enSrvOper.UpdateBankRequisites: res = dbpack.UpdateBankRequisites(finder, out ret); break;
                        case enSrvOper.NewFdAddBankRequisites: res = dbpack.NewFdAddBankRequisites(finder, out ret); break;
                        case enSrvOper.NewFdUpdateBankRequisites: res = dbpack.NewFdUpdateBankRequisites(finder, out ret); break;
                        case enSrvOper.AddBankRequisitesContr: res = dbpack.AddBankRequisitesContr(finder, out ret); break;
                        case enSrvOper.UpdateBankRequisitesContr: res = dbpack.UpdateBankRequisitesContr(finder, out ret); break;
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
            if (SrvRunProgramRole.IsBroker)
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

        public bool ChangeDogovorRequisitesSupp(DogovorRequisites finder, enSrvOper oper, out Returns ret)
        {
            ret = Utils.InitReturns();
            bool res = false;
            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.ChangeDogovorRequisitesSupp(finder, oper, out ret);
            }
            else
            {
                try
                {
                    DbPack dbpack = new DbPack();
                    switch (oper)
                    {
                        case enSrvOper.AddDogovorRequisites: res = dbpack.AddDogovorRequisitesSupp(finder, out ret); break;
                        case enSrvOper.DelDogovorRequisites: res = dbpack.DelDogovorRequisites(finder, out ret); break;
                        case enSrvOper.UpdateDogovorRequisites: res = dbpack.UpdateDogovorRequisitesSupp(finder, out ret); break;
                        case enSrvOper.AddERCDogovorRequisites: res = dbpack.AddERCDogovorRequisites(finder, out ret); break;
                        case enSrvOper.DelERCDogovorRequisites: res = dbpack.DelDogovorERCRequisites(finder, out ret); break;
                        case enSrvOper.UpdateERCDogovorRequisites: res = dbpack.UpdateERCDogovorRequisites(finder, out ret); break;
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
            if (SrvRunProgramRole.IsBroker)
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
            if (SrvRunProgramRole.IsBroker)
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
                if (SrvRunProgramRole.IsBroker)
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
                if (SrvRunProgramRole.IsBroker)
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
                if (SrvRunProgramRole.IsBroker)
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
                if (SrvRunProgramRole.IsBroker)
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

        public Returns MakeContDistribPayments(Payments finder)
        {
            Returns ret = Utils.InitReturns();
            
            try
            {
                var dlgt = new AMakeContDistribPaymentsDelegate(this.AMakeContDistribPayments);

                //object o = new object();
                var cb = new AsyncCallback(ACallbackCp);
                IAsyncResult ar = dlgt.BeginInvoke(finder, cb, dlgt);

                ret = new Returns(true, "Задание отправлено на выполнение.", -1);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка вызова Отчета Контроль распределения оплат";
                //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                if (Constants.Debug) MonitorLog.WriteLog("Ошибка UploadEFS" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            //db.Close();

            return ret;
        }

        public Returns AMakeContDistribPayments(Payments finder)
        {
            Returns ret = Utils.InitReturns();
            DbPack db = new DbPack();
            try
            {
                db.GenConDistrPaymentsPDF(finder, null);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка вызова отчета Контроль распределения";
                //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                if (Constants.Debug) MonitorLog.WriteLog("Ошибка AMakeContDistribPayments" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            finally { db.Close(); }
            return ret;

        }

        public delegate Returns AMakeContDistribPaymentsDelegate(Payments finder);

        public void ACallbackCp(IAsyncResult ar)
        {
            // Because you passed your original delegate in the asyncState parameter
            // of the Begin call, you can get it back here to complete the call.
            //SaveUploadedCounterReadingsDelegate dlgt = (SaveUploadedCounterReadingsDelegate) ar.AsyncState;

            // Complete the call.
            //Returns ret = dlgt.EndInvoke(ar);

            //MonitorLog.WriteLog("Загрузка завершена", MonitorLog.typelog.Info, true);
        }

        public ReturnsType GenContDistribPaymentsPDF(Payments finder)
        {
            try
            {
                if (SrvRunProgramRole.IsBroker)
                {
                    // продублировать вызов к внутреннему хосту
                    
                    
                    cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                    return cli.GenContDistribPaymentsPDF(finder);
                }
                else
                {
                    DbPack db = new DbPack();
                    Returns ret = db.GenConDistrPaymentsPDF(finder, null);
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

            if (SrvRunProgramRole.IsBroker)
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
                            DbChargeTemp dbcharge = new DbChargeTemp();
                            sum = dbcharge.GetSumKOplate(finder, out ret);
                            dbcharge.Close();
                            return sum;

                        case GetLsSumOperations.GetCurrentDolg:
                            DbChargeTemp dbcharge2 = new DbChargeTemp();
                            sum = dbcharge2.GetSumKOplate(finder, out ret);
                            dbcharge2.Close();
                            if (!ret.result) return 0;
                            return sum;

                        case GetLsSumOperations.GetSumOutSaldo:
                            DbPack dbcharge3 = new DbPack();
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
            if (SrvRunProgramRole.IsBroker)
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

            if (SrvRunProgramRole.IsBroker)
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

        public Returns DeleteManualDistrib(Pack_ls finder)
        {
            Returns ret = Utils.InitReturns();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.DeleteManualDistrib(finder);
            }
            else
            {
                try
                {
                    DbPack dbpack = new DbPack();
                    ret = dbpack.DeleteManualDistrib(finder);
                    dbpack.Close();
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции сохранения";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка DeleteManualDistrib()\n " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return ret;
        }

        public Returns GetPrincipForManualDistrib(List<ChargeForDistribSum> listfinder, out List<ChargeForDistribSum> res)
        {
            Returns ret = Utils.InitReturns();
            res = new List<ChargeForDistribSum>();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetPrincipForManualDistrib(listfinder, out res);
            }
            else
            {
                try
                {
                    DbPack dbpack = new DbPack();
                    ret = dbpack.GetPrincipForManualDistrib(listfinder, out res);
                    dbpack.Close();
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции сохранения";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetPrincipForManualDistrib()\n " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return ret;
        }


        public Returns CreatePackOverPayment(Pack finder)
        {
            Returns ret = Utils.InitReturns();

            if (SrvRunProgramRole.IsBroker)
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
        public Returns СreateUploading(FilterForBC finder)
        //----------------------------------------------------------------------
        {
            Returns ret;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                var cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.СreateUploading(finder);
            }
            else
            {
                try
                {
                    using (var db = new DbPack())
                    {
                        ret = db.СreateUploading(finder);
                    }
                }
                catch (Exception ex)
                {
                    ret = new Returns(false) {result = false, text = "Ошибка вызова функции добавления новой выгрузки"};
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка СreateUploading \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }

            return ret;
        }

        public Returns ChangeOperDay(OperDayFinder finder, out string date_oper, out string filename, out RecordMonth calcmonth)
        {
            Returns ret = Utils.InitReturns();

            date_oper = "";
            filename = "";
            calcmonth = Points.CalcMonth;

            if (SrvRunProgramRole.IsBroker)
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
        public Returns SaveCheckSend(int nzpUser, List<FilesUploadingBC> files)
        //----------------------------------------------------------------------
        {
            Returns ret;
            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                var cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.SaveCheckSend(nzpUser, files);
            }
            else
            {
                try
                {
                    using (var db = new DbPack())
                    {
                        ret = db.SaveCheckSend(nzpUser, files);
                    }
                }
                catch (Exception ex)
                {
                    ret = new Returns
                    {
                        result = false,
                        text = "Ошибка вызова функции - сохранение платежей обработаны банком"
                    };
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка SaveCheckSend \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }

            return ret;
        }

        //----------------------------------------------------------------------
        public List<InfoPayerBankClient> GetInfoPayers(FilterForBC finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<InfoPayerBankClient> res;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                var cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.GetInfoPayers(finder, out ret);
            }
            else
            {
                var db = new DbPack();
                try
                {
                    res = db.GetInfoPayers(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetInfoPayers() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = null;
                }
                db.Close();
            }
            return res;
        }

        //----------------------------------------------------------------------
        public List<InfoPayerBankClient> GetTransfersPayer(FilterForBC finder, out Returns ret)
            //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<InfoPayerBankClient> res;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                var cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.GetTransfersPayer(finder, out ret);
            }
            else
            {
                var db = new DbPack();
                try
                {
                    res = db.GetTransfersPayer(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetTransfersPayer() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = null;
                }
                db.Close();
            }
            return res;
        }

        //----------------------------------------------------------------------
        public List<InfoPayerBankClient> GetDogovorsWithTransfers(FilterForBC finder, out Returns ret)
            //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<InfoPayerBankClient> res;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                var cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.GetDogovorsWithTransfers(finder, out ret);
            }
            else
            {
                var db = new DbPack();
                try
                {
                    res = db.GetDogovorsWithTransfers(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetDogovorsWithTransfers() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = null;
                }
                db.Close();
            }
            return res;
        }

        //----------------------------------------------------------------------
        public List<FormatBC> GetFormats(int nzpUser, out Returns ret, List<int> formats)
        //----------------------------------------------------------------------
        {
            var list = new List<FormatBC>();
            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                var cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.GetFormats(nzpUser, out ret);
            }
            else
            {
                try
                {
                    using (var dbpack = new DbPack())
                    {
                        list = dbpack.GetFormats(nzpUser, out ret, formats);
                    }
                }
                catch (Exception ex)
                {
                    ret = new Returns(false) {result = false, text = "Ошибка вызова функции - загрузка списка форматов"};
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetFormats \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return list;
        }

        //----------------------------------------------------------------------
        public FormatBC GetFormat(int nzpUser, int idFormat, out Returns ret)
        //----------------------------------------------------------------------
        {
            var format = new FormatBC();
            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                var cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                format = cli.GetFormat(nzpUser, idFormat, out ret);
            }
            else
            {
                try
                {
                    using (var dbpack = new DbPack())
                    {
                        format = dbpack.GetFormat(nzpUser, idFormat, out ret);
                    }
                }
                catch (Exception ex)
                {
                    ret = new Returns(false) { result = false, text = "Ошибка вызова функции - загрузка формата" };
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetFormat \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return format;
        }

        //----------------------------------------------------------------------
        public int AddFormat(int nzpUser, string nameFormat, out Returns ret)
        //----------------------------------------------------------------------
        {
            int idFormat = 0;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                var cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                idFormat = cli.AddFormat(nzpUser, nameFormat, out ret);
            }
            else
            {
                var db = new DbPack();
                try
                {
                    idFormat = db.AddFormat(nzpUser, nameFormat, out ret);
                }
                catch (Exception ex)
                {
                    ret = new Returns(false)
                    {
                        result = false,
                        text = "Ошибка вызова функции - добавления нового формата"
                    };
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка AddFormat() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }

            return idFormat;
        }

        //----------------------------------------------------------------------
        public Returns SaveFormat(int nzpUser, FormatBC typ)
        //----------------------------------------------------------------------
        {
            Returns ret;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                var cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.SaveFormat(nzpUser, typ);
            }
            else
            {
                try
                {
                    using (var db = new DbPack())
                    {
                        ret = db.SaveFormat(nzpUser, typ);
                    }
                }
                catch (Exception ex)
                {
                    ret = new Returns(false)
                    {
                        result = false,
                        text = "Ошибка вызова функции - сохранения изменений формата"
                    };
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка SaveFormat() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }

            return ret;
        }

        //----------------------------------------------------------------------
        public Returns DeleteFormat(int nzpUser, int idFormat)
        //----------------------------------------------------------------------
        {
            Returns ret;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                var cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.DeleteFormat(nzpUser, idFormat);
            }
            else
            {
                var db = new DbPack();
                try
                {
                    ret = db.DeleteFormat(nzpUser, idFormat);
                }
                catch (Exception ex)
                {
                    ret = new Returns(false) {result = false, text = "Ошибка вызова функции - удаление формата"};
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка DeleteFormat() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }

            return ret;
        }

        //----------------------------------------------------------------------
        public List<TagBC> GetTags(int nzpUser, int indexFormat, out Returns ret)
        //----------------------------------------------------------------------
        {
            List<TagBC> list = null;
            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                var cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.GetTags(nzpUser, indexFormat, out ret);
            }
            else
            {
                try
                {
                    using (var dbpack = new DbPack())
                    {
                        list = dbpack.GetTags(nzpUser, out ret, indexFormat);
                    }
                }
                catch (Exception ex)
                {
                    ret = new Returns(false) {result = false, text = "Ошибка вызова функции - загрузка тегов формата"};
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetTags \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return list;
        }

        //----------------------------------------------------------------------
        public TagBC GetTag(int nzpUser, int idTag, out Returns ret)
        //----------------------------------------------------------------------
        {
            TagBC tag = null;
            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                var cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                tag = cli.GetTag(nzpUser, idTag, out ret);
            }
            else
            {
                try
                {
                    using (var dbpack = new DbPack())
                    {
                        tag = dbpack.GetTag(nzpUser, idTag, out ret);
                    }
                }
                catch (Exception ex)
                {
                    ret = new Returns(false) {result = false, text = "Ошибка вызова функции - загрузка тега"};
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetTag \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return tag;
        }

        //----------------------------------------------------------------------
        public int AddTag(int nzpUser, TagBC tag, out Returns ret)
        //----------------------------------------------------------------------
        {
            int idTag = 0;
            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                var cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                idTag = cli.AddTag(nzpUser, tag, out ret);
            }
            else
            {
                try
                {
                    using (var db = new DbPack())
                    {
                        idTag = db.AddTag(nzpUser, tag, out ret);
                    }
                }
                catch (Exception ex)
                {
                    ret = new Returns(false) {result = false, text = "Ошибка вызова функции - добавление тега"};
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка AddTag() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }

            return idTag;
        }

        //----------------------------------------------------------------------
        public Returns DeleteTag(int nzpUser, int idTag)
        //----------------------------------------------------------------------
        {
            Returns ret;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                var cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.DeleteTag(nzpUser, idTag);
            }
            else
            {
                try
                {
                    using (var db = new DbPack())
                    {
                        ret = db.DeleteTag(nzpUser, idTag);
                    }
                }
                catch (Exception ex)
                {
                    ret = new Returns(false) {result = false, text = "Ошибка вызова функции - удаления тега"};
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка DeleteTag() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }

            return ret;
        }

        //----------------------------------------------------------------------
        public Returns SaveTag(int nzpUser, TagBC tag)
        //----------------------------------------------------------------------
        {
            Returns ret;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                var cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.SaveTag(nzpUser, tag);
            }
            else
            {
                try
                {
                    using (var db = new DbPack())
                    {
                        ret = db.SaveTag(nzpUser, tag);
                    }
                }
                catch (Exception ex)
                {
                    ret = new Returns(false) {result = false, text = "Ошибка вызова функции - сохраниения тега"};
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка SaveTag() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }

            return ret;
        }

        //----------------------------------------------------------------------
        public Returns UpTag(int nzpUser, int idTag)
        //----------------------------------------------------------------------
        {
            Returns ret;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                var cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.UpTag(nzpUser, idTag);
            }
            else
            {
                try
                {
                    using (var db = new DbPack())
                    {
                        ret = db.UpTag(nzpUser, idTag);
                    }
                }
                catch (Exception ex)
                {
                    ret = new Returns(false) {result = false, text = "Ошибка вызова функции - изменения тега"};
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка UpTag() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }

            return ret;
        }

        //----------------------------------------------------------------------
        public Returns DownTag(int nzpUser, int idTag)
        //----------------------------------------------------------------------
        {
            Returns ret;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                var cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.DownTag(nzpUser, idTag);
            }
            else
            {
                try
                {
                    using (var db = new DbPack())
                    {
                        ret = db.DownTag(nzpUser, idTag);
                    }
                }
                catch (Exception ex)
                {
                    ret = new Returns(false) {result = false, text = "Ошибка вызова функции - добавление тега"};
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка DownTag() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }

            return ret;
        }

        //----------------------------------------------------------------------
        public List<ValueTagBC> GetTagValues(int nzpUser, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<ValueTagBC> list = null;
            if (SrvRunProgramRole.IsBroker)
            {
                var cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.GetTagValues(nzpUser, out ret);
            }
            else
            {
                try
                {
                    using (var dbpack = new DbPack())
                    {
                        list = dbpack.GetTagValues(nzpUser, out ret);
                    }
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка в функции получения списка значений тега";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetTagValues()\n " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return list;
        }

        //----------------------------------------------------------------------
        public List<TypeTagBC> GetTagTypes(int nzpUser, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<TypeTagBC> list = null;
            if (SrvRunProgramRole.IsBroker)
            {
                var cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.GetTagTypes(nzpUser, out ret);
            }
            else
            {
                try
                {
                    using (var dbpack = new DbPack())
                    {
                        list = dbpack.GetTagTypes(nzpUser, out ret);
                    }
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка в функции получения списка типов тега";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetTagTypes()\n " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return list;
        }

        public List<FilesUploadingOnWebBC> GetFilesUploading(int nzpUser, int idReestr, int skip, int rows, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            var list = new List<FilesUploadingOnWebBC>();

            if (SrvRunProgramRole.IsBroker)
            {
                var cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.GetFilesUploading(nzpUser, idReestr, skip, rows, out ret);
            }
            else
            {
                try
                {
                    using (var db = new DbPack())
                    {
                        list = db.GetFilesUploading(nzpUser, idReestr, skip, rows, out ret);
                    }
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetFilesUploading() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return list;
        }

        /// <summary>Возвращает список выгрузок (Банк-клиент)</summary>
        /// <param name="nzpUser">Идентификатор пользователя</param>
        /// <param name="skip">Кол-во строк пропустить</param>
        /// <param name="rows">Кол-во строк на вывод</param>
        /// <param name="ret">Результат функции</param>
        public List<UploadingOnWebBC> GetListUploading(int nzpUser, int skip, int rows, out Returns ret)
            //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            var list = new List<UploadingOnWebBC>();

            if (SrvRunProgramRole.IsBroker)
            {
                var cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.GetListUploading(nzpUser, skip, rows, out ret);
            }
            else
            {
                try
                {
                    using (var db = new DbPack())
                    {
                        list = db.GetUploading(nzpUser, skip, rows, 0, out ret);
                    }
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetListUploading() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return list;
        }


        /// <summary>Возващает данные по выгрузке (Банк-клиент)</summary>
        /// <param name="nzpUser">Идентификатор пользователя</param>
        /// <param name="idReestr">Идентификатор реестра</param>
        /// <param name="ret">Результат выполнения функции</param>
        public UploadingOnWebBC GetUploading(int nzpUser, int idReestr, out Returns ret)
            //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            var uploading = new UploadingOnWebBC();

            if (SrvRunProgramRole.IsBroker)
            {
                var cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                uploading = cli.GetUploading(nzpUser, idReestr, out ret);
            }
            else
            {
                try
                {
                    using (var db = new DbPack())
                    {
                        List<UploadingOnWebBC> list = db.GetUploading(nzpUser, 0, 0, idReestr, out ret);
                        if (list.Count > 0) uploading = list[0];
                    }
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetUploading() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return uploading;
        }

        public Returns FormPacksSbPay(EFSReestr finder, PackFinder packfinder)
        {
            Returns ret;

            if (SrvRunProgramRole.IsBroker)
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

            if (SrvRunProgramRole.IsBroker)
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
            if (SrvRunProgramRole.IsBroker)
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

            if (SrvRunProgramRole.IsBroker)
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

            if (SrvRunProgramRole.IsBroker)
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

            if (SrvRunProgramRole.IsBroker)
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
            if (SrvRunProgramRole.IsBroker)
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

        public List<string> GetRS(Pack_ls finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<string> list = null;
            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.GetRS(finder, out ret);
            }
            else
            {
                try
                {
                    DbPack dbpack = new DbPack();
                    list = dbpack.GetRS(finder, out ret);
                    dbpack.Close();
                }
                catch (Exception ex)
                {
                    ret = new Returns(false);
                    ret.result = false;
                    ret.text = "Ошибка вызова функции - загрузка списка расчетный счетов";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetRS \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return list;
        }

        public List<Pack_ls> GetKodSumList(Pack_ls finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<Pack_ls> list = new List<Pack_ls>();
            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.GetKodSumList(finder, out ret);
            }
            else
            {
                try
                {
                    DbPack dbpack = new DbPack();
                    list = dbpack.GetKodSumList(finder, out ret);
                    dbpack.Close();
                }
                catch (Exception ex)
                {
                    ret = new Returns(false);
                    ret.result = false;
                    ret.text = "Ошибка вызова функции - загрузка списка кодов квитанций";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetKodSumList \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return list;
        }

        public List<Pack> GetFilesName(Pack finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<Pack> list = new List<Pack>();
            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.GetFilesName(finder,out ret);
            }
            else
            {
                try
                {
                    DbPack dbpack = new DbPack();
                    list = dbpack.GetFilesName(finder, out ret);
                    dbpack.Close();
                }
                catch (Exception ex)
                {
                    ret = new Returns(false);
                    ret.result = false;
                    ret.text = "Ошибка вызова функции - загрузка списка кодов квитанций";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetFilesName \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return list;
        }

        public FilesImported FastCheck(FilesImported finder, out Returns ret)
        {
            FilesImported result = new FilesImported();
            ret = Utils.InitReturns();
            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                result = cli.FastCheck(finder, out ret);
            }
            else
            {
                try
                {
                    if (finder.upload_format == (int)FilesImported.UploadFormat.MariyEl)
                    {
                        using (var db = new DbPaymentsFromBankMariyEl())
                        {
                            result = db.FastCheck(finder, out ret);
                        }
                    }
                    else
                    if (finder.upload_format == (int)FilesImported.UploadFormat.BaikalskVstkb || finder.upload_format == (int)FilesImported.UploadFormat.BaikalskSber)
                    {
                        using (var db = new DbPaymentsFromBankBaikalVstkb())
                        {
                            result = db.FastCheck(finder, out ret);
                        }
                    } else
                    if (finder.upload_format == (int)FilesImported.UploadFormat.BaikalskSocProtectL)
                    {
                        using (var db = new DbPaymentsFromBankBaikalVstkb())
                        {
                            result = db.FastCheck(finder, out ret);
                        }
                    } else
                    if (finder.upload_format == (int)FilesImported.UploadFormat.BaikalskSocProtectS)
                    {
                        using (var db = new DbPaymentsFromBankBaikalVstkb())
                        {
                            result = db.FastCheck(finder, out ret);
                        }
                    } else
                    if (finder.upload_format == (int)FilesImported.UploadFormat.TagilSber)
                    {
                        using (var db = new DbPaymentsFromBankTagilSber())
                        {
                            result = db.FastCheck(finder, out ret);
                        }
                    }
                    else
                    {
                        using (var db = new DbPaymentsFromBank())
                        {
                            result = db.FastCheck(finder, out ret);
                        }
                    }
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции проверки реестра для загрузки в БЦ";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug)
                        MonitorLog.WriteLog("Ошибка FastCheck" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100,
                            true);
                }
            }
            return result;
        }

        public Returns UploadKvitReestr(FilesImported finder)
        {

            Returns ret = Utils.InitReturns();
            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.UploadKvitReestr(finder);
            }
            else
            {
                var db = new StartLoadPackFromBank();
                try
                {
                    ret = db.UploadReestrInFon(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции проверки реестра для загрузки в БЦ";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug)
                        MonitorLog.WriteLog("Ошибка FastCheck" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100,
                            true);
                }

            }
            return ret;
        }

        public SupplierInfo GetSupplierInfo(SupplierInfo finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            SupplierInfo si = new SupplierInfo();
            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                si = cli.GetSupplierInfo(finder, out ret);
            }
            else
            {
                try
                {
                    OperationsWithContracts operContr = new OperationsWithContracts();
                    si = operContr.GetSupplierInfo(finder, out ret);
                    operContr.Close();
                }
                catch (Exception ex)
                {
                    ret = new Returns(false);
                    ret.result = false;
                    ret.text = "Ошибка вызова функции - загрузка списка кодов квитанций";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка SupplierInfo \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return si;
        }

        public Returns UpdateSupplierScope(SupplierInfo finder)
        {
            Returns ret;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.UpdateSupplierScope(finder);
            }
            else
            {
                try
                {
                    OperationsWithContracts operContr = new OperationsWithContracts();
                    ret = operContr.UpdateSupplierScope(finder);
                    operContr.Close();
                }
                catch (Exception ex)
                {
                    ret = new Returns(false);
                    ret.result = false;
                    ret.text = "Ошибка вызова функции - загрузка списка кодов квитанций";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка SupplierInfo \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }

            }
            return ret;
        }

        public List<int> GetDogovorERCChildsScope(SupplierInfo finder, out Returns ret)
        {
            List<int> list = new List<int>();
            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.GetDogovorERCChildsScope(finder, out ret);
            }
            else
            {
                try
                {
                    using (OperationsWithContracts operContr = new OperationsWithContracts())
                    {
                        list = operContr.GetDogovorERCChildsScope(finder, out ret);
                    }
                }
                catch (Exception ex)
                {
                    ret = new Returns(false);
                    ret.result = false;
                    ret.text = "Ошибка вызова функции - загрузка списка кодов квитанций";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetFilesName \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return list;
        }

        public List<DogovorRequisites> GetListDogERCByAgentAndPrincip(DogovorRequisites finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<DogovorRequisites> list = null;
            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.GetListDogERCByAgentAndPrincip(finder, out ret);
            }
            else
            {
                try
                {
                    using (DbPack dbpack = new DbPack())
                    {
                        list = dbpack.GetListDogERCByAgentAndPrincip(finder, out ret);
                    }
                }
                catch (Exception ex)
                {
                    ret = new Returns(false);
                    ret.result = false;
                    ret.text = "Ошибка вызова функции - загрузка списка расчетный счетов";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetListDogERCByAgentAndPrincip \n  " + ex.Message + ex.StackTrace, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return list;
        }

        public  Returns CheckPackLsToDeleting(Pack_ls finder)
        {
            Returns ret = Utils.InitReturns();
            List<DogovorRequisites> list = null;
            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.CheckPackLsToDeleting(finder);
            }
            else
            {
                try
                {
                    using (var dbpack = new DbPack())
                    {
                        ret = dbpack.CheckPackLsToDeleting(finder);
                    }
                }
                catch (Exception ex)
                {
                    ret = new Returns(false);
                    ret.result = false;
                    ret.text = "Ошибка вызова функции - загрузка списка расчетный счетов";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetListDogERCByAgentAndPrincip \n  " + ex.Message + ex.StackTrace, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return ret;
        }
        public Returns SelectOverPayments(OverPaymentsParams finder)
        {
            Returns ret = Utils.InitReturns();
            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.SelectOverPayments(finder);
            }
            else
            {
                try
                {
                    using (var dbpack = new DbCalcPack())
                    {
                        ret = dbpack.SelectOverPayments(finder);
                    }
                }
                catch (Exception ex)
                {
                    ret = new Returns(false);
                    ret.result = false;
                    ret.text = "Ошибка вызова функции - отбор переплат";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name +
                        " \n  " + ex.Message + ex.StackTrace, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return ret;
        }

        public List<OverpaymentStatusFinder> GetOverpaymentManStatus(Finder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<OverpaymentStatusFinder> list = new List<OverpaymentStatusFinder>();
            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.GetOverpaymentManStatus(finder, out ret);
            }
            else
            {
                try
                {
                    using (var dbpack = new DbCalcPack())
                    {
                        list = dbpack.GetOverpaymentManStatus(finder, out ret);
                    }
                }
                catch (Exception ex)
                {
                    ret = new Returns(false);
                    ret.result = false;
                    ret.text = "Ошибка вызова функции - Статус процесса управления переплатами";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name +
                    " \n  " + ex.Message + ex.StackTrace, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return list;
        }
        public Returns SetStatusOverpaymentManProc(OverpaymentStatusFinder finder)
        {
            Returns ret = Utils.InitReturns();
            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.SetStatusOverpaymentManProc(finder);
            }
            else
            {
                try
                {
                    using (var dbpack = new DbCalcPack())
                    {
                        ret = dbpack.SetStatusOverpaymentManProc(finder);
                    }
                }
                catch (Exception ex)
                {
                    ret = new Returns(false);
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name +
                        " \n  " + ex.Message + ex.StackTrace, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return ret;
        }

        public Returns CheckChoosenOverPyment(OverpaymentStatusFinder finder)
        {
            Returns ret = Utils.InitReturns();
            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.CheckChoosenOverPyment(finder);
            }
            else
            {
                try
                {
                    using (var dbpack = new DbCalcPack())
                    {
                        ret = dbpack.CheckChoosenOverPyment(finder);
                    }
                }
                catch (Exception ex)
                {
                    ret = new Returns(false);
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name +
                        " \n  " + ex.Message + ex.StackTrace, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return ret;
        }

        public List<SettingsPackPrms> OperateSettingsPack(SettingsPackPrms finder, enSrvOper oper, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<SettingsPackPrms> list = new List<SettingsPackPrms>();
            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Pack cli = new cli_Pack(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.OperateSettingsPack(finder, oper, out ret);
            }
            else
            {
                try
                {
                    using (var dbsettpack = new DBSettingPack())
                    {
                        switch (oper)
                        {
                            case enSrvOper.SrvGet:
                                list = dbsettpack.GetSettingsPack(finder, out ret);
                                break;
                            case enSrvOper.SrvAdd:
                                ret = dbsettpack.AddSettingPack(finder);
                                break;
                            case enSrvOper.SrvEdit:
                                ret = dbsettpack.EditSettingPack(finder);
                                break;
                            case enSrvOper.srvDelete:
                                ret = dbsettpack.DeleteSettingPack(finder);
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug)
                        MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name +
                                            " \n  " + ex.Message + ex.StackTrace, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return list;
        }
    }
}
