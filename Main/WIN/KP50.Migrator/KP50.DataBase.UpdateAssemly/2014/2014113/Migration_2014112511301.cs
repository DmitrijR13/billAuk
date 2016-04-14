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
    [Migration(2014112511301, MigrateDataBase.CentralBank)]
    public class Migration_2014112511301_CentralBank : Migration
    {
        public override void Apply()
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
                        "ix7_" + peni_prov.Name + "peni_debt_id_nzp_wp_date_obligation",
                        peni_prov))
                {
                    Database.AddIndex(
                        "ix7_" + peni_prov.Name + "peni_debt_id_nzp_wp_date_obligation",
                        false,
                        peni_prov, "peni_debt_id", "nzp_wp", "date_obligation");
                }

                if (
                    !Database.IndexExists(
                        "ix8_" + peni_prov.Name + "peni_debt_id",
                        peni_prov))
                {
                    Database.AddIndex(
                        "ix8_" + peni_prov.Name + "peni_debt_id",
                        false,
                        peni_prov, "peni_debt_id");
                }
            }


            SchemaQualifiedObjectName peni_debt = new SchemaQualifiedObjectName();
            peni_debt.Schema = CurrentSchema;
            peni_debt.Name = "peni_debt";
            if (Database.TableExists(peni_debt))
            {
                if (
                    !Database.IndexExists(
                        "ix7_" + peni_debt.Name + "nzp_kvar_nzp_supp_peni_actions_id",
                        peni_debt))
                {
                    Database.AddIndex(
                        "ix7_" + peni_debt.Name + "nzp_kvar_nzp_supp_peni_actions_id",
                        false,
                        peni_debt, "nzp_kvar", "nzp_supp", "peni_actions_id");
                }
            }

            SchemaQualifiedObjectName peni_calc = new SchemaQualifiedObjectName();
            peni_calc.Schema = CurrentSchema;
            peni_calc.Name = "peni_calc";
            if (Database.TableExists(peni_calc))
            {
                if (
                    !Database.IndexExists(
                        "ix7_" + peni_calc.Name + "nzp_kvar_nzp_supp_peni_actions_id",
                        peni_calc))
                {
                    Database.AddIndex(
                        "ix7_" + peni_calc.Name + "nzp_kvar_nzp_supp_peni_actions_id",
                        false,
                        peni_calc, "nzp_kvar", "nzp_supp", "peni_actions_id");
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
                                    if (!Database.IndexExists("ix7_" + peni_debt.Name + "nzp_kvar_nzp_supp_peni_actions_id",
                                         peni_debt))
                                    {
                                        Database.AddIndex(
                                            "ix7_" + peni_debt.Name + "nzp_kvar_nzp_supp_peni_actions_id",
                                            false,
                                            peni_debt, "nzp_kvar", "nzp_supp", "peni_actions_id");
                                    }

                                }

                                peni_calc = new SchemaQualifiedObjectName();
                                //индексы по пени
                                peni_calc.Schema = pref + "_charge_" + (year - 2000).ToString("00");
                                peni_calc.Name = "peni_calc_" + year.ToString("0000") + "_" + nzp_wp;
                                if (Database.TableExists(peni_calc))
                                {

                                    if (
                                        !Database.IndexExists(
                                            "ix7_" + peni_calc.Name + "nzp_kvar_nzp_supp_peni_actions_id",
                                            peni_calc))
                                    {
                                        Database.AddIndex(
                                            "ix7_" + peni_calc.Name + "nzp_kvar_nzp_supp_peni_actions_id",
                                            false,
                                            peni_calc, "nzp_kvar", "nzp_supp", "peni_actions_id");
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
                                        if (!Database.IndexExists(
                                               "ix7_" + peni_prov.Name + "peni_debt_id_nzp_wp_date_obligation",
                                               peni_prov))
                                        {
                                            Database.AddIndex(
                                                "ix7_" + peni_prov.Name + "peni_debt_id_nzp_wp_date_obligation",
                                                false,
                                                peni_prov, "peni_debt_id", "nzp_wp", "date_obligation");
                                        }

                                        if (
                                            !Database.IndexExists(
                                                "ix8_" + peni_prov.Name + "peni_debt_id",
                                                peni_prov))
                                        {
                                            Database.AddIndex(
                                                "ix8_" + peni_prov.Name + "peni_debt_id",
                                                false,
                                                peni_prov, "peni_debt_id");
                                        }

                                    }

                                }
                            }

                        }
                    }
                }

            }
            #endregion

        }

        public override void Revert()
        {

        }
    }
}
