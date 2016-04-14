using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
     [Migration(2014062706302, MigrateDataBase.CentralBank)]
    public class Migration_2014062706302 : Migration
    {
         public override void Apply()
         {
             SetSchema(Bank.Kernel);
             SchemaQualifiedObjectName s_reg_prm = new SchemaQualifiedObjectName() { Name = "s_reg_prm", Schema = CurrentSchema };
             Database.Delete(s_reg_prm, "nzp_reg = 5 and nzp_prm in (7,463,2007,38)");
             Database.Insert(s_reg_prm, new string[] { "nzp_reg", "nzp_prm", "numer", "is_show" }, new string[] { "5", "7", "0", "0" });
             Database.Insert(s_reg_prm, new string[] { "nzp_reg", "nzp_prm", "numer", "is_show" }, new string[] { "5", "463", "0", "0" });
             Database.Insert(s_reg_prm, new string[] { "nzp_reg", "nzp_prm", "numer", "is_show" }, new string[] { "5", "2007", "0", "0" });
             Database.Insert(s_reg_prm, new string[] { "nzp_reg", "nzp_prm", "numer", "is_show" }, new string[] { "5", "38", "0", "0" });
         }
    }
}
