using System;
using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015023
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2015022802301, MigrateDataBase.CentralBank)]
    public class Migration_2015022802301_CentralBank : Migration
    {
        public override void Apply()
        {
            if (Database.ProviderName != "PostgreSQL") return;

            Console.WriteLine("Создание архива таблиц fn_bank и fn_dogovor");
            MakeArchive();
            Console.WriteLine("Архив таблиц fn_bank и fn_dogovor создан");

            Console.WriteLine("Создание таблиц и колонок");
            AgentContractCreateTablesAndColumns();
            Console.WriteLine("Таблицы созданы");

            Console.WriteLine("Создание индексов");
            AgentContractCreateIndexes();
            Console.WriteLine("Индексы созданы");

            Console.WriteLine("Создание первичных ключей");
            AgentContractCreatePrimaryKeys();
            Console.WriteLine("Первичные ключи созданы");

            Database.SetSchema(CentralKernel);

            int isTula = Convert.ToInt32(Database.ExecuteScalar("select count(*) from s_point where flag = 1 and substring(cast(bank_number as varchar (250)), 1,2) = '71' "));
            if (isTula > 0)
            {
                Console.WriteLine("Подготовка данных Тулы");
                PrepareTulaData();
                Console.WriteLine("Данные Тулы подготовлены");
            }

            Console.WriteLine("Создание внешних ключей");
            AgentContractCreateForeignKeys();
            Console.WriteLine("Внешние ключи созданы");

            Database.ExecuteNonQuery("drop function if exists " + CentralPrefix + "_kernel.trigger_supplier() CASCADE;");
            Console.WriteLine("Приведение данных");
            AgentContractPrepareData();
            Console.WriteLine("Приведение данных выполнено");
           
            Console.WriteLine("Создание триггеров");
            AgentContractCreateTriggers();
            Console.WriteLine("Триггеры созданы");
        }

        public override void Revert()
        {
        }

        private void PrepareTulaData()
        {
            Database.SetSchema(CentralKernel);

            InsertPayer(500, "ООО Наш дом I");
            InsertPayer(502, "ООО ЖЕУ");
            InsertPayer(504, "Миллениум - 1");
            InsertPayer(508, "ООО Управдом");
            InsertPayer(510, "УК Алексин");
            InsertPayer(518, "УК ПСЦ");

            // ... fn_bank
            Database.SetSchema(CentralData);

            /*select nzp_payer_bank from nftul_data.fn_bank where nzp_payer_bank > 0 group by 1
            having count(*) > 1 order by nzp_payer_bank*/

            Database.ExecuteNonQuery("update fn_bank set bik = '047003608', kcount = '30101810300000000608' where nzp_payer_bank = 7001");
            Database.ExecuteNonQuery("update fn_bank set bik = '047003608', kcount = '30101810300000000608' where nzp_payer_bank = 100049");
            Database.ExecuteNonQuery("update fn_bank set bik = '042007738', kcount = '30101810100000000738' where nzp_payer_bank = 110001");
            Database.ExecuteNonQuery("update fn_bank set bik = '047003715', kcount = '30101810400000000715' where nzp_payer_bank = 110003");
            Database.ExecuteNonQuery("update fn_bank set bik = '047003001', kcount = '00000000000000000000' where nzp_payer_bank = 110004");
            Database.ExecuteNonQuery("update fn_bank set bik = '042908701', kcount = '30101810600000000701' where nzp_payer_bank = 150065");
            Database.ExecuteNonQuery("update fn_bank set bik = '047003756', kcount = '30101810100000000756' where nzp_payer_bank = 150072");
            Database.ExecuteNonQuery("update fn_bank set bik = '047003764', kcount = '30101810600000000764' where nzp_payer_bank = 150073");
            Database.ExecuteNonQuery("update fn_bank set bik = '047003725', kcount = '30101810500000000725' where nzp_payer_bank = 150075");
            Database.ExecuteNonQuery("update fn_bank set bik = '047003726', kcount = '30101810800000000726' where nzp_payer_bank = 154107");
            Database.ExecuteNonQuery("update fn_bank set bik = '123456789', kcount = '12345678999123456789' where nzp_payer_bank = 154525");
            Database.ExecuteNonQuery("update fn_bank set bik = '044585342', kcount = '30101810400000000342' where nzp_payer_bank = 154579");
            Database.ExecuteNonQuery("update fn_bank set bik = '000000000', kcount = '00000000000000000000' where nzp_payer_bank = 154617");
            Database.ExecuteNonQuery("update fn_bank set bik = '044585297', kcount = '30101810500000000297' where nzp_payer_bank = 154630");
            Database.ExecuteNonQuery("update fn_bank set bik = '047003726', kcount = '30101810800000000726' where nzp_payer_bank = 154636");
            Database.ExecuteNonQuery("update fn_bank set bik = '042007738', kcount = '30101810100000000738' where nzp_payer_bank = 154640");
            Database.ExecuteNonQuery("update fn_bank set bik = '125485697', kcount = '55555555555555500000' where nzp_payer_bank = 156645");
            Database.ExecuteNonQuery("update fn_bank set bik = '125485697', kcount = '55555555555555500000' where nzp_payer_bank = 156665");
            Database.ExecuteNonQuery("update fn_bank set bik = '125485697', kcount = '55555555555555500000' where nzp_payer_bank = 156673");
        }

        /// <summary>
        /// Создание архива таблиц fn_bank и fn_dogovor
        /// </summary>
        private void MakeArchive()
        { 
            SetSchema(Bank.Data);

            // архив таблицы fn_bank
            SchemaQualifiedObjectName fn_bank_old = new SchemaQualifiedObjectName() { Name = "fn_bank_ac_arch", Schema = CurrentSchema };
            if (!Database.TableExists(fn_bank_old))
            {
                Database.ExecuteNonQuery("create table fn_bank_ac_arch (LIKE fn_bank)");
                Database.ExecuteNonQuery("insert into fn_bank_ac_arch select * from fn_bank");
            }

            // архив таблицы fn_dogovor
            SchemaQualifiedObjectName fn_dogovor_old = new SchemaQualifiedObjectName() { Name = "fn_dogovor_ac_arch", Schema = CurrentSchema };
            if (!Database.TableExists(fn_bank_old))
            {
                Database.ExecuteNonQuery("create table fn_dogovor_ac_arch (LIKE fn_dogovor_old)");
                Database.ExecuteNonQuery("insert into fn_dogovor_ac_arch select * from fn_dogovor_old");
            }
        }

        /// <summary>
        /// Создание таблиц и колонок
        /// </summary>
        private void AgentContractCreateTablesAndColumns()
        {
            SetSchema(Bank.Data);

            // fn_scope
            SchemaQualifiedObjectName fn_scope = new SchemaQualifiedObjectName() { Name = "fn_scope", Schema = CurrentSchema };
            if (!Database.TableExists(fn_scope))
            {
                Database.AddTable(fn_scope, 
                    new Column("nzp_scope", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("changed_by", DbType.Int32, ColumnProperty.NotNull),
                    new Column("changed_on", DbType.DateTime, ColumnProperty.NotNull, "now()")
                    );
            }

            // fn_scope_adres
            SchemaQualifiedObjectName fn_scope_adres = new SchemaQualifiedObjectName() { Name = "fn_scope_adres", Schema = CurrentSchema };
            if (!Database.TableExists(fn_scope_adres))
            {
                Database.AddTable(fn_scope_adres, 
                    new Column("nzp_scope_adres", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("nzp_scope", DbType.Int32, ColumnProperty.NotNull),
                    new Column("nzp_wp", DbType.Int32, ColumnProperty.NotNull),
                    new Column("nzp_town", DbType.Int32, ColumnProperty.Null),
                    new Column("nzp_raj", DbType.Int32, ColumnProperty.Null),
                    new Column("nzp_ul", DbType.Int32, ColumnProperty.Null),
                    new Column("nzp_dom", DbType.Int32, ColumnProperty.Null),
                    new Column("changed_by", DbType.Int32, ColumnProperty.NotNull),
                    new Column("changed_on", DbType.DateTime, ColumnProperty.NotNull, "now()")
                    );
            }

            // fn_dogovor_bank_lnk
            SchemaQualifiedObjectName fn_dogovor_bank_lnk = new SchemaQualifiedObjectName() { Name = "fn_dogovor_bank_lnk", Schema = CurrentSchema };
            if (!Database.TableExists(fn_dogovor_bank_lnk))
            {
                Database.AddTable(fn_dogovor_bank_lnk, 
                    new Column("id", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("nzp_fd", DbType.Int32, ColumnProperty.NotNull),
                    new Column("nzp_fb", DbType.Int32, ColumnProperty.NotNull),
                    new Column("changed_by", DbType.Int32, ColumnProperty.NotNull),
                    new Column("changed_on", DbType.DateTime, ColumnProperty.NotNull, "now()")
                    );
            }
                        
            // fn_bank
            SchemaQualifiedObjectName fn_bank = new SchemaQualifiedObjectName() { Name = "fn_bank", Schema = CurrentSchema };
            if (!Database.ColumnExists(fn_bank, "note")) Database.AddColumn(fn_bank, new Column("note", DbType.String.WithSize(1000)));
            
            // ... drop not null 
            if (Database.ColumnExists(fn_bank, "num_count")) Database.ExecuteNonQuery("alter table fn_bank ALTER COLUMN num_count DROP NOT NULL");
            if (Database.ColumnExists(fn_bank, "bank_name")) Database.ExecuteNonQuery("alter table fn_bank ALTER COLUMN bank_name DROP NOT NULL");
            if (Database.ColumnExists(fn_bank, "kcount")) Database.ExecuteNonQuery("alter table fn_bank ALTER COLUMN kcount DROP NOT NULL");
            if (Database.ColumnExists(fn_bank, "bik")) Database.ExecuteNonQuery("alter table fn_bank ALTER COLUMN bik DROP NOT NULL");
            if (Database.ColumnExists(fn_bank, "npunkt")) Database.ExecuteNonQuery("alter table fn_bank ALTER COLUMN npunkt DROP NOT NULL");
  
            // fn_dogovor
            SchemaQualifiedObjectName fn_dogovor = new SchemaQualifiedObjectName() { Name = "fn_dogovor", Schema = CurrentSchema };
            if (!Database.ColumnExists(fn_dogovor, "nzp_payer_agent")) Database.AddColumn(fn_dogovor, new Column("nzp_payer_agent", DbType.Int32));
            if (!Database.ColumnExists(fn_dogovor, "nzp_payer_princip")) Database.AddColumn(fn_dogovor, new Column("nzp_payer_princip", DbType.Int32));
            if (!Database.ColumnExists(fn_dogovor, "nzp_scope")) Database.AddColumn(fn_dogovor, new Column("nzp_scope", DbType.Int32));
  
            // ... drop not null 
            if (Database.ColumnExists(fn_dogovor, "nzp_area")) Database.ExecuteNonQuery("alter table fn_dogovor ALTER COLUMN nzp_area DROP NOT NULL");
            if (Database.ColumnExists(fn_dogovor, "nzp_payer_ar")) Database.ExecuteNonQuery("alter table fn_dogovor ALTER COLUMN nzp_payer_ar DROP NOT NULL");
            if (Database.ColumnExists(fn_dogovor, "nzp_fb")) Database.ExecuteNonQuery("alter table fn_dogovor ALTER COLUMN nzp_fb DROP NOT NULL");
            if (Database.ColumnExists(fn_dogovor, "nzp_osnov")) Database.ExecuteNonQuery("alter table fn_dogovor ALTER COLUMN nzp_osnov DROP NOT NULL");
            if (Database.ColumnExists(fn_dogovor, "kpp")) Database.ExecuteNonQuery("alter table fn_dogovor ALTER COLUMN kpp DROP NOT NULL");
            if (Database.ColumnExists(fn_dogovor, "max_sum")) Database.ExecuteNonQuery("alter table fn_dogovor ALTER COLUMN max_sum DROP NOT NULL");
            if (Database.ColumnExists(fn_dogovor, "priznak_perechisl")) Database.ExecuteNonQuery("alter table fn_dogovor ALTER COLUMN priznak_perechisl DROP NOT NULL");
            if (Database.ColumnExists(fn_dogovor, "min_sum")) Database.ExecuteNonQuery("alter table fn_dogovor ALTER COLUMN min_sum DROP NOT NULL");
            if (Database.ColumnExists(fn_dogovor, "naznplat")) Database.ExecuteNonQuery("alter table fn_dogovor ALTER COLUMN naznplat DROP NOT NULL");
            if (Database.ColumnExists(fn_dogovor, "nzp_supp")) Database.ExecuteNonQuery("alter table fn_dogovor ALTER COLUMN nzp_supp DROP NOT NULL");
            if (Database.ColumnExists(fn_dogovor, "nzp_payer")) Database.ExecuteNonQuery("alter table fn_dogovor ALTER COLUMN nzp_payer DROP NOT NULL");

            // kernel
            SetSchema(Bank.Kernel);
            
            // s_payer
            SchemaQualifiedObjectName s_payer = new SchemaQualifiedObjectName() { Name = "s_payer", Schema = CurrentSchema };
            if (Database.ColumnExists(s_payer, "nzp_supp")) Database.ExecuteNonQuery("alter table s_payer ALTER COLUMN nzp_supp DROP NOT NULL");
            if (Database.ColumnExists(s_payer, "nzp_type")) Database.ExecuteNonQuery("alter table s_payer ALTER COLUMN nzp_type DROP NOT NULL");
            if (Database.ColumnExists(s_payer, "changed_on")) Database.ExecuteNonQuery("alter table s_payer ALTER COLUMN changed_on SET DEFAULT now()");
            
            // payer_types
            Database.ExecuteNonQuery("alter table payer_types ALTER COLUMN changed_on SET DEFAULT now()");

            string kernel = CentralPrefix + "_kernel.";
            string localPref = "";
            using (IDataReader reader = Database.ExecuteReader("select trim(bd_kernel) as pref from " + kernel + "s_point order by 1"))
            { 
                while (reader.Read())
                {
                    localPref = (string)reader["pref"];

                    SchemaQualifiedObjectName supplier = new SchemaQualifiedObjectName() { Name = "supplier", Schema = localPref + "_kernel" };

                    if (!Database.ColumnExists(supplier, "nzp_payer_podr")) Database.AddColumn(supplier, new Column("nzp_payer_podr", DbType.Int32));
                    if (!Database.ColumnExists(supplier, "fn_dogovor_bank_lnk_id")) Database.AddColumn(supplier, new Column("fn_dogovor_bank_lnk_id", DbType.Int32));
                    if (!Database.ColumnExists(supplier, "dpd")) Database.AddColumn(supplier, new Column("dpd", DbType.Int16, defaultValue: 0));
                    if (!Database.ColumnExists(supplier, "nzp_scope")) Database.AddColumn(supplier, new Column("nzp_scope", DbType.Int32));
                    if (!Database.ColumnExists(supplier, "changed_on")) Database.AddColumn(supplier, new Column("changed_on", DbType.DateTime, ColumnProperty.NotNull, "now()"));

                    if (Database.ColumnExists(supplier, "adres_supp")) Database.ExecuteNonQuery("alter table supplier ALTER COLUMN adres_supp DROP NOT NULL");
                    if (Database.ColumnExists(supplier, "phone_supp")) Database.ExecuteNonQuery("alter table supplier ALTER COLUMN phone_supp DROP NOT NULL");
                    if (Database.ColumnExists(supplier, "geton_plat")) Database.ExecuteNonQuery("alter table supplier ALTER COLUMN geton_plat DROP NOT NULL");
                }
            }

            // fn_scope
            Database.ChangeDefaultValue(fn_scope, "nzp_scope", "nextval(('" + CentralPrefix + "_data" + ".fn_scope_nzp_scope_seq')::text)::regclass");
            // fn_scope_adres
            Database.ChangeDefaultValue(fn_scope_adres, "nzp_scope_adres", "nextval(('" + CentralPrefix + "_data" + ".fn_scope_adres_nzp_scope_adres_seq')::text)::regclass");
            // fn_dogovor_bank_lnk
            Database.ChangeDefaultValue(fn_dogovor_bank_lnk, "id", "nextval(('" + CentralPrefix + "_data" + ".fn_dogovor_bank_lnk_id_seq')::text)::regclass");
        }

        /// <summary>
        /// Создание индексов
        /// </summary>
        private void AgentContractCreateIndexes()
        {
            SetSchema(Bank.Data);
            
            SchemaQualifiedObjectName fn_scope = new SchemaQualifiedObjectName() { Name = "fn_scope", Schema = CurrentSchema };
            SchemaQualifiedObjectName fn_scope_adres = new SchemaQualifiedObjectName() { Name = "fn_scope_adres", Schema = CurrentSchema };
            SchemaQualifiedObjectName fn_dogovor_bank_lnk = new SchemaQualifiedObjectName() { Name = "fn_dogovor_bank_lnk", Schema = CurrentSchema };

            // ... unique index
            if (!Database.IndexExists("IX_fn_scope_nzp_scope", fn_scope)) Database.AddIndex("IX_fn_scope_nzp_scope", true, fn_scope, "nzp_scope");
            if (!Database.IndexExists("IX_fn_scope_adres_nzp_scope_adres", fn_scope_adres)) Database.AddIndex("IX_fn_scope_adres_nzp_scope_adres", true, fn_scope_adres, "nzp_scope_adres");
            if (!Database.IndexExists("IX_fn_dogovor_bank_lnk_id", fn_dogovor_bank_lnk)) Database.AddIndex("IX_fn_dogovor_bank_lnk_id", true, fn_dogovor_bank_lnk, "id");

            // ... fn_scope
            if (!Database.IndexExists("ix_fn_scope_changed_by", fn_scope)) Database.AddIndex("ix_fn_scope_changed_by", false, fn_scope, "changed_by");
            
            // ... fn_scope_adres
            if (!Database.IndexExists("IX_fn_scope_adres_nzp_scope", fn_scope_adres)) Database.AddIndex("IX_fn_scope_adres_nzp_scope", false, fn_scope_adres, "nzp_scope");
            if (!Database.IndexExists("IX_fn_scope_adres_nzp_wp", fn_scope_adres)) Database.AddIndex("IX_fn_scope_adres_nzp_wp", false, fn_scope_adres, "nzp_wp");
            if (!Database.IndexExists("IX_fn_scope_adres_nzp_town", fn_scope_adres)) Database.AddIndex("IX_fn_scope_adres_nzp_town", false, fn_scope_adres, "nzp_town");
            if (!Database.IndexExists("IX_fn_scope_adres_nzp_raj", fn_scope_adres)) Database.AddIndex("IX_fn_scope_adres_nzp_raj", false, fn_scope_adres, "nzp_raj");
            if (!Database.IndexExists("IX_fn_scope_adres_nzp_ul", fn_scope_adres)) Database.AddIndex("IX_fn_scope_adres_nzp_ul", false, fn_scope_adres, "nzp_ul");
            if (!Database.IndexExists("IX_fn_scope_adres_nzp_dom", fn_scope_adres)) Database.AddIndex("IX_fn_scope_adres_nzp_dom", false, fn_scope_adres, "nzp_dom");
            if (!Database.IndexExists("ix_fn_scope_adres_changed_by", fn_scope_adres)) Database.AddIndex("ix_fn_scope_adres_changed_by", false, fn_scope_adres, "changed_by");

            // ... fn_dogovor_bank_lnk
            if (!Database.IndexExists("IX_fn_dogovor_bank_lnk_nzp_fd", fn_dogovor_bank_lnk)) Database.AddIndex("IX_fn_dogovor_bank_lnk_nzp_fd", false, fn_dogovor_bank_lnk, "nzp_fd");
            if (!Database.IndexExists("IX_fn_dogovor_bank_lnk_nzp_fb", fn_dogovor_bank_lnk)) Database.AddIndex("IX_fn_dogovor_bank_lnk_nzp_fb", false, fn_dogovor_bank_lnk, "nzp_fb");
            if (!Database.IndexExists("IX_fn_dogovor_bank_lnk_changed_by", fn_dogovor_bank_lnk)) Database.AddIndex("IX_fn_dogovor_bank_lnk_changed_by", false, fn_dogovor_bank_lnk, "changed_by");

            // ... fn_bank
            SchemaQualifiedObjectName fn_bank = new SchemaQualifiedObjectName() { Name = "fn_bank", Schema = CurrentSchema };
            if (!Database.IndexExists("IX_fn_bank_nzp_payer", fn_bank)) Database.AddIndex("IX_fn_bank_nzp_payer", false, fn_bank, "nzp_payer");
            if (!Database.IndexExists("IX_fn_bank_nzp_payer_bank", fn_bank)) Database.AddIndex("IX_fn_bank_nzp_payer_bank", false, fn_bank, "nzp_payer_bank");
            if (!Database.IndexExists("IX_fn_bank_nzp_user", fn_bank)) Database.AddIndex("IX_fn_bank_nzp_user", false, fn_bank, "nzp_user");

            // ... fn_dogovor
            SchemaQualifiedObjectName fn_dogovor = new SchemaQualifiedObjectName() { Name = "fn_dogovor", Schema = CurrentSchema };
            if (!Database.IndexExists("IX_fn_dogovor_nzp_payer_agent", fn_dogovor)) Database.AddIndex("IX_fn_dogovor_nzp_payer_agent", false, fn_dogovor, "nzp_payer_agent");
            if (!Database.IndexExists("IX_fn_dogovor_nzp_payer_princip", fn_dogovor)) Database.AddIndex("IX_fn_dogovor_nzp_payer_princip", false, fn_dogovor, "nzp_payer_princip");
            if (!Database.IndexExists("IX_fn_dogovor_nzp_user", fn_dogovor)) Database.AddIndex("IX_fn_dogovor_nzp_user", false, fn_dogovor, "nzp_user");
            if (!Database.IndexExists("IX_fn_dogovor_nzp_scope", fn_dogovor)) Database.AddIndex("IX_fn_dogovor_nzp_scope", false, fn_dogovor, "nzp_scope");

            // kernel
            SetSchema(Bank.Kernel);

            // ... s_payer
            SchemaQualifiedObjectName s_payer = new SchemaQualifiedObjectName() { Name = "s_payer", Schema = CurrentSchema };
            if (!Database.IndexExists("ix_s_payer_changed_by", s_payer)) Database.AddIndex("ix_s_payer_changed_by", false, s_payer, "changed_by");

            // ... payer_types
            SchemaQualifiedObjectName payer_types = new SchemaQualifiedObjectName() { Name = "payer_types", Schema = CurrentSchema };
            if (!Database.IndexExists("ix_payer_types_changed_by", payer_types)) Database.AddIndex("ix_payer_types_changed_by", false, payer_types, "changed_by");

            // ... supplier
            string kernel = CentralPrefix + "_kernel.";
            string localPref = "";
            using (IDataReader reader = Database.ExecuteReader("select trim(bd_kernel) as pref from " + kernel + "s_point order by 1"))
            {
                while (reader.Read())
                {
                    localPref = (string)reader["pref"];

                    SchemaQualifiedObjectName supplier = new SchemaQualifiedObjectName() { Name = "supplier", Schema = localPref + "_kernel" };

                    if (!Database.IndexExists("IX_supplier_nzp_payer_agent", supplier)) Database.AddIndex("IX_supplier_nzp_payer_agent", false, supplier, "nzp_payer_agent");
                    if (!Database.IndexExists("IX_supplier_nzp_payer_princip", supplier)) Database.AddIndex("IX_supplier_nzp_payer_princip", false, supplier, "nzp_payer_princip");
                    if (!Database.IndexExists("IX_supplier_nzp_payer_podr", supplier)) Database.AddIndex("IX_supplier_nzp_payer_podr", false, supplier, "nzp_payer_podr");
                    if (!Database.IndexExists("IX_supplier_nzp_payer_supp", supplier)) Database.AddIndex("IX_supplier_nzp_payer_supp", false, supplier, "nzp_payer_supp");
                    if (!Database.IndexExists("IX_supplier_fn_dogovor_bank_lnk_id", supplier)) Database.AddIndex("IX_supplier_fn_dogovor_bank_lnk_id", false, supplier, "fn_dogovor_bank_lnk_id");
                    if (!Database.IndexExists("IX_supplier_nzp_scope", supplier)) Database.AddIndex("IX_supplier_nzp_scope", false, supplier, "nzp_scope");
                    if (!Database.IndexExists("IX_supplier_changed_by", supplier)) Database.AddIndex("IX_supplier_changed_by", false, supplier, "changed_by");
                }
            }
        }

        /// <summary>
        /// Создание первичных ключей
        /// </summary>
        private void AgentContractCreatePrimaryKeys()
        { 
            SetSchema(Bank.Data);
            CreatePrimaryKey(CurrentSchema, "users", "nzp_user");
            CreatePrimaryKey(CurrentSchema, "s_town", "nzp_town");
            CreatePrimaryKey(CurrentSchema, "s_rajon", "nzp_raj");
            CreatePrimaryKey(CurrentSchema, "s_ulica", "nzp_ul");
            CreatePrimaryKey(CurrentSchema, "dom", "nzp_dom");
            CreatePrimaryKey(CurrentSchema, "fn_bank", "nzp_fb");
            CreatePrimaryKey(CurrentSchema, "fn_dogovor", "nzp_fd");
            CreatePrimaryKey(CurrentSchema, "fn_scope", "nzp_scope");
            CreatePrimaryKey(CurrentSchema, "fn_scope_adres", "nzp_scope_adres");
            CreatePrimaryKey(CurrentSchema, "fn_dogovor_bank_lnk", "id");
            
            SetSchema(Bank.Kernel);
            CreatePrimaryKey(CurrentSchema, "s_point", "nzp_wp");
            CreatePrimaryKey(CurrentSchema, "s_payer_types", "nzp_payer_type");
            CreatePrimaryKey(CurrentSchema, "s_payer", "nzp_payer");
            CreatePrimaryKey(CurrentSchema, "payer_types", "nzp_pt");

            // ... supplier
            string kernel = CentralPrefix + "_kernel.";
            string localPref = "";
            using (IDataReader reader = Database.ExecuteReader("select trim(bd_kernel) as pref from " + kernel + "s_point order by 1"))
            {
                while (reader.Read())
                {
                    localPref = (string)reader["pref"];
                    CreatePrimaryKey(localPref + "_kernel", "supplier", "nzp_supp");
                }
            }
        }

        /// <summary>
        /// Создание внешних ключей
        /// </summary>
        private void AgentContractCreateForeignKeys()
        {
            string kernel = CentralPrefix + "_kernel";
            string sdata = CentralPrefix + "_data";

            //Database.ExecuteNonQuery("delete from " + sdata + ".fn_bank where nzp_payer not in (select nzp_payer from " + kernel + "s_payer)");
            //Database.ExecuteNonQuery("delete from " + kernel + ".payer_types where nzp_payer not in (select nzp_payer from " + kernel + "s_payer)");

            CreateForeignKey(kernel, "s_payer", "changed_by", sdata, "users", "nzp_user");
    
            CreateForeignKey(kernel, "payer_types", "nzp_payer", kernel, "s_payer", "nzp_payer");
            CreateForeignKey(kernel, "payer_types", "nzp_payer_type", kernel, "s_payer_types", "nzp_payer_type");
            CreateForeignKey(kernel, "payer_types", "changed_by", sdata, "users", "nzp_user");
  
            CreateForeignKey(sdata, "fn_scope", "changed_by", sdata, "users", "nzp_user");
  
            CreateForeignKey(sdata, "fn_dogovor_bank_lnk", "nzp_fb", sdata, "fn_bank", "nzp_fb");
            CreateForeignKey(sdata, "fn_dogovor_bank_lnk", "nzp_fd", sdata, "fn_dogovor", "nzp_fd");
            CreateForeignKey(sdata, "fn_dogovor_bank_lnk", "changed_by", sdata, "users", "nzp_user");
  
            CreateForeignKey(sdata, "fn_bank", "nzp_payer", kernel, "s_payer", "nzp_payer");
            CreateForeignKey(sdata, "fn_bank", "nzp_payer_bank", kernel, "s_payer", "nzp_payer");
            CreateForeignKey(sdata, "fn_bank", "nzp_user", sdata, "users", "nzp_user");
  
            CreateForeignKey(sdata, "fn_dogovor", "nzp_payer_agent", kernel, "s_payer", "nzp_payer");
            CreateForeignKey(sdata, "fn_dogovor", "nzp_payer_princip", kernel, "s_payer", "nzp_payer");
            CreateForeignKey(sdata, "fn_dogovor", "nzp_user", sdata, "users", "nzp_user");
            CreateForeignKey(sdata, "fn_dogovor", "nzp_scope", sdata, "fn_scope", "nzp_scope");
  
            CreateForeignKey(sdata, "fn_scope_adres", "nzp_dom", sdata, "dom", "nzp_dom");
            CreateForeignKey(sdata, "fn_scope_adres", "nzp_scope", sdata, "fn_scope", "nzp_scope");
            CreateForeignKey(sdata, "fn_scope_adres", "nzp_raj", sdata, "s_rajon", "nzp_raj");
            CreateForeignKey(sdata, "fn_scope_adres", "nzp_town", sdata, "s_town", "nzp_town");
            CreateForeignKey(sdata, "fn_scope_adres", "nzp_ul", sdata, "s_ulica", "nzp_ul");
            CreateForeignKey(sdata, "fn_scope_adres", "nzp_wp", kernel, "s_point", "nzp_wp");
            CreateForeignKey(sdata, "fn_scope_adres", "changed_by", sdata, "users", "nzp_user");

            string localPref = "";
            using (IDataReader reader = Database.ExecuteReader("select trim(bd_kernel) as pref from " + kernel + ".s_point order by 1"))
            {
                while (reader.Read())
                {
                    localPref = (string)reader["pref"];
                    CreateForeignKey(localPref + "_kernel", "supplier", "fn_dogovor_bank_lnk_id", sdata, "fn_dogovor_bank_lnk", "id");
                    CreateForeignKey(localPref + "_kernel", "supplier", "nzp_payer_agent", kernel, "s_payer", "nzp_payer");
                    CreateForeignKey(localPref + "_kernel", "supplier", "nzp_payer_podr", kernel, "s_payer", "nzp_payer");
                    CreateForeignKey(localPref + "_kernel", "supplier", "nzp_payer_princip", kernel, "s_payer", "nzp_payer");
                    CreateForeignKey(localPref + "_kernel", "supplier", "nzp_payer_supp", kernel, "s_payer", "nzp_payer");
                    CreateForeignKey(localPref + "_kernel", "supplier", "changed_by", sdata, "users", "nzp_user");
                    CreateForeignKey(localPref + "_kernel", "supplier", "nzp_scope", sdata, "fn_scope", "nzp_scope");
                }
            }
        }
        
        /// <summary>
        /// Приведение данных
        /// </summary>
        private void AgentContractPrepareData()
        {
            string kernel = CentralPrefix + "_kernel.";
            string sdata = CentralPrefix + "_data.";

            // ... s_payer_types
            int cnt = Convert.ToInt32(Database.ExecuteScalar("select count(*) from " + kernel + "s_payer_types where nzp_payer_type = 11"));
            if (cnt <= 0) 
            {
                Database.ExecuteNonQuery("insert into " + kernel + "s_payer_types (nzp_payer_type, type_name, is_system) values (11, 'Подрядчик', 1)");
            }

            // проставить БИК и расчетные счета
            Database.ExecuteNonQuery("update " + kernel + "s_payer s set " +
                " bik = (select max(trim(bik))   from " + sdata + "fn_bank b where s.nzp_payer = b.nzp_payer_bank), " + 
                " ks = (select max(trim(kcount)) from " + sdata + "fn_bank b where s.nzp_payer = b.nzp_payer_bank) " +
                " where s.nzp_payer in (select nzp_payer_bank from " + sdata + "fn_bank)");
  
            // для банков довставить типы
            Database.ExecuteNonQuery("insert into " + kernel + "payer_types (nzp_payer, nzp_payer_type, changed_by, changed_on) " +
                " select distinct nzp_payer, 8, 1, now() from " + sdata + "fn_bank " + 
                " where nzp_payer not in (select nzp_payer from " + kernel + "payer_types where nzp_payer_type = 8)");

            // fn_dogovor
            // ... nzp_payer_agent
            Database.ExecuteNonQuery("update " + sdata + "fn_dogovor d set " +
                " nzp_payer_agent = (select max(s.nzp_payer_agent) from " + kernel + "supplier s where s.nzp_supp = d.nzp_supp and s.nzp_payer_agent is not null) " + 
	            " where d.nzp_payer_agent is null");

            // ... nzp_payer_princip
            Database.ExecuteNonQuery("update " + sdata + "fn_dogovor d set " +
                " nzp_payer_princip = (select max(s.nzp_payer_princip) from " + kernel + "supplier s where s.nzp_supp = d.nzp_supp and s.nzp_payer_princip is not null) " + 
	            " where d.nzp_payer_princip is null");

            // ... довставить агентов и принципалов
            Database.ExecuteNonQuery("insert into " + sdata + "fn_dogovor (nzp_payer_agent, nzp_payer_princip, nzp_supp, nzp_user, dat_when) " +
                " select nzp_payer_agent, nzp_payer_princip, max(s.nzp_supp), 1, now() " +
                " from " + kernel + "supplier s " +
                " where s.nzp_payer_princip is not null " +
	                " and not exists (select 1 from " + sdata + "fn_dogovor d " +
	                " where d.nzp_payer_agent = s.nzp_payer_agent " + 
                        " and d.nzp_payer_princip = s.nzp_payer_princip " + 
                        " and s.nzp_payer_agent is not null " + 
                        " and s.nzp_payer_princip is not null) " +
                " group by 1,2");

            // fn_scope
            cnt = Convert.ToInt32(Database.ExecuteScalar("select count(*) from " + sdata + "fn_scope"));
            if (cnt > 0)
            {
                cnt = Convert.ToInt32(Database.ExecuteScalar("select max(nzp_scope) from " + sdata + "fn_scope"));
            }
            else
            {
                cnt = 1;
            }

            using (IDataReader reader = Database.ExecuteReader("select nzp_fd from " + sdata + "fn_dogovor " +
                " where nzp_scope is null " + 
                " and nzp_payer_agent is not null and nzp_payer_princip is not null"))
            {
                int nzp_fd = 0;
                
                while (reader.Read())
                { 
                    cnt++;
                    nzp_fd = (int)reader["nzp_fd"];

                    Database.ExecuteNonQuery("insert into " + sdata + "fn_scope (nzp_scope, changed_by, changed_on) values (" + cnt + ", 1, now() )");
                    Database.ExecuteNonQuery("insert into " + sdata + "fn_scope_adres (nzp_scope, nzp_wp, changed_by, changed_on) " + 
                        " select " + cnt + ", nzp_wp, 1, now() from " + kernel + "s_point");
                    Database.ExecuteNonQuery("update " + sdata + "fn_dogovor set nzp_scope = " + cnt + " where nzp_fd = " + nzp_fd);         
                }
            }

            Database.ExecuteNonQuery("ALTER SEQUENCE " + sdata + "fn_scope_nzp_scope_seq RESTART with " + cnt);

            // fn_dogovor_bank_lnk
            Database.ExecuteNonQuery("insert into " + sdata + "fn_dogovor_bank_lnk (nzp_fd, nzp_fb, changed_by, changed_on) " +
                " select a.nzp_fd, max(b.nzp_fb), 1, now() " +
                " from " + sdata + "fn_dogovor a, " + sdata + "fn_bank b " +
                " where b.nzp_payer = a.nzp_payer_princip " + 
                    " and not exists (select 1 from " + sdata + "fn_dogovor_bank_lnk l where l.nzp_fd = a.nzp_fd and l.nzp_fb = b.nzp_fb) " +
                "group by 1");

            // supplier
            string localPref = "";
            using (IDataReader reader = Database.ExecuteReader("select trim(bd_kernel) as pref from " + kernel + "s_point order by 1"))
            {
                while (reader.Read())
                {
                    localPref = (string)reader["pref"];
                    Database.ExecuteNonQuery("update " + localPref + "_kernel.supplier s set " +
                        " fn_dogovor_bank_lnk_id = (select max(l.id) " +
                        " from " + sdata + "fn_dogovor d, " + sdata + "fn_dogovor_bank_lnk l " +
                        " where s.nzp_payer_agent = d.nzp_payer_agent " +
                            " and s.nzp_payer_princip = d.nzp_payer_princip " +
                            " and d.nzp_fd = l.nzp_fd) " +
                        " where s.fn_dogovor_bank_lnk_id is null");
                }
            }
        }

        private void AgentContractCreateTriggers()
        {
            string kernel = CentralPrefix + "_kernel.";
            string sdata = CentralPrefix + "_data.";

            Database.ExecuteNonQuery(
"CREATE function " + kernel + @"trigger_supplier() RETURNS trigger AS 
$trigger_supplier$
DECLARE
  fn_dogovor_nzp_payer_agent integer;
  fn_dogovor_nzp_payer_princip integer;
BEGIN
  select nzp_payer_agent, nzp_payer_princip into fn_dogovor_nzp_payer_agent, fn_dogovor_nzp_payer_princip 
  from " + sdata + "fn_dogovor_bank_lnk l, " + sdata + @"fn_dogovor d
  where l.nzp_fd = d.nzp_fd
    and l.id = NEW.fn_dogovor_bank_lnk_id;

  if NEW.nzp_payer_agent <> fn_dogovor_nzp_payer_agent 
   and NEW.nzp_payer_agent > 0         and fn_dogovor_nzp_payer_agent > 0
   and NEW.nzp_payer_agent is not null and fn_dogovor_nzp_payer_agent is not null
  then
    RAISE EXCEPTION 'Платежный агент договора ЖКУ не соответствует платежному агента договора ЕРЦ';
  end if;

  if NEW.nzp_payer_princip <> fn_dogovor_nzp_payer_princip 
    and NEW.nzp_payer_princip > 0          and fn_dogovor_nzp_payer_princip > 0
    and NEW.nzp_payer_princip is not null  and fn_dogovor_nzp_payer_princip is not null
  then
    RAISE EXCEPTION 'Принципал договора ЖКУ не соответствует принципалу договора ЕРЦ';
  end if;      

  return NEW;
END;
$trigger_supplier$
LANGUAGE  plpgsql;");

            // supplier
            string localPref = "";
            using (IDataReader reader = Database.ExecuteReader("select trim(bd_kernel) as pref from " + kernel + "s_point order by 1"))
            {
                while (reader.Read())
                {
                    localPref = (string)reader["pref"];
                    Database.ExecuteNonQuery("CREATE TRIGGER ins_supplier BEFORE INSERT ON " + localPref + "_kernel.supplier FOR EACH ROW EXECUTE PROCEDURE " + kernel + "trigger_supplier()");
                    Database.ExecuteNonQuery("CREATE TRIGGER upd_supplier BEFORE UPDATE ON " + localPref + "_kernel.supplier FOR EACH ROW EXECUTE PROCEDURE " + kernel + "trigger_supplier()");
                }
            }
        }

        /// <summary>
        /// Создать первичный ключ
        /// </summary>
        /// <param name="schm">Схема</param>
        /// <param name="tbl">Таблица</param>
        /// <param name="clmn">Колонка</param>
        private void CreatePrimaryKey(string schm, string tbl, string clmn)
        { 
            string sql = "select count(*) " +
                " from information_schema.table_constraints t_c, information_schema.key_column_usage kcu " +
                " where t_c.constraint_name = kcu.constraint_name " +
                "   and t_c.constraint_type = 'PRIMARY KEY' " +
                "   and lower(kcu.table_schema) = lower(trim('" + schm + "')) " +
                "   and lower(t_c.table_schema) = lower(trim('" + schm + "')) " +
                "   and lower(kcu.table_name) = lower(trim('" + tbl + "')) " +
                "   and lower(t_c.table_name) = lower(trim('" + tbl + "')) " +
                "   and lower(kcu.column_name) = lower(trim('" + clmn + "'))";
            
            int cnt = Convert.ToInt32(Database.ExecuteScalar(sql));

            if (cnt <= 0)
            {
                Database.ExecuteNonQuery("alter table " + schm + "." + tbl + " ADD CONSTRAINT " + tbl.Trim() + "_pkey PRIMARY KEY (" + clmn + ")");
            }
        }

        /// <summary>
        /// Создать внешний ключ
        /// </summary>
        /// <param name="schm">Схема</param>
        /// <param name="tbl">Таблица</param>
        /// <param name="clmn">Колонка</param>
        private void CreateForeignKey(string schm, string tbl, string clmn, string ref_schema, string ref_table, string ref_clmn)
        {
            string cnstr = "FK_" + tbl.Trim() + "_" + clmn.Trim();

            string sql = "select count(*) " +
                " from information_schema.table_constraints t_c, information_schema.key_column_usage kcu " +
                " where t_c.constraint_name = kcu.constraint_name " +
                "   and t_c.constraint_type = 'FOREIGN KEY' " +
                "   and lower(kcu.table_schema) = lower(trim('" + schm + "')) " +
                "   and lower(t_c.table_schema) = lower(trim('" + schm + "')) " +
                "   and lower(kcu.table_name) = lower(trim('" + tbl + "')) " +
                "   and lower(t_c.table_name) = lower(trim('" + tbl + "')) " +
                "   and lower(kcu.column_name) = lower(trim('" + clmn + "'))";

            int cnt = Convert.ToInt32(Database.ExecuteScalar(sql));

            if (cnt <= 0)
            {
                SchemaQualifiedObjectName primaryTable = new SchemaQualifiedObjectName() { Name = tbl, Schema = schm };
                SchemaQualifiedObjectName refTable = new SchemaQualifiedObjectName() { Name = ref_table, Schema = ref_schema };

                if (!Database.ConstraintExists(primaryTable, cnstr))
                {
                    Database.AddForeignKey(cnstr, primaryTable, clmn, refTable, ref_clmn);
                }
            }
        }
    
        /// <summary>
        /// Вставка контрагентов
        /// </summary>
        /// <param name="nzp_payer"></param>
        /// <param name="payer"></param>
        private void InsertPayer(int nzp_payer, string payer)
        {
            int cnt = Convert.ToInt32(Database.ExecuteScalar("select count(*) from s_payer where nzp_payer = " + nzp_payer));
            if (cnt <= 0)
            {
                Database.ExecuteNonQuery("insert into s_payer (nzp_payer, payer, npayer, changed_by, changed_on) values (" + 
                    nzp_payer + "," + 
                    "'" + payer + "'" + "," + 
                    "'" + payer + "'" + "," + 
                    " 1, now()) ");
            }
                
        }
    }
}