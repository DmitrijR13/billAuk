using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
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
    public partial class FrmNewBase : Form
    {
        private IDbConnection connection = null;
        private String bank_id = null;
        private bool isNew = false;
        private String base_name = "";
        public FrmNewBase()
        {
            InitializeComponent();
            this.cmbType.DataSource = Enum.GetValues(typeof(BaselistTypes));
        }

        private void SetFromBase(DataGridViewRow row)
        {
            this.numYear.Value = int.Parse(row.Cells["ColumnPeriod"].Value.ToString());
            this.txtName.Text = row.Cells["ColumnName"].Value.ToString();
            this.txtComment.Text = row.Cells["ColumnComment"].Value.ToString();
            this.cmbType.SelectedItem = Enum.Parse(typeof(BaselistTypes), row.Cells["ColumnType"].Value.ToString());
            this.bank_id = row.Cells["ColumnID"].Value.ToString();
        }

        private void FrmNewBase_FormClosing(object sender, FormClosingEventArgs e)
        {
            // проверим правильность
            if (this.DialogResult == DialogResult.OK)
            {
                // не должно быть такого же периода в базе
                String new_bank = this.txtName.Text.Trim().ToLower();
                String sqlString;
                Returns ret;
                if (isNew)
                    sqlString = String.Format("SELECT count(1) FROM {0}s_baselist where lower(trim(dbname)) = '{1}'",
                        base_name + "_kernel" + DBManager.tableDelimiter, new_bank);
                else
                    sqlString = String.Format("SELECT count(1) FROM {0}s_baselist where lower(trim(dbname)) = '{1}' and nzp_bl <> {2}",
                        base_name + "_kernel" + DBManager.tableDelimiter, new_bank, bank_id);

                int count = int.Parse(DBManager.ExecScalar(this.connection, sqlString, out ret, true).ToString());
                if (count > 0)
                {
                    MessageBox.Show("Схема с таким именем уже существует в базе данных!");
                    e.Cancel = true;
                }
                else
                {
                    if (isNew)
                    {
                        if (MessageBox.Show("Сейчас будет создана новая схема.\nПродолжить?", "Подтверждение",
                            MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                        {
                            e.Cancel = true;
                        }
                    }
                    else
                    {
                        if (MessageBox.Show("Сейчас будет изменена схема.\nПродолжить?", "Подтверждение",
                            MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                        {
                            e.Cancel = true;
                        }
                    }
                }
            }
        }

        public DialogResult ShowDialog(IDbConnection connection, DataGridViewRow row, bool isNew, string base_name, out String newName, out String newComment, out int newYear)
        {
            this.connection = connection;
            this.isNew = isNew;
            this.base_name = base_name;
            this.SetFromBase(row);
            this.numYear.Enabled = isNew;
            DialogResult res = this.ShowDialog();
            newName = txtName.Text.Trim();
            newComment = txtComment.Text.Trim();
            newYear = (int)numYear.Value;
            return res;
        }

        private void numYear_ValueChanged(object sender, EventArgs e)
        {
            if (!String.IsNullOrEmpty(txtName.Text))
            {
                string prefix = txtName.Text.Substring(0, txtName.Text.LastIndexOf('_'));
                txtName.Text = String.Format("{0}_{1}", prefix, (this.numYear.Value-2000).ToString());
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {

        }
    }
}
