using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using KP50.DataBase.Migrator.Framework;

namespace KP50.DataBase.UpdateAssembly._2015._2015064
{
    [Migration(2015122906403, Migrator.Framework.DataBase.LocalBank | Migrator.Framework.DataBase.CentralBank)]
    public class Migration_2015122906403 : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            var prm_name = new SchemaQualifiedObjectName() { Name = "prm_name", Schema = CurrentSchema };

            if (Database.TableExists(prm_name))
            {
                if (NotExistRecord(prm_name, " WHERE nzp_prm=1976"))
                {
                    Database.Insert(prm_name, new [] { "nzp_prm", "name_prm", "type_prm", "prm_num" },
                     new[] { "1976", "Включить генерацию средних расходов для закрытых ИПУ", "bool", "10" });
                }
                else
                {
                    Database.Update(prm_name, new[] {"nzp_prm", "name_prm", "type_prm", "prm_num"},
                        new[] { "1976", "Включить генерацию средних расходов для закрытых ИПУ", "bool", "10" },
                        "nzp_prm=1976");
                }
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
