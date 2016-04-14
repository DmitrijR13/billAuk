using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.ServiceModel;
using SevenZip;
using System.Collections;
using System.Security.Cryptography;
using System.Threading;
using System.Configuration;
using System.ServiceModel.Configuration;
using System.Net;
using BinaryAnalysis.UnidecodeSharp;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Client;
using System.Runtime.Serialization.Formatters.Binary;
using System.Diagnostics;
using VersionCompile;


namespace updater
{
    public partial class MainForm : Form
    {
        delegate void SetTextCallback(int index, string text);//для статуса обновления в таблице
        delegate void SetImageCallback(bool flag, DataGridViewRow row);
        //---------------------------------------------------------------------
        private string key = @"SVkGXDBLF6PrbQruccs7VVk8k7LlXaX7FPVByYmfarebKKVdkWr7u5c8N4o4qYpAe5HKMkE28VTchzbBjfWVh079gohaI6vwFkcO0rCNtwSJI4WYtoGwgi0DDRW82vlc";
        //public const string DB_Connect = "Client Locale=ru_ru.CP1251;Database=updater;Database Locale=ru_ru.915;Server=ol_megan;UID = informix;Pwd = info";//строка подключения к БД
        public const string service_connection = "/srv";//сервис на IIS со стороны района
        public ArrayList Rajons = new ArrayList();

        //---------------------------------------------------------------------

        public MainForm()
        {
            try
            {
                #region Считывание настроек

                INIFile ini = new INIFile(Path.Combine(Path.GetTempPath(), @"settings.ini"));

                PathsAndKeys.DB_Connect = ini.Read("Connect", "DB_Connect");

                PathsAndKeys.PathToHost = ini.Read("Path", "PathToHost");
                PathsAndKeys.PathToWeb = ini.Read("Path", "PathToWeb");
                PathsAndKeys.PathToSQL = ini.Read("Path", "PathToSQL");
                PathsAndKeys.PathToUpdate = ini.Read("Path", "PathToUpdate");
                PathsAndKeys.PathToAssembly = ini.Read("Path", "PathToAssembly");
                PathsAndKeys.WebPathToUpdate = ini.Read("Path", "WebPathToUpdate");
               
                PathsAndKeys.HostKey = ini.Read("Key", "HostKey");
                PathsAndKeys.WebKey = ini.Read("Key", "WebKey");

                #endregion
                WCFParams.AdresWcfWeb = new WCFParamsType();
                WCFParams.AdresWcfWeb.srvPatch = "/patch";
                RajonsGrid();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (PathsAndKeys.WebPathToUpdate == "")
            {
                MessageBox.Show("Не настроена ссылка на папку с обновлениями");
                return;
            }            
            
            FileInfo HostUpdate = null, WebUpdate = null;
            DirectoryInfo DI = new DirectoryInfo(PathsAndKeys.PathToUpdate);
            FileInfo[] files = DI.GetFiles();
            foreach (FileInfo file in files)
            {
                if (file.Name == "host.7z")
                {
                    HostUpdate = new FileInfo(file.FullName);
                }
                else if (file.Name == "web.7z")
                {
                    WebUpdate = new FileInfo(file.FullName);
                }
            }

            if ((HostUpdate != null) || (WebUpdate != null))
            {
                foreach (DataGridViewRow row in dgv_rajons.SelectedRows)
                {
                    if (row.Cells["nowstatus"].Value.ToString() == "Готов")
                    {
                        row.Cells["nowstatus"].Value = "В процессе...";
                    }
                }



                foreach (DataGridViewRow row in dgv_rajons.SelectedRows)
                {
                    Info rajon = (Info)(Rajons[row.Index]);
                    if ((rajon.connect) && (rajon.now_status == "Готов"))
                    {
                        string dir = Path.Combine(PathsAndKeys.PathToUpdate, ((Info)Rajons[row.Index]).rajon_name.Unidecode());
                        if (Directory.Exists(dir))
                        {
                            Directory.Delete(dir, true);
                        }
                        Directory.CreateDirectory(dir);
                        if (HostUpdate != null)
                        {
                            File.Copy(Path.Combine(PathsAndKeys.PathToUpdate, HostUpdate.Name), Path.Combine(dir, HostUpdate.Name));
                        }
                        if (WebUpdate != null)
                        {
                            File.Copy(Path.Combine(PathsAndKeys.PathToUpdate, WebUpdate.Name), Path.Combine(dir, WebUpdate.Name));
                        }
                        ThreadPool.QueueUserWorkItem(delegate(object notUsed) { Rajon_Update((Info)Rajons[row.Index], Path.Combine(PathsAndKeys.WebPathToUpdate, ((Info)Rajons[row.Index]).rajon_name.Unidecode()) + '/', HostUpdate, WebUpdate); });
                        System.Threading.Thread.Sleep(1000);
                    }
                }
            }
            else
            {
                MessageBox.Show("Не найдены обновления");
            }
        }

        //история обновлений района
        public void button2_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow rj in dgv_rajons.SelectedRows)
            {
                Rajon_History_List((Info)Rajons[rj.Index]);
            }
        }


