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
    [Migration(2014122612401, MigrateDataBase.CentralBank | MigrateDataBase.LocalBank)]
    public class Migration_2014122612401_CentralBank : Migration
    {
        public override void Apply()
        {
            SetSchema(Bank.Data);
            SchemaQualifiedObjectName s_bankstr = new SchemaQualifiedObjectName() { Name = "s_bankstr", Schema = CurrentSchema };
            if (Database.TableExists(s_bankstr))
            {
                if (!Database.ColumnExists(s_bankstr, "sb20"))
                    Database.AddColumn(s_bankstr, new Column("sb20", DbType.String.WithSize(100)));
                else
                    Database.ChangeColumn(s_bankstr, "sb20", DbType.String.WithSize(100), false);

                if (!Database.ColumnExists(s_bankstr, "sb21"))
                    Database.AddColumn(s_bankstr, new Column("sb21", DbType.String.WithSize(100)));
                else
                    Database.ChangeColumn(s_bankstr, "sb21", DbType.String.WithSize(100), false);

                if (!Database.ColumnExists(s_bankstr, "sb22"))
                    Database.AddColumn(s_bankstr, new Column("sb22", DbType.String.WithSize(100)));
                else
                    Database.ChangeColumn(s_bankstr, "sb22", DbType.String.WithSize(100), false);

                if (!Database.ColumnExists(s_bankstr, "sb23"))
                    Database.AddColumn(s_bankstr, new Column("sb23", DbType.String.WithSize(100)));
                else
                    Database.ChangeColumn(s_bankstr, "sb23", DbType.String.WithSize(100), false);


                if (!Database.ColumnExists(s_bankstr, "sb24"))
                    Database.AddColumn(s_bankstr, new Column("sb24", DbType.String.WithSize(100)));
                else
                    Database.ChangeColumn(s_bankstr, "sb24", DbType.String.WithSize(100), false);

                if (!Database.ColumnExists(s_bankstr, "sb25"))
                    Database.AddColumn(s_bankstr, new Column("sb25", DbType.String.WithSize(100)));
                else
                    Database.ChangeColumn(s_bankstr, "sb25", DbType.String.WithSize(100), false);

                if (!Database.ColumnExists(s_bankstr, "sb26"))
                    Database.AddColumn(s_bankstr, new Column("sb26", DbType.String.WithSize(100)));
                else
                    Database.ChangeColumn(s_bankstr, "sb26", DbType.String.WithSize(100), false);

                if (!Database.ColumnExists(s_bankstr, "sb27"))
                    Database.AddColumn(s_bankstr, new Column("sb27", DbType.String.WithSize(100)));
                else
                    Database.ChangeColumn(s_bankstr, "sb27", DbType.String.WithSize(100), false);
            }
        }

        public override void Revert()
        {
            
        }
    }
}
