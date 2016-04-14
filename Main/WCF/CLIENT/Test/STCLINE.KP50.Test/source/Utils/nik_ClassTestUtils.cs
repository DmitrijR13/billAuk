using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace STCLine.Utils
{

    public class ClassNikTestUtils
    {
        public delegate void TestDelegate();

        public static Label l_TestMethod = null;
        public static Label l_Test = null;

        private static void AddMenuItem(ToolStripMenuItem menu, string name, string text, EventHandler click)
        {
            ToolStripMenuItem mn = new System.Windows.Forms.ToolStripMenuItem() { Name = name, Text = text };
            mn.Click += new EventHandler(click);
            menu.DropDownItems.Add(mn);
        }
        private static void AddMenuTestItem(ToolStripMenuItem menu, string name, string text, TestDelegate testDelegate)
        {
            AddMenuItem(menu, name, text,
                delegate(object sender, EventArgs e)
                {
                    string methodMenu = text;
                    ClassNikTestUtils.l_TestMethod.Text = "Тестируемый метод: " + methodMenu + "(" + testDelegate.Method.Name + ")" + '\n' + "Выполняется...";
                    ClassNikTestUtils.l_Test.Text = "";
                    ClassNikTestUtils.l_Test.Parent.Refresh();
                    ClassNikTestUtils.l_TestMethod.Parent.Refresh();
                    try
                    {
                        testDelegate();
                    }
                    catch (Exception ex)
                    {
                        ClassNikTestUtils.l_Test.Text = ex.Message;
                    }
                    ClassNikTestUtils.l_TestMethod.Text = "Тестируемый метод: " + methodMenu + "(" + testDelegate.Method.Name + ")" + '\n' + "Готово.";
                    ClassNikTestUtils.l_Test.Text = "Результат:" + '\n' + ClassNikTestUtils.l_Test.Text;
                    ClassNikTestUtils.l_Test.Parent.Refresh();
                    ClassNikTestUtils.l_TestMethod.Parent.Refresh();
                });
        }
        private static void AddMenuTestItem(ToolStripMenuItem menu, TestDelegate testDelegate)
        {
            AddMenuTestItem(menu, menu.Name + "_" + testDelegate.Method.Name, testDelegate.Method.Name, testDelegate);
        }
        public static void AddMenuTestItem(ToolStripMenuItem menu, TestDelegate[] testDelegates)
        {
            foreach (TestDelegate dlg in testDelegates)
            {
                if (dlg != null)
                {
                    AddMenuTestItem(menu, dlg);
                }
                else
                {
                    AddMenuSeparator(menu);
                }
            }
        }
        private static void AddMenuSeparator(ToolStripMenuItem menu)
        {
            menu.DropDownItems.Add(new ToolStripSeparator());
        }
    }

    public abstract class ClassTestData
    {
        public static DataGridView CreateDataGridView<T>(T testData, Control parent, Size size, string tabName) where T : class, new()
        {
            //string TabName = testData.GetType().ToString();
            DataGridView dgv = new DataGridView()
            {
                Name = "dgv" + tabName,
                Parent = parent,
                Dock = DockStyle.Top,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = false
            };
            dgv.Size = new Size(size.Width, size.Height);
            if (testData is Array || testData is ICollection)
            {
                dgv.DataSource = testData;
            }
            else
            {
                dgv.DataSource = new T[] { testData };
            }
            dgv.BringToFront();

            if (dgv.Rows.Count <= 1 && dgv.Columns.Count > 15)
            {
                dgv.Size = new Size(300, 70);
            }
            else if (dgv.Rows.Count <= 1)
            {
                dgv.Size = new Size(300, 50);
            }

            foreach (DataGridViewColumn col in dgv.Columns)
            {
                col.Width = 80;
            }

            return dgv;
        }
        public static DataGridView CreateDataGridView<T>(T testData, Control parent, Size size) where T : class, new()
        {
            return CreateDataGridView<T>(testData, parent, size, testData.GetType().ToString());
        }
        public static DataGridView CreateDataGridView<T>(T testData, Control parent) where T : ClassTestData, new()
        {
            return CreateDataGridView<T>(testData, parent, new Size(300, 50), "TestData");

        }
    }

    public class ClassOnGridUtils
    {
        public ClassOnGridUtils(Control parentControl)
        {
            this.parentControl = parentControl;
        }

        private readonly List<DataGridView> listDgv = new List<DataGridView>(0);
        public List<DataGridView> ListDgv
        {
            get { return listDgv; }
        }

        private readonly Control parentControl = null;

        public DataGridView AddDataGridView(string tabName)
        {
            DataGridView dgv = this.listDgv.Find((delegate(DataGridView item) { return item.Name == "dgv" + tabName; }));
            if (dgv == null)
            {
                GroupBox gbox = new GroupBox()
                {
                    Text = tabName,
                    Parent = parentControl,
                    Dock = DockStyle.Top,
                    Size = new Size(300, 150)
                };
                dgv = new DataGridView()
                {
                    Name = "dgv" + tabName,
                    Parent = gbox,
                    Dock = DockStyle.Fill,
                    AllowUserToAddRows = false,
                    AllowUserToDeleteRows = false,
                    ReadOnly = true
                };
                dgv.BringToFront();
            }

            listDgv.Add(dgv);
            return dgv;
        }
        public void ClearListDgv()
        {
            foreach (DataGridView dgv in listDgv)
            {
                Control gbox = dgv.Parent;
                dgv.Parent = null;
                dgv.Dispose();
                gbox.Parent = null;
                gbox.Dispose();
            }
            listDgv.Clear();
        }
        public void DataSetOnGrid(DataSet ds)
        {
            this.ClearListDgv();
            if (ds == null)
            {
                return;
            }

            foreach (DataTable dt in ds.Tables)
            {
                DataGridView dgv = this.AddDataGridView(dt.TableName);
                dgv.DataSource = dt;

                GroupBox gbox = ((GroupBox)(dgv.Parent));
                gbox.BringToFront();
                gbox.Text = ((GroupBox)(dgv.Parent)).Text + "(" + dt.Rows.Count.ToString() + ")";

                if (dt.Rows.Count <= 1 && dt.Columns.Count > 15)
                {
                    gbox.Size = new Size(300, 85);
                }
                else if (dt.Rows.Count <= 1)
                {
                    gbox.Size = new Size(300, 70);
                }
            }
        }

        public DataGridView ObjectOnGrid<T>(T[] obj, string tabName) where T : class, new()
        {
            
            DataGridView dgv = this.AddDataGridView(tabName.Trim()!= "" ? tabName.Trim() : obj.GetType().ToString());
            dgv.DataSource = obj;

            GroupBox gbox = ((GroupBox)(dgv.Parent));
            gbox.BringToFront();
            gbox.Text = ((GroupBox)(dgv.Parent)).Text + "(" + dgv.Rows.Count.ToString() + ")";

            if (dgv.Rows.Count <= 1 && dgv.Columns.Count > 15)
            {
                gbox.Size = new Size(300, 85);
            }
            else if (dgv.Rows.Count <= 1)
            {
                gbox.Size = new Size(300, 70);
            }

            foreach (DataGridViewColumn col in dgv.Columns)
            {
                col.Width = 80;                
            }
            return dgv;
        }
        public DataGridView ObjectOnGrid<T>(List<T> obj, string tabName) where T : class, new()
        {

            DataGridView dgv = this.AddDataGridView(tabName.Trim() != "" ? tabName.Trim() : obj.GetType().ToString());
            dgv.DataSource = obj;

            GroupBox gbox = ((GroupBox)(dgv.Parent));
            gbox.BringToFront();
            gbox.Text = ((GroupBox)(dgv.Parent)).Text + "(" + dgv.Rows.Count.ToString() + ")";

            if (dgv.Rows.Count <= 1 && dgv.Columns.Count > 15)
            {
                gbox.Size = new Size(300, 85);
            }
            else if (dgv.Rows.Count <= 1)
            {
                gbox.Size = new Size(300, 70);
            }

            foreach (DataGridViewColumn col in dgv.Columns)
            {
                col.Width = 80;
            }
            return dgv;
        }
        public DataGridView ObjectOnGrid<T>(T obj, string tabName) where T : class, new()
        {
            return ObjectOnGrid<T>(new T[] { obj }, tabName);
        }
        /*
        public void ObjectOnGrid<T>(T[] obj) where T : class, new()
        {
            ObjectOnGrid<T>(obj, obj.GetType().ToString());
        }
        public void ObjectOnGrid<T>(T obj) where T : class, new()
        {
            ObjectOnGrid<T>(new T[] { obj });
        }
         */

    }

}
