using System;
using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015064
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2015062406401, MigrateDataBase.CentralBank)]
    public class Migration_2015062406401_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            var s_overpaymentman_statuses = new SchemaQualifiedObjectName() { Name = "s_overpaymentman_statuses", Schema = CurrentSchema };
            if (!Database.TableExists(s_overpaymentman_statuses))
            {
                Database.AddTable("s_overpaymentman_statuses",
                    new Column("nzp_status", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("status", DbType.String.WithSize(30)));
            }

            if (Database.TableExists(s_overpaymentman_statuses))
            {
                Database.Delete(s_overpaymentman_statuses, "nzp_status in (1,2)");
                Database.Insert(s_overpaymentman_statuses, new[] { "nzp_status", "status" }, new[] { "1", "Отбор переплат" });
                Database.Insert(s_overpaymentman_statuses, new[] { "nzp_status", "status" }, new[] { "2", "Распределение переплат" });
            }

            var s_bank = new SchemaQualifiedObjectName() { Name = "s_bank", Schema = CurrentSchema };
            var s_payer = new SchemaQualifiedObjectName() { Name = "s_payer", Schema = CurrentSchema };
            if (Database.TableExists(s_bank))
            {
                if (Convert.ToInt32(Database.ExecuteScalar(" select count(*) from  " + s_bank.Name + " where nzp_bank = 79998")) == 0)
                {
                    if (Database.TableExists(s_payer))
                    {
                        if (Convert.ToInt32(Database.ExecuteScalar(" select count(*) from  " + s_payer.Name + " where nzp_payer = 79998")) == 0)
                        {
                            Database.Insert(s_payer, new string[] { "nzp_payer", "payer", "npayer" },
                            new string[] { "79998", "Безналичный расчёт", "Безналичный расчёт" });
                        }
                    }
                    Database.Insert(s_bank, new string[] { "nzp_bank", "bank", "nzp_payer" },
                    new string[] { "79998", "Безналичный платеж", "79998" });
                }
            }

            SetSchema(Bank.Data);

            var overpayment = new SchemaQualifiedObjectName() { Name = "overpayment", Schema = CurrentSchema };
            if (!Database.TableExists(overpayment))
            {
                Database.AddTable("overpayment",
                    new Column("nzp_key", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("pref", DbType.String.WithSize(20)),
                    new Column("num_ls", DbType.Int32),
                    new Column("nzp_kvar", DbType.Int32),
                    new Column("nzp_serv", DbType.Int32),
                    new Column("nzp_supp", DbType.Int32),
                    new Column("nzp_payer", DbType.Int32),
                    new Column("sum_payer", DbType.Decimal.WithSize(17, 2)),
                    new Column("sum_negative_outsaldo_payer", DbType.Decimal.WithSize(14, 2)),
                    new Column("sum_over_payer", DbType.Decimal.WithSize(17, 2)),
                    new Column("isdel", DbType.Int32),
                    new Column("sum_outsaldo", DbType.Decimal.WithSize(14, 2)),
                    new Column("rsum_tarif", DbType.Decimal.WithSize(14, 2)),
                    new Column("rsum_tarif_ls", DbType.Decimal.WithSize(14, 2), ColumnProperty.NotNull, 0),
                    new Column("sum_dolg_ls", DbType.Decimal.WithSize(14, 2), ColumnProperty.NotNull, 0),
                    new Column("sum_over_ls", DbType.Decimal.WithSize(14, 2), ColumnProperty.NotNull, 0),
                    new Column("sum_change", DbType.Decimal.WithSize(14, 2), ColumnProperty.NotNull, 0),
                    new Column("sum_dolg", DbType.Decimal.WithSize(14, 2), ColumnProperty.NotNull, 0),
                    new Column("sum_avans", DbType.Decimal.WithSize(14, 2), ColumnProperty.NotNull, 0),
                    new Column("sum_dolg_d", DbType.Decimal.WithSize(14, 2), ColumnProperty.NotNull, 0),
                    new Column("sum_dolg_d_ost", DbType.Decimal.WithSize(14, 2), ColumnProperty.NotNull, 0),
                    new Column("sum_avans_d", DbType.Decimal.WithSize(14, 2), ColumnProperty.NotNull, 0),
                    new Column("sum_avans_d_ost", DbType.Decimal.WithSize(14, 2), ColumnProperty.NotNull, 0),
                    new Column("sum_over_d", DbType.Decimal.WithSize(14, 2), ColumnProperty.NotNull, 0),
                    new Column("sum_over_d_ost", DbType.Decimal.WithSize(14, 2), ColumnProperty.NotNull, 0),
                    new Column("ordering_down", DbType.Int32),
                    new Column("sum_up", DbType.Decimal.WithSize(14, 2)),
                    new Column("nzp_pack_ls", DbType.Int32),
                    new Column("mark", DbType.Int32, ColumnProperty.NotNull, 0));

                Database.AddIndex("inx_ovpmnt_1", false, overpayment, "nzp_kvar");
                Database.AddIndex("inx_ovpmnt_2", false, overpayment, "nzp_supp", "nzp_payer", "nzp_serv");
            }
            

            var overpaymentman_status = new SchemaQualifiedObjectName() { Name = "overpaymentman_status", Schema = CurrentSchema };
            if (!Database.TableExists(overpaymentman_status))
            {
                Database.AddTable("overpaymentman_status",
                    new Column("id", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("nzp_user", DbType.Int32, ColumnProperty.NotNull),
                    new Column("nzp_status", DbType.Int32, ColumnProperty.NotNull),
                    new Column("nzp_fon_selection", DbType.Int32),
                    new Column("nzp_fon_distrib", DbType.Int32),
                    new Column("dat_when", DbType.DateTime, ColumnProperty.None, "now()"),
                    new Column("is_actual", DbType.Int32, ColumnProperty.NotNull),
                    new Column("is_interrupted", DbType.Boolean)
                    );
            }


            var joined_overpayments = new SchemaQualifiedObjectName() { Name = "joined_overpayments", Schema = CurrentSchema };
            if (!Database.TableExists(joined_overpayments))
            {
                Database.AddTable("joined_overpayments",
                    new Column("id", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("nzp_payer", DbType.Int32, ColumnProperty.NotNull),
                    new Column("sum_negative_outsaldo_payer", DbType.Decimal.WithSize(14, 2), ColumnProperty.None, 0),
                    new Column("sum_payer", DbType.Decimal.WithSize(14, 2), ColumnProperty.None, 0),
                    new Column("nzp_serv", DbType.Int32),
                    new Column("nzp_supp", DbType.Int32),
                    new Column("sum_outsaldo", DbType.Decimal.WithSize(14, 2), ColumnProperty.None, 0),
                    new Column("ls_count", DbType.Int32),
                    new Column("mark", DbType.Boolean, ColumnProperty.None, false)
                    );
                Database.AddIndex("inx_jov_1", false, joined_overpayments, "nzp_supp", "nzp_payer", "nzp_serv");
            }

            SetSchema(Bank.Upload);
            // TODO: Upgrade CentralPref_Upload
        }

        public override void Revert()
        {
            SetSchema(Bank.Kernel);
            // TODO: Downgrade CentralPref_Kernel

            SetSchema(Bank.Data);
            // TODO: Downgrade CentralPref_Data

            SetSchema(Bank.Upload);
            // TODO: Downgrade CentralPref_Upload
        }
    }

}
