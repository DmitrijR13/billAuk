using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015011
{
    [Migration(2015012301102, MigrateDataBase.CentralBank)]
    public class Migration_2015012301102_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            var services = new SchemaQualifiedObjectName() { Name = "services", Schema = CurrentSchema };
            var supplier = new SchemaQualifiedObjectName() { Name = "supplier", Schema = CurrentSchema };

            SetSchema(Bank.Data);
            var prohibited_recalc = new SchemaQualifiedObjectName() { Name = "prohibited_recalc", Schema = CurrentSchema };
            var dom = new SchemaQualifiedObjectName() { Name = "dom", Schema = CurrentSchema };
            var kvar = new SchemaQualifiedObjectName() { Name = "kvar", Schema = CurrentSchema };

            if (!Database.TableExists("prohibited_recalc"))
            {
                Database.AddTable(prohibited_recalc,
                    new Column("id", DbType.Int32, ColumnProperty.NotNull | ColumnProperty.Identity),
                    new Column("nzp_kvar", DbType.Int32, ColumnProperty.NotNull),
                    new Column("nzp_dom", DbType.Int32, ColumnProperty.NotNull),
                    new Column("nzp_serv", DbType.Int32, ColumnProperty.NotNull),
                    new Column("dat_s", DbType.DateTime, ColumnProperty.NotNull),
                    new Column("nzp_supp", DbType.Int32, ColumnProperty.NotNull),
                    new Column("dat_po", DbType.DateTime, ColumnProperty.NotNull),
                    new Column("is_actual", DbType.Int32, ColumnProperty.NotNull));

                Database.AddPrimaryKey("prohibited_recalc_unique_key", prohibited_recalc, "id");
            }
        }

        public override void Revert()
        {
        }
    }
}

