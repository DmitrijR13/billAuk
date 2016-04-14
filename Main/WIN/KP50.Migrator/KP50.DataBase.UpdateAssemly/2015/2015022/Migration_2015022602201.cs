using System;
using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015021
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2015022602201, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_2015022602201_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);

            SchemaQualifiedObjectName prm_name = new SchemaQualifiedObjectName() { Name = "prm_name", Schema = CurrentSchema }; ;
            SchemaQualifiedObjectName prm_tarifs = new SchemaQualifiedObjectName() { Name = "prm_tarifs", Schema = CurrentSchema };
            SchemaQualifiedObjectName prm_frm = new SchemaQualifiedObjectName() { Name = "prm_frm", Schema = CurrentSchema };
            SchemaQualifiedObjectName formuls_opis = new SchemaQualifiedObjectName() { Name = "formuls_opis", Schema = CurrentSchema };
            SchemaQualifiedObjectName services = new SchemaQualifiedObjectName() { Name = "services", Schema = CurrentSchema };

            //проверка на наличие параметра в банке данных
            var count_p =
                Convert.ToInt32(
                    Database.ExecuteScalar("SELECT count(*) FROM " + CurrentSchema + Database.TableDelimiter +
                                           prm_name.Name + " WHERE nzp_prm = 2463"));
            //проверка на наличие формулы в банке данных
            var count_f =
              Convert.ToInt32(
                  Database.ExecuteScalar("SELECT count(*) FROM " + CurrentSchema + Database.TableDelimiter +
                                         formuls_opis.Name + " WHERE nzp_frm = 1814"));

            //проверка на наличие услуги в банке данных
            var count_s =
              Convert.ToInt32(
                  Database.ExecuteScalar("SELECT count(*) FROM " + CurrentSchema + Database.TableDelimiter +
                                         services.Name + " WHERE nzp_serv = 8"));


            if (count_p > 0 && count_f > 0 && count_s > 0)
            {
                Database.Delete(prm_frm, "nzp_prm=2463 and nzp_frm=1814");
                Database.Delete(prm_tarifs, "nzp_prm=2463 and nzp_frm=1814");

                Database.Insert(prm_frm,
                    new[] { "nzp_frm", "frm_calc", "is_prm", "operation", "nzp_prm", "frm_p1", "frm_p2", "frm_p3", "result" },
                    new[] { "1814", "999", "1", "  FLD", "2463", " ", " ", " ", " " });

                Database.Insert(prm_tarifs,
                    new[] { "nzp_serv", "nzp_frm", "nzp_prm", "is_edit", "nzp_user" },
                    new[] { "8", "1814", "2463", "1", "1" });
            }
        }

        public override void Revert()
        {
        }
    }

}
