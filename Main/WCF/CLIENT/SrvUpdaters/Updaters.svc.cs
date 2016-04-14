using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using SevenZip;
using System.Net;
using STCLINE.KP50.Global;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using System.Globalization;
using STCLINE.KP50.Updater;
using System.Web.Configuration;
using STCLINE.KP50.DataBase;
using System.Configuration;
using STCLINE.KP50.SrvPatch;
using STCLINE.KP50.SrvBase;
using STCLINE.KP50.SrvUpdaters;
using System.Reflection;
using System.Diagnostics;

namespace STCLINE.KP50.SrvUpdaters
{
    // ПРИМЕЧАНИЕ. Если изменить имя класса "Service1" здесь, также необходимо обновить ссылку на класс "Service1" в файле Web.config и в связанном svc-файле.
    public class Updaters : IUpdaters
    {
        ArrayList host_list = new ArrayList();
        ArrayList web_list = new ArrayList();
        System.Timers.Timer timer = new System.Timers.Timer();
        UpData updater1 = new UpData();
        string sqlStr = "";
        bool webflag = false;
        
        Dictionary<string, string> host_info_update = new Dictionary<string, string>();
        Dictionary<string, string> web_info_update = new Dictionary<string, string>();

        public DictsClass GetDownloadPath(Dictionary<string, string> lib1, Dictionary<string, string> lib2, string raj_ip)
        {
            //чтение файла настроек для подключения к БД
            ReadConfigFile(DataBaseHead.ConfPref);

            MonitorLog.StartLog("web сервис", "Старт приложения");

            #region Настройка путей для районов(1)

            //-------------------STCLINE-------------------
            //try
            //{
            //    SevenZipCompressor.SetLibraryPath(@"C:\weboleg\bin\7z.dll");//путь к 7z.dll
            //}
            //catch (Exception exc)
            //{
            //    MonitorLog.StartLog("web сервис", "Старт приложения");
            //    MonitorLog.WriteLog("13" + exc.Message, MonitorLog.typelog.Info, true);
            //}
            //string ip = "www.stcline.ru";
            //---------------------------------------------


            //-------------------NAIL----------------------
            try
            {
                SevenZipCompressor.SetLibraryPath(@"7z.dll");
            }
            catch (Exception exc)
            {
                MonitorLog.WriteLog("13" + exc.Message, MonitorLog.typelog.Info, true);
            }
            //получение имени компьютера
            string host = System.Net.Dns.GetHostName();
            //Получение ip-адреса
            IPHostEntry hostInfo = Dns.GetHostEntry(host);
            string ip = "127.0.0.1";
            foreach (System.Net.IPAddress ipadd in hostInfo.AddressList)
            {
                if (ipadd.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    ip = ipadd.ToString();
                }
            }
           
            //---------------------------------------------


            #endregion

            

            string encryptpathkey = "zRZ1/BHciushX1T9Ks5WwdRNjgjEEGzFypuKZTmkAOE=";//ключ для шифрования пути к архиву
            string target = "";
            string version = "";

            Dictionary<string, string>.KeyCollection keys1 = lib1.Keys;
            Dictionary<string, string>.KeyCollection keys2 = lib2.Keys;
            Dictionary<string, string>.ValueCollection val2 = lib2.Values;
            int index = 0;

            foreach (string key1 in keys1)
            {

                string val1 = "";
                lib1.TryGetValue(key1, out val1);

                #region расшифрование пути скачки обновлений

                string downloadpath_host = Crypt.Decrypt(val1, (encryptpathkey + val2.ElementAt(index)));//разшифровка пути для скачки обновлений
                string filename1 = downloadpath_host.Substring(downloadpath_host.LastIndexOf('/') + 1);

                #endregion

                #region Настройка путей для районов(2)

                //-------------------STCLINE-------------------
                string targetpath1 = @"C:\web_update" + @"\" + filename1;
                string web_path = @"C:\webkp5";//папка обновления для web
                //---------------------------------------------

                //-------------------NAIL----------------------
               // string targetpath1 = @"C:\targus" + @"\" + filename1;
                //string web_path = @"D:\KOMPLAT.50\WEB\WebKomplat5";//папка обновления для web
                //string web_path = @"C:\temp_update";//папка обновления для web
                //---------------------------------------------

                #endregion

                #region копирование обновлений

                using (WebClient wc = new WebClient())
                {
                    wc.DownloadFile(downloadpath_host, targetpath1);
                }

                #endregion

                #region формирование записей для обновлений host

                if (filename1.Contains("host"))
                {

                    string str = "";

                    string last_update_part = filename1.Substring(filename1.LastIndexOf('_') + 2);
                    str = last_update_part.Substring(0, last_update_part.LastIndexOf('.'));

                    version = str;

                    UpData upd = new UpData();
                    upd.key = keys2.ElementAt(index);
                    upd.Path = targetpath1;
                    host_list.Add(upd);

                    //формирование записи в базу
                    target = @"http://" + ip + @"/web_update/" + filename1;

                    //Запись в бд о наличии обновлений
                    sqlStr += " INSERT INTO updater (typeUp, version, status, path, key, soup) values (\'" + "host" + "\' , " + version.ToString() + " , 0, \'" + target + "\', \'" + keys2.ElementAt(index) + "\', \'" + val2.ElementAt(index) + "\'); ";
                }
                #endregion

                #region формирование записей обновлений web
                else
                {
                    string str = "";

                    string last_update_part = filename1.Substring(filename1.LastIndexOf('_') + 2);
                    str = last_update_part.Substring(0, last_update_part.LastIndexOf('.'));

                    version = str;

                    UpData upd = new UpData();
                    upd.key = keys2.ElementAt(index);
                    upd.Path = targetpath1;
                    web_list.Add(upd);

                    //Запись в бд о наличии обновлений
                    sqlStr += " INSERT INTO updater (typeUp, version, status, path, key, soup, web_path) values (\'" + "web" + "\' , " + version.ToString() + " , 0, \'" + targetpath1 + "\', \'" + keys2.ElementAt(index) + "\', \'" + val2.ElementAt(index) + "\', \'" + web_path + "\'); ";
                }
                #endregion

                index++;
            }

            try
            {
                //подключение к базе данных
                //DB_Worker dbWriter = new DB_Worker("Client Locale=ru_ru.CP1251;Database=webzel;Database Locale=ru_ru.915;Server=ol_bars;UID = informix;Pwd = info", "web");
                MonitorLog.WriteLog("111" + " " + Constants.cons_Webdata, MonitorLog.typelog.Info, true);
                DB_Worker dbWriter = new DB_Worker(Constants.cons_Webdata, "web");
                string strlog = "";
                Returns rrt = dbWriter.WriteReportUpdate(null, sqlStr.ToString(), 0, ref strlog);
                
                MonitorLog.WriteLog("27"  + " " + rrt.text, MonitorLog.typelog.Info, true);
                            
            }
            catch (Exception exc)
            {
                MonitorLog.WriteLog("10" + exc.Message, MonitorLog.typelog.Error, true);
            }

            //опрашивает host на наличие новых установленных обновлений
            CreateTimer();


            while (!webflag)
            {
                System.Threading.Thread.Sleep(5000);
            }
            foreach (UpData upd in host_list)
            {
                File.Delete(upd.Path);
            }

            //формирование данных отчета

            DictsClass dc = new DictsClass();
            dc.L1 = new List<UpData2>();
            dc.L2 = new List<UpData2>();

            List<UpData> l1 = GetDownloadInfoHost();
            List<UpData> l2 = GetDownloadInfoWeb();

            if (l1 != null)
            {
                foreach (UpData up in l1)
                {
                    dc.L1.Add(new UpData2(up));
                }
            }
            if (l2 != null)
            {
                foreach (UpData up in l2)
                {
                    dc.L2.Add(new UpData2(up));
                }
            }
            return  dc;
            
        }

