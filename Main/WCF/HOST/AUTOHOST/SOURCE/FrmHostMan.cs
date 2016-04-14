using System;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Collections.Generic;
using System.IO;
using Bars.KP50.DB.DbSqAdmin;
using Bars.KP50.Gubkin;
using STCLINE.KP50.Server;
using STCLINE.KP50.Global;
using STCLINE.KP50.Utility;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.DataBase;
using SevenZip;
using STCLINE.KP50.Client;
using System.Data.OleDb;
using System.Configuration;
using IBM.Data.Informix;
using STCLINE.KP50.HostMan.SelectDataBase;
using Bars.CloudRoster.Contracts.Data;

using STCLINE.KP50.HostMan.KLADR;
using STCLINE.KP50.HostMan.SOURCE;

namespace STCLINE.KP50.HostMan
{
    public enum enGrid
    {
        Host,
        Broker,
        Web,
        Pwd,
        None,
        // для нового обработчика конфига
        NewHost,
        NewWeb
    }

    public partial class FrmHostMan : Form
    {
        private string key = @"SVkGXDBLF6PrbQruccs7VVk8k7LlXaX7FPVByYmfarebKKVdkWr7u5c8N4o4qYpAe5HKMkE28VTchzbBjfWVh079gohaI6vwFkcO0rCNtwSJI4WYtoGwgi0DDRW82vlc"; // для расшифровки sql файлов
        private string dataBaseName;
        private string baseConnString;

        /// <summary>
        /// Состояния режима хостинга
        /// </summary>
        enum HostingStates
        {
            Start,
            Stop
        }

        /// <summary>
        /// Текущее состояние режима хостинга
        /// </summary>
        HostingStates HostingCurrentState;

        enGrid grid;
        string config_file;
        string config_dir;

        BindingSource bs_Grid;
        List<ConfigKey> cnfGrid;
        List<PwdKey> pwdGrid;
        int pwdSort = Constants.sortby_login;

        public FrmHostMan()
        {
            InitializeComponent();

            SrvRun.ProgramRole = SrvRun.ProgramRoles.Host;
            SrvRun.tb_Message = tb_Message;
            SrvRun.MessageOutputMode = SrvRun.MessageOutputModes.WinForm;

            HostingCurrentState = HostingStates.Stop;
            grid = enGrid.None;

            TC.TabPages.Remove(tb_Control);
            dt_CalcMonth.Value = DateTime.Now;
            dt_CurMonth.Value = DateTime.Now;

            bs_Grid = new BindingSource();
            cnfGrid = new List<ConfigKey>();
            pwdGrid = new List<PwdKey>();
            EmptyMenu();

            WCFParams.AdresWcfHost = new WCFParamsType();
        }

        void EmptyMenu()
        {
            grid = enGrid.None;
            Mn_Save.Enabled = false;
            Tlb_Save.Enabled = Mn_Save.Enabled;
            сервисToolStripMenuItem.Visible = false;
            выгрузкаДанныхToolStripMenuItem.Visible = true;
            CreateCMDFile.Visible = false;

            cnfGrid.Clear();
            pwdGrid.Clear();
            Grid.Visible = false;

            St_Spis.Text = "";
        }
        private void Mn_Exit_Click(object sender, EventArgs e)
        {
            StopHosting();
            Close();
        }

        //------------------------------------------------------------
        //
        // Работа с hosting
        //
        //------------------------------------------------------------
        private void Mn_HostStart_Click(object sender, EventArgs e)
        {
            StartHosting();
            EnabledHostingMenu();
        }
        private void Mn_HostStop_Click(object sender, EventArgs e)
        {
            StopHosting();
            EnabledHostingMenu();
        }
        void EnabledHostingMenu()
        {
            Mn_HostStart.Enabled = (HostingCurrentState == HostingStates.Stop);
            Mn_HostStop.Enabled = (HostingCurrentState == HostingStates.Start);
            Tlb_HostStart.Enabled = Mn_HostStart.Enabled;
            Tlb_HostStop.Enabled = Mn_HostStop.Enabled;

            if (HostingCurrentState == HostingStates.Stop)
                St_Spis.Text = "Хостинг остановлен";
            else
                St_Spis.Text = "Хостинг запущен";
        }

