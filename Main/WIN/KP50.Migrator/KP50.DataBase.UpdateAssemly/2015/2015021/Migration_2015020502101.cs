using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015021
{
    [Migration(2015020502101, Migrator.Framework.DataBase.CentralBank)]
    public class Migration_2015020502101_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            var s_counters_types_alg = new SchemaQualifiedObjectName { Name = "s_counters_types_alg", Schema = CurrentSchema };

            if (!Database.TableExists("s_counters_types_alg"))
            {
                Database.AddTable(s_counters_types_alg,
                    new Column("id", DbType.Int32, ColumnProperty.NotNull | ColumnProperty.Identity),
                    new Column("algoritm", DbType.String.WithSize(50)));
                if (Database.TableExists(s_counters_types_alg))
                {
                    Database.Insert(s_counters_types_alg, new[] { "id", "algoritm" }, new[] { "1", "Период неработоспособности" });
                    Database.Insert(s_counters_types_alg, new[] { "id", "algoritm" }, new[] { "2", "Период работоспособности" });
                }

                var s_counters_bounds_types = new SchemaQualifiedObjectName { Name = "s_counters_bounds_types", Schema = CurrentSchema };
                if (Database.TableExists(s_counters_bounds_types))
                {
                    Database.AddColumn(s_counters_bounds_types, new Column("alg_id", new ColumnType(DbType.Int32)));
                    if (Database.ColumnExists(s_counters_bounds_types, "alg_id"))
                    {
                        Database.Update(s_counters_bounds_types, new[] { "alg_id" }, new[] { "1" }, " id=1");
                        Database.Update(s_counters_bounds_types, new[] { "alg_id" }, new[] { "2" }, " id=2");
                    }
                }
            }

        }

        public override void Revert()
        {
        }
    }
}
