SET SEARCH_PATH TO public;
--68,69,93,187
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
--ИПУ
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (68, 158, 158, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (68, 169, 169, 0, 0);
--ДПУ
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (69, 158, 158, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (69, 169, 169, 0, 0);
--ГрПУ
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (93, 158, 158, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (93, 169, 169, 0, 0);
--ОбщКвПУ
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (187, 158, 158, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (187, 169, 169, 0, 0);

--ИПУ
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (68, 158, 68);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (68, 169, 68);
--ДПУ
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (69, 158, 69);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (69, 169, 69);
--ГрПУ
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (93, 158, 68);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (93, 169, 68);
--ОбщКвПУ
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (187, 158, 69);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (187, 169, 69);

--ИПУ
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (903, 68, 158, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (903, 68, 169, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (901, 68, 158, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (901, 68, 169, 611);
--ДПУ
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (903, 69, 158, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (903, 69, 169, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (901, 69, 158, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (901, 69, 169, 611);
--ГрПУ
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (903, 93, 158, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (903, 93, 169, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (901, 93, 158, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (901, 93, 169, 611);
--ОбщКвПУ
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (903, 187, 158, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (903, 187, 169, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (901, 187, 158, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (901, 187, 169, 611);

DROP TRIGGER actions_show_add on actions_show; 
DROP FUNCTION add_to_actions_show(); 
DROP TRIGGER actions_lnk_add on actions_lnk; 
DROP FUNCTION add_to_actions_lnk(); 
DROP TRIGGER role_actions_add on role_actions; 
DROP FUNCTION add_to_role_actions(); 

update actions_show set sign = sort_kod::varchar||act_dd::varchar||act_tip::varchar||nzp_act::varchar||cur_page||'-'||nzp_ash||'actions_show'; 
update role_actions set sign = nzp_role::varchar||nzp_page::varchar||nzp_act::varchar||'-'||id::varchar||'role_actions' 
where nzp_role >= 10 and nzp_role < 1000; 
