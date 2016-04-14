using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(20140305024, MigrateDataBase.CentralBank)]
    public class Migration_20140305024_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);
            SchemaQualifiedObjectName fn_percent_dom = new SchemaQualifiedObjectName() { Name = "fn_percent_dom", Schema = CurrentSchema };
            if (!Database.TableExists(fn_percent_dom))
            {
                Database.AddTable(fn_percent_dom,
                    new Column("nzp_fp", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("nzp_payer", DbType.Int32),
                    new Column("nzp_supp", DbType.Int32),
                    new Column("nzp_serv", DbType.Int32),
                    new Column("nzp_area", DbType.Int32),
                    new Column("nzp_geu", DbType.Int32),
                    new Column("perc_ud", DbType.Decimal, ColumnProperty.None, 0),
                    new Column("dat_s", DbType.Date),
                    new Column("dat_po", DbType.Date),
                    new Column("nzp_rs", DbType.Int32, ColumnProperty.NotNull, 1),
                    new Column("nzp_bank", DbType.Int32, ColumnProperty.None, -1),
                    new Column("nzp_dom", DbType.Int32, ColumnProperty.None, -1),
                    new Column("minpl", DbType.Decimal, ColumnProperty.None, 0));
                if (Database.ProviderName == "Informix") Database.ExecuteNonQuery("ALTER TABLE fn_percent_dom LOCK MODE (ROW)");
                Database.AddIndex("i_rsp_dom_01", false, fn_percent_dom, "nzp_rs");
                Database.AddIndex("ix_perc_dom_1", true, fn_percent_dom, "nzp_fp");
                Database.AddIndex("ix_perc_dom_2", false, fn_percent_dom, "nzp_payer", "nzp_supp");
                Database.AddIndex("ix_perc_dom_3", false, fn_percent_dom, "nzp_payer", "nzp_area");
                Database.AddIndex("ix_perc_dom_4", false, fn_percent_dom, "dat_s", "dat_po", "nzp_supp", "nzp_area");
                Database.AddIndex("ix_perc_dom_6", false, fn_percent_dom, "nzp_payer", "nzp_area", "nzp_dom");
                Database.AddIndex("ix_perc_dom_7", false, fn_percent_dom, "nzp_payer", "nzp_supp", "nzp_serv", "nzp_dom");
                Database.AddIndex("ix_perc_dom_8", false, fn_percent_dom, "dat_s", "dat_po", "nzp_supp", "nzp_area", "nzp_dom");
                Database.AddIndex("ix_perc_dom_9", false, fn_percent_dom, "nzp_payer", "nzp_supp", "nzp_serv");
            }

            SchemaQualifiedObjectName tula_ex_sz = new SchemaQualifiedObjectName() { Name = "tula_ex_sz", Schema = CurrentSchema };
            if (Database.TableExists(tula_ex_sz) && !Database.ColumnExists(tula_ex_sz, "nzp_wp")) Database.AddColumn(tula_ex_sz, new Column("nzp_wp", DbType.Int32));
        }

        public override void Revert()
        {
            SetSchema(Bank.Data);
            SchemaQualifiedObjectName fn_percent_dom = new SchemaQualifiedObjectName() { Name = "fn_percent_dom", Schema = CurrentSchema };
            SchemaQualifiedObjectName tula_ex_sz = new SchemaQualifiedObjectName() { Name = "tula_ex_sz", Schema = CurrentSchema };
            if (Database.TableExists(fn_percent_dom)) Database.RemoveTable(fn_percent_dom);
            if (Database.ColumnExists(tula_ex_sz, "nzp_wp")) Database.RemoveColumn(tula_ex_sz, "nzp_wp");
        }
    }

    [Migration(20140305024, MigrateDataBase.Fin)]
    public class Migration_20140305024_Fin : Migration
    {
        public override void Apply()
        {
            SchemaQualifiedObjectName fn_naud_dom = new SchemaQualifiedObjectName() { Name = "fn_naud_dom", Schema = CurrentSchema };
            if (Database.TableExists(fn_naud_dom) && !Database.ColumnExists(fn_naud_dom, "sum_prih"))
                Database.AddColumn(fn_naud_dom, new Column("sum_prih", DbType.Decimal.WithSize(14, 2), ColumnProperty.None, 0.00));
            if (Database.TableExists(fn_naud_dom) && !Database.ColumnExists(fn_naud_dom, "perc_ud"))
                Database.AddColumn(fn_naud_dom, new Column("perc_ud", DbType.Decimal.WithSize(14, 2), ColumnProperty.None, 0.00));
        }

        public override void Revert()
        {
            SchemaQualifiedObjectName fn_naud_dom = new SchemaQualifiedObjectName() { Name = "fn_naud_dom", Schema = CurrentSchema };
            if (Database.ColumnExists(fn_naud_dom, "sum_prih")) Database.RemoveColumn(fn_naud_dom, "sum_prih");
            if (Database.ColumnExists(fn_naud_dom, "perc_ud")) Database.RemoveColumn(fn_naud_dom, "perc_ud");
        }
    }
}
