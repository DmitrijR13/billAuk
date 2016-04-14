using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2014._2014102
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2014103010403, MigrateDataBase.CentralBank)]
    public class Migration_2014103010403_CentralBank : Migration
    {
        public override void Apply()
        {
            if (Database.ProviderName == "PostgreSQL")
            {
                SetSchema(Bank.Kernel);

                #region Справочник типов задолженностей
                SchemaQualifiedObjectName s_peni_type_debt = new SchemaQualifiedObjectName();
                s_peni_type_debt.Name = "s_peni_type_debt";
                s_peni_type_debt.Schema = CurrentSchema;
                if (!Database.TableExists(s_peni_type_debt))
                {
                    Database.AddTable(s_peni_type_debt,
                       new Column("id", DbType.Int32, ColumnProperty.PrimaryKeyWithIdentity | ColumnProperty.NotNull),
                       new Column("type_debt", DbType.String)
                       );
                    if (Database.TableExists(s_peni_type_debt))
                    {
                        Database.Insert(s_peni_type_debt, new string[] { "id", "type_debt" }, new string[] { "1", "Повышение задолженности" });
                        Database.Insert(s_peni_type_debt, new string[] { "id", "type_debt" }, new string[] { "2", "Снятие задолженности" });
                    }
                }
                #endregion

                #region Справочник типов проводок
                SchemaQualifiedObjectName s_prov_types = new SchemaQualifiedObjectName();
                s_prov_types.Name = "s_prov_types";
                s_prov_types.Schema = CurrentSchema;
                if (!Database.TableExists(s_prov_types))
                {
                    Database.AddTable(s_prov_types,
                       new Column("id", DbType.Int32, ColumnProperty.PrimaryKeyWithIdentity | ColumnProperty.NotNull),
                       new Column("type_prov", DbType.String),
                       new Column("sign_plus", DbType.Boolean, ColumnProperty.NotNull, "true"),
                       new Column("source_id", DbType.String.WithSize(50)),
                       new Column("source", DbType.String.WithSize(50))
                       );
                    if (Database.TableExists(s_prov_types))
                    {
                        Database.Insert(s_prov_types, new string[] { "id", "type_prov", "sign_plus", "source_id", "source" },
                            new string[] { "1", "Начисления", "true", "nzp_charge", "charge_xx" });
                        Database.Insert(s_prov_types, new string[] { "id", "type_prov", "sign_plus", "source_id", "source" },
                            new string[] { "2", "Оплаты на счет РЦ", "false", "nzp_to", "fn_supplier_xx" });
                        Database.Insert(s_prov_types, new string[] { "id", "type_prov", "sign_plus", "source_id", "source" },
                            new string[] { "3", "Оплаты от поставщиков", "false", "nzp_to", "from_supplier" });
                        Database.Insert(s_prov_types, new string[] { "id", "type_prov", "sign_plus", "source_id", "source" },
                            new string[] { "4", "Перекидки оплат", "false", "nzp_to", "del_supplier" });
                        Database.Insert(s_prov_types, new string[] { "id", "type_prov", "sign_plus", "source_id", "source" },
                            new string[] { "5", "Недопоставки", "false", "nzp_charge", "charge_xx" });
                        Database.Insert(s_prov_types, new string[] { "id", "type_prov", "sign_plus", "source_id", "source" },
                            new string[] { "6", "Перерасчет", "true", "nzp_charge", "charge_xx" });
                        Database.Insert(s_prov_types, new string[] { "id", "type_prov", "sign_plus", "source_id", "source" },
                            new string[] { "7", "Введено вручную", "true", "nzp_charge", "charge_xx" });
                    }
                }
                #endregion

                #region Не расчитывать пени по услугам для договоров
                SchemaQualifiedObjectName peni_no_calc = new SchemaQualifiedObjectName();
                peni_no_calc.Name = "peni_no_calc";
                peni_no_calc.Schema = CurrentSchema;
                if (!Database.TableExists(peni_no_calc))
                {
                    Database.AddTable(peni_no_calc,
                       new Column("id", DbType.Int32, ColumnProperty.NotNull | ColumnProperty.Unique | ColumnProperty.PrimaryKeyWithIdentity),
                       new Column("nzp_serv", DbType.Int32, ColumnProperty.NotNull),
                       new Column("nzp_supp", DbType.Int32, ColumnProperty.NotNull),
                       new Column("date_from", DbType.DateTime),
                       new Column("date_to", DbType.DateTime),
                       new Column("is_actual", DbType.Int32),
                       new Column("created_on", DbType.DateTime),
                       new Column("created_by", DbType.Int32),
                       new Column("changed_on", DbType.DateTime),
                       new Column("changed_by", DbType.Int32)
                       );

                    if (Database.TableExists(peni_no_calc))
                    {
                        Database.AddIndex("ix1_peni_no_calc", false, peni_no_calc, "id", "nzp_serv", "nzp_supp");
                    }
                }
                #endregion
            }
        }

        public override void Revert()
        {
            DropTableCascade("s_peni_type_debt");
            DropTableCascade("s_prov_types");
            DropTableCascade("peni_no_calc");
        }

        private void DropTableCascade(string table)
        {
            Database.ExecuteNonQuery("DROP TABLE IF EXISTS " + CentralKernel + Database.TableDelimiter + " " + table + " CASCADE");
        }

    }

}
