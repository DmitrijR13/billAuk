using System;
using System.Data;
using System.Windows.Forms;
using Npgsql;
using NpgsqlTypes;
using webroles.Properties;
using System.Collections.Generic;
using webroles.TransferData;

namespace webroles.TablesFirstLevel
{
    class PageGroupsTable : TableFirstLevel, ISubject,IDeletable
    {
       
        DataGridViewColumn[] columnCollection;
        private const string nodeTreeViewText = "Группы страниц";
        readonly List<IObserver> observers = new List<IObserver>();
        private readonly string selectCommand = "with selectcreate as (select group_id, group_name, created_on, created_by, changed_on, changed_by, user_name as user_name_created " +
            "from " + Tables.page_groups + " pg LEFT OUTER JOIN " + Tables.users + " u on (pg.created_by=u.id)) " + 
            "select  group_id, group_name, created_on, created_by, changed_on, changed_by, user_name_created, user_name as user_name_changed " +
            "from selectcreate LEFT OUTER JOIN " + Tables.users + " u on (selectcreate.changed_by=u.id) ORDER BY selectcreate.group_id;";

        public override string NodeTreeViewText
        {
            get
            {
                return nodeTreeViewText;
            }
        }

        public override string TableName
        {
            get
            {
                return Tables.page_groups;
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
            columnCollection = new DataGridViewColumn[8];
            columnCollection[7] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.page_groups, "user_name_changed", Resources.user_name_chang, true, true);
            columnCollection[6] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.page_groups, "changed_by", Resources.changed_by, true, false);
            columnCollection[5] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.page_groups, "changed_on", Resources.changed_on, true, true);
            columnCollection[4] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.page_groups, "user_name_created", Resources.user_name_creat, true, true);
            columnCollection[3] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.page_groups, "created_by", Resources.created_by, true, false);
            columnCollection[2] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.page_groups, "created_on", Resources.created_on, true, true);
            columnCollection[1] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.page_groups, "group_name", Resources.page_groupsTable_group_name, false, true);
            columnCollection[0] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.page_groups, "group_id", Resources.page_groupsTable_group_id, false, true);
            columnCollection[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
        }

        public override bool Save(NpgsqlConnection connect, NpgsqlDataAdapter adapter, DataTable dt)
        {
            var rezult = true;
            adapter.DeleteCommand = new NpgsqlCommand("Delete from page_groups Where group_id=@group_id", connect);
            NpgsqlParameter paramter = adapter.DeleteCommand.Parameters.Add("@group_id", NpgsqlDbType.Integer);
            paramter.SourceColumn = "group_id";
            paramter.SourceVersion = DataRowVersion.Original;

            adapter.UpdateCommand = new NpgsqlCommand("Update page_groups set  group_name=@group_name, created_on=@created_on, created_by=@created_by, changed_on=@changed_on, changed_by=@changed_by where group_id=@group_id", connect);
            paramter = adapter.UpdateCommand.Parameters.Add("@group_id", NpgsqlDbType.Integer);
            paramter.SourceColumn = "group_id";
            adapter.UpdateCommand.Parameters.Add("@group_name", NpgsqlDbType.Char, 255, "group_name");
            paramter = adapter.UpdateCommand.Parameters.Add("@created_on", NpgsqlDbType.Timestamp);
            paramter.SourceColumn = "created_on";
            paramter = adapter.UpdateCommand.Parameters.Add("@created_by", NpgsqlDbType.Integer);
            paramter.SourceColumn = "created_by";
            paramter = adapter.UpdateCommand.Parameters.Add("@changed_on", NpgsqlDbType.Timestamp);
            paramter.SourceColumn = "changed_on";
            paramter = adapter.UpdateCommand.Parameters.Add("@changed_by", NpgsqlDbType.Integer);
            paramter.SourceColumn = "changed_by";

            adapter.InsertCommand = new NpgsqlCommand("insert into page_groups (group_id, group_name, created_on, created_by, changed_on, changed_by) " +
            "VALUES (@group_id, @group_name, @created_on, @created_by, @changed_on, @changed_by); ", connect);
            paramter = adapter.InsertCommand.Parameters.Add("@group_id", NpgsqlDbType.Integer);
            paramter.SourceColumn = "group_id";
            adapter.InsertCommand.Parameters.Add("@group_name", NpgsqlDbType.Char, 255, "group_name");
            paramter = adapter.InsertCommand.Parameters.Add("@created_on", NpgsqlDbType.Timestamp);
            paramter.SourceColumn = "created_on";
            paramter = adapter.InsertCommand.Parameters.Add("@created_by", NpgsqlDbType.Integer);
            paramter.SourceColumn = "created_by";
            paramter = adapter.InsertCommand.Parameters.Add("@changed_on", NpgsqlDbType.Timestamp);
            paramter.SourceColumn = "changed_on";
            paramter = adapter.InsertCommand.Parameters.Add("@changed_by", NpgsqlDbType.Integer);
            paramter.SourceColumn = "changed_by";

            if (!TransferData.TransferDataDb.UpdateDbTable(adapter, dt)) return false;
            NotifyObservers();
            return true;
        }
        public override void SetDefaultValuesAfterRowAdded(DataGridView dgv, int index)
        {
            dgv.Rows[index].Cells["created_on"].Value = DateTime.Now;
            dgv.Rows[index].Cells["created_by"].Value = MainWindow.UserId;
            dgv.Rows[index].Cells["user_name_created"].Value = MainWindow.UserName;
            dgv.Rows[index].Cells["changed_by"].Value = DBNull.Value;
        }

        public void AddObservers(List<IObserver> obsrvs)
        {
            observers.AddRange(obsrvs);
        }

        public void NotifyObservers()
        {
            var dt = ComboBoxBindingSource.Update("SELECT group_id as id, group_id ||' '|| group_name  as text from page_groups ORDER BY id;");
            for (int i = 0; i < observers.Count; i++)
            {
                if (!observers[i].AdditionalContition)
                    observers[i].update(dt);
                else
                    observers[i].update(ComboBoxBindingSource.Update(observers[i].AdditionalContotionString));
            }
        }

        public override void SetDefaultValuesAfterCellChange(DataGridView dgv, int row, int column)
        {
            dgv.Rows[row].Cells["user_name_changed"].Value = MainWindow.UserName;
            dgv.Rows[row].Cells["changed_on"].Value = DateTime.Now;
            dgv.Rows[row].Cells["changed_by"].Value = MainWindow.UserId;
        }

        public bool CheckToAllowDelete(DataGridView dvg)
        {
            if (dvg.CurrentRow == null) return true;
            var posit = dvg.CurrentRow.Cells["group_id"].Value;
            if (posit == DBNull.Value) return true;
            string[] tablesArr =
            {
                Tables.pages, Tables.page_links
            };
            Returns ret = new Returns(true, "");
            string commandText = "SELECT EXISTS (Select nzp_page from " + Tables.pages + " where group_id=" + posit + ");";
            commandText += "SELECT EXISTS (select id from " + Tables.page_links + " Where group_from=" + posit + " OR group_to="+posit+");";
            return DBManager.CheckExistsRefernces(commandText, tablesArr, (int)posit);
        }

    }
}
