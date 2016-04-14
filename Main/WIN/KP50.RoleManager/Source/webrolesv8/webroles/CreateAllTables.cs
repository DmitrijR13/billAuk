using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Npgsql;
using webroles.GenerateScriptTable;
using webroles.TablesFirstLevel;
using webroles.TablesSecondLevel;
using webroles.TransferData;

namespace webroles
{
    class CreateAllTables
    {
        BackgroundWorker bw;
        private readonly StatusStrip statusStrip;
     //   private DataSet dataSet;
        private readonly NpgsqlConnection connect;
        private NpgsqlDataAdapter adapter;
        private readonly TreeView treeView;
        private readonly BindingSource dataGridViewBinSour;
        private readonly ActionShowTable actionShowTable;
        private readonly PageGroupsTable pageGroupsTable;
        private readonly PageLinksTable pageLinksTable;
        private readonly PagesTable pagesTable;
        private readonly ImgLnkPagesTable imgLnkPagesTable;
        private readonly ProfilesTable profiles;
        private readonly SactionTable sactionTable;
        private readonly SrolesTable srolesTable;
        private readonly ReportTable reportTable;
        private readonly ActionsLnkTable actionsLnkTable;
        private readonly RoleActionsTable roleActions;
        private readonly TroleMergingTable troleMerging;
        private readonly ImgLnkSroleTable imgLnkSroles;
        private readonly ProfileRolesTable profileRoles;
        private readonly ImgLnkSactionsTable imgLnkSactions;
        private readonly DataGridView dataGridView;
        private readonly RolePagesTable rolePages;
        private readonly UseInRolePages useInRolePages;
        private readonly UseInRoleActions useInRoleActions;
        private readonly List<TableFirstLevel> firstLevelTables = new List<TableFirstLevel>();
        private readonly List<TableSecondLevel> secondLevelTables = new List<TableSecondLevel>();
        private readonly List<TableFirstLevel> allTablesList = new List<TableFirstLevel>();
        private readonly List<ISubject> subjects = new List<ISubject>();
        private readonly List<IDataSourceIndividComboBox> individualDataComboBox = new List<IDataSourceIndividComboBox>();
        private readonly Label selectedPointLabel;
        private TableFirstLevel selectedFirstLeveTable;
        private TableSecondLevel selectedSecondLeveTable;
        private string selTabName;
        private int profileIdNum;
        private string prevNameParentTable;
        private readonly List<INamePosition> namePositionFirstLevelTAblesList = new List<INamePosition>();
        public CreateAllTables(TreeView treeView, DataGridView dataGridView, BindingSource dataGridViewBinSour, Label selectedPointLabel,  StatusStrip statusStrip)
        {
            this.selectedPointLabel = selectedPointLabel;
            this.treeView = treeView;
            this.dataGridView = dataGridView;
            this.dataGridViewBinSour = dataGridViewBinSour;
            
            this.statusStrip = statusStrip;
            connect = ConnectionToPostgreSqlDb.GetConnection();
            // Коллекция страниц первого уровня
            // Таблица pages
            pagesTable = new PagesTable();
            firstLevelTables.Add(pagesTable);
            subjects.Add(pagesTable);
            namePositionFirstLevelTAblesList.Add(pagesTable);
            // Таблица s_actions
            sactionTable = new SactionTable();
            firstLevelTables.Add(sactionTable);
            subjects.Add(sactionTable);
            namePositionFirstLevelTAblesList.Add(sactionTable);
            // Таблица s_roles
            srolesTable = new SrolesTable();
            firstLevelTables.Add(srolesTable);
            subjects.Add(srolesTable);
            namePositionFirstLevelTAblesList.Add(srolesTable);
            // Таблица profiles
            profiles = new ProfilesTable();
            firstLevelTables.Add(profiles);
            namePositionFirstLevelTAblesList.Add(profiles);
            // Таблица page_groups
            pageGroupsTable= new PageGroupsTable();
            firstLevelTables.Add(pageGroupsTable);
            subjects.Add(pageGroupsTable);
            // Таблица page_links
            pageLinksTable= new PageLinksTable();
            firstLevelTables.Add(pageLinksTable);
           // Таблица users
            UsersTable usersTable = new UsersTable();
            firstLevelTables.Add(usersTable);
            // Таблица report
            reportTable= new ReportTable();
            firstLevelTables.Add(reportTable);

            // Коллекция страниц второго уровня
            // Таблица actions_show
            actionShowTable = new ActionShowTable(pagesTable.TableName);
            secondLevelTables.Add(actionShowTable);
            subjects.Add(actionShowTable);
            // Таблица actions_lnk
            actionsLnkTable = new ActionsLnkTable(pagesTable.TableName);
            secondLevelTables.Add(actionsLnkTable);
            // Таблица img_lnk
            imgLnkPagesTable = new ImgLnkPagesTable(pagesTable.TableName);
            secondLevelTables.Add(imgLnkPagesTable);
            // Таблица role_actions
            roleActions = new RoleActionsTable(srolesTable.TableName);
            secondLevelTables.Add(roleActions);
            individualDataComboBox.Add(roleActions);
            // Таблица role_pages
            rolePages = new RolePagesTable(srolesTable.TableName);
            secondLevelTables.Add(rolePages);
            // Таблица t_role_merging
            troleMerging = new TroleMergingTable(srolesTable.TableName);
            secondLevelTables.Add(troleMerging);
            // Таблица img_lnk
            imgLnkSroles = new ImgLnkSroleTable(srolesTable.TableName);
            secondLevelTables.Add(imgLnkSroles);
            // Таблица roleskey
            RolesKeyTable rolesKey = new RolesKeyTable(srolesTable.TableName);
            secondLevelTables.Add(rolesKey);
            // Таблица profile_roles
            profileRoles = new ProfileRolesTable(profiles.TableName);
            secondLevelTables.Add(profileRoles);
            // Таблица img_lnk
            imgLnkSactions = new ImgLnkSactionsTable(sactionTable.TableName);
            secondLevelTables.Add(imgLnkSactions);
            useInRolePages = new UseInRolePages(pagesTable.TableName);
            secondLevelTables.Add(useInRolePages);
            useInRoleActions= new UseInRoleActions(pagesTable.TableName);
            secondLevelTables.Add(useInRoleActions);

            allTablesList.AddRange(firstLevelTables);
            allTablesList.AddRange(secondLevelTables);
        }

