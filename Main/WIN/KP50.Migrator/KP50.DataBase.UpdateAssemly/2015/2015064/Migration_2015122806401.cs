using System;
using System.Data;
using KP50.DataBase.Migrator;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015064
{
    [Migration(2015122806401, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_2015122806401 : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            var prm_name = new SchemaQualifiedObjectName() { Name = "prm_name", Schema = CurrentSchema };
            if (Database.TableExists(prm_name))
            {
                #region   85|Пени|||float||10||||=1/300 -> Процент для пени
                if (NotExistRecord(prm_name, " WHERE nzp_prm=85"))
                {
                    Database.Insert(prm_name,
                        new[] { "nzp_prm", "name_prm", "type_prm", "prm_num" },
                        new[] { "85", "Процент для пени", "float", "10" });
                }
                else
                {
                    Database.Update(prm_name,
                        new[] { "name_prm", "type_prm", "prm_num" },
                        new[] { "Процент для пени", "float", "10" }, "nzp_prm=85");
                }
                #endregion

                //<NEW >
                #region 2118|Начальный процент для пени|||float||10|0|100|3|  =0
                if (NotExistRecord(prm_name, " WHERE nzp_prm=2118"))
                {
                    Database.Insert(prm_name,
                        new[] { "nzp_prm", "name_prm", "type_prm", "prm_num" },
                        new[] { "2118", "Начальный процент для пени", "float", "10" });
                }
                else
                {
                    Database.Update(prm_name,
                        new[] { "name_prm", "type_prm", "prm_num" },
                        new[] { "Начальный процент для пени", "float", "10" }, "nzp_prm=2118");
                }
                #endregion

                #region 2119|Повышенный процент для пени|||float||10|0|100|3| =1/130
                if (NotExistRecord(prm_name, " WHERE nzp_prm=2119"))
                {
                    Database.Insert(prm_name,
                        new[] { "nzp_prm", "name_prm", "type_prm", "prm_num" },
                        new[] { "2119", "Повышенный процент для пени", "float", "10" });
                }
                else
                {
                    Database.Update(prm_name,
                        new[] { "name_prm", "type_prm", "prm_num" },
                        new[] { "Повышенный процент для пени", "float", "10" }, "nzp_prm=2119");
                }
                #endregion
                //</NEW>

                #region  99|Дата начала расчета пени|||date||10|||| ->Месяц начала расчета пени
                if (NotExistRecord(prm_name, " WHERE nzp_prm=99"))
                {
                    Database.Insert(prm_name,
                        new[] { "nzp_prm", "name_prm", "type_prm", "prm_num", "is_day_uchet", "is_day_uchet_enable" },
                        new[] { "99", "Месяц начала расчета пени", "date", "10", "0", "0" });
                }
                else
                {
                    Database.Update(prm_name,
                        new[] { "name_prm", "type_prm", "prm_num", "is_day_uchet", "is_day_uchet_enable" },
                        new[] { "Месяц начала расчета пени", "date", "10", "0", "0" }, "nzp_prm=99");
                }
                #endregion

                #region  1376|Дата начала расчета пени по УК|||date||7||||->Месяц начала расчета пени УК
                if (NotExistRecord(prm_name, " WHERE nzp_prm=1376"))
                {
                    Database.Insert(prm_name,
                        new[] { "nzp_prm", "name_prm", "type_prm", "prm_num", "is_day_uchet", "is_day_uchet_enable" },
                        new[] { "1376", "Месяц начала расчета пени УК", "date", "7", "0", "0" });
                }
                else
                {
                    Database.Update(prm_name,
                        new[] { "name_prm", "type_prm", "prm_num", "is_day_uchet", "is_day_uchet_enable" },
                        new[] { "Месяц начала расчета пени УК", "date", "7", "0", "0" }, "nzp_prm=1376");
                }
                #endregion
            }
            var services = new SchemaQualifiedObjectName() { Name = "services", Schema = CurrentSchema };
            if (Database.TableExists(services))
            {
                if (NotExistRecord(services, " WHERE nzp_serv=506"))
                {
                    Database.Insert(services,
                        new[] { "nzp_serv", "service", "service_small", "service_name", "nzp_measure","ed_izmer", "nzp_frm", "ordering" },
                        new[] { "506", "Пени для капитального ремонта", "Пени к/р", "Пени для капитального ремонта", "1", "с кв.в мес.", "50", "506" });
                }
                else
                {
                    if (NotExistRecord(services, " WHERE nzp_serv=506 AND nzp_frm=50")) // ==услуга 506 есть и добавлена не этой миграцией
                    { 
                        //если услуга добавлена не этой миграцией, то значит имеется пересечение нумерации, которое нужно разрешать!
                        throw new Exception("Услуга с nzp_serv=506 уже занята!");
                    }
                    //услуга есть и добавлена ранее этой миграцией
                    Database.Update(services,
                        new[]
                        {
                           "service", "service_small", "service_name", "nzp_measure","ed_izmer", "nzp_frm", "ordering"
                        },
                        new[]
                        {
                           "Пени для капитального ремонта", "Пени к/р", "Пени для капитального ремонта", "1", "с кв.в мес.", "50", "506"
                        }, " nzp_serv=506");

                }

                var prm_frm = new SchemaQualifiedObjectName() { Schema = CurrentSchema, Name = "prm_frm" };
                var prm_tarifs = new SchemaQualifiedObjectName() { Schema = CurrentSchema, Name = "prm_tarifs" };
                if (Database.TableExists(prm_frm) && Database.TableExists(prm_tarifs))
                {
                    
                    Database.Delete(prm_tarifs, "nzp_frm=50 and nzp_serv in (500,506)");
                    Database.Insert(prm_tarifs,
                        new[] {"nzp_serv", "nzp_frm", "nzp_prm", "is_edit", "nzp_user"},
                        new[] {"500", "50", "85", "1", "1"});
                    Database.Insert(prm_tarifs,
                        new[] {"nzp_serv", "nzp_frm", "nzp_prm", "is_edit", "nzp_user"},
                        new[] {"500", "50", "99", "1", "1"});
                    Database.Insert(prm_tarifs,
                        new[] {"nzp_serv", "nzp_frm", "nzp_prm", "is_edit", "nzp_user"},
                        new[] {"506", "50", "85", "1", "1"});
                    Database.Insert(prm_tarifs,
                        new[] {"nzp_serv", "nzp_frm", "nzp_prm", "is_edit", "nzp_user"},
                        new[] {"506", "50", "99", "1", "1"});
                }
            }

        }
        private bool NotExistRecord(SchemaQualifiedObjectName table, string where)
        {
            return Convert.ToInt32(
                Database.ExecuteScalar("SELECT count(1) FROM " + GetFullTableName(table) + " " + where)) == 0;
        }
        private string GetFullTableName(SchemaQualifiedObjectName table)
        {
            return string.Format("{0}{1}{2}", table.Schema, Database.TableDelimiter, table.Name);
        }

    }
}
