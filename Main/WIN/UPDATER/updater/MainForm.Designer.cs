namespace updater
{
    partial class MainForm
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
            this.main_menu = new System.Windows.Forms.MenuStrip();
            this.файлToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmUpdUpd = new System.Windows.Forms.ToolStripMenuItem();
            this.зашифроватьSQLToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.dATAToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.kERNELToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.собратьВерсиюToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.RequestToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.sQLToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.monitolLogToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.свойЗапросToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.выполнитьPHPToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.настройкиToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.dgv_rajons = new System.Windows.Forms.DataGridView();
            this.dlgOpen = new System.Windows.Forms.OpenFileDialog();
            this.dlgSave = new System.Windows.Forms.SaveFileDialog();
            this.panel1 = new System.Windows.Forms.Panel();
            this.btn_update = new System.Windows.Forms.Button();
            this.btn_history = new System.Windows.Forms.Button();
            this.main_menu.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgv_rajons)).BeginInit();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // main_menu
            // 
            this.main_menu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.файлToolStripMenuItem,
            this.RequestToolStripMenuItem,
            this.настройкиToolStripMenuItem});
            this.main_menu.Location = new System.Drawing.Point(0, 0);
            this.main_menu.Name = "main_menu";
            this.main_menu.Padding = new System.Windows.Forms.Padding(9, 3, 0, 3);
            this.main_menu.Size = new System.Drawing.Size(1239, 29);
            this.main_menu.TabIndex = 3;
            this.main_menu.Text = "menuStrip1";
            // 
            // файлToolStripMenuItem
            // 
            this.файлToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsmUpdUpd,
            this.зашифроватьSQLToolStripMenuItem,
            this.собратьВерсиюToolStripMenuItem});
            this.файлToolStripMenuItem.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.файлToolStripMenuItem.Name = "файлToolStripMenuItem";
            this.файлToolStripMenuItem.Size = new System.Drawing.Size(53, 23);
            this.файлToolStripMenuItem.Text = "Файл";
            // 
            // tsmUpdUpd
            // 
            this.tsmUpdUpd.Name = "tsmUpdUpd";
            this.tsmUpdUpd.Size = new System.Drawing.Size(195, 24);
            this.tsmUpdUpd.Text = "Обновить Updater";
            this.tsmUpdUpd.Click += new System.EventHandler(this.tsmUpdUpd_Click);
            // 
            // зашифроватьSQLToolStripMenuItem
            // 
            this.зашифроватьSQLToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.dATAToolStripMenuItem,
            this.kERNELToolStripMenuItem});
            this.зашифроватьSQLToolStripMenuItem.Name = "зашифроватьSQLToolStripMenuItem";
            this.зашифроватьSQLToolStripMenuItem.Size = new System.Drawing.Size(195, 24);
            this.зашифроватьSQLToolStripMenuItem.Text = "Зашифровать SQL";
            // 
            // dATAToolStripMenuItem
            // 
            this.dATAToolStripMenuItem.Name = "dATAToolStripMenuItem";
            this.dATAToolStripMenuItem.Size = new System.Drawing.Size(157, 24);
            this.dATAToolStripMenuItem.Text = "DATA (.wd)";
            this.dATAToolStripMenuItem.Click += new System.EventHandler(this.dATAToolStripMenuItem_Click);
            // 
            // kERNELToolStripMenuItem
            // 
            this.kERNELToolStripMenuItem.Name = "kERNELToolStripMenuItem";
            this.kERNELToolStripMenuItem.Size = new System.Drawing.Size(157, 24);
            this.kERNELToolStripMenuItem.Text = "KERNEL (.wk)";
            this.kERNELToolStripMenuItem.Click += new System.EventHandler(this.kERNELToolStripMenuItem_Click);
            // 
            // собратьВерсиюToolStripMenuItem
            // 
            this.собратьВерсиюToolStripMenuItem.Name = "собратьВерсиюToolStripMenuItem";
            this.собратьВерсиюToolStripMenuItem.Size = new System.Drawing.Size(195, 24);
            this.собратьВерсиюToolStripMenuItem.Text = "Собрать версию";
            this.собратьВерсиюToolStripMenuItem.Click += new System.EventHandler(this.собратьВерсиюToolStripMenuItem_Click);
            // 
            // RequestToolStripMenuItem
            // 
            this.RequestToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.sQLToolStripMenuItem,
            this.monitolLogToolStripMenuItem,
            this.свойЗапросToolStripMenuItem,
            this.выполнитьPHPToolStripMenuItem});
            this.RequestToolStripMenuItem.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.RequestToolStripMenuItem.Name = "RequestToolStripMenuItem";
            this.RequestToolStripMenuItem.Size = new System.Drawing.Size(66, 23);
            this.RequestToolStripMenuItem.Text = "Запрос";
            // 
            // sQLToolStripMenuItem
            // 
            this.sQLToolStripMenuItem.Name = "sQLToolStripMenuItem";
            this.sQLToolStripMenuItem.Size = new System.Drawing.Size(178, 24);
            this.sQLToolStripMenuItem.Text = "SQL";
            this.sQLToolStripMenuItem.Click += new System.EventHandler(this.sQLToolStripMenuItem_Click);
            // 
            // monitolLogToolStripMenuItem
            // 
            this.monitolLogToolStripMenuItem.Name = "monitolLogToolStripMenuItem";
            this.monitolLogToolStripMenuItem.Size = new System.Drawing.Size(178, 24);
            this.monitolLogToolStripMenuItem.Text = "MonitolLog";
            this.monitolLogToolStripMenuItem.Click += new System.EventHandler(this.monitolLogToolStripMenuItem_Click);
            // 
            // свойЗапросToolStripMenuItem
            // 
            this.свойЗапросToolStripMenuItem.Name = "свойЗапросToolStripMenuItem";
            this.свойЗапросToolStripMenuItem.Size = new System.Drawing.Size(178, 24);
            this.свойЗапросToolStripMenuItem.Text = "Свой запрос";
            this.свойЗапросToolStripMenuItem.Click += new System.EventHandler(this.свойЗапросToolStripMenuItem_Click);
            // 
            // выполнитьPHPToolStripMenuItem
            // 
            this.выполнитьPHPToolStripMenuItem.Name = "выполнитьPHPToolStripMenuItem";
            this.выполнитьPHPToolStripMenuItem.Size = new System.Drawing.Size(178, 24);
            this.выполнитьPHPToolStripMenuItem.Text = "Выполнить PHP";
            this.выполнитьPHPToolStripMenuItem.Click += new System.EventHandler(this.выполнитьPHPToolStripMenuItem_Click);
            // 
            // настройкиToolStripMenuItem
            // 
            this.настройкиToolStripMenuItem.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.настройкиToolStripMenuItem.Name = "настройкиToolStripMenuItem";
            this.настройкиToolStripMenuItem.Size = new System.Drawing.Size(89, 23);
            this.настройкиToolStripMenuItem.Text = "Настройки";
            // 
            // dgv_rajons
            // 
            this.dgv_rajons.AllowUserToAddRows = false;
            this.dgv_rajons.AllowUserToResizeColumns = false;
            this.dgv_rajons.AllowUserToResizeRows = false;
            this.dgv_rajons.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.dgv_rajons.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgv_rajons.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
            this.dgv_rajons.BackgroundColor = System.Drawing.Color.Gray;
            this.dgv_rajons.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgv_rajons.Location = new System.Drawing.Point(0, 35);
            this.dgv_rajons.Margin = new System.Windows.Forms.Padding(4, 4, 0, 4);
            this.dgv_rajons.Name = "dgv_rajons";
            this.dgv_rajons.RowHeadersVisible = false;
            this.dgv_rajons.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgv_rajons.Size = new System.Drawing.Size(1239, 399);
            this.dgv_rajons.TabIndex = 4;
            // 
            // dlgOpen
            // 
            this.dlgOpen.Filter = "7z files|*.7z";
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.btn_update);
            this.panel1.Controls.Add(this.btn_history);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel1.Location = new System.Drawing.Point(0, 438);
            this.panel1.Margin = new System.Windows.Forms.Padding(0, 4, 4, 4);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1239, 46);
            this.panel1.TabIndex = 8;
            // 
            // btn_update
            // 
            this.btn_update.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            this.btn_update.Location = new System.Drawing.Point(13, 4);
            this.btn_update.Margin = new System.Windows.Forms.Padding(4);
            this.btn_update.Name = "btn_update";
            this.btn_update.Size = new System.Drawing.Size(143, 36);
            this.btn_update.TabIndex = 10;
            this.btn_update.Text = "Обновить";
            this.btn_update.UseVisualStyleBackColor = true;
            this.btn_update.Click += new System.EventHandler(this.button1_Click);
            // 
            // btn_history
            // 
            this.btn_history.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            this.btn_history.Location = new System.Drawing.Point(206, 4);
            this.btn_history.Margin = new System.Windows.Forms.Padding(4);
            this.btn_history.Name = "btn_history";
            this.btn_history.Size = new System.Drawing.Size(143, 36);
            this.btn_history.TabIndex = 8;
            this.btn_history.Text = "История";
            this.btn_history.UseVisualStyleBackColor = true;
            this.btn_history.Click += new System.EventHandler(this.button2_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 19F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.ClientSize = new System.Drawing.Size(1239, 484);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.dgv_rajons);
            this.Controls.Add(this.main_menu);
            this.Font = new System.Drawing.Font("Tahoma", 12F);
            this.MainMenuStrip = this.main_menu;
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Программа управления";
            this.main_menu.ResumeLayout(false);
            this.main_menu.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgv_rajons)).EndInit();
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip main_menu;
        private System.Windows.Forms.ToolStripMenuItem файлToolStripMenuItem;
        private System.Windows.Forms.DataGridView dgv_rajons;
        private System.Windows.Forms.ToolStripMenuItem RequestToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem sQLToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem monitolLogToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem tsmUpdUpd;
        private System.Windows.Forms.OpenFileDialog dlgOpen;
        private System.Windows.Forms.ToolStripMenuItem зашифроватьSQLToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem dATAToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem kERNELToolStripMenuItem;
        private System.Windows.Forms.SaveFileDialog dlgSave;
        private System.Windows.Forms.ToolStripMenuItem свойЗапросToolStripMenuItem;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button btn_update;
        private System.Windows.Forms.Button btn_history;
        private System.Windows.Forms.ToolStripMenuItem собратьВерсиюToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem выполнитьPHPToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem настройкиToolStripMenuItem;
    }
}

