using System;
using System.Collections.Generic;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.DataBase;

namespace STCLINE.KP50.Client
{
    public class cli_Gilec : I_Gilec  //реализация клиента сервиса Кассы
    {
        List<GilecPrib> gilSpis = new List<GilecPrib>();

        I_Gilec remoteObject;

        public cli_Gilec(int nzp_server)
            : base()
        {
            _cli_Gilec(nzp_server);
        }

        void _cli_Gilec(int nzp_server)
        {
            if (Points.IsMultiHost && nzp_server > 0)
            {
                //определить параметры доступа
                _RServer zap = MultiHost.GetServer(nzp_server);
                //remoteObject = HostChannel.CreateInstance<I_Gilec>(zap.login, zap.pwd, zap.ip_adr + WCFParams.srvGilec);
                remoteObject = HostChannel.CreateInstance<I_Gilec>(zap.login, zap.pwd, zap.ip_adr + WCFParams.AdresWcfWeb.srvGilec);
            }
            else
            {
                //по-умолчанию
                //remoteObject = HostChannel.CreateInstance<I_Gilec>(WCFParams.Adres + WCFParams.srvGilec);
                remoteObject = HostChannel.CreateInstance<I_Gilec>(WCFParams.AdresWcfWeb.Adres + WCFParams.AdresWcfWeb.srvGilec);
            }
        }

        ~cli_Gilec()
        {
            try { if (remoteObject != null) HostChannel.CloseProxy(remoteObject); }
            catch { }
        }

