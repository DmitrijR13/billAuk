using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2014._2014102
{

    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2014100710201, MigrateDataBase.LocalBank)]
    public class Migration_2014100710201_LocalBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);
            SchemaQualifiedObjectName counters_dom = new SchemaQualifiedObjectName() { Name = "counters_dom", Schema = CurrentSchema };

            if (Database.TableExists(counters_dom))
            {
                if (!Database.ColumnExists(counters_dom, "ist"))
                {
                    Database.AddColumn(counters_dom, new Column("ist", DbType.Int32));
                }
            }
        }

        public override void Revert()
        {
        }
    }

}
