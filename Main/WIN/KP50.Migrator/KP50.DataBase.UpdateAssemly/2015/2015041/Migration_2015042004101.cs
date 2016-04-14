using System;
using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015041
{
    // Добавление таблицы, которая позволяет однозначно выбрать норматив, по которому выставлялись значения параметра типа norm
    [Migration(2015042004101, MigrateDataBase.CentralBank)]
    public class Migration_2015042004101_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            // TODO: Upgrade CentralPref_Kernel
            SchemaQualifiedObjectName norm_prm_serv = new SchemaQualifiedObjectName() { Name = "norm_prm_serv", Schema = CurrentSchema };

            if (!Database.TableExists(norm_prm_serv))
            {
                Database.AddTable(norm_prm_serv,
                    new Column("id", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("nzp_prm", DbType.Int32, ColumnProperty.NotNull),
                    new Column("nzp_serv", DbType.Int32, ColumnProperty.NotNull),
                    new Column("nzp_measure", DbType.Int32, ColumnProperty.NotNull)
                    );
            }


            if (Database.TableExists(norm_prm_serv))
            {
                var count =
              Convert.ToInt32(
                  Database.ExecuteScalar("SELECT COUNT(1) FROM " + norm_prm_serv.Schema + Database.TableDelimiter +
                                         norm_prm_serv.Name));
                if (count == 0)
                {
                    Database.Insert(norm_prm_serv,
                        new[] { "id", "nzp_prm", "nzp_serv", "nzp_measure" }, new[] { "1", "7", "6", "3" });
                    Database.Insert(norm_prm_serv,
                        new[] { "id", "nzp_prm", "nzp_serv", "nzp_measure" }, new[] { "2", "38", "9", "3" });
                    Database.Insert(norm_prm_serv,
                        new[] { "id", "nzp_prm", "nzp_serv", "nzp_measure" }, new[] { "3", "463", "9", "3" });
                    Database.Insert(norm_prm_serv,
                        new[] { "id", "nzp_prm", "nzp_serv", "nzp_measure" }, new[] { "4", "1464", "510", "3" });
                    Database.Insert(norm_prm_serv,
                        new[] { "id", "nzp_prm", "nzp_serv", "nzp_measure" }, new[] { "5", "2050", "515", "5" });

                }
            }

            SetSchema(Bank.Data);
            // TODO: Upgrade CentralPref_Data

            SetSchema(Bank.Upload);
            // TODO: Upgrade CentralPref_Upload
        }

        public override void Revert()
        {
            SetSchema(Bank.Kernel);
            // TODO: Downgrade CentralPref_Kernel

            SetSchema(Bank.Data);
            // TODO: Downgrade CentralPref_Data

            SetSchema(Bank.Upload);
            // TODO: Downgrade CentralPref_Upload
        }
    }

}
