using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Timers;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;

namespace STCLINE.KP50.Updater
{
    public class Updater
    {
        #region Свойства
        //Таймер опроса хоста
        public Timer timer;
        //Аргументы при запуске
        string[] Mainargs;
        //Флаг Хост снова запущен
        public bool HostReStart = false;
        //флаг конца обновления
        public bool UpdateEnd = false;
        
        //Путь - название того, что обновляем @"D:\work_Oleg\KOMPLAT.50"
        public string TargetNamePath;
        //Путь к директории, куда скачиваются обновления @"D:\Test_Komplat_update\UpdateFrom7z\";
        public string DestPath;
        //Путь того, что обновляем @"D:\work_Oleg\KOMPLAT.50"
        public string TargetPath;
        //Название папки, куда распаковываются обновления(ЗАДАТЬ КОНСТ?) "KOMPLAT.50"
        public string UpdateDirectoryName;
        //Путь к Host.exe @"D:\work_Oleg\KOMPLAT.50\WCF\HOST\AutoHost\bin\Debug\STCLINE.KP50.Host.exe"
        public string HostPath;
        //Лог текстового файла(ВРЕМЕННО! Потом прямая запись в базу)
        public string TextLogPath = @"C:\UPDlog.txt";//@"D:\work_Oleg\KOMPLAT.50\UDATER\UPDlog.txt";
        //Тип обновления(host или web)
        public string typeUp; 
     
        //Строка отчет!
        string LOGER_STRING;

        //путь к распакованным обновлениям
        string PathUpdate;
        //путь к веб
        string PathWeb;
        //путь к цели обновления
        string PathTarget;
        //что обновляем? 0-хост, 1-веб, 2-брокер
        string UpdateIndex;
        //если просто восстановить из резервной копии
        string RestoreOnly;
        //строка регулярного выражения для поиска имени бд
        private string DBRegEx = @"(?i)(?<=database\s*=\s*)[\w\d]*?(?=[\s]*;)";


        #endregion

        public Updater(string[] Arguments)
        {
            // Mainargs[1] = PID процесса
            this.LOGER_STRING = "";
            this.Mainargs = Arguments[0].Split(new char[] { '♂' });
            Constants.cons_Webdata = Mainargs[0].Replace('€', ' ');
            Constants.Login = Mainargs[2];
            Constants.Password = Mainargs[3];
            PathUpdate = Mainargs[4].Replace('€',' ');
            PathWeb = Mainargs[5].Replace('€',' ');
            UpdateIndex = Mainargs[6];
            RestoreOnly = Mainargs[7];
            //установка пути к цели обновления
            if (UpdateIndex == "1")
            {
                PathTarget = PathWeb; //если обновляется веб
            }
            else
            {
                PathTarget = Directory.GetCurrentDirectory();//обновляется хост или брокер
            }
        }

        #region Таймер
        //Дожидаемся остановки хоста
        public void timer_Elapsed(object source, System.Timers.ElapsedEventArgs e)
        {
            if (UpdateIndex != "1")
            {
                if (CheckNameProcess())
                {
                    //Console.WriteLine("Хост остановлен!");
                    timer.Enabled = false;
                    if (RestoreOnly != "1")
                    {
                        Update();
                    }
                    else
                    {
                        Restore();
                    }
                }
                else
                {
                    Console.WriteLine("wait...");
                }
            }
            else
            {
                timer.Enabled = false;
                if (RestoreOnly != "1")
                {
                    Update();
                }
                else
                {
                    Restore();
                }
            }
        }
        //Создание таймера
        public void CreateTimer()
        {            
            // настраиваем таймер
            timer = new System.Timers.Timer();
            timer.AutoReset = true;
            timer.Interval = 2000; //in milliseconds
            timer.Elapsed += new System.Timers.ElapsedEventHandler(timer_Elapsed);

            // включаем таймер
            timer.Enabled = true;
        }

        //Получение процессов
        public bool CheckNameProcess()
        {
            Process[] p = Process.GetProcesses();
            for (int i = 0; i < p.Length; i++)
            {
                if (p[i].Id.ToString() == Mainargs[1])
                {                   
                    return false;
                }
            }
            return true;      
        }
        #endregion 

