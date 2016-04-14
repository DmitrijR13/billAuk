using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using KP50.DataBase.Migrator.Framework;

namespace KP50.DataBase.UpdateAssembly._2015._2015064
{
     [Migration(2015100306402, Migrator.Framework.DataBase.Web)]
    public class Migration_2015100306402:Migration
    {
         public override void Apply()
         {
             var cache_tables = new SchemaQualifiedObjectName() { Name = "cache_tables", Schema = CurrentSchema };
             if (Database.TableExists(cache_tables))
             {
                 if (!Database.IndexExists("ix1_" + cache_tables.Name, cache_tables))
                 {
                     Database.AddIndex("ix1_" + cache_tables.Name, true, cache_tables, new[] { "id" });
                 }
                 if (!Database.IndexExists("ix2_" + cache_tables.Name, cache_tables))
                 {
                     Database.AddIndex("ix2_" + cache_tables.Name, false, cache_tables, new[] { "nzp_user" });
                 }
                 if (!Database.IndexExists("ix3_" + cache_tables.Name, cache_tables))
                 {
                     Database.AddIndex("ix3_" + cache_tables.Name, false, cache_tables, new[] { "created_on" });
                 }
                 if (!Database.IndexExists("ix4_" + cache_tables.Name, cache_tables))
                 {
                     Database.AddIndex("ix4_" + cache_tables.Name, false, cache_tables, new[] { "number" });
                 }
             }
         }
    }
}
