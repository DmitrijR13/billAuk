using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2014._2014092
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2014091209202, MigrateDataBase.CentralBank)]
    public class Migration_2014091209202_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName prm_name = new SchemaQualifiedObjectName() { Name = "prm_name", Schema = CurrentSchema };

            IDataReader reader;
            if (Database.ProviderName == "PostgreSQL")
            {
                reader = Database.ExecuteReader("SELECT (max(nzp_prm) + 1) as count FROM " + prm_name);

                try
                {
                    while (reader.Read())
                    {
                        string count = reader["count"].ToString();
                        Database.ExecuteNonQuery("ALTER SEQUENCE " + prm_name + "_nzp_prm_seq RESTART " + count);
                    }
                }
                finally
                {
                    reader.Close();
                }
            }

        }

        public override void Revert()
        {
            SetSchema(Bank.Kernel);
            // TODO: Downgrade CentralPref_Kernel

            SetSchema(Bank.Data);
            // TODO: Downgrade CentralPref_Data

            SetSchema(Bank.Upload);
            // TODO: Downgrade CentralPref_Upload
        }
    }
}
