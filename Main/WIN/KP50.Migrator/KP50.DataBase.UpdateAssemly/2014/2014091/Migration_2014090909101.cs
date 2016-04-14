using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2014._2014091
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2014090909101, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_2014090909101_CentralBank : Migration
    {
        public override void Apply()
        {

            SetSchema(Bank.Data);
            SchemaQualifiedObjectName s_area = new SchemaQualifiedObjectName() { Name = "s_area", Schema = CurrentSchema };
            if (Database.TableExists(s_area))
            {
                int count = Database.ExecuteNonQuery("SELECT count(*) FROM " + s_area);

                if (count == 0)
                {
                    Database.Insert(s_area, new string[] { "nzp_area", "area" }, new string[] { "1", "Нет территории" });
                }
            }
        }

        public override void Revert()
        {

            SetSchema(Bank.Data);

        }
    }
}
