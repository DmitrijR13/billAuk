using System;
using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;


namespace KP50.DataBase.UpdateAssembly._2015._2015042
{
    [Migration(2015050704201, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_2015050704201_CentralOrLocalBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName services = new SchemaQualifiedObjectName() { Name = "services", Schema = CurrentSchema };

            
            object obj = Database.ExecuteScalar("SELECT count(*) FROM " + services.Schema + Database.TableDelimiter + services.Name +
                " WHERE nzp_serv = 496;");
            var count = Convert.ToInt32(obj);

            if (count == 0)
            {
                Database.Insert(services, new string[]
                {
                    "nzp_serv", "service", "service_small", "service_name", "ed_izmer", "type_lgot",
                    "nzp_frm", "ordering", "nzp_measure"
                }, new string[]
                {
                    "496", "% за рассрочку по Холодная вода для нужд ГВС", "РАС-ХВС для ГВС",
                    "% за рассрочку по Холодная вода для нужд ГВС", "с кв.в мес.", "1", "0", "496", "6"
                });
            }

            obj = Database.ExecuteScalar("SELECT count(*) FROM " + services.Schema + Database.TableDelimiter + services.Name +
                " WHERE nzp_serv = 497;");
            count = Convert.ToInt32(obj);

            if (count == 0)
            {
                Database.Insert(services, new string[]
                {
                    "nzp_serv", "service", "service_small", "service_name", "ed_izmer", "type_lgot",
                    "nzp_frm", "ordering", "nzp_measure"
                }, new string[]
                {
                    "497", "% за рассрочку по Электроснабжение ночное", "РАС-Эл.эн.ночн.",
                    "% за рассрочку по Электроснабжение ночное", "с кв.в мес.", "1", "0", "497", "6"
                });
            }
        }

        public override void Revert()
        {
        }
    }
}
