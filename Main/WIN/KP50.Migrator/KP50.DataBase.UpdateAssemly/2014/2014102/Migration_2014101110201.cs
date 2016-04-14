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
    [Migration(2014101110201, MigrateDataBase.CentralBank)]
    public class Migration_2014101110201_CentralBank : Migration
    {
        public override void Apply() {
            SetSchema(Bank.Data);
            var tulaExSz = new SchemaQualifiedObjectName { Name = "tula_ex_sz", Schema = CurrentSchema };
            if (Database.TableExists(tulaExSz))
            {
                //формат
                if (!Database.ColumnExists(tulaExSz, "is_format"))
                {
                    Database.AddColumn(tulaExSz, new Column("is_format", DbType.Int32));
                }
            }
        }

        public override void Revert() {
            SetSchema(Bank.Data);
            var tulaExSz = new SchemaQualifiedObjectName { Name = "tula_ex_sz", Schema = CurrentSchema };
            if (Database.TableExists(tulaExSz))
            {
                //формат
                if (Database.ColumnExists(tulaExSz, "is_format"))
                {
                    Database.RemoveColumn(tulaExSz, "is_format");
                }
            }
        }
    }
}
