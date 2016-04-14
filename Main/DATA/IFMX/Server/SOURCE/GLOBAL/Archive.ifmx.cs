using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Win32;
using System.Data;
using STCLINE.KP50.Global;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Text.RegularExpressions;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.DataBase
{
    public class DbArchive : DataBaseHead
    {
        private string 
            INFORMIXDIR      = "", 
            INFORMIXSERVER   = "", 
            INFORMIXSQLHOSTS = "", 
            ONCONFIG         = "",
            PATH             = "",
            DBTEMP           = "",
            DB_LOCALE        = "set DB_LOCALE=RU_RU.8859-5",
            DBMONEY          = "set DBMONEY=.",
            DBDATE           = "set DBDATE=DMY4.",
            CLIENT_LOCALE    = "set CLIENT_LOCALE=RU_RU.8859-5",
            SERVER_LOCALE    = "set SERVER_LOCALE=RU_RU.8859-5";

        private string CONNECTSERVER, CONNECTDATABASE;

        public void MakeArchive()
        {
            IDbConnection conn = GetConnection(Constants.cons_Kernel);
            Returns ret = OpenDb(conn, true);
            if (!ret.result) return;

            CONNECTSERVER = DBManager.getServer(conn);
            CONNECTDATABASE = conn.Database;

            string DB_REAL_SERVER = "";
            string DB_Server = DBManager.getServer(conn);

            RegistryKey rootKey = Registry.LocalMachine;
            RegistryKey subkey = rootKey.OpenSubKey(@"\Software\Informix\OnLine");
            string FileCMD = Path.Combine(Path.GetTempPath(), @"regreg.cmd");
            string FileLOG = Path.Combine(Path.GetTempPath(), @"reg.shu");

            #region Считывание, в зависимости от версии Informix'а и битности ОС
            try
            {
                if (subkey != null)
                {
                    try
                    {
                        DB_REAL_SERVER = subkey.GetSubKeyNames()[0];
                    }
                    catch
                    {
                        DB_REAL_SERVER = "*";
                    }
                }

                subkey = rootKey.OpenSubKey(@"\Software\Informix\OnLine\" + CONNECTSERVER + @"\Environment");
                if (subkey != null)
                {
                    if (subkey.GetValue("INFORMIXDIR") != null)
                    {
                        INFORMIXDIR = "set INFORMIXDIR=" + (string)subkey.GetValue("INFORMIXDIR");
                        PATH = "set PATH = " + INFORMIXDIR + @"\bin;%PATH%";
                        DBTEMP = "set DBTEMP = " + INFORMIXDIR + @"\infxtmp";
                    }
                    if (subkey.GetValue("INFORMIXSERVER") != null)
                        INFORMIXSERVER = "set INFORMIXSERVER=" + (string)subkey.GetValue("INFORMIXSERVER");
                    if (subkey.GetValue("INFORMIXSQLHOSTS") != null)
                        INFORMIXDIR = "set INFORMIXSQLHOSTS=" + (string)subkey.GetValue("INFORMIXSQLHOSTS");
                    if (subkey.GetValue("ONCONFIG") != null)
                        INFORMIXDIR = "set ONCONFIG=" + (string)subkey.GetValue("ONCONFIG");
                }
                else
                {
                    subkey = rootKey.OpenSubKey(@"\Software\Informix\OnLine\" + DB_REAL_SERVER + @"\Environment");
                    if (subkey != null)
                    {
                        if (subkey.GetValue("INFORMIXDIR") != null)
                        {
                            INFORMIXDIR = "set INFORMIXDIR=" + (string)subkey.GetValue("INFORMIXDIR");
                            PATH = "set PATH = " + INFORMIXDIR + @"\bin;%PATH%";
                            DBTEMP = "set DBTEMP = " + INFORMIXDIR + @"\infxtmp";
                        }
                        if (subkey.GetValue("INFORMIXSERVER") != null)
                            INFORMIXSERVER = "set INFORMIXSERVER=" + (string)subkey.GetValue("INFORMIXSERVER");
                        if (subkey.GetValue("INFORMIXSQLHOSTS") != null)
                            INFORMIXSQLHOSTS = "set INFORMIXSQLHOSTS=" + (string)subkey.GetValue("INFORMIXSQLHOSTS");
                        if (subkey.GetValue("ONCONFIG") != null)
                            ONCONFIG = "set ONCONFIG=" + (string)subkey.GetValue("ONCONFIG");
                    }
                    else
                    {
                        // 64 bit OS
                        File.Delete(FileCMD);
                        using (StreamWriter sw = new StreamWriter(FileCMD))
                        {
                            sw.WriteLine(Path.Combine(Environment.SystemDirectory, @"reg.exe") + @" EXPORT HKEY_LOCAL_MACHINE\SOFTWARE\Informix\OnLine " + FileLOG + @" /y /reg:64");
                        }
                        ProcessStartInfo psi = new ProcessStartInfo();
                        psi.FileName = FileCMD;
                        //psi.Arguments = @"\c " + FileCMD;
                        Process p = Process.Start(psi);

                        while (Process.GetProcesses().FirstOrDefault(x => x.Id == p.Id) != null)
                        {
                            Thread.Sleep(100);
                        }

                        try
                        {
                            using (StreamReader sr = new StreamReader(FileLOG))
                            {
                                bool bReadEnvironment = false;
                                string str = "";
                                while ((str = sr.ReadLine()) != null)
                                {
                                    if ((str.Trim() == "") && (bReadEnvironment)) break;
                                    str = str.Replace('\u0010'.ToString(), "");
                                    str = str.Replace('\u0013'.ToString(), "");
                                    str = str.Replace(" ", "");
                                    str = str.Replace("[", "");
                                    str = str.Replace("]", "");

                                    if (bReadEnvironment)
                                    {
                                        str = str.Replace("\"", "");
                                        str = str.Replace(@"\\", @"\");
                                        if (str.Contains("="))
                                        {
                                            if (str.Substring(0, str.IndexOf("=")) == "INFORMIXDIR")
                                            {
                                                INFORMIXDIR = "set INFORMIXDIR=" + str.Replace("INFORMIXDIR=", "");
                                                DBTEMP = "set DBTEMP=" + str.Replace("INFORMIXDIR=", "") + @"\infxtmp";
                                                PATH = "set PATH=" + str.Replace("INFORMIXDIR=", "") + @"\bin;%PATH%;";
                                            }
                                            if (str.Substring(0, str.IndexOf("=")) == "INFORMIXSERVER")
                                                INFORMIXSERVER = "set " + str;
                                            if (str.Substring(0, str.IndexOf("=")) == "ONCONFIG")
                                                ONCONFIG = "set " + str;
                                        }
                                    }

                                    if (str.Contains("Environment"))
                                    {
                                        str = str.Replace(@"HKEY_LOCAL_MACHINE\SOFTWARE\Informix\OnLine\", "");
                                        str = str.Replace(@"\Environment", "");
                                        DB_REAL_SERVER = str;
                                        bReadEnvironment = true;
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                        }
                    }
                }
            } // end try
            finally
            {
                rootKey.Close();
                if (subkey != null) subkey.Close();
            }
            #endregion

            string INFDISK = INFORMIXDIR.Length > 18 ? INFORMIXDIR.Substring(16, 2) : "";
            File.Delete(FileCMD);
            File.Delete(FileLOG);

            //string SysAdminBaseUser = connBld.UID;
            //string SysAdminBasePass = connBld.Pwd;

            //bool bSysAdminBaseExists = false;
            string FileArch = Path.Combine(Path.GetTempPath(), @"goarch.cmd");
            //string FileDELSQL = Path.Combine(Path.GetTempPath(), @"deldel.sql");
            File.Delete(FileArch);
            using (StreamWriter sw = new StreamWriter(FileArch))
            {
                sw.WriteLine(INFORMIXDIR);
                sw.WriteLine(ONCONFIG);
                sw.WriteLine(INFORMIXSQLHOSTS);
                sw.WriteLine(INFORMIXSERVER);
                sw.WriteLine(DBTEMP);
                sw.WriteLine(DBMONEY);
                sw.WriteLine(DBDATE);
                sw.WriteLine(CLIENT_LOCALE);
                sw.WriteLine(DB_LOCALE);
                sw.WriteLine(SERVER_LOCALE);
                sw.WriteLine("dbexport " + Points.Pref + "_kernel@" + DB_Server);
                sw.WriteLine("exit");
            }

            string FileOut = Path.Combine(Path.GetTempPath(), "dbexport.out");
            if (File.Exists(FileOut)) File.Delete(FileOut);

            MonitorLog.WriteLog("Начало архивации.", MonitorLog.typelog.Info, true);
            ProcessStartInfo psi2 = new ProcessStartInfo();
            psi2.FileName = FileArch;
            psi2.WorkingDirectory = Path.GetTempPath();
            Process p2;

            bool retry = true;
            do
            {
                p2 = Process.Start(psi2);

                while (Process.GetProcesses().FirstOrDefault(x => x.Id == p2.Id) != null)
                {
                    Thread.Sleep(100);
                }

                if (!File.Exists(FileOut)) break;

                using (StreamReader sr = new StreamReader(FileOut))
                {
                    string s1 = sr.ReadLine();
                    string s2 = sr.ReadLine();

                    MonitorLog.WriteLog(retry.ToString() + Environment.NewLine + s1 + Environment.NewLine + s2 + Environment.NewLine, MonitorLog.typelog.Info, true);
                    if ((s1 == null) || (s2 == null)) retry = false;
                    else if (!s1.Contains("-425") || !s2.Contains("-107")) retry = false;
                    MonitorLog.WriteLog(retry.ToString() + Environment.NewLine + !s1.Contains("-425") + Environment.NewLine + !s2.Contains("-107") + Environment.NewLine, MonitorLog.typelog.Info, true);
                }

                if (retry) {
                    //File.Delete(FileOut);
                    FileOut += "1";
                    Thread.Sleep(10000);
                }
            }
            while (retry);

            MonitorLog.WriteLog("Архивация закончена. Код архивации: " + p2.ExitCode, MonitorLog.typelog.Info, true);
        }
    }
}
