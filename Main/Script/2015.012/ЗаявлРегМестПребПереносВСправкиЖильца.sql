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

INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (135, 214, 39, 2, 5);

DELETE FROM actions_show WHERE cur_page=133 and nzp_act=214;

DROP TRIGGER actions_show_add on actions_show; 
DROP FUNCTION add_to_actions_show(); 

-- Сортирует отчеты по имени
WITH acs as (select distinct a.nzp_ash, a.sort_kod, a.cur_page, a.nzp_act as a_nzp_act, act_tip, act_dd, s.nzp_act as sact_nap_act, s.act_name as name_rep 
FROM actions_show a Left outer join s_actions s on (a.nzp_act=s.nzp_act) 
WHERE (act_tip=2 and act_dd=5 and  (cur_page=135 OR cur_page=133))), 
ord as (select *,(select count(1) from  acs ab where ab.name_rep <=acs.name_rep) as row_num FROM  acs order by row_num) 
UPDATE actions_show ah set sort_kod=ord.row_num from ord where ord.nzp_ash=ah.nzp_ash; 

update actions_show set sign = sort_kod::varchar||act_dd::varchar||act_tip::varchar||nzp_act::varchar||cur_page||'-'||nzp_ash||'actions_show'; 
