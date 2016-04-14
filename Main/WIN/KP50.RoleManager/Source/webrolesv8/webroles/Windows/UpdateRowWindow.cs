using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using webroles.GenerateScriptTable;
using webroles.Properties;
using webroles.TablesFirstLevel;

namespace webroles.Windows
{
    public partial class UpdateRowWindow : Form
    {
        public ChangedRow UpdateChangedRow { get; private set; }
        private DataGridViewColumn[] columnCollection = new DataGridViewColumn[4];
        readonly BindingSource dataGridViewBinSour = new BindingSource();
        private DataTable dt;
        private string tableName;
        private int idNum;
        private string nameIdColumn;

        public UpdateRowWindow(Dictionary<string,string> columnsName, string tableName,Dictionary<string, int> idColValue)
        {
            InitializeComponent();
            this.tableName = tableName;
            dataGridView1.AutoGenerateColumns = false;
            Text = "Что будем обновлять?";
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.CausesValidation = true;
            string updateTable = "updateTable";
            dt = new DataTable(updateTable);
            dt.Columns.Add("check", typeof(bool));
            dt.Columns.Add("num", typeof (int));
            dt.Columns.Add("columnName", typeof(string));
            dt.Columns.Add("value", typeof(string));
            int i = 0;
            foreach (KeyValuePair<string, string> column in columnsName)
            {
              DataRow dr = dt.NewRow();
              dr.SetField("check", false);
              dr.SetField("num", i+1);
              dr.SetField("columnName", column.Key);
              dr.SetField("value", column.Value);
              dt.Rows.Add(dr);
            }
            dataGridViewBinSour.DataSource = dt;
            dataGridView1.DataSource = dataGridViewBinSour;

            columnCollection[0] = CreateDataGridViewColumn.CreateCheckBoxColumn(updateTable, "check", "");
            columnCollection[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
            columnCollection[1] = CreateDataGridViewColumn.CreateTextBoxColumn(updateTable, "num", "№", true, true);
            columnCollection[1].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
            columnCollection[2] = CreateDataGridViewColumn.CreateTextBoxColumn(updateTable, "columnName", "Наименование колонки", true, true);
            columnCollection[2].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
            columnCollection[3] = CreateDataGridViewColumn.CreateTextBoxColumn(updateTable, "value", "Значение", true, true);
            columnCollection[3].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            dataGridView1.Columns.AddRange(columnCollection);
            dt.AcceptChanges();
            this.idNum = idColValue.FirstOrDefault().Value;
            this.nameIdColumn = idColValue.FirstOrDefault().Key;
           // dataGridView1.DataSource = dt;
  
        }

     

        private void cancelButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void okButton_Click(object sender, EventArgs e)
        {
           
            var datTab = dt.AsEnumerable();
            EnumerableRowCollection<DataRow> rows = datTab.Select(row => row).Where(row => row.Field<bool>("check"));
            
            if (!rows.Any())
            {
                MessageBox.Show("Ничего не выбрано!");
                return;
            }
            UpdateChangedRow = new ChangedRow(tableName,  nameIdColumn,  idNum);
            UpdateChangedRow.State= DataRowState.Modified;
            foreach (DataRow dr in rows)
            {
                if (tableName==Tables.pages && dr.Field<string>("columnName")=="sort_kod")
                {
                    UpdateChangedRow.IsSortKodChanged = true;
                    continue;
                }
                UpdateChangedRow.ColValuesDictionary.Add(dr.Field<string>("columnName"), dr.Field<string>("value"));
            }
            DialogResult = DialogResult.OK;
        }

    }
}
