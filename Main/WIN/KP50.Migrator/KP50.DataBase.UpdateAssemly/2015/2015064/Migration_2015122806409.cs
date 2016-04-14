using System;
using System.Collections.Generic;
using System.Data;
using KP50.DataBase.Migrator;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015064
{
    [Migration(2015122806409, MigrateDataBase.CentralBank)]
    public class Migration_2015122806409 : Migration
    {
        public override void Apply()
        {

            if (Database.ProviderName == "PostgreSQL")
            {
                Database.CommandTimeout = 6000;


                var central_peni_provodki = new SchemaQualifiedObjectName { Name = "peni_provodki", Schema = CentralData };
                var central_peni_provodki_arch = new SchemaQualifiedObjectName { Name = "peni_provodki_arch", Schema = CentralData };
                var central_peni_debt = new SchemaQualifiedObjectName { Name = "peni_debt", Schema = CentralData };
                var central_peni_calc = new SchemaQualifiedObjectName { Name = "peni_calc", Schema = CentralData };
                if (Database.TableExists(central_peni_calc)
                    && Database.TableExists(central_peni_provodki)
                    && Database.TableExists(central_peni_provodki_arch)
                    && Database.TableExists(central_peni_debt))
                {
                    using (var reader = Database.ExecuteReader("SELECT trim(bd_kernel) pref" +
                                                               " FROM " + CentralKernel + Database.TableDelimiter + "s_point" +
                                                               " WHERE nzp_wp>1 AND trim(bd_kernel)<>'" + CurrentPrefix+"'"))
                    {
                        while (reader.Read())
                        {
                            var pref = reader["pref"].ToString().Trim();
                            var local_peni_provodki = new SchemaQualifiedObjectName { Name = "peni_provodki", Schema = pref +"_data" };
                            var local_peni_debt = new SchemaQualifiedObjectName { Name = "peni_debt", Schema = pref + "_data" };
                            var local_peni_calc = new SchemaQualifiedObjectName { Name = "peni_calc", Schema = pref + "_data" };
                            if (Database.TableExists(local_peni_provodki)
                                && Database.TableExists(local_peni_debt)
                                && Database.TableExists(local_peni_calc))
                            {
                                var res = Verificate(central_peni_provodki, local_peni_provodki, pref);
                                if (!res)
                                {
                                    throw new Exception("Кол-во записей в исходной таблица и конечной таблице не совпадает: Таблица " +
                                       GetFullTableName(central_peni_provodki));
                                }
                                res = Verificate(central_peni_debt, local_peni_debt, pref);
                                if (!res)
                                {
                                    throw new Exception("Кол-во записей в исходной таблица и конечной таблице не совпадает: Таблица " +
                                       GetFullTableName(central_peni_debt));
                                }
                                res = Verificate(central_peni_calc, local_peni_calc, pref);
                                if (!res)
                                {
                                    throw new Exception("Кол-во записей в исходной таблица и конечной таблице не совпадает: Таблица " +
                                       GetFullTableName(central_peni_calc));
                                }
                            }
                        }
                      
                    }

                    //дропаем все старые мастер таблицы с партициями
                    DropTableCascadeFromData(central_peni_provodki);
                    DropTableCascadeFromData(central_peni_provodki_arch); //их не копировал, потому что толку от них нет
                    DropTableCascadeFromData(central_peni_debt);
                    DropTableCascadeFromData(central_peni_calc);
                }
            }
        }


        private bool Verificate(SchemaQualifiedObjectName central, SchemaQualifiedObjectName local, string pref)
        {
            var nzp_wp = (int)Database.ExecuteScalar("SELECT nzp_wp FROM " + CentralKernel + Database.TableDelimiter +
                                                  "s_point WHERE bd_kernel='" + pref + "'");

            var countCentral = (long)Database.ExecuteScalar(" SELECT COUNT(1) FROM " + GetFullTableName(central) +
                                                        " WHERE nzp_wp=" + nzp_wp);

            var countLocal = (long)Database.ExecuteScalar(" SELECT COUNT(1) FROM " + GetFullTableName(local) +
                                                        " WHERE nzp_wp=" + nzp_wp);

            if (countLocal != countCentral)
            {
                return false;
            }
            return true;
        }

        private string GetFullTableName(SchemaQualifiedObjectName table)
        {
            return string.Format("{0}{1}{2}", table.Schema, Database.TableDelimiter, table.Name);
        }

        private void DropTableCascadeFromData(SchemaQualifiedObjectName table)
        {
            Database.ExecuteNonQuery("DROP TABLE IF EXISTS " + GetFullTableName(table) + " CASCADE");
        }
    }

}
