using System;
using KP50.DataBase.Migrator.Framework;
using System.Data;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2014092209403, MigrateDataBase.CentralBank)]
    public class Migration_2014092209303_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);

            SchemaQualifiedObjectName kvar_factura_msg = new SchemaQualifiedObjectName()
            {
                Name = "kvar_factura_msg",
                Schema = CurrentSchema
            };
            if (!Database.TableExists(kvar_factura_msg))
            {
                Database.AddTable(kvar_factura_msg,
                    new Column("nzp_kvar", DbType.Int32, ColumnProperty.NotNull),
                    new Column("period", DbType.Date, ColumnProperty.NotNull),
                    new Column("msg", DbType.String.WithSize(8000)));
            }

            string ix_name = "ix_kvar_factura_msg";
            if (!Database.IndexExists(ix_name, kvar_factura_msg))
            {
                Database.AddIndex(ix_name, false, kvar_factura_msg, "nzp_kvar", "period");
            }

        }

        public override void Revert()
        {
            SetSchema(Bank.Data);
            // TODO: Downgrade CentralPref_Data
        }
    }
}