        /// <summary>
        /// Старт хостинга
        /// </summary>
        void StartHosting()
        {
            if (HostingCurrentState == HostingStates.Start) return;
            HostingCurrentState = HostingStates.Start;

            TC.SelectedIndex = 0;

            SrvRun.StartHostProgram();
            /*
            if (DataBaseHead.ConfPref == "L")
                SrvRun.Start3(new string[0]);
            else
                SrvRun.Start(new string[0]);
            */

            SrvRun.WriteMessage("Хостинг выполняется");

            //TC.SelectedIndex = 1;
        }

        /// <summary>
        /// Стоп хостинга
        /// </summary>
        void StopHosting()
        {
            if (HostingCurrentState == HostingStates.Stop) return;
            HostingCurrentState = HostingStates.Stop;

            TC.SelectedIndex = 0;
            SrvRun.WriteMessage("Хостинг в процессе остановки, ждите ...");
            SrvRun.TaskStop();
            SrvRun.WriteMessage("Хостинг остановлен");
            MonitorLog.Close("Остановка хостинга");
        }

        //------------------------------------------------------------
        //
        // Работа с config
        //
        //------------------------------------------------------------
        void EnabledConfigMenu()
        {
            Mn_Save.Enabled = (grid != enGrid.None);
            Tlb_Save.Enabled = Mn_Save.Enabled;
            сервисToolStripMenuItem.Visible = true;
            выгрузкаДанныхToolStripMenuItem.Visible = false;
            CreateCMDFile.Visible = true;

            if (grid == enGrid.None) return;
            if (grid == enGrid.Web)
                St_Spis.Text = "Открыт файл Web.config";
            else
                St_Spis.Text = "Открыт файл " + config_file + ".config";

            //tb_Message.AppendText("\r\n" + St_Spis.Text);
        }

        //список строк config
        void ConfigGrid()
        {
            TC.SelectedIndex = 1;

            Grid.Visible = true;
            Grid.AutoGenerateColumns = false;
            Grid.Dock = DockStyle.Fill;
            Grid.Columns.Clear();

            bs_Grid.DataSource = cnfGrid;
            Grid.DataSource = bs_Grid;

            DataGridViewTextBoxColumn column = new DataGridViewTextBoxColumn();
            //column.
            column.DataPropertyName = "key";
            column.HeaderText = "Ключ";
            column.ValueType = typeof(string);
            column.FillWeight = 30;
            column.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            column.ReadOnly = true;
            Grid.Columns.Add(column);

            column = new DataGridViewTextBoxColumn();
            column.DataPropertyName = "val";
            column.HeaderText = "Значение";
            column.ValueType = typeof(string);
            column.FillWeight = 300;
            column.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            Grid.Columns.Add(column);

            //Grid.EditMode = DataGridViewEditMode.EditProgrammatically;
            //Grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            //Grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            Grid.Focus();

        }

        //------------------------------------------------------------
        //
        // Тесты
        //
        //------------------------------------------------------------
        private void Mn_Test_Click(object sender, EventArgs e)
        {
            SrcHostMan hm = new SrcHostMan();
            TC.SelectedIndex = 0;
            if (WCFParams.AdresWcfHost.Adres == "")
            {
                TC.SelectedIndex = 0;
                SrvRun.StartHostProgram(false);
            }
            //tb_Info.AppendText(" \r\n " + hm.TestAdres());
            //tb_Message.AppendText("\r\n" + hm.TestIntervals2());
            //tb_Message.AppendText("\r\n" + hm.MustCalc());
            //tb_Message.AppendText("\r\n" + hm.MustCalc());

            SrvRun.WriteMessage(hm.PackXX(true));
            SrvRun.WriteMessage("Процесс завершен!");
        }
        private void Mn_Test2_Click(object sender, EventArgs e)
        {
            SrcHostMan hm = new SrcHostMan();
            TC.SelectedIndex = 0;
            if (WCFParams.AdresWcfHost.Adres == "")
            {
                TC.SelectedIndex = 0;
                SrvRun.StartHostProgram(false);
            }
            //tb_Info.AppendText(" \r\n " + hm.TestAdres());
            //tb_Message.AppendText("\r\n" + hm.TestIntervals2());
            //tb_Message.AppendText("\r\n" + hm.MustCalc());
            //tb_Message.AppendText("\r\n" + hm.MustCalc());

            SrvRun.WriteMessage(hm.PackXX(false));
            SrvRun.WriteMessage("Процесс завершен!");
        }


