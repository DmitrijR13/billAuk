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
    [Migration(2014101610201, MigrateDataBase.CentralBank)]
    public class Migration_2014101610201_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Debt);
            var lawsuit = new SchemaQualifiedObjectName { Name = "lawsuit", Schema = CurrentSchema };
            if (Database.TableExists(lawsuit))
            {
                if (!Database.ColumnExists(lawsuit, "lawsuit_price_peni"))
                {
                    Database.AddColumn(lawsuit, new Column("lawsuit_price_peni", DbType.Decimal.WithSize(14, 2), defaultValue: 0.00));
                }
                if (!Database.ColumnExists(lawsuit, "lawsuit_from"))
                {
                    Database.AddColumn(lawsuit, new Column("lawsuit_from", DbType.Date));
                }
                if (!Database.ColumnExists(lawsuit, "lawsuit_to"))
                {
                    Database.AddColumn(lawsuit, new Column("lawsuit_to", DbType.Date));
                }
                if (!Database.ColumnExists(lawsuit, "decide_number"))
                {
                    Database.AddColumn(lawsuit, new Column("decide_number", DbType.String));
                }
                if (!Database.ColumnExists(lawsuit, "dn_lawsuit_price"))
                {
                    Database.AddColumn(lawsuit, new Column("dn_lawsuit_price", DbType.Decimal.WithSize(14, 2), defaultValue: 0.00));
                }
                if (!Database.ColumnExists(lawsuit, "dn_tax"))
                {
                    Database.AddColumn(lawsuit, new Column("dn_tax", DbType.Decimal.WithSize(14, 2), defaultValue: 0.00));
                }
                if (!Database.ColumnExists(lawsuit, "dn_date"))
                {
                    Database.AddColumn(lawsuit, new Column("dn_date", DbType.Date));
                }
                if (!Database.ColumnExists(lawsuit, "dn_lawsuit_price_peni"))
                {
                    Database.AddColumn(lawsuit, new Column("dn_lawsuit_price_peni", DbType.Decimal.WithSize(14, 2), defaultValue: 0.00));
                }
                if (!Database.ColumnExists(lawsuit, "il_number"))
                {
                    Database.AddColumn(lawsuit, new Column("il_number", DbType.String));
                }
                if (!Database.ColumnExists(lawsuit, "il_where_directed"))
                {
                    Database.AddColumn(lawsuit, new Column("il_where_directed", DbType.String));
                }
                if (!Database.ColumnExists(lawsuit, "is_executed"))
                {
                    Database.AddColumn(lawsuit, new Column("is_executed", DbType.Int32, defaultValue: 0));
                }
            }
        }

        public override void Revert()
        {
            SetSchema(Bank.Debt);
            var lawsuit = new SchemaQualifiedObjectName { Name = "lawsuit", Schema = CurrentSchema };
            if (Database.TableExists(lawsuit))
            {
                if (Database.ColumnExists(lawsuit, "lawsuit_price_peni"))
                {
                    Database.RemoveColumn(lawsuit, "lawsuit_price_peni");
                }
                if (Database.ColumnExists(lawsuit, "lawsuit_from"))
                {
                    Database.RemoveColumn(lawsuit, "lawsuit_from");
                }
                if (Database.ColumnExists(lawsuit, "lawsuit_to"))
                {
                    Database.RemoveColumn(lawsuit, "lawsuit_to");
                }
                if (Database.ColumnExists(lawsuit, "decide_number"))
                {
                    Database.RemoveColumn(lawsuit, "decide_number");
                }
                if (Database.ColumnExists(lawsuit, "dn_lawsuit_price"))
                {
                    Database.RemoveColumn(lawsuit, "dn_lawsuit_price");
                }
                if (Database.ColumnExists(lawsuit, "dn_tax"))
                {
                    Database.RemoveColumn(lawsuit, "dn_tax");
                }
                if (Database.ColumnExists(lawsuit, "dn_date"))
                {
                    Database.RemoveColumn(lawsuit, "dn_date");
                }
                if (Database.ColumnExists(lawsuit, "dn_lawsuit_price_peni"))
                {
                    Database.RemoveColumn(lawsuit, "dn_lawsuit_price_peni");
                }
                if (Database.ColumnExists(lawsuit, "il_number"))
                {
                    Database.RemoveColumn(lawsuit, "il_number");
                }
                if (Database.ColumnExists(lawsuit, "il_where_directed"))
                {
                    Database.RemoveColumn(lawsuit, "il_where_directed");
                }
                if (Database.ColumnExists(lawsuit, "is_executed"))
                {
                    Database.RemoveColumn(lawsuit, "is_executed");
                }
            }
        }
    }
}
