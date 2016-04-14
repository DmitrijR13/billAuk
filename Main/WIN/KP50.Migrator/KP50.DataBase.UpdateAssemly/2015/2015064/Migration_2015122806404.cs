using System;
using System.Collections.Generic;
using System.Data;
using KP50.DataBase.Migrator;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015064
{
    [Migration(2015122806404, MigrateDataBase.LocalBank)]
    public class Migration_2015122806404 : Migration
    {
        public override void Apply()
        {
            if (Database.ProviderName == "PostgreSQL")
            {
                Database.CommandTimeout = 6000;

                SetSchema(Bank.Data);

                var peni_provodki = new SchemaQualifiedObjectName { Name = "peni_provodki", Schema = CentralData };
                var peni_debt = new SchemaQualifiedObjectName { Name = "peni_debt", Schema = CentralData };

                var peni_debt_refs = new SchemaQualifiedObjectName { Name = "peni_debt_refs", Schema = CurrentSchema };
                var peni_provodki_refs = new SchemaQualifiedObjectName { Name = "peni_provodki_refs", Schema = CurrentSchema };
                var nzp_wp = (int)Database.ExecuteScalar("SELECT nzp_wp FROM " + CentralKernel + Database.TableDelimiter +
                                                   "s_point WHERE bd_kernel='" + CurrentPrefix + "'");

                #region выполняем перенос связей

                if (Database.TableExists(peni_provodki_refs) && Database.ColumnExists(peni_provodki, "peni_debt_id"))
                {

                    var tablePath = GetFullTableName(peni_provodki);



                    if ((bool)Database.ExecuteScalar("SELECT COUNT(1)>0 FROM " + tablePath +
                        " WHERE peni_debt_id IS NOT NULL AND nzp_wp=" + nzp_wp + ""))
                    {
                        var max = (int)Database.ExecuteScalar("SELECT COALESCE(MAX(id),0) FROM " + tablePath +
                            " WHERE peni_debt_id IS NOT NULL AND nzp_wp=" + nzp_wp + "");


                        var _5million = 5000000;
                        for (var i = 0; i < max; i += _5million)
                        {

                            var num = i / _5million;
                            var toTable = CurrentSchema + Database.TableDelimiter + "peni_provodki_refs_" + num + "_" +
                                          nzp_wp;

                            if(!(bool)Database.ExecuteScalar("SELECT COUNT(1)>0 FROM " + tablePath +
                                " WHERE nzp_wp=" + nzp_wp + " AND peni_debt_id IS NOT NULL" +
                                " AND id>=" + i + " AND id<" + (i + _5million) + ""))
                            {
                                continue;
                            }

                            //триггер создает нужную партицию
                            Database.ExecuteNonQuery(
                                " INSERT INTO " + GetFullTableName(peni_provodki_refs) +
                                " (nzp_wp, peni_debt_id, peni_provodki_id, date_obligation, date_calc, nzp_kvar)" +
                                " SELECT nzp_wp, peni_debt_id, id, date_obligation, '01.01.1900'::DATE, nzp_kvar FROM " +
                                tablePath +
                                " WHERE nzp_wp=" + nzp_wp + " AND peni_debt_id IS NOT NULL" +
                                " AND id>=" + i + " AND id<" + (i + _5million) + " LIMIT 1");

                            Database.ExecuteNonQuery("DELETE FROM " + toTable);
                            //пишем в созданную партицию
                            Database.ExecuteNonQuery(
                                " INSERT INTO " + toTable +
                                " (nzp_wp, peni_debt_id, peni_provodki_id, date_obligation, date_calc, nzp_kvar)" +
                                " SELECT p.nzp_wp, p.peni_debt_id, p.id, p.date_obligation, d.date_calc, p.nzp_kvar" +
                                " FROM " + tablePath + " p, " + GetFullTableName(peni_debt) + " d" +
                                " WHERE p.nzp_wp=" + nzp_wp +
                                " AND p.peni_debt_id IS NOT NULL AND p.s_prov_types_id>0" +
                                " AND p.id>=" + i + " AND p.id<" + (i + _5million) +
                                " AND p.peni_debt_id=d.id AND p.nzp_wp=d.nzp_wp");

                            Database.ExecuteNonQuery("ANALYZE " + toTable);
                        }
                    }
                }

                #region выполняем перенос связей

                if (Database.TableExists(peni_debt_refs) && Database.ColumnExists(peni_debt, "peni_calc_id"))
                {
                    var tablePath = GetFullTableName(peni_debt);

                    using (var reader = Database.ExecuteReader(" SELECT DISTINCT date_calc FROM  " + tablePath +
                        " WHERE nzp_wp=" + nzp_wp + " AND peni_calc_id IS NOT NULL AND s_peni_type_debt_id=1 "))
                    {
                        while (reader.Read())
                        {
                            var date_calc = Convert.ToDateTime(reader["date_calc"]);
                            var toTable = CurrentPrefix + "_charge_" + (date_calc.Year - 2000).ToString("00") +
                                          Database.TableDelimiter + "peni_debt_refs_" + date_calc.Year +
                                          date_calc.Month.ToString("00") + "_" + nzp_wp;
                            
                            Database.ExecuteNonQuery(
                              " INSERT INTO " + GetFullTableName(peni_debt_refs)+
                              " (nzp_wp, peni_calc_id, peni_debt_id, date_calc)" +
                              " VALUES  ("+nzp_wp+", 0, 0, '"+date_calc.ToShortDateString()+"')");

                            Database.ExecuteNonQuery("DELETE FROM " + toTable + " WHERE peni_calc_id=0 AND peni_debt_id=0");

                            //пишем в созданную партицию
                            Database.ExecuteNonQuery(
                                " INSERT INTO " + toTable + " (nzp_wp, peni_calc_id, peni_debt_id, date_calc)" +
                                " SELECT  nzp_wp, peni_calc_id, id, date_calc FROM " + tablePath +
                                " WHERE nzp_wp=" + nzp_wp +
                                " AND peni_calc_id IS NOT NULL" +
                                " AND s_peni_type_debt_id=1 " +
                                " AND date_calc='"+date_calc.ToShortDateString()+"'");

                            Database.ExecuteNonQuery(" ANALYZE " + toTable);
                        }

                    }
                }

                #endregion выполняем перенос связей

                if (Database.TableExists(peni_provodki_refs) && Database.ColumnExists(peni_provodki, "peni_debt_id"))
                {
                    if (Database.TableExists(peni_debt_refs) && Database.ColumnExists(peni_debt, "peni_calc_id"))
                    {
                        //проверка на совпадение кол-ва записей
                        var equalCount1 = (bool)Database.ExecuteScalar("SELECT " +
                                                                        "(SELECT COUNT(1) " +
                                                                       " FROM " + GetFullTableName(peni_provodki) + " p,  " + GetFullTableName(peni_debt) + " d" +
                                                                        " WHERE p.peni_debt_id=d.id AND p.s_prov_types_id>0 " +
                                                                       "  AND p.nzp_wp=" + nzp_wp + " AND p.nzp_wp=d.nzp_wp" +
                                                                       "  AND d.s_peni_type_debt_id=1) " +
                                                                        " = " +
                                                                        "(SELECT COUNT(1) FROM " + CurrentPrefix + "_data" +
                                                                        Database.TableDelimiter + "peni_provodki_refs) ");

                        var equalCount2 = (bool)Database.ExecuteScalar("SELECT " +
                                                                        "(SELECT COUNT(1) FROM " + GetFullTableName(peni_debt) +
                                                                        " WHERE nzp_wp = " + nzp_wp + 
                                                                        " AND s_peni_type_debt_id=1 " +
                                                                        " AND peni_calc_id IS NOT NULL) " +
                                                                        " = " +
                                                                        "(SELECT COUNT(1) FROM " + GetFullTableName(peni_debt_refs) + ")");

                        //не совпадают
                        if (!(equalCount1 && equalCount2))
                        {
                            throw new Exception("Кол-во записей в исходной и целевой таблицах не совпадает!");
                        }
                    }
                }
            }

                #endregion выполняем перенос связей
        }


        private string GetFullTableName(SchemaQualifiedObjectName table)
        {
            return string.Format("{0}{1}{2}", table.Schema, Database.TableDelimiter, table.Name);
        }

    }
}
