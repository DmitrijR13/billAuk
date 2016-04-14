using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015064
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2015081406401, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_2015081406401_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            var prm_name = new SchemaQualifiedObjectName { Name = "prm_name", Schema = CurrentSchema };
            var serv_odn = new SchemaQualifiedObjectName { Name = "serv_odn", Schema = CurrentSchema };
            var resolution = new SchemaQualifiedObjectName { Name = "resolution", Schema = CurrentSchema };
            var res_y = new SchemaQualifiedObjectName { Name = "res_y", Schema = CurrentSchema };


            if (Database.TableExists(prm_name))
            {
                Database.ExecuteNonQuery(" INSERT INTO " + prm_name.Schema + Database.TableDelimiter + prm_name.Name +
                                             " (nzp_prm, name_prm, type_prm, nzp_res, prm_num) " +
                                             " SELECT 631, 'Способ управления дома', 'sprav', 10000, 2 " +
                                             " FROM " + serv_odn.Schema + Database.TableDelimiter + serv_odn.Name +
                                             " WHERE nzp_serv_link = 6 " +
                                             " AND NOT EXISTS (SELECT 1 FROM " + prm_name.Schema +
                                             Database.TableDelimiter + prm_name.Name + " WHERE nzp_prm = 631);");

                Database.ExecuteNonQuery(" INSERT INTO " + resolution.Schema + Database.TableDelimiter + resolution.Name +
                                             " (nzp_res, name_short, name_res) " +
                                             " SELECT 10000, 'СУМКД', 'таблица способов управления МКД' " +
                                             " FROM " + serv_odn.Schema + Database.TableDelimiter + serv_odn.Name +
                                             " WHERE nzp_serv_link = 6 " +
                                             " AND NOT EXISTS (SELECT 1 FROM " + resolution.Schema +
                                             Database.TableDelimiter + resolution.Name + " WHERE nzp_res = 10000);");

                Database.ExecuteNonQuery(" DELETE FROM " + res_y.Schema + Database.TableDelimiter + res_y.Name +
                                         " WHERE nzp_res = 10000");

                Database.ExecuteNonQuery(" INSERT INTO " + res_y.Schema + Database.TableDelimiter + res_y.Name +
                                         " (nzp_res, nzp_y, name_y) " +
                                         " VALUES (10000, 1, 'ТСЖ')");
                Database.ExecuteNonQuery(" INSERT INTO " + res_y.Schema + Database.TableDelimiter + res_y.Name +
                                         " (nzp_res, nzp_y, name_y) " +
                                         " VALUES (10000, 2, 'ЖК(жилищный кооператив)')");
                Database.ExecuteNonQuery(" INSERT INTO " + res_y.Schema + Database.TableDelimiter + res_y.Name +
                                         " (nzp_res, nzp_y, name_y) " +
                                         " VALUES (10000, 3, 'ЖСК(жилищно-строительный кооператив)')");
                Database.ExecuteNonQuery(" INSERT INTO " + res_y.Schema + Database.TableDelimiter + res_y.Name +
                                         " (nzp_res, nzp_y, name_y) " +
                                         " VALUES (10000, 4, 'УК(управляющая компания)')");
                Database.ExecuteNonQuery(" INSERT INTO " + res_y.Schema + Database.TableDelimiter + res_y.Name +
                                         " (nzp_res, nzp_y, name_y) " +
                                         " VALUES (10000, 5, 'НСУ(непосредственное управление)')");
            }
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
