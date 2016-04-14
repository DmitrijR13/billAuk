using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Forms;
using Npgsql;
using NpgsqlTypes;
using webroles.GenerateScriptTable;
using webroles.Properties;
using System.Data;
using webroles.TransferData;

namespace webroles.TablesSecondLevel
{
    class RolePagesTable:TableSecondLevel
    {
        public RolePagesTable(string nameParentTreeViewNode)
        {
            this.nameParentTreeViewNode = nameParentTreeViewNode;
        }
        private readonly string nameParentTreeViewNode;
        readonly List<IObserver> nzpPageList = new List<IObserver>();
        public List<IObserver> ObserversPagesTable
        {
            get { return nzpPageList; }
        }
        DataGridViewColumn[] columnCollection;
        private const string nodeTreeViewText = "Страницы ролей";
        string selectCommand = ""; //"WITH selectcreate AS (SELECT role_pages.id, nzp_role, nzp_page, created_on, created_by, user_name as user_name_created "+
//"FROM role_pages LEFT OUTER JOIN users ON (created_by=users.id)) "+
//"SELECT  selectcreate.id, selectcreate.nzp_role, selectcreate.nzp_page, selectcreate.created_on, selectcreate.created_by,  user_name_created, "+ 
//"s_roles.nzp_role||' '|| s_roles.role as role_name "+
//"FROM selectcreate LEFT OUTER JOIN s_roles ON (selectcreate.nzp_role= s_roles.nzp_role);";
        public override string NameParentTable
        {
            get { return nameParentTreeViewNode; }
        }

        public override string NameParentColumn
        {
            get { return "nzp_role"; }
        }
        public override string NameOwnBaseColumn
        {
            get { return "nzp_role"; }
        }

        public override void GetCorrespondTable(DataSet dataSet, NpgsqlDataAdapter adapter, NpgsqlConnection connect, int position)
        {
                selectCommand = "WITH selectcreate AS (SELECT rp.id, nzp_role, nzp_page, created_on, created_by, user_name as user_name_created "+
"FROM " + Tables.role_pages + " rp LEFT OUTER JOIN " + Tables.users + " u ON (created_by=u.id)) " +
"SELECT  selectcreate.id, selectcreate.nzp_role, selectcreate.nzp_page, selectcreate.created_on, selectcreate.created_by,  user_name_created, "+ 
"s.nzp_role||' '|| s.role as role_name "+
"FROM selectcreate LEFT OUTER JOIN " + Tables.s_roles + " s ON (selectcreate.nzp_role= s.nzp_role) WHERE selectcreate.nzp_role =" + position + "ORDER BY selectcreate.id ";
                adapter.SelectCommand.CommandText = selectCommand;
                dataSet.Tables[Tables.role_pages].Clear();
                TransferDataDb.Fill(adapter, dataSet.Tables[TableName]);
        }

        public override string NodeTreeViewText
        {
            get { return nodeTreeViewText; }
        }

        public override string TableName
        {
            get { return Tables.role_pages; }
        }

