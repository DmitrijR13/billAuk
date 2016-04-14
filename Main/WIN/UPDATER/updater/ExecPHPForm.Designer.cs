namespace updater
{
    partial class ExecPHPForm
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
            this.tabctrlPHP = new System.Windows.Forms.TabControl();
            this.tabpg1 = new System.Windows.Forms.TabPage();
            this.btnExecPHP = new System.Windows.Forms.Button();
            this.dgvPHPRajons = new System.Windows.Forms.DataGridView();
            this.colNumber = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colIP = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStatus = new System.Windows.Forms.ToolStripStatusLabel();
            this.tabctrlPHP.SuspendLayout();
            this.tabpg1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvPHPRajons)).BeginInit();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabctrlPHP
            // 
            this.tabctrlPHP.Controls.Add(this.tabpg1);
            this.tabctrlPHP.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabctrlPHP.Location = new System.Drawing.Point(0, 0);
            this.tabctrlPHP.Name = "tabctrlPHP";
            this.tabctrlPHP.SelectedIndex = 0;
            this.tabctrlPHP.Size = new System.Drawing.Size(1243, 413);
            this.tabctrlPHP.TabIndex = 0;
            // 
            // tabpg1
            // 
            this.tabpg1.Controls.Add(this.statusStrip1);
            this.tabpg1.Controls.Add(this.btnExecPHP);
            this.tabpg1.Controls.Add(this.dgvPHPRajons);
            this.tabpg1.Location = new System.Drawing.Point(4, 22);
            this.tabpg1.Name = "tabpg1";
            this.tabpg1.Padding = new System.Windows.Forms.Padding(3);
            this.tabpg1.Size = new System.Drawing.Size(1235, 387);
            this.tabpg1.TabIndex = 0;
            this.tabpg1.Text = "Доступные серверы";
            this.tabpg1.UseVisualStyleBackColor = true;
            // 
            // btnExecPHP
            // 
            this.btnExecPHP.Location = new System.Drawing.Point(8, 324);
            this.btnExecPHP.Name = "btnExecPHP";
            this.btnExecPHP.Size = new System.Drawing.Size(200, 33);
            this.btnExecPHP.TabIndex = 1;
            this.btnExecPHP.Text = "Выполнить скрипты";
            this.btnExecPHP.UseVisualStyleBackColor = true;
            this.btnExecPHP.Click += new System.EventHandler(this.btnExecPHP_Click);
            // 
            // dgvPHPRajons
            // 
            this.dgvPHPRajons.AllowUserToAddRows = false;
            this.dgvPHPRajons.AllowUserToDeleteRows = false;
            this.dgvPHPRajons.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
            this.dgvPHPRajons.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
            this.dgvPHPRajons.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvPHPRajons.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colNumber,
            this.colName,
            this.colIP});
            this.dgvPHPRajons.Location = new System.Drawing.Point(8, 6);
            this.dgvPHPRajons.Name = "dgvPHPRajons";
            this.dgvPHPRajons.ReadOnly = true;
            this.dgvPHPRajons.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvPHPRajons.Size = new System.Drawing.Size(1221, 312);
            this.dgvPHPRajons.TabIndex = 0;
            // 
            // colNumber
            // 
            this.colNumber.HeaderText = "Номер";
            this.colNumber.Name = "colNumber";
            this.colNumber.ReadOnly = true;
            this.colNumber.Width = 66;
            // 
            // colName
            // 
            this.colName.HeaderText = "Название";
            this.colName.Name = "colName";
            this.colName.ReadOnly = true;
            this.colName.Width = 82;
            // 
            // colIP
            // 
            this.colIP.HeaderText = "Адрес";
            this.colIP.Name = "colIP";
            this.colIP.ReadOnly = true;
            this.colIP.Width = 63;
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStatus});
            this.statusStrip1.Location = new System.Drawing.Point(3, 362);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(1229, 22);
            this.statusStrip1.TabIndex = 2;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // toolStatus
            // 
            this.toolStatus.Name = "toolStatus";
            this.toolStatus.Size = new System.Drawing.Size(0, 17);
            // 
            // ExecPHPForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1243, 413);
            this.Controls.Add(this.tabctrlPHP);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ExecPHPForm";
            this.Text = "ExecPHP";
            this.tabctrlPHP.ResumeLayout(false);
            this.tabpg1.ResumeLayout(false);
            this.tabpg1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvPHPRajons)).EndInit();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabctrlPHP;
        private System.Windows.Forms.TabPage tabpg1;
        private System.Windows.Forms.DataGridView dgvPHPRajons;
        private System.Windows.Forms.Button btnExecPHP;
        private System.Windows.Forms.DataGridViewTextBoxColumn colNumber;
        private System.Windows.Forms.DataGridViewTextBoxColumn colName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colIP;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel toolStatus;
    }
}