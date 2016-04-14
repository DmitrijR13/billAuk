using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace webroles
{
    /// <summary>
    /// Данный класс содержит методы создания колонок для объекта DataGridView
    /// </summary>
   public class CreateDataGridViewColumn
    {
        
        /// <summary>
        /// создает и добавляет TextBox колонку в DataGridView
        /// </summary>
        /// <param name="nameDataSetTable">название таблицы, определенной в объекте DataSet</param>
        /// <param name="nameColumnDataSetTable">название колонки</param>
        /// <param name="columnHeaderText">заголовок колонки</param>
        /// <param name="isReadonly">true - колонка только для чтения</param>
        public static DataGridViewTextBoxColumn CreateTextBoxColumn(string nameDataSetTable, string nameColumnDataSetTable, string columnHeaderText, bool isReadonly, bool isVisible)
        {
            DataGridViewTextBoxColumn textBoxColumn = new DataGridViewTextBoxColumn();
            textBoxColumn.DataPropertyName = nameColumnDataSetTable;
            textBoxColumn.Name = nameColumnDataSetTable;
            textBoxColumn.HeaderText = columnHeaderText;
            textBoxColumn.ReadOnly = isReadonly;
            textBoxColumn.Visible = isVisible;
            //textBoxColumn.
            //textBoxColumn.AutoSizeMode = mode;
           return textBoxColumn;
        }
        public static DataGridViewCheckBoxColumn CreateCheckBoxColumn(string nameDataSetTable, string nameColumnDataSetTable, string columnHeaderText, bool isReadOnly=false)
        {
            DataGridViewCheckBoxColumn chechBoxColumn = new DataGridViewCheckBoxColumn();
            chechBoxColumn.DataPropertyName = nameColumnDataSetTable;
            chechBoxColumn.Name = nameColumnDataSetTable;
            chechBoxColumn.HeaderText = columnHeaderText;
            chechBoxColumn.ReadOnly = isReadOnly;
            return chechBoxColumn;
        }
        /// <summary>
        /// создает и добавляет ComboBox колонку в DataGridView
        /// </summary>
        /// <param name="nameColumnDataSetTable">название колонки</param>
        /// <param name="columnHeaderText">заголовок колонки</param>
        /// <param name="valueMember">название колонки, относительно которой в списке comboBox будут отображаться значения определенные в displayMember</param>
        /// <param name="displayMember">название колонки, значения которой будут присвоены элементам ComboBox</param>
        /// <param name="bs">источник данных</param>
        /// <param name="dataPropertyName">название колонки, значения которой будут отображаться</param>

        public static DataGridViewComboBoxColumn CreateComboBoxColumn<T>(string columnHeaderText, string dataPropertyName)
        {
            DataGridViewComboBoxColumn comboBoxColumn = new DataGridViewComboBoxColumn();
            comboBoxColumn.DataPropertyName = dataPropertyName;
            comboBoxColumn.Name = dataPropertyName;
            comboBoxColumn.DisplayMember = "text";
            comboBoxColumn.ValueMember =  "id";
            comboBoxColumn.HeaderText = columnHeaderText;
            return comboBoxColumn;
        }

        public static DataGridViewComboBoxColumn CreateComboBoxColumn(string columnHeaderText, string dataPropertyName)
        {
            DataGridViewComboBoxColumn comboBoxColumn = new DataGridViewComboBoxColumn();
            comboBoxColumn.DataPropertyName = dataPropertyName;
            comboBoxColumn.Name = dataPropertyName;
            //comboBoxColumn.DisplayMember = "text";
            //comboBoxColumn.ValueMember = "id";
            comboBoxColumn.HeaderText = columnHeaderText;
            return comboBoxColumn;
        }


        public static DataGridViewComboBoxColumn CreateComboBoxColumn(string columnHeaderText)
        {
            string[] operName = { "Insert", "Update", "Delete", "none"};
            DataTable dt =new DataTable();
            dt.Columns.Add("id",typeof(int));
            dt.Columns.Add("text",typeof(string));
            for (int i = 0; i < operName.Length; i++)
            {
                DataRow dr = dt.NewRow();
                if (i != 3)
                {
                    dr.SetField("id", i + 1);
                }
                else
                {
                    dr.SetField("id", DBNull.Value);
                }
                dr.SetField("text",operName[i]);
                dt.Rows.Add(dr);
            }

            //DataGridViewComboBoxColumn comboBoxColumn = new DataGridViewComboBoxColumn();
            //comboBoxColumn.Name = "forscript";
            //comboBoxColumn.DataSource = Enum.GetValues(typeof(ScriptGenerateOper));
            //comboBoxColumn.ValueType = typeof (ScriptGenerateOper);
            //comboBoxColumn.HeaderText = columnHeaderText;
            //comboBoxColumn.DefaultCellStyle.NullValue = "none";
            //return comboBoxColumn;

            DataGridViewComboBoxColumn comboBoxColumn = new DataGridViewComboBoxColumn();
            comboBoxColumn.DataSource = dt;
           // comboBoxColumn.DataPropertyName = "forscr";
            comboBoxColumn.Name = "forscript";
            comboBoxColumn.DisplayMember = "text";
            comboBoxColumn.ValueMember = "id";
            comboBoxColumn.HeaderText = columnHeaderText;
            comboBoxColumn.DefaultCellStyle.NullValue = "none";
            return comboBoxColumn;
        }

        public static DataGridViewComboBoxColumn CreateComboBoxColumnPagesTableForScript(string columnHeaderText)
        {
            string[] operName = { "Insert", "Update", "Delete", "InsertWhole", "none"};
            DataTable dt = new DataTable();
            dt.Columns.Add("id", typeof(int));
            dt.Columns.Add("text", typeof(string));
            for (int i = 0; i < operName.Length; i++)
            {
                DataRow dr = dt.NewRow();
                if (i == 4)
                {
                    dr.SetField("id", DBNull.Value);
                }
                else
                {
                    dr.SetField("id", i + 1);
                }
                dr.SetField("text", operName[i]);
                dt.Rows.Add(dr);
            }

            //DataGridViewComboBoxColumn comboBoxColumn = new DataGridViewComboBoxColumn();
            //comboBoxColumn.Name = "forscript";
            //comboBoxColumn.DataSource = Enum.GetValues(typeof(ScriptGenerateOper));
            //comboBoxColumn.ValueType = typeof (ScriptGenerateOper);
            //comboBoxColumn.HeaderText = columnHeaderText;
            //comboBoxColumn.DefaultCellStyle.NullValue = "none";
            //return comboBoxColumn;

            DataGridViewComboBoxColumn comboBoxColumn = new DataGridViewComboBoxColumn();
            comboBoxColumn.DataSource = dt;
            // comboBoxColumn.DataPropertyName = "forscr";
            comboBoxColumn.Name = "forscript";
            comboBoxColumn.DisplayMember = "text";
            comboBoxColumn.ValueMember = "id";
            comboBoxColumn.HeaderText = columnHeaderText;
            comboBoxColumn.DefaultCellStyle.NullValue = "none";
            return comboBoxColumn;
        }
    }


}
