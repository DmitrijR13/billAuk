using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using webroles.GenerateScriptTable;

namespace webroles.Windows
{
    public partial class WatchGeneratedRowsWindow : Form
    {
        private DataGridViewColumn[] columnCollection = new DataGridViewColumn[6];
        public WatchGeneratedRowsWindow()
        {
            InitializeComponent();
            dataGridView1.AutoGenerateColumns = false;
            dataGridView1.AllowUserToAddRows = false;

            fillGridView();
        }

        private void selectAllButton_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < dataGridView1.Rows.Count; i++)
                dataGridView1.Rows[i].Cells["check"].Value = true;
        }

        private void unselectAllButton_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < dataGridView1.Rows.Count; i++)
                dataGridView1.Rows[i].Cells["check"].Value = false;
        }

        private void removeSelectedButton_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < dataGridView1.Rows.Count; i++)
            {
                if ((bool) dataGridView1.Rows[i].Cells["check"].Value)
                {
                    if (i == 0)
                    {
                        if (System.Windows.Forms.DialogResult.Cancel ==
                            MessageBox.Show("Вы уверены, что хотите удалить выделенные строки?",
                                "Подтверждение удаления", MessageBoxButtons.OKCancel, MessageBoxIcon.Question))
                       return;
                    }
                    GenerateScript.CheckOnSameData(dataGridView1.Rows[i].Cells["tableName"].Value.ToString(),
                        (int) dataGridView1.Rows[i].Cells["id"].Value);
                }
            }
            fillGridView();
        }

        private void fillGridView()
        {
            dataGridView1.Columns.Clear();
            List<WatchGeneratedRows> wgr = GenerateScript.GetGeneratedRows();
            string watchTable = "watchTable";
            DataTable dt = new DataTable(watchTable);
            dt.Columns.Add(new DataColumn("check", typeof(bool)));
            dt.Columns.Add(new DataColumn("num", typeof(int)));
            dt.Columns.Add(new DataColumn("scriptRow", typeof(string)));
            dt.Columns.Add(new DataColumn("comment", typeof(string)));
            dt.Columns.Add(new DataColumn("tableName", typeof(string)));
            dt.Columns.Add(new DataColumn("id", typeof(int)));


            for (int i = 0; i < wgr.Count; i++)
            {
                DataRow dr = dt.NewRow();
                dr.SetField("check", false);
                dr.SetField("num", i + 1);
                dr.SetField("scriptRow", wgr[i].ScriptRow);
                dr.SetField("tableName", wgr[i].TableName);
                dr.SetField("id", wgr[i].IdColumnValue);
                dr.SetField("comment", wgr[i].Comment);
                dt.Rows.Add(dr);
            }

            dataGridView1.DataSource = dt;

            columnCollection[0] = CreateDataGridViewColumn.CreateCheckBoxColumn(watchTable, "check", "");
            columnCollection[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
            columnCollection[1] = CreateDataGridViewColumn.CreateTextBoxColumn(watchTable, "num", "№", true, true);
            columnCollection[1].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
            columnCollection[2] = CreateDataGridViewColumn.CreateTextBoxColumn(watchTable, "scriptRow", "Строка скрипта", true, true);
            columnCollection[2].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
            columnCollection[3] = CreateDataGridViewColumn.CreateTextBoxColumn(watchTable, "comment", "Комментарий", true, true);
            columnCollection[3].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            columnCollection[4] = CreateDataGridViewColumn.CreateTextBoxColumn(watchTable, "id", "di", true, false);
            columnCollection[4].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
            columnCollection[5] = CreateDataGridViewColumn.CreateTextBoxColumn(watchTable, "tableName", "таблица", true, false);
            columnCollection[5].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dataGridView1.Columns.AddRange(columnCollection);
        }
    }
}
