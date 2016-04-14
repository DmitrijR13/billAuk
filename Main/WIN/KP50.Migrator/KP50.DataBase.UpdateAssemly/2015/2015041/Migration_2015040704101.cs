using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2014._2014124
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2015040704101, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_2015040704101_CentralBank : Migration
    {
        public override void Apply()
        {
            if (Database.ProviderName != "PostgreSQL") return;
            SetSchema(Bank.Kernel);

            var formuls = new SchemaQualifiedObjectName() { Name = "formuls", Schema = CurrentSchema };
            if (Database.TableExists(formuls))
            {
                var count =
              Convert.ToInt32(
                  Database.ExecuteScalar("SELECT COUNT(1) FROM " + formuls.Schema + Database.TableDelimiter +
                                         formuls.Name + " WHERE nzp_frm = 2004"));
                if (count == 0)
                {
                    Database.Insert(formuls,
                        new[] { "nzp_frm", "name_frm", "dat_s", "dat_po", "tarif", "nzp_measure", "is_device" },
                        new[] { "2004", "Перечень параметров к перерасчету", null, null, null, "6", "0" });

                }

                var prm_frm = new SchemaQualifiedObjectName() { Name = "prm_frm", Schema = CurrentSchema };

                if (Database.TableExists(prm_frm))
                {
                    //key - nzp_serv, values - nzp_prm
                    var dictPrms = new Dictionary<int, List<int>>();
                    
                    #region nzp_serv=1
                    //5	Количество жильцов                                                                                  
                    //10	Временно выбывшие                                                                                   
                    //51	Состояние счета                                                                                     
                    //90	Разрешить подневной расчет для лицевого счета                                                       
                    //130	Считать количество жильцов по АИС Паспортистка ЖЭУ
                    dictPrms[1] = new List<int>() { 5, 10, 51, 90, 130 };
                    #endregion nzp_serv=1

                    #region nzp_serv=6
                    //1557	Повышающий коэффициент по услуге хол.вода   
                    //373 Среднее значение домового счетчика по хол.воде
                    //2031 ОДН ХВ-Тип распределения
                    //2032 ОДН ХВ-НЕ начислять по Пост.307 если К<1
                    //2033 ОДН ХВ-Вид распределения
                    //2034 ОДН ХВ-Показатель распределения
                    //1214 Пост.344 ХВ -Разрешить превышение расхода ОДН над нормативом на ОДН
                    dictPrms[6] = new List<int>() { 1557, 373, 2031, 2032, 2033, 2034, 1214 };
                    #endregion nzp_serv=6

                    #region nzp_serv=7
                    //375 Среднее значение домового счетчика по канализацииъ
                    //2091 ОДН КАН-Тип распределения
                    //2092 ОДН КАН-НЕ начислять по Пост.307 если К<1
                    //2093 ОДН КАН-Вид распределения
                    //2094 ОДН КАН-Показатель распределения
                    dictPrms[7] = new List<int>() { 375, 2091, 2092, 2093, 2094 };
                    #endregion nzp_serv=7

                    #region nzp_serv=8
                    //1556	Повышающий коэффициент по услуге отопления 
                    //228 Среднее значение домового счетчика по отоплению
                    //2045 ОДН ОТ-Тип распределения
                    //2046 ОДН ОТ-НЕ начислять по Пост.307 если К<1
                    //2047 ОДН ОТ-Вид распределения
                    //2048 ОДН ОТ-Показатель распределения
                    dictPrms[8] = new List<int>() { 1556, 228, 2045, 2046, 2047, 2048 };
                    #endregion nzp_serv=8

                    #region nzp_serv=9
                    //1558	Повышающий коэффициент по услуге гор.вода  
                    //374 Среднее значение домового счетчика по гор.воде
                    //2035 ОДН ГВ-Тип распределения
                    //2036 ОДН ГВ-НЕ начислять по Пост.307 если К<1
                    //2037 ОДН ГВ-Вид распределения
                    //2038 ОДН ГВ-Показатель распределения
                    //1215 Пост.344 ГВ -Разрешить превышение расхода ОДН над нормативом на ОДН
                    //1397 Тип алгоритма расчета для ПУ от ГКал --гвс
                    dictPrms[9] = new List<int>() { 1558, 374, 2035, 2036, 2037, 2038, 1215, 1397 };
                    #endregion nzp_serv=9

                    #region nzp_serv=10
                    //2062 ОДН ГАЗ-Тип распределения
                    //2063 ОДН ГАЗ-НЕ начислять по Пост.307 если К<1
                    //2064 ОДН ГАЗ-Вид распределения
                    //2065 ОДН ГАЗ-Показатель распределения
                    //1217 Пост.344 ГАЗ-Разрешить превышение расхода ОДН над нормативом на ОДН
                    dictPrms[10] = new List<int>() { 2062, 2063, 2064, 2065, 1217 };
                    #endregion nzp_serv=10

                    #region nzp_serv=25
                    //1559	Повышающий коэффициент по услуге электроснабжение 
                    //376 Среднее значение домового счетчика по электроэнергии
                    //2039 ОДН ЭЭ-Тип распределения
                    //2040 ОДН ЭЭ-НЕ начислять по Пост.307 если К<1
                    //2041 ОДН ЭЭ-Вид распределения
                    //2042 ОДН ЭЭ-Показатель распределения
                    //1216 Пост.344 ЭЭ -Разрешить превышение расхода ОДН над нормативом на ОДН
                    dictPrms[25] = new List<int>() { 1559, 376, 2039, 2040, 2041, 2042, 1216 };
                    #endregion nzp_serv=25

                    Database.Delete(prm_frm, " nzp_frm=2004");
                    foreach (var serv in dictPrms)
                    {
                        foreach (var prm in serv.Value)
                        {
                            Database.Insert(prm_frm,
                           new[] { "nzp_frm", "frm_calc", "order", "is_prm", "operation", "nzp_prm", "frm_p1", "frm_p2", "frm_p3", "result" },
                           new[] { "2004", "0", "0", "1", "FLD", 
                                    prm.ToString(CultureInfo.InvariantCulture), 
                                    serv.Key.ToString(CultureInfo.InvariantCulture), 
                                    null, null, null });
                        }
                    }
                }
            }

        }

        public override void Revert()
        {
        }
    }


}
