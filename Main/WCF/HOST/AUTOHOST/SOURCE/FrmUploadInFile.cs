using System;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Collections.Generic;
using System.IO;

using STCLINE.KP50.Server;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.DataBase;
using SevenZip;
using STCLINE.KP50.Client;
using System.Data.OleDb;
using System.Configuration;
using IBM.Data.Informix;
using STCLINE.KP50.HostMan.SelectDataBase;
using Bars.CloudRoster.Contracts.Data;
using System.Threading;
using STCLINE.KP50.HostMan.KLADR;
namespace STCLINE.KP50.HostMan.SOURCE
{
    public partial class FrmUploadInFile : Form
    {
        private Loading.Loading loadForm;
        private Thread thread;

        // Закрытие окна прогрессбара
        delegate void CloseProgressBarCallBack(Loading.Loading form);
        private void CloseProgressBar(Loading.Loading form)
        {
            if (form.InvokeRequired)
            {
                form.BeginInvoke(new CloseProgressBarCallBack(CloseProgressBar), form);
            }
            else
            {
                form.Close();
            }
        }

        // Установка значения прогрессбара
        delegate void SetProgressBarCallBack(Loading.Loading form, int val);
        private void SetProgressBar(Loading.Loading form, int val)
        {
            if (form.InvokeRequired)
            {
                form.BeginInvoke(new SetProgressBarCallBack(SetProgressBar), form, val);
            }
            else
            {
                form.SetValue(val);
            }
        }
        public FrmUploadInFile()
        {
            InitializeComponent();
            comboBox1.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox2.DropDownStyle = ComboBoxStyle.DropDownList;
            string[] months = { "Январь", "Февраль", "Март", "Апрель", "Май", "Июнь", "Июль", "Август", "Сентябрь", "Октябрь", "Ноябрь", "Декабрь" };
            comboBox1.DataSource = months;
            for (int i = 1990; i <= int.Parse(DateTime.Now.ToString("yyyy")); i++)
            {
                comboBox2.Items.Add(i);
            }
            comboBox1.SelectedIndex = int.Parse(DateTime.Now.ToString("MM")) - 2;
            comboBox2.SelectedIndex = comboBox2.Items.Count - 1;
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

        private void button1_Click(object sender, EventArgs e)
        {
            bool flag = checkBox1.Checked;
            string month = "";
            string year = "";
            if (comboBox1.SelectedIndex + 1 < 10)
            {
                month = "0" + (comboBox1.SelectedIndex + 1).ToString();
            }
            else
            {
                month = (comboBox1.SelectedIndex + 1).ToString();
            }
            year = comboBox2.SelectedItem.ToString();

            string baseConnString = "";

            loadForm = new Loading.Loading("Выполняется выгрузка");
            thread = new Thread(
                   () =>
                   {
                       loadForm.ShowDialog();
                   });
            thread.Start();
            try
            {
                List<string> lPref = new List<string>();
                string Pref = "";
                string sqlString = "";
                MemoryStream memstr = new MemoryStream();
            
                string direct = "";
                string[] dir = Directory.GetCurrentDirectory().Split('\\');
                for (int i=0; i<dir.Length -5; i++)
                {
                    direct += dir[i] + "\\";   
                }

                StreamWriter writer = File.CreateText(direct +"WEB\\WebKomplat5\\files\\" + "Upload_"+ DateTime.Now.ToString("yyyy_MM_dd")+".txt");
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

                sqlString = "SELECT * FROM s_point where nzp_graj=0";
                IntfResultTableType dt = ClassDBUtils.OpenSQL(sqlString, conn);

                Pref = dt.resultData.Rows[0]["bd_kernel"].ToString().Trim();

                sqlString = "SELECT * FROM s_point where nzp_graj=1";
                dt = ClassDBUtils.OpenSQL(sqlString, conn);

                for (int i = 0; i < dt.resultData.Rows.Count; i++)
                {
                    lPref.Add((dt.resultData.Rows[i]["bd_kernel"]).ToString().Trim());
                }
                int count = 0;
                dt = null;



                DbAdres dbAdr = new DbAdres();
                DbCharge dbCh = new DbCharge();
                DbCounter dbCn = new DbCounter();
                DbLsServices dbLs = new DbLsServices();
                SetProgressBar(loadForm, 5);
                writer.WriteLine("                                                                                                          ");
                dbCn.HouseCounters(lPref, writer, conn);
                SetProgressBar(loadForm, 15);
                dbCh.InfAboutChargeServ(lPref, Pref, writer, conn, int.Parse(year), int.Parse(month));
                SetProgressBar(loadForm, 25);
                dbCh.InfoAboutServices(lPref, Pref, writer, conn);
                SetProgressBar(loadForm, 35);
                dbCn.IndivCounters(lPref, writer, conn);
                SetProgressBar(loadForm, 40);
                dbCh.LsUploadPererash(lPref, writer, conn, year.Substring(2), month);
                SetProgressBar(loadForm, 50);
                count += dbAdr.Management_companies(Pref, writer, conn, flag, int.Parse(year), int.Parse(month));
                SetProgressBar(loadForm, 55);
                dbAdr.LsUpload(lPref, writer, conn, flag, int.Parse(year), int.Parse(month));
                SetProgressBar(loadForm, 60);
                count += dbAdr.Homes(lPref, writer, conn, flag, int.Parse(year), int.Parse(month));
                SetProgressBar(loadForm, 65);
                count += dbAdr.WriteLs(writer, conn, flag);
                SetProgressBar(loadForm, 70);
                count += dbLs.Suppliers(lPref, Pref, writer, conn, flag);
                SetProgressBar(loadForm, 75);
                count += dbCh.WriteCharge(writer, conn);
                SetProgressBar(loadForm, 80);
                count += dbCh.WriteLsUploadPererash(writer, conn, flag);
                SetProgressBar(loadForm, 85);
                count += dbCh.WriteInfAboutChargeServ(writer, conn);
                SetProgressBar(loadForm, 90);
                count += dbCn.WriteHouseCounters(writer, conn);
                SetProgressBar(loadForm, 95);
                count += dbCn.WriteIndivCounters(writer, conn);
                writer.Flush();
                writer.Close();



            
                string ret = dbAdr.Title(Pref, writer, conn, int.Parse(month), int.Parse(year), count);
              


                FileStream fileStream = new FileStream(direct + "WEB\\WebKomplat5\\files\\" + "Upload_"+ DateTime.Now.ToString("yyyy_MM_dd")+".txt", FileMode.Open);
                StreamWriter streamWriter = new StreamWriter(fileStream);
                streamWriter.BaseStream.Seek(0, SeekOrigin.Begin);
                streamWriter.Write(ret);
                streamWriter.Close();
                
                fileStream.Close();


                SetProgressBar(loadForm, 100);
                
                thread.Abort();
                CloseProgressBar(loadForm);

                conn.Close();
                MessageBox.Show("Выгрузка завершена!");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        private void FrmUploadInFile_Load(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {

        }
    }
}
