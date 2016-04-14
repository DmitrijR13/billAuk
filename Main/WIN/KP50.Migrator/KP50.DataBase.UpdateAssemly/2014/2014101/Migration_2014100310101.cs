using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2014100310101, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_2014100310101_CentralBank: Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);
            SchemaQualifiedObjectName counters_group = new SchemaQualifiedObjectName() { Name = "counters_group", Schema = CurrentSchema };
            if (Database.TableExists(counters_group))
            {
                if (!Database.ColumnExists(counters_group, "ngp_cnt"))
                {
                    Database.AddColumn(counters_group, new Column("ngp_cnt", DbType.Decimal.WithSize(14, 7)));
                }
            }
        }
    }
}
