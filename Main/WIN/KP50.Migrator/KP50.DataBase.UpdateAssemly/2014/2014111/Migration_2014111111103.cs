using System;
using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2014111111103, MigrateDataBase.CentralBank)]
    public class Migration_2014111111103_CentralBank : Migration
    {
        private void ConsoleLog(string bank)
        {
            Console.WriteLine("Applying 2014111111103: Migration 2014111111103 Bank: " + bank);
        }

        public override void Apply()
        {

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
                                SchemaQualifiedObjectName peni_debt = new SchemaQualifiedObjectName();
                                peni_debt.Schema = pref + "_charge_" + (year - 2000).ToString("00");

                                peni_debt.Name = "peni_debt_" + year.ToString("0000") + "_" + nzp_wp;
                                if (Database.TableExists(peni_debt))
                                {
                                    if (
                                        !Database.IndexExists(
                                            "ix6_" + peni_debt.Name + "num_ls_nzp_serv_nzp_supp_date_from_date_to",
                                            peni_debt))
                                    {
                                        Database.AddIndex(
                                            "ix6_" + peni_debt.Name + "num_ls_nzp_serv_nzp_supp_date_from_date_to",
                                            false,
                                            peni_debt, "num_ls", "nzp_kvar", "nzp_serv", "nzp_supp", "date_from", "date_to");
                                    }

                                }

                                SchemaQualifiedObjectName peni_calc = new SchemaQualifiedObjectName();
                                //индексы по пени
                                peni_calc.Schema = pref + "_charge_" + (year - 2000).ToString("00");
                                peni_calc.Name = "peni_calc_" + year.ToString("0000") + "_" + nzp_wp;
                                if (Database.TableExists(peni_calc))
                                {
                                    if (
                                        !Database.IndexExists(
                                            "ix6_" + peni_calc.Name + "num_ls_nzp_supp_date_from_date_to",
                                            peni_calc))
                                    {
                                        Database.AddIndex(
                                            "ix6_" + peni_calc.Name + "num_ls_nzp_supp_date_from_date_to",
                                            false,
                                            peni_calc, "num_ls", "nzp_kvar", "nzp_supp", "date_from", "date_to");
                                    }
                                }


                            }
                        }
                    }
                }
                ConsoleLog(pref);

            }
            #endregion


            #region Редактирование справочника и реестра

            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName peni_actions_type = new SchemaQualifiedObjectName();
            peni_actions_type.Schema = CurrentSchema;
            peni_actions_type.Name = "peni_actions_type";

            if (Database.TableExists(peni_actions_type))
            {
                Database.RemoveTable(peni_actions_type);
            }
            if (!Database.TableExists(peni_actions_type))
            {
                Database.AddTable(peni_actions_type,
                 new Column("id", DbType.Int32, ColumnProperty.PrimaryKeyWithIdentity | ColumnProperty.NotNull),
                 new Column("type", DbType.String));

                if (Database.TableExists(peni_actions_type))
                {
                    Database.Insert(peni_actions_type, new string[] { "id", "type" }, new string[] { "1", "Запись проводок по закрытому опердню" });
                    Database.Insert(peni_actions_type, new string[] { "id", "type" }, new string[] { "2", "Запись проводок по закрытому месяцу" });
                    Database.Insert(peni_actions_type, new string[] { "id", "type" }, new string[] { "3", "Архивация проводок" });
                    Database.Insert(peni_actions_type, new string[] { "id", "type" }, new string[] { "4", "Запись задолженностей и пени" });
                    Database.Insert(peni_actions_type, new string[] { "id", "type" }, new string[] { "5", "Запись проводок для первого запуска расчета пени в банке данных" });
                }
            }

            SetSchema(Bank.Data);
            SchemaQualifiedObjectName peni_actions = new SchemaQualifiedObjectName();
            peni_actions.Schema = CurrentSchema;
            peni_actions.Name = "peni_actions";

            if (Database.TableExists(peni_actions))
            {
                if (!Database.ColumnExists(peni_actions, "date_from"))
                {
                    Database.AddColumn(peni_actions, new Column("date_from", DbType.Date));
                }
                if (!Database.ColumnExists(peni_actions, "date_to"))
                {
                    Database.AddColumn(peni_actions, new Column("date_to", DbType.Date));
                }
                if (!Database.ColumnExists(peni_actions, "nzp_wp"))
                {
                    Database.AddColumn(peni_actions, new Column("nzp_wp", DbType.Int32));
                }
            }


            SchemaQualifiedObjectName peni_prov = new SchemaQualifiedObjectName();
            peni_prov.Schema = CurrentSchema;
            peni_prov.Name = "peni_provodki";
            if (Database.TableExists(peni_prov))
            {
                if (!Database.ColumnExists(peni_prov, "peni_debt_id"))
                {
                    Database.AddColumn(peni_prov, new Column("peni_debt_id", DbType.Int32));
                }
            }

            #endregion
        }

    }
}