using System.Data;
using System.Diagnostics;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2014._2014082
{

    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2014080708201, MigrateDataBase.LocalBank)]
    public class Migration_2014080708201_LocalBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);
            SchemaQualifiedObjectName kvar = new SchemaQualifiedObjectName()
            {
                Name = "kvar",
                Schema = CurrentSchema
            };

            if (!Database.IndexExists("ix_kvar1", kvar))
            {
                Database.AddIndex("ix_kvar1", false, kvar, new string[] { "nkvar", "nkvar_n" });
            }
            else
            {
                Database.RemoveIndex("ix_kvar1", kvar);
                Database.AddIndex("ix_kvar1", false, kvar, new string[] { "nkvar", "nkvar_n" });
            }

            if (!Database.IndexExists("ix_pkod", kvar))
            {
                Database.AddIndex("ix_pkod", false, kvar, new string[] { "pkod" });
            }
            else
            {
                Database.RemoveIndex("ix_pkod", kvar);
                Database.AddIndex("ix_pkod", false, kvar, new string[] { "pkod" });
            }

            if (!Database.IndexExists("x_typek", kvar))
            {
                Database.AddIndex("x_typek", false, kvar, new string[] { "typek" });
            }
            else
            {
                Database.RemoveIndex("x_typek", kvar);
                Database.AddIndex("x_typek", false, kvar, new string[] { "typek" });
            }

            if (!Database.IndexExists("ix101_2", kvar))
            {
                Database.AddIndex("ix101_2", false, kvar, new string[] { "nzp_area" });
            }
            else
            {
                Database.RemoveIndex("ix101_2", kvar);
                Database.AddIndex("ix101_2", false, kvar, new string[] { "nzp_area" });
            }

            if (!Database.IndexExists("ix101_3", kvar))
            {
                Database.AddIndex("ix101_3", false, kvar, new string[] { "nzp_geu" });
            }
            else
            {
                Database.RemoveIndex("ix101_3", kvar);
                Database.AddIndex("ix101_3", false, kvar, new string[] { "nzp_geu" });
            }

            if (!Database.IndexExists("ix161_2", kvar))
            {
                Database.AddIndex("ix161_2", false, kvar, new string[] { "nzp_dom" });
            }
            else
            {
                Database.RemoveIndex("ix161_2", kvar);
                Database.AddIndex("ix161_2", false, kvar, new string[] { "nzp_dom" });
            }

            if (!Database.IndexExists("ix161_9", kvar))
            {
                Database.AddIndex("ix161_9", false, kvar, new string[] { "num_ls" });
            }
            else
            {
                Database.RemoveIndex("ix161_9", kvar);
                Database.AddIndex("ix161_9", false, kvar, new string[] { "num_ls" });
            }

            if (!Database.IndexExists("ix207_23", kvar))
            {
                Database.AddIndex("ix207_23", false, kvar, new string[] { "ikvar" });
            }
            else
            {
                Database.RemoveIndex("ix207_23", kvar);
                Database.AddIndex("ix207_23", false, kvar, new string[] { "ikvar" });
            }

            if (!Database.IndexExists("ixz_kv01", kvar))
            {
                Database.AddIndex("ixz_kv01", false, kvar, new string[] { "nzp_dom", "num_ls" });
            }
            else
            {
                Database.RemoveIndex("ixz_kv01", kvar);
                Database.AddIndex("ixz_kv01", false, kvar, new string[] { "nzp_dom", "num_ls" });
            }

            if (!Database.IndexExists("kv_uch", kvar))
            {
                Database.AddIndex("kv_uch", false, kvar, new string[] { "uch" });
            }
            else
            {
                Database.RemoveIndex("kv_uch", kvar);
                Database.AddIndex("kv_uch", false, kvar, new string[] { "uch" });
            }

            if (!Database.IndexExists("ik2", kvar))
            {
                Database.AddIndex("ik2", false, kvar, new string[] { "nzp_kvar", "remark" });
            }
            else
            {
                Database.RemoveIndex("ik2", kvar);
                Database.AddIndex("ik2", false, kvar, new string[] { "nzp_kvar", "remark" });
            }

            if (!Database.IndexExists("ix202_1", kvar))
            {
                Database.AddIndex("ix202_1", true, kvar, new string[] { "nzp_kvar" });
            }
            else
            {
                Database.RemoveIndex("ix202_1", kvar);
                Database.AddIndex("ix202_1", true, kvar, new string[] { "nzp_kvar" });
            }
        }

        public override void Revert()
        {
            SetSchema(Bank.Data);
        }
    }
    
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2014080708201, MigrateDataBase.CentralBank)]
    public class Migration_2014080708201_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);
            SchemaQualifiedObjectName kvar = new SchemaQualifiedObjectName()
            {
                Name = "kvar",
                Schema = CurrentSchema
            };

            if (!Database.IndexExists("ix_kvar1", kvar))
            {
                Database.AddIndex("ix_kvar1", false, kvar, new string[] { "nkvar", "nkvar_n" });
            }
            else
            {
                Database.RemoveIndex("ix_kvar1", kvar);
                Database.AddIndex("ix_kvar1", false, kvar, new string[] { "nkvar", "nkvar_n" });
            }

            if (!Database.IndexExists("ix_pkod", kvar))
            {
                Database.AddIndex("ix_pkod", false, kvar, new string[] {"pkod"});
            }
            else
            {
                Database.RemoveIndex("ix_pkod", kvar);
                Database.AddIndex("ix_pkod", false, kvar, new string[] { "pkod" });
            }

            if (!Database.IndexExists("x_typek", kvar))
            {
                Database.AddIndex("x_typek", false, kvar, new string[] {"typek"});
            }
            else
            {
                Database.RemoveIndex("x_typek", kvar);
                Database.AddIndex("x_typek", false, kvar, new string[] { "typek" });
            }

            if (!Database.IndexExists("ix101_2", kvar))
            {
                Database.AddIndex("ix101_2", false, kvar, new string[] {"nzp_area"});
            }
            else
            {
                Database.RemoveIndex("ix101_2", kvar);
                Database.AddIndex("ix101_2", false, kvar, new string[] { "nzp_area" });
            }

            if (!Database.IndexExists("ix101_3", kvar))
            {
                Database.AddIndex("ix101_3", false, kvar, new string[] {"nzp_geu"});
            }
            else
            {
                Database.RemoveIndex("ix101_3", kvar);
                Database.AddIndex("ix101_3", false, kvar, new string[] { "nzp_geu" });
            }

            if (!Database.IndexExists("ix161_2", kvar))
            {
                Database.AddIndex("ix161_2", false, kvar, new string[] {"nzp_dom"});
            }
            else
            {
                Database.RemoveIndex("ix161_2", kvar);
                Database.AddIndex("ix161_2", false, kvar, new string[] { "nzp_dom" });
            }

            if (!Database.IndexExists("ix161_9", kvar))
            {
                Database.AddIndex("ix161_9", false, kvar, new string[] {"num_ls"});
            }
            else
            {
                Database.RemoveIndex("ix161_9", kvar);
                Database.AddIndex("ix161_9", false, kvar, new string[] { "num_ls" });
            }

            if (!Database.IndexExists("ix207_23", kvar))
            {
                Database.AddIndex("ix207_23", false, kvar, new string[] {"ikvar"});
            }
            else
            {
                Database.RemoveIndex("ix207_23", kvar);
                Database.AddIndex("ix207_23", false, kvar, new string[] { "ikvar" });
            }

            if (!Database.IndexExists("ixz_kv01", kvar))
            {
                Database.AddIndex("ixz_kv01", false, kvar, new string[] {"nzp_dom", "num_ls"});
            }
            else
            {
                Database.RemoveIndex("ixz_kv01", kvar);
                Database.AddIndex("ixz_kv01", false, kvar, new string[] { "nzp_dom", "num_ls" });
            }

            if (!Database.IndexExists("kv_uch", kvar))
            {
                Database.AddIndex("kv_uch", false, kvar, new string[] {"uch"});
            }
            else
            {
                Database.RemoveIndex("kv_uch", kvar);
                Database.AddIndex("kv_uch", false, kvar, new string[] { "uch" });
            }

            if (!Database.IndexExists("ik2", kvar))
            {
                Database.AddIndex("ik2", false, kvar, new string[] {"nzp_kvar", "remark"});
            }
            else
            {
                Database.RemoveIndex("ik2", kvar);
                Database.AddIndex("ik2", false, kvar, new string[] { "nzp_kvar", "remark" });
            }
        }

        public override void Revert()
        {
            SetSchema(Bank.Data);
        }
    }

}
