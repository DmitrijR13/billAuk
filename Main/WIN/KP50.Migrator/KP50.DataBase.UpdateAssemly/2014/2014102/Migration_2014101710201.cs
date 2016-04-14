using System;
using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2014._2014102
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2014101710201, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_2014101710201_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName prm_name = new SchemaQualifiedObjectName() { Name = "prm_name", Schema = CurrentSchema };
            SchemaQualifiedObjectName resolution = new SchemaQualifiedObjectName() { Name = "resolution", Schema = CurrentSchema };
            SchemaQualifiedObjectName res_y = new SchemaQualifiedObjectName() { Name = "res_y", Schema = CurrentSchema };
            SchemaQualifiedObjectName res_x = new SchemaQualifiedObjectName() { Name = "res_x", Schema = CurrentSchema };
            SchemaQualifiedObjectName res_values = new SchemaQualifiedObjectName() { Name = "res_values", Schema = CurrentSchema };

            if (Database.TableExists(prm_name))
            {
                int count = Convert.ToInt32(Database.ExecuteScalar(
                    Database.FormatSql("SELECT count(nzp_prm) FROM {0:NAME} WHERE nzp_prm = 1373 ", prm_name)));
                if (count == 0)
                {
                    Database.Insert(prm_name, new string[] {"nzp_prm", "name_prm", "type_prm", "nzp_res", "prm_num"},
                        new string[] {"1373", "Тип собственности", "sprav", "3017", "1"});
                }
                else
                {
                    Database.Update(prm_name, new string[] { "nzp_prm", "name_prm", "type_prm", "nzp_res", "prm_num" },
                        new string[] { "1373", "Тип собственности", "sprav", "3017", "1" }, "nzp_prm = 1373");
                }

                count = Convert.ToInt32(Database.ExecuteScalar(
                    Database.FormatSql("SELECT count(nzp_prm) FROM {0:NAME} WHERE nzp_prm = 1374 ", prm_name)));
                if (count == 0)
                {
                    Database.Insert(prm_name, new string[] { "nzp_prm", "name_prm", "type_prm", "prm_num" },
                        new string[] { "1374", "Второе жилье для лицевого счета", "bool", "1" });
                }
                else
                {
                    Database.Update(prm_name, new string[] { "nzp_prm", "name_prm", "type_prm", "prm_num" },
                        new string[] { "1374", "Второе жилье для лицевого счета", "bool", "1" }, "nzp_prm = 1374");
                }

            }

            if (Database.TableExists(resolution))
            {
                Database.Delete(resolution, "nzp_res=3017");
                Database.Insert(resolution, new string[] { "nzp_res", "name_short", "name_res" },
                    new string[] { "3017", "ТТипКвПУС", "таблица Тип собственности" });
            }

            if (Database.TableExists(res_y))
            {
                Database.Delete(res_y, "nzp_res=3017");
                Database.Insert(res_y, new string[] { "nzp_res", "nzp_y", "name_y" },
                    new string[] { "3017", "1", "долевая" });
                Database.Insert(res_y, new string[] { "nzp_res", "nzp_y", "name_y" },
                    new string[] { "3017", "2", "совместная" });
                Database.Insert(res_y, new string[] { "nzp_res", "nzp_y", "name_y" },
                    new string[] { "3017", "3", "муниципальная" });
            }

            if (Database.TableExists(res_x))
            {
                Database.Delete(res_x, "nzp_res=3017");
                Database.Insert(res_x, new string[] { "nzp_res", "nzp_x", "name_x" },
                    new string[] { "3017", "1", "Номер" });
            }

            if (Database.TableExists(res_values))
            {
                Database.Delete(res_values, "nzp_res=3017");
                Database.Insert(res_values, new string[] { "nzp_res", "nzp_y", "nzp_x", "value" },
                    new string[] { "3017", "1", "1", "' '" });
                Database.Insert(res_values, new string[] { "nzp_res", "nzp_y", "nzp_x", "value" },
                    new string[] { "3017", "2", "1", "' '" });
                Database.Insert(res_values, new string[] { "nzp_res", "nzp_y", "nzp_x", "value" },
                    new string[] { "3017", "3", "1", "' '" });
            }


        }

        public override void Revert()
        {
            SetSchema(Bank.Kernel);
            // TODO: Downgrade CentralPref_Kernel

        }
    }

}
