using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;
using System.Collections.Generic;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(20140305008, MigrateDataBase.CentralBank)]
    public class Migration_20140305008_CentralBank : Migration
    {
        public override void Apply()
        {
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

            SchemaQualifiedObjectName files_imported = new SchemaQualifiedObjectName() { Name = "files_imported", Schema = CurrentSchema };
            if (Database.TableExists(files_imported) && !Database.ColumnExists(files_imported, "diss_status")) Database.AddColumn(files_imported, new Column("diss_status", DbType.StringFixedLength.WithSize(50)));
        }

        public override void Revert()
        {
            SetSchema(Bank.Data);
            SchemaQualifiedObjectName efs_reestr = new SchemaQualifiedObjectName() { Name = "efs_reestr", Schema = CurrentSchema };
            if (Database.TableExists(efs_reestr)) Database.RemoveTable(efs_reestr);
        }
    }

    [Migration(20140305008, MigrateDataBase.Charge)]
    public class Migration_20140305008_Charge : Migration
    {
        public override void Apply()
        {
            SchemaQualifiedObjectName counters_ord = new SchemaQualifiedObjectName() { Name = "counters_ord", Schema = CurrentSchema };
            if (!Database.ColumnExists(counters_ord, "nzp_counter")) Database.AddColumn(counters_ord, new Column("nzp_counter", DbType.Int32, ColumnProperty.None, 0));
        }

        public override void Revert()
        {
            SchemaQualifiedObjectName counters_ord = new SchemaQualifiedObjectName() { Name = "counters_ord", Schema = CurrentSchema };
            if (Database.ColumnExists(counters_ord, "nzp_counter")) Database.RemoveColumn(counters_ord, "nzp_counter");
        }
    }

    [Migration(20140305008, MigrateDataBase.Fin)]
    public class Migration_20140305008_Fin : Migration
    {
        public override void Apply()
        {
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

            SchemaQualifiedObjectName pack = new SchemaQualifiedObjectName() { Name = "pack", Schema = CurrentSchema };
            SchemaQualifiedObjectName pack_ls = new SchemaQualifiedObjectName() { Name = "pack_ls", Schema = CurrentSchema };
            if (Database.IndexExists("ix193_1", pack))
            {
                if (Database.ProviderName == "Informix") Database.ExecuteNonQuery(string.Format("RENAME INDEX \"{0}\"{1}ix193_1 TO ix_pack_1", CurrentSchema, Database.TableDelimiter));
                if (Database.ProviderName == "PostgreSQL") Database.ExecuteNonQuery(string.Format("ALTER INDEX \"{0}\"{1}ix193_1 RENAME TO ix_pack_1", CurrentSchema, Database.TableDelimiter));
            }
            if (Database.IndexExists("ix194_1", pack_ls))
            {
                if (Database.ProviderName == "Informix") Database.ExecuteNonQuery(string.Format("RENAME INDEX \"{0}\"{1}ix194_1 TO ix_pack_ls_1", CurrentSchema, Database.TableDelimiter));
                if (Database.ProviderName == "PostgreSQL") Database.ExecuteNonQuery(string.Format("ALTER INDEX \"{0}\"{1}ix194_1 RENAME TO ix_pack_ls_1", CurrentSchema, Database.TableDelimiter));
            }
            
            for (int i = 1; i <= 12; i++)
            {
                SchemaQualifiedObjectName fn_distrib_dom = new SchemaQualifiedObjectName() { Name = string.Format("fn_distrib_dom_{0}", i.ToString("00")), Schema = CurrentSchema };
                string IndexName = string.Format("ix_fnd_dom_3_{0}", i.ToString("00"));
                if (Database.IndexExists(IndexName.Replace("dom_", ""), fn_distrib_dom))
                {
                    if (Database.ProviderName == "Informix") Database.ExecuteNonQuery(string.Format("RENAME INDEX \"{0}\"{1}{2} TO {3}", CurrentSchema, Database.TableDelimiter, IndexName.Replace("dom_", ""), IndexName));
                    if (Database.ProviderName == "PostgreSQL") Database.ExecuteNonQuery(string.Format("ALTER INDEX \"{0}\"{1}{2} RENAME TO {3}", CurrentSchema, Database.TableDelimiter, IndexName.Replace("dom_", ""), IndexName));
                }
                if (!Database.IndexExists(IndexName, fn_distrib_dom)) Database.AddIndex(IndexName, false, fn_distrib_dom, "nzp_payer", "nzp_area", "nzp_serv", "nzp_dom");
            }
        }

        public override void Revert()
        {
            SchemaQualifiedObjectName efs_pay = new SchemaQualifiedObjectName() { Name = "efs_pay", Schema = CurrentSchema };
            if (Database.TableExists(efs_pay)) Database.RemoveTable(efs_pay);

            SchemaQualifiedObjectName efs_cnt = new SchemaQualifiedObjectName() { Name = "efs_cnt", Schema = CurrentSchema };
            if (Database.TableExists(efs_cnt)) Database.RemoveTable(efs_cnt);

            for (int i = 1; i <= 12; i++)
            {
                SchemaQualifiedObjectName Table = new SchemaQualifiedObjectName() { Name = string.Format("fn_distrib_dom_{0}", i.ToString("00")), Schema = CurrentSchema };
                string IndexName = string.Format("ix_fnd_dom_3_{0}", i.ToString("00"));
                if (Database.IndexExists(IndexName, Table)) Database.RemoveIndex(IndexName, Table);
            }
        }
    }

    [Migration(20140305008, MigrateDataBase.Web)]
    public class Migration_20140305008_Web : Migration
    {
        public override void Apply()
        {
            SchemaQualifiedObjectName source_pack = new SchemaQualifiedObjectName() { Name = "source_pack", Schema = CurrentSchema };
            if (!Database.TableExists(source_pack))
            {
                Database.AddTable(source_pack,
                    new Column("nzp_spack", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("par_pack", DbType.Int32),
                    new Column("nzp_user", DbType.Int32),
                    new Column("nzp_session", DbType.Int32),
                    new Column("place_of_made", DbType.StringFixedLength.WithSize(200)),
                    new Column("erc_code", DbType.StringFixedLength.WithSize(12)),
                    new Column("num_pack", DbType.StringFixedLength.WithSize(10)),
                    new Column("date_pack", DbType.Date),
                    new Column("time_pack", DbType.DateTime),
                    new Column("date_oper", DbType.Date),
                    new Column("count_in_pack", DbType.Int32),
                    new Column("sum_pack", DbType.Decimal.WithSize(14, 2)),
                    new Column("sum_nach", DbType.Decimal.WithSize(14, 2)),
                    new Column("sum_geton", DbType.Decimal.WithSize(13)),
                    new Column("version", DbType.StringFixedLength.WithSize(5)),
                    new Column("filename", DbType.StringFixedLength.WithSize(250)),
                    new Column("date_inp", DbType.Date),
                    new Column("time_inp", DbType.DateTime));

                Database.AddIndex("ix_spk_01", true, source_pack, "nzp_spack");
                Database.AddIndex("ix_spk_02", false, source_pack, "nzp_user");
                Database.AddIndex("ix_spk_03", false, source_pack, "nzp_session");
                Database.AddIndex("ix_spk_04", false, source_pack, "date_pack");
            }

            SchemaQualifiedObjectName source_pack_ls = new SchemaQualifiedObjectName() { Name = "source_pack_ls", Schema = CurrentSchema };
            if (!Database.TableExists(source_pack_ls))
            {
                Database.AddTable(source_pack_ls,
                    new Column("nzp_spack_ls", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("nzp_spack", DbType.Int32),
                    new Column("paycode", DbType.Decimal.WithSize(13, 0)),
                    new Column("num_ls", DbType.Int32),
                    new Column("pref", DbType.StringFixedLength.WithSize(10)),
                    new Column("g_sum_ls", DbType.Decimal.WithSize(10, 2)),
                    new Column("sum_ls", DbType.Decimal.WithSize(10, 2)),
                    new Column("geton_ls", DbType.Decimal.WithSize(10, 2)),
                    new Column("sum_peni", DbType.Decimal.WithSize(10, 2)),
                    new Column("dat_month", DbType.Date),
                    new Column("kod_sum", DbType.Int16),
                    new Column("nzp_supp", DbType.Int32),
                    new Column("paysource", DbType.Int32, ColumnProperty.None, 0),
                    new Column("id_bill", DbType.Int32),
                    new Column("dat_vvod", DbType.Date),
                    new Column("anketa", DbType.StringFixedLength.WithSize(10)),
                    new Column("info_num", DbType.Int32),
                    new Column("unl", DbType.Int32),
                    new Column("erc_code", DbType.Int32));

                Database.AddIndex("ix_spl_01", true, source_pack_ls, "nzp_spack_ls");
                Database.AddIndex("ix_spl_02", false, source_pack_ls, "nzp_spack");
                Database.AddIndex("ix_spl_03", false, source_pack_ls, "paycode");
                Database.AddIndex("ix_spl_04", false, source_pack_ls, "num_ls");
            }

            SchemaQualifiedObjectName source_pu_vals = new SchemaQualifiedObjectName() { Name = "source_pu_vals", Schema = CurrentSchema };
            if (!Database.TableExists(source_pu_vals))
            {
                Database.AddTable(source_pu_vals,
                    new Column("nzp_spv", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("nzp_spack_ls", DbType.Int32),
                    new Column("nzp_serv", DbType.Int32),
                    new Column("num_cnt", DbType.StringFixedLength.WithSize(20)),
                    new Column("val_cnt", DbType.Decimal.WithSize(14, 4)),
                    new Column("pu_order", DbType.Int32),
                    new Column("ordering", DbType.Int32));

                Database.AddIndex("ix_spv_01", true, source_pu_vals, "nzp_spv");
                Database.AddIndex("ix_spv_02", false, source_pu_vals, "nzp_spack_ls");
                Database.AddIndex("ix_spv_03", false, source_pu_vals, "nzp_serv");
            }

            SchemaQualifiedObjectName source_gil_sums = new SchemaQualifiedObjectName() { Name = "source_gil_sums", Schema = CurrentSchema };
            if (!Database.TableExists(source_gil_sums))
            {
                Database.AddTable(source_gil_sums,
                    new Column("nzp_ssums", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("nzp_spack_ls", DbType.Int32),
                    new Column("days_nedo", DbType.Int32),
                    new Column("sum_oplat", DbType.Decimal.WithSize(10, 2)),
                    new Column("ordering", DbType.Int32));

                Database.AddIndex("ix_sgs_01", true, source_gil_sums, "nzp_ssums");
                Database.AddIndex("ix_sgs_02", false, source_gil_sums, "nzp_spack_ls");
            }
        }

        public override void Revert()
        {
            SchemaQualifiedObjectName source_pack = new SchemaQualifiedObjectName() { Name = "source_pack", Schema = CurrentSchema };
            SchemaQualifiedObjectName source_pack_ls = new SchemaQualifiedObjectName() { Name = "source_pack_ls", Schema = CurrentSchema };
            SchemaQualifiedObjectName source_pu_vals = new SchemaQualifiedObjectName() { Name = "source_pu_vals", Schema = CurrentSchema };
            SchemaQualifiedObjectName source_gil_sums = new SchemaQualifiedObjectName() { Name = "source_gil_sums", Schema = CurrentSchema };
            if (Database.TableExists(source_pack)) Database.RemoveTable(source_pack);
            if (Database.TableExists(source_pack_ls)) Database.RemoveTable(source_pack_ls);
            if (Database.TableExists(source_pu_vals)) Database.RemoveTable(source_pu_vals);
            if (Database.TableExists(source_gil_sums)) Database.RemoveTable(source_gil_sums);
        }
    }
}
