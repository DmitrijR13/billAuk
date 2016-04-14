using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2014._2014085
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2014082708505, MigrateDataBase.CentralBank)]
    public class Migration_2014082708505_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Kernel);


            SchemaQualifiedObjectName pkod_types = new SchemaQualifiedObjectName() { Name = "pkod_types", Schema = CurrentSchema };
            if (!Database.TableExists(pkod_types))
            {
                Database.AddTable(pkod_types,
                    new Column("nzp_pkod_type", DbType.Int32, ColumnProperty.Identity | ColumnProperty.NotNull),
                    new Column("type_name", DbType.String.WithSize(100)),
                    new Column("description", DbType.String.WithSize(250)),
                    new Column("is_default", DbType.Int32));

            }

            SetSchema(Bank.Data);
            SchemaQualifiedObjectName kvar_pkodes = new SchemaQualifiedObjectName() { Name = "kvar_pkodes", Schema = CurrentSchema };
            if (!Database.ColumnExists(kvar_pkodes, "nzp_payer")) 
                Database.AddColumn(kvar_pkodes, new Column("nzp_payer", DbType.Int32));
            if (!Database.ColumnExists(kvar_pkodes, "is_princip"))
                Database.AddColumn(kvar_pkodes, new Column("is_princip", DbType.Int32));
            if (!Database.ColumnExists(kvar_pkodes, "is_default"))
                Database.AddColumn(kvar_pkodes, new Column("is_default", DbType.Int32));
            

            SchemaQualifiedObjectName area_codes = new SchemaQualifiedObjectName() { Name = "area_codes", Schema = CurrentSchema };
            if (!Database.ColumnExists(area_codes, "code"))
                Database.AddColumn(area_codes, new Column("code", DbType.Int32));
            if (!Database.ColumnExists(area_codes, "nzp_payer"))
                Database.AddColumn(area_codes, new Column("nzp_payer", DbType.Int32));
            if (!Database.ColumnExists(area_codes, "is_active"))
                Database.AddColumn(area_codes, new Column("is_active", DbType.Int32));
            if (!Database.ColumnExists(area_codes, "nzp_pkod_type"))
                Database.AddColumn(area_codes, new Column("nzp_pkod_type", DbType.Int32));


            IDataReader reader;
            if (Database.ProviderName == "PostgreSQL")
            {
                reader = Database.ExecuteReader("SELECT nzp_payer_agent, count(*)" +
                                                " FROM " + CentralKernel + ".supplier" +
                                                " WHERE nzp_payer_agent is not null " +
                                                " GROUP BY 1" +
                                                " ORDER BY 2 desc limit 1");

                try
                {
                    while (reader.Read())
                    {
                        string nzp_payer_agent = reader["nzp_payer_agent"].ToString();

                        if (Database.TableExists(area_codes))
                        {
                            Database.Update(area_codes, new string[] { "nzp_payer" }, new string[] { nzp_payer_agent }, " nzp_payer is null ");
                        }

                    }
                }
                finally
                {
                    reader.Close();
                }
            }

        }

        public override void Revert()
        {
            SetSchema(Bank.Kernel);
            // TODO: Downgrade CentralPref_Kernel 

            SetSchema(Bank.Data);
            // TODO: Downgrade CentralPref_Data

            SetSchema(Bank.Upload);
            // TODO: Downgrade CentralPref_Upload
        }
    }

}
