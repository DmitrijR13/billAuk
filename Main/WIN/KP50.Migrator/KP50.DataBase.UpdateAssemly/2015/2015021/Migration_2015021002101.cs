using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015021
{
    [Migration(2015021002101, MigrateDataBase.CentralBank)]
    public class Migration_2015021002101_CentralBank : Migration
    {
        public override void Apply() {
            SetSchema(Bank.Data);
            var tulaExSzFile = new SchemaQualifiedObjectName
            {
                Name = "tula_ex_sz_file", 
                Schema = CurrentSchema
            };
            if (Database.TableExists(tulaExSzFile))
            {
                if (!Database.ColumnExists(tulaExSzFile, "gku16")) Database.AddColumn(tulaExSzFile, new Column("gku16",DbType.StringFixedLength.WithSize(100)));
                if (!Database.ColumnExists(tulaExSzFile, "tarif16")) Database.AddColumn(tulaExSzFile, new Column("tarif16", DbType.Decimal.WithSize(15, 5)));
                if (!Database.ColumnExists(tulaExSzFile, "sum16")) Database.AddColumn(tulaExSzFile, new Column("sum16", DbType.Decimal.WithSize(14, 4)));
                if (!Database.ColumnExists(tulaExSzFile, "fakt16")) Database.AddColumn(tulaExSzFile, new Column("fakt16", DbType.Decimal.WithSize(14, 4)));
                if (!Database.ColumnExists(tulaExSzFile, "org16")) Database.AddColumn(tulaExSzFile, new Column("org16", DbType.StringFixedLength.WithSize(30)));
                if (!Database.ColumnExists(tulaExSzFile, "vidtar16")) Database.AddColumn(tulaExSzFile, new Column("vidtar16", DbType.Int32));
                if (!Database.ColumnExists(tulaExSzFile, "koef16")) Database.AddColumn(tulaExSzFile, new Column("koef16", DbType.Decimal.WithSize(12, 2)));
                if (!Database.ColumnExists(tulaExSzFile, "lchet16")) Database.AddColumn(tulaExSzFile, new Column("lchet16", DbType.StringFixedLength.WithSize(24)));
                if (!Database.ColumnExists(tulaExSzFile, "sumz16")) Database.AddColumn(tulaExSzFile, new Column("sumz16", DbType.Decimal.WithSize(14, 4)));
                if (!Database.ColumnExists(tulaExSzFile, "klmz16")) Database.AddColumn(tulaExSzFile, new Column("klmz16", DbType.Int32));
                if (!Database.ColumnExists(tulaExSzFile, "ozs16")) Database.AddColumn(tulaExSzFile, new Column("ozs16", DbType.Int32));
                if (!Database.ColumnExists(tulaExSzFile, "sumozs16")) Database.AddColumn(tulaExSzFile, new Column("sumozs16", DbType.Decimal.WithSize(14, 4)));
            }
        }

        public override void Revert() {
            SetSchema(Bank.Data);
            var tulaExSzFile = new SchemaQualifiedObjectName
            {
                Name = "tula_ex_sz_file",
                Schema = CurrentSchema
            };
            if (Database.TableExists(tulaExSzFile))
            {
                if (Database.ColumnExists(tulaExSzFile, "gku16")) Database.RemoveColumn(tulaExSzFile, "gku16");
                if (Database.ColumnExists(tulaExSzFile, "tarif16")) Database.RemoveColumn(tulaExSzFile, "tarif16");
                if (Database.ColumnExists(tulaExSzFile, "sum16")) Database.RemoveColumn(tulaExSzFile, "sum16");
                if (Database.ColumnExists(tulaExSzFile, "fakt16")) Database.RemoveColumn(tulaExSzFile, "fakt16");
                if (Database.ColumnExists(tulaExSzFile, "org16")) Database.RemoveColumn(tulaExSzFile, "org16");
                if (Database.ColumnExists(tulaExSzFile, "vidtar16")) Database.RemoveColumn(tulaExSzFile, "vidtar16");
                if (Database.ColumnExists(tulaExSzFile, "koef16")) Database.RemoveColumn(tulaExSzFile, "koef16");
                if (Database.ColumnExists(tulaExSzFile, "lchet16")) Database.RemoveColumn(tulaExSzFile, "lchet16");
                if (Database.ColumnExists(tulaExSzFile, "sumz16")) Database.RemoveColumn(tulaExSzFile, "sumz16");
                if (Database.ColumnExists(tulaExSzFile, "klmz16")) Database.RemoveColumn(tulaExSzFile, "klmz16");
                if (Database.ColumnExists(tulaExSzFile, "ozs16")) Database.RemoveColumn(tulaExSzFile, "ozs16");
                if (Database.ColumnExists(tulaExSzFile, "sumozs16")) Database.RemoveColumn(tulaExSzFile, "sumozs16");
            }
        }
    }
}
