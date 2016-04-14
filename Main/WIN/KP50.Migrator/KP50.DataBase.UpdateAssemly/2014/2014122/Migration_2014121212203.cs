using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
     [Migration(2014121212203, MigrateDataBase.LocalBank)]
    public class Migration_2014121212203_local : Migration
    {
         public override void Apply()
         {
             SetSchema(Bank.Data);
             var countersComment = new SchemaQualifiedObjectName
             {
                 Name = "counters_comment",
                 Schema = CurrentSchema
             };
             if (Database.TableExists(countersComment))
             {
                 if (!Database.ColumnExists(countersComment, "nzp_cnttype"))
                 Database.AddColumn(countersComment, new Column("nzp_cnttype", DbType.Int32));
             }
         }
    }
}
