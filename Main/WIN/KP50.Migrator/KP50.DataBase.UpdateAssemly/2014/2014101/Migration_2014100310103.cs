using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2014100310103, MigrateDataBase.Charge)]
    public class Migration_2014100310103_Charge : Migration
    {
        public override void Apply()
        {
            SchemaQualifiedObjectName from_supplier = new SchemaQualifiedObjectName() { Name = "from_supplier", Schema = CurrentSchema };
            SchemaQualifiedObjectName pack_ls = new SchemaQualifiedObjectName()
            {
                Name = "pack_ls",
                Schema = string.Format("{0}_fin_{1}", CentralPrefix,
                CurrentSchema.Replace(string.Format("{0}_charge_", CurrentPrefix), ""))
            };

            if (Database.TableExists(from_supplier) && Database.TableExists(pack_ls))
                Database.ExecuteNonQuery(Database.FormatSql(
                    "UPDATE {1:NAME} SET kod_sum=(SELECT kod_sum FROM {0:NAME} WHERE {1:NAME}.nzp_pack_ls = {0:NAME}.nzp_pack_ls) WHERE kod_sum = 50",
                    pack_ls, from_supplier));
        }
    }
}
