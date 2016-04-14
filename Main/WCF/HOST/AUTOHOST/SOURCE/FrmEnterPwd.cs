using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace STCLINE.KP50.HostMan.SOURCE
{
    public partial class FrmEnterPwd : Form
    {
        public FrmEnterPwd()
        {
            InitializeComponent();
        }

        private void b_Ok_Click(object sender, EventArgs e)
        {
            string correctPwd = DateTime.Now.ToString("yyyy-MM-dd");
            if (tbx_Login.Text != "hostman" || tbx_Pwd.Text != correctPwd)
            {
                MessageBox.Show("Неверный пароль!");
                DialogResult = System.Windows.Forms.DialogResult.None;
                return;
            }
            DialogResult = System.Windows.Forms.DialogResult.OK;
            Close();
        }
    }
}
