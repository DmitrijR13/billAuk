using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using FormatLibrary;
using Loader;

namespace FormatForm
{
    public partial class AddFormat : Form
    {
        private string[] FileNames;

        #region Инициализация формы
        public AddFormat()
        {
            InitializeComponent();
            CenterToScreen();
            var list = Instance.LoaderInstance.GetFormats();
            FormatCollection.DisplayMember = "FormatName";
            FormatCollection.ValueMember = "RegistrationName";
            FormatCollection.DataSource = list;
            //ddSchema.DataSource = Instance.LoaderInstance.GetSchemas();
            //cbMonth.DisplayMember = "month_name";
            //cbMonth.ValueMember = "month_id";
            //cbMonth.DataSource = GetMonth();
            //cbYear.DataSource = GetYears();
            //cbYear.SelectedIndex = GetYears().Count - 1;
        }

        protected class Month
        {
            public string month_name { get; set; }
            public int month_id { get; set; }
        }

        protected Month[] GetMonth()
        {
            var months = new Month[12];
            for (var i = 1; i <= 12; i++)
            {
                months[i - 1] = new Month { month_id = i, month_name = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(i) };
            }
            return months;
        }

        protected List<int> GetYears()
        {
            var list = new List<int>();
            for (var i = 2008; i <= DateTime.Now.Year; i++)
                list.Add(i);
            return list;
        }
        #endregion

        #region Добавление формата
        private void okButton_Click(object sender, EventArgs e)
        {
            if (FormatCollection.SelectedValue == null)
            {
                MessageBox.Show(string.Format("Выберите формат"));
                return;
            }
            if (FileNames == null)
            {
                MessageBox.Show(string.Format("Выберите файлы для проверки"));
                return;
            }
            var list = new List<Request>();
            FileNames.ToList().ForEach(x =>
            {
                if (x.Trim().Length != 0)
                    list.Add(new Request
                    {
                        FileName = x.Split('\\').Last(),
                        Path = x.Replace(x.Split('\\').Last(), ""),
                        //schema = ddSchema.SelectedItem.ToString(),
                        Format = FormatCollection.Text.ToString(CultureInfo.InvariantCulture),
                        type = (Instance.LoaderInstance.GetFormats().FirstOrDefault(y => y.RegistrationName == (FormatCollection.SelectedValue as string)).type),
                        TypeName = (Instance.LoaderInstance.GetFormats().FirstOrDefault(y => y.RegistrationName == (FormatCollection.SelectedValue as string)).type).FullName,
                        RegistrationName = (FormatCollection.SelectedValue as string),
                        StatusID = Statuses.Added,
                        Status = "Добавлен",
                        nzp_load = 0,
                        nzp_user = 301,
                        GisFileId = 14673,
                        date_charge = new DateTime()
                    });
            });
            if (!list.Any())
            {
                MessageBox.Show(string.Format("Выберите файлы для проверки"));
                return;
            }
            list.ForEach(x => x.nzp_load = Instance.LoaderInstance.Insert(x).nzp_load);
            (Owner as FormatList).AddDataAtList(list);
            Close();
        }
        #endregion

        #region Открыть FileDialog и получить выбранные файлы
        protected string[] GetFileNames()
        {
            try
            {
                using (var OpenDialog = new OpenFileDialog())
                {
                    OpenDialog.Filter = @"Archive Files (.rar,.zip,.7z)|*.rar;*.zip;*.7z|All Files (*.*)|*.*";
                    OpenDialog.FilterIndex = 1;
                    OpenDialog.Multiselect = true;
                    if (OpenDialog.ShowDialog() == DialogResult.OK)
                    {
                        return OpenDialog.FileNames;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Произошла ошибка:{0}", ex.Message));
            }
            return null;
        }

        private void openButton_Click(object sender, EventArgs e)
        {
            FileNames = GetFileNames();
            if (FileNames != null)
                displayFileName.Text = string.Join(",", FileNames.Select(x => x.Split('\\').Last()).ToArray());
        }
        #endregion

        #region Отмена
        private void cancelButton_Click(object sender, EventArgs e)
        {
            Close();
        }
        #endregion

    }
}
