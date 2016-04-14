using System;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Data;
using webroles.GenerateScriptTable;
using webroles.Windows;

namespace webroles
{
    public partial class MainWindow : Form
    {
        #region Variables
        int indexNewRow;
        public static int UserId { get; private set; }
        public static string Login { get; private set; }
        public static string UserName { get; private set; }
        readonly BindingSource dataGridViewBinSour = new BindingSource();
        private CreateAllTables tables;
        Label currentTableLabel;
        Label selectedPointLabel;
        private int i;
        private int j;
        private string prevSearchText;
      
        #endregion
        public MainWindow()
        {
            InitializeComponent();
            
        }

        protected override void OnShown(EventArgs e)
        {
           // Assembly.Load("Npgsql");
           // Assembly.Load("Mono.Security");
            base.OnShown(e);
            var autorization = new AutorizationWindow();
            if (autorization.ShowDialog() == DialogResult.Cancel)
            {
                Close();
                return;
            }
            Text = "RoleManager" + " - Пользователь: " + "[" + autorization.UserId + "," + autorization.Login + "]";
            UserId = autorization.UserId;
            Login = autorization.Login;
            UserName = autorization.UserName;
            Label lbl = new Label();
            lbl.Text = "Текущая таблица: ";
            lbl.AutoSize = true;
            lbl.Margin = new Padding(2, 6, 0, 0);
            tableLayoutPanel1.Controls.Add(lbl);
            currentTableLabel = new Label();
            currentTableLabel.AutoSize = true;
            currentTableLabel.Margin= new Padding(0, 6, 5, 0);
            currentTableLabel.BorderStyle = BorderStyle.FixedSingle;
            tableLayoutPanel1.Controls.Add(currentTableLabel);
            //Label lbl2 = new Label();
            //lbl2.Text = "Выбранный пункт таблицы первого уровня: ";
            //lbl2.AutoSize = true;
            //lbl2.Margin = new System.Windows.Forms.Padding(10, 6, 0, 0);
            //tableLayoutPanel1.Controls.Add(lbl2);
            selectedPointLabel = new Label();
            selectedPointLabel.AutoSize = true;
            selectedPointLabel.Margin = new Padding(0, 6, 5, 0);
            selectedPointLabel.BorderStyle = BorderStyle.FixedSingle;
            tableLayoutPanel1.Controls.Add(selectedPointLabel);
            dataGridView.KeyUp += dataGridView_KeyDown;
            GenerateScript.ScriptCollectionChanged += GenerateScript_ScriptCollectionChanged;
            if (!fillMainForm()) Close(); 
            
        }

        void dataGridView_KeyDown(object sender, KeyEventArgs e)
        {
            
            if (e.KeyValue==40 || e.KeyValue==38)
                selectedPointLabel.Text = tables.GetNamePositionFirstLevelTable();
            }

