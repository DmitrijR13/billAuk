SET SEARCH_PATH TO public;

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
-- Договоры ЖКУ
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (65, '~/kart/sprav/new_contracts.aspx', 'Договоры ЖКУ', 'Договоры ЖКУ', 'переходит на форму для просмотра и управления договорами на оказание жилищно-коммунальных услуг', 73, 73);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (65, 169, 15, 0, 0);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (65, 169, 86);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES  (65, null, null, 73);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act) VALUES  (942, 65, 169);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (933, 65, 169, null);

INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (10, 65);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (15, 65);

--Доступные услуги
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (114, '~/kart/serv/new_availableservices.aspx', 'Доступные услуги', 'Доступные услуги', 'переходит на страницу доступных услуг', 73, 73);
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (115, '~/kart/serv/new_availableservice.aspx', 'Доступная услуга', 'Доступная услуга', 'переходит на страницу доступная услуга', null, null);

INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (114, 5, 5, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (114, 76, 76, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (114, 701, 701, 2, 3);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (114, 702, 702, 2, 3);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (114, 703, 703, 2, 3);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (114, 705, 705, 2, 3);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (115, 5, 5, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (115, 21, 21, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (115, 26, 26, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (115, 610, 610, 2, 1);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (115, 611, 611, 2, 1);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (115, 76, 76, 3, 0);

INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (114, 5, 114);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (114, 76, 115);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (115, 21, 115);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (115, 26, 115);


INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (10, 114, 702, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (31, 114, 702, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (10, 114, 5, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (31, 114, 5, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (10, 114, 701, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (31, 114, 701, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (10, 114, 705, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (31, 114, 705, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (10, 114, 76, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (31, 114, 76, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (10, 114, 703, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (31, 114, 703, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (10, 115, 610, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (31, 115, 610, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (901, 115, 26, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (901, 115, 21, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (901, 115, 611, null);

INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (10, 114);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (31, 114);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (10, 115);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (31, 115);

-- перечень услуг
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (142, '~/kart/serv/new_spisservdom.aspx', 'Перечень услуг', 'Перечень услуг дома', 'переходит на список услуг для выбранного дома', 71, 71);

INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (142, 5, 5, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (142, 80, 80, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (142, 70, 70, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (142, 61, 61, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (142, 4, 4, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (142, 22, 22, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (142, 610, 610, 2, 1);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (142, 611, 611, 2, 1);

INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (142, 5, 142);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (142, 22, 142);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (142, 4, 142);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (142, 61, 142);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (142, 70, 142);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (142, 80, 50);


INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (10, 142, 5, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (10, 142, 80, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (10, 142, 610, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (921, 142, 70, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (901, 142, 611, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (901, 142, 22, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (901, 142, 4, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (901, 142, 61, 611);

INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (10, 142);

--счета контрагентов

INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (143, '~/finances/new_payerrequisites.aspx', 'Счета контрагентов', 'Счета контрагентов', 'переходит к списку расчетных счетов выбранного контрагента', 290, 290);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (143, 4, 4, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (143, 61, 61, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (143, 5, 5, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (143, 610, 610, 2, 1);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (143, 611, 611, 2, 1);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (143, 26, 1899, 3, 0);

INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (143, 4, 232);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (143, 61, 232);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (143, 5, 232);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (143, 26, 232);

INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (915, 143, 611, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (915, 143, 26, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (915, 143, 61, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (915, 143, 4, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (15, 143, 610, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (15, 143, 5, null);

INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (15, 143);

--Сальдо по перечислениям
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (144, '~/kart/charge/new_distrib_dom_supp.aspx', 'Сальдо по перечислениям', 'Сальдо по перечислениям', 'переходит на форму с данными о распределении оплат', 72, 72);

INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (144, 1, 1, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (144, 108, 108, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (144, 124, 124, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (144, 875, 875, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (144, 610, 610, 2, 1);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (144, 701, 701, 2, 3);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (144, 702, 702, 2, 3);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (144, 703, 703, 2, 3);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (144, 705, 705, 2, 3);

INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (144, 1, 144);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (144, 875, 144);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (144, 108, 144);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES  (144, null, null, 235);


INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (15, 144, 1, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (11, 144, 1, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (15, 144, 610, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (11, 144, 610, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (15, 144, 701, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (11, 144, 701, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (15, 144, 702, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (11, 144, 702, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (15, 144, 703, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (11, 144, 703, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (15, 144, 705, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (11, 144, 705, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (915, 144, 108, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (915, 144, 875, null);

INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (15, 144);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (11, 144);

--Контрагенты
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (145, '~/finances/sprav/new_contractors.aspx', 'Контрагенты', 'Контрагенты', 'отображает справочник контрагентов', 73, 73);

INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (145, 5, 5, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (145, 4, 4, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (145, 61, 61, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (145, 611, 611, 2, 1);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (145, 610, 610, 2, 1);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (145, 158, 158, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (145, 77, 77, 0, 0);

INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (145, 4, 145);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (145, 5, 145);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (145, 61, 145);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (145, 158, 145);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (145, 77, 28);

INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES  (145, null, null, 290);

INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (15, 145, 5, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (15, 145, 610, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (15, 145, 77, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (942, 145, 4, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (942, 145, 158, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (942, 145, 61, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (942, 145, 611, null);
delete from role_actions where nzp_page=145 and nzp_role=805;
delete from roleskey where nzp_role=15 and tip=105 and kod=805;
delete from s_roles where nzp_role=805;
INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (15, 145);

--редактирование Договоров ЖКУ
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (86, '~/kart/sprav/edit_new_contracts.aspx', 'Редактирование договора ЖКУ', 'Редактирование договора ЖКУ', 'переходит на форму редактирования договора ЖКУ', null, null);
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES  (920, 'Изменить обл. действия', 'Изменяет область действия выбранного договора');
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (86, 170, 170, 0,0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (86, 920, 920, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (86, 103, 103, 0, 0);

INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (86, 170, 65);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (65, 169, 86);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (86, 103, 175);
INSERT INTO img_lnk (cur_page, tip, kod, img_url) VALUES  (0, 2, 920, 'edit.png');


INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (942, 86, 920, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (942, 86, 170, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (10, 86, 103, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (933, 86, 170, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (933, 86, 920, null);

INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (10, 86);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (15, 86);

--Услуги

INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (116, '~/kart/serv/newspisserv.aspx', 'Перечень услуг', 'Перечень услуг', 'переходит на форму перечня услуг', 70, 70);
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (117, '~/kart/serv/newspisserv.aspx', 'Перечень услуг', 'Групповые операции с услугами', 'переходит на список услуг для выполнения групповой операции над выбранными лицевыми счетами', 74, 41);
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (118, '~/kart/serv/newspisserv.aspx', 'Перечень услуг', 'Групповые операции с услугами для выбранных домов', 'переходит на список услуг для выполнения групповой операции над лицевыми счетами выбранных домов', 74, 42);
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (119, '~/kart/serv/newserv.aspx', 'Поставщики и формулы расчета', 'Поставщики и формулы расчета', 'переходит на список периодов действия поставщиков и формул расчета для выбранной услуги и лицевого счета', null, null);
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (132, '~/kart/serv/newserv.aspx', 'Поставщики и формулы расчета', 'Групповые операции с услугой', 'переходит в окно добавления или удаления периода действия поставщиков и формул расчета для выбранной услуги и выбранных лицевых счетов', null, null);
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (140, '~/kart/serv/newserv.aspx', 'Поставщики и формулы расчета', 'Групповые операции с услугой для выбранных домов', 'переходит в окно добавления или удаления периода действия поставщиков и формул расчета для выбранной услуги и лицевых счетов выбранных домов', null, null);
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (141, '~/finances/newpayertransfer_supp.aspx', 'Перечисления контрагентам', 'Перечисления контрагентам', 'переходит на форму перечисления контрагентам средств по договорам', 235, 235);

INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (116, 5, 5, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (116, 70, 70, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (116, 76, 76, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (116, 77, 77, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (116, 610, 610, 2, 1);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (117, 5, 5, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (117, 611, 611, 2, 1);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (118, 5, 5, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (118, 611, 611, 2, 1);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (119, 5, 5, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (119, 21, 21, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (119, 22, 22, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (119, 70, 70, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (119, 610, 610, 2, 1);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (119, 611, 611, 2, 1);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (119, 76, 76, 3, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (132, 21, 21, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (132, 22, 22, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (132, 611, 611, 2, 1);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (140, 21, 21, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (140, 22, 22, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (140, 611, 611, 2, 1);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (141, 170, 170, 0, 0);

INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (116, 5, 116);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (116, 76, 119);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (116, 77, 176);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (117, 5, 117);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (118, 5, 118);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (119, 5, 119);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (119, 21, 119);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (119, 22, 119);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (132, 21, 132);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (132, 22, 132);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (140, 21, 140);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (140, 22, 140);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (141, 170, 141);


INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES  (119, null, null, 71);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES  (119, null, null, 70);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES  (119, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES  (119, null, 6, null);


INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (10, 116, 5, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (11, 116, 5, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (10, 116, 76, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (11, 116, 76, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (10, 116, 77, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (11, 116, 77, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (10, 116, 610, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (11, 116, 610, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (10, 119, 5, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (11, 119, 5, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (10, 119, 610, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (11, 119, 610, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (921, 116, 70, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (921, 119, 70, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (901, 117, 5, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (901, 117, 611, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (901, 118, 5, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (901, 118, 611, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (901, 119, 5, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (901, 119, 21, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (901, 119, 22, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (901, 119, 611, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (901, 132, 21, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (901, 132, 22, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (901, 132, 611, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (901, 140, 21, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (901, 140, 22, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (901, 140, 611, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (806, 141, 170, null);

INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (10, 116);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (11, 116);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (10, 119);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (11, 119);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (901, 117);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (901, 118);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (901, 132);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (901, 140);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (11, 141);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (15, 141);

--страницы Договоры ЕРЦ

INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (60, '~/finances/contracts/new_contract_details.aspx', 'Договоры ЕРЦ', 'Договоры ЕРЦ', 'переходит на страницу договоров ЕРЦ', 73, 73);

INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (60, 170, 1, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (60, 169, 2, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (60, 158, 3, 0, 0);

INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (60, 170, 60);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (60, 169, 60);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (60, 158, 60);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES  (60, null, null, 73);

INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (942, 60, 170, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (942, 60, 169, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (942, 60, 158, null);

INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (10, 60);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (15, 60);

delete from pages_show where cur_page in (232,355,353,359,95,96,190,104,105,182,183,224,352, 359) or page_url in (232,355,353,359,95,96,190,104,105,182,183,224,352,359);
DELETE FROM actions_show WHERE cur_page in(232,355,353,359,95,96,190,104,105,182,183,224,352,359);
DELETE FROM actions_lnk WHERE cur_page in(232,355,353,359,95,96,190,104,105,182,183,224,352,359);
DELETE FROM role_actions WHERE  nzp_page in(232,355,353,359,95,96,190,104,105,182,183,224,352,359);
DELETE FROM role_pages WHERE nzp_page in(232,355,353,359,95,96,190,104,105,182,183,224,352,359);
DELETE FROM pages WHERE nzp_page in(232,355,353,359,95,96,190,104,105,182,183,224,352,359);

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

DROP TRIGGER pages_add on pages; 
DROP FUNCTION add_to_pages();
DROP TRIGGER actions_show_add on actions_show; 
DROP FUNCTION add_to_actions_show(); 
DROP TRIGGER actions_lnk_add on actions_lnk; 
DROP FUNCTION add_to_actions_lnk(); 
DROP TRIGGER role_actions_add on role_actions; 
DROP FUNCTION add_to_role_actions(); 
DROP TRIGGER role_pages_add on role_pages; 
DROP FUNCTION add_to_role_pages(); 
DROP TRIGGER page_links_add on page_links; 
DROP FUNCTION add_to_page_links();
DROP TRIGGER s_actions_add on s_actions; 
DROP FUNCTION add_to_s_actions();  
DROP TRIGGER img_lnk_add on img_lnk; 
DROP FUNCTION add_to_img_lnk(); 



update actions_show set sign = sort_kod::varchar||act_dd::varchar||act_tip::varchar||nzp_act::varchar||cur_page||'-'||nzp_ash||'actions_show'; 
update role_actions set sign = nzp_role::varchar||nzp_page::varchar||nzp_act::varchar||'-'||id::varchar||'role_actions' 
where nzp_role >= 10 and nzp_role < 1000; 
update role_pages set sign = nzp_role::varchar||nzp_page::varchar||'-'||id::varchar||'role_pages' 
where nzp_role >= 10 and nzp_role < 1000; 
update pages_show set sign = sort_kod::varchar||up_kod::varchar||page_url||cur_page||'-'||nzp_psh||'pages_show';
