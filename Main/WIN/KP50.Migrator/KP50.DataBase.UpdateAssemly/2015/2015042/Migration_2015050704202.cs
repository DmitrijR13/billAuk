using System;
using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2015050704202, MigrateDataBase.LocalBank)]
    public class Migration_2015050704202 : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);
            var pere_gilec = new SchemaQualifiedObjectName() { Name = "pere_gilec", Schema = CurrentSchema };

            if (!Database.TableExists(pere_gilec)) return;
            if (!Database.ColumnExists(pere_gilec, "is_actual"))
            {
                Database.AddColumn(pere_gilec, new Column("is_actual", DbType.Int16, ColumnProperty.None, 1));
            }
        }
    }
}
