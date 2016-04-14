using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2014111911102, MigrateDataBase.CentralBank)]
    public class Migration_2014111911102_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);

            SchemaQualifiedObjectName peni_prov = new SchemaQualifiedObjectName();
            peni_prov.Schema = CurrentSchema;
            peni_prov.Name = "peni_provodki";
            
            if (Database.TableExists(peni_prov))
            {
                if (
                    !Database.IndexExists(
                        "ix6_" + peni_prov.Name + "num_ls_nzp_serv_nzp_supp_date_obligation",
                        peni_prov))
                {
                    Database.AddIndex(
                        "ix6_" + peni_prov.Name + "num_ls_nzp_serv_nzp_supp_date_obligation",
                        false,
                        peni_prov, "num_ls", "nzp_serv", "nzp_supp", "date_obligation");
                }
            }

            peni_prov.Name = "peni_provodki_acrh";
            if (Database.TableExists(peni_prov))
            {
                if (
                    !Database.IndexExists(
                        "ix6_" + peni_prov.Name + "num_ls_nzp_serv_nzp_supp_date_obligation",
                        peni_prov))
                {
                    Database.AddIndex(
                        "ix6_" + peni_prov.Name + "num_ls_nzp_serv_nzp_supp_date_obligation",
                        false,
                        peni_prov, "num_ls", "nzp_serv", "nzp_supp", "date_obligation");
                }
            }
        }

        public override void Revert()
        {

        }
    }
}
