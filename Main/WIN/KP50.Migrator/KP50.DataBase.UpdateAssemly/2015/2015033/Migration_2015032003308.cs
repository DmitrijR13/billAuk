using System.Collections.Generic;
using System.Data;
using KP50.DataBase.Migrator.Framework;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2015032003308, Migrator.Framework.DataBase.CentralBank)]
    public class Migration_2015032003308 : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);
            var documentBase = new SchemaQualifiedObjectName() { Name = "document_base", Schema = CurrentSchema };
            if (Database.ColumnExists(documentBase, "comment"))
            {
                Database.ChangeColumn(documentBase, "comment", DbType.String.WithSize(250), false);
            }
        }
    }
}
