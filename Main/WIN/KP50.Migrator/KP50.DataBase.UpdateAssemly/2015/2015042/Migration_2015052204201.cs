using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015042
{
    [Migration(2015052204201, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_2015052204201 : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);
            var sysEvents = new SchemaQualifiedObjectName() { Name = "sys_events", Schema = CurrentSchema };
            if (Database.TableExists(sysEvents))
            {
                if (Database.ColumnExists(sysEvents, "note"))
                {
                    Database.ChangeColumn(sysEvents, "note", DbType.String.WithSize(2000), false);
                }
            }
        }

    }

}
