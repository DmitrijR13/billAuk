﻿using KP50.DataBase.Migrator.Framework;
using System.Collections.Generic;
using System.Data;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015064
{
    [Migration(2016041906401, MigrateDataBase.LocalBank)]
    public class Migration_2016041906401 : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);
            Database.CommandTimeout = 6000;
            var peni_debt = new SchemaQualifiedObjectName() { Name = "peni_debt", Schema = CurrentSchema };
            var peni_calc = new SchemaQualifiedObjectName() { Name = "peni_calc", Schema = CurrentSchema };
            if (Database.TableExists(peni_debt))
            {
                var columns = new List<string> { "sum_debt", "over_payments", "sum_debt_result", "sum_peni" };
                foreach (var column in columns)
                {
                    Database.ExecuteNonQuery(string.Format("ALTER TABLE {0} ALTER COLUMN {1} TYPE NUMERIC ", peni_debt, column));
                }
                if (!Database.ColumnExists(peni_debt, "peni_rate"))
                {
                    Database.AddColumn(peni_debt, new Column("peni_rate", DbType.Decimal.WithSize(8, 5), ColumnProperty.None, 0));
                }
            }

            if (Database.TableExists(peni_calc))
            {
                var columns = new List<string> { "sum_peni", "sum_old_reval", "sum_new_reval" };
                foreach (var column in columns)
                {
                    Database.ExecuteNonQuery(string.Format("ALTER TABLE {0} ALTER COLUMN {1} TYPE NUMERIC ", peni_calc, column));
                }
            }
        }
    }
}


