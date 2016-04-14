using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using STCLINE.KP50.Global;
namespace STCLINE.KP50.Updater
{
    public static class Loger
    {       
        private static int CountSymbol = 90;

        //Процедура создания лога
        public static bool CreateLog(string RootFolder,string UpdateIndex, ref string LOGERSTR)
        {

           
                if (! logExist(ref LOGERSTR))
                {
                    LOGERSTR = "";
                    //try
                    //{
                    //    System.Threading.Thread.Sleep(3000);
                    //    File.CreateText(MainPathLog).Close();
                    //}
                    //catch (Exception exp)
                    //{
                    //    MonitorLog.WriteLog("24" + exp.Message + "/r/n/r/n" + exp.StackTrace, MonitorLog.typelog.Error, true);
                    //    return false;
                    //}
                    //StreamWriter sw = new StreamWriter(MainPathLog, true, Encoding.GetEncoding("windows-1251"));
                    //sw.WriteLine("");
                    LOGERSTR = "\r\n";
                    for (int i = 0; i < CountSymbol; i++)
                    {
                        //sw.Write('*');
                        LOGERSTR += "*";
                    }
                    //sw.WriteLine("\r\nОбновление " + RootFolder);
                    LOGERSTR += "\r\nДата создания : " + DateTime.Now;
                    //sw.WriteLine("Дата создания : " + DateTime.Now);
                    LOGERSTR += "\r\nИмя компьютера : " + System.Net.Dns.GetHostName();
                    //sw.WriteLine("Имя компьютера : " + System.Net.Dns.GetHostName());
                    LOGERSTR += "\r\nТекущий пользователь : " + Environment.UserDomainName + @"\" + Environment.UserName;
                    //sw.WriteLine("Текущий пользователь : " + Environment.UserDomainName + @"\" + Environment.UserName);
                    for (int i = 0; i < CountSymbol; i++)
                    {
                        //sw.Write('*');
                        LOGERSTR += "*";
                    }
                    //sw.WriteLine("\n\n\n");
                    LOGERSTR += "\r\n\r\n\r\n";
                    //Информация о версиях сборок
                    for (int i = 0; i < CountSymbol; i++)
                    {
                        //sw.Write('*');
                        LOGERSTR += "*";
                    }
                    if (UpdateIndex != "1")
                    {                        
                       //sw.WriteLine(Loger.GetAssemblyInformation());                                             
                        LOGERSTR += "\r\n\r\n" + Loger.GetAssemblyInformation();
                    }

                    for (int i = 0; i < CountSymbol; i++)
                    {
                        //sw.Write('*');
                        LOGERSTR += "*";
                    }

                    //sw.Flush();
                    //sw.Close();

                    return true;
                }
                else
                {
                    return true;
                }
            
           
        }

        public static void WriteInfo(string info, ref string LOGSTR)
        {
            try
            {
                if (!logExist(ref LOGSTR))
                {
                    Loger.CreateLog("---", "web", ref LOGSTR);
                }
                if (info != "")
                {
                    //StreamWriter sw = new StreamWriter(MainPathLog, true, Encoding.GetEncoding("windows-1251"));// (MainPathLog, true);  
                    //sw.WriteLine(info);
                    LOGSTR += "\r\n" + info;
                    //sw.Flush();
                    //sw.Close();
                }
            }
            catch (Exception)
            {
               
            }
        }
        
        //проверка существования файла
        public static bool logExist( ref string LOGSTR)
        {            
            //return  File.Exists(MainPathLog);
            if (LOGSTR == "")
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        //Добавление "--"
        public static string AddLine(string Expression)
        {
            try
            {
                string str = "";
                int M = (int)Math.Round(CountSymbol / 2d);
                for (int i = 0; i < M; i++)
                {
                    str += '-';
                }
                str += Expression;
                int N = CountSymbol - str.Length;
                for (int i = 0; i < N; i++)
                {
                    str += '-';
                }
                return str;
            }
            catch(Exception)
            {
                return Expression;
            }
        }
        //Удаление файла лога
        public static bool ClearLog(string RootFolder , ref string LOGSTR)
        {
            try
            {
                //System.Threading.Thread.Sleep(3000);
                //File.Delete(MainPathLog);
                LOGSTR = "";
                Loger.CreateLog(RootFolder,"web", ref LOGSTR);

                return true;
            }
            catch (Exception ex)
            {
                WriteInfo("При попытке очистить лог произошла ошибка :" + ex.Message,ref LOGSTR);
                return false;
            }
        }

        //Получение информации о версиях сборках
        public static string GetAssemblyInformation()
        {
            string retString = "";
            Version ver;
            Assembly assem;
            AssemblyName assemName;

            //Получение версии ОС
            OperatingSystem os = Environment.OSVersion;
            ver = os.Version;
            retString += "\r\nВерсия ОС: " + os.VersionString + ver.ToString();

            //Версия CLR
            ver = Environment.Version;
            retString += "\r\nВерсия CLR: " + ver.ToString();

            //Получене версии текущего приложения
            //получить ссылку на объект Assembly, который представляет исполняемый файл приложения 
            assem = Assembly.GetEntryAssembly();
            //извлекает его номер версии
            assemName = assem.GetName();
            ver = assemName.Version;

            retString += "\r\n Приложение :" + assemName.Name + "; Версия : " + ver.ToString();

            ////Извлечение версии текущей сборки
            ////получить ссылку на объект Assembly, который представляет текущую сборку
            //assem = Assembly.GetExecutingAssembly();
            //assemName = assem.GetName();
            //ver = assemName.Version;
            //retString += "\r\n" + assemName.Name + " Версия сборки : " + ver.ToString();

            ////Извлечение версии определенной сборки
            ////Цикл по всем файлам 
            //DirectoryInfo DirDll = new DirectoryInfo(@"D:\work_Oleg\KOMPLAT.50\WCF\HOST\AutoHost\bin\Debug");
            //FileInfo[] Dllfiles = DirDll.GetFiles("*.dll");
            //for (int i = 0; i < Dllfiles.Length; i++)
            //{
            //    try
            //    {
            //        //с помощью метода Assembly.ReflectionOnlyLoadFrom получить ссылку на объект Assembly
            //        assem = Assembly.ReflectionOnlyLoadFrom(Dllfiles[i].FullName);
            //        assemName = assem.GetName();
            //        ver = assemName.Version;
            //        retString += "\r\n\r\n" + assemName.Name + " Версия  сборки : " + ver.ToString();
            //    }
            //    catch (Exception exc)
            //    {                    
            //        retString += "\r\n\r\n" + "Ошибка определения версии файла " + Dllfiles[i].FullName + "; ошибка : " + exc.Message;
            //    }
            //}

            return retString;
        }

        public static void DelLOg(ref string LOGSTR)
        {
            if (logExist(ref LOGSTR))
            {
                //System.Threading.Thread.Sleep(3000);
                //File.Delete(Loger.MainPathLog);
                LOGSTR = "";
            }
        }
    }
}
