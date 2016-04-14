using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using Npgsql;
using webroles.GenerateScriptTable;
using webroles.Properties;

namespace webroles.TablesFirstLevel
{
    class PageLinksTable:TableFirstLevel
    {
        DataGridViewColumn[] columnCollection;

        private const string nodeTreeViewText = "Переходы м/у страницами";

        private readonly string selectCommand = "WITH selectcreate as (SELECT pl.id, page_from, group_from, page_to, group_to, created_on, created_by, changed_on, changed_by, u.user_name as user_name_created " +
            "FROM " + Tables.page_links + " pl LEFT OUTER JOIN " + Tables.users + " u ON (pl.created_by= u.id)) " + 
            "SELECT selectcreate.id, page_from, group_from, page_to, group_to, created_on, created_by, changed_on, changed_by, user_name_created, u.user_name as user_name_changed " +
            "FROM selectcreate LEFT OUTER JOIN " + Tables.users + " u ON (changed_by= u.id) ORDER BY selectcreate.id;";
        readonly List<IObserver> pageFromTo = new List<IObserver>();
        readonly List<IObserver> groupFromTo = new List<IObserver>();
       public List<IObserver> ObserversPageGroupsTable
       {
           get { return groupFromTo; }
       }
       public List<IObserver> ObserversPagesTable
       {
           get { return pageFromTo; }
       }
        public  override string NodeTreeViewText
        {
            get
            {
                return nodeTreeViewText;
            }
        }

        public  override string TableName
        {
            get
            {
                return Tables.page_links;
            }
        }
        public override string SelectCommand
        {
            get
            {
                return selectCommand;
            }
        }
        public override DataGridViewColumn[] GetTableColumns
        {
            get
            {
                return columnCollection;
            }
            set
            {
                columnCollection = value;
            }
        }

