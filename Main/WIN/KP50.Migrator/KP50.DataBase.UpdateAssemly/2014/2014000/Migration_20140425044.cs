using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
     [Migration(20140425044, MigrateDataBase.CentralBank)]
    public class Migration_20140425044_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);

            SchemaQualifiedObjectName supplier = new SchemaQualifiedObjectName() { Name = "supplier", Schema = CurrentSchema };

            if (Database.TableExists(supplier))
            {
                // Columns:
                if (!Database.ColumnExists(supplier, "nzp_payer_agent"))
                {
                    Database.AddColumn(supplier, new Column("nzp_payer_agent", DbType.Int32));
                }

                if (!Database.ColumnExists(supplier, "nzp_payer_princip"))
                {
                    Database.AddColumn(supplier, new Column("nzp_payer_princip", DbType.Int32));
                }

                if (!Database.ColumnExists(supplier, "nzp_payer_supp"))
                {
                    Database.AddColumn(supplier, new Column("nzp_payer_supp", DbType.Int32));
                }              
            }
        }

        public override void Revert()
        {
            //
        }
    }

     [Migration(20140425044, MigrateDataBase.LocalBank)]
     public class Migration_20140425044_LocalBank : Migration
     {
         public override void Apply()
         {
             SetSchema(Bank.Kernel);
             SchemaQualifiedObjectName supplier = new SchemaQualifiedObjectName() { Name = "supplier", Schema = CurrentSchema };

             if (Database.TableExists(supplier))
             {
                 // Columns:
                 if (!Database.ColumnExists(supplier, "nzp_payer_agent"))
                 {
                     Database.AddColumn(supplier, new Column("nzp_payer_agent", DbType.Int32));
                 }

                 if (!Database.ColumnExists(supplier, "nzp_payer_princip"))
                 {
                     Database.AddColumn(supplier, new Column("nzp_payer_princip", DbType.Int32));
                 }

                 if (!Database.ColumnExists(supplier, "nzp_payer_supp"))
                 {
                     Database.AddColumn(supplier, new Column("nzp_payer_supp", DbType.Int32));
                 }
             }
            
         }

         public override void Revert()
         {
             //
         }
     }
}
