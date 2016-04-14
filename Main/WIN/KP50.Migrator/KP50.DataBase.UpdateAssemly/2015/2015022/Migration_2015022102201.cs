using System.IO;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2015022102201, MigrateDataBase.LocalBank)]
    public class Migration_2015022102201 : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);

            var counters_bounds = new SchemaQualifiedObjectName { Name = "counters_bounds", Schema = CurrentSchema };
            var counters_spis = new SchemaQualifiedObjectName { Name = "counters_spis", Schema = CurrentSchema };

            //только дописываем, чтобы не потерять данные!
            if (Database.TableExists(counters_bounds) && Database.TableExists(counters_spis))
            {
                //записываем периоды поверок для ПУ
                //поверка - может пересекаться
                var sql = " INSERT INTO " + CurrentSchema + Database.TableDelimiter + counters_bounds.Name +
                          " (nzp_counter,type_id,date_from,date_to,is_actual,created_by,created_on)" +
                          " SELECT nzp_counter,2 as type_id,case when dat_prov is NULL then '01.01.1900' else dat_prov end " +
                          ",CASE WHEN dat_provnext is NULL THEN '01.01.3000' ELSE dat_provnext END, true, 1 as nzp_user,now()" +
                          " FROM " + CurrentSchema + Database.TableDelimiter + counters_spis.Name + " " +
                          " WHERE dat_provnext is NOT NULL OR dat_prov is NOT NULL ";
                Database.ExecuteNonQuery(sql);

                //записываем периоды поломок для ПУ
                //поломка - не может пересекаться
                sql = " INSERT INTO " + CurrentSchema + Database.TableDelimiter + counters_bounds.Name +
                      " (nzp_counter,type_id,date_from,date_to,is_actual,created_by,created_on) " +
                      " SELECT nzp_counter,1 as type_id,(case when dat_oblom is NULL then '01.01.1900' else dat_oblom end) " +
                      ",(case when dat_poch is NULL then '01.01.3000' else dat_poch end), true, 1,now()" +
                      " FROM " + CurrentSchema + Database.TableDelimiter + counters_spis.Name + " cs " +
                      " WHERE (dat_oblom is NOT NULL OR dat_poch is NOT NULL)  " +
                      " AND cs.nzp_counter NOT IN (SELECT t.nzp_counter FROM " + CurrentSchema + Database.TableDelimiter + counters_bounds.Name + " t " +
                      " WHERE t.type_id=1 AND cs.nzp_counter=t.nzp_counter AND t.is_actual=true " +
                      " AND (case when cs.dat_oblom is NULL then '01.01.1900' else cs.dat_oblom end)<t.date_to " +
                      " AND (case when cs.dat_poch is NULL then '01.01.3000' else cs.dat_poch end)>t.date_from )";
                Database.ExecuteNonQuery(sql);

            }

        }
    }
}
