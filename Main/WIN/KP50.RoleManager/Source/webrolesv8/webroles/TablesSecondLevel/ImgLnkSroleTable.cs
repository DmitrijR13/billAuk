using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Npgsql;
using NpgsqlTypes;
using webroles.GenerateScriptTable;
using webroles.Properties;
using System.Data;
using webroles.TransferData;

namespace webroles.TablesSecondLevel
{
    class ImgLnkSroleTable:TableSecondLevel
    {
        public ImgLnkSroleTable(string nameParentTreeViewNode)
        {
            this.nameParentTreeViewNode = nameParentTreeViewNode;
        }
        private readonly string nameParentTreeViewNode;
        readonly List<IObserver> observers = new List<IObserver>();
        public List<IObserver> ObserversPagesTable
        {
            get { return observers; }
        }
        DataGridViewColumn[] columnCollection;
        private const string nodeTreeViewText = "Изображения ролей";
        private const string tableName = "img_lnk_s_roles";
        string selectCommand = ""; //"WITH selectcreate as (SELECT nzp_img, cur_page, nzp_role, img_url, created_on, created_by, changed_on, changed_by, user_name as user_name_created " +
//"FROM img_lnk LEFT OUTER JOIN users ON (created_by=id)), " +
//"selectchange AS ( SELECT  nzp_img, cur_page, nzp_role, img_url,  created_on, created_by, changed_on, changed_by,user_name_created, user_name as user_name_changed " +
//"FROM selectcreate LEFT OUTER JOIN users ON (changed_by=id)) " +
//"SELECT nzp_img, cur_page, selectchange.nzp_role, img_url, selectchange.created_on, selectchange.created_by, selectchange.changed_on, selectchange.changed_by, " +
//"user_name_created, user_name_changed, s_roles.nzp_role||' '|| s_roles.role as role_name " +
//"FROM selectchange LEFT OUTER JOIN s_roles ON (selectchange.nzp_role=s_roles.nzp_role);";
  
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

        public override void GetCorrespondTable(DataSet dataSet, NpgsqlDataAdapter adapter, NpgsqlConnection connect, int positionParam)
        {

                selectCommand = "WITH selectcreate as (SELECT nzp_img, cur_page, nzp_role, img_url, kod, tip, created_on, created_by, changed_on, changed_by, user_name as user_name_created " +
"FROM " + Tables.img_lnk + " LEFT OUTER JOIN " + Tables.users + " ON (created_by=id)), " +
"selectchange AS ( SELECT  nzp_img, cur_page, nzp_role, img_url, kod, tip, created_on, created_by, changed_on, changed_by,user_name_created, user_name as user_name_changed " +
"FROM selectcreate LEFT OUTER JOIN " + Tables.users + " ON (changed_by=id)) " +
"SELECT nzp_img, cur_page, selectchange.nzp_role, img_url, kod, tip, selectchange.created_on, selectchange.created_by, selectchange.changed_on, selectchange.changed_by, " +
"user_name_created, user_name_changed, s.nzp_role||' '|| s.role as role_name " +
"FROM selectchange LEFT OUTER JOIN " + Tables.s_roles + " s ON (selectchange.nzp_role=s.nzp_role) WHERE selectchange.nzp_role=" + positionParam + "ORDER BY nzp_img; ";
                dataSet.Tables[tableName].Clear();
                adapter.SelectCommand.CommandText = selectCommand;
                TransferDataDb.Fill(adapter, dataSet.Tables[TableName]);
        }

        public override string NodeTreeViewText
        {
            get { return nodeTreeViewText; }
        }

        public override string TableName
        {
            get { return tableName; }
        }

        public override string SelectCommand
        {
            get { return getSelectCommand(); }
        }

        private string getSelectCommand()
        {
            return "WITH selectcreate as (SELECT nzp_img, cur_page, nzp_role, img_url, kod, tip, created_on, created_by, changed_on, changed_by, user_name as user_name_created " +
"FROM " + Tables.img_lnk + " LEFT OUTER JOIN " + Tables.users + " ON (created_by=id)), " +
"selectchange AS ( SELECT  nzp_img, cur_page, nzp_role, img_url, kod, tip, created_on, created_by, changed_on, changed_by,user_name_created, user_name as user_name_changed " +
"FROM selectcreate LEFT OUTER JOIN " + Tables.users + " ON (changed_by=id)) " +
"SELECT nzp_img, cur_page, selectchange.nzp_role, img_url, kod, tip, selectchange.created_on, selectchange.created_by, selectchange.changed_on, selectchange.changed_by, " +
"user_name_created, user_name_changed, s.nzp_role||' '|| s.role as role_name " +
"FROM selectchange LEFT OUTER JOIN " + Tables.s_roles + " s ON (selectchange.nzp_role=s.nzp_role) WHERE selectchange.nzp_role=" + Position + "ORDER BY nzp_img; ";
        }

        public override DataGridViewColumn[] GetTableColumns
        {
            get
            {
               return columnCollection;
            }
            set
            {
                columnCollection=value;
            }
        }

