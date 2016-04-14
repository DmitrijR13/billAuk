using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using STCLINE.KP50.HostMan;

namespace STCLINE.KP50.HostMan.SelectDataBase
{
    public partial class SelectDataBase : Form
    {
        private string DBFName;
        private string connectionString;
        private string nextForm;

        public SelectDataBase(DataTable kernelTablesList, string tDBFName, string tconnectionString, string tnextForm)
        {
            InitializeComponent();

            DBFName = tDBFName;
            connectionString = tconnectionString;
            nextForm = tnextForm;

            for (int i = 0; i < kernelTablesList.Rows.Count; i++)
            {
                if (kernelTablesList.Rows[i]["bd_kernel"] != null)
                {
                    kernelTablesList.Rows[i]["bd_kernel"] = kernelTablesList.Rows[i]["bd_kernel"].ToString().Trim() + "_data";
                }
            }

            cbBase.DataSource = kernelTablesList.DefaultView;
            cbBase.DisplayMember = "bd_kernel";
            cbBase.BindingContext = this.BindingContext;
            cbBase.SelectedIndex = 1;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            string[] words = connectionString.Split(';');
            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].Split('=')[0] == "Database")
                {
                    string asd = words[i].Split('=')[1];
                    connectionString = connectionString.Replace(words[i].Split('=')[1], cbBase.Text);
                    break;
                }
            }

            if (nextForm == "KLADR")
            {
                KLADR.KLADR KForm = new KLADR.KLADR(DBFName, connectionString);
                this.Hide();
                KForm.ShowDialog();
                this.Close();
            }
            if (nextForm == "AddressService")
            {
                AddressService.FrmAddressService addrServForm = new AddressService.FrmAddressService(connectionString);
                this.Hide();
                addrServForm.ShowDialog();
                //this.Close();
            }
        }
    }
}
