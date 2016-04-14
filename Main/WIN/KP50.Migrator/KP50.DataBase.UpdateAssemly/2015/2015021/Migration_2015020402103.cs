using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015021
{
    [Migration(2015020402103, Migrator.Framework.DataBase.LocalBank)]
    public class Migration_2015020402103_LocalBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);
            var counters_replaced = new SchemaQualifiedObjectName { Name = "counters_replaced", Schema = CurrentSchema };

            if (!Database.TableExists(counters_replaced))
            {
                Database.AddTable(counters_replaced,
                    new Column("id", DbType.Int32, ColumnProperty.NotNull | ColumnProperty.Identity),
                    new Column("nzp_counter_new", DbType.Int32, ColumnProperty.NotNull),
                    new Column("nzp_counter_old", DbType.Int32, ColumnProperty.NotNull)
                    );


                if (Database.TableExists(counters_replaced))
                {
                    if (!Database.IndexExists("ix_counters_replaced_1", counters_replaced))
                    {
                        Database.AddIndex("ix_counters_replaced_1", true, counters_replaced, "id");
                    }

                    if (!Database.IndexExists("ix_counters_replaced_2", counters_replaced))
                    {
                        Database.AddIndex("ix_counters_replaced_2", false, counters_replaced, "nzp_counter_new", "nzp_counter_old");
                    }

                }
            }

        }

        public override void Revert()
        {
            var counters_replaced = new SchemaQualifiedObjectName { Name = "counters_replaced", Schema = CurrentSchema };
            SetSchema(Bank.Data);
            if (Database.TableExists("counters_replaced"))
            {
                Database.RemoveTable(counters_replaced);
            }
        }
    }
}
