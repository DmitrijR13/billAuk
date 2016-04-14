using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015042
{
    
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2015051304202, MigrateDataBase.Charge)]
    public class Migration_2015051304202_Charge : Migration
    {
        public override void Apply()
        {            
            for (int mm = 1; mm <= 12; mm++)
            {
                var gil_mm = new SchemaQualifiedObjectName
                {
                    Name = "gil_" + mm.ToString("00"),
                    Schema = CurrentSchema
                };

                if (Database.TableExists(gil_mm))
                {
                    if (!Database.ColumnExists(gil_mm, "val6"))
                    {
                        Database.AddColumn(gil_mm, new Column("val6", DbType.Decimal.WithSize(11, 7), ColumnProperty.NotNull, "0"));
                    }
                }
            }
        }

        public override void Revert()
        {
            // TODO: Downgrade Charges

        }
    }

}
