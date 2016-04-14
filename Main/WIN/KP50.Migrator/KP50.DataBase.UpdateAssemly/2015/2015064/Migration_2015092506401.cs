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
    [Migration(2015092506401, MigrateDataBase.CentralBank)]
    public class Migration_2015092506401_CentralBank : Migration
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
                Database.Delete(s_group_check, " nzp_group in (61,62,63,64,65,66)");
                Database.Insert(s_group_check, new[] { "nzp_group", "ngroup" }, new[] { "61", "0Генерация средних расходов ИПУ - установлен расход" });
                Database.Insert(s_group_check, new[] { "nzp_group", "ngroup" }, new[] { "62", "0Генерация средних расходов ИПУ - нет показаний для генерации" });
                Database.Insert(s_group_check, new[] { "nzp_group", "ngroup" }, new[] { "63", "0Генерация средних расходов ИПУ - есть показание ИПУ" });
                Database.Insert(s_group_check, new[] { "nzp_group", "ngroup" }, new[] { "64", "0Генерация средних расходов ИПУ - нет ИПУ " });
                Database.Insert(s_group_check, new[] { "nzp_group", "ngroup" }, new[] { "65", "0Генерация средних расходов ИПУ - расход ИПУ равен 0" });
                Database.Insert(s_group_check, new[] { "nzp_group", "ngroup" }, new[] { "66", "0Генерация средних расходов ИПУ - превышение допустимого расхода" });
            }

          
        }





    }

}
