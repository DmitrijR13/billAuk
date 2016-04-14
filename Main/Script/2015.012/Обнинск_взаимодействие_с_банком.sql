SET SEARCH_PATH TO public;

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

INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES  (921, 'Формат версии 4 (Калужская область) с кодом 86040167', 'Формат версии 4 (Калужская область) с кодом 86040167');

INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (308, 921, 921, 2, 3);

DELETE FROM actions_show WHERE cur_page=308 and nzp_act=899;
DELETE FROM actions_show WHERE cur_page=308 and nzp_act=898;

INSERT INTO s_roles (nzp_role, role, page_url, sort) VALUES  (888, 'Взаимодействие с банком', 0, 888);

INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (888, 308, 158, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (888, 308, 169, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (888, 308, 170, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (888, 308, 921, null);

DELETE FROM role_actions WHERE nzp_role=998 and nzp_page=308 and nzp_act=158;
DELETE FROM role_actions WHERE nzp_role=998 and nzp_page=308 and nzp_act=170;
DELETE FROM role_actions WHERE nzp_role=998 and nzp_page=308 and nzp_act=169;
DELETE FROM role_actions WHERE nzp_role=998 and nzp_page=308 and nzp_act=898;
DELETE FROM role_actions WHERE nzp_role=998 and nzp_page=308 and nzp_act=899;

INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (888, 308);

DELETE FROM role_pages WHERE nzp_role=998 and nzp_page=308;

INSERT INTO roleskey (nzp_role, tip, kod) VALUES  (15, 105, 888);

DELETE FROM roleskey WHERE nzp_role=15 and tip=105 and kod=998;

DROP TRIGGER actions_show_add on actions_show; 
DROP FUNCTION add_to_actions_show(); 
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
