using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace webroles.GenerateScriptTable
{
    class FirstGenScript : GenerateScript
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
                    generateTochangeScript();
                    break;
                case TypeScript.FullDeveloper:
                    generateToFullScript(); break;
                case TypeScript.FullCustomer:
                    generateToFullScript(); break;
            }
        }

        public void generateTochangeScript()
        {
            AddToLinesList(new List<string> { "SET SEARCH_PATH TO public;", string.Empty });
           if  (ChangedRowCollection.Count(row => row.TableName == Tables.pages && row.State == DataRowState.Added)!=0)
            {
                var list = new List<string>
                {
                 "-- Триггер(ы). Необходимы для того, чтобы избежать ограничения внешнего ключа  ",
                "CREATE OR REPLACE FUNCTION add_to_pages() RETURNS trigger LANGUAGE plpgsql AS $body$ ",
                "BEGIN ",
                "if exists(select * from public.pages where nzp_page=NEW.nzp_page) then ",
                "raise NOTICE 'row pages ignored: nzp_page=%, page_name=%, page_url=%', NEW.nzp_page, NEW.page_name, NEW.page_url; ",
                "return NULL; ",
                "end if; ",
                "return NEW; ",
                "END; ",
                "$body$; ",
                "CREATE TRIGGER pages_add BEFORE INSERT ",
                "ON pages FOR EACH ROW ",
                "EXECUTE PROCEDURE add_to_pages() ; ",
                string.Empty
                };
                AddToLinesList(list);
               AddToDropTriggersList(new List<string>
               {
                   "DROP TRIGGER pages_add on pages; ",
                   "DROP FUNCTION add_to_pages();"
               });
            }

           if (ChangedRowCollection.Count(row => row.TableName == Tables.actions_show && row.State == DataRowState.Added) != 0)
           {
               var list = new List<string>
               {
                   "CREATE OR REPLACE FUNCTION add_to_actions_show() RETURNS trigger LANGUAGE plpgsql AS $body$ ",
                   "BEGIN ",
                   "if exists(select * from public.actions_show where cur_page=NEW.cur_page and nzp_act=NEW.nzp_act) then ",
                   "raise NOTICE 'row actions_show ignored: cur_page=%, nzp_act=%', NEW.cur_page, NEW.nzp_act; ",
                   "return NULL; ",
                   "end if; ",
                   "return NEW; ",
                   "END; ",
                   "$body$; ",
                   "CREATE TRIGGER actions_show_add BEFORE INSERT ",
                   "ON actions_show FOR EACH ROW ",
                   "EXECUTE PROCEDURE add_to_actions_show(); ",
                   string.Empty
               };
               AddToLinesList(list);
               AddToDropTriggersList(new List<string>
               {
                   "DROP TRIGGER actions_show_add on actions_show; ",
                   "DROP FUNCTION add_to_actions_show(); "
               });
           }

           if (ChangedRowCollection.Count(row => row.TableName == Tables.actions_lnk && row.State == DataRowState.Added) != 0)
           {
               var list = new List<string>
               {
                   "CREATE OR REPLACE FUNCTION add_to_actions_lnk() RETURNS trigger LANGUAGE plpgsql AS $body$ ",
                   "BEGIN ",
                   "if exists(select * from public.actions_lnk where cur_page=NEW.cur_page and nzp_act=NEW.nzp_act) then  ",
                   "raise NOTICE 'row actions_lnk ignored: cur_page=%, nzp_act=%', NEW.cur_page, NEW.nzp_act; ",
                   "return NULL; ",
                   "end if; ",
                   "return NEW; ",
                   "END; ",
                   "$body$; ",
                   "CREATE TRIGGER actions_lnk_add BEFORE INSERT ",
                   "ON actions_lnk FOR EACH ROW ",
                   "EXECUTE PROCEDURE add_to_actions_lnk(); ",
                   string.Empty
               };
               AddToLinesList(list);
               AddToDropTriggersList(new List<string>
               {
                   "DROP TRIGGER actions_lnk_add on actions_lnk; ",
                   "DROP FUNCTION add_to_actions_lnk(); "
               });
           }

           if (ChangedRowCollection.Count(row => row.TableName == Tables.img_lnk && row.State == DataRowState.Added) != 0)
           {
               var list = new List<string>
               {
                   "CREATE OR REPLACE FUNCTION add_to_img_lnk() RETURNS trigger LANGUAGE plpgsql AS $body$ ",
                   "BEGIN ",
                   "if exists(select * from public.img_lnk where cur_page=NEW.cur_page and tip=NEW.tip and kod= NEW.kod) then ",
                   "raise NOTICE 'row img_lnk ignored: cur_page=%, tip=%, kod=%', NEW.cur_page, NEW.tip, NEW.kod; ",
                   "return NULL; ",
                   "end if; ",
                   "return NEW;",
                   "END; ",
                   "$body$; ",
                   "CREATE TRIGGER img_lnk_add BEFORE INSERT ",
                   "ON img_lnk FOR EACH ROW ",
                   "EXECUTE PROCEDURE add_to_img_lnk(); ",
                   string.Empty
               };
               AddToLinesList(list);
               AddToDropTriggersList(new List<string>
               {
                   "DROP TRIGGER img_lnk_add on img_lnk; ",
                   "DROP FUNCTION add_to_img_lnk(); "
               });
           }
           if (ChangedRowCollection.Count(row => row.TableName == Tables.s_actions && row.State == DataRowState.Added) != 0)
           {
               var list = new List<string>
               {
                   "CREATE OR REPLACE FUNCTION add_to_s_actions() RETURNS trigger LANGUAGE plpgsql AS $body$ ",
                   "BEGIN ",
                   "if exists(select * from public.s_actions where nzp_act=NEW.nzp_act) then ",
                   "raise NOTICE 'row s_actions ignored: nzp_act=%, act_name=%, hlp=%', NEW.nzp_act, NEW.act_name, NEW.hlp; ",
                   "return NULL; ",
                   "end if; ",
                   "return NEW; ",
                   "END; ",
                   "$body$; ",
                   "CREATE TRIGGER s_actions_add BEFORE INSERT ",
                   "ON s_actions FOR EACH ROW ",
                   "EXECUTE PROCEDURE add_to_s_actions(); ",
                   string.Empty
               };
               AddToLinesList(list);
               AddToDropTriggersList(new List<string>
               {
                   "DROP TRIGGER s_actions_add on s_actions; ",
                   "DROP FUNCTION add_to_s_actions(); "
               });
           }

           if (ChangedRowCollection.Count(row => row.TableName == Tables.s_roles && row.State == DataRowState.Added) != 0)
           {
               var list = new List<string>
               {
                   "CREATE OR REPLACE FUNCTION add_to_s_roles() RETURNS trigger LANGUAGE plpgsql AS $body$ ",
                   "BEGIN ",
                   "if exists(select * from public.s_roles where nzp_role=NEW.nzp_role) then ",
                   " raise NOTICE 'row s_roles ignored: nzp_role=%, role=%', NEW.nzp_role, NEW.role; ",
                   "return NULL; ",
                   "end if; ",
                   "return NEW; ",
                   "END; ",
                   "$body$; ",
                   "CREATE TRIGGER s_roles_add BEFORE INSERT ",
                   "ON s_roles FOR EACH ROW ",
                   "EXECUTE PROCEDURE add_to_s_roles(); ",
                   string.Empty
               };
               AddToLinesList(list);
               AddToDropTriggersList(new List<string>
               {
                   "DROP TRIGGER s_roles_add on s_roles; ",
                   "DROP FUNCTION add_to_s_roles(); "
               });
           }

           if (ChangedRowCollection.Count(row => row.TableName == Tables.role_actions && row.State == DataRowState.Added) != 0)
           {
               var list = new List<string>
               {
                   "CREATE OR REPLACE FUNCTION add_to_role_actions() RETURNS trigger LANGUAGE plpgsql AS $body$ ",
                   "BEGIN ",
                   "if exists(select * from public.role_actions where nzp_role=NEW.nzp_role and nzp_page=NEW.nzp_page and nzp_act=NEW.nzp_act) then ",
                   "raise NOTICE 'row role_actions ignored: nzp_role=%, nzp_page=%, nzp_act=%', NEW.nzp_role, NEW.nzp_page, NEW.nzp_act; ",
                   "return NULL; ",
                   "end if; ",
                   "return NEW; ",
                   "END; ",
                   "$body$; ",
                   "CREATE TRIGGER role_actions_add BEFORE INSERT ",
                   "ON role_actions FOR EACH ROW ",
                   "EXECUTE PROCEDURE add_to_role_actions(); ",
                   string.Empty
               };
               AddToLinesList(list);
               AddToDropTriggersList(new List<string>
               {
                   "DROP TRIGGER role_actions_add on role_actions; ",
                   "DROP FUNCTION add_to_role_actions(); "
               });
           }


           if (ChangedRowCollection.Count(row => row.TableName == Tables.role_pages && row.State == DataRowState.Added) != 0)
           {
               var list = new List<string>
               {
                   "CREATE OR REPLACE FUNCTION add_to_role_pages() RETURNS trigger LANGUAGE plpgsql AS $body$ ",
                   "BEGIN ",
                   "if exists(select * from public.role_pages where nzp_role=NEW.nzp_role and nzp_page=NEW.nzp_page) then ",
                   " raise NOTICE 'row role_pages ignored: nzp_role=%, nzp_page=%', NEW.nzp_role, NEW.nzp_page; ",
                   "return NULL; ",
                   "end if; ",
                   "return NEW; ",
                   "END; ",
                   "$body$; ",
                   "CREATE TRIGGER role_pages_add BEFORE INSERT ",
                   "ON role_pages FOR EACH ROW ",
                   "EXECUTE PROCEDURE add_to_role_pages(); ",
                   string.Empty
               };
               AddToLinesList(list);
               AddToDropTriggersList(new List<string>
               {
                   "DROP TRIGGER role_pages_add on role_pages; ",
                   "DROP FUNCTION add_to_role_pages(); "
               });
           }

           if (ChangedRowCollection.Count(row => row.TableName == Tables.roleskey && row.State == DataRowState.Added) != 0)
           {
               var list = new List<string>
               {
                   "CREATE OR REPLACE FUNCTION add_to_roleskey() RETURNS trigger LANGUAGE plpgsql AS $body$ ",
                   "BEGIN ",
                   "if exists(select * from public.roleskey where nzp_role=NEW.nzp_role and tip=NEW.tip and kod=NEW.kod) then ",
                   "raise NOTICE 'row roleskey ignored: nzp_role=%, tip=%, kod=%', NEW.nzp_role, NEW.tip, NEW.kod; ",
                   "return NULL; ",
                   "end if; ",
                   "return NEW; ",
                   "END; ",
                   "$body$; ",
                   "CREATE TRIGGER roleskey_add BEFORE INSERT ",
                   "ON roleskey FOR EACH ROW ",
                   "EXECUTE PROCEDURE add_to_roleskey();  ",
                   string.Empty
               };
               AddToLinesList(list);
               AddToDropTriggersList(new List<string>
               {
                   "DROP TRIGGER roleskey_add on roleskey; ",
                   "DROP FUNCTION add_to_roleskey(); "
               });
           }


           if (ChangedRowCollection.Count(row => row.TableName == Tables.page_links && row.State == DataRowState.Added) != 0)
           {
               var list = new List<string>
               {
                   "CREATE OR REPLACE FUNCTION add_to_page_links() RETURNS trigger LANGUAGE plpgsql AS $body$ ",
                   "BEGIN ",
                   "if exists(select * from public.page_links where (page_from=NEW.page_from OR (page_from is null and NEW.page_from is NULL )) ",
                   "and (group_from=NEW.group_from OR (group_from is null and NEW.group_from is NULL )) ",
                   "and (page_to=NEW.page_to OR (page_to is null and NEW.page_to is NULL )) ",
                   "and (group_to=NEW.group_to OR (group_to is null and NEW.group_to is NULL ))) then ",
                   "raise NOTICE 'row page_links ignored: page_from=%, group_from=%, page_to=%, group_to=%', NEW.page_from, NEW.group_from, NEW.page_to, NEW.group_to ; ",
                   "return NULL; ",
                   "end if; ",
                   "return NEW; ",
                   "END; ",
                   "$body$; ",
                   "CREATE TRIGGER page_links_add BEFORE INSERT ",
                   "ON page_links FOR EACH ROW ",
                   "EXECUTE PROCEDURE add_to_page_links();  ",
                   string.Empty
               };
               AddToLinesList(list);
               AddToDropTriggersList(new List<string>
               {
                   "DROP TRIGGER page_links_add on page_links; ",
                   "DROP FUNCTION add_to_page_links(); "
               });
           }

           if (ChangedRowCollection.Count(row => row.TableName == Tables.report && row.State == DataRowState.Added) != 0)
           {
               var list = new List<string>
               {
                   "CREATE OR REPLACE FUNCTION add_to_report() RETURNS trigger LANGUAGE plpgsql AS $body$ ",
                   "BEGIN ",
                   "if exists(select * from public.report where nzp_act=NEW.nzp_act ) then  ",
                   "raise NOTICE 'row report ignored: nzp_act=%, name=%', NEW.nzp_act, NEW.name; ",
                   "return NULL; ",
                   "end if; ",
                   "return NEW; ",
                   "END; ",
                   "$body$; ",
                   "CREATE TRIGGER report_add BEFORE INSERT ",
                   "ON report FOR EACH ROW ",
                   "EXECUTE PROCEDURE add_to_report();  ",
                   string.Empty
               };
               AddToLinesList(list);
               AddToDropTriggersList(new List<string>
               {
                   "DROP TRIGGER report_add on report; ",
                   "DROP FUNCTION add_to_report(); "
               });
           }

           AddToDropTriggersList(new List<string>
               {
                   string.Empty
               });

        }

        private void generateToFullScript()
        {
            var list = new List<string>
            {
                "SET SEARCH_PATH TO public;",
                string.Empty,
                //  1.pages
                "--страницы",
                "CREATE TABLE IF NOT EXISTS " + Tables.pages,
                "( nzp_page  serial NOT NULL,",
                "  up_kod    integer,",
                "  group_id  integer,",
                "  page_url  char(200),",
                "  page_menu char(200),",
                "  page_name char(200),",
                "  hlp       char(200)," +
                "CONSTRAINT pk_pages PRIMARY KEY (nzp_page))",
                "WITHOUT OIDS;",
                "DROP INDEX IF EXISTS ix_pages_1;",
                "CREATE UNIQUE INDEX ix_pages_1 on " + Tables.pages +"(nzp_page);",
                string.Empty,
                // 2.page_links
                "--связи между страницами",
                "CREATE TABLE IF NOT EXISTS " + Tables.page_links +"  (",
                "	page_from integer,",
                "	group_from integer,",
                "	page_to integer,",
                "	group_to integer,",
                "	sign char(120))",
                "WITHOUT OIDS;",
                 string.Empty,
                //s_roles
                "--роли",
                "CREATE TABLE IF NOT EXISTS " + Tables.s_roles +" (",
                "nzp_role serial NOT NULL,",
                "role character(120),",
                "page_url integer DEFAULT 0,",
                "sort integer DEFAULT 0)",
                "WITHOUT OIDS;",
                "DROP INDEX if exists ix_srls_1;",
                "CREATE UNIQUE INDEX ix_srls_1 ON " + Tables.s_roles +" USING btree (nzp_role);",
                "DROP INDEX IF EXISTS ix_srlc_2;",
                "CREATE INDEX ix_srlc_2 ON " + Tables.s_roles +" USING btree  (nzp_role, page_url);",
                string.Empty,
                // 3.actions_lnk
                "--связь действий",
                "CREATE TABLE IF NOT EXISTS " + Tables.actions_lnk +" (",
                "nzp_al serial NOT NULL,",
                "cur_page integer NOT NULL,",
                "nzp_act integer NOT NULL DEFAULT 0,",
                "page_url integer NOT NULL DEFAULT 0)",
                "WITHOUT OIDS;",
                "DROP INDEX IF EXISTS ix_actl_1;",
                "CREATE UNIQUE INDEX ix_actl_1 on " + Tables.actions_lnk +"(nzp_al);",
                "DROP INDEX IF EXISTS ix_actl_2;",
                "CREATE INDEX ix_actl_2 on " + Tables.actions_lnk +" USING btree (cur_page, nzp_act);",
                string.Empty,
                // 4.actions_show
                "--действия со страницами",
                "CREATE TABLE IF NOT EXISTS " + Tables.actions_show +"(",
                "nzp_ash serial NOT NULL,",
                "cur_page integer NOT NULL,",
                "nzp_act integer NOT NULL DEFAULT 0,",
                "act_tip integer NOT NULL DEFAULT 0,",
                "act_dd integer NOT NULL DEFAULT 0,",
                "sort_kod integer NOT NULL DEFAULT 0,",
                "sign char(120)," ,
                "CONSTRAINT fk_actl_01 FOREIGN KEY (nzp_act)",
                "REFERENCES " + Tables.s_actions +"(nzp_act) MATCH SIMPLE",
                "ON UPDATE NO ACTION ON DELETE NO ACTION)",
                "WITHOUT OIDS;",
                "DROP INDEX IF EXISTS ix_actsh_1;",
                "CREATE UNIQUE INDEX ix_actsh_1 on " + Tables.actions_show +"(nzp_ash);",
                "DROP INDEX IF EXISTS ix_actsh_2;",
                "CREATE UNIQUE INDEX ix_actsh_2 ON " + Tables.actions_show +" USING btree (cur_page, nzp_act, act_tip, act_dd);",
               // "ALTER TABLE actions_show DROP  CONSTRAINT fk_actl_02;",
                string.Empty,
                // 5.img lnk
                "--изображения",
                "CREATE TABLE IF NOT EXISTS " + Tables.img_lnk +"(",
                "nzp_img serial NOT NULL,",
                "cur_page integer NOT NULL,",
                "tip integer NOT NULL,",
                "kod integer NOT NULL," +
                "img_url char(255))",
                "WITHOUT OIDS;",
                "DROP INDEX IF EXISTS ix_img_lnk_1;",
                "CREATE UNIQUE INDEX ix_img_lnk_1 on " + Tables.img_lnk +"(nzp_img);",
                "DROP INDEX IF EXISTS ix_img_lnk_2;",
                "CREATE UNIQUE INDEX ix_img_lnk_2 ON " + Tables.img_lnk +" USING btree (cur_page, tip, kod);",
                string.Empty,
                // 6.pages_show
                "--" + Tables.pages_show,
                "CREATE TABLE IF NOT EXISTS " + Tables.pages_show +"(",
                "nzp_psh serial NOT NULL,",
                "cur_page integer NOT NULL,",
                "page_url integer NOT NULL DEFAULT 0,",
                "up_kod integer NOT NULL DEFAULT 0,",
                "sort_kod integer NOT NULL DEFAULT 0,",
                "sign char(255))",
                "WITHOUT OIDS;",
                "DROP INDEX IF EXISTS ix_pagesh_1;",
                "CREATE UNIQUE INDEX ix_pagesh_1 on " + Tables.pages_show +"(nzp_psh);",
                "DROP INDEX IF EXISTS ix_pagesh_2;",
                "CREATE UNIQUE INDEX ix_pagesh_2 ON " + Tables.pages_show +" USING btree (cur_page, page_url, up_kod);",
                string.Empty,
                // 7.roleskey
                "--ключи ролей",
                "CREATE TABLE IF NOT EXISTS " + Tables.roleskey +"(",
                "nzp_rlsv serial NOT NULL,",
                "nzp_role integer NOT NULL,",
                "tip integer NOT NULL,",
                "kod integer NOT NULL,",
                "sign char(90))",
                "WITHOUT OIDS;",
                "DROP INDEX IF EXISTS ix_rlsk_1;",
                "CREATE UNIQUE INDEX ix_rlsk_1 on " + Tables.roleskey +"(nzp_rlsv);",
                "DROP INDEX IF EXISTS ix_rlsk_2;",
                "CREATE UNIQUE INDEX ix_rlsk_2 on " + Tables.roleskey +" USING btree(nzp_role, tip, kod);",
                string.Empty,
                // 8.s_actions
                "--s_actions",
                "CREATE TABLE IF NOT EXISTS " + Tables.s_actions +"(",
                "nzp_act serial NOT NULL,",
                "act_name char(200) NOT NULL,",
                "hlp char (255)," +
                "CONSTRAINT pk_actions PRIMARY KEY (nzp_act))",
                "WITHOUT OIDS;",
                "DROP INDEX IF EXISTS ix_sact_1;",
                "CREATE UNIQUE INDEX ix_sact_1 on " + Tables.s_actions +"(nzp_act);",
                string.Empty,
                // 9.role_pages
                "--страницы роли",
                "CREATE TABLE IF NOT EXISTS " + Tables.role_pages +" (",
                "	id serial NOT NULL,",
                "	nzp_role integer NOT NULL,",
                "	nzp_page integer NOT NULL,",
                "	sign char(120))",
                "WITHOUT OIDS;",
                "DROP INDEX IF EXISTS ix_role_pages_1;",
                "CREATE UNIQUE INDEX ix_role_pages_1 on " + Tables.role_pages +"(id);",
                "DROP INDEX IF EXISTS ix_role_pages_2;",
                "CREATE UNIQUE INDEX ix_role_pages_2 on " + Tables.role_pages +"(nzp_role, nzp_page);",
                string.Empty,
                // 10.role_actions
                "--действия роли",
                "create table if not exists " + Tables.role_actions +" (",
                "	id serial NOT NULL,",
                "	nzp_role integer NOT NULL,",
                "	nzp_page integer NOT NULL,",
                "	nzp_act integer NOT NULL,",
                "	mod_act integer,",
                "	sign char(120))",
                "WITHOUT OIDS;",
                "drop index if exists ix_role_actions_1;",
                "create unique index ix_role_actions_1 on " + Tables.role_actions +"(id);",
                "drop index if exists ix_role_actions_2;",
                "create unique index ix_role_actions_2 on " + Tables.role_actions +"(nzp_role, nzp_page, nzp_act);",
                string.Empty,
                // 11.report
                "--Создание табл. Report (отчеты)",
                "CREATE TABLE IF NOT EXISTS " + Tables.report +" (",
                "	nzp_rep serial NOT NULL,",
                "	nzp_act int,",
                "	name varchar(255),",
                "	file_name varchar(255))",
                "WITHOUT OIDS;",
                "DROP INDEX IF EXISTS ix_report_1;",
                "CREATE UNIQUE INDEX ix_report_1 on " + Tables.report +"(nzp_rep);",
                string.Empty,
                // очистка всех таблиц
                "--предварительное удаление страниц, ссылок и т.п. ",
                
                "DELETE FROM " + Tables.page_links +" WHERE 1=1;",
                "DELETE FROM " + Tables.actions_lnk +" WHERE 1=1;",
                "DELETE FROM " + Tables.actions_show +" WHERE 1=1;",
                "DELETE FROM " + Tables.img_lnk +" WHERE 1=1;",
                "DELETE FROM " + Tables.pages_show +" WHERE 1=1;",
                "DELETE FROM " + Tables.roleskey +" WHERE 1=1;",
                "DELETE FROM " + Tables.role_pages +" WHERE 1=1;",
                "DELETE FROM " + Tables.role_actions +" WHERE 1=1;",
                "DELETE FROM " + Tables.report +" WHERE 1=1;",
                "DELETE FROM " + Tables.s_roles +" WHERE nzp_role >= 10 and nzp_role < 1000;",
                "DELETE FROM " + Tables.s_actions +" WHERE 1=1;",
                "DELETE FROM " + Tables.pages +" WHERE 1=1;",

               // "delete from s_help WHERE 1=1;",
                string.Empty
            };
            AddToLinesList(list);
        }

     
    }
    }