        void GenerateScript_ScriptCollectionChanged(object sender, ScriptCollectionEventArgs e)
        {
            ChangeCollectCount.Text = e.Count.ToString();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            if (!Text.Last().Equals('*')) return;
            var dialogRezult = MessageBox.Show("Сохранить сделанные изменения в базу данных?", "Сохранение изменений", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
            if (dialogRezult == DialogResult.Cancel)
            {
                e.Cancel = true; 
                return;
            }
            if (dialogRezult == DialogResult.No) return;
            tables.SaveCurrentTable();
        }
        private bool fillMainForm()
        { 
            bindingNavigator1.BindingSource = dataGridViewBinSour;
            dataGridView.DataSource = dataGridViewBinSour;
            dataGridView.AutoGenerateColumns = false;
            dataGridView.AllowUserToAddRows = false;
            dataGridView.CausesValidation = true;
            dataGridView.RowsAdded += dataGridView_RowsAdded;
            dataGridView.AllowUserToDeleteRows = true;
            dataGridView.CellValueChanged += dataGridView_CellValueChanged;
            dataGridView.CellBeginEdit += dataGridView_CellBeginEdit;
            dataGridView.CellValidating += dataGridView_CellValidating;
            tables = new CreateAllTables(treeView1, dataGridView, dataGridViewBinSour, selectedPointLabel, statusStrip);
            if (!tables.FillDataSet()) return false;
            dataGridView.DataError += dataGridView_DataError;
            dataGridView.ColumnHeaderMouseClick += dataGridView_ColumnHeaderMouseClick;
            dataGridView.CellClick += dataGridView_CellClick;
            treeView1.BeforeSelect += treeView1_BeforeSelect;
            return true;
        }



        void dataGridView_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {

                e.Cancel = tables.RequarimentSaveChanges(e.ColumnIndex, Text);
               // tables.SetIndividualDataSourceToComboBoxCell();
        }

        private void treeView1_BeforeSelect(object sender, TreeViewCancelEventArgs e)
        {
            i = 0;
            j = 0;
            if (!Text.Last().Equals('*')) return;
            var dialogRezult = MessageBox.Show("Сохранить сделанные изменения в базу данных?", "Сохранение изменений", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
            if (dialogRezult == DialogResult.Cancel)
            {
                e.Cancel = true;
                return;
            }
            if (dialogRezult == DialogResult.No)
            {
                Text = Text.Remove(Text.Count() - 1);
                tables.RefreshTable();
                return;
            }
              tables.SaveCurrentTable();
              Text = Text.Remove(Text.Count() - 1);
        }

        void dataGridView_CellClick(object sender, DataGridViewCellEventArgs e)
        {          
            selectedPointLabel.Text = tables.GetNamePositionFirstLevelTable();
            //dataGridViewBinSour.EndEdit();
            //tables.SetIndividualDataSourceToComboBoxCell();
        }
        private void bindingNavigatorChangePosition_Click(object sender, EventArgs e)
        {
            selectedPointLabel.Text = tables.GetNamePositionFirstLevelTable();
            //tables.SetIndividualDataSourceToComboBoxCell();       
        }
        void dataGridView_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            tables.SetIndividualDataSourceToComboBoxCell();
        }

        private void bindingNavigatorAddNewItem_Click(object sender, EventArgs e)
        {
            if (!tables.CheckToAllowEdit(EditOperations.Add))
                return;
            try
            {
             //   tables.SortBeforeInsert();
              //  dataGridViewBinSour.EndEdit();
                dataGridViewBinSour.AddNew();
            }
            catch (InvalidConstraintException ex)
            {
                MessageBox.Show(ex.Message);
            }
            if (!Text.Last().Equals('*')) Text = Text + "*";
            dataGridView.CellValueChanged -= dataGridView_CellValueChanged;
            tables.SetDefaultValsAfterRowAdded(treeView1.SelectedNode.Text, indexNewRow);
            selectedPointLabel.Text = tables.GetNamePositionFirstLevelTable();
            dataGridView.CellValueChanged += dataGridView_CellValueChanged;
            tables.SetIndividualDataSourceToComboBoxCell();
        }

        void dataGridView_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            dataGridView.Rows[e.RowIndex].ErrorText = "В колонке "+ dataGridView.Columns[e.ColumnIndex].HeaderText+ " в строке "+ e.RowIndex+ " некорректное значение";
        }

        void dataGridView_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            dataGridView.Rows[e.RowIndex].ErrorText = "";
        }

