using System;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2014121612301, MigrateDataBase.CentralBank)]
    public class Migration_2014121612301_CentralBank: Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Supg);
            SchemaQualifiedObjectName s_attestation = new SchemaQualifiedObjectName()
            {
                Name = "s_attestation",
                Schema = CurrentSchema
            };

            if (Database.TableExists(s_attestation))
            {
                if (Convert.ToInt32(Database.ExecuteScalar(" select count(*) from  " + s_attestation.Name + " where nzp_atts = 1")) == 0)
                {
                    Database.Insert(s_attestation, new string[] { "nzp_atts", "atts_name" }, new string[] { "1", "Информация отсутствует" });
                }

                if (Convert.ToInt32(Database.ExecuteScalar(" select count(*) from  " + s_attestation.Name + " where nzp_atts = 2")) == 0)
                {
                    Database.Insert(s_attestation, new string[] { "nzp_atts", "atts_name" }, new string[] { "2", "Подтверждено жильцом" });
                }

                if (Convert.ToInt32(Database.ExecuteScalar(" select count(*) from  " + s_attestation.Name + " where nzp_atts = 3")) == 0)
                {
                    Database.Insert(s_attestation, new string[] { "nzp_atts", "atts_name" }, new string[] { "3", "Не подтверждено жильцом" });
                }
            }

            SchemaQualifiedObjectName s_result = new SchemaQualifiedObjectName()
            {
                Name = "s_result",
                Schema = CurrentSchema
            };
            Database.Update(s_result, new string[] { "res_type" }, new string[] { "1" }, "nzp_res in (2, 3, 4, 5)");
        }
    }
}
