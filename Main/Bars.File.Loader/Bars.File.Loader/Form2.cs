using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Bars.File.Loader
{
    public partial class Form2 : Form
    {
        public string connection { get; set; }
        public string directiry { get; set; }
        public Form2(string con, string p)
        {
            InitializeComponent();
            textBox1.Text = con;
            textBox2.Text = p;
        }

        private void Form2_Load(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            //connection = textBox1.Text;
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            //directiry = textBox2.Text;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            connection = textBox1.Text;
            directiry = textBox2.Text;
            Close();
        }

    }
}
