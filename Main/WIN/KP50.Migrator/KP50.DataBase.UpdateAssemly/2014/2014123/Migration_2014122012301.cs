using System;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;
using System.Data;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2014122312301, MigrateDataBase.Web)]
    public class Migration_2014122312301_CentralBank: Migration
    {
        public override void Apply()
        {
            SchemaQualifiedObjectName saldo_fon = new SchemaQualifiedObjectName()
            {
                Name = "saldo_fon",
                Schema = CurrentSchema
            };

            if (Database.TableExists(saldo_fon))
            {
                if (!Database.ColumnExists(saldo_fon, "dat_when"))
                {
                    Database.AddColumn(saldo_fon, new Column("dat_when", DbType.DateTime));
                }
            }

        }
    }
}
