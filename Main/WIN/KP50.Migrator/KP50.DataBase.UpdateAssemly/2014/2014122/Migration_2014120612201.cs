using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2014120612201, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_2014120612201 : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);
            SchemaQualifiedObjectName prm_10 = new SchemaQualifiedObjectName() { Name = "prm_10", Schema = CurrentSchema };
            Database.Delete(prm_10, "nzp_prm = 2464");

            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName prm_name = new SchemaQualifiedObjectName() { Name = "prm_name", Schema = CurrentSchema };
            SchemaQualifiedObjectName resolution = new SchemaQualifiedObjectName() { Name = "resolution", Schema = CurrentSchema };
            SchemaQualifiedObjectName res_y = new SchemaQualifiedObjectName() { Name = "res_y", Schema = CurrentSchema };
            SchemaQualifiedObjectName res_x = new SchemaQualifiedObjectName() { Name = "res_x", Schema = CurrentSchema };
            SchemaQualifiedObjectName res_values = new SchemaQualifiedObjectName() { Name = "res_values", Schema = CurrentSchema };

            Database.Delete(prm_name, "nzp_prm = 2464");
            Database.Delete(resolution, "nzp_res = 3027");
            Database.Delete(res_y, "nzp_res = 3027");
            Database.Delete(res_x, "nzp_res = 3027");
            Database.Delete(res_values, "nzp_res = 3027");

            Database.Insert(prm_name,
                new string[] { "nzp_prm", "name_prm", "type_prm", "nzp_res", "prm_num", "low_", "high_", "digits_" },
                new string[] { "2464", "'Расщепление оплат - порядок гашения пени", "sprav", "3027", "10", null, null, null });
            Database.Insert(resolution,
                new string[] { "nzp_res", "name_short", "name_res" },
                new string[] { "3027", "СНПениПорядок", "справочник порядка гашения пени" });
            Database.Insert(res_y, new string[] { "nzp_res", "nzp_y", "name_y" }, new string[] { "3027", "1", "гасить пени в первую очередь" });
            Database.Insert(res_y, new string[] { "nzp_res", "nzp_y", "name_y" }, new string[] { "3027", "2", "равномерно гасить пени" });
            Database.Insert(res_y, new string[] { "nzp_res", "nzp_y", "name_y" }, new string[] { "3027", "3", "гасить пени в последнюю очередь" });
            Database.Insert(res_x, new string[] { "nzp_res", "nzp_x", "name_x" }, new string[] { "3027", "1", "-" });
            Database.Insert(res_values, new string[] { "nzp_res", "nzp_y", "nzp_x", "value" }, new string[] { "3027", "1", "1", "0" });
            Database.Insert(res_values, new string[] { "nzp_res", "nzp_y", "nzp_x", "value" }, new string[] { "3027", "2", "1", "0" });
            Database.Insert(res_values, new string[] { "nzp_res", "nzp_y", "nzp_x", "value" }, new string[] { "3027", "3", "1", "0" });


            SetSchema(Bank.Data);
            if (Database.ProviderName == "PostgreSQL") Database.ExecuteNonQuery("INSERT INTO prm_10 (nzp, nzp_prm, dat_s, dat_po, val_prm, is_actual, nzp_user, dat_when) VALUES (0, 2464, '01.01.1900', '01.01.3000', 3, 1, 1, now())");
            if (Database.ProviderName == "Informix") Database.ExecuteNonQuery("INSERT INTO prm_10 (nzp, nzp_prm, dat_s, dat_po, val_prm, is_actual, nzp_user, dat_when) VALUES (0, 2464, '01.01.1900', '01.01.3000', 3, 1, 1, current)");
        }
    }
}
