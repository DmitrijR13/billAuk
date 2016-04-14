using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{   
    [Migration(2014090309101, MigrateDataBase.Web)]
    public class Migration_2014090309101_Web : Migration
    {
        public override void Apply()
        {
            SchemaQualifiedObjectName pack_ls = new SchemaQualifiedObjectName() { Name = "pack_ls", Schema = CurrentSchema };
            if (!Database.ColumnExists(pack_ls, "pkod")) Database.AddColumn(pack_ls, new Column("pkod", DbType.Decimal.WithSize(13,0)));
        }

        public override void Revert()
        {
            SchemaQualifiedObjectName pack_ls = new SchemaQualifiedObjectName() { Name = "pack_ls", Schema = CurrentSchema };
            if (Database.ColumnExists(pack_ls, "pkod")) Database.RemoveColumn(pack_ls, "pkod");
        }
    }
}
