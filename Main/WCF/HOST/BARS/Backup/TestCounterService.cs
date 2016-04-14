using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using BARS.ServCnt;
using STCLINE.KP50.Global;
using STCLINE.KP50.Bars.Services;
using STCLINE.KP50.Bars.Interfaces;

namespace BARS
{
    public partial class TestCounterService : Form
    {
        CounterResult cr = new CounterResult();

        public TestCounterService()
        {
            InitializeComponent();

            //пока явно определим
            Constants.cons_Webdata = "data source=RUBIN;initial catalog=D:\\Komplat.Lite\\kazan21\\DBKOM_kazan21.fdb;user id=SYSDBA;password=masterkey;dialect=3;port number=3050;connection lifetime=30;pooling=True;packet size=8192;isolation level=ReadCommitted;character set=WIN1251";

            Constants.Login = "Administrator"; //для записей в лог
            Constants.Password = "rubin";

            MonitorLog.StartLog("STCLINE.KP50.BARS", "Старт приложения");

            textBoxNumls.Text = "5448008326";
            txtFlat.Text = "15";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Int64 numID;
            if (!Int64.TryParse(textBoxNumls.Text, out numID)) return;

            AdresID acc = new AdresID();
            acc.numID = Convert.ToInt64(textBoxNumls.Text);
            acc.numFlat = txtFlat.Text;

            GetCounters(acc);
        }

        private void GetCounters(AdresID adresID)
        {
            // вызов через службу
            
            CounterClient client = new CounterClient();

            cr = client.GetCounters(adresID);
            client.Close();

            if (cr.counters != null)
            {
                dataGridView1.DataSource = cr.counters;
            }
            lblRet.Text = cr.retcode.tag + " Сообщение: " + cr.retcode.text;
            
            return;
            
/*
            // вызов напрямую через БД
            //Returns ret = Utils.InitReturns();
            if (adresID.numID < 1 || adresID.numFlat == "")
            {
                ret.tag = Constants.svc_wrongdata;
                //Utils.BarsReturns(ref ret);
                return;
            }
            string pkod = adresID.numID.ToString();
            int kod_erc;
            int num_ls;

            //декодирование платежного кода
            ret = Utils.DecodePKod(pkod, out kod_erc, out num_ls);
            if (!ret.result)
            {
                Utils.BarsReturns(ref ret);
                return;
            }

            //обращение к базе
            DbAdres.GetAdresString(out ret, kod_erc, num_ls, adresID.numFlat);

            if (ret.result)
            {
                counters = DbCounter.GetCounters(out ret, kod_erc, num_ls, adresID.numFlat);

                if (counters != null)
                {
                    dataGridView1.DataSource = counters;
                }
            }
            lblRet.Text = ret.tag + " Сообщение: " + ret.text;
*/            
        }

        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            try
            {
                dataGridView2.DataSource = cr.counters[dataGridView1.SelectedRows[0].Index].values;
            }
            catch
            {
                dataGridView2.DataSource = null;
            }
        }
    }
}
