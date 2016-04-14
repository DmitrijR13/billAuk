using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;
namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2014052805201, MigrateDataBase.CentralBank)]
    public class Migration_2014052805201_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName file_versions_kernel = new SchemaQualifiedObjectName() { Name = "file_versions", Schema = CurrentSchema };
            if (Database.TableExists(file_versions_kernel)) Database.Delete(file_versions_kernel);
            if (Database.TableExists(file_versions_kernel)) Database.Insert(file_versions_kernel, new[] { "nzp_version", "nzp_ff", "version_name" }, new[] { "1", "1", "1.0" });
            if (Database.TableExists(file_versions_kernel)) Database.Insert(file_versions_kernel, new[] { "nzp_version", "nzp_ff", "version_name" }, new[] { "2", "2", "1.2.1" });
            if (Database.TableExists(file_versions_kernel)) Database.Insert(file_versions_kernel, new[] { "nzp_version", "nzp_ff", "version_name" }, new[] { "3", "3", "1.2.2" });
            if (Database.TableExists(file_versions_kernel)) Database.Insert(file_versions_kernel, new[] { "nzp_version", "nzp_ff", "version_name" }, new[] { "4", "4", "1.3.2" });

            SchemaQualifiedObjectName prm_name = new SchemaQualifiedObjectName() { Name = "prm_name", Schema = CurrentSchema };
            if (Database.TableExists(prm_name)) Database.Delete(prm_name, " nzp_prm >= 502 AND nzp_prm <= 507");
            if (Database.TableExists(prm_name)) Database.Delete(prm_name, " nzp_prm = 1269");
            if (Database.TableExists(prm_name)) Database.Delete(prm_name, " nzp_prm >= 1305 and nzp_prm <= 1312");

            if (Database.TableExists(prm_name)) Database.Delete(prm_name, " nzp_prm = 502");
            if (Database.TableExists(prm_name)) Database.Insert(prm_name, new [] { "nzp_prm", "name_prm", "type_prm", "prm_num" }, new [] { "502", "'ИНН'", "'char'", "9" });
            if (Database.TableExists(prm_name)) Database.Delete(prm_name, " nzp_prm = 503");
            if (Database.TableExists(prm_name)) Database.Insert(prm_name, new [] { "nzp_prm", "name_prm", "type_prm", "prm_num" }, new [] { "503", "'КПП'", "'char'", "9" });
            if (Database.TableExists(prm_name)) Database.Delete(prm_name, " nzp_prm = 504");
            if (Database.TableExists(prm_name)) Database.Insert(prm_name, new [] { "nzp_prm", "name_prm", "type_prm", "prm_num" }, new [] { "504", "'Название'", "'char'", "9" });
            if (Database.TableExists(prm_name)) Database.Delete(prm_name, " nzp_prm = 505");
            if (Database.TableExists(prm_name)) Database.Insert(prm_name, new [] { "nzp_prm", "name_prm", "type_prm", "prm_num" }, new [] { "505", "'Юридический адрес'", "'char'", "9" });
            if (Database.TableExists(prm_name)) Database.Delete(prm_name, " nzp_prm = 506");
            if (Database.TableExists(prm_name)) Database.Insert(prm_name, new [] { "nzp_prm", "name_prm", "type_prm", "prm_num" }, new [] { "506", "'ОКОНХ1'", "'char'", "9" });
            if (Database.TableExists(prm_name)) Database.Delete(prm_name, "  nzp_prm = 507");
            if (Database.TableExists(prm_name)) Database.Insert(prm_name, new [] { "nzp_prm", "name_prm", "type_prm", "prm_num" }, new [] { "507", "'ОКПО'", "'char'", "9" });
            if (Database.TableExists(prm_name)) Database.Delete(prm_name, "  nzp_prm = 1269");
            if (Database.TableExists(prm_name)) Database.Insert(prm_name, new [] { "nzp_prm", "name_prm", "type_prm", "prm_num" }, new [] { "1269", "'Фактический адрес'", "'char'", "9" });
            if (Database.TableExists(prm_name)) Database.Delete(prm_name, "  nzp_prm = 1305");
            if (Database.TableExists(prm_name)) Database.Insert(prm_name, new [] { "nzp_prm", "name_prm", "type_prm", "prm_num" }, new [] { "1305", "'Расчетный счет'", "'char'", "9" });
            if (Database.TableExists(prm_name)) Database.Delete(prm_name, "  nzp_prm = 1306");
            if (Database.TableExists(prm_name)) Database.Insert(prm_name, new [] { "nzp_prm", "name_prm", "type_prm", "prm_num" }, new [] { "1306", "'Телефон руководителя'", "'char'", "9" });
            if (Database.TableExists(prm_name)) Database.Delete(prm_name, "  nzp_prm = 1307");
            if (Database.TableExists(prm_name)) Database.Insert(prm_name, new [] { "nzp_prm", "name_prm", "type_prm", "prm_num" }, new [] { "1307", "'Телефон бухгалтерии'", "'char'", "9" });
            if (Database.TableExists(prm_name)) Database.Delete(prm_name, "  nzp_prm = 1308");
            if (Database.TableExists(prm_name)) Database.Insert(prm_name, new [] { "nzp_prm", "name_prm", "type_prm", "prm_num" }, new [] { "1308", "'ФИО руководителя'", "'char'", "9" });
            if (Database.TableExists(prm_name)) Database.Delete(prm_name, "  nzp_prm = 1309");
            if (Database.TableExists(prm_name)) Database.Insert(prm_name, new [] { "nzp_prm", "name_prm", "type_prm", "prm_num" }, new [] { "1309", "'Должность руководителя'", "'char'", "9" });
            if (Database.TableExists(prm_name)) Database.Delete(prm_name, "  nzp_prm = 1310");
            if (Database.TableExists(prm_name)) Database.Insert(prm_name, new [] { "nzp_prm", "name_prm", "type_prm", "prm_num" }, new [] { "1310", "'ФИО бухгалтера'", "'char'", "9" });
            if (Database.TableExists(prm_name)) Database.Delete(prm_name, "  nzp_prm = 1311");
            if (Database.TableExists(prm_name)) Database.Insert(prm_name, new [] { "nzp_prm", "name_prm", "type_prm", "prm_num" }, new [] { "1311", "'ОКОНХ2'", "'char'", "9" });
            if (Database.TableExists(prm_name)) Database.Delete(prm_name, "  nzp_prm = 1312");
            if (Database.TableExists(prm_name)) Database.Insert(prm_name, new [] { "nzp_prm", "name_prm", "type_prm", "prm_num" }, new [] { "1312", "'Должность + ФИО руководителя в родит падеже'", "'char'", "9" });

            SchemaQualifiedObjectName s_payer_types = new SchemaQualifiedObjectName() { Name = "s_payer_types", Schema = CurrentSchema };
            if (Database.TableExists(s_payer_types)) Database.Delete(s_payer_types, "  nzp_payer_type = 5");
            if (Database.TableExists(s_payer_types)) Database.Insert(s_payer_types, new [] { "nzp_payer_type", "type_name", "is_system" }, new [] { "5", "'РЦ'", "1" });
            if (Database.TableExists(s_payer_types)) Database.Delete(s_payer_types, "  nzp_payer_type = 6");
            if (Database.TableExists(s_payer_types)) Database.Insert(s_payer_types, new [] { "nzp_payer_type", "type_name", "is_system" }, new [] { "6", "'Ресурсоснабжающая организация'", "1" });
            if (Database.TableExists(s_payer_types)) Database.Delete(s_payer_types, "  nzp_payer_type = 7");
            if (Database.TableExists(s_payer_types)) Database.Insert(s_payer_types, new [] { "nzp_payer_type", "type_name", "is_system" }, new [] { "7", "'Арендатор жилья'", "1" });
            if (Database.TableExists(s_payer_types)) Database.Delete(s_payer_types, "  nzp_payer_type = 8");
            if (Database.TableExists(s_payer_types)) Database.Insert(s_payer_types, new [] { "nzp_payer_type", "type_name", "is_system" }, new [] { "8", "'Банк'", "1" });
            if (Database.TableExists(s_payer_types)) Database.Delete(s_payer_types, "  nzp_payer_type = 9");
            if (Database.TableExists(s_payer_types)) Database.Insert(s_payer_types, new [] { "nzp_payer_type", "type_name", "is_system" }, new [] { "9", "'Субабонент'", "1" });

            SchemaQualifiedObjectName payer_types = new SchemaQualifiedObjectName() { Name = "payer_types", Schema = CurrentSchema };
            if (Database.IndexExists("ix_payer_types_1", payer_types)) 
                Database.RemoveIndex("ix_payer_types_1", payer_types);
            Database.AddIndex("ix_payer_types_1", false, payer_types, "nzp_payer");

            SetSchema(Bank.Upload);
            SchemaQualifiedObjectName file_area = new SchemaQualifiedObjectName() { Name = "file_area", Schema = CurrentSchema };
            if (Database.ColumnExists(file_area, "kpp")) Database.ChangeColumn(file_area, "kpp", DbType.String.WithSize(20), false);
            if (Database.ColumnExists(file_area, "inn")) Database.ChangeColumn(file_area, "inn", DbType.String.WithSize(20), false);

            SchemaQualifiedObjectName file_supp = new SchemaQualifiedObjectName(){Name = "file_supp", Schema = CurrentSchema};
            if (Database.ColumnExists(file_supp, "supp_name")) Database.ChangeColumn(file_supp, "supp_name", DbType.String.WithSize(100), false);
            
            SchemaQualifiedObjectName file_urlic = new SchemaQualifiedObjectName() { Name = "file_urlic", Schema = CurrentSchema };
            if (Database.ColumnExists(file_urlic, "supp_id")) Database.AddColumn(file_urlic, new Column("urlic_id", DbType.Decimal.WithSize(18, 0)));
            if (Database.ColumnExists(file_urlic, "supp_name")) Database.AddColumn(file_urlic, new Column("urlic_name", DbType.String.WithSize(100)));
            if (Database.ColumnExists(file_urlic, "nzp_supp")) Database.AddColumn(file_urlic, new Column("nzp_payer", DbType.Int32));
            if (Database.ColumnExists(file_urlic, "inn")) Database.ChangeColumn(file_urlic, "inn", DbType.String.WithSize(20), false);
            if (Database.ColumnExists(file_urlic, "kpp")) Database.ChangeColumn(file_urlic, "kpp", DbType.String.WithSize(20), false);
            
            if (!Database.ColumnExists(file_urlic, "urlic_name_s")) Database.AddColumn(file_urlic, new Column("urlic_name_s", DbType.String.WithSize(10)));
            if (!Database.ColumnExists(file_urlic, "main_activity")) Database.AddColumn(file_urlic, new Column("main_activity", DbType.Int32));
            if (!Database.ColumnExists(file_urlic, "is_area")) Database.AddColumn(file_urlic, new Column("is_area", DbType.Int32));
            if (!Database.ColumnExists(file_urlic, "is_supp")) Database.AddColumn(file_urlic, new Column("is_supp", DbType.Int32));
            if (!Database.ColumnExists(file_urlic, "is_arendator")) Database.AddColumn(file_urlic, new Column("is_arendator", DbType.Int32));
            if (!Database.ColumnExists(file_urlic, "is_rc")) Database.AddColumn(file_urlic, new Column("is_rc", DbType.Int32));
            if (!Database.ColumnExists(file_urlic, "is_rso")) Database.AddColumn(file_urlic, new Column("is_rso", DbType.Int32));
            if (!Database.ColumnExists(file_urlic, "is_agent")) Database.AddColumn(file_urlic, new Column("is_agent", DbType.Int32));
            if (!Database.ColumnExists(file_urlic, "is_subabonent")) Database.AddColumn(file_urlic, new Column("is_subabonent", DbType.Int32));
            if (!Database.ColumnExists(file_urlic, "is_bank")) Database.AddColumn(file_urlic, new Column("is_bank", DbType.Int32));

            SchemaQualifiedObjectName file_kvar = new SchemaQualifiedObjectName() { Name = "file_kvar", Schema = CurrentSchema };
            if (!Database.ColumnExists(file_kvar, "type_owner")) Database.AddColumn(file_kvar, new Column("type_owner", DbType.String.WithSize(30)));
            if (!Database.ColumnExists(file_kvar, "id_gil")) Database.AddColumn(file_kvar, new Column("id_gil", DbType.Int32));
            if (!Database.ColumnExists(file_kvar, "uch")) Database.AddColumn(file_kvar, new Column("uch", DbType.String.WithSize(60)));
            if (!Database.ColumnExists(file_kvar, "id_urlic_pass_dom")) Database.AddColumn(file_kvar, new Column("id_urlic_pass_dom", DbType.Int32));

            SchemaQualifiedObjectName file_dog = new SchemaQualifiedObjectName() { Name = "file_dog", Schema = CurrentSchema }; 
            if (!Database.TableExists(file_dog))
            {
                Database.AddTable(file_dog,
                    new Column("nzp_file", DbType.Int32),
                    new Column("dog_id", DbType.Int32),
                    new Column("id_agent", DbType.Int32),
                    new Column("id_urlic_p", DbType.Int32),
                    new Column("id_supp", DbType.Int32),
                    new Column("dog_name", DbType.String.WithSize(60)),
                    new Column("dog_num", DbType.String.WithSize(20)),
                    new Column("dog_date", DbType.DateTime),
                    new Column("comment", DbType.String.WithSize(200)),
                    new Column("nzp_supp", DbType.Int32));
            }
            
            SchemaQualifiedObjectName file_serv = new SchemaQualifiedObjectName() { Name = "file_serv", Schema = CurrentSchema };
            if (!Database.ColumnExists(file_serv, "dog_id")) Database.AddColumn(file_serv, new Column("dog_id", DbType.Int32));
            if (!Database.ColumnExists(file_serv, "met_calc")) Database.AddColumn(file_serv, new Column("met_calc", DbType.Int32));
            if (!Database.ColumnExists(file_serv, "pkod")) Database.AddColumn(file_serv, new Column("pkod", DbType.Decimal.WithSize(13, 0)));

            if (Database.ColumnExists(file_serv, "supp_id")) Database.ChangeColumn(file_serv, "supp_id", DbType.Decimal.WithSize(18, 0), false);

            SchemaQualifiedObjectName file_servp = new SchemaQualifiedObjectName() { Name = "file_servp", Schema = CurrentSchema };
            if (!Database.ColumnExists(file_servp, "dog_id")) Database.AddColumn(file_servp, new Column("dog_id", DbType.Int32));

            if (Database.ColumnExists(file_servp, "supp_id")) Database.ChangeColumn(file_servp, "supp_id", DbType.Decimal.WithSize(18, 0), false);

            SchemaQualifiedObjectName file_reestr_ls = new SchemaQualifiedObjectName() { Name = "file_reestr_ls", Schema = CurrentSchema };
            if (!Database.TableExists(file_reestr_ls))
            {
                Database.AddTable(file_reestr_ls,
                    new Column("nzp_file", DbType.Int32),
                    new Column("ls_id_supp", DbType.Int32),
                    new Column("ls_id_ns", DbType.String.WithSize(20)),
                    new Column("ls_pkod", DbType.String.WithSize(20)),
                    new Column("dat_open", DbType.DateTime),
                    new Column("open_osnov", DbType.String.WithSize(100)),
                    new Column("dat_close", DbType.DateTime),
                    new Column("close_osnov", DbType.String.WithSize(100)),
                    new Column("ls_id_sz", DbType.String.WithSize(20)));
            }

            SchemaQualifiedObjectName file_rs = new SchemaQualifiedObjectName() { Name = "file_rs", Schema = CurrentSchema };
            if (!Database.TableExists(file_rs))
            {
                Database.AddTable(file_rs,
                    new Column("id", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("nzp_file", DbType.Int32),
                    new Column("rs", DbType.String.WithSize(20)),
                    new Column("id_bank", DbType.Int32),
                    new Column("id_urlic", DbType.Int32),
                    new Column("ks", DbType.String.WithSize(20)),
                    new Column("bik", DbType.String.WithSize(20)));
            }

            SchemaQualifiedObjectName file_agreement = new SchemaQualifiedObjectName() { Name = "file_agreement", Schema = CurrentSchema };
            if (!Database.TableExists(file_agreement))
            {
                Database.AddTable(file_agreement,
                    new Column("id", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("nzp_file", DbType.Int32),
                    new Column("id_dog", DbType.Int32),
                    new Column("id_dom", DbType.String.WithSize(20)),
                    new Column("id_serv_from", DbType.Int32),
                    new Column("id_urlic_agent", DbType.Int32),
                    new Column("id_serv_to", DbType.Int32),
                    new Column("percent", DbType.Decimal.WithSize(12, 2)),
                    new Column("dat_s", DbType.DateTime),
                    new Column("dat_po", DbType.DateTime));
            }
            
            SchemaQualifiedObjectName file_versions = new SchemaQualifiedObjectName() { Name = "file_versions", Schema = CurrentSchema };
            if (Database.TableExists(file_versions)) Database.Delete(file_versions);
            if (Database.TableExists(file_versions)) Database.Insert(file_versions, new[] { "nzp_version", "nzp_ff", "version_name" }, new[] { "1", "1", "1.0" });
            if (Database.TableExists(file_versions)) Database.Insert(file_versions, new[] { "nzp_version", "nzp_ff", "version_name" }, new[] { "2", "2", "1.2.1" });
            if (Database.TableExists(file_versions)) Database.Insert(file_versions, new[] { "nzp_version", "nzp_ff", "version_name" }, new[] { "3", "3", "1.2.2" });
            if (Database.TableExists(file_versions)) Database.Insert(file_versions, new[] { "nzp_version", "nzp_ff", "version_name" }, new[] { "4", "4", "1.3.2" });

            SchemaQualifiedObjectName file_dom = new SchemaQualifiedObjectName() { Name = "file_dom", Schema = CurrentSchema };
            if (!Database.ColumnExists(file_dom, "kod_fias")) Database.AddColumn(file_dom, new Column("kod_fias", DbType.String.WithSize(30)));
            if (Database.ColumnExists(file_dom, "area_id")) Database.ChangeColumn(file_dom, "area_id", DbType.Decimal.WithSize(18,0), false);
        }

        public override void Revert()
        {
            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName file_versions_kernel = new SchemaQualifiedObjectName() { Name = "file_versions_kernel", Schema = CurrentSchema };
            if (Database.TableExists(file_versions_kernel)) Database.Delete(file_versions_kernel);
            if (Database.TableExists(file_versions_kernel)) Database.Insert(file_versions_kernel, new[] { "nzp_version", "nzp_ff", "version_name" }, new[] { "1", "1", "1.0" });
            if (Database.TableExists(file_versions_kernel)) Database.Insert(file_versions_kernel, new[] { "nzp_version", "nzp_ff", "version_name" }, new[] { "2", "2", "1.2.1" });
            if (Database.TableExists(file_versions_kernel)) Database.Insert(file_versions_kernel, new[] { "nzp_version", "nzp_ff", "version_name" }, new[] { "3", "3", "1.2.2" });
            if (Database.TableExists(file_versions_kernel)) Database.Insert(file_versions_kernel, new[] { "nzp_version", "nzp_ff", "version_name" }, new[] { "4", "4", "1.3.2" });

            SchemaQualifiedObjectName prm_name = new SchemaQualifiedObjectName() { Name = "prm_name", Schema = CurrentSchema };
            if (Database.TableExists(prm_name)) Database.Delete(prm_name, "  nzp_prm >= 502 AND nzp_prm <= 507");
            if (Database.TableExists(prm_name)) Database.Delete(prm_name, "  nzp_prm = 1269");
            if (Database.TableExists(prm_name)) Database.Delete(prm_name, "  nzp_prm >= 1305 and nzp_prm <= 1312");

            if (Database.TableExists(prm_name)) Database.Delete(prm_name, "  nzp_prm = 502");
            if (Database.TableExists(prm_name)) Database.Delete(prm_name, "  nzp_prm = 503");
            if (Database.TableExists(prm_name)) Database.Delete(prm_name, "  nzp_prm = 504");
            if (Database.TableExists(prm_name)) Database.Delete(prm_name, "  nzp_prm = 505");
            if (Database.TableExists(prm_name)) Database.Delete(prm_name, "  nzp_prm = 506");
            if (Database.TableExists(prm_name)) Database.Delete(prm_name, "  nzp_prm = 507");
            if (Database.TableExists(prm_name)) Database.Delete(prm_name, "  nzp_prm = 1269");
            if (Database.TableExists(prm_name)) Database.Delete(prm_name, "  nzp_prm = 1305");
            if (Database.TableExists(prm_name)) Database.Delete(prm_name, "  nzp_prm = 1306");
            if (Database.TableExists(prm_name)) Database.Delete(prm_name, "  nzp_prm = 1307");
            if (Database.TableExists(prm_name)) Database.Delete(prm_name, "  nzp_prm = 1308");
            if (Database.TableExists(prm_name)) Database.Delete(prm_name, "  nzp_prm = 1309");
            if (Database.TableExists(prm_name)) Database.Delete(prm_name, "  nzp_prm = 1310");
            if (Database.TableExists(prm_name)) Database.Delete(prm_name, "  nzp_prm = 1311");
            if (Database.TableExists(prm_name)) Database.Delete(prm_name, "  nzp_prm = 1312");

            SchemaQualifiedObjectName s_payer_types = new SchemaQualifiedObjectName() { Name = "s_payer_types", Schema = CurrentSchema };
            if (Database.TableExists(s_payer_types)) Database.Delete(s_payer_types, "  nzp_payer_type = 5");
            if (Database.TableExists(s_payer_types)) Database.Delete(s_payer_types, "  nzp_payer_type = 6");
            if (Database.TableExists(s_payer_types)) Database.Delete(s_payer_types, "  nzp_payer_type = 7");
            if (Database.TableExists(s_payer_types)) Database.Delete(s_payer_types, "  nzp_payer_type = 8");
            if (Database.TableExists(s_payer_types)) Database.Delete(s_payer_types, "  nzp_payer_type = 9");

            SetSchema(Bank.Upload);
            SchemaQualifiedObjectName file_urlic = new SchemaQualifiedObjectName() { Name = "file_urlic", Schema = CurrentSchema };
            if (Database.ColumnExists(file_urlic, "urlic_id")) Database.RemoveColumn(file_urlic, "urlic_id");
            if (Database.ColumnExists(file_urlic, "urlic_name")) Database.RemoveColumn(file_urlic, "urlic_name");
            if (Database.ColumnExists(file_urlic, "nzp_payer")) Database.RemoveColumn(file_urlic, "nzp_payer");

            if (Database.ColumnExists(file_urlic, "urlic_name_s")) Database.RemoveColumn(file_urlic, "urlic_name_s");
            if (Database.ColumnExists(file_urlic, "main_activity")) Database.RemoveColumn(file_urlic, "main_activity");
            if (Database.ColumnExists(file_urlic, "is_area")) Database.RemoveColumn(file_urlic, "is_area");
            if (Database.ColumnExists(file_urlic, "is_supp")) Database.RemoveColumn(file_urlic, "is_supp");
            if (Database.ColumnExists(file_urlic, "is_arendator")) Database.RemoveColumn(file_urlic, "is_arendator");
            if (Database.ColumnExists(file_urlic, "is_rc")) Database.RemoveColumn(file_urlic, "is_rc");
            if (Database.ColumnExists(file_urlic, "is_rso")) Database.RemoveColumn(file_urlic, "is_rso");
            if (Database.ColumnExists(file_urlic, "is_agent")) Database.RemoveColumn(file_urlic, "is_agent");
            if (Database.ColumnExists(file_urlic, "is_subabonent")) Database.RemoveColumn(file_urlic, "is_subabonent");
            if (Database.ColumnExists(file_urlic, "is_bank")) Database.RemoveColumn(file_urlic, "is_bank");
            

            SchemaQualifiedObjectName file_kvar = new SchemaQualifiedObjectName() { Name = "file_kvar", Schema = CurrentSchema };
            if (Database.ColumnExists(file_kvar, "type_owner")) Database.RemoveColumn(file_kvar, "type_owner");
            if (Database.ColumnExists(file_kvar, "id_gil")) Database.RemoveColumn(file_kvar, "id_gil");
            if (Database.ColumnExists(file_kvar, "uch")) Database.RemoveColumn(file_kvar, "uch");
            if (Database.ColumnExists(file_kvar, "id_urlic_pass_dom")) Database.RemoveColumn(file_kvar, "id_urlic_pass_dom");

            SchemaQualifiedObjectName file_dog = new SchemaQualifiedObjectName() { Name = "file_dog", Schema = CurrentSchema };
            if (Database.TableExists(file_dog)) Database.RemoveTable(file_dog);

            SchemaQualifiedObjectName file_serv = new SchemaQualifiedObjectName() { Name = "file_serv", Schema = CurrentSchema };
            if (Database.ColumnExists(file_serv, "dog_id")) Database.RemoveColumn(file_serv, "dog_id");
            if (Database.ColumnExists(file_serv, "met_calc")) Database.RemoveColumn(file_serv, "met_calc");
            if (Database.ColumnExists(file_serv, "pkod")) Database.RemoveColumn(file_serv, "pkod");

            SchemaQualifiedObjectName file_servp = new SchemaQualifiedObjectName() { Name = "file_servp", Schema = CurrentSchema };
            if (Database.ColumnExists(file_servp, "dog_id")) Database.RemoveColumn(file_servp, "dog_id");

            SchemaQualifiedObjectName file_reestr_ls = new SchemaQualifiedObjectName() { Name = "file_reestr_ls", Schema = CurrentSchema };
            if (Database.TableExists(file_reestr_ls)) Database.RemoveTable(file_reestr_ls);

            SchemaQualifiedObjectName file_rs = new SchemaQualifiedObjectName() { Name = "file_rs", Schema = CurrentSchema };
            if (Database.TableExists(file_rs)) Database.RemoveTable(file_rs);

            SchemaQualifiedObjectName file_agreement = new SchemaQualifiedObjectName() { Name = "file_agreement", Schema = CurrentSchema };
            if (Database.TableExists(file_agreement)) Database.RemoveTable(file_agreement);

            SchemaQualifiedObjectName file_versions = new SchemaQualifiedObjectName() { Name = "file_versions", Schema = CurrentSchema };
            if (Database.TableExists(file_versions)) Database.Delete(file_versions);
            if (Database.TableExists(file_versions)) Database.Insert(file_versions, new[] { "nzp_version", "nzp_ff", "version_name" }, new[] { "1", "1", "1.0" });
            if (Database.TableExists(file_versions)) Database.Insert(file_versions, new[] { "nzp_version", "nzp_ff", "version_name" }, new[] { "2", "2", "1.2.1" });
            if (Database.TableExists(file_versions)) Database.Insert(file_versions, new[] { "nzp_version", "nzp_ff", "version_name" }, new[] { "3", "3", "1.2.2" });
            if (Database.TableExists(file_versions)) Database.Insert(file_versions, new[] { "nzp_version", "nzp_ff", "version_name" }, new[] { "4", "4", "1.3.2" });

            SchemaQualifiedObjectName file_dom = new SchemaQualifiedObjectName() { Name = "file_dom", Schema = CurrentSchema };
            if (!Database.ColumnExists(file_dom, "kod_fias")) Database.RemoveColumn(file_dom, "kod_fias");

        }
    }

    [Migration(2014052805201, MigrateDataBase.LocalBank)]
    public class Migration_2014052805201_LocalBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);
            SchemaQualifiedObjectName prm_9 = new SchemaQualifiedObjectName() { Name = "prm_9", Schema = CurrentSchema };
            if (Database.ColumnExists(prm_9, "val_prm")) Database.ChangeColumn(prm_9, "val_prm", DbType.String.WithSize(200), false);

            SchemaQualifiedObjectName counters = new SchemaQualifiedObjectName() { Name = "counters", Schema = CurrentSchema };
            if (Database.ColumnExists(counters, "num_cnt")) Database.ChangeColumn(counters, "num_cnt", DbType.String.WithSize(40), false);

            SchemaQualifiedObjectName counters_spis = new SchemaQualifiedObjectName() { Name = "counters_spis", Schema = CurrentSchema };
            if (Database.ColumnExists(counters_spis, "num_cnt")) Database.ChangeColumn(counters_spis, "num_cnt", DbType.String.WithSize(40), false);

            SchemaQualifiedObjectName counters_dom = new SchemaQualifiedObjectName() { Name = "counters_dom", Schema = CurrentSchema };
            if (Database.ColumnExists(counters_dom, "num_cnt")) Database.ChangeColumn(counters_dom, "num_cnt", DbType.String.WithSize(40), false);
        }

        public override void Revert()
        {
        }
    }
}
