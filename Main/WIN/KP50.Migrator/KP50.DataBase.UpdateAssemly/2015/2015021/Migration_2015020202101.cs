using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015021
{
    [Migration(2015020202101, Migrator.Framework.DataBase.CentralBank)]
    public class Migration_2015020202101_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);
            var s_bill_archive = new SchemaQualifiedObjectName { Name = "s_bill_archive", Schema = CurrentSchema };

            if (!Database.TableExists("s_bill_archive"))
            {
                Database.AddTable(s_bill_archive,
                    new Column("bill_id", DbType.Int32, ColumnProperty.NotNull | ColumnProperty.Identity),
                    new Column("kind", DbType.Int32, ColumnProperty.NotNull),
                    new Column("month", DbType.Int32),
                    new Column("year", DbType.Int32),
                    new Column("path", DbType.String),
                    new Column("print_date", DbType.DateTime, ColumnProperty.NotNull),
                    new Column("is_saved", DbType.Boolean),
                    new Column("is_pack", DbType.Boolean),
                    new Column("num_ls", DbType.Int32),
                    new Column("nzp_area", DbType.Int32),
                    new Column("nzp_geu", DbType.Int32),
                    new Column("point", DbType.String),
                    new Column("charged", DbType.Decimal),
                    new Column("paid", DbType.Decimal));
            }
        }

        public override void Revert()
        {
            var s_bill_archive = new SchemaQualifiedObjectName { Name = "s_bill_archive", Schema = CurrentSchema };
            SetSchema(Bank.Data);
            if (Database.TableExists("s_bill_archive"))
            {
                Database.RemoveTable(s_bill_archive);
            }
        }
    }
}
