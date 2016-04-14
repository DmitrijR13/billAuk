using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2014._2014124
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2014122612403, MigrateDataBase.LocalBank)]
    public class Migration_2014122612403_LocalBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            // TODO: Upgrade LocalPref_Kernel

            SetSchema(Bank.Data);
            var otvetstv = new SchemaQualifiedObjectName() { Name = "otvetstv", Schema = CurrentSchema };

            if (!Database.TableExists(otvetstv))
            {
                Database.AddTable(otvetstv,
                   new Column("nzp_otv", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                   new Column("nzp_kvar", DbType.Int32, ColumnProperty.NotNull),
                   new Column("nzp_rod", DbType.Int32),
                   new Column("fam", DbType.StringFixedLength.WithSize(40)),
                   new Column("ima", DbType.StringFixedLength.WithSize(40)),
                   new Column("otch", DbType.StringFixedLength.WithSize(40)),
                   new Column("dat_rog", DbType.Date),
                   new Column("adress", DbType.StringFixedLength.WithSize(60)),
                   new Column("dop_info", DbType.StringFixedLength.WithSize(60)),
                   new Column("nzp_dok", DbType.Int32),
                   new Column("serij", DbType.StringFixedLength.WithSize(10)),
                   new Column("nomer", DbType.StringFixedLength.WithSize(7)),
                   new Column("vid_mes", DbType.StringFixedLength.WithSize(70)),
                   new Column("vid_dat", DbType.Date),
                   new Column("vipis_dat", DbType.Date),
                   new Column("nzp_gil", DbType.Int32),
                   new Column("dat_s", DbType.Date),
                   new Column("dat_po", DbType.Date),
                   new Column("is_actual", DbType.Int32, ColumnProperty.NotNull, 1),
                   new Column("nzp_user", DbType.Int32),
                   new Column("dat_when", DbType.Date)
                   );
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
