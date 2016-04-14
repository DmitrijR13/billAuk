using KP50.DataBase.Migrator.Framework;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2015032103301, Migrator.Framework.DataBase.CentralBank)]
    public class Migration_2015032103301_CentralBank : Migration
    {
        public override void Apply()
        {
            Database.ExecuteNonQuery("UPDATE " + CentralData + Database.TableDelimiter + "prm_10 f SET " +
                                     " val_prm = '1' where nzp_prm = 1273 and is_actual = 1 " +
                                     " and dat_po = '01.01.3000' and val_prm  in ('3','4')");
            Database.ExecuteNonQuery("delete from " + CentralKernel + Database.TableDelimiter + "res_y  " +
                                     " where nzp_res = 3020  and nzp_y in (3,4)");
        }
    }

    
}
