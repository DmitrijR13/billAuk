using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using STCLine.Utils;
using System.Windows.Forms;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.Test
{
    internal class EPaspTest
    {
        public EPaspTest(ToolStripMenuItem menu)
            : base()
        {
            InitializeComponentMenu(menu);
        }

        public Label l_TestResult
        {
            get { return ClassNikTestUtils.l_Test; }
        }

        private void InitializeComponentMenu(ToolStripMenuItem menu)
        {
            ClassNikTestUtils.AddMenuTestItem(menu, new ClassNikTestUtils.TestDelegate[]
            {
                this.TestPrepareEPaspXml,
                null,
                this.TestSelectServiceSample
            });
        }
        private void InitializeTest()
        {
            //_frmTest.onGridUtils.ClearListDgv();
            l_TestResult.Text = "";
        }


        public void TestPrepareEPaspXml()
        {
            //InitializeTest();

            //IntfResultType res = RemoteKP50.GetData<IntfResultType>(delegate(I_EPasp cli)
            //{
            //    return cli.UnloadEPaspXml(2012, 06, 1);
            //});

            //l_TestResult.Text = l_TestResult.Text + res.resultCode.ToString() + ", " + res.resultMessage + '\n';
        }
        public void TestSelectServiceSample()
        {
            InitializeTest();

            IntfResultType res = RemoteKP50.GetData<IntfResultType>(delegate(I_EPasp cli)
            {
                return cli.SelectServiceSample();
            });

            l_TestResult.Text = l_TestResult.Text + res.resultCode.ToString() + ", " + res.resultMessage + '\n';

        }
        

    }
}