        void dataGridView_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                //dataGridView.EndEdit();
                dataGridViewBinSour.EndEdit();
                dataGridView.CellValueChanged -= dataGridView_CellValueChanged;
            }
            catch (InvalidConstraintException ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }
            if (!tables.SetDefaultValsAfterCellChang(treeView1.SelectedNode.Text, e.RowIndex, e.ColumnIndex))
            {
                if (!Text.Last().Equals('*')) Text = Text + "*";
              //  UpdateRowWindow urw = new UpdateRowWindow(dataGridView.Rows[e.RowIndex]);
            }
            dataGridView.CellValueChanged += dataGridView_CellValueChanged;
        }
        void dataGridView_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            indexNewRow = e.RowIndex;
        }
        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
           currentTableLabel.Text=tables.treeViewAfterSelect(e.Node.Level);
        }
       private void bindingNavigatorDeleteItem_Click(object sender, EventArgs e)
       {
           if (!tables.CheckToAllowEdit(EditOperations.Delete))
               return;
           if (dataGridView.Rows.Count == 0) return;
           if (!tables.CheckToAllowDelete()) return;
           if (!Text.Last().Equals('*')) Text = Text + "*";
           try
           {
               dataGridViewBinSour.RemoveCurrent();
               dataGridViewBinSour.EndEdit();
               selectedPointLabel.Text = tables.GetNamePositionFirstLevelTable();
           }
           catch (InvalidConstraintException ex)
           {
               MessageBox.Show(ex.Message);
           }
       }

       private void saveCurrentTableBut_Click(object sender, EventArgs e)
       {
           if (!tables.CheckToAllowEdit(EditOperations.Save))
               return;
           tables.SaveCurrentTable();
           if (Text.Last().Equals('*')) Text = Text.Remove(Text.Count() - 1);
       }
        // Кнопка выгрузки скриптов
       private void uploadSqlScript_Click(object sender, EventArgs e)
       {
           TypeScriptWindow tsw = new TypeScriptWindow();
           if (tsw.ShowDialog() == DialogResult.Cancel) return;
           tables.UploadSqlScriptForCustomer(tsw.typeScript);
       }
        // кнопка обновить
       private void refreshButton_Click(object sender, EventArgs e)
       {
           tables.RefreshTable();
       }
        // кнопка авторизации
       private void autorizationButton_Click(object sender, EventArgs e)
       {
           var autorization = new AutorizationWindow();
           if (autorization.ShowDialog() == DialogResult.Cancel)
           { 
               return;
           }
           Text = "RoleManager" + " - Пользователь: " + "[" + autorization.UserId + "," + autorization.Login + "]";
           UserId = autorization.UserId;
           Login = autorization.Login;
           UserName = autorization.UserName;
       }

       private void searchToolStripButton_Click(object sender, EventArgs e)
       {
           Regex regex = null;
           var wholeWordOnly = (ToolStripMenuItem)searchToolStripButton.DropDownItems["wholeWordOnlyItem"];
           var caseItm = (ToolStripMenuItem)searchToolStripButton.DropDownItems["caseItem"];
           if (searchToolStripTextBox.Text != "")
           {
               if (wholeWordOnly.Checked && caseItm.Checked)
                   regex = new Regex(@"(\W|^)" + @searchToolStripTextBox.Text.Trim() + @"(\W|$)", RegexOptions.None);
               if (wholeWordOnly.Checked && !caseItm.Checked)
                   regex = new Regex(@"(\W|^)" + @searchToolStripTextBox.Text.Trim() + @"(\W|$)",
                       RegexOptions.IgnoreCase);
               if (!wholeWordOnly.Checked && caseItm.Checked)
                   regex = new Regex(@searchToolStripTextBox.Text, RegexOptions.None);
               if (!wholeWordOnly.Checked && !caseItm.Checked)
                   regex = new Regex(@searchToolStripTextBox.Text, RegexOptions.IgnoreCase);
           }
           // Обнуление переменных цикла поиска
          if (prevSearchText!=null)
           {
               if (!prevSearchText.Equals(searchToolStripTextBox.Text))
               {
                   i = 0;
                   j = 0;
               }   
           }
          prevSearchText = searchToolStripTextBox.Text;
           for (; i < dataGridView.ColumnCount;i++ )
           {
               if (!dataGridView.Columns[i].Visible) continue;
               //j = 0;
               for (; j < dataGridView.RowCount; j++)
               {
                   // Если строка поиска НЕ пустая
                   if (!searchToolStripTextBox.Text.Equals(""))
                   {
                       // Если колонка в которой производится поиск имеет тип DataGridViewComboBoxColumn
                       if (dataGridView.Columns[i] is DataGridViewComboBoxColumn)
                       {
                           //  
                           DataTable dataSourceTable1 = ((DataGridViewComboBoxColumn)dataGridView.Columns[i]).DataSource as DataTable;
                           // У ComboBox колонок таблиц role_actions и actions_lnk Datasource равен null, т.к. у каждой ComboBox ячейки свой DataSource
                           if (dataSourceTable1 == null)
                           {
                               // и здесь он определяется
                               DataTable dt = ((DataGridViewComboBoxCell) dataGridView.Rows[j].Cells[i]).DataSource as DataTable;
                               if (search(dt, i, ref j, regex)) return;
                           }
                               // Если у колонки ComboBox dataSource есть 
                           else
                           {
                               if (search(dataSourceTable1, i, ref j, regex)) return;
                           }                       
                       }
                       // Если колонка в которой производится поиск НЕ  имеет тип DataGridViewComboBoxColumn
                       else
                       {
                           if (searchInTextBoxCell(i, ref j, regex)) return;
                       }
                   }
                       // Если строка поиска ПУСТАЯ
                   else
                   {
                       // Если колонка в которой производится поиск  имеет тип DataGridViewComboBoxColumn
                       if (dataGridView.Columns[i] is DataGridViewComboBoxColumn)
                       {
                           var dataSourceTable =
                               ((DataGridViewComboBoxColumn) dataGridView.Columns[i]).DataSource as DataTable;
                           if (dataSourceTable == null)
                           {
                               DataTable dataSourceTable2 =
                                   ((DataGridViewComboBoxCell)dataGridView.Rows[j].Cells[i]).DataSource as DataTable;
                               if (search(dataSourceTable2, i, ref j, regex)) return;
                           }
                           else if (search(dataSourceTable, i, ref j, regex)) return;
                       }                            
                           // Если колонка в которой производится поиск НЕ имеет тип DataGridViewComboBoxColumn
                       else
                       {
                           if (searchInTextBoxCell(i, ref j, regex)) return;
                       }
                   }
               }
               if (j == dataGridView.RowCount)
               {
                   if (i!=dataGridView.ColumnCount-1) j = 0;
               }
           }

           if (i == dataGridView.ColumnCount & j == dataGridView.RowCount)
           {
               MessageBox.Show("Достигнут конец таблицы");
               i = 0;
               j = 0;
           }
       }
        private void searchToolStripTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyValue != 13) return;
            searchToolStripButton_Click(sender, new EventArgs());
        }
        private bool search(DataTable dtSource, int iVar, ref int jVar, Regex regexVar)
        {
            bool rezult;
        //    var t = dataGridView[iVar, jVar].Value.GetType();
            if (dataGridView[iVar, jVar].Value != null)
            {
                if (dataGridView[iVar, jVar].Value.GetType() != typeof (DBNull))
                {
                    int num =  Convert.ToInt32(dataGridView[iVar, jVar].Value);
                    // извлекаем из dataSource ComboBoxCell только те значения, которые соответствуют num
                    var selectTextList = from row in dtSource.AsEnumerable()
                        let field = row.Field<int?>("id")
                        where field != null && (field.Value.GetType()!=typeof(DBNull) && field.Equals(num))
                        select row.Field<string>("text");
                    if (!selectTextList.Any()) return false; // если список пуст
                    if (regexVar == null)
                    {
                        rezult = selectTextList.First().
                            Equals(searchToolStripTextBox.Text);
                    }
                    else
                    {
                        rezult = regexVar.IsMatch(selectTextList.First());
                    }
                    if (!rezult) return false;
                    dataGridView.CurrentCell = dataGridView[iVar, jVar];
                   // selectedPointLabel.Text = tables.getNamePosit();
                    selectedPointLabel.Text = tables.GetNamePositionFirstLevelTable();
                    jVar++;
                    return true;
                }
            }
            // равенство слову незадано
            if (regexVar == null)
            {
                rezult = dtSource.Rows[dtSource.Rows.Count - 1].Field<string>("text").
                    Equals(searchToolStripTextBox.Text);
            }
            else
            {
                var g = dtSource.Rows[dtSource.Rows.Count - 1].Field<string>("text");
                rezult = regexVar.IsMatch(dtSource.Rows[dtSource.Rows.Count - 1].Field<string>("text"));
            }
            if (!rezult) return false;
            // dataGridView.Rows[j].Selected = true;
            dataGridView.CurrentCell = dataGridView[iVar, jVar];
           // selectedPointLabel.Text = tables.getNamePosit();
            selectedPointLabel.Text = tables.GetNamePositionFirstLevelTable();
            jVar++;
            return true;
        }
        private bool searchInTextBoxCell(int iVar, ref int jVar, Regex regexVar)
        {
            
            bool rezult;
            if (regexVar == null)
            {
                rezult = dataGridView[iVar, jVar].Value.ToString().
                    Equals(searchToolStripTextBox.Text);
            }
            else
            {
                rezult = regexVar.IsMatch(dataGridView[iVar, jVar].Value.ToString());
            }
            if (rezult)
            {
                // dataGridView.Rows[j].Selected = true;
                dataGridView.CurrentCell = dataGridView[iVar, jVar];
               // selectedPointLabel.Text = tables.getNamePosit();
               selectedPointLabel.Text = tables.GetNamePositionFirstLevelTable();
                jVar++;
                return true;
            }
            return false;
        }
        private void searchToolStripItem_Click(object sender, EventArgs e)
        {
            var tsmi = (ToolStripMenuItem) sender;
            tsmi.Checked = !tsmi.Checked;
            i = 0;
            j = 0;
        }
        private void generateNewChangesScript_Click(object sender, EventArgs e)
        {
            if (GenerateScript.ChangedRowCollection.Count==0) return;
            if (DialogResult.Cancel ==
                MessageBox.Show("Предыдущие строки для генерирования скрипта будут удалены. Продолжить?", "Новый скрипт",
                    MessageBoxButtons.OKCancel, MessageBoxIcon.Question))
                return;
            GenerateScript.ClearChangedRowCollection();
            if (dataGridView.Rows.Count != 0)
            {
                if (dataGridView.Columns.Contains("forscript"))
                {
                    for (int i = 0; i < dataGridView.Rows.Count; i++)
                    {
                        dataGridView.Rows[i].Cells["forscript"].Value = null;
                    }
                }
            }
        }
        private void watchGeneratedRowsButton_Click(object sender, EventArgs e)
        {
            if (GenerateScript.ChangedRowCollection.Count == 0)
            {
                MessageBox.Show("Нет ни одной строки для выгрузки");
                return;
            }
            WatchGeneratedRowsWindow win = new WatchGeneratedRowsWindow();
            win.ShowDialog();
        }
    }
}
