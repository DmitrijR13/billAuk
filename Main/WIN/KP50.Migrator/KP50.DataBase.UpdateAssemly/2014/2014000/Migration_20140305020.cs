using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;
using System.Collections.Generic;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(20140305020, MigrateDataBase.CentralBank)]
    public class Migration_20140305020_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName prm_name = new SchemaQualifiedObjectName() { Name = "prm_name", Schema = CurrentSchema };
            if (Database.TableExists(prm_name))
            {
                Database.Delete(prm_name, "nzp_prm BETWEEN 1116 AND 1122");
                Database.Insert(prm_name,
                    new string[] { "nzp_prm", "name_prm", "type_prm", "prm_num", "low_", "high_", "digits_" },
                    new string[] { "1116", "Рассрочка(процент) по холодной воде", "float", "1", "0", "100", "4" });
                Database.Insert(prm_name,
                    new string[] { "nzp_prm", "name_prm", "type_prm", "prm_num", "low_", "high_", "digits_" },
                    new string[] { "1117", "Рассрочка(процент) по горячей воде", "float", "1", "0", "100", "4" });
                Database.Insert(prm_name,
                    new string[] { "nzp_prm", "name_prm", "type_prm", "prm_num", "low_", "high_", "digits_" },
                    new string[] { "1118", "Рассрочка(процент) по канализации", "float", "1", "0", "100", "4" });
                Database.Insert(prm_name,
                    new string[] { "nzp_prm", "name_prm", "type_prm", "prm_num", "low_", "high_", "digits_" },
                    new string[] { "1119", "Рассрочка(процент) по отоплению", "float", "1", "0", "100", "4" });
                Database.Insert(prm_name,
                    new string[] { "nzp_prm", "name_prm", "type_prm", "prm_num", "low_", "high_", "digits_" },
                    new string[] { "1120", "Рассрочка(процент) по электроснабжению", "float", "1", "0", "100", "4" });
                Database.Insert(prm_name,
                    new string[] { "nzp_prm", "name_prm", "type_prm", "prm_num", "low_", "high_", "digits_" },
                    new string[] { "1121", "Рассрочка(процент) по газу", "float", "1", "0", "100", "4" });
                Database.Insert(prm_name,
                    new string[] { "nzp_prm", "name_prm", "type_prm", "prm_num", "low_", "high_", "digits_" },
                    new string[] { "1122","Рассрочка(процент)","float","5","0","100","4" });
            }

            SetSchema(Bank.Data);
            SchemaQualifiedObjectName kredit = new SchemaQualifiedObjectName() { Name = "kredit", Schema = CurrentSchema };
            if (!Database.TableExists(kredit))
                Database.AddTable(kredit,
                    new Column("nzp_kredit", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("nzp_kvar", DbType.Int32, ColumnProperty.NotNull),
                    new Column("nzp_serv", DbType.Int32, ColumnProperty.NotNull),
                    new Column("dat_month", DbType.Date, ColumnProperty.NotNull),
                    new Column("dat_s", DbType.Date, ColumnProperty.NotNull),
                    new Column("dat_po", DbType.Date, ColumnProperty.NotNull),
                    new Column("valid", DbType.Int32, ColumnProperty.NotNull),
                    new Column("sum_dolg", DbType.Decimal.WithSize(14, 2), ColumnProperty.None, 0.00),
                    new Column("perc", DbType.Decimal.WithSize(5, 2), ColumnProperty.None, 0.00),
                    new Column("dog_num", DbType.String.WithSize(20)),
                    new Column("dog_dat", DbType.Date),
                    new Column("sum_real_p", DbType.Decimal.WithSize(14, 2)));
        }

        public override void Revert()
        {
            SetSchema(Bank.Data);
            SchemaQualifiedObjectName kredit = new SchemaQualifiedObjectName() { Name = "kredit", Schema = CurrentSchema };
            if (Database.TableExists(kredit)) Database.RemoveTable(kredit);
        }
    }

    [Migration(20140305020, MigrateDataBase.LocalBank)]
    public class Migration_20140305020_LocalBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);
            SchemaQualifiedObjectName kredit = new SchemaQualifiedObjectName() { Name = "kredit", Schema = CurrentSchema };
            if (!Database.TableExists(kredit))
                Database.AddTable(kredit,
                    new Column("nzp_kredit", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("nzp_kvar", DbType.Int32, ColumnProperty.NotNull),
                    new Column("nzp_serv", DbType.Int32, ColumnProperty.NotNull),
                    new Column("dat_month", DbType.Date, ColumnProperty.NotNull),
                    new Column("dat_s", DbType.Date, ColumnProperty.NotNull),
                    new Column("dat_po", DbType.Date, ColumnProperty.NotNull),
                    new Column("valid", DbType.Int32, ColumnProperty.NotNull),
                    new Column("sum_dolg", DbType.Decimal.WithSize(14, 2), ColumnProperty.None, 0.00),
                    new Column("perc", DbType.Decimal.WithSize(5, 2), ColumnProperty.None, 0.00),
                    new Column("dog_num", DbType.String.WithSize(20)),
                    new Column("dog_dat", DbType.Date),
                    new Column("sum_real_p", DbType.Decimal.WithSize(14, 2)));
        }

        public override void Revert()
        {
            SetSchema(Bank.Data);
            SchemaQualifiedObjectName kredit = new SchemaQualifiedObjectName() { Name = "kredit", Schema = CurrentSchema };
            if (Database.TableExists(kredit)) Database.RemoveTable(kredit);
        }
    }

    [Migration(20140305020, MigrateDataBase.Charge)]
    public class Migration_20140305020_Charge : Migration
    {
        public override void Apply()
        {
            for (int i = 1; i <= 12; i++)
            {
                SchemaQualifiedObjectName kredit = new SchemaQualifiedObjectName() { Name = string.Format("kredit_{0}", i.ToString("00")), Schema = CurrentSchema };
                if (!Database.TableExists(kredit))
                {
                    Database.AddTable(kredit,
                        new Column("nzp_kredx", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                        new Column("nzp_kvar", DbType.Int32, ColumnProperty.NotNull),
                        new Column("nzp_serv", DbType.Int32, ColumnProperty.NotNull),
                        new Column("nzp_kredit", DbType.Int32, ColumnProperty.NotNull),
                        new Column("sum_indolg", DbType.Decimal.WithSize(14, 2), ColumnProperty.None, 0.00),
                        new Column("sum_dolg", DbType.Decimal.WithSize(14, 2), ColumnProperty.None, 0.00),
                        new Column("sum_odna12", DbType.Decimal.WithSize(14, 2), ColumnProperty.None, 0.00),
                        new Column("sum_perc", DbType.Decimal.WithSize(14, 2), ColumnProperty.None, 0.00),
                        new Column("sum_charge", DbType.Decimal.WithSize(14, 2), ColumnProperty.None, 0.00),
                        new Column("sum_outdolg", DbType.Decimal.WithSize(14, 2), ColumnProperty.None, 0.00),
                        new Column("sum_money", DbType.Decimal.WithSize(14, 2)));
                    Database.AddIndex(string.Format("ixkrx1__{0}", i.ToString("00")), true, kredit, "nzp_kredx");
                    Database.AddIndex(string.Format("ixkrx2__{0}", i.ToString("00")), false, kredit, "nzp_kvar", "nzp_serv");
                    Database.AddIndex(string.Format("ixkrx3__{0}", i.ToString("00")), false, kredit, "nzp_kredit");
                }
            }
        }
        public override void Revert()
        {
            for (int i = 1; i <= 12; i++)
            {
                SchemaQualifiedObjectName kredit = new SchemaQualifiedObjectName() { Name = string.Format("kredit_{0}", i.ToString("00")), Schema = CurrentSchema };
                if (Database.TableExists(kredit)) Database.RemoveTable(kredit);
            }
        }
    }
}
