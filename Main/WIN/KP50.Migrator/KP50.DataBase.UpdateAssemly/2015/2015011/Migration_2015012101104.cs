using System;
using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2014._2014124
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2015012101104, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_2015012101104_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName prm_name = new SchemaQualifiedObjectName() { Name = "prm_name", Schema = CurrentSchema };

            var count =
                Convert.ToInt32(
                    Database.ExecuteScalar("SELECT count(*) FROM " + prm_name.Schema + Database.TableDelimiter +
                                           prm_name.Name + " WHERE nzp_prm=1987"));

            if (count > 0)
            {
                Database.Update(prm_name,
                    new[] { "nzp_prm", "name_prm", "type_prm", "prm_num" },
                    new[] { "1987", "Отменить расчет средних расходов ИПУ при расчете всей БД", "bool", "10" },
                    " nzp_prm=1987");
            }
            else
            {
                //1987|Отменить расчет средних расходов ИПУ при расчете всей БД|||bool||10||||
                Database.Insert(prm_name,
                new[] { "nzp_prm", "name_prm", "type_prm", "prm_num", "low_", "high_", "digits_" },
                new[] { "1987", "Отменить расчет средних расходов ИПУ при расчете всей БД", "bool", "10", null, null, null });
            }

            count =
                  Convert.ToInt32(
                      Database.ExecuteScalar("SELECT count(*) FROM " + prm_name.Schema + Database.TableDelimiter +
                                             prm_name.Name + " WHERE nzp_prm=1979"));

            if (count > 0)
            {
                Database.Update(prm_name,
                    new[] { "nzp_prm", "name_prm", "type_prm", "prm_num" },
                    new[] { "1979", "Запускать расчет средних расходов ИПУ всегда при расчете", "bool", "10" },
                    " nzp_prm=1979");
            }
            else
            {
                // 1979|Запускать расчет средних расходов ИПУ всегда при расчете|||bool||10||||
                Database.Insert(prm_name,
                    new[] { "nzp_prm", "name_prm", "type_prm", "prm_num", "low_", "high_", "digits_" },
                    new[] { "1979", "Запускать расчет средних расходов ИПУ всегда при расчете", "bool", "10", null, null, null });
            }




            SetSchema(Bank.Data);
            SchemaQualifiedObjectName prm_5 = new SchemaQualifiedObjectName() { Name = "prm_5", Schema = CurrentSchema };
            SchemaQualifiedObjectName prm_10 = new SchemaQualifiedObjectName() { Name = "prm_10", Schema = CurrentSchema };

            if (Database.TableExists(prm_5) && Database.TableExists(prm_10))
            {
                //перенос существующих значений 
                Database.ExecuteNonQuery(" INSERT INTO " + CurrentSchema + Database.TableDelimiter + "prm_10 " +
                                         " (nzp, nzp_prm, dat_s, dat_po, val_prm, is_actual, nzp_user, dat_when,dat_del, user_del, dat_block, user_block, month_calc)" +
                                         " SELECT nzp, nzp_prm, dat_s, dat_po, val_prm, is_actual, nzp_user, dat_when,dat_del, user_del, dat_block, user_block, month_calc " +
                                         " FROM " + CurrentSchema + Database.TableDelimiter + "prm_5 WHERE nzp_prm=1987");

                Database.ExecuteNonQuery(" DELETE FROM " + CurrentSchema + Database.TableDelimiter + "prm_5 WHERE nzp_prm=1987");
            }



        }

        public override void Revert()
        {
        }
    }


}
