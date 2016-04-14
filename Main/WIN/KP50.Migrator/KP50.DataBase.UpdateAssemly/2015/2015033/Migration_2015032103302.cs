using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KP50.DataBase.Migrator.Framework;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2015032103302, Migrator.Framework.DataBase.CentralBank)]
    public class Migration_2015032103302 : Migration
    {
        public override void Apply()
        {
            var payer_types = new SchemaQualifiedObjectName { Name = "payer_types", Schema = CentralKernel };
            Dictionary<string, string> colsValues = new Dictionary<string, string> 
            {{"nzp_payer", "f.nzp_payer_agent"}, 
            {"nzp_payer_type", "5"}};
            if (Database.ColumnExists(payer_types, "changed_by"))
            {
                colsValues.Add("changed_by", "1");
            }
            if (Database.ColumnExists(payer_types, "changed_on"))
            {
                colsValues.Add("changed_on", "now()");
            }
            string columns = String.Join(",", colsValues.Keys.ToArray());
            string values = String.Join(",", colsValues.Values.ToArray());
            string sql = "insert into " + CentralKernel + Database.TableDelimiter + "payer_types (" + columns + ") " +
                         "select distinct " + values + " from " + CentralData + Database.TableDelimiter + "fn_dogovor f" +
                         " where not exists (select 1 from " + CentralKernel + Database.TableDelimiter + "payer_types pt where " +
                         " f.nzp_payer_agent=pt.nzp_payer and pt.nzp_payer_type=5 and pt.nzp_payer is not null) and f.nzp_payer_agent is not null ";
            Database.ExecuteNonQuery(sql);
        }
    }
}

