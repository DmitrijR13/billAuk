using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using Bars.KP50.DataImport.SOURCE;
using STCLINE.KP50;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using Globals.SOURCE;


namespace Bars.File.Loader
{
    public partial class Form1 : Form
    {
        private string strFileName;
        private string strFilePath;
        protected string connectionString;// = "Server=192.168.170.215;Port=5432;User Id=postgres;Password=postgres;Database=webtul;Preload Reader=true;";
        private IDbConnection con_db;
        protected string path;// = @"D:\projects\Billing.GKH.KP50\Dev\Bars.File.Loader\Bars.File.Loader\bin\Debug PG\import\";
        private string pref = "pftul"; 
        
        protected StatusBar mainStatusBar = new StatusBar();
        protected StatusBarPanel statusPanel = new StatusBarPanel();
        protected StatusBarPanel datetimePanel = new StatusBarPanel();

        public Form1()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Нажатие кнопки "Проверить файл"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            con_db = DBManager.GetConnection(connectionString);

            Returns ret = DBManager.OpenDb(con_db, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка открытия соединения в ф-ции button1_Click", MonitorLog.typelog.Error, true);
                MessageBox.Show("Ошибка подсоединения к базе данных. Проверьте настройки соединения.");
                return;
            }
            try
            {
                if (strFileName == null || strFilePath == null)
                {
                    MessageBox.Show("Не выбран файл для загрузки.");
                    return;
                }

                FilesImported finder = new FilesImported();
                finder.nzp_user = 1;
                finder.loaded_name = strFileName;
                finder.saved_name = "u" + finder.nzp_user + "_" + finder.loaded_name.Substring(0, finder.loaded_name.Trim().Length - 4) + 
                    DateTime.Now.ToFileTime() + finder.loaded_name.Substring(finder.loaded_name.Trim().Length - 4, 4);
                finder.bank = "checkFile";
                finder.sections = new bool[29];
                for (int i = 0; i < 29; i++)
                {
                    finder.sections[i] = true;
                }

                
                //пересохранение файла
                System.IO.File.Delete(path + finder.saved_name);
                System.IO.File.Copy(strFilePath + "\\" + strFileName, path + finder.saved_name);

                MessageBox.Show("Файл проверяется, дождитесь появления названия файла в таблице загрузок.");

                statusPanel.Text = "Проверка файла " + finder.loaded_name;

                DbFileLoader fl = new DbFileLoader(con_db);
                ret = fl.LoadFile(finder, ref ret);
                if (!ret.result)
                {
                    MessageBox.Show(ret.text);
                    return;
                }

                System.IO.File.Delete(path + finder.saved_name);

                FillRows();

                statusPanel.Text = "Файл проверен";

            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                con_db.Close();
            }
        }


        /// <summary>
        /// Выбор файла
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LoadFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Stream myStream = null;
            OpenFileDialog openFileDialog = new OpenFileDialog();

