using System;
using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015064
{
    [Migration(2015062906403, MigrateDataBase.CentralBank)]
    public class Migration_2015062906403_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);

            SchemaQualifiedObjectName supplier_codes = new SchemaQualifiedObjectName() { Name = "supplier_codes", Schema = CurrentSchema };
            if (Database.TableExists(supplier_codes) && !Database.ColumnExists(supplier_codes, "nzp_payer"))
                Database.AddColumn(supplier_codes, new Column("nzp_payer", DbType.Int32));
        }

    }
}
