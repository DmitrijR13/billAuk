using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using Unloader;

namespace FormatForm
{
    public partial class AddFormat : Form
    {
        private string[] FileNames;
        private Points localPoint;

        #region Инициализация формы
        public AddFormat()
        {
            InitializeComponent();
            CenterToScreen();
            var list = UnloadInstanse.Instance.GetAllFormats();
            FormatCollection.DisplayMember = "FormatName";
            FormatCollection.ValueMember = "RegistrationName";
            FormatCollection.DataSource = list;
            cbMonth.DisplayMember = "month_name";
            cbMonth.ValueMember = "month_id";
            cbMonth.DataSource = GetMonth();
            cbYear.DataSource = GetYears();
            cbYear.SelectedIndex = GetYears().Count - 1;
            GetDb();
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
            if (cbDatabase.SelectedValue == null)
            {
                MessageBox.Show(string.Format("Выберите базу данных"));
                return;
            }
            if (lbPoints.CheckedItems.Count == 0)
            {
                MessageBox.Show(string.Format("Выберите банки данных"));
                return;
            }
            if (cbMonth.SelectedValue == null)
            {
                MessageBox.Show(string.Format("Выберите месяц"));
                return;
            }
            if (cbYear.SelectedValue == null)
            {
                MessageBox.Show(string.Format("Выберите год"));
                return;
            }
            localPoint.pointList = localPoint.pointList.Where(x => lbPoints.CheckedItems.Contains(x)).ToList();
            var request = new Request
            {
                Format = FormatCollection.Text.ToString(CultureInfo.InvariantCulture),
                db = cbDatabase.Text.ToString(CultureInfo.InvariantCulture),
                points = localPoint,
                schema = cbSchema.SelectedValue.ToString(),
                RegistrationName = FormatCollection.SelectedValue.ToString(),
                StatusID = Statuses.Added,
                month = Convert.ToInt32(cbMonth.SelectedValue),
                year = Convert.ToInt32(cbYear.SelectedValue),
                Status = "Добавлен",
                unloadID = 0,
            };
            (Owner as FormatList).AddDataAtList(request);
            Close();
        }
        #endregion

        #region Отмена
        private void cancelButton_Click(object sender, EventArgs e)
        {
            Close();
        }
        #endregion

        protected void GetDb()
        {
            var list = UnloadInstanse.Instance.GetDatabases();
            cbDatabase.DataSource = list;
        }

        private void cbDatabase_SelectedIndexChanged(object sender, EventArgs e)
        {
            cbSchema.Enabled = true;
            var list = UnloadInstanse.Instance.GetShemas(cbDatabase.SelectedItem.ToString());
            cbSchema.DataSource = list;
        }

        private void cbSchema_SelectedIndexChanged(object sender, EventArgs e)
        {
            lbPoints.Enabled = true;
            localPoint = UnloadInstanse.Instance.GetPoints(cbDatabase.SelectedItem.ToString());
            ((ListBox)lbPoints).DataSource = localPoint == null ? null : localPoint.pointList;
            ((ListBox)lbPoints).DisplayMember = "point";
            ((ListBox)lbPoints).ValueMember = "pref";
        }

    }
}
