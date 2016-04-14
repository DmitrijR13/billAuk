using System.Collections.Generic;
using System.Data;
using KP50.DataBase.Migrator.Framework;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2015032003303, Migrator.Framework.DataBase.CentralBank)]
    public class Migration_2015032003303 : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);
            var tulaFileReestr = new SchemaQualifiedObjectName() { Name = "tula_file_reestr", Schema = CurrentSchema };
            if (!Database.ColumnExists(tulaFileReestr, "nzp_serv"))
            {
                Database.AddColumn(tulaFileReestr, new Column("nzp_serv", DbType.Int32));
            }
        }
    }
}
