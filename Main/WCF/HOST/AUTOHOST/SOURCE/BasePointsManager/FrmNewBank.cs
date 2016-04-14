using System.Text.RegularExpressions;
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
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.HostMan.SOURCE
{
    public partial class FrmNewBank : Form
    {
        private IDbConnection connection = null;
        public FrmNewBank()
        {
            InitializeComponent();
        }

        public DialogResult ShowDialog(IDbConnection connection, out String newName, out String newComment, out int newYear, out bool saveNormatifs)
        {
            this.connection = connection;
            int newNumer = 0;
            numYear.Value = DateTime.Now.Year;
            tbNewPref.Text = "";
            #region предалагаем пользователю новый префикс 

            try
            {
                string sql =
                    " SELECT TRIM(bd_kernel) as pref" +
                    " FROM " + Points.Pref + DBManager.sKernelAliasRest + "s_point ";
                DataTable dt = DBManager.ExecSQLToTable(connection, sql);

                foreach (DataRow row in dt.Rows)
                {

                    int tmpNumer = 0;
                    Int32.TryParse(Regex.Match(row["pref"].ToString().Trim(), @"\d+").Value, out tmpNumer);
                    newNumer = tmpNumer > newNumer ? tmpNumer : newNumer;
                }
                newNumer ++;
                tbNewPref.Text = Points.Pref.Replace("f", "") + newNumer.ToString("00");
            }
            catch 
            {
                MonitorLog.WriteLog("Ошибка при генерации нового префикса! ", MonitorLog.typelog.Info, true);
            }

            #endregion предалагаем пользователю новый префикс

            tbNewName.Text = "Банк данных " + newNumer;
            DialogResult res = this.ShowDialog();
            newName = tbNewPref.Text.Trim();
            newComment = tbNewName.Text.Trim();
            newYear = (int)numYear.Value;
            saveNormatifs = cbSaveNormatifs.Checked;
            return res;
        }

        private void FrmNewBank_FormClosing(object sender, FormClosingEventArgs e)
        {
            // проверим правильность
            if (this.DialogResult == DialogResult.OK)
            {
                e.Cancel = true;

                
                Returns ret = new Returns() {text = String.Empty, result = true};
                string newPref = this.tbNewPref.Text.Trim().ToLower();
                string newName = this.tbNewName.Text.Trim().ToLower();

                //проверяем префикс на корректность
                if (Regex.Match(newPref.Substring(0, 1), @"\d").Success)
                {
                    ret.result = false;
                    ret.text += "Префикс нового банка должен начинаться с символа, а не с цифры!" + Environment.NewLine;
                }

                //не должно быть такого префикса или названия в базе
                string sql =
                    String.Format(
                        " SELECT " +
                        " MAX(CASE WHEN LOWER( TRIM(bd_kernel)) = '{0}' THEN '1' ELSE '0' END) AS pref, " +
                        " MAX(CASE WHEN LOWER( TRIM(point))     = '{1}' THEN '1' ELSE '0' END) AS name " +
                        " FROM " + Points.Pref + DBManager.sKernelAliasRest + "s_point  ",
                        newPref, newName);
                
                DataTable dt = DBManager.ExecSQLToTable(connection, sql);

                if (dt.Rows[0]["pref"].ToString() == "1")
                {
                    ret.result = false;
                    ret.text += "Банк данных с таким префиксом уже существует в базе данных!" + Environment.NewLine;
                }
                if (dt.Rows[0]["name"].ToString() == "1")
                {
                    ret.result = false;
                    ret.text += "Банк данных с таким названием уже существует в базе данных!" + Environment.NewLine;
                }
                if (string.IsNullOrEmpty(tbNewPref.Text))
                {
                    ret.result = false;
                    ret.text += "Не задан префикс нового банка!" + Environment.NewLine;
                }
                if (string.IsNullOrEmpty(tbNewName.Text))
                {
                    ret.result = false;
                    ret.text += "Не задано название нового банка!" + Environment.NewLine;
                }

                //если есть ошибки, то выводим сбщ и выходим
                if (!ret.result)
                {
                    MessageBox.Show(ret.text,"Ошибка!",MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                else
                {
                    e.Cancel = false;
                    if (MessageBox.Show("Сейчас будет создана схема нового банка данных.\nПродолжить?", "Подтверждение",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                    {
                        e.Cancel = true;
                    }
                }
            }
        }


        private void FrmNewBank_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {

        }
    }
}
