using System;
using KP50.DataBase.Migrator.Framework;
using System.Data;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2014091909302, MigrateDataBase.CentralBank)]
    public class Migration_2014091909302_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName s_typercl = new SchemaQualifiedObjectName() { Name = "s_typercl", Schema = CurrentSchema };
           
            if (Database.TableExists(s_typercl))
            {
                Database.Update(s_typercl, new string[] { "is_actual" }, new string[] { "100" });
                
                if (Convert.ToInt32(Database.ExecuteScalar(" select count(*) from  " + s_typercl.Name + " where type_rcl = 20")) > 0)
                {
                    Database.Update(s_typercl, new string[] { "is_actual", "typename", "nzp_type_uchet", "is_auto" }, new string[] { "1", "Корректировка cальдо", "1", "0" }, "type_rcl = 20");
                }

                if (Convert.ToInt32(Database.ExecuteScalar(" select count(*) from  " + s_typercl.Name + " where type_rcl = 102")) > 0)
                {
                    Database.Update(s_typercl, new string[] { "is_actual", "typename" }, new string[] { "1", "Корректировка начисления" }, "type_rcl = 102");
                }
            }

        }
    }
}
