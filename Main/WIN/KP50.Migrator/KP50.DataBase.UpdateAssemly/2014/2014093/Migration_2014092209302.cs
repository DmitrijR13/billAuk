using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2014092209302, MigrateDataBase.LocalBank)]
    public class Migration_2014092209302_LocalBank : Migration
    {
        public override void Apply() 
        {
            SetSchema(Bank.Data);
            var counters_arx = new SchemaQualifiedObjectName { Name = "counters_arx", Schema = CurrentSchema };
            if (!Database.ColumnExists(counters_arx, "nzp_prm"))
            {
                Database.AddColumn(counters_arx, new Column("nzp_prm", DbType.Int32));
            }
        }

        public override void Revert() 
        {
            
        }
    }
}
