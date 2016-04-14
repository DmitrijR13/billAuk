using System.Windows.Forms;

namespace FormatForm
{
    partial class AddFormat
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AddFormat));
            this.displayFileName = new System.Windows.Forms.TextBox();
            this.openButton = new System.Windows.Forms.Button();
            this.FormatCollection = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.okButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.lbFormatName = new System.Windows.Forms.Label();
            this.cbEndSymbol = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // displayFileName
            // 
            this.displayFileName.Location = new System.Drawing.Point(24, 20);
            this.displayFileName.Name = "displayFileName";
            this.displayFileName.ReadOnly = true;
            this.displayFileName.Size = new System.Drawing.Size(405, 20);
            this.displayFileName.TabIndex = 0;
            // 
            // openButton
            // 
            this.openButton.Location = new System.Drawing.Point(435, 17);
            this.openButton.Name = "openButton";
            this.openButton.Size = new System.Drawing.Size(75, 23);
            this.openButton.TabIndex = 1;
            this.openButton.Text = "Выбрать";
            this.openButton.UseVisualStyleBackColor = true;
            this.openButton.Click += new System.EventHandler(this.openButton_Click);
            // 
            // FormatCollection
            // 
            this.FormatCollection.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.FormatCollection.FormattingEnabled = true;
            this.FormatCollection.Location = new System.Drawing.Point(198, 46);
            this.FormatCollection.Name = "FormatCollection";
            this.FormatCollection.Size = new System.Drawing.Size(312, 21);
            this.FormatCollection.TabIndex = 2;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(21, 53);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(171, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "Выберите необходимый формат";
            // 
            // okButton
            // 
            this.okButton.Location = new System.Drawing.Point(354, 86);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(75, 23);
            this.okButton.TabIndex = 4;
            this.okButton.Text = "Ок";
            this.okButton.UseVisualStyleBackColor = true;
            this.okButton.Click += new System.EventHandler(this.okButton_Click);
            // 
            // cancelButton
            // 
            this.cancelButton.Location = new System.Drawing.Point(435, 86);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(75, 23);
            this.cancelButton.TabIndex = 5;
            this.cancelButton.Text = "Отмена";
            this.cancelButton.UseVisualStyleBackColor = true;
            this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
            // 
            // lbFormatName
            // 
            this.lbFormatName.AutoSize = true;
            this.lbFormatName.Location = new System.Drawing.Point(21, 96);
            this.lbFormatName.Name = "lbFormatName";
            this.lbFormatName.Size = new System.Drawing.Size(7, 13);
            this.lbFormatName.TabIndex = 6;
            this.lbFormatName.Text = "\r\n";
            // 
            // cbEndSymbol
            // 
            this.cbEndSymbol.AutoSize = true;
            this.cbEndSymbol.Location = new System.Drawing.Point(24, 73);
            this.cbEndSymbol.Name = "cbEndSymbol";
            this.cbEndSymbol.Size = new System.Drawing.Size(232, 17);
            this.cbEndSymbol.TabIndex = 7;
            this.cbEndSymbol.Text = "Формат без завершающего символа \"|\"";
            this.cbEndSymbol.UseVisualStyleBackColor = true;
            // 
            // AddFormat
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(522, 121);
            this.Controls.Add(this.cbEndSymbol);
            this.Controls.Add(this.lbFormatName);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.FormatCollection);
            this.Controls.Add(this.openButton);
            this.Controls.Add(this.displayFileName);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximumSize = new System.Drawing.Size(538, 160);
            this.MinimumSize = new System.Drawing.Size(538, 160);
            this.Name = "AddFormat";
            this.Text = "Добавление нового формата";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox displayFileName;
        private System.Windows.Forms.Button openButton;
        private System.Windows.Forms.ComboBox FormatCollection;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Button cancelButton;
        private Label lbFormatName;
        private CheckBox cbEndSymbol;
    }
}