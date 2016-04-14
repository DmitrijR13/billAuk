using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using KP50.DataBase.Migrator.Framework;

namespace KP50.DataBase.UpdateAssembly._2015._2015064
{
    [Migration(2015113006401, Migrator.Framework.DataBase.Charge)]
    public class Migration_2015113006401 : Migration
    {
        public override void Apply()
        {
            Database.CommandTimeout = 6000;
            var chargeXX = new SchemaQualifiedObjectName();
            chargeXX.Schema = CurrentSchema;

            for (int i = 1; i <= 12; i++)
            {
                chargeXX.Name = "charge_" + i.ToString("00");

                if (Database.TableExists(chargeXX))
                {
                    SetDefaulAndNotNull(chargeXX, "tarif          ");
                    SetDefaulAndNotNull(chargeXX, "tarif_p        ");
                    SetDefaulAndNotNull(chargeXX, "gsum_tarif     ");
                    SetDefaulAndNotNull(chargeXX, "rsum_tarif     ");
                    SetDefaulAndNotNull(chargeXX, "rsum_lgota     ");
                    SetDefaulAndNotNull(chargeXX, "sum_tarif      ");
                    SetDefaulAndNotNull(chargeXX, "sum_dlt_tarif  ");
                    SetDefaulAndNotNull(chargeXX, "sum_dlt_tarif_p");
                    SetDefaulAndNotNull(chargeXX, "sum_tarif_p    ");
                    SetDefaulAndNotNull(chargeXX, "sum_lgota      ");
                    SetDefaulAndNotNull(chargeXX, "sum_dlt_lgota  ");
                    SetDefaulAndNotNull(chargeXX, "sum_dlt_lgota_p");
                    SetDefaulAndNotNull(chargeXX, "sum_lgota_p    ");
                    SetDefaulAndNotNull(chargeXX, "sum_nedop      ");
                    SetDefaulAndNotNull(chargeXX, "sum_nedop_p    ");
                    SetDefaulAndNotNull(chargeXX, "sum_real       ");
                    SetDefaulAndNotNull(chargeXX, "sum_charge     ");
                    SetDefaulAndNotNull(chargeXX, "reval          ");
                    SetDefaulAndNotNull(chargeXX, "real_pere      ");
                    SetDefaulAndNotNull(chargeXX, "sum_pere       ");
                    SetDefaulAndNotNull(chargeXX, "real_charge    ");
                    SetDefaulAndNotNull(chargeXX, "sum_money      ");
                    SetDefaulAndNotNull(chargeXX, "money_to       ");
                    SetDefaulAndNotNull(chargeXX, "money_from     ");
                    SetDefaulAndNotNull(chargeXX, "money_del      ");
                    SetDefaulAndNotNull(chargeXX, "sum_fakt       ");
                    SetDefaulAndNotNull(chargeXX, "fakt_to        ");
                    SetDefaulAndNotNull(chargeXX, "fakt_from      ");
                    SetDefaulAndNotNull(chargeXX, "fakt_del       ");
                    SetDefaulAndNotNull(chargeXX, "sum_insaldo    ");
                    SetDefaulAndNotNull(chargeXX, "izm_saldo      ");
                    SetDefaulAndNotNull(chargeXX, "sum_outsaldo   ");
                    SetDefaulAndNotNull(chargeXX, "sum_subsidy    ");
                    SetDefaulAndNotNull(chargeXX, "sum_subsidy_p  ");
                    SetDefaulAndNotNull(chargeXX, "sum_subsidy_reval");
                    SetDefaulAndNotNull(chargeXX, "sum_subsidy_all");


                    SetDefaulAndNotNull(chargeXX, "sum_tarif_f       ");
                    SetDefaulAndNotNull(chargeXX, "sum_tarif_f_p     ");
                    SetDefaulAndNotNull(chargeXX, "sum_tarif_sn_eot  ");
                    SetDefaulAndNotNull(chargeXX, "sum_tarif_sn_eot_p");
                    SetDefaulAndNotNull(chargeXX, "sum_tarif_sn_f    ");
                    SetDefaulAndNotNull(chargeXX, "sum_tarif_sn_f_p  ");
                    SetDefaulAndNotNull(chargeXX, "tarif_f           ");
                    SetDefaulAndNotNull(chargeXX, "tarif_f_p         ");
                    SetDefaulAndNotNull(chargeXX, "reval_tarif     ");
                    SetDefaulAndNotNull(chargeXX, "reval_lgota     ");
                    SetDefaulAndNotNull(chargeXX, "sum_tarif_eot   ");
                    SetDefaulAndNotNull(chargeXX, "sum_tarif_eot_p ");
                    SetDefaulAndNotNull(chargeXX, "sum_lgota_eot   ");
                    SetDefaulAndNotNull(chargeXX, "sum_lgota_eot_p ");
                    SetDefaulAndNotNull(chargeXX, "sum_lgota_f     ");
                    SetDefaulAndNotNull(chargeXX, "sum_lgota_f_p   ");
                    SetDefaulAndNotNull(chargeXX, "sum_smo         ");
                    SetDefaulAndNotNull(chargeXX, "sum_smo_p       ");
                    SetDefaulAndNotNull(chargeXX, "rsum_subsidy    ");
                }
            }
        }

        private string GetFullTableName(SchemaQualifiedObjectName table)
        {
            return string.Format("{0}{1}{2}", table.Schema, Database.TableDelimiter, table.Name);
        }

        private void SetDefaulAndNotNull(SchemaQualifiedObjectName table, string column)
        {

            var sql = " UPDATE " + GetFullTableName(table) + " SET " + column + "=0 WHERE " + column + " IS NULL ";
            Database.ExecuteNonQuery(sql);

            sql = " ALTER TABLE " + GetFullTableName(table) +
                  " ALTER COLUMN " + column + " SET DEFAULT 0; " +
                  " ALTER TABLE " + GetFullTableName(table) +
                  " ALTER COLUMN  " + column + " SET NOT NULL;";
            Database.ExecuteNonQuery(sql);
        }

    }
}
