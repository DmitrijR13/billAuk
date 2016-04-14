namespace STCLINE.KP50.HostMan.SOURCE
{
    partial class FrmBasePointsManager
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.button1 = new System.Windows.Forms.Button();
            this.comboBoxBase = new System.Windows.Forms.ComboBox();
            this.labelGroupSelect = new System.Windows.Forms.Label();
            this.gridBaseList = new System.Windows.Forms.DataGridView();
            this.ColumnPeriod = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ColumnType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ColumnName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ColumnComment = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ColumnID = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.label1 = new System.Windows.Forms.Label();
            this.btnNewBase = new System.Windows.Forms.Button();
            this.btnEdit = new System.Windows.Forms.Button();
            this.btnDelete = new System.Windows.Forms.Button();
            this.btnNewBank = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.gridBaseList)).BeginInit();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.button1.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.button1.Location = new System.Drawing.Point(792, 463);
            this.button1.Margin = new System.Windows.Forms.Padding(4);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(112, 44);
            this.button1.TabIndex = 0;
            this.button1.Text = "Закрыть";
            this.button1.UseVisualStyleBackColor = true;
            // 
            // comboBoxBase
            // 
            this.comboBoxBase.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.comboBoxBase.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxBase.FormattingEnabled = true;
            this.comboBoxBase.Location = new System.Drawing.Point(120, 15);
            this.comboBoxBase.Margin = new System.Windows.Forms.Padding(4);
            this.comboBoxBase.Name = "comboBoxBase";
            this.comboBoxBase.Size = new System.Drawing.Size(783, 24);
            this.comboBoxBase.TabIndex = 8;
            this.comboBoxBase.SelectedIndexChanged += new System.EventHandler(this.comboBoxBank_SelectedIndexChanged);
            // 
            // labelGroupSelect
            // 
            this.labelGroupSelect.AutoSize = true;
            this.labelGroupSelect.Location = new System.Drawing.Point(12, 18);
            this.labelGroupSelect.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelGroupSelect.Name = "labelGroupSelect";
            this.labelGroupSelect.Size = new System.Drawing.Size(96, 17);
            this.labelGroupSelect.TabIndex = 7;
            this.labelGroupSelect.Text = "Банк данных:";
            // 
            // gridBaseList
            // 
            this.gridBaseList.AllowUserToAddRows = false;
            this.gridBaseList.AllowUserToDeleteRows = false;
            this.gridBaseList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gridBaseList.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridBaseList.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.ColumnPeriod,
            this.ColumnType,
            this.ColumnName,
            this.ColumnComment,
            this.ColumnID});
            this.gridBaseList.Enabled = false;
            this.gridBaseList.Location = new System.Drawing.Point(16, 104);
            this.gridBaseList.Margin = new System.Windows.Forms.Padding(4);
            this.gridBaseList.MultiSelect = false;
            this.gridBaseList.Name = "gridBaseList";
            this.gridBaseList.ReadOnly = true;
            this.gridBaseList.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridBaseList.Size = new System.Drawing.Size(888, 351);
            this.gridBaseList.TabIndex = 6;
            this.gridBaseList.SelectionChanged += new System.EventHandler(this.gridBaseList_SelectionChanged);
            // 
            // ColumnPeriod
            // 
            this.ColumnPeriod.HeaderText = "Период";
            this.ColumnPeriod.Name = "ColumnPeriod";
            this.ColumnPeriod.ReadOnly = true;
            this.ColumnPeriod.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            this.ColumnPeriod.Width = 60;
            // 
            // ColumnType
            // 
            this.ColumnType.HeaderText = "Тип";
            this.ColumnType.Name = "ColumnType";
            this.ColumnType.ReadOnly = true;
            // 
            // ColumnName
            // 
            this.ColumnName.HeaderText = "Наименование";
            this.ColumnName.Name = "ColumnName";
            this.ColumnName.ReadOnly = true;
            this.ColumnName.Width = 200;
            // 
            // ColumnComment
            // 
            this.ColumnComment.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.ColumnComment.HeaderText = "Примечание";
            this.ColumnComment.Name = "ColumnComment";
            this.ColumnComment.ReadOnly = true;
            // 
            // ColumnID
            // 
            this.ColumnID.HeaderText = "ColumnID";
            this.ColumnID.Name = "ColumnID";
            this.ColumnID.ReadOnly = true;
            this.ColumnID.Visible = false;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 60);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(132, 17);
            this.label1.TabIndex = 10;
            this.label1.Text = "Разделы/периоды:";
            this.label1.Visible = false;
            // 
            // btnNewBase
            // 
            this.btnNewBase.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnNewBase.Location = new System.Drawing.Point(667, 54);
            this.btnNewBase.Margin = new System.Windows.Forms.Padding(4);
            this.btnNewBase.Name = "btnNewBase";
            this.btnNewBase.Size = new System.Drawing.Size(236, 42);
            this.btnNewBase.TabIndex = 11;
            this.btnNewBase.Text = "Копировать в новый период";
            this.btnNewBase.UseVisualStyleBackColor = true;
            this.btnNewBase.Click += new System.EventHandler(this.btnNewBase_Click);
            // 
            // btnEdit
            // 
            this.btnEdit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnEdit.Location = new System.Drawing.Point(179, 54);
            this.btnEdit.Margin = new System.Windows.Forms.Padding(4);
            this.btnEdit.Name = "btnEdit";
            this.btnEdit.Size = new System.Drawing.Size(236, 42);
            this.btnEdit.TabIndex = 12;
            this.btnEdit.Text = "Изменить название";
            this.btnEdit.UseVisualStyleBackColor = true;
            this.btnEdit.Click += new System.EventHandler(this.btnEdit_Click);
            // 
            // btnDelete
            // 
            this.btnDelete.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnDelete.Location = new System.Drawing.Point(423, 54);
            this.btnDelete.Margin = new System.Windows.Forms.Padding(4);
            this.btnDelete.Name = "btnDelete";
            this.btnDelete.Size = new System.Drawing.Size(236, 42);
            this.btnDelete.TabIndex = 13;
            this.btnDelete.Text = "Удалить период";
            this.btnDelete.UseVisualStyleBackColor = true;
            this.btnDelete.Click += new System.EventHandler(this.btnDelete_Click);
            // 
            // btnNewBank
            // 
            this.btnNewBank.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnNewBank.Enabled = false;
            this.btnNewBank.Location = new System.Drawing.Point(16, 463);
            this.btnNewBank.Margin = new System.Windows.Forms.Padding(4);
            this.btnNewBank.Name = "btnNewBank";
            this.btnNewBank.Size = new System.Drawing.Size(231, 44);
            this.btnNewBank.TabIndex = 14;
            this.btnNewBank.Text = "Копировать в новый банк";
            this.btnNewBank.UseVisualStyleBackColor = true;
            this.btnNewBank.Click += new System.EventHandler(this.btnNewBank_Click);
            // 
            // FrmBasePointsManager
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.button1;
            this.ClientSize = new System.Drawing.Size(920, 522);
            this.Controls.Add(this.btnNewBank);
            this.Controls.Add(this.btnDelete);
            this.Controls.Add(this.btnEdit);
            this.Controls.Add(this.btnNewBase);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.comboBoxBase);
            this.Controls.Add(this.labelGroupSelect);
            this.Controls.Add(this.gridBaseList);
            this.Controls.Add(this.button1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Margin = new System.Windows.Forms.Padding(4);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmBasePointsManager";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Управление банками данных";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.FrmBankPointsManager_FormClosed);
            this.Load += new System.EventHandler(this.FrmBankPointsManager_Load);
            ((System.ComponentModel.ISupportInitialize)(this.gridBaseList)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.ComboBox comboBoxBase;
        private System.Windows.Forms.Label labelGroupSelect;
        private System.Windows.Forms.DataGridView gridBaseList;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnNewBase;
        private System.Windows.Forms.DataGridViewTextBoxColumn ColumnPeriod;
        private System.Windows.Forms.DataGridViewTextBoxColumn ColumnType;
        private System.Windows.Forms.DataGridViewTextBoxColumn ColumnName;
        private System.Windows.Forms.DataGridViewTextBoxColumn ColumnComment;
        private System.Windows.Forms.DataGridViewTextBoxColumn ColumnID;
        private System.Windows.Forms.Button btnEdit;
        private System.Windows.Forms.Button btnDelete;
        private System.Windows.Forms.Button btnNewBank;
    }
}