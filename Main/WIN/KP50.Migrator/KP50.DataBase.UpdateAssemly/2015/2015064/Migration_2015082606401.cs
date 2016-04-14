using System;
using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015064
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2015082606401, MigrateDataBase.CentralBank)]
    public class Migration_2015082606401 : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName s_prov_types = new SchemaQualifiedObjectName();
            s_prov_types.Name = "s_prov_types";
            s_prov_types.Schema = CurrentSchema;
            if (Database.TableExists(s_prov_types) && NotExistRecord(s_prov_types, " WHERE id=9"))
            {
                Database.Insert(s_prov_types, new[] { "id", "type_prov", "sign_plus", "source_id", "source" },
                    new[] { "9", "Корректировки", "true", "nzp_charge", "charge_xx" });
            }
            if (Database.TableExists(s_prov_types))
            {
                Database.Update(s_prov_types, new[] { "source_id", "source" }, new[] { "nzp_reval", "reval_xx" }, "id=6");

            }
        }

        private bool NotExistRecord(SchemaQualifiedObjectName table, string where)
        {
            return Convert.ToInt32(
                Database.ExecuteScalar("SELECT count(1) FROM " + GetFullTableName(table) + " " + where)) ==
                   0;
        }
        private string GetFullTableName(SchemaQualifiedObjectName table)
        {
            return string.Format("{0}{1}{2}", table.Schema, Database.TableDelimiter, table.Name);
        }

    }
}
