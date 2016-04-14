using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(20140305012, MigrateDataBase.CentralBank)]
    public class Migration_20140305012_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName s_dep_types = new SchemaQualifiedObjectName() { Name = "s_dep_types", Schema = CurrentSchema };
            if (!Database.TableExists(s_dep_types))
                Database.AddTable(s_dep_types,
                    new Column("nzp_dep", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("name_dep", DbType.String.WithSize(100)));
            Database.Delete(s_dep_types);
            Database.Insert(s_dep_types, new string[] { "nzp_dep", "name_dep" }, new string[] { "1", "Перерасчет" });
            Database.Insert(s_dep_types, new string[] { "nzp_dep", "name_dep" }, new string[] { "2", "Недопоставка" });
            Database.Insert(s_dep_types, new string[] { "nzp_dep", "name_dep" }, new string[] { "3", "Платежный документ" });

            SetSchema(Bank.Data);
            SchemaQualifiedObjectName saldo_date_area = new SchemaQualifiedObjectName() { Name = "saldo_date_area", Schema = CurrentSchema };
            if (!Database.TableExists(saldo_date_area))
                Database.AddTable(saldo_date_area,
                    new Column("id", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("nzp_area", DbType.Int32, ColumnProperty.NotNull),
                    new Column("month_", DbType.Int32, ColumnProperty.NotNull),
                    new Column("year_", DbType.Int32, ColumnProperty.NotNull),
                    new Column("is_current", DbType.Int16),
                    new Column("prev_month", DbType.Int32),
                    new Column("prev_year", DbType.Int32),
                    new Column("changed_by", DbType.Int32),
                    new Column("changed_on", DbType.DateTime));

            SchemaQualifiedObjectName dep_servs = new SchemaQualifiedObjectName() { Name = "dep_servs", Schema = CurrentSchema };
            if (!Database.TableExists(dep_servs))
                Database.AddTable(dep_servs,
                    new Column("nzp_dep_servs", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("nzp_dep", DbType.Int32, ColumnProperty.NotNull),
                    new Column("nzp_serv", DbType.Int32, ColumnProperty.NotNull),
                    new Column("nzp_serv_slave", DbType.Int32, ColumnProperty.NotNull),
                    new Column("nzp_area", DbType.Int32, ColumnProperty.None, 0),
                    new Column("dat_s", DbType.Date),
                    new Column("dat_po", DbType.Date),
                    new Column("is_actual", DbType.Int32, ColumnProperty.None, 1));
        }

        public override void Revert()
        {
            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName s_dep_types = new SchemaQualifiedObjectName() { Name = "s_dep_types", Schema = CurrentSchema };
            if (Database.TableExists(s_dep_types)) Database.RemoveTable(s_dep_types);

            SetSchema(Bank.Data);
            SchemaQualifiedObjectName saldo_date_area = new SchemaQualifiedObjectName() { Name = "saldo_date_area", Schema = CurrentSchema };
            SchemaQualifiedObjectName dep_servs = new SchemaQualifiedObjectName() { Name = "dep_servs", Schema = CurrentSchema };
            if (Database.TableExists(saldo_date_area)) Database.RemoveTable(saldo_date_area);
            if (Database.TableExists(dep_servs)) Database.RemoveTable(dep_servs);
        }
    }
}
