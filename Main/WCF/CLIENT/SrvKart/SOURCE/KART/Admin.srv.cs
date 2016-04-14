using System;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Net;
using System.Security.Principal;

using System.Collections.Generic;
using Bars.KP50.DataImport.SOURCE;
using Bars.KP50.Gubkin;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Global;
using STCLINE.KP50.Client;
using System.Data;
using STCLINE.KP50.REPORT;
using System.IO;

namespace STCLINE.KP50.Server
{
    public class srv_Admin : srv_Base, I_Admin
    {
        public ReturnsObjectType<BaseUser> GetUser(User finder)
        {
            ReturnsObjectType<BaseUser> ret;

            if (SrvRun.isBroker)
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
                        DbUser dbu = new DbUser();
                        ReturnsObjectType<BaseUser> result = dbu.GetUserOrganization(user);
                        dbu.Close();

                        if (result != null && result.result && result.returnsData != null)
                        {
                            user.payer = result.returnsData.payer;
                            user.nzp_supp = result.returnsData.nzp_supp;
                            user.nzp_area = result.returnsData.nzp_area;
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

        public ReturnsObjectType<List<UploadingData>> GetUploadingProgress(UploadingData finder)
        {
            ReturnsObjectType<List<UploadingData>> ret;

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetUploadingProgress(finder);
            }
            else
            {
                #region Подключение к БД
                IDbConnection con_db = DBManager.GetConnection(Global.Constants.cons_Kernel);
                if (!DBManager.OpenDb(con_db, true).result)
                {
                    ret = new ReturnsObjectType<List<UploadingData>>() { result = false };
                    ret.text = "Ошибка при открытии соединения.";
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка при открытии соединения в ф-ции GetUploadingProgress" + Environment.NewLine, MonitorLog.typelog.Error, 2, 100, true);
                }
                #endregion Подключение к БД
                DbKladr db = new DbKladr(con_db);
                try
                {
                    Returns r;
                    ret = new ReturnsObjectType<List<UploadingData>>(db.GetUploadingProgress(finder, out r));
                    ret.tag = r.tag;
                }
                catch (Exception ex)
                {
                    ret = new ReturnsObjectType<List<UploadingData>>() { result = false };
                    ret.text = "Ошибка вызова функции загрузки файлов в БД";
                    ////if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetUploadingProgress" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<List<FilesImported>> GetFiles(FilesImported finder)
        {
            ReturnsObjectType<List<FilesImported>> ret;

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetFiles(finder);
            }
            else
            {
                DbAdmin db = new DbAdmin();
                try
                {
                    Returns r; 
                    ret = new ReturnsObjectType<List<FilesImported>>(db.GetFiles(finder, out r));
                    ret.tag = r.tag;
                }
                catch (Exception ex)
                {
                    ret = new ReturnsObjectType<List<FilesImported>>() { result = false };
                    ret.text = "Ошибка вызова функции загрузки файлов в БД";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetFiles" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<List<User>> GetUsers(User finder)
        {
            ReturnsObjectType<List<User>> ret;

            if (SrvRun.isBroker)
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
                        DbUser dbu = new DbUser();
                        ReturnsType result2 = dbu.FillUserOrganization(ret.returnsData);
                        dbu.Close();
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
        
        public Returns UploadInDb(FilesImported finder, UploadOperations operation, UploadMode mode)
        {
            Returns ret = Utils.InitReturns();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.UploadInDb(finder, operation, mode);
            }
            else
            {
                DbAdmin db = new DbAdmin();
                try
                {
                    switch (operation)
                    {
                        case UploadOperations.Area:
                            if (mode == UploadMode.Add) ret = db.UploadAreaInDb(finder, true);
                            else ret = db.UploadAreaInDb(finder);
                            break;
                        case UploadOperations.Dom:
                            if (mode == UploadMode.Add) ret = db.UploadDomInDb(finder, true);
                            else ret = db.UploadDomInDb(finder);
                            break;
                        case UploadOperations.Kvar:
                            if (mode == UploadMode.Add) ret = db.UploadLsInDb(finder, true);
                            else ret = db.UploadLsInDb(finder);
                            break;
                        case UploadOperations.Supp:
                            if (mode == UploadMode.Add) ret = db.UploadSuppInDb(finder, true);
                            else ret = db.UploadSuppInDb(finder);
                            break;
                        default: ret = new Returns(false, "Неверное наименование операции"); break;
                    }

                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции UploadInDb";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка UploadInDb\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public Returns LoadHarGilFondGKU(FilesImported finder)
        {
            Returns ret = Utils.InitReturns();
            ReportGen ReportGenerator = new ReportGen();

            ParamContainer container = new ParamContainer();
            container.filesImportedFinder= finder;
            bool AddToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(new System.Threading.ParameterizedThreadStart(ReportGenerator.LoadHarGilFondGKU));
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    AddToThread = true;
                    break;
                }
            }


            if (AddToThread)
            {

            }
            else
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }
            return ret;
        }

        public Returns LoadOneTime(FilesImported finder)
        {
            Returns ret = Utils.InitReturns();
            ReportGen ReportGenerator = new ReportGen();

            ParamContainer container = new ParamContainer();
            container.filesImportedFinder = finder;
            bool AddToThread = false;
            for (int i = 0; i < Constants.ExcelThreads.Length; i++)
            {
                if (Constants.ExcelThreads[i] == null || !Constants.ExcelThreads[i].IsAlive)
                {
                    Constants.ExcelThreads[i] = new System.Threading.Thread(new System.Threading.ParameterizedThreadStart(ReportGenerator.LoadOneTime));
                    Constants.ExcelThreads[i].IsBackground = true;
                    Constants.ExcelThreads[i].Start(container);
                    AddToThread = true;
                    break;
                }
            }


            if (AddToThread)
            {

            }
            else
            {
                ret.result = false;
                ret.text = "Все потоки заняты, повторите операцию позже.";
                MonitorLog.WriteLog("Все потоки заняты, повторите операцию позже.", MonitorLog.typelog.Warn, true);
            }
            return ret;
        }

        public ReturnsObjectType<List<KLADRData>> LoadDataFromKLADR(KLADRFinder finder)
        {
            ReturnsObjectType<List<KLADRData>> ret = new ReturnsObjectType<List<KLADRData>>();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.LoadDataFromKLADR(finder);
            }
            else
            {
                #region Подключение к БД
                IDbConnection con_db = DBManager.GetConnection(Global.Constants.cons_Kernel);
                if (!DBManager.OpenDb(con_db, true).result)
                {
                    MonitorLog.WriteLog("Ошибка открытия соединения в ф-ции CalcProc " +
                                        "при  при обработке задачи разбора файла (taskLoadKladr)",
                        MonitorLog.typelog.Error, true);
                    return ret;
                }
                #endregion Подключение к БД

                DbKladr db = new DbKladr(con_db);
                try
                {
                    ret = db.LoadDataFromKLADR(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LoadRegionKLADR" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                
            }
            return ret;
        }

        public Returns RefreshKLADRFile(FilesImported finder)
        {
            Returns ret = Utils.InitReturns();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.RefreshKLADRFile(finder);
            }
            else
            {
                #region Подключение к БД
                IDbConnection con_db = DBManager.GetConnection(Global.Constants.cons_Kernel);
                if (!DBManager.OpenDb(con_db, true).result)
                {
                    MonitorLog.WriteLog("Ошибка открытия соединения в ф-ции CalcProc " +
                                        "при  при обработке задачи разбора файла (taskLoadKladr)",
                        MonitorLog.typelog.Error, true);
                    return ret;
                }
                #endregion Подключение к БД

                DbKladr db = new DbKladr(con_db); 
                try
                {
                    ret = db.RefreshKLADRFile(finder, ref ret);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка RefreshKLADRFile" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public Returns UploadUESCharge(FilesImported finder)
        {
            Returns ret = Utils.InitReturns();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.UploadUESCharge(finder);
            }
            else
            {
                //DbAdmin db = new DbAdmin();
                DbUES db = new DbUES();
                try
                {
                    ret = db.UploadUESCharge(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки ";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка UploadUESCharge" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public Returns UploadMURCPayment(FilesImported finder)
        {
            Returns ret = Utils.InitReturns();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.UploadMURCPayment(finder);
            }
            else
            {
                //DbAdmin db = new DbAdmin();
                DbMURC db = new DbMURC();
               
                try
                {
                    ret = db.UploadMURCPayment(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки ";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка UploadMURCPayment" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<List<ComparedAreas>> GetComparedArea(Finder finder)
        {
            var ret = new ReturnsObjectType<List<ComparedAreas>>();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetComparedArea(finder);
            }
            else
            {
                //DbAdmin  db = new DbAdmin();
                DbCompareFile db = new DbCompareFile();
                
                try
                {
                    ret = db.GetComparedArea(finder);
                    
                    
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetComparedArea" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<List<ComparedSupps>> GetComparedSupp(Finder finder)
        {
            var ret = new ReturnsObjectType<List<ComparedSupps>>();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetComparedSupp(finder);
            }
            else
            {
                //DbAdmin  db = new DbAdmin();
                DbCompareFile db = new DbCompareFile();
                try
                {
                    ret = db.GetComparedSupp(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetComparedSupp" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<List<ComparedVills>> GetComparedMO(Finder finder)
        {
            var ret = new ReturnsObjectType<List<ComparedVills>>();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetComparedMO(finder);
            }
            else
            {
                //DbAdmin  db = new DbAdmin();
                DbCompareFile db = new DbCompareFile();
                try
                {
                    ret = db.GetComparedMO(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetComparedMO" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<List<ComparedServs>> GetComparedServ(Finder finder)
        {
            var ret = new ReturnsObjectType<List<ComparedServs>>();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetComparedServ(finder);
            }
            else
            {
                //DbAdmin  db = new DbAdmin();
                DbCompareFile db = new DbCompareFile();
                try
                {
                    ret = db.GetComparedServ(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetComparedServ" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<List<ComparedMeasures>> GetComparedMeasure(Finder finder)
        {
            var ret = new ReturnsObjectType<List<ComparedMeasures>>();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetComparedMeasure(finder);
            }
            else
            {
                //DbAdmin  db = new DbAdmin();
                DbCompareFile db = new DbCompareFile();
                try
                {
                    ret = db.GetComparedMeasure(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetComparedMeasure" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<List<ComparedParTypes>> GetComparedParType(Finder finder)
        {
            var ret = new ReturnsObjectType<List<ComparedParTypes>>();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetComparedParType(finder);
            }
            else
            {
                //DbAdmin  db = new DbAdmin();
                DbCompareFile db = new DbCompareFile();
                try
                {
                    ret = db.GetComparedParType(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetComparedParType" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<List<ComparedParTypes>> GetComparedParBlag(Finder finder)
        {
            var ret = new ReturnsObjectType<List<ComparedParTypes>>();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetComparedParBlag(finder);
            }
            else
            {
                //DbAdmin  db = new DbAdmin();
                DbCompareFile db = new DbCompareFile();
                try
                {
                    ret = db.GetComparedParBlag(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetComparedParBlag" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public Returns ReadReestrFromCbb(FilesImported finderpack, FilesImported finder, string connectionString)
        {
            var ret = new Returns();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.ReadReestrFromCbb( finderpack , finder,  connectionString);
            }
            else
            {
                //DbAdmin db = new DbAdmin();
                Oplats db = new Oplats();
                try
                {
                    ret = db.ReadReestrFromCbb( finderpack , finder,  connectionString);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка ReadReestrFromCbb" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<List<ComparedParTypes>> GetComparedParGas(Finder finder)
        {
            var ret = new ReturnsObjectType<List<ComparedParTypes>>();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetComparedParGas(finder);
            }
            else
            {
                //DbAdmin  db = new DbAdmin();
                DbCompareFile db = new DbCompareFile();
                try
                {
                    ret = db.GetComparedParGas(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetComparedParGas" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<List<ComparedParTypes>> GetComparedParWater(Finder finder)
        {
            var ret = new ReturnsObjectType<List<ComparedParTypes>>();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetComparedParWater(finder);
            }
            else
            {
                //DbAdmin  db = new DbAdmin();
                DbCompareFile db = new DbCompareFile();
                try
                {
                    ret = db.GetComparedParWater(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetComparedParWater" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }     

        public ReturnsObjectType<List<ComparedTowns>> GetComparedTown(Finder finder)
        {
            var ret = new ReturnsObjectType<List<ComparedTowns>>();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetComparedTown(finder);
            }
            else
            {
                //DbAdmin  db = new DbAdmin();
                DbCompareFile db = new DbCompareFile();
                try
                {
                    ret = db.GetComparedTown(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetComparedTown" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<List<ComparedRajons>> GetComparedRajon(Finder finder)
        {
            var ret = new ReturnsObjectType<List<ComparedRajons>>();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetComparedRajon(finder);
            }
            else
            {
                //DbAdmin  db = new DbAdmin();
                DbCompareFile db = new DbCompareFile();
                try
                {
                    ret = db.GetComparedRajon(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetComparedRajon" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<List<ComparedStreets>> GetComparedStreets(Finder finder)
        {
            var ret = new ReturnsObjectType<List<ComparedStreets>>();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetComparedStreets(finder);
            }
            else
            {
                //DbAdmin  db = new DbAdmin();
                DbCompareFile db = new DbCompareFile();
                try
                {
                    ret = db.GetComparedStreets(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetComparedStreets" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<List<ComparedHouses>> GetComparedHouse(Finder finder)
        {
            var ret = new ReturnsObjectType<List<ComparedHouses>>();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetComparedHouse(finder);
            }
            else
            {

                //DbAdmin db = new DbAdmin();
                DbCompareFile db = new DbCompareFile();
                try
                {
                    ret = db.GetComparedHouse(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetComparedHouse" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<List<ComparedLS>> GetComparedLS(Finder finder)
        {
            var ret = new ReturnsObjectType<List<ComparedLS>>();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetComparedLS(finder);
            }
            else
            {
                //DbAdmin  db = new DbAdmin();
                DbCompareFile db = new DbCompareFile();
                try
                {
                    ret = db.GetComparedLS(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetComparedLS" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public Returns SetToChange(ServFormulFinder finder)
        {
            Returns ret = new Returns();

            if (SrvRun.isBroker)
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

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetServFormul(finder);
            }
            else
            {
                DbAdmin  db = new DbAdmin();

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

        public ReturnsObjectType<List<UncomparedAreas>> GetUncomparedArea(Finder finder)
        {
            ReturnsObjectType<List<UncomparedAreas>> ret = new ReturnsObjectType<List<UncomparedAreas>>();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetUncomparedArea(finder);
            }
            else
            {
                //DbAdmin db = new DbAdmin();
                DbCompareFile db = new DbCompareFile();
                try
                {
                    ret = db.GetUncomparedArea(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetUncomparedArea" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<List<UncomparedSupps>> GetUncomparedSupp(Finder finder)
        {
            ReturnsObjectType<List<UncomparedSupps>> ret = new ReturnsObjectType<List<UncomparedSupps>>();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetUncomparedSupp(finder);
            }
            else
            {

                //DbAdmin db = new DbAdmin();
                DbCompareFile db = new DbCompareFile();
                try
                {
                    ret = db.GetUncomparedSupp(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetUncomparedSupp" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<List<UncomparedVills>> GetUncomparedMO(Finder finder)
        {
            ReturnsObjectType<List<UncomparedVills>> ret = new ReturnsObjectType<List<UncomparedVills>>();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetUncomparedMO(finder);
            }
            else
            {

                //DbAdmin db = new DbAdmin();
                DbCompareFile db = new DbCompareFile();
                try
                {
                    ret = db.GetUncomparedMO(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetUncomparedMO" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<List<UncomparedServs>> GetUncomparedServ(Finder finder)
        {
            ReturnsObjectType<List<UncomparedServs>> ret = new ReturnsObjectType<List<UncomparedServs>>();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetUncomparedServ(finder);
            }
            else
            {
                //DbAdmin db = new DbAdmin();
                DbCompareFile db = new DbCompareFile();
                try
                {
                    ret = db.GetUncomparedServ(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetUncomparedServ" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<List<UncomparedParTypes>> GetUncomparedParType(Finder finder)
        {
            ReturnsObjectType<List<UncomparedParTypes>> ret = new ReturnsObjectType<List<UncomparedParTypes>>();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetUncomparedParType(finder);
            }
            else
            {

                //DbAdmin db = new DbAdmin();
                DbCompareFile db = new DbCompareFile();
                try
                {
                    ret = db.GetUncomparedParType(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetUncomparedParType" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<List<UncomparedParTypes>> GetUncomparedParBlag(Finder finder)
        {
            ReturnsObjectType<List<UncomparedParTypes>> ret = new ReturnsObjectType<List<UncomparedParTypes>>();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetUncomparedParBlag(finder);
            }
            else
            {

                //DbAdmin db = new DbAdmin();
                DbCompareFile db = new DbCompareFile();
                try
                {
                    ret = db.GetUncomparedParBlag(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetUncomparedParBlag" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<List<UncomparedParTypes>> GetUncomparedParGas(Finder finder)
        {
            ReturnsObjectType<List<UncomparedParTypes>> ret = new ReturnsObjectType<List<UncomparedParTypes>>();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetUncomparedParGas(finder);
            }
            else
            {

                //DbAdmin db = new DbAdmin();
                DbCompareFile db = new DbCompareFile();
                try
                {
                    ret = db.GetUncomparedParGas(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetUncomparedParGas" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<List<UncomparedParTypes>> GetUncomparedParWater(Finder finder)
        {
            ReturnsObjectType<List<UncomparedParTypes>> ret = new ReturnsObjectType<List<UncomparedParTypes>>();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetUncomparedParWater(finder);
            }
            else
            {

                //DbAdmin db = new DbAdmin();
                DbCompareFile db = new DbCompareFile();
                try
                {
                    ret = db.GetUncomparedParWater(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetUncomparedParWater" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<List<UncomparedMeasures>> GetUncomparedMeasure(Finder finder)
        {
            ReturnsObjectType<List<UncomparedMeasures>> ret = new ReturnsObjectType<List<UncomparedMeasures>>();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetUncomparedMeasure(finder);
            }
            else
            {
                //DbAdmin db = new DbAdmin();
                DbCompareFile db = new DbCompareFile();
                try
                {
                    ret = db.GetUncomparedMeasure(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetUncomparedMeasure" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<List<UncomparedTowns>> GetUncomparedTown(Finder finder)
        {
            ReturnsObjectType<List<UncomparedTowns>> ret = new ReturnsObjectType<List<UncomparedTowns>>();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetUncomparedTown(finder);
            }
            else
            {

                //DbAdmin db = new DbAdmin();
                DbCompareFile db = new DbCompareFile();
                try
                {
                    ret = db.GetUncomparedTown(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetUncomparedTown" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<List<UncomparedRajons>> GetUncomparedRajon(Finder finder)
        {
            ReturnsObjectType<List<UncomparedRajons>> ret = new ReturnsObjectType<List<UncomparedRajons>>();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetUncomparedRajon(finder);
            }
            else
            {

                //DbAdmin db = new DbAdmin();
                DbCompareFile db = new DbCompareFile();
                try
                {
                    ret = db.GetUncomparedRajon(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetUncomparedRajon" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<List<UncomparedStreets>> GetUncomparedStreets(Finder finder)
        {
            ReturnsObjectType<List<UncomparedStreets>> ret = new ReturnsObjectType<List<UncomparedStreets>>();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetUncomparedStreets(finder);
            }
            else
            {

                //DbAdmin db = new DbAdmin();
                DbCompareFile db = new DbCompareFile();
                try
                {
                    ret = db.GetUncomparedStreets(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetUncomparedStreets" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<List<UncomparedHouses>> GetUncomparedHouse(Finder finder)
        {
            ReturnsObjectType<List<UncomparedHouses>> ret = new ReturnsObjectType<List<UncomparedHouses>>();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetUncomparedHouse(finder);
            }
            else
            {

                //DbAdmin db = new DbAdmin();
                DbCompareFile db = new DbCompareFile();
                try
                {
                    ret = db.GetUncomparedHouse(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetUncomparedHouse" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<List<UncomparedLS>> GetUncomparedLS(Finder finder)
        {
            ReturnsObjectType<List<UncomparedLS>> ret = new ReturnsObjectType<List<UncomparedLS>>();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetUncomparedLS(finder);
            }
            else
            {

                //DbAdmin db = new DbAdmin();
                DbCompareFile db = new DbCompareFile();
                try
                {
                    ret = db.GetUncomparedLS(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetUncomparedLS" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsType LinkArea(UncomparedAreas finder)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.LinkArea(finder);
            }
            else
            {

                //DbAdmin db = new DbAdmin();
                DbCompareFile db = new DbCompareFile();
                try
                {
                    ret = db.LinkArea(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LinkArea" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsType LinkSupp(UncomparedSupps finder)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.LinkSupp(finder);
            }
            else
            {
                //DbAdmin db = new DbAdmin();
                DbCompareFile db = new DbCompareFile();
                try
                {
                    ret = db.LinkSupp(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LinkSupp" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsType LinkMO(UncomparedVills finder)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.LinkMO(finder);
            }
            else
            {
                //DbAdmin db = new DbAdmin();
                DbCompareFile db = new DbCompareFile();
                try
                {
                    ret = db.LinkMO(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LinkMO" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsType LinkServ(UncomparedServs finder)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.LinkServ(finder);
            }
            else
            {
                //DbAdmin db = new DbAdmin();
                DbCompareFile db = new DbCompareFile();
                try
                {
                    ret = db.LinkServ(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LinkServ" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsType LinkParType(UncomparedParTypes finder)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.LinkParType(finder);
            }
            else
            {
                //DbAdmin db = new DbAdmin();
                DbCompareFile db = new DbCompareFile();
                try
                {
                    ret = db.LinkParType(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LinkParType" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsType LinkParBlag(UncomparedParTypes finder)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.LinkParBlag(finder);
            }
            else
            {
                //DbAdmin db = new DbAdmin();
                DbCompareFile db = new DbCompareFile();
                try
                {
                    ret = db.LinkParBlag(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LinkParBlag" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsType LinkParGas(UncomparedParTypes finder)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.LinkParGas(finder);
            }
            else
            {
                //DbAdmin db = new DbAdmin();
                DbCompareFile db = new DbCompareFile();
                try
                {
                    ret = db.LinkParGas(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LinkParGas" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsType LinkParWater(UncomparedParTypes finder)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.LinkParWater(finder);
            }
            else
            {
                //DbAdmin db = new DbAdmin();
                DbCompareFile db = new DbCompareFile();
                try
                {
                    ret = db.LinkParWater(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LinkParWater" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsType LinkMeasure(UncomparedMeasures finder)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.LinkMeasure(finder);
            }
            else
            {
                //DbAdmin db = new DbAdmin();
                DbCompareFile db = new DbCompareFile();
                try
                {
                    ret = db.LinkMeasure(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LinkMeasure" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsType LinkTown(UncomparedTowns finder)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.LinkTown(finder);
            }
            else
            {
                //DbAdmin db = new DbAdmin();
                DbCompareFile db = new DbCompareFile();
                try
                {
                    ret = db.LinkTown(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LinkTown" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsType LinkRajon(UncomparedRajons finder)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.LinkRajon(finder);
            }
            else
            {
                //DbAdmin db = new DbAdmin();
                DbCompareFile db = new DbCompareFile();
                try
                {
                    ret = db.LinkRajon(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LinkRajon" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsType LinkNzpStreet(UncomparedStreets finder)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.LinkNzpStreet(finder);
            }
            else
            {
                //DbAdmin db = new DbAdmin();
                DbCompareFile db = new DbCompareFile();
                try
                {
                    ret = db.LinkNzpStreet(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LinkNzpStreet" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsType LinkNzpDom(UncomparedHouses finder)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.LinkNzpDom(finder);
            }
            else
            {
                //DbAdmin db = new DbAdmin();
                DbCompareFile db = new DbCompareFile();
                try
                {
                    ret = db.LinkNzpDom(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LinkNzpDom" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsType AddNewArea(UncomparedAreas finder)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.AddNewArea(finder);
            }
            else
            {
                //DbAdmin db = new DbAdmin();
                DbCompareFile db = new DbCompareFile();
                try
                {
                    ret = db.AddNewArea(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка AddNewArea" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsType AddNewSupp(UncomparedSupps finder)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.AddNewSupp(finder);
            }
            else
            {
                //DbAdmin db = new DbAdmin();
                DbCompareFile db = new DbCompareFile();
                try
                {
                    ret = db.AddNewSupp(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка AddNewSupp" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsType AddNewMO(UncomparedVills finder)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.AddNewMO(finder);
            }
            else
            {
                //DbAdmin db = new DbAdmin();
                DbCompareFile db = new DbCompareFile();
                try
                {
                    ret = db.AddNewMO(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка AddNewMO" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsType AddNewServ(UncomparedServs finder)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.AddNewServ(finder);
            }
            else
            {
                //DbAdmin db = new DbAdmin();
                DbCompareFile db = new DbCompareFile();
                try
                {
                    ret = db.AddNewServ(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка AddNewServ" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsType AddNewParType(UncomparedParTypes finder)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.AddNewParType(finder);
            }
            else
            {
                //DbAdmin db = new DbAdmin();
                DbCompareFile db = new DbCompareFile();
                try
                {
                    ret = db.AddNewParType(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка AddNewParType" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsType AddNewParBlag(UncomparedParTypes finder)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.AddNewParBlag(finder);
            }
            else
            {
                //DbAdmin db = new DbAdmin();
                DbCompareFile db = new DbCompareFile();
                try
                {
                    ret = db.AddNewParBlag(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка AddNewParBlag" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsType AddNewParGas(UncomparedParTypes finder)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.AddNewParGas(finder);
            }
            else
            {
                //DbAdmin db = new DbAdmin();
                DbCompareFile db = new DbCompareFile();
                try
                {
                    ret = db.AddNewParGas(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка AddNewParGas" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsType AddNewParWater(UncomparedParTypes finder)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.AddNewParWater(finder);
            }
            else
            {
                //DbAdmin db = new DbAdmin();
                DbCompareFile db = new DbCompareFile();
                try
                {
                    ret = db.AddNewParWater(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка AddNewParWater" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsType AddNewMeasure(UncomparedMeasures finder)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.AddNewMeasure(finder);
            }
            else
            {
                //DbAdmin db = new DbAdmin();
                DbCompareFile db = new DbCompareFile();
                try
                {
                    ret = db.AddNewMeasure(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка AddNewMeasure" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsType AddNewRajon(UncomparedRajons finder)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.AddNewRajon(finder);
            }
            else
            {
                //DbAdmin db = new DbAdmin();
                DbCompareFile db = new DbCompareFile();
                try
                {
                    ret = db.AddNewRajon(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка AddNewRajon" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsType AddNewStreet(UncomparedStreets finder)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.AddNewStreet(finder);
            }
            else
            {
                //DbAdmin db = new DbAdmin();
                DbCompareFile db = new DbCompareFile();
                try
                {
                    ret = db.AddNewStreet(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка AddNewStreet" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsType AddNewHouse(UncomparedHouses finder)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.AddNewHouse(finder);
            }
            else
            {
                //DbAdmin db = new DbAdmin();
                DbCompareFile db = new DbCompareFile();
                try
                {
                    ret = db.AddNewHouse(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка AddNewHouse" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<DownloadedData> GetFileGilec(Finder finder)
        {
            ReturnsObjectType<DownloadedData> ret = new ReturnsObjectType<DownloadedData>();

            if (SrvRun.isBroker)
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

            if (SrvRun.isBroker)
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

            if (SrvRun.isBroker)
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

            if (SrvRun.isBroker)
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

            if (SrvRun.isBroker)
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

            if (SrvRun.isBroker)
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

            if (SrvRun.isBroker)
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

            if (SrvRun.isBroker)
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

            if (SrvRun.isBroker)
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

            if (SrvRun.isBroker)
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

            if (SrvRun.isBroker)
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

        public Returns AddAllHouse(FilesImported finder)
        {
            Returns ret = new Returns();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.AddAllHouse(finder);
            }
            else
            {
                //DbAdmin db = new DbAdmin();
                DbCompareFile db = new DbCompareFile();
                try
                {
                    ret = db.AddAllHouse(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка AddAllHouse" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public Returns LinkStreetAutom(FilesImported finder)
        {
            Returns ret = new Returns();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.LinkStreetAutom(finder);
            }
            else
            {
                //DbAdmin db = new DbAdmin();
                DbCompareFile db = new DbCompareFile();
                try
                {
                    ret = db.LinkStreetAutom(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LinkStreetAutom" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public Returns LinkRajonAutom(FilesImported finder)
        {
            Returns ret = new Returns();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.LinkRajonAutom(finder);
            }
            else
            {
                //DbAdmin db = new DbAdmin();
                DbCompareFile db = new DbCompareFile();
                try
                {
                    ret = db.LinkRajonAutom(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка LinkRajonAutom" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public Returns DeleteUnrelatedInfo()
        {
            Returns ret = new Returns();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.DeleteUnrelatedInfo();
            }
            else
            {
                DbAdmin db = new DbAdmin();
                try
                {
                    ret = db.DeleteUnrelatedInfo();
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка DeleteUnrelatedInfo" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public Returns UsePreviousLinks(FilesImported finder)
        {
            Returns ret = new Returns();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.UsePreviousLinks(finder);
            }
            else
            {
                var db = new DbUsePreviousLinks();
                try
                {
                    ret = db.UsePreviousLinks(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции!";
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка выполнения процедуры UsePreviousLinks" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsType UnlinkArea(ComparedAreas finder)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.UnlinkArea(finder);
            }
            else
            {
                //DbAdmin db = new DbAdmin();
                DbCompareFile db = new DbCompareFile();
                try
                {
                    ret = db.UnlinkArea(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка UnlinkArea" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsType UnlinkSupp(ComparedSupps finder)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.UnlinkSupp(finder);
            }
            else
            {
                //DbAdmin db = new DbAdmin();
                DbCompareFile db = new DbCompareFile();
                try
                {
                    ret = db.UnlinkSupp(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка UnlinkSupp" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsType UnlinkMO(ComparedVills finder)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.UnlinkMO(finder);
            }
            else
            {
                //DbAdmin db = new DbAdmin();
                DbCompareFile db = new DbCompareFile();
                try
                {
                    ret = db.UnlinkMO(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка UnlinkMO" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsType UnlinkServ(ComparedServs finder)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.UnlinkServ(finder);
            }
            else
            {
                //DbAdmin db = new DbAdmin();
                DbCompareFile db = new DbCompareFile();
                try
                {
                    ret = db.UnlinkServ(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка UnlinkServ" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsType UnlinkParType(ComparedParTypes finder)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.UnlinkParType(finder);
            }
            else
            {
                //DbAdmin db = new DbAdmin();
                DbCompareFile db = new DbCompareFile();
                try
                {
                    ret = db.UnlinkParType(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка UnlinkParType" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsType UnlinkParBlag(ComparedParTypes finder)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.UnlinkParBlag(finder);
            }
            else
            {
                //DbAdmin db = new DbAdmin();
                DbCompareFile db = new DbCompareFile();
                try
                {
                    ret = db.UnlinkParBlag(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка UnlinkParBlag" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsType UnlinkParGas(ComparedParTypes finder)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.UnlinkParGas(finder);
            }
            else
            {
                //DbAdmin db = new DbAdmin();
                DbCompareFile db = new DbCompareFile();
                try
                {
                    ret = db.UnlinkParGas(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка UnlinkParGas" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsType UnlinkParWater(ComparedParTypes finder)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.UnlinkParWater(finder);
            }
            else
            {
                //DbAdmin db = new DbAdmin();
                DbCompareFile db = new DbCompareFile();
                try
                {
                    ret = db.UnlinkParWater(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка UnlinkParWater" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsType UnlinkMeasure(ComparedMeasures finder)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.UnlinkMeasure(finder);
            }
            else
            {
                //DbAdmin db = new DbAdmin();
                DbCompareFile db = new DbCompareFile();
                try
                {
                    ret = db.UnlinkMeasure(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка UnlinkMeasure" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsType UnlinkTown(ComparedTowns finder)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.UnlinkTown(finder);
            }
            else
            {
                //DbAdmin db = new DbAdmin();
                DbCompareFile db = new DbCompareFile();
                try
                {
                    ret = db.UnlinkTown(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка UnlinkTown" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsType UnlinkRajon(ComparedRajons finder)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.UnlinkRajon(finder);
            }
            else
            {
                //DbAdmin db = new DbAdmin();
                DbCompareFile db = new DbCompareFile();
                try
                {
                    ret = db.UnlinkRajon(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка UnlinkRajon" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsType UnlinkStreet(ComparedStreets finder)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.UnlinkStreet(finder);
            }
            else
            {
                //DbAdmin db = new DbAdmin();
                DbCompareFile db = new DbCompareFile();
                try
                {
                    ret = db.UnlinkStreet(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка UnlinkStreet" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsType UnlinkHouse(ComparedHouses finder)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.UnlinkHouse(finder);
            }
            else
            {
                //DbAdmin db = new DbAdmin();
                DbCompareFile db = new DbCompareFile();
                try
                {
                    ret = db.UnlinkHouse(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка UnlinkHouse" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsType UnlinkLS(ComparedLS finder)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.UnlinkLS(finder);
            }
            else
            {
                //DbAdmin db = new DbAdmin();
                DbCompareFile db = new DbCompareFile();
                try
                {
                    ret = db.UnlinkLS(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка UnlinkLS" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<List<UncomparedAreas>> GetAreaByFilter(UncomparedAreas finder)
        {
            ReturnsObjectType<List<UncomparedAreas>> ret = new ReturnsObjectType<List<UncomparedAreas>>();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetAreaByFilter(finder);
            }
            else
            {
                //DbAdmin db = new DbAdmin();
                DbCompareFile db = new DbCompareFile();
                try
                {
                    ret = db.GetAreaByFilter(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetAreaByFilter" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<List<UncomparedSupps>> GetSuppByFilter(UncomparedSupps finder)
        {
            ReturnsObjectType<List<UncomparedSupps>> ret = new ReturnsObjectType<List<UncomparedSupps>>();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetSuppByFilter(finder);
            }
            else
            {
                //DbAdmin db = new DbAdmin();
                DbCompareFile db = new DbCompareFile();
                try
                {
                    ret = db.GetSuppByFilter(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetSuppByFilter" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<List<UncomparedVills>> GetMOByFilter(UncomparedVills finder)
        {
            ReturnsObjectType<List<UncomparedVills>> ret = new ReturnsObjectType<List<UncomparedVills>>();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetMOByFilter(finder);
            }
            else
            {
                //DbAdmin db = new DbAdmin();
                DbCompareFile db = new DbCompareFile();
                try
                {
                    ret = db.GetMOByFilter(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetMOByFilter" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<List<UncomparedServs>> GetServByFilter(UncomparedServs finder)
        {
            ReturnsObjectType<List<UncomparedServs>> ret = new ReturnsObjectType<List<UncomparedServs>>();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetServByFilter(finder);
            }
            else
            {
                //DbAdmin db = new DbAdmin();
                DbCompareFile db = new DbCompareFile();
                try
                {
                    ret = db.GetServByFilter(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetServByFilter" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<List<UncomparedParTypes>> GetParTypeByFilter(UncomparedParTypes finder)
        {
            ReturnsObjectType<List<UncomparedParTypes>> ret = new ReturnsObjectType<List<UncomparedParTypes>>();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetParTypeByFilter(finder);
            }
            else
            {
                //DbAdmin db = new DbAdmin();
                DbCompareFile db = new DbCompareFile();
                try
                {
                    ret = db.GetParTypeByFilter(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetParTypeByFilter" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<List<UncomparedParTypes>> GetParBlagByFilter(UncomparedParTypes finder)
        {
            ReturnsObjectType<List<UncomparedParTypes>> ret = new ReturnsObjectType<List<UncomparedParTypes>>();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetParBlagByFilter(finder);
            }
            else
            {
                //DbAdmin db = new DbAdmin();
                DbCompareFile db = new DbCompareFile();
                try
                {
                    ret = db.GetParBlagByFilter(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetParBlagByFilter" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<List<UncomparedParTypes>> GetParGasByFilter(UncomparedParTypes finder)
        {
            ReturnsObjectType<List<UncomparedParTypes>> ret = new ReturnsObjectType<List<UncomparedParTypes>>();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetParGasByFilter(finder);
            }
            else
            {
                //DbAdmin db = new DbAdmin();
                DbCompareFile db = new DbCompareFile();
                try
                {
                    ret = db.GetParGasByFilter(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetParGasByFilter" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<List<UncomparedParTypes>> GetParWaterByFilter(UncomparedParTypes finder)
        {
            ReturnsObjectType<List<UncomparedParTypes>> ret = new ReturnsObjectType<List<UncomparedParTypes>>();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetParWaterByFilter(finder);
            }
            else
            {
                //DbAdmin db = new DbAdmin();
                DbCompareFile db = new DbCompareFile();
                try
                {
                    ret = db.GetParWaterByFilter(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetParWaterByFilter" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<List<UncomparedMeasures>> GetMeasureByFilter(UncomparedMeasures finder)
        {
            ReturnsObjectType<List<UncomparedMeasures>> ret = new ReturnsObjectType<List<UncomparedMeasures>>();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetMeasureByFilter(finder);
            }
            else
            {
                //DbAdmin db = new DbAdmin();
                DbCompareFile db = new DbCompareFile();
                try
                {
                    ret = db.GetMeasureByFilter(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetMeasureByFilter" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<List<UncomparedTowns>> GetTownByFilter(UncomparedTowns finder)
        {
            ReturnsObjectType<List<UncomparedTowns>> ret = new ReturnsObjectType<List<UncomparedTowns>>();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetTownByFilter(finder);
            }
            else
            {
                //DbAdmin db = new DbAdmin();
                DbCompareFile db = new DbCompareFile();
                try
                {
                    ret = db.GetTownByFilter(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetTownByFilter" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<List<UncomparedRajons>> GetRajonByFilter(UncomparedRajons finder)
        {
            ReturnsObjectType<List<UncomparedRajons>> ret = new ReturnsObjectType<List<UncomparedRajons>>();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetRajonByFilter(finder);
            }
            else
            {
                //DbAdmin db = new DbAdmin();
                DbCompareFile db = new DbCompareFile();
                try
                {
                    ret = db.GetRajonByFilter(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetRajonByFilter" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<List<UncomparedStreets>> GetStreetsByFilter(UncomparedStreets finder)
        {
            ReturnsObjectType<List<UncomparedStreets>> ret = new ReturnsObjectType<List<UncomparedStreets>>();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetStreetsByFilter(finder);
            }
            else
            {
                //DbAdmin db = new DbAdmin();
                DbCompareFile db = new DbCompareFile();
                try
                {
                    ret = db.GetStreetsByFilter(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetStreetsByFilter" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsObjectType<List<UncomparedHouses>> GetHouseByFilter(UncomparedHouses finder)
        {
            ReturnsObjectType<List<UncomparedHouses>> ret = new ReturnsObjectType<List<UncomparedHouses>>();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetHouseByFilter(finder);
            }
            else
            {
                //DbAdmin db = new DbAdmin();
                DbCompareFile db = new DbCompareFile();
                try
                {
                    ret = db.GetHouseByFilter(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка GetHouseByFilter" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsType ChangeTownForRajon(UncomparedRajons finder)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.ChangeTownForRajon(finder);
            }
            else
            {
                DbCompareFile db = new DbCompareFile();
                try
                {
                    ret = db.ChangeTownForRajon(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка ChangeTownForRajon" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public ReturnsType DbSaveFileToDisassembly( FilesImported finder)
        {
            ReturnsType ret = new ReturnsType();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.DbSaveFileToDisassembly(finder);
            }
            else
            {
                #region Подключение к БД
                IDbConnection con_db = DBManager.GetConnection(Global.Constants.cons_Kernel);
                if (!DBManager.OpenDb(con_db, true).result)
                {
                    MonitorLog.WriteLog("Ошибка открытия соединения в ф-ции DbSaveFileToDisassembly ", MonitorLog.typelog.Error, true);
                    ret.text = "Ошибка при открытии соединения.";
                    ret.result = false;
                    ret.tag = -1;
                    return ret;
                }
                #endregion Подключение к БД

                DbDisassembleFile db = new DbDisassembleFile(con_db);
                try
                {
                    ret = db.DbSaveFileToDisassembly(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции загрузки";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка DbSaveFileToDisassembly" + "\n" + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                }
                db.Close();
            }
            return ret;
        }

        public Returns OperateWithFileImported(FilesDisassemble finder, FilesImportedOperations operation)
        {
            Returns ret = Utils.InitReturns();

            if (SrvRun.isBroker)
            {
                //надо продублировать вызов к внутреннему хосту
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.OperateWithFileImported(finder, operation);
            }
            else
            {
                DbDeleteImportedFile delFile = new DbDeleteImportedFile();

                try
                {
                    switch (operation)
                    {
                        case FilesImportedOperations.Delete:
                            ret = delFile.DeleteImportedFile(finder);
                            break;
                        default:
                            ret = new Returns(false, "Неверное наименование операции");
                            break;
                    }

                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции OperateWithFileImported";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug)
                        MonitorLog.WriteLog("Ошибка OperateWithFileImported\n" + ex.Message, MonitorLog.typelog.Error, 2,
                            100, true);
                }
                finally
                {
                    delFile.Close();
                }
            }
            return ret;
        }

        public Returns UploadGilec(List<int> lst)
        {
            Returns ret = new Returns();
            if (SrvRun.isBroker)
            {
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.UploadGilec(lst);
            }
            else
            {
                try
                {
                    DbAdmin db = new DbAdmin();
                    ret = db.UploadGilec(lst);
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

          


        public Returns DeleteFromExchangeSZ(Finder finder, int nzp_ex_sz)
        //----------------------------------------------------------------------
        {
             Returns ret = Utils.InitReturns();
       

            if (SrvRun.isBroker)
            {
                //надо вызвать дальше другой хост
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.DeleteFromExchangeSZ(finder, nzp_ex_sz);
            }
            else
            {
                DbAdmin db = new DbAdmin();
                try
                {
                    ret = db.DeleteFromExchangeSZ(finder, nzp_ex_sz);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = "Ошибка вызова функции";
                    //if (Constants.Viewerror) ret.text += " : " + ex.Message;
                    if (Constants.Debug) MonitorLog.WriteLog("Ошибка DeleteFromExchangeSZ() \n  " + ex.Message, MonitorLog.typelog.Error, 2, 100, true);
                   
                }
                db.Close();
            }
            return ret;
        }

        public List<AreaCodes> GetAreaCodes(AreaCodes finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<AreaCodes> res = null;

            if (SrvRun.isBroker)
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
            if (SrvRun.isBroker)
            {
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.GetMaxCodeFromAreaCodes();
            }
            else
            {
                try
                {
                    DbAdmin db = new DbAdmin();
                    ret = db.GetMaxCodeFromAreaCodes();
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
            if (SrvRun.isBroker)
            {
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.SaveAreaCodes(finder);
            }
            else
            {
                try
                {
                    DbAdmin db = new DbAdmin();
                    ret = db.SaveAreaCodes(finder);
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
            if (SrvRun.isBroker)
            {
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.DeleteAreaCodes(finder);
            }
            else
            {
                try
                {
                    DbAdmin db = new DbAdmin();
                    ret = db.DeleteAreaCodes(finder);
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
            if (SrvRun.isBroker)
            {
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.CreateSequence();
            }
            else
            {
                try
                {
                    DbAdmin db = new DbAdmin();
                    ret = db.CreateSequence();
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

            if (SrvRun.isBroker)
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

            if (SrvRun.isBroker)
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

            if (SrvRun.isBroker)
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
            if (SrvRun.isBroker)
            {
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.DeleteFromEFSReestr(finder);
            }
            else
            {
                try
                {
                    DbAdmin db = new DbAdmin();
                    ret = db.DeleteFromEFSReestr(finder);
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
        /// Подготовить данные для печати ЛС
        /// </summary>
        /// <param name="finder">объект поиска</param>        
        /// <returns>результат</returns>
        public Returns PreparePrintInvoices(List<PointForPrepare> finder)
        {
            Returns ret = new Returns();
            if (SrvRun.isBroker)
            {
                cli_Admin cli = new cli_Admin(WCFParams.AdresWcfHost.CurT_Server);
                ret = cli.PreparePrintInvoices(finder);
            }
            else
            {
                try
                {
                    DbAdmin db = new DbAdmin();
                    ret = db.PreparePrintInvoices(finder);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = ex.Message;
                    MonitorLog.WriteLog("Ошибка PreparePrintInvoices\n" + ex.Message, MonitorLog.typelog.Error, 30, 301, true);//2,100
                }
            }
            return ret;
        }

         /// <summary>
        /// Получить данные из sys_events
        /// </summary>
        /// <param name="finder">объект поиска</param>        
        /// <returns>результат</returns>
        public List<SysEvents> GetSysEvents(SysEvents finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<SysEvents> res = null;

            if (SrvRun.isBroker)
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

            if (SrvRun.isBroker)
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

            if (SrvRun.isBroker)
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
        ///получить список названий сущностей, засветившихся в sys_events
        /// </summary>
        /// <param name="finder">объект поиска</param>        
        /// <returns>результат</returns>
        public List<SysEvents> GetSysEventsEntityList(SysEvents finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<SysEvents> res = null;

            if (SrvRun.isBroker)
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

            if (SrvRun.isBroker)
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

            if (SrvRun.isBroker)
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

            if (SrvRun.isBroker)
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

            if (SrvRun.isBroker)
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

            if (SrvRun.isBroker)
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

            if (SrvRun.isBroker)
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
    }
}
