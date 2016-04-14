using System;
using System.Data;
using System.Windows.Forms;
using Npgsql;
using webroles.GenerateScriptTable;
using webroles.Properties;
using System.Collections.Generic;
using webroles.TransferData;

namespace webroles.TablesFirstLevel
{
    class SactionTable:TableFirstLevel,ISubject,INamePosition, IDeletable
    {
        DataGridViewColumn[] columnCollection;
        private int position;
        private const string nodeTreeViewText = "Действия";
        private readonly string selectCommand = "WITH selectcreate as (SELECT nzp_act, act_name, hlp, is_dev, created_on, created_by, changed_on, changed_by, user_name as  user_name_created, _comment FROM " + Tables.s_actions + " s LEFT OUTER JOIN " + Tables.users + " u  ON (s.created_by= u.id)) " +
            "SELECT nzp_act, act_name, hlp, is_dev, created_on, created_by, changed_on, changed_by, user_name_created, user_name as user_name_changed, _comment FROM selectcreate LEFT OUTER JOIN  " + Tables.users + " u ON (selectcreate.changed_by= u.id) ORDER BY selectcreate.nzp_act;";

        readonly List<IObserver> observers = new List<IObserver>();
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
                return Tables.s_actions;
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
            columnCollection[10] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.s_actions, "user_name_changed", Resources.user_name_chang, true, true);
            columnCollection[9] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.s_actions, "changed_by", Resources.changed_by, true, false);
            columnCollection[8] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.s_actions, "changed_on", Resources.changed_on, true, true);
            columnCollection[7] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.s_actions, "user_name_created", Resources.user_name_creat, true, true);
            columnCollection[6] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.s_actions, "created_by", Resources.created_by, true, false);
            columnCollection[5] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.s_actions, "created_on", Resources.created_on, true, true);
            columnCollection[4] = CreateDataGridViewColumn.CreateCheckBoxColumn(Tables.s_actions, "is_dev", Resources.is_dev);
            columnCollection[3] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.s_actions, "_comment", "Комментарий", false, true);
            columnCollection[2] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.s_actions, "hlp", Resources.s_actionsTable_act_hlp, false, true);
            columnCollection[1] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.s_actions, "act_name", Resources.s_actionsTable_act_name, false, true);
            columnCollection[0] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.s_actions, "nzp_act", Resources.s_actionsTable_nzp_act, true, true);
            columnCollection[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
        }

        public override bool Save(NpgsqlConnection connect, NpgsqlDataAdapter adapter,DataTable dt)
        {
            adapter.DeleteCommand = new NpgsqlCommand("Delete from " + Tables.s_actions + " Where nzp_act=@nzp_act", connect);
            NpgsqlParameter paramter = adapter.DeleteCommand.Parameters.Add("@nzp_act", NpgsqlTypes.NpgsqlDbType.Integer);
            paramter.SourceColumn = "nzp_act";
            paramter.SourceVersion = DataRowVersion.Original;

            adapter.UpdateCommand = new NpgsqlCommand("Update " + Tables.s_actions + " set nzp_act=@nzp_act, act_name=@act_name, hlp=@hlp, is_dev=@is_dev, _comment=@_comment, created_on=@created_on, created_by=@created_by, changed_on=@changed_on, changed_by=@changed_by where nzp_act=@nzp_act", connect);
            paramter = adapter.UpdateCommand.Parameters.Add("@nzp_act", NpgsqlTypes.NpgsqlDbType.Integer);
            paramter.SourceColumn = "nzp_act";
            adapter.UpdateCommand.Parameters.Add("@act_name", NpgsqlTypes.NpgsqlDbType.Char, 255, "act_name");
            adapter.UpdateCommand.Parameters.Add("@hlp", NpgsqlTypes.NpgsqlDbType.Char, 255, "hlp");
            paramter = adapter.UpdateCommand.Parameters.Add("@is_dev", NpgsqlTypes.NpgsqlDbType.Integer);
            paramter.SourceColumn = "is_dev";
            adapter.UpdateCommand.Parameters.Add("@_comment", NpgsqlTypes.NpgsqlDbType.Varchar, 1000, "_comment");
            paramter = adapter.UpdateCommand.Parameters.Add("@created_on", NpgsqlTypes.NpgsqlDbType.Timestamp);
            paramter.SourceColumn = "created_on";
            paramter = adapter.UpdateCommand.Parameters.Add("@created_by", NpgsqlTypes.NpgsqlDbType.Integer);
            paramter.SourceColumn = "created_by";
            paramter = adapter.UpdateCommand.Parameters.Add("@changed_on", NpgsqlTypes.NpgsqlDbType.Timestamp);
            paramter.SourceColumn = "changed_on";
            paramter = adapter.UpdateCommand.Parameters.Add("@changed_by", NpgsqlTypes.NpgsqlDbType.Integer);
            paramter.SourceColumn = "changed_by";

            adapter.InsertCommand = new NpgsqlCommand("insert into " + Tables.s_actions + " ( act_name, hlp, is_dev, created_on, created_by, changed_on, changed_by, _comment) " +
                "VALUES (@act_name, @hlp, @is_dev, @created_on, @created_by, @changed_on, @changed_by,@_comment); ", connect);
            adapter.InsertCommand.Parameters.Add("@act_name", NpgsqlTypes.NpgsqlDbType.Char, 255, "act_name");
            adapter.InsertCommand.Parameters.Add("@hlp", NpgsqlTypes.NpgsqlDbType.Char, 255, "hlp");
            paramter = adapter.InsertCommand.Parameters.Add("@is_dev", NpgsqlTypes.NpgsqlDbType.Integer);
            paramter.SourceColumn = "is_dev";
            paramter = adapter.InsertCommand.Parameters.Add("@created_on", NpgsqlTypes.NpgsqlDbType.Timestamp);
            paramter.SourceColumn = "created_on";
            paramter = adapter.InsertCommand.Parameters.Add("@created_by", NpgsqlTypes.NpgsqlDbType.Integer);
            paramter.SourceColumn = "created_by";
            paramter = adapter.InsertCommand.Parameters.Add("@changed_on", NpgsqlTypes.NpgsqlDbType.Timestamp);
            paramter.SourceColumn = "changed_on";
            paramter = adapter.InsertCommand.Parameters.Add("@changed_by", NpgsqlTypes.NpgsqlDbType.Integer);
            paramter.SourceColumn = "changed_by";
            adapter.InsertCommand.Parameters.Add("@_comment", NpgsqlTypes.NpgsqlDbType.Varchar, 1000, "_comment");
            if (!TransferData.TransferDataDb.UpdateDbTable(adapter,dt)) return false;
            NotifyObservers();
            return true;
        }
        public override void SetDefaultValuesAfterRowAdded(DataGridView dgv, int index)
        {
            dgv.Rows[index].Cells["created_on"].Value = DateTime.Now;
            dgv.Rows[index].Cells["is_dev"].Value = 1;
            dgv.Rows[index].Cells["created_by"].Value = MainWindow.UserId;
            dgv.Rows[index].Cells["user_name_created"].Value = MainWindow.UserName;
            dgv.Rows[index].Cells["changed_by"].Value = DBNull.Value;
        }
        public string BaseColumn
        {
            get { return "nzp_act"; }
        }

        public void AddObservers(List<IObserver> obsrvs)
        {
            observers.AddRange(obsrvs);
        }

        public void NotifyObservers()
        {
            var dt = ComboBoxBindingSource.Update("SELECT nzp_act as id, nzp_act||' '|| act_name as text FROM s_actions ORDER BY id;");
            for (int i = 0; i < observers.Count; i++)
            {
                if (!observers[i].AdditionalContition)
                    observers[i].update(dt);
                else
                    observers[i].update(ComboBoxBindingSource.Update(observers[i].AdditionalContotionString));
            }
        }

        public string GetNamePosition(DataGridViewRow dataRow)
        {
             int num;
            string text;
            if (dataRow != null)
            {
                num = (int)dataRow.Cells["nzp_act"].Value;
                if (dataRow.Cells["nzp_act"].Value != null)
                {
                    text = dataRow.Cells["act_name"].Value.ToString();
                }
                else
                {
                    text = string.Empty;
                }
            }
            else
            {
                position = 0;  num = 1;
                text = "Выполнить поиск";
            }
            return "Действие" + ": " + num + " " + text;
        }
        public int PositionDefault
        {
            get { return 1; }
        }
  
        public int Position
        {
            get { return position; }
            set { position = value; }
        }
        public override void SetDefaultValuesAfterCellChange(DataGridView dgv, int row, int column)
        {
            var dataGridViewColumn = dgv.Columns["forscript"];
            if (dataGridViewColumn != null && dataGridViewColumn.Index == column)
            {
                // ключ значения для вставки
                Dictionary<string, string> valColumns = new Dictionary<string, string>
                {
                    {"act_name", GenerateScript.GetCellValue(dgv.Rows[row].Cells["act_name"])},
                    {"hlp", GenerateScript.GetCellValue(dgv.Rows[row].Cells["hlp"])}
                };
                // ключ значение для where
                Dictionary<string, string> whereDict = new Dictionary<string, string>
                {{"nzp_act", dgv.Rows[row].Cells["nzp_act"].Value.ToString()}};
              
                // ключ значение базовой колонки
                Dictionary<string, int> idColVal = new Dictionary<string, int> 
                { { "nzp_act", (int)dgv.Rows[row].Cells["nzp_act"].Value } };
                ScriptGenerateOper typescr;
                if (dgv.Rows[row].Cells["forscript"].Value == null)
                {
                    typescr = ScriptGenerateOper.none;
                }
                else
                {
                    typescr = (ScriptGenerateOper) dgv.Rows[row].Cells["forscript"].Value;
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
                GenerateScript.AddVal(TableName, valColumns, whereDict, idColVal, typescr );
                return;
            }
            dgv.Rows[row].Cells["user_name_changed"].Value = MainWindow.UserName;
            dgv.Rows[row].Cells["changed_on"].Value = DateTime.Now;
            dgv.Rows[row].Cells["changed_by"].Value = MainWindow.UserId;
        }

        public bool CheckToAllowDelete(DataGridView dvg)
        {
            if (dvg.CurrentRow == null) return true;
            var posit = dvg.CurrentRow.Cells["nzp_act"].Value;
            if (posit == DBNull.Value) return true;
            string[] tablesArr =
            {
                Tables.actions_lnk,  Tables.actions_show, Tables.img_lnk, Tables.role_actions
            };
            Returns ret = new Returns(true, "");
            string commandText = "SELECT EXISTS (Select nzp_al from " + Tables.actions_lnk + " where nzp_act=" + posit + ");";
            commandText += "SELECT EXISTS (select nzp_ash from " + Tables.actions_show + " Where nzp_act=" + posit + ");";
            commandText += "SELECT EXISTS (select nzp_img from " + Tables.img_lnk + " Where tip=2 and kod=" + posit + ");";
            commandText += "SELECT EXISTS (select id from " + Tables.role_actions + " Where nzp_act=" + posit + ");";
            return DBManager.CheckExistsRefernces(commandText, tablesArr, (int)posit);
        }
    }
}
