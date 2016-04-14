namespace RabbitTest
{
    partial class Form1
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
            this.messageTb = new System.Windows.Forms.TextBox();
            this.sendBtn = new System.Windows.Forms.Button();
            this.resultTb = new System.Windows.Forms.RichTextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.hostTb = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.queueTb = new System.Windows.Forms.TextBox();
            this.listenerBt = new System.Windows.Forms.Button();
            this.label4 = new System.Windows.Forms.Label();
            this.clearBtn = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // messageTb
            // 
            this.messageTb.Location = new System.Drawing.Point(84, 63);
            this.messageTb.Name = "messageTb";
            this.messageTb.Size = new System.Drawing.Size(570, 22);
            this.messageTb.TabIndex = 0;
            this.messageTb.Text = "Hello world";
            // 
            // sendBtn
            // 
            this.sendBtn.Location = new System.Drawing.Point(676, 63);
            this.sendBtn.Name = "sendBtn";
            this.sendBtn.Size = new System.Drawing.Size(119, 23);
            this.sendBtn.TabIndex = 1;
            this.sendBtn.Text = "Send message";
            this.sendBtn.UseVisualStyleBackColor = true;
            this.sendBtn.Click += new System.EventHandler(this.sendBtn_Click);
            // 
            // resultTb
            // 
            this.resultTb.Location = new System.Drawing.Point(11, 131);
            this.resultTb.Name = "resultTb";
            this.resultTb.Size = new System.Drawing.Size(784, 271);
            this.resultTb.TabIndex = 2;
            this.resultTb.Text = "";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 105);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(48, 17);
            this.label1.TabIndex = 3;
            this.label1.Text = "Result";
            // 
            // hostTb
            // 
            this.hostTb.Location = new System.Drawing.Point(84, 9);
            this.hostTb.Name = "hostTb";
            this.hostTb.Size = new System.Drawing.Size(209, 22);
            this.hostTb.TabIndex = 4;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(41, 12);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(37, 17);
            this.label2.TabIndex = 5;
            this.label2.Text = "Host";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(321, 12);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(51, 17);
            this.label3.TabIndex = 6;
            this.label3.Text = "Queue";
            // 
            // queueTb
            // 
            this.queueTb.Location = new System.Drawing.Point(378, 9);
            this.queueTb.Name = "queueTb";
            this.queueTb.Size = new System.Drawing.Size(276, 22);
            this.queueTb.TabIndex = 7;
            this.queueTb.Text = "test";
            // 
            // listenerBt
            // 
            this.listenerBt.Location = new System.Drawing.Point(676, 9);
            this.listenerBt.Name = "listenerBt";
            this.listenerBt.Size = new System.Drawing.Size(119, 23);
            this.listenerBt.TabIndex = 8;
            this.listenerBt.Text = "Set listener";
            this.listenerBt.UseVisualStyleBackColor = true;
            this.listenerBt.Click += new System.EventHandler(this.listenerBt_Click);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(13, 66);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(65, 17);
            this.label4.TabIndex = 9;
            this.label4.Text = "Message";
            // 
            // clearBtn
            // 
            this.clearBtn.Location = new System.Drawing.Point(67, 102);
            this.clearBtn.Name = "clearBtn";
            this.clearBtn.Size = new System.Drawing.Size(116, 23);
            this.clearBtn.TabIndex = 10;
            this.clearBtn.Text = "Clear result";
            this.clearBtn.UseVisualStyleBackColor = true;
            this.clearBtn.Click += new System.EventHandler(this.clearBtn_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(807, 412);
            this.Controls.Add(this.clearBtn);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.listenerBt);
            this.Controls.Add(this.queueTb);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.hostTb);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.resultTb);
            this.Controls.Add(this.sendBtn);
            this.Controls.Add(this.messageTb);
            this.Name = "Form1";
            this.Text = "Rabbit channel test";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox messageTb;
        private System.Windows.Forms.Button sendBtn;
        private System.Windows.Forms.RichTextBox resultTb;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox hostTb;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox queueTb;
        private System.Windows.Forms.Button listenerBt;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Button clearBtn;
    }
}

