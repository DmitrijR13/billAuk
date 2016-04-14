using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2014052005202, MigrateDataBase.LocalBank)]
    public class Migration_20140520052_LocalBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);
            SchemaQualifiedObjectName counters = new SchemaQualifiedObjectName() { Name = "counters", Schema = CurrentSchema };
            if (Database.ColumnExists(counters, "dat_uchet")) Database.ChangeColumn(counters, "dat_uchet", DbType.Date, true);
            if (Database.ColumnExists(counters, "val_cnt")) Database.ChangeColumn(counters, "val_cnt", DbType.Double, true);
        }
    }
}
