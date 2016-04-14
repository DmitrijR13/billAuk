using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using Npgsql;
using webroles.GenerateScriptTable;
using webroles.Properties;
using webroles.TransferData;

namespace webroles.TablesSecondLevel
{
    class ActionShowTable : TableSecondLevel,ISubject
    {
        private readonly string nameParentTreeViewNode;
        public ActionShowTable(string nameParentTreeViewNode)
        {
            this.nameParentTreeViewNode = nameParentTreeViewNode;
        }
        readonly List<IObserver> observers = new List<IObserver>();
        DataGridViewColumn[] columnCollection;
        private const string nodeTreeViewText = "Действия на странице";
        string selectCommand = "";
        readonly List<IObserver> nzpActList = new List<IObserver>();
        public List<IObserver> ObserversSactionsTable
       {
           get { return nzpActList; }
       }
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
                return Tables.actions_show;
            }
        }
        public override string SelectCommand
        {
            get
            {
                return getSelectCommand();
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

        private string getSelectCommand()
        {
            return  "WITH selectcreate as (SELECT nzp_ash, cur_page, nzp_act, sort_kod, is_dev, action_types_id, act_tip, act_dd, created_on, created_by, " +
                              "changed_on, changed_by, user_name as user_name_created " +
"FROM " + Tables.actions_show + " a LEFT OUTER JOIN " + Tables.users + " u ON (a.created_by=u.id)), " +
"selectchange AS (SELECT nzp_ash, cur_page, nzp_act, sort_kod, is_dev, action_types_id, act_tip, act_dd, created_on, created_by, changed_on, " +
                              "changed_by, user_name_created, user_name as user_name_changed " +
"FROM selectcreate LEFT OUTER JOIN " + Tables.users + " ON (changed_by=id)) " +
"SELECT nzp_ash, cur_page, selectchange.nzp_act, selectchange.sort_kod, selectchange.is_dev, action_types_id, selectchange.act_tip, selectchange.act_dd, selectchange.created_on, " +
"selectchange.created_by, selectchange.changed_on, selectchange.changed_by, user_name_created, user_name_changed, user_name_changed, p.nzp_page||' '||p.page_name AS page_name " +
"FROM selectchange LEFT OUTER JOIN " + Tables.pages + " p ON (selectchange.cur_page=p.nzp_page) WHERE selectchange.cur_page=" + Position + " ORDER BY nzp_ash ;";
        }

        public override void CreateColumns()
        {
            var actions_type = "SELECT id, id||' '||type_name AS text FROM action_types"; 
            columnCollection = new DataGridViewColumn[16];

            columnCollection[15] = CreateDataGridViewColumn.CreateComboBoxColumn("Для скрипта");
            columnCollection[14] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.actions_show, "user_name_changed", Resources.user_name_chang, true, true);
            columnCollection[13] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.actions_show, "changed_by", Resources.changed_by, true, false);
            columnCollection[12] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.actions_show, "changed_on", Resources.changed_on, true, true);
            columnCollection[11] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.actions_show, "user_name_created", Resources.user_name_creat, true, true);
            columnCollection[10] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.actions_show, "created_by", Resources.created_by, true, false);
            columnCollection[9] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.actions_show, "created_on", Resources.created_on, true, true);
            columnCollection[8] = CreateDataGridViewColumn.CreateCheckBoxColumn(Tables.actions_show, "is_dev", Resources.is_dev);
            columnCollection[7] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.actions_show, "sort_kod", Resources.sort_kod, false, true);
            columnCollection[6] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.actions_show, "act_tip", Resources.sort_kod, true, false);
            columnCollection[5] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.actions_show, "act_dd", Resources.sort_kod, true, false);
            columnCollection[4] = CreateDataGridViewColumn.CreateComboBoxColumn<string>(Resources.actions_showTable_actions_type_id, "action_types_id");
            var actionType = new ComboBoxBindingSource(actions_type,columnCollection[4], true);
            actionType.update(true);
            columnCollection[3] = CreateDataGridViewColumn.CreateComboBoxColumn<string>(Resources.actions_showTable_nzp_act, "nzp_act");
            nzpActList.Add(new ComboBoxBindingSource(columnCollection[3], true));
            columnCollection[2] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.actions_show, "page_name", "Наименование страницы", true, false);
            columnCollection[1] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.actions_show, "cur_page", "Текущая страница", true, false);
            columnCollection[0] = CreateDataGridViewColumn.CreateTextBoxColumn(Tables.actions_show, "nzp_ash", Resources.actions_showTable_id, true, true);
            columnCollection[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
        }

        public override bool Save(NpgsqlConnection connect, NpgsqlDataAdapter adapter, DataTable dt)
        {
            adapter.DeleteCommand = new NpgsqlCommand("Delete from " + Tables.actions_show + "  Where nzp_ash=@nzp_ash", connect);
            NpgsqlParameter parameter = adapter.DeleteCommand.Parameters.Add("@nzp_ash", NpgsqlTypes.NpgsqlDbType.Integer);
            parameter.SourceColumn = "nzp_ash";
            parameter.SourceVersion = DataRowVersion.Original;

            //nzp_ash=@nzp_ash,  nzp_ash=@nzp_ash, where nzp_ash=@nzp_ash
            adapter.UpdateCommand = new NpgsqlCommand("Update " + Tables.actions_show + " SET  cur_page=@cur_page, nzp_act=@nzp_act, sort_kod=@sort_kod, is_dev=@is_dev, action_types_id=@action_types_id, act_tip=@act_tip, act_dd=@act_dd, " +
            "created_on=@created_on, created_by=@created_by, changed_on=@changed_on, changed_by=@changed_by where nzp_ash=@nzp_ash", connect);
            parameter = adapter.UpdateCommand.Parameters.Add("@nzp_ash", NpgsqlTypes.NpgsqlDbType.Integer);
            parameter.SourceColumn = "nzp_ash";
            parameter = adapter.UpdateCommand.Parameters.Add("@cur_page", NpgsqlTypes.NpgsqlDbType.Integer);
            parameter.SourceColumn = "cur_page";
            parameter = adapter.UpdateCommand.Parameters.Add("@nzp_act", NpgsqlTypes.NpgsqlDbType.Integer);
            parameter.SourceColumn = "nzp_act";
            parameter = adapter.UpdateCommand.Parameters.Add("@sort_kod", NpgsqlTypes.NpgsqlDbType.Integer);
            parameter.SourceColumn = "sort_kod";
            parameter = adapter.UpdateCommand.Parameters.Add("@is_dev", NpgsqlTypes.NpgsqlDbType.Integer);
            parameter.SourceColumn = "is_dev";
            parameter = adapter.UpdateCommand.Parameters.Add("@action_types_id", NpgsqlTypes.NpgsqlDbType.Integer);
            parameter.SourceColumn = "action_types_id";
            parameter = adapter.UpdateCommand.Parameters.Add("@act_tip", NpgsqlTypes.NpgsqlDbType.Integer);
            parameter.SourceColumn = "act_tip";
            parameter = adapter.UpdateCommand.Parameters.Add("@act_dd", NpgsqlTypes.NpgsqlDbType.Integer);
            parameter.SourceColumn = "act_dd";
            parameter = adapter.UpdateCommand.Parameters.Add("@created_on", NpgsqlTypes.NpgsqlDbType.Timestamp);
            parameter.SourceColumn = "created_on";
            parameter = adapter.UpdateCommand.Parameters.Add("@created_by", NpgsqlTypes.NpgsqlDbType.Integer);
            parameter.SourceColumn = "created_by";
            parameter = adapter.UpdateCommand.Parameters.Add("@changed_on", NpgsqlTypes.NpgsqlDbType.Timestamp);
            parameter.SourceColumn = "changed_on";
            parameter = adapter.UpdateCommand.Parameters.Add("@changed_by", NpgsqlTypes.NpgsqlDbType.Integer);
            parameter.SourceColumn = "changed_by";

            adapter.InsertCommand = new NpgsqlCommand("Insert into " + Tables.actions_show + " ( cur_page, nzp_act, sort_kod, is_dev, created_on, created_by, changed_on, changed_by, action_types_id, act_tip, act_dd ) " +
           "Values (   @cur_page, @nzp_act, @sort_kod, @is_dev, @created_on, @created_by, @changed_on, @changed_by, @action_types_id,  @act_tip, @act_dd ); ", connect);
           //paramter = adapter.InsertCommand.Parameters.Add("@nzp_ash", NpgsqlTypes.NpgsqlDbType.Integer);
           //paramter.SourceColumn = "nzp_ash";
            parameter = adapter.InsertCommand.Parameters.Add("@cur_page", NpgsqlTypes.NpgsqlDbType.Integer);
            parameter.SourceColumn = "cur_page";
            parameter.SourceColumn = "cur_page";
            parameter = adapter.InsertCommand.Parameters.Add("@nzp_act", NpgsqlTypes.NpgsqlDbType.Integer);
            parameter.SourceColumn = "nzp_act";
            parameter = adapter.InsertCommand.Parameters.Add("@sort_kod", NpgsqlTypes.NpgsqlDbType.Integer);
            parameter.SourceColumn = "sort_kod";
            parameter = adapter.InsertCommand.Parameters.Add("@is_dev", NpgsqlTypes.NpgsqlDbType.Integer);
            parameter.SourceColumn = "is_dev";
            parameter = adapter.InsertCommand.Parameters.Add("@act_tip", NpgsqlTypes.NpgsqlDbType.Integer);
            parameter.SourceColumn = "act_tip";
            parameter = adapter.InsertCommand.Parameters.Add("@act_dd", NpgsqlTypes.NpgsqlDbType.Integer);
            parameter.SourceColumn = "act_dd";
            parameter = adapter.InsertCommand.Parameters.Add("@action_types_id", NpgsqlTypes.NpgsqlDbType.Integer);
            parameter.SourceColumn = "action_types_id";
            parameter = adapter.InsertCommand.Parameters.Add("@created_on", NpgsqlTypes.NpgsqlDbType.Timestamp);
            parameter.SourceColumn = "created_on";
            parameter = adapter.InsertCommand.Parameters.Add("@created_by", NpgsqlTypes.NpgsqlDbType.Integer);
            parameter.SourceColumn = "created_by";
            parameter = adapter.InsertCommand.Parameters.Add("@changed_on", NpgsqlTypes.NpgsqlDbType.Timestamp);
            parameter.SourceColumn = "changed_on";
            parameter = adapter.InsertCommand.Parameters.Add("@changed_by", NpgsqlTypes.NpgsqlDbType.Integer);
            parameter.SourceColumn = "changed_by";
            if (!TransferData.TransferDataDb.UpdateDbTable(adapter, dt)) return false;
            // Этот запрос сортирует отчеты на странице 133, 135 по имени.
            Returns ret= new Returns(true,"");
            string str =
                "with acs as (select distinct a.nzp_ash, a.sort_kod, a.cur_page, a.nzp_act as a_nzp_act, act_tip, act_dd, s.nzp_act as sact_nap_act, s.act_name as name_rep " +
                "from " + Tables.actions_show + " a Left outer join " + Tables.s_actions + " s on (a.nzp_act=s.nzp_act) " +
                " where (act_tip=2 and act_dd=5 and  (cur_page=135 OR cur_page=133))), " +
                " ord as (select *,(select count(1) from  acs ab where ab.name_rep <=acs.name_rep) as row_num " +
                " from  acs order by row_num) " +
                " update actions_show ah set sort_kod=ord.row_num from ord where ord.nzp_ash=ah.nzp_ash; ";
            TransferData.TransferDataDb.ExecuteScalar(str, out ret );
            if (!ret.Result)
                MessageBox.Show("Ошибка при сохрании записи" + ret.SqlError);
            NotifyObservers();
            return true;
        }
        public override string NameParentTable
        {
            get { return nameParentTreeViewNode; }
        }

        public override string NameOwnBaseColumn
        {
            get { return "cur_page"; }
        }

        public override void GetCorrespondTable(DataSet dataSet, NpgsqlDataAdapter adapter,  NpgsqlConnection connect, int positionParam )
        {

              selectCommand = "WITH selectcreate as (SELECT nzp_ash, cur_page, nzp_act, sort_kod, is_dev, action_types_id, act_tip, act_dd, created_on, created_by, " +
                              "changed_on, changed_by, user_name as user_name_created "+
"FROM " + Tables.actions_show + " a LEFT OUTER JOIN " + Tables.users + " u ON (a.created_by=u.id)), " +
"selectchange AS (SELECT nzp_ash, cur_page, nzp_act, sort_kod, is_dev, action_types_id, act_tip, act_dd, created_on, created_by, changed_on, " +
                              "changed_by, user_name_created, user_name as user_name_changed " +
"FROM selectcreate LEFT OUTER JOIN " + Tables.users + " ON (changed_by=id)) " +
"SELECT nzp_ash, cur_page, selectchange.nzp_act, selectchange.sort_kod, selectchange.is_dev, action_types_id, selectchange.act_tip, selectchange.act_dd, selectchange.created_on, " + 
"selectchange.created_by, selectchange.changed_on, selectchange.changed_by, user_name_created, user_name_changed, user_name_changed, p.nzp_page||' '||p.page_name AS page_name "+
"FROM selectchange LEFT OUTER JOIN " + Tables.pages + " p ON (selectchange.cur_page=p.nzp_page) WHERE selectchange.cur_page=" + positionParam + " ORDER BY nzp_ash ;";
                dataSet.Tables[TableName].Clear();
                adapter.SelectCommand.CommandText = selectCommand;
                TransferDataDb.Fill(adapter, dataSet.Tables[TableName]);
       
        }

        public override void SetDefaultValuesAfterRowAdded(DataGridView dgv, int index)
        {
            dgv.Rows[index].Cells["is_dev"].Value = 1;
            dgv.Rows[index].Cells["cur_page"].Value = position;
            dgv.Rows[index].Cells["nzp_act"].Value = DBNull.Value;
            dgv.Rows[index].Cells["action_types_id"].Value = DBNull.Value;
            dgv.Rows[index].Cells["created_on"].Value = DateTime.Now;
            dgv.Rows[index].Cells["created_by"].Value = MainWindow.UserId;
            dgv.Rows[index].Cells["user_name_created"].Value = MainWindow.UserName;
            dgv.Rows[index].Cells["changed_by"].Value = DBNull.Value;
        }

        public override string NameParentColumn
        {
            get { return "nzp_page"; }
        }

        public override void SetDefaultValuesAfterCellChange(DataGridView dgv, int row, int column)
        {
            var dataGridViewColumn = dgv.Columns["forscript"];
            if (dataGridViewColumn != null && dataGridViewColumn.Index == column)
            {
                // ключ значения для вставки
                Dictionary<string, string> valColumns = new Dictionary<string, string>
                {
                     {"sort_kod", GenerateScript.GetCellValue(dgv.Rows[row].Cells["sort_kod"])},
                      {"act_tip", GenerateScript.GetCellValue(dgv.Rows[row].Cells["act_tip"])},
                      {"act_dd", GenerateScript.GetCellValue(dgv.Rows[row].Cells["act_dd"])}
                };
                // ключ значение базовой колонки
                Dictionary<string, int> idColVal = new Dictionary<string, int>
                {
                    { "nzp_ash", (int)dgv.Rows[row].Cells["nzp_ash"].Value }
                };
                // ключ значение для where
                Dictionary<string, string> whereDict = new Dictionary<string, string> 
                {{"cur_page", dgv.Rows[row].Cells["cur_page"].Value.ToString()},
                {"nzp_act", dgv.Rows[row].Cells["nzp_act"].Value.ToString()}};
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
                             valColumns.Add("nzp_act", GenerateScript.GetCellValue(dgv.Rows[row].Cells["nzp_act"]));
                            break;
                        case ScriptGenerateOper.Update:

                            break;
                        case ScriptGenerateOper.Delete:
                            break;
                    }
                    GenerateScript.AddVal(TableName, valColumns, whereDict, idColVal, typescr, (int)dgv.Rows[row].Cells["cur_page"].Value);
                return;
            }

            var dataGridViewCol = dgv.Columns["action_types_id"];
            if (dataGridViewCol == null || column != dataGridViewCol.Index) return;
            var actTypeId = dgv.Rows[row].Cells["action_types_id"].Value as int?;
            if (actTypeId == null) return;
            switch (actTypeId)
            {
                case 0:
                    dgv.Rows[row].Cells["act_tip"].Value = 3;
                    dgv.Rows[row].Cells["act_dd"].Value = 0;
                    break;
                case 1:
                    dgv.Rows[row].Cells["act_tip"].Value = 0;
                    dgv.Rows[row].Cells["act_dd"].Value = 0;
                    break;
                case 2:
                    dgv.Rows[row].Cells["act_tip"].Value = 1;
                    dgv.Rows[row].Cells["act_dd"].Value = 1;
                    break;
                case 3:
                    dgv.Rows[row].Cells["act_tip"].Value = 2;
                    dgv.Rows[row].Cells["act_dd"].Value = 1;
                    break;
                case 4:
                    dgv.Rows[row].Cells["act_tip"].Value = 2;
                    dgv.Rows[row].Cells["act_dd"].Value = 2;
                    break;
                case 5:
                    dgv.Rows[row].Cells["act_tip"].Value = 2;
                    dgv.Rows[row].Cells["act_dd"].Value = 3;
                    break;
                case 6:
                    dgv.Rows[row].Cells["act_tip"].Value = 2;
                    dgv.Rows[row].Cells["act_dd"].Value = 4;
                    break;
                case 7:
                    dgv.Rows[row].Cells["act_tip"].Value = 2;
                    dgv.Rows[row].Cells["act_dd"].Value = 5;
                    break;
                case 8:
                    dgv.Rows[row].Cells["act_tip"].Value = 2;
                    dgv.Rows[row].Cells["act_dd"].Value = 6;
                    break;
            }

            dgv.Rows[row].Cells["user_name_changed"].Value = MainWindow.UserName;
            dgv.Rows[row].Cells["changed_on"].Value = DateTime.Now;
            dgv.Rows[row].Cells["changed_by"].Value = MainWindow.UserId;
        }

        public void AddObservers(List<IObserver> obsrvs)
        {
            observers.AddRange(obsrvs);
        }

        public void NotifyObservers()
        {
            var dt = ComboBoxBindingSource.Update("SELECT nzp_page AS id, nzp_page ||' '|| page_name  AS text FROM " + Tables.pages + " ORDER BY id;");
            for (int i = 0; i < observers.Count; i++)
            {
                if (!observers[i].AdditionalContition)
                    observers[i].update(dt);
                else
                    observers[i].update(ComboBoxBindingSource.Update(observers[i].AdditionalContotionString));
            }
        }
    }
}