        public override string SelectCommand
        {
            get { return getSelectCommand(); }
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

        private string getSelectCommand()
        {
            return "WITH selectcreate AS (SELECT rp.id, nzp_role, nzp_page, created_on, created_by, user_name as user_name_created " +
"FROM " + Tables.role_pages + " rp LEFT OUTER JOIN " + Tables.users + " u ON (created_by=u.id)) " +
"SELECT  selectcreate.id, selectcreate.nzp_role, selectcreate.nzp_page, selectcreate.created_on, selectcreate.created_by,  user_name_created, " +
"s.nzp_role||' '|| s.role as role_name " +
"FROM selectcreate LEFT OUTER JOIN " + Tables.s_roles + " s ON (selectcreate.nzp_role= s.nzp_role) WHERE selectcreate.nzp_role =" +Position+ "ORDER BY selectcreate.id ";
        }

        public override void CreateColumns()
        {
            columnCollection = new DataGridViewColumn[8];
            columnCollection[7] = CreateDataGridViewColumn.CreateComboBoxColumn("Для скрипта");
            columnCollection[6] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.role_pages, "user_name_created", Resources.user_name_creat, true, true);
            columnCollection[5] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.role_pages, "created_by", Resources.created_by, true, false);
            columnCollection[4] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.role_pages, "created_on", Resources.created_on, true, true);
            columnCollection[3] = CreateDataGridViewColumn.CreateComboBoxColumn<string>("Текущая страница", "nzp_page");
            columnCollection[3].SortMode = DataGridViewColumnSortMode.Automatic;
            nzpPageList.Add(new ComboBoxBindingSource(columnCollection[3], true, "", false, true));
            columnCollection[2] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.role_pages, "nzp_role", "Текущая роль", true, false);
            columnCollection[1] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.role_pages, "role_name", "Наименование роли", true, false);
            columnCollection[0] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.role_pages, "id", Resources.actions_showTable_id, true, true);
            columnCollection[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
        }

        public override bool Save(NpgsqlConnection connect, NpgsqlDataAdapter adapter, DataTable dt)
        {
            // Удаление строк
            adapter.DeleteCommand = new NpgsqlCommand("Delete from " + Tables.role_pages + " Where id=@id", connect);
            NpgsqlParameter paramter = adapter.DeleteCommand.Parameters.Add("@id", NpgsqlDbType.Integer);
            paramter.SourceColumn = "id";
            paramter.SourceVersion = DataRowVersion.Original;

            // Обновление данных
            adapter.UpdateCommand = new NpgsqlCommand("UPDATE " + Tables.role_pages + " SET id=@id, nzp_role=@nzp_role,  nzp_page=@nzp_page, " +
             "created_on=@created_on, created_by=@created_by " + "Where id=@id", connect);
            paramter = adapter.UpdateCommand.Parameters.Add("@id", NpgsqlDbType.Integer);
            paramter.SourceColumn = "id";
            paramter = adapter.UpdateCommand.Parameters.Add("@nzp_role", NpgsqlDbType.Bigint);
            paramter.SourceColumn = "nzp_role";
            paramter = adapter.UpdateCommand.Parameters.Add("@nzp_page", NpgsqlDbType.Integer);
            paramter.SourceColumn = "nzp_page";
            paramter = adapter.UpdateCommand.Parameters.Add("@created_on", NpgsqlDbType.Timestamp);
            paramter.SourceColumn = "created_on";
            paramter = adapter.UpdateCommand.Parameters.Add("@created_by", NpgsqlDbType.Integer);
            paramter.SourceColumn = "created_by";

            // Вставка строк id, @id,
            adapter.InsertCommand = new NpgsqlCommand("Insert into " + Tables.role_pages + " ( nzp_role,  nzp_page, created_on, created_by) " +
            "values (@nzp_role,  @nzp_page, @created_on, @created_by); ", connect);
            paramter = adapter.InsertCommand.Parameters.Add("@nzp_role", NpgsqlDbType.Bigint);
            paramter.SourceColumn = "nzp_role";
            paramter = adapter.InsertCommand.Parameters.Add("@nzp_page", NpgsqlDbType.Integer);
            paramter.SourceColumn = "nzp_page";
            paramter = adapter.InsertCommand.Parameters.Add("@created_on", NpgsqlDbType.Timestamp);
            paramter.SourceColumn = "created_on";
            paramter = adapter.InsertCommand.Parameters.Add("@created_by", NpgsqlDbType.Integer);
            paramter.SourceColumn = "created_by";
            if (!TransferData.TransferDataDb.UpdateDbTable(adapter, dt)) return false;
            return true;
        }

        public override void SetDefaultValuesAfterRowAdded(DataGridView dgv, int index)
        {
            dgv.Rows[index].Cells["nzp_role"].Value = position;
            dgv.Rows[index].Cells["nzp_page"].Value = DBNull.Value;
            dgv.Rows[index].Cells["created_on"].Value = DateTime.Now;
            dgv.Rows[index].Cells["created_by"].Value = MainWindow.UserId;
            dgv.Rows[index].Cells["user_name_created"].Value = MainWindow.UserName;
           // dgv.Rows[index].Cells["changed_by"].Value = DBNull.Value;
        }
        public override void SetDefaultValuesAfterCellChange(DataGridView dgv, int row, int column)
        {
            var dataGridViewCol = dgv.Columns["forscript"];
            if (dataGridViewCol == null || dataGridViewCol.Index != column) return;
            // ключ значения для вставки
            Dictionary<string, string> valColumns = new Dictionary<string, string>
            {
                {"nzp_role", GenerateScript.GetCellValue(dgv.Rows[row].Cells["nzp_role"])},
                {"nzp_page", GenerateScript.GetCellValue(dgv.Rows[row].Cells["nzp_page"])},
            };
            // ключ значение базовой колонки
            Dictionary<string, int> idColVal = new Dictionary<string, int>
            {
                {"id", (int) dgv.Rows[row].Cells["id"].Value}
            };
            // ключ значение для where
            Dictionary<string, string> whereVaCols = new Dictionary<string, string>
            {
                {"nzp_role", GenerateScript.GetCellValue(dgv.Rows[row].Cells["nzp_role"])},
                {"nzp_page", GenerateScript.GetCellValue(dgv.Rows[row].Cells["nzp_page"])},
            };
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
                case ScriptGenerateOper.Delete:
                    break;
                case ScriptGenerateOper.Update:
                    MessageBox.Show("Строки этой таблицы обновлять нельзя!");
                    typescr = ScriptGenerateOper.none;
                    dgv.Rows[row].Cells["forscript"].Value = null;
                    break;
            }

            var res = GenerateScript.AddValSpecialForRolePagesAndRoleActions(TableName, whereVaCols, valColumns, idColVal, typescr, (int)dgv.Rows[row].Cells["nzp_role"].Value);
            if (res == null)
            {
                dgv.Rows[row].Cells["forscript"].Value = null;
            }
        }
    }
}
