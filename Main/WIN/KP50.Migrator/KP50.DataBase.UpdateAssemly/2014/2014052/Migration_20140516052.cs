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
    [Migration(20140516052, MigrateDataBase.CentralBank)]
    public class Migration_20140516052_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);
            SchemaQualifiedObjectName fn_dogovor = new SchemaQualifiedObjectName() { Name = "fn_dogovor", Schema = CurrentSchema };
            if (!Database.ColumnExists(fn_dogovor, "nzp_supp")) Database.AddColumn(fn_dogovor, new Column("nzp_supp", DbType.Int32));
            if (!Database.IndexExists("ix_fd_7", fn_dogovor)) Database.AddIndex("ix_fd_7", false, fn_dogovor, "nzp_supp", "nzp_payer_ar", "nzp_payer");
        }

        public override void Revert()
        {
            SetSchema(Bank.Data);
            SchemaQualifiedObjectName fn_dogovor = new SchemaQualifiedObjectName() { Name = "fn_dogovor", Schema = CurrentSchema };
            if (Database.IndexExists("ix_fd_7", fn_dogovor)) Database.RemoveIndex("ix_fd_7", fn_dogovor);
        }
    }

    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(20140516052, MigrateDataBase.Fin)]
    public class Migration_20140516052_Fin : Migration
    {
        public override void Apply()
        {
            // TODO: Upgrade Fins
            for (int i = 1; i <= 12; i++)
            {
                SchemaQualifiedObjectName fn_distrib_dom = new SchemaQualifiedObjectName() { Name = string.Format("fn_distrib_dom_{0}", i.ToString("00")), Schema = CurrentSchema };
                string IndexDistribDomTemplate = "ix_fnd_dom_7_" + i.ToString("00");

                if (!Database.ColumnExists(fn_distrib_dom, "nzp_supp")) Database.AddColumn(fn_distrib_dom, new Column("nzp_supp", DbType.Int32));
                if (!Database.IndexExists(IndexDistribDomTemplate, fn_distrib_dom)) Database.AddIndex(IndexDistribDomTemplate, false, fn_distrib_dom, "nzp_supp", "nzp_serv");
            }

            SchemaQualifiedObjectName fn_sended = new SchemaQualifiedObjectName() { Name = "fn_sended", Schema = CurrentSchema };
            if (!Database.ColumnExists(fn_sended, "nzp_supp")) Database.AddColumn(fn_sended, new Column("nzp_supp", DbType.Int32));
            if (!Database.IndexExists("ix_fs_8", fn_sended)) Database.AddIndex("ix_fs_8", false, fn_sended, "dat_oper", "nzp_supp", "nzp_serv", "nzp_payer");
            if (!Database.IndexExists("ix_fs_9", fn_sended)) Database.AddIndex("ix_fs_9", false, fn_sended, "nzp_supp", "nzp_serv", "nzp_payer");

            SchemaQualifiedObjectName fn_sended_dom = new SchemaQualifiedObjectName() { Name = "fn_sended_dom", Schema = CurrentSchema };
            if (!Database.ColumnExists(fn_sended_dom, "nzp_supp")) Database.AddColumn(fn_sended_dom, new Column("nzp_supp", DbType.Int32));
            if (!Database.IndexExists("ix_fs_dom_8", fn_sended_dom)) Database.AddIndex("ix_fs_dom_8", false, fn_sended_dom, "dat_oper", "nzp_supp", "nzp_serv", "nzp_payer");
            if (!Database.IndexExists("ix_fs_dom_9", fn_sended_dom)) Database.AddIndex("ix_fs_dom_9", false, fn_sended_dom, "nzp_supp", "nzp_dom", "nzp_serv", "nzp_payer");
        }

        public override void Revert()
        {
            // TODO: Downgrade Fins
            for (int i = 1; i <= 12; i++)
            {
                SchemaQualifiedObjectName fn_distrib_dom = new SchemaQualifiedObjectName() { Name = string.Format("fn_distrib_dom_{0}", i.ToString("00")), Schema = CurrentSchema };
                string IndexDistribDomTemplate = "ix_fnd_dom_7_" + i.ToString("00");
                if (Database.IndexExists(IndexDistribDomTemplate, fn_distrib_dom)) Database.RemoveIndex(IndexDistribDomTemplate, fn_distrib_dom);
            }

            SchemaQualifiedObjectName fn_sended = new SchemaQualifiedObjectName() { Name = "fn_sended", Schema = CurrentSchema };
            if (Database.IndexExists("ix_fs_8", fn_sended)) Database.RemoveIndex("ix_fs_8", fn_sended);
            if (Database.IndexExists("ix_fs_9", fn_sended)) Database.RemoveIndex("ix_fs_9", fn_sended);

            SchemaQualifiedObjectName fn_sended_dom = new SchemaQualifiedObjectName() { Name = "fn_sended_dom", Schema = CurrentSchema };
            if (Database.IndexExists("ix_fs_dom_8", fn_sended_dom)) Database.RemoveIndex("ix_fs_dom_8", fn_sended_dom);
            if (Database.IndexExists("ix_fs_dom_9", fn_sended_dom)) Database.RemoveIndex("ix_fs_dom_9", fn_sended_dom);
        }
    }
}