            //openFileDialog1.InitialDirectory = "c:\\";
            openFileDialog.Filter = @"rar files (*.rar)|*.rar|zip files (*.zip)|*.zip";
            openFileDialog.FilterIndex = 2;
            openFileDialog.RestoreDirectory = true;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    FileInfo fInfo = new FileInfo(openFileDialog.FileName);
                    strFileName = fInfo.Name;
                    strFilePath = fInfo.DirectoryName;
                    statusPanel.Text = "Выбран файл " + strFileName;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: Could not read file from disk. Original error: " + ex.Message);
                }
            }
        }

        /// <summary>
        /// Загрузка формы
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_Load(object sender, EventArgs e)
        {
            //чтение соединения и пути
            ReadSettingsFromFile();

            //Настройка параметров для функций из Комплата
            SetParams();

            //загрузка таблицы
            LoadFilesGrid();
            FillRows();

            //строка состояния
            CreateStatusBar();
        }

        /// <summary>
        /// Формирование колонок грида
        /// </summary>
        private void LoadFilesGrid()
        {
            dataGridView1.Columns.Clear();
            dataGridView1.AutoGenerateColumns = false;
            dataGridView1.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.False;

            DataGridViewTextBoxColumn c0 = new DataGridViewTextBoxColumn();
            c0.Width = 30;
            c0.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            c0.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            c0.DataPropertyName = "numFile";
            c0.Name = "numFile";
            c0.HeaderText = "Номер файла";
            c0.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;

            DataGridViewTextBoxColumn c1 = new DataGridViewTextBoxColumn();
            //c1.Width = 200;
            c1.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            c1.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            c1.DataPropertyName = "fileName";
            c1.Name = "fileName";
            c1.HeaderText = "Имя файла";
            c1.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            DataGridViewTextBoxColumn c2 = new DataGridViewTextBoxColumn();
            c2.Width = 50;
            c2.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            c2.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            c2.DataPropertyName = "dateFile";
            c2.Name = "dateFile";
            c2.HeaderText = "Дата загрузки";
            c2.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;

            DataGridViewLinkColumn c3 = new DataGridViewLinkColumn();
            c3.Width = 30;
            c3.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            c3.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            c3.DataPropertyName = "logFile";
            c3.Name = "logName";
            c3.HeaderText = "Лог ошибок";
            c3.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;

            DataGridViewLinkColumn c4 = new DataGridViewLinkColumn();
            c4.Width = 0;
            c4.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            c4.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            c4.DataPropertyName = "savedName";
            c4.Name = "savedName";
            c4.HeaderText = "Сохраненное имя";
            c4.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            c4.Visible = false;

            DataGridViewLinkColumn c5 = new DataGridViewLinkColumn();
            c5.Width = 0;
            c5.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            c5.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            c5.DataPropertyName = "nzpFile";
            c5.Name = "nzpFile";
            c5.HeaderText = "Код файла";
            c5.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            c5.Visible = false;

            dataGridView1.Columns.Add(c0);
            dataGridView1.Columns.Add(c1);
            dataGridView1.Columns.Add(c2);
            dataGridView1.Columns.Add(c3);
            dataGridView1.Columns.Add(c4);
            dataGridView1.Columns.Add(c5);

            //dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            dataGridView1.AutoResizeColumns();
        }

        /// <summary>
        /// Получение списка проверенных файлов
        /// </summary>
        /// <returns></returns>
        private List<RowClass> GetRows()
        {
            List<RowClass> row_list = new List<RowClass>();

            con_db = DBManager.GetConnection(connectionString);

            Returns ret = DBManager.OpenDb(con_db, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка открытия соединения в ф-ции GetRows", MonitorLog.typelog.Error, true);
                MessageBox.Show("Ошибка подсоединения к базе данных. Проверьте настройки соединения.");
                return row_list;
            }
            try
            {
                IDataReader reader;
                string sql = " select loaded_name, nzp_exc_log, created_on, saved_name, nzp_file " +
                             " from " + Points.Pref + "_upload.files_imported " +
                             " where trim(upper(pref)) like upper('checkFile')" +
                             " and nzp_status <> 7 " +
                             " order by created_on desc";
                ret = DBManager.ExecRead(con_db, out reader, sql, true);
                if (!ret.result)
                {
                    MessageBox.Show("Ошибка заполения таблицы проверяемых файлов: " + ret.text);
                    return row_list;
                }

                int i = 0;
                while (reader.Read())
                {
                    i++;
                    RowClass rc = new RowClass();
                    rc.num = i.ToString();
                    rc.fileName = reader["loaded_name"] != DBNull.Value ? reader["loaded_name"].ToString() : null;
                    rc.dateFile = reader["created_on"] != DBNull.Value ? reader["created_on"].ToString() : null;
                    rc.logLink = reader["nzp_exc_log"] != DBNull.Value ? reader["nzp_exc_log"].ToString() : null;
                    rc.savedName = reader["saved_name"] != DBNull.Value ? reader["saved_name"].ToString() : null;
                    rc.nzp_file = reader["nzp_file"] != DBNull.Value ? Convert.ToInt32(reader["nzp_file"]) : 0;

                    row_list.Add(rc);
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                con_db.Close();
            }

            return row_list;
        }

        /// <summary>
        /// Заполнение грида
        /// </summary>
        private void FillRows()
        {
            List<RowClass> lrc = GetRows();
            dataGridView1.Rows.Clear();
            if (lrc.Count > 0)
            {
                foreach (RowClass rc in lrc)
                {
                    dataGridView1.Rows.Add(rc.num, rc.fileName,
                        rc.dateFile, rc.logLink, rc.savedName, rc.nzp_file);
                }
            }
        }

        /// <summary>
        /// Обработка нажатия на строку грида
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            int row = dataGridView1.CurrentCell.RowIndex;
            int col = dataGridView1.CurrentCell.ColumnIndex;

            dataGridView1.ClearSelection();
            dataGridView1.Rows[row].Selected = true;

            if (col == 3)
            {
                //string filePath = path + dataGridView1[4, row].Value.ToString().Trim();
                //filePath = filePath.Substring(0, filePath.Length - 4) + ".txt_LOG.zip";
                string filePath = dataGridView1[4, row].Value.ToString().Trim();
                filePath = path + Path.GetFileNameWithoutExtension(filePath) + ".txt_LOG.zip";

                if (System.IO.File.Exists(filePath))
                {
                    Process.Start(filePath);
                }
                else
                {
                    MessageBox.Show("Файл не найден");
                }
            }
            
        }

        /// <summary>
        /// Обработка нажатия кнопки "Удалить файл"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
        {
            con_db = DBManager.GetConnection(connectionString);

            Returns ret = DBManager.OpenDb(con_db, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка открытия соединения в ф-ции button2_Click", MonitorLog.typelog.Error, true);
                MessageBox.Show("Ошибка подсоединения к базе данных. Проверьте настройки соединения.");
                return;
            }
            try
            {
                if (dataGridView1.RowCount == 0)
                {
                    MessageBox.Show("Таблица файлов пуста.");
                    return;
                }

                int row = dataGridView1.CurrentCell.RowIndex;
                string nzp_f = dataGridView1[5, row].Value.ToString().Trim();

                FilesDisassemble finder = new FilesDisassemble();
                finder.nzp_user = 1;
                finder.nzp_file = Convert.ToInt32(nzp_f);
                finder.bank = "checkFile";

                statusPanel.Text = "Удаление файла ";

                DbDeleteImportedFile dif = new DbDeleteImportedFile(con_db);
                ret = dif.DeleteImportedFile(finder);
                if (!ret.result)
                {
                    MessageBox.Show(ret.text);
                    return;
                }

                //string filePath = path + dataGridView1[4, row].Value.ToString().Trim();
                //filePath = System.IO.Path.GetFileNameWithoutExtension(filePath) + ".txt_LOG.zip";
                //    //filePath.Substring(0, filePath.Length - 4) + ".txt_LOG.zip";
                string filePath = dataGridView1[4, row].Value.ToString().Trim();
                filePath = path + Path.GetFileNameWithoutExtension(filePath) + ".txt_LOG.zip";

                System.IO.File.Delete(filePath);

                dataGridView1.Rows.RemoveAt(row);

                MessageBox.Show("Файл успешно удалён!");

                statusPanel.Text = "Файл удален ";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка удаления файла: " + ex.Message);
            }
            finally
            {
                con_db.Close();
            }
        }

        /// <summary>
        /// Строка состояния
        /// </summary>
        private void CreateStatusBar()
        {
            // Set first panel properties and add to StatusBar
            statusPanel.BorderStyle = StatusBarPanelBorderStyle.Sunken;
            statusPanel.Text = "";
            statusPanel.ToolTipText = "Last Activity";
            statusPanel.AutoSize = StatusBarPanelAutoSize.Spring;
            mainStatusBar.Panels.Add(statusPanel);
            
            // Set second panel properties and add to StatusBar
            datetimePanel.BorderStyle = StatusBarPanelBorderStyle.Raised;
            datetimePanel.ToolTipText = "DateTime: " + System.DateTime.Today.ToString();
            datetimePanel.Text = System.DateTime.Today.ToLongDateString() + " .";
            datetimePanel.AutoSize = StatusBarPanelAutoSize.Contents;
            mainStatusBar.Panels.Add(datetimePanel);
            
            mainStatusBar.ShowPanels = true;
            // Add StatusBar to Form controls
            this.Controls.Add(mainStatusBar);
        }

        /// <summary>
        /// Получение настроек со второй формы
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SetingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form2 settings = new Form2(connectionString, path);
            var result = settings.ShowDialog();
            if (result == DialogResult.Cancel)
            {
                connectionString = settings.connection;
                path = settings.directiry;
                string pr = connectionString.Substring(connectionString.IndexOf("=web") + 4, 3);
                pr = "f" + pr;
                pref = pr;

                //Меняем параметры комплата
                Constants.cons_Webdata = connectionString;
                Constants.cons_Kernel = connectionString;

                statusPanel.Text = "Сохранение настроек";

                //Сохраняем изменения в файле
                SaveChangesInFile();

                FillRows();

                statusPanel.Text = "Настройки сохранены";
            }
        }

        /// <summary>
        /// Чтение настроек с файла
        /// </summary>
        private void ReadSettingsFromFile()
        {
            string dir = Directory.GetCurrentDirectory();
            if (System.IO.File.Exists(dir + @"\\configFile.txt"))
            {
                string line;
                //считываем настройки
                string[] lines = System.IO.File.ReadAllLines(dir + @"\\configFile.txt");
                connectionString = lines[0];
                path = lines[1];
                if (connectionString == "" || path == "")
                {
                    MessageBox.Show("Настройте соединение и директорию.");
                }
                else
                {
                    string pr = connectionString.Substring(connectionString.IndexOf("=web") + 4, 3);
                    pr = "f" + pr;
                    pref = pr;
                    pref = "pftul";
                }
            }
            else
            {
                MessageBox.Show("Настройте соединение и директорию.");
            }
        }

        /// <summary>
        /// Настройка параметров для функций из KOMPLAT 5.0 
        /// </summary>
        private void SetParams()
        {
            //лог ошибок
            ILog logger = NLogLogger.Create("nlog", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "nlog.config"));
            MonitorLog.SetLogger(logger);
            MonitorLog.StartLog("STCLINE.KP50.Host", "Старт приложения");

            //connections
            Constants.cons_Webdata = connectionString;
            Constants.cons_Kernel = connectionString;

            Points.Pref = pref;
            Constants.Directories.FilesDir = "";

            if (InputOutput.useFtp)
            {
                InputOutput.InitializeFileManager(FileManager.GetFtpInstance(Constants.Directories.FilesDir));
            }
            else
            {
                InputOutput.InitializeFileManager(FileManager.GetFolderInstance(Constants.Directories.FilesDir));
            }
            string dir = InputOutput.GetInputDir();
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        }

        /// <summary>
        /// Сохранение измененных настроек в файле
        /// </summary>
        private void SaveChangesInFile()
        {
            string dir = Directory.GetCurrentDirectory();
            string[] lines = {connectionString, path};
            System.IO.File.WriteAllLines(dir + @"\\configFile.txt", lines);
        }
    }
}
