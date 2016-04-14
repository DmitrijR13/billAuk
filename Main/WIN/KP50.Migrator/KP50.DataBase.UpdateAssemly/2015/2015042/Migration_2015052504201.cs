using System;
using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;


namespace KP50.DataBase.UpdateAssembly._2015._2015042
{
    [Migration(2015052504201, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_2015052504201_CentralOrLocalBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName services = new SchemaQualifiedObjectName() { Name = "services", Schema = CurrentSchema };


            object obj = Database.ExecuteScalar("SELECT count(*) FROM " + services.Schema + Database.TableDelimiter + services.Name +
                " WHERE nzp_serv = 33;");
            var count = Convert.ToInt32(obj);
            if (count == 0)
            {
                Database.Insert(services, new string[]
                {
                    "nzp_serv", "service", "service_small", "service_name", "ed_izmer", "type_lgot",
                    "nzp_frm", "ordering", "nzp_measure"
                }, new string[]
                {
                    "33", "ДОЛГ", "ДОЛГ",
                    "ДОЛГ", "с кв.в мес.", "1", "0", "33", "6"
                });
            }
        }

        public override void Revert()
        {
        }
    }
}