        //------------------------------------------------------------
        //
        // Пароли доступа
        //
        //------------------------------------------------------------
        private void Mn_PwdSortLogin_Click(object sender, EventArgs e)
        {
            pwdSort = (pwdSort == Math.Abs(pwdSort) ? pwdSort * (-1) : Math.Abs(Constants.sortby_login));
            PwdRun();
        }

        private void Mn_PwdSortKod_Click(object sender, EventArgs e)
        {
            pwdSort = (pwdSort == Math.Abs(pwdSort) ? pwdSort * (-1) : Math.Abs(Constants.sortby_nzp_user));
            PwdRun();
        }
        private void Mn_Pwd_Click(object sender, EventArgs e)
        {
            //pwdSort = (pwdSort == Math.Abs(pwdSort) ? pwdSort * (-1) : Constants.sortby_login);
            PwdRun();
        }
        private void PwdRun()
        {
            if (WCFParams.AdresWcfHost.Adres == "")
            {
                TC.SelectedIndex = 0;
                SrvRun.StartHostProgram(false);
            }

            SrcHostMan hm = new SrcHostMan();
            Returns ret = new Returns();
            hm.GetPwd(pwdGrid, pwdSort, out ret);

            if (ret.result)
            {
                TC.SelectedIndex = 1;
                PwdGrid();
            }
            else
            {
                TC.SelectedIndex = 0;
                SrvRun.WriteMessage(ret.text);
            }
            EnabledPwdMenu();
        }
        void EnabledPwdMenu()
        {
            Mn_Save.Enabled = (grid != enGrid.None);
            Tlb_Save.Enabled = Mn_Save.Enabled;

            if (grid == enGrid.Pwd)
            {
                St_Spis.Text = "Открыт список паролей";
                //tb_Message.AppendText("\r\n" + St_Spis.Text);
            }
        }
        //список строк паролей
        void PwdGrid()
        {
            grid = enGrid.Pwd;
            TC.SelectedIndex = 1;

            Grid.Visible = true;
            Grid.AutoGenerateColumns = false;
            Grid.Dock = DockStyle.Fill;
            Grid.Columns.Clear();

            bs_Grid.DataSource = pwdGrid;
            Grid.DataSource = bs_Grid;

            DataGridViewTextBoxColumn column = new DataGridViewTextBoxColumn();
            //column.SortMode = DataGridViewColumnSortMode.Programmatic;
            column.DataPropertyName = "nzp_user";
            column.HeaderText = "Код";
            column.ValueType = typeof(int);
            column.FillWeight = 30;
            column.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            column.ReadOnly = true;
            Grid.Columns.Add(column);

            column = new DataGridViewTextBoxColumn();
            //column.SortMode = DataGridViewColumnSortMode.Programmatic;
            column.DataPropertyName = "login";
            column.HeaderText = "Логин";
            column.ValueType = typeof(string);
            column.FillWeight = 50;
            column.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            Grid.Columns.Add(column);

            column = new DataGridViewTextBoxColumn();
            //column.SortMode = DataGridViewColumnSortMode.Programmatic;
            column.DataPropertyName = "pwd";
            column.HeaderText = "Пароль";
            column.ValueType = typeof(string);
            column.FillWeight = 50;
            column.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            Grid.Columns.Add(column);

            column = new DataGridViewTextBoxColumn();
            column.DataPropertyName = "uname";
            column.HeaderText = "Пользователь";
            column.ValueType = typeof(string);
            column.FillWeight = 100;
            column.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            Grid.Columns.Add(column);

            column = new DataGridViewTextBoxColumn();
            column.DataPropertyName = "email";
            column.HeaderText = "E-Mail";
            column.ValueType = typeof(string);
            column.FillWeight = 100;
            column.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            Grid.Columns.Add(column);

            //Grid.EditMode = DataGridViewEditMode.EditProgrammatically;
            //Grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            //Grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            Grid.Focus();
        }

