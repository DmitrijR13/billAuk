using System.Collections.Generic;
using System.Data;
using KP50.DataBase.Migrator.Framework;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2015032003309, Migrator.Framework.DataBase.CentralBank)]
    public class Migration_2015032003309 : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            var table = new SchemaQualifiedObjectName() { Name = "s_listfactura", Schema = CurrentSchema };
            if (!Database.ColumnExists(table, "custom_text"))
            {
                Database.AddColumn(table, new Column("custom_text", DbType.String.WithSize(1000)));
            }
        }
    }
}
