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
    [Migration(2015011201103, MigrateDataBase.Charge)]
    public class Migration_2015011201103_Charge : Migration
    {
        public override void Apply()
        {


            SchemaQualifiedObjectName calc_gku_xx = new SchemaQualifiedObjectName() { Schema = CurrentSchema };

            if (CurrentSchema.Contains("15"))
            {
                for (int i = 1; i <= 12; i++)
                {
                    calc_gku_xx.Name = "calc_gku_" + i.ToString("00");
                    if (Database.TableExists(calc_gku_xx))
                    {
                        if (Database.ColumnExists(calc_gku_xx, "stek"))
                        {
                            Database.ExecuteNonQuery("ALTER TABLE " + CurrentSchema + Database.TableDelimiter + calc_gku_xx.Name +
                                                     " ALTER COLUMN stek SET DEFAULT 3");
                        }
                        else
                        {
                            Database.AddColumn(calc_gku_xx, new Column("stek", new ColumnType(DbType.Int32), ColumnProperty.NotNull, "3"));
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

