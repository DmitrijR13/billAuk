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
    [Migration(2014082208401, MigrateDataBase.CentralBank)]
    public class Migration_2014082208401_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Upload);
            SchemaQualifiedObjectName file_versions = new SchemaQualifiedObjectName() { Name = "file_versions", Schema = CurrentSchema };
            if (Database.TableExists(file_versions))
            {
                Database.Delete(file_versions, "nzp_version = 7");
                Database.Insert(file_versions, new string[] { "nzp_version", "nzp_ff", "version_name" }, new string[] { "7", "7", "1.3.5" });
            }
        }

        public override void Revert()
        {
            SetSchema(Bank.Upload);
            SchemaQualifiedObjectName file_versions = new SchemaQualifiedObjectName() { Name = "file_versions", Schema = CurrentSchema };
            if (Database.TableExists(file_versions))
            {
                Database.Delete(file_versions, "nzp_version = 7");
            }
        }
    }

    
}
