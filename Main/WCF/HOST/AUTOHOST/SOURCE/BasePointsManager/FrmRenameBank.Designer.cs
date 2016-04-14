namespace STCLINE.KP50.HostMan.SOURCE.BasePointsManager
{
    partial class FrmRenameBank
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
            this.tbBankNewName = new System.Windows.Forms.TextBox();
            this.lBankNewName = new System.Windows.Forms.Label();
            this.btnSave = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // tbBankNewName
            // 
            this.tbBankNewName.Location = new System.Drawing.Point(180, 33);
            this.tbBankNewName.MaxLength = 100;
            this.tbBankNewName.Name = "tbBankNewName";
            this.tbBankNewName.Size = new System.Drawing.Size(163, 22);
            this.tbBankNewName.TabIndex = 0;
            // 
            // lBankNewName
            // 
            this.lBankNewName.AutoSize = true;
            this.lBankNewName.Location = new System.Drawing.Point(12, 36);
            this.lBankNewName.Name = "lBankNewName";
            this.lBankNewName.Size = new System.Drawing.Size(166, 17);
            this.lBankNewName.TabIndex = 1;
            this.lBankNewName.Text = "Новое название банка: ";
            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(243, 70);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(100, 28);
            this.btnSave.TabIndex = 2;
            this.btnSave.Text = "Сохранить";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.button1_Click);
            // 
            // FrmRenameBank
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(362, 132);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.lBankNewName);
            this.Controls.Add(this.tbBankNewName);
            this.Name = "FrmRenameBank";
            this.Text = "Изменение банка";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FrmRenameBank_FormClosing);
            this.Load += new System.EventHandler(this.FrmRenameBank_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox tbBankNewName;
        private System.Windows.Forms.Label lBankNewName;
        private System.Windows.Forms.Button btnSave;
    }
}