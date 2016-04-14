using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015064
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2015062606401, MigrateDataBase.CentralBank)]
    public class Migration_2015062606401_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName s_point = new SchemaQualifiedObjectName() { Name = "s_point", Schema = CurrentSchema };

            if (!Database.ConstraintExists(s_point, "s_point_pkey"))
                Database.AddPrimaryKey("s_point_pkey", s_point, "nzp_wp");

            SchemaQualifiedObjectName s_baselist = new SchemaQualifiedObjectName() { Name = "s_baselist", Schema = CurrentSchema };

            if (!Database.ConstraintExists(s_baselist, "fk_s_baselist_nzp_wp"))
                Database.AddForeignKey("fk_s_baselist_nzp_wp", s_baselist, "nzp_wp", s_point, "nzp_wp");
        }

        public override void Revert()
        {
            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName s_point = new SchemaQualifiedObjectName() { Name = "s_point", Schema = CurrentSchema };

            if (Database.ConstraintExists(s_point, "s_point_pkey"))
                Database.RemoveConstraint(s_point, "s_point_pkey");

            SchemaQualifiedObjectName s_baselist = new SchemaQualifiedObjectName() { Name = "s_baselist", Schema = CurrentSchema };

            if (Database.ConstraintExists(s_baselist, "fk_s_baselist_nzp_wp"))
                Database.RemoveConstraint(s_baselist, "fk_s_baselist_nzp_wp");

        }
    }
}
