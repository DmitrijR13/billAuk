using System;
using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015064
{
    [Migration(2015062706401, MigrateDataBase.CentralBank)]
    public class Migration_2015062706401_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);

            SchemaQualifiedObjectName overpaymentman_status = new SchemaQualifiedObjectName() { Name = "overpaymentman_status", Schema = CurrentSchema };
            if (Database.TableExists(overpaymentman_status) && !Database.ColumnExists(overpaymentman_status, "is_interrupted"))
                Database.AddColumn(overpaymentman_status, new Column("is_interrupted", DbType.Boolean));
        }

    }
}
