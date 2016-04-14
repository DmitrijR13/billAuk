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
    [Migration(2015092406401, MigrateDataBase.CentralBank)]
    public class Migration_2015092406401_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            var s_group_check = new SchemaQualifiedObjectName() { Name = "s_group_check", Schema = CurrentSchema };

            if (!Database.TableExists(s_group_check))
            {
                Database.AddTable(s_group_check,
                    new Column("nzp_group", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("ngroup", DbType.StringFixedLength.WithSize(80)));
            }

            if (Database.TableExists(s_group_check))
            {
                Database.Delete(s_group_check, " nzp_group in (3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24)");
                Database.Insert(s_group_check, new[] { "nzp_group", "ngroup" }, new[] { "3", "П-рассогласование в распределения оплат" });
                Database.Insert(s_group_check, new[] { "nzp_group", "ngroup" }, new[] { "4", "П-рассогласование учета оплат в сальдо" });
                Database.Insert(s_group_check, new[] { "nzp_group", "ngroup" }, new[] { "5", "П-рассогласование учета перекидок оплат" });
                Database.Insert(s_group_check, new[] { "nzp_group", "ngroup" }, new[] { "6", "П-большие расходы ИПУ" });
                Database.Insert(s_group_check, new[] { "nzp_group", "ngroup" }, new[] { "7", "П-большие расходы общеквартирных ПУ" });
                Database.Insert(s_group_check, new[] { "nzp_group", "ngroup" }, new[] { "8", "П-большие расходы групповых ПУ" });
                Database.Insert(s_group_check, new[] { "nzp_group", "ngroup" }, new[] { "9", "П-большие расходы ОДПУ" });
                Database.Insert(s_group_check, new[] { "nzp_group", "ngroup" }, new[] { "10", "П-недопоставка не учтена в расчете" });
                Database.Insert(s_group_check, new[] { "nzp_group", "ngroup" }, new[] { "11", "П-изменен расход ИПУ после расчета ОДН" });
                Database.Insert(s_group_check, new[] { "nzp_group", "ngroup" }, new[] { "12", "П-изменены параметры ЛС после расчета" });
                Database.Insert(s_group_check, new[] { "nzp_group", "ngroup" }, new[] { "13", "П-большие расходы ПУ полученные при расчете" });
                Database.Insert(s_group_check, new[] { "nzp_group", "ngroup" }, new[] { "14", "П-соответствие входящего и исходящего сальдо" });
                Database.Insert(s_group_check, new[] { "nzp_group", "ngroup" }, new[] { "15", "П-соответствие финансового месяца опер. дню" });
                Database.Insert(s_group_check, new[] { "nzp_group", "ngroup" }, new[] { "16", "П-большие начисления" });
                Database.Insert(s_group_check, new[] { "nzp_group", "ngroup" }, new[] { "17", "П-наличие показаний ИПУ без ИПУ" });
                Database.Insert(s_group_check, new[] { "nzp_group", "ngroup" }, new[] { "18", "П-наличие дублированных записей в начислениях" });
                Database.Insert(s_group_check, new[] { "nzp_group", "ngroup" }, new[] { "19", "П-наличие финансовых банков данных для след.года" });
                Database.Insert(s_group_check, new[] { "nzp_group", "ngroup" }, new[] { "20", "П-наличие показаний ОДПУ без ОДПУ" });
                Database.Insert(s_group_check, new[] { "nzp_group", "ngroup" }, new[] { "21", "П-наличие показаний Груп.ПУ без ПУ" });
                Database.Insert(s_group_check, new[] { "nzp_group", "ngroup" }, new[] { "22", "П-наличие дублированных показаний ИПУ" });
                Database.Insert(s_group_check, new[] { "nzp_group", "ngroup" }, new[] { "23", "П-наличие дублированных показаний ОДПУ" });
                Database.Insert(s_group_check, new[] { "nzp_group", "ngroup" }, new[] { "24", "П-наличие дублированных показаний групповых ПУ" });
                Database.Insert(s_group_check, new[] { "nzp_group", "ngroup" }, new[] { "25", "П-наличие открытых ЛС без начислений" });
            }

            SetSchema(Bank.Data);
            var s_group = new SchemaQualifiedObjectName() { Name = "s_group", Schema = CurrentSchema };

            if (Database.TableExists(s_group))
            {
                Database.Delete(s_group, " nzp_group in (3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24)");
                Database.Insert(s_group, new[] { "nzp_group", "ngroup" }, new[] { "3", "П-рассогласование в распределения оплат" });
                Database.Insert(s_group, new[] { "nzp_group", "ngroup" }, new[] { "4", "П-рассогласование учета оплат в сальдо" });
                Database.Insert(s_group, new[] { "nzp_group", "ngroup" }, new[] { "5", "П-рассогласование учета перекидок оплат" });
                Database.Insert(s_group, new[] { "nzp_group", "ngroup" }, new[] { "6", "П-большие расходы ИПУ" });
                Database.Insert(s_group, new[] { "nzp_group", "ngroup" }, new[] { "7", "П-большие расходы общеквартирных ПУ" });
                Database.Insert(s_group, new[] { "nzp_group", "ngroup" }, new[] { "8", "П-большие расходы групповых ПУ" });
                Database.Insert(s_group, new[] { "nzp_group", "ngroup" }, new[] { "9", "П-большие расходы ОДПУ" });
                Database.Insert(s_group, new[] { "nzp_group", "ngroup" }, new[] { "10", "П-недопоставка не учтена в расчете" });
                Database.Insert(s_group, new[] { "nzp_group", "ngroup" }, new[] { "11", "П-изменен расход ИПУ после расчета ОДН" });
                Database.Insert(s_group, new[] { "nzp_group", "ngroup" }, new[] { "12", "П-изменены параметры ЛС после расчета" });
                Database.Insert(s_group, new[] { "nzp_group", "ngroup" }, new[] { "13", "П-большие расходы ПУ полученные при расчете" });
                Database.Insert(s_group, new[] { "nzp_group", "ngroup" }, new[] { "14", "П-соответствие входящего и исходящего сальдо" });
                Database.Insert(s_group, new[] { "nzp_group", "ngroup" }, new[] { "15", "П-соответствие финансового месяца опер. дню" });
                Database.Insert(s_group, new[] { "nzp_group", "ngroup" }, new[] { "16", "П-большие начисления" });
                Database.Insert(s_group, new[] { "nzp_group", "ngroup" }, new[] { "17", "П-наличие показаний ИПУ без ИПУ" });
                Database.Insert(s_group, new[] { "nzp_group", "ngroup" }, new[] { "18", "П-наличие дублированных записей в начислениях" });
                Database.Insert(s_group, new[] { "nzp_group", "ngroup" }, new[] { "19", "П-наличие финансовых банков данных для след.года" });
                Database.Insert(s_group, new[] { "nzp_group", "ngroup" }, new[] { "20", "П-наличие показаний ОДПУ без ОДПУ" });
                Database.Insert(s_group, new[] { "nzp_group", "ngroup" }, new[] { "21", "П-наличие показаний Груп.ПУ без ПУ" });
                Database.Insert(s_group, new[] { "nzp_group", "ngroup" }, new[] { "22", "П-наличие дублированных показаний ИПУ" });
                Database.Insert(s_group, new[] { "nzp_group", "ngroup" }, new[] { "23", "П-наличие дублированных показаний ОДПУ" });
                Database.Insert(s_group, new[] { "nzp_group", "ngroup" }, new[] { "24", "П-наличие дублированных показаний групповых ПУ" });
                Database.Insert(s_group, new[] { "nzp_group", "ngroup" }, new[] { "25", "П-наличие открытых ЛС без начислений" });
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

    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2015092406401, MigrateDataBase.LocalBank)]
    public class Migration_2015092406401_LocalBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);
            var s_group = new SchemaQualifiedObjectName() { Name = "s_group", Schema = CurrentSchema };

            if (Database.TableExists(s_group))
            {
                Database.Delete(s_group, " nzp_group in (3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24)");
                Database.Insert(s_group, new[] { "nzp_group", "ngroup" }, new[] { "3", "П-рассогласование в распределения оплат" });
                Database.Insert(s_group, new[] { "nzp_group", "ngroup" }, new[] { "4", "П-рассогласование учета оплат в сальдо" });
                Database.Insert(s_group, new[] { "nzp_group", "ngroup" }, new[] { "5", "П-рассогласование учета перекидок оплат" });
                Database.Insert(s_group, new[] { "nzp_group", "ngroup" }, new[] { "6", "П-большие расходы ИПУ" });
                Database.Insert(s_group, new[] { "nzp_group", "ngroup" }, new[] { "7", "П-большие расходы общеквартирных ПУ" });
                Database.Insert(s_group, new[] { "nzp_group", "ngroup" }, new[] { "8", "П-большие расходы групповых ПУ" });
                Database.Insert(s_group, new[] { "nzp_group", "ngroup" }, new[] { "9", "П-большие расходы ОДПУ" });
                Database.Insert(s_group, new[] { "nzp_group", "ngroup" }, new[] { "10", "П-недопоставка не учтена в расчете" });
                Database.Insert(s_group, new[] { "nzp_group", "ngroup" }, new[] { "11", "П-изменен расход ИПУ после расчета ОДН" });
                Database.Insert(s_group, new[] { "nzp_group", "ngroup" }, new[] { "12", "П-изменены параметры ЛС после расчета" });
                Database.Insert(s_group, new[] { "nzp_group", "ngroup" }, new[] { "13", "П-большие расходы ПУ полученные при расчете" });
                Database.Insert(s_group, new[] { "nzp_group", "ngroup" }, new[] { "14", "П-соответствие входящего и исходящего сальдо" });
                Database.Insert(s_group, new[] { "nzp_group", "ngroup" }, new[] { "15", "П-соответствие финансового месяца опер. дню" });
                Database.Insert(s_group, new[] { "nzp_group", "ngroup" }, new[] { "16", "П-большие начисления" });
                Database.Insert(s_group, new[] { "nzp_group", "ngroup" }, new[] { "17", "П-наличие показаний ИПУ без ИПУ" });
                Database.Insert(s_group, new[] { "nzp_group", "ngroup" }, new[] { "18", "П-наличие дублированных записей в начислениях" });
                Database.Insert(s_group, new[] { "nzp_group", "ngroup" }, new[] { "19", "П-наличие финансовых банков данных для след.года" });
                Database.Insert(s_group, new[] { "nzp_group", "ngroup" }, new[] { "20", "П-наличие показаний ОДПУ без ОДПУ" });
                Database.Insert(s_group, new[] { "nzp_group", "ngroup" }, new[] { "21", "П-наличие показаний Груп.ПУ без ПУ" });
                Database.Insert(s_group, new[] { "nzp_group", "ngroup" }, new[] { "22", "П-наличие дублированных показаний ИПУ" });
                Database.Insert(s_group, new[] { "nzp_group", "ngroup" }, new[] { "23", "П-наличие дублированных показаний ОДПУ" });
                Database.Insert(s_group, new[] { "nzp_group", "ngroup" }, new[] { "24", "П-наличие дублированных показаний групповых ПУ" });
                Database.Insert(s_group, new[] { "nzp_group", "ngroup" }, new[] { "25", "П-наличие открытых ЛС без начислений" });
            }

        }

        public override void Revert()
        {
            SetSchema(Bank.Kernel);
            // TODO: Downgrade LocalPref_Kernel

            SetSchema(Bank.Data);
            // TODO: Downgrade LocalPref_Data

        }
    }

}
