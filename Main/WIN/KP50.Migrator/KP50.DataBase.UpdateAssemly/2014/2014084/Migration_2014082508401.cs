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
    [Migration(2014082508401, MigrateDataBase.CentralBank)]
    public class Migration_2014082508401_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Upload);
            // TODO: Upgrade CentralPref_Upload
            SchemaQualifiedObjectName file_urlic = new SchemaQualifiedObjectName() { Name = "file_urlic", Schema = CurrentSchema };

            if (Database.TableExists(file_urlic))
            {
                Database.ChangeColumn(file_urlic, "urlic_name_s", DbType.String.WithSize(25), false);
            }
        }

        public override void Revert()
        {
          SetSchema(Bank.Upload);
            // TODO: Downgrade CentralPref_Upload
          SchemaQualifiedObjectName file_urlic = new SchemaQualifiedObjectName() { Name = "file_urlic", Schema = CurrentSchema };

          if (Database.TableExists(file_urlic))
          {
              Database.ChangeColumn(file_urlic, "urlic_name_s", DbType.String.WithSize(10), false);
          }
        }
    }

   
}
