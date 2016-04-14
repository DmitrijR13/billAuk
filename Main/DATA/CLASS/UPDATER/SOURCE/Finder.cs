using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections;

using STCLINE.KP50.Global;
//Для скачки обновлений
using System.Net;
using System.Threading;
//Для распаковки
using SevenZip;
using System.Security.AccessControl;
using System.Security.Principal;

namespace STCLINE.KP50.Updater
{
    class Finder
    {
        //Путь KOMPLAT 5.0
        public string PathKomplat;
        //Путь к обновлениям
        public string PathUpdate;
        //Список добавленных каталогов
        public ArrayList Dir_wasAdded;
        //Список добавленных файлов
        public ArrayList Fil_wasAdded;
        //Ключ для расшифровки
        private const string encryptzipkey = "6KaZpzqRHNh4JtIlcZw7OCfOV1h5vdP30izIHzk/jBQ=";

        //Конструктор
        public Finder(string PathK , string PathU)
        {
            this.PathKomplat = PathK;
            this.PathUpdate = PathU;
            this.Dir_wasAdded = new ArrayList();
            this.Fil_wasAdded = new ArrayList();
        }

        //Добавление новых элементов - Главная процедура
        public void Scaner(string PathUpdate, string PathKomplat ,ref string LOGER_STR)
        {
            string LogString = "";
            
            //Получение подкаталогов и файлов обновления
            DirectoryInfo Main_Dir_Update = new DirectoryInfo(PathUpdate);
            DirectoryInfo[] Sub_dir_upd = Main_Dir_Update.GetDirectories();
            FileInfo[] Sub_files_upd = Main_Dir_Update.GetFiles();

            //Получение подкаталогов и файлов Комплата
            DirectoryInfo DirKomplat = new DirectoryInfo(PathKomplat);
            DirectoryInfo[] SdirKomplat = DirKomplat.GetDirectories();
            FileInfo[] KomplatFiles = DirKomplat.GetFiles();
           
            //Проверка на каталоги(переименнование лишних)       
            for (int j = 0; j < SdirKomplat.Length; j++)
            {
                bool flagExist = false;
                for (int i = 0; i < Sub_dir_upd.Length; i++)
                {
                    if (SdirKomplat[j].Name == Sub_dir_upd[i].Name)
                    {
                        flagExist = true;
                        break;
                    }
                }
                //директория не обнаружена -> переименнуем
                if (flagExist == false)
                {                   
                    //SdirKomplat[j].Delete(true);
                    SdirKomplat[j].MoveTo(SdirKomplat[j].FullName + ".bak");                   
                    Console.WriteLine(SdirKomplat[j].FullName + " УСПЕШНО СОЗДАНА РЕЗЕРВНАЯ КОПИЯ(КАТАЛОГ)");
                    LogString += "\r\n\r\nЛишняя директория : " + SdirKomplat[j].FullName + " УСПЕШНО СОЗДАНА РЕЗЕРВНАЯ КОПИЯ(КАТАЛОГ)";                    
                }
            }         

            //Проверка на каталоги(добавление новых)                                     
            for (int i = 0; i < Sub_dir_upd.Length; i++)
            {
                bool flagExist = false;
                for (int j = 0; j < SdirKomplat.Length; j++)
                {
                    if (SdirKomplat[j].Name == Sub_dir_upd[i].Name)
                    {
                        flagExist = true;
                        //Console.WriteLine(SdirKomplat[j].Name + " существует...");
                        break;
                    }
                }
                //Директории не сущ -> добавляем
                if (flagExist == false)
                {
                    Sub_dir_upd[i].MoveTo(DirKomplat.FullName + @"\" + Sub_dir_upd[i].Name);
                    Dir_wasAdded.Add(DirKomplat.FullName + @"\" + Sub_dir_upd[i].Name);
                    Console.WriteLine(DirKomplat.FullName + @"\" + Sub_dir_upd[i].Name + " *****ДОБАВЛЕН*****");
                    LogString += "\r\n\r\nДобавлена новая директория : " + DirKomplat.FullName + @"\" + Sub_dir_upd[i].Name;
                }
            }

            //обновляем список каталогов
            Sub_dir_upd = Main_Dir_Update.GetDirectories();


            //***Удаление лишних файлов _ ссылаясь на текстовый док***
            if (File.Exists(Main_Dir_Update + @"\$FilesMustDel.txt"))
            {
                //Удаляем файлы по путям из текстового дока
                StreamReader sr = new StreamReader(Main_Dir_Update + @"\$FilesMustDel.txt");
                string PathBase = this.PathKomplat;//
                while (!sr.EndOfStream)
                {
                    string pathDel = sr.ReadLine();
                    //пропуск пустых строк
                    while (pathDel == "")
                    {
                        pathDel = sr.ReadLine();
                    }
                    try
                    {
                        if (pathDel != null && File.Exists(PathBase + pathDel.Trim().Substring(pathDel.Trim().LastIndexOf('\\'))))
                        {
                            File.Delete(PathBase + pathDel.Trim().Substring(pathDel.Trim().LastIndexOf('\\')));

                            LogString += "\r\n\r\n" + PathBase + pathDel.Trim().Substring(pathDel.Trim().LastIndexOf('\\')) + " удален";
                        }
                    }
                    catch(Exception ex)
                    {
                        LogString += "\r\n\r\n" + "Ошибка удаления файла. " + ex.Message;
                    }
                }
                sr.Close();

                try
                {
                    //удаление текстового дока
                    File.Delete(Main_Dir_Update + @"\$FilesMustDel.txt");
                }
                catch(Exception ex)
                {
                    LogString += "\r\n\r\n" + "Ошибка удаления файла - списка $FilesMustDel.txt " + ex.Message;
                }
            }


            //Копирование и замена новых файлов
            for (int i = 0; i < Sub_files_upd.Length; i++)
            {
                if (Sub_files_upd[i].Name != "$FilesMustDel.txt")
                {
                    string path_ = DirKomplat.FullName + @"\" + Sub_files_upd[i].Name;
                    Sub_files_upd[i].CopyTo(path_, true);
                    Fil_wasAdded.Add(path_);
                    Console.WriteLine(path_ + " добавлен!");
                    LogString += "\r\n\r\n" + path_ + " добавлен";
                }
            }

            Loger.WriteInfo(LogString, ref LOGER_STR);
          
            //рекурсивно запускаем            
            for (int i = 0; i < Sub_dir_upd.Length; i++)
            {               
               Scaner(Sub_dir_upd[i].FullName, DirKomplat.FullName + @"\" + Sub_dir_upd[i].Name, ref LOGER_STR);                           
            }
        }//EndScaner

        //удаление резервных копий
        public void DeleterOldFilesAndFolders(string KomplatPath, ref string LOGSTR)
        {
            string LogString = "";

            //Получение файлов Комплата
            DirectoryInfo DirKomplat = new DirectoryInfo(KomplatPath);
            DirectoryInfo[] SdirKomplat = DirKomplat.GetDirectories();
            FileInfo[] KomplatFiles = DirKomplat.GetFiles();

            //Удаление файлов содержащих '.bak'
            for (int i = 0; i < KomplatFiles.Length; i++)
            {
                if (KomplatFiles[i].FullName.Substring(KomplatFiles[i].FullName.Length - 4) == ".bak")
                {
                    string fname = KomplatFiles[i].FullName;
                    KomplatFiles[i].Delete();
                    Console.WriteLine(fname + " удален...");
                    LogString += "\r\n\r\n" + fname + " удален...";
                }
            }

            //удаление директорий .bak
            for (int i = 0; i < SdirKomplat.Length; i++)
            {
                if (SdirKomplat[i].FullName.Substring(SdirKomplat[i].FullName.Length - 4) == ".bak")
                {
                    string dirname = SdirKomplat[i].FullName;
                    SdirKomplat[i].Delete(true);
                    Console.WriteLine(SdirKomplat[i].FullName + "  удален(каталог)" );
                    LogString += "\r\n\r\n" + SdirKomplat[i].FullName + "  удален(каталог)";
                }
            }            
            SdirKomplat = DirKomplat.GetDirectories(); //обновление списка директорий

            Loger.WriteInfo(LogString, ref LOGSTR);

            //Рекурсивный запуск
            for (int i = 0; i < SdirKomplat.Length; i++)
            {
                DeleterOldFilesAndFolders(SdirKomplat[i].FullName, ref LOGSTR);
            }
        }

        //откат назад(предполагается что резервная копия успешно создана)
        public void GoBack(string KomplatPath, ref string LOGSTR)
        {
            string LogString = "";

            //Получение файлов Комплата
            DirectoryInfo DirKomplat = new DirectoryInfo(KomplatPath);
            DirectoryInfo[] SdirKomplat = DirKomplat.GetDirectories();
            FileInfo[] KomplatFiles = DirKomplat.GetFiles();
            
            //Восстановление каталогов            
            for (int i = 0; i < SdirKomplat.Length; i++)
            {
                if (SdirKomplat[i].FullName.Substring(SdirKomplat[i].FullName.Length - 4) == ".bak")
                {                   
                    SdirKomplat[i].MoveTo(SdirKomplat[i].FullName.Substring(0,SdirKomplat[i].FullName.Length - 4));
                    LogString += "\r\n\r\n Восстановлен каталог : " + SdirKomplat[i].FullName;
                }
            }

            

            //Восстановление файлов           
            for (int i = 0; i < KomplatFiles.Length; i++)
            {
                //Если .bak то разименовываем
                if (KomplatFiles[i].FullName.Substring(KomplatFiles[i].FullName.Length - 4) == ".bak")
                {
                    KomplatFiles[i].MoveTo(KomplatFiles[i].FullName.Substring(0, KomplatFiles[i].FullName.Length - 4));
                    LogString += "\r\n\r\nВосстановлен файл: " + KomplatFiles[i].FullName;
                }

            }
            SdirKomplat = DirKomplat.GetDirectories();//обновление списка директорий
            Loger.WriteInfo(LogString, ref LOGSTR);
            //рекурсивно запускаем
            for (int i = 0; i < SdirKomplat.Length; i++)
            {
                GoBack(SdirKomplat[i].FullName, ref LOGSTR);
            }
        }
        //Перед тем как делать откат удалить все добавочные файлы
        public void Del_update_files(ref string LOGSTR)
        {
            string LogString = "";
            //Удаление добавочных каталогов
            LogString += "\r\n\r\nУдаление добавочных каталогов...";
            foreach (string di in Dir_wasAdded)
            {
                DirectoryInfo Del_directory = new DirectoryInfo(di);
                Del_directory.Delete(true);
            }
            LogString += "OK";
            //Удаление добавочных файлов
            LogString += "\r\n\r\nУдаление добавочных файлов...";
            foreach (string fi in Fil_wasAdded)
            {
                FileInfo Del_file = new FileInfo(fi);
                Del_file.Delete();
            }
            LogString += "OK";
            //Удаление списков
            this.Dir_wasAdded.Clear();
            this.Fil_wasAdded.Clear();

            Loger.WriteInfo(LogString, ref LOGSTR);
        }

        //Создание резервных копий файлов
        public void CreateBackUp(string PathUpdate, string KomplatPath, ref string LOGSTR)
        {          
            //Лог
            string LogString = "";

            //Получение подкаталогов и файлов обновления
            DirectoryInfo Main_Dir_Update = new DirectoryInfo(PathUpdate);
            DirectoryInfo[] Sub_dir_upd = Main_Dir_Update.GetDirectories();
            FileInfo[] Sub_files_upd = Main_Dir_Update.GetFiles();

            //Получение файлов Комплата
            DirectoryInfo DirKomplat = new DirectoryInfo(KomplatPath);
            DirectoryInfo[] SdirKomplat = DirKomplat.GetDirectories();
            FileInfo[] KomplatFiles = DirKomplat.GetFiles();

            //LogString += "Сканирование каталогов: \n";
            //LogString += "Источник обновления : " + PathUpdate + "\n";
            //LogString += "Цель обновления : " + KomplatPath + "\n";

            for (int i = 0; i < Sub_files_upd.Length; i++)
            {
                for (int j = 0; j < KomplatFiles.Length; j++)
                {
                    //одинаковые имена файлов - > создаем резервную копию
                    if (KomplatFiles[j].Name == Sub_files_upd[i].Name)
                    {
                        KomplatFiles[j].MoveTo(KomplatFiles[j].FullName + ".bak");
                        //Атрибуты в нормал
                        File.SetAttributes(KomplatFiles[j].FullName, FileAttributes.Normal);                            
                        Console.WriteLine(KomplatFiles[j].FullName + " создана резервная копия.");
                        LogString += "\r\n\r\n" + KomplatFiles[j].FullName + " создана резервная копия.";
                    }
                }
            }

            Loger.WriteInfo(LogString, ref LOGSTR);

            //Рекурсивно запускаем
            for (int i = 0; i < Sub_dir_upd.Length; i++)
            {
                //проверка на существование директории в КОПМПЛАТЕ
                if (Directory.Exists(DirKomplat.FullName + @"\" + Sub_dir_upd[i].Name))
                {
                    CreateBackUp(Sub_dir_upd[i].FullName, DirKomplat.FullName + @"\" + Sub_dir_upd[i].Name, ref LOGSTR);
                }
            }   
        }

        // создание 
        public void CopyAndCreateBuckup(string PathUpdate, string PathTarget, string UpdateIndex, ref string LOGSTR)
        {
            //string LogString = "";

            //Получение всех паппок и файлов из папки с обновлением
            DirectoryInfo PathUpdateDI = new DirectoryInfo(PathUpdate);
            DirectoryInfo[] PathUpdateDirs = PathUpdateDI.GetDirectories();
            FileInfo[] PathUpdateFiles = PathUpdateDI.GetFiles();

            //рекурсивный проход по папкам в папке с обновлением и добавление новых папок
            foreach (DirectoryInfo dir in PathUpdateDirs)
            {
                if (!Directory.Exists(Path.Combine(PathTarget, dir.Name)))
                {
                    Dir_wasAdded.Add(Path.Combine(PathTarget, dir.Name));
                    Directory.CreateDirectory(Path.Combine(PathTarget, dir.Name));
                    LOGSTR += "\r\n\r\nКаталог \"" + dir.Name + "\" успешно создан.";
                }
                CopyAndCreateBuckup(dir.FullName, Path.Combine(PathTarget, dir.Name), UpdateIndex, ref LOGSTR);
            }

            //Создание резервных копий заменяемых файлов
            foreach (FileInfo file in PathUpdateFiles)
            {
                // чтобы не копировались ненужные файлы хоста для брокера и наоборот
                if (!((((file.Name.ToLower().Contains("host")) || (file.Name.ToLower().Contains("hostman"))) && (UpdateIndex == "2")) || ((file.Name.ToLower().Contains("broker")) && (UpdateIndex == "0"))))
                {
                    if (File.Exists(Path.Combine(PathTarget, file.Name)))
                    {
                        File.SetAttributes(Path.Combine(PathTarget, file.Name), FileAttributes.Normal);
                        File.Move(Path.Combine(PathTarget, file.Name), Path.Combine(PathTarget, file.Name) + ".bak");
                        File.Move(file.FullName, Path.Combine(PathTarget, file.Name));

                        #region Изменение владельца файла

                        string pathIntern = Path.Combine(PathTarget, file.Name);
                        try
                        {
                            DirectoryInfo diIntern = new DirectoryInfo(pathIntern);
                            DirectorySecurity dsecIntern = diIntern.GetAccessControl();
                            IdentityReference newUser = new NTAccount(Environment.UserDomainName + "\\" + Environment.UserName);
                            dsecIntern.SetOwner(newUser);
                            FileSystemAccessRule permissions = new FileSystemAccessRule(newUser, FileSystemRights.FullControl, AccessControlType.Allow);
                            dsecIntern.AddAccessRule(permissions);
                            diIntern.SetAccessControl(dsecIntern);
                        }
                        catch (Exception ex)
                        {
                            LOGSTR += "Ошибка изменения владельца файла" + Environment.NewLine + pathIntern + Environment.NewLine + ex.Message;
                        }

                        try
                        {
                            FileInfo fInfo = new FileInfo(pathIntern);
                            if (fInfo.Exists)
                            {
                                FileSecurity fSec = fInfo.GetAccessControl();
                                fSec.SetAccessRuleProtection(false, false);
                                fInfo.SetAccessControl(fSec);
                            }
                        }
                        catch (Exception ex)
                        {
                            LOGSTR += "Ошибка изменения безопасности файла" + Environment.NewLine + pathIntern + Environment.NewLine + ex.Message;
                        }

                        #endregion

                        LOGSTR += "\r\n\r\nФайл \"" + Path.Combine(PathTarget, file.Name) + "\" успешно заменен и создана его резервная копия.";
                    }
                    else
                    {
                        file.MoveTo(Path.Combine(PathTarget, file.Name));
                        Fil_wasAdded.Add(Path.Combine(PathTarget, file.Name));
                        LOGSTR += "\r\n\r\nФайл \"" + Path.Combine(PathTarget, file.Name) + "\" успешно скопирован.";
                    }
                    File.SetAttributes(Path.Combine(PathTarget, file.Name), FileAttributes.Normal);
                }
            }
        }

        public void Restore(string RestorePath, string UpdateIndex, ref string LOGSTR)
        {
            string LogString = "\r\n\r\nНачало процедуры восстановлении резервной копии";
            LogString += "\r\nУдаление лишних файлов";

            StreamReader sr = new StreamReader(Directory.GetCurrentDirectory() + @"\DirsPath" + UpdateIndex + ".arl");
            while (!sr.EndOfStream)
            {
                string str = sr.ReadLine();
                if (str.Trim() != "")
                {
                    Dir_wasAdded.Add(str.Trim());
                }
            }
            sr.Close();

            sr = new StreamReader(Directory.GetCurrentDirectory() + @"\FilePath" + UpdateIndex + ".arl");
            while (!sr.EndOfStream)
            {
                string str = sr.ReadLine();
                if (str.Trim() != "")
                {
                    Fil_wasAdded.Add(str.Trim());
                }
            }
            sr.Close();

            foreach (string dir in Dir_wasAdded)
            {
                if (Directory.Exists(dir))
                {
                    Directory.Delete(dir, true);
                    LogString += "\r\nУдалена папка " + dir;
                }
            }

            foreach (string file in Fil_wasAdded)
            {
                if (File.Exists(file))
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                    File.Delete(file);
                    LogString += "\r\nФайл " + file + " удален";
                }
            }

            Loger.WriteInfo(LogString, ref LOGSTR);

            RestoreBAK(RestorePath, ref LOGSTR);
        }

        public void RestoreBAK(string RestorePath, ref string LOGSTR)
        {
            string LogString = "";

            DirectoryInfo DI = new DirectoryInfo(RestorePath);
            DirectoryInfo[] Dirs = DI.GetDirectories();
            FileInfo[] Files = DI.GetFiles();

            foreach (DirectoryInfo dir in Dirs)
            {
                if (!dir.FullName.Contains("UPDATER_EXE"))
                {
                    RestoreBAK(dir.FullName, ref LOGSTR);
                }
            }

            foreach (FileInfo file in Files)
            {
                if (file.Extension == ".bak")
                {
                    string RealName = file.FullName.Substring(0, file.FullName.Length - 4);
                    if (File.Exists(file.FullName))
                    {
                        if (File.Exists(RealName))
                        {
                            File.SetAttributes(RealName, FileAttributes.Normal);
                            File.Delete(RealName);
                        }
                        File.Move(file.FullName, RealName);
                        LogString += "\r\nФайл " + RealName + " восстановлен.";
                    }
                }
            }
        }

        //процедура создания пустых директорий
        public void CreateEmtyTree(string KomplatPath, string TreePath)
        {
            //Получение подкаталогов и файлов Комплата
            DirectoryInfo DirKomplat = new DirectoryInfo(KomplatPath);

            DirectoryInfo Mdir = Directory.CreateDirectory(TreePath + @"\" + DirKomplat.Name);

            DirectoryInfo[] SdirKomplat = DirKomplat.GetDirectories();
            FileInfo[] KomplatFiles = DirKomplat.GetFiles();

            
            //for (int i = 0; i < KomplatFiles.Length; i++)
            //{
            //    KomplatFiles[i].Delete();
            //}

            for (int i = 0; i < SdirKomplat.Length; i++)
            {
                //Создание директоии
                //DirectoryInfo dir = Directory.CreateDirectory(Mdir.FullName + @"\" + SdirKomplat[i].Name);
                CreateEmtyTree(SdirKomplat[i].FullName, Mdir.FullName);
            }
        }

        //Скачка обновлений по URI
        public Returns Transfer(string TargetPath, string DestPath, string UpdateDirectoryName, string key, string soup, ref string LOGSTR)
        {
            //-------------------перенос из updater.cs-------------------------

            //Путь, куда скачиваются обновления(разбивается на путь и имя файла)
            string DestinationPath = DestPath + @"\";
            string DestinationFile = "";

            if (TargetPath.Contains('/'))
            {
                DestinationFile = TargetPath.Substring(TargetPath.LastIndexOf('/') + 1);
            }
            else
            {
                DestinationFile = TargetPath.Substring(TargetPath.LastIndexOf('\\') + 1);
            }      
 
            //УДАЛЕНИЕ ФАЙЛОВ ОБНОВЛЕНИЙ(из Temp)
            try
            {
                Console.WriteLine("Проверка Temp...2сек");
                Loger.WriteInfo("Проверка Temp...", ref LOGSTR);
                if (Directory.Exists(DestinationPath + @"\" + UpdateDirectoryName))
                {
                    Directory.Delete(DestinationPath + @"\" + UpdateDirectoryName, true);
                }
                Loger.WriteInfo("OK", ref LOGSTR);
                System.Threading.Thread.Sleep(2000);
            }
            //Ошибка Удаления файлов обновления
            catch (Exception ex)
            {
                MonitorLog.WriteLog("23" + ex.Message, MonitorLog.typelog.Error, true);
                //НУЖНО ЛИ СТОПАРИТЬ СЛЕДУЮЩИЕ ??? МОЖЕТ БУДЕТ ЗАМЕНА И ВСЕ ОК?
                Loger.WriteInfo("Предварительное удаление файлов обновления - произошла ошибка " + ex.Message, ref LOGSTR);
            }
            //=================================================================






            Returns ret = Utils.InitReturns();
            string LogString = "\r\n\r\n" + Loger.AddLine("Загрузка обновлений");//       ***-=Загрузка обновлений=-***";
            try
            {
                //Копирование   
                if (TargetPath.IndexOf("http") == 0) 
                {
                    WebClient wc = new WebClient();
                    LogString += "\r\n\r\nПопытка загрузки обновлений...";
                    wc.DownloadFile(TargetPath, DestinationPath + DestinationFile);
                    LogString += "OK";
                }
                    //Обновления для web
                else
                {
                    //LogString += "\r\n\r\nПопытка загрузки обновлений...";
                    //File.Copy(TargetPath, DestinationPath + DestinationFile, true);
                    //LogString += "OK";
                }
                //Создание директории для разархивации
                LogString += "\r\n\r\nСоздание директории для разархивации...";
                DirectoryInfo DI = Directory.CreateDirectory(DestinationPath + UpdateDirectoryName);//"KOMPLAT.50");//DestinationFile.Substring(0, DestinationFile.LastIndexOf('.')) );
                LogString += "OK";

                //Получение ключа для расшифровки архива
                LogString += "\r\n\r\nРасшифровка...";
                string zipkey = Crypt.Decrypt(key, (encryptzipkey + soup));
                LogString += "OK";



                //Разархивация
                LogString += "\r\n\r\nРазархивация...";
                SevenZipExtractor extractor = new SevenZipExtractor(DestinationPath + DestinationFile, zipkey);
                string extractPath = DI.FullName;//DestinationPath.Substring(0, DestinationPath.LastIndexOf('\\'));
                extractor.ExtractArchive(extractPath);
                LogString += "OK";

                LogString += "\r\n\r\nЗагрузка обновлений успешно завершена!";
                Loger.WriteInfo(LogString , ref LOGSTR);

                //удаление архива обновления(из Temp)            
                try
                {
                    Loger.WriteInfo("Удаление архивов обновления...", ref LOGSTR);
                    //Directory.Delete(DestinationPath + @"\" + this.UpdateDirectoryName, true);
                    File.Delete(DestinationPath + DestinationFile);
                    Loger.WriteInfo("Успех!", ref LOGSTR);
                    //Console.WriteLine("Запуск следующего обновления...2сек");
                    //System.Threading.Thread.Sleep(2000);
                }
                //Ошибка Удаления файлов обновления
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("23" + ex.Message, MonitorLog.typelog.Error, true);
                    //НУЖНО ЛИ СТОПАРИТЬ СЛЕДУЮЩИЕ ??? МОЖЕТ БУДЕТ ЗАМЕНА И ВСЕ ОК?
                    Loger.WriteInfo("Удаление файлов обновления - произошла ошибка " + ex.Message, ref LOGSTR);
                    return ret;
                }

                return ret;
            }
            catch(Exception ex)
            {

                MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);

                ret.result = false;
                ret.text = ex.Message;
                LogString += "\r\n\r\nЗагрузка обновлений завершилась неудачей!";
                Loger.WriteInfo(LogString, ref LOGSTR);
                return ret;
            }
            
        }

        //процедура проверки входных путей на коректность
        public bool CheckPaths(string KomplatPath, string UpdatePath)
        {
            ////Заканчиваться далжны обязательно на KOMPLAT.5.0
            //if ((KomplatPath.LastIndexOf('\\') + 1) == (KomplatPath.LastIndexOf("KOMPLAT.50")))
            //{
            //    if ((UpdatePath.LastIndexOf('\\') + 1) == (UpdatePath.LastIndexOf("KOMPLAT.50")))
            //    {
            //        return true;
            //    }
            //}
            //return false;
            if ((KomplatPath.Substring(KomplatPath.LastIndexOf('\\') + 1)) == (UpdatePath.Substring(UpdatePath.LastIndexOf('\\') + 1)))
            {
                return true;
            }

            return false;
        }



    }//endClass
}