        //------------------------------------------------------------
        //
        // Сохранить изменения
        //
        //------------------------------------------------------------
        private void Mn_Save_Click(object sender, EventArgs e)
        {
            if (grid == enGrid.None)
            {
                EmptyMenu();
                return;
            }
            TC.SelectedIndex = 0;

            SrcHostMan hm = new SrcHostMan();
            if (grid == enGrid.Pwd)
            {
                SrvRun.WriteMessage(hm.Save(pwdGrid));
            }
            else if ((grid == enGrid.NewHost) || (grid == enGrid.NewWeb))
            {
                SrvRun.WriteMessage(hm.SaveToFile(cnfGrid, grid, config_file));
            }
            else
            {
                SrvRun.WriteMessage(hm.Save(cnfGrid, grid, config_dir));
            }
            EmptyMenu();
        }

        //------------------------------------------------------------
        //
        // Первый запуск приложения
        //
        //------------------------------------------------------------
        private void Mn_FirstRunApp_Click(object sender, EventArgs e)
        {
            MessageBoxButtons mb = MessageBoxButtons.YesNo;
            DialogResult rezMsgDialog = new DialogResult();
            rezMsgDialog = MessageBox.Show("Запустить процесс?", "Первый запуск приложения", mb, MessageBoxIcon.Question);

            if (rezMsgDialog.Equals(DialogResult.No))
            {
                return;
            }

            TC.SelectedIndex = 0;
            if (WCFParams.AdresWcfHost.Adres == "")
            {
                TC.SelectedIndex = 0;
                SrvRun.StartHostProgram(false);
            }

            SrvRun.WriteMessage("Выполняется первый запуск приложения, ждите ...");
            Refresh();
            SrcHostMan hm = new SrcHostMan();
            SrvRun.WriteMessage(hm.FirstRunApp());
        }
        //------------------------------------------------------------
        //
        // SaldoFon
        //
        //------------------------------------------------------------
        private void Mn_SaldoFon_Click(object sender, EventArgs e)
        {
            MessageBoxButtons mb = MessageBoxButtons.YesNo;
            DialogResult rezMsgDialog = new DialogResult();
            rezMsgDialog = MessageBox.Show("Запустить процесс?", "SaldoFon", mb, MessageBoxIcon.Question);

            if (rezMsgDialog.Equals(DialogResult.No))
            {
                return;
            }

            TC.SelectedIndex = 0;
            if (WCFParams.AdresWcfHost.Adres == "")
            {
                TC.SelectedIndex = 0;
                SrvRun.StartHostProgram(false);
            }

            SrvRun.WriteMessage("Выполняется SaldoFon, ждите ...");
            Refresh();
            SrcHostMan hm = new SrcHostMan();
            SrvRun.WriteMessage(hm.SaldoFon());
        }

        private void отобразитьТестыToolStripMenuItem_Click(object sender, EventArgs e)
        {
            M_Test.Visible = (!M_Test.Visible);
        }

        //------------------------------------------------------------
        //
        // Тестовый доступ
        //
        //------------------------------------------------------------
        private void Mn_TestDostup_Click(object sender, EventArgs e)
        {
            TC.SelectedIndex = 0;
            if (WCFParams.AdresWcfHost.Adres == "")
            {
                TC.SelectedIndex = 0;
                SrvRun.StartHostProgram(false);
            }

            Refresh();
            SrvRun.WriteMessage("");
            SrcHostMan hm = new SrcHostMan();
            SrvRun.WriteMessage(hm.TestDostup());
        }

        private void Mn_CalcGilXX_Click(object sender, EventArgs e)
        {
            TC.SelectedIndex = 0;
            if (WCFParams.AdresWcfHost.Adres == "")
            {
                TC.SelectedIndex = 0;
                SrvRun.StartHostProgram(false);
            }

            Refresh();
            SrvRun.WriteMessage("");
            SrcHostMan hm = new SrcHostMan();
            SrvRun.WriteMessage(hm.TestCalcGilXX());
        }

        //------------------------------------------------------------
        //
        // Тестовый вызов расчета
        //
        //------------------------------------------------------------
        private void Mn_GetAnlXX_Click(object sender, EventArgs e)
        {
            TC.SelectedIndex = 0;
            if (WCFParams.AdresWcfHost.Adres == "")
            {
                TC.SelectedIndex = 0;
                SrvRun.StartHostProgram(false);
            }

            Refresh();
            SrvRun.WriteMessage("");
            SrcHostMan shm = new SrcHostMan();
            SrvRun.WriteMessage(shm.GetAnlXX());
        }

