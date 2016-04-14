using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using System.ServiceModel.Activation;

using STCLINE.KP50.DataBase;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Global;
using System.Diagnostics;
using System.Collections;
using System.Net;
using System.IO;
using System.Security.Cryptography;
using System.Runtime.Serialization.Formatters.Binary;
using SevenZip;
using System.Text.RegularExpressions;
using STCLINE.KP50.Client;
using System.Runtime.InteropServices;

namespace STCLINE.KP50.Server
{
    /// <summary>
    /// Класс для определения битности системы
    /// </summary>
    public static class OSVersionInfo
    {
        #region ENUMS
        public enum SoftwareArchitecture
        {
            Unknown = 0,
            Bit32 = 1,
            Bit64 = 2
        }

        public enum ProcessorArchitecture
        {
            Unknown = 0,
            Bit32 = 1,
            Bit64 = 2,
            Itanium64 = 3
        }
        #endregion ENUMS

        #region DELEGATE DECLARATION
        private delegate bool IsWow64ProcessDelegate([In] IntPtr handle, [Out] out bool isWow64Process);
        #endregion DELEGATE DECLARATION

        #region BITS
        static public SoftwareArchitecture OSBits
        {
            get
            {
                SoftwareArchitecture osbits = SoftwareArchitecture.Unknown;

                switch (IntPtr.Size * 8)
                {
                    case 64:
                        osbits = SoftwareArchitecture.Bit64;
                        break;

                    case 32:
                        if (Is32BitProcessOn64BitProcessor())
                            osbits = SoftwareArchitecture.Bit64;
                        else
                            osbits = SoftwareArchitecture.Bit32;
                        break;

                    default:
                        osbits = SoftwareArchitecture.Unknown;
                        break;
                }

                return osbits;
            }
        }
        #endregion BITS

        #region 64 BIT OS DETECTION
        private static IsWow64ProcessDelegate GetIsWow64ProcessDelegate()
        {
            IntPtr handle = LoadLibrary("kernel32");

            if (handle != IntPtr.Zero)
            {
                IntPtr fnPtr = GetProcAddress(handle, "IsWow64Process");

                if (fnPtr != IntPtr.Zero)
                {
                    return (IsWow64ProcessDelegate)Marshal.GetDelegateForFunctionPointer((IntPtr)fnPtr, typeof(IsWow64ProcessDelegate));
                }
            }

            return null;
        }

        private static bool Is32BitProcessOn64BitProcessor()
        {
            IsWow64ProcessDelegate fnDelegate = GetIsWow64ProcessDelegate();

            if (fnDelegate == null)
            {
                return false;
            }

            bool isWow64;
            bool retVal = fnDelegate.Invoke(Process.GetCurrentProcess().Handle, out isWow64);

            if (retVal == false)
            {
                return false;
            }

            return isWow64;
        }
        #endregion 64 BIT OS DETECTION

