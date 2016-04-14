using System;
using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;
namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2014111111101, MigrateDataBase.CentralBank)]
    public class Migration_2014111111101_CentralBank : Migration
    {
         public override void Apply()
         {
             SetSchema(Bank.Kernel);
             SchemaQualifiedObjectName s_payer = new SchemaQualifiedObjectName() { Name = "s_payer", Schema = CurrentSchema };
             int nzp_payer = 0;
             if (Database.TableExists(s_payer))
             {
                 int count = Convert.ToInt32(Database.ExecuteScalar(
                   Database.FormatSql("SELECT count(nzp_payer) FROM {0:NAME} WHERE payer = 'Фиктивный контрагент' ", s_payer)));
                 if (count > 0)
                 {
                     object nzp = Database.ExecuteScalar(
                        Database.FormatSql("SELECT nzp_payer FROM {0:NAME} WHERE payer = 'Фиктивный контрагент' ", s_payer));

                     if (Int32.TryParse(nzp.ToString(), out nzp_payer)) nzp_payer = Convert.ToInt32(nzp);
                     else nzp_payer = 0;
                 }
            

                 if (nzp_payer == 0)
                 {
                     Database.Insert(s_payer, new string[] { "payer", "npayer" }, new string[] { "Фиктивный контрагент", "Фиктивный контрагент" });

                     if (Database.ProviderName == "Informix")
                     {
                         nzp_payer = Convert.ToInt32(Database.ExecuteScalar("SELECT first 1 dbinfo('sqlca.sqlerrd1') FROM systables "));
                     }
                     else
                     {
                         nzp_payer = Convert.ToInt32(Database.ExecuteScalar("SELECT lastval() "));
                     }
                 }
             }

             if (nzp_payer > 0)
             {
                 SetSchema(Bank.Data);
                 SchemaQualifiedObjectName s_area = new SchemaQualifiedObjectName() { Name = "s_area", Schema = CurrentSchema };
                 Database.Update(s_area, new string[] { "nzp_payer" }, new string[] { nzp_payer.ToString() }, "nzp_payer is null");
                 Database.ChangeColumn(s_area, "nzp_payer", DbType.Int32, true);
             }
         }
    }

    [Migration(2014111111101, MigrateDataBase.LocalBank)]
    public class Migration_2014111111101_LocalBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            SchemaQualifiedObjectName s_payer = new SchemaQualifiedObjectName() { Name = "s_payer", Schema = CentralKernel };
            int nzp_payer = 0;
            if (Database.TableExists(s_payer))
            {
                int count = Convert.ToInt32(Database.ExecuteScalar(
                    Database.FormatSql("SELECT count(nzp_payer) FROM {0:NAME} WHERE payer = 'Фиктивный контрагент' ", s_payer)));
                if (count > 0)
                {
                    object nzp = Database.ExecuteScalar(
                       Database.FormatSql("SELECT nzp_payer FROM {0:NAME} WHERE payer = 'Фиктивный контрагент' ", s_payer));

                    if (Int32.TryParse(nzp.ToString(), out nzp_payer)) nzp_payer = Convert.ToInt32(nzp);
                    else nzp_payer = 0;
                }
                if (nzp_payer == 0)
                {
                    Database.Insert(s_payer, new string[] { "payer", "npayer" }, new string[] { "Фиктивный контрагент", "Фиктивный контрагент" });

                    if (Database.ProviderName == "Informix")
                    {
                        nzp_payer = Convert.ToInt32(Database.ExecuteScalar("SELECT first 1 dbinfo('sqlca.sqlerrd1') FROM systables "));
                    }
                    else
                    {
                        nzp_payer = Convert.ToInt32(Database.ExecuteScalar("SELECT lastval() "));
                    }
                }
            }

            if (nzp_payer > 0)
            {
                SetSchema(Bank.Data);
                SchemaQualifiedObjectName s_area = new SchemaQualifiedObjectName() { Name = "s_area", Schema = CurrentSchema };
                Database.Update(s_area, new string[] { "nzp_payer" }, new string[] { nzp_payer.ToString() }, "nzp_payer is null");
                Database.ChangeColumn(s_area, "nzp_payer", DbType.Int32, true);
            }
        }
    }
}