        //------------------------------------------------------------
        //
        // Тестовый вызов расчета
        //
        //------------------------------------------------------------
        private void Mn_CalcGku_Click(object sender, EventArgs e)
        {
            //
            TC.SelectedIndex = 0;
            if (WCFParams.AdresWcfHost.Adres == "")
            {
                TC.SelectedIndex = 0;
                SrvRun.StartHostProgram(false);
            }

            Refresh();
            SrvRun.WriteMessage("");
            SrcHostMan hcalc = new SrcHostMan();
            SrvRun.WriteMessage(hcalc.CalcGkuXX_Do());
        }

        private void Mn_CalcRashod_Click(object sender, EventArgs e)
        {
            TC.SelectedIndex = 0;
            if (WCFParams.AdresWcfHost.Adres == "")
            {
                TC.SelectedIndex = 0;
                SrvRun.StartHostProgram(false);
            }

            Refresh();
            SrvRun.WriteMessage("");
            SrcHostMan hcalc = new SrcHostMan();
            SrvRun.WriteMessage(hcalc.CalcRashod());
        }

        private void Mn_Calc_Click(object sender, EventArgs e)
        {
            Mn_Calc.Checked = !Mn_Calc.Checked;

            if (Mn_Calc.Checked)
                TC.TabPages.Add(tb_Control);
            else
                TC.TabPages.Remove(tb_Control);
        }

        private void b_Calc_Click(object sender, EventArgs e)
        {
            //int nzp_dom, string pref, int yy, int mm, out Returns ret
            string pref = tb_Pref.Text;

            int nzp_dom = 0;
            int.TryParse(tb_Dom.Text, out nzp_dom);

            DateTime d = new DateTime();

            int cur_yy = 0;
            int cur_mm = 0;
            int calc_yy = 0;
            int calc_mm = 0;

            if (DateTime.TryParse(dt_CurMonth.Text, out d))
            {
                cur_yy = d.Year;
                cur_mm = d.Month;
            }
            if (DateTime.TryParse(dt_CalcMonth.Text, out d))
            {
                calc_yy = d.Year;
                calc_mm = d.Month;
            }

            if ((string.IsNullOrEmpty(pref)) || (cur_yy < 2010) || (calc_yy < 2010))
            {
                MessageBox.Show("Неверные входные данные!", "Внимание!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }


            TC.SelectedIndex = 0;
            if (WCFParams.AdresWcfHost.Adres == "")
            {
                TC.SelectedIndex = 0;
                SrvRun.StartHostProgram(false);
            }

            bool[] clc = new bool[]
            {
                chb_GilXX.Checked, chb_Rashod.Checked, chb_CalcNedo.Checked, chb_CalcGku.Checked, chb_ChargeXX.Checked
            };

            Refresh();
            SrvRun.WriteMessage("");
            SrcHostMan hcalc = new SrcHostMan();
            SrvRun.WriteMessage(hcalc.StartCalc(nzp_dom, pref/*, calc_yy, calc_mm, cur_yy, cur_mm*/, clc));
        }

        private void Mn_Test0_Click(object sender, EventArgs e)
        {
            //
            TC.SelectedIndex = 0;
            if (WCFParams.AdresWcfHost.Adres == "")
            {
                TC.SelectedIndex = 0;
                SrvRun.StartHostProgram(false);
            }

            Refresh();
            SrvRun.WriteMessage("");
            SrcHostMan hcalc = new SrcHostMan();
            SrvRun.WriteMessage(hcalc.Test0());
        }

        private void ExecWDWKToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if ((Constants.cons_Webdata == null) || (Constants.cons_Kernel == null))
            {
                Mn_TestDostup_Click(sender, e);
            }
            OpenFileDialog dlgOpen = new OpenFileDialog();
            dlgOpen.FileName = "";
            dlgOpen.Filter = "Files|*.wd; *.wk";
            if (dlgOpen.ShowDialog() == DialogResult.OK)
            {
                StreamReader sr = new StreamReader(dlgOpen.FileName);
                System.Collections.ArrayList SqlArray = new System.Collections.ArrayList();
                string str;

                while ((str = sr.ReadLine()) != null)
                {
                    SqlArray.Add(Crypt.Decrypt(str, key));
                }

                sr.Close();

                if (new FileInfo(dlgOpen.FileName).Extension == ".wd")
                {
                    str = Constants.cons_Webdata;
                }
                else
                {
                    str = Constants.cons_Kernel;
                }

                DbPatch db = new DbPatch();
                Returns ret = Utils.InitReturns();
                db.GoScript_DB(out ret, SqlArray, str);

                if (ret.result)
                {
                    MessageBox.Show("Процедура выполнена успешно");
                }
                else
                {
                    MessageBox.Show("Процедура выполнена неудачно");
                }
            }
        }

        private void выполнитьАрхивациюToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (WCFParams.AdresWcfHost.Adres == "")
            {
                SrvRun.StartHostProgram(false);
            }
            //WCFParams.AdresWcfHost.Adres = "net.tcp://localhost:8080";
            cli_Archive cli = new cli_Archive();
            Returns ret = cli.MakeArchive(new Finder());
            cli_Patch clip = new cli_Patch();
            DateTime dt = clip.GetCurrentMount();
            MessageBox.Show(dt.ToString("G") + ret.result.ToString());
        }

