using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Collections;
using System.ServiceModel;
using System.Diagnostics;

using STCLINE.KP50.Client;
using STCLINE.KP50.Global;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace updater
{
    public partial class GetMonLogForm : Form
    {
        private EventLogEntry[] MonLogList; 
        private string raj_ip = "";
        private string raj_name = "";
        private string raj_log = "";
        private string raj_pas = "";

        public GetMonLogForm(Info rajon)
        {
            InitializeComponent();
            dtpckBegDate.Value = System.DateTime.Now;
            dtpckEndDate.Value = System.DateTime.Now;
            dtpckBegTime.Value = System.DateTime.Now;
            dtpckEndTime.Value = System.DateTime.Now;
            
            //raj_name = dr.Cells[1].Value.ToString();
            //raj_ip = dr.Cells[2].Value.ToString();
            raj_name = rajon.rajon_name;
            raj_ip = rajon.rajon_ip;
            raj_log = rajon.rajon_login;
            raj_pas = rajon.rajon_password;

            Text = raj_name + " " + raj_ip;
        }

        private void GetMonitorLogs_Load(object sender, EventArgs e)
        {

        }

        private void RefreshRTB()
        {
            rtbGetMonLog.Visible = false;
            rtbGetMonLog.Clear();
            if (MonLogList == null)
            {
                return;
            }
            if (MonLogList.Length == 0)
            {
                rtbGetMonLog.AppendText("Лог пуст");
            }
            else
            {
                foreach (EventLogEntry entry in MonLogList)
                {
                    Color color = Color.White;
                    switch (entry.EntryType)
                    {
                        case EventLogEntryType.Information:
                            {
                                if (chbInfo.Checked)
                                {
                                    color = Color.LightGreen;
                                    break;
                                }
                                else
                                {
                                    continue;
                                }
                            }
                        case EventLogEntryType.Warning:
                            {
                                if (chbWarning.Checked)
                                {
                                    color = Color.Yellow;
                                    break;
                                }
                                else
                                {
                                    continue;
                                }
                            }
                        case EventLogEntryType.Error:
                            {
                                if (chbError.Checked)
                                {
                                color = Color.LightCoral;
                                break;
                                }
                                else
                                {
                                    continue;
                                }
                            }
                    }
                    int OldCount = rtbGetMonLog.Text.Length;
                    rtbGetMonLog.AppendText(entry.TimeWritten.ToString() + "\n" + entry.EntryType.ToString() + "\n" + entry.Message + "\n\n");
                    int NewCount = rtbGetMonLog.Text.Length;
                    rtbGetMonLog.SelectionStart = OldCount;
                    rtbGetMonLog.SelectionLength = NewCount - OldCount;
                    rtbGetMonLog.SelectionBackColor = color;
                    rtbGetMonLog.SelectionLength = 0;
                }
            }
            rtbGetMonLog.Visible = true;
        }

        private void btnGetLog_Click(object sender, EventArgs e)
        {
            bool var = false;
            rtbGetMonLog.Clear();
            DateTime BegDate = new DateTime(dtpckBegDate.Value.Year, dtpckBegDate.Value.Month, dtpckBegDate.Value.Day, dtpckBegTime.Value.Hour, dtpckBegTime.Value.Minute, dtpckBegTime.Value.Second);
            DateTime EndDate = new DateTime(dtpckEndDate.Value.Year, dtpckEndDate.Value.Month, dtpckEndDate.Value.Day, dtpckEndTime.Value.Hour, dtpckEndTime.Value.Minute, dtpckEndTime.Value.Second);
            try
            {
                cli_Patch cli = new cli_Patch(raj_ip + "/srv", raj_log, raj_pas);
                Stream stream = cli.GetMonitorLog(BegDate, EndDate);
                BinaryFormatter bf = new BinaryFormatter();
                MonLogList = (EventLogEntry[])bf.Deserialize(stream);
                MonLogList = (MonLogList.Reverse()).ToArray();
                var = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при соединении с районом: " + raj_name + "; Ошибка:" + ex.Message);
                var = false;
            }
            if (var)
            {
                RefreshRTB();
            }
            else
            {
                rtbGetMonLog.AppendText("Ошибка получения лога");
            }
        }

        private void chbInfo_CheckedChanged(object sender, EventArgs e)
        {
            chbInfo.Enabled = false;
            RefreshRTB();
            chbInfo.Enabled = true;
        }

        private void chbWarning_CheckedChanged(object sender, EventArgs e)
        {
            chbWarning.Enabled = false;
            RefreshRTB();
            chbWarning.Enabled = true;
        }

        private void chbError_CheckedChanged(object sender, EventArgs e)
        {
            chbError.Enabled = false;
            RefreshRTB();
            chbError.Enabled = true;
        }
    }
}
