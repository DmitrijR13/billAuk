namespace VersionCompile
{
    partial class CompileVersionsForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CompileVersionsForm));
            this.gbTypes = new System.Windows.Forms.GroupBox();
            this.cbWeb = new System.Windows.Forms.CheckBox();
            this.cbHost = new System.Windows.Forms.CheckBox();
            this.gbBase = new System.Windows.Forms.GroupBox();
            this.rbFireBird = new System.Windows.Forms.RadioButton();
            this.rbInformix = new System.Windows.Forms.RadioButton();
            this.gbTheme = new System.Windows.Forms.GroupBox();
            this.rbBars = new System.Windows.Forms.RadioButton();
            this.rbBlue = new System.Windows.Forms.RadioButton();
            this.chlbSQLFiles = new System.Windows.Forms.CheckedListBox();
            this.lbText = new System.Windows.Forms.Label();
            this.btnCompile = new System.Windows.Forms.Button();
            this.btnChangeConfig = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.gbVersionType = new System.Windows.Forms.GroupBox();
            this.rbFull = new System.Windows.Forms.RadioButton();
            this.rbForUpdate = new System.Windows.Forms.RadioButton();
            this.gbTypes.SuspendLayout();
            this.gbBase.SuspendLayout();
            this.gbTheme.SuspendLayout();
            this.gbVersionType.SuspendLayout();
            this.SuspendLayout();
            // 
            // gbTypes
            // 
            this.gbTypes.Controls.Add(this.cbWeb);
            this.gbTypes.Controls.Add(this.cbHost);
            this.gbTypes.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.gbTypes.Location = new System.Drawing.Point(12, 85);
            this.gbTypes.Name = "gbTypes";
            this.gbTypes.Size = new System.Drawing.Size(205, 67);
            this.gbTypes.TabIndex = 0;
            this.gbTypes.TabStop = false;
            this.gbTypes.Text = "Собрать";
            // 
            // cbWeb
            // 
            this.cbWeb.AutoSize = true;
            this.cbWeb.Checked = true;
            this.cbWeb.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbWeb.Location = new System.Drawing.Point(6, 42);
            this.cbWeb.Name = "cbWeb";
            this.cbWeb.Size = new System.Drawing.Size(59, 23);
            this.cbWeb.TabIndex = 1;
            this.cbWeb.Text = "Web";
            this.cbWeb.UseVisualStyleBackColor = true;
            this.cbWeb.CheckedChanged += new System.EventHandler(this.cbWeb_CheckedChanged);
            // 
            // cbHost
            // 
            this.cbHost.AutoSize = true;
            this.cbHost.Checked = true;
            this.cbHost.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbHost.Location = new System.Drawing.Point(6, 19);
            this.cbHost.Name = "cbHost";
            this.cbHost.Size = new System.Drawing.Size(60, 23);
            this.cbHost.TabIndex = 0;
            this.cbHost.Text = "Host";
            this.cbHost.UseVisualStyleBackColor = true;
            this.cbHost.CheckedChanged += new System.EventHandler(this.cbHost_CheckedChanged);
            // 
            // gbBase
            // 
            this.gbBase.Controls.Add(this.rbFireBird);
            this.gbBase.Controls.Add(this.rbInformix);
            this.gbBase.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.gbBase.Location = new System.Drawing.Point(12, 158);
            this.gbBase.Name = "gbBase";
            this.gbBase.Size = new System.Drawing.Size(205, 67);
            this.gbBase.TabIndex = 1;
            this.gbBase.TabStop = false;
            this.gbBase.Text = "База данных";
            // 
            // rbFireBird
            // 
            this.rbFireBird.AutoSize = true;
            this.rbFireBird.Location = new System.Drawing.Point(6, 42);
            this.rbFireBird.Name = "rbFireBird";
            this.rbFireBird.Size = new System.Drawing.Size(81, 23);
            this.rbFireBird.TabIndex = 1;
            this.rbFireBird.Text = "FireBird";
            this.rbFireBird.UseVisualStyleBackColor = true;
            // 
            // rbInformix
            // 
            this.rbInformix.AutoSize = true;
            this.rbInformix.Checked = true;
            this.rbInformix.Location = new System.Drawing.Point(6, 19);
            this.rbInformix.Name = "rbInformix";
            this.rbInformix.Size = new System.Drawing.Size(88, 23);
            this.rbInformix.TabIndex = 0;
            this.rbInformix.TabStop = true;
            this.rbInformix.Text = "Informix";
            this.rbInformix.UseVisualStyleBackColor = true;
            // 
            // gbTheme
            // 
            this.gbTheme.Controls.Add(this.rbBars);
            this.gbTheme.Controls.Add(this.rbBlue);
            this.gbTheme.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.gbTheme.Location = new System.Drawing.Point(12, 233);
            this.gbTheme.Name = "gbTheme";
            this.gbTheme.Size = new System.Drawing.Size(205, 67);
            this.gbTheme.TabIndex = 2;
            this.gbTheme.TabStop = false;
            this.gbTheme.Text = "Тема";
            // 
            // rbBars
            // 
            this.rbBars.AutoSize = true;
            this.rbBars.Location = new System.Drawing.Point(6, 42);
            this.rbBars.Name = "rbBars";
            this.rbBars.Size = new System.Drawing.Size(57, 23);
            this.rbBars.TabIndex = 1;
            this.rbBars.Text = "Bars";
            this.rbBars.UseVisualStyleBackColor = true;
            // 
            // rbBlue
            // 
            this.rbBlue.AutoSize = true;
            this.rbBlue.Checked = true;
            this.rbBlue.Location = new System.Drawing.Point(6, 19);
            this.rbBlue.Name = "rbBlue";
            this.rbBlue.Size = new System.Drawing.Size(57, 23);
            this.rbBlue.TabIndex = 0;
            this.rbBlue.TabStop = true;
            this.rbBlue.Text = "Blue";
            this.rbBlue.UseVisualStyleBackColor = true;
            // 
            // chlbSQLFiles
            // 
            this.chlbSQLFiles.CheckOnClick = true;
            this.chlbSQLFiles.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.chlbSQLFiles.FormattingEnabled = true;
            this.chlbSQLFiles.Location = new System.Drawing.Point(223, 34);
            this.chlbSQLFiles.Name = "chlbSQLFiles";
            this.chlbSQLFiles.Size = new System.Drawing.Size(235, 310);
            this.chlbSQLFiles.TabIndex = 3;
            // 
            // lbText
            // 
            this.lbText.AutoSize = true;
            this.lbText.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lbText.Location = new System.Drawing.Point(219, 12);
            this.lbText.Name = "lbText";
            this.lbText.Size = new System.Drawing.Size(235, 19);
            this.lbText.TabIndex = 4;
            this.lbText.Text = "Добавить в сборку SQL файлы";
            // 
            // btnCompile
            // 
            this.btnCompile.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnCompile.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.btnCompile.Location = new System.Drawing.Point(223, 360);
            this.btnCompile.Name = "btnCompile";
            this.btnCompile.Size = new System.Drawing.Size(235, 31);
            this.btnCompile.TabIndex = 5;
            this.btnCompile.Text = "Собрать версию";
            this.btnCompile.UseVisualStyleBackColor = true;
            this.btnCompile.Click += new System.EventHandler(this.btnCompile_Click);
            // 
            // btnChangeConfig
            // 
            this.btnChangeConfig.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnChangeConfig.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.btnChangeConfig.Location = new System.Drawing.Point(12, 313);
            this.btnChangeConfig.Name = "btnChangeConfig";
            this.btnChangeConfig.Size = new System.Drawing.Size(205, 31);
            this.btnChangeConfig.TabIndex = 6;
            this.btnChangeConfig.Text = "Настроить .config";
            this.btnChangeConfig.UseVisualStyleBackColor = true;
            this.btnChangeConfig.Click += new System.EventHandler(this.btnChangeConfig_Click);
            // 
            // button2
            // 
            this.button2.Cursor = System.Windows.Forms.Cursors.Hand;
            this.button2.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.button2.Location = new System.Drawing.Point(12, 360);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(205, 31);
            this.button2.TabIndex = 7;
            this.button2.Text = "Настройки программы";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // gbVersionType
            // 
            this.gbVersionType.Controls.Add(this.rbFull);
            this.gbVersionType.Controls.Add(this.rbForUpdate);
            this.gbVersionType.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.gbVersionType.Location = new System.Drawing.Point(12, 12);
            this.gbVersionType.Name = "gbVersionType";
            this.gbVersionType.Size = new System.Drawing.Size(205, 67);
            this.gbVersionType.TabIndex = 2;
            this.gbVersionType.TabStop = false;
            this.gbVersionType.Text = "Тип версии";
            // 
            // rbFull
            // 
            this.rbFull.AutoSize = true;
            this.rbFull.Location = new System.Drawing.Point(6, 42);
            this.rbFull.Name = "rbFull";
            this.rbFull.Size = new System.Drawing.Size(81, 23);
            this.rbFull.TabIndex = 1;
            this.rbFull.Text = "Полная";
            this.rbFull.UseVisualStyleBackColor = true;
            // 
            // rbForUpdate
            // 
            this.rbForUpdate.AutoSize = true;
            this.rbForUpdate.Checked = true;
            this.rbForUpdate.Location = new System.Drawing.Point(6, 19);
            this.rbForUpdate.Name = "rbForUpdate";
            this.rbForUpdate.Size = new System.Drawing.Size(147, 23);
            this.rbForUpdate.TabIndex = 0;
            this.rbForUpdate.TabStop = true;
            this.rbForUpdate.Text = "Для обновления";
            this.rbForUpdate.UseVisualStyleBackColor = true;
            // 
            // CompileVersionsForm
            // 
            this.AcceptButton = this.btnCompile;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(465, 401);
            this.Controls.Add(this.gbVersionType);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.btnChangeConfig);
            this.Controls.Add(this.btnCompile);
            this.Controls.Add(this.lbText);
            this.Controls.Add(this.chlbSQLFiles);
            this.Controls.Add(this.gbTheme);
            this.Controls.Add(this.gbBase);
            this.Controls.Add(this.gbTypes);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "CompileVersionsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Сборщик версий";
            this.gbTypes.ResumeLayout(false);
            this.gbTypes.PerformLayout();
            this.gbBase.ResumeLayout(false);
            this.gbBase.PerformLayout();
            this.gbTheme.ResumeLayout(false);
            this.gbTheme.PerformLayout();
            this.gbVersionType.ResumeLayout(false);
            this.gbVersionType.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox gbTypes;
        private System.Windows.Forms.CheckBox cbWeb;
        private System.Windows.Forms.CheckBox cbHost;
        private System.Windows.Forms.GroupBox gbBase;
        private System.Windows.Forms.RadioButton rbFireBird;
        private System.Windows.Forms.RadioButton rbInformix;
        private System.Windows.Forms.GroupBox gbTheme;
        private System.Windows.Forms.RadioButton rbBars;
        private System.Windows.Forms.RadioButton rbBlue;
        private System.Windows.Forms.CheckedListBox chlbSQLFiles;
        private System.Windows.Forms.Label lbText;
        private System.Windows.Forms.Button btnCompile;
        private System.Windows.Forms.Button btnChangeConfig;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.GroupBox gbVersionType;
        private System.Windows.Forms.RadioButton rbFull;
        private System.Windows.Forms.RadioButton rbForUpdate;
    }
}