namespace updater
{
    partial class ExecSQLListForm
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
            this.dlgSave = new System.Windows.Forms.SaveFileDialog();
            this.panel1 = new System.Windows.Forms.Panel();
            this.btnSave = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.rbKernel = new System.Windows.Forms.RadioButton();
            this.rbWebdata = new System.Windows.Forms.RadioButton();
            this.tbStep = new System.Windows.Forms.TextBox();
            this.lbStep = new System.Windows.Forms.Label();
            this.tbTableName = new System.Windows.Forms.TextBox();
            this.lbTableName = new System.Windows.Forms.Label();
            this.lbAfter = new System.Windows.Forms.Label();
            this.lbBefore = new System.Windows.Forms.Label();
            this.btnStart = new System.Windows.Forms.Button();
            this.rtbBefore = new System.Windows.Forms.RichTextBox();
            this.splitter1 = new System.Windows.Forms.Splitter();
            this.rtbAfter = new System.Windows.Forms.RichTextBox();
            this.btnTable = new System.Windows.Forms.Button();
            this.panel1.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // dlgSave
            // 
            this.dlgSave.Filter = "txt Files| *.txt";
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.btnTable);
            this.panel1.Controls.Add(this.btnSave);
            this.panel1.Controls.Add(this.groupBox1);
            this.panel1.Controls.Add(this.lbAfter);
            this.panel1.Controls.Add(this.lbBefore);
            this.panel1.Controls.Add(this.btnStart);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(803, 139);
            this.panel1.TabIndex = 5;
            // 
            // btnSave
            // 
            this.btnSave.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.btnSave.Location = new System.Drawing.Point(333, 70);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(186, 36);
            this.btnSave.TabIndex = 13;
            this.btnSave.Text = "Сохранить результаты";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.button1_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.rbKernel);
            this.groupBox1.Controls.Add(this.rbWebdata);
            this.groupBox1.Controls.Add(this.tbStep);
            this.groupBox1.Controls.Add(this.lbStep);
            this.groupBox1.Controls.Add(this.tbTableName);
            this.groupBox1.Controls.Add(this.lbTableName);
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(315, 96);
            this.groupBox1.TabIndex = 12;
            this.groupBox1.TabStop = false;
            // 
            // rbKernel
            // 
            this.rbKernel.AutoSize = true;
            this.rbKernel.Checked = true;
            this.rbKernel.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.rbKernel.Location = new System.Drawing.Point(192, 60);
            this.rbKernel.Name = "rbKernel";
            this.rbKernel.Size = new System.Drawing.Size(62, 20);
            this.rbKernel.TabIndex = 14;
            this.rbKernel.TabStop = true;
            this.rbKernel.Text = "Kernel";
            this.rbKernel.UseVisualStyleBackColor = true;
            // 
            // rbWebdata
            // 
            this.rbWebdata.AutoSize = true;
            this.rbWebdata.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.rbWebdata.Location = new System.Drawing.Point(32, 60);
            this.rbWebdata.Name = "rbWebdata";
            this.rbWebdata.Size = new System.Drawing.Size(78, 20);
            this.rbWebdata.TabIndex = 13;
            this.rbWebdata.Text = "WebData";
            this.rbWebdata.UseVisualStyleBackColor = true;
            // 
            // tbStep
            // 
            this.tbStep.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.tbStep.Location = new System.Drawing.Point(173, 32);
            this.tbStep.Name = "tbStep";
            this.tbStep.Size = new System.Drawing.Size(100, 23);
            this.tbStep.TabIndex = 12;
            // 
            // lbStep
            // 
            this.lbStep.AutoSize = true;
            this.lbStep.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lbStep.Location = new System.Drawing.Point(180, 16);
            this.lbStep.Name = "lbStep";
            this.lbStep.Size = new System.Drawing.Size(97, 16);
            this.lbStep.TabIndex = 11;
            this.lbStep.Text = "Величина шага";
            // 
            // tbTableName
            // 
            this.tbTableName.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.tbTableName.Location = new System.Drawing.Point(20, 32);
            this.tbTableName.Name = "tbTableName";
            this.tbTableName.Size = new System.Drawing.Size(100, 23);
            this.tbTableName.TabIndex = 10;
            // 
            // lbTableName
            // 
            this.lbTableName.AutoSize = true;
            this.lbTableName.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lbTableName.Location = new System.Drawing.Point(17, 16);
            this.lbTableName.Name = "lbTableName";
            this.lbTableName.Size = new System.Drawing.Size(118, 16);
            this.lbTableName.TabIndex = 9;
            this.lbTableName.Text = "Название таблицы";
            // 
            // lbAfter
            // 
            this.lbAfter.AutoSize = true;
            this.lbAfter.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lbAfter.Location = new System.Drawing.Point(556, 111);
            this.lbAfter.Name = "lbAfter";
            this.lbAfter.Size = new System.Drawing.Size(67, 16);
            this.lbAfter.TabIndex = 11;
            this.lbAfter.Text = "Результат";
            // 
            // lbBefore
            // 
            this.lbBefore.AutoSize = true;
            this.lbBefore.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lbBefore.Location = new System.Drawing.Point(145, 111);
            this.lbBefore.Name = "lbBefore";
            this.lbBefore.Size = new System.Drawing.Size(49, 16);
            this.lbBefore.TabIndex = 10;
            this.lbBefore.Text = "Скрипт";
            // 
            // btnStart
            // 
            this.btnStart.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.btnStart.Location = new System.Drawing.Point(333, 16);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(186, 36);
            this.btnStart.TabIndex = 9;
            this.btnStart.Text = "Выполнить";
            this.btnStart.UseVisualStyleBackColor = true;
            this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
            // 
            // rtbBefore
            // 
            this.rtbBefore.AcceptsTab = true;
            this.rtbBefore.Dock = System.Windows.Forms.DockStyle.Left;
            this.rtbBefore.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.rtbBefore.Location = new System.Drawing.Point(0, 139);
            this.rtbBefore.Name = "rtbBefore";
            this.rtbBefore.Size = new System.Drawing.Size(381, 375);
            this.rtbBefore.TabIndex = 6;
            this.rtbBefore.Text = "";
            // 
            // splitter1
            // 
            this.splitter1.Location = new System.Drawing.Point(381, 139);
            this.splitter1.Name = "splitter1";
            this.splitter1.Size = new System.Drawing.Size(3, 375);
            this.splitter1.TabIndex = 7;
            this.splitter1.TabStop = false;
            // 
            // rtbAfter
            // 
            this.rtbAfter.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rtbAfter.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.rtbAfter.Location = new System.Drawing.Point(384, 139);
            this.rtbAfter.Name = "rtbAfter";
            this.rtbAfter.ReadOnly = true;
            this.rtbAfter.Size = new System.Drawing.Size(419, 375);
            this.rtbAfter.TabIndex = 8;
            this.rtbAfter.Text = "";
            // 
            // btnTable
            // 
            this.btnTable.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.btnTable.Location = new System.Drawing.Point(583, 70);
            this.btnTable.Name = "btnTable";
            this.btnTable.Size = new System.Drawing.Size(186, 36);
            this.btnTable.TabIndex = 14;
            this.btnTable.Text = "Сформировать таблицу";
            this.btnTable.UseVisualStyleBackColor = true;
            this.btnTable.Click += new System.EventHandler(this.btnTable_Click);
            // 
            // ExecSQLListForm
            // 
            this.AcceptButton = this.btnStart;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(803, 514);
            this.Controls.Add(this.rtbAfter);
            this.Controls.Add(this.splitter1);
            this.Controls.Add(this.rtbBefore);
            this.Controls.Add(this.panel1);
            this.Name = "ExecSQLListForm";
            this.Text = "Выполнение скрипта";
            this.Resize += new System.EventHandler(this.ExecSQLListForm_Resize);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SaveFileDialog dlgSave;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.RichTextBox rtbBefore;
        private System.Windows.Forms.Splitter splitter1;
        private System.Windows.Forms.RichTextBox rtbAfter;
        private System.Windows.Forms.Label lbAfter;
        private System.Windows.Forms.Label lbBefore;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.RadioButton rbKernel;
        private System.Windows.Forms.RadioButton rbWebdata;
        private System.Windows.Forms.TextBox tbStep;
        private System.Windows.Forms.Label lbStep;
        private System.Windows.Forms.TextBox tbTableName;
        private System.Windows.Forms.Label lbTableName;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnTable;
    }
}