        public DictsClass GetUpdatesFullInfo()
        {
            //чтение файла настроек для подключения к БД
            ReadConfigFile(DataBaseHead.ConfPref);

            Returns ret = Utils.InitReturns();
            List<STCLINE.KP50.Global.UpData> host_up_list = new List<STCLINE.KP50.Global.UpData>();
            DbPatch db = new DbPatch();
            //host_up_list = db.GetUdatesDB(ref ret, Constants.cons_Webdata, null, "host");

            List<STCLINE.KP50.Global.UpData> web_up_list = new List<STCLINE.KP50.Global.UpData>();
            //web_up_list = db.GetUdatesDB(ref ret, Constants.cons_Webdata, null, "web");
            db.Close();

            DictsClass dc = new DictsClass();
            dc.L1 = new List<UpData2>();
            dc.L2 = new List<UpData2>();

            List<UpData> l1 = host_up_list;
            List<UpData> l2 = web_up_list;

            foreach (UpData up in l1)
            {
                dc.L1.Add(new UpData2(up));
            }

            foreach (UpData up in l2)
            {
                dc.L2.Add(new UpData2(up));
            }

            return dc;

        }

        #region Таймер
        //Дожидаемся остановки хоста
        public void timer_Elapsed(object source, System.Timers.ElapsedEventArgs e)
        {
            //временно останливаем таймер
            timer.Enabled = false;

            List<STCLINE.KP50.Global.UpData> host_update = new List<STCLINE.KP50.Global.UpData>();
            Returns ret = Utils.InitReturns();
           
            if (Constants.cons_Webdata != null)
            {
                MonitorLog.WriteLog(Constants.cons_Webdata.ToString(), MonitorLog.typelog.Warn, true);
                DbPatch db = new DbPatch();
                //host_update = db.GetUdatesDB(ref ret, Constants.cons_Webdata, host_list, "host");
                db.Close();
                //ArrayList host_update_full = DB_Worker.GetUdatesInfoHost(ref ret, "Client Locale=ru_ru.CP1251;Database=webzel;Database Locale=ru_ru.915;Server=ol_bars;UID = informix;Pwd = info", host_list);
                 MonitorLog.WriteLog("991", MonitorLog.typelog.Info, true);
            }
            if (host_update == null)
            {
                MonitorLog.WriteLog("69", MonitorLog.typelog.Info, true);
                //возобновляем таймер
                timer.Enabled = true;
            }
            else
            {
                timer.Enabled = false;
                MonitorLog.WriteLog("у хоста есть обновления", MonitorLog.typelog.Info, true);
                #region Обновление web - сервиса
                
                if (Constants.cons_Webdata != null)
                {
                    try
                    {
                        string[] st = new string[] { Constants.cons_Webdata.Replace(' ', '#'), Constants.Login, Constants.Password };
                        //уже не нужен
                        //STCLINE.KP50.Updater.Updater upad = new STCLINE.KP50.Updater.Updater(st, "web");
                        //upad.Update();
                        webflag = true;  
                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                    }                                 
                }

                #endregion
            }
        }
        //Создание таймера
        public void CreateTimer()
        {
            // настраиваем таймер
            timer = new System.Timers.Timer();
            timer.AutoReset = true;
            timer.Interval = 30000; //in milliseconds
            timer.Elapsed += new System.Timers.ElapsedEventHandler(timer_Elapsed);

            // включаем таймер
            timer.Enabled = true;
        }
        #endregion

