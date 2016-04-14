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
    [Migration(2014122312406, MigrateDataBase.Charge)]
    public class Migration_2014122312406_Charge : Migration
    {
        public override void Apply()
        {
            // ...
            if (Database.ProviderName == "PostgreSQL")
            {
                var reader = Database.ExecuteReader("SELECT tablename from "+
                    " (select tablename from pg_tables where schemaname='" + CurrentSchema + "' and tablename like 'counters%') b "+
                        " where tablename not SIMILAR to '%_dop|%_minus|%_ord|%_vals'");

                SchemaQualifiedObjectName counters_xx = new SchemaQualifiedObjectName() { Schema = CurrentSchema };
                try
                {
                    while (reader.Read())
                    {
                        counters_xx.Name = ((string)reader["tablename"]).Trim();
                        if (Database.TableExists(counters_xx))
                        {
                            if (!Database.ColumnExists(counters_xx, "dop87_source"))
                            {
                                Database.AddColumn(counters_xx,
                                    new Column("dop87_source", DbType.Decimal.WithSize(15, 7), ColumnProperty.NotNull,
                                        default(decimal)));
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
