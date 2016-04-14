using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015064
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2015102606402, MigrateDataBase.CentralBank)]
    public class Migration_2015102606402_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            // TODO: Upgrade CentralPref_Kernel

            SetSchema(Bank.Data);
            // TODO: Upgrade CentralPref_Data

            SetSchema(Bank.Upload);
            // TODO: Upgrade CentralPref_Upload

            Database.ExecuteNonQuery(
                "drop table if exists temp_t"
                );
            Database.ExecuteNonQuery(
                " SELECT DISTINCT table_schema as schema, table_name as name " +
                " INTO TEMP temp_t " +
                " FROM information_schema.columns " +
                " WHERE table_schema ilike '%charge___' " +
                " AND table_name ilike 'calc_gku___' " +
                " ORDER BY table_schema"
                );
            
            IDataReader reader;
            reader = Database.ExecuteReader(
                " select 'UPDATE '||\"schema\"||'.'||\"name\"||' SET dat_s = ''20'||SUBSTRING(\"schema\" from 15 for 2)||'.'||SUBSTRING(\"name\" from 10 for 2)||'.01'', dat_po = ''' " +
                " ||(date_trunc('month', ('20'||SUBSTRING(\"schema\" from 15 for 2)||'.'||SUBSTRING(\"name\" from 10 for 2)||'.01')::date) + INTERVAL '1 month - 1 DAY')::date||''' where dat_s is null;' as col from temp_t"
                );
            while (reader.Read())
            {
                string sql = reader["col"].ToString();
                Database.ExecuteNonQuery(sql);
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
