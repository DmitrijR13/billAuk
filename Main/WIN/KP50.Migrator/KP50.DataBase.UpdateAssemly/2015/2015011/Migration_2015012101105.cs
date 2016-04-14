using System.Data;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015011
{
    [Migration(2015012101105, MigrateDataBase.Web)]
    public class Migration_2015012101105_Web : Migration
    {
        public override void Apply()
        {
            var user_payer_agent = new SchemaQualifiedObjectName() { Name = "user_payer_agent", Schema = CurrentSchema };
            var users = new SchemaQualifiedObjectName() { Name = "users", Schema = CurrentSchema };
            var s_payer = new SchemaQualifiedObjectName() { Name = "s_payer", Schema = CentralKernel };
            
            if (!Database.TableExists("user_payer_agent"))
            {
                Database.AddTable(user_payer_agent,
                    new Column("nzp_user_payer_agent", DbType.Int32, ColumnProperty.NotNull | ColumnProperty.Identity),
                    new Column("nzp_user", DbType.Int32, ColumnProperty.NotNull),
                    new Column("nzp_payer_agent", DbType.Int32, ColumnProperty.NotNull),
                    new Column("changed_by", DbType.Int32, ColumnProperty.NotNull),
                    new Column("changed_on", DbType.DateTime, ColumnProperty.NotNull, "now()"));

                Database.AddPrimaryKey("PK_user_payer_agent", user_payer_agent, "nzp_user_payer_agent");
                
                Database.AddIndex("IX_user_payer_agent_nzp_user_payer_agent", true, user_payer_agent, "nzp_user_payer_agent");
                Database.AddIndex("IX_user_payer_agent_1", true, user_payer_agent, "nzp_user", "nzp_payer_agent");

                Database.AddIndex("IX_user_payer_agent_nzp_user", false, user_payer_agent, "nzp_user");
                Database.AddIndex("IX_user_payer_agent_nzp_payer_agent", false, user_payer_agent, "nzp_payer_agent");
                Database.AddIndex("IX_user_payer_agent_changed_by", false, user_payer_agent, "changed_by");

                Database.AddForeignKey("FK_user_payer_agent_users_nzp_user", user_payer_agent, "nzp_user", users, "nzp_user");
                Database.AddForeignKey("FK_user_payer_agent_users_changed_by", user_payer_agent, "changed_by", users, "nzp_user");
                Database.AddForeignKey("FK_user_payer_agent_s_payer", user_payer_agent, "nzp_payer_agent", s_payer, "nzp_payer");
            }
        }

        public override void Revert()
        {
        }
    }
}

