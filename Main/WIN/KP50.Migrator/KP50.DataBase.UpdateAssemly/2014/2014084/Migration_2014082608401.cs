using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2014._2014084
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2014082608401, MigrateDataBase.CentralBank)]
    public class Migration_2014082608401_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Upload);
            // TODO: Upgrade CentralPref_Upload
            SchemaQualifiedObjectName file_info_pu = new SchemaQualifiedObjectName() { Name = "file_info_pu", Schema = CurrentSchema };

            if (Database.TableExists(file_info_pu))
            {
                Database.ChangeColumn(file_info_pu, "num_ls_pu", DbType.String.WithSize(20), true);
            }
        }

        public override void Revert()
        {
            SetSchema(Bank.Upload);
            // TODO: Downgrade CentralPref_Upload
            SchemaQualifiedObjectName file_info_pu = new SchemaQualifiedObjectName() { Name = "file_info_pu", Schema = CurrentSchema };

            if (Database.TableExists(file_info_pu))
            {
                Database.ChangeColumn(file_info_pu, "num_ls_pu", DbType.Int32, true);
            }
        }
    }

    
}