        private void ChooseUpdatesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DirectoryInfo direct = new DirectoryInfo(PathsAndKeys.PathToUpdate);
            DirectoryInfo[] directfolders = direct.GetDirectories();
        }

        private void CreateEmptyUpdatesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string target = "", outP = "";
            FolderBrowserDialog dir = new FolderBrowserDialog();
            if (dir.ShowDialog() == DialogResult.OK)
            {
                target = dir.SelectedPath;
                FolderBrowserDialog vdir = new FolderBrowserDialog();
                if (vdir.ShowDialog() == DialogResult.OK)
                {
                    outP = vdir.SelectedPath;
                    CreateEmtyTree(dir.SelectedPath, vdir.SelectedPath);
                }
            }
        }

        #region Процедура создания пустых директорий

        //KomplatPath - ЦЕЛЬ. ПРИМЕР: @"D:\work_Oleg\KOMPLAT.50"
        //KomplatPath  - ВЫХОД КАРКАСА. ПРИМЕР: @"D:\Test_Komplat_update\UpdateFrom7z\KOMPLAT.50"
        public void CreateEmtyTree(string KomplatPath, string TreePath)
        {
            //DirectoryInfo Mdir = Directory.CreateDirectory(TreePath + @"\" + "host_update_v");
            DirectoryInfo Mdir = Directory.CreateDirectory(TreePath);
            
            string komplat = KomplatPath.Substring(0, KomplatPath.LastIndexOf("\\"));
            if (komplat == TreePath)
            {
                MessageBox.Show("Папка с таким именем уже существует, выберите другую директорию!");
            }
            else
            {
                //Получение подкаталогов и файлов Комплата
                DirectoryInfo DirKomplat = new DirectoryInfo(KomplatPath);

                DirectoryInfo[] SdirKomplat = DirKomplat.GetDirectories();
                FileInfo[] KomplatFiles = DirKomplat.GetFiles();

                for (int i = 0; i < SdirKomplat.Length; i++)
                {
                    //Создание директоии
                    CreateEmtyTree(SdirKomplat[i].FullName, Mdir.FullName);
                }
            }
        }

        #endregion

        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        #region Загрузка списка обновлений района

        public void Rajon_History_List(Info item)
        {
            if (PathsAndKeys.DB_Connect == "")
            {
                return;
            }
            ArrayList history = new ArrayList();
            string sql_hist = "Select rajon_name, update_status, update_type, update_version, update_date from rajon_history where rajon_name =" + "\'" + item.rajon_name + "\' and update_version <> 0 ORDER BY update_date DESC";
            RajonForm form2 = new RajonForm(item);
            Database DB = new Database();
            history = DB.Get_rajon_info(PathsAndKeys.DB_Connect, sql_hist, 0);
            if (history.Count != 0)
            {
                foreach (Info raj in history)
                {
                    Color color = Color.LightCoral;
                    if (raj.update_status == "1")
                    {
                        color = Color.LightGreen;
                    }
                    form2.dgv_rajon_history.Rows.Add(raj.rajon_name, raj.update_status, raj.update_type, raj.update_version, raj.update_date, "Отчет");

                    for (int i = 0; i < form2.dgv_rajon_history.ColumnCount; i++)
                    {
                        form2.dgv_rajon_history.Rows[form2.dgv_rajon_history.Rows.Count - 1].Cells[i].Style.BackColor = color;
                    }
                }
            }
            else
            {
                MessageBox.Show("У района " + item.rajon_name + " нет истории обновлений!");
            }
            form2.Text = item.rajon_name;
            form2.Show();
        }

        #endregion

        public void Updater_Update(Info rajon, string updatefolder, FileInfo UpdaterFile)
        {
            rajon.now_status = "Подготовка обновления";
            SetText(rajon.rowindex, rajon.now_status);
            DateTime StartUpdate = DateTime.Now;
            bool ContinueWaiting = true;
            int status = 1;
            if (UpdaterFile.Exists)
            {
                //обновление updater'a хоста и брокера
                //получаем последнее обновление
                cli_Patch cli = new cli_Patch(rajon.rajon_ip + "/srv", rajon.rajon_login, rajon.rajon_password);
                Stream stream = cli.GetHistoryLast(5);
                BinaryFormatter bf = new BinaryFormatter();
                UpData2[] lud2 = (UpData2[])(bf.Deserialize(stream));
                string LastUpdDate = lud2[0].date;

                // вычисление MD5 файла
                byte[] UpdateFile = File.ReadAllBytes(UpdaterFile.FullName);
                MD5 MD5Local = new MD5CryptoServiceProvider();
                byte[] retVal = MD5Local.ComputeHash(UpdateFile);
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < retVal.Length; i++)
                {
                    sb.Append(retVal[i].ToString("x2"));
                }
                string FileMD5 = sb.ToString();

                cli = new cli_Patch(rajon.rajon_ip + "/srv", rajon.rajon_login, rajon.rajon_password);
                ThreadPool.QueueUserWorkItem(delegate(object notUsed) { cli.FullUpdateStr(updatefolder + UpdaterFile.Name, FileMD5, 5, rajon.rajon_webpath, "", ""); });
                
                rajon.now_status = "Обновление запущено";
                SetText(rajon.rowindex, rajon.now_status);

                string NewUpdDate = LastUpdDate;
                do
                {
                    bool connect = false;
                    do
                    {
                        System.Threading.Thread.Sleep(5000);
                        try
                        {
                            cli = new cli_Patch(rajon.rajon_ip + "/srv", rajon.rajon_login, rajon.rajon_password);
                            connect = cli.CheckConn() == 1;
                        }
                        catch
                        {
                            connect = false;
                        }
                    }
                    while (!connect);

                    string ConStatus = "Район недоступен";
                    if (connect)
                    {
                        ConStatus = "Район доступен";
                        try
                        {
                            cli = new cli_Patch(rajon.rajon_ip + "/srv", rajon.rajon_login, rajon.rajon_password);
                            stream = cli.GetHistoryLast(5);
                            bf = new BinaryFormatter();
                            lud2 = (UpData2[])(bf.Deserialize(stream));
                            NewUpdDate = lud2[0].date;
                            status = int.Parse(lud2[0].status);
                        }
                        catch
                        {
                            NewUpdDate = LastUpdDate;
                        }
                    }
                    if (DateTime.Now - StartUpdate > new TimeSpan(0, 30, 0))
                    {
                        if (MessageBox.Show("Данных о завершении обновления нет более получаса.\r\nПодождать еще?\r\n\r\n" + ConStatus, "Host на районе" + rajon.rajon_name, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.OK)
                        {
                            StartUpdate = DateTime.Now;
                        }
                        else
                        {
                            ContinueWaiting = false;
                            status = 9;
                        }
                    }
                }
                while ((NewUpdDate == LastUpdDate) && (ContinueWaiting));
            }
            switch (status)
            {
                case 1:
                    {
                        rajon.now_status = "Готов";
                        break;
                    }
                case 2:
                    {
                        rajon.now_status = "Ошибка, откат успешен";
                        break;
                    }
                case 3:
                    {
                        rajon.now_status = "Ошибка, откат неудачен";
                        break;
                    }
                case 6:
                    {
                        rajon.now_status = "Ошибка удаления резервных копий";
                        break;
                    }
                case 7:
                    {
                        rajon.now_status = "Ошибка скрипта";
                        break;
                    }
                case 8:
                    {
                        rajon.now_status = "Ошибка обновления";
                        break;
                    }
                case 9:
                    {
                        rajon.now_status = "Отмена получения отчета";
                        break;
                    }
            }
            SetText(rajon.rowindex, rajon.now_status);
        }

        public void Rajon_Update(Info rajon, string updatefolder, FileInfo HostUpdateFile, FileInfo WebUpdateFile)
        {
            DateTime StartUpdate = DateTime.Now;
            bool ContinueWaiting = true;
            rajon.now_status = "Начало обновления";
            SetText(rajon.rowindex, rajon.now_status);

            #region Вычисление паролей MD5
            MD5 md5 = MD5.Create();
            byte[] inputBytes = Encoding.ASCII.GetBytes(PathsAndKeys.HostKey);
            byte[] hashBytes = md5.ComputeHash(inputBytes);
            StringBuilder str = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                str.Append(hashBytes[i].ToString("X2"));
            }

            string HostKeyMD5 = str.ToString();

            inputBytes = Encoding.ASCII.GetBytes(PathsAndKeys.WebKey);
            hashBytes = md5.ComputeHash(inputBytes);
            str = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                str.Append(hashBytes[i].ToString("X2"));
            }
            string WebKeyMD5 = str.ToString();

            #endregion

            if (HostUpdateFile != null)
            {
                //обновление хоста и брокера
                //получаем последнее обновление
                cli_Patch cli = new cli_Patch(rajon.rajon_ip + "/srv", rajon.rajon_login, rajon.rajon_password);
                Stream stream = cli.GetHistoryLast(2);
                BinaryFormatter bf = new BinaryFormatter();
                UpData2[] lud2 = (UpData2[])(bf.Deserialize(stream));
                string LastUpdDate = lud2[0].date;


                // вычисление MD5 файла
                byte[] UpdateFile = File.ReadAllBytes(HostUpdateFile.FullName);
                MD5 MD5Local = new MD5CryptoServiceProvider();
                byte[] retVal = MD5Local.ComputeHash(UpdateFile);
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < retVal.Length; i++)
                {
                    sb.Append(retVal[i].ToString("x2"));
                }
                string FileMD5 = sb.ToString();

                cli = new cli_Patch(rajon.rajon_ip + "/srv", rajon.rajon_login, rajon.rajon_password);
                ThreadPool.QueueUserWorkItem(delegate(object notUsed) { cli.FullUpdateStr(updatefolder + HostUpdateFile.Name, FileMD5, 2, rajon.rajon_webpath, PathsAndKeys.HostKey, HostKeyMD5); });
                rajon.now_status = "Запущено обновление хост, ожидание результата";
                SetText(rajon.rowindex, rajon.now_status);
                System.Threading.Thread.Sleep(10000);

                string NewUpdDate = LastUpdDate;
                do
                {
                    bool connect = false;
                    do
                    {
                        System.Threading.Thread.Sleep(5000);
                        try
                        {
                            cli = new cli_Patch(rajon.rajon_ip + "/srv", rajon.rajon_login, rajon.rajon_password);
                            connect = cli.CheckConn() == 1;
                        }
                        catch
                        {
                            connect = false;
                        }
                    }
                    while (!connect);

                    string ConStatus = "Район недоступен";
                    if (connect)
                    {
                        ConStatus = "Район доступен";
                        try
                        {
                            cli = new cli_Patch(rajon.rajon_ip + "/srv", rajon.rajon_login, rajon.rajon_password);
                            stream = cli.GetHistoryLast(2);
                            bf = new BinaryFormatter();
                            lud2 = (UpData2[])(bf.Deserialize(stream));
                            NewUpdDate = lud2[0].date;
                        }
                        catch
                        {
                            NewUpdDate = LastUpdDate;
                        }
                    }
                    if (DateTime.Now - StartUpdate > new TimeSpan(0,30,0))
                    {
                        if (MessageBox.Show("Данных о завершении обновления нет более получаса.\r\nПодождать еще?\r\n\r\n" + ConStatus, "Host на районе" + rajon.rajon_name, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.OK)
                        {
                            StartUpdate = DateTime.Now;
                        }
                        else
                        {
                            ContinueWaiting = false;
                        }
                    }
                }
                while ((NewUpdDate == LastUpdDate) && (ContinueWaiting));
                if (ContinueWaiting)
                {
                    if ((lud2[0].status != "1"))
                    {
                        int status = int.Parse(lud2[0].status);
                        WebUpdateFile = null;
                        cli = new cli_Patch(rajon.rajon_ip + "/srv", rajon.rajon_login, rajon.rajon_password);
                        cli.RestoreFromBackup("");
                        switch (status)
                        {
                            case 2:
                                {
                                    rajon.now_status = "Ошибка, откат успешен";
                                    break;
                                }
                            case 3:
                                {
                                    rajon.now_status = "Ошибка, откат неудачен";
                                    break;
                                }
                            case 6:
                                {
                                    rajon.now_status = "Ошибка удаления резервных копий";
                                    break;
                                }
                            case 7:
                                {
                                    rajon.now_status = "Ошибка скрипта";
                                    break;
                                }
                            case 8:
                                {
                                    rajon.now_status = "Ошибка обновления";
                                    break;
                                }
                            case 9:
                                {
                                    rajon.now_status = "Отмена получения отчета";
                                    break;
                                }
                            default:
                                {
                                    rajon.now_status = "Ошибка обновления, откат";
                                    break;
                                }
                        }
                    }
                    else
                    {
                        rajon.now_status = "Готов";
                    }
                }
                else
                {
                    rajon.now_status = "Отмена ожидания результата обновления хоста";    
                }
                SetText(rajon.rowindex, rajon.now_status);
            }
            if ((WebUpdateFile != null) && (ContinueWaiting))
            {
                StartUpdate = DateTime.Now;
                //обновление веб(а)
                //получаем последнее обновление
                cli_Patch cli = new cli_Patch(rajon.rajon_ip + "/srv", rajon.rajon_login, rajon.rajon_password);
                Stream stream = cli.GetHistoryLast(1);
                BinaryFormatter bf = new BinaryFormatter();
                UpData2[] lud2 = (UpData2[])(bf.Deserialize(stream));
                string LastUpdDate = lud2[0].date;

                // вычисление MD5 файла
                byte[] UpdateFile = File.ReadAllBytes(WebUpdateFile.FullName);
                MD5 MD5Local = new MD5CryptoServiceProvider();
                byte[] retVal = MD5Local.ComputeHash(UpdateFile);
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < retVal.Length; i++)
                {
                    sb.Append(retVal[i].ToString("x2"));
                }
                string FileMD5 = sb.ToString();

                cli = new cli_Patch(rajon.rajon_ip + "/srv", rajon.rajon_login, rajon.rajon_password);
                ThreadPool.QueueUserWorkItem(delegate(object notUsed) { cli.FullUpdateStr(updatefolder + WebUpdateFile.Name, FileMD5, 1, rajon.rajon_webpath, PathsAndKeys.WebKey, WebKeyMD5); });
                rajon.now_status = "Запущено обновление веб, ожидание результата";
                SetText(rajon.rowindex, rajon.now_status);
                System.Threading.Thread.Sleep(10000);

                string NewUpdDate = LastUpdDate;
                do
                {
                    bool connect = false;
                    do
                    {
                        System.Threading.Thread.Sleep(5000);
                        try
                        {
                            cli = new cli_Patch(rajon.rajon_ip + "/srv", rajon.rajon_login, rajon.rajon_password);
                            connect = cli.CheckConn() == 1;
                        }
                        catch
                        {
                            connect = false;
                        }
                    }
                    while (!connect);

                    string ConStatus = "Район недоступен";
                    if (connect)
                    {
                        ConStatus = "Район доступен";
                        try
                        {
                            cli = new cli_Patch(rajon.rajon_ip + "/srv", rajon.rajon_login, rajon.rajon_password);
                            stream = cli.GetHistoryLast(1);
                            bf = new BinaryFormatter();
                            lud2 = (UpData2[])(bf.Deserialize(stream));
                            NewUpdDate = lud2[0].date;
                        }
                        catch
                        {
                            NewUpdDate = LastUpdDate;
                        }
                    }

                    if (DateTime.Now - StartUpdate > new TimeSpan(0, 30, 0))
                    {
                        if (MessageBox.Show("Данных о завершении обновления нет более получаса.\r\nПодождать еще?\r\n\r\n" + ConStatus, "Web на районе" + rajon.rajon_name, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.OK)
                        {
                            StartUpdate = DateTime.Now;
                        }
                        else
                        {
                            ContinueWaiting = false;
                        }
                    }
                }
                while ((NewUpdDate == LastUpdDate) && (ContinueWaiting));



                if ((lud2[0].status != "1") && (ContinueWaiting))
                {
                    cli = new cli_Patch(rajon.rajon_ip + "/srv", rajon.rajon_login, rajon.rajon_password);
                    ThreadPool.QueueUserWorkItem(delegate(object notUsed) { cli.RestoreFromBackup(rajon.rajon_webpath); });
                    int status = int.Parse(lud2[0].status);
                    switch (status)
                    {
                        case 2:
                            {
                                rajon.now_status = "Ошибка, откат успешен";
                                break;
                            }
                        case 3:
                            {
                                rajon.now_status = "Ошибка, откат неудачен";
                                break;
                            }
                        case 6:
                            {
                                rajon.now_status = "Ошибка удаления резервных копий";
                                break;
                            }
                        case 7:
                            {
                                rajon.now_status = "Ошибка скрипта";
                                break;
                            }
                        case 8:
                            {
                                rajon.now_status = "Ошибка обновления";
                                break;
                            }
                        case 9:
                            {
                                rajon.now_status = "Отмена получения отчета";
                                break;
                            }
                        default:
                            {
                                rajon.now_status = "Ошибка обновления, откат";
                                break;
                            }
                    }
                }
                else if (ContinueWaiting)
                {
                    cli_Patch clip = new cli_Patch(rajon.rajon_ip + "/srv", rajon.rajon_login, rajon.rajon_password);
                    clip.RemoveBackupFiles(rajon.rajon_webpath);
                    rajon.now_status = "Готов";
                }
                else
                {
                    rajon.now_status = "Отмена получения отчета";
                }
            }
            if (Directory.Exists(Path.Combine(PathsAndKeys.PathToUpdate, rajon.rajon_name.Unidecode())))
            {
                Directory.Delete(Path.Combine(PathsAndKeys.PathToUpdate, rajon.rajon_name.Unidecode()), true);
            }
            SetText(rajon.rowindex, rajon.now_status);
        }// end rajon_update()

        public void Rajon_Updater(DataGridViewRow dr, string iis_folder, DirectoryInfo[] dirfolders)
        {
            if (PathsAndKeys.DB_Connect == "")
            {
                MessageBox.Show("Настройте подключение к БД");
                return;
            }
            #region старая версия обновления
            bool checker = false;
            string raj_number = dgv_rajons[0, dr.Index].Value.ToString();
            string raj_name = dgv_rajons[1, dr.Index].Value.ToString();
            string raj_ip = dgv_rajons[2, dr.Index].Value.ToString() + service_connection;
            string raj = dgv_rajons[2, dr.Index].Value.ToString();

            string folder_name = raj_name.Unidecode();

            DictsClass List = new DictsClass();

            Directory.CreateDirectory(iis_folder + "/" + folder_name);

            //проверка соединения с районом
            checker = Check.CheckConnect((Info)(Rajons[dr.Index]));

            if (checker == true)
            {
                #region Настройка путей район

                //-------------STCLINE--------------------
                //ip = "192.168.2.3";
                //----------------------------------------

                string host = System.Net.Dns.GetHostName();//получение имени компьютера
                //string ip = System.Net.Dns.GetHostByName(host).AddressList[0].ToString();//получение ip-адреса 
                IPHostEntry hostInfo = Dns.GetHostEntry(host);
                string ip = "127.0.0.1";
                foreach (System.Net.IPAddress ipadd in hostInfo.AddressList)
                {
                    if (ipadd.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        ip = ipadd.ToString();

                    }
                }
                //string ip = Dns.GetHostEntry(host).AddressList[2].ToString();
                #endregion

                //string rajon = dataGridView1[0, dr.Index].Value.ToString();
                string rajon = dr.Cells[0].Value.ToString();
                //ключ для шифрования ключа к архиву
                string encryptzipkey = "6KaZpzqRHNh4JtIlcZw7OCfOV1h5vdP30izIHzk/jBQ=";
                //ключ для шифрования пути к архиву
                string encryptpathkey = "zRZ1/BHciushX1T9Ks5WwdRNjgjEEGzFypuKZTmkAOE=";

                string soup = "";//добавочный передаваемы ключ
                int soup_length = 10;//длина добавочного передаваемого ключа

                string encryptpath_host = "";//зашифрованный путь к архиву host_updater
                //string encryptpath_web = "";//зашифрованный путь к архиву web_updater
                string daykey = "";//однодневный ключ для шифрования и дешифрования
                int daykey_length = 16;//длина ключа для шифрования однодневного ключа

                Dictionary<string, string> Lib1 = new Dictionary<string, string>();
                Dictionary<string, string> Lib2 = new Dictionary<string, string>();


                lock (this)
                {
                    for (int i = 0; i < dirfolders.Length; i++)
                    {
                        string zipkey = "";//зашифрованный ключ к архиву

                        string download_path = PathsAndKeys.WebPathToUpdate + folder_name + "/" + dirfolders[i].Name + ".7z";//ссылка на папку в IIS
                       
                        soup = Password.GeneratePassword(soup_length);

                        //шифрование пути к архиву
                        encryptpath_host = Crypt.Encrypt(download_path, (encryptpathkey + soup));
                        //encryptpath_web = Crypt.Encrypt(path_web, (encryptpathkey + soup));
                        //генерация 16-значного однодневного ключа
                        daykey = Password.GeneratePassword(daykey_length);
                        zipkey = Crypt.Encrypt(daykey, (encryptzipkey + soup));

                        SevenZipCompressor file = new SevenZipCompressor();

                        file.CompressDirectory(dirfolders[i].FullName, Path.Combine(PathsAndKeys.PathToUpdate, Path.Combine(folder_name, dirfolders[i].Name + ".7z")), daykey);
                        Lib1.Add(dirfolders[i].Name + ".7z", encryptpath_host);
                        Lib2.Add(zipkey, soup);
                    }
                }
                    #region ссылка на районы
                bool var = true;
                    //соединение для районов
                    //cli.getdownloadpatch
                    if (var)
                    {
                        try
                        {
                            lock (this)
                            {
                                Database db = new Database();
                                //запись данных об обновлениях в базу
                                foreach (UpData2 upd in List.L1)
                                {
                                    DateTime timer1 = DateTime.Now;
                                    string time = timer1.ToString("yyyy-MM-dd HH:mm");
                                    db.WriteUpdate(PathsAndKeys.DB_Connect, "INSERT INTO rajon_history VALUES (" + "\'" + raj_name + "\'," + "\'" + upd.status + "\'," + "\'" + upd.typeUp + "\'," + "\'" + upd.Version + "\'," + "\'" + time + "\'," + "?" + ")", upd.report);
                                }
                                foreach (UpData2 upd in List.L2)
                                {
                                    DateTime timer2 = DateTime.Now;
                                    string time = timer2.ToString("yyyy-MM-dd HH:mm");
                                    db.WriteUpdate(PathsAndKeys.DB_Connect, "INSERT INTO rajon_history VALUES (" + "\'" + raj_name + "\'," + "\'" + upd.status + "\'," + "\'" + upd.typeUp + "\'," + "\'" + upd.Version + "\'," + "\'" + time + "\'," + "?" + ")", upd.report);
                                }
                            }
                        }
                        catch (Exception)
                        {
                            MessageBox.Show("Ошибка при получении данных об обновлениях с района: " + raj_name);
                        }
                    }
                    Directory.Delete(iis_folder + @"/" + folder_name, true);
                Lib1.Clear();
                Lib2.Clear();
            }
           
            #endregion
            #endregion
        }

        private void ClearFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //удаление архивов
            DirectoryInfo dir = new DirectoryInfo(@"\\almir\для обновлений\source");
            DirectoryInfo[] dirfolders = dir.GetDirectories();
            for (int j = 0; j < dirfolders.Length; j++)
            {
                File.Delete(Path.Combine(@"\\almir\для обновлений", dirfolders[j].Name + ".7z"));
            }
            MessageBox.Show("Папка успешно очищена!");
        }

        public void RequestToolStripMenuItem_Click(object sender, EventArgs e)
        {

            string rajs = "";
            ArrayList rajons_ip = new ArrayList();
            ArrayList rajons_name = new ArrayList();
            foreach (DataGridViewRow rj in dgv_rajons.SelectedRows)
            {
                rajons_name.Add(dgv_rajons[1, rj.Index].Value.ToString());
                rajons_ip.Add(dgv_rajons[2, rj.Index].Value.ToString());
                rajs += dgv_rajons[1, rj.Index].Value.ToString() + "/";
            }
            ReqForm requestform = new ReqForm(rajs, rajons_ip, rajons_name);
            requestform.Show();
        }

        private void sQLToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string rajs = "";
            ArrayList rajons_ip = new ArrayList();
            ArrayList rajons_name = new ArrayList();
            foreach (DataGridViewRow rj in dgv_rajons.SelectedRows)
            {
                rajons_name.Add(dgv_rajons[1, rj.Index].Value.ToString());
                rajons_ip.Add(dgv_rajons[2, rj.Index].Value.ToString());
                rajs += dgv_rajons[1, rj.Index].Value.ToString() + "/";
            }
            ReqForm requestform = new ReqForm(rajs, rajons_ip, rajons_name);
            requestform.Show();
        }

        private void monitolLogToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (dgv_rajons.SelectedRows.Count > 0)
            {
                GetMonLogForm frmGetml = new GetMonLogForm((Info)(Rajons[dgv_rajons.SelectedRows[0].Index]));
                frmGetml.Show();
            }
        }

        private void tsmUpdUpd_Click(object sender, EventArgs e)
        {
            if (DateTime.Now > DateTime.Parse("1990-01-01 00:00:00"))
            {
                MessageBox.Show("Функция будет обновлена позже");
                return;
            }
            dlgOpen.Filter = @"7z files|*.7z";
            if (dlgOpen.ShowDialog() == DialogResult.OK)
            {
                FileInfo file = new FileInfo(dlgOpen.FileName);
                if ((file.FullName.ToLower().Trim() != @"\\almir\для обновлений\updater.7z") && (file.FullName.ToLower().Trim() != @"c:\source\updater.7z"))
                {
                    if (file.DirectoryName.ToLower() != @"\\almir\для обновлений")
                    {
                        if (File.Exists(@"\\almir\для обновлений\updater.7z"))
                        {
                            File.SetAttributes(@"\\almir\для обновлений\updater.7z", FileAttributes.Normal);
                            File.Delete(@"\\almir\для обновлений\updater.7z");
                        }
                        file.MoveTo(@"\\almir\для обновлений\updater.7z");
                    }
                }

                foreach (DataGridViewRow row in dgv_rajons.SelectedRows)
                {
                    if (row.Cells["nowstatus"].Value.ToString() == "Готов")
                    {
                        row.Cells["nowstatus"].Value = "В процессе...";
                    }
                }

                file = new FileInfo(@"\\almir\для обновлений\updater.7z");
                foreach (DataGridViewRow row in dgv_rajons.SelectedRows)
                {
                    Info rajon = (Info)(Rajons[row.Index]);
                    if ((rajon.connect) && (rajon.now_status == "Готов"))
                    {
                        ThreadPool.QueueUserWorkItem(delegate(object notUsed) { Updater_Update(rajon, PathsAndKeys.WebPathToUpdate, file); });
                        System.Threading.Thread.Sleep(5000);
                    }
                }
            }
        }

        public void EncryptSQLFile(string Extension)
        {
            dlgOpen.Filter = @"SQL files|*.sql";
            dlgSave.Filter = @"Cript SQL files|*" + Extension;
            dlgOpen.FileName = "";
            dlgSave.FileName = "";
            if (dlgOpen.ShowDialog() == DialogResult.OK)
            {
                if (dlgSave.ShowDialog() == DialogResult.OK)
                {
                    StreamReader sr = new StreamReader(dlgOpen.FileName);
                    StreamWriter sw = new StreamWriter(dlgSave.FileName);
                    string str;
                    bool comment = false;
                    while ((str = sr.ReadLine()) != null)
                    {
                        // удаление коментариев вида --...
                        if (str.IndexOf("--") != -1)
                            if (str.IndexOf("--") == 0)
                                continue;
                            else
                                str = str.Substring(0, str.IndexOf("--"));
                        // удаление коментариев вида /*...*/
                        if (str.IndexOf("/*") != -1)
                            comment = true;
                        if (comment)
                            if (str.IndexOf("*/") == -1)
                                continue;
                            else
                            {
                                str = str.Substring(str.IndexOf("*/") + 2, str.Length - str.IndexOf("*/") - 2);
                                comment = false;
                            }
                        // замена двойных кавычек на одинарные, если строка не содержит <p>
                        if (str.IndexOf("<p>") == -1)
                        {
                            while (str.IndexOf('\"') != -1)
                            {
                                int First, Second;
                                First = str.IndexOf('\"');
                                Second = First + str.Substring(First + 1, str.Length - First - 1).IndexOf('\"') + 1;
                                str = str.Substring(0, First) + "\'" + str.Substring(First + 1, Second - First - 1).Replace("\'", "\'\'") + "\'" + str.Substring(Second + 1, str.Length - Second - 1);
                            }
                        }
                        str = Crypt.Encrypt(str, key);
                        sw.WriteLine(str);
                    }
                    sr.Close();
                    sw.Close();
                    str = dlgSave.FileName.Substring(0, dlgSave.FileName.LastIndexOf('\\') + 1);
                    System.Diagnostics.Process.Start(str);
                }
            }
        }

        private void dATAToolStripMenuItem_Click(object sender, EventArgs e)
        {
            EncryptSQLFile(".wd");
        }

        private void kERNELToolStripMenuItem_Click(object sender, EventArgs e)
        {
            EncryptSQLFile(".wk");
        }

        //private void button3_Click(object sender, EventArgs e)
        //{
        //    int NewCount = 10;
        //    ArrayList al = new ArrayList();
        //    do
        //    {
        //        cli_Patch cli = new cli_Patch("net.tcp://192.168.1.125:8008/srv", "Administrator", "rubin");
        //        Stream stream = cli.GetSelect();
        //        BinaryFormatter bf = new BinaryFormatter();
        //        try
        //        {
        //            string[] strings = (string[])bf.Deserialize(stream);
        //            NewCount = strings.Length;
        //            al.AddRange(strings);
        //        }
        //        catch
        //        {
        //            NewCount = 0;
        //        }
        //    }
        //    while (NewCount == 10);

        //}

        private void свойЗапросToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int index = dgv_rajons.SelectedRows[0].Index;
            ExecSQLListForm formsql = new ExecSQLListForm(((Info)Rajons[index]).rajon_ip, ((Info)Rajons[index]).rajon_login, ((Info)Rajons[index]).rajon_password, ((Info)Rajons[index]).rajon_name);
            formsql.Show();
        }

        public void RajonsGrid()
        {
            try
            {
                dgv_rajons = new DataGridView();
                string sql_inf = "Select * from rajon_ip where updater_install = 1 Order By rajon_number";
                InitializeComponent();
                string column1name = "number";
                string column2name = "rajons";
                string column3name = "ip";
                string column4name = "webpath";
                string column5name = "login";
                string column6name = "password";
                string column7name = "raschmes";
                string column8name = "nowstatus";

                dgv_rajons.Columns.Add(column1name, "Номер");
                dgv_rajons.Columns.Add(column2name, "Районы");
                dgv_rajons.Columns.Add(column3name, "IP адреса получателей");
                dgv_rajons.Columns.Add(column4name, "Путь к WEB");
                dgv_rajons.Columns.Add(column5name, "Логин");
                dgv_rajons.Columns.Add(column6name, "Пароль");
                dgv_rajons.Columns.Add(column7name, "Расчетный месяц");
                dgv_rajons.Columns.Add(column8name, "Текущее состояние");


                dgv_rajons.Columns[0].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                dgv_rajons.Columns[0].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                dgv_rajons.Columns[0].DefaultCellStyle.Font = new Font("Arial", 14F, GraphicsUnit.Pixel);
                dgv_rajons.Columns[0].Width = 50;

                dgv_rajons.Columns[1].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                dgv_rajons.Columns[1].DefaultCellStyle.Font = new Font("Arial", 14F, GraphicsUnit.Pixel);
                dgv_rajons.Columns[1].Width = 150;

                dgv_rajons.Columns[2].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                dgv_rajons.Columns[2].DefaultCellStyle.Font = new Font("Arial", 14F, GraphicsUnit.Pixel);
                dgv_rajons.Columns[2].Width = 250;

                dgv_rajons.Columns[3].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                dgv_rajons.Columns[3].DefaultCellStyle.Font = new Font("Arial", 14F, GraphicsUnit.Pixel);
                dgv_rajons.Columns[3].Width = 150;

                dgv_rajons.Columns[4].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                dgv_rajons.Columns[4].DefaultCellStyle.Font = new Font("Arial", 14F, GraphicsUnit.Pixel);
                dgv_rajons.Columns[4].Width = 100;

                dgv_rajons.Columns[5].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                dgv_rajons.Columns[5].DefaultCellStyle.Font = new Font("Arial", 14F, GraphicsUnit.Pixel);
                dgv_rajons.Columns[5].Width = 100;

                dgv_rajons.Columns[6].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                dgv_rajons.Columns[6].DefaultCellStyle.Font = new Font("Arial", 14F, GraphicsUnit.Pixel);
                dgv_rajons.Columns[6].Width = 110;

                dgv_rajons.Columns[7].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                dgv_rajons.Columns[7].DefaultCellStyle.Font = new Font("Arial", 14F, GraphicsUnit.Pixel);
                dgv_rajons.Columns[7].Width = 100;

                dgv_rajons.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                Database DB = new Database();

                if (PathsAndKeys.DB_Connect == "")
                {
                    MessageBox.Show("Настройте подключение к БД");
                    return;
                }
                Rajons = DB.Get_rajon_info(PathsAndKeys.DB_Connect, sql_inf, 4);//загрузка информации о районах
                foreach (Info item in Rajons)
                {
                    dgv_rajons.Rows.Add(item.rajon_number, item.rajon_name, item.rajon_ip, item.rajon_webpath, item.rajon_login, item.rajon_password);//формируем таблицу
                }

                DataGridViewImageColumn img = new DataGridViewImageColumn();
                img.HeaderText = "";
                img.Width = 25;
                img.Name = "img";
                dgv_rajons.Columns.Add(img);

                foreach (DataGridViewRow oRow in dgv_rajons.Rows)
                {
                    Thread t = new Thread(CheckRajon);
                    t.Start(oRow);
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }


        public void CheckRajon(object row)
        {
            DataGridViewRow oRow = (DataGridViewRow)row;

                Info rajon = (Info)(Rajons[oRow.Index]);
                bool a = Check.CheckConnect(rajon);
                if (a)
                {
                    //если есть соединение, то выгружается еще и расчетный месяц
                    cli_Patch cli = new cli_Patch(rajon.rajon_ip + "/srv", rajon.rajon_login, rajon.rajon_password);
                    DateTime dt = cli.GetCurrentMount();
                    oRow.Cells["raschmes"].Value = dt.ToString("y");
                    rajon.now_status = "Готов";
                    SetImage(true, oRow);
                }
                else
                {
                    SetImage(false, oRow);
                    rajon.now_status = "Недоступен";
                }
                oRow.Cells["nowstatus"].Value = rajon.now_status;
                rajon.rowindex = oRow.Index;
        }

        private void SetImage(bool flag, DataGridViewRow row)
        {
            Image imag = null;
            if (flag)
                imag = Image.FromFile("true.png");
            else
                imag = Image.FromFile("false.png");

            if (this.dgv_rajons.InvokeRequired)
            {
                SetImageCallback d = new SetImageCallback(SetImage);
                this.Invoke(d, new object[] { flag, row });
            }
            else
            {
                row.Cells["img"].Value = imag;
            }
        }

        private void SetText(int index, string text)
        {
            if (this.dgv_rajons.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetText);
                this.Invoke(d, new object[] { index, text });
            }
            else
            {
                dgv_rajons.Rows[index].Cells["nowstatus"].Value = text;
            }
        }



        private void собратьВерсиюToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CompileVersionsForm frmComVer = new CompileVersionsForm();
            frmComVer.ShowDialog();
        }

        private void выполнитьPHPToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (PathsAndKeys.DB_Connect == "")
            {
                MessageBox.Show("Настройте подключение к БД");
                return;
            }
            string str_sql = @"Select rajon_number, rajon_name, rajon_ip from rajon_ip where php_ready = 1 Order By rajon_number";
            Database DB = new Database();
            ArrayList PhpRajons = DB.Get_rajon_info(PathsAndKeys.DB_Connect, str_sql, 5);

            ExecPHPForm frm = new ExecPHPForm(PhpRajons);
            frm.ShowDialog();
        }
    }
}
