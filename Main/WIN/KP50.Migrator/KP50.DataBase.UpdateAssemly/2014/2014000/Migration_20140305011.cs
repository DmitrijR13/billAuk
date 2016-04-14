using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(20140305011, MigrateDataBase.CentralBank)]
    public class Migration_20140305011_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);
            SchemaQualifiedObjectName fn_dogovor = new SchemaQualifiedObjectName() { Name = "fn_dogovor", Schema = CurrentSchema };
            if (Database.TableExists(fn_dogovor) && !Database.ColumnExists(fn_dogovor, "priznak_perechisl")) Database.AddColumn(fn_dogovor, new Column("priznak_perechisl", DbType.Int32));
            if (Database.TableExists(fn_dogovor) && !Database.ColumnExists(fn_dogovor, "min_sum")) Database.AddColumn(fn_dogovor, new Column("min_sum", DbType.Decimal.WithSize(14, 2)));
        }

        public override void Revert()
        {
            SetSchema(Bank.Data);
            SchemaQualifiedObjectName fn_dogovor = new SchemaQualifiedObjectName() { Name = "fn_dogovor", Schema = CurrentSchema };
            if (Database.TableExists(fn_dogovor) && Database.ColumnExists(fn_dogovor, "priznak_perechisl")) Database.RemoveColumn(fn_dogovor, "priznak_perechisl");
            if (Database.TableExists(fn_dogovor) && Database.ColumnExists(fn_dogovor, "min_sum")) Database.RemoveColumn(fn_dogovor, "min_sum");
        }
    }
}
