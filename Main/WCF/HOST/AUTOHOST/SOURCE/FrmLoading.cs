using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace STCLINE.KP50.HostMan.Loading
{
    public partial class Loading : Form
    {
        public Loading(string str)
        {
            InitializeComponent();
            this.Text = str;
        }

        public void SetMax(int max)
        {
            progressBar1.Maximum = max;
        }

        public void Inc()
        {
            progressBar1.Value++;
        }

        public void SetValue(int val)
        {
            progressBar1.Value = val;
        }
    }
}
