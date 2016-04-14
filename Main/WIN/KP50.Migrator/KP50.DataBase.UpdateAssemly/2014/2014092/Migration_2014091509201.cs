using KP50.DataBase.Migrator.Framework;
using System.Data;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2014091509201, MigrateDataBase.CentralBank)]
    public class Migration_2014091509201_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);

            SchemaQualifiedObjectName kvar_pkodes = new SchemaQualifiedObjectName() { Name = "kvar_pkodes", Schema = CurrentSchema };
            if (!Database.ColumnExists(kvar_pkodes, "pkod")) Database.AddColumn(kvar_pkodes, new Column("pkod", DbType.Decimal.WithSize(13, 0)));
        }
    }
}
