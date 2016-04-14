namespace STCLINE.KP50.HostMan.KLADR
{
    partial class KLADR
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
            this.cbRegion = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.cbCity = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.btnOk = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.cbSettlement = new System.Windows.Forms.ComboBox();
            this.label5 = new System.Windows.Forms.Label();
            this.cbDistrict = new System.Windows.Forms.ComboBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.btnClearStreet = new System.Windows.Forms.Button();
            this.cbStreet = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            this.btnClearSettlement = new System.Windows.Forms.Button();
            this.btnClearCity = new System.Windows.Forms.Button();
            this.btnClearDistrict = new System.Windows.Forms.Button();
            this.btnClearRegion = new System.Windows.Forms.Button();
            this.panel2 = new System.Windows.Forms.Panel();
            this.cbClearAddressSpace = new System.Windows.Forms.CheckBox();
            this.cbxIgnoreCityDistrict = new System.Windows.Forms.CheckBox();
            this.cbxLoadStreet = new System.Windows.Forms.CheckBox();
            this.ofSTREET = new System.Windows.Forms.OpenFileDialog();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.SuspendLayout();
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
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(-2, 14);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(43, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Регион";
            // 
            // cbCity
            // 
            this.cbCity.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbCity.Enabled = false;
            this.cbCity.FormattingEnabled = true;
            this.cbCity.Location = new System.Drawing.Point(106, 64);
            this.cbCity.Name = "cbCity";
            this.cbCity.Size = new System.Drawing.Size(228, 21);
            this.cbCity.Sorted = true;
            this.cbCity.TabIndex = 2;
            this.cbCity.SelectionChangeCommitted += new System.EventHandler(this.cbCity_SelectionChangeCommitted);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(-2, 67);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(37, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Город";
            // 
            // btnOk
            // 
            this.btnOk.Location = new System.Drawing.Point(302, 11);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(103, 21);
            this.btnOk.TabIndex = 4;
            this.btnOk.Text = "Выгрузить";
            this.btnOk.UseVisualStyleBackColor = true;
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(303, 50);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(103, 24);
            this.btnCancel.TabIndex = 5;
            this.btnCancel.Text = "Отменить";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(-2, 94);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(102, 13);
            this.label3.TabIndex = 7;
            this.label3.Text = "Населенный пункт";
            // 
            // cbSettlement
            // 
            this.cbSettlement.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbSettlement.Enabled = false;
            this.cbSettlement.FormattingEnabled = true;
            this.cbSettlement.Location = new System.Drawing.Point(106, 91);
            this.cbSettlement.Name = "cbSettlement";
            this.cbSettlement.Size = new System.Drawing.Size(228, 21);
            this.cbSettlement.Sorted = true;
            this.cbSettlement.TabIndex = 6;
            this.cbSettlement.SelectionChangeCommitted += new System.EventHandler(this.cbSettlement_SelectionChangeCommitted);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(-2, 40);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(38, 13);
            this.label5.TabIndex = 11;
            this.label5.Text = "Район";
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
            // panel1
            // 
            this.panel1.Controls.Add(this.btnClearStreet);
            this.panel1.Controls.Add(this.cbStreet);
            this.panel1.Controls.Add(this.label4);
            this.panel1.Controls.Add(this.btnClearSettlement);
            this.panel1.Controls.Add(this.btnClearCity);
            this.panel1.Controls.Add(this.btnClearDistrict);
            this.panel1.Controls.Add(this.btnClearRegion);
            this.panel1.Controls.Add(this.cbRegion);
            this.panel1.Controls.Add(this.label5);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Controls.Add(this.cbDistrict);
            this.panel1.Controls.Add(this.cbCity);
            this.panel1.Controls.Add(this.label2);
            this.panel1.Controls.Add(this.cbSettlement);
            this.panel1.Controls.Add(this.label3);
            this.panel1.Location = new System.Drawing.Point(13, 12);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(404, 144);
            this.panel1.TabIndex = 12;
            // 
            // btnClearStreet
            // 
            this.btnClearStreet.Enabled = false;
            this.btnClearStreet.Location = new System.Drawing.Point(341, 114);
            this.btnClearStreet.Name = "btnClearStreet";
            this.btnClearStreet.Size = new System.Drawing.Size(63, 23);
            this.btnClearStreet.TabIndex = 18;
            this.btnClearStreet.Text = "Очистить";
            this.btnClearStreet.UseVisualStyleBackColor = true;
            this.btnClearStreet.Click += new System.EventHandler(this.btnClearStreet_Click);
            // 
            // cbStreet
            // 
            this.cbStreet.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbStreet.Enabled = false;
            this.cbStreet.FormattingEnabled = true;
            this.cbStreet.Location = new System.Drawing.Point(106, 116);
            this.cbStreet.Name = "cbStreet";
            this.cbStreet.Size = new System.Drawing.Size(228, 21);
            this.cbStreet.Sorted = true;
            this.cbStreet.TabIndex = 16;
            this.cbStreet.SelectionChangeCommitted += new System.EventHandler(this.cbStreet_SelectionChangeCommitted);
            this.cbStreet.Click += new System.EventHandler(this.comboBox1_Click);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(-2, 119);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(39, 13);
            this.label4.TabIndex = 17;
            this.label4.Text = "Улица";
            // 
            // btnClearSettlement
            // 
            this.btnClearSettlement.Enabled = false;
            this.btnClearSettlement.Location = new System.Drawing.Point(341, 89);
            this.btnClearSettlement.Name = "btnClearSettlement";
            this.btnClearSettlement.Size = new System.Drawing.Size(63, 23);
            this.btnClearSettlement.TabIndex = 15;
            this.btnClearSettlement.Text = "Очистить";
            this.btnClearSettlement.UseVisualStyleBackColor = true;
            this.btnClearSettlement.Click += new System.EventHandler(this.btnClearSettlement_Click);
            // 
            // btnClearCity
            // 
            this.btnClearCity.Enabled = false;
            this.btnClearCity.Location = new System.Drawing.Point(341, 64);
            this.btnClearCity.Name = "btnClearCity";
            this.btnClearCity.Size = new System.Drawing.Size(63, 23);
            this.btnClearCity.TabIndex = 14;
            this.btnClearCity.Text = "Очистить";
            this.btnClearCity.UseVisualStyleBackColor = true;
            this.btnClearCity.Click += new System.EventHandler(this.btnClearCity_Click);
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
            this.btnClearDistrict.Click += new System.EventHandler(this.btnClearDistrict_Click);
            // 
            // btnClearRegion
            // 
            this.btnClearRegion.Location = new System.Drawing.Point(341, 11);
            this.btnClearRegion.Name = "btnClearRegion";
            this.btnClearRegion.Size = new System.Drawing.Size(63, 23);
            this.btnClearRegion.TabIndex = 12;
            this.btnClearRegion.Text = "Очистить";
            this.btnClearRegion.UseVisualStyleBackColor = true;
            this.btnClearRegion.Click += new System.EventHandler(this.btnClearRegion_Click);
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.cbClearAddressSpace);
            this.panel2.Controls.Add(this.cbxIgnoreCityDistrict);
            this.panel2.Controls.Add(this.btnCancel);
            this.panel2.Controls.Add(this.cbxLoadStreet);
            this.panel2.Controls.Add(this.btnOk);
            this.panel2.Location = new System.Drawing.Point(12, 155);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(405, 90);
            this.panel2.TabIndex = 13;
            // 
            // cbClearAddressSpace
            // 
            this.cbClearAddressSpace.AutoSize = true;
            this.cbClearAddressSpace.Location = new System.Drawing.Point(0, 57);
            this.cbClearAddressSpace.Name = "cbClearAddressSpace";
            this.cbClearAddressSpace.Size = new System.Drawing.Size(197, 17);
            this.cbClearAddressSpace.TabIndex = 8;
            this.cbClearAddressSpace.Text = "Очистить адресное пространство";
            this.cbClearAddressSpace.UseVisualStyleBackColor = true;
            this.cbClearAddressSpace.CheckedChanged += new System.EventHandler(this.cbClearAddressSpace_CheckedChanged);
            // 
            // cbxIgnoreCityDistrict
            // 
            this.cbxIgnoreCityDistrict.AutoSize = true;
            this.cbxIgnoreCityDistrict.Location = new System.Drawing.Point(0, 34);
            this.cbxIgnoreCityDistrict.Name = "cbxIgnoreCityDistrict";
            this.cbxIgnoreCityDistrict.Size = new System.Drawing.Size(195, 17);
            this.cbxIgnoreCityDistrict.TabIndex = 7;
            this.cbxIgnoreCityDistrict.Text = "Игнорировать городские районы";
            this.cbxIgnoreCityDistrict.UseVisualStyleBackColor = true;
            this.cbxIgnoreCityDistrict.CheckedChanged += new System.EventHandler(this.cbxIgnoreCityDistrict_CheckedChanged);
            // 
            // cbxLoadStreet
            // 
            this.cbxLoadStreet.AutoSize = true;
            this.cbxLoadStreet.Location = new System.Drawing.Point(0, 11);
            this.cbxLoadStreet.Name = "cbxLoadStreet";
            this.cbxLoadStreet.Size = new System.Drawing.Size(116, 17);
            this.cbxLoadStreet.TabIndex = 6;
            this.cbxLoadStreet.Text = "Выгружать улицы";
            this.cbxLoadStreet.UseVisualStyleBackColor = true;
            this.cbxLoadStreet.Click += new System.EventHandler(this.cbxLoadStreet_Click);
            // 
            // ofSTREET
            // 
            this.ofSTREET.Filter = "STREET.dbf|STREET.dbf";
            // 
            // KLADR
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(430, 253);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.Name = "KLADR";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Выгрузка из КЛАДР";
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ComboBox cbRegion;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox cbCity;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox cbSettlement;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ComboBox cbDistrict;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Button btnClearSettlement;
        private System.Windows.Forms.Button btnClearCity;
        private System.Windows.Forms.Button btnClearDistrict;
        private System.Windows.Forms.Button btnClearRegion;
        private System.Windows.Forms.Button btnClearStreet;
        private System.Windows.Forms.ComboBox cbStreet;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.CheckBox cbxLoadStreet;
        private System.Windows.Forms.OpenFileDialog ofSTREET;
        private System.Windows.Forms.CheckBox cbxIgnoreCityDistrict;
        private System.Windows.Forms.CheckBox cbClearAddressSpace;
    }
}