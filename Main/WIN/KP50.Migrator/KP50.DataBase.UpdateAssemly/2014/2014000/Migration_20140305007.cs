using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(20140305007, MigrateDataBase.Fin)]
    public class Migration_20140305007_Fin : Migration
    {
        public override void Apply()
        {
            SchemaQualifiedObjectName fn_operday_dom_mc = new SchemaQualifiedObjectName() { Name = "fn_operday_dom_mc", Schema = CurrentSchema };
            if (!Database.TableExists(fn_operday_dom_mc))
            {
                Database.AddTable(fn_operday_dom_mc,
                    new Column("nzp_oper", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("date_oper", DbType.Date),
                    new Column("nzp_dom", DbType.Int32));
                Database.AddIndex("idx_fn_oplog_dom_mc_1", true, fn_operday_dom_mc, "nzp_oper");
                Database.AddIndex("idx_fn_oplog_dom_mc_2", false, fn_operday_dom_mc, "date_oper", "nzp_dom");
            }

            SchemaQualifiedObjectName fn_reval_dom = new SchemaQualifiedObjectName() { Name = "fn_reval_dom", Schema = CurrentSchema };
            if (!Database.TableExists(fn_reval_dom))
            {
                Database.AddTable(fn_reval_dom,
                    new Column("nzp_reval_dom", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("nzp_reval", DbType.Int32, ColumnProperty.NotNull),
                    new Column("dat_oper", DbType.Date),
                    new Column("nzp_payer", DbType.Int32),
                    new Column("nzp_dom", DbType.Int32),
                    new Column("nzp_serv", DbType.Int32),
                    new Column("nzp_payer_2", DbType.Int32),
                    new Column("nzp_reval_2", DbType.Int32),
                    new Column("sum_reval", DbType.Decimal, ColumnProperty.NotNull, 0.00),
                    new Column("sum_reval_r", DbType.Decimal, ColumnProperty.NotNull, 0.00),
                    new Column("sum_reval_g", DbType.Decimal, ColumnProperty.NotNull, 0.00),
                    new Column("comment", DbType.StringFixedLength.WithSize(60)),
                    new Column("nzp_bl", DbType.Int32),
                    new Column("nzp_area", DbType.Int32),
                    new Column("nzp_geu", DbType.Int32),
                    new Column("nzp_user", DbType.Int32),
                    new Column("dat_when", DbType.Date),
                    new Column("nzp_bank", DbType.Int32, ColumnProperty.None, -1));
                Database.AddIndex("ix_reval_dom_01", true, fn_reval_dom, "nzp_reval_dom");
                Database.AddIndex("ix_revt_dom_1", false, fn_reval_dom, "dat_oper", "nzp_area", "nzp_geu", "nzp_payer", "nzp_dom", "nzp_serv");
                Database.AddIndex("ix_revt_dom_2", false, fn_reval_dom, "dat_oper", "nzp_area", "nzp_geu", "nzp_payer_2", "nzp_dom", "nzp_serv");
                Database.AddIndex("ix_revt_dom_3", false, fn_reval_dom, "nzp_payer", "nzp_dom", "nzp_serv");
                Database.AddIndex("ix_revt_dom_4", false, fn_reval_dom, "nzp_payer_2", "nzp_dom", "nzp_serv");
                Database.AddIndex("ix110_dom_5", false, fn_reval_dom, "nzp_reval_2");
                Database.AddIndex("ix110_dom_6", false, fn_reval_dom, "nzp_reval");
                if (Database.ProviderName == "Informix") { Database.ExecuteNonQuery("GRANT select, update, insert, delete, index ON fn_reval_dom TO public AS are"); }
                if (Database.ProviderName == "PostgreSQL") { Database.ExecuteNonQuery("GRANT select, update, insert, delete ON fn_reval_dom TO public"); }
            }
        }

        public override void Revert()
        {
            SchemaQualifiedObjectName fn_operday_dom_mc = new SchemaQualifiedObjectName() { Name = "fn_operday_dom_mc", Schema = CurrentSchema };
            if (Database.TableExists(fn_operday_dom_mc)) Database.RemoveTable(fn_operday_dom_mc);

            SchemaQualifiedObjectName fn_reval_dom = new SchemaQualifiedObjectName() { Name = "fn_reval_dom", Schema = CurrentSchema };
            if (Database.TableExists(fn_reval_dom)) Database.RemoveTable(fn_reval_dom);
        }
    }
}
