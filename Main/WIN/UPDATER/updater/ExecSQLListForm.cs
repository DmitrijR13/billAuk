using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using STCLINE.KP50.Client;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using STCLINE.KP50.Interfaces;
using System.Threading;
using System.Collections;
using STCLINE.KP50.Global;
using SevenZip;

namespace updater
{
    public partial class ExecSQLListForm : Form
    {
        private string ip;
        private string login;
        private string pass;
        private string name;

        private string key1 = "l!xU[kDm]Htuvc=EG/aoBSDjCUmG6\\0(}HFsN+2mv,jEvOT]4VTjKT6s!)ib]aKl.%n3+~\"nW6x*x84_)?I[E3F\\jgh2vJKPf9=ze]U0:(_+c\\/yfb1,8)vt[6/ZKwf,";
        private string key2 = "G№%J37)\"/;>/]s>AOxb6v~4\"[=88LiMX~Y?81,\\A!\"xyJkwl1m,9?{;n]4i\\L+Bi\\\"jzzFYN3xMpt:Ocq2g!TT{f8L;Qs,=4v:5R1p\\cG\\9zq[*UN:P2YA\\zm\\c8THt5";

        public ExecSQLListForm()
        {
            InitializeComponent();
        }

        public ExecSQLListForm(string newip, string newlogin, string newpass, string newname)
        {
            ip = newip;
            login = newlogin;
            pass = newpass;
            name = newname;
            InitializeComponent();
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            rtbAfter.Text = "";
            // получение даты последнего скрипта, дабы отличать новый от старого
            cli_Patch cli = new cli_Patch(ip + "/srv", login, pass);
            Stream stream = cli.GetHistoryLast(3);
            BinaryFormatter bf = new BinaryFormatter();
            UpData2[] lud2 = (UpData2[])(bf.Deserialize(stream));
            string LastUpdDate = lud2[0].date; // вот тут она и хранится

            string[] strings = rtbBefore.Lines;
            bf = new BinaryFormatter();
            MemoryStream ms = new MemoryStream();
            bf.Serialize(ms, strings.ToArray());
            ms.Seek(0, SeekOrigin.Begin);
            cli = new cli_Patch(ip + "/srv", login, pass);
            ThreadPool.QueueUserWorkItem(delegate(object notUsed) { cli.ExecSQLList(ms); });
            //cli.ExecSQLList(ms);
            System.Threading.Thread.Sleep(10000);

            DateTime SctiptStart = DateTime.Now;
            string NewUpdDate = LastUpdDate;
            bool ContinueWaiting = true;
            do
            {
                System.Threading.Thread.Sleep(5000);
                bool connect = false;
                do
                {                    
                    try
                    {
                        cli = new cli_Patch(ip + "/srv", login, pass);
                        connect = cli.CheckConn() == 1;
                    }
                    catch
                    {
                        connect = false;
                    }
                    Application.DoEvents();
                }
                while (!connect);

                string ConStatus = "Район недоступен";
                if (connect)
                {
                    ConStatus = "Район доступен";
                    try
                    {
                        cli = new cli_Patch(ip + "/srv", login, pass);
                        stream = cli.GetHistoryLast(3);
                        bf = new BinaryFormatter();
                        lud2 = (UpData2[])(bf.Deserialize(stream));
                        NewUpdDate = lud2[0].date;
                    }
                    catch
                    {
                        NewUpdDate = LastUpdDate;
                    }
                }
                if (DateTime.Now - SctiptStart > new TimeSpan(0, 30, 0))
                {
                    if (MessageBox.Show("Данных о завершении скрипта нет более получаса.\r\nПодождать еще?\r\n\r\n" + ConStatus, this.name, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                    {
                        SctiptStart = DateTime.Now;
                    }
                    else
                    {
                        ContinueWaiting = false;
                    }
                }
            }
            while ((NewUpdDate == LastUpdDate) && (ContinueWaiting));

            if (lud2[0].status == "-1")
            {
                rtbAfter.Text = "Ошибка выполнения операции!";
            }
            else
            {
                ArrayList al = new ArrayList();

                cli = new cli_Patch(ip + "/srv", login, pass);
                Stream stream2 = cli.GetSelect();
                BinaryFormatter bf2 = new BinaryFormatter();
                byte[] bytes;
                string FilePath = Path.GetTempPath() + "select";
                try
                {
                    bytes = (byte[])bf.Deserialize(stream2);
                    File.WriteAllBytes(FilePath + ".7z", bytes);
                }
                catch(Exception ex)
                {
                    rtbAfter.Text = "Ошибка получения файла " + ex.Message;
                    MonitorLog.WriteLog("Ошибка получения файла " + ex.Message, MonitorLog.typelog.Error, true);
                }
                try
                {
                    SevenZipExtractor.SetLibraryPath(Directory.GetCurrentDirectory() + @"\7z.dll");
                    SevenZipExtractor extractor = new SevenZipExtractor(FilePath + ".7z", key2);
                    extractor.ExtractArchive(Path.GetTempPath());
                    File.Delete(FilePath + ".7z");
                }
                catch(Exception ex)
                {
                    rtbAfter.Text = "Ошибка распаковки файла " + ex.Message;
                    MonitorLog.WriteLog("Ошибка распаковки файла " + ex.Message, MonitorLog.typelog.Error, true);
                }

                StreamReader sr = new StreamReader(Path.GetTempPath() + "select.txt");
                string str;
                while ((str = sr.ReadLine()) != null)
                {
                    al.Add(Crypt.Decrypt(str, key1));
                }
                sr.Close();

                if (al.Count > 0)
                {
                    foreach (string str2 in al)
                    {
                        rtbAfter.Text += str2 + "\r\n";
                    }
                }
                else
                {
                    rtbAfter.Text = "Скрипт не вернул результата";
                }
            }
        }

        private void ExecSQLListForm_Resize(object sender, EventArgs e)
        {
            rtbBefore.Width = Width / 2;
            lbBefore.Left = (rtbBefore.Width / 2) - 30;
            lbAfter.Left = (3 * rtbBefore.Width / 2) - 30;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (dlgSave.ShowDialog() == DialogResult.OK)
            {
                StreamWriter sw = new StreamWriter(dlgSave.FileName);
                sw.Write(rtbAfter.Text);
                sw.Close();
            }
        }

        private void btnTable_Click(object sender, EventArgs e)
        {
            ExecSQLListTable formESLT = new ExecSQLListTable();
            formESLT.dgvTable.Columns.Add("№", "№");
            string[] Header = rtbAfter.Lines[0].Split(new char[] { '|' });
            List<string> mylist = new List<string>();
            for (int i = 1; i < rtbAfter.Lines.Length; i++)
            {
                mylist.Add(rtbAfter.Lines[i]);
            }
            foreach (string str in mylist)
            {
                if (str != "")
                {
                    string strtemp = formESLT.dgvTable.RowCount.ToString() + '|' + str;
                    string[] values = strtemp.Split(new char[] { '|' });
                    while (values.Length > formESLT.dgvTable.ColumnCount)
                    {
                        //formESLT.dgvTable.Columns.Add("Column" + formESLT.dgvTable.ColumnCount, "Column" + formESLT.dgvTable.ColumnCount);
                        formESLT.dgvTable.Columns.Add(Header[formESLT.dgvTable.ColumnCount - 1], Header[formESLT.dgvTable.ColumnCount - 1]);
                    }
                    formESLT.dgvTable.Rows.Add(values);
                }
            }
            //formESLT.dgvTable.AutoResizeColumns( DataGridViewAutoSizeColumnsMode.AllCells );
            formESLT.Show();
        }
    }
}
