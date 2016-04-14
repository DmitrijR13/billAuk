using System;
using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2014._2014122
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2014121012201, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_2014121012201_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            // TODO: Upgrade CentralPref_Kernel

            SetSchema(Bank.Data);

            int month = Convert.ToInt32(Database.ExecuteScalar(
                Database.FormatSql("SELECT EXTRACT(month FROM now()) ")));
            int year = Convert.ToInt32(Database.ExecuteScalar(
                Database.FormatSql("SELECT EXTRACT(year FROM now()) ")));
            string thisMonth = "01." + month.ToString("00") + "." + year;

            SchemaQualifiedObjectName prm_10 = new SchemaQualifiedObjectName() { Name = "prm_10", Schema = CurrentSchema };
            if (Database.TableExists(prm_10))
            {
                int count = Convert.ToInt32(Database.ExecuteScalar(
                   Database.FormatSql("SELECT * FROM {0:NAME} WHERE nzp_prm = 2081 AND is_actual <> 100 AND " +
                                      " dat_s <= '" + thisMonth + "' AND dat_po >= '" + thisMonth + "' ", prm_10)));
                if (count == 0)
                {
                    Database.ExecuteNonQuery(
                        Database.FormatSql("UPDATE {0:NAME} SET is_actual = 100 WHERE nzp_prm = 2081 AND dat_s > '" + thisMonth + "' ", prm_10));
                    Database.Insert(prm_10, 
                        new string[] { "nzp_prm", "dat_s", "dat_po", "val_prm", "is_actual", "nzp_user", "dat_when", "user_del", "month_calc" },
                        new string[] { "2081", thisMonth, "3000-01-01", "2000", "1", "1", "now()", "1", "2014-12-01" });
                }

                count = Convert.ToInt32(Database.ExecuteScalar(
                   Database.FormatSql("SELECT * FROM {0:NAME} WHERE nzp_prm = 2082 AND is_actual <> 100 AND " +
                                      " dat_s <= '" + thisMonth + "' AND dat_po >= '" + thisMonth + "' ", prm_10)));
                if (count == 0)
                {
                    Database.ExecuteNonQuery(
                        Database.FormatSql("UPDATE {0:NAME} SET is_actual = 100 WHERE nzp_prm = 2082 AND dat_s > '" + thisMonth + "' ", prm_10));
                    Database.Insert(prm_10,
                        new string[] { "nzp_prm", "dat_s", "dat_po", "val_prm", "is_actual", "nzp_user", "dat_when", "user_del", "month_calc" },
                        new string[] { "2082", thisMonth, "3000-01-01", "70", "1", "1", "now()", "1", "2014-12-01" });
                }

                count = Convert.ToInt32(Database.ExecuteScalar(
                   Database.FormatSql("SELECT * FROM {0:NAME} WHERE nzp_prm = 2083 AND is_actual <> 100 AND " +
                                      " dat_s <= '" + thisMonth + "' AND dat_po >= '" + thisMonth + "' ", prm_10)));
                if (count == 0)
                {
                    Database.ExecuteNonQuery(
                        Database.FormatSql("UPDATE {0:NAME} SET is_actual = 100 WHERE nzp_prm = 2083 AND dat_s > '" + thisMonth + "' ", prm_10));
                    Database.Insert(prm_10,
                        new string[] { "nzp_prm", "dat_s", "dat_po", "val_prm", "is_actual", "nzp_user", "dat_when", "user_del", "month_calc" },
                        new string[] { "2083", thisMonth, "3000-01-01", "70", "1", "1", "now()", "1", "2014-12-01" });
                }

                count = Convert.ToInt32(Database.ExecuteScalar(
                   Database.FormatSql("SELECT * FROM {0:NAME} WHERE nzp_prm = 2084 AND is_actual <> 100 AND " +
                                      " dat_s <= '" + thisMonth + "' AND dat_po >= '" + thisMonth + "' ", prm_10)));
                if (count == 0)
                {
                    Database.ExecuteNonQuery(
                        Database.FormatSql("UPDATE {0:NAME} SET is_actual = 100 WHERE nzp_prm = 2084 AND dat_s > '" + thisMonth + "' ", prm_10));
                    Database.Insert(prm_10,
                        new string[] { "nzp_prm", "dat_s", "dat_po", "val_prm", "is_actual", "nzp_user", "dat_when", "user_del", "month_calc" },
                        new string[] { "2084", thisMonth, "3000-01-01", "3000", "1", "1", "now()", "1", "2014-12-01" });
                }

                count = Convert.ToInt32(Database.ExecuteScalar(
                   Database.FormatSql("SELECT * FROM {0:NAME} WHERE nzp_prm = 2085 AND is_actual <> 100 AND " +
                                      " dat_s <= '" + thisMonth + "' AND dat_po >= '" + thisMonth + "' ", prm_10)));
                if (count == 0)
                {
                    Database.ExecuteNonQuery(
                        Database.FormatSql("UPDATE {0:NAME} SET is_actual = 100 WHERE nzp_prm = 2085 AND dat_s > '" + thisMonth + "' ", prm_10));
                    Database.Insert(prm_10,
                        new string[] { "nzp_prm", "dat_s", "dat_po", "val_prm", "is_actual", "nzp_user", "dat_when", "user_del", "month_calc" },
                        new string[] { "2085", thisMonth, "3000-01-01", "1000000", "1", "1", "now()", "1", "2014-12-01" });
                }

                count = Convert.ToInt32(Database.ExecuteScalar(
                   Database.FormatSql("SELECT * FROM {0:NAME} WHERE nzp_prm = 2086 AND is_actual <> 100 AND " +
                                      " dat_s <= '" + thisMonth + "' AND dat_po >= '" + thisMonth + "' ", prm_10)));
                if (count == 0)
                {
                    Database.ExecuteNonQuery(
                        Database.FormatSql("UPDATE {0:NAME} SET is_actual = 100 WHERE nzp_prm = 2086 AND dat_s > '" + thisMonth + "' ", prm_10));
                    Database.Insert(prm_10,
                        new string[] { "nzp_prm", "dat_s", "dat_po", "val_prm", "is_actual", "nzp_user", "dat_when", "user_del", "month_calc" },
                        new string[] { "2086", thisMonth, "3000-01-01", "70000", "1", "1", "now()", "1", "2014-12-01" });
                }

                count = Convert.ToInt32(Database.ExecuteScalar(
                   Database.FormatSql("SELECT * FROM {0:NAME} WHERE nzp_prm = 2087 AND is_actual <> 100 AND " +
                                      " dat_s <= '" + thisMonth + "' AND dat_po >= '" + thisMonth + "' ", prm_10)));
                if (count == 0)
                {
                    Database.ExecuteNonQuery(
                        Database.FormatSql("UPDATE {0:NAME} SET is_actual = 100 WHERE nzp_prm = 2087 AND dat_s > '" + thisMonth + "' ", prm_10));
                    Database.Insert(prm_10,
                        new string[] { "nzp_prm", "dat_s", "dat_po", "val_prm", "is_actual", "nzp_user", "dat_when", "user_del", "month_calc" },
                        new string[] { "2087", thisMonth, "3000-01-01", "70000", "1", "1", "now()", "1", "2014-12-01" });
                }

                count = Convert.ToInt32(Database.ExecuteScalar(
                   Database.FormatSql("SELECT * FROM {0:NAME} WHERE nzp_prm = 2088 AND is_actual <> 100 AND " +
                                      " dat_s <= '" + thisMonth + "' AND dat_po >= '" + thisMonth + "' ", prm_10)));
                if (count == 0)
                {
                    Database.ExecuteNonQuery(
                        Database.FormatSql("UPDATE {0:NAME} SET is_actual = 100 WHERE nzp_prm = 2088 AND dat_s > '" + thisMonth + "' ", prm_10));
                    Database.Insert(prm_10,
                        new string[] { "nzp_prm", "dat_s", "dat_po", "val_prm", "is_actual", "nzp_user", "dat_when", "user_del", "month_calc" },
                        new string[] { "2088", thisMonth, "3000-01-01", "1000000", "1", "1", "now()", "1", "2014-12-01" });
                }
            }

            SetSchema(Bank.Upload);
            // TODO: Upgrade CentralPref_Upload
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
