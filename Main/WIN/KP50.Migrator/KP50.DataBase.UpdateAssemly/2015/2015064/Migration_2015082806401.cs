using System;
using System.Collections.Generic;
using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015064
{

    [Migration(2015082806401, MigrateDataBase.Fin)]
    public class Migration_2015082806401_Fin : Migration
    {
        public override void Apply()
        {
            SchemaQualifiedObjectName pack_ls = new SchemaQualifiedObjectName() { Name = "pack_ls", Schema = CurrentSchema }; 
            if (Database.TableExists(pack_ls))
            {
                if (!Database.ColumnExists(pack_ls, "changed_by"))
                    Database.AddColumn(pack_ls, new Column("changed_by", DbType.Int32));
                if (!Database.ColumnExists(pack_ls, "changed_on"))
                    Database.AddColumn(pack_ls, new Column("changed_on", DbType.DateTime));
            }
            IDataReader reader =
                Database.ExecuteReader("SELECT trim(bd_kernel) as bd_kernel FROM " + CentralKernel +
                                       Database.TableDelimiter + "s_point "); ;

            try
            {
                while (reader.Read())
                {
                    string pref = reader["bd_kernel"].ToString();
                    if (Database.TableExists(pack_ls))
                    {
                        var year = 2000 + Convert.ToInt32(pack_ls.Schema.Substring(pack_ls.Schema.Length - 2, 2));
                        if (year != 2015 && year != 2014) return;

                        string pl = pack_ls.Schema + Database.TableDelimiter + pack_ls.Name;
                        for (int i = 1; i <= 12; i++)
                        {
                            var month = i.ToString("00");
                            object obj =
                                Database.ExecuteScalar(
                                "SELECT count(*) FROM information_schema.tables" +
                                " WHERE table_schema = '" + pref + "_charge_" + year.ToString().Substring(2, 2) + "'" +
                                " AND table_name = 'fn_supplier" + month + "'");
                            var count = Convert.ToInt32(obj);
                            //если такой таблицы нет, то не вытаскиваем
                            if (count == 0) continue;
                            Database.ExecuteNonQuery(
                                " INSERT INTO " + pack_ls.Schema + Database.TableDelimiter + "pack_log" +
                                " (nzp_pack, nzp_pack_ls, dat_log, txt_log)" +
                                " SELECT pl.nzp_pack, pl.nzp_pack_ls, now(), 'Убрали из корзины через миграцию'" +
                                " FROM " + pl + " pl, " + pref + "_data" + Database.TableDelimiter + "kvar k" +
                                " WHERE pl.inbasket = 1 AND pl.num_ls = k.num_ls " +
                                " AND EXISTS" +
                                " (SELECT 1 FROM " + pref + "_charge_" + year.ToString().Substring(2, 2) + ".fn_supplier" + month + " f" +
                                "  WHERE pl.nzp_pack_ls = f.nzp_pack_ls)");
                            Database.ExecuteNonQuery(
                                " UPDATE " + pl +
                                " SET inbasket = 0, changed_on = now(), " +
                                " alg = (CASE WHEN coalesce(alg,'0') = '0' THEN '1' ELSE alg END)," +
                                " dat_uchet = (CASE WHEN dat_uchet is null THEN " +
                                    " (SELECT DISTINCT dat_uchet " +
                                    " FROM " + pref + "_charge_" + year.ToString().Substring(2, 2) + ".fn_supplier" + month + " f" +
                                    " WHERE  " + pl + ".nzp_pack_ls = f.nzp_pack_ls) ELSE dat_uchet END)" +
                                " WHERE inbasket = 1 " +
                                " AND EXISTS" +
                                " (SELECT 1 FROM " + pref + "_charge_" + year.ToString().Substring(2, 2) + ".fn_supplier" + month + " f" +
                                "  WHERE  " + pl + ".nzp_pack_ls = f.nzp_pack_ls)" +
                                " AND EXISTS " +
                                " (SELECT 1 FROM " + pref + "_data" + Database.TableDelimiter + "kvar k" +
                                "  WHERE  " + pl + ".num_ls = k.num_ls)");
                        }

                        object obj1 =
                            Database.ExecuteScalar(
                            "SELECT count(*) FROM information_schema.tables" +
                            " WHERE table_schema = '" + pref + "_charge_" + year.ToString().Substring(2, 2) + "'" +
                            " AND table_name = 'from_supplier'");
                        var count1 = Convert.ToInt32(obj1);
                        if (count1 == 0) continue;
                        Database.ExecuteNonQuery(
                            " INSERT INTO " + pack_ls.Schema + Database.TableDelimiter + "pack_log" +
                            " (nzp_pack, nzp_pack_ls, dat_log, txt_log)" +
                            " SELECT pl.nzp_pack, pl.nzp_pack_ls, now(), 'Убрали из корзины через миграцию'" +
                            " FROM " + pl + " pl, " + pref + "_data" + Database.TableDelimiter + "kvar k" +
                            " WHERE pl.inbasket = 1 AND pl.num_ls = k.num_ls AND EXISTS" +
                            " (SELECT 1 FROM " + pref + "_charge_" + year.ToString().Substring(2, 2) + ".from_supplier" + " f " +
                            "  WHERE pl.nzp_pack_ls = f.nzp_pack_ls)");
                        Database.ExecuteNonQuery(
                            " UPDATE " + pl +
                            " SET inbasket = 0, changed_on = now(), " +
                            " alg = (CASE WHEN coalesce(alg,'0') = '0' THEN '1' ELSE alg END)," +
                            " dat_uchet = (CASE WHEN dat_uchet is null THEN " +
                                " (SELECT DISTINCT dat_uchet " +
                                " FROM " + pref + "_charge_" + year.ToString().Substring(2, 2) + ".from_supplier" + " f" +
                                " WHERE  " + pl + ".nzp_pack_ls = f.nzp_pack_ls) ELSE dat_uchet END)" +
                            " WHERE inbasket = 1 AND EXISTS" +
                            " (SELECT 1 FROM " + pref + "_charge_" + year.ToString().Substring(2, 2) + ".from_supplier" + " f " +
                            "  WHERE " + pl + ".nzp_pack_ls = f.nzp_pack_ls)" +
                            " AND EXISTS " +
                                " (SELECT 1 FROM " + pref + "_data" + Database.TableDelimiter + "kvar k" +
                                "  WHERE  " + pl + ".num_ls = k.num_ls)");

                    }
                }

            }
            finally
            {
                reader.Close();
            }

        }

        public override void Revert()
        {
            // TODO: Downgrade Fins

        }
    }

}
