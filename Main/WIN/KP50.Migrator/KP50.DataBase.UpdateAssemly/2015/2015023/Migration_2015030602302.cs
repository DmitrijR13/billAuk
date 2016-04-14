using System;
using System.Data;
using System.IO;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2015030602302, MigrateDataBase.CentralBank)]
    public class Migration_2015030602302CentralBank : Migration
    {
        public override void Apply()
        {

            SetSchema(Bank.Data);

            var peni_debt = new SchemaQualifiedObjectName();
            peni_debt.Name = "peni_debt";
            peni_debt.Schema = CurrentSchema;
            if (Database.TableExists(peni_debt))
            {
                if (!Database.ColumnExists(peni_debt, "cnt_days_with_prm"))
                {
                    Database.CommandTimeout = 3000;
                    Database.AddColumn(peni_debt, new Column("cnt_days_with_prm", DbType.Int32, ColumnProperty.NotNull, "0"));
                    Database.CommandTimeout = 300;
                }

            }

        }

        public override void Revert()
        {
        }
    }
}


