namespace STCLINE.KP50.WinLogin
{
    partial class FrmLogin
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
            this.l_Login = new System.Windows.Forms.Label();
            this.tbx_Login = new System.Windows.Forms.TextBox();
            this.tbx_Pwd = new System.Windows.Forms.TextBox();
            this.l_Pwd = new System.Windows.Forms.Label();
            this.b_Ok = new System.Windows.Forms.Button();
            this.b_Cancel = new System.Windows.Forms.Button();
            this.l_Error = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // l_Login
            // 
            this.l_Login.AutoSize = true;
            this.l_Login.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.l_Login.Location = new System.Drawing.Point(34, 33);
            this.l_Login.Name = "l_Login";
            this.l_Login.Size = new System.Drawing.Size(47, 17);
            this.l_Login.TabIndex = 0;
            this.l_Login.Text = "Логин";
            // 
            // tbx_Login
            // 
            this.tbx_Login.Location = new System.Drawing.Point(122, 30);
            this.tbx_Login.Name = "tbx_Login";
            this.tbx_Login.Size = new System.Drawing.Size(144, 20);
            this.tbx_Login.TabIndex = 1;
            // 
            // tbx_Pwd
            // 
            this.tbx_Pwd.Location = new System.Drawing.Point(122, 56);
            this.tbx_Pwd.Name = "tbx_Pwd";
            this.tbx_Pwd.Size = new System.Drawing.Size(144, 20);
            this.tbx_Pwd.TabIndex = 3;
            // 
            // l_Pwd
            // 
            this.l_Pwd.AutoSize = true;
            this.l_Pwd.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.l_Pwd.Location = new System.Drawing.Point(34, 59);
            this.l_Pwd.Name = "l_Pwd";
            this.l_Pwd.Size = new System.Drawing.Size(57, 17);
            this.l_Pwd.TabIndex = 2;
            this.l_Pwd.Text = "Пароль";
            // 
            // b_Ok
            // 
            this.b_Ok.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.b_Ok.Location = new System.Drawing.Point(68, 97);
            this.b_Ok.Name = "b_Ok";
            this.b_Ok.Size = new System.Drawing.Size(75, 23);
            this.b_Ok.TabIndex = 4;
            this.b_Ok.Text = "OK";
            this.b_Ok.UseVisualStyleBackColor = true;
            this.b_Ok.Click += new System.EventHandler(this.b_Ok_Click);
            // 
            // b_Cancel
            // 
            this.b_Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.b_Cancel.Location = new System.Drawing.Point(167, 97);
            this.b_Cancel.Name = "b_Cancel";
            this.b_Cancel.Size = new System.Drawing.Size(75, 23);
            this.b_Cancel.TabIndex = 5;
            this.b_Cancel.Text = "Отмена";
            this.b_Cancel.UseVisualStyleBackColor = true;
            this.b_Cancel.Click += new System.EventHandler(this.b_No_Click);
            // 
            // l_Error
            // 
            this.l_Error.AutoSize = true;
            this.l_Error.ForeColor = System.Drawing.Color.Red;
            this.l_Error.Location = new System.Drawing.Point(34, 9);
            this.l_Error.Name = "l_Error";
            this.l_Error.Size = new System.Drawing.Size(0, 13);
            this.l_Error.TabIndex = 6;
            // 
            // FrmLogin
            // 
            this.AcceptButton = this.b_Ok;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(314, 152);
            this.Controls.Add(this.l_Error);
            this.Controls.Add(this.b_Cancel);
            this.Controls.Add(this.b_Ok);
            this.Controls.Add(this.tbx_Pwd);
            this.Controls.Add(this.l_Pwd);
            this.Controls.Add(this.tbx_Login);
            this.Controls.Add(this.l_Login);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmLogin";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Регистрация в системе";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label l_Login;
        private System.Windows.Forms.TextBox tbx_Login;
        private System.Windows.Forms.TextBox tbx_Pwd;
        private System.Windows.Forms.Label l_Pwd;
        private System.Windows.Forms.Button b_Ok;
        private System.Windows.Forms.Button b_Cancel;
        private System.Windows.Forms.Label l_Error;

    }
}