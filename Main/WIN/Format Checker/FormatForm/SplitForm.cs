using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Web.UI.WebControls;
using System.Windows.Forms;
using FormatLibrary;
using Microsoft.SqlServer.Server;

namespace FormatForm
{
    public partial class SplitForm : Form
    {
        private string FileName;
        private string Path;
        private Type format;
        public delegate void SetTextCallback(int value);
        public delegate void ShowDialog(string link);

        public SplitForm(string Path, string FileName, Type format)
        {
            InitializeComponent();
            var comboSource = new Dictionary<string, string> { { "1", "zip" }, { "2", "7z" }/*, { "3", "rar" }*/ };
            cbType.DataSource = new BindingSource(comboSource, null);
            cbType.DisplayMember = "Value";
            cbType.ValueMember = "Key";
            this.FileName = FileName;
            this.Path = Path;
            this.format = format;
            textBox1.Text = FileName.Split('.').First();
            progressBar1.Style = ProgressBarStyle.Continuous;
            progressBar1.Maximum = 100;
            progressBar1.Value = 0;
        }

        private void btSave_Click(object sender, EventArgs e)
        {
            if (textBox1.Text.Length == 0)
            {
                MessageBox.Show("Не указано наименование сохраняемого файла");
                return;
            }
            var split = new SplitFile(((KeyValuePair<string, string>)cbType.SelectedItem).Value.ToString(), Path, FileName, textBox1.Text.Trim(), format);
            var thread = new Thread(split.Split);
            split.mainProgress += GetProgressMeta;
            thread.Start();
        }

        protected void GetProgressMeta(object sender, ProgressArgs args)
        {
            SetText((int)(args.progress * 100));
            if (args.link != null)
            {
                var d = new ShowDialog(ShowSaveDialog);
                Invoke(d, new object[] { args.link });
            }
        }

        private void SetText(int value)
        {
            if (progressBar1.InvokeRequired)
            {
                var d = new SetTextCallback(SetText);
                Invoke(d, new object[] { value });
            }
            else
            {
                if (value > 0)
                    progressBar1.Value = value;
            }
        }

        private void ShowSaveDialog(string link)
        {
            using (var svd = new SaveFileDialog())
            {
                svd.Filter = @"MS WORD Files (.docx)|*.docx|All Files (*.*)|*.*";
                svd.Title = @"Сохраните мета-данные формата";
                svd.FileName = link.Split('\\').Last();
                svd.FilterIndex = 2;
                svd.ShowDialog();
                if (svd.FileName.Trim().Length != 0)
                {
                    if (File.Exists(svd.FileName))
                        File.Delete(svd.FileName);
                    File.Copy(link, svd.FileName);
                }
                Close();
            }
        }

    }
}
