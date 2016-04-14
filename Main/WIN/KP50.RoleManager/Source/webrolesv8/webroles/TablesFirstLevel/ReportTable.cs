using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using Npgsql;
using NpgsqlTypes;
using webroles.GenerateScriptTable;
using webroles.Properties;

namespace webroles.TablesFirstLevel
{
    class ReportTable : TableFirstLevel
    {
        private const string nodeTreeViewText = "Отчеты";
        private readonly string selectCommand =
            "WITH selectcreate AS (SELECT nzp_rep, nzp_act, name, file_name, created_on, created_by, changed_on, changed_by, u.user_name as user_name_created " +
            "FROM " + Tables.report + " r LEFT OUTER JOIN " + Tables.users + " u ON (r.created_by= u.id)) " +
            "SELECT nzp_rep, nzp_act, name, file_name, created_on, created_by, changed_on, changed_by, user_name_created, u.user_name as user_name_changed " +
            "FROM selectcreate LEFT OUTER JOIN " + Tables.users + " u ON (changed_by= u.id) ORDER BY selectcreate.nzp_rep; ";

        DataGridViewColumn[] columnCollection;
        readonly List<IObserver> nzpActList = new List<IObserver>();
        public List<IObserver> ObserversSactionsTable
        {
            get { return nzpActList; }
        }
        public override string NodeTreeViewText
        {
            get { return nodeTreeViewText; }
        }

        public override string TableName
        {
            get { return Tables.report; }
        }

        public override string SelectCommand
        {
            get { return selectCommand; }
        }

        public override DataGridViewColumn[] GetTableColumns
        {
            get { return columnCollection; }
            set { columnCollection = value; }
        }

