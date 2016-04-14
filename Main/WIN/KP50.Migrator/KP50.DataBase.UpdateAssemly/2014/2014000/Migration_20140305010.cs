using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(20140305010, MigrateDataBase.CentralBank)]
    public class Migration_20140305010_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName bc_types = new SchemaQualifiedObjectName() { Name = "bc_types", Schema = CurrentSchema };
            if (!Database.TableExists(bc_types))
            {
                Database.AddTable(bc_types,
                    new Column("id", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("name_", DbType.String.WithSize(100)),
                    new Column("is_active", DbType.Int16));
                Database.AddIndex("ix1_types", true, bc_types, "id");
            }

            SchemaQualifiedObjectName bc_schema = new SchemaQualifiedObjectName() { Name = "bc_schema", Schema = CurrentSchema };
            if (!Database.TableExists(bc_schema))
            {
                Database.AddTable(bc_schema,
                    new Column("id", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("id_bc_type", DbType.Int32),
                    new Column("id_bc_row_type", DbType.Int16),
                    new Column("num", DbType.Int32),
                    new Column("tag_name", DbType.String.WithSize(50)),
                    new Column("tag_descr", DbType.String.WithSize(250)),
                    new Column("id_bc_field", DbType.Int32),
                    new Column("is_requared", DbType.Int16),
                    new Column("is_show_empty", DbType.Int16));
                Database.AddIndex("ix1_schema", true, bc_schema, "id");
            }

            SchemaQualifiedObjectName bc_fields = new SchemaQualifiedObjectName() { Name = "bc_fields", Schema = CurrentSchema };
            if (!Database.TableExists(bc_fields))
            {
                Database.AddTable(bc_fields,
                    new Column("id", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("name_", DbType.String.WithSize(80)),
                    new Column("note_", DbType.String.WithSize(150)));
                Database.AddIndex("ix1_fields", true, bc_fields, "id");
            }

            SchemaQualifiedObjectName bc_row_type = new SchemaQualifiedObjectName() { Name = "bc_row_type", Schema = CurrentSchema };
            if (!Database.TableExists(bc_row_type))
            {
                Database.AddTable(bc_row_type,
                    new Column("id", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("name_", DbType.String.WithSize(255)));
                Database.AddIndex("ix1_row_type", true, bc_row_type, "id");
            }

            SchemaQualifiedObjectName s_payer = new SchemaQualifiedObjectName() { Name = "s_payer", Schema = CurrentSchema };
            if (!Database.ColumnExists(s_payer, "bik")) Database.AddColumn(s_payer, new Column("bik", DbType.String.WithSize(9)));
            if (!Database.ColumnExists(s_payer, "ks")) Database.AddColumn(s_payer, new Column("ks", DbType.String.WithSize(20)));
            if (!Database.ColumnExists(s_payer, "id_bc_type")) Database.AddColumn(s_payer, new Column("id_bc_type", DbType.Int32));
            if (!Database.ColumnExists(s_payer, "city")) Database.AddColumn(s_payer, new Column("city", DbType.String.WithSize(40)));

            SetSchema(Bank.Data);
            SchemaQualifiedObjectName efs_reestr = new SchemaQualifiedObjectName() { Name = "efs_reestr", Schema = CurrentSchema };
            if (!Database.TableExists(efs_reestr))
            {
                Database.AddTable(efs_reestr,
                    new Column("nzp_efs_reestr", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("file_name", DbType.StringFixedLength.WithSize(20)),
                    new Column("date_uchet", DbType.Date),
                    new Column("packstatus", DbType.Int32),
                    new Column("changed_on", DbType.DateTime),
                    new Column("changed_by", DbType.Int32));
                Database.AddIndex("ind_efs_reestr", true, efs_reestr, "nzp_efs_reestr");
            }

            SchemaQualifiedObjectName bc_reestr = new SchemaQualifiedObjectName() { Name = "bc_reestr", Schema = CurrentSchema };
            if (!Database.TableExists(bc_reestr))
            {
                Database.AddTable(bc_reestr,
                    new Column("id", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("date_reestr", DbType.DateTime),
                    new Column("num_reestr", DbType.Int32),
                    new Column("nzp_user", DbType.Int32));
                Database.AddIndex("ix1_reestr", true, bc_reestr, "id");
            }

            SchemaQualifiedObjectName bc_reestr_files = new SchemaQualifiedObjectName() { Name = "bc_reestr_files", Schema = CurrentSchema };
            if (!Database.TableExists(bc_reestr_files))
            {
                Database.AddTable(bc_reestr_files,
                    new Column("id", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("id_bc_reestr", DbType.Int32),
                    new Column("id_bc_type", DbType.Int32),
                    new Column("id_payer_bank", DbType.Int32),
                    new Column("file_name", DbType.String.WithSize(255)),
                    new Column("nzp_exc", DbType.Int32),
                    new Column("is_treaster", DbType.Int16));
                Database.AddIndex("ix1_reestr_files", true, bc_reestr_files, "id");
            }

            SchemaQualifiedObjectName fn_bank = new SchemaQualifiedObjectName() { Name = "fn_bank", Schema = CurrentSchema };
            if (!Database.ColumnExists(fn_bank, "nzp_payer_bank")) Database.AddColumn(fn_bank, new Column("nzp_payer_bank", DbType.Int32));
        }

        public override void Revert()
        {
            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName bc_types = new SchemaQualifiedObjectName() { Name = "bc_types", Schema = CurrentSchema };
            SchemaQualifiedObjectName bc_schema = new SchemaQualifiedObjectName() { Name = "bc_schema", Schema = CurrentSchema };
            SchemaQualifiedObjectName bc_fields = new SchemaQualifiedObjectName() { Name = "bc_fields", Schema = CurrentSchema };
            SchemaQualifiedObjectName bc_row_type = new SchemaQualifiedObjectName() { Name = "bc_row_type", Schema = CurrentSchema };
            if (Database.TableExists(bc_types)) Database.RemoveTable(bc_types);
            if (Database.TableExists(bc_schema)) Database.RemoveTable(bc_schema);
            if (Database.TableExists(bc_fields)) Database.RemoveTable(bc_fields);
            if (Database.TableExists(bc_row_type)) Database.RemoveTable(bc_row_type);

            SchemaQualifiedObjectName s_payer = new SchemaQualifiedObjectName() { Name = "s_payer", Schema = CurrentSchema };
            if (Database.ColumnExists(s_payer, "bik")) Database.RemoveColumn(s_payer, "bik");
            if (Database.ColumnExists(s_payer, "ks")) Database.RemoveColumn(s_payer, "ks");
            if (Database.ColumnExists(s_payer, "id_bc_type")) Database.RemoveColumn(s_payer, "id_bc_type");
            if (Database.ColumnExists(s_payer, "city")) Database.RemoveColumn(s_payer, "city");

            SetSchema(Bank.Data);
            SchemaQualifiedObjectName efs_reestr = new SchemaQualifiedObjectName() { Name = "efs_reestr", Schema = CurrentSchema };
            SchemaQualifiedObjectName bc_reestr = new SchemaQualifiedObjectName() { Name = "bc_reestr", Schema = CurrentSchema };
            SchemaQualifiedObjectName bc_reestr_files = new SchemaQualifiedObjectName() { Name = "bc_reestr_files", Schema = CurrentSchema };
            SchemaQualifiedObjectName fn_bank = new SchemaQualifiedObjectName() { Name = "fn_bank", Schema = CurrentSchema };
            if (Database.TableExists(efs_reestr)) Database.RemoveTable(efs_reestr);
            if (Database.TableExists(bc_reestr)) Database.RemoveTable(bc_reestr);
            if (Database.TableExists(bc_reestr_files)) Database.RemoveTable(bc_reestr_files);
            if (!Database.ColumnExists(fn_bank, "nzp_payer_bank")) Database.AddColumn(fn_bank, new Column("nzp_payer_bank", DbType.Int32));
        }
    }


    [Migration(20140305010, MigrateDataBase.Charge)]
    public class Migration_20140305010_Charge : Migration
    {
        public override void Apply()
        {
            SchemaQualifiedObjectName counters_ord = new SchemaQualifiedObjectName() { Name = "counters_ord", Schema = CurrentSchema };
            if (!Database.ColumnExists(counters_ord, "nzp_counter"))
                Database.AddColumn(counters_ord, new Column("nzp_counter", DbType.Int32, ColumnProperty.None, 0));
        }

        public override void Revert()
        {
            SchemaQualifiedObjectName counters_ord = new SchemaQualifiedObjectName() { Name = "counters_ord", Schema = CurrentSchema };
            if (Database.ColumnExists(counters_ord, "nzp_counter")) Database.RemoveColumn(counters_ord, "nzp_counter");
        }
    }

    [Migration(20140305010, MigrateDataBase.Fin)]
    public class Migration_20140305010_Fin : Migration
    {
        public override void Apply()
        {
            // TODO: Upgrade Fins
            SchemaQualifiedObjectName efs_pay = new SchemaQualifiedObjectName() { Name = "efs_pay", Schema = CurrentSchema };
            if (!Database.TableExists(efs_pay))
            {
                Database.AddTable(efs_pay,
                    new Column("nzp_pay", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("nzp_efs_reestr", DbType.Int32),
                    new Column("id_pay", DbType.Decimal.WithSize(13)),
                    new Column("id_serv", DbType.StringFixedLength.WithSize(30)),
                    new Column("ls_num", DbType.Decimal.WithSize(13)),
                    new Column("summa", DbType.Decimal.WithSize(14, 2)),
                    new Column("pay_date", DbType.Date),
                    new Column("barcode", DbType.StringFixedLength.WithSize(30)),
                    new Column("address", DbType.StringFixedLength.WithSize(255)),
                    new Column("plpor_num", DbType.Int32),
                    new Column("plpor_date", DbType.Date));

                Database.AddIndex("ind_efs_pay", false, efs_pay, "nzp_efs_reestr", "nzp_pay", "id_pay", "id_serv");
            }

            SchemaQualifiedObjectName efs_cnt = new SchemaQualifiedObjectName() { Name = "efs_cnt", Schema = CurrentSchema };
            if (!Database.TableExists(efs_cnt))
            {
                Database.AddTable(efs_cnt,
                    new Column("nzp_cnt", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("nzp_efs_reestr", DbType.Int32),
                    new Column("id_pay", DbType.Decimal.WithSize(13)),
                    new Column("cnt_num", DbType.Int32),
                    new Column("cnt_val", DbType.Decimal.WithSize(10, 4)),
                    new Column("cnt_val_be", DbType.Decimal.WithSize(10, 4)));
                Database.AddIndex("ind_efs_cnt", false, efs_cnt, "nzp_efs_reestr", "nzp_cnt", "id_pay");
            }

            for (int i = 1; i <= 12; i++)
            {
                SchemaQualifiedObjectName fn_distrib_dom = new SchemaQualifiedObjectName() { Name = string.Format("fn_distrib_dom_{0}", i.ToString("00")), Schema = CurrentSchema };
                string idx_name = string.Format("ix_fnd_dom_3_{0}", i.ToString("00"));
                if (!Database.IndexExists(idx_name, fn_distrib_dom)) Database.AddIndex(idx_name, false, fn_distrib_dom, "nzp_payer", "nzp_area", "nzp_serv", "nzp_dom");
            }
        }

        public override void Revert()
        {
            SchemaQualifiedObjectName efs_pay = new SchemaQualifiedObjectName() { Name = "efs_pay", Schema = CurrentSchema };
            SchemaQualifiedObjectName efs_cnt = new SchemaQualifiedObjectName() { Name = "efs_cnt", Schema = CurrentSchema };
            if (Database.TableExists(efs_pay)) Database.RemoveTable(efs_pay);
            if (Database.TableExists(efs_cnt)) Database.RemoveTable(efs_cnt);

            for (int i = 1; i <= 12; i++)
            {
                SchemaQualifiedObjectName fn_distrib_dom = new SchemaQualifiedObjectName() { Name = string.Format("fn_distrib_dom_{0}", i.ToString("00")), Schema = CurrentSchema };
                string idx_name = string.Format("ix_fnd_3_dom{0}", i.ToString("00"));
                if (Database.IndexExists(idx_name, fn_distrib_dom)) Database.RemoveIndex(idx_name, fn_distrib_dom);
            }
        }
    }
}
