using System;
using System.Collections.Generic;
using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015064
{

    [Migration(2015090106401, MigrateDataBase.Web)]
    public class Migration_2015090106401_Web : Migration
    {
        public override void Apply()
        {
            var cache_tables = new SchemaQualifiedObjectName() {Name = "cache_tables", Schema = CurrentSchema};
            if (!Database.TableExists(cache_tables))
            {
                Database.AddTable(cache_tables,
                    new Column("id", DbType.Int64, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("table_name", DbType.String, ColumnProperty.NotNull),
                    new Column("number", DbType.Int32, ColumnProperty.NotNull),
                    new Column("nzp_user", DbType.Int32, ColumnProperty.NotNull),
                    new Column("type", DbType.Int32, ColumnProperty.NotNull),
                    new Column("created_on", DbType.DateTime, ColumnProperty.NotNull, "now()"));

                if (Database.TableExists(cache_tables))
                {
                    Database.ExecuteNonQuery(" INSERT INTO " + cache_tables.Name +
                                             " (nzp_user,number,table_name, created_on, type)" +
                                             " SELECT \"substring\"(table_name, 2 ,strpos(table_name, '_selectedls')-2)::int as nzp_user, " +
                                             " \"substring\"(table_name, strpos(table_name, '_selectedls')+length('_selectedls'),length(table_name))::int as number, " +
                                             " table_name, now() as created_on,1 as type " +
                                             " FROM information_schema.tables" +
                                             " WHERE table_name ILIKE '%selectedls%' AND table_schema='public' " +
                                             " ORDER BY 1,2");
                }

                if (!Database.IndexExists("ix1_" + cache_tables.Name, cache_tables))
                {
                    Database.AddIndex("ix1_" + cache_tables.Name, true, cache_tables, new[] {"id"});
                }
                if (!Database.IndexExists("ix2_" + cache_tables.Name, cache_tables))
                {
                    Database.AddIndex("ix2_" + cache_tables.Name, false, cache_tables, new[] {"nzp_user"});
                }
                if (!Database.IndexExists("ix3_" + cache_tables.Name, cache_tables))
                {
                    Database.AddIndex("ix3_" + cache_tables.Name, false, cache_tables, new[] {"created_on"});
                }
                if (!Database.IndexExists("ix4_" + cache_tables.Name, cache_tables))
                {
                    Database.AddIndex("ix4_" + cache_tables.Name, false, cache_tables, new[] {"number"});
                }
            }
        }

        public override void Revert()
        {
            // TODO: Downgrade Fins

        }
    }

}
