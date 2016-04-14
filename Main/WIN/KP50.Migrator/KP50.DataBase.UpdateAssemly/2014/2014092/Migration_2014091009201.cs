using KP50.DataBase.Migrator.Framework;
using System.Collections.Generic;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2014091009201, MigrateDataBase.CentralBank)]
    public class Migration_2014091009201_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            List<string> lstQuerys = new List<string>();
           
            if (Database.ProviderName == "PostgreSQL")
            {
                lstQuerys = new List<string>();
                lstQuerys.Add(  "CREATE OR REPLACE FUNCTION "+CurrentSchema+".getsumostatok(pnzp_pack_ls integer) " +
                                " RETURNS numeric AS " +
                                "$BODY$  DECLARE sum_out decimal(14,2); "+
                                "begin  sum_out = 0; select AVG(znak*g_sum_ls)-SUM(sum_prih_d+sum_prih_a+sum_prih_u+sum_prih_s) into sum_out from t_opl where nzp_pack_ls = pnzp_pack_ls; return sum_out; end; $BODY$ "+
                                "  LANGUAGE plpgsql VOLATILE " +
                                "  COST 100; ");
                foreach (string Query in lstQuerys) Database.ExecuteNonQuery(Query);

                lstQuerys = new List<string>();
                lstQuerys.Add("ALTER FUNCTION " + CurrentSchema + ".getsumostatok(integer) OWNER TO postgres;");
                foreach (string Query in lstQuerys) Database.ExecuteNonQuery(Query);                
            }
        }
    }
}
