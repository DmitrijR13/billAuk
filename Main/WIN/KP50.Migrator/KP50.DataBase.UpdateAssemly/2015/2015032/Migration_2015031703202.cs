using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015032
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2015031703202, MigrateDataBase.CentralBank)]
    public class Migration_2015031703202_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Upload);
            
            SchemaQualifiedObjectName file_dom = new SchemaQualifiedObjectName() { Name = "file_dom", Schema = CurrentSchema };
            if (!Database.ColumnExists(file_dom, "sid"))
            {
                Database.AddColumn(file_dom, new Column("sid", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull));
            }

            SchemaQualifiedObjectName file_kvar = new SchemaQualifiedObjectName() { Name = "file_kvar", Schema = CurrentSchema };
            if (!Database.ColumnExists(file_kvar, "sid"))
            {
                Database.AddColumn(file_kvar, new Column("sid", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull));
            }
        }
    }
}