        private void btnKLADR_Click(object sender, EventArgs e)
        {
            //считывание параметров из конфига
            Configuration config = ConfigurationManager.OpenExeConfiguration("KP50.Host.exe");

            string sectionName = "appSettings";
            AppSettingsSection appSettings = (AppSettingsSection)config.GetSection(sectionName);

            List<ConfigKey> confList = new List<ConfigKey>();
            if (appSettings.Settings.Count != 0)
            {
                foreach (string key in appSettings.Settings.AllKeys)
                {
                    string value = TryDecrypt(appSettings.Settings[key].Value);
                    if (!string.IsNullOrEmpty(value))
                    {
                        ConfigKey conf = new ConfigKey();
                        conf.val = value;
                        conf.key = key;
                        confList.Add(conf);
                    }
                }
            }
            else
            {
                MessageBox.Show("Строки не определены!");
            }
            baseConnString = confList[3].val;
            DialogResult res = ofKLADR.ShowDialog();
            if (res == DialogResult.OK)
            {
                #region открытие формы выбора базы данных
                //IfxConnection conn = new IfxConnection() { ConnectionString = baseConnString };
                //conn.Open();
                //string sqlString = "SELECT * FROM s_point";
                //IntfResultTableType dt = ClassDBUtils.OpenSQL(sqlString, conn);
                //conn.Close();
                //SelectDataBase.SelectDataBase selBase = new SelectDataBase.SelectDataBase(dt.resultData, ofKLADR.FileName, baseConnString, "KLADR");
                //selBase.ShowDialog();
                #endregion

                //открытие сразу формы кладр
                KLADR.KLADR KForm = new KLADR.KLADR(ofKLADR.FileName, baseConnString);
                //this.Hide();
                KForm.ShowDialog();
            }
        }

        private string TryDecrypt(string s)
        {
            try
            {
                return Encryptor.Decrypt(s, null);
            }
            catch
            {
                return "";
            }
        }

