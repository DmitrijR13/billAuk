namespace updater
{
    partial class GetMonLogForm
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
            this.lbBegDate = new System.Windows.Forms.Label();
            this.dtpckBegDate = new System.Windows.Forms.DateTimePicker();
            this.dtpckEndDate = new System.Windows.Forms.DateTimePicker();
            this.lbEndDate = new System.Windows.Forms.Label();
            this.pnlGetMonLog = new System.Windows.Forms.Panel();
            this.chbError = new System.Windows.Forms.CheckBox();
            this.chbWarning = new System.Windows.Forms.CheckBox();
            this.chbInfo = new System.Windows.Forms.CheckBox();
            this.dtpckEndTime = new System.Windows.Forms.DateTimePicker();
            this.dtpckBegTime = new System.Windows.Forms.DateTimePicker();
            this.btnGetLog = new System.Windows.Forms.Button();
            this.rtbGetMonLog = new System.Windows.Forms.RichTextBox();
            this.pnlGetMonLog.SuspendLayout();
            this.SuspendLayout();
            // 
            // lbBegDate
            // 
            this.lbBegDate.AutoSize = true;
            this.lbBegDate.Location = new System.Drawing.Point(13, 18);
            this.lbBegDate.Name = "lbBegDate";
            this.lbBegDate.Size = new System.Drawing.Size(88, 13);
            this.lbBegDate.TabIndex = 0;
            this.lbBegDate.Text = "Начальная дата";
            // 
            // dtpckBegDate
            // 
            this.dtpckBegDate.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpckBegDate.Location = new System.Drawing.Point(107, 12);
            this.dtpckBegDate.Name = "dtpckBegDate";
            this.dtpckBegDate.Size = new System.Drawing.Size(94, 20);
            this.dtpckBegDate.TabIndex = 1;
            // 
            // dtpckEndDate
            // 
            this.dtpckEndDate.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpckEndDate.Location = new System.Drawing.Point(107, 54);
            this.dtpckEndDate.Name = "dtpckEndDate";
            this.dtpckEndDate.Size = new System.Drawing.Size(94, 20);
            this.dtpckEndDate.TabIndex = 3;
            // 
            // lbEndDate
            // 
            this.lbEndDate.AutoSize = true;
            this.lbEndDate.Location = new System.Drawing.Point(13, 60);
            this.lbEndDate.Name = "lbEndDate";
            this.lbEndDate.Size = new System.Drawing.Size(81, 13);
            this.lbEndDate.TabIndex = 2;
            this.lbEndDate.Text = "Конечная дата";
            // 
            // pnlGetMonLog
            // 
            this.pnlGetMonLog.Controls.Add(this.chbError);
            this.pnlGetMonLog.Controls.Add(this.chbWarning);
            this.pnlGetMonLog.Controls.Add(this.chbInfo);
            this.pnlGetMonLog.Controls.Add(this.dtpckEndTime);
            this.pnlGetMonLog.Controls.Add(this.dtpckBegTime);
            this.pnlGetMonLog.Controls.Add(this.btnGetLog);
            this.pnlGetMonLog.Controls.Add(this.lbBegDate);
            this.pnlGetMonLog.Controls.Add(this.lbEndDate);
            this.pnlGetMonLog.Controls.Add(this.dtpckEndDate);
            this.pnlGetMonLog.Controls.Add(this.dtpckBegDate);
            this.pnlGetMonLog.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlGetMonLog.Location = new System.Drawing.Point(0, 0);
            this.pnlGetMonLog.Name = "pnlGetMonLog";
            this.pnlGetMonLog.Size = new System.Drawing.Size(798, 90);
            this.pnlGetMonLog.TabIndex = 4;
            // 
            // chbError
            // 
            this.chbError.AutoSize = true;
            this.chbError.Checked = true;
            this.chbError.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chbError.Location = new System.Drawing.Point(585, 12);
            this.chbError.Name = "chbError";
            this.chbError.Size = new System.Drawing.Size(66, 17);
            this.chbError.TabIndex = 9;
            this.chbError.Text = "Ошибки";
            this.chbError.UseVisualStyleBackColor = true;
            this.chbError.CheckedChanged += new System.EventHandler(this.chbError_CheckedChanged);
            // 
            // chbWarning
            // 
            this.chbWarning.AutoSize = true;
            this.chbWarning.Checked = true;
            this.chbWarning.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chbWarning.Location = new System.Drawing.Point(447, 12);
            this.chbWarning.Name = "chbWarning";
            this.chbWarning.Size = new System.Drawing.Size(113, 17);
            this.chbWarning.TabIndex = 8;
            this.chbWarning.Text = "Предупреждения";
            this.chbWarning.UseVisualStyleBackColor = true;
            this.chbWarning.CheckedChanged += new System.EventHandler(this.chbWarning_CheckedChanged);
            // 
            // chbInfo
            // 
            this.chbInfo.AutoSize = true;
            this.chbInfo.Checked = true;
            this.chbInfo.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chbInfo.Location = new System.Drawing.Point(324, 12);
            this.chbInfo.Name = "chbInfo";
            this.chbInfo.Size = new System.Drawing.Size(92, 17);
            this.chbInfo.TabIndex = 7;
            this.chbInfo.Text = "Информация";
            this.chbInfo.UseVisualStyleBackColor = true;
            this.chbInfo.CheckedChanged += new System.EventHandler(this.chbInfo_CheckedChanged);
            // 
            // dtpckEndTime
            // 
            this.dtpckEndTime.Format = System.Windows.Forms.DateTimePickerFormat.Time;
            this.dtpckEndTime.Location = new System.Drawing.Point(207, 53);
            this.dtpckEndTime.Name = "dtpckEndTime";
            this.dtpckEndTime.Size = new System.Drawing.Size(82, 20);
            this.dtpckEndTime.TabIndex = 6;
            // 
            // dtpckBegTime
            // 
            this.dtpckBegTime.Format = System.Windows.Forms.DateTimePickerFormat.Time;
            this.dtpckBegTime.Location = new System.Drawing.Point(207, 11);
            this.dtpckBegTime.Name = "dtpckBegTime";
            this.dtpckBegTime.Size = new System.Drawing.Size(82, 20);
            this.dtpckBegTime.TabIndex = 5;
            // 
            // btnGetLog
            // 
            this.btnGetLog.Location = new System.Drawing.Point(324, 50);
            this.btnGetLog.Name = "btnGetLog";
            this.btnGetLog.Size = new System.Drawing.Size(157, 23);
            this.btnGetLog.TabIndex = 4;
            this.btnGetLog.Text = "Получить Лог";
            this.btnGetLog.UseVisualStyleBackColor = true;
            this.btnGetLog.Click += new System.EventHandler(this.btnGetLog_Click);
            // 
            // rtbGetMonLog
            // 
            this.rtbGetMonLog.BackColor = System.Drawing.SystemColors.Window;
            this.rtbGetMonLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rtbGetMonLog.Location = new System.Drawing.Point(0, 90);
            this.rtbGetMonLog.Name = "rtbGetMonLog";
            this.rtbGetMonLog.ReadOnly = true;
            this.rtbGetMonLog.Size = new System.Drawing.Size(798, 400);
            this.rtbGetMonLog.TabIndex = 5;
            this.rtbGetMonLog.Text = "";
            // 
            // GetMonLogForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(798, 490);
            this.Controls.Add(this.rtbGetMonLog);
            this.Controls.Add(this.pnlGetMonLog);
            this.Name = "GetMonLogForm";
            this.Text = "GetMonitorLogs";
            this.Load += new System.EventHandler(this.GetMonitorLogs_Load);
            this.pnlGetMonLog.ResumeLayout(false);
            this.pnlGetMonLog.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label lbBegDate;
        private System.Windows.Forms.DateTimePicker dtpckBegDate;
        private System.Windows.Forms.DateTimePicker dtpckEndDate;
        private System.Windows.Forms.Label lbEndDate;
        private System.Windows.Forms.Panel pnlGetMonLog;
        private System.Windows.Forms.RichTextBox rtbGetMonLog;
        private System.Windows.Forms.Button btnGetLog;
        private System.Windows.Forms.DateTimePicker dtpckEndTime;
        private System.Windows.Forms.DateTimePicker dtpckBegTime;
        private System.Windows.Forms.CheckBox chbError;
        private System.Windows.Forms.CheckBox chbWarning;
        private System.Windows.Forms.CheckBox chbInfo;
    }
}