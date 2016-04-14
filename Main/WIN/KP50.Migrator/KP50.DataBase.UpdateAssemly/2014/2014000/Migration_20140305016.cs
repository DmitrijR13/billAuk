using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(20140305016, MigrateDataBase.Web)]
    public class Migration_20140305016_Web : Migration
    {
        public override void Apply()
        {
            SchemaQualifiedObjectName excel_utility = new SchemaQualifiedObjectName() { Name = "excel_utility", Schema = CurrentSchema };
            if (!Database.ColumnExists(excel_utility, "file_name")) Database.AddColumn(excel_utility, new Column("file_name", DbType.String.WithSize(100)));
        }

        public override void Revert()
        {
            SchemaQualifiedObjectName excel_utility = new SchemaQualifiedObjectName() { Name = "excel_utility", Schema = CurrentSchema };
            if (Database.ColumnExists(excel_utility, "file_name")) Database.RemoveColumn(excel_utility, "file_name");
        }
    }
}
