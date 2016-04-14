using System.Data;
using KP50.DataBase.Migrator.Framework;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2015032103303, Migrator.Framework.DataBase.Fin)]
    public class Migration_2015032103303:Migration
    {
        public override void Apply()
        {
            var fn_sended = new SchemaQualifiedObjectName { Name = "fn_sended", Schema = CurrentSchema };
            if (Database.ColumnExists(fn_sended, "nzp_fd"))
            {
                Database.ChangeColumn(fn_sended, "nzp_fd", new ColumnType(DbType.Int32), false);
            }
            var fn_sended_dom = new SchemaQualifiedObjectName { Name = "fn_sended_dom", Schema = CurrentSchema };
            if (!Database.ColumnExists(fn_sended_dom, "fn_dogovor_bank_lnk_id"))
            {
                Database.AddColumn(fn_sended_dom, new Column("fn_dogovor_bank_lnk_id", new ColumnType(DbType.Int32)));
            }
        }
    }
}
