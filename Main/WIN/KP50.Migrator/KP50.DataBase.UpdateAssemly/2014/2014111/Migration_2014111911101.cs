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
    [Migration(2014111911101, MigrateDataBase.CentralBank)]
    public class Migration_2014111911101_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Supg);
            var users = new SchemaQualifiedObjectName { Name = "users", Schema = CurrentSchema };
            Database.ChangeColumn(users, "name", DbType.String.WithSize(40), true);
        }

        public override void Revert()
        {

        }
    }
}
