using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Loader;

namespace FormatForm
{
    public partial class FormatList : Form
    {
        private List<Request> resultList;
        protected int page = 1;
        protected int pager = 20;

        #region Инициализация формы
        public FormatList()
        {
            InitializeComponent();
            Instance.sendMessage += GetMessage;
            Instance.mainProgress += GetProgress;
            FormatGrid.CellClick += OpenFile;
            FormatGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            FormatGrid.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            FormatGrid.ColumnHeadersVisible = true;
            FormatGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            FormatGrid.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.AutoSizeToDisplayedHeaders;
            FormatGrid.AutoResizeRowHeadersWidth(DataGridViewRowHeadersWidthSizeMode.AutoSizeToAllHeaders);
            FormatGrid.Dock = DockStyle.Fill;
            resultList = Initiate();
            CenterToScreen();
            LoadFromFile();
            Refresh();
        }
        #endregion

        #region Загрузить список задач
        protected void LoadFromFile()
        {
            try
            {
                resultList = Instance.LoaderInstance.GetImportValues();
                var formats = Instance.LoaderInstance.GetFormats();
                resultList.ForEach(elem =>
                {
                    elem.type =
                        formats.FirstOrDefault(x => x.RegistrationName == elem.RegistrationName).type;
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Ошибка:" + ex.Message));
            }
            UpdateResultCount();
            UpdateCurrentPage();
            Invoke((MethodInvoker)LoadPager);
            Invoke((MethodInvoker)Refresh);
        }
        #endregion

        #region Подписка на событие 'Получение сообщений'
        protected void GetMessage(object sender, SendArgs args)
        {
            Thread.Sleep(500);
            var index = GetIndexBynzp_load(args.nzp_load);
            if (index == -1) return;
            resultList[index].StatusID = args.result;
            resultList[index].Status = GetEnumValue(args.result.ToString()) + "(" + resultList[index].progress * 100 + "%)";
            if (resultList[index].StatusID == Statuses.Finished || resultList[index].StatusID == Statuses.Error)
            {
                resultList[index].Result = args.link != null ? args.link.Split('\\').Last() : args.Message;
                resultList[index].link = args.link;
            }
            else resultList[index].Result = args.Message;
            if (resultList[index].StatusID == Statuses.Finished)
                LoadFromFile();
            Invoke((MethodInvoker)Refresh);
        }
        #endregion

        #region Подписка на событие 'Обновление прогресса'
        protected void GetProgress(object sender, ProgressArgs args)
        {
            var index = GetIndexBynzp_load(args.nzp_load);
            if (index == -1) return;
            resultList[index].progress = args.progress;
            resultList[index].Status = GetEnumValue(resultList[index].StatusID.ToString()) + "(" + resultList[index].progress * 100 + "%)";
            Invoke((MethodInvoker)Refresh);
        }
        #endregion

        public List<Request> Initiate()
        {
            return new List<Request>();
        }

        #region Добавление в список
        public void AddDataAtList(List<Request> list)
        {
            resultList.AddRange(list);
            UpdateResultCount();
            UpdateCurrentPage();
            Refresh();
        }
        #endregion

        #region Обновление отображения списка
        private void Renumber()
        {
            for (var i = 0; i < resultList.Count; i++)
                resultList[i].Num = i + 1;
        }
        private void Refresh()
        {
            var i = FormatGrid.SelectedRows.Count == 0 ? 0 : FormatGrid.SelectedRows[0].Index;
            Renumber();
            FormatGrid.AllowUserToAddRows = false;
            FormatGrid.Rows.Clear();
            resultList.Take(page * pager).Skip((page - 1) * pager).ToList().ForEach(
                x =>
                {
                    FormatGrid.Rows.Add(x.Num, x.FileName, x.org_name, x.dat_month == null ? "" : x.dat_month.Value.ToShortDateString(),
                        x.date_load == null ? "" : x.date_load.Value.ToShortDateString(), x.Format, x.Result, x.Status);
                    FormatGrid.Rows[FormatGrid.RowCount - 1].Cells[6] = (x.StatusID == Statuses.Finished || x.StatusID == Statuses.Error) && x.link != null
                        ? (DataGridViewCell)new DataGridViewLinkCell { Value = x.Result }
                        : new DataGridViewTextBoxCell { Value = x.Result };
                });
            SelectRow(i);
        }

        public void SelectRow(int index)
        {
            index = index > FormatGrid.Rows.Count - 1 ? 0 : index;
            if (FormatGrid.Rows.Count != 0)
                FormatGrid.Rows[index].Selected = true;
        }
        #endregion

        private void AddNewItem_Click(object sender, EventArgs e)
        {
            var form = new AddFormat();
            form.ShowDialog(this);
            Refresh();
        }

        #region Удаление элемента
        private void DeleteItem_Click(object sender, EventArgs e)
        {
            if (FormatGrid.Rows.Count == 0 || FormatGrid.SelectedRows[0] == null)
            {
                MessageBox.Show(string.Format("Не выбран формат для удаления"));
                return;
            }
            var Number = FormatGrid.SelectedRows[0].Cells[0].Value;
            var index = resultList.IndexOf(resultList.FirstOrDefault(x => x.Num == Convert.ToInt32(Number)));
            if (resultList[index].StatusID == Statuses.Execute)
            {
                MessageBox.Show(string.Format("Не возможно удалить проверку во время выполнения"));
                return;
            }
            Instance.LoaderInstance.Delete(resultList[index].nzp_load);
            resultList.Remove(resultList.FirstOrDefault(x => x.Num == Convert.ToInt32(Number)));
            UpdateResultCount();
            UpdateCurrentPage();
            Refresh();
        }
        #endregion

        #region Отправка задачи на выполнение
        private void uploadScriptButton_Click(object sender, EventArgs e)
        {
            if (FormatGrid.Rows.Count == 0 || FormatGrid.SelectedRows[0] == null)
            {
                MessageBox.Show(string.Format("Не выбран формат для проверки"));
                return;
            }
            var Number = FormatGrid.SelectedRows[0].Cells[0].Value;
            var index = resultList.IndexOf(resultList.FirstOrDefault(x => x.Num == Convert.ToInt32(Number)));
            switch (resultList[index].StatusID)
            {
                case Statuses.Error:
                case Statuses.Finished:
                case Statuses.Added:
                    Instance.LoaderInstance.Start(resultList[index]);
                    resultList[index].StatusID = Statuses.InQueue;
                    resultList[index].Status = GetEnumValue(Statuses.InQueue.ToString());
                    Refresh();
                    break;
                case Statuses.Stopped:
                    Instance.StopResume(resultList[index].nzp_load, resultList[index].StatusID == Statuses.Execute);
                    break;
                default:
                    MessageBox.Show(string.Format("Не возможно запустить проверку"));
                    break;
            }
        }
        #endregion

        #region Получение индекса элемента по ключу
        protected int GetIndexBynzp_load(int nzp_load)
        {
            return resultList.IndexOf(resultList.FirstOrDefault(x => x.nzp_load == Convert.ToInt32(nzp_load)));
        }
        #endregion

        #region Получение индекса элемента по номеру
        protected int GetIndexByNumber(int Number)
        {
            return resultList.IndexOf(resultList.FirstOrDefault(x => x.Num == Convert.ToInt32(Number)));
        }
        #endregion

        #region Обновление списка
        private void RefreshButton_Click(object sender, EventArgs e)
        {
            Refresh();
        }
        #endregion

        #region Получение статуса по значению Enum-а
        protected string GetEnumValue(string val)
        {
            var type = typeof(Statuses);
            var memInfo = type.GetMember(val);
            var attributes = memInfo[0].GetCustomAttributes(typeof(DescriptionAttribute),
                false);
            return ((DescriptionAttribute)attributes[0]).Description;
        }
        #endregion

        #region Остановка выполнения задачи
        private void Stop_Click(object sender, EventArgs e)
        {
            if (FormatGrid.Rows.Count == 0 || FormatGrid.SelectedRows[0] == null)
            {
                MessageBox.Show(string.Format("Не выбран запущенный формат для остановки"));
                return;
            }
            var Number = FormatGrid.SelectedRows[0].Cells[0].Value;
            var index = resultList.IndexOf(resultList.FirstOrDefault(x => x.Num == Convert.ToInt32(Number)));
            if ((resultList[index].StatusID != Statuses.Execute))
            {
                MessageBox.Show(string.Format("Проверка формата завершена или не запущена"));
                return;
            }
            Instance.StopResume(resultList[index].nzp_load, resultList[index].StatusID == Statuses.Execute);
        }
        #endregion

        #region Открытие файла
        private void OpenFile(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex != 6 || e.RowIndex < 0) return;
            try
            {
                var index = GetIndexByNumber(Convert.ToInt32(FormatGrid.Rows[e.RowIndex].Cells[0].Value));
                if (resultList[index].StatusID == Statuses.Finished)
                    Process.Start("notepad.exe", resultList[index].link);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Ошибка:" + ex.Message));
            }
        }
        #endregion

        #region Действия при закрытии формы
        private void FormatList_FormClosing(object sender, FormClosingEventArgs e)
        {
            resultList.ForEach(elem =>
            {
                if (elem.StatusID == Statuses.Added || elem.StatusID == Statuses.Stopped || elem.StatusID == Statuses.Execute || elem.StatusID == Statuses.InQueue)
                {
                    elem.Status = "";
                    elem.StatusID = Statuses.Added;
                }
                //elem.TypeName = elem.type.FullName;
            });
            try
            {
                Instance.LoaderInstance.UpdateImportValue(resultList);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Ошибка:" + ex.Message));
            }
        }
        #endregion

        #region Пагинатор
        public void UpdateResultCount()
        {
            if (page > GetResultCount() && GetResultCount() != 0) { page--; Refresh(); }
            bindingNavigatorCountItem.Text = resultList.Any() ? string.Format(" из {0}", Math.Ceiling((double)resultList.Count / pager)) : string.Format(" из {0}", " из 1");
        }

        public void UpdateCurrentPage()
        {
            if (page > GetResultCount() && GetResultCount() != 0) { page--; Refresh(); }
            bindingNavigatorPositionItem.Text = page.ToString(CultureInfo.InvariantCulture);
        }

        private void bindingNavigatorMoveFirstItem_Click(object sender, EventArgs e)
        {
            page = 1;
            UpdateCurrentPage();
            Refresh();
        }

        private void bindingNavigatorMovePreviousItem_Click(object sender, EventArgs e)
        {
            if (page == 1)
            {
                MessageBox.Show(@"Вы достигли первой страницы");
                return;
            }
            page -= 1;
            UpdateCurrentPage();
            Refresh();
        }

        protected int GetResultCount()
        {
            return (int)Math.Ceiling((double)resultList.Count / pager);
        }

        private void bindingNavigatorMoveNextItem_Click(object sender, EventArgs e)
        {
            if (page == GetResultCount() || GetResultCount() == 0)
            {
                MessageBox.Show(@"Вы достигли последней страницы");
                return;
            }
            page += 1;
            UpdateCurrentPage();
            Refresh();
        }

        private void bindingNavigatorMoveLastItem_Click(object sender, EventArgs e)
        {
            page = GetResultCount() == 0 ? 1 : GetResultCount();
            UpdateCurrentPage();
            Refresh();
        }

        protected class Pager
        {
            public int index { get; set; }
            public string value { get; set; }
        }

        protected void LoadPager()
        {
            cmbPager.DisplayMember = "value";
            cmbPager.ValueMember = "index";
            cmbPager.DataSource = new List<Pager>
            {
                new Pager{index = 10,value = "10 записей"},
                new Pager{index = 20,value = "20 записей"},
                new Pager{index = 50,value = "50 записей"},
                new Pager{index = 100,value = "100 записей"}
            };
            cmbPager.SelectedIndex = 1;
        }

        private void cmbPager_SelectedIndexChanged(object sender, EventArgs e)
        {
            pager = (int)cmbPager.SelectedValue;
            page = 1;
            UpdateResultCount();
            UpdateCurrentPage();
            Refresh();
        }
        #endregion
    }
}

