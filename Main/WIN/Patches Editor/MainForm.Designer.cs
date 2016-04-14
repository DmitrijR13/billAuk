namespace PathcesEditor2014
{
    partial class MainForm
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
            this.components = new System.ComponentModel.Container();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.rbNonCrypted = new System.Windows.Forms.RadioButton();
            this.rbCrypted = new System.Windows.Forms.RadioButton();
            this.rbAll = new System.Windows.Forms.RadioButton();
            this.btnDecrypt = new System.Windows.Forms.Button();
            this.btnEncrypt = new System.Windows.Forms.Button();
            this.chbxlstFiles = new System.Windows.Forms.CheckedListBox();
            this.chbxRecur = new System.Windows.Forms.CheckBox();
            this.btnSelectPath = new System.Windows.Forms.Button();
            this.tbDirPath = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.label2 = new System.Windows.Forms.Label();
            this.btnSaveAs = new System.Windows.Forms.Button();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnOpen = new System.Windows.Forms.Button();
            this.rtbFile = new System.Windows.Forms.RichTextBox();
            this.tmrMain = new System.Windows.Forms.Timer(this.components);
            this.dlgOpenFile = new System.Windows.Forms.OpenFileDialog();
            this.dlgSaveFile = new System.Windows.Forms.SaveFileDialog();
            this.dlgOpenDir = new System.Windows.Forms.FolderBrowserDialog();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Margin = new System.Windows.Forms.Padding(4);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(826, 568);
            this.tabControl1.TabIndex = 0;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.groupBox1);
            this.tabPage1.Controls.Add(this.btnDecrypt);
            this.tabPage1.Controls.Add(this.btnEncrypt);
            this.tabPage1.Controls.Add(this.chbxlstFiles);
            this.tabPage1.Controls.Add(this.chbxRecur);
            this.tabPage1.Controls.Add(this.btnSelectPath);
            this.tabPage1.Controls.Add(this.tbDirPath);
            this.tabPage1.Controls.Add(this.label1);
            this.tabPage1.Location = new System.Drawing.Point(4, 28);
            this.tabPage1.Margin = new System.Windows.Forms.Padding(4);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(4);
            this.tabPage1.Size = new System.Drawing.Size(818, 536);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Шифрование/Дешифрование папок с файлами";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.rbNonCrypted);
            this.groupBox1.Controls.Add(this.rbCrypted);
            this.groupBox1.Controls.Add(this.rbAll);
            this.groupBox1.Location = new System.Drawing.Point(631, 64);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(180, 121);
            this.groupBox1.TabIndex = 7;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Фильтр";
            // 
            // rbNonCrypted
            // 
            this.rbNonCrypted.AutoSize = true;
            this.rbNonCrypted.Location = new System.Drawing.Point(7, 85);
            this.rbNonCrypted.Name = "rbNonCrypted";
            this.rbNonCrypted.Size = new System.Drawing.Size(165, 23);
            this.rbNonCrypted.TabIndex = 2;
            this.rbNonCrypted.Text = "Незашифрованные";
            this.rbNonCrypted.UseVisualStyleBackColor = true;
            this.rbNonCrypted.CheckedChanged += new System.EventHandler(this.chbxRecur_CheckedChanged);
            // 
            // rbCrypted
            // 
            this.rbCrypted.AutoSize = true;
            this.rbCrypted.Location = new System.Drawing.Point(7, 56);
            this.rbCrypted.Name = "rbCrypted";
            this.rbCrypted.Size = new System.Drawing.Size(148, 23);
            this.rbCrypted.TabIndex = 1;
            this.rbCrypted.Text = "Зашифрованные";
            this.rbCrypted.UseVisualStyleBackColor = true;
            this.rbCrypted.CheckedChanged += new System.EventHandler(this.chbxRecur_CheckedChanged);
            // 
            // rbAll
            // 
            this.rbAll.AutoSize = true;
            this.rbAll.Checked = true;
            this.rbAll.Location = new System.Drawing.Point(7, 27);
            this.rbAll.Name = "rbAll";
            this.rbAll.Size = new System.Drawing.Size(51, 23);
            this.rbAll.TabIndex = 0;
            this.rbAll.TabStop = true;
            this.rbAll.Text = "Все";
            this.rbAll.UseVisualStyleBackColor = true;
            this.rbAll.CheckedChanged += new System.EventHandler(this.chbxRecur_CheckedChanged);
            // 
            // btnDecrypt
            // 
            this.btnDecrypt.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnDecrypt.Location = new System.Drawing.Point(630, 503);
            this.btnDecrypt.Name = "btnDecrypt";
            this.btnDecrypt.Size = new System.Drawing.Size(180, 27);
            this.btnDecrypt.TabIndex = 6;
            this.btnDecrypt.Text = "Расшифровать";
            this.btnDecrypt.UseVisualStyleBackColor = true;
            this.btnDecrypt.Click += new System.EventHandler(this.btnDecrypt_Click);
            // 
            // btnEncrypt
            // 
            this.btnEncrypt.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnEncrypt.Location = new System.Drawing.Point(630, 470);
            this.btnEncrypt.Name = "btnEncrypt";
            this.btnEncrypt.Size = new System.Drawing.Size(180, 27);
            this.btnEncrypt.TabIndex = 5;
            this.btnEncrypt.Text = "Зашифровать";
            this.btnEncrypt.UseVisualStyleBackColor = true;
            this.btnEncrypt.Click += new System.EventHandler(this.btnEncrypt_Click);
            // 
            // chbxlstFiles
            // 
            this.chbxlstFiles.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.chbxlstFiles.CheckOnClick = true;
            this.chbxlstFiles.FormattingEnabled = true;
            this.chbxlstFiles.Location = new System.Drawing.Point(12, 64);
            this.chbxlstFiles.Name = "chbxlstFiles";
            this.chbxlstFiles.ScrollAlwaysVisible = true;
            this.chbxlstFiles.Size = new System.Drawing.Size(612, 466);
            this.chbxlstFiles.TabIndex = 4;
            // 
            // chbxRecur
            // 
            this.chbxRecur.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.chbxRecur.AutoSize = true;
            this.chbxRecur.Checked = true;
            this.chbxRecur.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chbxRecur.Location = new System.Drawing.Point(679, 19);
            this.chbxRecur.Name = "chbxRecur";
            this.chbxRecur.Size = new System.Drawing.Size(112, 23);
            this.chbxRecur.TabIndex = 3;
            this.chbxRecur.Text = "Рекурсивно";
            this.chbxRecur.UseVisualStyleBackColor = true;
            this.chbxRecur.CheckedChanged += new System.EventHandler(this.chbxRecur_CheckedChanged);
            // 
            // btnSelectPath
            // 
            this.btnSelectPath.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSelectPath.Location = new System.Drawing.Point(630, 16);
            this.btnSelectPath.Name = "btnSelectPath";
            this.btnSelectPath.Size = new System.Drawing.Size(33, 27);
            this.btnSelectPath.TabIndex = 2;
            this.btnSelectPath.Text = "...";
            this.btnSelectPath.UseVisualStyleBackColor = true;
            this.btnSelectPath.Click += new System.EventHandler(this.btnSelectPath_Click);
            // 
            // tbDirPath
            // 
            this.tbDirPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbDirPath.Location = new System.Drawing.Point(118, 17);
            this.tbDirPath.Name = "tbDirPath";
            this.tbDirPath.ReadOnly = true;
            this.tbDirPath.Size = new System.Drawing.Size(506, 27);
            this.tbDirPath.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(8, 20);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(103, 19);
            this.label1.TabIndex = 0;
            this.label1.Text = "Путь к папке";
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.label2);
            this.tabPage2.Controls.Add(this.btnSaveAs);
            this.tabPage2.Controls.Add(this.btnSave);
            this.tabPage2.Controls.Add(this.btnOpen);
            this.tabPage2.Controls.Add(this.rtbFile);
            this.tabPage2.Location = new System.Drawing.Point(4, 28);
            this.tabPage2.Margin = new System.Windows.Forms.Padding(4);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(4);
            this.tabPage2.Size = new System.Drawing.Size(818, 536);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Редактирование зашифрованного файла";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.label2.Location = new System.Drawing.Point(671, 361);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(139, 59);
            this.label2.TabIndex = 4;
            this.label2.Text = "Файлы должны быть в win-1251 кодировке";
            // 
            // btnSaveAs
            // 
            this.btnSaveAs.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSaveAs.Location = new System.Drawing.Point(671, 491);
            this.btnSaveAs.Name = "btnSaveAs";
            this.btnSaveAs.Size = new System.Drawing.Size(139, 28);
            this.btnSaveAs.TabIndex = 3;
            this.btnSaveAs.Text = "Сохранить как...";
            this.btnSaveAs.UseVisualStyleBackColor = true;
            this.btnSaveAs.Click += new System.EventHandler(this.btnSaveAs_Click);
            // 
            // btnSave
            // 
            this.btnSave.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSave.Location = new System.Drawing.Point(671, 457);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(139, 28);
            this.btnSave.TabIndex = 2;
            this.btnSave.Text = "Сохранить";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // btnOpen
            // 
            this.btnOpen.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOpen.Location = new System.Drawing.Point(671, 423);
            this.btnOpen.Name = "btnOpen";
            this.btnOpen.Size = new System.Drawing.Size(139, 28);
            this.btnOpen.TabIndex = 1;
            this.btnOpen.Text = "Открыть";
            this.btnOpen.UseVisualStyleBackColor = true;
            this.btnOpen.Click += new System.EventHandler(this.btnOpen_Click);
            // 
            // rtbFile
            // 
            this.rtbFile.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.rtbFile.Font = new System.Drawing.Font("Consolas", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.rtbFile.Location = new System.Drawing.Point(8, 7);
            this.rtbFile.Name = "rtbFile";
            this.rtbFile.Size = new System.Drawing.Size(657, 512);
            this.rtbFile.TabIndex = 0;
            this.rtbFile.Text = "";
            // 
            // tmrMain
            // 
            this.tmrMain.Enabled = true;
            this.tmrMain.Tick += new System.EventHandler(this.tmrMain_Tick);
            // 
            // dlgOpenFile
            // 
            this.dlgOpenFile.Filter = "Все зашифрованные файлы|*.inic;*.sqc|inic files|*.inic|sqc files|*.sqc";
            // 
            // dlgSaveFile
            // 
            this.dlgSaveFile.Filter = "inic files|*.inic|sqc files|*.sqc|Все файлы|*.*";
            // 
            // dlgOpenDir
            // 
            this.dlgOpenDir.SelectedPath = " ";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 19F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(826, 568);
            this.Controls.Add(this.tabControl1);
            this.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "MainForm";
            this.Text = "Patches Editor 2014";
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.tabPage2.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.CheckBox chbxRecur;
        private System.Windows.Forms.Button btnSelectPath;
        private System.Windows.Forms.TextBox tbDirPath;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.CheckedListBox chbxlstFiles;
        private System.Windows.Forms.Button btnDecrypt;
        private System.Windows.Forms.Button btnEncrypt;
        private System.Windows.Forms.Button btnSaveAs;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnOpen;
        private System.Windows.Forms.RichTextBox rtbFile;
        private System.Windows.Forms.Timer tmrMain;
        private System.Windows.Forms.OpenFileDialog dlgOpenFile;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.SaveFileDialog dlgSaveFile;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.RadioButton rbNonCrypted;
        private System.Windows.Forms.RadioButton rbCrypted;
        private System.Windows.Forms.RadioButton rbAll;
        private System.Windows.Forms.FolderBrowserDialog dlgOpenDir;
    }
}

