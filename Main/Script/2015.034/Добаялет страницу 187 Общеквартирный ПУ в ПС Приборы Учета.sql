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

CREATE OR REPLACE FUNCTION add_to_page_links() RETURNS trigger LANGUAGE plpgsql AS $body$ 
BEGIN 
if exists(select * from public.page_links where (page_from=NEW.page_from OR (page_from is null and NEW.page_from is NULL )) 
and (group_from=NEW.group_from OR (group_from is null and NEW.group_from is NULL )) 
and (page_to=NEW.page_to OR (page_to is null and NEW.page_to is NULL )) 
and (group_to=NEW.group_to OR (group_to is null and NEW.group_to is NULL ))) then 
raise NOTICE 'row page_links ignored: page_from=%, group_from=%, page_to=%, group_to=%', NEW.page_from, NEW.group_from, NEW.page_to, NEW.group_to ; 
return NULL; 
end if; 
return NEW; 
END; 
$body$; 
CREATE TRIGGER page_links_add BEFORE INSERT 
ON page_links FOR EACH ROW 
EXECUTE PROCEDURE add_to_page_links();  

INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (187, '~/kart/counter/countercard.aspx', 'Общеквартирный ПУ', 'Общеквартирный прибор учета', 'переходит в карточку общеквартирного прибора учета', null, null);

INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (187, 14, 14, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (187, 61, 61, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (187, 610, 610, 2, 1);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (187, 611, 611, 2, 1);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (187, 3, 1757, 3, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (187, 4, 1758, 3, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (187, 158, 158, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (187, 169, 169, 0, 0);

INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES  (187, null, null, 70);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES  (187, null, null, 71);

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

INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (13, 187, 3, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (13, 187, 14, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (13, 187, 610, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (901, 187, 4, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (901, 187, 611, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (901, 187, 61, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (901, 187, 158, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (901, 187, 169, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (903, 187, 4, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (903, 187, 611, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (903, 187, 158, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (903, 187, 61, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (903, 187, 169, 611);

INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (13, 187);
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

UPDATE pages_show SET sort_kod =1 WHERE page_url = 362;
UPDATE pages_show SET sort_kod =1 WHERE page_url = 126;
UPDATE pages_show SET sort_kod =1 WHERE page_url = 121;
UPDATE pages_show SET sort_kod =1 WHERE page_url = 168;
UPDATE pages_show SET sort_kod =1 WHERE page_url = 108;
UPDATE pages_show SET sort_kod =1 WHERE page_url = 41;
UPDATE pages_show SET sort_kod =1 WHERE page_url = 205;
UPDATE pages_show SET sort_kod =2 WHERE page_url = 98;
UPDATE pages_show SET sort_kod =2 WHERE page_url = 50;
UPDATE pages_show SET sort_kod =2 WHERE page_url = 193;
UPDATE pages_show SET sort_kod =2 WHERE page_url = 109;
UPDATE pages_show SET sort_kod =2 WHERE page_url = 170;
UPDATE pages_show SET sort_kod =2 WHERE page_url = 99;
UPDATE pages_show SET sort_kod =3 WHERE page_url = 104;
UPDATE pages_show SET sort_kod =3 WHERE page_url = 49;
UPDATE pages_show SET sort_kod =3 WHERE page_url = 42;
UPDATE pages_show SET sort_kod =3 WHERE page_url = 95;
UPDATE pages_show SET sort_kod =3 WHERE page_url = 190;
UPDATE pages_show SET sort_kod =4 WHERE page_url = 53;
UPDATE pages_show SET sort_kod =4 WHERE page_url = 51;
UPDATE pages_show SET sort_kod =4 WHERE page_url = 182;
UPDATE pages_show SET sort_kod =4 WHERE page_url = 256;
UPDATE pages_show SET sort_kod =4 WHERE page_url = 59;
UPDATE pages_show SET sort_kod =5 WHERE page_url = 54;
UPDATE pages_show SET sort_kod =5 WHERE page_url = 100;
UPDATE pages_show SET sort_kod =5 WHERE page_url = 63;
UPDATE pages_show SET sort_kod =5 WHERE page_url = 355;
UPDATE pages_show SET sort_kod =5 WHERE page_url = 56;
UPDATE pages_show SET sort_kod =6 WHERE page_url = 102;
UPDATE pages_show SET sort_kod =6 WHERE page_url = 92;
UPDATE pages_show SET sort_kod =6 WHERE page_url = 66;
UPDATE pages_show SET sort_kod =6 WHERE page_url = 352;
UPDATE pages_show SET sort_kod =6 WHERE page_url = 197;
UPDATE pages_show SET sort_kod =7 WHERE page_url = 180;
UPDATE pages_show SET sort_kod =7 WHERE page_url = 60;
UPDATE pages_show SET sort_kod =7 WHERE page_url = 185;
UPDATE pages_show SET sort_kod =7 WHERE page_url = 189;
UPDATE pages_show SET sort_kod =8 WHERE page_url = 61;
UPDATE pages_show SET sort_kod =8 WHERE page_url = 110;
UPDATE pages_show SET sort_kod =8 WHERE page_url = 238;
UPDATE pages_show SET sort_kod =8 WHERE page_url = 65;
UPDATE pages_show SET sort_kod =9 WHERE page_url = 62;
UPDATE pages_show SET sort_kod =9 WHERE page_url = 67;
UPDATE pages_show SET sort_kod =9 WHERE page_url = 179;
UPDATE pages_show SET sort_kod =9 WHERE page_url = 142;
UPDATE pages_show SET sort_kod =10 WHERE page_url = 319;
UPDATE pages_show SET sort_kod =10 WHERE page_url = 55;
UPDATE pages_show SET sort_kod =10 WHERE page_url = 196;
UPDATE pages_show SET sort_kod =10 WHERE page_url = 186;
UPDATE pages_show SET sort_kod =11 WHERE page_url = 354;
UPDATE pages_show SET sort_kod =11 WHERE page_url = 191;
UPDATE pages_show SET sort_kod =11 WHERE page_url = 165;
UPDATE pages_show SET sort_kod =11 WHERE page_url = 241;
UPDATE pages_show SET sort_kod =12 WHERE page_url = 7;
UPDATE pages_show SET sort_kod =12 WHERE page_url = 124;
UPDATE pages_show SET sort_kod =12 WHERE page_url = 263;
UPDATE pages_show SET sort_kod =13 WHERE page_url = 162;
UPDATE pages_show SET sort_kod =13 WHERE page_url = 264;
UPDATE pages_show SET sort_kod =13 WHERE page_url = 167;
UPDATE pages_show SET sort_kod =14 WHERE page_url = 184;
UPDATE pages_show SET sort_kod =14 WHERE page_url = 122;
UPDATE pages_show SET sort_kod =14 WHERE page_url = 281;
UPDATE pages_show SET sort_kod =15 WHERE page_url = 192;
UPDATE pages_show SET sort_kod =15 WHERE page_url = 123;
UPDATE pages_show SET sort_kod =16 WHERE page_url = 166;
UPDATE pages_show SET sort_kod =16 WHERE page_url = 57;
UPDATE pages_show SET sort_kod =17 WHERE page_url = 137;
UPDATE pages_show SET sort_kod =17 WHERE page_url = 97;
UPDATE pages_show SET sort_kod =18 WHERE page_url = 111;
UPDATE pages_show SET sort_kod =19 WHERE page_url = 131;
UPDATE pages_show SET sort_kod =20 WHERE page_url = 177;
UPDATE pages_show SET sort_kod =21 WHERE page_url = 240;
UPDATE pages_show SET sort_kod =22 WHERE page_url = 133;
UPDATE pages_show SET sort_kod =23 WHERE page_url = 244;
UPDATE pages_show SET sort_kod =24 WHERE page_url = 262;
UPDATE pages_show SET sort_kod =25 WHERE page_url = 296;
UPDATE pages_show SET sort_kod =26 WHERE page_url = 344;
UPDATE pages_show SET sort_kod =27 WHERE page_url = 345;
UPDATE pages_show SET sort_kod =28 WHERE page_url = 347;
UPDATE pages_show SET sort_kod =30 WHERE page_url = 201;
UPDATE pages_show SET sort_kod =321 WHERE page_url = 9;
UPDATE pages_show SET sort_kod =338 WHERE page_url = 13;

DROP TRIGGER pages_add on pages; 
DROP FUNCTION add_to_pages();
DROP TRIGGER actions_show_add on actions_show; 
DROP FUNCTION add_to_actions_show(); 
DROP TRIGGER role_actions_add on role_actions; 
DROP FUNCTION add_to_role_actions(); 
DROP TRIGGER role_pages_add on role_pages; 
DROP FUNCTION add_to_role_pages(); 
DROP TRIGGER page_links_add on page_links; 
DROP FUNCTION add_to_page_links(); 

update actions_show set sign = sort_kod::varchar||act_dd::varchar||act_tip::varchar||nzp_act::varchar||cur_page||'-'||nzp_ash||'actions_show'; 
update role_actions set sign = nzp_role::varchar||nzp_page::varchar||nzp_act::varchar||'-'||id::varchar||'role_actions' 
where nzp_role >= 10 and nzp_role < 1000; 
update role_pages set sign = nzp_role::varchar||nzp_page::varchar||'-'||id::varchar||'role_pages' 
where nzp_role >= 10 and nzp_role < 1000; 
update pages_show set sign = sort_kod::varchar||up_kod::varchar||page_url||cur_page||'-'||nzp_psh||'pages_show';
