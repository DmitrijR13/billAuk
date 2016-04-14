using System;
using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015064
{
    [Migration(2015091106401, MigrateDataBase.CentralBank)]
    public class Migration_2015091106401_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            var servNormKoef = new SchemaQualifiedObjectName() { Name = "serv_norm_koef", Schema = CurrentSchema };

            if (!Database.TableExists(servNormKoef))
            {
                Database.AddTable(servNormKoef,
                    new Column("id", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("nzp_serv", DbType.Int32),
                    new Column("nzp_serv_link", DbType.Int32),
                    new Column("nzp_frm", DbType.Int32),
                    new Column("nzp_prm", DbType.Int32));
            }
        }

        public override void Revert()
        {
        }
    }
}
