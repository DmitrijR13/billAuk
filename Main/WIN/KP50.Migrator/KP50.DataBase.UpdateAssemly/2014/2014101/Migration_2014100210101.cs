using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2014100210101, MigrateDataBase.Fin)]
    public class Migration_2014100210101 : Migration
    {
        public override void Apply()
        {
            SchemaQualifiedObjectName pack = new SchemaQualifiedObjectName();
            pack.Name = "pack";
            pack.Schema = CurrentSchema;

            if (Database.TableExists(pack))
            {
                if (!Database.ColumnExists(pack, "nzp_payer"))
                {
                    Database.AddColumn(pack, new Column("nzp_payer", DbType.Int32));
                }
                //перенос контрагентов 
                if (Database.ColumnExists(pack, "nzp_payer"))
                {
                    string delimetr = "";
                    if (Database.ProviderName == "PostgreSQL")
                    {
                        delimetr = ".";
                    }
                    if (Database.ProviderName == "Informix")
                    {
                        delimetr = ":";
                    }

                    Database.ExecuteNonQuery(
                            "UPDATE pack SET nzp_payer = nzp_supp WHERE nzp_supp in (SELECT nzp_payer FROM " + CentralKernel + delimetr + "s_payer)");

                }
                //обнуляем перенесенные данные
                if (Database.ColumnExists(pack, "nzp_supp"))
                {
                    Database.Update(pack, new string[] { "nzp_supp" }, new string[] { null },
                     " nzp_payer is not null");
                }
                //удаляем левые значения из nzp_supp
                if (Database.ColumnExists(pack, "nzp_supp"))
                {
                    string delimetr = "";
                    if (Database.ProviderName == "PostgreSQL")
                    {
                        delimetr = ".";
                    }
                    if (Database.ProviderName == "Informix")
                    {
                        delimetr = ":";
                    }

                    Database.ExecuteNonQuery(
                            "UPDATE pack SET nzp_supp = null WHERE nzp_supp not in (SELECT nzp_supp FROM " + CentralKernel + delimetr + "supplier)");
                }

                SetSchema(Bank.Kernel);
                SchemaQualifiedObjectName s_payer = new SchemaQualifiedObjectName();
                s_payer.Name = "s_payer";
                s_payer.Schema = CurrentSchema;

                SchemaQualifiedObjectName supplier = new SchemaQualifiedObjectName();
                supplier.Name = "supplier";
                supplier.Schema = CurrentSchema;

                //if (Database.ConstraintExists(pack, "fk_pack_nzp_payer"))
                //{
                //    Database.RemoveConstraint(pack, "fk_pack_nzp_payer");
                //}

                //if (Database.ConstraintExists(pack, "fk_pack_nzp_supp"))
                //{
                //    Database.RemoveConstraint(pack, "fk_pack_nzp_supp");
                //}

                if (Database.ColumnExists(supplier, "nzp_supp"))
                {
                    Database.ExecuteNonQuery("ALTER TABLE supplier ADD UNIQUE (nzp_supp) ");
                    //Database.Delete(supplier, " nzp_supp=0");
                    //Database.Insert(supplier, new string[] {"nzp_supp"}, new string[] {"0"});
                    int count = Database.ExecuteNonQuery("SELECT count(*) FROM " + supplier.Schema+Database.TableDelimiter+ supplier.Name + " where nzp_supp=0");

                    if (count == 0)
                    {
                        Database.Insert(supplier, new string[] { "nzp_supp" }, new string[] { "0" });
                    }
                }
                

                if (!Database.ConstraintExists(pack, "fk_pack_nzp_payer"))
                {
                    Database.AddForeignKey("fk_pack_nzp_payer", pack, "nzp_payer", s_payer, "nzp_payer");
                }

                if (!Database.ConstraintExists(pack, "fk_pack_nzp_supp"))
                {
                    Database.AddForeignKey("fk_pack_nzp_supp", pack, "nzp_supp", supplier, "nzp_supp");
                }
            }
        }
    }
}
