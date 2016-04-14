using System;
using System.Collections.Generic;
using STCLINE.KP50.Client;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.DataBase.Server;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.Server
{
    //----------------------------------------------------------------------
    public class srv_Gilec : srv_Base, I_Gilec //сервис паспортистки
    //----------------------------------------------------------------------
    {
        List<GilecPrib> gilSpis = new List<GilecPrib>();

        public List<GilecFullInf> GetFullInfGilList(Gilec finder, out Returns ret)
        {
            return CallFullInfGilList(finder, out ret);
        }

        //----------------------------------------------------------------------
        public List<GilecFullInf> CallFullInfGilList(Gilec finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            // List<GilecFullInf> lst = new List<GilecFullInf>();
            // lst =   DbGilec.test();
            using (DbGilec db = new DbGilec())
            {
                List<GilecFullInf> res = db.GetFullInfGilList(finder, out ret);
                return res;
            }
            
        }

        //=====================================================================================
        List<Kart> kartSpis = new List<Kart>();

        //----------------------------------------------------------------------
        public List<Kart> GetKart(Kart finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            return CallSrvKart(finder, out ret, false);
        }

        //----------------------------------------------------------------------
        public List<Kart> FindKart(Kart finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            return CallSrvKart(finder, out ret, true);
        }
        //----------------------------------------------------------------------
        public List<Kart> CallSrvKart(Kart finder, out Returns ret, bool find)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            kartSpis.Clear();
            using (var db = new DbGilec())
            {
                if (find) db.FindKart(finder, out ret);
                if (ret.result)
                    kartSpis = db.GetKart(finder, out ret);
                else
                    kartSpis = null;
            }
            return kartSpis;
        }

        public Kart LoadKart(Kart finder, out Returns ret)
        {
            Kart res;
            using (DbGilec db = new DbGilec())
            {
                res = db.LoadKart(finder, out ret);
            }
            return res;
        }

        public List<Kart> LoadPaspInfo(Kart finder, out Returns ret)
        {
            List<Kart> res;
            using (var db = new DbGilec())
            {
                res = db.LoadPaspInfo(finder, out ret);
            }
            return res;
        }

        public List<String> ProverkaKart(Kart new_kart, out Returns ret)
        {
            List<String> res;
            using (var db = new DbGilec())
            {
                res = db.ProverkaKart(new_kart, out ret);
            }
            return res;
        }

        public Kart SaveKart(Kart old_kart, Kart new_kart, out Returns ret)
        {
            Kart res;
            using (var db = new DbGilec())
            {
                res = db.SaveKart(old_kart, new_kart, out ret);
            }
            return res;
        }

        //=====================================================================================
        //        List<Sprav> spravSpis = new List<Sprav>();


        //----------------------------------------------------------------------
        public List<Sprav> FindSprav(Sprav finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            List<Sprav> res;
            using (var db = new DbGilec())
            {
                res = db.FindSprav(finder, out ret);
            }
            return res;
        }


        //=====================================================================================
        //----------------------------------------------------------------------
        public List<GilPer> GetGilPer(GilPer finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<GilPer> res;
            using (var db = new DbGilec())
            {
                res = db.GetGilPer(finder, out ret);
            }
            return res;
        }

        //----------------------------------------------------------------------
        public List<GilPer> FindGilPer(GilPer finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<GilPer> res = null;
            using (var db = new DbGilec())
            {
                db.FindGilPer(finder, out ret);
                
                if (ret.result)
                    res = db.GetGilPer(finder, out ret);
            }
            return res;
        }

        //----------------------------------------------------------------------
        public string[] GetPasportistInformation(Gilec US, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            string[] res;
            using (var db = new DbGilec())
            {
                res = db.GetPasportistInformation(US, out ret);
            }
            return res;
        }

        //--------------------------------------------------------------------------------
        public List<GilecFullInf> GetFullInfGilList_AllKards(Gilec finder, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<GilecFullInf> res;
            using (DbGilec db = new DbGilec())
            {
                res = db.GetFullInfGilList_AllKards(finder, out ret);
            }
            return res;
        }

        public Returns RecalcGillXX(Kart finder)
        {
            Returns ret = Utils.InitReturns();
            if (SrvRun.isBroker)
            {
                cli_Gilec cli = new cli_Gilec(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.RecalcGillXX(finder);
            }
            else
            {
                try
                {
                    DbGilec db = new DbGilec();
                    ret = db.RecalcGillXX(finder);
                    db.Close();
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка в операции удаления документа о собственности";
                    if (Constants.Viewerror) ret.text += ": " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка SaveDocSobstw()\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return ret;    
        }
        ////-----------------------------------------------------------------------------------------------------------------------------
        //public string Fill_Sprav_Smert(Object rep, int y_, int m_, string date, int vidSprav, Gilec finder, Gilec us, out Returns ret)
        ////-----------------------------------------------------------------------------------------------------------------------------
        //{
        //    //для инициализации интерфейса
        //    ret = Utils.InitReturns();            
        //    return null;
        //}
        ////-------------------------------------------------------------------------------------------------------------------------------------------------------------
        //public string Fill_web_sprav_samara2(Object rep, int y_, int m_, string date, int vidSprav, Ls finder, Gilec us, out Returns ret)
        ////-------------------------------------------------------------------------------------------------------------------------------------------------------------
        //{
        //    //для инициализации интерфейса
        //    ret = Utils.InitReturns();
        //    return null;
        //}

        public GilPer LoadGilPer(GilPer finder, out Returns ret)
        {
            GilPer res;
            using (var db = new DbGilec())
            {
                res = db.LoadGilPer(finder, out ret);
            }
            return res;
        }
        public GilPer SaveGilPer(GilPer old_gilper, GilPer new_gilper, bool delete_flag, out Returns ret)
        {
            GilPer res;
            using (var db = new DbGilec())
            {
                res = db.SaveGilPer(old_gilper, new_gilper, delete_flag, out ret);
            }
            return res;
        }

        ////------------------------------------------------------------------------------
        //public string Fill_web_sost_fam(Object rep, int y_, int m_, out Returns ret, Ls finder)
        ////------------------------------------------------------------------------------
        //{
        //    //для инициализации интерфейса
        //    ret = Utils.InitReturns();
        //    return null;
        //}

        ////------------------------------------------------------------------------------------
        //public string Fill_web_vip_dom(Object rep, out Returns ret, int y_, int m_, Ls finder)
        ////------------------------------------------------------------------------------------
        //{
        //    //для инициализации интерфейса
        //    ret = Utils.InitReturns();
        //    return null;
        //}

        public List<Kart> Reg_Po_MestuGilec(out Returns ret, Gilec finder)
        {
            ret = Utils.InitReturns();
            List<Kart> res;
            using (var db = new DbGilec())
            {
                res = db.Reg_Po_MestuGilec(finder, out ret);
            }
            return res;
        }


        public Kart KartForSprav(Kart finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            Kart res;
            using (var db = new DbGilec())
            {
                res = db.KartForSprav(finder, out ret);
            }
            return res;
        }

        public void CopyToOwner(Gilec finder, List<string> list, out Returns ret)
        {
            ret = Utils.InitReturns();
            using (var db = new DbGilec())
            {
                db.CopyToOwner(finder, list, out ret);
            }
        }

        public void MakeResponsible(Otvetstv finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            using (var db = new DbGilec())
            {
                db.MakeResponsible(finder, out ret);
            }
        }

        public List<Kart> NeighborKart(Kart finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Kart> res;
            using (var db = new DbGilec())
            {

                res = db.NeighborKart(finder, out ret);
            }
            return res;
        }


        //=====================================================================================
        //----------------------------------------------------------------------
        public List<Sobstw> GetSobstw(Sobstw finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<Sobstw> res;
            using (DbGilec db = new DbGilec())
            {
                res = db.GetSobstw(finder, out ret);
            }
            return res;
        }

        //----------------------------------------------------------------------
        public List<Sobstw> FindSobstw(Sobstw finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<Sobstw> res = null;
            using (DbGilec db = new DbGilec())
            {
                db.FindSobstw(finder, out ret);

                if (ret.result)
                    res = db.GetSobstw(finder, out ret);
            }
            return res;
        }

        public Sobstw LoadSobstw(Sobstw finder, out Returns ret)
        {
            Sobstw res;
            using (var db = new DbGilec())
            {
                res = db.LoadSobstw(finder, out ret);
            }
            return res;
        }
        public Sobstw SaveSobstw(Sobstw old_Sobstw, Sobstw new_Sobstw, bool delete_flag, out Returns ret)
        {
            Sobstw res;
            using (var db = new DbGilec())
            {
                res = db.SaveSobstw(old_Sobstw, new_Sobstw, delete_flag, out ret);
            }
            return res;
        }
        public List<String> ProverkaSobstw(Sobstw new_Sobstw, bool delete_flag, out Returns ret)
        {
            List<String> res;
            using (var db = new DbGilec())
            {
                res = db.ProverkaSobstw(new_Sobstw, delete_flag, out ret);
            }
            return res;
        }
        public List<DocSobstw> GetListDocSobstv(DocSobstw finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            DbGilec db = new DbGilec();
            List<DocSobstw> res = db.GetListDocSobstv(finder, out ret);            
            db.Close();
            return res;
        }

        public Returns DeleteDocSobstv(DocSobstw finder)
        {
            Returns ret = Utils.InitReturns();
            if (SrvRun.isBroker)
            {
                cli_Gilec cli = new cli_Gilec(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.DeleteDocSobstv(finder);
            }
            else
            {
                try
                {
                    DbGilec db = new DbGilec();
                    ret = db.DeleteDocSobstv(finder);
                    db.Close();
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка в операции удаления документа о собственности";
                    if (Constants.Viewerror) ret.text += ": " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка DeleteDocSobstv()\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return ret;
        }

        public Returns SaveDepartureType(Sprav finder)
        {
            Returns ret = Utils.InitReturns();
            if (SrvRun.isBroker)
            {
                cli_Gilec cli = new cli_Gilec(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.SaveDepartureType(finder);
            }
            else
            {
                try
                {
                    DbGilec db = new DbGilec();
                    ret = db.SaveDepartureType(finder);
                    db.Close();
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка в операции сохранения справочника типов временного убытия";
                    if (Constants.Viewerror) ret.text += ": " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка SaveDepartureType()\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return ret;
        }

        public Returns SavePlaceRequirement(Sprav finder)
        {
            Returns ret = Utils.InitReturns();
            if (SrvRun.isBroker)
            {
                cli_Gilec cli = new cli_Gilec(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.SavePlaceRequirement(finder);
            }
            else
            {
                try
                {
                    DbGilec db = new DbGilec();
                    ret = db.SavePlaceRequirement(finder);
                    db.Close();
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка в операции сохранения справочника места требования";
                    if (Constants.Viewerror) ret.text += ": " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка SavePlaceRequirement()\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return ret;
        }

        public Returns SaveDocSobstw(List<DocSobstw> finder)
        {           
            Returns ret = Utils.InitReturns();
            if (SrvRun.isBroker)
            {
                cli_Gilec cli = new cli_Gilec(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.SaveDocSobstw(finder);
            }
            else
            {
                try
                {
                    DbGilec db = new DbGilec();
                    ret = db.SaveDocSobstw(finder);
                    db.Close();
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка в операции удаления документа о собственности";
                    if (Constants.Viewerror) ret.text += ": " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка SaveDocSobstw()\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return ret;           
        }

        public FullAddress LoadFullAddress(Ls finder, out Returns ret)
        {
            FullAddress res;
            using (var db = new DbGilec())
            {
                 res = db.LoadFullAddress(finder, out ret);
            }
            return res;
        }

        public string TotalRooms(Ls finder, out Returns ret)
        {
            string res;
            using (var db = new DbGilec())
            {
                res = db.TotalRooms(finder, out ret);
            }
            return res;
        }

        public List<PaspCelPrib> FindCelPrib(PaspCelPrib finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<PaspCelPrib> list = null;
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Gilec cli = new cli_Gilec(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.FindCelPrib(finder, out ret);
            }
            else
            {
                try
                {
                    using (DbServerGilec db = new DbServerGilec())
                    {
                        list = db.FindCelPrib(finder, out ret);
                    }
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка в функции получения списка целей прибытия/убытия";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка FindCelPrib()\n " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return list;
        }

        public Returns OperateWithCelPrib(PaspCelPrib finder, enSrvOper oper)
        {
            Returns ret = Utils.InitReturns();
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Gilec cli = new cli_Gilec(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.OperateWithCelPrib(finder, oper);
            }
            else
            {
                try
                {
                    using (var db = new DbServerGilec())
                    {
                        switch (oper)
                        {
                            case enSrvOper.srvSave:
                                ret = db.SaveCelPrib(finder);
                                break;
                            default:
                                ret = new Returns(false, "Неверное наименование операции");
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка в операции с целью прибытия/убытия";
                    if (Constants.Viewerror) ret.text += ": " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка OperateWithCelPrib(" + oper.ToString() + ")\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return ret;
        }

        public List<PaspDoc> FindDocs(PaspDoc finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<PaspDoc> list = null;
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Gilec cli = new cli_Gilec(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.FindDocs(finder, out ret);
            }
            else
            {
                try
                {
                    using (DbServerGilec db = new DbServerGilec())
                    {
                        list = db.FindDocs(finder, out ret);
                    }
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка в функции получения значений справочника \"Документы\"";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка FindDocs()\n " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return list;
        }

        public Returns OperateWithDoc(PaspDoc finder, enSrvOper oper)
        {
            Returns ret = Utils.InitReturns();
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Gilec cli = new cli_Gilec(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.OperateWithDoc(finder, oper);
            }
            else
            {
                try
                {
                    using (DbServerGilec db = new DbServerGilec())
                    {
                        switch (oper)
                        {
                            case enSrvOper.srvSave:
                                ret = db.SaveDoc(finder);
                                break;
                            default:
                                ret = new Returns(false, "Неверное наименование операции");
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка в операции с документом";
                    if (Constants.Viewerror) ret.text += ": " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка OperateWithDoc(" + oper.ToString() + ")\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return ret;
        }

        public Returns OperateWithSprav(Sprav finder, enSrvOper oper)
        {
            Returns ret = Utils.InitReturns();
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Gilec cli = new cli_Gilec(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.OperateWithSprav(finder, oper);
            }
            else
            {
                DbServerGilec db = new DbServerGilec();
                try
                {

                    switch (oper)
                    {
                        case enSrvOper.srvSave:
                            ret = db.SaveSprav(finder);
                            break;
                        case enSrvOper.srvDelete:
                            ret = db.DeleteSprav(finder);
                            break;
                        default:
                            ret = new Returns(false, "Неверное наименование операции");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка в операции с документом";
                    if (Constants.Viewerror) ret.text += ": " + ex.Message;
                    if (Constants.Debug)
                        MonitorLog.WriteLog("Ошибка OperateWithSprav(" + oper.ToString() + ")\n" + ex.Message,
                            MonitorLog.typelog.Error, 2, 100, true);
                }
                finally
                {
                    db.Close();
                }
            }
            return ret;
        }

        public List<PaspOrganRegUcheta> FindOrganRegUcheta(PaspOrganRegUcheta finder, out bool hasRequisites, out Returns ret)
        {
            ret = Utils.InitReturns();
            hasRequisites = false;
            List<PaspOrganRegUcheta> list = null;
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Gilec cli = new cli_Gilec(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.FindOrganRegUcheta(finder, out hasRequisites, out ret);
            }
            else
            {
                try
                {
                    using (var db = new DbServerGilec())
                    {
                        list = db.FindOrganRegUcheta(finder, out hasRequisites, out ret);
                    }
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка в функции получения значений справочника \"Орган рнгистрационного учета\"";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка FindOrganRegUcheta()\n " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return list;
        }

        public Returns OperateWithOrganRegUcheta(PaspOrganRegUcheta finder, enSrvOper oper)
        {
            Returns ret = Utils.InitReturns();
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Gilec cli = new cli_Gilec(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.OperateWithOrganRegUcheta(finder, oper);
            }
            else
            {
                DbServerGilec db = new DbServerGilec();
                try
                {

                    switch (oper)
                    {
                        case enSrvOper.srvSave:
                            ret = db.SaveOrganRegUcheta(finder);
                            break;
                        default:
                            ret = new Returns(false, "Неверное наименование операции");
                            break;
                    }

                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка в операции с документом";
                    if (Constants.Viewerror) ret.text += ": " + ex.Message;
                    if (Constants.Debug)
                        MonitorLog.WriteLog("Ошибка OperateWithOrganRegUcheta(" + oper.ToString() + ")\n" + ex.Message,
                            MonitorLog.typelog.Error, 2, 100, true);
                }
                finally
                {
                    db.Close();
                }
            }
            return ret;
        }

        public List<Sobstw> GetSobstvForOtchet(Sobstw finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Sobstw> list = null;
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Gilec cli = new cli_Gilec(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.GetSobstvForOtchet(finder, out ret);
            }
            else
            {
                try
                {
                    using (var db = new DbServerGilec())
                    {
                        list = db.GetSobstvForOtchet(finder, out ret);
                    }
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка в функции получения собственников для отчета";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetSobstvForOtchet()\n " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return list;
        }

        public Returns GetFioVlad(Ls finder)
        {
            Returns ret = Utils.InitReturns();
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Gilec cli = new cli_Gilec(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetFioVlad(finder);
            }
            else
            {
                try
                {
                    using (var db = new DbServerGilec())
                    {
                        ret = db.GetFioVlad(finder);
                    }
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка в при получении ФИО владельца жилья";
                    if (Constants.Viewerror) ret.text += ": " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetFioVlad()\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return ret;
        }

        public List<Kart> GetDataFromTXXTable(Kart finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Kart> list = null;
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Gilec cli = new cli_Gilec(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.GetDataFromTXXTable(finder, out ret);
            }
            else
            {
                try
                {
                    using (var db = new DbServerGilec())
                    {
                        list = db.GetDataFromTXXTable(finder, out ret);
                    }
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка в функции получения собственников для отчета";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetDataFromTXXTable()\n " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return list;
        }
        public List<Kart> GetDataFromKart(Kart finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Kart> list = null;
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Gilec cli = new cli_Gilec(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.GetDataFromKart(finder, out ret);
            }
            else
            {
                try
                {
                    using (var db = new DbServerGilec())
                    {
                        list = db.GetDataFromKart(finder, out ret);
                    }
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка в функции получения списка жильцов из карты";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetDataFromKart()\n " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return list;
        }

        public List<Otvetstv> GetOtvetstv(Ls finder, out Returns ret)
        {
            ret = new Returns();
            List<Otvetstv> list = null;
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Gilec cli = new cli_Gilec(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.GetOtvetstv(finder, out ret);
            }
            else
            {
                try
                {
                    using (var db = new DbServerGilec())
                    {
                        list = db.GetOtvetstv(finder, out ret);
                    }
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка в функции получения списка ответственных физических лиц";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetOtvetstv()\n " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return list;
        }

        public Returns SaveOtvetstv(Otvetstv finder)
        {
            Returns ret = Utils.InitReturns();
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Gilec cli = new cli_Gilec(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.SaveOtvetstv(finder);
            }
            else
            {
                try
                {
                    using (var db = new DbServerGilec())
                    {
                        ret = db.SaveOtvetstv(finder);
                    }
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка в функции сохранения физического лица, который может быть назначен ответственным";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка SaveOtvetstv()\n " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return ret;
        }

        public List<RelationsFinder> LoadRelations(RelationsFinder finder, out Returns ret)
        {
            ret = new Returns();
            List<RelationsFinder> list = null;
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Gilec cli = new cli_Gilec(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.LoadRelations(finder, out ret);
            }
            else
            {
                try
                {
                    using (var db = new DbServerGilec())
                    {
                        list = db.LoadRelations(finder, out ret);
                    }
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка в функции получения списка родственных отношений";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LoadRelations()\n " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return list;
        }

        public List<Kart> GetKvarKart(Kart finder, out Returns ret)
        {
            ret = new Returns();
            List<Kart> list = null;
            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Gilec cli = new cli_Gilec(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.GetKvarKart(finder, out ret);
            }
            else
            {
                try
                {
                    using (var db = new DbServerGilec())
                    {
                        list = db.GetKvarKart(finder, out ret);
                    }
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка в функции получения списка родственных отношений";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetKvarKart()\n " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
            }
            return list;
        }

    }
}
