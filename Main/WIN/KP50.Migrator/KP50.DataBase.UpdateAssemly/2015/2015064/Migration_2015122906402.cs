using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using KP50.DataBase.Migrator.Framework;

namespace KP50.DataBase.UpdateAssembly._2015._2015064
{
    [Migration(2015122906402, Migrator.Framework.DataBase.LocalBank | Migrator.Framework.DataBase.CentralBank)]
    public class Migration_2015122906402 : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            var prm_name = new SchemaQualifiedObjectName() { Name = "prm_name", Schema = CurrentSchema };
            SetSchema(Bank.Data);
            var prm_10 = new SchemaQualifiedObjectName() { Name = "prm_10", Schema = CurrentSchema };
            var prm_5 = new SchemaQualifiedObjectName() { Name = "prm_5", Schema = CurrentSchema };
            //меняем номер prm_num
            if (Database.TableExists(prm_name))
            {
                if (NotExistRecord(prm_name," WHERE nzp_prm=1390"))
                {
                    Database.Insert(prm_name, new [] { "nzp_prm", "name_prm", "type_prm", "prm_num" },
                     new[] { "1390", "Учитывать среднее значение ИПУ после даты закрытия (п.354)", "bool", "10" });
                }
                else
                {
                    Database.Update(prm_name, new[] {"nzp_prm", "name_prm", "type_prm", "prm_num"},
                        new[] {"1390", "Учитывать среднее значение ИПУ после даты закрытия (п.354)", "bool", "10"},
                        "nzp_prm=1390");
                }
            }

            //переносим данные из prm_5 -> prm_10
            if (Database.TableExists(prm_5) && Database.TableExists(prm_10))
            {
                Database.ExecuteNonQuery(" INSERT INTO " + GetFullTableName(prm_10) + " " +
                                         " (nzp,nzp_prm,dat_s,dat_po,val_prm,is_actual,nzp_user,dat_when,dat_del,user_del,month_calc)" +
                                         " SELECT  nzp,nzp_prm,dat_s,dat_po,val_prm,is_actual,nzp_user,dat_when,dat_del,user_del,month_calc" +
                                         " FROM " + GetFullTableName(prm_5) + " WHERE nzp_prm=1390;" +
                                         " DELETE FROM " + GetFullTableName(prm_5) + " WHERE nzp_prm=1390;");

                //по-умолчанию параметр должен быть включен
                Database.ExecuteNonQuery(" INSERT INTO " + GetFullTableName(prm_10) + " " +
                                         " (nzp,nzp_prm,dat_s,dat_po,val_prm,is_actual,nzp_user,dat_when,dat_del,user_del,month_calc)" +
                                         " SELECT 0,1390,'01.01.1900'::DATE,'01.01.3000'::DATE,1,1,1,now(),null,null,null" +
                                         " WHERE NOT EXISTS (SELECT 1 FROM " + GetFullTableName(prm_10) +
                                         " WHERE nzp_prm=1390 AND is_actual=1) ");
            }
        }

        private bool NotExistRecord(SchemaQualifiedObjectName table, string where)
        {
            return Convert.ToInt32(
                Database.ExecuteScalar("SELECT count(*) FROM " + GetFullTableName(table) + " " + where)) ==
                   0;
        }
        private string GetFullTableName(SchemaQualifiedObjectName table)
        {
            return string.Format("{0}{1}{2}", table.Schema, Database.TableDelimiter, table.Name);
        }
    }
}
