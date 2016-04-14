using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2014121212201, MigrateDataBase.LocalBank)]
    public class Migration_2014121212201_local: Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);
            var countersComment = new SchemaQualifiedObjectName
            {
                Name = "counters_comment",
                Schema = CurrentSchema
            };
            if (!Database.TableExists(countersComment))
            {
                Database.AddTable(countersComment,
                   new Column("nzp_cr", DbType.Int32, ColumnProperty.NotNull),
                   new Column("comment", DbType.String.WithSize(250)),
                   new Column("is_actual", DbType.Int32)
                   );
            }
        }
    }
}
