using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2014062006301, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_2014062006301 : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);
            SchemaQualifiedObjectName users = new SchemaQualifiedObjectName() { Name = "users", Schema = CurrentSchema };

            Database.ChangeColumn(users, "name", DbType.StringFixedLength.WithSize(40), true);
            Database.ChangeColumn(users, "comment", DbType.StringFixedLength.WithSize(200), false);
        }
    }
}