using System;
using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2014._2014084
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2014082208402, MigrateDataBase.CentralBank)]
    public class Migration_2014082208402_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Upload);
            SchemaQualifiedObjectName file_info_pu = new SchemaQualifiedObjectName() { Name = "file_info_pu", Schema = CurrentSchema };

            if (!Database.TableExists(file_info_pu))
            {

                Database.AddTable(file_info_pu,
                    new Column("nzp_file", DbType.Int32),
                    new Column("id", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("id_pu", DbType.String.WithSize(20)),
                    new Column("num_ls_pu", DbType.Int32)
                    );
            }

            SchemaQualifiedObjectName file_odpu = new SchemaQualifiedObjectName() { Name = "file_odpu", Schema = CurrentSchema };
            if (Database.TableExists(file_odpu))
            {
                if (!Database.ColumnExists(file_odpu, "type_pu"))
                {
                    Database.AddColumn(file_odpu, new Column("type_pu", DbType.Int32));
                }
            }

        }

        public override void Revert()
        {
            SetSchema(Bank.Upload);
            SchemaQualifiedObjectName file_info_pu = new SchemaQualifiedObjectName() { Name = "file_info_pu", Schema = CurrentSchema };
            if (Database.TableExists(file_info_pu)) 
                Database.RemoveTable(file_info_pu);

            SchemaQualifiedObjectName file_odpu = new SchemaQualifiedObjectName() { Name = "file_odpu", Schema = CurrentSchema };
            if (Database.TableExists(file_odpu))
            {
                if (Database.ColumnExists(file_odpu, "type_pu"))
                {
                    Database.RemoveColumn(file_odpu, "type_pu");
                }
            }


        }
    }

    
}
