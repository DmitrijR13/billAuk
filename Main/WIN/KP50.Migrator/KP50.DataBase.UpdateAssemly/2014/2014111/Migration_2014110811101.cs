using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2014._2014111
{
    [Migration(2014110811101, MigrateDataBase.CentralBank)]
    public class Migration_2014110811101_CentralBank : Migration
    {
        public override void Apply() {
            SetSchema(Bank.Data);
            var tulaExSzFile = new SchemaQualifiedObjectName {Name = "tula_ex_sz_file", Schema = CurrentSchema};
            if (Database.TableExists(tulaExSzFile))
            {
                if (Database.IndexExists("ix_ex_1", tulaExSzFile))
                {
                    Database.RemoveIndex("ix_ex_1", tulaExSzFile);
                }
                if (!Database.IndexExists("ix_ex_sz_1", tulaExSzFile))
                {
                    Database.AddIndex("ix_ex_sz_1", false, tulaExSzFile, "nzp_ex_sz");
                }
            }
        }

        public override void Revert() {
            SetSchema(Bank.Data);
            var tulaExSzFile = new SchemaQualifiedObjectName { Name = "tula_ex_sz_file", Schema = CurrentSchema };
            if (Database.TableExists(tulaExSzFile))
            {
                if (!Database.IndexExists("ix_ex_1", tulaExSzFile))
                {
                    Database.AddIndex("ix_ex_1", false, tulaExSzFile, "lchet");
                }
                if (Database.IndexExists("ix_ex_sz_1", tulaExSzFile))
                {
                    Database.RemoveIndex("ix_ex_sz_1", tulaExSzFile);
                }
            }
        }
    }

   
}
