using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015064
{
    [Migration(2015070206401, MigrateDataBase.LocalBank)]
    public class Migration_2015070206401_LocalBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            // TODO: Upgrade LocalPref_Kernel

            SetSchema(Bank.Data);
            SchemaQualifiedObjectName s_payer = new SchemaQualifiedObjectName() { Name = "s_payer", Schema = CentralKernel };
            SchemaQualifiedObjectName users = new SchemaQualifiedObjectName() { Name = "users", Schema = CentralData };
            SchemaQualifiedObjectName dom = new SchemaQualifiedObjectName() { Name = "dom", Schema = CurrentSchema };
            SchemaQualifiedObjectName alias_dom = new SchemaQualifiedObjectName() { Name = "alias_dom", Schema = CurrentSchema };

            if (!Database.TableExists(alias_dom))
            {
                Database.AddTable(alias_dom,
                    new Column("id", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("nzp_dom", DbType.Int32, ColumnProperty.NotNull),
                    new Column("kod_dom", DbType.String.WithSize(20), ColumnProperty.NotNull),
                    new Column("nzp_payer", DbType.Int32, ColumnProperty.NotNull),
                    new Column("comment", DbType.String.WithSize(100), ColumnProperty.NotNull),
                    new Column("nzp_user", DbType.Int32),
                    new Column("dat_when", DbType.DateTime, ColumnProperty.None, "now()"),
                    new Column("is_actual", DbType.Int32)
                    );

                if (Database.TableExists(alias_dom))
                {
                    Database.AddIndex("inx_alias_dom_1", true, alias_dom, new[] { "nzp_dom", "nzp_payer", "kod_dom" });
                    Database.AddIndex("inx_alias_dom_2", true, alias_dom, new[] { "id" });
                    Database.AddForeignKey("fk_alias_dom_nzp_dom", alias_dom, "nzp_dom", dom, "nzp_dom");
                    Database.AddForeignKey("fk_alias_dom_nzp_user", alias_dom, "nzp_user", users, "nzp_user");
                    Database.AddForeignKey("fk_alias_dom_nzp_payer", alias_dom, "nzp_payer", s_payer, "nzp_payer");
                }
            }


        }

        public override void Revert()
        {
            SetSchema(Bank.Kernel);
            // TODO: Downgrade LocalPref_Kernel

            SetSchema(Bank.Data);
            // TODO: Downgrade LocalPref_Data

        }
    }

}
