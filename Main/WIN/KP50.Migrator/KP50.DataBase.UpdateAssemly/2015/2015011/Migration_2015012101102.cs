using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015011
{
    [Migration(2015012101102, MigrateDataBase.CentralBank)]
    public class Migration_2015012101102_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);
            var fn_curoperday = new SchemaQualifiedObjectName() { Name = "fn_curoperday", Schema = CurrentSchema };
            var s_payer = new SchemaQualifiedObjectName() { Name = "s_payer", Schema = CentralKernel };

            if (!Database.ColumnExists(fn_curoperday, "nzp_payer_agent")) 
            {
                Database.AddColumn(fn_curoperday, new Column("nzp_payer_agent", DbType.Int32));
                Database.AddForeignKey("FK_fn_curoperday_nzp_payer_agent", fn_curoperday, "nzp_payer_agent", s_payer, "nzp_payer");
                Database.AddIndex("IX_fn_curoperday_nzp_payer_agent", false, fn_curoperday, "nzp_payer_agent");
            }
        }

        public override void Revert()
        {
        }
    }

    [Migration(2015012101102, MigrateDataBase.Fin)]
    public class Migration_2015012101102_Fin : Migration
    {
        public override void Apply()
        {
            var pack = new SchemaQualifiedObjectName() { Name = "pack", Schema = CurrentSchema };
            var s_payer = new SchemaQualifiedObjectName() { Name = "s_payer", Schema = CentralKernel };

            if (!Database.ColumnExists(pack, "nzp_payer_agent"))
            {
                Database.AddColumn(pack, new Column("nzp_payer_agent", DbType.Int32));
                Database.AddForeignKey("FK_pack_nzp_payer_agent", pack, "nzp_payer_agent", s_payer, "nzp_payer");
                Database.AddIndex("IX_pack_nzp_payer_agent", false, pack, "nzp_payer_agent");
            }
        }

        public override void Revert()
        {
        }
    }
}
