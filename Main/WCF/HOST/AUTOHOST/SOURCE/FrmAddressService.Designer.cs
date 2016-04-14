namespace STCLINE.KP50.HostMan.AddressService
{
    partial class FrmAddressService
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
            this.panel2 = new System.Windows.Forms.Panel();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnOk = new System.Windows.Forms.Button();
            this.panel1 = new System.Windows.Forms.Panel();
            this.btnClearHouse = new System.Windows.Forms.Button();
            this.cbHouse = new System.Windows.Forms.ComboBox();
            this.label6 = new System.Windows.Forms.Label();
            this.btnClearStreet = new System.Windows.Forms.Button();
            this.cbStreet = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            this.btnClearSettlement = new System.Windows.Forms.Button();
            this.btnClearDistrict = new System.Windows.Forms.Button();
            this.btnClearRegion = new System.Windows.Forms.Button();
            this.cbRegion = new System.Windows.Forms.ComboBox();
            this.label5 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.cbDistrict = new System.Windows.Forms.ComboBox();
            this.cbSettlement = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.panel2.SuspendLayout();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.btnCancel);
            this.panel2.Controls.Add(this.btnOk);
            this.panel2.Location = new System.Drawing.Point(11, 142);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(405, 34);
            this.panel2.TabIndex = 15;
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(309, 3);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(96, 23);
            this.btnCancel.TabIndex = 5;
            this.btnCancel.Text = "Отменить";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // btnOk
            // 
            this.btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOk.Location = new System.Drawing.Point(197, 4);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(103, 23);
            this.btnOk.TabIndex = 4;
            this.btnOk.Text = "Загрузить";
            this.btnOk.UseVisualStyleBackColor = true;
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.btnClearHouse);
            this.panel1.Controls.Add(this.cbHouse);
            this.panel1.Controls.Add(this.label6);
            this.panel1.Controls.Add(this.btnClearStreet);
            this.panel1.Controls.Add(this.cbStreet);
            this.panel1.Controls.Add(this.label4);
            this.panel1.Controls.Add(this.btnClearSettlement);
            this.panel1.Controls.Add(this.btnClearDistrict);
            this.panel1.Controls.Add(this.btnClearRegion);
            this.panel1.Controls.Add(this.cbRegion);
            this.panel1.Controls.Add(this.label5);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Controls.Add(this.cbDistrict);
            this.panel1.Controls.Add(this.cbSettlement);
            this.panel1.Controls.Add(this.label3);
            this.panel1.Location = new System.Drawing.Point(12, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(404, 140);
            this.panel1.TabIndex = 14;
            // 
            // btnClearHouse
            // 
            this.btnClearHouse.Enabled = false;
            this.btnClearHouse.Location = new System.Drawing.Point(341, 113);
            this.btnClearHouse.Name = "btnClearHouse";
            this.btnClearHouse.Size = new System.Drawing.Size(63, 23);
            this.btnClearHouse.TabIndex = 21;
            this.btnClearHouse.Text = "Очистить";
            this.btnClearHouse.UseVisualStyleBackColor = true;
            // 
            // cbHouse
            // 
            this.cbHouse.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbHouse.Enabled = false;
            this.cbHouse.FormattingEnabled = true;
            this.cbHouse.Location = new System.Drawing.Point(106, 115);
            this.cbHouse.Name = "cbHouse";
            this.cbHouse.Size = new System.Drawing.Size(228, 21);
            this.cbHouse.Sorted = true;
            this.cbHouse.TabIndex = 19;
            this.cbHouse.SelectionChangeCommitted += new System.EventHandler(this.cbHouse_SelectionChangeCommitted);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(-2, 118);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(30, 13);
            this.label6.TabIndex = 20;
            this.label6.Text = "Дом";
            // 
            // btnClearStreet
            // 
            this.btnClearStreet.Enabled = false;
            this.btnClearStreet.Location = new System.Drawing.Point(341, 86);
            this.btnClearStreet.Name = "btnClearStreet";
            this.btnClearStreet.Size = new System.Drawing.Size(63, 23);
            this.btnClearStreet.TabIndex = 18;
            this.btnClearStreet.Text = "Очистить";
            this.btnClearStreet.UseVisualStyleBackColor = true;
            // 
            // cbStreet
            // 
            this.cbStreet.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbStreet.Enabled = false;
            this.cbStreet.FormattingEnabled = true;
            this.cbStreet.Location = new System.Drawing.Point(106, 88);
            this.cbStreet.Name = "cbStreet";
            this.cbStreet.Size = new System.Drawing.Size(228, 21);
            this.cbStreet.Sorted = true;
            this.cbStreet.TabIndex = 16;
            this.cbStreet.SelectionChangeCommitted += new System.EventHandler(this.cbStreet_SelectionChangeCommitted);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(-2, 91);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(39, 13);
            this.label4.TabIndex = 17;
            this.label4.Text = "Улица";
            // 
            // btnClearSettlement
            // 
            this.btnClearSettlement.Enabled = false;
            this.btnClearSettlement.Location = new System.Drawing.Point(341, 61);
            this.btnClearSettlement.Name = "btnClearSettlement";
            this.btnClearSettlement.Size = new System.Drawing.Size(63, 23);
            this.btnClearSettlement.TabIndex = 15;
            this.btnClearSettlement.Text = "Очистить";
            this.btnClearSettlement.UseVisualStyleBackColor = true;
            // 
            // btnClearDistrict
            // 
            this.btnClearDistrict.Enabled = false;
            this.btnClearDistrict.Location = new System.Drawing.Point(341, 38);
            this.btnClearDistrict.Name = "btnClearDistrict";
            this.btnClearDistrict.Size = new System.Drawing.Size(63, 23);
            this.btnClearDistrict.TabIndex = 13;
            this.btnClearDistrict.Text = "Очистить";
            this.btnClearDistrict.UseVisualStyleBackColor = true;
            // 
            // btnClearRegion
            // 
            this.btnClearRegion.Location = new System.Drawing.Point(341, 11);
            this.btnClearRegion.Name = "btnClearRegion";
            this.btnClearRegion.Size = new System.Drawing.Size(63, 23);
            this.btnClearRegion.TabIndex = 12;
            this.btnClearRegion.Text = "Очистить";
            this.btnClearRegion.UseVisualStyleBackColor = true;
            // 
            // cbRegion
            // 
            this.cbRegion.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbRegion.FormattingEnabled = true;
            this.cbRegion.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.cbRegion.Location = new System.Drawing.Point(106, 11);
            this.cbRegion.Name = "cbRegion";
            this.cbRegion.Size = new System.Drawing.Size(228, 21);
            this.cbRegion.Sorted = true;
            this.cbRegion.TabIndex = 0;
            this.cbRegion.SelectionChangeCommitted += new System.EventHandler(this.cbRegion_SelectionChangeCommitted);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(-2, 40);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(73, 13);
            this.label5.TabIndex = 11;
            this.label5.Text = "Район/Город";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(-2, 14);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(43, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Регион";
            // 
            // cbDistrict
            // 
            this.cbDistrict.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbDistrict.Enabled = false;
            this.cbDistrict.FormattingEnabled = true;
            this.cbDistrict.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.cbDistrict.Location = new System.Drawing.Point(106, 37);
            this.cbDistrict.Name = "cbDistrict";
            this.cbDistrict.Size = new System.Drawing.Size(228, 21);
            this.cbDistrict.Sorted = true;
            this.cbDistrict.TabIndex = 10;
            this.cbDistrict.SelectionChangeCommitted += new System.EventHandler(this.cbDistrict_SelectionChangeCommitted);
            // 
            // cbSettlement
            // 
            this.cbSettlement.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbSettlement.Enabled = false;
            this.cbSettlement.FormattingEnabled = true;
            this.cbSettlement.Location = new System.Drawing.Point(106, 63);
            this.cbSettlement.Name = "cbSettlement";
            this.cbSettlement.Size = new System.Drawing.Size(228, 21);
            this.cbSettlement.Sorted = true;
            this.cbSettlement.TabIndex = 6;
            this.cbSettlement.SelectionChangeCommitted += new System.EventHandler(this.cbSettlement_SelectionChangeCommitted);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(-2, 66);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(102, 13);
            this.label3.TabIndex = 7;
            this.label3.Text = "Населенный пункт";
            // 
            // FrmAddressService
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(428, 176);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.Name = "FrmAddressService";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Адресный сервис";
            this.panel2.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button btnClearHouse;
        private System.Windows.Forms.ComboBox cbHouse;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Button btnClearStreet;
        private System.Windows.Forms.ComboBox cbStreet;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Button btnClearSettlement;
        private System.Windows.Forms.Button btnClearDistrict;
        private System.Windows.Forms.Button btnClearRegion;
        private System.Windows.Forms.ComboBox cbRegion;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox cbDistrict;
        private System.Windows.Forms.ComboBox cbSettlement;
        private System.Windows.Forms.Label label3;

    }
}