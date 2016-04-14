using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using KP50.DataBase.Migrator.Framework;

namespace KP50.DataBase.UpdateAssembly._2015._2015064
{
    [Migration(2015120106401, Migrator.Framework.DataBase.CentralBank | Migrator.Framework.DataBase.LocalBank)]
    public class Migration_2015120106401 : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            var prm_name = new SchemaQualifiedObjectName() { Name = "prm_name", Schema = CurrentSchema };
            var s_counts = new SchemaQualifiedObjectName() { Name = "s_counts", Schema = CurrentSchema };
            if (Database.TableExists(prm_name) 
                && Database.TableExists(s_counts)
                && Database.ColumnExists(prm_name, "is_day_uchet") 
                && Database.ColumnExists(prm_name, "is_day_uchet_enable"))
            {
                using (var reader = Database.ExecuteReader("SELECT nzp_prm_sred FROM " + GetFullTableName(s_counts)+ " WHERE nzp_prm_sred>0"))
                {
                    while (reader.Read())
                    {
                        Database.Update(prm_name, 
                            new[] {"is_day_uchet", "is_day_uchet_enable"}, new[] {"1", "1"},
                            " nzp_prm=" + reader["nzp_prm_sred"]);
                    }
                }
            }

        }

        private string GetFullTableName(SchemaQualifiedObjectName table)
        {
            return string.Format("{0}{1}{2}", table.Schema, Database.TableDelimiter, table.Name);
        }
    }
}
