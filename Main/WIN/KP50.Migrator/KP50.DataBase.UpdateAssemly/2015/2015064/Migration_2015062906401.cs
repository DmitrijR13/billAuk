using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015064
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2015062906401, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_2015062906401_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName s_counttypes = new SchemaQualifiedObjectName() { Name = "s_counttypes", Schema = CurrentSchema };

            if (Database.TableExists(s_counttypes))
            {
                if (Database.ColumnExists(s_counttypes, "mmnog"))
                {
                    Database.ChangeColumn(s_counttypes, "mmnog", DbType.Decimal.WithSize(16,7), false);
                }
            }

        }

        public override void Revert()
        {
            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName s_counttypes = new SchemaQualifiedObjectName() { Name = "s_counttypes", Schema = CurrentSchema };

            if (Database.TableExists(s_counttypes))
            {
                if (Database.ColumnExists(s_counttypes, "mmnog"))
                {
                    Database.ChangeColumn(s_counttypes, "mmnog", DbType.Int32, false);
                }
            }
        }
    }
}
