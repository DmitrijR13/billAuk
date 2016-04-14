using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{

        [Migration(2014121512302, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
   
        public class Migration_2014121512302_Central : Migration
        {
            public override void Apply()
            {
                SetSchema(Bank.Data);
                SchemaQualifiedObjectName prm_10 = new SchemaQualifiedObjectName()
                {
                    Name = "prm_10",
                    Schema = CurrentSchema
                };
                Database.Delete(prm_10, "nzp_prm = 1368");


                SetSchema(Bank.Kernel);
                SchemaQualifiedObjectName prm_name = new SchemaQualifiedObjectName()
                {
                    Name = "prm_name",
                    Schema = CurrentSchema
                };

                Database.Delete(prm_name, "nzp_prm = 1368");
                Database.Insert(prm_name,
                    new string[] { "nzp_prm", "name_prm", "type_prm", "nzp_res", "prm_num", "low_", "high_", "digits_" },
                new string[] { "1368", "Режим учета контрольных показаний", "bool", null, "10", null, null, null });

                SetSchema(Bank.Data);
                if (Database.ProviderName == "PostgreSQL")
                    Database.ExecuteNonQuery(
                        "INSERT INTO prm_10 (nzp, nzp_prm, dat_s, dat_po, val_prm, is_actual, nzp_user, dat_when) VALUES (0, 1368, '01.01.1900', '01.01.3000', 0, 1, 1, now())");
                if (Database.ProviderName == "Informix")
                    Database.ExecuteNonQuery(
                        "INSERT INTO prm_10 (nzp, nzp_prm, dat_s, dat_po, val_prm, is_actual, nzp_user, dat_when) VALUES (0, 1368, '01.01.1900', '01.01.3000', 0, 1, 1, current)");
            }
        } 
}
