using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;


namespace KP50.DataBase.UpdateAssembly._2014._2014111
{
    [Migration(2014110711103, MigrateDataBase.CentralBank)]
    public class Migration_2014110711103_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Supg);
            SchemaQualifiedObjectName s_zvktype = new SchemaQualifiedObjectName()
            {
                Name = "s_zvktype ",
                Schema = CurrentSchema
            };

            if (Database.TableExists(s_zvktype))
            {
                Database.Delete(s_zvktype, "nzp_ztype = 99");
                Database.Insert(
                    s_zvktype,
                    new[] {"nzp_ztype", "zvk_type"},
                    new[] {"99", "Сообщения с порталов"});
            }
        }
    }
}
