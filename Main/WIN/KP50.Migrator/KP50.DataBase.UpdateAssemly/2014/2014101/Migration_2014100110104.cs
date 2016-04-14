using System;
using System.Data;
using System.Linq;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2014._2014101
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2014100110104, MigrateDataBase.CentralBank)]
    public class Migration_2014100110104_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Supg);

            SchemaQualifiedObjectName nedop_kvar = new SchemaQualifiedObjectName() { Name = "nedop_kvar", Schema = CurrentSchema};
            SchemaQualifiedObjectName jrn_upg_nedop = new SchemaQualifiedObjectName(){ Name = "jrn_upg_nedop", Schema = CurrentSchema};


            if (!Database.SequenceExists(CurrentSchema, "nedop_kvar_nzp_jrn_seq"))
            {
                Database.AddSequence(CurrentSchema, "nedop_kvar_nzp_jrn_seq");
                int count = Database.ExecuteScalar(
                    Database.FormatSql("SELECT MAX(TB.count) FROM (" +
                                       "SELECT max(nzp_jrn) AS count FROM {0:NAME} UNION " +
                                       "SELECT max(no) AS count FROM {1:NAME}) AS TB", nedop_kvar, jrn_upg_nedop))
                    as int? ?? 1;
                Database.ExecuteNonQuery(Database.FormatSql("ALTER SEQUENCE {0:NAME} RESTART WITH {1}",
                    nedop_kvar.Name += "_nzp_jrn_seq", count + 1));
            }
        }

        public override void Revert()
        {
            SetSchema(Bank.Supg);

            SchemaQualifiedObjectName nedop_kvar = new SchemaQualifiedObjectName() { Name = "nedop_kvar", Schema = CurrentSchema };

            if (!Database.SequenceExists(CurrentSchema, nedop_kvar + "_nzp_jrn_seq"))
                Database.RemoveSequence(CurrentSchema, nedop_kvar + "_nzp_jrn_seq");
        }
    }


}
