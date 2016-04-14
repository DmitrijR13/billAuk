using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using KP50.DataBase.Migrator.Framework;

namespace KP50.DataBase.UpdateAssembly._2015._2015064
{
    [Migration(2015122506401, Migrator.Framework.DataBase.Fin)]
    public class Migration_2015122506401 : Migration
    {
        public override void Apply()
        {
            var packLs = new SchemaQualifiedObjectName() { Name = "pack_ls", Schema = CurrentSchema };
            if (!Database.TableExists(packLs)) return;
            if (Database.ColumnExists(packLs, "calc_month")) return;
            Database.AddColumn(packLs, new Column("calc_month", DbType.Date));
        }
    }

    [Migration(2015122506401, Migrator.Framework.DataBase.Charge)]
    public class Migration_2015122506401_Charge : Migration
    {
        public override void Apply()
        {
            int i;
            var lstFnSupplier = new List<SchemaQualifiedObjectName>();
            for (i = 1; i <= 12; i++) lstFnSupplier.Add(new SchemaQualifiedObjectName() { Name = string.Format("fn_supplier{0}", i.ToString("00")), Schema = CurrentSchema });
            foreach (var fnSupplier in lstFnSupplier)
            {
                if (!Database.TableExists(fnSupplier)) continue;
                if (Database.ColumnExists(fnSupplier, "calc_month")) continue;
                Database.AddColumn(fnSupplier, new Column("calc_month", DbType.Date));
            }
            lstFnSupplier.Clear();

            var fromSupplier = new SchemaQualifiedObjectName() { Name = "from_supplier", Schema = CurrentSchema };
            if (!Database.TableExists(fromSupplier)) return;
            if (Database.ColumnExists(fromSupplier, "calc_month")) return;
            Database.AddColumn(fromSupplier, new Column("calc_month", DbType.Date));
        }
    }

    [Migration(2015122506401, Migrator.Framework.DataBase.CentralBank)]
    public class Migration_2015122506401_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Debt);
            var kreditPay = new SchemaQualifiedObjectName() { Name = "kredit_pay", Schema = CurrentSchema };
            if (!Database.TableExists(kreditPay)) return;
            if (Database.ColumnExists(kreditPay, "sum_money_main")) return;
            Database.AddColumn(kreditPay, new Column("sum_money_main", DbType.Decimal.WithSize(14, 2), ColumnProperty.NotNull, 0));
            if (Database.ColumnExists(kreditPay, "sum_indolg_main")) return;
            Database.AddColumn(kreditPay, new Column("sum_indolg_main", DbType.Decimal.WithSize(14, 2), ColumnProperty.NotNull, 0));
            if (Database.ColumnExists(kreditPay, "sum_outdolg_main")) return;
            Database.AddColumn(kreditPay, new Column("sum_outdolg_main", DbType.Decimal.WithSize(14, 2), ColumnProperty.NotNull, 0));
        }
    }
}
