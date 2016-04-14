using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using KP50.DataBase.Migrator.Framework;

namespace KP50.DataBase.UpdateAssembly
{
     [Migration(2014082708502, Migrator.Framework.DataBase.Charge)]
    public class Migration_2014082708502_Charge : Migration
    {
        public override void Apply()
        {
            IDataReader reader;
            // ...
            if (Database.ProviderName == "PostgreSQL")
            {
                reader = Database.ExecuteReader("select tablename from pg_tables where schemaname='" + CurrentSchema +
                                                "' and tablename like 'kvar_calc%' order by 1");

                try
                {
                    while (reader.Read())
                    {
                        string stbl = (string) reader["tablename"];

                        SchemaQualifiedObjectName kvar_calc_xx = new SchemaQualifiedObjectName()
                        {
                            Name = stbl,
                            Schema = CurrentSchema
                        };

                        if (Database.TableExists(kvar_calc_xx))
                        {
                            if (Database.ColumnExists(kvar_calc_xx, "dat_calc"))
                            {
                                Database.ChangeColumn(kvar_calc_xx, "dat_calc", new ColumnType(DbType.DateTime), true);
                            }
                        }
                    }
                }
                finally
                {
                         reader.Close(); 
                }
            }
        }
    }
}
