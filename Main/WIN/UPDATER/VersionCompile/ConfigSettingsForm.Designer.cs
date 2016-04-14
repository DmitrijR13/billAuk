namespace VersionCompile
{
    partial class ConfigSettingsForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ConfigSettingsForm));
            this.tcConfigSet = new System.Windows.Forms.TabControl();
            this.tpHost = new System.Windows.Forms.TabPage();
            this.dgvHost = new System.Windows.Forms.DataGridView();
            this.prefHost = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.valueHost = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.tpWeb = new System.Windows.Forms.TabPage();
            this.dgvWeb = new System.Windows.Forms.DataGridView();
            this.prefWeb = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.valueWeb = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnOK = new System.Windows.Forms.Button();
            this.tcConfigSet.SuspendLayout();
            this.tpHost.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvHost)).BeginInit();
            this.tpWeb.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvWeb)).BeginInit();
            this.SuspendLayout();
            // 
            // tcConfigSet
            // 
            this.tcConfigSet.Controls.Add(this.tpHost);
            this.tcConfigSet.Controls.Add(this.tpWeb);
            this.tcConfigSet.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.tcConfigSet.Location = new System.Drawing.Point(0, 0);
            this.tcConfigSet.Name = "tcConfigSet";
            this.tcConfigSet.SelectedIndex = 0;
            this.tcConfigSet.Size = new System.Drawing.Size(784, 345);
            this.tcConfigSet.TabIndex = 0;
            // 
            // tpHost
            // 
            this.tpHost.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.tpHost.Controls.Add(this.dgvHost);
            this.tpHost.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.tpHost.Location = new System.Drawing.Point(4, 25);
            this.tpHost.Name = "tpHost";
            this.tpHost.Padding = new System.Windows.Forms.Padding(3);
            this.tpHost.Size = new System.Drawing.Size(776, 316);
            this.tpHost.TabIndex = 0;
            this.tpHost.Text = "Host.config";
            this.tpHost.UseVisualStyleBackColor = true;
            // 
            // dgvHost
            // 
            this.dgvHost.AllowUserToResizeColumns = false;
            this.dgvHost.AllowUserToResizeRows = false;
            this.dgvHost.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
            this.dgvHost.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
            this.dgvHost.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvHost.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.prefHost,
            this.valueHost});
            this.dgvHost.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvHost.Location = new System.Drawing.Point(3, 3);
            this.dgvHost.Name = "dgvHost";
            this.dgvHost.Size = new System.Drawing.Size(766, 306);
            this.dgvHost.TabIndex = 0;
            // 
            // prefHost
            // 
            this.prefHost.HeaderText = "Префикс";
            this.prefHost.Name = "prefHost";
            this.prefHost.Width = 84;
            // 
            // valueHost
            // 
            this.valueHost.HeaderText = "Значение";
            this.valueHost.Name = "valueHost";
            this.valueHost.Width = 89;
            // 
            // tpWeb
            // 
            this.tpWeb.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.tpWeb.Controls.Add(this.dgvWeb);
            this.tpWeb.Location = new System.Drawing.Point(4, 25);
            this.tpWeb.Name = "tpWeb";
            this.tpWeb.Padding = new System.Windows.Forms.Padding(3);
            this.tpWeb.Size = new System.Drawing.Size(776, 316);
            this.tpWeb.TabIndex = 1;
            this.tpWeb.Text = "Web.config";
            this.tpWeb.UseVisualStyleBackColor = true;
            // 
            // dgvWeb
            // 
            this.dgvWeb.AllowUserToResizeColumns = false;
            this.dgvWeb.AllowUserToResizeRows = false;
            this.dgvWeb.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
            this.dgvWeb.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
            this.dgvWeb.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvWeb.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.prefWeb,
            this.valueWeb});
            this.dgvWeb.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvWeb.Location = new System.Drawing.Point(3, 3);
            this.dgvWeb.Name = "dgvWeb";
            this.dgvWeb.Size = new System.Drawing.Size(766, 306);
            this.dgvWeb.TabIndex = 1;
            // 
            // prefWeb
            // 
            this.prefWeb.HeaderText = "Префикс";
            this.prefWeb.Name = "prefWeb";
            this.prefWeb.Width = 84;
            // 
            // valueWeb
            // 
            this.valueWeb.HeaderText = "Значение";
            this.valueWeb.Name = "valueWeb";
            this.valueWeb.Width = 89;
            // 
            // btnCancel
            // 
            this.btnCancel.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.btnCancel.Location = new System.Drawing.Point(406, 351);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(152, 28);
            this.btnCancel.TabIndex = 11;
            this.btnCancel.Text = "Отмена";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // btnOK
            // 
            this.btnOK.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOK.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.btnOK.Location = new System.Drawing.Point(218, 351);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(163, 28);
            this.btnOK.TabIndex = 10;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // ConfigSettingsForm
            // 
            this.AcceptButton = this.btnOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(785, 387);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.tcConfigSet);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ConfigSettingsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "ConfigSettingsForm";
            this.tcConfigSet.ResumeLayout(false);
            this.tpHost.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvHost)).EndInit();
            this.tpWeb.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvWeb)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tcConfigSet;
        private System.Windows.Forms.TabPage tpHost;
        private System.Windows.Forms.TabPage tpWeb;
        private System.Windows.Forms.DataGridView dgvHost;
        private System.Windows.Forms.DataGridView dgvWeb;
        private System.Windows.Forms.DataGridViewTextBoxColumn prefHost;
        private System.Windows.Forms.DataGridViewTextBoxColumn valueHost;
        private System.Windows.Forms.DataGridViewTextBoxColumn prefWeb;
        private System.Windows.Forms.DataGridViewTextBoxColumn valueWeb;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnOK;
    }
}