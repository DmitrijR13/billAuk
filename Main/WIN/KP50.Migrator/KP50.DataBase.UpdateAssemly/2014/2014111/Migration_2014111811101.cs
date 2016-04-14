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
    [Migration(2014111811101, MigrateDataBase.CentralBank)]
    public class Migration_2014111811101_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Upload);

            var file_supp_charge = new SchemaQualifiedObjectName { Name = "file_supp_charge", Schema = CurrentSchema };

            for (int i = 1; i <= 3; i++)
            { 
                if (!Database.ColumnExists(file_supp_charge, "num_cnt" + i))
                {
                    Database.AddColumn(file_supp_charge, new Column("num_cnt" + i, DbType.String.WithSize(20)));
                }
            }

            if (!Database.IndexExists("ix_fsc_nzp_load_1", file_supp_charge))
            {
                Database.AddIndex("ix_fsc_nzp_load_1", false, file_supp_charge, "nzp_load");
            }

            if (!Database.IndexExists("ix_fsc_nzp_kvar_2", file_supp_charge))
            {
                Database.AddIndex("ix_fsc_nzp_kvar_2", false, file_supp_charge, "nzp_kvar");
            }

            if (!Database.IndexExists("ix_fsc_3", file_supp_charge))
            {
                Database.AddIndex("ix_fsc_3", false, file_supp_charge, "nzp_kvar", "nzp_supp", "nzp_serv", "nzp_frm");
            }
        }

        public override void Revert()
        {

        }
    }
}
