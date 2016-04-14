using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015021
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2015013002103, MigrateDataBase.CentralBank)]
    public class Migration_2015013002103_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            // TODO: Upgrade CentralPref_Kernel

            SetSchema(Bank.Data);
            SchemaQualifiedObjectName s_remark_kart = new SchemaQualifiedObjectName() { Name = "s_remark_kart", Schema = CurrentSchema };

            if (!Database.TableExists("s_remark_kart"))
            {
                Database.AddTable(s_remark_kart,
                    new Column("id_rem", DbType.Int32, ColumnProperty.NotNull | ColumnProperty.Identity),
                    new Column("remark", DbType.String, ColumnProperty.NotNull));
            }

            SetSchema(Bank.Upload);
            // TODO: Upgrade CentralPref_Upload
        }

        public override void Revert()
        {
            SetSchema(Bank.Kernel);
            // TODO: Downgrade CentralPref_Kernel

            SetSchema(Bank.Data);
            // TODO: Downgrade CentralPref_Data

            SetSchema(Bank.Upload);
            // TODO: Downgrade CentralPref_Upload
        }
    }

  
}
