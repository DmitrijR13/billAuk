using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2014100710101, MigrateDataBase.Fin)]
    public class Migration_2014100710101_Fin : Migration
    {
        public override void Apply()
        {
            SchemaQualifiedObjectName pack_ls = new SchemaQualifiedObjectName() { Name = "pack_ls", Schema = CurrentSchema };
            if (Database.TableExists(pack_ls))
            {
                if (!Database.ColumnExists(pack_ls, "nzp_payer"))
                {
                    Database.AddColumn(pack_ls, new Column("nzp_payer", DbType.Int32));
                }
            }
        }
    }
}
