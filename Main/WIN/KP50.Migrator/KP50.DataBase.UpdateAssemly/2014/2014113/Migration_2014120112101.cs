using System;
using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2014120112101, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_2014120112101 : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName prm_name = new SchemaQualifiedObjectName()
            {
                Name = "prm_name",
                Schema = CurrentSchema
            };
          
            if (Database.TableExists(prm_name)) Database.Delete(prm_name, " nzp_prm = 2005");
            if (Database.TableExists(prm_name))
                Database.Insert(prm_name, new[] {"nzp_prm", "name_prm", "type_prm", "prm_num", "high_", "digits_", "is_day_uchet"},
                    new[] {"2005", "Количество прописанных", "int", "1", "1000", "4", "1"});
        }
    }
}