        //Процедура обновления
        //public void Update(string[] arguments)
        public void Update()
        {
            if (Directory.Exists(this.PathTarget) && Directory.Exists(this.PathUpdate))
            {
                //тестовый счетчик(завалить обновление на tempCount шаге)
                //int tempCount = 0;            

                this.LOGER_STRING = "";
                Returns ret = Utils.InitReturns();

                //Создание отчета(заголовок)
                try
                {
                    Loger.CreateLog(this.PathTarget, this.UpdateIndex, ref this.LOGER_STRING);
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("16_1" + ex.Message, MonitorLog.typelog.Error, true);
                }

                //Объект для работы с БД
                DB_Worker dbW = new DB_Worker(Constants.cons_Webdata, this.UpdateIndex);

                UpData upd = new UpData();
                DbPatch db = new DbPatch();
                upd = db.GetHistoryLast(int.Parse(this.UpdateIndex));

                //Объект для выполнения операций с файлами
                Finder f = new Finder(this.PathTarget, this.PathUpdate);

                bool SqlSuccess = true; // результат выполнения скрипта

                #region Поиск файлов .sql, их выполнение и удаление, а также удаление .config файлов
                DirectoryInfo D = new DirectoryInfo(this.PathUpdate);
                FileInfo[] Files = D.GetFiles();
                foreach (FileInfo aFile in Files)
                {
                    ArrayList SqlArray = new ArrayList();
                    if (aFile.Extension == ".sql")
                    {
                        if (UpdateIndex == "0")
                        {
                            ////Поиск базы данных в строке подключения
                            string webXXX = "";
                            Regex regex = new Regex(DBRegEx);
                            Match match = regex.Match(Constants.cons_Webdata);
                            if (match.Success)
                            {
                                webXXX = match.Value;
                            }

                            StreamReader SReader = new StreamReader(aFile.FullName, Encoding.Default);
                            string SqlLine;
                            while ((SqlLine = SReader.ReadLine()) != null)
                            {
                                if (SqlLine.IndexOf("database webXXX") != -1)
                                {
                                    SqlLine = "database " + webXXX + ";";
                                }
                                SqlArray.Add(SqlLine);
                            }
                            SReader.Close();
                            Console.WriteLine("Запуск SQL скрипта...");
                            Loger.WriteInfo(Loger.AddLine("Запуск SQL скрипта..."), ref this.LOGER_STRING);
                            try
                            {
                                DbPatch b = new DbPatch();
                                b.GoScript_DB(out ret, SqlArray, Constants.cons_Webdata);
                                if (!ret.result)
                                {
                                    Console.WriteLine("Ошибка выполнения SQL скрипта!");
                                    Loger.WriteInfo("\r\nОшибка выполнения SQL скрипта!", ref this.LOGER_STRING);
                                    //dbW.WriteReportUpdate(upd, "", 7, ref this.LOGER_STRING);
                                    dbW.AddUpdate(PathUpdate, PathWeb, UpdateIndex, 7, ref LOGER_STRING);
                                    SqlSuccess = false;
                                    //UpdateAboreded = true;
                                }
                                else
                                {
                                    Console.WriteLine("SQL скрипт успешно выполнен!");
                                    Loger.WriteInfo("\r\nSQL скрипт успешно выполнен!", ref this.LOGER_STRING);
                                    SqlSuccess = true;
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("Ошибка выполнения SQL скрипта!");
                                Loger.WriteInfo("\r\nОшибка выполнения SQL скрипта!" + ex.Message, ref this.LOGER_STRING);
                                //dbW.WriteReportUpdate(upd, "", 7, ref this.LOGER_STRING);
                                dbW.AddUpdate(PathUpdate, PathWeb, UpdateIndex, 7, ref LOGER_STRING);
                                SqlSuccess = false;
                                //UpdateAboreded = true;
                            }
                        }
                        aFile.Delete();
                    }
                    else if (aFile.Extension == ".config")
                    {
                        File.SetAttributes(aFile.FullName, FileAttributes.Normal);
                        aFile.Delete();
                    }
                }
                #endregion

                //Обновление

                if (SqlSuccess)
                {
                    #region Обновление
                    bool FlagError = false;
                    try
                    {
                        DbPatch dbp = new DbPatch();
                        dbp.RemoveBackupFiles(new DirectoryInfo(this.PathTarget));
                        dbp.RemoveBackupFiles(new DirectoryInfo(this.PathUpdate));
                        Console.WriteLine("Попытка обновления...");
                        Loger.WriteInfo(Loger.AddLine("Старт процедуры обновления"), ref this.LOGER_STRING);

                        f.CopyAndCreateBuckup(PathUpdate, PathTarget, UpdateIndex, ref this.LOGER_STRING);

                        Console.WriteLine("Обновление успешно завершено!");
                        Loger.WriteInfo("\r\nОбновление успешно завершено!", ref this.LOGER_STRING);
                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog("18" + ex.Message, MonitorLog.typelog.Error, true);
                        FlagError = true;
                        //UpdateAboreded = true;

                        Console.WriteLine("Ошибка обновления");
                        Loger.WriteInfo("\r\nОшибка обновления." + ex.Message + " Попытка восстановления из резервной копии...", ref this.LOGER_STRING);

                    }
                    #endregion

                    if (!FlagError)
                    {
                        #region Удаление папки с обновлением
                        Directory.Delete(this.PathUpdate, true);
                        Loger.WriteInfo("\r\n\r\nУдаление папки с обновлением завершено", ref LOGER_STRING);

                        File.Delete(Directory.GetCurrentDirectory() + @"\DirsPath" + UpdateIndex + ".arl");
                        File.Delete(Directory.GetCurrentDirectory() + @"\FilePath" + UpdateIndex + ".arl");

                        StreamWriter sw = new StreamWriter(Directory.GetCurrentDirectory() + @"\DirsPath" + UpdateIndex + ".arl");
                        foreach (string dirstr in f.Dir_wasAdded)
                        {
                            sw.WriteLine(dirstr);
                        }
                        sw.Close();

                        sw = new StreamWriter(Directory.GetCurrentDirectory() + @"\FilePath" + UpdateIndex + ".arl");
                        foreach (string filstr in f.Fil_wasAdded)
                        {
                            sw.WriteLine(filstr);
                        }
                        sw.Close();

                        Loger.WriteInfo(Loger.AddLine("Попытка записи в базу"), ref this.LOGER_STRING);//   "\r\n\r\n       ***-=Попытка записи в базу=-***");
                        //string str = "\r\nЗапись в банку : " + dbW.WriteReportUpdate(upd, "", 1, ref this.LOGER_STRING).text;
                        string str = "\r\nЗапись в банку : " + dbW.AddUpdate(PathUpdate, PathWeb, UpdateIndex, 1, ref LOGER_STRING).text;
                        Console.WriteLine(str);
                        Loger.WriteInfo(str, ref this.LOGER_STRING);

                        //restart iis
                        if (UpdateIndex == "1")
                        {
                            System.Diagnostics.Process.Start("cmd.exe", "/C iisreset /noforce");
                        }
                        #endregion
                    }
                    else
                    {
                        #region
                        bool FlagError2 = false;
                        try
                        {
                            Console.WriteLine("***Обновление завершилось с ошибкой!***");
                            Console.WriteLine("\nПопытка восстановления...");
                            Loger.WriteInfo(Loger.AddLine("Обновление завершилось с ошибкой!"), ref this.LOGER_STRING);
                            Loger.WriteInfo("Попытка восстановления...", ref this.LOGER_STRING);
                            f.Restore(this.PathTarget, UpdateIndex, ref LOGER_STRING);
                            Console.WriteLine("Восстановление успешно заврешено!");
                            Loger.WriteInfo("\r\nВосстановление успешно заврешено!", ref this.LOGER_STRING);

                        }
                        catch (Exception ex)
                        {
                            MonitorLog.WriteLog("22" + ex.Message, MonitorLog.typelog.Error, true);
                            //ошибка отката
                            FlagError2 = true;
                            Console.WriteLine("Ошибка отката");
                            Loger.WriteInfo("\r\nОшибка отката", ref this.LOGER_STRING);
                        }
                        //Откат - ОК(Обновлене завершилось с ошибкой, но откат успешен status = 2)
                        if (!FlagError2)
                        {
                            Loger.WriteInfo("\r\nОткат успешно зевершен. Попытка записи в базу.", ref this.LOGER_STRING);
                            //string str = "Запись в банку : " + dbW.WriteReportUpdate(upd, "", 2, ref this.LOGER_STRING).text;
                            string str = "Запись в банку : " + dbW.AddUpdate(PathUpdate, PathWeb, UpdateIndex, 2, ref LOGER_STRING).text;
                            Loger.WriteInfo(str, ref this.LOGER_STRING);
                            Console.WriteLine(str);
                        }
                        //И обновление и откат сломались(status = 3)
                        else
                        {
                            Loger.WriteInfo("\r\nОткат завершился неудачей. Требуется вмешательство.", ref this.LOGER_STRING);
                            Loger.WriteInfo("\r\nПопытка записи в базу...", ref this.LOGER_STRING);
                            //DB_Worker dbW = new DB_Worker(arguments[0].Replace('#', ' '));
                            //string str = "Запись в банку : " + dbW.WriteReportUpdate(upd, "", 3, ref this.LOGER_STRING).text;
                            string str = "Запись в банку : " + dbW.AddUpdate(PathUpdate, PathWeb, UpdateIndex, 3, ref LOGER_STRING).text;
                            Loger.WriteInfo(str, ref this.LOGER_STRING);
                            Console.WriteLine(str);
                        }
                        #endregion
                    }
                }
                //Скрипт не выполнился
                else
                {
                    //UpdateAboreded = true;

                    Loger.WriteInfo(Loger.AddLine("Попытка записи в базу"), ref this.LOGER_STRING);
                    //string str = "\r\nЗапись в банку : " + dbW.WriteReportUpdate(upd, "", 3, ref this.LOGER_STRING).text;
                    string str = "Запись в банку : " + dbW.AddUpdate(PathUpdate, PathWeb, UpdateIndex, 3, ref LOGER_STRING).text;
                    Console.WriteLine(str);
                    Loger.WriteInfo(str, ref this.LOGER_STRING);
                } //end if (SqlSuccess)

                Loger.ClearLog(this.TextLogPath, ref this.LOGER_STRING);

                try
                {
                    //удаление лога
                    Loger.DelLOg(ref this.LOGER_STRING);
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("24" + ex.Message, MonitorLog.typelog.Error, true);
                }

                //запуск Хоста или Брокера
                if (this.UpdateIndex != "1")
                {
                    if (this.UpdateIndex == "0")
                    {
                        // Хоста
                        System.Diagnostics.Process.Start(this.PathTarget + @"\KP50.Host.exe");//@"D:\work_Oleg\KOMPLAT.50\WCF\HOST\AutoHost\bin\Debug\STCLINE.KP50.Host.exe");
                    }
                    else
                    {
                        // Брокера
                        System.Diagnostics.Process.Start(this.PathTarget + @"\KP50.Broker.exe");
                    }
                }
            }
            // не найдено папок с обновлениями
            else
            {
                DB_Worker dbW = new DB_Worker(Constants.cons_Webdata, this.UpdateIndex);
                LOGER_STRING = "Не найден, по крайней мере, один из путей обновления: \"" + this.PathTarget + "\" или \"" + this.PathUpdate + "\".";
                dbW.AddUpdate(PathUpdate, PathWeb, UpdateIndex, 8, ref LOGER_STRING);
                Console.WriteLine("\r\nНе найдены пути для обновления. Обновление не произведено.\r\nИсточник: " + PathUpdate + "\r\nПриемник: " + PathTarget);
                MonitorLog.WriteLog("\r\nНе найдены пути для обновления. Обновление не произведено.\r\nИсточник: " + PathUpdate + "\r\nПриемник: " + PathTarget, MonitorLog.typelog.Error, true);
            }
            UpdateEnd = true;
        } //end Update()

        public void Restore()
        {
            Console.WriteLine("Restore begin...");
            Finder f = new Finder(PathTarget, "");
            string temp = "";
            if (!PathTarget.Contains("UPDATER_EXE"))
            {
                f.RestoreBAK(PathTarget, ref temp);
            }
            Console.WriteLine("Restore end.");

            //запуск Хоста или Брокера
            if (this.UpdateIndex != "1")
            {
                if (this.UpdateIndex == "0")
                {
                    // Хоста
                    System.Diagnostics.Process.Start(this.PathTarget + @"\KP50.Host.exe");//@"D:\work_Oleg\KOMPLAT.50\WCF\HOST\AutoHost\bin\Debug\STCLINE.KP50.Host.exe");
                }
                else
                {
                    // Брокера
                    System.Diagnostics.Process.Start(this.PathTarget + @"\KP50.Broker.exe");
                }
            }
            UpdateEnd = true;
        }
    }
}
