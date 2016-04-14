using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;
using SevenZip;
using System.Threading;
using System.Collections;

namespace VersionCompile
{
    public partial class CompileVersionsForm : Form
    {
        private string RegexCompDebug = "(?i)(?<=\\s*<compilation debug=\")\\w+(?=\">)";
        private string RegexTheme = "(?i)(?<=\\s*)<pages theme=\"\\w+\" validateRequest=\"false\" masterPageFile=\"[~/.\\w]+\">";

        // Config
        public ArrayList HostValues = new ArrayList();
        public ArrayList WebValues = new ArrayList();

        public CompileVersionsForm()
        {
            InitializeComponent();

            // загружаются имена файлов в CheckListBox
            LoadFromINI();
            if (Directory.Exists(PathsAndKeys.PathToSQL))
            {
                FileInfo[] files = (new DirectoryInfo(PathsAndKeys.PathToSQL)).GetFiles();
                foreach (FileInfo file in files)
                {
                    if (file.Extension == ".sql")
                    {
                        chlbSQLFiles.Items.Add(file.Name);
                    }
                }
            }

            if (File.Exists(Path.Combine(PathsAndKeys.PathToHost, "Host.config")))
            {
                HostValues = ConfigSaveLoad.GetValuesFromConfig(Path.Combine(PathsAndKeys.PathToHost, "Host.config"));
            }

            if (File.Exists(Path.Combine(PathsAndKeys.PathToWeb, "Web.config")))
            {
                WebValues = ConfigSaveLoad.GetValuesFromConfig(Path.Combine(PathsAndKeys.PathToWeb, "Connect.config"));
            } 
        }

        private void cbWeb_CheckedChanged(object sender, EventArgs e)
        {
            if (!cbWeb.Checked)
            {
                gbBase.Enabled = false;
                gbTheme.Enabled = false;
                if (!cbHost.Checked)
                {
                    btnCompile.Enabled = false;
                }
                else
                {
                    btnCompile.Enabled = true;
                }
            }
            else
            {
                gbBase.Enabled = true;
                gbTheme.Enabled = true;
                btnCompile.Enabled = true;
            }
        }

        private void cbHost_CheckedChanged(object sender, EventArgs e)
        {
            if (!cbWeb.Checked)
            {
                btnCompile.Enabled = cbHost.Checked;
            }
        }

