using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015021
{
    [Migration(2015020402101, Migrator.Framework.DataBase.CentralBank)]
    public class Migration_2015020402101_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            var s_counters_bounds_types = new SchemaQualifiedObjectName { Name = "s_counters_bounds_types", Schema = CurrentSchema };

            if (!Database.TableExists("s_counters_bounds_types"))
            {
                Database.AddTable(s_counters_bounds_types,
                    new Column("id", DbType.Int32, ColumnProperty.NotNull | ColumnProperty.Identity),
                    new Column("type", DbType.String));
                if (Database.TableExists("s_counters_bounds_types"))
                {
                    Database.Insert(s_counters_bounds_types, new[] { "id", "type" }, new[] { "1", "Период поломки" });
                    Database.Insert(s_counters_bounds_types, new[] { "id", "type" }, new[] { "2", "Период поверки" });
                }
            }

        }

        public override void Revert()
        {
            var s_counters_bounds_types = new SchemaQualifiedObjectName { Name = "s_counters_bounds_types", Schema = CurrentSchema };
            SetSchema(Bank.Kernel);
            if (Database.TableExists("s_counters_bounds_types"))
            {
                Database.RemoveTable(s_counters_bounds_types);
            }
        }
    }
}
