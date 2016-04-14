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
    [Migration(2014092309402, MigrateDataBase.CentralBank)]
    public class Migration_2014092309402_CentralBank : Migration
    {
        public override void Apply() 
        {
            SetSchema(Bank.Upload);
            var file_supp_charge = new SchemaQualifiedObjectName { Name = "file_supp_charge", Schema = CurrentSchema };

            if (!Database.TableExists(file_supp_charge))
            {
                Database.AddTable(file_supp_charge,
                    new Column("nzp_fsc", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("nzp_load", DbType.Int32),
                    new Column("nzp_wp", DbType.Int32),
                    new Column("nzp_supp", DbType.Int32),
                    new Column("nzp_kvar", DbType.Int32),
                    new Column("nzp_frm", DbType.Int32),
                    new Column("nzp_serv", DbType.Int32),
                    new Column("nzp_charge", DbType.Int32),
                    new Column("nzp_tarif", DbType.Int32),
                    new Column("nzp_cls", DbType.Int32),
                    new Column("nzp_counter1", DbType.Int32),
                    new Column("nzp_counter2", DbType.Int32),
                    new Column("nzp_counter3", DbType.Int32),
                    new Column("nzp_measure", DbType.Int32),
                    new Column("calc_year", DbType.Int32),
                    new Column("calc_month", DbType.Int32),
                    new Column("date_calc", DbType.Date),
                    new Column("adres", DbType.String.WithSize(200)),
                    new Column("num_ls_supp", DbType.String.WithSize(40)),
                    new Column("service", DbType.String.WithSize(40)),
                    new Column("measure", DbType.String.WithSize(20)),
                    new Column("tarif", DbType.Decimal.WithSize(14, 2)),
                    new Column("c_calc", DbType.Decimal.WithSize(14, 2)),
                    new Column("sum_real", DbType.Decimal.WithSize(14, 2)),
                    new Column("sum_outsaldo", DbType.Decimal.WithSize(14, 2)),
                    new Column("nzp_cr1", DbType.Int32),
                    new Column("nzp_cr2", DbType.Int32),
                    new Column("nzp_cr3", DbType.Int32),
                    new Column("val_cnt1", DbType.Decimal.WithSize(14, 2)),
                    new Column("val_cnt2", DbType.Decimal.WithSize(14, 2)),
                    new Column("val_cnt3", DbType.Decimal.WithSize(14, 2)));
            }
            else
            {
                if (!Database.ColumnExists(file_supp_charge, "nzp_frm")) Database.AddColumn(file_supp_charge, new Column("nzp_frm", DbType.Int32));

                if (!Database.ColumnExists(file_supp_charge, "nzp_cr1")) Database.AddColumn(file_supp_charge, new Column("nzp_cr1", DbType.Int32));
                if (!Database.ColumnExists(file_supp_charge, "nzp_cr2")) Database.AddColumn(file_supp_charge, new Column("nzp_cr2", DbType.Int32));
                if (!Database.ColumnExists(file_supp_charge, "nzp_cr3")) Database.AddColumn(file_supp_charge, new Column("nzp_cr3", DbType.Int32));
            }
        }

        public override void Revert() 
        {
                
        }
    }
}