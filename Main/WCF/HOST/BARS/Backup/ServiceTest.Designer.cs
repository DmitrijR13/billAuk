namespace BARS
{
    partial class ServiceTest
    {
        /// <summary>
        /// Требуется переменная конструктора.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Освободить все используемые ресурсы.
        /// </summary>
        /// <param name="disposing">истинно, если управляемый ресурс должен быть удален; иначе ложно.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Код, автоматически созданный конструктором форм Windows

        /// <summary>
        /// Обязательный метод для поддержки конструктора - не изменяйте
        /// содержимое данного метода при помощи редактора кода.
        /// </summary>
        private void InitializeComponent()
        {
            this.button1 = new System.Windows.Forms.Button();
            this.txtAcc = new System.Windows.Forms.TextBox();
            this.txtFlat = new System.Windows.Forms.TextBox();
            this.lblRes = new System.Windows.Forms.Label();
            this.lblCompany = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(105, 94);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 0;
            this.button1.Text = "button1";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // txtAcc
            // 
            this.txtAcc.Location = new System.Drawing.Point(44, 44);
            this.txtAcc.Name = "txtAcc";
            this.txtAcc.Size = new System.Drawing.Size(100, 20);
            this.txtAcc.TabIndex = 1;
            // 
            // txtFlat
            // 
            this.txtFlat.Location = new System.Drawing.Point(162, 44);
            this.txtFlat.Name = "txtFlat";
            this.txtFlat.Size = new System.Drawing.Size(100, 20);
            this.txtFlat.TabIndex = 2;
            // 
            // lblRes
            // 
            this.lblRes.AutoSize = true;
            this.lblRes.Location = new System.Drawing.Point(82, 152);
            this.lblRes.Name = "lblRes";
            this.lblRes.Size = new System.Drawing.Size(35, 13);
            this.lblRes.TabIndex = 3;
            this.lblRes.Text = "label1";
            // 
            // lblCompany
            // 
            this.lblCompany.AutoSize = true;
            this.lblCompany.Location = new System.Drawing.Point(82, 175);
            this.lblCompany.Name = "lblCompany";
            this.lblCompany.Size = new System.Drawing.Size(35, 13);
            this.lblCompany.TabIndex = 4;
            this.lblCompany.Text = "label1";
            // 
            // ServiceTest
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(679, 277);
            this.Controls.Add(this.lblCompany);
            this.Controls.Add(this.lblRes);
            this.Controls.Add(this.txtFlat);
            this.Controls.Add(this.txtAcc);
            this.Controls.Add(this.button1);
            this.Name = "ServiceTest";
            this.Text = "Form1";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.TextBox txtAcc;
        private System.Windows.Forms.TextBox txtFlat;
        private System.Windows.Forms.Label lblRes;
        private System.Windows.Forms.Label lblCompany;
    }
}

