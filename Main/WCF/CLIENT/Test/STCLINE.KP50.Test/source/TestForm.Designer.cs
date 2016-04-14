namespace STCLINE.KP50.Test
{
    partial class TestForm
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
            this.MMenu = new System.Windows.Forms.MenuStrip();
            this.mmEPasp = new System.Windows.Forms.ToolStripMenuItem();
            this.mmNebo = new System.Windows.Forms.ToolStripMenuItem();
            this.pnlTestMethod = new System.Windows.Forms.Panel();
            this.l_TestMethod = new System.Windows.Forms.Label();
            this.pnlTestResult = new System.Windows.Forms.Panel();
            this.l_TestResult = new System.Windows.Forms.Label();
            this.MMenu.SuspendLayout();
            this.pnlTestMethod.SuspendLayout();
            this.pnlTestResult.SuspendLayout();
            this.SuspendLayout();
            // 
            // MMenu
            // 
            this.MMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mmEPasp,
            this.mmNebo});
            this.MMenu.Location = new System.Drawing.Point(0, 0);
            this.MMenu.Name = "MMenu";
            this.MMenu.Padding = new System.Windows.Forms.Padding(8, 2, 0, 2);
            this.MMenu.Size = new System.Drawing.Size(701, 28);
            this.MMenu.TabIndex = 0;
            this.MMenu.Text = "menuStrip1";
            // 
            // mmEPasp
            // 
            this.mmEPasp.Name = "mmEPasp";
            this.mmEPasp.Size = new System.Drawing.Size(71, 24);
            this.mmEPasp.Text = "&1.EPasp";
            // 
            // mmNebo
            // 
            this.mmNebo.Name = "mmNebo";
            this.mmNebo.Size = new System.Drawing.Size(69, 24);
            this.mmNebo.Text = "&2.Nebo";
            // 
            // pnlTestMethod
            // 
            this.pnlTestMethod.Controls.Add(this.l_TestMethod);
            this.pnlTestMethod.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlTestMethod.Location = new System.Drawing.Point(0, 28);
            this.pnlTestMethod.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.pnlTestMethod.Name = "pnlTestMethod";
            this.pnlTestMethod.Size = new System.Drawing.Size(701, 53);
            this.pnlTestMethod.TabIndex = 1;
            // 
            // l_TestMethod
            // 
            this.l_TestMethod.AutoSize = true;
            this.l_TestMethod.Location = new System.Drawing.Point(11, 10);
            this.l_TestMethod.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.l_TestMethod.Name = "l_TestMethod";
            this.l_TestMethod.Size = new System.Drawing.Size(141, 17);
            this.l_TestMethod.TabIndex = 0;
            this.l_TestMethod.Text = "Тестируемый метод";
            // 
            // pnlTestResult
            // 
            this.pnlTestResult.Controls.Add(this.l_TestResult);
            this.pnlTestResult.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlTestResult.Location = new System.Drawing.Point(0, 81);
            this.pnlTestResult.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.pnlTestResult.Name = "pnlTestResult";
            this.pnlTestResult.Size = new System.Drawing.Size(701, 324);
            this.pnlTestResult.TabIndex = 2;
            // 
            // l_TestResult
            // 
            this.l_TestResult.AutoSize = true;
            this.l_TestResult.Location = new System.Drawing.Point(11, 10);
            this.l_TestResult.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.l_TestResult.Name = "l_TestResult";
            this.l_TestResult.Size = new System.Drawing.Size(87, 17);
            this.l_TestResult.TabIndex = 0;
            this.l_TestResult.Text = "l_TestResult";
            // 
            // TestForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(701, 405);
            this.Controls.Add(this.pnlTestResult);
            this.Controls.Add(this.pnlTestMethod);
            this.Controls.Add(this.MMenu);
            this.MainMenuStrip = this.MMenu;
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.Name = "TestForm";
            this.Text = "КП5.0.Тест";
            this.MMenu.ResumeLayout(false);
            this.MMenu.PerformLayout();
            this.pnlTestMethod.ResumeLayout(false);
            this.pnlTestMethod.PerformLayout();
            this.pnlTestResult.ResumeLayout(false);
            this.pnlTestResult.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip MMenu;
        private System.Windows.Forms.ToolStripMenuItem mmEPasp;
        private System.Windows.Forms.Panel pnlTestMethod;
        private System.Windows.Forms.Label l_TestMethod;
        private System.Windows.Forms.Panel pnlTestResult;
        private System.Windows.Forms.Label l_TestResult;
        private System.Windows.Forms.ToolStripMenuItem mmNebo;
    }
}

