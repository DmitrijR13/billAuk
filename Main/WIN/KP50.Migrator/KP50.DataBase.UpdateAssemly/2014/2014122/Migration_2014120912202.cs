using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2014._2014122
{
    [Migration(2014120912202, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_2014120912202_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);
            SchemaQualifiedObjectName s_group = new SchemaQualifiedObjectName()
            {
                Name = "s_group",
                Schema = CurrentSchema
            };
            if (Database.TableExists(s_group))
            {
                Database.Delete(s_group, " nzp_group = 6");
                Database.Insert(s_group, new[] {"nzp_group", "ngroup"}, new[] {"6", "П-превыш.показ. в ИПУ"});
            }
        }
    }

}
