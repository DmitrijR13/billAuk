using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace VersionCompile
{
    public partial class ProgramSettingsForm : Form
    {
        public ProgramSettingsForm()
        {
            InitializeComponent();
            tbConnStr.Text = PathsAndKeys.DB_Connect;

            tbPathToHost.Text = PathsAndKeys.PathToHost;
            tbPathToWeb.Text = PathsAndKeys.PathToWeb;
            tbPathToSQL.Text = PathsAndKeys.PathToSQL;
            tbPathToUpdate.Text = PathsAndKeys.PathToUpdate;
            tbPathToAssembly.Text = PathsAndKeys.PathToAssembly;
            tbWebPathToUpdate.Text = PathsAndKeys.WebPathToUpdate;

            tbHostKey.Text = PathsAndKeys.HostKey;
            tbWebKey.Text = PathsAndKeys.WebKey;
        }

        private void btnPathToHost_Click(object sender, EventArgs e)
        {
            if (dlgOpenFolder.ShowDialog() == DialogResult.OK)
            {
                tbPathToHost.Text = dlgOpenFolder.SelectedPath;
            }
        }

        private void btnPathToWeb_Click(object sender, EventArgs e)
        {
            if (dlgOpenFolder.ShowDialog() == DialogResult.OK)
            {
                tbPathToWeb.Text = dlgOpenFolder.SelectedPath;
            }
        }

        private void btnPathToSQL_Click(object sender, EventArgs e)
        {
            if (dlgOpenFolder.ShowDialog() == DialogResult.OK)
            {
                tbPathToSQL.Text = dlgOpenFolder.SelectedPath;
            }
        }

        private void btnPathToUpdate_Click(object sender, EventArgs e)
        {
            if (dlgOpenFolder.ShowDialog() == DialogResult.OK)
            {
                tbPathToUpdate.Text = dlgOpenFolder.SelectedPath;
            }
        }

        private void btnHostKey_Click(object sender, EventArgs e)
        {
            if (dlgOpenFolder.ShowDialog() == DialogResult.OK)
            {
                tbHostKey.Text = dlgOpenFolder.SelectedPath;
            }
        }

        private void btnWebKey_Click(object sender, EventArgs e)
        {
            if (dlgOpenFolder.ShowDialog() == DialogResult.OK)
            {
                tbWebKey.Text = dlgOpenFolder.SelectedPath;
            }
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            PathsAndKeys.DB_Connect = tbConnStr.Text.Trim();

            PathsAndKeys.PathToHost = tbPathToHost.Text.Trim();
            PathsAndKeys.PathToWeb = tbPathToWeb.Text.Trim();
            PathsAndKeys.PathToSQL = tbPathToSQL.Text.Trim();
            PathsAndKeys.PathToUpdate = tbPathToUpdate.Text.Trim();
            PathsAndKeys.PathToAssembly = tbPathToAssembly.Text.Trim();
            PathsAndKeys.WebPathToUpdate = tbWebPathToUpdate.Text.Trim();

            PathsAndKeys.HostKey = tbHostKey.Text.Trim();
            PathsAndKeys.WebKey = tbWebKey.Text.Trim();
        }

        private void btnPathToAssembly_Click(object sender, EventArgs e)
        {
            if (dlgOpenFolder.ShowDialog() == DialogResult.OK)
            {
                tbPathToAssembly.Text = dlgOpenFolder.SelectedPath;
            }
        }

        private void btnWebPathToUpdate_Click(object sender, EventArgs e)
        {
            if (dlgOpenFolder.ShowDialog() == DialogResult.OK)
            {
                tbWebPathToUpdate.Text = dlgOpenFolder.SelectedPath;
            }
        }
    }
}
