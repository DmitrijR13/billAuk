using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015021
{
    [Migration(2015020402106, Migrator.Framework.DataBase.Charge)]
    public class Migration_2015020402106_Charge : Migration
    {
        public override void Apply()
        {
            var months = new[] { "", "январь", "февраль", "март", "апрель", "май", "июнь", "июль", "август", "сентябрь", "октябрь", "ноябрь", "декабрь" };
            for (int mm = 1; mm <= 12; mm++)
            {
                var bill_archive_mm = new SchemaQualifiedObjectName { Name = "bill_archive_" + mm.ToString("00"), Schema = CurrentSchema };

                if (!Database.TableExists(bill_archive_mm))
                {
                    Database.AddTable(bill_archive_mm,
                        new Column("bill_id", DbType.Int32, ColumnProperty.NotNull | ColumnProperty.Identity),
                        new Column("kind", DbType.Int32, ColumnProperty.NotNull),
                        new Column("month", DbType.Int32),
                        new Column("year", DbType.Int32),
                        new Column("path", DbType.String.WithSize(1000)),
                        new Column("name", DbType.String),
                        new Column("print_date", DbType.DateTime, ColumnProperty.NotNull),
                        new Column("is_pack", DbType.Boolean),
                        new Column("num_ls", DbType.Int32),
                        new Column("sum_charge", DbType.Decimal),
                        new Column("sum_outsaldo", DbType.Decimal));
                }

                if (Database.TableExists(bill_archive_mm))
                {
                    Database.ExecuteNonQuery(
                        " COMMENT ON TABLE " + bill_archive_mm.Name + " IS 'Архив квитанций за " + months[mm] + "'; " +
                        " COMMENT ON COLUMN " + bill_archive_mm.Name + ".bill_id IS 'Номер записи'; " +
                        " COMMENT ON COLUMN " + bill_archive_mm.Name + ".kind IS 'Код формы квитанции в биллинге'; " +
                        " COMMENT ON COLUMN " + bill_archive_mm.Name + ".month IS 'Месяц за который сформирована квитанция'; " +
                        " COMMENT ON COLUMN " + bill_archive_mm.Name + ".year IS 'Год за который сформирована квитанция'; " +
                        " COMMENT ON COLUMN " + bill_archive_mm.Name + ".path IS 'Путь по которому находится скачанная квитанция'; " +
                        " COMMENT ON COLUMN " + bill_archive_mm.Name + ".name IS 'Название файла квитанции'; " +
                        " COMMENT ON COLUMN " + bill_archive_mm.Name + ".print_date IS 'Дата и время формирования квитанции'; " +
                        " COMMENT ON COLUMN " + bill_archive_mm.Name + ".is_pack IS 'Множественное формирование квитанций или единичная квитанция'; " +
                        " COMMENT ON COLUMN " + bill_archive_mm.Name + ".num_ls IS 'Лицевой счет для единичной квитанции'; " +
                        " COMMENT ON COLUMN " + bill_archive_mm.Name + ".sum_charge IS 'Начислено'; " +
                        " COMMENT ON COLUMN " + bill_archive_mm.Name + ".sum_outsaldo IS 'Исходящее сальдо'; ");
                }
            }
        }

        public override void Revert()
        {
            for (int mm = 1; mm <= 12; mm++)
            {
                var bill_archive_mm = new SchemaQualifiedObjectName { Name = "bill_archive_" + mm.ToString("00"), Schema = CurrentSchema };
                if (Database.TableExists(bill_archive_mm))
                {
                    Database.RemoveTable(bill_archive_mm);
                }
            }
        }
    }
}
