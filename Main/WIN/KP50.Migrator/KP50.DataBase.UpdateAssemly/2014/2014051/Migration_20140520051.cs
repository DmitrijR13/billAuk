using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;
using System;

namespace KP50.DataBase.UpdateAssembly
{


    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(20140520051, MigrateDataBase.Fin)]
    public class Migration_20140520051_Fin : Migration
    {
        public override void Apply()
        {
            for (int i = 1; i <= 12; i++)
            {
                // TODO: Upgrade Fins
                SchemaQualifiedObjectName upload_pu = new SchemaQualifiedObjectName() { Name = "upload_pu_" + i.ToString("00"), Schema = CurrentSchema };
                if (!Database.TableExists(upload_pu))
                {
                    Database.AddTable(upload_pu,
                        new Column("nzp_upload", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                        new Column("nzp_reestr", DbType.Int32),
                        new Column("nzp_kvar", DbType.Int32),
                        new Column("nzp_serv", DbType.Int32),
                        new Column("nzp_counter", DbType.Int32),
                        new Column("ns", DbType.StringFixedLength.WithSize(2)),
                        new Column("usl", DbType.StringFixedLength.WithSize(4))
                        );

                    if (Database.TableExists(upload_pu))
                    {
                        Database.AddIndex("ix_upload_pu_" + i.ToString("00"), false, upload_pu, "nzp_upload", "nzp_kvar", "nzp_serv", "nzp_counter");
                        Database.AddIndex("ix2_upload_pu_" + i.ToString("00"), false, upload_pu, "nzp_upload", "ns", "usl");
                    }
                }
            }

        }

        public override void Revert()
        {
            for (int i = 1; i <= 12; i++)
            {
                SchemaQualifiedObjectName upload_pu = new SchemaQualifiedObjectName() { Name = "upload_pu" + i.ToString("00"), Schema = CurrentSchema };
                if (Database.TableExists(upload_pu)) Database.RemoveTable(upload_pu);

                string idx_name = string.Format("ix_upload_pu_{0}", i.ToString("00"));
                string idx_name2 = string.Format("ix2_upload_pu_{0}", i.ToString("00"));

                if (Database.IndexExists(idx_name, upload_pu)) Database.RemoveIndex(idx_name, upload_pu);
                if (Database.IndexExists(idx_name2, upload_pu)) Database.RemoveIndex(idx_name2, upload_pu);

            }
        }
    }


    [Migration(20140520051, MigrateDataBase.CentralBank)]
    public class Migration_20140520051_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);
            // TODO: Upgrade CentralPref_Data

            SchemaQualifiedObjectName reestr_upload_pu = new SchemaQualifiedObjectName();
            reestr_upload_pu.Name = "reestr_upload_pu";
            reestr_upload_pu.Schema = CurrentSchema;

            if (!Database.TableExists(reestr_upload_pu))
            {
                Database.AddTable(reestr_upload_pu,
                    new Column("nzp_reestr", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("nzp_area", DbType.Int32, ColumnProperty.NotNull),
                    new Column("month", DbType.Int32),
                    new Column("year", DbType.Int32),
                    new Column("is_actual", DbType.Int32),
                    new Column("date_upload", DbType.DateTime),
                    new Column("date_when", DbType.DateTime)
                    );
                if (Database.TableExists(reestr_upload_pu))
                {
                    Database.AddIndex("ix_reestr_upload_pu", false, reestr_upload_pu, "nzp_reestr", "nzp_area");
                }
            }

            #region Режим "Взаимодействие с Банком"

            #region создание таблиц
            SchemaQualifiedObjectName tula_file_reestr = new SchemaQualifiedObjectName();
            tula_file_reestr.Name = "tula_file_reestr";
            tula_file_reestr.Schema = CurrentSchema;
            if (!Database.TableExists(tula_file_reestr))
            {
                Database.AddTable(tula_file_reestr,
                   new Column("nzp_reestr_d", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                   new Column("pkod", DbType.Decimal.WithSize(13, 0)),
                   new Column("nzp_kvar", DbType.Int32),
                   new Column("nzp_kvit_reestr", DbType.Int32),
                   new Column("sum_charge", DbType.Decimal.WithSize(14, 2)),
                   new Column("transaction_id", DbType.StringFixedLength.WithSize(26)),
                   new Column("cnt1", DbType.StringFixedLength.WithSize(100)),
                   new Column("val_cnt1", DbType.Decimal.WithSize(14, 2)),
                   new Column("cnt2", DbType.StringFixedLength.WithSize(100)),
                   new Column("val_cnt2", DbType.Decimal.WithSize(14, 2)),
                   new Column("cnt3", DbType.StringFixedLength.WithSize(100)),
                   new Column("val_cnt3", DbType.Decimal.WithSize(14, 2)),
                   new Column("cnt4", DbType.StringFixedLength.WithSize(100)),
                   new Column("val_cnt4", DbType.Decimal.WithSize(14, 2)),
                   new Column("cnt5", DbType.StringFixedLength.WithSize(100)),
                   new Column("val_cnt5", DbType.Decimal.WithSize(14, 2)),
                   new Column("cnt6", DbType.StringFixedLength.WithSize(100)),
                   new Column("val_cnt6", DbType.Decimal.WithSize(14, 2)),
                   new Column("date_plat_poruch", DbType.StringFixedLength.WithSize(20)),
                   new Column("nomer_plat_poruch", DbType.String)
                   );
                if (Database.TableExists(tula_file_reestr))
                {
                    Database.AddIndex("ix_tula_file_reestr_1", false, tula_file_reestr, "nzp_reestr_d");
                }
            }

            SchemaQualifiedObjectName tula_kvit_reestr = new SchemaQualifiedObjectName();
            tula_kvit_reestr.Name = "tula_kvit_reestr";
            tula_kvit_reestr.Schema = CurrentSchema;
            if (!Database.TableExists(tula_kvit_reestr))
            {
                Database.AddTable(tula_kvit_reestr,
                   new Column("nzp_kvit_reestr", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                   new Column("date_plat", DbType.Date),
                   new Column("file_name", DbType.StringFixedLength.WithSize(20)),
                   new Column("kod_dop", DbType.Int32),
                   new Column("count_rows", DbType.Int32),
                   new Column("sum_plat", DbType.Decimal.WithSize(14, 2)),
                   new Column("branch_id", DbType.StringFixedLength.WithSize(5)),
                   new Column("is_itog", DbType.Int32)
                   );
                if (Database.TableExists(tula_kvit_reestr))
                {
                    Database.AddIndex("ix_tula_kvit_reestr_1", false, tula_file_reestr, "nzp_kvit_reestr");
                }
            }

            SchemaQualifiedObjectName tula_reestr_downloads = new SchemaQualifiedObjectName();
            tula_reestr_downloads.Name = "tula_reestr_downloads";
            tula_reestr_downloads.Schema = CurrentSchema;
            if (!Database.TableExists(tula_reestr_downloads))
            {
                Database.AddTable(tula_reestr_downloads,
                   new Column("nzp_download", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                   new Column("file_name", DbType.StringFixedLength.WithSize(20)),
                   new Column("nzp_type", DbType.Int32),
                   new Column("count_rows", DbType.Int32),
                   new Column("date_download", DbType.DateTime),
                   new Column("user_downloaded", DbType.Int32),
                   new Column("branch_id", DbType.StringFixedLength.WithSize(5)),
                   new Column("day", DbType.Int32),
                   new Column("month", DbType.Int32),
                   new Column("nzp_bank", DbType.Int32)
                   );
                if (Database.TableExists(tula_reestr_downloads))
                {
                    Database.AddIndex("ix_tula_reestr_downloads_1", false, tula_reestr_downloads, "nzp_download");
                }
            }

            SchemaQualifiedObjectName tula_reestr_sprav = new SchemaQualifiedObjectName();
            tula_reestr_sprav.Name = "tula_reestr_sprav";
            tula_reestr_sprav.Schema = CurrentSchema;
            if (!Database.TableExists(tula_reestr_sprav))
            {
                Database.AddTable(tula_reestr_sprav,
                   new Column("nzp_type", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                   new Column("name_type", DbType.StringFixedLength.WithSize(50))
                   );
                if (Database.TableExists(tula_reestr_sprav))
                {
                    Database.AddIndex("ix_tula_reestr_sprav_1", false, tula_reestr_sprav, "nzp_type");
                }
            }


            SchemaQualifiedObjectName tula_reestr_unloads = new SchemaQualifiedObjectName();
            tula_reestr_unloads.Name = "tula_reestr_unloads";
            tula_reestr_unloads.Schema = CurrentSchema;
            if (!Database.TableExists(tula_reestr_unloads))
            {
                Database.AddTable(tula_reestr_unloads,
                   new Column("nzp_reestr", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                   new Column("name_file", DbType.StringFixedLength.WithSize(20)),
                   new Column("date_unload", DbType.Date),
                   new Column("unloading_date", DbType.StringFixedLength.WithSize(255)),
                   new Column("user_unloaded", DbType.Int32),
                   new Column("nzp_exc", DbType.Int32),
                   new Column("branch_id", DbType.StringFixedLength.WithSize(5)),
                   new Column("is_actual", DbType.Int32)
                   );
                if (Database.TableExists(tula_reestr_unloads))
                {
                    Database.AddIndex("ix_tula_reestr_unloads_1", false, tula_reestr_unloads, "nzp_reestr");
                }
            }

            SchemaQualifiedObjectName tula_s_bank = new SchemaQualifiedObjectName();
            tula_s_bank.Name = "tula_s_bank";
            tula_s_bank.Schema = CurrentSchema;
            if (!Database.TableExists(tula_s_bank))
            {
                Database.AddTable(tula_s_bank,
                   new Column("id", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                   new Column("nzp_bank", DbType.Int32),
                   new Column("branch_id", DbType.StringFixedLength.WithSize(5)),
                   new Column("branch_name", DbType.StringFixedLength.WithSize(50)),
                   new Column("is_actual", DbType.Int32, ColumnProperty.None, 1),
                   new Column("dat_add", DbType.DateTime),
                   new Column("dat_del", DbType.DateTime)
                   );
                if (Database.TableExists(tula_s_bank))
                {
                    Database.AddIndex("ix_tula_s_bank_1", false, tula_s_bank, "id");
                }
            }


            #endregion

            #region добавление и правка колонок

            if (Database.TableExists(tula_s_bank))
            {
                Database.ChangeColumn(tula_s_bank, "branch_id", DbType.StringFixedLength.WithSize(5), false);

                if (!Database.ColumnExists(tula_s_bank, "is_actual"))
                {
                    Database.AddColumn(tula_s_bank, new Column("is_actual", DbType.Int32, ColumnProperty.None, 1));
                }
                if (!Database.ColumnExists(tula_s_bank, "dat_add"))
                {
                    Database.AddColumn(tula_s_bank, new Column("dat_add", DbType.DateTime));
                }
                if (!Database.ColumnExists(tula_s_bank, "dat_del"))
                {
                    Database.AddColumn(tula_s_bank, new Column("dat_del", DbType.DateTime));
                }
            }

            if (Database.TableExists(tula_kvit_reestr))
            {
                Database.ChangeColumn(tula_kvit_reestr, "branch_id", DbType.StringFixedLength.WithSize(5), false);
            }

            if (Database.TableExists(tula_reestr_downloads))
            {
                Database.ChangeColumn(tula_reestr_downloads, "branch_id", DbType.StringFixedLength.WithSize(5), false);

                if (!Database.ColumnExists(tula_reestr_downloads, "nzp_bank"))
                {
                    Database.AddColumn(tula_reestr_downloads, new Column("nzp_bank", DbType.Int32));
                    Database.ExecuteNonQuery(" UPDATE tula_reestr_downloads SET nzp_bank=((SELECT nzp_bank FROM tula_s_bank s WHERE tula_reestr_downloads.branch_id=s.branch_id));");
                }
                if (!Database.ColumnExists(tula_reestr_downloads, "proc"))
                {
                    Database.AddColumn(tula_reestr_downloads, new Column("proc", DbType.Decimal.WithSize(8, 4)));
                    Database.Update(tula_reestr_downloads, new string[] { "proc" }, new string[] { "100" });
                }
                if (!Database.ColumnExists(tula_reestr_downloads, "status"))
                {
                    Database.AddColumn(tula_reestr_downloads, new Column("status", DbType.Int32));
                    Database.Update(tula_reestr_downloads, new string[] { "status" }, new string[] { "1" });
                }
                if (!Database.ColumnExists(tula_reestr_downloads, "nzp_exc"))
                {
                    Database.AddColumn(tula_reestr_downloads, new Column("nzp_exc", DbType.Int32));
                }
            }

            #endregion
            #endregion
        }

        public override void Revert()
        {
            SchemaQualifiedObjectName reestr_upload_pu = new SchemaQualifiedObjectName() { Name = "reestr_upload_pu", Schema = CurrentSchema };
            if (Database.TableExists(reestr_upload_pu)) Database.RemoveTable(reestr_upload_pu);

            if (Database.IndexExists("ix_reestr_upload_pu", reestr_upload_pu)) Database.RemoveIndex("ix_reestr_upload_pu", reestr_upload_pu);

            #region Режим "Взаимодействие с Банком"
            SchemaQualifiedObjectName tula_file_reestr = new SchemaQualifiedObjectName();
            SchemaQualifiedObjectName tula_kvit_reestr = new SchemaQualifiedObjectName();
            SchemaQualifiedObjectName tula_reestr_downloads = new SchemaQualifiedObjectName();
            SchemaQualifiedObjectName tula_reestr_sprav = new SchemaQualifiedObjectName();
            SchemaQualifiedObjectName tula_reestr_unloads = new SchemaQualifiedObjectName();
            SchemaQualifiedObjectName tula_s_bank = new SchemaQualifiedObjectName();

            if (Database.TableExists(tula_file_reestr)) Database.RemoveTable(tula_file_reestr);
            if (Database.TableExists(tula_kvit_reestr)) Database.RemoveTable(tula_kvit_reestr);
            if (Database.TableExists(tula_reestr_downloads)) Database.RemoveTable(tula_reestr_downloads);
            if (Database.TableExists(tula_reestr_sprav)) Database.RemoveTable(tula_reestr_sprav);
            if (Database.TableExists(tula_reestr_unloads)) Database.RemoveTable(tula_reestr_unloads);
            if (Database.TableExists(tula_s_bank)) Database.RemoveTable(tula_s_bank);

            if (Database.IndexExists("ix_tula_file_reestr_1", tula_file_reestr)) Database.RemoveIndex("ix_tula_file_reestr_1", tula_file_reestr);
            if (Database.IndexExists("ix_tula_kvit_reestr_1", tula_kvit_reestr)) Database.RemoveIndex("ix_tula_kvit_reestr_1", tula_kvit_reestr);
            if (Database.IndexExists("ix_tula_reestr_downloads_1", tula_reestr_downloads)) Database.RemoveIndex("ix_tula_reestr_downloads_1", tula_reestr_downloads);
            if (Database.IndexExists("ix_tula_reestr_sprav_1", tula_reestr_sprav)) Database.RemoveIndex("ix_tula_reestr_sprav_1", tula_reestr_sprav);
            if (Database.IndexExists("ix_tula_reestr_unloads_1", tula_reestr_unloads)) Database.RemoveIndex("ix_tula_reestr_unloads_1", tula_reestr_unloads);
            if (Database.IndexExists("ix_tula_s_bank_1", tula_s_bank)) Database.RemoveIndex("ix_tula_s_bank_1", tula_s_bank);


            #endregion

        }
    }
}
