using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using FormatLibrary;
using System.Globalization;

namespace FormatForm
{
    public partial class Viewer : Form
    {
        private class Section
        {
            public int section_id { get; set; }//номер секции
            public string section_name { get; set; }//Наименование секции
            public int section_start { get; set; }//Номер строки начала секции
            public int section_end { get; set; }//Последняя строка секции
        }

        protected int page = 1;//Номер текущей страницы
        protected int pager = 20;//Разбиение по умолчанию
        protected int pageCount = 1;//Количество страниц
        private string Path { get; set; }//Путь до файла
        private string FileName { get; set; }//Имя файла
        private int lineCount { get; set; }//Количество строк файла
        private Type Format { get; set; }//Формат загружаемого файла
        private Dictionary<string, string> templatesHead;//Шаблон с именами заголовков
        private Dictionary<string, List<Template>> bodyDict;//Шаблон с данными формата
        private bool EnableUpdate = true;//Запретить событие обновления при изменения значения выпадающего списка 
        private bool PagingPartial = false;//Включения режима показа отдельных строк секции
        public bool Initialize = false;//Показатель корректной загрузки основных параметров формы
        private List<Section> listSect = new List<Section>();//Список секций файла

        #region Конструкторы формы представления
        public Viewer(string Path, string FileName)
        {
            this.Path = Path;
            this.FileName = FileName;
            InitializeComponent();
            SectionAnalyze();
            LoadPager();
        }
   
        public Viewer(string Path, string FileName, Type Format)
        {
            this.Path = Path;
            this.FileName = FileName;
      
            if (Format.Name == "PortalGovServ11" || Format.Name == "PortalGovServ1")
            {
                MessageBox.Show("Режим для данного типа формата не поддерживается");
                return;
            }
            if (GetFilesIfWorkWithArchive(Path, FileName) == null)
                return;
            this.Format = Format;
            var list = Format.GetNestedTypes(BindingFlags.Instance | BindingFlags.Public).ToList();
            IFormatChecker Checker = null;
            list.ForEach(x =>
            {
                if (!typeof(IFormatChecker).IsAssignableFrom(x))
                    return;
                Checker = (IFormatChecker)Create<IFormatChecker>(x);
            });
            templatesHead = Checker.GetTemplatesHead();
            bodyDict = Checker.FillTemplates();
            InitializeComponent();
            Text = Text + " '" + FileName + "'";
            if (!SectionAnalyze())
                return;
            LoadSectionSelection();
            LoadPager();
            CreateColumns();
            FillRows();
            Initialize = true;
        }

        public Viewer()
        {
            InitializeComponent();
        }
        #endregion
        protected IFormatBase Create<T>(Type type) where T : IFormatBase
        {
            return (IFormatBase)Activator.CreateInstance(type);
        }

        public string[] ReadLines()
        {
            string[] str = null;
            try
            {
                str = File.ReadAllLines(System.IO.Path.Combine(Path, FileName), Encoding.GetEncoding(1251));
            }
            catch
            {
                MessageBox.Show(@"Указанный файл не соответствует формату");
            }
            return str;
        }

        private string[] ReadLinesPartial(int sectionStart, int sectionEnd)
        {
            var array = new List<string>();
            var counter = pageCount = (int)Math.Ceiling((double)(sectionEnd - sectionStart + 1) / pager) < 1 ? 1 : (int)Math.Ceiling((double)(sectionEnd - sectionStart + 1) / pager);
            if (page > GetResultCount() && GetResultCount() != 0)
            {
                page = 1;
                bindingNavigatorCountItem.Text = pageCount != 0
                    ? string.Format(" из {0}", GetResultCount())
                    : string.Format(" из {0}", " из 1");
                bindingNavigatorPositionItem.Text = page.ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                UpdateResultCount();
                UpdateCurrentPage();
            }

            using (var file = new FileStream(System.IO.Path.Combine(Path, FileName), FileMode.Open))
            {
                using (var reader = new StreamReader(file, Encoding.GetEncoding(1251)))
                {
                    var begin = sectionStart + (page - 1) * pager;
                    var end = page == counter ? sectionEnd - (counter - page) * pager : pager * page + sectionStart;
                    var i = 1;
                    while (!reader.EndOfStream && i <= end)
                    {
                        if (i >= begin)
                        {
                            array.Add(i + "|" + reader.ReadLine());
                        }
                        else
                            reader.ReadLine();
                        i++;
                    }
                    reader.Close();
                }
                file.Close();
            }
            return array.ToArray();
        }

        public bool SectionAnalyze()
        {
            var headers = templatesHead;
            listSect = new List<Section>();
            var strings = ReadLines();
            if (strings == null)
                return false;
            lineCount = strings.Count();
            var dict = new Dictionary<string, int>();
            for (var i = 0; i < strings.Count(); i++)
            {
                var section = strings[i].Split('|')[0];
                if (dict.ContainsKey(section))
                {
                    dict[section] = ++dict[section];
                }
                else
                    dict.Add(section, 1);
            }
            var count = 1;
            try
            {
                foreach (var d in dict)
                {
                    listSect.Add(new Section
                    {
                        section_id = Convert.ToInt32(d.Key),
                        section_name = headers[d.Key],
                        section_start = d.Key == "1" ? 1 : count,
                        section_end = count + d.Value - 1
                    });
                    count += d.Value;
                }
            }
            catch
            {
                MessageBox.Show("Неверный формат файла");
                return false;
            }
            return true;
        }

