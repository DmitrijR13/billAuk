using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Server;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace STCLINE.KP50.HostMan.SOURCE
{
    public partial class FrmCreateNewBank : Form
    {
        private Thread thObjBank;
        private CreateBank objCreateBank;

        private short[] shortDBCreated;

        public void SetEnabled(bool Enabled = false)
        {
            if (InvokeRequired)
            {
                this.Invoke(new Action<bool>(SetEnabled), new object[] { Enabled });
                return;
            }

            this.tbxNewBankName.Enabled = Enabled;
            this.tbxNewBankPrefix.Enabled = Enabled;
            this.tbxNewBankPrefix.Focus();
            this.btnAddNewBank.Enabled = Enabled;
        }

        public void WriteLog(string value)
        {
            if (InvokeRequired)
            {
                this.Invoke(new Action<string>(WriteLog), new object[] { value });
                return;
            }
            this.tbxLog.Text += value;
        }

        public void SetPrefixValue(string value)
        {
            if (InvokeRequired)
            {
                this.Invoke(new Action<string>(SetPrefixValue), new object[] { value });
                return;
            }
            this.tbxNewBankPrefix.Text = value;
        }

        public FrmCreateNewBank()
        {
            InitializeComponent();
        }

        private void FrmCreateNewBank_Load(object sender, EventArgs e)
        {
            this.shortDBCreated = new short[3];
            SrvRun.StartHostProgram(false);
            this.objCreateBank = new CreateBank(this);
            this.thObjBank = new Thread(new ThreadStart(this.objCreateBank.Prepare));
            this.thObjBank.Start();
        }

        private void btnAddNewBank_Click(object sender, EventArgs e)
        {
            SetEnabled();
            Thread thKernel = new Thread(new ThreadStart(MakeKernel));
            Thread thData = new Thread(new ThreadStart(MakeData));
            Thread thCharge = new Thread(new ThreadStart(MakeCharge));
            Thread thUpdate = new Thread(new ThreadStart(UpdatePoints));
            thKernel.Start();
            thData.Start();
            thCharge.Start();
            thUpdate.Start();
        }

        private void MakeKernel()
        {
            if (this.objCreateBank.CreateDataBases(CreateBank.DataBase.Kernel, this.tbxNewBankPrefix.Text))
            {
                this.shortDBCreated[0] = 1;
                WriteLog("База данных Kernel успешно создана.\r\n");
            }
            else
            {
                this.shortDBCreated[0] = 2;
                WriteLog("Ошибка создания базы данных Kernel.\r\n");
            }
        }
        private void MakeData()
        {
            if (this.objCreateBank.CreateDataBases(CreateBank.DataBase.Data, this.tbxNewBankPrefix.Text))
            {
                this.shortDBCreated[1] = 1;
                WriteLog("База данных Data успешно создана.\r\n");
            }
            else
            {
                this.shortDBCreated[1] = 2;
                WriteLog("Ошибка создания базы данных Data.\r\n");
            }
        }
        private void MakeCharge()
        {
            if (this.objCreateBank.CreateDataBases(CreateBank.DataBase.Charge, this.tbxNewBankPrefix.Text))
            {
                this.shortDBCreated[2] = 1;
                WriteLog("База данных Charge успешно создана.\r\n");
            }
            else
            {
                this.shortDBCreated[2] = 2;
                WriteLog("Ошибка создания базы данных Charge.\r\n");
            }
        }
        private void UpdatePoints()
        {
            bool boolCreated;
            do
            {
                boolCreated = true;
                foreach (short st in this.shortDBCreated) if (st == 0) { boolCreated = false; break; }
                Thread.Sleep(1000);
            } while (!boolCreated);
            this.objCreateBank.UpdateLinks(this.tbxNewBankPrefix.Text, this.tbxNewBankName.Text);
            WriteLog("Банк данных успешно создан.\r\n");
        }
    }

    public class CreateBank : DataBaseHead
    {
        [Flags]
        public enum DataBase
        {
            Kernel,
            Data,
            Charge
        }
        
        private FrmCreateNewBank frm;

        public CreateBank(FrmCreateNewBank form) { this.frm = form; }
        
        public void Prepare()
        {
            IDbConnection conn = GetConnection(Constants.cons_Kernel);
            IDataReader reader = null;
            List<string> lstPrefix = new List<string>();
            int intNewBankNum = 0;
            Returns ret = Utils.InitReturns();
            string sql = String.Format("SELECT bd_kernel FROM {0}_kernel{1}s_point WHERE nzp_graj = 1;", Points.Pref, DBManager.tableDelimiter);
            try
            {
                conn.Open();
                ret = ExecRead(conn, out reader, sql, true);
                while (reader.Read())
                    if (reader["bd_kernel"] != DBNull.Value) lstPrefix.Add(reader["bd_kernel"].ToString());
            }
            catch { }
            finally
            {
                conn.Close();
                this.frm.WriteLog("Существующие банки: ");
                int intI = 0;
                foreach (string strPrefix in lstPrefix)
                {
                    int intCurrentBankNum = 0;
                    this.frm.WriteLog(String.Format(intI++ > 0 ? ", {0}" : "{0}", strPrefix.Trim()));
                    if (Int32.TryParse(Regex.Match(strPrefix.Trim(), @"\d+").Value, out intCurrentBankNum)) if (intCurrentBankNum > intNewBankNum) intNewBankNum = intCurrentBankNum;
                }
                this.frm.WriteLog(".\r\n");
                if (lstPrefix.Count > 0) this.frm.SetPrefixValue(String.Format("{0}{1}", lstPrefix[0].Replace(Regex.Match(lstPrefix[0].Trim(), @"\d+").Value, "").Trim(), (++intNewBankNum).ToString("00")));
                this.frm.SetEnabled(true);
            }
        }

        public bool CreateDataBases(DataBase DB, string Prefix)
        {
            Returns ret = Utils.InitReturns();
            int intStep = 0;
            foreach (string strFile in Directory.GetFiles(Path.Combine(Environment.CurrentDirectory, "patches\\bank"), String.Format("{0}-step-*.sqc", Enum.GetName(typeof(DataBase), DB).ToLower().Trim())))
            {
                ++intStep;
                using (StreamReader streamQueryFile = new StreamReader(strFile, Encoding.GetEncoding(1251)))
                {
                    string strQuery = "";
                    string strLine = "";
                    switch (intStep)
                    {
                        case 1:
                            this.frm.WriteLog(String.Format("Создание схемы данных {0}. Шаг {1}.\r\n", Enum.GetName(typeof(DataBase), DB).TrimEnd(), intStep));
                            break;
                        case 2:
                            this.frm.WriteLog(String.Format("Заполнение схемы данных {0}. Шаг {1}.\r\n", Enum.GetName(typeof(DataBase), DB).TrimEnd(), intStep));
                            break;
                    }
                    while ((strLine = streamQueryFile.ReadLine()) != null)
                    {
                        strQuery += (StringCrypter.Decrypt(strLine, StringCrypter.pass) + Environment.NewLine).Replace("%PREFIX%", Prefix).Replace("%YEAR[2C]%", DateTime.Now.ToString("yy")).Replace("%CENTRALPREFIX%", Points.Pref);
                        if (intStep ==2)
                        {
                            IDbConnection conn = GetConnection(Constants.cons_Kernel);
                            ret = Utils.InitReturns();
                            try { ret = ExecSQL(conn, strQuery); }
                            finally { conn.Close(); strQuery = ""; }
                            if (!ret.result) MonitorLog.WriteLog(ret.sql_error, MonitorLog.typelog.Warn, true);
                            ret = Utils.InitReturns();
                        }
                    }
                    streamQueryFile.Close();
                    if (intStep == 1)
                    {
                        IDbConnection conn = GetConnection(Constants.cons_Kernel);
                        ret = Utils.InitReturns();
                        try { ret = ExecSQL(conn, strQuery); }
                        finally { conn.Close(); }
                        if (!ret.result)
                        {
                            MonitorLog.WriteLog(ret.sql_error, MonitorLog.typelog.Warn, true);
                            return false;
                        }
                    }
                    streamQueryFile.Close();
                }
            }
            return ret.result;
        }

        public void UpdateLinks(string Prefix, string BankName)
        {
            string strQuery = String.Format("INSERT INTO {0}_kernel{1}s_point (nzp_graj, n, point, bd_kernel, flag, bank_number) VALUES (1, {2}, {3}, {4}, 2, {2});", Points.Pref, DBManager.tableDelimiter, Regex.Match(Prefix.Trim(), @"\d+").Value, Utils.EStrNull(BankName), Utils.EStrNull(Prefix));
            IDbConnection conn = GetConnection(Constants.cons_Kernel);
            Returns ret = Utils.InitReturns();
            try { ret = ExecSQL(conn, strQuery); }
            finally { conn.Close(); }
            if (!ret.result) MonitorLog.WriteLog(ret.sql_error, MonitorLog.typelog.Warn, true);
        }
    }
}
