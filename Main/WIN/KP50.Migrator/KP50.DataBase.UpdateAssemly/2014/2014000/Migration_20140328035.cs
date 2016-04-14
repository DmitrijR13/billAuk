using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(20140328035, MigrateDataBase.CentralBank)]
    public class Migration_20140328035_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            //if (Database.ProviderName == "Informix") Database.ExecuteNonQuery(string.Format("CREATE DATABASE IF NOT EXISTS {0}_upload WITH LOG", CurrentPrefix));
            if (Database.ProviderName == "PostgreSQL") Database.ExecuteNonQuery(string.Format("CREATE SCHEMA IF NOT EXISTS {0}_upload", CurrentPrefix));

            SetSchema(Bank.Data);
            SchemaQualifiedObjectName pack_ls_block = new SchemaQualifiedObjectName() { Name = "pack_ls_block", Schema = CurrentSchema };
            if (!Database.TableExists(pack_ls_block))
            {
                if (Database.ProviderName == "Informix") Database.ExecuteNonQuery("CREATE TABLE pack_ls_block(nzp_pack_ls INTEGER, year_ INTEGER, nzp_user INTEGER, dat_when DATETIME YEAR to FRACTION(3))");
                if (Database.ProviderName == "PostgreSQL") Database.ExecuteNonQuery("CREATE TABLE pack_ls_block(nzp_pack_ls INTEGER, year_ INTEGER, nzp_user INTEGER, dat_when TIMESTAMP)");
            }

            SchemaQualifiedObjectName fn_dogovor_bank = new SchemaQualifiedObjectName() { Name = "fn_dogovor_bank", Schema = CurrentSchema };
            if (!Database.TableExists(fn_dogovor_bank))
            {
                Database.AddTable(fn_dogovor_bank,
                    new Column("nzp_fd_bank", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("nzp_fd", DbType.Int32),
                    new Column("nzp_bank", DbType.Int32),
                    new Column("dat_when", DbType.Date),
                    new Column("nzp_user", DbType.Int32));
                Database.AddIndex("ix_fdbank_1", true, fn_dogovor_bank, "nzp_fd_bank");
                Database.AddIndex("ix_fdbank_2", false, fn_dogovor_bank, "nzp_fd");
                Database.AddIndex("ix_fdbank_3", false, fn_dogovor_bank, "nzp_bank");
            }

            SchemaQualifiedObjectName tula_ex_sz = new SchemaQualifiedObjectName() { Name = "tula_ex_sz", Schema = CurrentSchema };
            if (Database.TableExists(tula_ex_sz) && !Database.ColumnExists(tula_ex_sz, "proc"))
                Database.AddColumn(tula_ex_sz, new Column("proc", DbType.Decimal.WithSize(6, 4)));

            SchemaQualifiedObjectName prm_8 = new SchemaQualifiedObjectName() { Name = "prm_8", Schema = CurrentSchema };
            if (Database.TableExists(prm_8) && !Database.ColumnExists(prm_8, "val_prm"))
                Database.AddColumn(prm_8, new Column("val_prm", DbType.String.WithSize(100)));

            SchemaQualifiedObjectName dat_files_imported = new SchemaQualifiedObjectName() { Name = "files_imported", Schema = CurrentSchema };
            if (Database.TableExists(dat_files_imported) && !Database.ColumnExists(dat_files_imported, "pref"))
                Database.AddColumn(dat_files_imported, new Column("pref", DbType.String.WithSize(20)));

            SchemaQualifiedObjectName sys_dictionary_values = new SchemaQualifiedObjectName() { Name = "sys_dictionary_values", Schema = CurrentSchema };
            if (Database.TableExists(sys_dictionary_values) && !Database.ColumnExists(sys_dictionary_values, "action"))
                Database.AddColumn(sys_dictionary_values, new Column("action", DbType.Int32));

            SchemaQualifiedObjectName actions_types = new SchemaQualifiedObjectName() { Name = "actions_types", Schema = CurrentSchema };
            if (!Database.TableExists(actions_types))
            {
                Database.AddTable(actions_types,
                    new Column("id", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("action_name", DbType.String.WithSize(25)));
                Database.Insert(actions_types, new string[] { "id", "action_name" }, new string[] { "1", "Системное" });
                Database.Insert(actions_types, new string[] { "id", "action_name" }, new string[] { "2", "Просмотр" });
                Database.Insert(actions_types, new string[] { "id", "action_name" }, new string[] { "3", "Удаление" });
                Database.Insert(actions_types, new string[] { "id", "action_name" }, new string[] { "4", "Добавление" });
                Database.Insert(actions_types, new string[] { "id", "action_name" }, new string[] { "5", "Изменение" });
                Database.Insert(actions_types, new string[] { "id", "action_name" }, new string[] { "6", "Открытие" });
                Database.Insert(actions_types, new string[] { "id", "action_name" }, new string[] { "7", "Закрытие" });
                Database.Insert(actions_types, new string[] { "id", "action_name" }, new string[] { "8", "Расчет" });
                Database.Insert(actions_types, new string[] { "id", "action_name" }, new string[] { "9", "Формирование" });
                Database.Insert(actions_types, new string[] { "id", "action_name" }, new string[] { "10", "Отмена" });
                Database.Insert(actions_types, new string[] { "id", "action_name" }, new string[] { "11", "Подтверждение" });
                Database.Insert(actions_types, new string[] { "id", "action_name" }, new string[] { "12", "Печать" });
                Database.Update(sys_dictionary_values, new string[] { "action" }, new string[] { "1" }, "nzp_dict IN (6479, 6480)");
                Database.Update(sys_dictionary_values, new string[] { "action" }, new string[] { "2" }, "nzp_dict IN (6490, 6605, 6606, 6607)");
                Database.Update(sys_dictionary_values, new string[] { "action" }, new string[] { "3" }, "nzp_dict IN (6482, 6485, 6487, 6490, 6493, 6500, 6598, 6601, 6603, 7431)");
                Database.Update(sys_dictionary_values, new string[] { "action" }, new string[] { "4" }, "nzp_dict IN (6481, 6484, 6486, 6489, 6492, 6497, 6499, 6597, 6600, 6602)");
                Database.Update(sys_dictionary_values, new string[] { "action" }, new string[] { "5" }, "nzp_dict IN (6488, 6491, 6494, 6495, 6496, 6498, 6599, 6604, 6610, 6611, 7428, 8214, 8215, 8216)");
                Database.Update(sys_dictionary_values, new string[] { "action" }, new string[] { "6" }, "nzp_dict IN (6608)");
                Database.Update(sys_dictionary_values, new string[] { "action" }, new string[] { "7" }, "nzp_dict IN (6483, 6609, 6613)");
                Database.Update(sys_dictionary_values, new string[] { "action" }, new string[] { "8" }, "nzp_dict IN (6594, 6595, 7427)");
                Database.Update(sys_dictionary_values, new string[] { "action" }, new string[] { "9" }, "nzp_dict IN (7429, 7430)");
                Database.Update(sys_dictionary_values, new string[] { "action" }, new string[] { "10" }, "nzp_dict IN (6612)");
                Database.Update(sys_dictionary_values, new string[] { "action" }, new string[] { "11" }, "nzp_dict IN (6637)");
                Database.Update(sys_dictionary_values, new string[] { "action" }, new string[] { "12" }, "nzp_dict IN (6596, 7800)");
            }

            SchemaQualifiedObjectName counters = new SchemaQualifiedObjectName() { Name = "counters", Schema = CurrentSchema };
            if (Database.TableExists(counters) && !Database.ColumnExists(counters, "num_cnt"))
                Database.AddColumn(counters, new Column("num_cnt", DbType.String.WithSize(30)));

            SchemaQualifiedObjectName counters_spis = new SchemaQualifiedObjectName() { Name = "counters_spis", Schema = CurrentSchema };
            if (Database.TableExists(counters_spis) && !Database.ColumnExists(counters_spis, "num_cnt"))
                Database.AddColumn(counters_spis, new Column("num_cnt", DbType.String.WithSize(30)));

            SchemaQualifiedObjectName supplier_codes = new SchemaQualifiedObjectName() { Name = "supplier_codes", Schema = CurrentSchema };
            if (!Database.TableExists(supplier_codes))
                Database.AddTable(supplier_codes,
                    new Column("nzp_sc", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("nzp_kvar", DbType.Int32),
                    new Column("nzp_supp", DbType.Int32),
                    new Column("kod_geu", DbType.Int32),
                    new Column("pkod10", DbType.Int32, ColumnProperty.None, 0),
                    new Column("pkod_supp", DbType.Decimal.WithSize(13), ColumnProperty.NotNull, 0.0000000000000000));

            SetSchema(Bank.Upload);
            SchemaQualifiedObjectName file_area = new SchemaQualifiedObjectName() { Name = "file_area", Schema = CurrentSchema };
            if (!Database.TableExists(file_area))
            {
                Database.AddTable(file_area,
                    new Column("id", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("nzp_file", DbType.Int32, ColumnProperty.NotNull),
                    new Column("area", DbType.String.WithSize(40)),
                    new Column("jur_address", DbType.String.WithSize(100)),
                    new Column("fact_address", DbType.String.WithSize(100)),
                    new Column("inn", DbType.String.WithSize(20)),
                    new Column("kpp", DbType.String.WithSize(20)),
                    new Column("rs", DbType.String.WithSize(20)),
                    new Column("bank", DbType.String.WithSize(100)),
                    new Column("bik", DbType.String.WithSize(20)),
                    new Column("ks", DbType.String.WithSize(20)),
                    new Column("nzp_area", DbType.Int32));
                Database.AddIndex("ix1_file_area", false, file_area, "id");
                Database.AddIndex("i1_file_area", false, file_area, "nzp_file", "nzp_area");
                Database.AddIndex("ix2_file_area", false, file_area, "nzp_file", "id", "nzp_area");
            }

            SchemaQualifiedObjectName file_blag = new SchemaQualifiedObjectName() { Name = "file_blag", Schema = CurrentSchema };
            if (!Database.TableExists(file_blag))
                Database.AddTable(file_blag,
                    new Column("id", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("id_prm", DbType.Int32),
                    new Column("name", DbType.String.WithSize(100)),
                    new Column("nzp_file", DbType.Int32),
                    new Column("nzp_prm", DbType.Int32));

            SchemaQualifiedObjectName file_dom = new SchemaQualifiedObjectName() { Name = "file_dom", Schema = CurrentSchema };
            if (!Database.TableExists(file_dom))
            {
                Database.AddTable(file_dom,
                    new Column("id", DbType.Decimal.WithSize(18, 0)),
                    new Column("nzp_file", DbType.Int32, ColumnProperty.NotNull),
                    new Column("ukds", DbType.Int32),
                    new Column("town", DbType.String.WithSize(30)),
                    new Column("rajon", DbType.String.WithSize(30)),
                    new Column("ulica", DbType.String.WithSize(40)),
                    new Column("ndom", DbType.String.WithSize(10)),
                    new Column("nkor", DbType.String.WithSize(3)),
                    new Column("area_id", DbType.Decimal.WithSize(18, 0)),
                    new Column("cat_blago", DbType.String.WithSize(30)),
                    new Column("etazh", DbType.Int32, ColumnProperty.NotNull),
                    new Column("build_year", DbType.Date),
                    new Column("total_square", DbType.Decimal.WithSize(14, 2), ColumnProperty.NotNull),
                    new Column("mop_square", DbType.Decimal.WithSize(14, 2)),
                    new Column("useful_square", DbType.Decimal.WithSize(14, 2)),
                    new Column("mo_id", DbType.Decimal.WithSize(13, 0)),
                    new Column("params", DbType.String.WithSize(250)),
                    new Column("ls_row_number", DbType.Int32, ColumnProperty.NotNull),
                    new Column("odpu_row_number", DbType.Int32, ColumnProperty.NotNull),
                    new Column("nzp_ul", DbType.Int32),
                    new Column("nzp_dom", DbType.Int32),
                    new Column("comment", DbType.String.WithSize(250)),
                    new Column("local_id", DbType.String.WithSize(20)),
                    new Column("nzp_raj", DbType.Int32),
                    new Column("nzp_town", DbType.Int32),
                    new Column("nzp_geu", DbType.Int32),
                    new Column("uch", DbType.Int32),
                    new Column("kod_kladr", DbType.String.WithSize(30)));
                Database.AddIndex("ix1_file_dom", false, file_dom, "id");
                Database.AddIndex("ix21_file_dom", false, file_dom, "nzp_file", "id");
                Database.AddIndex("i1_file_dom", false, file_dom, "nzp_file", "nzp_dom");
                Database.AddIndex("isss1a", false, file_dom, "nzp_file", "area_id", "id");
            }

            SchemaQualifiedObjectName file_gaz = new SchemaQualifiedObjectName() { Name = "file_gaz", Schema = CurrentSchema };
            if (!Database.TableExists(file_gaz))
                Database.AddTable(file_gaz,
                    new Column("id", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("id_prm", DbType.Int32),
                    new Column("name", DbType.String.WithSize(100)),
                    new Column("nzp_file", DbType.Int32),
                    new Column("nzp_prm", DbType.Int32));

            SchemaQualifiedObjectName file_gilec = new SchemaQualifiedObjectName() { Name = "file_gilec", Schema = CurrentSchema };
            if (!Database.TableExists(file_gilec))
            {
                Database.AddTable(file_gilec,
                    new Column("id", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("nzp_file", DbType.Int32),
                    new Column("num_ls", DbType.Int32),
                    new Column("nzp_gil", DbType.Int32),
                    new Column("nzp_kart", DbType.Int32),
                    new Column("nzp_tkrt", DbType.Int32),
                    new Column("fam", DbType.String.WithSize(40)),
                    new Column("ima", DbType.String.WithSize(40)),
                    new Column("otch", DbType.String.WithSize(40)),
                    new Column("dat_rog", DbType.Date),
                    new Column("fam_c", DbType.String.WithSize(40)),
                    new Column("ima_c", DbType.String.WithSize(40)),
                    new Column("otch_c", DbType.String.WithSize(40)),
                    new Column("dat_rog_c", DbType.Date),
                    new Column("gender", DbType.String.WithSize(1)),
                    new Column("nzp_dok", DbType.Int32),
                    new Column("serij", DbType.String.WithSize(10)),
                    new Column("nomer", DbType.String.WithSize(7)),
                    new Column("vid_dat", DbType.Date),
                    new Column("vid_mes", DbType.String.WithSize(70)),
                    new Column("kod_podrazd", DbType.String.WithSize(7)),
                    new Column("strana_mr", DbType.String.WithSize(30)),
                    new Column("region_mr", DbType.String.WithSize(30)),
                    new Column("okrug_mr", DbType.String.WithSize(30)),
                    new Column("gorod_mr", DbType.String.WithSize(30)),
                    new Column("npunkt_mr", DbType.String.WithSize(30)),
                    new Column("rem_mr", DbType.String.WithSize(180)),
                    new Column("strana_op", DbType.String.WithSize(30)),
                    new Column("region_op", DbType.String.WithSize(30)),
                    new Column("okrug_op", DbType.String.WithSize(30)),
                    new Column("gorod_op", DbType.String.WithSize(30)),
                    new Column("npunkt_op", DbType.String.WithSize(30)),
                    new Column("rem_op", DbType.String.WithSize(180)),
                    new Column("strana_ku", DbType.String.WithSize(30)),
                    new Column("region_ku", DbType.String.WithSize(30)),
                    new Column("okrug_ku", DbType.String.WithSize(30)),
                    new Column("gorod_ku", DbType.String.WithSize(30)),
                    new Column("npunkt_ku", DbType.String.WithSize(30)),
                    new Column("rem_ku", DbType.String.WithSize(180)),
                    new Column("rem_p", DbType.String.WithSize(40)),
                    new Column("tprp", DbType.String.WithSize(1)),
                    new Column("dat_prop", DbType.Date),
                    new Column("dat_oprp", DbType.Date),
                    new Column("dat_pvu", DbType.Date),
                    new Column("who_pvu", DbType.String.WithSize(40)),
                    new Column("dat_svu", DbType.Date),
                    new Column("namereg", DbType.String.WithSize(80)),
                    new Column("kod_namereg", DbType.String.WithSize(7)),
                    new Column("rod", DbType.String.WithSize(30)),
                    new Column("nzp_celp", DbType.Int32),
                    new Column("nzp_celu", DbType.Int32),
                    new Column("dat_sost", DbType.Date),
                    new Column("dat_ofor", DbType.Date),
                    new Column("comment", DbType.String.WithSize(40)));
                Database.AddIndex("ix1_file_gilec", false, file_gilec, "num_ls", "id");
                Database.AddIndex("ix2_file_gilec", false, file_gilec, "nzp_file", "id", "comment");
            }

            SchemaQualifiedObjectName file_head = new SchemaQualifiedObjectName() { Name = "file_head", Schema = CurrentSchema };
            if (!Database.TableExists(file_head))
                Database.AddTable(file_head,
                    new Column("id", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("nzp_file", DbType.Int32, ColumnProperty.NotNull),
                    new Column("org_name", DbType.String.WithSize(40), ColumnProperty.NotNull),
                    new Column("branch_name", DbType.String.WithSize(40), ColumnProperty.NotNull),
                    new Column("inn", DbType.String.WithSize(20), ColumnProperty.NotNull),
                    new Column("kpp", DbType.String.WithSize(20), ColumnProperty.NotNull),
                    new Column("file_no", DbType.Int32, ColumnProperty.NotNull),
                    new Column("file_date", DbType.Date, ColumnProperty.NotNull),
                    new Column("sender_phone", DbType.String.WithSize(20), ColumnProperty.NotNull),
                    new Column("sender_fio", DbType.String.WithSize(80), ColumnProperty.NotNull),
                    new Column("calc_date", DbType.Date, ColumnProperty.NotNull),
                    new Column("row_number", DbType.Int32, ColumnProperty.NotNull));

            SchemaQualifiedObjectName file_ipu = new SchemaQualifiedObjectName() { Name = "file_ipu", Schema = CurrentSchema };
            if (!Database.TableExists(file_ipu))
            {
                Database.AddTable(file_ipu,
                    new Column("id", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("nzp_file", DbType.Int32, ColumnProperty.NotNull),
                    new Column("ls_id", DbType.String.WithSize(20)),
                    new Column("nzp_serv", DbType.Int32),
                    new Column("rashod_type", DbType.Int32),
                    new Column("serv_type", DbType.Int32),
                    new Column("counter_type", DbType.String.WithSize(25)),
                    new Column("cnt_stage", DbType.Int32),
                    new Column("mmnog", DbType.Int32),
                    new Column("num_cnt", DbType.String.WithSize(20)),
                    new Column("dat_uchet", DbType.Date),
                    new Column("val_cnt", DbType.Single),
                    new Column("nzp_measure", DbType.Int32),
                    new Column("dat_prov", DbType.Date),
                    new Column("dat_provnext", DbType.Date),
                    new Column("nzp_kvar", DbType.Int32),
                    new Column("nzp_counter", DbType.Int32),
                    new Column("local_id", DbType.String.WithSize(20)),
                    new Column("kod_serv", DbType.String.WithSize(20)),
                    new Column("doppar", DbType.String.WithSize(25)));
                Database.AddIndex("ix1_file_ipu", false, file_ipu, "local_id", "id");
            }

            SchemaQualifiedObjectName file_ipu_p = new SchemaQualifiedObjectName() { Name = "file_ipu_p", Schema = CurrentSchema };
            if (!Database.TableExists(file_ipu_p))
            {
                Database.AddTable(file_ipu_p,
                    new Column("id", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("nzp_file", DbType.Int32),
                    new Column("id_ipu", DbType.String.WithSize(20)),
                    new Column("rashod_type", DbType.Int32),
                    new Column("dat_uchet", DbType.Date),
                    new Column("val_cnt", DbType.Single),
                    new Column("kod_serv", DbType.Int32));
                Database.AddIndex("ix1_file_ipu_p", false, file_ipu_p, "id_ipu", "id");
            }

            SchemaQualifiedObjectName file_kvar = new SchemaQualifiedObjectName() { Name = "file_kvar", Schema = CurrentSchema };
            if (!Database.TableExists(file_kvar))
            {
                Database.AddTable(file_kvar,
                    new Column("id", DbType.String.WithSize(20), ColumnProperty.NotNull),
                    new Column("nzp_file", DbType.Int32, ColumnProperty.NotNull),
                    new Column("ukas", DbType.Int32),
                    new Column("dom_id", DbType.Decimal.WithSize(18, 0), ColumnProperty.NotNull),
                    new Column("ls_type", DbType.Int32, ColumnProperty.NotNull),
                    new Column("fam", DbType.String.WithSize(40)),
                    new Column("ima", DbType.String.WithSize(40)),
                    new Column("otch", DbType.String.WithSize(40)),
                    new Column("birth_date", DbType.Date),
                    new Column("nkvar", DbType.String.WithSize(10)),
                    new Column("nkvar_n", DbType.String.WithSize(3)),
                    new Column("open_date", DbType.Date),
                    new Column("opening_osnov", DbType.String.WithSize(100)),
                    new Column("close_date", DbType.Date),
                    new Column("closing_osnov", DbType.String.WithSize(100)),
                    new Column("kol_gil", DbType.Int32, ColumnProperty.NotNull),
                    new Column("kol_vrem_prib", DbType.Int32, ColumnProperty.NotNull),
                    new Column("kol_vrem_ub", DbType.Int32, ColumnProperty.NotNull),
                    new Column("room_number", DbType.Int32, ColumnProperty.NotNull),
                    new Column("total_square", DbType.Decimal.WithSize(14,2), ColumnProperty.NotNull),
                    new Column("living_square", DbType.Decimal.WithSize(14,2)),
                    new Column("otapl_square", DbType.Decimal.WithSize(14,2)),
                    new Column("naim_square", DbType.Decimal.WithSize(14,2)),
                    new Column("is_communal", DbType.Int32, ColumnProperty.NotNull),
                    new Column("is_el_plita", DbType.Int32),
                    new Column("is_gas_plita", DbType.Int32),
                    new Column("is_gas_colonka", DbType.Int32),
                    new Column("is_fire_plita", DbType.Int32),
                    new Column("gas_type", DbType.Int32),
                    new Column("water_type", DbType.Int32),
                    new Column("hotwater_type", DbType.Int32),
                    new Column("canalization_type", DbType.Int32),
                    new Column("is_open_otopl", DbType.Int32),
                    new Column("params", DbType.String.WithSize(250)),
                    new Column("service_row_number", DbType.Int32, ColumnProperty.NotNull),
                    new Column("reval_params_row_number", DbType.Int32, ColumnProperty.NotNull),
                    new Column("ipu_row_number", DbType.Int32, ColumnProperty.NotNull),
                    new Column("nzp_dom", DbType.Int32),
                    new Column("nzp_kvar", DbType.Int32),
                    new Column("comment", DbType.String.WithSize(250)),
                    new Column("nzp_status", DbType.Int32),
                    new Column("id_urlic", DbType.String.WithSize(20)));
                Database.AddIndex("ix1_file_kvar", false, file_kvar, "id");
                Database.AddIndex("ix21_file_kvar", false, file_kvar, "nzp_file", "id");
            }

            SchemaQualifiedObjectName file_kvarp = new SchemaQualifiedObjectName() { Name = "file_kvarp", Schema = CurrentSchema };
            if (!Database.TableExists(file_kvarp))
                Database.AddTable(file_kvarp,
                    new Column("id", DbType.String.WithSize(20), ColumnProperty.NotNull),
                    new Column("reval_month", DbType.Date),
                    new Column("nzp_file", DbType.Int32, ColumnProperty.NotNull),
                    new Column("fam", DbType.String.WithSize(40)),
                    new Column("ima", DbType.String.WithSize(40)),
                    new Column("otch", DbType.String.WithSize(40)),
                    new Column("birth_date", DbType.Date),
                    new Column("nkvar", DbType.String.WithSize(10)),
                    new Column("nkvar_n", DbType.String.WithSize(3)),
                    new Column("open_date", DbType.Date),
                    new Column("opening_osnov", DbType.String.WithSize(100)),
                    new Column("close_date", DbType.Date),
                    new Column("closing_osnov", DbType.String.WithSize(100)),
                    new Column("kol_gil", DbType.Int32, ColumnProperty.NotNull),
                    new Column("kol_vrem_prib", DbType.Int32, ColumnProperty.NotNull),
                    new Column("kol_vrem_ub", DbType.Int32, ColumnProperty.NotNull),
                    new Column("room_number", DbType.Int32, ColumnProperty.NotNull),
                    new Column("total_square", DbType.Decimal.WithSize(14, 2), ColumnProperty.NotNull),
                    new Column("living_square", DbType.Decimal.WithSize(14, 2)),
                    new Column("otapl_square", DbType.Decimal.WithSize(14, 2)),
                    new Column("naim_square", DbType.Decimal.WithSize(14, 2)),
                    new Column("is_communal", DbType.Int32, ColumnProperty.NotNull),
                    new Column("is_el_plita", DbType.Int32),
                    new Column("is_gas_plita", DbType.Int32),
                    new Column("is_gas_colonka", DbType.Int32),
                    new Column("is_fire_plita", DbType.Int32),
                    new Column("gas_type", DbType.Int32),
                    new Column("water_type", DbType.Int32),
                    new Column("hotwater_type", DbType.Int32),
                    new Column("canalization_type", DbType.Int32),
                    new Column("is_open_otopl", DbType.Int32),
                    new Column("params", DbType.String.WithSize(250)),
                    new Column("nzp_dom", DbType.Int32),
                    new Column("nzp_kvar", DbType.Int32),
                    new Column("comment", DbType.String.WithSize(250)),
                    new Column("nzp_status", DbType.Int32),
                    new Column("local_id", DbType.String.WithSize(20)));

            SchemaQualifiedObjectName file_measures = new SchemaQualifiedObjectName() { Name = "file_measures", Schema = CurrentSchema };
            if (!Database.TableExists(file_measures))
            {
                Database.AddTable(file_measures,
                    new Column("id", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("id_measure", DbType.Int32),
                    new Column("measure", DbType.String.WithSize(100)),
                    new Column("nzp_file", DbType.Int32),
                    new Column("nzp_measure", DbType.Int32));
                Database.AddIndex("ix1_file_measures", false, file_measures, "id_measure", "id");
            }

            SchemaQualifiedObjectName file_mo = new SchemaQualifiedObjectName() { Name = "file_mo", Schema = CurrentSchema };
            if (!Database.TableExists(file_mo))
            {
                Database.AddTable(file_mo,
                    new Column("id", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("id_mo", DbType.Int32),
                    new Column("vill", DbType.String.WithSize(50)),
                    new Column("nzp_vill", DbType.Decimal.WithSize(13, 0)),
                    new Column("nzp_raj", DbType.Int32),
                    new Column("nzp_file", DbType.Int32),
                    new Column("raj", DbType.String.WithSize(60)),
                    new Column("mo_name", DbType.String.WithSize(60)));
                Database.AddIndex("ix1_file_mo", false, file_mo, "id_mo", "id");
            }

            SchemaQualifiedObjectName file_nedopost = new SchemaQualifiedObjectName() { Name = "file_nedopost", Schema = CurrentSchema };
            if (!Database.TableExists(file_nedopost))
            {
                Database.AddTable(file_nedopost,
                    new Column("id", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("nzp_file", DbType.Int32),
                    new Column("ls_id", DbType.String.WithSize(20)),
                    new Column("id_serv", DbType.String.WithSize(20)),
                    new Column("type_ned", DbType.Decimal.WithSize(10, 0)),
                    new Column("temper", DbType.Int32),
                    new Column("dat_nedstart", DbType.Date),
                    new Column("dat_nedstop", DbType.Date),
                    new Column("sum_ned", DbType.Decimal.WithSize(10, 2)));
                Database.AddIndex("ix1_file_nedopost", false, file_nedopost, "type_ned", "id");
            }

            SchemaQualifiedObjectName file_odpu = new SchemaQualifiedObjectName() { Name = "file_odpu", Schema = CurrentSchema };
            if (!Database.TableExists(file_odpu))
            {
                Database.AddTable(file_odpu,
                    new Column("id", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("nzp_file", DbType.Int32, ColumnProperty.NotNull),
                    new Column("dom_id", DbType.Decimal.WithSize(18, 0)),
                    new Column("nzp_serv", DbType.Int32),
                    new Column("rashod_type", DbType.Int32),
                    new Column("serv_type", DbType.Int32),
                    new Column("counter_type", DbType.String.WithSize(25)),
                    new Column("cnt_stage", DbType.Int32),
                    new Column("mmnog", DbType.Int32),
                    new Column("num_cnt", DbType.String.WithSize(20)),
                    new Column("dat_uchet", DbType.Date),
                    new Column("val_cnt", DbType.Single),
                    new Column("nzp_measure", DbType.Int32),
                    new Column("dat_prov", DbType.Date),
                    new Column("dat_provnext", DbType.Date),
                    new Column("nzp_dom", DbType.Int32),
                    new Column("nzp_counter", DbType.Int32),
                    new Column("local_id", DbType.String.WithSize(20)),
                    new Column("doppar", DbType.String.WithSize(20)));
                Database.AddIndex("ix1_file_odpu", false, file_odpu, "local_id", "id");
            }

            SchemaQualifiedObjectName file_odpu_p = new SchemaQualifiedObjectName() { Name = "file_odpu_p", Schema = CurrentSchema };
            if (!Database.TableExists(file_odpu_p))
            {
                Database.AddTable(file_odpu_p,
                    new Column("id", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("nzp_file", DbType.Int32),
                    new Column("id_odpu", DbType.String.WithSize(20)),
                    new Column("rashod_type", DbType.Int32),
                    new Column("dat_uchet", DbType.Date),
                    new Column("val_cnt", DbType.Single),
                    new Column("id_ipu", DbType.Int32),
                    new Column("kod_serv", DbType.Decimal.WithSize(10, 0)));
                Database.AddIndex("ix1_file_odpu_p", false, file_odpu_p, "id_odpu", "id");
            }

            SchemaQualifiedObjectName file_oplats = new SchemaQualifiedObjectName() { Name = "file_oplats", Schema = CurrentSchema };
            if (!Database.TableExists(file_oplats))
            {
                Database.AddTable(file_oplats,
                    new Column("id", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("ls_id", DbType.String.WithSize(20)),
                    new Column("type_oper", DbType.Int32),
                    new Column("numplat", DbType.String.WithSize(80)),
                    new Column("dat_opl", DbType.Date),
                    new Column("dat_uchet", DbType.Date),
                    new Column("dat_izm", DbType.Date),
                    new Column("sum_oplat", DbType.Decimal.WithSize(14, 2)),
                    new Column("ist_opl", DbType.String.WithSize(80)),
                    new Column("mes_oplat", DbType.Date),
                    new Column("nzp_file", DbType.Int32),
                    new Column("nzp_pack", DbType.Int32),
                    new Column("id_serv", DbType.Int32));
                Database.AddIndex("ix1_file_oplats", false, file_oplats, "ls_id", "id");
            }

            SchemaQualifiedObjectName file_pack = new SchemaQualifiedObjectName() { Name = "file_pack", Schema = CurrentSchema };
            if (!Database.TableExists(file_pack))
            {
                Database.AddTable(file_pack,
                    new Column("id", DbType.Int32),
                    new Column("nzp_file", DbType.Int32),
                    new Column("dat_plat", DbType.Date),
                    new Column("num_plat", DbType.String.WithSize(20)),
                    new Column("sum_plat", DbType.Decimal.WithSize(14, 2)),
                    new Column("kol_plat", DbType.Int32));
                Database.AddIndex("ix1_file_pack", false, file_pack, "num_plat", "id");
            }

            SchemaQualifiedObjectName file_paramsdom = new SchemaQualifiedObjectName() { Name = "file_paramsdom", Schema = CurrentSchema };
            if (!Database.TableExists(file_paramsdom))
            {
                Database.AddTable(file_paramsdom,
                    new Column("id", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("id_dom", DbType.String.WithSize(20)),
                    new Column("id_prm", DbType.Int32),
                    new Column("val_prm", DbType.String.WithSize(100)),
                    new Column("nzp_dom", DbType.Int32),
                    new Column("nzp_file", DbType.Int32));
                Database.AddIndex("ix1_file_paramsdom", false, file_paramsdom, "id_dom", "id");
            }

            SchemaQualifiedObjectName file_paramsls = new SchemaQualifiedObjectName() { Name = "file_paramsls", Schema = CurrentSchema };
            if (!Database.TableExists(file_paramsls))
            {
                Database.AddTable(file_paramsls,
                    new Column("id", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("ls_id", DbType.String.WithSize(20)),
                    new Column("id_prm", DbType.Int32),
                    new Column("val_prm", DbType.String.WithSize(100)),
                    new Column("num_ls", DbType.Int32),
                    new Column("nzp_file", DbType.Int32));
                Database.AddIndex("ix1_file_paramsls", false, file_paramsls, "ls_id", "id");
            }

            SchemaQualifiedObjectName file_section = new SchemaQualifiedObjectName() { Name = "file_section", Schema = CurrentSchema };
            if (!Database.TableExists(file_section))
                Database.AddTable(file_section,
                    new Column("id", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("num_sec", DbType.Int32),
                    new Column("sec_name", DbType.String.WithSize(100)),
                    new Column("nzp_file", DbType.Int32),
                    new Column("is_need_load", DbType.Int32, ColumnProperty.None, 1));

            SchemaQualifiedObjectName file_serv = new SchemaQualifiedObjectName() { Name = "file_serv", Schema = CurrentSchema };
            if (!Database.TableExists(file_serv))
            {
                Database.AddTable(file_serv,
                    new Column("id", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("nzp_file", DbType.Int32, ColumnProperty.NotNull),
                    new Column("ls_id", DbType.String.WithSize(20), ColumnProperty.NotNull),
                    new Column("supp_id", DbType.Decimal.WithSize(18, 0), ColumnProperty.NotNull),
                    new Column("nzp_serv", DbType.Int32, ColumnProperty.NotNull),
                    new Column("sum_insaldo", DbType.Decimal.WithSize(14, 2), ColumnProperty.NotNull),
                    new Column("eot", DbType.Decimal.WithSize(14, 3), ColumnProperty.NotNull),
                    new Column("reg_tarif_percent", DbType.Decimal.WithSize(14, 3), ColumnProperty.NotNull),
                    new Column("reg_tarif", DbType.Decimal.WithSize(14, 3), ColumnProperty.NotNull),
                    new Column("nzp_measure", DbType.Int32, ColumnProperty.NotNull),
                    new Column("fact_rashod", DbType.Decimal.WithSize(18, 7), ColumnProperty.NotNull),
                    new Column("norm_rashod", DbType.Decimal.WithSize(18, 7), ColumnProperty.NotNull),
                    new Column("is_pu_calc", DbType.Int32, ColumnProperty.NotNull),
                    new Column("sum_nach", DbType.Decimal.WithSize(14, 2), ColumnProperty.NotNull),
                    new Column("sum_reval", DbType.Decimal.WithSize(14, 2), ColumnProperty.NotNull),
                    new Column("sum_subsidy", DbType.Decimal.WithSize(14, 2), ColumnProperty.NotNull),
                    new Column("sum_subsidyp", DbType.Decimal.WithSize(14, 2), ColumnProperty.NotNull),
                    new Column("sum_lgota", DbType.Decimal.WithSize(14, 2), ColumnProperty.NotNull),
                    new Column("sum_lgotap", DbType.Decimal.WithSize(14, 2), ColumnProperty.NotNull),
                    new Column("sum_smo", DbType.Decimal.WithSize(14, 2), ColumnProperty.NotNull),
                    new Column("sum_smop", DbType.Decimal.WithSize(14, 2), ColumnProperty.NotNull),
                    new Column("sum_money", DbType.Decimal.WithSize(14, 2), ColumnProperty.NotNull),
                    new Column("is_del", DbType.Int32, ColumnProperty.NotNull),
                    new Column("sum_outsaldo", DbType.Decimal.WithSize(14, 2), ColumnProperty.NotNull),
                    new Column("servp_row_number", DbType.Int32, ColumnProperty.NotNull),
                    new Column("nzp_kvar", DbType.Int32),
                    new Column("nzp_supp", DbType.Int32));
                Database.AddIndex("ix1_file_serv", false, file_serv, "ls_id", "nzp_serv", "nzp_measure", "id");
                Database.AddIndex("ix21_file_serv", false, file_serv, "nzp_file", "ls_id", "nzp_serv", "nzp_measure", "id");
                Database.AddIndex("ix2_file_serv", false, file_serv, "nzp_file", "ls_id", "nzp_serv");
            }

            SchemaQualifiedObjectName file_serv_tuning = new SchemaQualifiedObjectName() { Name = "file_serv_tuning", Schema = CurrentSchema };
            if (!Database.TableExists(file_serv_tuning))
                Database.AddTable(file_serv_tuning,
                    new Column("id", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("nzp_serv", DbType.Int32),
                    new Column("nzp_supp", DbType.Int32),
                    new Column("nzp_measure", DbType.Int32),
                    new Column("nzp_frm", DbType.Int32));

            SchemaQualifiedObjectName file_services = new SchemaQualifiedObjectName() { Name = "file_services", Schema = CurrentSchema };
            if (!Database.TableExists(file_services))
            {
                Database.AddTable(file_services,
                    new Column("id", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("id_serv", DbType.Int32),
                    new Column("service", DbType.String.WithSize(100)),
                    new Column("service2", DbType.String.WithSize(100)),
                    new Column("nzp_file", DbType.Int32),
                    new Column("nzp_measure", DbType.Int32),
                    new Column("ed_izmer", DbType.String.WithSize(30)),
                    new Column("type_serv", DbType.Int32),
                    new Column("nzp_serv", DbType.Int32));
                Database.AddIndex("ix1_file_services", false, file_services, "id_serv", "id");
            }

            SchemaQualifiedObjectName file_servls = new SchemaQualifiedObjectName() { Name = "file_servls", Schema = CurrentSchema };
            if (!Database.TableExists(file_servls))
            {
                Database.AddTable(file_servls,
                    new Column("id", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("nzp_file", DbType.Int32),
                    new Column("ls_id", DbType.Decimal.WithSize(14, 0)),
                    new Column("id_serv", DbType.String.WithSize(100)),
                    new Column("dat_start", DbType.Date),
                    new Column("dat_stop", DbType.Date),
                    new Column("supp_id", DbType.Decimal.WithSize(18, 0)));
                Database.AddIndex("ix1_file_servls", false, file_servls, "ls_id", "id");
            }

            SchemaQualifiedObjectName file_servp = new SchemaQualifiedObjectName() { Name = "file_servp", Schema = CurrentSchema };
            if (!Database.TableExists(file_servp))
            {
                Database.AddTable(file_servp,
                    new Column("id", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("nzp_file", DbType.Int32, ColumnProperty.NotNull),
                    new Column("reval_month", DbType.Date),
                    new Column("ls_id", DbType.String.WithSize(20), ColumnProperty.NotNull),
                    new Column("supp_id", DbType.Decimal.WithSize(18, 0), ColumnProperty.NotNull),
                    new Column("nzp_serv", DbType.Int32, ColumnProperty.NotNull),
                    new Column("eot", DbType.Decimal.WithSize(14, 3), ColumnProperty.NotNull),
                    new Column("reg_tarif_percent", DbType.Decimal.WithSize(14, 3), ColumnProperty.NotNull),
                    new Column("reg_tarif", DbType.Decimal.WithSize(14, 3), ColumnProperty.NotNull),
                    new Column("nzp_measure", DbType.Int32, ColumnProperty.NotNull),
                    new Column("fact_rashod", DbType.Decimal.WithSize(18, 7), ColumnProperty.NotNull),
                    new Column("norm_rashod", DbType.Decimal.WithSize(18, 7), ColumnProperty.NotNull),
                    new Column("is_pu_calc", DbType.Int32, ColumnProperty.NotNull),
                    new Column("sum_reval", DbType.Decimal.WithSize(14, 2), ColumnProperty.NotNull),
                    new Column("sum_subsidyp", DbType.Decimal.WithSize(14, 2), ColumnProperty.NotNull),
                    new Column("sum_lgotap", DbType.Decimal.WithSize(14, 2), ColumnProperty.NotNull),
                    new Column("sum_smop", DbType.Decimal.WithSize(14, 2), ColumnProperty.NotNull),
                    new Column("nzp_kvar", DbType.Int32),
                    new Column("nzp_supp", DbType.Int32));
                Database.AddIndex("ix1_file_servp", false, file_servp, "ls_id", "id");
            }

            SchemaQualifiedObjectName file_sql = new SchemaQualifiedObjectName() { Name = "file_sql", Schema = CurrentSchema };
            if (!Database.TableExists(file_sql))
            {
                Database.AddTable(file_sql,
                    new Column("id", DbType.Int32, ColumnProperty.NotNull),
                    new Column("nzp_file", DbType.Int32),
                    new Column("sql_zapr", DbType.String.WithSize(2000)));
                if (Database.ProviderName == "Informix") Database.ExecuteNonQuery("ALTER TABLE file_sql LOCK MODE (ROW)");
            }

            SchemaQualifiedObjectName file_supp = new SchemaQualifiedObjectName() { Name = "file_supp", Schema = CurrentSchema };
            if (!Database.TableExists(file_supp))
            {
                Database.AddTable(file_supp,
                    new Column("id", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("nzp_file", DbType.Int32, ColumnProperty.NotNull),
                    new Column("supp_id", DbType.Decimal.WithSize(18, 0), ColumnProperty.NotNull),
                    new Column("supp_name", DbType.String.WithSize(25), ColumnProperty.NotNull),
                    new Column("jur_address", DbType.String.WithSize(100)),
                    new Column("fact_address", DbType.String.WithSize(100)),
                    new Column("inn", DbType.String.WithSize(20)),
                    new Column("kpp", DbType.String.WithSize(20)),
                    new Column("rs", DbType.String.WithSize(20)),
                    new Column("bank", DbType.String.WithSize(100)),
                    new Column("bik", DbType.String.WithSize(20)),
                    new Column("ks", DbType.String.WithSize(20)),
                    new Column("nzp_supp", DbType.Int32));
                Database.AddIndex("ix1_file_supp", false, file_supp, "supp_id", "id");
            }

            SchemaQualifiedObjectName file_typenedopost = new SchemaQualifiedObjectName() { Name = "file_typenedopost", Schema = CurrentSchema };
            if (!Database.TableExists(file_typenedopost))
            {
                Database.AddTable(file_typenedopost,
                    new Column("id", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("nzp_file", DbType.Int32),
                    new Column("type_ned", DbType.Decimal.WithSize(10, 0)),
                    new Column("ned_name", DbType.String.WithSize(100)));
                Database.AddIndex("ix1_file_typenedopost", false, file_typenedopost, "type_ned", "id");
            }

            SchemaQualifiedObjectName file_typeparams = new SchemaQualifiedObjectName() { Name = "file_typeparams", Schema = CurrentSchema };
            if (!Database.TableExists(file_typeparams))
                Database.AddTable(file_typeparams,
                    new Column("id", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("id_prm", DbType.Int32),
                    new Column("prm_name", DbType.String.WithSize(100), ColumnProperty.None, "'1'"),
                    new Column("level_", DbType.Int32, ColumnProperty.None, 28),
                    new Column("type_prm", DbType.Int32, ColumnProperty.None, 2002),
                    new Column("nzp_file", DbType.Int32),
                    new Column("nzp_prm", DbType.Int32));

            SchemaQualifiedObjectName file_uchs = new SchemaQualifiedObjectName() { Name = "file_uchs", Schema = CurrentSchema };
            if (!Database.TableExists(file_uchs))
                Database.AddTable(file_uchs,
                    new Column("uch", DbType.Int32),
                    new Column("geu", DbType.String.WithSize(50)),
                    new Column("iddom", DbType.String.WithSize(15)),
                    new Column("nzp_dom", DbType.Int32),
                    new Column("nzp_geu", DbType.Int32));

            SchemaQualifiedObjectName file_urlic = new SchemaQualifiedObjectName() { Name = "file_urlic", Schema = CurrentSchema };
            if (!Database.TableExists(file_urlic))
            {
                Database.AddTable(file_urlic,
                    new Column("id", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("nzp_file", DbType.Int32, ColumnProperty.NotNull),
                    new Column("supp_id", DbType.Decimal.WithSize(18, 0), ColumnProperty.NotNull),
                    new Column("supp_name", DbType.String.WithSize(100), ColumnProperty.NotNull),
                    new Column("jur_address", DbType.String.WithSize(100)),
                    new Column("fact_address", DbType.String.WithSize(100)),
                    new Column("inn", DbType.String.WithSize(20)),
                    new Column("kpp", DbType.String.WithSize(20)),
                    new Column("rs", DbType.String.WithSize(20)),
                    new Column("bank", DbType.String.WithSize(100)),
                    new Column("bik_bank", DbType.String.WithSize(20)),
                    new Column("ks", DbType.String.WithSize(20)),
                    new Column("tel_chief", DbType.String.WithSize(20)),
                    new Column("tel_b", DbType.String.WithSize(20)),
                    new Column("chief_name", DbType.String.WithSize(100)),
                    new Column("chief_post", DbType.String.WithSize(40)),
                    new Column("b_name", DbType.String.WithSize(100)),
                    new Column("okonh1", DbType.String.WithSize(20)),
                    new Column("okonh2", DbType.String.WithSize(20)),
                    new Column("okpo", DbType.String.WithSize(20)),
                    new Column("bank_pr", DbType.String.WithSize(100)),
                    new Column("bank_adr", DbType.String.WithSize(100)),
                    new Column("bik", DbType.String.WithSize(20)),
                    new Column("rs_pr", DbType.String.WithSize(20)),
                    new Column("ks_pr", DbType.String.WithSize(20)),
                    new Column("post_and_name", DbType.String.WithSize(200)),
                    new Column("nzp_supp", DbType.Int32));
                Database.AddIndex("ix1_file_urlic", false, file_urlic, "supp_id", "id");
            }

            SchemaQualifiedObjectName file_voda = new SchemaQualifiedObjectName() { Name = "file_voda", Schema = CurrentSchema };
            if (!Database.TableExists(file_voda))
            {
                Database.AddTable(file_voda,
                    new Column("id", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("id_prm", DbType.Int32),
                    new Column("name", DbType.String.WithSize(100)),
                    new Column("nzp_file", DbType.Int32),
                    new Column("nzp_prm", DbType.Int32));
            }

            SchemaQualifiedObjectName file_vrub = new SchemaQualifiedObjectName() { Name = "file_vrub", Schema = CurrentSchema };
            if (!Database.TableExists(file_vrub))
            {
                Database.AddTable(file_vrub,
                    new Column("id", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("nzp_file", DbType.Int32),
                    new Column("ls_id", DbType.String.WithSize(20)),
                    new Column("gil_id", DbType.Int32),
                    new Column("dat_vrvib", DbType.Date),
                    new Column("dat_end", DbType.Date));
                Database.AddIndex("ix1_file_vrub", false, file_vrub, "ls_id", "id");
            }

            SchemaQualifiedObjectName files_imported = new SchemaQualifiedObjectName() { Name = "files_imported", Schema = CurrentSchema };
            if (!Database.TableExists(files_imported))
            {
                Database.AddTable(files_imported,
                    new Column("nzp_file", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("nzp_version", DbType.Int32, ColumnProperty.NotNull),
                    new Column("loaded_name", DbType.String.WithSize(90)),
                    new Column("saved_name", DbType.String.WithSize(90)),
                    new Column("nzp_status", DbType.Int32, ColumnProperty.NotNull),
                    new Column("created_by", DbType.Int32, ColumnProperty.NotNull),
                    new Column("created_on", DbType.DateTime, ColumnProperty.NotNull),
                    new Column("file_type", DbType.Int32),
                    new Column("nzp_exc", DbType.Int32),
                    new Column("nzp_exc_log", DbType.Int32),
                    new Column("percent", DbType.Decimal.WithSize(3, 2)),
                    new Column("pref", DbType.String.WithSize(20)),
                    new Column("diss_status", DbType.String.WithSize(50)));
                Database.AddIndex("ix2_files_imported", false, files_imported, "nzp_file");
            }

            SchemaQualifiedObjectName files_selected = new SchemaQualifiedObjectName() { Name = "files_selected", Schema = CurrentSchema };
            if (!Database.TableExists(files_selected))
            {
                Database.AddTable(files_selected,
                    new Column("nzp_file", DbType.Int32),
                    new Column("nzp_user", DbType.Int32),
                    new Column("pref", DbType.String.WithSize(20)),
                    new Column("num", DbType.Int32),
                    new Column("comment", DbType.String.WithSize(100)));
                Database.AddIndex("fi_sel_1", false, files_selected, "nzp_file", "nzp_user", "pref");
            }

            SchemaQualifiedObjectName file_formats = new SchemaQualifiedObjectName() { Name = "file_formats", Schema = CurrentSchema };
            if (!Database.TableExists(file_formats))
            {
                Database.AddTable(file_formats,
                    new Column("nzp_ff", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("format_name", DbType.String.WithSize(90)));
                Database.Insert(file_formats, new string[] { "nzp_ff", "format_name" }, new string[] { "1", "Стандартный" });
            }

            SchemaQualifiedObjectName file_statuses = new SchemaQualifiedObjectName() { Name = "file_statuses", Schema = CurrentSchema };
            if (!Database.TableExists(file_statuses))
            {
                Database.AddTable(file_statuses,
                    new Column("nzp_stat", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("status_name", DbType.String.WithSize(90)));
                Database.Insert(file_statuses, new string[] { "nzp_stat", "status_name" }, new string[] { "1", "Загружается" });
                Database.Insert(file_statuses, new string[] { "nzp_stat", "status_name" }, new string[] { "2", "Загружен" });
                Database.Insert(file_statuses, new string[] { "nzp_stat", "status_name" }, new string[] { "3", "Загружен с ошибками" });
                Database.Insert(file_statuses, new string[] { "nzp_stat", "status_name" }, new string[] { "4", "Учитывается" });
                Database.Insert(file_statuses, new string[] { "nzp_stat", "status_name" }, new string[] { "5", "Учтен" });
                Database.Insert(file_statuses, new string[] { "nzp_stat", "status_name" }, new string[] { "6", "Учтен с ошибками" });
                Database.Insert(file_statuses, new string[] { "nzp_stat", "status_name" }, new string[] { "7", "Удален" });
            }

            SchemaQualifiedObjectName file_versions = new SchemaQualifiedObjectName() { Name = "file_versions", Schema = CurrentSchema };
            if (!Database.TableExists(file_versions))
            {
                Database.AddTable(file_versions,
                    new Column("nzp_version", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("nzp_ff", DbType.Int32),
                    new Column("version_name", DbType.String.WithSize(90)));
                Database.Insert(file_versions, new string[] { "nzp_version", "nzp_ff", "version_name" }, new string[] { "1", "1", "1.0" });
                Database.Insert(file_versions, new string[] { "nzp_version", "nzp_ff", "version_name" }, new string[] { "3", "3", "1.2.1" });
                Database.Insert(file_versions, new string[] { "nzp_version", "nzp_ff", "version_name" }, new string[] { "2", "2", "1.2.2" });
            }
        }

        public override void Revert()
        {
            SetSchema(Bank.Data);
            SchemaQualifiedObjectName supplier_codes = new SchemaQualifiedObjectName() { Name = "supplier_codes", Schema = CurrentSchema };
            SchemaQualifiedObjectName counters = new SchemaQualifiedObjectName() { Name = "counters", Schema = CurrentSchema };
            SchemaQualifiedObjectName counters_spis = new SchemaQualifiedObjectName() { Name = "counters_spis", Schema = CurrentSchema };
            SchemaQualifiedObjectName pack_ls_block = new SchemaQualifiedObjectName() { Name = "pack_ls_block", Schema = CurrentSchema };
            SchemaQualifiedObjectName fn_dogovor_bank = new SchemaQualifiedObjectName() { Name = "fn_dogovor_bank", Schema = CurrentSchema };
            SchemaQualifiedObjectName tula_ex_sz = new SchemaQualifiedObjectName() { Name = "tula_ex_sz", Schema = CurrentSchema };
            SchemaQualifiedObjectName prm_8 = new SchemaQualifiedObjectName() { Name = "prm_8", Schema = CurrentSchema };
            SchemaQualifiedObjectName dat_files_imported = new SchemaQualifiedObjectName() { Name = "files_imported", Schema = CurrentSchema };
            SchemaQualifiedObjectName actions_types = new SchemaQualifiedObjectName() { Name = "actions_types", Schema = CurrentSchema };
            SchemaQualifiedObjectName sys_dictionary_values = new SchemaQualifiedObjectName() { Name = "sys_dictionary_values", Schema = CurrentSchema };
            if (Database.TableExists(supplier_codes)) Database.RemoveTable(supplier_codes);
            if (Database.ColumnExists(counters, "num_cnt")) Database.RemoveColumn(counters, "num_cnt");
            if (Database.ColumnExists(counters_spis, "num_cnt")) Database.RemoveColumn(counters_spis, "num_cnt");
            if (Database.TableExists(pack_ls_block)) Database.RemoveTable(pack_ls_block);
            if (Database.TableExists(fn_dogovor_bank)) Database.RemoveTable(fn_dogovor_bank);
            if (Database.ColumnExists(tula_ex_sz, "proc")) Database.RemoveColumn(tula_ex_sz, "proc");
            if (Database.ColumnExists(prm_8, "val_prm")) Database.RemoveColumn(prm_8, "val_prm");
            if (Database.ColumnExists(dat_files_imported, "pref")) Database.RemoveColumn(dat_files_imported, "pref");
            if (Database.TableExists(actions_types)) Database.RemoveTable(actions_types);
            if (Database.ColumnExists(sys_dictionary_values, "action")) Database.RemoveColumn(sys_dictionary_values, "action");

            SetSchema(Bank.Upload);
            SchemaQualifiedObjectName file_area = new SchemaQualifiedObjectName() { Name = "file_area", Schema = CurrentSchema };
            SchemaQualifiedObjectName file_blag = new SchemaQualifiedObjectName() { Name = "file_blag", Schema = CurrentSchema };
            SchemaQualifiedObjectName file_dom = new SchemaQualifiedObjectName() { Name = "file_dom", Schema = CurrentSchema };
            SchemaQualifiedObjectName file_gaz = new SchemaQualifiedObjectName() { Name = "file_gaz", Schema = CurrentSchema };
            SchemaQualifiedObjectName file_gilec = new SchemaQualifiedObjectName() { Name = "file_gilec", Schema = CurrentSchema };
            SchemaQualifiedObjectName file_head = new SchemaQualifiedObjectName() { Name = "file_head", Schema = CurrentSchema };
            SchemaQualifiedObjectName file_ipu = new SchemaQualifiedObjectName() { Name = "file_ipu", Schema = CurrentSchema };
            SchemaQualifiedObjectName file_ipu_p = new SchemaQualifiedObjectName() { Name = "file_ipu_p", Schema = CurrentSchema };
            SchemaQualifiedObjectName file_kvar = new SchemaQualifiedObjectName() { Name = "file_kvar", Schema = CurrentSchema };
            SchemaQualifiedObjectName file_kvarp = new SchemaQualifiedObjectName() { Name = "file_kvarp", Schema = CurrentSchema };
            SchemaQualifiedObjectName file_measures = new SchemaQualifiedObjectName() { Name = "file_measures", Schema = CurrentSchema };
            SchemaQualifiedObjectName file_mo = new SchemaQualifiedObjectName() { Name = "file_mo", Schema = CurrentSchema };
            SchemaQualifiedObjectName file_nedopost = new SchemaQualifiedObjectName() { Name = "file_nedopost", Schema = CurrentSchema };
            SchemaQualifiedObjectName file_odpu = new SchemaQualifiedObjectName() { Name = "file_odpu", Schema = CurrentSchema };
            SchemaQualifiedObjectName file_odpu_p = new SchemaQualifiedObjectName() { Name = "file_odpu_p", Schema = CurrentSchema };
            SchemaQualifiedObjectName file_oplats = new SchemaQualifiedObjectName() { Name = "file_oplats", Schema = CurrentSchema };
            SchemaQualifiedObjectName file_pack = new SchemaQualifiedObjectName() { Name = "file_pack", Schema = CurrentSchema };
            SchemaQualifiedObjectName file_paramsdom = new SchemaQualifiedObjectName() { Name = "file_paramsdom", Schema = CurrentSchema };
            SchemaQualifiedObjectName file_paramsls = new SchemaQualifiedObjectName() { Name = "file_paramsls", Schema = CurrentSchema };
            SchemaQualifiedObjectName file_section = new SchemaQualifiedObjectName() { Name = "file_section", Schema = CurrentSchema };
            SchemaQualifiedObjectName file_serv = new SchemaQualifiedObjectName() { Name = "file_serv", Schema = CurrentSchema };
            SchemaQualifiedObjectName file_serv_tuning = new SchemaQualifiedObjectName() { Name = "file_serv_tuning", Schema = CurrentSchema };
            SchemaQualifiedObjectName file_services = new SchemaQualifiedObjectName() { Name = "file_services", Schema = CurrentSchema };
            SchemaQualifiedObjectName file_servls = new SchemaQualifiedObjectName() { Name = "file_servls", Schema = CurrentSchema };
            SchemaQualifiedObjectName file_servp = new SchemaQualifiedObjectName() { Name = "file_servp", Schema = CurrentSchema };
            SchemaQualifiedObjectName file_sql = new SchemaQualifiedObjectName() { Name = "file_sql", Schema = CurrentSchema };
            SchemaQualifiedObjectName file_supp = new SchemaQualifiedObjectName() { Name = "file_supp", Schema = CurrentSchema };
            SchemaQualifiedObjectName file_typenedopost = new SchemaQualifiedObjectName() { Name = "file_typenedopost", Schema = CurrentSchema };
            SchemaQualifiedObjectName file_typeparams = new SchemaQualifiedObjectName() { Name = "file_typeparams", Schema = CurrentSchema };
            SchemaQualifiedObjectName file_uchs = new SchemaQualifiedObjectName() { Name = "file_uchs", Schema = CurrentSchema };
            SchemaQualifiedObjectName file_urlic = new SchemaQualifiedObjectName() { Name = "file_urlic", Schema = CurrentSchema };
            SchemaQualifiedObjectName file_voda = new SchemaQualifiedObjectName() { Name = "file_voda", Schema = CurrentSchema };
            SchemaQualifiedObjectName file_vrub = new SchemaQualifiedObjectName() { Name = "file_vrub", Schema = CurrentSchema };
            SchemaQualifiedObjectName files_imported = new SchemaQualifiedObjectName() { Name = "files_imported", Schema = CurrentSchema };
            SchemaQualifiedObjectName files_selected = new SchemaQualifiedObjectName() { Name = "files_selected", Schema = CurrentSchema };
            SchemaQualifiedObjectName file_formats = new SchemaQualifiedObjectName() { Name = "file_formats", Schema = CurrentSchema };
            SchemaQualifiedObjectName file_statuses = new SchemaQualifiedObjectName() { Name = "file_statuses", Schema = CurrentSchema };
            SchemaQualifiedObjectName file_versions = new SchemaQualifiedObjectName() { Name = "file_versions", Schema = CurrentSchema };
            if (Database.TableExists(file_area)) Database.RemoveTable(file_area);
            if (Database.TableExists(file_blag)) Database.RemoveTable(file_blag);
            if (Database.TableExists(file_dom)) Database.RemoveTable(file_dom);
            if (Database.TableExists(file_gaz)) Database.RemoveTable(file_gaz);
            if (Database.TableExists(file_gilec)) Database.RemoveTable(file_gilec);
            if (Database.TableExists(file_head)) Database.RemoveTable(file_head);
            if (Database.TableExists(file_ipu)) Database.RemoveTable(file_ipu);
            if (Database.TableExists(file_ipu_p)) Database.RemoveTable(file_ipu_p);
            if (Database.TableExists(file_kvar)) Database.RemoveTable(file_kvar);
            if (Database.TableExists(file_kvarp)) Database.RemoveTable(file_kvarp);
            if (Database.TableExists(file_measures)) Database.RemoveTable(file_measures);
            if (Database.TableExists(file_mo)) Database.RemoveTable(file_mo);
            if (Database.TableExists(file_nedopost)) Database.RemoveTable(file_nedopost);
            if (Database.TableExists(file_odpu)) Database.RemoveTable(file_odpu);
            if (Database.TableExists(file_odpu_p)) Database.RemoveTable(file_odpu_p);
            if (Database.TableExists(file_oplats)) Database.RemoveTable(file_oplats);
            if (Database.TableExists(file_pack)) Database.RemoveTable(file_pack);
            if (Database.TableExists(file_paramsdom)) Database.RemoveTable(file_paramsdom);
            if (Database.TableExists(file_paramsls)) Database.RemoveTable(file_paramsls);
            if (Database.TableExists(file_section)) Database.RemoveTable(file_section);
            if (Database.TableExists(file_serv)) Database.RemoveTable(file_serv);
            if (Database.TableExists(file_serv_tuning)) Database.RemoveTable(file_serv_tuning);
            if (Database.TableExists(file_services)) Database.RemoveTable(file_services);
            if (Database.TableExists(file_servls)) Database.RemoveTable(file_servls);
            if (Database.TableExists(file_servp)) Database.RemoveTable(file_servp);
            if (Database.TableExists(file_sql)) Database.RemoveTable(file_sql);
            if (Database.TableExists(file_supp)) Database.RemoveTable(file_supp);
            if (Database.TableExists(file_typenedopost)) Database.RemoveTable(file_typenedopost);
            if (Database.TableExists(file_typeparams)) Database.RemoveTable(file_typeparams);
            if (Database.TableExists(file_uchs)) Database.RemoveTable(file_uchs);
            if (Database.TableExists(file_urlic)) Database.RemoveTable(file_urlic);
            if (Database.TableExists(file_voda)) Database.RemoveTable(file_voda);
            if (Database.TableExists(file_vrub)) Database.RemoveTable(file_vrub);
            if (Database.TableExists(files_imported)) Database.RemoveTable(files_imported);
            if (Database.TableExists(files_selected)) Database.RemoveTable(files_selected);
            if (Database.TableExists(file_formats)) Database.RemoveTable(file_formats);
            if (Database.TableExists(file_statuses)) Database.RemoveTable(file_statuses);
            if (Database.TableExists(file_versions)) Database.RemoveTable(file_versions);
        }
    }

    [Migration(20140328035, MigrateDataBase.LocalBank)]
    public class Migration_20140328035_LocalBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);

            SetSchema(Bank.Data);
            SchemaQualifiedObjectName prm_8 = new SchemaQualifiedObjectName() { Name = "prm_8", Schema = CurrentSchema };
            if (Database.TableExists(prm_8) && !Database.ColumnExists(prm_8, "val_prm"))
                Database.AddColumn(prm_8, new Column("val_prm", DbType.String.WithSize(100)));

            SchemaQualifiedObjectName kart = new SchemaQualifiedObjectName() { Name = "kart", Schema = CurrentSchema };
            if (Database.TableExists(kart))
            {
                if (!Database.ColumnExists(kart, "strana_mr")) Database.AddColumn(kart, new Column("strana_mr", DbType.String.WithSize(30), ColumnProperty.None, null));
                if (!Database.ColumnExists(kart, "region_mr")) Database.AddColumn(kart, new Column("region_mr", DbType.String.WithSize(30), ColumnProperty.None, null));
                if (!Database.ColumnExists(kart, "okrug_mr")) Database.AddColumn(kart, new Column("okrug_mr", DbType.String.WithSize(30), ColumnProperty.None, null));
                if (!Database.ColumnExists(kart, "gorod_mr")) Database.AddColumn(kart, new Column("gorod_mr", DbType.String.WithSize(30), ColumnProperty.None, null));
                if (!Database.ColumnExists(kart, "npunkt_mr")) Database.AddColumn(kart, new Column("npunkt_mr", DbType.String.WithSize(30), ColumnProperty.None, null));
                if (!Database.ColumnExists(kart, "strana_op")) Database.AddColumn(kart, new Column("strana_op", DbType.String.WithSize(30), ColumnProperty.None, null));
                if (!Database.ColumnExists(kart, "region_op")) Database.AddColumn(kart, new Column("region_op", DbType.String.WithSize(30), ColumnProperty.None, null));
                if (!Database.ColumnExists(kart, "okrug_op")) Database.AddColumn(kart, new Column("okrug_op", DbType.String.WithSize(30), ColumnProperty.None, null));
                if (!Database.ColumnExists(kart, "npunkt_op")) Database.AddColumn(kart, new Column("npunkt_op", DbType.String.WithSize(30), ColumnProperty.None, null));
                if (!Database.ColumnExists(kart, "gorod_op")) Database.AddColumn(kart, new Column("gorod_op", DbType.String.WithSize(30), ColumnProperty.None, null));
                if (!Database.ColumnExists(kart, "strana_ku")) Database.AddColumn(kart, new Column("strana_ku", DbType.String.WithSize(30), ColumnProperty.None, null));
                if (!Database.ColumnExists(kart, "region_ku")) Database.AddColumn(kart, new Column("region_ku", DbType.String.WithSize(30), ColumnProperty.None, null));
                if (!Database.ColumnExists(kart, "okrug_ku")) Database.AddColumn(kart, new Column("okrug_ku", DbType.String.WithSize(30), ColumnProperty.None, null));
                if (!Database.ColumnExists(kart, "gorod_ku")) Database.AddColumn(kart, new Column("gorod_ku", DbType.String.WithSize(30), ColumnProperty.None, null));
                if (!Database.ColumnExists(kart, "npunkt_ku")) Database.AddColumn(kart, new Column("npunkt_ku", DbType.String.WithSize(30), ColumnProperty.None, null));
                if (!Database.ColumnExists(kart, "dat_fio_c")) Database.AddColumn(kart, new Column("dat_fio_c", DbType.Date, ColumnProperty.None, null));
                if (!Database.ColumnExists(kart, "rodstvo")) Database.AddColumn(kart, new Column("rodstvo", DbType.String.WithSize(30), ColumnProperty.None, null));
            }

            SchemaQualifiedObjectName counters = new SchemaQualifiedObjectName() { Name = "counters", Schema = CurrentSchema };
            if (Database.TableExists(counters) && !Database.ColumnExists(counters, "num_cnt"))
                Database.AddColumn(counters, new Column("num_cnt", DbType.String.WithSize(30)));

            SchemaQualifiedObjectName counters_spis = new SchemaQualifiedObjectName() { Name = "counters_spis", Schema = CurrentSchema };
            if (Database.TableExists(counters_spis) && !Database.ColumnExists(counters_spis, "num_cnt"))
                Database.AddColumn(counters_spis, new Column("num_cnt", DbType.String.WithSize(30)));
        }

        public override void Revert()
        {
            SetSchema(Bank.Kernel);

            SetSchema(Bank.Data);
            SchemaQualifiedObjectName prm_8 = new SchemaQualifiedObjectName() { Name = "prm_8", Schema = CurrentSchema };
            if (Database.ColumnExists(prm_8, "val_prm")) Database.RemoveColumn(prm_8, "val_prm");

            SchemaQualifiedObjectName kart = new SchemaQualifiedObjectName() { Name = "kart", Schema = CurrentSchema };
            if (Database.TableExists(kart))
            {
                if (Database.ColumnExists(kart, "strana_mr")) Database.RemoveColumn(kart, "strana_mr");
                if (Database.ColumnExists(kart, "region_mr")) Database.RemoveColumn(kart, "region_mr");
                if (Database.ColumnExists(kart, "okrug_mr")) Database.RemoveColumn(kart, "okrug_mr");
                if (Database.ColumnExists(kart, "gorod_mr")) Database.RemoveColumn(kart, "gorod_mr");
                if (Database.ColumnExists(kart, "npunkt_mr")) Database.RemoveColumn(kart, "npunkt_mr");
                if (Database.ColumnExists(kart, "strana_op")) Database.RemoveColumn(kart, "strana_op");
                if (Database.ColumnExists(kart, "region_op")) Database.RemoveColumn(kart, "region_op");
                if (Database.ColumnExists(kart, "okrug_op")) Database.RemoveColumn(kart, "okrug_op");
                if (Database.ColumnExists(kart, "npunkt_op")) Database.RemoveColumn(kart, "npunkt_op");
                if (Database.ColumnExists(kart, "gorod_op")) Database.RemoveColumn(kart, "gorod_op");
                if (Database.ColumnExists(kart, "strana_ku")) Database.RemoveColumn(kart, "strana_ku");
                if (Database.ColumnExists(kart, "region_ku")) Database.RemoveColumn(kart, "region_ku");
                if (Database.ColumnExists(kart, "okrug_ku")) Database.RemoveColumn(kart, "okrug_ku");
                if (Database.ColumnExists(kart, "gorod_ku")) Database.RemoveColumn(kart, "gorod_ku");
                if (Database.ColumnExists(kart, "npunkt_ku")) Database.RemoveColumn(kart, "npunkt_ku");
                if (Database.ColumnExists(kart, "dat_fio_c")) Database.RemoveColumn(kart, "dat_fio_c");
                if (Database.ColumnExists(kart, "rodstvo")) Database.RemoveColumn(kart, "rodstvo");
            }

            SchemaQualifiedObjectName counters = new SchemaQualifiedObjectName() { Name = "counters", Schema = CurrentSchema };
            SchemaQualifiedObjectName counters_spis = new SchemaQualifiedObjectName() { Name = "counters_spis", Schema = CurrentSchema };
            if (Database.ColumnExists(counters, "num_cnt")) Database.RemoveColumn(counters, "num_cnt");
            if (Database.ColumnExists(counters_spis, "num_cnt")) Database.RemoveColumn(counters_spis, "num_cnt");
        }
    }
}
