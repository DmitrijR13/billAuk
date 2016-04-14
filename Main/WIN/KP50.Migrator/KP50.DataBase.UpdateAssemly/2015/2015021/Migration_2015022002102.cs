using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015021
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2015022002102, MigrateDataBase.CentralBank)]
    public class Migration_2015022002102_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Upload);
            SchemaQualifiedObjectName file_versions = new SchemaQualifiedObjectName() { Name = "file_versions", Schema = CurrentSchema };
            if (Database.TableExists(file_versions))
            {
                Database.Delete(file_versions, "nzp_version = 10");
                Database.Insert(file_versions, new string[] { "nzp_version", "nzp_ff", "version_name" }, new string[] { "10", "10", "1.3.7" });
            }

        }

        public override void Revert()
        {
            SetSchema(Bank.Upload);
            SetSchema(Bank.Upload);
            SchemaQualifiedObjectName file_versions = new SchemaQualifiedObjectName() { Name = "file_versions", Schema = CurrentSchema };
            if (Database.TableExists(file_versions))
            {
                Database.Delete(file_versions, "nzp_version = 10");
            }
        }
    }
}
