using KP50.DataBase.Migrator.Framework;
using System.Data;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2014091509301, MigrateDataBase.CentralBank)]
    public class Migration_2014091509301_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName s_reg = new SchemaQualifiedObjectName() { Name = "s_reg", Schema = CurrentSchema };
            Database.Delete(s_reg, "nzp_reg = 5");//Параметры группового ПУ
            Database.Delete(s_reg, "nzp_reg = 6");//Параметры общедомового ПУ

            Database.Insert(s_reg,
                new string[] { "nzp_reg", "name_reg" },
                new string[] { "5", "Параметры группового ПУ" });

            Database.Insert(s_reg,
                new string[] { "nzp_reg", "name_reg" },
                new string[] { "6", "Параметры общедомового ПУ" });

            SchemaQualifiedObjectName s_reg_prm = new SchemaQualifiedObjectName() { Name = "s_reg_prm", Schema = CurrentSchema };
            Database.Delete(s_reg_prm, "nzp_reg = 5");
            Database.Insert(s_reg_prm, new string[] { "nzp_reg", "nzp_prm", "nzp_serv", "numer", "is_show" }, new string[] { "5", "979", "0", "1", "1" });
            Database.Insert(s_reg_prm, new string[] { "nzp_reg", "nzp_prm", "nzp_serv", "numer", "is_show" }, new string[] { "5", "2024", "0", "2", "1" });
            Database.Insert(s_reg_prm, new string[] { "nzp_reg", "nzp_prm", "nzp_serv", "numer", "is_show" }, new string[] { "5", "2025", "0", "3", "1" });
            Database.Insert(s_reg_prm, new string[] { "nzp_reg", "nzp_prm", "nzp_serv", "numer", "is_show" }, new string[] { "5", "2026", "0", "4", "1" });
            Database.Insert(s_reg_prm, new string[] { "nzp_reg", "nzp_prm", "nzp_serv", "numer", "is_show" }, new string[] { "5", "2027", "0", "5", "1" });
            Database.Insert(s_reg_prm, new string[] { "nzp_reg", "nzp_prm", "nzp_serv", "numer", "is_show" }, new string[] { "5", "974", "0", "6", "1" });
            Database.Insert(s_reg_prm, new string[] { "nzp_reg", "nzp_prm", "nzp_serv", "numer", "is_show" }, new string[] { "5", "2068", "0", "7", "1" });
            Database.Insert(s_reg_prm, new string[] { "nzp_reg", "nzp_prm", "nzp_serv", "numer", "is_show" }, new string[] { "5", "1152", "0", "8", "1" });
            Database.Insert(s_reg_prm, new string[] { "nzp_reg", "nzp_prm", "nzp_serv", "numer", "is_show" }, new string[] { "5", "1157", "0", "9", "1" });
            Database.Insert(s_reg_prm, new string[] { "nzp_reg", "nzp_prm", "nzp_serv", "numer", "is_show" }, new string[] { "5", "2471", "0", "10", "1" });
            Database.Insert(s_reg_prm, new string[] { "nzp_reg", "nzp_prm", "nzp_serv", "numer", "is_show" }, new string[] { "5", "2472", "0", "11", "1" });
            Database.Insert(s_reg_prm, new string[] { "nzp_reg", "nzp_prm", "nzp_serv", "numer", "is_show" }, new string[] { "5", "2473", "0", "12", "1" });

            Database.Delete(s_reg_prm, "nzp_reg = 6");
            Database.Insert(s_reg_prm, new string[] { "nzp_reg", "nzp_prm", "nzp_serv", "numer", "is_show" }, new string[] { "6", "979", "0", "1", "1" });
            Database.Insert(s_reg_prm, new string[] { "nzp_reg", "nzp_prm", "nzp_serv", "numer", "is_show" }, new string[] { "6", "2024", "0", "2", "1" });
            Database.Insert(s_reg_prm, new string[] { "nzp_reg", "nzp_prm", "nzp_serv", "numer", "is_show" }, new string[] { "6", "2025", "0", "3", "1" });
            Database.Insert(s_reg_prm, new string[] { "nzp_reg", "nzp_prm", "nzp_serv", "numer", "is_show" }, new string[] { "6", "2026", "0", "4", "1" });
            Database.Insert(s_reg_prm, new string[] { "nzp_reg", "nzp_prm", "nzp_serv", "numer", "is_show" }, new string[] { "6", "2027", "0", "5", "1" });
            Database.Insert(s_reg_prm, new string[] { "nzp_reg", "nzp_prm", "nzp_serv", "numer", "is_show" }, new string[] { "6", "974", "0", "6", "1" });
            Database.Insert(s_reg_prm, new string[] { "nzp_reg", "nzp_prm", "nzp_serv", "numer", "is_show" }, new string[] { "6", "2068", "0", "7", "1" });

            Database.Delete(s_reg_prm, "nzp_reg = 4");
            Database.Insert(s_reg_prm, new string[] { "nzp_reg", "nzp_prm", "nzp_serv", "numer", "is_show" }, new string[] { "4", "979", "0", "1", "1" });
            Database.Insert(s_reg_prm, new string[] { "nzp_reg", "nzp_prm", "nzp_serv", "numer", "is_show" }, new string[] { "4", "2024", "0", "2", "1" });
            Database.Insert(s_reg_prm, new string[] { "nzp_reg", "nzp_prm", "nzp_serv", "numer", "is_show" }, new string[] { "4", "2025", "0", "3", "1" });
            Database.Insert(s_reg_prm, new string[] { "nzp_reg", "nzp_prm", "nzp_serv", "numer", "is_show" }, new string[] { "4", "2026", "0", "4", "1" });
            Database.Insert(s_reg_prm, new string[] { "nzp_reg", "nzp_prm", "nzp_serv", "numer", "is_show" }, new string[] { "4", "2027", "0", "5", "1" });
            Database.Insert(s_reg_prm, new string[] { "nzp_reg", "nzp_prm", "nzp_serv", "numer", "is_show" }, new string[] { "4", "974", "0", "6", "1" });
        }
    }
}