        private void LoadSectionSelection()
        {
            var list = listSect.Select(x => new { sectName = x.section_name + " (" + x.section_start + " - " + x.section_end + ")", x.section_id }).ToList();
            cbSection.DisplayMember = "sectName";
            cbSection.ValueMember = "section_id";
            cbSection.DataSource = list;
        }

        public void CreateColumns()
        {
            var section = cbSection.SelectedValue.ToString();
            var data = bodyDict[section];
            dgAuto.Rows.Clear();
            dgAuto.Columns.Clear();
            dgAuto.Columns.Add(new DataGridViewColumn
            {
                Name = "x",
                CellTemplate = new DataGridViewTextBoxCell(),
                Visible = true,
                ValueType = typeof(string),
                AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells,
                HeaderText = "Номер строки",
                Resizable = DataGridViewTriState.True
            });
            for (var i = 0; i < data.Count; i++)
            {
                var cell = new DataGridViewTextBoxCell(); //Specify which type of cell in this column

                var col = new DataGridViewColumn
                {
                    Name = "x" + i,
                    CellTemplate = cell,
                    Visible = true,
                    ValueType = typeof(string),
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells,
                    HeaderText = data[i].fieldName,
                    Resizable = DataGridViewTriState.True
                };
                dgAuto.Columns.Add(col);
            }
        }

        private void cbSection_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (EnableUpdate)
            {
                PagingPartial = false;
                CreateColumns();
                FillRows();
            }
            EnableUpdate = true;
        }

        public void FillRows()
        {
            if (PagingPartial)
            {
                PartialFillRows();
                return;
            }
            var section = Convert.ToInt32(cbSection.SelectedValue.ToString());
            var obj = listSect.FirstOrDefault(x => x.section_id == section);
            var strings = ReadLinesPartial(obj.section_start, obj.section_end);
            foreach (var str in strings)
            {
                dgAuto.Rows.Add(str.Split('|'));
            }
        }

        #region Пагинатор
        public void UpdateResultCount()
        {
            if (page > GetResultCount() && GetResultCount() != 0) { page--; CreateColumns(); FillRows(); }
            bindingNavigatorCountItem.Text = pageCount != 0 ? string.Format(" из {0}", GetResultCount()) : string.Format(" из {0}", " из 1");
        }

        public void UpdateCurrentPage()
        {
            if (page > GetResultCount() && GetResultCount() != 0) { page--; CreateColumns(); FillRows(); }
            bindingNavigatorPositionItem.Text = page.ToString(CultureInfo.InvariantCulture);
        }

        private void bindingNavigatorMoveFirstItem_Click(object sender, EventArgs e)
        {
            page = 1;
            UpdateCurrentPage();
            CreateColumns();
            FillRows();
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
            CreateColumns();
            FillRows();
        }

        protected int GetResultCount()
        {
            return pageCount;
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
            CreateColumns();
            FillRows();
        }

        private void bindingNavigatorMoveLastItem_Click(object sender, EventArgs e)
        {
            page = GetResultCount() == 0 ? 1 : GetResultCount();
            UpdateCurrentPage();
            CreateColumns();
            FillRows();
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
            CreateColumns();
            FillRows();
        }
        #endregion

        private void button1_Click(object sender, EventArgs e)
        {
            PartialFillRows();
        }

        private void PartialFillRows()
        {
            PagingPartial = true;
            if (tbFrom.Text.Trim().Length == 0 || tbTo.Text.Trim().Length == 0)
            {
                MessageBox.Show(@"Не указан диапозон значений");
                return;
            }
            var fromValue = Convert.ToInt32(tbFrom.Text);
            var toValue = Convert.ToInt32(tbTo.Text);
            if (toValue < fromValue)
            {
                MessageBox.Show(@"Неверно указан диапозон значений");
                return;
            }
            Section section = null;
            foreach (var s in listSect)
            {
                if (s.section_start <= fromValue && s.section_end >= toValue)
                    section = s;
            }
            if (section == null)
            {
                MessageBox.Show(@"Диапозон значений должен быть в рамках определенной секции");
                return;
            }

            EnableUpdate = false;
            cbSection.SelectedValue = section.section_id;
            CreateColumns();
            var strings = ReadLinesPartial(fromValue, toValue);
            foreach (var str in strings)
            {
                dgAuto.Rows.Add(str.Split('|'));
            }
        }

        public string GetFilesIfWorkWithArchive(string Path, string FileName)
        {
            string file = null;
            try
            {
                var tempPath =
                    Directory.CreateDirectory(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "FormatChecker"));
                try
                {
                    file =
                        Archive.GetInstance(Path + FileName)
                            .Decompress(Path + FileName, tempPath.FullName)
                            .FirstOrDefault();
                }
                catch (Exception ex)
                {
                    if (!File.Exists(System.IO.Path.Combine(tempPath.FullName, FileName)))
                        File.Copy(System.IO.Path.Combine(Path, FileName),
                            System.IO.Path.Combine(tempPath.FullName, FileName));
                    file = FileName;
                }
                this.Path = tempPath.FullName + "\\";
            }
            catch
            {
                MessageBox.Show("Указанного файла по заданному пути не найдено");
                this.Close();
            }
            return file;
        }
    }
}
