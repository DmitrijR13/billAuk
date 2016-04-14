using System;
using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2015021002102, MigrateDataBase.CentralBank)]
    public class Migration_2015021002102 : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);

            //только для Калужской области
            object obj = Database.ExecuteScalar("select count(*) from " + CurrentSchema + Database.TableDelimiter + "s_point " + 
                " where flag = 1 and substring(cast(bank_number as varchar (250)), 1,2) = '40' ");

            int cnt = 0;
            try
            { cnt = Convert.ToInt32(obj); }
            catch
            { cnt = 0; }

            if (cnt > 0)
            {
                // добавить параметр "Псевдоним ПУ для выгрузки"
                Database.ExecuteNonQuery("delete from " + CurrentSchema + Database.TableDelimiter + "prm_name where nzp_prm = 1426");
                Database.ExecuteNonQuery("insert into " + CurrentSchema + Database.TableDelimiter + "prm_name " + 
                    " (nzp_prm, name_prm, old_field, type_prm, prm_num, is_day_uchet) " + 
                    " values " + 
                    " (1426, 'Псевдоним ПУ для выгрузки', 0, 'char', 17, 0)");

                // открыть параметр на редактирование
                Database.ExecuteNonQuery("delete from " + CurrentSchema + Database.TableDelimiter + "s_reg_prm where nzp_prm = 1426");
                obj = Database.ExecuteScalar("select max(numer) from " + CurrentSchema + Database.TableDelimiter + "s_reg_prm where nzp_reg = 6");
                
                int num = 0;
                try
                { num = Convert.ToInt32(obj); }
                catch
                { num = 0; }
                
                num++;
                Database.ExecuteNonQuery("insert into " + CurrentSchema + Database.TableDelimiter + "s_reg_prm (nzp_reg, nzp_prm, nzp_serv, numer, is_show) values (6, 1426, 0, " + num + ", 1)");
            }
        }
    }
}
