using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(20140514052, MigrateDataBase.CentralBank)]
    public class Migration_20140514052_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);
            SchemaQualifiedObjectName s_area = new SchemaQualifiedObjectName() { Name = "s_area", Schema = CurrentSchema };
            if (Database.TableExists(s_area) && !Database.ColumnExists(s_area, "nzp_payer"))
                Database.AddColumn(s_area, new Column("nzp_payer", DbType.Int32));

            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName s_payer = new SchemaQualifiedObjectName() { Name = "s_payer", Schema = CurrentSchema };
            if (Database.TableExists(s_payer) && !Database.ColumnExists(s_payer, "changed_on"))
                Database.AddColumn(s_payer, new Column("changed_on", DbType.DateTime));
            if (Database.TableExists(s_payer) && !Database.ColumnExists(s_payer, "changed_by"))
                Database.AddColumn(s_payer, new Column("changed_by", DbType.Int32));
        }           
    }

    [Migration(20140514052, MigrateDataBase.LocalBank)]
    public class Migration_20140514052_LocalBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);
            SchemaQualifiedObjectName s_area = new SchemaQualifiedObjectName() { Name = "s_area", Schema = CurrentSchema };
            if (Database.TableExists(s_area) && !Database.ColumnExists(s_area, "nzp_payer"))
                Database.AddColumn(s_area, new Column("nzp_payer", DbType.Int32));
        }

    }

    
}
