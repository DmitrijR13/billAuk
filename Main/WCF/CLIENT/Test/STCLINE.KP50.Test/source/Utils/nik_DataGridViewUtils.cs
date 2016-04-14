using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace STCLine.Utils
{
    public class ClassNikDataGridViewUtils
    {
        static public void SetDefaultStyle(DataGridView dgv)
        {
            System.Windows.Forms.DataGridViewCellStyle dgvHeadStyle = new System.Windows.Forms.DataGridViewCellStyle()
            {
                Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter,
                BackColor = System.Drawing.SystemColors.Control,
                Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204))),
                ForeColor = System.Drawing.SystemColors.WindowText,
                SelectionBackColor = System.Drawing.SystemColors.Highlight,
                SelectionForeColor = System.Drawing.SystemColors.HighlightText,
                WrapMode = System.Windows.Forms.DataGridViewTriState.True
            };

            System.Windows.Forms.DataGridViewCellStyle dgvCellStyle = new System.Windows.Forms.DataGridViewCellStyle()
            {
                Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft,
                BackColor = System.Drawing.SystemColors.Window,
                Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204))),
                ForeColor = System.Drawing.SystemColors.WindowText,
                SelectionBackColor = System.Drawing.SystemColors.Highlight,
                SelectionForeColor = System.Drawing.SystemColors.HighlightText,
                WrapMode = System.Windows.Forms.DataGridViewTriState.False
            };

            dgv.AutoGenerateColumns = false;
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;
            dgv.BackgroundColor = System.Drawing.SystemColors.Window;
            dgv.ColumnHeadersDefaultCellStyle = dgvHeadStyle;
            dgv.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgv.DefaultCellStyle = dgvCellStyle;
            dgv.MultiSelect = false;
            dgv.ReadOnly = true;
            dgv.RowHeadersVisible = false;
            dgv.RowTemplate.DefaultCellStyle.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            dgv.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            dgv.BorderStyle = BorderStyle.None;
        }
        static public void InitializeDataGridView(DataGridView dgv, DataGridViewColumn[] cols, ContextMenuStrip contextMenu)
        {
            InitializeDataGridView(dgv, cols, contextMenu, null);
        }
        static public void InitializeDataGridView(DataGridView dgv, DataGridViewColumn[] cols, ContextMenuStrip contextMenu, Control parentControl)
        {
            if (cols != null)
            {
                dgv.Columns.AddRange(cols);
            }
            SetDefaultStyle(dgv);
            dgv.ContextMenuStrip = contextMenu;
            if (parentControl != null)
            {
                parentControl.Controls.Add(dgv);
            }
        }
        /// <summary>Обработчик события форматирования строк DataGridView. 
        /// Веделяет цветом выбранные строки</summary>
        /// <param name="sender">Объект типа дата DataGridView, если другой тип объекта, то исключение.
        /// Строки таблицы должны отображать или DataRow  с колонкой bool is_marked, или
        /// объекты поддерживающие интерфейс ClassNikDataGridViewUtils.IMarkedObject
        /// </param>
        /// <param name="e">Аргументы, через</param>
        static public void dgvRequestTranslist_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (sender is DataGridView)
            {
                DataGridView dgv = (sender as DataGridView);

                bool _is_marked = false;
                if (dgv.Rows[e.RowIndex].DataBoundItem is DataRowView)
                {
                    DataRow dr = (dgv.Rows[e.RowIndex].DataBoundItem as DataRowView).Row;

                    _is_marked = (dr["is_marked"] != DBNull.Value ? dr.Field<bool>("is_marked") : false);
                }
                else if (dgv.Rows[e.RowIndex].DataBoundItem is IIsMarkedObject)
                {
                    Nullable<bool> _IsMarked = (dgv.Rows[e.RowIndex].DataBoundItem as IIsMarkedObject).IsMarked();
                    _is_marked = (_IsMarked != null ? (bool)(_IsMarked) : false);

                }
                if (_is_marked)
                {
                    e.CellStyle.ForeColor = Color.Blue;
                    e.CellStyle.SelectionForeColor = Color.FromArgb(204, 255, 255);
                }
            }
            else
            {
                throw new Exception("Параметр sender должен иметь тип DataGridView");
            }
        }

        interface IIsMarkedObject
        {
            Nullable<bool> IsMarked();
        }

        public class TextBoxColumn : System.Windows.Forms.DataGridViewTextBoxColumn
        {
            public TextBoxColumn() : base() { }
            public TextBoxColumn(string name, string dataPropertyName, string headerText, int width,
                System.Windows.Forms.DataGridViewContentAlignment alignment)
                : this()
            {
                this.Name = name;
                this.DataPropertyName = dataPropertyName;
                this.HeaderText = headerText;
                this.Width = width;

                this.DefaultCellStyle = new System.Windows.Forms.DataGridViewCellStyle() { Alignment = alignment };
                this.SortMode = DataGridViewColumnSortMode.NotSortable;
                this.ReadOnly = true;
            }
            public TextBoxColumn(string name, string dataPropertyName, string headerText, int width)
                : this(name, dataPropertyName, headerText, width, System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft) { }
        }
        public class CheckBoxColumn : System.Windows.Forms.DataGridViewCheckBoxColumn
        {
            public CheckBoxColumn() : base() { }
            public CheckBoxColumn(string name, string dataPropertyName, string headerText, int width,
                System.Windows.Forms.DataGridViewContentAlignment alignment)
                : this()
            {
                this.Name = name;
                this.DataPropertyName = dataPropertyName;
                this.HeaderText = headerText;
                this.Width = width;

                this.DefaultCellStyle = new System.Windows.Forms.DataGridViewCellStyle() { Alignment = alignment };
                this.SortMode = DataGridViewColumnSortMode.NotSortable;
                this.ReadOnly = true;
            }
            public CheckBoxColumn(string name, string dataPropertyName, string headerText, int width)
                : this(name, dataPropertyName, headerText, width, System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft) { }
        }


        /// <summary>Переносит в начало в порядке указанном col_names.
        /// Остальные колонки не рассматриваются
        /// </summary>
        static public void OrderColumns(DataGridView dgv, string[] col_names)
        {
            int i = -1;
            foreach (string cn in col_names)
            {
                i++;
                dgv.Columns[cn].DisplayIndex = i;                
            }
            dgv.Refresh();
        }
    }
}
