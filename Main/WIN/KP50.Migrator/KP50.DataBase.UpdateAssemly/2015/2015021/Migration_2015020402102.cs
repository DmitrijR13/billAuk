using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015021
{
    [Migration(2015020402102, Migrator.Framework.DataBase.LocalBank)]
    public class Migration_2015020402102_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);
            var counters_bounds = new SchemaQualifiedObjectName { Name = "counters_bounds", Schema = CurrentSchema };

            if (!Database.TableExists(counters_bounds))
            {
                Database.AddTable(counters_bounds,
                    new Column("id", DbType.Int32, ColumnProperty.NotNull | ColumnProperty.Identity),
                    new Column("nzp_counter", DbType.Int32, ColumnProperty.NotNull, 0),
                    new Column("type_id", DbType.Int32, ColumnProperty.NotNull),
                    new Column("date_from", DbType.Date),
                    new Column("date_to", DbType.Date),
                    new Column("is_actual", DbType.Boolean, ColumnProperty.NotNull, "true"),
                    new Column("created_by", DbType.Int32, ColumnProperty.NotNull, 0),
                    new Column("created_on", DbType.DateTime, ColumnProperty.NotNull, "now()"),
                    new Column("changed_by", DbType.Int32),
                    new Column("changed_on", DbType.DateTime));

                if (Database.TableExists(counters_bounds))
                {
                    if (!Database.IndexExists("ix_counters_bounds_1", counters_bounds))
                    {
                        Database.AddIndex("ix_counters_bounds_1", true, counters_bounds, "id");
                    }
                    if (!Database.IndexExists("ix_counters_bounds_2", counters_bounds))
                    {
                        Database.AddIndex("ix_counters_bounds_2", false, counters_bounds, "nzp_counter","date_from","date_to","is_actual");
                    }
                    if (!Database.IndexExists("ix_counters_bounds_3", counters_bounds))
                    {
                        Database.AddIndex("ix_counters_bounds_3", false, counters_bounds, "nzp_counter","date_from","date_to","is_actual","type_id");
                    }
                }
            }

        }

        public override void Revert()
        {
            var counters_bounds = new SchemaQualifiedObjectName { Name = "counters_bounds", Schema = CurrentSchema };
            SetSchema(Bank.Data);
            if (Database.TableExists("counters_bounds"))
            {
                Database.RemoveTable(counters_bounds);
            }
        }
    }
}
