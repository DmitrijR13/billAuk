using System;
using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015042
{
    [Migration(2015051804201, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_2015051804201_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            var res_y = new SchemaQualifiedObjectName() { Name = "res_y", Schema = CurrentSchema };
            if (Database.TableExists(res_y))
            {
                Database.Delete(res_y, "nzp_res in (3002,3006,3007,3008,3022) and nzp_y in (1,2,3,4,5,6)");

                Database.Insert(res_y, new string[] { "nzp_res", "nzp_y", "name_y" },
                    new string[] { "3002", "6", "Формула распределения остатка ОДН на ЛС без ИПУ" });

                Database.Insert(res_y, new string[] { "nzp_res", "nzp_y", "name_y" },
                    new string[] { "3006", "5", "Формула распределения остатка ОДН на ЛС без ИПУ" });

                Database.Insert(res_y, new string[] { "nzp_res", "nzp_y", "name_y" },
                    new string[] { "3007", "5", "Формула распределения остатка ОДН на ЛС без ИПУ" });

                Database.Insert(res_y, new string[] { "nzp_res", "nzp_y", "name_y" },
                    new string[] { "3008", "5", "Формула распределения остатка ОДН на ЛС без ИПУ" });

                Database.Insert(res_y, new string[] { "nzp_res", "nzp_y", "name_y" },
                    new string[] { "3022", "4", "Формула распределения остатка ОДН на ЛС без ИПУ" });

                Database.Insert(res_y, new string[] { "nzp_res", "nzp_y", "name_y" },
                    new string[] { "3002", "1", "Формула 9 Постановления №307" });

                Database.Insert(res_y, new string[] { "nzp_res", "nzp_y", "name_y" },
                    new string[] { "3002", "2", "Формула 6 Постановления №307" });

                Database.Insert(res_y, new string[] { "nzp_res", "nzp_y", "name_y" },
                    new string[] { "3002", "3", "Формула 11 Постановления №354" });

                Database.Insert(res_y, new string[] { "nzp_res", "nzp_y", "name_y" },
                    new string[] { "3006", "1", "Формула  9 Постановления №307" });

                Database.Insert(res_y, new string[] { "nzp_res", "nzp_y", "name_y" },
                    new string[] { "3006", "2", "Формула  6 Постановления №307" });

                Database.Insert(res_y, new string[] { "nzp_res", "nzp_y", "name_y" },
                   new string[] { "3006", "3", "Формула 11 Постановления №354" });

                Database.Insert(res_y, new string[] { "nzp_res", "nzp_y", "name_y" },
                    new string[] { "3006", "4", "Формула 20 Постановления №354" });

                Database.Insert(res_y, new string[] { "nzp_res", "nzp_y", "name_y" },
                   new string[] { "3007", "1", "Формула  9 Постановления №307" });

                Database.Insert(res_y, new string[] { "nzp_res", "nzp_y", "name_y" },
                    new string[] { "3007", "2", "Формула  6 Постановления №307" });

                Database.Insert(res_y, new string[] { "nzp_res", "nzp_y", "name_y" },
                    new string[] { "3007", "3", "Формула 11 Постановления №354" });

                Database.Insert(res_y, new string[] { "nzp_res", "nzp_y", "name_y" },
                    new string[] { "3007", "4", "Формула 11 Постановления №354 2х-тарифный" });

                Database.Insert(res_y, new string[] { "nzp_res", "nzp_y", "name_y" },
                    new string[] { "3008", "1", "Формула  9 Постановления №307" });

                Database.Insert(res_y, new string[] { "nzp_res", "nzp_y", "name_y" },
                    new string[] { "3008", "2", "Формула  6 Постановления №307" });

                Database.Insert(res_y, new string[] { "nzp_res", "nzp_y", "name_y" },
                    new string[] { "3008", "3", "Формула 14 Постановления №354" });

                Database.Insert(res_y, new string[] { "nzp_res", "nzp_y", "name_y" },
                    new string[] { "3008", "4", "Формула 18 Постановления №354" });
            }
        }

        public override void Revert()
        {

        }
    }
}
