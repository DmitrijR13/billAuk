using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;
namespace KP50.DataBase.UpdateAssembly
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2014061606201, MigrateDataBase.CentralBank)]
    public class Migration_2014061606201_CentralBank : Migration
    {
        public override void Apply()
        {
        }

        public override void Revert()
        {
        }
    }

    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2014061606201, MigrateDataBase.LocalBank)]
    public class Migration_2014061606201_LocalBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);
            // TODO: Upgrade LocalPref_Data
            SchemaQualifiedObjectName kredit = new SchemaQualifiedObjectName() { Name = "kredit", Schema = CurrentSchema };
            if (!Database.ColumnExists(kredit, "dog_num")) Database.AddColumn(kredit, new Column("dog_num", DbType.StringFixedLength.WithSize(20)));
            if (!Database.ColumnExists(kredit, "dog_dat")) Database.AddColumn(kredit, new Column("dog_dat", DbType.Date));
        }

        public override void Revert()
        {
            SetSchema(Bank.Kernel);
            // TODO: Downgrade LocalPref_Kernel

            SetSchema(Bank.Data);
            // TODO: Downgrade LocalPref_Data

        }
    }

    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2014061606201, MigrateDataBase.Charge)]
    public class Migration_2014061606201_Charge : Migration
    {
        public override void Apply()
        {
            // TODO: Upgrade Charges

        }

        public override void Revert()
        {
            // TODO: Downgrade Charges

        }
    }

    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2014061606201, MigrateDataBase.Fin)]
    public class Migration_2014061606201_Fin : Migration
    {
        public override void Apply()
        {
            // TODO: Upgrade Fins

        }

        public override void Revert()
        {
            // TODO: Downgrade Fins

        }
    }

    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2014061606201, MigrateDataBase.Web)]
    public class Migration_2014061606201_Web : Migration
    {
        public override void Apply()
        {
            // TODO: Upgrade Web

        }

        public override void Revert()
        {
            // TODO: Downgrade Web

        }
    }
}
