using System.Collections.Generic;
using System.Data;
using KP50.DataBase.Migrator.Framework;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2015032003306, Migrator.Framework.DataBase.CentralBank)]
    public class Migration_2015032003306 : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);
            var tulaReestrUnloads = new SchemaQualifiedObjectName() { Name = "tula_reestr_unloads", Schema = CurrentSchema };
            if (Database.ColumnExists(tulaReestrUnloads, "name_file"))
            {
                Database.ChangeColumn(tulaReestrUnloads, "name_file", DbType.String.WithSize(100), false);
            }
        }
    }
}
