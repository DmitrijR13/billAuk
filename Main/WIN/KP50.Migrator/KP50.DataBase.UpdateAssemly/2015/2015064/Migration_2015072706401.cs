using System.Data;
using System;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015064
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2015072706401, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_2015072706401 : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            var res_y = new SchemaQualifiedObjectName() { Name = "res_y", Schema = CurrentSchema };
            if (Database.TableExists(res_y))
            {
                Database.Update(res_y, new[] { "name_y" },
                 new[] { "Формула 13 Постановления №354" }, "nzp_res=3008 and nzp_y=3");
            }
        }

        private bool NotExistRecord(SchemaQualifiedObjectName table, string where)
        {
            return Convert.ToInt32(
                Database.ExecuteScalar("SELECT count(*) FROM " + GetFullTableName(table) + " " + where)) ==
                   0;
        }
        private string GetFullTableName(SchemaQualifiedObjectName table)
        {
            return string.Format("{0}{1}{2}", table.Schema, Database.TableDelimiter, table.Name);
        }

    }

}