        #region Чтение Config файла

        public bool ReadConfigFile(string ConfPref)
        {
            //загрузка            
            //Loger.WriteInfo("\r\n Загрузка файла настроек...");

            ////////      
            //Constants.Login = "Admin";
            //Constants.Password = "Portaltul";
            //MonitorLog.StartLog("web сервис", "ASAX file go");
            ///////
            Constants.Debug = true; //
            Constants.Viewerror = true; //режим раскрытия ошибки

            try
            {
                Constants.cons_Webdata = WebConfigurationManager.ConnectionStrings[DataBaseHead.ConfPref + "1"].ConnectionString;
                Constants.cons_User = WebConfigurationManager.ConnectionStrings[DataBaseHead.ConfPref + "2"].ConnectionString;//ConfigurationSettings.AppSettings[DataBaseHead.ConfPref + "2"];
                AdresWCF.Adres = WebConfigurationManager.ConnectionStrings[DataBaseHead.ConfPref + "3"].ConnectionString; //Adres

                //MonitorLog.WriteLog("stroka = " + Constants.cons_Webdata, MonitorLog.typelog.Info, true);

                Constants.cons_Webdata = Encryptor.Decrypt(Constants.cons_Webdata, null);
                Constants.cons_User = Encryptor.Decrypt(Constants.cons_User, null);
                AdresWCF.Adres = Encryptor.Decrypt(AdresWCF.Adres, null);
                //MonitorLog.WriteLog(AdresWCF.Adres, MonitorLog.typelog.Info, true);

                Utils.UserLogin(Constants.cons_User, ref Constants.Login, ref Constants.Password);
            }
            catch (Exception ex)
            {
                //Loger.WriteInfo("FAIL!" + "\r\nОшибка загрузки файла настроек!");
                MonitorLog.WriteLog("Ошибка загрузки файла настроек!" + "\r\n\r\n" + ex.Message, MonitorLog.typelog.Error, true);

                return false;
            }

            return true;

        }
        #endregion

