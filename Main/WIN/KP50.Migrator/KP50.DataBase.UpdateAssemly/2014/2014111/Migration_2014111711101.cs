using System;
using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2014111711101, MigrateDataBase.CentralBank)]
    public class Migration_2014111711101_CentralBank : Migration
    {
      

        public override void Apply()
        {

            #region Добавление параметров
            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName prm_name = new SchemaQualifiedObjectName();
            prm_name.Schema = CurrentSchema;
            prm_name.Name = "prm_name";

            //добавление в список параметров
            if (Database.TableExists(prm_name))
            {
                Database.Delete(prm_name, "nzp_prm = 1382");
                Database.Insert(
                    prm_name,
                    new[] { "nzp_prm", "name_prm", "type_prm", "prm_num", "nzp_res" },
                    new[] { "1382", "Алгоритм расчета пени", "sprav", "10", "3024" });
            }

            //для всех локальных банков
            var reader = Database.ExecuteReader("SELECT * from " + CentralKernel + Database.TableDelimiter + "s_point");
            while (reader.Read())
            {
                var pref = reader["bd_kernel"] != DBNull.Value ? reader["bd_kernel"].ToString().Trim() : "";
                prm_name.Schema = pref + "_kernel";
                if (Database.TableExists(prm_name))
                {

                    Database.Delete(prm_name, "nzp_prm = 1382");
                    Database.Insert(
                        prm_name,
                        new[] { "nzp_prm", "name_prm", "type_prm", "prm_num", "nzp_res" },
                        new[] { "1382", "Алгоритм расчета пени", "sprav", "10", "3024" });
                }
            }

            #endregion

            #region Добавление справочника
            SchemaQualifiedObjectName resolution = new SchemaQualifiedObjectName();
            resolution.Schema = CurrentSchema;
            resolution.Name = "resolution";

            SchemaQualifiedObjectName res_y = new SchemaQualifiedObjectName();
            res_y.Schema = CurrentSchema;
            res_y.Name = "res_y";

            SchemaQualifiedObjectName res_x = new SchemaQualifiedObjectName();
            res_x.Schema = CurrentSchema;
            res_x.Name = "res_x";

            SchemaQualifiedObjectName res_values = new SchemaQualifiedObjectName();
            res_values.Schema = CurrentSchema;
            res_values.Name = "res_values";

            //добавление в список параметров
            if (Database.TableExists(resolution))
            {
                Database.Delete(resolution, "nzp_res = 3024");
                Database.Insert(
                    resolution,
                    new[] { "nzp_res", "name_short", "name_res", "is_readonly" },
                    new[] { "3024", "ТТипАлгПени", "Алгоритм расчета пени", "1" });
                if (Database.TableExists(res_y))
                {
                    Database.Delete(res_y, "nzp_res = 3024");
                    Database.Insert(
                       res_y,
                       new[] { "nzp_res", "nzp_y", "name_y" },
                       new[] { "3024", "1", "Стандартный расчет пени" });
                    Database.Insert(
                        res_y,
                        new[] { "nzp_res", "nzp_y", "name_y" },
                        new[] { "3024", "2", "Новый расчет пени" });

                    if (Database.TableExists(res_x))
                    {
                        Database.Delete(res_x, "nzp_res = 3024");
                        Database.Insert(
                           res_x,
                           new[] { "nzp_res", "nzp_x", "name_x" },
                           new[] { "3024", "1", "-" });
                        if (Database.TableExists(res_values))
                        {
                            Database.Delete(res_values, "nzp_res = 3024");
                            Database.Insert(
                               res_values,
                               new[] { "nzp_res", "nzp_y", "nzp_x" },
                               new[] { "3024", "1", "1" });
                            Database.Insert(
                              res_values,
                              new[] { "nzp_res", "nzp_y", "nzp_x" },
                              new[] { "3024", "2", "1" });
                            Database.Insert(
                            res_values,
                            new[] { "nzp_res", "nzp_y", "nzp_x" },
                            new[] { "3024", "3", "1" });
                        }
                    }
                }

            }


            //для всех локальных банков
            reader = Database.ExecuteReader("SELECT * from " + CentralKernel + Database.TableDelimiter + "s_point");
            while (reader.Read())
            {
                var pref = reader["bd_kernel"] != DBNull.Value ? reader["bd_kernel"].ToString().Trim() : "";
                resolution.Schema = pref + "_kernel";
                res_y.Schema = pref + "_kernel";
                res_x.Schema = pref + "_kernel";
                res_values.Schema = pref + "_kernel";

                //добавление в список параметров
                if (Database.TableExists(resolution))
                {
                    Database.Delete(resolution, "nzp_res = 3024");
                    Database.Insert(
                        resolution,
                        new[] { "nzp_res", "name_short", "name_res", "is_readonly" },
                        new[] { "3024", "ТТипАлгПени", "Алгоритм расчета пени", "1" });
                    if (Database.TableExists(res_y))
                    {
                        Database.Delete(res_y, "nzp_res = 3024");
                        Database.Insert(
                           res_y,
                           new[] { "nzp_res", "nzp_y", "name_y" },
                           new[] { "3024", "1", "Стандартный расчет пени" });
                        Database.Insert(
                            res_y,
                            new[] { "nzp_res", "nzp_y", "name_y" },
                            new[] { "3024", "2", "Новый расчет пени" });

                        if (Database.TableExists(res_x))
                        {
                            Database.Delete(res_x, "nzp_res = 3024");
                            Database.Insert(
                               res_x,
                               new[] { "nzp_res", "nzp_x", "name_x" },
                               new[] { "3024", "1", "-" });
                            if (Database.TableExists(res_values))
                            {
                                Database.Delete(res_values, "nzp_res = 3024");
                                Database.Insert(
                                   res_values,
                                   new[] { "nzp_res", "nzp_y", "nzp_x" },
                                   new[] { "3024", "1", "1" });
                                Database.Insert(
                                  res_values,
                                  new[] { "nzp_res", "nzp_y", "nzp_x" },
                                  new[] { "3024", "2", "1" });
                                Database.Insert(
                                res_values,
                                new[] { "nzp_res", "nzp_y", "nzp_x" },
                                new[] { "3024", "3", "1" });
                            }
                        }
                    }

                }
            }


            #endregion

        }

    }
}