namespace STCLINE.KP50.HostMan.SOURCE
{
    partial class FrmEnterPwd
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
            this.b_Cancel = new System.Windows.Forms.Button();
            this.b_Ok = new System.Windows.Forms.Button();
            this.tbx_Pwd = new System.Windows.Forms.TextBox();
            this.l_Pwd = new System.Windows.Forms.Label();
            this.tbx_Login = new System.Windows.Forms.TextBox();
            this.l_Login = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // b_Cancel
            // 
            this.b_Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.b_Cancel.Location = new System.Drawing.Point(231, 124);
            this.b_Cancel.Margin = new System.Windows.Forms.Padding(4);
            this.b_Cancel.Name = "b_Cancel";
            this.b_Cancel.Size = new System.Drawing.Size(100, 28);
            this.b_Cancel.TabIndex = 12;
            this.b_Cancel.Text = "Отмена";
            this.b_Cancel.UseVisualStyleBackColor = true;
            // 
            // b_Ok
            // 
            this.b_Ok.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.b_Ok.Location = new System.Drawing.Point(99, 124);
            this.b_Ok.Margin = new System.Windows.Forms.Padding(4);
            this.b_Ok.Name = "b_Ok";
            this.b_Ok.Size = new System.Drawing.Size(100, 28);
            this.b_Ok.TabIndex = 11;
            this.b_Ok.Text = "OK";
            this.b_Ok.UseVisualStyleBackColor = true;
            this.b_Ok.Click += new System.EventHandler(this.b_Ok_Click);
            // 
            // tbx_Pwd
            // 
            this.tbx_Pwd.Location = new System.Drawing.Point(171, 74);
            this.tbx_Pwd.Margin = new System.Windows.Forms.Padding(4);
            this.tbx_Pwd.Name = "tbx_Pwd";
            this.tbx_Pwd.Size = new System.Drawing.Size(191, 22);
            this.tbx_Pwd.TabIndex = 10;
            // 
            // l_Pwd
            // 
            this.l_Pwd.AutoSize = true;
            this.l_Pwd.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.l_Pwd.Location = new System.Drawing.Point(53, 78);
            this.l_Pwd.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.l_Pwd.Name = "l_Pwd";
            this.l_Pwd.Size = new System.Drawing.Size(72, 20);
            this.l_Pwd.TabIndex = 9;
            this.l_Pwd.Text = "Пароль";
            // 
            // tbx_Login
            // 
            this.tbx_Login.Location = new System.Drawing.Point(171, 42);
            this.tbx_Login.Margin = new System.Windows.Forms.Padding(4);
            this.tbx_Login.Name = "tbx_Login";
            this.tbx_Login.Size = new System.Drawing.Size(191, 22);
            this.tbx_Login.TabIndex = 8;
            // 
            // l_Login
            // 
            this.l_Login.AutoSize = true;
            this.l_Login.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.l_Login.Location = new System.Drawing.Point(53, 46);
            this.l_Login.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.l_Login.Name = "l_Login";
            this.l_Login.Size = new System.Drawing.Size(59, 20);
            this.l_Login.TabIndex = 7;
            this.l_Login.Text = "Логин";
            // 
            // FrmEnterPwd
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(419, 187);
            this.Controls.Add(this.b_Cancel);
            this.Controls.Add(this.b_Ok);
            this.Controls.Add(this.tbx_Pwd);
            this.Controls.Add(this.l_Pwd);
            this.Controls.Add(this.tbx_Login);
            this.Controls.Add(this.l_Login);
            this.Name = "FrmEnterPwd";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Введите пароль";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button b_Cancel;
        private System.Windows.Forms.Button b_Ok;
        private System.Windows.Forms.TextBox tbx_Pwd;
        private System.Windows.Forms.Label l_Pwd;
        private System.Windows.Forms.TextBox tbx_Login;
        private System.Windows.Forms.Label l_Login;

    }
}