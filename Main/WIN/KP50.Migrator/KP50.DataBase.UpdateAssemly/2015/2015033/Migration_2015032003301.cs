using System.Collections.Generic;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2015032003301, MigrateDataBase.CentralBank)]
    public class Migration_2015032003301_CentralBank : Migration
    {
        public override void Apply()
        {
            var s_actionsTable = new SchemaQualifiedObjectName { Name = "s_actions", Schema = "public" };
            var actions_showTable = new SchemaQualifiedObjectName { Name = "actions_show", Schema = "public" };
            var role_actionsTable = new SchemaQualifiedObjectName { Name = "role_actions", Schema = "public" };
            var img_lnkTable = new SchemaQualifiedObjectName { Name = "img_lnk", Schema = "public" };

            string cur_page = "268";
            string nzp_act = "138";
            string nzp_role = "12";

            // Проставить правильное значение для последовательностей
            string actions_showMaxVal = Database.ExecuteScalar("select max(nzp_ash) from public.actions_show").ToString();
            Database.ExecuteNonQuery("select setval('public.actions_show_nzp_ash_seq', " + actions_showMaxVal + ")");
            string role_actionsMaxVal = Database.ExecuteScalar("select max(id) from public.role_actions").ToString();
            Database.ExecuteNonQuery("select setval('public.role_actions_id_seq', " + role_actionsMaxVal + ")");

            // actions_show del
            if (Database.TableExists(actions_showTable))
            {
                Database.Delete(actions_showTable, "nzp_act = " + nzp_act + " and cur_page = " + cur_page);
            }

            // s_actions del
            if (Database.TableExists(s_actionsTable))
            {
                Database.Delete(s_actionsTable, "nzp_act = " + nzp_act);
            }

            // s_actions ins
            if (Database.TableExists(s_actionsTable))
            {
                Database.Insert(s_actionsTable, new
                {
                    nzp_act = nzp_act,
                    act_name = "Открыть месяц",
                    hlp = "Открывает расчетный месяц"
                });
            }

            // actions_show ins
            if (Database.TableExists(actions_showTable))
            {
                Database.Insert(actions_showTable, new
                {
                    cur_page = cur_page,
                    nzp_act = nzp_act,
                    act_tip = 0,
                    act_dd = 0,
                    sort_kod = 3
                });

                Database.ExecuteNonQuery("update public.actions_show set sign = sort_kod::varchar || act_dd::varchar || act_tip:: varchar || nzp_act::varchar || cur_page::varchar || '-' || nzp_ash::varchar || 'actions_show' where cur_page = " + cur_page + " and nzp_act = " + nzp_act);
            }

            // role_actions
            if (Database.TableExists(role_actionsTable))
            {
                Database.Delete(role_actionsTable, "nzp_role = " + nzp_role + " and nzp_act = " + nzp_act + " and nzp_page = " + cur_page);

                Database.Insert(role_actionsTable, new
                {
                    nzp_role = nzp_role,
                    nzp_page = cur_page,
                    nzp_act = nzp_act
                });

                Database.ExecuteNonQuery("update public.role_actions set sign = nzp_role::varchar || nzp_page::varchar || nzp_act::varchar || '-' || id::varchar || 'role_actions' where nzp_page = " + cur_page + " and nzp_act = " + nzp_act);
            }

            // img_lnk
            if (Database.TableExists(img_lnkTable))
            {
                Database.Delete(img_lnkTable, "kod = " + nzp_act);

                Database.Insert(img_lnkTable, new
                {
                    cur_page = cur_page,
                    tip = 2,
                    kod = nzp_act,
                    img_url = "go_back.png"
                });
            }
        }
    }
}