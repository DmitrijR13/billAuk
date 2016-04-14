using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using STCLine.Utils;

namespace STCLINE.KP50.Test
{
    public partial class TestForm : Form
    {
        public TestForm()
        {
            InitializeComponent();

            onGridUtils = new ClassOnGridUtils(pnlTestResult);

            epaspTest = new EPaspTest(this.mmEPasp);
            neboTest = new NeboTest(this.mmNebo, onGridUtils);


            ClassNikTestUtils.l_Test = this.l_TestResult;
            ClassNikTestUtils.l_TestMethod = this.l_TestMethod;
            this.l_TestResult.Text = "";
        }

        public readonly ClassOnGridUtils onGridUtils = null;
        private readonly EPaspTest epaspTest = null;
        private readonly NeboTest neboTest = null;

    }
}
