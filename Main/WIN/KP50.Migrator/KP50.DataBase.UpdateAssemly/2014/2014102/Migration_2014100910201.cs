using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;


namespace KP50.DataBase.UpdateAssembly._2014._2014102
{
    [Migration(2014100910201, MigrateDataBase.CentralBank)]
    public class Migration_2014100910201_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);
            SchemaQualifiedObjectName sys_dictionary_values = new SchemaQualifiedObjectName()
            {
                Name = "sys_dictionary_values",
                Schema = CurrentSchema
            };

            if (Database.TableExists(sys_dictionary_values))
            {
                if (!Database.IndexExists("ix_sys_dictionary_values_1", sys_dictionary_values))
                {
                    Database.AddIndex("ix_sys_dictionary_values_1", true, sys_dictionary_values, "nzp_dict");
                }

                Database.Delete(sys_dictionary_values, "nzp_dict = 8217");

                Database.Insert(
                    sys_dictionary_values,
                    new[] {"nzp_dict", "nzp_tdict", "name"},
                    new[] {"8217", "101", "Запуск проверок перед закрытием месяца"});
            }

        }

    }
}
