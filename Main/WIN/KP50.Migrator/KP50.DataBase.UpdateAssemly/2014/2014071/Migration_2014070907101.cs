using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2014070907101, MigrateDataBase.CentralBank)]
    public class Migration_2014070907101_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName s_type_uchet = new SchemaQualifiedObjectName() { Name = "s_type_uchet", Schema = CurrentSchema };
            if (!Database.TableExists(s_type_uchet))
            {
                Database.AddTable(
                    s_type_uchet,
                    new Column("nzp_type_uchet", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull), // Identity property is SERIAL and NOT NULL value default
                    new Column("type_uchet", DbType.String.WithSize(100), ColumnProperty.NotNull)
                    );
            }
            Database.Delete(s_type_uchet, "nzp_type_uchet = 1");
            Database.Insert(s_type_uchet, new string[] { "nzp_type_uchet", "type_uchet" }, new string[] { "1", "Входящее сальдо" });
            Database.Delete(s_type_uchet, "nzp_type_uchet = 2");
            Database.Insert(s_type_uchet, new string[] { "nzp_type_uchet", "type_uchet" }, new string[] { "2", "Исходящее сальдо" });
            Database.Delete(s_type_uchet, "nzp_type_uchet = 3");
            Database.Insert(s_type_uchet, new string[] { "nzp_type_uchet", "type_uchet" }, new string[] { "3", "Начисление" });
            Database.Delete(s_type_uchet, "nzp_type_uchet = 4");
            Database.Insert(s_type_uchet, new string[] { "nzp_type_uchet", "type_uchet" }, new string[] { "4", "Оплаты" });
            Database.Delete(s_type_uchet, "nzp_type_uchet = 5");
            Database.Insert(s_type_uchet, new string[] { "nzp_type_uchet", "type_uchet" }, new string[] { "5", "Расход" });

            SchemaQualifiedObjectName s_typercl = new SchemaQualifiedObjectName() 
            { Name = "s_typercl", Schema = CurrentSchema };
            if (!Database.ColumnExists(s_typercl, "nzp_type_uchet")) Database.AddColumn(s_typercl, new Column("nzp_type_uchet", DbType.Int32));
            if (!Database.ColumnExists(s_typercl, "is_auto")) Database.AddColumn(s_typercl, new Column("is_auto", DbType.Int32));
            if (!Database.ColumnExists(s_typercl, "is_actual")) Database.AddColumn(s_typercl, new Column("is_actual", DbType.Int32));
            if (!Database.ColumnExists(s_typercl, "comment")) Database.AddColumn(s_typercl, new Column("comment", DbType.String.WithSize(100)));
            if (!Database.ColumnExists(s_typercl, "changed_by")) Database.AddColumn(s_typercl, new Column("changed_by", DbType.Int32));
            if (!Database.ColumnExists(s_typercl, "changed_on")) Database.AddColumn(s_typercl, new Column("changed_on", DbType.DateTime));
            if (Database.ColumnExists(s_typercl, "typename")) Database.ChangeColumn(s_typercl, "typename", DbType.String.WithSize(150), true);
            if (Database.ColumnExists(s_typercl, "is_actual")) Database.Update(s_typercl, new string[] { "is_actual" }, new string[] { "100" },
                " type_rcl not in (100,101,102,103,104,105,106,107,163) and type_rcl < 100000");
            
            Database.Delete(s_typercl, "type_rcl = 100");
            Database.Insert(s_typercl, new string[] { "type_rcl", "is_volum", "typename" , "nzp_type_uchet", "is_auto", "comment", "is_actual" },
                new string[] { "100", "0", "Корректировка входящего сальдо", "1", "0", "Ручное изменение входящего сальдо", "1" });
            Database.Delete(s_typercl, "type_rcl = 101");
            Database.Insert(s_typercl, new string[] { "type_rcl", "is_volum", "typename", "nzp_type_uchet", "is_auto", "comment", "is_actual" },
                new string[] { "101", "0", "Корректировка исходящего сальдо", "2", "0", "Ручное изменение исходящего сальдо", "1" });
            Database.Delete(s_typercl, "type_rcl = 102");
            Database.Insert(s_typercl, new string[] { "type_rcl", "is_volum", "typename", "nzp_type_uchet", "is_auto", "comment", "is_actual" },
                new string[] { "102", "0", "Корректировка начисления", "3", "0", "Ручное изменение начислений", "1" });
            Database.Delete(s_typercl, "type_rcl = 103");
            Database.Insert(s_typercl, new string[] { "type_rcl", "is_volum", "typename", "nzp_type_uchet", "is_auto", "comment", "is_actual" },
                new string[] { "103", "0", "Корректировка оплаты", "4", "0", "Ручное изменение сальдо оплатой", "1" });
            Database.Delete(s_typercl, "type_rcl = 104");
            Database.Insert(s_typercl, new string[] { "type_rcl", "is_volum", "typename", "nzp_type_uchet", "is_auto", "comment", "is_actual" },
                new string[] { "104", "0", "Автоматическая, внутри поставщика", "4", "1", "Перераспределение переплат по услугам поставщика", "1" });
            Database.Delete(s_typercl, "type_rcl = 105");
            Database.Insert(s_typercl, new string[] { "type_rcl", "is_volum", "typename", "nzp_type_uchet", "is_auto", "comment", "is_actual" },
                new string[] { "105", "0", "Автоматическая, внутри принципала, между поставщиками", "4", "1", "Перераспределение переплат по услугам поставщиков, указанного принципала", "1" });
            Database.Delete(s_typercl, "type_rcl = 106");
            Database.Insert(s_typercl, new string[] { "type_rcl", "is_volum", "typename", "nzp_type_uchet", "is_auto", "comment", "is_actual" },
                new string[] { "106", "0", "Автоматическая, между принципалами", "4", "1", "Перераспределение переплат по услугам поставщиков всех принципалов лицевого счёта", "1" });
            Database.Delete(s_typercl, "type_rcl = 107");
            Database.Insert(s_typercl, new string[] { "type_rcl", "is_volum", "typename", "nzp_type_uchet", "is_auto", "comment", "is_actual" },
                new string[] { "107", "0", "Автоматическая, перенос сальдо между поставщиками", "2", "1", "Перенос сальдо с одного поставщика на другого", "1" });
            Database.Delete(s_typercl, "type_rcl = 163");
            Database.Insert(s_typercl, new string[] { "type_rcl", "is_volum", "typename", "nzp_type_uchet", "is_auto", "comment", "is_actual" },
                new string[] { "163", "1", "Корректировка расхода", "5", "0", "Корректировка расхода по коммунальной услуге", "1" });

            SchemaQualifiedObjectName s_type_doc = new SchemaQualifiedObjectName() { Name = "s_type_doc", Schema = CurrentSchema };
            if (!Database.TableExists(s_type_doc))
            {
                Database.AddTable(
                    s_type_doc,
                    new Column("nzp_type_doc", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull), // Identity property is SERIAL and NOT NULL value default
                    new Column("doc_name", DbType.String.WithSize(100), ColumnProperty.NotNull)
                    );
            }
            Database.Delete(s_type_doc, "nzp_type_doc = 1");
            Database.Insert(s_type_doc, new string[] { "nzp_type_doc", "doc_name" }, new string[] { "1", "Решение суда" });
            Database.Delete(s_type_doc, "nzp_type_doc = 2");
            Database.Insert(s_type_doc, new string[] { "nzp_type_doc", "doc_name" }, new string[] { "2", "Заявление абонента" });
            Database.Delete(s_type_doc, "nzp_type_doc = 3");
            Database.Insert(s_type_doc, new string[] { "nzp_type_doc", "doc_name" }, new string[] { "3", "Письмо принципала" });
            Database.Delete(s_type_doc, "nzp_type_doc = 4");
            Database.Insert(s_type_doc, new string[] { "nzp_type_doc", "doc_name" }, new string[] { "4", "Письмо поставщика" });
            Database.Delete(s_type_doc, "nzp_type_doc = 5");
            Database.Insert(s_type_doc, new string[] { "nzp_type_doc", "doc_name" }, new string[] { "5", "Письмо агента" });

        }
    }

    [Migration(2014070907101, MigrateDataBase.Charge)]
    public class Migration_2014070907101_Charge : Migration
    {
        public override void Apply()
        {
            SchemaQualifiedObjectName perekidka = new SchemaQualifiedObjectName() { Name = "perekidka", Schema = CurrentSchema };
            if (!Database.ColumnExists(perekidka, "nzp_type_doc")) Database.AddColumn(perekidka, new Column("nzp_type_doc", DbType.Int32));
            if (!Database.ColumnExists(perekidka, "num_doc")) Database.AddColumn(perekidka, new Column("num_doc", DbType.String.WithSize(20)));
            if (!Database.ColumnExists(perekidka, "dat_doc")) Database.AddColumn(perekidka, new Column("dat_doc", DbType.DateTime));
        }
    }
}
