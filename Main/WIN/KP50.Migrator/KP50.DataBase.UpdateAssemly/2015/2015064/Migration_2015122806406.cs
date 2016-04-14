using System;
using System.Collections.Generic;
using System.Data;
using KP50.DataBase.Migrator;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015064
{
    [Migration(2015122806406, MigrateDataBase.LocalBank)]
    public class Migration_2015122806406 : Migration
    {
        public override void Apply()
        {

            if (Database.ProviderName == "PostgreSQL")
            {
                Database.CommandTimeout = 6000;
                var central_peni_provodki = new SchemaQualifiedObjectName { Name = "peni_provodki", Schema = CentralData };
                var central_peni_debt = new SchemaQualifiedObjectName { Name = "peni_debt", Schema = CentralData };
                var central_peni_calc = new SchemaQualifiedObjectName { Name = "peni_calc", Schema = CentralData };


                if (Database.TableExists(central_peni_calc) && Database.TableExists(central_peni_provodki) && Database.TableExists(central_peni_debt))
                {
                    SetSchema(Bank.Data);


                    //переименовали партиции
                    RenamePartitions(central_peni_provodki.Name);
                    RenamePartitions(central_peni_debt.Name);
                    RenamePartitions(central_peni_calc.Name);

                }
            }
        }

        private void RenamePartitions(string masterTableName)
        {
            var sql =
                @"SELECT cn.nspname AS schema_child, c.relname AS child, pn.nspname AS schema_parent, p.relname AS parent
                  FROM pg_inherits 
                  JOIN pg_class AS c ON (inhrelid=c.oid)
                  JOIN pg_class as p ON (inhparent=p.oid)
                  JOIN pg_namespace pn ON pn.oid = p.relnamespace
                  JOIN pg_namespace cn ON cn.oid = c.relnamespace
                  WHERE p.relname ILIKE '" + masterTableName + 
                  @"' and pn.nspname = '" + CentralData + @"'"+
                  @"  and cn.nspname ILIKE '" + CurrentPrefix + @"_charge_%'" +
                  @" and c.relname NOT ILIKE '%to_remove%'";

            //получаем список всех партиций и переименовываем их
            using (var reader = Database.ExecuteReader(sql))
            {
                while (reader.Read())
                {
                    var peni_debt_part = new SchemaQualifiedObjectName
                    {
                        Name = reader["child"].ToString(),
                        Schema = reader["schema_child"].ToString()
                    };
                    Database.RenameTable(peni_debt_part, peni_debt_part.Name + "_to_remove");
                }

            }
        }

        /// <summary>
        /// проверка на существование таблицы для пени, если ее нет, то она будет автоматически создана
        /// </summary>
        /// <returns></returns>
        public void CheckExistTablePeni(int nzp_wp, DateTime date_calc, string pref, TablesForPeniCalc type, int part_id = 0)
        {
            string sql = "";
            var id = 0;
            var table = "peni_debt";
            var sDataAliasRest = "_data" + Database.TableDelimiter;
            switch (type)
            {
                case TablesForPeniCalc.PeniDebt:
                    {
                        sql = " INSERT INTO " + pref + sDataAliasRest + table + " (nzp_supp,nzp_serv,nzp_wp,date_calc,peni_actions_id,s_peni_type_debt_id) " +
                              " VALUES (0,0," + nzp_wp + "," + EStrNull(date_calc.ToShortDateString()) + ",0,1)";
                        Database.ExecuteNonQuery(sql);

                        sql = "DELETE FROM " + pref + sDataAliasRest + table +
                            " WHERE s_peni_type_debt_id=1 AND nzp_serv=0 AND nzp_supp=0 AND nzp_wp=" + nzp_wp + " AND date_calc=" +
                         EStrNull(date_calc.ToShortDateString());
                        Database.ExecuteNonQuery(sql);


                        sql = " INSERT INTO " + pref + sDataAliasRest + table + " (nzp_supp,nzp_serv,nzp_wp,date_calc,peni_actions_id,s_peni_type_debt_id) " +
                                " VALUES (0,0," + nzp_wp + "," + EStrNull(date_calc.ToShortDateString()) + ",0,2)";
                        Database.ExecuteNonQuery(sql);


                        sql = "DELETE FROM " + pref + sDataAliasRest + table +
                             " WHERE s_peni_type_debt_id=2 AND nzp_serv=0 AND nzp_supp=0 AND nzp_wp=" + nzp_wp + " AND date_calc=" +
                             EStrNull(date_calc.ToShortDateString());
                        Database.ExecuteNonQuery(sql);
                        break;
                    }
                case TablesForPeniCalc.PeniCalc:
                    {
                        table = "peni_calc";
                        sql = " INSERT INTO " + pref + sDataAliasRest + table + " (nzp_supp,nzp_wp,date_calc,peni_actions_id) " +
                               " VALUES (0," + nzp_wp + "," + EStrNull(date_calc.ToShortDateString()) + ",0)";
                        Database.ExecuteNonQuery(sql);

                        sql = "DELETE FROM " + pref + sDataAliasRest + table +
                            " WHERE nzp_supp =0 AND peni_actions_id=0 AND nzp_wp=" + nzp_wp + " AND date_calc=" +
                             EStrNull(date_calc.ToShortDateString());
                        Database.ExecuteNonQuery(sql);
                        break;
                    }
                case TablesForPeniCalc.PeniOff:
                    {
                        table = "peni_off";
                        sql = " INSERT INTO " + pref + sDataAliasRest + table + " (nzp_wp,date_calc,peni_off_id,peni_debt_id) " +
                               " VALUES (" + nzp_wp + "," + date_calc.ToShortDateStringWithQuote() + ",0,0)";
                        Database.ExecuteNonQuery(sql);


                        sql = " DELETE FROM " + pref + sDataAliasRest + table +
                              " WHERE nzp_wp=" + nzp_wp + " AND date_calc=" + date_calc.ToShortDateStringWithQuote() +
                              " AND peni_off_id=0 AND peni_debt_id=0";
                        Database.ExecuteNonQuery(sql);
                        break;
                    }
                case TablesForPeniCalc.PeniDebtRefs:
                    {
                        table = "peni_debt_refs";
                        sql = " INSERT INTO " + pref + sDataAliasRest + table + " (nzp_wp,date_calc,peni_calc_id,peni_debt_id) " +
                               " VALUES (" + nzp_wp + "," + date_calc.ToShortDateStringWithQuote() + ",0,0)";
                        Database.ExecuteNonQuery(sql);


                        sql = " DELETE FROM " + pref + sDataAliasRest + table +
                              " WHERE nzp_wp=" + nzp_wp + " AND date_calc=" + date_calc.ToShortDateStringWithQuote() +
                              " AND peni_calc_id=0 AND peni_debt_id=0";
                        Database.ExecuteNonQuery(sql);
                        break;
                    }
                case TablesForPeniCalc.PeniProvodkiRefs:
                    {
                        table = "peni_provodki_refs";

                        sql = " INSERT INTO " + pref + sDataAliasRest + table + " (nzp_wp, peni_debt_id, peni_provodki_id, date_obligation, date_calc, nzp_kvar) " +
                                  " VALUES (" + nzp_wp + ",0," + part_id * 5000000 + ", '01.01.1900'::DATE, " + date_calc.ToShortDateStringWithQuote() + ",0)";
                        Database.ExecuteNonQuery(sql);


                        sql = " DELETE FROM " + pref + sDataAliasRest + table +
                                " WHERE nzp_wp=" + nzp_wp + " AND date_calc=" + date_calc.ToShortDateStringWithQuote() +
                                " AND peni_provodki_id=" + part_id * 5000000 + " AND peni_debt_id=0";
                        Database.ExecuteNonQuery(sql);

                        break;
                    }
                case TablesForPeniCalc.PeniProvodki:
                    {
                        table = "peni_provodki";

                        sql = " INSERT INTO " + pref + sDataAliasRest + table + " (nzp_serv,nzp_supp,nzp_wp,date_obligation,s_prov_types_id,peni_actions_id) " +
                                     " VALUES (0,0," + nzp_wp + "," + EStrNull(date_calc.ToShortDateString()) + ",0,0)";
                        Database.ExecuteNonQuery(sql);

                        sql = "DELETE FROM " + pref + sDataAliasRest + table +
                              " WHERE nzp_serv=0 AND nzp_supp=0 AND s_prov_types_id=0 AND nzp_wp=" + nzp_wp +
                              " AND date_obligation=" + EStrNull(date_calc.ToShortDateString());
                        Database.ExecuteNonQuery(sql);

                        break;
                    }
            }

        }


        protected string EStrNull(string s)
        {
            return "'" + s + "'";
        }
        public enum TablesForPeniCalc
        {
            PeniProvodki = 1,
            PeniDebt = 2,
            PeniCalc = 3,
            PeniOff = 4,
            PeniProvodkiRefs = 5,
            PeniDebtRefs = 6
        }
    }

    public static class DateTimeExtensions
    {
        public static string ToShortDateStringWithQuote(this DateTime dt, string quote = "'")
        {
            return string.Format("{0}{1}{0}", quote, dt.ToShortDateString());
        }
    }
}
