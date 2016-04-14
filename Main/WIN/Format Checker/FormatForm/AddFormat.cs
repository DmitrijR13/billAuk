using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using FormatLibrary;
using System.IO;
using System.Text;

namespace FormatForm
{
    public partial class AddFormat : Form
    {
        private string[] FileNames;
        public string FileFormat;
        #region Инициализация формы
        public AddFormat()
        {
            InitializeComponent();
            CenterToScreen();
            FormatCreator.Instance.LoadAllDLL();
            var list = FormatCreator.Instance.GetAllFormattedNames();
            FormatCollection.DisplayMember = "FormatName";
            FormatCollection.ValueMember = "type";
            FormatCollection.DataSource = list;
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
                            Format = FormatCollection.Text.ToString(CultureInfo.InvariantCulture),
                            type = (FormatCollection.SelectedValue as Type),
                            StatusID = Statuses.Added,
                            Status = "Добавлен",
                            formatID = 0,
                            date = DateTime.Now,
                            WithEndSymbol = !cbEndSymbol.Checked
                        });
                });
            if (!list.Any())
            {
                MessageBox.Show(string.Format("Выберите файлы для проверки"));
                return;
            }
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
                    OpenDialog.Filter = @"Text Files (.txt)|*.txt|Archive Files (.rar,.zip,.7z)|*.rar;*.zip;*.7z|All Files (*.*)|*.*";
                    OpenDialog.FilterIndex = 1;
                    OpenDialog.Multiselect = true;
                    if (OpenDialog.ShowDialog() == DialogResult.OK)
                    {
                        try
                        {
                            var Path = OpenDialog.FileNames[0].Replace(OpenDialog.FileNames[0].Split('\\').Last(), "");
                            var FileName = GetFilesIfWorkWithArchive(Path, OpenDialog.FileNames[0].Split('\\').Last());
                            if (File.Exists(System.IO.Path.Combine(Path, FileName)))
                            {
                                var lines = File.ReadAllLines(System.IO.Path.Combine(Path, FileName),
                                    Encoding.GetEncoding(1251));
                                var split = lines[0].Split('|');
                                lbFormatName.Text = string.Format("Формат первого выбранного файла:{0}", split[1]);

                                FormatCollection.SelectedValue = FormatCreator.Instance.GetAllFormattedNames().FirstOrDefault(x => x.Version == split[1]).type;
                            }
                        }
                        catch
                        {
                        }
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

        public string GetFilesIfWorkWithArchive(string Path, string FileName)
        {
            string file;
            try
            {
                file = Archive.GetInstance(Path + FileName).Decompress(Path + FileName, Path).FirstOrDefault();
            }
            catch (Exception ex)
            {
                file = FileName;
            }
            return file;
        }

    }
}
