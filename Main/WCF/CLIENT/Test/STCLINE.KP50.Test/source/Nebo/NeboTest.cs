using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using STCLine.Utils;
using System.Windows.Forms;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.Test
{
    internal partial class NeboTest
    {
        public NeboTest(ToolStripMenuItem menu, ClassOnGridUtils onGridUtils)
            : base()
        {
            InitializeComponentMenu(menu);

            this.onGridUtils = onGridUtils;
        }

        public readonly ClassOnGridUtils onGridUtils = null;

        public Label l_TestResult
        {
            get { return ClassNikTestUtils.l_Test; }
        }

        private void InitializeComponentMenu(ToolStripMenuItem menu)
        {
            ClassNikTestUtils.AddMenuTestItem(menu, new ClassNikTestUtils.TestDelegate[]
            {
                this.TestGetServiceList,
                this.TestGetDomList,
                this.TestGetSupplierList,
                this.TestGetRentersList,                
                this.TestGetAreaList,
                this.GetReestrInfo,
                this.TestGetSaldoReestr,
                this.TestGetSuppReestr,
                this.TestGetPaymentReestr
            });
        }
        private void InitializeTest()
        {
            //_frmTest.onGridUtils.ClearListDgv();
            l_TestResult.Text = "";
        }


        public void TestGetServiceList()
        {
            InitializeTest();

            IntfResultObjectType<List<NeboService>> res = RemoteKP50.GetData<IntfResultObjectType<List<NeboService>>>(delegate(I_Nebo cli)
            {
                return cli.GetServiceList(68, new RequestPaging() { curPageNumber = 1 });
            });

            onGridUtils.ObjectOnGrid<NeboService>(res.resultData, "GetServiceList");

            l_TestResult.Text = l_TestResult.Text + res.resultCode.ToString() + ", " + res.resultMessage + '\n';
        }

        public void TestGetDomList()
        {
            InitializeTest();

            IntfResultObjectType<List<NeboDom>> res = RemoteKP50.GetData<IntfResultObjectType<List<NeboDom>>>(delegate(I_Nebo cli)
            {
                return cli.GetDomList(68, new RequestPaging() { curPageNumber = 1 });
            });

            onGridUtils.ObjectOnGrid<NeboDom>(res.resultData, "GetDomList");

            l_TestResult.Text = l_TestResult.Text + res.resultCode.ToString() + ", " + res.resultMessage + '\n';
        }

        public void TestGetSupplierList()
        {
            InitializeTest();

            IntfResultObjectType<List<NeboSupplier>> res = RemoteKP50.GetData<IntfResultObjectType<List<NeboSupplier>>>(delegate(I_Nebo cli)
            {
                return cli.GetSupplierList(68, new RequestPaging() { curPageNumber = 1 });
            });

            onGridUtils.ObjectOnGrid<NeboSupplier>(res.resultData, "GetSupplierList");

            l_TestResult.Text = l_TestResult.Text + res.resultCode.ToString() + ", " + res.resultMessage + '\n';
        }

        public void TestGetRentersList()
        {
            InitializeTest();

            IntfResultObjectType<List<NeboRenters>> res = RemoteKP50.GetData<IntfResultObjectType<List<NeboRenters>>>(delegate(I_Nebo cli)
            {
                return cli.GetRentersList(68, new RequestPaging() { curPageNumber = 1 });
            });

            onGridUtils.ObjectOnGrid<NeboRenters>(res.resultData, "GetRentersList");

            l_TestResult.Text = l_TestResult.Text + res.resultCode.ToString() + ", " + res.resultMessage + '\n';
        }

        public void TestGetAreaList()
        {
            InitializeTest();

            IntfResultObjectType<List<NeboArea>> res = RemoteKP50.GetData<IntfResultObjectType<List<NeboArea>>>(delegate(I_Nebo cli)
            {
                return cli.GetAreaList(68);
            });

            onGridUtils.ObjectOnGrid<NeboArea>(res.resultData, "GetAreaList");

            l_TestResult.Text = l_TestResult.Text + res.resultCode.ToString() + ", " + res.resultMessage + '\n';
        }

        public void GetReestrInfo()
        {
            InitializeTest();

            IntfResultObjectType<List<NeboReestr>> res = RemoteKP50.GetData<IntfResultObjectType<List<NeboReestr>>>(delegate(I_Nebo cli)
            {
                return cli.GetReestrInfo(0);
            });

            onGridUtils.ObjectOnGrid<NeboReestr>(res.resultData, "GetReestrInfo");

            l_TestResult.Text = l_TestResult.Text + res.resultCode.ToString() + ", " + res.resultMessage + '\n';
        }


        public void TestGetSaldoReestr()
        {
            InitializeTest();

            NeboSaldo neboSaldo = new NeboSaldo();
            neboSaldo.nzp_area = 56;
            neboSaldo.nzp_nebo_reestr = 5;
            neboSaldo.page_number = 1;
            IntfResultObjectType<List<NeboSaldo>> res = RemoteKP50.GetData<IntfResultObjectType<List<NeboSaldo>>>(delegate(I_Nebo cli)
            {
                return cli.GetSaldoReestr(neboSaldo, new RequestPaging() { curPageNumber = 1 });
            });

            onGridUtils.ObjectOnGrid<NeboSaldo>(res.resultData, "GetSaldoReestr");

            l_TestResult.Text = l_TestResult.Text + res.resultCode.ToString() + ", " + res.resultMessage + '\n';
        }

        public void TestGetSuppReestr()
        {
            InitializeTest();

            NeboSupp neboSupp = new NeboSupp();
            neboSupp.nzp_nebo_reestr = 39;
            neboSupp.nzp_area = 68;
            neboSupp.page_number = 1;
            IntfResultObjectType<List<NeboSupp>> res = RemoteKP50.GetData<IntfResultObjectType<List<NeboSupp>>>(delegate(I_Nebo cli)
            {
                return cli.GetSuppReestr(neboSupp);
            });

            onGridUtils.ObjectOnGrid<NeboSupp>(res.resultData, "GetSuppReestr");

            l_TestResult.Text = l_TestResult.Text + res.resultCode.ToString() + ", " + res.resultMessage + '\n';
        }

        public void TestGetPaymentReestr()
        {
            InitializeTest();

            IntfResultObjectType<List<NeboPaymentReestr>> res = RemoteKP50.GetData<IntfResultObjectType<List<NeboPaymentReestr>>>(delegate(I_Nebo cli)
            {
                return cli.GetPaymentReestr(38, 56, new RequestPaging() { curPageNumber = 5 });
            });

            onGridUtils.ObjectOnGrid<NeboPaymentReestr>(res.resultData, "GetPaymentReestr");

            l_TestResult.Text = l_TestResult.Text + res.resultCode.ToString() + ", " + res.resultMessage + '\n';
        }
    }
}