        public List<GilecFullInf> CallFullInfGilList(Gilec finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<GilecFullInf> list = null;
            try
            {
                list = remoteObject.GetFullInfGilList(finder, out ret);
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
                else ret.text = "Ошибка сервиса получение списка жильцов";

                MonitorLog.WriteLog("Ошибка CallFullInfGilList \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return list;
        }
        //----------------------------------------------------------------------
        public List<GilecFullInf> GetFullInfGilList(Gilec finder, out Returns ret)
        {
            return CallFullInfGilList(finder, out ret);
        }
        //==============================================================================
        List<Kart> kartSpis = new List<Kart>();

        //----------------------------------------------------------------------
        public List<Kart> CallSrvKart(Kart finder, out Returns ret, byte tip)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                switch (tip)
                {
                    case 0:
                        {
                            kartSpis = remoteObject.FindKart(finder, out ret);
                            break;
                        }
                    case 1:
                        {
                            kartSpis = remoteObject.GetKart(finder, out ret);
                            break;
                        }

                }

                HostChannel.CloseProxy(remoteObject);
                return kartSpis;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка CallSrvKart(" + tip.ToString() + ") " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        //----------------------------------------------------------------------
        public List<Kart> GetKart(Kart finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            return CallSrvKart(finder, out ret, 1);
        }

        //----------------------------------------------------------------------
        public List<Kart> FindKart(Kart finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            return CallSrvKart(finder, out ret, 0);
        }

        public Kart LoadKart(Kart finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                Kart oneKart = remoteObject.LoadKart(finder, out ret);

                HostChannel.CloseProxy(remoteObject);
                return oneKart;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка LoadKart" + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public List<Kart> LoadPaspInfo(Kart finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                var manyKart = remoteObject.LoadPaspInfo(finder, out ret);

                HostChannel.CloseProxy(remoteObject);
                return manyKart;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка LoadPaspInfo" + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        List<String> my_msg = new List<String>();
        public List<String> ProverkaKart(Kart new_kart, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                my_msg = remoteObject.ProverkaKart(new_kart, out ret);

                HostChannel.CloseProxy(remoteObject);
                return my_msg;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка ProverkaKart" + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }


        public Kart SaveKart(Kart old_kart, Kart new_kart, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                Kart oneKart = remoteObject.SaveKart(old_kart, new_kart, out ret);

                HostChannel.CloseProxy(remoteObject);
                return oneKart;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка SaveKart" + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        //=====================================================================================
        List<Sprav> spravSpis = new List<Sprav>();


        //----------------------------------------------------------------------
        public List<Sprav> FindSprav(Sprav finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                spravSpis = remoteObject.FindSprav(finder, out ret);

                HostChannel.CloseProxy(remoteObject);
                return spravSpis;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка FindSaprav" + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }


        //==============================================================================
        List<GilPer> gilperSpis = new List<GilPer>();

        //----------------------------------------------------------------------
        public List<GilPer> CallSrvGilPer(GilPer finder, out Returns ret, byte tip)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                switch (tip)
                {
                    case 0:
                        {
                            gilperSpis = remoteObject.FindGilPer(finder, out ret);
                            break;
                        }
                    case 1:
                        {
                            gilperSpis = remoteObject.GetGilPer(finder, out ret);
                            break;
                        }

                }

                HostChannel.CloseProxy(remoteObject);
                return gilperSpis;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка CallSrvGilPer(" + tip.ToString() + ") " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        //----------------------------------------------------------------------
        public List<GilPer> GetGilPer(GilPer finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            return CallSrvGilPer(finder, out ret, 1);
        }

        //----------------------------------------------------------------------
        public List<GilPer> FindGilPer(GilPer finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            return CallSrvGilPer(finder, out ret, 0);
        }

        //-----------------------------------------------------------------------
        public string[] GetPasportistInformation(Gilec US, out Returns ret)
        //-----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            string[] arr = null;
            try
            {
                arr = remoteObject.GetPasportistInformation(US, out ret);
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
                else ret.text = "Ошибка сервиса получение информации о паспортисте";

                MonitorLog.WriteLog("Ошибка GetPasportistInformation \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return arr;
        }

        //---------------------------------------------------------------------------------
        public List<GilecFullInf> GetFullInfGilList_AllKards(Gilec finder, out Returns ret)
        {
            //для инициализации интерфейса
            ret = new Returns();
            List<GilecFullInf> list = null;
            try
            {
                list = remoteObject.GetFullInfGilList_AllKards(finder, out ret);
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
                else ret.text = "Ошибка сервиса получения списка жильцов";

                MonitorLog.WriteLog("Ошибка GetFullInfGilList_AllKards \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return list;
        }
        ////----------------------------------------------------------------------------------------------------------------------------
        //public string Fill_Sprav_Smert(Object rep, int y_, int m_, string date, int vidSprav, Gilec finder, Gilec us, out Returns ret)
        ////----------------------------------------------------------------------------------------------------------------------------
        //{
        //    ret = Utils.InitReturns();            
        //    ReportS report = new ReportS();
        //    //Список жильцов
        //    List<GilecFullInf> list = remoteObject.GetFullInfGilList_AllKards(finder, out ret);
        //    //Паспортист, начальник
        //    string[] psp = remoteObject.GetPasportistInformation(us, out ret);
        //    try
        //    {
        //        ret.result = true;
        //        return report.Fill_web_sprav_samara((Report)rep, y_, m_, date, vidSprav, finder, out ret, list, psp);
        //    }
        //    catch 
        //    {
        //        ret.text = "Ошибка заполнения отчета.";
        //        ret.result = false;
        //        return ""; 
        //    }
        //}
        ////------------------------------------------------------------------------------------------------------------------------------------------------------------
        //public string Fill_web_sprav_samara2(Object rep, int y_, int m_, string date, int vidSprav, Ls finder, Gilec us, out Returns ret)
        ////------------------------------------------------------------------------------------------------------------------------------------------------------------
        //{
        //    ret = Utils.InitReturns();
        //    ReportS report = new ReportS();
        //    //Список жильцов
        //    List<GilecFullInf> list = remoteObject.GetFullInfGilList(finder, out ret);
        //    //Паспортист, начальник
        //    string[] psp = remoteObject.GetPasportistInformation(us, out ret);
        //    try
        //    {
        //        ret.result = true;

        //        Gilec finder2 = new Gilec();
        //        finder2.nzp_user = finder.nzp_user;
        //        finder2.nzp_kvar = finder.nzp_kvar;
        //        finder2.nzp_kart = us.nzp_kart;
        //        finder2.pref = finder.pref;
        //        return report.Fill_web_sprav_samara2((Report)rep, y_, m_, date, vidSprav, finder2, out ret, list, psp);
        //    }
        //    catch
        //    {
        //        ret.text = "Ошибка заполнения отчета.";
        //        ret.result = false;
        //        return "";
        //    }
        //}


        public GilPer LoadGilPer(GilPer finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                GilPer oneGilPer = remoteObject.LoadGilPer(finder, out ret);

                HostChannel.CloseProxy(remoteObject);
                return oneGilPer;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка LoadGilPer" + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }


        public GilPer SaveGilPer(GilPer old_gilper, GilPer new_gilper, bool delete_flag, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                GilPer oneGilPer = remoteObject.SaveGilPer(old_gilper, new_gilper, delete_flag, out ret);

                HostChannel.CloseProxy(remoteObject);
                return oneGilPer;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка SaveGilPer" + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        ////---------------------------------------------------------------------------------------------------
        //public string Fill_web_sost_fam(Object rep, int y_, int m_, out Returns ret, Ls finder)
        ////---------------------------------------------------------------------------------------------------
        //{
        //    ret = Utils.InitReturns();
        //    ReportS report = new ReportS();
        //    //Список жильцов
        //    List<GilecFullInf> list = this.CallFullInfGilList(finder, out ret);                 
        //    try
        //    {
        //        ret.result = true;
        //        return report.Fill_web_sost_fam((Report)rep, y_, m_, finder, list);
        //    }
        //    catch
        //    {
        //        ret.text = "Ошибка заполнения отчета.";
        //        ret.result = false;
        //        return "";
        //    }
        //}

        ////---------------------------------------------------------------------------------------------------
        //public string Fill_web_vip_dom(Object rep, out Returns ret, int y_, int m_, Ls finder)
        ////---------------------------------------------------------------------------------------------------
        //{
        //    ret = Utils.InitReturns();
        //    ReportS report = new ReportS();
        //    //Список
        //    List<GilecFullInf> list = null;
        //    list = this.CallFullInfGilList(finder, out ret);

        //    try
        //    {
        //        ret.result = true;
        //        return report.Fill_web_vip_dom((Report)rep, y_, m_, finder, list);
        //    }
        //    catch
        //    {
        //        ret.text = "Ошибка заполнения отчета.";
        //        ret.result = false;
        //        return "";
        //    }

        //}

        ////---------------------------------------------------------------------------------------------------------
        //public string Reg_Po_MestuGilec_1(Object rep, out Returns ret, int y_, int m_, Gilec finder)
        ////---------------------------------------------------------------------------------------------------------
        //{
        //    ret = Utils.InitReturns();
        //    ReportS report = new ReportS();
        //    //Список
        //    List<Kart> listKart = null;
        //    listKart = remoteObject.Reg_Po_MestuGilec(out ret, finder);

        //    try
        //    {
        //        ret.result = true;
        //        return report.Reg_Po_MestuGilec((Report)rep, y_, m_, finder, listKart); 
        //    }
        //    catch
        //    {
        //        ret.text = "Ошибка заполнения отчета.";
        //        ret.result = false;
        //        return "";
        //    }
        //}
        //-------------------------------------------------------------------------------------------------------
        public List<Kart> Reg_Po_MestuGilec(out Returns ret, Gilec finder)
        //-------------------------------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<Kart> list = null;
            try
            {
                list = remoteObject.Reg_Po_MestuGilec(out ret, finder);
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
                else ret.text = "Ошибка сервиса получение информации по месту жительства";

                MonitorLog.WriteLog("Ошибка Reg_Po_MestuGilec \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return list;
        }


        //-------------------------------------------------------------------------------------------------------
        public Kart KartForSprav(Kart finder, out Returns ret)
        //-------------------------------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                Kart oneKart = remoteObject.KartForSprav(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return oneKart;
            }
            catch (Exception ex)
            {
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = "Ошибка сервиса получение карточки жильца для справки";

                MonitorLog.WriteLog("Ошибка KartForSparav \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        //-------------------------------------------------------------------------------------------------------
        public List<Kart> NeighborKart(Kart finder, out Returns ret)
        //-------------------------------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            List<Kart> list = null;
            try
            {
                list = remoteObject.NeighborKart(finder, out ret);
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
                else ret.text = "Ошибка сервиса получение списка соседей";

                MonitorLog.WriteLog("Ошибка NeighborKart \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            return list;
        }

        //-------------------------------------------------------------------------------------------------------
        public string DbMakeWhereString(Kart finder, out Returns ret)
        //-------------------------------------------------------------------------------------------------------
        {
            DbGilecClient db = new DbGilecClient();
            string res = db.MakeWhereString(finder, out ret);
            db.Close();
            return res;
        }
        //-------------------------------------------------------------------------------------------------------
        public string DbMakeWhereString(Gilec finder, out Returns ret)
        //-------------------------------------------------------------------------------------------------------
        {
            DbGilecClient db = new DbGilecClient();
            string res = db.MakeWhereString(finder, out ret);
            db.Close();
            return res;
        }
        public string DbMakeWhereString(string prms)
        {
            DbGilecClient db = new DbGilecClient();
            string res = db.MakeWhereString(prms);
            db.Close();
            return res;
        }

        public void DbSaveCheckedList(Gilec finder, List<string> checkedList, List<string> unCheckedList, out Returns ret)
        {
            DbGilecClient db = new DbGilecClient();
            db.SaveCheckedList(finder, checkedList, unCheckedList, out ret);
            db.Close();
        }

        
      public void MakeResponsible(Otvetstv finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                remoteObject.MakeResponsible(finder,out ret);
                HostChannel.CloseProxy(remoteObject);
                return ;
            }
            catch (Exception ex)
            {
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = "Ошибка обновления ФИО владельца квартиры из поквартирной карточки";

                MonitorLog.WriteLog("Ошибка MakeResponsible \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return ;
            }
        }
        public void CopyToOwner(Gilec finder, List<string> checkedList, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                remoteObject.CopyToOwner(finder, checkedList, out ret);
                HostChannel.CloseProxy(remoteObject);
                return ;
            }
            catch (Exception ex)
            {
                ret.result = false;
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret.text = Constants.access_error;
                    ret.tag = Constants.access_code;
                }
                else ret.text = "Ошибка копирования карточки жильца";

                MonitorLog.WriteLog("Ошибка CopyToOwner \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                return ;
            }
        }

        public string DbMakeGilPerWhereString(string prms)
        {
            DbGilecClient db = new DbGilecClient();
            string res = db.MakeGilPerWhereString(prms);
            db.Close();
            return res;
        }

        public List<Kart> DbGetKart(Kart finder, out Returns ret)
        {
            DbGilecClient db = new DbGilecClient();
            List<Kart> res = db.GetKart(finder, out ret);
            db.Close();
            return res;
        }

        //==============================================================================
        List<Sobstw> SobstwSpis = new List<Sobstw>();

        //----------------------------------------------------------------------
        public List<Sobstw> CallSrvSobstw(Sobstw finder, out Returns ret, byte tip)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            try
            {
                switch (tip)
                {
                    case 0:
                        {
                            SobstwSpis = remoteObject.FindSobstw(finder, out ret);
                            break;
                        }
                    case 1:
                        {
                            SobstwSpis = remoteObject.GetSobstw(finder, out ret);
                            break;
                        }

                }

                HostChannel.CloseProxy(remoteObject);
                return SobstwSpis;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка CallSrvSobstw(" + tip.ToString() + ") " + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        //----------------------------------------------------------------------
        public List<Sobstw> GetSobstw(Sobstw finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            return CallSrvSobstw(finder, out ret, 1);
        }

        //----------------------------------------------------------------------
        public List<Sobstw> FindSobstw(Sobstw finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            return CallSrvSobstw(finder, out ret, 0);
        }

        public List<DocSobstw> GetListDocSobstv(DocSobstw finder, out Returns ret)
        {
            try
            {
                List<DocSobstw> list = remoteObject.GetListDocSobstv(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return list;
            }
            catch (Exception ex)
            {
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret = new Returns(false, Constants.access_error, Constants.access_code);
                }
                else ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка GetListDocSobstv" + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public Returns DeleteDocSobstv(DocSobstw finder)
        {
            Returns ret;
            try
            {
                ret = remoteObject.DeleteDocSobstv(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret = new Returns(false, Constants.access_error, Constants.access_code);
                }
                else ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка DeleteDocSobstv()" + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public Returns SaveDepartureType(Sprav finder)
        {
            Returns ret;
            try
            {
                ret = remoteObject.SaveDepartureType(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret = new Returns(false, Constants.access_error, Constants.access_code);
                }
                else ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка SaveDepartureType()" + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public Returns SavePlaceRequirement(Sprav finder)
        {
            Returns ret;
            try
            {
                ret = remoteObject.SavePlaceRequirement(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret = new Returns(false, Constants.access_error, Constants.access_code);
                }
                else ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка SavePlaceRequirement()" + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public Returns SaveDocSobstw(List<DocSobstw> finder)
        {
            Returns ret;
            try
            {
                ret = remoteObject.SaveDocSobstw(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret = new Returns(false, Constants.access_error, Constants.access_code);
                }
                else ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка SaveDocSobstw()" + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public Sobstw LoadSobstw(Sobstw finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                Sobstw oneSobstw = remoteObject.LoadSobstw(finder, out ret);

                HostChannel.CloseProxy(remoteObject);
                return oneSobstw;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка LoadSobstw" + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }
        public Sobstw SaveSobstw(Sobstw old_Sobstw, Sobstw new_Sobstw, bool delete_flag, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                Sobstw oneSobstw = remoteObject.SaveSobstw(old_Sobstw, new_Sobstw, delete_flag, out ret);

                HostChannel.CloseProxy(remoteObject);
                return oneSobstw;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка SaveSobstw" + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }
        public string DbMakeSobstwWhereString(string prms)
        {
            DbGilecClient db = new DbGilecClient();
            string res = db.MakeSobstwWhereString(prms);
            db.Close();
            return res;
        }

        public List<String> ProverkaSobstw(Sobstw new_Sobstw, bool delete_flag, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                my_msg = remoteObject.ProverkaSobstw(new_Sobstw, delete_flag, out ret);

                HostChannel.CloseProxy(remoteObject);
                return my_msg;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка ProverkaSobstw" + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public FullAddress LoadFullAddress(Ls finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                FullAddress oneFullAddress = remoteObject.LoadFullAddress(finder, out ret);

                HostChannel.CloseProxy(remoteObject);
                return oneFullAddress;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка LoadFullAddress" + err, MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public string TotalRooms(Ls finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                string res = remoteObject.TotalRooms(finder, out ret);

                HostChannel.CloseProxy(remoteObject);
                return res;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка TotalRooms" + err, MonitorLog.typelog.Error, 2, 100, true);
                return "";
            }
        }

        public List<PaspCelPrib> FindCelPrib(PaspCelPrib finder, out Returns ret)
        {
            try
            {
                List<PaspCelPrib> list = remoteObject.FindCelPrib(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return list;
            }
            catch (Exception ex)
            {
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret = new Returns(false, Constants.access_error, Constants.access_code);
                }
                else ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка FindCelPrib" + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public Returns OperateWithCelPrib(PaspCelPrib finder, enSrvOper oper)
        {
            Returns ret;
            try
            {
                ret = remoteObject.OperateWithCelPrib(finder, oper);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret = new Returns(false, Constants.access_error, Constants.access_code);
                }
                else ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка OperateWithCelPrib(" + oper.ToString() + ")" + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public List<PaspDoc> FindDocs(PaspDoc finder, out Returns ret)
        {
            try
            {
                List<PaspDoc> list = remoteObject.FindDocs(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return list;
            }
            catch (Exception ex)
            {
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret = new Returns(false, Constants.access_error, Constants.access_code);
                }
                else ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка FindDocs" + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public Returns RecalcGillXX(Kart finder)
        {
            Returns ret;
            try
            {
                ret = remoteObject.RecalcGillXX(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret = new Returns(false, Constants.access_error, Constants.access_code);
                }
                else ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка RecalcGillXX()" + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public Returns OperateWithDoc(PaspDoc finder, enSrvOper oper)
        {
            Returns ret;
            try
            {
                ret = remoteObject.OperateWithDoc(finder, oper);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret = new Returns(false, Constants.access_error, Constants.access_code);
                }
                else ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка OperateWithDoc(" + oper.ToString() + ")" + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public Returns OperateWithSprav(Sprav finder, enSrvOper oper)
        {
            Returns ret;
            try
            {
                ret = remoteObject.OperateWithSprav(finder, oper);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret = new Returns(false, Constants.access_error, Constants.access_code);
                }
                else ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка OperateWithSprav(" + oper.ToString() + ")" + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public List<PaspOrganRegUcheta> FindOrganRegUcheta(PaspOrganRegUcheta finder, out bool hasRequisites, out Returns ret)
        {
            hasRequisites = false;
            try
            {
                List<PaspOrganRegUcheta> list = remoteObject.FindOrganRegUcheta(finder, out hasRequisites, out ret);
                HostChannel.CloseProxy(remoteObject);
                return list;
            }
            catch (Exception ex)
            {
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret = new Returns(false, Constants.access_error, Constants.access_code);
                }
                else ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка FindOrganRegUcheta" + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public Returns OperateWithOrganRegUcheta(PaspOrganRegUcheta finder, enSrvOper oper)
        {
            Returns ret;
            try
            {
                ret = remoteObject.OperateWithOrganRegUcheta(finder, oper);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret = new Returns(false, Constants.access_error, Constants.access_code);
                }
                else ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка OperateWithOrganRegUcheta(" + oper.ToString() + ")" + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public List<Sobstw> GetSobstvForOtchet(Sobstw finder, out Returns ret)
        {
            try
            {
                List<Sobstw> list = remoteObject.GetSobstvForOtchet(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return list;
            }
            catch (Exception ex)
            {
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret = new Returns(false, Constants.access_error, Constants.access_code);
                }
                else ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка GetSobstvForOtchet" + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public Returns GetFioVlad(Ls finder)
        {
            Returns ret;
            try
            {
                ret = remoteObject.GetFioVlad(finder);
                HostChannel.CloseProxy(remoteObject);
            }
            catch (Exception ex)
            {
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret = new Returns(false, Constants.access_error, Constants.access_code);
                }
                else ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка GetFioVlad()" + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
            }
            return ret;
        }

        public List<Kart> GetDataFromTXXTable(Kart finder, out Returns ret)
        {
            try
            {
                List<Kart> list = remoteObject.GetDataFromTXXTable(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return list;
            }
            catch (Exception ex)
            {
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret = new Returns(false, Constants.access_error, Constants.access_code);
                }
                else ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка GetDataFromTXXTable" + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }
        public List<Kart> GetDataFromKart(Kart finder, out Returns ret)
        {
            try
            {
                List<Kart> list = remoteObject.GetDataFromKart(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return list;
            }
            catch (Exception ex)
            {
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret = new Returns(false, Constants.access_error, Constants.access_code);
                }
                else ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка GetDataFromKart" + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        /// <summary>
        /// получение списка ответственных физ лиц (не проживающих и не собственников)
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public List<Otvetstv> GetOtvetstv(Ls finder, out Returns ret)
        {
            try
            {
                List<Otvetstv> list = remoteObject.GetOtvetstv(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return list;
            }
            catch (Exception ex)
            {
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret = new Returns(false, Constants.access_error, Constants.access_code);
                }
                else ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка GetOtvetstv" + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        /// <summary>
        /// Сохранение физического лица, который может быть назначен ответственным
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        public Returns SaveOtvetstv(Otvetstv finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                ret = remoteObject.SaveOtvetstv(finder);
                HostChannel.CloseProxy(remoteObject);
                return ret;
            }
            catch (Exception ex)
            {
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret = new Returns(false, Constants.access_error, Constants.access_code);
                }
                else ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка SaveOtvetstv " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return ret;
            }
        }

        public List<RelationsFinder> LoadRelations(RelationsFinder finder, out Returns ret)
        {
            try
            {
                List<RelationsFinder> list = remoteObject.LoadRelations(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return list;
            }
            catch (Exception ex)
            {
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret = new Returns(false, Constants.access_error, Constants.access_code);
                }
                else ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка LoadRelations" + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }

        public List<Kart> GetKvarKart(Kart finder, out Returns ret)
        {
            try
            {
                List<Kart> list = remoteObject.GetKvarKart(finder, out ret);
                HostChannel.CloseProxy(remoteObject);
                return list;
            }
            catch (Exception ex)
            {
                if (ex is System.ServiceModel.EndpointNotFoundException)
                {
                    ret = new Returns(false, Constants.access_error, Constants.access_code);
                }
                else ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка GetKvarKart" + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 2, 100, true);
                return null;
            }
        }
    }
}
