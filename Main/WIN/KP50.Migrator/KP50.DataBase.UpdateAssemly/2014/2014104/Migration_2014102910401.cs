using System.Collections.Generic;
using System.Data;
using KP50.DataBase.Migrator.Framework;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2014102910401, Migrator.Framework.DataBase.LocalBank)]
    public class Migration_2014102910401:Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);
            SchemaQualifiedObjectName gil_periods = new SchemaQualifiedObjectName() { Name = "gil_periods", Schema = CurrentSchema };
            if (Database.TableExists(gil_periods))
            {
                if (Database.ColumnExists(gil_periods, "no_podtv_docs"))
                {
                    Database.ExecuteNonQuery(string.Format("alter table {0}{1}{2} alter column no_podtv_docs set default 0 ", CurrentSchema, Database.TableDelimiter, gil_periods.Name));
                    Database.ExecuteNonQuery(string.Format("update {0}{1}{2} set no_podtv_docs = 0 where  no_podtv_docs is null", CurrentSchema, Database.TableDelimiter, gil_periods.Name));
                }
            }
        }
    }
}
