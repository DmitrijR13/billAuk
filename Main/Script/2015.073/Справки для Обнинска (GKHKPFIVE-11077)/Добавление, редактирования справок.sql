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

CREATE OR REPLACE FUNCTION add_to_report() RETURNS trigger LANGUAGE plpgsql AS $body$ 
BEGIN 
if exists(select * from public.report where nzp_act=NEW.nzp_act ) then  
raise NOTICE 'row report ignored: nzp_act=%, name=%', NEW.nzp_act, NEW.name; 
return NULL; 
end if; 
return NEW; 
END; 
$body$; 
CREATE TRIGGER report_add BEFORE INSERT 
ON report FOR EACH ROW 
EXECUTE PROCEDURE add_to_report(); 
--Карточка прописки

INSERT INTO s_actions (act_name, hlp, nzp_act) VALUES  ('Карточка прописки', 'Карточка прописки', 953);

INSERT INTO actions_show (sort_kod, act_tip, act_dd, cur_page, nzp_act) VALUES  (52, 2, 5, 135, 953);

INSERT INTO role_actions (mod_act, nzp_role, nzp_page, nzp_act) VALUES  (null, 14, 135, 953);

INSERT INTO report (name, file_name, nzp_act) VALUES  ('Карточка прописки', 'web_kart_propiski.frx', 953);

--Листок статистического учета выбытия
INSERT INTO s_actions (act_name, hlp, nzp_act) VALUES  ('40 Листок статистического учета выбытия', 'Листок статистического учета выбытия', 936);

INSERT INTO actions_show (sort_kod, act_tip, act_dd, cur_page, nzp_act) VALUES  (12, 2, 5, 135, 936);

INSERT INTO role_actions (mod_act, nzp_role, nzp_page, nzp_act) VALUES  (null, 14, 135, 936);

INSERT INTO report (name, file_name, nzp_act) VALUES  ('40 Листок статистического учета выбытия', 'Web_listok_stat_ubit_obn.frx', 936);

--40 Листок статистического учета выбытия
INSERT INTO s_actions (act_name, hlp, nzp_act) VALUES  ('40 Листок статистического учета прибытия', 'Листок статистического учета прибытия', 937);

INSERT INTO actions_show (sort_kod, act_tip, act_dd, cur_page, nzp_act) VALUES  (13, 2, 5, 135, 937);


INSERT INTO role_actions (mod_act, nzp_role, nzp_page, nzp_act) VALUES  (null, 14, 135, 937);

INSERT INTO report (name, file_name, nzp_act) VALUES  ('40 Листок статистического учета прибытия', 'Web_listok_stat_prib_obn.frx', 937);

--Отрывной талон к листку статистического учета выбытия
UPDATE s_actions Set act_name='Отрывной талон к листку статистического учета выбытия', hlp='Отрывной талон к листку статистического учета выбытия' WHERE nzp_act=895;

--Сведения о регистрации иностранного гражданина по месту жительства на территории РФ (РФЛ2)
INSERT INTO s_actions (act_name, hlp, nzp_act) VALUES  ('Сведения о регистрации иностранного гражданина по месту жительства на территории РФ (РФЛ2)', 'Сведения о регистрации иностранного гражданина по месту жительства на территории РФ (РФЛ2)', 954);

INSERT INTO actions_show (sort_kod, act_tip, act_dd, cur_page, nzp_act) VALUES  (64, 2, 5, 135, 954);

INSERT INTO role_actions (mod_act, nzp_role, nzp_page, nzp_act) VALUES  (null, 14, 135, 954);

INSERT INTO report (name, file_name, nzp_act) VALUES  ('Сведения о регистрации иностранного гражданина по месту жительства на территории РФ (РФЛ2)', 'web_rfl2.frx', 954);



DROP TRIGGER actions_show_add on actions_show; 
DROP FUNCTION add_to_actions_show(); 
DROP TRIGGER s_actions_add on s_actions; 
DROP FUNCTION add_to_s_actions(); 
DROP TRIGGER role_actions_add on role_actions; 
DROP FUNCTION add_to_role_actions(); 
DROP TRIGGER report_add on report; 
DROP FUNCTION add_to_report(); 

-- Сортирует отчеты по имени
WITH acs as (select distinct a.nzp_ash, a.sort_kod, a.cur_page, a.nzp_act as a_nzp_act, act_tip, act_dd, s.nzp_act as sact_nap_act, s.act_name as name_rep 
FROM actions_show a Left outer join s_actions s on (a.nzp_act=s.nzp_act) 
WHERE (act_tip=2 and act_dd=5 and  (cur_page=135 OR cur_page=133))), 
ord as (select *,(select count(1) from  acs ab where ab.name_rep <=acs.name_rep) as row_num FROM  acs order by row_num) 
UPDATE actions_show ah set sort_kod=ord.row_num from ord where ord.nzp_ash=ah.nzp_ash; 

update actions_show set sign = sort_kod::varchar||act_dd::varchar||act_tip::varchar||nzp_act::varchar||cur_page||'-'||nzp_ash||'actions_show'; 
update role_actions set sign = nzp_role::varchar||nzp_page::varchar||nzp_act::varchar||'-'||id::varchar||'role_actions' 
where nzp_role >= 10 and nzp_role < 1000; 
