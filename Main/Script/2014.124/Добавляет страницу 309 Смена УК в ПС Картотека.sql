SET SEARCH_PATH TO public;

-- Триггер(ы). Необходимы для того, чтобы избежать ограничения внешнего ключа  
CREATE OR REPLACE FUNCTION add_to_pages() RETURNS trigger LANGUAGE plpgsql AS $body$ 
BEGIN 
if exists(select * from public.pages where nzp_page=NEW.nzp_page) then 
raise NOTICE 'row pages ignored: nzp_page=%, page_name=%, page_url=%', NEW.nzp_page, NEW.page_name, NEW.page_url; 
return NULL; 
end if; 
return NEW; 
END; 
$body$; 
CREATE TRIGGER pages_add BEFORE INSERT 
ON pages FOR EACH ROW 
EXECUTE PROCEDURE add_to_pages() ; 

CREATE OR REPLACE FUNCTION add_to_actions_show() RETURNS trigger LANGUAGE plpgsql AS $body$ 
BEGIN 
if exists(select * from public.actions_show where cur_page=NEW.cur_page and nzp_act=NEW.nzp_act) then 
raise NOTICE 'row actions_show ignored: cur_page=%, nzp_act=%', NEW.cur_page, NEW.nzp_act; 
return NULL; 
end if; 
return NEW; 
END; 
$body$; 
CREATE TRIGGER actions_show_add BEFORE INSERT 
ON actions_show FOR EACH ROW 
EXECUTE PROCEDURE add_to_actions_show(); 

CREATE OR REPLACE FUNCTION add_to_actions_lnk() RETURNS trigger LANGUAGE plpgsql AS $body$ 
BEGIN 
if exists(select * from public.actions_lnk where cur_page=NEW.cur_page and nzp_act=NEW.nzp_act) then  
raise NOTICE 'row actions_lnk ignored: cur_page=%, nzp_act=%', NEW.cur_page, NEW.nzp_act; 
return NULL; 
end if; 
return NEW; 
END; 
$body$; 
CREATE TRIGGER actions_lnk_add BEFORE INSERT 
ON actions_lnk FOR EACH ROW 
EXECUTE PROCEDURE add_to_actions_lnk(); 

CREATE OR REPLACE FUNCTION add_to_img_lnk() RETURNS trigger LANGUAGE plpgsql AS $body$ 
BEGIN 
if exists(select * from public.img_lnk where cur_page=NEW.cur_page and tip=NEW.tip and kod= NEW.kod) then 
raise NOTICE 'row img_lnk ignored: cur_page=%, tip=%, kod=%', NEW.cur_page, NEW.tip, NEW.kod; 
return NULL; 
end if; 
return NEW;
END; 
$body$; 
CREATE TRIGGER img_lnk_add BEFORE INSERT 
ON img_lnk FOR EACH ROW 
EXECUTE PROCEDURE add_to_img_lnk(); 

CREATE OR REPLACE FUNCTION add_to_s_actions() RETURNS trigger LANGUAGE plpgsql AS $body$ 
BEGIN 
if exists(select * from public.s_actions where nzp_act=NEW.nzp_act) then 
raise NOTICE 'row s_actions ignored: nzp_act=%, act_name=%, hlp=%', NEW.nzp_act, NEW.act_name, NEW.hlp; 
return NULL; 
end if; 
return NEW; 
END; 
$body$; 
CREATE TRIGGER s_actions_add BEFORE INSERT 
ON s_actions FOR EACH ROW 
EXECUTE PROCEDURE add_to_s_actions(); 

CREATE OR REPLACE FUNCTION add_to_s_roles() RETURNS trigger LANGUAGE plpgsql AS $body$ 
BEGIN 
if exists(select * from public.s_roles where nzp_role=NEW.nzp_role) then 
 raise NOTICE 'row s_roles ignored: nzp_role=%, role=%', NEW.nzp_role, NEW.role; 
return NULL; 
end if; 
return NEW; 
END; 
$body$; 
CREATE TRIGGER s_roles_add BEFORE INSERT 
ON s_roles FOR EACH ROW 
EXECUTE PROCEDURE add_to_s_roles(); 

CREATE OR REPLACE FUNCTION add_to_role_actions() RETURNS trigger LANGUAGE plpgsql AS $body$ 
BEGIN 
if exists(select * from public.role_actions where nzp_role=NEW.nzp_role and nzp_page=NEW.nzp_page and nzp_act=NEW.nzp_act) then 
raise NOTICE 'row role_actions ignored: nzp_role=%, nzp_page=%, nzp_act=%', NEW.nzp_role, NEW.nzp_page, NEW.nzp_act; 
return NULL; 
end if; 
return NEW; 
END; 
$body$; 
CREATE TRIGGER role_actions_add BEFORE INSERT 
ON role_actions FOR EACH ROW 
EXECUTE PROCEDURE add_to_role_actions(); 

