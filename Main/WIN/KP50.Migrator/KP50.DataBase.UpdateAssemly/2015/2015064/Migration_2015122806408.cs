using System;
using System.Collections.Generic;
using System.Data;
using KP50.DataBase.Migrator;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015064
{
    [Migration(2015122806408, MigrateDataBase.LocalBank)]
    public class Migration_2015122806408 : Migration
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
                    var peni_provodki = new SchemaQualifiedObjectName { Name = "peni_provodki", Schema = CurrentSchema };
                    var peni_debt = new SchemaQualifiedObjectName { Name = "peni_debt", Schema = CurrentSchema };
                    var peni_calc = new SchemaQualifiedObjectName { Name = "peni_calc", Schema = CurrentSchema };

                    //чистим данные в новых мастер-таблицах на случай повторного проведения миграции
                    Database.ExecuteNonQuery(" DELETE FROM " + GetFullTableName(peni_provodki));
                    Database.ExecuteNonQuery(" DELETE FROM " + GetFullTableName(peni_debt));
                    Database.ExecuteNonQuery(" DELETE FROM " + GetFullTableName(peni_calc));

                    //теперь создадим новые в новых структурах
                    ReplaceDataFromPeniProvodki(central_peni_provodki);
                    ReplaceDataFromPeniDebt(central_peni_debt);
                    ReplaceDataFromPeniCalc(central_peni_calc);
                    
                    //двигаем последовательности
                    SetSequence(peni_provodki);
                    SetSequence(peni_debt);
                    SetSequence(peni_calc);
                }
            }
        }

      

        private void SetSequence(SchemaQualifiedObjectName local)
        {
            var maxID = Convert.ToInt32(Database.ExecuteScalar("SELECT COALESCE(max(id),1)+1 FROM " + GetFullTableName(local)));
            Database.ExecuteNonQuery("SELECT SETVAL(" + GetSeqName(local) + "," + maxID + ") ");
        }
        private string GetSeqName(SchemaQualifiedObjectName table)
        {
            return string.Format("'{0}{1}{2}_id_seq'", table.Schema, Database.TableDelimiter, table.Name);
        }


        private void ReplaceDataFromPeniProvodki(SchemaQualifiedObjectName centralTable)
        {
            var nzp_wp = (int)Database.ExecuteScalar("SELECT nzp_wp FROM " + CentralKernel + Database.TableDelimiter +
                                                 "s_point WHERE bd_kernel='" + CurrentPrefix + "'");

            using (var reader = Database.ExecuteReader(" SELECT DISTINCT date_trunc('month',date_obligation)::DATE as date_obligation" +
                                                       " FROM " + GetFullTableName(centralTable) + " WHERE nzp_wp=" + nzp_wp))
            {
                while (reader.Read())
                {
                    var date_obligation = Convert.ToDateTime(reader["date_obligation"]);
                    var schema = CurrentPrefix + "_charge_" + (date_obligation.Year - 2000).ToString("00");
                    var tableName = "peni_provodki_" + date_obligation.Year + date_obligation.Month.ToString("00") + "_" +
                                    nzp_wp;
                    var fullTableName = schema + Database.TableDelimiter + tableName;
                    CheckExistTablePeni(nzp_wp, date_obligation, CurrentPrefix, TablesForPeniCalc.PeniProvodki);

                    Database.ExecuteNonQuery(" INSERT INTO " + fullTableName +
                                             " (id,nzp_kvar,num_ls,nzp_dom,nzp_serv,nzp_supp,nzp_wp,s_prov_types_id,nzp_source,rsum_tarif," +
                                             " sum_prih,sum_nedop,sum_reval,date_prov,date_obligation,created_on," +
                                             " created_by,changed_on,changed_by,peni_actions_id)" +
                                             " SELECT id,nzp_kvar,num_ls,nzp_dom,nzp_serv,nzp_supp,nzp_wp," +
                                             " s_prov_types_id,nzp_source,rsum_tarif,sum_prih,sum_nedop,sum_reval," +
                                             " date_prov,date_obligation,created_on,created_by,changed_on,changed_by,peni_actions_id" +
                                             " FROM " + GetFullTableName(centralTable) +
                                             " WHERE nzp_wp=" + nzp_wp +
                                             " AND date_trunc('month',date_obligation)::DATE=" + date_obligation.ToShortDateStringWithQuote());
                }
            }
        }

        private void ReplaceDataFromPeniDebt(SchemaQualifiedObjectName centralTable)
        {
            var nzp_wp = (int)Database.ExecuteScalar("SELECT nzp_wp FROM " + CentralKernel + Database.TableDelimiter +
                                                 "s_point WHERE bd_kernel='" + CurrentPrefix + "'");

            using (var reader = Database.ExecuteReader(" SELECT DISTINCT date_trunc('month',date_calc)::DATE as date_calc" +
                                                       " FROM " + GetFullTableName(centralTable) + " WHERE nzp_wp=" + nzp_wp))
            {
                while (reader.Read())
                {
                    var date_calc = Convert.ToDateTime(reader["date_calc"]);
                    var schema = CurrentPrefix + "_charge_" + (date_calc.Year - 2000).ToString("00");
                    var tableNameUp = "peni_debt_" + date_calc.Year + date_calc.Month.ToString("00") + "_" + nzp_wp + "_up";
                    var tableNameDown = "peni_debt_" + date_calc.Year + date_calc.Month.ToString("00") + "_" + nzp_wp + "_down";
                    var fullTableNameUp = schema + Database.TableDelimiter + tableNameUp;
                    var fullTableNameDown = schema + Database.TableDelimiter + tableNameDown;

                    CheckExistTablePeni(nzp_wp, date_calc, CurrentPrefix, TablesForPeniCalc.PeniDebt);

                    Database.ExecuteNonQuery(" INSERT INTO " + fullTableNameUp +
                                             " (id, nzp_kvar, num_ls, nzp_supp, nzp_serv, nzp_wp, s_peni_type_debt_id, date_from, date_to," +
                                             " sum_debt, over_payments, sum_debt_result, cnt_days, cnt_days_with_prm, sum_peni," +
                                             " date_calc, created_on, created_by, peni_actions_id, peni_calc)" +
                                             " SELECT id, nzp_kvar, num_ls, nzp_supp, nzp_serv, nzp_wp, s_peni_type_debt_id, date_from, date_to," +
                                             " sum_debt, over_payments, sum_debt_result, cnt_days, cnt_days_with_prm, sum_peni," +
                                             " date_calc, created_on, created_by, peni_actions_id, peni_calc" +
                                             " FROM " + GetFullTableName(centralTable) +
                                             " WHERE nzp_wp=" + nzp_wp +
                                             " AND date_trunc('month',date_calc)::DATE=" + date_calc.ToShortDateStringWithQuote() +
                                             " AND s_peni_type_debt_id=1");

                    Database.ExecuteNonQuery(" INSERT INTO " + fullTableNameDown +
                                             " (id, nzp_kvar, num_ls, nzp_supp, nzp_serv, nzp_wp, s_peni_type_debt_id, date_from, date_to," +
                                             " sum_debt, over_payments, sum_debt_result, cnt_days, cnt_days_with_prm, sum_peni," +
                                             " date_calc, created_on, created_by, peni_actions_id, peni_calc)" +
                                             " SELECT id, nzp_kvar, num_ls, nzp_supp, nzp_serv, nzp_wp, s_peni_type_debt_id, date_from, date_to," +
                                             " sum_debt, over_payments, sum_debt_result, cnt_days, cnt_days_with_prm, sum_peni," +
                                             " date_calc, created_on, created_by, peni_actions_id, peni_calc" +
                                             " FROM " + GetFullTableName(centralTable) +
                                             " WHERE nzp_wp=" + nzp_wp +
                                             " AND date_trunc('month',date_calc)::DATE=" + date_calc.ToShortDateStringWithQuote() +
                                             " AND s_peni_type_debt_id=2");
                }
            }
        }

        private void ReplaceDataFromPeniCalc(SchemaQualifiedObjectName centralTable)
        {
            var nzp_wp = (int)Database.ExecuteScalar("SELECT nzp_wp FROM " + CentralKernel + Database.TableDelimiter +
                                                    "s_point WHERE bd_kernel='" + CurrentPrefix + "'");

            using (var reader = Database.ExecuteReader(" SELECT DISTINCT date_trunc('year',date_calc)::DATE as date_calc" +
                                                       " FROM " + GetFullTableName(centralTable) + " WHERE nzp_wp=" + nzp_wp))
            {
                while (reader.Read())
                {
                    var date_calc = Convert.ToDateTime(reader["date_calc"]);
                    var schema = CurrentPrefix + "_charge_" + (date_calc.Year - 2000).ToString("00");
                    var tableName = "peni_calc_" + date_calc.Year + "_" + nzp_wp;
                    var fullTableName = schema + Database.TableDelimiter + tableName;

                    CheckExistTablePeni(nzp_wp, date_calc, CurrentPrefix, TablesForPeniCalc.PeniCalc);

                    Database.ExecuteNonQuery(" INSERT INTO " + fullTableName +
                                             " (id, nzp_kvar, num_ls, nzp_supp, nzp_wp, date_from, date_to, sum_peni," +
                                             "  sum_old_reval, sum_new_reval, date_calc, created_on, created_by, peni_actions_id)" +
                                             " SELECT id, nzp_kvar, num_ls, nzp_supp, nzp_wp, date_from, date_to, sum_peni," +
                                             "  sum_old_reval, sum_new_reval, date_calc, created_on, created_by, peni_actions_id" +
                                             " FROM " + GetFullTableName(centralTable) +
                                             " WHERE nzp_wp=" + nzp_wp +
                                             " AND date_trunc('year',date_calc)::DATE=" + date_calc.ToShortDateStringWithQuote());
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
