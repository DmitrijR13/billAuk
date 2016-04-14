using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2014061606202, MigrateDataBase.CentralBank)]
    public class Migration_2014061606202_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName payer_types = new SchemaQualifiedObjectName() { Name = "payer_types", Schema = CurrentSchema };
            if (!Database.TableExists(payer_types))
            {
                Database.AddTable(payer_types,
                    new Column("nzp_pt", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("nzp_payer", DbType.Int32),
                    new Column("nzp_payer_type", DbType.Int32),
                    new Column("changed_by", DbType.Int32, ColumnProperty.NotNull),
                    new Column("changed_on", DbType.DateTime, ColumnProperty.NotNull));
                Database.AddIndex("ix_payer_types_1", true, payer_types, "nzp_payer");
                Database.AddIndex("ix_payer_types_2", false, payer_types, "nzp_payer_type");
                Database.AddIndex("ix_payer_types_3", false, payer_types, "nzp_pt");
            }
            else
            {
                Database.Delete(payer_types, "nzp_payer_type in (1, 100)");
            }

            SchemaQualifiedObjectName s_payer = new SchemaQualifiedObjectName() { Name = "s_payer", Schema = CurrentSchema };
            if (Database.TableExists(s_payer))
            {
                Database.Update(s_payer, new string[] { "nzp_type" }, new string[] { null }, "nzp_type in (1, 100)");
            }

            SchemaQualifiedObjectName s_payer_types = new SchemaQualifiedObjectName() { Name = "s_payer_types", Schema = CurrentSchema };
            if (Database.TableExists(s_payer_types))
            {
                Database.Delete(s_payer_types, "nzp_payer_type in (1, 10)");

                Database.Update(s_payer_types, new string[] { "type_name" }, new string[] { "Агент (Расчетный центр)" }, "nzp_payer_type = 5");
                Database.Update(s_payer_types, new[] { "type_name" }, new[] { "Ресурсоснабжающая организация" }, "nzp_payer_type = 6");
                Database.Update(s_payer_types, new[] { "type_name" }, new[] { "Арендатор жилья" }, "nzp_payer_type = 7");
                Database.Update(s_payer_types, new[] { "type_name" }, new[] { "Банк" }, "nzp_payer_type = 8");
                Database.Update(s_payer_types, new[] { "type_name" }, new[] { "Субабонент" }, "nzp_payer_type = 9");
                Database.Insert(s_payer_types, new string[] { "nzp_payer_type", "type_name", "is_system" }, new string[] { "10", "Принципал (Исполнитель услуг)", "1" });
            }
        }
    }
}