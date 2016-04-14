using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using webroles;
using webroles.GenerateScriptTable;
using webroles.TransferData;

namespace webroles.Windows
{
    public partial class SelectProfileWindow : Form
    {
        public int  NzpPage { get; set; }
        public ProfilesEnum ProfileEnum { get; set; }
        private List<RadioButton> RadioButtons=new List<RadioButton>();
        public Dictionary<ProfilesEnum, string> radiButtonTextVals;
        public bool IsNoProfiles { get; set; }
        public SelectProfileWindow(int nzp_page)
        {
            NzpPage = nzp_page;
            getProfilesList();
            if (radiButtonTextVals.Count == 0)
            {
                IsNoProfiles = true;
                return;
            }
            InitializeComponent();
            TableLayoutPanel tableLayPan = new TableLayoutPanel();
            tableLayPan.Parent = this;
            tableLayPan.Padding = new Padding(5);
            tableLayPan.ColumnCount = 2;
            tableLayPan.AutoSize = true;

            //tableLayPan.CellBorderStyle = TableLayoutPanelCellBorderStyle.InsetDouble;
            // tableLayPan.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom;
            tableLayPan.SuspendLayout();
            Text = "Выберите профиль";
            MaximizeBox = false;
            MinimizeBox = false;

            ShowInTaskbar = MinimizeBox = false;

            Button okButton = new System.Windows.Forms.Button();
            Button cancelButton = new System.Windows.Forms.Button();

            // button1
            // 
            tableLayPan.SuspendLayout();
            int y = Font.Height;
            int i = 0;
            foreach (KeyValuePair<ProfilesEnum, string> rad in radiButtonTextVals)
            {
                RadioButton radioButton = new System.Windows.Forms.RadioButton();
                tableLayPan.SetColumnSpan(radioButton, 2);
                tableLayPan.Controls.Add(radioButton);
                tableLayPan.RowStyles.Add(new System.Windows.Forms.RowStyle());
               // this.radioButton1.Location = new System.Drawing.Point(3, 3);
                radioButton.Dock = System.Windows.Forms.DockStyle.Fill;
                radioButton.UseVisualStyleBackColor = true;
                radioButton.Location = new System.Drawing.Point(Font.Height + 3, 2 * y * i);
                radioButton.Name = "radioButton" + (int)rad.Key;
                radioButton.Text = rad.Value;
                radioButton.AutoSize = true;
                // radioButton.Width = tableLayPan.Size.Width;
                //radioButton.Margin = new System.Windows.Forms.Padding(10, 7, 8, 7);

                // radioButton.Size = new System.Drawing.Size(groupBox1.Width - 20, 17);
                if (i == 0) radioButton.Checked = true;
                RadioButtons.Add(radioButton);
                i++;
                //y += radioButton.Height + 5;
            }



            okButton.TabIndex = 1;
            okButton.AutoSize = true;
            okButton.Click += okButton_Click;
            okButton.Text = "ОК";
            okButton.Parent = tableLayPan;
            okButton.Location = new System.Drawing.Point(13, RadioButtons[RadioButtons.Count - 1].Location.Y + 40);
            okButton.Size = new System.Drawing.Size(75, 27);
            okButton.Margin = new System.Windows.Forms.Padding(10, 10, 10, 10);
            okButton.UseVisualStyleBackColor = true;
            tableLayPan.Controls.Add(okButton);
            tableLayPan.RowStyles.Add(new System.Windows.Forms.RowStyle());


            cancelButton.TabIndex = 2;
            cancelButton.Click += cancelButton_Click;
            cancelButton.Parent = tableLayPan;
            cancelButton.Text = "Отмена";
            cancelButton.UseVisualStyleBackColor = true;
            cancelButton.AutoSize = true;
            cancelButton.Size = new System.Drawing.Size(75, 27);
            cancelButton.Location = new System.Drawing.Point(tableLayPan.Width - cancelButton.Size.Width - y * 2, RadioButtons[RadioButtons.Count - 1].Location.Y + 40);
            tableLayPan.Controls.Add(cancelButton);
            tableLayPan.ResumeLayout();
            tableLayPan.RowStyles.Add(new System.Windows.Forms.RowStyle());

            //cancelButton.Margin = new System.Windows.Forms.Padding(80, 10, 10, 10);

           
         //   Height = tableLayPan.Height;
           // Width = tableLayPan.Width;
            tableLayPan.Dock = DockStyle.Fill;
           
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            foreach (RadioButton t in RadioButtons)
            {
                if (!t.Checked) continue;
                foreach (KeyValuePair<ProfilesEnum, string> rad in radiButtonTextVals)
                {
                    if (rad.Value != t.Text) continue;
                    ProfileEnum = rad.Key;
                    break;
                }
                break;
            }
            DialogResult = DialogResult.OK;
            
        }

        private void getProfilesList()
        {

            radiButtonTextVals = new Dictionary<ProfilesEnum, string>();
            string sql = "select distinct p.id, p.profile_name  from "+DBManager.sDefaultSchema+"profile_roles pr, "
                         +DBManager.sDefaultSchema+"role_pages r, " +DBManager.sDefaultSchema+Tables.profiles+" p "+
                         "where r.nzp_role=pr.nzp_role and pr.profile_id=p.id  and r.nzp_page=" + NzpPage +" order by id";
            IDbConnection connection = ConnectionToPostgreSqlDb.GetConnection();
            IDataReader reader = null;
            Returns ret = new Returns(true, "");
            try
            {
                connection.Open();
                reader = TransferDataDb.ExecuteReader(sql, out ret, connection);
                while (reader.Read())
                {
                    if (reader["id"] == DBNull.Value) continue;
                    radiButtonTextVals.Add((ProfilesEnum)reader["id"], reader["profile_name"].ToString());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                if (reader != null) reader.Close();
                if (connection != null) connection.Close();
            }    
        }



    }
}
