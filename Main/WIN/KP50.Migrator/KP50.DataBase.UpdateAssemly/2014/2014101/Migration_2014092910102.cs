using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2014092910102, MigrateDataBase.CentralBank)]
    public class Migration_2014092910102_CentralBank: Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName s_reg = new SchemaQualifiedObjectName() { Name = "s_reg", Schema = CurrentSchema };
            Database.Delete(s_reg, "nzp_reg = 7");//Показывать банк в групповых операциях
            Database.Insert(s_reg,
                new string[] { "nzp_reg", "name_reg" },
                new string[] { "7", "Показывать банк в групповых операциях" });

            SchemaQualifiedObjectName s_reg_prm = new SchemaQualifiedObjectName() { Name = "s_reg_prm", Schema = CurrentSchema };
            Database.Delete(s_reg_prm, "nzp_reg = 7 and nzp_prm in (7,463,2007,38)");
            Database.Insert(s_reg_prm, new string[] { "nzp_reg", "nzp_prm", "numer", "is_show" }, new string[] { "7", "7", "0", "0" });
            Database.Insert(s_reg_prm, new string[] { "nzp_reg", "nzp_prm", "numer", "is_show" }, new string[] { "7", "463", "0", "0" });
            Database.Insert(s_reg_prm, new string[] { "nzp_reg", "nzp_prm", "numer", "is_show" }, new string[] { "7", "2007", "0", "0" });
            Database.Insert(s_reg_prm, new string[] { "nzp_reg", "nzp_prm", "numer", "is_show" }, new string[] { "7", "38", "0", "0" });
        }
    }
}
