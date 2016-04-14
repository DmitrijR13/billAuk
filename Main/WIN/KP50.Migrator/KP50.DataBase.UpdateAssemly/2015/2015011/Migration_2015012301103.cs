using System;
using System.Collections.Generic;
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
    [Migration(2015012301103, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_2015012301103_CentralBank : Migration
    {
        public override void Apply()
        {
            if (Database.ProviderName != "PostgreSQL") return;
            SetSchema(Bank.Kernel);

            var formuls = new SchemaQualifiedObjectName() { Name = "formuls", Schema = CurrentSchema };
            if (Database.TableExists(formuls))
            {
                Database.Delete(formuls, " nzp_frm=2004");
                Database.Insert(formuls,
                    new[] { "nzp_frm", "name_frm", "dat_s", "dat_po", "tarif", "nzp_measure", "is_device" },
                    new[] { "2004", "Перечень параметров к перерасчету", null, null, null, "6", "0" });

                var prm_frm = new SchemaQualifiedObjectName() { Name = "prm_frm", Schema = CurrentSchema };

                if (Database.TableExists(prm_frm))
                {
                    //5	Количество жильцов                                                                                  
                    //10	Временно выбывшие                                                                                   
                    //51	Состояние счета                                                                                     
                    //90	Разрешить подневной расчет для лицевого счета                                                       
                    //130	Считать количество жильцов по АИС Паспортистка ЖЭУ                                                  
                    //1556	Повышающий коэффициент по услуге отопления                                                          
                    //1557	Повышающий коэффициент по услуге хол.вода                                                           
                    //1558	Повышающий коэффициент по услуге гор.вода                                                           
                    //1559	Повышающий коэффициент по услуге электроснабжение                                                   
                    var listPrms = new List<int>() { 5, 10, 51, 130, 90, 1556, 1557, 1558, 1559 };
                    Database.Delete(prm_frm, " nzp_frm=2004");
                    foreach (var prm in listPrms)
                    {
                     
                        Database.Insert(prm_frm,
                       new[] { "nzp_frm", "frm_calc", "order", "is_prm", "operation", "nzp_prm", "frm_p1", "frm_p2", "frm_p3", "result" },
                       new[] { "2004", "0", "0", "1", "FLD", prm.ToString(), null, null, null, null });
                    }
                }
            }

        }

        public override void Revert()
        {
        }
    }


}
