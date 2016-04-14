SET SEARCH_PATH TO public;

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

INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (15, 123, 5, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (15, 123, 610, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (15, 123, 701, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (15, 123, 702, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (15, 123, 703, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (15, 123, 601, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (15, 123, 602, null);

DELETE FROM role_actions WHERE nzp_role=15 and nzp_page=122 and nzp_act=5;
DELETE FROM role_actions WHERE nzp_role=15 and nzp_page=122 and nzp_act=70;
DELETE FROM role_actions WHERE nzp_role=15 and nzp_page=122 and nzp_act=520;
DELETE FROM role_actions WHERE nzp_role=15 and nzp_page=122 and nzp_act=521;
DELETE FROM role_actions WHERE nzp_role=15 and nzp_page=122 and nzp_act=522;
DELETE FROM role_actions WHERE nzp_role=15 and nzp_page=122 and nzp_act=523;
DELETE FROM role_actions WHERE nzp_role=15 and nzp_page=122 and nzp_act=528;
DELETE FROM role_actions WHERE nzp_role=15 and nzp_page=122 and nzp_act=922;
DELETE FROM role_actions WHERE nzp_role=15 and nzp_page=122 and nzp_act=925;

INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (15, 123);

DELETE FROM role_pages WHERE nzp_role=15 and nzp_page=122;

DROP TRIGGER role_actions_add on role_actions; 
DROP FUNCTION add_to_role_actions(); 
DROP TRIGGER role_pages_add on role_pages; 
DROP FUNCTION add_to_role_pages(); 

update role_actions set sign = nzp_role::varchar||nzp_page::varchar||nzp_act::varchar||'-'||id::varchar||'role_actions' 
where nzp_role >= 10 and nzp_role < 1000; 
update role_pages set sign = nzp_role::varchar||nzp_page::varchar||'-'||id::varchar||'role_pages' 
where nzp_role >= 10 and nzp_role < 1000; 
