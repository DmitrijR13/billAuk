using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015021
{
    [Migration(2015020402104, Migrator.Framework.DataBase.LocalBank)]
    public class Migration_2015020402104_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);
            var counters_bounds = new SchemaQualifiedObjectName { Name = "counters_bounds", Schema = CurrentSchema };

            if (Database.TableExists(counters_bounds))
            {
                if (Database.ColumnExists(counters_bounds,"created_on "))
                {
                    Database.RenameColumn(counters_bounds, "created_on ","created_on");
                }
                if (Database.ColumnExists(counters_bounds, "changed_on "))
                {
                    Database.RenameColumn(counters_bounds, "changed_on ", "changed_on");
                }
            }

        }

        public override void Revert()
        {
        }
    }
}
