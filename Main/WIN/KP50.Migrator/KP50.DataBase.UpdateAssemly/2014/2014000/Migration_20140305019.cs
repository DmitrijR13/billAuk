using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(20140305019, MigrateDataBase.CentralBank)]
    public class Migration_20140305019_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);
            SchemaQualifiedObjectName tula_reestr_unloads = new SchemaQualifiedObjectName() { Name = "tula_reestr_unloads", Schema = CurrentSchema };
            if (Database.TableExists(tula_reestr_unloads) && Database.ColumnExists(tula_reestr_unloads, "unloading_date"))
                Database.ChangeColumn(tula_reestr_unloads, "unloading_date", DbType.String.WithSize(255), false);
        }
    }

    [Migration(20140305019, MigrateDataBase.LocalBank)]
    public class Migration_20140305019_LocalBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);
            SchemaQualifiedObjectName supplier_codes = new SchemaQualifiedObjectName() { Name = "supplier_codes", Schema = CurrentSchema };
            if (!Database.TableExists(supplier_codes))
                Database.AddTable(supplier_codes,
                    new Column("nzp_sc", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("nzp_kvar", DbType.Int32),
                    new Column("nzp_supp", DbType.Int32),
                    new Column("kod_geu", DbType.Int32),
                    new Column("pkod10", DbType.Int32, ColumnProperty.None, 0),
                    new Column("pkod_supp", DbType.Decimal.WithSize(13), ColumnProperty.NotNull, 0.0000000000000000));

            SchemaQualifiedObjectName must_calc = new SchemaQualifiedObjectName() { Name = "must_calc", Schema = CurrentSchema };
            if (Database.TableExists(must_calc) && !Database.ColumnExists(must_calc, "comment"))
                Database.AddColumn(must_calc, new Column("comment", DbType.String.WithSize(1000)));
        }

        public override void Revert()
        {
            SetSchema(Bank.Data);
            SchemaQualifiedObjectName supplier_codes = new SchemaQualifiedObjectName() { Name = "supplier_codes", Schema = CurrentSchema };
            if (Database.TableExists(supplier_codes)) Database.RemoveTable(supplier_codes);
            SchemaQualifiedObjectName must_calc = new SchemaQualifiedObjectName() { Name = "must_calc", Schema = CurrentSchema };
            if (Database.TableExists(must_calc) && Database.ColumnExists(must_calc, "comment")) Database.RemoveColumn(must_calc, "comment");
        }
    }
}
