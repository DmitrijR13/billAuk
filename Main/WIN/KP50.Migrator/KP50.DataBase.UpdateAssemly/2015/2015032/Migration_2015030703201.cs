using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015032
{
    [Migration(2015030703201, MigrateDataBase.CentralBank)]
    public class Migration_2015030703201_CentralBank : Migration
    {
        public override void Apply() {
            SetSchema(Bank.Data);
            var tulaExSzFile = new SchemaQualifiedObjectName { Name = "tula_ex_sz_file", Schema = CurrentSchema };
            if (Database.TableExists(tulaExSzFile))
            {
                //Норматив
                for (int i = 1; i <= 16; i++)
                {
                    if (!Database.ColumnExists(tulaExSzFile, "norm" + i))
                    {
                        Database.AddColumn(tulaExSzFile, new Column("norm" + i, DbType.Decimal.WithSize(10, 4)));
                    }
                }
            }
        }

        public override void Revert() {
            SetSchema(Bank.Data);
            var tulaExSzFile = new SchemaQualifiedObjectName { Name = "tula_ex_sz_file", Schema = CurrentSchema };
            if (Database.TableExists(tulaExSzFile))
            {
                //Норматив
                for (int i = 1; i <= 16; i++)
                {
                    if (Database.ColumnExists(tulaExSzFile, "norm" + 1))
                    {
                        Database.RemoveColumn(tulaExSzFile, "norm" + 1);
                    }
                }
            }
        }
    }
}
