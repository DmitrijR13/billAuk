using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2014100310102, MigrateDataBase.CentralBank)]
    public class Migration_2014100310102_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName kodsum = new SchemaQualifiedObjectName() { Name = "kodsum", Schema = CurrentSchema };

            if (Database.TableExists(kodsum))
            {
                Database.Delete(kodsum, "kod=49");
                Database.Insert(kodsum, new string[] { "kod", "comment", "cnt_shkodes", "is_id_bill" }, new string[] { "49", "платежи контрагентов", "1", "1" });
                Database.Update(kodsum, new string[] { "comment" }, new string[] { "платежи по договорам" }, "kod=50");
                Database.Update(kodsum, new string[] { "cnt_shkodes" }, new string[] { "1" }, "kod in (33,40,49,50)");
                Database.Update(kodsum, new string[] { "cnt_shkodes" }, new string[] { "0" }, "kod not in (33,40,49,50)");
            }
        }
    }
}