        private void CompileVersion(bool Host, bool Web, int Database, int Theme, string[] SQLFiles, ArrayList HostConfig, ArrayList WebConfig, bool forUpdate)
        {
            // настройка архиватора
            SevenZipCompressor.SetLibraryPath(Path.Combine(Directory.GetCurrentDirectory(), "7z.dll"));

            Process process = new Process();

            string UpdatePath = "", TempUP = "";
            // создание новой папки для обновления
            if (Host || Web || (SQLFiles.Length > 0))
            {
                int count = 1;
                if (forUpdate)
                {
                    UpdatePath = PathsAndKeys.PathToUpdate;
                    File.Delete(Path.Combine(UpdatePath, "host.7z"));
                    File.Delete(Path.Combine(UpdatePath, "web.7z"));
                }
                else
                {
                    TempUP = DateTime.Now.ToString("yyyy-MM-dd");
                    UpdatePath = TempUP;
                    while (Directory.Exists(Path.Combine(PathsAndKeys.PathToAssembly, UpdatePath)))
                    {
                        UpdatePath = TempUP + " (" + (count++).ToString() + ')';
                    }
                    Directory.CreateDirectory(Path.Combine(PathsAndKeys.PathToAssembly, UpdatePath));
                }
            }

            string WebTempPath2 = Path.Combine(Path.GetTempPath(), @"Web2"); // путь для временной компиляции web

            // если надо собрать Web, то все важное копируется во временную папку и запускается встроенный компилятор
            if (Web)
            {
                string WebTempPath = Path.Combine(Path.GetTempPath(), @"Web"); // путь к временной папке Web
                
                // удаление старой папки (если есть)
                if (Directory.Exists(WebTempPath))
                {
                    Process cmd = Process.Start("cmd.exe", "/C rmdir " + WebTempPath + " /s /q");
                    while (CheckIdProcess(cmd.Id))
                    {
                        Thread.Sleep(500);
                    }
                }
                // копирование нужные файлы во временную папку
                DirectoryCopy(PathsAndKeys.PathToWeb, WebTempPath, true);

                // замена connect.config
                ConfigSaveLoad.SaveValuesToConfig(Path.Combine(WebTempPath, "Connect.config"), WebConfig, 1);

                // формирование новых строк для web.config
                string CompDebug = "\t\t<compilation debug=\"false\">";
                string ThemeStr = "";
                if (Theme == 0)
                {
                    ThemeStr = "      <pages theme=\"blue\" validateRequest=\"false\" masterPageFile=\"~/master/Main.Master\">";
                }
                else
                {
                    ThemeStr = "      <pages theme=\"bars\" validateRequest=\"false\" masterPageFile=\"~/master/Bars.Master\">";
                }

                #region изменение web.config
                File.Move(Path.Combine(WebTempPath, @"Web.config"), Path.Combine(WebTempPath, @"Web.config.bak"));
                StreamReader sr = new StreamReader(Path.Combine(WebTempPath, @"Web.config.bak"));
                StreamWriter sw = new StreamWriter(Path.Combine(WebTempPath, @"Web.config"));

                string str = "";
                while ((str = sr.ReadLine()) != null)
                {
                    Regex regex = new Regex(RegexCompDebug);
                    Match match = regex.Match(str);
                    if (match.Success)
                    {
                        sw.WriteLine(CompDebug);
                    }
                    else
                    {
                        regex = new Regex(RegexTheme);
                        match = regex.Match(str);
                        if (match.Success)
                        {
                            sw.WriteLine(ThemeStr);
                        }
                        else
                        {
                            sw.WriteLine(str);
                        }
                    }
                }
                sr.Close();
                sw.Close();
                #endregion

                // запуск компилятора для web
                if (Directory.Exists(WebTempPath2))
                {
                    Process cmd = Process.Start("cmd.exe", "/C rmdir " + WebTempPath2 + " /s /q");
                    while (CheckIdProcess(cmd.Id))
                    {
                        Thread.Sleep(500);
                    }
                }
                Directory.CreateDirectory(WebTempPath2);
                process = Process.Start(@"C:\Windows\Microsoft.NET\Framework\v2.0.50727\aspnet_compiler.exe", "-v none -p " + WebTempPath + " " + WebTempPath2);
            }

            // пока собирается web (если вообще собирается), собирается все остальное
            if (Host)
            {
                string HostTempPath = Path.GetTempPath() + @"Host"; // путь к временной папке Host

                // создание пустой папки Host
                if (Directory.Exists(HostTempPath))
                {
                    Process cmd = Process.Start("cmd.exe", "/C rmdir " + HostTempPath + " /s /q");
                    while (CheckIdProcess(cmd.Id))
                    {
                        Thread.Sleep(500);
                    }
                }
                Directory.CreateDirectory(HostTempPath);

                // копирование файлов
                FileInfo[] files = (new DirectoryInfo(PathsAndKeys.PathToHost)).GetFiles();
                foreach (FileInfo file in files)
                {
                    if ((file.Extension == ".dll") || (((file.Extension == ".exe") || ((file.Extension == ".config") && !forUpdate)) && (file.Name.ToLower().Contains("host") || file.Name.ToLower().Contains("hostman.exe") || file.Name.ToLower().Contains("broker.exe")) && (!file.Name.ToLower().Contains("vshost")))) 
                    {
                        file.CopyTo(Path.Combine(HostTempPath,file.Name), true);
                        File.SetAttributes(Path.Combine(HostTempPath,file.Name), FileAttributes.Normal);
                    }
                }

                // замена host.config
                ConfigSaveLoad.SaveValuesToConfig(Path.Combine(HostTempPath, "Host.config"), HostConfig, 0);

                // копирование папок TEMPLATE и UPDATER_EXE

                foreach (DirectoryInfo HostSubDir in (new DirectoryInfo(PathsAndKeys.PathToHost)).GetDirectories())
                {
                    if (!forUpdate || (HostSubDir.Name != "UPDATER_EXE"))
                    {
                        Directory.CreateDirectory(Path.Combine(HostTempPath, HostSubDir.Name));
                        files = HostSubDir.GetFiles();
                        foreach (FileInfo file in files)
                        {
                            if ((file.Extension == ".xls") || (file.Extension == ".dll") || ((file.Extension == ".exe") && (!file.Name.Contains("vshost"))))
                            {
                                file.CopyTo(Path.Combine(Path.Combine(HostTempPath, HostSubDir.Name), file.Name), true);
                                File.SetAttributes(Path.Combine(Path.Combine(HostTempPath, HostSubDir.Name), file.Name), FileAttributes.Normal);
                            }
                        }
                    }
                }

                if (forUpdate)
                {
                    foreach (string str in SQLFiles)
                    {
                        File.Copy(Path.Combine(PathsAndKeys.PathToSQL, str), Path.Combine(HostTempPath, str));
                        File.SetAttributes(Path.Combine(HostTempPath, str), FileAttributes.Normal);
                    }
                }

                SevenZipCompressor comp = new SevenZipCompressor();
                if (forUpdate)
                {
                    comp.CompressDirectory(HostTempPath, Path.Combine(PathsAndKeys.PathToUpdate, UpdatePath) + @"\host.7z", true, PathsAndKeys.HostKey);
                }
                else
                {
                    comp.CompressDirectory(HostTempPath, Path.Combine(PathsAndKeys.PathToAssembly, UpdatePath) + @"\host.7z", true, PathsAndKeys.HostKey);
                }
                Process cmd2 = Process.Start("cmd.exe", "/C rmdir " + Path.GetTempPath() + "Host /s /q");
                while (CheckIdProcess(cmd2.Id))
                {
                    Thread.Sleep(500);
                }
            }

            if (!forUpdate)
            {
                foreach (string str in SQLFiles)
                {
                    File.Copy(Path.Combine(PathsAndKeys.PathToSQL, str), Path.Combine(PathsAndKeys.PathToAssembly, UpdatePath) + '\\' + str);
                    File.SetAttributes(Path.Combine(PathsAndKeys.PathToAssembly, UpdatePath) + '\\' + str, FileAttributes.Normal);
                }
            }

            if (Web)
            {
                // ожидание завершении компиляции
                while (CheckIdProcess(process.Id))
                {
                    Thread.Sleep(500);
                }

                #region Удаление ненужных файлов
                if (forUpdate)
                {
                    DirectoryInfo diweb2 = new DirectoryInfo(WebTempPath2);
                    FileInfo[] filesweb2 = diweb2.GetFiles();
                    foreach (var file in filesweb2)
                    {
                        if (file.Extension == ".config")
                        {
                            file.Delete();
                        }
                    }
                }

                if (Directory.Exists(WebTempPath2 + @"\ExcelReport"))
                {
                    Process cmd = Process.Start("cmd.exe", "/C rmdir " + WebTempPath2 + "\\ExcelReport /s /q");
                    while (CheckIdProcess(cmd.Id))
                    {
                        Thread.Sleep(500);
                    }
                }
                Directory.CreateDirectory(WebTempPath2 + @"\ExcelReport");

                if (Directory.Exists(WebTempPath2 + @"\obj"))
                {
                    Process cmd = Process.Start("cmd.exe", "/C rmdir " + WebTempPath2 + @"\\obj /s /q");
                    while (CheckIdProcess(cmd.Id))
                    {
                        Thread.Sleep(500);
                    }
                }

                if (Directory.Exists(WebTempPath2 + @"\kart\gil"))
                {
                    FileInfo[] files = (new DirectoryInfo(WebTempPath2 + @"\kart\gil")).GetFiles();
                    foreach (FileInfo file in files)
                    {
                        if ((file.Extension != ".aspx") || (file.Name.Contains("3.aspx") ^ (Database == 1)))
                        {
                            file.Delete();
                        }
                    }
                }

                if (Directory.Exists(WebTempPath2 + @"\App_Themes"))
                {
                    DirectoryInfo[] dirs = (new DirectoryInfo(WebTempPath2 + @"\App_Themes")).GetDirectories();
                    foreach (DirectoryInfo dir in dirs)
                    {
                        if (!(((dir.Name.ToLower() == "blue") && (Theme == 0)) || ((dir.Name.ToLower() == "bars") && (Theme == 1))))
                        {
                            dir.Delete(true);
                        }
                    }
                }

                FileInfo[] files2 = (new DirectoryInfo(WebTempPath2)).GetFiles();
                foreach (FileInfo file in files2)
                {
                    if ((file.Name.ToLower() != "404.html") && (file.Name.ToLower() != "default.aspx") && (file.Name.ToLower() != "connect.config") && (file.Name.ToLower() != "web.config") && (file.Name.ToLower() != "precompiledapp.config"))
                    {
                        file.Delete();
                    }
                }

                SevenZipCompressor comp = new SevenZipCompressor();
                if (files2.Length > 0)
                {
                    if (forUpdate)
                    {
                        comp.CompressDirectory(WebTempPath2, Path.Combine(PathsAndKeys.PathToUpdate, UpdatePath) + @"\web.7z", true, PathsAndKeys.WebKey);
                    }
                    else
                    {
                        comp.CompressDirectory(WebTempPath2, Path.Combine(PathsAndKeys.PathToAssembly, UpdatePath) + @"\web.7z", true, PathsAndKeys.WebKey);
                    }

                    Process cmd2 = Process.Start("cmd.exe", "/C rmdir " + Path.GetTempPath() + "Web /s /q");
                    while (CheckIdProcess(cmd2.Id))
                    {
                        Thread.Sleep(500);
                    }

                    cmd2 = Process.Start("cmd.exe", "/C rmdir " + WebTempPath2 + " /s /q");
                    while (CheckIdProcess(cmd2.Id))
                    {
                        Thread.Sleep(500);
                    }
                }
                else
                {
                    MessageBox.Show("Ошибка сборки версии", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                #endregion;
            }

            MessageBox.Show("Версия собрана.\r\n" + UpdatePath);
        }

        // рекурсивное копирование содержимого папки sourceDirName в destDirName (из MSDN)
        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);
            DirectoryInfo[] dirs = dir.GetDirectories();

            // If the source directory does not exist, throw an exception.
            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            // If the destination directory does not exist, create it.
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }


