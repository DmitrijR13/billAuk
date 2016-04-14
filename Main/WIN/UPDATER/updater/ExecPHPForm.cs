using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Collections;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using VersionCompile;

namespace updater
{
    public partial class ExecPHPForm : Form
    {
        delegate void AddInfoCallback(PHPInfo info);//для статуса обновления в таблице
        delegate void AddExecRajonCallback();//для статуса обновления в таблице

        ArrayList AvailableRajons = new ArrayList();

        int AllRajons = 0, ExecRajons = 0;

        private List<int> rajonTabs = new List<int>();
        private List<DataGridView> rajonGrids = new List<DataGridView>();
        private string[,] columnHeaders = new string[12, 2]; // пока используется один столбец, но позже будет 2 (для красивых назаний стольбцов)
        private const string jsonRE = "\"value\":\"(?<value>[\\d- :]+)\", \"caption\":\"(?<caption>[\\w]+)\"";

        private void AddInfo(PHPInfo info)
        {
            if (this.tabctrlPHP.InvokeRequired)
            {
                AddInfoCallback d = new AddInfoCallback(AddInfo);
                this.Invoke(d, new object[] { info });
            }
            else
            {
                AddInfoRow(info);
            }
        }

        private void AddExecRajonInfo()
        {
            if (this.statusStrip1.InvokeRequired)
            {
                AddExecRajonCallback d = new AddExecRajonCallback(AddExecRajonInfo);
                this.Invoke(d, new object[] {});
            }
            else
            {
                AddExecRajon();
            }
        }

        public void AddExecRajon()
        {
            lock (statusStrip1)
            {
                ExecRajons++;
                toolStatus.Text = "Выполнено " + ExecRajons.ToString() + " из " + AllRajons.ToString();
                //statusStrip1.Refresh();
            }
        }

        public void ExecPHP(ArrayList rajon)
        {
            string rajon_number = rajon[0].ToString();
            string rajon_name = rajon[1].ToString();
            string rajon_ip = rajon[2].ToString();
            PHPInfo info = new PHPInfo();
            info.rajon_name = rajon_name;
            info.rajon_number = int.Parse(rajon_number);

            WebRequest request = WebRequest.Create(rajon_ip + @"/faktura/pages/get_pgu.php");
            request.ContentType = "application/json; charset=utf-8";
            string text;
            try
            {
                WebResponse response = request.GetResponse();

                using (var sr = new StreamReader(response.GetResponseStream()))
                {
                    text = sr.ReadToEnd();
                }

                Dictionary<string, string> dic = ParseJson(text);

                info.dat_when = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                info.dat_pgu = dic["dat_pgu"];
                info.cnt_access = dic["cnt_access"];
                info.cnt_ls = dic["cnt_ls"];
                info.cnt_device = dic["cnt_device"];
                info.cnt_faktura = dic["cnt_faktura"];
                info.cnt_pay = dic["cnt_pay"];
                info.cnt_access_day = dic["cnt_access_day"];
                info.cnt_ls_day = dic["cnt_ls_day"];
                info.cnt_device_day = dic["cnt_device_day"];
                info.cnt_faktura_day = dic["cnt_faktura_day"];
                info.cnt_pay_day = dic["cnt_pay_day"];

                Database DB = new Database();
                DB.Insert_PHP_info(PathsAndKeys.DB_Connect, info);
                AddInfo(info);
            }
            catch
            {
                //
            }
            finally
            {
                AddExecRajon();
            }
        }

