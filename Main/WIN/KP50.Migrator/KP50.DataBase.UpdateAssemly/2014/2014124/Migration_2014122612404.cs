using System;
using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2014._2014124
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2014122612404, MigrateDataBase.Charge)]
    public class Migration_2014122612404_Charge : Migration
    {
        public override void Apply()
        {
            // ...
            if (Database.ProviderName == "PostgreSQL")
            {
                var reader = Database.ExecuteReader("select tablename from pg_tables where schemaname='" + CurrentSchema + "' and tablename like 'calc_gku%'");

                SchemaQualifiedObjectName calc_gku_xx = new SchemaQualifiedObjectName() { Schema = CurrentSchema };
                try
                {
                    while (reader.Read())
                    {
                        calc_gku_xx.Name = ((string)reader["tablename"]).Trim();
                        if (Database.TableExists(calc_gku_xx))
                        {
                            if (!Database.ColumnExists(calc_gku_xx, "up_kf"))
                            {
                                Database.AddColumn(calc_gku_xx,
                                    new Column("up_kf", DbType.Decimal.WithSize(15, 7), ColumnProperty.NotNull, 1));
                            }
                        }
                    }
                }
                finally { reader.Close(); }

            }

        }

        public override void Revert()
        {
        }
    }


}
