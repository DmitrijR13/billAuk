using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Server;
using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using STCLINE.KP50.WinLogin;


namespace STCLINE.KP50.HostMan.SOURCE
{
    public partial class ClearBank : Form
    {
        protected StatusBar mainStatusBar = new StatusBar();
        protected StatusBarPanel statusPanel = new StatusBarPanel();
        protected StatusBarPanel datetimePanel = new StatusBarPanel();

        GetBanksClass gbc = new GetBanksClass();
        public ClearBank()
        {
            InitializeComponent();
        }

        private void ClearBank_Load(object sender, EventArgs e)
        {
            SrvRun.StartHostProgram(false);
            Banks.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            //получаем список банков
            List<Bank> lb = gbc.GetLocalBanks();
            lb.ForEach(bank=>Banks.Items.Add(bank));
            CreateStatusBar();
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            FrmEnterPwd pwdFrm = new FrmEnterPwd();
            pwdFrm.ShowDialog();
            if (pwdFrm.DialogResult != DialogResult.OK)
            {
                return;
            }


            int count = 0;
            string tochno = "";
            do
            {
                string[] args = new string[count];
                for (int t = 0; t < count; t++) args[t] = "точно";
                tochno = string.Join("-", args);
                count++;
            } while (count <= 5 &&
                     MessageBox.Show(
                         string.Format("Вы {0} уверены, что хотите очистить банк {1}?", tochno, Banks.SelectedText),
                         "Важный вопрос!!", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes);

            if (count > 5)
            {
                MessageBox.Show("Процесс очистки банка запущен", ">_<'", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                statusPanel.Text = "Очистка банка " + Banks.SelectedItem;
                gbc.ClearLocalbank(Banks.SelectedItem as Bank);
                statusPanel.Text = "Банк " + Banks.SelectedItem + " очищен";
            }
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Строка состояния
        /// </summary>
        private void CreateStatusBar()
        {
            // Set first panel properties and add to StatusBar
            statusPanel.BorderStyle = StatusBarPanelBorderStyle.Sunken;
            statusPanel.Text = "";
            statusPanel.ToolTipText = "Last Activity";
            statusPanel.AutoSize = StatusBarPanelAutoSize.Spring;
            mainStatusBar.Panels.Add(statusPanel);

            // Set second panel properties and add to StatusBar
            datetimePanel.BorderStyle = StatusBarPanelBorderStyle.Raised;
            datetimePanel.ToolTipText = "DateTime: " + System.DateTime.Today.ToString();
            datetimePanel.Text = System.DateTime.Today.ToLongDateString() + " .";
            datetimePanel.AutoSize = StatusBarPanelAutoSize.Contents;
            mainStatusBar.Panels.Add(datetimePanel);

            mainStatusBar.ShowPanels = true;
            // Add StatusBar to Form controls
            this.Controls.Add(mainStatusBar);
        }

    }

    public class Bank
    {
        public string Text { get; private set; }
        public int Value { get; private set; }

        public string Pref { get; private set; }

        public override string ToString()
        {
            return Text;
        }

        public Bank(string name, int num, string pref)
        {
            this.Text = name;
            this.Value = num;
            this.Pref = pref;
        }
    }

    public class GetBanksClass : DataBaseHead
    {
        public List<Bank> GetLocalBanks()
        {
            
            IDbConnection conn = GetConnection(Constants.cons_Kernel); IDataReader reader = null;
            List<Bank> banks = new List<Bank>();
            Returns ret = Utils.InitReturns();
            string sql = String.Format("SELECT nzp_wp, point, bd_kernel FROM {0}_kernel{1}s_point WHERE nzp_graj = 1;", Points.Pref, DBManager.tableDelimiter);
            try
            {
                conn.Open();
                ret = ExecRead(conn, out reader, sql, true);
                while (reader.Read())
                    if (reader["nzp_wp"] != DBNull.Value)
                    {
                        string name = reader["point"].ToString().Trim();
                        int num = Convert.ToInt32(reader["nzp_wp"]);
                        string pref = reader["bd_kernel"].ToString().Trim();
                        Bank b = new Bank(name, num, pref);
                        banks.Add(b);
                    }
            }
            catch
            {
                MessageBox.Show("Ошибка при получении списка банков");
            }
            finally
            {
                conn.Close();
            }
            return banks;
        }

        public void ClearLocalbank(params Bank[] bank)
        {
            foreach (Bank b in bank)
            {
                IDbConnection conn = GetConnection(Constants.cons_Kernel); 
                try
                {
                    Returns ret = new DbClearBase(conn).ClearBase(new FilesDisassemble() {bank = b.Pref, nzp_user = 1});
                    if (!ret.result)
                    {
                        MessageBox.Show("Банк " + b.Text + " очищен с ошибками!! Смотри лог ошибок");
                    }
                    else
                    {
                        MessageBox.Show("Банк " + b.Text + " успешно очищен");
                    }
                }
                catch
                {
                    MessageBox.Show("Банк " + b.Text + " очищен с ошибками!! Смотри лог ошибок");
                }
                finally
                {
                    conn.Close();
                }
            }

        }
    }

}
