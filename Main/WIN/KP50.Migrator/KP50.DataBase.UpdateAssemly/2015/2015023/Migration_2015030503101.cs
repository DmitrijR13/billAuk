using System;
using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2015030503101, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_2015030503101 : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName supplier = new SchemaQualifiedObjectName()
            {
                Name = "supplier",
                Schema = CurrentSchema
            };

            //корректируем данные
            Database.ExecuteNonQuery(
                    " UPDATE " + supplier.Schema + Database.TableDelimiter + supplier.Name +
                    " SET name_supp = 'код '||nzp_supp "+
                    " WHERE name_supp IS NULL OR TRIM(name_supp) = ''");
            
            //ставим NOT NULL
            Database.ChangeColumn(supplier, "name_supp", DbType.String.WithSize(100), true);


            //nzp_supp = 0 должно быть "Итого(системное значение для расчета)"
            var count =
                Convert.ToInt32(Database.ExecuteScalar(
                    " SELECT count(*) " +
                    " FROM " + supplier.Schema + Database.TableDelimiter + supplier.Name +
                    " WHERE nzp_supp = 0 "));

            if (count == 0)
            {
                Database.Insert(
                    supplier,
                    new[] {"nzp_supp", "name_supp"},
                    new[] {"0", "Итого(системное значение для расчета)"});
            }
            else
            {
                Database.Update(
                    supplier, 
                    new [] {"name_supp"},
                    new[] { "Итого(системное значение для расчета)" },
                    "nzp_supp = 0");
            }
        }
    }
}