            // Get the file contents of the directory to copy.
            FileInfo[] files = dir.GetFiles();

            foreach (FileInfo file in files)
            {
                // Create the path to the new copy of the file.
                string temppath = Path.Combine(destDirName, file.Name);

                // Copy the file.
                file.CopyTo(temppath, false);
                // Установка аттрибутов в нормальные
                File.SetAttributes(temppath, FileAttributes.Normal);
            }

            // If copySubDirs is true, copy the subdirectories.
            if (copySubDirs)
            {

                foreach (DirectoryInfo subdir in dirs)
                {
                    // Create the subdirectory.
                    string temppath = Path.Combine(destDirName, subdir.Name);

                    // Copy the subdirectories.
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
        }

        private void btnCompile_Click(object sender, EventArgs e)
        {
            if ((PathsAndKeys.PathToHost != "") && (PathsAndKeys.PathToWeb != "") && (PathsAndKeys.PathToSQL != "") && (PathsAndKeys.PathToUpdate != "") && (PathsAndKeys.PathToAssembly != ""))
            {
                // получение списка выбранных sql файлов
                List<string> SQLFilesList = new List<string>();
                for (int i = 0; i < chlbSQLFiles.CheckedItems.Count; i++)
                {
                    SQLFilesList.Add(chlbSQLFiles.CheckedItems[i].ToString());
                }
                // запуск компиляции
                CompileVersion(cbHost.Checked, cbWeb.Checked, rbInformix.Checked ? 0 : 1, rbBlue.Checked ? 0 : 1, SQLFilesList.ToArray(), HostValues, WebValues, rbForUpdate.Checked);
            }
            else
            {
                // не все пути указаны
                MessageBox.Show("Не все пути указаны.\r\nНастройте программу...");
            }
        }

        public bool CheckIdProcess(int id)
        {
            Process[] p = Process.GetProcesses();
            for (int i = 0; i < p.Length; i++)
            {
                if (p[i].Id == id)
                {
                    return true;
                }
            }
            return false;
        }

        #region 
        public void SaveToINI()
        {
            INIFile ini = new INIFile(Path.Combine(Path.GetTempPath(), @"settings.ini"));

            ini.Write("Connect", "DB_Connect", PathsAndKeys.DB_Connect);

            ini.Write("Path", "PathToHost", PathsAndKeys.PathToHost);
            ini.Write("Path", "PathToWeb", PathsAndKeys.PathToWeb);
            ini.Write("Path", "PathToSQL", PathsAndKeys.PathToSQL);
            ini.Write("Path", "PathToUpdate", PathsAndKeys.PathToUpdate);
            ini.Write("Path", "PathToAssembly", PathsAndKeys.PathToAssembly);
            ini.Write("Path", "WebPathToUpdate", PathsAndKeys.WebPathToUpdate);

            ini.Write("Key", "HostKey", PathsAndKeys.HostKey);
            ini.Write("Key", "WebKey", PathsAndKeys.WebKey);
        }

        public void LoadFromINI()
        {
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
        }
        #endregion

        public void button2_Click(object sender, EventArgs e)
        {
            ProgramSettingsForm frmProsSet = new ProgramSettingsForm();
            if (frmProsSet.ShowDialog() == DialogResult.OK)
            {
                SaveToINI();
            }

            if (MessageBox.Show("Пути обновления были изменены.\r\nОбновить настройки .config файлов?", "", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                if (File.Exists(Path.Combine(PathsAndKeys.PathToHost, "Host.config")))
                {
                    HostValues = ConfigSaveLoad.GetValuesFromConfig(Path.Combine(PathsAndKeys.PathToHost, "Host.config"));
                }

                if (File.Exists(Path.Combine(PathsAndKeys.PathToWeb, "Web.config")))
                {
                    WebValues = ConfigSaveLoad.GetValuesFromConfig(Path.Combine(PathsAndKeys.PathToWeb, "Connect.config"));
                }
            }
        }

        private void btnChangeConfig_Click(object sender, EventArgs e)
        {
            ConfigSettingsForm frmConSet = new ConfigSettingsForm(PathsAndKeys.PathToHost, PathsAndKeys.PathToWeb, HostValues, WebValues);

            if (frmConSet.ShowDialog() == DialogResult.OK)
            {
                HostValues = frmConSet.HostConfig;
                WebValues = frmConSet.WebConfig;
            }
        }
    }
}
