using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;


namespace KP50.DataBase.UpdateAssembly._2014._2014111
{
    [Migration(2014110711102, MigrateDataBase.CentralBank)]
    public class Migration_2014110711102_CentralBank : Migration
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
                Database.Delete(s_group, "nzp_group = 10007");
                Database.Insert(
                    s_group,
                    new[] {"nzp_group", "ngroup"},
                    new[] {"10007", "Открытые ЛС без начислений"});
            }
        }
    }

    [Migration(2014110711102, MigrateDataBase.LocalBank)]
    public class Migration_2014110711102_LocalBank : Migration
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
                Database.Delete(s_group, "nzp_group = 10007");
                Database.Insert(
                    s_group,
                    new[] { "nzp_group", "ngroup" },
                    new[] { "10007", "Открытые ЛС без начислений" });
            }
        }
    }
}
