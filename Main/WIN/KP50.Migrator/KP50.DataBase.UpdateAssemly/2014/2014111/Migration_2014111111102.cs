using System;
using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2014111111102, MigrateDataBase.CentralBank)]
    public class Migration_2014111111102_CentralBank : Migration
    {
        private void ConsoleLog(string bank)
        {
            Console.WriteLine("Applying 2014111111102: Migration 2014111111102 Bank: " + bank);
        }

        public override void Apply()
        {

            #region Индексы для проводок
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
                                //индексы по проводкам
                                SchemaQualifiedObjectName peni_prov = new SchemaQualifiedObjectName();
                                peni_prov.Schema = pref + "_charge_" + (year - 2000).ToString("00");
                                for (int i = 1; i <= 12; i++)
                                {
                                    peni_prov.Name = "peni_provodki_" + year.ToString("0000") + i.ToString("00") + "_" + nzp_wp;
                                    if (Database.TableExists(peni_prov))
                                    {
                                        if (
                                            !Database.IndexExists(
                                                "ix6_" + peni_prov.Name + "num_ls_nzp_serv_nzp_supp_date_obligation",
                                                peni_prov))
                                        {
                                            Database.AddIndex(
                                                "ix6_" + peni_prov.Name + "num_ls_nzp_serv_nzp_supp_date_obligation",
                                                false,
                                                peni_prov, "num_ls", "nzp_serv", "nzp_supp", "date_obligation");
                                        }

                                    }
                                }
                                //индексы по архивам проводок
                                peni_prov.Schema = pref + "_charge_" + (year - 2000).ToString("00");
                                for (int i = 1; i <= 12; i++)
                                {
                                    peni_prov.Name = "peni_provodki_arch_" + year.ToString("0000") + i.ToString("00") + "_" + nzp_wp;
                                    if (Database.TableExists(peni_prov))
                                    {
                                        if (
                                            !Database.IndexExists(
                                                "ix6_" + peni_prov.Name + "num_ls_nzp_serv_nzp_supp_date_obligation",
                                                peni_prov))
                                        {
                                            Database.AddIndex(
                                                "ix6_" + peni_prov.Name + "num_ls_nzp_serv_nzp_supp_date_obligation",
                                                false,
                                                peni_prov, "num_ls", "nzp_serv", "nzp_supp", "date_obligation");
                                        }
                                    }
                                }

                            }
                        }
                    }
                }
                ConsoleLog(pref);

            }
            #endregion


            #region Добавление параметров
            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName prm_name = new SchemaQualifiedObjectName();
            prm_name.Schema = CurrentSchema;
            prm_name.Name = "prm_name";

            //добавление в список параметров
            if (Database.TableExists(prm_name))
            {
                Database.Delete(prm_name, "nzp_prm = 1375");
                Database.Insert(
                    prm_name,
                    new[] { "nzp_prm", "name_prm", "type_prm", "prm_num" },
                    new[] { "1375", "Число дней до даты обязательств по оплатам(для пени)", "int", "10" });

                Database.Delete(prm_name, "nzp_prm = 1382");
                Database.Insert(
                    prm_name,
                    new[] { "nzp_prm", "name_prm", "type_prm", "prm_num" },
                    new[] { "1382", "Новый алгоритм расчета пени", "bool", "10" });
            }

            //для всех локальных банков
            reader = Database.ExecuteReader("SELECT * from " + CentralKernel + Database.TableDelimiter + "s_point");
            while (reader.Read())
            {
                var pref = reader["bd_kernel"] != DBNull.Value ? reader["bd_kernel"].ToString().Trim() : "";
                prm_name.Schema = pref + "_kernel";
                if (Database.TableExists(prm_name))
                {
                    Database.Delete(prm_name, "nzp_prm = 1375");
                    Database.Insert(
                        prm_name,
                        new[] { "nzp_prm", "name_prm", "type_prm", "prm_num" },
                        new[] { "1375", "Число дней до даты обязательств по оплатам(для пени)", "int", "10" });

                    Database.Delete(prm_name, "nzp_prm = 1382");
                    Database.Insert(
                        prm_name,
                        new[] { "nzp_prm", "name_prm", "type_prm", "prm_num" },
                        new[] { "1382", "Новый алгоритм расчета пени", "bool", "10" });
                }
            }


            #endregion
        }

    }
}