        private void miCloudRegister_Click(object sender, EventArgs e)
        {
            try
            {
                //считывание параметров из конфига
                Configuration config = ConfigurationManager.OpenExeConfiguration("KP50.Host.exe");

                string sectionName = "appSettings";
                AppSettingsSection appSettings = (AppSettingsSection)config.GetSection(sectionName);

                List<ConfigKey> confList = new List<ConfigKey>();
                if (appSettings.Settings.Count != 0)
                {
                    foreach (string key in appSettings.Settings.AllKeys)
                    {
                        string value = TryDecrypt(appSettings.Settings[key].Value);
                        if (!string.IsNullOrEmpty(value))
                        {
                            ConfigKey conf = new ConfigKey();
                            conf.val = value;
                            conf.key = key;
                            confList.Add(conf);
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Строки не определены!");
                }
                baseConnString = confList[3].val;
                IfxConnection conn = new IfxConnection() { ConnectionString = baseConnString };
                conn.Open();
                string sqlString = "SELECT * FROM s_point";
                IntfResultTableType dt = ClassDBUtils.OpenSQL(sqlString, conn);
                conn.Close();
                SelectDataBase.SelectDataBase selBase = new SelectDataBase.SelectDataBase(dt.resultData, ofKLADR.FileName, baseConnString, "AddressService");
                selBase.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void UnloadingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FrmUploadInFile upload = new FrmUploadInFile();
            upload.Show();
        }

        private void сгенерироватьПКодToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
             {
                //считывание параметров из конфига
                Configuration config = ConfigurationManager.OpenExeConfiguration("KP50.Host.exe");

                string sectionName = "appSettings";
                AppSettingsSection appSettings = (AppSettingsSection)config.GetSection(sectionName);

                List<ConfigKey> confList = new List<ConfigKey>();
                if (appSettings.Settings.Count != 0)
                {
                    foreach (string key in appSettings.Settings.AllKeys)
                    {
                        string value = TryDecrypt(appSettings.Settings[key].Value);
                        if (!string.IsNullOrEmpty(value))
                        {
                            ConfigKey conf = new ConfigKey();
                            conf.val = value;
                            conf.key = key;
                            confList.Add(conf);
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Строки не определены!");
                }
                baseConnString = confList[3].val;
                DbAdres prm = new DbAdres();
                SrvRun.StartHostProgram(false); // инициализируем Points
                var ret = prm.DbUpdateMovedHousesPkod(baseConnString);

                if (ret.result)
                    MessageBox.Show("Генерация платежных кодов выполнена");
                else
                    MessageBox.Show("Ошибка! " + ret.text);
            }
            catch (IfxException ex)
            {
                MessageBox.Show("Ошибка! " + ex.Message);
            }
        }

        private void кнопкаДляВызоваToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TC.SelectedIndex = 0;
            if (WCFParams.AdresWcfHost.Adres == "")
            {
                TC.SelectedIndex = 0;
                SrvRun.StartHostProgram(false);
            }

            Refresh();
            
            
            //DbAdmin dbadmin = new DbAdmin();
            Oplats dbadmin = new Oplats();
            dbadmin.ReadReestrFromCbb(new FilesImported() { saved_name = "D:\\TestFiles\\reestr1.txt" }, new FilesImported() { saved_name = "D:\\TestFiles\\reestr2.txt" }, Constants.cons_Kernel);


        }

        private void configManaferToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FrmConfigManager frmConfigManager = new FrmConfigManager();
            frmConfigManager.ShowDialog();
        }

        private void updateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SrvRun.WriteMessage("Попытка обновления. Операция может занять несколько минут...");
            SrvRun.StartHostProgram(false);
            Patcher patcher = new Patcher(RunFrom.HostMan);
            if (!patcher.CheckPatches(RunFrom.HostMan))
            {
                SrvRun.WriteMessage("Ошибка обновления. Обратитесь к разработчику.");
                return;
            }
            else SrvRun.WriteMessage("Операция успешно завершена.");
        }

        private void сервисToolStripMenuItem_Click(object sender, EventArgs e)
        {
            сервисToolStripMenuItem.Visible = true;
        }

        private void выгрузкаДанныхToolStripMenuItem_Click(object sender, EventArgs e)
        {
            srv_EPasp epasp = new srv_EPasp();
            if(Points.Pref == null)
                SrvRun.StartHostProgram(false);
            DateTime date = new DateTime(Points.CalcMonth.year_, Points.CalcMonth.month_, 1);
            //данные за предыдущий месяц
            date.AddMonths(-1);
            IntfResultType res = epasp.PrepareEPaspXml(date.Year, date.Month, 23);
            if (res.resultCode != 0)
            {
                MessageBox.Show(res.resultMessage);
            }
        }

        private void hostOrConnectConfigsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofDialog = new OpenFileDialog();
            ofDialog.Filter = "Config files|*.config";
            if (ofDialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            List<string> param = new List<string>();
            using (StreamReader sr = new StreamReader(ofDialog.FileName))
            {
                string str = sr.ReadLine();
                if (!str.Contains("<connectionStrings>") && !str.Contains("<appSettings>"))
                {
                    MessageBox.Show("Не определен тип файла!");
                    return;
                }

                if (str.Contains("<connectionStrings>"))
                {
                    grid = enGrid.NewWeb;
                }
                else
                {
                    grid = enGrid.NewHost;;
                }

                while ((str = sr.ReadLine()) != null)
                {
                    if (str.Contains("<add"))
                    {
                        param.Add(str);
                    }
                }
            }

            if (param.Count == 0)
            {
                MessageBox.Show("Не найдены строки!");
                return;
            }

            this.cnfGrid = new SrcHostMan().LoadNewConfig(param);
            this.config_file = ofDialog.FileName;
            this.TC.SelectedIndex = 1;
            this.ConfigGrid();
            this.EnabledConfigMenu();
        }

        private void управлениеБанкамиДанныхToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FrmBasePointsManager dialog = new FrmBasePointsManager();
            dialog.ShowDialog();
        }