        #region 64 BIT OS DETECTION
        [DllImport("kernel32", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        public extern static IntPtr LoadLibrary(string libraryName);

        [DllImport("kernel32", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        public extern static IntPtr GetProcAddress(IntPtr hwnd, string procedureName);
        #endregion 64 BIT OS DETECTION
    }


    public class srv_Patch : srv_Base, I_Patch
    {
        //собственно реализация метода выполняющего патч
        public Dictionary<string, object> GoPatch(ArrayList sql_array, string dataBaseType, byte[] soup)
        {
            Returns ret = Utils.InitReturns();
            Dictionary<string, object> retDic = null;
            
            if (SrvRun.isBroker)
            {
                cli_Patch cli = new cli_Patch();
                retDic = cli.GoPatch(sql_array, dataBaseType, soup);
            }
            else
            {
                try
                {
                    DbPatch dbPatch = new DbPatch();
                    retDic = dbPatch.GoPatch_DB(out ret, sql_array, dataBaseType, soup);
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка выполнения запроса Sql патча : " + ex.Message, MonitorLog.typelog.Error, true);
                    return null;
                }
            }

            return retDic;                     
        }

        // получение лога за период
        public Stream GetMonitorLog(DateTime BeginDate, DateTime EndDate)
        {
            if (SrvRun.isBroker)
            {
                cli_Patch cli = new cli_Patch();
                Stream stream = cli.GetMonitorLog(BeginDate, EndDate);
                BinaryFormatter bf = new BinaryFormatter();
                EventLogEntry[] evlog = (EventLogEntry[])bf.Deserialize(stream);
                MemoryStream Result = new MemoryStream();
                bf = new BinaryFormatter();
                bf.Serialize(Result, evlog.ToArray());
                Result.Seek(0, SeekOrigin.Begin);
                return Result;
            }
            else
            {
                try
                {
                    DbPatch dbPatch = new DbPatch();
                    List<EventLogEntry> evlog = dbPatch.GetMonitorLog_Host(BeginDate, EndDate);
                    MemoryStream Result = new MemoryStream();
                    BinaryFormatter bf = new BinaryFormatter();
                    bf.Serialize(Result, evlog.ToArray());
                    Result.Seek(0, SeekOrigin.Begin);
                    return Result;
                }
                catch
                {
                    MonitorLog.WriteLog("Ошибка получения событий Komplat50log на хосте", MonitorLog.typelog.Error, true);
                    return null;
                }
            }
        }

        // выполнение команд пседоязыка
        public void ExecSQLList(Stream List)
        {
            BinaryFormatter bf = new BinaryFormatter();
            string[] strings = (string[])bf.Deserialize(List);
            if (SrvRun.isBroker)
            {
                bf = new BinaryFormatter();
                MemoryStream ms = new MemoryStream();
                bf.Serialize(ms, strings.ToArray());
                ms.Seek(0, SeekOrigin.Begin);
                cli_Patch cli = new cli_Patch();
                cli.ExecSQLList(ms);
                return;
            }
            else
            {
                try
                {
                    DbPatch dbPatch = new DbPatch();
                    dbPatch.ExecSQLList(strings);
                    return;
                }
                catch
                {
                    MonitorLog.WriteLog("Ошибка выполнения команд псевдоязыка", MonitorLog.typelog.Error, true);
                    return;
                }
            }
        }

        public Stream GetSelect()
        {
            if (SrvRun.isBroker)
            {
                cli_Patch cli = new cli_Patch();
                Stream stream = cli.GetSelect();
                BinaryFormatter bf = new BinaryFormatter();
                byte[] bytes = (byte[])bf.Deserialize(stream);
                MemoryStream ms = new MemoryStream();
                bf = new BinaryFormatter();
                bf.Serialize(ms, bytes.ToArray());
                ms.Seek(0, SeekOrigin.Begin);
                return ms;
            }
            else
            {
                try
                {
                    DbPatch db = new DbPatch();
                    byte[] bytes = db.GetSelect();
                    MemoryStream ms = new MemoryStream();
                    BinaryFormatter bf = new BinaryFormatter();
                    bf.Serialize(ms, bytes.ToArray());
                    ms.Seek(0, SeekOrigin.Begin);
                    return ms;
                }
                catch
                {
                    MonitorLog.WriteLog("Ошибка получения результата обновления ", MonitorLog.typelog.Error, true);
                    return null;
                }
            }
        }

        public void FullUpdateStr(string UpdateFile, string UpdateMD5, int UpdateIndex, string WebPath, string pass, string passMD5)
        {
            UpdateClass[] UpdateByte = new UpdateClass[1];
            UpdateByte[0] = new UpdateClass();

            try
            {
                // скачивание файла и проверка его MD5
                string FileMD5;
                do
                {
                    try
                    {
                        WebClient wc = new WebClient();
                        UpdateByte[0].File = wc.DownloadData(UpdateFile);
                        MD5 MD5Local = new MD5CryptoServiceProvider();
                        byte[] retVal = MD5Local.ComputeHash(UpdateByte[0].File);

                        StringBuilder sb = new StringBuilder();
                        for (int i = 0; i < retVal.Length; i++)
                        {
                            sb.Append(retVal[i].ToString("x2"));
                        }
                        FileMD5 = sb.ToString();
                    }
                    catch
                    {
                        FileMD5 = "";
                        System.Threading.Thread.Sleep(60000);
                    }
                }
                while (UpdateMD5 != FileMD5);
                UpdateByte[0].Index = UpdateIndex;
                UpdateByte[0].MD5 = UpdateMD5;
                UpdateByte[0].WebPath = WebPath;
                UpdateByte[0].pass = pass;
                UpdateByte[0].passMD5 = passMD5;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка скачивания файла\n" + ex.Message, MonitorLog.typelog.Error, true);
                return;
            }

            MemoryStream ms = new MemoryStream();
            BinaryFormatter bf = new BinaryFormatter();
            try
            {   
                bf.Serialize(ms, UpdateByte.ToArray());
                ms.Seek(0, SeekOrigin.Begin);
                if (UpdateIndex < 4)
                {
                    FullUpdate(ms);
                }
                else
                {
                    UpdateUpdater(ms);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка обновления хоста " + ex.Message + ms.Length, MonitorLog.typelog.Error, true);
                return;
            }
        }

        public void FullUpdate(Stream UpdateFile)
        {
            BinaryFormatter bf = new BinaryFormatter();
            UpdateClass[] uc = new UpdateClass[1];
            try
            {
                uc = (UpdateClass[])(bf.Deserialize(UpdateFile));
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка получения файла " + ex.Message, MonitorLog.typelog.Error, true);
                return;
            }

            #region проверка пароля

            MD5 md5 = MD5.Create();
            byte[] inputBytes = Encoding.ASCII.GetBytes(uc[0].pass);
            byte[] hashBytes = md5.ComputeHash(inputBytes);

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                sb.Append(hashBytes[i].ToString("X2"));
            }

            if (sb.ToString() != uc[0].passMD5)
            {
                MonitorLog.WriteLog("Неправильный пароль к архиву", MonitorLog.typelog.Error, true);
                return;
            }

            #endregion

            string status = "1";
            MonitorLog.WriteLog("Начало процедуры обновления", MonitorLog.typelog.Info, true);

            if ((SrvRun.isBroker) && (uc[0].Index != 1))
            {
                int Index = 0;
                uc[0].Index = 0;
                DbPatch db = new DbPatch();
                UpData up = db.GetHistoryLast(Index);
                string LastUpdDate = up.date;

                MemoryStream ms = new MemoryStream();
                bf = new BinaryFormatter();
                bf.Serialize(ms, uc.ToArray());
                ms.Seek(0, SeekOrigin.Begin);

                cli_Patch cli = new cli_Patch();
                System.Threading.ThreadPool.QueueUserWorkItem(delegate(object notUsed) { cli.FullUpdate(ms); });
                System.Threading.Thread.Sleep(15000);

                #region ожидание обновления
                string NewUpdDate = LastUpdDate;
                do
                {
                    System.Threading.Thread.Sleep(2000);
                    int connect = 0;
                    do
                    {
                        try
                        {
                            cli = new cli_Patch();
                            connect = cli.CheckConn();
                        }
                        catch
                        {
                            connect = 0;
                        }
                    }
                    while (connect == 0);

                    if (connect == 1)
                    {
                        try
                        {
                            cli = new cli_Patch();
                            Stream stream = cli.GetHistoryLast(Index);
                            bf = new BinaryFormatter();
                            UpData2[] lud2 = (UpData2[])(bf.Deserialize(stream));
                            NewUpdDate = lud2[0].date;
                            status = lud2[0].status;
                        }
                        catch
                        {
                            NewUpdDate = LastUpdDate;
                        }
                    }
                }
                while (NewUpdDate == LastUpdDate);
                #endregion

                uc[0].Index = 2;
            }
            if (status == "1")
            {
                if (uc[0].Index != 1)
                {
                    if (SrvRun.isBroker)
                    {
                        uc[0].Index = 2;
                    }
                    else
                    {
                        uc[0].Index = 0;
                    }
                }
                DbPatch dbp = new DbPatch();
                dbp.FullUpdate(uc[0].File, uc[0].MD5, uc[0].Index, uc[0].WebPath, uc[0].pass, uc[0].passMD5);
                if (uc[0].Index != 1)
                {
                    Console.WriteLine("Остановка хостинга для обновления через 2 сек...");
                    System.Threading.Thread.Sleep(2000);
                    SrvRun.TaskStop();
                    Process.GetCurrentProcess().Kill();
                }
            }
            else
            {
                uc[0].Index = 0;
                cli_Patch cli = new cli_Patch();
                cli.RestoreFromBackup(uc[0].WebPath);
                DbPatch db = new DbPatch();
                db.AddUpdateInfo(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), "broker", "5");
            }
        }

        public void UpdateUpdater(Stream UpdateFile)
        {
            BinaryFormatter bf = new BinaryFormatter();
            UpdateClass[] uc = new UpdateClass[1];
            try
            {
                uc = (UpdateClass[])(bf.Deserialize(UpdateFile));
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка получения файла " + ex.Message, MonitorLog.typelog.Error, true);
                return;
            }

            string status = "1";
            MonitorLog.WriteLog("Начало процедуры обновления", MonitorLog.typelog.Info, true);

            if (SrvRun.isBroker)
            {

                int Index = 4;
                uc[0].Index = 4;
                DbPatch db = new DbPatch();
                UpData up = db.GetHistoryLast(Index);
                string LastUpdDate = up.date;

                MemoryStream ms = new MemoryStream();
                bf = new BinaryFormatter();
                bf.Serialize(ms, uc.ToArray());
                ms.Seek(0, SeekOrigin.Begin);

                cli_Patch cli = new cli_Patch();
                System.Threading.ThreadPool.QueueUserWorkItem(delegate(object notUsed) { cli.UpdateUpdater(ms); });
                System.Threading.Thread.Sleep(15000);

                string NewUpdDate = LastUpdDate;
                do
                {
                    System.Threading.Thread.Sleep(2000);
                    int connect = 0;
                    do
                    {
                        try
                        {
                            cli = new cli_Patch();
                            connect = cli.CheckConn();
                        }
                        catch
                        {
                            connect = 0;
                        }
                    }
                    while (connect == 0);

                    if (connect == 1)
                    {
                        try
                        {
                            cli = new cli_Patch();
                            Stream stream = cli.GetHistoryLast(Index);
                            bf = new BinaryFormatter();
                            UpData2[] lud2 = (UpData2[])(bf.Deserialize(stream));
                            NewUpdDate = lud2[0].date;
                            status = lud2[0].status;
                        }
                        catch
                        {
                            NewUpdDate = LastUpdDate;
                        }
                    }
                }
                while (NewUpdDate == LastUpdDate);
                uc[0].Index = 5;
            }
            if (status == "1")
            {
                DbPatch dbp = new DbPatch();
                dbp.UpdateUpdater(uc[0].File, uc[0].Index);
            }
        }

        public void RemoveBackupFiles(string WebPath)
        {
            if (SrvRun.isBroker)
            {
                cli_Patch cli = new cli_Patch();
                cli.RemoveBackupFiles(WebPath);
            }
            DbPatch db = new DbPatch();
            db.RemoveBackupFiles(new DirectoryInfo(Directory.GetCurrentDirectory()));
            if (Directory.Exists(WebPath))
            {
                db.RemoveBackupFiles(new DirectoryInfo(WebPath));
            }
        }

        public void RestoreFromBackup(string WebPath)
        {
            if (SrvRun.isBroker)
            {
                cli_Patch cli = new cli_Patch();
                cli.RestoreFromBackup(WebPath);
            }
            DbPatch db = new DbPatch();
            if (SrvRun.isBroker)
            {
                db.RestoreFromBackup(WebPath,2);
            }
            else
            {
                db.RestoreFromBackup(string.Empty,0);
            }
            Console.WriteLine("Остановка хостинга для восстановления через 2 сек...");
            System.Threading.Thread.Sleep(2000);
            SrvRun.TaskStop();
            Process.GetCurrentProcess().Kill();
        }

        public Stream GetHistoryFull()
        {
            if (SrvRun.isBroker)
            {
                try
                {
                    cli_Patch cli = new cli_Patch();
                    if (cli.CheckConn() == 1)
                    {
                        cli = new cli_Patch();
                        Stream stream_host = cli.GetHistoryFull();
                        BinaryFormatter formatter = new BinaryFormatter();
                        UpData2[] upd_host = (UpData2[])(formatter.Deserialize(stream_host));

                        DbPatch dbPatch = new DbPatch();
                        Stream stream_broker = dbPatch.GetHistoryFull();
                        UpData2[] upd_broker = (UpData2[])(formatter.Deserialize(stream_broker));

                        UpData2[] upd_full = new UpData2[upd_host.Length + upd_broker.Length];
                        for (int i = 0; i < upd_host.Length; i++)
                        {
                            upd_full[i] = upd_host[i];
                        }
                        for (int i = 0; i < upd_broker.Length; i++)
                        {
                            upd_full[upd_host.Length + i] = upd_broker[i];
                        }

                        MemoryStream stream = new MemoryStream();
                        formatter.Serialize(stream, upd_full.ToArray());
                        stream.Seek(0, SeekOrigin.Begin);
                        return stream;
                    }
                    else
                    {
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка обновления 2 " + ex.Message, MonitorLog.typelog.Error, true);
                    return null;
                }
            }
            else
            {
                try
                {
                    DbPatch dbPatch = new DbPatch();
                    Stream stream = dbPatch.GetHistoryFull();
                    return stream;
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка обновления 3 " + ex.Message, MonitorLog.typelog.Error, true);
                    return null;
                }
            }
        }

        public Stream GetHistoryLast(int count)
        {
            try
            {
                DbPatch dbPatch = new DbPatch();
                UpData ud = dbPatch.GetHistoryLast(count);
                //Stream stream = dbPatch.GetHistoryLast(count);
                List<UpData2> lup2 = new List<UpData2>();
                lup2.Add(new UpData2(ud));

                MemoryStream Result = new MemoryStream();
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(Result, lup2.ToArray());
                Result.Seek(0, SeekOrigin.Begin);
                return Result;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка обновления 4 " + ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }
        }

        public int CheckConn()
        {
            string pathToUpdater = Path.Combine(Environment.CurrentDirectory, "UPDATER_EXE"); // путь к updater_exe

            if (SrvRun.isBroker)
            {
                try
                {
                    #region Проверка локального файла 7z.dll
                    if (Directory.Exists(pathToUpdater))
                    {
                        bool ok = false;
                        if (File.Exists(Path.Combine(pathToUpdater, "7z.dll")))
                        {
                            string result = "";
                            using (FileStream fs = System.IO.File.OpenRead(Path.Combine(pathToUpdater, "7z.dll")))
                            {
                                MD5 md5 = new MD5CryptoServiceProvider();
                                byte[] fileData = new byte[fs.Length];
                                fs.Read(fileData, 0, (int)fs.Length);
                                byte[] checkSum = md5.ComputeHash(fileData);
                                result = BitConverter.ToString(checkSum).Replace("-", String.Empty);
                            }
                            if (((OSVersionInfo.OSBits == OSVersionInfo.SoftwareArchitecture.Bit32) && (result == "42EDF51C86E726F00379CCBDAD2BC796")) ||
                                ((OSVersionInfo.OSBits == OSVersionInfo.SoftwareArchitecture.Bit64) && (result == "5F50F725EE7759A9F1D8F6430A2C1FDA")))
                            {
                                ok = true;
                            }
                            else
                            {
                                ok = false;
                            }
                        }

                        if (!ok)
                        {
                            try
                            {
                                if (File.Exists(Path.Combine(pathToUpdater, "7z.dll")))
                                {
                                    File.Delete(Path.Combine(pathToUpdater, "7z.dll"));
                                }
                                WebClient wc = new WebClient();
                                if (OSVersionInfo.OSBits == OSVersionInfo.SoftwareArchitecture.Bit32)
                                {
                                    wc.DownloadFile(@"http://www.stcline.ru/source/32bit/7z.dll", Path.Combine(pathToUpdater, "7z.dll"));
                                }
                                else
                                {
                                    wc.DownloadFile(@"http://www.stcline.ru/source/64bit/7z.dll", Path.Combine(pathToUpdater, "7z.dll"));
                                }
                            }
                            catch
                            {
                                return 4;
                            }
                        }
                    }
                    else
                    {
                        return 4;
                    }
                    #endregion

                    #region Проверка подключения к хосту
                    cli_Patch cli = new cli_Patch();
                    int check = cli.CheckConn();
                    #endregion

                    #region Замена 7z.dll на хосте (если требуется)
                    if ((check > 1) && (check < 4))
                    {
                        byte[] File7z;
                        try
                        {
                            WebClient wc = new WebClient();
                            if (check == 2)
                            {
                                File7z = wc.DownloadData(@"http://www.stcline.ru/source/32bit/7z.dll");
                            }
                            else
                            {
                                File7z = wc.DownloadData(@"http://www.stcline.ru/source/64bit/7z.dll");
                            }
                        }
                        catch
                        {
                            return 5;
                        }
                        cli = new cli_Patch();
                        check = cli.Replace7zdll(File7z) ? 1 : 6;
                    }
                    #endregion

                    return check;
                }
                catch
                {
                    return 0;
                }
            }  // not broker
            else
            {
                if (Directory.Exists(pathToUpdater))
                {
                    #region Проверка локального файла 7z.dll
                    bool ok = false;
                    if (File.Exists(Path.Combine(pathToUpdater, "7z.dll")))
                    {
                        string result = "";
                        using (FileStream fs = System.IO.File.OpenRead(Path.Combine(pathToUpdater, "7z.dll")))
                        {
                            MD5 md5 = new MD5CryptoServiceProvider();
                            byte[] fileData = new byte[fs.Length];
                            fs.Read(fileData, 0, (int)fs.Length);
                            byte[] checkSum = md5.ComputeHash(fileData);
                            result = BitConverter.ToString(checkSum).Replace("-", String.Empty);
                        }
                        if (((OSVersionInfo.OSBits == OSVersionInfo.SoftwareArchitecture.Bit32) && (result == "42EDF51C86E726F00379CCBDAD2BC796")) ||
                            ((OSVersionInfo.OSBits == OSVersionInfo.SoftwareArchitecture.Bit64) && (result == "5F50F725EE7759A9F1D8F6430A2C1FDA")))
                        {
                            ok = true;
                        }
                        else
                        {
                            ok = false;
                        }
                    }

                    if (ok)
                    {
                        return 1;
                    }
                    else
                    {
                        return (OSVersionInfo.OSBits == OSVersionInfo.SoftwareArchitecture.Bit32) ? 2 : 3;
                    }
                    #endregion
                }
                else
                {
                    return 7;
                }
            }
        }

        public bool ExecSQLFile(Stream SQLStream)
        {
            try
            {
                BinaryFormatter bf = new BinaryFormatter();
                string[] SQLList = (string[])bf.Deserialize(SQLStream);

                string webXXX = "";
                Regex regex = new Regex(@"(?i)(?<=database[\s]*=)[\w\d]*?(?=[\s]*;)");
                Match match = regex.Match(Constants.cons_Webdata);
                if (match.Success)
                {
                    webXXX = match.Value;
                }
                else
                {
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения SQL файла " + ex.Message, MonitorLog.typelog.Error, true);
                return false;
            }
        }

        public DateTime GetCurrentMount()
        {
            Returns ret = Utils.InitReturns();
            cli_Sprav cli = new cli_Sprav(WCFParams.AdresWcfHost.CurT_Server);
            _PointWebData p = new _PointWebData(false);
            Points.PointList = cli.PointLoad(out ret, out p);
            DateTime Result = new DateTime(p.calcMonth.year_, p.calcMonth.month_, 1);
            return Result;
        }

        public bool Replace7zdll(byte[] File7z)
        {
            try
            {
                string pathToFile = Path.Combine(Path.Combine(Environment.CurrentDirectory, "UPDATER_EXE"), "7z.dll");
                if (File.Exists(pathToFile))
                {
                    File.Delete(pathToFile);
                }
                FileStream Writer = File.Open(pathToFile, FileMode.Append, FileAccess.Write, FileShare.None);
                Writer.Write(File7z, 0, File7z.Length);
                Writer.Close();
                Writer.Dispose();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool RestartHosting()
        {
            try
            {
                if (SrvRun.isBroker)
                {
                    cli_Patch cli = new cli_Patch();
                    return cli.RestartHosting();
                }
                else
                {
                    MonitorLog.WriteLog("Запущен перезапуск хостинга", MonitorLog.typelog.Info, true);
                    SrvRun.TaskStop();
                    SrvRun.TaskStarting();
                    MonitorLog.WriteLog("Хостинг успешно перезапущен", MonitorLog.typelog.Info, true);
                    return true;
                }
            }
            catch(Exception ex)
            {
                MonitorLog.WriteLog("Ошибка перезапуска хостинга: " + ex.Message, MonitorLog.typelog.Error, true);
                return false;
            }
        }
    }


    #region Класс таймера для обновления
    //-------------------------------------класс Таймера обновления-----------------------------------------------------------------
    public static class TimerUpdater
    {
        public static bool FlagUdate = false;
        public static System.Timers.Timer timer;

        public static void CreateTimer()
        {
            // настраиваем таймер
            timer = new System.Timers.Timer();
            timer.AutoReset = true;
            timer.Interval = 10000; //in milliseconds
            timer.Elapsed += new System.Timers.ElapsedEventHandler(timer_Elapsed);

            // включаем таймер
            timer.Enabled = true;
        }

        //Дожидаемся остановки хоста
        public static void timer_Elapsed(object source, System.Timers.ElapsedEventArgs e)
        {
            //временно приостанавливаем таймер
            timer.Enabled = false;

            Returns ret = Utils.InitReturns();
            DbPatch dbPatch = new DbPatch();

            if (dbPatch.CheckForUpdates(out ret, Constants.cons_Webdata, "host").Count != 0)
            {
                try
                {
                    timer.Enabled = false;

                    Console.WriteLine("Хост остановлен для обновления!");
                    FlagUdate = true;

                    //получение id процесса  host             
                    int PID = Process.GetCurrentProcess().Id;

                    string LoginLog = Constants.Login;
                    string LoginPas = Constants.Password;
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(@"UPDATER_EXE\KP50.Updater.exe", Constants.cons_Webdata.Replace(' ', '€') + "$" + PID + "$" + LoginLog + "$" + LoginPas)); //@"D:\work_Oleg\KOMPLAT.50\UDATER\Updater 3.5\Updater 3.5\Updater 3.5\bin\Debug\Updater 3.5.exe", Constants.cons_Webdata.Replace(' ', '#')));                
                    //Остановка Хоста
                    MonitorLog.Close("Остановка хостинга для обновления");
                    SrvRun.TaskStop();
                    Process.GetCurrentProcess().Kill();
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);              
                }
            }
            else
            {
                //Console.WriteLine("wait for udates...");
                //возобновляем таймер
                timer.Enabled = true;
            }
        }
    }
    //-------------------------------------------------------------------------------------------------------------------------------------------------
    #endregion
}
