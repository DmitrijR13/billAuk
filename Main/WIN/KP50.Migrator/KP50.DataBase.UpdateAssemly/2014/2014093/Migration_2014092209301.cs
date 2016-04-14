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
    [Migration(2014092209301, MigrateDataBase.CentralBank)]
    public class Migration_2014092209301_CentralBank : Migration
    {
        public override void Apply() {
            SetSchema(Bank.Data);
            var tulaExSzFile = new SchemaQualifiedObjectName { Name = "tula_ex_sz_file", Schema = CurrentSchema };
            if (Database.TableExists(tulaExSzFile))
            {
                //Служебное поле
                if (!Database.ColumnExists(tulaExSzFile, "sl"))
                {
                    Database.AddColumn(tulaExSzFile, new Column("sl", DbType.Int16));
                }
                //Кол-во зарегистрированных(по данным предприятия)
                if (!Database.ColumnExists(tulaExSzFile, "kolzrp"))
                {
                    Database.AddColumn(tulaExSzFile, new Column("kolzrp", DbType.Int16));
                } 
            }
        }

        public override void Revert() {
            SetSchema(Bank.Data);
            var tulaExSzFile = new SchemaQualifiedObjectName { Name = "tula_ex_sz_file", Schema = CurrentSchema };
            if (Database.TableExists(tulaExSzFile))
            {
                //Служебное поле
                if (Database.ColumnExists(tulaExSzFile, "sl"))
                {
                    Database.RemoveColumn(tulaExSzFile, "sl");
                }
                //Кол-во зарегистрированных(по данным предприятия)
                if (Database.ColumnExists(tulaExSzFile, "kolzrp"))
                {
                    Database.RemoveColumn(tulaExSzFile, "kolzrp");
                }
            }
        }
    }
}
