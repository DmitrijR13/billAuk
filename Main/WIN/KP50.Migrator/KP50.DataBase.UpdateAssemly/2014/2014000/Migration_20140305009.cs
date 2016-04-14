using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(20140305009, MigrateDataBase.LocalBank)]
    public class Migration_20140305009_LocalBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);
            SchemaQualifiedObjectName kredit = new SchemaQualifiedObjectName() { Name = "kredit", Schema = CurrentSchema };
            if (!Database.TableExists(kredit))
            {
                Database.AddTable(kredit,
                    new Column("nzp_kredit", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("nzp_kvar", DbType.Int32, ColumnProperty.NotNull),
                    new Column("nzp_serv", DbType.Int32, ColumnProperty.NotNull),
                    new Column("dat_month", DbType.Date, ColumnProperty.NotNull),
                    new Column("dat_s", DbType.Date, ColumnProperty.NotNull),
                    new Column("dat_po", DbType.Date, ColumnProperty.NotNull),
                    new Column("valid", DbType.Int32, ColumnProperty.NotNull),
                    new Column("sum_dolg", DbType.Decimal.WithSize(14, 2), ColumnProperty.None, 0.00),
                    new Column("perc", DbType.Decimal.WithSize(5, 2), ColumnProperty.None, 0.00));
            }
        }

        public override void Revert()
        {
            SetSchema(Bank.Data);
            SchemaQualifiedObjectName kredit = new SchemaQualifiedObjectName() { Name = "kredit", Schema = CurrentSchema };
            if (Database.TableExists(kredit)) Database.RemoveTable(kredit);
        }
    }
}
