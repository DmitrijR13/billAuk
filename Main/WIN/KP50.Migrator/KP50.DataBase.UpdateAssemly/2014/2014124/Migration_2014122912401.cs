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
    [Migration(2014122912401, MigrateDataBase.CentralBank)]
    public class Migration_2014122912401_CentralBank : Migration
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
                Database.Delete(s_group_check, " nzp_group in (3,4,5,6,7,8,9,10,11,12)");
                Database.Insert(s_group_check, new[] { "nzp_group", "ngroup" }, new[] { "3", "П-рассогл. в распр оплат" });
                Database.Insert(s_group_check, new[] { "nzp_group", "ngroup" }, new[] { "4", "П-рассогл. учета оплат в сальдо" });
                Database.Insert(s_group_check, new[] { "nzp_group", "ngroup" }, new[] { "5", "П-рассогл. в перекидках оплат" });
                Database.Insert(s_group_check, new[] { "nzp_group", "ngroup" }, new[] { "6", "П-превыш.показ. в ИПУ" });
                Database.Insert(s_group_check, new[] { "nzp_group", "ngroup" }, new[] { "7", "П-превыш.показ. в квартирн.ПУ" });
                Database.Insert(s_group_check, new[] { "nzp_group", "ngroup" }, new[] { "8", "П-превыш.показ. в группов.ПУ" });
                Database.Insert(s_group_check, new[] { "nzp_group", "ngroup" }, new[] { "9", "П-превыш.показ. в ОДПУ" });
                Database.Insert(s_group_check, new[] { "nzp_group", "ngroup" }, new[] { "10", "П-недопост. не учтена в расчете" });
                Database.Insert(s_group_check, new[] { "nzp_group", "ngroup" }, new[] { "11", "П-изм.расхода ПУ после рассчит.ОДН" });
                Database.Insert(s_group_check, new[] { "nzp_group", "ngroup" }, new[] { "12", "П-изм.пар-ров ЛС после рассчета" });
            }

            SetSchema(Bank.Data);
            // TODO: Upgrade CentralPref_Data

            SetSchema(Bank.Upload);
            // TODO: Upgrade CentralPref_Upload
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
