using System;
using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2014._2014124
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2015012301101, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_2015012301101_CentralBank : Migration
    {
        public override void Apply()
        {
            if (Database.ProviderName != "PostgreSQL") return;
            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName formuls_opis = new SchemaQualifiedObjectName()
            {
                Name = "formuls_opis",
                Schema = CurrentSchema
            };

            if (Database.TableExists(formuls_opis))
            {
                SchemaQualifiedObjectName formuls = new SchemaQualifiedObjectName()
                {
                    Name = "formuls",
                    Schema = CurrentSchema
                };
                SchemaQualifiedObjectName prm_tarifs = new SchemaQualifiedObjectName()
                {
                    Name = "prm_tarifs",
                    Schema = CurrentSchema
                };
                if (Database.TableExists(formuls))
                {
                    var count =
                        Convert.ToInt32(
                            Database.ExecuteScalar("SELECT count(*) FROM " + formuls.Schema +
                                                   Database.TableDelimiter +
                                                   formuls.Name + " WHERE nzp_frm=610"));
                    if (count > 0)
                    {
                        //двигаем последовательность до актуальных значений
                        Database.ExecuteNonQuery("SELECT setval('" + CurrentSchema + Database.TableDelimiter +
                                                 "formuls_opis_nzp_ops_seq'," +
                                                 " (SELECT max(nzp_ops)+1 FROM " + CurrentSchema +
                                                 Database.TableDelimiter + "formuls_opis))");
                        Database.Delete(prm_tarifs, " nzp_frm=610");
                        Database.Delete(formuls_opis, " nzp_frm=610");
                        Database.Insert(formuls_opis,
                            new[]
                            {
                                "nzp_frm", "nzp_frm_kod", "nzp_frm_typ", "nzp_prm_tarif_ls",
                                "nzp_prm_tarif_lsp", "nzp_prm_tarif_dm", "nzp_prm_tarif_su",
                                "nzp_prm_tarif_bd", "nzp_frm_typrs", "nzp_prm_rash", "nzp_prm_rash1",
                                "nzp_prm_rash2", "dat_s", "dat_po"
                            },
                            new[]
                            {"610", "610", "2", "342", "0", "0", "337", "124", "610", "0", "0", "0", null, null});


                        SchemaQualifiedObjectName services = new SchemaQualifiedObjectName()
                        {
                            Name = "services",
                            Schema = CurrentSchema
                        };
                        var count1 =
                            Convert.ToInt32(
                                Database.ExecuteScalar("SELECT count(*) FROM " + services.Schema +
                                                       Database.TableDelimiter +
                                                       services.Name + " WHERE nzp_serv=202"));
                        if (count1 > 0)
                        {
                           
                            if (Database.TableExists(prm_tarifs))
                            {
                                Database.Delete(prm_tarifs, " nzp_frm=610");

                                Database.Insert(prm_tarifs,
                                    new[] {"nzp_serv", "nzp_frm", "nzp_prm", "is_edit", "nzp_user", "dat_when"},
                                    new[] {"202", "610", "347", "1", "-1000", DateTime.Now.ToShortDateString()});
                                Database.Insert(prm_tarifs,
                                    new[] {"nzp_serv", "nzp_frm", "nzp_prm", "is_edit", "nzp_user", "dat_when"},
                                    new[] {"202", "610", "348", "1", "-1000", DateTime.Now.ToShortDateString()});
                                Database.Insert(prm_tarifs,
                                    new[] {"nzp_serv", "nzp_frm", "nzp_prm", "is_edit", "nzp_user", "dat_when"},
                                    new[] {"202", "610", "349", "1", "-1000", DateTime.Now.ToShortDateString()});
                                Database.Insert(prm_tarifs,
                                    new[] {"nzp_serv", "nzp_frm", "nzp_prm", "is_edit", "nzp_user", "dat_when"},
                                    new[] {"202", "610", "350", "1", "-1000", DateTime.Now.ToShortDateString()});
                                Database.Insert(prm_tarifs,
                                    new[] {"nzp_serv", "nzp_frm", "nzp_prm", "is_edit", "nzp_user", "dat_when"},
                                    new[] {"202", "610", "351", "1", "-1000", DateTime.Now.ToShortDateString()});
                                Database.Insert(prm_tarifs,
                                    new[] {"nzp_serv", "nzp_frm", "nzp_prm", "is_edit", "nzp_user", "dat_when"},
                                    new[] {"202", "610", "352", "1", "-1000", DateTime.Now.ToShortDateString()});
                            }
                        }
                    }
                }


            }
        }

        public override void Revert()
        {
        }
    }


}