        public override void CreateColumns()
        {
            columnCollection = new DataGridViewColumn[11];
            columnCollection[10] = CreateDataGridViewColumn.CreateComboBoxColumn("Для скрипта");
            columnCollection[9] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.report, "user_name_changed", Resources.user_name_chang, true, true);
            columnCollection[8] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.report, "changed_by", Resources.changed_by, true, false);
            columnCollection[7] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.report, "changed_on", Resources.changed_on, true, true);
            columnCollection[6] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.report, "user_name_created", Resources.user_name_creat, true, true);
            columnCollection[5] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.report, "created_by", Resources.created_by, true, false);
            columnCollection[4] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.report, "created_on", Resources.created_on, true, true);
            columnCollection[3] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.report, "file_name", "Наименование файла", false, true);
            columnCollection[2] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.report, "name", "Наименование", false, true);
            columnCollection[1] = CreateDataGridViewColumn.CreateComboBoxColumn<string>(Resources.actions_showTable_nzp_act, "nzp_act");
            nzpActList.Add(new ComboBoxBindingSource(columnCollection[1], true));
            columnCollection[0] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.report, "nzp_rep", Resources.actions_showTable_id, true, true);
            columnCollection[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
        }

        public override bool Save(NpgsqlConnection connect, NpgsqlDataAdapter adapter, DataTable dt)
        {
            adapter.DeleteCommand = new NpgsqlCommand("Delete from " + Tables.report + " Where nzp_rep=@nzp_rep", connect);
            NpgsqlParameter parameter = adapter.DeleteCommand.Parameters.Add("@nzp_rep", NpgsqlDbType.Integer);
            parameter.SourceColumn = "nzp_rep";
            parameter.SourceVersion = DataRowVersion.Original;

            adapter.UpdateCommand = new NpgsqlCommand("Update " + Tables.report + " SET  nzp_act=@nzp_act, file_name=@file_name, name=@name, " +
            "created_on=@created_on, created_by=@created_by, changed_on=@changed_on, changed_by=@changed_by where nzp_rep=@nzp_rep;", connect);
            parameter = adapter.UpdateCommand.Parameters.Add("@nzp_rep", NpgsqlDbType.Integer);
            parameter.SourceColumn = "nzp_rep";
            parameter = adapter.UpdateCommand.Parameters.Add("@nzp_act", NpgsqlDbType.Integer);
            parameter.SourceColumn = "nzp_act";
            adapter.UpdateCommand.Parameters.Add("@file_name", NpgsqlDbType.Char, 255, "file_name");
            adapter.UpdateCommand.Parameters.Add("@name", NpgsqlDbType.Char, 255, "name");
            parameter = adapter.UpdateCommand.Parameters.Add("@created_on", NpgsqlDbType.Timestamp);
            parameter.SourceColumn = "created_on";
            parameter = adapter.UpdateCommand.Parameters.Add("@created_by", NpgsqlDbType.Integer);
            parameter.SourceColumn = "created_by";
            parameter = adapter.UpdateCommand.Parameters.Add("@changed_on", NpgsqlDbType.Timestamp);
            parameter.SourceColumn = "changed_on";
            parameter = adapter.UpdateCommand.Parameters.Add("@changed_by", NpgsqlDbType.Integer);
            parameter.SourceColumn = "changed_by";

            adapter.InsertCommand = new NpgsqlCommand("Insert into " + Tables.report + " ( nzp_act, name, file_name, created_on, created_by, changed_on, changed_by ) " +
           "Values (@nzp_act, @name, @file_name, @created_on, @created_by, @changed_on, @changed_by); ", connect);
            parameter = adapter.InsertCommand.Parameters.Add("@nzp_act", NpgsqlDbType.Integer);
            parameter.SourceColumn = "nzp_act";
            adapter.InsertCommand.Parameters.Add("@name", NpgsqlDbType.Char, 255, "name");
            adapter.InsertCommand.Parameters.Add("@file_name", NpgsqlDbType.Char, 255, "file_name");
            parameter = adapter.InsertCommand.Parameters.Add("@created_on", NpgsqlDbType.Timestamp);
            parameter.SourceColumn = "created_on";
            parameter = adapter.InsertCommand.Parameters.Add("@created_by", NpgsqlDbType.Integer);
            parameter.SourceColumn = "created_by";
            parameter = adapter.InsertCommand.Parameters.Add("@changed_on", NpgsqlDbType.Timestamp);
            parameter.SourceColumn = "changed_on";
            parameter = adapter.InsertCommand.Parameters.Add("@changed_by", NpgsqlDbType.Integer);
            parameter.SourceColumn = "changed_by";
            if (!TransferData.TransferDataDb.UpdateDbTable(adapter,dt)) return false;
            return true;
        }

        public override void SetDefaultValuesAfterRowAdded(DataGridView dgv, int index)
        {
            dgv.Rows[index].Cells["nzp_act"].Value = DBNull.Value;
            dgv.Rows[index].Cells["created_on"].Value = DateTime.Now;
            dgv.Rows[index].Cells["created_by"].Value = MainWindow.UserId;
            dgv.Rows[index].Cells["user_name_created"].Value = MainWindow.UserName;
            dgv.Rows[index].Cells["changed_by"].Value = DBNull.Value;
            dgv.Rows[index].Cells["file_name"].Value = string.Empty;
            dgv.Rows[index].Cells["name"].Value = string.Empty;
        }

        public override void SetDefaultValuesAfterCellChange(DataGridView dgv, int row, int column)
        {
            var dataGridViewColumn = dgv.Columns["forscript"];
            if (dataGridViewColumn != null && dataGridViewColumn.Index == column)
            {
                // ключ значение для вставки
                Dictionary<string, string> valColumns = new Dictionary<string, string>
                {
                    {"name", GenerateScript.GetCellValue(dgv.Rows[row].Cells["name"])}, 
                    {"file_name", GenerateScript.GetCellValue(dgv.Rows[row].Cells["file_name"])}, 
                };
                // ключ значение основной колонки
                Dictionary<string, int> idColVal = new Dictionary<string, int> 
                { { "nzp_rep", (int)dgv.Rows[row].Cells["nzp_rep"].Value } };
                // ключ значение для where
                Dictionary<string, string> whereDict = new Dictionary<string, string> 
                {{"nzp_act", dgv.Rows[row].Cells["nzp_act"].Value.ToString()}};
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
                            valColumns.Add("nzp_act", GenerateScript.GetCellValue(dgv.Rows[row].Cells["nzp_act"]));
                            break;
                        case ScriptGenerateOper.Update:
                            break;
                        case ScriptGenerateOper.Delete:
                            break;
                    }
                GenerateScript.AddVal(TableName, valColumns, whereDict, idColVal, typescr);
       
                return;
            }
            dgv.Rows[row].Cells["user_name_changed"].Value = MainWindow.UserName;
            dgv.Rows[row].Cells["changed_on"].Value = DateTime.Now;
            dgv.Rows[row].Cells["changed_by"].Value = MainWindow.UserId;
        }
    }
}
