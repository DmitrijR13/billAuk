//using System.Data;
//using KP50.DataBase.Migrator.Framework;
//using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

//namespace KP50.DataBase.UpdateAssembly._2015._2015064
//{
//    // TODO: Set migration version as YYYYMMDDVVV
//    // YYYY - Year
//    // MM   - Month
//    // DD   - Day
//    // VVV  - Version
//    [Migration(2015062506401, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
//    public class Migration_2015062506401_CentralBank : Migration
//    {
//        public override void Apply()
//        {
//            SetSchema(Bank.Data);
//            SchemaQualifiedObjectName prm_2 = new SchemaQualifiedObjectName() { Name = "prm_2", Schema = CurrentSchema };

//            if (Database.TableExists(prm_2))
//            {
//                if (Database.ColumnExists(prm_2, "val_prm"))
//                {
//                    Database.ChangeColumn(prm_2, "val_prm", DbType.String.WithSize(40), false);
//                }
//            }
//        }

//        public override void Revert()
//        {
//            SetSchema(Bank.Kernel);
//            // TODO: Downgrade CentralPref_Kernel

//            SetSchema(Bank.Data);
//            // TODO: Downgrade CentralPref_Data

//            SetSchema(Bank.Upload);
//            // TODO: Downgrade CentralPref_Upload
//        }
//    }
//}
