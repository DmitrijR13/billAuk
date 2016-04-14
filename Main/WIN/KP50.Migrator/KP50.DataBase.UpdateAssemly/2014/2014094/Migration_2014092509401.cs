using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2014092509401, MigrateDataBase.CentralBank)]
    public class Migration_2014092509401_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);

            #region список услуг для нормативов
            SchemaQualifiedObjectName s_serv_for_norm = new SchemaQualifiedObjectName();
            s_serv_for_norm.Name = "s_serv_for_norm";
            s_serv_for_norm.Schema = CurrentSchema;
            if (!Database.TableExists(s_serv_for_norm))
            {
                Database.AddTable(s_serv_for_norm,
                   new Column("nzp_serv", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                   new Column("service", DbType.String),
                   new Column("ordering", DbType.Int32)
                   );
                if (Database.TableExists(s_serv_for_norm))
                {
                    Database.AddIndex("s_serv_for_norm_ix4", false, s_serv_for_norm, "nzp_serv");
                    Database.ExecuteNonQuery("INSERT INTO s_serv_for_norm (nzp_serv,service,ordering) SELECT nzp_serv,service,ordering FROM services WHERE nzp_serv in (6,7,25,9,10)");
                    Database.Insert(s_serv_for_norm, new string[] { "nzp_serv", "service", "ordering" }, new string[] { "1007", "Канализация ГВС", "6" });
                }
            }
            #endregion

            #region типы нормативов
            SchemaQualifiedObjectName norm_types = new SchemaQualifiedObjectName();
            norm_types.Name = "norm_types";
            norm_types.Schema = CurrentSchema;
            if (!Database.TableExists(norm_types))
            {
                Database.AddTable(norm_types,
                   new Column("id", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                   new Column("nzp_serv", DbType.Int32),
                   new Column("s_kind_norm_id", DbType.Int32),
                   new Column("nzp_measure", DbType.Int32),
                   new Column("name_type_norm", DbType.String),
                   new Column("date_from", DbType.Date),
                   new Column("date_to", DbType.Date),
                   new Column("norm_doc_id", DbType.Int32),
                   new Column("is_finished", DbType.Boolean),
                   new Column("created_by", DbType.Int32),
                   new Column("created_on", DbType.DateTime),
                   new Column("changed_by", DbType.Int32),
                   new Column("changed_on", DbType.DateTime),
                   new Column("is_day_period", DbType.Boolean)
                   );
                if (Database.TableExists(norm_types))
                {
                    Database.AddIndex("norm_types_ix1", false, norm_types, "id", "nzp_serv", "nzp_measure");
                }
            }
            #endregion

            #region сигнатуры типов нормативов
            SchemaQualifiedObjectName norm_types_sign = new SchemaQualifiedObjectName();
            norm_types_sign.Name = "norm_types_sign";
            norm_types_sign.Schema = CurrentSchema;
            if (!Database.TableExists(norm_types_sign))
            {
                Database.AddTable(norm_types_sign,
                   new Column("id", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                   new Column("norm_type_id", DbType.Int32),
                   new Column("nzp_prm_ls", DbType.Int32, ColumnProperty.NotNull),
                   new Column("nzp_prm_house", DbType.Int32, ColumnProperty.NotNull),
                   new Column("min_val", DbType.Int32),
                   new Column("max_val", DbType.Int32),
                   new Column("max_count", DbType.Int32),
                   new Column("type_val_sign_id", DbType.Int32),
                   new Column("ordering", DbType.Int32),
                   new Column("is_finished", DbType.Boolean)
                   );
                if (Database.TableExists(norm_types_sign))
                {
                    Database.AddIndex("norm_types_sign_ix1", false, norm_types_sign, "id", "norm_type_id", "nzp_prm_ls", "nzp_prm_house");
                }
            }
            #endregion

            #region нормативы
            SchemaQualifiedObjectName norm_tables = new SchemaQualifiedObjectName();
            norm_tables.Name = "norm_tables";
            norm_tables.Schema = CurrentSchema;
            if (!Database.TableExists(norm_tables))
            {
                Database.AddTable(norm_tables,
                    new Column("id", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("norm_type_id", DbType.Int32),
                    new Column("norm_value", DbType.Decimal.WithSize(14, 7)),
                    new Column("created_on", DbType.DateTime),
                    new Column("changed_by", DbType.Int32),
                    new Column("changed_on", DbType.DateTime)
                    );
                if (Database.TableExists(norm_tables))
                {
                    Database.AddIndex("norm_tables_ix1", false, norm_tables, "id", "norm_type_id");
                }
            }
            #endregion

            #region влиящие параметры
            SchemaQualifiedObjectName influence_params = new SchemaQualifiedObjectName();
            influence_params.Name = "influence_params";
            influence_params.Schema = CurrentSchema;
            if (!Database.TableExists(influence_params))
            {
                Database.AddTable(influence_params,
                   new Column("id", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                   new Column("norm_tables_id", DbType.Int32, ColumnProperty.NotNull),
                   new Column("nzp_prm", DbType.Int32, ColumnProperty.NotNull),
                   new Column("param_value1", DbType.Decimal.WithSize(14, 7)),
                   new Column("param_value2", DbType.Decimal.WithSize(14, 7)),
                   new Column("date_value1", DbType.DateTime),
                   new Column("date_value2", DbType.DateTime),
                   new Column("name_prm_val", DbType.String),
                   new Column("ordering", DbType.Int32),
                   new Column("created_on", DbType.DateTime),
                   new Column("changed_by", DbType.Int32),
                   new Column("changed_on", DbType.DateTime)
                   );
                if (Database.TableExists(influence_params))
                {
                    Database.AddIndex("influence_params_ix1", false, influence_params, "id", "norm_tables_id", "nzp_prm");
                    Database.AddIndex("influence_params_ix2", false, influence_params, "id", "norm_tables_id", "nzp_prm", "param_value1");
                    Database.AddIndex("influence_params_ix3", false, influence_params, "id", "norm_tables_id", "nzp_prm", "param_value1", "param_value2", "date_value1", "date_value2");
                }
            }
            #endregion

            #region справочник типов параметров
            SchemaQualifiedObjectName s_type_val_sign = new SchemaQualifiedObjectName();
            s_type_val_sign.Name = "s_type_val_sign";
            s_type_val_sign.Schema = CurrentSchema;
            if (!Database.TableExists(s_type_val_sign))
            {
                Database.AddTable(s_type_val_sign,
                    new Column("id", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("description", DbType.String)
                    );
                if (Database.TableExists(s_type_val_sign))
                {
                    Database.Insert(s_type_val_sign, new string[] { "id", "description" }, new string[] { "1", "Число" });
                    Database.Insert(s_type_val_sign, new string[] { "id", "description" }, new string[] { "2", "Дата" });
                    Database.Insert(s_type_val_sign, new string[] { "id", "description" }, new string[] { "3", "Период" });
                    Database.Insert(s_type_val_sign, new string[] { "id", "description" }, new string[] { "4", "Период значений" });
                    Database.Insert(s_type_val_sign, new string[] { "id", "description" }, new string[] { "5", "Да/Нет" });
                    Database.Insert(s_type_val_sign, new string[] { "id", "description" }, new string[] { "6", "Справочник" });
                }
            }
            #endregion

            #region справочник типов нормативов (ОДН/не ОДН)
            SchemaQualifiedObjectName s_kind_norm = new SchemaQualifiedObjectName();
            s_kind_norm.Name = "s_kind_norm";
            s_kind_norm.Schema = CurrentSchema;
            if (!Database.TableExists(s_kind_norm))
            {
                Database.AddTable(s_kind_norm,
                    new Column("id", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("description", DbType.String)
                    );
                if (Database.TableExists(s_kind_norm))
                {
                    Database.Insert(s_kind_norm, new string[] { "id", "description" }, new string[] { "1", "Норматив для населения" });
                    Database.Insert(s_kind_norm, new string[] { "id", "description" }, new string[] { "2", "Норматив для ОДН" });
                }
            }
            #endregion

            #region список банков действия для типов нормативов
            SchemaQualifiedObjectName norm_banks = new SchemaQualifiedObjectName();
            norm_banks.Name = "norm_banks";
            norm_banks.Schema = CurrentSchema;
            if (!Database.TableExists(norm_banks))
            {
                Database.AddTable(norm_banks,
                    new Column("norm_type_id", DbType.Int32),
                    new Column("nzp_wp", DbType.Int32)
                    );
            }
            #endregion

            SetSchema(Bank.Data);
            SchemaQualifiedObjectName prm_5 = new SchemaQualifiedObjectName();
            prm_5.Name = "prm_5";
            prm_5.Schema = CurrentSchema;
            Database.Delete(prm_5, "nzp_prm = 1984");
            Database.Delete(prm_5, "nzp_prm = 1983");

            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName prm_name = new SchemaQualifiedObjectName();
            prm_name.Name = "prm_name";
            prm_name.Schema = CurrentSchema;
            if (Database.TableExists(prm_name))
            {
                Database.Delete(prm_name, "nzp_prm = 1984");
                Database.Insert(prm_name, new string[] { "nzp_prm", "name_prm", "type_prm", "prm_num", "is_day_uchet" },
                    new string[] { "1984", "Услуга для ЖКУ (ГВС)", "serv", "5", "0" });
            }

            SetSchema(Bank.Data);
            if (Database.TableExists(prm_5))
            {
                Database.Insert(prm_5, new string[] { "nzp", "nzp_prm", "dat_s", "dat_po", "val_prm", "is_actual"},
                    new string[] { "0", "1984", "1900-01-01", "3000-01-01", "9", "1" });

                Database.Insert(prm_5, new string[] { "nzp", "nzp_prm", "dat_s", "dat_po", "val_prm", "is_actual" },
                 new string[] { "0", "1983", "1900-01-01", "3000-01-01", "1", "100" }); //по умолчанию новые нормативы неактивны
            }

        }

        public override void Revert()
        {

        }
    }
}