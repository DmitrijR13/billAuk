using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(20140305021, MigrateDataBase.CentralBank)]
    public class Migration_20140305021_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName serv_odn = new SchemaQualifiedObjectName() { Name = "serv_odn", Schema = CurrentSchema };
            if (!Database.TableExists(serv_odn))
            {
                Database.AddTable(serv_odn,
                    new Column("nzp_serv", DbType.Int32),
                    new Column("nzp_serv_link", DbType.Int32),
                    new Column("nzp_frm", DbType.Int32),
                    new Column("nzp_frm_eqv", DbType.Int32),
                    new Column("nzp_prm_mop", DbType.Int32),
                    new Column("nzp_prm_mopgr", DbType.Int32),
                    new Column("nzp_serv_repay", DbType.Int32),
                    new Column("nzp_frm_repay", DbType.Int32),
                    new Column("nzp_prm_repay", DbType.Int32),
                    new Column("nzp_prm_repays", DbType.Int32));
                Database.Delete(serv_odn, "nzp_serv BETWEEN 510 AND 517");
                Database.Insert(serv_odn,
                    new string[] { "nzp_serv", "nzp_serv_link", "nzp_frm", "nzp_frm_eqv", "nzp_prm_mop", "nzp_prm_mopgr", "nzp_serv_repay", "nzp_frm_repay", "nzp_prm_repay", "nzp_prm_repays" },
                    new string[] { "510", "6", "990", "980", "2474", "2472", "306", "1992", "1116", "1122" });
                Database.Insert(serv_odn,
                    new string[] { "nzp_serv", "nzp_serv_link", "nzp_frm", "nzp_frm_eqv", "nzp_prm_mop", "nzp_prm_mopgr", "nzp_serv_repay", "nzp_frm_repay", "nzp_prm_repay", "nzp_prm_repays" },
                    new string[] { "511", "7", "991", "981", "2049", "2471", "308", "1993", "1118", "1122" });
                Database.Insert(serv_odn,
                    new string[] { "nzp_serv", "nzp_serv_link", "nzp_frm", "nzp_frm_eqv", "nzp_prm_mop", "nzp_prm_mopgr", "nzp_serv_repay", "nzp_frm_repay", "nzp_prm_repay", "nzp_prm_repays" },
                    new string[] { "512", "8", "992", "982", "2049", "2471", "309", "1994", "1119", "1122" });
                Database.Insert(serv_odn,
                    new string[] { "nzp_serv", "nzp_serv_link", "nzp_frm", "nzp_frm_eqv", "nzp_prm_mop", "nzp_prm_mopgr", "nzp_serv_repay", "nzp_frm_repay", "nzp_prm_repay", "nzp_prm_repays" },
                    new string[] { "513", "9", "993", "983", "2475", "2473", "307", "1995", "1117", "1122" });
                Database.Insert(serv_odn,
                    new string[] { "nzp_serv", "nzp_serv_link", "nzp_frm", "nzp_frm_eqv", "nzp_prm_mop", "nzp_prm_mopgr", "nzp_serv_repay", "nzp_frm_repay", "nzp_prm_repay", "nzp_prm_repays" },
                    new string[] { "514", "14", "994", "984", "2049", "2471", "496", "1996", "1117", "1122" });
                Database.Insert(serv_odn,
                    new string[] { "nzp_serv", "nzp_serv_link", "nzp_frm", "nzp_frm_eqv", "nzp_prm_mop", "nzp_prm_mopgr", "nzp_serv_repay", "nzp_frm_repay", "nzp_prm_repay", "nzp_prm_repays" },
                    new string[] { "515", "25", "995", "985", "2049", "2471", "310", "1997", "1120", "1122" });
                Database.Insert(serv_odn,
                    new string[] { "nzp_serv", "nzp_serv_link", "nzp_frm", "nzp_frm_eqv", "nzp_prm_mop", "nzp_prm_mopgr", "nzp_serv_repay", "nzp_frm_repay", "nzp_prm_repay", "nzp_prm_repays" },
                    new string[] { "516", "210", "996", "986", "2049", "2471", "497", "1998", "1120", "1122" });
                Database.Insert(serv_odn,
                    new string[] { "nzp_serv", "nzp_serv_link", "nzp_frm", "nzp_frm_eqv", "nzp_prm_mop", "nzp_prm_mopgr", "nzp_serv_repay", "nzp_frm_repay", "nzp_prm_repay", "nzp_prm_repays" },
                    new string[] { "517", "10", "998", "988", "2049", "2471", "311", "1999", "1121", "1122" });
            }
        }

        public override void Revert()
        {
            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName serv_odn = new SchemaQualifiedObjectName() { Name = "serv_odn", Schema = CurrentSchema };
            if (Database.TableExists(serv_odn)) Database.RemoveTable(serv_odn);
        }
    }
}
