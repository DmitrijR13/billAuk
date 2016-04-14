using System.Data;
using System.IO;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2015022102205, MigrateDataBase.LocalBank)]
    public class Migration_2015022102205 : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);

            if (Database.ProviderName == "PostgreSQL")
            {
                //удаляем мусорные табоица
                var reader =
                    Database.ExecuteReader("select tablename from pg_tables where schemaname='" + CurrentSchema +
                                           "' and tablename like 't_recalc_ids_%' ");
                try
                {
                    while (reader.Read())
                    {
                        Database.ExecuteNonQuery("DROP TABLE IF EXISTS " + reader["tablename"]);
                    }
                }
                finally
                {
                    reader.Close();
                }
            }
        }
    }
}
