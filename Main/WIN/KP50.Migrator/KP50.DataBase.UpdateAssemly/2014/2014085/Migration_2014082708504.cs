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
    [Migration(2014082708504, MigrateDataBase.CentralBank)]
    public class Migration_2014082708504_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Upload);
            // TODO: Upgrade CentralPref_Upload
            SchemaQualifiedObjectName file_reestr_ls = new SchemaQualifiedObjectName() { Name = "file_reestr_ls", Schema = CurrentSchema };

            if (Database.TableExists(file_reestr_ls))
            {
                Database.ChangeColumn(file_reestr_ls, "ls_id_supp", DbType.String.WithSize(20), true);
            }

        }

        public override void Revert()
        {
            SetSchema(Bank.Upload);
            // TODO: Downgrade CentralPref_Upload
            SchemaQualifiedObjectName file_reestr_ls = new SchemaQualifiedObjectName() { Name = "file_reestr_ls", Schema = CurrentSchema };

            if (Database.TableExists(file_reestr_ls))
            {
                Database.ChangeColumn(file_reestr_ls, "ls_id_supp", DbType.Int32, true);
            }

        }
    }

    
}
