namespace VersionCompile
{
    partial class ProgramSettingsForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ProgramSettingsForm));
            this.dlgOpenFolder = new System.Windows.Forms.FolderBrowserDialog();
            this.gbPaths = new System.Windows.Forms.GroupBox();
            this.tbWebPathToUpdate = new System.Windows.Forms.TextBox();
            this.lbWebPathToUpdate = new System.Windows.Forms.Label();
            this.btnPathToAssembly = new System.Windows.Forms.Button();
            this.tbPathToAssembly = new System.Windows.Forms.TextBox();
            this.lbPathToAssembly = new System.Windows.Forms.Label();
            this.btnPathToUpdate = new System.Windows.Forms.Button();
            this.tbPathToUpdate = new System.Windows.Forms.TextBox();
            this.lbPathToUpdate = new System.Windows.Forms.Label();
            this.btnPathToSQL = new System.Windows.Forms.Button();
            this.tbPathToSQL = new System.Windows.Forms.TextBox();
            this.lbPathToSQL = new System.Windows.Forms.Label();
            this.btnPathToWeb = new System.Windows.Forms.Button();
            this.tbPathToWeb = new System.Windows.Forms.TextBox();
            this.lbPathToWeb = new System.Windows.Forms.Label();
            this.btnPathToHost = new System.Windows.Forms.Button();
            this.tbPathToHost = new System.Windows.Forms.TextBox();
            this.lbPathToHost = new System.Windows.Forms.Label();
            this.gbKeys = new System.Windows.Forms.GroupBox();
            this.tbWebKey = new System.Windows.Forms.TextBox();
            this.lbWebKey = new System.Windows.Forms.Label();
            this.tbHostKey = new System.Windows.Forms.TextBox();
            this.lbHostKey = new System.Windows.Forms.Label();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.tbConnStr = new System.Windows.Forms.TextBox();
            this.gbPaths.SuspendLayout();
            this.gbKeys.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // gbPaths
            // 
            this.gbPaths.Controls.Add(this.tbWebPathToUpdate);
            this.gbPaths.Controls.Add(this.lbWebPathToUpdate);
            this.gbPaths.Controls.Add(this.btnPathToAssembly);
            this.gbPaths.Controls.Add(this.tbPathToAssembly);
            this.gbPaths.Controls.Add(this.lbPathToAssembly);
            this.gbPaths.Controls.Add(this.btnPathToUpdate);
            this.gbPaths.Controls.Add(this.tbPathToUpdate);
            this.gbPaths.Controls.Add(this.lbPathToUpdate);
            this.gbPaths.Controls.Add(this.btnPathToSQL);
            this.gbPaths.Controls.Add(this.tbPathToSQL);
            this.gbPaths.Controls.Add(this.lbPathToSQL);
            this.gbPaths.Controls.Add(this.btnPathToWeb);
            this.gbPaths.Controls.Add(this.tbPathToWeb);
            this.gbPaths.Controls.Add(this.lbPathToWeb);
            this.gbPaths.Controls.Add(this.btnPathToHost);
            this.gbPaths.Controls.Add(this.tbPathToHost);
            this.gbPaths.Controls.Add(this.lbPathToHost);
            this.gbPaths.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.gbPaths.Location = new System.Drawing.Point(12, 83);
            this.gbPaths.Name = "gbPaths";
            this.gbPaths.Size = new System.Drawing.Size(678, 230);
            this.gbPaths.TabIndex = 3;
            this.gbPaths.TabStop = false;
            this.gbPaths.Text = "Настройка путей";
            // 
            // tbWebPathToUpdate
            // 
            this.tbWebPathToUpdate.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.tbWebPathToUpdate.Location = new System.Drawing.Point(165, 196);
            this.tbWebPathToUpdate.Name = "tbWebPathToUpdate";
            this.tbWebPathToUpdate.Size = new System.Drawing.Size(507, 27);
            this.tbWebPathToUpdate.TabIndex = 19;
            // 
            // lbWebPathToUpdate
            // 
            this.lbWebPathToUpdate.AutoSize = true;
            this.lbWebPathToUpdate.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lbWebPathToUpdate.Location = new System.Drawing.Point(16, 199);
            this.lbWebPathToUpdate.Name = "lbWebPathToUpdate";
            this.lbWebPathToUpdate.Size = new System.Drawing.Size(127, 19);
            this.lbWebPathToUpdate.TabIndex = 18;
            this.lbWebPathToUpdate.Text = "Ссылка на файл";
            // 
            // btnPathToAssembly
            // 
            this.btnPathToAssembly.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.btnPathToAssembly.Location = new System.Drawing.Point(639, 163);
            this.btnPathToAssembly.Name = "btnPathToAssembly";
            this.btnPathToAssembly.Size = new System.Drawing.Size(33, 27);
            this.btnPathToAssembly.TabIndex = 17;
            this.btnPathToAssembly.Text = "...";
            this.btnPathToAssembly.UseVisualStyleBackColor = true;
            this.btnPathToAssembly.Click += new System.EventHandler(this.btnPathToAssembly_Click);
            // 
            // tbPathToAssembly
            // 
            this.tbPathToAssembly.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.tbPathToAssembly.Location = new System.Drawing.Point(165, 163);
            this.tbPathToAssembly.Name = "tbPathToAssembly";
            this.tbPathToAssembly.Size = new System.Drawing.Size(468, 27);
            this.tbPathToAssembly.TabIndex = 16;
            // 
            // lbPathToAssembly
            // 
            this.lbPathToAssembly.AutoSize = true;
            this.lbPathToAssembly.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lbPathToAssembly.Location = new System.Drawing.Point(16, 166);
            this.lbPathToAssembly.Name = "lbPathToAssembly";
            this.lbPathToAssembly.Size = new System.Drawing.Size(122, 19);
            this.lbPathToAssembly.TabIndex = 15;
            this.lbPathToAssembly.Text = "Путь к сборкам";
            // 
            // btnPathToUpdate
            // 
            this.btnPathToUpdate.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.btnPathToUpdate.Location = new System.Drawing.Point(639, 130);
            this.btnPathToUpdate.Name = "btnPathToUpdate";
            this.btnPathToUpdate.Size = new System.Drawing.Size(33, 28);
            this.btnPathToUpdate.TabIndex = 14;
            this.btnPathToUpdate.Text = "...";
            this.btnPathToUpdate.UseVisualStyleBackColor = true;
            this.btnPathToUpdate.Click += new System.EventHandler(this.btnPathToUpdate_Click);
            // 
            // tbPathToUpdate
            // 
            this.tbPathToUpdate.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.tbPathToUpdate.Location = new System.Drawing.Point(165, 130);
            this.tbPathToUpdate.Name = "tbPathToUpdate";
            this.tbPathToUpdate.Size = new System.Drawing.Size(468, 27);
            this.tbPathToUpdate.TabIndex = 13;
            // 
            // lbPathToUpdate
            // 
            this.lbPathToUpdate.AutoSize = true;
            this.lbPathToUpdate.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lbPathToUpdate.Location = new System.Drawing.Point(16, 133);
            this.lbPathToUpdate.Name = "lbPathToUpdate";
            this.lbPathToUpdate.Size = new System.Drawing.Size(127, 19);
            this.lbPathToUpdate.TabIndex = 12;
            this.lbPathToUpdate.Text = "Путь к updater\'у";
            // 
            // btnPathToSQL
            // 
            this.btnPathToSQL.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.btnPathToSQL.Location = new System.Drawing.Point(639, 97);
            this.btnPathToSQL.Name = "btnPathToSQL";
            this.btnPathToSQL.Size = new System.Drawing.Size(33, 28);
            this.btnPathToSQL.TabIndex = 11;
            this.btnPathToSQL.Text = "...";
            this.btnPathToSQL.UseVisualStyleBackColor = true;
            this.btnPathToSQL.Click += new System.EventHandler(this.btnPathToSQL_Click);
            // 
            // tbPathToSQL
            // 
            this.tbPathToSQL.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.tbPathToSQL.Location = new System.Drawing.Point(165, 97);
            this.tbPathToSQL.Name = "tbPathToSQL";
            this.tbPathToSQL.Size = new System.Drawing.Size(468, 27);
            this.tbPathToSQL.TabIndex = 10;
            // 
            // lbPathToSQL
            // 
            this.lbPathToSQL.AutoSize = true;
            this.lbPathToSQL.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lbPathToSQL.Location = new System.Drawing.Point(16, 100);
            this.lbPathToSQL.Name = "lbPathToSQL";
            this.lbPathToSQL.Size = new System.Drawing.Size(91, 19);
            this.lbPathToSQL.TabIndex = 9;
            this.lbPathToSQL.Text = "Путь к SQL";
            // 
            // btnPathToWeb
            // 
            this.btnPathToWeb.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.btnPathToWeb.Location = new System.Drawing.Point(639, 64);
            this.btnPathToWeb.Name = "btnPathToWeb";
            this.btnPathToWeb.Size = new System.Drawing.Size(33, 28);
            this.btnPathToWeb.TabIndex = 8;
            this.btnPathToWeb.Text = "...";
            this.btnPathToWeb.UseVisualStyleBackColor = true;
            this.btnPathToWeb.Click += new System.EventHandler(this.btnPathToWeb_Click);
            // 
            // tbPathToWeb
            // 
            this.tbPathToWeb.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.tbPathToWeb.Location = new System.Drawing.Point(165, 64);
            this.tbPathToWeb.Name = "tbPathToWeb";
            this.tbPathToWeb.Size = new System.Drawing.Size(468, 27);
            this.tbPathToWeb.TabIndex = 7;
            // 
            // lbPathToWeb
            // 
            this.lbPathToWeb.AutoSize = true;
            this.lbPathToWeb.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lbPathToWeb.Location = new System.Drawing.Point(16, 67);
            this.lbPathToWeb.Name = "lbPathToWeb";
            this.lbPathToWeb.Size = new System.Drawing.Size(93, 19);
            this.lbPathToWeb.TabIndex = 6;
            this.lbPathToWeb.Text = "Путь к Web";
            // 
            // btnPathToHost
            // 
            this.btnPathToHost.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.btnPathToHost.Location = new System.Drawing.Point(639, 29);
            this.btnPathToHost.Name = "btnPathToHost";
            this.btnPathToHost.Size = new System.Drawing.Size(33, 29);
            this.btnPathToHost.TabIndex = 5;
            this.btnPathToHost.Text = "...";
            this.btnPathToHost.UseVisualStyleBackColor = true;
            this.btnPathToHost.Click += new System.EventHandler(this.btnPathToHost_Click);
            // 
            // tbPathToHost
            // 
            this.tbPathToHost.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.tbPathToHost.Location = new System.Drawing.Point(165, 31);
            this.tbPathToHost.Name = "tbPathToHost";
            this.tbPathToHost.Size = new System.Drawing.Size(468, 27);
            this.tbPathToHost.TabIndex = 4;
            // 
            // lbPathToHost
            // 
            this.lbPathToHost.AutoSize = true;
            this.lbPathToHost.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lbPathToHost.Location = new System.Drawing.Point(16, 34);
            this.lbPathToHost.Name = "lbPathToHost";
            this.lbPathToHost.Size = new System.Drawing.Size(94, 19);
            this.lbPathToHost.TabIndex = 3;
            this.lbPathToHost.Text = "Путь к Host";
            // 
            // gbKeys
            // 
            this.gbKeys.Controls.Add(this.tbWebKey);
            this.gbKeys.Controls.Add(this.lbWebKey);
            this.gbKeys.Controls.Add(this.tbHostKey);
            this.gbKeys.Controls.Add(this.lbHostKey);
            this.gbKeys.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.gbKeys.Location = new System.Drawing.Point(12, 340);
            this.gbKeys.Name = "gbKeys";
            this.gbKeys.Size = new System.Drawing.Size(678, 105);
            this.gbKeys.TabIndex = 4;
            this.gbKeys.TabStop = false;
            this.gbKeys.Text = "Настройка ключей архивации";
            // 
            // tbWebKey
            // 
            this.tbWebKey.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.tbWebKey.Location = new System.Drawing.Point(160, 64);
            this.tbWebKey.Name = "tbWebKey";
            this.tbWebKey.Size = new System.Drawing.Size(512, 27);
            this.tbWebKey.TabIndex = 13;
            // 
            // lbWebKey
            // 
            this.lbWebKey.AutoSize = true;
            this.lbWebKey.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lbWebKey.Location = new System.Drawing.Point(11, 67);
            this.lbWebKey.Name = "lbWebKey";
            this.lbWebKey.Size = new System.Drawing.Size(116, 19);
            this.lbWebKey.TabIndex = 12;
            this.lbWebKey.Text = "Ключ для Web";
            // 
            // tbHostKey
            // 
            this.tbHostKey.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.tbHostKey.Location = new System.Drawing.Point(160, 31);
            this.tbHostKey.Name = "tbHostKey";
            this.tbHostKey.Size = new System.Drawing.Size(512, 27);
            this.tbHostKey.TabIndex = 10;
            // 
            // lbHostKey
            // 
            this.lbHostKey.AutoSize = true;
            this.lbHostKey.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lbHostKey.Location = new System.Drawing.Point(11, 34);
            this.lbHostKey.Name = "lbHostKey";
            this.lbHostKey.Size = new System.Drawing.Size(117, 19);
            this.lbHostKey.TabIndex = 9;
            this.lbHostKey.Text = "Ключ для Host";
            // 
            // btnOK
            // 
            this.btnOK.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOK.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.btnOK.Location = new System.Drawing.Point(177, 451);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(163, 28);
            this.btnOK.TabIndex = 6;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.btnCancel.Location = new System.Drawing.Point(365, 451);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(152, 28);
            this.btnCancel.TabIndex = 7;
            this.btnCancel.Text = "Отмена";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.tbConnStr);
            this.groupBox1.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(677, 65);
            this.groupBox1.TabIndex = 8;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Подключение к БД";
            // 
            // tbConnStr
            // 
            this.tbConnStr.Location = new System.Drawing.Point(6, 26);
            this.tbConnStr.Name = "tbConnStr";
            this.tbConnStr.Size = new System.Drawing.Size(665, 27);
            this.tbConnStr.TabIndex = 0;
            // 
            // ProgramSettingsForm
            // 
            this.AcceptButton = this.btnOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(701, 488);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.gbKeys);
            this.Controls.Add(this.gbPaths);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "ProgramSettingsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Настройки программы";
            this.gbPaths.ResumeLayout(false);
            this.gbPaths.PerformLayout();
            this.gbKeys.ResumeLayout(false);
            this.gbKeys.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.FolderBrowserDialog dlgOpenFolder;
        private System.Windows.Forms.GroupBox gbPaths;
        private System.Windows.Forms.Button btnPathToHost;
        private System.Windows.Forms.TextBox tbPathToHost;
        private System.Windows.Forms.Label lbPathToHost;
        private System.Windows.Forms.GroupBox gbKeys;
        private System.Windows.Forms.Button btnPathToUpdate;
        private System.Windows.Forms.TextBox tbPathToUpdate;
        private System.Windows.Forms.Label lbPathToUpdate;
        private System.Windows.Forms.Button btnPathToSQL;
        private System.Windows.Forms.TextBox tbPathToSQL;
        private System.Windows.Forms.Label lbPathToSQL;
        private System.Windows.Forms.Button btnPathToWeb;
        private System.Windows.Forms.TextBox tbPathToWeb;
        private System.Windows.Forms.Label lbPathToWeb;
        private System.Windows.Forms.TextBox tbWebKey;
        private System.Windows.Forms.Label lbWebKey;
        private System.Windows.Forms.TextBox tbHostKey;
        private System.Windows.Forms.Label lbHostKey;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnPathToAssembly;
        private System.Windows.Forms.TextBox tbPathToAssembly;
        private System.Windows.Forms.Label lbPathToAssembly;
        private System.Windows.Forms.TextBox tbWebPathToUpdate;
        private System.Windows.Forms.Label lbWebPathToUpdate;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.TextBox tbConnStr;
    }
}