using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace STCLINE.KP50.HostMan.SOURCE
{
    public partial class FrmConfigManager : Form
    {
        private ConfigManager objConfigManager;
        private int intSelectedGroupID;
        private string strSelectedGroupName;
        private List<ConfigFile> cfgFile;

        private void Update(bool UpdateComboBox)
        {
            if (this.intSelectedGroupID > -1)
            {
                this.cfgFile = this.objConfigManager.GetFilesList(this.intSelectedGroupID);
                if (UpdateComboBox)
                {
                    //int intSelectedItem = -1;
                    this.comboBoxGroups.Items.Clear();
                    for (int i = 0; i < objConfigManager.GroupsNames.Count; i++)
                    {
                        ComboBoxItem item = new ComboBoxItem();
                        item.Text = objConfigManager.GroupsNames[i].GroupName;
                        item.Value = objConfigManager.GroupsNames[i].GroupID;
                        this.comboBoxGroups.Items.Add(item);
                    }
                }
                this.dataGridViewFiles.Rows.Clear();
                foreach (ConfigFile fl in this.cfgFile)
                {
                    this.dataGridViewFiles.Rows.Add();
                    this.dataGridViewFiles.Rows[this.dataGridViewFiles.Rows.Count - 1].Cells["File"].Value = fl.OriginalName;
                    this.dataGridViewFiles.Rows[this.dataGridViewFiles.Rows.Count - 1].Cells["Path"].Value = fl.OutputDirectory;
                }
            }
            if (this.dataGridViewFiles.Rows.Count > 0)
            {
                this.toolStripButtonDeleteFile.Enabled = true;
                this.toolStripButtonLoad.Enabled = true;
            }
            else
            {
                this.toolStripButtonDeleteFile.Enabled = false;
                this.toolStripButtonLoad.Enabled = false;
            }

            if (objConfigManager.GroupsNames.Count > 0)
            {
                this.comboBoxGroups.Enabled = true;
                this.dataGridViewFiles.Enabled = true;
                this.toolStrip.Enabled = true;
            }
            else
            {
                this.comboBoxGroups.Enabled = false;
                this.dataGridViewFiles.Enabled = false;
                this.toolStrip.Enabled = false;
            }
        }

        public FrmConfigManager()
        {
            InitializeComponent();
            this.objConfigManager = new ConfigManager("ConfigManager.xml");
            try { 
                objConfigManager.Load();
                this.Update(true);
            }
            catch (Exception) { }
        }

        private void buttonCreateGroup_Click(object sender, EventArgs e)
        {
            if (this.textBoxNewGroupName.Text != "")
            {
                this.objConfigManager.AddGroup(this.textBoxNewGroupName.Text);
                this.objConfigManager.Save();
                this.Update(true);
            }
        }

        private void buttonDropGroup_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(String.Format("Удалить группу \"{0}\" [GID: {1}]", this.strSelectedGroupName, this.intSelectedGroupID), "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.Yes)
            {
                objConfigManager.DeleteGroup(this.strSelectedGroupName);
                objConfigManager.Save();
                this.Update(true);
            }
        }

        private void comboBoxGroups_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.comboBoxGroups.SelectedIndex > -1)
            {
                this.intSelectedGroupID = Convert.ToInt32((this.comboBoxGroups.SelectedItem as ComboBoxItem).Value.ToString());
                this.strSelectedGroupName = (this.comboBoxGroups.SelectedItem as ComboBoxItem).Text;
            }
            this.Update(false);
        }

        private void toolStripButtonAddFile_Click(object sender, EventArgs e)
        {
            if (this.openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                this.objConfigManager.AddFile(this.intSelectedGroupID, this.openFileDialog.FileName);
                this.objConfigManager.Save();
                this.Update(false);
            }
            
        }

        private void toolStripButtonDeleteFile_Click(object sender, EventArgs e)
        {
            if (this.dataGridViewFiles.SelectedRows.Count > 0)
            {
                if (MessageBox.Show("Удалить выбранный файл?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.Yes)
                {
                    this.objConfigManager.DeleteFile(this.intSelectedGroupID, this.dataGridViewFiles.SelectedRows[0].Cells["File"].Value.ToString());
                    this.objConfigManager.Save();
                    this.comboBoxGroups.Items.Clear();
                    this.Update(false);
                }
            }
        }

        private void toolStripButtonLoad_Click(object sender, EventArgs e) { 
            this.objConfigManager.RestoreFiles(this.intSelectedGroupID);
            MessageBox.Show("Операция завершена.");
        }
    }

    class ComboBoxItem
    {
        public string Text { get; set; }
        public object Value { get; set; }

        public override string ToString()
        {
            return Text;
        }
    }
}
