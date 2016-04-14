using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(20140305014, MigrateDataBase.Fin)]
    public class Migration_20140305014_Fin : Migration
    {
        public override void Apply()
        {
            SchemaQualifiedObjectName fn_sended_dom = new SchemaQualifiedObjectName() { Name = "fn_sended_dom", Schema = CurrentSchema };
            if (Database.ColumnExists(fn_sended_dom, "nzp_send"))
                Database.ChangeColumn(fn_sended_dom, "nzp_send", DbType.Int32, true);
        }
    }
}
