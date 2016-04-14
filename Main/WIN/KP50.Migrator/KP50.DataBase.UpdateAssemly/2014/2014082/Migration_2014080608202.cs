using System;
using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2014080608202, MigrateDataBase.CentralBank)]
    public class Migration_2014080608202_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName s_doc_groups = new SchemaQualifiedObjectName() { Name = "s_doc_groups", Schema = CurrentSchema };
            if (!Database.TableExists(s_doc_groups))
            {
                Database.AddTable(
                  s_doc_groups,
                  new Column("nzp_doc_group", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull), // Identity property is SERIAL and NOT NULL value default
                  new Column("doc_group", DbType.String.WithSize(250), ColumnProperty.NotNull)
                  );
            }
            if (!Database.IndexExists("ix_s_doc_groups_1", s_doc_groups))
            Database.AddIndex("ix_s_doc_groups_1", false, s_doc_groups, new string[] { "nzp_doc_group" });

            if (!Database.ConstraintExists(s_doc_groups, "pk_s_doc_groups")) 
            Database.AddPrimaryKey("pk_s_doc_groups", s_doc_groups, new string[] {"nzp_doc_group"});                               
            

            if (Database.TableExists(s_doc_groups))
            {
                if (Convert.ToInt32(Database.ExecuteScalar(" select count(*) from  " + s_doc_groups.Name + " where nzp_doc_group = 1")) == 0)
                {
                    Database.Insert(s_doc_groups, new string[] { "nzp_doc_group", "doc_group" },
                    new string[] { "1", "Изменение сальдо" });
                }
            }

            SchemaQualifiedObjectName s_type_doc = new SchemaQualifiedObjectName() { Name = "s_type_doc", Schema = CurrentSchema };
            if (Database.TableExists(s_type_doc))
            {
                if (!Database.ColumnExists(s_type_doc, "nzp_doc_group"))
                    Database.AddColumn(s_type_doc, new Column("nzp_doc_group", DbType.Int32));

                if (Convert.ToInt32(Database.ExecuteScalar(" select count(*) from  " + s_type_doc.Name + " where nzp_type_doc = 6")) == 0)
                {
                    Database.Insert(s_type_doc, new string[] { "nzp_type_doc", "doc_name"},
                    new string[] { "6", "Не указан" });
                }

                if (Database.ColumnExists(s_type_doc, "nzp_doc_group"))
                {
                    Database.Update(s_type_doc, new string[] { "nzp_doc_group" }, new string[] { "1" }, " nzp_type_doc in (1,2,3,4,5,6)");
                }
                if (!Database.IndexExists("ix_s_type_doc_1", s_type_doc))
                    Database.AddIndex("ix_s_type_doc_1", false, s_type_doc, new string[] { "nzp_type_doc" });

                if (!Database.ConstraintExists(s_type_doc, "pk_s_type_doc")) 
                Database.AddPrimaryKey("pk_s_type_doc", s_type_doc, new string[] { "nzp_type_doc" });           

                if (Database.ProviderName == "PostgreSQL")
                    if (!Database.ConstraintExists(s_type_doc, "fk_s_type_doc_nzp_doc_group")) 
                        Database.AddForeignKey("fk_s_type_doc_nzp_doc_group", s_type_doc, "nzp_doc_group", s_doc_groups, "nzp_doc_group");
            }

            SetSchema(Bank.Data);
            SchemaQualifiedObjectName document_base = new SchemaQualifiedObjectName() { Name = "document_base", Schema = CurrentSchema };
            if (!Database.TableExists(document_base))
            {
                Database.AddTable(
                  document_base,
                  new Column("nzp_doc_base", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull), // Identity property is SERIAL and NOT NULL value default
                  new Column("num_doc", DbType.String.WithSize(20), ColumnProperty.NotNull),
                  new Column("dat_doc", DbType.Date, ColumnProperty.NotNull),
                  new Column("nzp_type_doc", DbType.Int32, ColumnProperty.NotNull),
                  new Column("comment", DbType.String.WithSize(100), ColumnProperty.NotNull)
                  );
            }
            if (!Database.IndexExists("ix_document_base_1", document_base))
                Database.AddIndex("ix_document_base_1", false, document_base, new string[] { "nzp_doc_base" });
            if (!Database.ConstraintExists(document_base, "pk_document_base")) 
                Database.AddPrimaryKey("pk_document_base", document_base, new string[] { "nzp_doc_base" });
            if (Database.ProviderName == "PostgreSQL")
                if (!Database.ConstraintExists(document_base, "fk_document_base_nzp_type_doc")) 
                        Database.AddForeignKey("fk_document_base_nzp_type_doc", document_base, "nzp_type_doc", s_type_doc, "nzp_type_doc");
            

            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName s_type_uchet = new SchemaQualifiedObjectName() { Name = "s_type_uchet", Schema = CurrentSchema };
            if (Database.TableExists(s_type_uchet))
            {
                Database.Delete(s_type_uchet, "nzp_type_uchet in (6, 7)");
                Database.Insert(s_type_uchet, new string[] { "nzp_type_uchet", "type_uchet" },
                    new string[] { "6", "Изменение сальдо" });                
            }
            
            SchemaQualifiedObjectName s_typercl = new SchemaQualifiedObjectName() { Name = "s_typercl", Schema = CurrentSchema };
            if (Database.TableExists(s_typercl))
            {
                Database.Update(s_typercl, new string[] { "nzp_type_uchet", "typename" }, 
                    new string[] { "6", "Автоматическое распределение суммы по поставщику с возможностью указания услуги" }, " type_rcl = 110");

                Database.Update(s_typercl, new string[] { "nzp_type_uchet", "typename" }, new string[] { "4", "Автоматическая, внутри поставщика оплатами" }, " type_rcl = 104");
                Database.Delete(s_typercl, "type_rcl = 114");
                Database.Insert(s_typercl, new string[] { "type_rcl", "is_volum", "typename", "nzp_type_uchet", "is_auto", "comment", "is_actual" },
                    new string[] { "114", "0", "Автоматическая, внутри поставщика изменением сальдо", "6", "1", "Перераспределение переплат по услугам поставщика", "1" });

                Database.Update(s_typercl, new string[] { "nzp_type_uchet", "typename" }, new string[] { "4", "Автоматическая, внутри принципала, между поставщиками оплатами" }, " type_rcl = 105");
                Database.Delete(s_typercl, "type_rcl = 115");
                Database.Insert(s_typercl, new string[] { "type_rcl", "is_volum", "typename", "nzp_type_uchet", "is_auto", "comment", "is_actual" },
                    new string[] { "115", "0", "Автоматическая, внутри принципала, между поставщиками изменением сальдо", "6", "1", "Перераспределение переплат по услугам поставщиков, указанного принципала", "1" });

                Database.Update(s_typercl, new string[] { "nzp_type_uchet", "typename" }, new string[] { "4", "Автоматическая, между принципалами оплатами" }, " type_rcl = 106");
                Database.Delete(s_typercl, "type_rcl = 116");
                Database.Insert(s_typercl, new string[] { "type_rcl", "is_volum", "typename", "nzp_type_uchet", "is_auto", "comment", "is_actual" },
                    new string[] { "116", "0", "Автоматическая, между принципалами изменением сальдо", "6", "1", "Перераспределение переплат по услугам поставщиков всех принципалов лицевого счёта", "1" });

                if (!Database.IndexExists("ix_s_type_rcl_1", s_typercl))
                    Database.AddIndex("ix_s_type_rcl_1", false, s_typercl, new string[] { "type_rcl" });

                if (!Database.ConstraintExists(s_typercl, "pk_s_typercl")) 
                    Database.AddPrimaryKey("pk_s_typercl", s_typercl, new string[] { "type_rcl" });
            }

            SetSchema(Bank.Data);
            SchemaQualifiedObjectName reestr_perekidok = new SchemaQualifiedObjectName() { Name = "reestr_perekidok", Schema = CurrentSchema };
            if (!Database.ColumnExists(reestr_perekidok, "nzp_doc_base"))
                Database.AddColumn(reestr_perekidok, new Column("nzp_doc_base", DbType.Int32));
            if (!Database.IndexExists("ix_reestr_perekidok_1", reestr_perekidok))
                Database.AddIndex("ix_reestr_perekidok_1", false, reestr_perekidok, new string[] { "nzp_doc_base" });
            if (!Database.IndexExists("ix_reestr_perekidok_2", reestr_perekidok))
                Database.AddIndex("ix_reestr_perekidok_2", false, reestr_perekidok, new string[] { "nzp_reestr" });

            if (!Database.ConstraintExists(reestr_perekidok, "pk_reestr_perekidok")) 
                Database.AddPrimaryKey("pk_reestr_perekidok", reestr_perekidok, new string[] { "nzp_reestr" });

            if (Database.ProviderName == "PostgreSQL")
                if (!Database.ConstraintExists(reestr_perekidok, "fk_reestr_perekidok_nzp_doc_base")) 
                    Database.AddForeignKey("fk_reestr_perekidok_nzp_doc_base", reestr_perekidok, "nzp_doc_base", document_base, "nzp_doc_base");
        }
    }

    [Migration(2014080608202, MigrateDataBase.Charge)]
    public class Migration_2014080608202_Charge : Migration
    {
        public override void Apply()
        {
            SchemaQualifiedObjectName document_base = new SchemaQualifiedObjectName() { Name = "document_base", Schema = CentralData };
            SchemaQualifiedObjectName reestr_perekidok = new SchemaQualifiedObjectName() { Name = "reestr_perekidok", Schema = CentralData };
            SchemaQualifiedObjectName perekidka = new SchemaQualifiedObjectName() { Name = "perekidka", Schema = CurrentSchema };
            if (Database.TableExists(perekidka))
            {
                if (!Database.ColumnExists(perekidka, "nzp_doc_base")) Database.AddColumn(perekidka, new Column("nzp_doc_base", DbType.Int32));
                if (!Database.ConstraintExists(perekidka, "pk_perekidka")) Database.AddPrimaryKey("pk_perekidka", perekidka, new string[] { "nzp_rcl" });

                if (Database.ProviderName == "PostgreSQL")
                {
                    if (!Database.ConstraintExists(perekidka, "fk_perekidka_nzp_doc_base"))
                        Database.AddForeignKey("fk_perekidka_nzp_doc_base", perekidka, "nzp_doc_base", document_base, "nzp_doc_base");
                }
            }

            SchemaQualifiedObjectName del_supplier = new SchemaQualifiedObjectName() { Name = "del_supplier", Schema = CurrentSchema };
            if (Database.TableExists(del_supplier))
            {
                if (!Database.ColumnExists(del_supplier, "nzp_doc_base")) Database.AddColumn(del_supplier, new Column("nzp_doc_base", DbType.Int32));
                if (!Database.ColumnExists(del_supplier, "nzp_reestr")) Database.AddColumn(del_supplier, new Column("nzp_reestr", DbType.Int32));

                if (Database.ProviderName == "PostgreSQL")
                {
                    if (!Database.ConstraintExists(del_supplier, "fk_perekidka_nzp_doc_base"))
                        Database.AddForeignKey("fk_perekidka_nzp_doc_base", del_supplier, "nzp_doc_base", document_base, "nzp_doc_base");
                    if (!Database.ConstraintExists(del_supplier, "fk_perekidka_nzp_reestr"))
                        Database.AddForeignKey("fk_perekidka_nzp_reestr", del_supplier, "nzp_reestr", reestr_perekidok, "nzp_reestr");
                }
            }
        }
    }
}
