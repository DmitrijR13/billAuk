using System;
using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015032
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2015031603201, MigrateDataBase.CentralBank)]
    public class Migration_2015031603201_CentralBank : Migration
    {
        public override void Apply()
        {
            if (Database.ProviderName != "PostgreSQL") return;

            string fin_15 = CentralPrefix + "_fin_15";

            if (
                Convert.ToInt32(
                    Database.ExecuteScalar("select count(*) from information_schema.schemata where schema_name = '" +
                                           fin_15 + "'")) == 0)
                return;

            SchemaQualifiedObjectName pack_ls = new SchemaQualifiedObjectName()
            {
                Name = "pack_ls",
                Schema = fin_15
            };

            if (!Database.TableExists(pack_ls)) return;

            if (!Database.IndexExists("ix_pack_ls_transaction_id_nzp_pack_ls", pack_ls))
            {
                Database.AddIndex("ix_pack_ls_transaction_id_nzp_pack_ls", false, pack_ls, "transaction_id",
                    "nzp_pack_ls");
            }
            else
            {
                Database.RemoveIndex("ix_pack_ls_transaction_id_nzp_pack_ls", pack_ls);
                Database.AddIndex("ix_pack_ls_transaction_id_nzp_pack_ls", false, pack_ls,
                    new[] {"transaction_id", "nzp_pack_ls"});
            }
        }
    }
}
