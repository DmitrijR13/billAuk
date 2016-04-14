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

INSERT INTO actions_show (sort_kod, act_tip, act_dd, cur_page, nzp_act) VALUES  (18, 0, 0, 55, 18);
INSERT INTO actions_show (sort_kod, act_tip, act_dd, cur_page, nzp_act) VALUES  (19, 0, 0, 55, 19);

INSERT INTO actions_lnk (page_url, cur_page, nzp_act) VALUES  (55, 55, 18);
INSERT INTO actions_lnk (page_url, cur_page, nzp_act) VALUES  (55, 55, 19);

INSERT INTO pages_show (cur_page,page_url,up_kod,sort_kod)
SELECT DISTINCT a.nzp_page, b.nzp_page, COALESCE(b.up_kod,0), b.nzp_page
FROM pages a, pages b, page_links  pl
WHERE (pl.page_from = a.nzp_page or pl.group_from = a.group_id or (pl.page_from is null and pl.group_from is null))
and (pl.page_to = b.nzp_page or pl.group_to = b.group_id)
and not exists (select 1 from pages_show ps where ps.cur_page = a.nzp_page and ps.page_url = b.nzp_page);

--добавить недостающие пункты меню, у которых есть подменю
CREATE temp table ps (cur_page integer, up_kod integer);
insert into ps select distinct cur_page, up_kod from pages_show a where up_kod > 0 and not exists (select 1 from pages_show where cur_page = a.cur_page and page_url = a.up_kod);
insert into pages_show (cur_page, page_url, up_kod, sort_kod) select cur_page, ps.up_kod, 0, 0 from ps, pages Where ps.up_kod=pages.nzp_page;
drop table ps;

--Обновление кодов сортировки для корректного отображения пунктов меню
UPDATE pages_show SET sort_kod =3, up_kod = 0 WHERE page_url = 70;
UPDATE pages_show SET sort_kod =4, up_kod = 0 WHERE page_url = 254;
UPDATE pages_show SET sort_kod =5, up_kod = 0 WHERE page_url = 71;
UPDATE pages_show SET sort_kod =6, up_kod = 0 WHERE page_url = 235;
UPDATE pages_show SET sort_kod =7, up_kod = 0 WHERE page_url = 236;
UPDATE pages_show SET sort_kod =8, up_kod = 0 WHERE page_url = 275;
UPDATE pages_show SET sort_kod =9, up_kod = 0 WHERE page_url = 276;
UPDATE pages_show SET sort_kod =10, up_kod = 0 WHERE page_url = 78;
UPDATE pages_show SET sort_kod =11, up_kod = 0 WHERE page_url = 79;
UPDATE pages_show SET sort_kod =12, up_kod = 0 WHERE page_url = 80;
UPDATE pages_show SET sort_kod =13, up_kod = 0 WHERE page_url = 76;
UPDATE pages_show SET sort_kod =14, up_kod = 0 WHERE page_url = 74;
UPDATE pages_show SET sort_kod =15, up_kod = 0 WHERE page_url = 77;
UPDATE pages_show SET sort_kod =16, up_kod = 0 WHERE page_url = 75;
UPDATE pages_show SET sort_kod =17, up_kod = 0 WHERE page_url = 40;
UPDATE pages_show SET sort_kod =18, up_kod = 0 WHERE page_url = 30;
UPDATE pages_show SET sort_kod =19, up_kod = 0 WHERE page_url = 291;
UPDATE pages_show SET sort_kod =20, up_kod = 0 WHERE page_url = 150;
UPDATE pages_show SET sort_kod =21, up_kod = 0 WHERE page_url = 160;
UPDATE pages_show SET sort_kod =22, up_kod = 0 WHERE page_url = 357;
UPDATE pages_show SET sort_kod =23, up_kod = 0 WHERE page_url = 73;
UPDATE pages_show SET sort_kod =24, up_kod = 0 WHERE page_url = 72;

