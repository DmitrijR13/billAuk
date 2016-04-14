using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015064
{

    [Migration(2015072906401, MigrateDataBase.Fin)]
    public class Migration_2015072906401_Fin : Migration
    {
        public override void Apply()
        {
            var pack_ls_arch = new SchemaQualifiedObjectName() { Name = "pack_ls_arch", Schema = CurrentSchema };
            if (!Database.TableExists(pack_ls_arch))
            {
                Database.AddTable(pack_ls_arch,
                    new Column("nzp_pack_ls", DbType.Int32, ColumnProperty.PrimaryKeyWithIdentity | ColumnProperty.Unique),
                    new Column("nzp_pack", DbType.Int32),
                    new Column("prefix_ls", DbType.Int32),
                    new Column("num_ls", DbType.Int32),
                    new Column("g_sum_ls", DbType.Decimal.WithSize(10, 2)),
                    new Column("erc_code", DbType.String.WithSize(12)),
                    new Column("sum_ls", DbType.Decimal.WithSize(10, 2)),
                    new Column("geton_ls", DbType.Decimal.WithSize(10, 2)),
                    new Column("sum_peni", DbType.Decimal.WithSize(10, 2)),
                    new Column("dat_month", DbType.Date),
                    new Column("kod_sum", DbType.Int16),
                    new Column("nzp_supp", DbType.Int32),
                    new Column("paysource", DbType.Int32, ColumnProperty.None, 0),
                    new Column("id_bill", DbType.Int32),
                    new Column("dat_vvod", DbType.DateTime),
                    new Column("dat_uchet", DbType.DateTime),
                    new Column("anketa", DbType.String.WithSize(10)),
                    new Column("info_num", DbType.Int32),
                    new Column("inbasket", DbType.Int16),
                    new Column("alg", DbType.String.WithSize(2)),
                    new Column("unl", DbType.Int32),
                    new Column("date_distr", DbType.DateTime),
                    new Column("date_rdistr", DbType.DateTime),
                    new Column("nzp_user", DbType.Int32),
                    new Column("incase", DbType.Int16, ColumnProperty.None, 0),
                    new Column("nzp_rs", DbType.Int32, ColumnProperty.NotNull, 1),
                    new Column("distr_month", DbType.Date),
                    new Column("pkod", DbType.Decimal.WithSize(13, 0)),
                    new Column("transaction_id", DbType.String.WithSize(255)),
                    new Column("month_from", DbType.Date),
                    new Column("month_to", DbType.Date),
                    new Column("type_pay", DbType.Int32),
                    new Column("nzp_payer", DbType.Int32),
                    new Column("old_num_ls", DbType.String.WithSize(100)),
                    new Column("sum_peni_off", DbType.Decimal.WithSize(10, 2), ColumnProperty.NotNull, 0.00)
                    );
                Database.AddIndex("idx_pack_ls_arch_00", false, pack_ls_arch, new[] { "num_ls", "dat_month" });
                Database.AddIndex("idx_pack_ls_arch_1", false, pack_ls_arch, new[] { "date_distr" });
                Database.AddIndex("idx_pack_ls_arch_2", false, pack_ls_arch, new[] { "date_rdistr" });
                Database.AddIndex("idx_pack_ls_arch_incase", false, pack_ls_arch, new[] { "incase" });
                Database.AddIndex("ix_pack_ls_arch_1", false, pack_ls_arch, new[] { "nzp_pack_ls" });
                Database.AddIndex("ix_pcls_arch_101", false, pack_ls_arch, new[] { "inbasket", "nzp_pack_ls" });
                Database.AddIndex("ix_pls_arch2", false, pack_ls_arch, new[] { "nzp_pack" });
                Database.AddIndex("ix_pls_arch_1", false, pack_ls_arch, new[] { "dat_uchet", "inbasket" });
                Database.AddIndex("ix_pls_arch_unl", false, pack_ls_arch, new[] { "unl" });
                Database.AddIndex("ix_rs_arch12", false, pack_ls_arch, new[] { "nzp_rs" });
            }

            var pack_ls = new SchemaQualifiedObjectName() { Name = "pack_ls", Schema = CurrentSchema };
            if (Database.TableExists(pack_ls))
            {
                if (!Database.ColumnExists(pack_ls, "changed_by"))
                    Database.AddColumn(pack_ls, new Column("changed_by", DbType.Int32));
                if (!Database.ColumnExists(pack_ls, "changed_on"))
                    Database.AddColumn(pack_ls, new Column("changed_on", DbType.DateTime));
            }

            if (Database.TableExists(pack_ls_arch))
            {
                if (!Database.ColumnExists(pack_ls_arch, "changed_by"))
                    Database.AddColumn(pack_ls_arch, new Column("changed_by", DbType.Int32));
                if (!Database.ColumnExists(pack_ls_arch, "changed_on"))
                    Database.AddColumn(pack_ls_arch, new Column("changed_on", DbType.DateTime, ColumnProperty.None, "now()"));
            }

            Database.ExecuteReader(" DROP TRIGGER IF EXISTS pack_ls_arch_add ON " + pack_ls_arch.Schema + Database.TableDelimiter + "pack_ls ");

            Database.ExecuteReader(" CREATE OR REPLACE FUNCTION " + pack_ls_arch.Schema + Database.TableDelimiter + "add_to_pack_ls_arch()" +
                                   " RETURNS trigger LANGUAGE plpgsql AS $body$ " +
                                   " BEGIN " +
                                   " INSERT INTO " + pack_ls_arch.Schema + Database.TableDelimiter + "pack_ls_arch (" +
                                   " nzp_pack_ls, nzp_pack, prefix_ls, num_ls, g_sum_ls, erc_code, " +
                                   " sum_ls, geton_ls, sum_peni, dat_month, kod_sum, nzp_supp, paysource, " +
                                   " id_bill, dat_vvod, dat_uchet, anketa, info_num, inbasket, alg, " +
                                   " unl, date_distr, date_rdistr, nzp_user, incase, nzp_rs, distr_month, " +
                                   " pkod, transaction_id, month_from, month_to, type_pay, nzp_payer, " +
                                   " old_num_ls, changed_by, changed_on)" +
                                   " SELECT      nzp_pack_ls, nzp_pack, prefix_ls, num_ls, g_sum_ls, erc_code, " +
                                   " sum_ls, geton_ls, sum_peni, dat_month, kod_sum, nzp_supp, paysource, " +
                                   " id_bill, dat_vvod, dat_uchet, anketa, info_num, inbasket, alg, " +
                                   " unl, date_distr, date_rdistr, nzp_user, incase, nzp_rs, distr_month, " +
                                   " pkod, transaction_id, month_from, month_to, type_pay, nzp_payer, " +
                                   " old_num_ls, changed_by, now() " +
                                   " FROM  " + pack_ls_arch.Schema + Database.TableDelimiter + "pack_ls WHERE nzp_pack_ls = OLD.nzp_pack_ls;                      " +
                                   " return OLD; " +
                                   " END; " +
                                   " $body$; " +
                                   " CREATE TRIGGER pack_ls_arch_add BEFORE DELETE " +
                                   " ON  " + pack_ls_arch.Schema + Database.TableDelimiter + "pack_ls FOR EACH ROW " +
                                   " EXECUTE PROCEDURE " + pack_ls_arch.Schema + Database.TableDelimiter + "add_to_pack_ls_arch(); ");

            var pack_arch = new SchemaQualifiedObjectName() { Name = "pack_arch", Schema = CurrentSchema };
            if (!Database.TableExists(pack_arch))
            {
                Database.AddTable(pack_arch,
                    new Column("nzp_pack", DbType.Int32,
                        ColumnProperty.PrimaryKeyWithIdentity | ColumnProperty.Unique),
                    new Column("par_pack", DbType.Int32),
                    new Column("pack_type", DbType.Int16),
                    new Column("nzp_bank", DbType.Int32),
                    new Column("nzp_supp", DbType.Int32),
                    new Column("nzp_oper", DbType.Int32),
                    new Column("num_pack", DbType.String.WithSize(10)),
                    new Column("dat_uchet", DbType.DateTime),
                    new Column("dat_pack", DbType.Date),
                    new Column("num_charge", DbType.Int16),
                    new Column("yearr", DbType.Int32),
                    new Column("count_kv", DbType.Int32),
                    new Column("sum_pack", DbType.Decimal.WithSize(14, 2)),
                    new Column("geton_pack", DbType.Decimal.WithSize(14, 2)),
                    new Column("real_sum", DbType.Decimal.WithSize(14, 2)),
                    new Column("real_geton", DbType.Decimal.WithSize(14, 2)),
                    new Column("real_count", DbType.Int32),
                    new Column("flag", DbType.Int16),
                    new Column("dat_vvod", DbType.DateTime),
                    new Column("islock", DbType.Int16),
                    new Column("operday_payer", DbType.Date),
                    new Column("peni_pack", DbType.Decimal.WithSize(14, 2)),
                    new Column("sum_rasp", DbType.Decimal.WithSize(14, 2)),
                    new Column("sum_nrasp", DbType.Decimal.WithSize(14, 2)),
                    new Column("nzp_rs", DbType.Int32, ColumnProperty.NotNull, 1),
                    new Column("dat_inp", DbType.Date, ColumnProperty.None, "now()"),
                    new Column("time_inp", DbType.DateTime, ColumnProperty.None, "now()"),
                    new Column("erc_code", DbType.String.WithSize(12)),
                    new Column("file_name", DbType.String.WithSize(200)),
                    new Column("changed_by", DbType.Int32),
                    new Column("changed_on", DbType.DateTime),
                    new Column("nzp_payer", DbType.Int32),
                    new Column("nzp_payer_agent", DbType.Int32)
                    );
            }

            Database.ExecuteReader(" DROP TRIGGER IF EXISTS pack_arch_add ON " + pack_arch.Schema + Database.TableDelimiter + "pack ");


            Database.ExecuteReader(" CREATE OR REPLACE FUNCTION " + pack_arch.Schema + Database.TableDelimiter + "add_to_pack_arch()" +
                                   " RETURNS trigger LANGUAGE plpgsql AS $body$ " +
                                   " BEGIN " +
                                   " INSERT INTO " + pack_arch.Schema + Database.TableDelimiter + "pack_arch (" +
                                   " nzp_pack, par_pack, pack_type, nzp_bank, nzp_supp, nzp_oper, " +
                                   " num_pack, dat_uchet, dat_pack, num_charge, yearr, count_kv, sum_pack, " +
                                   " geton_pack, real_sum, real_geton, real_count, flag, dat_vvod, " +
                                   " islock, operday_payer, peni_pack, sum_rasp, sum_nrasp, nzp_rs, " +
                                   " dat_inp, time_inp, erc_code, file_name, changed_by, changed_on, " +
                                   " nzp_payer, nzp_payer_agent)" +
                                   " SELECT nzp_pack, par_pack, pack_type, nzp_bank, nzp_supp, nzp_oper, " +
                                   " num_pack, dat_uchet, dat_pack, num_charge, yearr, count_kv, sum_pack, " +
                                   " geton_pack, real_sum, real_geton, real_count, flag, dat_vvod, " +
                                   " islock, operday_payer, peni_pack, sum_rasp, sum_nrasp, nzp_rs, " +
                                   " dat_inp, time_inp, erc_code, file_name, changed_by, now(), " +
                                   " nzp_payer, nzp_payer_agent " +
                                   " FROM  " + pack_arch.Schema + Database.TableDelimiter + "pack WHERE nzp_pack = OLD.nzp_pack;                      " +
                                   " return OLD; " +
                                   " END; " +
                                   " $body$; " +
                                   " CREATE TRIGGER pack_arch_add BEFORE DELETE " +
                                   " ON  " + pack_arch.Schema + Database.TableDelimiter + "pack FOR EACH ROW " +
                                   " EXECUTE PROCEDURE " + pack_arch.Schema + Database.TableDelimiter + "add_to_pack_arch(); ");
            //на некоторых базах есть эта таблица, но без этих колонок
            if (Database.TableExists(pack_arch))
            {
                if (!Database.ColumnExists(pack_arch, "time_inp"))
                {
                    Database.AddColumn(pack_arch, new Column("time_inp", DbType.DateTime, ColumnProperty.None, "now()"));
                }
                if (!Database.ColumnExists(pack_arch, "nzp_payer_agent"))
                {
                    Database.AddColumn(pack_arch, new Column("nzp_payer_agent", DbType.Int32));
                }
            }

            var gil_sums_arch = new SchemaQualifiedObjectName() { Name = "gil_sums_arch", Schema = CurrentSchema };
            if (!Database.TableExists(gil_sums_arch))
            {
                Database.AddTable(gil_sums_arch,
                    new Column("nzp_sums", DbType.Int32, ColumnProperty.PrimaryKeyWithIdentity | ColumnProperty.Unique),
                    new Column("nzp_pack_ls", DbType.Int32),
                    new Column("num_ls", DbType.Int32),
                    new Column("nzp_serv", DbType.Int32),
                    new Column("days_nedo", DbType.Int32),
                    new Column("sum_nach", DbType.Decimal.WithSize(10, 2)),
                    new Column("sum_oplat", DbType.Decimal.WithSize(10, 2)),
                    new Column("dat_month", DbType.Date),
                    new Column("ordering", DbType.Int32),
                    new Column("dat_uchet", DbType.Date),
                    new Column("is_union", DbType.Int32, ColumnProperty.None, 0),
                    new Column("nzp_supp", DbType.Int32)
                    );
                Database.AddIndex("idx_sums_arch", false, gil_sums_arch, new[] { "nzp_sums" });
                Database.AddIndex("idx_sums_arch_1", false, gil_sums_arch, new[] { "num_ls", "nzp_serv" });
                Database.AddIndex("idx_sums_arch_2", false, gil_sums_arch, new[] { "nzp_serv", "num_ls" });
                Database.AddIndex("idx_sums_arch_3", false, gil_sums_arch, new[] { "nzp_pack_ls" });
                Database.AddIndex("idx_sums_arch_4", false, gil_sums_arch, new[] { "nzp_pack_ls", "sum_oplat" });
            }

            Database.ExecuteReader(" DROP TRIGGER IF EXISTS gil_sums_arch_add ON " + pack_ls_arch.Schema + Database.TableDelimiter + "gil_sums ");

            Database.ExecuteReader(" CREATE OR REPLACE FUNCTION " + pack_ls_arch.Schema + Database.TableDelimiter + "add_to_gil_sums_arch()" +
                                   " RETURNS trigger LANGUAGE plpgsql AS $body$ " +
                                   " BEGIN " +
                                   " INSERT INTO " + pack_ls_arch.Schema + Database.TableDelimiter + "gil_sums_arch (" +
                                   " nzp_sums, nzp_pack_ls, num_ls, nzp_serv, days_nedo, sum_nach, " +
                                   " sum_oplat, dat_month, ordering, dat_uchet, is_union, nzp_supp)" +
                                   " SELECT      nzp_sums, nzp_pack_ls, num_ls, nzp_serv, days_nedo, sum_nach, " +
                                   " sum_oplat, dat_month, ordering, dat_uchet, is_union, nzp_supp" +
                                   " FROM " + pack_ls_arch.Schema + Database.TableDelimiter + "gil_sums WHERE nzp_sums = OLD.nzp_sums;" +
                                   " return OLD; " +
                                   " END; " +
                                   " $body$; " +
                                   " CREATE TRIGGER gil_sums_arch_add BEFORE DELETE " +
                                   " ON " + pack_ls_arch.Schema + Database.TableDelimiter + "gil_sums FOR EACH ROW " +
                                   " EXECUTE PROCEDURE " + pack_ls_arch.Schema + Database.TableDelimiter + "add_to_gil_sums_arch();");

            var pu_vals_arch = new SchemaQualifiedObjectName() { Name = "pu_vals_arch", Schema = CurrentSchema };
            if (!Database.TableExists(pu_vals_arch))
            {
                Database.AddTable(pu_vals_arch,
                    new Column("nzp_pv", DbType.Int32, ColumnProperty.PrimaryKeyWithIdentity | ColumnProperty.Unique),
                    new Column("nzp_pack_ls", DbType.Int32),
                    new Column("num_ls", DbType.Int32),
                    new Column("nzp_ck", DbType.Int32),
                    new Column("val_cnt", DbType.Decimal.WithSize(14, 4)),
                    new Column("dat_month", DbType.Date),
                    new Column("pu_order", DbType.Int32),
                    new Column("cur_unl", DbType.Int32),
                    new Column("nzp_counter", DbType.Int32)
                    );
                Database.AddIndex("ix_pu_vals_arch_1", false, pu_vals_arch, new[] { "nzp_pv" });
                Database.AddIndex("ix_pu_vals_arch_2", false, pu_vals_arch, new[] { "nzp_pack_ls" });
            }

            Database.ExecuteReader(" DROP TRIGGER IF EXISTS pu_vals_arch_add ON  " + pack_ls_arch.Schema + Database.TableDelimiter + "pu_vals ");

            Database.ExecuteReader(" CREATE OR REPLACE FUNCTION " + pack_ls_arch.Schema + Database.TableDelimiter + "add_to_pu_vals_arch()" +
                                   " RETURNS trigger LANGUAGE plpgsql AS $body$ " +
                                   " BEGIN " +
                                   " INSERT INTO  " + pack_ls_arch.Schema + Database.TableDelimiter + "pu_vals_arch (" +
                                   " nzp_pv, nzp_pack_ls, num_ls, nzp_ck, val_cnt, dat_month, pu_order, " +
                                   " cur_unl, nzp_counter)" +
                                   " SELECT      nzp_pv, nzp_pack_ls, num_ls, nzp_ck, val_cnt, dat_month, pu_order, " +
                                   " cur_unl, nzp_counter" +
                                   " FROM  " + pack_ls_arch.Schema + Database.TableDelimiter + "pu_vals WHERE nzp_pv = OLD.nzp_pv;" +
                                   " return OLD; " +
                                   " END; " +
                                   " $body$; " +
                                   " CREATE TRIGGER pu_vals_arch_add BEFORE DELETE " +
                                   " ON  " + pack_ls_arch.Schema + Database.TableDelimiter + "pu_vals FOR EACH ROW " +
                                   " EXECUTE PROCEDURE  " + pack_ls_arch.Schema + Database.TableDelimiter + "add_to_pu_vals_arch();");

        }

        public override void Revert()
        {
            // TODO: Downgrade Fins

        }
    }

}
