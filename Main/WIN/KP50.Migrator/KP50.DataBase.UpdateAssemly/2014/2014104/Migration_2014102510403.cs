using System.Collections.Generic;
using System.Data;
using KP50.DataBase.Migrator.Framework;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2014102510403, Migrator.Framework.DataBase.CentralBank)]
    public class Migration_2014102510403 : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Debt);
            var lawsuit = new SchemaQualifiedObjectName() { Name = "lawsuit", Schema = CurrentSchema };
            if (!Database.ColumnExists(lawsuit,"il_date"))
            {
                Database.AddColumn(lawsuit, new Column("il_date", DbType.Date));
            }
        }
    }
}
