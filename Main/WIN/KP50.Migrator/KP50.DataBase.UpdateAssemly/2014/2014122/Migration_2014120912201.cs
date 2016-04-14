using System;
using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2014._2014122
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2014120912201, MigrateDataBase.CentralBank)]
    public class Migration_2014120912201_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName prm_name = new SchemaQualifiedObjectName() { Name = "prm_name", Schema = CurrentSchema };
            if (Database.TableExists(prm_name))
            {
                var count =
                    Convert.ToInt32(Database.ExecuteScalar("SELECT count(*) FROM " + prm_name.Schema + Database.TableDelimiter +
                                           prm_name.Name + " WHERE nzp_prm = 2085"));
                if (count == 0)
                {
                    Database.Insert(prm_name,
                    new[] { "nzp_prm", "name_prm", "type_prm", "prm_num", "low_", "high_", "digits_" },
                    new[] { "2085", "Расход ОДПУ по услуге Электроснабжение не должен превышать, Квт", "float", "10", "0", "1000000", "7" });
                }

                count =
                    Convert.ToInt32(Database.ExecuteScalar("SELECT count(*) FROM " + prm_name.Schema + Database.TableDelimiter +
                                           prm_name.Name + " WHERE nzp_prm = 2086"));
                if (count == 0)
                {
                    Database.Insert(prm_name,
                    new[] { "nzp_prm", "name_prm", "type_prm", "prm_num", "low_", "high_", "digits_" },
                    new[] { "2086", "Расход ОДПУ по услуге ГВС не должен превышать, м3", "float", "10", "0", "1000000", "7" });
                }

                count =
                    Convert.ToInt32(Database.ExecuteScalar("SELECT count(*) FROM " + prm_name.Schema + Database.TableDelimiter +
                                           prm_name.Name + " WHERE nzp_prm = 2087"));
                if (count == 0)
                {
                    Database.Insert(prm_name,
                    new[] { "nzp_prm", "name_prm", "type_prm", "prm_num", "low_", "high_", "digits_" },
                    new[] { "2087", "Расход ОДПУ по услуге ХВС не должен превышать, м3", "float", "10", "0", "1000000", "7" });
                }

                count =
                    Convert.ToInt32(Database.ExecuteScalar("SELECT count(*) FROM " + prm_name.Schema + Database.TableDelimiter +
                                           prm_name.Name + " WHERE nzp_prm = 2088"));
                if (count == 0)
                {
                    Database.Insert(prm_name,
                        new[] { "nzp_prm", "name_prm", "type_prm", "prm_num", "low_", "high_", "digits_" },
                        new[] { "2088", "Расход ОДПУ по услуге по услуге Газ не должен превышать, м3", "float", "10", "0", "1000000", "7" });
                }

            }


            SetSchema(Bank.Data);
            SchemaQualifiedObjectName s_group = new SchemaQualifiedObjectName() { Name = "s_group", Schema = CurrentSchema };
            if (Database.TableExists(s_group))
            {
                Database.Delete(s_group, " nzp_group in (3,4,5,6,7,8,9,10,11,12)");
                Database.Insert(s_group, new[] { "nzp_group", "ngroup" }, new[] { "3", "П-рассогл. в распр оплат" });
                Database.Insert(s_group, new[] { "nzp_group", "ngroup" }, new[] { "4", "П-рассогл. учета оплат в сальдо" });
                Database.Insert(s_group, new[] { "nzp_group", "ngroup" }, new[] { "5", "П-рассогл. в перекидках оплат" });
                Database.Insert(s_group, new[] { "nzp_group", "ngroup" }, new[] { "6", "П-превыш.показ. в ИПУ" });
                Database.Insert(s_group, new[] { "nzp_group", "ngroup" }, new[] { "7", "П-превыш.показ. в квартирн.ПУ" });
                Database.Insert(s_group, new[] { "nzp_group", "ngroup" }, new[] { "8", "П-превыш.показ. в группов.ПУ" });
                Database.Insert(s_group, new[] { "nzp_group", "ngroup" }, new[] { "9", "П-превыш.показ. в ОДПУ" });
                Database.Insert(s_group, new[] { "nzp_group", "ngroup" }, new[] { "10", "П-недопост. не учтена в расчете" });
                Database.Insert(s_group, new[] { "nzp_group", "ngroup" }, new[] { "11", "П-изм.расхода ПУ после рассчит.ОДН" });
                Database.Insert(s_group, new[] { "nzp_group", "ngroup" }, new[] { "12", "П-изм.пар-ров ЛС после рассчета" });
            }


            SchemaQualifiedObjectName checkChMon = new SchemaQualifiedObjectName() { Name = "checkchmon", Schema = CurrentSchema };
            if (Database.TableExists(checkChMon))
            {
                if (!Database.ColumnExists(checkChMon, "nzp_exc"))
                    Database.AddColumn(checkChMon, new Column("nzp_exc", DbType.Int32));
            }

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

    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2014120912201, MigrateDataBase.LocalBank)]
    public class Migration_2014120912201_LocalBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName prm_name = new SchemaQualifiedObjectName() { Name = "prm_name", Schema = CurrentSchema };
            if (Database.TableExists(prm_name))
            {
                var count =
                    Convert.ToInt32(Database.ExecuteScalar("SELECT count(*) FROM " + prm_name.Schema + Database.TableDelimiter +
                                           prm_name.Name + " WHERE nzp_prm = 2085"));
                if (count == 0)
                {
                    Database.Insert(prm_name,
                    new[] { "nzp_prm", "name_prm", "type_prm", "prm_num", "low_", "high_", "digits_" },
                    new[] { "2085", "Расход ОДПУ по услуге Электроснабжение не должен превышать, Квт", "float", "10", "0", "1000000", "7" });
                }

                count =
                    Convert.ToInt32(Database.ExecuteScalar("SELECT count(*) FROM " + prm_name.Schema + Database.TableDelimiter +
                                           prm_name.Name + " WHERE nzp_prm = 2086"));
                if (count == 0)
                {
                    Database.Insert(prm_name,
                    new[] { "nzp_prm", "name_prm", "type_prm", "prm_num", "low_", "high_", "digits_" },
                    new[] { "2086", "Расход ОДПУ по услуге ГВС не должен превышать, м3", "float", "10", "0", "1000000", "7" });
                }

                count =
                    Convert.ToInt32(Database.ExecuteScalar("SELECT count(*) FROM " + prm_name.Schema + Database.TableDelimiter +
                                           prm_name.Name + " WHERE nzp_prm = 2087"));
                if (count == 0)
                {
                    Database.Insert(prm_name,
                    new[] { "nzp_prm", "name_prm", "type_prm", "prm_num", "low_", "high_", "digits_" },
                    new[] { "2087", "Расход ОДПУ по услуге ХВС не должен превышать, м3", "float", "10", "0", "1000000", "7" });
                }

                count =
                    Convert.ToInt32(Database.ExecuteScalar("SELECT count(*) FROM " + prm_name.Schema + Database.TableDelimiter +
                                           prm_name.Name + " WHERE nzp_prm = 2088"));
                if (count == 0)
                {
                    Database.Insert(prm_name,
                        new[] { "nzp_prm", "name_prm", "type_prm", "prm_num", "low_", "high_", "digits_" },
                        new[] { "2088", "Расход ОДПУ по услуге по услуге Газ не должен превышать, м3", "float", "10", "0", "1000000", "7" });
                }

            }


            SetSchema(Bank.Data);
            SchemaQualifiedObjectName s_group = new SchemaQualifiedObjectName() { Name = "s_group", Schema = CurrentSchema };
            if (Database.TableExists(s_group))
            {
                Database.Delete(s_group, " nzp_group in (3,4,5,6,7,8,9,10,11,12)");
                Database.Insert(s_group, new[] { "nzp_group", "ngroup" }, new[] { "3", "П-рассогл. в распр оплат" });
                Database.Insert(s_group, new[] { "nzp_group", "ngroup" }, new[] { "4", "П-рассогл. учета оплат в сальдо" });
                Database.Insert(s_group, new[] { "nzp_group", "ngroup" }, new[] { "5", "П-рассогл. в перекидках оплат" });
                Database.Insert(s_group, new[] { "nzp_group", "ngroup" }, new[] { "6", "П-превыш.показ. в ИПУ" });
                Database.Insert(s_group, new[] { "nzp_group", "ngroup" }, new[] { "7", "П-превыш.показ. в квартирн.ПУ" });
                Database.Insert(s_group, new[] { "nzp_group", "ngroup" }, new[] { "8", "П-превыш.показ. в группов.ПУ" });
                Database.Insert(s_group, new[] { "nzp_group", "ngroup" }, new[] { "9", "П-превыш.показ. в ОДПУ" });
                Database.Insert(s_group, new[] { "nzp_group", "ngroup" }, new[] { "10", "П-недопост. не учтена в расчете" });
                Database.Insert(s_group, new[] { "nzp_group", "ngroup" }, new[] { "11", "П-изм.расхода ПУ после рассчит.ОДН" });
                Database.Insert(s_group, new[] { "nzp_group", "ngroup" }, new[] { "12", "П-изм.пар-ров ЛС после рассчета" });
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
