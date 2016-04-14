using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015021
{
    [Migration(2015020602101, Migrator.Framework.DataBase.Charge)]
    public class Migration_2015020602101_Charge : Migration
    {
        public override void Apply()
        {
            for (int mm = 1; mm <= 12; mm++)
            {
                var charge_mm = new SchemaQualifiedObjectName { Name = "charge_" + mm.ToString("00"), Schema = CurrentSchema };

                if (Database.ColumnExists(charge_mm, "c_calc"))
                    Database.ChangeColumn(charge_mm, "c_calc", DbType.Decimal.WithSize(14,5), false);
            }
        }
    }
}
