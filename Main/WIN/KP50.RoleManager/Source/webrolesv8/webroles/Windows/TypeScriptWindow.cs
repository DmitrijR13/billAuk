using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace webroles.Windows
{
    public partial class TypeScriptWindow : Form
    {
        public  TypeScript typeScript  { get; set; }
        readonly RadioButton [] radioButtons= new RadioButton[4];
        public TypeScriptWindow()
        {
            InitializeComponent();

            MaximizeBox = false;
            ShowInTaskbar = MinimizeBox = false;

            TableLayoutPanel tableLayPan = new TableLayoutPanel();
            tableLayPan.Parent = this;
            tableLayPan.Padding = new Padding(5);
            tableLayPan.ColumnCount = 2;
            tableLayPan.AutoSize = true;

           
            Button okButton = new System.Windows.Forms.Button();
            Button cancelButton = new System.Windows.Forms.Button();

            string[] radiButtonText = { "", "Только изменения", "Полный для разработчиков", "Полный для заказчиков" };
            // 
            // button1
            // 

            int y = Font.Height;
            for (int i = 0; i < radiButtonText.Length; i++)
            {
                //RadioButton radioButton = new System.Windows.Forms.RadioButton();

                // this.radioButton1.Location = new System.Drawing.Point(3, 3);
                // radioButton.Width = tableLayPan.Size.Width;
                //radioButton.Margin = new System.Windows.Forms.Padding(10, 7, 8, 7);

                // radioButton.Size = new System.Drawing.Size(groupBox1.Width - 20, 17);


                RadioButton radioButton = new System.Windows.Forms.RadioButton();
                if (i == 0)
                {
                    radioButton.Visible = false;
                    radioButtons[i] = radioButton;
                    continue;
                }
                tableLayPan.SetColumnSpan(radioButton, 2);
                tableLayPan.Controls.Add(radioButton);
                tableLayPan.RowStyles.Add(new System.Windows.Forms.RowStyle());
                radioButton.Dock = System.Windows.Forms.DockStyle.Fill;
                radioButton.Location = new System.Drawing.Point(Font.Height + 3, 2 * y * i);
                radioButton.Name = "radioButton" + i;
                radioButton.Text = radiButtonText[i];
                radioButton.AutoSize = true;
                radioButton.UseVisualStyleBackColor = true;
              //  radioButton.Size = new System.Drawing.Size(groupBox1.Width-20, 17);
                if (i == 0) radioButton.Checked = true;
                radioButtons[i] = radioButton;

               // y += radioButton.Height+5;
            }


            tableLayPan.SuspendLayout();
            okButton.Location = new System.Drawing.Point(13, radioButtons[radioButtons.Length - 1].Location.Y + 40);
            okButton.Size = new System.Drawing.Size(75, 30);
            okButton.TabIndex = 1;
            okButton.Click += okButton_Click;
            okButton.Text = "ОК";
            okButton.Margin = new System.Windows.Forms.Padding(10, 10, 10, 10);
            okButton.UseVisualStyleBackColor = true;
            tableLayPan.Controls.Add(okButton);
            tableLayPan.RowStyles.Add(new System.Windows.Forms.RowStyle());

            cancelButton.Location = new System.Drawing.Point(tableLayPan.Width - cancelButton.Size.Width - y * 2, radioButtons[radioButtons.Length - 1].Location.Y + 40);
            cancelButton.Size = new System.Drawing.Size(75, 30);
            cancelButton.TabIndex = 2;
            cancelButton.Click += cancelButton_Click;
            cancelButton.Text = "Отмена";
            cancelButton.UseVisualStyleBackColor = true;
            cancelButton.Margin = new System.Windows.Forms.Padding(80, 10, 10, 10);
            tableLayPan.Controls.Add(cancelButton);
            tableLayPan.ResumeLayout();
            tableLayPan.RowStyles.Add(new System.Windows.Forms.RowStyle());

            AcceptButton = okButton;
            CancelButton = cancelButton;
            tableLayPan.Dock = DockStyle.Fill;

        }

        void okButton_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < radioButtons.Length; i++)
            {
                if (!radioButtons[i].Checked) continue;
                typeScript = (TypeScript)i;
                break;
            }
            DialogResult = DialogResult.OK;
        }

        void cancelButton_Click(object sender, EventArgs e)
        {
           DialogResult= DialogResult.Cancel;
        }
           

     
    }
}
