using System;
using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2014112711301, MigrateDataBase.CentralBank)]
    public class Migration_2014112711301_CentralBank : Migration
    {
        public override void Apply()
        {
            if (Database.ProviderName == "PostgreSQL")
            {
                #region Индексы для мастер-таблиц
                SetSchema(Bank.Data);

                SchemaQualifiedObjectName peni_prov = new SchemaQualifiedObjectName();
                peni_prov.Schema = CurrentSchema;
                peni_prov.Name = "peni_provodki";

                if (Database.TableExists(peni_prov))
                {
                    if (
                       !Database.IndexExists(
                           "ix9_" + peni_prov.Name + "_index",
                           peni_prov))
                    {
                        Database.AddIndex(
                            "ix9_" + peni_prov.Name + "_index",
                            true,
                            peni_prov, "id");
                    }

                    if (
                        !Database.IndexExists(
                            "ix10_" + peni_prov.Name + "_index",
                            peni_prov))
                    {
                        Database.AddIndex(
                            "ix10_" + peni_prov.Name + "_index",
                            false,
                            peni_prov, "nzp_kvar", "nzp_serv", "nzp_supp", "date_obligation", "nzp_wp");
                    }

                }


                SchemaQualifiedObjectName peni_debt = new SchemaQualifiedObjectName();
                peni_debt.Schema = CurrentSchema;
                peni_debt.Name = "peni_debt";
                if (Database.TableExists(peni_debt))
                {
                    if (
                        !Database.IndexExists(
                            "ix3_" + peni_debt.Name + "_index",
                            peni_debt))
                    {
                        Database.AddIndex(
                            "ix3_" + peni_debt.Name + "_index",
                            false,
                            peni_debt, "nzp_kvar");
                    }
                    if (
                        !Database.IndexExists(
                            "ix4_" + peni_debt.Name + "_index",
                            peni_debt))
                    {
                        Database.ExecuteNonQuery(" CREATE INDEX  ix4_" + peni_debt.Name + "_index " +
                                                 " ON " + CentralData + Database.TableDelimiter + "peni_debt (sum_debt,peni_calc,peni_actions_id,s_peni_type_debt_id) " +
                                                 " WHERE sum_debt>0 and peni_calc=true and s_peni_type_debt_id<>2");
                    }
                }

                SchemaQualifiedObjectName peni_calc = new SchemaQualifiedObjectName();
                peni_calc.Schema = CurrentSchema;
                peni_calc.Name = "peni_calc";
                if (Database.TableExists(peni_calc))
                {
                    if (
                        !Database.IndexExists(
                            "ix3_" + peni_calc.Name + "_index",
                            peni_calc))
                    {
                        Database.AddIndex(
                            "ix3_" + peni_calc.Name + "_index",
                            false,
                            peni_calc, "nzp_kvar");
                    }
                    if (
                       !Database.IndexExists(
                           "ix4_" + peni_calc.Name + "_index",
                           peni_calc))
                    {
                        Database.AddIndex(
                            "ix4_" + peni_calc.Name + "_index",
                            false,
                            peni_calc, "num_ls");
                    }
                }

                #endregion

                #region Индексы для задолжностей  и пени
                SetSchema(Bank.Kernel);

                IDataReader reader = Database.ExecuteReader("SELECT * from " + CentralKernel + Database.TableDelimiter + "s_point");

                while (reader.Read())
                {
                    var pref = reader["bd_kernel"] != DBNull.Value ? reader["bd_kernel"].ToString().Trim() : "";
                    var nzp_wp = reader["nzp_wp"] != DBNull.Value ? Convert.ToInt32(reader["nzp_wp"]) : 0;

                    if (pref != "" && nzp_wp > 0)
                    {
                        IDataReader reader2 =
                            Database.ExecuteReader("SELECT yearr FROM " + CentralKernel + Database.TableDelimiter + "s_baselist WHERE nzp_wp=" + nzp_wp +
                                                   " GROUP BY yearr");
                        while (reader2.Read())
                        {
                            var year = reader2["yearr"] != DBNull.Value ? Convert.ToInt32(reader2["yearr"]) : 0;
                            if (year > 0)
                            {
                                Database.ExecuteNonQuery("SET search_path = " + pref + "_charge_" +
                                                         (year - 2000).ToString("00"));
                                //костыль на проверку существования схемы
                                if (Database.TableExists("charge_01"))
                                {
                                    //индексы по задолженностям
                                    peni_debt = new SchemaQualifiedObjectName();
                                    peni_debt.Schema = pref + "_charge_" + (year - 2000).ToString("00");

                                    peni_debt.Name = "peni_debt_" + year.ToString("0000") + "_" + nzp_wp;
                                    if (Database.TableExists(peni_debt))
                                    {
                                        if (!Database.IndexExists("ix3_" + peni_debt.Name + "_index", peni_debt))
                                        {
                                            Database.AddIndex(
                                                "ix3_" + peni_debt.Name + "_index",
                                                false,
                                                peni_debt, "nzp_kvar");
                                        }
                                        if (!Database.IndexExists("ix4_" + peni_debt.Name + "_index", peni_debt))
                                        {
                                            Database.ExecuteNonQuery(" CREATE INDEX  ix4_" + peni_debt.Name + "_index " +
                                                                     " ON " + peni_debt.Name +
                                                                     " (sum_debt,peni_calc,peni_actions_id,s_peni_type_debt_id) " +
                                                                     " WHERE sum_debt>0 and peni_calc=true and s_peni_type_debt_id<>2");
                                        }

                                    }

                                    peni_calc = new SchemaQualifiedObjectName();
                                    //индексы по пени
                                    peni_calc.Schema = pref + "_charge_" + (year - 2000).ToString("00");
                                    peni_calc.Name = "peni_calc_" + year.ToString("0000") + "_" + nzp_wp;
                                    if (Database.TableExists(peni_calc))
                                    {

                                        if (!Database.IndexExists("ix3_" + peni_calc.Name + "_index", peni_calc))
                                        {
                                            Database.AddIndex("ix3_" + peni_calc.Name + "_index", false, peni_calc, "nzp_kvar");
                                        }
                                        if (!Database.IndexExists("ix4_" + peni_calc.Name + "_index", peni_calc))
                                        {
                                            Database.AddIndex("ix4_" + peni_calc.Name + "_index", false, peni_calc, "num_ls");
                                        }
                                    }


                                }
                            }
                        }
                    }

                }
                #endregion

                #region Индексы по дочерним таблицам проводок
                SetSchema(Bank.Kernel);

                reader = Database.ExecuteReader("SELECT * from " + CentralKernel + Database.TableDelimiter + "s_point");

                while (reader.Read())
                {
                    var pref = reader["bd_kernel"] != DBNull.Value ? reader["bd_kernel"].ToString().Trim() : "";
                    var nzp_wp = reader["nzp_wp"] != DBNull.Value ? Convert.ToInt32(reader["nzp_wp"]) : 0;

                    if (pref != "" && nzp_wp > 0)
                    {
                        IDataReader reader2 =
                            Database.ExecuteReader("SELECT yearr FROM " + CentralKernel + Database.TableDelimiter + "s_baselist WHERE nzp_wp=" + nzp_wp +
                                                   " GROUP BY yearr");
                        while (reader2.Read())
                        {
                            var year = reader2["yearr"] != DBNull.Value ? Convert.ToInt32(reader2["yearr"]) : 0;
                            if (year > 0)
                            {
                                Database.ExecuteNonQuery("SET search_path = " + pref + "_charge_" +
                                                         (year - 2000).ToString("00"));
                                for (int i = 1; i <= 12; i++)
                                {//костыль на проверку существования схемы
                                    if (Database.TableExists("charge_01"))
                                    {
                                        //индексы по задолженностям
                                        peni_prov = new SchemaQualifiedObjectName();
                                        peni_prov.Schema = pref + "_charge_" + (year - 2000).ToString("00");
                                        peni_prov.Name = "peni_provodki_" + year.ToString("0000") + i.ToString("00") + "_" + nzp_wp;
                                        if (Database.TableExists(peni_prov))
                                        {
                                            if (!Database.IndexExists("ix9_" + peni_prov.Name + "_index", peni_prov))
                                            {
                                                Database.AddIndex("ix9_" + peni_prov.Name + "_index", true, peni_prov, "id");
                                            }
                                            if (!Database.IndexExists("ix10_" + peni_prov.Name + "_index", peni_prov))
                                            {
                                                Database.AddIndex("ix10_" + peni_prov.Name + "_index", false,
                                                    peni_prov, "nzp_kvar", "nzp_serv", "nzp_supp", "date_obligation", "nzp_wp");
                                            }

                                        }

                                    }
                                }

                            }
                        }
                    }

                }
                #endregion

                reader.Close();
            }
        }

        public override void Revert()
        {

        }
    }
}
