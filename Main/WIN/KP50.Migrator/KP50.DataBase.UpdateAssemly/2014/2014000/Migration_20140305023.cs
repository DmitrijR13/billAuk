using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(20140305023, MigrateDataBase.LocalBank)]
    public class Migration_20140305023_LocalBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);
            SchemaQualifiedObjectName kredit = new SchemaQualifiedObjectName() { Name = "kredit", Schema = CurrentSchema };
            if (Database.TableExists(kredit) && !Database.ColumnExists(kredit, "sum_real_p"))
                Database.AddColumn(kredit, new Column("sum_real_p", DbType.Decimal.WithSize(14, 2), ColumnProperty.None, 0.00));
        }

        public override void Revert()
        {
            SetSchema(Bank.Data);
            SchemaQualifiedObjectName kredit = new SchemaQualifiedObjectName() { Name = "kredit", Schema = CurrentSchema };
            if (Database.ColumnExists(kredit, "sum_real_p")) Database.RemoveColumn(kredit, "sum_real_p");
        }
    }
}
