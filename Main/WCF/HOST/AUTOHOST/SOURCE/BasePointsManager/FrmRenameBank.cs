using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace STCLINE.KP50.HostMan.SOURCE.BasePointsManager
{
    public partial class FrmRenameBank : Form
    {
        public FrmRenameBank()
        {
            InitializeComponent();
        }

        private void FrmRenameBank_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (tbBankNewName.Text.Trim() == "")
            {
                MessageBox.Show("Название банка не может быть пустым!", "Ошибка!", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }
            this.DialogResult = DialogResult.OK;
            
        }

        public DialogResult ShowDialog(out string newName)
        {
            DialogResult res = this.ShowDialog();
            newName = tbBankNewName.Text.Trim();
            return res;
        }

        private void FrmRenameBank_FormClosing(object sender, FormClosingEventArgs e)
        {
        }
    }
}
