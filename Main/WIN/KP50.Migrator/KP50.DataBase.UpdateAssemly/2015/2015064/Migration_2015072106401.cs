using System;
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
    [Migration(2015072106401, MigrateDataBase.CentralBank)]
    public class Migration_2015072106401_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);
            SchemaQualifiedObjectName prm_1 = new SchemaQualifiedObjectName() { Name = "prm_1", Schema = CurrentSchema };
            if (Database.TableExists(prm_1))
            {
                Database.Delete(prm_1, "nzp_prm = 1116");
                Database.Delete(prm_1, "nzp_prm = 1117");
                Database.Delete(prm_1, "nzp_prm = 1118");
                Database.Delete(prm_1, "nzp_prm = 1119");
                Database.Delete(prm_1, "nzp_prm = 1120");
                Database.Delete(prm_1, "nzp_prm = 1121");

            }

            SchemaQualifiedObjectName prm_5 = new SchemaQualifiedObjectName() { Name = "prm_5", Schema = CurrentSchema };
            if (Database.TableExists(prm_5))
            {
                Database.Delete(prm_5, "nzp_prm = 1122");
            }

            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName formuls = new SchemaQualifiedObjectName() { Name = "formuls", Schema = CurrentSchema };
            if (Database.TableExists(formuls))
            {

                object obj =
                   Database.ExecuteScalar("SELECT count(*) FROM " + formuls.Schema + Database.TableDelimiter +
                                          formuls.Name +
                                          " WHERE nzp_frm = 1992;");
                var count = Convert.ToInt32(obj);

                if (count == 0)
                {
                    Database.Insert(formuls, new string[] { "nzp_frm", "name_frm", "nzp_measure", "is_device" }, new string[] { "1992", "Рассрочка-ХВС платежа по Пост.№354", "6", "0" });
                }

                object obj1 =
   Database.ExecuteScalar("SELECT count(*) FROM " + formuls.Schema + Database.TableDelimiter +
                          formuls.Name +
                          " WHERE nzp_frm = 1993;");
                var count1 = Convert.ToInt32(obj1);

                if (count1 == 0)
                {
                    Database.Insert(formuls, new string[] { "nzp_frm", "name_frm", "nzp_measure", "is_device" }, new string[] { "1993", "Рассрочка-КАН платежа по Пост.№354", "6", "0" });
                }

                object obj2 =
   Database.ExecuteScalar("SELECT count(*) FROM " + formuls.Schema + Database.TableDelimiter +
                          formuls.Name +
                          " WHERE nzp_frm = 1994;");
                var count2 = Convert.ToInt32(obj2);

                if (count2 == 0)
                {
                    Database.Insert(formuls, new string[] { "nzp_frm", "name_frm", "nzp_measure", "is_device" }, new string[] { "1994", "Рассрочка-ОТП платежа по Пост.№354", "6", "0" });
                }

                object obj3 =
   Database.ExecuteScalar("SELECT count(*) FROM " + formuls.Schema + Database.TableDelimiter +
                          formuls.Name +
                          " WHERE nzp_frm = 1995;");
                var count3 = Convert.ToInt32(obj3);

                if (count3 == 0)
                {
                    Database.Insert(formuls, new string[] { "nzp_frm", "name_frm", "nzp_measure", "is_device" }, new string[] { "1995", "Рассрочка-ГВС платежа по Пост.№354", "6", "0" });
                }

                object obj4 =
   Database.ExecuteScalar("SELECT count(*) FROM " + formuls.Schema + Database.TableDelimiter +
                          formuls.Name +
                          " WHERE nzp_frm = 1996;");
                var count4 = Convert.ToInt32(obj4);

                if (count4 == 0)
                {
                    Database.Insert(formuls, new string[] { "nzp_frm", "name_frm", "nzp_measure", "is_device" }, new string[] { "1996", "Рассрочка-ХГВ платежа по Пост.№354", "6", "0" });
                }

                object obj5 =
   Database.ExecuteScalar("SELECT count(*) FROM " + formuls.Schema + Database.TableDelimiter +
                          formuls.Name +
                          " WHERE nzp_frm = 1997;");
                var count5 = Convert.ToInt32(obj5);

                if (count5 == 0)
                {
                    Database.Insert(formuls, new string[] { "nzp_frm", "name_frm", "nzp_measure", "is_device" }, new string[] { "1997", "Рассрочка-ЭЛД платежа по Пост.№354", "6", "0" });
                }

                object obj6 =
   Database.ExecuteScalar("SELECT count(*) FROM " + formuls.Schema + Database.TableDelimiter +
                          formuls.Name +
                          " WHERE nzp_frm = 1998;");
                var count6 = Convert.ToInt32(obj6);

                if (count6 == 0)
                {
                    Database.Insert(formuls, new string[] { "nzp_frm", "name_frm", "nzp_measure", "is_device" }, new string[] { "1998", "Рассрочка-ЭЛН платежа по Пост.№354", "6", "0" });
                }

                object obj7 =
   Database.ExecuteScalar("SELECT count(*) FROM " + formuls.Schema + Database.TableDelimiter +
                          formuls.Name +
                          " WHERE nzp_frm = 1999;");
                var count7 = Convert.ToInt32(obj7);

                if (count7 == 0)
                {
                    Database.Insert(formuls, new string[] { "nzp_frm", "name_frm", "nzp_measure", "is_device" }, new string[] { "1999", "Рассрочка-ГАЗ платежа по Пост.№354", "6", "0" });
                }

            }

            SchemaQualifiedObjectName formuls_opis = new SchemaQualifiedObjectName() { Name = "formuls_opis", Schema = CurrentSchema };
            if (Database.TableExists(formuls))
            {
                Database.Delete(formuls_opis, "nzp_frm = 1992");
                Database.Delete(formuls_opis, "nzp_frm = 1993");
                Database.Delete(formuls_opis, "nzp_frm = 1994");
                Database.Delete(formuls_opis, "nzp_frm = 1995");
                Database.Delete(formuls_opis, "nzp_frm = 1996");
                Database.Delete(formuls_opis, "nzp_frm = 1997");
                Database.Delete(formuls_opis, "nzp_frm = 1998");
                Database.Delete(formuls_opis, "nzp_frm = 1999");

                Database.Insert(formuls_opis, new string[] { "nzp_frm", "nzp_frm_kod", "nzp_frm_typ", "nzp_prm_tarif_ls", "nzp_prm_tarif_lsp", "nzp_prm_tarif_dm", "nzp_prm_tarif_su", "nzp_prm_tarif_bd",
  "nzp_frm_typrs", "nzp_prm_rash", "nzp_prm_rash1" }, new string[] { "1992", "1992", "1999", "0", "0", "0", "0", "0", "4", "0", "0" });

                Database.Insert(formuls_opis, new string[] { "nzp_frm", "nzp_frm_kod", "nzp_frm_typ", "nzp_prm_tarif_ls", "nzp_prm_tarif_lsp", "nzp_prm_tarif_dm", "nzp_prm_tarif_su", "nzp_prm_tarif_bd",
  "nzp_frm_typrs", "nzp_prm_rash", "nzp_prm_rash1"}, new string[] { "1993", "1993", "1999", "0", "0", "0", "0", "0", "4", "0", "0" });

                Database.Insert(formuls_opis, new string[] { "nzp_frm", "nzp_frm_kod", "nzp_frm_typ", "nzp_prm_tarif_ls", "nzp_prm_tarif_lsp", "nzp_prm_tarif_dm", "nzp_prm_tarif_su", "nzp_prm_tarif_bd",
  "nzp_frm_typrs", "nzp_prm_rash", "nzp_prm_rash1"}, new string[] { "1994", "1994", "1999", "0", "0", "0", "0", "0", "4", "0", "0" });

                Database.Insert(formuls_opis, new string[] { "nzp_frm", "nzp_frm_kod", "nzp_frm_typ", "nzp_prm_tarif_ls", "nzp_prm_tarif_lsp", "nzp_prm_tarif_dm", "nzp_prm_tarif_su", "nzp_prm_tarif_bd",
  "nzp_frm_typrs", "nzp_prm_rash", "nzp_prm_rash1"}, new string[] { "1995", "1995", "1999", "0", "0", "0", "0", "0", "4", "0", "0" });

                Database.Insert(formuls_opis, new string[] { "nzp_frm", "nzp_frm_kod", "nzp_frm_typ", "nzp_prm_tarif_ls", "nzp_prm_tarif_lsp", "nzp_prm_tarif_dm", "nzp_prm_tarif_su", "nzp_prm_tarif_bd",
  "nzp_frm_typrs", "nzp_prm_rash", "nzp_prm_rash1"}, new string[] { "1996", "1996", "1999", "0", "0", "0", "0", "0", "4", "0", "0" });

                Database.Insert(formuls_opis, new string[] { "nzp_frm", "nzp_frm_kod", "nzp_frm_typ", "nzp_prm_tarif_ls", "nzp_prm_tarif_lsp", "nzp_prm_tarif_dm", "nzp_prm_tarif_su", "nzp_prm_tarif_bd",
  "nzp_frm_typrs", "nzp_prm_rash", "nzp_prm_rash1"}, new string[] { "1997", "1997", "1999", "0", "0", "0", "0", "0", "4", "0", "0" });

                Database.Insert(formuls_opis, new string[] { "nzp_frm", "nzp_frm_kod", "nzp_frm_typ", "nzp_prm_tarif_ls", "nzp_prm_tarif_lsp", "nzp_prm_tarif_dm", "nzp_prm_tarif_su", "nzp_prm_tarif_bd",
  "nzp_frm_typrs", "nzp_prm_rash", "nzp_prm_rash1"}, new string[] { "1998", "1998", "1999", "0", "0", "0", "0", "0", "4", "0", "0" });

                Database.Insert(formuls_opis, new string[] { "nzp_frm", "nzp_frm_kod", "nzp_frm_typ", "nzp_prm_tarif_ls", "nzp_prm_tarif_lsp", "nzp_prm_tarif_dm", "nzp_prm_tarif_su", "nzp_prm_tarif_bd",
  "nzp_frm_typrs", "nzp_prm_rash", "nzp_prm_rash1"}, new string[] { "1999", "1999", "1999", "0", "0", "0", "0", "0", "4", "0", "0" });
            }

            SchemaQualifiedObjectName prm_tarifs = new SchemaQualifiedObjectName() { Name = "prm_tarifs", Schema = CurrentSchema };
            if (Database.TableExists(prm_tarifs))
            {
                Database.Delete(prm_tarifs, "nzp_frm = 1992");
                Database.Delete(prm_tarifs, "nzp_frm = 1993");
                Database.Delete(prm_tarifs, "nzp_frm = 1994");
                Database.Delete(prm_tarifs, "nzp_frm = 1995");
                Database.Delete(prm_tarifs, "nzp_frm = 1996");
                Database.Delete(prm_tarifs, "nzp_frm = 1997");
                Database.Delete(prm_tarifs, "nzp_frm = 1998");
                Database.Delete(prm_tarifs, "nzp_frm = 1999");

                Database.Insert(prm_tarifs, new string[] { "nzp_serv", "nzp_frm", "nzp_prm", "is_edit", "nzp_user" }, new string[] { "306", "1992", "0", "1", "-1000" });
                Database.Insert(prm_tarifs, new string[] { "nzp_serv", "nzp_frm", "nzp_prm", "is_edit", "nzp_user" }, new string[] { "308", "1993", "0", "1", "-1000" });
                Database.Insert(prm_tarifs, new string[] { "nzp_serv", "nzp_frm", "nzp_prm", "is_edit", "nzp_user" }, new string[] { "309", "1994", "0", "1", "-1000" });
                Database.Insert(prm_tarifs, new string[] { "nzp_serv", "nzp_frm", "nzp_prm", "is_edit", "nzp_user" }, new string[] { "307", "1995", "0", "1", "-1000" });
                Database.Insert(prm_tarifs, new string[] { "nzp_serv", "nzp_frm", "nzp_prm", "is_edit", "nzp_user" }, new string[] { "307", "1996", "0", "1", "-1000" });
                Database.Insert(prm_tarifs, new string[] { "nzp_serv", "nzp_frm", "nzp_prm", "is_edit", "nzp_user" }, new string[] { "310", "1997", "0", "1", "-1000" });
                Database.Insert(prm_tarifs, new string[] { "nzp_serv", "nzp_frm", "nzp_prm", "is_edit", "nzp_user" }, new string[] { "310", "1998", "0", "1", "-1000" });
                Database.Insert(prm_tarifs, new string[] { "nzp_serv", "nzp_frm", "nzp_prm", "is_edit", "nzp_user" }, new string[] { "311", "1999", "0", "1", "-1000" });

            }

            SchemaQualifiedObjectName prm_name = new SchemaQualifiedObjectName() { Name = "prm_name", Schema = CurrentSchema };
            if (Database.TableExists(prm_name))
            {
                Database.Delete(prm_1, "nzp_prm = 1116");
                Database.Delete(prm_1, "nzp_prm = 1117");
                Database.Delete(prm_1, "nzp_prm = 1118");
                Database.Delete(prm_1, "nzp_prm = 1119");
                Database.Delete(prm_1, "nzp_prm = 1120");
                Database.Delete(prm_1, "nzp_prm = 1121");
                Database.Delete(prm_5, "nzp_prm = 1122");
            }






            SetSchema(Bank.Upload);
            // TODO: Upgrade CentralPref_Upload
        }

        public override void Revert()
        {
            SetSchema(Bank.Kernel);
            // TODO: Downgrade LocalPref_Kernel

            SetSchema(Bank.Data);
            // TODO: Downgrade LocalPref_Data

        }

    }

    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2015072106401, MigrateDataBase.LocalBank)]
    public class Migration_2015072106401_LocalBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);
            SchemaQualifiedObjectName prm_1 = new SchemaQualifiedObjectName() { Name = "prm_1", Schema = CurrentSchema };
            if (Database.TableExists(prm_1))
            {
                Database.Delete(prm_1, "nzp_prm = 1116");
                Database.Delete(prm_1, "nzp_prm = 1117");
                Database.Delete(prm_1, "nzp_prm = 1118");
                Database.Delete(prm_1, "nzp_prm = 1119");
                Database.Delete(prm_1, "nzp_prm = 1120");
                Database.Delete(prm_1, "nzp_prm = 1121");

            }

            SchemaQualifiedObjectName prm_5 = new SchemaQualifiedObjectName() { Name = "prm_5", Schema = CurrentSchema };
            if (Database.TableExists(prm_5))
            {
                Database.Delete(prm_5, "nzp_prm = 1122");
            }

            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName formuls = new SchemaQualifiedObjectName() { Name = "formuls", Schema = CurrentSchema };
            if (Database.TableExists(formuls))
            {
                object obj =
                   Database.ExecuteScalar("SELECT count(*) FROM " + formuls.Schema + Database.TableDelimiter +
                                          formuls.Name +
                                          " WHERE nzp_frm = 1992;");
                var count = Convert.ToInt32(obj);

                if (count == 0)
                {
                    Database.Insert(formuls, new string[] { "nzp_frm", "name_frm", "nzp_measure", "is_device" }, new string[] { "1992", "Рассрочка-ХВС платежа по Пост.№354", "6", "0" });
                }

                object obj1 =
   Database.ExecuteScalar("SELECT count(*) FROM " + formuls.Schema + Database.TableDelimiter +
                          formuls.Name +
                          " WHERE nzp_frm = 1993;");
                var count1 = Convert.ToInt32(obj1);

                if (count1 == 0)
                {
                    Database.Insert(formuls, new string[] { "nzp_frm", "name_frm", "nzp_measure", "is_device" }, new string[] { "1993", "Рассрочка-КАН платежа по Пост.№354", "6", "0" });
                }

                object obj2 =
   Database.ExecuteScalar("SELECT count(*) FROM " + formuls.Schema + Database.TableDelimiter +
                          formuls.Name +
                          " WHERE nzp_frm = 1994;");
                var count2 = Convert.ToInt32(obj2);

                if (count2 == 0)
                {
                    Database.Insert(formuls, new string[] { "nzp_frm", "name_frm", "nzp_measure", "is_device" }, new string[] { "1994", "Рассрочка-ОТП платежа по Пост.№354", "6", "0" });
                }

                object obj3 =
   Database.ExecuteScalar("SELECT count(*) FROM " + formuls.Schema + Database.TableDelimiter +
                          formuls.Name +
                          " WHERE nzp_frm = 1995;");
                var count3 = Convert.ToInt32(obj3);

                if (count3 == 0)
                {
                    Database.Insert(formuls, new string[] { "nzp_frm", "name_frm", "nzp_measure", "is_device" }, new string[] { "1995", "Рассрочка-ГВС платежа по Пост.№354", "6", "0" });
                }

                object obj4 =
   Database.ExecuteScalar("SELECT count(*) FROM " + formuls.Schema + Database.TableDelimiter +
                          formuls.Name +
                          " WHERE nzp_frm = 1996;");
                var count4 = Convert.ToInt32(obj4);

                if (count4 == 0)
                {
                    Database.Insert(formuls, new string[] { "nzp_frm", "name_frm", "nzp_measure", "is_device" }, new string[] { "1996", "Рассрочка-ХГВ платежа по Пост.№354", "6", "0" });
                }

                object obj5 =
   Database.ExecuteScalar("SELECT count(*) FROM " + formuls.Schema + Database.TableDelimiter +
                          formuls.Name +
                          " WHERE nzp_frm = 1997;");
                var count5 = Convert.ToInt32(obj5);

                if (count5 == 0)
                {
                    Database.Insert(formuls, new string[] { "nzp_frm", "name_frm", "nzp_measure", "is_device" }, new string[] { "1997", "Рассрочка-ЭЛД платежа по Пост.№354", "6", "0" });
                }

                object obj6 =
   Database.ExecuteScalar("SELECT count(*) FROM " + formuls.Schema + Database.TableDelimiter +
                          formuls.Name +
                          " WHERE nzp_frm = 1998;");
                var count6 = Convert.ToInt32(obj6);

                if (count6 == 0)
                {
                    Database.Insert(formuls, new string[] { "nzp_frm", "name_frm", "nzp_measure", "is_device" }, new string[] { "1998", "Рассрочка-ЭЛН платежа по Пост.№354", "6", "0" });
                }

                object obj7 =
   Database.ExecuteScalar("SELECT count(*) FROM " + formuls.Schema + Database.TableDelimiter +
                          formuls.Name +
                          " WHERE nzp_frm = 1999;");
                var count7 = Convert.ToInt32(obj7);

                if (count7 == 0)
                {
                    Database.Insert(formuls, new string[] { "nzp_frm", "name_frm", "nzp_measure", "is_device" }, new string[] { "1999", "Рассрочка-ГАЗ платежа по Пост.№354", "6", "0" });
                }

            }

            SchemaQualifiedObjectName formuls_opis = new SchemaQualifiedObjectName() { Name = "formuls_opis", Schema = CurrentSchema };
            if (Database.TableExists(formuls))
            {
                Database.Delete(formuls_opis, "nzp_frm = 1992");
                Database.Delete(formuls_opis, "nzp_frm = 1993");
                Database.Delete(formuls_opis, "nzp_frm = 1994");
                Database.Delete(formuls_opis, "nzp_frm = 1995");
                Database.Delete(formuls_opis, "nzp_frm = 1996");
                Database.Delete(formuls_opis, "nzp_frm = 1997");
                Database.Delete(formuls_opis, "nzp_frm = 1998");
                Database.Delete(formuls_opis, "nzp_frm = 1999");

                Database.Insert(formuls_opis, new string[] { "nzp_frm", "nzp_frm_kod", "nzp_frm_typ", "nzp_prm_tarif_ls", "nzp_prm_tarif_lsp", "nzp_prm_tarif_dm", "nzp_prm_tarif_su", "nzp_prm_tarif_bd",
  "nzp_frm_typrs", "nzp_prm_rash", "nzp_prm_rash1" }, new string[] { "1992", "1992", "1999", "0", "0", "0", "0", "0", "4", "0", "0" });

                Database.Insert(formuls_opis, new string[] { "nzp_frm", "nzp_frm_kod", "nzp_frm_typ", "nzp_prm_tarif_ls", "nzp_prm_tarif_lsp", "nzp_prm_tarif_dm", "nzp_prm_tarif_su", "nzp_prm_tarif_bd",
  "nzp_frm_typrs", "nzp_prm_rash", "nzp_prm_rash1"}, new string[] { "1993", "1993", "1999", "0", "0", "0", "0", "0", "4", "0", "0" });

                Database.Insert(formuls_opis, new string[] { "nzp_frm", "nzp_frm_kod", "nzp_frm_typ", "nzp_prm_tarif_ls", "nzp_prm_tarif_lsp", "nzp_prm_tarif_dm", "nzp_prm_tarif_su", "nzp_prm_tarif_bd",
  "nzp_frm_typrs", "nzp_prm_rash", "nzp_prm_rash1"}, new string[] { "1994", "1994", "1999", "0", "0", "0", "0", "0", "4", "0", "0" });

                Database.Insert(formuls_opis, new string[] { "nzp_frm", "nzp_frm_kod", "nzp_frm_typ", "nzp_prm_tarif_ls", "nzp_prm_tarif_lsp", "nzp_prm_tarif_dm", "nzp_prm_tarif_su", "nzp_prm_tarif_bd",
  "nzp_frm_typrs", "nzp_prm_rash", "nzp_prm_rash1"}, new string[] { "1995", "1995", "1999", "0", "0", "0", "0", "0", "4", "0", "0" });

                Database.Insert(formuls_opis, new string[] { "nzp_frm", "nzp_frm_kod", "nzp_frm_typ", "nzp_prm_tarif_ls", "nzp_prm_tarif_lsp", "nzp_prm_tarif_dm", "nzp_prm_tarif_su", "nzp_prm_tarif_bd",
  "nzp_frm_typrs", "nzp_prm_rash", "nzp_prm_rash1"}, new string[] { "1996", "1996", "1999", "0", "0", "0", "0", "0", "4", "0", "0" });

                Database.Insert(formuls_opis, new string[] { "nzp_frm", "nzp_frm_kod", "nzp_frm_typ", "nzp_prm_tarif_ls", "nzp_prm_tarif_lsp", "nzp_prm_tarif_dm", "nzp_prm_tarif_su", "nzp_prm_tarif_bd",
  "nzp_frm_typrs", "nzp_prm_rash", "nzp_prm_rash1"}, new string[] { "1997", "1997", "1999", "0", "0", "0", "0", "0", "4", "0", "0" });

                Database.Insert(formuls_opis, new string[] { "nzp_frm", "nzp_frm_kod", "nzp_frm_typ", "nzp_prm_tarif_ls", "nzp_prm_tarif_lsp", "nzp_prm_tarif_dm", "nzp_prm_tarif_su", "nzp_prm_tarif_bd",
  "nzp_frm_typrs", "nzp_prm_rash", "nzp_prm_rash1"}, new string[] { "1998", "1998", "1999", "0", "0", "0", "0", "0", "4", "0", "0" });

                Database.Insert(formuls_opis, new string[] { "nzp_frm", "nzp_frm_kod", "nzp_frm_typ", "nzp_prm_tarif_ls", "nzp_prm_tarif_lsp", "nzp_prm_tarif_dm", "nzp_prm_tarif_su", "nzp_prm_tarif_bd",
  "nzp_frm_typrs", "nzp_prm_rash", "nzp_prm_rash1"}, new string[] { "1999", "1999", "1999", "0", "0", "0", "0", "0", "4", "0", "0" });
            }

            SchemaQualifiedObjectName prm_tarifs = new SchemaQualifiedObjectName() { Name = "prm_tarifs", Schema = CurrentSchema };
            if (Database.TableExists(prm_tarifs))
            {
                Database.Delete(prm_tarifs, "nzp_frm = 1992");
                Database.Delete(prm_tarifs, "nzp_frm = 1993");
                Database.Delete(prm_tarifs, "nzp_frm = 1994");
                Database.Delete(prm_tarifs, "nzp_frm = 1995");
                Database.Delete(prm_tarifs, "nzp_frm = 1996");
                Database.Delete(prm_tarifs, "nzp_frm = 1997");
                Database.Delete(prm_tarifs, "nzp_frm = 1998");
                Database.Delete(prm_tarifs, "nzp_frm = 1999");

                Database.Insert(prm_tarifs, new string[] { "nzp_serv", "nzp_frm", "nzp_prm", "is_edit", "nzp_user" }, new string[] { "306", "1992", "0", "1", "-1000" });
                Database.Insert(prm_tarifs, new string[] { "nzp_serv", "nzp_frm", "nzp_prm", "is_edit", "nzp_user" }, new string[] { "308", "1993", "0", "1", "-1000" });
                Database.Insert(prm_tarifs, new string[] { "nzp_serv", "nzp_frm", "nzp_prm", "is_edit", "nzp_user" }, new string[] { "309", "1994", "0", "1", "-1000" });
                Database.Insert(prm_tarifs, new string[] { "nzp_serv", "nzp_frm", "nzp_prm", "is_edit", "nzp_user" }, new string[] { "307", "1995", "0", "1", "-1000" });
                Database.Insert(prm_tarifs, new string[] { "nzp_serv", "nzp_frm", "nzp_prm", "is_edit", "nzp_user" }, new string[] { "307", "1996", "0", "1", "-1000" });
                Database.Insert(prm_tarifs, new string[] { "nzp_serv", "nzp_frm", "nzp_prm", "is_edit", "nzp_user" }, new string[] { "310", "1997", "0", "1", "-1000" });
                Database.Insert(prm_tarifs, new string[] { "nzp_serv", "nzp_frm", "nzp_prm", "is_edit", "nzp_user" }, new string[] { "310", "1998", "0", "1", "-1000" });
                Database.Insert(prm_tarifs, new string[] { "nzp_serv", "nzp_frm", "nzp_prm", "is_edit", "nzp_user" }, new string[] { "311", "1999", "0", "1", "-1000" });

            }

            SchemaQualifiedObjectName prm_name = new SchemaQualifiedObjectName() { Name = "prm_name", Schema = CurrentSchema };
            if (Database.TableExists(prm_name))
            {
                Database.Delete(prm_1, "nzp_prm = 1116");
                Database.Delete(prm_1, "nzp_prm = 1117");
                Database.Delete(prm_1, "nzp_prm = 1118");
                Database.Delete(prm_1, "nzp_prm = 1119");
                Database.Delete(prm_1, "nzp_prm = 1120");
                Database.Delete(prm_1, "nzp_prm = 1121");
                Database.Delete(prm_5, "nzp_prm = 1122");
            }

        }

        public override void Revert()
        {
            SetSchema(Bank.Kernel);
            // TODO: Downgrade LocalPref_Kernel

            SetSchema(Bank.Data);
            // TODO: Downgrade LocalPref_Data

        }
    }


}