        public override void CreateColumns()
        {
            columnCollection = new DataGridViewColumn[14];
            columnCollection[13] = CreateDataGridViewColumn.CreateComboBoxColumn("Для скрипта");
            columnCollection[12] = CreateDataGridViewColumn.CreateTextBoxColumn(tableName, "user_name_changed", Resources.user_name_chang, true, true);
            columnCollection[11] = CreateDataGridViewColumn.CreateTextBoxColumn(tableName, "changed_by", Resources.changed_by, true, false);
            columnCollection[10] = CreateDataGridViewColumn.CreateTextBoxColumn(tableName, "changed_on", Resources.changed_on, true, true);
            columnCollection[9] = CreateDataGridViewColumn.CreateTextBoxColumn(tableName, "user_name_created", Resources.user_name_creat, true, true);
            columnCollection[8] = CreateDataGridViewColumn.CreateTextBoxColumn(tableName, "created_by", Resources.created_by, true, false);
            columnCollection[7] = CreateDataGridViewColumn.CreateTextBoxColumn(tableName, "created_on", Resources.created_on, true, true);
            columnCollection[6] = CreateDataGridViewColumn.CreateTextBoxColumn(tableName, "img_url", "URL картинки", false, true);
            columnCollection[5] = CreateDataGridViewColumn.CreateTextBoxColumn(tableName, "kod", "Роль", true, false);
            columnCollection[4] = CreateDataGridViewColumn.CreateTextBoxColumn(tableName, "tip", "Роль", true, false);
            columnCollection[3] = CreateDataGridViewColumn.CreateTextBoxColumn(tableName, "nzp_role", "Роль", true, false);
            columnCollection[2] = CreateDataGridViewColumn.CreateTextBoxColumn(tableName, "role_name", "Наименование роли", true, false);
            //ColumnCollection[2] = CreateDataGridViewColumn.CreateTextBoxColumn(tableName, "tip", "Тип", false, true);
            columnCollection[1] = CreateDataGridViewColumn.CreateComboBoxColumn<string>("Текущая страница", "cur_page");
            observers.Add(new ComboBoxBindingSource(columnCollection[1], true, "", false, true));
            columnCollection[0] = CreateDataGridViewColumn.CreateTextBoxColumn(tableName, "nzp_img", "Код", true, true);
            columnCollection[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
        }

        public override bool Save(NpgsqlConnection connect, NpgsqlDataAdapter adapter, DataTable dt)
        {
            // Удаление строк
            adapter.DeleteCommand = new NpgsqlCommand("Delete from " + Tables.img_lnk + " Where nzp_img=@nzp_img", connect);
            NpgsqlParameter paramter = adapter.DeleteCommand.Parameters.Add("@nzp_img", NpgsqlDbType.Integer);
            paramter.SourceColumn = "nzp_img";
            paramter.SourceVersion = DataRowVersion.Original;

            // Обновление данных
            adapter.UpdateCommand = new NpgsqlCommand("UPDATE " + Tables.img_lnk + " SET nzp_img=@nzp_img, cur_page=@cur_page, nzp_role=@nzp_role, img_url=@img_url, kod=@kod, tip=@tip, " +
             "created_on=@created_on, created_by=@created_by, changed_on=@changed_on, changed_by=@changed_by " + "Where nzp_img=@nzp_img", connect);
            paramter = adapter.UpdateCommand.Parameters.Add("@nzp_img", NpgsqlDbType.Integer);
            paramter.SourceColumn = "nzp_img";
            paramter = adapter.UpdateCommand.Parameters.Add("@cur_page", NpgsqlDbType.Bigint);
            paramter.SourceColumn = "cur_page";
            paramter = adapter.UpdateCommand.Parameters.Add("@nzp_role", NpgsqlDbType.Integer);
            paramter.SourceColumn = "nzp_role";
            paramter = adapter.UpdateCommand.Parameters.Add("@kod", NpgsqlDbType.Integer);
            paramter.SourceColumn = "kod";
            paramter = adapter.UpdateCommand.Parameters.Add("@tip", NpgsqlDbType.Integer);
            paramter.SourceColumn = "tip";
            adapter.UpdateCommand.Parameters.Add("@img_url", NpgsqlDbType.Char, 200, "img_url");
            paramter = adapter.UpdateCommand.Parameters.Add("@created_on", NpgsqlDbType.Timestamp);
            paramter.SourceColumn = "created_on";
            paramter = adapter.UpdateCommand.Parameters.Add("@created_by", NpgsqlDbType.Integer);
            paramter.SourceColumn = "created_by";
            paramter = adapter.UpdateCommand.Parameters.Add("@changed_on", NpgsqlDbType.Timestamp);
            paramter.SourceColumn = "changed_on";
            paramter = adapter.UpdateCommand.Parameters.Add("@changed_by", NpgsqlDbType.Integer);
            paramter.SourceColumn = "changed_by";
            paramter.SourceVersion = DataRowVersion.Original;

            // Вставка строк nzp_img, nextval('img_lnk_nzp_img_seq'::regclass),
            adapter.InsertCommand = new NpgsqlCommand("Insert into " + Tables.img_lnk + " ( cur_page,  nzp_role, img_url, kod, tip, created_on, created_by, changed_on, changed_by) " +
            "values ( @cur_page,  @nzp_role, @img_url, @kod, @tip, @created_on, @created_by, @changed_on, @changed_by); ", connect);
            //paramter = adapter.InsertCommand.Parameters.Add("@nzp_img", NpgsqlTypes.NpgsqlDbType.Integer);
            //paramter.SourceColumn = "nzp_img";
            paramter = adapter.InsertCommand.Parameters.Add("@cur_page", NpgsqlDbType.Bigint);
            paramter.SourceColumn = "cur_page";
            paramter = adapter.InsertCommand.Parameters.Add("@nzp_role", NpgsqlDbType.Integer);
            paramter.SourceColumn = "nzp_role";
            paramter = adapter.InsertCommand.Parameters.Add("@kod", NpgsqlDbType.Integer);
            paramter.SourceColumn = "kod";
            paramter = adapter.InsertCommand.Parameters.Add("@tip", NpgsqlDbType.Integer);
            paramter.SourceColumn = "tip";
            adapter.InsertCommand.Parameters.Add("@img_url", NpgsqlDbType.Char, 255, "img_url");
            paramter = adapter.InsertCommand.Parameters.Add("@created_on", NpgsqlDbType.Timestamp);
            paramter.SourceColumn = "created_on";
            paramter = adapter.InsertCommand.Parameters.Add("@created_by", NpgsqlDbType.Integer);
            paramter.SourceColumn = "created_by";
            paramter = adapter.InsertCommand.Parameters.Add("@changed_on", NpgsqlDbType.Timestamp);
            paramter.SourceColumn = "changed_on";
            paramter = adapter.InsertCommand.Parameters.Add("@changed_by", NpgsqlDbType.Integer);
            paramter.SourceColumn = "changed_by";
            if (!TransferData.TransferDataDb.UpdateDbTable(adapter,dt)) return false;
            return true;
        }

        public override void SetDefaultValuesAfterRowAdded(DataGridView dgv, int index)
        {
            dgv.Rows[index].Cells["nzp_role"].Value = position;
            dgv.Rows[index].Cells["cur_page"].Value = DBNull.Value;
            dgv.Rows[index].Cells["role_name"].Value = DBNull.Value;
            dgv.Rows[index].Cells["created_on"].Value = DateTime.Now;
            dgv.Rows[index].Cells["created_by"].Value = MainWindow.UserId;
            dgv.Rows[index].Cells["user_name_created"].Value = MainWindow.UserName;
            dgv.Rows[index].Cells["changed_by"].Value = DBNull.Value;
            dgv.Rows[index].Cells["kod"].Value = position;
            dgv.Rows[index].Cells["tip"].Value = 3;
        }

        public override void SetDefaultValuesAfterCellChange(DataGridView dgv, int row, int column)
        {
            var dataGridViewColumn = dgv.Columns["forscript"];
            if (dataGridViewColumn != null && dataGridViewColumn.Index == column)
            {
                // ключ значения для вставки
                Dictionary<string, string> valColumns = new Dictionary<string, string>
                {
                    {"img_url", GenerateScript.GetCellValue(dgv.Rows[row].Cells["img_url"])},
                };
                // ключ значение базовой колонки
                Dictionary<string, int> idColVal = new Dictionary<string, int>
                {
                    {"nzp_img", (int) dgv.Rows[row].Cells["nzp_img"].Value}
                };
                // ключ значение для where
                Dictionary<string, string> whereDict = new Dictionary<string, string>
                {
                    {"cur_page", GenerateScript.GetCellValue(dgv.Rows[row].Cells["cur_page"])},
                    {"tip", GenerateScript.GetCellValue(dgv.Rows[row].Cells["tip"])},
                    {"kod", GenerateScript.GetCellValue(dgv.Rows[row].Cells["kod"])}
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
                        valColumns.Add("cur_page", GenerateScript.GetCellValue(dgv.Rows[row].Cells["cur_page"]));
                        valColumns.Add("tip", GenerateScript.GetCellValue(dgv.Rows[row].Cells["tip"]));
                        valColumns.Add("kod", GenerateScript.GetCellValue(dgv.Rows[row].Cells["kod"]));
                        break;
                    case ScriptGenerateOper.Update:
                        break;
                    case ScriptGenerateOper.Delete:
                        break;
                }
                GenerateScript.AddVal(Tables.img_lnk, valColumns, whereDict, idColVal, typescr, (int)dgv.Rows[row].Cells["nzp_role"].Value);
                return;
            }
            dgv.Rows[row].Cells["user_name_changed"].Value = MainWindow.UserName;
            dgv.Rows[row].Cells["changed_on"].Value = DateTime.Now;
            dgv.Rows[row].Cells["changed_by"].Value = MainWindow.UserId;
        }
    }
}
