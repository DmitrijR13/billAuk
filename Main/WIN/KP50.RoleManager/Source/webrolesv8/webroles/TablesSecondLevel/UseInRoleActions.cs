using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Npgsql;
using webroles.TransferData;

namespace webroles.TablesSecondLevel
{
    class UseInRoleActions : TableSecondLevel, IEditable
    {
                DataGridViewColumn[] columnCollection;
        private readonly string nameParentTreeViewNode;
        public UseInRoleActions(string nameParentTreeViewNode)
        {
            this.nameParentTreeViewNode = nameParentTreeViewNode;
        }

        private string selectCommand;
        public override string NameParentTable
        {
            get { return nameParentTreeViewNode; }
        }

        public override string NameParentColumn
        {
            get { return "nzp_page"; }
        }
        public override string NameOwnBaseColumn
        {
            get { return "nzp_page"; }
        }

        private string getSelectCommand()
        {
            return " with ra as (select distinct r.nzp_role, r.nzp_role ||' '|| role as role from role_actions r, s_roles s where nzp_page=" + Position + " and s.nzp_role=r.nzp_role order by nzp_role ), " +
                " tula as (select nzp_role from profile_roles where profile_id=1), " +
                " obn as (select nzp_role from profile_roles where profile_id=10), " +
                " saha as (select nzp_role from profile_roles where profile_id=11) " +
                "select ra.role, " +
                "case when tula.nzp_role is null then 0 else 1 end as tul, " +
                "case when obn.nzp_role is null then 0 else 1 end as ob, " +
                "case when saha.nzp_role is null then 0 else 1 end as sah  " +
                "from  ra left outer join obn on (ra.nzp_role=obn.nzp_role) " +
                " left outer join tula on (ra.nzp_role=tula.nzp_role)  " +
                " left outer join saha on (ra.nzp_role=saha.nzp_role)   Order by ra.nzp_role  ";
        }

        public override DataTable GetDataSource(NpgsqlConnection connect, NpgsqlDataAdapter adapter)
        {
            DataTable dt = new DataTable();
            adapter.SelectCommand.CommandText = SelectCommand;
            TransferDataDb.Fill(adapter, dt);
            return dt;
        }

        public override void GetCorrespondTable(System.Data.DataSet dataSet, Npgsql.NpgsqlDataAdapter adapter, Npgsql.NpgsqlConnection connect, int positionParam)
        {
            selectCommand =
                " with ra as (select distinct r.nzp_role, r.nzp_role ||' '|| role as role from role_actions r, s_roles s where nzp_page=" + positionParam + " and s.nzp_role=r.nzp_role order by nzp_role ), " +
                " tula as (select nzp_role from profile_roles where profile_id=1), " +
                " obn as (select nzp_role from profile_roles where profile_id=10), " +
                " saha as (select nzp_role from profile_roles where profile_id=11) " +
                "select ra.role, " +
                "case when tula.nzp_role is null then 0 else 1 end as tul, " +
                "case when obn.nzp_role is null then 0 else 1 end as ob, " +
                "case when saha.nzp_role is null then 0 else 1 end as sah  " +
                "from  ra left outer join obn on (ra.nzp_role=obn.nzp_role) " +
                " left outer join tula on (ra.nzp_role=tula.nzp_role)  " +
                " left outer join saha on (ra.nzp_role=saha.nzp_role)   Order by ra.nzp_role  ";
            adapter.SelectCommand.CommandText = selectCommand;
            dataSet.Tables[TableName].Clear();
            TransferDataDb.Fill(adapter, dataSet.Tables[TableName]);
        }

        public override string NodeTreeViewText
        {
            get { return "Наличие в действиях ролей (info)";}
        }

        public override string TableName
        {
            get { return Tables.useInRoleActions; }
        }

        public override string SelectCommand
        {
            get { return getSelectCommand(); }
        }

        public override System.Windows.Forms.DataGridViewColumn[] GetTableColumns
        {
            get
            {
                return columnCollection;
            }
            set
            {
                columnCollection = value;
            }
        }

        public override void CreateColumns()
        {
            columnCollection = new DataGridViewColumn[4];
            columnCollection[0] = CreateDataGridViewColumn.CreateTextBoxColumn(TableName, "role", "Наименование роли", true, true);
            columnCollection[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
            columnCollection[1] = CreateDataGridViewColumn.CreateCheckBoxColumn("TableName", "tul","Профиль Тула",true);
            //columnCollection[1].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
            columnCollection[2] = CreateDataGridViewColumn.CreateCheckBoxColumn("TableName", "ob", "Профиль Обнинск", true);
            //columnCollection[2].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
            columnCollection[3] = CreateDataGridViewColumn.CreateCheckBoxColumn("TableName", "sah", "Профиль Саха", true);
           // columnCollection[3].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
        }

        public override bool Save(Npgsql.NpgsqlConnection connect, Npgsql.NpgsqlDataAdapter adapter, DataTable dt)
        {
            return true;
        }

        public override void SetDefaultValuesAfterRowAdded(System.Windows.Forms.DataGridView dgv, int index)
        {
           
        }

        public override void SetDefaultValuesAfterCellChange(System.Windows.Forms.DataGridView dgv, int row, int column)
        {
            
        }

        public bool AllowEdit(EditOperations operation)
        {
            MessageBox.Show("Данная таблица только для просмотра", "Только просмотр", MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return false;
        }
    }
}
