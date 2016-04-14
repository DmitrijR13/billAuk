using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
     [Migration(20140425044, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_20140429001_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            var supplier_point = new SchemaQualifiedObjectName() { Name = "suppler_point", Schema = CurrentSchema };

            if (!Database.TableExists(supplier_point))
            {
                Database.AddTable(supplier_point,
                    new Column("nzp_supp", DbType.Int32, ColumnProperty.NotNull),
                    new Column("nzp_wp", DbType.Int32, ColumnProperty.NotNull)
                    );
            }
        }
    }
}
