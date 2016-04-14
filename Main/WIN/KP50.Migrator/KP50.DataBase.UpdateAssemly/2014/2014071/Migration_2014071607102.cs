using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2014071607102, MigrateDataBase.LocalBank)]
    public class Migration_2014071607102_LocalBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);
            SchemaQualifiedObjectName users = new SchemaQualifiedObjectName() { Name = "users", Schema = CurrentSchema };
            Database.Delete(users, "nzp_user = -88888888");

            Database.Insert(users,
                    new string[] { "nzp_user", "name", "comment" },
                    new string[] { "-88888888", "''", "'Автоматическое закрытие/открытие пени для рассрочки'" });

        }

        public override void Revert()
        {

        }
    }

}

