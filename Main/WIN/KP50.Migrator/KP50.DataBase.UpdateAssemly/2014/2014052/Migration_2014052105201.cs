using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2014052105201, MigrateDataBase.Fin)]
    public class Migration_20140521052_Fin : Migration
    {
        public override void Apply()
        {
            for (int i = 1; i <= 12; i++)
            {
                // TODO: Upgrade Fins
                SchemaQualifiedObjectName upload_pu = new SchemaQualifiedObjectName() { Name = "upload_pu_" + i.ToString("00"), Schema = CurrentSchema };
                if (Database.TableExists(upload_pu))
                {
                    if (!Database.ColumnExists(upload_pu, "subpkod"))
                        Database.AddColumn(upload_pu, new Column("subpkod", DbType.StringFixedLength.WithSize(13)));
                }
            }
        }
    }
}
