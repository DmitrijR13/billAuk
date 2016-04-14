using System;
using System.Collections.Generic;
using System.Data;
using KP50.DataBase.Migrator;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015064
{
    [Migration(2015122806407, MigrateDataBase.LocalBank)]
    public class Migration_2015122806407 : Migration
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

                    //теперь создадим новые в новых структурах
                    CreatePartitionsPeniProvodki(central_peni_provodki);
                    CreatePartitionsPeniDebt(central_peni_debt);
                    CreatePartitionsPeniCalc(central_peni_calc);

                }
            }
        }




        private void CreatePartitionsPeniProvodki(SchemaQualifiedObjectName centralTable)
        {
            var nzp_wp = (int)Database.ExecuteScalar("SELECT nzp_wp FROM " + CentralKernel + Database.TableDelimiter +
                                                 "s_point WHERE bd_kernel='" + CurrentPrefix + "'");

            using (var reader = Database.ExecuteReader(" SELECT DISTINCT date_trunc('month',date_obligation) as date_obligation" +
                                                       " FROM " + GetFullTableName(centralTable) + " WHERE nzp_wp=" + nzp_wp))
            {
                while (reader.Read())
                {
                    var date_obligation = Convert.ToDateTime(reader["date_obligation"]);
                    CheckExistTablePeni(nzp_wp, date_obligation, CurrentPrefix, TablesForPeniCalc.PeniProvodki);
                }
            }
        }

        private void CreatePartitionsPeniDebt(SchemaQualifiedObjectName centralTable)
        {
            var nzp_wp = (int)Database.ExecuteScalar("SELECT nzp_wp FROM " + CentralKernel + Database.TableDelimiter +
                                                 "s_point WHERE bd_kernel='" + CurrentPrefix + "'");

            using (var reader = Database.ExecuteReader(" SELECT DISTINCT date_trunc('month',date_calc) as date_calc" +
                                                       " FROM " + GetFullTableName(centralTable) + " WHERE nzp_wp=" + nzp_wp))
            {
                while (reader.Read())
                {
                    var date_calc = Convert.ToDateTime(reader["date_calc"]);
                    CheckExistTablePeni(nzp_wp, date_calc, CurrentPrefix, TablesForPeniCalc.PeniDebt);

                }
            }
        }

        private void CreatePartitionsPeniCalc(SchemaQualifiedObjectName centralTable)
        {
            var nzp_wp = (int)Database.ExecuteScalar("SELECT nzp_wp FROM " + CentralKernel + Database.TableDelimiter +
                                                    "s_point WHERE bd_kernel='" + CurrentPrefix + "'");

            using (var reader = Database.ExecuteReader(" SELECT DISTINCT date_trunc('year',date_calc) as date_calc" +
                                                       " FROM " + GetFullTableName(centralTable) + " WHERE nzp_wp=" + nzp_wp))
            {
                while (reader.Read())
                {
                    var date_calc = Convert.ToDateTime(reader["date_calc"]);
                    CheckExistTablePeni(nzp_wp, date_calc, CurrentPrefix, TablesForPeniCalc.PeniCalc);
                }
            }
        }

        private string GetFullTableName(SchemaQualifiedObjectName table)
        {
            return string.Format("{0}{1}{2}", table.Schema, Database.TableDelimiter, table.Name);
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

}
