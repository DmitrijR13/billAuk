using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Collections;
using System.Text.RegularExpressions;
using STCLINE.KP50.Global;

namespace VersionCompile
{
    public partial class ConfigSettingsForm : Form
    {
        private string PathToHost = "";
        private string PathToWeb = "";

        public ArrayList HostConfig = new ArrayList();
        public ArrayList WebConfig = new ArrayList();

        public ConfigSettingsForm(string pth, string ptw, ArrayList hc, ArrayList wc)
        {
            InitializeComponent();

            HostConfig = hc;
            WebConfig = wc;

            PathToHost = pth;
            PathToWeb = ptw;

            foreach (string[] masstr in HostConfig)
            {
                dgvHost.Rows.Add(masstr[0], masstr[1]);
            }

            foreach (string[] masstr in WebConfig)
            {
                dgvWeb.Rows.Add(masstr[0], masstr[1]);
            }
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            if (File.Exists(Path.Combine(PathToHost, "Host.config")))
            {
                HostConfig = new ArrayList();
                foreach (DataGridViewRow row in dgvHost.Rows)
                {
                    if ((row.Cells["prefHost"].Value != null) && (row.Cells["valueHost"].Value != null))
                    {
                        HostConfig.Add(new string[2] { row.Cells["prefHost"].Value.ToString(), row.Cells["valueHost"].Value.ToString() });
                    }
                }
            }

            if (File.Exists(Path.Combine(PathToWeb, "Connect.config")))
            {
                WebConfig = new ArrayList();
                foreach (DataGridViewRow row in dgvWeb.Rows)
                {
                    if ((row.Cells["prefWeb"].Value != null) && (row.Cells["valueWeb"].Value != null))
                    {
                        WebConfig.Add(new string[2] { row.Cells["prefWeb"].Value.ToString(), row.Cells["valueWeb"].Value.ToString() });
                    }
                }
            }
        }
    }
}
