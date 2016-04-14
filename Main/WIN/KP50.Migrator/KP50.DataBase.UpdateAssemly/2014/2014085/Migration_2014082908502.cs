using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2014._2014085
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2014082908502, MigrateDataBase.CentralBank)]
    public class Migration_2014082908502_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);

            SchemaQualifiedObjectName supplier = new SchemaQualifiedObjectName() { Name = "supplier", Schema = CurrentSchema };
            if (!Database.ColumnExists(supplier, "dpd"))
            {
                Database.AddColumn(supplier, new Column("dpd", DbType.Int16, defaultValue:0));
            }
             
        }

        public override void Revert()
        { 

        }
    }
}
