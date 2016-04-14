using System;
using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2014052605201, MigrateDataBase.CentralBank)]
    public class Migration_2014052605201_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);
            // TODO: Upgrade CentralPref_Kernel
            SchemaQualifiedObjectName s_payer = new SchemaQualifiedObjectName() { Name = "s_payer", Schema = CurrentSchema };
            if (!Database.ColumnExists(s_payer, "changed_on"))
            {
                Database.AddColumn(s_payer, new Column("changed_on", DbType.DateTime));
            }
            if (!Database.ColumnExists(s_payer, "changed_by"))
            {
                Database.AddColumn(s_payer, new Column("changed_by", DbType.DateTime));
            }
            //todo удалить nzp_supp

            SchemaQualifiedObjectName pkod_types = new SchemaQualifiedObjectName() { Name = "pkod_types", Schema = CurrentSchema };
            if (!Database.TableExists(pkod_types))
            {
                Database.AddTable(pkod_types,
                    new Column("nzp_pkod_type", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("type_name", DbType.String.WithSize(100)), 
                    new Column("description", DbType.String.WithSize(250)),
                    new Column("is_default", DbType.Int16));

                Database.AddPrimaryKey("PK_pkod_types", pkod_types, "nzp_pkod_type");
            }



            SetSchema(Bank.Data);
            // TODO: Upgrade CentralPref_Data
            SchemaQualifiedObjectName area_codes = new SchemaQualifiedObjectName() { Name = "area_codes", Schema = CurrentSchema };
            if (!Database.ColumnExists(area_codes, "nzp_payer"))
            {
                Database.AddColumn(area_codes, new Column("nzp_payer", DbType.Int32));                
            }
            if (!Database.ColumnExists(area_codes, "nzp_pkod_type"))
            {
                Database.AddColumn(area_codes, new Column("nzp_pkod_type", DbType.Int32));
            }
            if (!Database.ColumnExists(area_codes, "code"))
            {
                Database.AddPrimaryKey("PK_code", area_codes, "code");
            }
            //todo удалить nzp_area

            SchemaQualifiedObjectName kvar_pkodes = new SchemaQualifiedObjectName() { Name = "kvar_pkodes", Schema = CurrentSchema };

            if (Database.TableExists(kvar_pkodes))
            {
                if (Convert.ToInt32(Database.ExecuteScalar(" select count(*) from  " + kvar_pkodes.Name)) == 0)
                {
                    Database.RemoveTable(kvar_pkodes);
                }
            }
                       
            if (!Database.TableExists(kvar_pkodes))
            {
                Database.AddTable(kvar_pkodes,
                    new Column("nzp_kvar_pkod", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("nzp_kvar", DbType.Int32),
                    new Column("nzp_payer_agent", DbType.Int32),
                    new Column("nzp_payer_princip", DbType.Int32),
                    new Column("area_code", DbType.Int32),
                    new Column("pkod10", DbType.Int32),
                    new Column("is_default", DbType.Int32),
                    new Column("change_on", DbType.DateTime),
                    new Column("change_by", DbType.Int32));

                Database.AddPrimaryKey("PK_kvar_pkodes", kvar_pkodes, "nzp_kvar_pkod");
                Database.AddForeignKey("FK_area_code", kvar_pkodes, "area_code", area_codes, "code");
            }
        }

        public override void Revert()
        {
            SetSchema(Bank.Kernel);
            // TODO: Downgrade CentralPref_Kernel
            SchemaQualifiedObjectName s_payer = new SchemaQualifiedObjectName() { Name = "s_payer", Schema = CurrentSchema };
            if (Database.ColumnExists(s_payer, "changed_on"))
            {
                Database.RemoveColumn(s_payer, "changed_on");
            }
            if (Database.ColumnExists(s_payer, "changed_by"))
            {
                Database.RemoveColumn(s_payer, "changed_by");
            }            

            SchemaQualifiedObjectName pkod_types = new SchemaQualifiedObjectName() { Name = "pkod_types", Schema = CurrentSchema };
            if (Database.TableExists(pkod_types))
            {
                Database.RemoveTable(pkod_types);                
            }

            SetSchema(Bank.Data);
            // TODO: Downgrade CentralPref_Data
            SchemaQualifiedObjectName area_codes = new SchemaQualifiedObjectName() { Name = "area_codes", Schema = CurrentSchema };
            if (Database.ColumnExists(area_codes, "nzp_payer"))
            {
                Database.RemoveColumn(area_codes, "nzp_payer");
            }
            if (Database.ColumnExists(area_codes, "nzp_pkod_type"))
            {
                Database.RemoveColumn(area_codes, "nzp_pkod_type");
            }            

            SchemaQualifiedObjectName kvar_pkodes = new SchemaQualifiedObjectName() { Name = "kvar_pkodes", Schema = CurrentSchema };
            if (Database.TableExists(kvar_pkodes))
            {
                Database.RemoveConstraint(kvar_pkodes, "FK_area_code");
                Database.RemoveTable(kvar_pkodes);                
            }
        }
    }
}
