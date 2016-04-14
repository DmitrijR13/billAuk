using System;
using System.Data;
using KP50.DataBase.Migrator;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015064
{
    [Migration(2015122806402, MigrateDataBase.CentralBank)]
    public class Migration_2015122806402 : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            var peni_settings = new SchemaQualifiedObjectName() { Name = "peni_settings", Schema = CurrentSchema };
            if (!Database.TableExists(peni_settings))
            {
                Database.AddTable(peni_settings,
                    new Column("id", DbType.Int32, ColumnProperty.PrimaryKeyWithIdentity | ColumnProperty.NotNull),
                    new Column("nzp_serv", DbType.Int32, ColumnProperty.NotNull),
                    new Column("nzp_peni_serv", DbType.Int32, ColumnProperty.NotNull),
                    new Column("low_percent", DbType.Int32, ColumnProperty.NotNull),
                    new Column("middle_percent", DbType.Int32, ColumnProperty.NotNull),
                    new Column("high_percent", DbType.Int32, ColumnProperty.NotNull)
                );


                Database.Insert(peni_settings,
                    new[] { "nzp_serv", "nzp_peni_serv", "low_percent", "middle_percent", "high_percent" },
                    new[] { "1", "500", "2118", "85", "2119" });
                Database.Insert(peni_settings,
                    new[] { "nzp_serv", "nzp_peni_serv", "low_percent", "middle_percent", "high_percent" },
                    new[] {"206", "506", "85", "85", "85"});

                Database.AddIndex("ix1_" + peni_settings.Name, true, peni_settings, "id");
                Database.AddIndex("ix2_" + peni_settings.Name, false, peni_settings, "nzp_serv", "low_percent", "middle_percent", "high_percent");
            }
        }

    }
}
