using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2014
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2014080108201, MigrateDataBase.CentralBank)]
    public class Migration_2014080108201_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);
            SchemaQualifiedObjectName tula_counters_reestr = new SchemaQualifiedObjectName() { Name = "tula_counters_reestr", Schema = CurrentSchema };
            var tula_file_reestr = new SchemaQualifiedObjectName() { Name = "tula_file_reestr", Schema = CurrentSchema };

            if (!Database.TableExists(tula_counters_reestr))
            {
                Database.AddTable(
                    tula_counters_reestr,
                    new Column("nzp_tcr", DbType.Int32, ColumnProperty.PrimaryKeyWithIdentity | ColumnProperty.NotNull),
                    // Identity property is SERIAL and NOT NULL value default
                    new Column("nzp_reestr_d", DbType.Int32, ColumnProperty.NotNull),
                    new Column("nzp_counter", DbType.Int32, ColumnProperty.NotNull),
                    new Column("cnt", DbType.String.WithSize(100), ColumnProperty.NotNull),
                    new Column("val_cnt", DbType.Decimal.WithSize(14,4),  ColumnProperty.NotNull)
                    );
                Database.AddIndex("ix_tcr_01",true,tula_counters_reestr,"nzp_tcr");
                Database.AddIndex("ix_tcr_02", false, tula_counters_reestr, "nzp_counter");
                Database.AddIndex("ix_tcr_03", false, tula_counters_reestr, "nzp_reestr_d");
                if (!Database.IndexExists("ix_tula_file_reestr_1", tula_file_reestr))
                    Database.AddIndex("ix_tula_file_reestr_1", true, tula_file_reestr, "nzp_reestr_d");

                Database.AddForeignKey("FK_nzp_reestr_d", tula_counters_reestr, "nzp_reestr_d", tula_file_reestr, "nzp_reestr_d");
            }




            if (!Database.IndexExists("ix_tula_file_reestr_2", tula_file_reestr))
            {
                Database.AddIndex("ix_tula_file_reestr_2", false, tula_file_reestr, "pkod");
            }
            if (!Database.IndexExists("ix_tula_file_reestr_3", tula_file_reestr))
            {
                Database.AddIndex("ix_tula_file_reestr_3", false, tula_file_reestr, "nzp_kvit_reestr");
            }

            
        }

        public override void Revert()
        {
            
        
        }
    }


    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2014080108201, MigrateDataBase.Fin)]
    public class Migration_2014080108201_Fin : Migration
    {
        public override void Apply()
        {

            SchemaQualifiedObjectName pack_ls = new SchemaQualifiedObjectName() { Name = "pack_ls" };

            if (!Database.ColumnExists(pack_ls,"transaction_id") )
            {
                Column transaction_id = new Column("transaction_id", DbType.String);
                Database.AddColumn(pack_ls, transaction_id);
            }
           
        }

        public override void Revert()
        {
            
        
        }
    }

    
    

}