        public bool FillDataSet()
        {
            adapter = new NpgsqlDataAdapter("",connect);
            for (int i = 0; i < firstLevelTables.Count; i++)
            {
                firstLevelTables[i].CreateColumns();
                treeView.Nodes.Add(firstLevelTables[i].TableName, firstLevelTables[i].NodeTreeViewText);
            }
            for (int i = 0; i < secondLevelTables.Count; i++)
            {
                secondLevelTables[i].CreateColumns();
                treeView.Nodes[secondLevelTables[i].NameParentTable].Nodes.Add(secondLevelTables[i].TableName, secondLevelTables[i].NodeTreeViewText);
            }
            prevNameParentTable = "pages";
            setObservers();
            treeView.SelectedNode = treeView.Nodes[0];
            foreach (ISubject subject in subjects)
                subject.NotifyObservers();
            return true;
        }

        /// <summary>
        /// Добавляет наблюдателей в соответствующие таблицы (паттерн Наблюдатель)
        /// </summary>
        private void setObservers()
        {
            // Наблюдатели таблицы pages
            pagesTable.AddObservers(pageLinksTable.ObserversPagesTable);
            pagesTable.AddObservers(imgLnkPagesTable.ObserversPagesTable);
            pagesTable.AddObservers(imgLnkSactions.ObserversPagesTable);
            pagesTable.AddObservers(imgLnkSroles.ObserversPagesTable);
           // pagesTable.AddObservers(roleActions.ObserversPagesTable);
            pagesTable.AddObservers(rolePages.ObserversPagesTable);
            pagesTable.AddObservers(srolesTable.ObserversPagesTable);
            // Наблюдатели таблицы page_groups
            pageGroupsTable.AddObservers(pagesTable.ObserversPageGroupsTable);
            pageGroupsTable.AddObservers(pageLinksTable.ObserversPageGroupsTable);
            // Наблюдатели таблицы s_action
            sactionTable.AddObservers(actionShowTable.ObserversSactionsTable);
            sactionTable.AddObservers(actionsLnkTable.ObserversSactionsTable);
            sactionTable.AddObservers(reportTable.ObserversSactionsTable);
            // Наблюдатели таблицы s_roles
            srolesTable.AddObservers(profileRoles.ObserversSrolesTable);
            srolesTable.AddObservers(troleMerging.ObserversSrolesTable);
            // Наблюдатели таблицы actions_show
            actionShowTable.AddObservers(roleActions.ObserversPagesTable);
            actionShowTable.AddObservers(actionsLnkTable.ObserversPagesTable);
        }

