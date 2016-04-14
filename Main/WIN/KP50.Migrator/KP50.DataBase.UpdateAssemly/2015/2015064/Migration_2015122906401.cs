using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using KP50.DataBase.Migrator.Framework;

namespace KP50.DataBase.UpdateAssembly._2015._2015064
{
    [Migration(2015122906401, Migrator.Framework.DataBase.LocalBank)]
    public class Migration_2015122906401 : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);
            var counters_replaced = new SchemaQualifiedObjectName { Name = "counters_replaced", Schema = CurrentSchema };
            var counters_spis = new SchemaQualifiedObjectName { Name = "counters_spis", Schema = CurrentSchema };
            if (!Database.TableExists(counters_replaced))
            {
                Database.AddTable(counters_replaced,
                    new Column("id", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull | ColumnProperty.Unique),
                    new Column("nzp_counter_new", DbType.Int32, ColumnProperty.NotNull),
                    new Column("nzp_counter_old", DbType.Int32, ColumnProperty.NotNull),
                    new Column("is_actual", DbType.Boolean, ColumnProperty.NotNull, "true"),
                    new Column("created_on", DbType.DateTime, ColumnProperty.NotNull, "now()"),
                    new Column("created_by", DbType.Int32, ColumnProperty.NotNull),
                    new Column("changed_on", DbType.DateTime),
                    new Column("changed_by", DbType.Int32));
            }
            else
            {
                if (!Database.ColumnExists(counters_replaced, "is_actual"))
                {
                    Database.AddColumn(counters_replaced,
                        new Column("is_actual", DbType.Boolean, ColumnProperty.NotNull, "true"));
                }
                if (!Database.ColumnExists(counters_replaced, "created_on"))
                {
                    Database.AddColumn(counters_replaced,
                        new Column("created_on", DbType.DateTime, ColumnProperty.NotNull, "now()"));
                }
                if (!Database.ColumnExists(counters_replaced, "created_by"))
                {
                    Database.AddColumn(counters_replaced,
                        new Column("created_by", DbType.Int32, ColumnProperty.NotNull));
                }
                if (!Database.ColumnExists(counters_replaced, "changed_on"))
                {
                    Database.AddColumn(counters_replaced,
                        new Column("changed_on", DbType.DateTime));
                }
                if (!Database.ColumnExists(counters_replaced, "changed_by"))
                {
                    Database.AddColumn(counters_replaced,
                        new Column("changed_by", DbType.Int32));
                }
            }

            if (Database.IndexExists("ix_counters_replaced_2", counters_replaced))
            {
                Database.RemoveIndex("ix_counters_replaced_2", counters_replaced);
            }

            var indexName = "ix1" + counters_replaced.Name;
            if (!Database.IndexExists(indexName, counters_replaced))
            {
                Database.ExecuteNonQuery("CREATE INDEX " + indexName + " ON " + counters_replaced.Name +
                                         "(nzp_counter_old,nzp_counter_new) WHERE is_actual=true");
            }
            indexName = "ix2" + counters_replaced.Name;
            if (!Database.IndexExists(indexName, counters_replaced))
            {
                Database.AddIndex(indexName, false, counters_replaced, "nzp_counter_new");
            }
            var constraintName = "FK_nzp_counter1";
            if (!Database.ConstraintExists(counters_replaced, constraintName))
            {
                Database.AddForeignKey(constraintName, counters_replaced, new[] { "nzp_counter_old" },
                    counters_spis, new[] { "nzp_counter" });
            }
            constraintName = "FK_nzp_counter2";
            if (!Database.ConstraintExists(counters_replaced, constraintName))
            {
                Database.AddForeignKey(constraintName, counters_replaced, new[] { "nzp_counter_new" },
                    counters_spis, new[] { "nzp_counter" });
            }
        }
    }
}
