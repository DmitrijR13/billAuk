using System.Collections.Generic;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2014102710401, MigrateDataBase.CentralBank)]
    public class Migration_2014102710401_CentralBank : Migration
    {
        public override void Apply()
        {
            List<string> lstQuerys = new List<string>();
            if (Database.ProviderName == "PostgreSQL")
            {
                SetSchema(Bank.Kernel);
                lstQuerys = new List<string>();
                lstQuerys.Add(
                    " CREATE OR REPLACE FUNCTION " + CurrentSchema + ".getsumostatok(pnzp_pack_ls integer)" +
                    " RETURNS numeric AS " +
                    " $BODY$ " +
                    " DECLARE sum_out decimal(14,2); " +
                    " BEGIN " +
                    " sum_out = 0; " +
                    " SELECT " +
                    " AVG(g_sum_ls)-SUM(sum_prih_d+sum_prih_a+sum_prih_u+sum_prih_s) " +
                    " into sum_out from t_opl where nzp_pack_ls = pnzp_pack_ls; " +
                    " return sum_out; " +
                    " end; " +
                    " $BODY$ " +
                    " LANGUAGE plpgsql VOLATILE " +
                    " COST 100; "
                    );
                foreach (string Query in lstQuerys) Database.ExecuteNonQuery(Query);

                lstQuerys = new List<string>();
                lstQuerys.Add("ALTER FUNCTION " + CurrentSchema + ".getsumostatok(pnzp_pack_ls integer) OWNER TO postgres;");
                lstQuerys.Add("GRANT EXECUTE ON FUNCTION " + CurrentSchema + ".getsumostatok(pnzp_pack_ls integer) TO postgres;");
                lstQuerys.Add("GRANT EXECUTE ON FUNCTION " + CurrentSchema + ".getsumostatok(pnzp_pack_ls integer) TO public;");
                foreach (string Query in lstQuerys) Database.ExecuteNonQuery(Query);
            }

            if (Database.ProviderName == "Informix")
            {
                SetSchema(Bank.Kernel);
                if (!Database.ProcedureExists(CurrentSchema, "getsumostatok"))
                {
                    lstQuerys = new List<string>();
                    lstQuerys.Add("drop function \"are\".getsumostatok(integer)");

                    foreach (string Query in lstQuerys) Database.ExecuteNonQuery(Query);

                    lstQuerys = new List<string>();
                    lstQuerys.Add(
                        " CREATE FUNCTION \"are\".getsumostatok(pnzp_pack_ls integer) " +
                        " RETURNING decimal(14,2); " +
                        " DEFINE sum_out decimal(14,2); " +
                        " LET sum_out = 0; " +
                        " SELECT " +
                        " AVG(g_sum_ls)-SUM(sum_prih_d+sum_prih_a+sum_prih_u+sum_prih_s) " +
                        " into sum_out from t_opl where nzp_pack_ls = pnzp_pack_ls; " +
                        " RETURN sum_out; " +
                        " END FUNCTION; "
                        );
                    lstQuerys.Add(
                        "grant execute on function \"are\".getsumostatok(pnzp_pack_ls integer) to public as are");
                    foreach (string Query in lstQuerys) Database.ExecuteNonQuery(Query);
                }
                else
                {
                    lstQuerys = new List<string>();
                    lstQuerys.Add(
                        " CREATE FUNCTION \"are\".getsumostatok(pnzp_pack_ls integer) " +
                        " RETURNING decimal(14,2); " +
                        " DEFINE sum_out decimal(14,2); " +
                        " LET sum_out = 0; " +
                        " SELECT " +
                        " AVG(g_sum_ls)-SUM(sum_prih_d+sum_prih_a+sum_prih_u+sum_prih_s) " +
                        " into sum_out from t_opl where nzp_pack_ls = pnzp_pack_ls; " +
                        " RETURN sum_out; " +
                        " END FUNCTION; "
                        );
                    lstQuerys.Add(
                        "grant execute on function \"are\".getsumostatok(pnzp_pack_ls integer) to public as are");
                    foreach (string Query in lstQuerys) Database.ExecuteNonQuery(Query);
                }
            }
        }

        public override void Revert()
        {

        }
    }


    [Migration(2014102710401, MigrateDataBase.Fin)]
    public class Migration_2014102710401_Fin : Migration
    {
        public override void Apply()
        {
            SchemaQualifiedObjectName packLs = new SchemaQualifiedObjectName()
            {
                Name = "pack_ls",
                Schema = CurrentSchema
            };
            SchemaQualifiedObjectName pack = new SchemaQualifiedObjectName()
            {
                Name = "pack",
                Schema = CurrentSchema
            };
            if (Database.TableExists(packLs) && Database.TableExists(pack))
            {
                
                string sql =
                    Database.FormatSql(" ( SELECT DISTINCT a.nzp_payer FROM {0:NAME} a where {1:NAME}.nzp_pack = a.nzp_pack) ", pack, packLs);
                string update =
                    Database.FormatSql(" UPDATE {0:NAME} SET nzp_payer = " + sql + " WHERE kod_sum= 49 and nzp_payer=0;",
                        packLs);
                Database.ExecuteNonQuery(update);
                update =
                    Database.FormatSql(" UPDATE {0:NAME} SET nzp_supp = 0  where kod_sum = 49 and nzp_supp > 0 and nzp_payer > 0 ", packLs);
                Database.ExecuteNonQuery(update);
                update =
                    Database.FormatSql(" UPDATE {0:NAME} SET kod_sum = 40  where nzp_pack = 255", packLs);
                Database.ExecuteNonQuery(update);
                                    
                if (!Database.ConstraintExists(packLs, "pack_ls_1_check"))
                Database.AddCheckConstraint("pack_ls_1_check", packLs,
                    "CASE WHEN kod_sum = 49 THEN nzp_payer > 0 AND nzp_supp = 0 ELSE CASE WHEN kod_sum = 50 THEN nzp_supp > 0 AND nzp_payer = 0 END END");
            }

            if (Database.TableExists(pack))
            {
                if (!Database.ConstraintExists(pack, "pack_1_check"))
                Database.AddCheckConstraint("pack_1_check", pack,
                    "CASE WHEN pack_type = 20 THEN nzp_payer > 0 AND nzp_supp > 0 END ");
            }
        }

        public override void Revert()
        {

        }
    }
}
