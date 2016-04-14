using System;
using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2014052205201, MigrateDataBase.LocalBank)]
    public class Migration_20140522052_LocalBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);
            SchemaQualifiedObjectName users = new SchemaQualifiedObjectName() { Name = "users", Schema = CurrentSchema };
            if (Database.ColumnExists(users, "name")) Database.ChangeColumn(users, "name", new ColumnType(DbType.String, 40), true);
        }
    }
    [Migration(2014052205201, MigrateDataBase.CentralBank)]
    public class Migration_20140522052_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);
            SchemaQualifiedObjectName users = new SchemaQualifiedObjectName() { Name = "users", Schema = CurrentSchema };
            if (Database.ColumnExists(users, "name")) Database.ChangeColumn(users, "name", new ColumnType(DbType.String, 40), true);
        }
    }
    [Migration(2014052205201, MigrateDataBase.Web)]
    public class Migration_20140522052_Web : Migration
    {
        public override void Apply()
        {
            SchemaQualifiedObjectName users = new SchemaQualifiedObjectName() { Name = "users", Schema = CurrentSchema };
            if (Database.ColumnExists(users, "name")) Database.ChangeColumn(users, "name", new ColumnType(DbType.String, 40), true);
        }
    }
}
