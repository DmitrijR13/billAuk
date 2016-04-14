using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace STCLINE.KP50.WinLogin
{
    public partial class FrmLogin : Form
    {
        public bool access;

        public FrmLogin()
        {
            access = false;
            InitializeComponent();
        }

        private void b_Ok_Click(object sender, EventArgs e)
        {
            if (tbx_Login.Text != "hostman" || tbx_Pwd.Text !="1")
            {
                l_Error.Text = "Неверный пароль";
                DialogResult = System.Windows.Forms.DialogResult.None;
                return;
            }
            access = true;
            DialogResult = System.Windows.Forms.DialogResult.OK;
            Close();
        }

        private void b_No_Click(object sender, EventArgs e)
        {
            //
        }
    }
}
