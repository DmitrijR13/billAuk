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
    [Migration(2014122212402, MigrateDataBase.CentralBank)]
    public class Migration_2014122212402_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);
            SchemaQualifiedObjectName s_remark = new SchemaQualifiedObjectName() { Name = "s_remark", Schema = CurrentSchema };
            if (Database.TableExists(s_remark))
            {
                if (!Database.ColumnExists(s_remark, "remark"))
                {
                    Database.AddColumn(s_remark, new Column("remark", DbType.String.WithSize(2000)));
                }
                else
                {
                    Database.ChangeColumn(s_remark, "remark", DbType.String.WithSize(2000), false);
                }
            }


        }

        public override void Revert()
        {

        }
    }


}