        private void createNewBankToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new FrmCreateNewBank().ShowDialog();
        }

        private void ClearBankToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Visible = false;
            new ClearBank().ShowDialog();
            this.Visible = true;
        }

        private void выгрузкаХарактеристикЖКУToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Visible = false;
            new DataUnload().ShowDialog();
            this.Visible = true;
        }

        private void SetFileLoadProgress(int CurrentProgress, int TotalProgress)
        {
            if (InvokeRequired)
            {
                this.Invoke(new Action<int, int>(SetFileLoadProgress), new object[] {CurrentProgress, TotalProgress});
                return;
            } 
            progressBar1.Maximum = TotalProgress;
            progressBar1.Value = CurrentProgress;
            progressBar1.Visible = progressBar1.Maximum != progressBar1.Value;
        }

        private void WriteExecResult(string Message)
        {
            MessageBox.Show(Message, "Выполнено");
        }

        private void administratorToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            if (WCFParams.AdresWcfHost.Adres == "")
            {
                //запуск хоста
                SrvRun.StartHostProgram(false);
            }

            //проверка на наличие файлов с расширением "*.sq" в папке patches
            string path = @"PatchesBars\";

            //проверяем директорию, если не существует - создаем
            if (!Directory.Exists(System.IO.Path.GetDirectoryName(path)))
            {
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
            }

            //считываем все файлы с расширением "*.sq"
            FileInfo[] sqFiles = new DirectoryInfo(path).GetFiles("*.sq");

            if (sqFiles.Count() == 0)
            {
                MessageBox.Show("Обновлений не найдено", "Обновлений не найдено", MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }
            if (MessageBox.Show("Обнаружен пакет обновления для базы данных. Обновить базу?", "Найдено обновление",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
            {
                return;
            }
           
            DbSqAdmin sq = new DbSqAdmin();
            sq.oneFileLoad += new DbSqAdmin.FileLoadEventHandler(SetFileLoadProgress);
            sq.SendExecResult += new DbSqAdmin.ExecResultEventHandler(WriteExecResult);
            Thread th = new Thread(new ParameterizedThreadStart(sq.Run));
            th.Start(sqFiles);
            
        }

        private void обновленияToolStripMenuItem_Click(object sender, EventArgs e)
        {
            обновленияToolStripMenuItem.Visible = (!обновленияToolStripMenuItem.Visible);
        }

        //private void Mn_Save_Click(object sender, EventArgs e) {
        //    if (grid == enGrid.None)
        //    {
        //        EmptyMenu();
        //        return;
        //    }
        //    TC.SelectedIndex = 0;

        //    SrcHostMan hm = new SrcHostMan();
        //    if (grid == enGrid.Pwd)
        //    {
        //        SrvRun.WriteMessage(hm.Save(pwdGrid));
        //    }
        //    else if ((grid == enGrid.NewHost) || (grid == enGrid.NewWeb))
        //    {
        //        SrvRun.WriteMessage(hm.SaveToFile(cnfGrid, grid, config_file));
        //    }
        //    else
        //    {
        //        SrvRun.WriteMessage(hm.Save(cnfGrid, grid, config_dir));
        //    }
        //    EmptyMenu();
        //}

        private void CreateCMDFile_Click(object sender, EventArgs e) {
            config_dir = config_file.Substring(0, config_file.LastIndexOf('\\'));
            TC.SelectedIndex = 0;
            var hm = new SrcHostMan();
            if ((grid == enGrid.NewHost) || (grid == enGrid.NewWeb))
            {
                SrvRun.WriteMessage(hm.SaveToCMDFile(cnfGrid, grid, config_dir));
            }
        }

    }
}
