using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;
namespace KP50.DataBase.UpdateAssembly
{
    [Migration(20140505051, MigrateDataBase.Fin)]
    public class Migration_20140505051_Fin : Migration
    {
        public override void Apply()
        {
            SchemaQualifiedObjectName fn_reval = new SchemaQualifiedObjectName() { Name = "fn_reval", Schema = CurrentSchema };
            if (Database.TableExists(fn_reval) && !Database.ColumnExists(fn_reval, "nzp_supp"))
                Database.AddColumn(fn_reval, new Column("nzp_supp", DbType.Int32));

            SchemaQualifiedObjectName fn_reval_dom = new SchemaQualifiedObjectName() { Name = "fn_reval_dom", Schema = CurrentSchema };
            if (Database.TableExists(fn_reval_dom) && !Database.ColumnExists(fn_reval, "nzp_supp"))
                Database.AddColumn(fn_reval_dom, new Column("nzp_supp", DbType.Int32)); 
        }

        public override void Revert()
        {
            SchemaQualifiedObjectName fn_reval = new SchemaQualifiedObjectName() { Name = "fn_reval", Schema = CurrentSchema };
            if (Database.ColumnExists(fn_reval, "nzp_supp")) Database.RemoveColumn(fn_reval, "nzp_supp");

            SchemaQualifiedObjectName fn_reval_dom = new SchemaQualifiedObjectName() { Name = "fn_reval_dom", Schema = CurrentSchema };
            if (Database.ColumnExists(fn_reval_dom, "nzp_supp")) Database.RemoveColumn(fn_reval, "nzp_supp");
        }
    }
}
