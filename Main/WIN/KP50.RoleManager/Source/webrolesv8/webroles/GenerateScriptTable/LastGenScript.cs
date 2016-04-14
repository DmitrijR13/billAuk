using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace webroles.GenerateScriptTable
{
    class LastGenScript:GenerateScript
    {



        public override void Request()
        {
            switch (TypeScr)
            {
                case TypeScript.ChangeDeveloper: break;
                case TypeScript.ChangeCustomer: break;
                case TypeScript.FullDeveloper:
                     break;
                case TypeScript.FullCustomer:
                     break;
            }
        }

        public override void GenerateScr()
        {
            switch (TypeScr)
            {
                case TypeScript.ChangeDeveloper: break;
                case TypeScript.ChangeCustomer:
                    if (DropTriggersList!=null && DropTriggersList.Count != 0)
                        AddToLinesList(DropTriggersList);
                    addCrypt();
                        break;
                case TypeScript.FullDeveloper:
                    generateToFullScript(); break;
                case TypeScript.FullCustomer:
                    generateToFullScript(); break;
            }
        }

        private void addCrypt()
        {
            
            if (ChangedRowCollection.Count(row => row.TableName == Tables.actions_show && (row.State == DataRowState.Added || row.State == DataRowState.Modified)) != 0)
            {
                var list = new List<string>();
                if (ChangedRowCollection.Count(
                        row => row.TableName == Tables.actions_show &&
                            (row.State == DataRowState.Added) && 
                            row.ColValuesDictionary!=null &&
                            row.ColValuesDictionary.Count!=0 &&
                            (row.ColValuesDictionary["cur_page"] == "133" ||
                             row.ColValuesDictionary["cur_page"] == "135") &&
                            row.ColValuesDictionary["act_tip"] == "2" && row.ColValuesDictionary["act_dd"] == "5") != 0)
                {
                    {
                        list = new List<string>
                        {
                            "-- Сортирует отчеты по имени",
                            "WITH acs as (select distinct a.nzp_ash, a.sort_kod, a.cur_page, a.nzp_act as a_nzp_act, act_tip, act_dd, s.nzp_act as sact_nap_act, s.act_name as name_rep ",
                            "FROM " + Tables.actions_show + " a Left outer join " + Tables.s_actions + " s on (a.nzp_act=s.nzp_act) ",
                            "WHERE (act_tip=2 and act_dd=5 and  (cur_page=135 OR cur_page=133))), ",
                            "ord as (select *,(select count(1) from  acs ab where ab.name_rep <=acs.name_rep) as row_num FROM  acs order by row_num) ",
                            "UPDATE actions_show ah set sort_kod=ord.row_num from ord where ord.nzp_ash=ah.nzp_ash; "
                        };
                        list.Add(string.Empty);
                    }
                }

                list.Add(
                    "update actions_show set sign = sort_kod::varchar||act_dd::varchar||act_tip::varchar||nzp_act::varchar||cur_page||'-'||nzp_ash||'actions_show'; ");
                AddToLinesList(list);
            }

            if (ChangedRowCollection.Count(row => row.TableName == Tables.role_actions && (row.State == DataRowState.Added || row.State == DataRowState.Modified)) != 0)
            {
                var list = new List<string>
                {
                   "update role_actions set sign = nzp_role::varchar||nzp_page::varchar||nzp_act::varchar||'-'||id::varchar||'role_actions' ",
                   "where nzp_role >= 10 and nzp_role < 1000; ",
                };
                AddToLinesList(list);
            }

            if (ChangedRowCollection.Count(row => row.TableName == Tables.roleskey && (row.State == DataRowState.Added || row.State == DataRowState.Modified)) != 0)
            {
                var list = new List<string>
                {
                  "update roleskey set sign = tip::varchar||kod::varchar||nzp_role::varchar||'-'||nzp_rlsv::varchar||'roles' ",
                  "where (nzp_role >= 10 and nzp_role < 1000) or (tip = 105 and kod >= 10 and kod < 1000); ",
                };
                AddToLinesList(list);
            }

            if (ChangedRowCollection.Count(row => row.TableName == Tables.role_pages && (row.State == DataRowState.Added || row.State == DataRowState.Modified)) != 0)
            {
                var list = new List<string>
                {
                  "update role_pages set sign = nzp_role::varchar||nzp_page::varchar||'-'||id::varchar||'role_pages' ",
                  "where nzp_role >= 10 and nzp_role < 1000; ",
                };
                AddToLinesList(list);
            }

            if (ChangedRowCollection.Count(row => (row.TableName == Tables.pages && row.State == DataRowState.Added) || (row.TableName == Tables.page_links && (row.State == DataRowState.Added || row.State == DataRowState.Modified))) != 0)
            {
                var list = new List<string>
                {
                  "update pages_show set sign = sort_kod::varchar||up_kod::varchar||page_url||cur_page||'-'||nzp_psh||'pages_show';",
                };
                AddToLinesList(list);
            }
        }

        private void generateToFullScript()
        {
            var list = new List<string>
            {
          "--Шифрование данных",
"update " + Tables.pages_show + " set sign = sort_kod::varchar||up_kod::varchar||page_url||cur_page||'-'||nzp_psh||'" + Tables.pages_show + "';" ,
          "update " + Tables.actions_show + " set sign = sort_kod::varchar||act_dd::varchar||act_tip::varchar||nzp_act::varchar||cur_page||'-'||nzp_ash||'" + Tables.actions_show + "';",
"update " + Tables.roleskey + " set sign = tip::varchar||kod::varchar||nzp_role::varchar||'-'||nzp_rlsv::varchar||'roles' ",
"where (nzp_role >= 10 and nzp_role < 1000) or (tip = 105 and kod >= 10 and kod < 1000);",
"update " + Tables.role_pages + " set sign = nzp_role::varchar||nzp_page::varchar||'-'||id::varchar||'" + Tables.role_pages + "'",
"where nzp_role >= 10 and nzp_role < 1000;",
"update " + Tables.role_actions + " set sign = nzp_role::varchar||nzp_page::varchar||nzp_act::varchar||'-'||id::varchar||'" + Tables.role_actions + "'",
"where nzp_role >= 10 and nzp_role < 1000;",
string.Empty
            };
            AddToLinesList(list);
        }
    }
}
