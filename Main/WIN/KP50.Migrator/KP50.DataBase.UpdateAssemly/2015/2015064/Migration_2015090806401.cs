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
    [Migration(2015090806401, MigrateDataBase.CentralBank)]
    public class Migration_2015090806401_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            var calc_method = new SchemaQualifiedObjectName() { Name = "calc_method", Schema = CurrentSchema };

            if (Database.TableExists(calc_method))
            {
                if (NotExistRecord(calc_method, " WHERE nzp_calc_method=7"))
                {
                    Database.Insert(calc_method,
                        new[] {"nzp_calc_method", "method_name"},
                        new[] {"7", "по жилой площади"});
                }
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
