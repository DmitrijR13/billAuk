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
    [Migration(2014100110105, MigrateDataBase.CentralBank)]
    public class Migration_2014100110105_CentralBank : Migration
    {
        public override void Apply() {
            SetSchema(Bank.Data);
            var tulaExSzFile = new SchemaQualifiedObjectName { Name = "tula_ex_sz_file", Schema = CurrentSchema };
            if (Database.TableExists(tulaExSzFile))
            {
                //Общая площадь(по данным предприятия)
                if (!Database.ColumnExists(tulaExSzFile, "oplj"))
                {
                    Database.AddColumn(tulaExSzFile, new Column("oplj", DbType.Decimal.WithSize(14,2)));
                }
            }
        }

        public override void Revert() {
            SetSchema(Bank.Data);
            var tulaExSzFile = new SchemaQualifiedObjectName { Name = "tula_ex_sz_file", Schema = CurrentSchema };
            if (Database.TableExists(tulaExSzFile))
            {
                //Общая площадь(по данным предприятия)
                if (Database.ColumnExists(tulaExSzFile, "oplj"))
                {
                    Database.RemoveColumn(tulaExSzFile, "oplj");
                }
            }
        }
    }
}