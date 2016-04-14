namespace STCLINE.KP50.HostMan.SOURCE
{
    partial class FrmCreateNewBank
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
            this.lblNewBankPrefix = new System.Windows.Forms.Label();
            this.tbxNewBankPrefix = new System.Windows.Forms.TextBox();
            this.btnAddNewBank = new System.Windows.Forms.Button();
            this.tbxLog = new System.Windows.Forms.TextBox();
            this.tbxNewBankName = new System.Windows.Forms.TextBox();
            this.lblNewBankName = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // lblNewBankPrefix
            // 
            this.lblNewBankPrefix.AutoSize = true;
            this.lblNewBankPrefix.Location = new System.Drawing.Point(9, 15);
            this.lblNewBankPrefix.Name = "lblNewBankPrefix";
            this.lblNewBankPrefix.Size = new System.Drawing.Size(127, 13);
            this.lblNewBankPrefix.TabIndex = 0;
            this.lblNewBankPrefix.Text = "Префикс нового банка:";
            // 
            // tbxNewBankPrefix
            // 
            this.tbxNewBankPrefix.Enabled = false;
            this.tbxNewBankPrefix.Location = new System.Drawing.Point(142, 12);
            this.tbxNewBankPrefix.Name = "tbxNewBankPrefix";
            this.tbxNewBankPrefix.Size = new System.Drawing.Size(180, 20);
            this.tbxNewBankPrefix.TabIndex = 1;
            // 
            // btnAddNewBank
            // 
            this.btnAddNewBank.Enabled = false;
            this.btnAddNewBank.Location = new System.Drawing.Point(328, 10);
            this.btnAddNewBank.Name = "btnAddNewBank";
            this.btnAddNewBank.Size = new System.Drawing.Size(75, 23);
            this.btnAddNewBank.TabIndex = 2;
            this.btnAddNewBank.Text = "Создать";
            this.btnAddNewBank.UseVisualStyleBackColor = true;
            this.btnAddNewBank.Click += new System.EventHandler(this.btnAddNewBank_Click);
            // 
            // tbxLog
            // 
            this.tbxLog.Location = new System.Drawing.Point(12, 64);
            this.tbxLog.Multiline = true;
            this.tbxLog.Name = "tbxLog";
            this.tbxLog.ReadOnly = true;
            this.tbxLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.tbxLog.Size = new System.Drawing.Size(391, 290);
            this.tbxLog.TabIndex = 3;
            // 
            // tbxNewBankName
            // 
            this.tbxNewBankName.Enabled = false;
            this.tbxNewBankName.Location = new System.Drawing.Point(142, 38);
            this.tbxNewBankName.Name = "tbxNewBankName";
            this.tbxNewBankName.Size = new System.Drawing.Size(180, 20);
            this.tbxNewBankName.TabIndex = 4;
            this.tbxNewBankName.Text = "Новый банк данных";
            // 
            // lblNewBankName
            // 
            this.lblNewBankName.AutoSize = true;
            this.lblNewBankName.Location = new System.Drawing.Point(9, 41);
            this.lblNewBankName.Name = "lblNewBankName";
            this.lblNewBankName.Size = new System.Drawing.Size(103, 13);
            this.lblNewBankName.TabIndex = 5;
            this.lblNewBankName.Text = "Имя нового банка:";
            // 
            // FrmCreateNewBank
            // 
            this.AcceptButton = this.btnAddNewBank;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(416, 366);
            this.Controls.Add(this.lblNewBankName);
            this.Controls.Add(this.tbxNewBankName);
            this.Controls.Add(this.tbxLog);
            this.Controls.Add(this.btnAddNewBank);
            this.Controls.Add(this.tbxNewBankPrefix);
            this.Controls.Add(this.lblNewBankPrefix);
            this.Name = "FrmCreateNewBank";
            this.Text = "FrmCreateNewBank";
            this.Load += new System.EventHandler(this.FrmCreateNewBank_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblNewBankPrefix;
        private System.Windows.Forms.TextBox tbxNewBankPrefix;
        private System.Windows.Forms.Button btnAddNewBank;
        private System.Windows.Forms.TextBox tbxLog;
        private System.Windows.Forms.TextBox tbxNewBankName;
        private System.Windows.Forms.Label lblNewBankName;
    }
}