        public List<STCLINE.KP50.Global.UpData> GetDownloadInfoHost()
        {
            //чтение файла настроек для подключения к БД
            ReadConfigFile(DataBaseHead.ConfPref);

            Returns ret = Utils.InitReturns();
            Dictionary<string, string> retDic = new Dictionary<string, string>();
            List<STCLINE.KP50.Global.UpData> obg = new List<STCLINE.KP50.Global.UpData>();
            DbPatch db = new DbPatch();
            //obg = db.GetUdatesDB(ref ret, Constants.cons_Webdata, host_list, "host");
            db.Close();
            
            //foreach (UpData data in obg)
            //{
            //    retDic.Add(data.Version.ToString(), data.status);
            //}

            return obg;
            //host_update_full = DB_Worker.GetUdatesDB(ref ret, Constants.cons_Webdata, host_list, "host");
            //return host_update_full;
        }


        public List<STCLINE.KP50.Global.UpData> GetDownloadInfoWeb()
        {
            //чтение файла настроек для подключения к БД
            ReadConfigFile(DataBaseHead.ConfPref);

            Returns ret = Utils.InitReturns();
            Dictionary<string, string> retDic = new Dictionary<string, string>();
            List<STCLINE.KP50.Global.UpData> obg = new List<STCLINE.KP50.Global.UpData>();
            DbPatch db = new DbPatch();
            //obg = db.GetUdatesDB(ref ret, Constants.cons_Webdata, web_list, "web");
            db.Close();

            //foreach (UpData data in obg)
            //{
            //    retDic.Add(data.Version.ToString(), data.status);
            //}

            return obg;
            //host_update_full = DB_Worker.GetUdatesDB(ref ret, Constants.cons_Webdata, host_list, "host");
            //return host_update_full;
        }


        //public ArrayList GetReportByVersionHost(string versR, string versL)
        //{
        //    DB_Worker db_host = new DB_Worker(Constants.cons_Webdata,"host");
        //    Returns ret;
        //    return db_host.GetReportByVersion(out ret, versL, versR, "host"); 
        //}

        //public ArrayList GetReportByVersionWeb(string versR, string versL)
        //{
        //    DB_Worker db_web = new DB_Worker(Constants.cons_Webdata, "web");
        //    Returns ret;
        //    return db_web.GetReportByVersion(out ret, versL, versR, "web");
        //}

        public Dictionary<string, object> GetPatchResult(ArrayList sql_str, string data_base_type, byte[] soup)
        {
            //чтение файла настроек для подключения к БД
            ReadConfigFile(DataBaseHead.ConfPref);
            cli_Patch cli = new cli_Patch();
            return cli.GoPatch(sql_str, data_base_type, soup);
        }
    }
}
