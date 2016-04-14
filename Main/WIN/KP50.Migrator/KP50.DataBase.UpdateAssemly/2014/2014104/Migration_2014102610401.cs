using System.Collections.Generic;
using System.Data;
using KP50.DataBase.Migrator.Framework;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2014102610401, Migrator.Framework.DataBase.CentralBank)]
    public class Migration_2014102610401 : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);
            var fn_percent_dom = new SchemaQualifiedObjectName() { Name = "fn_percent_dom", Schema = CurrentSchema };
            if (!Database.ColumnExists(fn_percent_dom, "nzp_supp_snyat"))
            {
                Database.AddColumn(fn_percent_dom, new Column("nzp_supp_snyat", DbType.Int32, ColumnProperty.None, -1));
            }

             if (Database.ColumnExists(fn_percent_dom, "nzp_supp_snyat"))
             {
                 Database.Update(fn_percent_dom, new string[] { "nzp_supp_snyat" }, new string[] { "-1" }, "nzp_supp_snyat is null");
             }
        }
    }
}
