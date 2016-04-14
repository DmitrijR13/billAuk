using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015064
{
    [Migration(2015101506401, MigrateDataBase.LocalBank)]
    public class Migration_2015101506401_LocalBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);
            SchemaQualifiedObjectName s_namereg = new SchemaQualifiedObjectName() { Name = "s_namereg", Schema = CurrentSchema };
            if (Database.TableExists(s_namereg))
            {
                if (!Database.ColumnExists(s_namereg, "kod_namereg_prn"))
                    Database.AddColumn(s_namereg, new Column("kod_namereg_prn", DbType.String.WithSize(7)));
            }
        }

        public override void Revert()
        {
        }
    }
}
