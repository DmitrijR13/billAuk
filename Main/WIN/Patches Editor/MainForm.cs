using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace PathcesEditor2014
{
    public partial class MainForm : Form
    {
        #region Свойства
        /// <summary>
        /// Путь к папке с файлами
        /// </summary>
        private string DirPath = "";

        /// <summary>
        /// Путь к зашифрованному файлу
        /// </summary>
        private string FilePath = "";

        /// <summary>
        /// Список найденных файлов в папке, [0] - полный путь, [1] - путь, относительно выбранной папки
        /// </summary>
        private List<string[]> files = new List<string[]>();
        #endregion

        #region Обработчики действий формы
        /// <summary>
        /// Конструктор формы
        /// </summary>
        public MainForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Проверка доступности кнопок
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tmrMain_Tick(object sender, EventArgs e)
        {
            if (DirPath == "")
            {
                btnDecrypt.Enabled = btnEncrypt.Enabled = false;
            }
            else
            {
                btnDecrypt.Enabled = btnEncrypt.Enabled = true;
            }

            if (FilePath == "")
            {
                btnSave.Enabled = btnSaveAs.Enabled = false;
            }
            else
            {
                btnSave.Enabled = btnSaveAs.Enabled = true;
            }
        }

        /// <summary>
        /// Выбор папки с файлами
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSelectPath_Click(object sender, EventArgs e)
        {
            UpdateFilesList(true);
        }

        /// <summary>
        /// Смена чекбокса "Рекурсивно"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void chbxRecur_CheckedChanged(object sender, EventArgs e)
        {
            UpdateFilesList(false);
        }

        /// <summary>
        /// Шифрование всех выбранных файлов
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnEncrypt_Click(object sender, EventArgs e)
        {
            try
            {
                foreach (object item in chbxlstFiles.CheckedItems)
                {
                    CryptFile(files[chbxlstFiles.Items.IndexOf(item)][0], true);
                }
                MessageBox.Show("Файлы успешно зашифрованы");
                UpdateFilesList(false);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка шифрования файла: " + ex.Message);
            }
        }

        /// <summary>
        /// Дешифрование всех выбранных файлов
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnDecrypt_Click(object sender, EventArgs e)
        {
            try
            {
                foreach (object item in chbxlstFiles.CheckedItems)
                {
                    CryptFile(files[chbxlstFiles.Items.IndexOf(item)][0], false);
                }
                MessageBox.Show("Файлы успешно расшифрованы");
                UpdateFilesList(false);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка расшифровки файла: " + ex.Message);
            }
        }

        /// <summary>
        /// Открытие файла
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnOpen_Click(object sender, EventArgs e)
        {
            if (dlgOpenFile.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;

            FilePath = dlgOpenFile.FileName;

            try
            {
                rtbFile.Clear();
                using (StreamReader sr = new StreamReader(FilePath, Encoding.GetEncoding(1251)))
                {
                    string str = "";
                    while ((str = sr.ReadLine()) != null)
                    {
                        rtbFile.Text += StringCrypter.Decrypt(str) + Environment.NewLine;
                    }
                    rtbFile.Text = rtbFile.Text.TrimEnd('\r', '\n');
                }
            }
            catch (Exception ex)
            {
                FilePath = "";
                rtbFile.Clear();
                MessageBox.Show("Ошибка открытия файла: " + ex.Message);
            }
        }

        /// <summary>
        /// Обработчик кнопки "Сохранить"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSave_Click(object sender, EventArgs e)
        {
            SaveToDecryptFile();
        }

        /// <summary>
        /// Обработчик кнопки "Сохранить как..."
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSaveAs_Click(object sender, EventArgs e)
        {
            if (dlgSaveFile.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;

            FilePath = dlgSaveFile.FileName;
            SaveToDecryptFile();
        }
        #endregion

        #region Функции работы с файлами
        /// <summary>
        /// Получение всех файлов в директории
        /// </summary>
        /// <param name="files">ссылка на список файлов</param>
        /// <param name="path">относительный путь от первой выбранной папки</param>
        /// <param name="recur">true - рекурсивный просмотр папок</param>
        void GetAllFiles(ref List<string[]> files, string path, bool recur)
        {
            DirectoryInfo dir = new DirectoryInfo(Path.Combine(DirPath, path));
            foreach (var file in dir.GetFiles("*.*"))
            {
                bool check = false;
                if ((rbAll.Checked) && ((file.Extension == ".sq") || (file.Extension == ".sqc") || (file.Extension == ".ini") || (file.Extension == ".inic"))) check = true;
                else if ((rbCrypted.Checked) && ((file.Extension == ".sqc") || (file.Extension == ".inic"))) check = true;
                else if ((rbNonCrypted.Checked) && ((file.Extension == ".sq") || (file.Extension == ".ini"))) check = true;
                if (check)
                {
                    files.Add(new string[2] { file.FullName, Path.Combine(path, file.Name) });
                }
            }

            if (recur)
            {
                foreach (var d in dir.GetDirectories())
                {
                    GetAllFiles(ref files, Path.Combine(path, d.Name), recur);
                }
            }
        }

        /// <summary>
        /// Обновление списка файлов
        /// </summary>
        /// <param name="selectPath">true - выбрать путь к файлам заново</param>
        void UpdateFilesList(bool selectPath)
        {
            if (selectPath)
            {
                if (dlgOpenDir.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                    return;
                tbDirPath.Text = DirPath = dlgOpenDir.SelectedPath;
            }
            else
            {
                if (DirPath == "") return;
            }

            files.Clear();
            GetAllFiles(ref files, "", chbxRecur.Checked);
            chbxlstFiles.Items.Clear();
            chbxlstFiles.Items.AddRange(files.Select(x => x[1]).ToList().ToArray());
            for (int i = 0; i < chbxlstFiles.Items.Count; i++)
            {
                chbxlstFiles.SetItemChecked(i, true);
            }
        }

        /// <summary>
        /// Шифрование/Дешифрование файла
        /// </summary>
        /// <param name="path">Путь к файлу</param>
        /// <param name="crypt">true - зашифровать</param>
        void CryptFile(string path, bool crypt)
        {
            if (((Path.GetExtension(path) == ".sq") || (Path.GetExtension(path) == ".ini")) && (!crypt)) return;
            if (((Path.GetExtension(path) == ".sqc") || (Path.GetExtension(path) == ".inic")) && (crypt)) return;
            using (StreamReader sr = new StreamReader(path, Encoding.GetEncoding(1251)))
            using (StreamWriter sw = new StreamWriter(crypt ? path + "c" : path.Substring(0, path.Length - 1), false, Encoding.GetEncoding(1251)))
            {
                string str = "";
                while ((str = sr.ReadLine()) != null)
                {
                    if (crypt)
                    {
                        sw.WriteLine(StringCrypter.Encrypt(str));
                    }
                    else
                    {
                        sw.WriteLine(StringCrypter.Decrypt(str));
                    }
                }
            }
        }

        /// <summary>
        /// Сохранения зашифрованного файла
        /// </summary>
        void SaveToDecryptFile()
        {
            try
            {
                File.SetAttributes(FilePath, FileAttributes.Normal);
                using (StreamWriter sw = new StreamWriter(FilePath, false, Encoding.GetEncoding(1251)))
                {
                    foreach (var s in rtbFile.Lines)
                    {
                        sw.WriteLine(StringCrypter.Encrypt(s));
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка сохранения файла: " + ex.Message);
            }
        }
        #endregion
    }
}