UPDATE pages_show SET sort_kod =1 WHERE page_url = 126;
UPDATE pages_show SET sort_kod =1 WHERE page_url = 205;
UPDATE pages_show SET sort_kod =1 WHERE page_url = 108;
UPDATE pages_show SET sort_kod =1 WHERE page_url = 41;
UPDATE pages_show SET sort_kod =1 WHERE page_url = 362;
UPDATE pages_show SET sort_kod =1 WHERE page_url = 168;
UPDATE pages_show SET sort_kod =1 WHERE page_url = 111;
UPDATE pages_show SET sort_kod =2 WHERE page_url = 50;
UPDATE pages_show SET sort_kod =2 WHERE page_url = 193;
UPDATE pages_show SET sort_kod =2 WHERE page_url = 170;
UPDATE pages_show SET sort_kod =2 WHERE page_url = 109;
UPDATE pages_show SET sort_kod =3 WHERE page_url = 42;
UPDATE pages_show SET sort_kod =3 WHERE page_url = 117;
UPDATE pages_show SET sort_kod =3 WHERE page_url = 49;
UPDATE pages_show SET sort_kod =3 WHERE page_url = 104;
UPDATE pages_show SET sort_kod =3 WHERE page_url = 95;
UPDATE pages_show SET sort_kod =3 WHERE page_url = 190;
UPDATE pages_show SET sort_kod =4 WHERE page_url = 256;
UPDATE pages_show SET sort_kod =4 WHERE page_url = 118;
UPDATE pages_show SET sort_kod =4 WHERE page_url = 53;
UPDATE pages_show SET sort_kod =4 WHERE page_url = 182;
UPDATE pages_show SET sort_kod =5 WHERE page_url = 56;
UPDATE pages_show SET sort_kod =5 WHERE page_url = 100;
UPDATE pages_show SET sort_kod =5 WHERE page_url = 355;
UPDATE pages_show SET sort_kod =6 WHERE page_url = 197;
UPDATE pages_show SET sort_kod =6 WHERE page_url = 352;
UPDATE pages_show SET sort_kod =6 WHERE page_url = 102;
UPDATE pages_show SET sort_kod =7 WHERE page_url = 121;
UPDATE pages_show SET sort_kod =7 WHERE page_url = 60;
UPDATE pages_show SET sort_kod =7 WHERE page_url = 99;
UPDATE pages_show SET sort_kod =7 WHERE page_url = 180;
UPDATE pages_show SET sort_kod =8 WHERE page_url = 110;
UPDATE pages_show SET sort_kod =8 WHERE page_url = 65;
UPDATE pages_show SET sort_kod =8 WHERE page_url = 189;
UPDATE pages_show SET sort_kod =9 WHERE page_url = 179;
UPDATE pages_show SET sort_kod =9 WHERE page_url = 238;
UPDATE pages_show SET sort_kod =10 WHERE page_url = 319;
UPDATE pages_show SET sort_kod =10 WHERE page_url = 196;
UPDATE pages_show SET sort_kod =11 WHERE page_url = 354;
UPDATE pages_show SET sort_kod =11 WHERE page_url = 241;
UPDATE pages_show SET sort_kod =12 WHERE page_url = 7;
UPDATE pages_show SET sort_kod =12 WHERE page_url = 263;
UPDATE pages_show SET sort_kod =12 WHERE page_url = 124;
UPDATE pages_show SET sort_kod =13 WHERE page_url = 98;
UPDATE pages_show SET sort_kod =13 WHERE page_url = 142;
UPDATE pages_show SET sort_kod =13 WHERE page_url = 264;
UPDATE pages_show SET sort_kod =14 WHERE page_url = 281;
UPDATE pages_show SET sort_kod =14 WHERE page_url = 184;
UPDATE pages_show SET sort_kod =16 WHERE page_url = 57;
UPDATE pages_show SET sort_kod =19 WHERE page_url = 59;
UPDATE pages_show SET sort_kod =19 WHERE page_url = 116;
UPDATE pages_show SET sort_kod =25 WHERE page_url = 240;
UPDATE pages_show SET sort_kod =25 WHERE page_url = 192;
UPDATE pages_show SET sort_kod =30 WHERE page_url = 201;
UPDATE pages_show SET sort_kod =31 WHERE page_url = 185;
UPDATE pages_show SET sort_kod =31 WHERE page_url = 51;
UPDATE pages_show SET sort_kod =37 WHERE page_url = 92;
UPDATE pages_show SET sort_kod =37 WHERE page_url = 54;
UPDATE pages_show SET sort_kod =43 WHERE page_url = 66;
UPDATE pages_show SET sort_kod =43 WHERE page_url = 63;
UPDATE pages_show SET sort_kod =44 WHERE page_url = 371;
UPDATE pages_show SET sort_kod =49 WHERE page_url = 62;
UPDATE pages_show SET sort_kod =49 WHERE page_url = 186;
UPDATE pages_show SET sort_kod =50 WHERE page_url = 372;
UPDATE pages_show SET sort_kod =51 WHERE page_url = 373;
UPDATE pages_show SET sort_kod =55 WHERE page_url = 67;
UPDATE pages_show SET sort_kod =55 WHERE page_url = 162;
UPDATE pages_show SET sort_kod =56 WHERE page_url = 177;
UPDATE pages_show SET sort_kod =61 WHERE page_url = 55;
UPDATE pages_show SET sort_kod =62 WHERE page_url = 61;
UPDATE pages_show SET sort_kod =67 WHERE page_url = 191;
UPDATE pages_show SET sort_kod =67 WHERE page_url = 165;
UPDATE pages_show SET sort_kod =73 WHERE page_url = 262;
UPDATE pages_show SET sort_kod =73 WHERE page_url = 167;
UPDATE pages_show SET sort_kod =76 WHERE page_url = 158;
UPDATE pages_show SET sort_kod =76 WHERE page_url = 367;
UPDATE pages_show SET sort_kod =79 WHERE page_url = 347;
UPDATE pages_show SET sort_kod =79 WHERE page_url = 122;
UPDATE pages_show SET sort_kod =81 WHERE page_url = 159;
UPDATE pages_show SET sort_kod =85 WHERE page_url = 166;
UPDATE pages_show SET sort_kod =85 WHERE page_url = 137;
UPDATE pages_show SET sort_kod =91 WHERE page_url = 146;
UPDATE pages_show SET sort_kod =97 WHERE page_url = 296;
UPDATE pages_show SET sort_kod =103 WHERE page_url = 244;
UPDATE pages_show SET sort_kod =109 WHERE page_url = 123;
UPDATE pages_show SET sort_kod =115 WHERE page_url = 131;
UPDATE pages_show SET sort_kod =121 WHERE page_url = 85;
UPDATE pages_show SET sort_kod =127 WHERE page_url = 88;
UPDATE pages_show SET sort_kod =133 WHERE page_url = 97;
UPDATE pages_show SET sort_kod =139 WHERE page_url = 344;
UPDATE pages_show SET sort_kod =145 WHERE page_url = 345;
UPDATE pages_show SET sort_kod =151 WHERE page_url = 133;
UPDATE pages_show SET sort_kod =321 WHERE page_url = 9;
UPDATE pages_show SET sort_kod =338 WHERE page_url = 13;

INSERT INTO role_actions (mod_act, nzp_role, nzp_page, nzp_act) VALUES  (611, 901, 55, 18);
INSERT INTO role_actions (mod_act, nzp_role, nzp_page, nzp_act) VALUES  (611, 901, 55, 19);

DROP TRIGGER actions_show_add on actions_show; 
DROP FUNCTION add_to_actions_show(); 
DROP TRIGGER actions_lnk_add on actions_lnk; 
DROP FUNCTION add_to_actions_lnk(); 
DROP TRIGGER role_actions_add on role_actions; 
DROP FUNCTION add_to_role_actions(); 

update actions_show set sign = sort_kod::varchar||act_dd::varchar||act_tip::varchar||nzp_act::varchar||cur_page||'-'||nzp_ash||'actions_show'; 
update role_actions set sign = nzp_role::varchar||nzp_page::varchar||nzp_act::varchar||'-'||id::varchar||'role_actions' 
where nzp_role >= 10 and nzp_role < 1000; 
