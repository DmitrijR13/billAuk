using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2014061806303, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_2014061806303_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);
            SchemaQualifiedObjectName s_area = new SchemaQualifiedObjectName() { Name = "s_area", Schema = CurrentSchema };
            SchemaQualifiedObjectName s_geu = new SchemaQualifiedObjectName() { Name = "s_geu", Schema = CurrentSchema };
            SchemaQualifiedObjectName kvar = new SchemaQualifiedObjectName() { Name = "kvar", Schema = CurrentSchema };
            SchemaQualifiedObjectName dom = new SchemaQualifiedObjectName() { Name = "dom", Schema = CurrentSchema };

            if (!Database.ConstraintExists(kvar, "fk_kvar_nzp_area"))
            {
                Database.AddForeignKey("fk_kvar_nzp_area", kvar, "nzp_area", s_area, "nzp_area");
            }

            if (!Database.ConstraintExists(kvar, "fk_kvar_nzp_geu"))
            {
                Database.AddForeignKey("fk_kvar_nzp_geu", kvar, "nzp_geu", s_geu, "nzp_geu");
            }

            if (!Database.ConstraintExists(dom, "fk_dom_nzp_area"))
            {
                Database.AddForeignKey("fk_dom_nzp_area", dom, "nzp_area", s_area, "nzp_area");
            }

            if (!Database.ConstraintExists(dom, "fk_dom_nzp_geu"))
            {
                Database.AddForeignKey("fk_dom_nzp_geu", dom, "nzp_geu", s_geu, "nzp_geu");
            }
        }
    }
}