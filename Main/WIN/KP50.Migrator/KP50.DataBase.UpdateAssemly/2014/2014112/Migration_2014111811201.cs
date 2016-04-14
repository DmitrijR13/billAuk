using System;
using System.Data;
using KP50.DataBase.Migrator.Framework;
using System.Collections.Generic;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2014111811201, MigrateDataBase.Charge)]
    public class Migration_2014111811201_Charge : Migration
    {
        public override void Apply() {
            IDataReader reader;
            // ...
            if (Database.ProviderName == "PostgreSQL")
            {
                reader = Database.ExecuteReader("select tablename from pg_tables where schemaname='" + CurrentSchema +
                                                "' and tablename like 'gil%' order by 1");

                try
                {
                    while (reader.Read())
                    {
                        string stbl = (string)reader["tablename"];

                        SchemaQualifiedObjectName gil_xx = new SchemaQualifiedObjectName()
                        {
                            Name = stbl,
                            Schema = CurrentSchema
                        };
                        if (Database.TableExists(gil_xx))
                        {
                            if (!Database.ColumnExists(gil_xx, "val6"))
                            {
                                Database.AddColumn(gil_xx, new Column("val6", DbType.Decimal.WithSize(11, 7), ColumnProperty.NotNull, 0.0000000));
                            }
                        }
                    }
                }
                finally { reader.Close(); }

            }
            // ...
        }

        public override void Revert() {
            // toDoList
        }
    }

   
}
