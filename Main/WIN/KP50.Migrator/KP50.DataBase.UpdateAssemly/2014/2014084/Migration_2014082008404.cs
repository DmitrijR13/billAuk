using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using KP50.DataBase.Migrator.Framework;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2014082008404, Migrator.Framework.DataBase.Web)]
    public class Migration_2014082008404_Web : Migration
    {
        public override void Apply()
        {
            SchemaQualifiedObjectName excel_utility = new SchemaQualifiedObjectName() { Name = "excel_utility", Schema = CurrentSchema };

            if (Database.TableExists(excel_utility))
            {
                if (Database.ProviderName == "PostgreSQL")
                {
                    if (!Database.IndexExists("ix_excel_utility_1", excel_utility))
                    {
                        string[] columns = { "is_shared", "nzp_user" };
                        Database.AddIndex("ix_excel_utility_1", false, excel_utility, columns);
                    }
                }
            }

        }
    }
}