        public bool RequarimentSaveChanges(int columnIndex, string text)
        {
            var tables = allTablesList.Select(table => table).Where(table => treeView.SelectedNode.Name.Equals(table.TableName));
            if (columnIndex == tables.First().GetTableColumns.Count() - 1)
            {
                if (text.Last().Equals('*'))
                {
                    MessageBox.Show("Сохраните все изменения.", "Сохраните все изменения", MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return true;
                }
            }
            return false;

        }

        private bool b;
        /// <summary>
        /// Вызывается при нажатии на любой узел treeView
        /// </summary>
        /// <param name="level">Уровень узла</param>
        /// <returns>Наименование узла</returns>
        /// 
        public string treeViewAfterSelect(int level)
        {
            switch (level)
            {
                case 0:
                    foreach (TableFirstLevel table in firstLevelTables)
                    {
                        if (!treeView.SelectedNode.Name.Equals(table.TableName)) continue;
                        dataGridViewBinSour.Position = 0;
                        selectedFirstLeveTable = table;
                        dataGridView.Columns.Clear();
                        dataGridViewBinSour.DataSource = table.GetDataSource(connect, adapter);
                        
                        dataGridView.Columns.AddRange(table.GetTableColumns);
                        GenerateScript.InsertPrevValuesForScript(table.TableName,dataGridView);
                       
                    
                       selectedPointLabel.Text = GetNamePositionFirstLevelTable();

                        return table.NodeTreeViewText;
                    }
                    break;
                case 1:
                    foreach (TableSecondLevel table in secondLevelTables)
                    {
                        if (!treeView.SelectedNode.Name.Equals(table.TableName)) continue;
                        selectedSecondLeveTable = table;
                        
                        selectedPointLabel.Text= GetNamePositionSecLevelTable();
                        
                        
                        dataGridViewBinSour.DataSource = table.GetDataSource(connect, adapter);// dataSet.Tables[table.TableName];
                       // table.GetCorrespondTable(dataSet,adapter, connect,table.Position);
                        dataGridView.Columns.Clear();
                        dataGridView.Columns.AddRange(table.GetTableColumns);
                        GenerateScript.InsertPrevValuesForScript(table.TableName, dataGridView, table.Position);
                        var t = table as IDataSourceIndividComboBox;
                        if (t != null)
                            t.SetDataSourceToIndividualComboBoxCell();
                        return table.NodeTreeViewText;
                    }
                    break;
            }
            return "";
        } 

        /// <summary>
        /// Вызывается при сохранении текущей таблицы
        /// </summary>
        public bool SaveCurrentTable()
        {
            dataGridView.EndEdit();
            // TablesConstraints.Clear(dataSet);
            foreach (TableFirstLevel table in allTablesList)
            {
                if (!treeView.SelectedNode.Text.Equals(table.NodeTreeViewText)) continue;
                  // ((PagesTable)table).TakeRows(dataSet);
                var datsourBinSour = dataGridView.DataSource as BindingSource;
                if (datsourBinSour == null)
                {
                    MessageBox.Show("Ошибка сохранения");
                    return false;
                }
                DataTable dt = datsourBinSour.DataSource as DataTable;
                 if (dt == null)
                {
                    MessageBox.Show("Ошибка сохранения");
                    return false;
                }
                if (!table.Save(connect, adapter, dt))
                {
                    //RefreshTable();
                  //  ((PagesTable)table).ClearIntermediateCollections();
                    GenerateScript.InsertPrevValuesForScript(table.TableName, dataGridView);
                    return false;
                }
                updateCurrentTable(table.SelectCommand,dt);
                //  ((PagesTable)table).AddToBaseCollection(dataSet);
                    var t = table as IDataSourceIndividComboBox;
                    if (t != null) t.SetDataSourceToIndividualComboBoxCell();
                var position = -1;
                var level = table as TableSecondLevel;
                string nameBaseCol = "";
                if (level != null)
                {
                    position = level.Position;
                    nameBaseCol = level.NameOwnBaseColumn;
                }
                GenerateScript.InsertPrevValuesForScript(table.TableName, dataGridView, position);
                break;
            }
            return true;
        }

        private void updateCurrentTable(string selectCommand, DataTable dt)
       {
           dt.Clear();
         //  DataTable dataTable = new DataTable(tableName);
           adapter.SelectCommand.CommandText = selectCommand;
         //  dataSet.Tables.Add(dataTable);
           adapter.Fill(dt);
           //dataGridViewBinSour.DataSource = dataSet.Tables[tableName];
           
       }

        public void RefreshTable()
        {
            foreach (TableFirstLevel table in allTablesList)
            {
                if (!treeView.SelectedNode.Text.Equals(table.NodeTreeViewText)) continue;
                var datsourBinSour = dataGridView.DataSource as BindingSource;
                if (datsourBinSour == null)
                {
                    MessageBox.Show("Ошибка сохранения");
                    return;
                }
                DataTable dt = datsourBinSour.DataSource as DataTable;
                if (dt == null)
                {
                    MessageBox.Show("Ошибка сохранения");
                    return;
                }
                updateCurrentTable(table.SelectCommand,dt);
                var t = table as IDataSourceIndividComboBox;
                if (t != null) t.SetDataSourceToIndividualComboBoxCell();
                break;
            }
        }
        /// <summary>
        /// Устанавливает значения по умолчанию, после после добавления строки.
        /// </summary>
        /// <param name="selectedNodeText">Наименование выбранного узла</param>
        /// <param name="index">Индекс добавленной строки</param>
        public void SetDefaultValsAfterRowAdded(string selectedNodeText, int index)
        {
            foreach (TableFirstLevel tableFirstLevel in allTablesList)
            {
                if(!tableFirstLevel.NodeTreeViewText.Equals(selectedNodeText)) continue;
                tableFirstLevel.SetDefaultValuesAfterRowAdded(dataGridView, index);       
            }
        }


        /// <summary>
        /// Устанавливает значения ячеек (обычно когда и кем изменен), после изменения какой-либо ячейки.
        /// </summary>
        /// <param name="selectedNodeText">Наименование выбранного узла</param>
        /// <param name="row">Индекс строки, в которой находится ячейка, в кторой меняется значение</param>
        /// <param name="column">Индекс колонки, в которой находится ячейка, в кторой меняется значение</param>

        public bool SetDefaultValsAfterCellChang(string selectedNodeText, int row, int column)
        {
            if (GenerateScript.NotRepeatCreateForScript)
            {
                GenerateScript.NotRepeatCreateForScript = false;
                return true;
            }
            foreach (TableFirstLevel tableFirstLevel in allTablesList)
            {
                if (!tableFirstLevel.NodeTreeViewText.Equals(selectedNodeText)) continue;
                if (column == tableFirstLevel.GetTableColumns.Count() - 1)
                {
                    tableFirstLevel.SetDefaultValuesAfterCellChange(dataGridView, row, column);
                    return true;
                }
                tableFirstLevel.SetDefaultValuesAfterCellChange(dataGridView, row, column);
                break;
            }
            return false;
        }
        public string GetNamePositionFirstLevelTable()
        {
            //// Если этот метод вызвался в процессе изменения строкив DataGridView в таблицах второго уровня, выйти
            if (secondLevelTables.Where(table => table.TableName.Equals(treeView.SelectedNode.Name)).Select(tab => tab).Count() != 0) return selectedPointLabel.Text; 
            // Если Таблица первого уровня НЕ реализует интерфейс INamePosition, выйти
            if (!(selectedFirstLeveTable is INamePosition))
            {
                prevNameParentTable = treeView.SelectedNode.Name;
                selectedPointLabel.Visible = false;
                return string.Empty;
            }
            if (selectedSecondLeveTable != null)
            {
                if (selectedFirstLeveTable.TableName.Equals(selectedSecondLeveTable.NameParentTable))
                {
                    dataGridViewBinSour.Position = ((INamePosition)selectedFirstLeveTable).Position;
                    selectedSecondLeveTable = null;
                    selectedPointLabel.Visible = true;
                    return selectedPointLabel.Text;
                }
                selectedSecondLeveTable = null;
            }
            selectedPointLabel.Visible = true;
           // если количество строк в выбранной таблице оказалось ==0, выйти
          // if (dataSet.Tables[selectedFirstLeveTable.TableName].Rows.Count == 0) return string.Empty;
           // Код будет выполняться только если выбранный узел в treeView относится к таблицам первого уровня и реализуют интерфейс INamePosition и 
            ((INamePosition) selectedFirstLeveTable).Position = dataGridViewBinSour.Position;
           
            foreach (TableSecondLevel table in secondLevelTables.Where(table => table.NameParentTable.Equals(treeView.SelectedNode.Name)))
           {
               // Передаем таблицам второго уровня номер, соответствующий позиции
               //table.Position = dataSet.Tables[selectedFirstLeveTable.TableName].Rows[dataGridViewBinSour.Position].Field<int>(table.NameBaseColumn);
               var ds = dataGridView.Rows[dataGridViewBinSour.Position].Cells[table.NameParentColumn].Value;
               if ((ds as int?) == null) return selectedPointLabel.Text; 
               profileIdNum = table.Position =(Int32)ds;
                   //(int)dataGridView.Rows[dataGridViewBinSour.Position].Cells[table.NameBaseColumn].Value;
           }
            prevNameParentTable = treeView.SelectedNode.Name;
           // возвращаем строку, которая будет отображаться в positionLabel
          // return ((INamePosition)selectedFirstLeveTable).GetNamePosition(dataSet.Tables[selectedFirstLeveTable.TableName].Rows[dataGridViewBinSour.Position]);
           return ((INamePosition)selectedFirstLeveTable).GetNamePosition(dataGridView.Rows[dataGridViewBinSour.Position]);
        }


        public string GetNamePositionSecLevelTable()
        {
            // Если мы переключаемся между узлами treeView одного и того же родительского узла, выйти
            if (selectedSecondLeveTable.NameParentTable.Equals(prevNameParentTable)) return selectedPointLabel.Text;
            prevNameParentTable = selectedSecondLeveTable.NameParentTable;
            // Если у родительской таблицы нет строк, выйти
          //  if (dataSet.Tables[selectedSecondLeveTable.NameParentTable].Rows.Count == 0) return selectedSecondLeveTable.NameParentTable + ": " + "у данной таблицы нет строк";
            // Находим соответствующую родительскую таблицу первого уровня
            IEnumerable <TableFirstLevel> parentTable = from firstLevTab in firstLevelTables
                                                          where firstLevTab.TableName.Equals(selectedSecondLeveTable.NameParentTable)
                                                          select firstLevTab;
            string labeltext =((INamePosition)parentTable.First()).GetNamePosition(null);
            foreach (TableSecondLevel table in secondLevelTables.Where(table => table.NameParentTable.Equals(parentTable.First().TableName)))
            {
                  //  ((INamePosition)parentTable.First()).Position = row.Field<int>(table.NameParentColumn);
                    table.Position = ((INamePosition)parentTable.First()).PositionDefault;
                //table.Position =(int) dataGridView.Rows[0].Cells[table.NameBaseColumn].Value;
            }
            selectedPointLabel.Visible = true;
            // смело приводим к интерфейсу, т.к. если выполнение дошло до этого места, значит родительская таблица реализует интерфейс INamePosition
           // namePrevNode = selectedSecondLeveTable.NameParentTable;
           // return ((INamePosition)parentTable.First()).GetNamePosition(dataSet.Tables[parentTable.First().TableName].Rows[0]);
            return labeltext;
        }

      

        /// <summary>
        /// Вызывается при нажатии на головную ячейку DataGridView (при нажатии, DataSource отдельной ячейки отвязывается!) только для классов, реализующих интерфейс IDataSourceIndividComboBox.Метод нужен для того, чтобы перепривязать данные, инче выйдет сообщение о неккоретных данных.
        /// </summary>
        public void SetIndividualDataSourceToComboBoxCell()
        {
            foreach (var item in individualDataComboBox)
            {
                if (!((TableFirstLevel)item).TableName.Equals(treeView.SelectedNode.Name)) continue;

                //dataGridViewBinSour.DataSource = dataSet.Tables[((TableFirstLevel)item).TableName];
                item.SetDataSourceToIndividualComboBoxCell();
            }
        }

        public void SortBeforeInsert()
        {
            foreach (var item in individualDataComboBox)
            {
                if (!((TableFirstLevel)item).TableName.Equals(treeView.SelectedNode.Name)) continue;
                item.Sort();
                break;

            }
        }

        private string s = "";
        // Метод выгрузки скриптов
        public void UploadSqlScriptForCustomer(TypeScript typeScr)
        {
            // Для полного скрипта  для разработчиков или полного скрипта для заказчика
            if (typeScr == TypeScript.FullCustomer || typeScr == TypeScript.FullDeveloper)
            {
                // проверяем выбранный профиль
                if (!treeView.SelectedNode.Name.Equals(profiles.TableName) &
                    !treeView.SelectedNode.Name.Equals(profileRoles.TableName))
                {
                    MessageBox.Show("Выберите профиль, для которого нужно выгрузить sql скрипт", "Выберите профиль",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            // Для остальных типов скриптов проверяем количетсво строк для выгрузки
            if (typeScr == TypeScript.ChangeCustomer)
            {
                if (GenerateScript.ChangedRowCollection.Count == 0)
                {
                    MessageBox.Show("Отметьте строки, для которых нужно выгрузить скрипт.", "Нет строк для выгрузки",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
            }
            GenerateScript.TypeScr = typeScr;
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.FileName = "update";
            saveFileDialog.Filter = "|*.sql";
            if (saveFileDialog.ShowDialog() != DialogResult.OK) return;
            GenerateScript.ClearLineList();
            s = saveFileDialog.FileName;
            try
            {
                var fs = new FileStream(s, FileMode.Create);
                fs.Dispose();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }
            GenerateScript.FileName = s;
            var ss = (ToolStripProgressBar)statusStrip.Items["toolStripProgressBar"];
            ss.Maximum = 100;
            ss.Minimum = 0;
            ss.Value = 0;
            switch (typeScr)
            {
                case TypeScript.ChangeCustomer:
                       bw = new BackgroundWorker {WorkerReportsProgress = true};
            bw.DoWork += bw_DoWorkChange;
            bw.ProgressChanged += new ProgressChangedEventHandler(bw_ProgressChanged);
            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bw_RunWorkerCompleted);
            bw.RunWorkerAsync();
                    break;
                case TypeScript.FullCustomer:
                case TypeScript.FullDeveloper:
            bw = new BackgroundWorker {WorkerReportsProgress = true};
            bw.DoWork += bw_DoWorkFull;
            bw.ProgressChanged += new ProgressChangedEventHandler(bw_ProgressChanged);
            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bw_RunWorkerCompleted);
            bw.RunWorkerAsync();
                    break;
            }
            //ThreadPool.QueueUserWorkItem(Append, saveFileDialog.FileName);
        }

        void bw_DoWorkChange(object sender, DoWorkEventArgs e)
        {
            var first = new FirstGenScript();
            first.GenerateScr();
            var pages = new PagesTableGenScript();
            pages.GenerateScr();
            var s_actions = new SactionsTableGenScript();
            s_actions.GenerateScr();
            var actionsShow = new ActionsShowTableGenScript();
            actionsShow.GenerateScr();
            var actionsLnk = new ActionsLnkTableGenScript();
            actionsLnk.GenerateScr();
            var pageLinks = new PageLinksTableGenScript();
            pageLinks.GenerateScr();
            var pagesshow = new PagesShowTableGenScript();
             pagesshow.GenerateScr();
            var img_lnkPages = new ImgLnkPagesTableGenScript();
            img_lnkPages.GenerateScr();
            var s_Roles = new SrolesTableGenScript();
            s_Roles.GenerateScr();
            var roleActions = new RoleActionsTableGenScript();
            roleActions.GenerateScr();
            var rolePages = new RolePagesTableGenScript();
            rolePages.GenerateScr();
            var rolesKey = new RoleskeyTableGenScript();
            rolesKey.GenerateScr();
            var report = new ReportTableGenScript();
            report.GenerateScr();
            var last = new LastGenScript();
            last.GenerateScr();
          //  var img_LnkActions = new ImgLnkSactionsTableGenScript();
            //img_LnkActions.GenerateScr();
            //var img_LnkRoles = new ImgLnkSrolesTableGenScript();
            //img_LnkRoles.GenerateScr();
            for (int i = 0; i < GenerateScript.LinesList.Count; i++)
            {
                try
                {
                    if (i == 0)
                        File.WriteAllLines(s, GenerateScript.LinesList[i]);
                    else
                        File.AppendAllLines(s, GenerateScript.LinesList[i]);
                }
                catch (IOException ex)
                {
                    MessageBox.Show(ex.Message, "Неудачно", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                ((BackgroundWorker)sender).ReportProgress(i + 1);
                //Thread.Sleep(30);
            }
            MessageBox.Show("Выгрузка скрипта прошла успешно", "Успешно", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        
        void bw_DoWorkFull(object sender, DoWorkEventArgs e)
        {
            var s_roles = new SrolesTableGenScript();
            s_roles.ProfileIdValue = profileIdNum;
            s_roles.Request();
            var role_actions = new RoleActionsTableGenScript();
            role_actions.Request();
            var roleskey = new RoleskeyTableGenScript();
            roleskey.Request();
            var role_pages = new RolePagesTableGenScript();
            role_pages.Request();
            var s_actions = new SactionsTableGenScript();
            s_actions.Request();
            var pages = new PagesTableGenScript();
            pages.Request();
            var page_links = new PageLinksTableGenScript();
            page_links.Request();
            var actions_lnk = new ActionsLnkTableGenScript();
            actions_lnk.Request();
            var actions_show = new ActionsShowTableGenScript();
            actions_show.Request();
            var img_lnk_pages = new ImgLnkPagesTableGenScript();
            img_lnk_pages.Request();
            var img_lnk_s_actions = new ImgLnkSactionsTableGenScript();
            img_lnk_s_actions.Request();
            var img_lnk_s_roles = new ImgLnkSrolesTableGenScript();
            img_lnk_s_roles.Request();
            var report = new ReportTableGenScript();
            report.Request();
            var first = new FirstGenScript();
            var last = new LastGenScript();
            var pages_show = new PagesShowTableGenScript();
            pages_show.Request();
            first.GenerateScr();
            pages.GenerateScr();
            img_lnk_pages.GenerateScr();
            s_actions.GenerateScr();
            img_lnk_s_actions.GenerateScr();
            report.GenerateScr();
            actions_show.GenerateScr();
            actions_lnk.GenerateScr();
            page_links.GenerateScr();
            pages_show.GenerateScr();
            s_roles.GenerateScr();
            img_lnk_s_roles.GenerateScr();
            roleskey.GenerateScr();
            role_pages.GenerateScr();
            role_actions.GenerateScr();
            last.GenerateScr();
            for (int i = 0; i < GenerateScript.LinesList.Count; i++)
            {
               try
                {
                   if (i==0)
                       File.WriteAllLines(s, GenerateScript.LinesList[i]);
                   else
                    File.AppendAllLines(s, GenerateScript.LinesList[i]);
                }
                catch (IOException ex)
                {
                    MessageBox.Show(ex.Message, "Неудачно", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                  ((BackgroundWorker)sender).ReportProgress(i+1);
                  //Thread.Sleep(30);
            }
          MessageBox.Show("Выгрузка скрипта прошла успешно", "Успешно", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        void bw_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            var ss = (ToolStripProgressBar) statusStrip.Items["toolStripProgressBar"];
            ss.Value = e.ProgressPercentage * 100 / GenerateScript.LinesList.Count;
        }

        void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            var ss = (ToolStripProgressBar)statusStrip.Items["toolStripProgressBar"];
            ss.Value = 0 ;
            bw.Dispose();
        }

        public bool CheckToAllowEdit(EditOperations operation)
        {
          var curTable= allTablesList.First(table => treeView.SelectedNode.Name.Equals(table.TableName));
            if (!(curTable is IEditable)) return true;
            if (!((IEditable) curTable).AllowEdit(operation))
                return false;
            return true;
        }

        public bool CheckToAllowDelete()
        {
            var curTable = allTablesList.First(table => treeView.SelectedNode.Name.Equals(table.TableName));
            if (!(curTable is IDeletable)) return true;
            if (!((IDeletable)curTable).CheckToAllowDelete(dataGridView))
                return false;
            return true;
        }
    }


}
