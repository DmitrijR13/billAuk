using System;
using System.Windows.Forms;
using Bars.KP50.DataImport.SOURCE.EXCHANGE;
using STCLINE.KP50.Global;
using STCLINE.KP50.Server;

namespace STCLINE.KP50.HostMan.SOURCE
{
    public partial class DataUnload : Form
    {
        FilesExchange fileExch = new FilesExchange();

        public DataUnload()
        {
            InitializeComponent();
            if (WCFParams.AdresWcfHost.Adres == "")
            {
                //запуск хоста
                SrvRun.StartHostProgram(false);
            }
            
            #region заполнение списка месяцев
            //string[] months = { "Январь", "Февраль", "Март", "Апрель", "Май", "Июнь", "Июль", "Август", "Сентябрь", "Октябрь", "Ноябрь", "Декабрь" };
            //cbMonth.DataSource = months;
            //cbMonth.SelectedIndex = int.Parse(DateTime.Now.ToString("MM")) - 1;//выбираем текущий месяц
            #endregion заполнение списка месяцев

            #region заполнение списка годов
            //for (int i = 2012; i <= int.Parse(DateTime.Now.ToString("yyyy")); i++)
            //{
            //    cbYear.Items.Add(i);
            //}
            //cbYear.SelectedIndex = cbYear.Items.Count - 1;//выбираем последний год
            #endregion заполнение списка годов

            #region заполнение списка банков
            GetBanksClass tmpBankClass = new GetBanksClass();
            var banks = tmpBankClass.GetLocalBanks();
            tmpBankClass.Close();

            foreach (var bank in banks)
            {
                cbBankList.Items.Add(bank);
            }
            cbBankList.SelectedIndex = 0;
            #endregion заполнение списка банков

            #region заполнение списка версий
            cbVersion.Items.Add("1.0");
            cbVersion.Items.Add("1.2.1");
            cbVersion.Items.Add("1.2.2");
            cbVersion.Items.Add("1.3.2");
            cbVersion.SelectedIndex = 3;//по умолчанию - версия 1.3.2
            #endregion заполнение списка версий
            
        }

        private void button1_Click(object sender, EventArgs e)
        {
            
            Returns ret = new Returns();

            try
            {

                #region выбор банка данных

                var bankInfo = cbBankList.SelectedItem as Bank;
                if ((cbBankList.SelectedItem as Bank) != null)
                {
                    fileExch.bankPref = bankInfo.Pref.Trim();
                }
                else
                {
                    MessageBox.Show("Не выбран банк данных!");
                }

                #endregion выбор банка данных

                

                fileExch.versionName = cbVersion.SelectedItem.ToString().Trim();
                fileExch.exchangeType = 2;
                fileExch.selectedDate = monthCalendar1.SelectionEnd;


                switch (fileExch.versionName)
                {
                        //версия "1.0"
                    case "1.0":
                    {
                        //
                        break;
                    }
                        //версия "1.2.1"
                    case "1.2.1":
                    {
                        //
                        break;
                    }
                        //версия "1.2.2"
                    case "1.2.2":
                    {
                        #region заполнение выбранных секций для выгрузки
                        fileExch.selectedSections.Clear();
                        fileExch.selectedSections.Add("Uk122", chbUk.Checked);
                        fileExch.selectedSections.Add("Houses122", chbHouse.Checked);
                        fileExch.selectedSections.Add("Ls122", chbLs.Checked);
                        fileExch.selectedSections.Add("Supp122", chbSupp.Checked);
                        fileExch.selectedSections.Add("Serv122", chbServ.Checked);
                        
                        #endregion заполнение выбранных секций для выгрузки
                        //Создание экземпляра класса для версии 1.2.2 и вызов его метода выгрузки
                        using (UnlFormat_1_2_2 unl = new UnlFormat_1_2_2())
                        {
                            ret = unl.Run(fileExch);
                        }
                        break;
                    }
                    case "1.3.2":
                    {
                        #region заполнение выбранных секций для выгрузки
                        fileExch.selectedSections.Clear();
                        fileExch.selectedSections.Add("Urlic132", chbUrlic.Checked);
                        fileExch.selectedSections.Add("Houses132", chbHouse.Checked);
                        fileExch.selectedSections.Add("Ls132", chbLs.Checked);
                        fileExch.selectedSections.Add("Supp132", chbSupp.Checked);
                        fileExch.selectedSections.Add("Serv132", chbServ.Checked);
                        fileExch.selectedSections.Add("Odpu", chbOdpu.Checked);
                        fileExch.selectedSections.Add("PokazaniaOdpu", chbOdpuP.Checked);
                        fileExch.selectedSections.Add("Ipu", chbIpu.Checked);
                        fileExch.selectedSections.Add("PokazaniaIpu", chbIpuP.Checked);
                        fileExch.selectedSections.Add("MoInfo", chbMO.Checked);
                        fileExch.selectedSections.Add("GilecInfo", chbGilec.Checked);
                        #endregion заполнение выбранных секций для выгрузки
                        //Создание экземпляра класса для версии 1.3.2 и вызов его метода выгрузки
                        using (UnlFormat_1_3_2 unl = new UnlFormat_1_3_2())
                        {
                            ret = unl.Run(fileExch);
                        }
                        break;
                    }
                }
                MessageBox.Show(ret.text);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка при выполнении выгрузки:" + ex.Message + ex.StackTrace,MonitorLog.typelog.Error, true);
                MessageBox.Show("Ошибка при выполнении выгрузки! Смотрите лог ошибок!");
            }

        }

        private void DataUnload_Load(object sender, EventArgs e)
        {

        }
        
    }
}