        public void AddInfoRow(PHPInfo info)
        {
            lock (rajonGrids)
            {
                lock (rajonTabs)
                {
                    DataGridView dgv = new DataGridView();
                    if (rajonTabs.IndexOf(info.rajon_number) == -1)
                    {
                        rajonTabs.Add(info.rajon_number);

                        TabPage tbpg = new TabPage();
                        tbpg.Name = "tbpg" + info.rajon_number.ToString();
                        tbpg.Text = info.rajon_name;

                        dgv.Name = "dgv" + info.rajon_number.ToString();
                        int n = columnHeaders.GetLength(0);
                        for (int i = 0; i < columnHeaders.GetLength(0); i++)
                        {
                            dgv.Columns.Add(columnHeaders[i, 0] + info.rajon_number, columnHeaders[i, 0]);
                        }

                        dgv.Parent = tbpg;
                        dgv.Dock = DockStyle.Fill;
                        dgv.ReadOnly = true;
                        dgv.AllowUserToAddRows = false;
                        dgv.AllowUserToDeleteRows = false;
                        dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
                        dgv.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
                        rajonGrids.Add(dgv);
                        tabctrlPHP.TabPages.Add(tbpg);
                    }
                    else
                    {
                        foreach (DataGridView temp in rajonGrids)
                        {
                            if (temp.Name == "dgv" + info.rajon_number.ToString())
                            {
                                dgv = temp;
                                break;
                            }
                        }
                    }
                    dgv.Rows.Add(info.dat_when, info.dat_pgu, info.cnt_access, info.cnt_ls, info.cnt_device, info.cnt_faktura, info.cnt_pay, info.cnt_access_day, info.cnt_ls_day, info.cnt_device_day, info.cnt_faktura_day, info.cnt_pay_day);
                }
            }
        }

        public ExecPHPForm(ArrayList Rajons)
        {
            InitializeComponent();

            AvailableRajons = Rajons;

            columnHeaders[0, 0] = "dat_when";
            columnHeaders[1, 0] = "dat_pgu";
            columnHeaders[2, 0] = "cnt_access";
            columnHeaders[3, 0] = "cnt_ls";
            columnHeaders[4, 0] = "cnt_device";
            columnHeaders[5, 0] = "cnt_faktura";
            columnHeaders[6, 0] = "cnt_pay";
            columnHeaders[7, 0] = "cnt_access_day";
            columnHeaders[8, 0] = "cnt_ls_day";
            columnHeaders[9, 0] = "cnt_device_day";
            columnHeaders[10, 0] = "cnt_faktura_day";
            columnHeaders[11, 0] = "cnt_pay_day";

            foreach (Info rajon in Rajons)
            {
                string[] ip = rajon.rajon_ip.Split(new char[] {':'});
                if (ip[1].Length > 5)
                {
                    dgvPHPRajons.Rows.Add(rajon.rajon_number, rajon.rajon_name, "http:" + ip[1]);
                }
            }

            Database DB = new Database();
            string str_sql = @"SELECT * FROM rajon_execphp";
            List<PHPInfo> history = DB.Get_PHP_Info(PathsAndKeys.DB_Connect, str_sql);

            foreach (PHPInfo info in history)
            {
                AddInfoRow(info);
            }
        }

        private void btnExecPHP_Click(object sender, EventArgs e)
        {
            btnExecPHP.Enabled = false;
            ExecRajons = 0;
            AllRajons = dgvPHPRajons.SelectedRows.Count;

            foreach (DataGridViewRow dr in dgvPHPRajons.SelectedRows)
            {
                string strNumber = dr.Cells["colNumber"].Value.ToString();
                string strName = dr.Cells["colName"].Value.ToString();
                string strIp = dr.Cells["colIp"].Value.ToString();
                //ExecPHP(strNumber, strName, strIp);
                ArrayList arr = new ArrayList();
                arr.Add(strNumber);
                arr.Add(strName);
                arr.Add(strIp);
                ThreadPool.QueueUserWorkItem(delegate(object notUsed) { ExecPHP(arr); });
                //System.Threading.Thread.Sleep(100);
                //ThreadPool.QueueUserWorkItem(delegate(object notUsed) { ExecPHP(dr.Cells["colNumber"].Value.ToString(), dr.Cells["colName"].Value.ToString(), dr.Cells["colIp"].Value.ToString()); });
                //System.Threading.Thread.Sleep(500);
            }

            btnExecPHP.Enabled = true;
        }

        static Dictionary<string, string> ParseJson(string res)
        {
            Dictionary<string, string> retDic = new Dictionary<string, string>();

            // проверка на const
            Regex _Regex = new Regex(jsonRE);
            MatchCollection _Matches = _Regex.Matches(res);

            foreach (Match match in _Matches)
            {
                retDic.Add(match.Groups["caption"].ToString(), match.Groups["value"].ToString());
            }

            return retDic;
        }
    }
}
