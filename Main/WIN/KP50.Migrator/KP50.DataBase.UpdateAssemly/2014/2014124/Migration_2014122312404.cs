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
    [Migration(2014122312404, MigrateDataBase.Charge)]
    public class Migration_2014122312404_Charge : Migration
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
                            if (!Database.ColumnExists(counters_xx, "norm_type_id"))
                            {
                                Database.AddColumn(counters_xx,
                                    new Column("norm_type_id", DbType.Int32, ColumnProperty.None, default(int)));
                            }
                            if (!Database.ColumnExists(counters_xx, "norm_tables_id"))
                            {
                                Database.AddColumn(counters_xx,
                                    new Column("norm_tables_id", DbType.Int32, ColumnProperty.None, default(int)));
                            }
                            if (!Database.ColumnExists(counters_xx, "val1_source"))
                            {
                                Database.AddColumn(counters_xx,
                                    new Column("val1_source", DbType.Decimal.WithSize(15, 7), ColumnProperty.NotNull,
                                        default(decimal)));
                            }
                            if (!Database.ColumnExists(counters_xx, "val4_source"))
                            {
                                Database.AddColumn(counters_xx,
                                    new Column("val4_source", DbType.Decimal.WithSize(15, 7), ColumnProperty.NotNull,
                                        default(decimal)));
                            }
                            if (!Database.ColumnExists(counters_xx, "up_kf"))
                            {
                                Database.AddColumn(counters_xx,
                                    new Column("up_kf", DbType.Decimal.WithSize(15, 7), ColumnProperty.NotNull,
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
