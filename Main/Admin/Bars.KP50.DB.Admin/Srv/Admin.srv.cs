using System;

using System.Collections.Generic;
using Newtonsoft.Json;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Global;
using STCLINE.KP50.Client;
using System.Data;


namespace STCLINE.KP50.Server
{
    public class srv_Admin : srv_Base, I_Admin
    {
        public ReturnsObjectType<BaseUser> GetUser(User finder)
        {
            ReturnsObjectType<BaseUser> ret;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetUser(finder);
            }
            else
            {
                try
                {
                    BaseUser user = cli_Admin.DbGetUser(finder);
                    ret = new ReturnsObjectType<BaseUser>(user, user != null, "");
                    // определение организации, к которой привязан пользователь
                    if (user != null && user.nzp_payer > 0)
                    {
                        using (var dbu = new DbUser())
                        {
                            ReturnsObjectType<BaseUser> result = dbu.GetUserOrganization(user);
                            if (result != null && result.result && result.returnsData != null)
                            {
                                user.payer = result.returnsData.payer;
                                user.nzp_supp = result.returnsData.nzp_supp;
                                user.nzp_area = result.returnsData.nzp_area;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetUser()\n " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    ret = new ReturnsObjectType<BaseUser>(null, false, ex.Message);
                }
            }
            return ret;
        }


        public Returns RemoveUserLock(Finder WebUserId)
        {
            Returns ret = Utils.InitReturns();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.RemoveUserLock(WebUserId);
            }
            else
            {
                DbAdmin db = new DbAdmin();
                try
                {
                    ret = db.RemoveUserLock(WebUserId);
                }
                catch (Exception ex)
                {
                    ret = Utils.InitReturns();
                    ret.text = "Ошибка вызова функции загрузки файлов в БД";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка RemoveUserLock" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }

            return ret;
        }

        public ReturnsObjectType<List<User>> GetUsers(User finder)
        {
            ReturnsObjectType<List<User>> ret;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetUsers(finder);
            }
            else
            {
                try
                {
                    ret = cli_Admin.DbGetUsers(finder);

                    if (ret != null && ret.result && ret.returnsData != null)
                    {
                        using (DbUser dbu = new DbUser())
                        {
                            dbu.FillUserOrganization(ret.returnsData);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка GetUsers()\n " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    ret = new ReturnsObjectType<List<User>>(null, false, ex.Message);
                }
            }
            return ret;
        }




#warning Старый метод загрузки характеристик и ЖКУ
        //public Returns LoadHarGilFondGKU(FilesImported finder)
        //{
        //    Returns ret = Utils.InitReturns();
        //    ReportGen ReportGenerator = new ReportGen();

        //    ParamContainer container = new ParamContainer();
        //    container.filesImportedFinder= finder;
        //    bool AddToThread = false;
        //    for (int i = 0; i < Constants.ExcelThreads.Length; i++)
        //    {
        //        if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
        //        {
        //            Constants.ExcelThreads[i] = new System.Threading.Thread(new System.Threading.ParameterizedThreadStart(ReportGenerator.LoadHarGilFondGKU));
        //            Constants.ExcelThreads[i].IsBackground = true;
        //            Constants.ExcelThreads[i].Start(container);
        //            AddToThread = true;
        //            break;
        //        }
        //    }


        //    if (AddToThread)
        //    {

        //    }
        //    else
        //    {
        //        ret.result = false;
        //        ret.text = "Все потоки заняты, повторите операцию позже.";
        //        MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
        //    }
        //    return ret;
        //}














        public Returns SetToChange(ServFormulFinder finder)
        {
            Returns ret = new Returns();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.SetToChange(finder);
            }
            else
            {
                DbAdmin db = new DbAdmin();
                try
                {
                    ret = db.SetToChange(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка SetToChange" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<List<ServFormulFinder>> GetServFormul(Finder finder)
        {
            ReturnsObjectType<List<ServFormulFinder>> ret = new ReturnsObjectType<List<ServFormulFinder>>();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetServFormul(finder);
            }
            else
            {
                DbAdmin db = new DbAdmin();

                try
                {
                    ret = db.GetServFormul(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetServFormul" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }






        public ReturnsObjectType<DownloadedData> GetFileGilec(Finder finder)
        {
            ReturnsObjectType<DownloadedData> ret = new ReturnsObjectType<DownloadedData>();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetFileGilec(finder);
            }
            else
            {
                DbAdmin db = new DbAdmin();
                try
                {
                    ret = db.GetFileGilec(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetFileGilec" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<DownloadedData> GetFileIpu(Finder finder)
        {
            ReturnsObjectType<DownloadedData> ret = new ReturnsObjectType<DownloadedData>();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetFileIpu(finder);
            }
            else
            {
                DbAdmin db = new DbAdmin();
                try
                {
                    ret = db.GetFileIpu(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetFileIpu" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<DownloadedData> GetFileIpuP(Finder finder)
        {
            ReturnsObjectType<DownloadedData> ret = new ReturnsObjectType<DownloadedData>();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetFileIpuP(finder);
            }
            else
            {
                DbAdmin db = new DbAdmin();
                try
                {
                    ret = db.GetFileIpuP(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetFileIpu" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<DownloadedData> GetFileOdpu(Finder finder)
        {
            ReturnsObjectType<DownloadedData> ret = new ReturnsObjectType<DownloadedData>();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetFileOdpu(finder);
            }
            else
            {
                DbAdmin db = new DbAdmin();
                try
                {
                    ret = db.GetFileOdpu(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetFileOdpu" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<DownloadedData> GetFileOdpuP(Finder finder)
        {
            ReturnsObjectType<DownloadedData> ret = new ReturnsObjectType<DownloadedData>();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetFileOdpuP(finder);
            }
            else
            {
                DbAdmin db = new DbAdmin();
                try
                {
                    ret = db.GetFileOdpuP(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetFileOdpu" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<DownloadedData> GetFileNedopost(Finder finder)
        {
            ReturnsObjectType<DownloadedData> ret = new ReturnsObjectType<DownloadedData>();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetFileNedopost(finder);
            }
            else
            {
                DbAdmin db = new DbAdmin();
                try
                {
                    ret = db.GetFileNedopost(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetFileNedopost" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<DownloadedData> GetFileOplats(Finder finder)
        {
            ReturnsObjectType<DownloadedData> ret = new ReturnsObjectType<DownloadedData>();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetFileOplats(finder);
            }
            else
            {
                DbAdmin db = new DbAdmin();
                try
                {
                    ret = db.GetFileOplats(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetFileOplats" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<DownloadedData> GetFileParamDom(Finder finder)
        {
            ReturnsObjectType<DownloadedData> ret = new ReturnsObjectType<DownloadedData>();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetFileParamDom(finder);
            }
            else
            {
                DbAdmin db = new DbAdmin();
                try
                {
                    ret = db.GetFileParamDom(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetFileParamDom" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<DownloadedData> GetFileParamLs(Finder finder)
        {
            ReturnsObjectType<DownloadedData> ret = new ReturnsObjectType<DownloadedData>();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetFileParamLs(finder);
            }
            else
            {
                DbAdmin db = new DbAdmin();
                try
                {
                    ret = db.GetFileParamLs(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetFileParamLs" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<DownloadedData> GetFileTypeNedop(Finder finder)
        {
            ReturnsObjectType<DownloadedData> ret = new ReturnsObjectType<DownloadedData>();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetFileTypeNedop(finder);
            }
            else
            {
                DbAdmin db = new DbAdmin();
                try
                {
                    ret = db.GetFileTypeNedop(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetFileTypeNedop" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<DownloadedData> GetFileTypeParams(Finder finder)
        {
            ReturnsObjectType<DownloadedData> ret = new ReturnsObjectType<DownloadedData>();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetFileTypeParams(finder);
            }
            else
            {
                DbAdmin db = new DbAdmin();
                try
                {
                    ret = db.GetFileTypeParams(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetFileTypeParams" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }


        public List<AreaCodes> GetAreaCodes(AreaCodes finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<AreaCodes> res = null;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.GetAreaCodes(finder, out ret);
            }
            else
            {
                DbAdmin db = new DbAdmin();
                try
                {
                    res = db.GetAreaCodes(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetAreaCodes() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = null;
                }
                db.Close();
            }
            return res;
        }

        public Returns GetMaxCodeFromAreaCodes()
        {
            Returns ret = new Returns();
            if (SrvRunProgramRole.IsBroker)
            {
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetMaxCodeFromAreaCodes();
            }
            else
            {
                try
                {
                    using (var db = new DbAdmin())
                    {
                        ret = db.GetMaxCodeFromAreaCodes();
                    }
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = ex.Message;
                    MonitorLog.WriteLog("Ошибка GetMaxCodeFromAreaCodes\n" + ex.Message, MonitorLog.typelog.Error, 30, 301, true);//2,100
                }
            }
            return ret;
        }

        public Returns SaveAreaCodes(AreaCodes finder)
        {
            Returns ret = new Returns();
            if (SrvRunProgramRole.IsBroker)
            {
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.SaveAreaCodes(finder);
            }
            else
            {
                try
                {
                    using (var db = new DbAdmin())
                    {
                        ret = db.SaveAreaCodes(finder);
                    }
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = ex.Message;
                    MonitorLog.WriteLog("Ошибка SaveAreaCodes\n" + ex.Message, MonitorLog.typelog.Error, 30, 301, true);//2,100
                }
            }
            return ret;
        }

        public Returns DeleteAreaCodes(AreaCodes finder)
        {
            Returns ret = new Returns();
            if (SrvRunProgramRole.IsBroker)
            {
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.DeleteAreaCodes(finder);
            }
            else
            {
                try
                {
                    using (DbAdmin db = new DbAdmin())
                    {
                        ret = db.DeleteAreaCodes(finder);
                    }
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = ex.Message;
                    MonitorLog.WriteLog("Ошибка DeleteAreaCodes\n" + ex.Message, MonitorLog.typelog.Error, 30, 301, true);//2,100
                }
            }
            return ret;
        }

        public Returns CreateSequence()
        {
            Returns ret = new Returns();
            if (SrvRunProgramRole.IsBroker)
            {
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.CreateSequence();
            }
            else
            {
                try
                {
                    using (var db = new DbAdmin())
                    {
                        ret = db.CreateSequence();
                    }
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = ex.Message;
                    MonitorLog.WriteLog("Ошибка CreateSequence\n" + ex.Message, MonitorLog.typelog.Error, 30, 301, true);//2,100
                }
            }
            return ret;
        }

        public Returns UploadEFS(FilesImported finder)
        {
            Returns ret = Utils.InitReturns();


            try
            {
                AUploadEFSDelegate dlgt = new AUploadEFSDelegate(this.AUploadEFS);

                //object o = new object();
                AsyncCallback cb = new AsyncCallback(ACallback);
                IAsyncResult ar = dlgt.BeginInvoke(finder, cb, dlgt);

                ret = new Returns(true, "Задание отправлено на выполнение.", -1);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка вызова функции загрузки ежедневных файлов сверки в БД";
                //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                if (Constants.Debug) MonitorLog.WriteLog("Ошибка UploadEFS" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            //db.Close();

            return ret;
        }

        public Returns AUploadEFS(FilesImported finder)
        {
            Returns ret = Utils.InitReturns();
            DbAdmin db = new DbAdmin();
            try
            {
                db.UploadEFS(finder);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка вызова функции загрузки ежедневных файлов сверки в БД";
                //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                if (Constants.Debug) MonitorLog.WriteLog("Ошибка AUploadEFS" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
            }
            finally { db.Close(); }
            return ret;

        }

        public delegate Returns AUploadEFSDelegate(FilesImported finder);

        public void ACallback(IAsyncResult ar)
        {
            // Because you passed your original delegate in the asyncState parameter
            // of the Begin call, you can get it back here to complete the call.
            //SaveUploadedCounterReadingsDelegate dlgt = (SaveUploadedCounterReadingsDelegate) ar.AsyncState;

            // Complete the call.
            //Returns ret = dlgt.EndInvoke(ar);

            //MonitorLog.WriteLog("Загрузка завершена", MonitorLog.typelog.Info, true);
        }
        public List<EFSReestr> GetEFSReestr(EFSReestr finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<EFSReestr> res = null;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.GetEFSReestr(finder, out ret);
            }
            else
            {
                DbAdmin db = new DbAdmin();
                try
                {
                    res = db.GetEFSReestr(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetEFSReestr() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = null;
                }
                db.Close();
            }
            return res;
        }

        public List<EFSPay> GetEFSPay(EFSPay finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<EFSPay> res = null;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.GetEFSPay(finder, out ret);
            }
            else
            {
                DbAdmin db = new DbAdmin();
                try
                {
                    res = db.GetEFSPay(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetEFSPay() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = null;
                }
                db.Close();
            }
            return res;
        }

        public List<EFSCnt> GetEFSCnt(EFSCnt finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<EFSCnt> res = null;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.GetEFSCnt(finder, out ret);
            }
            else
            {
                DbAdmin db = new DbAdmin();
                try
                {
                    res = db.GetEFSCnt(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetEFSCnt() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = null;
                }
                db.Close();
            }
            return res;
        }

        public Returns DeleteFromEFSReestr(EFSReestr finder)
        {
            Returns ret = new Returns();
            if (SrvRunProgramRole.IsBroker)
            {
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.DeleteFromEFSReestr(finder);
            }
            else
            {
                try
                {
                    using (var db = new DbAdmin())
                    {
                        ret = db.DeleteFromEFSReestr(finder);
                    }
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = ex.Message;
                    MonitorLog.WriteLog("Ошибка DeleteFromEFSReestr\n" + ex.Message, MonitorLog.typelog.Error, 30, 301, true);//2,100
                }
            }
            return ret;
        }

        /// <summary>


        /// <summary>
        /// Получить данные из sys_events
        /// </summary>
        /// <param name="finder">объект поиска</param>        
        /// <returns>результат</returns>
        public List<SysEvents> GetSysEvents(SysEvents finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<SysEvents> res = null;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.GetSysEvents(finder, out ret);
            }
            else
            {
                DbAdmin db = new DbAdmin();
                try
                {
                    res = db.GetSysEvents(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetSysEvents() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = null;
                }
                db.Close();
            }
            return res;
        }

        /// <summary>
        /// Получить список пользователей, которые засветились в sys_events
        /// </summary>
        /// <param name="finder">объект поиска</param>        
        /// <returns>результат</returns>
        public List<SysEvents> GetSysEventsUsersList(SysEvents finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<SysEvents> res = null;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.GetSysEventsUsersList(finder, out ret);
            }
            else
            {
                DbAdmin db = new DbAdmin();
                try
                {
                    res = db.GetSysEventsUsersList(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetSysEventsUsersList() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = null;
                }
                db.Close();
            }
            return res;
        }

        /// <summary>
        //получить список названий событий, засветившихся в sys_events
        /// </summary>
        /// <param name="finder">объект поиска</param>        
        /// <returns>результат</returns>
        public List<SysEvents> GetSysEventsEventsList(SysEvents finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<SysEvents> res = null;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.GetSysEventsEventsList(finder, out ret);
            }
            else
            {
                DbAdmin db = new DbAdmin();
                try
                {
                    res = db.GetSysEventsEventsList(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetSysEventsEventsList() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = null;
                }
                db.Close();
            }
            return res;
        }

        /// <summary>
        //получить список названий событий, засветившихся в sys_events
        /// </summary>
        /// <param name="finder">объект поиска</param>        
        /// <returns>результат</returns>
        public List<SysEvents> GetSysEventsActionsList(SysEvents finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<SysEvents> res = null;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.GetSysEventsActionsList(finder, out ret);
            }
            else
            {
                DbAdmin db = new DbAdmin();
                try
                {
                    res = db.GetSysEventsActionsList(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetSysEventsActionsList() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = null;
                }
                db.Close();
            }
            return res;
        }

        /// <summary>
        ///получить список названий сущностей, засветившихся в sys_events
        /// </summary>
        /// <param name="finder">объект поиска</param>        
        /// <returns>результат</returns>
        public List<SysEvents> GetSysEventsEntityList(SysEvents finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<SysEvents> res = null;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.GetSysEventsEntityList(finder, out ret);
            }
            else
            {
                DbAdmin db = new DbAdmin();
                try
                {
                    res = db.GetSysEventsEntityList(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetSysEventsEntityList() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = null;
                }
                db.Close();
            }
            return res;
        }

        /// <summary>
        ///получить историю изменений ИПУ
        /// </summary>
        /// <param name="finder">объект поиска</param>        
        /// <returns>результат</returns>
        public List<CountersArx> GetCountersChangeHistory(CountersArx finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<CountersArx> res = null;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.GetCountersChangeHistory(finder, out ret);
            }
            else
            {
                DbAdmin db = new DbAdmin();
                try
                {
                    res = db.GetCountersChangeHistory(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetCountersChangeHistory() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = null;
                }
                db.Close();
            }
            return res;
        }

        /// <summary>
        ///получить список полей ИПУ
        /// </summary>
        /// <param name="finder">объект поиска</param>        
        /// <returns>результат</returns>
        public List<CountersArx> GetCountersFields(CountersArx finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<CountersArx> res = null;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.GetCountersFields(finder, out ret);
            }
            else
            {
                DbAdmin db = new DbAdmin();
                try
                {
                    res = db.GetCountersFields(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetCountersFields() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = null;
                }
                db.Close();
            }
            return res;
        }

        /// <summary>
        ///получить пользователей ИПУ
        /// </summary>
        /// <param name="finder">объект поиска</param>        
        /// <returns>результат</returns>
        public List<CountersArx> GetCountersArxUsersList(CountersArx finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<CountersArx> res = null;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.GetCountersArxUsersList(finder, out ret);
            }
            else
            {
                DbAdmin db = new DbAdmin();
                try
                {
                    res = db.GetCountersArxUsersList(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetCountersArxUsersList() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = null;
                }
                db.Close();
            }
            return res;
        }

        /// <summary>
        ///сохранить в sys_events
        /// </summary>
        /// <param name="finder">объект поиска</param>        
        /// <returns>результат</returns>
        public bool InsertSysEvent(SysEvents finder)
        {
            var ret = Utils.InitReturns();
            bool res = false;

            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.InsertSysEvent(finder);
            }
            else
            {
                DbAdmin db = new DbAdmin();
                try
                {
                    res = db.InsertSysEvent(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка InsertSysEvent() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                    res = false;
                }
                db.Close();
            }
            return res;
        }

        /// <summary>
        /// получить дерево логов
        /// </summary>
        /// <param name="finder">объект поиска</param>        
        /// <returns>результат</returns>
        public LogsTree GetHostLogsList(LogsTree finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            LogsTree res = new LogsTree();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.GetHostLogsList(finder, out ret);
            }
            else
            {
                DbAdmin db = new DbAdmin();
                try
                {
                    res = db.GetHostLogsList(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetHostLogsList() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return res;
        }

        /// <summary>
        /// получить логи с хоста
        /// </summary>
        /// <param name="finder">объект поиска</param>        
        /// <returns>результат</returns>
        public LogsTree GetHostLogsFile(LogsTree finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            LogsTree res = new LogsTree();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо вызвать дальше другой хост
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                res = cli.GetHostLogsFile(finder, out ret);
            }
            else
            {
                DbAdmin db = new DbAdmin();
                try
                {
                    res = db.GetHostLogsFile(finder, out ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetHostLogsFile() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return res;
        }

        public List<KeyValue> LoadRoleSprav(Role finder, int role_kod, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<KeyValue> list = new List<KeyValue>();

            if (SrvRunProgramRole.IsBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.LoadRoleSprav(finder, role_kod, out ret);
            }
            else
            {
                DbAdmin db = new DbAdmin();
                try
                {
                    switch (role_kod)
                    {
                        case Constants.role_sql_area: list = db.LoadAreaAvailableForRole(finder, out ret); break;
                        case Constants.role_sql_geu: list = db.LoadGeuAvailableForRole(finder, out ret); break;
                        case Constants.role_sql_serv: list = db.LoadServiceAvailableForRole(finder, out ret); break;
                        case Constants.role_sql_supp: list = db.LoadSupplierAvailableForRole(finder, out ret); break;
                        case Constants.role_sql_prm: list = db.LoadPrmAvailableForRole(finder, out ret); break;
                        case Constants.role_sql_wp: list = db.LoadPointAvailableForRole(finder, out ret); break;
                    }
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции получения данных";
                    if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LoadRoleSprav() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return list;
        }

        public List<TransferHome> GetHouseList(TransferHome finder, out Returns ret)
        {
            var list = new List<TransferHome>();
            ret = Utils.InitReturns();
            if (SrvRunProgramRole.IsBroker)
            {
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                list = cli.GetHouseList(finder, out ret);
            }
            else
            {
                try
                {
                    using (var db = new DbAdmin())
                    {
                        list = db.GetHouseList(finder, out ret);
                    }
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = ex.Message;
                    MonitorLog.WriteLog("Ошибка DeleteFromEFSReestr\n" + ex.Message, MonitorLog.typelog.Error, 30, 301, true);//2,100
                }
            }
            return list;
        }

        public Returns RePrepareProvsOnListLs(Ls finder, TypePrepareProvs type)
        {
            var ret = Utils.InitReturns();
            if (SrvRunProgramRole.IsBroker)
            {
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.RePrepareProvsOnListLs(finder, type);
            }
            else
            {
                try
                {
                    using (var db = new DbAdmin())
                    {
                        ret = db.RePrepareProvsOnListLs(finder, type);
                    }
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = ex.Message;
                    MonitorLog.WriteLog("Ошибка RePrepareProvsOnListLs\n" + ex.Message, MonitorLog.typelog.Error, 30, 301, true);//2,100
                }
            }
            return ret;
        }

        public DateTime GetDateStartPeni(Finder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            DateTime startPeniDateTime = new DateTime();
            if (SrvRunProgramRole.IsBroker)
            {
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                startPeniDateTime = cli.GetDateStartPeni(finder, out ret);
            }
            else
            {
                try
                {
                    using (var db = new DbAdmin())
                    {
                        startPeniDateTime = db.GetDateStartPeni(finder, out ret);
                    }
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = ex.Message;
                    MonitorLog.WriteLog("Ошибка GetDateStartPeni\n" + ex.Message, MonitorLog.typelog.Error, 30, 301, true);//2,100
                }
            }
            return startPeniDateTime;
        }

        public int GetCountDayToDateObligation(Finder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            int countDay = -1;
            if (SrvRunProgramRole.IsBroker)
            {
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                countDay = cli.GetCountDayToDateObligation(finder, out ret);
            }
            else
            {
                try
                {
                    using (var db = new DbAdmin())
                    {
                        countDay = db.GetCountDayToDateObligation(finder, out ret);
                    }
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = ex.Message;
                    MonitorLog.WriteLog("Ошибка GetCountDayToDateObligation\n" + ex.Message, MonitorLog.typelog.Error, 30, 301, true);//2,100
                }
            }
            return countDay;
        }



        /// <summary>
        /// Запись проводок в фоновой задаче
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="type"></param>
        /// <param name="ret"></param>
        public void AddFonTaskProv(Finder finder, CalcFonTask.Types type, out Returns ret)
        {
            var calcfon = new CalcFonTask(Points.GetCalcNum(0))
            {
                TaskType = type,
                Status = FonTask.Statuses.New,
                nzp_user = finder.nzp_user,
                nzp = finder.nzp_wp,
                nzpt = finder.nzp_wp,
                pref = finder.pref
            };
            calcfon.txt = calcfon.processName;
            calcfon.parameters = JsonConvert.SerializeObject(finder);
            using (var db = new DbCalcQueueClient())
            {
                ret = db.AddTask(calcfon);
            }
        }

      

        public Returns DeleteCurrentRole(Finder finder)
        {
            var ret = Utils.InitReturns();
            if (SrvRunProgramRole.IsBroker)
            {
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.DeleteCurrentRole(finder);
            }
            else
            {
                try
                {
                    using (var db = new DbAdmin())
                    {
                        ret = db.DeleteCurrentRole(finder);
                    }
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = ex.Message;
                    MonitorLog.WriteLog("Ошибка DeleteCurrentRole\n" + ex.Message, MonitorLog.typelog.Error, 30, 301, true);//2,100
                }
            }
            return ret;
        }

        public Returns PrepareProvsForFirstCalcPeni(Finder finder)
        {
            var ret = Utils.InitReturns();
            if (SrvRunProgramRole.IsBroker)
            {
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.PrepareProvsForFirstCalcPeni(finder);
            }
            else
            {
                try
                {
                    AddFonTaskProv(finder, CalcFonTask.Types.taskRePrepareProvOnClosedCalcMonth, out ret);
                   
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = ex.Message;
                    MonitorLog.WriteLog("Ошибка PrepareProvsForFirstCalcPeni\n" + ex.Message, MonitorLog.typelog.Error, 30, 301, true);//2,100
                }
            }
            return ret;
        }
    }
}