        public override void CreateColumns()
        {
           columnCollection = new DataGridViewColumn[12];
           columnCollection[11] = CreateDataGridViewColumn.CreateComboBoxColumn("Для скрипта");
           columnCollection[10] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.page_links, "user_name_changed", Resources.user_name_chang, true, true);
           columnCollection[9] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.page_links, "changed_by", Resources.changed_by, true, false);
           columnCollection[8] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.page_links, "changed_on", Resources.changed_on, true, true);
           columnCollection[7] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.page_links, "user_name_created", Resources.user_name_creat, true, true);
           columnCollection[6] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.page_links, "created_by", Resources.created_by, true, false);
           columnCollection[5] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.page_links, "created_on", Resources.created_on, true, true);
           columnCollection[4] = CreateDataGridViewColumn.CreateComboBoxColumn<string>(Resources.page_linksTable_group_to, "group_to");
           groupFromTo.Add(new ComboBoxBindingSource(columnCollection[4],true));
           columnCollection[3] = CreateDataGridViewColumn.CreateComboBoxColumn<string>(Resources.page_linksTable_page_to, "page_to");
           pageFromTo.Add(new ComboBoxBindingSource(columnCollection[3], true));
           columnCollection[2] = CreateDataGridViewColumn.CreateComboBoxColumn<string>(Resources.page_linksTable_group_from,  "group_from");
           groupFromTo.Add(new ComboBoxBindingSource( columnCollection[2], true));
           columnCollection[1] = CreateDataGridViewColumn.CreateComboBoxColumn<string>(Resources.page_linksTable_page_from, "page_from");
           pageFromTo.Add(new ComboBoxBindingSource( columnCollection[1], true));
           columnCollection[0] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.page_links, "id", Resources.page_groupsTable_group_id, true, true);
           columnCollection[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
        }

        public override bool Save(NpgsqlConnection connect, NpgsqlDataAdapter adapter, DataTable dt)
        {
            adapter.DeleteCommand = new NpgsqlCommand("Delete from " + Tables.page_links + " Where id=@id", connect);
            NpgsqlParameter paramter = adapter.DeleteCommand.Parameters.Add("@id", NpgsqlTypes.NpgsqlDbType.Integer);
            paramter.SourceColumn = "id";
            paramter.SourceVersion = DataRowVersion.Original;

            adapter.UpdateCommand = new NpgsqlCommand("Update " + Tables.page_links + " set  page_from=@page_from, group_from=@group_from, page_to=@page_to, group_to=@group_to, created_by=@created_by, changed_on=@changed_on, changed_by=@changed_by where id=@id", connect);
            paramter = adapter.UpdateCommand.Parameters.Add("@id", NpgsqlTypes.NpgsqlDbType.Integer);
            paramter.SourceColumn = "id";
            paramter = adapter.UpdateCommand.Parameters.Add("@page_from", NpgsqlTypes.NpgsqlDbType.Integer);
            paramter.SourceColumn = "page_from";
            paramter = adapter.UpdateCommand.Parameters.Add("@group_from", NpgsqlTypes.NpgsqlDbType.Integer);
            paramter.SourceColumn = "group_from";
            paramter = adapter.UpdateCommand.Parameters.Add("@page_to", NpgsqlTypes.NpgsqlDbType.Integer);
            paramter.SourceColumn = "page_to";
            paramter = adapter.UpdateCommand.Parameters.Add("@group_to", NpgsqlTypes.NpgsqlDbType.Integer);
            paramter.SourceColumn = "group_to";
            paramter = adapter.UpdateCommand.Parameters.Add("@created_on", NpgsqlTypes.NpgsqlDbType.Timestamp);
            paramter.SourceColumn = "created_on";
            paramter = adapter.UpdateCommand.Parameters.Add("@created_by", NpgsqlTypes.NpgsqlDbType.Integer);
            paramter.SourceColumn = "created_by";
            paramter = adapter.UpdateCommand.Parameters.Add("@changed_on", NpgsqlTypes.NpgsqlDbType.Timestamp);
            paramter.SourceColumn = "changed_on";
            paramter = adapter.UpdateCommand.Parameters.Add("@changed_by", NpgsqlTypes.NpgsqlDbType.Integer);
            paramter.SourceColumn = "changed_by";
  
            adapter.InsertCommand = new NpgsqlCommand("insert into " + Tables.page_links + " (page_from, group_from, page_to, group_to, created_on, created_by, changed_on, changed_by) " +
            "VALUES (@page_from, @group_from, @page_to, @group_to, @created_on, @created_by, @changed_on, @changed_by); ", connect);
            paramter = adapter.InsertCommand.Parameters.Add("@page_from", NpgsqlTypes.NpgsqlDbType.Integer);
            paramter.SourceColumn = "page_from";
            paramter = adapter.InsertCommand.Parameters.Add("@group_from", NpgsqlTypes.NpgsqlDbType.Integer);
            paramter.SourceColumn = "group_from";
            paramter = adapter.InsertCommand.Parameters.Add("@page_to", NpgsqlTypes.NpgsqlDbType.Integer);
            paramter.SourceColumn = "page_to";
            paramter = adapter.InsertCommand.Parameters.Add("@group_to", NpgsqlTypes.NpgsqlDbType.Integer);
            paramter.SourceColumn = "group_to";
            adapter.InsertCommand.Parameters.Add("@group_name", NpgsqlTypes.NpgsqlDbType.Char, 255, "group_name");
            paramter = adapter.InsertCommand.Parameters.Add("@created_on", NpgsqlTypes.NpgsqlDbType.Timestamp);
            paramter.SourceColumn = "created_on";
            paramter = adapter.InsertCommand.Parameters.Add("@created_by", NpgsqlTypes.NpgsqlDbType.Integer);
            paramter.SourceColumn = "created_by";
            paramter = adapter.InsertCommand.Parameters.Add("@changed_on", NpgsqlTypes.NpgsqlDbType.Timestamp);
            paramter.SourceColumn = "changed_on";
            paramter = adapter.InsertCommand.Parameters.Add("@changed_by", NpgsqlTypes.NpgsqlDbType.Integer);
            paramter.SourceColumn = "changed_by";

            if (!TransferData.TransferDataDb.UpdateDbTable(adapter, dt)) return false;     
            return true;
        }

        public override void SetDefaultValuesAfterRowAdded(DataGridView dgv, int index)
        {
            dgv.Rows[index].Cells["page_to"].Value = DBNull.Value;
            dgv.Rows[index].Cells["group_to"].Value = DBNull.Value;
            dgv.Rows[index].Cells["page_from"].Value = DBNull.Value;
            dgv.Rows[index].Cells["group_from"].Value = DBNull.Value;
            dgv.Rows[index].Cells["created_on"].Value = DateTime.Now;
            dgv.Rows[index].Cells["created_by"].Value = MainWindow.UserId;
            dgv.Rows[index].Cells["user_name_created"].Value = MainWindow.UserName;
            dgv.Rows[index].Cells["changed_by"].Value = DBNull.Value;
        }

        public override void SetDefaultValuesAfterCellChange(DataGridView dgv, int row,int column)
        {
            var dataGridViewColumn = dgv.Columns["forscript"];
            if (dataGridViewColumn != null && dataGridViewColumn.Index == column)
            {
                List<string> valColumns = new List<string>();
                Dictionary<string, string> whereDictAndColsValuesDict = new Dictionary<string, string>();
                whereDictAndColsValuesDict.Add("page_from", GenerateScript.GetCellValue(dgv.Rows[row].Cells["page_from"]));
                whereDictAndColsValuesDict.Add("group_from", GenerateScript.GetCellValue(dgv.Rows[row].Cells["group_from"]));
                whereDictAndColsValuesDict.Add("page_to", GenerateScript.GetCellValue(dgv.Rows[row].Cells["page_to"]) );
                whereDictAndColsValuesDict.Add("group_to", GenerateScript.GetCellValue(dgv.Rows[row].Cells["group_to"]));
                Dictionary<string, int> idColVal = new Dictionary<string, int> 
                { { "id", (int)dgv.Rows[row].Cells["id"].Value } };

                ScriptGenerateOper typescr;
                if (dgv.Rows[row].Cells["forscript"].Value == null)
                {
                    typescr = ScriptGenerateOper.none;
                }
                else
                {
                    typescr = (ScriptGenerateOper)dgv.Rows[row].Cells["forscript"].Value;
                }

                switch (typescr)
                    {
                        case ScriptGenerateOper.Insert:
                            break;
                        case ScriptGenerateOper.Update:
                            MessageBox.Show("Строки этой таблицы обновлять нельзя!");
                            dgv.Rows[row].Cells["forscript"].Value = null;
                            //GenerateScript.AddVal(TableName, whereDictAndColsValuesDict, whereDictAndColsValuesDict, idColVal, typeScr);
                            return;
                        case ScriptGenerateOper.Delete:
                            break;
                    }
                GenerateScript.AddVal(TableName, whereDictAndColsValuesDict, whereDictAndColsValuesDict, idColVal, typescr);

                return;
            }
            dgv.Rows[row].Cells["user_name_changed"].Value = MainWindow.UserName;
            dgv.Rows[row].Cells["changed_on"].Value = DateTime.Now;
            dgv.Rows[row].Cells["changed_by"].Value = MainWindow.UserId;
        }
    }
    }

