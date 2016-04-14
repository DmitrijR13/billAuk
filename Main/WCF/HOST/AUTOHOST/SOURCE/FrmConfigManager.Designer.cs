namespace STCLINE.KP50.HostMan.SOURCE
{
    partial class FrmConfigManager
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmConfigManager));
            this.labelGroupSelect = new System.Windows.Forms.Label();
            this.comboBoxGroups = new System.Windows.Forms.ComboBox();
            this.buttonDropGroup = new System.Windows.Forms.Button();
            this.textBoxNewGroupName = new System.Windows.Forms.TextBox();
            this.buttonCreateGroup = new System.Windows.Forms.Button();
            this.dataGridViewFiles = new System.Windows.Forms.DataGridView();
            this.File = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Path = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.splitContainer = new System.Windows.Forms.SplitContainer();
            this.toolStrip = new System.Windows.Forms.ToolStrip();
            this.toolStripButtonAddFile = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonDeleteFile = new System.Windows.Forms.ToolStripButton();
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripButtonLoad = new System.Windows.Forms.ToolStripButton();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewFiles)).BeginInit();
            this.splitContainer.Panel1.SuspendLayout();
            this.splitContainer.Panel2.SuspendLayout();
            this.splitContainer.SuspendLayout();
            this.toolStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // labelGroupSelect
            // 
            this.labelGroupSelect.AutoSize = true;
            this.labelGroupSelect.Location = new System.Drawing.Point(12, 15);
            this.labelGroupSelect.Name = "labelGroupSelect";
            this.labelGroupSelect.Size = new System.Drawing.Size(45, 13);
            this.labelGroupSelect.TabIndex = 0;
            this.labelGroupSelect.Text = "Группа:";
            // 
            // comboBoxGroups
            // 
            this.comboBoxGroups.Enabled = false;
            this.comboBoxGroups.FormattingEnabled = true;
            this.comboBoxGroups.Location = new System.Drawing.Point(63, 12);
            this.comboBoxGroups.Name = "comboBoxGroups";
            this.comboBoxGroups.Size = new System.Drawing.Size(207, 21);
            this.comboBoxGroups.TabIndex = 1;
            this.comboBoxGroups.SelectedIndexChanged += new System.EventHandler(this.comboBoxGroups_SelectedIndexChanged);
            // 
            // buttonDropGroup
            // 
            this.buttonDropGroup.Location = new System.Drawing.Point(276, 10);
            this.buttonDropGroup.Name = "buttonDropGroup";
            this.buttonDropGroup.Size = new System.Drawing.Size(60, 23);
            this.buttonDropGroup.TabIndex = 2;
            this.buttonDropGroup.Text = "Удалить";
            this.buttonDropGroup.UseVisualStyleBackColor = true;
            this.buttonDropGroup.Click += new System.EventHandler(this.buttonDropGroup_Click);
            // 
            // textBoxNewGroupName
            // 
            this.textBoxNewGroupName.Location = new System.Drawing.Point(354, 12);
            this.textBoxNewGroupName.Name = "textBoxNewGroupName";
            this.textBoxNewGroupName.Size = new System.Drawing.Size(207, 20);
            this.textBoxNewGroupName.TabIndex = 3;
            // 
            // buttonCreateGroup
            // 
            this.buttonCreateGroup.Location = new System.Drawing.Point(567, 10);
            this.buttonCreateGroup.Name = "buttonCreateGroup";
            this.buttonCreateGroup.Size = new System.Drawing.Size(58, 23);
            this.buttonCreateGroup.TabIndex = 4;
            this.buttonCreateGroup.Text = "Создать";
            this.buttonCreateGroup.UseVisualStyleBackColor = true;
            this.buttonCreateGroup.Click += new System.EventHandler(this.buttonCreateGroup_Click);
            // 
            // dataGridViewFiles
            // 
            this.dataGridViewFiles.AllowUserToAddRows = false;
            this.dataGridViewFiles.AllowUserToDeleteRows = false;
            this.dataGridViewFiles.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridViewFiles.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.File,
            this.Path});
            this.dataGridViewFiles.Location = new System.Drawing.Point(3, 3);
            this.dataGridViewFiles.MultiSelect = false;
            this.dataGridViewFiles.Name = "dataGridViewFiles";
            this.dataGridViewFiles.ReadOnly = true;
            this.dataGridViewFiles.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dataGridViewFiles.Size = new System.Drawing.Size(604, 306);
            this.dataGridViewFiles.TabIndex = 5;
            // 
            // File
            // 
            this.File.FillWeight = 150F;
            this.File.HeaderText = "File";
            this.File.Name = "File";
            this.File.ReadOnly = true;
            this.File.Width = 150;
            // 
            // Path
            // 
            this.Path.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.Path.HeaderText = "Path";
            this.Path.Name = "Path";
            this.Path.ReadOnly = true;
            // 
            // splitContainer
            // 
            this.splitContainer.Location = new System.Drawing.Point(15, 39);
            this.splitContainer.Name = "splitContainer";
            this.splitContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer.Panel1
            // 
            this.splitContainer.Panel1.Controls.Add(this.toolStrip);
            // 
            // splitContainer.Panel2
            // 
            this.splitContainer.Panel2.Controls.Add(this.dataGridViewFiles);
            this.splitContainer.Size = new System.Drawing.Size(610, 343);
            this.splitContainer.SplitterDistance = 25;
            this.splitContainer.TabIndex = 6;
            // 
            // toolStrip
            // 
            this.toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripButtonLoad,
            this.toolStripSeparator1,
            this.toolStripButtonAddFile,
            this.toolStripButtonDeleteFile});
            this.toolStrip.Location = new System.Drawing.Point(0, 0);
            this.toolStrip.Name = "toolStrip";
            this.toolStrip.Size = new System.Drawing.Size(610, 25);
            this.toolStrip.TabIndex = 0;
            this.toolStrip.Text = "toolStrip1";
            // 
            // toolStripButtonAddFile
            // 
            this.toolStripButtonAddFile.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripButtonAddFile.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonAddFile.Image")));
            this.toolStripButtonAddFile.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonAddFile.Name = "toolStripButtonAddFile";
            this.toolStripButtonAddFile.Size = new System.Drawing.Size(90, 22);
            this.toolStripButtonAddFile.Text = "Добавить файл";
            this.toolStripButtonAddFile.Click += new System.EventHandler(this.toolStripButtonAddFile_Click);
            // 
            // toolStripButtonDeleteFile
            // 
            this.toolStripButtonDeleteFile.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripButtonDeleteFile.Enabled = false;
            this.toolStripButtonDeleteFile.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonDeleteFile.Image")));
            this.toolStripButtonDeleteFile.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonDeleteFile.Name = "toolStripButtonDeleteFile";
            this.toolStripButtonDeleteFile.Size = new System.Drawing.Size(84, 22);
            this.toolStripButtonDeleteFile.Text = "Удалить файл";
            this.toolStripButtonDeleteFile.Click += new System.EventHandler(this.toolStripButtonDeleteFile_Click);
            // 
            // openFileDialog
            // 
            this.openFileDialog.FileName = "openFileDialog1";
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
            // 
            // toolStripButtonLoad
            // 
            this.toolStripButtonLoad.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripButtonLoad.Enabled = false;
            this.toolStripButtonLoad.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonLoad.Image")));
            this.toolStripButtonLoad.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonLoad.Name = "toolStripButtonLoad";
            this.toolStripButtonLoad.Size = new System.Drawing.Size(166, 22);
            this.toolStripButtonLoad.Text = "Загрузить файлы этой группы";
            this.toolStripButtonLoad.Click += new System.EventHandler(this.toolStripButtonLoad_Click);
            // 
            // FrmConfigManager
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(645, 396);
            this.Controls.Add(this.splitContainer);
            this.Controls.Add(this.buttonCreateGroup);
            this.Controls.Add(this.textBoxNewGroupName);
            this.Controls.Add(this.buttonDropGroup);
            this.Controls.Add(this.comboBoxGroups);
            this.Controls.Add(this.labelGroupSelect);
            this.Name = "FrmConfigManager";
            this.Text = "FrmConfigManager";
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewFiles)).EndInit();
            this.splitContainer.Panel1.ResumeLayout(false);
            this.splitContainer.Panel1.PerformLayout();
            this.splitContainer.Panel2.ResumeLayout(false);
            this.splitContainer.ResumeLayout(false);
            this.toolStrip.ResumeLayout(false);
            this.toolStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label labelGroupSelect;
        private System.Windows.Forms.ComboBox comboBoxGroups;
        private System.Windows.Forms.Button buttonDropGroup;
        private System.Windows.Forms.TextBox textBoxNewGroupName;
        private System.Windows.Forms.Button buttonCreateGroup;
        private System.Windows.Forms.DataGridView dataGridViewFiles;
        private System.Windows.Forms.DataGridViewTextBoxColumn File;
        private System.Windows.Forms.DataGridViewTextBoxColumn Path;
        private System.Windows.Forms.SplitContainer splitContainer;
        private System.Windows.Forms.ToolStrip toolStrip;
        private System.Windows.Forms.ToolStripButton toolStripButtonAddFile;
        private System.Windows.Forms.ToolStripButton toolStripButtonDeleteFile;
        private System.Windows.Forms.OpenFileDialog openFileDialog;
        private System.Windows.Forms.ToolStripButton toolStripButtonLoad;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
    }
}