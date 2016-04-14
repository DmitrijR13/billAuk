using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Npgsql;

namespace webroles.GenerateScriptTable
{
    class TRoleMergingTableGenScript : GenerateScript
    {



        public override void Request()
        {
            Adapter.SelectCommand.CommandText = "With dsf as (SELECT * FROM s_roles_intermed) " +
                                                "SELECT id, nzp_role_from, nzp_role_to FROM t_role_merging, dsf, s_roles_intermed " +
                                                "WHERE nzp_role_to=dsf.nzp_role AND  nzp_role_from=s_roles_intermed.nzp_role;";
            DataTable = new DataTable();
            Adapter.Fill(DataTable);
        }

        public override void GenerateScr()
        {
            var comList = (from DataRow dr in DataTable.Rows
                           select "INSERT INTO t_role_merging (nzp_role_from, nzp_role_to) VALUES ("+
                                  (dr.Field<int?>("nzp_role_from").HasValue ? dr.Field<int?>("nzp_role_from").ToString() : "null") + ", " +
                                  (dr.Field<int?>("nzp_role_to").HasValue ? dr.Field<int?>("nzp_role_to").ToString() : "null") + ")" + ";")
    .ToList();
            comList.Insert(0, "--Заполнение таблицы t_role_merging");
            comList.Add(string.Empty);
            try
            {
                File.AppendAllLines(FileName, comList);
            }
            catch
            {
                MessageBox.Show("Что-то пошло не так!");
            }
        }
    }
}