CREATE OR REPLACE FUNCTION add_to_role_pages() RETURNS trigger LANGUAGE plpgsql AS $body$ 
BEGIN 
if exists(select * from public.role_pages where nzp_role=NEW.nzp_role and nzp_page=NEW.nzp_page) then 
 raise NOTICE 'row role_pages ignored: nzp_role=%, nzp_page=%', NEW.nzp_role, NEW.nzp_page; 
return NULL; 
end if; 
return NEW; 
END; 
$body$; 
CREATE TRIGGER role_pages_add BEFORE INSERT 
ON role_pages FOR EACH ROW 
EXECUTE PROCEDURE add_to_role_pages(); 

CREATE OR REPLACE FUNCTION add_to_roleskey() RETURNS trigger LANGUAGE plpgsql AS $body$ 
BEGIN 
if exists(select * from public.roleskey where nzp_role=NEW.nzp_role and tip=NEW.tip and kod=NEW.kod) then 
raise NOTICE 'row roleskey ignored: nzp_role=%, tip=%, kod=%', NEW.nzp_role, NEW.tip, NEW.kod; 
return NULL; 
end if; 
return NEW; 
END; 
$body$; 
CREATE TRIGGER roleskey_add BEFORE INSERT 
ON roleskey FOR EACH ROW 
EXECUTE PROCEDURE add_to_roleskey();  

INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (309, '~/kart/adres/changearea.aspx', 'Смена УК', 'Смена управляющей компании', 'Отображает форму для смены управляющей компании по списку выбранных домов', 74, 42);

INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES  (184, 'Перенести', 'Переносит все лицевые счета выбранных домов в новую УК');

INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (309, 184, 184, 0, 0);

INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (309, 184, 309);

INSERT INTO pages_show (cur_page,page_url,up_kod,sort_kod)
SELECT DISTINCT a.nzp_page, b.nzp_page, COALESCE(b.up_kod,0), b.nzp_page
FROM pages a, pages b, page_links  pl
WHERE (pl.page_from = a.nzp_page or pl.group_from = a.group_id or (pl.page_from is null and pl.group_from is null))
and (pl.page_to = b.nzp_page or pl.group_to = b.group_id)
and (select count(*) from pages_show ps where ps.cur_page = a.nzp_page and ps.page_url = b.nzp_page) = 0;

--добавить недостающие пункты меню, у которых есть подменю
CREATE temp table ps (cur_page integer, up_kod integer);
insert into ps select distinct cur_page, up_kod from pages_show a where up_kod > 0 and not exists (select 1 from pages_show where cur_page = a.cur_page and page_url = a.up_kod);
insert into pages_show (cur_page, page_url, up_kod, sort_kod) select cur_page, ps.up_kod, 0, 0 from ps, pages Where ps.up_kod=pages.nzp_page;
drop table ps;

INSERT INTO img_lnk (cur_page, tip, kod, img_url) VALUES  (0, 2, 184, 'play.png');

INSERT INTO s_roles (nzp_role, role, page_url, sort) VALUES  (994, 'Смена управляющей организации', 0, 994);

INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (994, 309, 184, null);

INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (994, 309);

INSERT INTO roleskey (nzp_role, tip, kod) VALUES  (10, 105, 994);

DROP TRIGGER pages_add on pages; 
DROP FUNCTION add_to_pages();
DROP TRIGGER actions_show_add on actions_show; 
DROP FUNCTION add_to_actions_show(); 
DROP TRIGGER actions_lnk_add on actions_lnk; 
DROP FUNCTION add_to_actions_lnk(); 
DROP TRIGGER img_lnk_add on img_lnk; 
DROP FUNCTION add_to_img_lnk(); 
DROP TRIGGER s_actions_add on s_actions; 
DROP FUNCTION add_to_s_actions(); 
DROP TRIGGER s_roles_add on s_roles; 
DROP FUNCTION add_to_s_roles(); 
DROP TRIGGER role_actions_add on role_actions; 
DROP FUNCTION add_to_role_actions(); 
DROP TRIGGER role_pages_add on role_pages; 
DROP FUNCTION add_to_role_pages(); 
DROP TRIGGER roleskey_add on roleskey; 
DROP FUNCTION add_to_roleskey(); 

update actions_show set sign = sort_kod::varchar||act_dd::varchar||act_tip::varchar||nzp_act::varchar||cur_page||'-'||nzp_ash||'actions_show'; 
update role_actions set sign = nzp_role::varchar||nzp_page::varchar||nzp_act::varchar||'-'||id::varchar||'role_actions' 
where nzp_role >= 10 and nzp_role < 1000; 
update roleskey set sign = tip::varchar||kod::varchar||nzp_role::varchar||'-'||nzp_rlsv::varchar||'roles' 
where (nzp_role >= 10 and nzp_role < 1000) or (tip = 105 and kod >= 10 and kod < 1000); 
update role_pages set sign = nzp_role::varchar||nzp_page::varchar||'-'||id::varchar||'role_pages' 
where nzp_role >= 10 and nzp_role < 1000; 
update pages_show set sign = sort_kod::varchar||up_kod::varchar||page_url||cur_page||'-'||nzp_psh||'pages_show';
