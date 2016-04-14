using System;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2015032503301, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_2015032503301_LocalBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            // TODO: Upgrade CentralPref_Kernel

            //1463,'НЕ генерировать средний расход ПУ','bool',17
            var parameters = new SchemaQualifiedObjectName { Name = "prm_name", Schema = CurrentSchema };
            var count = Convert.ToInt32(Database.ExecuteScalar("SELECT COUNT(1) FROM " + parameters.Schema + Database.TableDelimiter + parameters.Name + " WHERE nzp_prm = 1463"));
            if (count == 0)
            {
                Database.Insert(parameters, new
                    {
                        nzp_prm = 1463,
                        name_prm = "Отключить генерацию среднего расхода ПУ",
                        type_prm = "bool",
                        prm_num = 17
                    });
                //добавляем для отображения в параметрах ПУ
                if (CurrentSchema == CentralKernel)
                {
                    var s_reg_prm = new SchemaQualifiedObjectName { Name = "s_reg_prm", Schema = CurrentSchema };
                    Database.Delete(s_reg_prm, " nzp_prm=1463");
                    for (var i = 4; i <= 6; i++)
                    {
                        Database.Insert(s_reg_prm, new
                        {
                            nzp_reg = i,
                            nzp_prm = 1463,
                            nzp_serv = 0,
                            numer = 17,
                            is_show = 1
                        });
                    }
                }


            }
        }

        public override void Revert()
        {

        }
    }
}
