using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015023
{
    
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2015030202301, MigrateDataBase.Web)]
    public class Migration_2015030202301_Web : Migration
    {
        public override void Apply()
        {

            var source_gil_sums = new SchemaQualifiedObjectName { Name = "source_gil_sums", Schema = CurrentSchema };
            if (Database.TableExists(source_gil_sums))
            {
                if (!Database.ColumnExists(source_gil_sums, "nzp_supp"))
                {
                    Database.AddColumn(source_gil_sums, new Column("nzp_supp", DbType.Int32));
                }
                if (!Database.ColumnExists(source_gil_sums, "nzp_serv"))
                {
                    Database.AddColumn(source_gil_sums, new Column("nzp_serv", DbType.Int32));
                }
            }

        }

        public override void Revert()
        {
            // TODO: Downgrade Web

        }
    }
}
